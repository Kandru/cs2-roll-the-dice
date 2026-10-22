using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Hud;
using RollTheDice.Utils;

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
            // every text in the reader's language (info menu setting / css_lang)
            if (_globalConfig.NotifyOtherPlayers)
            {
                string otherKey = $"dice_{diceName}_other";
                if (!_localizer[otherKey].ResourceNotFound)
                {
                    foreach (CCSPlayerController entry in Utilities.GetPlayers())
                    {
                        if (entry == player || !entry.IsValid || entry.IsBot || entry.IsHLTV)
                        {
                            continue;
                        }
                        entry.PrintToChat(Format(_localizer.ForPlayerSafe(entry, otherKey), data));
                    }
                }
            }

            string selfKey = $"dice_{diceName}_player";
            if (_localizer[selfKey].ResourceNotFound)
            {
                return;
            }

            string playerMessage = Format(_localizer.ForPlayerSafe(player, selfKey), data);
            if (_globalConfig.NotifyPlayerViaChatMsg)
            {
                SendChat(player, _localizer.ForPlayerSafe(player, "command.prefix") + playerMessage);
            }
            Description = playerMessage;
            Banner.Notify(player, playerMessage);
        }

        public void NotifyStatus(CCSPlayerController player, string diceName, Dictionary<string, string> data)
        {
            string statusKey = $"dice_{diceName}_status";
            if (_localizer[statusKey].ResourceNotFound)
            {
                return;
            }

            string message = Format(_localizer.ForPlayerSafe(player, statusKey), data);
            SendChat(player, _localizer.ForPlayerSafe(player, "command.prefix") + message);
            Description = message;
            Banner.Notify(player, message);
        }

        // held back while the player's reel spins, so chat does not reveal the result early
        private static void SendChat(CCSPlayerController player, string message)
        {
            if (!Reel.TryDeferChat(player, message))
            {
                player.PrintToChat(message);
            }
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
