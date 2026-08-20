# 施法管线完整对照审计 — 2026-08-21

> 以火球术（FireBall）为标准样本，逐阶段对照原版 C# WinForms 客户端与 Godot 移植版的施法特效管线。

## 总览

| 阶段 | 状态 | 说明 |
|---|---|---|
| 1. 释放时机 | ✅ 一致 | 原版在施法动画播完时释放；Godot 在 SpellAnimEnded 时释放 |
| 2. 掌心聚拢特效位置 | ✅ 一致 | 同一基线 + OffSet(3,-31) |
| 3. 投射物起点 | ✅ 一致 | CurrentLocation + 地图偏移 + MovingOffSet 等价 |
| 4. 投射物终点 | ⚠️ 部分 | 打怪物一致；打其他玩家不一致 |
| 5. 飞行插值 | ✅ 一致 | 线性插值，数学等价 |
| 6. 绘制偏移 | ✅ 一致 | UseOffSet + frame OffSetX/Y，等价 |
| 7. 飞行时长/方向 | ✅ 一致 | Chebyshev 距离 + Direction16 |
| 8. 到达着弹 | ⚠️ 部分 | 目标实体一致；地面落点有差异 |
| 9. 飞行中 Z 排序 | ❌ 不一致 | 原版固定目标行；Godot 插值 |
| 10. 玩家移动偏移 | ✅ 一致 | CameraOffset 数学等价 |

---

## 差异 A（❌ 飞行中 Z 排序）— 影响所有投射物技能

### 原版

`Client/Scenes/Views/MapControl.cs:431-451`：

```csharp
foreach (MapObject ob in Objects)
{
    if (ob.RenderY == y)
        ob.Draw();
}

foreach (MirEffect ob in Effects)
{
    if (ob.DrawType != DrawType.Object) continue;
    if (ob.MapTarget.IsEmpty && ob.Target != null)
    {
        if (ob.Target.RenderY == y && ob.Target != User)
            ob.Draw();
    }
    else if (ob.MapTarget.Y == y)
        ob.Draw();
}
```

原版按地图行循环绘制。投射物在 `DrawType.Object` 分支中，**整个飞行过程固定在目标行**（`Target.RenderY` 或 `MapTarget.Y`）的深度。火球从第一帧就在目标那行的图层，不会与施法者身体产生中途遮挡变化。

### 当前 Godot

`GodotClient/Scripts/MirProjectileNode.cs:118-124`：

```csharp
if (DrawType == EffectLayer.Object)
{
    int renderY = (int)MathF.Round(Mathf.Lerp(Origin.Y, CurrentRenderY, (float)t));
    ZIndex = RenderOrder.ObjectEffect(renderY);
}
```

Godot 的 renderY 从 `Origin.Y`（起点行）插值到 `CurrentRenderY`（目标行），飞行过程中逐帧改变深度。这导致火球在刚生成时处于起点行的深度，与施法者身体在同一行，初始几帧火球被绘制在角色身体之上/之中——视觉上像是从身体里出来的。

### 修复方案

将 Z 排序改为固定在目标行，与原版一致：

```csharp
// 改前：renderY 从起点插值到目标
int renderY = (int)MathF.Round(Mathf.Lerp(Origin.Y, CurrentRenderY, (float)t));

// 改后：固定在目标行
int renderY = CurrentRenderY;
```

---

## 差异 B（⚠️ 投射物终点）— 影响打其他玩家

### 原版

`Client/Models/MirProjectile.cs:38,50-51`：

```csharp
Point location = Target?.CurrentLocation ?? MapTarget;
int x1 = (location.X - User.CurrentLocation.X + MapObject.OffSetX) * CellWidth - User.MovingOffSet.X;
int y1 = (location.Y - User.CurrentLocation.Y + MapObject.OffSetY) * CellHeight - User.MovingOffSet.Y;
```

原版始终使用目标的**地图格坐标**（`CurrentLocation`），不包含目标身体偏移或 objectBaseline。

### 当前 Godot

`GodotClient/Scripts/MirProjectileNode.cs:68-76`：

```csharp
Vector2 targetScreen = (_targetNode != null && IsInstanceValid(_targetNode))
    ? _targetNode is MapObjectNode targetObject
        ? _cameraFnByCell(targetObject.CellX, targetObject.CellY)   // ✅ 打怪物：地图格
        : _targetNode.Position                                       // ❌ 打玩家：身体视觉位置
    : (_target != null && IsInstanceValid(_target))
        ? _cameraFnByCell(_target.CellX, _target.CellY)
        : _cameraFnByCell(_targetCellX, _targetCellY);
```

对 `MapObjectNode` 目标（怪物/NPC）已经修正为地图格坐标。但对 `PlayerRenderer` 目标（其他玩家）仍使用 `_targetNode.Position`，这是角色的身体视觉位置（含移动偏移和 objectBaseline），与原版不一致。

### 修复方案

对所有目标统一使用地图格坐标：

```csharp
Vector2 targetScreen = (_targetNode != null && IsInstanceValid(_targetNode))
    ? _targetNode is MapObjectNode targetObject
        ? _cameraFnByCell(targetObject.CellX, targetObject.CellY)
        : (_targetNode is PlayerRenderer player
            ? _cameraFnByCell(player.CellX, player.CellY)
            : _targetNode.Position)
    : (_target != null && IsInstanceValid(_target))
        ? _cameraFnByCell(_target.CellX, _target.CellY)
        : _cameraFnByCell(_targetCellX, _targetCellY);
```

---

## 差异 C（⚠️ 地面落点着弹）— 影响有 MagicLocations 的投射物

### 原版

`Client/Models/MapObject.cs:843-851`：

```csharp
foreach (Point point in MagicLocations)
{
    Effects.Add(spell = new MirProjectile(420, 5, ..., CurrentLocation, FireballTrail)
    {
        Blend = true,
        MapTarget = point,
    });
    spell.Process();
}
```

地面落点弹道**没有 CompleteAction**。`MirProjectile.cs:96-101` 在 `Target==null && !Explode` 时继续飞行直到出屏，不播放着弹特效。

### 当前 Godot

`GodotClient/Scripts/GameScene.cs:3946-3951`：

```csharp
SpawnProjectileDefinition(proj, fromX, fromY, toX, toY, def.MapImpact ?? def.Impact, ...);
```

`MapImpact ?? Impact` 在 `MapImpact=null` 时回退到 `Impact(580)`，导致地面弹道设置了 CompleteAction，会在落点停下并播放 580 爆炸。原版地面弹道不会这样。

### 修复方案

只在 `MapImpact` 显式非空时才为地面弹道设置 CompleteAction：

```csharp
SpawnProjectileDefinition(proj, fromX, fromY, toX, toY, def.MapImpact, ...);
```

---

## 一致的阶段（详细证据）

### 1. 释放时机

原版：`MapObject.cs:768` 的 release switch 在 `DoNextAction`（`UpdateFrame:612`）中 `frame==FrameCount` 时触发。FireBall 不在 `FrameIndexChanged`（`:5176`）中，确认是动画结束释放，不是帧关键点释放。

Godot：`GameScene.cs:3169` 的 `OnObjectMagic` 先播 1820 起手，然后注册 `SpellAnimEnded` → 释放 420 投射物。`SpellAnimEnded` 在 `PlayerRenderer.ApplyAnimation:267` 离开施法动画时触发，等价于原版的动画结束。

### 2. 掌心聚拢特效（1820）

原版：`MirEffect(1820, 8, 70ms, Magic)`，`Target=this`，`UseOffSet=true`。`MirEffect.Process:162-165` → `DrawX=Target.DrawX`。`MirLibrary.Draw:609-613` → `x+=OffSetX(3), y+=OffSetY(-31)`。

Godot：`SetupTarget(sourceNode)` → `Position=sourceNode.Position`。`_Draw:260-261` → `ox=img.OffSetX(3), oy=img.OffSetY(-31)`。同一基线 + 同一偏移。

### 3. 投射物起点

原版：`MirProjectile.cs:47-48` → `x=(Origin.X-User.X+OffSetX)*CellWidth-User.MovingOffSet.X`。

Godot：`MirProjectileNode.cs:67` → `originScreen=_cameraFnByCell(Origin.X,Origin.Y)` = `CellToScreen(cell,false)`，包含 `CameraOffset`（等价于 `-User.MovingOffSet`）。

### 5. 飞行插值

原版：`DrawX = x + (int)(time.Ticks / (duration / x2))` = `x + (elapsed/duration)*(x1-x)`。

Godot：`Position = originScreen.Lerp(targetScreen, (float)t)` = `origin + (target-origin)*t`。数学等价，Godot 子像素更平滑。

### 6. 绘制偏移

原版：`MirLibrary.Draw:609-613` → `if(useOffSet){x+=OffSetX;y+=OffSetY}` 然后中心化绘制。

Godot：`MirProjectileNode._Draw:205-208` → `destRect=Rect2(OffSetX, OffSetY, w, h)`。等价。

### 7. 飞行时长/方向

原版和 Godot 都使用 `Functions.Direction16`（22.5° 划分）和 `Functions.Distance`（Chebyshev）。`ToLegacyProjectilePoint` 做了 `y/32*48` 等距换算。完全一致。

### 10. 玩家移动偏移

原版：`MirProjectile.cs:47-48` 显式 `-User.MovingOffSet.X/Y`。

Godot：`MapView.CellToScreen` 包含 `CameraOffset`，由 `GameScene` 在 `SendMouseMove` 中设为 `(predicted-from)*CellSize*k` 衰减到 0，数学等价于 `-User.MovingOffSet`。