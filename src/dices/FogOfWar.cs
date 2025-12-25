using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Enums;
using System.Drawing;

// based on https://gist.github.com/21Joakim/b5eb46bd9acb117349e9d76d3971f34e
namespace RollTheDice.Dices
{
    public class FogOfWar : DiceBlueprint
    {
        public override string ClassName => "FogOfWar";
        public override List<string> Listeners => [];

        public FogOfWar(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer) : base(GlobalConfig, Config, Localizer)
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
            // get fog controller
            CFogController? fogController = GetOrCreateFogController(player);
            if (fogController == null)
            {
                // do nothing if fog controller does not exist
                return;
            }
            // set fog parameters
            fogController.Fog.Enable = true;
            // set color from hex code and default to Color.DarkGray if invalid
            Color color = ColorTranslator.FromHtml(_config.Dices.FogOfWar.Color);
            if (color == Color.Empty)
            {
                color = Color.DarkOrange;
            }
            fogController.Fog.ColorPrimary = color;
            //fogController.Fog.ColorSecondary = Color.DarkOrange;
            fogController.Fog.Exponent = _config.Dices.FogOfWar.Exponent;
            fogController.Fog.Maxdensity = _config.Dices.FogOfWar.Density;
            fogController.Fog.End = _config.Dices.FogOfWar.EndDistance;
            // adjust player visibility
            ChangePlayerVisibility(_config.Dices.FogOfWar.PlayerVisibility);
            // activate fog controller for player only
            player.Pawn.Value.AcceptInput("SetFogController", fogController, fogController, "!activator");
            // add player to list
            _players.Add(player);
            // notify players
            NotifyPlayers(player, ClassName, new()
            {
                { "playerName", player.PlayerName }
            });
        }

        public override void Remove(CCSPlayerController player, DiceRemoveReason reason = DiceRemoveReason.GameLogic)
        {
            _ = _players.Remove(player);
            // remove fog controller (if any)
            CFogController? fogController = GetOrCreateFogController(player);
            if (fogController == null)
            {
                return;
            }
            fogController.Remove();
        }

        public override void Reset()
        {
            // remove fog for players
            foreach (CCSPlayerController player in _players)
            {
                Remove(player, DiceRemoveReason.GameLogic);
            }
        }

        public override void Destroy()
        {
            Reset();
        }

        private CFogController? GetOrCreateFogController(CCSPlayerController player)
        {
            string fogControllerName = $"PlayerFogController_{player.Slot}";
            // check if fog controller already exists
            foreach (CFogController? entry in Utilities.FindAllEntitiesByDesignerName<CFogController>("env_fog_controller"))
            {
                if (entry.Entity!.Name == fogControllerName)
                {
                    return entry;
                }
            }
            // create fog controller
            CFogController? envFogController = Utilities.CreateEntityByName<CFogController>("env_fog_controller");
            if (envFogController == null)
            {
                return null;
            }
            // spawn fog controller
            envFogController.Entity!.Name = fogControllerName;
            envFogController.DispatchSpawn();
            return envFogController;
        }

        private void ChangePlayerVisibility(float visibility = 0.9f)
        {
            // try to get env_player_visibility if available
            CPlayerVisibility? envPlayerVisibility = Utilities.FindAllEntitiesByDesignerName<CPlayerVisibility>("env_player_visibility").FirstOrDefault();
            if (envPlayerVisibility == null)
            {
                // try to create env_player_visibility if not available
                envPlayerVisibility = Utilities.CreateEntityByName<CPlayerVisibility>("env_player_visibility");
                if (envPlayerVisibility == null)
                {
                    return;
                }
                envPlayerVisibility.DispatchSpawn();
            }
            // set visibility strength
            envPlayerVisibility.FogMaxDensityMultiplier = visibility;
            Utilities.SetStateChanged(envPlayerVisibility, "CPlayerVisibility", "m_flFogMaxDensityMultiplier");
        }
    }
}