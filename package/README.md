# DadsQoL

One configurable plugin for Valheim 1.0.12:

- Base carry weight: `5000` by default.
- Automatic pickup radius: `6` meters by default.
- Hand equipment remains usable while swimming.
- Hold `Left Shift` or controller `Left Bumper` while interacting to harvest matching nearby pickables and beehives.
- Hold the same shortcut while planting to place a configurable grid, default `5x5`, with placement previews.
- Selected fireplaces, torches, braziers, stone ovens, hot tubs, and configured processing stations remain fully fueled.
- `FarmersHoe` adds a pure `Flatten Terrain` action directly to the vanilla hoe while retaining normal hoe costs and restrictions.

Each feature and the complete plugin can be switched on or off in `BepInEx/config/com.dadisbored.dadsqol.cfg`. Mass-farming shortcuts, radius, grid dimensions, anchoring, stamina use, and durability use are configurable. Eternal fuel includes individual piece-group controls and custom prefab names; smelters, blast furnaces, and eitr refineries default to disabled.

Requires BepInEx only. It works in local games and can run on a dedicated server. Multiplayer FarmersHoe use requires DadsQoL on the server and every client.
