// Mass-farming behavior adapted for DadsQoL from Xeio/MassFarming under the MIT License.
// Copyright (c) 2021 Joshua Shaffer. See THIRD_PARTY_LICENSES/MassFarming-LICENSE.txt.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Splatform;
using UnityEngine;
using UnityEngine.Rendering;

namespace DadsQoL.MassFarming;

internal static class MassPlantingState
{
    private static readonly FieldInfo NoPlacementCostField = AccessTools.Field(typeof(Player), "m_noPlacementCost");
    private static readonly FieldInfo PlacementGhostField = AccessTools.Field(typeof(Player), "m_placementGhost");
    private static readonly FieldInfo BuildPiecesField = AccessTools.Field(typeof(Player), "m_buildPieces");
    private static readonly MethodInfo GetRightItemMethod = AccessTools.Method(typeof(Humanoid), "GetRightItem");

    internal static Vector3 PlacedPosition;
    internal static Quaternion PlacedRotation;
    internal static Piece PlacedPiece = null!;
    internal static bool PlaceSuccessful;
    internal static int? SavedRotation;
    internal static GameObject[] PlacementGhosts = new GameObject[1];
    internal static Piece FakeResourcePiece = null!;
    internal static readonly int PlantSpaceMask = LayerMask.GetMask(
        "Default",
        "static_solid",
        "Default_small",
        "piece",
        "piece_nonsolid");

    internal static bool Active => DadsQoLPlugin.MassFarmingActive && DadsQoLPlugin.MassFarmingShortcutHeld();

    internal static bool NoPlacementCost(Player player) => (bool)NoPlacementCostField.GetValue(player);

    internal static GameObject? PlacementGhost(Player player) => PlacementGhostField.GetValue(player) as GameObject;

    internal static PieceTable? BuildPieces(Player player) => BuildPiecesField.GetValue(player) as PieceTable;

    internal static ItemDrop.ItemData? RightItem(Player player) =>
        GetRightItemMethod.Invoke(player, Array.Empty<object>()) as ItemDrop.ItemData;

    internal static IEnumerable<Vector3> BuildGridPositions(Vector3 origin, Plant plant, Quaternion rotation)
    {
        int width = Math.Max(1, DadsQoLPlugin.PlantGridWidth.Value);
        int length = Math.Max(1, DadsQoLPlugin.PlantGridLength.Value);
        float spacing = plant.m_growRadius * 2f;
        Vector3 across = rotation * Vector3.left * spacing;
        Vector3 forward = rotation * Vector3.forward * spacing;
        Vector3 gridOrigin = origin;

        if (DadsQoLPlugin.CenterPlantingGridLength.Value)
        {
            gridOrigin -= forward * (length / 2);
        }
        if (DadsQoLPlugin.CenterPlantingGridWidth.Value)
        {
            gridOrigin -= across * (width / 2);
        }

        for (int x = 0; x < length; x++)
        {
            Vector3 position = gridOrigin;
            for (int z = 0; z < width; z++)
            {
                position.y = ZoneSystem.instance.GetGroundHeight(position);
                yield return position;
                position += across;
            }
            gridOrigin += forward;
        }
    }

    internal static bool HasGrowSpace(Vector3 position, GameObject prefab)
    {
        Plant plant = prefab.GetComponent<Plant>();
        return plant == null || Physics.OverlapSphere(position, plant.m_growRadius, PlantSpaceMask).Length == 0;
    }

    internal static bool EnsureGhostsBuilt(Player player)
    {
        int requiredSize = Math.Max(1, DadsQoLPlugin.PlantGridWidth.Value) *
                           Math.Max(1, DadsQoLPlugin.PlantGridLength.Value);
        bool needsRebuild = PlacementGhosts.Length != requiredSize || PlacementGhosts[0] == null;
        if (needsRebuild)
        {
            DestroyGhosts();
            PlacementGhosts = new GameObject[requiredSize];

            PieceTable? pieceTable = BuildPieces(player);
            GameObject? prefab = pieceTable != null ? pieceTable.GetSelectedPrefab() : null;
            if (prefab == null || prefab.GetComponent<Piece>().m_repairPiece)
            {
                return false;
            }

            for (int index = 0; index < PlacementGhosts.Length; index++)
            {
                PlacementGhosts[index] = CreateGhost(prefab);
            }
        }

        if (FakeResourcePiece == null)
        {
            FakeResourcePiece = PlacementGhosts[0].GetComponent<Piece>();
            FakeResourcePiece.m_dlc = string.Empty;
            FakeResourcePiece.m_resources = new[] { new Piece.Requirement() };
        }

        return true;
    }

    internal static void DestroyGhosts()
    {
        foreach (GameObject ghost in PlacementGhosts)
        {
            if (ghost != null)
            {
                UnityEngine.Object.Destroy(ghost);
            }
        }
        FakeResourcePiece = null!;
    }

    internal static void SetGhostsActive(bool active)
    {
        foreach (GameObject ghost in PlacementGhosts)
        {
            if (ghost != null)
            {
                ghost.SetActive(active);
            }
        }
    }

    private static GameObject CreateGhost(GameObject prefab)
    {
        ZNetView.m_forceDisableInit = true;
        GameObject ghost;
        try
        {
            ghost = UnityEngine.Object.Instantiate(prefab);
        }
        finally
        {
            ZNetView.m_forceDisableInit = false;
        }

        ghost.name = prefab.name;
        foreach (Joint joint in ghost.GetComponentsInChildren<Joint>())
        {
            UnityEngine.Object.Destroy(joint);
        }
        foreach (Rigidbody body in ghost.GetComponentsInChildren<Rigidbody>())
        {
            UnityEngine.Object.Destroy(body);
        }

        int layer = LayerMask.NameToLayer("ghost");
        foreach (Transform child in ghost.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = layer;
        }
        foreach (TerrainModifier modifier in ghost.GetComponentsInChildren<TerrainModifier>())
        {
            UnityEngine.Object.Destroy(modifier);
        }
        foreach (GuidePoint guidePoint in ghost.GetComponentsInChildren<GuidePoint>())
        {
            UnityEngine.Object.Destroy(guidePoint);
        }
        foreach (Light light in ghost.GetComponentsInChildren<Light>())
        {
            UnityEngine.Object.Destroy(light);
        }

        Transform ghostOnly = ghost.transform.Find("_GhostOnly");
        if (ghostOnly != null)
        {
            ghostOnly.gameObject.SetActive(true);
        }

        foreach (MeshRenderer renderer in ghost.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.sharedMaterial == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = new(materials[index]);
                material.SetFloat("_RippleDistance", 0f);
                material.SetFloat("_ValueNoise", 0f);
                materials[index] = material;
            }
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        return ghost;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
internal static class TryPlacePiecePatch
{
    private static void Prefix(int ___m_placeRotation)
    {
        if (MassPlantingState.Active && MassPlantingState.SavedRotation == null)
        {
            MassPlantingState.SavedRotation = ___m_placeRotation;
        }
    }

    private static void Postfix(Player __instance, bool __result, Piece piece, ref int ___m_placeRotation)
    {
        MassPlantingState.PlaceSuccessful = __result;
        GameObject? placementGhost = MassPlantingState.PlacementGhost(__instance);
        if (__result && placementGhost != null)
        {
            MassPlantingState.PlacedPosition = placementGhost.transform.position;
            MassPlantingState.PlacedRotation = placementGhost.transform.rotation;
            MassPlantingState.PlacedPiece = piece;
        }

        if (MassPlantingState.Active && MassPlantingState.SavedRotation.HasValue)
        {
            ___m_placeRotation = MassPlantingState.SavedRotation.Value;
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
internal static class UpdatePlacementPatch
{
    private static void Prefix(ref int ___m_placeRotation)
    {
        MassPlantingState.PlaceSuccessful = false;
        if (MassPlantingState.Active && MassPlantingState.SavedRotation.HasValue)
        {
            ___m_placeRotation = MassPlantingState.SavedRotation.Value;
        }
    }

    private static void Postfix(Player __instance, int ___m_placeRotation)
    {
        if (MassPlantingState.Active)
        {
            MassPlantingState.SavedRotation = ___m_placeRotation;
        }

        if (!MassPlantingState.PlaceSuccessful || !MassPlantingState.Active)
        {
            return;
        }

        Plant plant = MassPlantingState.PlacedPiece.gameObject.GetComponent<Plant>();
        Heightmap heightmap = Heightmap.FindHeightmap(MassPlantingState.PlacedPosition);
        if (plant == null || heightmap == null)
        {
            return;
        }

        PieceTable? pieceTable = MassPlantingState.BuildPieces(__instance);
        foreach (Vector3 position in MassPlantingState.BuildGridPositions(
                     MassPlantingState.PlacedPosition,
                     plant,
                     MassPlantingState.PlacedRotation))
        {
            if (MassPlantingState.PlacedPosition == position ||
                (MassPlantingState.PlacedPiece.m_cultivatedGroundOnly && !heightmap.IsCultivated(position)) ||
                !MassPlantingState.HasGrowSpace(position, MassPlantingState.PlacedPiece.gameObject))
            {
                continue;
            }

            ItemDrop.ItemData? tool = MassPlantingState.RightItem(__instance);
            if (tool == null)
            {
                return;
            }

            if (!DadsQoLPlugin.IgnorePlantingStamina.Value &&
                !__instance.HaveStamina(tool.m_shared.m_attack.m_attackStamina))
            {
                Hud.instance.StaminaBarUppgradeFlash();
                return;
            }

            if (!MassPlantingState.NoPlacementCost(__instance) &&
                !__instance.HaveRequirements(MassPlantingState.PlacedPiece, Player.RequirementMode.CanBuild))
            {
                return;
            }

            GameObject placedObject = UnityEngine.Object.Instantiate(
                MassPlantingState.PlacedPiece.gameObject,
                position,
                MassPlantingState.PlacedRotation);
            Piece placedPiece = placedObject.GetComponent<Piece>();
            if (placedPiece != null)
            {
                placedPiece.SetCreator(
                    __instance.GetPlayerID(),
                    PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            }

            MassPlantingState.PlacedPiece.m_placeEffect.Create(
                position,
                MassPlantingState.PlacedRotation,
                placedObject.transform);
            Game.instance.IncrementPlayerStat(PlayerStatType.Builds);
            __instance.ConsumeResources(MassPlantingState.PlacedPiece.m_resources, 0, -1);

            if (!DadsQoLPlugin.IgnorePlantingStamina.Value)
            {
                __instance.UseStamina(tool.m_shared.m_attack.m_attackStamina);
            }
            if (pieceTable != null)
            {
                __instance.RaiseSkill(pieceTable.m_skill);
            }
            if (!DadsQoLPlugin.IgnorePlantingDurability.Value && tool.m_shared.m_useDurability)
            {
                tool.m_durability -= tool.m_shared.m_useDurabilityDrain;
                if (tool.m_durability <= 0f)
                {
                    return;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
internal static class SetupPlacementGhostPatch
{
    private static void Prefix(int ___m_placeRotation)
    {
        if (MassPlantingState.Active && MassPlantingState.SavedRotation == null)
        {
            MassPlantingState.SavedRotation = ___m_placeRotation;
        }
    }

    private static void Postfix(ref int ___m_placeRotation)
    {
        if (MassPlantingState.Active && MassPlantingState.SavedRotation.HasValue)
        {
            ___m_placeRotation = MassPlantingState.SavedRotation.Value;
        }
        MassPlantingState.DestroyGhosts();
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
internal static class UpdatePlacementGhostPatch
{
    private static void Postfix(Player __instance)
    {
        GameObject? mainGhost = MassPlantingState.PlacementGhost(__instance);
        if (!DadsQoLPlugin.MassFarmingActive ||
            !DadsQoLPlugin.MassFarmingShortcutHeld() ||
            mainGhost == null ||
            !mainGhost.activeSelf)
        {
            MassPlantingState.SetGhostsActive(false);
            return;
        }

        Plant plant = mainGhost.GetComponent<Plant>();
        if (plant == null || !MassPlantingState.EnsureGhostsBuilt(__instance))
        {
            MassPlantingState.SetGhostsActive(false);
            return;
        }

        Piece mainPiece = mainGhost.GetComponent<Piece>();
        Piece.Requirement requirement = mainPiece.m_resources.FirstOrDefault(
            resource => resource.m_resItem != null && resource.m_amount > 0);
        if (requirement == null)
        {
            MassPlantingState.SetGhostsActive(false);
            return;
        }

        MassPlantingState.FakeResourcePiece.m_resources[0].m_resItem = requirement.m_resItem;
        MassPlantingState.FakeResourcePiece.m_resources[0].m_amount = 0;

        ItemDrop.ItemData? tool = MassPlantingState.RightItem(__instance);
        Heightmap heightmap = Heightmap.FindHeightmap(mainGhost.transform.position);
        if (tool == null || heightmap == null)
        {
            MassPlantingState.SetGhostsActive(false);
            return;
        }

        float stamina = __instance.GetStamina();
        List<Vector3> positions = MassPlantingState.BuildGridPositions(
            mainGhost.transform.position,
            plant,
            mainGhost.transform.rotation).ToList();

        for (int index = 0; index < MassPlantingState.PlacementGhosts.Length; index++)
        {
            GameObject ghost = MassPlantingState.PlacementGhosts[index];
            Vector3 position = positions[index];
            if (mainGhost.transform.position == position)
            {
                ghost.SetActive(false);
                continue;
            }

            MassPlantingState.FakeResourcePiece.m_resources[0].m_amount += requirement.m_amount;
            ghost.transform.position = position;
            ghost.transform.rotation = mainGhost.transform.rotation;
            ghost.SetActive(true);

            bool invalid = (mainPiece.m_cultivatedGroundOnly && !heightmap.IsCultivated(position)) ||
                           !MassPlantingState.HasGrowSpace(position, mainGhost) ||
                           (!DadsQoLPlugin.IgnorePlantingStamina.Value &&
                            stamina < tool.m_shared.m_attack.m_attackStamina) ||
                           (!MassPlantingState.NoPlacementCost(__instance) &&
                            !__instance.HaveRequirements(
                                MassPlantingState.FakeResourcePiece,
                                Player.RequirementMode.CanBuild));

            stamina -= tool.m_shared.m_attack.m_attackStamina;
            ghost.GetComponent<Piece>().SetInvalidPlacementHeightlight(invalid);
        }
    }
}
