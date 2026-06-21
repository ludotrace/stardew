using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace LudoTrace;

public class LudoTraceMod : Mod
{
    private EventLog _log = null!;
    private string _lastLocation = "";
    private List<string> _questsAtDayStart = new();
    private Dictionary<string, int> _friendshipAtDayStart = new();
    private HashSet<string> _recipesAtDayStart = new();
    private static LudoTraceMod? _instance;

    public override void Entry(IModHelper helper)
    {
        _instance = this;
        _log = new EventLog(Path.Combine(Constants.GamePath, "lt_stardew_events.jsonl"), Monitor);

        helper.Events.GameLoop.SaveLoaded += (_, _) =>
        {
            _lastLocation = "";
            _log.Write(BuildSnapshot("session_start"));
        };
        helper.Events.GameLoop.Saving         += (_, _) => _log.Write(BuildSnapshot("session_end"));
        helper.Events.GameLoop.DayStarted     += OnDayStarted;
        helper.Events.Player.Warped           += OnWarped;
        helper.Events.Player.LevelChanged     += OnLevelChanged;
        helper.Events.Player.InventoryChanged += OnInventoryChanged;
        helper.Events.GameLoop.DayEnding      += OnDayEnding;

        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.monsterDrop)),
            postfix:  new HarmonyMethod(typeof(LudoTraceMod), nameof(MonsterDropPostfix)));
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.caughtFish)),
            postfix:  new HarmonyMethod(typeof(LudoTraceMod), nameof(CaughtFishPostfix)));
        harmony.Patch(
            original: AccessTools.Method(typeof(NPC), nameof(NPC.receiveGift)),
            postfix:  new HarmonyMethod(typeof(LudoTraceMod), nameof(ReceiveGiftPostfix)));

        Monitor.Log($"[LudoTrace {ModManifest.Version}] writing to {Path.Combine(Constants.GamePath, "lt_stardew_events.jsonl")}", LogLevel.Info);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _questsAtDayStart = Game1.player.questLog.Select(q => q.questTitle.ToString()).ToList();
        _friendshipAtDayStart = new Dictionary<string, int>();
        foreach (string key in Game1.player.friendshipData.Keys)
            _friendshipAtDayStart[key] = Game1.player.friendshipData[key].Points;
        _recipesAtDayStart = new HashSet<string>(
            Game1.player.cookingRecipes.Keys.Concat(Game1.player.craftingRecipes.Keys));
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.NewLocation is MineShaft mine)
        {
            _log.Write(new
            {
                type = "mine_level",
                floor = mine.mineLevel,
                is_skull_cavern = mine.mineLevel > 120,
                game_date = GameDate(),
                game_time = GameTime()
            });
            _lastLocation = "";
            return;
        }

        string name = e.NewLocation?.Name ?? "";
        if (string.IsNullOrEmpty(name) || name == _lastLocation) return;
        _lastLocation = name;
        _log.Write(new { type = "location", name, game_date = GameDate(), game_time = GameTime() });

        if (e.NewLocation is IslandLocation)
        {
            _log.Write(new
            {
                type     = "near_collectible",
                name     = "Golden Walnuts",
                category = "golden_walnut",
                game_date = GameDate(),
                game_time = GameTime()
            });
        }
    }

    private void OnLevelChanged(object? sender, LevelChangedEventArgs e)
    {
        _log.Write(new
        {
            type  = "stat",
            stat  = e.Skill.ToString(),
            value = e.NewLevel,
            game_date = GameDate(),
            game_time = GameTime()
        });
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        foreach (var item in e.Removed)
        {
            if (item is StardewValley.Object obj && obj.Edibility >= 0)
            {
                _log.Write(new
                {
                    type     = "used",
                    item     = item.Name,
                    category = obj.Category == StardewValley.Object.CookingCategory ? "food" : "consumable",
                    game_date = GameDate(),
                    game_time = GameTime()
                });
            }
        }
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        _log.Write(new
        {
            type   = "sleep",
            result = Game1.player.exhausted.Value ? "passed_out" : "slept",
            game_date = GameDate()
        });

        var currentQuests = Game1.player.questLog.Select(q => q.questTitle.ToString()).ToHashSet();
        foreach (var title in currentQuests.Except(_questsAtDayStart))
            _log.Write(new { type = "quest_stage", quest = title, stage = "started", game_date = GameDate() });
        foreach (var title in _questsAtDayStart.Except(currentQuests))
            _log.Write(new { type = "quest_stage", quest = title, stage = "completed", game_date = GameDate() });

        var bin = Game1.getFarm()?.getShippingBin(Game1.player);
        if (bin?.Count > 0)
        {
            _log.Write(new
            {
                type  = "ship",
                items = bin.Select(item => new
                {
                    name  = item.Name,
                    count = item.Stack,
                    value = item is StardewValley.Object obj ? obj.sellToStorePrice() * item.Stack : 0
                }).ToArray(),
                game_date = GameDate()
            });
        }

        foreach (string npc in Game1.player.friendshipData.Keys)
        {
            var friendship = Game1.player.friendshipData[npc];
            int prev  = _friendshipAtDayStart.TryGetValue(npc, out int p) ? p : 0;
            int delta = friendship.Points - prev;
            if (delta == 0) continue;
            _log.Write(new
            {
                type   = "friendship",
                npc,
                hearts = friendship.Points / NPC.friendshipPointsPerHeartLevel,
                delta,
                game_date = GameDate()
            });
        }

        foreach (var recipe in Game1.player.cookingRecipes.Keys.Concat(Game1.player.craftingRecipes.Keys))
        {
            if (_recipesAtDayStart.Contains(recipe)) continue;
            _log.Write(new
            {
                type = "recipe_learned",
                name = recipe,
                kind = Game1.player.cookingRecipes.ContainsKey(recipe) ? "cooking" : "crafting",
                game_date = GameDate()
            });
        }
    }

    // Harmony postfixes — static; access instance state via _instance

    private static void MonsterDropPostfix(StardewValley.Monsters.Monster monster, Farmer who)
    {
        if (who != Game1.player) return;
        _instance?._log.Write(new
        {
            type   = "kill",
            target = monster.Name,
            game_date = GameDate(),
            game_time = GameTime()
        });
    }

    private static void CaughtFishPostfix(Farmer __instance, string itemId, int size)
    {
        if (__instance != Game1.player) return;
        string name = ItemRegistry.Create(itemId)?.DisplayName ?? itemId;
        _instance?._log.Write(new
        {
            type     = "fish",
            name,
            size,
            location = Game1.player.currentLocation?.Name ?? "Unknown",
            game_date = GameDate(),
            game_time = GameTime()
        });
    }

    private static void ReceiveGiftPostfix(NPC __instance, StardewValley.Object o, Farmer giver)
    {
        if (giver != Game1.player) return;
        string reaction = __instance.getGiftTasteForThisItem(o) switch
        {
            NPC.gift_taste_love    => "loved",
            NPC.gift_taste_like    => "liked",
            NPC.gift_taste_neutral => "neutral",
            NPC.gift_taste_dislike => "disliked",
            NPC.gift_taste_hate    => "hated",
            _                      => "neutral"
        };
        _instance?._log.Write(new
        {
            type     = "gift",
            npc      = __instance.Name,
            item     = o.Name,
            reaction,
            game_date = GameDate(),
            game_time = GameTime()
        });
    }

    internal static string GameDate() =>
        $"{Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}";

    internal static string GameTime()
    {
        int t = Game1.timeOfDay;
        return $"{t / 100:D2}:{t % 100:D2}";
    }

    private static object BuildSnapshot(string type)
    {
        var farmer = Game1.player;
        return new
        {
            type,
            wall_time    = DateTime.UtcNow.ToString("o"),
            game_date    = GameDate(),
            game_time    = GameTime(),
            farm_name    = Game1.getFarm()?.Name ?? "",
            farmer_name  = farmer.Name,
            money        = farmer.Money,
            energy       = (int)farmer.Stamina,
            skills = new
            {
                farming  = farmer.FarmingLevel,
                mining   = farmer.MiningLevel,
                foraging = farmer.ForagingLevel,
                fishing  = farmer.FishingLevel,
                combat   = farmer.CombatLevel
            },
            relationships = farmer.friendshipData.Keys
                .Cast<string>()
                .Select(key => new { npc = key, hearts = farmer.friendshipData[key].Points / NPC.friendshipPointsPerHeartLevel })
                .OrderByDescending(r => r.hearts)
                .ToArray()
        };
    }
}

internal sealed class EventLog
{
    private readonly string _path;
    private readonly IMonitor _monitor;

    private static readonly JsonSerializerOptions _opts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal EventLog(string path, IMonitor monitor)
    {
        _path    = path;
        _monitor = monitor;
    }

    internal void Write(object evt)
    {
        try
        {
            string line = JsonSerializer.Serialize(evt, _opts);
            File.AppendAllText(_path, line + "\n");
        }
        catch (Exception ex)
        {
            _monitor.Log($"[LudoTrace] write failed: {ex.Message}", LogLevel.Warn);
        }
    }
}
