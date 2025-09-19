using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class StripWeapons : DiceBlueprint
    {
        public override string ClassName => "StripWeapons";
        public readonly Random _random = new();
        private readonly List<string> _weaponsToKeep = [
            CsItem.C4.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture),
            $"weapon_{CsItem.C4.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}",
            CsItem.Bomb.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture),
            $"weapon_{CsItem.Bomb.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}",
            "knife",
            "weapon_knife",
            CsItem.Knife.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture),
            $"weapon_{CsItem.Knife.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}",
            CsItem.KnifeCT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture),
            $"weapon_{CsItem.KnifeCT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}",
            CsItem.KnifeT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture),
            $"weapon_{CsItem.KnifeT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}",
            CsItem.DefaultKnifeCT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture),
            $"weapon_{CsItem.DefaultKnifeCT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}",
            CsItem.DefaultKnifeT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture),
            $"weapon_{CsItem.DefaultKnifeT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}",
        ];

        public StripWeapons(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (!player.IsValid
                || player?.Pawn?.Value?.IsValid == false
                || player?.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }
            // get weapons to remove
            List<nint> myWeapons = [];
            foreach (CounterStrikeSharp.API.Modules.Utils.CHandle<CBasePlayerWeapon> weapon in player.Pawn.Value.WeaponServices.MyWeapons)
            {
                // ignore unknown weapons
                if (weapon == null || weapon.Value == null || weapon.Value.DesignerName == null)
                {
                    continue;
                }
                // remove all other weapons
                if (!_weaponsToKeep.Contains(weapon.Value!.DesignerName.ToLower(System.Globalization.CultureInfo.CurrentCulture)))
                {
                    myWeapons.Add(weapon.Value!.Handle);
                }
            }
            // remove weapons
            foreach (nint entry in myWeapons)
            {
                // get weapon from raw handle
                CBasePlayerWeapon weapon = new(entry);
                if (weapon == null
                    || !weapon.IsValid)
                {
                    continue;
                }
                // set player active weapon
                player.Pawn.Value.WeaponServices.ActiveWeapon.Raw = weapon.EntityHandle;
                // drop active weapon
                player.DropActiveWeapon();
                // remove weapon
                weapon.AcceptInput("kill");
            }
            // give player one valid weapon in his hands
            foreach (CounterStrikeSharp.API.Modules.Utils.CHandle<CBasePlayerWeapon> weapon in player.Pawn.Value.WeaponServices.MyWeapons)
            {
                // set player active weapon
                player.Pawn.Value.WeaponServices.ActiveWeapon.Raw = weapon.Raw;
                break;
            }
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }
    }
}
