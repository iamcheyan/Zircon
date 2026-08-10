#!/usr/bin/env python3
"""Extract the shared EI window background draw routine around 0x00423D00."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-base-draw-evidence.json"))
    args = parser.parse_args()
    lines = disassemble(args.exe)
    body = [x for x in lines if 0x423D00 <= x["address"] < 0x423E71]
    result = {
        "source": str(args.exe),
        "method": "static x86 disassembly",
        "evidence_level": "primary-static-window-paint-candidate",
        "routine": {
            "va": "0x00423d00",
            "likely_role": "shared/base window background paint",
            "entry_state_field": "this+0x30 tested for a valid window/resource state",
            "resource_object": "this+0x2c",
            "frame_or_position_fields": ["this+0x08", "this+0x0c", "this+0x10", "this+0x14"],
            "global_render_context_test": "0x008b1874",
            "direct_calls": [
                {
                    "va": "0x00423d62",
                    "target": "0x00460240",
                    "branch": "global render context nonzero",
                    "literal_dimensions": {"width": 800, "height": 600},
                    "clip_literals": [65535, 65535],
                    "argument_observation": "resource selected frame buffer and frame dimensions are pushed before the 800x600 literals; exact ABI remains pending",
                },
                {
                    "va": "0x00423dfa",
                    "target": "0x004542a0",
                    "branch": "fallback/transformed rendering",
                    "argument_observation": "uses position/size-derived floating-point values and a context at ECX=0x005600fc",
                },
                {
                    "va": "0x00423e66",
                    "target": "0x004542f0",
                    "branch": "fallback/transformed rendering",
                    "argument_observation": "uses alpha/color bytes this+0x50/0x51 and transformed rectangles",
                },
            ],
            "callee_observations": {
                "0x00460240": [
                    "allocates a large local work area and clips source/destination bounds",
                    "reads a source/object buffer through context+0x1c and invokes a virtual method at object vtable+0x64",
                    "contains explicit pixel-buffer loops and recognizes 0xc0/0xc1/0xc2/0xc3 word markers",
                    "therefore behaves like a transparent/encoded image blit or decode-to-target routine, not merely a rectangle constructor",
                ],
                "0x004542A0": [
                    "validates two small integer indices and dispatches to 0x00454DA0 with context offset 0x13f60",
                ],
                "0x004542F0": [
                    "performs float coordinate transforms and several virtual graphics-context calls",
                    "its exact backend surface and final target remain unresolved",
                ],
            },
            "raw_instructions": [x["text"].strip() for x in body],
        },
        "interpretation_boundary": [
            "800x600 literals are primary binary evidence that this branch targets the fixed client viewport.",
            "The routine is a shared base paint candidate; it does not by itself identify every derived window's child-control paint order.",
            "0x00460240, 0x004542A0 and 0x004542F0 need callee analysis/runtime verification before being assigned exact API names.",
        ],
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("base_window_direct_calls=3")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
