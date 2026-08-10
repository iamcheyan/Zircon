#!/usr/bin/env python3
"""Compatibility entry point; canonical implementation lives in Tools/content/parse_mir3_magic_exp.py."""
from pathlib import Path
import runpy
import sys

_root = Path(__file__).resolve().parent
_target = _root / "content" / "parse_mir3_magic_exp.py"
sys.path.insert(0, str(_target.parent))
sys.path.insert(0, str(_root))
runpy.run_path(str(_target), run_name="__main__")
