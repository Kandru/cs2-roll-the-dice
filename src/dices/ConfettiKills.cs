using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Hud;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class ConfettiKills : DiceBlueprint
    {
        public override string ClassName => "ConfettiKills";
        public override List<string> Events { get; } = ["EventPlayerDeath"];
        /// <summary>The confetti of the de_overpass balloons (precached in Precache.cs).</summary>
        public const string ParticleEffect = "particles/maps/de_overpass/balloon_impact_cofetti.vpcf";
        private const float ParticleSeconds = 3f;
        // bursts follow each other this fast and lie this far around the body
        private const float BurstGap = 0.08f;
        private const float BurstSpread = 28f;

        private static float Spread() => (float)((Random.Shared.NextDouble() * 2d) - 1d) * BurstSpread;

        public ConfettiKills(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>Victims of the holder burst into confetti with a pop everyone near hears (cosmetic).</summary>
        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? killer = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (_players.Count == 0
                || killer is not { IsValid: true }
                || !_players.Contains(killer)
                || victim is not { IsValid: true }
                || victim == killer
                || victim.PlayerPawn.Value is not { IsValid: true, AbsOrigin: { } origin })
            {
                return HookResult.Continue;
            }

            ConfettiKillsConfig config = _config.Dices.ConfettiKills;
            Vector position = new(origin.X, origin.Y, origin.Z + 40f);
            // one balloon's confetti is little: several bursts around the body, a little apart in time
            for (int i = 0; i < Math.Max(1, config.Bursts); i++)
            {
                Vector at = i == 0
                    ? position
                    : new Vector(position.X + Spread(), position.Y + Spread(), position.Z + Spread());
                Particle.Start(ParticleEffect, at, ParticleSeconds, delay: i * BurstGap);
            }
            if (!string.IsNullOrEmpty(config.Sound))
            {
                foreach ((CCSPlayerController listener, CCSPlayerPawn _, float _) in Targets.Around(position, config.SoundRange))
                {
                    if (!listener.IsBot)
                    {
                        Reel.PlaySound(listener, config.Sound);
                    }
                }
            }
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class ConfettiKillsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // confetti bursts per kill
        [JsonPropertyName("bursts")] public int Bursts { get; set; } = 8;
        // heard by living players this close to the victim (sound event or sounds/... path, empty = off)
        [JsonPropertyName("sound")] public string Sound { get; set; } = "sounds/physics/plastic/balloon_pop_01.vsnd";
        [JsonPropertyName("sound_range")] public float SoundRange { get; set; } = 1500f;
    }
}
