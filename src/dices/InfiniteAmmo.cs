using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class InfiniteAmmo : DiceBlueprint
    {
        public override string ClassName => "InfiniteAmmo";
        public override List<string> Events => [
            "EventWeaponFire"
        ];
        public readonly Random _random = new();

        public InfiniteAmmo(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;

            // player validation check
            if (player?.Pawn?.Value?.WeaponServices?.ActiveWeapon?.Value == null)
            {
                return HookResult.Continue;
            }
            // player has dice check
            if (!_players.Contains(player))
            {
                return HookResult.Continue;
            }
            // weapon check
            CBasePlayerWeapon activeWeapon = player.Pawn.Value.WeaponServices.ActiveWeapon.Value!;
            if (activeWeapon == null
                || !activeWeapon.IsValid)
            {
                return HookResult.Continue;
            }
            // unlimited ammo
            if (activeWeapon.Clip1 < 10)
            {
                activeWeapon.Clip1 = 10;
            }
            else
            {
                activeWeapon.Clip1 += 1;
            }

            return HookResult.Continue;
        }
    }
}
