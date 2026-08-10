#!/usr/bin/env python3
"""Compatibility entry point; canonical implementation lives in Tools/reverse-engineering/resolve_mir3_control_positions.py."""
from pathlib import Path
import runpy
import sys

_root = Path(__file__).resolve().parent
_target = _root / "reverse-engineering" / "resolve_mir3_control_positions.py"
sys.path.insert(0, str(_target.parent))
sys.path.insert(0, str(_root))
runpy.run_path(str(_target), run_name="__main__")
