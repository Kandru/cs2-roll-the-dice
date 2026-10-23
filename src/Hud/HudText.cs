using System.Text;

namespace RollTheDice.Hud
{
    /// <summary>Player supplied text (names, the perk search) before it reaches a Panorama label.</summary>
    public static class HudText
    {
        /// <summary>Longest player name shown (Steam allows 32 characters).</summary>
        public const int MaxNameLength = 32;

        /// <summary>
        /// Control characters removed (a line break would break the fixed row layout) and cut to
        /// <paramref name="maxLength"/>. Returns the same string when nothing changes, so the per-second repaints of
        /// the round list do not allocate.
        /// </summary>
        public static string Clean(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            bool clean = text.Length <= maxLength;
            for (int i = 0; clean && i < text.Length; i++)
            {
                clean = !char.IsControl(text[i]);
            }
            if (clean)
            {
                return text;
            }

            StringBuilder result = new(Math.Min(text.Length, maxLength));
            foreach (char c in text)
            {
                if (result.Length >= maxLength)
                {
                    break;
                }
                if (!char.IsControl(c))
                {
                    _ = result.Append(c);
                }
            }

            // never end on half of a surrogate pair
            if (result.Length > 0 && char.IsHighSurrogate(result[^1]))
            {
                result.Length--;
            }
            return result.ToString().Trim();
        }
    }
}
