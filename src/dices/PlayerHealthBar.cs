using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class PlayerHealthBar : DiceBlueprint
    {
        public override string ClassName => "PlayerHealthBar";
        public override List<string> Events => [
            "EventPlayerHurt"
        ];
        public override List<string> Listeners => [
        //    "OnTick" -> disabled for now due to error Invalid argument type(s) supplied to Virtual Function (GetPickerEntity)
        ];
        private Dictionary<CCSPlayerController, Dictionary<CCSPlayerPawn, float>> _playersHealthbar = [];

        public PlayerHealthBar(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player);
            _playersHealthbar.Add(player, []);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            _ = _playersHealthbar.Remove(player);
        }

        public override void Reset()
        {
            _players.Clear();
            _playersHealthbar.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? victim = @event.Userid;
            CCSPlayerController? attacker = @event.Attacker;

            // Early exits with single validation
            if (victim?.PlayerPawn?.Value is not { IsValid: true } victimPawn
            || attacker?.IsValid != true
            || victim.TeamNum == attacker.TeamNum
            || victim == attacker
            || !_playersHealthbar.ContainsKey(attacker))
            {
                return HookResult.Continue;
            }

            float newHealth = @event.Health;
            float oldHealth = newHealth + @event.DmgHealth;

            if (oldHealth == newHealth) return HookResult.Continue;

            // send message
            var message = UserMessage.FromPartialName("UpdateScreenHealthBar");
            message.SetInt("entidx", (int)victim.PlayerPawn.Index);
            message.SetFloat("healthratio_old", oldHealth / victimPawn.MaxHealth);
            message.SetFloat("healthratio_new", newHealth / victimPawn.MaxHealth);
            message.SetInt("style", 0);
            message.Send(attacker);
            return HookResult.Continue;
        }

        public void OnTick()
        {
            if (_playersHealthbar.Count == 0
                || Server.TickCount % 8 != 0
                || GameRules._gameRules == null)
            {
                return;
            }

            // worker
            foreach (KeyValuePair<CCSPlayerController, Dictionary<CCSPlayerPawn, float>> playerEntry in _playersHealthbar.ToDictionary())
            {
                CCSPlayerController player = playerEntry.Key;
                Dictionary<CCSPlayerPawn, float> healthBarData = playerEntry.Value;

                // sanity checks
                if (player?.PlayerPawn?.Value is not { IsValid: true, LifeState: (byte)LifeState_t.LIFE_ALIVE })
                {
                    continue;
                }

                // check if player is aiming at another player
                CCSPlayerPawn? playerTarget = GameRules._gameRules.FindPickerEntity<CCSPlayerPawn>(player);
                if (playerTarget is not { IsValid: true, LifeState: (byte)LifeState_t.LIFE_ALIVE, Health: > 0 }
                    || playerTarget.TeamNum == player.TeamNum
                    || playerTarget.DesignerName != "player")
                {
                    continue;
                }

                // check if we should resend the message (only every 2 seconds)
                if (healthBarData.TryGetValue(playerTarget, out float lastSentTime))
                {
                    if (lastSentTime + 2.0f > Server.CurrentTime)
                    {
                        continue;
                    }

                    healthBarData[playerTarget] = Server.CurrentTime;
                }
                else
                {
                    healthBarData.Add(playerTarget, Server.CurrentTime);
                }

                // send message
                float healthRatio = (float)playerTarget.Health / playerTarget.MaxHealth;
                UserMessage message = UserMessage.FromPartialName("UpdateScreenHealthBar");
                message.SetInt("entidx", (int)playerTarget.Index);
                message.SetFloat("healthratio_old", healthRatio);
                message.SetFloat("healthratio_new", healthRatio);
                message.SetInt("style", 0);
                message.Send(player);
            }
        }
    }
}
