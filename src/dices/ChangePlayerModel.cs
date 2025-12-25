using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;

namespace RollTheDice.Dices
{
    public class ChangePlayerModel : DiceBlueprint
    {
        public override string ClassName => "ChangePlayerModel";
        private readonly Dictionary<CCSPlayerController, string> _oldModels = [];

        public ChangePlayerModel(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
        {
            Console.WriteLine(_localizer["dice.class.initialize"].Value.Replace("{name}", ClassName));
        }

        public override void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid
                || player.Pawn?.Value?.CBodyComponent?.SceneNode?.GetSkeletonInstance() == null)
            {
                return;
            }
            // get current player model
            string playerModel = player.Pawn.Value.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName;
            // stop if player model could not be saved
            if (playerModel == "")
            {
                return;
            }
            // save old model
            _oldModels[player] = playerModel;
            // set new player model
            if (player.Team == CsTeam.Terrorist)
            {
                ChangeModel(player, _config.Dices.ChangePlayerModel.TModel);
            }
            else
            {
                ChangeModel(player, _config.Dices.ChangePlayerModel.CTModel);
            }
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            ChangeModel(player, _oldModels[player]);
            _ = _players.Remove(player);
            _ = _oldModels.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players.ToList())
            {
                Remove(player);
            }
            _players.Clear();
            _oldModels.Clear();
        }

        public override void Destroy()
        {
            Reset();
        }

        private static void ChangeModel(CCSPlayerController? player, string model)
        {
            if (player?.Pawn?.IsValid == false
                || player?.Pawn?.Value == null)
            {
                return;
            }
            // reset player model
            player.Pawn.Value.SetModel(model);
        }
    }
}
