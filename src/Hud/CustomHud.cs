using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Hud
{
    /// <summary>Single-panel CCSCustomHudLayout driver for the Roll The Dice banner.</summary>
    public static class CustomHud
    {
        private const string LayoutResource =
            "panorama/layout/custom_game/rollthedice/banner.vxml_c";

        private static readonly TimeSpan CloseTransmitGrace = TimeSpan.FromSeconds(1.0);

        private static CCSCustomHudLayout? _layout;
        private static readonly HashSet<int> _visibleSlots = [];
        private static readonly Dictionary<int, DateTime> _closingUntil = [];
        private static readonly Dictionary<int, Dictionary<string, bool>> _classBySlot = [];
        private static readonly Dictionary<int, Dictionary<string, string>> _textBySlot = [];

        public static bool IsHumanViewer(CCSPlayerController? player) =>
            player is { IsValid: true, IsBot: false, IsHLTV: false };

        public static bool EnsureSpawned(CCSPlayerController player) =>
            IsHumanViewer(player) && EnsureLayout();

        public static bool IsVisible(CCSPlayerController player) =>
            player is { IsValid: true } && _visibleSlots.Contains(player.Slot);

        public static bool HasTransmitInterest() =>
            _visibleSlots.Count > 0 || _closingUntil.Count > 0;

        public static bool ShowPanel(CCSPlayerController player, string panelId)
        {
            if (player is not { IsValid: true })
            {
                return false;
            }

            int slot = player.Slot;
            bool wasVisible = _visibleSlots.Contains(slot);
            _ = _visibleSlots.Add(slot);
            _ = _closingUntil.Remove(slot);
            if (!SetHasClass(player, panelId, "ph-off", false))
            {
                _ = _visibleSlots.Remove(slot);
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
                    && _visibleSlots.Contains(slot))
                {
                    SetHasClass(live, panelId, "ph-fading", false);
                }
            });
            return true;
        }

        public static void HidePanel(CCSPlayerController player, string panelId)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            SetHasClass(player, panelId, "ph-fading", true);
            _closingUntil[player.Slot] = DateTime.UtcNow + CloseTransmitGrace;
            _ = _visibleSlots.Remove(player.Slot);
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

        public static void OnCheckTransmit(CCheckTransmitInfoList infoList)
        {
            ExpireClosing();
            if (_layout is not { IsValid: true } layout
                || (_visibleSlots.Count == 0 && _closingUntil.Count == 0))
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
                if (_visibleSlots.Contains(slot) || _closingUntil.ContainsKey(slot))
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

            PurgeOrphans(_visibleSlots, live);
            PurgeOrphans(_classBySlot, live);
            PurgeOrphans(_textBySlot, live);
            PurgeOrphans(_closingUntil, live);
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

        public static void Shutdown()
        {
            _visibleSlots.Clear();
            _closingUntil.Clear();
            _classBySlot.Clear();
            _textBySlot.Clear();
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
        }

        private static void ClearSlot(int slot)
        {
            _ = _visibleSlots.Remove(slot);
            _ = _closingUntil.Remove(slot);
            _ = _classBySlot.Remove(slot);
            _ = _textBySlot.Remove(slot);
        }

        private static void ExpireClosing()
        {
            if (_closingUntil.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            List<int>? expired = null;
            foreach ((int slot, DateTime until) in _closingUntil)
            {
                if (now < until)
                {
                    continue;
                }
                expired ??= [];
                expired.Add(slot);
            }

            if (expired == null)
            {
                return;
            }

            foreach (int slot in expired)
            {
                _ = _closingUntil.Remove(slot);
                if (Utilities.GetPlayerFromSlot(slot) is not { IsValid: true } player)
                {
                    continue;
                }
                SetHasClass(player, Banner.Panel, "ph-off", true);
                SetHasClass(player, Banner.Panel, "ph-fading", false);
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
                _layout = null;
            }

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

        private static void PurgeOrphans(HashSet<int> set, HashSet<int> live)
        {
            List<int>? dead = null;
            foreach (int slot in set)
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
                set.Remove(slot);
            }
        }

    }
}
