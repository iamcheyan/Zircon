#!/usr/bin/env python3
"""Decode and parse the human-readable skill records in EI Magic.exp."""
from __future__ import annotations

import argparse
import hashlib
import json
import re
from pathlib import Path

from decode_mir3_exp import decode


BLOCK_RE = re.compile(r"(?ms)^#(?P<id>\d+)\s*\n(?P<body>.*?)(?=^#\d+\s*$|\Z)")
NAME_RE = re.compile(r"^\[(.*?)\]\s*$", re.M)
FIELD_RE = re.compile(r"^(属性|元素|说明)\s*:\s*(.*?)\s*$", re.M)
LEVEL_RE = re.compile(
    r"^修炼(?P<level>\d+)级需要等级\s*:\s*(?P<need>.*?)\s*\n"
    r"\s*-\s*修炼值\s*:\s*(?P<practice>.*?)\s*$", re.M
)


def parse_records(text: str) -> list[dict]:
    records: list[dict] = []
    for match in BLOCK_RE.finditer(text):
        body = match.group("body").strip("\r\n")
        name_match = NAME_RE.search(body)
        fields = {key: value for key, value in FIELD_RE.findall(body)}
        levels = []
        for level in LEVEL_RE.finditer(body):
            def number_or_text(value: str):
                value = value.strip()
                return int(value) if value.isdigit() else value
            levels.append({
                "level": int(level.group("level")),
                "required_level": number_or_text(level.group("need")),
                "practice_value": number_or_text(level.group("practice")),
            })
        records.append({
            "id": int(match.group("id")),
            "name": name_match.group(1).strip() if name_match else None,
            "attribute": fields.get("属性"),
            "element": fields.get("元素"),
            "levels": levels,
            "description": fields.get("说明"),
            "raw_block": body,
        })
    # The file is ordered in three contiguous blocks.  The first record of
    # each block has the same numeric ID as the legacy class marker convention
    # (#3, #1, #2), but the number is also the actual skill ID; retain both
    # facts explicitly instead of pretending it is a separate header.
    starts = [(0, 3, "warrior-candidate"), (8, 1, "wizard-candidate"),
              (31, 2, "taoist-candidate")]
    for index, (start, marker, semantic) in enumerate(starts):
        end = starts[index + 1][0] if index + 1 < len(starts) else len(records)
        for record in records[start:end]:
            record["ordered_section"] = {
                "marker_id": marker,
                "semantic_candidate": semantic,
                "evidence_level": "primary-file-order-plus-content-candidate",
            }
    return records


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("source", type=Path)
    ap.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/magic-exp-records.json"))
    ap.add_argument("--text-out", type=Path, default=Path("docs/research/ei-ui-layout/Magic.exp.decoded.txt"))
    args = ap.parse_args()
    raw = args.source.read_bytes()
    decoded, meta = decode(raw)
    text = decoded.decode("gb18030", errors="replace")
    records = parse_records(text)
    result = {
        "source": {
            "file": str(args.source),
            "sha256": hashlib.sha256(raw).hexdigest(),
            "decoder": "Tools/content/decode_mir3_exp.py",
            "decode": meta,
            "text_encoding": "GB18030",
        },
        "record_count": len(records),
        "records": records,
        "evidence_level": "primary-decoded-client-file",
        "warning": "This is client Magic.exp content; do not merge it with Mud3 Envir/magic.dat without an explicit comparison key.",
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.text_out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    # The client text uses CRLF and a few cosmetic trailing spaces.  The
    # repository copy is a readable evidence export, so normalize only this
    # derived display file; raw bytes remain represented by the source hash
    # and every JSON record's raw_block.
    normalized_text = "\n".join(line.rstrip() for line in text.splitlines()) + "\n"
    args.text_out.write_text(normalized_text, encoding="utf-8")
    print(f"records={len(records)} json={args.out} text={args.text_out}")


if __name__ == "__main__":
    main()
