#!/usr/bin/env python3
"""Extract shared image-composition calls from known EI window wrappers.

This is a static call-graph/evidence extractor.  It does not claim that every
call is a complete window paint routine: wrappers may call helpers, and the
shared compositor's exact ABI is still being recovered.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble, immediate

WINDOWS = {
    0x42EA80: "window.inventory",
    0x44B130: "window.status",
    0x44D310: "window.store-candidate",
    0x4159D0: "window.exchange-candidate",
    0x424E60: "window.guild-candidate",
    0x424250: "window.group",
    0x414060: "window.chat-pop",
    0x4503B0: "window.group-pop-candidate",
    0x440FE0: "window.option",
    0x4473E0: "window.quest",
    0x4268C0: "window.horse",
    0x439250: "window.other-14-candidate",
    0x43ED00: "window.npc-candidate",
}
COMPOSITOR = 0x45F2D0
FRAME_SELECT = 0x466130
CONTROL_INIT = 0x417550


def is_target(line: dict, target: int) -> bool:
    return line["op"] == "call" and re.search(rf"0x{target:x}(?:\s|$)", line["args"], re.I) is not None


def extract(lines: list[dict]) -> list[dict]:
    by_addr = {x["address"]: i for i, x in enumerate(lines)}
    records = []
    for start, window_id in WINDOWS.items():
        if start not in by_addr:
            continue
        begin = by_addr[start]
        end = len(lines)
        for i in range(begin + 1, len(lines)):
            if lines[i]["op"] == "ret":
                end = i + 1
                break
        body = lines[begin:end]
        calls = []
        for i, line in enumerate(body):
            if not is_target(line, COMPOSITOR):
                continue
            before = body[max(0, i - 12):i]
            pushes = []
            for x in before:
                if x["op"] == "push":
                    pushes.append({
                        "va": f"0x{x['address']:08x}",
                        "args": x["args"],
                        "immediate": immediate(x["args"]),
                    })
            calls.append({
                "call_va": f"0x{line['address']:08x}",
                "sequence": len(calls),
                "frame_select_calls_nearby": [x["text"].strip() for x in before if is_target(x, FRAME_SELECT)],
                "control_init_calls_nearby": [x["text"].strip() for x in before if is_target(x, CONTROL_INIT)],
                "pushes_nearby": pushes,
                "immediate_values_nearby": [x["immediate"] for x in pushes if x["immediate"] is not None],
                "raw_neighborhood": [x["text"].strip() for x in before] + [line["text"].strip()],
                "evidence": "primary-static-shared-compositor-call",
            })
        records.append({
            "window_id": window_id,
            "wrapper_va": f"0x{start:08x}",
            "wrapper_role": "constructor/control initialization wrapper; not itself proven to be a paint method",
            "wrapper_range_end": f"0x{body[-1]['address']:08x}" if body else None,
            "compositor_va": f"0x{COMPOSITOR:08x}",
            "frame_selector_va": f"0x{FRAME_SELECT:08x}",
            "shared_compositor_calls": calls,
            "call_count": len(calls),
            "evidence": {
                "level": "primary-static-call-graph",
                "source": "Mir3.exe",
                "warning": "Wrapper range ends at first RET; helper calls and indirect drawing remain unresolved.",
            },
        })
    return records


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-draw-calls.json"))
    args = parser.parse_args()
    records = extract(disassemble(args.exe))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "known window wrapper body to first RET; direct calls to 0x0045F2D0",
        "evidence_level": "primary-static-call-graph",
        "warning": "This is not yet a final paint order. Indirect calls and runtime clipping/state branches require further analysis.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"window_wrappers={len(records)}")
    print(f"wrappers_with_compositor={sum(bool(x['call_count']) for x in records)}")
    print(f"compositor_calls={sum(x['call_count'] for x in records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
