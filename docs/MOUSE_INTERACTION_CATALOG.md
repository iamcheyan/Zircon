# 旧版客户端鼠标交互全量目录与 Godot 移植对照(排查清单)

> 用途:逐条排查 Godot 客户端是否完整复刻旧版 DX 客户端的鼠标交互。凡"状态"列为 ⚠️/❌ 的条目都是排查/修复对象;✅ 表示已实现且已核对源码。
> 旧版权威:Client/(WinForms + DXControl 事件体系);Godot 对应:GodotClient/。行号以当前仓库实际文件为准,已实测核对。
> 生成日期:2026-08-08。修改本文件时须同步更新行号。

## 对照速览

| 领域 | 旧版条目 | 状态 |
|---|---|---|
| 世界地图交互 | 17 | 详见 §1 |
| 物品格与窗口控件 | 22 | 详见 §2 |
| 对话框与全局 | 30 | 详见 §3 |
| 已知缺陷与待查 | — | 详见 §4 |

## 1. 世界地图交互

| # | 操作 | 触发方式 | 行为/网络包 | 旧版位置 | Godot 位置 | 状态 |
|---|---|---|---|---|---|---|
| 1.1 | 按住左键走路 | 左键按住(地图上,不在 UI) | 朝鼠标方向走 1 格/600ms `C.Move`;Shift 按住=原地攻击不走;脚下有可点物/采矿条件不拦路 | MapControl.cs:909-919,929-989 | MouseWalker.cs:88-104(轮询);GameScene.cs:602-620(构造拦截) | ✅ |
| 1.2 | 按住右键跑步 | 右键按住(地图上) | 朝鼠标方向跑(负重/骑马步数>2)/600ms `C.Move` | MapControl.cs:1031-1045 | MouseWalker.cs:89,122-128 | ✅ |
| 1.3 | 右键近身转身 | 右键按住且鼠标在玩家 2 格内 | 不移动,`C.Turn` 转身 | MapControl.cs:1039-1043 | MouseWalker.cs:130-136 | ✅ |
| 1.4 | 撞墙绕路 | 按住移动时正方向被阻挡 | `MouseDirectionBest` 按 22.5° 找相邻可行格,不行原地转身 | MapControl.cs:940-947,1003-1011 | MouseWalker.cs:138-158,170-189 | ✅ |
| 1.5 | 点击地面物品拾取 | 左键点地面掉落物(玩家所在格) | `C.PickUp`(250ms 节流) | MapControl.cs:989-997 | GameScene.cs:6011-6019;BlockLeftMouseMovement GameScene.cs:3628-3647 | ✅ |
| 1.6 | 点击 NPC | 左键点 NPC(1s 节流) | `C.NPCCall{ObjectID}` | MapControl.cs:760-770 | GameScene.cs:6044-6059 | ✅ |
| 1.7 | **Ctrl+右键点玩家** | Ctrl 按住 + 右键点其他玩家(2.5s 节流) | `C.Inspect{Index,Ranking}`(查看装备) | MapControl.cs:776-791 | GameScene.cs:6021-6041 | ✅ |
| 1.8 | 左键点怪物攻击 | 左键点 1 格内未死怪物 | 选中目标+近战/远程/接近 | MapControl.cs:589-620(OnMouseDown) | CombatController.cs:163-184 | ✅ |
| 1.9 | Shift+左键点空地攻击 | Shift+左键(无目标) | 朝鼠标方向空挥 `C.Attack` | MapControl.cs:909-919 | CombatController.cs:166-170 | ✅ |
| 1.10 | 选中目标自动攻击 | 无按键时目标存在 | 距离=1 自动近战,>1 自动接近 | ProcessInput.cs(原版无独立实现,在 ProcessInput) | CombatController.cs:118-147 | ✅ |
| 1.11 | 右键取消目标 | 右键按下(设置开启时) | 怪物目标清空(`RightClickDeTarget`) | MapControl.cs:599-604 | CombatController.cs:186-207 | ✅ |
| 1.12 | Alt+左键钓鱼 | Alt+左键,持鱼竿+鱼袍且点在钓鱼区 | `C.FishingCast{Cast}` | MapControl.cs:924-955 | GameScene.cs:5961-5987 | ✅ |
| 1.13 | Alt+左键驯马 | Alt+左键,持驯马索点 AI135 怪物(≤驯马距离) | `C.Taming{Cast}`;超距离无操作 | MapControl.cs:955-967 | GameScene.cs:5988-6002 | ✅(超距已修) |
| 1.14 | Alt+左键采集 | Alt+左键(无鱼竿/驯马索) | `C.Harvest{Direction}` | MapControl.cs:967-969 | GameScene.cs:6003-6009 | ✅ |
| 1.15 | 左键点矿点采矿 | 左键点相邻可采矿格(持镐+地图 CanMine+Flag) | `C.Mining{Direction}`;采矿中持续采集 | MapControl.cs:997-1021 | GameScene.cs:6062-6082 | ✅ |
| 1.16 | 点击地图取消自动寻路/钓鱼/驯马 | 任意左/右键 press | `CancelAutoPath`;钓鱼中→`C.FishingCast{Cancel}`;驯马中→`C.Taming{Cancel}` | MapControl.cs:593-596 | GameScene.cs:5889-5904 | ✅ |
| 1.17 | 拖物品到地图丢弃 | 左键点地图(已拿起背包物品) | 数量窗→`C.ItemDrop{CellLinkInfo}`;腰带格→解绑 `C.BeltLinkChanged`;自动药格→`SendRowUpdate`;货币→`C.CurrencyDrop` | MapControl.cs:605-730 | GameScene.cs:5916-5958 | ✅ |

## 2. 物品格与窗口控件

| # | 操作 | 触发方式 | 行为/网络包 | 旧版位置 | Godot 位置 | 状态 |
|---|---|---|---|---|---|---|
| 2.1 | 拿起/放下物品 | 左键单击有物品格 / 再点同格或空格 | `SelectedCell` 状态机;发 `C.ItemMove` | DXItemCell.cs:818-839,1884-1888 | DXItemCell.cs(Godot):334-343,405-460 | ✅ |
| 2.2 | 移动/合并物品 | 左键点目标格(已拿起) | `C.ItemMove{From,To,Slot,Merge}` | DXItemCell.cs:967-1098 | DXItemCell.cs(Godot):405-460,611-723 | ✅ |
| 2.3 | 装备穿戴 | 左键点装备格(已拿起,背包物品) | `ToEquipment`:校验 `CanWearItem`/`CorrectSlot`,`C.ItemMove` | DXItemCell.cs:888-966 | DXItemCell.cs(Godot):442-446 | ✅ |
| 2.4 | 伙伴装备 | 左键点伙伴装备格 | `ToCompanionEquipment` | DXItemCell.cs:893-921 | DXItemCell.cs(Godot):446 | ✅ |
| 2.5 | 存入仓库/零件仓 | 左键点 Storage/PartsStorage 格 | `C.ItemMove`;类型校验 | DXItemCell.cs:882-887 | DXItemCell.cs(Godot):611-723 | ✅ |
| 2.6 | **Shift+左键拆分** | Shift+左键(背包/仓库/零件/行会仓/伙伴包,Count>1) | 数量窗→`C.ItemSplit` | DXItemCell.cs:1870-1885 | DXItemCell.cs(Godot):338-342 | ✅ |
| 2.7 | Alt+左键物品格 | Alt+左键点物品 | 无操作(旧版占位注释) | DXItemCell.cs:1865-1869 | —(Godot 无此分支,语义=无操作) | ✅ 一致 |
| 2.8 | **左键双击使用/穿戴** | 左键双击 Inventory/装备/Belt/AutoPotion 格 | `UseItem()`→`C.ItemUse` 或 `ToEquipment`;婚戒→`C.MarriageTeleport` | DXItemCell.cs:2541-2574 | DXItemCell.cs(Godot):325-332(按下分支检测双击,已修) | ✅(已修) |
| 2.9 | 右键使用/穿戴 | 右键点物品格 | 背包:按上下文路由(修理/精炼/打孔/出售/寄售/邮件/仓库/交易/行会/伙伴),否则 `UseItem`;装备格:卸下/婚戒传送 | DXItemCell.cs:1908-2524 | DXItemCell.cs(Godot):344-367 | ✅ |
| 2.10 | 中键锁定/解锁 | 中键点物品格 | `C.ItemLock` | DXItemCell.cs:1890-1899 | DXItemCell.cs(Godot):370-376,897-902 | ✅ |
| 2.11 | Ctrl+中键聊天链接 | Ctrl+中键点物品格 | `ChatTextBox.LinkItem`(不发包) | DXItemCell.cs:1893-1896 | DXItemCell.cs(Godot):370-376 | ✅ |
| 2.12 | 悬停物品提示 | 鼠标移入物品格 | 悬浮物品标签(`MouseItem`),清除"新物品"标记,边框高亮 | DXItemCell.cs:1820-1840 | DXItemCell.cs(Godot):386-391 + GameScene 轮询 | ✅ |
| 2.13 | 滚轮滚动物品格 | 滚轮在格上(已订阅容器) | `MouseWheel→DXVScrollBar.DoMouseWheel` | DXItemCell.cs:553-556;各 Dialog 订阅 | DXItemCell.cs(Godot):377-381 | ✅ |
| 2.14 | 悬停格按锁定键 | 格上按 Scroll Lock(默认键位) | `C.ItemLock` | DXItemCell.cs:2576-2593 | KeyBindManager.cs:139(ScrollLock 默认)+GameScene.cs:1414-1419(悬停格) | ✅(本次补) |
| 2.15 | 删除物品 | 左键点背包"垃圾桶"(已拿起) | `C.ItemDelete` | InventoryDialog.cs:364-379 | InventoryDialog.cs(Godot):149 | ✅ |
| 2.16 | 整理背包/仓库 | 左键点"整理" | `C.ItemSort` | InventoryDialog.cs:348-354;StorageDialog.cs:321-331 | InventoryDialog.cs(Godot):158;StorageDialog(Godot) | ✅ |
| 2.17 | 出售模式 | 出售模式点"卖出" | 多选累加→`C.NPCSell` | InventoryDialog.cs:383-431 | NPCGoodsPanel.cs:35-38,117 | ✅ |
| 2.18 | 货币拾取 | 左键点钱包货币标签 | `CurrencyPickedUp` 状态机(再点放回) | InventoryDialog.cs:432-489;CurrencyDialog.cs:246-277 | CurrencyDialog.cs(Godot):144-145;GameScene.cs:5906-5915 | ✅ |
| 2.19 | 窗口拖动 | 左键按住标题栏 | 置顶+跟随拖动 | DXControl.cs:1505-1557;DXWindow.cs:265 | DXWindow.cs(Godot):114-146(标题栏区域) | ✅ |
| 2.20 | 窗口缩放 | 拖拽窗口边缘(AllowResize) | 按边/角调整 Size+Location | DXControl.cs:1586-1627 | DXWindow.cs(Godot):AllowResize 边缘缩放(本次实现) | ✅(本次补) |
| 2.21 | 滚动条 | 点 ▲/▼/轨道/拖滑块/滚轮 | `Value±Change`/跳转/拖动 | DXVScrollBar.cs:192-349 | DXVScrollBar.cs(Godot):119-193 | ✅ |
| 2.22 | 通用控件双击 | 第二次点击(同键 DoubleClickTime 内) | 只发 `MouseDoubleClick`,不发 Click | DXScene.cs:113-158 | DXControl.cs(Godot):259-269(按下分支,已修) | ✅(已修) |

## 3. 对话框与全局交互

| # | 操作 | 触发方式 | 行为/网络包 | 旧版位置 | Godot 位置 | 状态 |
|---|---|---|---|---|---|---|
| 3.1 | ESC 关闭窗口 | 键盘 ESC | `CloseButton.InvokeMouseClick()`→窗口隐藏;`EscapeCloseAll` 关全部 | 各对话框 OnKeyDown | GameScene.cs:6125-6133(WindowManager.CloseTop) | ✅ |
| 3.2 | 主面板 9 按钮 | 左键点主面板按钮 | 开/关背包/人物/魔法/任务/邮件/腰带/组队/菜单/商城 | MainPanel.cs:138-272 | GameScene.cs:3502-3530;MainPanel.cs(Godot) | ✅ |
| 3.3 | 腰带数字键 1-0 | 键盘 1-0(Shift 不冲突) | 有拿起物品→移入腰带格;否则用腰带药 | GameScene.cs:1404-1405 | GameScene.cs(Godot):1466-1467;6080-6087 | ✅ |
| 3.4 | 地图滚轮滚动聊天 | 滚轮在地图上 | 遍历 ChatTab 滚动 | GameScene.cs:457-465 | ChatLogPanel.cs(Godot):55 | ✅ |
| 3.5 | Ctrl+悬停地面物品提示 | Ctrl 按住悬停地面物品 | 显示物品标签(仅 Ctrl 时) | GameScene.cs:1073-1079 | GameScene.cs:5783-5792(Ctrl+悬停地面物品,本次补) | ✅(本次补) |
| 3.6 | 交易拖物品入格 | 拖到己方 TradeUser 格 | `C.TradeAddItem`,格子置 ReadOnly | TradeDialog.cs:105-115 | TradeDialog.cs(Godot):119-129 | ✅ |
| 3.7 | 交易金币 | 左键点己方金币标签 | 数量窗→`C.TradeAddGold` | TradeDialog.cs:183,249-256 | TradeDialog.cs(Godot):39-45 | ✅ |
| 3.8 | 交易确认 | 左键点"确认交易" | `C.TradeConfirm`(禁用至对方确认) | TradeDialog.cs:234-240 | TradeDialog.cs(Godot):45 | ✅ |
| 3.9 | 好友添加/删除 | 左键"添加"输入窗;选中行"删除" | `C.FriendAdd`/`C.FriendRemove` | CommunicationDialog.cs:574-611 | CommunicationDialog.cs(Godot):183,213-220 | ✅ |
| 3.10 | 邮件读信/收取/删除 | 左键行/按钮 | `C.MailOpened`/`C.MailGetItem`/`C.MailDelete` | CommunicationDialog.cs:687-780 | CommunicationDialog.cs(Godot):238-241,273 | ✅ |
| 3.11 | 黑名单添加/移除 | 左键按钮 | `C.BlockAdd`/`C.BlockRemove` | CommunicationDialog.cs:950-985 | CommunicationDialog.cs(Godot) | ✅ |
| 3.12 | 组队邀请/移除 | 左键按钮/成员行 | `C.GroupInvite`/`C.GroupRemove` | GroupDialog.cs:300-344 | GroupDialog(Godot) | ✅ |
| 3.13 | 允许组队/LFG | 左键勾选/LFG 行 | `C.GroupSwitch`/`C.GroupRequest` | GroupDialog.cs:263,460-508 | GroupDialog(Godot) | ✅ |
| 3.14 | NPC 商品单击/双击 | 左键单击/双击商品格 | 单击选中;双击→`C.NPCBuy` | NPCDialog.cs:836-951 | NPCGoodsPanel.cs(Godot):118-119(双击购买,本次补) | ✅(本次补) |
| 3.15 | NPC 页面按钮/选项 | 左键按钮/选项标签 | `C.NPCButton`/`C.NPCRoll` | NPCDialog.cs:487-491,594-611 | NPCTextControl.cs(Godot):84-116 | ✅ |
| 3.16 | 修理页批量放入 | 左键点 Inventory/Equipment/Storage/行会仓标签 | 批量 `MoveItem` 入修理格 | NPCDialog.cs:1486-1552 | NPCRepairPanel.cs(Godot):43-45 | ✅ |
| 3.17 | 修理/精炼/恢复 | 左键按钮+确认 | `C.NPCRepair`/`C.NPCRefine`/`C.NPCRefineRetrieve` | NPCDialog.cs:1615,2071,2403 | NPCRepairPanel/NPCAdvancedPanels(Godot) | ✅ |
| 3.18 | 寄售搜索/购买/下架 | 左键按钮/行 | `C.MarketPlaceBuy`/`C.MarketPlaceCancelConsign` | ConsignmentDialog.cs:232-263,295,339 | ConsignmentDialog(Godot) | ✅ |
| 3.19 | 行会成员行三键 | 左/右/中键点成员行 | 编辑权限 `C.GuildEditMember`/踢人 `C.GuildKickMember`;大地图定位;中键 `C.GroupInvite` | GuildDialog.cs:2639-2671,2934-2952 | GuildDialog.cs(Godot):GuildMemberRow 三键(本次补) | ✅(本次补) |
| 3.20 | 行会城堡/公告/税收 | 左键按钮 | `C.GuildEditNotice`/`C.GuildTax`/`C.GuildToggleCastleGates` 等 | GuildDialog.cs:981-1006,1177-1351,1857-1905 | GuildDialog.cs(Godot):城堡/税收按钮已实现 | ✅ |
| 3.21 | 任务放弃/追踪/里程碑 | 左键按钮/行 | `C.QuestAbandon`;里程碑 `ClaimMilestone` | QuestDialog.cs:872-880,1206-2382 | QuestDialog.cs(Godot):156-248 | ✅ |
| 3.22 | 排行榜行选中/搜索/观察 | 左键行/按钮 | `C.RankSearch`/`C.ObserverRequest` | RankingDialog.cs:774-851 | RankingDialog.cs(Godot):115,60,65-69 | ✅ |
| 3.23 | 大图右键传送 | 右键点大地图 | `C.TeleportRing{Location,Index}` | BigMapDialog.cs:276-285 | BigMapDialog.cs(Godot):64-75 | ✅ |
| 3.24 | **大图双击寻路** | 左键双击大地图 | `C.AutoPathWaypoint{MapIndex,Location}` | BigMapDialog.cs:286-298 | BigMapDialog.cs(Godot):57-63(依赖 DXControl.MouseDoubleClick,已修) | ✅(已修) |
| 3.25 | 大图双击 NPC 图标 | 左键双击 NPC 图标 | `C.AutoPathStart(NPCIndex)` | BigMapDialog.cs:368-372 | BigMapDialog.cs(Godot):58-62(8x8 标记双击,本次补) | ✅(本次补) |
| 3.26 | 大图移动图标切图 | 左键点移动图标 | 切换到目标地图 | BigMapDialog.cs:465-470 | BigMapDialog.cs(Godot) | ✅ |
| 3.27 | 小地图尺寸/透明/大地图 | 左键点小地图按钮 | ToggleSize/ToggleTransparency/ToggleOpen | MiniMapDialog.cs:150-172 | MiniMapDialog.cs(Godot):81-99,330-345 | ✅ |
| 3.28 | 钓鱼按住收杆 | 左键按住鱼钩按钮 | 收杆进度;关闭→`C.FishingCast{Cancel}` | FishingDialog.cs:280-283,435-441 | FishingDialog.cs(Godot):77-84 | ✅ |
| 3.29 | 驯马套索动画点击 | 左键点套索动画 | 角度判定→进度,满 100→`C.TamingSuccess` | HorseTameDialog.cs:252-285 | HorseTameDialog.cs(Godot):角度判定完整重写(本次补) | ✅(本次补) |
| 3.30 | 魔法图标点击/按键绑定 | 左键点图标 / 按快捷键 | 清除绑定 `C.MagicKey` / 绑定快捷键 | MagicDialog.cs:598-628 | MagicBar.cs(Godot):143-161;MagicDialog.cs(Godot):313-318 | ✅ |
| 3.31 | 聊天模式/选项 | 左键点聊天模式按钮 | 循环 ChatMode(7 种)/开关选项 | ChatTextBox.cs:99-111 | ChatTextBox.cs(Godot) | ✅ |
| 3.32 | 死亡复活 | 左键点复活提示 | `C.TownRevive` | ChatTab.cs:438-441 | GameScene.cs(Godot)(复活流程) | ✅ |
| 3.33 | 退出窗 | 左键按钮 | `C.Logout`/关闭客户端(战斗 10s 内拒绝) | ExitDialog.cs:70-102 | 退出流程(Godot) | ✅ |

## 4. 已知缺陷与待查项

### 已修复(本次)

| 项 | 问题 | 修复 |
|---|---|---|
| A. DXControl 双击 | Godot `DoubleClick` 只在第二次按下为 true,原代码在 release 分支检测 → `MouseDoubleClick` 永不触发(大图双击寻路失效) | 改到按下分支检测;同时抑制本次 release 的 `MouseClick`(对齐旧版"双击不发 Click")——DXControl.cs:255-269 |
| B. DXControl 拖拽粘滞 | 鼠标在控件外松开时 `_GuiInput` 不再到达 → `IsPressed/_dragging` 卡死(滚动条滑块/窗口粘住) | `_Process` 轮询左键,松开即复位——DXControl.cs:201-215 |
| C. DXWindow 拖动释放 | 同上,标题栏拖动在窗口外松开会粘滞 | `_Process` 复位 `_moving`——DXWindow.cs:103-110 |
| D. Alt+左键驯马超距 | 驯马索对超距离 AI135 怪物原本错误地落到 `C.Harvest`(旧版为无操作) | 超距直接吞事件——GameScene.cs:5988-6002 |

### 待查(本次已全部核实/实现)

| 项 | 旧版行为 | 结论 |
|---|---|---|
| E. Scroll Lock 锁定物品 | 悬停物品格按 Scroll Lock 发 `C.ItemLock` | ✅ 已补:默认键位 ScrollLock→ToggleItemLock(KeyBindManager.cs:139);语义改为**悬停格**(`DXControl.MouseControl as DXItemCell`,GameScene.cs:1414-1419);前置校验补齐(锁定/链接源格/只读,DXItemCell.cs:967-978) |
| F. 窗口边缘缩放 | 拖拽窗口边缘调整大小 | ✅ 已实现:DXWindow.cs 通用 AllowResize 边缘缩放(6px 命中、四边+四角、视口钳制、GetAcceptableResize 吸附);接入 ChatOptions/ChatTextBox(仅横向)/MiniMap(大图模式 150-300)/QuestTracker;Belt 保留原有右下角缩放 |
| G. Ctrl+悬停地面物品提示 | Ctrl 按住悬停地面物品显示物品标签 | ✅ 已实现:GameScene.cs 每帧检测 `Ctrl && MouseObject.Kind.Item` → 显示 DisplayName 标签(旧版 GameScene.cs:1073-1079) |
| H. NPC 商品双击购买 | 双击商品格 `C.NPCBuy` | ✅ 已实现:NPCGoodsPanel.cs 行绑定 `MouseDoubleClick → BuySelected()` |
| I. 行会成员行三键 | 左/右/中键分别编辑权限/大地图定位/组队邀请 | ✅ 已实现:GuildDialog.cs GuildMemberRow 控件(区分三键)+ GameScene.ShowGuildMemberOnMap(定位限同地图在线成员) |
| J. 大图 NPC 图标双击 | 双击 NPC 图标 `C.AutoPathStart` | ✅ 已实现:BigMapDialog.cs NPC 标记 3x3→8x8 可点 + MouseDoubleClick → SendAutoPathStart(旧版 BigMapDialog.cs:368-372) |
| K. 驯马套索角度判定 | 点击角度 vs 目标角度 ±10-20 进度 | ✅ 已实现:HorseTameDialog.cs 完整重写:随机延迟(1-5s)显示目标角度提示(7620+角)、点击帧判定(7600+角)、±10-20、结果动画(7610+角)、初始进度随机;旧版 ResultFrameCount/AnimationFrameDuration 对齐 |
| L. 右键菜单等效 | 旧版无上下文菜单,右键按控件语义 | ✅ 语义一致(物品格右键使用/地图右键跑步) |
| M. 腰带数字键 1-0 | Shift 按住时跳过(打聊天) | ✅ 键位表 UseBelt01-10 默认 Shift+1..0(KeyBindManager.cs:137-146),与旧版 Shift 语义一致 |

## 附录:验证方法

- 协议覆盖审计:对照 `Client` 中实际 `Enqueue(new C.X)` 的 X,检查 GodotClient 是否存在同一 X 的发送入口。
- 双击/拖拽:实际点击验证(见 §4 已修复项)。
- 待查项(E-M)逐项在 Godot 源码中确认实现或标注缺失,再决定是否补实现。
