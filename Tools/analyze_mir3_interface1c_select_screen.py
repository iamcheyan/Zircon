#!/usr/bin/env python3
"""Recover the character-selection Interface1c screen context."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

import wilsdk
from extract_mir3_ui_layout import DEFAULT_EXE, disassemble


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    ap.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/interface1c-select-screen-context.json"))
    args = ap.parse_args()
    lines = disassemble(args.exe)
    callers = []
    for i, line in enumerate(lines):
        if line["op"] == "call" and "0x4026e0" in line["args"].lower():
            callers.append({"call_va": f"0x{line['address']:08x}", "neighborhood": [x["text"].strip() for x in lines[max(0, i - 14):i + 1]]})
    lib = wilsdk.WilLibrary("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/Interface1c.wil")
    frames = {}
    for i in [11, 13, 15, 17, 50]:
        h = lib.header(i)
        if h:
            frames[str(i)] = {"width": h["width"], "height": h["height"], "offsetX": h["offsetX"], "offsetY": h["offsetY"]}
    result = {
        "source": str(args.exe),
        "constructor_va": "0x004026e0",
        "resource_objects": [
            {"expression": "owner+0x46c", "path": "Data/gameinter.wil", "load_va": "0x0040272a"},
            {"expression": "owner+0x5b0", "path": "Data/Interface1c.wil", "load_va": "0x0040273e"},
        ],
        "callers": callers,
        "interface1c_frames": frames,
        "controls": [
            {"frame": 11, "state_frame": 11, "position": {"x": 459, "y": 436}, "size": {"width": 96, "height": 24}},
            {"frame": 13, "state_frame": 13, "position": {"x": 139, "y": 379}, "size": {"width": 96, "height": 26}},
            {"frame": 15, "state_frame": 15, "position": {"x": 279, "y": 379}, "size": {"width": 96, "height": 26}},
            {"frame": 17, "state_frame": 17, "position": {"x": 439, "y": 379}, "size": {"width": 48, "height": 26}},
        ],
        "static_interpretation": {
            "candidate": "character-selection-screen",
            "basis": [
                "The four Interface1c labels visually correspond to the original character-selection button family.",
                "The constructor is called during the client startup sequence at 0x004020A8.",
                "The controls have fixed coordinates in the 640x480-era UI coordinate space.",
            ],
            "confidence": "candidate-with-visual-and-static-support",
            "warning": "Exact Chinese text and state-machine transition still require runtime confirmation.",
        },
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"callers={len(callers)} frames={len(frames)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
