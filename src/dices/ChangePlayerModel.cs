using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;

namespace RollTheDice.Dices
{
    public class ChangePlayerModel : ParentDice
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
            // save old model
            _oldModels[player] = playerModel;
            // set new player model
            if (player.Team == CsTeam.Terrorist)
            {
                ChangeModel(player, "characters/models/ctm_sas/ctm_sas.vmdl");
            }
            else
            {
                ChangeModel(player, "characters/models/tm_phoenix/tm_phoenix.vmdl");
            }
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                //{ "gui", CreateMainGUI(player, ClassName, data) }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            ChangeModel(player, _oldModels[player]);
            _ = _players.Remove(player);
            _ = _oldModels.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            // remove all GUIs for all players
            foreach (KeyValuePair<CCSPlayerController, Dictionary<string, CPointWorldText?>> kvp in _players)
            {
                ChangeModel(kvp.Key, _oldModels[kvp.Key]);
            }
            _players.Clear();
            _oldModels.Clear();
        }

        private static void ChangeModel(CCSPlayerController player, string model)
        {
            if (player == null
                || player.Pawn == null
                || !player.Pawn.IsValid
                || player.Pawn.Value == null
                || player.LifeState != (byte)LifeState_t.LIFE_ALIVE)
            {
                return;
            }
            // reset player model
            player.Pawn.Value.SetModel(model);
        }
    }
}
