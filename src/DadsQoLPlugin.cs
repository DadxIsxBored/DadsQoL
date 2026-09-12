using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DadsQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class DadsQoLPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.dadisbored.dadsqol";
    public const string PluginName = "DadsQoL";
    public const string PluginVersion = "1.1.0";

    internal static ConfigEntry<bool> ModEnabled = null!;
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

    private Harmony? _harmony;

    private void Awake()
    {
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
