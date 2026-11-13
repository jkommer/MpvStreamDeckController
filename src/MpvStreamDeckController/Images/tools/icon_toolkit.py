"""Helpers for creating icons in the MPV Stream Deck icon style.
See docs/ICON_STYLE_GUIDE.md.   Requires: pip install cairosvg
Usage: python icon_toolkit.py   (builds an example icon into ./out)
"""
import os, cairosvg

ACCENT, WHITE, GREY = "#d49bff", "#ffffff", "#8a8a94"

def chevrons(direction, color=ACCENT):
    """Standard direction marker. direction: 'next' (») or 'prev' («)."""
    d = ("M58 103 L68 113 L58 123 M76 103 L86 113 L76 123" if direction == "next"
         else "M86 103 L76 113 L86 123 M68 103 L58 113 L68 123")
    return (f'<path d="{d}" fill="none" stroke="{color}" stroke-width="7" '
            f'stroke-linecap="round" stroke-linejoin="round"/>')

def key_svg(inner, size=144, off=False):
    """Wrap glyph markup (144x144 coordinates) in the standard purple tile.
    off=True uses the neutral grey tile reserved for off/muted states."""
    c0, c1, bc, bo = ("#2f2f35", "#151518", GREY, ".45") if off else ("#3a1d4d", "#14101a", "#8e4fb3", ".5")
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 144 144" width="{size}" height="{size}">'
            '<defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">'
            f'<stop offset="0" stop-color="{c0}"/><stop offset="1" stop-color="{c1}"/></linearGradient></defs>'
            '<rect width="144" height="144" rx="26" fill="url(#bg)"/>'
            '<rect x="1.5" y="1.5" width="141" height="141" rx="24.5" fill="none" '
            f'stroke="{bc}" stroke-opacity="{bo}" stroke-width="3"/>'
            f'{inner}</svg>')

def dial_svg(inner):
    """Transparent version for touch-strip/dial icons (no tile)."""
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 144 144" width="144" height="144">{inner}</svg>'

def export(name, inner, out="out", dial=False, off=False):
    """Write SVG files only (keys and dials natively support SVG): key/NAME.svg, or if dial=True
    dial/NAME_dial.svg (transparent). The manifest reuses the same file as the action-list icon."""
    sub = "dial" if dial else "key"
    os.makedirs(f"{out}/{sub}", exist_ok=True)
    if dial:
        open(f"{out}/dial/{name}_dial.svg", "w").write(dial_svg(inner))
    else:
        open(f"{out}/key/{name}.svg", "w").write(key_svg(inner, off=off))

def export_plugin_icon(svg_text, out="out", name="mpv"):
    """Plugin icon (manifest 'Icon') does not support SVG, so it needs PNGs.
    Writes plugin/svg/NAME.svg (source), plugin/png/NAME.png (256x256) and plugin/png/NAME@2x.png (512x512)
    and checks the sizes. SVG and PNG go in separate subfolders because manifest paths have no extension
    and a shared base name (mpv.svg / mpv.png) would be ambiguous."""
    import struct
    os.makedirs(f"{out}/plugin/svg", exist_ok=True); os.makedirs(f"{out}/plugin/png", exist_ok=True)
    open(f"{out}/plugin/svg/{name}.svg", "w").write(svg_text)
    for size, fn in ((256, f"{name}.png"), (512, f"{name}@2x.png")):
        path = f"{out}/plugin/png/{fn}"
        cairosvg.svg2png(bytestring=svg_text.encode(), write_to=path, output_width=size, output_height=size)
        with open(path, "rb") as fh:
            w, h = struct.unpack(">II", fh.read(24)[16:24])
        assert (w, h) == (size, size), f"{fn} is {w}x{h}, expected {size}x{size}"

def off_state(inner_builder):
    """Build an 'off' icon: call your glyph builder with GREY colours, then add the slash."""
    return inner_builder(GREY, GREY) + f'<path d="M30 112 L114 32" stroke="{GREY}" stroke-width="8" stroke-linecap="round"/>'

# --- example: "10_volume_up" speaker with a » marker -------------------------
def speaker(accent=ACCENT, white=WHITE):
    return (f'<polygon points="30,48 48,48 68,31 68,87 48,70 30,70" fill="{white}" stroke="{white}" '
            f'stroke-width="5" stroke-linejoin="round"/>'
            f'<path d="M80 46 Q92 59 80 72" fill="none" stroke="{accent}" stroke-width="6" stroke-linecap="round"/>'
            f'<path d="M90 35 Q112 59 90 83" fill="none" stroke="{accent}" stroke-width="6" stroke-linecap="round"/>')

if __name__ == "__main__":
    export("10_example_speaker_next", speaker() + chevrons("next"))
    print("wrote ./out (SVG only)")
