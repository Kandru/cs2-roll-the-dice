using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class HighGravity : DiceBlueprint
    {
        public override string ClassName => "HighGravity";
        public readonly Random _random = new();

        public HighGravity(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // set high gravity
            ChangePlayerGravity(player, _config.Dices.HighGravity.GravityScale);
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            ChangePlayerGravity(player, 1f);
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        private static void ChangePlayerGravity(CCSPlayerController? player, float gravityScale)
        {
            if (player?.Pawn?.Value != null
                && player.Pawn.Value.IsValid)
            {
                player.Pawn.Value.ActualGravityScale = gravityScale;
            }
        }
    }
}
