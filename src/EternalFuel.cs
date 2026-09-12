using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;

namespace DadsQoL;

internal static class EternalFuel
{
    private static string _customPrefabSource = string.Empty;
    private static HashSet<string> _customPrefabs = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, Func<bool>> BuiltInPrefabs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fire_pit"] = () => DadsQoLPlugin.EternalCampfires.Value,
        ["bonfire"] = () => DadsQoLPlugin.EternalBonfires.Value,
        ["hearth"] = () => DadsQoLPlugin.EternalHearths.Value,
        ["piece_walltorch"] = () => DadsQoLPlugin.EternalSconces.Value,
        ["piece_groundtorch"] = () => DadsQoLPlugin.EternalStandingIronTorches.Value,
        ["piece_groundtorch_wood"] = () => DadsQoLPlugin.EternalStandingWoodTorches.Value,
        ["piece_groundtorch_green"] = () => DadsQoLPlugin.EternalStandingGreenTorches.Value,
        ["piece_groundtorch_blue"] = () => DadsQoLPlugin.EternalStandingBlueTorches.Value,
        ["piece_brazierfloor01"] = () => DadsQoLPlugin.EternalStandingBraziers.Value,
        ["piece_brazierceiling01"] = () => DadsQoLPlugin.EternalHangingBraziers.Value,
        ["piece_jackoturnip"] = () => DadsQoLPlugin.EternalJackOTurnips.Value,
        ["piece_oven"] = () => DadsQoLPlugin.EternalStoneOvens.Value,
        ["piece_bathtub"] = () => DadsQoLPlugin.EternalHotTubs.Value,
        ["smelter"] = () => DadsQoLPlugin.EternalSmelters.Value,
        ["blastfurnace"] = () => DadsQoLPlugin.EternalBlastFurnaces.Value,
        ["eitrrefinery"] = () => DadsQoLPlugin.EternalEitrRefineries.Value
    };

    internal static bool EnabledFor(UnityEngine.Component component)
    {
        if (component == null || !DadsQoLPlugin.FeatureEnabled(DadsQoLPlugin.EternalFuelEnabled))
        {
            return false;
        }

        string prefabName = component.gameObject.name;
        const string cloneSuffix = "(Clone)";
        if (prefabName.EndsWith(cloneSuffix, StringComparison.OrdinalIgnoreCase))
        {
            prefabName = prefabName.Substring(0, prefabName.Length - cloneSuffix.Length);
        }
        if (BuiltInPrefabs.TryGetValue(prefabName, out Func<bool>? enabled))
        {
            return enabled();
        }

        return CustomPrefabs().Contains(prefabName);
    }

    internal static void Fill(ZNetView view, float maximumFuel)
    {
        if (view == null || !view.IsValid() || !view.IsOwner() || view.GetZDO() == null)
        {
            return;
        }

        view.GetZDO().Set(ZDOVars.s_fuel, maximumFuel);
    }

    private static HashSet<string> CustomPrefabs()
    {
        string configured = DadsQoLPlugin.EternalCustomPrefabs.Value ?? string.Empty;
        if (string.Equals(configured, _customPrefabSource, StringComparison.Ordinal))
        {
            return _customPrefabs;
        }

        _customPrefabSource = configured;
        _customPrefabs = new HashSet<string>(
            configured
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .Where(value => value.Length > 0),
            StringComparer.OrdinalIgnoreCase);
        return _customPrefabs;
    }
}

[HarmonyPatch(typeof(Fireplace), "UpdateFireplace")]
internal static class EternalFireplacePatch
{
    private static void Prefix(Fireplace __instance, ZNetView ___m_nview, out bool __state)
    {
        __state = __instance.m_infiniteFuel;
        if (!EternalFuel.EnabledFor(__instance))
        {
            return;
        }

        __instance.m_infiniteFuel = true;
        EternalFuel.Fill(___m_nview, __instance.m_maxFuel);
    }

    private static void Postfix(Fireplace __instance, bool __state)
    {
        __instance.m_infiniteFuel = __state;
    }
}

[HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
internal static class EternalCookingStationUpdatePatch
{
    private static void Prefix(CookingStation __instance, ZNetView ___m_nview)
    {
        if (EternalFuel.EnabledFor(__instance))
        {
            EternalFuel.Fill(___m_nview, __instance.m_maxFuel);
        }
    }
}

[HarmonyPatch(typeof(CookingStation), "SetFuel")]
internal static class EternalCookingStationSetFuelPatch
{
    private static void Prefix(CookingStation __instance, ZNetView ___m_nview, ref float fuel)
    {
        if (EternalFuel.EnabledFor(__instance) && ___m_nview != null && ___m_nview.IsValid() && ___m_nview.IsOwner())
        {
            fuel = __instance.m_maxFuel;
        }
    }
}

[HarmonyPatch(typeof(Smelter), "UpdateSmelter")]
internal static class EternalSmelterUpdatePatch
{
    private static void Prefix(Smelter __instance, ZNetView ___m_nview)
    {
        if (EternalFuel.EnabledFor(__instance))
        {
            EternalFuel.Fill(___m_nview, __instance.m_maxFuel);
        }
    }
}

[HarmonyPatch(typeof(Smelter), "SetFuel")]
internal static class EternalSmelterSetFuelPatch
{
    private static void Prefix(Smelter __instance, ZNetView ___m_nview, ref float fuel)
    {
        if (EternalFuel.EnabledFor(__instance) && ___m_nview != null && ___m_nview.IsValid() && ___m_nview.IsOwner())
        {
            fuel = __instance.m_maxFuel;
        }
    }
}
