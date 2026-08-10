#!/usr/bin/env python3
"""lib_frame_stats.py — per-library frame-level usage stats + preview sheets.

Objective-sect-6 deliverable: for every WIL/ZL library used by the EI maps,
extract the most-used frames (top-10 by cell count), min/max frame, 3 random
frames, empty/black-frame counts, mean brightness, non-transparent pixel
ratio, and per-frame size + WIL offset — then render a preview contact sheet
(``previews/<lib>.png``) so the resource side of every map reference is
visually auditable.

Inputs:
  - catalog/lib-frames/lib-stats.json (frame_min/frame_max per lib)
  - catalog/maps/*.json             (per-map per-lib frame usage counts)

A fresh frame histogram is accumulated here by re-reading the .map files
(same parsing as build_map_catalog) so the histograms are exact counts of
referenced frames, not estimates.  Per-frame decoding stats (size, offsets,
alpha/black/mean) come from the WIL headers + sampled decode.

Usage:
    python3 Tools/maps/lib_frame_stats.py '<EI Map dir>' \
        --data '<EI Data dir>' \
        -o docs/research/mir3-map-reconstruction/catalog/lib-frames
"""
import argparse
import json
import os
import random
import sys
from collections import Counter

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mapviewer
from audit_mir3_maps import v_lookup, kr_lib_id, EMPTY_FRAME, NO_OBJECT_FILE


def parse_layers(path: str):
    """Return (ground Counter[(lid,frame)], mid Counter, front Counter)."""
    with open(path, "rb") as f:
        data = np.frombuffer(f.read(), dtype=np.uint8)
    if data.size < 28:
        return Counter(), Counter(), Counter()
    w = int(np.frombuffer(data[22:24], dtype="<u2")[0])
    h = int(np.frombuffer(data[24:26], dtype="<u2")[0])
    if not w or not h:
        return Counter(), Counter(), Counter()
    cell_off = 28 + (w // 2) * (h // 2) * 3
    cb = (data.size - cell_off) / (w * h)
    if cb not in (13.0, 14.0):
        return Counter(), Counter(), Counter()
    cb = int(cb)

    g = data[28:cell_off].reshape((w // 2) * (h // 2), 3)
    g_file = g[:, 0].copy()
    g_frame = g[:, 1:3].copy().view("<u2").reshape(-1)
    gq, gr, gv = v_lookup(g_file)
    g_kr = kr_lib_id(gq, gr, gv, g_file)
    g_draw = (gr <= 2) & (gv <= 69) & (g_kr >= 0)
    gcount = Counter((int(l), int(fr)) for l, fr in
                     zip(g_kr[g_draw], g_frame[g_draw]))

    cells = data[cell_off:cell_off + w * h * cb].reshape(w, h, cb)
    mid_file = cells[:, :, 4].copy()
    mid_frame = cells[:, :, 5:7].copy().view("<u2").reshape(w, h)
    front_file = cells[:, :, 3].copy()
    front_frame = cells[:, :, 7:9].copy().view("<u2").reshape(w, h)

    def obj_counter(file_arr, frame_arr):
        q, r, v = v_lookup(file_arr)
        lib_id = kr_lib_id(q, r, v, file_arr)
        skip = ((frame_arr == EMPTY_FRAME) | (file_arr == NO_OBJECT_FILE)
                | (r <= 2) | (v > 69) | (lib_id < 0))
        draw = ~skip
        return Counter((int(l), int(fr)) for l, fr in
                       zip(lib_id[draw], frame_arr[draw]))

    return gcount, obj_counter(mid_file, mid_frame), obj_counter(front_file, front_frame)


def frame_pixel_stats(pool: mapviewer.FramePool, lid: int, fr: int):
    """dict with ok/… — ok=False when the frame has no data (phantom index)."""
    lib = pool._get_lib(lid)
    if lib is None:
        return {"ok": False, "why": "lib unreadable"}
    img = lib.decode(fr) if fr < lib.count else None
    if img is None:
        return {"ok": False, "why": "frame has no data (phantom index)"}
    hdr = lib.header(fr)
    a = np.asarray(img.convert("RGBA"), dtype=np.int32)
    alpha = a[:, :, 3]
    used = alpha > 8
    n = int(np.count_nonzero(used))
    if n == 0:
        black_frac, mean = 1.0, 0.0
    else:
        rgb = a[used][:, :3]
        black_frac = float((rgb.sum(axis=1) < 30).mean())
        mean = float(rgb.mean())
    return {
        "ok": True,
        "w": img.width, "h": img.height,
        "off_x": int(hdr["offsetX"]) if hdr else None,
        "off_y": int(hdr["offsetY"]) if hdr else None,
        "alpha_frac": float(n / (img.width * img.height)),
        "black_frac": black_frac,
        "mean": round(mean, 1),
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("maps_dir")
    ap.add_argument("--data", required=True)
    ap.add_argument("-o", "--out", default=None, help="lib-frames output dir")
    args = ap.parse_args()
    out_dir = args.out or "lib-frames"
    prev_dir = os.path.join(out_dir, "previews")
    os.makedirs(prev_dir, exist_ok=True)

    pool = mapviewer.FramePool(args.data)

    names = sorted(f for f in os.listdir(args.maps_dir) if f.lower().endswith(".map"))
    hist = Counter()  # (lid, frame) -> cells
    for i, name in enumerate(names):
        g, m, f_ = parse_layers(os.path.join(args.maps_dir, name))
        for c in (g, m, f_):
            hist.update(c)
        if (i + 1) % 100 == 0:
            print(f"... hist {i + 1}/{len(names)}", file=sys.stderr)

    # per-lib aggregate
    by_lib: dict[int, dict] = {}
    for (lid, fr), cells in hist.items():
        e = by_lib.setdefault(lid, {"cells": 0, "frames": Counter()})
        e["cells"] += cells
        e["frames"][fr] += cells

    rng = random.Random(20260810)
    libs_out = []
    for lid in sorted(by_lib):
        e = by_lib[lid]
        name = mapviewer.KR_ORDER[lid] if lid < len(mapviewer.KR_ORDER) else f"lib{lid}"
        frames = e["frames"]
        cap = 0
        lib = pool._get_lib(lid)
        if lib is not None:
            cap = lib.count
        # 0xFF00-0xFFFE are reserved markers (0xFFFF empty sentinel excluded
        # upstream); they are not real frames and never decode.
        reserved = {fr: c for fr, c in frames.items() if 0xFF00 <= fr <= 0xFFFE}
        real = {fr: c for fr, c in frames.items() if fr < 0xFF00 and (cap == 0 or fr < cap)}
        if not real:
            libs_out.append({
                "lib_id": lid,
                "lib": name,
                "cells": e["cells"],
                "frames_used": len(frames),
                "reserved_frames": len(reserved),
                "real_frames_used": 0,
                "frame_min": None, "frame_max": None,
                "top": [], "sampled": [],
                "sample_black": 0, "sample_empty": 0,
                "note": "all references are reserved/OOB markers (0xFF00+); no decodable frames",
            })
            continue
        top = Counter(real).most_common(10)
        sample = [fr for fr, _ in top]
        fr_min = min(real)
        fr_max = max(real)
        for fr in (fr_min, fr_max):
            if fr not in sample:
                sample.append(fr)
        rnds = rng.sample(sorted(real), min(3, len(real)))
        for fr in rnds:
            if fr not in sample:
                sample.append(fr)
        sample.sort()

        stats = []
        for fr in sample:
            st = frame_pixel_stats(pool, lid, fr)
            st["frame"] = fr
            st["cells"] = real[fr]
            st["rank"] = sorted(real.keys(), key=lambda k: -real[k]).index(fr) + 1
            stats.append(st)
        black = sum(1 for s in stats if s.get("ok") and s["black_frac"] > 0.9)
        empty = sum(1 for s in stats if s.get("ok") and s["alpha_frac"] < 0.01)
        libs_out.append({
            "lib_id": lid,
            "lib": name,
            "cells": e["cells"],
            "frames_used": len(frames),
            "reserved_frames": len(reserved),
            "real_frames_used": len(real),
            "frame_min": fr_min,
            "frame_max": fr_max,
            "top": [(fr, c) for fr, c in top],
            "sampled": stats,
            "sample_black": black,
            "sample_empty": empty,
        })
    libs_out.sort(key=lambda r: -r["cells"])

    with open(os.path.join(out_dir, "lib-frame-stats.json"), "w") as f:
        json.dump({"count": len(libs_out), "libs": libs_out}, f, indent=1, ensure_ascii=False)

    # markdown summary
    lines = ["# Mir3 EI per-library frame-level statistics",
             "",
             f"libs: {len(libs_out)} · histogram from {len(names)} maps "
             "(every referenced frame counted across ground/mid/front)",
             "",
             "| # | lib | cells | frames_used | min | max | top10 | sampled_black | sampled_empty |",
             "|---|---|---|---|---|---|---|---|---|"]
    for r in libs_out:
        lines.append(f"| {r['lib_id']} | {r['lib']} | {r['cells']} | {r['frames_used']} | "
                     f"{r['frame_min']} | {r['frame_max']} | "
                     f"{len(r['top'])} | {r['sample_black']} | {r['sample_empty']} |")
    with open(os.path.join(out_dir, "lib-frame-stats.md"), "w") as f:
        f.write("\n".join(lines) + "\n")

    # preview contact sheets (top-10 + min + max + 3 random, up to 15 frames)
    from PIL import Image, ImageDraw
    n_made = 0
    for r in libs_out:
        if not r["sampled"]:
            continue
        lib = pool._get_lib(r["lib_id"])
        if lib is None:
            continue
        imgs = []
        for st in r["sampled"]:
            if not st.get("ok"):
                continue
            im = lib.decode(st["frame"])
            if im is not None:
                imgs.append((st["frame"], im))
        if not imgs:
            continue
        cell = 64
        cols = 5
        rows = (len(imgs) + cols - 1) // cols
        sheet = Image.new("RGBA", (cols * cell, rows * (cell + 18) + 4), (28, 28, 34, 255))
        d = ImageDraw.Draw(sheet)
        for k, (fr, im) in enumerate(imgs):
            cx = (k % cols) * cell
            cy = (k // cols) * (cell + 18)
            im2 = im.convert("RGBA")
            im2.thumbnail((cell - 4, cell - 4), Image.LANCZOS)
            sheet.alpha_composite(im2, (cx + (cell - im2.width) // 2, cy + 2))
            d.text((cx + 2, cy + cell + 2), str(fr), fill=(200, 220, 240))
        fn = os.path.join(prev_dir, f"{r['lib_id']:02d}_{r['lib']}.png")
        sheet.convert("RGB").save(fn, optimize=True)
        n_made += 1
    print(f"histogram: {len(hist)} (lib,frame) pairs across {len(libs_out)} libs")
    print(f"preview sheets: {n_made} -> {prev_dir}")
    print(f"stats -> {out_dir}/lib-frame-stats.json/.md")


if __name__ == "__main__":
    main()
