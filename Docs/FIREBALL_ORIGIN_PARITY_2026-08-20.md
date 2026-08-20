# 火球术投射起点对照记录

## 结论摘要

原版火球术的逻辑起点不是独立的“掌心坐标”，而是施法者当前地图格 `CurrentLocation`。原版的掌心聚拢动画和飞行火球是两个独立特效：

1. 掌心施法动画：`MirEffect(1820, 8, 70ms, LibraryFile.Magic)`，目标为施法者自身；
2. 飞行火球：`MirProjectile(420, 5, 100ms, LibraryFile.Magic)`，起点为施法者 `CurrentLocation`。

原版源码证据：

- `Client/Models/MapObject.cs:3778-3784`：FireBall 起手 `MirEffect(1820...)`，`Target=this`，方向为施法方向；
- `Client/Models/MapObject.cs:842-859`：FireBall 飞行物 `MirProjectile(420...)`，origin=`CurrentLocation`，地点目标使用 `MapTarget`，对象目标使用 `Target`；
- `Client/Models/MirProjectile.cs:47-54`：轨迹起点由 `Origin`、`MapObject.OffSetX/Y` 和玩家移动偏移计算；
- `Client/Models/MirProjectile.cs:84-91`：飞行位置和粒子轨迹使用火球帧自身的 `OffSet`；
- `Client/Models/MirEffect.cs:41`：原版默认 `UseOffSet=true`。

## 当前 Godot 实现

当前数据和代码与原版的主要语义一致：

- `ClientData/magic-effects.json:3645-3800`：FireBall 的起手帧 1820、飞行帧 420、命中帧 580；
- `ClientData/magic-effects.json:3690-3732`：飞行物 `origin=caster`；
- `GodotClient/Scripts/GameScene.cs:3954-3962`：从 `fromX/fromY` 建立投射物起点；
- `GodotClient/Scripts/MirProjectileNode.cs:67-86`：由起点/目标格计算轨迹；
- `GodotClient/Scripts/MirProjectileNode.cs:203-207`：绘制时应用当前帧 `img.OffSetX/Y`。

因此不能简单把起点硬改成角色“手部坐标”。正确对照目标是：保持 `CurrentLocation` 起点，同时确保原版地图偏移、移动偏移、帧 `OffSetX/Y`、投射物释放时机和绘制基线一致。

## 已确认的风险点

1. `ComputeEffectScreenPos()` 使用 `MapView.CellToScreen(cellX, cellY, false)`；该接口是不带 object baseline 的地图原点。
2. 原版 `MirProjectile` 的轨迹公式显式叠加 `MapObject.OffSetX/Y` 和 `User.MovingOffSet`。
3. 当前 Godot `MirProjectileNode` 使用地图回调得到起点屏幕坐标；需要核对该回调与原版 `OffSetX/Y` 是否完全等价。
4. 当前 `Magic.Zl` 帧偏移已经参与绘制，但需要使用运行时资源核对 `Magic[420]` 的 `OffSetX/Y` 与图像尺寸。
5. 火球释放时机必须保持 `Combat1` 的 release 语义；掌心 1820 特效不能被误当作飞行物起点。

## 修整原则

- 不新增未经原版证据支持的固定“手部坐标”；
- 不改变火球的地图格逻辑起点；
- 优先修正基线/偏移/释放时间；
- 使用原版 `MapObject.cs` 和 `MirProjectile.cs` 作为行为权威；
- 使用 `Magic.Zl` 420 帧的真实偏移做视觉对齐；
- 修复后必须运行 projectile audit 和真实窗口技能烟测。

## 本轮状态

本文档先记录原版证据和当前实现差异。后续修复将以运行时帧偏移与原版投射物位置公式对照为准，不直接猜测手部坐标。

## 原版偏移的进一步确认

`Client/Models/MapObject.cs:338-343` 证明原版 `OffSetX/Y` 是
`MapControl.OffSetX/Y`，不是玩家手部坐标。`MapObject.Process()` 在
`:371-382` 用它们计算地图画布偏移，再叠加 `MovingOffSet` 和 `PixelOffset`。
因此原版 `MirProjectile` 的 origin 仍然是地图格起点；视觉上的“从手里发出”
来自 `Magic.Zl` 帧的 `OffSetX/Y`、图像尺寸、施法动作时序和地图基线共同形成，
不是额外的手部 anchor。

**当前结论**：不能把火球起点盲目改成 `player.Position + 固定手部偏移`；
那会偏离原版。若实际画面仍从身体中心发射，应继续对照 `Magic.Zl` frame 420
的真实 `OffSetX/Y`、`MapView.CellToScreen(false)` 基线和 `Combat1` release
时刻，而不是新增猜测性的手部坐标。

## 已执行对照修正

`MirProjectileNode` 的目标实体终点原先读取 `_targetNode.Position`，
即对象 baseline；原版 `MirProjectile.Process()` 使用
`Target.CurrentLocation` 计算 `x1/y1`。现已改为对 `MapObjectNode`
目标使用 `_cameraFnByCell(target.CellX, target.CellY)`，与原版目标格原点一致。
这修正的是投射物轨迹基线，不是凭猜测新增手部坐标。
