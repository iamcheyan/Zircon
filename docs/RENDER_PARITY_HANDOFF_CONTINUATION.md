# Godot / 旧版客户端贴图与动画对比任务交接

更新时间：2026-08-08

## 1. 原始目标

持续完成 Godot 客户端与旧版 Windows 客户端之间的全面渲染对齐，范围包括：

- 所有 ZL/ZL2 贴图的 Alpha、颜色键、黑色像素、Shadow、Overlay；
- 地图、中层、前景、地图动画、地图对象和绘制层级；
- 玩家、职业、装备、武器、盾牌、头盔、翅膀、坐骑和怪物附加层；
- 技能、投射物、链条、绳索、爆炸、命中特效和光照；
- 雨、雪、雾、闪电等天气粒子的贴图、定位、缩放和播放时序；
- UI、纸娃娃、窗口阴影、按钮状态、2 倍缩放、Offset 和点击区域；
- 最终要有详细对比文档，并在可验证范围内修正代码。

这个目标目前仍未完成，不能调用 `update_goal complete`。原因不是主要代码路径没有检查，而是当前环境无法启动旧版 Windows 客户端，缺少旧版与 Godot 同场景的最终窗口级截图 A/B 证据。

## 2. 当前工作树与环境

工作目录：

```text
/home/tetsuya/development/Zircon
```

Godot：

```text
/home/tetsuya/.local/bin/godot-mono
Godot 4.6.3 Mono
```

编译命令：

```bash
dotnet build GodotClient/ZirconClient.csproj --no-restore
```

隔离运行环境通常使用：

```bash
XDG_CONFIG_HOME=/tmp/zircon-godot-config \
XDG_CACHE_HOME=/tmp/zircon-godot-cache \
XDG_DATA_HOME=/tmp/zircon-godot-data
```

当前工作树本来就有大量用户改动，不能执行 `git reset --hard`、`git checkout --`、清理 `.godot` 或删除未跟踪文件。主要涉及 Godot Controls/Scripts、BotRunner、Tools 和多个文档；这些改动不能当作本任务新建的独立补丁来回滚。

## 3. 已完成的资源级结论

### 3.1 ZL 解码与像素数据

旧版独立读取器 `ZlPixelReference` 已对照 Godot 的 `ZlReader`，覆盖主图、Shadow、Overlay、DXT/BC7 payload。现有 P-001 记录的全量资源比较结果：

- 312 个去重图库；
- 1,483,776 个主图帧；
- 244,866 个 Shadow/Overlay 层；
- 总计 1,728,642 次 BGRA 比较；
- `different=0`，全部 PASS。

这证明“Godot 解码出来的原始像素与旧版 payload 解码不同”不是当前主要问题。它不等于最终客户端绘制结果已经逐像素一致，因为绘制层、Blend、父节点缩放和窗口环境仍可能不同。

### 3.2 普通图与颜色键图必须分开

旧版调用语义已经从源码核实：

- `Particle.Draw` 调用 `DrawBlendCentered(..., ImageType.Image, ...)`；
- `MirEffect.Draw` 的 Blend 分支仍调用 `DrawBlend(..., ImageType.Image)`；
- `MirProjectile` 继承 `MirEffect`，仍是 `ImageType.Image`；
- `MapControl` 的 Middle/Front `DrawBlend` 也明确传 `ImageType.Image`；
- `ExteriorEffectManager` 的外观特效同样传 `ImageType.Image`。

因此：`Blend=true` 不代表黑色颜色键。Godot 正式绘制路径已改为 `GetImageTexture`；`GetEffectTexture`、`GetWeatherTexture`、`GetFogTexture` 只保留给诊断或明确的颜色键场景。不要为了消除截图里的黑色像素，把正式 ImageType.Image 路径换回 keyed cache。

天气 9 帧已经用旧版独立 payload 做 Alpha 对照：

```text
500, 509, 510, 511, 512, 513, 514, 540, 550
全部 alphaMismatch=0
```

代表性数据：

```text
500: legacy/formal transparent=0 visible=256
540: legacy/formal transparent=72538 visible=58534
550: legacy/formal transparent=0 visible=262144
```

对应 keyed 诊断数据明显不同，尤其 540 和 550，不能拿 keyed 预览图判断旧版最终天气效果。

## 4. 已确认并修正的绘制差异

### 4.1 Legacy Normal Blend 数学

旧版 D3D11/DX9 的 `BlendMode.NORMAL` 不是 Add，也不是普通 Mix/source-over。有效形式是：

```text
src.rgb = texel.rgb * texel.a * colour.rgb * colour.a
src.a   = texel.a * colour.a
out     = src * (1 - destination) + destination
```

并且旧版 NORMAL 的 `blendRate` 不额外乘进顶点 Alpha。Godot 已加入：

```text
GodotClient/Shaders/LegacyScreenBlend.gdshader
GodotClient/Scripts/LegacyBlendMaterial.cs
```

正式 Blend 节点已使用该材质，而不是 `blend_add`。重点修正过：

- `MirEffectNode.cs`；
- `MirProjectileNode.cs`；
- `MirLineEffectNode.cs`；
- `BlendLayerNode.cs`；
- `BlendImageLayerNode.cs`；
- `DXImageControl.cs` 的普通/灰度 UI Blend；
- 怪物附加层、玩家/坐骑外观层；
- 地图 Blend 行。

### 4.2 香炉外观双层

旧版 `Client/Models/Player/ExteriorEffectManager.cs` 的四类 Thurible 明确是：

```text
第一层：Draw(Image)
第二层：DrawBlend(Image)
```

Godot 之前两层都套 Blend，现已修正：

- `BlendImageLayerNode.Configure(..., blend)` 支持逐层选择 source-over / Legacy Screen；
- `PlayerRenderer.DrawThurible` 第一层传 `false`，第二层传 `true`；
- 两层仍使用普通 Image Alpha，不使用颜色键。

### 4.3 地图 Middle/Front 动画尺寸分支（最近发现的实际差异）

旧版 `Client/Scenes/Views/MapControl.cs` 的源码条件不是 Middle/Front 完全相同：

- Middle 标准尺寸 `48x32` 或 `96x64`：即使 `MiddleAnimationBlend` 为 true，也走普通 `Draw`；
- Middle 非标准尺寸：按 `MiddleAnimationBlend` 选择 Draw 或 DrawBlend；
- Front 标准/大型尺寸：按 `FrontAnimationBlend` 选择 Draw 或 DrawBlend；
- Front 非标准尺寸：同样按 `FrontAnimationBlend` 选择；
- Middle 标准/大型底边使用贴图自身高度；
- Front 标准/大型底边使用一个 CellHeight，非标准贴图使用自身高度。

Godot `MapView.cs` 已按此修正：

- 新增 `IsCellSized`；
- Middle 标准尺寸 Blend 位不进入 Blend 行；
- Middle 使用自身高度基线；
- Front 保留标准尺寸 Blend，并使用旧版 Front 基线。

这是本轮从旧版源码重新对照后补出的修正，后续代理不要把旧文档中“所有带 Blend 位的地图帧都 Blend”的历史记录当作当前规则。

### 4.4 天气参数、时序和 2 倍缩放

`MapWeatherLayer.cs` 已按旧版以下文件对照：

```text
Client/Models/Particles/Weather/Rain.cs
Client/Models/Particles/Weather/Snow.cs
Client/Models/Particles/Weather/Fog.cs
Client/Models/Particles/Weather/Lightning.cs
Client/Models/Particles/Particle.cs
```

已对齐的关键点：

- 雨：509，10ms 生成，509→510..514 水花序列，每帧 100ms；
- 雪：500，20ms 生成，落地后停止旋转、缩放消融；
- 雾：550，4 个，Scale=4，按 `texture.Width * scale` 横向连接，DarkGray；
- 闪电：540，最多 3 个，1000..5000ms 生成，100..200ms 生命周期，FadeRate=0.1；
- 粒子运动采用旧版 10ms 更新频率的等效 100Hz，而不是直接按 60Hz 缓慢移动；
- 世界节点统一 `WorldScale=2`，天气内部只用逻辑坐标，不重复放大贴图或位置；
- 正式天气使用普通 Image Alpha，keyed 只用于诊断。

### 4.5 其它已处理项目

- `MirProjectileNode` 的 `UseOffSet=false` 已按旧版投射物使用左上角原点，居中只留给 `DrawBlendCentered` 粒子；
- 投射物飞行期间按实时目标 RenderY 更新排序；
- 玩家、装备、头发、头盔、坐骑、翅膀、武器、盾牌的帧映射与 2x 世界坐标已做组合矩阵审计；
- 玩家/怪物/NPC/坐骑/物品 Shadow 按资源 Shadow、当前帧轮廓和旧版 fallback 分层处理；
- UI 使用独立 CanvasLayer 2x 缩放，不能套世界缩放；
- UI `DXImageControl` 的 ForeColour、禁用色、Alpha、灰度和 Blend 入口已修正；
- GreenSludgeBall 的 `MonMagicEx23[2780..2785]` 有效，2786..2799 为空；Godot 保持旧版方向计算和空帧不绘制，不擅自换帧；
- 地图/角色/技能/天气的 nearest 过滤、Offset 和绘制层级已有专项审计。

## 5. 当前验证结果

最近一次编译：

```text
dotnet build GodotClient/ZirconClient.csproj --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

最近一次 `git diff --check`：通过。

最近一次综合 `MapTestScene --render-audit --action-audit` 日志达到以下结果；命令因场景持续运行被 timeout 杀掉，但这些 PASS 行已实际输出，不能把 timeout 本身当成测试失败：

- sample transparency：`libraries=312 frames=3189 transparentFrames=2499 cornerPollution=0`；
- WeatherAudit：9/9，`alphaMismatch=0`；
- LayerOrderAudit：PASS；
- MagicCoverageAudit：`castConfigured=142 attackOnly=11 missingOriginalSpell=0`；
- MagicFrameAudit：142 skills，1 个 GreenSludgeBall 资源异常已明确记录；
- SpellTimingAudit：Combat1 releaseDelay=400ms，total=600ms；
- ProjectileAudit：多次 PASS，最近一次 `samples=44 travel=201.0px`；
- ActionAudit：Walking、Running、HorseWalking、HorseRunning、Combat、RangeAttack、Spell、采集、钓鱼、驯服、隐身、推开、死亡等全部序列 PASS。

新增的全帧 Alpha 审计支持：

```text
--full-texture-audit
--audit-only
--audit-file=<LibraryFile>
--audit-start=<n>
--audit-end=<n>
```

`--audit-only` 是本轮新增的退出开关，防止审计完成后因为 MapTest 场景常驻而只能靠 timeout 判断。已经明确通过的代表性图库：

```text
Interface       282 frames
GameInter       2485 frames
EquipEffect_UI  367 frames
EquipEffect_Part 2809 frames
Magic           1755 frames
MagicEx         792 frames
MonMagic        728 frames
MonMagicEx      670 frames
MonMagicEx23    1249 frames
ProgUse         392 frames
```

这些单库均输出 `PASS mode=full ... cornerPollution=0`。此前文档还记录了多个 EquipEffect、MagicEx、角色、坐骑、武器、怪物和 UI 辅助库的分项终点，但接手代理如果需要最终发布级报告，应重新按当前代码执行并把日志保存下来。刚才被用户中断的 19 个库并行审计不能算完成证据。

## 6. 明确未完成的部分

### 6.1 最重要：旧版窗口级 A/B

旧版客户端工程是 Windows-only：

- `Client/Client.csproj` / `RenderingCore` 使用 `net10.0-windows8.0`、WinForms、SharpDX/SilkVulkan；
- 当前机器为 ARM64 Linux；
- 本机没有可用 Wine；尝试的 Wine/FEX/box64 路径受架构、页大小或固定地址限制；
- 没有找到可直接运行的旧版 exe。

因此下列项目没有真正关闭：

- P-002：地图族同场景原版/Godot A/B；
- P-004：天气、夜晚、黄昏、光照同场景 A/B；
- P-005：生产场景和 UI 窗口 A/B；
- P-007：NPC、商城、交易、角色、任务等窗口 A/B；
- P-008：职业/装备/坐骑/翅膀组合 A/B；
- P-009：技能、投射物、外观 Blend 的最终颜色/边缘 A/B。

这些不是“没有代码检查”，而是缺少旧版实际输出。不能用第三方截图、Godot 自己的截图或 keyed 诊断 PNG 冒充同场景 A/B。

### 6.2 当前显示环境的限制

headless 可以完成资源加载、几何、坐标、帧序列、审计，但不能证明最终屏幕的每个混合像素。需要在能运行窗口的 Linux/Windows 环境重新生成：

- `/tmp/zircon-render-audit.png`；
- `/tmp/zircon-projectile-audit.png`；
- `/tmp/zircon-weather-rain-fog-lightning.png`；
- `/tmp/zircon-game-audit.png`；
- UI 各窗口 2x 截图。

并和旧版同分辨率、同地图、同对象、同天气、同动作帧截图比较。

### 6.3 全帧 Alpha 审计的剩余执行

采样审计已经覆盖 312 个库，关键库已全帧通过；但如果下一代理要提交“所有库全帧通过”的强结论，应继续使用 `--audit-file` 分片，把所有枚举库的日志保存并汇总。刚才被用户打断的并行命令不能纳入汇总。

## 7. 接手后的推荐顺序

1. 先阅读本文件、`docs/IMAGE_ANIMATION_AUDIT.md`、`docs/ORIGINAL_GODOT_PARITY_AUDIT.md`，再查看 `git status`；不要清理用户改动。
2. 编译并跑 `git diff --check`。
3. 用 `--full-texture-audit --audit-only --audit-file=...` 继续未完成的单库全帧分片，保存日志。
4. 重点复核 `MapView.cs` 最新 Middle/Front 尺寸分支，不要回退到旧文档中的统一 Blend 规则。
5. 在 Windows 主机准备旧版客户端，建立固定测试场景：同地图、同坐标、同天气、同玩家方向、同动作帧、同 UI 分辨率。
6. 对旧版和 Godot 分别截图，做同坐标裁剪、Alpha/RGBA 差异图和 Blend 边缘检查。
7. 若旧版截图显示黑色/灰色是有效像素，保留普通 Image Alpha；只有旧版实际透明的区域才允许引入颜色键。
8. 只有所有 A/B 项都有真实证据，且编译、全帧审计、UI/动作/天气回归再次通过后，才考虑关闭 Goal。

## 8. 推荐命令

编译：

```bash
dotnet build GodotClient/ZirconClient.csproj --no-restore
git diff --check
```

天气与动作：

```bash
env XDG_CONFIG_HOME=/tmp/zircon-godot-config \
XDG_CACHE_HOME=/tmp/zircon-godot-cache \
XDG_DATA_HOME=/tmp/zircon-godot-data \
timeout 30s /home/tetsuya/.local/bin/godot-mono \
  --headless --path GodotClient --scene Scenes/MapTestScene.tscn -- \
  --render-audit --action-audit --projectile-audit
```

单库全帧审计：

```bash
env XDG_CONFIG_HOME=/tmp/zircon-godot-config \
XDG_CACHE_HOME=/tmp/zircon-godot-cache \
XDG_DATA_HOME=/tmp/zircon-godot-data \
/home/tetsuya/.local/bin/godot-mono \
  --headless --path GodotClient --scene Scenes/MapTestScene.tscn -- \
  --full-texture-audit --audit-only --audit-file=ProgUse
```

天气资源诊断（仅诊断，不是正式 Alpha 结论）：

```bash
... godot-mono --headless --path GodotClient \
  --scene Scenes/MapTestScene.tscn -- --weather-texture-dump
```

## 9. 重要防误判规则

- 不要把黑色像素自动判成透明；先查旧版调用的 `ImageType`。
- 不要把 `Blend=true` 自动判成颜色键或 Add；旧版 NORMAL 是 Screen Blend。
- 不要把 timeout 当作审计 PASS；必须看到明确的 PASS 行和退出码。
- 不要把 headless 无纹理回读当成最终视觉 A/B。
- 不要把旧的交接文档中的历史记录当作当前代码规则，尤其是地图尺寸 Blend 和 Add/Mix 表述；以当前 `Client/Scenes/Views/MapControl.cs`、`MapView.cs`、`LegacyScreenBlend.gdshader` 为准。
- 不要重置或清理工作树；本仓库存在大量并行任务的用户改动。

