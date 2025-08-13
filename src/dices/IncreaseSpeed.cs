using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class IncreaseSpeed : DiceBlueprint
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
            float speedIncrease = (_random.NextSingle() *
                (_config.Dices.IncreaseSpeed.MaxSpeed - _config.Dices.IncreaseSpeed.MinSpeed)) +
                _config.Dices.IncreaseSpeed.MinSpeed;
            _playerSpeed.Add(player, speedIncrease);
            _players.Add(player);
            // increase speed
            SetPlayerSpeed(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "percentageIncrease", Math.Round((speedIncrease - 1.0) * 100, 2).ToString() }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player.PlayerPawn?.Value?.IsValid == true)
            {
                SetPlayerSpeed(player, 1f);
            }
            _ = _playerSpeed.Remove(player);
            _ = _players.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            _playerSpeed.Clear();
            _players.Clear();
            foreach (CCSPlayerController player in _players)
            {
                if (player.PlayerPawn?.Value?.IsValid == true)
                {
                    SetPlayerSpeed(player, 1f);
                }
            }
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
            if (!_config.Dices.IncreaseSpeed.ResetOnHostageRescue)
            {
                return HookResult.Continue;
            }
            SetPlayerSpeed(@event.Userid, 1f);
            return HookResult.Continue;
        }

        public HookResult EventHostageStopsFollowing(EventHostageStopsFollowing @event, GameEventInfo info)
        {
            SetPlayerSpeed(@event.Userid);
            return HookResult.Continue;
        }

        private void SetPlayerSpeed(CCSPlayerController? player, float speed = -1f)
        {
            if (player?.IsValid != true
            || !_players.Contains(player)
            || player.PlayerPawn?.Value?.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                return;
            }
            // delay 3 frames to ensure the velocity modifier is set correctly
            Server.NextFrame(() =>
            {
                Server.NextFrame(() =>
                {
                    Server.NextFrame(() =>
                    {
                        if (player.PlayerPawn?.Value?.IsValid == true)
                        {
                            player.PlayerPawn.Value.VelocityModifier = speed >= 0 ? speed : _playerSpeed[player];
                            Utilities.SetStateChanged(player.PlayerPawn.Value, "CCSPlayerPawn", "m_flVelocityModifier");
                        }
                    });
                });
            });
        }
    }
}
