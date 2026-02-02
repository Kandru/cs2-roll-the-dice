using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class ResetOnReload : DiceBlueprint
    {
        public override string ClassName => "ResetOnReload";
        public readonly Random _random = new();
        public override List<string> Events => [
            "EventWeaponReload",
        ];

        public ResetOnReload(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public HookResult EventWeaponReload(EventWeaponReload @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player?.IsValid != true
                || !_players.Contains(player))
            {
                return HookResult.Continue;
            }
            player.Respawn();
            return HookResult.Continue;
        }
    }
}
