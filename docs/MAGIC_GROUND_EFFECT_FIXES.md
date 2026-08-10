# 魔法地面特效修复记录

## 修复方法概述

本次修复了 Godot 客户端魔法特效配置中三类系统性问题。所有修复都在
`GodotClient/Scripts/MagicEffectTable.cs`（特效配置表）和
`GodotClient/Scripts/MirEffectNode.cs` / `GameScene.cs`（渲染管线）中完成，
不涉及原版源码。

### 问题 A：地面特效被 `CastAtSource` 阻断（6 个技能）

**根因**：原版这些技能的释放特效用 `MapTarget = point`（在每个地面格播放）。
Godot 表把同一帧配成了 `Impact`（只在 targets 循环对目标对象播放）。当
`CastAtSource = true` 时，`RenderObjectMagic` 的 destCells 循环跳过
`SpawnCastEffect` 分支，地面特效完全丢失。

**修复**：把 `Impact` 改成 `MapImpact`（在 destCells 循环地面格播放）。

**受影响技能**：
- FireStorm（火风暴）：950/7 地面火焰
- IceStorm（冰暴）：780/7 地面冰暴
- LightningWave（闪电波）：980/8 地面闪电
- DragonTornado（龙卷风）：1040/16 地面龙卷
- MassHeal（群体治疗）：670/7 地面治疗光环
- ThunderStrike（雷击）：1450/3 同时地面+目标（保留 Impact + 加 MapImpact）

### 问题 B：主 CastEffect 被 `Source` 阻断不在地面播放（4 个技能）

**根因**：原版这些技能有两段：起手 `Target=this`（施法者身上）+ 释放
`MapTarget=point`（地面格）。Godot 把起手配成 `Source`，把释放配成主
`CastEffect`。但 `RenderObjectMagic` 的 destCells 循环里，
`def.Source == null && !def.CastAtSource` 为 false（Source != null），
主 CastEffect 的 `SpawnCastEffect` 被跳过——地面特效不播放。

**修复**：加 `MapImpact` 指向原版地面特效帧，让 destCells 循环走
`SpawnImpact(MapImpact)` 分支。Source 保留（在 `RenderObjectMagicStart`
播放，起手特效正确）。

**受影响技能**：
- ScortchedEarth（焦土）：1900/30 爆发火焰 + 2450/10 地面持续火焰
  - ScortchedEarth 额外修了 2450 漏配（原版 3 个特效，Godot 旧配置只有 2 个）
- ChainLightning（连环闪电）：470/10 地面闪电
- FrozenEarth（冰冻大地）：90/20 地面冰特效
- GreaterFrozenEarth（强化冰冻大地）：90/20 同上

### 问题 C：Dxt1 暗色帧被透明键误删（ScortchedEarth 2450/1900）

**根因**：`ZlReader.GetEffectTexture` 的 `EffectTransparentKeyTolerance=32`
把 RGB 值 ≤32 的 opaque 像素清成透明（模拟原版 colour-key）。但 Dxt1 565
压缩会把暗色火焰/冰系帧的主体压到 RGB≤32，透明键误删主体——只剩 4-5%
灰色残留，看不见。原版 GPU 直接采样 DXT 纹理，不做 RGB 抠除。

**修复**：
1. `MirEffectNode` 加 `UseEffectTransparency` 标志（默认 true 保持现状），
   `_Draw` 根据 it 选 `GetEffectTexture`（有透明键）或 `GetImageTexture`
   （无透明键，仅靠 Dxt1 alpha 通道透明）。
2. `CastEffect` 和 `ImpactDef` 加 `NoColourKey` 字段。
3. `SpawnCastEffect` / `SpawnCastEffectTarget` / `SpawnImpact` /
   `SpawnImpactTarget` 传递 `UseEffectTransparency = !NoColourKey`。
4. ScortchedEarth 的 1900（主 CastEffect）和 2450（Additional ImpactDef）
   设 `NoColourKey = true`。

## 全面审计结果

用脚本扫描原版 `Client/Models/MapObject.cs` 全部 263 处
`MirEffect`/`MirProjectile` 构造，对比 Godot 141 个 CastEffect 配置，
检查两类问题：

1. **主 CastEffect 被阻断**：原版有 `map` MirEffect（非 MirProjectile），
   Godot 主 CastEffect StartIndex 匹配该 map 帧但有 Source/CastAtSource
   且无 MapImpact → 被阻断。
2. **map 特效完全缺失**：原版 map 帧不在 Godot 配置的任何 StartIndex 中。

审计结果：
- 问题 A（CastAtSource + Impact）：6 个，已全部修复。
- 问题 B（Source 阻断主 CastEffect）：4 个（含 ScortchedEarth），已全部修复。
- 问题 C（透明键误删）：目前确认 ScortchedEarth 1900/2450 受影响。
  其他技能的地面特效帧若也有暗色主体被误删，需要逐个验证像素内容
  后设 `NoColourKey = true`。已建立机制，后续发现可快速修复。

## 相关源文件

- `GodotClient/Scripts/MagicEffectTable.cs` — 特效配置表（所有修复的主战场）
- `GodotClient/Scripts/MirEffectNode.cs` — `UseEffectTransparency` 标志 +
  `_Draw` 纹理选择
- `GodotClient/Scripts/GameScene.cs` — `SpawnCastEffect`/`SpawnImpact` 等
  传递 `NoColourKey`；`RenderObjectMagic` destCells 循环逻辑
- `GodotClient/Formats/ZlReader.cs` — `GetEffectTexture`（透明键）vs
  `GetImageTexture`（无透明键）
- `Client/Models/MapObject.cs` — 原版 Spell 分支（只读参考）