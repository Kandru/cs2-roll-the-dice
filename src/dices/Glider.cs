using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Glider : DiceBlueprint
    {
        public override string ClassName => "Glider";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerTakeDamagePre"];
        // fall damage is blocked this long after the last gliding tick
        private const float FallGraceSeconds = 0.5f;
        private readonly Dictionary<CCSPlayerController, float> _glidedAt = [];

        public Glider(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _glidedAt.Remove(player);
        }

        public override void Reset()
        {
            base.Reset();
            _glidedAt.Clear();
        }

        public void OnTick()
        {
            if (_players.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            float fall = _config.Dices.Glider.GlideFallSpeed;
            float drift = _config.Dices.Glider.MinForwardSpeed;
            foreach (CCSPlayerController player in _players)
            {
                if ((player.Buttons & PlayerButtons.Use) == 0
                    || !IsAlivePlayer(player)
                    || player.PlayerPawn.Value is not { IsValid: true } pawn
                    || Motion.IsOnGround(pawn))
                {
                    continue;
                }

                _glidedAt[player] = now;
                Vector v = Motion.Velocity(pawn);
                bool changed = false;
                if (v.Z < -fall)
                {
                    v.Z = -fall;
                    changed = true;
                }
                // a little drift along the view, so gliding carries the player somewhere
                if (drift > 0f && MathF.Sqrt((v.X * v.X) + (v.Y * v.Y)) < drift)
                {
                    float yaw = pawn.EyeAngles.Y * MathF.PI / 180f;
                    v.X = MathF.Cos(yaw) * drift;
                    v.Y = MathF.Sin(yaw) * drift;
                    changed = true;
                }
                if (changed)
                {
                    Motion.SetVelocity(pawn, v);
                }
            }
        }

        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
        {
            if (_glidedAt.Count == 0
                || (info.BitsDamageType & DamageTypes_t.DMG_FALL) == 0
                || HolderOf(pawn) is not { } player
                || !_glidedAt.TryGetValue(player, out float at)
                || Server.CurrentTime - at > FallGraceSeconds)
            {
                return HookResult.Continue;
            }
            return HookResult.Stop;
        }
    }
}

namespace RollTheDice.Configs
{
    public class GliderConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // falling speed while USE is held in the air (units per second)
        [JsonPropertyName("glide_fall_speed")] public float GlideFallSpeed { get; set; } = 80f;
        // horizontal speed the glide keeps at least (0 = no drift)
        [JsonPropertyName("min_forward_speed")] public float MinForwardSpeed { get; set; } = 150f;
    }
}
