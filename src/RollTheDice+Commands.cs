using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using System.Drawing;
using System.Reflection;

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
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
            {
                if (playerName == null
                    || playerName == "" || playerName == "*"
                    || entry.PlayerName.Contains(playerName, StringComparison.OrdinalIgnoreCase)) availablePlayers.Add(entry);
            }
            if (availablePlayers.Count == 0)
            {
                command.ReplyToCommand(Localizer["command.givedice.noplayers"]);
            }
            else if (availablePlayers.Count == 1 || playerName == null || playerName == "" || playerName == "*")
            {
                foreach (CCSPlayerController entry in availablePlayers)
                {
                    // remove dice for player (if any)
                    if (_playersThatRolledTheDice.ContainsKey(entry))
                    {
                        // remove gui
                        RemoveGUI(entry);
                        // reset dice for player if possible
                        string methodName = $"{_playersThatRolledTheDice[entry]["dice"]}ResetForPlayer";
                        var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                        if (method != null)
                        {
                            DebugPrint($"Resetting dice: {_playersThatRolledTheDice[entry]["dice"]} for {entry.PlayerName}");
                            method.Invoke(this, [entry]);
                        }
                        // remove player from dices
                        _playersThatRolledTheDice.Remove(entry);
                    }
                    // check if random dice should be rolled
                    if (diceName == null || diceName == "")
                    {
                        var diceIndex = GetRandomDice();
                        if (diceIndex == -1)
                        {
                            command.ReplyToCommand(Localizer["command.givedice.nodicefound"]);
                            return;
                        }
                        // add player to list
                        _playersThatRolledTheDice.Add(entry, new Dictionary<string, object> { { "dice", _dices[diceIndex].Method.Name } });
                        // count dice roll
                        _countRolledDices[_dices[diceIndex].Method.Name]++;
                        // execute
                        ExecuteDice(entry, diceIndex);
                    } // check if dice is found
                    else if (_dices.Any(d => d.Method.Name.Contains(diceName, StringComparison.OrdinalIgnoreCase)))
                    {
                        // get index
                        var diceIndex = _dices.FindIndex(d => d.Method.Name.Contains(diceName, StringComparison.OrdinalIgnoreCase));
                        // add player to list
                        _playersThatRolledTheDice.Add(entry, new Dictionary<string, object> { { "dice", _dices[diceIndex].Method.Name } });
                        // execute
                        ExecuteDice(entry, diceIndex);
                    }
                    else
                    {
                        // oops, no dice found
                        command.ReplyToCommand(Localizer["command.givedice.nodicefound"]);
                    }
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
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CommandRollTheDice(CCSPlayerController player, CommandInfo command)
        {
            Config.MapConfigs.TryGetValue(_currentMap, out var mapConfig);
            // check if config is enabled
            if ((!Config.Enabled)
                || (Config.Enabled && mapConfig != null && !mapConfig.Enabled))
            {
                if (command.CallingContext == CommandCallingContext.Console)
                    player.PrintToChat(Localizer["core.disabled"]);
                command.ReplyToCommand(Localizer["core.disabled"]);
                return;
            }
            // check if warmup period
            if ((bool)GetGameRule("WarmupPeriod")!)
            {
                if (command.CallingContext == CommandCallingContext.Console)
                    player.PrintToChat(Localizer["command.rollthedice.iswarmup"]);
                command.ReplyToCommand(Localizer["command.rollthedice.iswarmup"]);
                return;
            }
            // check if round is active
            if (!_isDuringRound)
            {
                if (command.CallingContext == CommandCallingContext.Console)
                    player.PrintToChat(Localizer["command.rollthedice.noactiveround"]);
                command.ReplyToCommand(Localizer["command.rollthedice.noactiveround"]);
                return;
            }
            // check if player already rolled the dice
            if (_playersThatRolledTheDice.ContainsKey(player))
            {
                if (_playersThatRolledTheDice[player].ContainsKey("message"))
                {
                    string message = Localizer["command.rollthedice.alreadyrolled"].Value
                            .Replace("{dice}", (string)_playersThatRolledTheDice[player]["message"]);
                    if (command.CallingContext == CommandCallingContext.Console)
                        player.PrintToChat(message);
                    command.ReplyToCommand(message);
                }
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
                        player.PrintToChat(message);
                    command.ReplyToCommand(message);
                    return;
                }
                else if (Config.CooldownSeconds > 0 && _PlayerCooldown[player] >= (int)Server.CurrentTime)
                {
                    int secondsLeft = _PlayerCooldown[player] - (int)Server.CurrentTime;
                    string message = Localizer["command.rollthedice.cooldown.seconds"].Value
                        .Replace("{seconds}", secondsLeft.ToString());
                    if (command.CallingContext == CommandCallingContext.Console)
                        player.PrintToChat(message);
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
                        player.PrintToChat(message);
                    command.ReplyToCommand(message);
                    return;
                }
                else
                {
                    player.InGameMoneyServices!.Account -= Config.PriceToDice;
                    Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
                }
            }
            if (player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                if (command.CallingContext == CommandCallingContext.Console)
                    player.PrintToChat(Localizer["command.rollthedice.notalive"]);
                command.ReplyToCommand(Localizer["command.rollthedice.notalive"]);
                return;
            }
            // get random dice
            int diceIndex = GetRandomDice();
            if (diceIndex == -1)
            {
                if (command.CallingContext == CommandCallingContext.Console)
                    player.PrintToChat(Localizer["core.nodicesenabled"]);
                command.ReplyToCommand(Localizer["command.rollthedice.nodicesenabled"]);
                return;
            }
            // debug print
            DebugPrint($"Player {player.PlayerName} rolled the dice and got {_dices[diceIndex].Method.Name}");
            // add player to list
            _playersThatRolledTheDice.Add(player, new Dictionary<string, object> { { "dice", _dices[diceIndex].Method.Name } });
            // add player to cooldown (if applicable)
            if (Config.CooldownRounds > 0)
            {
                if (!_PlayerCooldown.ContainsKey(player)) _PlayerCooldown.Add(player, 0);
                _PlayerCooldown[player] = Config.CooldownRounds;
            }
            if (Config.CooldownSeconds > 0)
            {
                if (!_PlayerCooldown.ContainsKey(player)) _PlayerCooldown.Add(player, 0);
                _PlayerCooldown[player] = (int)Server.CurrentTime + Config.CooldownSeconds;
            }
            // count dice roll
            _countRolledDices[_dices[diceIndex].Method.Name]++;
            // execute dice function
            ExecuteDice(player, diceIndex);
            // play sound
            if (Config.CommandSound != null && Config.CommandSound != "") player.ExecuteClientCommand($"play {Config.CommandSound}");
        }

        public void ExecuteDice(CCSPlayerController player, int dice = -1)
        {
            if (player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null
                || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            if (dice == -1) return;
            // execute dice function
            Dictionary<string, string> data = _dices[dice](player, player.PlayerPawn.Value);
            // check for error
            if (data.ContainsKey("error"))
            {
                string message = Localizer[data["error"]];
                // send message to player
                player.PrintToCenter(message);
                player.PrintToChat(message);
                // change player message
                _playersThatRolledTheDice[player]["message"] = message;
                return;
            }
            // set default locale to the dice method name
            string locale = _dices[dice].Method.Name;
            // check if we should use a provided locale
            if (data.ContainsKey("locale")) locale = data["locale"];
            // send message to all players
            if (!Localizer[$"{locale}_all"].ResourceNotFound)
            {
                string message = Localizer[$"{locale}_all"].Value;
                foreach (var kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                SendGlobalChatMessage(message);
            }
            // send message to other players (and maybe player)
            else if (!Localizer[$"{locale}_other"].ResourceNotFound)
            {
                // send message to others
                string message = Localizer[$"{locale}_other"].Value;
                foreach (var kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                SendGlobalChatMessage(message, player: player);
            }
            // if player should get a message
            if (!Localizer[$"{locale}_player"].ResourceNotFound)
            {
                string message = Localizer[$"{locale}_player"].Value;
                foreach (var kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                player.PrintToCenter(message);
                player.PrintToChat(message);
                // change player message (if any)
                _playersThatRolledTheDice[player]["message"] = message;
            }
            // if player should get a GUI message
            if (!Localizer[$"{locale}_gui"].ResourceNotFound && Config.GUIPositions.ContainsKey(Config.GUIPosition))
            {
                string message = Localizer["command.prefix"].Value
                    + " "
                    + Localizer[$"{locale}_gui"].Value;
                foreach (var kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                // create gui entity
                Color messageColor;
                try
                {
                    messageColor = ColorTranslator.FromHtml(Config.GUIPositions[Config.GUIPosition].MessageColor);
                }
                catch
                {
                    messageColor = Color.Purple;
                }
                CPointWorldText? playerGUIMessage = WorldTextManager.Create(
                    player: player,
                    message: message,
                    size: Config.GUIPositions[Config.GUIPosition].MessageFontSize,
                    color: messageColor,
                    font: Config.GUIPositions[Config.GUIPosition].MessageFont,
                    shiftX: Config.GUIPositions[Config.GUIPosition].MessageShiftX,
                    shiftY: Config.GUIPositions[Config.GUIPosition].MessageShiftY,
                    drawBackground: Config.GUIPositions[Config.GUIPosition].MessageDrawBackground,
                    backgroundFactor: Config.GUIPositions[Config.GUIPosition].MessageBackgroundFactor
                );
                if (playerGUIMessage != null) _playersThatRolledTheDice[player]["gui_message"] = playerGUIMessage;
                // create (empty) status gui entity
                Color statusColor;
                try
                {
                    statusColor = ColorTranslator.FromHtml(Config.GUIPositions[Config.GUIPosition].StatusColorDisabled);
                }
                catch
                {
                    statusColor = Color.Red;
                }
                statusColor = Color.Red;
                CPointWorldText? playerGUIStatus = WorldTextManager.Create(
                    player: player,
                    message: "",
                    size: Config.GUIPositions[Config.GUIPosition].StatusFontSize,
                    color: statusColor,
                    font: Config.GUIPositions[Config.GUIPosition].StatusFont,
                    shiftX: Config.GUIPositions[Config.GUIPosition].StatusShiftX,
                    shiftY: Config.GUIPositions[Config.GUIPosition].StatusShiftY,
                    drawBackground: Config.GUIPositions[Config.GUIPosition].StatusDrawBackground,
                    backgroundFactor: Config.GUIPositions[Config.GUIPosition].StatusBackgroundFactor
                );
                if (playerGUIStatus != null) _playersThatRolledTheDice[player]["gui_status"] = playerGUIStatus;
            }
        }
    }
}
