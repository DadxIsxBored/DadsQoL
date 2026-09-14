using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DadsQoL;

internal static class FarmersHoe
{
    private const string PrefabName = "DadsQoL_FarmersHoe_Flatten";
    private const string PieceName = "Flatten Terrain";
    private const string VanillaLevelPrefabName = "mud_road_v2";
    private static readonly FieldInfo NamedPrefabsField = AccessTools.Field(typeof(ZNetScene), "m_namedPrefabs");

    private static GameObject? _flattenPrefab;

    internal static void Refresh(ObjectDB? objectDb = null)
    {
        objectDb ??= ObjectDB.instance;
        if (objectDb == null)
        {
            return;
        }

        GameObject hoePrefab = objectDb.GetItemPrefab("Hoe");
        ItemDrop? hoe = hoePrefab == null ? null : hoePrefab.GetComponent<ItemDrop>();
        PieceTable? table = hoe?.m_itemData.m_shared.m_buildPieces;
        if (table == null)
        {
            DadsQoLPlugin.ModLog.LogWarning("FarmersHoe could not locate the vanilla Hoe piece table.");
            return;
        }

        GameObject? registered = table.m_pieces.FirstOrDefault(piece => piece != null && piece.name == PrefabName);
        if (registered != null)
        {
            _flattenPrefab = registered;
        }

        if (!DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.FarmersHoeEnabled))
        {
            table.m_pieces.RemoveAll(piece => piece != null && piece.name == PrefabName);
            RefreshAvailablePieces();
            return;
        }

        if (_flattenPrefab == null)
        {
            GameObject? template = FindLevelTemplate(table);
            if (template == null)
            {
                DadsQoLPlugin.ModLog.LogWarning("FarmersHoe could not locate the vanilla Level Ground terrain operation.");
                return;
            }

            _flattenPrefab = CreateFlattenPrefab(template);
        }

        if (!table.m_pieces.Contains(_flattenPrefab))
        {
            table.m_pieces.Add(_flattenPrefab);
        }

        RegisterNetworkPrefab(ZNetScene.instance);
        RefreshAvailablePieces();
    }

    internal static void RegisterNetworkPrefab(ZNetScene? scene)
    {
        if (scene == null || _flattenPrefab == null)
        {
            return;
        }

        int hash = StableHash(PrefabName);
        Dictionary<int, GameObject>? namedPrefabs = NamedPrefabsField.GetValue(scene) as Dictionary<int, GameObject>;
        if (namedPrefabs == null)
        {
            return;
        }

        if (namedPrefabs.TryGetValue(hash, out GameObject existing) && existing != _flattenPrefab)
        {
            DadsQoLPlugin.ModLog.LogError($"FarmersHoe prefab hash collision with {existing.name}; Flatten Terrain was not registered.");
            return;
        }

        namedPrefabs[hash] = _flattenPrefab;
        if (!scene.m_prefabs.Contains(_flattenPrefab))
        {
            scene.m_prefabs.Add(_flattenPrefab);
        }
    }

    private static GameObject? FindLevelTemplate(PieceTable table)
    {
        GameObject? template = table.m_pieces.FirstOrDefault(piece =>
            piece != null && piece.name == VanillaLevelPrefabName);
        if (template != null)
        {
            return template;
        }

        return table.m_pieces.FirstOrDefault(piece =>
        {
            TerrainOp? operation = piece == null ? null : piece.GetComponentInChildren<TerrainOp>(true);
            return operation != null && operation.m_settings != null && operation.m_settings.m_level;
        });
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            int hash1 = 5381;
            int hash2 = hash1;
            for (int index = 0; index < value.Length && value[index] != '\0'; index += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ value[index];
                if (index == value.Length - 1 || value[index + 1] == '\0')
                {
                    break;
                }
                hash2 = ((hash2 << 5) + hash2) ^ value[index + 1];
            }
            return hash1 + hash2 * 1566083941;
        }
    }

    private static void RefreshAvailablePieces()
    {
        if (Player.m_localPlayer != null)
        {
            Player.m_localPlayer.UpdateAvailablePiecesList();
        }
    }

    private static GameObject CreateFlattenPrefab(GameObject template)
    {
        GameObject prefab = Object.Instantiate(template);
        prefab.name = PrefabName;
        prefab.SetActive(false);
        Object.DontDestroyOnLoad(prefab);

        Piece piece = prefab.GetComponent<Piece>();
        piece.m_name = PieceName;
        piece.m_description = "Flatten terrain to a single plane without smoothing.";
        piece.m_enabled = true;

        TerrainOp operation = prefab.GetComponentInChildren<TerrainOp>(true);
        operation.m_settings.m_level = true;
        operation.m_settings.m_raise = false;
        operation.m_settings.m_smooth = false;
        operation.m_settings.m_paintCleared = false;

        return prefab;
    }
}

[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
internal static class FarmersHoeObjectDbCopyPatch
{
    private static void Postfix(ObjectDB __instance)
    {
        FarmersHoe.Refresh(__instance);
    }
}

[HarmonyPatch(typeof(PieceTable), nameof(PieceTable.UpdateAvailable))]
internal static class FarmersHoeAvailabilityPatch
{
    private static void Prefix(PieceTable __instance, HashSet<string> __0, out bool __state)
    {
        __state = false;
        if (!DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.FarmersHoeEnabled) ||
            !__instance.m_pieces.Any(piece => piece != null && piece.name == "DadsQoL_FarmersHoe_Flatten"))
        {
            return;
        }

        __state = __0.Add("Flatten Terrain");
    }

    private static void Postfix(HashSet<string> __0, bool __state)
    {
        if (__state)
        {
            __0.Remove("Flatten Terrain");
        }
    }
}

[HarmonyPatch(typeof(ObjectDB), "Awake")]
internal static class FarmersHoeObjectDbPatch
{
    private static void Postfix(ObjectDB __instance)
    {
        FarmersHoe.Refresh(__instance);
    }
}

[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class FarmersHoeZNetScenePatch
{
    private static void Postfix(ZNetScene __instance)
    {
        FarmersHoe.RegisterNetworkPrefab(__instance);
    }
}
