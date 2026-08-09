#!/usr/bin/env python3
"""Record the extra GameInter window/control cluster initialized at 0x43E260."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from wilsdk import WilLibrary


CONTROLS = [
    {"call_va": "0x0043e2bb", "frame": 161, "state_frame": 162, "x": 655, "y": 16, "object_offset": "0x54",
     "expression": "x=window_arg4+0x224; y=window_arg8+0x10"},
    {"call_va": "0x0043e2e4", "frame": 606, "state_frame": 607, "x": 603, "y": 27, "object_offset": "0x108",
     "expression": "x=window_arg4+0x224+0x1f0; y=window_arg8+0x10+0x1b"},
]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data"))
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/gameinter-cluster-43e260.json"))
    args = parser.parse_args()
    lib = WilLibrary(str(args.root / "GameInter.wil"))
    records = []
    for item in CONTROLS:
        header = lib.header(item["frame"])
        if not header:
            raise RuntimeError(f"missing GameInter Frame {item['frame']}")
        records.append({
            "id": f"gameinter.0x43e260.control.{item['call_va']}",
            "kind": "secondary-control",
            "scope": "gameinter-cluster-0x43e260",
            "resource": {"file": "GameInter.wil", "frame": item["frame"], "state_frame": item["state_frame"],
                         "object_expression": "main_ui_this+0x1c"},
            "position": {"x": item["x"], "y": item["y"], "source": "Mir3.exe push-time expression",
                         "expression": item["expression"]},
            "size": {"width": header["width"], "height": header["height"], "source": f"Data/GameInter.wil Frame {item['frame']} header"},
            "hit_rect": {"x": item["x"], "y": item["y"], "width": header["width"], "height": header["height"],
                         "basis": "0x00417550 SetRect", "evidence_level": "primary-static-expression-plus-primary-resource"},
            "object_offset": item["object_offset"],
            "evidence": {"level": "primary-static-candidate", "source": "Mir3.exe + GameInter.wil",
                         "warning": "Parent window business name and exact window-origin semantics remain unresolved."},
        })
    result = {
        "source": "Mir3.exe + Data/GameInter.wil",
        "window_candidate": {"constructor_va": "0x0043e260", "main_call_va": "0x0042797e",
                             "frame": 602, "resource": "main_ui_this+0x1c",
                             "call_argument_note": "Main initializer pushes 15, resource, 602, 107, 110, 584, 252, 0, 3; semantic slot names remain under review."},
        "records": records,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"records={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
