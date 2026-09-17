using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DadsQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class DadsQoLPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.dadisbored.dadsqol";
    public const string PluginName = "DadsQoL";
    public const string PluginVersion = "1.3.7";

    internal static ConfigEntry<bool> ModEnabled = null!;
    internal static ManualLogSource ModLog = null!;
    internal static ConfigEntry<bool> CarryWeightEnabled = null!;
    internal static ConfigEntry<float> CarryWeight = null!;
    internal static ConfigEntry<bool> PickupRadiusEnabled = null!;
    internal static ConfigEntry<float> PickupRadius = null!;
    internal static ConfigEntry<bool> EquipmentInWaterEnabled = null!;
    internal static ConfigEntry<bool> MassFarmingEnabled = null!;
    internal static ConfigEntry<KeyboardShortcut> MassFarmingKeyboardShortcut = null!;
    internal static ConfigEntry<KeyboardShortcut> MassFarmingControllerShortcut = null!;
    internal static ConfigEntry<float> MassHarvestRadius = null!;
    internal static ConfigEntry<int> PlantGridWidth = null!;
    internal static ConfigEntry<int> PlantGridLength = null!;
    internal static ConfigEntry<bool> IgnorePlantingStamina = null!;
    internal static ConfigEntry<bool> IgnorePlantingDurability = null!;
    internal static ConfigEntry<bool> CenterPlantingGridWidth = null!;
    internal static ConfigEntry<bool> CenterPlantingGridLength = null!;
    internal static ConfigEntry<bool> EternalFuelEnabled = null!;
    internal static ConfigEntry<bool> EternalCampfires = null!;
    internal static ConfigEntry<bool> EternalBonfires = null!;
    internal static ConfigEntry<bool> EternalHearths = null!;
    internal static ConfigEntry<bool> EternalSconces = null!;
    internal static ConfigEntry<bool> EternalStandingIronTorches = null!;
    internal static ConfigEntry<bool> EternalStandingWoodTorches = null!;
    internal static ConfigEntry<bool> EternalStandingGreenTorches = null!;
    internal static ConfigEntry<bool> EternalStandingBlueTorches = null!;
    internal static ConfigEntry<bool> EternalStandingBraziers = null!;
    internal static ConfigEntry<bool> EternalHangingBraziers = null!;
    internal static ConfigEntry<bool> EternalJackOTurnips = null!;
    internal static ConfigEntry<bool> EternalStoneOvens = null!;
    internal static ConfigEntry<bool> EternalHotTubs = null!;
    internal static ConfigEntry<bool> EternalSmelters = null!;
    internal static ConfigEntry<bool> EternalBlastFurnaces = null!;
    internal static ConfigEntry<bool> EternalEitrRefineries = null!;
    internal static ConfigEntry<string> EternalCustomPrefabs = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        ModLog = Logger;
        ModEnabled = Config.Bind(
            "1 - General",
            "Enabled",
            true,
            "Enable or disable every DadsQoL feature.");

        CarryWeightEnabled = Config.Bind(
            "2 - Carry Weight",
            "Enabled",
            true,
            "Enable the configured base carry weight.");
        CarryWeight = Config.Bind(
            "2 - Carry Weight",
            "Base Carry Weight",
            5000f,
            "Base carry weight before food, equipment, status-effect, and world modifiers. Vanilla is 300.");

        PickupRadiusEnabled = Config.Bind(
            "3 - Pickup Radius",
            "Enabled",
            true,
            "Enable the configured automatic pickup radius.");
        PickupRadius = Config.Bind(
            "3 - Pickup Radius",
            "Radius",
            6f,
            "Automatic pickup radius in meters. Vanilla is 2.");

        EquipmentInWaterEnabled = Config.Bind(
            "4 - Equipment In Water",
            "Enabled",
            true,
            "Allow tools, weapons, shields, bows, and other hand equipment to remain usable while swimming.");

        MassFarmingEnabled = Config.Bind(
            "5 - Mass Farming",
            "Enabled",
            true,
            "Enable mass harvesting and grid planting.");
        MassFarmingKeyboardShortcut = Config.Bind(
            "5 - Mass Farming",
            "Keyboard Shortcut",
            new KeyboardShortcut(KeyCode.LeftShift),
            "Hold while harvesting or planting to use mass farming.");
        MassFarmingControllerShortcut = Config.Bind(
            "5 - Mass Farming",
            "Controller Shortcut",
            new KeyboardShortcut(KeyCode.JoystickButton4),
            "Hold while harvesting or planting to use mass farming with a controller.");
        MassHarvestRadius = Config.Bind(
            "5 - Mass Farming",
            "Harvest Radius",
            5f,
            "Radius in meters for harvesting matching pickables and extracting nearby beehives.");
        PlantGridWidth = Config.Bind(
            "5 - Mass Farming",
            "Plant Grid Width",
            5,
            "Number of plants across the grid.");
        PlantGridLength = Config.Bind(
            "5 - Mass Farming",
            "Plant Grid Length",
            5,
            "Number of plants along the grid.");
        IgnorePlantingStamina = Config.Bind(
            "5 - Mass Farming",
            "Ignore Planting Stamina",
            false,
            "Do not consume stamina for the additional plants.");
        IgnorePlantingDurability = Config.Bind(
            "5 - Mass Farming",
            "Ignore Planting Durability",
            false,
            "Do not consume cultivator durability for the additional plants.");
        CenterPlantingGridWidth = Config.Bind(
            "5 - Mass Farming",
            "Center Grid Width",
            true,
            "Center the grid across the first plant instead of extending to one side.");
        CenterPlantingGridLength = Config.Bind(
            "5 - Mass Farming",
            "Center Grid Length",
            true,
            "Center the grid along the first plant instead of extending forward.");

        EternalFuelEnabled = Config.Bind("6 - Eternal Fuel", "Enabled", true, "Keep selected fuel-burning pieces fully fueled.");
        EternalCampfires = Config.Bind("6 - Eternal Fuel", "Campfires", true, "Keep campfires fueled.");
        EternalBonfires = Config.Bind("6 - Eternal Fuel", "Bonfires", true, "Keep bonfires fueled.");
        EternalHearths = Config.Bind("6 - Eternal Fuel", "Hearths", true, "Keep hearths fueled.");
        EternalSconces = Config.Bind("6 - Eternal Fuel", "Sconces", true, "Keep wall sconces fueled.");
        EternalStandingIronTorches = Config.Bind("6 - Eternal Fuel", "Standing Iron Torches", true, "Keep standing iron torches fueled.");
        EternalStandingWoodTorches = Config.Bind("6 - Eternal Fuel", "Standing Wood Torches", true, "Keep standing wood torches fueled.");
        EternalStandingGreenTorches = Config.Bind("6 - Eternal Fuel", "Standing Green Torches", true, "Keep green-burning standing torches fueled.");
        EternalStandingBlueTorches = Config.Bind("6 - Eternal Fuel", "Standing Blue Torches", true, "Keep blue-burning standing torches fueled.");
        EternalStandingBraziers = Config.Bind("6 - Eternal Fuel", "Standing Braziers", true, "Keep standing braziers fueled.");
        EternalHangingBraziers = Config.Bind("6 - Eternal Fuel", "Hanging Braziers", true, "Keep hanging braziers fueled.");
        EternalJackOTurnips = Config.Bind("6 - Eternal Fuel", "Jack-o-turnips", true, "Keep jack-o-turnips fueled.");
        EternalStoneOvens = Config.Bind("6 - Eternal Fuel", "Stone Ovens", true, "Keep stone ovens fueled.");
        EternalHotTubs = Config.Bind("6 - Eternal Fuel", "Hot Tubs", true, "Keep hot tubs fueled.");
        EternalSmelters = Config.Bind("6 - Eternal Fuel", "Smelters", false, "Keep smelters fueled.");
        EternalBlastFurnaces = Config.Bind("6 - Eternal Fuel", "Blast Furnaces", false, "Keep blast furnaces fueled.");
        EternalEitrRefineries = Config.Bind("6 - Eternal Fuel", "Eitr Refineries", false, "Keep eitr refineries fueled.");
        EternalCustomPrefabs = Config.Bind(
            "6 - Eternal Fuel",
            "Custom Prefabs",
            string.Empty,
            "Comma-separated exact prefab names whose Fireplace, CookingStation, or Smelter fuel must remain full.");

        ModEnabled.SettingChanged += OnPickupSettingChanged;
        PickupRadiusEnabled.SettingChanged += OnPickupSettingChanged;
        PickupRadius.SettingChanged += OnPickupSettingChanged;

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(DadsQoLPlugin).Assembly);
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private static void OnPickupSettingChanged(object sender, EventArgs eventArgs)
    {
        if (Player.m_localPlayer != null)
        {
            PlayerAwakePatch.Apply(Player.m_localPlayer);
        }
    }

    private void OnDestroy()
    {
        MassFarming.MassPlantingState.DestroyGhosts();
        _harmony?.UnpatchSelf();
    }

    internal static bool FeatureEnabled(ConfigEntry<bool> feature)
    {
        return ModEnabled.Value && feature.Value;
    }

    internal static bool MassFarmingActive => FeatureEnabled(MassFarmingEnabled);

    internal static bool MassFarmingShortcutHeld()
    {
        return ShortcutHeld(MassFarmingKeyboardShortcut.Value) ||
               ShortcutHeld(MassFarmingControllerShortcut.Value);
    }

    private static bool ShortcutHeld(KeyboardShortcut shortcut)
    {
        if (shortcut.MainKey == KeyCode.None || !Input.GetKey(shortcut.MainKey))
        {
            return false;
        }

        foreach (KeyCode modifier in shortcut.Modifiers)
        {
            if (!Input.GetKey(modifier))
            {
                return false;
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
internal static class PlayerAwakePatch
{
    private sealed class OriginalRange
    {
        internal float Value;
    }

    private static readonly ConditionalWeakTable<Player, OriginalRange> OriginalRanges = new();

    private static void Prefix(Player __instance)
    {
        OriginalRanges.GetValue(__instance, player => new OriginalRange { Value = player.m_autoPickupRange });
    }

    private static void Postfix(Player __instance)
    {
        Apply(__instance);
    }

    internal static void Apply(Player player)
    {
        OriginalRange original = OriginalRanges.GetValue(
            player,
            current => new OriginalRange { Value = current.m_autoPickupRange });

        player.m_autoPickupRange = DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.PickupRadiusEnabled)
            ? Math.Max(0f, DadsQoLPlugin.PickupRadius.Value)
            : original.Value;
    }
}

[HarmonyPatch(typeof(Player), "AutoPickup")]
internal static class AutoPickupCapacityPatch
{
    private static float _nextLargeRadiusScan;
    private static float _nextFullInventoryScan;

    private static bool Prefix(Player __instance, out float __state)
    {
        __state = __instance != null ? __instance.m_autoPickupRange : 0f;
        if (__instance == null || __instance != Player.m_localPlayer)
        {
            return true;
        }

        PlayerAwakePatch.Apply(__instance);
        __state = __instance.m_autoPickupRange;
        if (!DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.PickupRadiusEnabled)) return true;

        Inventory inventory = __instance.GetInventory();
        if (inventory == null) return true;

        bool hasEmptySlot = inventory.HaveEmptySlot();
        if (!hasEmptySlot)
        {
            bool hasPartialStack = false;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item?.m_shared == null || item.m_stack >= item.m_shared.m_maxStackSize) continue;
                hasPartialStack = true;
                break;
            }
            if (!hasPartialStack || Time.unscaledTime < _nextFullInventoryScan) return false;
            _nextFullInventoryScan = Time.unscaledTime + 0.5f;
        }

        if (__instance.m_autoPickupRange > 32f)
        {
            if (Time.unscaledTime < _nextLargeRadiusScan)
            {
                __instance.m_autoPickupRange = 12f;
            }
            else
            {
                _nextLargeRadiusScan = Time.unscaledTime + 0.5f;
                if (__instance.m_colliders == null || __instance.m_colliders.Length < 4096)
                    __instance.m_colliders = new Collider[4096];
            }
        }

        return true;
    }

    private static void Postfix(Player __instance, float __state)
    {
        if (__instance != null) __instance.m_autoPickupRange = __state;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
internal static class CarryWeightPatch
{
    private static void Prefix(Player __instance, out float __state)
    {
        __state = __instance.m_maxCarryWeight;
        if (DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.CarryWeightEnabled))
        {
            __instance.m_maxCarryWeight = Math.Max(0f, DadsQoLPlugin.CarryWeight.Value);
        }
    }

    private static void Postfix(Player __instance, float __state)
    {
        __instance.m_maxCarryWeight = __state;
    }
}

[HarmonyPatch]
internal static class EquipmentInWaterPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UpdateEquipment), new[] { typeof(float) });
        yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem), new[] { typeof(ItemDrop.ItemData), typeof(bool) });
    }

    private static void Prefix(Humanoid __instance, ref float ___m_swimTimer, out float __state)
    {
        __state = ___m_swimTimer;
        if (DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.EquipmentInWaterEnabled) &&
            __instance.IsPlayer() &&
            ___m_swimTimer < 0.5f)
        {
            ___m_swimTimer = 0.5f;
        }
    }

    private static void Postfix(ref float ___m_swimTimer, float __state)
    {
        ___m_swimTimer = __state;
    }
}
