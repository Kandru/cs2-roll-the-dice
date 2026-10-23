using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    /// <summary>
    /// A bullet hit of the holder puts a timed effect on the enemy (Incendiary Rounds, Frost Rounds): a new hit starts
    /// the time again, it never stacks. The victim is tinted while it lasts. Effects keep running after the shooter
    /// died; they end on time, on the victim's death and at round end.
    /// </summary>
    public abstract class HitEffect(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : DiceBlueprint(GlobalConfig, Config, Localizer)
    {
        public override List<string> Events { get; } = ["EventPlayerHurt"];
        public override List<string> Listeners { get; } = ["OnTick"];

        protected sealed class Afflicted(CCSPlayerController shooter, float until)
        {
            public CCSPlayerController Shooter = shooter;
            public float Until = until;
            public float NextAt;
        }

        protected readonly Dictionary<CCSPlayerController, Afflicted> _victims = [];

        protected abstract float Seconds { get; }
        protected abstract Color Color { get; }

        /// <summary>Called every tick for every victim while the effect lasts.</summary>
        protected abstract void Apply(CCSPlayerController victim, CCSPlayerPawn pawn, Afflicted effect, float now);

        /// <summary>Called once when the effect ends (time over, death, round end).</summary>
        protected virtual void End(CCSPlayerController victim)
        {
        }

        public override void Reset()
        {
            base.Reset();
            foreach (CCSPlayerController victim in _victims.Keys)
            {
                Finish(victim);
            }
            _victims.Clear();
        }

        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? shooter = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (_players.Count == 0
                || Damage.IsDealing
                || shooter is not { IsValid: true }
                || !_players.Contains(shooter)
                || victim is not { IsValid: true }
                || !Targets.IsEnemy(shooter, victim)
                || !Weapons.IsGun(@event.Weapon)
                || !Targets.IsAlive(victim))
            {
                return HookResult.Continue;
            }

            float now = Server.CurrentTime;
            if (_victims.TryGetValue(victim, out Afflicted? effect))
            {
                effect.Shooter = shooter;
                effect.Until = now + Seconds;
                return HookResult.Continue;
            }
            _victims[victim] = new Afflicted(shooter, now + Seconds) { NextAt = now };
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_victims.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            foreach ((CCSPlayerController victim, Afflicted effect) in _victims)
            {
                if (now >= effect.Until || !Targets.IsAlive(victim) || victim.PlayerPawn.Value is not { IsValid: true } pawn)
                {
                    _ = _victims.Remove(victim);
                    Finish(victim);
                    continue;
                }
                Tint.Set(pawn, Color);
                Apply(victim, pawn, effect, now);
            }
        }

        private void Finish(CCSPlayerController victim)
        {
            if (victim.IsValid)
            {
                Tint.Reset(victim.PlayerPawn.Value);
            }
            End(victim);
        }
    }
}
