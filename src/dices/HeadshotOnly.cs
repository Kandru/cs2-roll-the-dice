using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class HeadshotOnly : DiceBlueprint
    {
        public override string ClassName => "HeadshotOnly";
        public readonly Random _random = new();
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];
        public readonly List<uint> _playerWithHeadshotOnly = [];

        public HeadshotOnly(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _playerWithHeadshotOnly.Add(player.Pawn.Value.Index);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
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
            _ = _playerWithHeadshotOnly.Remove(player.Pawn.Value.Index);
        }

        public override void Reset()
        {
            _players.Clear();
            _playerWithHeadshotOnly.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            return info.Attacker.Value == null
                || !_playerWithHeadshotOnly.Contains(info.Attacker.Value.Index)
                ? HookResult.Continue
                : info.GetHitGroup() == HitGroup_t.HITGROUP_HEAD ? HookResult.Continue : HookResult.Stop;
        }
    }
}
