using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class NoHeadshot : DiceBlueprint
    {
        public override string ClassName => "NoHeadshot";
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
            if (!IsAlivePlayer(player) || player.Pawn?.Value == null)
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
            base.Remove(player, reason);
            if (player.Pawn?.Value is { IsValid: true } pawn)
            {
                _ = _playerWithNoHeadshot.Remove(pawn.Index);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _playerWithNoHeadshot.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
        {
            return info.Attacker.Value == null
                || !_playerWithNoHeadshot.Contains(info.Attacker.Value.Index)
                ? HookResult.Continue
                : info.HitGroupId == HitGroup_t.HITGROUP_HEAD ? HookResult.Stop : HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class NoHeadshotConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
