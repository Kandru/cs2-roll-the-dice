using System.Text.RegularExpressions;

namespace RollTheDice.Utils
{
    /// <summary>
    /// "sounds/..." paths from the config are played with the client command "play &lt;path&gt;". A path with ";",
    /// quotes or a line break would chain further client commands, so only plain paths are played.
    /// </summary>
    public static partial class SoundPath
    {
        [GeneratedRegex(@"^sounds/[A-Za-z0-9_\-./]+$")]
        private static partial Regex Plain();

        private static readonly HashSet<string> _warned = [];

        public static bool IsSafe(string path)
        {
            if (Plain().IsMatch(path) && !path.Contains("..", StringComparison.Ordinal))
            {
                return true;
            }

            // once per value: the sound is played on every roll
            if (_warned.Add(path))
            {
                Console.WriteLine($"[RollTheDice] Ignoring sound path '{path}': only sounds/ followed by letters, digits, _ - . / is allowed");
            }
            return false;
        }
    }
}
