#!/usr/bin/env python3
"""Find grouped UI-like constants in the original Mir3.exe.

This is a triage tool, not a decompiler.  It reports call-site neighborhoods
where a known early-Mir3 frame pair and one or more known relative coordinates
occur together.  Every result remains a candidate until the called function's
arguments and consumers are identified.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble, immediate

FRAME_PAIRS = {
    "exchange": (80, 81),
    "minimap": (82, 83),
    "skill_entry": (84, 85),
    "exit": (90, 91),
    "logout": (92, 93),
    "group": (94, 95),
    "guild": (96, 97),
    "belt": (52, 53),
    "skill": (100, 101),
    "chat": (102, 103),
    "quest": (104, 105),
    "option": (106, 107),
    "party": (108, 109),
    "status": (110, 111),
    "inventory": (112, 113),
    "store": (114, 115),
}

COORDINATE_VALUES = {0, 13, 34, 50, 65, 66, 88, 101, 102, 103, 104, 161, 204, 228, 252, 397, 616, 648, 664, 665, 703, 718}


def call_target(args: str) -> str:
    m = re.search(r"(0x[0-9a-f]+)", args, re.I)
    return m.group(1) if m else args


def find(lines: list[dict], radius: int) -> list[dict]:
    results = []
    for i, line in enumerate(lines):
        if line["op"] != "call":
            continue
        window = lines[max(0, i - radius):i]
        values = []
        for item in window:
            value = immediate(item["args"]) if item["op"] == "push" else None
            if value is not None:
                values.append((item["address"], value))
        raw = [value for _, value in values]
        matches = []
        for name, pair in FRAME_PAIRS.items():
            if pair[0] in raw and pair[1] in raw:
                matches.append(name)
        coords = sorted({value for value in raw if value in COORDINATE_VALUES})
        if not matches:
            continue
        results.append({
            "call_va": f"0x{line['address']:08x}",
            "target": call_target(line["args"]),
            "frame_pair_candidates": matches,
            "coordinate_values_in_window": coords,
            "score": len(matches) * 10 + min(len(coords), 10),
            "preceding_pushes": [{"va": f"0x{va:08x}", "value": value} for va, value in values],
        })
    return sorted(results, key=lambda item: (-item["score"], item["call_va"]))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--radius", type=int, default=28)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/ui-pattern-candidates.json"))
    args = parser.parse_args()
    records = find(disassemble(args.exe), args.radius)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "grouped immediate triage around direct call instructions",
        "warning": "Candidates are not confirmed draw calls or screen rectangles.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"pattern_candidates={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
