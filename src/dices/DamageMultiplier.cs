using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

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
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
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
            _ = _players.Remove(player);
            if (player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _ = _playerMultipliers.Remove(player.Pawn.Value.Index);
        }

        public override void Reset()
        {
            _players.Clear();
            _playerMultipliers.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            if (info.Attacker.Value == null
                || !_playerMultipliers.ContainsKey(info.Attacker.Value.Index))
            {
                return HookResult.Continue;
            }
            if (_playerMultipliers.TryGetValue(info.Attacker.Value.Index, out float multiplier))
            {
                info.Damage *= multiplier;
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }
    }
}
