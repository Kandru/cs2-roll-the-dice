using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Dices
{
    public class ChangePlayerModel : DiceBlueprint
    {
        public override string ClassName => "ChangePlayerModel";
        public override List<string> Events => [
            "EventPlayerDeath"
        ];
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
            _players.Add(player);
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player)
        {
            ChangeModel(player, _oldModels[player]);
            _ = _players.Remove(player);
            _ = _oldModels.Remove(player);
        }

        public override void Reset()
        {
            foreach (CCSPlayerController player in _players)
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

        public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !_players.Contains(player)
                || player.PlayerPawn?.Value?.WeaponServices == null)
            {
                return HookResult.Continue;
            }
            Remove(player);
            return HookResult.Continue;
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
