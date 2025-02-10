using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private List<string> _weaponsToKeep = [
            CsItem.C4.ToString().ToLower(),
            $"weapon_{CsItem.C4.ToString().ToLower()}",
            CsItem.Bomb.ToString().ToLower(),
            $"weapon_{CsItem.Bomb.ToString().ToLower()}",
            "knife",
            "weapon_knife",
            CsItem.Knife.ToString().ToLower(),
            $"weapon_{CsItem.Knife.ToString().ToLower()}",
            CsItem.KnifeCT.ToString().ToLower(),
            $"weapon_{CsItem.KnifeCT.ToString().ToLower()}",
            CsItem.KnifeT.ToString().ToLower(),
            $"weapon_{CsItem.KnifeT.ToString().ToLower()}",
            CsItem.DefaultKnifeCT.ToString().ToLower(),
            $"weapon_{CsItem.DefaultKnifeCT.ToString().ToLower()}",
            CsItem.DefaultKnifeT.ToString().ToLower(),
            $"weapon_{CsItem.DefaultKnifeT.ToString().ToLower()}",
        ];
        private Dictionary<string, string> DiceStripWeapons(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            if (playerPawn.WeaponServices == null)
                return new Dictionary<string, string>
                {
                    {"error", "command.rollthedice.error"}
                };
            // get weapons to remove
            List<nint> myWeapons = [];
            foreach (var weapon in playerPawn.WeaponServices.MyWeapons)
            {
                // ignore unknown weapons
                if (weapon == null || weapon.Value == null || weapon.Value.DesignerName == null) continue;
                // remove all other weapons
                if (!_weaponsToKeep.Contains(weapon.Value!.DesignerName.ToLower()))
                {
                    myWeapons.Add(weapon.Value!.Handle);
                }
            }

            foreach (var entry in myWeapons)
            {
                // get weapon from raw handle
                CBasePlayerWeapon weapon = new CBasePlayerWeapon(entry);
                if (weapon == null || !weapon.IsValid) continue;
                // set player active weapon
                playerPawn.WeaponServices.ActiveWeapon.Raw = weapon.EntityHandle;
                // drop active weapon
                player.DropActiveWeapon();
                // remove weapon
                weapon.AcceptInput("kill");
            }

            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName}
            };
        }
    }
}
