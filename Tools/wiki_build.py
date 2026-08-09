#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""wiki_build.py — 构建 EI 3.0 百科数据 JSON。

输入:
  - docs/database/views/  (Zircon System.db 玩家视图, 怪物/装备/技能/NPC/任务/地图)
  - docs/terminology/     (中英对照)
  - /tmp/mud3_*.json      (Mud3 服务端: 刷怪/商人/守卫/地图名)
  - /tmp/ei_maps.json     (EI 客户端 544 图尺寸)
输出: /tmp/wiki_data.json
"""
import json, os, re, sys

ROOT = os.path.dirname(os.path.abspath(__file__))
DOCS = os.path.join(ROOT, "..", "docs")

def read(p):
    with open(p, encoding="utf-8") as f:
        return f.read()

# ---------------------------------------------------------------- views: monsters
def parse_monsters(text):
    """### NNN · Name · Lv 级 / - 属性 / - 特征 / - 刷新 / - 掉落"""
    out = {}
    cur = None
    for line in text.splitlines():
        line = line.strip()
        m = re.match(r"### (\d+) · (.+?)(?: · (\d+) 级)?$", line)
        if m:
            cur = {"id": int(m.group(1)), "name": m.group(2),
                   "level": int(m.group(3)) if m.group(3) else None,
                   "attrs": [], "traits": "", "spawns": "", "drops": "",
                   "zh": "", "boss": False, "undead": False}
            out[cur["id"]] = cur
            continue
        if cur is None: continue
        if line.startswith("- 属性："): cur["attrs"] = line.split("：", 1)[1].split(" · ")
        elif line.startswith("- 特征："): cur["traits"] = line.split("：", 1)[1]
        elif line.startswith("- 刷新："): cur["spawns"] = line.split("：", 1)[1]
        elif line.startswith("- 掉落："): cur["drops"] = line.split("：", 1)[1]
        elif line.startswith("### Boss"): pass
    for v in out.values():
        t = v["traits"]
        v["boss"] = "Boss" in t
        v["undead"] = "亡灵" in t
        v["tame"] = "可捕捉" in t
    return out

# ---------------------------------------------------------------- views: items
def parse_items(text, category):
    """### NNN · Name（职业） / - 属性 / - 类型… / - 掉落 / - 套装 / - 说明"""
    out = {}
    cur = None
    for line in text.splitlines():
        line = line.strip()
        m = re.match(r"### (\d+) · (.+?)(?:（(.+?)）)?$", line)
        if m:
            cur = {"id": int(m.group(1)), "name": m.group(2),
                   "class": m.group(3) or "全职业", "category": category,
                   "type_zh": "", "attrs": [], "meta": "", "drops": "", "set": "", "desc": "",
                   "zh": ""}
            out[cur["id"]] = cur
            continue
        if cur is None: continue
        if line.startswith("- 属性："): cur["attrs"] = line[4:].split(" · ")
        elif line.startswith("- 类型"):
            cur["meta"] = line[2:]
            tm = re.match(r"类型\s*(\S+)", line[2:])
            if tm: cur["type_zh"] = tm.group(1)
        elif line.startswith("- 掉落："): cur["drops"] = line[4:]
        elif line.startswith("- 套装："): cur["set"] = line[4:]
        elif line.startswith("- 说明："): cur["desc"] = line[4:]
    return out

# ---------------------------------------------------------------- views: skills
def parse_skills(text):
    """### N · Name / - 类型 / - 威力 / - 耗蓝 / - 等级门槛 / - 说明; 职业分组标题"""
    out = {}
    cur = None
    klass = None
    for line in text.splitlines():
        line = line.strip()
        if line.startswith("## "):
            m = re.match(r"## (.+?)（(\d+) 个）", line)
            if m: klass = m.group(1)
            continue
        m = re.match(r"### (\d+) · (.+)$", line)
        if m:
            cur = {"id": int(m.group(1)), "name": m.group(2), "klass": klass,
                   "type": "", "school": "", "prop": "", "power": "", "cost": "",
                   "delay": "", "levels": "", "exp": "", "icon": "", "desc": "",
                   "zh": ""}
            out[cur["id"]] = cur
            continue
        if cur is None: continue
        if line.startswith("- 类型："):
            for seg in line.split(" · "):
                k, _, v = seg.partition("：")
                k = k.strip()
                if k.endswith("类型"): cur["type"] = v
                elif k.endswith("派系"): cur["school"] = v
                elif k.endswith("属性"): cur["prop"] = v
                elif k.endswith("图标"): cur["icon"] = v
        elif line.startswith("- 威力：") or line.startswith("- 耗蓝：") or line.startswith("- 延迟："):
            for seg in line.split(" · "):
                k, _, v = seg.partition("：")
                k = k.strip()
                if k.endswith("威力"): cur["power"] = v
                elif k.endswith("耗蓝"): cur["cost"] = v
                elif k.endswith("延迟"): cur["delay"] = v
        elif line.startswith("- 等级门槛："):
            for seg in line.split(" · "):
                k, _, v = seg.partition("：")
                k = k.strip()
                if k.endswith("等级门槛"): cur["levels"] = v
                elif k.endswith("熟练度"): cur["exp"] = v
        elif line.startswith("- 说明："): cur["desc"] = line.split("：", 1)[1]
    return out

# ---------------------------------------------------------------- views: npcs / quests
def parse_npcs(text):
    """### NNN · Name / - 地图 M · 图标 I · 头像 A / - 介绍 / - 可接任务"""
    out = {}
    cur = None
    for line in text.splitlines():
        line = line.strip()
        m = re.match(r"### (\d+) · (.+)$", line)
        if m:
            cur = {"id": int(m.group(1)), "name": m.group(2),
                   "map": None, "icon": None, "face": None, "desc": "",
                   "quests_in": 0, "quests_out": 0, "zh": ""}
            out[cur["id"]] = cur
            continue
        if cur is None: continue
        m = re.match(r"- 地图 (\S+) · 图标 (\d+) · 头像 (\d+)", line)
        if m:
            cur["map"] = m.group(1); cur["icon"] = int(m.group(2)); cur["face"] = int(m.group(3))
            q = re.search(r"可接任务 (\d+) 个 · 可交任务 (\d+) 个", line)
            if q:
                cur["quests_in"] = int(q.group(1)); cur["quests_out"] = int(q.group(2))
            continue
        if line.startswith("- 介绍："): cur["desc"] = line.split("：", 1)[1]
    return out

def parse_quests(text):
    """### N · Name（Type）/ - 接取 / - 说明 / - 目标 / - 奖励"""
    out = {}
    cur = None
    for line in text.splitlines():
        line = line.strip()
        m = re.match(r"### (\d+) · (.+?)(?:（(.+?)）)?$", line)
        if m:
            cur = {"id": int(m.group(1)), "name": m.group(2),
                   "type": m.group(3) or "", "npc": "", "desc": "",
                   "goals": "", "rewards": "", "zh": ""}
            out[cur["id"]] = cur
            continue
        if cur is None: continue
        if line.startswith("- 接取："): cur["npc"] = line[4:]
        elif line.startswith("- 说明："): cur["desc"] = line[4:]
        elif line.startswith("- 目标："): cur["goals"] = line[4:]
        elif line.startswith("- 奖励："): cur["rewards"] = line[4:]
    return out

# ---------------------------------------------------------------- views: maps
def parse_maps(text):
    """### N · Name / - 文件 F · 等级 / - 怪物：Name ×N、…"""
    out = {}
    cur = None
    for line in text.splitlines():
        line = line.strip()
        m = re.match(r"### (\d+) · (.+)$", line)
        if m:
            cur = {"id": int(m.group(1)), "name": m.group(2),
                   "file": "", "env": "", "monsters": [], "zh": ""}
            out[cur["id"]] = cur
            continue
        if cur is None: continue
        m = re.match(r"- 文件 (\S+)", line)
        if m:
            cur["file"] = m.group(1)
            rest = line[m.end():]
            m2 = re.search(r"· (.+)$", rest)
            if m2: cur["env"] = m2.group(1)
            continue
        if line.startswith("- 怪物："):
            for seg in line.split("：", 1)[1].split("、"):
                mm = re.match(r"(.+?) ×(\d+)(?:（Boss）)?", seg.strip())
                if mm:
                    cur["monsters"].append({"name": mm.group(1), "count": int(mm.group(2)),
                                            "boss": "（Boss）" in seg})
    return out

# ---------------------------------------------------------------- terminology
def parse_terminology(cat_files):
    """术语表: | Index | English | 中文 | 备注 |  → {english: chinese}"""
    out = {}
    for cat, path in cat_files.items():
        text = read(os.path.join(DOCS, "terminology", path))
        for line in text.splitlines():
            m = re.match(r"\|\s*\d+\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|", line)
            if m:
                en, zh = m.group(1).strip(), m.group(2).strip()
                if en and zh and not en.startswith("Index") and zh != "英文原名":
                    out[en] = zh
    return out

# ---------------------------------------------------------------- companion
def parse_companion(text):
    """### #N · Name (#MonsterIdx) / | MonsterInfo | … | Price | … | Available | …"""
    out = {}
    cur = None
    for line in text.splitlines():
        line = line.strip()
        m = re.match(r"### #(\d+) · (.+?) \(#(\d+)\)", line)
        if m:
            cur = {"id": int(m.group(1)), "name": m.group(2),
                   "monster_id": int(m.group(3)), "price": None, "available": None}
            out[cur["id"]] = cur
            continue
        if cur is None: continue
        m = re.match(r"\| Price \| (.+?) \|", line)
        if m:
            cur["price"] = int(m.group(1).replace(",", ""))
            continue
        m = re.match(r"\| Available \| (\w+) \|", line)
        if m:
            cur["available"] = m.group(1) == "true"
    return out

def main():
    views = os.path.join(DOCS, "database", "views")
    monsters = parse_monsters(read(os.path.join(views, "monsters.md")))
    items = {}
    for cat in ["weapons", "armour", "jewellery", "consumables", "materials"]:
        items.update(parse_items(read(os.path.join(views, "items", f"{cat}.md")), cat))
    skills = parse_skills(read(os.path.join(views, "skills.md")))
    npcs = parse_npcs(read(os.path.join(views, "npcs.md")))
    quests = parse_quests(read(os.path.join(views, "quests.md")))
    maps = parse_maps(read(os.path.join(views, "maps.md")))
    companion = parse_companion(read(os.path.join(DOCS, "database", "data", "CompanionInfo.md")))

    # 术语表
    term = parse_terminology({
        "monster": "08-怪物.md", "item": "03-武器.md",
        "map": "10-地图.md", "npc": "09-NPC.md", "skill": "02-技能.md",
    })
    # 补充: 03-武器.md 只是武器; 04-06/07 防具首饰道具材料技能书
    for path in ["04-防具.md", "05-首饰.md", "06-道具与材料.md", "07-技能书与宠物用品.md"]:
        for en, zh in parse_terminology({"x": path}).items():
            term.setdefault(en, zh)

    # 应用中文名
    for v in monsters.values():
        v["zh"] = term.get(v["name"], v["name"])
    for v in items.values():
        v["zh"] = term.get(v["name"], v["name"])
    for v in skills.values():
        v["zh"] = term.get(v["name"], v["name"])
    for v in npcs.values():
        v["zh"] = term.get(v["name"], v["name"])
    for v in quests.values():
        v["zh"] = term.get(v["name"], v["name"])
    for v in maps.values():
        v["zh"] = term.get(v["name"], v["name"])

    # Mud3 服务端数据
    mud3 = {
        "spawns": json.load(open("/tmp/mud3_spawns.json", encoding="utf-8")),
        "merchants": json.load(open("/tmp/mud3_merchants.json", encoding="utf-8")),
        "guards": json.load(open("/tmp/mud3_guards.json", encoding="utf-8")),
        "mapinfo": json.load(open("/tmp/mud3_mapinfo.json", encoding="utf-8")),
    }
    ei_maps = json.load(open("/tmp/ei_maps.json", encoding="utf-8"))

    data = {
        "monsters": list(monsters.values()),
        "items": list(items.values()),
        "skills": list(skills.values()),
        "npcs": list(npcs.values()),
        "quests": list(quests.values()),
        "maps": list(maps.values()),
        "companion": list(companion.values()),
        "mud3": mud3,
        "ei_maps": ei_maps,
        "terminology": term,
        "stages": read(os.path.join(DOCS, "notes", "22-传奇EI2.0资料整理-地图装备技能阶段.md")),
        "diff": read(os.path.join(DOCS, "EI_CLIENT_DIFF_2026-08-09.md")),
    }
    out = "/tmp/wiki_data.json"
    with open(out, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False)
    print(f"written {out}  "
          f"monsters={len(data['monsters'])} items={len(data['items'])} "
          f"skills={len(data['skills'])} npcs={len(data['npcs'])} "
          f"quests={len(data['quests'])} maps={len(data['maps'])} "
          f"spawns={len(mud3['spawns'])} merchants={len(mud3['merchants'])} "
          f"guards={len(mud3['guards'])} mapinfo={len(mud3['mapinfo'])} "
          f"ei_maps={len(ei_maps)}")

if __name__ == "__main__":
    main()
