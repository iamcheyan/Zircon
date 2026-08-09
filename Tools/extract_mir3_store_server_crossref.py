#!/usr/bin/env python3
"""Cross-reference original Mud3 merchant entries with their NPC scripts.

This is a secondary evidence source for naming the client store state machine.
It never changes the original Mud3 files and deliberately keeps client-side
Frame/state bindings separate from server script semantics.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


def read_legacy(path: Path) -> str:
    data = path.read_bytes()
    for enc in ("gbk", "cp936", "utf-8"):
        try:
            return data.decode(enc)
        except UnicodeDecodeError:
            continue
    return data.decode("latin1", errors="replace")


def script_files(root: Path, stem: str, map_id: str) -> list[Path]:
    names = [f"{stem}-{map_id}.txt", f"{stem}_{map_id}.txt", f"{stem}.txt"]
    found: list[Path] = []
    for folder in (root / "Market_Def", root / "Convert_Def" / "Market_Def"):
        for name in names:
            p = folder / name
            if p.exists() and p not in found:
                found.append(p)
        if not found:
            found.extend(sorted(folder.glob(stem + "-*.txt")))
    return found


def categories(text: str) -> list[str]:
    out: list[str] = []
    sections = re.findall(r"^\[@([^\]]+)\]", text, re.M)
    for section in sections:
        low = section.lower()
        if "storage" in low or "getback" in low:
            label = "storage"
        elif "buy" in low:
            label = "buy"
        elif "sell" in low:
            label = "sell"
        elif "main" in low:
            label = "npc-main"
        else:
            label = "other"
        if label not in out:
            out.append(label)
    return out


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", type=Path, default=Path("/home/tetsuya/NAS/TMP/Mud3/Envir"))
    ap.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/store-server-crossref.json"))
    args = ap.parse_args()
    merchant = args.root / "Merchant.txt"
    records: list[dict] = []
    for line_no, raw in enumerate(read_legacy(merchant).splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith(";"):
            continue
        fields = line.split()
        if len(fields) < 7:
            continue
        stem, map_id, x, y, name = fields[:5]
        files = script_files(args.root, stem, map_id)
        scripts = []
        all_categories: list[str] = []
        for path in files:
            text = read_legacy(path)
            cats = categories(text)
            all_categories.extend(c for c in cats if c not in all_categories)
            scripts.append({"path": str(path), "sections": re.findall(r"^\[@([^\]]+)\]", text, re.M), "categories": cats})
        records.append({
            "merchant_line": line_no,
            "merchant_stem": stem,
            "map": map_id,
            "world_position": [int(x), int(y)] if x.isdigit() and y.isdigit() else [x, y],
            "server_name_raw": name,
            "matched_scripts": scripts,
            "server_categories": all_categories,
            "evidence_level": "secondary-server-script-cross-reference",
        })
    summary = {cat: sum(cat in r["server_categories"] for r in records) for cat in ("storage", "buy", "sell", "npc-main", "other")}
    result = {
        "source": {"merchant_file": str(merchant), "script_root": str(args.root), "encoding": "GBK/CP936 fallback"},
        "warning": "Server scripts name NPC behavior; they do not prove which Mir3.exe client state/Frame is shown. Client-side state/Frame evidence remains primary.",
        "counts": {"merchant_records": len(records), "matched_script_records": sum(bool(r["matched_scripts"]) for r in records), "categories": summary},
        "records": records,
    }
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result["counts"], ensure_ascii=False))
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
