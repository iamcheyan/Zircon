#!/usr/bin/env python3
"""Recover the skill-book window's static text/resource context."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE


TEXTS = {
    "0x0047c330": ("火", "fire"),
    "0x0047c32c": ("冰", "ice"),
    "0x0047c328": ("电", "lightning"),
    "0x0047c324": ("风", "wind"),
    "0x0047c31c": ("神圣", "holy"),
    "0x0047c314": ("黑暗", "dark"),
    "0x0047c30c": ("幻影", "illusion"),
    "0x0047c308": ("剑", "sword"),
}

LABEL_CALLS = {
    "0x0047c330": "0x00439334",
    "0x0047c32c": "0x0043935d",
    "0x0047c328": "0x00439386",
    "0x0047c324": "0x004393b3",
    "0x0047c31c": "0x004393e0",
    "0x0047c314": "0x0043940d",
    "0x0047c30c": "0x00439437",
    "0x0047c308": "0x00439464",
}


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    ap.add_argument("--layout", type=Path, default=Path("docs/research/ei-ui-layout/layout.json"))
    ap.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/skill-window-context.json"))
    args = ap.parse_args()
    layout = json.loads(args.layout.read_text(encoding="utf-8"))
    controls = [r for r in layout.get("control_constructors", []) if r.get("window_id") == "window.other-14-candidate"]
    by_call = {r["call_va"].lower(): r for r in controls}
    labels = []
    for va, (text, key) in TEXTS.items():
        call_va = LABEL_CALLS[va]
        control = by_call.get(call_va.lower())
        labels.append({
            "literal_va": va,
            "text": text,
            "category_key": key,
            "control_call_va": call_va,
            "frame_pair": control.get("resource", {}).get("frame_pair") if control else None,
            "position": control.get("position") if control else None,
            "evidence": {"level": "primary-static-text-control-correlation", "source": "Mir3.exe GB18030 literal + constructor call"},
        })
    result = {
        "window_id": "window.other-14-candidate",
        "wrapper_va": "0x00439250",
        "main_init_call_va": "0x00427904",
        "window_frame": 400,
        "window_size": {"width": 296, "height": 332},
        # The executable literal is the bare filename `Magic.exp`; unlike WIL
        # libraries it is loaded from the client root in the supplied EI
        # package.  Keep this distinct from Mud3/Envir/magic.dat.
        "resources": [{"file": "Data/GameInter.wil", "frame": 400}, {"file": "Magic.exp", "literal_va": "0x0047c2fc", "client_root_candidate": "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Magic.exp", "encrypted_or_encoded": True}],
        "category_labels": labels,
        "skill_control_records": controls,
        "interpretation": {
            "candidate": "skill-book-with-element-or-school-tabs",
            "confidence": "primary-static-plus-visual-candidate",
            "warning": "The labels identify category text passed during construction; exact skill-list paging and runtime selection still require input-path tracing.",
        },
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"labels={len(labels)} controls={len(controls)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
