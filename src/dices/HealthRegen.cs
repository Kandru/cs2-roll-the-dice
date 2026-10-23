using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class HealthRegen : DiceBlueprint
    {
        public override string ClassName => "HealthRegen";
        public override List<string> Events { get; } = ["EventPlayerHurt"];
        public override List<string> Listeners { get; } = ["OnTick"];
        // last damage taken, next regeneration step
        private readonly Dictionary<CCSPlayerController, (float HurtAt, float NextAt)> _state = [];

        public HealthRegen(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _state[player] = (0f, 0f);
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _state.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _state.Clear();
        }

        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            if (_state.Count > 0 && @event.Userid is { IsValid: true } victim && _state.ContainsKey(victim))
            {
                _state[victim] = (Server.CurrentTime, 0f);
            }
            return HookResult.Continue;
        }

        /// <summary>After a while without damage: one health point per step up to the maximum.</summary>
        public void OnTick()
        {
            if (_state.Count == 0)
            {
                return;
            }

            HealthRegenConfig config = _config.Dices.HealthRegen;
            float now = Server.CurrentTime;
            float step = 1f / Math.Max(1, config.HealthPerSecond);
            foreach ((CCSPlayerController player, (float hurtAt, float nextAt)) in _state)
            {
                if (now < nextAt
                    || now - hurtAt < config.DelaySeconds
                    || !IsAlivePlayer(player)
                    || player.PlayerPawn.Value is not { IsValid: true } pawn)
                {
                    continue;
                }
                Damage.Heal(pawn, 1);
                _state[player] = (hurtAt, now + step);
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class HealthRegenConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // seconds without damage before the regeneration starts
        [JsonPropertyName("delay_seconds")] public float DelaySeconds { get; set; } = 4f;
        [JsonPropertyName("health_per_second")] public int HealthPerSecond { get; set; } = 5;
    }
}
