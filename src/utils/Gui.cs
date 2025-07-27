using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Drawing;

namespace RollTheDice.Utils
{
    public static class GUI
    {
        public static void ChangeColor(CPointWorldText worldText, string color)
        {
            try
            {
                worldText.Color = ColorTranslator.FromHtml(color);
            }
            catch
            {
                worldText.Color = Color.White;
            }
            Utilities.SetStateChanged(worldText, "CPointWorldText", "m_Color");
        }

        public static CPointWorldText? AddGUI(CCSPlayerController player, string message, int size, Color color, string font, float shiftX, float shiftY, bool drawBackground, float backgroundFactor)
        {
            return WorldTextManager.Create(
                player: player,
                message: message,
                size: size,
                color: color,
                font: font,
                shiftX: shiftX,
                shiftY: shiftY,
                drawBackground: drawBackground,
                backgroundFactor: backgroundFactor
            );
        }

        public static void ChangeGUI(CPointWorldText worldText, string message)
        {
            if (worldText == null || !worldText.IsValid)
            {
                return;
            }
            worldText.AcceptInput("SetMessage", worldText, worldText, message);
        }

        public static void RemoveGUIs(List<CPointWorldText> worldTexts)
        {
            if (worldTexts == null || worldTexts.Count == 0)
            {
                return;
            }
            foreach (CPointWorldText worldText in worldTexts)
            {
                RemoveGUI(worldText);
            }
        }

        public static void RemoveGUI(CPointWorldText? worldText)
        {
            if (worldText == null || !worldText.IsValid)
            {
                return;
            }
            worldText.AcceptInput("kill");
        }
    }
}
