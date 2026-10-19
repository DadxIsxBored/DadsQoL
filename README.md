# DadsQoL

DadsQoL combines configurable Valheim quality-of-life features in one Valheim 1.0.16 plugin:

- Configurable base carry weight, default `5000` (`300` is vanilla).
- Configurable automatic pickup radius, default `6` meters (`2` is vanilla).
- Fish attraction has its own `Attract Fish` switch under `3 - Pickup Radius`. It defaults to off, so automatic pickup does not pull fish behind the player; manual fish pickup remains available.
- Equipment, tools, weapons, bows, and shields remain usable while swimming.
- Hold `Left Shift` or controller `Left Bumper` while interacting to harvest matching nearby pickables or extract nearby beehives.
- Hold the same shortcut while planting to place a configurable grid, default `5x5`, with placement previews.
- Configurable eternal fuel for campfires, bonfires, hearths, sconces, standing torches, braziers, jack-o-turnips, stone ovens, hot tubs, smelters, blast furnaces, Frost Foundries, eitr refineries, and custom prefab names.
- Shared crafting and building recipes: a discovery by one player becomes available to everyone in the world.
- Automatic map sharing: explored areas are shared with current players and later joiners without a cartography table.

Every feature has its own switch. A master switch controls the complete plugin. Mass farming includes configurable shortcuts, harvest radius, planting-grid dimensions, grid anchoring, stamina use, and durability use. Eternal fuel has separate controls for every supported piece group; smelters, blast furnaces, and eitr refineries default to disabled.

The two multiplayer switches are under `7 - Multiplayer` and default to enabled. Install DadsQoL on the server and every player for sharing. World-specific shared discoveries are stored in `BepInEx/config/DadsQoL/` on the server.

## Installation

Install BepInExPack for Valheim, then place `DadsQoL.dll` in `BepInEx/plugins/DadsQoL/`.

The configuration file is created at `BepInEx/config/com.dadisbored.dadsqol.cfg`.

## Compatibility

- Built against Valheim `1.0.12`.
- Requires BepInEx only.
- Works in local games and can run on a dedicated server.
- Do not run another mod that changes the same carry-weight, pickup-radius, or equipment-in-water behavior.

## Credits

The feature set follows the established scope of Kojeak's WeightChangeMod, mtnewton's BiggerPickupRadius, LVH-IT's Use Equipment in Water, and Marf's FuelEternal. The mass-farming implementation is adapted from Xeio's MIT-licensed MassFarming source, distributed on Thunderstore by MainStreetGaming. Eternal fuel is newly written against Valheim 1.0.12. The retained MassFarming license is in `THIRD_PARTY_LICENSES/MassFarming-LICENSE.txt`.
