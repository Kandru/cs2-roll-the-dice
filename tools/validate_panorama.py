#!/usr/bin/env python3
"""Validate the Roll The Dice Panorama addon (workshop/content/panorama)."""
from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / "workshop" / "content" / "panorama"
LAYOUTS = CONTENT / "layout" / "custom_game" / "rollthedice"
STYLES = CONTENT / "styles" / "custom_game" / "rollthedice"
HUD_VCSS = STYLES / "hud.vcss"
BANNER_CS = ROOT / "src" / "Hud" / "Banner.cs"
CUSTOM_HUD_CS = ROOT / "src" / "Hud" / "CustomHud.cs"

VAR_RE = re.compile(r"\{s:([A-Za-z0-9_]+)\}")
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


def fail(msg: str) -> None:
    print(f"ERROR: {msg}", file=sys.stderr)
    raise SystemExit(1)


def main() -> None:
    if not HUD_VCSS.is_file():
        fail(f"missing stylesheet {HUD_VCSS}")
    css = HUD_VCSS.read_text(encoding="utf-8")
    if HIDDEN_CLASS.search(css):
        fail("hud.vcss uses forbidden class name hidden/Hidden")
    bad = BAD_CSS.search(css)
    if bad:
        fail(f"hud.vcss has unsupported CSS near: {bad.group(0)!r}")

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
            ET.fromstring(text)
        except ET.ParseError as ex:
            fail(f"{path.name}: XML parse error: {ex}")
        xml_ids.update(PANEL_ID_RE.findall(text))
        xml_vars.update(VAR_RE.findall(text))

    cs_text = BANNER_CS.read_text(encoding="utf-8") + "\n" + CUSTOM_HUD_CS.read_text(encoding="utf-8")
    cs_panels = set(CS_PANEL_RE.findall(cs_text))
    cs_vars = set(CS_TEXT_VAR_RE.findall(cs_text)) | set(CS_SET_TEXT_RE.findall(cs_text))

    missing_panels = cs_panels - xml_ids
    if missing_panels:
        fail(f"C# panel ids missing from XML: {sorted(missing_panels)}")
    missing_vars = cs_vars - xml_vars
    if missing_vars:
        fail(f"C# dialog vars missing from XML: {sorted(missing_vars)}")
    unused_vars = xml_vars - cs_vars
    if unused_vars:
        fail(f"XML dialog vars unused in C#: {sorted(unused_vars)}")

    print(f"OK: {len(xml_files)} layout(s), {len(xml_ids)} panel id(s), {len(xml_vars)} dialog var(s)")


if __name__ == "__main__":
    main()
