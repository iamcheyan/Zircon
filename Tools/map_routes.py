#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""解析 Mud3 服务端 Envir/Mapinfo.txt 的地图连接记录。

Mapinfo.txt (GBK) 含两类行:
  1. 地图定义: [地图码 中文名 0] DAY horse KSPD ...
  2. 地图连接: 地图A x,y -> 地图B x,y   (传送门/出入口, 双向可走)

输出 /tmp/map_links.json:
  {
    "names": { "3": "比奇县", "d2001": "", ... },   # 地图码 -> 中文名 (Mapinfo 定义, 更全)
    "links": [ ["3","0150"], ["41","d2001"], ... ],  # 无向连接对, 去重
  }
"""
import json
import os
import re

MAPINFO = "/home/tetsuya/NAS/TMP/Mud3/Envir/Mapinfo.txt"
OUT = "/tmp/map_links.json"


def main():
    raw = open(MAPINFO, encoding="gbk", errors="replace").read()
    names = {}
    links = set()
    pairs = 0
    for line in raw.splitlines():
        line = line.strip()
        if not line or line.startswith(";"):
            continue
        m = re.match(r"^\[(\S+)\s+(\S+)\s+\d+\]", line)
        if m:
            code, name = m.group(1), m.group(2).strip()
            names[code] = name
            continue
        m = re.match(r"^(\S+)\s+[\d,]+\s*->\s*(\S+)\s+[\d,]+$", line)
        if m:
            a, b = m.group(1), m.group(2)
            if a == b:
                continue
            links.add(tuple(sorted((a.lower(), b.lower()))))
            pairs += 1
    data = {
        "names": names,
        "links": sorted(links),
    }
    json.dump(data, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(f"地图定义 {len(names)} 张 / 连接对 {len(links)} 条 (原始行 {pairs}) -> {OUT}")
    for a, b in sorted(links)[:10]:
        print(f"  {a} <-> {b}   {names.get(a,'')} / {names.get(b,'')}")


if __name__ == "__main__":
    main()
