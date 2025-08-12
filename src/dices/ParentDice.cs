using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class ParentDice(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
    {
        public readonly PluginConfig _globalConfig = GlobalConfig;
        public readonly MapConfig _config = Config;
        public readonly IStringLocalizer _localizer = Localizer;
        public readonly List<CCSPlayerController> _players = [];
        public virtual string Description { get; private set; } = "Unknown Dice";
        public virtual string ClassName => "ParentDice";
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
        }

        public virtual void Remove(CCSPlayerController player)
        {
            _ = _players.Remove(player);
        }

        public virtual void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", GetType().Name));
            _players.Clear();
        }

        public void NotifyPlayers(CCSPlayerController player, string diceName, Dictionary<string, string> data)
        {
            // send message to all players
            if (!_localizer[$"dice_{diceName}_all"].ResourceNotFound)
            {
                string message = _localizer[$"dice_{diceName}_all"].Value;
                foreach (KeyValuePair<string, string> kvp in data)
                {
                    message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
                Server.PrintToChatAll(message);
                // update description if available
                Description = message;
            }
            // send message to other players (and maybe player)
            else if (!_localizer[$"dice_{diceName}_other"].ResourceNotFound)
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
            // if player should get a message
            if (!_localizer[$"dice_{diceName}_player"].ResourceNotFound)
            {
                string message = _localizer[$"dice_{diceName}_player"].Value;
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