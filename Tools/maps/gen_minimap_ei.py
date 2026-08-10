#!/usr/bin/env python3
"""Generate the EI-client minimap index dump for mapviewer.

The EI (2003 Mir3) client has no System.db; its minimap frame numbers come
from the EI *server's* Envir/MiniMap.txt (WEMADE Mir3 layout):

    <map-stem>  <value>

Value interpretation (verified against rendered maps, 2026-08-09):
  - value >= 1001  -> overland/city minimap in FMMap.wil, frame = value - 1001
                     (0.map Bichon -> frame 0, the walled city with SW lake + N river)
  - value <  1001  -> dungeon/field minimap in MMap.wil, frame = value
                     (D001 ghost forest -> frame 1, D401 maze -> frame 11)

Only maps that exist in the client Map dir and whose frame actually decodes
are emitted.  Output format (tab-separated, one per line):

    <stem>\t<libname>\t<frame>

Consumed by mapviewer's `_minimap_index_ei()` (MINIMAP_EI_FILE).

Usage:
    python3 Tools/maps/gen_minimap_ei.py \
        /home/tetsuya/NAS/TMP/Mud3/Envir/MiniMap.txt \
        /home/tetsuya/NAS/TMP/EI传奇3.0客户端/Map \
        /home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data \
        > /tmp/minimap_map_ei.txt
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from wilsdk import WilLibrary  # noqa: E402


def main() -> None:
    if len(sys.argv) != 4:
        print(__doc__, file=sys.stderr)
        sys.exit(2)
    mmap_txt, map_dir, data_dir = sys.argv[1:4]

    rows: dict[str, int] = {}
    for line in open(mmap_txt, encoding="gbk", errors="replace"):
        line = line.strip()
        if not line or line.startswith(";;"):
            continue
        m = re.match(r"(\S+)\s+(\d+)", line)
        if m:
            rows[m.group(1)] = int(m.group(2))

    client_maps = {
        os.path.splitext(f)[0]
        for f in os.listdir(map_dir)
        if f.lower().endswith(".map")
    }

    libs = {
        name: WilLibrary(os.path.join(data_dir, name))
        for name in ("FMMap.wil", "MMap.wil")
        if os.path.exists(os.path.join(data_dir, name))
    }

    def has(lib, fid: int) -> bool:
        try:
            return lib.decode(fid) is not None
        except Exception:
            return False

    for stem in sorted(rows):
        if stem not in client_maps:
            continue
        val = rows[stem]
        if val >= 1001:
            libname, fid = "FMMap.wil", val - 1001
        else:
            libname, fid = "MMap.wil", val
        lib = libs.get(libname)
        if lib is None or not (0 <= fid < lib.count) or not has(lib, fid):
            continue
        print(f"{stem}\t{libname}\t{fid}")


if __name__ == "__main__":
    main()
