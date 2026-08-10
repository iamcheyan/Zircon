#!/usr/bin/env python3
"""Associate 0x00417550 controls with the original window wrapper bodies."""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble

CONTROL = 0x417550
WINDOWS = {
    0x42EA80: "window.inventory",
    0x44B130: "window.status",
    0x44D310: "window.store-candidate",
    0x4159D0: "window.exchange-candidate",
    0x424E60: "window.guild-candidate",
    0x424250: "window.group",
    0x414060: "window.chat-pop",
    0x4503B0: "window.group-pop-candidate",
    0x440FE0: "window.option",
    0x4473E0: "window.quest",
    0x4268C0: "window.horse",
    0x439250: "window.other-14-candidate",
    0x43ED00: "window.npc-candidate",
}


def immediate(args: str):
    m = re.fullmatch(r"(?:0x[0-9a-f]+|[0-9]+|-0x[0-9a-f]+|-?[0-9]+)", args.strip(), re.I)
    return int(m.group(0), 0) if m else None


def extract(lines: list[dict]) -> list[dict]:
    by_addr = {line["address"]: i for i, line in enumerate(lines)}
    records = []
    for start, window_id in WINDOWS.items():
        if start not in by_addr:
            continue
        begin = by_addr[start]
        end = len(lines)
        for j in range(begin + 1, len(lines)):
            if lines[j]["op"] == "ret":
                end = j + 1
                break
        for i in range(begin, end):
            line = lines[i]
            if line["op"] != "call" or not re.search(r"0x417550(?:\s|$)", line["args"], re.I):
                continue
            # Use only the current constructor's argument block.  A broad
            # radius can otherwise pull the previous control's frame pair
            # into this record because window controls are emitted back to
            # back in the original constructors.
            last_call = max((j for j in range(begin, i) if lines[j]["op"] == "call"), default=begin - 1)
            window = lines[max(begin, last_call + 1, i - 20):i]
            pushes = []
            for item in window:
                if item["op"] == "push":
                    pushes.append({"va": f"0x{item['address']:08x}", "args": item["args"], "immediate": immediate(item["args"])})
            values = [p["immediate"] for p in pushes if p["immediate"] is not None]
            pairs = []
            for a, b in zip(values, values[1:]):
                if a >= 40 and b >= 40 and abs(a - b) == 1:
                    pairs.append(sorted([a, b]))
            leas = [item["text"].strip() for item in window if item["op"] == "lea"]
            constructor_args = None
            if len(pushes) >= 9:
                # 0x00417550 is a thiscall with nine stack arguments. The
                # caller emits arg9..arg1; arg1 is resource, arg2/arg3 are
                # normal/state frames, and arg4/arg5 are x/y.
                ordered = pushes[-9:]
                constructor_args = {
                    "resource_arg1": ordered[8],
                    "normal_frame_arg2": ordered[7],
                    "state_frame_arg3": ordered[6],
                    "x_arg4": ordered[5],
                    "y_arg5": ordered[4],
                    "arg6": ordered[3],
                    "arg7": ordered[2],
                    "arg8": ordered[1],
                    "arg9": ordered[0],
                    "calling_convention": "caller pushes arg9..arg1; callee ret 0x24",
                }
            records.append({
                "window_id": window_id,
                "wrapper_va": f"0x{start:08x}",
                "control_constructor_va": "0x00417550",
                "call_va": f"0x{line['address']:08x}",
                "frame_pair_candidates": pairs,
                "object_lea": leas[-1] if leas else None,
                "constructor_args": constructor_args,
                "preceding_pushes": pushes,
                "raw_neighborhood": [item["text"].strip() for item in window],
                "evidence": {"level": "primary-static", "source": "Mir3.exe"},
            })
    return records


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-control-calls.json"))
    args = parser.parse_args()
    records = extract(__import__("extract_mir3_ui_layout").disassemble(args.exe))
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "window wrapper ranges to first RET; direct calls to 0x00417550",
        "warning": "Wrapper boundaries are static heuristics; frame pairs and object offsets are primary evidence, business control names remain unresolved.",
        "records": records,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"window_control_calls={len(records)}")
    print(f"with_frame_pair={sum(bool(r['frame_pair_candidates']) for r in records)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
