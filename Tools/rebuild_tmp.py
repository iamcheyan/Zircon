#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""rebuild_tmp.py — 重建 /tmp 数据产物(在 NAS 挂载恢复后运行)。

生成(全部由 NAS/TMP 原始数据聚合):
  /tmp/ei_maps.json            EI 客户端 Map/ 544 张 [{name,w,h}]
  /tmp/mud3_spawns.json        Mon_Def/*.gen 刷怪聚合 (GBK)
  /tmp/mud3_merchants.json     Merchant.txt 商人
  /tmp/mud3_guards.json        GuardList.txt 守卫
  /tmp/mud3_mapinfo.json       Mapinfo.txt 代码→中文名 (GBK)
  /tmp/report_full.json        544 图 × 刷怪/商人/守卫 + 怪物图鉴 + 差异
"""
import json, os, re, struct, sys
from collections import Counter, defaultdict

NAS = "/home/tetsuya/NAS/TMP"
EI_MAP = os.path.join(NAS, "EI传奇3.0客户端", "Map")
MEI_MAP = os.path.join(NAS, "mir3ei", "Map")
ENVIR = os.path.join(NAS, "Mud3", "Envir")
MON_DEF = os.path.join(ENVIR, "Mon_Def")

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from wiki_build import parse_terminology, DOCS

# ---------------------------------------------------------------- 1. ei_maps
def map_dims(path):
    try:
        with open(path, "rb") as f:
            head = f.read(28)
        if len(head) < 28:
            return None
        w, h = struct.unpack_from("<HH", head, 22)
        return w, h
    except Exception:
        return None

def build_ei_maps():
    out = []
    for fn in sorted(os.listdir(EI_MAP)):
        if not fn.lower().endswith(".map"):
            continue
        dims = map_dims(os.path.join(EI_MAP, fn))
        if dims:
            out.append({"name": fn, "w": dims[0], "h": dims[1]})
    json.dump(out, open("/tmp/ei_maps.json", "w", encoding="utf-8"), ensure_ascii=False)
    print(f"ei_maps.json: {len(out)} 张")

# ---------------------------------------------------------------- 2. spawns
def build_spawns():
    """Mon_Def/*.gen: 行 = 地图 X Y 怪物名 数量 延迟 范围 (GBK)。"""
    rows = []          # (mapcode, mon, n)
    for fn in os.listdir(MON_DEF):
        if not fn.lower().endswith(".gen"):
            continue
        with open(os.path.join(MON_DEF, fn), encoding="gbk", errors="replace") as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith(";") or line.startswith("["):
                    continue
                parts = line.split()
                if len(parts) < 4:
                    continue
                code, mon = parts[0], parts[3]
                try:
                    n = int(parts[4]) if len(parts) > 4 else 1
                except ValueError:
                    n = 1
                rows.append((code, mon, n))
    by_map = defaultdict(Counter)
    summary = defaultdict(lambda: {"count": 0, "maps": set()})
    for code, mon, n in rows:
        by_map[code][mon] += n
        summary[mon]["count"] += n
        summary[mon]["maps"].add(code)
    global _spawn_records
    _spawn_records = len(rows)
    json.dump({k: [[m, c] for m, c in v.items()] for k, v in by_map.items()},
              open("/tmp/mud3_spawns.json", "w", encoding="utf-8"), ensure_ascii=False)
    json.dump({k: {"count": v["count"], "maps": sorted(v["maps"])} for k, v in summary.items()},
              open("/tmp/mud3_mon_summary.json", "w", encoding="utf-8"), ensure_ascii=False)
    print(f"mud3_spawns.json: {len(rows)} 条刷怪, {len(by_map)} 图, {len(summary)} 种")

# ---------------------------------------------------------------- 3. merchants
def build_merchants():
    """Merchant.txt: 行 = 脚本 地图 X Y 名字 脸 身 (GBK)。"""
    out = []
    path = os.path.join(ENVIR, "Merchant.txt")
    for line in open(path, encoding="gbk", errors="replace"):
        line = line.strip()
        if not line or line.startswith(";") or line.startswith("["):
            continue
        parts = line.split()
        if len(parts) < 5:
            continue
        script, mp, x, y, name = parts[0], parts[1], parts[2], parts[3], parts[4]
        try:
            out.append({"name": name, "map": mp, "x": int(x), "y": int(y), "script": script})
        except ValueError:
            continue
    json.dump(out, open("/tmp/mud3_merchants.json", "w", encoding="utf-8"), ensure_ascii=False)
    print(f"mud3_merchants.json: {len(out)} 商人")

# ---------------------------------------------------------------- 4. guards
def build_guards():
    """GuardList.txt: 行 = 名字 地图 x,y : dir (GBK)。"""
    out = []
    path = os.path.join(ENVIR, "GuardList.txt")
    for line in open(path, encoding="gbk", errors="replace"):
        line = line.strip()
        if not line or line.startswith(";"):
            continue
        parts = line.split()
        if len(parts) < 3:
            continue
        name, mp = parts[0], parts[1]
        out.append({"name": name, "map": mp})
    json.dump(out, open("/tmp/mud3_guards.json", "w", encoding="utf-8"), ensure_ascii=False)
    print(f"mud3_guards.json: {len(out)} 守卫")

# ---------------------------------------------------------------- 5. mapinfo
def build_mapinfo():
    """Mapinfo.txt: [code 中文名 flags] (GBK)。"""
    out = {}
    path = os.path.join(ENVIR, "Mapinfo.txt")
    for line in open(path, encoding="gbk", errors="replace"):
        line = line.strip()
        if not line.startswith("[") or line.startswith(";;"):
            continue
        m = re.match(r"^\[(\S+)\s+(\S+)", line)
        if m:
            out[m.group(1)] = m.group(2)
    json.dump(out, open("/tmp/mud3_mapinfo.json", "w", encoding="utf-8"), ensure_ascii=False)
    print(f"mud3_mapinfo.json: {len(out)} 个地图代码")

# ---------------------------------------------------------------- 6. report_full
def build_report_full():
    ei_files = sorted(fn for fn in os.listdir(EI_MAP) if fn.lower().endswith(".map"))
    mei_files = sorted(fn for fn in os.listdir(MEI_MAP) if fn.lower().endswith(".map"))
    ei_lower = {fn.lower() for fn in ei_files}
    mei_lower = {fn.lower() for fn in mei_files}
    ei_only = sorted(fn for fn in ei_files if fn.lower() not in mei_lower)
    mei_only = sorted(fn for fn in mei_files if fn.lower() not in ei_lower)

    mapinfo = json.load(open("/tmp/mud3_mapinfo.json", encoding="utf-8"))
    spawns = json.load(open("/tmp/mud3_spawns.json", encoding="utf-8"))
    mon_summary = json.load(open("/tmp/mud3_mon_summary.json", encoding="utf-8"))
    merchants = json.load(open("/tmp/mud3_merchants.json", encoding="utf-8"))
    guards = json.load(open("/tmp/mud3_guards.json", encoding="utf-8"))

    term_mon = parse_terminology({"monster": "08-怪物.md"})
    term_map = parse_terminology({"map": "10-地图.md"})
    term_npc = parse_terminology({"npc": "09-NPC.md"})
    wiki = json.load(open("/tmp/wiki_data.json", encoding="utf-8"))
    zir_codes = {m["file"].lower() for m in wiki["maps"] if m.get("file")}

    report = []
    for fn in ei_files:
        code = fn[:-4]
        srv = mapinfo.get(code, "")
        dims = map_dims(os.path.join(EI_MAP, fn))
        entry = {
            "file": fn,
            "srv_name": srv,
            "w": dims[0] if dims else 0,
            "h": dims[1] if dims else 0,
            "spawns": [[m, c] for m, c in sorted(spawns.get(code, []), key=lambda x: -x[1])],
            "merchants": [me["name"] for me in merchants if me["map"] == code],
            "guards": [g["name"] for g in guards if g["map"] == code],
            "ei_only": fn.lower() not in mei_lower,
            "mei_only": False,
            "in_zircon": code.lower() in zir_codes,
        }
        report.append(entry)

    stats = {
        "ei_maps": len(ei_files),
        "mei_maps": len(mei_files),
        "spawn_records": _spawn_records,
        "mon_kinds": len(mon_summary),
        "merchants": len(merchants),
        "guards": len(guards),
        "spawn_maps": sum(1 for v in spawns.values() if v),
    }
    data = {
        "report": report,
        "mon_summary": mon_summary,
        "mon_zh": term_mon,
        "map_zh": term_map,
        "npc_zh": term_npc,
        "ei_only": ei_only,
        "mei_only": mei_only,
        "stats": stats,
        "merchants_all": merchants,
        "guards_all": guards,
    }
    json.dump(data, open("/tmp/report_full.json", "w", encoding="utf-8"), ensure_ascii=False)
    print(f"report_full.json: {len(report)} 图, ei_only={len(ei_only)}, mei_only={len(mei_only)}, "
          f"刷怪={stats['spawn_records']} 商人={stats['merchants']} 守卫={stats['guards']}")

if __name__ == "__main__":
    build_ei_maps()
    build_spawns()
    build_merchants()
    build_guards()
    build_mapinfo()
    build_report_full()
