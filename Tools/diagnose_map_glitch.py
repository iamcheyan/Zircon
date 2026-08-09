#!/usr/bin/env python3
"""diagnose_map_glitch.py — Check map cell files and images used in current map."""
import os
import sys
import struct

def main():
    map_dir = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Map"
    if not os.path.exists(map_dir):
        print("Map dir not found:", map_dir)
        return

    maps = [f for f in os.listdir(map_dir) if f.lower().endswith(".map")]
    print(f"Found {len(maps)} map files in {map_dir}")
    
    # Pick a map to analyze (e.g. 0.map or D001.map or B1.map)
    for mname in sorted(maps)[:5]:
        mpath = os.path.join(map_dir, mname)
        with open(mpath, "rb") as f:
            data = f.read()

        w = struct.unpack_from("<H", data, 22)[0]
        h = struct.unpack_from("<H", data, 24)[0]

        offset = 28
        offset += (w // 2) * (h // 2) * 3  # skip back layer

        back_files = set()
        mid_files = set()
        front_files = set()

        for x in range(w):
            for y in range(h):
                ff = data[offset + 3]
                mf = data[offset + 4]
                if mf != 255: mid_files.add(mf)
                if ff != 255: front_files.add(ff)
                offset += 14

        print(f"Map '{mname}' ({w}x{h}): mid_files={sorted(mid_files)}, front_files={sorted(front_files)}")

if __name__ == "__main__":
    main()
