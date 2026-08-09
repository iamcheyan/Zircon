#!/usr/bin/env python3
"""Collect likely window/resource initialization calls from the main UI init.

This intentionally reports candidates only.  The callee signatures are not
recovered yet, so a numeric frame ID is not automatically a window name.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble, immediate

WINDOW_FRAMES = {
    50, 51, 145, 169, 200, 201, 202, 250, 251, 253, 254, 255,
    300, 350, 600, 700, 750, 850, 900, 1050, 1100,
}
LEA_RE = re.compile(r"lea\s+([a-z]+),\s*\[([a-z]+)\s*\+\s*(0x[0-9a-f]+|[0-9]+)\]", re.I)


def extract(lines: list[dict], start: int, end: int, radius: int = 18) -> list[dict]:
    scoped = [line for line in lines if start <= line["address"] < end]
    records = []
    for pos, line in enumerate(scoped):
        if line["op"] != "call":
            continue
        window = scoped[max(0, pos - radius):pos]
        values = []
        for item in window:
            if item["op"] == "push":
                value = immediate(item["args"])
                if value in WINDOW_FRAMES:
                    values.append({"va": f"0x{item['address']:08x}", "value": value})
        if not values:
            continue
        leas = []
        for item in window:
            m = LEA_RE.fullmatch(f"{item['op']} {item['args']}")
            if m:
                leas.append({"va": f"0x{item['address']:08x}", "register": m.group(1), "base": m.group(2), "offset": int(m.group(3), 0), "text": item["text"].strip()})
        records.append({
            "source": "Mir3.exe",
            "call_va": f"0x{line['address']:08x}",
            "target": line["args"],
            "resource_frame_candidates": values,
            "object_lea_candidates": leas[-4:],
            "raw_neighborhood": [item["text"].strip() for item in window],
            "confidence": "primary-static-window-init-candidate",
        })
    return records


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--start", type=lambda value: int(value, 0), default=0x427600)
    parser.add_argument("--end", type=lambda value: int(value, 0), default=0x4279B2)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window_init_candidates.json"))
    args = parser.parse_args()
    records = extract(disassemble(args.exe), args.start, args.end)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "range": [f"0x{args.start:08x}", f"0x{args.end:08x}"],
        "warning": "Frame IDs and object fields are candidates until each callee signature and resource loading path is traced.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"window_init_candidates={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
