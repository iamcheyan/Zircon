#!/usr/bin/env python3
"""Record cautious visual readings of the original GameInter window frames."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

import wilsdk


VISUALS = {
    200: {"candidate": "equipment-or-character-status", "notes": "Vertical equipment-slot panel; exact status/equipment business role remains unresolved."},
    250: {"candidate": "inventory", "notes": "Grid of item slots with bottom currency/command area."},
    350: {"candidate": "chat-window", "notes": "Wide text panel with bottom channel/action buttons."},
    400: {"candidate": "skill-book", "notes": "Open book with skill-category glyphs on the left and page slots; strong visual candidate for the skill interface."},
    600: {"candidate": "guild-or-social-management", "notes": "Large tabular/list management panel; guild interpretation remains static/visual candidate."},
    700: {"candidate": "quest", "notes": "Scroll/parchment layout with list/content area."},
    750: {"candidate": "system-options", "notes": "Compact options panel with multiple labeled toggles."},
    850: {"candidate": "horse-or-mount", "notes": "Grid panel with mount-related command labels and status bars."},
    900: {"candidate": "group-party", "notes": "Small party/group panel with member-list area."},
    1000: {"candidate": "store-or-npc-shop", "notes": "Item list slots and purchase/currency area; static window role is still store-candidate."},
    1050: {"candidate": "exchange-trade", "notes": "Two-sided item grid and two currency/action areas."},
    1100: {"candidate": "npc-dialogue", "notes": "Wide dialogue/prompt base; special paint routine selects frames 1100–1102."},
}


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--wil", type=Path, default=Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/GameInter.wil"))
    ap.add_argument("--layout", type=Path, default=Path("docs/research/ei-ui-layout/layout.json"))
    ap.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-frame-visual-semantics.json"))
    args = ap.parse_args()
    lib = wilsdk.WilLibrary(str(args.wil))
    records = []
    layout = json.loads(args.layout.read_text(encoding="utf-8")) if args.layout.exists() else {"records": []}
    for record in layout.get("records", []):
        if record.get("kind") != "window":
            continue
        frame = record.get("resource", {}).get("frame")
        if frame not in VISUALS:
            continue
        header = lib.header(frame)
        records.append({
            "window_id": record["id"],
            "frame": frame,
            "header": {k: header[k] for k in ("width", "height", "offsetX", "offsetY")} if header else None,
            "visual": VISUALS[frame],
            "evidence": {
                "level": "secondary-resource-visual-review",
                "source": f"{args.wil} Frame {frame}",
                "warning": "Visual semantics are a cautious aid to static reverse engineering, not runtime proof of the business window name.",
            },
        })
    result = {
        "method": "original GameInter.wil frame decode and visual review",
        "source": str(args.wil),
        "records": records,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"visual_records={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
