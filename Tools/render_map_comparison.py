#!/usr/bin/env python3
"""render_map_comparison.py — side-by-side map renders: EI vs ZL data.

Renders the same .map through the authoritative renderer (mapviewer,
rect layout = original Mir3.exe projection) with two different library
sources — the EI 3.0 client data (WIL theme folders) and the Zircon 2017
data (Map Data ZL libraries) — and stitches the two images into one
comparison PNG with labelled panels.

Motivation: the per-library frame counts differ between EI and ZL (see
docs/research/mir3-map-reconstruction/ei-vs-zl-libraries.json); maps whose
frames exceed the EI library (e.g. 3.map mid file 25 -> wood_smobjectsc, EI
969 frames vs frame_max 2531; 41.map -> sand_housesc/sand_smobjectsc) render
with sparse holes under EI data and complete under ZL data.  Note the ZL
panel's "dark" tiles are NOT missing frames — ZL's own frames are merely
darker/different artwork from EI's (see comparisons/README.md).  This tool
makes the difference visible per map.

Usage:
    python3 Tools/render_map_comparison.py <maps_dir> \
        --data-ei <EI client Data dir> --data-zl <ZL Data/Map Data dir> \
        --maps 3,0,D612 --z 4 --out docs/research/mir3-map-reconstruction/comparisons

Outputs one PNG per map: <out>/<stem>__ei_vs_zl_z<z>.png
"""

import argparse
import io
import os
import struct
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mapviewer  # MapCache, FramePool, render_full_map, LAYOUT_RECT


def cell_bytes(path: str) -> float:
    """Detect the cell-format generation: 14 B/cell (current EI) or
    13 B/cell (legacy maps without trailing attr bytes; not in the server
    minimap index, and parse_map / renderer assume 14)."""
    with open(path, "rb") as f:
        data = f.read()
    w = struct.unpack_from("<H", data, 22)[0]
    h = struct.unpack_from("<H", data, 24)[0]
    cell_off = 28 + (w // 2) * (h // 2) * 3
    rem = len(data) - cell_off
    return rem / (w * h) if w and h else 0.0


def render_rgba(map_cache, pool, stem: str, z: int) -> Image.Image:
    """Render a map to an RGB image via the authoritative path."""
    buf = mapviewer.render_full_map(map_cache, pool, stem + ".map", z,
                                    fmt="PNG", layout=mapviewer.LAYOUT_RECT)
    return Image.open(io.BytesIO(buf)).convert("RGB")


def main():
    ap = argparse.ArgumentParser(description="Side-by-side EI vs ZL map renders")
    ap.add_argument("maps_dir")
    ap.add_argument("--data-ei", required=True, help="EI client Data dir (WIL theme folders)")
    ap.add_argument("--data-zl", required=True, help="ZL Data/Map Data dir (ZL libraries)")
    ap.add_argument("--maps", default="3,0,D612",
                    help="comma-separated map stems to render (default 3,0,D612)")
    ap.add_argument("--z", type=int, default=4, help="zoom (scale = 1<<z)")
    ap.add_argument("--out", default="docs/research/mir3-map-reconstruction/comparisons",
                    help="output directory")
    ap.add_argument("--ground-only", action="store_true",
                    help="render only the ground layer (debug: isolate back layer)")
    ap.add_argument("--objects-only", action="store_true",
                    help="render only mid/front object layers")
    args = ap.parse_args()

    stems = [s.strip() for s in args.maps.split(",") if s.strip()]
    os.makedirs(args.out, exist_ok=True)

    map_cache = mapviewer.MapCache(args.maps_dir)
    pool_ei = mapviewer.FramePool(args.data_ei)
    pool_zl = mapviewer.FramePool(args.data_zl)

    draw_ground = not args.objects_only
    draw_objects = not args.ground_only
    tag = []
    if args.ground_only:
        tag.append("ground")
    if args.objects_only:
        tag.append("objects")
    tag = ("_" + "_".join(tag)) if tag else ""

    for stem in stems:
        path = os.path.join(args.maps_dir, stem + ".map")
        if not os.path.exists(path):
            print(f"[!] missing {stem}.map — skipped", file=sys.stderr)
            continue
        cb = cell_bytes(path)
        if abs(cb - 14.0) > 0.01:
            print(f"[!] {stem}.map is legacy {cb:.1f} B/cell format — "
                  f"not renderable by parse_map (14 B/cell); skipped", file=sys.stderr)
            continue
        print(f"[*] rendering {stem} (z{args.z}) ...")
        img_ei = render_rgba(map_cache, pool_ei, stem, args.z)
        img_zl = render_rgba(map_cache, pool_zl, stem, args.z)
        w, h = img_ei.size
        assert img_zl.size == img_ei.size, "panels must match"

        label_h = 34
        canvas = Image.new("RGB", (w * 2, h + label_h), (20, 20, 24))
        canvas.paste(img_ei, (0, label_h))
        canvas.paste(img_zl, (w, label_h))
        d = ImageDraw.Draw(canvas)
        for i, (txt, color) in enumerate((
                ("EI 3.0 client (WIL theme libs)", (120, 200, 255)),
                ("ZL 2017 (Map Data ZL libs)", (255, 200, 120)))):
            d.text((12 + i * w, 10), txt, fill=color)
        # vertical divider
        d.line((w - 1, label_h, w - 1, label_h + h), fill=(90, 90, 90))
        out_path = os.path.join(args.out, f"{stem}__ei_vs_zl_z{args.z}{tag}.png")
        canvas.save(out_path)
        print(f"    -> {out_path} ({canvas.size[0]}x{canvas.size[1]})")

    print(f"done — {len(stems)} map(s) in {args.out}")


if __name__ == "__main__":
    main()
