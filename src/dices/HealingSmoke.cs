using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class HealingSmoke : SmokeAura
    {
        public override string ClassName => "HealingSmoke";
        protected override float Radius => _config.Dices.HealingSmoke.Radius;

        public HealingSmoke(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>Teammates (and the thrower) heal inside the smoke.</summary>
        protected override void Apply(CCSPlayerController owner, CCSPlayerController player, CCSPlayerPawn pawn)
        {
            if (!Targets.IsEnemy(owner, player))
            {
                Damage.Heal(pawn, _config.Dices.HealingSmoke.HealPerSecond);
            }
        }
    }
}

namespace RollTheDice.Configs
{
    public class HealingSmokeConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("heal_per_second")] public int HealPerSecond { get; set; } = 5;
        // distance from the smoke centre in units (a smoke cloud is about 150 wide)
        [JsonPropertyName("radius")] public float Radius { get; set; } = 160f;
    }
}
