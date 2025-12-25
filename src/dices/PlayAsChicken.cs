using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class PlayAsChicken : DiceBlueprint
    {
        public override string ClassName => "PlayAsChicken";
        public override List<string> Listeners => [
            "OnTick",
            "CheckTransmit",
            "OnPlayerButtonsChanged"
        ];
        public override Dictionary<int, HookMode> UserMessages => new()
        {
            { 208, HookMode.Pre },
        };
        private readonly Random _random = new(Guid.NewGuid().GetHashCode());
        private Dictionary<CCSPlayerController, Dictionary<string, object>> _chickens = [];
        private readonly string _playersAsChickenModel = "models/chicken/chicken.vmdl";
        private readonly Dictionary<string, uint> _chickenSounds = new()
        {
            { "Chicken.Idle", 4037203721 },
            { "Chicken.Panic", 2004932112 },
        };

        public PlayAsChicken(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            CDynamicProp? prop = Entities.CreatePropEntity(
                player.Pawn.Value.AbsOrigin!,
                new QAngle(0, player.Pawn.Value.V_angle.Y, 0),
                _playersAsChickenModel,
                5f,
                player.Pawn.Value
            );
            // do nothing if the chicken could not be created
            if (prop == null
                || !prop.IsValid)
            {
                return;
            }
            _players.Add(player);
            _chickens.Add(player, []);
            _chickens[player]["next_sound"] = (int)Server.CurrentTime;
            _chickens[player]["prop"] = prop;
            SetPlayerVisibility(player, 0);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            SetPlayerVisibility(player, 255);
            Entities.RemoveEntity((CDynamicProp)_chickens[player]["prop"]);
            _ = _players.Remove(player);
            _ = _chickens.Remove(player);
        }

        public override void Reset()
        {
            foreach (var kvp in _chickens)
            {
                Remove(kvp.Key);
            }
            _players.Clear();
            _chickens.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public void OnTick()
        {
            if (_chickens.Count == 0)
            {
                return;
            }
            Dictionary<CCSPlayerController, Dictionary<string, object>> _chickensCopy = new(_chickens);
            foreach (var kvp in _chickensCopy)
            {
                // sanity checks
                if (kvp.Key == null
                || kvp.Key.PlayerPawn == null || !kvp.Key.PlayerPawn.IsValid || kvp.Key.PlayerPawn.Value == null
                || kvp.Key.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE
                || !kvp.Value.ContainsKey("prop")) continue;
                // make sound if time
                if ((int)_chickensCopy[kvp.Key]["next_sound"] <= (int)Server.CurrentTime)
                {
                    kvp.Key.EmitSound(_chickenSounds.ElementAt(_random.Next(_chickenSounds.Count)).Key);
                    _chickens[kvp.Key]["next_sound"] = (int)Server.CurrentTime + _random.Next(
                        _config.Dices.PlayAsChicken.MinSoundWaitTime,
                        _config.Dices.PlayAsChicken.MaxSoundWaitTime);
                }
            }
        }

        public void CheckTransmit(CCheckTransmitInfoList infoList)
        {
            if (_chickens.Count == 0)
            {
                return;
            }
            // check for the chicken of the players and hide the chicken for the players
            foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
            {
                // do nothing when player is invalid
                if (player == null
                    || !player.IsValid
                    || player.IsBot
                    || !_chickens.ContainsKey(player)) continue;
                var prop = (CDynamicProp)_chickens[player]["prop"];
                if (prop == null
                    || !prop.IsValid) continue;
                info.TransmitEntities.Remove(prop);
            }
        }

        public void OnPlayerButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (player == null
                || !player.IsValid
                || !_chickens.ContainsKey(player)
                || player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null)
            {
                return;
            }

            if (pressed == PlayerButtons.Duck)
            {
                ((CDynamicProp)_chickens[player]["prop"]).Teleport(
                    new Vector(
                        player.PlayerPawn.Value.AbsOrigin!.X,
                        player.PlayerPawn.Value.AbsOrigin!.Y,
                        player.PlayerPawn.Value.AbsOrigin!.Z - 18
                    )
                );
            }
            else if (released == PlayerButtons.Duck)
            {
                ((CDynamicProp)_chickens[player]["prop"]).Teleport(
                    new Vector(
                        player.PlayerPawn.Value.AbsOrigin!.X,
                        player.PlayerPawn.Value.AbsOrigin!.Y,
                        player.PlayerPawn.Value.AbsOrigin!.Z
                    )
                );
            }
        }

        public HookResult HookUserMessage208(UserMessage um)
        {
            var soundevent = um.ReadUInt("soundevent_hash");
            var soundevent_guid = um.ReadUInt("soundevent_guid");
            if (!_chickenSounds.ContainsValue(soundevent))
            {
                return HookResult.Continue;
            }
            // SosSetSoundEventParams
            var extend = UserMessage.FromId(210);
            extend.SetUInt("soundevent_guid", soundevent_guid);
            var volumeBytes = GetSoundVolumeParams(_config.Dices.PlayAsChicken.SoundVolume);
            // volume -> 0.5f [0xE9, 0x54, 0x60, 0xBD, 0x08, 0x04, 0x00, 0x00, 0x00, 0x00, 0x3F]
            // volume -> 2f [0xE9, 0x54, 0x60, 0xBD, 0x08, 0x04, 0x00, 0x00, 0x00, 0x00, 0x40]
            var packedParams = new byte[] { 0xE9, 0x54, 0x60, 0xBD, 0x08, 0x04, 0x00 }.Concat(volumeBytes).ToArray();
            extend.SetBytes("packed_params", packedParams);
            extend.Recipients = um.Recipients;
            extend.Send();
            return HookResult.Continue;
        }

        private byte[] GetSoundVolumeParams(float volume) => BitConverter.GetBytes(volume);

        private void SetPlayerVisibility(CCSPlayerController? player, int alpha)
        {
            if (player?.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            player.Pawn.Value.Render = Color.FromArgb(alpha, 255, 255, 255);
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseModelEntity", "m_clrRender");
        }
    }
}
