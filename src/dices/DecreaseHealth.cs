using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class DecreaseHealth : ParentDice
    {
        public override string ClassName => "DecreaseHealth";
        public readonly Random _random = new();

        public DecreaseHealth(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // get random amount of health to decrease
            int healthDecrease = _random.Next(
                _config.Dices.DecreaseHealth.MinHealth,
                _config.Dices.DecreaseHealth.MaxHealth + 1
            );
            // check if health-decrease should be able to kill the player
            if (_config.Dices.DecreaseHealth.PreventDeath
                && player.Pawn.Value.Health - healthDecrease <= 0)
            {
                healthDecrease = player.Pawn.Value.Health - 1;
            }
            // decrease health
            player.Pawn.Value.Health -= healthDecrease;
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "healthDecrease", healthDecrease.ToString() }
            });
        }
    }
}
