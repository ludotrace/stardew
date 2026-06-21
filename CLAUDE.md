# Stardew Valley LudoTrace Mod

## What it does

A SMAPI mod that writes structured JSONL events to `lt_stardew_events.jsonl` in the game folder.
Follows the LudoTrace mod spec at `mods/SPEC.md` — dumb emitter, append-only, no opinions.

## Repo structure

```
stardew/
  src/LudoTraceMod.cs              ← single-file SMAPI mod
  manifest.json                    ← SMAPI manifest (version placeholder: __VERSION__)
  LudoTrace.csproj                 ← .NET 6 build
  dist/LudoTrace/                  ← compiled output committed for CI packaging
  docs/coaching_prompt.md          ← standalone Claude.ai prompt, included in release zip
  VERSION                          ← single source of truth for version (e.g. v0.1.0)
  Makefile                         ← build / run / release targets
  .github/workflows/release.yml   ← tag → zip → GitHub Release → NexusMods
  README.md                        ← community-facing (NexusMods page)
  CHANGELOG.md                     ← auto-updated by CI on release
```

## Tech

- **SMAPI 4.x** — the standard Stardew modding API. Install alongside the game.
- **Target**: Stardew Valley 1.6.x, .NET 6
- **Install**: drop compiled `LudoTrace.dll` + `manifest.json` into `Stardew Valley/Mods/LudoTrace/`

## Events file path

```
<game-root>/lt_stardew_events.jsonl
```

`Constants.GamePath` in SMAPI points to the Stardew Valley game root (not Mods/).

## lt-client config entry

```toml
[[games]]
game_id     = "stardew"
watch_path  = "C:/Program Files (x86)/Steam/steamapps/common/Stardew Valley"
events_file = "lt_stardew_events.jsonl"
```

`watch_path` is the Stardew Valley game root — same directory `Constants.GamePath` resolves to.

## Date/time format deviation

Stardew has a non-Gregorian calendar (Season / Day 1–28 / Year). We cannot produce ISO 8601
`game_date`. Instead:

```json
"game_date": "Spring 15 Y1"
```

This deviates from the SPEC's ISO 8601 recommendation but preserves semantic meaning for the LLM.
`game_time` follows the SPEC: `"HH:MM"` (Stardew time runs 06:00–02:00 the next day, i.e. up to "26:00").

## Event types

### Reserved (from SPEC)

| Type | Trigger |
|------|---------|
| `session_start` | `GameLoop.SaveLoaded` |
| `session_end` | `GameLoop.Saving` |
| `location` | `Player.Warped` — new named area |
| `kill` | Harmony postfix on `GameLocation.monsterDrop` |
| `quest_stage` | `GameLoop.DayEnding` — quest list delta |
| `stat` | `Player.LevelChanged` |
| `used` | `Player.InventoryChanged` — items consumed |
| `near_collectible` | `Player.Warped` — artifact spots, Golden Walnut zones |

### Game-specific

| Type | Fields | Trigger |
|------|--------|---------|
| `fish` | `name`, `size`, `location` | Harmony postfix on `Farmer.caughtFish` |
| `gift` | `npc`, `item`, `reaction` | Harmony postfix on `NPC.receiveGift` |
| `sleep` | `result` (`"slept"`, `"passed_out"`) | `GameLoop.DayEnding` |
| `mine_level` | `floor`, `is_skull_cavern` | `Player.Warped` to `UndergroundMine` |
| `recipe_learned` | `name`, `kind` (`"cooking"`, `"crafting"`) | `GameLoop.DayEnding` — recipe delta vs `DayStarted` snapshot |
| `ship` | `items` (array of `{name, count, value}`) | `GameLoop.DayEnding` — `Farm.getShippingBin(player)` |
| `friendship` | `npc`, `hearts`, `delta` | `GameLoop.DayEnding` — friendship delta vs `DayStarted` snapshot |

## session_start / session_end snapshot fields

```json
{
  "type": "session_start",
  "game_date": "Spring 15 Y1",
  "game_time": "06:00",
  "farm_name": "Sunridge Farm",
  "farmer_name": "Kris",
  "money": 4200,
  "energy": 270,
  "skills": {"farming": 3, "mining": 2, "foraging": 1, "fishing": 4, "combat": 2},
  "relationships": [{"npc": "Penny", "hearts": 6}, ...]
}
```

## Implementation notes

### Monster kills
SMAPI has no direct monster kill event. Reliable approach: patch
`GameLocation.monsterDrop` with Harmony, or check `location.characters` delta on
`GameLoop.UpdateTicked` with low frequency. The Harmony patch is cleaner.

### Fishing
`Event.command_catch` is the cleanest hook. Alternatively patch
`Farmer.caughtFish(Item, int, GameLocation)`.

### Item use (consumed)
`Player.InventoryChanged` gives `ItemsLost` — filter for `Edible` items (Object.Edibility >= 0)
to identify consumables rather than crafted/dropped items.

### Quest tracking
SMAPI doesn't expose a quest-completed event reliably. Compare `Farmer.questLog` at
`DayStarted` vs `DayEnding` to detect additions/completions.

## Build

```bash
make build
```

Injects `VERSION` into `manifest.json`, runs `dotnet build -c Release`, restores
`manifest.json`, then copies `LudoTrace.dll` and a versioned `manifest.json` to
`dist/LudoTrace/`. Commit `dist/` so CI can package without a build environment.

## Release

```bash
# 1. Bump VERSION file
# 2. Commit: git add VERSION dist/ && git commit -m "Release vX.Y.Z"
make release   # guards clean state + main branch, tags, pushes
```

GitHub Actions builds the zip from `dist/` and uploads to NexusMods automatically.
First release requires a manual NexusMods upload to create the mod page; after that,
set `NEXUSMODS_API_KEY` (secret) and `NEXUSMODS_GROUP_ID` (var) in the repo settings.

## Dependencies

- SMAPI (smapi.io)
- Harmony (bundled with SMAPI, no separate install)

## Backend prerequisites

- [ ] Core game registry: `stardew` registered before first lt-client upload
