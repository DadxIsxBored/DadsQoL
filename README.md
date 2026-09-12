# DadsQoL

DadsQoL combines three configurable Valheim quality-of-life features in one Valheim 1.0.12 plugin:

- Configurable base carry weight, default `5000` (`300` is vanilla).
- Configurable automatic pickup radius, default `6` meters (`2` is vanilla).
- Equipment, tools, weapons, bows, and shields remain usable while swimming.

Every feature has its own switch. A master switch controls the complete plugin. Changes to carry weight take effect on the next weight calculation. Pickup-radius changes apply to the active player when the setting changes.

## Installation

Install BepInExPack for Valheim, then place `DadsQoL.dll` in `BepInEx/plugins/DadsQoL/`.

The configuration file is created at `BepInEx/config/com.dadisbored.dadsqol.cfg`.

## Compatibility

- Built against Valheim `1.0.12`.
- Requires BepInEx only.
- Client-side plugin.
- Do not run another mod that changes the same carry-weight, pickup-radius, or equipment-in-water behavior.

## Credits

The feature set follows the established scope of Kojeak's WeightChangeMod, mtnewton's BiggerPickupRadius, and LVH-IT's Use Equipment in Water. DadsQoL is a separate implementation for Valheim 1.0.12 and contains no third-party DLLs or artwork.
