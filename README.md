# DadsQoL

DadsQoL combines configurable Valheim quality-of-life features in one Valheim 1.0.12 plugin:

- Configurable base carry weight, default `5000` (`300` is vanilla).
- Configurable automatic pickup radius, default `6` meters (`2` is vanilla).
- Equipment, tools, weapons, bows, and shields remain usable while swimming.
- Hold `Left Shift` or controller `Left Bumper` while interacting to harvest matching nearby pickables or extract nearby beehives.
- Hold the same shortcut while planting to place a configurable grid, default `5x5`, with placement previews.

Every feature has its own switch. A master switch controls the complete plugin. Mass farming includes configurable shortcuts, harvest radius, planting-grid dimensions, grid anchoring, stamina use, and durability use.

## Installation

Install BepInExPack for Valheim, then place `DadsQoL.dll` in `BepInEx/plugins/DadsQoL/`.

The configuration file is created at `BepInEx/config/com.dadisbored.dadsqol.cfg`.

## Compatibility

- Built against Valheim `1.0.12`.
- Requires BepInEx only.
- Client-side plugin.
- Do not run another mod that changes the same carry-weight, pickup-radius, or equipment-in-water behavior.

## Credits

The feature set follows the established scope of Kojeak's WeightChangeMod, mtnewton's BiggerPickupRadius, and LVH-IT's Use Equipment in Water. The mass-farming implementation is adapted from Xeio's MIT-licensed MassFarming source, distributed on Thunderstore by MainStreetGaming. DadsQoL contains no third-party DLLs or artwork. The retained license is in `THIRD_PARTY_LICENSES/MassFarming-LICENSE.txt`.
