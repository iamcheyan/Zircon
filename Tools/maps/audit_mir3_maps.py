#!/usr/bin/env python3
"""audit_mir3_maps.py — per-map resource audit for Mir3 EI maps.

Reads every .map in a directory (28-byte header + 3-byte/block ground layer +
14-byte/cell object layer), resolves each mid/front cell's file/frame
through the renderer's authoritative lookup (raw file byte -> KR_ORDER key;
Mir3.exe indexes a contiguous 14-slot/group table with v = file - floor(file/14),
and ZL's discrete 12-key groups are v + q = file, so both clients resolve the
same library from the same file byte), then checks the resolved frame
against the real library frame count as loaded by the renderer (FramePool with
the EI data dir: theme folder first, root fallback).

Flags every cell whose frame is out of range (would render blank or crash),
whose file is a reserved marker (0xffff empty / file 15 no-object), or whose
resolved library does not exist in the data dir.  Emits per-map anomaly
counters and a ranked summary (JSON + stdout).

Usage:
    python3 Tools/maps/audit_mir3_maps.py <maps_dir> --data <data_dir> [-o out.json] [--limit N]
"""

import argparse
import html
import json
import os
import sys
from collections import Counter

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mapviewer  # KR_ORDER, FramePool, LAYOUT constants


def v_lookup(file_arr: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """Authoritative v-transform (Mir3.exe 0x43bb82 sequence, see
    /tmp/decode_sim.py): v = file - floor(file/14); q = v // 14; r = v % 14.
    q = theme group (0 = base, 1 = wood, 2 = sand, 3 = snow, 4 = forest),
    r = library within the group.  Cells with r <= 2 (ground libs) or v > 69
    are not drawn as objects (0x43bba5 lib<3 skip / 0x43bbb2 v>0x45 skip).
    """
    v = file_arr - np.floor_divide(file_arr, 14)
    q = np.floor_divide(v, 14)
    r = np.mod(v, 14)
    return q, r, v


def kr_lib_id(q: np.ndarray, r: np.ndarray, v: np.ndarray, file_arr: np.ndarray) -> np.ndarray:
    """Map a cell's raw file byte to the KR_ORDER library id used by the
    renderer (raw file -> KR_ORDER[file], same lookup as GodotClient
    MapView.cs:278 and mapviewer's FramePool._get_lib).

    Why the raw file value, not v: Mir3.exe indexes its 14-lib/group slot
    table with the v-transform (slot = v = file - floor(file/14); groups are
    CONTIGUOUS 14-slot bands 0-13/14-27/28-41/42-55/56-69, each including
    object1c/object2c — 0x43bbce lea 0x5600fc(,%edx,4) with edx=v*9, stride
    324 B; 0x43bbb2 skips v>0x45=69=5*14).  ZL KR_ORDER instead uses DISCRETE
    12-lib groups (0-13, 15-26, 30-41, 45-56, 60-71 — no object1c/2c), so
    ZL key = 14q + r + q = v + q = file - floor(file/14) + floor(file/14)
    = file for regular libs (r <= 11).  Both clients agree on the same map
    file bytes; the renderer's raw lookup IS the EI semantics.  Files outside
    the KR_ORDER key set -> -1 (unresolved).
    """
    out = np.full(q.shape, -1, dtype=np.int32)
    keys = np.fromiter(mapviewer.KR_ORDER, dtype=np.int32)
    ok = np.isin(file_arr, keys)
    out[ok] = file_arr[ok]
    return out


EMPTY_FRAME = 0xFFFF
NO_OBJECT_FILE = 15  # v=14 -> theme-1 ground lib 0, lib<3 skip in object pass


def audit_map(path: str, pool: mapviewer.FramePool) -> dict:
    with open(path, "rb") as f:
        data = np.frombuffer(f.read(), dtype=np.uint8)

    theme = int(np.frombuffer(data[20:22], dtype="<u2")[0])
    w = int(np.frombuffer(data[22:24], dtype="<u2")[0])
    h = int(np.frombuffer(data[24:26], dtype="<u2")[0])
    cell_off = 28 + (w // 2) * (h // 2) * 3
    rem = int(data.size) - cell_off
    cell_bytes = rem / (w * h) if w and h else 0
    # two map generations: 14 B/cell (with +12/+13 attr bytes, EI current) and
    # 13 B/cell (no attr bytes, legacy; these maps are not in the server index)
    if cell_bytes not in (13.0, 14.0):
        return {
            "name": os.path.basename(path), "theme": theme, "w": w, "h": h,
            "size": int(data.size), "size_ok": False, "cell_bytes": cell_bytes,
            "size_mismatch": True,
        }
    cb = int(cell_bytes)
    result = {
        "name": os.path.basename(path),
        "theme": theme,
        "w": w,
        "h": h,
        "size": int(data.size),
        "size_ok": True,
        "cell_bytes": cb,
    }

    # Ground layer: 2x2 blocks, 3 bytes each (file byte, frame u16) on even cells
    g = data[28:cell_off].reshape((w // 2) * (h // 2), 3)
    g_file = g[:, 0].copy()
    g_frame = g[:, 1:3].copy().view("<u2").reshape(-1)

    # Object layer: cb bytes/cell (14: +0 flag, +1 midAnim, +2 frontAnim,
    # +3 frontFile, +4 midFile, +5 midImg u16, +7 frontImg u16, +9..11 res,
    # +12/+13 attr; 13: same without the trailing attr bytes)
    cells = data[cell_off:cell_off + w * h * cb].reshape(w, h, cb)
    mid_file = cells[:, :, 4].copy()
    mid_frame = cells[:, :, 5:7].copy().view("<u2").reshape(w, h)
    front_file = cells[:, :, 3].copy()
    front_frame = cells[:, :, 7:9].copy().view("<u2").reshape(w, h)

    anom = Counter()

    # --- ground layer checks: ground file also goes through the v-transform
    # (file 0/1/2 are the q=0, r=0/1/2 special case; e.g. file 17 -> v=16 ->
    # q=1 r=2 wood_tiles5c is a valid ground lib).  Drawn when r <= 2 and
    # v <= 0x45 (0x43b317), i.e. same rule as the object pass' complement.
    gq, gr, gv = v_lookup(g_file)
    g_draw = (gr <= 2) & (gv <= 69)
    g_undrawn = int(np.count_nonzero(~g_draw))
    if g_undrawn:
        anom["ground_not_drawn"] = g_undrawn
    g_used = {}
    g_kr = kr_lib_id(gq, gr, gv, g_file)
    for lid in np.unique(g_kr[g_draw & (g_kr >= 0)]):
        m = g_draw & (g_kr == lid)
        frames = g_frame[m]
        lib = pool._get_lib(int(lid))
        cap = lib.count if lib is not None else 0
        reserved = (frames >= 0xFF00) & (frames <= 0xFFFE)
        n_res = int(np.count_nonzero(reserved))
        oob = int(np.count_nonzero((frames != EMPTY_FRAME) & (frames >= cap) & ~reserved))
        if n_res:
            result["ground_reserved"] = result.get("ground_reserved", 0) + n_res
        if oob:
            anom[f"ground_lib{int(lid)}_frame_oob"] = oob
        g_used[str(int(lid))] = {"blocks": int(np.count_nonzero(m)),
                                 "frame_max": int(frames.max()), "frame_oob": oob,
                                 "reserved_frames": n_res,
                                 "lib_frames": cap}

    # --- object layer checks
    def object_check(file_arr, frame_arr, label):
        q, r, v = v_lookup(file_arr)
        lib_id = kr_lib_id(q, r, v, file_arr)
        # skip conditions the renderer honours: empty frame, no-object file,
        # ground-group lib (r<=2), v>69, unresolved lib id
        skip = (frame_arr == EMPTY_FRAME) | (file_arr == NO_OBJECT_FILE) | (r <= 2) | (v > 69) | (lib_id < 0)
        draw = ~skip
        n_draw = int(np.count_nonzero(draw))
        if not n_draw:
            return {}
        fids = lib_id[draw]
        frms = frame_arr[draw]
        libs = {}
        for lid in np.unique(fids):
            m = fids == lid
            frames = frms[m]
            lib = pool._get_lib(int(lid))
            cap = lib.count if lib is not None else 0
            # 0xff00-0xfffe are sparse reserved markers (26 distinct values
            # across all libs, <=16 cells each) — not animation, not OOB
            reserved = (frames >= 0xFF00) & (frames <= 0xFFFE)
            n_res = int(np.count_nonzero(reserved))
            oob = int(np.count_nonzero((frames >= cap) & ~reserved))
            libs[str(lid)] = {"cells": int(np.count_nonzero(m)),
                              "frame_min": int(frames.min()), "frame_max": int(frames.max()),
                              "frame_oob": oob, "reserved_frames": n_res,
                              "lib_frames": cap}
            if n_res:
                result[f"{label}_reserved"] = result.get(f"{label}_reserved", 0) + n_res
            if oob:
                anom[f"{label}_lib{lid}_frame_oob"] = oob
        # files that resolve to nothing (no KR entry or v>69) but are not the
        # documented no-object marker
        unresolved = int(np.count_nonzero((lib_id < 0) & ~skip))
        if unresolved:
            anom[f"{label}_unresolved_file"] = unresolved
        return libs

    result["mid_libs"] = object_check(mid_file, mid_frame, "mid")
    result["front_libs"] = object_check(front_file, front_frame, "front")
    result["ground"] = g_used

    # non-zero cell flags (bytes 0/1/2/9..11/12/13) — informational
    flag0 = cells[:, :, 0].copy()
    counts = Counter(int(x) for x in np.unique(flag0))
    result["cell_flag0"] = dict(sorted(counts.items()))
    anim = int(np.count_nonzero((cells[:, :, 1] != 0xFF) | (cells[:, :, 2] != 0xFF)))
    if anim:
        result["animated_cells"] = anim
    result["anomalies"] = dict(sorted(anom.items()))
    result["anomaly_total"] = sum(anom.values())
    return result


def main():
    ap = argparse.ArgumentParser(description="Audit Mir3 EI maps against library resources")
    ap.add_argument("maps_dir")
    ap.add_argument("--data", required=True, help="EI Data dir (theme folders + root wil libs)")
    ap.add_argument("-o", "--out", default=None, help="JSON output path")
    ap.add_argument("--limit", type=int, default=0, help="only audit first N maps (0 = all)")
    ap.add_argument("--html", default=None, help="write anomaly ranking HTML report")
    args = ap.parse_args()

    pool = mapviewer.FramePool(args.data)
    names = sorted(f for f in os.listdir(args.maps_dir) if f.lower().endswith(".map"))
    if args.limit:
        names = names[: args.limit]

    results = []
    for i, name in enumerate(names):
        r = audit_map(os.path.join(args.maps_dir, name), pool)
        results.append(r)
        if (i + 1) % 100 == 0:
            print(f"... {i + 1}/{len(names)}", file=sys.stderr)

    ranked = sorted(results, key=lambda r: -r.get("anomaly_total", 0))
    print(f"maps audited: {len(results)}")
    print(f"with any anomaly: {sum(1 for r in results if r.get('anomaly_total', 0))}")
    print(f"size mismatch: {sum(1 for r in results if r.get('size_mismatch'))}")
    print()
    print("top 30 by anomaly total:")
    print(f"{'map':32s} {'th':>3s} {'w':>5s} {'h':>5s} {'anom':>6s}")
    for r in ranked[:30]:
        print(f"{r['name']:32s} {r['theme']:3d} {r['w']:5d} {r['h']:5d} {r.get('anomaly_total', 0):6d}")

    if args.out:
        with open(args.out, "w") as f:
            json.dump({"data_dir": args.data, "maps_dir": args.maps_dir,
                       "results": ranked}, f, indent=1, ensure_ascii=False)
        print(f"\nwrote {args.out}")

    if args.html:
        write_html_report(args.html, ranked, args.data, args.maps_dir)


def write_html_report(path: str, ranked: list, data_dir: str, maps_dir: str) -> None:
    """Standalone HTML anomaly ranking report (no external assets)."""
    n_any = sum(1 for r in ranked if r.get("anomaly_total", 0))
    n_mismatch = sum(1 for r in ranked if r.get("size_mismatch"))
    rows = []
    for r in ranked:
        anoms = r.get("anomalies", {})
        detail = "".join(
            f"<li><code>{k}</code>: {v}</li>" for k, v in sorted(anoms.items())
        ) or "<li>none</li>"
        resv = sum(v for k, v in r.items() if k.endswith("_reserved"))
        rows.append(f"""<tr>
<td><code>{html.escape(r['name'])}</code></td>
<td class="c">{r['theme']}</td>
<td class="c">{r['w']}</td>
<td class="c">{r['h']}</td>
<td class="c">{r.get('cell_bytes', '-')}</td>
<td class="c">{r.get('size_mismatch', False) and 'MISMATCH' or 'ok'}</td>
<td class="c">{r.get('animated_cells', 0)}</td>
<td class="c">{resv}</td>
<td class="c num">{r.get('anomaly_total', 0)}</td>
<td><ul class="d">{detail}</ul></td>
</tr>""")
    html_doc = f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<title>Mir3 EI map audit</title>
<style>
body {{ font-family: ui-monospace, Menlo, Consolas, monospace; margin: 2em; }}
table {{ border-collapse: collapse; }}
th, td {{ border: 1px solid #999; padding: 3px 8px; text-align: left; vertical-align: top; }}
td.c {{ text-align: center; }}
td.num {{ text-align: right; }}
tr:nth-child(even) {{ background: #f2f2f2; }}
ul.d {{ margin: 0; padding-left: 1.2em; }}
h1 {{ font-size: 1.4em; }}
</style></head><body>
<h1>Mir3 EI map audit — anomaly ranking</h1>
<p>maps: {len(ranked)} &nbsp; with anomalies: {n_any} &nbsp; size mismatch: {n_mismatch}</p>
<p>maps: <code>{html.escape(maps_dir)}</code><br>data: <code>{html.escape(data_dir)}</code></p>
<table>
<tr><th>map</th><th>th</th><th>w</th><th>h</th><th>B/cell</th><th>size</th>
<th>anim cells</th><th>reserved</th><th>anomalies</th><th>detail</th></tr>
{''.join(rows)}
</table>
<p>Library lookup = raw file byte -> KR_ORDER key (renderer path; Mir3.exe
resolves the same library via v = file - floor(file/14) into a contiguous
14-slot/group table, and ZL's discrete 12-key groups equal v + q = file for
regular libs).  EI Wood/SmObjectsc.wil holds only 969 frames, so file 25
cells with large frames are the real OOB driver on 3.map etc.
0xff00-0xfffe are sparse reserved markers, excluded from frame-oob.
Legacy 13 B/cell maps are not in the server minimap index.</p>
</body></html>"""
    with open(path, "w") as f:
        f.write(html_doc)
    print(f"wrote {path}")


if __name__ == "__main__":
    main()
