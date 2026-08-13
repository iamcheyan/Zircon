#!/usr/bin/env python3
"""自动替换唯一键的中文字面量: "中文" -> Lang.Key
只处理：单行、非注释、值唯一映射（同值多键跳过）。
用法: python3 scripts/i18n_auto_replace.py [--dry-run]
"""
import re, glob, os, sys

os.chdir('/home/tetsuya/development/zircon')

# 旧版中文值->键映射
cn = open('GodotClient/Translations/ChineseMessages.cs', encoding='utf-8').read()
pairs = re.findall(r'public override string (\w+) \{ get; set; \} = "([^"]*)"', cn)
value_to_keys = {}
for k, v in pairs:
    value_to_keys.setdefault(v, []).append(k)
single_value_keys = {v: ks[0] for v, ks in value_to_keys.items() if len(ks) == 1}

DRY = '--dry-run' in sys.argv
files = glob.glob('GodotClient/Controls/*.cs') + glob.glob('GodotClient/Scripts/*.cs') + glob.glob('GodotClient/Scenes/*.cs')
done = {'InventoryDialog.cs', 'MainPanel.cs', 'ConfigDialog.cs', 'Lang.cs'}

changed_files = []
total_replaced = 0
for f in files:
    name = f.split('/')[-1]
    if name in done: continue
    lines = open(f, encoding='utf-8').read().split('\n')
    new_lines = []
    file_changed = 0
    for line in lines:
        stripped = line.strip()
        if stripped.startswith('//') or stripped.startswith('*'):
            new_lines.append(line)
            continue
        # 逐字符串替换：只处理值唯一映射的
        def repl(m):
            global total_replaced
            v = m.group(1)
            if v in single_value_keys:
                total_replaced += 1
                return f'Lang.{single_value_keys[v]}'
            return m.group(0)
        nline, n = re.subn(r'"([^"]*[\u4e00-\u9fff][^"]*)"', repl, line)
        # 只统计实际替换（nline != line 时）
        if nline != line:
            file_changed += 1
            line = nline
        new_lines.append(line)
    if file_changed:
        changed_files.append((name, file_changed))
        if not DRY:
            open(f, 'w', encoding='utf-8').write('\n'.join(new_lines))

changed_files.sort(key=lambda x: -x[1])
print(f'{"[DRY-RUN] " if DRY else ""}自动替换完成: {total_replaced} 处, {len(changed_files)} 个文件')
for name, n in changed_files:
    print(f'  {n:3d}  {name}')
