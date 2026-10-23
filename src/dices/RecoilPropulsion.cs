using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class RecoilPropulsion : DiceBlueprint
    {
        public override string ClassName => "RecoilPropulsion";
        public override List<string> Events { get; } = ["EventWeaponFire"];

        private static readonly string[] Shotguns = ["nova", "xm1014", "mag7", "sawedoff"];
        private static readonly string[] Snipers = ["awp", "ssg08", "scar20", "g3sg1", "deagle", "revolver"];

        public RecoilPropulsion(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>Every shot pushes the holder against the view: shooting down lifts them, shotguns push hardest.</summary>
        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (_players.Count == 0
                || player is not { IsValid: true }
                || !_players.Contains(player)
                || !Weapons.IsGun(@event.Weapon))
            {
                return HookResult.Continue;
            }

            float force = Force(@event.Weapon);
            Server.NextFrame(() =>
            {
                if (!Targets.IsAlive(player) || player.PlayerPawn.Value is not { IsValid: true } pawn)
                {
                    return;
                }
                Vector forward = Entities.GetForwardVector(pawn.EyeAngles);
                Vector v = Motion.Velocity(pawn);
                v.X -= forward.X * force;
                v.Y -= forward.Y * force;
                v.Z = Math.Min(_config.Dices.RecoilPropulsion.MaxUpSpeed, v.Z - (forward.Z * force));
                Motion.SetVelocity(pawn, v);
            });
            return HookResult.Continue;
        }

        private float Force(string weapon)
        {
            RecoilPropulsionConfig config = _config.Dices.RecoilPropulsion;
            if (Shotguns.Any(w => weapon.Contains(w, StringComparison.OrdinalIgnoreCase)))
            {
                return config.ShotgunForce;
            }
            return Snipers.Any(w => weapon.Contains(w, StringComparison.OrdinalIgnoreCase)) ? config.SniperForce : config.DefaultForce;
        }
    }
}

namespace RollTheDice.Configs
{
    public class RecoilPropulsionConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // push per shot in units/s against the view
        [JsonPropertyName("shotgun_force")] public float ShotgunForce { get; set; } = 450f;
        // AWP, scout, auto snipers, Deagle, R8
        [JsonPropertyName("sniper_force")] public float SniperForce { get; set; } = 350f;
        // rifles, SMGs, pistols
        [JsonPropertyName("default_force")] public float DefaultForce { get; set; } = 40f;
        // a shot straight down lifts at most this fast (a normal jump is 300)
        [JsonPropertyName("max_up_speed")] public float MaxUpSpeed { get; set; } = 500f;
    }
}
