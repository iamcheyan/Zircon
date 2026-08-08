# Godot 原版 1:1 迁移交接文档

更新时间：2026-08-08

## 1. 任务目标

本仓库的长期目标是：把 `Client/` 目录中的原版 WinForms Mir 客户端，在 `GodotClient/` 中完整、可运行、尽量逐像素地迁移；不仅要有“看起来类似”的 UI，还要保持原版的窗口层级、贴图图库/帧、坐标、滚动、分页、按钮交互、网络操作保护、角色/怪物渲染、动画、Blend、阴影和地图绘制行为。

当前目标仍未完成，不能把已有自动化审计的 PASS 误认为“全部 1:1 完成”。自动化审计主要证明结构、几何和操作守卫正确；原版与 Godot 的同场景截图 A/B、实际在线场景和少数资源异常仍需要继续补齐。

## 2. 当前已经完成或显著推进的内容

### 2.1 UI 与窗口框架

已在 Godot 端实现并通过组合审计的主要窗口/系统包括：

- HUD、主面板、职业/等级/属性图标和快捷键 Hint。
- 背包、仓库、角色、伙伴、伙伴仓库、装备格、物品格和物品图标来源。
- 背包 NPC 出售模式：普通/出售模式、出售类型过滤、多选、价格总计、Sell/Sell All、NPCSell 回包解锁。
- NPC 主对话及 22 种 NPC 子面板，包括购买、维修、精炼、精炼取回、伙伴、婚戒、武器制作、配件、打孔、宝石合成、任务等。
- 通信、聊天、聊天选项、技能分页、排行榜、怪物信息、任务日志和任务详情。
- 组队、LFG、行会、商城、寄售、钱包、帮助、命理、钓鱼、驯马、副本查找、角色编辑、自动喝药、配置、快捷键。
- 窗口边框、标题、关闭按钮、Footer、无标题贴图窗口和 `DXWindow` 默认关闭按钮。

最近完成的窗口对照修复：

- 原版标题通常位于 `y=8`，使用约 10px 字体并垂直居中；已经修正行会、聊天选项、角色修改、NPC 商品/维修/收服伙伴、伙伴仓库、任务列表/任务详情、寄售主窗口/寄售确认、背包、仓库和登录辅助窗口。
- `Interface[15]` 关闭按钮不再普遍使用固定 `Size - 30`，已在大量窗口改成按实际贴图宽度计算：`Size.X - close.Size.X - 3`。
- `ChatOptionsDialog` 和 `EditCharacterDialog` 关闭了重复的 `DXWindow` 默认标题层，保留原版背景窗口上的手工标题。
- `DXWindow` 已支持标准窗口自动补关闭按钮和“关闭”提示，并保留无标题浮动窗口的显式例外。
- 打孔目标格改为使用 `LibraryFile.Inventory`，不再错误使用 StoreItem 图库；`DXItemCell.ItemLibraryFile` 支持窗口指定图库。

关键文件：

- `GodotClient/Controls/DXWindow.cs`
- `GodotClient/Controls/LegacyWindowFrame.cs`
- `GodotClient/Controls/DXItemCell.cs`
- `GodotClient/Controls/InventoryDialog.cs`
- `GodotClient/Controls/StorageDialog.cs`
- `GodotClient/Controls/NPCSocketPanels.cs`
- `GodotClient/Controls/NPCSocketDialogs.cs`
- `GodotClient/Controls/NPCQuestDialogs.cs`
- `GodotClient/Controls/ChatOptionsDialog.cs`
- `GodotClient/Controls/EditCharacterDialog.cs`
- `GodotClient/Controls/ConsignmentDialog.cs`

### 2.2 渲染、地图、动画和天气

- 对象影子已修复偶发变成细长直线的问题：怪物/NPC/坐骑现在只有在 Shadow 元数据、纹理尺寸和 payload 可信时直接绘制；异常或空 payload 进入原版 `ShadowType` fallback。
- 玩家影子使用身体/装备轮廓投影；怪物、NPC、坐骑优先使用当前帧 Shadow 通道，并保留原版 fallback。
- 移动、走路、跑步、骑马、攻击、远程、施法、蓄力、采集、钓鱼、驯服、受击、推开、隐身、龙威、死亡等动作已有回归覆盖。
- 地图中层/前景动画的 Blend 规则已按旧版尺寸分支修正：标准尺寸 Middle 即使带 `MiddleAnimationBlend` 也走普通 Draw；Front 标准/大型和非标准尺寸按 `FrontAnimationBlend` 决定是否使用 Normal Screen Blend。
- `MirEffectNode`、`MirLineEffectNode`、`BlendLayerNode`、玩家/坐骑 Blend 层已从错误 Add 改为原版 Normal Screen，并保留原版 Alpha；NORMAL 的 blendRate 不额外乘入顶点 Alpha。
- 平滑移动、调试标签、天气、光照、地图族加载、真实 Vulkan 2x 投射物均已有专项回归。
- 已生成过 `/tmp/zircon-render-audit.png`、`/tmp/ui_test.png` 等临时验证截图；这些不是最终原版 A/B 证据。

关键文件：

- `GodotClient/Scripts/ObjectRenderer.cs`
- `GodotClient/Scripts/PlayerRenderer.cs`
- `GodotClient/Scripts/RenderPrimitives.cs`
- `GodotClient/Scripts/MapTestScene.cs`
- `GodotClient/Scripts/MapTerrainRow.cs`
- `GodotClient/Scripts/MapWeatherLayer.cs`
- `GodotClient/Scripts/MirEffectNode.cs`
- `GodotClient/Scripts/MirLineEffectNode.cs`
- `GodotClient/Scripts/BlendLayerNode.cs`
- `GodotClient/Scripts/BlendImageLayerNode.cs`

### 2.4 特效生命周期（在线实战崩溃修复）

- 修复在线实战 `ObjectDisposedException`（`MirEffectNode._Process` 读已释放目标节点）：目标被 `S.ObjectRemove` 释放后仍播放的一次性特效，现按原版 `MirEffect` 语义用 `IsInstanceValid` 守卫并冻结在最后位置播完；`MirProjectileNode` 按原版 `Target?.CurrentLocation ?? MapTarget` 回退目标格。修复涉及 `MirEffectNode.cs`（`_Process`/`UpdateRenderLayer`/`CurrentRenderY`）与 `MirProjectileNode.cs`（`_Process`）。
- 新增确定性回归审计 `--effect-target-free-audit`（`MapTestScene`）：锚定特效+飞行物→`Free()` 目标→断言特效存活、位置冻结、无异常；去掉守卫即抛 9 次异常（审计敏感），修复后全部 PASS。在线回归（真实 ServerCore+BotRunner，map=1 作战 100s）0 次异常。

### 2.3 网络和操作保护

已补充多处原版操作语义和 observer/无效索引保护，包括：

- 任务接受/完成/放弃。
- 寄售上架、购买确认、下架和数量边界。
- NPC 购买、出售、维修、精炼、配件和打孔操作。
- 物品格点击、Alt 操作、Storage/GuildStorage/PartsStorage 限制。
- 断线幂等、迟到包顺序、自动寻路切图、右键取消、拾取/攻击优先级和物品数量边界。

## 3. 已通过的验证

正确的 Godot 项目文件是 `GodotClient/ZirconClient.csproj`，不是 `GodotClient/GodotClient.csproj`。

### 3.1 编译

```bash
dotnet build GodotClient/ZirconClient.csproj --no-restore -v:minimal
git diff --check
```

最近结果：0 Warning、0 Error，`git diff --check` 通过。

### 3.2 完整 UI 组合审计

测试场景会等待输入，建议使用 `--quit-after`，避免把等待误判成失败：

```bash
timeout 15s /home/tetsuya/.local/bin/godot-mono \
  --headless --quit-after 8 --path GodotClient \
  res://Scenes/UITestScene.tscn -- \
  --ui-audit --npc-audit --communication-audit --magic-audit \
  --ranking-audit --monster-audit --quest-audit --chat-audit \
  --character-audit --storage-audit --minimap-audit --fortune-audit \
  --fishing-audit --companion-audit --group-audit --guild-audit \
  --gamestore-audit --consignment-audit --currency-audit --help-audit \
  --horse-audit --dungeon-audit --edit-character-audit \
  --auto-potion-audit --group-lfg-audit --config-audit \
  --keybind-audit --window-chrome-audit
```

最近结果：上述基础 UI/HUD、NPC 22 模式、通信、技能、排行榜、怪物、任务、聊天、角色、仓库、小地图、命理、钓鱼、伙伴、组队、行会、商城、寄售、钱包、帮助、驯马、副本、角色编辑、自动喝药、LFG、配置、快捷键和窗口边框全部 `PASS`。

注意：`UITestScene` 代码读取的是 `--npc-audit`、`--magic-audit` 等参数，不是 `--ui-npc-audit`。只有 `--ui-audit` 这一项本身带 `ui-` 前缀。

**`--quit-after` 必须放在 `--` 之前**（引擎参数）：放在 `--` 之后会被当作用户参数（`OS.GetCmdlineUserArgs()`），引擎不会退出，进程会无限空转，表现为"PASS 后挂起"。正确形态：

```bash
timeout 120s /home/tetsuya/.local/bin/godot-mono --headless --quit-after 8 --path GodotClient \
  res://Scenes/MapTestScene.tscn -- --network-audit
```

### 3.4 全量回归（2026-08-08 20:20–20:23，全部 exit=0 干净退出）

- headless `MapTestScene`：`--map-audit`（258 文件 / 25,856,732 格 / 186,728 引用）、`--shadow-audit`（110 库 / 698,766 帧 / 2,848 解码非空帧）、`--action-audit --skip-sound-audit`（TransparencyAudit 312 库 3,189 帧 cornerPollution=0、WeatherAudit 9/9、LayerOrderAudit、MagicFrameAudit、SpellTimingAudit）、`--light-audit`、`--player-matrix-audit`（23,552 组合）、`--cursor-audit` 全部 PASS。
- headless `--network-audit`：`[NetworkAudit] PASS` + `[AnomalyReplay] PASS`（0.90s）。
- headless `UITestScene --ui-audit`：11 项 UI 审计全部 PASS（3.0s）。
- desktop Vulkan（DISPLAY=:0）：`--render-audit`（MonsterAudit/ObjectLabelAudit）、`--light-render-audit`（3 档）、`--weather-render-audit`、`--projectile-audit`（21 样本 / 198.4px）、`--map-family-render-audit`（5 族，16.4s）全部 PASS 并干净退出；证据 PNG 已重生成（`/tmp/zircon-*.png` 时间戳 20:23）。
- 生产场景（main 场景自动登录 + `--screenshot-after-enter`）19:38 已 PASS（map=1 / viewport 3024x1964），本轮代码未变，证据有效。

### 3.3 地图/资源/渲染审计

`MapTestScene` 支持的专项参数和已有结果：

```bash
godot-mono --headless --path GodotClient \
  res://Scenes/MapTestScene.tscn -- --shadow-audit
godot-mono --headless --path GodotClient \
  res://Scenes/MapTestScene.tscn -- --pixel-audit --pixel-sample=64
godot-mono --headless --path GodotClient \
  res://Scenes/MapTestScene.tscn -- --render-audit
```

最近 Shadow 结果：`libraries=110 frames=698766 metadata=5268 metadataUsable=5268 decoded=2848 nonEmpty=2848 thinContent=0 longContent=0`，通过。

最近 Pixel 结果：`libraries=312 frames=8071 layers=1211 compared=9282`，通过。

动作审计、地图引用审计、MagicFrame 审计、Projectile Vulkan 2x 审计在前序回归中通过；MagicFrame 中原版资源异常一项（GreenSludgeBall）已核实并闭合，见下文 P-003。

## 4. 明确未完成的事项

完整未闭合清单也在 `docs/ORIGINAL_GODOT_PARITY_AUDIT.md` 的 P-002 至 P-009 中。下列项目不能删除或标成完成，除非取得原版和 Godot 的同场景证据。

### P-002：地图族原版 A/B 证据

**Godot 侧证据已完成（2026-08-08 重生成）**：`--map-family-render-audit` 对城镇/野外/沙漠族 `0/1/5/D001/E01` 五组 Vulkan 2x 截图全部 PASS（viewport 1492x1876），`MapAudit` 258/258、25,856,732 cells、186,728 textureRefs、missingRefs=0、missingLibraries=0、missingTextures=0。截图存 `/tmp/zircon-map-family-{0,1,5,D001,E01}.png`。

**原版侧阻塞（本机不可执行）**：原版客户端是 Windows-only（`Client/` TFM `net10.0-windows8.0` + SharpDX/SilkVulkan/WinForms），本机为 ARM64 Linux。已逐一核实不可行路径：dnf 安装 wine i686/x86_64 依赖解析失败（无 x86_64 mesa-dri-drivers 等外源架构包）；FEX rootfs 自带 wine64 为 stub 且 FEX 运行时 jemalloc 不支持 16K 页；box64 无法映射 wine64 固定低地址。`docs/research/images/17173-map/` 与 `sdo-map/` 有第三方发布的原版游戏截图可作粗略参照，但非 WinForms 客户端同场景截图。**逐地图、逐视口、逐缩放的像素 A/B 需在 Windows 主机运行原版客户端后完成**。

### P-003：GreenSludgeBall 原版资源不一致 —— **已闭合**

结论：这是原版代码与当前资源版本之间的矛盾，不是 Godot 缺陷；Godot 与原版对空帧的行为逐位一致，不需要也不应该改动。

证据：

1. `MonMagicEx23.Zl` 元数据共 2800 条，其中 2786..2799 为空（0 字节 payload），只有 2780..2785（方向 0 命中帧）有有效图像；
2. 原版 `MirLibrary.CheckImage`（`index < Images.Length && Images[index] != null`）与 Godot `_Draw`（越界/空帧跳过）对空帧的处理完全一致，方向 1+ 按旧版逻辑不绘制；
3. `MagicFrameAudit` 输出 `originalResourceExceptions=1 (GreenSludgeBall impact dir0-only verified)`，`--green-sludge-dump` 已导出 2780..2785 PNG 存档（`/tmp/zircon-green-sludge-*.png`）；
4. Godot 命中特效方向已按原版 `action.Direction` 播放（此前误固定为 Up）。

若未来取得包含 2786+ 帧的原版资源版本，再按新资源重开此条目；当前不静默替换为“看起来合理”的帧号。

### P-004：天气/光照原版 A/B

**Godot 侧证据已完成（2026-08-08 重生成）**：`--light-render-audit` 三组截图 `/tmp/zircon-light-{night,twilight,default}.png` 整帧亮度 63.7/99.6/106.6，与 `MapLightLayer.AmbientFor` 0.250/0.392/0.420 单调一致；`--weather-render-audit` `/tmp/zircon-weather-rain-fog-lightning.png` 雨丝与雾层同帧可见；天气正式层已逐像素匹配旧版 `Particle.Draw(... ImageType.Image)` 的 DXT1 Alpha（keyed 缓存仅用于诊断）。

**原版侧阻塞**：同 P-002，原版客户端无法在本机运行（Windows-only + ARM64 无可用 wine），原版同天气/同光照截图不可获取。DXT1 Alpha、雾层、雨层、夜色、透明度和层级的最终原版 A/B 需在 Windows 主机完成。

### P-005：生产 UI 和完整在线场景 A/B

**Godot 侧证据已完成（2026-08-08 19:38 重生成）**：`--screenshot-after-enter` 经真实 `ServerCore`（127.0.0.1:7000）自动登录 test@test.com 进入 `map=1`，生成 `/tmp/zircon-game-audit.png`（viewport 3024x1964，2x UI）。复核：HUD（HP/MP/IP/CL/LV/CP/FP/AC/MAC/DC/SC）、小地图（Bichon Town）、底部动作栏、聊天栏（`[Normal] Bot16: Bot16: 大家好，我叫Bot16。`，inspect_image 对 20:24 重生成截图逐字复核）、玩家角色、守卫 NPC、怪物群、对象血条与阴影同帧可见；顶部深色区经裁图复核为屋顶/墙体/植被真实贴图，非渲染缺陷。

**原版侧阻塞**：同 P-002，原版客户端无法在本机运行。同一角色/同一地图/同一窗口尺寸的原版截图，以及 HUD、背包、角色、技能、任务、NPC、商城、聊天、组队/行会、寄售、配置等逐窗口 UI 的字体字形/描边/贴图边框/层级/遮挡/滚动条/悬停/分页 A/B，需在 Windows 主机完成。

### P-006：网络真实异常序列 —— **已闭合**

`MapTestScene` 新增 `RunAnomalyReplayAudit`（并入 `--network-audit`），用真实 loopback socket 回放异常序列：`TcpListener`+`TcpClient` 包成 `ServerConnection`，本地 `Pump()` 镜像 `NetworkManager._Process` 的同步轮询语义（读可用字节→`Library.Network.Packet.ReceivePacket`→`ReceiveList.Enqueue`→`Connection.Process()`，泵送前 `UpdateTimeOut()` 避免默认 TimeOutTime 误判断线），规避 Godot 异步回调可能不触发的问题。

覆盖场景（全部 PASS，headless 0.90s）：分片半帧暂存后恰好一次投递、乱序合包顺序正确、启动积压包只入队一次、运行态实时派发、切图后迟到重复包不入积压、服务端 FIN 断线事件恰好一次且重复 `NotifyDisconnected` 折叠（`_disconnectNotified`）、垃圾字节卡帧不崩溃不分发。原始包序列、事件计数与最终状态均可在 `--network-audit` 输出中复核。

仍可选的扩展（不阻塞闭合）：用真实在线服务器抓包回放库存回包与 UI 锁定并发、observer 状态切换等业务级序列。

### P-007：复杂 UI/NPC 原版 A/B

**Godot 侧证据已完成**：`UITestScene --ui-audit --quit-after 8` 组合回归 PASS——HUD、NPC 22 模式、通信、技能、排行榜、怪物、任务、聊天、角色、仓库、小地图、命理、钓鱼、伙伴、组队、行会、商城、寄售、钱包、帮助、驯马、副本、角色编辑、自动喝药、LFG、配置、快捷键、窗口边框，以及专项 `UISocketAudit`（打孔/合孔 188x320/192x326 外框、三孔/三宝石动态坐标、21 帧合成动画）、`UIInventorySaleAudit`、`UIBeltPotionAudit`、`UIKeyBindAudit`（双槽位+第二键触发）、`UIWindowChromeAudit`（无边框例外）全部 PASS。

**原版侧阻塞**：同 P-002，原版客户端无法在本机运行。精炼、配件、武器制作、伙伴、打孔/合孔、任务奖励、滚动列表和寄售确认窗口的逐像素 A/B 需在 Windows 主机完成。

### P-008：角色服装/坐骑 A/B

**Godot 侧证据已完成**：`--player-matrix-audit` 4096 组合 PASS；2x `--render-audit` 生成 `/tmp/zircon-render-audit.png`（2026-08-08 重生成），怪物/NPC ZL 对象全身绘制、名称条、血条与逐帧投影阴影同帧可见；`MonsterAudit`/`ObjectLabelAudit` PASS；玩家阴影按原版逐帧 `Height/2 + ShadowOffset` 投影，坐骑按 HorseShape 0–7 保留专用 Shadow（`--player-matrix-audit` 覆盖 royal/blue dragon 等 8 种坐骑形状）。

**原版侧阻塞**：同 P-002，原版客户端无法在本机运行。原版与 Godot 相同装备/方向/动作帧的截图矩阵（性别、职业、武器、衣服、发型、发色、盔甲色、坐骑、Blend、Shadow，尤其翅膀与稀有外观）需在 Windows 主机完成。

### P-009：透明边缘与 Blend 视觉证据

**Godot 侧证据已完成**：Blend 入口已按已核实的原版数学统一（`BlendMode.NORMAL` = Screen Blend `out = src*(1-dst)+dst`、blendRate no-op、`src.rgb = texel.rgb*texel.a*Col.rgb*Col.a`），全部 `LegacyScreenBlend`；`--weather-texture-dump`/`--proguse-dump` 导出普通/keyed 成对 PNG（`/tmp/zircon-proguse-{200..681}-{ordinary,keyed}.png`）供透明边缘检查；透明审计 `libraries=312 frames=3189 cornerPollution=0`（无黑边/白边）；生产截图复核未见透明边缘黑边或颜色相加伪影。

**原版侧阻塞**：同 P-002，原版客户端无法在本机运行。精灵边缘、半透明特效、雨雾、光环、玩家/坐骑叠层的原版/Godot 同特效像素 A/B 需在 Windows 主机完成。

## 5. 建议下一个智能体的工作顺序

1. 先读取本文件和 `docs/ORIGINAL_GODOT_PARITY_AUDIT.md`，再执行 `git status --short`、`git diff --check`，确认当前工作区状态；不要 reset、checkout 或清理 `.godot/`。
2. 先跑 `dotnet build GodotClient/ZirconClient.csproj --no-restore` 和上面的 UI 组合审计，确认交接基线没有被环境并发破坏。
3. 不要重复改已经有 PASS 的布局；P-002/P-004/P-005/P-007/P-008/P-009 的剩余缺口统一是原版 A/B 截图（本机无法运行原版客户端，详见各条目“原版侧阻塞”），需要 Windows 主机；本机侧不要再花时间尝试运行原版客户端。
4. 对每个视觉差异，先在 `Client/Scenes/Views/` 找原版构造函数、`TitleLabel.Location`、`CloseButton.Location`、`ClientArea`、`LibraryFile` 和 `Index`，再改 Godot；不要凭截图猜帧号。
5. 新增窗口时必须在 `UITestScene.cs` 加可重复的几何/交互审计，并使用 `--quit-after 8` 回归。
6. 阴影、Blend、动画、天气的结论必须同时看资源元数据、源码逻辑和截图；只改一种证据来源会重新引入偏差。
7. 每次改动后更新 `docs/ORIGINAL_GODOT_PARITY_AUDIT.md`，明确写“已完成的实现”和“仍缺原版证据”，不能把临时截图当成原版 A/B。

## 6. 工作区和安全注意事项

当前工作区是脏的，且包含用户/前序智能体的改动，不能使用以下破坏性操作：

```bash
git reset --hard
git checkout -- .
rm -rf GodotClient/.godot
```

当前已看到的未跟踪/修改内容包括 BotRunner、Tools、GodotClient、Shader、Blend 材质、地图/天气/渲染、UI、文档等。它们不应被假设全部由本次窗口修复产生。开始新工作前，使用 `git diff -- <具体文件>` 只检查目标文件；不要覆盖无关改动。

Godot Mono 临时目录可能在 Godot 运行时被占用。如果出现 `.godot/mono/temp/...sourcelink.json` 找不到，先结束仍在运行的 Godot 测试进程，再重新运行编译；这不是代码编译错误。

**其他会话的后台审计批处理会持续产出并发进程**：2026-08-08 会话中曾发现前序会话遗留的 `bun omp` 批处理循环（约每 3–5 分钟一个 `--full-texture-audit`/`--ui-audit`/`dotnet build`），表现为：DLL 在审计运行中途被半拷贝覆盖（`BCnEncoder` FileNotFound、autoload 无法实例化）、以及"PASS 后不退出"的假象。开始审计前用 `ps aux | grep -E 'godot-mono|dotnet build'` 确认无并发进程；审计期间若出现上述症状，先排查其他会话的批处理再怀疑代码。

## 7. 参考文档

- `docs/ORIGINAL_GODOT_PARITY_AUDIT.md`：主审计清单、P-002 至 P-009 未闭合项、历史回归记录。
- `docs/IMAGE_ANIMATION_AUDIT.md`：图库、动画、帧和资源覆盖审计。
- `docs/OPERATION_PARITY_TASK.md`：网络操作/功能一致性任务记录。
- `GodotClient/Scripts/UITestScene.cs`：UI 组合审计入口和所有参数名称。
- `GodotClient/Scripts/MapTestScene.cs`：地图、影子、动作、像素和渲染审计入口。
- `Client/Scenes/Views/`：原版窗口源码，所有 1:1 坐标/图库/帧号判断应优先回到这里核对。

交接结论：当前 Godot 客户端已经具备较完整的原版 UI/渲染/操作骨架，所有已登记自动化专项均通过，且 2026-08-08 20:20–20:23 全量回归（headless 地图/影子/动作/光照/玩家矩阵/光标/网络异常回放/11 项 UI + desktop 渲染/光照/天气/投射物/五族地图）全部 exit=0 干净退出。P-003（GreenSludgeBall 资源异常，已核实为原版代码/资源版本矛盾、Godot 行为逐位一致）与 P-006（网络真实异常序列，真实 loopback socket 回放全部 PASS）已闭合；P-002/P-004/P-005/P-007/P-008/P-009 的 Godot 侧截图证据已按当前代码全部重生成并通过复核（`/tmp/zircon-*.png` 共 110 张，含生产在线场景、五族地图、三档光照、雨雾天气、渲染/投射物审计、普通/keyed 成对透明图）。剩余未闭合项统一阻塞于同一事实：原版客户端为 Windows-only（`net10.0-windows8.0` + SharpDX/SilkVulkan），本机 ARM64 Linux 无可用 wine（dnf 外源架构依赖不可解析、FEX jemalloc 16K 页不兼容、box64 无法映射 wine64 低地址），原版同场景截图在本机不可获取。后续智能体应在 Windows 主机运行原版客户端补齐逐窗口/逐地图 A/B 截图后再声明“完整原版 1:1”；在本机继续工作时应保留现有截图回归，不要宣布任务完成。
