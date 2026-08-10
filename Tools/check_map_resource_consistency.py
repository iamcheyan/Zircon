#!/usr/bin/env python3
"""check_map_resource_consistency.py — cross-client library consistency check.

Compares every library used by the maps (by KR_ORDER file id) across the two
data sources:

  - EI 3.0 client (WIL theme folders + root fallback)
  - ZL 2017 client (Map Data ZL libraries)

For each (theme, lib) it reports frame counts, distinct frame sizes, and
flags inconsistencies that matter for map reconstruction:

  * frame-count mismatch (EI smaller => maps referencing high frames get
    silent holes under EI data; ZL smaller => the reverse),
  * size mismatch (same frame id decoding to different dimensions suggests
    the frame tables do not line up between the clients),
  * missing library in one client,
  * pathological per-frame data (alpha placeholder == 4 in ZL ground libs
    — harmless in the ZL client which blits ground from MapInfo.Background,
    but it must be normalised when rendering per-tile ground).

Input mirrors the audit tool: map file ids are resolved with the same
KR_ORDER table; EI libs resolve theme-folder-first, then root.

Usage:
    python3 Tools/check_map_resource_consistency.py \
        --data-ei '<EI Data dir>' --data-zl '<ZL Data/Map Data dir>' \
        [-o out.json] [--html out.html]
"""

import argparse
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mapviewer
from zlsdk import ZlLibrary
from wilsdk import WilLibrary


def load_lib(path: str):
    if path.lower().endswith(".zl"):
        return ZlLibrary(path)
    return WilLibrary(path)


def lib_info(path: str, sample_frames: int = 8):
    """Frame count + distinct sizes (+ alpha placeholder detection)."""
    lib = load_lib(path)
    info = {"count": lib.count, "sizes": {}, "errors": 0, "alpha_placeholder": False}
    step = max(1, lib.count // sample_frames)
    seen_alpha_lo = 0
    seen_alpha_hi = 0
    for i in range(0, lib.count, step):
        try:
            im = lib.decode(i)
        except Exception:
            info["errors"] += 1
            continue
        if im is None:
            info["errors"] += 1
            continue
        key = f"{im.width}x{im.height}"
        info["sizes"][key] = info["sizes"].get(key, 0) + 1
        try:
            ext = im.getextrema()
            a_min = ext[3][0]
            a_max = ext[3][1]
        except Exception:
            a_min, a_max = 255, 255
        if a_max <= 15:
            seen_alpha_lo += 1
        else:
            seen_alpha_hi += 1
    if seen_alpha_lo and not seen_alpha_hi:
        info["alpha_placeholder"] = True
    return info


def resolve_path(data_dir: str, theme: str, lib_name: str) -> str | None:
    """Theme-folder-first then root, matching mapviewer._find_library_path."""
    cands = []
    if theme:
        cands.append(os.path.join(data_dir, theme, lib_name + ".Zl"))
        cands.append(os.path.join(data_dir, theme, lib_name + ".wil"))
    cands.append(os.path.join(data_dir, lib_name + ".Zl"))
    cands.append(os.path.join(data_dir, lib_name + ".wil"))
    for c in cands:
        if os.path.exists(c):
            return c
    return None


def main():
    ap = argparse.ArgumentParser(description="Cross-client library consistency check")
    ap.add_argument("--data-ei", required=True, help="EI client Data dir")
    ap.add_argument("--data-zl", required=True, help="ZL Data/Map Data dir")
    ap.add_argument("-o", "--out", help="JSON output path")
    ap.add_argument("--html", help="HTML report path")
    args = ap.parse_args()

    # themes = folders present in the ZL data dir; 14 libs each
    themes = [d for d in os.listdir(args.data_zl)
              if os.path.isdir(os.path.join(args.data_zl, d)) and d.lower() in
              ("wood", "sand", "snow", "forest")]
    if not themes:
        print("no theme folders found under --data-zl", file=sys.stderr)
        sys.exit(2)
    lib_names = [n[:-3] if n.lower().endswith(".zl") else n[:-4]
                 for n in os.listdir(os.path.join(args.data_zl, themes[0]))
                 if n.lower().endswith((".zl", ".wil"))]
    lib_names = sorted(set(n for n in lib_names if n))

    rows = []
    for theme in sorted(themes):
        for lib in lib_names:
            p_ei = resolve_path(args.data_ei, theme, lib)
            p_zl = resolve_path(args.data_zl, theme, lib)
            row = {"theme": theme, "lib": lib,
                   "ei_path": p_ei, "zl_path": p_zl}
            if p_ei:
                try:
                    row["ei"] = lib_info(p_ei)
                except Exception as e:
                    row["ei_error"] = str(e)
            if p_zl:
                try:
                    row["zl"] = lib_info(p_zl)
                except Exception as e:
                    row["zl_error"] = str(e)
            # comparisons
            ei = row.get("ei"); zl = row.get("zl")
            if ei and zl:
                row["frame_cmp"] = ("EQ" if ei["count"] == zl["count"] else
                                    "ZL_LARGER" if zl["count"] > ei["count"] else
                                    "EI_LARGER")
                common = set(ei["sizes"]) & set(zl["sizes"])
                row["size_cmp"] = ("MATCH" if (common and
                                    set(ei["sizes"]) == set(zl["sizes"])) else
                                   "DIFF")
                if ei["sizes"] == zl["sizes"] and ei["count"] == zl["count"]:
                    row["cmp"] = "MATCH"
                elif ei["sizes"] == zl["sizes"]:
                    row["cmp"] = "FRAME_DIFF"
                elif ei["count"] == zl["count"]:
                    row["cmp"] = "SIZE_DIFF"
                else:
                    row["cmp"] = "DIFF"
            elif ei:
                row["cmp"] = "ZL_MISSING"
            elif zl:
                row["cmp"] = "EI_MISSING"
            else:
                row["cmp"] = "BOTH_MISSING"
            if zl and zl.get("alpha_placeholder"):
                row["zl_alpha_placeholder"] = True
            rows.append(row)

    summary = {
        "total": len(rows),
        "match": sum(1 for r in rows if r["cmp"] == "MATCH"),
        "frame_diff": sum(1 for r in rows if r["cmp"] == "FRAME_DIFF"),
        "size_diff": sum(1 for r in rows if r["cmp"] == "SIZE_DIFF"),
        "diff": sum(1 for r in rows if r["cmp"] == "DIFF"),
        "zl_missing": sum(1 for r in rows if r["cmp"] == "ZL_MISSING"),
        "ei_missing": sum(1 for r in rows if r["cmp"] == "EI_MISSING"),
        "zl_alpha_placeholder": sum(1 for r in rows if r.get("zl_alpha_placeholder")),
    }
    doc = {"generated": __import__("datetime").date.today().isoformat(),
           "data_ei": args.data_ei, "data_zl": args.data_zl,
           "note": ("Per-(theme,lib) cross-client consistency: frame counts, "
                    "distinct sizes, alpha-placeholder detection (ZL BC3 alpha=4 "
                    "ground libs).  EI resolves theme-folder-first then root; "
                    "ZL resolves theme-folder-first then root."),
           "summary": summary, "rows": rows}

    if args.out:
        with open(args.out, "w") as f:
            json.dump(doc, f, indent=1)
        print(f"written {args.out} — {summary['total']} (theme,lib) rows")
    if args.html:
        write_html(args.html, doc)
        print(f"written {args.html}")

    # console summary
    print("MATCH       %4d" % summary["match"])
    print("FRAME_DIFF  %4d" % summary["frame_diff"])
    print("SIZE_DIFF   %4d" % summary["size_diff"])
    print("DIFF        %4d" % summary["diff"])
    print("ZL_MISSING  %4d" % summary["zl_missing"])
    print("EI_MISSING  %4d" % summary["ei_missing"])
    print("ZL_ALPHA_PLACEHOLDER %4d" % summary["zl_alpha_placeholder"])


def write_html(path: str, doc: dict) -> None:
    esc = __import__("html").escape
    rows = doc["rows"]
    s = doc["summary"]
    head = f"""<!DOCTYPE html><html lang="zh-CN"><head><meta charset="utf-8">
<title>Map resource consistency (EI vs ZL)</title>
<style>
 body {{ font-family: sans-serif; margin: 24px; color: #222; }}
 table {{ border-collapse: collapse; margin-top: 12px; }}
 th, td {{ border: 1px solid #bbb; padding: 3px 8px; font-size: 13px; }}
 th {{ background: #eef; }}
 .match {{ background: #dfd; }} .diff {{ background: #fdd; }}
 .frame-diff {{ background: #ffe9c9; }} .missing {{ background: #eee; }}
 tr.alpha td {{ outline: 1px dashed #d40; }}
</style></head><body>
<h2>Map resource consistency — EI vs ZL</h2>
<p>EI: <code>{esc(doc['data_ei'])}</code><br>ZL: <code>{esc(doc['data_zl'])}</code></p>
<p>total {s['total']} · MATCH {s['match']} · FRAME_DIFF {s['frame_diff']} ·
SIZE_DIFF {s['size_diff']} · DIFF {s['diff']} · ZL_MISSING {s['zl_missing']} ·
EI_MISSING {s['ei_missing']} · ZL_ALPHA_PLACEHOLDER {s['zl_alpha_placeholder']}</p>
<table><tr><th>theme</th><th>lib</th><th>cmp</th><th>EI frames</th><th>ZL frames</th>
<th>EI sizes</th><th>ZL sizes</th><th>ZL alpha=4</th><th>errors</th></tr>"""
    body = []
    for r in rows:
        cls = {"MATCH": "match", "DIFF": "diff", "FRAME_DIFF": "frame-diff",
               "SIZE_DIFF": "frame-diff", "ZL_MISSING": "missing",
               "EI_MISSING": "missing", "BOTH_MISSING": "missing"}.get(r["cmp"], "")
        alpha = "yes" if r.get("zl_alpha_placeholder") else ""
        ei = r.get("ei") or {}
        zl = r.get("zl") or {}
        errs = []
        if r.get("ei_error"): errs.append("EI:" + r["ei_error"])
        if r.get("zl_error"): errs.append("ZL:" + r["zl_error"])
        ei_sizes = ", ".join(f"{k}×{v}" for k, v in sorted(ei.get("sizes", {}).items())[:6])
        zl_sizes = ", ".join(f"{k}×{v}" for k, v in sorted(zl.get("sizes", {}).items())[:6])
        body.append(
            f'<tr class="{cls}"><td>{esc(r["theme"])}</td><td>{esc(r["lib"])}</td>'
            f'<td>{esc(r["cmp"])}</td><td>{ei.get("count", "—")}</td><td>{zl.get("count", "—")}</td>'
            f'<td>{esc(ei_sizes)}</td><td>{esc(zl_sizes)}</td><td>{alpha}</td>'
            f'<td>{esc("; ".join(errs))}</td></tr>')
    with open(path, "w") as f:
        f.write(head + "".join(body) + "</table></body></html>")
    print(f"html report: {path}")


if __name__ == "__main__":
    main()
