using CounterStrikeSharp.API.Core;

namespace RollTheDice.Hud
{
    public static class Banner
    {
        public const string Panel = "Banner";
        public const string TextVar = "dice";

        public static void Show(CCSPlayerController player, string text)
        {
            if (!CustomHud.EnsureSpawned(player))
            {
                return;
            }

            CustomHud.SetText(player, Panel, TextVar, text);
            CustomHud.ShowPanel(player, Panel);
        }

        public static void Hide(CCSPlayerController player)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            CustomHud.HidePanel(player, Panel);
            CustomHud.SetText(player, Panel, TextVar, string.Empty);
        }

        public static void WriteDefaults(CCSPlayerController player)
        {
            CustomHud.SetText(player, Panel, TextVar, string.Empty);
            CustomHud.SetHasClass(player, Panel, "ph-fading", false);
            CustomHud.SetHasClass(player, Panel, "ph-off", true);
        }
    }
}
