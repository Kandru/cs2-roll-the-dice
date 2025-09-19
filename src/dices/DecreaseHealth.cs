using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class DecreaseHealth : DiceBlueprint
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
            ChangeHealth(player, player.Pawn.Value.Health - healthDecrease);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "healthDecrease", healthDecrease.ToString() }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            ChangeHealth(player, 100);
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

        private static void ChangeHealth(CCSPlayerController? player, int health)
        {
            if (player?.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            player.Pawn.Value.Health = health;
            player.Pawn.Value.MaxHealth = health;
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iMaxHealth");
        }
    }
}
