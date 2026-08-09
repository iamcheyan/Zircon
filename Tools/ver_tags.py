#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""ver_tags.py — 给 wiki_data.json 全部板块打版本标签 (ver) + 图片引用 (img)。

版本集合: mud3 / ei / mei / zircon
  - ei / mei = WIL 素材存在性（wil_probe: 客户端 Data 目录有对应图库且帧可解出非空）
    —— 不代表服务端内容存在; 服务端存在性由 mud3/zircon 承担
  - mud3     = 服务端数据匹配（three_versions 共有怪 / MonItems 掉落 / magic.dat SKILL_MAP）
  - zircon   = Zircon 服务端来源条目恒含
  - 老版 DAT 独有条目（wiki_dat.json 合并）: ver=["mud3"]（素材存在性由其 img 显示, 不双标）
  - 筛选 "仅X" = ver == {X}; "含X" = X in ver

图片引用 img: {lib, frame, src} 或 null; lib 为 EI/mir3ei 客户端 WIL 文件:
  - 怪物: Image 枚举名 → MonsterLookup.cs (Mon_N, shape) → 帧 40 + shape*1000, Mon-N.wil
  - 装备: ItemInfo.Image → StoreItem.wil 帧
  - 技能: MagicInfo.Icon  → MIcon.wil 帧
  - NPC:  NPCInfo.FaceImage → NPCface.wil; Image → NPC.wil (Zircon 私有 Zl, 不探客户端素材)
  - 宠物: 绑定怪物 → 同怪物图

数据源:
  - /tmp/wiki_data.json     (百科数据)
  - /tmp/three_versions.json(三版本交叉: 地图/怪物/装备)
  - /tmp/wiki_images.json   (SystemDbProbe --images: 各实体图片编号, 权威)
  - /tmp/wiki_dat.json      (dat_integrate.py: 老版 DAT 独有条目 + both/changed 挂靠)
输出: /tmp/wiki_data_v2.json
"""
import json, os, re, sys
from datetime import datetime, timezone

ROOT = os.path.dirname(os.path.abspath(__file__))
DOCS = os.path.join(ROOT, "..", "docs")
sys.path.insert(0, ROOT)
import wil_probe  # noqa: E402

def read(p):
    with open(p, encoding="utf-8") as f:
        return f.read()

w = json.load(open("/tmp/wiki_data.json"))
tv = json.load(open("/tmp/three_versions.json"))
imgs = json.load(open("/tmp/wiki_images.json"))

# 老版 DAT 独有条目 + both/changed 挂靠（dat_integrate.py 产出; 缺失时链未跑, 跳过）
try:
    dat = json.load(open("/tmp/wiki_dat.json", encoding="utf-8"))
except FileNotFoundError:
    dat = None

# ============ MonsterLookup: 枚举名 -> (库序号, shape) ============
mon_lookup = {}
lookup_path = os.path.join(ROOT, "..", "GodotClient", "Formats", "MonsterLookup.cs")
if os.path.exists(lookup_path):
    txt = read(lookup_path)
    for m in re.finditer(r"MonsterImage\.(\w+),\s*\(LibraryFile\.(\w+),\s*(\d+)\)", txt):
        mon_lookup[m.group(1)] = (m.group(2), int(m.group(3)))
print(f"MonsterLookup {len(mon_lookup)} 条枚举映射")

def mon_img(enum_name):
    """枚举名 -> {lib, frame, shape} 或 None"""
    if enum_name in mon_lookup:
        lib, shape = mon_lookup[enum_name]
        n = lib.replace("Mon_", "")
        if n.isdigit():
            return {"lib": f"Mon-{int(n)}.wil", "frame": 40 + shape * 1000,
                    "shape": shape, "src": "ei"}
    return None

# ============ 中文名族 -> 英文族关键词 (与 three_versions_check 一致) ============
FAMILY = {
    "诺玛": ["numa", "noma"], "祖玛": ["zuma"], "沃玛": ["oma", "worm"],
    "骷髅": ["skeleton", "bone"], "僵尸": ["zombie", "thirsty"], "蜘蛛": ["spider"],
    "蚂蚁": ["ant"], "沙漠": ["sand", "desert"], "蛇": ["snake", "serpent"],
    "蝎": ["scorpion"], "蛆": ["maggot", "worm"], "蜈蚣": ["centipede"],
    "蜂": ["bee", "flea"], "甲虫": ["beetle", "bug"], "蛾": ["moth"],
    "蝙蝠": ["bat"], "猫": ["cat"], "猪": ["boar", "pig"], "狼": ["wolf"],
    "鹿": ["deer"], "鸡": ["chicken", "hen"], "牛": ["cow", "bull", "ox"],
    "雪人": ["yeti"], "树": ["tree"], "花": ["flower", "plant"],
    "虫": ["worm", "bug", "insect"], "恶魔": ["demon", "devil"],
    "半兽": ["orc", "ogre"], "精灵": ["elf", "spirit"],
    "鬼": ["ghost", "spirit", "spectre"], "幽灵": ["ghost", "phantom"],
    "石": ["golem", "rock", "stone"], "卫士": ["guard"],
    "战士": ["warrior", "grunt", "soldier"], "法老": ["mage", "pharaoh", "elder"],
    "教主": ["king", "lord", "master"], "神": ["god", "lord", "king"],
    "龙": ["dragon"], "鹰": ["eagle", "hawk"], "鲨": ["shark"],
    "章鱼": ["octopus"], "蛙": ["frog", "toad"], "蛤蟆": ["frog", "toad"],
}

def zir_in_mud3(zname, zzh):
    """Zircon 怪物 (英文名, 术语中文名) 是否对应 MUD3 共享集合中的某个中文名。"""
    if zzh in shared_zh:
        return True
    for s in shared_zh:
        if not s or len(s) < 2 or len(zzh) < 2:
            continue
        if s in zzh or zzh in s:
            return True
    low = zname.lower()
    for kw, ens in FAMILY.items():
        if any(e in low for e in ens) and any(kw in s for s in shared_zh):
            return True
    return False

# ============ 1. 地图版本 ============
M = tv["maps"]
ver_map = {}
def add(f, *vs):
    ver_map.setdefault(f.lower(), set()).update(vs)
for f in M["core"]: add(f, "mud3", "ei", "mei", "zircon")
for f in M["mud3_only"]: add(f, "mud3")
for f in M["mei_mud3_shared"]: add(f, "mud3", "mei")
for f in M["three_wo_ei"]: add(f, "mud3", "mei", "zircon")
for f in M["ei_only"]: add(f, "ei")
for f in M["zir_only"]: add(f, "zircon")
for f in M.get("mei_zir_shared", []): add(f, "mei", "zircon")
for f in M.get("mud3_zir_shared", []): add(f, "mud3", "zircon")

for m in w["maps"]:
    m["ver"] = sorted(ver_map.get(m["file"].lower() + ".map", ["zircon"]))
for m in w["ei_maps"]:
    m["ver"] = sorted(ver_map.get(m["name"].lower(), ["ei"]))

# ============ 2. 怪物版本 + 图 ============
shared_zh = set(s["zh"] for s in tv["monsters"]["shared"])
term = w["terminology"]
mon_img_map = imgs["monsters"]   # 显示名 -> 枚举名

for m in w["monsters"]:
    zh = term.get(m["name"], m["name"])
    m["zh"] = zh
    vs = ["zircon"]
    if zir_in_mud3(m["name"], zh):
        vs.append("mud3")
    en = mon_img_map.get(m["name"])
    m["img"] = mon_img(en) if en else None
    if m["img"] is not None:
        # legacy 老版怪 img 可能无 shape: frame = 40 + shape*1000 → 反推
        if "shape" not in m["img"]:
            f = m["img"].get("frame")
            if isinstance(f, int) and f % 1000 == 40:
                m["img"]["shape"] = f // 1000
        # A1: ei/mei 按素材存在性真打（不伪造服务端存在性）
        vs += wil_probe.client_tags(m["img"]["lib"], m["img"]["frame"])
    m["ver"] = sorted(set(vs))
    # 怪物动画: 有素材帧即有 (动作帧数由 img_pipeline 渲染后回填)
    m["anim"] = {"lib": m["img"]["lib"], "shape": m["img"]["shape"],
                 "actions": {}} if m["img"] and "shape" in m["img"] else None
    if en and m["img"] is None:
        pass  # 枚举名在 MonsterLookup 无映射 (CastleFlag 等)

# ============ 3. 装备版本 + 图 ============
# MUD3 掉落中文名集合
mud3_item_zh = set()
pat = re.compile(r"^\s*1/\d+\s+(.+)$")
MUD3 = "/home/tetsuya/NAS/TMP/Mud3/Envir"
if os.path.isdir(MUD3):
    import glob
    for fn in glob.glob(os.path.join(MUD3, "MonItems", "*.txt")):
        for line in open(fn, encoding="gbk", errors="replace"):
            mm = pat.match(line)
            if mm:
                name = mm.group(1).strip()
                if not name.startswith("金币"):
                    mud3_item_zh.add(name)
pat = re.compile(r"^\s*1/\d+\s+(.+)$")
MUD3 = "/home/tetsuya/NAS/TMP/Mud3/Envir"

item_img_map = imgs["items"]   # ItemName -> Image 帧号
for it in w["items"]:
    zh = term.get(it["name"], it["name"])
    it["zh"] = zh
    vs = ["zircon"]
    if zh in mud3_item_zh or (len(zh) >= 2 and any(
            s in zh or zh in s for s in mud3_item_zh if len(s) >= 2)):
        vs.append("mud3")
    img = item_img_map.get(it["name"])
    it["img"] = {"lib": "StoreItem.wil", "frame": img, "src": "ei"} if img is not None else None
    if it["img"] is not None:
        vs += wil_probe.client_tags("StoreItem.wil", img)
    it["ver"] = sorted(set(vs))

# ============ 4. 技能版本 + 图 ============
skill_img_map = imgs["skills"]
try:
    skill_anim = json.load(open("/tmp/skills_anim.json", encoding="utf-8"))
except FileNotFoundError:
    skill_anim = {}
# 老版 magic.dat 有同名对应（SKILL_MAP both/changed）→ 服务端数据确在 MUD3
ref_skill_ids = set(dat.get("ref", {}).get("skills", {})) if dat else set()
for s in w["skills"]:
    vs = ["zircon"]
    if str(s["id"]) in ref_skill_ids:
        vs.append("mud3")
    ic = skill_img_map.get(s["name"], s.get("icon"))
    s["img"] = {"lib": "MIcon.wil", "frame": int(ic), "src": "ei"} if ic is not None else None
    if s["img"] is not None:
        vs += wil_probe.client_tags("MIcon.wil", int(ic))
    s["ver"] = sorted(set(vs))
    a = skill_anim.get(s["type"])
    s["anim"] = a if a else None  # 施法动画 (Magic.Zl/MagicEx*.Zl 帧段), 无则诚实标注

# ============ 5. NPC 版本 + 图 ============
# 权威图源 = Zircon 客户端 NPC.Zl / NPCface.Zl:
#   NPCInfo.Image (shape) × 100 + 站立帧 (DefaultNPC = 帧 100..103)
#   NPCface.Zl 帧号 = NPCInfo.Image 直接对应 (System.db FaceImage 未填, 全 0)
#   EI NPC.wil 帧号 ≠ NPCInfo.Image, 直接映射会错位(全铁匠) — 弃用
npc_img_map = imgs["npcs"]
for n in w["npcs"]:
    n["ver"] = ["zircon"]  # 老版 NPC.wil 布局与 Zircon NPC.Zl 不同, 不标 ei/mei（诚实）
    e = npc_img_map.get(n["name"])
    if e:
        n["img"] = {"lib": "NPC.Zl", "frame": e["image"] * 100, "shape": e["image"], "src": "zir"}
        # 头像: NPCface.Zl 帧号 = Image; 帧存在才给 (部分 NPC 无头像)
        n["face_img"] = {"lib": "NPCface.Zl", "frame": e["image"], "shape": e["image"], "src": "zir"}
    else:
        n["img"] = None
        n["face_img"] = None

# ============ 6. 任务 ============
for q in w["quests"]:
    q["ver"] = ["zircon"]
    q["img"] = None

# ============ 7. 宠物坐骑 ============
companion_mon = imgs["companions"]   # 怪物名 -> {price, available}
for c in w["companion"]:
    c["ver"] = ["zircon"]
    mon_name = c.get("name", c.get("monster", ""))
    en = mon_img_map.get(mon_name)
    c["img"] = mon_img(en) if en else None

# ============ 8. 老版 DAT 条目并入（dat_integrate.py 产物） ============
if dat is not None:
    # both/changed 挂靠: 老版属性作 old 子对象挂 Zircon 条目（不建卡）
    for board, ref_key in (("monsters", "monsters"), ("items", "items"),
                           ("skills", "skills")):
        refs = dat["ref"].get(ref_key, {})
        for x in w[board]:
            r = refs.get(str(x["id"]))
            if r:
                x["old"] = r
                vs = set(x.get("ver", []))
                vs.add("mud3")  # 老版 DAT 有同名对应 → 服务端数据确在 MUD3
                x["ver"] = sorted(vs)
    # 老版独有条目: 负 id 卡, 已带 ver/img/legacy, 原样并入主列表
    for board in ("monsters", "items", "skills"):
        for x in dat.get(board, []):
            x.setdefault("ver", ["mud3"])
            x.setdefault("img", None)
            if board == "monsters" and x.get("img") and "shape" not in x["img"]:
                f = x["img"].get("frame")
                if isinstance(f, int) and f % 1000 == 40:
                    x["img"]["shape"] = f // 1000
            w[board].append(x)

# ============ 输出 ============
out = "/tmp/wiki_data_v2.json"
w["_meta"] = {"generated_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
              "script": "ver_tags.py",
              "input": ["wiki_data.json", "three_versions.json", "wiki_images.json",
                        "wiki_dat.json", "skills_anim.json"]}
json.dump(w, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)

from collections import Counter
print(f"输出 {out}")
for k in ["maps", "monsters", "items", "skills", "npcs", "quests", "companion"]:
    c = Counter()
    for x in w[k]:
        c["+".join(x.get("ver", []))] += 1
    img_n = sum(1 for x in w[k] if x.get("img"))
    leg = sum(1 for x in w[k] if x.get("legacy"))
    print(f"{k}: {len(w[k])} 条 | ver {dict(c)} | 有图 {img_n} | 老版卡 {leg}")
if dat is not None:
    print(f"挂靠: 怪 {len(dat['ref']['monsters'])} / 装 {len(dat['ref']['items'])} / "
          f"技 {len(dat['ref']['skills'])}")
