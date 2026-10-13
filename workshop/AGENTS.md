# Roll The Dice HUD (AI)

C# under [`src/Hud/`](../src/Hud/): [`CustomHud.cs`](../src/Hud/CustomHud.cs) (spawn, transmit, `SetText` / `SetHasClass`, fade `ph-fading` → `ph-off`), [`Banner.cs`](../src/Hud/Banner.cs), [`HudTheme.cs`](../src/Hud/HudTheme.cs) (config `panorama_theme` → one `theme-*` on the card). One stylesheet: [`hud.vcss`](content/panorama/styles/custom_game/rollthedice/hud.vcss) (see [panorama-css.md](panorama-css.md)). Banner copy comes only from C# `IStringLocalizer` into `{s:dice}` — no hardcoded Label text in XML.

`workshop/content` is the addon root (copy into `csgo_addons/<addon>/`). Layouts load as `panorama/layout/custom_game/rollthedice/<name>.vxml_c`.

## Panorama versions

Repo sources stay `banner.xml`, `hud.vcss`.

## Server can only

1. Dialog var on the layout root card → Label text (never path/colour).
2. `SetHasClass` on a panel **id**.

No panel create / width / colour / image from C#. Layout edits need VPK republish.

**Round / notify:** `Banner.Notify` (center + panorama) and `Banner.Show` (panorama status); config `notify_player_via_centermsg` / `notify_player_via_panorama` via `BindConfig`. `Suspend()` clears owner/shown text + `CustomHud.ResetRound` (round start/end, intermission). `Shutdown()` clears state only (map end/unload). Never Kill/respawn live layouts mid-round.

**Spectator mirror:** per-owner text (incl. bots; only humans get `Present`). Resolve watch target by `ObserverTarget` handle → `PlayerPawn` (`ObserverPawn` then `PlayerPawn`). Alive non-observers see their own text. `OnTick` + `Show` sync. Death forces `Banner.Hide`.

**Fade:** show = `ph-fading` then clear next frame; hide = `ph-fading` 1s then `ph-off`.

## Layouts

| Layout | Root | Routed ids |
| --- | --- | --- |
| `banner.xml` | `Banner` | `Banner` (`{s:dice}`) |

`make panorama` validates ids/`{s:…}` against C#.

## Preview

`make preview` serves [`preview/banner.html`](preview/banner.html) (uses [`preview/kit.css`](preview/kit.css)). Interactive controls mirror show / fade / hide / status text / theme. Browser approximation only - game Panorama is authoritative.
