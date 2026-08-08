# 旧版客户端点击动作与 Godot 迁移记录

本文以仓库中的旧版 `Client` 为行为基准，记录地图、主界面、物品格和主要窗口的鼠标动作。代码依据：

- `Client/Controls/DXItemCell.cs`
- `Client/Scenes/Views/MapControl.cs`
- `Client/TargetForm.cs`
- `GodotClient/Controls/DXControl.cs`
- `GodotClient/Controls/DXItemCell.cs`

## 1. 全局鼠标分发

旧版 `TargetForm` 把移动、按下、抬起、单击、双击、滚轮分发到当前 `DXControl.ActiveScene`。右键按下时，如果正在拿起物品或货币，会优先取消/处理拿起状态。

Godot 由 `Control._GuiInput` 分发；`DXControl` 把左键、右键统一转换为 `MouseClick`，但物品格 `DXItemCell` 有自己的 `_GuiInput`，必须直接实现旧版语义。

## 2. 地图点击

| 动作 | 旧版效果 |
|---|---|
| 左键按下地图 | 取消自动寻路；若正在拿起物品，打开丢弃数量窗口（仅背包/伙伴背包可丢）；若拿起腰带/自动药水链接则清除链接；否则选中可攻击目标，或清除目标。 |
| 左键单击 NPC | 发送 `NPCCall`，有 1 秒 NPC 点击节流。 |
| 右键按下地图 | 取消自动寻路；按配置取消怪物目标。 |
| Ctrl+右键单击玩家 | 发送 `Inspect`，打开角色查看。 |
| 地图移动/拖动 | 更新鼠标地图坐标，并由移动逻辑执行寻路/移动。 |
| 滚轮 | 由场景/地图控件处理缩放或滚动。 |

Godot 对应入口是 `MapView`/`MouseWalker`/`GameScene`，需保持“UI 优先、地图其次”：被 `MouseFilter.Stop` 的窗口控件不能把点击传给地图。

## 3. 物品格统一语义

| 按键 | 旧版效果 |
|---|---|
| 左键 | 无拿起物品时拿起；已有拿起物品时移动、交换或堆叠；点击自身取消拿起。Shift+左键拆分数量；Alt+左键建立聊天物品链接。 |
| 右键 | 背包/仓库等上下文中把物品放入维修、镶嵌、精炼、制作、寄售等目标格；普通背包右键使用；装备右键卸下到背包；腰带右键使用。 |
| 中键 | 切换物品锁定；Ctrl+中键在旧版中链接到聊天。 |
| 左键双击 | 背包、仓库、伙伴背包/装备、腰带执行 `UseItem`；装备上的婚戒触发传送。 |
| 鼠标悬停 | 更新物品提示和边框。 |
| 滚轮 | 在仓库/物品列表中滚动。 |

## 4. `UseItem` 行为

武器、衣服、头盔、盾、首饰、鞋、毒、护符、花、徽章、时装、马具、钓鱼装备、伙伴装备等，根据 `ItemType` 自动选择正确装备槽并发送物品移动。

消耗品、卷轴、伙伴食物、物品部件、书籍在允许的网格中发送 `ItemUse`；受使用冷却、骑马、钓鱼/驯马状态、可用性和物品锁限制。

`Bundle` 发送 `BundleOpen`；`LootBox` 发送 `LootBoxOpen`。腰带格不是物品本体，而是找到背包中的链接物品后调用同一 `UseItem`。

## 5. 物品拖放目标

旧版支持普通背包、装备、仓库、部件仓库、伙伴背包/装备、腰带、自动药水，以及维修、镶嵌、精炼、制作、寄售、婚戒等专用格。专用格通常只建立 `CellLinkInfo`，确认按钮才发送最终操作；不能误把背包物品直接移动出背包。

## 6. 主界面和窗口

主界面按钮点击主要是窗口开关：角色、背包、技能、任务、邮件、腰带、队伍、菜单和商城。窗口标题栏支持关闭、拖动；Escape 关闭当前窗口（配置允许时可连续关闭）。页签切换只更换窗口背景/内容，不发送游戏操作。

菜单窗口进入仓库、设置、帮助、公会、排行、伙伴、称号和退出确认。NPC、交易、商店、任务、通讯、钓鱼、宝箱等窗口的按钮均在各自 `MouseClick` 回调中发送对应网络包。

## 7. 当前 Godot 对照结果

已发现并修复：`DXItemGrid.ItemGrid` 后绑定时，原实现没有同步到已创建的 `DXItemCell`，导致背包格的 `Item` 始终为空，点击自然没有效果。修复后网格数组变更会同步每个格子并刷新图标/数量/边框。

当前 `GodotClient/Controls/DXItemCell.cs` 已实现左键移动、右键使用、左键双击使用、中键锁定/聊天物品链接、装备自动穿戴/卸下、腰带链接、自动药水链接、邮件附件、行会仓库和专用加工格链接。维修、镶嵌、精炼、制作、交易和寄售窗口都通过 `CellLinkInfo` 路由，避免把临时目标格误当成真实背包。

## 8. 已接入的邮件与行会仓库

写邮件页提供 5 个 `GridType.SendMail` 附件格，背包右键物品会建立来源链接，发送时收集 `CellLinkInfo` 写入 `MailSend.Links`。行会仓库页提供 `GridType.GuildStorage` 网格，支持背包与仓库之间的旧版物品移动包，并接收 `GuildNewItem`/`GuildGetItem` 更新。

阅读邮件页显示最多 7 个附件；点击附件发送 `MailGetItem` 领取，与旧版 `ReadGrid` 点击附件的行为一致。

NPC 买卖窗口的背包右键已进入出售选择列表，提交时发送 `NPCSell.Links`；地图掉落物左键发送 `PickUp`，地图 NPC 左键发送 `NPCCall`。

地图玩家现在加入统一命中代理；Ctrl+右键会使用服务器下发的角色索引发送 `Inspect`，实际玩家外观仍由独立的 `PlayerRenderer` 绘制。

装备镐子时，地图左键点击玩家相邻空格会发送 `Mining`；距离和地图最终合法性仍由客户端初筛与服务端共同校验。

交易窗口点击“添加金币”现在弹出数量输入框并发送实际 `TradeAddGold.Gold`，不再固定发送 0。

交易物品格已改为旧版 `TradeAddItem.Cell` 链接包：背包物品进入己方交易栏只建立来源链接，不发送普通 `ItemMove`；对方物品格保持只读。

交易打开、对方物品/金币更新、解锁和关闭回包均已接入交易窗口状态刷新。

## 9. 任务、伙伴与行会高级动作

任务日志的可接任务左键发送 `QuestAccept`；当前任务左键追踪，右键放弃，完成任务左键提交。若旧版任务包含 `QuestReward.Choice`，Godot 会先显示可选奖励，点击后使用奖励对象的 `Index` 作为 `QuestComplete.ChoiceIndex`，与旧版 NPC 任务提交逻辑一致。

伙伴窗口的装备/背包仍通过统一物品格处理；“收起伙伴”和“释放伙伴”分别发送 `CompanionStore`、`CompanionRelease`。伙伴 NPC 管理相关的 `CompanionRetrieve`、`CompanionUnlock`、`CompanionAdopt` 协议也已在网络层保留对应入口。

行会战争页读取地图中的 `CastleInfo`，展示城堡领主与攻城时间，并提供“发起行会战”(`GuildWar`)、逐座“申请攻城”(`GuildRequestConquest`)、开关城门(`GuildToggleCastleGates`)、修理城门(`GuildRepairCastleGates`)和修理守卫(`GuildRepairCastleGuards`)动作；风格页的旗帜和颜色修改仍分别发送 `GuildFlag`、`GuildColour`。

排行榜增加角色名搜索并发送 `RankSearch`；通讯好友页增加在线/忙碌/离开循环状态并发送 `ChangeOnlineState`；设置页的“显示头盔”开关发送 `HelmetToggle`，不再只是本地勾选。

通讯窗口新增屏蔽页：登录包中的 `BlockList` 会填充列表，添加按钮发送 `BlockAdd`，点击已屏蔽角色发送 `BlockRemove`，并接收 `BlockAdd`/`BlockRemove` 回包实时刷新。

角色属性页已接入旧版修炼动作：显示当前修炼等级/经验和下一等级要求，达到旧版等级条件时启用“提升修炼”，点击发送 `IncreaseDiscipline`；`DisciplineUpdate` 与经验变化回包会刷新显示。

本轮补齐行会税率(`GuildTax`)、婚戒制作(`MarriageMakeRing`)、婚姻邀请接受/拒绝(`MarriageInvite` + `MarriageResponse`)和戒指传送的网络入口(`TeleportRing`)；婚戒 NPC 专用格提交时发送实际戒指槽位，不再调用普通 NPC 按钮。

角色隐士页的 AC、MR、生命、魔法、DC、MC、SC、武器元素按钮分别发送对应 `Hermit.Stat`，与旧版逐项加点回调一致。

寄售搜索页新增“历史”按钮，按当前选中物品发送 `MarketPlaceHistory`，并显示成交数、最近成交价和平均价；历史回包按旧版 `Display` 标识匹配，避免异步结果串到其他物品。

角色窗口新增“城镇复活”入口，点击发送旧版 `TownRevive` 请求，覆盖旧版聊天复活动作在 Godot 中没有聊天动作菜单时的可操作入口。

快捷栏释放 `MagicSchool.Toggle` 技能时改为切换本地启用状态并发送 `MagicToggle`，不再把切换类技能当作普通定点施法；同一技能再次点击会发送关闭状态。

钱包中的可掉落货币现在可点击选中；随后点击地图会发送 `CurrencyDrop`，并在发送后清除选中状态。不可掉落货币、数量为零的货币不会进入该流程。

交易请求回包现在会弹出接受/拒绝面板，分别发送 `TradeRequestResponse.Accept=true/false`；不再只处理已经建立的交易窗口。

大师精炼面板新增旧版“评估”动作，使用当前五组材料链接发送 `NPCMasterRefineEvaluate`，与直接提交精炼分开。

骰子/Yut NPC 现在分两步处理：点击“开始”发送 `NPCRoll`，收到服务器结果后显示结果并提供“领取结果”，点击后发送 `NPCRollResult`，对应旧版动画结束后的提交动作。

大地图双击寻路在发送 `AutoPathWaypoint` 后同步发送 `AutoPathMoveStarted`，补齐旧版自动寻路开始时的服务器状态通知。

NPC 伙伴管理页不再只是说明文字，新增召回当前伙伴、收起当前伙伴、释放当前伙伴按钮，分别走 `CompanionRetrieve`、`CompanionStore`、`CompanionRelease`。

伙伴管理页同时提供伙伴编号/名称输入，解锁和收服按钮分别发送 `CompanionUnlock`、`CompanionAdopt`，覆盖旧版 NPC 管理页的全部伙伴状态动作。

里程碑页的服务器通知与追踪状态请求也已补齐：打开/关闭页面发送 `MilestoneNotify(Receive)`，勾选或取消追踪发送 `MilestoneActive(Index, Active)`；达成弹窗的领取仍发送 `MilestoneClaim`。

任务日志现在新增“里程碑”页，显示服务器下发的里程碑、完成状态、描述和未领取奖励；点击标题切换追踪状态，点击未领取项领取奖励，并在离开页面/关闭窗口时停止 `MilestoneNotify`。

NPC 精炼/制作/饰品操作、复活计时、观察模式以及行会创建、成员处理、税率、扩容、邀请等回包现在由 GameScene 统一订阅并显示结果状态，避免请求成功或失败后客户端没有任何反馈。

设置页“游戏”分类新增“允许被观察”开关，点击时发送 `ObservableSwitch(Allow)`；角色、技能快捷键、头盔显示和语言切换动作均已与旧版请求参数对齐。

商城右侧推荐/热销商品不再只是文字：每个推荐项均可点击并按当前货币发送购买请求；组队窗口的“刷新 LFG”会重新构建当前服务器列表。

退出游戏按钮在断开连接前发送旧版 `Logout`，避免只关闭 Godot 场景而没有通知服务器。

选角页新增“删除角色”按钮，确认后发送 `DeleteCharacter(CharacterIndex, CheckSum)`，并处理 `DeleteCharacter` 回包刷新角色列表。校验串按旧版客户端方式持久化到 Godot `user://checksum.bin`，同时用于登录、注册和建角请求；删除不再发送空校验串。

设置页“界面”分类新增语言切换按钮，切换中文/English 时发送旧版 `SelectLanguage` 请求。

物品使用前增加旧版状态限制：钓鱼/驯马过程中禁止使用物品，骑马时禁止消耗品、书、礼包和宝箱使用，避免 Godot 比旧版多发无效 `ItemUse` 请求。

地图左键/右键在钓鱼或驯马进行中会优先发送对应取消请求并停止后续地图动作；驯马取消请求携带当前目标对象 ID，与旧版 `TamingState.Cancel` 一致。

地图左键/右键进入地图未处理输入前会先发送 `AutoPathCancel`，与旧版 `MapControl.OnMouseDown` 的自动寻路打断顺序一致。

背包“删除”严格保持旧版保护条件：仅允许删除背包物品，锁定物品和婚戒物品拒绝删除，并在发送 `ItemDelete` 前锁定格子等待回包。

商城“充值”按钮现在使用登录回包中的服务器 `Address`，按旧版规则拼接当前角色名后调用系统浏览器打开；没有地址时明确提示服务器未提供充值入口，不再显示一个与旧版动作不一致的静态提示。
