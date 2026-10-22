using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Hud
{
    public static partial class Banner
    {
        public const string Panel = "Banner";
        public const string TextVar = "dice";

        [GeneratedRegex(@"\{(?:green|purple|default|red|blue|yellow|orange|olive|lightpurple|lightblue|lightgreen|lightred|grey|gray|white|darkblue|darkred)\}", RegexOptions.IgnoreCase)]
        private static partial Regex ChatColorTokens();

        private static readonly Dictionary<int, string> _ownerText = [];
        private static readonly Dictionary<int, string> _shownText = [];
        private static bool _centerEnabled = true;
        private static bool _panoramaEnabled = true;

        /// <summary>Chat colour tokens ({green} …) removed, for text shown outside the chat.</summary>
        public static string StripColors(string? text) =>
            ChatColorTokens().Replace(text ?? string.Empty, string.Empty).Trim();

        public static void BindConfig(PluginConfig config)
        {
            _centerEnabled = config.NotifyPlayerViaCenterMsg;
            _panoramaEnabled = config.NotifyPlayerViaPanorama;
        }

        /// <summary>Center and/or panorama dice info (config-gated). Bots skip center; text is kept for spectators.</summary>
        public static void Notify(CCSPlayerController player, string message)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            // a spinning reel replaces the center message; the banner follows when it stops
            if (Reel.TryDeferBanner(player, message))
            {
                return;
            }

            if (_centerEnabled && !player.IsBot)
            {
                player.PrintToCenter(message);
            }

            Show(player, message);
        }

        /// <summary>Panorama only. Hides when panorama is disabled.</summary>
        public static void Show(CCSPlayerController player, string text)
        {
            if (player is not { IsValid: true } || !player.PawnIsAlive)
            {
                return;
            }

            if (Reel.TryDeferBanner(player, text))
            {
                return;
            }

            if (!_panoramaEnabled)
            {
                Hide(player);
                return;
            }

            string next = StripColors(text);
            int slot = player.Slot;
            if (_ownerText.TryGetValue(slot, out string? prev) && prev == next)
            {
                return;
            }

            _ownerText[slot] = next;
            SyncViewers();
        }

        public static void Hide(CCSPlayerController? player)
        {
            if (player is not { IsValid: true } || !_ownerText.Remove(player.Slot))
            {
                return;
            }

            SyncViewers();
        }

        public static void Release(CCSPlayerController? player)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            _ = _shownText.Remove(player.Slot);
            Hide(player);
        }

        public static void Suspend()
        {
            _ownerText.Clear();
            _shownText.Clear();
            CustomHud.ResetRound();
        }

        public static void Shutdown()
        {
            _ownerText.Clear();
            _shownText.Clear();
        }

        public static void SyncViewers()
        {
            if (_ownerText.Count == 0 && !CustomHud.HasTransmitInterest())
            {
                return;
            }

            foreach (CCSPlayerController viewer in Utilities.GetPlayers())
            {
                if (!CustomHud.IsHumanViewer(viewer))
                {
                    continue;
                }

                string? text = ResolveText(viewer);
                if (text is { Length: > 0 })
                {
                    Present(viewer, text);
                }
                else
                {
                    ClearPresentation(viewer);
                }
            }
        }

        public static void WriteDefaults(CCSPlayerController player)
        {
            CustomHud.SetText(player, Panel, TextVar, string.Empty);
            CustomHud.SetHasClass(player, Panel, "ph-fading", false);
            CustomHud.SetHasClass(player, Panel, "ph-off", true);
            HudTheme.Apply(player);
        }

        private static string? ResolveText(CCSPlayerController viewer)
        {
            CCSPlayerController? owner = GetSpectatedOwner(viewer);
            if (owner is null && viewer.PawnIsAlive)
            {
                owner = viewer;
            }

            return owner is null ? null : _ownerText.GetValueOrDefault(owner.Slot);
        }

        private static void Present(CCSPlayerController viewer, string text)
        {
            int slot = viewer.Slot;
            if (_shownText.TryGetValue(slot, out string? shown)
                && shown == text
                && CustomHud.IsVisible(viewer, Panel))
            {
                return;
            }

            if (!CustomHud.EnsureSpawned(viewer))
            {
                return;
            }

            HudTheme.Apply(viewer);
            if (!CustomHud.SetText(viewer, Panel, TextVar, text)
                || !CustomHud.ShowPanel(viewer, Panel))
            {
                return;
            }

            _shownText[slot] = text;
        }

        private static void ClearPresentation(CCSPlayerController viewer)
        {
            if (!_shownText.Remove(viewer.Slot) && !CustomHud.IsVisible(viewer, Panel))
            {
                return;
            }

            CustomHud.HidePanel(viewer, Panel);
            CustomHud.SetText(viewer, Panel, TextVar, string.Empty);
        }

        internal static CCSPlayerController? GetSpectatedOwner(CCSPlayerController viewer)
        {
            CCSPlayerController? From(CBasePlayerPawn? pawn)
            {
                CBaseEntity? target = pawn?.ObserverServices?.ObserverTarget.Value;
                if (target is not { IsValid: true })
                {
                    return null;
                }

                nint handle = target.Handle;
                foreach (CCSPlayerController candidate in Utilities.GetPlayers())
                {
                    if (candidate.IsValid
                        && candidate != viewer
                        && candidate.PlayerPawn?.Value is { IsValid: true } candidatePawn
                        && candidatePawn.Handle == handle)
                    {
                        return candidate;
                    }
                }

                return null;
            }

            return From(viewer.ObserverPawn?.Value) ?? From(viewer.PlayerPawn?.Value);
        }
    }
}
