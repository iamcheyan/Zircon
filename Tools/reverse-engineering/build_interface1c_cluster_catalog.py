#!/usr/bin/env python3
"""Materialize the statically closed Interface1c control cluster at 0x4027."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from wilsdk import WilLibrary


CONTROLS = [
    {"call_va": "0x004027df", "frame": 11, "state_frame": 11, "x": 459, "y": 436, "object_offset": "0xA68",
     "x_push_va": "0x004027cf", "y_push_va": "0x004027ca"},
    {"call_va": "0x00402801", "frame": 13, "state_frame": 13, "x": 139, "y": 379, "object_offset": "0xB1C",
     "x_push_va": "0x004027f1", "y_push_va": "0x004027ec"},
    {"call_va": "0x00402823", "frame": 15, "state_frame": 15, "x": 279, "y": 379, "object_offset": "0xBD0",
     "x_push_va": "0x00402813", "y_push_va": "0x0040280e"},
    {"call_va": "0x00402845", "frame": 17, "state_frame": 17, "x": 439, "y": 379, "object_offset": "0xC84",
     "x_push_va": "0x00402835", "y_push_va": "0x00402830"},
]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data"))
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/interface1c-cluster-4027.json"))
    args = parser.parse_args()
    lib = WilLibrary(str(args.root / "Interface1c.wil"))
    records = []
    for item in CONTROLS:
        header = lib.header(item["frame"])
        if not header:
            raise RuntimeError(f"missing Interface1c Frame {item['frame']}")
        records.append({
            "id": f"interface1c.0x4027.control.{item['call_va']}",
            "kind": "secondary-control",
            "scope": "interface1c-cluster-0x4027",
            "resource": {"file": "Interface1c.wil", "frame": item["frame"], "state_frame": item["state_frame"],
                         "object_expression": "owner+0x5b0"},
            "position": {"x": item["x"], "y": item["y"], "source": "Mir3.exe constructor push arguments",
                         "x_push_va": item["x_push_va"], "y_push_va": item["y_push_va"]},
            "size": {"width": header["width"], "height": header["height"], "source": f"Data/Interface1c.wil Frame {item['frame']} header"},
            "hit_rect": {"x": item["x"], "y": item["y"], "width": header["width"], "height": header["height"],
                         "basis": "0x00417550 SetRect", "evidence_level": "primary-static-plus-primary-resource"},
            "resource_handle": {"file": "Data/Interface1c.wil", "path_literal_va": "0x0047aaa0",
                                 "object_expression": "owner+0x5b0", "load_call_va": "0x0040273e"},
            "evidence": {"level": "primary-static-closed", "source": "Mir3.exe + Interface1c.wil",
                         "warning": "Business control name and parent window remain unresolved."},
        })
    result = {"source": "Mir3.exe + Data/Interface1c.wil", "cluster": "0x004027df-0x00402845",
              "records": records}
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"records={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
