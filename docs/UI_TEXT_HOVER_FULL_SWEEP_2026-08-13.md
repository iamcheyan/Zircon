# 全窗口文本/悬停提示排查修复（2026-08-13 第二轮）

## 1. 任务目标与执行摘要

延续「文本渲染点全面排查」的思路，对 Godot 客户端**所有窗口**做一次新旧对照，
重点找：动态变色（ForeColour 条件切换）、图标悬停 Hint、状态反馈文本三类缺口。

排查方法：扫描旧版全部窗口（`Client/Scenes/Views/*.cs`）中使用
`ForeColour = Color.X` 动态变色和 `.Hint =` 的位置，逐一核对新版对应窗口。

## 2. 排查结论

**大部分窗口已完整移植**（无需改动）：CharacterDialog（名字/行会/结婚/属性变色）、
InventoryDialog（金币/GG/出售模式 Total）、GroupDialog（成员血量绿/白）、
BuffDialog（暂停红/剩余时间渐变）、QuestDialog（任务状态文本）、TradeDialog、
BeltDialog、FishingDialog、NPCSocketDialogs 等。

**发现并修复 6 处缺口**：

| 窗口 | 缺口 | 旧版对照 | 修复 |
|---|---|---|---|
| CommunicationDialog | 自己的在线状态按钮点击后不刷新文本/颜色 | UpdateStateLabel：在线绿/离开橙/忙碌红/离线灰 | ✅ `RefreshOwnState` + GameScene 回调 |
| NPCGoodsPanel | 商品价格不随余额变红 | UpdateCosts：不足红/够黄 | ✅ 余额对比变色 |
| NPCGoodsPanel | 缺 "Can use Item/Cannot use Item" 需求提示 | UpdateColours：可用 Aquamarine/不可 Red | ✅ 按 CanUseItem 显示 |
| MagicDialog | 未学习技能 Required Level 恒金色 | 等级足够 LimeGreen/不足 Red | ✅ 按玩家等级红/绿 |
| MiniMapDialog | NPC 标记无悬停名；昼夜图标无提示 | control.Hint = name / TimeOfDayImage.Hint | ✅ TooltipText |
| BigMapDialog | NPC 标记无悬停名 | control.Hint = name | ✅ TooltipText |
| GameStoreDialog | 收藏按钮无悬停提示 | RefreshFavourite Hint | ✅ TooltipText |

## 3. 修复详情

### 3.1 CommunicationDialog 在线状态（UpdateStateLabel 移植）

旧版点击状态按钮循环 在线→忙碌→离开→在线，并即时刷新文本+颜色。

- `CommunicationDialog.RefreshOwnState(OnlineState)`：文本（在线/离开/忙碌/离线）
  与颜色（LimeGreen/Orange/Red/Gray）；
- `GameScene.CycleOnlineState` 切换后回调刷新（对齐旧版 UpdateStateLabel 的即时性）。

### 3.2 NPCGoodsPanel 价格与需求提示（UpdateCosts/UpdateColours 移植）

- 商品行价格：余额不足 → Red，足够 → 黄（旧版 CostLabel 红/黄）；
- 可装备/可消耗类商品行追加 "Can use Item"（Aquamarine）/ "Cannot use Item"（Red），
  判定复用 `GameScene.CanUseItem`（性别/职业/等级/AC 等校验，与旧版同源）。

### 3.3 MagicDialog Required Level 颜色

未学习技能的经验文本：玩家等级满足需求 → LimeGreen，否则 Red
（旧版 `ExperienceLabel.ForeColour` 红/绿分支）。

### 3.4 地图标记悬停提示（Hint → TooltipText）

- MiniMapDialog / BigMapDialog 的 NPC 标记：悬停显示 NPC 名
  （旧版 `control.Hint = name`）；
- MiniMapDialog 昼夜图标：悬停显示 黎明/白天/黄昏/夜晚；
- GameStoreDialog 收藏按钮：悬停显示 收藏/取消收藏。

## 4. 验证

- `dotnet build GodotClient/ZirconClient.csproj`：✅ 0 警告 0 错误；
- 建议真机核对：好友面板点状态按钮看颜色变化、商店买不起的商品价格红色 +
  Can/Cannot use Item 提示、技能页未学习技能 Required Level 红绿、
  大小地图悬停 NPC 显示名字、小地图昼夜图标提示、商城收藏按钮提示。

## 5. 变更文件

| 文件 | 修复 |
|---|---|
| `GodotClient/Controls/CommunicationDialog.cs` | RefreshOwnState（在线状态文本+颜色） |
| `GodotClient/Scripts/GameScene.cs` | CycleOnlineState 回调刷新 |
| `GodotClient/Controls/NPCGoodsPanel.cs` | 价格变色 + Can/Cannot use Item |
| `GodotClient/Controls/MagicDialog.cs` | Required Level 红/绿 |
| `GodotClient/Controls/MiniMapDialog.cs` | NPC 标记 + 昼夜图标 Tooltip |
| `GodotClient/Controls/BigMapDialog.cs` | NPC 标记 Tooltip |
| `GodotClient/Controls/GameStoreDialog.cs` | 收藏按钮 Tooltip |

对照旧版：`Client/Scenes/Views/CommunicationDialog.cs`（UpdateStateLabel）、
`NPCDialog.cs`（NPCGoodsPanel.UpdateCosts/UpdateColours）、
`MagicDialog.cs`、`MiniMapDialog.cs`、`BigMapDialog.cs`、`GameStoreDialog.cs`。
