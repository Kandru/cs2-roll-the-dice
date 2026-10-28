using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;

namespace RollTheDice.Hud
{
    public readonly record struct TextSlot(int Index);

    public readonly record struct ClassSlot(int Index);

    /// <summary>
    /// One layout file = one custom_hud_layout entity (reference implementation of the Panorama ruleset, §11).
    /// Bindings are registered once at load (the layout contract). Every write goes through a per-slot diff cache
    /// that starts at the XML defaults, so only real changes reach the network. The root gets is-off / is-fading.
    /// </summary>
    public sealed class HudLayout
    {
        public const int MaxSlots = 65;
        public const float FadeSeconds = 0.25f;

        private const string OffClass = "is-off";
        private const string FadingClass = "is-fading";
        private const short None = -1;

        private sealed record TextBinding(string PanelId, string Name, string Authored);

        /// <summary>One-of classes on one panel. Index -1 = none set. A flag is a group of one class.</summary>
        private sealed record ClassBinding(string PanelId, string[] Classes, short Authored);

        private readonly string _resource;
        private readonly List<TextBinding> _texts = [];
        private readonly List<ClassBinding> _classes = [];
        private readonly string?[]?[] _text = new string?[MaxSlots][];
        private readonly short[]?[] _class = new short[MaxSlots][];
        private readonly bool[] _visible = new bool[MaxSlots];
        private readonly long[] _closingUntil = new long[MaxSlots];
        private readonly ClassSlot _rootOff;
        private readonly ClassSlot _rootFading;
        private CCSCustomHudLayout? _entity;
        private int _entityIndex = -1;
        private int _slotCount;
        private long _nextExpiry = long.MaxValue;

        public HudLayout(string resource, string rootId)
        {
            _resource = resource;
            RootId = rootId;
            _rootOff = Flag(rootId, OffClass, authoredOn: true);
            _rootFading = Flag(rootId, FadingClass, authoredOn: false);
        }

        public string RootId { get; }

        /// <summary>Native writes so far (only real changes count): lets painters spread a large paint over ticks.</summary>
        public long Writes { get; private set; }

        // ---- Contract (load time only) ----------------------------------------------------------

        public TextSlot Text(string panelId, string name, string authored = "")
        {
            _texts.Add(new TextBinding(panelId, name, authored));
            return new TextSlot(_texts.Count - 1);
        }

        public ClassSlot Flag(string panelId, string className, bool authoredOn = false) =>
            AddClasses(panelId, [className], authoredOn ? (short)0 : None);

        public ClassSlot Group(string panelId, string[] classes, int authored = None) =>
            AddClasses(panelId, classes, (short)authored);

        private ClassSlot AddClasses(string panelId, string[] classes, short authored)
        {
            _classes.Add(new ClassBinding(panelId, classes, authored));
            return new ClassSlot(_classes.Count - 1);
        }

        // ---- Per-player writes ------------------------------------------------------------------

        public void SetText(CCSPlayerController player, TextSlot slot, string value)
        {
            if (!TryWrite(player, out CCSCustomHudLayout hud, out string?[] text, out _)
                || string.Equals(text[slot.Index], value, StringComparison.Ordinal))
            {
                return;
            }

            TextBinding b = _texts[slot.Index];
            hud.SetDialogVariableStringForPlayer(player, b.PanelId, b.Name, value);
            text[slot.Index] = value;
            Writes++;
        }

        public void SetFlag(CCSPlayerController player, ClassSlot slot, bool on) =>
            SetIndex(player, slot, on ? (short)0 : None);

        public void SetGroup(CCSPlayerController player, ClassSlot slot, int index) =>
            SetIndex(player, slot, (short)index);

        /// <summary>Exactly one of <paramref name="flags"/> on (tabs, filters). -1 = none.</summary>
        public void SetExclusive(CCSPlayerController player, ReadOnlySpan<ClassSlot> flags, int active)
        {
            for (int i = 0; i < flags.Length; i++)
            {
                SetFlag(player, flags[i], i == active);
            }
        }

        private void SetIndex(CCSPlayerController player, ClassSlot slot, short next)
        {
            if (!TryWrite(player, out CCSCustomHudLayout hud, out _, out short[] state))
            {
                return;
            }

            short prev = state[slot.Index];
            if (prev == next)
            {
                return;
            }

            ClassBinding b = _classes[slot.Index];
            if (prev >= 0)
            {
                hud.SetHasClassForPlayer(player, b.PanelId, b.Classes[prev], false);
                Writes++;
            }

            if (next >= 0)
            {
                hud.SetHasClassForPlayer(player, b.PanelId, b.Classes[next], true);
                Writes++;
            }

            state[slot.Index] = next;
        }

        // ---- Visibility -------------------------------------------------------------------------

        public bool IsVisible(int slot) => (uint)slot < MaxSlots && _visible[slot];

        public void Show(CCSPlayerController player)
        {
            int slot = player.Slot;
            if ((uint)slot >= MaxSlots || _visible[slot] || !EnsureEntity())
            {
                return;
            }

            _visible[slot] = true;
            _closingUntil[slot] = 0;
            SetFlag(player, _rootFading, true);
            SetFlag(player, _rootOff, false);
            // one frame in the faded state gives the opacity transition a start value
            Server.NextFrame(() =>
            {
                if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } live && _visible[slot])
                {
                    SetFlag(live, _rootFading, false);
                }
            });
        }

        public void Hide(CCSPlayerController player)
        {
            int slot = player.Slot;
            if ((uint)slot >= MaxSlots || !_visible[slot])
            {
                return;
            }

            _visible[slot] = false;
            SetFlag(player, _rootFading, true);
            long until = Environment.TickCount64 + (long)(FadeSeconds * 1000);
            _closingUntil[slot] = until;
            _nextExpiry = Math.Min(_nextExpiry, until);
        }

        /// <summary>Per tick: one comparison while no fade-out is due.</summary>
        public void Tick()
        {
            long now = Environment.TickCount64;
            if (now < _nextExpiry)
            {
                return;
            }

            _nextExpiry = long.MaxValue;
            for (int slot = 0; slot < MaxSlots; slot++)
            {
                long until = _closingUntil[slot];
                if (until == 0)
                {
                    continue;
                }

                if (now < until)
                {
                    _nextExpiry = Math.Min(_nextExpiry, until);
                    continue;
                }

                _closingUntil[slot] = 0;
                if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } player)
                {
                    SetFlag(player, _rootOff, true);
                    SetFlag(player, _rootFading, false);
                }
            }
        }

        // ---- Lifecycle --------------------------------------------------------------------------

        /// <summary>
        /// Round restart: the client rebuilt the layout from XML. Write the defaults back so server and client agree,
        /// at most <paramref name="budget"/> writes per call (a full list would burst). True once nothing differs.
        /// </summary>
        public bool RestoreDefaults(CCSPlayerController player, int budget = int.MaxValue)
        {
            if (!TryWrite(player, out CCSCustomHudLayout hud, out string?[] text, out _))
            {
                return true;
            }

            _visible[player.Slot] = false;
            _closingUntil[player.Slot] = 0;
            long limit = Writes + budget;
            for (int i = 0; i < _texts.Count; i++)
            {
                TextBinding b = _texts[i];
                if (!string.Equals(text[i], b.Authored, StringComparison.Ordinal))
                {
                    if (Writes >= limit)
                    {
                        return false;
                    }
                    hud.SetDialogVariableStringForPlayer(player, b.PanelId, b.Name, b.Authored);
                    text[i] = b.Authored;
                    Writes++;
                }
            }

            for (int i = 0; i < _classes.Count; i++)
            {
                if (Writes >= limit)
                {
                    return false;
                }
                SetIndex(player, new ClassSlot(i), _classes[i].Authored);
            }

            return true;
        }

        /// <summary>
        /// Disconnect: hide the slot. The diff caches stay - the entity keeps each slot's state across connections, so
        /// the next player in this slot inherits it (dropping the caches left stale rows and tones in the menu after a
        /// reconnect). <see cref="RestoreDefaults"/> on connect writes the difference back.
        /// </summary>
        public void Forget(int slot)
        {
            if ((uint)slot < MaxSlots)
            {
                _visible[slot] = false;
                _closingUntil[slot] = 0;
            }
        }

        /// <summary>Map start: the entity went with the old map.</summary>
        public void ResetForMap() => LoseEntity();

        public void Shutdown()
        {
            if (_entity is { IsValid: true })
            {
                _entity.AcceptInput("Kill");
            }

            LoseEntity();
        }

        // ---- Transmit ---------------------------------------------------------------------------

        /// <summary>CheckTransmit: send the entity only to human viewers that see it (or watch it fade out).</summary>
        public void FilterTransmit(CCheckTransmitInfoList infoList)
        {
            if (_entityIndex < 0 || _entity is not { IsValid: true })
            {
                return;
            }

            foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
            {
                if (viewer is null || (uint)viewer.Slot >= MaxSlots)
                {
                    continue;
                }

                int slot = viewer.Slot;
                if (_visible[slot] || _closingUntil[slot] != 0)
                {
                    info.TransmitEntities.Add(_entityIndex);
                }
                else
                {
                    info.TransmitEntities.Remove(_entityIndex);
                }
            }
        }

        public void SetInputCapture(CCSPlayerController player, bool on)
        {
            if (player is { IsValid: true, IsBot: false, IsHLTV: false } && EnsureEntity())
            {
                _entity!.SetInputCaptureEnabled(player, on);
            }
        }

        public bool Owns(CCSCustomHudLayout layout) => _entity is { IsValid: true } && layout.Handle == _entity.Handle;

        // ---- Internals --------------------------------------------------------------------------

        private bool TryWrite(CCSPlayerController player, out CCSCustomHudLayout hud, out string?[] text, out short[] state)
        {
            hud = null!;
            text = null!;
            state = null!;
            if (player is not { IsValid: true, IsBot: false, IsHLTV: false } || (uint)player.Slot >= MaxSlots || !EnsureEntity())
            {
                return false;
            }

            int slot = player.Slot;
            if (slot >= _slotCount)
            {
                _slotCount = _entity!.PlayerLayoutStates.Count;
                if (slot >= _slotCount)
                {
                    return false;
                }
            }

            hud = _entity!;
            text = _text[slot] ??= AuthoredTexts();
            state = _class[slot] ??= AuthoredClasses();
            return true;
        }

        private string?[] AuthoredTexts()
        {
            string?[] values = new string?[_texts.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = _texts[i].Authored;
            }

            return values;
        }

        private short[] AuthoredClasses()
        {
            short[] values = new short[_classes.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = _classes[i].Authored;
            }

            return values;
        }

        /// <summary>Cheap while the entity lives; spawns or adopts it otherwise. Never call first inside a client command.</summary>
        public bool EnsureEntity()
        {
            if (_entity is { IsValid: true })
            {
                return true;
            }

            if (_entity is not null)
            {
                LoseEntity();
            }

            foreach (CCSCustomHudLayout found in Utilities.FindAllEntitiesByDesignerName<CCSCustomHudLayout>("custom_hud_layout"))
            {
                if (found is { IsValid: true } && found.StrLayout == _resource)
                {
                    Adopt(found);
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

                using (CEntityKeyValues kv = new())
                {
                    kv.SetVector("origin", 0f, 0f, 0f);
                    kv.SetString("layout", _resource);
                    kv.SetBool("observable", false);
                    hud.DispatchSpawn(kv);
                }

                if (!hud.IsValid)
                {
                    return false;
                }

                Adopt(hud);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RollTheDice][HudLayout] Spawn failed: {ex.Message}");
                return false;
            }
        }

        private void Adopt(CCSCustomHudLayout hud)
        {
            _entity = hud;
            _entityIndex = (int)hud.Index;
            _slotCount = hud.PlayerLayoutStates.Count;
        }

        /// <summary>A new entity starts at the XML defaults for everyone: drop every cache.</summary>
        private void LoseEntity()
        {
            _entity = null;
            _entityIndex = -1;
            _slotCount = 0;
            Array.Clear(_text);
            Array.Clear(_class);
            Array.Clear(_visible);
            Array.Clear(_closingUntil);
            _nextExpiry = long.MaxValue;
        }
    }
}
