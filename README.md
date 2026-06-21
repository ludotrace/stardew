# LudoTrace for Stardew Valley

A session journal for Stardew Valley. Records your farm progress, skill gains, gifts,
fishing, mine depth, and daily activity to a plain JSONL file — then get coaching from Claude.

## Requirements

- [SMAPI](https://smapi.io/) 4.x

## Install

Drop the `LudoTrace` folder from the release zip into your Stardew Valley `Mods/` directory:

```
Stardew Valley/
  Mods/
    LudoTrace/
      LudoTrace.dll
      manifest.json
```

Install via Vortex, or copy manually. SMAPI loads it automatically on next launch.

## Usage

**1. Play.** Load any save — the mod starts recording automatically. Sleep to end the day
and flush that session's events. The log is written to:

```
Stardew Valley/lt_stardew_events.jsonl
```

**2. Get coaching.** Open [Claude.ai](https://claude.ai), start a new chat, and paste
the contents of `coaching_prompt.md` followed by your log.

**Example output:**

> **Penny is at 6 hearts and you gave her a Daffodil — her neutral gift.** She loves
> Poppy, Sandfish, and Melon. One Poppy before the end of Spring locks in the 8-heart
> event before Summer.
>
> **You reached mine floor 45 in this session but have Mining level 2.** The ore density
> below floor 40 rewards higher Mining — spend the next two rainy days in the mines before
> going deeper.
>
> **You shipped 12 Parsnips at base value.** With Farming 3 you're one level from the
> Tiller profession — hold the next harvest until you level up and choose Tiller for a
> permanent 10% crop price bonus.

## Event format

The log is plain JSONL (one JSON object per line), append-only across sessions:

```jsonl
{"type":"session_start","game_date":"spring 15 Y1","game_time":"06:00","farm_name":"Sunridge Farm","money":4200,"energy":270,"skills":{"farming":3,"mining":2,"foraging":1,"fishing":4,"combat":2}}
{"type":"location","name":"Town","game_date":"spring 15 Y1","game_time":"09:30"}
{"type":"gift","npc":"Penny","item":"Poppy","reaction":"loved","game_date":"spring 15 Y1","game_time":"10:15"}
{"type":"mine_level","floor":45,"is_skull_cavern":false,"game_date":"spring 15 Y1","game_time":"11:30"}
{"type":"stat","stat":"Mining","value":4,"game_date":"spring 15 Y1","game_time":"12:00"}
{"type":"fish","name":"Eel","size":18,"location":"Ocean","game_date":"spring 15 Y1","game_time":"14:30"}
{"type":"sleep","result":"slept","game_date":"spring 15 Y1"}
{"type":"ship","items":[{"name":"Parsnip","count":12,"value":120}],"game_date":"spring 15 Y1"}
{"type":"session_end","game_date":"spring 16 Y1","game_time":"06:00","farm_name":"Sunridge Farm","money":5650,"energy":270,"skills":{"farming":3,"mining":4,"foraging":1,"fishing":4,"combat":2}}
```

Paste the contents from the last `session_start` to the end of the file for single-session
coaching, or the full file for a cross-session summary.

## Notes

- The log does not contain your Steam account or any identifying information.
- Each session runs from `session_start` (save loaded) to `session_end` (save written).
- The `game_date` field uses Stardew's in-game calendar: `"spring 15 Y1"`, `"fall 28 Y3"`, etc.
