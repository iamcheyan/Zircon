# Zircon 设置界面完整审计 — 2026-08-20

> 基于 ConfigDialog 全部 5 个标签页、56 项设置的逐项代码审计。
> 每项追踪了完整链路：UI 控件 → ClientSettings 字段 → 消费方代码。

## 总览

| 状态 | 数量 | 说明 |
|---|---|---|
| ✅ 正常工作 | 49 | 有写入方 + 有消费方，功能完整 |
| ❌ 有 Bug | 1 | SoundInBackground 焦点恢复错误 |
| ⚠️ 接线缺失 | 4 | DrawEffects(部分)、DrawParticles、ShiftOpenChat、LogChat |
| 🔶 装饰性 | 1 | RenderingPipeline |
| ✅ 已修复 | — | 2026-08-13 审计中的 P0 问题大部分已解决 |

---

## ❌ 有 Bug 的设置

### 1. SoundInBackground（后台播放声音）— P1

**现象**：关闭"后台播放声音"后，窗口失焦 → 游戏静音；窗口恢复焦点 → **永久静音**。

**根因**：`GameScene.cs:8093-8099` 焦点丢失时执行 `AudioServer.SetBusMute(0, true)`（静音 Master 总线）。但焦点恢复时调用 `ApplyAudioSettings()`，该方法只操作 5 条子总线（System/Music/Player/Monster/Magic），**从不触碰 Master 总线** → Master 永远处于 mute 状态。

**修复**：
- **文件**：`GodotClient/Scripts/GameScene.cs`（焦点恢复通知处）
- **改法**：焦点恢复时，先 `AudioServer.SetBusMute(0, false)` 取消 Master 静音，再调 `ApplyAudioSettings()`。

```csharp
// 焦点恢复 (NOTIFICATION_APPLICATION_FOCUS_IN 或等效)
AudioServer.SetBusMute(0, false);  // ← 加这一行
ClientSettings.ApplyAudioSettings();
```

---

## ⚠️ 接线缺失的设置

### 2. DrawEffects（显示特效）— 魔法特效部分不生效 — P1

**现象**：取消勾选"显示特效"后，盔甲/徽章光效消失（正常），但**魔法弹道、爆炸、闪电、锁链特效仍然显示**。

**根因**：4 个魔法特效节点用了 AND 逻辑门：
```csharp
// MirEffectNode.cs:237, MirProjectileNode.cs:184, MirLineEffectNode.cs:85, MirRopeEffectNode.cs:70
if (!ClientSettings.DrawEffects && !ClientSettings.DrawParticles) return;
```
这意味着**两个开关都关闭**才会隐藏特效。只关 DrawEffects 时 DrawParticles 仍为 true → 条件不满足 → 特效继续渲染。

PlayerRenderer.cs:972 的盔甲/徽章光效单独检查 `DrawEffects`，所以那部分是正常的。

**修复**：
- **文件**：`MirEffectNode.cs`、`MirProjectileNode.cs`、`MirLineEffectNode.cs`、`MirRopeEffectNode.cs`
- **改法**：将 AND 门改为 OR 门——任一关闭即隐藏：

```csharp
// 改前
if (!ClientSettings.DrawEffects && !ClientSettings.DrawParticles) return;
// 改后
if (!ClientSettings.DrawEffects || !ClientSettings.DrawParticles) return;
```

### 3. DrawParticles（显示粒子）— 无独立消费方 — P1

**现象**：取消勾选"显示粒子"后，游戏内**无任何可见变化**。

**根因**：
- `MapWeatherLayer`（唯一的粒子系统）只检查 `DrawWeather`，不检查 `DrawParticles`
- Mir*Node 用 AND 门，关 DrawParticles 但开 DrawEffects 时条件不满足 → 无变化

**修复**（依赖上面的 DrawEffects 修复）：
- 上述 4 个 Mir*Node 改成 OR 门后，关 DrawParticles 即可隐藏魔法特效
- **可选**：让 `MapWeatherLayer` 也检查 `DrawParticles`，使"粒子"同时控制天气粒子

```csharp
// MapWeatherLayer.cs — SetEnabled 或 _Draw 中加入
if (!ClientSettings.DrawParticles) return;
```

### 4. ShiftOpenChat（按 Shift 打开聊天）— 无消费方 — P1

**现象**：勾选/取消勾选"按 Shift 打开聊天"后，聊天打开行为**无变化**。

**根因**：
- `ChatTextBox.cs` 打开聊天通过 Space/Enter/`/`/`@`/`!`，从不引用 `ShiftOpenChat`
- `KeyBindManager.cs` 的 Shift+1 固定绑定到 UseBelt01，不检查此设置

**修复**（两种方案）：
- **方案 A（推荐）**：在 `ChatTextBox.cs` 的输入处理中，当 `ShiftOpenChat=true` 时，Shift+Space/Shift+Enter 打开聊天而非走默认快捷键
- **方案 B**：如果原版行为是"Shift+数字键打开聊天而非使用腰带"，则在 `KeyBindManager` 的 Shift 修饰键处理中加入 `ShiftOpenChat` 检查

### 5. LogChat（记录聊天）— 无消费方 — P1

**现象**：勾选"记录聊天"后，聊天**不会写入任何文件**。

**根因**：`ChatLogPanel.cs:AddMessage` 从不检查 `LogChat`，也不写文件。

**修复**：
- **文件**：`GodotClient/Controls/ChatLogPanel.cs`
- **改法**：在 `AddMessage` 末尾，如果 `LogChat=true`，追加写入 `user://Chat Logs.txt`

```csharp
// ChatLogPanel.cs — AddMessage 方法末尾
if (ClientSettings.LogChat)
{
    using var f = FileAccess.Open("user://Chat Logs.txt", FileAccess.ModeFlags.ReadWriteAppend);
    if (f != null)
        f.StoreLine($"[{DateTime.Now:HH:mm:ss}] {message}");
}
```

---

## 🔶 装饰性设置

### 6. RenderingPipeline（渲染管线）— P2

**现象**：下拉可选 Forward Plus 等，但切换后**无实际效果**。

**原因**：原版是 DX9 管线选项，Godot 4 无运行时管线切换 API。字段只存储不消费。

**建议**：保留下拉但标注"(仅显示)"或移除该选项。

---

## ✅ 正常工作的设置（49 项）

### 图形页（15/17 正常）

| 设置 | 消费方 | 状态 |
|---|---|---|
| 全屏显示 FullScreen | `ApplyDisplaySettings` → `WindowSetMode(Fullscreen)` | ✅ |
| 无边框窗口 Borderless | `ApplyDisplaySettings` → `WindowSetFlag(Borderless)` | ✅ |
| 游戏分辨率 GameSize | `ApplyDisplaySettings` → `WindowSetSize` | ✅ |
| 默认显示器 DefaultMonitor | `ApplyDisplaySettings` → `WindowSetCurrentScreen` | ✅ |
| 垂直同步 VSync | `ApplyDisplaySettings` → `WindowSetVsyncMode` | ✅ |
| 限制帧率 LimitFPS | `ApplyDisplaySettings` → `Engine.MaxFps=60` | ✅ |
| 限制鼠标 ClipMouse | `ApplyDisplaySettings` → `Input.MouseMode=Confined` | ✅ |
| 平滑移动 SmoothMove | `PlayerRenderer.cs:745` 插值开关 | ✅ |
| 调试标签 DebugLabel | `GameScene.cs:8255-8266` 每帧同步可见性 | ✅ |
| 语言 Language | `Lang.Reload()` + `SelectTab` 重建页 + `SendSelectLanguage` | ✅ |
| 显示天气与特效 DrawWeather | `GameScene.cs:346` → `MapWeatherLayer.SetEnabled` | ✅ |
| 隐藏头盔 HideHead | `PlayerRenderer.cs:942` + 服务端状态同步 | ✅ |
| 渲染管线 RenderingPipeline | — | 🔶 装饰性 |
| 显示粒子 DrawParticles | — | ⚠️ 见 #3 |
| 显示特效 DrawEffects | `PlayerRenderer.cs:972`（部分）| ⚠️ 见 #2 |

### 声音页（6/6 正常）

| 设置 | 消费方 | 状态 |
|---|---|---|
| 5 类音量 × 5 | `ApplyAudioSettings` → `AudioServer.SetBusVolumeDb` | ✅ |
| 5 类静音 × 5 | `ApplyAudioSettings` → `AudioServer.SetBusMute` | ✅ |
| 后台播放声音 SoundInBackground | `GameScene.cs:8093-8099` | ❌ 见 #1 |

音频总线结构确认：`default_bus_layout.tres` = Master → {System, Music, Player, Monster, Magic}，`PlaySound` 按 `SoundCategory` 路由到对应总线。

### 游戏页（8/8 正常）

| 设置 | 消费方 | 状态 |
|---|---|---|
| 显示物品名称 ShowItemNames | `ObjectRenderer.cs:572` | ✅ |
| 显示怪物名称 ShowMonsterNames | `ObjectRenderer.cs:573` | ✅ |
| 显示人物名称 ShowPlayerNames | `PlayerRenderer.cs:873-877` | ✅ |
| 显示生命条 ShowUserHealth | `PlayerRenderer.cs:881` | ✅ |
| 显示伤害数字 ShowDamageNumbers | `GameScene.cs:4084/4099/4112` | ✅ |
| 右键取消目标 RightClickDeTarget | `GameScene.cs:1032` | ✅ |
| 可观察 AllowObservable | `GameScene.cs:446` → `SendObservable` | ✅ |
| 7 项目标颜色 | `GameScene.cs:8340-8352` → `ObjectRenderer.cs:379-383` | ✅ |

### 网络页（3/3 正常）

| 设置 | 消费方 | 状态 |
|---|---|---|
| 使用网络配置 UseNetworkConfig | `LoginScene.cs:80-82` | ✅ |
| 服务器地址 IPAddress | `LoginScene.cs:80-82` | ✅ |
| 服务器端口 Port | `LoginScene.cs:80-82` | ✅ |

### 界面页（17/19 正常）

| 设置 | 消费方 | 状态 |
|---|---|---|
| 隐藏聊天栏 HideChatBar | `GameScene.cs:354` | ✅ |
| Esc 关闭所有窗口 EscapeCloseAll | `GameScene.cs:9908` | ✅ |
| 快捷键设置按钮 | `KeyBindDialog`（已实现） | ✅ |
| 13 组聊天颜色（前景+背景） | `ChatLogPanel.cs:117` → `ChatForeColour/ChatBackColour` | ✅ |
| 按 Shift 打开聊天 ShiftOpenChat | — | ⚠️ 见 #4 |
| 记录聊天 LogChat | — | ⚠️ 见 #5 |

---

## 修复优先级

| 优先级 | 设置 | 工作量 | 影响 |
|---|---|---|---|
| P1 | SoundInBackground 焦点恢复 | 1 行 | 失焦后永久静音 |
| P1 | DrawEffects AND→OR 门 | 4 文件各 1 行 | 魔法特效不受开关控制 |
| P1 | DrawParticles 独立消费 | 1-5 行 | 粒子开关无效 |
| P1 | ShiftOpenChat 消费方 | ~10 行 | 设置项无效果 |
| P1 | LogChat 文件写入 | ~5 行 | 设置项无效果 |
| P2 | RenderingPipeline 标注 | 1 行 | 装饰性，用户困惑 |

---

## 关于分辨率下拉

分辨率功能**已正确接线**：
- 下拉列表注入了当前窗口尺寸（`DisplayServer.WindowGetSize()`），保证当前值总能匹配
- 选择后调用 `ApplyDisplaySettings()` → `DisplayServer.WindowSetSize(GameSize)`
- `--window` 启动参数会覆盖 ini 设置，强制 75% 屏幕尺寸

**注意**：macOS 上 `DisplayServer.ScreenGetSize()` 返回的是物理像素（如 2704×1756），不是逻辑点。分辨率下拉显示的"显示器 1 (2704 x 1756)"是物理分辨率，不是 Godot 视口分辨率。这可能导致混淆但功能本身正确。

---

## 2026-08-20 执行结果

已按本审计执行代码修改：

| 项目 | 修改状态 | 实现 |
|---|---|---|
| SoundInBackground | ✅ 已修复 | 焦点恢复先解除 Master mute，再应用分类音量 |
| DrawEffects | ✅ 已修复 | MirEffect/Projectile/Line/Rope 从 AND 门改为 OR 门 |
| DrawParticles | ✅ 已修复 | 魔法特效节点和 MapWeatherLayer 均受开关控制 |
| ShiftOpenChat | ✅ 已修复 | `ShiftOpenChat=true` 时 Shift+数字键打开聊天；关闭时保留腰带快捷键 |
| LogChat | ✅ 已修复 | 追加写入 `user://Chat Logs.txt` |
| RenderingPipeline | ✅ 已处理 | 改为不可用的固定 Forward Plus 选项，避免误导 |
| 分辨率显示 | ✅ 已处理 | 下拉当前值使用实际 `WindowGetSize()`；显示器项明确标注物理分辨率 |


### 分辨率启动参数修正

补充修复：`ClientSettings.ApplyDisplaySettings()` 不再在每次调用时重写
`GameSize`。`--window` 现在只在启动阶段强制窗口模式并确定初始尺寸；
进入设置后选择分辨率会保留用户选择，不会被后续 `ApplyDisplaySettings()`
重新覆盖。分辨率下拉显示 `DisplayServer.WindowGetSize()` 的当前窗口尺寸，
显示器下拉明确标注 `物理` 分辨率。
构建验证：`dotnet build GodotClient/ZirconClient.csproj -c Debug`，0 错误，3 个已有 warning。