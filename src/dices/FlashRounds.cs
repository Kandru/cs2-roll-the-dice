using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class FlashRounds : DiceBlueprint
    {
        public override string ClassName => "FlashRounds";
        public override List<string> Events { get; } = ["EventPlayerHurt"];
        private readonly Random _random = new();

        public FlashRounds(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>A hit can blind the victim briefly; an already blinded victim is not blinded again.</summary>
        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            FlashRoundsConfig config = _config.Dices.FlashRounds;
            if (_players.Count == 0
                || attacker is not { IsValid: true }
                || victim is not { IsValid: true }
                || attacker == victim
                || !_players.Contains(attacker)
                || !Targets.IsEnemy(attacker, victim)
                || victim.PlayerPawn.Value is not { IsValid: true } pawn
                || pawn.BlindUntilTime > Server.CurrentTime
                || _random.NextDouble() >= config.Chance)
            {
                return HookResult.Continue;
            }

            Blind(pawn, config.BlindSeconds);
            return HookResult.Continue;
        }

        /// <summary>Same fields a flashbang sets (as in WeirdGrenades): the client draws the white screen from them.</summary>
        internal static void Blind(CCSPlayerPawn pawn, float seconds)
        {
            float now = Server.CurrentTime;
            pawn.FlashMaxAlpha = 255f;
            pawn.FlashDuration = seconds;
            pawn.BlindStartTime = now;
            pawn.BlindUntilTime = now + seconds;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flFlashMaxAlpha");
            Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_flFlashDuration");
        }
    }
}

namespace RollTheDice.Configs
{
    public class FlashRoundsConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // chance per hit (0.0 - 1.0)
        [JsonPropertyName("chance")] public float Chance { get; set; } = 0.25f;
        [JsonPropertyName("blind_seconds")] public float BlindSeconds { get; set; } = 0.7f;
    }
}
