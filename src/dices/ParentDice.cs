using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;
using RollTheDice.Utils;
using System.Drawing;

namespace RollTheDice.Dices
{
    public class ParentDice(PluginConfig GlobalConfig, MapConfig Config, IStringLocalizer Localizer)
    {
        public readonly PluginConfig _globalConfig = GlobalConfig;
        public readonly MapConfig _config = Config;
        public readonly IStringLocalizer _localizer = Localizer;
        public readonly Dictionary<CCSPlayerController, Dictionary<string, CPointWorldText?>> _players = [];
        public virtual string ClassName => "ParentDice";
        public virtual List<string> Events => [];
        public virtual List<string> Listeners => [];
        public virtual Dictionary<int, HookMode> UserMessages => [];
        public virtual List<string> Precache => [];

        public virtual void Add(CCSPlayerController player)
        {
            // check if player is valid and has a pawn
            if (player == null
                || !player.IsValid
                || player.Pawn?.Value == null
                || !player.Pawn.Value.IsValid)
            {
                return;
            }
            _players.Add(player, []);
        }

        public virtual void Remove(CCSPlayerController player)
        {
            GUI.RemoveGUIs([.. _players[player].Values.Cast<CPointWorldText>()]);
            _ = _players.Remove(player);
        }

        public virtual void Destroy()
        {
            Console.WriteLine(_localizer["dice.class.destroy"].Value.Replace("{name}", GetType().Name));
            // remove all GUIs for all players
            foreach (KeyValuePair<CCSPlayerController, Dictionary<string, CPointWorldText?>> kvp in _players)
            {
                GUI.RemoveGUIs([.. kvp.Value.Values.Cast<CPointWorldText>()]);
            }
            _players.Clear();
        }

        public CPointWorldText? CreateMainGUI(CCSPlayerController player, string dice, Dictionary<string, string> data)
        {
            return CreateGUI(player, dice, data, "gui");
        }

        public CPointWorldText? CreateStatusGUI(CCSPlayerController player, string dice, Dictionary<string, string> data)
        {
            return CreateGUI(player, dice, data, "status");
        }

        private CPointWorldText? CreateGUI(CCSPlayerController player, string dice, Dictionary<string, string> data, string suffix)
        {
            // check if message and gui position are valid
            if (_localizer[$"dice_{dice}_{suffix}"].ResourceNotFound
            || !_globalConfig.GUIPositions.TryGetValue(_globalConfig.GUIPosition, out GuiPositionConfig? value))
            {
                return null;
            }

            // create message with data replacement
            string message = _localizer["command.prefix"].Value + " " + _localizer[$"dice_{dice}_{suffix}"].Value;
            foreach (KeyValuePair<string, string> kvp in data)
            {
                message = message.Replace($"{{{kvp.Key}}}", kvp.Value);
            }

            // set color with fallback
            Color messageColor = Color.White;
            try
            {
                messageColor = ColorTranslator.FromHtml(value.MessageColor);
            }
            catch { }

            // return GUI
            return GUI.AddGUI(
            player: player,
            message: message,
            size: value.MessageFontSize,
            color: messageColor,
            font: value.MessageFont,
            shiftX: value.MessageShiftX,
            shiftY: value.MessageShiftY,
            drawBackground: value.MessageDrawBackground,
            backgroundFactor: value.MessageBackgroundFactor
            );
        }
    }
}