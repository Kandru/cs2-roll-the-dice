namespace RollTheDice.Hud
{
    /// <summary>
    /// Contract of info_v4.xml: the only place its ids, dialog variables and classes are spelled.
    /// Pool sizes and prefixes must match tools/gen_info_panorama.py (validated by make panorama).
    /// </summary>
    public static class InfoMenuIds
    {
        public const string Resource = "panorama/layout/custom_game/rollthedice/info_v4.vxml_c";

        public const int Rows = 112;
        public const int Tabs = 5;
        public const int Languages = 2;
        public const int Filters = 4;
        public const int TeamRows = 32;

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

        // classes
        public const string ActiveClass = "is-active";
        public const string DisabledClass = "is-disabled";
        public const string OnClass = "is-on";
        public const string OffClass = "is-off";
        public const string SelfClass = "is-self";
        public static readonly string[] Views = ["view-round", "view-list", "view-perks", "view-settings"];
        public static readonly string[] Tones = ["tone-none", "tone-good", "tone-bad", "tone-neutral"];
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
