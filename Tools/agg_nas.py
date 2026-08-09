#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""NAS 派生数据聚合: 从 /home/tetsuya/NAS/TMP 生成百科所需的派生 json。

输出:
  /tmp/ei_maps.json        [{name, w, h}]           EI 客户端地图尺寸 (.map 头 22/24 偏移)
  /tmp/mud3_mapinfo.json   [{file, zh}]             Mud3 服务端地图中文名 (Mapinfo.txt, GBK)
  /tmp/mud3_spawns.json    {map: {mon: count}}      刷怪聚合 (MonGen.txt -> Mon_Def/*.gen, GBK)
  /tmp/mud3_merchants.json {map: [name]}            商人 (Merchant.txt, GBK)
  /tmp/mud3_guards.json    {map: [name]}            守卫 (GuardList.txt, GBK)
  /tmp/report_full.json    地图内容报告全量数据 (EI_MAP_REPORT_gen.py / WikiServer 依赖)

数据源优先级: Mud3 服务端 Envir > EI 客户端 Map。
"""
import json
import os
import re
import struct
from collections import defaultdict

NAS = "/home/tetsuya/NAS/TMP"
EI_MAP = os.path.join(NAS, "EI传奇3.0客户端", "Map")
MEI_MAP = os.path.join(NAS, "mir3ei", "Map")
ENVIR = os.path.join(NAS, "Mud3", "Envir")
DOCS = "/home/tetsuya/development/Zircon/docs/terminology"


def read_gbk(p):
    with open(p, encoding="gbk", errors="replace") as f:
        return f.read()


def scan_maps(d):
    out = {}
    for fn in os.listdir(d):
        if not fn.lower().endswith(".map"):
            continue
        with open(os.path.join(d, fn), "rb") as f:
            head = f.read(26)
        if len(head) < 26:
            continue
        w = struct.unpack_from("<H", head, 22)[0]
        h = struct.unpack_from("<H", head, 24)[0]
        out[fn] = (w, h)
    return out


# ---------- 1. ei_maps ----------
ei_maps = sorted(
    ({"name": fn, "w": w, "h": h} for fn, (w, h) in scan_maps(EI_MAP).items()),
    key=lambda x: x["name"].lower(),
)
json.dump(ei_maps, open("/tmp/ei_maps.json", "w", encoding="utf-8"), ensure_ascii=False)

# ---------- 2. Mapinfo ----------
mapinfo = []
for line in read_gbk(os.path.join(ENVIR, "Mapinfo.txt")).splitlines():
    line = line.strip()
    if not line.startswith("[") or line.startswith(";;"):
        continue
    m = re.match(r"^\[(\S+)\s+(\S+)", line)
    if m:
        mapinfo.append({"file": m.group(1), "zh": m.group(2)})
json.dump(mapinfo, open("/tmp/mud3_mapinfo.json", "w", encoding="utf-8"), ensure_ascii=False)

# ---------- 3. spawns ----------
spawns = defaultdict(lambda: defaultdict(int))
spawn_records = 0
gen_names = []
for line in read_gbk(os.path.join(ENVIR, "MonGen.txt")).splitlines():
    line = line.strip()
    if not line or line.startswith(";"):
        continue
    # 行内 loadgen (同一行可能多个, 或带路径/参数)
    for m in re.finditer(r'loadgen\s+"([^"]+)"', line, re.I):
        gen_names.append(m.group(1))
    if not gen_names or gen_names[-1] != (re.match(r'loadgen\s+"([^"]+)"', line, re.I).group(1) if re.match(r'loadgen\s+"([^"]+)"', line, re.I) else None):
        pass

MON_DEF = os.path.join(ENVIR, "Mon_Def")
resolved = set()
for gn in gen_names:
    gpath = os.path.join(MON_DEF, gn)
    if not os.path.exists(gpath):
        alt = gn.lstrip("!")
        gpath = os.path.join(MON_DEF, alt)
    if not os.path.exists(gpath):
        for root, _, files in os.walk(MON_DEF):
            if gn in files or (gn.lstrip("!") in files):
                gpath = os.path.join(root, gn if gn in files else gn.lstrip("!"))
                break
        else:
            continue
    resolved.add(os.path.normpath(gpath))

# 兜底: Mon_Def 全量扫描 (MonGen.txt 未引用但实存的刷怪, 如 !Lv3_JumaField / ArcherGuard / Event_*)
for root, _, files in os.walk(MON_DEF):
    for fn in files:
        if fn.endswith(".gen"):
            resolved.add(os.path.normpath(os.path.join(root, fn)))

def parse_gen(p):
    nonlocal_records = 0
    for line in read_gbk(p).splitlines():
        line = line.strip()
        if not line or line.startswith(";") or line.startswith("[") or line.startswith("//"):
            continue
        m = re.match(r"^(\S+)\s+(\d+)\s+(\d+)\s+(.+?)\s+(\d+)\s+(\d+)\s+(\d+)\s*$", line)
        if not m:
            continue
        mp, mon, cnt = m.group(1), m.group(4).strip(), int(m.group(5))
        spawns[mp.lower()][mon] += cnt
        nonlocal_records += 1
    return nonlocal_records

for gpath in sorted(resolved):
    spawn_records += parse_gen(gpath)
json.dump({k: dict(v) for k, v in spawns.items()},
          open("/tmp/mud3_spawns.json", "w", encoding="utf-8"), ensure_ascii=False)

# ---------- 4. merchants ----------
merchants = defaultdict(list)
for line in read_gbk(os.path.join(ENVIR, "Merchant.txt")).splitlines():
    line = line.strip()
    if not line or line.startswith(";"):
        continue
    parts = line.split()
    if len(parts) < 5:
        continue
    merchants[parts[1].lower()].append(parts[4])
json.dump({k: v for k, v in merchants.items()},
          open("/tmp/mud3_merchants.json", "w", encoding="utf-8"), ensure_ascii=False)

# ---------- 5. guards ----------
guards = defaultdict(list)
for line in read_gbk(os.path.join(ENVIR, "GuardList.txt")).splitlines():
    line = line.strip()
    if not line or line.startswith(";"):
        continue
    m = re.match(r"^(\S+)\s+(\S+)", line)
    if m:
        guards[m.group(2).lower()].append(m.group(1))
json.dump({k: v for k, v in guards.items()},
          open("/tmp/mud3_guards.json", "w", encoding="utf-8"), ensure_ascii=False)

# ---------- 6. report_full ----------
ei_files = {fn.lower() for fn in os.listdir(EI_MAP) if fn.lower().endswith(".map")}
mei_files = {fn.lower() for fn in os.listdir(MEI_MAP) if fn.lower().endswith(".map")}
ei_only = sorted(ei_files - mei_files)
mei_only = sorted(mei_files - ei_files)
zh_by_file = {x["file"].lower(): x["zh"] for x in mapinfo}


def term_map(cat):
    out = {}
    p = os.path.join(DOCS, cat)
    if not os.path.exists(p):
        return out
    for line in open(p, encoding="utf-8"):
        m = re.match(r"\|\s*\d+\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|", line)
        if m:
            en, zh = m.group(1).strip(), m.group(2).strip()
            if en and zh and not en.startswith("Index") and zh != "英文原名":
                out[en] = zh
    return out


mon_zh = term_map("08-怪物.md")
map_zh = term_map("10-地图.md")
npc_zh = term_map("09-NPC.md")

report = []
mon_summary = defaultdict(lambda: {"count": 0, "maps": set()})
for em in ei_maps:
    key = em["name"].lower()
    key_noext = key[:-4] if key.endswith(".map") else key
    sp = spawns.get(key) or spawns.get(key_noext) or {}
    r = {
        "file": em["name"],
        "srv_name": zh_by_file.get(key_noext, ""),
        "w": em["w"],
        "h": em["h"],
        "spawns": sorted(sp.items(), key=lambda kv: -kv[1]),
        "merchants": merchants.get(key) or merchants.get(key_noext) or [],
        "guards": guards.get(key) or guards.get(key_noext) or [],
        "ei_only": key in ei_only,
        "mei_only": key in mei_only,
    }
    for mon, cnt in r["spawns"]:
        mon_summary[mon]["count"] += cnt
        mon_summary[mon]["maps"].add(em["name"])
    report.append(r)

for k in mon_summary:
    mon_summary[k]["maps"] = sorted(mon_summary[k]["maps"])

report_full = {
    "report": report,
    "mon_summary": {k: {"count": v["count"], "maps": v["maps"]} for k, v in mon_summary.items()},
    "mon_zh": mon_zh,
    "map_zh": map_zh,
    "npc_zh": npc_zh,
    "ei_only": ei_only,
    "mei_only": mei_only,
    "stats": {"ei_maps": len(ei_files), "mei_maps": len(mei_files),
              "spawn_records": spawn_records},
}
json.dump(report_full, open("/tmp/report_full.json", "w", encoding="utf-8"), ensure_ascii=False)

print(f"ei_maps={len(ei_maps)} mei_maps={len(mei_files)} "
      f"ei_only={len(ei_only)} mei_only={len(mei_only)} "
      f"spawns={len(spawns)} spawn_records={spawn_records} "
      f"merchants={sum(map(len, merchants.values()))} guards={sum(map(len, guards.values()))} "
      f"report={len(report)} mon_kind={len(mon_summary)}")
