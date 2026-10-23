using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Configs;
using RollTheDice.Utils;
using System.Text.Json.Serialization;

namespace RollTheDice.Dices
{
    public class Martyrdom : DiceBlueprint
    {
        public override string ClassName => "Martyrdom";
        public override List<string> Events { get; } = ["EventPlayerDeath"];

        public Martyrdom(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        /// <summary>
        /// The holder drops a live HE on death. Runs before the plugin removes the dice on death (module events are
        /// hooked Pre), so the holder is still in the list here.
        /// </summary>
        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (_players.Count == 0
                || player is not { IsValid: true }
                || !_players.Contains(player)
                || player.PlayerPawn.Value is not { IsValid: true, AbsOrigin: { } origin } pawn)
            {
                return HookResult.Continue;
            }

            MartyrdomConfig config = _config.Dices.Martyrdom;
            Vector position = new(origin.X, origin.Y, origin.Z + 16f);
            Server.NextFrame(() =>
            {
                Explosion.Detonate(position, pawn.IsValid ? pawn : null, config.DelaySeconds, config.Damage, config.Radius);
                Tell(player, "dice_Martyrdom_dropped");
            });
            return HookResult.Continue;
        }
    }
}

namespace RollTheDice.Configs
{
    public class MartyrdomConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("delay_seconds")] public float DelaySeconds { get; set; } = 1.5f;
        // damage and radius of a normal HE grenade
        [JsonPropertyName("damage")] public float Damage { get; set; } = 99f;
        [JsonPropertyName("radius")] public float Radius { get; set; } = 350f;
    }
}
