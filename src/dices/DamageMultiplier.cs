using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class DamageMultiplier : DiceBlueprint
    {
        public override string ClassName => "DamageMultiplier";
        public readonly Random _random = new();
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];
        public readonly Dictionary<uint, float> _playerMultipliers = [];

        public DamageMultiplier(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            if (!IsAlivePlayer(player) || player.Pawn?.Value == null)
            {
                return;
            }
            _players.Add(player);
            float min = _config.Dices.DamageMultiplier.MinMultiplier;
            float max = _config.Dices.DamageMultiplier.MaxMultiPlier;
            float multiplier = (float)Math.Round((_random.NextDouble() * (max - min)) + min, 2);
            _playerMultipliers.Add(player.Pawn.Value.Index, multiplier);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "multiplier", multiplier.ToString() }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            if (player.Pawn?.Value is { IsValid: true } pawn)
            {
                _ = _playerMultipliers.Remove(pawn.Index);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _playerMultipliers.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
        {
            if (info.Attacker.Value == null
                || !_playerMultipliers.TryGetValue(info.Attacker.Value.Index, out float multiplier))
            {
                return HookResult.Continue;
            }
            info.Damage *= multiplier;
            return HookResult.Changed;
        }
    }
}

namespace RollTheDice.Configs
{
    public class DamageMultiplierConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("min_multiplier")] public float MinMultiplier { get; set; } = 0.5f;
        [JsonPropertyName("max_multiplier")] public float MaxMultiPlier { get; set; } = 2.0f;
    }
}
