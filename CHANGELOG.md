# Changelog

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
