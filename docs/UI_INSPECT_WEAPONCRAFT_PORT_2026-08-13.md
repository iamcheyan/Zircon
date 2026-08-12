# UI 移植核对：查看角色（Inspect）与武器制作（WeaponCraft）（2026-08-13）

## 1. 任务目标与执行摘要

目标：核对旧版 Client（`Client/Scenes/Views/`）中 **InspectBox（查看他人角色）** 与
**NPCWeaponCraftWindow（NPC 武器制作）** 两个窗口在 Godot 客户端（`GodotClient/`）的移植状态，
确认功能链路完整，并补充移植文档。

结论：**两个窗口均已完整移植**，但新版采用了与旧版不同的架构组织方式
（旧版是独立窗口类，新版合并进既有窗口的「模式」），功能与交互一一对应。

| 旧版窗口 | 新版实现 | 移植方式 |
|---|---|---|
| `InspectBox`（`CharacterDialog(true)` 实例） | `CharacterDialog._inspectMode`（`ApplyInspect`/`ShowOwn`） | 模式复用：同一窗口在「自己的角色页」与「查看他人页」间切换 |
| `NPCWeaponCraftWindow`（独立 `DXWindow`） | `NPCAdvancedPanel.BuildWeaponCraft()`（`NPCDialogType.WeaponCraft`） | 面板模式：作为 NPC 对话框的附属面板（`Location(0, Size.Y)`） |

验证：`dotnet build GodotClient/ZirconClient.csproj` 通过（0 警告 0 错误）。

## 2. Inspect（查看他人角色）

### 2.1 旧版实现

- 窗口类：`CharacterDialog`，GameScene 中实例化为 `InspectBox = new CharacterDialog(true)`；
  第二个参数为查看模式。
- 定位（`SetDefaultLocations`）：
  ```csharp
  CharacterBox.Location = Point.Empty;                          // 自己的角色窗在 (0,0)
  InspectBox.Location = new Point(CharacterBox.Size.Width, 0);  // 查看窗在角色窗右侧
  ```
- 数据来源：`S.Inspect` 网络包 → `InspectBox.ApplyInspect` 填充等级/职业/装备/行会。
- 装备格：`GridType.Inspect` 只读格，展示 `InspectBox.Equipment`；
  套装预览按 `InspectBox.Level/Class/Equipment` 计算（GameScene.cs:2860 附近）。

### 2.2 新版实现（GodotClient）

- 类：`GodotClient/Controls/CharacterDialog.cs`，内置 `_inspectMode` 状态：
  - `ApplyInspect(S.Inspect info)`：置 `_inspectMode = true`，切换背景 `Interface[115]`、
    尺寸 `InspectSize(331,374)`，隐藏属性/隐士/加点面板，显示纸娃娃与只读装备格，
    填充 `_inspectItems`（17 格）与行会/配偶信息；
  - `ShowOwn()`：恢复 `_inspectMode = false`、背景 `Interface[110]`、`OwnSize`，
    回到可操作的角色装备页。
- 接线：`GameScene.OnInspect`（S.Inspect 事件）→ `ApplyInspect` → `WindowManager.Open`；
  排行榜查看走 `_rankingDialog.ApplyInspect` 分支。
- 定位：`_characterDialog.Location = Vector2I.Zero`（LayoutHud），查看时复用同一窗口，
  不另开右侧窗口——这是与旧版唯一的布局差异（合并显示，功能等价）。

### 2.3 新旧对照

| 项目 | 旧版 | 新版 | 一致 |
|---|---|---|---|
| 查看触发 | `S.Inspect` → InspectBox | `S.Inspect` → `OnInspect` → `ApplyInspect` | ✅ |
| 展示数据 | 名称/等级/职业/装备/行会/配偶 | 同（`_characterNameLabel`/`_guild*`/`_inspectItems`/`_doll.SetInspect`） | ✅ |
| 装备格 | `GridType.Inspect` 只读 | `GridType.Inspect` + `ReadOnly=true` | ✅ |
| 纸娃娃 | `CharacterDialog` 纸娃娃 | `PaperDoll.SetInspect(info, items)` | ✅ |
| 位置 | 角色窗右侧（`(CharW, 0)`） | 复用角色窗原位（`(0,0)`） | ⚠️ 布局差异 |
| 关闭后 | Dispose | `ShowOwn()` 切回自己 | ✅ 等效 |

## 3. NPC 武器制作（NPCWeaponCraftWindow）

### 3.1 旧版实现

- 窗口类：`NPCWeaponCraftWindow : DXWindow`（NPCDialog.cs:7171），客户区 `250×280`。
- 结构：
  - `TemplateCell`：模板/武器格（`GridType.WeaponCraftTemplate`）
  - `Yellow/Blue/Red/Purple/Green/GreyCell`：六个材料格（对应品质）
  - `PreviewImageBox`：`Equip` 图库预览（1110=默认，1111-1114=战/法/道/刺）
  - `ClassComboBox`：职业下拉（None/Warrior/Wizard/Taoist/Assassin）
  - `AttemptButton`：Craft 按钮；`CanCraft` = 费用≤金币 && 模板已链 && 职业非 None
- 费用：`Globals.CraftWeaponPercentCost`，若模板为成品武器按 Rarity 换
  `Common/Superior/EliteCraftWeaponPercentCost`。
- 发送：`C.NPCWeaponCraft { Class, Template, Yellow…Grey }`（`CellLinkInfo` 链接格信息，
  不真正移动物品；发送前各格 `Link.Locked = true`）。
- 定位：`NPCWeaponCraftBox.Location = (Size.Width - w, Size.Height)`（NPC 对话框右侧），
  `NPCDialog` 切换页面时 `Visible` 开关。

### 3.2 新版实现（GodotClient）

- 类：`GodotClient/Controls/NPCAdvancedPanels.cs` 的 `NPCAdvancedPanel`，
  模式 `NPCDialogType.WeaponCraft` → `BuildWeaponCraft()`：
  - 面板基础：`Base("Weapon Craft", 268, 326)`
  - `AddGrid(GridType.WeaponCraftTemplate/Yellow/Blue/Red/Purple/Green/Grey, …)`
    （坐标 107,40 / 18,104 / 57,104 / 96,104 / 18,164 / 57,164 / 96,164）
  - `_weaponPreview`：`Equip` 图库预览，模板为 WeaponTemplate 时按职业显示 1111-1114
  - 「职业」按钮循环 `RequiredClass`（CycleClass），显示 `职业: X`
  - `_weaponCraftButton`（制作）→ `GameScene.SendNPCWeaponCraft`（8 参数 CellLinkInfo）
  - `UpdateWeaponCraftState()`：费用按 Rarity 计算、按钮 Enabled = 模板链+职业+金币充足
- 触发：`NPCDialog` 页面切换时 `_advanced.Configure(_page.DialogType)`，
  面板定位 `Location(0, Size.Y)`（NPC 对话框正下方）。
- 响应：`GameScene.OnNPCWeaponCraft(S.NPCWeaponCraft)` → `ConsumeNpcLinks` 消耗材料 +
  聊天提示「武器制作成功/失败」。

### 3.3 新旧对照

| 项目 | 旧版 | 新版 | 一致 |
|---|---|---|---|
| 材料格 | 7 格（模板+6 品质） | 7 格（同 GridType） | ✅ |
| 预览 | `Equip` 1110/1111-1114 | 同（`_weaponPreview`） | ✅ |
| 职业选择 | 下拉 ComboBox | 「职业」按钮循环 | ⚠️ 交互差异（等效） |
| 费用 | Rarity 分支 | 同（`UpdateWeaponCraftState`） | ✅ |
| 发送 | `C.NPCWeaponCraft` + Lock | `SendNPCWeaponCraft`（同包） | ✅ |
| 材料处理 | 链接格 + CellLinkInfo | 同（`Link()`/`BeginSubmit`） | ✅ |
| 位置 | NPC 框右侧 | NPC 框正下方 `(0, Size.Y)` | ⚠️ 布局差异 |
| 结果反馈 | 服务端响应 | `OnNPCWeaponCraft` + 系统提示 | ✅ |

## 4. 布局差异说明

两处布局差异均为**新版架构选择的自然结果，非功能缺失**：

1. **Inspect 复用角色窗**：旧版查看他人时在角色窗右侧另开一窗；新版同一
   `CharacterDialog` 窗口在查看/自己两态间切换，屏幕只占一个窗口位置。
2. **WeaponCraft 面板化**：旧版独立窗口浮在 NPC 框右侧；新版作为
   `NPCAdvancedPanel` 附属面板贴在 NPC 框正下方，与精炼/碎片等其它工艺窗口
   共用同一套面板机制（`Location(0, Size.Y)`），交互统一。

## 5. 验证

- `dotnet build GodotClient/ZirconClient.csproj`：✅ 成功（0 警告 0 错误）
- 网络链路核对：`C.NPCWeaponCraft` / `S.Inspect` / `S.NPCWeaponCraft` 收发两端均接线
  （`ServerConnection` 事件 ↔ `GameScene` 处理 ↔ 控件层调用）
- GridType：`WeaponCraft*` 7 个网格类型新旧同名同义

## 6. 相关文件

| 旧版 | 新版 |
|---|---|
| `Client/Scenes/Views/CharacterDialog.cs` | `GodotClient/Controls/CharacterDialog.cs` |
| `Client/Scenes/Views/NPCDialog.cs`（NPCWeaponCraftWindow:7171） | `GodotClient/Controls/NPCAdvancedPanels.cs`（BuildWeaponCraft:801） |
| `Client/Scenes/GameScene.cs`（InspectBox/NPCWeaponCraftBox 实例与定位） | `GodotClient/Scripts/GameScene.cs`（OnInspect:2372 / OnNPCWeaponCraft:2708 / SendNPCWeaponCraft:5836） |
| — | `GodotClient/Network/ServerConnection.cs`（事件接线） |
