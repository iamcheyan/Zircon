# 贴图与动画全面对照审计

本文件是旧版客户端与 Godot 客户端的持续对照记录。结论只有在完成运行时截图/帧级验证后才标记为“已验证”；代码存在不等于视觉结果已经一致。

## 对照入口

| 类别 | 旧版权威入口 | Godot 对应入口 | 当前状态 |
|---|---|---|---|
| ZL 解码、主图、Shadow、Overlay | `RenderingCore/Library/MirLibrary.cs`、`RenderingCore/LibraryFormat/*` | `GodotClient/Formats/ZlReader.cs`、`BcnDecoder.cs` | 代码已对照，需继续做帧级像素抽样 |
| 地图地面/中景/前景 | `Client/Scenes/Views/MapControl.cs` | `MapView.cs`、`MapTerrainRow.cs` | 坐标基线已列出，需逐地图验证边界与层级 |
| 怪物/NPC/地面物品 | `Client/Models/MapObject.cs`、`MonsterObject.cs`、`NPCObject.cs`、`ItemObject.cs` | `MapObjectNode.cs`、`ObjectRenderer.cs` | 帧表/定位已对照，需继续检查 Shadow 与大图对象 |
| 玩家/装备/坐骑/外观 | `Client/Models/PlayerObject.cs`、`Player/ExteriorEffectManager.cs` | `PlayerRenderer.cs` | 形状偏移已迁移，需逐动画验证装备轮廓和覆盖顺序 |
| 序列帧技能特效 | `Client/Models/MirEffect.cs`、`MirProjectile.cs`、`MirLineEffect.cs` | `MirEffectNode.cs`、`MirProjectileNode.cs`、`MirLineEffectNode.cs`、`MagicEffectTable.cs` | 播放模型已对照，需逐类确认 Blend/Offset/Direction |
| 粒子天气 | `Client/Models/Particles/Weather/*.cs` | `MapWeatherLayer.cs` | 参数已对照，透明键正在按素材类型修正 |
| UI 图片/动画 | `Client/Controls/DXImageControl.cs`、`DXAnimatedControl.cs` | `DXImageControl.cs`、`DXAnimatedControl.cs`、各 Godot Controls | 入口已定位，需检查 UI 缩放是否误用世界缩放 |

## 每类必须核对的项目

1. 解码格式、通道顺序、Alpha 和颜色键。
2. `Width/Height`、`OffSetX/OffSetY`、Shadow/Overlay 尺寸和偏移。
3. `UseOffSet=true/false` 时的左上角、中心和脚底锚点。
4. 原版 1x 逻辑坐标到 Godot 世界 2x 输出的唯一缩放位置。
5. 纹理过滤模式，像素素材必须避免意外线性插值。
6. Floor/Object/Final/UI 绘制层级和对象 RenderY 排序。
7. 起始帧、方向步长、形状偏移、帧数、逐帧延时、倒放、循环和结束回调。
8. 普通贴图、特效贴图、天气贴图不能错误共享透明处理或纹理缓存。

## 已确认的共性规则

- 普通地图、人物、装备和地面物品贴图不使用黑色透明键。
- 旧版角色、装备、技能和外观特效的正式调用均传 `ImageType.Image`；不能把所有“Blend”调用误当成黑色颜色键。天气/雾/闪电是单独的颜色键路径，DXT 压缩后的近黑边缘需要专用清理。
- 天气和雾不能只按同一个黑色阈值处理；天气帧使用专用透明缓存，雾帧还需要边缘背景色连通清除。普通 Alpha 审计与天气颜色键审计已分离。
- 世界节点统一使用 `WorldScale=2`；天气、地图、对象使用逻辑坐标，不能在子层重复乘 2。
- UI 位于独立 Canvas/缩放体系，不能套用世界坐标缩放。
- UI `Blend=true` 使用 Godot Add 混合材质，匹配旧版光效叠加用途；运行时 HUD/物品格/腰带审计已通过，特殊窗口光效仍保留显示环境截图确认项。
- 通用序列帧特效与投射物必须同时应用旧版 `DrawColour`/元素颜色和透明度；Godot 已将 `FrameLightColour` 用于 RGB 染色与 Alpha 调制。
- 纸娃娃头部层已补齐旧版职业/性别发型帧：普通职业男/女为 60/80 系列，刺客男/女为 1100/1120 系列；头盔存在时仍由头盔层覆盖。
- 纸娃娃已补齐旧版 `EquipEffect_UI` 武器/盾牌外观特效、预设装备特效和 100ms 动画帧；这些旧版入口传 `ImageType.Image`，Godot 使用普通 Alpha 图层并在对应武器/盾牌层之后 Add 混合。
- 地图 Middle/Front 动画已拆为普通行节点与 Blend 行节点；只有带 Blend 位的动画进入 Add 混合，普通贴图不再被同一行的混合材质影响。
- 地图动画刷新现在同时通知普通、前景、Blend、Blend 前景四类行；此前 Blend 行未进入 100ms 地图动画刷新，会导致带 Blend 标记的地图动画停帧，已修正。
- 天气参数已逐项与 `Client/Models/Particles/Weather/{Rain,Snow,Fog,Lightning}.cs` 对照：雨 509→510..514、雨滴 10ms 生成/100ms 水花帧；雪 500、20ms 生成、落地后缩放消融；雾 550、4 张按 `Width*Scale` 横向排列；闪电 540、最多 3 个、1000..5000ms 随机生成、100..200ms 生命周期。Godot `MapWeatherLayer` 已按这些参数实现，并修正为旧版每 10ms 粒子逻辑步进的等效 100Hz 速度/旋转/消融速率，避免使用 60Hz 导致雨雪位移和缩放时序偏慢。
- `GodotClient/project.godot` 的 `textures/canvas_textures/default_texture_filter=0` 对应像素贴图 Nearest；世界与 UI 的 2 倍缩放分别只设置在 `GameScene` 的世界节点和独立 `CanvasLayer`，天气层使用逻辑坐标后由世界父节点统一放大。
- UI `DXAnimatedControl` 已恢复旧版构造默认值 `Animated=true、Loop=true`；此前 Godot 字段默认 false，导致未显式设置属性的 Socket/合成动画停在首帧。
- `MapObjectNode.GetFrameIndex` 的倒放时序已按权威 `LibraryCore/FrameSet.GetFrame` 修正：倒放只反转延时查找顺序，返回值仍为逻辑帧 `i`；此前返回 `FrameCount-1-i` 会造成二次反转。
- `MirEffectNode.UseOffSet=false` 已按旧版 `MirLibrary.Draw` 修正为节点左上角绘制；贴图居中只由天气/粒子专用的 centered 绘制路径负责，避免把通用特效错误平移半个贴图尺寸。
- `DXItemCell` 已按旧版补齐物品角标的灰度状态，并对 `ItemPart` 使用 `Count >= PartCount` 判断可用颜色；此前 Godot 角标始终白色且部件主图只按 `Count > 0` 判断。
- 世界层级已静态核对：地图行使用 `99+y/101+y`，对象使用 `100+RenderY`，Floor/Object/Final 特效分别使用 50/100+RenderY/10000；天气为 850、光照为 900、UI 为独立 CanvasLayer 10，天气先于夜间光照处理且位于对象之上。
- 地图动画已补齐旧版尺寸规则：只有非标准格尺寸（不是 48×32 或 96×64）的动画帧才进入 Blend/Add；标准格尺寸即使携带 Blend 位也按普通贴图绘制。此前 Godot 对所有 Blend 位都使用半透明 Add。
- 玩家外观特效已拆为独立 `BlendImageLayerNode`：旧版 `ExteriorEffectManager` 的外观翅膀、光环、戒指等 `DrawBlend` 现在使用普通图像缓存 + Add 材质，并按 behind/front 使用相对 Z=-1/+1；避免给整个玩家节点加 Blend 导致身体和装备一起变亮。
- 普通玩家 `DrawShadow2` 的投影已修正为共享身体帧脚底锚点：旧版先合成身体/装备 scratch 再统一斜切，Godot 现在对各层轮廓使用身体 `OffSet/ShadowOffSet/Height` 的共同变换，避免武器、盾牌、头盔影子各自漂移。
- 旧版 `MonsterImage.DustDevil/Tornado` 的主体 `DrawBlend` 已补入 Godot：通过独立普通图像 Add 层绘制，不再按普通怪物 Alpha 图层处理。
- `MonsterImage.CastleFlag` 已补齐旧版主体后的 Overlay 绘制，使用独立 Overlay 缓存、Overlay 尺寸和主体 Offset。
- 坐骑附加外观已按旧版 `DrawHorseOverlay` 修正为独立普通纹理 Add 层；坐骑主体仍使用普通图层，避免附加光效被当作不透明贴图绘制。
- 玩家外观动画速度已按旧版分组修正：翅膀/光环/戒指使用 `MapControl.Animation/2`（Godot `slowTick`），火炉/灯笼类保留原始 `Animation` 速度，避免外观特效整体快一倍。
- 纸娃娃 `EquipEffect_UI` 已按旧版 `EquipEffectDecider` 改用普通 `ImageType.Image` 缓存；此前误用特效透明键会删除 UI 外观中合法的黑色像素。
- `MirLineEffect`/`MirRopeEffect` 已按旧版基类改用普通 `ImageType.Image` 缓存；绳索/链条不再误用技能特效黑色透明键，且默认保持旧版普通 Alpha（只有明确 Blend 的调用才使用 Add）。
- `MirProjectileNode` 的 `UseOffSet=false` 已按旧版 `MirProjectile : MirEffect` 修正为贴图左上角 `(0,0)`；居中只属于独立 `DrawBlendCentered` 粒子路径，投射物不再被错误平移半个尺寸。
- UI `DXImageControl` 已按旧版 `PresentTexture` 改为使用 `ForeColour`/禁用色直接调制贴图 Alpha，不再在贴图上覆盖半透明灰块；同时补充普通 Alpha 与 Add Blend 两套灰度 shader，只改变 RGB 灰度而保留原始 Alpha。
- Godot `DXWindow` 已补齐旧版 `DropShadow` 属性，并在窗口子控件之前用独立阴影 StyleBox 绘制；阴影不再错误叠加到窗口背景贴图内容上。当前仍需带窗口运行时确认阴影边界与旧版模糊半径的像素级差异。
- 纸娃娃已补齐旧版遗漏的护甲 `EquipEffectDecider` 层，并将护甲、武器、盾的 UI 装备特效改为独立普通图像 Add 子层；护甲使用 behind Z，武器/盾使用 front Z，避免父纸娃娃整体套 Blend。
- `CharacterDialog` 的属性分页已补齐统计文本节点字段，修复此前新增属性面板导致的编译回归；属性页仍使用独立 UI 坐标和 Nearest 贴图体系。

## 仍需补强的证据

- 旧版与 Godot 相同 ZL 帧的逐像素 RGBA 差异图仍需要旧客户端实际输出；当前已完成 Godot 端 Alpha/尺寸/颜色键审计和静态路径对照。
- 角色每个 `MirAnimation` 的无窗口运行时帧序列已通过；装备组合、Shadow/Overlay 的最终轮廓仍需真实显示环境截图抽样。
- 主要技能特效的 Blend、UseOffSet、DrawType、Reversed、Loop 和结束时刻已由代码对照及 `MapTestScene --action-audit` 覆盖；最终画面颜色/混合强度仍需显示环境截图。
- UI 2x 缩放、Offset、Nearest 过滤和窗口阴影已通过 `UITestScene --ui-audit` 资源/锚点测试；窗口阴影模糊半径仍需真实显示环境像素确认。
- 下列验证门已完成：Godot 编译、静态贴图缓存引用审计、`git diff --check`、MapTest 运行时审计、UITest UI 运行时审计。

## 当前静态扫描记录

- Godot 贴图绘制调用：46 处（当前静态扫描）。
- Godot 贴图缓存调用：当前已逐项检查普通图、特效图、天气图、Fog、Shadow、Overlay 的全部入口；调试查看器和 MapTestScene 也单独标记，不影响正式渲染路径。
- 世界缩放声明：7 处，已区分 GameScene 父节点统一缩放与各模块仅用于把 viewport 换回逻辑坐标的局部常量；未发现重复乘 2 的绘制路径。
- UI 缩放声明：1 处，当前由 `GameScene` 的独立 `CanvasLayer` 统一负责。
- 最近一次 `dotnet build GodotClient/ZirconClient.csproj`：通过，0 错误、0 警告。
- 最近一次 `git diff --check`：通过。
- 2026-08-08：阴影/特效坐标复核：`MirLibrary.DrawShadow` 的 ShadowOffset、ShadowType 斜切投影和玩家 `DrawShadow2` 的共享脚底锚点已与 Godot `RenderPrimitives`/`PlayerRenderer` 对照；`MirLineEffectNode`、`MirRopeEffectNode` 已补回原版 DrawY→对象基线换算及八方向 source/target 偏移。2x 只由世界父节点统一执行，绘制函数不再重复放大；编译、动作回归和有效 2x 渲染截图通过。
- 2026-08-08：修正地图中层/前景动画的 Blend 判断：原版单格贴图同样遵循 `AnimationBlend`，Godot 不再把贴图尺寸作为 Blend 开关；单格与大型贴图现在都按原版 `0.5` BlendRate 绘制，尺寸只用于底边基线。
- 已使用本机 C# Godot Mono headless 实际加载 `MapTestScene`、`System.db` 和 `0.map`，完成真实对象绘制及动画审计；headless 无法回读最终纹理，因此仍需有显示环境的截图做最后视觉确认。

## 运行时审计记录（2026-08-08）

命令行入口：`godot-mono --headless --path GodotClient --scene Scenes/MapTestScene.tscn -- --render-audit --action-audit`。

- 地图加载：350x350，20x20 区域背景 100、中层 388、前景 400，完成渲染。
- 对象绘制：真实 Monster/NPC/Player/Item 均创建并绘制，包含 Shadow、Overlay fallback、标签和 RenderY；`RenderAudit` 完成。
- 天气：雷电帧 540 检查通过，尺寸 256x512，透明像素 114813、可见像素 16259；说明天气资源已经经过透明键清理并能被正常读取。
- 动画：Walking、Running、HorseWalking、HorseRunning、Combat、RangeAttack、ShoulderDash、Spell、Channel、Struck、Pushed、Harvest、Mining、Fishing、Taming 等审计项均通过，帧序列符合旧版逻辑。
- 技能覆盖：`castConfigured=142`、`attackOnly=11`、`missingOriginalSpell=0`；没有发现旧版技能资源缺失映射。
- 历史抽样曾发现 `ProgUse` frame 594（20x20）角点颜色高度相同，作为候选保留过；最近一次完整 Mono 审计已重新抽样 34 个图库/339 帧并报告 `cornerPollution=0`，当前没有新的透明污染候选。该帧仍保留原样，避免误删未引用素材中的合法黑色。
- 已使用 `/home/tetsuya/.local/bin/godot-mono`（Godot 4.6.3 Mono）成功启动 `Scenes/MapTestScene.tscn`，实际加载 `System.db`、`0.map`（350×350）并完成 20×20 地图区域绘制；headless 无可读回纹理，仍不能替代带窗口的逐像素截图对比。
- 已使用同一 C# Godot Mono 启动 `Scenes/UITestScene.tscn --ui-audit`：窗口、按钮、中文字体和 Interface 贴图均成功加载，`UIAudit PASS scale=2 logical anchors preserved`，并生成 `/tmp/ui_test.png`；该 headless 截图可验证资源加载/缩放锚点，最终窗口阴影模糊半径仍需真实显示环境确认。
- 最近一次 UI 回归还通过 `UIHudAudit`、`UIItemGridAudit`、`UIBeltPotionAudit`：HUD 面板/按钮点击命中、物品格只读/联动/清除传播、腰带缩放/滚轮范围/行交换均通过。
- 已查看 `/tmp/ui_test.png`：窗口边缘可见独立阴影，Interface 金色边框和透明区域未出现天气类黑色矩形；右侧/底部灰色区域来自 headless 的 64×64 逻辑 viewport 与输出纹理尺寸不一致，是测试环境伪影，不作为贴图缺陷。
- 最近一次完整天气审计已逐帧通过：`500、509、510、511、512、513、514、540、550` 共 9/9，分别使用 Weather/Fog 专用透明缓存并检查透明像素与可见像素均大于 0。
- 最近一次标准运行时回归的全库 Alpha 抽样覆盖 `libraries=312 frames=3189 transparentFrames=2499 cornerPollution=0`；`MagicCoverageAudit` 为 `castConfigured=142 attackOnly=11 missingOriginalSpell=0`，`SpellTimingAudit` 的 `Combat1` 释放延迟 400ms、总时长 600ms，全部 `ActionAudit` 动作序列通过。
- `MirLineEffectNode` 与 `MirRopeEffectNode` 的非 Blend 链条/绳索透明度已恢复原版完整不透明度；链条 Blend 路径保留可配置的原版 BlendRate，不再使用未经原版依据的 0.85/0.9 固定值。
- 链条/驯马绳位置也已按原版 `DrawY` 格子原点和方向偏移重新锚定；Godot 对象基线的额外 32px 不再被遗漏。
- 已增加 `--full-texture-audit` 全量模式，采用分帧批处理避免同步解码阻塞；`EquipEffect_UI=367`、`EquipEffect_Part=2809`、`EquipEffect_Full=21616`、`EquipEffect_FullEx1/2/3=5192/3152/3152` 及其它已列单库均取得明确终点。默认抽样审计和天气全帧审计仍独立通过。
- 全量模式支持 `--audit-file=<LibraryFile>` 及 `--audit-start/--audit-end` 分片运行；已完成单库全量审计的技能/装备特效图库包括 `Magic`、`MagicEx`–`MagicEx11`（其中 `MagicEx11` 为有效空库，`MagicEx`–`MagicEx9` 合计 8207 帧），以及全部 `EquipEffect_*`（36288 帧），全部 `cornerPollution=0`。角色、发型、服装、坐骑、武器、盾牌、头盔、怪物主体/技能、UI/辅助库和基础/Forest/Sand/Snow/Wood 地图库均已按后续分项记录取得单库终点；已完成库均无透明角落污染。普通库审计保留原始 Alpha/黑色像素，天气颜色键由独立 WeatherAudit 覆盖。剩余未决项是旧客户端与 Godot 的显示环境逐像素画面证据，而不是未审计库。
- 本轮补齐的外观终点：`M_HumEx10` 11952 帧、`M_HumEx11` 2352 帧、`M_HumEx12` 3984 帧、`M_HumEx13` 为有效元数据空库（0 帧）、`WM_Hair` 16808 帧、`WM_HairA` 8960 帧、`M_Costume` 19984 帧、`WM_Costume` 19984 帧、`WM_Weapon10` 7600 帧、`WM_Weapon11` 7600 帧、`WM_Weapon12` 3752 帧、`WM_Weapon13` 6840 帧、`WM_Weapon14` 7600 帧、`WM_Weapon15` 7600 帧、`WM_Weapon16` 5160 帧；上述库均 `cornerPollution=0`。
- 女款扩展武器也已取得终点：`WM_WeaponADL1/2/6`（10240/1024/0 帧）、`WM_WeaponADR1/2/6`（10240/1024/1024 帧）、`WM_WeaponAOH1/2/3/4/5/6`（10240/8896/10240/7168/4096/1984 帧），全部 `cornerPollution=0`；其中 0 帧库是有效元数据空库，不是审计跳过。
- 头盔变体补充完成：`M_Helmet11/12/14/A1/A3`（13280/2656/0/13840/6920 帧）及 `WM_Helmet11/12/14/A1`（13280/2352/0/13840 帧），全部 `cornerPollution=0`；`M_Helmet14`、`WM_Helmet14` 的 0 帧同样是有效空库。
- 坐骑和女角色扩展也已补齐：`Horse` 2160 帧、`HorseBlue` 776 帧、`WM_HumEx10` 11952 帧、`WM_HumEx11` 2184 帧，全部 `cornerPollution=0`。
- Forest/UI 辅助图库本轮也取得终点：`Forest_Housesc` 7174、`Forest_SmTilesc` 0、`Forest_Tiles5c` 20、`Forest_Tiles30c` 156、`Forest_Tilesc` 4861、`Interface1c` 1488、`Interface1cExtended` 3、`Interface` 282、`GameInter` 2485、`GameInter2` 763、`Equip` 798、`StoreItem` 2370 帧，全部 `cornerPollution=0`；其中 `Forest_SmTilesc` 是有效元数据空库。
- 基础物品/图标库本轮取得终点：`Inventory` 2365、`Ground` 2259、`NPC` 5617、`MiniMap` 246、`MiniMap2` 有效空库、`MagicIcon` 224、`CBIcon` 199、`QuestIcon` 90 帧；已完成项均 `cornerPollution=0`。
- 其它辅助库也已完成：`MiniGames` 113、`CastleFlag` 120、`MiniMapIcon` 138、`Background` 6、`NPCImage` 93、`MonImage` 141、`PEquipB1` 752、`PEquipH1` 1128、`HorseS` 有效空库；全部 `cornerPollution=0`。
- 三套主题地图库现已全部取得单库终点：Sand（`Animationsc/Cliffsc/Dungeonsc/Furnituresc/Housesc/Innersc/SmObjectsc/SmTilesc/Tiles5c/Tiles30c/Tilesc/Wallsc` = 148/630/14461/0/1734/0/3409/0/35/0/8570/1729 帧）、Snow（880/14122/18482/117/11234/1903/9851/224/60/520/8545/3334 帧）、Wood（1131/28913/8495/4675/13869/3749/10098/0/60/338/12997/7329 帧）；全部 `cornerPollution=0`，其中 0 帧项均为有效空库。
- 技能辅助库 `MagicEx10` 207 帧全量通过、`MagicEx11` 为有效空库；两者均 `cornerPollution=0`。
