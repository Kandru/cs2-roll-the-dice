using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Hud
{
    /// <summary>CCSCustomHudLayout driver for the Roll The Dice banner and reel (one layout, per-panel visibility).</summary>
    public static class CustomHud
    {
        private const string LayoutResource =
            "panorama/layout/custom_game/rollthedice/banner.vxml_c";

        public static readonly TimeSpan DefaultCloseGrace = TimeSpan.FromSeconds(1.0);

        private static CCSCustomHudLayout? _layout;
        private static readonly Dictionary<int, HashSet<string>> _visibleBySlot = [];
        private static readonly Dictionary<(int Slot, string Panel), DateTime> _closingUntil = [];
        private static readonly Dictionary<int, Dictionary<string, bool>> _classBySlot = [];
        private static readonly Dictionary<int, Dictionary<string, string>> _textBySlot = [];
        private static readonly Dictionary<string, bool> _globalClasses = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> _globalTexts = new(StringComparer.Ordinal);

        public static bool IsHumanViewer(CCSPlayerController? player) =>
            player is { IsValid: true, IsBot: false, IsHLTV: false };

        public static bool EnsureSpawned(CCSPlayerController player) =>
            IsHumanViewer(player) && EnsureLayout();

        public static bool IsVisible(CCSPlayerController player, string panelId) =>
            player is { IsValid: true }
            && _visibleBySlot.TryGetValue(player.Slot, out HashSet<string>? panels)
            && panels.Contains(panelId);

        public static bool HasTransmitInterest() =>
            _visibleBySlot.Count > 0 || _closingUntil.Count > 0;

        public static bool ShowPanel(CCSPlayerController player, string panelId)
        {
            if (player is not { IsValid: true })
            {
                return false;
            }

            int slot = player.Slot;
            bool wasVisible = IsVisible(player, panelId);
            MarkVisible(slot, panelId, true);
            _ = _closingUntil.Remove((slot, panelId));
            if (!SetHasClass(player, panelId, "ph-off", false))
            {
                MarkVisible(slot, panelId, false);
                return false;
            }

            if (wasVisible)
            {
                _ = SetHasClass(player, panelId, "ph-fading", false);
                return true;
            }

            _ = SetHasClass(player, panelId, "ph-fading", true);
            Server.NextFrame(() =>
            {
                if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } live
                    && IsVisible(live, panelId))
                {
                    SetHasClass(live, panelId, "ph-fading", false);
                }
            });
            return true;
        }

        /// <summary>Fade now (<c>ph-fading</c>), collapse (<c>ph-off</c>) once <paramref name="grace"/> has passed.</summary>
        public static void HidePanel(CCSPlayerController player, string panelId, TimeSpan? grace = null)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            SetHasClass(player, panelId, "ph-fading", true);
            _closingUntil[(player.Slot, panelId)] = DateTime.UtcNow + (grace ?? DefaultCloseGrace);
            MarkVisible(player.Slot, panelId, false);
        }

        public static bool SetText(CCSPlayerController player, string panelId, string name, string value)
        {
            if (player is not { IsValid: true })
            {
                return false;
            }

            string text = value ?? string.Empty;
            int slot = player.Slot;
            if (_textBySlot.TryGetValue(slot, out Dictionary<string, string>? vars)
                && vars.TryGetValue(name, out string? prev)
                && prev == text)
            {
                return true;
            }

            if (!TryWrite(player, out CCSCustomHudLayout hud))
            {
                return false;
            }

            hud.SetDialogVariableStringForPlayer(player, panelId, name, text);
            if (vars == null)
            {
                _textBySlot[slot] = vars = new Dictionary<string, string>(StringComparer.Ordinal);
            }
            vars[name] = text;
            return true;
        }

        public static bool SetHasClass(CCSPlayerController player, string panelId, string className, bool enabled)
        {
            if (player is not { IsValid: true } || string.IsNullOrEmpty(className))
            {
                return false;
            }

            int slot = player.Slot;
            string key = panelId + '\0' + className;
            if (_classBySlot.TryGetValue(slot, out Dictionary<string, bool>? classes)
                && classes.TryGetValue(key, out bool prev)
                && prev == enabled)
            {
                return true;
            }

            if (!TryWrite(player, out CCSCustomHudLayout hud))
            {
                return false;
            }

            hud.SetHasClassForPlayer(player, panelId, className, enabled);
            if (classes == null)
            {
                _classBySlot[slot] = classes = new Dictionary<string, bool>(StringComparer.Ordinal);
            }
            classes[key] = enabled;
            return true;
        }

        /// <summary>Same text for every viewer (GlobalLayoutState). A variable is either global or per player, never both.</summary>
        public static bool SetGlobalText(string panelId, string name, string value)
        {
            string text = value ?? string.Empty;
            string key = panelId + '\0' + name;
            if (_globalTexts.TryGetValue(key, out string? prev) && prev == text)
            {
                return true;
            }

            if (_layout is not { IsValid: true } hud)
            {
                return false;
            }

            hud.SetDialogVariableString(panelId, name, text);
            _globalTexts[key] = text;
            return true;
        }

        /// <summary>Same class for every viewer (GlobalLayoutState).</summary>
        public static bool SetGlobalClass(string panelId, string className, bool enabled)
        {
            string key = panelId + '\0' + className;
            if (_globalClasses.TryGetValue(key, out bool prev) && prev == enabled)
            {
                return true;
            }

            if (_layout is not { IsValid: true } hud)
            {
                return false;
            }

            hud.SetHasClass(panelId, className, enabled);
            _globalClasses[key] = enabled;
            return true;
        }

        public static bool HasLayout() => _layout is { IsValid: true };

        public static void OnCheckTransmit(CCheckTransmitInfoList infoList)
        {
            ExpireClosing();
            if (_layout is not { IsValid: true } layout
                || (_visibleBySlot.Count == 0 && _closingUntil.Count == 0))
            {
                return;
            }

            int index = (int)layout.Index;
            foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
            {
                if (!IsHumanViewer(viewer))
                {
                    continue;
                }

                int slot = viewer!.Slot;
                if (_visibleBySlot.ContainsKey(slot) || IsClosing(slot))
                {
                    info.TransmitEntities.Add(index);
                }
                else
                {
                    info.TransmitEntities.Remove(index);
                }
            }
        }

        public static void ResetRound()
        {
            Reel.CancelAll();
            HashSet<int> live = [];
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (!IsHumanViewer(player))
                {
                    continue;
                }
                live.Add(player.Slot);
                RestoreDefaults(player);
            }

            PurgeOrphans(_visibleBySlot, live);
            PurgeOrphans(_classBySlot, live);
            PurgeOrphans(_textBySlot, live);
            PurgeClosing(slot => !live.Contains(slot));
            Reel.WriteGlobalDefaults();
        }

        public static void ReleasePlayer(CCSPlayerController? player)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            RestoreDefaults(player);
            ClearSlot(player.Slot);
        }

        /// <summary>
        /// A player joined: the layout keeps per-slot state across connections, so hide every panel whatever the
        /// slot's previous connection left there (a disconnect mid-roll kept the reel on screen for good).
        /// </summary>
        public static void ClaimSlot(CCSPlayerController? player)
        {
            if (!IsHumanViewer(player) || !HasLayout())
            {
                return;
            }

            // cached writes belong to the previous connection: without them every default below is written
            ClearSlot(player!.Slot);
            Banner.WriteDefaults(player);
            Reel.WriteDefaults(player, force: true);
            Reel.Forget(player);
        }

        public static void Shutdown()
        {
            _visibleBySlot.Clear();
            _closingUntil.Clear();
            ClearCaches();
            if (_layout is { IsValid: true } layout)
            {
                layout.AcceptInput("Kill");
            }
            _layout = null;
        }

        private static void RestoreDefaults(CCSPlayerController player)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            ClearSlot(player.Slot);
            Banner.WriteDefaults(player);
            Reel.WriteDefaults(player);
        }

        private static void ClearSlot(int slot)
        {
            _ = _visibleBySlot.Remove(slot);
            _ = _classBySlot.Remove(slot);
            _ = _textBySlot.Remove(slot);
            PurgeClosing(s => s == slot);
        }

        /// <summary>A new layout entity starts at the XML defaults for everyone: drop every cache.</summary>
        private static void ClearCaches()
        {
            _classBySlot.Clear();
            _textBySlot.Clear();
            _globalClasses.Clear();
            _globalTexts.Clear();
            Reel.OnLayoutReset();
        }

        private static void MarkVisible(int slot, string panelId, bool visible)
        {
            if (visible)
            {
                if (!_visibleBySlot.TryGetValue(slot, out HashSet<string>? panels))
                {
                    _visibleBySlot[slot] = panels = new HashSet<string>(StringComparer.Ordinal);
                }
                _ = panels.Add(panelId);
                return;
            }

            if (_visibleBySlot.TryGetValue(slot, out HashSet<string>? current)
                && current.Remove(panelId)
                && current.Count == 0)
            {
                _ = _visibleBySlot.Remove(slot);
            }
        }

        private static bool IsClosing(int slot)
        {
            foreach ((int closingSlot, _) in _closingUntil.Keys)
            {
                if (closingSlot == slot)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ExpireClosing()
        {
            if (_closingUntil.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            List<(int Slot, string Panel)>? expired = null;
            foreach (((int Slot, string Panel) key, DateTime until) in _closingUntil)
            {
                if (now < until)
                {
                    continue;
                }
                expired ??= [];
                expired.Add(key);
            }

            if (expired == null)
            {
                return;
            }

            foreach ((int slot, string panel) in expired)
            {
                _ = _closingUntil.Remove((slot, panel));
                if (Utilities.GetPlayerFromSlot(slot) is not { IsValid: true } player)
                {
                    continue;
                }
                SetHasClass(player, panel, "ph-off", true);
                SetHasClass(player, panel, "ph-fading", false);
            }
        }

        private static bool EnsureLayout()
        {
            if (_layout is { IsValid: true } existing && LayoutPath(existing) == LayoutResource)
            {
                return true;
            }

            if (_layout is { IsValid: true } stale)
            {
                stale.AcceptInput("Kill");
            }
            _layout = null;
            ClearCaches();

            foreach (CCSCustomHudLayout found in
                     Utilities.FindAllEntitiesByDesignerName<CCSCustomHudLayout>("custom_hud_layout"))
            {
                if (found is { IsValid: true } && LayoutPath(found) == LayoutResource)
                {
                    _layout = found;
                    return true;
                }
            }

            try
            {
                CCSCustomHudLayout? hud = Utilities.CreateEntityByName<CCSCustomHudLayout>("custom_hud_layout");
                if (hud is null || hud.Handle == IntPtr.Zero)
                {
                    return false;
                }

                using CEntityKeyValues kv = new();
                kv.SetVector("origin", 0f, 0f, 0f);
                kv.SetString("layout", LayoutResource);
                kv.SetBool("observable", false);
                hud.DispatchSpawn(kv);

                if (hud is not { IsValid: true })
                {
                    return false;
                }

                _layout = hud;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RollTheDice][CustomHud] Spawn failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryWrite(CCSPlayerController player, out CCSCustomHudLayout hud)
        {
            hud = null!;
            if (!EnsureSpawned(player) || _layout is not { IsValid: true } layout)
            {
                return false;
            }

            hud = layout;
            try
            {
                return player.Slot >= 0 && player.Slot < hud.PlayerLayoutStates.Count;
            }
            catch
            {
                return false;
            }
        }

        private static string? LayoutPath(CCSCustomHudLayout layout)
        {
            try
            {
                return layout.StrLayout;
            }
            catch
            {
                return null;
            }
        }

        private static void PurgeClosing(Func<int, bool> drop)
        {
            List<(int Slot, string Panel)>? dead = null;
            foreach ((int Slot, string Panel) key in _closingUntil.Keys)
            {
                if (!drop(key.Slot))
                {
                    continue;
                }
                dead ??= [];
                dead.Add(key);
            }
            if (dead == null)
            {
                return;
            }
            foreach ((int Slot, string Panel) key in dead)
            {
                _closingUntil.Remove(key);
            }
        }

        private static void PurgeOrphans<T>(IDictionary<int, T> map, HashSet<int> live)
        {
            List<int>? dead = null;
            foreach (int slot in map.Keys)
            {
                if (live.Contains(slot))
                {
                    continue;
                }
                dead ??= [];
                dead.Add(slot);
            }
            if (dead == null)
            {
                return;
            }
            foreach (int slot in dead)
            {
                map.Remove(slot);
            }
        }

    }
}
