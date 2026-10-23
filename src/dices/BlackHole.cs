using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class BlackHole : DiceBlueprint
    {
        public override string ClassName => "BlackHole";
        public override List<string> Events { get; } = ["EventDecoyStarted"];
        public override List<string> Listeners { get; } = ["OnTick"];
        private static readonly Color Purple = Color.FromArgb(255, 150, 60, 255);
        // the spokes that show the field turn this fast (radians per second)
        private const float Spin = 2.5f;
        private const int SpokeCount = 6;

        private sealed class Hole(int decoyId, Vector center, CCSPlayerController owner, float start, float until)
        {
            public readonly int DecoyId = decoyId;
            public readonly Vector Center = center;
            public readonly CCSPlayerController Owner = owner;
            public readonly float Start = start;
            public readonly float Until = until;
            // turning beams from the middle to the edge of the field
            public readonly List<CBeam> Spokes = [];
            // a beam from the middle to every enemy being pulled
            public readonly Dictionary<CCSPlayerController, CBeam> Pulls = [];
        }

        private readonly List<Hole> _holes = [];

        public BlackHole(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                Weapons.GiveOnce(player, "weapon_decoy");
            }
        }

        public override void Reset()
        {
            base.Reset();
            foreach (Hole hole in _holes)
            {
                RemoveBeams(hole);
            }
            _holes.Clear();
        }

        /// <summary>The holder's decoy turns into a black hole where it starts.</summary>
        public HookResult EventDecoyStarted(EventDecoyStarted @event, GameEventInfo info)
        {
            if (_players.Count > 0 && @event.Userid is { IsValid: true } owner && _players.Contains(owner))
            {
                float now = Server.CurrentTime;
                Hole hole = new(@event.Entityid, new Vector(@event.X, @event.Y, @event.Z + 32f), owner, now, now + _config.Dices.BlackHole.PullSeconds);
                for (int i = 0; i < SpokeCount; i++)
                {
                    if (Beam.Create(hole.Center, Spoke(hole, i, 0f), Purple, 2f) is { } spoke)
                    {
                        hole.Spokes.Add(spoke);
                    }
                }
                _holes.Add(hole);
                Debug($"{owner.PlayerName} black hole");
            }
            return HookResult.Continue;
        }

        /// <summary>
        /// Every second tick enemies in range are pulled to the middle (stronger near it); when the time is up a small
        /// implosion goes off and the decoy is gone.
        /// </summary>
        public void OnTick()
        {
            if (_holes.Count == 0 || Server.TickCount % 2 != 0)
            {
                return;
            }

            BlackHoleConfig config = _config.Dices.BlackHole;
            float now = Server.CurrentTime;
            float step = Server.TickInterval * 2f;
            for (int i = _holes.Count - 1; i >= 0; i--)
            {
                Hole hole = _holes[i];
                if (now >= hole.Until || !hole.Owner.IsValid)
                {
                    _holes.RemoveAt(i);
                    RemoveBeams(hole);
                    CCSPlayerPawn? owner = hole.Owner.IsValid ? hole.Owner.PlayerPawn.Value : null;
                    Explosion.Detonate(hole.Center, owner, 0f, config.ImplosionDamage, config.Radius * 0.5f, showGrenade: false);
                    Entities.RemoveEntity(Utilities.GetEntityFromIndex<CBaseEntity>(hole.DecoyId));
                    continue;
                }

                float turn = (now - hole.Start) * Spin;
                for (int s = 0; s < hole.Spokes.Count; s++)
                {
                    Beam.Move(hole.Spokes[s], hole.Center, Spoke(hole, s, turn));
                }

                HashSet<CCSPlayerController> pulled = [];
                foreach ((CCSPlayerController player, CCSPlayerPawn pawn, float distance) in Targets.Around(hole.Center, config.Radius))
                {
                    if (!Targets.IsEnemy(hole.Owner, player) || distance < 24f || pawn.AbsOrigin is not { } origin)
                    {
                        continue;
                    }
                    float strength = config.PullSpeed * (1f - (distance / config.Radius) * 0.5f) * step * 4f;
                    Vector v = Motion.Velocity(pawn);
                    v.X += (hole.Center.X - origin.X) / distance * strength;
                    v.Y += (hole.Center.Y - origin.Y) / distance * strength;
                    v.Z += (hole.Center.Z - origin.Z) / distance * strength;
                    // a small lift keeps ground friction from eating the pull
                    if (Motion.IsOnGround(pawn))
                    {
                        v.Z = Math.Max(v.Z, 60f);
                    }
                    float speed = MathF.Sqrt((v.X * v.X) + (v.Y * v.Y));
                    if (speed > config.PullSpeed)
                    {
                        v.X *= config.PullSpeed / speed;
                        v.Y *= config.PullSpeed / speed;
                    }
                    Motion.SetVelocity(pawn, v);

                    _ = pulled.Add(player);
                    Vector body = new(origin.X, origin.Y, origin.Z + 40f);
                    if (hole.Pulls.TryGetValue(player, out CBeam? beam) && beam.IsValid)
                    {
                        Beam.Move(beam, hole.Center, body);
                    }
                    else if (Beam.Create(hole.Center, body, Purple, 3f) is { } created)
                    {
                        hole.Pulls[player] = created;
                    }
                }

                // enemies that got out (or died) lose their beam
                foreach (CCSPlayerController gone in hole.Pulls.Keys.Where(p => !pulled.Contains(p)).ToList())
                {
                    Entities.RemoveEntity(hole.Pulls[gone]);
                    _ = hole.Pulls.Remove(gone);
                }
            }
        }

        /// <summary>The outer end of spoke <paramref name="index"/>, turned by <paramref name="turn"/> radians.</summary>
        private Vector Spoke(Hole hole, int index, float turn)
        {
            float angle = turn + (index * 2f * MathF.PI / SpokeCount);
            float radius = _config.Dices.BlackHole.Radius;
            return new Vector(hole.Center.X + (MathF.Cos(angle) * radius), hole.Center.Y + (MathF.Sin(angle) * radius), hole.Center.Z);
        }

        private static void RemoveBeams(Hole hole)
        {
            foreach (CBeam beam in hole.Spokes.Concat(hole.Pulls.Values))
            {
                if (beam.IsValid)
                {
                    Entities.RemoveEntity(beam);
                }
            }
            hole.Spokes.Clear();
            hole.Pulls.Clear();
        }
    }
}

namespace RollTheDice.Configs
{
    public class BlackHoleConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("radius")] public float Radius { get; set; } = 400f;
        [JsonPropertyName("pull_seconds")] public float PullSeconds { get; set; } = 3f;
        // highest speed towards the middle in units/s
        [JsonPropertyName("pull_speed")] public float PullSpeed { get; set; } = 320f;
        [JsonPropertyName("implosion_damage")] public float ImplosionDamage { get; set; } = 25f;
    }
}
