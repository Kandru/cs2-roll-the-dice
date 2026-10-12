# Roll The Dice HUD (AI)

C# under [`src/Hud/`](../src/Hud/): [`CustomHud.cs`](../src/Hud/CustomHud.cs) (spawn, transmit, `SetText` / `SetHasClass`, fade `ph-fading` → `ph-off`), [`Banner.cs`](../src/Hud/Banner.cs). One stylesheet: [`hud.vcss`](content/panorama/styles/custom_game/rollthedice/hud.vcss) (see [panorama-css.md](panorama-css.md)).

`workshop/content` is the addon root (copy into `csgo_addons/<addon>/`). Layouts load as `panorama/layout/custom_game/rollthedice/<name>.vxml_c`.

## Panorama versions

Repo sources stay `banner.xml`, `hud.vcss` - never add `vN` to those names here. `PanoramaVersion` in [`Version.cs`](../src/Version.cs) selects `banner_vN` at runtime. On workshop copy, rename layouts and the stylesheet include by hand. Bump only when panel ids / dialog vars / classes / structure would break old DLLs.

## Server can only

1. Dialog var on the layout root card → Label text (never path/colour).
2. `SetHasClass` on a panel **id**.

No panel create / width / colour / image from C#. Layout edits need VPK republish.

**Round restart:** layouts survive rounds; client resets XML defaults while server keeps last classes/vars. `CustomHud.ResetRound()` → `Banner.WriteDefaults`. Never Kill/respawn live layouts; `Shutdown` only on map end/unload.

**Fade:** show = `ph-fading` then clear next frame; hide = `ph-fading` 1s then `ph-off`.

## Layouts

| Layout | Root | Routed ids |
| --- | --- | --- |
| `banner.xml` | `Banner` | `Banner` (`{s:dice}`) |

`make panorama` validates ids/`{s:…}` against C#.
