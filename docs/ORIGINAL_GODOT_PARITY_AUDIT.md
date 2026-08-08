# 原版客户端 / Godot 客户端持续一致性审计

本文件是 Zircon 移植的长期对齐任务清单。所有差异必须以原版代码、资源元数据、运行时日志或同场景截图为依据；仅凭“代码看起来相似”不得标记为完成。

## 对照范围

| 领域 | 原版权威入口 | Godot 对应入口 | 状态 |
|---|---|---|---|
| ZL/ZL2 解码、通道、Alpha、颜色键 | `RenderingCore/Library/*`、`RenderingCore/LibraryFormat/*` | `GodotClient/Formats/ZlReader.cs`、`BcnDecoder.cs`、`ZlPixelAudit.cs` | 已完成原版独立读取器与全量逐帧主图/Shadow/Overlay 对照 |
| 地图背景、地面、中层、前景 | `Client/Scenes/Views/MapControl.cs` | `MapView.cs`、`MapTerrainRow.cs` | 已修复中层/前景排序；待多地图验证 |
| 玩家主体、装备、头发、武器、坐骑 | `Client/Models/PlayerObject.cs`、`ExteriorEffectManager.cs` | `PlayerRenderer.cs` | 动作链已覆盖；待逐方向/逐装备抽样 |
| 怪物、NPC、物品、特殊对象 | `MapObject.cs`、`MonsterObject.cs`、`NPCObject.cs`、`ItemObject.cs` | `MapObjectNode.cs`、`ObjectRenderer.cs` | 基础显示和动作已覆盖；待特殊大图与 Shadow 抽样 |
| 普通攻击、职业攻击、受击、推开、死亡 | `PlayerObject.SetFrame/UpdateFrame`、攻击回包 | `CombatController.cs`、`PlayerRenderer.cs`、`MapObjectNode.cs` | 动作审计通过；待网络时序实测 |
| 技能动作、轨迹、落点、Blend、光照 | `MirEffect.cs`、`MirProjectile.cs`、技能表和 `MapControl` | `MagicEffectTable.cs`、`MirEffectNode.cs`、`MirProjectileNode.cs`、`GameScene.cs` | 分类和基础轨迹已迁移；待全部技能逐类验证 |
| 天气、昼夜、局部光照 | `MapControl.UpdateWeather/Light`、`Particles/*` | `MapWeatherLayer.cs`、`MapLightLayer.cs` | 参数已迁移；待真实天气截图和明暗曲线对照 |
| UI 图片、动画、窗口、鼠标命中 | `Client/Controls/*`、`Client/Scenes/Views/*` | `GodotClient/Controls/*`、`GameScene.cs` | 功能覆盖较多；待 2x 逐窗口截图 |
| 网络状态、场景切换、对象生命周期 | `GameScene`、网络包处理 | `GameScene.cs`、`ServerConnection.cs` | 已修复启动/地图切换及断线清理；待真实断线、重复包、迟到包验证 |

## 缩放不变量

- 世界逻辑格固定为 `48x32`。
- 世界根节点统一 `Scale=2`；地图、角色、对象、技能、天气、光照内部只使用逻辑坐标。
- UI 使用独立 CanvasLayer 的 `Transform=2`，不能再乘世界缩放。
- 纹理像素过滤为 nearest；不得在资源尺寸、粒子位置、光照半径和技能轨迹中重复乘 2。
- 鼠标屏幕坐标必须通过全局 Canvas 变换还原到逻辑世界后再做格子判断。

## 当前已确认并已修复

### 地图与遮挡

- 地图中层和前景此前在同一 CanvasItem，角色无法被前景正确遮挡。
- 当前拆为两组 `MapTerrainRow`：中层 `99+CellY`、对象 `100+RenderY`、前景 `101+CellY`。
- `MapTestScene --render-audit` 已启用真实世界 2x，地图、对象、标签和像素过滤均已截图检查。

### 阴影与对象基线

- 玩家普通状态使用当前身体轮廓的斜切压扁投影；坐骑保留专用 Shadow；掉落物按原版只绘制主体。
- 怪物/NPC 只使用当前帧 Shadow 通道；旧 ZL 的 Shadow payload 不可用时按 `ShadowType` 49/50/176/177 从主体帧生成原版投影，未知类型保持无影；龙卷风/城门等原版无影对象不再额外画阴影。
- 已增加全图库 Shadow 审计：相关 110 个图库、698,766 帧中 5,268 帧带 Shadow 元数据；发现 2,420 帧是全透明占位 Shadow，已改为缓存为空并在渲染端恢复原版 ShadowType fallback。
- 玩家阴影锚点已从错误的统一椭圆改为原版逐帧 `Height/2 + ShadowOffset` 投影；怪物/NPC 使用资源 Shadow 的原始偏移，并跟随大型对象纵向偏移。

### 动作和输入

- Walking、Running、HorseWalking、HorseRunning、普通攻击、远程攻击、职业攻击、施法、蓄力、受击、推开、采集、钓鱼、驯服、隐身、龙反弹和死亡已有自动动作审计。
- 2x `MapTestScene --action-audit` 已通过 27 条动作记录；同一审计现在还会输出 `MagicCoverageAudit`，区分施法轨迹、攻击命中特效和尚未映射的 MagicType，避免把攻击特效误算成施法轨迹覆盖。
- 左键目标、右键跑步、Shift 原地攻击、远程攻击和目标接近逻辑已有对应入口。

### 特效透明和缩放

- 主体/地图/装备使用普通纹理缓存。
- 技能、怪物附加效果、玩家外观效果和天气使用特效/天气透明键缓存。
- 投射物飞行期间按实时目标 RenderY 更新排序。

## 已确认但仍需继续处理的差异

| 编号 | 差异 | 证据/风险 | 下一步 |
|---|---|---|---|
| P-001 | 已闭合：原版独立 payload 读取器与 Godot 解码路径的全量主图/Shadow/Overlay 逐像素差异 | 8 个可复现批次覆盖 312 个去重图库、1,483,776 个主图帧、244,866 个 Shadow/Overlay 层，合计 1,728,642 次比较，全部 `PASS` 且 `different=0` | 保留批次日志；后续资源变更时重新执行批次审计 |
| P-002 | 全部 258 张地图的结构、分层和地形资源引用已审计通过，但仍缺少不同地形族边界的逐图 Vulkan 截图 | 元数据引用已无 missingRefs；不同地形族的大型建筑/洞穴基线仍需像素画面确认 | 抽样城镇、森林、沙漠、雪地、洞穴地图边界并生成 2x 截图 |
| P-003 | 技能表已按轨迹分类，已增加投射物运动和 ZL 帧范围审计；当前仍有 `GreenSludgeBall` 原版命中帧 `2780 + Direction*10` 超出现有 `MonMagicEx23.Zl` 的 2800 帧边界 | 原版代码与现有资源元数据不一致，不能凭猜测改方向 | 核对原版完整 `MonMagicEx23` 资源版本/方向表后修正，暂不静默替换为错误帧 |
| P-004 | 光照已完成 Night/Twilight/Default 同场景 2x 截图；天气已生成固定 `RainFogLightning` 截图，但仍需与原版同天气截图做最终 A/B | Godot 天气截图已确认雨滴存在、没有黑色颜色键矩形；原版源码确认 Fog 使用 DarkGray/Normal 混合，仍需最终同场景视觉比对 | 保留三组光照截图和天气截图，继续补原版同场景截图后关闭 |
| P-005 | UI/RenderAudit 已可在桌面 Vulkan 窗口生成 2x 截图，但尚未覆盖所有生产窗口和完整在线游戏场景 | 测试场景截图已证明实际 CanvasLayer/世界缩放锚点；生产自动登录截图已覆盖真实 GameScene/HUD/对象/特效，但仍不能替代逐窗口原版截图 | 保留 `UITestScene`、`MapTestScene` 和 `--screenshot-after-enter` 真实 PNG 回归，并继续补复杂窗口/在线场景截图 |
| P-006 | 网络对象生命周期已统一断线通知和 socket 清理；自动寻路移动/切图语义已补齐；运行态包只派发一次，切图会清理旧世界积压包 | `NetworkAudit` 已覆盖重复断线、移除目标引用、自动寻路切图/暂存策略，以及切图前积压包、切图包、迟到移动包的顺序回放 | 仍需可用服务端抓取并回放跨连接异常序列 |
| P-007 | 复杂 NPC、商城、交易、精炼和角色窗口已有构造/尺寸审计，但尚未逐项与原版截图对照 | 22 种 NPC 模式和通信页已验证正尺寸，仍不能证明贴图索引、滚动区域和按钮状态像素一致 | 建立 UI 控件索引和尺寸快照，并补原版/Godot 逐窗口 2x A/B |
| P-008 | 玩家各职业、武器、装备、头盔、翅膀、坐骑组合的原版/Godot 逐截图 A/B 仍未完成 | 原版 `UpdateLibraries`、`ArmourShift`、HorseShape 0–7 已对照；Godot 组合矩阵已覆盖 23,552 个性别/职业/装备/方向/动作/坐骑分支，未发现图库缺失或帧越界；最终外观仍需逐职业原版截图 | 保留 `--player-matrix-audit` 和 2x `--render-audit`，继续补原版/Godot 同场景 A/B，尤其翅膀与稀有外观 |
| P-009 | 原版 `DrawBlend` 使用 `DrawTextureBlend` 的 `BlendMode.NORMAL`，按 blend rate 进行颜色/Alpha 混合；此前 Godot 曾把主体和附加层混在一起 | 火焰、雷电、旋风等效果会错误影响主体，或因透明键/层顺序变灰 | Godot 已拆分主体与附加层并使用 Mix 材质；仍需技能截图复核颜色和透明边缘 |

## 每一项的完成标准

一项差异只有同时满足以下条件才能标记完成：

1. 找到原版对应代码或资源元数据。
2. 找到 Godot 对应实现并说明坐标、帧序、状态和缩放映射。
3. 修复实现并增加自动断言或可重复测试。
4. 在 2x 世界或 2x UI 环境完成截图/像素/日志验证。
5. 编译通过、`git diff --check` 通过，并在本文件记录证据。

## 验证命令

```text
dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental
godot-mono --path GodotClient/ --scene Scenes/MapTestScene.tscn -- --render-audit
godot-mono --headless --path GodotClient/ --scene Scenes/MapTestScene.tscn -- --action-audit
godot-mono --path GodotClient/ --scene Scenes/MapTestScene.tscn -- --shadow-audit
godot-mono --path GodotClient/ --scene Scenes/MapTestScene.tscn -- --projectile-audit
godot-mono --headless --path GodotClient/ --scene Scenes/MapTestScene.tscn -- --light-audit --network-audit
godot-mono --headless --path GodotClient/ --scene Scenes/MapTestScene.tscn -- --pixel-audit --pixel-batch=0/8  # 0/8 至 7/8 全量批次
godot-mono --path GodotClient/ --scene Scenes/UITestScene.tscn -- --ui-audit
git diff --check
```

## 长期任务状态

- [x] 建立原版/Godot 对照入口
- [x] 建立 2x 缩放不变量
- [x] 修复地图中层/对象/前景基本遮挡顺序
- [x] 建立对象和动作自动审计
- [x] 完成资源帧级像素差异工具及全量资源扫描
- [ ] 完成多地图地形和大图对象对照
- [ ] 完成全部技能轨迹/帧链对照
- [ ] 完成天气、昼夜、局部光照截图对照
- [ ] 完成 UI 窗口和 2x 锚点快照
- [ ] 完成网络状态和场景生命周期回放
- [ ] 清空所有已确认差异

## 本轮修复记录

- 2026-08-08：对照 `RenderingCore/Library/MirLibrary.DrawBlend` 与 Godot 特效绘制，确认原版是 `BlendMode.NORMAL` 下按 blend rate 混合，不是 Add/亮化混合；Godot 已使用 Mix 材质并将附加层从主体拆开。
- 2026-08-08：`MirEffectNode`、`MirProjectileNode` 和怪物附加层分别维护透明混合层，避免主体被错误改变；颜色/Alpha 仍以原版 blend rate 为准。
- 2026-08-08：对照原版 `PlayerObject.DrawBody` 的 scratch bounds，确认玩家阴影必须包含当前可见身体、武器、盾牌、头盔/头发，而不是只投影身体；`PlayerRenderer` 已改为逐可见装备层共同投影到同一脚底锚点。
- 2026-08-08：对照原版 `MonsterObject.DrawShadow`，修正怪物/NPC 阴影顺序为 Shadow 通道优先、主体轮廓兜底；LobsterLord 合并三层 Shadow，DustDevil/Tornado/SabukGate 保持原版不绘制普通阴影。
- 2026-08-08：对照原版 `MapControl.UpdateAmbientLight` 修正 `MapLightLayer` 的环境光档位：Night=`15/255`、Twilight=`100/255`、Light=`1`；此前 Godot Night 被错误抬高到黄昏亮度。
- 2026-08-08：2x `MagicCoverageAudit` 对照原版 `MapObject.SetAction` 的 `MirAction.Spell` 集合：`castConfigured=142`、`attackOnly=11`、`missingOriginalSpell=0`、`noMapEffect=67`。后 67 项被记录为原版无地图特效、Buff/Effect 包或动作关键帧处理，不再误报为主动施法缺失。
- 2026-08-08：新增 2x `ProjectileAudit`，使用原版 FireBall 资源从 `(0,0)` 飞到 `(4,2)`，验证投射物持续位移并在到达后结束；结果 `PASS samples=17 travel=202.4px`。
- 2026-08-08：补充 `--projectile-audit` 的真实 2x Vulkan 截图回归，生成 `/tmp/zircon-projectile-audit.png`（`1492x1940`）；截图中的飞行帧可见，日志同时输出 `ProjectileRenderAudit PASS` 与 `ProjectileAudit PASS`。截图下部灰色区域属于诊断场景未覆盖的区域，不纳入生产地图结论。
- 2026-08-08：修复特效透明键的 DXT 压缩边缘：`ProgUse` 第 594 帧此前四角近黑像素被审计为不透明；扩展四角连通背景清理到所有特效缓存后，2x `TransparencyAudit` 变为 `cornerPollution=0`，天气帧仍保持透明/可见像素同时存在。
- 2026-08-08：修复启动地图竞态：`StartGame` 成功后优先等待 `MapChanged`，兜底延迟从 0.5 秒调整为 2 秒，避免过期 `StartInformation.MapIndex` 先渲染成错误场景。
- 2026-08-08：`MapTestScene` 审计地形也改用生产排序（背景 `90+y`、中层 `99+y`、对象 `100+RenderY`、前景 `101+y`），2x 截图不再使用创建顺序冒充遮挡验证。
- 2026-08-08：新增 `--map-audit`，逐个解析 `Debug/Client/Map` 下 258 个 `.map` 文件并检查尺寸、单元格和图层；结果 `files=258 valid=258 layered=258 cells=25856732`。
- 2026-08-08：新增 `UITestScene --ui-audit`，改用与生产 HUD 相同的独立 CanvasLayer `Transform=2`；逻辑窗口/按钮锚点保持原版坐标，输出 `[UIAudit] PASS scale=2 logical anchors preserved`。截图仍受当前运行环境的 viewport/readback 尺寸异常影响，未将其误标为完整像素验收。
- 2026-08-08：修复 `GameScene` 匿名网络事件无法在 `_ExitTree` 解除的问题。交易、NPC、商城、拍卖、伙伴等 29 个匿名处理器现在由 `TrackEvent` 统一登记并在场景退出时解除，避免重复进图后旧场景继续消费网络包。
- 2026-08-08：网络订阅对称性复核又补齐 8 个遗漏的命名处理器解除（邮件、自动寻路、命运检查、钓鱼、驯服、新技能）；静态对照结果为所有非 `TrackEvent` 订阅均有对应 `-=`。
- 2026-08-08：统一 `ServerConnection` 的断线入口：服务器 Disconnect 包、TCP EOF、轮询异常和主动退出都只触发一次 `DisconnectedEvent`，并关闭 TCP；`NetworkManager.Connect` 建立新连接前先释放旧连接，避免旧 socket 和旧场景状态残留。
- 2026-08-08：新增 `--shadow-audit` 全图库影子审计；修正 ZL Shadow payload 全透明时仍被判定为“可用”的问题，空 Shadow 现在交给原版 ShadowType 49/50/176/177 fallback。
- 2026-08-08：新增 `Tools/WtlToZl.py`，按原版 `ImageManager/WTLLibrary` 的 WTL v1.3 块解码规则生成 ZL2/BGRA32；已将缺失的 `MagicEx10.wtl` 转为 `Debug/Client/Data/MagicEx10.Zl`，`MagicFrameAudit` 已消除 `ElementalSwords` 的资源缺口。
- 2026-08-08：Godot 编译通过，0 errors、0 warnings。
- 2026-08-08：聊天设置按原版 `ChatOptionsDialog/ChatOptionsPanel` 拆为每标签独立状态，补齐窗口命名、移除、添加、重置、保存/重新加载、透明/提醒/隐藏标签/倒序/清理/淡出和十类消息过滤；聊天记录尺寸恢复为原版默认 `400x150`。
- 2026-08-08：队伍常驻血条接入当前生命、最大生命、生命变化、怪物资料包，补齐数值文本与比例刷新；小地图迁移的按钮/时间图标逻辑接入 Godot `_Process`，缩放、透明和大地图按钮不再永久隐藏。
- 2026-08-08：NPC 商品/维修界面对照原版补齐行会资金余额校验、行会仓库导入和第二排维修按钮布局；构建、GameScene 启动和 UI 缩放审计继续通过。
- 2026-08-08：大地图补齐原版右键传送戒指与左键双击自动寻路坐标换算；小地图/大地图切图时同步释放旧 NPC/出口静态标记，避免跨地图残留。
- 2026-08-08：商城对照原版 `GameStoreDialog` 补齐排序下拉（名称/最高价/最低价/收藏）、新品/收藏/商品标签/ItemType 分类树、原版商城坐标（左侧货币区、409x432 商品区、174x425 热销区）和分页位置。
- 2026-08-08：商城商品行补齐 1~10 数量选择、购买确认窗口和赠送角色名窗口，并接通 `GameStoreGift` 网络包；仓库整理改为先弹出原版同语义的确认窗口后发送 `ItemSort`。
- 2026-08-08：商城/仓库改动后 `dotnet build GodotClient/ZirconClient.csproj --no-restore` 通过，0 errors、0 warnings；`GameScene` headless 启动通过，`git diff --check` 通过。
- 2026-08-08：交易窗口按原版 `Interface 125` 修正左右 5x2 网格的逻辑坐标、用户/玩家标签居中、金币标签和值分离，并在交易结束/断线时清理用户链接格和对方物品状态。
- 2026-08-08：技能栏对照原版 `MagicBarDialog` 接入 `ShowMagicBarFrames`：带框模式使用 `49/46` 间距和边框资源，无框模式使用 `37/36` 间距、36px 图标和双排高度；设置页新增技能栏边框开关，切换后立即重排快捷栏。
- 2026-08-08：技能栏切换后 `GameScene` headless 启动、`UITestScene --ui-audit` 均通过，后者输出 `[UIAudit] PASS scale=2 logical anchors preserved`。
- 2026-08-08：通过 `MirSkin` 读取原版资源确认角色窗口尺寸：Interface 110/111/112 为 `331x488`，Inspect 115 为 `331x374`；`CharacterDialog` 已按原版尺寸、姓名面板 `(93,51)`、装备大格尺寸、Inspect 背景和底部属性区坐标修正，并移除原版不存在的角色窗口“外观/城镇复活”按钮。
- 2026-08-08：资源尺寸审计确认 Interface 121=`410x479`、125=`428x244`、300=`720x440`、301=`720x332`、310=`800x515`；仓库、交易、寄售行、商城窗口已改为使用对应原版外框尺寸。寄售搜索/我的寄售 6 行列表、滚动条、右上搜索、底部按钮和购买/下架数量确认按源码坐标重排。
- 2026-08-08：继续资源尺寸审计确认 Interface 130=`264x436`、141=`464x372`、200=`296x424`；背包、伙伴、通信窗口同步修正外框尺寸。伙伴装备槽改为原版右侧三槽/左侧食物槽坐标，移除原版不存在的收起/释放按钮；通信内容区和滚动条高度同步到原版外框。
- 2026-08-08：角色页补齐原版底部属性分页（攻击、防御、负重、其他、元素攻击/优势/劣势），数据读取 `PlayerStats`、`WearWeight`、`HandWeight`、`BagWeight`；Inspect 模式隐藏本地属性分页，自己的角色页恢复显示。
- 2026-08-08：社会窗口资源审计确认 Interface 240=`240x424`、260=`456x556`、261–266=`456x440`；队伍窗口高度修正，行会窗口初始创建页使用 260，进入行会/切换主页、成员、仓库、战争、风格页时动态跟随对应 261–265 外框尺寸，并同步滚动区和底部按钮位置。
- 2026-08-08：修正行会页签模型：根窗口始终保留原版 `456x556`，页签只切换背景子图；补齐原版缺失的“城堡”页（Interface 266）及开关城门、修理城门、修理守卫操作和确认窗口。
- 2026-08-08：伙伴窗口按原版 `CompanionDialog` 补齐 Interface 142/143 的加成、过滤、背包侧板；恢复名称/等级/经验/饥饿标签与状态条、7 个等级加成滚动列表、职业/稀有度/物品类型勾选，并将完整过滤列表发送为 `SendCompanionFilters`。
- 2026-08-08：通信窗口补齐原版页签底图 Interface 201–205；写邮件恢复附件栏与金币输入，`MailSend.Gold` 由 Godot 客户端传入，阅读邮件切换到 Interface 205。
- 2026-08-08：寄售行补齐原版 Interface 301/302 页内容底图，并拆出 Interface 303–306 的独立 `ConsignItemDialog`；背包拖入弹窗后可调整价格、确认寄售并携带行会资金选项。
- 2026-08-08：伙伴、通信、寄售改动后 `dotnet build`、Godot editor solution build、GameScene headless 和 `UITestScene --ui-audit` 均通过，0 errors、0 warnings。
- 2026-08-08：任务日志对照原版 `QuestDialog/QuestTab` 重排为左侧任务树与右侧详情栏；补齐任务描述、目标、固定奖励、选择奖励、起止 NPC 和当前任务提交/放弃按钮，隐藏原版默认不可见的“已完成”页签。
- 2026-08-08：任务日志双栏改动后 `dotnet build`、Godot editor solution build、GameScene headless 和 `UITestScene --ui-audit` 均通过。
- 2026-08-08：HUD 主面板任务提示图标按原版 `AvailableQuestIcon.VisibleChanged` 规则重排：可接与已完成提示同时存在时上下错位，不再互相覆盖；工作区现有地图审计代码的透明审计分支也恢复为 RGBA 字节判定，构建重新通过。
- 2026-08-08：任务追踪对照原版 `QuestTrackerDialog` 增加 `TrackingEnabled` 状态；任务详情栏增加“显示任务追踪”开关，刷新任务时尊重开关并同步隐藏/重建追踪列表。
- 2026-08-08：HUD/任务追踪改动后 `dotnet build`、Godot editor solution build、GameScene headless 和 `UITestScene --ui-audit` 均通过，0 errors、0 warnings。
- 2026-08-08：在清理遗留 Godot 审计进程后严格串行重建，`dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental` 通过，0 errors、0 warnings；此前 `DXItemCell.LinkedCount` 报错确认为旧程序集竞争造成的假失败，当前源码与程序集一致。
- 2026-08-08：2x `--render-audit` 通过，真实 Monster/NPC/Player/Item 均完成主体、装备、标签、阴影和 RenderY 绘制诊断；`--projectile-audit` 通过，实测 30 个采样点、位移 197.1px；`--map-audit` 通过，258/258 地图有效、分层有效、单元格 25,856,732。
- 2026-08-08：2x `--action-audit` 通过：透明审计 `libraries=312 frames=3189 cornerPollution=0`，天气 9/9，技能覆盖 `castConfigured=142 attackOnly=11 missingOriginalSpell=0`，`MagicFrameAudit skills=142`，并实际通过 Walking/Running/HorseWalking/HorseRunning、Combat/RangeAttack/Spell、蓄力、采集、钓鱼、驯服、受击、死亡等全部动作链。
- 2026-08-08：2x `--shadow-audit` 通过：`libraries=110 frames=698766 metadata=5268 metadataUsable=5268 decoded=2848 nonEmpty=2848`；`UITestScene --ui-audit` 通过，`scale=2 logical anchors preserved`，窗口资源尺寸和中文字体均可读。
- 2026-08-08：复核原版 `MapControl.UpdateAmbientLight` 后发现 Godot 曾把 Night 错误抬到 Twilight 亮度；已修正 `MapLightLayer` 为 Night=`15/255`、Twilight=`100/255`、Light=`255/255`、Default=`DayTime`，新增 `--light-audit` 并通过。
- 2026-08-08：新增 `--network-audit`，创建本地回环 TCP 连接并连续触发两次主动断线、再次调用 `Disconnect()`；结果 `PASS duplicate disconnect collapsed to one event and transport closed`。这确认 Godot 断线事件只发一次且传输已关闭；迟到包/切图回放仍需独立包序列测试。
- 2026-08-08：修复对象标签与头顶血条错位：原版 `DrawName` 以 48x32 格中心为锚点，Godot 改为局部 `x=24` 并恢复原版垂直公式；怪物/玩家血条改用 Interface 79/80 的 `48x2/48x4` 资源和 `DrawY-55` 坐标；底部 HUD 条增加容器裁剪，避免 MP/FP 填充越界覆盖属性区。`ObjectLabelAudit`、`MonsterAudit`、`RenderAudit`、`UILayoutAudit`、`UIHudAudit` 均通过。
- 2026-08-08：移除 `GameScene` 城镇随机天气测试开关。原版 `MapControl.UpdateWeather` 只读取 `MapInfo.Weather`（并受 `Config.DrawWeather` 控制），Godot 现已不再对地图 0–4 随机注入天气，恢复真实地图天气来源。
- 2026-08-08：补齐原版 `Config.DrawWeather` 行为：设置窗口“显示天气与特效”现在调用 `GameScene.SetDrawWeather`，天气层支持运行时启停，关闭时清空粒子，切图后仍继承开关；编译和 2x `UITestScene --ui-audit` 通过。
- 2026-08-08：逐行对照原版 `SnowParticle.Completed`，修正雪花落地后仍继续旋转的差异；Godot 现在在落地时将 `AngularVelocity` 清零，再按原版缩放/淡出。2x 动作审计中天气 9/9、透明审计、技能帧和全部动作链均通过。
- 2026-08-08：对照原版 `MonsterObject(S.ObjectMonster)`，修正 Godot 丢弃 `Dead=true` 的 `ObjectMonster` 包的问题。现在仍创建死亡对象并播放 `MirAnimation.Dead`，等待后续 `ObjectRemove`，渲染审计新增 `Dead=Die` 断言并通过。
- 2026-08-08：Buff 栏按原版过滤 Ranking/Developer 类型，恢复 CBIcon 图标悬停说明（物品增益、行会、地图/副本效果、剩余时间与暂停状态），保留永久 ItemBuff 合并和倒计时颜色。
- 2026-08-08：本轮最终回归：`MapAudit PASS files=258 valid=258 layered=258 cells=25856732`、`ShadowAudit PASS libraries=110 frames=698766 metadata=5268 metadataUsable=5268 decoded=2848 nonEmpty=2848`。
- 2026-08-08：资源审计确认 `Interface 279=152x260`，菜单窗口移除原版不存在的“称号”项，恢复设置/帮助/行会/仓库/排行/伙伴/退出的原版垂直坐标。
- 2026-08-08：掉落箱对照 `GameInter2 2900=260x296`、`2920/2925/2926/2927=128x20`，重排 5x3 奖励格、状态消息、重抽次数/按钮和领取/确认按钮，保留锁定格、选择格、重抽与服务器确认逻辑。
- 2026-08-08：礼包资源审计确认 `GameInter 3350=180x268`，修正外框、4x4 预览格和领取按钮坐标，补齐 AnyOf/AllOf/OneOf/AutoOpen 行为及源物品锁定。
- 2026-08-08：计时器按原版 `120x100` 重排为纯数字图层，加入 `GameInter 960` 六帧蛋图动画（333ms、按类型显示）并移除原版不存在的键名/文字倒计时。
- 2026-08-08：腰带恢复原版按物品格吸附的横向/纵向布局，支持 1–10 格和边缘拖拽调整；菜单、掉落箱、礼包、计时器、腰带改动后 `dotnet build`、editor solution build、GameScene headless 均通过。
- 2026-08-08：资源审计确认帮助窗口 `GameInter 9300=720x401`、菜单按钮 `9310/9311=134x21`；`HelpDialog` 改为原版外框、左侧主题滚动、选中按钮贴图、右侧页签和正文滚动区。
- 2026-08-08：修正世界 ZIndex 超出 Godot [-4096,4096] 上限的问题；将原版无上限 painter-order 压缩为合法层级，保持地形/物体/效果/玩家/粒子的相对顺序，GameScene 启动不再出现 `set_z_index` 错误。
- 2026-08-08：对照原版 `GreenSludgeBall` 命中分支，修正 Godot 将命中特效方向固定为 Up 的差异，现按 `action.Direction` 播放；由于现有 `MonMagicEx23.Zl` 只有 2800 帧，`MagicFrameAudit` 输出 `PASS skills=142 originalResourceExceptions=1`，资源矛盾仍显式保留。
- 2026-08-08：读取分块全纹理审计结果：`EquipEffect_Full` 10 个区间均无 `FAIL/REVIEW/SUSPECT/EXCEPTION`，所有已解码帧的透明键边缘审计通过；其中空区间明确记录为 `frames=0`，不是解码失败。
- 2026-08-08：排行榜按原版 `RankingDialog` 重排 330x456/576x456 两种外框：顶部职业筛选、仅在线、搜索/观察，固定 11 行列表，3624/3625 在线图标、名次/等级/角色名/排名变化分栏和滚动分页；职业筛选现在携带 `RankRequest.Class`。
- 2026-08-08：Caption 对照 `DXWindow.SetClientSize(325x50)` 和 Interface 通用框架尺寸（0/2/3/10/126），恢复总窗口 343x150、标题/底栏、客户端区标签/输入框/Change/[?] 提示及字符校验坐标。
- 2026-08-08：资源审计确认钓鱼装备窗口 `Interface 220=224x268`、收线窗口 `230=252x144`、钓鱼条 `231/232=216x12/216x8`、指针 `234=16x9`；装备窗口修正为 224x268，收线窗口接入原版鱼条、进度条、移动/抛竿指针、鱼咬钩 4500/4501/4510 图层和自动状态。
- 2026-08-08：驯马资源审计确认 7600/7610 为 80x40、7620 为角度提示、7630/7631 为 76x4/76x6；驯马提示从文字按钮改为原版 10 帧套索动画、角度帧与进度条，继续发送成功回包。
- 2026-08-08：Fortune Checker 按原版 `SetClientSize(485x551)` 恢复 503x597 通用窗口框架、筛选栏、9 行结果滚动区和原版行间距；Dungeon Finder 按 `SetClientSize(560x461)` 恢复 578x507 框架、滚动条、9 行副本列表和关闭按钮坐标。
- 2026-08-08：角色编辑窗口按原版 `Size=260x(650-90)` 恢复 260x560、标题/底栏框架、关闭按钮和底部确认按钮位置；现有性别/发型/染色/名称操作继续沿用服务器请求入口，内容区仍列入后续细节审计。
- 2026-08-08：本轮综合回归通过：`dotnet build` 0 errors/0 warnings、`git diff --check`、GameScene headless（仅保留既有退出时 RID/ObjectDB 泄漏提示）、`UIAudit PASS scale=2`、`MapAudit PASS files=258 valid=258 layered=258 cells=25856732`、`ShadowAudit PASS libraries=110 frames=698766 metadata=5268 metadataUsable=5268 decoded=2848 nonEmpty=2848`。
- 2026-08-08：全局 `DXButton` 对照原版 `DrawGeneratedButtonParts` 增加 Interface 16/17/18 默认按钮、41/42/43 小按钮、53–58 页签和 241/242/243/245 加号/减号/LFG/选项按钮拼接绘制；队伍窗口改用原版三个功能图标和成员/LFG区域布局。
- 2026-08-08：新增 HUD 专项回归：真实 `MainPanel` 按钮使用原版 `(索引, X, Y)` 坐标，`CanvasLayer Transform=2`、逻辑锚点、按钮 `MouseFilter` 和输入事件命中均自动检查；`UITestScene --ui-audit` 输出 `[UIHudAudit] PASS panel=(1024, 68) buttons=9 click=hit`。
- 2026-08-08：通用弹窗继续按原版 `DXWindow.SetClientSize` 对照：掉落过滤恢复 `266x371` 客户区对应的标题窗口总尺寸 `284x429`、10 行过滤输入与 Small 保存按钮；数量窗口恢复 `DXItemCell + AmountBox + HasFooter` 的 `200x46` 客户区结构。
- 2026-08-08：聊天设置窗口恢复原版 `350x250` 客户区对应的标题/底栏外框、左侧 120px 标签列表、右侧 200px 选项区和底部保存/重载/重置按钮，并统一使用原版 Small/Default 按钮拼接皮肤。
- 2026-08-08：Dungeon Finder 补齐原版 Dungeons/Raids 页签、Name/Sort/Search 筛选行、Name/Level/Player Count 排序、9 行滚动列表与 Join Instance SmallButton；Fortune Checker 的 Item 下拉由固定 All 改为可循环 ItemType 过滤并参与搜索。
- 2026-08-08：角色编辑窗口对照原版 `EditCharacterDialog` 补齐 Select Gender 图标按钮（Interface1c 110/111/115/116）、Customization 区、Hair Type/Hair Colour/Armour Colour、Preview 区和 Name 输入；GenderChange 发送当前选中的性别而非固定翻转。
- 2026-08-08：本轮验证：`dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental` 0 errors/0 warnings；`UITestScene --ui-audit` 输出 `[UIAudit] PASS scale=2 logical anchors preserved` 与 `[UIHudAudit] PASS panel=(1024, 68) buttons=9 click=hit`。
- 2026-08-08：资源尺寸自检补齐 Interface 280=`152x240`、281=`252x128`；确认框、商城赠送框和退出框统一改为 281 原版尺寸与 SmallButton，销售历史移除错误的 280 整图，恢复 `270x110` 客户区的通用标题框架。
- 2026-08-08：非增量编译回归发现并清理两处工作区旧移植残留：`Key.ScrollLock` 改为 Godot `Key.Scrolllock`；物品格锁定校验改用现有 `LinkedSourceSlot`，移除不存在的 `CurrencyPickedUp/Observer/Linked/Link` 引用。
- 2026-08-08：窗口尺寸修正后重新验证 `dotnet build` 0 errors/0 warnings；GameScene、MapAudit `258/258`、ShadowAudit `libraries=110 frames=698766 metadataUsable=5268 decoded=2848 nonEmpty=2848`、UIAudit/HUD 命中审计均通过。
- 2026-08-08：对照原版 `ItemObject.Draw`、`NPCObject.DrawShadow`、`MonsterObject.DrawShadow`，移除 Godot 给掉落物/NPC/怪物追加的统一椭圆阴影和主体轮廓兜底。原版掉落物不绘制 Shadow；NPC/怪物只绘制当前 ZL 帧的 Shadow 通道，payload 无效时恢复 ShadowType 49/50/176/177 fallback，避免所有对象退化成同一个错位圆盘。玩家仍保留原版 `DrawShadow2` 的轮廓投影，坐骑仍使用专用 Shadow 通道。
- 2026-08-08：重新推导原版 `PlayerObject.DrawShadow2` 的投影矩阵，移除固定 `(12,16)` 脚底锚点；Godot 现在按每帧 `Height/2 + ShadowOffSetX`、`ShadowOffSetY` 生成斜切压扁影子，并让 2x 世界根节点统一处理缩放。不同体型、装备和动作帧不再共用一个错误影子位置。
- 2026-08-08：移除玩家/坐骑最后的通用几何椭圆兜底；普通玩家按 `DrawShadow2`/身体帧 Shadow，坐骑按专用 Horse Shadow，资源不可用时按原版 ShadowType fallback 或保持无影。渲染回归确认不再由统一圆盘制造伪影。
- 2026-08-08：对照原版 `CConnection.Process(S.ObjectRemove)`，补齐 Godot 对 `TargetObject/MouseObject` 的移除清理；自动攻击在每帧也校验 `IsInstanceValid`，切图清空对象时同步清除战斗引用。`--network-audit` 现在回放重复断线与移除目标引用，确认迟到移除不会继续访问旧节点。
- 2026-08-08：网络生命周期回归通过：`--network-audit` 输出 `duplicate disconnect collapsed, transport closed, removed-object references cleared`；综合 2x headless 回归同时通过 `RenderAudit`、`MapAudit files=258 valid=258 layered=258 cells=25856732`、`ShadowAudit libraries=110 frames=698766 metadata=5268 metadataUsable=5268 decoded=2848 nonEmpty=2848`。
- 2026-08-08：对照原版 `CConnection.Process(S.MapChanged)`，补齐 Godot 对 `InstanceIndex` 的保存：`StartInformation`、启动阶段延迟 MapChanged、正式切图和 `CurrentInstanceInfo` 均保持地图实例一致，不再只切换 MapIndex。
- 2026-08-08：实例状态改动后回归通过：`dotnet build` 0 errors/0 warnings、`MapAudit PASS files=258 valid=258 layered=258 cells=25856732`、`NetworkAudit PASS duplicate disconnect collapsed, transport closed, removed-object references cleared`。
- 2026-08-08：真实桌面 Vulkan 2x 截图验证恢复：非 headless `MapTestScene --render-audit` 成功生成并检查 `/tmp/zircon-render-audit.png`，实际视口 `1492x1940`；`UITestScene --ui-audit` 成功生成并检查 `/tmp/ui_test.png`，实际 PNG `1492x1940`，UIAudit/HUD 命中审计均通过。MapTest 截图下方灰色区域明确是诊断场景只绘制 20x20 地图区域，不代表生产 GameScene。
- 2026-08-08：新增回归参数 `--screenshot-after-enter`：自动登录后等待真实 `StartGame/MapChanged`、地图对象和 2x UI 完成，保存生产 `GameScene` 截图 `/tmp/zircon-game-audit.png` 并自动退出；不改变正常运行流程，用于后续与原版同场景截图对照。
- 2026-08-08：继续对照原版选角页：角色列表恢复 `SelectButton` 的 `280x75`、名称 `(135,8,130,15)`、职业 `(135,28,53,15)`、等级 `(235,28,30,15)`、地图 `(135,48,130,15)` 坐标；创建角色区恢复原版 `HairNumberBox` 位置 `(90,25)`、颜色输入行 `(90,50)/(90,75)`、预览区 `(5,100,190,225)` 和名称输入 `(75,570,155,20)`，并增加按职业/性别切换的预览动画。
- 2026-08-08：对照原版 `NPCCompanionStorageDialog` 的 Interface 147：伙伴预览点修正为 `(55,90)`，名称/等级/经验/饥饿值改为居中字段，经验/饥饿条改用 GameInter 4310/4311 裁剪填充，左右按钮与底部 Store/Retrieve/Release 坐标恢复原版关系；资源审计确认 GameInter 商城控件 `4830/4835/4855/4857=24x22`、翻页 `4840/4845=16x16`、悬停行 `4872=200x78`。
- 2026-08-08：组队邀请和寄售单价输入改为 Godot 客户端的 DXTextInput 皮肤控件，聊天输入栏的频道/选项按钮改为原版 SmallButton 类型；商城商品行继续使用原版 4830/4835/4855/4857/4872 坐标与数量循环。
- 2026-08-08：对照原版 `PlayerObject.DrawBody/DrawHorseShadow` 修正坐骑外观矩阵：蓝龙 `HorseShape=7` 必须使用外观库 `DrawFrame`，不能沿用普通马的 `HorseFrame` 偏移；普通/铁/银/金/暗马的影子使用基础 `HorseLibrary`，皇家/蓝龙才使用外观库影子。新增 `--player-matrix-audit`，覆盖性别 2×职业 4×坐骑外观 8×方向 8×动画 8=`4096` 组合，全部通过。
- 2026-08-08：坐骑矩阵修复后重新生成 2x `--render-audit` 截图 `/tmp/zircon-render-audit.png`（`1492x1940`），日志确认玩家首帧为 `HorseStanding`，编译无警告/错误；灰色下半区仍是诊断场景未绘制区域。
- 2026-08-08：行会风格页移除原生 `ColorPickerButton`，改为原版风格的 110x20 颜色色块/循环选择控件，旗帜底图与颜色叠加继续使用 CastleFlag 图库；伙伴收服页的预览、名称/价格、左右翻页和按钮坐标按原版 `Interface 146` 对照修正。
- 2026-08-08：继续对照原版 `DXWindow.GetSize/GetClientArea`：NPC 商品窗口恢复客户区/滚动条/商品行宽度，隐藏原版不存在的商品窗口出售按钮；维修窗口恢复无 Footer、默认 `GridPadding=0` 的 11×5 维修格和两排来源按钮，工艺窗口同步修正精炼石、碎片、精炼、取回、主精炼、饰品与武器制作的总尺寸和客户区坐标。
- 2026-08-08：新增 `DXTextArea` 作为 Mir 风格多行 `DXTextBox`，聊天输入改为带原版边框/长度/回车提交行为的 `DXTextInput`，通信写信正文不再使用裸 Godot 默认主题 `TextEdit`。
- 2026-08-08：贴图缺口归因完成：生产地图首帧的 `missingTextures=13` 全部来自原版 ZL 元数据中的 `Housesc[0]` 空占位条目，而不是解码失败；MapView 已将空图条目与真实纹理缺失分开统计。复核结果为 `missingLibraries=0, missingTextures=0, emptyImageEntries=13`，不再掩盖真实缺图，也不把合法空帧误报为故障。
- 2026-08-08：复杂 UI 专项回归通过：`UITestScene --ui-audit --npc-audit --communication-audit` 覆盖 22 种 NPC 工艺/交易/伙伴/婚戒/Socket/寄售模式，全部正尺寸；通信窗口 4 页签、主体区域和分页均通过，2x UI 锚点、HUD 点击命中和物品格只读/链接传播均通过。复杂 UI 仍需逐窗口原版截图 A/B，不能仅凭构造审计关闭 P-007。
- 2026-08-08：对照原版 `GameScene.AutoPath.TryQueueAutoPathMove`，修复 Godot 忽略 `ObjectMove.MapChanged` 和自动寻路暂存移动的差异：普通自动寻路移动先暂存到下一帧，跨地图移动丢弃旧步进并交给地图切换；取消自动寻路同步清空本地路径/待移动。进一步修复运行态“入队后实时派发、之后再次排空”的重复执行路径；切图清掉旧世界积压包，并在 `NetworkAudit` 中加入切图前积压包→切图→迟到移动包的顺序回放。
- 2026-08-08：扩展 `MapAudit` 到全地图地形引用：258 张地图、25,856,732 个单元格、186,728 个唯一图库/帧引用全部完成元数据校验；结果 `emptyRefs=46` 为合法 ZL 空条目，`ignoredRefs=42` 全为原版同样跳过的 fileByte=255 未使用层标记，`missingRefs=0`。此前地图审计只验证结构，已补上实际资源引用覆盖。
- 2026-08-08：新增 `MapTestScene --map-family-render-audit`，按生产 `MapInfo.Background` 加载并在真实 Vulkan 2x 视口截图验证 `0/1/5/D001/E01` 五类地图（城镇、野外、沙漠/特殊地形、洞穴/副本样式）；五张图均保存成功，实际视口 `1492x1940`，每张首帧 `missingLibraries=0, missingTextures=0, emptyImageEntries=0`。地图族的原版 A/B 仍待保留，但不再只依赖 map 0。
- 2026-08-08：新增 `MapTestScene --light-render-audit`，在真实 Vulkan 2x 视口中生成 `/tmp/zircon-light-night.png`、`/tmp/zircon-light-twilight.png`、`/tmp/zircon-light-default.png`；三阶段均保存成功，实际视口 `1492x1940`，截图检查确认 Night 明显压暗、Twilight/Default 按原版环境光档位递增。天气粒子仍需单独做生产天气截图，P-004 暂不关闭。
- 2026-08-08：新增 `MapTestScene --weather-render-audit`，在真实 Vulkan 2x 视口生成 `/tmp/zircon-weather-rain-fog-lightning.png`；固定 `RainFogLightning` 运行 30 帧后截图，确认雨滴、雾和闪电层可见，未出现黑色颜色键矩形。对照原版 `FogParticle`（DarkGray、Normal 混合）后，截图中的灰色雾团属于原版雾图可见区域，不将其误报为黑底；P-004 仍保留最终原版 A/B 截图项。
- 2026-08-08：本轮验证：`dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental` 0 errors/0 warnings；`UITestScene --ui-audit`、`GameScene` headless、`git diff --check` 通过；既有 `MapAudit 258/258` 与 `ShadowAudit metadataUsable=5268 decoded=2848 nonEmpty=2848` 保持通过。
- 2026-08-08：真实生产 `GameScene` 自动登录截图首次暴露小地图裁剪差异：Godot 的 `MiniMapDialog.Panel` 未开启 Clip，导致原始 MiniMap 贴图溢出 200×200 逻辑客户区，在 2x 视口中覆盖大半屏幕，视觉上误认为大地图自动打开；对照原版客户区容器后已开启 `Panel.Clip=true`，并在 HUD 创建后显式保持 `BigMap.Visible=false`。修复后生产截图确认仅保留右上角 2x 小地图，大地图不再启动时出现。
- 2026-08-08：生产 Vulkan 2x 回归通过：自动登录进入 `MapIndex=1`、生成 `/tmp/zircon-game-audit.png`，实际视口 `1492x1940`；HUD、角色、NPC、怪物、死亡对象、投射特效、装备层和对象阴影均进入同一截图。日志仍报告 `missingTextures=13`，该项列为下一轮逐纹理归因，不把它误判为全部资源已完成。
- 2026-08-08：通信窗口按原版 `Interface 200–205` 重排：外框 `296x424`、TabControl 页签 `(10,37)/(71,37)/(132,37)/(193,37)`、内容页 `(0,60)` `296x316`；收件页恢复三列标题、5 行 `49px` 间距、`20x308` 滚动条和 Collect/Delete/New 底部按钮；写信附件恢复 `5x1` 原版位置；阅读邮件恢复 Interface 205、7 格附件、Reply/Delete 底部按钮及正文独立滚动区。
- 2026-08-08：通信页新增 `AuditLayout` 与 `--communication-audit`，验证窗口总尺寸、内容裁剪区、四个页签和好友/收件/屏蔽底部操作坐标；与 `--ui-audit --npc-audit` 联合回归全部通过。
- 2026-08-08：`DXLabel` 增加固定客户区内的换行/逐字符宽度换行，使阅读邮件等原版多行标签不再把正文绘制成单行溢出；`DXTextArea` 增加只读与纵向滚动位置接口。
- 2026-08-08：角色编辑和选角创建页的颜色输入从循环色块/原生控件改为 Godot 版 `DXColourControl/DXColourPicker`，恢复原版 `380x253` 调色板、RGB 输入、当前颜色、清除、选择/取消交互，并将最终 Hair/Armour 颜色写入 GenderChange/HairChange/ArmourDye 包。
- 2026-08-08：物品数量窗口移除原生 `SpinBox`，改为 Mir 风格 `DXTextInput`、上下调节按钮和 `DXItemCell`，保留原版 `200x46` 客户区关系、最大数量钳位、回车确认所需的输入行为和数量回写。
- 2026-08-08：颜色调色板进一步对照 `RenderingCore/ColourPaletteHelper` 修正为 `200x149`，优先加载原版 `Debug/Client/Data/Pallete.png`，无文件时使用同一 HSV 生成规则。
- 2026-08-08：通信滚动条审计发现并修正空列表时的行为：原版好友/收件/屏蔽/写信/阅读正文滚动条均保留占位显示，Godot 不再因内容不足一页自动隐藏。
- 2026-08-08：继续对照原版 `MagicDialog`/`DXTabControl`：技能页签恢复 `MarginLeft=56`、`Padding=2`、`Interface 19` 的 `TabHeight=21`，溢出时按原版左右箭头和首个可见页签计算；技能列表滚动条恢复始终占位；技能行解绑/快捷键绑定区域收窄为原版图标 `(9,9,36x36)`，点击名称不再误解绑。
- 2026-08-08：新增 `--magic-audit`，验证技能窗口 `419x511`、列表 `(15,70)/(375,418)`、滚动条 `(390,68)/(20,424)` 和原版 TabHeight。
- 2026-08-08：生产桌面 Vulkan 自动登录截图 `/tmp/zircon-game-audit.png` 复核通过：2x HUD、小地图裁剪、人物/NPC/怪物当前帧、装备层、名称、投射/攻击效果和投影阴影均在同一场景中；退出时仍有既有 Godot RID/ObjectDB 清理警告，未出现通信/颜色控件新增错误。
- 2026-08-08：技能快捷栏继续逐行对照原版 `MagicBarDialog`：空槽恢复 `GameInter2` 的 `MagicSchool.None` 边框，槽位数字与冷却文本统一使用 `MirSkin` 字体；背包钱包恢复原版无可见内容的 `45x40` 点击热区，移除错误的“¤”字符。构建、UI/NPC/通信/技能审计、GameScene、MapAudit 和 ShadowAudit 均通过。
- 2026-08-08：登录/选角页继续对照原版 `LoginDialog/SelectDialog/NewCharacterDialog`：选角窗口补回通用标题/底栏贴图，底部按钮恢复原版 `DefaultHeight` 和 `(25,382)/(120,382)/(215,382)` 坐标，新建角色页恢复原版右上关闭按钮；登录框输入恢复 `170x14`、主按钮恢复 Interface 16 高度，并补回 `(280,38)` 标题。登录页、选角页独立启动和编译通过，仅保留既有退出时资源泄漏提示。
- 2026-08-08：`DXTextInput/DXTextArea` 的内部 Godot 编辑器统一改用 `MirSkin` 字体、字号覆盖、原版白色文字/金色光标和透明编辑边框；登录输入按原版覆盖字号 8。UI、通信、技能审计及登录页启动均通过。
- 2026-08-08：本轮最终回归：`dotnet build --no-restore --no-incremental` 0 errors/0 warnings；`UITestScene` 的 UI/HUD/物品/NPC/通信/技能审计全部 PASS；`MapAudit` `258/258`、`ShadowAudit` `metadataUsable=5268 decoded=2848 nonEmpty=2848`、GameScene headless 和 `git diff --check` 均通过。退出时仅保留既有 Godot RID/ObjectDB 清理警告。
- 2026-08-08：MiniMap/BigMap 继续按原版源码修正：MiniMap 恢复 `DXWindow` 标题客户区与 `Area.Inflate(6,6)` 关系、时间图标初始帧 0、透明按钮 130/131 及所有地图层透明度同步；MiniMap/BigMap 出口恢复 `NeedInstance`/当前副本地图过滤。BigMap 客户区尺寸不再重复加 Footer 高度。
- 2026-08-08：新增 `MapTestScene --pixel-audit` 独立帧级资源审计：另开 `ZlPixelReference` 按原版 `MirLibrary` 的 metadata/payload/BC7 fallback 顺序读取，再与 Godot 解码结果逐像素比较；`--pixel-sample=8` 已通过 `libraries=312 frames=1280 compared=1280`，`Interface.Zl` 全量 `282/282` 通过。全图库全帧审计仍在执行清单中，不将抽样结果冒充全量完成。
- 2026-08-08：完成 P-001 全量资源帧审计：`--pixel-audit --pixel-batch=0/8` 至 `7/8` 8 个批次覆盖 312 个去重图库、1,483,776 个主图帧和 244,866 个 Shadow/Overlay 层，共 1,728,642 次 BGRA 比较；8 个批次均 `PASS`，未发现任何差异像素、差异字节或解码错误。审计仅增加批次调度和进度输出，比较路径仍是原版独立 payload 解码器对 Godot 缓存结果。
- 2026-08-08：P-008 装备/坐骑矩阵继续收紧：按原版 `DrawBody/DrawHorse/DrawHorseShadow` 修正 HorseShape 4–7 的外观库帧、普通坐骑基础影子库和皇家/蓝龙影子库；新增护甲、时装、头盔、盾牌、单/双手武器的实际映射组合，覆盖性别 2×职业 4×方向 8×动作 8，并通过 `PlayerMatrixAudit` 共 23,552 个组合，无图库缺失/帧越界。`--render-audit` 继续使用真实 Vulkan 2x 视口生成 `/tmp/zircon-render-audit.png`；P-008 仍保留原版/Godot 逐截图 A/B 项，不把矩阵日志冒充视觉完成。
- 2026-08-08：对照原版 `MirProjectile.Process` 修正投射物生命周期：无对象目标且 `Explode=false` 时，到达初始距离后继续绘制，直到贴图完全离开视口；有目标或 `Explode=true` 才在到达时触发命中/删除。`--projectile-audit` 在真实 Vulkan 2x 视口生成 `/tmp/zircon-projectile-audit.png`，日志 `travel=44.3px` 通过。另修复 Config 页 2x/UI 的 `Vector2` 到 `Vector2I` 编译回归，完整构建恢复为 0 warning/0 error。
- 2026-08-08：对照原版 `MirLineEffect.Draw` 与 `MirRopeEffect` 修正链条/驯马绳的透明度：非 Blend 路径恢复原版 `Opacity=1`，不再固定为 0.85/0.9；Blend 路径改为可配置 `BlendRate`。完整构建、动作回归和 2x 技能/投射物截图回归通过。
- 2026-08-08：继续对照原版 `MirLineEffect.ToWorld`/`MirRopeEffect.ToWorld` 修正链条与驯马绳锚点：还原 Godot objectBaseline 与原版 DrawY（格子原点）的 32px 差异、链条 `-25/-50` 偏移，以及驯马绳按施法者/目标方向的 SourceOffset/TargetOffset。避免特效整体落在角色脚下或向下偏移一格；构建通过，后续截图回归继续覆盖。
- 2026-08-08：投射物修复后的回归补齐：`UITestScene --ui-audit --magic-audit --npc-audit --communication-audit` 通过，确认 2x UI 锚点、HUD 点击、NPC 页面、通信页和技能栏没有被缩放类型修复破坏；动作序列审计仍全部通过。
- 2026-08-08：帧级审计继续通过：`GameInter.Zl 2485/2485`、`GameInter2.Zl 763/763`、`Magic.Zl 1755/1755`、`MagicEx.Zl 792/792`、`Interface1c-Extended.Zl 3/3` 均为 `different=0`；这些结果只关闭对应图库的解码差异，不代表尚未运行的全部 279 个资源文件已完成。
- 2026-08-08：技能栏释放链补回原版 `GameScene.ToggleTime`：切换技能现在执行 1 秒防连点，快捷栏冷却遮罩按原版将 `NextCast` 与相关切换技能的 `ToggleTime` 取较晚时间；编译、`UITestScene --ui-audit --magic-audit` 和 GameScene headless 回归通过。
- 2026-08-08：MiniMap/BigMap NPC 标记不再统一绘制白色方块；新增 `MapMarkerFactory`，按原版 `GetNPCControl` 显示 QuestIcon 的任务类型/`!/?` 状态、NPC MapIcon 或黄色 3×3 fallback，并恢复大地图 NPC 双击自动寻路入口。构建、UI 审计、MapAudit 和 ShadowAudit 复测通过。
- 2026-08-08：`ZlPixelAudit` 扩展到原版主图、Shadow、Overlay 三层 BGRA 数据；`Interface.Zl` 全量 `282` 主图、`Mon-50.Zl` 全量 `304` 主图+Shadow 均为 `different=0`。Overlay 仍需在包含有效 Overlay payload 的图库上继续批量验证。
- 2026-08-08：任务日志继续按原版行为修正：背景尺寸从错误的 `732x490` 改回资源实际 `732x480`，滚动条保留原版占位，任务排序恢复 Story/Account/General/Daily/Weekly/Repeatable；点击可接任务只选中详情，不再误发立即接受，接受改由详情按钮发送，并切换里程碑使用 Interface 292。
- 2026-08-08：怪物信息窗补回原版资源层：血量填充改用 `GameInter 5430` 并裁剪显示，攻击属性位按 `Stats.GetAffinityElement()` 使用 `GameInter 1510–1517`，保留原版 `186x54/175` 收起/展开尺寸；编译和差异检查通过。
- 2026-08-08：排行榜观察交互按原版修正：选择在线且 `Observable` 的行才启用观察，发送 `C.Inspect { Ranking=true }`；排名观察响应留在排行榜详情区，不再错误打开普通角色窗口。编译通过。
- 2026-08-08：排行榜全榜观察区继续按原版 `RankingDialog` 补齐：左侧恢复 `252x456` InspectPanel、姓名/行会/职位/等级字段、纸娃娃锚点 `(100,290)` 和 17 个只读装备槽（含 Interface 空槽底图）；新增 `UIRankingAudit`，验证 `576x456` 外框、17 槽和观察区几何通过。排行榜完整网络数据回放和原版截图 A/B 仍未关闭 P-007。
- 2026-08-08：怪物信息窗展开区按原版 `MonsterDialog` 恢复资源化布局：`176x110` DetailsPanel、AC/MR/DC、8 个元素抗性图标/数值、攻击/移动速度区间图标、可驯服/不死图标；新增 `UIMonsterAudit`，验证 `186x54` 收起尺寸、27 个展开控件和 8 个抗性项通过。真实战斗数据/提示文本仍需截图 A/B。
- 2026-08-08：任务日志继续对照原版 `QuestTree/QuestTab`：当前/可接任务按起始地图分组并显示任务类型前缀，详情区的起始/结束 NPC 地点恢复可点击打开大地图；新增 `UIQuestAudit`，验证 `732x480` 外框、`680x415` 内容区、`300x405` 详情区和占位滚动条通过。树节点图标/折叠状态和完整任务包回放仍需继续补齐。
- 2026-08-08：怪物窗透明度继续按原版 `MonsterDialog` 校正：窗口整体 `Opacity=0.3`，黑底信息面板与展开 DetailsPanel 使用 `0.6`，并将这些透明度加入 `UIMonsterAudit`。
- 2026-08-08：聊天链路按原版 `ChatTab.ProcessText`/`ChatTextBox` 补齐：`S.Chat.LinkedItems` 不再被丢弃，聊天物品链接转换为黄色可悬停 overlay 并驱动物品提示，聊天行点击玩家名可发起私聊；严格链接正则避免 `[消息类型] 玩家:` 前缀误判。新增 `UIChatAudit`，验证链接 overlay `1/1`。
- 2026-08-08：聊天页签补回原版 `ChatTab.AlertIcon`：每个页签创建 `GameInter 240` 未读提示层，新消息按该页签的消息过滤设置触发，切换页签时清除提示；`UIChatAudit` 同时验证 alert icon 存在。
- 2026-08-08：聊天记录布局按原版 `ResizeChat/UpdateItems` 改为按实际换行高度累计，倒序列表从 `_textArea` 底部锚定，长消息不再固定 16px 覆盖或被底部裁剪；聊天链接审计保持通过。
- 2026-08-08：生产启动回归发现无网络直启动时 `_net.Connection.StopPendingPacketBuffering()` 空引用；按场景生命周期语义改为可空调用。重新编译与 `GameScene.tscn --quit-after 60` 通过，仅保留既有 Godot 退出清理警告。
2026-08-08：角色窗口继续按原版 CharacterDialog 对齐：补齐配偶图标/名称坐标、Inspect 行会旗帜的 GameInter Image + Overlay 双层染色、声望命中区域，并接入 MarriageInfo 状态刷新；新增 `--character-audit`，验证自用窗口 `(331,488)`、Inspect 窗口 `(331,374)`、17 个装备格及关键锚点。
2026-08-08：角色页的修炼/隐士页不再共用内容区；修炼页补入 Interface 215+等级图、等级/经验、提升按钮和最多 4 个职业修炼技能图标，隐士按钮独立到原页容器；`--character-audit` 增加三页互斥可见性检查。
2026-08-08：角色页底部统计从单段摘要改为原版七页结构：攻击、防御、负重、其他、元素攻击、元素优势、元素劣势；补入原版列坐标、范围值、元素图标（ProgUse 600–606 / GameInter 1517）和正负/倍率颜色状态；`--character-audit` 验证 7 页、40 个动态绑定且页面互斥。
2026-08-08：仓库窗口按原版 StorageDialog 修正：内容网格/滚动条从窗口 Y=72 开始，滚动条 X 使用 10 列完整网格宽度，修复初始 1 行网格造成的错误 X=58；仓库/碎片页签补入选中态切换；新增 `--storage-audit`，验证 `(410,479)`、10 列网格、滚动范围和两页互斥。
2026-08-08：小地图按原版 MiniMapDialog 补齐初始 `AllowResize=true`，并处理无 MiniMap 资源时缩为 32px 高、恢复地图时还原尺寸；新增 `--minimap-audit`，验证默认 `(200,200)`、三按钮位置和缩放开关。
2026-08-08：大地图打开指定地图时改为读取目标 `.map` 文件头部宽高再计算 `ScaleX/ScaleY`，不再沿用当前地图尺寸；因此任务/NPC 打开跨地图大图时，NPC、出口、玩家和自动寻路标记使用目标地图坐标系。
2026-08-08：Fortune Checker 继续按原版逐项修正：ItemType 从循环按钮改为可滚动下拉列表，恢复 37 种物品类型的 Description 文本；结果行恢复 465x55、物品格 `(5,5)`、名称 `(49,22)`、Drop Count/Fortune Drop in/Last Check 三组动态字段和 `(410,34,50x25)` Check 按钮；检查操作恢复原版确认弹窗并阻止观察者发送请求。新增 `--fortune-audit`，验证 503x597、9 行滚动区、菜单 38 项/10 行可见。
2026-08-08：Fishing 对照原版 `FishingDialog/FishingCatchDialog` 修正：装备窗保持 `Interface 220=224x268` 与五个装备格坐标；收线窗移除原版不存在的状态文字/收线按钮，恢复 `DXCheckBox Auto Cast`、Interface 233 移动指针、234 抛竿距离指针、231 鱼线有效区和 232 进度裁剪；加入 50ms 鱼/玩家位置模拟、有效区透明度和 `CaughtFish` 参数发送。新增 `--fishing-audit`，装备/收线两窗布局与资源索引通过。
2026-08-08：伙伴窗口继续按原版 `CompanionDialog` 对齐：页签恢复根坐标 Y=38，伙伴实体预览接入 `ObjectRenderer` 并锚定 `(90,140)`，装备槽补回 Interface 99/100/101/102 空槽图，生命/经验/饥饿/负重条改为 GameInter 4375/4310/4311/4312 的裁剪资源，筛选双列间距恢复 110px 并保留 Elite/Superior 颜色；新增 `--companion-audit`，验证 464x372、4 个装备格、3 个侧面板和 7 个加成行。
2026-08-08：组队窗口按原版 `GroupDialog` 补齐：成员区恢复两列 100px 间距和可选中成员，Add/Remove/LFG/Options 四个 36px 图标回到 `(35/81/127/173,217)`；允许组队恢复复选框，LFG 区恢复 `Group Name/Status` 表头、5 行三列数据、启用项排序/过滤和 Interface 60/61/62 滚动条；新增 `--group-audit`，验证 240x424、成员区 `(13,60,194x148)` 与 LFG `(210,268,24x140)`。
2026-08-08：行会窗口继续按原版 `GuildDialog` 修正：无行会时只显示创建页签，拥有行会时首个页签切换为主页；六个页签恢复根 Y=39，页签背景切换时保留原版 260（创建）/261–266（功能页）资源和 456x556 根窗口，不再让无效页签覆盖创建页。新增 `--guild-audit`，验证 6 个页签、无行会可见性和 456x556 外框。
2026-08-08：商城按原版 `GameStoreDialog` 继续对齐：保留 `Interface 310=800x515`、分类/货币/搜索/排序/分页/热门榜根坐标；商品卡恢复两列 `(i%2*202, i/2*80)`、`200x78` 悬停层、物品格 `(19,18)` 和 `GameInter 4830/4835/4855/4857` 操作图标；排序恢复四项下拉列表，数量恢复 1–10 下拉选择，热门榜改为 5 行名次+物品格+名称结构（行距 87、物品格 `(19,26)`），热销条目点击仅筛选商品不直接购买。新增 `--gamestore-audit`，输出 `[UIGameStoreAudit] PASS size=(800,515) list=(199,67) top=(174,425) rows=5`；编译与 Godot headless 审计通过。
2026-08-08：钱包窗口按原版 `CurrencyDialog.SetClientSize(227x302)` 修正通用框架总尺寸为 `245x348`，客户区恢复 `(9,37)/(227,302)`，补回关闭按钮、`GameInter 4870/4871` 分类展开图标与 210px 内容行；新增 `--currency-audit`，验证窗口、客户区和滚动条几何。
2026-08-08：自动寻路路线层按原版 `AutoPathRouteControl` 修正：路线点改为沿线每 6px 的黑边/中心色 4×4 方点，已走点按地图和进度隐藏，终点恢复 15×15 编号标记；保留小地图/大地图现有 `SetRoutes` 调用接口并通过非增量编译。
2026-08-08：大地图按原版 `BigMapDialog` 通用 `DXWindow.GetClientArea` 修正：补回标题/底栏贴图框架和关闭按钮，客户区恢复 `(9,37)`，底栏/边距按 Interface `126/2/10` 计算，地图客户区尺寸限制恢复 `320x240`–`800x520`，总窗口随客户区加原版边框尺寸变化。
2026-08-08：钱包货币行补回原版无掉落物时的 `StoreItem 2683` 图标，物品格改为 `(2,2)`、名称/数量改为 `(40,2)/(40,20)`，行宽恢复为 210px；非增量编译和 UI 组合审计基础回归保持通过。
2026-08-08：帮助窗口按原版 `HelpContainer/HelpItem` 重构正文：主题仍使用 `GameInter 9310/9311`，页签与正文区保持原版坐标，帮助条目改为左标题 120px、右正文 345px 的动态高度分栏，并恢复 `GameInter 9315` 行分隔线、独立滚动和颜色标记清理；新增 `--help-audit`，当前数据库无 HelpInfo 时明确输出 `helpData=0`，几何仍 PASS。
2026-08-08：驯马提示按原版资源尺寸修正：套索动画区 `80x40`，整体窗口 `80x48`，进度填充/外框恢复 `7630=76x4`、`7631=76x6`，裁剪宽度从错误的 130px 改为 0–76px；新增 `--horse-audit`，角度判定和成功回包逻辑保持原版。
2026-08-08：副本查找器按原版 `DungeonFinderDialog` 恢复客户区内筛选栏、`515x40` 独立四列行、9 行 `43px` 行距、`14x402` 滚动条和窗口右上 `(490,35)/(80,25)` 加入按钮；新增 `--dungeon-audit`，布局审计通过。
2026-08-08：角色编辑窗口移除原版不存在的窗口内“变更项目”按钮行，并将 `Select Gender`/`Customization` 面板恢复到原版 `(30,45)/(200,85)`、`(30,140)/(200,330)`；姓名、确认、性别按钮坐标按原版恢复，新增 `--edit-character-audit` 通过。
2026-08-08：行会主页由拼接文本改为原版公告多行编辑/滚动区、右侧滚动条、统计分栏和税率操作；成员页补回标题/列头及 `23px` 行距，仓库页补回名称筛选和原版网格起始位置。`--guild-audit` 现覆盖无行会外框及带成员数据的主页/成员/仓库页，输出 `pages=home=True ...`，仍需继续补完整行会数据回放和截图 A/B。
2026-08-08：自动喝药窗口按原版 `SetClientSize(280x398)` 恢复根窗口 `298x498`、客户区 `(9,37)/(264,398)`、滚动条 `(275,38)/(14,396)`、8 个 `260x46` 行和行内上下按钮 `(5,5)/(5,29)`；新增 `--auto-potion-audit` 通过。
2026-08-08：组队 LFG 按原版 `GroupLFGInputWindow` 恢复独立输入窗口：组名、PvE/PvP 切换、人数范围、Enable/Disable/Cancel 和关闭行为；组队页不再直接发送硬编码的“普通/5人”包，新增 `--group-lfg-audit` 通过。
2026-08-08：配置窗口按原版 `DXConfigWindow` 恢复 `Interface 282=364x416` 根尺寸、五个页签的原版起始坐标和 `348x340` 内容区；新增 `--config-audit` 通过。配置页完整分组资源、音量条、颜色面板和快捷键窗口仍列入后续高差异补齐。
2026-08-08：配置页补回原版独立 `DXKeyBindWindow`：`448x430` 根窗口、`430x330` 客户列表、70 条键位滚动、Defaults/Apply/Close 操作，并从“界面”页接入；新增 `--keybind-audit` 通过。列表已支持选中高亮、单键及 Ctrl/Alt/Shift 捕获，完整配置持久化仍需继续接入。
2026-08-08：配置页基础控件继续按原版 `DXConfigSection`/`DXCheckBox`/`DXSoundBar` 重构：接入 `GameInter 161/162` 勾选图、`4740/4741/4742/4743/4745/4746` 音量资源和分组容器；五个页签现在分别构造 `3/2/1/1/1` 个分组，配置审计输出 `soundBars=True` 并通过。
2026-08-08：退出确认按原版网络生命周期修正：`返回角色选择` 只发送 `C.Logout`，等待 `S.GameLogout { Characters }` 后复用登录连接创建 `SelectScene`；`退出客户端` 独立执行断开并退出，不再把两个按钮错误地绑定到同一行为。GameScene 场景退出时同步解绑 `GameLogoutEvent`，避免迟到回包访问旧场景。
2026-08-08：本轮回归通过：`dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental` 0 errors/0 warnings；`UITestScene` 全组合 UI 审计全部 PASS（含 HUD、NPC、技能、社交、角色、仓库、地图、商城、配置、快捷键、副本、自动喝药和 LFG）；编辑器导入后 `GameScene.tscn` headless 启动无运行错误。完整原版截图 A/B、配置持久化和剩余生产窗口仍保持开放清单，目标未宣告完成。
2026-08-08：配置选项继续接入原版运行时消费：对象/怪物/玩家名称、怪物/玩家血条、伤害飘字、特效、天气、聊天栏、任务追踪和技能框架均按 `ClientSettings` 实时生效；音量条恢复原版 `4742` 填充宽度与 `4745/4746` 滑块位置，并支持点击/拖动/静音。
2026-08-08：快捷键窗口补齐 Defaults 与持久化：保存第一/第二键及 Ctrl/Alt/Shift 修饰符，Defaults 恢复完整默认表，不再只清空第二键；网络页由占位勾选框改为原版地址/端口文本输入，登录连接读取 `UseNetworkConfig/IPAddress/Port`。`UIConfigAudit`、`UIKeyBindAudit` 和编译保持通过。
2026-08-08：最新组合回归通过：编辑器导入后 `UITestScene` 的 UI/HUD/物品/NPC/通信/技能/排行榜/怪物/任务/聊天/角色/仓库/小地图/命理/钓鱼/伙伴/组队/行会/商城/钱包/帮助/驯马/副本/角色编辑/自动喝药/LFG/配置/快捷键审计全部 `PASS`；随后 `GameScene.tscn` headless 运行 12 秒无 C# 运行错误。迁移清单仍保留原版截图 A/B 和未完全回放的生产数据项。
2026-08-08：配置页进一步对齐原版分组：游戏页恢复第二个 `Target Colours` 两列分组，界面页恢复第二个 `Chat Colours` 两列分组及 13 个颜色选择入口；`UIConfigAudit` 从 `game1/ui1` 收紧为 `game2/ui2` 并通过。颜色值与原版聊天颜色模型的完整持久化/逐色截图 A/B 仍列入开放清单。
2026-08-08：聊天颜色分组的每个入口改为原版 `DXColourControlPair` 双色块（前景/背景），不再用单色占位按钮；配置窗口构建、非增量编译和 `UIConfigAudit` 继续通过。
2026-08-08：登录/选角入口继续逐项对照：登录框改为按 `Interface[151]` 实际高度计算底部定位，记住账号恢复 `GameInter[161/162]` 原版复选框，忘记密码恢复悬停变色的可点击文字；选角配置按钮改为读取 `GameInter[116]` 实际尺寸，选择窗口横向位置修正为原版 `(Size.Width / 2 - 320) / 2`。登录页和选角页独立 headless 启动无 C# 运行错误。
2026-08-08：配置颜色入口接入运行时：原版 13 组聊天前景/背景色与 7 组目标颜色写入 `user://Zircon.ini`，聊天消息颜色按 `MessageType` 使用对应前景色；`UIConfigAudit`、非增量编译通过。目标描边的完整生产对象 A/B 仍保留在开放清单。
2026-08-08：颜色运行时消费继续补齐：聊天行现在按原版 `MessageType` 同时使用配置的前景/背景色，修改调色器后立即影响新消息并写入 `Zircon.ini`；`UIChatAudit` 与 `UIConfigAudit` 通过。另修正 NPC 商品审计复用已加载 `ItemInfo`，避免测试对象未绑定 MirDB Session 的空引用，完整 UI 组合回归恢复全部 PASS。
2026-08-08：目标高亮按原版 `MonsterObject/NPCObject.DrawBody(mouseOver)` 收紧：移除错误的固定 48×32 黄/红格子框，改为当前主体帧向外扩展 2px 的目标色轮廓；怪物按宠物友好、等级差低/同/高，NPC 使用 NPC 目标色，并受 `ShowTargetOutline` 控制。非增量编译、`GameScene.tscn` headless 和 `git diff --check` 通过。
2026-08-08：配置图形页继续按原版 `DXConfigWindow` 修正：显示区恢复全屏/无边框/V-Sync/FPS 限制复选框，以及渲染管线、游戏分辨率、默认显示器下拉框；可用性区补回平滑移动、限制鼠标、调试标签和语言下拉框。新增 `ConfigSelect` 原版风格展开列表，图形值写入 `user://Zircon.ini` 并映射到 Godot 窗口模式、边框、V-Sync、最大帧率、尺寸、显示器和鼠标模式；编译、`UIConfigAudit`、`GameScene.tscn` headless 通过。
2026-08-08：目标高亮继续覆盖原版 `PlayerObject`：远程玩家命中代理与实际 `PlayerRenderer` 联动，组员使用友好色、其他玩家使用敌对色；玩家主体/坐骑/装备层先按当前资源帧向外扩展 2px，再绘制正常外观，不再依赖固定格子框。非增量编译、目标 UI 回归和 `git diff --check` 通过。
2026-08-08：继续复核阴影与 2x 世界坐标：对照原版 `MirLibrary.Draw/DrawShadow`、`PlayerObject.DrawShadow2`、`NPCObject.DrawShadow`、`MonsterObject.DrawShadow`，确认 Godot 的玩家影子按身体/装备轮廓投影，坐骑/NPC/怪物优先使用当前帧 Shadow 通道并按原版 ShadowType fallback；掉落物不追加伪影椭圆。链条/驯马绳同步修正为原版 `DrawY` 格子原点、对象基线和方向偏移，非 Blend 恢复 Opacity=1，Blend 使用原版 BlendRate。非增量编译 0 警告/0 错误、`git diff --check` 和动作回归通过；有效 2x Vulkan 渲染截图为 `/tmp/zircon-render-audit.png`。P-002/P-004/P-005/P-007/P-008/P-009 的原版同场景 A/B 证据仍未冒充完成。
2026-08-08：地图层复核发现并修正一处实际差异：原版 `MapControl.DrawObjects` 对单格和大型中层/前景贴图都遵循 `Middle/FrontAnimationBlend`，贴图尺寸只影响底边基线，不影响 Blend；Godot 原先错误地仅对非单格贴图启用 Blend，已改为所有带 Blend 标志的动画帧均使用 `BlendRate=0.5`。非增量编译 0 警告/0 错误、`git diff --check` 通过；桌面 Godot 进程正在占用当前工作区，运行时地图截图回归待下一次无并发窗口时执行。
2026-08-08：图形页的平滑移动/调试标签补回运行时消费：`SmoothMove=false` 时玩家与远程玩家按当前移动动画帧使用离散偏移，开启时按原版帧表总时长连续回拉；`DebugLabel` 控制 FPS、地图和坐标信息层的可见性。非增量编译、编辑器导入、`GameScene.tscn` headless 启动和 `git diff --check` 通过。
2026-08-08：主 HUD 继续补齐原版 Hint 行为：9 个功能按钮、职业/等级/属性图标加入悬停提示，提示中的快捷键由持久化 `KeyBindManager` 生成；`GameScene` 在创建 HUD 前加载键位配置，改键后重新进入游戏即可直接反映。非增量编译、编辑器导入、`GameScene.tscn` headless 启动和 `git diff --check` 通过。
2026-08-08：快捷键窗口按原版 `DXKeyBindWindow` 修正双槽位语义：每行同时显示第一键/第二键，重复点击行切换编辑槽位，普通键、数字键盘、修饰键清除和 Esc 恢复均写入对应槽位；Esc 恢复为窗口打开时的快照，不再错误清空第二键。`UIKeyBindAudit`、非增量编译、编辑器导入和 `git diff --check` 通过。
2026-08-08：快捷键分发按原版 `CEnvir.GetKeyBind` 修正：第一键和第二键都能触发同一动作，数字小键盘统一映射到主键数字；`UIKeyBindAudit` 新增 `secondKey=True` 回归断言并通过。
2026-08-08：通用 `DXImageControl` 对 `Interface[15]` 关闭图标补回原版默认 Hint“关闭”，窗口未覆盖专用提示时自动生效；配置/快捷键审计和非增量编译继续通过。
2026-08-08：窗口基类继续按原版 `DXWindow` 构造语义收紧：没有手工关闭按钮的标准窗口现在自动补 `Interface[15]` 与“关闭”提示；Buff、腰带、聊天输入、怪物、小地图、任务追踪等无标题浮动窗口显式保留无关闭按钮/无边框例外。新增 `--window-chrome-audit`，标准窗口与无边框例外均 `PASS`；非增量编译 0 警告/0 错误。
2026-08-08：窗口基类改动完成桌面 Vulkan 回归：`UITestScene --ui-audit --window-chrome-audit` 实际生成 `/tmp/ui_test.png`（3024x1964），`UIAudit`、`UIHudAudit`、`UIWindowChromeAudit` 全部 PASS；`GameScene` editor/headless 启动和 `git diff --check` 通过。
