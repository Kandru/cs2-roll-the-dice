using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Configs;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    /// <summary>
    /// Slippery ice: the holder's ground friction is lowered, so stopping and turning slide on while speeding up works
    /// as usual. Setting the velocity every tick instead does not work: a Teleport takes the pawn off the ground for the
    /// next tick (weak air acceleration, slow to get going), and a velocity written in place is overwritten by the
    /// movement code.
    /// </summary>
    public class IceSkating : DiceBlueprint
    {
        public override string ClassName => "IceSkating";
        public override List<string> Listeners { get; } = ["OnTick"];
        private const float NormalFriction = 1f;

        public IceSkating(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            base.Remove(player, reason);
            SetFriction(player.IsValid ? player.PlayerPawn.Value : null, NormalFriction);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players)
            {
                SetFriction(player.IsValid ? player.PlayerPawn.Value : null, NormalFriction);
            }
            base.Reset();
        }

        /// <summary>Keeps the low friction on the holder's pawn (a respawned pawn starts with the normal one).</summary>
        public void OnTick()
        {
            if (_players.Count == 0)
            {
                return;
            }

            float friction = Math.Clamp(_config.Dices.IceSkating.Friction, 0.01f, 1f);
            foreach (CCSPlayerController player in _players)
            {
                if (IsAlivePlayer(player))
                {
                    SetFriction(player.PlayerPawn.Value, friction);
                }
            }
        }

        private static void SetFriction(CCSPlayerPawn? pawn, float friction)
        {
            if (pawn is not { IsValid: true } || pawn.Friction == friction)
            {
                return;
            }
            pawn.Friction = friction;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_flFriction");
        }
    }
}

namespace RollTheDice.Configs
{
    public class IceSkatingConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // share of the normal ground friction (1.0 = normal ground, lower = slides further)
        [JsonPropertyName("friction")] public float Friction { get; set; } = 0.1f;
    }
}
