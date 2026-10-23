using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using System.Globalization;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    /// <summary>
    /// A joker card: one extra "!rtd" although the player rolled this round already. The card is kept per SteamID
    /// until the match ends (win panel, map change, new match), so it outlives the round and the dice itself.
    /// </summary>
    public class Joker : DiceBlueprint
    {
        public override string ClassName => "Joker";
        private static readonly Dictionary<ulong, int> _jokers = [];

        public Joker(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            // bots never type !rtd
            if (!_players.Contains(player) || player.IsBot || player.SteamID == 0)
            {
                return;
            }
            int held = Math.Min(_config.Dices.Joker.MaxJokers, _jokers.GetValueOrDefault(player.SteamID) + 1);
            _jokers[player.SteamID] = held;
            ShowStatus(player, "dice_Joker_status", new() { { "jokers", held.ToString(CultureInfo.InvariantCulture) } });
        }

        public static int Count(CCSPlayerController player) => _jokers.GetValueOrDefault(player.SteamID);

        /// <summary>Uses one joker of the player; false when they hold none.</summary>
        public static bool TryUse(CCSPlayerController player)
        {
            if (player.SteamID == 0 || !_jokers.TryGetValue(player.SteamID, out int held) || held <= 0)
            {
                return false;
            }
            if (held == 1)
            {
                _ = _jokers.Remove(player.SteamID);
            }
            else
            {
                _jokers[player.SteamID] = held - 1;
            }
            return true;
        }

        /// <summary>Match over: every joker is gone.</summary>
        public static void ClearAll() => _jokers.Clear();
    }
}

namespace RollTheDice.Configs
{
    public class JokerConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // jokers a player can hold at once (a further joker dice adds nothing beyond this)
        [JsonPropertyName("max_jokers")] public int MaxJokers { get; set; } = 1;
    }
}
