using CounterStrikeSharp.API.Core;
using System.Drawing;

namespace RollTheDice
{
    public partial class RollTheDice : BasePlugin
    {
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
            var positions = new Dictionary<string, (
                string MessageFont,
                int MessageFontSize,
                string MessageColor,
                float MessageShiftX,
                float MessageShiftY,
                bool MessageDrawBackground,
                float MessageBackgroundFactor,
                string StatusFont,
                int StatusFontSize,
                string StatusColor,
                float StatusShiftX,
                float StatusShiftY,
                bool StatusDrawBackground,
                float StatusBackgroundFactor
            )>
            {
                { "top_center", (
                    "Arial Black Standard",
                    32,
                    "white",
                    -2.9f,
                    4.0f,
                    true,
                    1.0f,
                    "Arial Black Standard",
                    30,
                    "red",
                    -2.85f,
                    3.7f,
                    true,
                    1.0f
                ) },
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
                        MessageDrawBackground = position.Value.MessageDrawBackground,
                        MessageBackgroundFactor = position.Value.MessageBackgroundFactor,
                        StatusFont = position.Value.StatusFont,
                        StatusFontSize = position.Value.StatusFontSize,
                        StatusColor = position.Value.StatusColor,
                        StatusShiftX = position.Value.StatusShiftX,
                        StatusShiftY = position.Value.StatusShiftY,
                        StatusDrawBackground = position.Value.StatusDrawBackground,
                        StatusBackgroundFactor = position.Value.StatusBackgroundFactor
                    };
                }
            }
        }
    }
}
