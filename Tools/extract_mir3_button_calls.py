#!/usr/bin/env python3
"""Extract primary-binary button constructor calls from Mir3.exe.

VA 0x00417550 is a fixed-argument UI-control initializer in the EI binary:
the callee stores image/frame fields and the two position arguments into the
control object, then builds its hit rectangle from the WIL image dimensions.
This script records the raw call neighborhood and register expressions.  It
does not pretend that a register expression is a final screen coordinate
until its base object is identified.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble, immediate

TARGET = "0x417550"
# The EI window classes use additional local control pairs (for example
# 161/162 and 264/265) that are not part of the early public source's small
# frame table.  Keep the broad range for discovery, but preserve all raw
# pushes and treat the result as a candidate until the resource file is
# checked.
FRAME_IDS = set(range(40, 1201))

CALL_RE = re.compile(r"^0x?([0-9a-f]+)", re.I)
REG_PUSH_RE = re.compile(r"push\s+(eax|ebx|ecx|edx|esi|edi|ebp)$", re.I)


def is_target(args: str) -> bool:
    return bool(re.search(r"0x417550(?:\s|$)", args, re.I))


def extract(lines: list[dict], radius: int = 18) -> list[dict]:
    records = []
    for i, line in enumerate(lines):
        if line["op"] != "call" or not is_target(line["args"]):
            continue
        window = lines[max(0, i - radius):i]
        immediate_pushes = [(item["address"], immediate(item["args"])) for item in window if item["op"] == "push" and immediate(item["args"]) is not None]
        frame_values = [(va, value) for va, value in immediate_pushes if value in FRAME_IDS]
        pairs = []
        if len(frame_values) >= 2:
            for a, b in zip(frame_values, frame_values[1:]):
                if abs(a[1] - b[1]) == 1:
                    pairs.append({
                        "push_sequence": [a[1], b[1]],
                        "frame_pair": sorted([a[1], b[1]]),
                        "push_vas": [f"0x{a[0]:08x}", f"0x{b[0]:08x}"],
                    })
        pair = pairs[-1] if pairs else None
        reg_pushes = []
        for item in window:
            match = REG_PUSH_RE.fullmatch(f"{item['op']} {item['args']}")
            if match:
                reg_pushes.append({"va": f"0x{item['address']:08x}", "register": match.group(1).lower()})
        assignments = []
        for item in window:
            if item["op"] in {"mov", "add", "sub", "lea"}:
                assignments.append(item["text"].strip())
        object_lea = next((item["text"].strip() for item in reversed(window) if item["op"] == "lea"), None)
        records.append({
            "source": "Mir3.exe",
            "constructor_va": TARGET,
            "call_va": f"0x{line['address']:08x}",
            "frame_pair": pair,
            "frame_pair_candidates": pairs,
            "position_register_pushes_near_call": reg_pushes[-4:],
            "object_lea_near_call": object_lea,
            "register_assignments_near_call": assignments[-12:],
            "raw_neighborhood": [item["text"].strip() for item in window[-18:]],
            "confidence": "primary-static-control-initializer" if pair else "primary-call-unknown-control",
        })
    return records


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/button_constructor_calls.json"))
    args = parser.parse_args()
    records = extract(disassemble(args.exe))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "static x86 disassembly; direct calls to VA 0x00417550",
        "warning": "The position registers still require base-object tracing; frame pairs and constructor semantics are primary-binary evidence.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"button_constructor_calls={len(records)}")
    print(f"with_frame_pair={sum(1 for r in records if r['frame_pair'])}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
