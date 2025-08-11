using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class NoRecoil : ParentDice
    {
        public override string ClassName => "NoRecoil";
        public override List<string> Events => [
            "EventWeaponFire"
        ];
        public readonly Random _random = new();

        public NoRecoil(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                //{ "gui", CreateMainGUI(player, ClassName, data) }
            });
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null
                || player.PlayerPawn.Value.WeaponServices == null
                || player.PlayerPawn.Value.WeaponServices.ActiveWeapon == null
                || !player.PlayerPawn.Value.WeaponServices.ActiveWeapon.IsValid
                || player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value == null)
            {
                return HookResult.Continue;
            }

            if (!_players.ContainsKey(player))
            {
                return HookResult.Continue;
            }

            CBasePlayerWeapon weapon = player.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon!.Value!;
            // reset playerpawn recoil
            player.PlayerPawn.Value.AimPunchAngle.X = 0;
            player.PlayerPawn.Value.AimPunchAngle.Y = 0;
            player.PlayerPawn.Value.AimPunchAngle.Z = 0;
            player.PlayerPawn.Value.AimPunchAngleVel.X = 0;
            player.PlayerPawn.Value.AimPunchAngleVel.Y = 0;
            player.PlayerPawn.Value.AimPunchAngleVel.Z = 0;
            player.PlayerPawn.Value.AimPunchTickBase = -1;
            player.PlayerPawn.Value.AimPunchTickFraction = 0;
            //decrease recoil
            weapon.As<CCSWeaponBase>().FlRecoilIndex = 0;
            //nospread
            weapon.As<CCSWeaponBase>().AccuracyPenalty = 0;
            return HookResult.Continue;
        }
    }
}
