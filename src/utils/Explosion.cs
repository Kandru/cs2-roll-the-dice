using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils
{
    /// <summary>
    /// An explosion caused by a dice (Martyrdom, cluster grenades, air strikes). A created hegrenade_projectile
    /// never goes off by itself (it has no detonate think), so an env_explosion deals the damage; the grenade only
    /// shows where it will blow. An env_explosion draws no fireball in CS2, so the HE grenade particle effect is started
    /// with it. Both are timed with entity I/O; only the thrower's own damage waits in OnTick, because env_explosion spares
    /// its owner.
    /// </summary>
    public static class Explosion
    {
        /// <summary>The fireball of a real HE grenade (precached in Precache.cs).</summary>
        public const string ParticleEffect = "particles/explosions_fx/explosion_hegrenade.vpcf";
        /// <summary>A smaller blast for frequent explosions (explosive rounds; precached in Precache.cs).</summary>
        public const string SmallParticleEffect = "particles/explosions_fx/explosion_basic.vpcf";
        // the particle system lingers this long after the blast for the smoke
        private const float ParticleSeconds = 5f;
        private const float FloorSearch = 256f;
        // the blast is measured to the middle of the body, not the feet
        private const float BodyHeight = 32f;

        // env_explosion spares its owner; a real HE hurts its thrower, so the thrower's share is dealt here when the
        // blast goes off (OnTick, registered by the plugin)
        private sealed record OwnerHit(Vector Position, CCSPlayerPawn Owner, float At, float Damage, float Radius);

        private static readonly List<OwnerHit> _ownerHits = [];

        /// <summary>Listener OnTick: hurts throwers whose own blast went off near them.</summary>
        public static void OnTick()
        {
            if (_ownerHits.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            for (int i = _ownerHits.Count - 1; i >= 0; i--)
            {
                OwnerHit hit = _ownerHits[i];
                if (now < hit.At)
                {
                    continue;
                }
                _ownerHits.RemoveAt(i);
                HurtOwner(hit);
            }
        }

        /// <summary>Drops pending thrower hits (round start, unload).</summary>
        public static void Clear() => _ownerHits.Clear();

        /// <summary>Linear falloff like the HE grenade; walls between the blast and the thrower stop it.</summary>
        private static void HurtOwner(OwnerHit hit)
        {
            if (!hit.Owner.IsValid || hit.Owner.LifeState != (byte)LifeState_t.LIFE_ALIVE || hit.Owner.AbsOrigin is not { } origin)
            {
                return;
            }

            Vector body = new(origin.X, origin.Y, origin.Z + BodyHeight);
            float distance = Vectors.GetDistance(hit.Position, body);
            if (distance >= hit.Radius)
            {
                return;
            }
            // from a bit above the blast: the marker grenade lying there must not block the line
            Vector from = new(hit.Position.X, hit.Position.Y, hit.Position.Z + 12f);
            TraceResult trace = RayTrace.TraceLine(from, body, hit.Owner);
            if (trace.DidHit() && trace.Fraction < 0.97f)
            {
                return;
            }
            Damage.Deal(hit.Owner, hit.Owner, hit.Damage * (1f - (distance / hit.Radius)), DamageTypes_t.DMG_BLAST);
        }

        /// <summary>
        /// A random floor point within <paramref name="radius"/> around <paramref name="center"/> (cluster bomblets, air
        /// strike hits): the way out stops in front of walls, then the point drops to the floor.
        /// </summary>
        public static Vector Scatter(Vector center, float radius, Random random)
        {
            float angle = (float)(random.NextDouble() * 2 * Math.PI);
            // square root: points spread evenly over the disc instead of crowding the middle
            float distance = radius * MathF.Sqrt((float)random.NextDouble());
            Vector start = new(center.X, center.Y, center.Z + 32f);
            Vector wanted = new(start.X + (MathF.Cos(angle) * distance), start.Y + (MathF.Sin(angle) * distance), start.Z);
            TraceResult across = RayTrace.TraceLine(start, wanted);
            Vector point = across.DidHit() ? Back(start, across.EndPos, 16f) : wanted;
            TraceResult down = RayTrace.TraceLine(point, new Vector(point.X, point.Y, point.Z - FloorSearch));
            return down.DidHit() ? new Vector(down.EndPos.X, down.EndPos.Y, down.EndPos.Z + 8f) : point;
        }

        private static Vector Back(Vector from, Vector to, float gap)
        {
            float length = Vectors.GetDistance(from, to);
            if (length <= gap)
            {
                return from;
            }
            float keep = (length - gap) / length;
            return new Vector(from.X + ((to.X - from.X) * keep), from.Y + ((to.Y - from.Y) * keep), from.Z + ((to.Z - from.Z) * keep));
        }

        /// <summary>
        /// Explodes at <paramref name="position"/> after <paramref name="delay"/> seconds. The attacker gets the kills
        /// (kill feed, score); friendly fire follows the server rules because the attacker is a player.
        /// </summary>
        public static void Detonate(Vector position, CCSPlayerPawn? attacker, float delay, float damage, float radius, bool showGrenade = true, bool hurtsOwner = true, string particle = ParticleEffect)
        {
            CEnvExplosion? explosion = Utilities.CreateEntityByName<CEnvExplosion>("env_explosion");
            if (explosion == null)
            {
                return;
            }

            CCSPlayerPawn? owner = attacker is { IsValid: true } ? attacker : null;
            explosion.Magnitude = (int)MathF.Round(damage);
            explosion.PlayerDamage = damage;
            explosion.RadiusOverride = (int)MathF.Round(radius);
            if (owner != null)
            {
                explosion.Inflictor.Raw = owner.EntityHandle.Raw;
                explosion.OwnerEntity.Raw = owner.EntityHandle.Raw;
                explosion.TeamNum = owner.TeamNum;
            }
            explosion.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            explosion.DispatchSpawn();

            float wait = Math.Max(0f, delay);
            explosion.AddEntityIOEvent("Explode", owner, owner, "", wait);
            explosion.AddEntityIOEvent("Kill", null, null, "", wait + 1f);

            Particle.Start(particle, position, ParticleSeconds, wait);

            if (owner != null && hurtsOwner)
            {
                _ownerHits.Add(new OwnerHit(new Vector(position.X, position.Y, position.Z), owner, Server.CurrentTime + wait, damage, radius));
            }

            if (showGrenade && wait > 0f && Utilities.CreateEntityByName<CHEGrenadeProjectile>("hegrenade_projectile") is { } grenade)
            {
                grenade.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                grenade.DispatchSpawn();
                grenade.AddEntityIOEvent("Kill", null, null, "", wait);
            }
        }
    }
}
