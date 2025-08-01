using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;


namespace RollTheDice.Dices
{
    public class ChangePlayerSize : ParentDice
    {
        public override string ClassName => "ChangePlayerSize";
        public readonly Random _random = new();

        public ChangePlayerSize(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // get random player size
            float playerSize = float.Round((float)((_random.NextDouble()
                * ((float)_config.Dices.ChangePlayerSize.MaxSize
                - (float)_config.Dices.ChangePlayerSize.MinSize))
                + (float)_config.Dices.ChangePlayerSize.MinSize)
            , 2);
            // change player size
            ChangeSize(player, playerSize);
            // create GUI for player
            Dictionary<string, string> data = new()
            {
                { "playerName", player.PlayerName },
                { "playerSize", playerSize.ToString() }
            };
            _players.Add(player, new Dictionary<string, CPointWorldText?>
            {
                { "gui", CreateMainGUI(player, ClassName, data) }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            GUI.RemoveGUIs([.. _players[player].Values.Cast<CPointWorldText>()]);
            ChangeSize(player, 1f);
            _ = _players.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            // remove all GUIs for all players
            foreach (KeyValuePair<CCSPlayerController, Dictionary<string, CPointWorldText?>> kvp in _players)
            {
                ChangeSize(kvp.Key, 1f);
                GUI.RemoveGUIs([.. kvp.Value.Values.Cast<CPointWorldText>()]);
            }
            _players.Clear();
        }

        private static void ChangeSize(CCSPlayerController player, float size)
        {
            if (player?.Pawn?.Value?.CBodyComponent?.SceneNode == null
                || !player.Pawn.IsValid)
            {
                return;
            }
            player.Pawn.Value.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = size;
            player.Pawn.Value.AcceptInput("SetScale", null, null, size.ToString());
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_CBodyComponent");
        }
    }
}
