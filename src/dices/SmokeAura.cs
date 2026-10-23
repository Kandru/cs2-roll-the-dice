using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    /// <summary>
    /// Smokes of the holder act on the players standing in them once per second (Healing Smoke, Toxic Smoke). A smoke
    /// keeps working after the thrower died; it ends when it fades, at round end or when the thrower leaves.
    /// </summary>
    public abstract class SmokeAura(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : DiceBlueprint(GlobalConfig, Config, Localizer)
    {
        public override List<string> Events { get; } = ["EventSmokegrenadeDetonate", "EventSmokegrenadeExpired"];
        public override List<string> Listeners { get; } = ["OnTick"];
        // a CS2 smoke lasts about 18 s; this ends an aura whose expiry event never came
        private const float MaxSmokeSeconds = 22f;
        // the smoke cloud sits above the point it went off
        private const float CloudHeight = 40f;

        private sealed class Smoke(int entityId, CCSPlayerController owner, Vector center, float until)
        {
            public readonly int EntityId = entityId;
            public readonly CCSPlayerController Owner = owner;
            public readonly Vector Center = center;
            public readonly float Until = until;
            public float NextAt;
        }

        private readonly List<Smoke> _smokes = [];

        protected abstract float Radius { get; }

        /// <summary>Called once per second for every living player inside one of the owner's smokes.</summary>
        protected abstract void Apply(CCSPlayerController owner, CCSPlayerController player, CCSPlayerPawn pawn);

        /// <summary>The holder gets a smoke to throw (unless one is carried already).</summary>
        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                Weapons.GiveOnce(player, "weapon_smokegrenade");
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            if (reason == DiceRemoveReason.Disconnect)
            {
                _ = _smokes.RemoveAll(s => s.Owner == player);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _smokes.Clear();
        }

        public HookResult EventSmokegrenadeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
        {
            if (_players.Count > 0 && @event.Userid is { IsValid: true } owner && _players.Contains(owner))
            {
                float now = Server.CurrentTime;
                _smokes.Add(new Smoke(@event.Entityid, owner, new Vector(@event.X, @event.Y, @event.Z + CloudHeight), now + MaxSmokeSeconds)
                {
                    NextAt = now + 1f,
                });
            }
            return HookResult.Continue;
        }

        public HookResult EventSmokegrenadeExpired(EventSmokegrenadeExpired @event, GameEventInfo info)
        {
            if (_smokes.Count > 0)
            {
                _ = _smokes.RemoveAll(s => s.EntityId == @event.Entityid);
            }
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_smokes.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            for (int i = _smokes.Count - 1; i >= 0; i--)
            {
                Smoke smoke = _smokes[i];
                if (now > smoke.Until || !smoke.Owner.IsValid)
                {
                    _smokes.RemoveAt(i);
                    continue;
                }
                if (now < smoke.NextAt)
                {
                    continue;
                }

                smoke.NextAt += 1f;
                foreach ((CCSPlayerController player, CCSPlayerPawn pawn, float distance) in Targets.Around(smoke.Center, Radius))
                {
                    Debug($"smoke of {smoke.Owner.PlayerName}: {player.PlayerName} inside ({distance:0}), hp {pawn.Health}");
                    Apply(smoke.Owner, player, pawn);
                }
            }
        }
    }
}
