#!/usr/bin/env python3
"""Record the static resource-handle flow used by the EI main UI.

This is deliberately a small evidence extractor rather than a decompiler
claiming full data-flow recovery.  The addresses and expressions below are
anchored in Mir3.exe instructions and are kept verbatim so later work can
replace a derived expression with a stronger proof without losing history.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path


WINDOWS = {
    "window.inventory": {"wrapper": "0x0042ea80", "call": "0x00427750"},
    "window.status": {"wrapper": "0x0044b130", "call": "0x00427776"},
    "window.store-candidate": {"wrapper": "0x0044d310", "call": "0x0042779c"},
    "window.exchange-candidate": {"wrapper": "0x004159d0", "call": "0x004277c2"},
    "window.guild-candidate": {"wrapper": "0x00424e60", "call": "0x004277e8"},
    "window.group": {"wrapper": "0x00424250", "call": "0x00427811"},
    "window.chat-pop": {"wrapper": "0x00414060", "call": "0x00427839"},
    "window.group-pop-candidate": {"wrapper": "0x004503b0", "call": "0x00427862"},
    "window.option": {"wrapper": "0x00440fe0", "call": "0x0042788d"},
    "window.quest": {"wrapper": "0x004473e0", "call": "0x004278b3"},
    "window.horse": {"wrapper": "0x004268c0", "call": "0x004278d9"},
    "window.other-14-candidate": {"wrapper": "0x00439250", "call": "0x00427904"},
    "window.npc-candidate": {"wrapper": "0x0043ed00", "call": "0x0042792a"},
}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--controls", type=Path, default=Path("docs/research/ei-ui-layout/window-control-calls.json"))
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-resource-handle-bindings.json"))
    args = parser.parse_args()

    controls = json.loads(args.controls.read_text(encoding="utf-8"))["records"]
    by_window: dict[str, list[dict]] = {}
    for control in controls:
        by_window.setdefault(control["window_id"], []).append(control)

    records = []
    for window_id, info in WINDOWS.items():
        owned = by_window.get(window_id, [])
        records.append({
            "window_id": window_id,
            "main_init_call_va": info["call"],
            "wrapper_va": info["wrapper"],
            "window_resource_argument": {
                "expression": "[main_ui_this+0x1c]",
                "main_assignment_va": "0x00427611",
                "caller_source_expression": "[main_owner+0xe11e4]+0x5898",
                "argument_register_at_call": "eax/ecx/edx (call-site dependent)",
            },
            "control_resource_arguments": {
                "register": "edi",
                "frame_constructor": "0x00417550",
                "calls": [c["call_va"] for c in owned],
                "basis": "each extracted constructor call pushes edi as resource_arg1",
            },
            "resource_library": {
                "file": "Data/GameInter.wil",
                "path_literal_va": "0x0047ce0c",
                "copied_to_owner_field": "+0xf848",
                "copy_site_va": "0x0045361d",
                "handle_array_base": "+0x5898",
                "load_loop_va": "0x00452ae6",
                "load_count": 70,
            },
            "evidence": {
                "level": "primary-static-handle-flow",
                "source": "Mir3.exe",
                "warning": "The wrapper-boundary and register propagation are static evidence; runtime object identity and every indirect use still require dynamic confirmation.",
            },
        })

    result = {
        "source": "Mir3.exe",
        "method": "static x86 call-site and field-flow tracing",
        "main_ui_resource": {
            "main_init_va": "0x00427600",
            "assignment_va": "0x00427611",
            "expression": "main_ui_this+0x1c = caller_arg+0x5898",
            "loader_owner_field": "+0x5898",
            "wil_path_copy_va": "0x0045361d",
            "wil_path_literal_va": "0x0047ce0c",
            "wil_file": "Data/GameInter.wil",
        },
        "records": records,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"windows={len(records)} controls={sum(len(r['control_resource_arguments']['calls']) for r in records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
