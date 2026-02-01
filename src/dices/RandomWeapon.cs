using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class RandomWeapon : DiceBlueprint
    {
        public override string ClassName => "RandomWeapon";
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
            "EventEnterBuyzone"
        ];

        public RandomWeapon(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // get randomized weapon
            bool givePrimary = false;
            bool giveSecondary = false;

            if (_config.Dices.RandomWeapon.RandomSecondaryAndPrimaryWeapon)
            {
                givePrimary = true;
                giveSecondary = true;
            }
            else
            {
                int choice = _random.Next(0, 2); // 0: Primary, 1: Secondary
                if (choice == 0)
                {
                    givePrimary = true;
                }
                else
                {
                    giveSecondary = true;
                }
            }

            if (givePrimary && _config.Dices.RandomWeapon.PrimaryWeapons.Count > 0)
            {
                string randomPrimary = _config.Dices.RandomWeapon.PrimaryWeapons[_random.Next(_config.Dices.RandomWeapon.PrimaryWeapons.Count)];
                _ = player.GiveNamedItem(randomPrimary);
            }
            if (giveSecondary && _config.Dices.RandomWeapon.SecondaryWeapons.Count > 0)
            {
                string randomSecondary = _config.Dices.RandomWeapon.SecondaryWeapons[_random.Next(_config.Dices.RandomWeapon.SecondaryWeapons.Count)];
                _ = player.GiveNamedItem(randomSecondary);
            }

            if (givePrimary)
            {
                player.ExecuteClientCommand("slot1");
            }
            else if (giveSecondary)
            {
                player.ExecuteClientCommand("slot2");
            }

            // give player one valid weapon in his hands
            foreach (CHandle<CBasePlayerWeapon> weapon in player.Pawn.Value.WeaponServices.MyWeapons)
            {
                // set player active weapon
                player.Pawn.Value.WeaponServices.ActiveWeapon.Raw = weapon.Raw;
                break;
            }
            // disallow buyzone
            if (_config.Dices.RandomWeapon.DisableBuymenu)
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
                || !_config.Dices.RandomWeapon.DisableBuymenu)
            {
                return HookResult.Continue;
            }
            player.PlayerPawn.Value.InBuyZone = false;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_bInBuyZone");
            return HookResult.Continue;
        }
    }
}
