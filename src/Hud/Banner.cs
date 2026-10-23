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
        // pawn handle -> player, rebuilt per sync: spectator lookups need it for every dead viewer
        private static readonly Dictionary<nint, CCSPlayerController> _pawnOwners = [];
        // the tick only follows spectator switches; Show / Hide sync at once
        private const int TickSyncInterval = 4;
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

        /// <summary>Listener OnTick: follows spectator target changes every few ticks.</summary>
        public static void OnTick()
        {
            if (Server.TickCount % TickSyncInterval == 0)
            {
                SyncViewers();
            }
        }

        public static void SyncViewers()
        {
            if (_ownerText.Count == 0 && !CustomHud.HasTransmitInterest())
            {
                return;
            }

            List<CCSPlayerController> players = Utilities.GetPlayers();
            Dictionary<nint, CCSPlayerController> pawnOwners = IndexPawns(players, _pawnOwners);
            foreach (CCSPlayerController viewer in players)
            {
                if (!CustomHud.IsHumanViewer(viewer))
                {
                    continue;
                }

                string? text = ResolveText(viewer, pawnOwners);
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

        private static string? ResolveText(CCSPlayerController viewer, Dictionary<nint, CCSPlayerController> pawnOwners)
        {
            CCSPlayerController? owner = GetSpectatedOwner(viewer, pawnOwners);
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

        /// <summary>Pawn handle -> player of every live pawn, filled into <paramref name="index"/> once per pass.</summary>
        internal static Dictionary<nint, CCSPlayerController> IndexPawns(List<CCSPlayerController> players, Dictionary<nint, CCSPlayerController> index)
        {
            index.Clear();
            foreach (CCSPlayerController player in players)
            {
                if (player.IsValid && player.PlayerPawn?.Value is { IsValid: true } pawn)
                {
                    index[pawn.Handle] = player;
                }
            }
            return index;
        }

        /// <summary>
        /// Player the dead <paramref name="viewer"/> spectates. Always null while the viewer is alive: the observer
        /// pawn can keep its last target after a respawn, so a living player would otherwise follow someone else's HUD.
        /// </summary>
        internal static CCSPlayerController? GetSpectatedOwner(CCSPlayerController viewer, Dictionary<nint, CCSPlayerController> pawnOwners)
        {
            if (viewer.PawnIsAlive)
            {
                return null;
            }

            CCSPlayerController? From(CBasePlayerPawn? pawn)
            {
                CBaseEntity? target = pawn?.ObserverServices?.ObserverTarget.Value;
                return target is { IsValid: true }
                    && pawnOwners.TryGetValue(target.Handle, out CCSPlayerController? owner)
                    && owner != viewer
                    ? owner
                    : null;
            }

            return From(viewer.ObserverPawn?.Value) ?? From(viewer.PlayerPawn?.Value);
        }
    }
}
