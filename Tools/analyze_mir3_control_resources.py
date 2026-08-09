#!/usr/bin/env python3
"""Compare window-control Frame candidates across EI UI WIL libraries."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from wilsdk import WilLibrary  # noqa: E402


DEFAULT_DATA = Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data")


def frame_info(lib, frame):
    h = lib.header(frame)
    if not h:
        return None
    return {"width": h["width"], "height": h["height"], "bbox": list(lib.decode(frame).getbbox() or ())}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--data", type=Path, default=DEFAULT_DATA)
    parser.add_argument("--controls", type=Path, default=Path("docs/research/ei-ui-layout/window-control-calls.json"))
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-control-resource-analysis.json"))
    args = parser.parse_args()

    libs = {}
    for name in ("GameInter.wil", "Interface1c.wil"):
        path = args.data / name
        if path.exists():
            libs[name] = WilLibrary(str(path))
    records = []
    for control in json.loads(args.controls.read_text(encoding="utf-8"))["records"]:
        pairs = control["frame_pair_candidates"]
        pair = pairs[-1] if pairs else []
        frames = []
        for frame in pair:
            frames.append({"frame": frame, "libraries": {name: frame_info(lib, frame) for name, lib in libs.items()}})
        records.append({
            "window_id": control["window_id"],
            "wrapper_va": control["wrapper_va"],
            "call_va": control["call_va"],
            "frame_pair": pair,
            "frames": frames,
            "evidence": {"level": "primary-static-plus-primary-resource", "source": str(args.data)},
        })
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.data),
        "method": "window-control Frame pairs cross-checked against GameInter.wil and Interface1c.wil",
        "warning": "Availability in a library does not prove which resource object the wrapper passes; object-handle tracing is still required.",
        "libraries": sorted(libs),
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"analyzed={len(records)} libraries={','.join(sorted(libs))}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
