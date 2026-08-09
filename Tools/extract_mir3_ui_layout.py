#!/usr/bin/env python3
"""Extract static three-WORD UI initializers from the original Mir3.exe.

The first target is the helper at VA 0x00449c50.  Its body writes three WORD
arguments into the object pointer supplied by the caller.  The surrounding
constructor contains many fixed UI records, so this produces evidence-backed
candidates without pretending that every triple is already a screen Rect.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
from pathlib import Path

DEFAULT_EXE = Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe")
HELPER = 0x449C50

LINE_RE = re.compile(r"^\s*([0-9a-f]+):\s+(.*?)\s{2,}([a-z][a-z0-9]*)\s*(.*)$", re.I)
PUSH_RE = re.compile(r"push\s+(0x[0-9a-f]+|[0-9]+)$", re.I)
LEA_RE = re.compile(r"lea\s+([a-z]+),\s*\[([a-z]+)\s*\+\s*(0x[0-9a-f]+|[0-9]+)\]", re.I)


def disassemble(exe: Path) -> list[dict]:
    cmd = ["llvm-objdump", "-d", "--x86-asm-syntax=intel", str(exe)]
    text = subprocess.check_output(cmd, text=True, errors="replace")
    out = []
    for line in text.splitlines():
        m = LINE_RE.match(line)
        if not m:
            continue
        out.append({
            "address": int(m.group(1), 16),
            "bytes": m.group(2).strip(),
            "op": m.group(3).lower(),
            "args": m.group(4).strip(),
            "text": line.rstrip(),
        })
    return out


def immediate(args: str) -> int | None:
    m = PUSH_RE.fullmatch("push " + args.strip())
    if not m:
        return None
    return int(m.group(1), 0)


def extract(lines: list[dict]) -> list[dict]:
    result = []
    for i, line in enumerate(lines):
        if line["op"] != "call" or not re.match(rf"0x{HELPER:x}(?:\s|$)", line["args"], re.I):
            continue
        window = lines[max(0, i - 12):i]
        pushes = []
        pointer = None
        for item in window:
            if item["op"] == "push":
                value = immediate(item["args"])
                if value is not None:
                    pushes.append((item["address"], value))
            m = LEA_RE.fullmatch(f"{item['op']} {item['args']}")
            if m:
                pointer = {"register": m.group(1), "base": m.group(2), "offset": int(m.group(3), 0), "address": item["address"]}
        # The helper receives: pointer, value1, value2, value3.  With the
        # x86 cdecl-style call sequence the caller emits value3, value2,
        # value1, pointer, so the last three immediate pushes must be
        # reversed before assigning semantic field names.
        if len(pushes) < 3:
            continue
        triple = [value for _, value in reversed(pushes[-3:])]
        result.append({
            "source": "Mir3.exe",
            "helper_va": f"0x{HELPER:08x}",
            "call_va": f"0x{line['address']:08x}",
            "object": pointer,
            "values": {"value1": triple[0], "value2": triple[1], "value3": triple[2]},
            "raw_pushes": [{"va": f"0x{va:08x}", "value": value} for va, value in pushes[-3:]],
            "confidence": "static-initializer-candidate",
        })
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/static_rect_initializers.json"))
    args = parser.parse_args()
    lines = disassemble(args.exe)
    records = extract(lines)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "llvm-objdump x86 disassembly; calls to static helper 0x00449c50",
        "warning": "Values are raw three-WORD initializers; they require association with the consuming UI class before being called screen rectangles.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"disassembled={len(lines)} instructions")
    print(f"helper_calls={len(records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
