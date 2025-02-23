using CounterStrikeSharp.API.Core;
using System.Drawing;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
        private CPointWorldText? CreateGUI(CCSPlayerController player, string message, int size = 100, Color? color = null, string font = "", float shiftX = 0f, float shiftY = 0f, bool drawBackground = true, float backgroundFactor = 1f)
        {
            return WorldTextManager.Create(
                player,
                message,
                size,
                color,
                font,
                shiftX,
                shiftY,
                drawBackground,
                backgroundFactor
            );
        }

        private void RemoveAllGUIs()
        {
            foreach (CCSPlayerController player in _playersThatRolledTheDice.Keys)
            {
                if (player != null) RemoveGUI(player);
            }
        }

        private void RemoveGUI(CCSPlayerController player)
        {
            if (!_playersThatRolledTheDice.ContainsKey(player)) return;
            // remove gui message
            if (_playersThatRolledTheDice[player].ContainsKey("gui_message")
                && (CPointWorldText)_playersThatRolledTheDice[player]["gui_message"] != null)
            {
                CPointWorldText worldText = (CPointWorldText)_playersThatRolledTheDice[player]["gui_message"];
                worldText.AcceptInput("Kill");
            }
            // remove gui status
            if (_playersThatRolledTheDice[player].ContainsKey("gui_status")
                && (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"] != null)
            {
                CPointWorldText worldText = (CPointWorldText)_playersThatRolledTheDice[player]["gui_status"];
                worldText.AcceptInput("Kill");
            }
        }

        private void CheckGUIConfig()
        {
            var positions = new Dictionary<string, (string MessageFont, int MessageFontSize, string MessageColor, float MessageShiftX, float MessageShiftY, string StatusFont, int StatusFontSize, string StatusColor, float StatusShiftX, float StatusShiftY)>
            {
                { "top_center", ("Verdana", 40, "Purple",-2.9f, 4.4f, "Verdana", 30, "Red", -2.75f, 4.0f) },
            };

            foreach (var position in positions)
            {
                if (!Config.GUIPositions.ContainsKey(position.Key))
                {
                    Config.GUIPositions[position.Key] = new GuiPositionConfig
                    {
                        MessageFont = position.Value.MessageFont,
                        MessageFontSize = position.Value.MessageFontSize,
                        MessageColor = position.Value.MessageColor,
                        MessageShiftX = position.Value.MessageShiftX,
                        MessageShiftY = position.Value.MessageShiftY,
                        StatusFont = position.Value.StatusFont,
                        StatusFontSize = position.Value.StatusFontSize,
                        StatusColor = position.Value.StatusColor,
                        StatusShiftX = position.Value.StatusShiftX,
                        StatusShiftY = position.Value.StatusShiftY,
                    };
                }
            }
        }
    }
}
