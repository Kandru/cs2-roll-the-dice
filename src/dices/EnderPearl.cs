using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class EnderPearl : DiceBlueprint
    {
        public override string ClassName => "EnderPearl";
        public override List<string> Listeners { get; } = ["OnEntitySpawned", "OnTick"];
        // a pearl that never hits anything is dropped after this long
        private const float MaxFlightSeconds = 5f;
        private readonly Dictionary<CCSPlayerController, int> _throwsLeft = [];
        private readonly List<(CDecoyProjectile Pearl, CCSPlayerController Owner, float ThrownAt)> _flying = [];

        public EnderPearl(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            base.Add(player);
            if (_players.Contains(player))
            {
                _throwsLeft[player] = _config.Dices.EnderPearl.Throws;
                Weapons.GiveOnce(player, "weapon_decoy");
            }
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            _ = _throwsLeft.Remove(player);
            _ = _flying.RemoveAll(f => f.Owner == player);
        }

        public override void Reset()
        {
            base.Reset();
            _throwsLeft.Clear();
            _flying.Clear();
        }

        public void OnEntitySpawned(CEntityInstance entity)
        {
            if (_throwsLeft.Count == 0 || entity.DesignerName != "decoy_projectile")
            {
                return;
            }

            // the thrower is set after the spawn: read it one frame later
            CDecoyProjectile pearl = entity.As<CDecoyProjectile>();
            Server.NextFrame(() =>
            {
                CCSPlayerPawn? thrower = pearl.IsValid ? pearl.OriginalThrower.Value ?? pearl.Thrower.Value : null;
                if (thrower?.Controller.Value?.As<CCSPlayerController>() is not { IsValid: true } owner
                    || !_throwsLeft.TryGetValue(owner, out int left)
                    || left <= 0)
                {
                    return;
                }
                _flying.Add((pearl, owner, Server.CurrentTime));
                Debug($"pearl of {owner.PlayerName} tracked");
            });
        }

        /// <summary>First contact of a pearl: the thrower lands there (if a player fits), the decoy is gone.</summary>
        public void OnTick()
        {
            if (_flying.Count == 0)
            {
                return;
            }

            float now = Server.CurrentTime;
            for (int i = _flying.Count - 1; i >= 0; i--)
            {
                (CDecoyProjectile pearl, CCSPlayerController owner, float thrownAt) = _flying[i];
                if (!pearl.IsValid)
                {
                    _flying.RemoveAt(i);
                    continue;
                }
                bool expired = now - thrownAt > MaxFlightSeconds;
                if (pearl.Bounces == 0 && !expired)
                {
                    continue;
                }

                _flying.RemoveAt(i);
                if (!expired)
                {
                    Land(owner, pearl);
                }
                Entities.RemoveEntity(pearl);
            }
        }

        private void Land(CCSPlayerController owner, CDecoyProjectile pearl)
        {
            if (!IsAlivePlayer(owner)
                || owner.PlayerPawn.Value is not { IsValid: true } pawn
                || pearl.AbsOrigin is not { } target
                || !_throwsLeft.TryGetValue(owner, out int left))
            {
                return;
            }

            if (!SafeTeleport.TryTeleport(pawn, target, pearl.LastHitSurfaceNormal))
            {
                Debug($"pearl of {owner.PlayerName} blocked at {target}: {SafeTeleport.LastRefusal}");
                Alert(owner, "dice_EnderPearl_blocked");
                // the throw is not used up: the pearl comes back
                Weapons.GiveOnce(owner, "weapon_decoy");
                return;
            }
            Debug($"{owner.PlayerName} teleported to {target}");

            int cost = _config.Dices.EnderPearl.HealthCost;
            if (cost > 0 && pawn.Health > 1)
            {
                pawn.Health = Math.Max(1, pawn.Health - cost);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }

            _throwsLeft[owner] = --left;
            if (left > 0)
            {
                Weapons.GiveOnce(owner, "weapon_decoy");
            }
            ShowStatus(owner, "dice_EnderPearl_status", new() { { "throws", left.ToString() } });
        }
    }
}

namespace RollTheDice.Configs
{
    public class EnderPearlConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // teleporting decoys per round (a new one is given after each teleport)
        [JsonPropertyName("throws")] public int Throws { get; set; } = 2;
        [JsonPropertyName("health_cost")] public int HealthCost { get; set; } = 5;
    }
}
