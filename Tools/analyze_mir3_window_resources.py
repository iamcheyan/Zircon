#!/usr/bin/env python3
"""Compare EI window constructor rectangles with decoded WIL alpha bounds."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from wilsdk import WilLibrary  # noqa: E402


DEFAULT_WIL = Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/GameInter.wil")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--wil", type=Path, default=DEFAULT_WIL)
    parser.add_argument("--windows", type=Path, default=Path("docs/research/ei-ui-layout/window_layout.json"))
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-resource-analysis.json"))
    args = parser.parse_args()

    lib = WilLibrary(str(args.wil))
    records = []
    for item in json.loads(args.windows.read_text(encoding="utf-8"))["records"]:
        frame = int(item["resource"]["frame"])
        hdr = lib.header(frame)
        image = lib.decode(frame) if hdr else None
        bbox = image.getbbox() if image else None
        window = item["window"]
        visible = None
        delta = None
        if bbox:
            visible = {
                "left": bbox[0], "top": bbox[1], "right": bbox[2], "bottom": bbox[3],
                "width": bbox[2] - bbox[0], "height": bbox[3] - bbox[1],
            }
            delta = {
                "width": window["width"] - visible["width"],
                "height": window["height"] - visible["height"],
            }
        records.append({
            "id": item["id"],
            "frame": frame,
            "resource": {"file": lib.name, "header": hdr, "visible_bbox": visible},
            "constructor_window": {
                "x": window["x"], "y": window["y"],
                "width": window["width"], "height": window["height"],
            },
            "constructor_minus_visible_bbox": delta,
            "evidence": {
                "level": "primary-resource-plus-primary-static",
                "source": f"{args.wil} + window_layout.json",
                "notes": "Decoded alpha bbox is an observed resource property; matching dimensions do not by themselves prove the draw origin.",
            },
        })

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.wil),
        "method": "decode WIL frames and compare non-transparent RGBA bbox with EI constructor dimensions",
        "warning": "This is a correlation report. Draw origin, clipping and window hit-test semantics still require call-site tracing.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"analyzed={len(records)}")
    for r in records:
        print(r["id"], "frame", r["frame"], "bbox", r["resource"]["visible_bbox"], "delta", r["constructor_minus_visible_bbox"])
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
