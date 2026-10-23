using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class DoubleJump : DiceBlueprint
    {
        public override string ClassName => "DoubleJump";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerButtonsChanged"];
        // the button callback runs after the engine processed the jump from the ground (the player is already in the
        // air there): a press this soon after the last ground contact is that normal jump, not an extra one
        private const float GroundJumpSeconds = 0.15f;
        // extra jumps used since the last ground contact, time of that contact
        private readonly Dictionary<CCSPlayerController, (int Used, float GroundAt)> _state = [];

        public DoubleJump(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _state[player] = (0, Server.CurrentTime);
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

        /// <summary>Ground contact refills the extra jumps.</summary>
        public void OnTick()
        {
            if (_state.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            foreach ((CCSPlayerController player, (int used, float _)) in _state)
            {
                if (player.IsValid && player.PlayerPawn.Value is { IsValid: true } pawn && Motion.IsOnGround(pawn))
                {
                    _state[player] = (0, now);
                }
            }
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_state.Count == 0
                || (pressed & PlayerButtons.Jump) == 0
                || !_state.TryGetValue(player, out (int Used, float GroundAt) state)
                || state.Used >= _config.Dices.DoubleJump.ExtraJumps
                || Server.CurrentTime - state.GroundAt < GroundJumpSeconds
                || !IsAlivePlayer(player)
                || player.PlayerPawn.Value is not { IsValid: true } pawn
                || Motion.IsOnGround(pawn))
            {
                return;
            }

            _state[player] = (state.Used + 1, state.GroundAt);
            Motion.SetVerticalVelocity(pawn, _config.Dices.DoubleJump.JumpVelocity);
            Debug($"{player.PlayerName} extra jump {state.Used + 1}");
        }
    }
}

namespace RollTheDice.Configs
{
    public class DoubleJumpConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("extra_jumps")] public int ExtraJumps { get; set; } = 1;
        [JsonPropertyName("jump_velocity")] public float JumpVelocity { get; set; } = 320f;
    }
}
