using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class Glow : DiceBlueprint
    {
        public override string ClassName => "Glow";
        public readonly Random _random = new();
        public readonly Dictionary<CCSPlayerController, (CDynamicProp?, CDynamicProp?)> _playerGlows = [];

        public Glow(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            _playerGlows.Add(player, CreateGlow(player.Pawn.Value, player.TeamNum == (int)CsTeam.Terrorist ? Color.Red : Color.Blue));
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            if (_playerGlows.TryGetValue(player, out (CDynamicProp?, CDynamicProp?) glow))
            {
                RemoveGlow(glow.Item1, glow.Item2);
            }
            _ = _players.Remove(player);
            _ = _playerGlows.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
            _playerGlows.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        public static (CDynamicProp?, CDynamicProp?) CreateGlow(CBaseEntity entity, Color color)
        {
            CDynamicProp? _glowProxy = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            CDynamicProp? _glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (_glowProxy == null
                || _glow == null)
            {
                return (null, null);
            }

            string modelName = entity.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;
            // create proxy
            _glowProxy.Spawnflags = 256u;
            _glowProxy.RenderMode = RenderMode_t.kRenderNone;
            _glowProxy.SetModel(modelName);
            _glowProxy.AcceptInput("FollowEntity", entity, _glowProxy, "!activator");
            _glowProxy.DispatchSpawn();
            // create glow
            _glow.SetModel(modelName);
            _glow.AcceptInput("FollowEntity", _glowProxy, _glow, "!activator");
            _glow.DispatchSpawn();
            _glow.Render = Color.FromArgb(255, 255, 255, 255);
            _glow.Glow.GlowColorOverride = color;
            _glow.Spawnflags = 256u;
            _glow.RenderMode = RenderMode_t.kRenderGlow;
            _glow.Glow.GlowRange = 5000;
            _glow.Glow.GlowTeam = -1;
            _glow.Glow.GlowType = 3;
            _glow.Glow.GlowRangeMin = 20;
            return (_glowProxy, _glow);
        }

        public static void RemoveGlow(CBaseEntity? glowProxy, CBaseEntity? glow)
        {
            Entities.RemoveEntity(glowProxy);
            Entities.RemoveEntity(glow);
        }
    }
}
