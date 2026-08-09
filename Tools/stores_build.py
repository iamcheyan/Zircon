#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""stores_build.py — 商店板块数据聚合。

输入:
  /tmp/stores.json        SystemDbProbe --stores 导出 (NPC 含地图/位置 + NPCGood 货品)
  /tmp/wiki_data_v2.json  百科主数据 (物品中文名/图/价格)
  docs/terminology/09-NPC.md   NPC 中文名对照
  docs/database/views/npcs.md  NPC 介绍/地图

商店模型:
  Zircon 的 NPCInfo.GoodsIndex 全部为 0 (未按页分区), 故每家店 = 一个 NPC,
  店名取 NPC 所在位置 (MapRegion.ServerDescription 中 ' - ' 后的部分,
  如 "Weapon Store" / "Potion Store" / "Book Store")。
  货品按店类型 -> 物品类别/小类映射分配 (武器店卖 weapons, 书店卖技能书 …)。
  纯服务型 NPC (传送石/公告板/训练师/管家…) 无在售货品, 仅展示位置。

输出:
  /tmp/wiki_stores.json — 商店板块渲染数据:
    stores: [{name, name_zh, map, map_file, kind, kind_zh, npcs:[{id,name,zh,icon,map}],
              goods:[{item_id,name,zh,category,type_zh,price,rate,img}]}]
    stats:  shops / kinds / npcs / goods / items
"""
import json
import os
import re

STORES = "/tmp/stores.json"
WIKI = "/tmp/wiki_data_v2.json"
TERM_NPC = os.path.join(os.path.dirname(__file__), "..", "docs", "terminology", "09-NPC.md")
VIEW_NPC = os.path.join(os.path.dirname(__file__), "..", "docs", "database", "views", "npcs.md")
OUT = "/tmp/wiki_stores.json"

# 店类型 -> (中文名, 货品筛选器)
# 筛选器: (category, type_zh) 匹配; None 表示无货品 (服务型)
KIND_ZH = {
    "Weapon Store": ("武器店", ("weapons", None)),
    "Armour Store": ("防具店", ("armour", None)),
    "Accessory Store": ("首饰店", ("jewellery", None)),
    "Book Store": ("书店", ("consumables", "技能书")),
    "Butcher Store": ("肉店", ("consumables", "肉类")),
    "Potion Store": ("药店", ("consumables", "消耗品")),
    "Essential Store": ("杂货店", ("materials", None)),
    "Collector Store": ("回收店", ("materials", None)),
    "Refine Smith": ("铁匠铺", ("materials", None)),
    "Accessory Refiner": ("首饰加工", ("materials", None)),
    "Rusty Accessory NPC": ("旧首饰商", ("materials", None)),
    "Weapon Craft NPC": ("武器打造", ("materials", None)),
    "Emblem NPC": ("徽章商人", ("materials", None)),
    "Item Fragment": ("碎片商人", ("materials", None)),
    "Stables Store": ("马厩", ("consumables", "宠物食物")),
    "Stables": ("马厩", ("consumables", "宠物食物")),
    "Companion Manager": ("宠物管理员", ("consumables", "宠物食物")),
    "Companoin Manager": ("宠物管理员", ("consumables", "宠物食物")),
    "Well": ("水井", ("consumables", "消耗品")),
    "Notice Board": ("公告板", None),
    "Teleport Stone": ("传送石", None),
    "Teleport Stone Castle": ("传送石(城堡)", None),
    "Teleport Stone Left": ("传送石(左)", None),
    "Warrior Trainer": ("战士训练师", None),
    "Taoist Mentor": ("道士导师", None),
    "Wizard Teacher": ("法师导师", None),
    "Village Elder": ("村长", None),
    "Dock Manager": ("码头管理员", None),
    "Administrator": ("管理员", None),
    "Sailor NPC": ("水手", None),
    "Chief Yonghyeon": ("村长", None),
    "Warrior Trainer": ("战士训练师", None),
}


def read_utf8(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def load_term_npc(path):
    """docs/terminology/09-NPC.md: | id | 英文 | 中文 | 备注 |"""
    zh = {}
    for line in read_utf8(path).splitlines():
        m = re.match(r"\|\s*(\d+)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|", line)
        if m:
            zh[int(m.group(1))] = m.group(3).strip()
    return zh


def load_view_npc(path):
    """docs/database/views/npcs.md: ### id · 英文名  / - 介绍：xxx  / - 地图 N · ..."""
    info = {}
    cur = None
    for line in read_utf8(path).splitlines():
        m = re.match(r"###\s*(\d+)\s*·\s*(.+)", line)
        if m:
            cur = int(m.group(1))
            info[cur] = {"desc": "", "map": ""}
            continue
        if cur is None:
            continue
        m = re.match(r"- 介绍：\s*(.+)", line)
        if m:
            info[cur]["desc"] = m.group(1).strip()
            continue
        m = re.match(r"- 地图\s*([^·]*?)\s*·", line)
        if m:
            info[cur]["map"] = m.group(1).strip()
    return info


def main():
    stores = json.load(open(STORES))
    wiki = json.load(open(WIKI))
    term_zh = load_term_npc(TERM_NPC)
    view_info = load_view_npc(VIEW_NPC)

    # 物品索引: name -> item; 价格从 meta 提取
    items_by_name = {}
    price_map = {}
    for it in wiki["items"]:
        items_by_name.setdefault(it["name"], it)
        m = re.search(r"价格\s*(\d+)", it.get("meta", ""))
        price_map[it["name"]] = int(m.group(1)) if m else 0

    # 全部货品 (去重)
    all_goods = []
    seen = set()
    for g in stores["goods"]:
        it = items_by_name.get(g["item"])
        if it is None or g["item"] in seen:
            continue
        seen.add(g["item"])
        all_goods.append({
            "item_id": it["id"],
            "name": it["name"],
            "zh": it.get("zh") or it["name"],
            "category": it.get("category", ""),
            "type_zh": it.get("type_zh", ""),
            "price": price_map.get(it["name"], 0),
            "rate": float(g.get("rate", 1)),
            "img": it.get("img"),
        })

    # 每家店 = 一个 NPC; 位置 ' - ' 后段为店类型
    shops = []
    for n in stores["npcs"]:
        m = n["map"]
        mapn = m.split(" (")[0]
        loc = m.split(" - ")[-1] if " - " in m else ""
        kind = loc or "Unknown"
        kind_zh, filt = KIND_ZH.get(kind, (loc, None))
        # 该店货品: 按筛选器过滤
        goods = []
        if filt:
            cat, tzh = filt
            for gd in all_goods:
                if cat and gd["category"] != cat:
                    continue
                if tzh and gd["type_zh"] != tzh:
                    continue
                goods.append(gd)
        shops.append({
            "name": f"{mapn} · {kind}",
            "name_zh": f"{mapn} · {kind_zh}",
            "map": mapn,
            "map_file": n.get("mapFile", ""),
            "kind": kind,
            "kind_zh": kind_zh,
            "npcs": [{
                "id": n["index"],
                "name": n["name"],
                "zh": term_zh.get(n["index"]) or n["name"],
                "icon": n["image"],
                "map": m,
            }],
            "goods": goods,
        })

    shops.sort(key=lambda s: (s["kind"], s["map"]))
    kinds = []
    seen_k = set()
    for s in shops:
        if s["kind"] not in seen_k:
            seen_k.add(s["kind"])
            kinds.append({"name": s["kind"], "zh": s["kind_zh"]})

    result = {
        "stores": shops,
        "stats": {
            "shops": len(shops),
            "kinds": len(kinds),
            "npcs": len(shops),
            "goods": len(all_goods),
            "items": len(all_goods),
        },
    }
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)
    print(f"商店 {result['stats']['shops']} 家 / 类型 {result['stats']['kinds']} / NPC {result['stats']['npcs']} / 货品 {result['stats']['goods']} / 去重物品 {result['stats']['items']} -> {OUT}")


if __name__ == "__main__":
    main()
