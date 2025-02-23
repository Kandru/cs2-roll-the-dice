using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private List<CCSPlayerController> _playersCanInstantDefuse = new();
        private List<CCSPlayerController> _playersCanInstantPlant = new();
        private List<CCSPlayerPawn> _playersCanInstantRescueHostages = new();

        private Dictionary<string, string> DiceFastMapAction(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // check for map entities
            var bombEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon_c4").ToArray();
            var plantetBombEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("planted_c4").ToArray();
            var hostageEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("hostage_entity").ToArray();
            // if bomb map
            if (bombEntities.Length > 0 || plantetBombEntities.Length > 0)
            {
                // create listener if not exists
                if (_playersCanInstantDefuse.Count() == 0 && _playersCanInstantPlant.Count() == 0)
                {
                    RegisterEventHandler<EventBombBegindefuse>(DiceFastBombActionEventBeginDefuse);
                    RegisterEventHandler<EventBombBeginplant>(DiceFastBombActionEventBeginPlant);
                }
                if (playerPawn.TeamNum == (int)CsTeam.Terrorist)
                {
                    _playersCanInstantPlant.Add(player);
                    return new Dictionary<string, string>
                    {
                        {"locale", "DiceFastBombActionT"},
                        { "playerName", player.PlayerName }
                    };
                }
                else if (playerPawn.TeamNum == (int)CsTeam.CounterTerrorist)
                {
                    _playersCanInstantDefuse.Add(player);
                    return new Dictionary<string, string>
                    {
                        {"locale", "DiceFastBombActionCT"},
                        { "playerName", player.PlayerName }
                    };
                }
                else
                {
                    return new Dictionary<string, string>
                    {
                        {"error", "command.rollthedice.error"}
                    };
                }
            }
            else if (hostageEntities.Length > 0)
            {
                if (playerPawn.TeamNum == (int)CsTeam.CounterTerrorist)
                {
                    _playersCanInstantRescueHostages.Add(playerPawn);
                    return new Dictionary<string, string>
                    {
                        {"locale", "DiceFastHostageActionCT"},
                        { "playerName", player.PlayerName }
                    };
                }
                else
                {
                    return new Dictionary<string, string>
                    {
                        {"locale", "DiceFastHostageActionUnlucky"},
                        { "playerName", player.PlayerName }
                    };
                }
            }
            else
            {
                return new Dictionary<string, string>
                {
                    {"locale", "DiceFastHostageActionUnlucky"},
                        { "playerName", player.PlayerName }
                };
            }
        }

        private void DiceFastMapActionUnload()
        {
            DiceFastMapActionReset();
        }

        private void DiceFastMapActionReset()
        {
            DeregisterEventHandler<EventBombBegindefuse>(DiceFastBombActionEventBeginDefuse);
            DeregisterEventHandler<EventBombBeginplant>(DiceFastBombActionEventBeginPlant);
            _playersCanInstantDefuse.Clear();
            _playersCanInstantPlant.Clear();
            _playersCanInstantRescueHostages.Clear();
        }

        private void DiceFastMapActionResetForPlayer(CCSPlayerController player)
        {
            if (_playersCanInstantDefuse.Contains(player)) _playersCanInstantDefuse.Remove(player);
            if (_playersCanInstantPlant.Contains(player)) _playersCanInstantPlant.Remove(player);
            if (player.PlayerPawn != null
                && player.PlayerPawn.IsValid
                && player.PlayerPawn.Value != null
                && _playersCanInstantRescueHostages.Contains(player.PlayerPawn.Value)) _playersCanInstantRescueHostages.Remove(player.PlayerPawn.Value);
        }

        private HookResult DiceFastBombActionEventBeginDefuse(EventBombBegindefuse @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null) return HookResult.Continue;
            if (!_playersCanInstantDefuse.Contains(player)) return HookResult.Continue;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return HookResult.Continue;
            var c4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").First();
            Server.NextFrame(() =>
            {
                if (c4 == null) return;
                c4.DefuseCountDown = Server.CurrentTime;
            });
            return HookResult.Continue;
        }

        private HookResult DiceFastBombActionEventBeginPlant(EventBombBeginplant @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null) return HookResult.Continue;
            if (!_playersCanInstantPlant.Contains(player)) return HookResult.Continue;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return HookResult.Continue;
            var weaponService = playerPawn.WeaponServices?.ActiveWeapon;
            if (weaponService == null) return HookResult.Continue;
            var activeWeapon = weaponService.Value;
            if (activeWeapon == null) return HookResult.Continue;
            if (!activeWeapon.DesignerName.Contains("c4")) return HookResult.Continue;
            var c4 = new CC4(activeWeapon.Handle);
            Server.NextFrame(() =>
            {
                if (c4 == null) return;
                c4.ArmedTime = Server.CurrentTime;
            });
            return HookResult.Continue;
        }

        // Hook HostageEntity grab begin
        [EntityOutputHook("*", "OnHostageBeginGrab")]
        public HookResult OnPickup(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
        {
            if (activator.DesignerName != "hostage_entity") return HookResult.Continue;
            if (caller.DesignerName != "player") return HookResult.Continue;
            CCSPlayerPawn playerPawn = new CCSPlayerPawn(caller.Handle);
            if (playerPawn == null
                || !playerPawn.IsValid)
                return HookResult.Continue;
            // check if user is eligible to rescue a hostage instantly
            if (!_playersCanInstantRescueHostages.Contains(playerPawn)) return HookResult.Continue;
            // set success time to current time
            Schema.SetSchemaValue(activator.Handle, "CHostage", "m_flGrabSuccessTime", Server.CurrentTime);
            return HookResult.Continue;
        }
    }
}
