using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class BigTaserBattery : ParentDice
    {
        public override List<string> Events => [
            "EventWeaponFire"
        ];
        public readonly Random _random = new();
        public readonly Dictionary<CCSPlayerController, int> _ammunition = [];

        public BigTaserBattery(PluginConfig Config, IStringLocalizer Localizer) : base(Config, Localizer)
        {
            Console.WriteLine("[RollTheDice] Initializing BigTaserBattery...");
        }

        public override void Destroy()
        {
            Console.WriteLine("[RollTheDice] Destroying BigTaserBattery...");
            _players.Clear();
        }

        public override void Add(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || player.Pawn?.IsValid == false
                || player.Pawn?.Value?.WeaponServices == null)
            {
                return;
            }
            int battery = _random.Next(
                Convert.ToInt32(_config.Dices.BigTaserBattery.MinAmount),
                Convert.ToInt32(_config.Dices.BigTaserBattery.MaxAmount) + 1
            );
            _players.Add(player);
            _ammunition.Add(player, battery);
            // give taser if not exists
            if (player.Pawn.Value.WeaponServices.MyWeapons != null)
            {
                bool hasTaser = false;
                foreach (var gun in player.Pawn.Value.WeaponServices.MyWeapons)
                {
                    var weapon = gun.Value;
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
                    player.GiveNamedItem("weapon_taser");
                }
            }
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName },
                { "batterySize", battery.ToString() }
            };
        }

        private HookResult EventWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            CCSPlayerController player = @event.Userid!;
            if (!_players.Contains(player)) return HookResult.Continue;
            if (player == null || player.Pawn == null || !player.Pawn.IsValid || player.Pawn.Value == null) return HookResult.Continue;
            if (player.Pawn.Value.WeaponServices == null || player.Pawn.Value.WeaponServices.ActiveWeapon == null) return HookResult.Continue;
            if (player.Pawn.Value.WeaponServices.ActiveWeapon == null || player.Pawn.Value.WeaponServices.ActiveWeapon.Value == null) return HookResult.Continue;
            if (player.Pawn.Value.WeaponServices.ActiveWeapon.Value!.DesignerName != "weapon_taser") return HookResult.Continue;
            if (_ammunition[player] <= 0) return HookResult.Continue;
            // recharge taser but only if we have at least 2 charges left
            if (_ammunition[player] > 1) player.Pawn.Value.WeaponServices.ActiveWeapon.Value!.Clip1 = 2;
            // decrease battery size
            _ammunition[player]--;
            // update gui if available
            if (_players.Contains(player)
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
    }
}
