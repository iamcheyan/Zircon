# Zircon 原版客户端/Godot 移植长期 Goal 交接文档

更新时间：2026-08-08

## 0. 交接结论（先读）

当前目标**没有完成**，不能因为编译通过或专项审计通过就宣告完成。

目标是持续对比原版客户端和 Godot 客户端的贴图、动画、输入、战斗、技能、
地图渲染、遮挡、天气、光照、UI 和网络状态，并逐项修复到原版行为。当前已经
完成了大量代码修正和自动化审计，但最重要的“原版客户端与 Godot 同场景、同状态、
同分辨率的截图 A/B”仍然不完整，因此仍需继续。

当前 Codex goal ID：

`019fdbd0-1672-73a2-b5b1-6cf70ebcc9cb`

不要调用 `update_goal(complete)`。除非所有开放项都有原版/Godot 证据并且回归通过，
否则保持 goal active。

## 1. 工作区和重要文件

- Godot 客户端：`GodotClient/`
- 原版客户端：`Client/`
- 原版渲染核心：`RenderingCore/`
- 共享图库/数据：`LibraryCore/`、`Debug/Client/Data/`
- 原版/Godot 差异总审计：`docs/ORIGINAL_GODOT_PARITY_AUDIT.md`
- 图片、动画、透明度审计：`docs/IMAGE_ANIMATION_AUDIT.md`
- 操作、战斗、技能任务：`docs/OPERATION_PARITY_TASK.md`
- 本文：`docs/AGENT_HANDOFF_PARITY_GOAL.md`

工作树本来就是大量未提交修改，均视为用户当前工作，不要 reset、checkout 或清理。
`git status` 中已有的修改不要覆盖。

## 2. 已完成或已确认通过的工作

### 2.1 2 倍缩放和坐标

Godot 世界采用逻辑坐标后统一 `Scale = Vector2.One * 2`，UI 仍按逻辑坐标锚定。
已经修正过地图、对象、角色、技能、UI 的大量 2x 偏移问题。UI 审计包含逻辑锚点、
HUD、物品格、腰带、窗口边框和点击命中。

不要再用“直接把每个 UI 控件的像素坐标乘 2”的方式修复；应保持逻辑坐标，检查根节点
缩放和 viewport/stretch 设置。

### 2.2 地图

- `.map` 基本读取、`.Zl` 读取、地形行绘制已经工作。
- 258 张地图审计通过：
  `files=258 valid=258 layered=258 cells=25856732 textureRefs=186728 emptyRefs=46 ignoredRefs=42 missingRefs=0`
- 地图族 `0/1/5/D001/E01` 的隔离渲染审计通过，缺失图库/贴图为 0。
- 修正了大型贴图基线和单格/大型贴图的 RenderY 参与。
- 修正了 `Middle/FrontAnimationBlend` 不应只对大型贴图生效的问题。
- 地图对象已拆出 `MapTerrainRow`，开始参与全局 Y 排序。

尚未完成的是原版同一地图、同一角色位置、同一镜头的逐像素 A/B，而不是地图文件
能否读取。

### 2.3 对象、动作和遮挡

已覆盖并通过自动审计：

- Walking、Running、HorseWalking、HorseRunning
- Combat、RangeAttack、ShoulderDash、Spell、Channel
- Struck、Pushed、Harvest、Mining、Fishing、Taming
- Cloak、Dragon、Dead
- Monster/NPC/Player/Item 的基础绘制、名称、血条、RenderY
- 玩家、装备、坐骑、怪物/NPC 的阴影 fallback 和当前帧阴影优先逻辑

`MapTestScene` 的动作审计窗口曾有“循环动作恰好回到第 0 帧”的假失败，已经改为
至少 `max(1000ms, frame.Sum+400ms)`，这是测试窗口修复，不是运行时动作改动。

### 2.4 影子和透明度

已经对照：

- `Client/Models/PlayerObject.cs`
- `Client/Models/NPCObject.cs`
- `Client/Models/MonsterObject.cs`
- `Client/Models/MirLibrary.cs`
- `PlayerRenderer.cs`、`ObjectRenderer.cs`

已修复玩家影子基线、装备影子投影、坐骑/NPC/怪物阴影资源异常 fallback，移除了不符合
原版的统一伪造椭圆影子（掉落物不应强行追加椭圆）。ShadowAudit 当前 `thinContent=0`
且 `longContent=0`。

但仍需要原版/Godot 同对象截图确认不同 ShadowType、不同角色、坐骑、怪物和大型对象的
视觉轮廓是否完全一致；自动检查只能证明资源存在和形状没有明显退化。

### 2.5 技能、投射物和施法动作

- 技能覆盖：`castConfigured=142 attackOnly=11 missingOriginalSpell=0`
- `MagicFrameAudit` 通过，仍明确保留 1 个原版资源异常：`GreenSludgeBall` 的原版方向帧
  超出当前 `MonMagicEx23.Zl` 资源范围，不能静默伪造。
- `SpellTimingAudit`：`Combat1` 释放延迟 400ms，总时长 600ms。
- 投射物不再一按就在施法者身上爆炸；直线/目标/落点类型已分开，轨迹审计通过。
- 左键攻击、右键移动/跑步、Shift 强制攻击、目标名称/点击目标链路已修过。
- 施法动画、方向、帧序和非目标/目标投射物已有基础实现。

仍需重点对照实际原版：不同技能的运动类型、发射点、轨迹速度、到达/爆炸时间、
施法者动作、攻击目标锁定和技能结束后的移动恢复。自动轨迹审计只证明“在移动”，
不能证明与原版轨迹完全相同。

### 2.6 天气、光照

- Night/Twilight/Default 光照已有实现。
- 生产天气按 `mapInfo.Weather` 选择。
- Rain/Fog/Lightning 已恢复正式 `ImageType.Image` 路径。
- 9 帧天气审计通过：`500,509,510,511,512,513,514,540,550`，逐像素 Alpha mismatch=0。
- 原版 Fog 使用 `Texture=550`、`DarkGray`、`Scale=4`、4 个链式粒子；截图中的灰色雾块
  不是简单黑色矩形 bug，需要用原版同场景截图判断强度。

仍需完成原版客户端同天气、同地图、同时间的 A/B；尤其是夜晚/黄昏的亮度曲线、雾的
层级、雷电闪烁、天气粒子速度、2x 下的粒子大小和边界。

### 2.7 UI

隔离 UI 组合审计已覆盖并通过：HUD、NPC 22 模式、通信、技能、排行榜、怪物、任务、
聊天、角色、仓库、小地图、命理、钓鱼、伙伴、组队、行会、商城、寄售、钱包、帮助、
驯马、副本、角色编辑、自动喝药、LFG、配置、快捷键和窗口边框。

已经修过很多原版窗口标题、关闭按钮、页签、物品格、滚动条、窗口客户区、配置颜色、
快捷键双键语义、NPC 出售和网络页配置。

UI 仍缺：原版完整生产数据回放、所有窗口的原版/Godot 截图 A/B、字体与字体抗锯齿、
不同窗口缩放下的阴影、颜色持久化的逐色对照。

### 2.8 网络和场景生命周期

NetworkAudit 已覆盖：重复断线、socket 关闭、对象引用清理、攻击/拾取优先级、自动寻路
切图、右键取消、Alt 采集、迟到包/旧包顺序、物品数量和拆分边界。

还没有真实跨连接服务器异常序列的完整录制；需要用实际服务端连接验证登录、选角、进入
游戏、切图、回角色、重连、断线、迟到包等生命周期。

## 3. 本轮刚发现并刚修改、需要优先复核的内容

### 原版 `BlendMode.NORMAL` 不是 Godot Mix

对照原版后发现：

- `RenderingCore/Rendering/SilkD3D11/SilkD3D11RenderingPipeline.cs`（默认管线）
- `RenderingCore/Rendering/SilkVulkan/SilkVulcanRenderingPipeline.cs`

原版 NORMAL 的颜色混合因子为：

`src = InverseDestinationColor`，`dst = One`

也就是 Screen Blend，而不是普通 source-over Mix，也不是简单 Add。之前的 Godot
Add/Mix 会使技能、地图 Blend、UI Blend、装备外观 Blend 的颜色明显不一致。

本轮已加入：

- `GodotClient/Shaders/LegacyScreenBlend.gdshader`
- `GodotClient/Scripts/LegacyBlendMaterial.cs`

并将以下路径切换到这个材质：

- `MirEffectNode`
- `MirProjectileNode`
- `MirLineEffectNode`
- `BlendLayerNode` / `BlendImageLayerNode`
- `MapTerrainRow`（BlendOnly 行）
- `DXImageControl` 的 Blend/灰度 Blend

**已核实并修正的原版有效数学**（SilkD3D11 默认管线与 SilkVulkan 一致；D3D11 在
像素着色器里预乘，Vulkan 对 Bgra32/解压 DXT 在上传时预乘、原生 BC 在着色器里
预乘，有效结果相同）：

```
src.rgb = texel.rgb * texel.a * Col.rgb * Col.a
src.a   = texel.a * Col.a
out     = src * (1 - dst) + dst      // RGB 与 Alpha 两个通道都是 Screen
顶点 Col = (R/255, G/255, B/255, colour.A/255 * _opacity)，_opacity 默认 1F
blendRate 对 NORMAL 是 no-op（AppliesBlendRateToVertexColour 只覆盖
           COLORFY/MASK/EFFECTMASK/LIGHTMAP；NORMAL 混合状态无 BlendFactor）
```

shader 使用 screen texture + `blend_add` 计算 `source*(1-destination)+destination`。
Godot 贴图为直通 RGBA8，因此 RGB 的 `texel.a` 预乘必须在 shader 内显式完成
（`source_rgb = texel.rgb * texel.a * COLOR.rgb * COLOR.a`）。

**本轮修正的 Godot 偏差**（全部对照原版调用链核实）：

1. `LegacyScreenBlend.gdshader`：`source_rgb` 缺少 `texel.a` 预乘（旧实现只乘
   `COLOR.a`，透明边缘会偏暗/漏光）。
2. `MirEffectNode` / `MirProjectileNode`：Blend 路径把 `BlendRate` 乘进顶点
   Alpha（原版 `MirEffect.Draw → DrawBlend → NORMAL` 忽略 blendRate，元素颜色
   不透明 → 顶点 Alpha=1.0 全 Alpha Screen Blend）。
3. `MirLineEffectNode`：Blend 路径 `_opacity * _blendRate`；原版
   `DrawBlendScaled(..., Opacity, ...)` 的 rate 被忽略 → 用 `_opacity`。
4. `MapView.DrawCell` / `MapTerrainRow`：地图 Middle/FrontAnimationBlend 原版是
   `DrawBlend(..., Color.White, false, 0.5F, ...)` —— 0.5F 是**被忽略的**
   blendRate，原版实际是全 Alpha Screen Blend；Godot 原先写成 0.5 Alpha + Mix。
5. `ObjectRenderer.AddBlendLayer`：怪物附加层（InfernalSoldier/NumaHighMage/
   JinamStoneGate/NewMob1）原版 `DrawBlend(..., Color.White, true, 1f, ...)`
   全 Alpha；Godot 原先 0.82 Alpha。
6. `DXImageControl`：UI Blend 原版 `SetBlend(true, ImageOpacity, NORMAL)` 的
   ImageOpacity 是**被忽略的** rate → 顶点 Alpha = ForeColour 全不透明；Godot
   原先把 ImageOpacity 乘进 Alpha。灰度 shader 同步修正为原版
   `gray = dot(texel.rgb,...); out = gray * Col.rgb * texel.a * Col.a`
   （灰度只对直通 texel 计算，不能先乘 COLOR）。

**这一修改已经通过 workspace C# 编译，并通过隔离目录的对象渲染、投射物轨迹审计，
但尚未完成真实 Vulkan 截图验证。下一个智能体必须优先复核这里。**

特别注意：`MirRopeEffectNode` 的非 Blend 路径仍是普通 Mix/无材质，这是正确的；不要把
所有 CanvasItem 都改成 Screen Blend。

## 4. 当前验证命令

工作区编译：

```bash
dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental
git diff --check
```

隔离审计目录曾使用 `/tmp/zircon-audit`，其中 `GodotClient` 是副本，`Debug` 指向工作区
数据。工作区运行 Godot 可能被登录场景/autoload 或其他 Godot 进程干扰，优先使用隔离目录。

常用审计场景：

```bash
godot-mono --path /tmp/zircon-audit/GodotClient --headless \
  --rendering-method gl_compatibility Scenes/MapTestScene.tscn -- --render-audit

godot-mono --path /tmp/zircon-audit/GodotClient --headless \
  --rendering-method gl_compatibility Scenes/MapTestScene.tscn -- --projectile-audit
```

带窗口的 Vulkan 截图审计比 headless 更有价值；历史有效截图：

- `/tmp/zircon-render-audit.png`
- `/tmp/zircon-projectile-audit.png`
- `/tmp/zircon-weather-rain-fog-lightning.png`
- `/tmp/ui_test.png`
- `/tmp/zircon-map-family-0.png` 等

这些测试场景下方出现的灰色/空白区域可能只是 20x20 诊断地图没有铺满 viewport，不能
直接当成生产地图 bug。

## 5. 明确未完成清单（按优先级）

### P0：刚修改的 Screen Blend 真实验证

本轮（2026-08-08 第二次）已核实原版有效数学（SilkD3D11 + SilkVulkan 双后端）并修正
6 处 Godot 偏差（见 §3）：shader 补 `texel.a` 预乘；特效/投射物/链条去掉
BlendRate 乘 Alpha；地图 Blend 去 0.5 Alpha 并改用 Screen 材质；怪物附加层 0.82→1.0；
UI Blend 不再把 ImageOpacity 乘进 Alpha；灰度 shader 对齐原版数学。剩余：

1. 编译后把最新 `GodotClient/Shaders/` 和相关 C# 同步到隔离目录。
2. 用 Vulkan 2x 运行技能、地图动画、UI 光效、装备外观、天气截图。
3. 按已核实的公式验证截图：`out.rgb = texel.rgb*texel.a*COLOR.rgb*COLOR.a*(1-dst.rgb)+dst.rgb`、
   `out.a = texel.a*COLOR.a*(1-dst.a)+dst.a`；blendRate 对 NORMAL 不参与。
4. 如果 Godot screen texture 的采样时机造成递归/上一帧污染，改为合适的 CanvasItem
   shader 或离屏层，并重新验证。
5. 更新 `ORIGINAL_GODOT_PARITY_AUDIT.md` 中旧的“Mix/0.5 Alpha”表述，避免文档自相矛盾。

### P0：技能行为逐类 A/B

建立技能表，至少包含：施法动作、发射点、是否直线、是否目标追踪、是否落点、飞行时间、
爆炸帧、方向、是否阻塞移动、结束后状态。对原版和 Godot 各录制：火球、火符、流星、
地面范围、链条/绳索、跟踪弹、立即落点技能。不能只用“samples/travel PASS”代替。

### P1：人物动作和战斗输入

对照原版实际输入：左键点名字攻击、右键走/跑、Shift 攻击、攻击中移动、技能释放中移动、
目标死亡、目标丢失、切换目标、宠物攻击。检查 action state、方向、帧序、速度和网络回包
是否一致。尤其要记录原版按键时序，而不是只看最终位置。

### P1：影子、RenderY、遮挡

用同一地图放置：树前/树后、墙前/墙后、玩家与怪物交叉、坐骑、掉落物、大型建筑、技能
特效。对比影子脚底锚点、阴影形状、主体覆盖关系、血条/名称层级。确认 2x 下没有把逻辑
坐标重复放大。

### P1：夜晚/黄昏/天气

原版同地图截取 Default、Twilight、Night、Rain、Fog、Lightning；比较色阶、粒子、天气
层级和 UI 是否受环境光影响。避免用 keyed 诊断纹理代替正式 ImageType.Image。

### P2：UI 原版 A/B 和真实数据

逐个打开生产窗口截图，优先配置、角色、伙伴、行会、商城、交易、NPC、技能、背包/仓库、
小地图/大地图。检查字体、阴影、边框、客户区、滚动条、按钮 hover/pressed/disabled、
窗口拖动和 2x 点击命中。

### P2：网络生命周期

录制真实服务端：登录、版本、选角、进入游戏、切图、回角色、断线、重连、重复登录、迟到
包、对象销毁后回包。把原版和 Godot 的包/状态序列写进文档。

## 6. 交接规则

- 不要删除现有用户修改；不要 `git reset --hard`。
- 每改一个渲染路径，必须先查原版对应函数，再改 Godot，再跑编译和专项审计。
- 自动审计 PASS 只能证明测试断言，不代表原版视觉一致；必须在文档中区分“逻辑 PASS”
  和“截图 A/B PASS”。
- 发现资源越界或原版资源异常时，记录为显式 exception，不要用错误方向帧静默替代。
- 每轮都更新本文件和三份已有审计文档，写清命令、截图路径、结果和未决项。
- 只有在所有 P0/P1 和关键 P2 均有证据、没有 confirmed gap 时，才允许结束 goal。

## 7. 当前最后一次已知结果

- workspace `dotnet build`：0 warnings / 0 errors。
- `git diff --check`：通过。
- 隔离对象 RenderAudit：通过。
- 隔离 ProjectileAudit：`PASS samples=46 travel=201.0px`。
- Screen Blend shader：已加入并编译引用，真实 Vulkan 色彩 A/B：未完成。
- Goal：仍 active，未完成。
