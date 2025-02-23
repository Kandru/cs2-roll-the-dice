using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<CCSPlayerController, Dictionary<string, string>> _playersWithRespawnAbility = new();

        private Dictionary<string, string> DicePlayerRespawn(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // create listener if not exists
            if (_playersWithRespawnAbility.Count() == 0)
            {
                RegisterEventHandler<EventPlayerDeath>(EventDicePlayerRespawnOnPlayerDeath);
                RegisterEventHandler<EventPlayerTeam>(EventDicePlayerRespawnOnPlayerTeam);
                RegisterListener<Listeners.OnTick>(EventDicePlayerRespawnOnTick);
            }
            // add player to list
            _playersWithRespawnAbility.Add(player, new Dictionary<string, string>());
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerRespawnUnload()
        {
            DicePlayerRespawnReset();
        }

        private void DicePlayerRespawnReset()
        {
            DeregisterEventHandler<EventPlayerDeath>(EventDicePlayerRespawnOnPlayerDeath);
            DeregisterEventHandler<EventPlayerTeam>(EventDicePlayerRespawnOnPlayerTeam);
            RemoveListener<Listeners.OnTick>(EventDicePlayerRespawnOnTick);
            _playersWithRespawnAbility.Clear();
        }

        private void DicePlayerRespawnResetForPlayer(CCSPlayerController player)
        {
            // check if player has this dice
            if (!_playersWithRespawnAbility.ContainsKey(player)) return;
            // remove player
            _playersWithRespawnAbility.Remove(player);
            // remove listener if no players have this dice
            if (_playersWithRespawnAbility.Count == 0) DicePlayerRespawnReset();
        }

        private void EventDicePlayerRespawnOnTick()
        {
            if (_playersWithRespawnAbility.Count() == 0) return;
            // worker
            Dictionary<CCSPlayerController, Dictionary<string, string>> _playersWithRespawnAbilityCopy = new(_playersWithRespawnAbility);
            foreach (var (player, playerData) in _playersWithRespawnAbilityCopy)
            {
                try
                {
                    // sanity checks
                    if (player == null
                    || player.Pawn == null
                    || player.Pawn.Value == null
                    || player.PlayerPawn == null || !player.PlayerPawn.IsValid || player.PlayerPawn.Value == null
                    || player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE
                    || !playerData.ContainsKey("weapons")
                    || !_playersWithRespawnAbility.ContainsKey(player)) continue;
                    // respawn player
                    player.Respawn();
                    // give player weapons of attacker
                    foreach (string weapons in playerData["weapons"].Split(',').ToList())
                    {
                        player.GiveNamedItem(weapons);
                    }
                    // set armor for player
                    player.PlayerPawn.Value.ArmorValue = 100;
                    _playersWithRespawnAbility.Remove(player);
                    SendGlobalChatMessage(Localizer["DicePlayerRespawnSuccess"].Value
                        .Replace("{playerName}", player.PlayerName),
                        player: player);
                }
                catch (Exception e)
                {
                    // remove player
                    _playersWithRespawnAbility.Remove(player);
                    // log error
                    Console.WriteLine(Localizer["core.error"].Value.Replace("{error}", e.Message));
                }
            }
        }

        private HookResult EventDicePlayerRespawnOnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null) return HookResult.Continue;
            if (!_playersWithRespawnAbility.ContainsKey(player)) return HookResult.Continue;
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return HookResult.Continue;
            var weaponService = playerPawn.WeaponServices;
            if (weaponService == null) return HookResult.Continue;
            // give weapons from attacker because player is dead and has no weapons in weaponsService (they are removed)
            var tmpWeaponList = new List<string>();
            var attacker = @event.Attacker;
            if (attacker != null
                && attacker.PlayerPawn != null
                && attacker.PlayerPawn.Value != null
                && attacker.PlayerPawn.Value.WeaponServices != null)
            {
                foreach (var weapon in attacker.PlayerPawn.Value.WeaponServices.MyWeapons)
                {
                    // ignore unknown weapons
                    if (weapon == null || !weapon.IsValid || weapon.Value == null || (weapon.Value != null && weapon.Value.DesignerName == null)) continue;
                    // ignore knife and C4
                    if (weapon.Value!.DesignerName == $"weapon_{CsItem.C4.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == "weapon_knife" // necessary because CsItem.Knife is not always this value
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.Knife.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.KnifeCT.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.KnifeT.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.DefaultKnifeCT.ToString().ToLower()}"
                        || weapon.Value!.DesignerName == $"weapon_{CsItem.DefaultKnifeT.ToString().ToLower()}") continue;
                    // add Designername
                    tmpWeaponList.Add(weapon.Value.DesignerName!);
                }
            }
            // save weapons to string separated by comma for respawn
            _playersWithRespawnAbility[player]["weapons"] = string.Join(",", tmpWeaponList);
            return HookResult.Continue;
        }

        private HookResult EventDicePlayerRespawnOnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            if (@event.Team != (byte)CsTeam.Spectator && @event.Team != (byte)CsTeam.None) return HookResult.Continue;
            var player = @event.Userid;
            if (player == null) return HookResult.Continue;
            if (!_playersWithRespawnAbility.ContainsKey(player)) return HookResult.Continue;
            _playersWithRespawnAbility.Remove(player);
            return HookResult.Continue;
        }
    }
}
