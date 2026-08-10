#!/usr/bin/env python3
"""Bind the known main-window wrapper calls to nearby derived vtable writes.

The EI main UI calls one wrapper per window.  Each wrapper is preceded in the
same object-class code cluster by a derived constructor writing a non-base
vtable, then later restores the common base vtable during cleanup/destruction.
This tool records the nearest preceding derived assignment as a *static
binding candidate* and keeps the distance/raw context so it can be audited.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble
from extract_mir3_window_controls import WINDOWS


BASE_VTABLE = 0x476624


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-vtable-bindings.json"))
    args = parser.parse_args()
    lines = disassemble(args.exe)
    by_addr = {x["address"]: i for i, x in enumerate(lines)}
    writes = []
    for i, line in enumerate(lines):
        if line["op"] != "mov" or "dword ptr [esi]" not in line["args"]:
            continue
        if not line["args"].strip().startswith("dword ptr [esi],"):
            continue
        try:
            value = int(line["args"].split(",", 1)[1].strip(), 0)
        except ValueError:
            continue
        if not 0x476600 <= value < 0x476c10 or value == BASE_VTABLE:
            continue
        writes.append({"index": i, "va": line["address"], "vtable": value})
    records = []
    for wrapper, window_id in WINDOWS.items():
        if wrapper not in by_addr:
            continue
        wi = by_addr[wrapper]
        candidates = [x for x in writes if 0 < wi - x["index"] <= 500]
        chosen = min(candidates, key=lambda x: wi - x["index"], default=None)
        if chosen is None:
            records.append({"window_id": window_id, "wrapper_va": f"0x{wrapper:08x}", "binding_status": "unresolved"})
            continue
        ci = chosen["index"]
        table = next((x for x in json.loads(Path("docs/research/ei-ui-layout/window-vtable-evidence.json").read_text(encoding="utf-8"))["vtable_tables"] if int(x["vtable_va"], 16) == chosen["vtable"]), None)
        records.append({
            "window_id": window_id,
            "wrapper_va": f"0x{wrapper:08x}",
            "derived_vtable_va": f"0x{chosen['vtable']:08x}",
            "derived_assignment_va": f"0x{lines[ci]['address']:08x}",
            "instruction_distance": wi - chosen["va"],
            "paint_slot_candidate_plus_0xc": table["paint_slot_candidate_plus_0xc"] if table else None,
            "paint_slot_matches_shared_base": table["paint_slot_candidate_plus_0xc"] == "0x00423d00" if table else False,
            "binding_status": "nearby-derived-vtable-candidate",
            "warning": "Nearest preceding assignment heuristic; verify object lifetime/constructor boundary before promoting to verified binding.",
            "assignment_context": [x["text"].strip() for x in lines[max(0, ci - 5):ci + 8]],
        })
    result = {
        "source": str(args.exe),
        "method": "main window wrapper VA + nearest preceding non-base vtable assignment within 500 disassembly instructions",
        "evidence_level": "primary-static-binding-candidate",
        "records": records,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"window_bindings={len(records)}")
    print(f"resolved_candidates={sum(x['binding_status'] != 'unresolved' for x in records)}")
    print(f"shared_base_paint_candidates={sum(x.get('paint_slot_matches_shared_base',False) for x in records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
