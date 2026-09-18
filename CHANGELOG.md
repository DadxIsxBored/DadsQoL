# Changelog

## 1.3.10

- Kept nearby automatic pickup scans running when all slots are occupied but existing stacks have room. Matching drops can finish moving into pickup range instead of repeatedly overshooting it on throttled scans.
- Retained the broad-radius scan limit, per-drop capacity checks, and the stop when no empty slots or partial stacks remain.

## 1.3.9

- Moved distant drops at the normal pickup speed by passing elapsed broad-scan time to Valheim's pickup routine.

## 1.3.8

- Accessed Valheim's private pickup collider buffer through Harmony instead of a compile-time publicized field, preventing runtime `FieldAccessException` on broad scans.

## 1.3.7

- Allowed automatic pickup into an existing partial stack when ordinary inventory slots are full, checking at most twice per second in that state.
- Expanded Valheim's fixed 100-collider pickup buffer for configured radii above 32 meters and limited broad scans to twice per second while retaining nearby scans between them.

## 1.3.6

- Applied the configured pickup radius to the current player immediately before each automatic-pickup scan, so in-game setting changes do not require a restart.

## 1.3.5

- Stopped the automatic-pickup scan whenever no ordinary inventory slot is empty, even when an existing stack is below its maximum size.

## 1.3.4

- Stopped the automatic-pickup scan when the player inventory has no empty slot and no partial stack with remaining capacity.
- Automatic pickup resumes as soon as a slot or stack has capacity again.

## 1.3.3

- Removed FarmersHoe completely, including its Hoe piece-table changes, terrain-operation prefab, Harmony patches, configuration entry, and network registration.
- Restored ownership of the vanilla Hoe menu and terrain actions to Valheim and other installed mods.

## 1.3.2

- Changed the FarmersHoe `PieceTable.UpdateAvailable` hook to Harmony's positional argument binding so it matches Valheim 1.0.12's `knownRecipies` parameter.
- Restored complete DadsQoL Harmony initialization when FarmersHoe is enabled.

## 1.3.1

- Registered FarmersHoe after `ObjectDB.CopyOtherDB`, when Valheim's final Hoe piece table is available.
- Located Valheim 1.0.12's `mud_road_v2` Level Ground prefab and its child `TerrainOp` component.
- Made Flatten Terrain immediately available in the Hoe menu without permanently altering the player's known recipes.

## 1.3.0

- Added `FarmersHoe`, a pure Flatten Terrain action integrated into the vanilla hoe.
- Retained the vanilla hoe's placement, stamina, durability, and access rules.
- Added no other MyDirtyHoe terrain, paint, radius, reset, command, tool, or status-effect features.
- Kept DadsQoL dependent only on BepInEx.

## 1.2.1

- Rebuilt against BepInEx `5.4.23.5` from BepInExPack Valheim `5.4.2350`.

## 1.2.0

- Added configurable eternal fuel for supported fireplaces, torches, braziers, cooking stations, hot tubs, and processing stations.
- Added exact prefab-name support for custom fueled pieces.
- Retained standard defaults: smelters, blast furnaces, and eitr refineries are disabled until configured.

## 1.1.0

- Added mass harvesting for matching nearby pickables.
- Added mass extraction for nearby accessible beehives.
- Added configurable grid planting with placement previews.
- Added keyboard and controller shortcuts.
- Added configurable harvest radius, grid dimensions, anchoring, stamina use, and durability use.

## 1.0.0

- Added configurable base carry weight.
- Added configurable automatic pickup radius.
- Added equipment use while swimming.
- Added independent feature switches and a master switch.
- Added Valheim 1.0.12 build and Thunderstore package support.
