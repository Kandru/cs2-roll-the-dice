#!/usr/bin/env python3
"""Validate the Roll The Dice Panorama addon (workshop/content/panorama)."""
from __future__ import annotations

import importlib.util
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / "workshop" / "content" / "panorama"
LAYOUTS = CONTENT / "layout" / "custom_game" / "rollthedice"
STYLES = CONTENT / "styles" / "custom_game" / "rollthedice"
PREVIEW = ROOT / "workshop" / "preview"
HUD_VCSS = STYLES / "hud.vcss"
INFO_VCSS = STYLES / "info_v3.vcss"
BANNER_XML = LAYOUTS / "banner.xml"
INFO_XML = LAYOUTS / "info_v3.xml"
KIT_CSS = PREVIEW / "kit.css"
BANNER_HTML = PREVIEW / "banner.html"
REEL_HTML = PREVIEW / "reel.html"
BANNER_CS = ROOT / "src" / "Hud" / "Banner.cs"
CUSTOM_HUD_CS = ROOT / "src" / "Hud" / "CustomHud.cs"
REEL_CS = ROOT / "src" / "Hud" / "Reel.cs"
THEME_CS = ROOT / "src" / "Hud" / "HudTheme.cs"
GEN_REEL = ROOT / "tools" / "gen_reel_panorama.py"
GEN_INFO = ROOT / "tools" / "gen_info_panorama.py"
INFO_IDS_CS = ROOT / "src" / "Hud" / "InfoMenuIds.cs"
NAME_BUDGET = 1244  # DialogVariableNames per layout
# Language-neutral glyphs allowed as literal Label text (ruleset 4.2).
LITERAL_GLYPHS = {"//", "‹", "›"}

# name -> accent hex (rainbow order; keep in sync with HudTheme / hud.vcss).
THEMES: list[tuple[str, str]] = [
    ("red", "#e05a5a"),
    ("coral", "#ff7a59"),
    ("orange", "#ff8a3d"),
    ("copper", "#e08a5a"),
    ("gold", "#f0a531"),
    ("t", "#eabe54"),
    ("lime", "#c6f04a"),
    ("green", "#6fdc78"),
    ("teal", "#2ec9a6"),
    ("cyan", "#3ee0f0"),
    ("blue", "#5b8cff"),
    ("ct", "#96c8fa"),
    ("purple", "#b07cff"),
    ("magenta", "#e85dff"),
    ("pink", "#ff6b9a"),
    ("silver", "#c9d2dc"),
]
THEME_NAMES = [name for name, _ in THEMES]
EXPECTED_BARS = [(name, accent.lower()) for name, accent in THEMES]

VAR_RE = re.compile(r"\{s:([A-Za-z0-9_]+)\}")
DIALOG_ONLY_RE = re.compile(r"^\{s:[A-Za-z0-9_]+\}$")
HIDDEN_CLASS = re.compile(r"(?<![\w-])(?:hidden|Hidden)(?![\w-])")
BAD_CSS = re.compile(
    r"(?:"
    r"(?:[{;]|^)\s*display\s*:"
    r"|\b(?:rgba?|hsla?|calc|var)\s*\("
    r"|background-size\s*:\s*contain\b"
    r"|@media\b"
    r"|@define\b"
    r"|::(?:before|after)\b"
    r"|visibility\s*:\s*hidden\b"
    r"|overflow\s*:\s*hidden\b"
    r"|font-size\s*:\s*\d+(?:px|em|rem|%)\b"
    r"|vertical-align\s*:\s*center\b"
    r"|\bmargin(?:-(?:top|right|bottom|left))?\s*:[^;]*-\d"  # negative margins: unproven, use rotateZ flips
    r"|transition-property\s*:[^;]*box-shadow"  # unproven transition target
    r")",
    re.M | re.I,
)
PANEL_ID_RE = re.compile(r'\bid="([^"]+)"')
CS_PANEL_RE = re.compile(r'Panel\s*=\s*"([^"]+)"')
CS_TEXT_VAR_RE = re.compile(r'TextVar\s*=\s*"([^"]+)"')
CS_SET_TEXT_RE = re.compile(r'SetText\([^,]+,\s*[^,]+,\s*"([^"]+)"')
CS_VAR_CONST_RE = re.compile(r'const\s+string\s+\w+Var\s*=\s*"([^"]+)"')
CS_INT_CONST_RE = r"const\s+int\s+{name}\s*=\s*(\d+)\s*;"
CS_STRING_CONST_RE = r'const\s+string\s+{name}\s*=\s*"([^"]+)"'
THEME_BAR_RE = re.compile(
    r"\.theme-([a-z]+)\s+\.ph-bar\s*\{\s*background(?:-color)?\s*:\s*(#[0-9a-fA-F]{6})\s*;\s*\}"
)
THEMES_JS_RE = re.compile(r"const\s+THEMES\s*=\s*\{(.*?)\};", re.S)
THEMES_JS_ENTRY_RE = re.compile(r"([a-z]+)\s*:\s*'(#[0-9a-fA-F]{6})'")
HUD_THEME_ALL_RE = re.compile(r"string\[\]\s+All\s*=\s*\[(.*?)\]\s*;", re.S)
HUD_THEME_NAME_RE = re.compile(r'"([a-z]+)"')


def fail(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def expect_theme_bars(pairs: list[tuple[str, str]], label: str) -> None:
    got = [(name, accent.lower()) for name, accent in pairs]
    if got != EXPECTED_BARS:
        fail(f"{label}: theme bars must match palette order/colors (got {got})")


def check_themes() -> None:
    if not HUD_VCSS.is_file():
        fail(f"missing stylesheet {HUD_VCSS}")
    if not KIT_CSS.is_file():
        fail(f"missing {KIT_CSS}")
    if not BANNER_HTML.is_file():
        fail(f"missing {BANNER_HTML}")
    if not THEME_CS.is_file():
        fail(f"missing {THEME_CS}")

    for sheet in (HUD_VCSS, INFO_VCSS):
        if not sheet.is_file():
            fail(f"missing stylesheet {sheet}")
        sheet_css = sheet.read_text(encoding="utf-8")
        if HIDDEN_CLASS.search(sheet_css):
            fail(f"{sheet.name} uses forbidden class name hidden/Hidden")
        bad = BAD_CSS.search(sheet_css)
        if bad:
            fail(f"{sheet.name} has unsupported CSS near: {bad.group(0)!r}")

    css = HUD_VCSS.read_text(encoding="utf-8")

    expect_theme_bars(THEME_BAR_RE.findall(css), "hud.vcss")
    expect_theme_bars(THEME_BAR_RE.findall(KIT_CSS.read_text(encoding="utf-8")), "kit.css")

    for preview in (BANNER_HTML, REEL_HTML):
        if not preview.is_file():
            fail(f"missing {preview}")
        match = THEMES_JS_RE.search(preview.read_text(encoding="utf-8"))
        if not match:
            fail(f"{preview.name}: missing const THEMES = {{ ... }}")
        expect_theme_bars(THEMES_JS_ENTRY_RE.findall(match.group(1)), f"{preview.name} THEMES")

    theme_cs = THEME_CS.read_text(encoding="utf-8")
    all_match = HUD_THEME_ALL_RE.search(theme_cs)
    if not all_match:
        fail("HudTheme.cs: missing All string array")
    found = HUD_THEME_NAME_RE.findall(all_match.group(1))
    if found != THEME_NAMES:
        fail(f"HudTheme.cs All must match palette ({', '.join(THEME_NAMES)})")


def cs_const(source: str, pattern: str, name: str, label: str) -> str:
    match = re.search(pattern.format(name=name), source)
    if not match:
        fail(f"{label}: missing const {name}")
    return match.group(1)


def load_generator(path: Path = GEN_REEL):
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location(path.stem, path)
    if spec is None or spec.loader is None:
        fail(f"cannot load {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def reel_contract() -> tuple[set[str], set[str]]:
    """Expand the Reel.cs row pool into panel ids and dialog vars; check it matches the generator."""
    source = REEL_CS.read_text(encoding="utf-8")
    rows = int(cs_const(source, CS_INT_CONST_RE, "RowCount", "Reel.cs"))
    win = int(cs_const(source, CS_INT_CONST_RE, "WinRow", "Reel.cs"))
    row_prefix = cs_const(source, CS_STRING_CONST_RE, "RowIdPrefix", "Reel.cs")
    var_prefix = cs_const(source, CS_STRING_CONST_RE, "FillerVarPrefix", "Reel.cs")

    gen = load_generator()
    if (rows, win, row_prefix, var_prefix) != (gen.ROW_COUNT, gen.WIN_ROW, gen.ROW_ID_PREFIX, gen.FILLER_VAR_PREFIX):
        fail("Reel.cs RowCount/WinRow/RowIdPrefix/FillerVarPrefix differ from tools/gen_reel_panorama.py")
    if [name for name, _ in gen.THEMES] != THEME_NAMES or [a for _, a in gen.THEMES] != [a for _, a in THEMES]:
        fail("tools/gen_reel_panorama.py THEMES must match the palette")
    # Panorama caps fit-children to the parent: the strip needs its full height spelled out.
    css = HUD_VCSS.read_text(encoding="utf-8")
    strip = re.search(r"\.RrStrip\s*\{([^}]*)\}", css)
    row = re.search(r"\.RrRow\s*\{([^}]*)\}", css)
    strip_height = re.search(r"height:\s*(\d+)px", strip.group(1)) if strip else None
    row_height = re.search(r"height:\s*(\d+)px", row.group(1)) if row else None
    if not strip_height or not row_height or int(row_height.group(1)) != gen.ROW_HEIGHT:
        fail(f"hud.vcss: .RrRow needs height: {gen.ROW_HEIGHT}px and .RrStrip an explicit px height")
    if int(strip_height.group(1)) != rows * gen.ROW_HEIGHT:
        fail(f"hud.vcss: .RrStrip height must be {rows * gen.ROW_HEIGHT}px (RowCount * {gen.ROW_HEIGHT}px)")

    for path, text in gen.render().items():
        if path.read_text(encoding="utf-8") != text:
            fail(f"{path.name}: generated reel block out of date - run tools/gen_reel_panorama.py")

    ids = {f"{row_prefix}{i}" for i in range(rows) if i != win}
    variables = {f"{var_prefix}{i}" for i in range(rows) if i != win}
    return ids, variables


def info_contract() -> tuple[set[str], set[str]]:
    """Expand InfoMenuIds.cs into panel ids and dialog vars; check it matches the generator."""
    source = INFO_IDS_CS.read_text(encoding="utf-8")
    rows = int(cs_const(source, CS_INT_CONST_RE, "Rows", "InfoMenuIds.cs"))
    tabs = int(cs_const(source, CS_INT_CONST_RE, "Tabs", "InfoMenuIds.cs"))
    langs = int(cs_const(source, CS_INT_CONST_RE, "Languages", "InfoMenuIds.cs"))
    filters = int(cs_const(source, CS_INT_CONST_RE, "Filters", "InfoMenuIds.cs"))
    team_rows = int(cs_const(source, CS_INT_CONST_RE, "TeamRows", "InfoMenuIds.cs"))

    def const(name: str) -> str:
        return cs_const(source, CS_STRING_CONST_RE, name, "InfoMenuIds.cs")

    gen = load_generator(GEN_INFO)
    if (rows, tabs, langs, filters, team_rows) != (gen.ROWS, gen.TABS, gen.LANGUAGES, gen.FILTERS, gen.TEAM_ROWS):
        fail("InfoMenuIds.cs Rows/Tabs/Languages/Filters/TeamRows differ from tools/gen_info_panorama.py")
    if [(k, p) for k, p in gen.TEAMS] != [("ct", const("CtRowIdPrefix")), ("t", const("TRowIdPrefix"))]:
        fail("InfoMenuIds.cs team row prefixes differ from tools/gen_info_panorama.py")
    if (const("RootId"), const("TabIdPrefix"), const("LangIdPrefix"), const("RowIdPrefix"), const("FilterIdPrefix")) != (
        gen.ROOT_ID, gen.TAB_ID_PREFIX, gen.LANG_ID_PREFIX, gen.ROW_ID_PREFIX, gen.FILTER_ID_PREFIX
    ):
        fail("InfoMenuIds.cs id prefixes differ from tools/gen_info_panorama.py")
    if [name for name, _ in gen.THEMES] != THEME_NAMES or [a for _, a in gen.THEMES] != [a for _, a in THEMES]:
        fail("tools/gen_info_panorama.py THEMES must match the palette")
    for path, text in gen.render().items():
        if not path.is_file() or path.read_text(encoding="utf-8") != text:
            fail(f"{path.name}: generated info menu out of date - run tools/gen_info_panorama.py")

    ids = set(re.findall(r'const\s+string\s+\w+Id\s*=\s*"([^"]+)"', source))
    ids |= {f"{const('TabIdPrefix')}{i}" for i in range(tabs)}
    ids |= {f"{const('LangIdPrefix')}{i}" for i in range(langs)}
    ids |= {f"{const('RowIdPrefix')}{i}" for i in range(rows)}
    ids |= {f"{const('FilterIdPrefix')}{i}" for i in range(filters)}
    ids |= {f"{const(name)}{i}" for name in ("CtRowIdPrefix", "TRowIdPrefix") for i in range(team_rows)}
    variables = set(CS_VAR_CONST_RE.findall(source))
    variables |= {f"{const('TabVarPrefix')}{i}" for i in range(tabs)}
    variables |= {f"{const('TabCountVarPrefix')}{i}" for i in range(tabs)}
    variables |= {f"{const('LangVarPrefix')}{i}" for i in range(langs)}
    variables |= {f"{const('FilterVarPrefix')}{i}" for i in range(filters)}
    variables |= {
        f"{const(name)}{i}{suffix}"
        for name in ("CtRowVarPrefix", "TRowVarPrefix")
        for i in range(team_rows)
        for suffix in ("_a", "_c")
    }
    variables |= {f"{const('RowVarPrefix')}{i}{suffix}" for i in range(rows) for suffix in ("_i", "_a", "_b", "_c")}
    return ids, variables


def scan_layout(path: Path) -> tuple[set[str], set[str]]:
    """Panel ids and dialog vars of one layout; enforces text, hittest and button rules."""
    text = path.read_text(encoding="utf-8")
    if HIDDEN_CLASS.search(text):
        fail(f"{path.name}: forbidden class name hidden/Hidden")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as ex:
        fail(f"{path.name}: XML parse error: {ex}")

    ids: set[str] = set()
    variables: set[str] = set()
    for element in root.iter():
        panel_id = element.attrib.get("id")
        if panel_id:
            if panel_id in ids:
                fail(f"{path.name}: duplicate id {panel_id!r}")
            ids.add(panel_id)
        if element.tag in ("Panel", "Label") and element.attrib.get("hittest") != "false":
            fail(f"{path.name}: <{element.tag} class={element.attrib.get('class')!r}> needs hittest=\"false\"")
        if element.tag == "Button" and (not panel_id or "hittest" in element.attrib):
            fail(f"{path.name}: every <Button> needs an id and no hittest attribute")
        text_attr = element.attrib.get("text")
        if text_attr is None:
            continue
        value = text_attr.strip()
        if value in LITERAL_GLYPHS:
            continue
        if not DIALOG_ONLY_RE.fullmatch(value):
            fail(
                f"{path.name}: Label text must be a single dialog var "
                f"{{s:name}} or one of {sorted(LITERAL_GLYPHS)} (got {value!r})"
            )
        variables.update(VAR_RE.findall(value))
    if len(variables) > NAME_BUDGET:
        fail(f"{path.name}: {len(variables)} dialog vars exceed the budget of {NAME_BUDGET}")
    return ids, variables


def compare(label: str, xml_ids: set[str], xml_vars: set[str], cs_ids: set[str], cs_vars: set[str], exact_ids: bool) -> None:
    missing_panels = cs_ids - xml_ids
    if missing_panels:
        fail(f"{label}: C# panel ids missing from XML: {sorted(missing_panels)}")
    if exact_ids and xml_ids - cs_ids:
        fail(f"{label}: XML ids unknown to C#: {sorted(xml_ids - cs_ids)}")
    missing_vars = cs_vars - xml_vars
    if missing_vars:
        fail(f"{label}: C# dialog vars missing from XML: {sorted(missing_vars)}")
    unused_vars = xml_vars - cs_vars
    if unused_vars:
        fail(f"{label}: XML dialog vars unused in C#: {sorted(unused_vars)}")


def main() -> None:
    check_themes()

    xml_files = sorted(LAYOUTS.glob("*.xml"))
    known = {BANNER_XML, INFO_XML}
    unknown = [p.name for p in xml_files if p not in known]
    if unknown:
        fail(f"layouts without a contract check: {unknown}")
    for path in known:
        if not path.is_file():
            fail(f"missing layout {path}")

    banner_ids, banner_vars = scan_layout(BANNER_XML)
    cs_text = "\n".join(p.read_text(encoding="utf-8") for p in (BANNER_CS, CUSTOM_HUD_CS, REEL_CS))
    reel_ids, reel_vars = reel_contract()
    cs_panels = set(CS_PANEL_RE.findall(cs_text)) | reel_ids
    cs_vars = (
        set(CS_TEXT_VAR_RE.findall(cs_text))
        | set(CS_SET_TEXT_RE.findall(cs_text))
        | set(CS_VAR_CONST_RE.findall(cs_text))
        | reel_vars
    )
    compare(BANNER_XML.name, banner_ids, banner_vars, cs_panels, cs_vars, exact_ids=False)

    info_ids, info_vars = scan_layout(INFO_XML)
    cs_info_ids, cs_info_vars = info_contract()
    compare(INFO_XML.name, info_ids, info_vars, cs_info_ids, cs_info_vars, exact_ids=True)

    print(
        f"OK: {len(xml_files)} layout(s), {len(banner_ids) + len(info_ids)} panel id(s), "
        f"{len(banner_vars) + len(info_vars)} dialog var(s), {len(THEMES)} theme(s)"
    )


if __name__ == "__main__":
    main()
