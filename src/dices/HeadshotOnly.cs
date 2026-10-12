using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class HeadshotOnly : DiceBlueprint
    {
        public override string ClassName => "HeadshotOnly";
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
            if (!IsAlivePlayer(player) || player.Pawn?.Value == null)
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
            base.Remove(player, reason);
            if (player.Pawn?.Value is { IsValid: true } pawn)
            {
                _ = _playerWithHeadshotOnly.Remove(pawn.Index);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _playerWithHeadshotOnly.Clear();
        }

        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn player, CTakeDamageInfo info)
        {
            return info.Attacker.Value == null
                || !_playerWithHeadshotOnly.Contains(info.Attacker.Value.Index)
                ? HookResult.Continue
                : info.HitGroupId == HitGroup_t.HITGROUP_HEAD ? HookResult.Continue : HookResult.Stop;
        }
    }
}

namespace RollTheDice.Configs
{
    public class HeadshotOnlyConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    }
}
