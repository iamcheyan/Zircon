# Godot 客户端操作一致性修复交接文档

更新时间：2026-08-08
工作区：`/home/tetsuya/development/Zircon`

## 1. 交接结论

当前工作已经完成了大量 Godot 客户端操作层的补强，但**整个原版客户端操作一致性任务尚未完成**，不能把当前状态当作最终验收通过。原始任务要求是：对照原版客户端，逐项修复拾取、移动、背包、穿戴、使用、仓库、邮件、NPC、交易、寄售、商城等操作，并且每项修复后检查和测试，直到全部完成。

当前权威任务清单是 [OPERATION_PARITY_TASK.md](OPERATION_PARITY_TASK.md)。该清单仍有 37 项未勾选，后续智能体必须以它为主清单逐项推进，不要只根据本交接文档中“已处理”的描述判断整体完成。

## 2. 当前工作区状态

工作区包含前序工作留下的大量修改，属于用户当前工作的一部分，接手时不要执行 `git reset --hard`、大范围 checkout 或清理未跟踪文件。最近一次状态包含：

- 约 56 个已修改文件，约 1212 行新增、529 行删除。
- 主要修改集中在 `GodotClient/Controls/`、`GodotClient/Scripts/`、`GameScene.cs`、测试场景和 `docs/`。
- 有未跟踪的 Godot 导入目录、shader、`LegacyBlendMaterial.cs` 以及旧 csproj 备份；先确认用途再处理。
- `docs/OPERATION_PARITY_TASK.md` 和其他审计文档本身也有修改，不要用版本库旧内容覆盖。

接手第一步建议执行：

```bash
git status --short
git diff --stat
git diff --check
```

## 3. 已实现或已部分验证的内容

下面的“已处理”表示代码已经有对应实现或专项审计，**不表示对应的大类已经通过完整原版回归**。仍需在真实客户端/服务器流程中补测。

### 3.1 地图、拾取与鼠标优先级

- 当前格拾取优先于攻击，并增加约 250ms 拾取节流。
- 拾取增加观察者、死亡、麻痹、包含关系、龙之 repulse 等状态保护。
- 光标扫描顺序对齐原版 `CheckCursor` 思路：鼠标逻辑格距离 `d=0..3`，并按存活对象、玩家、NPC、怪物、宠物、死亡对象、地面物品等顺序处理。
- Ctrl+左键不再误发观察请求；Ctrl+右键走观察逻辑。
- 自动寻路状态切换/取消、地图右键取消选中物品或金币已处理。
- Alt 采集、钓鱼、驯服、挖矿的状态逻辑已有补强。
- 地图金币丢弃增加余额、数量、观察者、索引和当前状态检查，统一调用 `GameScene.SendCurrencyDrop`。

最近一次专项结果：

```text
[NetworkAudit] PASS ... current-cell pickup priority ... currency-drop bounds ...
[CursorAudit] PASS newest per-cell hit order preserved
```

但地图全量操作矩阵仍未完成，特别是移动/跑步/自动寻路/右键优先级、Shift 静止攻击、Alt 操作取消和真实服务器回包仍要继续。

### 3.2 背包、物品格与物品使用

主要文件：`GodotClient/Controls/DXItemCell.cs`、`GodotClient/Scripts/GameScene.cs`。

- 选中格与视觉状态同步。
- Alt+左键只阻止拾取/移动，Ctrl+中键发送聊天物品链接。
- 点击事件顺序改为只读检查后再执行动作，减少重复发送。
- Shift 拆分默认数量、数量边界、取消、失败解锁和新格分配已有保护。
- 物品使用和冷却处理已补强；净化药水、书籍、`CanUseItem` 等特殊分支已有处理。
- 系统物品、bundle、loot 等特殊使用路由已有处理。
- `ItemMove`、`ItemSplit`、`ItemDelete`、`ItemChanged`、`ItemsChanged` 的失败、过期、迟到回包和身份保护已有补强。
- 仓库、材料、行会、伙伴格子的数量更新会向腰带/自动药水等引用传播。
- QuickInfo、QuickItem、腰带交换/清空/使用、自动药水行链接/清空/更新已有处理。
- 物品移动/删除时会清理相关腰带、自动药水和特殊链接。
- `ToEquipment`、`ToCompanionEquipment` 已改为返回 `bool`，`UseItem` 穿戴分支会使用实际成功结果。
- 戒指/手镯使用辅助逻辑保留原版“优先空槽，否则使用第二槽”的语义。

重点风险：最近的在线装备审计没有形成完整最终日志，不能据此宣布穿戴全通过；必须重新运行并补真实服务端响应、满包、失败和迟到回包场景。

### 3.3 装备、伙伴和婚戒

- 装备类型映射、等级/职业/性别/重量/钓鱼装备槽/状态/坐骑等 `CanWearItem` 限制已有补强。
- 右键卸下装备并回到背包或伙伴背包的路径已有处理。
- 婚戒逻辑、双戒指/双手镯的两个槽位语义已有处理。
- 伙伴背包、伙伴装备回包数组和当前选中伙伴索引已有保护。
- 伙伴仓库存取、释放、解锁、领养等操作增加非法索引及观察者保护。

### 3.4 仓库、材料和邮件

- 安全区、`CanStore`、物品部件/材料存储规则已有处理。
- 仓库容量动态变化、超出容量的禁用格、仓库/材料分页和滚动已有处理。
- 关闭或取消时会清理临时链接和 `SelectedCell`。
- 增加了 `StorageDialog.AuditCancelLinks`。
- 邮件附件数量对话框、发送时 `ItemsChanged` 待处理源格锁定、成功/失败/断线清理已有处理。
- 避免重复 `MailOpened`；禁止空附件领取；`MailGetItem` 以 `(mailIndex, slot)` 去重。
- 邮件发送/领取/删除增加观察者、索引、收件人和金币检查。

专项结果：

```text
[UIStorageAudit] PASS ... capacity=23 edge=True overflow=True cleared=True sourceSlot=-1 selected=False
[UICommunicationAudit] PASS ... mail=first=True failure=True success=True
```

仍需用真实邮件服务器回包验证附件数量、列表刷新、领取后状态和断线重连。

### 3.5 NPC、任务、修理、宝石和强化

- NPC 买入增加观察者、金币、负重、索引、数量边界保护。
- 卖出/全部卖出和筛选逻辑已有补强。
- 普通修理、特殊修理、耐久度和行会资金检查已有处理，失败会解锁。
- 镶嵌/宝石形状、稀有度、目标、类型不匹配检查已有处理。
- 黑铁、矿石、首饰、武器强化，重置、碎片、石头和特殊材料计数路径已有处理。
- 武器打造模板、颜色槽和数量路由已有处理。
- NPC 任务接受、完成、放弃、奖励选择和任务详情增加确认回调、观察者/索引保护。
- 伙伴 NPC 仓库存取已接入当前伙伴索引保护。
- NPC 变更发送层已覆盖 socket、combine、fragment、refine、master refine、accessory、weapon craft、roll、retrieve、buy/sell/repair 等入口。

专项 NPC/UI/伙伴审计曾通过，但完整数量、取消、失败和真实回包矩阵仍未完成。

### 3.6 交易、寄售和商城

- 交易物品/金币源引用、待处理源字典、失败/关闭/迟到响应解锁和身份保护已有处理。
- 交易请求、响应、确认、金币和物品发送层增加观察者、槽位/数量和实时余额保护。
- 交易金币边界专项审计通过：

```text
[UITradeAudit] PASS local/remote gold routing player=1,234 other=5,678
```

- 寄售数量对话框、观察者/源格/数量/价格/确认保护和回包刷新已有处理。
- 商城购买、赠送、收藏、参数、收件人、数量和持续时间显示已有补强。
- 已通过过 `--consignment-audit` 和 `--gamestore-audit`。

注意：正确的测试参数是 `--gamestore-audit`，旧文档中出现的 `--game-store-audit` 不正确。真实服务器交易断线、响应顺序、寄售计数刷新、商城分页/排序/筛选仍未完成。

## 4. 最近验证过的命令

先确保 Godot/.NET 环境和数据库测试数据可用，再执行：

```bash
dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental -warnaserror
git diff --check

timeout 35s ~/.local/bin/godot-mono --headless --path GodotClient \
  --scene Scenes/UITestScene.tscn -- --ui-audit

timeout 35s ~/.local/bin/godot-mono --headless --path GodotClient \
  --scene Scenes/UITestScene.tscn -- --communication-audit --ui-audit

timeout 35s ~/.local/bin/godot-mono --headless --path GodotClient \
  --scene Scenes/UITestScene.tscn -- --storage-audit --ui-audit

timeout 35s ~/.local/bin/godot-mono --headless --path GodotClient \
  --scene Scenes/MapTestScene.tscn -- \
  --network-audit --cursor-audit --skip-sound-audit
```

测试过程可能约 27 秒无输出。数据库缺少 `EventInfo`、`EventTarget`、`EventAction`、`FlagInfo` 等测试数据的警告属于已知噪声；最终必须以对应审计输出 `PASS` 以及进程退出码为准。

## 5. 尚未完成的清单

以下是从 `OPERATION_PARITY_TASK.md` 读取的未勾选项目，必须逐项完成、逐项验证并在原清单中更新复选框。不要因为某个相关代码已有补丁就直接勾选。

### 地图、移动、拾取、光标

- 当前格拾取范围、当前格/鼠标格优先级的完整矩阵。
- NPC/玩家观察、怪物/宠物/死亡对象重叠时的优先级。
- 移动、跑步、自动寻路、右键和 UI 点击优先级。
- Shift 静止攻击、远程攻击、接近目标的完整行为。
- Alt 采集/钓鱼/驯服/挖矿的取消、距离、冷却和失败回包。
- 地图选中金币丢弃的数量对话框、取消、余额边界和失败回包。

### 背包与物品

- 左键拾取/丢弃/交换/空格移动以及点击外部取消。
- 单击移动与双击使用的区分，不能重复发包。
- Shift 拆分上下界、取消、失败解锁和新槽位完整矩阵。
- 右键使用/穿戴/卸下、婚戒传送、特殊物品优先级。
- 中键锁定、Ctrl+中键聊天链接、快捷键锁定。
- bundle/loot/system 改名、发型、染色、幸运、称号等特殊物品。
- 数量、稀有度、部件、过期和不可用物品的显示一致性。

### 装备与腰带

- 全部装备类型双击对应槽位。
- 双戒指/双手镯的空槽、已有装备替换和失败回滚。
- 等级、职业、性别、重量、槽位、坐骑等限制的原版逐项比对。
- 右键卸下、背包满时保留装备并正确提示。
- 装备/腰带链接清理和图标刷新。
- 背包到腰带的 Info/Item 链接。
- 腰带交换、清空、使用时源格、数量和显示刷新。
- 自动药水链接/清空/行更新/服务端响应。
- 物品移动/删除后的所有关联链接清理。

### 仓库、部件、邮件

- 安全区、可存储性、部件存储规则的完整边界。
- 仓库/部件双向移动、交换、堆叠和排序。
- 邮件附件数量、发送/领取/删除、列表刷新和回包顺序。

### NPC、任务、交易、寄售、商城

- NPC 买/卖、数量输入、选择、取消和失败解锁。
- 普通/特殊修理、耐久度和行会资金。
- socket 目标/宝石、合成、类型不匹配。
- 黑铁/矿石/首饰强化、升级、重置。
- 大师强化碎片、石头和特殊材料计数。
- 武器打造模板、颜色槽和数量。
- 任务奖励/物品、NPC 伙伴仓库。
- 交易物品/金币/数量、锁定、确认、取消和断线。
- 寄售链接、价格、数量、行会资金和确认；购买/取消后的刷新。
- 商城购买、赠送、收藏、排序、筛选和分页。
- `ItemUseDelay`、bundle/loot 回包后的状态解锁。

## 6. 推荐后续执行顺序

1. 先编译并运行已有审计，确认当前基线没有回归。
2. 读取原版客户端对应实现，建立每个未勾选项的“原版行为—Godot 行为—服务端包—失败/取消行为—测试证据”五列记录。
3. 优先完成背包/装备/腰带/自动药水，因为它们共享 `DXItemCell` 和 `GameScene` 的物品状态机。
4. 再完成仓库/邮件和 NPC，因为这些依赖物品源格锁、数量输入及服务端回包。
5. 最后完成地图剩余矩阵、交易/寄售/商城以及断线/迟到回包。
6. 每个小项完成后立即运行最小专项审计；若涉及共享发送层，再运行全部相关审计。
7. 只有在真实服务器流程、取消、失败、满格、边界数量、重复点击、断线和迟到回包都验证后，才在任务清单勾选。

## 7. 继续开发时的硬性验收标准

- 不允许只以“能编译”作为完成标准。
- 不允许把本地 UI 状态变化当作服务端操作成功；必须处理成功、失败、取消、断线和迟到回包。
- 每个异步操作都要有源格/源物品身份保护，避免旧回包解锁新操作或清掉新状态。
- 所有数量输入必须覆盖 0、1、最大值、超过最大值、余额不足、负重不足和背包满。
- 观察者、死亡、麻痹、地图切换、关闭窗口和重复点击必须覆盖。
- 修改共享逻辑后，重新跑地图、UI、通信、仓库、NPC、交易、寄售和商城相关审计。
- `git diff --check` 必须通过；最终编译使用 `-warnaserror`。
- 最终报告必须列出每个清单项的代码位置、测试命令、结果和仍存在的限制。

## 8. 关键代码入口

- 地图输入/拾取/移动/发包：`GodotClient/Scripts/GameScene.cs`、`MapTestScene.cs`
- 物品格与背包交互：`GodotClient/Controls/DXItemCell.cs`、`InventoryDialog.cs`
- 装备/伙伴：`CharacterDialog.cs`、`CompanionDialog.cs`、`NPCCompanionStorageDialog.cs`
- 腰带/自动药水：`MainPanel.cs`、`GameScene.cs`
- 仓库/部件：`StorageDialog.cs`
- 邮件/通信：`CommunicationDialog.cs`
- NPC 买卖/修理/强化/打造：`NPCDialog.cs`、`NPCGoodsPanel.cs`、`NPCRepairPanel.cs`、`NPCSocketPanels.cs`、`NPCAdvancedPanels.cs`
- 交易/寄售/商城：`TradeDialog.cs`、`ConsignmentDialog.cs`、`GameStoreDialog.cs`、`GameStoreGiftDialog.cs`
- 测试场景：`GodotClient/Scenes/UITestScene.tscn`、`GodotClient/Scenes/MapTestScene.tscn`
- 原版/差异审计资料：`docs/ORIGINAL_GODOT_PARITY_AUDIT.md`、`docs/IMAGE_ANIMATION_AUDIT.md`、`docs/OPERATION_PARITY_TASK.md`

## 9. 最后提醒

本文件是交接说明，不是最终验收报告。接手智能体应先检查实际 diff 和当前测试输出，再从未勾选清单继续；完成所有项目并补齐证据后，才能宣布原始任务完成。
