#!/usr/bin/env python3
"""wilextract.py — batch-export Mir3 EI WIL images to PNG.

Examples:
  # export every image of one library to ./out (one PNG per frame)
  python3 wilextract.py storeitem.wil -o out

  # export a frame range
  python3 wilextract.py Mon-1.wil -r 0-360 -o mon1

  # contact sheet (montage) instead of individual PNGs
  python3 wilextract.py Inventory.wil --sheet --cols 20 --scale 3 -o out.png

  # export all libraries under a directory, each into its own subfolder
  python3 wilextract.py /path/to/Data --all -o out

  # write a sidecar JSON with per-frame metadata (size/anchors)
  python3 wilextract.py storeitem.wil -o out --meta
"""
from __future__ import annotations

import argparse
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import wilsdk  # noqa: E402


def export_library(lib: wilsdk.WilLibrary, outdir: str, rng: tuple[int, int] | None,
                   meta: bool, sheet: bool, cols: int, scale: int) -> dict:
    sheet_file = None
    if sheet and outdir.lower().endswith((".png", ".jpg")):
        sheet_file = outdir
        outdir = os.path.dirname(outdir) or "."
    os.makedirs(outdir, exist_ok=True)
    start, end = rng if rng else (0, lib.count)
    end = min(end, lib.count)
    records = []
    imgs = []
    ok = 0
    for i in range(start, end):
        hdr = lib.header(i)
        try:
            im = lib.decode(i)
        except Exception as e:
            print(f"  [{i}] decode error: {e}", file=sys.stderr)
            im = None
        if hdr:
            records.append({**hdr, "saved": im is not None})
        if im is not None:
            ok += 1
            if sheet:
                imgs.append(im)
            else:
                im.save(os.path.join(outdir, f"{i:05d}.png"))
    if sheet:
        sheet_img = wilsdk.contact_sheet(imgs, cols, scale)
        dest = sheet_file or os.path.join(outdir, "sheet.png")
        sheet_img.save(dest)
        print(f"sheet saved: {dest} ({sheet_img.width}x{sheet_img.height}, {len(imgs)} frames)")
    if meta:
        meta_dir = os.path.dirname(sheet_file) if sheet_file else outdir
        with open(os.path.join(meta_dir, "meta.json"), "w", encoding="utf-8") as f:
            json.dump(records, f, ensure_ascii=False, indent=1)
    return {"file": lib.name, "range": [start, end], "decoded": ok, "total": lib.count}


def main():
    ap = argparse.ArgumentParser(description="Export WIL images to PNG / contact sheets")
    ap.add_argument("target", help="a .wil file, or a directory with --all")
    ap.add_argument("-o", "--out", default="out", help="output dir (or PNG path with --sheet)")
    ap.add_argument("-r", "--range", help="frame range like 0-360")
    ap.add_argument("--sheet", action="store_true", help="montage sheet instead of per-frame PNGs")
    ap.add_argument("--cols", type=int, default=20, help="sheet columns (default 20)")
    ap.add_argument("--scale", type=int, default=1, help="sheet scale factor (default 1)")
    ap.add_argument("--meta", action="store_true", help="write meta.json with per-frame headers")
    ap.add_argument("--all", action="store_true", help="target is a directory: export every .wil")
    args = ap.parse_args()

    rng = None
    if args.range:
        a, b = args.range.split("-")
        rng = (int(a), int(b))

    if args.all:
        root = args.target
        libs = wilsdk.scan_libraries(root)
        print(f"{len(libs)} libraries found under {root}")
        for lib in libs:
            sub = os.path.join(args.out, os.path.splitext(lib.name)[0])
            try:
                res = export_library(lib, sub, rng, args.meta, args.sheet, args.cols, args.scale)
                print(f"  {lib.name}: {res['decoded']}/{res['total']}")
            except Exception as e:
                print(f"  {lib.name}: FAILED {e}", file=sys.stderr)
        return

    wil_path = args.target
    if not wil_path.lower().endswith(".wil"):
        sys.exit("target must be a .wil file (or a directory with --all)")
    if not os.path.exists(os.path.splitext(wil_path)[0] + ".wix"):
        sys.exit(f"index file missing: {os.path.splitext(wil_path)[0] + '.wix'}")
    lib = wilsdk.WilLibrary(wil_path)
    print(f"{lib.name}: {lib.count} images")
    res = export_library(lib, args.out, rng, args.meta, args.sheet, args.cols, args.scale)
    print(f"decoded {res['decoded']} frames")


if __name__ == "__main__":
    main()
