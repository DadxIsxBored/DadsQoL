// Mass-farming behavior adapted for DadsQoL from Xeio/MassFarming under the MIT License.
// Copyright (c) 2021 Joshua Shaffer. See THIRD_PARTY_LICENSES/MassFarming-LICENSE.txt.

using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DadsQoL.MassFarming;

[HarmonyPatch(typeof(Player), "Interact")]
internal static class MassHarvest
{
    private static readonly FieldInfo InteractMaskField = AccessTools.Field(typeof(Player), "m_interactMask");
    private static readonly MethodInfo ExtractMethod = AccessTools.Method(typeof(Beehive), "Extract", System.Type.EmptyTypes);
    private static bool _processingMassInteraction;
    private static Collider[] _colliders = new Collider[64];

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
        int colliderCount;
        do
        {
            colliderCount = Physics.OverlapSphereNonAlloc(go.transform.position, radius, _colliders, interactMask);
            if (colliderCount < _colliders.Length) break;
            System.Array.Resize(ref _colliders, _colliders.Length * 2);
        } while (true);

        _processingMassInteraction = true;
        try
        {
            if (interactable is Pickable targetedPickable)
            {
                string targetPrefab = targetedPickable.m_itemPrefab != null
                    ? targetedPickable.m_itemPrefab.name
                    : string.Empty;

                for (int index = 0; index < colliderCount; index++)
                {
                    Collider collider = _colliders[index];
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
                for (int index = 0; index < colliderCount; index++)
                {
                    Collider collider = _colliders[index];
                    Beehive? nearbyBeehive = collider != null
                        ? collider.gameObject.GetComponentInParent<Beehive>()
                        : null;
                    if (nearbyBeehive != null &&
                        nearbyBeehive != targetedBeehive &&
                        PrivateArea.CheckAccess(nearbyBeehive.transform.position))
                    {
                        ExtractMethod.Invoke(nearbyBeehive, null);
                    }
                }
            }
        }
        finally
        {
            System.Array.Clear(_colliders, 0, colliderCount);
            _processingMassInteraction = false;
        }
    }
}
