# LudoTrace for Stardew Valley

A session black box for Stardew Valley. Records your farm progress, skill gains, gifts,
fishing, mine depth, and daily activity to a structured log — then get coaching from Claude.

## Requirements

- [SMAPI](https://smapi.io/) 4.x

## Install

Drop the contents of the release zip into your Stardew Valley `Mods/` folder:

```
Stardew Valley/
  Mods/
    LudoTrace/
      LudoTrace.dll
      manifest.json
```

Install via Vortex or Mod Organizer 2, or copy manually.

## Usage

**1. Play.** Load any save — the mod starts recording automatically. The log is written to:

```
Stardew Valley/lt_stardew_events.jsonl
```

**2. Get coaching.** Open [Claude.ai](https://claude.ai), start a new chat, and paste
the prompt from `coaching_prompt.md` followed by your log contents.

**Example output:**

> **Penny is at 6 hearts and you gave her a Daffodil — her neutral gift.** She loves
> Poppy, Sandfish, and Melon. One Poppy before the end of Spring locks in the 8-heart
> event before summer.
>
> **You reached mine floor 45 in this session but have Mining level 2.** The ore density
> below floor 40 rewards higher Mining — spend the next two rainy days in the mines before
> going deeper.
>
> **You shipped 12 Parsnips at base value.** With Farming 3 you're one level from the
> Tiller profession — hold the next harvest until you level up and choose Tiller for a
> permanent 10% crop price bonus.

## Snapshot format

```jsonl
{"type":"session_start","game_date":"Spring 15 Y1","game_time":"06:00","farm_name":"Sunridge Farm","money":4200,"energy":270,"skills":{"farming":3,"mining":2,"foraging":1,"fishing":4,"combat":2}}
{"type":"location","name":"Pelican Town","game_date":"Spring 15 Y1","game_time":"09:30"}
{"type":"gift","npc":"Penny","item":"Poppy","reaction":"loved","game_date":"Spring 15 Y1","game_time":"10:15"}
{"type":"mine_level","floor":45,"is_skull_cavern":false,"game_date":"Spring 15 Y1","game_time":"11:30"}
{"type":"stat","stat":"Mining","value":4,"game_date":"Spring 15 Y1","game_time":"12:00"}
{"type":"fish","name":"Eel","size":18,"location":"Ocean","game_date":"Spring 15 Y1","game_time":"14:30"}
{"type":"session_end","game_date":"Spring 16 Y1","game_time":"06:00","farm_name":"Sunridge Farm","money":5650,"energy":270,"skills":{"farming":3,"mining":4,"foraging":1,"fishing":4,"combat":2}}
```

## Notes

- The log is append-only across sessions. lt-client uploads and manages the file automatically.
  If using Claude.ai manually, paste the contents of the most recent session (from the last
  `session_start` line to the end of the file).
- The log does not contain your Steam account or any identifying information.
