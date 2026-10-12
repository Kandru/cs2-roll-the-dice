using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RollTheDice.Hud
{
    public static class Banner
    {
        public const string Panel = "Banner";
        public const string TextVar = "dice";

        private static readonly Dictionary<int, string> _ownerText = [];
        private static readonly Dictionary<int, string> _shownText = [];
        private static bool _suspended;

        public static void Show(CCSPlayerController player, string text)
        {
            if (_suspended || player is not { IsValid: true } || !player.PawnIsAlive)
            {
                return;
            }

            string next = text ?? string.Empty;
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

            int slot = player.Slot;
            if (_ownerText.Remove(slot))
            {
                SyncViewers();
            }
            _ = _shownText.Remove(slot);
        }

        public static void Suspend()
        {
            _suspended = true;
            _ownerText.Clear();
            _shownText.Clear();
            CustomHud.ResetRound();
        }

        public static void Shutdown()
        {
            _suspended = true;
            _ownerText.Clear();
            _shownText.Clear();
        }

        public static void Resume() => _suspended = false;

        public static void SyncViewers()
        {
            if (_suspended || (_ownerText.Count == 0 && !CustomHud.HasTransmitInterest()))
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
            if (viewer.PawnIsAlive)
            {
                return _ownerText.GetValueOrDefault(viewer.Slot);
            }

            CCSPlayerController? target = GetSpectatedOwner(viewer);
            return target is null ? null : _ownerText.GetValueOrDefault(target.Slot);
        }

        private static void Present(CCSPlayerController viewer, string text)
        {
            int slot = viewer.Slot;
            if (_shownText.TryGetValue(slot, out string? shown)
                && shown == text
                && CustomHud.IsVisible(viewer))
            {
                return;
            }

            if (!CustomHud.EnsureSpawned(viewer))
            {
                return;
            }

            HudTheme.Apply(viewer);
            CustomHud.SetText(viewer, Panel, TextVar, text);
            CustomHud.ShowPanel(viewer, Panel);
            _shownText[slot] = text;
        }

        private static void ClearPresentation(CCSPlayerController viewer)
        {
            int slot = viewer.Slot;
            bool hadShown = _shownText.Remove(slot);
            if (!hadShown && !CustomHud.IsVisible(viewer))
            {
                return;
            }

            CustomHud.HidePanel(viewer, Panel);
            CustomHud.SetText(viewer, Panel, TextVar, string.Empty);
        }

        private static CCSPlayerController? GetSpectatedOwner(CCSPlayerController viewer)
        {
            if (viewer.ObserverPawn?.Value is not { IsValid: true, ObserverServices: not null } obsPawn)
            {
                return null;
            }

            CPlayer_ObserverServices services = obsPawn.ObserverServices;
            ObserverMode_t mode = (ObserverMode_t)services.ObserverMode;
            if (mode is not (ObserverMode_t.OBS_MODE_IN_EYE
                or ObserverMode_t.OBS_MODE_CHASE
                or ObserverMode_t.OBS_MODE_FIXED))
            {
                return null;
            }

            if (services.ObserverTarget.Value is not CCSPlayerPawnBase { IsValid: true } targetPawn)
            {
                return null;
            }

            CCSPlayerController? controller = targetPawn.OriginalController.Value;
            return controller is { IsValid: true } && controller != viewer
                ? controller
                : null;
        }
    }
}
