using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class NoRecoil : DiceBlueprint
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

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            player.ReplicateConVar("weapon_accuracy_nospread", "0");
            _ = _players.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !_players.Contains(player)
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
        private static void ApplyNoRecoil(CCSPlayerController? player)
        {
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid
                || player.PlayerPawn.Value.AimPunchServices == null)
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
            // Complete recoil removal
            player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngle.X = 0;
            player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngle.Y = 0;
            player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngle.Z = 0;

            player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngleVel.X = 0;
            player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngleVel.Y = 0;
            player.PlayerPawn.Value.AimPunchServices.PredictableBaseAngleVel.Z = 0;

            player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.X = 0;
            player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Y = 0;
            player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Z = 0;

            player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.X = 0;
            player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Y = 0;
            player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseAngle.Z = 0;

            weapon.AccuracyPenalty = 0;
            // Reset tick-based recoil
            player.PlayerPawn.Value.AimPunchServices.PredictableBaseTick = -1;
            player.PlayerPawn.Value.AimPunchServices.UnpredictableBaseTick = -1;
        }
    }
}
