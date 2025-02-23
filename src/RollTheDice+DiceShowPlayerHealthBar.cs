using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<CCSPlayerController, Dictionary<CCSPlayerPawn, float>> _playersWithHealthBarShown = new();

        private Dictionary<string, string> DiceShowPlayerHealthBar(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            // create listener if not exists
            if (_playersWithHealthBarShown.Count() == 0)
            {
                RegisterListener<Listeners.OnTick>(DiceShowPlayerHealthBarOnTick);
                RegisterEventHandler<EventPlayerHurt>(EventDiceShowPlayerHealthBarOnPlayerHurt);
            }
            _playersWithHealthBarShown.Add(player, new Dictionary<CCSPlayerPawn, float>());
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DiceShowPlayerHealthBarUnload()
        {
            DiceShowPlayerHealthBarReset();
        }

        private void DiceShowPlayerHealthBarReset()
        {
            RemoveListener<Listeners.OnTick>(DiceShowPlayerHealthBarOnTick);
            DeregisterEventHandler<EventPlayerHurt>(EventDiceShowPlayerHealthBarOnPlayerHurt);
            _playersWithHealthBarShown.Clear();
        }

        private void DiceShowPlayerHealthBarResetForPlayer(CCSPlayerController player)
        {
            // check if player has this dice
            if (!_playersWithHealthBarShown.ContainsKey(player)) return;
            // remove player
            _playersWithHealthBarShown.Remove(player);
            // remove listener if no players have this dice
            if (_playersWithHealthBarShown.Count == 0) DiceShowPlayerHealthBarReset();
        }

        private HookResult EventDiceShowPlayerHealthBarOnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;
            if (victim == null
                || !victim.IsValid
                || victim.PlayerPawn == null
                || !victim.PlayerPawn.IsValid
                || victim.PlayerPawn.Value == null
                || attacker == null
                || !attacker.IsValid
                || victim.TeamNum == attacker.TeamNum) return HookResult.Continue;
            if (victim == attacker) return HookResult.Continue;
            if (!_playersWithHealthBarShown.ContainsKey(attacker)) return HookResult.Continue;
            float oldHealth = @event.Health + @event.DmgHealth;
            float newHealth = @event.Health;
            if (oldHealth == newHealth) return HookResult.Continue;
            // send message
            var message = UserMessage.FromPartialName("UpdateScreenHealthBar");
            message.SetInt("entidx", (int)victim.PlayerPawn.Index);
            message.SetFloat("healthratio_old", oldHealth / victim.PlayerPawn.Value!.MaxHealth);
            message.SetFloat("healthratio_new", newHealth / victim.PlayerPawn.Value!.MaxHealth);
            message.SetInt("style", 0);
            message.Send(attacker);
            return HookResult.Continue;
        }

        private void DiceShowPlayerHealthBarOnTick()
        {
            if (_playersWithHealthBarShown.Count() == 0) return;
            // only every 32 ticks (roughly one second)
            if (Server.TickCount % 8 != 0) return;
            // worker
            Dictionary<CCSPlayerController, Dictionary<CCSPlayerPawn, float>> _playersWithHealthBarShownCopy = new(_playersWithHealthBarShown);
            foreach (CCSPlayerController player in _playersWithHealthBarShownCopy.Keys)
            {
                try
                {
                    // sanity checks
                    if (player == null
                    || player.PlayerPawn == null
                    || !player.PlayerPawn.IsValid
                    || player.PlayerPawn.Value == null
                    || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE
                    || !_playersWithHealthBarShown.ContainsKey(player)) continue;
                    // check if player is aiming at another player
                    CCSPlayerPawn? playerTarget = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")?
                        .FirstOrDefault()?
                        .GameRules?
                        .FindPickerEntity<CCSPlayerPawn>(player);
                    if (playerTarget == null
                        || !playerTarget.IsValid
                        || playerTarget.LifeState != (byte)LifeState_t.LIFE_ALIVE
                        || playerTarget.TeamNum == player.TeamNum
                        || playerTarget.DesignerName == null
                        || playerTarget.DesignerName != "player"
                        || playerTarget.Health <= 0) continue;
                    // check if we should resend the message (only every 2 seconds)
                    if (_playersWithHealthBarShown[player].ContainsKey(playerTarget))
                    {
                        if (_playersWithHealthBarShown[player][playerTarget] + 2.0f <= Server.CurrentTime)
                        {
                            _playersWithHealthBarShown[player][playerTarget] = Server.CurrentTime;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        _playersWithHealthBarShown[player].Add(playerTarget, Server.CurrentTime);
                    }
                    // send message
                    var message = UserMessage.FromPartialName("UpdateScreenHealthBar");
                    message.SetInt("entidx", (int)playerTarget.Index);
                    message.SetFloat("healthratio_old", (float)playerTarget.Health / (float)playerTarget.MaxHealth);
                    message.SetFloat("healthratio_new", (float)playerTarget.Health / (float)playerTarget.MaxHealth);
                    message.SetInt("style", 0);
                    message.Send(player);
                }
                catch (Exception e)
                {
                    // remove player
                    _playersWithCloak.Remove(player);
                    // log error
                    Console.WriteLine(Localizer["core.error"].Value.Replace("{error}", e.Message));
                }
            }
        }
    }
}
