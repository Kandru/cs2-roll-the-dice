using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class ChickenLeader : ParentDice
    {
        public override string _className => "ChickenLeader";
        public readonly Random _random = new();

        public ChickenLeader(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", _className));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.IsValid == false
                || player.Pawn?.Value?.CBodyComponent?.SceneNode?.GetSkeletonInstance() == null)
            {
                return;
            }
            // spawn chicken
            SpawnChicken(player);
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                { "gui", CreateMainGUI(player, _className, data) }
            });
        }

        private void SpawnChicken(CCSPlayerController player)
        {
            if (player?.Pawn?.Value?.AbsOrigin == null
                || !player.Pawn.IsValid)
            {
                return;
            }
            // spawn chickens
            for (int i = 0; i < _config.Dices.ChickenLeader.Amount; i++)
            {
                CChicken? chicken = Utilities.CreateEntityByName<CChicken>("chicken");
                if (chicken != null)
                {
                    Vector offset = new Vector(
                        (float)(100 * Math.Cos(2 * Math.PI * i / _config.Dices.ChickenLeader.Amount)),
                        (float)(100 * Math.Sin(2 * Math.PI * i / _config.Dices.ChickenLeader.Amount)),
                        0
                    );
                    chicken.Teleport(player.Pawn.Value.AbsOrigin + offset, player.Pawn.Value.AbsRotation, player.Pawn.Value.AbsVelocity);
                    chicken.DispatchSpawn();
                }
            }
        }
    }
}
