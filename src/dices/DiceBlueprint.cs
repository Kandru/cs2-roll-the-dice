using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Hud;

namespace RollTheDice.Dices
{
    public class DiceBlueprint(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
    {
        private static readonly List<string> NoNames = [];
        private static readonly Dictionary<int, HookMode> NoUserMessages = [];

        public readonly PluginConfig _globalConfig = GlobalConfig;
        public readonly MapConfig _config = Config;
        public readonly IStringLocalizer _localizer = Localizer;
        public readonly List<CCSPlayerController> _players = [];
        public virtual string Description { get; private set; } = "Unknown Dice";
        public virtual string ClassName => "DiceBlueprint";
        public virtual List<string> Events => NoNames;
        public virtual List<string> Listeners => NoNames;
        public virtual Dictionary<int, HookMode> UserMessages => NoUserMessages;

        protected static bool IsAlivePlayer(CCSPlayerController? player) =>
            player is { IsValid: true }
            && player.Pawn?.Value is { IsValid: true } pawn
            && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE;

        public virtual void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player))
            {
                return;
            }
            _players.Add(player);
            NotifyPlayers(player, ClassName, new() { { "playerName", player.PlayerName } });
        }

        public virtual void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            Banner.Hide(player);
        }

        public virtual void Reset()
        {
            foreach (CCSPlayerController player in _players)
            {
                Banner.Hide(player);
            }
            _players.Clear();
        }

        public virtual void Destroy() => Reset();

        public void NotifyPlayers(CCSPlayerController player, string diceName, Dictionary<string, string> data)
        {
            if (_globalConfig.NotifyOtherPlayers)
            {
                LocalizedString other = _localizer[$"dice_{diceName}_other"];
                if (!other.ResourceNotFound)
                {
                    string message = Format(other.Value, data);
                    foreach (CCSPlayerController entry in Utilities.GetPlayers())
                    {
                        if (entry == player || !entry.IsValid || entry.IsBot || entry.IsHLTV)
                        {
                            continue;
                        }
                        entry.PrintToChat(message);
                    }
                }
            }

            LocalizedString self = _localizer[$"dice_{diceName}_player"];
            if (self.ResourceNotFound)
            {
                return;
            }

            string playerMessage = Format(self.Value, data);
            if (_globalConfig.NotifyPlayerViaChatMsg)
            {
                player.PrintToChat(_localizer["command.prefix"].Value + playerMessage);
            }
            Description = playerMessage;
            Banner.Notify(player, playerMessage);
        }

        public void NotifyStatus(CCSPlayerController player, string diceName, Dictionary<string, string> data)
        {
            LocalizedString status = _localizer[$"dice_{diceName}_status"];
            if (status.ResourceNotFound)
            {
                return;
            }

            string message = Format(status.Value, data);
            player.PrintToChat(_localizer["command.prefix"].Value + message);
            Description = message;
            Banner.Notify(player, message);
        }

        private static string Format(string message, Dictionary<string, string> data)
        {
            foreach ((string key, string value) in data)
            {
                message = message.Replace($"{{{key}}}", value);
            }
            return message;
        }
    }
}
