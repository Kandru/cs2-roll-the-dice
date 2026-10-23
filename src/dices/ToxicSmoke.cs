using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class ToxicSmoke : SmokeAura
    {
        public override string ClassName => "ToxicSmoke";
        protected override float Radius => _config.Dices.ToxicSmoke.Radius;

        public ToxicSmoke(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>
        /// Enemies inside the smoke take damage (the thrower gets the kill and sees who is hit), and so does the thrower:
        /// standing in your own poison hurts, dealt by the world so it counts like fall damage. Teammates are spared.
        /// Generic damage like the Voodoo doll, which is known to land; DMG_POISON belongs to the engine's time-based
        /// damage types.
        /// </summary>
        protected override void Apply(CCSPlayerController owner, CCSPlayerController player, CCSPlayerPawn pawn)
        {
            float damage = _config.Dices.ToxicSmoke.DamagePerSecond;
            if (player == owner)
            {
                if (_config.Dices.ToxicSmoke.HurtsThrower)
                {
                    Damage.Deal(pawn, null, damage);
                }
                return;
            }
            if (!Targets.IsEnemy(owner, player))
            {
                return;
            }
            Damage.Deal(pawn, owner.PlayerPawn.Value, damage);
            ShowStatus(owner, "dice_ToxicSmoke_hit", new() { { "playerName", player.PlayerName } });
        }
    }
}

namespace RollTheDice.Configs
{
    public class ToxicSmokeConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("damage_per_second")] public float DamagePerSecond { get; set; } = 15f;
        // the thrower takes the same damage in their own smoke
        [JsonPropertyName("hurts_thrower")] public bool HurtsThrower { get; set; } = true;
        // distance from the smoke centre in units (a smoke cloud is about 150 wide)
        [JsonPropertyName("radius")] public float Radius { get; set; } = 160f;
    }
}
