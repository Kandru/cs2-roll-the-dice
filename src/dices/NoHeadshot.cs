using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class NoHeadshot : DiceBlueprint
    {
        public override string ClassName => "NoHeadshot";
        public readonly Random _random = new();
        public override List<string> Listeners => [
            "OnPlayerTakeDamagePre"
        ];
        public readonly List<uint> _playerWithNoHeadshot = [];

        public NoHeadshot(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _playerWithNoHeadshot.Add(player.Pawn.Value.Index);
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
            _ = _playerWithNoHeadshot.Remove(player.Pawn.Value.Index);
        }

        public override void Reset()
        {
            _players.Clear();
            _playerWithNoHeadshot.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CBaseEntity entity, CTakeDamageInfo info)
        {
            return info.Attacker.Value == null
                || !_playerWithNoHeadshot.Contains(info.Attacker.Value.Index)
                ? HookResult.Continue
                : info.GetHitGroup() == HitGroup_t.HITGROUP_HEAD ? HookResult.Stop : HookResult.Continue;
        }
    }
}
