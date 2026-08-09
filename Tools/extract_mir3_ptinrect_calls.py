#!/usr/bin/env python3
"""Extract PtInRect call sites from the original EI Mir3.exe.

This is deliberately an evidence extractor, not a semantic decompiler.  It
keeps the raw argument neighborhood because most window hit tests use dynamic
RECT/POINT values.  The three calls in the original button object at
0x00417791, 0x004177D1 and 0x00417802 are additionally identified as the
button hover/click hit-test methods.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble, immediate

PTINRECT_IAT = "0x4762b4"
BUTTON_METHODS = {
    "0x00417791": "button-hover-or-state-update",
    "0x004177d1": "button-click-test",
    "0x00417802": "button-secondary-click-test",
}


def is_direct(args: str) -> bool:
    return bool(re.search(r"\[0x4762b4\]", args, re.I))


def extract(lines: list[dict], radius: int = 12) -> list[dict]:
    records: list[dict] = []
    for index, line in enumerate(lines):
        if line["op"] != "call" or not is_direct(line["args"]):
            continue
        window = lines[max(0, index - radius):index]
        pushes = []
        for item in window:
            if item["op"] == "push":
                pushes.append({
                    "va": f"0x{item['address']:08x}",
                    "args": item["args"],
                    "immediate": immediate(item["args"]),
                })
        records.append({
            "source": "Mir3.exe",
            "ptinrect_iat": PTINRECT_IAT,
            "call_va": f"0x{line['address']:08x}",
            "known_role": BUTTON_METHODS.get(f"0x{line['address']:08x}"),
            "argument_convention": "PtInRect(rect_ptr, POINT{x,y}); caller pushes y, x, rect_ptr",
            "recent_pushes": pushes[-3:],
            "raw_neighborhood": [item["text"].strip() for item in window],
            "confidence": "primary-static-ptinrect-call",
        })
    return records


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/ptinrect_calls.json"))
    args = parser.parse_args()
    records = extract(disassemble(args.exe))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "llvm-objdump x86 disassembly; direct calls through USER32.PtInRect IAT 0x004762B4",
        "warning": "Most call sites are only candidates until their owning window/control object is traced.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"ptinrect_calls={len(records)}")
    print(f"button_method_calls={sum(1 for r in records if r['known_role'])}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
