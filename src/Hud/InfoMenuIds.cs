namespace RollTheDice.Hud
{
    /// <summary>
    /// Contract of info_v5.xml: the only place its ids, dialog variables and classes are spelled.
    /// Pool sizes and prefixes must match tools/gen_info_panorama.py (validated by make panorama).
    /// </summary>
    public static class InfoMenuIds
    {
        public const string Resource = "panorama/layout/custom_game/rollthedice/info_v5.vxml_c";

        public const int Rows = 112;
        public const int Tabs = 6;
        public const int Languages = 2;
        public const int Filters = 6;
        public const int TeamRows = 32;
        public const int AdminPlayerRows = 64;
        public const int AdminPerkRows = 112;
        public const int Categories = 5;
        public const int AdminSections = 2;

        // panel ids
        public const string RootId = "ri-root";
        public const string CloseId = "ri-close";
        public const string AutoId = "ri-auto";
        public const string EmptyId = "ri-empty";
        public const string SortId = "ri-sort";
        public const string SearchId = "ri-search";
        public const string SearchClearId = "ri-search-clear";
        public const string TabIdPrefix = "ri-tab-";
        public const string LangIdPrefix = "ri-lang-";
        public const string RowIdPrefix = "ri-row-";
        public const string FilterIdPrefix = "ri-flt-";
        public const string CtRowIdPrefix = "ri-ctr-";
        public const string TRowIdPrefix = "ri-tr-";
        public const string CtEmptyId = "ri-ct-empty";
        public const string TEmptyId = "ri-t-empty";
        public const string AdminLockId = "ri-alock";
        public const string AdminMirrorId = "ri-amirror";
        public const string AdminAllRollId = "ri-aall-roll";
        public const string AdminAllGiveId = "ri-aall-give";
        public const string AdminPerksId = "ri-aperks";
        public const string AdminPickCloseId = "ri-apick-x";
        public const string AdminPerkEmptyId = "ri-ak-empty";
        public const string AdminPlayerIdPrefix = "ri-ap-";
        public const string AdminRollIdPrefix = "ri-apr-";
        public const string AdminGiveIdPrefix = "ri-apg-";
        public const string AdminLockIdPrefix = "ri-apl-";
        public const string AdminPerkIdPrefix = "ri-ak-";
        public const string AdminCategoryIdPrefix = "ri-acat-";
        public const string AdminSectionIdPrefix = "ri-asec-";

        // dialog variables (all on the root)
        public const string TitleVar = "ri_title";
        public const string SubVar = "ri_sub";
        public const string NavHeadVar = "ri_nav_h";
        public const string SectionVar = "ri_sec";
        public const string CountVar = "ri_count";
        public const string EmptyVar = "ri_empty";
        public const string AutoHeadVar = "ri_auto_h";
        public const string AutoVar = "ri_auto";
        public const string AutoStateVar = "ri_auto_state";
        public const string AutoHintVar = "ri_auto_hint";
        public const string LangHeadVar = "ri_lang_h";
        public const string LangHintVar = "ri_lang_hint";
        public const string SortVar = "ri_sort";
        public const string SearchVar = "ri_search";
        public const string SearchHintVar = "ri_search_hint";
        public const string HintVar = "ri_hint";
        public const string CreditVar = "ri_credit";
        public const string TabVarPrefix = "ri_t";        // ri_t<n>
        public const string TabCountVarPrefix = "ri_tc";  // ri_tc<n>
        public const string LangVarPrefix = "ri_l";       // ri_l<n>
        public const string FilterVarPrefix = "ri_f";     // ri_f<n>
        public const string RowVarPrefix = "ri_r";        // ri_r<n>_i / _a / _b / _c
        public const string CtHeadVar = "ri_ct_h";
        public const string CtCountVar = "ri_ct_n";
        public const string CtEmptyVar = "ri_ct_empty";
        public const string THeadVar = "ri_t_h";
        public const string TCountVar = "ri_t_n";
        public const string TEmptyVar = "ri_t_empty";
        public const string CtRowVarPrefix = "ri_ctr";    // ri_ctr<n>_a (name) / _c (dice)
        public const string TRowVarPrefix = "ri_tr";      // ri_tr<n>_a / _c
        // admin view
        public const string AdminLockVar = "ri_alock";
        public const string AdminLockStateVar = "ri_alock_state";
        public const string AdminMirrorVar = "ri_amirror";
        public const string AdminMirrorStateVar = "ri_amirror_state";
        public const string AdminAllRollVar = "ri_aall_roll";
        public const string AdminAllGiveVar = "ri_aall_give";
        public const string AdminPickVar = "ri_apick";
        public const string AdminPerkEmptyVar = "ri_ak_empty";
        // button and state words shared by every admin row (one variable on the root)
        public const string AdminRollLabelVar = "ri_ab_roll";
        public const string AdminGiveLabelVar = "ri_ab_give";
        public const string AdminLockLabelVar = "ri_ab_lock";
        public const string AdminUnlockLabelVar = "ri_ab_unlock";
        public const string AdminOnVar = "ri_ab_on";
        public const string AdminOffVar = "ri_ab_off";
        public const string AdminCatOffVar = "ri_ab_catoff";
        public const string AdminPickLabelVar = "ri_ab_pick";
        public const string AdminPlayerVarPrefix = "ri_ap";     // ri_ap<n>_t (team) / _a (name) / _c (dice)
        public const string AdminPerkVarPrefix = "ri_ak";       // ri_ak<n>_a (name) / _b (category)
        public const string AdminCategoryVarPrefix = "ri_ac";   // ri_ac<n>
        public const string AdminSectionVarPrefix = "ri_as";    // ri_as<n>

        // classes
        public const string ActiveClass = "is-active";
        public const string DisabledClass = "is-disabled";
        public const string OnClass = "is-on";
        public const string OffClass = "is-off";
        public const string OpenClass = "is-open";
        public const string SelfClass = "is-self";
        public const string CategoryOffClass = "cat-off";
        public static readonly string[] Views = ["view-round", "view-list", "view-perks", "view-settings", "view-admin"];
        public static readonly string[] AdminSectionClasses = ["asec-players", "asec-perks"];
        // index = InfoTone
        public static readonly string[] Tones = ["tone-none", "tone-good", "tone-bad", "tone-neutral", "tone-very-good", "tone-very-bad"];
        public static readonly string[] Teams = ["team-none", "team-ct", "team-t"];
        public static readonly string[] Lives = ["life-alive", "life-dead"];

        public static readonly string[] TabIds = Indexed(TabIdPrefix, Tabs, string.Empty);
        public static readonly string[] LangIds = Indexed(LangIdPrefix, Languages, string.Empty);
        public static readonly string[] FilterIds = Indexed(FilterIdPrefix, Filters, string.Empty);
        public static readonly string[] RowIds = Indexed(RowIdPrefix, Rows, string.Empty);
        public static readonly string[] TabVars = Indexed(TabVarPrefix, Tabs, string.Empty);
        public static readonly string[] TabCountVars = Indexed(TabCountVarPrefix, Tabs, string.Empty);
        public static readonly string[] LangVars = Indexed(LangVarPrefix, Languages, string.Empty);
        public static readonly string[] FilterVars = Indexed(FilterVarPrefix, Filters, string.Empty);
        public static readonly string[] RowIndexVars = Indexed(RowVarPrefix, Rows, "_i");
        public static readonly string[] RowTitleVars = Indexed(RowVarPrefix, Rows, "_a");
        public static readonly string[] RowMetaVars = Indexed(RowVarPrefix, Rows, "_b");
        public static readonly string[] RowDescVars = Indexed(RowVarPrefix, Rows, "_c");
        public static readonly string[] CtRowIds = Indexed(CtRowIdPrefix, TeamRows, string.Empty);
        public static readonly string[] TRowIds = Indexed(TRowIdPrefix, TeamRows, string.Empty);
        public static readonly string[] CtRowNameVars = Indexed(CtRowVarPrefix, TeamRows, "_a");
        public static readonly string[] CtRowDiceVars = Indexed(CtRowVarPrefix, TeamRows, "_c");
        public static readonly string[] TRowNameVars = Indexed(TRowVarPrefix, TeamRows, "_a");
        public static readonly string[] TRowDiceVars = Indexed(TRowVarPrefix, TeamRows, "_c");
        public static readonly string[] AdminPlayerIds = Indexed(AdminPlayerIdPrefix, AdminPlayerRows, string.Empty);
        public static readonly string[] AdminRollIds = Indexed(AdminRollIdPrefix, AdminPlayerRows, string.Empty);
        public static readonly string[] AdminGiveIds = Indexed(AdminGiveIdPrefix, AdminPlayerRows, string.Empty);
        public static readonly string[] AdminLockIds = Indexed(AdminLockIdPrefix, AdminPlayerRows, string.Empty);
        public static readonly string[] AdminPerkIds = Indexed(AdminPerkIdPrefix, AdminPerkRows, string.Empty);
        public static readonly string[] AdminCategoryIds = Indexed(AdminCategoryIdPrefix, Categories, string.Empty);
        public static readonly string[] AdminSectionIds = Indexed(AdminSectionIdPrefix, AdminSections, string.Empty);
        public static readonly string[] AdminPlayerTeamVars = Indexed(AdminPlayerVarPrefix, AdminPlayerRows, "_t");
        public static readonly string[] AdminPlayerNameVars = Indexed(AdminPlayerVarPrefix, AdminPlayerRows, "_a");
        public static readonly string[] AdminPlayerDiceVars = Indexed(AdminPlayerVarPrefix, AdminPlayerRows, "_c");
        public static readonly string[] AdminPerkNameVars = Indexed(AdminPerkVarPrefix, AdminPerkRows, "_a");
        public static readonly string[] AdminPerkCategoryVars = Indexed(AdminPerkVarPrefix, AdminPerkRows, "_b");
        public static readonly string[] AdminCategoryVars = Indexed(AdminCategoryVarPrefix, Categories, string.Empty);
        public static readonly string[] AdminSectionVars = Indexed(AdminSectionVarPrefix, AdminSections, string.Empty);

        private static string[] Indexed(string prefix, int count, string suffix)
        {
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = prefix + i + suffix;
            }
            return names;
        }
    }
}
