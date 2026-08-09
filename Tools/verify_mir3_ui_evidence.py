#!/usr/bin/env python3
"""Read-only integrity audit for the EI 3.0 UI reverse-engineering evidence.

The audit deliberately does not infer missing coordinates or promote candidates.
It only checks structural invariants and reports pending evidence so a later
runtime capture cannot silently invalidate the catalog.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys


DEFAULT_EXE = Path("/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe")
DEFAULT_CLIENT = DEFAULT_EXE.parent
DEFAULT_EVIDENCE = Path("docs/research/ei-ui-layout")
ALLOWED_LEVEL_PREFIXES = ("primary", "secondary", "candidate", "pending")


def load_json(path: Path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001 - report every broken artifact
        raise RuntimeError(f"invalid JSON: {path}: {exc}") from exc


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--client", type=Path, default=DEFAULT_CLIENT)
    ap.add_argument("--evidence", type=Path, default=DEFAULT_EVIDENCE)
    args = ap.parse_args()

    errors: list[str] = []
    warnings: list[str] = []
    required = [
        args.client / "Mir3.exe",
        args.client / "mir3.dat",
        args.client / "Data" / "GameInter.wil",
        args.client / "Data" / "GameInter.wix",
        args.client / "Data" / "MMap.wil",
        args.client / "Data" / "FMMap.wil",
    ]
    for path in required:
        if not path.is_file():
            errors.append(f"missing original: {path}")

    layout_path = args.evidence / "layout.json"
    try:
        layout = load_json(layout_path)
    except RuntimeError as exc:
        errors.append(str(exc))
        layout = {}

    viewport = layout.get("viewport", {})
    if viewport.get("width") != 800 or viewport.get("height") != 600:
        errors.append(f"layout viewport is not 800x600: {viewport}")
    records = layout.get("records", [])
    ids = [r.get("id") for r in records]
    duplicates = sorted({x for x in ids if x is not None and ids.count(x) > 1})
    if duplicates:
        errors.append(f"duplicate layout record IDs: {duplicates}")
    for record in records:
        level = str(record.get("evidence", {}).get("level", ""))
        if not level or not level.startswith(ALLOWED_LEVEL_PREFIXES):
            errors.append(f"record lacks evidence level: {record.get('id')}")

    pending_total = 0
    parsed = 0
    for path in sorted(args.evidence.glob("*.json")):
        try:
            data = load_json(path)
        except RuntimeError as exc:
            errors.append(str(exc))
            continue
        parsed += 1
        pending = data.get("pending", []) if isinstance(data, dict) else []
        if pending:
            pending_total += len(pending)
            warnings.append(f"{path.name}: pending={len(pending)}")

    if not records:
        errors.append("layout has no records")
    print(f"original_files={sum(path.is_file() for path in required)}/{len(required)}")
    print(f"layout_records={len(records)} viewport={viewport.get('width')}x{viewport.get('height')}")
    print(f"json_artifacts={parsed} pending_items={pending_total}")
    for warning in warnings:
        print(f"PENDING {warning}")
    for error in errors:
        print(f"ERROR {error}", file=sys.stderr)
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
