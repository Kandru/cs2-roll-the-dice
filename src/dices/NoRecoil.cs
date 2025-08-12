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
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            player.ReplicateConVar("weapon_accuracy_nospread", "0");
            _ = _players.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            foreach (CCSPlayerController player in _players)
            {
                player.ReplicateConVar("weapon_accuracy_nospread", "0");
            }
            _players.Clear();
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

            if (!_players.Contains(player))
            {
                return HookResult.Continue;
            }

            CBasePlayerWeapon weapon = player.PlayerPawn!.Value!.WeaponServices!.ActiveWeapon!.Value!;
            // reset playerpawn recoil
            ApplyNoRecoil(player);
            //decrease recoil
            weapon.As<CCSWeaponBase>().FlRecoilIndex = 0;
            //nospread
            weapon.As<CCSWeaponBase>().AccuracyPenalty = 0;
            return HookResult.Continue;
        }

        // TODO: apply when switching weapons
        private static void ApplyNoRecoil(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }

            // replicate convar
            player.ReplicateConVar("weapon_accuracy_nospread", "1");

            // Get active weapon and apply weapon-specific control
            CBasePlayerWeapon? activeWeapon = player.PlayerPawn.Value.WeaponServices?.ActiveWeapon?.Value;
            if (activeWeapon == null)
            {
                return;
            }

            CCSWeaponBase weapon = activeWeapon.As<CCSWeaponBase>();
            if (weapon == null)
            {
                return;
            }

            // Get weapon class name to check weapon type
            string weaponName = weapon.DesignerName.ToLower(System.Globalization.CultureInfo.CurrentCulture);

            // Skip specific shotguns
            if (weaponName.Contains("mag7") ||
                weaponName.Contains("nova") ||
                weaponName.Contains("sawedoff") ||
                weaponName.Contains("xm1014"))
            {
                return;
            }

            // Handle Deagle and Revolver differently
            if (weaponName.Contains("deagle") || weaponName.Contains("revolver"))
            {
                // Softer recoil control for heavy pistols
                player.PlayerPawn.Value.AimPunchAngle.X *= 0.2f;
                player.PlayerPawn.Value.AimPunchAngle.Y *= 0.2f;
                player.PlayerPawn.Value.AimPunchAngleVel.X *= 0.2f;
                player.PlayerPawn.Value.AimPunchAngleVel.Y *= 0.2f;
                weapon.AccuracyPenalty *= 0.2f;
            }
            // Handle other pistols
            else if (weaponName.Contains("pistol")
                || weaponName.Contains("glock")
                || weaponName.Contains("usp")
                || weaponName.Contains("p250"))
            {
                // Moderate recoil control for regular pistols
                player.PlayerPawn.Value.AimPunchAngle.X *= 0.1f;
                player.PlayerPawn.Value.AimPunchAngle.Y *= 0.1f;
                player.PlayerPawn.Value.AimPunchAngleVel.X *= 0.1f;
                player.PlayerPawn.Value.AimPunchAngleVel.Y *= 0.1f;
                weapon.AccuracyPenalty *= 0.1f;
            }
            // All other weapons (rifles, SMGs, etc)
            else
            {
                // Complete recoil removal
                player.PlayerPawn.Value.AimPunchAngle.X = 0;
                player.PlayerPawn.Value.AimPunchAngle.Y = 0;
                player.PlayerPawn.Value.AimPunchAngle.Z = 0;

                player.PlayerPawn.Value.AimPunchAngleVel.X = 0;
                player.PlayerPawn.Value.AimPunchAngleVel.Y = 0;
                player.PlayerPawn.Value.AimPunchAngleVel.Z = 0;

                weapon.AccuracyPenalty = 0;
            }

            // Reset tick-based recoil
            player.PlayerPawn.Value.AimPunchTickBase = -1;
            player.PlayerPawn.Value.AimPunchTickFraction = 0;
        }
    }
}
