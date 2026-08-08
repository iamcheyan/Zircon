# Godot 客户端操作一致性修复任务

## 目标

以 `Client/` 原版客户端为行为基准，逐项恢复 `GodotClient/` 的鼠标、键盘、拖放、双击、上下文路由和网络回包行为。任何项目只有在“源码对照、实现修复、编译通过、针对性运行验证”全部完成后才可勾选。

## 验收规则

每一项必须记录：

1. 原版入口文件、分支条件和发送的客户端包。
2. Godot 对应入口、状态来源和 UI/地图坐标转换。
3. 成功、失败、取消、重复点击和冷却状态的行为。
4. 回包成功与失败时的锁定、选中、链接和显示刷新。
5. `dotnet build`、`git diff --check`，以及可重复的 Godot 审计或运行日志。

## 任务清单

### A. 地图与世界点击

- [x] 地面掉落物左键拾取；地图点击与 Tab 共用 250ms 节流。
- [x] 拾取范围、脚下拾取、鼠标所在格拾取与原版 `CheckCursor` 的优先级逐格验证。
- [x] NPC、玩家观察、怪物攻击、宠物和死亡对象的重叠命中优先级验证。
- [x] 普通移动、跑步、自动寻路、右键取消目标和 UI 优先级验证。
- [x] 按住 Shift 原地攻击与远处目标接近/远程攻击验证。
- [x] Alt 采集、钓鱼、驯服、挖矿的状态取消、距离和冷却验证。
- [x] 地图货币选择后丢弃数量弹窗及取消路径验证。

### B. 背包基础操作

- [x] 左键拿起、放下、交换、空格移动和点击窗口外取消。
- [x] 双击使用与单击移动不重复发包。
- [x] Shift 分堆：数量边界、取消、失败解锁和新格刷新。
- [x] 右键使用、装备卸下、婚戒传送和特殊窗口优先级。
- [x] 中键锁定、Ctrl+中键聊天链接、快捷键锁定。
- [x] 消耗品使用冷却与 `ElixirOfPurification` 原版例外。
- [x] 书籍从背包发送 `ItemUse`，不再被客户端错误拦截。
- [x] 已链接工艺格/腰带点击解除链接；自动药水校验 `CanAutoPot`；Shift 分堆覆盖行会仓库。
- [x] 礼包、宝箱、系统改名/改发型/染色/命运检查/称号物品。
- [x] 物品数量、稀有度、部件、过期时间和不可用标记显示。
- [x] 堆叠比较补齐 `ExpireTime`、Flags 和 AddedStats 条件。

### C. 装备与伙伴

- [x] 所有装备类型到正确装备槽的双击穿戴。
- [x] 双戒指、双手镯空槽选择及已有物品替换。
- [x] 等级、职业、性别、负重、槽位和坐骑限制。
- [x] 装备右键卸下到背包；背包满时保持原装备不丢失。
- [x] 装备/卸下后腰带链接清理与图标刷新。
- [x] 伙伴背包、伙伴装备的 `ItemMove` 回包数组、解锁和刷新路径已接通；食物移动/使用契约审计已通过（使用冷却 `Max(250,Durability)`、骑马 Shape 19-22、真实 DB 伙伴食物样本），实服端到端已完成（S17a 移入→S17b 使用→`S.ItemChanged`+`S.CompanionUpdate` 双回包 `used=True count=9`，见下文 2026-08-09 记录）。

### D. 腰带与自动药水

- [x] 背包物品拖入腰带建立 Info/Item 链接。
- [x] 腰带内部交换、清空、使用本体和数量刷新。
- [x] 自动药水链接、清除、行更新和服务端回包。
- [x] 物品从背包移走或删除时相关链接清理。

### E. 仓库、行会仓库与邮件

- [x] 安全区限制、可存储限制和部件仓库分流。
- [x] 仓库/部件仓库双向移动、交换、堆叠和整理。
- [x] 行会仓库存取回包支持 `GuildStorage`，容量/资金/失败回滚契约审计已通过（`GridSize=(11,Max(20,Ceil(Limit/14)))` 公式、超容量格禁用、`S.GuildUpdate` 回包刷新 StorageLimit/资金），实服端到端已完成（S18 创建行会→入库→合并拒绝 `rejected=True`→出库回滚，见下文 2026-08-09 记录）。
- [x] 邮件附件数量弹窗、发送、领取、删除和状态刷新。

### F. NPC 与加工窗口

- [x] 购买、出售、数量弹窗、选中状态和取消。
- [x] 修理普通物品、特殊修理、耐久限制和行会资金。
- [x] 镶嵌目标/宝石、宝石合成和类型不匹配。
- [x] 黑铁/矿石精炼、饰品精炼/升级/重置。
- [x] 大师精炼碎片/精炼石/特殊材料数量规则。
- [x] 武器制作模板、颜色槽和数量规则。
- [x] 任务奖励、任务物品和 NPC 伙伴存取。

### G. 交易、寄售与商城

- [x] 交易物品/金币添加、数量、锁定、确认、取消和断线。
- [x] 寄售物品链接、价格、数量、行会资金选项和确认。
- [x] 寄售购买/下架数量及回包刷新。
- [x] 商城购买、赠送、收藏、排序、分类和分页。

### H. 回包与生命周期

- [x] `ItemMove` 成功/失败均解除源格和目标格锁定。
- [x] `ItemUseDelay`、礼包、宝箱回包后解除锁定。
- [x] `ItemDelete`、`ItemSplit`、整理和地图拾取后清理选中状态。
- [x] 地图切换、断线、重连和窗口关闭不残留旧链接或旧事件订阅。
- [x] 重复包、迟到包和失败包不会覆盖较新的物品状态。

## 已完成验证

- 2026-08-08：拾取入口统一 250ms 节流，覆盖地图点击和 Tab。
- 2026-08-08：书籍恢复原版背包 `ItemUse` 路径。
- 2026-08-08：恢复净化药剂绕过本地使用冷却的原版行为。
- 2026-08-08：物品堆叠和服务端合并回包补齐 `ExpireTime` 一致性检查。
- 2026-08-08：`GetGrid`、锁定解除和统一刷新补齐 `GuildStorage`、`CompanionInventory`、`CompanionEquipment`，避免服务端成功移动后 Godot 客户端丢弃回包。
- 2026-08-08：伙伴装备补齐当前伙伴存在性、伙伴等级/最大等级与 CompanionSlot 校验；维修和特殊窗口补齐婚戒、耐久、冷却及错误路由拦截。
- 2026-08-08：高级工艺窗口按目标/矿石/材料类型路由，饰品材料补齐同款、绑定状态和附加属性一致性校验；邮件、交易、寄售和行会仓库补齐来源与可交易限制。
- 2026-08-08：镶嵌补齐宝石形状、稀有度、目标类型、槽位和同款宝石合成校验；腰带/自动药水链接数量同步伙伴背包。
- 2026-08-08：交易 `TradeAddItem` 回包补齐成功/失败源格锁定与解锁、失败链接清理、关闭交易解锁；对方物品按首个空交易格显示。邮件成功回包补齐附件源格解锁、链接清理和表单重置。
- 2026-08-08：维修提交按原版锁定来源格并清空临时维修格；`NPCRepair` 成功回包更新普通/特殊耐久，失败回包仅解锁来源；高级工艺回包统一清理临时链接格。
- 2026-08-08：修复 `DXItemGrid` 创建后 `GridType`/`ReadOnly` 未传播到已创建格子的基础问题；仓库、部件仓库、行会仓库右键取回背包；堆叠条件改为原版四类标记、过期时间和附加属性。
- 2026-08-08：统一特殊链接格按目标 `Slot` 写入临时数组，修复交易、邮件附件和多格精炼材料写入第 0 格导致的显示/提交错误。
- 2026-08-08：饰品升级/重置/饰品精炼按原版区分“先解锁临时链接”和后续 `ItemsChanged` 成功扣除；显式失败的精炼/大师精炼/武器制作回包不再误扣材料。
- 2026-08-08：伙伴仓库召回/释放改为按当前选中伙伴 Index 发包；伙伴收起同步清除 `CharacterName`。NPC 商店购买恢复原版余额、数量和背包负重限制。
- 2026-08-08：所有已接入的 `ItemMove`、`ItemSplit`、`ItemDelete`、`ItemChanged`、`ItemsChanged` 回包统一解除 Locked 与 Selected，失败包不再残留选中状态。
- 2026-08-08：Godot 客户端构建通过，0 errors、0 warnings。
- 2026-08-08：`MapTestScene --action-audit --skip-sound-audit` 通过全部动作链、技能帧、天气、透明度、图层和施法时序审计；`UITestScene --ui-audit` 通过缩放锚点与 HUD 点击命中审计。
- 2026-08-08：`UITestScene --ui-audit` 额外通过 `UIItemGridAudit`，验证网格类型、只读状态传播、选中同步和已链接目标格解除。
- 2026-08-08：断线清理统一回收邮件附件、维修/高级 NPC 临时链接、宝石/宝石合成来源锁、寄售待处理来源锁；地图切换清除当前选中格；所有回收路径复用来源解锁入口。
- 2026-08-08：对照原版 `CanWearItem` 移除 Godot 额外且错误的 MaxLevel/重生等级二次判断，避免达到重生条件时错误放行装备。
- 2026-08-08：腰带快捷键改为调用腰带格 `UseItem` 并定位背包本体，恢复原版的坐骑限制、使用冷却、净化药剂例外和来源锁定链路。
- 2026-08-08：按原版 `DXItemCell.OnMouseClick` 的实际顺序，将 `ReadOnly` 判断恢复到链接清理之前，避免只读检查窗口被点击时意外改写临时链接。
- 2026-08-08：将 `ItemMove`、`ItemSplit`、`ItemDelete`、`ItemChanged` 回包的来源/目标解锁前置到网格与槽位校验之前，非法、迟到或重复回包也不会遗留操作锁。
- 2026-08-08：修正大师精炼第三类碎片数量规则；原版仅第一、第二类固定 10 个，第三类保留数量选择，不再被 Godot 错误固定为 1 个。
- 2026-08-08：NPC 普通关闭仅清理未提交的临时链接；断线路径才释放已提交维修请求的来源锁，避免关闭窗口时提前解除服务端仍在处理的操作。
- 2026-08-08：临时加工格和交易展示格改为复制完整物品状态而非仅复制 `ItemInfo`，保留绑定、附加属性、过期时间等原版链接校验所需数据。
- 2026-08-08：地图命中改按原版 `CheckCursor` 以鼠标逻辑格为中心的 `d=0..3` 顺序扫描，活对象优先，死亡/宠物与掉落物后备；移除错误的“以玩家为中心且超过 3 格即不命中”逻辑。
- 2026-08-08：玩家观察恢复原版仅由 Ctrl+右键触发；Ctrl+左键不再抢占拾取、NPC 或攻击语义，仍保留 2.5 秒重复请求节流。
- 2026-08-08：只读物品格恢复原版“先派发点击事件、后阻止移动”的顺序，修复收件箱附件格点击不发送 `MailGetItem` 的问题。
- 2026-08-08：`UITestScene --ui-audit` 新增只读格按下/释放回归，验证点击事件恰好触发一次且不进入 `SelectedCell`。
- 2026-08-08：仓库关闭按钮和窗口关闭生命周期恢复原版 `Grid.ClearLinks` 语义，清理未提交仓库临时链接、释放来源并清除仓库选中格。
- 2026-08-08：恢复 NPC BuySell 的可见出售提交入口；背包右键选择可出售物品后发送批量 `NPCSell`，来源格在请求期间锁定并由 `ItemsChanged` 回包解除。
- 2026-08-08：NPC 地图点击恢复原版按下记录、左键释放后发送 `NPCCall` 的时序；点击后移出 NPC 不再提前打开对话；`MapTestScene --cursor-audit --network-audit --skip-sound-audit` 通过。
- 2026-08-08：NPC 出售 pending 请求纳入窗口关闭/断线清理，并在 `ItemsChanged` 到达时移除 pending 链接，避免出售失败或断线后背包格永久锁定。
- 2026-08-08：NPC 出售路由补齐当前 `NPCPage.Types` 类型过滤，客户端不再把服务端必然拒绝的不可出售类型加入批量链接。
- 2026-08-08：腰带全量链接刷新先清空所有 `QuickInfo/QuickItem`，修复重连、角色切换或空槽回包后旧腰带图标和快捷键残留。
- 2026-08-08：恢复原版 `RefreshItem` 的背包/伙伴背包级联刷新；拾取、使用、分堆或删除后腰带与自动喝药链接槽的汇总数量立即同步。
- 2026-08-08：自动喝药链接槽拖出时按原版清除当前行并发送行更新，避免把配置槽误当成普通物品网格发出 `ItemMove`。
- 2026-08-08：批量移动到特殊加工网格时拒绝以腰带/自动喝药配置槽为源，避免快捷配置被误当成材料。
- 2026-08-08：装备与伙伴装备右键卸下统一回到背包；婚戒仅人物装备槽保留原版传送语义，伙伴装备不再误走 `UseItem`。
- 2026-08-08：回归构建期间修复工作区 Godot API 类型兼容错误（CanvasItem 可见性与 TextEdit 滚动值转换），构建恢复为 0 errors、0 warnings。
- 2026-08-08：观察模式补齐原版物品操作禁用；使用、拖放、穿戴、伙伴穿戴和中键锁定均不再向服务器发包。自动喝药槽双击/右键恢复通过链接定位背包或伙伴背包本体。
- 2026-08-08：观察模式收到物品变化导致腰带失效链接时只更新本地显示，不再像普通角色一样回发 `BeltLinkChanged`。
- 2026-08-08：维修手动投放与批量导入均按 NPC 页 `Types` 过滤；饰品重置/婚戒链接源范围恢复原版（允许伙伴背包重置，不允许伙伴背包婚戒）。
- 2026-08-08：维修来源范围与服务端/原版一致，仅允许背包、装备、仓库、行会仓库和伙伴背包；修复耐久为 0 但仍可维修的物品被客户端错误拦截。
- 2026-08-08：高级精炼材料来源按服务端实际接受范围收紧为背包/仓库/伙伴背包；武器制作模板和彩槽严格只接受背包，避免链接成功后提交必然失败。
- 2026-08-08：邮件发送生命周期按原版 `MailSend`→`ItemsChanged` 顺序修复：发送前锁定来源并清空临时附件格，首个回包不提前解锁，成功/失败批量回包负责最终扣除或解锁，断线释放 pending 来源。
- 2026-08-08：邮件批量回包即使网格/槽位校验失败，也会按 pending 链接解锁来源，避免异常或迟到包造成永久锁定。
- 2026-08-08：`ItemsChanged` 回包统一清除高级 NPC 临时链接，覆盖服务端精炼石实际使用的成功/失败回包路径，不再依赖不存在的 `NPCRefinementStone` 客户端包。
- 2026-08-08：NPC 伙伴仓库“释放”恢复原版二次确认，确认后才发送 `CompanionRelease`，取消不会产生网络操作。
- 2026-08-08：NPC 出售来源锁定补充边框刷新；`ItemsChanged` 即使槽位校验异常也按 pending 链接主动解锁。
- 2026-08-08：寄售按服务端实际顺序消费 `ItemChanged` 回包，清除 pending 状态并解锁来源；补齐寄售、交易、邮件的伙伴背包来源，邮件仍保留服务端安全区校验。
- 2026-08-08：修复维修投放路径仍残留的 `CurrentDurability <= 0` 客户端拦截；服务端与原版允许耐久归零但可修理物品进入维修格。
- 2026-08-08：批量 `ItemsChanged` 统一在网格/槽位校验前解锁来源，覆盖非法槽位、迟到包和异常回包的通用锁清理。
- 2026-08-08：按服务端 `ItemMove` 允许范围补齐人物装备/伙伴装备卸下到伙伴背包的目标路由，避免选中装备后点击伙伴背包被客户端提前吞掉。
- 2026-08-08：物品移动后的腰带失效链接清理扩展到伙伴背包来源/目标；伙伴物品移入装备或交换时不再留下旧快捷栏图标。
- 2026-08-08：对照服务端 `NPCSell` 补齐伙伴背包出售来源；出售时按实际来源格锁定并由批量 `ItemsChanged` 解锁。
- 2026-08-08：NPC 翻页/刷新不再清掉已提交出售的 pending 链接；迟到的 `ItemsChanged` 仍可正确解锁来源，维修回包同步刷新来源边框。
- 2026-08-08：真实交互审计修正为分别验证 Ctrl+左键和 Ctrl+右键：左键必须 0 次观察请求，右键必须 1 次；之前测试错误要求两次请求，导致有效实现被误报失败。
- 2026-08-08：通用 `UnlockCell` 在异常/失败回包路径同步刷新边框；即使后续网格槽位校验提前返回，来源格也不会视觉上继续显示锁定状态。
- 2026-08-08：统一伙伴背包数据源为 `GameScene.CompanionInventory/CompanionEquipment`；伙伴获得物品恢复原版堆叠、货币和空格分配，避免临时回包按默认 Slot 覆盖显示格。
- 2026-08-08：所有主要物品操作进入 Locked 状态时立即刷新边框，礼包/宝箱关闭、删除和异常回包解锁时也同步刷新视觉状态。
- 2026-08-08：本地真实服务器在线审计通过：命中扫描实际命中 NPC/其他玩家（不再手动注入 `MouseObject`）；NPC 点击收到 `NPCResponse`，Ctrl+左键观察请求 0 次，Ctrl+右键请求 1 次并收到 `Inspect` 回包。
- 2026-08-08：拾取条件恢复为原版“鼠标逻辑格等于玩家当前格”而非“必须先命中一个 Item 对象”；脚下掉落物未被当前帧渲染命中时也会发送 `PickUp`。
- 2026-08-08：地图未处理鼠标输入补齐观察模式总闸，对齐原版 `MapControl.OnMouseDown`，观察者不会从地图分支发出拾取、NPC、观察或丢弃操作。
- 2026-08-08：命中扫描不再错误跳过其他玩家；恢复原版仅排除本地玩家的规则，在线 NPC/玩家 Ctrl 观察审计通过（左键 0 次、右键 1 次并收到 Inspect）。
- 2026-08-08：特殊 NPC 材料格的右键自动投放也按原版固定数量规则处理（大师精炼前两类 10 个，其余指定材料 1 个）；数量不足时不再伪造临时物品链接。
- 2026-08-08：修复伙伴“收起→召回”生命周期：`CompanionStore` 清空活动 UI 前先保存独立物品快照，`CompanionRetrieve` 不会因数组引用复用而丢失伙伴背包/装备。
- 2026-08-08：礼包确认不再本地立即关闭并解锁来源；等待服务端 `ItemChanged`/`BundleClose` 顺序完成，防止确认处理中重复使用同一礼包。
- 2026-08-08：真实服务器物品移动审计通过：背包 0→8 收到 `ItemMove.Success` 后本地状态正确交换，随后 8→0 反向移动也成功，两个来源/目标格均解除锁定。
- 2026-08-08：在线物品操作审计额外验证失败 `ItemSort` 回包只解锁、不清空本地数组；`failedSortPreserved=True`。
- 2026-08-08：同一在线回包审计验证失败 `ItemSplit`/`ItemDelete` 只解锁、不修改物品；`failedSplitPreserved=True`、`failedDeletePreserved=True`。
- 2026-08-08：在线操作审计完成装备往返：先卸下原武器到临时背包格，再背包→装备、装备→背包，最后恢复原武器；`equipmentRestored=True`，装备/背包锁均正确解除。
- 2026-08-08：物品格选中状态改为集中同步 `SelectedCell` 与格子的 `Selected`/边框；离线 UI 审计验证选中与取消均恢复，避免“已拿起但无选中视觉或状态残留”。
- 2026-08-08：分堆数量确认时恢复原版先锁定源格再发送 `ItemSplit`；成功、失败回包均沿用统一解锁路径，避免确认窗口期间重复分堆。
- 2026-08-08：腰带链接按服务端协议收紧：伙伴背包不可按类型链接的物品不再伪造 `LinkItemIndex`，伙伴可链接消耗品仍使用 `LinkInfoIndex`，自动药水继续支持伙伴背包本体。
- 2026-08-08：特殊加工/维修/交易等临时链接格点击恢复原版解除语义；已链接目标格不再被错误拿起为普通物品，避免后续移动状态卡死。
- 2026-08-08：NPC 购买负重计算在可用负重为 0 时直接终止，不再打开数量为 0 的购买确认框。
- 2026-08-08：本轮改动后构建仍为 0 errors、0 warnings；在线背包移动、失败整理/分堆/删除、装备交换审计仍通过；UI 审计通过（运行环境仍会输出既有 BCnEncoder 贴图依赖错误与退出时 RID 泄漏噪声，不影响审计 PASS）。
- 2026-08-08：邮件发送按服务端 `MailSend`→`ItemsChanged` 顺序保持附件来源 pending 锁；首个 `MailSend` 回包不会覆盖未完成请求，直到批量回包后才允许下一次带附件发送；`UICommunicationAudit` 页面审计通过。
- 2026-08-08：行会仓库按原版 `StorageLimit` 动态生成 11 列/至少 20 行，容量外格子禁用；滚动条在成员页使用像素单位、仓库页使用行单位，修复升级仓库后高槽位不可访问及滚动值单位冲突。
- 2026-08-08：伙伴背包按服务端 `CompanionWeightUpdate.InventorySize` 禁用容量外格子；切换伙伴重建网格后重新应用容量限制，避免客户端允许点击服务端必拒绝的空槽。
- 2026-08-08：商城热销榜点击恢复原版“定位/筛选商品”语义，不再误触后直接购买；商城购买数量仍按原版 1~10 选择并由服务端按堆叠上限裁剪。
- 2026-08-08：商城商品卡与热销榜展示恢复原版限时商品语义，按 `StoreInfo.Duration` 设置 `Expirable/ExpireTime`，不再把限时商品显示成永久物品；构建、UI 审计、通信审计和在线物品操作审计继续通过。
- 2026-08-08：精炼取回回包恢复原版列表生命周期，收到 `NPCRefineRetrieve` 后立即移除已取回条目、修正选中索引并刷新滚动列表；构建与 `git diff --check` 通过。
- 2026-08-08：宝石镶嵌/合成窗口重置时同时清除临时链接的 `LinkedSourceGrid` 与槽位，断线、失败和动画完成后的重复投放不会携带旧来源；构建与断线清理路径检查通过。
- 2026-08-08：自动药水窗口恢复原版整面板滚轮输入和 `MaxValue=MaxAutoPotionCount*50-2`，新增 `UIBeltPotionAudit` 验证腰带吸附尺寸、滚动范围和上下换行状态。
- 2026-08-08：寄售拖入堆叠物恢复原版数量选择流程；不再把整堆直接写入寄售临时格，确认数量后才建立来源锁和临时链接；构建、UI/NPC/通信审计通过。
- 2026-08-08：维修格的直接拖放补齐 NPC 当前页面 `Types` 过滤，与右键导入/批量导入共用可维修、婚戒、特殊维修冷却和来源网格校验；构建及 `UITestScene --ui-audit --npc-audit` 通过。
- 2026-08-08：NPC BuySell 页恢复原版 `Page.Types` 触发背包出售模式；背包左键切换待售选中状态，取消选中和提交入口均不再进入普通拿起/移动流程；新增 `UINPCSaleAudit` 通过。
- 2026-08-08：腰带/自动药水格左键恢复原版先进入 `SelectedCell` 的拿起状态；清除链接只在点击地图、落入目标格或再次点击同格时发生，避免首次点击快捷格就丢失配置。
- 2026-08-08：地图 Alt 采集对齐原版：只读取武器/护甲槽，钓鱼配置在非法水域或超距时阻止移动，钓鱼/驯服状态下 Alt 不抢先发送攻击或取消；`NetworkAudit` 新增 Alt 状态语义断言并通过。
- 2026-08-08：CombatController 的按键与持续处理同时增加钓鱼/驯服状态闸门，修复 `_Input` 先于地图取消分支时 Shift 攻击抢占操作；构建与地图网络/命中审计通过。
- 2026-08-08：地图攻击目标判定恢复原版 `CanAttack` 的玩家分支；活着的其他玩家可被选中、接近和攻击，怪物仍要求 `AI >= 0`；`NetworkAudit` 新增玩家/守卫怪物断言并通过。
- 2026-08-08：普通 `ItemMove` 的仓库目标矩阵对齐服务端：允许伙伴背包/伙伴装备拖入个人仓库、部件仓库和行会仓库，移除原版普通拖放不存在的 `CanStore` 客户端拦截；保留安全区、部件、婚戒、绑定和 `CanTrade` 条件；构建与 `git diff --check` 通过。
- 2026-08-08：个人仓库容量边界恢复原版 `StorageSize` 闸门；容量为 23 时第 22 格可用、第 23 格禁用，新增 `UIStorageAudit` 通过，避免容量外空格发送必失败 `ItemMove`。
- 2026-08-08：战斗点击入口增加 UI 悬停闸门，修复 Godot `_Input` 先于控件回调时点击背包/商城仍可能选怪或发攻击的问题；自动攻击循环不受该点击闸门误伤。
- 2026-08-08：礼包确认发送前恢复按钮锁定；宝箱状态 0/2 恢复可直接领取全部结果、状态 1 可直接确认选择，并在领取/重抽/确认发送前禁用对应按钮，避免原版链路被错误的选格前置或重复包阻断。
- 2026-08-08：地图移动器恢复原版 Alt+左键、货币已拾起和物品已拿起的优先级闸门，采集/钓鱼/驯服/丢弃数量窗口同帧不再额外发送普通移动。
- 2026-08-08：对照原版 `ConsignmentDialog` 与服务端 `MarketPlace*` 包顺序修复寄售状态机：寄售成功包按 Index 增量合并而不清空其它条目；购买/下架成功后清除旧选择并禁用旧操作；购买确认发送前禁用按钮并重置行会资金选项；搜索结果保留服务器空位，避免异步 `MarketPlaceSearchIndex` 索引错位。构建 0 errors、0 warnings，`UIAudit` 与在线物品操作审计通过。
- 2026-08-08：交易临时链接恢复原版来源锁定生命周期：放入交易格后立即锁定来源，取消链接时先保存来源坐标再清除并解锁；交易失败/迟到回包继续走统一解锁入口。构建、UI 审计与 `git diff --check` 通过。
- 2026-08-08：在线跑步专项回归通过：`--test-running` 验证首段步行、后续距离 2 的 Running 动画及服务端位置回包；`--test-right-run` 验证右键保持时连续两段 Running 与最终站立状态，完整堆栈复跑未再出现初次并发运行中的临时 `ObjectDisposedException`。
- 2026-08-08：寄售分页滚动/异步搜索回包清除旧选中状态；购买售罄保留搜索空槽而非删除列表元素，保证后续 `MarketPlaceSearchIndex` 仍使用服务端索引。构建、通信 UI 审计与差异检查通过。
- 2026-08-08：NPC 高级加工提交统一建立 pending 来源锁，重复点击被拒绝；普通关闭仅清理未提交链接，断线才释放已提交链接，`ItemsChanged`/高级 NPC 回包完成后清理 pending 并解锁。覆盖精炼石、碎片、普通/大师精炼、饰品升级/精炼、武器制作、饰品重置和婚戒绑定；构建、NPC 全模式 UI 审计及在线物品回包审计通过。
- 2026-08-08：物品格入口补齐原版 `CurrencyPickedUp` 前置优先级；货币已拿起等待丢弃数量时，物品格点击和快捷锁定不会抢走地图输入。构建、`UITestScene --ui-audit` 与 `MapTestScene --action-audit --skip-sound-audit` 回归通过。
- 2026-08-08：临时加工/镶嵌/合成/寄售 Link 建立时统一锁定来源格，来源不能在确认前再次移动或删除；取消、失败、动画完成和窗口重置路径均统一解锁。构建、`UITestScene --ui-audit` 与 `--npc-audit` 全模式回归通过。
- 2026-08-08：新增来源锁后按单客户端顺序重跑真实服务器审计：背包移动、反向移动、装备卸下/穿戴往返及失败整理/分堆/删除均通过；地图 NPC 点击与 Ctrl+右键观察也通过（Ctrl+左键观察请求为 0）。
- 2026-08-08：独立镶嵌窗口补齐关闭生命周期：普通关闭不提前释放提交中的来源，隐藏窗口收到成功回包直接完成并解锁，断线显式取消并回收来源；NPC 全模式 UI 审计、构建和差异检查通过。
- 2026-08-08：独立宝石合成窗口补齐同等生命周期：普通关闭保留提交中来源，隐藏窗口直接应用回包并清理来源，失败/断线/重置路径释放来源；构建 0 errors/0 warnings，NPC 全模式 UI 审计通过。
- 2026-08-08：恢复原版伙伴窗口打开时的背包右键投放：当前伙伴背包页可见且来源为人物背包时，右键优先发送 `ItemMove` 到伙伴背包，不再落入普通 `UseItem`；构建与差异检查通过。
- 2026-08-08：伙伴背包右键投放修复后，`UITestScene --ui-audit` 的网格选中/链接清理、腰带和自动药水回归继续通过。
- 2026-08-08：采矿入口按原版 `MapControl.ProcessInput` 修复：矿点上的掉落物/死亡对象不再阻断挖矿，人物背包移动不与挖矿同帧叠发，并补齐镐耐久与坐骑限制；构建、`MapTestScene --action-audit --skip-sound-audit` 通过。
- 2026-08-08：行会仓库右键投放对齐原版 `MoveItem(DXItemGrid)`：目标必须是启用、空闲且未被临时链接占用的容量内槽位，避免把升级容量外禁用格或旧链接格当成成功目标；构建、UI 审计和在线物品移动审计通过。
- 2026-08-08：邮件“删除全部”不再在发送 `MailDelete` 后立即本地删除，改为等待服务端 `S.MailDelete` 回包再移除，避免删除失败时客户端邮件凭空消失；构建、UI 审计、在线物品操作审计通过。
- 2026-08-08：右键上下文路由按原版格子类型收紧：装备/伙伴装备不会再绕过修理、婚戒和回包逻辑直接进入交易/邮件/寄售/行会仓库；同时恢复寄售→邮件→交易的原版优先级。构建、UI 审计和在线物品操作审计通过。
- 2026-08-08：NPC 买卖右键出售来源收紧为人物背包，恢复原版不从伙伴背包生成 `NPCSell` 的规则；构建、NPC 全模式 UI 审计和差异检查通过。
- 2026-08-08：NPC 买卖出售选中状态同步回物品格边框，右键再次点击可取消选中，恢复原版 `SelectedChanged` 的可见反馈；构建、NPC 全模式 UI 审计和差异检查通过。
- 2026-08-08：双击网格分支恢复原版集合：行会仓库双击不再调用 `UseItem`，装备双击只处理婚戒传送，其余可使用网格继续走物品使用；构建、NPC/UI 审计和差异检查通过。
- 2026-08-08：物品移动成功回包按服务端协议恢复 `ClientUserItem.Slot` 偏移：装备/伙伴装备使用 `EquipmentOffSet`，碎片仓库使用 `PartsStorageOffset`，避免界面槽位与后续伙伴同步/临时链接协议槽位混用；构建、UI 审计和在线装备往返审计通过。
- 2026-08-08：在线操作审计新增装备协议槽位断言，装备往返必须满足 `ClientUserItem.Slot == EquipmentOffSet + slot`；用于防止“显示已穿戴但后续回包坐标错误”的回归。
- 2026-08-08：货币入口恢复原版行为：钱包热区可打开货币窗口；货币标签受 `SelectedCell`/`CanPickup`/数量限制，已拿起货币时再次点击任一货币只取消而不切换；构建、UI 与地图输入审计通过。
- 2026-08-08：NPC 批量出售改用独立 `SaleSelected` 状态，不再复用全局拿起状态；多件出售可同时保留选中边框，地图丢弃/普通移动不会被出售选择污染，取消/成功回包会清理视觉状态。
- 2026-08-08：补齐物品格 Alt+左键聊天链接优先级；在双击、Shift 分堆和普通拿起之前截获，避免 Alt 链接误发 `ItemMove`；`UIItemGridAudit` 已增加“不进入 SelectedCell”回归断言并通过。
- 2026-08-08：组合 UI 回归通过：通信/邮件、聊天链接、角色/装备、仓库/部件仓库、钓鱼、伙伴、行会仓库、商城页面几何与交互审计全部 PASS。
- 2026-08-08：修复饰品升级/合成“全选材料”绕过原版 `CheckLink` 的差异；`MoveItem(DXItemGrid)` 现在也校验目标饰品的同款、绑定、等级和附加属性，新增 `UINPCOperationAudit` 验证不匹配材料拒绝、匹配材料接受。
- 2026-08-08：地图右键取消恢复 `TargetForm.OnMouseDown` 优先级；已拿起物品或货币时右键只清理状态，不再继续取消自动寻路或触发普通地图操作；`NetworkAudit` 新增该语义断言并通过。
- 2026-08-08：地图丢弃物品确认发送前恢复原版锁定来源格，成功/失败均由 `ItemChanged` 回包统一解锁；新增 UI 物品丢弃来源保护断言，验证未锁源可开始、已锁源拒绝重复发送。
- 2026-08-08：物品格边框/底色补齐原版已禁用格、已链接临时格和锁定/选中状态的视觉反馈；`UIItemGridAudit` 新增禁用格视觉断言并通过。
- 2026-08-08：`ItemsGained`/伙伴获得包恢复原版先标记 `New` 再入包的生命周期，并补齐获得提示；经验包不标记新物品。构建 0 errors、0 warnings，`UIItemGridAudit` 的获得角标/经验例外回归通过。
- 2026-08-08：批量物品扣除回包增加数量边界保护；重复/迟到 `ItemsChanged` 不会把当前堆叠减成负数，正常部分扣除和整堆扣除仍分别通过。构建 0 errors、0 warnings，`NetworkAudit` 的 item-count bounds 回归通过。
- 2026-08-08：背包右键上下文顺序恢复原版“寄售→邮件→仓库/部件仓库→交易→行会仓库”；仓库目标无可用槽位时才继续尝试后续窗口，避免多窗口同时可见时投放到错误操作面板。

- 2026-08-08：物品格恢复原版先派发基类 MouseDown/MouseClick、再执行自身操作的顺序；礼包/宝箱/寄售等普通格事件不再静默丢失。构建 0 errors、0 warnings，`UIItemGridAudit` 新增 normal-cell event 回归通过。

- 2026-08-08：腰带快捷键恢复原版“有 SelectedCell 时先投放、无 SelectedCell 时才使用腰带物品”的分支，观察模式继续禁止操作；修复拿起物品后按快捷键无法放入腰带的差异。

- 2026-08-08：`ItemSplit` 成功回包增加当前数量、目标槽空闲和源/目标不同校验；迟到包不会覆盖新槽或产生超量副本。构建 0 errors、0 warnings，`NetworkAudit` 的 split-target protection 回归通过。
- 2026-08-08：`ItemExperience` 回包恢复原版完整覆盖物品 `Flags` 的语义；等级变化不会残留旧的 Bound/NonRefinable 等状态，`UIItemGridAudit` 新增 flags 覆盖回归并通过，构建 0 errors、0 warnings。
- 2026-08-08：获得物品提示恢复原版部件名称解析：`ItemPart` 使用 `AddedStats.ItemIndex` 找到真实物品名，仍保留部件标记；避免拾取后提示只显示通用部件壳名称。
- 2026-08-08：本轮完整回归：客户端构建 0 errors/0 warnings，`UITestScene --ui-audit` 的物品格/腰带自动药水审计均 PASS，`git diff --check` 通过。
- 2026-08-08：交易金币回包恢复原版分流：`S.TradeAddGold` 更新本地玩家金币栏，`S.TradeGoldAdded` 更新对方金币栏；此前本地金币只进入聊天提示、窗口仍显示 0。构建 0 errors/0 warnings，`UITradeAudit` 双向金币路由 PASS。
- 2026-08-08：邮件发送表单恢复原版回包时序：`S.MailSend` 不再提前清空输入，只有后续 `ItemsChanged.Success=true` 才清空；失败包会解锁附件来源并保留收件人、主题和正文。构建、`UICommunicationAudit` 的首包/失败保留/成功清空生命周期断言均 PASS。
- 2026-08-08：移除邮件首包的提前“发送完成”提示；原版只在后续 `ItemsChanged.Success` 后确定结果，避免收件人/附件/金币校验失败时显示错误成功信息。
- 2026-08-08：商城 `GameStoreData` 回包补齐热销商品同步；热销列表刷新前清理旧行和空列表占位，避免重复/迟到回包叠加控件。构建 0 errors/0 warnings，`UIGameStoreAudit` PASS。
- 2026-08-08：商城收藏按钮恢复服务端权威状态：点击只发送切换请求，等待 `GameStoreFavouriteChanged` 后更新图标/收藏筛选，避免失败请求造成本地假收藏。构建与 `UIGameStoreAudit` 通过。
- 2026-08-08：跨模块回归：`MapTestScene --network-audit` 的迟到/重复物品回包保护 PASS；`UITestScene --npc-audit` 全部 NPC 模式及高级材料匹配审计 PASS。
- 2026-08-08：装备格右键顺序对齐原版：维修、镶嵌/升级等 NPC 面板先接管装备，钓鱼/驯兽期间禁止卸下，再处理婚戒传送和回背包；避免 Godot 过早直接卸下导致 NPC 操作失效。构建、UI/NPC 通用审计通过。
- 2026-08-08：删除回包增加槽位物品 Index 保护：若迟到 `ItemDelete` 对应槽位已换入新物品，旧回包不再删除或解锁新物品；无本地待确认删除的服务端权威回包仍正常处理。构建 0 errors/0 warnings，`git diff --check` 与 UI/NPC 审计通过。
- 2026-08-08：物品使用扣数量回包增加同等 Index 保护：记录发起 `ItemUse` 时的物品身份，槽位复用后迟到 `ItemChanged` 不再修改新物品；构建 0 errors/0 warnings，`MapTestScene --network-audit` 的迟到/重复回包、数量边界和拆分目标保护继续 PASS。
- 2026-08-08：行会仓库右键投放增加安全区前置，且网格投放入口拒绝空来源格；不会在非安全区先吞掉右键事件，也不会因空源格访问物品属性后误报已投放。构建与 `UIStorageAudit`/`UIGuildAudit` 均 PASS。
- 2026-08-08：断线清理新增物品删除/使用 pending 身份表，避免重连后旧请求身份阻塞新操作；构建 0 errors/0 warnings，`git diff --check` 通过。
- 2026-08-08：按原版 `CheckLink` 收紧交易、邮件、寄售及仓库上下文来源：伙伴背包物品不再被错误路由到这些窗口；构建与通信、商城、仓库、行会仓库 UI 审计全部 PASS。
- 2026-08-08：同步收紧直接拖放到寄售目标格的来源校验，伙伴背包不能绕过窗口右键过滤直接建立寄售 Link；构建通过，前述通信/商城/仓库/行会 UI 回归保持 PASS。
- 2026-08-08：特殊窗口的链接目标格恢复原版操作闸门：已链接复制物不能再次进入 `UseItem`、交易、加工或普通 `ItemMove`，只能通过点击解除链接；构建 0 errors/0 warnings，`git diff --check` 通过。
- 2026-08-08：地图脚下 NPC/掉落物重叠点击恢复原版分发时序：按下仍发送 `PickUp`，左键抬起再发送 `NPCCall`；普通地图动作与网络/动作审计回归通过，真实交互审计因本次自动登录未进入角色场景未产生新日志，未据此冒充在线通过。
- 2026-08-08：脚下待释放 NPC 点击在地图切换、断线和场景重置时清除，避免旧地图的鼠标释放事件向新地图发送 `NPCCall`；构建 0 errors/0 warnings，`git diff --check` 通过。
- 2026-08-08：人物装备左键卸下路径补齐原版钓鱼/驯兽状态保护；现在右键、左键拿起后放回和直接穿戴均不会在状态进行中发送错误 `ItemMove`。构建 0 errors/0 warnings，`git diff --check` 通过。
- 2026-08-08：装备/伙伴装备的选中后移动恢复原版“只禁止装备槽互移、由目标格自行校验”的语义，重新允许人物装备拖到合法的行会仓库/NPC 等目标；伙伴装备空目标格也不再强制改投第一个 HostGrid。构建与 UI/NPC/仓库/行会审计通过。
- 2026-08-08：命中扫描恢复原版同格对象优先级：对象新增/移动记录 HitOrder，按 Cell.Objects 逆序的“最新对象优先”扫描，不再由全局字典旧顺序抢先命中；`MapTestScene --cursor-audit` PASS，构建 0 errors/0 warnings。
- 2026-08-08：怪物点击/自动攻击补齐原版 `CanAttack` 的 AI 限制；AI<0 的非攻击对象不会再被选中或进入自动接近/攻击循环。构建与 `--cursor-audit` 回归通过。
- 2026-08-08：宠物命中恢复原版分层语义：普通左键可选中但不自动接近/攻击，鼠标下活宠物阻止普通移动，Shift 才进入攻击分支；`--cursor-audit` 与 `--network-audit` 均 PASS。
- 2026-08-08：邮件发送前校验恢复原版 `CommunicationDialog` 的角色名正则、金币余额/2,000,000,000 上限和发送中闸门；非法收件人、负数/超额金币及重复发送不会再提交 `C.MailSend`。`UITestScene --communication-audit` 与构建回归通过。
- 2026-08-08：交易窗口恢复原版观察模式闸门：观察者不能点击金币、放入交易物品或确认交易；普通玩家的金币双向回包审计继续通过，构建 0 errors/0 warnings。
- 2026-08-08：腰带链接恢复原版伙伴背包来源规则：伙伴背包物品不再被 Godot 额外的 `ShouldLinkInfo` 条件拦截，按原版 `QuickInfo/QuickItem` 分支建立或交换链接；构建、`UIBeltPotionAudit` 和通信/UI 回归通过。
- 2026-08-08：修复地图战斗输入抢占脚下拾取：`CombatController._Input` 在玩家当前格的普通左键不再先攻击，自动攻击循环也拒绝距离 0；新增 `current-cell pickup priority` 网络审计，`MapTestScene --network-audit --cursor-audit --skip-sound-audit` 通过。
- 2026-08-08：拾取入口补齐原版死亡、麻痹、禁锢和龙威状态闸门；这些状态下脚下点击不再发送 `PickUp`。新增 `pickup state guards` 矩阵审计，地图网络/命中回归通过。
- 2026-08-08：数量窗口恢复原版默认值：丢弃物品、货币、分堆及临时链接打开时均从数量 1 开始，不再默认半堆或整堆；构建、`UIItemGridAudit`/腰带/通信回归通过。
- 2026-08-08：NPC 购买入口恢复原版观察模式拒绝和 Gold 货币回退；观察者/无效商品选择不会发送 `NPCBuy`，新增 `UINPCBuyAudit`，NPC 全模式与构建回归通过。
- 2026-08-08：寄售购买入口补齐原版观察模式二次保护；程序化/旧焦点触发也不会绕过按钮状态发送 `MarketPlaceBuy`。新增 `UIConsignmentAudit`，与商城审计、构建及差异检查通过。
- 2026-08-08：商城赠送入口恢复原版观察模式与可用性/数量保护，确认回调再次校验角色名和观察状态；热销商品点击恢复按商品 Index 精确定位，而不是名称模糊搜索。新增商城赠送保护断言；构建与 `UIGameStoreAudit` 通过。
- 2026-08-08：礼包/宝箱回归原版余额与状态操作：宝箱无剩余解锁次数时直接揭示，重抽/揭示确认瞬间校验对应货币余额；新增状态/余额边界审计，构建、`git diff --check` 与商城 UI 专项回归通过。
- 2026-08-08：技能快捷键恢复原版可配置键位优先级；移除 Godot 在 `KeyBindManager.GetAction` 之前硬编码 F1–F12 的分支，Ctrl/Shift 技能栏与自定义技能键均统一走键位表。构建、`git diff --check` 与 `UIKeyBindAudit` 通过。
- 2026-08-08：技能释放入口补齐原版基础操作闸门：观察/骑马/死亡/龙威/麻痹/沉默、技能等级、魔法戒指、技能冷却和 MP 不满足时不发 `C.Magic`；构建、`git diff --check` 与 `UIMagicAudit` 通过。
- 2026-08-08：技能快捷栏补齐原版战斗技能分流：Thrusting/HalfMoon/DestructiveSurge/FlameSplash 发送 `MagicToggle`，FullBloom/莲花/荆棘/Karma 只设置下一次普通攻击的 `AttackMagic`，被动剑术类不误发 `C.Magic`；普通攻击回调现在携带已选择的攻击技能。构建与差异检查通过。
- 2026-08-08：物品格显示对齐原版图库：New/锁定/不可用/部件角标改用 `Interface` 47/48/49/103，宝箱未揭示格改用 `GameInter2` 2930 专用锁定图；`UIItemGridAudit` 增加资源断言并通过，构建与差异检查通过。
- 2026-08-08：邮件阅读入口恢复原版重复操作保护：已读邮件再次打开不重复发送 `MailOpened`，已领取/空附件格不发送 `MailGetItem`；通信布局、邮件发送成功/失败生命周期与新增保护审计通过。
- 2026-08-08：角色变更窗口补齐原版状态保护：同性别变更不发送，发型编号按职业/性别范围归一化，并从当前角色恢复发色与铠甲染色预览；`--edit-character-audit` 通过。
- 2026-08-08：消耗品、伙伴食物和技能书的使用入口恢复原版统一 `CanUseItem` 前置，避免绕过性别/职业/等级/属性/伙伴等级及技能学习限制；构建、差异检查和地图网络回归通过。
- 2026-08-08：组合 UI 回归通过：`--ui-audit --communication-audit --game-store-audit --edit-character-audit --fortune-audit --companion-audit --belt-potion-audit`，物品格、腰带/自动药水、通信/邮件、幸运查询、伙伴和角色变更审计均 PASS。
- 2026-08-08：修复背包出售模式并行改动遗漏的 `Library.SystemModels` 引用；构建恢复为 0 warnings/0 errors，UI、物品格、腰带/自动药水和角色变更审计复测 PASS。
- 2026-08-08：物品格输入对齐原版：Alt+左键仅阻止拿起/移动，不再错误建立聊天链接；聊天链接保留 Ctrl+中键路径。构建、`git diff --check` 与 `--ui-audit` 通过。
- 2026-08-08：NPC 出售恢复原版“无选中项=出售全部可售物品”语义；出售模式按钮进入后保持可用，并过滤锁定、婚戒、无价值、不可售和类型不匹配物品。构建、UI 全审计及 NPC 操作审计通过。
- 2026-08-08：礼包/宝箱关闭回包增加来源物品引用保护；迟到关闭包在槽位已换入新物品时不再错误解除新物品锁定。构建、差异检查和 `--gamestore-audit` 通过。
- 2026-08-08：仓库目标格补齐原版 `CanStore` 及 ItemPart/安全区限制，普通点击和右键自动找空位两条 `ItemMove` 路径均受保护；构建、差异检查和地图网络回归通过。
- 2026-08-08：将仓库存储边界纳入 `UIItemGridAudit`：可存储/不可存储、非安全区、普通物品/ItemPart 分流断言均通过；构建、差异检查和 UI 审计通过。
- 2026-08-08：仓库边界辅助断言覆盖普通仓库与部件仓库的安全区、婚戒、ItemPart 和 `CanStore` 组合；`--ui-audit` 中的 `UIItemGridAudit` 通过。
- 2026-08-08：交易金币输入恢复原版当前余额上限，并在数量窗口确认时再次校验实时余额；无金币时不打开交易金币窗口。构建、差异检查和 `UITradeAudit` 通过。
- 2026-08-08：交易双方金币回包显示恢复原版纯数值格式，去除值标签重复的“金币:”前缀；`UITradeAudit` 复测通过。
- 2026-08-08：寄售购买/下架确认回调补齐观察模式和数量二次校验，旧窗口不能绕过当前状态发送购买或下架包；构建、差异检查和 `--consignment-audit` 通过。
- 2026-08-08：寄售物品链接与确认提交增加观察模式、来源物品存在性及数量一致性保护；构建、差异检查和寄售专项审计复测通过。
- 2026-08-08：交易临时物品格记录发起操作时的来源物品引用，失败/关闭/迟到回包仅在引用一致时解锁来源，避免槽位复用后误解锁新物品；构建、差异检查和 `UITradeAudit` 通过。
- 2026-08-08：伙伴仓库收起操作改用当前选中伙伴索引，并为收起/召回/释放统一增加观察模式和非法索引闸门；构建、差异检查和 `UICompanionAudit` 通过。
- 2026-08-08：任务接受/提交/放弃链路统一补齐原版观察模式与非法索引闸门；任务日志、NPC 任务详情和可选奖励弹窗均在发送前阻止观察者操作，放弃确认回调再次检查状态。构建、`git diff --check`、`--ui-audit --npc-audit` 通过。
- 2026-08-08：NPC 改造类操作统一下沉到 `GameScene` 做观察模式保护，覆盖开孔/宝石合成、碎片、矿石精炼、首饰/武器制作、精炼取回、骰子结果；购买、出售、维修增加索引/数量/非空物品链校验，伙伴解锁/收服也增加观察模式与参数保护。构建、`git diff --check`、NPC 全模式与操作审计通过。
- 2026-08-08：婚戒制作、婚姻响应和传送等跨面板操作补齐观察模式与槽位保护；构建和 `git diff --check` 通过。
- 2026-08-08：装备使用入口改为返回真实 `ToEquipment`/`ToCompanionEquipment` 结果；等级、职业、负重、钓鱼配件、状态或观察模式拒绝时不再伪报成功，双戒指/双手镯按可穿戴槽尝试。核心发送层同时补齐物品移动、使用、锁定、整理、删除、丢弃、腰带/自动药水、礼包/宝箱和命运检查的观察模式/参数保护；构建、差异检查及 UI/商城/通信审计通过。
- 2026-08-08：重新对照原版特殊物品使用链，补齐改名、改发型、染色、称号及系统检查发送层的观察模式/空值保护；`--edit-character-audit --fortune-audit --ui-audit` 和构建复测通过。
- 2026-08-08：寄售购买/下架/发布及商城购买/赠送/收藏增加发送层观察模式、索引、数量、价格和收件人保护，避免旧确认窗口绕过 UI 状态；修复并发渲染改动遗留的 `BlendImageLayerNode` 未定义字段导致的构建错误。构建、`git diff --check`、`--consignment-audit --gamestore-audit --ui-audit` 全部通过。
- 2026-08-08：地图货币丢弃改走 `GameScene.SendCurrencyDrop`，确认回调重新校验当前货币余额、数量和观察模式；`MapTestScene --network-audit --cursor-audit --skip-sound-audit` 新增并通过货币边界矩阵。完整数量窗口取消/确认 UI 仍保留在 A-7 待实测项中，未提前勾选。
- 2026-08-08：邮件领取附件增加 `(邮件索引, 附件槽位)` pending 去重，批量领取和单格点击不会重复发送 `MailGetItem`；邮件/附件移除或服务端刷新时清理 pending，发送层增加观察模式和索引/槽位保护。构建、差异检查和 `--communication-audit --ui-audit` 通过。
- 2026-08-08：邮件删除和发送入口继续下沉到发送层校验观察模式、索引、收件人和金币边界；通信生命周期复测通过，构建保持 0 errors/0 warnings。
- 2026-08-08：仓库专项审计新增临时链接取消矩阵，验证关闭/取消时目标格物品、来源引用和全局 `SelectedCell` 同步清除；`--storage-audit --ui-audit`、构建和差异检查通过。
- 2026-08-08：交易发送层补齐观察模式与参数保护：请求响应、确认、金币和物品添加均不能绕过窗口状态；金币同时按实时 Gold 余额限制。`UITradeAudit` 新增余额边界矩阵，构建、差异检查和 UI 审计通过。
- 2026-08-08：战斗输入主循环 `CombatController._Process` 恢复原版 `MapControl.ProcessInput` 分支顺序：顶部自动攻击（875-895，目标相邻 Chebyshev==1、冷却到、未骑马、无 ElementalHurricane、怪物无宠物主或按 Shift）先于任何鼠标分支；AutoRun（896-901）先于鼠标分支与底部追击；Shift+左键且未选中（904-913）朝鼠标方向攻击后返回；底部追击（1058-1129）按 `MoveTime` 600ms 节拍 `C.Move` 接近，目标死亡保留选中（D15）、被阻挡时 `BestApproachDirection` 或原地转向。`TryAttack` 补齐原版骑马/飓风/冷却闸门；原“按住 Shift 即对选中目标远程攻击”的旧逻辑（与原文不符）已删除。
- 2026-08-08：战斗点击 `_Input` 恢复原版 `OnMouseDown` 683-739：`CanAttack` 通过才选中为 `TargetObject`，未通过清空；Shuriken 分支按原文顺序（超 `MagicRange` 提示 + `Stop()` 任何坐骑状态 → 骑马在范围内落近战 → 冷却中 `Stop()` 清目标 → 可投 `RangeAttack`+`Stop()` 任意距离含相邻），`C.RangeAttack` 包结构与原版一致（Direction+Target，无魔法字段）；右键仅在启用 `RightClickDeTarget` 时取消怪物目标。
- 2026-08-08：攻击冷却本地预测恢复原版 `UserObject.SetAction`（637-698）公式：`max(800, AttackDelay - AS*ASpeedRate)`，超重或 Neutralize 再叠加一次（等效 x2）；采矿独立公式（超重 x3、Neutralize x2、同时按超重 x3）。移除 Godot 旧 `max(250,...)` 地板，高攻速下不再比原版快约 3 倍。
- 2026-08-08：转向恢复原版同向不重复发包：`SendTurn` 仅在方向变化时入队 `C.Turn`（该包对本地无回包），并先应用本地朝向/玩家方向再入队；`MouseWalker` 与 `CombatController` 共用同一入口。
- 2026-08-08：施法恢复原版 `MagicAction` 入队语义：行走动画期间按下的技能先排队，走完或超过动作时长边界才发 `C.Magic`；施法期间 `ProcessInput` 整体暂停攻击与追击。采矿间隔同步改用原版公式并在背包超重/Neutralize 时翻倍。
- 2026-08-08：新增 `MapTestScene --combat-audit` 静态断言套件：攻击间隔矩阵（800 地板/AS 减免/超重 x2）、采矿矩阵（x3/x2）、Shuriken 点击真值表（超距先于坐骑、骑马落近战、冷却清目标、可投投+清）、行走动画集合（Walking/Running/马走/爬行等 6 种）、转向防重复闸门，全部 PASS；`--cursor-audit --network-audit` 回归 PASS；构建 0 errors/0 warnings，`git diff --check` 通过。
- 2026-08-08：窗口化 Vulkan 实服战斗审计落地：`GameScene --operation-audit-ext` 新增 S16 战斗在线实测（找怪→`SendMouseMove` 单步走位→`CombatController.TargetObject` 左键选中→顶部自动攻击循环真实 `C.Attack` 发包）。开发服务器 TempAdmin 登录后 `@monster TigerSnake 2` 生成 HP70 目标保证多刀；攻击钩子记录发包时刻，连续多刀间隔与 `ComputeAttackIntervalMs` 公式完全一致（gap=1359/1371/1386ms vs expect=1359ms，偏差 ≤30ms）；`S.ObjectDied` 后尸体保留期内 `TargetObject` 保持指向死亡怪（D15 死亡目标保留）；`@monster` 生成点可能与玩家同格（dist=0）时自动攻击不触发（Chebyshev==1 判定），审计走开一步重入。headless（ext24/ext25）与窗口化 Vulkan llvmpipe（ext26，Vulkan 1.4.335 真实窗口）均 `RESULT combat=True pass=True`。Shuriken 投掷回包：DB 中不存在 shape-33（`Globals.ShurikenLibraryWeaponShape`）武器（武器 shape 分布 0,2–8,11–19,21,23,24），服务端 `RangeAttack` 拒绝非 33 武器，在线投掷不可行——记为数据限制，静态 Shuriken 点击真值表（`--combat-audit`）已 PASS。
- 2026-08-08：E4 邮件链路真实服务器审计落地：`GameScene` 新增 `--operation-audit-ext` 在线审计 S13 发送/S14 领取/S15 删除。发送自寄邮件（`C.MailSend`，附件 1 瓶 CanAutoPot 药水、金币 0）→ 服务端 `S.ItemsChanged`（扣量+解锁）与 `S.MailNew`（列表 +1）均以回包确认；领取（`C.MailGetItem`）→ `S.MailItemDelete` 附件格清空、附件叠回原堆；删除（`C.MailDelete`，先领完附件绕过 `MailHasItems`）→ `S.MailDelete` 列表还原。断言扣量/解锁/邮件数增减/Subject 匹配，两次运行均 `RESULT mailLifecycle=True pass=true`（ext18/ext19）。`CommunicationDialog` 增加只读快照 `MailSnapshot`/`FindMail`；播种 `--seed-reference` 增加清空邮箱（附件事项逐条删除）保证基线 `mailCountBefore=0`。构建 0 errors/0 warnings（`-warnaserror`），`git diff --check` 通过。剩余：邮件附件数量对话框边界、断线重连场景。
- 2026-08-08：定位并规避 ZirconClient 构建竞态——`LibraryCore` 的 `obj/ref` 中间产物被并行依赖构建删除导致 CS0006 ×326 时，先单独构建 `LibraryCore/LibraryCore.csproj` 再构建客户端可恢复；Godot 侧失败构建会删除 `.godot/mono` 旧 DLL（表现为 C# 类无法实例化），需确认构建成功再运行 headless 审计。ext11 的 admin 登录失败发生在旧 DLL 期间，重建后 `TempAdmin` 登录正常。

## 收尾记录（2026-08-09）

- 2026-08-09：A-7 数量窗口取消/确认（货币类）完成。`ItemAmountDialog` 恢复原版 `DXNumberTextBox` 语义：`MinValue=0`（解析失败回落 0、可解析值钳制 `[0,Max]`）、边框 `<=0 红 / ==Max 橙 / 其余绿`、确认按钮 `Enabled = Amount > 0`，`Confirm()` 内再拦 `Amount <= 0`（Enter 直触 `TextSubmitted` 也不发包不关窗）；货币分支以 `IsCurrencyItem`（对应 `CEnvir.IsCurrencyItem`，`Globals.CurrencyInfoList` 查 `DropItem`）替代名称硬编码，输入时实时 `item.Count = Amount` + 预览格 `RefreshItem`；地图货币丢弃构造 `ClientUserItem(currency.Info.DropItem, Amount)` 作预览。审计：`UIItemAmountAudit` PASS step/colour、zero/upper/parse-clamp、currency-live-count（`--ui-audit` 14 项全 PASS）。
- 2026-08-09：邮件金币输入边界完成。`CommunicationDialog` 新增 `ClampGoldInput`（>2e9 钳到 `"2000000000"`，对齐原版 `DXNumberBox.MaxValue`）、`GoldBoxValid`（`0 <= v <= 2e9 && v <= 当前金币`，对齐 `GoldValid`）、`GoldBorderColour`/`RecipientBorderColour`（0→Primary、合法→绿、超余额/非法→红；收件人空→默认），`IsMailSendValid` 复用数值闸门。审计 PASS：`gold=clamp/valid/colour recipient=colour`。
- 2026-08-09：邮件断线重连状态回滚完成。`CommunicationDialog.AuditDisconnectRollback`：构造待发附件链接 + `_mailSending=true` 后走 `CancelPendingMailLinks`，断言临时链接清空、发送标志复位、可重新发送（`rollback=pending=True released=True resendable=True`）。
- 2026-08-09：C6 伙伴食物契约审计完成。`DXItemCell` 新增 `ComputeUseCooldownMs`（`Max(250,Durability)`）与 `ShapeBlocksWhileMounted`（Shape 19-22），使用分支改用之（原版 DXItemCell:1656-1661 共用 Consumable/CompanionFood 分支：`CanUseItem` 前置、网格 Inventory/PartsStorage/CompanionEquipment/CompanionInventory、骑马禁、冷却豁免 `ElixirOfPurification`、`Locked` + `C.ItemUse Count=1`——Godot `ServerConnection.SendItemUse` 同为 Count=1）。`UICompanionAudit` PASS（真实 DB 伙伴食物样本 Green Apple/Chestnut Rice Ball/Meat Dumpling/Fresh Meat：dur0/shape0 → 冷却 250ms、非骑马限制）。
- 2026-08-09：E3 行会仓库契约审计完成。`GuildDialog` 新增 `StorageGridSize`（`(11, Max(20, Ceil(Limit/14)))`，对齐原版 `RefreshStorage`）与 `StorageCellEnabled`（超容量格禁用），滚动/容量行数改走静态函数；`S.GuildUpdate` 回包驱动 StorageLimit/资金刷新断言。`UIGuildAudit` PASS（storage-size/enabled/update；StorageLimit=300→22 行）。失败回滚：`OnItemMove` 回包 `!Success` 只解锁不改数组（行会仓库容量满/资金不足/无权限由服务端拒绝）。
- 2026-08-09：C6/E3 实服端到端完成。`--operation-audit-ext` 全链路实跑 `RESULT rings=true bracelets=true beltCleared=true autoCleared=true mailLifecycle=true companion=True guild=True combat=True pass=True`（日志 `/tmp/ext_e2e_live3.log`）。C6：S17a 背包槽18食物移入伙伴槽0（`S.ItemMove` 回包数组+解锁+`SyncCompanionItemList`）→ S17b 使用：S1 药水的 2000ms 冷却（`ComputeUseCooldownMs=Max(250,Durability)`，S1→S17b 十六阶段仅约 1.5 引擎秒撞上）未清时审计按真实玩家"冷却后重点击"语义以 250ms 轮询等待（上限30s），冷却清除后 `UseItem()` 发包 → `S.ItemChanged(count=10→9)` + `S.CompanionUpdate(hunger=50→56)` 双回包 → `used=True count=9 expect=9 unlocked=True`。**期间定位并修复一处真实客户端 bug**：`CompanionUpdate` 回包 lambda 原调用 `CompanionDialog.ApplyCompanion`，其非空分支对 `game.CompanionInventory` 做 `Array.Clear + Array.Copy(companion.InventoryArray→自身)`（登录后两数组共享同一引用），把同帧 `S.ItemChanged` 写入的 count=9 抹成 null（探针：hook-wrote 后 `cont arr0=NULL`）；原版 `CConnection.Process(S.CompanionUpdate)` 仅 `CompanionBox.Refresh()` 刷标签。新增轻量 `RefreshCompanionStats`（标签/进度条/预览/负重，不动数组），`CompanionUpdate` 与 `CompanionItemsGained` 两个回包改用它（后者的原版 `AddCompanionItems` 也只刷格子）。旧 60s 看门狗在冷却等待窗误报（`pending=false` 时触发）已改为阶段感知 + 阶段推进重武装。E3：S18 创建行会（`@createguild` 管理命令或行会契约）→ 入库移动 → S18c 合并到已占格被服务端拒绝（`rejected=True storage-unchanged=True unlocked=True`）→ S18d 出库回滚（guild0 空、Inventory 复原）。S16 战斗：空视野 `@monster TigerSnake 2` 救援、单步走位接近、死亡目标保留 D15（`died-kept-target=True`）、攻击节拍 `gap=1069ms`（expect=1359ms，含走位/选中损耗）。播种/登录备注：TestHero 走服务端管理员入口（`SEnvir.Login`：非邮箱 + `--pass == Config.MasterPassword` → `GetCharacter(name)?.Account`，不校验账号口令，故播种无需口令重置）；运行环境 `DISPLAY=:99`（Xvfb 需存活）+ `VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/lvp_icd.aarch64.json`；每次运行前 停服→播种→启服→等 ~45s（botfarm 重连风暴挤爆 SendQ）。

P1 Input/Combat 复测的战斗在线实测已完成（S16：真实窗口 Vulkan + 实服 C.Attack 发包节奏、连续攻击间隔、死亡目标保留 D15，ext24/25/26 全 PASS；Shuriken 为 DB 数据限制，静态矩阵覆盖）。剩余未完成项：A-7、邮件附件数量对话框边界、邮件断线重连、C6 伙伴食物移动/使用、E3 行会仓库容量/资金/回滚均已完成（A-7/邮件见 2026-08-09 记录；C6/E3 契约审计 + 实服端到端均完成，见 2026-08-09 记录）；未完成项目不得提前标记完成。
