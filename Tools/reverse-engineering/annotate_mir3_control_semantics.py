#!/usr/bin/env python3
"""Add cautious semantic group labels to decoded main-window controls."""
from __future__ import annotations

import argparse
import json
from pathlib import Path


def semantic(window_id: str, pair: list[int]) -> dict | None:
    a = pair[0] if pair else None
    if window_id == "window.other-14-candidate":
        if a in {410, 412, 414, 416, 418, 420, 422, 424, 426, 428, 430, 432, 434, 436, 438, 440, 442, 444, 446, 448, 450, 452, 454, 456, 458}:
            return {"group": "skill-book-category-or-skill-slot", "notes": "Frame 400 is a book; these repeated small paired frames form its category/slot controls."}
    if window_id == "window.horse" and a in {860, 862, 864, 866}:
        return {"group": "mount-command", "notes": "Four repeated command buttons along the mount panel bottom."}
    if window_id == "window.quest" and a in {721, 723}:
        return {"group": "quest-page-navigation", "notes": "Two small controls on the quest scroll."}
    if window_id == "window.option" and a in {760, 762}:
        return {"group": "option-toggle-or-value", "notes": "Repeated paired controls aligned to option rows."}
    if window_id == "window.chat-pop" and a in {360, 362, 364, 366, 368, 370}:
        return {"group": "chat-channel-or-action", "notes": "Six repeated bottom chat controls."}
    if window_id == "window.npc-candidate" and a in {52, 54}:
        return {"group": "npc-dialogue-choice-or-marker", "notes": "Small repeated Interface-style markers inside NPC dialogue area."}
    if window_id == "window.group" and a in {910, 912, 914, 920}:
        return {"group": "party-action", "notes": "Bottom/center party panel action controls."}
    if window_id == "window.store-candidate" and a in {1010, 1012, 1014, 1016}:
        return {"group": "shop-navigation-or-item-action", "notes": "Repeated store panel buttons; exact action names pending text/input trace."}
    if window_id == "window.exchange-candidate" and a in {1061, 1064}:
        return {"group": "trade-action", "notes": "Trade panel action controls."}
    if window_id == "window.guild-candidate" and a in {610, 612, 614, 616, 618, 620, 622, 624}:
        return {"group": "guild-tab-or-list-action", "notes": "Repeated guild/social panel controls; exact tab names pending."}
    if window_id in {"window.inventory", "window.status", "window.group-pop-candidate"} and a in {161}:
        return {"group": "window-close", "notes": "Shared close-button frame pair used across windows."}
    return None


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--layout", type=Path, default=Path("docs/research/ei-ui-layout/layout.json"))
    ap.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/control-semantic-catalog.json"))
    args = ap.parse_args()
    layout = json.loads(args.layout.read_text(encoding="utf-8"))
    records = []
    for control in layout.get("control_constructors", []):
        pair = control.get("resource", {}).get("frame_pair", [])
        value = semantic(control.get("window_id", ""), pair)
        if not value:
            continue
        records.append({
            "control_id": control["id"],
            "window_id": control["window_id"],
            "call_va": control["call_va"],
            "frame_pair": pair,
            "semantic": value,
            "evidence": {"level": "secondary-frame-pattern-review", "warning": "Group label is not a final business name; confirm with text/input/runtime path."},
        })
    result = {"method": "frame-pair repetition + original window-frame visual review", "records": records}
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"semantic_records={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
