# UI 字体缩放适配（FontScale）（2026-08-13）

## 1. 问题背景与执行摘要

现象：Godot 客户端 UI 文字普遍偏小，对比旧版客户端有明显差距。

根因（两个叠加因素）：

1. **字号单位不一致**：旧版字号用 `CEnvir.FontSize()`，单位是 **pt**（点）：
   ```csharp
   // 旧版 Client/Envir/CEnvir.cs
   public static float FontSize(float size)
       => (size - Config.FontSizeMod) * (96F / RenderingPipelineManager.GetHorizontalDpi());
   ```
   96 DPI 下 8pt ≈ **10.67px**。而新版 `DXLabel.FontSize` 直接是**逻辑像素**（8/9/10/13），
   沿用旧数值但没有 pt→px 换算，天然小约 25%。

2. **UiScale 不是字体缩放**：`_uiLayer.Transform = Scaled(UiScale)` 缩放的是整个 UI 层
   （含字体），但：
   - 窗口默认 1024×768 → UiScale = min(768/768, 1024/1024) = **1.0**，字体零放大；
   - 1920×1080 → UiScale = 1.406，9px 字仅 ~12.7px，依然偏小；
   - UiScale 上限 2，超过 2048×1536 后不再增长。

修复方案：引入**全局字体缩放系数 `MirSkin.FontScale`**（默认 4/3 ≈ 1.333，即 pt→px 视觉对齐），
所有文本绘制/测量统一经过 `ScaledSize()`。与 UiScale 正交：UiScale 管整个 UI 层缩放，
FontScale 管「基准字号本身」，两者相乘才是最终屏幕字号。

## 2. 实现

### 2.1 核心：MirSkin 全局系数

`GodotClient/Controls/MirSkin.cs`：

```csharp
/// <summary>全局字体缩放系数。旧版字号是 pt（96dpi 下 8pt ≈ 10.67px），
/// 新版是逻辑像素，直接沿用旧数值会小 ~25%；本系数把逻辑像素字号放大
/// 到与旧版视觉一致，并允许按 DPI/分辨率微调。</summary>
public static float FontScale = 4f / 3f;

/// <summary>应用全局缩放后的实际绘制字号（取整，保证布局测量一致）。</summary>
public static int ScaledSize(int size) => Mathf.Max(1, Mathf.RoundToInt(size * FontScale));
public static int ScaledSize(float size) => Mathf.Max(1, Mathf.RoundToInt(size * FontScale));
```

`MirSkin.MeasureText` 内部应用缩放：

```csharp
public static Vector2 MeasureText(string text, int fontSize)
{
    ...
    return font.GetStringSize(text, HorizontalAlignment.Left, -1, ScaledSize(fontSize));
}
```

**关键：测量与绘制必须走同一缩放**，否则换行/对齐/命中区域全部错位。

### 2.2 覆盖范围（所有文本渲染入口）

| 文件 | 改动 |
|---|---|
| `MirSkin.cs` | `FontScale` + `ScaledSize` + `MeasureText` 内部缩放 |
| `DXLabel.cs` | `DrawControl` 用 `ScaledSize(FontSize)`（所有标签/按钮/数量文字的基类） |
| `DXTextArea.cs` | `AddThemeFontSizeOverride("font_size", ScaledSize(...))`（聊天输入/多行） |
| `FilterDropDialog.cs`（DXTextInput） | 同上（单行输入框） |
| `MagicBar.cs` | 技能栏数字/冷却秒数 `ScaledSize(9/10)` |
| `MagicDialog.cs` | 技能信息名称/等级/经验 `ScaledSize(13/11/10)` |
| `GroupHealthPanel.cs` | 组队血条名称/数值 `ScaledSize(10/9)` |
| `AutoPathRouteControl.cs` | 自动寻路路点编号 `ScaledSize(10)` |
| `NPCTextControl.cs` | NPC 对话富文本：Glyph 记录 `ScaledSize(fontSize)`，绘制用记录的缩放字号（顺带修复「测量用 fontSize、绘制硬编码 10」的旧 bug） |
| `RenderPrimitives.cs` | 世界层名字标签 `DrawLabel`/`OriginalNameBaseline` 用 `ScaledSize` |

不动的部分：

- 旧版 `Config.FontSizeMod`（用户可调偏移）——新版暂未暴露 UI 设置项，如需要可后续把
  `FontScale` 接到 `ClientSettings`；
- 世界层 UI 缩放逻辑（UiScale/`_uiLayer`）——保持正交，不混入字体系数。

### 2.3 生效后的字号对照（FontScale = 4/3）

| 声明字号 | 旧版等效（pt） | 新版屏幕字号（×4/3） |
|---|---|---|
| 8 | 8pt ≈ 10.67px | 11px |
| 9 | 9pt = 12px | 12px |
| 10 | 10pt ≈ 13.3px | 13px |
| 12 | 12pt = 16px | 16px |
| 13 | 13pt ≈ 17.3px | 17px |

## 3. 验证

- `dotnet build GodotClient/ZirconClient.csproj`：✅ 0 警告 0 错误；
- 覆盖审计：`grep -rn 'DrawString'` 全部 7 个文件 + `AddThemeFontSizeOverride` 全部 2 个
  输入控件均经 `ScaledSize`；`MeasureText` 单点内部缩放，保证测量/绘制一致；
- 未运行真实客户端做像素级目测——建议启动后重点核对：聊天日志行高、NPC 对话富文本
  命中区域、MagicBar 冷却数字位置（这三个是「测量与绘制分离」的高风险点）。

## 4. 相关文件

| 文件 | 说明 |
|---|---|
| `GodotClient/Controls/MirSkin.cs` | 全局 FontScale/ScaledSize + MeasureText |
| `GodotClient/Controls/DXLabel.cs` | 标签基类绘制 |
| `GodotClient/Controls/DXTextArea.cs`、`FilterDropDialog.cs` | 输入控件主题字号 |
| `GodotClient/Controls/MagicBar.cs`、`MagicDialog.cs` | 技能栏/技能信息 |
| `GodotClient/Controls/GroupHealthPanel.cs`、`AutoPathRouteControl.cs` | 组队血条/路点 |
| `GodotClient/Controls/NPCTextControl.cs` | NPC 富文本（含旧 bug 修复） |
| `GodotClient/Scripts/RenderPrimitives.cs` | 世界层名字标签 |

对照旧版：`Client/Envir/CEnvir.cs:946`（FontSize）、`Client/Envir/Config.cs:42`（FontSizeMod）。
