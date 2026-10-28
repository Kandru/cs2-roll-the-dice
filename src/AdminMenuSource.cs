using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;
using RollTheDice.Dices;
using RollTheDice.Hud;
using RollTheDice.Utils;

namespace RollTheDice
{
    /// <summary>
    /// Admin tab of "!dice info": lock rolling for everyone or single players, choose which dices a roll can draw
    /// (five categories and every dice on its own) and hand out dices. Saved in the config section "admin" at once.
    /// Every action checks the permission again: the menu only hides the tab.
    /// </summary>
    public partial class RollTheDice
    {
        public bool IsAdmin(CCSPlayerController player) =>
            player is { IsValid: true, IsBot: false, IsHLTV: false } && AdminManager.PlayerHasPermissions(player, AdminPermission);

        /// <summary>True when the player may not roll on their own (command, auto roll, timed rolls).</summary>
        private bool IsRollLocked(CCSPlayerController player) =>
            Config.Admin.RollsLocked || IsPlayerLocked(player);

        private bool IsPlayerLocked(CCSPlayerController player) =>
            player.SteamID != 0 && Config.Admin.LockedPlayers.Contains(player.SteamID);

        /// <summary>A random roll may draw this dice: neither the dice nor its category is switched off.</summary>
        private bool IsRollable(DiceBlueprint dice) =>
            !Config.Admin.DisabledDices.Contains(dice.ClassName, StringComparer.OrdinalIgnoreCase)
            && IsCategoryOn(Reel.ToneOf(dice.ClassName));

        private bool IsCategoryOn(ReelTone tone) =>
            !Config.Admin.DisabledCategories.Contains(Reel.ToneName(tone), StringComparer.OrdinalIgnoreCase);

        public AdminState Admin()
        {
            bool[] categories = new bool[InfoMenu.Categories.Length];
            for (int i = 0; i < categories.Length; i++)
            {
                categories[i] = IsCategoryOn(ToReelTone(InfoMenu.Categories[i]));
            }
            return new AdminState(Config.Admin.RollsLocked, Config.Admin.MirrorTeams, categories, _dices.Count(IsRollable), _dices.Count);
        }

        /// <summary>Every player but HLTV (bots too: they can get a dice), CT first, then T, then the rest; by name.</summary>
        public void FillAdminPlayers(CCSPlayerController viewer, List<AdminPlayerRow> rows)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player is not { IsValid: true, IsHLTV: false } || player.UserId is not int userId)
                {
                    continue;
                }

                (InfoTeam team, string badge) = player.Team switch
                {
                    CsTeam.CounterTerrorist => (InfoTeam.Ct, TeamBadge("CT")),
                    CsTeam.Terrorist => (InfoTeam.T, TeamBadge("T")),
                    _ => (InfoTeam.None, "-"),
                };
                bool rolled = _roundDice.TryGetValue(player, out string? className);
                rows.Add(new AdminPlayerRow(
                    userId,
                    HudText.Clean(player.PlayerName, HudText.MaxNameLength),
                    badge,
                    team,
                    rolled ? Reel.LabelFor(viewer, className!) : Text(viewer, "info.admin.no_dice"),
                    rolled ? ToInfoTone(Reel.ToneOf(className!)) : InfoTone.None,
                    player.PawnIsAlive,
                    IsPlayerLocked(player)));
            }

            rows.Sort(static (a, b) =>
            {
                int team = TeamOrder(a.Team).CompareTo(TeamOrder(b.Team));
                return team != 0 ? team : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }

        /// <summary>Every dice of the map, best category first, then by name.</summary>
        public void FillAdminPerks(CCSPlayerController viewer, List<AdminPerkRow> rows)
        {
            CultureInfo culture = Localization.LanguageOf(viewer);
            foreach (DiceBlueprint dice in _dices)
            {
                ReelTone tone = Reel.ToneOf(dice.ClassName);
                InfoTone infoTone = ToInfoTone(tone);
                rows.Add(new AdminPerkRow(
                    dice.ClassName,
                    Reel.LabelFor(viewer, dice.ClassName),
                    Text(viewer, InfoMenu.ToneKey(infoTone)),
                    infoTone,
                    !Config.Admin.DisabledDices.Contains(dice.ClassName, StringComparer.OrdinalIgnoreCase),
                    IsCategoryOn(tone)));
            }

            rows.Sort((a, b) =>
            {
                int order = InfoMenu.ToneOrder(a.Tone).CompareTo(InfoMenu.ToneOrder(b.Tone));
                return order != 0 ? order : string.Compare(a.Title, b.Title, culture, CompareOptions.IgnoreCase);
            });
        }

        public void ToggleRollLock(CCSPlayerController admin)
        {
            if (!IsAdmin(admin))
            {
                return;
            }

            Config.Admin.RollsLocked = !Config.Admin.RollsLocked;
            SaveAdminConfig();
            TellEveryone(Config.Admin.RollsLocked ? "admin.menu.locked_all" : "admin.menu.unlocked_all");
        }

        /// <summary>Mirror mode on/off; perks owed so far are dropped either way.</summary>
        public void ToggleMirror(CCSPlayerController admin)
        {
            if (!IsAdmin(admin))
            {
                return;
            }

            Config.Admin.MirrorTeams = !Config.Admin.MirrorTeams;
            ClearMirror();
            SaveAdminConfig();
            TellEveryone(Config.Admin.MirrorTeams ? "admin.menu.mirror_on" : "admin.menu.mirror_off");
        }

        private void TellEveryone(string key)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player is { IsValid: true, IsBot: false, IsHLTV: false })
                {
                    player.PrintToChat(Text(player, key));
                }
            }
        }

        public void ToggleCategory(CCSPlayerController admin, InfoTone tone)
        {
            if (!IsAdmin(admin) || tone == InfoTone.None)
            {
                return;
            }

            Toggle(Config.Admin.DisabledCategories, Reel.ToneName(ToReelTone(tone)));
            SaveAdminConfig();
        }

        public void TogglePerk(CCSPlayerController admin, string className)
        {
            if (!IsAdmin(admin) || !_dices.Exists(d => d.ClassName == className))
            {
                return;
            }

            Toggle(Config.Admin.DisabledDices, className);
            SaveAdminConfig();
        }

        public void TogglePlayerLock(CCSPlayerController admin, int userId)
        {
            if (!IsAdmin(admin) || Utilities.GetPlayerFromUserid(userId) is not { IsValid: true } target)
            {
                return;
            }
            // bots never roll on their own, and a lock is kept by SteamID
            if (target.IsBot || target.SteamID == 0)
            {
                admin.PrintToChat(Text(admin, "admin.menu.bot"));
                return;
            }

            bool locked = !Config.Admin.LockedPlayers.Remove(target.SteamID);
            if (locked)
            {
                Config.Admin.LockedPlayers.Add(target.SteamID);
            }
            SaveAdminConfig();
            target.PrintToChat(Text(target, locked ? "admin.menu.player_locked" : "admin.menu.player_unlocked"));
        }

        /// <summary>
        /// Like "!givedice": no lock, cooldown or price applies. A random dice comes from the allowed pool; a chosen
        /// dice is given even when it is switched off for rolls.
        /// </summary>
        public void AdminRoll(CCSPlayerController admin, int? userId, string? className)
        {
            if (!IsAdmin(admin) || _dices.Count == 0)
            {
                return;
            }
            if (className == null && !_dices.Exists(IsRollable))
            {
                admin.PrintToChat(Text(admin, "admin.menu.nodices"));
                return;
            }
            if (className != null && !_dices.Exists(d => d.ClassName == className))
            {
                return;
            }

            List<CCSPlayerController> targets = [];
            if (userId is int id)
            {
                if (Utilities.GetPlayerFromUserid(id) is { IsValid: true } target)
                {
                    targets.Add(target);
                }
            }
            else
            {
                targets.AddRange(Utilities.GetPlayers().Where(static p => p is { IsValid: true, IsHLTV: false, PawnIsAlive: true }));
            }

            foreach (CCSPlayerController target in targets)
            {
                if (!target.PawnIsAlive)
                {
                    admin.PrintToChat(Text(admin, "admin.menu.dead").Replace("{player}", target.PlayerName));
                    continue;
                }

                (string? rolledDice, string? diceDescription) = RollTheDiceForPlayer(target, className, ReelSource.Admin);
                if (string.IsNullOrEmpty(rolledDice))
                {
                    continue;
                }

                _playersThatRolledTheDice[target] = diceDescription ?? rolledDice;
                PlayDiceSoundForPlayer(target, rolledDice, false);
                admin.PrintToChat(Text(admin, "command.givedice.success")
                    .Replace("{dice}", Reel.LabelFor(admin, rolledDice))
                    .Replace("{player}", target.PlayerName));
            }
        }

        private void SaveAdminConfig()
        {
            try
            {
                Config.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RollTheDice] Saving the admin settings failed: {ex.Message}");
            }
        }

        private static void Toggle(List<string> list, string value)
        {
            if (list.RemoveAll(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase)) == 0)
            {
                list.Add(value);
            }
        }

        private static ReelTone ToReelTone(InfoTone tone) => tone switch
        {
            InfoTone.VeryGood => ReelTone.VeryGood,
            InfoTone.Good => ReelTone.Good,
            InfoTone.Bad => ReelTone.Bad,
            InfoTone.VeryBad => ReelTone.VeryBad,
            _ => ReelTone.Neutral,
        };
    }
}
