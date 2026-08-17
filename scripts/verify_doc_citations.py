#!/usr/bin/env python3
"""Compatibility entry point; canonical implementation lives in Mir3-Research/Tools/i18n/verify_doc_citations.py."""
import sys, runpy
sys.path.insert(0, "/home/tetsuya/development/Mir3-Research/Tools/i18n")
runpy.run_module("verify_doc_citations", run_name="__main__", alter_sys=True)
