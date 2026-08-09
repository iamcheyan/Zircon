#!/usr/bin/env python3
"""三版本归属分析 v5: 双层匹配 (中文词根 + 英文族关键词)

修正 v4 的重大误判:
  93 个「MUD3 独有」中, 诺玛/祖玛/沃玛/骷髅/蜘蛛/蚂蚁/沙漠 等
  在 Zircon 有对应英文名族 (Numa/Zuma/Oma/Skeleton/Spider/Ant/Desert/Sand),
  只是术语表中文名缺失 -> 应为「共享(术语表缺口)」而非「真独有」。

匹配规则 (按优先级):
  1. 中文名全名/词根命中 Zircon 中文名       -> 共享
  2. MUD3 中文名族关键词 -> Zircon 英文名族   -> 共享
  3. 否则 -> MUD3 真独有
输出 /tmp/three_versions.json
"""
import json, os, re, glob
from datetime import datetime, timezone
from collections import Counter

NAS = "/home/tetsuya/NAS/TMP"
MUD3 = os.path.join(NAS, "Mud3", "Envir")

w = json.load(open("/tmp/wiki_data.json"))
r = json.load(open("/tmp/report_full.json"))
term = w["terminology"]

def base(n): return re.sub(r"[\s\d]+$", "", n)

# ---------- 1. 地图 (同 v4) ----------
ei = {m["name"].lower() for m in w["ei_maps"]}
mei = {f.lower() for f in os.listdir(os.path.join(NAS, "mir3ei", "Map")) if f.lower().endswith(".map")}
zir = {m["file"].lower() + ".map" for m in w["maps"]}
mud3_codes = {}
for line in open(os.path.join(MUD3, "Mapinfo.txt"), encoding="gbk", errors="replace"):
    line = line.strip()
    if not line.startswith("[") or line.startswith(";;"): continue
    m = re.match(r"^\[(\S+)\s+(\S+)", line)
    if m: mud3_codes[m.group(1).lower()] = m.group(2)
mud3 = {c + ".map" for c in mud3_codes}

def zh_of(f): return mud3_codes.get(f[:-4], "")

maps = {
    "ei_total": len(ei), "mei_total": len(mei),
    "zir_total": len(zir), "mud3_total": len(mud3_codes),
    "mud3_only": sorted(mud3 - ei - mei - zir),
    "mud3_only_zh": {f: zh_of(f) for f in sorted(mud3 - ei - mei - zir)},
    "mei_only": sorted(mei - ei - zir - mud3),
    "zir_only": sorted(zir - ei - mei - mud3),
    "ei_only": sorted(ei - mei - zir - mud3),
    "mei_mud3_shared": sorted((mei & mud3) - ei - zir),
    "mud3_zir_shared": sorted((mud3 & zir) - ei - mei),
    "mei_zir_shared": sorted((mei & zir) - ei - mud3),
    "three_wo_ei": sorted((mei & zir & mud3) - ei),
    "core": sorted(ei & mei & zir & mud3),
}
for k in ["mei_mud3_shared", "mud3_zir_shared", "mei_zir_shared", "three_wo_ei", "core"]:
    maps[k + "_zh"] = {f: zh_of(f) for f in maps[k]}

# ---------- 2. 怪物: 双层匹配 ----------
mud3_mon = dict(r["mon_summary"])
mud3_agg = {}
for zh, info in mud3_mon.items():
    b = base(zh)
    a = mud3_agg.setdefault(b, {"count": 0, "maps": set(), "variants": []})
    a["count"] += info["count"]
    a["maps"].update(info["maps"])
    a["variants"].append(zh)

zir_mon = w["monsters"]
zir_zh = set()
for m in zir_mon:
    zh = term.get(m["name"])
    zir_zh.add(zh if zh else m["name"])

# 中文名族 -> Zircon 英文名族关键词 (人工整理的权威映射)
FAMILY = {
    "诺玛": ["numa", "noma"],
    "祖玛": ["zuma"],
    "沃玛": ["oma", "worm"],
    "骷髅": ["skeleton", "bone"],
    "僵尸": ["zombie", "thirsty"],
    "蜘蛛": ["spider"],
    "蚂蚁": ["ant"],
    "沙漠": ["sand", "desert"],
    "蛇": ["snake", "serpent"],
    "蝎": ["scorpion"],
    "蛆": ["maggot", "worm"],
    "蜈蚣": ["centipede"],
    "蜂": ["bee", "flea"],
    "甲虫": ["beetle", "bug"],
    "蛾": ["moth"],
    "蝙蝠": ["bat"],
    "猫": ["cat"],
    "猪": ["boar", "pig"],
    "狼": ["wolf"],
    "鹿": ["deer"],
    "鸡": ["chicken", "hen"],
    "牛": ["cow", "bull", "ox"],
    "雪人": ["yeti"],
    "树": ["tree"],
    "花": ["flower", "plant"],
    "虫": ["worm", "bug", "insect"],
    "恶魔": ["demon", "devil"],
    "半兽": ["orc", "ogre"],
    "精灵": ["elf", "spirit"],
    "鬼": ["ghost", "spirit", "spectre"],
    "幽灵": ["ghost", "phantom"],
    "石": ["golem", "rock", "stone"],
    "卫士": ["guard"],
    "战士": ["warrior", "grunt", "soldier"],
    "法老": ["mage", "pharaoh", "elder"],
    "教主": ["king", "lord", "master"],
    "神": ["god", "lord", "king"],
    "龙": ["dragon"],
    "鹰": ["eagle", "hawk"],
    "鲨": ["shark"],
    "章鱼": ["octopus"],
    "蛙": ["frog", "toad"],
    "蛤蟆": ["frog", "toad"],
}

def match(name):
    """返回 (匹配类型, Zircon 侧命中)。None 表示真独有。"""
    if name in zir_zh:
        return ("zh_exact", name)
    # 中文词根: 双向包含
    for z in zir_zh:
        if not z: continue
        if len(z) >= 2 and z in name:
            return ("zh_contains", z)
        if len(name) >= 2 and name in z:
            return ("zh_in", z)
    # 英文族关键词
    for kw, en in FAMILY.items():
        if kw in name:
            hits = [mm["name"] for mm in zir_mon
                    if any(e in mm["name"].lower() for e in en)]
            if hits:
                return ("en_family", f"{kw}->{hits[0]}+{len(hits)}")
    return None

mud3_only, shared = [], []
for b, a in sorted(mud3_agg.items(), key=lambda x: -x[1]["count"]):
    hit = match(b)
    if hit:
        shared.append({"zh": b, "match": hit[0], "zir": hit[1],
                       "count": a["count"], "maps": len(a["maps"]),
                       "variants": a["variants"]})
    else:
        mud3_only.append({"zh": b, "count": a["count"],
                          "maps": sorted(a["maps"]), "variants": a["variants"]})

monsters = {
    "mud3_total": len(mud3_agg), "zir_total": len(zir_mon),
    "mud3_only": mud3_only, "mud3_only_count": len(mud3_only),
    "shared": shared, "shared_count": len(shared),
}

# ---------- 3. 装备 (同 v4) ----------
mud3_items = set()
pat = re.compile(r"^\s*1/\d+\s+(.+)$")
for fn in glob.glob(os.path.join(MUD3, "MonItems", "*.txt")):
    for line in open(fn, encoding="gbk", errors="replace"):
        m = pat.match(line)
        if m:
            name = m.group(1).strip()
            if name.startswith("金币"): continue
            mud3_items.add(name)

zir_item_zh = set()
for it in w["items"]:
    zh = term.get(it["name"])
    zir_item_zh.add(zh if zh else it["name"])

mud3_only_items = sorted(n for n in mud3_items
                         if n not in zir_item_zh and base(n) not in zir_item_zh)
items = {
    "mud3_total": len(mud3_items), "zir_total": len(w["items"]),
    "mud3_only": mud3_only_items, "mud3_only_count": len(mud3_only_items),
}

# ---------- 4. 技能 ----------
klass_count = Counter(s["klass"] for s in w["skills"])
skills = {
    "zir_total": len(w["skills"]),
    "klass_dist": dict(klass_count),
    "assassin_count": klass_count.get("刺客", 0),
    "assassin_skills": [s["name"] for s in w["skills"] if s["klass"] == "刺客"],
    "three_class_count": sum(v for k, v in klass_count.items() if k != "刺客"),
}

out = {"maps": maps, "monsters": monsters, "items": items, "skills": skills,
       "_meta": {"generated_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
                 "script": "three_versions_check.py"}}
json.dump(out, open("/tmp/three_versions.json", "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)

print("== 怪物 (双层匹配) ==")
print(f"MUD3 聚合 {monsters['mud3_total']} / Zircon {monsters['zir_total']}")
print(f"共享 {monsters['shared_count']} 种 (含英文族匹配):")
for s in shared[:40]:
    print(f"  {s['zh']} x{s['count']} [{s['match']}] -> {s['zir']}")
print(f"MUD3 真独有 {monsters['mud3_only_count']} 种:")
for m in mud3_only[:40]:
    print(f"  {m['zh']} x{m['count']} ({len(m['maps'])}图)")
