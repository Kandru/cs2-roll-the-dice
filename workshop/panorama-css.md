# Panorama CSS (this HUD)

Web CSS is dropped with no log. Run `make panorama` (`tools/validate_panorama.py`) before calling a layout done.

**One stylesheet:** [`hud.vcss`](content/panorama/styles/custom_game/rollthedice/hud.vcss).

## Layout model

`flow-children: down|right|none`. Size with `width`/`height` (`px`, `%`, `fit-children`, `fill-parent-flow(n)`).

**Padding does not inset `width/height: 100%` children** (border box).

**Centered text:** Label needs explicit width + `text-align: center`. Prefer stock `horizontal-center` / `text-align-center`.

## Values that work

`background-color` hex / `#rrggbbaa` / `gradient(linear, …)` / `none` - **no `rgba()`/`rgb()`/`hsl()`**. `background-size: contains` (not web `contain`). `font-size` **unitless**. Hex colours only - no `@define` / `var()` / bareword tokens. `visibility: collapse` (no `display`). `overflow: noclip|clip|scroll`. HUD `z-index` ~`100` for this overlay.

## Shared / do not

Shared: `.ph-card` / `.ph-fading` / `.ph-off` · `.ph-bg` / `.ph-bar` · `Label.ph-title` · `.theme-*` (config `panorama_theme`). Hide with `ph-off` - never class name `hidden`/`Hidden`. Label `text` must be a dialog var (`{s:dice}`); wording lives in C# lang files.

Do not: `display`/flex/grid, `calc()`, `var()`, `@define`, `@media`, `::before`/`::after`, `background-size: contain`, `visibility: hidden`, `overflow: hidden`, unitful `font-size`, `vertical-align: center`.

## Preview

Browser mirror: [`preview/banner.html`](preview/banner.html) + [`preview/kit.css`](preview/kit.css). Keep class names in sync with `hud.vcss`.
