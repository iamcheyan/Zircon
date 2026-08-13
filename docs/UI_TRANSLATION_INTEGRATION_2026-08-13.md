# 客户端 UI 翻译完整接入（第一批：基础设施 + 核心窗口）（2026-08-13）

## 1. 目标

把旧版客户端（`Client/`）的完整翻译体系接入新版 Godot 客户端（`GodotClient/`）：
UI 文本从硬编码中文字面量改为 `Lang.XXX` 键引用，支持 中/英 即时切换。

## 2. 现状调研

- **旧版客户端**：`Client/Envir/Translations/` 三件套
  - `StringMessages.cs`（抽象基类，**757 个键**，按 Message/Common/Scenes 分区）
  - `EnglishMessages.cs` / `ChineseMessages.cs`（两套实现，各约 820 行，
    带 `[ConfigPath]` 可被 ini 覆盖）
  - UI 通过 `CEnvir.Language.XXX` 引用（692 个键被 UI 使用）
  - 加载：`CEnvir.LoadLanguage()` 按 `Config.Language` 选实例
- **新版客户端**：全部 UI 文本硬编码中文（**1024 个唯一中文字面量、1277 处**），
  无任何翻译引用；仅 ConfigDialog 有"语言"选项发送 `C.SelectLanguage` 给
  服务端（只管聊天消息双语）。
- **基础设施**：`LibraryCore/ConfigReader.cs`（ConfigPath/ConfigSection 特性 +
  ini 加载）两端共用，新版可直接复用。

## 3. 实现（第一批）

### 3.1 翻译三件套移植

```
Client/Envir/Translations/  →  GodotClient/Translations/
  StringMessages.cs  EnglishMessages.cs  ChineseMessages.cs
  命名空间: Client.Envir.Translations → ZirconClient.Translations
```

### 3.2 Lang 门面类（GodotClient/Scripts/Lang.cs）

- `Lang.Current`：当前语言实例（默认 ChineseMessages）；
- `Lang.XXX`：757 个属性转发（生成脚本产出，等价旧版 `CEnvir.Language.XXX`）；
- `Lang.Reload()`：按语言重新加载，优先级 `--lang` 命令行 > `ClientSettings.Language`。

### 3.3 接入点

| 文件 | 改动 |
|---|---|
| `ClientSettings.cs` | `Load()` 里 `Lang.Reload()`（放在 file.Load 之前——ini 不存在时 Load 提前 return 会导致语言不初始化；末尾再 Reload 一次用持久化语言） |
| `AutoLoginArgs.cs` | 新增 `--lang`/`--language` 参数（真机验证/测试用） |
| `ConfigDialog.cs` | 语言切换回调加 `Lang.Reload()`；标题/页签改用 Lang 键 |

### 3.4 窗口替换（第一批 3 个）

| 窗口 | 替换内容 |
|---|---|
| `InventoryDialog.cs` | 标题 InventoryDialogTitle、主货币 InventoryDialogPrimaryCurrencyTitle、次货币 InventoryDialogSecondaryCurrencyTitle |
| `MainPanel.cs` | 9 个按钮 Hint（MainPanel*ButtonHint，string.Format 带 `{0}` 键位）、属性图标 Hint（Class/Level/AC/MR/DC/MC/SC） |
| `ConfigDialog.cs` | 标题 CommonControlConfigWindowTitle、5 个页签 TabLabel |

## 4. 验证（Xvfb 无头环境）

```text
启动: godot-mono --path GodotClient -- --user test@test.com --pass test123 \
      --char TestHero --window --lang ENGLISH
结果: 背包标题 "Inventory"、金币 "Gold"（截图确认）✅
默认: 无 --lang 时保持中文 "背包"/"金币" ✅
```

- `dotnet build GodotClient/ZirconClient.csproj`：✅ 0 错误

## 5. 后续批次（未完成）

剩余窗口字面量替换（按数量排序，待做）：
GameScene.cs(186) / GuildDialog.cs(80) / LoginScene.cs(65) /
CharacterDialog.cs(55) / SelectScene.cs(54) / NPCAdvancedPanels.cs(53) /
CommunicationDialog.cs(49) / ConsignmentDialog.cs(47) / GameStoreDialog.cs(39) /
ChatOptionsDialog.cs(38) / QuestDialog.cs(27) / MonsterDialog.cs(27) 等。
键大多在旧版 StringMessages 已有（692 键被旧版引用），可直接映射；
新版新增 UI（FP/CP 图标、出售模式标题等）需补充键。

## 6. 变更文件

| 文件 | 说明 |
|---|---|
| `GodotClient/Translations/StringMessages.cs` | 新增（旧版移植，757 键） |
| `GodotClient/Translations/EnglishMessages.cs` | 新增（旧版移植） |
| `GodotClient/Translations/ChineseMessages.cs` | 新增（旧版移植） |
| `GodotClient/Scripts/Lang.cs` | 新增：翻译门面（757 属性转发 + Reload） |
| `GodotClient/Scripts/ClientSettings.cs` | Load 触发 Lang.Reload |
| `GodotClient/Scripts/AutoLoginArgs.cs` | --lang 参数 |
| `GodotClient/Controls/ConfigDialog.cs` | 标题/页签/语言切换接入 |
| `GodotClient/Controls/InventoryDialog.cs` | 标题/货币标签替换 |
| `GodotClient/Controls/MainPanel.cs` | 按钮/属性 Hint 替换 |
