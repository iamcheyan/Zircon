# 设置界面（ConfigDialog）完整审计 — 2026-08-13

> 任务背景：用户反馈设置界面"好多功能点不了、点了没反应"，尤其分辨率、音乐、
> 显示特效、粒子等应"点完即生效"。本文档为逐项代码审计结论 + 修复方案，
> 供后续实施。审计时 HEAD = `1023edd`（含单机模式 `90b9512`）。

## 0. 结论摘要

设置界面（原版 DXConfigWindow 的 Godot 移植）的问题分三类：

| 类别 | 数量 | 用户观感 |
|---|---|---|
| **A. UI 布局缺陷** | 3 | "点不了 / 点了没反应"（下拉被压、菜单看不见） |
| **B. 设置项无实际效果（接线缺失）** | 8 | "点了有勾但游戏里没变化"（音量、粒子、特效等） |
| **C. 已正常工作** | 15+ | 无问题 |

最核心的两个根因：
1. **ConfigSelect 下拉菜单是原地子节点**，被后续兄弟控件（树序更晚 = 画在上层 +
   优先命中）覆盖 → 下拉"打不开/看不见/点选项没反应"。
2. **音量 5 项 + 静音、SoundInBackground、DrawParticles 只有写入方没有消费方**
   —— 设置保存了但游戏根本不读。`SoundCatalog` 已有 `SoundCategory` 分类，
   接线条件齐备，只差音频总线和应用逻辑。

---

## 1. 代码结构

| 文件 | 职责 |
|---|---|
| `GodotClient/Controls/ConfigDialog.cs` | 设置窗口主体，5 页签（图形/声音/游戏/网络/界面），每页 Build*Page() 重建 |
| `GodotClient/Controls/ConfigControls.cs` | ConfigCheckBox（勾选框）/ ConfigSoundBar（音量条）/ ConfigSelect（下拉框）/ ConfigSectionPanel（分区容器） |
| `GodotClient/Controls/DXColourControl.cs` | 颜色块 + DXColourPicker 取色器窗口 |
| `GodotClient/Scripts/ClientSettings.cs` | 全部设置的静态属性 + `user://Zircon.ini` 读写 + ApplyDisplaySettings() |
| `GodotClient/Scripts/GameScene.cs` | PlaySound()/各游戏开关的消费方 |
| `GodotClient/Scripts/SoundPlayback.cs` | 登录/选角场景音效播放（无 GameScene 时） |
| `GodotClient/Scripts/SoundCatalog.cs` | SoundIndex → (文件名, SoundCategory, Loop) 映射表（746 行，机械移植原版 SoundList） |
| `GodotClient/Scripts/PlayerRenderer.cs` / `ObjectRenderer.cs` | 名称/血条/外观特效的渲染消费方 |
| `GodotClient/Scripts/MapWeatherLayer.cs` | 天气粒子层（唯一的粒子系统） |

ini 实测位置：`~/.local/share/godot/app_userdata/ZirconClient/Zircon.ini`

---

## 2. A 类：UI 布局缺陷（"点不了/没反应"的直接原因）

### A1. ConfigSelect 下拉菜单被后续控件遮挡 + 抢点击 ⭐ 最严重

**代码**：`ConfigControls.cs:172-183`

```csharp
_menu = new DXControl
{
    Location = new Vector2I(0, 18),   // 原地子节点，向下展开
    ...
};
AddControl(_menu);                     // 挂在 ConfigSelect 自己名下
```

**根因**：Godot 的 CanvasItem 按**树序**绘制和命中——同层中后添加的兄弟画在
上层、优先接收鼠标。下拉菜单位于第 N 行 ConfigSelect 的子树内，而第 N+1 行
及之后的行、后面的分区（ConfigSectionPanel）都是**更晚添加的兄弟**，全部压在
菜单上面。结果：

- 点下拉按钮后菜单其实打开了（`_menu.Visible` 翻转，`ConfigControls.cs:169`），
  但被下面几行的控件**完全盖住** → 用户观感"点了没反应"；
- 菜单区域点击全部落在覆盖它的行控件上 → 可能误触发下面的勾选框。

**受影响**：图形页 4 个下拉（渲染管线/游戏分辨率/默认显示器/语言）全部。

**佐证**：用户 ini 里 `DrawParticles=true`（勾选过）、音量全是 25（拖过或默认），
说明勾选框本身工作正常，问题集中在下拉和"生效"链路。

### A2. ConfigSelect 无"点击外部关闭"

`ConfigControls.cs:169`：只有再点一次按钮才收起。点别处菜单保持打开（虽然
被盖住看不见），状态错乱。原版 DXComboBox 有全屏透明捕获层模式。

### A3. 图形页垂直溢出 18px，特效区底部被裁

**代码**：`ConfigDialog.cs:40`（`_page` 高 340，`Clip=true`）+ `:195-200`

分区高度公式：`ConfigSectionPanel = 30 + ceil(rows/columns) * 20`（ConfigControls.cs:224）

| 分区 | rows/cols | 高 | y 起点 | 底边 |
|---|---|---|---|---|
| 显示 | 7/1 | 170 | 0 | 170 |
| 可用性 | 4/1 | 110 | 174 | 284 |
| 特效 | 4/2 | 70 | 288 | **358 > 340** |

特效区第二行（"显示天气与特效"、"隐藏头盔"）位于全局 y 335-355，底部 15px
被 `_page` 的 `Clip` 裁掉 → 视觉残缺。（注：Godot 的 ClipContents 只裁绘制不裁
命中，所以这两个还能点，但看起来是坏的。）

---

## 3. B 类：设置项无实际效果（接线缺失）

### B1. 音量 5 项 + 5 个静音开关 ⭐ 用户明确点名

**写入方**：`ConfigDialog.cs:209-213`（SoundBar 拖动/静音 → ClientSettings 字段
`ClientSettings.cs:49-58` → ini）。

**消费方：无。** `GameScene.PlaySound()`（GameScene.cs:3514-3564）和
`SoundPlayback.Play()`（SoundPlayback.cs:33）创建的 AudioStreamPlayer 全部
`Bus = "Master"`，没有分音量、没有静音。

**接线条件已齐备**：`SoundCatalog.cs:10` 已有
`enum SoundCategory { Music, Player, System, Magic, Monster }`，每个音效条目
都带分类（如 B000.wav=Music、脚步=Player、按钮=System、魔法=Magic、怪物=Monster），
与设置页 5 个音量一一对应。缺的只是：

1. 项目无 `default_bus_layout.tres`（glob 确认 GodotClient/ 下无 bus layout 文件）
   → 需建 Master 下 5 条子总线；
2. 播放时按 `entry.Category` 选总线；
3. 设置页改动时应用音量（`linear_to_db(v/100)`）与静音到总线。

### B2. SoundInBackground（后台播放声音）

`ClientSettings.cs:36`，仅 ConfigDialog.cs:206 写入，无消费方。需窗口失焦检测
（`NOTIFICATION_APPLICATION_FOCUS_OUT/IN`）+ 总线临时静音。

### B3. DrawParticles（显示粒子）

`ClientSettings.cs:13`，仅写入无消费。**现状**：客户端唯一的粒子系统是
`MapWeatherLayer`（天气），而天气由独立的 DrawWeather 开关控制且**已接线**
（GameScene.cs:334 `SetDrawWeather` → `_weatherLayer.SetEnabled`）。
即"显示粒子"目前没有可作用的对象——魔法弹道/特效节点（MirProjectileNode /
MirEffectNode / MirLineEffectNode / MirRopeEffectNode）不属于粒子系统。

另注意：`DrawParticles` 默认值是 `false`（ClientSettings.cs:13），原版语义默认开。
用户 ini 已是 true（手动勾过）。

### B4. DrawEffects（显示特效）覆盖不全

唯一消费点 `PlayerRenderer.cs:972`（外观特效：盔甲/徽章光效）。
魔法特效主体 `MirEffectNode / MirProjectileNode / MirLineEffectNode /
MirRopeEffectNode` 的 `_Draw()` 均不检查任何开关 → 关掉"显示特效"后魔法
弹道/爆炸依旧。GameScene 的 Spawn* 入口也无判断。

### B5. 隐藏头盔初始状态硬编码

`ConfigDialog.cs:199`：`Check("隐藏头盔", false, ...)` 恒为 false。服务端回包
路径存在且工作（GameScene.cs:2485-2489 更新 `_player.HideHead`），但重开设置
窗口不反映当前真实状态。真实状态在 `GameScene.Game` 的 `_player.HideHead` /
`StartInfo.HideHead`，需要暴露读取接口。

### B6. 游戏分辨率下拉档位硬编码且匹配不上当前值

`ConfigDialog.cs:120`：固定 6 档（1024x768 … 1920x1080）。窗口模式默认按主屏
75% 计算（`ApplyDisplaySettings`，ClientSettings.cs:296-298），例如 2880x1800
屏 → 2160x1350，**不在列表里** → `SelectItem` 匹配失败，下拉显示第 0 项
"1024 x 768"，与实际窗口不符。`ApplyDisplaySettings` 本身（WindowSetSize 等，
ClientSettings.cs:280-321）实现正确——选列表内档位是生效的，问题只在档位表
与显示不真实。

### B7. 渲染管线（RenderingPipeline）装饰性

`ClientSettings.cs:43` 默认 "Forward Plus"，只存不用（原版是 DX9 管线选项，
Godot 无对应概念）。`DXImageControl.cs:74` 的注释提到
RenderingPipelineManager 是原版概念说明，非本设置的消费方。

### B8. LogChat（记录聊天）未接线

`ConfigDialog.cs:266` 写入 `ClientSettings.LogChat`，ChatLogPanel 无读取，
聊天落盘功能不存在。

### B9. 语言切换不刷新已打开的页

`ConfigDialog.cs:186-192`：切换语言 → `Lang.Reload()` + 通知服务端，但当前
已构建的页（分区标题"显示/可用性"、选项文本如"显示粒子"等**硬编码中文**或
建页时取 Lang 快照）不会刷新。需重建当前页（调 SelectTab 当前索引）。另：
ConfigDialog 里大量选项文本是硬编码中文字符串而非 Lang 属性（对比：
SmoothMove 用了 Lang，"全屏显示"/"显示粒子"等没用），多语言下会混排。

---

## 4. C 类：已核实正常工作的设置（有消费方，代码级确认）

| 设置 | 消费点 |
|---|---|
| 全屏 / 无边框 / 垂直同步 / 限制帧率(60fps) / 限制鼠标 / 默认显示器 | `ClientSettings.ApplyDisplaySettings()` ClientSettings.cs:280-321（WindowSetMode/WindowSetVsyncMode/Engine.MaxFps/Input.MouseMode/WindowSetCurrentScreen） |
| 游戏分辨率（列表内档位） | 同上 WindowSetSize |
| 平滑移动 | PlayerRenderer.cs:745（插值开关） |
| 调试标签 | GameScene 每帧同步可见性（ConfigDialog.cs:170-175 注释） |
| 显示物品/怪物名称 | ObjectRenderer.cs:572-573 |
| 显示人物名称 | PlayerRenderer.cs:873-877 |
| 显示生命条 | PlayerRenderer.cs:881 |
| 显示伤害数字 | GameScene.cs:4061 / 4076 / 4089（SpawnDamagePopup 门控） |
| 显示天气与特效 | GameScene.cs:334 SetDrawWeather → MapWeatherLayer.SetEnabled |
| 右键取消目标 / Esc 关闭所有 / 隐藏聊天栏 / 可观察 | GameScene 对应 Set* 方法 + 网络包 |
| 聊天颜色 13 组前景/背景 | ChatLogPanel.cs:117 经 ChatForeColour/ChatBackColour（ClientSettings.cs:101-133）；取色器 DXColourPicker 正常 |
| 目标颜色 7 项 | ObjectRenderer 轮廓绘制（ObjectRenderer.cs:379-383） |
| 服务器地址/端口 | LoginScene.cs:80-82（需勾选"使用网络配置"才生效，逻辑与原版一致） |
| 快捷键设置按钮 | KeyBindDialog（已实现） |
| Shift 打开聊天 | ChatTextBox（迁移记录确认） |

**排除项**（审计中怀疑过、核实后无问题的）：
- `Save()` 开头调 `Load()`（ClientSettings.cs:222-224）：Load 有 `_loaded`
  一次性守卫（cs:137），不会用 ini 旧值回滚刚修改的设置。**非 bug**。
- 音效资源加载：PlaySound 按文件存在性加载 WAV，日志无缺失报错。

---

## 5. 修复方案（按优先级）

### P0-1 ConfigSelect 下拉顶层化 + 点击外部关闭
- 打开时将 `_menu` 从 ConfigSelect reparent 到**设置窗口根**（ConfigDialog，
  即 `_page` 之外——避开 `_page` 的 Clip 和兄弟树序），坐标用
  `GlobalPosition` 换算；关闭时移回（或不移回，Free 重建）。
- 简化替代：菜单显示时在 ConfigDialog 放一个全窗口透明捕获层（原版
  DXComboBox 模式），点它 = 关闭菜单且不透传。
- 顺带解决 A2（点击外部关闭）。

### P0-2 图形页消除溢出
- 方案 a（推荐）：特效区 4 项并入"可用性"分区（4+4=8 行 → 高 190，
  总高 170+4+190=364 仍超 340 → 需同时压缩"显示"分区或）；
- 方案 b：`_page` 加 DXVScrollBar（项目里已有 DXVScrollBar 组件，
  GuildDialog/KeyBindDialog 均有现成用法）；
- 方案 c：窗口加高（原版 DXConfigWindow 图形页本身就是滚动的，方案 b 最贴原版）。

### P0-3 音频总线接线（B1/B2 一并解决）
1. 新建 `GodotClient/default_bus_layout.tres`：Master → System/Music/Player/
   Monster/Magic 五条子总线；
2. `GameScene.PlaySound` / `SoundPlayback.Play` 按 `entry.Category` 设置
   player.Bus；
3. 新增 `ClientSettings.ApplyAudioSettings()`：五类音量
   `volume_db = linear_to_db(value/100)`（0 → -80dB 或 mute），静音位直通
   `AudioServer.SetBusMute`；ConfigDialog 的 SoundBar 回调与启动时调用；
4. SoundInBackground：GameScene 监听
   `NOTIFICATION_APPLICATION_FOCUS_OUT/IN`，失焦且 !SoundInBackground 时
   Master 总线临时静音。

### P1-4 DrawEffects / DrawParticles 接线（B3/B4）
- `MirEffectNode/MirProjectileNode/MirLineEffectNode/MirRopeEffectNode._Draw()`
  开头加 `if (!ClientSettings.DrawEffects) return;`（或 GameScene Spawn* 入口
  统一判断——节点内部判断可覆盖存量节点，推荐节点内）；
- DrawParticles 现阶段无独立对象：与 DrawEffects 合并语义（都门控特效节点），
  或暂时隐藏该选项并在本文档记录；默认值改 true（ClientSettings.cs:13）。

### P1-5 隐藏头盔初始状态（B5）
- GameScene 暴露 `public bool HideHead => _player?.HideHead ?? false`（或读
  StartInfo.HideHead），ConfigDialog 建页时取真实值。

### P1-6 分辨率下拉动态档位（B6）
- 列表 = 当前窗口尺寸 + 常用档位去重排序；当前值不匹配任何档时补
  "当前 (WxH)" 项，保证显示真实。

### P2-7 语言切换刷新 + 文本走 Lang（B9）
- language.SelectedChanged 里调 `SelectTab(当前)` 重建页面；
- ConfigDialog 硬编码中文选项迁移到 Lang 属性（翻译键已大量存在，如
  CommonControlConfigWindowGraphicsTabDrawParticlesLabel 等）。

### P2-8 LogChat（B8）
- ChatLogPanel 收到消息时若 LogChat 则追加写 `user://Chat Logs.txt`
  （原版行为），启动时可选加载。

---

## 6. 验证清单（修复后）

1. `dotnet build GodotClient/ZirconClient.csproj` 零错误；
2. 单机模式进游戏（不带 --server）；
3. 打开设置：图形页 4 个下拉逐个点开——菜单完整可见、选项可点、点击外部关闭；
4. 分辨率切 1600x900 → 窗口立即变；切回原尺寸；
5. 音量：拖系统音量到 0 → 按钮音消失；拖回 → 恢复；音乐音量影响 BGM；
   静音图标点击生效；
6. 显示特效关 → 施法无弹道/爆炸（外观光效同关）；显示天气关 → 雨/雪停；
7. 隐藏头盔勾选 → 头盔消失，重开设置窗口勾选状态保持；
8. `ZIRCON_UI_AUDIT=1` 启动确认无越界控件；
9. 游戏内截图三页留档（docs/bugs/ 或本目录）。

## 7. 关联

- 单机模式文档：`docs/SINGLE_PLAYER_MODE_2026-08-13.md`
- UI 布局审计工具（ZIRCON_UI_AUDIT）：commit `a3e4418`
- 原版 DXConfigWindow 参照：`Client/Controls/DXConfigWindow.cs`（不在本仓库，
  见原版客户端源码）
