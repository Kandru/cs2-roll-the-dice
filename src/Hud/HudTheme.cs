using CounterStrikeSharp.API.Core;

namespace RollTheDice.Hud
{
    /// <summary>Applies config <c>theme</c> classes (<c>theme-*</c>) to the banner card.</summary>
    public static class HudTheme
    {
        /// <summary>Keep in sync with hud.vcss / preview (validated by <c>make panorama</c>). Rainbow order.</summary>
        public static readonly string[] All =
        [
            "red", "coral", "orange", "copper", "gold", "t", "lime", "green",
            "teal", "cyan", "blue", "ct", "purple", "magenta", "pink", "silver",
        ];

        private static readonly string[] Classes = BuildClasses();
        private static readonly int GoldIndex = ExactIndex("gold");
        private static int _activeIndex = GoldIndex;

        public static void BindConfig(PluginConfig config)
        {
            _activeIndex = ResolveIndex(config.Theme);
        }

        public static void Apply(CCSPlayerController player)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            int active = _activeIndex;
            for (int i = 0; i < Classes.Length; i++)
            {
                CustomHud.SetHasClass(player, Banner.Panel, Classes[i], i == active);
            }
        }

        private static string[] BuildClasses()
        {
            string[] classes = new string[All.Length];
            for (int i = 0; i < All.Length; i++)
            {
                classes[i] = "theme-" + All[i];
            }
            return classes;
        }

        private static int ExactIndex(string name)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i] == name)
                {
                    return i;
                }
            }
            return 0;
        }

        private static int ResolveIndex(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
            {
                return GoldIndex;
            }

            ReadOnlySpan<char> value = theme.AsSpan().Trim();
            for (int i = 0; i < All.Length; i++)
            {
                if (value.Equals(All[i], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return GoldIndex;
        }
    }
}
