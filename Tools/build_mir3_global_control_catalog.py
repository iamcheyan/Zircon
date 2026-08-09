#!/usr/bin/env python3
"""Classify every direct 0x00417550 call without discarding unassigned UI.

The main-window extractor intentionally has a narrow scope.  This companion
catalog preserves the wider binary scan so controls belonging to secondary
windows can be bound later instead of disappearing from the evidence set.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--all", type=Path, default=Path("docs/research/ei-ui-layout/button_constructor_calls.json"))
    parser.add_argument("--known", type=Path, default=Path("docs/research/ei-ui-layout/window-control-calls.json"))
    parser.add_argument("--layout", type=Path, default=Path("docs/research/ei-ui-layout/layout.json"))
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/global-control-constructor-catalog.json"))
    args = parser.parse_args()

    all_records = json.loads(args.all.read_text(encoding="utf-8"))["records"]
    known = json.loads(args.known.read_text(encoding="utf-8"))["records"]
    known_by_call = {r["call_va"]: r for r in known}
    layout = json.loads(args.layout.read_text(encoding="utf-8"))
    hud_calls = {
        address.lower()
        for r in layout.get("records", [])
        if r.get("kind") == "button"
        for address in r.get("evidence", {}).get("addresses", [])
    }

    records = []
    for item in all_records:
        call_va = item["call_va"]
        known_item = known_by_call.get(call_va)
        if known_item:
            classification = "main-window-control"
            owner = known_item["window_id"]
        elif call_va.lower() in hud_calls:
            classification = "main-hud-control"
            owner = "hud"
        else:
            classification = "unassigned-control-candidate"
            owner = None
        records.append({
            "id": f"control.{call_va}",
            "call_va": call_va,
            "classification": classification,
            "owner": owner,
            "frame_pair_candidates": item.get("frame_pair_candidates", []),
            "object_lea_near_call": item.get("object_lea_near_call"),
            "position_register_pushes_near_call": item.get("position_register_pushes_near_call", []),
            "raw_neighborhood": item.get("raw_neighborhood", []),
            "evidence": {
                "level": item.get("confidence", "primary-call-unknown-control"),
                "source": "Mir3.exe",
                "warning": "Unassigned controls require wrapper/function ownership and resource-handle tracing before coordinates are promoted.",
            },
        })

    result = {
        "source": "Mir3.exe",
        "method": "all direct-call records from button_constructor_calls.json classified against current HUD/window catalogs",
        "counts": {
            "all": len(records),
            "main_window": sum(r["classification"] == "main-window-control" for r in records),
            "main_hud": sum(r["classification"] == "main-hud-control" for r in records),
            "unassigned": sum(r["classification"] == "unassigned-control-candidate" for r in records),
        },
        "records": records,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(result["counts"])
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
