#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""dat_integrate.py — 老版 EI2.0 服务端三 DAT 条目接入 wiki 数据链。

读取:
  - docs/research/mud3-dat-decoded/{stditem,magic,monster}.json（解码结果, 已含 tag/tag_note）
  - docs/research/mud3-dat-decoded/build_comparison.py 的映射表（SKILL_MAP/ITEM_MAP/MONSTER_MAP）
  - /tmp/three_versions.json（mud3_only 怪物 30 / 装备 289）
  - /tmp/report_full.json（各图 spawns, 用于按变体名聚合真实刷怪量）
输出: /tmp/wiki_dat.json
  monsters/items/skills — 老版独有条目卡（负 id, ver=["mud3"], legacy=True, 按名去重）
  ref.{monsters,items,skills} — both/changed 老版记录挂靠（zir_id -> {source,tag,tag_note,fields}）

诚实原则:
  - 老版条目显示完整解码属性, 标注来源（*.dat 文件名）与判定标签;
  - 有 Zircon 同名/锚点映射 → 挂靠不建卡（避免主列表双卡泛滥）;
  - 无图（Appr 越界 / Looks 越界 / 帧空）→ img=None 占位, 不伪造;
  - 刷怪量从 report_full.json 按变体名（去尾数字）精确聚合, 不伪造每图数量。
"""
from __future__ import annotations

import json
import os
import re
import sys
from collections import Counter
from datetime import datetime, timezone

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DOCS = os.path.join(ROOT, "docs")
DAT_DIR = os.path.join(DOCS, "research", "mud3-dat-decoded")
sys.path.insert(0, DAT_DIR)
sys.path.insert(0, ROOT)
sys.path.insert(0, os.path.join(ROOT, "Tools", "reverse-engineering"))

import wil_probe  # noqa: E402

EI_CLIENT = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端"
EI_DATA = os.path.join(EI_CLIENT, "Data")

STDMODE_ZH = {0: "药品", 5: "武器", 10: "衣服", 11: "衣服", 20: "首饰", 22: "戒指",
              24: "手镯", 25: "药粉材料", 26: "手镯", 30: "杂物", 31: "药包",
              40: "材料", 41: "货币", 51: "技能书", 58: "任务", 99: "特殊"}
SCHOOL_ZH = {0: "火", 1: "冰", 2: "雷", 3: "风", 4: "治疗", 5: "符咒",
             6: "召唤", 7: "战技", 99: "装备"}
KIND_ZH = {0: "战技/被动", 1: "攻击魔法", 2: "辅助"}


def strip_digits(name):
    """去尾部数字后缀（变体 0/8/61/62/70… 及 '名 2' 式）→ 基础名。"""
    return re.sub(r"\s*\d+\s*$", "", name or "")


def iso():
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


# ============================================================ 数据载入
w = json.load(open("/tmp/wiki_data.json", encoding="utf-8"))
tv = json.load(open("/tmp/three_versions.json", encoding="utf-8"))
report = json.load(open("/tmp/report_full.json", encoding="utf-8"))

stditem = json.load(open(os.path.join(DAT_DIR, "stditem.json"), encoding="utf-8"))
magic = json.load(open(os.path.join(DAT_DIR, "magic.json"), encoding="utf-8"))
monster = json.load(open(os.path.join(DAT_DIR, "monster.json"), encoding="utf-8"))

import build_comparison as bc  # noqa: E402


# ============================================================ 刷怪聚合（变体名 → 每图数量）
def build_spawn_agg():
    """report_full 各图 spawns → {基础名: {图码: 数量}}（真实聚合, 不变体伪造）。"""
    agg = {}
    for rep in report["report"]:
        code = rep["file"].lower().removesuffix(".map")
        for name, count in rep["spawns"]:
            base = strip_digits(name)
            d = agg.setdefault(base, {})
            d[code] = d.get(code, 0) + count
    return agg


def spawn_str(agg_map):
    """{图码: 数量} → 'MAP ×N、MAP ×N'（按数量降序）。"""
    if not agg_map:
        return ""
    return "、".join(f"{c} ×{n}" for c, n in
                     sorted(agg_map.items(), key=lambda x: -x[1]))


SPAWN_AGG = build_spawn_agg()


# ============================================================ 怪物
def mon_img(appr):
    """老版 Appr → Mon-{Appr//10}.wil 帧 (Appr%10)*1000+40; 解不出 → None。"""
    if not appr or appr <= 0:
        return None
    lib = f"Mon-{appr // 10}.wil"
    frame = (appr % 10) * 1000 + 40
    if not (wil_probe.frame_ok(EI_DATA, lib, frame)
            or wil_probe.frame_ok(wil_probe.MEI_DATA, lib, frame)):
        return None
    return {"lib": lib, "frame": frame, "src": "ei"}


def mon_attrs(r):
    out = []
    out.append(f"生命 {r['HP']}")
    out.append(f"物防 {r['ACMin']}" if r["ACMin"] == r["ACMax"] else f"物防 {r['ACMin']}-{r['ACMax']}")
    out.append(f"魔防 {r['MAC']}")
    out.append(f"物攻 {r['DCMin']}-{r['DCMax']}")
    if r["MC"]:
        out.append(f"魔法 {r['MC']}")
    out.append(f"经验 {r['Exp']}")
    out.append(f"外观 {r['Appr']}")
    if r["Strength"]:
        out.append(f"强度 {r['Strength']}")
    return out


def mon_fields(r):
    """挂靠用字段（中文标签 → 值, 展示友好）。"""
    return {
        "等级": r["Level"], "生命 HP": r["HP"],
        "物防 AC": r["ACMin"] if r["ACMin"] == r["ACMax"] else f"{r['ACMin']}-{r['ACMax']}",
        "魔防 MAC": r["MAC"],
        "物攻 DC": f"{r['DCMin']}-{r['DCMax']}",
        "经验": r["Exp"], "外观 Appr": r["Appr"], "种族 Race": r["Race"],
    }


MONSTER_PREFIX = [(p, zid) for p, zid, _ in bc.MONSTER_MAP if zid is not None]


def monster_zir_id(name):
    """老版怪物名（基础名）→ Zircon id（前缀锚点, 仅在 tag=both/changed 时使用）。"""
    for prefix, zid in MONSTER_PREFIX:
        if name == prefix or name.startswith(prefix):
            return zid
    return None


old_monsters = {}
ref_monsters = {}
_n = [0]


def new_mid():
    _n[0] -= 1
    return _n[0]


# 1) DAT 记录 → 卡 / 挂靠（先按基础名去重: 变体并入主记录）
dat_bases = {}
for r in monster["records"]:
    if not r.get("Name") or not r.get("Index"):
        continue
    base = strip_digits(r["Name"])
    if base not in dat_bases or r["Name"] == base:
        dat_bases[base] = r

for base, r in sorted(dat_bases.items(), key=lambda x: x[0]):
    zid = monster_zir_id(base) if r.get("tag") in ("both", "changed") else None
    if zid is not None:
        ref_monsters[str(zid)] = {"source": "monster.dat", "tag": r["tag"],
                                  "tag_note": r.get("tag_note", ""),
                                  "fields": mon_fields(r)}
        continue
    if base in old_monsters:
        continue
    old_monsters[base] = {
        "id": new_mid(), "name": base, "zh": base, "legacy": True,
        "source": "monster.dat", "tag": r.get("tag", "unverified"),
        "tag_note": r.get("tag_note", ""),
        "level": r["Level"], "attrs": mon_attrs(r),
        "traits": f"老版怪物（monster.dat）· 外观 {r['Appr']}",
        "spawns": spawn_str(SPAWN_AGG.get(base, {})),
        "drops": f"掉落组 #{r['DropTable']}" if r["DropTable"] else "",
        "boss": False, "undead": False, "tame": False,
        "ver": ["mud3"], "img": mon_img(r["Appr"]),
        "dat": {"Index": r["Index"], "Appr": r["Appr"], "Race": r["Race"],
                "HP": r["HP"], "Exp": r["Exp"], "ACMin": r["ACMin"], "ACMax": r["ACMax"],
                "MAC": r["MAC"], "DCMin": r["DCMin"], "DCMax": r["DCMax"],
                "Level": r["Level"], "DropTable": r["DropTable"]},
    }

# 2) three_versions mud3_only（刷怪驱动）并入: 有 DAT 卡 → 复用（补来源标注）; 无 → 纯刷怪卡
for x in tv["monsters"]["mud3_only"]:
    base = strip_digits(x["zh"])
    if base in old_monsters:
        m = old_monsters[base]
        if "source" in m and "MonItems" not in m.get("drops", ""):
            m["drops"] = (m["drops"] + " · " if m["drops"] else "") + "来源: MUD3 服务端刷怪记录"
    else:
        old_monsters[base] = {
            "id": new_mid(), "name": base, "zh": base, "legacy": True,
            "source": "MUD3 服务端刷怪记录",
            "tag": "unverified", "tag_note": "仅刷怪记录; monster.dat 无此名（变体 %s）" % "、".join(x["variants"][:4]),
            "level": None,
            "attrs": ["老版 monster.dat 无对应条目"],
            "traits": "仅在 MUD3 服务端刷怪记录中出现, 无 DAT 属性数据",
            "spawns": spawn_str(SPAWN_AGG.get(base, {})),
            "drops": "", "boss": False, "undead": False, "tame": False,
            "ver": ["mud3"], "img": None, "dat": {},
        }


# ============================================================ 装备
def item_img(looks):
    if looks is None:
        return None
    if not (wil_probe.frame_ok(EI_DATA, "StoreItem.wil", looks)
            or wil_probe.frame_ok(wil_probe.MEI_DATA, "StoreItem.wil", looks)):
        return None
    return {"lib": "StoreItem.wil", "frame": looks, "src": "ei"}


def item_attrs(r):
    out = []
    out.append(f"价格 {r['Price']}")
    out.append(f"重量 {r['Weight']}")
    if r["DuraMax"]:
        out.append(f"持久 {r['DuraMax']}")
    if r["ACMin"] or r["ACMax"]:
        out.append(f"防御 {r['ACMin']}-{r['ACMax']}")
    if r["MACMin"] or r["MACMax"]:
        out.append(f"魔御 {r['MACMin']}-{r['MACMax']}")
    if r["DCMin"] or r["DCMax"]:
        out.append(f"攻击 {r['DCMin']}-{r['DCMax']}")
    if r["NeedLevel"]:
        out.append(f"需求等级 {r['NeedLevel']}")
    out.append(f"类型 {STDMODE_ZH.get(r['StdMode'], r['StdMode'])}")
    if r["Shape"]:
        out.append(f"形状 {r['Shape']}")
    return out


def item_fields(r):
    return {
        "价格": r["Price"], "重量": r["Weight"], "持久 DuraMax": r["DuraMax"],
        "防御 AC": f"{r['ACMin']}-{r['ACMax']}" if (r["ACMin"] or r["ACMax"]) else 0,
        "魔御 MAC": f"{r['MACMin']}-{r['MACMax']}" if (r["MACMin"] or r["MACMax"]) else 0,
        "攻击 DC": f"{r['DCMin']}-{r['DCMax']}" if (r["DCMin"] or r["DCMax"]) else 0,
        "需求等级": r["NeedLevel"], "类型 StdMode": STDMODE_ZH.get(r["StdMode"], r["StdMode"]),
        "外观 Looks": r["Looks"],
    }


ITEM_MAP_BY_NAME = {name: (zid, note) for name, zid, note in bc.ITEM_MAP}
old_items = {}
ref_items = {}
_ni = [0]


def new_iid():
    _ni[0] -= 1
    return _ni[0]


for r in stditem["records"]:
    if not r.get("Name"):
        continue
    name = r["Name"]
    mapped = ITEM_MAP_BY_NAME.get(name)
    if mapped and r.get("tag") in ("both", "changed") and mapped[0] is not None:
        ref_items[str(mapped[0])] = {"source": "stditem.dat", "tag": r["tag"],
                                     "tag_note": mapped[1] or r.get("tag_note", ""),
                                     "fields": item_fields(r)}
        continue
    if name in old_items:
        continue
    old_items[name] = {
        "id": new_iid(), "name": name, "zh": name, "legacy": True,
        "source": "stditem.dat", "tag": r.get("tag", "unverified"),
        "tag_note": r.get("tag_note", ""),
        "category": "mud3", "class": "全职业",
        "type_zh": "MUD3 独有（老版 DAT）",
        "attrs": item_attrs(r),
        "meta": "", "drops": "", "set": "", "desc": "",
        "ver": ["mud3"], "img": item_img(r["Looks"]),
        "dat": {"Index": r["Index"], "StdMode": r["StdMode"], "Shape": r["Shape"],
                "Looks": r["Looks"], "Price": r["Price"], "Weight": r["Weight"],
                "DuraMax": r["DuraMax"], "ACMin": r["ACMin"], "ACMax": r["ACMax"],
                "MACMin": r["MACMin"], "MACMax": r["MACMax"],
                "DCMin": r["DCMin"], "DCMax": r["DCMax"], "NeedLevel": r["NeedLevel"],
                "StackSize": r["StackSize"], "Attr1": r["Attr1"], "Attr2": r["Attr2"],
                "Attr3": r["Attr3"], "Attr4": r["Attr4"], "Luck": r["Luck"]},
    }

# three_versions items mud3_only（MonItems 掉落驱动）并入
for name in tv["items"]["mud3_only"]:
    base = strip_digits(name)
    if base in old_items:
        old_items[base]["drops"] = "MonItems 掉落表" if not old_items[base]["drops"] else old_items[base]["drops"] + " · MonItems 掉落表"
    else:
        old_items[base] = {
            "id": new_iid(), "name": base, "zh": base, "legacy": True,
            "source": "MUD3 服务端 MonItems 掉落表",
            "tag": "unverified", "tag_note": "MonItems 掉落表条目, stditem.dat 无此名",
            "category": "mud3", "class": "全职业",
            "type_zh": "MUD3 独有（MonItems 掉落）",
            "attrs": ["老版 stditem.dat 无对应条目"], "meta": "",
            "drops": "MonItems 掉落表", "set": "", "desc": "",
            "ver": ["mud3"], "img": None, "dat": {},
        }


# ============================================================ 技能
def skill_fields(r):
    return {
        "系别 MagicSchool": SCHOOL_ZH.get(r["MagicSchool"], r["MagicSchool"]),
        "类型 Kind": KIND_ZH.get(r["Kind"], r["Kind"]),
        "威力 TrioA": f"{r['TrioA1']}/{r['TrioA2']}/{r['TrioA3']}",
        "耗蓝 TrioB": f"{r['TrioB1']}/{r['TrioB2']}/{r['TrioB3']}",
        "等级门槛": f"{r['NeedLevel1']}/{r['NeedLevel2']}/{r['NeedLevel3']}",
        "修炼经验": f"{r['TrainExp1']}/{r['TrainExp2']}/{r['TrainExp3']}",
        "技能点": r["SkillPoints"],
    }


SKILL_MAP_BY_NAME = {k: (zid, note) for k, (zid, note) in bc.SKILL_MAP.items()}
old_skills = []
ref_skills = {}
_ns = [0]


def new_sid():
    _ns[0] -= 1
    return _ns[0]


for r in magic["records"]:
    if not r.get("Name"):
        continue
    name = r["Name"]
    mapped = SKILL_MAP_BY_NAME.get(name)
    if mapped and r.get("tag") in ("both", "changed"):
        ref_skills[str(mapped[0])] = {"source": "magic.dat", "tag": r["tag"],
                                      "tag_note": mapped[1] or r.get("tag_note", ""),
                                      "fields": skill_fields(r)}
        continue
    school = SCHOOL_ZH.get(r["MagicSchool"], str(r["MagicSchool"]))
    old_skills.append({
        "id": new_sid(), "name": name, "zh": name, "legacy": True,
        "source": "magic.dat", "tag": r.get("tag", "old-only"),
        "tag_note": r.get("tag_note", ""),
        "klass": f"老版·{school}",
        "type": KIND_ZH.get(r["Kind"], str(r["Kind"])), "school": school,
        "prop": "", "power": f"{r['TrioA1']}/{r['TrioA2']}/{r['TrioA3']}",
        "cost": f"{r['TrioB1']}/{r['TrioB2']}/{r['TrioB3']}",
        "delay": "", "levels": f"{r['NeedLevel1']}/{r['NeedLevel2']}/{r['NeedLevel3']}",
        "exp": f"{r['TrainExp1']}/{r['TrainExp2']}/{r['TrainExp3']}",
        "icon": "", "desc": f"老版魔法（magic.dat）· Zircon 无直接对应",
        "ver": ["mud3"], "img": None, "anim": None,
        "dat": {"Index": r["Index"], "MagicSchool": r["MagicSchool"], "Kind": r["Kind"],
                "TrioA1": r["TrioA1"], "TrioA2": r["TrioA2"], "TrioA3": r["TrioA3"],
                "TrioB1": r["TrioB1"], "TrioB2": r["TrioB2"], "TrioB3": r["TrioB3"],
                "NeedLevel1": r["NeedLevel1"], "NeedLevel2": r["NeedLevel2"],
                "NeedLevel3": r["NeedLevel3"], "SkillPoints": r["SkillPoints"]},
    })


# ============================================================ 输出
data = {
    "_meta": {"generated_at": iso(), "script": "dat_integrate.py",
              "input": ["wiki_data.json", "three_versions.json", "report_full.json",
                        "stditem/magic/monster.dat 解码 JSON"]},
    "monsters": sorted(old_monsters.values(), key=lambda m: m["id"]),
    "items": sorted(old_items.values(), key=lambda i: i["id"]),
    "skills": sorted(old_skills, key=lambda s: s["id"]),
    "ref": {"monsters": ref_monsters, "items": ref_items, "skills": ref_skills},
}
out = "/tmp/wiki_dat.json"
json.dump(data, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(f"输出 {out}")
print(f"  老版怪物卡 {len(data['monsters'])}（含 tv mud3_only 并入）| 挂靠 {len(ref_monsters)}")
print(f"  老版装备卡 {len(data['items'])}（含 MonItems 并入）| 挂靠 {len(ref_items)}")
print(f"  老版技能卡 {len(data['skills'])} | 挂靠 {len(ref_skills)}")
img_n = sum(1 for m in data["monsters"] if m.get("img"))
print(f"  怪物有图 {img_n}/{len(data['monsters'])} · 装备有图 "
      f"{sum(1 for i in data['items'] if i.get('img'))}/{len(data['items'])}")
