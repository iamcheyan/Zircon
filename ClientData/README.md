# ClientData — 客户端共享数据层

E5 数据层的 canonical 位置: Godot 客户端与 web 测试台 (:8822) 共读同一套 JSON, **文件即唯一事实源, 没有第二份副本**。
生成器与门禁工具住在 `~/development/Mir3-Research/Tools/`; 设计文档 `Mir3-Research/docs/editor/E5_DATALAYER_GOAL.md`。

## 三文件一览

| 文件 | 管什么 | 唯一源 | 网页编辑 | 验证 |
|---|---|---|---|---|
| `magic-effects.json` | 技能特效表: 148 技能 (original 原版段 + godot 结构段) + attackTable 26 + 两白名单 | 本文件 (E5/B4 cutover 已删 C# 硬编码表) | ✅ /lab 编辑回写 | `gen_cs_table.py --check` |
| `frame-formulas.json` | 动画帧表 94 表 552 项 + 枚举/分派/纸娃娃公式 | C# 源 (`FrameSet.cs` 等), 本文件是单向镜像 | ❌ 只读 | `frameformulas.py --check` |
| `sounds.json` | 三张音效 catalog: sounds 731 / magic 159 组 / monster 118 | 本文件 (C# 表已删) | ❌ 暂无编辑器 | 启动看 `[DataLayer] sounds:` 日志 |

## 数据流向

**Godot 端** (改 JSON → 重启客户端即生效):

```
NetworkManager._Ready()        autoload 首位, 先于任何场景脚本消费表
  └─ DataLayer.LoadAll()       GodotClient/Scripts/DataLayer.cs
       ├─ frame-formulas.json → Library.FrameSet 全部静态字典 (先 Clear 再重填, 94 表)
       ├─ magic-effects.json  → MagicEffectTable._table/_attackTable/两白名单 (godot 段)
       └─ sounds.json         → SoundCatalog.Entries / MagicSoundCatalog.Explicit / MonsterSoundCatalog.Entries
```

- 目录解析顺序: ① 环境变量 `ZIRCON_CLIENT_DATA` → ② `res://ClientData` (导出包) → ③ dev checkout (工程上级 `../ClientData`) → ④ 进程工作目录。
- 容错: 缺文件/坏条目只 `GD.PrintErr` 跳过不崩客户端; 一致性由门禁兜底。

**web 端** (`Mir3-Research/Tools/webclient/serve.py`, 经 `MIR3_ZIRCON_ROOT` 解析本目录):

```
GET  /lab/table           magic-effects.json → 投影回旧扁平结构 (lab.js 兼容)
GET  /lab/frame-formulas  frame-formulas.json 原样返回 (只读)
POST /lab/save            写回 magic-effects.json original 段:
                          帧三元组变化时同步 godot 段 → 写 .bak → 回读回显校验
                          → gen_cs_table --check --skip-runtime 自检 → 失败自动回滚
```

## 怎么改

### magic-effects.json — 网页编辑 (推荐) 或手改 original 段
- 推荐 `/lab` 页编辑保存: `/lab/save` 自动做 godot 段同步 + `.bak` 备份 + 自检 + 失败回滚。
  不支持增删特效条目, 帧三元组数量变化会被 422 拒绝。
- 手改: 改 `skills.<技能>.original` 段后必须跑 `gen_cs_table.py --check` — godot 段三元组须与
  original 段一致 (7 项历史可接受差异见 `_meta.acceptable`, 新增差异会被拦)。
- 改完 Godot 重启生效, 网页刷新即可见。

### sounds.json — 手改 JSON (暂无网页编辑器)
- 键名必须与枚举名严格一致 (DataLayer 用 `Enum.Parse`, 拼错即启动 PrintErr 跳过):
  - `sounds` 键 = SoundIndex 名, 值 `{file, category, loop}`;
  - `magic` 键 = MagicType 名 → 阶段 `Start/Travel/End/Duration` → `[{sound, gate}]`,
    gate ∈ `Always/Locations/Targets/LocationsOrTargets`;
  - `monster` 键 = MonsterImage 名, 值 `{attack, struck, die}` (SoundIndex 名)。
- 技能无显式条目时按 `{magic}{phase}` 名回退解析 (规则见 `_meta.fallback`)。
- 改后启动 Godot 确认 `[DataLayer] sounds: N / N / N` 且无 PrintErr。

### frame-formulas.json — 禁止手改 (单向镜像)
- 本文件由 `frameformulas.py` 从 C# 源提取 (`FrameSet.cs` 570 条 `new Frame` 硬编码保持原样,
  共享库零改动); 运行时 DataLayer 清空 FrameSet 静态字典并用本 JSON 重填, JSON↔C# 全等靠门禁锁。
- 手改会被 `--check` drift 门禁拦下。**要改动画帧: 改 C# 源 → 重跑生成器** (命令见下), 不要碰 JSON。

## 常用命令

在 `~/development/Mir3-Research` 下执行 (`MIR3_ZIRCON_ROOT` 未设时默认解析 `../zircon`):

```bash
# 特效表门禁 — 文件层: godot 段三元组 vs 原版段 + 白名单口径
python3 Tools/magiclab/gen_cs_table.py --check --skip-runtime

# 特效表门禁 — 文件层 + 运行时层: headless Godot 导出运行中的表, 与 JSON 逐字段全等
python3 Tools/magiclab/gen_cs_table.py --check

# 帧表 drift 门禁: frame-formulas.json 与 C# 源一致
python3 Tools/resedit/frameformulas.py --check

# 改 C# 后重新生成 frame-formulas.json (写回本目录)
python3 Tools/resedit/frameformulas.py

# 无头导出运行中客户端全部表 (等价性取证, 与 e5-proof 同口径; 需 godot-mono + Xvfb :77)
cd ~/development/zircon && DISPLAY=:77 godot-mono --path GodotClient --headless \
  res://Scenes/MapTestScene.tscn -- --table-snapshot=/tmp/snap.json
```

## 溯源与存证

- 每份 JSON 头部自带 `_meta`: schema 版本 / 生成器 / generated_at / sources (来源文件与 commit)。
  magic-effects 另有 `acceptable` 可接受差异清单; sounds 另有 `fallback` 回退规则与 gates/deadRefs。
- `_meta/` 目录: 改造前取证快照, **不参与运行时装载, 仅对账用** —
  `godot-table.json` (原 MagicEffectTable.cs 硬编码表镜像), `original-effects.json` (原版特效提取)。
- 等价性存证: `Mir3-Research/docs/editor/e5-proof/` — snapshot-before/after/cutover.json (改造前后全等),
  coverage-A.md (三组覆盖率 100% 对账), 审计日志与 C 阶段截图。

## 字段速查

| 字段 | 出现在 | 释义 |
|---|---|---|
| `frame` / `startIndex` | original.effects / godot 段 | 资源库内起始帧号 |
| `count` / `frameCount` | 同上 | 帧数 |
| `delayMs` | 同上 + frameSets | 每帧延迟 (毫秒) |
| `lib` / `file` | original.effects / godot 段 | 资源库名 (LibraryFile 枚举, 如 Magic); sounds 里指 wav 文件名 |
| `colour` | 同上 | 色调染: MagicEffectTable 静态色名 (Fire/Ice/…) 或 RGBA 数组 |
| `kind` | original.effects | 条目形态 (effect / projectile) |
| `target` | original.effects | 作用目标 (this / point / target) |
| `ctx` | original.effects | 上下文标记 (如 `arrival` = 弹道落地后触发) |
| `segment` | original.effects | 特效分段 (castEffect / projectile / hitEffect) |
| `gate` | sounds.magic | 播放门控 (Always / Locations / Targets / LocationsOrTargets) |
| `start` / `offset` / `ms` | frameSets | 起始帧 / 方向帧偏移基数 / 帧时长 (ms) |
