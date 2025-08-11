using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class Respawn : ParentDice
    {
        public override string ClassName => "Respawn";
        public readonly Random _random = new();
        public override List<string> Events => [
            "EventWeaponFire",
            "EventPlayerTeam"
        ];

        public Respawn(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !_players.Contains(player)
                || player.PlayerPawn?.Value?.WeaponServices == null)
            {
                return HookResult.Continue;
            }
            // give weapons from attacker because player is dead and has no weapons in weaponsService (they are removed)
            List<string> tmpWeaponList = [];
            CCSPlayerController? attacker = @event.Attacker;
            if (attacker?.PlayerPawn?.Value?.WeaponServices != null)
            {
                foreach (CounterStrikeSharp.API.Modules.Utils.CHandle<CBasePlayerWeapon> weapon in attacker.PlayerPawn.Value.WeaponServices.MyWeapons)
                {
                    // ignore unknown weapons
                    if (weapon == null
                        || !weapon.IsValid
                        || weapon.Value == null
                        || (weapon.Value != null && weapon.Value.DesignerName == null))
                    {
                        continue;
                    }
                    // ignore knife and C4
                    if (weapon.Value!.DesignerName == $"weapon_{CsItem.C4.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == "weapon_knife" // necessary because CsItem.Knife is not always this value
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.Knife.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.KnifeCT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.KnifeT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.DefaultKnifeCT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.DefaultKnifeT.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}")
                    {
                        continue;
                    }
                    // add Designername
                    tmpWeaponList.Add(weapon.Value.DesignerName!);
                }
            }
            // respawn player
            Server.NextFrame(() =>
            {
                // sanity checks
                if (player?.PlayerPawn?.Value == null
                    || player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    || !_players.Contains(player)) return;
                // respawn player
                player.Respawn();
                // give weapons next frame to ensure player is respawned
                Server.NextFrame(() =>
                {
                    // strip all other weapons
                    player.RemoveWeapons();
                    // add default knife to player
                    player.GiveNamedItem($"weapon_{CsItem.Knife.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)}");
                    // give player weapons of attacker (if any)
                    if (tmpWeaponList.Count > 0)
                    {
                        foreach (string weapons in tmpWeaponList)
                        {
                            player.GiveNamedItem(weapons);
                        }
                    }
                    else
                    {
                        // give some default loadout if no weapons are available
                        player.GiveNamedItem(_config.Dices.Respawn.DefaultPrimaryWeapon);
                        player.GiveNamedItem(_config.Dices.Respawn.DefaultSecondaryWeapon);
                    }
                    // set armor for player
                    player.PlayerPawn.Value.ArmorValue = 100;
                    // remove player from list to avoid respawning again
                    _players.Remove(player);
                });
            });
            return HookResult.Continue;
        }
    }
}
