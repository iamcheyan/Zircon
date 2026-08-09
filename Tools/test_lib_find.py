#!/usr/bin/env python3
"""test_lib_find.py — Check which WIL files KR_ORDER IDs map to and whether they exist."""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mapviewer import KR_ORDER, _find_library_path

def main():
    data_dir = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data"
    print("Testing Library Resolution for all KR_ORDER IDs:")
    missing = []
    found = []
    for k, v in sorted(KR_ORDER.items()):
        path = _find_library_path(data_dir, v)
        if path and os.path.exists(path):
            found.append((k, v, path))
        else:
            missing.append((k, v))

    print(f"\nFound {len(found)} libraries.")
    print(f"Missing {len(missing)} libraries:\n")
    for k, v in missing:
        print(f"  ID {k:2d}: {v:20s}")

if __name__ == "__main__":
    main()
