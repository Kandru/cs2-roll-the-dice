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
        Admin,
    }

    /// <summary>Index into <see cref="InfoMenuIds.Tones"/> (the two strong tones came later, hence the order).</summary>
    public enum InfoTone
    {
        None,
        Good,
        Bad,
        Neutral,
        VeryGood,
        VeryBad,
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

    /// <summary>One player in the admin list. <see cref="UserId"/> identifies the target of a click.</summary>
    public readonly record struct AdminPlayerRow(
        int UserId,
        string Name,
        string Badge,
        InfoTeam Team,
        string Dice,
        InfoTone Tone,
        bool Alive,
        bool Locked);

    /// <summary>One perk in the admin list: own switch and whether its category is switched on.</summary>
    public readonly record struct AdminPerkRow(string ClassName, string Title, string Category, InfoTone Tone, bool On, bool CategoryOn);

    /// <summary>
    /// Global admin state. <see cref="CategoryOn"/> follows <see cref="InfoMenu.Categories"/>; Rollable counts the perks
    /// a roll can draw right now.
    /// </summary>
    public readonly record struct AdminState(bool Locked, bool[] CategoryOn, int Rollable, int Total);

    /// <summary>Data and actions behind the menu; implemented by the plugin.</summary>
    public interface IInfoMenuSource
    {
        string Text(CCSPlayerController? player, string key);
        InfoSettings Settings(CCSPlayerController player);
        void FillRows(CCSPlayerController player, InfoTab tab, List<InfoRow> rows);
        void ToggleAutoRoll(CCSPlayerController player);
        void SetLanguage(CCSPlayerController player, int language);

        // admin tab: every call is checked against IsAdmin by the menu and again by the plugin
        bool IsAdmin(CCSPlayerController player);
        AdminState Admin();
        void FillAdminPlayers(CCSPlayerController viewer, List<AdminPlayerRow> rows);
        void FillAdminPerks(CCSPlayerController viewer, List<AdminPerkRow> rows);
        void ToggleRollLock(CCSPlayerController admin);
        void ToggleCategory(CCSPlayerController admin, InfoTone tone);
        void TogglePerk(CCSPlayerController admin, string className);
        void TogglePlayerLock(CCSPlayerController admin, int userId);
        /// <summary>Rolls for one player (<paramref name="userId"/>) or every living player (null); a random allowed perk when <paramref name="className"/> is null.</summary>
        void AdminRoll(CCSPlayerController admin, int? userId, string? className);
    }

    /// <summary>
    /// Info menu opened with "!dice info" (info_v4.xml, own layout entity): this round's dice (start page), own
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
        /// <summary>The five perk categories from best to worst: filter chips after "all", admin category switches.</summary>
        public static readonly InfoTone[] Categories = [InfoTone.VeryGood, InfoTone.Good, InfoTone.Neutral, InfoTone.Bad, InfoTone.VeryBad];
        private static readonly string[] FilterKeys = ["info.filter.all", "info.filter.very_good", "info.filter.good", "info.filter.neutral", "info.filter.bad", "info.filter.very_bad"];
        // filter chip -> tone it keeps (None = all)
        private static readonly InfoTone[] FilterTones = [InfoTone.None, .. Categories];
        // admin pick target: none, or every living player (else a user id)
        private const int NoPick = int.MinValue;
        private const int PickAll = -1;

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

        // authored state of info_v4.xml: view-list, theme-gold, tab 0 and filter 0 active, search box/rows/empty off
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
        // the admin tab is authored hidden
        private static readonly ClassSlot _adminTabOff = _hud.Flag(InfoMenuIds.TabIds[(int)InfoTab.Admin], InfoMenuIds.OffClass, authoredOn: true);

        // admin view: authored asec-players, section 0 active, lock off, every category on, no pick, rows off
        private static readonly ClassSlot _adminSection = _hud.Group(InfoMenuIds.RootId, InfoMenuIds.AdminSectionClasses, authored: 0);
        private static readonly ClassSlot[] _adminSectionActive = Flags(InfoMenuIds.AdminSectionIds, InfoMenuIds.ActiveClass, authoredOn: 0);
        private static readonly ClassSlot _adminLockOn = _hud.Flag(InfoMenuIds.AdminLockId, InfoMenuIds.OnClass);
        private static readonly ClassSlot _adminPicking = _hud.Flag(InfoMenuIds.AdminPerksId, InfoMenuIds.OpenClass);
        private static readonly ClassSlot[] _adminCategoryOn = Flags(InfoMenuIds.AdminCategoryIds, InfoMenuIds.OnClass, authoredOn: -2);
        private static readonly ClassSlot _adminPerkEmptyOff = _hud.Flag(InfoMenuIds.AdminPerkEmptyId, InfoMenuIds.OffClass, authoredOn: true);
        private static readonly TextSlot _adminLock = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminLockVar);
        private static readonly TextSlot _adminLockState = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminLockStateVar);
        private static readonly TextSlot _adminAllRoll = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminAllRollVar);
        private static readonly TextSlot _adminAllGive = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminAllGiveVar);
        private static readonly TextSlot _adminPick = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminPickVar);
        private static readonly TextSlot _adminPerkEmpty = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminPerkEmptyVar);
        private static readonly TextSlot _adminRollLabel = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminRollLabelVar);
        private static readonly TextSlot _adminGiveLabel = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminGiveLabelVar);
        private static readonly TextSlot _adminLockLabel = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminLockLabelVar);
        private static readonly TextSlot _adminUnlockLabel = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminUnlockLabelVar);
        private static readonly TextSlot _adminOnLabel = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminOnVar);
        private static readonly TextSlot _adminOffLabel = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminOffVar);
        private static readonly TextSlot _adminCatOffLabel = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminCatOffVar);
        private static readonly TextSlot _adminPickLabel = _hud.Text(InfoMenuIds.RootId, InfoMenuIds.AdminPickLabelVar);
        private static readonly TextSlot[] _adminSectionLabel = Texts(InfoMenuIds.AdminSectionVars);
        private static readonly TextSlot[] _adminCategoryLabel = Texts(InfoMenuIds.AdminCategoryVars);
        private static readonly TextSlot[] _adminPlayerTeam = Texts(InfoMenuIds.AdminPlayerTeamVars);
        private static readonly TextSlot[] _adminPlayerName = Texts(InfoMenuIds.AdminPlayerNameVars);
        private static readonly TextSlot[] _adminPlayerDice = Texts(InfoMenuIds.AdminPlayerDiceVars);
        private static readonly ClassSlot[] _adminPlayerOff = Flags(InfoMenuIds.AdminPlayerIds, InfoMenuIds.OffClass, authoredOn: -2);
        private static readonly ClassSlot[] _adminPlayerLocked = Flags(InfoMenuIds.AdminPlayerIds, InfoMenuIds.OnClass, authoredOn: -1);
        private static readonly ClassSlot[] _adminPlayerTone = Groups(InfoMenuIds.AdminPlayerIds, InfoMenuIds.Tones, authored: (int)InfoTone.None);
        private static readonly ClassSlot[] _adminPlayerTeamClass = Groups(InfoMenuIds.AdminPlayerIds, InfoMenuIds.Teams, authored: (int)InfoTeam.None);
        private static readonly ClassSlot[] _adminPlayerLife = Groups(InfoMenuIds.AdminPlayerIds, InfoMenuIds.Lives, authored: (int)InfoLife.None);
        private static readonly TextSlot[] _adminPerkName = Texts(InfoMenuIds.AdminPerkNameVars);
        private static readonly TextSlot[] _adminPerkCategory = Texts(InfoMenuIds.AdminPerkCategoryVars);
        private static readonly ClassSlot[] _adminPerkOff = Flags(InfoMenuIds.AdminPerkIds, InfoMenuIds.OffClass, authoredOn: -2);
        private static readonly ClassSlot[] _adminPerkOn = Flags(InfoMenuIds.AdminPerkIds, InfoMenuIds.OnClass, authoredOn: -2);
        private static readonly ClassSlot[] _adminPerkCatOff = Flags(InfoMenuIds.AdminPerkIds, InfoMenuIds.CategoryOffClass, authoredOn: -1);
        private static readonly ClassSlot[] _adminPerkTone = Groups(InfoMenuIds.AdminPerkIds, InfoMenuIds.Tones, authored: (int)InfoTone.None);

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
        // server tick of the last paint: clicks beyond the first per tick only mark the menu dirty
        private static readonly int[] _paintedTick = new int[MaxSlots];
        private static readonly InfoTab[] _tab = new InfoTab[MaxSlots];
        private static readonly int[] _filter = new int[MaxSlots];
        private static readonly bool[] _sortByTone = new bool[MaxSlots];
        private static readonly string[] _searchText = Filled(string.Empty);
        // admin view per viewer: section, pick target (NoPick, PickAll or a user id) and what each row painted last
        private static readonly int[] _adminSectionIndex = new int[MaxSlots];
        private static readonly int[] _pickTarget = FilledInt(NoPick);
        private static readonly string[] _pickName = Filled(string.Empty);
        private static readonly int[][] _adminPlayerKeys = Jagged<int>(InfoMenuIds.AdminPlayerRows);
        private static readonly string?[][] _adminPerkKeys = Jagged<string?>(InfoMenuIds.AdminPerkRows);
        private static readonly List<AdminPlayerRow> _adminPlayers = [];
        private static readonly List<AdminPerkRow> _adminPerks = [];
        private static readonly List<InfoRow>[] _rows = BuildLists();
        private static readonly List<InfoRow> _shown = [];
        private static readonly int[] _toneCount = new int[InfoMenuIds.Tones.Length];
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
            string text = HudText.Clean(search?.Trim(), MaxSearchLength);

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
                    _dirty[slot] |= _open[slot] && _tab[slot] is InfoTab.History or InfoTab.Round or InfoTab.Admin;
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

        /// <summary>
        /// A player joined: the slot may still carry the previous connection's menu state on the entity. Write the XML
        /// defaults back over the next ticks (budgeted like a round start) before the menu is used.
        /// </summary>
        public static void Claim(CCSPlayerController? player)
        {
            if (!CustomHud.IsHumanViewer(player) || (uint)player!.Slot >= MaxSlots)
            {
                return;
            }
            ResetState(player.Slot);
            SetRestoring(player.Slot, true);
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
                    if (id.StartsWith("ri-a", StringComparison.Ordinal))
                    {
                        // admin ids: the permission is checked on every click, a hidden tab protects nothing
                        if (!_source!.IsAdmin(player) || !HandleAdminClick(player, id))
                        {
                            return;
                        }
                        break;
                    }

                    int tab = Array.IndexOf(InfoMenuIds.TabIds, id);
                    int lang = Array.IndexOf(InfoMenuIds.LangIds, id);
                    int filter = Array.IndexOf(InfoMenuIds.FilterIds, id);
                    if (tab == (int)InfoTab.Admin && !_source!.IsAdmin(player))
                    {
                        return;
                    }
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

            // feedback within the same tick: paint now, not on the next scheduler pass. A flood of clicks paints
            // once per tick; the rest only marks the menu dirty for the budgeted scheduler.
            if (_paintedTick[slot] == Server.TickCount)
            {
                _dirty[slot] = true;
                return;
            }
            Paint(player);
        }

        /// <summary>Admin view clicks (permission already checked). False: unknown id, nothing changed.</summary>
        private static bool HandleAdminClick(CCSPlayerController player, string id)
        {
            IInfoMenuSource source = _source!;
            int slot = player.Slot;
            switch (id)
            {
                case InfoMenuIds.AdminLockId:
                    source.ToggleRollLock(player);
                    MarkAllDirty();
                    return true;
                case InfoMenuIds.AdminAllRollId:
                    source.AdminRoll(player, null, null);
                    return true;
                case InfoMenuIds.AdminAllGiveId:
                    StartPick(slot, PickAll, string.Empty);
                    return true;
                case InfoMenuIds.AdminPickCloseId:
                    _pickTarget[slot] = NoPick;
                    return true;
                default:
                    break;
            }

            int section = Array.IndexOf(InfoMenuIds.AdminSectionIds, id);
            if (section >= 0)
            {
                _adminSectionIndex[slot] = section;
                _pickTarget[slot] = NoPick;
                return true;
            }

            int category = Array.IndexOf(InfoMenuIds.AdminCategoryIds, id);
            if (category >= 0)
            {
                source.ToggleCategory(player, Categories[category]);
                MarkAllDirty();
                return true;
            }

            int perk = Array.IndexOf(InfoMenuIds.AdminPerkIds, id);
            if (perk >= 0)
            {
                if (_adminPerkKeys[slot][perk] is not { } className)
                {
                    return false;
                }
                int target = _pickTarget[slot];
                if (target == NoPick)
                {
                    source.TogglePerk(player, className);
                    MarkAllDirty();
                    return true;
                }
                source.AdminRoll(player, target == PickAll ? null : target, className);
                _pickTarget[slot] = NoPick;
                _adminSectionIndex[slot] = 0;
                return true;
            }

            int row = Array.IndexOf(InfoMenuIds.AdminRollIds, id);
            if (row >= 0)
            {
                source.AdminRoll(player, _adminPlayerKeys[slot][row], null);
                return true;
            }

            row = Array.IndexOf(InfoMenuIds.AdminGiveIds, id);
            if (row >= 0)
            {
                int userId = _adminPlayerKeys[slot][row];
                StartPick(slot, userId, Utilities.GetPlayerFromUserid(userId) is { IsValid: true } target ? HudText.Clean(target.PlayerName, HudText.MaxNameLength) : string.Empty);
                return true;
            }

            row = Array.IndexOf(InfoMenuIds.AdminLockIds, id);
            if (row >= 0)
            {
                source.TogglePlayerLock(player, _adminPlayerKeys[slot][row]);
                MarkAllDirty();
                return true;
            }
            return false;
        }

        /// <summary>"Give a perk": the perk section opens in pick mode, the next perk click gives it.</summary>
        private static void StartPick(int slot, int target, string name)
        {
            _pickTarget[slot] = target;
            _pickName[slot] = name;
            _adminSectionIndex[slot] = 1;
        }

        private static void Paint(CCSPlayerController player)
        {
            IInfoMenuSource? source = _source;
            int slot = player.Slot;
            _dirty[slot] = false;
            _paintedTick[slot] = Server.TickCount;
            if (source == null)
            {
                return;
            }

            string T(string key) => source.Text(player, key);

            // the admin tab follows the permission at every paint (rights can change while the menu is open)
            bool admin = source.IsAdmin(player);
            if (_tab[slot] == InfoTab.Admin && !admin)
            {
                _tab[slot] = InfoTab.Round;
            }
            InfoTab tab = _tab[slot];
            _hud.SetFlag(player, _adminTabOff, !admin);
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
                _hud.SetText(player, _tabCount[t], t is (int)InfoTab.Settings or (int)InfoTab.Admin ? string.Empty : NumberText(_rows[t].Count));
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
                case InfoTab.Admin:
                    _hud.SetGroup(player, _view, 4);
                    PaintAdmin(player, source, T);
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
        /// Admin view: global lock, section chips, then the player list or the perk list (pick mode while a perk is
        /// chosen for a player). Rows stop at <see cref="RowWriteBudget"/> writes like every list.
        /// </summary>
        private static void PaintAdmin(CCSPlayerController player, IInfoMenuSource source, Func<string, string> T)
        {
            int slot = player.Slot;
            AdminState state = source.Admin();
            _hud.SetText(player, _adminLock, T("info.admin.lock"));
            _hud.SetText(player, _adminLockState, T(state.Locked ? "info.admin.locked" : "info.admin.unlocked"));
            _hud.SetFlag(player, _adminLockOn, state.Locked);
            _hud.SetText(player, _count, NumberText(state.Rollable) + " / " + NumberText(state.Total));

            // shared row words: one variable each on the root, read by every row
            _hud.SetText(player, _adminRollLabel, T("info.admin.roll"));
            _hud.SetText(player, _adminGiveLabel, T("info.admin.give"));
            _hud.SetText(player, _adminLockLabel, T("info.admin.lock_player"));
            _hud.SetText(player, _adminUnlockLabel, T("info.admin.unlock_player"));
            _hud.SetText(player, _adminOnLabel, T("info.settings.on"));
            _hud.SetText(player, _adminOffLabel, T("info.settings.off"));
            _hud.SetText(player, _adminCatOffLabel, T("info.admin.category_off"));
            _hud.SetText(player, _adminPickLabel, T("info.admin.pick_row"));

            _adminPlayers.Clear();
            _adminPerks.Clear();
            source.FillAdminPlayers(player, _adminPlayers);
            source.FillAdminPerks(player, _adminPerks);
            _hud.SetText(player, _adminSectionLabel[0], T("info.admin.players") + "  " + NumberText(_adminPlayers.Count));
            _hud.SetText(player, _adminSectionLabel[1], T("info.admin.perks") + "  " + NumberText(state.Rollable));

            // a picked player who left ends the pick
            int target = _pickTarget[slot];
            if (target >= 0 && Utilities.GetPlayerFromUserid(target) is not { IsValid: true })
            {
                _pickTarget[slot] = target = NoPick;
            }
            int section = _adminSectionIndex[slot];
            _hud.SetGroup(player, _adminSection, section);
            _hud.SetExclusive(player, _adminSectionActive, section);
            _hud.SetFlag(player, _adminPicking, target != NoPick);
            _hud.SetText(player, _adminPick, target == NoPick
                ? string.Empty
                : target == PickAll
                    ? T("info.admin.pick_all")
                    : T("info.admin.pick").Replace("{player}", _pickName[slot]));

            if (section == 0)
            {
                _hud.SetText(player, _adminAllRoll, T("info.admin.roll_all"));
                _hud.SetText(player, _adminAllGive, T("info.admin.give_all"));
                PaintAdminPlayers(player);
                return;
            }

            for (int c = 0; c < InfoMenuIds.Categories; c++)
            {
                _hud.SetText(player, _adminCategoryLabel[c], T(ToneKey(Categories[c])));
                _hud.SetFlag(player, _adminCategoryOn[c], state.CategoryOn[c]);
            }
            _hud.SetText(player, _adminPerkEmpty, T("info.empty.perks"));
            _hud.SetFlag(player, _adminPerkEmptyOff, _adminPerks.Count != 0);
            PaintAdminPerks(player);
        }

        private static void PaintAdminPlayers(CCSPlayerController player)
        {
            int slot = player.Slot;
            int[] keys = _adminPlayerKeys[slot];
            long limit = _hud.Writes + RowWriteBudget;
            for (int r = 0; r < InfoMenuIds.AdminPlayerRows; r++)
            {
                if (_hud.Writes >= limit)
                {
                    _dirty[slot] = true;
                    return;
                }

                if (r >= _adminPlayers.Count)
                {
                    _hud.SetFlag(player, _adminPlayerOff[r], true);
                    continue;
                }

                AdminPlayerRow row = _adminPlayers[r];
                keys[r] = row.UserId;
                _hud.SetText(player, _adminPlayerTeam[r], row.Badge);
                _hud.SetText(player, _adminPlayerName[r], row.Name);
                _hud.SetText(player, _adminPlayerDice[r], row.Dice);
                _hud.SetGroup(player, _adminPlayerTone[r], (int)row.Tone);
                _hud.SetGroup(player, _adminPlayerTeamClass[r], (int)row.Team);
                _hud.SetGroup(player, _adminPlayerLife[r], (int)(row.Alive ? InfoLife.Alive : InfoLife.Dead));
                _hud.SetFlag(player, _adminPlayerLocked[r], row.Locked);
                _hud.SetFlag(player, _adminPlayerOff[r], false);
            }
        }

        private static void PaintAdminPerks(CCSPlayerController player)
        {
            int slot = player.Slot;
            string?[] keys = _adminPerkKeys[slot];
            long limit = _hud.Writes + RowWriteBudget;
            for (int r = 0; r < InfoMenuIds.AdminPerkRows; r++)
            {
                if (_hud.Writes >= limit)
                {
                    _dirty[slot] = true;
                    return;
                }

                if (r >= _adminPerks.Count)
                {
                    keys[r] = null;
                    _hud.SetFlag(player, _adminPerkOff[r], true);
                    continue;
                }

                AdminPerkRow row = _adminPerks[r];
                keys[r] = row.ClassName;
                _hud.SetText(player, _adminPerkName[r], row.Title);
                _hud.SetText(player, _adminPerkCategory[r], row.Category);
                _hud.SetGroup(player, _adminPerkTone[r], (int)row.Tone);
                _hud.SetFlag(player, _adminPerkOn[r], row.On);
                _hud.SetFlag(player, _adminPerkCatOff[r], !row.CategoryOn);
                _hud.SetFlag(player, _adminPerkOff[r], false);
            }
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
            _adminSectionIndex[slot] = 0;
            _pickTarget[slot] = NoPick;
            _pickName[slot] = string.Empty;
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

        /// <summary>Best to worst (sort by type, admin perk list).</summary>
        public static int ToneOrder(InfoTone tone) => tone switch
        {
            InfoTone.VeryGood => 0,
            InfoTone.Good => 1,
            InfoTone.Neutral => 2,
            InfoTone.Bad => 3,
            InfoTone.VeryBad => 4,
            _ => 5,
        };

        /// <summary>Language key of a category name.</summary>
        public static string ToneKey(InfoTone tone) => tone switch
        {
            InfoTone.VeryGood => "info.tone.very_good",
            InfoTone.Good => "info.tone.good",
            InfoTone.Bad => "info.tone.bad",
            InfoTone.VeryBad => "info.tone.very_bad",
            _ => "info.tone.neutral",
        };

        private static string TabKey(InfoTab tab) => tab switch
        {
            InfoTab.Round => "round",
            InfoTab.History => "history",
            InfoTab.Perks => "perks",
            InfoTab.Commands => "commands",
            InfoTab.Settings => "settings",
            InfoTab.Admin => "admin",
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

        private static int[] FilledInt(int value)
        {
            int[] values = new int[MaxSlots];
            Array.Fill(values, value);
            return values;
        }

        private static T[][] Jagged<T>(int rows)
        {
            T[][] values = new T[MaxSlots][];
            for (int i = 0; i < MaxSlots; i++)
            {
                values[i] = new T[rows];
            }
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
