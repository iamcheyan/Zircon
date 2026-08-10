#!/usr/bin/env python3
"""Compatibility entry point; canonical implementation lives in Tools/maps/test_lib_find.py."""
from pathlib import Path
import runpy
import sys

_root = Path(__file__).resolve().parent
_target = _root / "maps" / "test_lib_find.py"
sys.path.insert(0, str(_target.parent))
sys.path.insert(0, str(_root))
runpy.run_path(str(_target), run_name="__main__")
