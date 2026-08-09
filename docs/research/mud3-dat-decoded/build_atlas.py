#!/usr/bin/env python3
"""把解码后的老版 DAT 记录接入 legacy-atlas 内容百科，制作逐条版本对照表。

- 读取 decode.py 产出的 stditem.json / magic.json / monster.json（纯解码数据）
- 读取 build_comparison.py 的映射表（SKILL_MAP / ITEM_MAP / MONSTER_MAP）
- 给每条记录打标签：old-only / current-only / both / changed / unverified（写回 JSON）
- 生成可复用「老版对照」section 片段（tagbar 筛选 + 表格），注入：
  * catalog-mud3.html       —— 汇总页：技能105 + Zircon新增113 + 物品1143 + 怪物432
  * catalog-skills.html     —— 替换过时的「老版基线」占位为完整技能对照表
  * catalog-items.html      —— 判定方式之后插入「老版物品」对照表
  * catalog-world.html      —— 当前怪物之前插入「老版怪物」对照表
  * catalog.html            —— 更新「阅读规则」（解码已完成，不再是"下一轮"）

标签规则（诚实原则：未做全量对照的一律 unverified，不假装全映射）：
  magic   : 映射且 等级/耗蓝/威力 全等 → both；任一不等 → changed
            装备技能(MagicSchool=99) 或老版特有 → old-only
  stditem : 锚点映射且价格全等 → both；价格不等 → changed；锚点无 Zircon 侧 → old-only
            未映射 → unverified（老版 1143 条仅锚点级对照，未做全量名映射）
  monster : 名字以数字结尾（变体后缀 0/61/62/9/96+）→ old-only（先于映射判定）
            锚点映射 → changed；锚点无 Zircon 侧 → old-only；未映射 → unverified
  Zircon 侧技能：不在 SKILL_MAP 值域 → current-only（magic.dat 全 105 条已确认，无对应即无）
"""
from __future__ import annotations

import json
import re
from pathlib import Path

import build_comparison as bc

D = Path(__file__).resolve().parent
ATLAS = Path("/home/tetsuya/development/Zircon/docs/legacy-atlas")
CONTENT = ATLAS / "content"

magic = bc.magic
stditem = bc.stditem
monster = bc.monster
skills_z = bc.skills_z
items_z = bc.items_z
monsters_z = bc.monsters_z

TAG_BUTTONS = ["old-only", "current-only", "both", "changed", "unverified"]

# ---------------------------------------------------------------- tags

def skill_equal(lr, z) -> bool:
    """等级门槛 + B1耗蓝 + 威力增量 三锚点全等才算 both。"""
    try:
        zneed = tuple(int(x) for x in z["need"].replace(" ", "").replace("级", "").split("/")[:3])
    except Exception:
        zneed = None
    need_ok = zneed == (lr["NeedLevel1"], lr["NeedLevel2"], lr["NeedLevel3"])
    try:
        zmp = int(z["mp"])
    except Exception:
        zmp = None
    mp_ok = zmp == lr["TrioB1"]
    zpw = z.get("power_lvl", "")
    m = re.match(r"\+(\d+)-(\d+)", zpw)
    pw_ok = m is not None and int(m.group(1)) == lr["TrioA2"] and int(m.group(2)) == lr["TrioA3"]
    return need_ok and mp_ok and pw_ok


def tag_skills() -> dict:
    tags = {}
    for r in magic["records"]:
        name = r["Name"]
        if name in bc.SKILL_MAP:
            zid = bc.SKILL_MAP[name][0]
            if skill_equal(r, skills_z[zid]):
                tags[("magic", r["Index"])] = ("both", f"→ {skills_z[zid]['name']} (id={zid})")
            else:
                tags[("magic", r["Index"])] = ("changed", f"→ {skills_z[zid]['name']} (id={zid})")
        elif r["MagicSchool"] == 99:
            tags[("magic", r["Index"])] = ("old-only", "装备技能体系，Zircon 无 1:1 对应")
        else:
            tags[("magic", r["Index"])] = ("old-only", "老版特有技能，Zircon 无对应")
    return tags


def tag_items() -> dict:
    tags = {}
    by_name = {n: (zid, note) for n, zid, note in bc.ITEM_MAP}
    for r in stditem["records"]:
        hit = by_name.get(r["Name"])
        if hit is None:
            tags[("item", r["Index"])] = ("unverified", "未做全量名映射，有无 Zircon 对应待查")
            continue
        zid, note = hit
        if zid is None:
            tags[("item", r["Index"])] = ("old-only", note)
            continue
        z = items_z[zid]
        if z.get("price") == r["Price"]:
            tags[("item", r["Index"])] = ("both", f"→ {z['name']} (id={zid})")
        else:
            tags[("item", r["Index"])] = ("changed", f"→ {z['name']} (id={zid})")
    return tags


def tag_monsters() -> dict:
    tags = {}
    by_prefix = {}
    for n, zid, note in bc.MONSTER_MAP:
        by_prefix.setdefault(n, []).append((zid, note))
    for r in monster["records"]:
        name = r["Name"]
        if name and name[-1].isdigit():
            tags[("mon", r["Index"])] = ("old-only", "老版变体（数值微调/活动/强化版），Zircon 无对应")
            continue
        hit = None
        for p, lst in by_prefix.items():
            if name.startswith(p):
                hit = lst[0]
                break
        if hit:
            zid, note = hit
            if zid is None:
                tags[("mon", r["Index"])] = ("old-only", note)
            else:
                tags[("mon", r["Index"])] = ("changed", f"→ {monsters_z[zid]['name']} (id={zid})")
        else:
            tags[("mon", r["Index"])] = ("unverified", "未做全量名映射，有无 Zircon 对应待查")
    return tags


def write_tags_into_json():
    t_skill = tag_skills()
    t_item = tag_items()
    t_mon = tag_monsters()
    for fname, key, tags in (("magic.json", "magic", t_skill),
                             ("stditem.json", "item", t_item),
                             ("monster.json", "mon", t_mon)):
        data = json.loads((D / fname).read_text(encoding="utf-8"))
        n = 0
        for r in data["records"]:
            tag, note = tags[(key, r["Index"])]
            r["tag"] = tag
            r["tag_note"] = note
            n += 1
        (D / fname).write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
    return t_skill, t_item, t_mon


# ---------------------------------------------------------------- rendering

def esc(s):
    return (str(s).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
            .replace('"', "&quot;"))


def tagbar_html(prefix):
    buttons = "".join(
        [f'<button data-ftag="all" class="active">全部</button>'] +
        [f'<button data-ftag="{t}">{t}</button>' for t in TAG_BUTTONS])
    return (f'<div class="tagbar" id="{prefix}Bar">{buttons}</div>')


def tagbar_js(prefix):
    return ("<script>(function(){var bar=document.getElementById('" + prefix +
            "Bar'),input=document.getElementById('" + prefix +
            "Filter');if(!bar||!input)return;bar.querySelectorAll('button').forEach(function(b){b.addEventListener('click',function(){bar.querySelectorAll('button').forEach(function(x){x.classList.remove('active')});b.classList.add('active');var t=b.dataset.ftag,rows=bar.closest('section').querySelectorAll('tbody tr');rows.forEach(function(r){r.hidden=t!=='all'&&r.dataset.tags!==t})})});input.addEventListener('input',function(){var q=input.value.toLowerCase();bar.closest('section').querySelectorAll('tbody tr').forEach(function(r){r.hidden=q&&!r.textContent.toLowerCase().includes(q)})})})();</script>")


def make_section(h2, note, head, rows, prefix):
    trs = "\n".join(rows)
    head_html = "".join(f"<th>{h}</th>" for h in head)
    return (f'<section class="catalog-section"><h2>{h2}</h2>'
            f'<p class="catalog-note">{note}</p>'
            f'<label class="catalog-search">筛选：<input id="{prefix}Filter" type="search" placeholder="输入名称、编号、标记……"></label>'
            f'{tagbar_html(prefix)}'
            f'<div class="table-wrap"><table class="catalog-table"><thead><tr>{head_html}</tr></thead>'
            f'<tbody>{trs}</tbody></table></div></section>')


def build_sections(t_skill, t_item, t_mon):
    """返回 {name: (section_html, js)} 四段。"""
    rows_skill = []
    for r in magic["records"]:
        tag, note = t_skill[("magic", r["Index"])]
        zcell = note.replace("→ ", "")
        rows_skill.append(
            f'<tr data-tags="{tag}"><td>{r["Index"]}</td><td>{esc(r["Name"])}</td>'
            f'<td>{r["NeedLevel1"]}/{r["NeedLevel2"]}/{r["NeedLevel3"]}</td>'
            f'<td>{r["TrainExp1"]}/{r["TrainExp2"]}/{r["TrainExp3"]}</td>'
            f'<td>{r["TrioB1"]}</td><td>({r["TrioA1"]},{r["TrioA2"]},{r["TrioA3"]})</td>'
            f'<td>{esc(zcell)}</td><td><span class="tag {tag}">{tag}</span> <small>{esc(note)}</small></td></tr>')

    rows_zcur = []
    mapped_ids = {v[0] for v in bc.SKILL_MAP.values()}
    for zid in sorted(skills_z):
        if zid in mapped_ids:
            continue
        z = skills_z[zid]
        rows_zcur.append(
            f'<tr data-tags="current-only"><td>{zid}</td><td>{esc(z["name"])}</td>'
            f'<td>{esc(z["school"])}</td><td>{esc(z["need"])}</td></tr>')

    rows_item = []
    for r in stditem["records"]:
        tag, note = t_item[("item", r["Index"])]
        rows_item.append(
            f'<tr data-tags="{tag}"><td>{r["Index"]}</td><td>{esc(r["Name"])}</td>'
            f'<td>{r["StdMode"]}</td><td>{r["Price"]}</td><td>{esc(r["NeedLevel"])}</td>'
            f'<td><span class="tag {tag}">{tag}</span> <small>{esc(note)}</small></td></tr>')

    rows_mon = []
    for r in monster["records"]:
        if "Level" not in r:
            continue
        tag, note = t_mon[("mon", r["Index"])]
        rows_mon.append(
            f'<tr data-tags="{tag}"><td>{r["Index"]}</td><td>{esc(r["Name"])}</td>'
            f'<td>{r["Level"]}</td><td>{r["HP"]}</td><td>{r["DCMin"]}-{r["DCMax"]}</td>'
            f'<td>{r["Exp"]}</td><td><span class="tag {tag}">{tag}</span> <small>{esc(note)}</small></td></tr>')

    nmon = len(rows_mon)
    return {
        "skill": (make_section("✨ 老版技能 · magic.dat（105 条）",
                               "magic.dat 全 105 条逐条对照：等级门槛/耗蓝/威力三锚点对齐 Zircon；带数字后缀的为老版特有或装备技能。",
                               ("#", "老版名称", "等级门槛", "修炼经验", "耗蓝B1", "威力TrioA", "Zircon 对照", "标记"),
                               rows_skill, "skillLegacy"), tagbar_js("skillLegacy")),
        "zcur": (make_section("➕ Zircon 新增技能（113 条）",
                              "magic.dat 全 105 条已确认，无对应即不存在于老版：刺客系、后期强化与 Augmentation 技能。",
                              ("ID", "名称", "派系", "等级门槛"), rows_zcur, "zcurLegacy"),
                 tagbar_js("zcurLegacy")),
        "item": (make_section("⚔️ 老版物品 · stditem.dat（1143 条）",
                              "锚点级对照（价格/属性全等确认）；未映射条目标 unverified，不做名称猜测。",
                              ("#", "老版名称", "StdMode", "价格", "NeedLevel", "标记"),
                              rows_item, "itemLegacy"), tagbar_js("itemLegacy")),
        "mon": (make_section(f"🐉 老版怪物 · monster.dat（433 条，含头部占位记录 0）",
                             "Boss 锚点对照；名字以数字结尾的为老版变体（0=微调 / 61,62=一击死 / 9=99级强化 / 96+ =多属性）。",
                             ("#", "老版名称", "等级", "HP", "物攻", "经验", "标记"),
                             rows_mon, "monLegacy"), tagbar_js("monLegacy")),
    }


# ---------------------------------------------------------------- page builds

def build_catalog_mud3(sections):
    hero = ('<nav class="breadcrumb"><a href="index.html">内容百科</a> / 老版 DAT 解码逐条目录</nav>'
            '<header class="page-hero"><p class="eyebrow">CONTENT CATALOG · 逐条版本对照</p>'
            '<h1>🗃️ 老版 DAT 解码逐条目录</h1>'
            '<p>20 年前 Mud3 / EI 2.0 服务端数据文件（stditem.dat / magic.dat / monster.dat）逐条解码结果，与当前 Zircon 逐条对照。解码器、字段偏移/类型/来源与完整 JSON 见 <a href="../../research/mud3-dat-decoded/">docs/research/mud3-dat-decoded/</a>；分析结论见 <a href="../../research/mud3-dat-decoded/comparison.md">comparison.md</a>。</p>'
            '<p class="catalog-note">标记定义：<b>old-only</b>=仅老版存在 · <b>current-only</b>=仅当前 Zircon 存在 · <b>both</b>=两端存在且数值一致 · <b>changed</b>=两端存在但数值有差异 · <b>unverified</b>=未确认（未做全量名映射，不冒充完整数据）。老版字段全部来自原始 DAT 解码，未用 Zircon 数据反推。</p></header>')
    parts = [hero]
    for key in ("skill", "zcur", "item", "mon"):
        sec, js = sections[key]
        parts.append(sec + js)
    html = ("<!doctype html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\">"
            "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
            "<title>老版 DAT 解码逐条目录 · Legacy Atlas</title>"
            "<link rel=\"stylesheet\" href=\"../style.css\"></head><body>"
            "<main class=\"site-shell\">" + "\n".join(parts) + "</main></body></html>")
    (CONTENT / "catalog-mud3.html").write_text(html, encoding="utf-8")
    print("catalog-mud3.html: 汇总页已生成")


def inject_skill_into_skills_page(sections):
    p = CONTENT / "catalog-skills.html"
    h = p.read_text(encoding="utf-8")
    sec, js = sections["skill"]
    # 替换旧「老版基线」section（含我之前的手写说明）
    pat = re.compile(r'<section class="catalog-section"><h2>老版基线：EI 2\.0 / Mud3 已确认范围</h2>.*?</section>', re.S)
    intro = ('<section class="catalog-section"><h2>老版基线：EI 2.0 / Mud3 已确认范围</h2>'
             '<p>早期版本的核心是战士、法师、道士三职业。刺客及后期强化技能不应倒灌到“20年前基线”。老版 magic.dat 已按原始 DAT 逐字段解码并验证（解码器、偏移/类型/来源与证据见 <a href="../../research/mud3-dat-decoded/">docs/research/mud3-dat-decoded/</a>），完整 105 条老版技能逐条对照已展开如下，标记 old-only/both/changed/unverified；汇总对照见 <a href="catalog-mud3.html">老版 DAT 解码逐条表</a>。</p></section>')
    if pat.search(h):
        h = pat.sub(intro, h, count=1)
    else:
        # 兜底：插在「当前 Zircon」之前
        h = h.replace('<section class="catalog-section"><h2>当前 Zircon：174 条技能记录</h2>',
                      intro + '<section class="catalog-section"><h2>当前 Zircon：174 条技能记录</h2>')
    # 老版技能表插到「当前 Zircon」section 之前
    anchor = '<section class="catalog-section"><h2>当前 Zircon：174 条技能记录</h2>'
    if sec.split("magic.dat")[1][:3] and ('老版技能 · magic.dat' not in h):
        h = h.replace(anchor, sec + js + anchor)
    p.write_text(h, encoding="utf-8")
    print("catalog-skills.html: 老版技能对照表已注入")


def inject_item_into_items_page(sections):
    p = CONTENT / "catalog-items.html"
    h = p.read_text(encoding="utf-8")
    sec, js = sections["item"]
    if '老版物品 · stditem.dat' in h:
        print("catalog-items.html: 已存在，跳过")
        return
    anchor = '<section class="catalog-section"><h2>老版基线的判定方式</h2>'
    i = h.find(anchor)
    j = h.find("</section>", i) + len("</section>")
    h = h[:j] + sec + js + h[j:]
    p.write_text(h, encoding="utf-8")
    print("catalog-items.html: 老版物品对照表已注入")


def inject_mon_into_world_page(sections):
    p = CONTENT / "catalog-world.html"
    h = p.read_text(encoding="utf-8")
    sec, js = sections["mon"]
    if '老版怪物 · monster.dat' in h:
        print("catalog-world.html: 已存在，跳过")
        return
    anchor = '<section class="catalog-section"><h2>当前怪物：309 条</h2>'
    h = h.replace(anchor, sec + js + anchor, 1)
    p.write_text(h, encoding="utf-8")
    print("catalog-world.html: 老版怪物对照表已注入")


def update_catalog_rules():
    p = CONTENT / "catalog.html"
    h = p.read_text(encoding="utf-8")
    old = "当前版本的完整明细先落地，下一轮再把 stditem.dat / magic.dat 解码结果补到同一套表格中，届时每条记录会有“20年前 / 20年后 / 新增 / 删除 / 改名 / 待确认”状态。"
    new = ("老版 stditem.dat / magic.dat / monster.dat 解码结果已补入各分类页（技能/物品/怪物均有对照表），"
           "每条记录带 old-only / current-only / both / changed / unverified 状态；汇总页见 <a href=\"catalog-mud3.html\">老版 DAT 解码逐条表</a>。")
    if old in h:
        h = h.replace(old, new)
        p.write_text(h, encoding="utf-8")
        print("catalog.html: 阅读规则已更新")
    else:
        print("catalog.html: 阅读规则已是最新，跳过")


# ---------------------------------------------------------------- main

def main():
    t_skill, t_item, t_mon = write_tags_into_json()
    sections = build_sections(t_skill, t_item, t_mon)
    build_catalog_mud3(sections)
    inject_skill_into_skills_page(sections)
    inject_item_into_items_page(sections)
    inject_mon_into_world_page(sections)
    update_catalog_rules()
    from collections import Counter
    c = Counter()
    for r in magic["records"] + stditem["records"] + monster["records"]:
        c[r["tag"]] += 1
    print("标签汇总:", dict(c))


if __name__ == "__main__":
    main()
