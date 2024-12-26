using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<string, string> DiceStripWeapons(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            if (playerPawn.WeaponServices == null)
                return new Dictionary<string, string>
                {
                    {"_translation_player", "command.rollthedice.error"},
                    { "playerName", player.PlayerName }
                };
            var playerWeapons = playerPawn.WeaponServices!;
            foreach (var weapon in playerWeapons.MyWeapons)
            {
                // ignore unknown weapons
                if (weapon.Value == null || weapon.Value != null && weapon.Value.DesignerName == null) continue;
                if (weapon.Value!.DesignerName == CsItem.C4.ToString().ToLower()
                    || weapon.Value!.DesignerName == CsItem.Bomb.ToString().ToLower())
                {
                    // change weapon to currently active weapon
                    playerPawn.WeaponServices.ActiveWeapon.Raw = weapon.Raw;
                    // drop active weapon
                    player.DropActiveWeapon();
                }
            }
            AddTimer(0.1f, () =>
            {
                if (player == null || !player.IsValid) return;
                player.RemoveWeapons();
                player.GiveNamedItem("weapon_knife");
            });

            return new Dictionary<string, string>
            {
                {"_translation_player", "DiceStripWeaponsPlayer"},
                {"_translation_other", "DiceStripWeapons"},
                { "playerName", player.PlayerName }
            };
        }
    }
}
