using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Localization;

namespace RollTheDice.Utils
{
    /// <summary>
    /// Per-player texts that also work for bots: CounterStrikeSharp's ForPlayer / GetLanguage throw for SteamID 0
    /// (bots, players not yet authorized). Those get the server language.
    /// </summary>
    public static class Localization
    {
        private static bool HasLanguage(CCSPlayerController? player) =>
            player is { IsValid: true, IsBot: false, IsHLTV: false } && player.SteamID != 0;

        public static string ForPlayerSafe(this IStringLocalizer localizer, CCSPlayerController? player, string key) =>
            HasLanguage(player) ? localizer.ForPlayer(player, key) : localizer[key].Value;

        public static CultureInfo LanguageOf(CCSPlayerController? player) =>
            HasLanguage(player) ? player.GetLanguage() : PlayerLanguageManager.Instance.GetDefaultLanguage();
    }
}
