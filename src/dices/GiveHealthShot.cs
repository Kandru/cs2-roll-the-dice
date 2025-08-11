using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class GiveHealthShot : ParentDice
    {
        public override string ClassName => "GiveHealthShot";
        public readonly Random _random = new();

        public GiveHealthShot(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // amount of health shots to give
            int amount = _random.Next(
                _config.Dices.GiveHealthShot.MinShots,
                _config.Dices.GiveHealthShot.MaxShots + 1
            );
            for (int i = 0; i < amount; i++)
            {
                player.GiveNamedItem("weapon_healthshot");
            }
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "amount", amount.ToString() }
            });
        }
    }
}
