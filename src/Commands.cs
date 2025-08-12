using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;
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
            List<CCSPlayerController> availablePlayers = [];
            foreach (CCSPlayerController entry in Utilities.GetPlayers().Where(static p => p.IsValid && !p.IsHLTV))
            {
                if (playerName == null
                    || playerName == "" || playerName == "*"
                    || entry.PlayerName.Contains(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    availablePlayers.Add(entry);
                }
            }
            if (availablePlayers.Count == 0)
            {
                command.ReplyToCommand(Localizer["command.givedice.noplayers"]);
            }
            else if (availablePlayers.Count == 1 || playerName == null || playerName == "" || playerName == "*")
            {
                foreach (CCSPlayerController entry in availablePlayers)
                {
                    _ = RollTheDiceForPlayer(entry, diceName);
                }
            }
            else
            {
                command.ReplyToCommand(Localizer["command.givedice.toomanyplayers"]);
            }
        }

        [ConsoleCommand("rollthedice", "Roll the Dice")]
        [ConsoleCommand("rtd", "Roll the Dice")]
        [ConsoleCommand("dice", "Roll the Dice")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "!rtd <auto>")]
        public void CommandRollTheDice(CCSPlayerController player, CommandInfo command)
        {
            // check if config is enabled
            if (!Config.Enabled
                || !_currentMapConfig.Enabled
                || _dices.Count == 0)
            {
                if (command.CallingContext == CommandCallingContext.Console)
                {
                    player.PrintToChat(Localizer["core.disabled"]);
                }

                command.ReplyToCommand(Localizer["core.disabled"]);
                return;
            }
            // check if argument is "auto" and change behaviour
            if (command.GetArg(1) == "auto")
            {
                if (!_playerConfigs.TryGetValue(player.SteamID, out PlayerConfig? playerConfig))
                {
                    playerConfig = new PlayerConfig();
                    _playerConfigs.Add(player.SteamID, playerConfig);
                }
                // toggle rtd on spawn
                if (playerConfig.RtdOnSpawn)
                {
                    playerConfig.RtdOnSpawn = false;
                    command.ReplyToCommand(Localizer["command.rollthedice.rtdonspawn.disabled"]);
                }
                else
                {
                    playerConfig.RtdOnSpawn = true;
                    command.ReplyToCommand(Localizer["command.rollthedice.rtdonspawn.enabled"]);
                }
                return;
            }
            // check if warmup period
            object? warmupPeriodObj = GameRules.Get("WarmupPeriod");
            if (!Config.AllowRtdDuringWarmup && warmupPeriodObj is bool warmupPeriod && warmupPeriod)
            {
                if (command.CallingContext == CommandCallingContext.Console)
                {
                    player.PrintToChat(Localizer["command.rollthedice.iswarmup"]);
                }

                command.ReplyToCommand(Localizer["command.rollthedice.iswarmup"]);
                return;
            }
            // check if round is active
            if (!_isDuringRound)
            {
                if (command.CallingContext == CommandCallingContext.Console)
                {
                    player.PrintToChat(Localizer["command.rollthedice.noactiveround"]);
                }

                command.ReplyToCommand(Localizer["command.rollthedice.noactiveround"]);
                return;
            }
            // check if player already rolled the dice
            if (_playersThatRolledTheDice.TryGetValue(player, out string? value))
            {
                string message = Localizer["command.rollthedice.alreadyrolled"].Value
                        .Replace("{dice}", value);
                if (command.CallingContext == CommandCallingContext.Console)
                {
                    player.PrintToChat(message);
                }
                player.PrintToCenter(value);
                command.ReplyToCommand(message);
                return;
            }
            // check if player is in cooldown
            if (_PlayerCooldown.ContainsKey(player))
            {
                if (Config.CooldownRounds > 0 && _PlayerCooldown[player] > 0)
                {
                    string message = Localizer["command.rollthedice.cooldown.rounds"].Value
                        .Replace("{rounds}", _PlayerCooldown[player].ToString());
                    if (command.CallingContext == CommandCallingContext.Console)
                    {
                        player.PrintToChat(message);
                    }

                    command.ReplyToCommand(message);
                    return;
                }
                else if (Config.CooldownSeconds > 0 && _PlayerCooldown[player] >= (int)Server.CurrentTime)
                {
                    int secondsLeft = _PlayerCooldown[player] - (int)Server.CurrentTime;
                    string message = Localizer["command.rollthedice.cooldown.seconds"].Value
                        .Replace("{seconds}", secondsLeft.ToString());
                    if (command.CallingContext == CommandCallingContext.Console)
                    {
                        player.PrintToChat(message);
                    }

                    command.ReplyToCommand(message);
                    return;
                }
            }
            // check if player has enough money
            if (Config.PriceToDice > 0)
            {
                if (player.InGameMoneyServices!.Account < Config.PriceToDice)
                {
                    string message = Localizer["command.rollthedice.notenoughmoney"].Value.Replace("{money}", Config.PriceToDice.ToString());
                    if (command.CallingContext == CommandCallingContext.Console)
                    {
                        player.PrintToChat(message);
                    }

                    command.ReplyToCommand(message);
                    return;
                }
                else
                {
                    player.InGameMoneyServices!.Account -= Config.PriceToDice;
                    Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
                }
            }

            // check if player is alive
            if (!player.PlayerPawn?.IsValid == true
                || player.PlayerPawn?.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                if (command.CallingContext == CommandCallingContext.Console)
                {
                    player.PrintToChat(Localizer["command.rollthedice.notalive"]);
                }

                command.ReplyToCommand(Localizer["command.rollthedice.notalive"]);
                return;
            }
            // roll the dice
            (string? rolledDice, string? diceDescription) = RollTheDiceForPlayer(player);
            // check if rolledDice is null or empty
            if (rolledDice is null or "")
            {
                if (command.CallingContext == CommandCallingContext.Console)
                {
                    player.PrintToChat(Localizer["command.rollthedice.unlucky"]);
                }

                command.ReplyToCommand(Localizer["command.rollthedice.unlucky"]);
                return;
            }
            // add player to the list of players that rolled the dice
            _playersThatRolledTheDice[player] = diceDescription ?? rolledDice;
            // add player to cooldown (if applicable)
            if (Config.CooldownRounds > 0)
            {
                _ = _PlayerCooldown.TryAdd(player, 0);
                _PlayerCooldown[player] = Config.CooldownRounds;
            }
            if (Config.CooldownSeconds > 0)
            {
                _ = _PlayerCooldown.TryAdd(player, 0);
                _PlayerCooldown[player] = (int)Server.CurrentTime + Config.CooldownSeconds;
            }
            // play sound
            if (Config.CommandSound is not null and not "")
            {
                if (Config.CommandSound.StartsWith("sounds/"))
                {
                    // simply play sound (will be played at 100% volume regardless of the player's volume settings)
                    player.ExecuteClientCommand($"play {Config.CommandSound}");
                }
                else
                {
                    // only players that rolled the dice will hear the sound
                    RecipientFilter filter = [player];
                    // will be played at the player's volume settings
                    _ = player.EmitSound(Config.CommandSound, filter);
                }
            }
        }

        [ConsoleCommand("rollthedice", "RollTheDice admin commands")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 1, usage: "<command>")]
        public void CommandMapVote(CCSPlayerController player, CommandInfo command)
        {
            string subCommand = command.GetArg(1);
            switch (subCommand.ToLower(System.Globalization.CultureInfo.CurrentCulture))
            {
                case "reload":
                    Config.Reload();
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
                default:
                    command.ReplyToCommand(Localizer["admin.unknown_command"].Value
                        .Replace("{command}", subCommand));
                    break;
            }
        }
    }
}
