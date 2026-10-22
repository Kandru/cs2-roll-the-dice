using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using RollTheDice.Dices;
using RollTheDice.Enums;
using RollTheDice.Hud;
using RollTheDice.Utils;

namespace RollTheDice
{
    /// <summary>Data behind the "!dice info" menu: settings, commands, perks, own history and this round's dice.</summary>
    public partial class RollTheDice : IInfoMenuSource
    {
        private const int MaxHistory = 12;
        private const string AdminPermission = "@rollthedice/admin";

        /// <summary>Order of the language buttons in the menu.</summary>
        private static readonly string[] MenuLanguages = ["de", "en"];

        private sealed record HistoryEntry(string ClassName, string Text, DateTime At);

        private sealed record CommandHelp(string Command, string Key, string? MetaKey);

        private static readonly CommandHelp[] PlayerCommands =
        [
            new("!rtd  ·  !dice", "info.cmd.rtd", null),
            new("!rtd auto", "info.cmd.auto", null),
            new("!dice info", "info.cmd.info", null),
            new("!dice info <text>", "info.cmd.search", null),
        ];

        // chat commands only (the server console commands are not usable from the game); shown to admins only
        private static readonly CommandHelp[] AdminCommands =
        [
            new("!givedice", "info.cmd.givedice_list", "info.meta.admin"),
            new("!givedice <player|*> [dice]", "info.cmd.givedice", "info.meta.admin"),
        ];

        // newest last; per SteamID, kept across maps while the plugin is loaded
        private readonly Dictionary<ulong, List<HistoryEntry>> _history = [];
        // dice class rolled this round per player (bots included: they show up in the round list)
        private readonly Dictionary<CCSPlayerController, string> _roundDice = [];

        public string Text(CCSPlayerController? player, string key) =>
            player is { IsValid: true } ? Localizer.ForPlayerSafe(player, key) : Localizer[key].Value;

        public InfoSettings Settings(CCSPlayerController player)
        {
            bool forced = Config.DiceTrigger.ForceAllPlayers;
            bool allowed = Config.DiceTrigger.AllowPlayerAutoRtd;
            bool own = _playerConfigs.TryGetValue(player.SteamID, out PlayerConfig? config) && config.RtdOnSpawn;
            string hintKey = forced
                ? "info.settings.auto_forced"
                : !allowed
                    ? "info.settings.auto_disabled"
                    : Config.DiceTrigger.TriggerEvent == DiceTriggerEvent.RoundStart
                        ? "info.settings.auto_hint_roundstart"
                        : "info.settings.auto_hint_freezeend";
            string language = Localization.LanguageOf(player).TwoLetterISOLanguageName;
            return new InfoSettings(
                AutoOn: forced || (allowed && own),
                AutoLocked: forced || !allowed,
                AutoHint: Text(player, hintKey),
                Language: Array.IndexOf(MenuLanguages, language));
        }

        public void ToggleAutoRoll(CCSPlayerController player)
        {
            _ = ToggleRtdOnSpawn(player);
        }

        public void SetLanguage(CCSPlayerController player, int language)
        {
            if ((uint)language >= MenuLanguages.Length || player.IsBot)
            {
                return;
            }

            string code = MenuLanguages[language];
            GetOrCreatePlayerConfig(player).Language = code;
            ApplyLanguage(player, code);
        }

        public void FillRows(CCSPlayerController player, InfoTab tab, List<InfoRow> rows)
        {
            switch (tab)
            {
                case InfoTab.Commands:
                    FillCommands(player, rows);
                    break;
                case InfoTab.Perks:
                    FillPerks(player, rows);
                    break;
                case InfoTab.History:
                    FillHistory(player, rows);
                    break;
                case InfoTab.Round:
                    FillRound(player, rows);
                    break;
                case InfoTab.Settings:
                default:
                    break;
            }
        }

        private void FillCommands(CCSPlayerController player, List<InfoRow> rows)
        {
            foreach (CommandHelp help in PlayerCommands)
            {
                rows.Add(new InfoRow(help.Command, string.Empty, Text(player, help.Key)));
            }

            if (!AdminManager.PlayerHasPermissions(player, AdminPermission))
            {
                return;
            }

            foreach (CommandHelp help in AdminCommands)
            {
                rows.Add(new InfoRow(help.Command, Text(player, help.MetaKey!), Text(player, help.Key)));
            }
        }

        private void FillPerks(CCSPlayerController player, List<InfoRow> rows)
        {
            foreach (DiceBlueprint dice in _dices)
            {
                ReelTone tone = Reel.ToneOf(dice.ClassName);
                string info = Text(player, $"dice_{dice.ClassName}_info");
                rows.Add(new InfoRow(
                    Reel.LabelFor(player, dice.ClassName),
                    Text(player, ToneKey(tone)),
                    info == $"dice_{dice.ClassName}_info" ? string.Empty : info,
                    ToInfoTone(tone)));
            }

            CultureInfo culture = Localization.LanguageOf(player);
            rows.Sort((a, b) => string.Compare(a.Title, b.Title, culture, CompareOptions.IgnoreCase));
        }

        private void FillHistory(CCSPlayerController player, List<InfoRow> rows)
        {
            if (!_history.TryGetValue(player.SteamID, out List<HistoryEntry>? entries))
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                HistoryEntry entry = entries[i];
                rows.Add(new InfoRow(
                    Reel.LabelFor(player, entry.ClassName),
                    RelativeTime(player, now - entry.At),
                    entry.Text,
                    ToInfoTone(Reel.ToneOf(entry.ClassName))));
            }
        }

        /// <summary>Sorted by team (CT, T, others), then alive before dead, then name. The team is the row badge.</summary>
        private void FillRound(CCSPlayerController player, List<InfoRow> rows)
        {
            foreach ((CCSPlayerController owner, string className) in _roundDice)
            {
                if (owner is not { IsValid: true })
                {
                    continue;
                }

                bool alive = owner.PawnIsAlive;
                (InfoTeam team, string badge) = owner.Team switch
                {
                    CsTeam.CounterTerrorist => (InfoTeam.Ct, TeamBadge("CT")),
                    CsTeam.Terrorist => (InfoTeam.T, TeamBadge("T")),
                    _ => (InfoTeam.None, "-"),
                };
                rows.Add(new InfoRow(
                    owner.PlayerName,
                    string.Empty, // alive/dead is the row style (red frame, greyed text), not a label
                    Reel.LabelFor(player, className),
                    ToInfoTone(Reel.ToneOf(className)),
                    alive ? InfoLife.Alive : InfoLife.Dead,
                    owner == player,
                    team,
                    badge));
            }

            rows.Sort(static (a, b) =>
            {
                int team = TeamOrder(a.Team).CompareTo(TeamOrder(b.Team));
                if (team != 0)
                {
                    return team;
                }
                int life = a.Life.CompareTo(b.Life);
                return life != 0 ? life : string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
            });
        }

        // The client localizes a dialog variable that equals a token: "CT" showed up as the German team name.
        // Non-breaking spaces keep the text from matching while it still looks the same.
        private static string TeamBadge(string team) => "\u00a0" + team + "\u00a0";

        private static int TeamOrder(InfoTeam team) => team switch
        {
            InfoTeam.Ct => 0,
            InfoTeam.T => 1,
            _ => 2,
        };

        /// <summary>Called right after a dice was applied: feeds the round list and the player's history.</summary>
        private void RecordRoll(CCSPlayerController player, DiceBlueprint dice)
        {
            _roundDice[player] = dice.ClassName;
            if (!player.IsBot && player.SteamID != 0)
            {
                if (!_history.TryGetValue(player.SteamID, out List<HistoryEntry>? entries))
                {
                    _history[player.SteamID] = entries = [];
                }

                entries.Add(new HistoryEntry(dice.ClassName, Banner.StripColors(dice.Description), DateTime.UtcNow));
                if (entries.Count > MaxHistory)
                {
                    entries.RemoveAt(0);
                }
            }
            InfoMenu.MarkAllDirty();
        }

        private bool ToggleRtdOnSpawn(CCSPlayerController player)
        {
            PlayerConfig config = GetOrCreatePlayerConfig(player);
            config.RtdOnSpawn = !config.RtdOnSpawn;
            return config.RtdOnSpawn;
        }

        private PlayerConfig GetOrCreatePlayerConfig(CCSPlayerController player)
        {
            if (!_playerConfigs.TryGetValue(player.SteamID, out PlayerConfig? config))
            {
                config = new PlayerConfig();
                _playerConfigs.Add(player.SteamID, config);
            }
            return config;
        }

        /// <summary>Saved menu choice → CounterStrikeSharp player language (also used by every ForPlayer lookup).</summary>
        private void ApplySavedLanguage(CCSPlayerController? player)
        {
            if (player is not { IsValid: true, IsBot: false, IsHLTV: false }
                || !_playerConfigs.TryGetValue(player.SteamID, out PlayerConfig? config)
                || string.IsNullOrEmpty(config.Language))
            {
                return;
            }
            ApplyLanguage(player, config.Language);
        }

        private static void ApplyLanguage(CCSPlayerController player, string code)
        {
            if (player.AuthorizedSteamID is not SteamID steamId)
            {
                return;
            }

            try
            {
                PlayerLanguageManager.Instance.SetLanguage(steamId, new CultureInfo(code));
            }
            catch (CultureNotFoundException)
            {
                Console.WriteLine($"[RollTheDice] Unknown language '{code}' for {player.PlayerName}");
            }
        }

        private string RelativeTime(CCSPlayerController player, TimeSpan age)
        {
            int minutes = (int)age.TotalMinutes;
            if (minutes < 1)
            {
                return Text(player, "info.time.now");
            }
            if (minutes < 60)
            {
                return Text(player, "info.time.minutes").Replace("{n}", minutes.ToString());
            }
            return Text(player, "info.time.hours").Replace("{n}", ((int)age.TotalHours).ToString());
        }

        private static string ToneKey(ReelTone tone) => tone switch
        {
            ReelTone.Good => "info.tone.good",
            ReelTone.Bad => "info.tone.bad",
            ReelTone.Neutral => "info.tone.neutral",
            _ => "info.tone.neutral",
        };

        private static InfoTone ToInfoTone(ReelTone tone) => tone switch
        {
            ReelTone.Good => InfoTone.Good,
            ReelTone.Bad => InfoTone.Bad,
            ReelTone.Neutral => InfoTone.Neutral,
            _ => InfoTone.None,
        };
    }
}
