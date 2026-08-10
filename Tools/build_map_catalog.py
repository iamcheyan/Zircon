#!/usr/bin/env python3
"""build_map_catalog.py — per-map JSON catalog + summary for reconstruction.

For every .map in the EI maps dir, emit `maps/<stem>.json` with:

  - header metadata (theme, w, h, size, bytes/cell, legacy 13B flag),
  - MiniMap.txt server-side display name (``display`` key, absent if the
    map is not indexed by the server),
  - per-layer library usage (ground / mid / front): for each KR_ORDER lib
    id the cell count, frame min/max, out-of-range frame count (frames >=
    the EI library's frame count), and the EI lib's frame count,
  - cell-attribute summary (flag byte 0 histogram, animated-cell count),
  - anomaly counters (same semantics as audit_mir3_maps.py).

`map-catalog.json` aggregates all maps with per-lib totals;
`map-catalog.md` is a human-readable table.

The EI data dir is required to resolve library frame counts (theme folder
first, root fallback — same as mapviewer.FramePool).

Usage:
    python3 Tools/build_map_catalog.py '<EI Map dir>' \
        --data '<EI Data dir>' \
        --minimap '<Mud3/Envir/MiniMap.txt>' \
        -o docs/research/mir3-map-reconstruction/catalog
"""

import argparse
import json
import os
import sys
from collections import Counter

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mapviewer
from audit_mir3_maps import v_lookup, kr_lib_id, EMPTY_FRAME, NO_OBJECT_FILE

THEMES = ["", "Wood", "Sand", "Snow", "Forest"]


def load_minimap_index(path: str) -> dict:
    """MiniMap.txt: 'display<TAB>minimapIdx'.  Return {display: minimap_idx}."""
    out = {}
    if not path or not os.path.exists(path):
        return out
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith(";"):
                continue
            parts = line.split()
            if len(parts) >= 2:
                out[parts[0].lower()] = parts[1]
    return out


def lib_frame_count(pool: mapviewer.FramePool, lid: int) -> int:
    lib = pool._get_lib(lid)
    return lib.count if lib is not None else 0


def catalog_map(path: str, pool: mapviewer.FramePool,
                minimap: dict) -> dict:
    with open(path, "rb") as f:
        data = np.frombuffer(f.read(), dtype=np.uint8)

    theme = int(np.frombuffer(data[20:22], dtype="<u2")[0])
    w = int(np.frombuffer(data[22:24], dtype="<u2")[0])
    h = int(np.frombuffer(data[24:26], dtype="<u2")[0])
    cell_off = 28 + (w // 2) * (h // 2) * 3
    rem = int(data.size) - cell_off
    cell_bytes = rem / (w * h) if w and h else 0
    if cell_bytes not in (13.0, 14.0):
        return {"name": os.path.basename(path), "theme": theme, "w": w, "h": h,
                "size": int(data.size), "size_ok": False, "cell_bytes": cell_bytes}
    cb = int(cell_bytes)

    stem = os.path.splitext(os.path.basename(path))[0]
    doc = {
        "name": os.path.basename(path),
        "display": minimap.get(stem.lower()),
        "theme": theme,
        "theme_name": THEMES[theme] if theme < len(THEMES) else str(theme),
        "w": w,
        "h": h,
        "size": int(data.size),
        "size_ok": True,
        "cell_bytes": cb,
        "legacy_13b": cb == 13,
    }

    # Ground layer (2x2 blocks, 3 bytes each on even cells)
    g = data[28:cell_off].reshape((w // 2) * (h // 2), 3)
    g_file = g[:, 0].copy()
    g_frame = g[:, 1:3].copy().view("<u2").reshape(-1)
    gq, gr, gv = v_lookup(g_file)
    g_draw = (gr <= 2) & (gv <= 69)
    g_kr = kr_lib_id(gq, gr, gv, g_file)
    ground = {}
    for lid in np.unique(g_kr[g_draw & (g_kr >= 0)]):
        m = g_draw & (g_kr == lid)
        frames = g_frame[m]
        cap = lib_frame_count(pool, int(lid))
        reserved = (frames >= 0xFF00) & (frames <= 0xFFFE)
        oob = int(np.count_nonzero((frames != EMPTY_FRAME) & (frames >= cap) & ~reserved))
        ground[str(int(lid))] = {
            "lib": mapviewer.KR_ORDER[int(lid)],
            "cells": int(np.count_nonzero(m)),
            "frame_min": int(frames.min()), "frame_max": int(frames.max()),
            "frame_oob": oob, "reserved": int(np.count_nonzero(reserved)),
            "lib_frames": cap,
        }

    # Object layers
    cells = data[cell_off:cell_off + w * h * cb].reshape(w, h, cb)
    mid_file = cells[:, :, 4].copy()
    mid_frame = cells[:, :, 5:7].copy().view("<u2").reshape(w, h)
    front_file = cells[:, :, 3].copy()
    front_frame = cells[:, :, 7:9].copy().view("<u2").reshape(w, h)

    def object_libs(file_arr, frame_arr):
        q, r, v = v_lookup(file_arr)
        lib_id = kr_lib_id(q, r, v, file_arr)
        skip = ((frame_arr == EMPTY_FRAME) | (file_arr == NO_OBJECT_FILE)
                | (r <= 2) | (v > 69) | (lib_id < 0))
        draw = ~skip
        out = {}
        fids = lib_id[draw]
        frms = frame_arr[draw]
        for lid in np.unique(fids):
            m = fids == lid
            frames = frms[m]
            cap = lib_frame_count(pool, int(lid))
            reserved = (frames >= 0xFF00) & (frames <= 0xFFFE)
            oob = int(np.count_nonzero((frames >= cap) & ~reserved))
            out[str(int(lid))] = {
                "lib": mapviewer.KR_ORDER[int(lid)],
                "cells": int(np.count_nonzero(m)),
                "frame_min": int(frames.min()), "frame_max": int(frames.max()),
                "frame_oob": oob, "reserved": int(np.count_nonzero(reserved)),
                "lib_frames": cap,
            }
        return out

    doc["ground"] = ground
    doc["mid"] = object_libs(mid_file, mid_frame)
    doc["front"] = object_libs(front_file, front_frame)

    flag0 = cells[:, :, 0].copy()
    doc["cell_flag0"] = dict(sorted(Counter(int(x) for x in np.unique(flag0)).items()))
    anim = int(np.count_nonzero((cells[:, :, 1] != 0xFF) | (cells[:, :, 2] != 0xFF)))
    if anim:
        doc["animated_cells"] = anim

    # anomaly aggregate
    anom = Counter()
    for layer in ("ground", "mid", "front"):
        for lid, info in doc[layer].items():
            if info["frame_oob"]:
                anom[f"{layer}_lib{lid}_frame_oob"] = info["frame_oob"]
    g_not_drawn = int(np.count_nonzero(~g_draw))
    if g_not_drawn:
        anom["ground_not_drawn"] = g_not_drawn
    doc["anomalies"] = dict(sorted(anom.items()))
    doc["anomaly_total"] = sum(anom.values())
    return doc


def main():
    ap = argparse.ArgumentParser(description="Build per-map JSON catalog")
    ap.add_argument("maps_dir")
    ap.add_argument("--data", required=True, help="EI Data dir (frame counts)")
    ap.add_argument("--minimap", default=None, help="Mud3/Envir/MiniMap.txt")
    ap.add_argument("-o", "--out", default=None, help="output dir (default: ./catalog)")
    args = ap.parse_args()

    out_dir = args.out or "catalog"
    maps_dir = os.path.join(out_dir, "maps")
    os.makedirs(maps_dir, exist_ok=True)

    pool = mapviewer.FramePool(args.data)
    minimap = load_minimap_index(args.minimap)

    names = sorted(f for f in os.listdir(args.maps_dir) if f.lower().endswith(".map"))
    docs = []
    for i, name in enumerate(names):
        d = catalog_map(os.path.join(args.maps_dir, name), pool, minimap)
        docs.append(d)
        with open(os.path.join(maps_dir, os.path.splitext(name)[0] + ".json"), "w") as f:
            json.dump(d, f, indent=1, ensure_ascii=False)
        if (i + 1) % 100 == 0:
            print(f"... {i + 1}/{len(names)}", file=sys.stderr)

    # aggregate
    agg = {
        "maps_dir": args.maps_dir,
        "data_dir": args.data,
        "count": len(docs),
        "legacy": sum(1 for d in docs if d.get("legacy_13b")),
        "size_mismatch": sum(1 for d in docs if not d.get("size_ok", True)),
        "with_anomalies": sum(1 for d in docs if d.get("anomaly_total", 0)),
        "anomaly_total": sum(d.get("anomaly_total", 0) for d in docs),
    }
    with open(os.path.join(out_dir, "map-catalog.json"), "w") as f:
        json.dump({"summary": agg, "maps": docs}, f, indent=1, ensure_ascii=False)

    # markdown table
    lines = ["# Mir3 EI map catalog", "",
             f"maps: {agg['count']} · legacy 13B/cell: {agg['legacy']} · "
             f"size mismatch: {agg['size_mismatch']} · with anomalies: "
             f"{agg['with_anomalies']} (total {agg['anomaly_total']})", "",
             "| map | display | th | w | h | B/cell | anim | anom | layers |",
             "|---|---|---|---|---|---|---|---|---|"]
    for d in docs:
        layers = []
        for layer in ("ground", "mid", "front"):
            n = len(d.get(layer, {}))
            if n:
                layers.append(f"{layer}:{n}")
        lines.append(f"| {d['name']} | {d.get('display') or '-'} | {d['theme']} | "
                     f"{d['w']} | {d['h']} | {d.get('cell_bytes') or '-'} | "
                     f"{d.get('animated_cells', 0)} | {d.get('anomaly_total', 0)} | "
                     f"{', '.join(layers)} |")
    with open(os.path.join(out_dir, "map-catalog.md"), "w") as f:
        f.write("\n".join(lines) + "\n")

    print(f"cataloged {len(docs)} maps -> {out_dir}")
    print(f"legacy 13B: {agg['legacy']} · size mismatch: {agg['size_mismatch']} · "
          f"anomaly total: {agg['anomaly_total']} across {agg['with_anomalies']} maps")


if __name__ == "__main__":
    main()
