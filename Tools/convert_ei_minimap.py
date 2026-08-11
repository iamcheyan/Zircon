#!/usr/bin/env python3
"""convert_ei_minimap.py — 把 EI 客户端 FMMap.wil + MMap.wil 合并为 Zircon MiniMap.Zl。

背景: Zircon 客户端已替换 544 张 EI 地图, 但小地图库 MiniMap.Zl 仍是旧版
Zircon 资源 (537帧 v1 DXT1), 与 EI 地图布局不匹配。本工具把 EI 城镇大地图
(FMMap.wil, 31帧) 与洞穴小地图 (MMap.wil, 255帧) 解码为 RGBA 后合并写入
单个 ZL2 (PNG codec, raw deflate, C# DeflateStream 兼容)。

合并布局 (帧索引即 MiniMapDialog Image.Index 的取值):
  frame 0     = 空白占位 (对齐旧 MiniMap.Zl: 帧0空, DB 索引 1-based)
  frame 1-31  = FMMap.wil f0-30   (城镇, 0.map=比奇 f0 -> index 1)
  frame 32-286= MMap.wil f0-254  (洞穴, MMap f1 -> index 33)

DB 侧 MapInfo.MiniMap 字段 (System.db, MirDB 格式):
  FMMap.wil 绑定: MiniMap = frame + 1
  MMap.wil  绑定: MiniMap = frame + 32
  无绑定地图:     MiniMap = 0 (MiniMapDialog 收缩为 32px 条, 不显示)

依赖:
  - Mir3-Research/Tools/common/wilsdk.py (WIL/WIX 解码)
  - 本目录 zl2writer.py (ZL2 写入, PNG codec)

用法:
  python3 convert_ei_minimap.py <EI Data 目录> <输出 MiniMap.Zl>
  示例:
  python3 convert_ei_minimap.py "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data" \
      /home/tetsuya/development/Zircon/Debug/Client/Data/MiniMap.Zl
"""
from __future__ import annotations

import os
import sys

# wilsdk 与 zl2writer 的查找路径
for p in (
    "/home/tetsuya/development/Mir3-Research/Tools/common",
    os.path.dirname(os.path.abspath(__file__)),
):
    if p not in sys.path:
        sys.path.insert(0, p)

from wilsdk import WilLibrary  # noqa: E402
from zl2writer import write_zl2  # noqa: E402


def load_wil(wil_path: str) -> list:
    """读 WIL 全部帧为 zl2writer 帧列表 (空白帧 -> None, 保留 offset)。"""
    lib = WilLibrary(wil_path)
    frames = []
    for i in range(lib.count):
        hdr = lib.header(i)
        if hdr is None or hdr["width"] <= 0 or hdr["height"] <= 0:
            frames.append(None)
            continue
        img = lib.decode(i)
        frames.append(None if img is None else {
            "image": img,
            "offsetX": hdr["offsetX"],
            "offsetY": hdr["offsetY"],
            "shadowType": 49 if hdr.get("shadow") else 0,
        })
    lib.close()
    return frames


def convert(fm_wil: str, mm_wil: str, out_zl: str) -> dict:
    """合并 FMMap + MMap -> MiniMap.Zl (帧0空白 + FMMap + MMap)。"""
    fm = load_wil(fm_wil)
    mm = load_wil(mm_wil)
    merged = [None] + fm + mm  # 1 + 31 + 255 = 287
    os.makedirs(os.path.dirname(os.path.abspath(out_zl)), exist_ok=True)
    stats = write_zl2(out_zl, merged)
    return {
        "fm_frames": len(fm),
        "mm_frames": len(mm),
        "total_frames": len(merged),
        "payload_count": stats["payload_count"],
        "file_size": stats["file_size"],
    }


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(1)
    data_dir, out = sys.argv[1], sys.argv[2]
    fm_wil = os.path.join(data_dir, "FMMap.wil")
    mm_wil = os.path.join(data_dir, "MMap.wil")
    for p in (fm_wil, mm_wil):
        if not os.path.exists(p):
            print(f"[ERR] 缺少 {p}")
            sys.exit(1)
    print(f"FMMap={fm_wil}\nMMap={mm_wil}\n-> {out}")
    stats = convert(fm_wil, mm_wil, out)
    print(f"  frames={stats['total_frames']} (blank + FMMap {stats['fm_frames']} "
          f"+ MMap {stats['mm_frames']}) payloads={stats['payload_count']} "
          f"size={stats['file_size']:,} bytes")
