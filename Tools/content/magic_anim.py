#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""解析 GodotClient/Scripts/MagicEffectTable.cs 的施法动画映射(权威 = 客户端实际渲染表)。

输出 /tmp/skills_anim.json:
  { "FireBall": {"lib": "Magic.Zl", "start": 420, "count": 5, "delay": 100, "src": "table"}, ... }

规则:
  - _table(CastEffect) = 客户端 SpawnCastEffect 实际渲染的施法特效 (施法动画)。
  - _attackTable(ImpactDef) = 近战技能命中时的挥舞/挥击特效 (MirAction.Attack 用),
    作为施法动画的补充, src="attack", 供 wiki 标注"近战挥击特效"。
    两者都渲染可播放; 但 _table 优先 (src 区分)。
  - 条目按 "[MagicType.X] = new CastEffect {" 起点切分, 正文直到下一条目起点,
    杜绝单行条目吞掉后续条目 (此前 SwiftBlade 单行吞掉 FireBall 的 bug)。
  - 客户端 GameScene 释放技能 switch 直接 return 的被动/变身技能
    (Swordsmanship/SpiritSword/VineTreeDance/WillowDance) 排除 —— 数据表有登记
    但实际绝不渲染 CastEffect。
  - 字段取条目正文顶层出现的 File/StartIndex/FrameCount/DelayMs
    (嵌套 Source/Projectile/Impact 的字段在嵌套块内, 不会先于顶层出现)。
  - DelayMs 缺省 100 (与 CastEffect/ImpactDef 字段默认值一致)。
"""
import json
import os
import re

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

LIB_FILE = {
    "LibraryFile.Magic": "Magic.Zl",
    "LibraryFile.MagicEx": "MagicEx.Zl",
    "LibraryFile.MagicEx2": "MagicEx2.Zl",
    "LibraryFile.MagicEx3": "MagicEx3.Zl",
    "LibraryFile.MagicEx4": "MagicEx4.Zl",
    "LibraryFile.MagicEx5": "MagicEx5.Zl",
    "LibraryFile.MagicEx6": "MagicEx6.Zl",
    "LibraryFile.MagicEx7": "MagicEx7.Zl",
    "LibraryFile.MagicEx8": "MagicEx8.Zl",
    "LibraryFile.MagicEx9": "MagicEx9.Zl",
    "LibraryFile.MagicEx10": "MagicEx10.Zl",
    "LibraryFile.MagicEx11": "MagicEx11.Zl",
}

CAST_RE = re.compile(r"\[MagicType\.(\w+)\] = new CastEffect\s*\{")
ATTACK_RE = re.compile(r"\[MagicType\.(\w+)\] = new ImpactDef\s*\{")


def _parse_dict(src, dict_name, start_pat, src_name, out):
    pat = re.compile(rf"private static readonly Dictionary<MagicType, \w+> {dict_name} = new\(\)\s*\{{(.*?)\n    \}};", re.S)
    m = pat.search(src)
    if not m:
        return
    body = m.group(1)
    starts = [(x.start(), x.group(1)) for x in start_pat.finditer(body)]
    for i, (s, tn) in enumerate(starts):
        end = starts[i + 1][0] if i + 1 < len(starts) else len(body)
        fields = body[s:end]
        # 顶层字段 = 条目开头到第一个嵌套特效对象前的部分
        nst = re.search(r"\b(?:Source|Projectile|Impact|MapImpact|TargetEffect|FrameEffect|Additional)\s*=\s*new\s+\w+Def\s*\{", fields)
        top = fields[:nst.start()] if nst else fields
        mf = re.search(r"File\s*=\s*(LibraryFile\.\w+)", top)
        ms = re.search(r"StartIndex\s*=\s*(\d+)", top)
        mc = re.search(r"FrameCount\s*=\s*(\d+)", top)
        md = re.search(r"DelayMs\s*=\s*(\d+)", top)
        if not mf or not ms or mf.group(1) not in LIB_FILE:
            continue
        try:
            start = int(ms.group(1))
            count = int(mc.group(1)) if mc else 1
            delay = int(md.group(1)) if md else 100
        except ValueError:
            continue
        if count < 1 or count > 60:
            count = 1
        if tn not in out:  # 先解析的 _table 优先, _attackTable 不覆盖
            out[tn] = {"lib": LIB_FILE[mf.group(1)], "start": start, "count": count,
                       "delay": delay, "src": src_name}


def parse_effect_table():
    """解析 MagicEffectTable.cs → {type_name: {lib, start, count, delay, src}}"""
    src = open(os.path.join(REPO, "GodotClient/Scripts/MagicEffectTable.cs"),
               encoding="utf-8").read()
    out = {}
    _parse_dict(src, "_table", CAST_RE, "table", out)
    _parse_dict(src, "_attackTable", ATTACK_RE, "attack", out)
    # 客户端 GameScene 释放技能 switch 中直接 return 的技能 = 被动/变身,
    # 绝不渲染 CastEffect(数据表虽有登记, 但实际无施法动画)。
    for t in ("Swordsmanship", "SpiritSword", "VineTreeDance", "WillowDance"):
        out.pop(t, None)
    return out


def main():
    anim = parse_effect_table()
    with open("/tmp/skills_anim.json", "w", encoding="utf-8") as fh:
        json.dump(anim, fh, ensure_ascii=False, indent=1)
    print(f"施法动画映射: {len(anim)} 条 -> /tmp/skills_anim.json")
    for k in sorted(anim):
        print("  ", k, anim[k])


if __name__ == "__main__":
    main()
