# 交互/反馈类缺口排查修复（2026-08-13 第三轮）

## 1. 任务目标与执行摘要

在文本类排查（前三轮）完成后，转向**交互与反馈类**缺口：音效、拖拽、绘制钩子、
闪烁提醒、悬停反馈、动画淡入淡出、窗口特性。

排查方法：统计新旧客户端各交互机制的使用分布（Sound/DragDrop/AfterDraw/
MouseEnter/AlertIcon/Flash/Fade），逐窗口核对差异。

## 2. 排查结论

**已完整移植（无缺口）**：

| 机制 | 结论 |
|---|---|
| 物品特效（ItemEffectDecider） | 新版 DrawSpecialItemEffect 与旧版完全一致（仅 DarkStone 2020-2050 动画，偏移 -5/+20 相同） |
| 音效基础设施 | DXControl.Sound + DXItemCell.GetItemSound（武器/防具/药水分类音效）+ DXButton 默认 ButtonA |
| 悬停反馈 | MouseEnter/Leave 全部窗口已覆盖 |
| 闪烁/提醒 | ChatLogPanel AlertIcon（非选中 tab 新消息提醒）完整 |
| 窗口缩放 | DXWindow.AllowResize 边缘缩放已实现 |
| 聊天淡出 | ChatLogPanel FadeOut 完整 |

**发现并修复 3 处缺口**：

| 缺口 | 旧版 | 新版（修复前） | 修复 |
|---|---|---|---|
| 聊天物品链接音效 | ChatTab：链接 `Sound = ButtonC`（悬停触发） | 无 | ✅ 链接标签 Sound + MouseEnter 播放 |
| 任务窗口页签提醒 | QuestDialog.UpdateAlertIcons：可接任务>0 / 里程碑未领奖励 → 页签 AlertIcon | 无 | ✅ _tabAlerts + UpdateAlertIcons |
| 里程碑提醒联动 | 里程碑数据变化后刷新提醒 | 无刷新路径 | ✅ OnUserMilestones/OnMilestoneEarned 调 RefreshAlerts |

**记录为已知差异（不强行移植）**：
- ChatOptionsDialog 的 AllowDragOut（聊天选项卡拖出成独立窗口）：复杂度高，
  新版多 tab 已可用，拖出功能留待后续。

## 3. 修复详情

### 3.1 聊天物品链接音效（ChatTab.cs:682 → ChatLogPanel）

旧版 `ProcessText` 生成的链接标签带 `Sound = SoundIndex.ButtonC`，MouseEnter
时触发。新版链接标签补上 Sound 属性，并在 MouseEnter 处理器里播放
（旧版语义是悬停音效，而非点击音效）。

### 3.2 任务窗口页签提醒（QuestDialog.UpdateAlertIcons）

旧版：可接任务页签（可接任务>0）和里程碑页签（存在未领取奖励的里程碑）
显示 GameInter 240 感叹号图标。

- 新版 `AddTab` 给每个页签挂 `AlertIcon`（GameInter 240，右上角 78,4），
  存入 `_tabAlerts[page]`；
- `UpdateAlertIcons()`：page 1 按 `_available.Count > 0`，page 3 按
  `HasUnclaimedMilestoneReward()`；
- `SetQuests` 后自动刷新。

### 3.3 里程碑提醒联动

- GameScene 新增 `HasUnclaimedMilestoneReward()`（对齐旧版：IsComplete &&
  !Claimed && Info?.Reward != null）；
- `OnUserMilestones` / `OnMilestoneEarned` 后调用 `_questDialog?.RefreshAlerts()`
  （旧版里程碑奖励到达时提醒立即出现）。

## 4. 验证

- `dotnet build GodotClient/ZirconClient.csproj`：✅ 0 警告 0 错误；
- 建议真机核对：聊天点物品链接有提示音；有可接任务时任务窗口"可接任务"
  页签显示感叹号；里程碑完成未领取时"里程碑"页签显示感叹号。

## 5. 变更文件

| 文件 | 修复 |
|---|---|
| `GodotClient/Controls/ChatLogPanel.cs` | 链接标签音效（Sound + MouseEnter） |
| `GodotClient/Controls/QuestDialog.cs` | 页签 AlertIcon + UpdateAlertIcons |
| `GodotClient/Scripts/GameScene.cs` | HasUnclaimedMilestoneReward + 里程碑刷新联动 |

对照旧版：`Client/Scenes/Views/ChatTab.cs`（ProcessText Sound）、
`QuestDialog.cs`（UpdateAlertIcons）、`Client/Scenes/GameScene.cs`
（HasUnclaimedMilestoneReward）。
