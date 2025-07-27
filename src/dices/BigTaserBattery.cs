using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class BigTaserBattery : ParentDice
    {
        public override string ClassName => "BigTaserBattery";
        public override List<string> Events => [
            "EventWeaponFire"
        ];
        public readonly Random _random = new();
        public readonly Dictionary<CCSPlayerController, int> _ammunition = [];

        public BigTaserBattery(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid
                || player.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }
            // get random taser size
            int battery = _random.Next(
                Convert.ToInt32(_config.Dices.BigTaserBattery.MinAmount),
                Convert.ToInt32(_config.Dices.BigTaserBattery.MaxAmount) + 1
            );
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "batterySize", battery.ToString() }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                { "gui", CreateMainGUI(player, ClassName, data) },
                { "status", CreateStatusGUI(player, ClassName, data) }
            });
            // add ammuniton for player
            _ammunition.Add(player, battery);
            // give taser if not exists
            if (player.Pawn.Value.WeaponServices.MyWeapons != null)
            {
                bool hasTaser = false;
                foreach (CounterStrikeSharp.API.Modules.Utils.CHandle<CBasePlayerWeapon> gun in player.Pawn.Value.WeaponServices.MyWeapons)
                {
                    CBasePlayerWeapon? weapon = gun.Value;
                    if (weapon != null && weapon.IsValid && weapon.DesignerName.Contains("taser"))
                    {
                        // recharge taser
                        weapon.Clip1 = 1;
                        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
                        hasTaser = true;
                        break;
                    }
                }
                if (!hasTaser)
                {
                    _ = player.GiveNamedItem("weapon_taser");
                }
            }
        }

        public override void Remove(CCSPlayerController player)
        {
            GUI.RemoveGUIs([.. _players[player].Values.Cast<CPointWorldText>()]);
            _ = _players.Remove(player);
            _ = _ammunition.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            // remove all GUIs for all players
            foreach (KeyValuePair<CCSPlayerController, Dictionary<string, CPointWorldText?>> kvp in _players)
            {
                GUI.RemoveGUIs([.. kvp.Value.Values.Cast<CPointWorldText>()]);
            }
            _players.Clear();
            _ammunition.Clear();
        }

        public HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;

            // player validation check
            if (player?.Pawn?.Value?.WeaponServices?.ActiveWeapon?.Value == null)
            {
                return HookResult.Continue;
            }
            // player has dice check
            if (!_players.TryGetValue(player, out Dictionary<string, CPointWorldText?>? playerData))
            {
                return HookResult.Continue;
            }
            // weapon check
            CBasePlayerWeapon activeWeapon = player.Pawn.Value.WeaponServices.ActiveWeapon.Value!;
            if (activeWeapon.DesignerName != "weapon_taser" || _ammunition[player] <= 0)
            {
                return HookResult.Continue;
            }
            // Recharge taser if we have multiple charges
            if (_ammunition[player] > 1)
            {
                activeWeapon.Clip1 = 2;
            }
            // Decrease ammunition and update GUI
            _ammunition[player]--;
            UpdateStatusGUI(player, playerData);

            return HookResult.Continue;
        }

        private void UpdateStatusGUI(CCSPlayerController player, Dictionary<string, CPointWorldText?> playerData)
        {
            if (_localizer[$"{ClassName}_status"].ResourceNotFound ||
            !playerData.TryGetValue("status", out CPointWorldText? worldText) ||
            worldText == null)
            {
                return;
            }

            bool isEnabled = _ammunition[player] > 0;
            string color = isEnabled
            ? _globalConfig.GUIPositions[_globalConfig.GUIPosition].StatusColorEnabled
            : _globalConfig.GUIPositions[_globalConfig.GUIPosition].StatusColorDisabled;

            GUI.ChangeColor(worldText, color);
            GUI.ChangeGUI(worldText, _localizer[$"{ClassName}_status"].Value.Replace(
            "{batterySize}", _ammunition[player].ToString()));
        }
    }
}
