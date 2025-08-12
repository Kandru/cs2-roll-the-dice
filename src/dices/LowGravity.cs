using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class LowGravity : ParentDice
    {
        public override string ClassName => "LowGravity";
        public readonly Random _random = new();

        public LowGravity(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // set low gravity
            ChangePlayerGravity(player, _config.Dices.LowGravity.GravityScale);
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            ChangePlayerGravity(player, 1f);
            _ = _players.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            foreach (CCSPlayerController player in _players)
            {
                ChangePlayerGravity(player, 1f);
            }
            _players.Clear();
        }

        private void ChangePlayerGravity(CCSPlayerController player, float gravityScale)
        {
            if (player.Pawn?.Value != null && player.Pawn.Value.IsValid)
            {
                player.Pawn.Value.ActualGravityScale = gravityScale;
            }
        }
    }
}
