#!/usr/bin/env python3
"""Compatibility entry point; canonical implementation lives in Mir3-Research/Tools/i18n/i18n_auto_replace.py."""
import sys, runpy
sys.path.insert(0, "/home/tetsuya/development/Mir3-Research/Tools/i18n")
runpy.run_module("i18n_auto_replace", run_name="__main__", alter_sys=True)
