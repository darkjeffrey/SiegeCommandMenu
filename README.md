# Siege Command Menu

A Mount & Blade II: Bannerlord (v1.5.7) mod that adds a **Siege Planning** screen to the besieger's left game menu. Queue, build, deploy and reserve siege engines without leaving the menu, with a clickable panel showing each engine's picture and stats.

## Features

- **Siege Planning** option in the besieger's siege menu: one entry per ranged slot, melee slot and reserve engine, plus **Deploy all from reserve**. Melee slots follow the base game and are named after what they take: the ram slot first, then the siege tower slots.
- Build only what your siege tier allows, with build times in in-game hours or days (including the siege camp).
- Engine panel beside the menu with pictures, damage, hit chance, build time and current health (e.g. `80% HP`). Cards are clickable and the panel pages four cards at a time with Scroll up / Scroll down buttons.
- Unfinished engines can be cancelled but not sent to the reserve; mark them to **move to reserve when finished**.
- All text can be translated (`_Module/ModuleData/Languages`).
- Options (Mod Configuration Menu): panel on/off, clickable panel, send new engines to reserve by default, hide non-fire engines when fire versions exist, debug log, and a **Cheats** group (instant siege camp, instant engines).

## Requirements

- Bannerlord 1.5.7 (singleplayer campaign)
- [Mod Configuration Menu v5](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) (optional; without it the defaults are used and there is no options screen)

## Install

Copy the built `SiegeCommandMenu` folder into `Modules` in your Bannerlord install and enable **Siege Command Menu** in the launcher.

## Build

Needs the .NET SDK and a Bannerlord install (the game assemblies are referenced, not included).

```
dotnet build -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\"
```

The build also copies the module (`_Module` plus the DLL) into `Modules\SiegeCommandMenu` in the game folder. If MCM is installed somewhere else, pass `-p:McmDll=<path to MCMv5.dll>`.

## Releasing

1. Add a `## <version>` section to `CHANGELOG.md`, run `bltools.py bump SiegeCommandMenu <version>`, rebuild and push.
2. Run `bltools.py package SiegeCommandMenu` to make `release/SiegeCommandMenu-v<version>.zip`.
3. Publish a GitHub Release tagged `v<version>` with that zip attached. The "Publish to Nexus Mods" workflow then uploads it to Nexus as the new version of the main file, with the changelog section as its changelog. It stops if the tag, `_Module/SubModule.xml` and the zip don't have the same version.

The workflow needs the repository secret `NEXUS_API_KEY` (your personal key from Nexus Mods' API keys page).

## Layout

| Path | Contents |
| --- | --- |
| `src/Siege/` | All calls into the game's siege API (`SiegeActions`): queries, commands, melee slot rules, cheats |
| `src/Menu/` | The game menus: planning screen and slot screen |
| `src/Panel/` | The Gauntlet panel: view models, overlay, engine sprites |
| `src/Settings/` | MCM settings class and the safe `Options` reader |
| `src/Text.cs` | Helper for the translatable `{=scm_id}English` strings |
| `_Module/` | `SubModule.xml`, the panel prefab and the translation file (`ModuleData/Languages/std_SiegeCommandMenu.xml`) |

Build, deploy, reserve and cancel go through the game's own `SiegeEvent.DoSiegeAction`. Changing the engine containers directly crashed the game at the next daily tick, so don't.

## Troubleshooting

Errors are always written to `error_log.txt` in `Modules\SiegeCommandMenu`. In the mod options, the **Debug** group has two switches:

- **Write debug log**: writes `debug_log.txt` with every build, deploy, reserve and cancel (with the full engine state before and after) and the state on every hourly and daily tick. Lines are written one at a time, so the log survives a crash. Attach `debug_log.txt` to bug reports.
- **Print debug info in game**: also shows each action as a message in the game's message feed.

## Translations

Text lives in the code as `{=scm_id}English`. `_Module/ModuleData/Languages/std_SiegeCommandMenu.xml` lists every string; copy it into a language folder (for example `DE/std_SiegeCommandMenu-de.xml` with a `language_data.xml`) and translate the `text` values.

## Support

If you enjoy the mod, you can support me on [Ko-fi](https://ko-fi.com/darkjeffrey).

## License

MIT, see [LICENSE](LICENSE).
