#!/usr/bin/env python3
"""Extract direct SetRect call neighborhoods from the EI client binary.

The import address is derived from the PE import table:
USER32.SetRect is at VA 0x004762B0 for this Mir3.exe.  The output keeps raw
register pushes because some rectangles are dynamic expressions based on WIL
frame dimensions.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble, immediate

SETRECT_IAT = "0x4762b0"
LEA_RE = re.compile(r"lea\s+([a-z]+),\s*\[([a-z]+)\s*\+\s*(0x[0-9a-f]+|[0-9]+)\]", re.I)


def is_setrect(args: str) -> bool:
    return bool(re.search(r"\[0x4762b0\]", args, re.I))


def is_indirect_setrect(lines: list[dict], index: int) -> bool:
    """Recognize a short-lived register loaded from the SetRect IAT."""
    line = lines[index]
    if line["op"] != "call" or line["args"] not in {"edi", "dword ptr [edi]"}:
        return False
    for item in reversed(lines[max(0, index - 80):index]):
        if item["op"] == "mov" and re.search(r"(?:edi|eax|ecx|edx),\s*dword ptr \[0x4762b0\]", item["args"], re.I):
            return True
        if item["op"] == "call":
            continue
    return False


def extract(lines: list[dict], radius: int = 14) -> list[dict]:
    records = []
    for i, line in enumerate(lines):
        indirect = is_indirect_setrect(lines, i)
        if line["op"] != "call" or (not is_setrect(line["args"]) and not indirect):
            continue
        window = lines[max(0, i - radius):i]
        pushes = [
            {"va": f"0x{item['address']:08x}", "op": item["op"], "args": item["args"], "immediate": immediate(item["args"]) if item["op"] == "push" else None}
            for item in window if item["op"] == "push"
        ]
        leas = []
        for item in window:
            m = LEA_RE.fullmatch(f"{item['op']} {item['args']}")
            if m:
                leas.append({"va": f"0x{item['address']:08x}", "register": m.group(1), "base": m.group(2), "offset": int(m.group(3), 0), "text": item["text"].strip()})
        # A Win32 SetRect call emits bottom, right, top, left, rect in that
        # order. Keep that convention explicit rather than guessing values
        # held in registers.
        args_emitted = pushes[-5:] if len(pushes) >= 5 else pushes
        object_lea = leas[-1] if leas else None
        records.append({
            "source": "Mir3.exe",
            "setrect_iat": SETRECT_IAT,
            "call_va": f"0x{line['address']:08x}",
            "call_mode": "indirect-register" if indirect else "direct-import-call",
            "argument_order_emitted": ["bottom", "right", "top", "left", "rect_ptr"],
            "arguments_emitted": args_emitted,
            "object_lea_candidate": object_lea,
            "raw_neighborhood": [item["text"].strip() for item in window],
            "confidence": "primary-static-SetRect-call",
        })
    return records


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/setrect_calls.json"))
    args = parser.parse_args()
    records = extract(disassemble(args.exe))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "static x86 disassembly; calls through USER32.SetRect IAT 0x004762B0",
        "warning": "Register-held coordinates and object field semantics require further tracing.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"setrect_calls={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
