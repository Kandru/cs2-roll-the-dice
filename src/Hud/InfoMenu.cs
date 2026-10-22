using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;

namespace RollTheDice.Hud
{
    /// <summary>Rail order of the menu; the first tab is the start page.</summary>
    public enum InfoTab
    {
        Round,
        History,
        Perks,
        Commands,
        Settings,
    }

    /// <summary>Index into <see cref="InfoMenuIds.Tones"/>.</summary>
    public enum InfoTone
    {
        None,
        Good,
        Bad,
        Neutral,
    }

    /// <summary>Index into <see cref="InfoMenuIds.Teams"/>.</summary>
    public enum InfoTeam
    {
        None,
        Ct,
        T,
    }

    /// <summary>Index into <see cref="InfoMenuIds.Lives"/>; None = no class.</summary>
    public enum InfoLife
    {
        None = -1,
        Alive,
        Dead,
    }

    /// <summary>
    /// One list entry, already localized for the viewer. <see cref="Badge"/> replaces the running number in the
    /// index box (team in the round list).
    /// </summary>
    public readonly record struct InfoRow(
        string Title,
        string Meta,
        string Desc,
        InfoTone Tone = InfoTone.None,
        InfoLife Life = InfoLife.None,
        bool Self = false,
        InfoTeam Team = InfoTeam.None,
        string? Badge = null);

    /// <summary>Settings view state. <see cref="Language"/> indexes the language buttons (-1 = none matches).</summary>
    public readonly record struct InfoSettings(bool AutoOn, bool AutoLocked, string AutoHint, int Language);

    /// <summary>Data and actions behind the menu; implemented by the plugin.</summary>
    public interface IInfoMenuSource
    {
        string Text(CCSPlayerController? player, string key);
        InfoSettings Settings(CCSPlayerController player);
        void FillRows(CCSPlayerController player, InfoTab tab, List<InfoRow> rows);
        void ToggleAutoRoll(CCSPlayerController player);
        void SetLanguage(CCSPlayerController player, int language);
    }

    /// <summary>
    /// Info menu opened with "!dice info" (info_v3.xml, own layout entity): this round's dice (start page), own
    /// history, perks (tone filter, sort, search via "!dice info &lt;text&gt;"), chat commands and settings (auto roll,
    /// language). Mouse driven: input capture while open, clicks arrive through <see cref="OnClicked"/>. Every list is
    /// one client-scrolled page over a fixed row pool; paints run only on change and are spread over ticks by a write
    /// budget, so a full perk list never reaches the client as one burst.
    /// </summary>
    public static class InfoMenu
    {
        private const float OpenDelaySeconds = 0.2f;
        private const float CaptureDelaySeconds = 0.2f;
        private const float WarmUpDelaySeconds = 1.0f;
        private const int PaintsPerTick = 2;
        // writes per paint for the row pool / per tick for a round-start restore (one burst stays small)
        private const int RowWriteBudget = 72;
        private const int RestoreWriteBudget = 64;
        private const int MaxSearchLength = 32;
        private const string CreditText = "made by: kandru.net";
        private const string HintText = "!dice info";
        private const int MaxSlots = HudLayout.MaxSlots;

        private static readonly InfoTab[] ListTabs = [InfoTab.Round, InfoTab.History, InfoTab.Perks, InfoTab.Commands];
        private static readonly string[] FilterKeys = ["info.filter.all", "info.filter.good", "info.filter.bad", "info.filter.neutral"];
        // filter chip -> tone it keeps (None = all)
        private static readonly InfoTone[] FilterTones = [InfoTone.None, InfoTone.Good, InfoTone.Bad, InfoTone.Neutral];

        private static readonly HudLayout _hud = new(InfoMenuIds.Resource, InfoMenuIds.RootId);
        private static readonly TextSlot _title = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.TitleVar);
        private static readonly TextSlot _sub = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.SubVar);
        private static readonly TextSlot _navHead = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.NavHeadVar);
        private static readonly TextSlot _section = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.SectionVar);
        private static readonly TextSlot _count = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.CountVar);
        private static readonly TextSlot _empty = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.EmptyVar);
        private static readonly TextSlot _autoHead = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AutoHeadVar);
        private static readonly TextSlot _auto = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AutoVar);
        private static readonly TextSlot _autoState = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AutoStateVar);
        private static readonly TextSlot _autoHint = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AutoHintVar);
        private static readonly TextSlot _langHead = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.LangHeadVar);
        private static readonly TextSlot _langHint = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.LangHintVar);
        private static readonly TextSlot _sort = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.SortVar);
        private static readonly TextSlot _search = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.SearchVar);
        private static readonly TextSlot _searchHint = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.SearchHintVar);
        private static readonly TextSlot _hint = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.HintVar);
        private static readonly TextSlot _credit = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.CreditVar);
        private static readonly TextSlot[] _tabLabel = Texts(InfoMenuIds.TabVars);
        private static readonly TextSlot[] _tabCount = Texts(InfoMenuIds.TabCountVars);
        private static readonly TextSlot[] _langLabel = Texts(InfoMenuIds.LangVars);
        private static readonly TextSlot[] _filterLabel = Texts(InfoMenuIds.FilterVars);
        private static readonly TextSlot[] _rowIndex = Texts(InfoMenuIds.RowIndexVars);
        private static readonly TextSlot[] _rowTitle = Texts(InfoMenuIds.RowTitleVars);
        private static readonly TextSlot[] _rowMeta = Texts(InfoMenuIds.RowMetaVars);
        private static readonly TextSlot[] _rowDesc = Texts(InfoMenuIds.RowDescVars);

        // authored state of info_v3.xml: view-list, theme-gold, tab 0 and filter 0 active, search box/rows/empty off
        private static readonly ClassSlot _view = _hud.Group(InfoMenuIds.RootId, InfoMenuIds.Views, authored: 0);
        private static readonly ClassSlot _theme = _hud.Group(InfoMenuIds.RootId, HudTheme.ClassNames, authored: HudTheme.DefaultIndex);
        private static readonly ClassSlot _autoOn = _hud.Flag(InfoMenuIds.AutoId, InfoMenuIds.OnClass);
        private static readonly ClassSlot _autoDisabled = _hud.Flag(InfoMenuIds.AutoId, InfoMenuIds.DisabledClass);
        private static readonly ClassSlot _emptyOff = _hud.Flag(InfoMenuIds.EmptyId, InfoMenuIds.OffClass, authoredOn: true);
        private static readonly ClassSlot _searchOff = _hud.Flag(InfoMenuIds.SearchId, InfoMenuIds.OffClass, authoredOn: true);
        private static readonly ClassSlot[] _tabActive = Flags(InfoMenuIds.TabIds, InfoMenuIds.ActiveClass, authoredOn: 0);
        private static readonly ClassSlot[] _langActive = Flags(InfoMenuIds.LangIds, InfoMenuIds.ActiveClass, authoredOn: -1);
        private static readonly ClassSlot[] _filterActive = Flags(InfoMenuIds.FilterIds, InfoMenuIds.ActiveClass, authoredOn: 0);
        private static readonly ClassSlot[] _rowOff = Flags(InfoMenuIds.RowIds, InfoMenuIds.OffClass, authoredOn: -2);
        private static readonly ClassSlot[] _rowSelf = Flags(InfoMenuIds.RowIds, InfoMenuIds.SelfClass, authoredOn: -1);
        private static readonly ClassSlot[] _rowTone = Groups(InfoMenuIds.RowIds, InfoMenuIds.Tones, authored: (int)InfoTone.None);
        private static readonly ClassSlot[] _rowTeam = Groups(InfoMenuIds.RowIds, InfoMenuIds.Teams, authored: (int)InfoTeam.None);
        private static readonly ClassSlot[] _rowLife = Groups(InfoMenuIds.RowIds, InfoMenuIds.Lives, authored: (int)InfoLife.None);

        /// <summary>Row pool of one team column in the round view (name, dice, tone, alive/dead, own row).</summary>
        private sealed class TeamPool(string[] rowIds, string[] nameVars, string[] diceVars, string emptyId, string headVar, string countVar, string emptyVar)
        {
            public readonly TextSlot[] Name = Texts(nameVars);
            public readonly TextSlot[] Dice = Texts(diceVars);
            public readonly ClassSlot[] Off = Flags(rowIds, InfoMenuIds.OffClass, authoredOn: -2);
            public readonly ClassSlot[] Self = Flags(rowIds, InfoMenuIds.SelfClass, authoredOn: -1);
            public readonly ClassSlot[] Tone = Groups(rowIds, InfoMenuIds.Tones, authored: (int)InfoTone.None);
            public readonly ClassSlot[] Life = Groups(rowIds, InfoMenuIds.Lives, authored: (int)InfoLife.None);
            public readonly ClassSlot EmptyOff = _hud.Flag(emptyId, InfoMenuIds.OffClass, authoredOn: true);
            public readonly TextSlot Head = _hud.Text(InfoMenuIds.RootId, headVar);
            public readonly TextSlot Count = _hud.Text(InfoMenuIds.RootId, countVar);
            public readonly TextSlot Empty = _hud.Text(InfoMenuIds.RootId, emptyVar);
            public readonly List<InfoRow> Rows = [];
        }

        private static readonly TeamPool _ct = new(
            InfoMenuIds.CtRowIds, InfoMenuIds.CtRowNameVars, InfoMenuIds.CtRowDiceVars, InfoMenuIds.CtEmptyId,
            InfoMenuIds.CtHeadVar, InfoMenuIds.CtCountVar, InfoMenuIds.CtEmptyVar);
        private static readonly TeamPool _t = new(
            InfoMenuIds.TRowIds, InfoMenuIds.TRowNameVars, InfoMenuIds.TRowDiceVars, InfoMenuIds.TEmptyId,
            InfoMenuIds.THeadVar, InfoMenuIds.TCountVar, InfoMenuIds.TEmptyVar);

        private static readonly bool[] _open = new bool[MaxSlots];
        private static readonly bool[] _dirty = new bool[MaxSlots];
        private static readonly bool[] _restoring = new bool[MaxSlots];
        private static readonly int[] _token = new int[MaxSlots];
        private static readonly InfoTab[] _tab = new InfoTab[MaxSlots];
        private static readonly int[] _filter = new int[MaxSlots];
        private static readonly bool[] _sortByTone = new bool[MaxSlots];
        private static readonly string[] _searchText = Filled(string.Empty);
        private static readonly List<InfoRow>[] _rows = BuildLists();
        private static readonly List<InfoRow> _shown = [];
        private static readonly int[] _toneCount = new int[4];
        private static readonly string[] _indexText = BuildNumberText(99, "00");
        private static readonly string[] _numberText = BuildNumberText(999, "0");

        private static BasePlugin? _plugin;
        private static IInfoMenuSource? _source;
        private static int _openCount;
        private static int _restoreCount;
        private static float _nextRefresh;

        public static void Bind(BasePlugin plugin, IInfoMenuSource source)
        {
            _plugin = plugin;
            _source = source;
        }

        public static bool IsOpen(CCSPlayerController? player) =>
            player is { IsValid: true } && (uint)player.Slot < MaxSlots && _open[player.Slot];

        /// <summary>
        /// Chat command. Without <paramref name="search"/>: open on the start page (one timer later, never inside the
        /// command's tick) or close. With a search text: show the perks matching it, opening the menu if needed.
        /// </summary>
        public static void Toggle(CCSPlayerController player, string? search = null)
        {
            if (!CustomHud.IsHumanViewer(player) || (uint)player.Slot >= MaxSlots || _plugin == null)
            {
                return;
            }

            int slot = player.Slot;
            string text = (search ?? string.Empty).Trim();
            if (text.Length > MaxSearchLength)
            {
                text = text[..MaxSearchLength];
            }

            if (text.Length > 0)
            {
                _searchText[slot] = text;
                _filter[slot] = 0;
                if (_open[slot])
                {
                    _tab[slot] = InfoTab.Perks;
                    Paint(player);
                    return;
                }
            }
            else if (_open[slot])
            {
                Close(player);
                return;
            }

            InfoTab start = text.Length > 0 ? InfoTab.Perks : InfoTab.Round;
            int token = ++_token[slot];
            _ = _plugin.AddTimer(OpenDelaySeconds, () =>
            {
                if (_token[slot] != token
                    || Utilities.GetPlayerFromSlot(slot) is not { IsValid: true } live
                    || !_hud.EnsureEntity())
                {
                    return;
                }
                Open(live, start);
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        public static void Close(CCSPlayerController? player)
        {
            if (player is not { IsValid: true } || (uint)player.Slot >= MaxSlots)
            {
                return;
            }

            int slot = player.Slot;
            _token[slot]++; // a pending open or capture sees a stale token and does nothing
            if (!_open[slot])
            {
                return;
            }

            SetOpen(slot, false);
            _hud.SetInputCapture(player, false);
            _hud.Hide(player);
        }

        /// <summary>Something the menu shows changed (a roll, a death): repaint every open menu.</summary>
        public static void MarkAllDirty()
        {
            if (_openCount == 0)
            {
                return;
            }
            for (int slot = 0; slot < MaxSlots; slot++)
            {
                _dirty[slot] |= _open[slot];
            }
        }

        public static void OnTick()
        {
            _hud.Tick();
            if (_restoreCount > 0)
            {
                ContinueRestore();
            }
            if (_openCount == 0)
            {
                return;
            }

            // history shows relative times and the round list alive states: refresh both once per second
            float now = Server.CurrentTime;
            if (now >= _nextRefresh)
            {
                _nextRefresh = now + 1f;
                for (int slot = 0; slot < MaxSlots; slot++)
                {
                    _dirty[slot] |= _open[slot] && _tab[slot] is InfoTab.History or InfoTab.Round;
                }
            }

            int budget = PaintsPerTick;
            for (int slot = 0; slot < MaxSlots && budget > 0; slot++)
            {
                if (!_dirty[slot] || !_open[slot])
                {
                    continue;
                }
                if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } player)
                {
                    Paint(player);
                    budget--;
                }
                else
                {
                    _dirty[slot] = false;
                }
            }
        }

        /// <summary>Listener OnCustomHudClicked: only clicks on this layout from a player with the menu open.</summary>
        public static void OnClicked(CCSPlayerController player, CCSCustomHudLayout layout, string buttonId)
        {
            if (player is not { IsValid: true } || !IsOpen(player) || !_hud.Owns(layout) || _source == null)
            {
                return;
            }

            try
            {
                HandleClick(player, buttonId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RollTheDice][InfoMenu] Click '{buttonId}' failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Round restart: the client rebuilt the layout from XML. Close every menu and write the defaults back,
        /// spread over ticks (<see cref="RestoreWriteBudget"/>): a filled row pool would otherwise burst.
        /// </summary>
        public static void ResetRound()
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (!CustomHud.IsHumanViewer(player) || (uint)player.Slot >= MaxSlots)
                {
                    continue;
                }
                Close(player);
                SetRestoring(player.Slot, true);
            }
        }

        public static void Forget(int slot)
        {
            if ((uint)slot >= MaxSlots)
            {
                return;
            }
            _token[slot]++;
            SetOpen(slot, false);
            SetRestoring(slot, false);
            ResetState(slot);
            _hud.Forget(slot);
        }

        /// <summary>Map start: the entity went with the old map; spawn the new one a moment in, never inside a command.</summary>
        public static void OnMapStart()
        {
            ForgetAll();
            _hud.ResetForMap();
            WarmUp();
        }

        public static void WarmUp() => _ = _plugin?.AddTimer(WarmUpDelaySeconds, () => _hud.EnsureEntity(), TimerFlags.STOP_ON_MAPCHANGE);

        public static void Shutdown()
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (IsOpen(player))
                {
                    _hud.SetInputCapture(player, false);
                }
            }
            ForgetAll();
            _hud.Shutdown();
        }

        public static void FilterTransmit(CCheckTransmitInfoList infoList) => _hud.FilterTransmit(infoList);

        private static void Open(CCSPlayerController player, InfoTab tab)
        {
            int slot = player.Slot;
            if (_restoring[slot])
            {
                // the round-start restore has not finished: finish it before painting over it
                _ = _hud.RestoreDefaults(player);
                SetRestoring(slot, false);
            }

            SetOpen(slot, true);
            _tab[slot] = tab;
            Paint(player);
            _hud.Show(player);

            // capture one timer after the first paint, so spawn/first write and capture are not in one burst
            int token = _token[slot];
            _ = _plugin?.AddTimer(CaptureDelaySeconds, () =>
            {
                if (_token[slot] == token && _open[slot] && Utilities.GetPlayerFromSlot(slot) is { IsValid: true } live)
                {
                    _hud.SetInputCapture(live, true);
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        private static void HandleClick(CCSPlayerController player, string id)
        {
            int slot = player.Slot;
            switch (id)
            {
                case InfoMenuIds.CloseId:
                    Close(player);
                    return;
                case InfoMenuIds.AutoId:
                    if (_source!.Settings(player).AutoLocked)
                    {
                        return;
                    }
                    _source.ToggleAutoRoll(player);
                    break;
                case InfoMenuIds.SortId:
                    _sortByTone[slot] = !_sortByTone[slot];
                    break;
                case InfoMenuIds.SearchClearId:
                    _searchText[slot] = string.Empty;
                    break;
                default:
                    int tab = Array.IndexOf(InfoMenuIds.TabIds, id);
                    int lang = Array.IndexOf(InfoMenuIds.LangIds, id);
                    int filter = Array.IndexOf(InfoMenuIds.FilterIds, id);
                    if (tab >= 0)
                    {
                        _tab[slot] = (InfoTab)tab;
                    }
                    else if (lang >= 0)
                    {
                        _source!.SetLanguage(player, lang);
                    }
                    else if (filter >= 0)
                    {
                        _filter[slot] = filter;
                    }
                    else
                    {
                        return;
                    }
                    break;
            }

            // feedback within the same tick: paint now, not on the next scheduler pass
            Paint(player);
        }

        private static void Paint(CCSPlayerController player)
        {
            IInfoMenuSource? source = _source;
            int slot = player.Slot;
            _dirty[slot] = false;
            if (source == null)
            {
                return;
            }

            string T(string key) => source.Text(player, key);

            InfoTab tab = _tab[slot];
            _hud.SetGroup(player, _theme, HudTheme.ActiveIndex);
            _hud.SetText(player, _title, T("info.title"));
            _hud.SetText(player, _sub, T("info.subtitle"));
            _hud.SetText(player, _navHead, T("info.nav"));
            _hud.SetText(player, _hint, T("info.hint").Replace("{command}", HintText));
            _hud.SetText(player, _credit, CreditText);
            _hud.SetText(player, _section, T($"info.section.{TabKey(tab)}"));

            // every list is built on each paint: the tab counts need them all, and paints are rare
            foreach (InfoTab listTab in ListTabs)
            {
                List<InfoRow> rows = _rows[(int)listTab];
                rows.Clear();
                source.FillRows(player, listTab, rows);
            }

            for (int t = 0; t < InfoMenuIds.Tabs; t++)
            {
                _hud.SetText(player, _tabLabel[t], T($"info.tab.{TabKey((InfoTab)t)}"));
                _hud.SetText(player, _tabCount[t], t == (int)InfoTab.Settings ? string.Empty : NumberText(_rows[t].Count));
            }
            _hud.SetExclusive(player, _tabActive, (int)tab);

            switch (tab)
            {
                case InfoTab.Round:
                    _hud.SetGroup(player, _view, 0);
                    PaintRound(player, T);
                    break;
                case InfoTab.Settings:
                    _hud.SetGroup(player, _view, 3);
                    PaintSettings(player, source.Settings(player), T);
                    break;
                case InfoTab.Perks:
                    _hud.SetGroup(player, _view, 2);
                    PaintPerkTools(player, T);
                    PaintList(player, _shown, T(_searchText[slot].Length > 0 ? "info.empty.search" : "info.empty.perks"));
                    break;
                default:
                    _hud.SetGroup(player, _view, 1);
                    List<InfoRow> list = _rows[(int)tab];
                    _hud.SetText(player, _count, NumberText(list.Count));
                    PaintList(player, list, T($"info.empty.{TabKey(tab)}"));
                    break;
            }
        }

        /// <summary>Perk list: search filter first (it decides the chip counts), then the tone filter, then the sort.</summary>
        private static void PaintPerkTools(CCSPlayerController player, Func<string, string> T)
        {
            int slot = player.Slot;
            string search = _searchText[slot];
            List<InfoRow> all = _rows[(int)InfoTab.Perks];

            Array.Clear(_toneCount);
            _shown.Clear();
            InfoTone keep = FilterTones[_filter[slot]];
            foreach (InfoRow row in all)
            {
                if (search.Length > 0
                    && !row.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                    && !row.Desc.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                _toneCount[(int)InfoTone.None]++;
                _toneCount[(int)row.Tone]++;
                if (keep == InfoTone.None || row.Tone == keep)
                {
                    _shown.Add(row);
                }
            }

            bool byTone = _sortByTone[slot];
            _shown.Sort((a, b) =>
            {
                int order = byTone ? ToneOrder(a.Tone).CompareTo(ToneOrder(b.Tone)) : 0;
                return order != 0 ? order : StringComparer.InvariantCultureIgnoreCase.Compare(a.Title, b.Title);
            });

            for (int f = 0; f < InfoMenuIds.Filters; f++)
            {
                _hud.SetText(player, _filterLabel[f], T(FilterKeys[f]) + "  " + NumberText(_toneCount[(int)FilterTones[f]]));
            }
            _hud.SetExclusive(player, _filterActive, _filter[slot]);
            _hud.SetText(player, _sort, T(byTone ? "info.sort.tone" : "info.sort.name"));
            _hud.SetText(player, _count, NumberText(_shown.Count) + " / " + NumberText(all.Count));

            bool searching = search.Length > 0;
            _hud.SetText(player, _search, searching ? T("info.search.active").Replace("{text}", search) : string.Empty);
            _hud.SetText(player, _searchHint, T("info.search.hint").Replace("{command}", HintText));
            _hud.SetFlag(player, _searchOff, !searching);
        }

        private static void PaintSettings(CCSPlayerController player, InfoSettings settings, Func<string, string> T)
        {
            _hud.SetText(player, _autoHead, T("info.settings.auto_head"));
            _hud.SetText(player, _auto, T("info.settings.auto"));
            _hud.SetText(player, _autoState, T(settings.AutoOn ? "info.settings.on" : "info.settings.off"));
            _hud.SetText(player, _autoHint, settings.AutoHint);
            _hud.SetFlag(player, _autoOn, settings.AutoOn);
            _hud.SetFlag(player, _autoDisabled, settings.AutoLocked);
            _hud.SetText(player, _langHead, T("info.settings.lang_head"));
            _hud.SetText(player, _langHint, T("info.settings.lang_hint"));
            _hud.SetText(player, _langLabel[0], T("info.settings.lang_de"));
            _hud.SetText(player, _langLabel[1], T("info.settings.lang_en"));
            _hud.SetExclusive(player, _langActive, settings.Language);
        }

        /// <summary>
        /// One scrolled page over the row pool. Stops after <see cref="RowWriteBudget"/> writes and stays dirty, so the
        /// next ticks finish it (rows already sent cost nothing then: the diff cache drops them).
        /// </summary>
        private static void PaintList(CCSPlayerController player, List<InfoRow> rows, string emptyText)
        {
            int slot = player.Slot;
            _hud.SetText(player, _empty, emptyText);
            _hud.SetFlag(player, _emptyOff, rows.Count != 0);

            long limit = _hud.Writes + RowWriteBudget;
            for (int r = 0; r < InfoMenuIds.Rows; r++)
            {
                if (_hud.Writes >= limit)
                {
                    _dirty[slot] = true;
                    return;
                }

                if (r >= rows.Count)
                {
                    // hide only: stale text under is-off is invisible and saves writes
                    _hud.SetFlag(player, _rowOff[r], true);
                    continue;
                }

                InfoRow row = rows[r];
                _hud.SetText(player, _rowIndex[r], row.Badge ?? (r + 1 < _indexText.Length ? _indexText[r + 1] : NumberText(r + 1)));
                _hud.SetText(player, _rowTitle[r], row.Title);
                _hud.SetText(player, _rowMeta[r], row.Meta);
                _hud.SetText(player, _rowDesc[r], row.Desc);
                _hud.SetGroup(player, _rowTone[r], (int)row.Tone);
                _hud.SetGroup(player, _rowTeam[r], (int)row.Team);
                _hud.SetGroup(player, _rowLife[r], (int)row.Life);
                _hud.SetFlag(player, _rowSelf[r], row.Self);
                _hud.SetFlag(player, _rowOff[r], false);
            }
        }

        /// <summary>Round view: CT and T side by side; rows arrive sorted (alive first, then name), players without a team are left out.</summary>
        private static void PaintRound(CCSPlayerController player, Func<string, string> T)
        {
            _ct.Rows.Clear();
            _t.Rows.Clear();
            foreach (InfoRow row in _rows[(int)InfoTab.Round])
            {
                (row.Team == InfoTeam.Ct ? _ct.Rows : row.Team == InfoTeam.T ? _t.Rows : null)?.Add(row);
            }

            _hud.SetText(player, _count, NumberText(_ct.Rows.Count + _t.Rows.Count));
            // non-breaking spaces: the client localizes a dialog variable that equals one of its tokens (team names)
            _hud.SetText(player, _ct.Head, "\u00a0" + T("info.team.ct") + "\u00a0");
            _hud.SetText(player, _t.Head, "\u00a0" + T("info.team.t") + "\u00a0");
            string empty = T("info.empty.team");
            long limit = _hud.Writes + RowWriteBudget;
            if (!PaintTeam(player, _ct, empty, limit) || !PaintTeam(player, _t, empty, limit))
            {
                _dirty[player.Slot] = true; // budget used up: the next ticks finish it
            }
        }

        private static bool PaintTeam(CCSPlayerController player, TeamPool pool, string emptyText, long limit)
        {
            _hud.SetText(player, pool.Count, NumberText(pool.Rows.Count));
            _hud.SetText(player, pool.Empty, emptyText);
            _hud.SetFlag(player, pool.EmptyOff, pool.Rows.Count != 0);
            for (int r = 0; r < InfoMenuIds.TeamRows; r++)
            {
                if (_hud.Writes >= limit)
                {
                    return false;
                }

                if (r >= pool.Rows.Count)
                {
                    _hud.SetFlag(player, pool.Off[r], true);
                    continue;
                }

                InfoRow row = pool.Rows[r];
                _hud.SetText(player, pool.Name[r], row.Title);
                _hud.SetText(player, pool.Dice[r], row.Desc);
                _hud.SetGroup(player, pool.Tone[r], (int)row.Tone);
                _hud.SetGroup(player, pool.Life[r], (int)row.Life);
                _hud.SetFlag(player, pool.Self[r], row.Self);
                _hud.SetFlag(player, pool.Off[r], false);
            }
            return true;
        }

        private static void ContinueRestore()
        {
            int budget = PaintsPerTick;
            for (int slot = 0; slot < MaxSlots && budget > 0; slot++)
            {
                if (!_restoring[slot])
                {
                    continue;
                }
                budget--;
                if (Utilities.GetPlayerFromSlot(slot) is not { IsValid: true } player
                    || _hud.RestoreDefaults(player, RestoreWriteBudget))
                {
                    SetRestoring(slot, false);
                }
            }
        }

        private static void SetOpen(int slot, bool open)
        {
            if (_open[slot] == open)
            {
                return;
            }
            _open[slot] = open;
            _dirty[slot] = false;
            _openCount += open ? 1 : -1;
        }

        private static void SetRestoring(int slot, bool restoring)
        {
            if (_restoring[slot] == restoring)
            {
                return;
            }
            _restoring[slot] = restoring;
            _restoreCount += restoring ? 1 : -1;
        }

        private static void ResetState(int slot)
        {
            _tab[slot] = InfoTab.Round;
            _filter[slot] = 0;
            _sortByTone[slot] = false;
            _searchText[slot] = string.Empty;
        }

        private static void ForgetAll()
        {
            for (int slot = 0; slot < MaxSlots; slot++)
            {
                _token[slot]++;
                SetOpen(slot, false);
                SetRestoring(slot, false);
            }
        }

        private static int ToneOrder(InfoTone tone) => tone switch
        {
            InfoTone.Good => 0,
            InfoTone.Neutral => 1,
            InfoTone.Bad => 2,
            _ => 3,
        };

        private static string TabKey(InfoTab tab) => tab switch
        {
            InfoTab.Round => "round",
            InfoTab.History => "history",
            InfoTab.Perks => "perks",
            InfoTab.Commands => "commands",
            InfoTab.Settings => "settings",
            _ => "round",
        };

        private static string NumberText(int value) => (uint)value < _numberText.Length ? _numberText[value] : value.ToString();

        private static TextSlot[] Texts(string[] names)
        {
            TextSlot[] slots = new TextSlot[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                slots[i] = _hud.Text(InfoMenuIds.RootId, names[i]);
            }
            return slots;
        }

        /// <summary>authoredOn: index authored on, -1 none, -2 all.</summary>
        private static ClassSlot[] Flags(string[] panelIds, string className, int authoredOn)
        {
            ClassSlot[] slots = new ClassSlot[panelIds.Length];
            for (int i = 0; i < panelIds.Length; i++)
            {
                slots[i] = _hud.Flag(panelIds[i], className, authoredOn == -2 || authoredOn == i);
            }
            return slots;
        }

        private static ClassSlot[] Groups(string[] panelIds, string[] classes, int authored)
        {
            ClassSlot[] slots = new ClassSlot[panelIds.Length];
            for (int i = 0; i < panelIds.Length; i++)
            {
                slots[i] = _hud.Group(panelIds[i], classes, authored);
            }
            return slots;
        }

        private static List<InfoRow>[] BuildLists()
        {
            List<InfoRow>[] lists = new List<InfoRow>[InfoMenuIds.Tabs];
            for (int i = 0; i < lists.Length; i++)
            {
                lists[i] = [];
            }
            return lists;
        }

        private static string[] Filled(string value)
        {
            string[] values = new string[MaxSlots];
            Array.Fill(values, value);
            return values;
        }

        private static string[] BuildNumberText(int max, string format)
        {
            string[] values = new string[max + 1];
            for (int i = 0; i <= max; i++)
            {
                values[i] = i.ToString(format);
            }
            return values;
        }
    }
}
