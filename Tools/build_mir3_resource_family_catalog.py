#!/usr/bin/env python3
"""Join the recovered Mir3.exe path table with the original WIL libraries.

This is an evidence index, not a semantic window-name guess.  It records which
resource families exist in the supplied EI client and how many WIL frames each
library exposes, so later UI/resource matches can be made reproducibly.
"""
from __future__ import annotations

import argparse
import json
import os
import struct
from pathlib import Path

import wilsdk


def category(path: str) -> str:
    name = path.replace("\\", "/").lower()
    base = os.path.basename(name)
    if base in {"gameinter.wil", "interface1c.wil"}:
        return "ui"
    if base in {"magic.wil", "magicex.wil", "monmagic.wil", "monmagicex.wil"}:
        return "magic"
    if base in {"inventory.wil", "equip.wil", "ground.wil", "micon.wil", "proguse.wil", "storeitem.wil"}:
        return "items"
    if base.startswith(("m-hum", "wm-hum", "m-weapon", "wm-weapon", "m-hair", "wm-hair", "m-helmet", "wm-helmet")):
        return "character-and-gear"
    if base in {"npc.wil", "npcface.wil"}:
        return "npc"
    if base.startswith(("mon-", "mons-", "dmon-", "dmons-", "monimg")):
        return "monsters"
    if any(part in name for part in ("/wood/", "/sand/", "/forest/", "/snow/")) or base.startswith(("mmap", "fmmap", "tiles", "object", "houses", "cliffs", "dungeons", "inners", "furniture", "smobject", "animations", "walls")):
        return "map"
    return "other"


def resolve_case_insensitive(path: Path) -> Path:
    """Resolve original Windows-style case on the Linux-mounted client tree."""
    if path.exists():
        return path
    parent = path.parent
    if not parent.is_dir():
        return path
    wanted = path.name.lower()
    for entry in parent.iterdir():
        if entry.name.lower() == wanted:
            return entry
    return path


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", type=Path, default=Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端"))
    ap.add_argument("--paths", type=Path, default=Path("docs/research/ei-ui-layout/resource-path-table.json"))
    ap.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/resource-family-catalog.json"))
    args = ap.parse_args()

    path_data = json.loads(args.paths.read_text(encoding="utf-8"))
    records = []
    for item in path_data.get("records", []):
        rel = item["path"].replace("\\", "/").lstrip("./")
        wil = resolve_case_insensitive(args.root / rel)
        wix = resolve_case_insensitive(wil.with_suffix(".wix"))
        record = {
            "path": item["path"],
            "category": category(item["path"]),
            "owner_field": item.get("owner_field"),
            "path_literal_va": item.get("path_literal_va"),
            "copy_source_va": item.get("copy_source_va"),
            "files": {"wil": str(wil), "wix": str(wix), "wil_exists": wil.is_file(), "wix_exists": wix.is_file()},
            "evidence": {"level": "primary-static-path-plus-library-header", "source": "Mir3.exe + original WIL/WIX"},
        }
        if wil.is_file() and wix.is_file() and wil.suffix.lower() == ".wil":
            try:
                lib = wilsdk.WilLibrary(str(wil))
                nonblank = sum(1 for i in range(lib.count) if lib.header(i) is not None)
                record["library"] = {"frame_count": lib.count, "nonblank_frame_count": nonblank}
            except (OSError, ValueError, struct.error) as exc:  # type: ignore[name-defined]
                record["library_error"] = str(exc)
        records.append(record)

    result = {
        "source": {"path_table": str(args.paths), "client_root": str(args.root)},
        "method": "join static path-table records with original WIL/WIX headers",
        "warning": "Categories are resource-family labels only; they do not prove a UI window's business name or draw order.",
        "counts": {"records": len(records), "existing_wil": sum(r["files"]["wil_exists"] for r in records), "existing_wix": sum(r["files"]["wix_exists"] for r in records)},
        "records": records,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"family_records={len(records)} existing_wil={result['counts']['existing_wil']} existing_wix={result['counts']['existing_wix']}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
