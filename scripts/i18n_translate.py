#!/usr/bin/env python3
"""按批次翻译文件写入三语。批次格式: 键名<TAB>中文<TAB>英文<TAB>日文
用法: python3 scripts/i18n_translate.py batch_001.tsv
"""
import re, sys, os

os.chdir('/home/tetsuya/development/zircon')
BATCH = sys.argv[1]

entries = []  # (key, cn, en, ja)
for line in open(BATCH, encoding='utf-8'):
    line = line.rstrip('\n')
    if not line.strip() or line.startswith('#'): continue
    parts = line.split('\t')
    if len(parts) < 4: continue
    entries.append((parts[0], parts[1], parts[2], parts[3]))
print(f'批次: {len(entries)} 条')

def apply(fname, idx, pairs):
    content = open(fname, encoding='utf-8').read()
    n = 0
    for k, v in pairs:
        # 精确替换 override string K { get; set; } = "旧值";
        pat = re.compile(r'(public override string ' + re.escape(k) + r' \{ get; set; \} = ")([^"]*)(";)')
        m = pat.search(content)
        if m:
            content = pat.sub(lambda mm: mm.group(1) + v + mm.group(3), content, count=1)
            n += 1
        else:
            print(f'  !! 未找到键 {k} in {fname}')
    open(fname, 'w', encoding='utf-8').write(content)
    return n

n_cn = apply('GodotClient/Translations/ChineseMessages.cs', 1, [(k, c) for k, c, e, j in entries])
n_en = apply('GodotClient/Translations/EnglishMessages.cs', 2, [(k, e) for k, c, e, j in entries])
n_ja = apply('GodotClient/Translations/JapaneseMessages.cs', 3, [(k, j) for k, c, e, j in entries])
print(f'写入: CN {n_cn}, EN {n_en}, JA {n_ja}')
