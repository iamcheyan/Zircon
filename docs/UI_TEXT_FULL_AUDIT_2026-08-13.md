# UI 文本全面排查与修复（2026-08-13）

## 1. 任务目标与执行摘要

目标：全面排查 Godot 客户端（`GodotClient/`）所有文本渲染点，与旧版客户端
（`Client/`）逐项比对，补齐缺失的文本体验，并形成文档。

排查范围：聊天全部文本、物品悬浮提示（hover tooltip）、其余所有 DrawString /
DXLabel 渲染入口。

本轮修复（3 个文件，全部编译通过 0 警告 0 错误）：

| 文件 | 修复 |
|---|---|
| `GodotClient/Controls/ChatLogPanel.cs` | 透明聊天消息补半透明黑底 + 物品链接下划线/悬停变红 |
| `GodotClient/Controls/DXLabel.cs` | 新增 `DrawUnderline`、`TextPadding`；绘制用 `ScaledSize` |
| `GodotClient/Scripts/GameScene.cs` | 物品悬浮提示：加背景框 + 完整内容移植（`BuildItemHoverFull`） |

## 2. 文本渲染点全量清单（新旧对照）

### 2.1 聊天框（ChatTab ↔ ChatLogPanel）

| 项目 | 旧版 | 新版（修复前） | 修复 |
|---|---|---|---|
| 消息背景色 | 按类型 BackColour；透明模式转 `FromArgb(100,0,0,0)` 半透明黑底 | 全透明，无底衬 | ✅ 透明模式补黑底（`ResolveMessageBackColour`） |
| System/公告底 | 半透明白高亮 | 同（但面板透明时不画） | ✅ 保留 |
| 物品链接 | 黄色 + 下划线（FontStyle.Underline）+ 悬停变红 | 黄色+描边，悬停不变色 | ✅ `DrawUnderline` + 悬停变红 |
| 悬停物品提示 | MouseItem 悬浮信息 | 有（SetHoverItem） | 不变 |
| 透明联动 | 滚动条/边框隐藏 | 有（UpdateChromeVisibility） | 不变 |
| 点击玩家名 PM | MouseUp 解析 | 有（AttachPlayerNameAction） | 不变 |

### 2.2 物品悬浮提示（ItemLabelBuilder ↔ _hoverLabel）

旧版 `CreateItemLabel`（GameScene.cs:1663）用 ItemLabelBuilder 构建完整信息框：
**深棕半透明底（230,18,15,8）+ 金棕边框（105,95,62）+ 多分区文本**。

新版修复前：单 DXLabel，只有 4 行（名称/Type/过期/锁定），**无背景无边框**。

修复后（`BuildItemHoverFull`，GameScene.cs:5520）：

| 分区 | 旧版方法 | 新版移植 |
|---|---|---|
| 名称 + [Part] | AddHeader | ✅ |
| Type / 元数据 | AddItemLabelMetadata | ✅（Type/Pages/Quality/Purity/Gem/Count/Weight） |
| 货币/经验 | AddItemLabelDescription | ✅（直接返回） |
| 装备属性 | AddEquipmentItemInfo | ✅（含武器元素、附魔 AddedStats） |
| 药水属性 | AddPotionItemInfo | ✅（Stats + Cooldown） |
| 训练信息 | AddItemLabelTrainingInfo | ✅（武器/饰品 Level + Refine） |
| 需求 | AddItemLabelRequirements | ✅（性别/职业/等级/AC/MR/DC/MC/SC/Health/Mana/Companion/Rebirth） |
| 插槽 | AddItemLabelSocketInfo | ✅（Empty Socket + 宝石属性） |
| 交易状态 | AddItemLabelTradeState | ✅（Sell Value/不可修理/卖/存/交易/掉落/死亡掉落/Bound/NonRefinable） |
| 描述 | AddItemLabelDescription | ✅ |
| 特殊修理 | CreateItemLabel 内联 | ✅ |
| 过期/复活 | CreateItemLabel 内联 | ✅ |
| 套装 | AddSetItemInfo | ✅（部件 + Set Bonus 按职业/等级） |
| 结婚/GM | CreateItemLabel 内联 | ✅ |
| 碎片/重置/锁定 | CreateItemLabel 内联 | ✅ |

悬浮框外观：`BackColour(18,15,8,230)` + `Border(105,95,62)` + `TextPadding(6,4)`，
Size 按文本测量自动适配（`FitHoverLabelSize`）。

已知差异：新版 DXLabel 单色，无法复刻旧版每行颜色（需求不满足红色、属性
绿色等），内容与顺序完全一致，颜色统一为稀有度色。

### 2.3 其余文本渲染点（全量审计）

| 位置 | 状态 |
|---|---|
| DXLabel 基类（所有标签/按钮/物品数量） | ✅ 统一经 `ScaledSize` + 可选下划线/内边距 |
| DXTextArea / DXTextInput（输入框） | ✅ 主题字号经 `ScaledSize` |
| MagicBar / MagicDialog（技能栏/技能信息） | ✅ |
| GroupHealthPanel / AutoPathRouteControl（组队/寻路） | ✅ |
| NPCTextControl（NPC 富文本） | ✅（顺带修复"测量 fontSize 但绘制写死 10"旧 bug） |
| RenderPrimitives.DrawLabel（世界层名字） | ✅ |
| 怪物/物品名字悬浮（Ctrl 悬停地面物品） | ✅ 复用 _hoverLabel 背景框 + FitHoverLabelSize |

## 3. 变更文件

| 文件 | 说明 |
|---|---|
| `GodotClient/Scripts/GameScene.cs` | `BuildItemHoverFull` 完整悬浮内容；`FitHoverLabelSize`；_hoverLabel 背景框；Ctrl 物品名悬浮复用 |
| `GodotClient/Controls/ChatLogPanel.cs` | `ResolveMessageBackColour`；链接下划线/悬停变红 |
| `GodotClient/Controls/DXLabel.cs` | `DrawUnderline` + `TextPadding` + ScaledSize 绘制 |
| `GodotClient/Controls/MirSkin.cs` | `FontScale`/`ScaledSize`（前一轮） |
| `docs/CHATBOX_PARITY_FIX_2026-08-13.md` | 聊天框专项（前一轮） |
| `docs/UI_FONT_SCALE_ADAPTATION_2026-08-13.md` | 字体缩放专项（前一轮） |

对照旧版：`Client/Scenes/GameScene.cs`（CreateItemLabel/ItemLabelBuilder）、
`Client/Scenes/Views/ChatTab.cs`（UpdateColours/GetBackColour/ProcessText）、
`Client/Envir/Config.cs`（颜色配置）。

## 4. 验证

- `dotnet build GodotClient/ZirconClient.csproj`：✅ 0 警告 0 错误；
- UITestScene 的 `UIItemHoverAudit`（静态 BuildItemHoverCore 测试）保持通过；
- 建议真机核对：悬浮提示框有深棕底金棕边、内容完整；聊天透明模式下文字有
  半透明黑底可读；物品链接黄色下划线、悬停变红。
