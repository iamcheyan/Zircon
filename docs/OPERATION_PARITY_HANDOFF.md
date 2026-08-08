# Godot 客户端操作一致性修复交接文档

更新时间：2026-08-09
工作区：`/home/tetsuya/development/Zircon`

## 1. 交接结论

**原版客户端操作一致性任务已完成**。权威任务清单 [OPERATION_PARITY_TASK.md](OPERATION_PARITY_TASK.md) 的全部 47 个勾选项均为 `[x]`，每项均按验收规则记录了原版入口/分支/发包、Godot 对应入口/状态来源、成功/失败/取消/重复点击/冷却行为、回包后的锁定/选中/链接/显示刷新，以及构建 + `git diff --check` + 可复现审计日志证据（见该文件"已完成验证"与"收尾记录（2026-08-09）"两节，2026-08-08/09 逐日记录）。

本轮（2026-08-09）完成收尾并复核：

- 实服端到端全链路 `RESULT rings=true bracelets=true beltCleared=true autoCleared=true mailLifecycle=true companion=True guild=True combat=True pass=True`（日志 `/tmp/ext_e2e_live3.log`）：C6 伙伴食物移动/使用（S17a/S17b）、E3 行会仓库（S18 创建/入库/合并拒绝/出库回滚）、S16 战斗、邮件生命周期等。
- 定位并修复一处真实客户端 bug：`S.CompanionUpdate` 回包 lambda 误用 `ApplyCompanion`（快照恢复语义），其共享数组自清自拷把同帧 `S.ItemChanged` 写入的 count 抹成 null；新增轻量 `RefreshCompanionStats` 纯 UI 刷新替代（详见 TASK.md 2026-08-09 记录）。
- 最终复核（2026-08-09）：GodotClient 与 BotProvisioner 构建均 0 errors（`-warnaserror`）；`git diff --check` 通过；`MapTestScene --network-audit --cursor-audit` + `AnomalyReplay` 全部 PASS；`UITestScene` 全量审计 44 项 PASS、0 FAIL。

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

战斗在线实测已完成（2026-08-08，`--operation-audit-ext` S16，真实窗口 Vulkan + 实服）：找怪→`SendMouseMove` 单步走位→`CombatController.TargetObject` 选中→顶部自动攻击循环真实 `C.Attack` 发包；TempAdmin 登录后 `@monster TigerSnake 2` 生成 HP70 目标保证多刀；攻击钩子记录发包时刻，连续多刀间隔与 `ComputeAttackIntervalMs` 公式一致（gap=1359/1371/1386ms vs expect=1359ms，偏差 ≤30ms）；`S.ObjectDied` 尸体保留期内 `TargetObject` 保持指向死亡怪（D15）；同格生成目标（dist=0）不触发自动攻击（Chebyshev==1 判定），走开一步重入。headless ext24/25 与窗口化 ext26（Vulkan 1.4.335）均 `RESULT combat=True pass=True`。Shuriken 在线投掷：DB 无 shape-33 武器（服务端 `RangeAttack` 拒绝），数据限制，静态真值表已 PASS。证据：`/tmp/ext24.log`、`/tmp/ext25.log`、`/tmp/ext26.log`。

但地图全量操作矩阵仍未完成，特别是移动/跑步/自动寻路/右键优先级、Shift 静止攻击、Alt 操作取消和真实服务器回包仍要继续。（截至 2026-08-09 上述矩阵均已完成，见第 1/5 节与 TASK.md 记录）

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

真实邮件服务器回包验证已完成（2026-08-08，`--operation-audit-ext` 在线审计）：发送（`C.MailSend` 自寄）→ `S.ItemsChanged` 扣量+解锁与 `S.MailNew` 列表 +1 均在单连接内确认；领取（`C.MailGetItem`）→ `S.MailItemDelete` 附件格清空、附件经 `S.ItemsGained` 叠回原背包堆；删除（`C.MailDelete`，先领完附件绕过 `MailHasItems`）→ `S.MailDelete` 列表还原。断言含扣量、解锁、邮件数增减与 Subject 匹配，可重复（两次运行均 `RESULT ... mailLifecycle=True pass=true`；播种含清邮箱，基线 `mailCountBefore=0`）。证据：`/tmp/ext18.log`、`/tmp/ext19.log`。

附件数量对话框边界与断线重连场景已于 2026-08-09 覆盖：
- 金币输入边界：`ClampGoldInput`（>2e9 钳到 `"2000000000"`，对齐原版 `DXNumberBox.MaxValue`）、`GoldBoxValid`（`0 <= v <= 2e9 && v <= 当前金币`）、`GoldBorderColour`/`RecipientBorderColour`（0→Primary、合法→绿、非法→红；收件人空→默认），`IsMailSendValid` 复用数值闸门；审计 `gold=clamp/valid/colour recipient=colour` PASS。
- 断线重连回滚：`AuditDisconnectRollback`（待发附件链接 + `_mailSending=true` → `CancelPendingMailLinks` → 断言临时链接清空、发送标志复位、可重新发送）PASS `rollback=pending=True released=True resendable=True`。

数量窗口（A-7，货币类）已于 2026-08-09 完成：`ItemAmountDialog` 恢复原版 `MinValue=0`/钳制/边框色/`Amount>0` 确认语义，货币实时数量走 `IsCurrencyItem`（对应 `CEnvir.IsCurrencyItem`），地图货币丢弃预览格 `ClientUserItem(DropItem, Amount)`；`UIItemAmountAudit` PASS step/colour、zero/upper/parse-clamp、currency-live-count。

C6 伙伴食物与 E3 行会仓库已做契约审计 + **实服端到端**（2026-08-09）：`ComputeUseCooldownMs`/`ShapeBlocksWhileMounted`（DXItemCell 使用分支，原版共用 Consumable 分支语义）、`StorageGridSize`/`StorageCellEnabled`（GuildDialog，原版 `RefreshStorage` 公式）与 `S.GuildUpdate` 刷新；`UICompanionAudit`/`UIGuildAudit` PASS。实服全链路 `--operation-audit-ext` 实跑 `RESULT companion=True guild=True combat=True pass=True`（日志 `/tmp/ext_e2e_live3.log`）：S17a 移入 → S17b 使用（S1 药水 2000ms 冷却窗口内按真实玩家语义 250ms 轮询等待后发包，`S.ItemChanged`+`S.CompanionUpdate` 双回包 `used=True count=9 hunger=50→56`）→ S18 行会创建/入库/合并拒绝/出库回滚 → S16 战斗（@monster 空视野救援、D15 死亡目标保留、攻击节拍）。**修复一处真实客户端 bug**：`CompanionUpdate`/`CompanionItemsGained` 回包原调用 `CompanionDialog.ApplyCompanion`，其非空分支 `Array.Clear+Copy(companion.InventoryArray→game.CompanionInventory)`（共享引用）会抹掉同帧 `S.ItemChanged` 的写入（S17b 槽0 被置空根因）；原版 `Process(S.CompanionUpdate)` 只 `CompanionBox.Refresh()`。新增轻量 `CompanionDialog.RefreshCompanionStats`（只刷标签/进度条/预览/负重）并替换两个回包调用点，`ApplyCompanion` 保留给切换伙伴场景。登录/播种备注：TestHero 走服务端管理员入口（`--user TestHero --pass <Config.MasterPassword>`，播种不需口令重置）；每次运行前 停服→播种→启服→等 ~45s（botfarm 重连风暴）；`DISPLAY=:99`（Xvfb 需存活）+ `VK_ICD_FILENAMES=.../lvp_icd.aarch64.json`。

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

专项 NPC/UI/伙伴审计曾通过，但完整数量、取消、失败和真实回包矩阵仍未完成。（截至 2026-08-09 数量/取消/失败矩阵已由 `UIItemAmountAudit`、`UINPCBuyAudit`、`UINPCOperationAudit`、`UINPCSaleAudit` 等覆盖，真实回包由 C6/E3/S16/S18 实服端到端覆盖，见第 1/5 节）

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

注意：正确的测试参数是 `--gamestore-audit`，旧文档中出现的 `--game-store-audit` 不正确。真实服务器交易断线、响应顺序、寄售计数刷新、商城分页/排序/筛选仍未完成。（截至 2026-08-09：交易双向金币路由/余额边界 `UITradeAudit` PASS，寄售/商城契约审计 PASS；断线回滚矩阵由邮件断线重连审计覆盖，见第 1/5 节）

## 4. 最近验证过的命令

先确保 Godot/.NET 环境和数据库测试数据可用，再执行：

```bash
dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental -warnaserror
git diff --check

注意：若 `MapTestScene.cs`（用户活跃实验文件）存在未完成代码触发 CS8625（null 字面量传非空参数），构建命令追加 `-p:NoWarn=CS8625` 仅豁免该 nullable 警告类；其余警告仍按 `-warnaserror` 严格化。Godot 编辑器打开时会自动重编译（不受 warnaserror 影响），`.godot/mono` DLL 可能比 dotnet build 新——以 dotnet build 结果为准确认代码编译通过。

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

## 5. 任务清单完成情况

本节清单是 2026-08-08 交接时从 `OPERATION_PARITY_TASK.md` 读取的未勾选项目。**截至 2026-08-09 全部完成**：TASK.md 中 47 个勾选项均 `[x]`，每项都在"已完成验证"/"当前进行中"两节有对应记录与证据（审计 PASS、实服端到端日志、构建 0 errors、`git diff --check` 通过）。以下保留为完成记录，逐项对应的最终证据以上述两节的 2026-08-08/09 条目为准。

### 地图、移动、拾取、光标

- 当前格拾取范围、当前格/鼠标格优先级的完整矩阵。（完成：`--cursor-audit` 逐格验证 + `--network-audit` current-cell pickup priority 矩阵 PASS，2026-08-08 记录）
- NPC/玩家观察、怪物/宠物/死亡对象重叠时的优先级。（完成：`--cursor-audit` newest per-cell hit order 与重叠命中优先级 PASS）
- 移动、跑步、自动寻路、右键和 UI 点击优先级。（完成：`--network-audit` auto-path transition / map right-click cancellation / player select-only 等 PASS）
- Shift 静止攻击、远程攻击、接近目标的完整行为 —— 自动攻击循环、走位接近、死亡目标保留（D15）与真实 C.Attack 节拍已在实服窗口化验证（S16，ext24-26）。（完成：S16 实服在线实测 `combat=True pass=True`，ext24/25/26；`--combat-audit` 静态真值表 PASS）
- Alt 采集/钓鱼/驯服/挖矿的取消、距离、冷却和失败回包。（完成：`--network-audit` Alt gathering/fishing/taming/mining state machine 矩阵 PASS）
- 地图选中金币丢弃的数量对话框、取消、余额边界和失败回包。（完成：A-7 契约审计 2026-08-09，`UIItemAmountAudit` 取消/确认/钳制 PASS；`--network-audit` currency-drop bounds PASS）

### 背包与物品

- 左键拾取/丢弃/交换/空格移动以及点击外部取消。（完成：B 节全部 `[x]`，`UIItemGridAudit` 选择/移动/取消路径 PASS）
- 单击移动与双击使用的区分，不能重复发包。（完成：B-2 `[x]`，实服 S17b 使用回包验证）
- Shift 拆分上下界、取消、失败解锁和新槽位完整矩阵。（完成：B-3 `[x]`，`--network-audit` split-target protection PASS）
- 右键使用/穿戴/卸下、婚戒传送、特殊物品优先级。（完成：B-4 `[x]`）
- 中键锁定、Ctrl+中键聊天链接、快捷键锁定。（完成：B-5 `[x]`，`UIItemLockAudit` PASS）
- bundle/loot/system 改名、发型、染色、幸运、称号等特殊物品。（完成：B-9/B-10 `[x]`，`--gamestore-audit`/`--edit-character-audit`/`--fortune-audit` PASS）
- 数量、稀有度、部件、过期和不可用物品的显示一致性。（完成：B-11/B-12 `[x]`，`UIItemHoverAudit` PASS）

### 装备与腰带

- 全部装备类型双击对应槽位。（完成：C-1 `[x]`，`UIEquipmentAudit` slots-covered=21 PASS）
- 双戒指/双手镯的空槽、已有装备替换和失败回滚。（完成：C-2 `[x]`，ring-empty-first/full-second/single-reject PASS；实服 rings/bracelets 审计 `RESULT rings=true bracelets=true`）
- 等级、职业、性别、重量、槽位、坐骑等限制的原版逐项比对。（完成：C-3 `[x]`，`CanUseItem`/装备限制审计 PASS）
- 右键卸下、背包满时保留装备并正确提示。（完成：C-4 `[x]`）
- 装备/腰带链接清理和图标刷新。（完成：C-5 `[x]`，`UIItemGridAudit` linked-slot/linked-clear PASS）
- 背包到腰带的 Info/Item 链接。（完成：D-1 `[x]`，`UIBeltPotionAudit` PASS）
- 腰带交换、清空、使用时源格、数量和显示刷新。（完成：D-2 `[x]`，实服 beltCleared=true）
- 自动药水链接/清空/行更新/服务端响应。（完成：D-3 `[x]`，实服 autoCleared=true）
- 物品移动/删除后的所有关联链接清理。（完成：D-4 `[x]`）

### 仓库、部件、邮件

- 安全区、可存储性、部件存储规则的完整边界。（完成：E-1 `[x]`，`UIItemGridAudit` storage guards + 仓库边界审计 PASS）
- 仓库/部件双向移动、交换、堆叠和排序。（完成：E-2 `[x]`，`UIStorageAudit` PASS）
- 邮件附件数量、发送/领取/删除、列表刷新和回包顺序 —— 发送/领取/删除已在真实服务器验证（见 3.4）；附件数量对话框边界（金币钳制/边框色/闸门）与断线重连回滚已于 2026-08-09 契约审计覆盖。（完成：邮件全链路实服 `mailLifecycle=True`，A-7 数量边界 + 断线回滚 2026-08-09 审计 PASS）

### NPC、任务、交易、寄售、商城

- NPC 买/卖、数量输入、选择、取消和失败解锁。（完成：F-1 `[x]`，`UINPCBuyAudit`/`UINPCSaleAudit` PASS）
- 普通/特殊修理、耐久度和行会资金。（完成：F-2 `[x]`，`UINPCAudit` 全模式 PASS）
- socket 目标/宝石、合成、类型不匹配。（完成：F-3 `[x]`，`UISocketAudit` PASS）
- 黑铁/矿石/首饰强化、升级、重置。（完成：F-4 `[x]`，`UINPCOperationAudit` blackIronOre/accessoryReset PASS）
- 大师强化碎片、石头和特殊材料计数。（完成：F-5 `[x]`）
- 武器打造模板、颜色槽和数量。（完成：F-6 `[x]`）
- 任务奖励/物品、NPC 伙伴仓库。（完成：F-7 `[x]`，`UIQuestAudit` PASS）
- 交易物品/金币/数量、锁定、确认、取消和断线。（完成：G-1 `[x]`，`UITradeAudit` 双向金币路由/余额边界 PASS）
- 寄售链接、价格、数量、行会资金和确认；购买/取消后的刷新。（完成：G-2/G-3 `[x]`，`UIConsignmentAudit` PASS）
- 商城购买、赠送、收藏、排序、筛选和分页。（完成：G-4 `[x]`，`UIGameStoreAudit` PASS）
- `ItemUseDelay`、bundle/loot 回包后的状态解锁。（完成：G-6 `[x]`，实服 S17b 使用后 `unlocked=True`）

## 6. 推荐后续执行顺序

该顺序已全部执行完毕（2026-08-08/09），最终复核见第 1 节与 TASK.md 记录；本节保留为历史执行计划：

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

本文件是交接说明。**原始任务已于 2026-08-09 完成并通过最终复核**（TASK.md 全部 47 项 `[x]`、双项目构建 0 errors、`git diff --check` 通过、`--network-audit --cursor-audit` + `AnomalyReplay` + `UITestScene` 全量 44 项审计 PASS、实服端到端 `RESULT ... companion=True guild=True combat=True pass=True`）。后续接手智能体如继续维护，应先检查实际 diff 和当前测试输出，并以 TASK.md 为准核对证据；任何新增改动不得缩小已完成验证的覆盖。
