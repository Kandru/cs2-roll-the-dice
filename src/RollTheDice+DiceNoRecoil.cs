using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private List<CCSPlayerController> _playersWithoutRecoil = new();

        private Dictionary<string, string> DiceNoRecoil(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // create listener if not exists
            if (_playersWithoutRecoil.Count() == 0) RegisterEventHandler<EventWeaponFire>(EventDiceNoRecoilOnWeaponFire);
            _playersWithoutRecoil.Add(player);
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DiceNoRecoilUnload()
        {
            DiceNoRecoilReset();
        }

        private void DiceNoRecoilReset()
        {
            DeregisterEventHandler<EventWeaponFire>(EventDiceNoRecoilOnWeaponFire);
            _playersWithoutRecoil.Clear();
        }

        private void DiceNoRecoilResetForPlayer(CCSPlayerController player)
        {
            if (!_playersWithoutRecoil.Contains(player)) return;
            _playersWithoutRecoil.Remove(player);
            // remove listener if no players have this dice
            if (_playersWithoutRecoil.Count == 0) DiceNoRecoilReset();
        }

        private HookResult EventDiceNoRecoilOnWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null
                || player.PlayerPawn.Value.WeaponServices == null
                || player.PlayerPawn.Value.WeaponServices.ActiveWeapon == null
                || !player.PlayerPawn.Value.WeaponServices.ActiveWeapon.IsValid
                || player.PlayerPawn.Value.WeaponServices.ActiveWeapon.Value == null) return HookResult.Continue;
            if (!_playersWithoutRecoil.Contains(player)) return HookResult.Continue;
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
