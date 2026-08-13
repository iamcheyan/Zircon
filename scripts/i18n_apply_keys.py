#!/usr/bin/env python3
"""按审核后的映射表执行：三文件补键 + 代码替换（状态机版，正确处理 $ 插值）
用法: python3 scripts/i18n_apply_keys.py /tmp/i18n_keymap_final.txt
映射表格式: 键名<TAB>"中文值"

规则:
- "纯中文"        -> Lang.Key
- $"中文{expr}"   -> string.Format(Lang.Key, expr...)  (键值占位 {0},{1}...)
- 插值内含嵌套引号/复杂结构 -> 整行跳过（人工处理）
"""
import re, sys, os

os.chdir('/home/tetsuya/development/zircon')
KEYMAP = sys.argv[1]

value_to_key = {}
for line in open(KEYMAP, encoding='utf-8'):
    line = line.strip()
    if not line or line.startswith('#'): continue
    parts = line.split('\t')
    if len(parts) < 2: continue
    key = parts[0]
    m = re.match(r'"(.*)"$', parts[1])
    if not m: continue
    v = m.group(1)
    if v not in value_to_key:
        value_to_key[v] = key

def split_expr_fmt(raw):
    depth = 0
    for ci in range(len(raw)):
        c = raw[ci]
        if c in '([{': depth += 1
        elif c in ')]}': depth -= 1
        elif c == ':' and depth == 0:
            return raw[:ci], raw[ci+1:]
    return raw, ''

def fmt_value(v):
    out = []
    idx = 0
    i = 0
    while i < len(v):
        ch = v[i]
        if ch == '{':
            if i+1 < len(v) and v[i+1] == '{':
                out.append('{{'); i += 2; continue
            depth = 1; start = i+1; i += 1
            while i < len(v) and depth > 0:
                if v[i] == '{': depth += 1
                elif v[i] == '}': depth -= 1
                i += 1
            inner = v[start:i-1]
            expr, fmt = split_expr_fmt(inner)
            out.append(f'{{{idx}' + (f':{fmt}' if fmt else '') + '}')
            idx += 1
        elif ch == '}' and i+1 < len(v) and v[i+1] == '}':
            out.append('}}'); i += 2
        else:
            out.append(ch); i += 1
    return ''.join(out)

# 1. 补键
def add_keys(fname, template, has_value=False):
    content = open(fname, encoding='utf-8').read()
    existing = set(re.findall(r'abstract string (\w+)', content)) if 'abstract' in template else \
               set(re.findall(r'override string (\w+)', content))
    new_keys = [(k, v) for v, k in value_to_key.items() if k not in existing]
    if not new_keys: return 0
    def render(k, v):
        return template.format(k=k, v=fmt_value(v)) if has_value else template.format(k=k)
    add = '\n'.join(render(k, v) for k, v in sorted(new_keys))
    # 在最后一个 "    }\n}"（类闭合 + namespace 闭合）之前插入
    marker = '\n    }\n}'
    idx = content.rfind(marker)
    if idx < 0:
        # 兜底：文件末尾前插入
        content = content.rstrip() + '\n' + add + '\n'
    else:
        content = content[:idx] + '\n' + add + content[idx:]
    open(fname, 'w', encoding='utf-8').write(content)
    return len(new_keys)

n1 = add_keys('GodotClient/Translations/StringMessages.cs',
    '        public abstract string {k} {{ get; set; }}')
n2 = add_keys('GodotClient/Translations/ChineseMessages.cs',
    '        public override string {k} {{ get; set; }} = "{v}";', has_value=True)
n3 = add_keys('GodotClient/Translations/EnglishMessages.cs',
    '        public override string {k} {{ get; set; }} = "{v}";', has_value=True)
print(f'补键: StringMessages +{n1}, Chinese +{n2}, English +{n3}')

# 1.5 Lang.cs 补转发属性
lang_path = 'GodotClient/Scripts/Lang.cs'
lang_content = open(lang_path, encoding='utf-8').read()
existing_lang = set(re.findall(r'public static string (\w+) => Current\.(\w+);', lang_content))
new_lang = []
for v, k in value_to_key.items():
    if k not in existing_lang:
        new_lang.append(k)
if new_lang:
    add = '\n'.join(f'    public static string {k} => Current.{k};' for k in sorted(new_lang))
    marker = '\n}'
    idx = lang_content.rfind(marker)
    lang_content = lang_content[:idx] + '\n' + add + lang_content[idx:]
    open(lang_path, 'w', encoding='utf-8').write(lang_content)
print(f'Lang.cs 补转发: {len(new_lang)}')

# 2. 代码替换（逐字符状态机）
files = []
for d in ['GodotClient/Controls', 'GodotClient/Scripts', 'GodotClient/Scenes']:
    for f in sorted(os.listdir(d)):
        if f.endswith('.cs'): files.append(f'{d}/{f}')

replaced_plain = 0
replaced_interp = 0
skipped_lines = []
CJK = re.compile(r'[\u4e00-\u9fff]')

def process_line(line):
    """返回 (新行, 是否跳过)。跳过时原样返回。"""
    global replaced_plain, replaced_interp
    out = []
    i = 0
    n = len(line)
    while i < n:
        ch = line[i]
        # 检测 $"
        if ch == '$' and i+1 < n and line[i+1] == '"':
            # 扫描到结束引号（允许转义 \"，但遇嵌套引号则整行放弃）
            j = i + 2
            inner_chars = []
            complex = False
            while j < n:
                if line[j] == '\\':
                    inner_chars.append(line[j]); 
                    if j+1 < n: inner_chars.append(line[j+1])
                    j += 2; continue
                if line[j] == '"':
                    # 结束引号标志: 后面跟 , ; ) ] } : ? 或行尾
                    nxt = line[j+1] if j+1 < n else ''
                    if nxt in ',;)]}:? ' or nxt == '' or nxt == '.':
                        break
                    else:
                        complex = True
                        break
                inner_chars.append(line[j]); j += 1
            if complex:
                return line, True
            inner = ''.join(inner_chars)
            close = j
            # 提取占位符
            exprs = []
            k = 0
            while k < len(inner):
                if inner[k] == '{':
                    if k+1 < len(inner) and inner[k+1] == '{': k += 2; continue
                    depth = 1; start = k+1; k += 1
                    while k < len(inner) and depth > 0:
                        if inner[k] == '{': depth += 1
                        elif inner[k] == '}': depth -= 1
                        k += 1
                    raw = inner[start:k-1]
                    if '"' in raw:
                        return line, True  # 表达式含字符串字面量
                    expr, _ = split_expr_fmt(raw)
                    exprs.append(expr)
                else:
                    k += 1
            if CJK.search(inner) and inner in value_to_key:
                key = value_to_key[inner]
                if exprs:
                    replaced_interp += 1
                    out.append(f'string.Format(Lang.{key}, {", ".join(exprs)})')
                else:
                    replaced_interp += 1
                    out.append(f'Lang.{key}')
                i = close + 1
                continue
            else:
                out.append(line[i:close+1])
                i = close + 1
                continue
        # 检测普通 "纯中文"（前一个字符不是 $）
        elif ch == '"' and (i == 0 or line[i-1] != '$'):
            # 扫描普通字符串结束
            j = i + 1
            buf = []
            while j < n:
                if line[j] == '\\':
                    buf.append(line[j])
                    if j+1 < n: buf.append(line[j+1])
                    j += 2; continue
                if line[j] == '"': break
                buf.append(line[j]); j += 1
            s = ''.join(buf)
            close = j
            if CJK.search(s) and s in value_to_key and '"' not in s:
                replaced_plain += 1
                out.append(f'Lang.{value_to_key[s]}')
                i = close + 1
                continue
            else:
                out.append(line[i:close+1])
                i = close + 1
                continue
        else:
            out.append(ch)
            i += 1
    return ''.join(out), False

for f in files:
    if f.endswith('Lang.cs'): continue
    lines = open(f, encoding='utf-8').read().split('\n')
    new_lines = []
    for line in lines:
        stripped = line.strip()
        if stripped.startswith('//') or stripped.startswith('*'):
            new_lines.append(line); continue
        if 'GD.Print' in line or 'GD.PushError' in line or 'Console.' in line:
            new_lines.append(line); continue
        nl, skipped = process_line(line)
        if skipped:
            skipped_lines.append(f)
        new_lines.append(nl)
    open(f, 'w', encoding='utf-8').write('\n'.join(new_lines))
print(f'替换: 纯中文 {replaced_plain} 处, $插值 {replaced_interp} 处')
print(f'跳过的复杂行文件: {len(set(skipped_lines))} 个 ({sorted(set(skipped_lines))[:8]})')
