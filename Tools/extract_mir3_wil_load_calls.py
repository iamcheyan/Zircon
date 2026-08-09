#!/usr/bin/env python3
"""Extract WIL/resource-object load calls from the EI client."""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble, immediate

LOAD = "0x4660e0"
KNOWN_PATHS = {
    0x47AAA0: r".\Data\Interface1c.wil",
    0x47AAB8: r".\Data\gameinter.wil",
    0x47AAC0: r".\Data\gameinter.wil",
    0x47CE10: r".\Data\GameInter.wil",
}


def extract(lines: list[dict], radius: int = 16) -> list[dict]:
    records = []
    for i, line in enumerate(lines):
        if line["op"] != "call" or not re.search(r"0x4660e0(?:\s|$)", line["args"], re.I):
            continue
        window = lines[max(0, i - radius):i]
        pushes = []
        for item in window:
            if item["op"] == "push":
                value = immediate(item["args"])
                pushes.append({"va": f"0x{item['address']:08x}", "args": item["args"], "immediate": value,
                               "known_path": KNOWN_PATHS.get(value) if value is not None else None})
        object_lea = None
        for item in reversed(window):
            if item["op"] == "lea" and re.search(r"\becx,", item["args"], re.I):
                object_lea = item["text"].strip()
                break
        records.append({
            "source": "Mir3.exe",
            "load_helper_va": LOAD,
            "call_va": f"0x{line['address']:08x}",
            "object_lea_candidate": object_lea,
            "path_push_candidates": [p for p in pushes if p["known_path"]],
            "preceding_pushes": pushes,
            "raw_neighborhood": [item["text"].strip() for item in window],
            "evidence": {"level": "primary-static", "notes": "Path labels are based on PE .rdata string VA; object ownership still requires constructor tracing."},
        })
    return records


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/wil_load_calls.json"))
    args = parser.parse_args()
    records = extract(disassemble(args.exe))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "llvm-objdump x86 disassembly; direct calls to 0x004660E0",
        "warning": "Only calls carrying a recognized .rdata path are labeled; resource handle propagation remains to be traced.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"wil_load_calls={len(records)}")
    print(f"with_known_path={sum(bool(r['path_push_candidates']) for r in records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
