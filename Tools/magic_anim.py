#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""解析 GodotClient/Scripts/MagicEffectTable.cs 的魔法施法动画映射。

输出 /tmp/skills_anim.json:
  { "HalfMoon": {"lib": "Magic.Zl", "start": 230, "count": 6, "delay": 100}, ... }

数据源 (权威):
  - MagicEffectTable._table      (CastEffect: 施法/投射/落点主特效)
  - MagicEffectTable._attackTable (ImpactDef:  近战技能的挥舞特效)
  - LibraryCore/Enum.cs MagicType 枚举名 → 与 wiki skills[].type 同名
  - LibraryCore/Libraries.cs LibraryFile → 实际 .Zl 文件名
优先 _table, 无则 _attackTable。
"""
import json
import os
import re
import sys

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


def parse_effect_table():
    """解析 MagicEffectTable.cs → {type_name: {lib, start, count, delay}}"""
    src = open(os.path.join(REPO, "GodotClient/Scripts/MagicEffectTable.cs"),
               encoding="utf-8").read()
    out = {}

    def block(name, cls):
        m = re.search(rf"private static readonly Dictionary<MagicType, \w+> {name} = new\(\)\s*\{{(.*?)\n    \}};", src, re.S)
        if not m:
            return
        body = m.group(1)
        # 按顶层条目切分: 单行 [MagicType.X] = new CastEffect {...}, 与多行嵌套条目
        for entry in re.finditer(r"\[MagicType\.(\w+)\] = new CastEffect\s*\{(.*?)\n        \},?", body, re.S):
            tn, fields = entry.group(1), entry.group(2)
            parse_fields(out, tn, fields)
        for entry in re.finditer(r"\[MagicType\.(\w+)\] = new ImpactDef\s*\{(.*?)\n        \},?", body, re.S):
            tn, fields = entry.group(1), entry.group(2)
            parse_fields(out, tn, fields)
        # 单行条目 (字段间无换行, 条目间以 [MagicType. 或 \n    }; 结尾)
        for entry in re.finditer(r"\[MagicType\.(\w+)\] = new (?:CastEffect|ImpactDef) \{ ([^}\n]+) \},?", body):
            tn, fields = entry.group(1), entry.group(2)
            parse_fields(out, tn, fields)

    def parse_fields(out, tn, fields):
        if tn in out:
            return  # _table 优先
        # 取第一个 File/StartIndex/FrameCount/DelayMs = 顶层主特效
        # (嵌套 Source/Projectile/Impact 的字段在行内更靠后, 忽略)
        mf = re.search(r"File\s*=\s*(LibraryFile\.\w+)", fields)
        ms = re.search(r"StartIndex\s*=\s*(\d+)", fields)
        mc = re.search(r"FrameCount\s*=\s*(\d+)", fields)
        md = re.search(r"DelayMs\s*=\s*(\d+)", fields)
        if not mf or not ms or mf.group(1) not in LIB_FILE:
            return
        try:
            start = int(ms.group(1))
            count = int(mc.group(1)) if mc else 1
            delay = int(md.group(1)) if md else 100
        except ValueError:
            return
        if count < 1 or count > 30:
            count = 1
        out[tn] = {"lib": LIB_FILE[mf.group(1)], "start": start, "count": count, "delay": delay}

    # 嵌套的 Projectile/Impact 里的 StartIndex 不能当主特效; 只取顶层字段。
    # 上面正则按缩进(8空格)切条目, 嵌套块(12空格)不会误切。
    block("_table", "CastEffect")
    block("_attackTable", "ImpactDef")
    return out


def main():
    anim = parse_effect_table()
    out_path = "/tmp/skills_anim.json"
    json.dump(anim, open(out_path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(f"技能动画映射: {len(anim)} 条 -> {out_path}")
    for k in sorted(anim)[:8]:
        print("  ", k, anim[k])


if __name__ == "__main__":
    main()
