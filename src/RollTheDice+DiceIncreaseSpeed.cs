using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private Dictionary<CCSPlayerController, float> _playersWithIncreasedSpeed = new();

        private Dictionary<string, string> DiceIncreaseSpeed(CCSPlayerController player, CCSPlayerPawn playerPawn)
        {
            Dictionary<string, object> config = GetDiceConfig("DiceIncreaseSpeed");
            // create listener if not exists
            if (_playersWithIncreasedSpeed.Count() == 0)
            {
                RegisterEventHandler<EventPlayerHurt>(EventDiceIncreaseSpeedOnPlayerHurt);
                RegisterEventHandler<EventPlayerFalldamage>(EventDiceIncreaseSpeedOnPlayerFalldamage);
            }
            var speedIncrease = _random.NextDouble() * ((float)config["max_speed"] - (float)config["min_speed"]) + (float)config["min_speed"];
            playerPawn.VelocityModifier *= (float)speedIncrease;
            _playersWithIncreasedSpeed.Add(player, (float)playerPawn.VelocityModifier);
            Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_flVelocityModifier");
            var percentageIncrease = (speedIncrease - 1.0) * 100;
            return new Dictionary<string, string>
            {
                { "playerName", player.PlayerName },
                { "percentageIncrease", Math.Round(percentageIncrease, 2).ToString() }
            };
        }

        private void DiceIncreaseSpeedUnload()
        {
            DiceIncreaseSpeedReset();
        }

        private void DiceIncreaseSpeedReset()
        {
            DeregisterEventHandler<EventPlayerHurt>(EventDiceIncreaseSpeedOnPlayerHurt);
            DeregisterEventHandler<EventPlayerFalldamage>(EventDiceIncreaseSpeedOnPlayerFalldamage);
            // iterate through all players
            Dictionary<CCSPlayerController, float> _playersWithIncreasedSpeedCopy = new(_playersWithIncreasedSpeed);
            foreach (var kvp in _playersWithIncreasedSpeedCopy)
            {
                try
                {
                    if (kvp.Key == null
                        || kvp.Key.PlayerPawn == null
                        || !kvp.Key.PlayerPawn.IsValid
                        || kvp.Key.PlayerPawn.Value == null
                        || kvp.Key.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
                    // get player pawn
                    var playerPawn = kvp.Key.PlayerPawn.Value!;
                    // reset player speed
                    playerPawn.VelocityModifier = 1.0f;
                    // set state changed
                    Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_flVelocityModifier");
                }
                catch
                {
                    // do nothing
                }
            }
            _playersWithIncreasedSpeed.Clear();
        }

        private void DiceIncreaseSpeedResetForPlayer(CCSPlayerController player)
        {
            if (player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null) return;
            if (!_playersWithIncreasedSpeed.ContainsKey(player)) return;
            player.PlayerPawn.Value.VelocityModifier = 1.0f;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            _playersWithIncreasedSpeed.Remove(player);
        }

        private HookResult EventDiceIncreaseSpeedOnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null) return HookResult.Continue;
            if (!_playersWithIncreasedSpeed.ContainsKey(victim)) return HookResult.Continue;
            if (victim == null || victim.PlayerPawn == null || !victim.PlayerPawn.IsValid || victim.PlayerPawn.Value == null || victim.LifeState != (byte)LifeState_t.LIFE_ALIVE) return HookResult.Continue;
            Server.NextFrame(() =>
            {
                if (victim == null
                    || !victim.IsValid
                    || victim.PlayerPawn == null
                    || !victim.PlayerPawn.IsValid
                    || !_playersWithIncreasedSpeed.ContainsKey(victim)) return;
                CCSPlayerPawn playerPawn = victim.PlayerPawn.Value!;
                // set player speed
                playerPawn.VelocityModifier = _playersWithIncreasedSpeed[victim];
                // set state changed
                Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_flVelocityModifier");
            });
            return HookResult.Continue;
        }

        private HookResult EventDiceIncreaseSpeedOnPlayerFalldamage(EventPlayerFalldamage @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            if (victim == null) return HookResult.Continue;
            if (!_playersWithIncreasedSpeed.ContainsKey(victim)) return HookResult.Continue;
            if (victim == null || victim.PlayerPawn == null || !victim.PlayerPawn.IsValid || victim.PlayerPawn.Value == null || victim.LifeState != (byte)LifeState_t.LIFE_ALIVE) return HookResult.Continue;
            Server.NextFrame(() =>
            {
                if (victim == null
                    || !victim.IsValid
                    || victim.PlayerPawn == null
                    || !victim.PlayerPawn.IsValid
                    || !_playersWithIncreasedSpeed.ContainsKey(victim)) return;
                CCSPlayerPawn playerPawn = victim.PlayerPawn.Value!;
                // set player speed
                playerPawn.VelocityModifier = _playersWithIncreasedSpeed[victim];
                // set state changed
                Utilities.SetStateChanged(playerPawn, "CCSPlayerPawn", "m_flVelocityModifier");
            });
            return HookResult.Continue;
        }

        private Dictionary<string, object> DiceIncreaseSpeedConfig()
        {
            var config = new Dictionary<string, object>();
            config["min_speed"] = (float)1.5f;
            config["max_speed"] = (float)2.0f;
            return config;
        }
    }
}
