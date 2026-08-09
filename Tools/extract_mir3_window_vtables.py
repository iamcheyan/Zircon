#!/usr/bin/env python3
"""Recover EI window vtable targets used by constructors and paint dispatch.

The binary has no symbols.  This tool combines direct ``mov [esi], vtable``
assignments with the vtable contents in PE .rdata, preserving raw addresses.
The +0xc entry is recorded as a paint-method candidate only; its semantic slot
name is not assumed without a call-site proof.
"""
from __future__ import annotations

import argparse
import json
import re
import struct
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble

IMAGE_BASE = 0x400000
RDATA_RVA = 0x76000
RDATA_RAW = 0x76000
VTABLE_MIN = 0x476600
VTABLE_MAX = 0x476c10
CODE_MIN = 0x401000
CODE_MAX = 0x476000
ASSIGN_RE = re.compile(r"mov\s+dword ptr \[esi\],\s*(0x[0-9a-f]+)", re.I)


def va_to_raw(va: int) -> int:
    rva = va - IMAGE_BASE
    if RDATA_RVA <= rva < RDATA_RVA + 0x4000:
        return RDATA_RAW + (rva - RDATA_RVA)
    raise ValueError(f"VA outside .rdata: {va:#x}")


def read_table(data: bytes, va: int, count: int = 12) -> list[int]:
    off = va_to_raw(va)
    return list(struct.unpack_from(f"<{count}I", data, off))


def function_context(lines: list[dict], index: int) -> dict:
    # A conservative context: the nearest preceding RET, with the assignment
    # itself retained as the authoritative constructor location.
    start = index
    for i in range(index - 1, max(-1, index - 80), -1):
        if lines[i]["op"] == "ret":
            start = i + 1
            break
    return {
        "heuristic_function_start": f"0x{lines[start]['address']:08x}",
        "raw_neighborhood": [x["text"].strip() for x in lines[max(start, index - 4):index + 8]],
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-vtable-evidence.json"))
    args = parser.parse_args()
    data = args.exe.read_bytes()
    lines = disassemble(args.exe)
    assignments = []
    seen = set()
    for i, line in enumerate(lines):
        m = ASSIGN_RE.search(line["text"])
        if not m:
            continue
        vtable = int(m.group(1), 0)
        if not VTABLE_MIN <= vtable < VTABLE_MAX:
            continue
        key = (line["address"], vtable)
        if key in seen:
            continue
        seen.add(key)
        entries = read_table(data, vtable)
        assignments.append({
            "assignment_va": f"0x{line['address']:08x}",
            "vtable_va": f"0x{vtable:08x}",
            "entry_targets": [f"0x{x:08x}" for x in entries],
            "paint_slot_candidate_plus_0xc": f"0x{entries[3]:08x}",
            "paint_slot_matches_shared_base_0x423d00": entries[3] == 0x423D00,
            **function_context(lines, i),
        })
    tables = []
    for va in sorted({int(x["vtable_va"], 16) for x in assignments}):
        entries = read_table(data, va)
        tables.append({
            "vtable_va": f"0x{va:08x}",
            "entry_targets": [f"0x{x:08x}" for x in entries],
            "paint_slot_candidate_plus_0xc": f"0x{entries[3]:08x}",
            "has_code_like_entries": sum(CODE_MIN <= x < CODE_MAX for x in entries),
        })
    indirect_paint_calls = []
    indirect_re = re.compile(r"call\s+dword ptr \[(eax|ecx|edx|esi|edi) \+ 0xc\]", re.I)
    for i, line in enumerate(lines):
        if not indirect_re.search(line["text"]):
            continue
        indirect_paint_calls.append({
            "call_va": f"0x{line['address']:08x}",
            "raw_neighborhood": [x["text"].strip() for x in lines[max(0, i - 4):i + 1]],
            "evidence": "primary-static-indirect-slot-call",
        })
    result = {
        "source": str(args.exe),
        "method": "PE .rdata vtable reads + direct mov [esi], immediate assignments",
        "evidence_level": "primary-static-vtable-candidate",
        "slot_warning": "+0xc is a candidate based on the base dispatch path; virtual slot naming and per-window semantics still require call-site validation.",
        "vtable_tables": tables,
        "constructor_assignments": assignments,
        "indirect_plus_0xc_calls": indirect_paint_calls,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"vtable_tables={len(tables)}")
    print(f"constructor_assignments={len(assignments)}")
    print(f"base_paint_slot_matches={sum(x['paint_slot_matches_shared_base_0x423d00'] for x in assignments)}")
    print(f"indirect_plus_0xc_calls={len(indirect_paint_calls)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
