# HUD「整体上移」观感分析与修复计划

> 日期：2026-08-09  
> 状态：**已按本文实施修复**（2026-08-09）  
> 相关截图：城镇 Bichon Town / 废矿等（技能栏贴顶、中间悬空滚动条、血条像没对齐）

---

## 1. 用户观感

1. 顶部技能/Buff、小地图像被**裁掉一部分**。  
2. 底部操作栏像**往上浮**，血条/蓝条与底图凹槽对不齐。  
3. 怀疑：**整层 UI 统一往上偏了一点**，整体下移即可。

---

## 2. 结论（判断）

### 2.1 不是「整层 UI 统一少了一个向下偏移」

| 证据 | 说明 |
|------|------|
| `CanvasLayer` 变换 | 仅 `Scale(UiScale)`，**无 Y 平移**（`RefreshUiScale`） |
| 顶/底锚点是设计 | 小地图/Buff：`Y=0`；MainPanel：`Y = vp.Y - H` |
| 截图底边 | 主面板贴游戏区底边（编辑器绿条在视口外），**不是**板子抬高后底下空一截 |

若强行「整层 UI 下移 N 像素」：

- 底栏下空、顶栏上空，与原版 `SetDefaultLocations` 更不一致。  
- **不采用**全局下移作为方案。

### 2.2 真实问题是「分项叠加」

| # | 现象 | 根因 | 位置 |
|---|------|------|------|
| A | 技能栏整条贴在**屏幕顶部** | ① 旧默认 `(10,0)` / 配置 `UserMoved`；② 仅当 `!UserMoved` 才锚底；③ 配置里若是 `(50,0)` 等仍被当成用户位置 | `MagicBar.cs`、`ClientSettings.MagicBarPosition`、`LayoutHud` |
| B | 顶被「切一点」 | 小地图 `Y=0` 贴顶 + 编辑器嵌入窗口边 | 预期行为；非全局偏移 |
| C | 地图中下**悬空竖滚动条** | `ChatLogPanel` 默认 Transparent + HideTab + FadeOut，淡出后只剩滚动条 | `ChatLogPanel.cs` |
| D | 血蓝「对不齐」 | MainPanel **内部**填充/标签居中与 `GameInter[50]` 凹槽，非整板 Y | `MainPanel.cs` |

### 2.3 布局数学（正确理解）

```
物理视口 (viewport) ──÷── UiScale ──► 逻辑视口 vp
CanvasLayer.Transform = Scale(UiScale)  // 原点 (0,0)，无平移

MainPanel:  ( (vp.X - W)/2 , vp.Y - H )
MiniMap:    ( vp.X - miniW , 0 )
Buff:       ( vp.X - miniW - buffW - 5 , 0 )
MagicBar:   未拖动时 → 主面板左上侧
ChatLog:    主面板上方
```

世界层 `GameScene.Scale = 2` 与 UI 层分离，**不会**把 UI 整体抬高。

---

## 3. 修复计划（按序）

### P0-A — 技能栏默认必须在底栏旁

1. 收紧「可恢复的记忆位置」：贴顶带（`Y` 过小）一律视为未设置。  
2. 启动时若配置无效，强制 `UserMoved=false` 并 `ApplyDefaultAnchor`。  
3. `LayoutHud` / 尺寸变化后继续锚底（未拖动时）。

### P0-C — 透明聊天无内容时不露滚动条

1. `FadeOut` / 透明且无可见行时：隐藏滚动条（或整块 chat log 不抢视线）。  
2. 有消息且未淡出时再显示滚动条。

### P1-D — MainPanel 血蓝对齐

1. 核对 `CreateBar(35,22/36)` 与填充绘制原点。  
2. `CenterBarLabel` 用更稳的垂直居中（含描边时的半像素）。  
3. 必要时相对底图微调 1～2px（以 `GameInter` 50/52/54 为准）。

### 不做

- 不给 `_uiLayer` 加全局 `origin.Y += N`。  
- 不把小地图改成 `Y > 0` 除非原版有同等边距。

---

## 4. 验证清单

- [x] 新开/清无效配置后：技能栏在**主面板左侧上方**，不在屏幕顶。  
- [x] 读盘时 `Y < 48` 的 MagicBarPosition 丢弃；Layout 时 `ClearInvalidPersistedPosition`。  
- [x] 透明/淡出/无消息：聊天滚动条强制隐藏（修 HideWhenNoScroll 覆盖）。  
- [x] 血蓝填充相对条容器垂直居中；标签 +1px 贴中线。  
- [x] `dotnet build GodotClient` 通过。  
- [ ] **请人工进游戏确认**：技能栏在底、无悬空滑块、血蓝目视对齐。  

### 4.1 已改文件

| 文件 | 改动 |
|------|------|
| `docs/UI_GLOBAL_OFFSET_ANALYSIS.md` | 本文 |
| `GodotClient/Controls/MagicBar.cs` | 贴顶配置无效；`ClearInvalidPersistedPosition` |
| `GodotClient/Scripts/ClientSettings.cs` | 读盘丢弃 `Y < 48` 的 MagicBarPosition |
| `GodotClient/Scripts/GameScene.cs` | LayoutHud 清脏配置后锚底 |
| `GodotClient/Controls/ChatLogPanel.cs` | `UpdateChromeVisibility`；淡出不画底 |
| `GodotClient/Controls/MainPanel.cs` | 条填充/标签垂直对齐微调 |

---

## 5. 涉及文件

- `GodotClient/Controls/MagicBar.cs`
- `GodotClient/Scripts/GameScene.cs`（LayoutHud / 锚点）
- `GodotClient/Controls/ChatLogPanel.cs`
- `GodotClient/Controls/MainPanel.cs`
- `GodotClient/Scripts/ClientSettings.cs`（如需读时归一化 MagicBarPosition）

---

## 6. 一句话

**观感像整体上移，实际是技能栏贴顶 + 透明聊天只剩滚动条 + 主面板内部条位；按控件修，不整层下移。**
