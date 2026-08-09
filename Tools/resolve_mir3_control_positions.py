#!/usr/bin/env python3
"""Resolve window-control x/y expressions from EI wrapper disassembly.

The result is intentionally conservative.  It evaluates only register
expressions whose stack argument origin and arithmetic are unambiguous; raw
instructions remain in window-control-calls.json for auditability.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from extract_mir3_ui_layout import DEFAULT_EXE, disassemble

REGS = {"eax", "ebx", "ecx", "edx", "esi", "edi", "ebp", "esp"}
MEM_ESP_RE = re.compile(r"dword ptr \[esp\s*\+\s*(0x[0-9a-f]+|[0-9]+)\]", re.I)
LEA_RE = re.compile(r"lea\s+(\w+),\s*\[(\w+)(?:\s*\+\s*(0x[0-9a-f]+|[0-9]+))?\]", re.I)
MOV_IMM_RE = re.compile(r"mov\s+(\w+),\s*(0x[0-9a-f]+|[0-9]+|-0x[0-9a-f]+|-?[0-9]+)$", re.I)
MOV_REG_RE = re.compile(r"mov\s+(\w+),\s*(\w+)$", re.I)
ADD_SUB_RE = re.compile(r"(add|sub)\s+(\w+),\s*(0x[0-9a-f]+|[0-9]+)$", re.I)
XOR_RE = re.compile(r"xor\s+(\w+),\s*(\w+)$", re.I)


def signed_int(text: str) -> int:
    return int(text, 0)


def add_expr(value: str | None, amount: int) -> str:
    base = value or "unknown"
    if amount == 0:
        return base
    sign = "+" if amount > 0 else "-"
    return f"({base}{sign}{abs(amount)})"


def stack_arg(delta: int, offset: int) -> str | None:
    absolute = delta + offset
    if absolute < 4 or (absolute - 4) % 4:
        return None
    return f"arg{((absolute - 4) // 4) + 1}"


def simulate(lines: list[dict], start_i: int, end_i: int) -> tuple[dict[str, str], dict[str, int], dict[int, str]]:
    state: dict[str, str] = {}
    delta = 0
    snapshots: dict[str, int] = {}
    # A constructor argument is consumed by the callee after it is pushed.
    # Capture the register expression at that exact instruction; the same
    # register may be overwritten immediately afterwards for the thiscall.
    push_values: dict[int, str] = {}
    for i in range(start_i, end_i):
        item = lines[i]
        op, args = item["op"], item["args"]
        if op == "mov":
            m = MEM_ESP_RE.search(args)
            dst = args.split(",", 1)[0].strip().lower() if "," in args else ""
            if m and dst in REGS:
                origin = stack_arg(delta, signed_int(m.group(1)))
                if origin:
                    state[dst] = origin
                    continue
            m = MOV_IMM_RE.fullmatch(f"mov {args}")
            if m:
                state[m.group(1).lower()] = str(signed_int(m.group(2)))
                continue
            m = MOV_REG_RE.fullmatch(f"mov {args}")
            if m and m.group(1).lower() in REGS:
                state[m.group(1).lower()] = state.get(m.group(2).lower(), m.group(2).lower())
                continue
        elif op == "lea":
            m = LEA_RE.fullmatch(f"lea {args}")
            if m and m.group(1).lower() in REGS:
                base = state.get(m.group(2).lower(), m.group(2).lower())
                offset = signed_int(m.group(3)) if m.group(3) else 0
                state[m.group(1).lower()] = add_expr(base, offset)
                continue
        elif op in {"add", "sub"}:
            m = ADD_SUB_RE.fullmatch(f"{op} {args}")
            if m and m.group(2).lower() in REGS:
                amount = signed_int(m.group(3)) * (1 if op == "add" else -1)
                if m.group(2).lower() == "esp":
                    delta += amount
                    continue
                state[m.group(2).lower()] = add_expr(state.get(m.group(2).lower()), amount)
                continue
        elif op == "xor":
            m = XOR_RE.fullmatch(f"xor {args}")
            if m and m.group(1).lower() == m.group(2).lower():
                state[m.group(1).lower()] = "0"
                continue
        if op == "push":
            operand = args.strip().lower()
            if operand in REGS:
                push_values[item["address"]] = state.get(operand, operand)
            else:
                push_values[item["address"]] = operand
            delta -= 4
        elif op == "pop":
            delta += 4
        elif op == "sub" and args.lower().startswith("esp,"):
            delta -= signed_int(args.split(",", 1)[1].strip())
        elif op == "add" and args.lower().startswith("esp,"):
            delta += signed_int(args.split(",", 1)[1].strip())
    return state, {"esp_delta": delta}, push_values


def substitute(expr: str | None, window_x: int | None, window_y: int | None) -> tuple[str | None, int | None]:
    if not expr:
        return None, None
    readable = expr.replace("arg4", "window.x").replace("arg5", "window.y")
    if window_x is None or window_y is None or "unknown" in expr:
        return readable, None
    numeric = re.sub(r"arg4", str(window_x), expr)
    numeric = re.sub(r"arg5", str(window_y), numeric)
    if not re.fullmatch(r"[0-9()+\- ]+", numeric):
        return readable, None
    try:
        return readable, int(eval(numeric, {"__builtins__": {}}, {}))
    except (ArithmeticError, SyntaxError, ValueError):
        return readable, None


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--exe", type=Path, default=DEFAULT_EXE)
    parser.add_argument("--controls", type=Path, default=Path("docs/research/ei-ui-layout/window-control-calls.json"))
    parser.add_argument("--windows", type=Path, default=Path("docs/research/ei-ui-layout/window_layout.json"))
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/window-control-position-analysis.json"))
    args = parser.parse_args()

    lines = disassemble(args.exe)
    by_addr = {line["address"]: i for i, line in enumerate(lines)}
    controls = json.loads(args.controls.read_text(encoding="utf-8"))["records"]
    windows = {r["id"]: r["window"] for r in json.loads(args.windows.read_text(encoding="utf-8"))["records"]}
    results = []
    for control in controls:
        call_va = int(control["call_va"], 16)
        start_va = int(control["wrapper_va"], 16)
        ci, si = by_addr.get(call_va), by_addr.get(start_va)
        args_info = control.get("constructor_args") or {}
        if ci is None or si is None:
            continue
        state, stack, push_values = simulate(lines, si, ci)
        win = windows.get(control["window_id"], {})
        x_arg = args_info.get("x_arg4", {}).get("args")
        y_arg = args_info.get("y_arg5", {}).get("args")
        x_reg = x_arg.lower() if x_arg and x_arg.lower() in REGS else None
        y_reg = y_arg.lower() if y_arg and y_arg.lower() in REGS else None
        x_push_va = args_info.get("x_arg4", {}).get("va")
        y_push_va = args_info.get("y_arg5", {}).get("va")
        x_expr = push_values.get(int(x_push_va, 16)) if x_push_va and x_reg else x_arg
        y_expr = push_values.get(int(y_push_va, 16)) if y_push_va and y_reg else y_arg
        x_readable, x_abs = substitute(x_expr, win.get("x"), win.get("y"))
        y_readable, y_abs = substitute(y_expr, win.get("x"), win.get("y"))
        x_axis_ok = bool(x_readable and ("window.x" in x_readable or "window." not in x_readable))
        y_axis_ok = bool(y_readable and ("window.y" in y_readable or "window." not in y_readable))
        if not x_axis_ok:
            x_abs = None
        if not y_axis_ok:
            y_abs = None
        inside_window = None
        if x_abs is not None and y_abs is not None and win:
            inside_window = (win.get("x", 0) <= x_abs <= win.get("x", 0) + win.get("width", 0)
                             and win.get("y", 0) <= y_abs <= win.get("y", 0) + win.get("height", 0))
        geometric_status = "inside-window" if inside_window is True else ("outside-window" if inside_window is False else "not-evaluated")
        results.append({
            "window_id": control["window_id"],
            "wrapper_va": control["wrapper_va"],
            "call_va": control["call_va"],
            "frame_pair": (control.get("frame_pair_candidates") or [None])[-1],
            "x": {"argument": x_arg, "register": x_reg, "expression": x_readable, "absolute_candidate": x_abs},
            "y": {"argument": y_arg, "register": y_reg, "expression": y_readable, "absolute_candidate": y_abs},
            "axis_validation": {"x_uses_window_x": x_axis_ok, "y_uses_window_y": y_axis_ok},
            "geometric_status": geometric_status,
            "window_origin_used": {"x": win.get("x"), "y": win.get("y")},
            "register_state_at_call": state,
            "stack": stack,
            "evidence": {"level": "primary-static-expression", "source": "Mir3.exe + window_layout.json",
                         "notes": "Absolute candidates are emitted only for simple expressions; inspect raw neighborhood before upgrading."},
        })
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps({
        "source": str(args.exe),
        "method": "symbolic register/ESP simulation from each known window wrapper entry to 0x00417550",
        "warning": "This is a conservative expression resolver, not a complete x86 emulator. Absolute candidates require manual evidence review.",
        "records": results,
    }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    resolved = sum(r["x"]["absolute_candidate"] is not None and r["y"]["absolute_candidate"] is not None for r in results)
    inside = sum(r["geometric_status"] == "inside-window" for r in results)
    print(f"position_records={len(results)}")
    print(f"both_absolute_candidates={resolved}")
    print(f"inside_window_candidates={inside}")
    print(f"wrote={args.out}")
    for r in results[:8]:
        print(r["window_id"], r["call_va"], r["x"], r["y"])


if __name__ == "__main__":
    main()
