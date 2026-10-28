using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using RollTheDice.Dices;
using RollTheDice.Hud;
using RollTheDice.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin, IPluginConfig<PluginConfig>
    {
        private const int GiveDiceChatLineLimit = 200;
        // "!rtd" from a console bind is not chat-flood limited: one command per player and interval, the rest is dropped
        private const long CommandIntervalMs = 500;
        private readonly long[] _nextCommandAt = new long[HudLayout.MaxSlots];

        [ConsoleCommand("givedice", "Give Dice to player")]
        [RequiresPermissions("@rollthedice/admin")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "[player] [dice]")]
        public void CommandGiveDice(CCSPlayerController player, CommandInfo command)
        {
            string playerName = command.GetArg(1);
            string diceName = command.GetArg(2);

            if (string.IsNullOrWhiteSpace(playerName))
            {
                ReplyAvailableDices(command);
                return;
            }

            List<CCSPlayerController>? targets = FindGiveDiceTargets(playerName);
            if (targets == null)
            {
                command.ReplyToCommand(Text(player, "command.givedice.toomanyplayers"));
                return;
            }
            if (targets.Count == 0)
            {
                command.ReplyToCommand(Text(player, "command.givedice.noplayers"));
                return;
            }

            if (!TryResolveGiveDiceName(diceName, command, out string? resolvedDiceName))
            {
                return;
            }

            foreach (CCSPlayerController target in targets)
            {
                (string? rolledDice, string? diceDescription) = RollTheDiceForPlayer(target, resolvedDiceName, ReelSource.Admin);
                if (string.IsNullOrEmpty(rolledDice))
                {
                    continue;
                }

                _playersThatRolledTheDice[target] = diceDescription ?? rolledDice;
                PlayDiceSoundForPlayer(target, rolledDice, false);
                command.ReplyToCommand(Text(player, "command.givedice.success")
                    .Replace("{dice}", rolledDice)
                    .Replace("{player}", target.PlayerName));
            }
        }

        // null = ambiguous; empty = none. Includes bots (admin debugging only).
        private static List<CCSPlayerController>? FindGiveDiceTargets(string playerName)
        {
            IEnumerable<CCSPlayerController> players = Utilities.GetPlayers().Where(static p => p.IsValid && !p.IsHLTV);
            if (playerName == "*")
            {
                return [.. players];
            }

            List<CCSPlayerController> matches = [.. players
                .Where(p => p.PlayerName.Contains(playerName, StringComparison.OrdinalIgnoreCase))];
            if (matches.Count <= 1)
            {
                return matches;
            }

            List<CCSPlayerController> exact = [.. matches
                .Where(p => string.Equals(p.PlayerName, playerName, StringComparison.OrdinalIgnoreCase))];
            return exact.Count == 1 ? exact : null;
        }

        private bool TryResolveGiveDiceName(string diceName, CommandInfo command, out string? resolvedName)
        {
            resolvedName = null;
            if (string.IsNullOrWhiteSpace(diceName))
            {
                return true;
            }

            List<DiceBlueprint> matches = [.. _dices
                .Where(d => d.ClassName.Contains(diceName, StringComparison.OrdinalIgnoreCase))];
            DiceBlueprint? exact = matches
                .Find(d => string.Equals(d.ClassName, diceName, StringComparison.OrdinalIgnoreCase));

            if (exact != null)
            {
                resolvedName = exact.ClassName;
                return true;
            }
            if (matches.Count == 1)
            {
                resolvedName = matches[0].ClassName;
                return true;
            }
            if (matches.Count == 0)
            {
                command.ReplyToCommand(Text(command.CallingPlayer, "command.givedice.nodicefound"));
            }
            else
            {
                command.ReplyToCommand(Text(command.CallingPlayer, "command.givedice.ambiguousdice")
                    .Replace("{dices}", string.Join(", ", matches.Select(static d => d.ClassName))));
            }
            return false;
        }

        private void ReplyAvailableDices(CommandInfo command)
        {
            if (_dices.Count == 0)
            {
                command.ReplyToCommand(Text(command.CallingPlayer, "core.nodicesenabled"));
                return;
            }

            command.ReplyToCommand(Text(command.CallingPlayer, "command.givedice.availabledices"));
            string prefix = Text(command.CallingPlayer, "command.prefix");
            System.Text.StringBuilder line = new(prefix);
            foreach (string name in _dices.Select(static d => d.ClassName))
            {
                if (line.Length > prefix.Length && line.Length + name.Length + 2 > GiveDiceChatLineLimit)
                {
                    command.ReplyToCommand(line.ToString());
                    _ = line.Clear().Append(prefix);
                }
                if (line.Length > prefix.Length)
                {
                    _ = line.Append(", ");
                }
                _ = line.Append(name);
            }
            if (line.Length > prefix.Length)
            {
                command.ReplyToCommand(line.ToString());
            }
        }

        [ConsoleCommand("rtd", "Roll the Dice")]
        [ConsoleCommand("dice", "Roll the Dice")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "!rtd <auto|info>")]
        public void CommandRollTheDice(CCSPlayerController player, CommandInfo command)
        {
            if (IsCommandThrottled(player))
            {
                return;
            }

            // the info menu also works while dicing is disabled (settings, commands, history)
            // "!dice info <text>" searches the perks
            if (string.Equals(command.GetArg(1), "info", StringComparison.OrdinalIgnoreCase))
            {
                string args = command.ArgString.Trim();
                int space = args.IndexOf(' ');
                InfoMenu.Toggle(player, space < 0 ? null : args[(space + 1)..].Trim().Trim('"'));
                return;
            }

            if (!Config.Enabled || !_currentMapConfig.Enabled || _dices.Count == 0)
            {
                command.ReplyToCommand(Text(player, "core.disabled"));
                return;
            }

            if (command.GetArg(1) == "auto")
            {
                bool enabled = ToggleRtdOnSpawn(player);
                command.ReplyToCommand(Text(player, enabled ? "command.rollthedice.rtdonspawn.enabled" : "command.rollthedice.rtdonspawn.disabled"));
                InfoMenu.MarkAllDirty();
                return;
            }

            // admin locks (info menu, admin tab)
            if (IsRollLocked(player))
            {
                command.ReplyToCommand(Text(player, Config.Admin.RollsLocked ? "command.rollthedice.locked" : "command.rollthedice.locked_player"));
                return;
            }
            // every dice switched off in the admin menu: say so before anything is charged
            if (!_dices.Exists(IsRollable))
            {
                command.ReplyToCommand(Text(player, "command.rollthedice.nodices"));
                return;
            }

            // check if warmup period
            if (!Config.AllowRtdDuringWarmup && GameRules.Get("WarmupPeriod") is true)
            {
                command.ReplyToCommand(Text(player, "command.rollthedice.iswarmup"));
                return;
            }

            if (!_isDuringRound)
            {
                command.ReplyToCommand(Text(player, "command.rollthedice.noactiveround"));
                return;
            }

            // a joker (Joker dice, kept until the match ends) buys one more roll this round: no cooldown, no price
            bool joker = false;
            if (_playersThatRolledTheDice.ContainsKey(player)
                && player.PlayerPawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE
                && Dices.Joker.TryUse(player))
            {
                joker = true;
                _ = _playersThatRolledTheDice.Remove(player);
                command.ReplyToCommand(Text(player, "command.rollthedice.joker").Replace("{jokers}", Dices.Joker.Count(player).ToString()));
            }

            if (_playersThatRolledTheDice.TryGetValue(player, out string? diceVal))
            {
                string message = Text(player, "command.rollthedice.alreadyrolled").Replace("{dice}", diceVal);
                Banner.Notify(player, diceVal);
                command.ReplyToCommand(message);
                return;
            }

            if (!joker && _PlayerCooldown.TryGetValue(player, out int cooldown))
            {
                if (Config.CooldownRounds > 0 && cooldown > 0)
                {
                    command.ReplyToCommand(Text(player, "command.rollthedice.cooldown.rounds").Replace("{rounds}", cooldown.ToString()));
                    return;
                }

                int secondsLeft = cooldown - (int)Server.CurrentTime;
                if (Config.CooldownSeconds > 0 && secondsLeft > 0)
                {
                    command.ReplyToCommand(Text(player, "command.rollthedice.cooldown.seconds").Replace("{seconds}", secondsLeft.ToString()));
                    return;
                }
            }

            if (player.PlayerPawn?.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                command.ReplyToCommand(Text(player, "command.rollthedice.notalive"));
                return;
            }

            // charged last: a dead player must not pay for a roll that never happens
            if (!joker && Config.PriceToDice > 0)
            {
                if (player.InGameMoneyServices is not { } money || money.Account < Config.PriceToDice)
                {
                    command.ReplyToCommand(Text(player, "command.rollthedice.notenoughmoney").Replace("{money}", Config.PriceToDice.ToString()));
                    return;
                }
                money.Account -= Config.PriceToDice;
                Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
            }

            (string? rolledDice, string? diceDescription) = RollTheDiceForPlayer(player);
            if (string.IsNullOrEmpty(rolledDice))
            {
                command.ReplyToCommand(Text(player, "command.rollthedice.unlucky"));
                return;
            }

            _playersThatRolledTheDice[player] = diceDescription ?? rolledDice;

            if (Config.CooldownRounds > 0)
            {
                _PlayerCooldown[player] = Config.CooldownRounds;
            }
            else if (Config.CooldownSeconds > 0)
            {
                _PlayerCooldown[player] = (int)Server.CurrentTime + Config.CooldownSeconds;
            }

            PlayDiceSoundForPlayer(player, rolledDice, true);
        }


        private bool IsCommandThrottled(CCSPlayerController? player)
        {
            if (player is not { IsValid: true } || (uint)player.Slot >= _nextCommandAt.Length)
            {
                return false;
            }

            long now = Environment.TickCount64; // real time: Server.CurrentTime restarts with every map
            if (now < _nextCommandAt[player.Slot])
            {
                return true;
            }
            _nextCommandAt[player.Slot] = now + CommandIntervalMs;
            return false;
        }

        [ConsoleCommand("rollthedice", "RollTheDice admin commands")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 1, usage: "<command>")]
        public void CommandAdmin(CCSPlayerController player, CommandInfo command)
        {
            string subCommand = command.GetArg(1);
            switch (subCommand.ToLower(System.Globalization.CultureInfo.CurrentCulture))
            {
                case "reload":
                    Config.Reload();
                    LoadMapConfig(_currentMap);
                    command.ReplyToCommand(Localizer["admin.reload"]);
                    break;
                case "disable":
                    Config.Enabled = false;
                    Config.Update();
                    command.ReplyToCommand(Localizer["admin.disable"]);
                    break;
                case "enable":
                    Config.Enabled = true;
                    Config.Update();
                    command.ReplyToCommand(Localizer["admin.enable"]);
                    break;
                case "createmapconfig":
                    // save _currentMapConfig to disk
                    Config.MapConfigs[_currentMap] = _currentMapConfig;
                    Config.Update();
                    command.ReplyToCommand(Localizer["admin.mapconfig.created"].Value.Replace("{mapName}", _currentMap));
                    break;
                case "deletemapconfig":
                    // delete _currentMapConfig from disk
                    _ = Config.MapConfigs.Remove(_currentMap);
                    Config.Update();
                    command.ReplyToCommand(Localizer["admin.mapconfig.deleted"].Value.Replace("{mapName}", _currentMap));
                    break;
                default:
                    command.ReplyToCommand(Localizer["admin.unknown_command"].Value
                        .Replace("{command}", subCommand));
                    break;
            }
        }
    }
}
