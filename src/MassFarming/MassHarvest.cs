// Mass-farming behavior adapted for DadsQoL from Xeio/MassFarming under the MIT License.
// Copyright (c) 2021 Joshua Shaffer. See THIRD_PARTY_LICENSES/MassFarming-LICENSE.txt.

using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DadsQoL.MassFarming;

[HarmonyPatch(typeof(Player), nameof(Player.Interact))]
internal static class MassHarvest
{
    private static readonly FieldInfo InteractMaskField = AccessTools.Field(typeof(Player), "m_interactMask");
    private static bool _processingMassInteraction;

    private static void Prefix(Player __instance, GameObject go, bool hold, bool alt)
    {
        if (!DadsQoLPlugin.MassFarmingActive ||
            _processingMassInteraction ||
            hold ||
            __instance.InAttack() ||
            __instance.InDodge() ||
            !DadsQoLPlugin.MassFarmingShortcutHeld())
        {
            return;
        }

        Interactable interactable = go.GetComponentInParent<Interactable>();
        float radius = Mathf.Max(0f, DadsQoLPlugin.MassHarvestRadius.Value);
        int interactMask = (int)InteractMaskField.GetValue(__instance);
        Collider[] colliders = Physics.OverlapSphere(go.transform.position, radius, interactMask);

        _processingMassInteraction = true;
        try
        {
            if (interactable is Pickable targetedPickable)
            {
                string targetPrefab = targetedPickable.m_itemPrefab != null
                    ? targetedPickable.m_itemPrefab.name
                    : string.Empty;

                foreach (Collider collider in colliders)
                {
                    Pickable? nearbyPickable = collider != null
                        ? collider.gameObject.GetComponentInParent<Pickable>()
                        : null;
                    if (nearbyPickable != null &&
                        nearbyPickable != targetedPickable &&
                        nearbyPickable.m_itemPrefab != null &&
                        nearbyPickable.m_itemPrefab.name == targetPrefab)
                    {
                        nearbyPickable.Interact(__instance, false, alt);
                    }
                }
            }
            else if (interactable is Beehive targetedBeehive)
            {
                foreach (Collider collider in colliders)
                {
                    Beehive? nearbyBeehive = collider != null
                        ? collider.gameObject.GetComponentInParent<Beehive>()
                        : null;
                    if (nearbyBeehive != null &&
                        nearbyBeehive != targetedBeehive &&
                        PrivateArea.CheckAccess(nearbyBeehive.transform.position))
                    {
                        nearbyBeehive.Extract();
                    }
                }
            }
        }
        finally
        {
            _processingMassInteraction = false;
        }
    }
}
