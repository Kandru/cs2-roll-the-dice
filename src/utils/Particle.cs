using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Utils
{
    /// <summary>Particle effects started by a dice (explosions, confetti, smoke clouds). Effects must be precached (Precache.cs).</summary>
    public static class Particle
    {
        // default time between stopping an effect and removing its entity
        public const float FadeSeconds = 3f;

        /// <summary>
        /// The particle smoke cloud of the smoke grenade. A real smoke cannot be spawned: a created smokegrenade_projectile
        /// never thinks, and one forced into its smoke state crashed the server. This cloud only hides, it does not count
        /// as a smoke for the game (bots and the smoke checks see through it). The look of the CS2 smoke
        /// (explosion_smokegrenade_adaptive) leaves a green wireframe box on the clients, the other smoke effects show
        /// nothing or a flat red cloud.
        /// </summary>
        public const string Smoke = "particles/explosions_fx/explosion_smokegrenade_fallback.vpcf";

        /// <summary>
        /// Starts <paramref name="effect"/> at <paramref name="position"/> after <paramref name="delay"/> seconds, stops it
        /// <paramref name="seconds"/> later and removes it after <paramref name="fadeSeconds"/> more (entity I/O, no timer).
        /// </summary>
        public static void Start(string effect, Vector position, float seconds, float delay = 0f, float fadeSeconds = FadeSeconds)
        {
            if (Utilities.CreateEntityByName<CParticleSystem>("info_particle_system") is not { } fx)
            {
                return;
            }
            fx.EffectName = effect;
            // started by input even without a delay: the way the explosion fireballs were seen working in game
            fx.StartActive = false;
            fx.Teleport(position, new QAngle(0, 0, 0), new Vector(0, 0, 0));
            fx.DispatchSpawn();
            float wait = Math.Max(0f, delay);
            fx.AddEntityIOEvent("Start", null, null, "", wait);
            // stopped first so it fades out; removing a running smoke left its volume box drawn on the clients
            fx.AddEntityIOEvent("Stop", null, null, "", wait + seconds);
            fx.AddEntityIOEvent("Kill", null, null, "", wait + seconds + Math.Max(0f, fadeSeconds));
        }
    }
}
