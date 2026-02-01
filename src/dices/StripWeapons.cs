using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;

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
        public override List<string> Events => [
            "EventEnterBuyzone",
            "EventItemPickup"
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
                || player?.PlayerPawn?.Value == null
                || player?.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }
            // get weapons to remove
            List<nint> myWeapons = [];
            foreach (CHandle<CBasePlayerWeapon> weapon in player.Pawn.Value.WeaponServices.MyWeapons)
            {
                // ignore unknown weapons
                if (weapon == null || weapon.Value == null || weapon.Value.DesignerName == null)
                {
                    continue;
                }
                // remove all other weapons
                if (!_weaponsToKeep.Contains(weapon.Value.DesignerName.ToLower(System.Globalization.CultureInfo.CurrentCulture)))
                {
                    myWeapons.Add(weapon.Value.Handle);
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
            foreach (CHandle<CBasePlayerWeapon> weapon in player.Pawn.Value.WeaponServices.MyWeapons)
            {
                // set player active weapon
                player.Pawn.Value.WeaponServices.ActiveWeapon.Raw = weapon.Raw;
                break;
            }
            // disallow buyzone
            if (_config.Dices.StripWeapons.DisableBuymenu)
            {
                player.PlayerPawn.Value.InBuyZone = false;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_bInBuyZone");
            }
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public HookResult EventEnterBuyzone(EventEnterBuyzone @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player?.IsValid != true
                || player.PlayerPawn?.Value == null
                || !_players.Contains(player)
                || !_config.Dices.StripWeapons.DisableBuymenu)
            {
                return HookResult.Continue;
            }
            player.PlayerPawn.Value.InBuyZone = false;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_bInBuyZone");
            return HookResult.Continue;
        }

        public HookResult EventItemPickup(EventItemPickup @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player?.IsValid != true
                || player.PlayerPawn?.Value == null
                || !_players.Contains(player)
                || !_config.Dices.StripWeapons.DisableWeaponPickup)
            {
                return HookResult.Continue;
            }
            string weapon = @event.Item;
            Server.NextFrame(() =>
            {
                if (player == null
                    || !player.IsValid
                    || player.Pawn?.Value?.WeaponServices == null
                    || player.Pawn?.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                {
                    return;
                }
                foreach (CHandle<CBasePlayerWeapon> weaponHandle in player.Pawn.Value.WeaponServices.MyWeapons)
                {
                    // skip invalid weapon handles
                    if (weaponHandle == null
                        || !weaponHandle.IsValid)
                    {
                        continue;
                    }
                    // get weapon from handle
                    CBasePlayerWeapon? playerWeapon = weaponHandle.Value;
                    // skip invalid weapon
                    if (playerWeapon == null
                        || !playerWeapon.IsValid)
                    {
                        continue;
                    }
                    string? weapon_name = Entities.PlayerWeaponName(playerWeapon);
                    if (weapon_name != null && weapon_name.Contains(weapon))
                    {
                        // set weapon as currently active weapon
                        player.Pawn.Value.WeaponServices.ActiveWeapon.Raw = weaponHandle.Raw;
                        // drop active weapon
                        player.DropActiveWeapon();
                        // delete weapon entity
                        playerWeapon.AddEntityIOEvent("Kill", playerWeapon, null, "", 0.1f);
                    }
                }
            });
            return HookResult.Continue;
        }
    }
}
