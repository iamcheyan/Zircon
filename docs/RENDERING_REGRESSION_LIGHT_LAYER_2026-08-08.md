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

## 根因

最近加入了 `MapLightLayer` 全屏光照合成。该节点使用 `SCREEN_TEXTURE` 读取已经绘制的画面，再通过一个覆盖整个视口的矩形进行环境光计算。

原来的节点顺序是：

1. 地图；
2. 角色、怪物和动态特效；
3. `MapLightLayer` 全屏矩形。

同时，角色和动态对象的 Z 值仍然低于光照层（光照层为 `RenderOrder.FinalEffects + 1`）。在 Godot 的 CanvasItem 绘制流程中，光照 Shader 读取到的屏幕内容并不可靠地包含其后续动态节点。全屏矩形随后覆盖了角色和动态对象，因此造成“图库已加载但人物不见”的假象。

这不是地图 `.map` 文件损坏，也不是 ZL 贴图批量丢失。生产日志确认：

```text
[MapView] 贴图诊断: missingLibraries=0, missingTextures=0
```

角色日志也确认身体、头发、头盔和武器图库均成功加载。

## 修复

在 `GodotClient/Scripts/GameScene.cs` 中，将光照层从所有动态对象之上移到地图底层之后：

```csharp
_lightLayer = new MapLightLayer
{
    ZIndex = RenderOrder.TerrainBase + 1
};
```

修复后的绘制关系为：

1. 地图基础层；
2. `MapLightLayer`；
3. 地形中层和前景；
4. 角色、怪物、NPC、物品；
5. 动态特效和 UI。

这样全屏光照矩形不会再覆盖角色和动态对象。地图建筑、前景和角色继续由各自的渲染节点正常绘制。

## 验证

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

- 使用 `SCREEN_TEXTURE` 的全屏 CanvasItem 不能默认放在角色和动态对象之上；
- 修改渲染层级后，必须运行真实图形截图，而不能只依赖 headless 日志；
- 地图、角色和特效应分别检查“资源是否加载”和“节点是否实际出现在最终画面”两类问题；
- 生产截图审计应保留角色、建筑、怪物和施法特效作为固定检查项；
- 如果以后要让环境光影响角色和特效，应改为统一的最终合成方案，不能简单把全屏 Shader 再移回所有对象之上。

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
