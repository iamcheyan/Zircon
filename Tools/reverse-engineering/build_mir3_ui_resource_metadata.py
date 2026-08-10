#!/usr/bin/env python3
"""Record dimensions of the primary GameInter.wil frames used by UI evidence."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from wilsdk import WilLibrary  # noqa: E402

DEFAULT_WIL = Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/GameInter.wil")
FRAME_GROUPS = {
    "main_hud": [50],
    "hud_belt": [52, 53],
    "hud_stats": [60, 61, 63, 67],
    "hud_buttons": list(range(80, 86)) + list(range(90, 98)) + list(range(100, 116)),
}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--wil", type=Path, default=DEFAULT_WIL)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/gameinter-frame-metadata.json"))
    args = parser.parse_args()
    lib = WilLibrary(str(args.wil))
    frames = {}
    for group, indexes in FRAME_GROUPS.items():
        for index in indexes:
            header = lib.header(index)
            frames[str(index)] = {
                "frame": index,
                "groups": sorted(set(frames.get(str(index), {}).get("groups", []) + [group])),
                "width": header.get("width") if header else None,
                "height": header.get("height") if header else None,
                "header": header,
                "evidence": {"level": "primary-resource", "source": str(args.wil)},
            }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.wil),
        "library_count": lib.count,
        "frames": frames,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"frames={len(frames)} library_count={lib.count}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
