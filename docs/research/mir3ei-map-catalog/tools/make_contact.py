#!/usr/bin/env python3
"""Recreate the 34 family contact sheets from the full-size renders in views/.

Layout matches the audited originals: 8 columns, 160x90 thumb cells with the
map filename in yellow at the bottom-left of each cell. Sheet filenames match
the family names used by build_report.py (num.png, D11xx.png, other.png, ...).

Usage:  python3 make_contact.py [views_dir] [out_dir]
"""
import glob, os, re, sys
from PIL import Image, ImageDraw, ImageFont

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VIEWS = sys.argv[1] if len(sys.argv) > 1 else os.path.join(BASE, "views")
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(BASE, "contact")

CELL_W, CELL_H = 168, 98   # 8 cols -> 1344 wide sheet
THUMB_W, THUMB_H = 160, 90

ORDER = ["num", "other", "kt", "E", "DM", "B",
         "D00xx", "D01xx", "D02xx", "D03xx", "D04xx", "D05xx",
         "D10xx", "D11xx", "D12xx", "D13xx", "D14xx", "D15xx",
         "D20xx", "D40xx", "D41xx", "D42xx", "D43xx", "D44xx", "D45xx",
         "D50xx", "D60xx", "D61xx", "D70xx", "D71xx",
         "D80xx", "D81xx", "D82xx", "D90xx"]


def family_of(mid):
    if mid.startswith("kt"): return "kt"
    if mid.startswith("E"): return "E"
    if mid.startswith("DM"): return "DM"
    if mid.startswith("B"): return "B"
    if re.fullmatch(r"[0-9]+", mid): return "num"
    if mid[0].isdigit() and "_" in mid: return "other"
    if mid.startswith("d"): return "other"
    if mid.startswith("D"):
        m = re.match(r"D(\d{2})", mid)
        if m:
            fam = "D" + m.group(1) + "xx"
            if fam in ORDER: return fam
    return "other"


def make_sheet(members, out_path):
    n = len(members)
    cols = 8
    rows = (n + cols - 1) // cols
    sheet = Image.new("RGB", (CELL_W * cols, CELL_H * rows), (8, 8, 8))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("/usr/share/fonts/dejavu/DejaVuSansMono-Bold.ttf", 14)
    except Exception:
        font = ImageFont.load_default()
    for i, mid in enumerate(members):
        try:
            im = Image.open(os.path.join(VIEWS, mid + ".png")).convert("RGB")
        except FileNotFoundError:
            continue
        im.thumbnail((THUMB_W, THUMB_H))
        cx = (i % cols) * CELL_W + (CELL_W - im.size[0]) // 2
        cy = (i // cols) * CELL_H + (CELL_H - im.size[1]) // 2
        sheet.paste(im, (cx, cy))
        draw.text((i % cols * CELL_W + 4, i // cols * CELL_H + CELL_H - 20),
                  mid, fill=(255, 220, 60), font=font)
    sheet.save(out_path)
    return n


def main():
    os.makedirs(OUT, exist_ok=True)
    files = sorted(os.path.basename(p)[:-4] for p in glob.glob(os.path.join(VIEWS, "*.png")))
    fams = {}
    for mid in files:
        fams.setdefault(family_of(mid), []).append(mid)
    total = 0
    for fam in ORDER:
        members = sorted(fams.get(fam, []))
        if not members:
            continue
        n = make_sheet(members, os.path.join(OUT, fam + ".png"))
        total += n
        print(f"{fam}: {n}")
    print(f"total {total} sheets {len(ORDER)}")


if __name__ == "__main__":
    main()
