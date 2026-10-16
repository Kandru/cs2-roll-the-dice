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
KIT_CSS = PREVIEW / "kit.css"
BANNER_HTML = PREVIEW / "banner.html"
REEL_HTML = PREVIEW / "reel.html"
BANNER_CS = ROOT / "src" / "Hud" / "Banner.cs"
CUSTOM_HUD_CS = ROOT / "src" / "Hud" / "CustomHud.cs"
REEL_CS = ROOT / "src" / "Hud" / "Reel.cs"
THEME_CS = ROOT / "src" / "Hud" / "HudTheme.cs"
GEN_REEL = ROOT / "tools" / "gen_reel_panorama.py"

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

    css = HUD_VCSS.read_text(encoding="utf-8")
    if HIDDEN_CLASS.search(css):
        fail("hud.vcss uses forbidden class name hidden/Hidden")
    bad = BAD_CSS.search(css)
    if bad:
        fail(f"hud.vcss has unsupported CSS near: {bad.group(0)!r}")

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


def load_generator():
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location("gen_reel_panorama", GEN_REEL)
    if spec is None or spec.loader is None:
        fail(f"cannot load {GEN_REEL}")
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
    for path, text in gen.render().items():
        if path.read_text(encoding="utf-8") != text:
            fail(f"{path.name}: generated reel block out of date - run tools/gen_reel_panorama.py")

    ids = {f"{row_prefix}{i}" for i in range(rows) if i != win}
    variables = {f"{var_prefix}{i}" for i in range(rows) if i != win}
    return ids, variables


def main() -> None:
    check_themes()

    xml_files = sorted(LAYOUTS.glob("*.xml"))
    if not xml_files:
        fail(f"no layouts in {LAYOUTS}")

    xml_ids: set[str] = set()
    xml_vars: set[str] = set()
    for path in xml_files:
        text = path.read_text(encoding="utf-8")
        if HIDDEN_CLASS.search(text):
            fail(f"{path.name}: forbidden class name hidden/Hidden")
        try:
            root = ET.fromstring(text)
        except ET.ParseError as ex:
            fail(f"{path.name}: XML parse error: {ex}")

        for element in root.iter():
            panel_id = element.attrib.get("id")
            if panel_id:
                xml_ids.add(panel_id)
            text_attr = element.attrib.get("text")
            if text_attr is None:
                continue
            value = text_attr.strip()
            if not DIALOG_ONLY_RE.fullmatch(value):
                fail(
                    f"{path.name}: Label text must be a single dialog var "
                    f"{{s:name}} (got {value!r})"
                )
            xml_vars.update(VAR_RE.findall(value))

    cs_text = "\n".join(p.read_text(encoding="utf-8") for p in (BANNER_CS, CUSTOM_HUD_CS, REEL_CS))
    reel_ids, reel_vars = reel_contract()
    cs_panels = set(CS_PANEL_RE.findall(cs_text)) | reel_ids
    cs_vars = (
        set(CS_TEXT_VAR_RE.findall(cs_text))
        | set(CS_SET_TEXT_RE.findall(cs_text))
        | set(CS_VAR_CONST_RE.findall(cs_text))
        | reel_vars
    )

    missing_panels = cs_panels - xml_ids
    if missing_panels:
        fail(f"C# panel ids missing from XML: {sorted(missing_panels)}")
    missing_vars = cs_vars - xml_vars
    if missing_vars:
        fail(f"C# dialog vars missing from XML: {sorted(missing_vars)}")
    unused_vars = xml_vars - cs_vars
    if unused_vars:
        fail(f"XML dialog vars unused in C#: {sorted(unused_vars)}")

    print(
        f"OK: {len(xml_files)} layout(s), {len(xml_ids)} panel id(s), "
        f"{len(xml_vars)} dialog var(s), {len(THEMES)} theme(s)"
    )


if __name__ == "__main__":
    main()
