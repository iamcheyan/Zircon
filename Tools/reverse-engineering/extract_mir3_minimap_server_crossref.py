#!/usr/bin/env python3
"""Build a traceable EI minimap mapping from the original Mud3 server config.

This is a cross-reference, not executable evidence: Mir3.exe proves the WIL
resource families and selection paths, while Envir/MiniMap.txt supplies the
server map-name -> numeric minimap value used by this EI server distribution.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

from wilsdk import WilLibrary


def parse_rows(path: Path) -> list[dict]:
    rows: list[dict] = []
    for line_no, raw in enumerate(path.read_text(encoding="gbk", errors="replace").splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith(";;") or line.startswith(";"):
            continue
        match = re.match(r"^(\S+)\s+(\d+)(?:\s|$)", line)
        if not match:
            continue
        stem, value_text = match.groups()
        value = int(value_text)
        if value >= 1001:
            library, frame = "FMMap.wil", value - 1001
            family = "overland-or-city"
        else:
            library, frame = "MMap.wil", value
            family = "dungeon-or-field"
        rows.append({
            "source_line": line_no,
            "map_stem": stem,
            "server_value": value,
            "library": library,
            "frame": frame,
            "family_candidate": family,
        })
    return rows


def parse_map_names(path: Path | None) -> dict[str, list[str]]:
    """Read the EI server's display names without treating them as exe proof."""
    if path is None or not path.exists():
        return {}
    result: dict[str, list[str]] = {}
    pattern = re.compile(r"^\[([^\s\]]+)\s+(.+?)\s+\d+\]")
    for raw in path.read_text(encoding="gb18030", errors="replace").splitlines():
        line = raw.strip()
        if not line or line.startswith(";"):
            continue
        match = pattern.match(line)
        if not match:
            continue
        stem, name = match.groups()
        result.setdefault(stem, []).append(name.strip())
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("minimap_txt", type=Path)
    parser.add_argument("map_dir", type=Path)
    parser.add_argument("data_dir", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--mapinfo", type=Path, help="Optional EI Envir/Mapinfo.txt for secondary display names")
    args = parser.parse_args()

    libraries = {}
    for name in ("FMMap.wil", "MMap.wil"):
        path = args.data_dir / name
        if path.exists():
            libraries[name] = WilLibrary(str(path))

    map_stems = {p.stem for p in args.map_dir.glob("*.map")}
    map_names = parse_map_names(args.mapinfo)
    rows = parse_rows(args.minimap_txt)
    for row in rows:
        names = map_names.get(row["map_stem"], [])
        if names:
            row["server_map_names"] = names
        library = libraries.get(row["library"])
        frame = row["frame"]
        row["client_map_exists"] = row["map_stem"] in map_stems
        row["frame_in_library_range"] = bool(library and 0 <= frame < library.count)
        row["frame_nonblank_decodes"] = False
        if row["frame_in_library_range"]:
            try:
                row["frame_nonblank_decodes"] = library.decode(frame) is not None
            except Exception:
                row["frame_nonblank_decodes"] = False

    output = {
        "source": {
            "server_file": str(args.minimap_txt),
            "client_data_dir": str(args.data_dir),
            "client_map_dir": str(args.map_dir),
            "evidence_level": "secondary-server-cross-reference",
            "warning": "The numeric mapping comes from the original EI server configuration; Mir3.exe remains the primary source for rendering and resource-selection behavior.",
            "optional_name_source": str(args.mapinfo) if args.mapinfo else None,
            "optional_name_warning": "Names are decoded from the server Mapinfo.txt as secondary content labels; they do not establish client rendering semantics.",
        },
        "rules": [
            {"condition": "server_value >= 1001", "library": "FMMap.wil", "frame": "server_value - 1001"},
            {"condition": "server_value < 1001", "library": "MMap.wil", "frame": "server_value"},
        ],
        "library_counts": {name: lib.count for name, lib in libraries.items()},
        "rows": rows,
        "stats": {
            "total_rows": len(rows),
            "map_file_matches": sum(r["client_map_exists"] for r in rows),
            "decodable_frame_matches": sum(r["frame_nonblank_decodes"] for r in rows),
            "fmmap_rows": sum(r["library"] == "FMMap.wil" for r in rows),
            "mmap_rows": sum(r["library"] == "MMap.wil" for r in rows),
        },
    }
    args.output.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
