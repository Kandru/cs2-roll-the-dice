using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;


namespace RollTheDice.Dices
{
    public class ChangePlayerSize : DiceBlueprint
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
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName },
                { "playerSize", playerSize.ToString() }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            ChangeSize(player, 1f);
            _ = _players.Remove(player);
        }

        public override void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", ClassName));
            foreach (CCSPlayerController player in _players)
            {
                ChangeSize(player, 1f);
            }
            _players.Clear();
        }

        private void ChangeSize(CCSPlayerController player, float size)
        {
            if (player?.Pawn?.Value?.CBodyComponent?.SceneNode == null
                || !player.Pawn.IsValid)
            {
                return;
            }
            player.Pawn.Value.CBodyComponent.SceneNode.GetSkeletonInstance().Scale = size;
            player.Pawn.Value.AcceptInput("SetScale", null, null, size.ToString());
            Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_CBodyComponent");
            // adjust health if enabled
            if (_config.Dices.ChangePlayerSize.AdjustHealth)
            {
                player.Pawn.Value.Health = (int)(100 * size);
                player.Pawn.Value.MaxHealth = (int)(100 * size);
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
                Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iMaxHealth");
            }
        }
    }
}
