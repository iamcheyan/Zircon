# UI 文本垂直偏移修复：Godot DrawString 基线语义（2026-08-13）

## 1. 问题现象

真机验证（Xvfb 无头环境跑客户端 + 截图）发现：**所有 UI 文本整体偏高**——
物品数量数字贴在格子上沿、标签/按钮文字偏上、聊天行距视觉不对齐。
用户观察："所有的文本都偏高，是行距还是定位有问题"。

## 2. 根因

**Godot `CanvasItem.DrawString` 的 Y 坐标是字体基线（baseline），
旧版 GDI `TextRenderer.DrawText` 的 Y 坐标是文本顶部（top）。**

旧版 DXLabel（`Client/Controls/DXLabel.cs:429`）：

```csharp
TextRenderer.DrawText(graphics, Text, Font,
    new Rectangle(1, 0, width, height), ForeColour, DrawFormat);
// rectangle 的 Y 是文本顶部
```

新版 DXLabel（修复前，`GodotClient/Controls/DXLabel.cs:58`）：

```csharp
Vector2 linePos = new(pos.X, pos.Y + i * lineHeight);
DrawString(font, linePos, ...);   // Godot: Y 是基线！
```

基线的 Y 比文本顶部的 Y **高一个 ascent（字体升部）**，于是所有文字整体上移
约半个字高 → 文本偏高。

`MirSkin.MeasureText` 用 `GetStringSize`（返回含行距的整体高度，语义=顶部尺寸），
所以测量正确、绘制错位——这正是"行距没错但看着偏高"的原因。

## 3. 修复

`GodotClient/Controls/DXLabel.cs` DrawControl：

```csharp
// Godot DrawString 的 Y 是基线，旧版 TextRenderer 的 Y 是文本顶部。
// 补偿 ascent，使视觉位置与旧版一致。
float ascent = font.GetAscent(MirSkin.ScaledSize(FontSize));
Vector2 linePos = new(pos.X, pos.Y + i * lineHeight + ascent);
```

## 4. 覆盖范围审计

| 绘制点 | 语义 | 处理 |
|---|---|---|
| `DXLabel.DrawControl`（所有标签/按钮/列表/聊天/物品数量） | 顶部语义（旧版 TextRenderer） | ✅ 已补偿 ascent |
| `RenderPrimitives.OriginalNameBaseline`（世界层名字） | 已自行做顶部→基线换算 | 不动 |
| `RenderPrimitives.DrawLabel`（世界层标签） | 已按基线语义传入 | 不动 |
| `NPCTextControl`（NPC 富文本） | 已用 `y + ScaledSize(fontSize)` 近似基线 | 不动 |
| `MagicBar`（技能栏数字/冷却） | 图标内相对定位（基线语义写法） | 不动 |
| `MagicDialog`（技能信息） | 窗口内基线语义写法 | 不动 |
| `DXTextArea`/`DXTextInput`（输入框） | Godot 原生 LineEdit/TextEdit 主题字号 | 不动 |

核心结论：**只有 DXLabel 是纯顶部语义**，且它承载了 UI 文本的绝大部分，
一处修复覆盖全部。

## 5. 验证

- `dotnet build GodotClient/ZirconClient.csproj`：✅ 0 警告 0 错误；
- **真机验证**（Xvfb + openbox + Godot 4.6.3 mono + 测试账号 TestHero 登录 +
  scrot 截图 + 视觉核对）：
  - 修复前：背包物品数量数字贴在格子上沿；
  - 修复后：数字垂直居中于格子中部 ✅；
  - 快捷栏数字水平居中、贴近图标底部（对齐旧版布局）✅。

## 6. 变更文件

| 文件 | 说明 |
|---|---|
| `GodotClient/Controls/DXLabel.cs` | DrawControl 基线补偿（ascent） |

## 7. 无头环境验证流程（可复用）

```bash
# 1. 服务端
cd ~/development/zircon/Debug/ServerCore && dotnet ServerCore.dll &
# 2. Xvfb 虚拟显示 + openbox（窗口焦点）
Xvfb :99 -screen 0 1280x800x24 &  DISPLAY=:99 openbox &
# 3. Godot 客户端（自动登录测试账号）
DISPLAY=:99 godot-mono --path ~/development/zircon/GodotClient \
    -- --user test@test.com --pass test123 --char TestHero --window &
# 4. 截图 + 按键
DISPLAY=:99 scrot /tmp/shot.png
DISPLAY=:99 xdotool search --name ZirconClient | xargs -I{} xdotool windowactivate --sync {}
DISPLAY=:99 xdotool key --window <WID> w   # 背包 (W) / 角色 (Q) / 技能 (E) / 任务 (K)
```
