#!/usr/bin/env python3
"""Recover WIL path-table entries copied by the EI resource initializers."""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble

ABS_PATH_RE = re.compile(r"edi,\s*(0x[0-9a-f]+)", re.I)
PUSH_PATH_RE = re.compile(r"(0x[0-9a-f]+)", re.I)
DEST_RE = re.compile(r"lea\s+edx,\s*\[ebx\s*\+\s*(0x[0-9a-f]+|[0-9]+)\]", re.I)
ANY_LEA_FIELD_RE = re.compile(r"lea\s+\w+,\s*\[(ebx|esi|edi)\s*\+\s*(0x[0-9a-f]+|[0-9]+)\]", re.I)


def read_c_string(exe: Path, va: int) -> str | None:
    data = exe.read_bytes()
    off = va - 0x400000
    if off < 0 or off >= len(data):
        return None
    raw = data[off:off + 256].split(b"\0", 1)[0]
    if not raw.startswith(b".\\Data\\"):
        return None
    try:
        return raw.decode("latin-1")
    except UnicodeDecodeError:
        return None


def extract(lines: list[dict], exe: Path) -> list[dict]:
    records = []
    last_destination = None
    last_destination_va = None
    for i, line in enumerate(lines):
        # Resource tables are emitted by separate helper functions.  Do not
        # carry an LEA destination across a function return, otherwise the
        # first path in the next table can inherit an unrelated field.
        if line["op"] == "ret":
            last_destination = None
            last_destination_va = None
            continue
        dm_current = DEST_RE.fullmatch(f"{line['op']} {line['args']}")
        if dm_current:
            last_destination = int(dm_current.group(1), 0)
            last_destination_va = line["address"]
            continue
        match = ABS_PATH_RE.fullmatch(line["args"] if line["op"] == "mov" else "")
        if not match:
            continue
        source_va = int(match.group(1), 0)
        path = read_c_string(exe, source_va)
        if not path:
            continue
        destination = last_destination
        destination_va = last_destination_va
        destination_basis = "preceding-lea-edx"
        # The first entry in each table begins before its first LEA.  In that
        # case the LEA immediately following the path copy supplies the
        # destination for this first entry; later entries inherit the LEA
        # emitted at the end of the preceding copy block.
        if destination is None:
            for later in lines[i + 1:i + 70]:
                if later["op"] == "mov" and ABS_PATH_RE.fullmatch(later["args"]):
                    break
                dm = DEST_RE.fullmatch(f"{later['op']} {later['args']}")
                if dm:
                    destination = int(dm.group(1), 0)
                    destination_va = later["address"]
                    destination_basis = "following-lea-edx"
                    break
        records.append({
            "path_literal_va": f"0x{source_va:08x}",
            "path": path,
            "owner_field": f"+0x{destination:x}" if destination is not None else None,
            "destination_lea_va": f"0x{destination_va:08x}" if destination_va is not None else None,
            "destination_basis": destination_basis if destination is not None else None,
            "copy_source_va": f"0x{line['address']:08x}",
            "evidence": {"level": "primary-static-path-table", "source": str(exe)},
        })
    # Some resource initializers pass the path literal directly as a stack
    # argument to a loader (not through the ``mov edi; rep movs`` idiom above).
    # Capture these as a separate static path-flow form; this closes the
    # Interface1c/GameInter loader at 0x00402735–0x0040273E.
    for i, line in enumerate(lines):
        if line["op"] != "push":
            continue
        match = PUSH_PATH_RE.fullmatch(line["args"])
        if not match:
            continue
        source_va = int(match.group(1), 0)
        path = read_c_string(exe, source_va)
        if not path:
            continue
        candidates = []
        for j in range(max(0, i - 12), min(len(lines), i + 20)):
            if j == i:
                continue
            lm = ANY_LEA_FIELD_RE.fullmatch(f"{lines[j]['op']} {lines[j]['args']}")
            if lm:
                candidates.append((abs(j - i), j, int(lm.group(2), 0)))
        if candidates:
            _, j, destination = min(candidates)
            destination_va = lines[j]["address"]
            owner_field = f"+0x{destination:x}"
            basis = "nearby-lea-field"
        else:
            owner_field = None
            destination_va = None
            basis = None
        records.append({
            "path_literal_va": f"0x{source_va:08x}",
            "path": path,
            "owner_field": owner_field,
            "destination_lea_va": f"0x{destination_va:08x}" if destination_va is not None else None,
            "destination_basis": basis,
            "copy_source_va": f"0x{line['address']:08x}",
            "evidence": {"level": "primary-static-path-loader-argument", "source": str(exe)},
        })
    # Preserve the first occurrence for a literal/path/field triple, but keep
    # different owner fields if the same WIL is loaded into multiple managers.
    unique = {}
    for record in records:
        key = (record["path_literal_va"], record["owner_field"])
        unique.setdefault(key, record)
    return list(unique.values())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/resource-path-table.json"))
    args = parser.parse_args()
    records = extract(disassemble(args.exe), args.exe)
    result = {
        "source": str(args.exe),
        "method": "absolute path-literal and owner+offset copy-sequence recovery",
        "warning": "The table identifies copied path fields; it does not by itself prove which runtime object later consumes every field.",
        "records": records,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"path_records={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
