#!/usr/bin/env python3
"""Group unassigned control constructors into code clusters for follow-up."""
from __future__ import annotations

import argparse
import json
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--catalog", type=Path, default=Path("docs/research/ei-ui-layout/global-control-constructor-catalog.json"))
    parser.add_argument("--out", type=Path, default=Path("docs/research/ei-ui-layout/unassigned-control-clusters.json"))
    args = parser.parse_args()
    data = json.loads(args.catalog.read_text(encoding="utf-8"))
    items = [r for r in data["records"] if r["classification"] == "unassigned-control-candidate"]
    items.sort(key=lambda r: int(r["call_va"], 16))
    clusters = []
    for item in items:
        va = int(item["call_va"], 16)
        if not clusters or va - clusters[-1]["last_call_va"] > 0x100:
            clusters.append({"cluster_id": f"unassigned-cluster-{len(clusters)+1:02d}", "first_call_va": va, "last_call_va": va, "records": []})
        cluster = clusters[-1]
        cluster["last_call_va"] = va
        cluster["records"].append(item)
    for cluster in clusters:
        cluster["range"] = [f"0x{cluster['first_call_va']:08x}", f"0x{cluster['last_call_va']:08x}"]
        cluster["call_count"] = len(cluster["records"])
        cluster["frame_pairs"] = [pair for r in cluster["records"] for pair in (r.get("frame_pair_candidates") or [])]
        if cluster["range"] == ["0x004027df", "0x00402845"]:
            cluster["resource_binding"] = {
                "file": "Data/Interface1c.wil",
                "path_literal_va": "0x0047aaa0",
                "resource_object_expression": "owner+0x5b0",
                "load_call_va": "0x0040273e",
                "evidence_level": "primary-static-resource-handle-flow",
                "notes": "0x00402735 loads owner+0x5b0 with the Interface1c path; all four constructors push esi=owner+0x5b0.",
            }
        cluster.pop("first_call_va")
        cluster.pop("last_call_va")
    result = {
        "source": data["source"],
        "method": "address-gap clustering of unassigned 0x00417550 constructor calls",
        "warning": "Clusters are triage units, not recovered C++ class boundaries; each still needs function and handle tracing.",
        "cluster_count": len(clusters),
        "clusters": clusters,
    }
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"clusters={len(clusters)} records={len(items)}")
    print(f"wrote={args.out}")


if __name__ == "__main__":
    main()
