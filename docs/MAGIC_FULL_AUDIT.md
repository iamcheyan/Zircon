# 全量魔法特效审计与修复

## 审计方法

用脚本扫描原版 `Client/Models/MapObject.cs` 全部 263 处 `MirEffect`/`MirProjectile`
构造（142 个 MagicType），对比 Godot `MagicEffectTable.cs` 的 141 个 CastEffect
配置，逐参数核对 StartIndex、FrameCount、LibraryFile、DelayMs、目标类型
（Target=this / MapTarget=point / Target=attackTarget）、Blend、Opacity、
DrawType、StartDelay、DistanceDelay 等。

## 发现的问题分类与修复

### 问题 A：地面特效被 CastAtSource 阻断（6 个，已修复，前次提交）

原版 `MapTarget=point` 地面特效被配成 `Impact`（目标命中），`CastAtSource=true`
阻断 destCells 循环。改为 `MapImpact`。

- FireStorm, IceStorm, LightningWave, DragonTornado, MassHeal, ThunderStrike

### 问题 B：主 CastEffect 被 Source 阻断不在地面播放（4 个，已修复，前次提交）

原版两段特效（起手 Target=this + 释放 MapTarget=point），Godot 把释放帧配成
主 CastEffect 但设了 Source，destCells 循环跳过 SpawnCastEffect。加 MapImpact。

- ScortchedEarth, ChainLightning, FrozenEarth, GreaterFrozenEarth

### 问题 C：透明键误删暗色帧（已修复，前次提交）

Dxt1 压缩后暗色火焰主体 RGB≤32 被透明键清除。加 NoColourKey 机制。

- ScortchedEarth 1900/2450

### 问题 D：起手特效（Target=this）缺失 Source（9 个，本次修复）

原版 start switch 创建 `Target=this` 起手特效（施法者身上的视觉），Godot
没有配 Source → 起手特效不播放。补上 Source ImpactDef。

| 技能 | 原版起手帧 | 修复 |
|---|---|---|
| CelestialLight | 280/8/MagicEx2 | +Source |
| Chain | 0/7/MagicEx7 (140ms) | +Source |
| ElectricShock | 0/10/Magic (60ms) | +Source |
| HellFire | 1520/15/MagicEx4 (Floor, BlendRate=0.4) | +Source |
| Resurrection | 310/10/MagicEx (60ms) | +Source |
| ScortchedEarth | 1820/8/Magic (DirectionFromCast) | +Source |
| SearingLight | 1190/8/MagicEx3 (70ms) | +Source |
| StrengthOfFaith | 360/10/MagicEx2 | +Source |
| WraithGrip | 1460/15/MagicEx4 (Floor, BlendRate=0.4) | +Source + Impact(1420/14) + Additional(1440/14) |

### 问题 E：近战技能主 CastEffect 帧/图库错误（3 个，本次修复）

Godot 主 CastEffect 用了错误的 StartIndex/FrameCount/LibraryFile，与原版不符。

| 技能 | Godot 旧配置 | 原版正确值 | 修复 |
|---|---|---|---|
| DestructiveSurge | 500/10/Magic | 1420/6/MagicEx2 | 改帧+图库+CastAtSource |
| HalfMoon | 480/8/Magic | 230/6/Magic | 改帧+CastAtSource+DirectionFromCast |
| FlameSplash | 580/10/Magic | 900/8/MagicEx4 | 改帧+图库+CastAtSource |

### 问题 F：MagicCombustion 弹道 StartIndex 错误（本次修复）

原版 `MirProjectile(100, 6, MagicEx7)` 但 Godot 配 StartIndex=0。改为 100。

### 问题 G：WraithGrip 缺失第二目标特效（本次修复）

原版对 AttackTargets 创建两个 MirEffect（1420/14 + 1440/14），Godot 只有主
CastEffect（1420/14）。补 Impact=1420/14 + Additional={1440/14}。

## 未修复项（不在 Godot 表中，原版也只有 Target=this）

以下 11 个技能在原版只有 start switch 的 `Target=this` 特效，release switch
无对应 case。它们的 Godot `_attackTable`（近战攻击表）已有正确配置，
`_table`（施法特效表）无条目——因为它们是被动/切换型近战技能，不通过
ObjectMagic 包触发施法特效：

BladeStorm, CrushingWave, DefensiveBlow, DragonBlood, DragonRise, FrostBite,
None, SeismicSlam, Slaying, Spiritualism, Thrusting

这些不是 bug——它们的视觉效果走 `_attackTable.GetAttack()` 在攻击帧触发，
不走 `MagicEffectTable.Get()` 在施法包触发。

## 审计结论

- 原版 142 个有特效的 MagicType，Godot 表覆盖 141 个（1 个差异：`None`
  在 _attackTable 而非 _table，正确）
- 发现并修复 13 个特效配置问题（本次）+ 10 个前次修复 = 共 23 个修复
- 剩余 11 个仅在 _attackTable 的近战技能无问题
- 无遗漏的特效配置差异