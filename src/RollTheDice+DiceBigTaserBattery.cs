using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<CCSPlayerController, int> _playersWithBigTaserBattery = [];

        private Dictionary<string, string> DiceBigTaserBattery(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceBigTaserBattery");
            // create listener if not exists
            if (_playersWithBigTaserBattery.Count == 0)
            {
                RegisterEventHandler<EventWeaponFire>(EventDiceBigTaserBatteryOnWeaponFire);
            }
            int battery = _random.Next(
                Convert.ToInt32(config["min_batteries"]),
                Convert.ToInt32(config["max_batteries"]) + 1
            );
            _playersWithBigTaserBattery.Add(player, battery);
            // give taser if not exists
            if (playerPawn.WeaponServices?.MyWeapons != null)
            {
                bool hasTaser = false;
                foreach (var gun in playerPawn.WeaponServices!.MyWeapons)
                {
                    var weapon = gun.Value;
                    if (weapon != null && weapon.IsValid && weapon.DesignerName.Contains("taser"))
                    {
                        hasTaser = true;
                        break;
                    }
                }
                if (!hasTaser)
                {
                    player.GiveNamedItem("weapon_taser");
                }
            }
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName },
                { "batterySize", battery.ToString() }
            };
        }

        private void DiceBigTaserBatteryUnload()
        {
            DiceBigTaserBatteryReset();
        }

        private void DiceBigTaserBatteryReset()
        {
            DeregisterEventHandler<EventWeaponFire>(EventDiceBigTaserBatteryOnWeaponFire);
            _playersWithBigTaserBattery.Clear();
        }

        private void DiceBigTaserBatteryResetForPlayer(CCSPlayerController player)
        {
            // check if player has this dice
            if (!_playersWithBigTaserBattery.ContainsKey(player)) return;
            // remove player
            _playersWithBigTaserBattery.Remove(player);
            // remove event listener when no players have this dice
            if (_playersWithBigTaserBattery.Count == 0) DiceBigTaserBatteryReset();
        }

        private HookResult EventDiceBigTaserBatteryOnWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid!;
            if (!_playersWithBigTaserBattery.ContainsKey(player)) return HookResult.Continue;
            if (player == null || player.Pawn == null || !player.Pawn.IsValid || player.Pawn.Value == null) return HookResult.Continue;
            if (player.Pawn.Value.WeaponServices == null || player.Pawn.Value.WeaponServices.ActiveWeapon == null) return HookResult.Continue;
            if (player.Pawn.Value.WeaponServices.ActiveWeapon == null || player.Pawn.Value.WeaponServices.ActiveWeapon.Value == null) return HookResult.Continue;
            if (player.Pawn.Value.WeaponServices.ActiveWeapon.Value!.DesignerName != "weapon_taser") return HookResult.Continue;
            if (_playersWithBigTaserBattery[player] <= 0) return HookResult.Continue;
            player.Pawn.Value.WeaponServices.ActiveWeapon.Value!.Clip1 = 2;
            _playersWithBigTaserBattery[player]--;
            // update gui if available
            if (_playersThatRolledTheDice.ContainsKey(player)
                && _playersThatRolledTheDice[player].ContainsKey("gui_status")
                && (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"] != null)
            {
                CPointWorldText worldText = (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"];
                if (_playersWithBigTaserBattery[player] == 0)
                    ChangeColor(worldText, Config.GUIPositions[Config.GUIPosition].StatusColorDisabled);
                else
                    ChangeColor(worldText, Config.GUIPositions[Config.GUIPosition].StatusColorEnabled);
                worldText.AcceptInput("SetMessage", worldText, worldText, Localizer["DiceBigTaserBattery_status"].Value.Replace(
                    "{batterySize}", _playersWithBigTaserBattery[player].ToString()
                ));
            }
            return HookResult.Continue;
        }

        private Dictionary<string, object> DiceBigTaserBatteryConfig()
        {
            var config = new Dictionary<string, object>();
            config["min_batteries"] = (int)2;
            config["max_batteries"] = (int)10;
            return config;
        }
    }
}
