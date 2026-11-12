# MPV Stream Deck Icon Style Guide

Instructions for humans and AI tools creating **additional icons** that match the existing MPV Stream Deck plugin icon set. Follow this guide exactly. When in doubt, copy the geometry of an existing icon in `key/` rather than inventing new values.

A ready-to-use helper script is in `tools/icon_toolkit.py`. It encodes the tile, palette, chevrons and export sizes described here.

---

## 1. Style summary

- Flat, vector, geometric. No gradients inside glyphs, no shadows, no outlines-on-outlines, no photo or texture.
- A dark purple rounded-square **tile** with a subtle border, holding a **white + lavender glyph**.
- **White = the object** (speaker, subtitle box, list rows). **Lavender accent = state, highlight, or direction** (chevrons, the current item, progress).
- Rounded everything: round line caps, round joins, rounded rect ends.
- **No text, letters or numbers** in icons. Users label keys with Stream Deck's title feature.
- Do not imitate the official mpv logo or any other trademarked mark.

## 2. Palette (use only these)

| Role | Value |
|---|---|
| Accent (lavender) | `#d49bff` |
| Primary glyph | `#ffffff` |
| Disabled / "off" state | `#8a8a94` |
| Dimmed track (inactive part of a bar/ring) | white at `fill-opacity .35`, or accent at `stroke-opacity .3-.35` |
| Tile gradient | linear, `x1=0 y1=0 x2=1 y2=1`, from `#3a1d4d` to `#14101a` |
| Tile border | `#8e4fb3`, `stroke-opacity .5` |
| **Off-state tile** (key/action icons only) | gradient `#2f2f35` to `#151518`, border `#8a8a94` at `stroke-opacity .45` (neutral grey, no purple anywhere) |

## 3. Canvas and tile (key icons)

- `viewBox="0 0 144 144"`, `width="144" height="144"`.
- Tile: `<rect width="144" height="144" rx="26" fill="url(#bg)"/>` with gradient id `bg`.
- Border: `<rect x="1.5" y="1.5" width="141" height="141" rx="24.5" fill="none" stroke="#8e4fb3" stroke-opacity=".5" stroke-width="3"/>`
- Plugin icon uses the same tile at 256 units: `rx="56"`, border `x=3 y=3 width=250 height=250 rx=53 stroke-width=5`.

## 4. Glyph geometry rules (144 canvas)

- **Safe area:** glyph content stays within roughly x 26-118. Leave at least ~20 units of tile margin.
- **Glyphs with a direction chevron** (next/prev actions): main glyph occupies about y 28-90; the chevron pair sits at y 103-123, centered on x=72.
- **Glyphs without a chevron** (states, toggles): center the glyph vertically on y=72 (use a `translate`).
- **Stroke widths:** outlines 6; chevrons 7; thin accent lines 4. Filled shapes that need rounded corners get a same-colour stroke of 5-10 with `stroke-linejoin="round"`.
- **Bars/rows:** height 7-9 (rows) or 12-16 (progress bars), `rx` = half the height.
- **Chevron pair** (always accent, `stroke-width 7`, round caps/joins, `fill="none"`):
  - next `»`: `M58 103 L68 113 L58 123 M76 103 L86 113 L76 123`
  - prev `«`: `M86 103 L76 113 L86 123 M68 103 L58 113 L68 123`
- **Direction convention:** `»` = next / forward, `«` = previous / back. Never use single-line arrows or triangles for direction.
- **Highlight convention:** in lists or segmented bars, exactly one item is accent and the rest are white. The accent item is the one in the direction of travel: the **last** item (right/bottom) for next, the **first** item (left/top) for previous. Chapter icons add a bookmark above the accent segment. Playlist icons use three rows and follow the same rule. Never highlight the middle item of a next/prev icon.

## 5. Existing icon set (reference)

| File | Meaning | Main glyph | Accent usage |
|---|---|---|---|
| `01_next_audio_track` | Next audio track | Solid speaker + two wave arcs | waves, `»` |
| `02_next_subtitle` | Next subtitle | Rounded caption box (white outline) with two caption lines | lines, `»` |
| `03_next_chapter` | Next chapter | Three-segment timeline + bookmark | highlighted right segment, bookmark, `»` |
| `04_prev_chapter` | Previous chapter | Same, highlight on left | left segment, bookmark, `«` |
| `05_next_playlist_entry` | Next playlist entry | Three bulleted list rows | last row, `»` |
| `06_prev_playlist_entry` | Previous playlist entry | Same | first row, `«` |
| `07_play_pause` | Play/pause | White play triangle + accent pause bars | pause bars |
| `08a_acompressor_on` | Compressor on | Waveform clamped between dashed lines + inward arrows | dashed lines, arrows |
| `08b_acompressor_off` | Compressor off | Identical to the on icon (same clamped waveform, limit lines, arrows) | all `#8a8a94`, plus slash |
| `09_seek` | Seek | Progress bar + round knob + `«  »` | filled progress, chevrons |
| `10_custom_command` | Run a custom mpv command/script | Terminal window (white outline + title line) with prompt `>` and cursor bar | prompt chevron |
| `11a_volume` / `11b_volume_muted` (+ `_dial` versions) | Dial volume control / muted state | Bold speaker + two wave arcs; muted = everything `#8a8a94` plus slash `stroke-width 8` | waves |
| `12_playback_speed_dial` (dial only) | Dial playback speed control | Speedometer: 240° arc (dimmed white track), five white tick marks, white tapered needle on a white hub | filled part of the arc up to the needle, hub ring |

Toggle actions: provide an **on** icon (normal palette) and an **off** icon. The off icon is the on icon with identical geometry (do not alter shapes, heights or indicator elements), recoloured entirely to `#8a8a94`, with a diagonal slash from lower-left to upper-right (`M30 112 L114 32`, `stroke-width 8`, round caps) drawn on top. Nothing in the glyph may be rearranged or enlarged so that it collides with other elements. The same slash is used for the muted volume icon.

## 6. Variants and when to use each

| Variant | Where | Background | Colours | Notes |
|---|---|---|---|---|
| **Key icon** | Action state `Image` | Tile | Full palette | Primary design |
| **Action-list icon** | Action `Icon` | Tile (same as key) | Full palette | Same SVG, `width/height 40` |
| **Dial icon** | Encoder/touch-strip state `Image` | **Transparent** | Accent + white, no tile | Bolder strokes, simple shapes |
| **Plugin icon** | Manifest `Icon` | Tile, 256 units | Full palette | Play triangle in a progress ring with knob |
| **Category icon** | Manifest `CategoryIcon` | **Transparent** | Accent only | Thicker ring (stroke 22), no white (must be visible on light and dark) |
| **.ico (exe)** | Windows executable | Tile | Full palette | 16/24/32 px use a simplified, bolder variant |

Do not use white-on-transparent for anything except the dial icon: it is invisible on light backgrounds.

## 7. Export sizes and file layout

| Asset | Format | Folder / filename |
|---|---|---|
| Key icon | **SVG only** | `key/NN_name.svg` |
| Action-list icon | **SVG only** | `action/NN_name.svg` (same tile, root `width/height 40`) |
| Dial icon | **SVG only** | `dial/NN_name_dial.svg` (transparent) |
| Category icon | **SVG only** | `plugin/svg/mpv-category.svg` |
| Plugin icon | **SVG + PNG 256x256 + PNG @2x 512x512** | `plugin/svg/mpv.svg` (source), `plugin/png/mpv.png`, `plugin/png/mpv@2x.png` |
| Windows icon | ICO, sizes 16, 24, 32, 48, 64, 128, 256 | `plugin/mpv.ico` (PNG-compressed entries) |

**Do not generate PNGs for key, action-list, dial or category icons.** Stream Deck supports SVG natively for these, so SVG is the only deliverable. The plugin icon (manifest `Icon`) is the exception because it must be a PNG. Keep `plugin/svg/mpv.svg` as the source and render both PNGs from it (256x256 and 512x512 for `@2x`); verify the pixel dimensions after rendering (the toolkit's `export_plugin_icon` asserts them). PNG previews and the `.ico` are generated separately and are not part of the manifest.

- **Naming:** `NN_verb_object` in snake_case with a two-digit sequence number (`13_...` for the next new icon). Use `a`/`b` suffixes for states (`08a_..._on`, `08b_..._off`). Dial versions end in `_dial`.
- **Rasterizing** (only for the plugin icon, the `.ico` and preview images): use `cairosvg` (`cairosvg.svg2png(bytestring=..., output_width=N, output_height=N)`), always rendered from the SVG at the target size; never upscale a smaller PNG.

## 8. Manifest rules

- **File extensions are not required** in manifest image paths: Stream Deck resolves `.svg`/`.png` (and `@2x`) from the base name. Use extension-less paths, as in the original manifest (`Images/key/03_next_chapter`). Because paths have no extension, **never put an SVG and a PNG with the same base name in the same folder** (Stream Deck cannot choose between `mpv.svg` and `mpv.png`). Keep them in separate `svg/` and `png/` subfolders, as the `plugin/` folder does.
- Plugin-level `Icon` must resolve to a PNG: `Images/plugin/png/mpv` -> `mpv.png` (256x256) with `mpv@2x.png` (512x512) beside it. All other images (key, action-list, dial, category) are **SVG**.
- Folder layout inside the plugin: `Images/key/`, `Images/action/`, `Images/dial/` (SVG files), plus `Images/plugin/png/` (`mpv.png`, `mpv@2x.png`) and `Images/plugin/svg/` (`mpv.svg` source, `mpv-category.svg`). Key/action/dial folders hold SVG only, so they need no `svg/`/`png/` split.
- Per action: `Icon` -> `Images/action/<name>`; each state `Image` -> `Images/key/<name>`. Dial/encoder actions use `Images/dial/<name>_dial`. `CategoryIcon` -> `Images/plugin/svg/mpv-category`; plugin `Icon` -> `Images/plugin/png/mpv`.

## 9. Pitfalls already encountered

1. **Resizing the root `<svg>` tag:** use `.replace('width="144" height="144"', 'width="40" height="40"', 1)` with count `1`. Without it, the tile `<rect width="144" height="144">` is resized too and the tile shrinks into a corner.
2. **White-on-transparent action icons** looked blank in light viewers. Use the tile version.
3. **"File not found" validator errors** (including for the original `Images/mpv`) were a sign the `Images` folder wasn't where the validator looked, not a sign that extensions are required. Extension-less paths are valid; check that the files exist at the expected location first.
4. **Small sizes (16-32 px):** the thin plugin ring turns to mush. Use the bolder simplified variant (ring stroke 24, knob 24, triangle stroke 14, scaled to 0.74 inside a tile with `rx=52`).
5. **Unnecessary PNGs:** keys, dials, action-list and category icons are SVG only; do not generate PNG or `@2x` files for them.

## 10. Process for creating a new icon

1. **Choose the metaphor:** one recognisable object for the concept (e.g. volume = speaker, speed = gauge). Reuse an existing object if the concept is a variant (next/prev, on/off).
2. **Decide the roles:** object in white; the changing part (state, current item, direction) in accent.
3. **Add direction/state markers** using the standard chevrons, highlight convention or off-state grey. Do not invent new marker styles.
4. **Draw on the 144 canvas** inside the safe area, using only the stroke widths and radii above. Keep to 3 or fewer distinct shapes plus chevrons.
5. **Wrap in the tile** (section 3) and export all variants needed (section 6/7). Use `tools/icon_toolkit.py`.
6. **Verify visually:** render on both a white and a dark background and at 20 px. Every part must remain visible and distinguishable.
7. **Update** the manifest paths.

## 11. Acceptance checklist

- [ ] Uses only palette colours; no extra gradients, shadows or text
- [ ] Round caps/joins; stroke widths from section 4
- [ ] Within safe area; chevron zone respected (or glyph centred if no chevron)
- [ ] `»` / `«` direction convention followed; highlighted item in accent per the highlight convention
- [ ] Off/disabled states are the on icon in `#8a8a94` with the standard slash, on the grey off-state tile (no purple visible)
- [ ] Legible at 20 px and on white and dark backgrounds
- [ ] SVG exported for every needed variant (key, action-list, dial, category); **no PNGs** for those
- [ ] Plugin icon only: SVG source + 256x256 PNG + 512x512 `@2x` PNG, dimensions verified
- [ ] Manifest paths are extension-less base names matching files that exist in `Images/` (plugin `Icon` resolves to the PNG pair)
- [ ] No trademarked logos or characters

## 12. Prompt template for an AI assistant

> Create a new Stream Deck icon for the MPV plugin: **\<action description\>**. Follow `docs/ICON_STYLE_GUIDE.md` exactly. Use `tools/icon_toolkit.py` to build the SVG on the standard 144x144 purple tile (white glyph, lavender `#d49bff` accents, no text). Use `»`/`«` chevrons if the action has a direction. Export SVG only: key, action-list and, if it is a dial action, a transparent dial version (no PNGs). Name it `NN_verb_object`. Show a preview on white and dark backgrounds and list the manifest lines to add.
