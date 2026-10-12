# Roll The Dice HUD (AI)

C# under [`src/Hud/`](../src/Hud/): [`CustomHud.cs`](../src/Hud/CustomHud.cs) (spawn, transmit, `SetText` / `SetHasClass`, fade `ph-fading` → `ph-off`), [`Banner.cs`](../src/Hud/Banner.cs), [`HudTheme.cs`](../src/Hud/HudTheme.cs) (config `theme` → one `theme-*` on the card). One stylesheet: [`hud.vcss`](content/panorama/styles/custom_game/rollthedice/hud.vcss) (see [panorama-css.md](panorama-css.md)). Banner copy comes only from C# `IStringLocalizer` into `{s:dice}` — no hardcoded Label text in XML.

`workshop/content` is the addon root (copy into `csgo_addons/<addon>/`). Layouts load as `panorama/layout/custom_game/rollthedice/<name>.vxml_c`.

## Panorama versions

Repo sources stay `banner.xml`, `hud.vcss` - never add `vN` to those names here. `PanoramaVersion` in [`Version.cs`](../src/Version.cs) selects `banner_vN` at runtime. On workshop copy, rename layouts and the stylesheet include by hand. Bump only when panel ids / dialog vars / classes / structure would break old DLLs.

## Server can only

1. Dialog var on the layout root card → Label text (never path/colour).
2. `SetHasClass` on a panel **id**.

No panel create / width / colour / image from C#. Layout edits need VPK republish.

**Round restart / suspend:** layouts survive rounds; client resets XML defaults while server keeps last classes/vars. `Banner.Suspend()` clears owner/shown text and runs `CustomHud.ResetRound()` → `Banner.WriteDefaults` on round start, round end, and `cs_intermission`. `Banner.Resume()` only when the round is active (warmup with RTD disabled stays suspended). `Banner.Shutdown()` clears state without rewriting clients (map end / unload). Never Kill/respawn live layouts; `CustomHud.Shutdown` only on map end/unload.

**Spectator mirror:** `Banner` stores per-owner `{s:dice}` text. Alive viewers see their own text; dead/spectating viewers get the text of `ObserverPawn.ObserverServices.ObserverTarget` (`OBS_MODE_IN_EYE` / `CHASE` / `FIXED`). `OnTick` + immediate sync on `Show` keep spectate switches up to date. Death forces `Banner.Hide` for that owner even when a dice skips `base.Remove`.

**Fade:** show = `ph-fading` then clear next frame; hide = `ph-fading` 1s then `ph-off`.

## Layouts

| Layout | Root | Routed ids |
| --- | --- | --- |
| `banner.xml` | `Banner` | `Banner` (`{s:dice}`) |

`make panorama` validates ids/`{s:…}` against C#.

## Preview

`make preview` serves [`preview/banner.html`](preview/banner.html) (uses [`preview/kit.css`](preview/kit.css)). Interactive controls mirror show / fade / hide / status text / theme. Browser approximation only - game Panorama is authoritative.
