# Changelog

## 1.0.4
- The "Scroll up" and "Scroll down" labels in the engine panel now come from the mod's code like the rest of its text, so they're always translated and never show a raw text id.

## 1.0.3
- Updated for Bannerlord v1.5.7.
- The launcher no longer warns that the dependencies (Native, SandBoxCore, Sandbox, MCM) don't match: each one now states the version it needs.

## 1.0.2
- "Deploy all from reserve" option in Siege Planning: moves every reserve engine that fits into a free slot.
- Melee slots are named after what they take ("Ram slot", "Siege tower slot 1/2") instead of "Tower or ram slot".
- Fixed a crash on the campaign map after a battering ram finished building or redeploying: rams now go in the first melee slot and siege towers in the other two, the same layout the base game uses (the ram used to go in the middle slot).
- Engines already sitting in the wrong melee slot are taken out automatically: finished ones go to the reserve, unfinished ones are cancelled.
- All player-facing text is translatable, with the English string table in `ModuleData/Languages`.
- Removed the last direct engine-container edits (build and cancel now only use the game's own siege actions).

## 1.0.0
- Siege Planning option in the besieger's siege menu: queue, build, deploy, move to reserve and cancel siege engines.
- Engine panel with pictures, stats, current health (`80% HP`) and build time, clickable and paged four cards at a time.
- Unfinished engines can be cancelled but not sent to the reserve; engines can be marked to move to the reserve when finished.
- Reserve engines show "no free slot available" instead of a deploy option when nothing fits.
- Mod Configuration Menu options: panel, clickable panel, send new engines to reserve, hide non-fire engines, debug, cheats.
- Debug log and in-game debug messages.
