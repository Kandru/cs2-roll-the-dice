using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class IncreaseSpeed : ParentDice
    {
        public override string ClassName => "IncreaseSpeed";
        public override List<string> Events => [
            "EventPlayerHurt",
            "EventPlayerFalldamage",
            "EventHostageFollows",
            "EventHostageStopsFollowing"
        ];
        public readonly Random _random = new();
        public readonly Dictionary<CCSPlayerController, float> _playerSpeed = [];

        public IncreaseSpeed(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.PlayerPawn?.Value == null
                || !player.PlayerPawn.Value.IsValid)
            {
                return;
            }
            // get random amount of speed to increase
            float speedIncrease = _random.NextSingle() *
                (_config.Dices.IncreaseSpeed.MaxSpeed - _config.Dices.IncreaseSpeed.MinSpeed) +
                _config.Dices.IncreaseSpeed.MinSpeed;
            // increase speed
            player.PlayerPawn.Value.VelocityModifier *= (float)speedIncrease;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
            _playerSpeed.Add(player, speedIncrease);
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName },
                { "percentageIncrease", Math.Round((speedIncrease - 1.0) * 100, 2).ToString() }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                { "gui", CreateMainGUI(player, ClassName, data) }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            GUI.RemoveGUIs([.. _players[player].Values.Cast<CPointWorldText>()]);
            _ = _playerSpeed.Remove(player);
            _ = _players.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            // remove all GUIs for all players
            foreach (KeyValuePair<CCSPlayerController, Dictionary<string, CPointWorldText?>> kvp in _players)
            {
                GUI.RemoveGUIs([.. kvp.Value.Values.Cast<CPointWorldText>()]);
            }
            _playerSpeed.Clear();
            _players.Clear();
        }

        public HookResult EventPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            SetPlayerSpeed(@event.Userid);
            return HookResult.Continue;
        }

        public HookResult EventPlayerFalldamage(EventPlayerFalldamage @event, GameEventInfo info)
        {
            SetPlayerSpeed(@event.Userid);
            return HookResult.Continue;
        }

        public HookResult EventHostageFollows(EventHostageFollows @event, GameEventInfo info)
        {
            SetPlayerSpeed(@event.Userid);
            return HookResult.Continue;
        }

        public HookResult EventHostageStopsFollowing(EventHostageStopsFollowing @event, GameEventInfo info)
        {
            SetPlayerSpeed(@event.Userid);
            return HookResult.Continue;
        }

        private void SetPlayerSpeed(CCSPlayerController? player)
        {
            if (player?.IsValid != true
            || !_players.ContainsKey(player)
            || player.PlayerPawn?.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
                return;

            Server.NextFrame(() =>
            {
                if (player.PlayerPawn?.Value?.IsValid == true)
                {
                    player.PlayerPawn.Value.VelocityModifier = _playerSpeed[player];
                    Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                }
            });
        }
    }
}
