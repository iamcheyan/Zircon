#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""EI 传奇3.0 客户端地图报告生成器。

读取 /tmp/report_full.json(由 NAS/TMP 下的数据聚合而成),生成单文件
EI_MAP_REPORT.html: 544 张地图全列表 + 每图怪物/NPC + 怪物图鉴 + 差异高亮。
"""
import json, os, sys

def main():
    data = json.load(open("/tmp/report_full.json", encoding="utf-8"))
    report = data["report"]
    mon_summary = data["mon_summary"]
    mon_zh = data["mon_zh"]
    map_zh = data["map_zh"]
    npc_zh = data["npc_zh"]
    ei_only = data["ei_only"]
    mei_only = data["mei_only"]
    stats = data["stats"]

    # ---- 预计算 ----
    # 地图 -> 中文名 (服务端中文优先, 术语表次之)
    srv_name_by_file = {r["file"]: r["srv_name"] for r in report}
    map_desc = {}
    for r in report:
        f = r["file"]
        # 术语表 map_zh 键是英文描述或代码名; 尝试代码名
        code = f[:-4]
        desc = map_zh.get(code, "")
        map_desc[f] = desc

    # 怪物 -> 中文名(术语表) 或原样
    def mon_zh_name(m):
        return mon_zh.get(m, m)

    # ---- 统计 ----
    n_spawn_maps = sum(1 for r in report if r["spawns"])
    n_merch_maps = sum(1 for r in report if r["merchants"])
    n_guard_maps = sum(1 for r in report if r["guards"])
    n_merch = sum(len(r["merchants"]) for r in report)
    n_guard = sum(len(r["guards"]) for r in report)

    # ---- HTML ----
    rows = []
    for r in sorted(report, key=lambda x: x["file"].lower()):
        flag = ""
        if r["ei_only"]:
            flag = '<span class="tag tag-ei">EI 独有</span>'
        elif r["mei_only"]:
            flag = '<span class="tag tag-mei">mir3ei 新增</span>'
        name = r["srv_name"] or r["file"]
        desc = map_desc[r["file"]]
        dims = f'{r["w"]}×{r["h"]}'
        mobs = "、".join(
            f'<span class="mon" title="{mon_zh_name(m)}">{m}</span>×{c}'
            for m, c in r["spawns"][:12]
        ) + ("…" if len(r["spawns"]) > 12 else "")
        merchs = "、".join(r["merchants"][:8]) + ("…" if len(r["merchants"]) > 8 else "")
        guards = "、".join(r["guards"][:5]) + ("…" if len(r["guards"]) > 5 else "")
        rows.append(f"""<tr data-f="{r['file'].lower()}" data-name="{name}">
  <td class="mono">{r['file']}</td>
  <td>{name}{f' <span class="dim">{desc}</span>' if desc else ''} {flag}</td>
  <td class="mono dim">{dims}</td>
  <td>{mobs or '<span class="dim">—</span>'}</td>
  <td>{merchs or '<span class="dim">—</span>'}</td>
  <td>{guards or '<span class="dim">—</span>'}</td>
</tr>""")

    # 怪物图鉴
    mon_rows = []
    for m in sorted(mon_summary, key=lambda x: -mon_summary[x]["count"]):
        e = mon_summary[m]
        maps = "、".join(f'<a href="#map-{f.lower()}" class="mono">{f}</a>' for f in sorted(e["maps"])[:6])
        more = f' <span class="dim">+{len(e["maps"])-6}</span>' if len(e["maps"]) > 6 else ""
        zh = mon_zh_name(m)
        mon_rows.append(f"""<tr>
  <td>{zh}</td><td class="mono dim">{m}</td><td class="mono">{e['count']}</td>
  <td>{len(e['maps'])}</td><td>{maps}{more}</td>
</tr>""")

    html = f"""<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>EI 传奇3.0 客户端 · 地图内容报告</title>
<style>
  :root {{ --bg:#15181d; --panel:#1e232b; --panel2:#262c36; --line:#333b47;
          --fg:#d7dde6; --dim:#8b95a3; --acc:#e8a33d; --good:#4ec9a0; --bad:#e86a6a; }}
  * {{ box-sizing:border-box; margin:0; padding:0; }}
  body {{ background:var(--bg); color:var(--fg); font:14px/1.6 "PingFang SC","Microsoft YaHei",sans-serif; padding:24px; max-width:1200px; margin:0 auto; }}
  h1 {{ font-size:24px; color:var(--acc); margin-bottom:6px; }}
  h2 {{ font-size:18px; color:var(--acc); margin:28px 0 12px; border-bottom:1px solid var(--line); padding-bottom:6px; }}
  .sub {{ color:var(--dim); margin-bottom:18px; font-size:13px; }}
  .stats {{ display:flex; gap:14px; flex-wrap:wrap; margin:14px 0 8px; }}
  .stat {{ background:var(--panel); border:1px solid var(--line); border-radius:8px; padding:10px 16px; min-width:110px; }}
  .stat b {{ display:block; font-size:20px; color:var(--acc); }}
  .stat span {{ font-size:12px; color:var(--dim); }}
  .controls {{ display:flex; gap:8px; margin:14px 0; flex-wrap:wrap; }}
  .controls input, .controls select {{ padding:7px 10px; background:var(--panel2); border:1px solid var(--line); border-radius:6px; color:var(--fg); outline:none; font-size:13px; }}
  .controls input:focus, .controls select:focus {{ border-color:var(--acc); }}
  table {{ width:100%; border-collapse:collapse; background:var(--panel); border-radius:8px; overflow:hidden; font-size:13px; }}
  th {{ background:var(--panel2); color:var(--acc); text-align:left; padding:8px 10px; font-weight:600; white-space:nowrap; }}
  td {{ padding:7px 10px; border-top:1px solid var(--line); vertical-align:top; }}
  tr:hover td {{ background:var(--panel2); }}
  .mono {{ font-family:ui-monospace,"Cascadia Mono",Consolas,monospace; font-size:12px; }}
  .dim {{ color:var(--dim); }}
  .tag {{ display:inline-block; padding:2px 7px; border-radius:10px; font-size:11px; font-weight:600; }}
  .tag-ei {{ background:#3a2a1e; color:var(--acc); }}
  .tag-mei {{ background:#2a1e3a; color:#c98ae8; }}
  .mon {{ color:var(--good); }}
  a {{ color:var(--acc); text-decoration:none; }}
  a:hover {{ text-decoration:underline; }}
  .nav {{ position:sticky; top:0; background:var(--bg); padding:8px 0; z-index:10; }}
  .nav a {{ margin-right:14px; }}
  #summary {{ background:var(--panel); border:1px solid var(--line); border-radius:8px; padding:14px 18px; margin:14px 0; }}
  #summary ul {{ margin:8px 0 0 20px; }}
  #summary li {{ margin:4px 0; }}
  .diff-box {{ background:var(--panel); border:1px solid var(--line); border-radius:8px; padding:14px 18px; margin:10px 0; }}
  .diff-box h3 {{ color:var(--acc); font-size:15px; margin-bottom:8px; }}
  .diff-box code {{ background:var(--panel2); padding:1px 6px; border-radius:4px; color:var(--good); }}
</style>
</head>
<body>
<h1>EI 传奇3.0 客户端 · 地图内容报告</h1>
<div class="sub">数据源: EI 客户端 Map/ (544 图) · Mud3 服务端 Mon_Def/*.gen (3221 条刷怪) · Merchant.txt (318 商人) · GuardList (117 守卫) · Zircon 术语表</div>

<div class="stats">
  <div class="stat"><b>{stats['ei_maps']}</b><span>EI 地图</span></div>
  <div class="stat"><b>{stats['mei_maps']}</b><span>mir3ei 地图</span></div>
  <div class="stat"><b>{len(ei_only)}</b><span>EI 独有</span></div>
  <div class="stat"><b>{len(mei_only)}</b><span>mir3ei 新增</span></div>
  <div class="stat"><b>{len(mon_summary)}</b><span>怪物种类</span></div>
  <div class="stat"><b>{stats['spawn_records']}</b><span>刷怪记录</span></div>
  <div class="stat"><b>{n_spawn_maps}</b><span>有怪地图</span></div>
  <div class="stat"><b>{n_merch}</b><span>商人</span></div>
</div>

<div class="nav">
  <a href="#diff">客户端差异</a>
  <a href="#maps">地图清单</a>
  <a href="#monsters">怪物图鉴</a>
  <a href="#method">数据与方法</a>
</div>

<h2 id="diff">一、EI vs mir3ei 客户端差异</h2>
<div class="diff-box">
  <h3>EI 独有地图（4 张，早期内容）</h3>
  <p><code>{'</code> <code>'.join(ei_only)}</code></p>
</div>
<div class="diff-box">
  <h3>mir3ei 新增地图（{len(mei_only)} 张，20 年积累的新内容）</h3>
  <p><code>{'</code> <code>'.join(mei_only)}</code></p>
  <p class="dim">集中在诺玛地下深层（D1500 系）与赤月山谷（D900 系）——裁剪的首要候选。</p>
</div>
<div class="diff-box">
  <h3>客户端资源差异</h3>
  <ul>
    <li>Data/: EI 182 文件 vs mir3ei 174（GameInter.wil 等 25 个同名 .wil 内容不同）</li>
    <li>Sound/: EI 609 vs mir3ei 761（+152 声音）</li>
    <li>根目录: EI 含 Mir4.exe（刺客版）/ Mir3hg.exe / Mir31.exe.bak 多版本入口</li>
  </ul>
</div>

<h2 id="maps">二、EI 地图清单（{len(report)} 张）</h2>
<div class="controls">
  <input id="q" type="text" placeholder="搜索地图 / 怪物 / 商人…" style="flex:1;min-width:240px">
  <select id="filt">
    <option value="">全部</option>
    <option value="spawn">有刷怪</option>
    <option value="merch">有商人</option>
    <option value="ei">EI 独有</option>
  </select>
</div>
<table>
<thead><tr><th>文件</th><th>名称</th><th>尺寸</th><th>怪物（×数量）</th><th>商人</th><th>守卫</th><th></th></tr></thead>
<tbody id="tbody">{''.join(rows)}</tbody>
</table>

<h2 id="monsters">三、怪物图鉴（{len(mon_summary)} 种，按总刷新量降序）</h2>
<table>
<thead><tr><th>中文名</th><th>英文/原名</th><th>总刷新量</th><th>出现地图数</th><th>出现地图</th></tr></thead>
<tbody>{''.join(mon_rows)}</tbody>
</table>

<h2 id="method">四、数据与方法</h2>
<div id="summary">
  <ul>
    <li><b>地图轴</b>: EI 客户端 <code>Map/*.map</code> 544 张, 28 字节头解析宽/高。</li>
    <li><b>刷怪</b>: Mud3 服务端 <code>Envir/Mon_Def/*.gen</code>（72 个文件, GBK 编码）, 格式 <code>地图 X Y 怪物名 数量 延迟 范围</code>, 共 3221 条, 覆盖 214 张 EI 图。</li>
    <li><b>商人</b>: <code>Merchant.txt</code> 318 个（脚本 地图 X Y 名字 脸 身）, 覆盖 64 张 EI 图。</li>
    <li><b>守卫</b>: <code>GuardList.txt</code> 117 个。</li>
    <li><b>服务端地图名</b>: <code>Mapinfo.txt</code> 365 个代码→中文名; 308 个与 EI 文件名直接同名命中。</li>
    <li><b>中英对照</b>: Zircon 术语表（怪物 301 / 地图 228 / NPC 98）。</li>
    <li><b>差异</b>: EI vs mir3ei 客户端 Map/ 目录文件名（忽略大小写）。</li>
  </ul>
</div>

<script>
const q = document.getElementById('q'), filt = document.getElementById('filt'), tbody = document.getElementById('tbody');
const rows = Array.from(tbody.children);
function apply() {{
  const s = q.value.trim().toLowerCase();
  const f = filt.value;
  for (const tr of rows) {{
    const txt = tr.textContent.toLowerCase();
    const has = txt.includes(s);
    let keep = has;
    if (keep && f === 'spawn') keep = !tr.querySelector('.mon') ? false : true;
    else if (keep && f === 'merch') keep = tr.cells[4].textContent.trim() !== '—';
    else if (keep && f === 'ei') keep = tr.textContent.includes('EI 独有');
    tr.style.display = keep ? '' : 'none';
  }}
}}
q.addEventListener('input', apply);
filt.addEventListener('change', apply);
</script>
</body>
</html>"""

    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "EI_MAP_REPORT.html")
    with open(out, "w", encoding="utf-8") as fh:
        fh.write(html)
    print(f"written: {out} ({os.path.getsize(out)} bytes)")

if __name__ == "__main__":
    main()
