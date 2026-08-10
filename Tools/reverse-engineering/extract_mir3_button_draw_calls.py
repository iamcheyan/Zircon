#!/usr/bin/env python3
"""Record the original EI button draw chain without inventing final coordinates.

The button renderer at VA 0x004179B0 selects the current WIL frame, derives
the frame size, builds transformed rectangles, and calls VA 0x0045F2D0.  This
is intentionally an evidence extractor: it preserves the raw instructions
and records only conclusions directly supported by the binary.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble

BUTTON_RENDER = 0x4179B0
BLIT_CANDIDATE = 0x45F2D0


def in_range(address: int, start: int, end: int) -> bool:
    return start <= address < end


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument(
        "--out",
        type=Path,
        default=Path("docs/research/ei-ui-layout/button-draw-calls.json"),
    )
    args = parser.parse_args()
    lines = disassemble(args.exe)

    renderer = [x for x in lines if in_range(x["address"], BUTTON_RENDER, 0x417C71)]
    callee = [x for x in lines if in_range(x["address"], BLIT_CANDIDATE, 0x45FD3B)]
    renderer_calls = [x for x in renderer if x["op"] == "call"]
    blit_calls = [x for x in renderer if f"0x{BLIT_CANDIDATE:x}" in x["args"].lower()]
    all_blit_sites = []
    for i, line in enumerate(lines):
        if line["op"] != "call" or f"0x{BLIT_CANDIDATE:x}" not in line["args"].lower():
            continue
        before = lines[max(0, i - 10):i]
        all_blit_sites.append({
            "va": f"0x{line['address']:08x}",
            "raw_call": line["text"].strip(),
            "caller_neighborhood": [x["text"].strip() for x in before],
            "common_context_register": "ECX=0x008AB7A8 at most observed call sites",
        })

    result = {
        "source": str(args.exe),
        "method": "static x86 disassembly",
        "evidence_level": "primary-static-draw-candidate",
        "warning": (
            "0x0045F2D0 is a draw/composition candidate, not yet a formally "
            "named API. Final destination coordinates require runtime/API "
            "verification; SetRect calls in 0x004179B0 include transformed "
            "intermediate rectangles."
        ),
        "button_renderer": {
            "va": "0x004179b0",
            "frame_selection": "this+0x08 passed to 0x00466130; resource object is this+0x04",
            "frame_dimensions": "selected resource frame at resource+0x38, signed WORD width/height",
            "scale_fields": ["this+0x0c", "this+0x10"],
            "mode_field": "this+0x48",
            "setrect_iat": "0x004762b0",
            "draw_calls": [
                {
                    "va": f"0x{x['address']:08x}",
                    "target": f"0x{BLIT_CANDIDATE:08x}",
                    "register_this": "0x008ab7a8",
                    "raw": x["text"].strip(),
                    "interpretation": "candidate final image/composition call after source buffer and destination values are prepared",
                }
                for x in blit_calls
            ],
            "call_targets": [x["text"].strip() for x in renderer_calls],
            "raw_instructions": [x["text"].strip() for x in renderer],
        },
        "all_composition_call_sites": all_blit_sites,
        "composition_callee": {
            "va": "0x0045f2d0",
            "observations": [
                "callee uses ECX as an object/context pointer",
                "first visible argument is read from [esp+0x0c] before the local stack frame is allocated",
                "that argument is dereferenced as an image/rectangle-like structure",
                "callee reads fields at +0, +4, +8, +0xc and computes clipped widths/heights",
                "callee reads context fields +0x1c, +0x58 and +0x5a and performs pixel-buffer work",
                "callee contains a virtual call through [object+0x64], consistent with a graphics/composition backend",
            ],
            "raw_instructions": [x["text"].strip() for x in callee[:110]],
            "confidence_boundary": "The function is strongly supported as a graphics/composition routine, but its exact calling convention and backend surface still require runtime or caller-wide cross-checking.",
        },
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"renderer_instructions={len(renderer)}")
    print(f"renderer_draw_calls={len(blit_calls)}")
    print(f"callee_instructions={len(callee)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
