#!/usr/bin/env python3
"""Build the content-first Legacy Atlas catalog pages.

The source Markdown is generated from System.db and is deliberately kept as
the source of truth.  This script only turns it into browsable HTML; it does
not invent missing legacy records.
"""
from html import escape
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "docs/legacy-atlas/content"
DB = ROOT / "docs/database/views"
OLD_MAP = Path("/home/tetsuya/NAS/TMP/Mud3/Envir/Mapinfo.txt")

CSS = "../style.css"

def inline(text: str) -> str:
    text = escape(text.strip())
    text = re.sub(r"&lt;(https?://[^&]+)&gt;", r"&lt;\1&gt;", text)
    text = re.sub(r"\[([^]]+)\]\(([^)]+)\)", r'<a href="\2">\1</a>', text)
    text = re.sub(r"`([^`]+)`", r"<code>\1</code>", text)
    return text

def markdown_body(path: Path) -> str:
    lines = path.read_text(encoding="utf-8").splitlines()
    out, i = [], 0
    while i < len(lines):
        line = lines[i]
        if not line.strip():
            i += 1
            continue
        m = re.match(r"^(#{1,4})\s+(.*)$", line)
        if m:
            level = len(m.group(1))
            out.append(f"<h{level + 1}>{inline(m.group(2))}</h{level + 1}>")
            i += 1
            continue
        if line.startswith(">"):
            quote = []
            while i < len(lines) and lines[i].startswith(">"):
                quote.append(lines[i].lstrip("> "))
                i += 1
            out.append('<p class="catalog-note">' + inline(" ".join(quote)) + "</p>")
            continue
        if line.startswith("|") and i + 1 < len(lines) and "---" in lines[i + 1]:
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                cells = [c.strip() for c in lines[i].strip().strip("|").split("|")]
                if not all(re.fullmatch(r":?-+:?", c) for c in cells):
                    rows.append(cells)
                i += 1
            if rows:
                head, body = rows[0], rows[1:]
                out.append('<div class="table-wrap"><table class="catalog-table"><thead><tr>' + ''.join(f"<th>{inline(c)}</th>" for c in head) + '</tr></thead><tbody>')
                for row in body:
                    out.append('<tr>' + ''.join(f"<td>{inline(c)}</td>" for c in row) + '</tr>')
                out.append('</tbody></table></div>')
            continue
        if re.match(r"^[-*]\s+", line):
            items = []
            while i < len(lines) and re.match(r"^[-*]\s+", lines[i]):
                items.append(re.sub(r"^[-*]\s+", "", lines[i]))
                i += 1
            out.append('<ul>' + ''.join(f"<li>{inline(x)}</li>" for x in items) + '</ul>')
            continue
        para = [line]
        i += 1
        while i < len(lines) and lines[i].strip() and not re.match(r"^(#|>|\||[-*]\s)", lines[i]):
            para.append(lines[i]); i += 1
        out.append('<p>' + inline(" ".join(para)) + '</p>')
    return "\n".join(out)

def old_map_rows() -> list[tuple[str, str, str]]:
    if not OLD_MAP.exists():
        return []
    text = OLD_MAP.read_bytes().decode("gb18030", errors="replace")
    rows = []
    for line in text.splitlines():
        if line.startswith(";;") or not line.startswith("["):
            continue
        m = re.match(r"^\[([^\s\]]+)\s+(.+?)\s+(-?\d+)\](.*)$", line)
        if m:
            rows.append((m.group(1), m.group(2).strip(), (m.group(4) or "").strip()))
    return rows

def page(title: str, subtitle: str, body: str, search=False) -> str:
    search_box = '<label class="catalog-search">筛选当前目录：<input id="catalogFilter" type="search" placeholder="输入名称、编号、职业、属性……"></label>' if search else ""
    script = '''<script>const f=document.getElementById('catalogFilter');if(f){f.addEventListener('input',()=>{const q=f.value.toLowerCase();document.querySelectorAll('.catalog-table tbody tr, .catalog-section').forEach(e=>{e.hidden=q&&!e.textContent.toLowerCase().includes(q)})})}</script>'''
    return f'''<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{escape(title)} · Legacy Atlas</title><link rel="stylesheet" href="{CSS}"></head><body><main class="site-shell"><nav class="breadcrumb"><a href="index.html">内容百科</a> / {escape(title)}</nav><header class="page-hero"><p class="eyebrow">CONTENT CATALOG · 内容逐条目录</p><h1>{escape(title)}</h1><p>{escape(subtitle)}</p>{search_box}</header>{body}</main>{script}</body></html>'''

def write(name: str, title: str, subtitle: str, body: str, search=False):
    (OUT / name).write_text(page(title, subtitle, body, search), encoding="utf-8")

def build():
    OUT.mkdir(parents=True, exist_ok=True)
    write("catalog-skills.html", "技能逐条目录", "老版基线先列已确认的三职业范围；右侧完整展开当前 Zircon 的 174 条技能记录。", '<section class="catalog-section"><h2>老版基线：EI 2.0 / Mud3 已确认范围</h2><p>早期版本的核心是战士、法师、道士三职业。刺客及后期强化技能不应倒灌到“20年前基线”。老版 magic.dat 的逐字段解码仍需独立验证，因此本处不把当前技能名反推成老版记录。</p><p>已确认的早期主干包括：战士的基本剑术、攻杀剑术、刺杀剑术、半月弯刀、野蛮冲撞、烈火剑法；法师的火球术、雷电术、冰咆哮、地狱火、魔法盾、诱惑之光；道士的治愈术、精神力战法、施毒术、灵魂火符、隐身术、集体隐身术、召唤骷髅、神兽、群体治愈术、复活术。</p></section><section class="catalog-section"><h2>当前 Zircon：174 条技能记录</h2>' + markdown_body(DB / "skills.md") + '</section>', True)

    item_parts = []
    for p in [DB / "items" / "weapons.md", DB / "items" / "armour.md", DB / "items" / "jewellery.md", DB / "items" / "consumables.md", DB / "items" / "materials.md"]:
        item_parts.append(f'<section class="catalog-section"><h2>{escape(p.stem)}</h2>{markdown_body(p)}</section>')
    write("catalog-items.html", "装备与物品逐条目录", "当前数据库按武器、防具、首饰、消耗品、材料/其他展开；每件记录保留编号、属性、价格、耐久、掉落与套装信息。", '<section class="catalog-section"><h2>老版基线的判定方式</h2><p>老版装备必须以 Mud3 的 stditem.dat、magic.dat 及配套文本/图片为准；当前数据库中的 Dragon Lord、Sama、Odyn、Dragon Abyss 等后期套装不能直接归入20年前。</p></section>' + ''.join(item_parts), True)

    old = old_map_rows()
    old_html = '<div class="table-wrap"><table class="catalog-table"><thead><tr><th>地图文件</th><th>区域名称（原文）</th><th>规则标记</th></tr></thead><tbody>' + ''.join(f'<tr><td>{escape(a)}</td><td>{escape(b)}</td><td>{escape(c)}</td></tr>' for a,b,c in old) + '</tbody></table></div>'
    write("catalog-maps.html", "地图逐条目录", f"Mud3 Mapinfo.txt 读取到 {len(old)} 条未注释地图记录；后半部分为当前数据库的 244 张地图。", '<section class="catalog-section"><h2>20年前：Mud3 / Mapinfo.txt 原始地图表</h2><p>以下名称直接来自文件，保留“比奇县、银杏山谷、毒蛇山谷、沙巴克、祖玛、沃玛、矿区、赤月、神舰、蚂蚁洞、真天宫/黑度宫”等区域及其子地图，不把客户端显示名翻译成另一套名称。</p>' + old_html + '</section><section class="catalog-section"><h2>20年后：当前 Zircon 地图表</h2>' + markdown_body(DB / "maps.md") + '</section>', True)

    write("catalog-world.html", "怪物、NPC与任务逐条目录", "把地图之外的实体内容也展开，便于逐条判断哪些属于旧世界、哪些是后期扩展。", '<section class="catalog-section"><h2>当前怪物：309 条</h2>' + markdown_body(DB / "monsters.md") + '</section><section class="catalog-section"><h2>当前 NPC：125 条</h2>' + markdown_body(DB / "npcs.md") + '</section><section class="catalog-section"><h2>当前任务：34 条</h2>' + markdown_body(DB / "quests.md") + '</section>', True)

    hub = '''<section class="content-grid"><a class="content-card" href="catalog-maps.html"><span>01</span><h2>地图逐条表</h2><p>Mud3 原始 Mapinfo.txt 记录 + 当前 244 张地图。</p></a><a class="content-card" href="catalog-skills.html"><span>02</span><h2>技能逐条表</h2><p>老版三职业基线 + 当前 174 条技能记录。</p></a><a class="content-card" href="catalog-items.html"><span>03</span><h2>装备与物品</h2><p>当前 1078 件物品按类别完整展开。</p></a><a class="content-card" href="catalog-world.html"><span>04</span><h2>怪物、NPC、任务</h2><p>逐条查看当前世界内容，辅助做增删判断。</p></a></section><section class="catalog-section"><h2>阅读规则</h2><p>这部分是资料整理站，不是把“当前存在”直接等同于“20年后新增”。每条老版记录必须能回指 Mud3 原始文件、EI 2.0 资料或已存档图片；无法从老文件可靠读出的项目会明确标记为待解码。</p><p>当前版本的完整明细先落地，下一轮再把 stditem.dat / magic.dat 解码结果补到同一套表格中，届时每条记录会有“20年前 / 20年后 / 新增 / 删除 / 改名 / 待确认”状态。</p></section>'''
    write("catalog.html", "详细内容资料库", "按用户真正关心的内容差异组织：地图、技能、装备、物品、怪物、NPC、任务。", hub)

if __name__ == "__main__":
    build()
    print("built detailed content catalog")
