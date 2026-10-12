# Siege Command Menu

A Mount & Blade II: Bannerlord (v1.4.8) mod that adds a **Siege Planning** screen to the besieger's left game menu. Queue, build, deploy and reserve siege engines without leaving the menu, with a clickable panel showing each engine's picture and stats.

## Features

- **Siege Planning** option in the besieger's siege menu: one entry per ranged slot, "Tower or ram slot" and reserve engine.
- Build only what your siege tier allows, with build times in in-game hours or days (including the siege camp).
- Engine panel beside the menu with pictures, damage, hit chance, build time and current health (e.g. `80% HP`). Cards are clickable and the panel pages four cards at a time with Scroll up / Scroll down buttons.
- Base-game slot rules: the middle melee slot takes the battering ram, the two outer ones take siege towers.
- Unfinished engines can be cancelled but not sent to the reserve; mark them to **move to reserve when finished**.
- Options (Mod Configuration Menu): panel on/off, clickable panel, send new engines to reserve by default, hide non-fire engines when fire versions exist, debug log, and a **Cheats** group (instant siege camp, instant engines).

## Requirements

- Bannerlord 1.4.8 (singleplayer campaign)
- [Mod Configuration Menu v5](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) (optional; without it the defaults are used and there is no options screen)

## Install

Copy the built `SiegeCommandMenu` folder into `Modules` in your Bannerlord install and enable **Siege Command Menu** in the launcher.

## Build

Needs the .NET SDK and a Bannerlord install (the game assemblies are referenced, not included).

```
dotnet build -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\"
```

The build also copies the module (`_Module` plus the DLL) into `Modules\SiegeCommandMenu` in the game folder. If MCM is installed somewhere else, pass `-p:McmDll=<path to MCMv5.dll>`.

## Layout

| Path | Contents |
| --- | --- |
| `src/Siege/` | All calls into the game's siege API (`SiegeActions`): queries, commands, melee slot rules, cheats |
| `src/Menu/` | The game menus: planning screen and slot screen |
| `src/Panel/` | The Gauntlet panel: view models, overlay, engine sprites |
| `src/Settings/` | MCM settings class and the safe `Options` reader |
| `_Module/` | `SubModule.xml` and the panel prefab |

Build, deploy, reserve and cancel go through the game's own `SiegeEvent.DoSiegeAction`. Changing the engine containers directly crashed the game at the next daily tick, so don't.

## Troubleshooting

Errors are written to `error_log.txt` in `Modules\SiegeCommandMenu`. Turn on **Write debug log** in the mod options to also get `debug_log.txt` with every siege action.

## License

MIT, see [LICENSE](LICENSE).
