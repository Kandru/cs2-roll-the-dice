using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class IncreaseHealth : DiceBlueprint
    {
        public override string ClassName => "IncreaseHealth";
        public readonly Random _random = new();

        public IncreaseHealth(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // get random amount of health to increase
            int healthIncrease = _random.Next(
                _config.Dices.IncreaseHealth.MinHealth,
                _config.Dices.IncreaseHealth.MaxHealth + 1
            );
            // increase health
            player.Pawn.Value.Health += healthIncrease;
            player.Pawn.Value.MaxHealth = player.Pawn.Value.Health;
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iMaxHealth");
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "healthIncrease", healthIncrease.ToString() }
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
            if (player?.Pawn?.IsValid == false
                || player?.Pawn?.Value == null)
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
