using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using RollTheDice.Dices;
using RollTheDice.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin, IPluginConfig<PluginConfig>
    {
        [ConsoleCommand("givedice", "Give Dice to player")]
        [RequiresPermissions("@rollthedice/admin")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "[player] [dice]")]
        public void CommandGiveDice(CCSPlayerController player, CommandInfo command)
        {
            string playerName = command.GetArg(1);
            string diceName = command.GetArg(2);

            if (string.IsNullOrWhiteSpace(playerName))
            {
                command.ReplyToCommand(Localizer["command.givedice.availabledices"]);
                foreach (DiceBlueprint entry in _dices)
                {
                    command.ReplyToCommand($"- {entry.ClassName}");
                }
                return;
            }

            bool isWildcard = string.IsNullOrWhiteSpace(playerName) || playerName == "*";

            var targetPlayers = Utilities.GetPlayers()
                .Where(p => p.IsValid && !p.IsHLTV && !p.IsBot)
                .Where(p => isWildcard || p.PlayerName.Contains(playerName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (targetPlayers.Count == 0)
            {
                command.ReplyToCommand(Localizer["command.givedice.noplayers"]);
                return;
            }

            if (!isWildcard && targetPlayers.Count > 1)
            {
                command.ReplyToCommand(Localizer["command.givedice.toomanyplayers"]);
                return;
            }

            foreach (var target in targetPlayers)
            {
                // RollTheDiceForPlayer handles removing old dice
                (string? rolledDice, string? diceDescription) = RollTheDiceForPlayer(target, string.IsNullOrWhiteSpace(diceName) ? null : diceName);

                if (string.IsNullOrEmpty(rolledDice)) continue;

                _playersThatRolledTheDice[target] = diceDescription ?? rolledDice;
                PlayDiceSoundForPlayer(target, rolledDice, false);
            }
        }

        [ConsoleCommand("rtd", "Roll the Dice")]
        [ConsoleCommand("dice", "Roll the Dice")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "!rtd <auto>")]
        public void CommandRollTheDice(CCSPlayerController player, CommandInfo command)
        {
            if (!Config.Enabled || !_currentMapConfig.Enabled || _dices.Count == 0)
            {
                command.ReplyToCommand(Localizer["core.disabled"]);
                return;
            }

            if (command.GetArg(1) == "auto")
            {
                if (!_playerConfigs.TryGetValue(player.SteamID, out var playerConfig))
                {
                    playerConfig = new PlayerConfig();
                    _playerConfigs.Add(player.SteamID, playerConfig);
                }

                playerConfig.RtdOnSpawn = !playerConfig.RtdOnSpawn;
                command.ReplyToCommand(Localizer[playerConfig.RtdOnSpawn ? "command.rollthedice.rtdonspawn.enabled" : "command.rollthedice.rtdonspawn.disabled"]);
                return;
            }

            // check if warmup period
            if (!Config.AllowRtdDuringWarmup && GameRules.Get("WarmupPeriod") is true)
            {
                command.ReplyToCommand(Localizer["command.rollthedice.iswarmup"]);
                return;
            }

            if (!_isDuringRound)
            {
                command.ReplyToCommand(Localizer["command.rollthedice.noactiveround"]);
                return;
            }

            if (_playersThatRolledTheDice.TryGetValue(player, out var diceVal))
            {
                string message = Localizer["command.rollthedice.alreadyrolled"].Value.Replace("{dice}", diceVal);
                player.PrintToCenter(diceVal);
                command.ReplyToCommand(message);
                return;
            }

            if (_PlayerCooldown.TryGetValue(player, out int cooldown))
            {
                if (Config.CooldownRounds > 0 && cooldown > 0)
                {
                    command.ReplyToCommand(Localizer["command.rollthedice.cooldown.rounds"].Value.Replace("{rounds}", cooldown.ToString()));
                    return;
                }

                int secondsLeft = cooldown - (int)Server.CurrentTime;
                if (Config.CooldownSeconds > 0 && secondsLeft > 0)
                {
                    command.ReplyToCommand(Localizer["command.rollthedice.cooldown.seconds"].Value.Replace("{seconds}", secondsLeft.ToString()));
                    return;
                }
            }

            if (Config.PriceToDice > 0)
            {
                if (player.InGameMoneyServices!.Account < Config.PriceToDice)
                {
                    command.ReplyToCommand(Localizer["command.rollthedice.notenoughmoney"].Value.Replace("{money}", Config.PriceToDice.ToString()));
                    return;
                }
                player.InGameMoneyServices.Account -= Config.PriceToDice;
                Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
            }

            if (player.PlayerPawn?.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                command.ReplyToCommand(Localizer["command.rollthedice.notalive"]);
                return;
            }

            (string? rolledDice, string? diceDescription) = RollTheDiceForPlayer(player);
            if (string.IsNullOrEmpty(rolledDice))
            {
                command.ReplyToCommand(Localizer["command.rollthedice.unlucky"]);
                return;
            }

            _playersThatRolledTheDice[player] = diceDescription ?? rolledDice;

            if (Config.CooldownRounds > 0) _PlayerCooldown[player] = Config.CooldownRounds;
            else if (Config.CooldownSeconds > 0) _PlayerCooldown[player] = (int)Server.CurrentTime + Config.CooldownSeconds;

            PlayDiceSoundForPlayer(player, rolledDice, true);
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
