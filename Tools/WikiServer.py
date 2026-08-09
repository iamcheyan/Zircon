#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""WikiServer.py — EI 传奇3.0 客户端游戏百科 本地服务。

路由:
  /                        首页
  /maps                    地图列表（544 图网格, 搜索/过滤）
  /map/<file>              地图详情（缩略图 + 怪物/NPC/商人）
  /monsters                怪物图鉴（309 种, 分类/等级/版本筛选）
  /monster/<name>          怪物详情
  /items                   装备（类型分组 + 职业过滤）
  /item/<id>               装备详情
  /skills                  技能
  /npcs                    NPC
  /quests                  任务
  /companions              宠物与坐骑
  /library                 资源库（WIL 图库浏览）
  /diff                    差异裁剪（EI vs mir3ei vs Zircon）
  /thumb/<file>            地图缩略图（磁盘缓存, 缺失则实时渲染）
  /data/wiki.json          数据 JSON
数据来源: /tmp/wiki_data.json + /tmp/report_full.json + /tmp/wiki_thumbs/
"""
import html, json, os, re, sys, threading, time, urllib.parse
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

ROOT = os.path.dirname(os.path.abspath(__file__))
DATA_JSON = "/tmp/wiki_data_v2.json"
REPORT_JSON = "/tmp/report_full.json"
STORES_JSON = "/tmp/wiki_stores.json"
MAP_LINKS_JSON = "/tmp/map_links.json"
THUMBS_DIR = "/tmp/wiki_thumbs"
IMGS_DIR = "/tmp/wiki_imgs"
IMG_BOARDS = ("monsters", "items", "skills", "npcs", "companion", "skills_anim")
EI_CLIENT = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端"
EI_MAPS = os.path.join(EI_CLIENT, "Map")
EI_DATA = os.path.join(EI_CLIENT, "Data")
PORT = 8777

# 商店类型英文 -> 中文 (与 stores_build.py 一致)
KIND_ZH = {
    "Weapon Store": "武器店", "Armour Store": "防具店", "Accessory Store": "首饰店",
    "Book Store": "书店", "Butcher Store": "肉店", "Potion Store": "药店",
    "Essential Store": "杂货店", "Collector Store": "回收店", "Refine Smith": "铁匠铺",
    "Accessory Refiner": "首饰加工", "Rusty Accessory NPC": "旧首饰商",
    "Weapon Craft NPC": "武器打造", "Emblem NPC": "徽章商人", "Item Fragment": "碎片商人",
    "Stables Store": "马厩", "Stables": "马厩", "Companion Manager": "宠物管理员",
    "Companoin Manager": "宠物管理员", "Well": "水井", "Notice Board": "公告板",
    "Teleport Stone": "传送石", "Teleport Stone Castle": "传送石(城堡)",
    "Teleport Stone Left": "传送石(左)", "Warrior Trainer": "战士训练师",
    "Taoist Mentor": "道士导师", "Wizard Teacher": "法师导师", "Village Elder": "村长",
    "Dock Manager": "码头管理员", "Administrator": "管理员", "Sailor NPC": "水手",
    "Chief Yonghyeon": "村长",
}

# ---------------------------------------------------------------- ver 筛选
# ei/mei = 客户端 WIL 素材存在性（wil_probe 真打）; mud3/zircon = 服务端数据存在性
VER_OPTS = [("", "全部版本"), ("ei", "含 EI 素材"), ("mei", "含 mir3ei 素材"),
            ("mud3", "仅 MUD3 独有"), ("zir", "仅 Zircon 独有")]

def ver_matches(ver, sel):
    """条目 ver 集合是否命中筛选。
    含X = X in ver（ei/mei 为素材存在性, 老版条目普遍同时含 zircon/mud3）
    仅X = ver 恰为 {X}（MUD3 独有 = 老版 DAT 条目; Zircon 独有 = 仅 Zircon 服务端+素材）"""
    if not sel:
        return True
    if sel == "ei":
        return "ei" in (ver or [])
    if sel == "mei":
        return "mei" in (ver or [])
    if sel == "mud3":
        return sorted(set(ver or [])) == ["mud3"]
    if sel == "zir":
        return sorted(set(ver or [])) == ["zircon"]
    return True

def ver_select(sel):
    opts = "".join(f'<option value="{v}" {"selected" if v == sel else ""}>{l}</option>'
                   for v, l in VER_OPTS)
    return f'<select name="ver">{opts}</select>'

# ---------------------------------------------------------------- 怪物分类/等级
# 分类三标签, 无"全部"(全不选 = 全部); 多选任一命中
MON_CATS = [("boss", "Boss"), ("undead", "不死系"), ("normal", "普通")]
# 未知等级 (level=0, 显示"Lv ?") 单独一类, 不进 0-9 级
MON_LVS = [("unknown", "未知等级"), ("1-9", "1-9 级"), ("10-29", "10-29 级"),
           ("30-59", "30-59 级"), ("60-89", "60-89 级"), ("90", "90 级以上")]
MON_SORTS = [("", "默认排序"), ("lv_desc", "等级 ↓"), ("lv_asc", "等级 ↑"),
             ("name", "名称 A-Z"), ("spawn", "刷新量 ↓")]
ITEM_SORTS = [("", "默认排序"), ("price_desc", "价格 ↓"), ("price_asc", "价格 ↑"), ("name", "名称 A-Z")]
PAGE = 60

def mon_has_lv(m):
    """有真实等级 (level 非 None/0)。level=0 是展示/吉祥物怪, 归"未知等级"。"""
    return m.get("level") not in (None, 0)

def mon_cat_ok(m, cats):
    """多选分类: 任一命中即过; 空列表 = 全部。"""
    if not cats:
        return True
    for c in cats:
        if c == "boss": 
            if m.get("boss"): return True
        elif c == "undead":
            if m.get("undead"): return True
        elif c == "normal":
            if not m.get("boss") and not m.get("undead"): return True
    return False

def mon_lv_ok(m, lv):
    if not lv:
        return True
    if lv == "unknown":
        return not mon_has_lv(m)
    l = m.get("level") or 0
    if lv == "1-9": return 1 <= l <= 9
    if lv == "10-29": return 10 <= l <= 29
    if lv == "30-59": return 30 <= l <= 59
    if lv == "60-89": return 60 <= l <= 89
    if lv == "90": return l >= 90
    return True

def mon_cat_chips(sel):
    """分类 checkbox 标签组 (多选, 同名 cat)。"""
    sel = set(sel or [])
    chips = ""
    for v, l in MON_CATS:
        chk = ' checked' if v in sel else ''
        chips += (f'<label class="chip"><input type="checkbox" name="cat" value="{v}"{chk}>'
                  f'<span>{l}</span></label>')
    return chips

def mon_lv_select(sel):
    opts = "".join(f'<option value="{v}" {"selected" if v == sel else ""}>{l}</option>'
                   for v, l in MON_LVS)
    return f'<select name="lv"><option value="">全部等级</option>{opts}</select>'

def sort_select(sel, page_kind):
    opts_ = MON_SORTS if page_kind == "monsters" else ITEM_SORTS
    opts = "".join(f'<option value="{v}" {"selected" if v == sel else ""}>{l}</option>'
                   for v, l in opts_)
    return f'<select name="sort">{opts}</select>'

def pager(params, page, pages, base="/monsters"):
    """分页导航; 保留全部筛选参数, 仅换 p。居中, 上下留白, 含首/末页与省略号, 附下拉跳页。"""
    if pages <= 1:
        return ""
    def href(p):
        ps = dict(params)
        ps["p"] = p
        return base + "?" + urllib.parse.urlencode(ps, doseq=True)
    # 可见页码: 首 1, 末 pages, 当前页 ±2, 缺口用省略号
    shown = sorted({1, pages, page - 2, page - 1, page, page + 1, page + 2})
    shown = [p for p in shown if 1 <= p <= pages]
    parts = []
    if page > 1:
        parts.append(f'<a class="pg" href="{href(1)}" title="第一页">«</a>')
        parts.append(f'<a class="pg" href="{href(page - 1)}" title="上一页">‹</a>')
    prev = None
    for p in shown:
        if prev is not None and p - prev > 1:
            parts.append('<span class="pg dots">…</span>')
        cls = ' class="pg on"' if p == page else ' class="pg"'
        parts.append(f'<a{cls} href="{href(p)}">{p}</a>')
        prev = p
    if page < pages:
        parts.append(f'<a class="pg" href="{href(page + 1)}" title="下一页">›</a>')
        parts.append(f'<a class="pg" href="{href(pages)}" title="最后一页">»</a>')
    # 下拉跳页: 列出全部页码, 选即跳转 (保留筛选参数)
    opts = ['<option value="" disabled selected>跳转到…</option>']
    opts += [f'<option value="{href(p)}">{p}</option>' for p in range(1, pages + 1)]
    jump = (f'<select class="pg-go" onchange="if(this.value) location.href=this.value" '
            f'aria-label="跳转到指定页">{"".join(opts)}</select>')
    return (f'<div class="pager">{"".join(parts)} '
            f'<span class="pager-info">第 <b>{page}</b> / {pages} 页</span>{jump}</div>')

def item_price(i):
    """从 meta '价格 80000' 提取整数价格; 无则 None。"""
    m = re.search(r"价格\s*([\d,]+)", i.get("meta", ""))
    return int(m.group(1).replace(",", "")) if m else None

def item_sort_key(i, sort):
    leg = 1 if i.get("legacy") else 0
    if sort == "price_desc":
        return (leg, -(item_price(i) if item_price(i) is not None else -1), i["name"].lower())
    if sort == "price_asc":
        return (leg, item_price(i) if item_price(i) is not None else 1 << 62, i["name"].lower())
    if sort == "name":
        return (leg, i["name"].lower())
    return (leg, (i.get("type_zh") or ""), i["name"])

def mon_sort_key(m, sort):
    leg = 1 if m.get("legacy") else 0
    if sort == "lv_desc":
        return (leg, -(m.get("level") or 0), m["name"].lower())
    if sort == "lv_asc":
        return (leg, (m.get("level") or 0), m["name"].lower())
    if sort == "name":
        return (leg, m["name"].lower())
    if sort == "spawn":
        return (leg, -sum(c for _, c in parse_spawns(m.get("spawns"))), m["name"].lower())
    return (leg, (m.get("level") or 0), m["name"].lower())

def mon_map_zh(code):
    """怪物出现地图码 → 中文名; 无则 None。优先 report srv_name, 回退 Mapinfo 定义名。"""
    c = str(code).lower()
    m = Data.map_code.get(c)
    if m and m.get("srv_name"):
        return m["srv_name"]
    return Data.mapinfo_names.get(c) or None

# 怪物 → 出现地图列表 (去重, 保序)
def mon_maps(m):
    return sorted({c for c, _ in parse_spawns(m.get("spawns"))}, key=lambda x: x.lower())

# ---------------------------------------------------------------- 装备细分分组
ITEM_GROUPS = [
    ("", "全部分类"),
    ("weapon", "武器"),
    ("armour", "护甲 / 时装 / 马甲"),
    ("helmet", "头盔"),
    ("shoes", "鞋子"),
    ("shield", "盾牌"),
    ("jewellery", "首饰（项链 / 手镯 / 戒指 / 护身符）"),
    ("potion", "药水 / 消耗品"),
    ("book", "技能书 / 卷轴"),
    ("material", "材料（矿石 / 暗石 / 精炼 / 部件）"),
    ("gem", "宝石 / 徽章"),
    ("pet", "宠物用品"),
    ("fish", "钓鱼用品"),
    ("money", "货币 / 礼包 / 宝箱"),
    ("other", "其他"),
]
_ITEM_GROUP_TYPES = {
    "weapon": {"武器"}, "armour": {"护甲", "时装", "马甲"}, "helmet": {"头盔"},
    "shoes": {"鞋子"}, "shield": {"盾牌"},
    "jewellery": {"项链", "手镯", "戒指", "护身符"},
    "potion": {"消耗品", "毒药", "肉类", "花", "火把"},
    "book": {"技能书", "卷轴"},
    "material": {"矿石", "暗石", "精炼材料", "部件"},
    "gem": {"宝石", "徽章"},
    "pet": {"宠物食物", "宠物背包", "宠物头饰", "宠物背饰"},
    "fish": {"钓钩", "浮标", "鱼饵", "探测器", "卷线器"},
    "money": {"货币", "礼包", "宝箱", "系统物品"},
}

def item_group(i):
    t = (i.get("type_zh") or "").strip()
    for gid, types in _ITEM_GROUP_TYPES.items():
        if t in types:
            return gid
    return "other"

def item_group_chips(sel):
    """装备分类标签组 (多选, 同名 group); 跳过"全部"占位项。"""
    sel = set(sel or [])
    chips = ""
    for v, l in ITEM_GROUPS:
        if not v:
            continue
        chk = ' checked' if v in sel else ''
        short = l.split("（")[0]
        chips += (f'<label class="chip"><input type="checkbox" name="group" value="{v}"{chk}>'
                  f'<span>{esc(short)}</span></label>')
    return chips

def ver_badges(ver, legacy=False):
    s = set(ver or [])
    out = ""
    if legacy:
        out += '<span class="tag tag-legacy">老版 DAT</span>'
    else:
        if "mud3" in s: out += '<span class="tag tag-mud3">MUD3</span>'
    if "ei" in s: out += '<span class="tag tag-ei">EI</span>'
    if "mei" in s: out += '<span class="tag tag-mei">mir3ei</span>'
    if "zircon" in s: out += '<span class="tag tag-zir">Zircon</span>'
    return out

def img_url(board, iid):
    return f'/img/{board}/{iid}.png'

def icon_img(board, iid, cls="icon", alt=""):
    """列表卡片/表格小图; 文件缺失时返回占位。"""
    p = os.path.join(IMGS_DIR, board, f"{iid}.png")
    if os.path.exists(p):
        return f'<img class="{cls}" src="{img_url(board, iid)}" alt="{esc(alt)}">'
    return f'<div class="noimg {cls}"></div>'

def npc_img(n, cls="icon"):
    """NPC 图: NPCface.Zl 头像优先, 回退 NPC.Zl 全身像, 再回退占位。"""
    p_face = os.path.join(IMGS_DIR, "npcs_face", f"{n['id']}.png")
    if os.path.exists(p_face):
        return f'<img class="{cls}" src="{img_url("npcs_face", n["id"])}" alt="{esc(n.get("zh") or n.get("name") or "")}">'
    return icon_img("npcs", n["id"], cls, n.get("zh") or n.get("name") or "")

def dash(v):
    """空值统一 '—'。"""
    if v is None:
        return "—"
    s = str(v).strip()
    return s if s else "—"

def old_block(rec):
    """both/changed 挂靠: 老版 DAT 属性对照区块。"""
    f = rec.get("fields") or {}
    kv = "".join(f"<dt>{esc(k)}</dt><dd>{esc(v)}</dd>" for k, v in f.items())
    return f"""<h2>老版 DAT 对照（{esc(rec.get('source',''))}）</h2>
<p class="lead">判定: <span class="badge">{esc(rec.get('tag',''))}</span> {esc(rec.get('tag_note',''))}</p>
<dl class="kv">{kv}</dl>"""

def parse_spawns(s):
    """spawns 字符串 'D1505 ×2、D1501 ×1' → [(map, count), …]"""
    if not s:
        return []
    out = []
    for seg in re.split(r"[、,，]", str(s)):
        mm = re.match(r"\s*([A-Za-z0-9_]+)\s*[×xX*]\s*(\d+)", seg)
        if mm:
            out.append((mm.group(1), int(mm.group(2))))
    return out

# ---------------------------------------------------------------- data load
class Data:
    _lock = threading.Lock()
    _t = 0.0

    @classmethod
    def get(cls):
        # 30s 缓存 + 文件变更检测
        mtime = max(os.path.getmtime(DATA_JSON), os.path.getmtime(REPORT_JSON),
                    os.path.getmtime(STORES_JSON))
        with cls._lock:
            if mtime != cls._t:
                with open(DATA_JSON, encoding="utf-8") as f:
                    cls.wiki = json.load(f)
                with open(REPORT_JSON, encoding="utf-8") as f:
                    cls.report = json.load(f)
                with open(STORES_JSON, encoding="utf-8") as f:
                    cls.stores = json.load(f)
                cls._t = mtime
                cls._build()
            return cls.wiki, cls.report, cls.stores
    @classmethod
    def _build(cls):
        """预建索引。"""
        w, r = cls.wiki, cls.report
        # 地图: file -> report 条目
        cls.map_by_file = {m["file"]: m for m in r["report"]}
        # 怪物图鉴 (v2 三版合并): 英文名/中文名 -> 条目
        cls.monsters = w["monsters"]
        cls.mon_by_id = {m["id"]: m for m in w["monsters"]}
        cls.mon_by_zh = {}
        for m in w["monsters"]:
            zh = m.get("zh") or m["name"]
            cls.mon_by_zh.setdefault(zh, m)
            cls.mon_by_zh.setdefault(m["name"], m)
        # 怪物动画帧数 (img_pipeline 渲染后回填)
        try:
            with open("/tmp/mon_anim.json", encoding="utf-8") as f:
                cls.mon_anim = json.load(f)
        except (FileNotFoundError, ValueError):
            cls.mon_anim = {}
        # 装备
        cls.items = w["items"]
        cls.item_by_id = {i["id"]: i for i in w["items"]}
        cls.item_cats = []
        seen = set()
        for i in w["items"]:
            if i["category"] not in seen:
                seen.add(i["category"]); cls.item_cats.append(i["category"])
        # 技能: 职业分组
        cls.skills = w["skills"]
        cls.skill_by_id = {s["id"]: s for s in w["skills"]}
        cls.skill_classes = []
        seen = set()
        for s in w["skills"]:
            if s["klass"] and s["klass"] not in seen:
                seen.add(s["klass"]); cls.skill_classes.append(s["klass"])
        # NPC: map 索引
        cls.npcs = w["npcs"]
        cls.npc_by_map = {}
        for n in w["npcs"]:
            cls.npc_by_map.setdefault(n["map"], []).append(n)
        cls.npc_by_id = {n["id"]: n for n in w["npcs"]}
        # 商店: NPC id -> 店 (反查: NPC 详情页显示所属商店)
        cls.store_by_npc = {}
        for si, sh in enumerate(cls.stores["stores"]):
            for nn in sh["npcs"]:
                cls.store_by_npc[nn["id"]] = (si, sh)
        # 任务
        cls.quests = w["quests"]
        # 宠物
        cls.companions = w["companion"]
        # EI 客户端地图 (544, 带 ver)
        cls.ei_maps = w["ei_maps"]
        # 商人: map 索引 (merchants_all: map -> list)
        cls.merch_by_map = {}
        for me in r["merchants_all"]:
            cls.merch_by_map.setdefault(str(me["map"]), []).append(me)
        # EI 图 -> 服务端 map code 映射（文件名去 .map 小写）
        cls.map_code = {}
        for m in r["report"]:
            code = m["file"].lower().removesuffix(".map")
            cls.map_code[code] = m
        # 磁盘缩略图真实文件名（ei_maps 原名, 大小写敏感）
        cls.thumb_name = {}
        for m in w["ei_maps"]:
            cls.thumb_name[m["name"].lower()] = m["name"]
        # 地图路线图 (Mapinfo.txt 连接对): 码 -> 邻接列表 + 中文名
        try:
            with open(MAP_LINKS_JSON, encoding="utf-8") as f:
                links = json.load(f)
        except FileNotFoundError:
            links = {"names": {}, "links": []}
        cls.map_adj = {}
        for a, b in links["links"]:
            cls.map_adj.setdefault(a, []).append(b)
            cls.map_adj.setdefault(b, []).append(a)
        cls.mapinfo_names = links.get("names", {})

# ---------------------------------------------------------------- templates
BASE = """<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{title} · EI 传奇3.0 百科</title>
<style>
:root {{
  --bg:#0d1117; --panel:#151e28; --line:#2a3a49; --fg:#e7eef5;
  --dim:#9eafbf; --acc:#e7b76c; --ac2:#78d5cc; --good:#8bd3a6; --bad:#f18b82;
}}
* {{ box-sizing:border-box; margin:0; padding:0; }}
body {{ background:var(--bg); color:var(--fg); font:15px/1.6 "PingFang SC","Microsoft YaHei",system-ui,sans-serif; }}
a {{ color:var(--ac2); text-decoration:none; }} a:hover {{ text-decoration:underline; }}
header {{ position:sticky; top:0; z-index:50; background:rgba(18,20,23,.97); border-bottom:1px solid var(--line); }}
nav {{ display:flex; flex-wrap:wrap; gap:2px 14px; padding:10px 18px; max-width:1280px; margin:0 auto; }}
nav a {{ padding:4px 8px; border-radius:6px; color:var(--fg); font-size:14px; }}
nav a:hover {{ background:var(--panel); text-decoration:none; }}
nav a.active {{ color:var(--acc); font-weight:700; }}
main {{ max-width:1280px; margin:0 auto; padding:20px 18px 80px; }}
h1 {{ font-size:24px; margin:6px 0 16px; }}
h2 {{ font-size:19px; margin:24px 0 10px; border-bottom:1px solid var(--line); padding-bottom:6px; }}
.lead {{ color:var(--dim); margin-bottom:18px; }}
.cards {{ display:grid; grid-template-columns:repeat(auto-fill,minmax(210px,1fr)); gap:14px; }}
.card {{ background:var(--panel); border:1px solid var(--line); border-radius:10px; overflow:hidden; }}
.card a {{ color:var(--fg); display:block; padding:10px 12px; }}
.card a:hover {{ background:rgba(255,255,255,.03); text-decoration:none; }}
.card .thumb {{ width:100%; aspect-ratio:16/10; object-fit:cover; background:#0d0f12; display:block; }}
.card .name {{ font-weight:600; }}
.card .sub {{ color:var(--dim); font-size:12.5px; margin-top:2px; }}
.card .sub .avatar {{ width:34px; height:34px; object-fit:contain; vertical-align:-10px; margin-right:8px; background:#0d0f12; border-radius:6px; }}
.card .sub .avatar.noimg {{ width:34px; height:34px; display:inline-block; vertical-align:-10px; margin-right:8px; }}
.tag {{ display:inline-block; font-size:11px; padding:1px 7px; border-radius:9px; margin-left:6px; vertical-align:1px; }}
.tag-ei {{ background:#5a2d2d; color:#ff9d9d; }}
.tag-mei {{ background:#2d3a5a; color:#9db8ff; }}
.tag-zir {{ background:#3a2d5a; color:#d0b3ff; }}
.tag-mud3 {{ background:#2d5a3a; color:#a8f0c0; }}
.tag-legacy {{ background:#5a4a2d; color:#ffd98d; }}
.icon {{ width:64px; height:64px; object-fit:contain; background:#0d0f12; border-radius:8px; }}
.noimg {{ background:#0d0f12; border-radius:8px; }}
.card .pic {{ width:100%; height:118px; object-fit:contain; background:#0d0f12; padding:8px; display:block; }}
.card .noimg.pic {{ height:118px; }}
.detail .bigpic {{ width:150px; height:150px; object-fit:contain; background:#0d0f12; border-radius:10px; }}
.detail .noimg.bigpic {{ width:150px; height:150px; }}
table img.icon {{ width:44px; height:44px; }}
.none {{ color:var(--dim); font-style:italic; }}
.badge {{ display:inline-block; font-size:11px; padding:1px 8px; border-radius:9px; margin:2px 4px 2px 0; background:#263040; color:#c9d6ea; }}
table {{ border-collapse:collapse; width:100%; margin:8px 0; }}
th,td {{ border:1px solid var(--line); padding:6px 10px; text-align:left; font-size:14px; }}
th {{ background:#20242c; color:var(--dim); font-weight:600; }}
tr:nth-child(even) td {{ background:rgba(255,255,255,.015); }}
.filters {{ display:flex; flex-wrap:wrap; gap:8px; margin:12px 0 16px; }}
.filters input,.filters select {{ background:var(--panel); border:1px solid var(--line); color:var(--fg);
  border-radius:8px; padding:6px 10px; font-size:14px; }}
.filters button {{ background:#2b3547; color:var(--fg); border:none; border-radius:8px; padding:6px 14px;
  cursor:pointer; font-size:14px; }} .filters button:hover {{ background:#35435c; }}
.chip {{ display:inline-flex; align-items:center; gap:5px; background:var(--panel); border:1px solid var(--line);
  border-radius:16px; padding:3px 11px 3px 8px; cursor:pointer; font-size:13.5px; user-select:none; }}
.chip input {{ accent-color:var(--acc); cursor:pointer; }}
.chip:has(input:checked) {{ border-color:var(--acc); background:#2b2a1f; color:var(--acc); }}
.chip a {{ color:var(--fg); text-decoration:none; }}
.chip a:hover {{ color:var(--acc); }}
.chip.on {{ border-color:var(--acc); background:#2b2a1f; color:var(--acc); }}
.chips {{ margin:10px 0 18px; display:flex; flex-wrap:wrap; gap:7px; }}
.store-sec {{ margin:22px 0; padding:14px 16px; background:var(--panel); border:1px solid var(--line); border-radius:12px; }}
.store-sec h2 {{ margin:0 0 6px; font-size:17px; }}
.store-sec .sub {{ color:var(--dim); font-size:13px; margin-bottom:10px; }}
.store-goods {{ display:flex; flex-wrap:wrap; gap:8px; margin-top:8px; align-items:center; }}
.sgood {{ display:flex; flex-direction:column; align-items:center; gap:2px; text-decoration:none; }}
.sgood .thumb {{ width:52px; height:52px; object-fit:contain; background:#0d0f12; border:1px solid var(--line); border-radius:6px; padding:2px; }}
.sgood:hover .thumb {{ border-color:var(--acc); }}
.sgood .sprice {{ color:var(--dim); font-size:11px; }}
.more {{ color:var(--dim); font-size:12.5px; padding-left:4px; }}
.store-head {{ display:flex; align-items:baseline; gap:8px; }}
.crumbs {{ color:var(--dim); font-size:13px; margin-bottom:6px; }}
.crumbs a {{ color:var(--ac2); }}
.panel-npc {{ display:flex; gap:14px; align-items:center; background:var(--panel); border:1px solid var(--line); border-radius:10px; padding:12px 14px; margin:12px 0 20px; }}
.panel-npc .pic {{ width:64px; height:64px; object-fit:contain; background:#0d0f12; border-radius:8px; }}
.panel-npc .noimg.pic {{ width:64px; height:64px; }}
.pager {{ display:flex; align-items:center; justify-content:center; gap:8px; margin:34px 0; flex-wrap:wrap; }}
.pager .pg {{ display:inline-block; min-width:32px; text-align:center; padding:5px 9px; border:1px solid var(--line);
  border-radius:7px; background:var(--panel); color:var(--fg); font-size:13.5px; transition:border-color .15s, background .15s; }}
.pager .pg:hover {{ border-color:var(--acc); background:#1b2631; text-decoration:none; }}
.pager .pg.on {{ background:var(--acc); color:#1a1408; border-color:var(--acc); font-weight:700; }}
.pager .pg.dots {{ border:none; background:none; min-width:auto; color:var(--dim); }}
.pager-info {{ font-size:13.5px; color:var(--dim); margin:0 4px; }}
.pager-info b {{ color:var(--acc); font-size:14.5px; }}
.pager .pg-go {{ padding:5px 10px; border:1px solid var(--line); border-radius:7px; background:var(--panel);
  color:var(--fg); font-size:13.5px; cursor:pointer; max-width:120px; }}
.pager .pg-go:hover {{ border-color:var(--acc); }}
.anim {{ background:var(--panel); border:1px solid var(--line); border-radius:10px; padding:10px; }}
.anim-stage {{ display:flex; align-items:center; justify-content:center; min-height:150px; background:#0d0f12;
  border:1px solid var(--line); border-radius:8px; margin-bottom:8px; overflow:hidden; }}
.anim-stage img {{ display:block; max-width:100%; height:auto; image-rendering:pixelated; }}
.anim-ctrl {{ display:flex; align-items:center; gap:10px; flex-wrap:wrap; }}
.anim-ctrl button {{ padding:5px 16px; border:1px solid var(--acc); background:var(--acc); color:#1a1408;
  border-radius:7px; font-size:14px; font-weight:700; cursor:pointer; }}
.anim-ctrl button:hover {{ filter:brightness(1.1); }}
.anim-ctrl button:disabled {{ opacity:.5; cursor:default; }}
.mon-anim {{ background:var(--panel); border:1px solid var(--line); border-radius:10px; padding:10px; margin:14px 0; }}
.mon-anim .anim-stage {{ min-height:180px; }}
.mon-btns {{ display:flex; gap:6px; flex-wrap:wrap; }}
.mon-btns button {{ padding:5px 14px; border:1px solid var(--line); background:transparent; color:var(--fg);
  border-radius:7px; cursor:pointer; font-size:13.5px; }}
.mon-btns button:hover {{ border-color:var(--acc); }}
.mon-btns button.on {{ background:var(--acc); color:#1a1408; border-color:var(--acc); font-weight:700; }}
#monPlay:disabled {{ opacity:.45; cursor:not-allowed; }}
.mapnet {{ display:flex; flex-wrap:wrap; gap:8px; margin:8px 0; }}
.mapnet a {{ background:var(--panel); border:1px solid var(--line); border-radius:9px; padding:7px 12px;
  font-size:13.5px; color:var(--fg); }}
.mapnet a:hover {{ border-color:var(--acc); text-decoration:none; }}
.mapnet a .dim {{ display:block; font-size:11.5px; }}
.stat {{ display:inline-block; background:var(--panel); border:1px solid var(--line); border-radius:10px;
  padding:10px 16px; margin:0 10px 10px 0; text-align:center; min-width:110px; }}
.stat b {{ display:block; font-size:22px; color:var(--acc); }}
.stat span {{ font-size:12px; color:var(--dim); }}
.detail {{ display:flex; gap:24px; flex-wrap:wrap; margin:14px 0 26px; }}
.detail img {{ border:1px solid var(--line); border-radius:10px; max-width:480px; background:#0d0f12; }}
.meta {{ flex:1; min-width:280px; }}
.kv {{ display:grid; grid-template-columns:110px 1fr; gap:4px 12px; font-size:14px; }}
.kv dt {{ color:var(--dim); }}
.grid3 {{ display:grid; grid-template-columns:repeat(auto-fill,minmax(300px,1fr)); gap:12px; }}
.grid4 {{ display:grid; grid-template-columns:repeat(auto-fill,minmax(200px,1fr)); gap:12px; }}
.dim {{ color:var(--dim); }}
.panel {{ background:var(--panel); border:1px solid var(--line); border-radius:10px; padding:12px 14px; }}
.panel h3 {{ font-size:15px; margin-bottom:6px; }}
.mono {{ font-family:ui-monospace,Consolas,monospace; font-size:13px; color:var(--dim); }}
.good {{ color:var(--good); }} .bad {{ color:var(--bad); }}
.bad-undead {{ display:inline-block; font-size:11px; padding:1px 7px; border-radius:9px; margin-left:4px; background:#3d2f2f; color:#ffb0b0; }}
.bad-tame {{ display:inline-block; font-size:11px; padding:1px 7px; border-radius:9px; margin-left:4px; background:#2f3d30; color:#b0ffb0; }}
footer {{ max-width:1280px; margin:0 auto; padding:8px 18px 30px; color:var(--dim); font-size:12.5px; }}
</style>
<script>
// 筛选即时生效: select/checkbox/radio 变化即提交; 文本输入 500ms 防抖。保留提交按钮。
document.addEventListener('DOMContentLoaded', function () {{
  var f = document.getElementById('filters');
  if (!f) return;
  var t = null;
  f.addEventListener('change', function (e) {{
    if (e.target.matches('select, input[type=checkbox], input[type=radio]')) f.submit();
  }});
  f.addEventListener('input', function (e) {{
    if (e.target.matches('input.q, input[name=q]')) {{
      clearTimeout(t);
      t = setTimeout(function () {{ f.submit(); }}, 500);
    }}
  }});
}});
// 施法动画播放: 点击 ▶ 后逐帧切换, 循环; 再点暂停。
var _animTimer = null;
function animPlay(btn) {{
  var box = btn.closest('.anim');
  var img = box.querySelector('#animFrame');
  var n = parseInt(box.dataset.frames, 10) || 1;
  var delay = parseInt(box.dataset.delay, 10) || 100;
  var idx = parseInt(box.dataset.i || '0', 10);
  if (btn.dataset.on === '1') {{
    btn.dataset.on = '0'; btn.textContent = '▶ 播放';
    clearInterval(_animTimer); _animTimer = null;
    return;
  }}
  btn.dataset.on = '1'; btn.textContent = '⏸ 暂停';
  var step = function () {{
    img.src = '/img/skills_anim/' + box.dataset.sid + '/' + String(idx).padStart(3, '0') + '.png';
    box.dataset.i = idx;
    var l = box.querySelector('#animIdx');
    if (l) l.textContent = idx + 1;
    idx = (idx + 1) % n;
  }};
  step();
  clearInterval(_animTimer);
  _animTimer = setInterval(step, delay);
}}
// 怪物动画: 动作按钮切换 + 播放/暂停
var _monTimer = null, _monIdx = 0;
var MON_ACT_ZH = {{ standing: '站立', walking: '行走', combat: '攻击', struck: '受击', die: '死亡' }};
function monStop() {{
  if (_monTimer) {{ clearInterval(_monTimer); _monTimer = null; }}
  var b = document.getElementById('monPlay');
  if (b) {{ b.dataset.on = '0'; b.textContent = '▶ 播放'; }}
}}
function monPlay(btn, mid) {{
  monStop();
  var img = document.getElementById('monFrame');
  var act = btn.dataset.act;
  var n = parseInt(btn.dataset.n, 10) || 1;
  img.dataset.act = act; img.dataset.mid = mid;
  img.src = '/img/mon_anim/' + mid + '/' + act + '/000.png';
  _monIdx = 0;
  document.getElementById('monIdx').textContent = '1';
  document.getElementById('monCnt').textContent = n;
  document.getElementById('monActZh').textContent = MON_ACT_ZH[act] || act;
  Array.prototype.forEach.call(document.querySelectorAll('.mon-act'), function (b) {{
    b.classList.remove('on');
  }});
  btn.classList.add('on');
  var pb = document.getElementById('monPlay');
  pb.disabled = false; pb.dataset.on = '0'; pb.textContent = '▶ 播放';
  // 点动作按钮即自动播放该动作
  pb.click();
}}
function monPlayBtn(btn) {{
  var img = document.getElementById('monFrame');
  var mid = img.dataset.mid, act = img.dataset.act;
  var n = parseInt(document.getElementById('monCnt').textContent, 10) || 1;
  if (!mid) return;
  if (btn.dataset.on === '1') {{
    btn.dataset.on = '0'; btn.textContent = '▶ 播放';
    clearInterval(_monTimer); _monTimer = null;
    return;
  }}
  btn.dataset.on = '1'; btn.textContent = '⏸ 暂停';
  var delay = (act === 'standing' || act === 'die') ? 180 : 120;
  var step = function () {{
    img.src = '/img/mon_anim/' + mid + '/' + act + '/' + String(_monIdx).padStart(3, '0') + '.png';
    document.getElementById('monIdx').textContent = _monIdx + 1;
    _monIdx = (_monIdx + 1) % n;
  }};
  step();
  clearInterval(_monTimer);
  _monTimer = setInterval(step, delay);
}}
// 页面加载后自动播放默认动作 (行走)
window.addEventListener('load', function () {{
  var pb = document.getElementById('monPlay');
  if (pb) {{ pb.disabled = false; pb.click(); }}
}});
</script>
</head>
<body>
<header><nav>
<a href="/" {home}>首页</a>
<a href="/maps" {maps}>地图</a>
<a href="/monsters" {monsters}>怪物图鉴</a>
<a href="/items" {items}>装备</a>
<a href="/skills" {skills}>技能</a>
<a href="/npcs" {npcs}>NPC</a>
<a href="/quests" {quests}>任务</a>
<a href="/companions" {companions}>宠物与坐骑</a>
<a href="/stores" {stores}>商店</a>
<a href="/library" {library}>资源库</a>
<a href="/diff" {diff}>差异裁剪</a>
</nav></header>
<main>
{body}
</main>
<footer>EI 传奇3.0 客户端百科 · 数据源: Mud3 服务端 Envir / Zircon System.db / EI 客户端资源 · 本地服务{footer_note}</footer>
</body></html>
"""

def page(title, body, active=""):
    nav = {k: "" for k in ["home","maps","monsters","items","skills","npcs","quests","companions","stores","library","diff"]}
    nav[active] = 'class="active"'
    try:
        meta = Data.get()[0].get("_meta", {})
        fnote = f" · 构建 {esc(meta.get('generated_at',''))}" if meta.get("generated_at") else ""
    except Exception:
        fnote = ""
    return BASE.format(title=html.escape(title), body=body, footer_note=fnote, **nav)

def esc(s):
    return html.escape(str(s), quote=False)

def mon_zh(name):
    """怪物英文/中文名 → 中文显示名。"""
    w, _ = Data.get()
    mon = Data.mon_by_zh.get(name)
    if mon:
        return mon.get("zh") or mon["name"]
    return Data.report["mon_zh"].get(name, name)

def mon_lookup(name):
    """刷怪名 → 条目: 精确匹配; 失败则去尾数字后缀（老版变体 '血金刚0'→'血金刚'）。"""
    m = Data.mon_by_zh.get(name)
    if not m:
        m = Data.mon_by_zh.get(re.sub(r"\s*\d+\s*$", "", name))
    return m

def mon_link(name):
    """刷怪名 → 详情链接（老版卡用负 id 路由, 避开中文名抢占; 未收录 → 纯文本）。"""
    m = mon_lookup(name)
    if not m:
        return esc(name)
    disp = m.get("zh") or m["name"]
    href = f"/monster/{m['id']}" if m.get("legacy") else f"/monster/{urllib.parse.quote(m['name'])}"
    mono = f' <span class="mono">{esc(m["name"])}</span>' if (disp != m["name"] and not m.get("legacy")) else ""
    return f'<a href="{href}">{esc(disp)}{mono}</a>'

def file_link(f):
    return f'<a href="/map/{urllib.parse.quote(f)}">{esc(f)}</a>'

# ---------------------------------------------------------------- handlers
class Handler(BaseHTTPRequestHandler):
    server_version = "EIWiki/1.0"

    def log_message(self, fmt, *args):
        pass

    def _send(self, body, ctype="text/html; charset=utf-8", code=200):
        if isinstance(body, str):
            body = body.encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _send_json(self, obj):
        self._send(json.dumps(obj, ensure_ascii=False), "application/json; charset=utf-8")

    def do_GET(self):
        try:
            self.route(urllib.parse.urlparse(self.path))
        except BrokenPipeError:
            pass
        except Exception as e:
            try:
                self._send(page("错误", f'<h1>服务器错误</h1><p class="lead">{esc(e)}</p>'), code=500)
            except Exception:
                pass

    def route(self, u):
        p = u.path
        if p == "/": return self.home()
        if p == "/maps": return self.maps(u.query)
        if p.startswith("/map/"): return self.map_detail(urllib.parse.unquote(p[5:]))
        if p == "/monsters": return self.monsters(u.query)
        if p.startswith("/monster/"): return self.monster_detail(urllib.parse.unquote(p[9:]))
        if p == "/items": return self.items(u.query)
        if p.startswith("/item/"): return self.item_detail(p[6:])
        if p == "/skills": return self.skills(u.query)
        if p.startswith("/skill/"): return self.skill_detail(p[7:])
        if p == "/npcs": return self.npcs(u.query)
        if p.startswith("/npc/"):
            try: return self.npc_detail(int(p[5:]))
            except ValueError: pass
        if p == "/quests": return self.quests(u.query)
        if p == "/companions": return self.companions(u.query)
        if p == "/stores": return self.stores(u.query)
        if p.startswith("/store/"):
            try: return self.store_detail(int(p[7:]))
            except ValueError: pass
        if p == "/diff": return self.diff()
        if p == "/library": return self.library(u.query)
        if p.startswith("/thumb/"): return self.thumb(urllib.parse.unquote(p[7:]))
        if p.startswith("/img/"): return self.img(urllib.parse.unquote(p[5:]))
        if p == "/data/wiki.json": return self._send_json(Data.get()[0])
        if p == "/data/report.json": return self._send_json(Data.get()[1])
        self._send(page("404", f"<h1>404</h1><p class='lead'>路径不存在: {esc(p)}</p>"), code=404)

    # ---------------- 首页
    def home(self):
        w, r, s = Data.get()
        st = r["stats"]
        c = len(Data.companions)
        nm = len(Data.monsters)
        n_legmon = sum(1 for x in Data.monsters if x.get("legacy"))
        n_legitem = sum(1 for x in Data.items if x.get("legacy"))
        n_legskill = sum(1 for x in Data.skills if x.get("legacy"))
        body = f"""
<h1>EI 传奇3.0 客户端 游戏百科</h1>
<p class="lead">以 EI 传奇3.0 客户端为底板（Mud3 服务端数据为权威来源），对照 Zircon / mir3ei 整理的完整资料库；老版 EI2.0 服务端三 DAT（monster/stditem/magic）解码条目已并入，判定为 both/changed 的挂靠 Zircon 条目显示「老版 DAT 对照」，old-only 独立建卡。</p>
<div>
  <div class="stat"><b>{st['ei_maps']}</b><span>EI 地图</span></div>
  <div class="stat"><b>{nm}</b><span>怪物种类</span></div>
  <div class="stat"><b>{len(w['items'])}</b><span>装备道具</span></div>
  <div class="stat"><b>{len(w['skills'])}</b><span>技能</span></div>
  <div class="stat"><b>{len(w['npcs'])}</b><span>NPC</span></div>
  <div class="stat"><b>{len(w['quests'])}</b><span>任务</span></div>
  <div class="stat"><b>{c}</b><span>宠物坐骑</span></div>
  <div class="stat"><b>{len(s['stores'])}</b><span>商店</span></div>
  <div class="stat"><b>{s['stats']['npcs']}</b><span>在售 NPC</span></div>
  <div class="stat"><b>{s['stats']['goods']}</b><span>在售货品</span></div>
  <div class="stat"><b>{st['spawn_records']}</b><span>刷怪记录</span></div>
  <div class="stat"><b>{n_legmon}</b><span>老版怪物（DAT）</span></div>
  <div class="stat"><b>{n_legitem}</b><span>老版装备（DAT）</span></div>
  <div class="stat"><b>{n_legskill}</b><span>老版技能（DAT）</span></div>
</div>
<h2>板块</h2>
<div class="grid3">
  <div class="panel"><h3><a href="/maps">地图 · {st['ei_maps']} 张</a></h3>
    全部地图缩略图 + 每图怪物刷新 / 商人 / 守卫，支持搜索与差异过滤。</div>
  <div class="panel"><h3><a href="/monsters">怪物图鉴 · {nm} 种</a></h3>
    怪物中文名聚合，显示刷新数量与分布地图。</div>
  <div class="panel"><h3><a href="/items">装备 · {len(w['items'])} 件</a></h3>
    武器 / 防具 / 首饰 / 消耗品 / 材料，按职业过滤，附属性与掉落。</div>
  <div class="panel"><h3><a href="/skills">技能 · {len(w['skills'])} 个</a></h3>
    战士 / 法师 / 道士技能表，威力、耗蓝、等级门槛。</div>
  <div class="panel"><h3><a href="/npcs">NPC · {len(w['npcs'])} 位</a></h3>
    地图分布 + 可接 / 可交任务。</div>
  <div class="panel"><h3><a href="/quests">任务 · {len(w['quests'])} 个</a></h3>
    任务接取、目标与奖励。</div>
  <div class="panel"><h3><a href="/companions">宠物与坐骑 · {c} 种</a></h3>
    宠物商店宠物与坐骑一览。</div>
  <div class="panel"><h3><a href="/stores">商店 · {s['stats']['kinds']} 类</a></h3>
    武器店 / 防具店 / 药店 / 首饰店等 {s['stats']['kinds']} 类商店，NPC 与在售货品（图文）。</div>
  <div class="panel"><h3><a href="/library">资源库</a></h3>
    EI 客户端 WIL 图库浏览（怪物 / 装备 / 地图贴图 / 图标）。</div>
  <div class="panel"><h3><a href="/diff">差异裁剪</a></h3>
    EI 客户端 vs mir3ei vs Zircon 差异对照 + 老版 DAT vs Zircon 逐条对照表，为裁剪 mir3ei 新内容提供依据。</div>
</div>
"""
        self._send(page("首页", body, "home"))

    # ---------------- 地图
    def maps(self, qs):
        q = urllib.parse.parse_qs(qs)
        kw = (q.get("q", [""])[0]).strip().lower()
        ver = q.get("ver", [""])[0]
        only = q.get("only", [""])[0]   # 有怪物 / 有商人
        ver_of = {fv["name"].lower(): set(fv.get("ver", [])) for fv in Data.ei_maps}
        rows = []
        for f in Data.report.get("mei_only", []):
            rows.append({"file": f, "ver": ["mei"], "w": "—", "h": "—",
                         "spawns": [], "merchants": [], "srv_name": "", "no_thumb": True})
        for m in Data.report["report"]:
            m = dict(m)
            m["ver"] = sorted(ver_of.get(m["file"].lower(), ["ei"]))
            rows.append(m)
        rows = [m for m in rows
                if ver_matches(m.get("ver"), ver)
                and (not only or (only == "mon" and m["spawns"]) or (only == "npc" and m["merchants"]))]
        if kw:
            rows = [m for m in rows if kw in m["file"].lower()
                    or kw in (m.get("srv_name") or "").lower()]
        rows.sort(key=lambda x: x["file"].lower())
        stats = Data.report["stats"]
        cards = []
        for m in rows:
            flags = ver_badges(m.get("ver"))
            nmon = len(m["spawns"]); nnpc = len(m["merchants"])
            sub = f"{m['w']}×{m['h']} · {nmon} 种怪物 · {nnpc} 个NPC"
            if m.get("srv_name"): sub = f"{esc(m['srv_name'])} · {sub}"
            thumb = "" if m.get("no_thumb") else ('<img class="thumb" loading="lazy" decoding="async" src="/thumb/' + urllib.parse.quote(m['file']) + '" alt="' + esc(m['file']) + '">')
            cards.append(f"""<div class="card">
  <a href="/map/{urllib.parse.quote(m['file'])}">
    {thumb}
    <div><span class="name">{esc(m['file'])}</span>{flags}</div>
    <div class="sub">{sub}</div>
  </a></div>""")
        body = f"""
<h1>地图 · {stats['ei_maps']} 张（EI 客户端 + mir3ei 独有）</h1>
<p class="lead">全部地图缩略图 + 每图怪物刷新 / 商人 / 守卫，支持搜索与版本过滤。</p>
<form class="filters" method="get" action="/maps">
  <input name="q" placeholder="搜索文件名或中文名…" value="{esc(kw)}">
  {ver_select(ver)}
  <select name="only">
    <option value="">全部地图</option>
    <option value="mon" {"selected" if only=="mon" else ""}>有怪物刷新</option>
    <option value="npc" {"selected" if only=="npc" else ""}>有 NPC</option>
  </select>
  <button type="submit">筛选</button>
</form>
<p class="lead">共 {len(rows)} 张</p>
<div class="cards">{''.join(cards)}</div>
"""
        self._send(page("地图", body, "maps"))

    def map_detail(self, fname):
        f = fname
        m = Data.map_by_file.get(f)
        if not m:
            if f.lower() in (x.lower() for x in Data.report.get("mei_only", [])):
                self._send(page("地图", f"<h1>{esc(f)}</h1><p class='lead'>mir3ei 独有地图，无 EI 客户端数据与缩略图。</p><a href='/maps'>← 返回地图列表</a>"), code=404)
                return
            self._send(page("地图", f"<h1>未找到</h1><p class='lead'>{esc(f)}</p>"), code=404)
            return
        flags = ""
        if m["ei_only"]: flags += '<span class="tag tag-ei">EI</span>'
        if m["mei_only"]: flags += '<span class="tag tag-mei">mir3ei</span>'
        # 怪物表
        mon_rows = ""
        for name, count in sorted(m["spawns"], key=lambda x: -x[1]):
            mon_rows += f"<tr><td>{mon_link(name)}</td><td>{count}</td></tr>"
        # NPC/商人
        npc_rows = ""
        for me in Data.merch_by_map.get(m["file"].lower().removesuffix(".map"), []) + Data.merch_by_map.get(m["file"], []):
            npc_rows += f"<tr><td>{esc(me['name'])}</td><td>{esc(me.get('script',''))}</td><td>{me['x']},{me['y']}</td></tr>"
        if not npc_rows:
            npc_rows = '<tr><td colspan="3" class="lead">无 NPC 记录</td></tr>'
        # 路线图: 相连地图 (Mapinfo.txt 连接对, 双向)
        code = f.lower().removesuffix(".map")
        nb = sorted(Data.map_adj.get(code, []), key=lambda c: c.lower())
        net_rows = ""
        if nb:
            for c2 in nb:
                zh = Data.mapinfo_names.get(c2) or ""
                tgt = Data.map_code.get(c2)
                if tgt and not zh:
                    zh = tgt.get("srv_name") or ""
                label = f"{zh}（{c2}）" if zh else c2
                if tgt:
                    net_rows += (f'<a href="/map/{urllib.parse.quote(tgt["file"])}">'
                                 f'{esc(zh or c2)}<span class="dim">{esc(c2)}</span></a>')
                else:
                    net_rows += f'<a href="/maps?q={urllib.parse.quote(c2)}" title="无 EI 客户端缩略图">{esc(label)}</a>'
        else:
            net_rows = '<p class="dim">无连接记录（Mapinfo.txt 无此图的出入口）</p>'
        body = f"""
<a href="/maps">← 返回地图列表</a>
<h1>{esc(m['file'])}{flags}</h1>
<div class="detail">
  <img src="/thumb/{urllib.parse.quote(m['file'])}" alt="{esc(m['file'])}">
  <div class="meta">
    <dl class="kv">
      <dt>尺寸</dt><dd>{m['w']} × {m['h']}</dd>
      <dt>服务端名</dt><dd>{esc(m.get('srv_name','—'))}</dd>
      <dt>怪物种类</dt><dd>{len(m['spawns'])}</dd>
      <dt>NPC 数</dt><dd>{len(m['merchants'])}</dd>
      <dt>Zircon 对照</dt><dd>{'已收录' if m.get('in_zircon') else '未收录'} {file_link(m['file']) if m.get('in_zircon') else ''}</dd>
    </dl>
  </div>
</div>
<h2>相连地图（{len(nb)} 张 · 出入口）</h2>
<div class="mapnet">{net_rows}</div>
<h2>怪物刷新（{len(m['spawns'])} 种）</h2>
<table><tr><th>怪物</th><th>数量</th></tr>{mon_rows}</table>
<h2>NPC / 商人（{len(m['merchants'])} 个）</h2>
<table><tr><th>名称</th><th>脚本</th><th>坐标</th></tr>{npc_rows}</table>
"""
        self._send(page(f"地图 {m['file']}", body, "maps"))

    # ---------------- 怪物
    def monsters(self, qs):
        q = urllib.parse.parse_qs(qs)
        kw = (q.get("q", [""])[0]).strip().lower()
        ver = q.get("ver", [""])[0]
        cats = q.get("cat", [])
        lv = q.get("lv", [""])[0]
        sort = q.get("sort", [""])[0]
        mapf = q.get("map", [""])[0].strip().lower()
        try:
            pg = max(1, int(q.get("p", ["1"])[0]))
        except ValueError:
            pg = 1
        rows = [m for m in Data.monsters
                if ver_matches(m.get("ver"), ver)
                and (not kw or kw in m["name"].lower() or kw in (m.get("zh") or "").lower())
                and mon_cat_ok(m, cats)
                and mon_lv_ok(m, lv)
                and (not mapf or mapf in [c.lower() for c in mon_maps(m)])]
        rows.sort(key=lambda x: mon_sort_key(x, sort))
        pages = max(1, (len(rows) + PAGE - 1) // PAGE)
        pg = min(pg, pages)
        shown = rows[(pg - 1) * PAGE:pg * PAGE]
        items = []
        for m in shown:
            disp = m.get("zh") or m["name"]
            en = f'<span class="mono">{esc(m["name"])}</span>' if disp != m["name"] else ""
            lv_txt = f"Lv {m.get('level') or '?'}"
            sub = f"{lv_txt}"
            tags = []
            if m.get("boss"): tags.append('<span class="bad">Boss</span>')
            if m.get("undead"): tags.append('<span class="bad bad-undead">不死</span>')
            if m.get("tame"): tags.append('<span class="bad bad-tame">可捕捉</span>')
            if tags: sub += " · " + " ".join(tags)
            sp = parse_spawns(m.get("spawns"))
            if sp:
                sub += f" · {sum(c for _, c in sp)} 只 / {len(sp)} 图"
            # 分布地图中文名 (取前 3 张)
            maps_zh = []
            for c, _ in sp:
                zh = mon_map_zh(c)
                if zh:
                    maps_zh.append(zh)
                    if len(maps_zh) >= 3:
                        break
            if maps_zh:
                sub += f" · <span class='dim'>{'、'.join(esc(z) for z in maps_zh)}</span>"
            href = f"/monster/{m['id']}" if m.get("legacy") else f"/monster/{urllib.parse.quote(m['name'])}"
            items.append(f"""<div class="card"><a href="{href}">
  {icon_img('monsters', m['id'], 'pic', disp)}
  <div><span class="name">{esc(disp)}</span>{en} {ver_badges(m.get('ver'), m.get('legacy'))}</div>
  <div class="sub">{sub}</div>
</a></div>""")
        map_opts = ""
        map_names = sorted({c.lower() for m in Data.monsters for c in mon_maps(m)})
        for c in map_names:
            zh = mon_map_zh(c)
            label = f"{zh}（{c}）" if zh else c
            map_opts += f'<option value="{esc(c)}" {"selected" if mapf == c else ""}>{esc(label)}</option>'
        body = f"""
<h1>怪物图鉴 · {len(Data.monsters)} 种</h1>
<p class="lead">Zircon System.db {sum(1 for x in Data.monsters if not x.get('legacy'))} 种 + 老版 monster.dat 独有 {sum(1 for x in Data.monsters if x.get('legacy'))} 种（Boss {sum(1 for x in Data.monsters if x.get('boss'))} / 不死系 {sum(1 for x in Data.monsters if x.get('undead'))} / 可捕捉 {sum(1 for x in Data.monsters if x.get('tame'))}）。ei/mei 徽章 = 客户端素材存在性（WIL 可解帧），非服务端内容声明。</p>
<form class="filters" method="get" action="/monsters" id="filters">
  <input name="q" placeholder="搜索怪物名…" value="{esc(kw)}" class="q">
  {mon_cat_chips(cats)}
  {mon_lv_select(lv)}
  <select name="map"><option value="">全部地图</option>{map_opts}</select>
  {sort_select(sort, "monsters")}
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
<p class="lead">共 {len(rows)} 种</p>
<div class="cards">{''.join(items) or '<p class="none">无匹配条目</p>'}</div>
{pager(q, pg, pages)}
"""
        self._send(page("怪物图鉴", body, "monsters"))

    def monster_detail(self, name):
        try:
            m = Data.mon_by_id.get(int(name))
        except ValueError:
            m = Data.mon_by_zh.get(name)
            if not m:
                m = Data.mon_by_zh.get(re.sub(r"\s*\d+\s*$", "", name))
        if not m:
            self._send(page("怪物", f"<h1>未找到</h1><p class='lead'>{esc(name)}</p>"), code=404)
            return
        legacy = m.get("legacy")
        disp = m.get("zh") or m["name"]
        name_html = f' <span class="mono">{esc(m["name"])}</span>' if (disp != m["name"] and not legacy) else ""
        flags = ver_badges(m.get("ver"), legacy)
        attrs = " · ".join(esc(a) for a in m.get("attrs", []))
        sp = parse_spawns(m.get("spawns"))
        map_rows = ""
        for f, c in sorted(sp, key=lambda x: x[0].lower()):
            zh = mon_map_zh(f)
            fdisp = f"<span class='dim'>{esc(zh)}</span>" if zh else ""
            map_rows += f"<tr><td>{file_link(f + '.map')} {fdisp}</td><td>{c}</td></tr>"
        if not map_rows:
            map_rows = '<tr><td colspan="2" class="none">无刷怪记录</td></tr>'
        boss = '<span class="tag tag-ei">Boss</span>' if m.get("boss") else ''
        undead = '<span class="bad">亡灵</span>' if m.get("undead") else ''
        src_row = (f'<dt>来源</dt><dd>{esc(m.get("source",""))} · 判定 {esc(m.get("tag",""))}</dd>'
                   if legacy else "")
        note = esc(m.get("tag_note") or "") if legacy else esc(m.get("traits", ""))
        img_note = '<p class="dim">无客户端素材图（诚实占位）</p>' if legacy and not m.get("img") else ""
        old = old_block(m.get("old")) if m.get("old") else ""
        # 怪物动画播放器 (img_pipeline 渲染逐帧 PNG)
        mon_anim_html = ""
        anim_cnt = m.get("anim") or {}
        ma = Data.mon_anim.get(str(m["id"])) or {}
        if m.get("img") and ma:
            ACT_ZH = [("standing", "站立"), ("walking", "行走"), ("combat", "攻击"),
                      ("struck", "受击"), ("die", "死亡")]
            btns = ""
            for act, zh in ACT_ZH:
                n = ma.get(act, 0)
                if n <= 0:
                    continue
                on = ' class="mon-act on"' if act == "walking" else ' class="mon-act"'
                btns += (f'<button type="button"{on} data-act="{act}" data-n="{n}" '
                         f'onclick="monPlay(this, {m["id"]})">{zh}</button>')
            first_act = "walking" if ma.get("walking", 0) > 0 else "standing"
            first_n = ma.get(first_act, 0)
            mon_anim_html = f"""
<div class="mon-anim">
  <div class="anim-stage"><img id="monFrame" src="/img/mon_anim/{m['id']}/{first_act}/000.png"
       alt="怪物动画" data-mid="{m['id']}" data-act="{first_act}"></div>
  <div class="anim-ctrl">
    <span class="mon-btns">{btns}</span>
    <button type="button" id="monPlay" onclick="monPlayBtn(this)" disabled>▶ 播放</button>
    <span class="dim">第 <b id="monIdx">1</b>/<b id="monCnt">{first_n}</b> 帧 · <span id="monActZh">行走</span></span>
  </div>
</div>"""
        body = f"""
<a href="/monsters">← 返回图鉴</a>
<h1>{esc(disp)}{name_html} {flags}</h1>
<div class="detail">
  {icon_img('monsters', m['id'], 'bigpic', disp)}
  <div class="meta">
    <dl class="kv">
      <dt>等级</dt><dd>{m.get('level') or '?'} {boss} {undead}</dd>
      <dt>属性</dt><dd>{attrs or '—'}</dd>
      <dt>刷新量</dt><dd>{sum(c for _, c in sp) if sp else '—'} 只 / {len(sp)} 图</dd>
      <dt>掉落</dt><dd>{esc(m.get('drops','—'))}</dd>
      {src_row}
    </dl>
    <p class="lead">{note}</p>
    {img_note}
  </div>
</div>
{mon_anim_html}
<h2>分布地图（{len(sp)} 张）</h2>
<table><tr><th>地图</th><th>数量</th></tr>{map_rows}</table>
{old}
"""
        self._send(page(f"怪物 {disp}", body, "monsters"))

    # ---------------- 装备
    def items(self, qs):
        q = urllib.parse.parse_qs(qs)
        groups = q.get("group", [])
        klass = q.get("class", [""])[0]
        kw = (q.get("q", [""])[0]).strip().lower()
        ver = q.get("ver", [""])[0]
        sort = q.get("sort", [""])[0]
        try:
            pg = max(1, int(q.get("p", ["1"])[0]))
        except ValueError:
            pg = 1
        rows = [i for i in Data.items
                if (not groups or item_group(i) in groups)
                and (not klass or klass in i.get("class", ""))
                and (not kw or kw in i["name"].lower() or kw in (i.get("zh") or "").lower())
                and ver_matches(i.get("ver"), ver)]
        rows.sort(key=lambda i: item_sort_key(i, sort))
        pages = max(1, (len(rows) + PAGE - 1) // PAGE)
        pg = min(pg, pages)
        shown = rows[(pg - 1) * PAGE:pg * PAGE]
        klass_opts = "".join(f'<option value="{c}" {"selected" if c==klass else ""}>{c}</option>'
                             for c in ["战士","法师","道士","战法道","战道","战法","法道","全职业"])
        items = []
        for i in shown:
            en = f'<span class="mono">{esc(i["name"])}</span>' if i.get("zh") and i["zh"] != i["name"] else ""
            price = item_price(i)
            sub = esc(i.get("type_zh") or i['category']) + " · " + esc(i.get('class',''))
            if price is not None:
                sub += f' · <span class="good">{price:,} 金</span>'
            items.append(f"""<div class="card"><a href="/item/{i['id']}">
  {icon_img('items', i['id'], 'pic', i.get('zh') or i['name'])}
  <div><span class="name">{esc(i.get('zh') or i['name'])}</span>{en} {ver_badges(i.get('ver'), i.get('legacy'))}</div>
  <div class="sub">{sub}</div>
</a></div>""")
        body = f"""
<h1>装备 · {len(Data.items)} 件</h1>
<p class="lead">Zircon System.db {sum(1 for x in Data.items if not x.get('legacy'))} 件 + 老版 stditem.dat / MonItems 独有 {sum(1 for x in Data.items if x.get('legacy'))} 件（{len([g for g in ITEM_GROUPS if g[0]])} 类）。</p>
<form class="filters" method="get" action="/items" id="filters">
  <input name="q" placeholder="搜索装备名…" value="{esc(kw)}" class="q">
  {item_group_chips(groups)}
  <select name="class"><option value="">全部职业</option>{klass_opts}</select>
  {sort_select(sort, "items")}
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
<p class="lead">共 {len(rows)} 件</p>
<div class="cards">{''.join(items) or '<p class="none">无匹配条目</p>'}</div>
{pager(q, pg, pages, base="/items")}
"""
        self._send(page("装备", body, "items"))

    def item_detail(self, sid):
        try:
            i = Data.item_by_id.get(int(sid))
        except ValueError:
            i = None
        if not i:
            self._send(page("装备", f"<h1>未找到</h1><p class='lead'>#{esc(sid)}</p>"), code=404)
            return
        legacy = i.get("legacy")
        attrs = " · ".join(esc(a) for a in i.get("attrs", []))
        src_row = (f'<dt>来源</dt><dd>{esc(i.get("source",""))} · 判定 {esc(i.get("tag",""))}</dd>'
                   if legacy else "")
        note = esc(i.get("tag_note") or "") if legacy else ""
        img_note = '<p class="dim">无客户端素材图（诚实占位）</p>' if legacy and not i.get("img") else ""
        old = old_block(i.get("old")) if i.get("old") else ""
        body = f"""
<a href="/items">← 返回装备列表</a>
<h1>{esc(i.get('zh') or i['name'])} <span class="mono">{esc(i['name'])}</span> {ver_badges(i.get('ver'), legacy)}</h1>
<div class="detail">{icon_img('items', i['id'], 'bigpic', i.get('zh') or i['name'])}<div class="meta">
<dl class="kv">
  <dt>分类</dt><dd>{esc(i.get('type_zh') or i['category'])}</dd>
  <dt>职业</dt><dd>{dash(i.get('class'))}</dd>
  <dt>属性</dt><dd>{attrs or '—'}</dd>
  <dt>其他</dt><dd>{dash(i.get('meta'))}</dd>
  <dt>掉落</dt><dd>{dash(i.get('drops'))}</dd>
  <dt>套装</dt><dd>{dash(i.get('set'))}</dd>
  <dt>说明</dt><dd>{dash(i.get('desc'))}</dd>
  {src_row}
</dl>
<p class="lead">{note}</p>
{img_note}
</div></div>
{old}
"""
        self._send(page(f"装备 {i.get('zh') or i['name']}", body, "items"))

    # ---------------- 技能
    def skills(self, qs):
        q = urllib.parse.parse_qs(qs)
        ver = q.get("ver", [""])[0]
        kw = (q.get("q", [""])[0]).strip().lower()
        by = {}
        for s in Data.skills:
            if not ver_matches(s.get("ver"), ver):
                continue
            if kw and not (kw in s["name"].lower() or kw in (s.get("zh") or "").lower()):
                continue
            by.setdefault(s.get("klass") or "通用", []).append(s)
        secs = ""
        for k in Data.skill_classes + ["通用"]:
            arr = by.get(k)
            if not arr: continue
            rows = ""
            for s in arr:
                rows += (f"<tr><td>{icon_img('skills', s['id'], 'icon', s.get('zh') or s['name'])} "
                         f"<a href='/skill/{s['id']}'>{esc(s.get('zh') or s['name'])}</a> "
                         f"<span class='mono'>{esc(s['name'])}</span> {ver_badges(s.get('ver'), s.get('legacy'))}</td>"
                         f"<td>{dash(s.get('type'))} {esc(s.get('school') or '')}</td>"
                         f"<td>{dash(s.get('power'))}</td><td>{dash(s.get('cost'))}</td>"
                         f"<td>{dash(s.get('levels'))}</td><td>{esc(s.get('desc',''))[:60]}</td></tr>")
            secs += f"<h2>{esc(k)}（{len(arr)}）</h2><table><tr><th>技能</th><th>类型</th><th>威力</th><th>耗蓝</th><th>等级门槛</th><th>说明</th></tr>{rows}</table>"
        n_leg = sum(1 for s in Data.skills if s.get("legacy"))
        body = f"""<h1>技能 · {len(Data.skills)} 个</h1>
<p class="lead">Zircon System.db {len(Data.skills) - n_leg} 个 + 老版 magic.dat 独有 {n_leg} 个（老版系别标注为「老版·系别」）。</p>
<form class="filters" method="get" action="/skills" id="filters">
  <input name="q" placeholder="搜索技能名…" value="{esc(kw)}" class="q">
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
{secs or '<p class="none">无匹配条目</p>'}"""
        self._send(page("技能", body, "skills"))

    def skill_detail(self, sid):
        try:
            s = Data.skill_by_id.get(int(sid))
        except ValueError:
            s = None
        if not s:
            self._send(page("技能", f"<h1>未找到</h1><p class='lead'>#{esc(sid)}</p>"), code=404)
            return
        anim = s.get("anim")
        legacy = s.get("legacy")
        anim_html = ""
        if legacy:
            anim_html = ("<h2>施法动画</h2><p class='dim'>老版魔法（magic.dat）无特效表定义，"
                         "Zircon 亦无对应实现 → 无施法动画数据（诚实标注）</p>")
        elif anim:
            ap = os.path.join(IMGS_DIR, "skills_anim", f"{s['id']}.png")
            if os.path.exists(ap):
                nf = len(os.listdir(os.path.join(IMGS_DIR, "skills_anim", str(s["id"])))) \
                    if os.path.isdir(os.path.join(IMGS_DIR, "skills_anim", str(s["id"]))) else 0
                if nf < 1:
                    nf = anim["count"]
                src_note = "施法特效" if anim.get("src") != "attack" else "近战挥击特效"
                anim_html = f"""
<h2>{src_note}</h2>
<div class="anim" data-frames="{nf}" data-delay="{anim['delay']}" data-sid="{s['id']}">
  <div class="anim-stage"><img id="animFrame" src="/img/skills_anim/{s['id']}/000.png" alt="{src_note}"></div>
  <div class="anim-ctrl">
    <button type="button" id="animPlay" onclick="animPlay(this)">▶ 播放</button>
    <span class="dim">第 <b id="animIdx">1</b>/{nf} 帧</span>
    <span class="dim">· {esc(anim['lib'])} 帧 {anim['start']}+{anim['count']} · 每帧 {anim['delay']}ms（来自客户端 MagicEffectTable 渲染表）</span>
  </div>
</div>"""
            else:
                anim_html = f"<h2>施法动画</h2><p class='dim'>素材 {esc(anim['lib'])} 帧 {anim['start']} 暂无渲染图</p>"
        else:
            prop = (s.get("prop") or "").strip()
            if prop in ("Active", "Charge"):
                anim_html = ("<h2>施法动画</h2><p class='dim'>主动技能但 MagicEffectTable "
                             "未收录施法特效（无动画记录，诚实标注）</p>")
            else:
                anim_html = "<h2>施法动画</h2><p class='dim'>被动 / 无独立施法特效</p>"
        src_row = (f'<dt>来源</dt><dd>{esc(s.get("source",""))} · 判定 {esc(s.get("tag",""))}</dd>'
                   if legacy else "")
        old = old_block(s.get("old")) if s.get("old") else ""
        body = f"""
<a href="/skills">← 返回技能列表</a>
<h1>{esc(s.get('zh') or s['name'])} <span class="mono">{esc(s['name'])}</span> {ver_badges(s.get('ver'), legacy)}</h1>
<div class="detail">{icon_img('skills', s['id'], 'bigpic', s.get('zh') or s['name'])}<div class="meta">
<dl class="kv">
  <dt>职业</dt><dd>{dash(s.get('klass'))}</dd>
  <dt>类型</dt><dd>{dash(s.get('type'))} · {dash(s.get('school'))}</dd>
  <dt>威力</dt><dd>{dash(s.get('power'))}</dd>
  <dt>耗蓝</dt><dd>{dash(s.get('cost'))}</dd>
  <dt>冷却</dt><dd>{dash(s.get('delay'))}</dd>
  <dt>等级门槛</dt><dd>{dash(s.get('levels'))}</dd>
  <dt>升级经验</dt><dd>{dash(s.get('exp'))}</dd>
  <dt>说明</dt><dd>{dash(s.get('desc'))}</dd>
  {src_row}
</dl></div></div>
{anim_html}
{old}
"""
        self._send(page(f"技能 {s.get('zh') or s['name']}", body, "skills"))

    # ---------------- NPC
    def npcs(self, qs):
        q = urllib.parse.parse_qs(qs)
        ver = q.get("ver", [""])[0]
        rows = ""
        for n in sorted(Data.npcs, key=lambda x: x["name"]):
            if not ver_matches(n.get("ver"), ver):
                continue
            # 坐标 (来自 Region.PointRegion; 商店 NPC 经 stores 合并)
            st = Data.store_by_npc.get(n["id"])
            pos = "—"
            if st:
                p = st[1]["npcs"][0].get("pos") or {}
                if p.get("x") is not None:
                    pos = f"({p['x']}, {p['y']})"
            # 所属商店
            shop = f'<a class="chip" href="/store/{st[0]}">{esc(st[1]["name_zh"])}</a>' if st else ""
            rows += (f"<tr><td>{icon_img('npcs', n['id'], 'icon', n.get('zh') or n['name'])} "
                     f"<a href='/npc/{n['id']}'>{esc(n.get('zh') or n['name'])}</a> <span class='mono'>{esc(n['name'])}</span> {ver_badges(n.get('ver'))}</td>"
                     f"<td>{file_link(n['map'] + '.map') if n.get('map') else '—'}</td>"
                     f"<td class='mono'>{pos}</td>"
                     f"<td>{shop}</td>"
                     f"<td>{dash(n.get('desc'))}</td>"
                     f"<td>{n.get('quests_in',0)} / {n.get('quests_out',0)}</td></tr>")
        body = f"""<h1>NPC · {len(Data.npcs)} 位</h1>
<p class="lead">地图为 Zircon 视图编号；坐标来自地图区域点（Region.PointRegion）。点击 NPC 名查看详情。</p>
<form class="filters" method="get" action="/npcs">
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
<table><tr><th>NPC</th><th>地图</th><th>坐标</th><th>所属商店</th><th>介绍</th><th>可接/可交任务</th></tr>{rows or '<tr><td colspan="6" class="none">无匹配条目</td></tr>'}</table>"""
        self._send(page("NPC", body, "npcs"))

    # ---------------- NPC 详情
    def npc_detail(self, nid):
        w, r, s = Data.get()
        n = Data.npc_by_id.get(nid)
        if n is None:
            self._send(page("404", f"<h1>404</h1><p class='lead'>NPC 不存在: {esc(nid)}</p>"), code=404)
            return
        # 坐标: 商店侧 pos (Region.PointRegion); 非商店 NPC 无
        store_info = Data.store_by_npc.get(nid)
        pos = None
        if store_info:
            pos = (store_info[1]["npcs"][0].get("pos") or {}).get("x"), (store_info[1]["npcs"][0].get("pos") or {}).get("y")
        # 所属商店链接
        store_link = ""
        if store_info:
            si, sh = store_info
            store_link = f'<a class="chip" href="/store/{si}">{esc(sh["name_zh"])}</a>'
        # 头像 (face 优先) + 全身像备用
        face = npc_img(n, "pic")
        full = icon_img("npcs", nid, "pic", n.get("zh") or n["name"])
        # 地图: 视图编号 -> 地图详情
        mlink = file_link(n["map"] + ".map") if n.get("map") else "—"
        pos_html = f"({pos[0]}, {pos[1]})" if pos and pos[0] is not None else "—"
        body = f"""<p class="crumbs"><a href="/npcs">NPC</a> › {esc(n.get('zh') or n['name'])}</p>
<h1>{esc(n.get('zh') or n['name'])} <span class="mono">{esc(n['name'])}</span> {ver_badges(n.get('ver'))}</h1>
<div class="detail">{face}<div class="meta">
<div class="sub">头像来源 NPCface.Zl · <a href="/npcs">返回列表</a></div>
{store_link}
</div></div>
<p class="lead">地图 {mlink} · 坐标 {pos_html}</p>
<div class="grid2">
<div class="panel"><h3>介绍</h3><p>{esc(n.get('desc') or '—')}</p></div>
<div class="panel"><h3>任务</h3><p>可接 {n.get('quests_in',0)} · 可交 {n.get('quests_out',0)}</p></div>
</div>
<h2>全身像</h2>
<div class="panel-npc">{full}<div><div class="name">{esc(n.get('zh') or n['name'])}</div>
<div class="sub">{esc(n['name'])} · {esc(n.get('map',''))}</div></div></div>"""
        self._send(page(n.get("zh") or n["name"], body, "npcs"))

    # ---------------- 任务
    def quests(self, qs):
        q = urllib.parse.parse_qs(qs)
        ver = q.get("ver", [""])[0]
        rows = ""
        for qq in Data.quests:
            if not ver_matches(qq.get("ver"), ver):
                continue
            rows += (f"<tr><td>{esc(qq.get('zh') or qq['name'])} <span class='mono'>{esc(qq['name'])}</span> {ver_badges(qq.get('ver'))}</td>"
                     f"<td>{esc(qq.get('type',''))}</td><td>{esc(qq.get('npc',''))}</td>"
                     f"<td>{esc(qq.get('desc',''))}</td><td>{esc(qq.get('rewards',''))}</td></tr>")
        body = f"""<h1>任务 · {len(Data.quests)} 个</h1>
<form class="filters" method="get" action="/quests">
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
<table><tr><th>任务</th><th>类型</th><th>接取</th><th>说明</th><th>奖励</th></tr>{rows or '<tr><td colspan="5" class="none">无匹配条目</td></tr>'}</table>"""
        self._send(page("任务", body, "quests"))

    # ---------------- 宠物与坐骑
    def companions(self, qs):
        q = urllib.parse.parse_qs(qs)
        ver = q.get("ver", [""])[0]
        cards = ""
        mon = {m["id"]: m for m in Data.monsters}
        for c in Data.companions:
            if not ver_matches(c.get("ver"), ver):
                continue
            m = mon.get(c["monster_id"])
            mlink = f'<a href="/monster/{urllib.parse.quote(m["name"])}">{esc(m.get("zh") or m["name"])}</a>' if m else "—"
            avail = '<span class="good">可购买</span>' if c.get("available") else '<span class="bad">未开放</span>'
            has_img = os.path.exists(os.path.join(IMGS_DIR, "companion", f"{c['id']}.png"))
            img_note = " · 缺图（无客户端素材，诚实占位）" if not has_img else ""
            cards += f"""<div class="card"><a href="/companions">
  {icon_img('companion', c['id'], 'pic', c['name'])}
  <div><span class="name">{esc(c['name'])}</span> {ver_badges(c.get('ver'))}</div>
  <div class="sub">宠物 #{c['monster_id']} · 对应怪物 {mlink}</div>
  <div class="sub">价格 {c.get('price') or '—'} 金币 · {avail}{img_note}</div>
</a></div>"""
        body = f"""<h1>宠物与坐骑 · {len(Data.companions)} 种</h1>
<p class="lead">来自 CompanionInfo 表。对应怪物条目见怪物图鉴。</p>
<form class="filters" method="get" action="/companions">
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
<div class="cards">{cards or '<p class="none">无匹配条目</p>'}</div>"""
        self._send(page("宠物与坐骑", body, "companions"))

    # ---------------- 商店
    def stores(self, qs):
        q = urllib.parse.parse_qs(qs)
        kw = (q.get("q", [""])[0]).strip().lower()
        kind = q.get("kind", [""])[0]
        w, r, s = Data.get()
        st = s["stats"]
        # 类型列表 (保序)
        kinds = []
        seen = set()
        for sh in s["stores"]:
            if sh["kind"] not in seen:
                seen.add(sh["kind"])
                kinds.append(sh["kind"])
        # 筛选
        shops = []
        for sh in s["stores"]:
            if kind and sh["kind"] != kind:
                continue
            if kw:
                n = sh["npcs"][0]
                if not (kw in sh["name"].lower() or kw in n["name"].lower()
                        or kw in n["zh"].lower() or kw in sh["kind_zh"].lower()):
                    continue
            shops.append(sh)
        # 商店卡片 (店名 + NPC + 货品图标行)
        cards = ""
        for si, sh in enumerate(shops):
            n = sh["npcs"][0]
            goods_n = len(sh["goods"])
            gn = f"· {goods_n} 件货品" if goods_n else "· 服务型"
            gicons = ""
            if sh["goods"]:
                gicons = '<div class="store-goods">'
                for gd in sh["goods"][:8]:
                    price_s = f"{gd['price']:,}" if gd["price"] else "—"
                    gicons += (f'<a class="sgood" href="/item/{gd["item_id"]}" '
                               f'title="{esc(gd["zh"])} · {price_s} 金币">'
                               f'{icon_img("items", gd["item_id"], "thumb", gd["zh"])}'
                               f'<span class="sprice">{price_s}</span></a>')
                gicons += f'<span class="more"><a href="/store/{si}">…共 {goods_n} 件</a></span>' if goods_n > 8 else ""
                gicons += "</div>"
            # 店主坐标
            p = n.get("pos") or {}
            pos_s = f"({p['x']}, {p['y']})" if p.get("x") is not None else ""
            cards += f"""<div class="card"><div class="store-body">
  <div class="store-head"><a class="name" href="/store/{si}">{esc(sh['name_zh'])}</a> {gn}</div>
  <div class="sub">{npc_img(n, 'avatar')} <a href="/npc/{n['id']}">{esc(n['zh'])}</a> <span class="mono">{esc(n['name'])}</span> · 地图 {esc(sh['map'])} {pos_s}</div>
  {gicons}
</div></div>"""
        # 类型 chips
        chips = ('<a class="chip on" href="/stores">全部 · %d</a>' % st["shops"]
                 if not kind else '<a class="chip" href="/stores">全部 · %d</a>' % st["shops"])
        for k in kinds:
            on = ' class="chip on"' if kind == k else ' class="chip"'
            cnt = sum(1 for sh in s["stores"] if sh["kind"] == k)
            chips += f'<a{on} href="/stores?kind={urllib.parse.quote(k)}">{esc(KIND_ZH.get(k, k))} · {cnt}</a>'
        body = f"""<h1>商店 · {st['shops']} 家</h1>
<p class="lead">按 NPC 所在位置分组（武器店 / 药店 / 防具店…），货品按商店类别匹配（武器店卖武器、书店卖技能书…），价格来自物品表。</p>
<form class="filters" method="get" action="/stores">
  <input name="q" value="{esc(kw)}" placeholder="搜索商店 / NPC">
  <button type="submit">搜索</button>
</form>
<div class="chips">{chips}</div>
<div class="cards">{cards or '<p class="none">无匹配商店</p>'}</div>"""
        self._send(page("商店", body, "stores"))

    # ---------------- 商店详情
    def store_detail(self, idx):
        w, r, s = Data.get()
        if idx < 0 or idx >= len(s["stores"]):
            self._send(page("404", f"<h1>404</h1><p class='lead'>商店不存在: {esc(idx)}</p>"), code=404)
            return
        sh = s["stores"][idx]
        n = sh["npcs"][0]
        # NPC 图 (头像优先, 回退全身)
        npc_pic = npc_img(n, "pic")
        # 坐标
        p = n.get("pos") or {}
        pos_s = f"({p['x']}, {p['y']})" if p.get("x") is not None else "—"
        # 地图链接
        mlink = file_link(sh["map"] + ".map")
        # 货品表格: 图 + 名 + 类型 + 价格 (+ 物品详情链接)
        rows = ""
        for gd in sh["goods"]:
            price_s = f"{gd['price']:,}" if gd["price"] else "—"
            rows += (f"<tr><td>{icon_img('items', gd['item_id'], 'icon', gd['zh'])} "
                     f"<a href='/item/{gd['item_id']}'>{esc(gd['zh'])}</a> "
                     f"<span class='mono'>{esc(gd['name'])}</span></td>"
                     f"<td>{esc(gd.get('type_zh',''))}</td>"
                     f"<td>{price_s}</td></tr>")
        body = f"""<p class="crumbs"><a href="/stores">商店</a> › {esc(sh['name_zh'])}</p>
<h1>{esc(sh['name_zh'])} <span class="dim">· {esc(sh['kind'])}</span></h1>
<p class="lead">地图 {mlink} · 店主 <a href="/npc/{n['id']}">{esc(n['zh'])}</a> <span class="mono">{esc(n['name'])}</span> · 坐标 {pos_s}</p>
<div class="panel-npc">{npc_pic}<div><div class="name">{esc(n['zh'])}</div>
<div class="sub">{esc(n['name'])} · {esc(n['map'])} · 坐标 {pos_s}</div></div></div>
<h2>在售货品 · {len(sh['goods'])} 件</h2>
<table><tr><th>货品</th><th>类型</th><th>价格</th></tr>{rows or '<tr><td colspan="3" class="none">服务型商店, 无在售货品</td></tr>'}</table>
<p class="sub">货品按商店类别匹配（同类型店共享货架）; 价格 = 物品基础价 × 商店倍率。</p>"""
        self._send(page(sh["name_zh"], body, "stores"))

    # ---------------- 资源库（WIL 浏览）
    def library(self, qs):
        q = urllib.parse.parse_qs(qs)
        cat = q.get("cat", [""])[0]
        try:
            sys.path.insert(0, ROOT)
            from wilsdk import scan_libraries, categorize
        except Exception as e:
            self._send(page("资源库", f"<h1>资源库</h1><p class='lead'>wilsdk 不可用: {esc(e)}</p>"))
            return
        libs = scan_libraries(EI_DATA)
        cats = {}
        for lib in libs:
            c = categorize(lib.name)
            cats.setdefault(c, []).append(lib)
        secs = ""
        for c in sorted(cats):
            if cat and c != cat: continue
            lis = "".join(f'<a class="badge" href="/library?cat={urllib.parse.quote(c)}&lib={urllib.parse.quote(l.name)}">{esc(l.name)}</a>'
                          for l in sorted(cats[c], key=lambda x: x.name))
            secs += f"<h2>{esc(c)}（{len(cats[c])}）</h2><p>{lis}</p>"
        lib = q.get("lib", [""])[0]
        libsec = ""
        if lib:
            libsec = f"<h2>{esc(lib)} 预览</h2><p class='lead'>图库帧预览（前 24 帧）</p>" + self._wil_preview(lib)
        body = f"<h1>资源库 · WIL 图库</h1><p class='lead'>EI 客户端 Data/ 目录图库按类别浏览。</p>{secs}{libsec}"
        self._send(page("资源库", body, "library"))

    def _wil_preview(self, lib):
        try:
            from wilsdk import open_library, contact_sheet
            from PIL import Image
            import io, base64
            path = os.path.join(EI_DATA, lib)
            if not os.path.exists(path):
                return f'<p class="lead">找不到 {esc(lib)}</p>'
            wl = open_library(path)
            n = wl.count if wl.count else 0
            n = min(n, 24)
            imgs = []
            for i in range(n):
                try:
                    im = wl.decode(i)
                except Exception:
                    im = None
                imgs.append(im)
            sheet = contact_sheet(imgs, cols=6, scale=2)
            buf = io.BytesIO()
            sheet.convert("RGB").save(buf, "PNG")
            b64 = base64.b64encode(buf.getvalue()).decode()
            return f'<p class="lead">共 {esc(wl.count)} 帧 · 前 {n} 帧</p>' \
                   f'<img src="data:image/png;base64,{b64}" style="max-width:100%">'
        except Exception as e:
            import traceback; traceback.print_exc()
            return f'<p class="lead">预览失败: {esc(e)}</p>'

    # ---------------- 差异裁剪（三版本比对）
    def diff(self):
        import json as _json
        tv_path = "/tmp/three_versions.json"
        if os.path.exists(tv_path):
            tv = _json.load(open(tv_path, encoding="utf-8"))
        else:
            tv = None
        if tv is None:
            self._send(page("差异裁剪", "<h1>差异裁剪</h1><p class='lead'>缺少三版本分析数据 /tmp/three_versions.json，请先运行 three_versions_check.py。</p>", "diff"))
            return
        m = tv["maps"]; mo = tv["monsters"]; it = tv["items"]; sk = tv["skills"]
        ei_map_names = {x["name"].lower() for x in Data.wiki.get("ei_maps", [])}

        def map_rows(files, with_link=True):
            rows = ""
            for f in files:
                # EI 客户端无此文件时不给链接 (点了只会 404)
                has_ei = (f.lower() in ei_map_names)
                cell = file_link(f) if (with_link and has_ei) else esc(f)
                zh = m.get("mud3_only_zh", {}).get(f, "")
                zh_cell = f' <span class="dim">{esc(zh)}</span>' if zh else ""
                rows += f"<tr><td>{cell}{zh_cell}</td></tr>"
            return rows

        # EI 独有地图带尺寸 (report 顶层 ei_only)
        ei_rows = ""
        for f in m["ei_only"]:
            mf = Data.map_by_file.get(f)
            sz = f"{mf['w']}×{mf['h']}" if mf else "—"
            ei_rows += f"<tr><td>{file_link(f)}</td><td>{sz}</td></tr>"
        stat_cards = f"""
<div class="grid4">
  <div class="stat"><b>{m['mud3_total']}</b><span>MUD3 服务端地图</span></div>
  <div class="stat"><b>{m['ei_total']}</b><span>EI 客户端地图</span></div>
  <div class="stat"><b>{m['mei_total']}</b><span>mir3ei 地图</span></div>
  <div class="stat"><b>{m['zir_total']}</b><span>Zircon 地图</span></div>
</div>
<div class="grid3">
  <div class="stat"><b>{len(m['mud3_only'])}</b><span>MUD3 独有</span></div>
  <div class="stat"><b>{len(m['mei_mud3_shared'])}</b><span>MUD3+mir3ei 共享</span></div>
  <div class="stat"><b>{len(m['zir_only'])}</b><span>Zircon 独有</span></div>
  <div class="stat"><b>{len(m['core'])}</b><span>四版核心</span></div>
  <div class="stat"><b>{mo['mud3_only_count']}</b><span>MUD3 独有怪物</span></div>
  <div class="stat"><b>{sk['assassin_count']}</b><span>Zircon 独有刺客技能</span></div>
</div>"""

        # 怪物表
        mon_rows = ""
        for x in mo["mud3_only"][:40]:
            mon_rows += (f"<tr><td>{esc(x['zh'])}</td><td>{x['count']:,}</td>"
                         f"<td>{len(x['maps'])}</td><td>{esc('、'.join(x['variants'][:3]))}</td></tr>")
        mon_more = ""
        if mo["mud3_only_count"] > 40:
            mon_more = (f"<p class='lead'>…另有 {mo['mud3_only_count'] - 40} 种。"
                        f"<a href='/monsters?ver=mud3'>查看 MUD3 独有怪物完整名单（含老版 DAT 属性与刷怪） →</a></p>")
        else:
            mon_more = f"<p class='lead'><a href='/monsters?ver=mud3'>查看 MUD3 独有怪物完整名单 →</a></p>"

        # 装备表
        it_rows = ""
        for n in it["mud3_only"][:60]:
            it_rows += f"<tr><td>{esc(n)}</td></tr>"
        it_more = (f"<p class='lead'>…另有 {max(0, it['mud3_only_count'] - 60)} 件，共 {it['mud3_only_count']} 件。"
                   f"<a href='/items?ver=mud3'>查看 MUD3 独有装备完整名单（含老版 DAT 属性与掉落） →</a></p>") if it["mud3_only_count"] > 60 else f"<p class='lead'><a href='/items?ver=mud3'>查看 MUD3 独有装备完整名单 →</a></p>"

        # 刺客技能
        as_rows = ""
        for n in sk["assassin_skills"][:40]:
            as_rows += f"<tr><td>{esc(n)}</td></tr>"

        # 老版 DAT vs Zircon 对照（both/changed 挂靠, 来自 wiki_data_v2.json 的 old 子对象）
        oldsecs = ""
        for label, board, pfx, keyf in (("怪物", "monsters", "/monster/", "name"),
                                        ("装备", "items", "/item/", "id"),
                                        ("技能", "skills", "/skill/", "id")):
            rows = ""
            cnt = 0
            for x in Data.wiki.get(board, []):
                rec = x.get("old")
                if not rec:
                    continue
                cnt += 1
                href = pfx + (urllib.parse.quote(x["name"]) if keyf == "name" else str(x["id"]))
                f = rec.get("fields") or {}
                kv = " · ".join(f"{esc(k)} {esc(v)}" for k, v in list(f.items())[:4])
                rows += (f"<tr><td><a href='{href}'>{esc(x.get('zh') or x['name'])}</a></td>"
                         f"<td>{esc(rec.get('source',''))}</td><td>{kv}</td>"
                         f"<td><span class='badge'>{esc(rec.get('tag',''))}</span> {esc(rec.get('tag_note',''))}</td></tr>")
            oldsecs += (f"<h2>老版 DAT vs Zircon 对照 · {label}（{cnt}）</h2>"
                        f"<table><tr><th>条目</th><th>来源</th><th>老版关键属性</th><th>判定 / 备注</th></tr>"
                        f"{rows or '<tr><td colspan=\"4\" class=\"none\">无挂靠条目</td></tr>'}</table>")

        body = f"""
<h1>差异裁剪 · 三版本比对</h1>
<p class="lead">以 MUD3 服务端（最早）为基线，对照 mir3ei（中间）与 Zircon（20 年后实现）。
数据源: MUD3 Envir/Mapinfo.txt + Mon_Def + MonItems + 老版 EI2.0 服务端三 DAT 解码 · EI 客户端 544 图 · mir3ei 566 图 · Zircon System.db。
完整分析见 <a href="docs/EI_CLIENT_DIFF_2026-08-09.md">EI_CLIENT_DIFF_2026-08-09.md</a>。</p>
{stat_cards}

<h2>MUD3 独有地图（{len(m['mud3_only'])} 张）</h2>
<p class="lead">仅 MUD3 服务端存在（其余三版本无），多为活动/试练/后期新增图:</p>
<table><tr><th>地图</th><th>中文名</th></tr>{map_rows(m['mud3_only'])}</table>

<h2>MUD3 + mir3ei 共享（{len(m['mei_mud3_shared'])} 张）</h2>
<p class="lead">MUD3 服务端与 mir3ei 都有，EI 客户端与 Zircon 无 —— 即真天宫/诺玛深 D1500 系等中后期地图:</p>
<table><tr><th>地图</th><th>中文名</th></tr>{map_rows(m['mei_mud3_shared'])}</table>

<h2>三版共享（无 EI 客户端）（{len(m['three_wo_ei'])} 张）</h2>
<table><tr><th>地图</th><th>中文名</th></tr>{map_rows(m['three_wo_ei'])}</table>

<h2>EI 客户端独有（{len(m['ei_only'])} 张）</h2>
<table><tr><th>地图</th><th>尺寸</th></tr>{ei_rows}</table>

<h2>Zircon 独有地图（{len(m['zir_only'])} 张）</h2>
<p class="lead">仅 Zircon 本地库存在（20 年后新实现，含英文命名新图）:</p>
<table><tr><th>地图</th></tr>{map_rows(m['zir_only'][:60])}</table>
<p class="lead">…另有 {max(0, len(m['zir_only']) - 60)} 张，共 {len(m['zir_only'])} 张。</p>

<h2>怪物差异</h2>
<p class="lead">MUD3 服务端刷怪聚合 {mo['mud3_total']} 种，与 Zircon {mo['zir_total']} 种按「中文词根 + 英文族关键词」双层匹配后：
<strong>{mo['shared_count']} 种共享</strong>、<strong>{mo['mud3_only_count']} 种为 MUD3 真独有</strong>（Zircon 无对应怪物/怪物族）。</p>
<p class="lead">共享中含大量「术语表缺口」：诺玛(Numa 系 10)、祖玛(Zuma 系 5)、沃玛(Oma 系 6)、骷髅(Skeleton 系 9)、蜘蛛(Spider 系 4)等在 Zircon 以英文名存在，仅术语表未收录中文音译名。</p>
<table><tr><th>MUD3 独有怪物</th><th>刷怪量</th><th>地图数</th><th>变体</th></tr>{mon_rows}</table>{mon_more}

<h2>装备差异</h2>
<p class="lead">MUD3 服务端 MonItems 掉落表 {it['mud3_total']} 件，Zircon {it['zir_total']} 件；<strong>MUD3 独有 {it['mud3_only_count']} 件</strong>（Zircon 装备表无对应）。</p>
<table><tr><th>MUD3 独有装备</th></tr>{it_rows}</table>{it_more}

<h2>技能差异</h2>
<p class="lead">Zircon 共 {sk['zir_total']} 技能：三职业（战法道）{sk['three_class_count']} + <strong>刺客 {sk['assassin_count']} = Zircon 独有</strong>（MUD3/mir3ei 无刺客职业）。
职业分布: {'、'.join(f'{k} {v}' for k, v in sk['klass_dist'].items())}。</p>
<table><tr><th>Zircon 独有刺客技能</th></tr>{as_rows}</table>

{oldsecs}
"""
        self._send(page("差异裁剪", body, "diff"))

    # ---------------- 条目图片（/img/<board>/<id>.png 或 /img/<board>/<id>/<frame>.png, 磁盘缓存）
    def img(self, path):
        parts = path.split("/")
        if len(parts) == 4 and parts[0] == "mon_anim":
            board, sid, act, fname = parts
            iid = sid
        elif len(parts) == 3 and parts[0] in IMG_BOARDS:
            board, sid, fname = parts
            iid = sid
        elif len(parts) == 2 and parts[0] in IMG_BOARDS:
            board, fname = parts
            iid = fname.removesuffix(".png")
        else:
            self._send(page("404", f"<p class='lead'>图片不存在: {esc(path)}</p>"), code=404)
            return
        if iid.lstrip("-").isdigit():
            if len(parts) == 4:
                png = os.path.join(IMGS_DIR, board, str(int(iid)), act, fname)
            elif len(parts) == 3:
                # /img/<board>/<id>/<frame>.png → <board>/<id>/<frame>.png
                png = os.path.join(IMGS_DIR, board, str(int(iid)), fname)
            else:
                png = os.path.join(IMGS_DIR, board, f"{int(iid)}.png")
        else:
            self._send(page("404", f"<p class='lead'>图片不存在: {esc(path)}</p>"), code=404)
            return
        if not os.path.exists(png):
            self._send(page("404", f"<p class='lead'>图片未生成: {esc(path)}</p>"), code=404)
            return
        with open(png, "rb") as fh:
            data = fh.read()
        self._send(data, "image/png")

    # ---------------- 缩略图
    def thumb(self, f):
        # 防路径穿越 + 大小写归位（report 用小写, 磁盘是原名）
        f = os.path.basename(f)
        real = Data.thumb_name.get(f.lower(), f)
        png = os.path.join(THUMBS_DIR, real + ".png")
        if not os.path.exists(png):
            self._send(page("缩略图", f"<p class='lead'>缩略图未生成: {esc(f)}</p>"), code=404)
            return
        with open(png, "rb") as fh:
            data = fh.read()
        self._send(data, "image/png")

def main():
    import argparse
    ap = argparse.ArgumentParser(description="EI 传奇3.0 游戏百科本地服务")
    ap.add_argument("--port", type=int, default=PORT)
    ap.add_argument("--host", default="127.0.0.1")
    args = ap.parse_args()
    # 预热数据
    Data.get()
    srv = ThreadingHTTPServer((args.host, args.port), Handler)
    print(f"[*] EI 百科服务: http://{args.host}:{args.port}/  (数据 {DATA_JSON})")
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        print("\n[*] 停止。")

if __name__ == "__main__":
    main()
