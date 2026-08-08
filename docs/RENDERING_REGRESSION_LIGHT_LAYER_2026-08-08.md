# 渲染回归记录：地图、角色和施法效果消失

日期：2026-08-08  
模块：GodotClient 场景渲染、地图光照层  
状态：已修复并验证

## 现象

生产客户端出现了以下组合故障：

- 地图大部分只剩下地面底色，地砖、建筑和前景贴图看起来像没有绘制；
- 自己的角色、其他玩家和部分动态物体消失；
- 角色施法或场景中的动态对象更新后，人物会进一步消失；
- 网络对象仍然正常收到，图库也没有整体加载失败。

## 根因（最终结论）

`MapLightLayer` 使用 `hint_screen_texture`（SCREEN_TEXTURE）读取已绘制的画面。Godot 2D 的整屏拷贝语义是：**同一画布绘制过程中，只在第一个使用 screen_texture 的节点绘制前自动做一次整屏拷贝，后续所有使用它的节点共享这一次拷贝**（官方文档：后续节点不会再有新的拷贝；`renderer_canvas_render_rd.cpp` 中 `material_screen_texture_cached` 是每次 `canvas_render_items`（即每个 CanvasLayer）调用内的局部变量）。

因此拷贝点被"劫持"：任何更早绘制的 screen_texture 用户都会把拷贝时刻固定在它自己的绘制位置，之后才绘制的画面不包含在采样里。劫持源包括：

- 地形 Blend 行（`MapTerrainRow.BlendOnly`，`ZIndex = TerrainMiddle(y) = y*4`，很低）——角色**移动**时新滚动进视口的行；
- 施法/命中特效（`fx.Blend=true`，`ZIndex = ObjectEffect(y) = y*4+3`）——**施法**时。

若光照层仍在世界画布内（哪怕 ZIndex=3401 最大），它采样到的拷贝缺少其后绘制的对象/特效，再用该残缺画面覆盖全屏 → 表现为"移动或施法时所有贴图消失"，静止时（无 blend 行、无特效）光照层是第一个用户 → 亮度正常。旧客户端不存在此问题：LLayer 是在 DrawObjects()（地形+对象+特效）全部完成后的独立全屏合成。

这不是地图 `.map` 文件损坏，也不是 ZL 贴图批量丢失。生产日志确认：

```text
[MapView] 贴图诊断: missingLibraries=0, missingTextures=0
```

角色日志也确认身体、头发、头盔和武器图库均成功加载。

## 修复（最终方案）

把光照层移入**独立 CanvasLayer（Layer=1）**，并在其上挂载 `MapLightLayer`：

```csharp
var lightCanvas = new CanvasLayer
{
    Layer = 1,
    Transform = new Transform2D(0f, Vector2.One * WorldScale, 0f, Vector2.Zero),
};
_lightLayer = new MapLightLayer { ZIndex = RenderOrder.LightOverlay };
lightCanvas.AddChild(_lightLayer);
AddChild(lightCanvas);
```

绘制关系为：

1. 世界画布（默认层）：地图、地形 Blend 行、对象、天气粒子、施法特效——完整绘制；
2. `CanvasLayer(Layer=1)`：光照层在此触发**一次全新的整屏拷贝**，必然采样到完整世界，再叠加环境光与光源光斑；
3. `CanvasLayer(Layer=10)`：UI 窗口。

关键点：

- CanvasLayer 按层索引排序、每层独立渲染，`canvas_render_items` 每层各调用一次 → 拷贝标志每层重置，层内首个 screen_texture 用户（光照层）拿到的是本层绘制点的新拷贝，不受世界画布内低 ZIndex 劫持源影响；
- 世界画布内 Blend 行/特效自身的自动拷贝语义不变（它们只需采样身后地形），不会退化；
- CanvasLayer Transform 用 2x 与根节点 `Scale = WorldScale` 一致，`MapLightLayer._Draw` 仍用逻辑坐标，无需改动；`ZIndex = LightOverlay` 保留层内排序；
- UI（小地图/大地图/窗口）在 Layer=10，位于光照层之上，不受影响（与原版 HUD 在 LLayer 之后一致）。

## 验证（2026-08-08 重做）

真实 Vulkan 视口（Wayland，viewport 2304x1296）`--light-render-audit` 三档全 PASS，新增"拷贝点劫持探针"（低 Z 黑底 + screen_texture 劫持灰块 + 白板）读数与环境光精确一致——证明白板被正确压暗（采样含全部对象）：

```text
[LightRenderAudit] PASS night   ambient=0.250 probe=(0x404040ff) lum=0.251
[LightRenderAudit] PASS twilight ambient=0.392 probe=(0x646464ff) lum=0.392
[LightRenderAudit] PASS default ambient=0.420 probe=(0x6b6b6bff) lum=0.420
```

若光照层仍在世界画布末尾，探针读数会落到黑底（≈0）而 FAIL。headless 回归：

- `[ProjectileAudit] stage0 PASS travel=200.3px duration=198ms≈192ms`、`stage1 PASS pos=(192,64)`；
- `[DeadTargetAudit] PASS dead-target fallback at destCells + corpse anchored parity`；
- `[LayerOrderAudit] PASS`、`[ActionAudit] PASS all action sequences`（27 序列）、`[MagicFrameAudit] PASS skills=142`；
- `dotnet build GodotClient/ZirconClient.csproj --no-incremental`：0 警告、0 错误。

### 中间状态验证（TerrainBase+1 方案，已被上方 CanvasLayer 方案取代）

使用真实服务器和图形后端重新进入 Bichon Town，并生成生产截图：

- 地图地砖和建筑恢复；
- 自己的角色恢复；
- 其他玩家、怪物、NPC 和地面物品恢复；
- 施法中的人物和施法特效同时可见；
- `missingLibraries=0`；
- `missingTextures=0`；
- 生产截图审计通过：

```text
[ProductionScreenshot] PASS map=1 instance=-1 viewport=3024x1964
```

自动化审计结果：

- `MagicFrameAudit PASS skills=142`；
- `ActionAudit PASS all action sequences`；
- `dotnet build GodotClient/ZirconClient.csproj --no-restore`：0 警告、0 错误；
- `git diff --check`：通过。

## 防止回归

- 全屏 `hint_screen_texture` 节点必须放在**独立 CanvasLayer** 中，让每层渲染触发一次新的整屏拷贝；放在世界画布内（无论 ZIndex 多大）都会因首用户劫持拷贝点而采样残缺画面；
- `--light-render-audit` 的"拷贝点劫持探针"（低 Z 黑底 + screen_texture 劫持灰块 + 白板）会断言白板读数 ≈ ambient，作为本回归的固定检查项；任何把光照层移回世界画布的改动都会使其 FAIL；
- 修改渲染层级后，必须运行真实图形截图（`--light-render-audit`、`--render-audit`），不能只依赖 headless 日志；
- 地图、角色和特效应分别检查"资源是否加载"和"节点是否实际出现在最终画面"两类问题；
- 生产截图审计应保留角色、建筑、怪物和施法特效作为固定检查项。

## 后续技能贴图回归：方形背景、偏移和火球残影

### 现象

光照层修复后，地图和角色已经恢复，但部分技能仍会出现：

- 技能帧带出一整块矩形背景，覆盖地面和建筑；
- 投射物或命中特效被下一行地形遮住，只剩边缘、阴影或不完整的火焰；
- 地面目标和角色目标使用了不同的旧客户端锚定规则，却被新客户端混成同一条坐标路径。

### 根因

1. `MirEffectNode` 和 `MirProjectileNode` 使用了 `GetImageTexture()` 的原始解码帧。旧客户端的 `ImageType.Image` 绘制仍会经过图库的颜色键透明处理；Godot 的原始纹理不会自动抠除技能帧背景，因此会出现方形贴图。
2. 投射物曾使用 `100 + renderY` 的旧临时 Z 值，而地图现在使用 `RenderOrder` 的紧凑行排序。两套数值不在同一个排序体系内，导致投射物可能被地形或建筑覆盖。
3. 地面 `MapTarget` 与对象 `Target` 在旧客户端是两个分支。普通 `Impact`（例如火球 580 号爆炸）只应挂在对象目标上；只有显式 `MapImpact` 才应在地面格子完成时播放。
4. 历史提交 `505fa2c` 引入 `LegacyScreenBlend.gdshader` 后，特效节点通过 `SCREEN_TEXTURE` 重新合成画面，并强制输出 `alpha=1`。这会把透明帧的整个矩形变成不透明区域；在当前 2 倍世界缩放下，采样内容还会发生明显错位。

### 修复

- 技能序列帧和投射物改用 `GetEffectTexture()`，执行颜色键透明处理；角色、地图和建筑仍保留 `GetImageTexture()`。
- 投射物对象层改用 `RenderOrder.ObjectEffect(renderY)`，与普通对象特效、地形和建筑共享同一套行排序。
- `SpawnProjectile()` 的地面完成特效只传递 `def.MapImpact`，不再把 `def.Impact` 错误复制到每个地面目标。
- 修正技能节点和投射物上的 `LegacyScreenBlend` 路径：保留旧端的 NORMAL screen 混合，但对完全透明像素直接丢弃，避免 `SCREEN_TEXTURE` 把透明帧写成整块矩形。
- 修正特效颜色来源：旧端 `DrawColour` 默认白色，`FrameLightColour` 仅用于光照；Godot 不再用 `FrameLightColour` 直接染色技能贴图，避免火球被 `OrangeRed` 过度染红。
- 补齐 `MapTestScene` 中未完成的混合审计声明，使项目可以稳定编译。

### 验证

```text
dotnet build GodotClient/ZirconClient.csproj --no-restore -m:1 -nodeReuse:false
Build succeeded. 0 Warning(s), 0 Error(s)

[ProductionScreenshot] PASS map=1 instance=-1 viewport=3024x1964
[ProjectileRenderAudit] PASS viewport=1492x930
[ProjectileAudit] PASS samples=19 travel=198.4px
```

生产截图中已确认矩形技能背景消失，地图、建筑、角色和火焰特效可同时显示。
