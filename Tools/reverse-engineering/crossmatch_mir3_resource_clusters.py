#!/usr/bin/env python3
"""Cross-match closed control clusters against recovered path-loader fields."""
from __future__ import annotations

import argparse
import json
from pathlib import Path


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--paths", type=Path, default=Path("docs/research/ei-ui-layout/resource-path-table.json"))
    ap.add_argument("--clusters", type=Path, nargs="+", default=[
        Path("docs/research/ei-ui-layout/interface1c-cluster-4027.json"),
        Path("docs/research/ei-ui-layout/interface1c-cluster-456d.json"),
        Path("docs/research/ei-ui-layout/gameinter-cluster-43e260.json"),
    ])
    ap.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/resource-cluster-crossmatch.json"))
    args = ap.parse_args()

    paths = json.loads(args.paths.read_text(encoding="utf-8")).get("records", [])
    by_field = {}
    for item in paths:
        if item.get("owner_field"):
            by_field.setdefault(item["owner_field"].lower(), []).append(item)

    matches = []
    for cluster_path in args.clusters:
        if not cluster_path.exists():
            continue
        cluster = json.loads(cluster_path.read_text(encoding="utf-8"))
        binding = cluster.get("resource_binding", {})
        field = binding.get("resource_object_expression", "")
        if not field and cluster.get("records"):
            field = cluster["records"][0].get("resource_handle", {}).get("object_expression", "")
            if not field:
                field = cluster["records"][0].get("resource", {}).get("object_expression", "")
        if "+0x" not in field.lower():
            continue
        field_key = "+0x" + field.lower().split("+0x", 1)[1].split()[0]
        candidates = by_field.get(field_key, [])
        matches.append({
            "cluster": cluster.get("cluster_id", cluster_path.stem),
            "artifact": str(cluster_path),
            "resource_field": field,
            "path_records": candidates,
            "match_status": "matched" if candidates else "unmatched",
            "evidence": {
                "level": "primary-static-crossmatch",
                "warning": "This confirms a shared owner-field offset; it does not assign a business window name.",
            },
        })
    result = {
        "method": "exact owner+offset join between closed control clusters and Mir3.exe path table",
        "source": {"paths": str(args.paths), "clusters": [str(p) for p in args.clusters]},
        "counts": {"clusters": len(matches), "matched": sum(m["match_status"] == "matched" for m in matches)},
        "records": matches,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"clusters={len(matches)} matched={result['counts']['matched']}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
