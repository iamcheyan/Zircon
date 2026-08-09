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
THUMBS_DIR = "/tmp/wiki_thumbs"
IMGS_DIR = "/tmp/wiki_imgs"
IMG_BOARDS = ("monsters", "items", "skills", "npcs", "companion")
EI_CLIENT = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端"
EI_MAPS = os.path.join(EI_CLIENT, "Map")
EI_DATA = os.path.join(EI_CLIENT, "Data")
PORT = 8777

# ---------------------------------------------------------------- ver 筛选
VER_OPTS = [("", "全部版本"), ("ei", "仅 EI 独有"), ("mei", "仅 mir3ei 独有"), ("zir", "仅 Zircon 独有")]

def ver_matches(ver, sel):
    """条目 ver 集合是否命中筛选。仅 X 独有 = 含 X 且不含另外两个客户端版。"""
    if not sel:
        return True
    s = set(ver or [])
    if sel == "ei":
        return "ei" in s and "mei" not in s and "zircon" not in s
    if sel == "mei":
        return "mei" in s and "ei" not in s and "zircon" not in s
    if sel == "zir":
        return "zircon" in s and "ei" not in s and "mei" not in s and "mud3" not in s
    return True

def ver_select(sel):
    opts = "".join(f'<option value="{v}" {"selected" if v == sel else ""}>{l}</option>'
                   for v, l in VER_OPTS)
    return f'<select name="ver">{opts}</select>'

# ---------------------------------------------------------------- 怪物分类/等级
MON_CATS = [("", "全部类型"), ("boss", "Boss"), ("undead", "不死系"), ("normal", "普通")]
MON_LVS = [("", "全部等级"), ("0-9", "0-9 级"), ("10-29", "10-29 级"),
           ("30-59", "30-59 级"), ("60-89", "60-89 级"), ("90", "90 级以上")]

def mon_cat_ok(m, cat):
    if not cat:
        return True
    if cat == "boss": return m.get("boss")
    if cat == "undead": return m.get("undead")
    if cat == "tame": return m.get("tame")
    if cat == "normal": return not m.get("boss") and not m.get("undead")
    return True

def mon_lv_ok(m, lv):
    if not lv:
        return True
    l = m.get("level") or 0
    if lv == "0-9": return l <= 9
    if lv == "10-29": return 10 <= l <= 29
    if lv == "30-59": return 30 <= l <= 59
    if lv == "60-89": return 60 <= l <= 89
    if lv == "90": return l >= 90
    return True

def mon_cat_select(sel):
    opts = "".join(f'<option value="{v}" {"selected" if v == sel else ""}>{l}</option>'
                   for v, l in MON_CATS)
    return f'<select name="cat">{opts}</select>'

def mon_lv_select(sel):
    opts = "".join(f'<option value="{v}" {"selected" if v == sel else ""}>{l}</option>'
                   for v, l in MON_LVS)
    return f'<select name="lv">{opts}</select>'

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

def item_group_select(sel):
    opts = "".join(f'<option value="{v}" {"selected" if v == sel else ""}>{l}</option>'
                   for v, l in ITEM_GROUPS)
    return f'<select name="group">{opts}</select>'

def ver_badges(ver):
    s = set(ver or [])
    out = ""
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
        mtime = max(os.path.getmtime(DATA_JSON), os.path.getmtime(REPORT_JSON))
        with cls._lock:
            if mtime != cls._t:
                with open(DATA_JSON, encoding="utf-8") as f:
                    cls.wiki = json.load(f)
                with open(REPORT_JSON, encoding="utf-8") as f:
                    cls.report = json.load(f)
                cls._t = mtime
                cls._build()
            return cls.wiki, cls.report

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

# ---------------------------------------------------------------- templates
BASE = """<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{title} · EI 传奇3.0 百科</title>
<style>
:root {{
  --bg:#121417; --panel:#1a1e24; --line:#2a303a; --fg:#e8e6e3;
  --dim:#8b919c; --acc:#d9a441; --ac2:#6fb3e0; --good:#7ec97e; --bad:#e07a7a;
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
.tag {{ display:inline-block; font-size:11px; padding:1px 7px; border-radius:9px; margin-left:6px; vertical-align:1px; }}
.tag-ei {{ background:#5a2d2d; color:#ff9d9d; }}
.tag-mei {{ background:#2d3a5a; color:#9db8ff; }}
.tag-zir {{ background:#3a2d5a; color:#d0b3ff; }}
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
<a href="/library" {library}>资源库</a>
<a href="/diff" {diff}>差异裁剪</a>
</nav></header>
<main>
{body}
</main>
<footer>EI 传奇3.0 客户端百科 · 数据源: Mud3 服务端 Envir / Zircon System.db / EI 客户端资源 · 本地服务</footer>
</body></html>
"""

def page(title, body, active=""):
    nav = {k: "" for k in ["home","maps","monsters","items","skills","npcs","quests","companions","library","diff"]}
    nav[active] = 'class="active"'
    return BASE.format(title=html.escape(title), body=body, **nav)

def esc(s):
    return html.escape(str(s), quote=False)

def mon_zh(name):
    """怪物英文/中文名 → 中文显示名。"""
    w, _ = Data.get()
    mon = Data.mon_by_zh.get(name)
    if mon:
        return mon.get("zh") or mon["name"]
    return Data.report["mon_zh"].get(name, name)

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
        if p == "/npcs": return self.npcs(u.query)
        if p == "/quests": return self.quests(u.query)
        if p == "/companions": return self.companions(u.query)
        if p == "/diff": return self.diff()
        if p == "/library": return self.library(u.query)
        if p.startswith("/thumb/"): return self.thumb(urllib.parse.unquote(p[7:]))
        if p.startswith("/img/"): return self.img(urllib.parse.unquote(p[5:]))
        if p == "/data/wiki.json": return self._send_json(Data.get()[0])
        if p == "/data/report.json": return self._send_json(Data.get()[1])
        self._send(page("404", f"<h1>404</h1><p class='lead'>路径不存在: {esc(p)}</p>"), code=404)

    # ---------------- 首页
    def home(self):
        w, r = Data.get()
        st = r["stats"]
        c = len(Data.companions)
        nm = len(Data.monsters)
        body = f"""
<h1>EI 传奇3.0 客户端 游戏百科</h1>
<p class="lead">以 EI 传奇3.0 客户端为底板（Mud3 服务端数据为权威来源），对照 Zircon / mir3ei 整理的完整资料库。</p>
<div>
  <div class="stat"><b>{st['ei_maps']}</b><span>EI 地图</span></div>
  <div class="stat"><b>{nm}</b><span>怪物种类</span></div>
  <div class="stat"><b>{len(w['items'])}</b><span>装备道具</span></div>
  <div class="stat"><b>{len(w['skills'])}</b><span>技能</span></div>
  <div class="stat"><b>{len(w['npcs'])}</b><span>NPC</span></div>
  <div class="stat"><b>{len(w['quests'])}</b><span>任务</span></div>
  <div class="stat"><b>{c}</b><span>宠物坐骑</span></div>
  <div class="stat"><b>{st['spawn_records']}</b><span>刷怪记录</span></div>
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
  <div class="panel"><h3><a href="/library">资源库</a></h3>
    EI 客户端 WIL 图库浏览（怪物 / 装备 / 地图贴图 / 图标）。</div>
  <div class="panel"><h3><a href="/diff">差异裁剪</a></h3>
    EI 客户端 vs mir3ei vs Zircon 差异对照，为裁剪 mir3ei 新内容提供依据。</div>
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
            thumb = "" if m.get("no_thumb") else ('<img class="thumb" src="/thumb/' + urllib.parse.quote(m['file']) + '" alt="' + esc(m['file']) + '">')
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
            zh = Data.mon_by_zh.get(name)
            link = f'<a href="/monster/{urllib.parse.quote(name)}">{esc(name)}</a>'
            if zh and zh["name"] != name:
                link = f'<a href="/monster/{urllib.parse.quote(name)}">{esc(zh.get("zh") or zh["name"])} <span class="mono">{esc(name)}</span></a>'
            mon_rows += f"<tr><td>{link}</td><td>{count}</td></tr>"
        # NPC/商人
        npc_rows = ""
        for me in Data.merch_by_map.get(m["file"].lower().removesuffix(".map"), []) + Data.merch_by_map.get(m["file"], []):
            npc_rows += f"<tr><td>{esc(me['name'])}</td><td>{esc(me.get('script',''))}</td><td>{me['x']},{me['y']}</td></tr>"
        if not npc_rows:
            npc_rows = '<tr><td colspan="3" class="lead">无 NPC 记录</td></tr>'
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
        cat = q.get("cat", [""])[0]
        lv = q.get("lv", [""])[0]
        rows = [m for m in Data.monsters
                if ver_matches(m.get("ver"), ver)
                and (not kw or kw in m["name"].lower() or kw in (m.get("zh") or "").lower())
                and mon_cat_ok(m, cat)
                and mon_lv_ok(m, lv)]
        rows.sort(key=lambda x: (x.get("level") or 0, x["name"].lower()))
        items = []
        for m in rows:
            disp = m.get("zh") or m["name"]
            en = f'<span class="mono">{esc(m["name"])}</span>' if disp != m["name"] else ""
            sub = f"Lv {m.get('level') or '?'}"
            tags = []
            if m.get("boss"): tags.append('<span class="bad">Boss</span>')
            if m.get("undead"): tags.append('<span class="bad bad-undead">不死</span>')
            if m.get("tame"): tags.append('<span class="bad bad-tame">可捕捉</span>')
            if tags: sub += " · " + " ".join(tags)
            sp = parse_spawns(m.get("spawns"))
            if sp:
                sub += f" · {sum(c for _, c in sp)} 只 / {len(sp)} 图"
            items.append(f"""<div class="card"><a href="/monster/{urllib.parse.quote(m['name'])}">
  {icon_img('monsters', m['id'], 'pic', disp)}
  <div><span class="name">{esc(disp)}</span>{en} {ver_badges(m.get('ver'))}</div>
  <div class="sub">{sub}</div>
</a></div>""")
        body = f"""
<h1>怪物图鉴 · {len(Data.monsters)} 种</h1>
<p class="lead">Zircon System.db 全量 {len(Data.monsters)} 种怪物（Boss {sum(1 for x in Data.monsters if x.get('boss'))} / 不死系 {sum(1 for x in Data.monsters if x.get('undead'))} / 可捕捉 {sum(1 for x in Data.monsters if x.get('tame'))}）。</p>
<form class="filters" method="get" action="/monsters">
  <input name="q" placeholder="搜索怪物名…" value="{esc(kw)}">
  {mon_cat_select(cat)}
  {mon_lv_select(lv)}
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
<p class="lead">共 {len(rows)} 种</p>
<div class="cards">{''.join(items) or '<p class="none">无匹配条目</p>'}</div>
"""
        self._send(page("怪物图鉴", body, "monsters"))

    def monster_detail(self, name):
        m = Data.mon_by_zh.get(name)
        if not m:
            self._send(page("怪物", f"<h1>未找到</h1><p class='lead'>{esc(name)}</p>"), code=404)
            return
        disp = m.get("zh") or m["name"]
        flags = ver_badges(m.get("ver"))
        attrs = " · ".join(esc(a) for a in m.get("attrs", []))
        sp = parse_spawns(m.get("spawns"))
        map_rows = ""
        for f, c in sorted(sp, key=lambda x: x[0].lower()):
            map_rows += f"<tr><td>{file_link(f + '.map')}</td><td>{c}</td></tr>"
        if not map_rows:
            map_rows = '<tr><td colspan="2" class="none">无刷怪记录</td></tr>'
        boss = '<span class="tag tag-ei">Boss</span>' if m.get("boss") else ''
        undead = '<span class="bad">亡灵</span>' if m.get("undead") else ''
        body = f"""
<a href="/monsters">← 返回图鉴</a>
<h1>{esc(disp)} <span class="mono">{esc(m['name'])}</span> {flags}</h1>
<div class="detail">
  {icon_img('monsters', m['id'], 'bigpic', disp)}
  <div class="meta">
    <dl class="kv">
      <dt>等级</dt><dd>{m.get('level') or '?'} {boss} {undead}</dd>
      <dt>属性</dt><dd>{attrs or '—'}</dd>
      <dt>刷新量</dt><dd>{sum(c for _, c in sp) if sp else '—'} 只 / {len(sp)} 图</dd>
      <dt>掉落</dt><dd>{esc(m.get('drops','—'))}</dd>
    </dl>
    <p class="lead">{esc(m.get('traits',''))}</p>
  </div>
</div>
<h2>分布地图（{len(sp)} 张）</h2>
<table><tr><th>地图</th><th>数量</th></tr>{map_rows}</table>
"""
        self._send(page(f"怪物 {disp}", body, "monsters"))

    # ---------------- 装备
    def items(self, qs):
        q = urllib.parse.parse_qs(qs)
        group = q.get("group", [""])[0]
        klass = q.get("class", [""])[0]
        kw = (q.get("q", [""])[0]).strip().lower()
        ver = q.get("ver", [""])[0]
        rows = [i for i in Data.items
                if (not group or item_group(i) == group)
                and (not klass or klass in i.get("class", ""))
                and (not kw or kw in i["name"].lower() or kw in (i.get("zh") or "").lower())
                and ver_matches(i.get("ver"), ver)]
        rows.sort(key=lambda i: ((i.get("type_zh") or ""), i["name"]))
        klass_opts = "".join(f'<option value="{c}" {"selected" if c==klass else ""}>{c}</option>'
                             for c in ["战士","法师","道士","战法道","战道","战法","法道","全职业"])
        items = []
        for i in rows:
            en = f'<span class="mono">{esc(i["name"])}</span>' if i.get("zh") and i["zh"] != i["name"] else ""
            sub = esc(i.get("type_zh") or i['category']) + " · " + esc(i.get('class',''))
            items.append(f"""<div class="card"><a href="/item/{i['id']}">
  {icon_img('items', i['id'], 'pic', i.get('zh') or i['name'])}
  <div><span class="name">{esc(i.get('zh') or i['name'])}</span>{en} {ver_badges(i.get('ver'))}</div>
  <div class="sub">{sub}</div>
</a></div>""")
        body = f"""
<h1>装备 · {len(Data.items)} 件</h1>
<p class="lead">Zircon System.db 全量 {len(Data.items)} 件（武器 / 护甲 / 首饰 / 药水 / 材料等 {len([g for g in ITEM_GROUPS if g[0]])} 类）。</p>
<form class="filters" method="get" action="/items">
  <input name="q" placeholder="搜索装备名…" value="{esc(kw)}">
  {item_group_select(group)}
  <select name="class"><option value="">全部职业</option>{klass_opts}</select>
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
<p class="lead">共 {len(rows)} 件</p>
<div class="cards">{''.join(items) or '<p class="none">无匹配条目</p>'}</div>
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
        attrs = " · ".join(esc(a) for a in i.get("attrs", []))
        body = f"""
<a href="/items">← 返回装备列表</a>
<h1>{esc(i.get('zh') or i['name'])} <span class="mono">{esc(i['name'])}</span> {ver_badges(i.get('ver'))}</h1>
<div class="detail">{icon_img('items', i['id'], 'bigpic', i.get('zh') or i['name'])}<div class="meta">
<dl class="kv">
  <dt>分类</dt><dd>{esc(i.get('type_zh') or i['category'])}</dd>
  <dt>职业</dt><dd>{esc(i.get('class',''))}</dd>
  <dt>属性</dt><dd>{attrs or '—'}</dd>
  <dt>其他</dt><dd>{esc(i.get('meta',''))}</dd>
  <dt>掉落</dt><dd>{esc(i.get('drops','—'))}</dd>
  <dt>套装</dt><dd>{esc(i.get('set','—'))}</dd>
  <dt>说明</dt><dd>{esc(i.get('desc','—'))}</dd>
</dl></div></div>
"""
        self._send(page(f"装备 {i.get('zh') or i['name']}", body, "items"))

    # ---------------- 技能
    def skills(self, qs):
        q = urllib.parse.parse_qs(qs)
        ver = q.get("ver", [""])[0]
        by = {}
        for s in Data.skills:
            if not ver_matches(s.get("ver"), ver):
                continue
            by.setdefault(s.get("klass") or "通用", []).append(s)
        secs = ""
        for k in Data.skill_classes + ["通用"]:
            arr = by.get(k)
            if not arr: continue
            rows = ""
            for s in arr:
                rows += (f"<tr><td>{icon_img('skills', s['id'], 'icon', s.get('zh') or s['name'])} "
                         f"<a href='#'>{esc(s.get('zh') or s['name'])}</a> "
                         f"<span class='mono'>{esc(s['name'])}</span> {ver_badges(s.get('ver'))}</td>"
                         f"<td>{esc(s.get('type',''))} {esc(s.get('school',''))}</td>"
                         f"<td>{esc(s.get('power',''))}</td><td>{esc(s.get('cost',''))}</td>"
                         f"<td>{esc(s.get('levels',''))}</td><td>{esc(s.get('desc',''))[:60]}</td></tr>")
            secs += f"<h2>{esc(k)}（{len(arr)}）</h2><table><tr><th>技能</th><th>类型</th><th>威力</th><th>耗蓝</th><th>等级门槛</th><th>说明</th></tr>{rows}</table>"
        body = f"""<h1>技能 · {len(Data.skills)} 个</h1>
<form class="filters" method="get" action="/skills">
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
{secs or '<p class="none">无匹配条目</p>'}"""
        self._send(page("技能", body, "skills"))

    # ---------------- NPC
    def npcs(self, qs):
        q = urllib.parse.parse_qs(qs)
        ver = q.get("ver", [""])[0]
        rows = ""
        for n in sorted(Data.npcs, key=lambda x: x["name"]):
            if not ver_matches(n.get("ver"), ver):
                continue
            rows += (f"<tr><td>{icon_img('npcs', n['id'], 'icon', n.get('zh') or n['name'])} "
                     f"{esc(n.get('zh') or n['name'])} <span class='mono'>{esc(n['name'])}</span> {ver_badges(n.get('ver'))}</td>"
                     f"<td>{file_link(n['map'] + '.map') if n.get('map') else '—'}</td>"
                     f"<td>{esc(n.get('desc',''))}</td>"
                     f"<td>{n.get('quests_in',0)} / {n.get('quests_out',0)}</td></tr>")
        body = f"""<h1>NPC · {len(Data.npcs)} 位</h1>
<p class="lead">地图为 Zircon 视图编号；Mud3 商人见地图详情页。</p>
<form class="filters" method="get" action="/npcs">
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
<table><tr><th>NPC</th><th>地图</th><th>介绍</th><th>可接/可交任务</th></tr>{rows or '<tr><td colspan="4" class="none">无匹配条目</td></tr>'}</table>"""
        self._send(page("NPC", body, "npcs"))

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
            cards += f"""<div class="card"><a href="/companions">
  {icon_img('companion', c['id'], 'pic', c['name'])}
  <div><span class="name">{esc(c['name'])}</span> {ver_badges(c.get('ver'))}</div>
  <div class="sub">宠物 #{c['monster_id']} · 对应怪物 {mlink}</div>
  <div class="sub">价格 {c.get('price') or '—'} 金币 · {avail}</div>
</a></div>"""
        body = f"""<h1>宠物与坐骑 · {len(Data.companions)} 种</h1>
<p class="lead">来自 CompanionInfo 表。对应怪物条目见怪物图鉴。</p>
<form class="filters" method="get" action="/companions">
  {ver_select(ver)}
  <button type="submit">筛选</button>
</form>
<div class="cards">{cards or '<p class="none">无匹配条目</p>'}</div>"""
        self._send(page("宠物与坐骑", body, "companions"))

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
            mon_more = f"<p class='lead'>…另有 {mo['mud3_only_count'] - 40} 种。</p>"

        # 装备表
        it_rows = ""
        for n in it["mud3_only"][:60]:
            it_rows += f"<tr><td>{esc(n)}</td></tr>"
        it_more = f"<p class='lead'>…另有 {max(0, it['mud3_only_count'] - 60)} 件，共 {it['mud3_only_count']} 件。</p>" if it["mud3_only_count"] > 60 else ""

        # 刺客技能
        as_rows = ""
        for n in sk["assassin_skills"][:40]:
            as_rows += f"<tr><td>{esc(n)}</td></tr>"

        body = f"""
<h1>差异裁剪 · 三版本比对</h1>
<p class="lead">以 MUD3 服务端（最早）为基线，对照 mir3ei（中间）与 Zircon（20 年后实现）。
数据源: MUD3 Envir/Mapinfo.txt + Mon_Def + MonItems · EI 客户端 544 图 · mir3ei 566 图 · Zircon System.db。
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
"""
        self._send(page("差异裁剪", body, "diff"))

    # ---------------- 条目图片（/img/<board>/<id>.png, 磁盘缓存）
    def img(self, path):
        parts = path.split("/")
        if len(parts) != 2 or parts[0] not in IMG_BOARDS:
            self._send(page("404", f"<p class='lead'>图片不存在: {esc(path)}</p>"), code=404)
            return
        board, fname = parts
        iid = fname.removesuffix(".png")
        if not iid.isdigit():
            self._send(page("404", f"<p class='lead'>图片不存在: {esc(path)}</p>"), code=404)
            return
        png = os.path.join(IMGS_DIR, board, f"{int(iid)}.png")
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
