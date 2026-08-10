#!/usr/bin/env python3
"""Extract the EI NPC/dialogue-specific paint method at 0x0043F040."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble, immediate


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/npc-paint-evidence.json"))
    args = parser.parse_args()
    lines = disassemble(args.exe)
    body = [x for x in lines if 0x43F040 <= x["address"] < 0x43F44E]
    calls = []
    for i, x in enumerate(body):
        if x["op"] != "call":
            continue
        target = x["args"].split()[0].lower() if x["args"] else ""
        if target not in {"0x466130", "0x460240", "0x4542a0", "0x4542f0"}:
            continue
        before = body[max(0, i - 14):i]
        calls.append({
            "va": f"0x{x['address']:08x}",
            "target": target,
            "immediate_pushes": [immediate(y["args"]) for y in before if y["op"] == "push" and immediate(y["args"]) is not None],
            "field_references": [y["text"].strip() for y in before if "[esi +" in y["args"]],
            "raw_neighborhood": [y["text"].strip() for y in before] + [x["text"].strip()],
        })
    result = {
        "source": str(args.exe),
        "method": "static x86 disassembly of vtable paint candidate 0x0043F040",
        "window_binding": "window.npc-candidate / vtable 0x00476938 candidate",
        "evidence_level": "primary-static-npc-paint-candidate",
        "confirmed_frame_selects": ["0x44c (1100)", "0x44d (1101)", "0x44e (1102)"],
        "confirmed_target_viewport_literals": {"width": 800, "height": 600, "clip": [65535, 65535]},
        "interpretation": [
            "The method selects three consecutive dialogue-related frames from the window resource object.",
            "It calls 0x00460240 for the background/encoded image path and uses this+0x520/0x524, this+0x530/0x534 and this+0x540/0x544 coordinate/data fields.",
            "It loops over a count at this+0x51c and advances a per-entry offset by 0x12 before compositing Frame 1101-like content.",
            "Fallback rendering uses 0x004542A0 and 0x004542F0 with alpha bytes at this+0x580/0x581.",
        ],
        "calls": calls,
        "raw_instructions": [x["text"].strip() for x in body],
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"npc_paint_calls={len(calls)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
