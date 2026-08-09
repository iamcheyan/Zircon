#!/usr/bin/env python3
"""Render the traceable EI minimap cross-reference as a human-readable catalog.

The JSON cross-reference remains the machine-readable source.  This report is
deliberately generated from it so map names and frame numbers cannot drift
from the evidence collected from MiniMap.txt and the WIL headers.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("input", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()

    data = json.loads(args.input.read_text(encoding="utf-8"))
    rows = data["rows"]
    lines = [
        "# EI 3.0 地图/小地图资源目录",
        "",
        "> 本目录由 `minimap-server-crossref.json` 自动生成。服务器的 `MiniMap.txt`",
        "> 只作为地图名与数值的二级交叉引用；客户端 `Mir3.exe` 的资源选择和固定",
        "> 目标矩形仍是一级证据。不要把服务器名称误当作客户端绘制语义。",
        "",
        f"- 记录：{data['stats']['total_rows']} 条；FMMap：{data['stats']['fmmap_rows']} 条；MMap：{data['stats']['mmap_rows']} 条。",
        f"- 客户端地图文件匹配：{data['stats']['map_file_matches']} 条；资源帧可解码：{data['stats']['decodable_frame_matches']} 条。",
        "- 客户端选择规则：服务器值 `>=1001` → `FMMap.wil` 的 `value-1001`；否则 → `MMap.wil` 的 `value`。",
        "- 原版固定小地图目标：`(672,0)-(800,128)`；显示表面在静态代码中有 `128×128` 与 `256×256` 两种模式。",
        "",
        "## 目录",
        "",
        "| # | 地图文件名 | 服务器值 | 资源库 | Frame | 服务器名称 | 客户端 `.map` | 帧可解码 |",
        "|---:|---|---:|---|---:|---|:---:|:---:|",
    ]
    for index, row in enumerate(rows, 1):
        names = "、".join(row.get("server_map_names", [])) or "—"
        map_exists = "是" if row.get("client_map_exists") else "否"
        decodes = "是" if row.get("frame_nonblank_decodes") else "否"
        lines.append(
            f"| {index} | `{row['map_stem']}` | {row['server_value']} | "
            f"`{row['library']}` | {row['frame']} | {names} | {map_exists} | {decodes} |"
        )
    lines.extend([
        "",
        "## 证据边界",
        "",
        "- `server_value`、地图文件名和名称来自 `/home/tetsuya/NAS/TMP/Mud3/Envir/MiniMap.txt` 与 `Mapinfo.txt`。",
        "- 资源库、帧范围和非空解码来自原版客户端 `Data/MMap.wil`、`Data/FMMap.wil` 的 WIL 头与解码结果。",
        "- 最终屏幕合成路径来自 `Mir3.exe` 的 `0x0043D4D0/0x0043D780/0x0043DA80`；本表不证明标记颜色的业务名称，也不证明全地图窗口已经恢复。",
        "",
    ])
    args.output.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    main()
