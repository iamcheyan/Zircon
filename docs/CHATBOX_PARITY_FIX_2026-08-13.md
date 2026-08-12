# 聊天框新旧对比与体验修复（2026-08-13）

## 1. 问题现象

用户反馈新版聊天框体验不完整：**文字没有背景衬底、看不清**；记忆里旧版
「文本激活时有一个背景色」，新版什么都没有。

## 2. 新旧实现比对

### 2.1 旧版（`Client/Scenes/Views/ChatTab.cs`）

- 每条消息是独立 `DXLabel`，按消息类型设置 **前景色 + 背景色**（`UpdateColours`，
  ChatTab.cs:489-550）：
  ```csharp
  case MessageType.Normal:
      label.BackColour = GetBackColour(Config.LocalTextBackColour);
      label.ForeColour = Config.LocalTextForeColour;
      ...
  ```
- **关键逻辑 `GetBackColour`（ChatTab.cs:552-560）**：透明聊天模式下，原本全透明的
  背景色替换为 **`Color.FromArgb(100, 0, 0, 0)`（半透明黑底）**：
  ```csharp
  private Color GetBackColour(Color color)
  {
      if (Panel?.TransparentCheckBox.Checked == true && color == Color.FromArgb(0, 0, 0, 0))
          return Color.FromArgb(100, 0, 0, 0);   // ← 用户记忆中的"背景色"
      return color;
  }
  ```
  这保证透明模式下文字仍可读。
- System/公告/GM 密语背景是 `FromArgb(200, 255, 255, 255)`（半透明白高亮）。
- 物品链接（`ProcessText`，ChatTab.cs:631-708）：**黄色 + 下划线**
  （`FontStyle.Underline`），悬停变红 + 显示物品悬浮提示 + 音效。
- 透明模式还联动：滚动条隐藏、窗口边框隐藏、Tab 按钮半透明（`TransparencyChanged`）。

### 2.2 新版（`GodotClient/Controls/ChatLogPanel.cs`）

- 消息行是 `DXLabel`，**有** `BackColour` 字段，但所有类型的 `BackColour` 默认都是
  `Colors.Transparent`（alpha=0，见 `ClientSettings.cs:80-92`）；
- **致命点**：面板 `_Draw()` 在 `transparent` 为 true 时直接 `return`，不画任何背景
  （旧代码注释：消除悬浮灰色块）；而透明聊天又恰是新版默认设置
  （`defaultSettings.Transparent = true`）→ **文字直接浮在画面上，无任何底衬**。
- 物品链接：黄色 + 黑色描边，悬停只触发 `SetHoverItem`，**不变红、无下划线**。

### 2.3 差异结论

| 项目 | 旧版 | 新版（修复前） |
|---|---|---|
| 透明模式消息背景 | `FromArgb(100,0,0,0)` 半透明黑底 | 全透明，无底衬 ❌ |
| System/公告背景 | 半透明白高亮 | 同（但面板透明时不画） |
| 物品链接 | 黄色+下划线+悬停变红 | 黄色+描边，悬停不变红 ❌ |
| 悬停物品提示 | MouseItem 悬浮提示 | 有（SetHoverItem） |
| 透明联动 | 滚动条/边框隐藏 | 有（UpdateChromeVisibility） |

## 3. 修复内容（2026-08-13）

### 3.1 消息背景衬底（对齐旧版 GetBackColour）

`ChatLogPanel.cs`：

- 面板 `_Draw()` 注释更新：透明模式下背景由「每条消息的半透明黑底」承担，
  面板本身不再画整块底色（避免悬浮灰色块回归）；
- 新增 `ResolveMessageBackColour(backColour, transparent)`（ChatLogPanel.cs:495）：
  ```csharp
  if (!transparent) return backColour;
  if (backColour.A <= 0f) return new Color(0f, 0f, 0f, 100f / 255f);  // FromArgb(100,0,0,0)
  return backColour;
  ```
- 消息行构建时传入 `_tabSettings[_selectedTab].Transparent` 参与背景解析。

### 3.2 物品链接下划线 + 悬停变红

- `DXLabel.cs` 新增 `DrawUnderline` 属性：绘制文字底部 1px 下划线；
- `ChatLogPanel.AddLinkedItemLabels`：链接标签 `DrawUnderline = true`，
  悬停变红（`TextColour = Colors.Red`）+ `SetHoverItem`，离开恢复黄色
  （对齐旧版 MouseEnter/MouseLeave 行为）。

## 4. 验证

- `dotnet build GodotClient/ZirconClient.csproj`：✅ 0 警告 0 错误；
- 静态核对：背景解析覆盖消息行构建路径；下划线绘制与 DrawString 同循环；
- 建议真机核对：透明主聊天下看普通消息是否有半透明黑底、System 消息白底、
  物品链接是否黄色下划线且悬停变红。

## 5. 相关文件

| 文件 | 说明 |
|---|---|
| `GodotClient/Controls/ChatLogPanel.cs` | 消息背景解析 + 链接悬停 |
| `GodotClient/Controls/DXLabel.cs` | DrawUnderline 下划线绘制 |
| `GodotClient/Scripts/ClientSettings.cs` | 消息颜色配置（未改，默认值已对齐旧版） |

对照旧版：`Client/Scenes/Views/ChatTab.cs`（UpdateColours/GetBackColour/ProcessText）、
`Client/Envir/Config.cs`（颜色默认值）。
