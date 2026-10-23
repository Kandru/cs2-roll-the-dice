using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Drawing;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class PlayDead : DiceBlueprint
    {
        public override string ClassName => "PlayDead";
        public override List<string> Listeners { get; } = ["OnTick", "OnPlayerTakeDamagePre"];

        private sealed class DeadState
        {
            public bool Used;
            public float Until;
        }

        private readonly Dictionary<CCSPlayerController, DeadState> _state = [];

        public PlayDead(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _state[player] = new DeadState();
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            if (_state.Remove(player, out DeadState? state) && state.Until > 0f)
            {
                Show(player);
            }
        }

        public override void Reset()
        {
            foreach ((CCSPlayerController player, DeadState state) in _state)
            {
                if (state.Until > 0f)
                {
                    Show(player);
                }
            }
            _state.Clear();
            base.Reset();
        }

        /// <summary>
        /// The first hit that leaves the holder below the threshold fakes their death: a kill feed line for every
        /// client (sent to the clients only, so no plugin counts it), then a few seconds invisible, faster and nearly
        /// immune.
        /// </summary>
        public HookResult OnPlayerTakeDamagePre(CCSPlayerPawn pawn, CTakeDamageInfo info)
        {
            if (_state.Count == 0
                || HolderOf(pawn) is not { } player
                || !_state.TryGetValue(player, out DeadState? state))
            {
                return HookResult.Continue;
            }

            PlayDeadConfig config = _config.Dices.PlayDead;
            float now = Server.CurrentTime;
            if (now < state.Until)
            {
                info.Damage *= 1f - config.DamageReduction;
                return HookResult.Changed;
            }
            if (state.Used || pawn.Health - info.Damage >= config.HealthThreshold || pawn.Health - info.Damage <= 0f)
            {
                return HookResult.Continue;
            }

            state.Used = true;
            state.Until = now + config.Seconds;
            CCSPlayerController? attacker = info.Attacker.Value is { IsValid: true, DesignerName: "player" } a
                ? a.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>()
                : null;
            string weapon = attacker?.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value?.DesignerName.Replace("weapon_", string.Empty, StringComparison.Ordinal) ?? "world";
            Server.NextFrame(() =>
            {
                FakeKill(player, attacker, weapon);
                // a puff of smoke where the "body" went, to sell the disappearance
                if (pawn.IsValid && pawn.AbsOrigin is { } origin && config.SmokeSeconds > 0f)
                {
                    // a short puff: most of the time it fades, so it is gone after SmokeSeconds
                    float fade = Math.Min(1.5f, config.SmokeSeconds);
                    Particle.Start(Particle.Smoke, new Vector(origin.X, origin.Y, origin.Z + 4f), config.SmokeSeconds - fade, fadeSeconds: fade);
                }
                Hide(player);
                Alert(player, "dice_PlayDead_faked");
            });
            Debug($"{player.PlayerName} plays dead");
            return HookResult.Continue;
        }

        /// <summary>While playing dead: invisible and faster; afterwards back to normal.</summary>
        public void OnTick()
        {
            if (_state.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            foreach ((CCSPlayerController player, DeadState state) in _state)
            {
                if (state.Until <= 0f || !IsAlivePlayer(player) || player.PlayerPawn.Value is not { IsValid: true } pawn)
                {
                    continue;
                }
                if (now >= state.Until)
                {
                    state.Until = 0f;
                    Show(player);
                    continue;
                }
                float speed = _config.Dices.PlayDead.SpeedFactor;
                if (pawn.VelocityModifier < speed)
                {
                    pawn.VelocityModifier = speed;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            }
        }

        private static void FakeKill(CCSPlayerController victim, CCSPlayerController? attacker, string weapon)
        {
            if (!victim.IsValid)
            {
                return;
            }
            EventPlayerDeath death = new(true)
            {
                Userid = victim,
                Attacker = attacker is { IsValid: true } ? attacker : victim,
                Weapon = weapon,
            };
            foreach (CCSPlayerController viewer in Utilities.GetPlayers())
            {
                if (viewer.IsValid && !viewer.IsBot)
                {
                    death.FireEventToClient(viewer);
                }
            }
        }

        private static void Hide(CCSPlayerController player) => SetAlpha(player, 0);

        private static void Show(CCSPlayerController player)
        {
            SetAlpha(player, 255);
            if (player.IsValid && player.PlayerPawn.Value is { IsValid: true } pawn && pawn.VelocityModifier > 1f)
            {
                pawn.VelocityModifier = 1f;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
            }
        }

        /// <summary>The model and every carried weapon (a floating gun would give the trick away).</summary>
        private static void SetAlpha(CCSPlayerController player, int alpha)
        {
            if (!player.IsValid || player.PlayerPawn.Value is not { IsValid: true } pawn)
            {
                return;
            }
            pawn.Render = Color.FromArgb(alpha, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            if (pawn.WeaponServices?.MyWeapons is not { } weapons)
            {
                return;
            }
            foreach (var handle in weapons)
            {
                if (handle.Value is { IsValid: true } weapon)
                {
                    weapon.Render = Color.FromArgb(alpha, 255, 255, 255);
                    Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
                }
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class PlayDeadConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // the first hit that leaves less health than this fakes the death (once per round)
        [JsonPropertyName("health_threshold")] public int HealthThreshold { get; set; } = 50;
        [JsonPropertyName("seconds")] public float Seconds { get; set; } = 3f;
        // share of the damage taken away while playing dead (0.9 = 90 % less)
        [JsonPropertyName("damage_reduction")] public float DamageReduction { get; set; } = 0.9f;
        [JsonPropertyName("speed_factor")] public float SpeedFactor { get; set; } = 1.2f;
        // how long the smoke puff where the player "died" stays (0 = none)
        [JsonPropertyName("smoke_seconds")] public float SmokeSeconds { get; set; } = 2.5f;
    }
}
