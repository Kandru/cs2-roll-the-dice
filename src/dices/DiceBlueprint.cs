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

        /// <summary>The holder whose pawn this is: damage hooks get pawns, not controllers. Null for everyone else.</summary>
        protected CCSPlayerController? HolderOf(CEntityInstance? pawn)
        {
            if (pawn is not { IsValid: true } || _players.Count == 0)
            {
                return null;
            }

            uint index = pawn.Index;
            foreach (CCSPlayerController player in _players)
            {
                if (player.IsValid && player.PlayerPawn.Index == index)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>Banner line in the player's language, without a chat message (live states: fuel, distance).</summary>
        protected void ShowStatus(CCSPlayerController player, string key, Dictionary<string, string>? data = null)
        {
            string text = _localizer.ForPlayerSafe(player, key);
            Banner.Show(player, data == null ? text : Format(text, data));
        }

        /// <summary>Console line when the config enables "debug".</summary>
        protected void Debug(string message)
        {
            if (_globalConfig.Debug)
            {
                Console.WriteLine($"[RollTheDice][{ClassName}] {message}");
            }
        }

        /// <summary>Short center alert in the player's language for a one-off event (a jam, a dropped gun).</summary>
        protected void Alert(CCSPlayerController player, string key, Dictionary<string, string>? data = null)
        {
            if (!player.IsValid || player.IsBot)
            {
                return;
            }
            string text = Banner.StripColors(_localizer.ForPlayerSafe(player, key));
            player.PrintToCenterAlert(data == null ? text : Format(text, data));
        }

        /// <summary>Chat line with the plugin prefix in the player's language (not held back by the reel).</summary>
        protected void Tell(CCSPlayerController player, string key, Dictionary<string, string>? data = null)
        {
            if (!player.IsValid || player.IsBot)
            {
                return;
            }
            string text = _localizer.ForPlayerSafe(player, key);
            player.PrintToChat(_localizer.ForPlayerSafe(player, "command.prefix") + (data == null ? text : Format(text, data)));
        }

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

        protected static string Format(string message, Dictionary<string, string> data)
        {
            foreach ((string key, string value) in data)
            {
                message = message.Replace($"{{{key}}}", value);
            }
            return message;
        }
    }
}
