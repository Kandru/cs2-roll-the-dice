using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Globalization;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class ThrowingKnives : DiceBlueprint
    {
        public override string ClassName => "ThrowingKnives";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerButtonsChanged"];
        // a knife that hits nothing is gone after this long
        private const float MaxFlightSeconds = 3f;
        private const float SpinDegreesPerSecond = 1440f;
        private static readonly TraceOptions ShotOptions = new() { InteractsWith = Masks.Shot };

        // The knife is simulated here, not by a grenade: a created projectile never thinks (see Explosion). Each tick
        // traces the stretch it flew, so it cannot skip through a player; the prop only shows it.
        private sealed class Knife(CCSPlayerController owner, CDynamicProp prop, Vector position, Vector velocity, float thrownAt)
        {
            public readonly CCSPlayerController Owner = owner;
            public readonly CDynamicProp Prop = prop;
            public Vector Position = position;
            public Vector Velocity = velocity;
            public readonly float ThrownAt = thrownAt;
            public float Pitch;
            // hit a wall or the floor: the prop stays there for a moment
            public bool Stuck;
        }

        private sealed class ThrowerState
        {
            public int Knives;
            public float NextKnifeAt;
        }

        private readonly Dictionary<CCSPlayerController, ThrowerState> _state = [];
        private readonly List<Knife> _flying = [];
        // knives sticking in walls (entity I/O removes them; Reset clears them earlier at round end)
        private readonly List<CDynamicProp> _stuck = [];

        public ThrowingKnives(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _state[player] = new ThrowerState { Knives = _config.Dices.ThrowingKnives.Knives };
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
            foreach (Knife knife in _flying)
            {
                Entities.RemoveEntity(knife.Prop);
            }
            _flying.Clear();
            foreach (CDynamicProp prop in _stuck)
            {
                Entities.RemoveEntity(prop);
            }
            _stuck.Clear();
            _state.Clear();
        }

        /// <summary>USE with the knife drawn throws one.</summary>
        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_state.Count == 0
                || (pressed & PlayerButtons.Use) == 0
                || !_state.TryGetValue(player, out ThrowerState? state)
                || state.Knives <= 0
                || !IsAlivePlayer(player)
                || player.PlayerPawn.Value is not { IsValid: true } pawn
                || pawn.IsDefusing
                || pawn.WeaponServices?.ActiveWeapon.Value is not { IsValid: true } weapon
                || !Weapons.IsKnife(weapon.DesignerName))
            {
                return;
            }

            ThrowingKnivesConfig config = _config.Dices.ThrowingKnives;
            if (_flying.Count(k => k.Owner == player) >= config.Knives)
            {
                return;
            }

            Vector eyes = Targets.Eyes(pawn);
            Vector forward = Entities.GetForwardVector(pawn.EyeAngles);
            Vector start = new(eyes.X + (forward.X * 16f), eyes.Y + (forward.Y * 16f), eyes.Z + (forward.Z * 16f) - 4f);
            string model = Entities.GetModel(weapon);
            if (string.IsNullOrEmpty(model) || CreateProp(start, pawn.EyeAngles, model) is not { } prop)
            {
                return;
            }

            Vector velocity = new(forward.X * config.Speed, forward.Y * config.Speed, forward.Z * config.Speed);
            _flying.Add(new Knife(player, prop, start, velocity, Server.CurrentTime) { Pitch = pawn.EyeAngles.X });
            Debug($"{player.PlayerName} throws a knife (model {model})");
            if (state.Knives == config.Knives)
            {
                state.NextKnifeAt = Server.CurrentTime + config.RechargeSeconds;
            }
            state.Knives--;
            ShowKnives(player, state);
        }

        public void OnTick()
        {
            if (_state.Count == 0 && _flying.Count == 0)
            {
                return;
            }

            ThrowingKnivesConfig config = _config.Dices.ThrowingKnives;
            float now = Server.CurrentTime;
            float dt = Server.TickInterval;
            for (int i = _flying.Count - 1; i >= 0; i--)
            {
                Knife knife = _flying[i];
                if (Fly(knife, config, now, dt))
                {
                    continue;
                }
                _flying.RemoveAt(i);
                if (knife.Stuck)
                {
                    _stuck.Add(knife.Prop);
                    knife.Prop.AddEntityIOEvent("Kill", null, null, "", config.StickSeconds);
                }
                else
                {
                    Entities.RemoveEntity(knife.Prop);
                }
            }

            if (Server.TickCount % 8 != 0)
            {
                return;
            }
            _ = _stuck.RemoveAll(static p => !p.IsValid);
            foreach ((CCSPlayerController player, ThrowerState state) in _state)
            {
                if (state.Knives < config.Knives && now >= state.NextKnifeAt)
                {
                    state.Knives++;
                    state.NextKnifeAt = now + config.RechargeSeconds;
                    ShowKnives(player, state);
                }
            }
        }

        /// <summary>One tick of flight; false when the knife is done (hit, wall, timeout). A wall hit sets Stuck.</summary>
        private bool Fly(Knife knife, ThrowingKnivesConfig config, float now, float dt)
        {
            if (!knife.Prop.IsValid || now - knife.ThrownAt > MaxFlightSeconds)
            {
                return false;
            }

            Vector next = new(
                knife.Position.X + (knife.Velocity.X * dt),
                knife.Position.Y + (knife.Velocity.Y * dt),
                knife.Position.Z + (knife.Velocity.Z * dt));
            // the trace starts inside the knife prop, so it ignores the prop; the thrower is behind the knife from the first
            // tick on (it starts in front of the eyes)
            TraceResult trace = Trace.TraceEndShape(knife.Position, next, knife.Prop, ShotOptions);
            if (trace.DidHit())
            {
                if (trace.HitEntity() is { IsValid: true, DesignerName: "player" } entity
                    && entity.As<CCSPlayerPawn>() is { IsValid: true } victimPawn
                    && victimPawn.Controller.Value?.As<CCSPlayerController>() is { IsValid: true } victim
                    && knife.Owner.IsValid
                    && Targets.IsEnemy(knife.Owner, victim))
                {
                    Damage.Deal(victimPawn, knife.Owner.PlayerPawn.Value, config.Damage, DamageTypes_t.DMG_SLASH);
                    Debug($"{knife.Owner.PlayerName} knife hit {victim.PlayerName}");
                }
                else if (config.StickSeconds > 0f && trace.HitEntity() is not { DesignerName: "player" })
                {
                    // the blade stays in the wall, pointing the way it flew
                    QAngle flight = Vectors.GetLookAtAngle(new Vector(0, 0, 0), knife.Velocity);
                    knife.Prop.Teleport(trace.EndPos, new QAngle(flight.X, flight.Y, 0), new Vector(0, 0, 0));
                    knife.Stuck = true;
                }
                Debug($"knife of {knife.Owner.PlayerName} stopped at {trace.EndPos} on {trace.HitEntity()?.DesignerName}");
                return false;
            }

            knife.Position = next;
            knife.Velocity.Z -= config.Gravity * dt;
            knife.Pitch = (knife.Pitch + (SpinDegreesPerSecond * dt)) % 360f;
            QAngle angles = Vectors.GetLookAtAngle(new Vector(0, 0, 0), knife.Velocity);
            knife.Prop.Teleport(next, new QAngle(knife.Pitch, angles.Y, 0), new Vector(0, 0, 0));
            return true;
        }

        /// <summary>A prop with the thrower's knife model (it is loaded, the knife is in the hand).</summary>
        private static CDynamicProp? CreateProp(Vector position, QAngle angles, string model)
        {
            if (Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override") is not { } prop)
            {
                return null;
            }
            // the model goes in with the spawn (a prop spawned without one warns "has no model name")
            CEntityKeyValues kv = new();
            kv.SetString("model", model);
            prop.DispatchSpawn(kv);
            prop.SetModel(model);
            prop.Teleport(position, angles, new Vector(0, 0, 0));
            return prop;
        }

        private void ShowKnives(CCSPlayerController player, ThrowerState state)
        {
            ShowStatus(player, "dice_ThrowingKnives_status", new()
            {
                { "knives", state.Knives.ToString(CultureInfo.InvariantCulture) },
                { "max", _config.Dices.ThrowingKnives.Knives.ToString(CultureInfo.InvariantCulture) },
            });
        }
    }
}

namespace RollTheDice.Configs
{
    public class ThrowingKnivesConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("knives")] public int Knives { get; set; } = 3;
        // seconds per knife coming back
        [JsonPropertyName("recharge_seconds")] public float RechargeSeconds { get; set; } = 10f;
        [JsonPropertyName("speed")] public float Speed { get; set; } = 1500f;
        // downward pull in units/s² (a player falls with 800)
        [JsonPropertyName("gravity")] public float Gravity { get; set; } = 400f;
        // 500 kills through any armor
        [JsonPropertyName("damage")] public float Damage { get; set; } = 500f;
        // a knife that hits a wall or the floor stays there this long (0 = gone at once)
        [JsonPropertyName("stick_seconds")] public float StickSeconds { get; set; } = 5f;
    }
}
