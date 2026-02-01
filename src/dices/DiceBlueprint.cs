using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class DiceBlueprint(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
    {
        public readonly PluginConfig _globalConfig = GlobalConfig;
        public readonly MapConfig _config = Config;
        public readonly IStringLocalizer _localizer = Localizer;
        public readonly List<CCSPlayerController> _players = [];
        public virtual string Description { get; private set; } = "Unknown Dice";
        public virtual string ClassName => "DiceBlueprint";
        public virtual List<string> Events => [];
        public virtual List<string> Listeners => [];
        public virtual Dictionary<int, HookMode> UserMessages => [];
        public virtual List<string> Precache => [];

        public virtual void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public virtual void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
        }

        public virtual void Reset()
        {
            _players.Clear();
        }

        public virtual void Destroy()
        {
            Reset();
        }

        public void NotifyPlayers(CCSPlayerController player, string diceName, Dictionary<string, string> data)
        {
            // send message to other players (not player) if enabled
            if (!_localizer[$"dice_{diceName}_other"].ResourceNotFound
                && _globalConfig.NotifyOtherPlayers)
            {
                // send message to others
                string message = _localizer[$"dice_{diceName}_other"].Value;
                foreach (KeyValuePair<string, string> kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                foreach (CCSPlayerController entry in Utilities.GetPlayers().Where(p => p != player))
                {
                    entry.PrintToChat(message);
                }
            }
            // send message to player who rolled the dice
            if (!_localizer[$"dice_{diceName}_player"].ResourceNotFound
                && (_globalConfig.NotifyPlayerViaChatMsg || _globalConfig.NotifyPlayerViaCenterMsg))
            {
                string message = _localizer[$"dice_{diceName}_player"].Value;
                foreach (KeyValuePair<string, string> kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                if (_globalConfig.NotifyPlayerViaCenterMsg)
                {
                    player.PrintToCenter(message);
                }
                if (_globalConfig.NotifyPlayerViaChatMsg)
                {
                    player.PrintToChat(_localizer["command.prefix"].Value + message);
                }
                // update description if available
                Description = message;
            }
        }

        public void NotifyStatus(CCSPlayerController player, string diceName, Dictionary<string, string> data)
        {
            // if player should get a message
            if (!_localizer[$"dice_{diceName}_status"].ResourceNotFound)
            {
                string message = _localizer[$"dice_{diceName}_status"].Value;
                foreach (KeyValuePair<string, string> kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                player.PrintToCenter(message);
                player.PrintToChat(_localizer["command.prefix"].Value + message);
                // update description if available
                Description = message;
            }
        }
    }
}