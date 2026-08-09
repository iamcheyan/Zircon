# 快捷腰带栏 (BeltDialog) 功能说明与默认锚点修复文档

## 1. 界面元素解答

在你提供的截图（`/tmp/sumika-clip-1786256776207727205.png`）中，屏幕上展示的 10 个灰色格子为传奇 3 / Zircon 的**快捷腰带栏 (`BeltDialog` / 快捷药品槽)**。

### 快捷腰带栏的职能：
1. **存放快捷物品**：玩家可以将背包中的红药、蓝药、随机卷轴、回城卷、特殊道具拖入这 10 个格子。
2. **快捷使用 (1-9, 0 键)**：每个格子右上角有数字角标 `1-9` 和 `0`。按键盘主键盘区数字键 `1-9, 0` 可直接使用对应槽位中的药品/卷轴。
3. **折叠与显示 (F 键)**：
   - 按键盘 **`F` 键** 或点击底部主面板右侧的**腰带按钮 (BeltButton)** 可以自由开关/折叠该栏目。

---

## 2. 为什么之前会诡异地悬挂在左上角 (0, 0)？

### 排查定位：
- `BeltDialog` 原本设计了 `ApplyDefaultAnchor()` 方法（未手动拖拽时，将其自然吸附在主面板 `MainPanel` 的右上上方）。
- 但在 `GameScene.cs` 的 `LayoutHud()` 初始化中，**遗漏了对 `_beltDialog.ApplyDefaultAnchor()` 的调用**。
- 这导致在首次打开时，腰带栏没有计算相对主面板的基准位置，而是直接降级回退到了初始坐标 `(0, 0)`（左上角）。

---

## 3. 修复方案

1. **补全默认布局 ([`GameScene.cs`](file:///home/tetsuya/development/Zircon/GodotClient/Scripts/GameScene.cs#L4623-L4625))**：
   在 `LayoutHud()` 中加入 `_beltDialog.ApplyDefaultAnchor(vp, _mainPanel.Location, _mainPanel.Size)`。
   - 当玩家未自定义拖拽位置时，腰带栏会非常精致地默认贴在主操作栏右侧上方。
2. **位置持久化保存**：
   当玩家手动拖动腰带栏到屏幕任意位置后，其坐标会自动保存写回 `ClientSettings`，下次登录或切地图保持玩家放好的自定义位置。
