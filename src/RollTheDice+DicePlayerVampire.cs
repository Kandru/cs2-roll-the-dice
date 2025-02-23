using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private List<CCSPlayerController> _playerVampires = new();

        private Dictionary<string, string> DicePlayerVampire(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            Dictionary<string, object> config = GetDiceConfig("DicePlayerVampire");
            // create listener if not exists
            if (_playerVampires.Count() == 0) RegisterEventHandler<EventPlayerHurt>(EventDicePlayerVampireOnPlayerHurt);
            _playerVampires.Add(player);
            playerPawn.MaxHealth = Convert.ToInt32(config["max_health"]);
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName }
            };
        }

        private void DicePlayerVampireUnload()
        {
            DicePlayerVampireReset();
        }

        private void DicePlayerVampireReset()
        {
            DeregisterEventHandler<EventPlayerHurt>(EventDicePlayerVampireOnPlayerHurt);
            _playerVampires.Clear();
        }

        private void DicePlayerVampireResetForPlayer(CCSPlayerController player)
        {
            // check if player has this dice
            if (!_playerVampires.Contains(player)) return;
            // remove player
            _playerVampires.Remove(player);
            // remove listener if no players have this dice
            if (_playerVampires.Count == 0) DicePlayerVampireReset();
        }

        private HookResult EventDicePlayerVampireOnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            if (attacker == null || victim == null) return HookResult.Continue;
            if (!_playerVampires.Contains(attacker)) return HookResult.Continue;
            var playerPawn = attacker.PlayerPawn.Value;
            if (playerPawn == null) return HookResult.Continue;
            Dictionary<string, object> config = GetDiceConfig("DicePlayerVampire");
            playerPawn.Health += (int)float.Round(@event.DmgHealth);
            if (playerPawn.Health > Convert.ToInt32(config["max_health"])) playerPawn.Health = Convert.ToInt32(config["max_health"]);
            attacker.PrintToCenterAlert($"+{(int)float.Round(@event.DmgHealth)} health!");
            Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_iHealth");
            return HookResult.Continue;
        }

        private Dictionary<string, object> DicePlayerVampireConfig()
        {
            var config = new Dictionary<string, object>();
            config["max_health"] = (int)200;
            return config;
        }
    }
}
