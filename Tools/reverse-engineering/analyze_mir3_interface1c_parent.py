#!/usr/bin/env python3
"""Document the parent context of the closed Interface1c control cluster."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

import wilsdk
from extract_mir3_ui_layout import DEFAULT_EXE, disassemble


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    ap.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/interface1c-parent-context.json"))
    args = ap.parse_args()
    lines = disassemble(args.exe)
    callers = []
    for i, line in enumerate(lines):
        if line["op"] == "call" and "0x456cb0" in line["args"].lower():
            neighborhood = [x["text"].strip() for x in lines[max(0, i - 12):i + 1]]
            callers.append({"call_va": f"0x{line['address']:08x}", "neighborhood": neighborhood})
    lib = wilsdk.WilLibrary("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/Interface1c.wil")
    frames = {}
    for i in [50, 51, 53, 55, 57, 86, 87, 89, 90, 92, 93, 95, 96, 98, 99]:
        h = lib.header(i)
        if h:
            frames[str(i)] = {"width": h["width"], "height": h["height"], "offsetX": h["offsetX"], "offsetY": h["offsetY"]}
    result = {
        "source": str(args.exe),
        "constructor_va": "0x00456cb0",
        "object_initializer_va": "0x00456a90",
        "resource_objects": [
            {"expression": "owner+0x8", "path": "Data/gameinter.wil", "load_va": "0x00456cc4"},
            {"expression": "owner+0x14c", "path": "Data/Interface1c.wil", "load_va": "0x00456cd8"},
        ],
        "callers": callers,
        "interface1c_frames": frames,
        "static_interpretation": {
            "candidate": "character-selection-or-character-creation-screen",
            "basis": [
                "Interface1c Frame 50 is a 640x480 full-screen background candidate.",
                "The same initializer constructs nine Interface1c text-button pairs at fixed 640x480 coordinates.",
                "The surrounding global state transitions call 0x00456CB0 with ECX=0x008A7140.",
            ],
            "confidence": "candidate-not-runtime-confirmed",
            "warning": "The Chinese button labels and exact screen state still require runtime/input-path confirmation; do not rename this as final UI business truth.",
        },
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"callers={len(callers)} frames={len(frames)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
