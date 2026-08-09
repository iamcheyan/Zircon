#!/usr/bin/env python3
"""Extract calls to EI's resource/frame selection helper.

0x00466130 is the helper repeatedly called as ``resource->SetIndex(frame)``
by the UI constructors.  The exact class name is not assumed here; the tool
preserves the nearby object register and raw pushes so later draw-call tracing
can distinguish window setup from ordinary game sprites.
"""
from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble, immediate

TARGET = "0x466130"
UI_FRAMES = set(range(50, 116)) | {145, 169, 200, 201, 202, 250, 251, 253, 254, 255,
                                  270, 280, 281, 300, 350, 400, 600, 700, 750, 850, 900,
                                  1000, 1050, 1100}


def is_target(args: str) -> bool:
    return bool(re.search(r"0x466130(?:\s|$)", args, re.I))


def extract(lines: list[dict], radius: int = 12) -> list[dict]:
    records = []
    for i, line in enumerate(lines):
        if line["op"] != "call" or not is_target(line["args"]):
            continue
        window = lines[max(0, i - radius):i]
        pushes = []
        for item in window:
            if item["op"] == "push":
                value = immediate(item["args"])
                pushes.append({"va": f"0x{item['address']:08x}", "args": item["args"], "immediate": value})
        frame = pushes[-1]["immediate"] if pushes else None
        ecx_setup = [item["text"].strip() for item in window if re.search(r"\bmov\s+ecx,", item["text"], re.I)]
        records.append({
            "source": "Mir3.exe",
            "helper_va": TARGET,
            "call_va": f"0x{line['address']:08x}",
            "frame_candidate": frame,
            "known_ui_frame_candidate": frame in UI_FRAMES if frame is not None else False,
            "ecx_setup": ecx_setup[-3:],
            "preceding_pushes": pushes,
            "raw_neighborhood": [item["text"].strip() for item in window],
            "confidence": "primary-static-resource-select-candidate",
        })
    return records


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/resource_select_calls.json"))
    args = parser.parse_args()
    records = extract(disassemble(args.exe))
    counts = Counter(r["frame_candidate"] for r in records if r["known_ui_frame_candidate"])
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "llvm-objdump x86 disassembly; direct calls to VA 0x00466130",
        "warning": "The last immediate push is treated as a frame candidate only when the helper's one-argument calling convention is confirmed; all records retain raw pushes.",
        "ui_frame_frequency": {str(k): v for k, v in sorted(counts.items(), key=lambda kv: (kv[0] is None, kv[0]))},
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"resource_select_calls={len(records)}")
    print(f"ui_frame_candidates={sum(1 for r in records if r['known_ui_frame_candidate'])}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
