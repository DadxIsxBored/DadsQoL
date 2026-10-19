using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace DadsQoL;

internal static class MultiplayerSharing
{
    private const string HelloRpc = "DadsQoL_SharingHello";
    private const string RecipeRpc = "DadsQoL_SharingRecipe";
    private const string MapRpc = "DadsQoL_SharingMap";
    private const string StateRpc = "DadsQoL_SharingState";
    private const int MaximumMapBytes = 2 * 1024 * 1024;
    private static readonly FieldInfo KnownRecipesField = AccessTools.Field(typeof(Player), "m_knownRecipes");
    private static readonly FieldInfo BuildPiecesField = AccessTools.Field(typeof(ObjectDB), "m_buildPieces");
    private static readonly FieldInfo ExploredField = AccessTools.Field(typeof(Minimap), "m_explored");
    private static readonly FieldInfo ExploredOthersField = AccessTools.Field(typeof(Minimap), "m_exploredOthers");
    private static readonly FieldInfo FogTextureField = AccessTools.Field(typeof(Minimap), "m_fogTexture");
    private static readonly MethodInfo AddKnownRecipeMethod = AccessTools.Method(typeof(Player), "AddKnownRecipe");
    private static readonly MethodInfo AddKnownPieceMethod = AccessTools.Method(typeof(Player), "AddKnownPiece");
    private static readonly HashSet<string> ServerRecipes = new(StringComparer.Ordinal);
    private static byte[] _serverMap = Array.Empty<byte>();
    private static ZRoutedRpc? _router;
    private static long _worldId;
    private static bool _helloSent;
    private static bool _mapDirty;
    private static bool _applyingRecipes;
    private static float _nextMapSend;
    private static string? _savePath;
    internal static Player? LoadedPlayer;
    internal static Minimap? LoadedMap;

    internal static void Reset()
    {
        _router = null;
        _worldId = 0;
        _helloSent = false;
        _mapDirty = false;
        _savePath = null;
        ServerRecipes.Clear();
        _serverMap = Array.Empty<byte>();
        LoadedPlayer = null;
        LoadedMap = null;
    }

    internal static void Update()
    {
        ZNet net = ZNet.instance;
        ZRoutedRpc router = ZRoutedRpc.instance;
        if (net == null || router == null || net.GetWorld() == null) return;

        long worldId = net.GetWorldUID();
        if (_router != router || _worldId != worldId)
        {
            _router = router;
            _worldId = worldId;
            _helloSent = false;
            _mapDirty = false;
            ServerRecipes.Clear();
            _serverMap = Array.Empty<byte>();
            _savePath = null;
            router.Register<ZPackage>(HelloRpc, OnHello);
            router.Register<ZPackage>(RecipeRpc, OnRecipe);
            router.Register<ZPackage>(MapRpc, OnMap);
            router.Register<ZPackage>(StateRpc, OnState);
            if (net.IsServer() && worldId != 0) LoadServerState(worldId);
        }

        Player player = Player.m_localPlayer;
        Minimap map = Minimap.instance;
        if (player == null || map == null || LoadedPlayer != player || LoadedMap != map || worldId == 0) return;

        if (!_helloSent)
        {
            if (!net.IsServer() && net.GetServerPeer() == null) return;
            _helloSent = true;
            if (net.IsServer())
            {
                bool changed = MergeLocal(player, map);
                ApplyState(player, map);
                if (changed) BroadcastState();
            }
            else
            {
                router.InvokeRoutedRPC(net.GetServerPeer().m_uid, HelloRpc, MakeHello(player, map));
            }
            _nextMapSend = Time.unscaledTime + 10f;
        }

        if (DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.AutomaticMapSharing) &&
            (_mapDirty || Time.unscaledTime >= _nextMapSend + 110f) && Time.unscaledTime >= _nextMapSend)
        {
            _mapDirty = false;
            _nextMapSend = Time.unscaledTime + 10f;
            byte[] bits = GetMapBits(map);
            if (net.IsServer())
            {
                if (MergeMap(bits)) BroadcastState();
            }
            else
            {
                ZPackage package = new();
                package.Write(Compress(bits));
                ZNetPeer server = net.GetServerPeer();
                if (server != null) router.InvokeRoutedRPC(server.m_uid, MapRpc, package);
            }
        }
    }

    internal static void RecipeDiscovered(Player player, Recipe recipe)
    {
        string name = recipe?.m_item?.m_itemData?.m_shared?.m_name ?? string.Empty;
        KnownDiscovered(player, name);
    }

    internal static void PieceDiscovered(Player player, Piece piece) =>
        KnownDiscovered(player, piece?.m_name ?? string.Empty);

    private static void KnownDiscovered(Player player, string name)
    {
        if (_applyingRecipes || !_helloSent || player != Player.m_localPlayer ||
            !DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.SharedRecipes)) return;
        if (string.IsNullOrEmpty(name)) return;
        ZNet net = ZNet.instance;
        if (net == null || _router == null) return;
        if (net.IsServer())
        {
            if (ServerRecipes.Add(name))
            {
                SaveServerState();
                BroadcastState();
            }
        }
        else
        {
            ZPackage package = new();
            package.Write(name);
            ZNetPeer server = net.GetServerPeer();
            if (server != null) _router.InvokeRoutedRPC(server.m_uid, RecipeRpc, package);
        }
    }

    internal static void MapExplored(Minimap map)
    {
        if (map == Minimap.instance) _mapDirty = true;
    }

    private static ZPackage MakeHello(Player player, Minimap map)
    {
        ZPackage package = new();
        bool recipes = DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.SharedRecipes);
        bool sharing = DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.AutomaticMapSharing);
        package.Write(recipes);
        package.Write(sharing);
        HashSet<string> known = recipes ? GetKnownRecipes(player) : new HashSet<string>();
        package.Write(known.Count);
        foreach (string name in known) package.Write(name);
        package.Write(sharing ? Compress(GetMapBits(map)) : Array.Empty<byte>());
        return package;
    }

    private static void OnHello(long sender, ZPackage package)
    {
        ZNet net = ZNet.instance;
        if (net == null || !net.IsServer() || net.GetPeer(sender) == null) return;
        bool recipes = package.ReadBool();
        bool sharing = package.ReadBool();
        int count = package.ReadInt();
        if (count < 0 || count > 10000) return;
        bool changed = false;
        for (int i = 0; i < count; i++)
        {
            string name = package.ReadString();
            if (recipes && DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.SharedRecipes) &&
                name.Length is > 0 and < 256) changed |= ServerRecipes.Add(name);
        }
        byte[] data = package.ReadByteArray();
        if (sharing && DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.AutomaticMapSharing))
            changed |= MergeMap(Decompress(data));
        if (changed) SaveServerState();
        SendState(sender);
        if (changed) BroadcastState(sender);
    }

    private static void OnRecipe(long sender, ZPackage package)
    {
        ZNet net = ZNet.instance;
        if (net == null || !net.IsServer() || net.GetPeer(sender) == null ||
            !DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.SharedRecipes)) return;
        string name = package.ReadString();
        if (name.Length is > 0 and < 256 && ServerRecipes.Add(name))
        {
            SaveServerState();
            BroadcastState();
        }
    }

    private static void OnMap(long sender, ZPackage package)
    {
        ZNet net = ZNet.instance;
        if (net == null || !net.IsServer() || net.GetPeer(sender) == null ||
            !DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.AutomaticMapSharing)) return;
        if (MergeMap(Decompress(package.ReadByteArray())))
        {
            SaveServerState();
            BroadcastState();
        }
    }

    private static void OnState(long sender, ZPackage package)
    {
        ZNet net = ZNet.instance;
        if (net == null || net.IsServer() || net.GetServerPeer() == null || sender != net.GetServerPeer().m_uid) return;
        Player player = Player.m_localPlayer;
        Minimap map = Minimap.instance;
        if (player == null || map == null || LoadedPlayer != player || LoadedMap != map) return;
        int count = package.ReadInt();
        if (count < 0 || count > 10000) return;
        HashSet<string> names = new(StringComparer.Ordinal);
        for (int i = 0; i < count; i++) names.Add(package.ReadString());
        byte[] bits = Decompress(package.ReadByteArray());
        if (DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.SharedRecipes)) ApplyRecipes(player, names);
        if (DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.AutomaticMapSharing)) ApplyMap(map, bits);
    }

    private static bool MergeLocal(Player player, Minimap map)
    {
        bool changed = false;
        if (DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.SharedRecipes))
            foreach (string name in GetKnownRecipes(player)) changed |= ServerRecipes.Add(name);
        if (DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.AutomaticMapSharing))
            changed |= MergeMap(GetMapBits(map));
        if (changed) SaveServerState();
        return changed;
    }

    private static void ApplyState(Player player, Minimap map)
    {
        if (DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.SharedRecipes)) ApplyRecipes(player, ServerRecipes);
        if (DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.AutomaticMapSharing)) ApplyMap(map, _serverMap);
    }

    private static void ApplyRecipes(Player player, IEnumerable<string> names)
    {
        ObjectDB db = ObjectDB.instance;
        if (db == null) return;
        _applyingRecipes = true;
        try
        {
            foreach (string name in names)
            {
                if (player.IsRecipeKnown(name)) continue;
                foreach (Recipe recipe in db.m_recipes)
                {
                    if (recipe?.m_item?.m_itemData?.m_shared?.m_name != name) continue;
                    AddKnownRecipeMethod.Invoke(player, new object[] { recipe });
                    goto NextName;
                }
                foreach (Piece piece in (List<Piece>)BuildPiecesField.GetValue(db))
                {
                    if (piece?.m_name != name) continue;
                    AddKnownPieceMethod.Invoke(player, new object[] { piece });
                    break;
                }
                NextName:;
            }
        }
        finally { _applyingRecipes = false; }
    }

    private static HashSet<string> GetKnownRecipes(Player player) =>
        (HashSet<string>)KnownRecipesField.GetValue(player);

    private static byte[] GetMapBits(Minimap map)
    {
        BitArray own = (BitArray)ExploredField.GetValue(map);
        BitArray others = (BitArray)ExploredOthersField.GetValue(map);
        byte[] bytes = new byte[(own.Length + 7) / 8];
        own.CopyTo(bytes, 0);
        byte[] shared = new byte[bytes.Length];
        others.CopyTo(shared, 0);
        for (int i = 0; i < bytes.Length; i++) bytes[i] |= shared[i];
        return bytes;
    }

    private static bool MergeMap(byte[] incoming)
    {
        if (incoming.Length == 0 || incoming.Length > MaximumMapBytes ||
            (_serverMap.Length != 0 && _serverMap.Length != incoming.Length)) return false;
        if (_serverMap.Length == 0) _serverMap = new byte[incoming.Length];
        bool changed = false;
        for (int i = 0; i < incoming.Length; i++)
        {
            byte merged = (byte)(_serverMap[i] | incoming[i]);
            if (merged == _serverMap[i]) continue;
            _serverMap[i] = merged;
            changed = true;
        }
        return changed;
    }

    private static void ApplyMap(Minimap map, byte[] bits)
    {
        BitArray own = (BitArray)ExploredField.GetValue(map);
        BitArray others = (BitArray)ExploredOthersField.GetValue(map);
        if (bits.Length != (own.Length + 7) / 8) return;
        Texture2D fog = (Texture2D)FogTextureField.GetValue(map);
        Color[]? pixels = null;
        bool changed = false;
        for (int i = 0; i < own.Length; i++)
        {
            if ((bits[i >> 3] & (1 << (i & 7))) == 0 || own[i] || others[i]) continue;
            pixels ??= fog.GetPixels();
            others[i] = true;
            pixels[i].g = 0f;
            changed = true;
        }
        if (!changed) return;
        fog.SetPixels(pixels!);
        fog.Apply();
    }

    private static void SendState(long peerId)
    {
        if (_router == null) return;
        ZPackage package = new();
        bool recipes = DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.SharedRecipes);
        package.Write(recipes ? ServerRecipes.Count : 0);
        if (recipes) foreach (string name in ServerRecipes) package.Write(name);
        package.Write(DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.AutomaticMapSharing)
            ? Compress(_serverMap) : Array.Empty<byte>());
        _router.InvokeRoutedRPC(peerId, StateRpc, package);
    }

    private static void BroadcastState(long except = 0)
    {
        ZNet net = ZNet.instance;
        if (net == null || !net.IsServer()) return;
        foreach (ZNetPeer peer in net.GetPeers())
            if (peer.IsReady() && peer.m_uid != except) SendState(peer.m_uid);
        if (Player.m_localPlayer != null && Minimap.instance != null)
            ApplyState(Player.m_localPlayer, Minimap.instance);
    }

    private static byte[] Compress(byte[] bytes)
    {
        if (bytes.Length == 0) return bytes;
        using MemoryStream output = new();
        using (GZipStream gzip = new(output, CompressionMode.Compress, true)) gzip.Write(bytes, 0, bytes.Length);
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] bytes)
    {
        if (bytes.Length == 0) return bytes;
        using MemoryStream input = new(bytes);
        using GZipStream gzip = new(input, CompressionMode.Decompress);
        using MemoryStream output = new();
        byte[] buffer = new byte[8192];
        int read;
        while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (output.Length + read > MaximumMapBytes) return Array.Empty<byte>();
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private static void LoadServerState(long worldId)
    {
        _savePath = Path.Combine(Paths.ConfigPath, "DadsQoL", "multiplayer-" + worldId + ".dat");
        if (!File.Exists(_savePath)) return;
        try { ReadState(File.ReadAllBytes(_savePath)); }
        catch (Exception exception) { DadsQoLPlugin.ModLog.LogWarning("Could not read multiplayer state: " + exception.Message); }
    }

    private static void SaveServerState()
    {
        if (_savePath == null) return;
        try
        {
            // Merge the current file before every write, including changes made since startup.
            if (File.Exists(_savePath) && !ReadState(File.ReadAllBytes(_savePath))) return;
            ZPackage package = new();
            package.Write(1);
            package.Write(ServerRecipes.Count);
            foreach (string name in ServerRecipes) package.Write(name);
            package.Write(_serverMap);
            Directory.CreateDirectory(Path.GetDirectoryName(_savePath));
            File.WriteAllBytes(_savePath, package.GetArray());
        }
        catch (Exception exception) { DadsQoLPlugin.ModLog.LogWarning("Could not save multiplayer state: " + exception.Message); }
    }

    private static bool ReadState(byte[] bytes)
    {
        ZPackage package = new(bytes);
        if (package.ReadInt() != 1) return false;
        int count = package.ReadInt();
        if (count < 0 || count > 10000) return false;
        for (int i = 0; i < count; i++) ServerRecipes.Add(package.ReadString());
        byte[] savedMap = package.ReadByteArray();
        if (savedMap.Length != 0 && _serverMap.Length != 0 && savedMap.Length != _serverMap.Length) return false;
        MergeMap(savedMap);
        return true;
    }
}

[HarmonyPatch(typeof(Player), "AddKnownRecipe")]
internal static class SharedRecipePatch
{
    private static void Postfix(Player __instance, Recipe recipe) => MultiplayerSharing.RecipeDiscovered(__instance, recipe);
}

[HarmonyPatch(typeof(Player), "AddKnownPiece")]
internal static class SharedPiecePatch
{
    private static void Postfix(Player __instance, Piece piece) => MultiplayerSharing.PieceDiscovered(__instance, piece);
}

[HarmonyPatch(typeof(Player), nameof(Player.Load), new[] { typeof(ZPackage) })]
internal static class SharedRecipeLoadPatch
{
    private static void Postfix(Player __instance) => MultiplayerSharing.LoadedPlayer = __instance;
}

[HarmonyPatch(typeof(Minimap), "LoadMapData")]
internal static class SharedMapLoadPatch
{
    private static void Postfix(Minimap __instance) => MultiplayerSharing.LoadedMap = __instance;
}

[HarmonyPatch(typeof(Minimap), "Explore", new[] { typeof(int), typeof(int) })]
internal static class SharedMapExplorePatch
{
    private static void Postfix(Minimap __instance, bool __result)
    {
        if (__result) MultiplayerSharing.MapExplored(__instance);
    }
}
