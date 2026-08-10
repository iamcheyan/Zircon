#!/usr/bin/env python3
"""Materialize the Interface1c control cluster initialized near 0x456CB0."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from wilsdk import WilLibrary


# (call VA, normal frame, state frame, x, y, object offset)
CONTROLS = [
    ("0x00456dc1", 51, 51, 0x1B8, 0x5D, "0x9E8"),
    ("0x00456de0", 53, 53, 0x4F, 0xF3, "0xA9C"),
    ("0x00456dff", 55, 55, 0x103, 0x31, "0xB50"),
    ("0x00456e1e", 57, 57, 0x1C, 0x1B6, "0xC04"),
    ("0x00456e40", 92, 93, 0x10A, 0x1A3, "0xD38"),
    ("0x00456e62", 95, 96, 0x134, 0x1A3, "0xDEC"),
    ("0x00456e84", 98, 98, 0x160, 0x1A3, "0xEA0"),
    ("0x00456ea6", 86, 87, 0x1C2, 0x1BC, "0xF54"),
    ("0x00456ec8", 89, 90, 0x1EB, 0x1BC, "0x1008"),
]


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data"))
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/interface1c-cluster-456d.json"))
    args = parser.parse_args()
    lib = WilLibrary(str(args.root / "Interface1c.wil"))
    records = []
    for call_va, frame, state_frame, x, y, object_offset in CONTROLS:
        header = lib.header(frame)
        if not header:
            raise RuntimeError(f"missing Interface1c Frame {frame}")
        records.append({
            "id": f"interface1c.0x456d.control.{call_va}",
            "kind": "secondary-control",
            "scope": "interface1c-cluster-0x456d",
            "resource": {"file": "Interface1c.wil", "frame": frame, "state_frame": state_frame,
                         "object_expression": "owner+0x14c"},
            "position": {"x": x, "y": y, "source": "Mir3.exe constructor push arguments"},
            "size": {"width": header["width"], "height": header["height"], "source": f"Data/Interface1c.wil Frame {frame} header"},
            "hit_rect": {"x": x, "y": y, "width": header["width"], "height": header["height"],
                         "basis": "0x00417550 SetRect", "evidence_level": "primary-static-plus-primary-resource"},
            "resource_handle": {"file": "Data/Interface1c.wil", "path_literal_va": "0x0047aaa0",
                                 "object_expression": "owner+0x14c", "load_call_va": "0x00456cd6"},
            "object_offset": object_offset,
            "evidence": {"level": "primary-static-closed", "source": "Mir3.exe + Interface1c.wil",
                         "warning": "Business control name, parent window, and draw layer remain unresolved."},
        })
    result = {"source": "Mir3.exe + Data/Interface1c.wil", "cluster": "0x00456dc1-0x00456ec8", "records": records}
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"records={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
