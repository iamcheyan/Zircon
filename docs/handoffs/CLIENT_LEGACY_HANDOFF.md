# CLIENT_LEGACY_HANDOFF — Zircon 原版 C# WinForms 客户端接手文档

> 适用范围：仓库 `/home/tetsuya/development/zircon`（分支 master）内的**原版客户端遗产**
> （`Client/` + `LibraryCore/` 客户端侧 + `RenderingCore/` + `Launcher/Patcher/PatchManager/`
> 更新链 + `ImageManager/LibraryEditor/Plugin*/BotRunner` 周边工程）。
> 读者对象：任何需要**读懂、对照、移植**原版客户端行为到新版（GodotClient）的开发者。
>
> 编写日期：2026-08-17。所有行号均为当日实测核对（核验记录见文末自检节）。
>
> **任务口径差异声明（重要）**：任务目标描述中的「DXManager」与当前代码不符。
> 现仓库已无 `Client/` 内的 DXManager 类——渲染由
> `RenderingCore/Rendering/RenderingPipelineManager.cs`（静态类，:11）统一管理，
> 管线 ID 见 `RenderingCore/Rendering/RenderingPipelineIds.cs`（SilkDXD3D11 默认 /
> SilkOpenGL / SilkVulkan）；`RenderingCore/Rendering/SharpDXD3D9|SharpDXD3D11/`
> 目录存在但被 `RenderingCore/RenderingCore.csproj:17-18` 的 `<Compile Remove>` 排除编译
> （**遗留代码，不可用**）。本仓库内**唯一**名为 DXManager 的类位于服务端工程
> `Server/Views/MapViewer.cs:370`（GDI+/System.Drawing 地图查看器，服务端工具，与客户端渲染无关）。
> 本文档一律以当前代码为准。

---

## 目录

1. [工程总览与规模实测](#1-工程总览与规模实测)
2. [模块结构图（ASCII）与进程拓扑](#2-模块结构图ascii与进程拓扑)
3. [启动流程：入口 → 初始化 → 登录 → 进游戏](#3-启动流程入口--初始化--登录--进游戏)
4. [渲染管线：RenderingPipelineManager 体系](#4-渲染管线renderingpipelinemanager-体系)
5. [每帧逻辑：GameLoop → 场景 → 控件树](#5-每帧逻辑gameloop--场景--控件树)
6. [网络层与协议](#6-网络层与协议)
7. [关键文件地图（Client/ 逐目录）](#7-关键文件地图client-逐目录)
8. [资源格式：.map / .Zl / System.db / 翻译](#8-资源格式map--zl--systemdb--翻译)
9. [周边工程（更新链 / 工具 / BotRunner）](#9-周边工程更新链--工具--botrunner)
10. [Godot 对照与移植要点](#10-godot-对照与移植要点)
11. [已知坑与「别做什么」](#11-已知坑与别做什么)
12. [延伸资料](#12-延伸资料)

---

## 1. 工程总览与规模实测

本仓库 `git` 根目录工程清单（`ls` 实测，2026-08-17）：

```
AGENTS.md  BotRunner/  Client/  ClientData/  Components/  Config/  Debug/
Docs/  GodotClient/  ImageManager/  Launcher/  LibraryCore/  LibraryEditor/
PatchManager/  Patcher/  PluginCore/  PluginStandalone/  RenderingCore/  Server/
ServerCore/  ServerLibrary/  System.db  Tools/  "Zircon Server.sln"
```

其中 **Client/ 及周边工程（WinForms/控制台/工具，非服务端、非 Godot）规模实测**
（`wc -l` 逐文件合计）：

| 工程 | 目录 | .cs 文件数 | 总行数 | 说明 |
|---|---|---|---|---|
| 客户端主程序 | `Client/` | 121 | 104,344 | WinForms 原版客户端（可执行产物 Zircon.exe） |
| └ 场景 UI | `Client/Scenes/Views/` | 44 | ≈42,000 | 全部对话框（详见 §7） |
| 共享库 | `LibraryCore/` | 65 | 23,269 | 协议/DB/模型/枚举（客户端+服务端共享） |
| └ DB 模型 | `LibraryCore/SystemModels/` | 39 | 13,611 | DBObject 派生模型 |
| 渲染库 | `RenderingCore/` | 39 | 15,585 | 多管线渲染层 + .Zl 图库加载 |
| 启动器 | `Launcher/` | 9 | 883 | DevExpress WinForms，HTTP 更新 |
| 补丁下载器 | `Patcher/` | 6 | 310 | DevExpress WaitForm，原子替换 |
| 补丁打包 | `PatchManager/` | 8 | 977 | WinSCP 上传补丁 |
| 图片工具 | `ImageManager/` | 8 | 1,282 | 图库/图片转换 |
| 编辑器 | `LibraryEditor/` | 26 | 10,734 | 图库/WIL 编辑器 |
| 插件框架 | `PluginCore/` | 14 | 484 | 服务端插件接口 |
| 插件独立程序 | `PluginStandalone/` | 4 | 184 | 插件宿主 |
| 机器人 | `BotRunner/` | 13 | 4,630 | 协议级测试机器人 |
| 二进制组件 | `Components/` | **0** | — | SlimDX / ManagedSquish / NativeSquish_x64 等 DLL |

**总量**：约 359 个 .cs、约 218,000 行（不含服务端 Server*/ 与 GodotClient/）。

关键常识：

- 方案 `Zircon Server.sln` 位于仓库根；客户端是 `Client/` 工程（`Client/Client.csproj`）。
- 客户端产物输出到 `Debug/Client/`（`Client/Client.csproj` OutputPath `..\..\Debug\Client\`），
  该目录含运行时数据 `Data/`（.Zl 图库 + System.db + Map/）、`Sound/`、`Database/` 等
  （`Debug/Client/Sound/` 实测约 3172 个 .wav/.ogg）。
- 运行时数据资产在 `Debug/Client/Data/`（**不在本仓库根**——仓库根只有 `ClientData/`、
  `Config/`、`Debug/`）。

## 2. 模块结构图（ASCII）与进程拓扑

```
                        ┌────────────────────────────────────────────┐
                        │                Zircon.exe                   │
                        │                (Client/)                    │
   ┌────────────────────┼──────────────────────┐                     │
   │  Program.cs 入口    │                      │                     │
   │  (CEnvir.Init)      │                      │                     │
   └─────────┬──────────┘                      │                     │
             │                                  │                     │
   ┌─────────▼────────────────────────────┐    │                     │
   │  Client/Envir/  CEnvir (static)      │    │                     │
   │   Config.cs (Zircon.ini)             │    │                     │
   │   CConnection : BaseConnection       │    │                     │
   │   DXSoundManager / DXSound           │    │                     │
   │   Translations/ (757 键三件套)        │    │                     │
   └───────┬───────────────────┬──────────┘    │                     │
           │                   │               │                     │
   ┌───────▼──────────┐ ┌──────▼──────────────┐│                     │
   │  Scenes/          │ │  Controls/          ││                     │
   │  LoginScene       │ │  DXControl (根控件)  ││                     │
   │  SelectScene      │ │  DXScene            ││                     │
   │  GameScene (5051) │ │  DXWindow/ DXLabel/ ││                     │
   │  Views/ 44 文件   │ │  DXButton/DXItemGrid││                     │
   └───────┬──────────┘ └──────┬──────────────┘│                     │
           │                   │               │                     │
   ┌───────▼───────────────────▼───────────────▼─────────────────────┐
   │  RenderingCore/（库，Client+Server 共享）                        │
   │   Rendering/RenderingPipelineManager.cs   ← 多管线管理器（默认   │
   │   Rendering/SilkD3D11|SilkVulkan/         ← 可用管线实现         │
   │   Rendering/SharpDXD3D9|SharpDXD3D11/     ← csproj 排除(遗留)    │
   │   Library/MirLibrary.cs (1885 行)         ← .Zl 图库加载器       │
   │   LibraryFormat/ZlFormat.cs               ← ZL2 容器/编解码      │
   └───────┬───────────────────────┬─────────────────────────────────┘
           │                       │
   ┌───────▼───────────────┐ ┌─────▼──────────────────────────────────┐
   │  LibraryCore/（库）    │ │  TCP 7000（Login/Select/Game 全程）     │
   │  Network/Packet.cs     │ │                                        │
   │  Network/ClientPackets │ │  ┌──────────────────────────┐          │
   │  153 包 / ServerPackets│─┼─►│  ServerCore + ServerLibrary│          │
   │  216 包 / General 7    │ │  │  （游戏服务器，另文档）    │          │
   │  MirDB/ Session        │ │  └──────────────────────────┘          │
   │  SystemModels/ 39      │ │                                        │
   │  Enum.cs / Stat.cs     │ │                                        │
   └────────────────────────┘ └────────────────────────────────────────┘

  更新链（Launcher.exe → Zircon.exe）：
  PatchManager(PC, WinSCP/FTP) ──补丁包──► 补丁服务器(mirfiles.com)
        ▲                                        │
        │                                        ▼ HTTP
  ┌──────┴────────┐  自更新(写内嵌 Patcher.exe)  ┌──────────────┐
  │  Launcher.exe │ ────────────────► Patcher.exe │              │
  │  (LMain.cs)   │ ◄────原子替换+重启──────────  │              │
  └───────────────┘                                └──────┬───────┘
                                                          ▼
                                                  Zircon.exe（Client）
```

**进程拓扑事实**：

- 游戏进程 `Zircon.exe` 只与游戏服务器一条 TCP 连接（默认 `127.0.0.1:7000`，
  `Client/Envir/Config.cs:13-14`）；登录、选人、进游戏全程同一条连接。
- `Launcher.exe`（HTTP 更新）+ `Patcher.exe`（原子替换）是**联网更新链**，日常开发可跳过；
  `Launcher/LMain.cs:22` 的 `ClientFileName` 指向 `Zircon.exe`。
- `BotRunner`（net10.0 console）是**协议级机器人**：直连游戏服务器 7000 端口、
  走真实 `ClientPackets/ServerPackets` 协议；被 GodotClient 单机模式主动拉起
  （§9），也是 82 生产服务器部署组件。
- 插件机制（`PluginCore`/`PluginStandalone`）**仅服务端**使用（`Server/SMain.cs:41-45`）；
  `Client/Client.csproj` 无插件引用、源码零命中——**客户端不加载插件**。
- `Components/` 四 DLL 中 `SlimDX.dll` 全仓库无引用（死资产）；`ManagedSquish` +
  `NativeSquish_x64` 仅被 `ImageManager/`、`LibraryEditor/` 引用（DXT 解码/编码）。

## 3. 启动流程：入口 → 初始化 → 登录 → 进游戏

### 3.1 入口 `Client/Program.cs`

```
Main()
 ├─ ConfigReader.Load()                       // :9-13（Zircon.ini）
 ├─ Init()
 │   ├─ MirLibrary 静态钩子注入                // :44-52（GetNow/GetUseZlAtlasPages/
 │   │                                        //    GetCacheDuration/DrawCounted）
 │   ├─ foreach Libraries.LibraryList          // :53-57
 │   │      + File.Exists 过滤 → new MirLibrary(@".\" + pair.Value)  // 加载全部 .Zl
 │   ├─ CEnvir.Init(args)                     // :58
 │   ├─ TargetForm 实例化                      // :59-66（含渲染管线上下文工厂 :88-95）
 │   ├─ RenderingPipelineManager.InitializeWithFallback(
 │   │      Config.RenderingPipeline, context) // :63 附近（失败 fallback 默认管线）
 │   ├─ DXSoundManager.Create()               // 音频
 │   └─ DXControl.ActiveScene = new LoginScene( // :68
 │         Config.ExtendedLogin ? Config.GameSize : Config.IntroSceneSize);
 └─ RenderingPipelineManager.RunMessageLoop(   // :70
        CEnvir.Target, CEnvir.GameLoop);
```

要点：

- `CEnvir.C`（客户端校验和）在 `Init` 中经 `ConfigReader` 读取，登录时放入
  `C.Login.CheckSum`（`Client/Scenes/LoginScene.cs:926-935`）。
- 图库加载失败（缺 .Zl 文件）不崩溃，跳过即可；运行时引用缺失图库的帧也不崩
  （该区域不渲染，见 §11 坑 6/8）。
- 渲染管线初始化在 **ActiveScene 创建之前**（`Program.cs:63` 先于 `:68`），
  失败自动 fallback（`RenderingPipelineManager.cs:280-295`）。

### 3.2 `Client/Envir/CEnvir.cs`（1102 行，static 类）初始化

| 关注点 | 位置 | 事实 |
|---|---|---|
| `Language` 静态属性 / `LoadLanguage` | :73 / :98-108 | `switch (Config.Language)` 选 EnglishMessages/ChineseMessages 实例（**无 default**，未知语言 → 语言 null → UI 空引用崩溃，见 §11 坑 4） |
| `LibraryList` | :30 附近声明，Program.cs:53-57 填充 | `Dictionary<LibraryFile, MirLibrary>` |
| `LoadDatabase` | :378-417 | `new MirDB.Session(SessionMode.Users, @".\Data\")`（:386）；`Session.Initialize(typeof(ItemInfo), typeof(WindowSetting))`（:388-391）；`GetCollection` 灌入 ItemInfoList/MagicInfoList/MapInfoList/CurrencyInfoList/InstanceInfoList/NPCPageList/MonsterInfoList/FishingInfoList/StoreInfoList/NPCInfoList/MovementInfoList/QuestInfoList/QuestTaskList/CompanionInfoList/CompanionLevelInfoList/DisciplineInfoList 等 21 类列表（:398-417） |
| `Enqueue(Packet)` | :554-557 | `Connection?.Enqueue(packet)`——所有发包的唯一入口 |
| `GameLoop` | :160-348 | 见 §5 |
| `ReturnToLogin` | :363-371 | 重建 LoginScene |

### 3.3 登录握手（与服务器 7000 端口）

时序（`Client/Envir/CConnection.cs` + `Client/Scenes/LoginScene.cs` 实测）：

```
LoginScene.Process()                              // LoginScene.cs:318-338 重连逻辑
 ├─ 未连接且到 ConnectionTime → new TcpClient() → BeginConnect(
 │     Config.UseNetworkConfig ? IPAddress:Port : DefaultIPAddress:DefaultPort)  // :342-345
 └─ Connecting(IAsyncResult)                      // :373-396
     └─ 成功 → CEnvir.Connection = new CConnection(client)   // :393
CConnection 构造（:33-46）→ BeginReceive()        // BaseConnection.cs:69
  → 收到 G.Connected → 回 Enqueue(new G.Connected)          // CConnection.cs:131-135
  → 收到 G.CheckVersion → SHA256(本进程 .dll) 发 G.Version   // :136-143
  → 收到 G.GoodVersion → Encryption.SetKey(p.DatabaseKey)   // :147（AES-256 数据库密钥）
                          + scene.LoadDatabase()            // :152（CEnvir.LoadDatabase）
                          + 发 C.SelectLanguage             // :154
  → 收到 G.Ping → 回 G.Ping；G.PingResponse → Ping = p.Ping // :158-162
玩家点登录 → Login()                              // LoginScene.cs:922-935
  → CEnvir.Enqueue(new C.Login { EMailAddress, Password, CheckSum = CEnvir.C })
  → 服务端回 S.LoginSuccessful → 切 SelectScene    // CConnection.cs:565-573
       DXControl.ActiveScene = new SelectScene(...) { SelectBox = { CharacterList = p.Characters } }
  → 玩家点开始 → S.StartGame(Success)             // CConnection.cs:755-771
       GameScene scene = new GameScene(Config.GameSize);
       DXControl.ActiveScene = scene;
       scene.MapControl.MapInfo = Globals.MapInfoList…(p.StartInformation.MapIndex);
       scene.MapControl.InstanceInfo = …InstanceIndex;
       scene.User = new UserObject(p.StartInformation);       // :771
```

**关键事实**：

- 网络加密：**无**（协议明文 TCP）。`LibraryCore/Encryption.cs`（94 行）是
  **本地 System.db 的 AES-256 加解密**（密钥由服务端 GoodVersion 下发，
  `CConnection.cs:147`），不是网络流量加密。
- `G.GoodVersion` 同时下发 `SystemDatabaseVersion`——客户端据此比对本地
  `Data/System.db` 版本（`LoginScene.cs` `CompareSystemDatabaseVersions`），
  不一致在登录页黄色/红色提示。
- 断线：`CConnection.Disconnect()`（:51-70）把 `CEnvir.Connection` 置空；
  LoginScene 场景内调用 `scene.Disconnected()`；游戏内弹 `DXMessageBox` 后
  `ReturnToLogin`（`CEnvir.cs:363-371` 重建 LoginScene）。

### 3.4 三场景切换总表

| 从 → 到 | 触发 | 代码 |
|---|---|---|
| (程序) → LoginScene | 启动 | `Program.cs:68` |
| LoginScene → SelectScene | `S.LoginSuccessful` | `CConnection.cs:565-573` |
| SelectScene → GameScene | `S.StartGame`(Success) | `CConnection.cs:755-771` |
| GameScene → LoginScene | 断线/退出登录 | `CEnvir.cs:363-371`（`ReturnToLogin`） |
| 任意 → LoginScene | 服务端 G.Disconnect | `CConnection.cs:78-125`（按 DisconnectReason 弹框） |

场景基类 `DXScene`（`Client/Controls/DXScene.cs`，抽象）：持有 DebugLabel/HintLabel/
PingLabel 与鼠标焦点管理；本身继承自 `DXControl`，因此**场景也是控件树根**——所有
窗口以 `Parent = ActiveScene` 挂入（如 `DXMessageBox.cs:73`、`DXInputWindow.cs:33`）。

## 4. 渲染管线：RenderingPipelineManager 体系

> **与任务描述差异**：没有 `DXManager`。曾用 SharpDX 直连 D3D9/D3D11，现已被
> **Silk.NET 多管线抽象**取代；旧 `SharpDXD3D9/SharpDXD3D11` 目录仍在
> `RenderingCore/Rendering/` 下但被 csproj 排除编译（见下）。

### 4.1 管线注册与选择

`RenderingCore/Rendering/RenderingPipelineManager.cs`（1206 行，静态类，:11）：

| 事实 | 位置 |
|---|---|
| 默认管线 `DefaultPipelineId = SilkDXD3D11` | :13 |
| 工厂注册：`SilkDXD3D11` 与 `SilkVulkan` 两条 | :14-19（`PipelineFactories`） |
| 开启管线会话 | `CreateSession` :236-259（失败回滚上一会话） |
| 失败 fallback | `InitializeWithFallback` :280-295（非默认管线失败 → 建默认管线并打日志） |
| 运行时切换（延迟到下一帧） | `RequestSwitchPipeline` :321-329 → `GameLoop` 开头 `ApplyPendingPipelineSwitch()` 消费（`CEnvir.cs:162`） |
| 消息循环 | `RunMessageLoop` :406-412（驱动 `CEnvir.GameLoop` 回调） |
| 全屏/分辨率 | `ToggleFullScreen` :428-434 / `SetResolution` :436-443 |

管线 ID 常量：`RenderingCore/Rendering/RenderingPipelineIds.cs:7-8`
（`SilkDXD3D11 = "DirectX 11"`、`SilkOpenGL`、`SilkVulkan`）。

**管线枚举注意**：`Client/Envir/Config.cs:32` 的 `RenderingPipeline` 配置默认
`SilkDXD3D11`；但 `SilkOpenGL` **工厂未注册**（`RenderingPipelineManager.cs:14-19`
只有 Silk D3D11 与 Vulkan）——配置成 OpenGL 会在 `CreateSession` 抛
`ArgumentException`（:240-242）并 fallback 到 D3D11。

### 4.2 管线接口

`RenderingCore/Rendering/IRenderingPipeline.cs`（:9）：

- `Initialize(RenderingPipelineContext)`；`RunMessageLoop(Form, GameLoop)`；
  `RenderFrame(Action drawScene)` 每帧调用传入的委托。
- 绘图语义：`DrawTexture`/`SetBlend`/`SetOpacity`/`MeasureText`/`DrawLine`/
  `DrawRectangle` 等全套 2D 绘制 API——**客户端所有 UI 都是「贴图 + 纹理绘制」模型**，
  无原生字体/控件（唯一原生控件是宿主 `TargetForm` 的 WinForms 消息泵）。
- 纹理/声音缓存统一走 `RenderingCore/Rendering/CacheItems.cs`：
  `ITextureCacheItem`（:5）与 `ISoundCacheItem`（:12）——`Client/Controls/DXControl.cs:23`
  （`DXControl : IDisposable, ITextureCacheItem`）与
  `Client/Envir/DXSound.cs:12`（`DXSound : ISoundCacheItem`）分别实现，由管线统一
  过期回收（`Config.CacheDuration`，默认 30 分钟）。

### 4.3 遗留 SharpDX 管线（已退役，勿启用）

```
RenderingCore/Rendering/SharpDXD3D9/*.cs
RenderingCore/Rendering/SharpDXD3D11/*.cs
```

- `RenderingCore/RenderingCore.csproj:17-18`：`<Compile Remove="Rendering\SharpDXD3D9\**\*.cs" />`
  与 SharpDXD3D11 同款——**目录存在但从不编译**。
- `Client/Client.csproj:52-53` 仍引用 SharpDX 4.2.0 包（历史遗留；触及
  `SharpDX.DirectSound`（`DXSound.cs`）与 render target 语义，但**管线未注册**，
  运行时不使用 SharpDX 渲染路径）。
- 结论：接手时**不要**试图启用 SharpDX 管线；Silk 管线已承担全部绘制。

### 4.4 每帧绘制调用链（进入渲染）

```
CEnvir.GameLoop()（CEnvir.cs:160）→ RenderGame()（:349-361）
  → RenderingPipelineManager.RenderFrame(drawScene)   // RenderingPipelineManager.cs
  → drawScene = DXControl.ActiveScene?.Draw()          // CEnvir.cs:357
  → DXControl.Draw() 模板方法                          // Client/Controls/DXControl.cs:1761-1769
      OnBeforeDraw → DrawControl → OnBeforeChildrenDraw → DrawChildControls
      → DrawBorder → OnAfterDraw
  → 各子控件同法递归（DrawChildControls :1849；缓存子树 DrawCachedChildControls :1932）
```

- `DXScene.DrawTexture = false`（`DXScene.cs:48`）——场景本身不缓存纹理，子控件可缓存。
- 不可见/零尺寸直接 return（:1762-1763 `IsVisible`/`DisplayArea` 检查）。

## 5. 每帧逻辑：GameLoop → 场景 → 控件树

### 5.1 `CEnvir.GameLoop()`（`Client/Envir/CEnvir.cs:160-348`）

```
GameLoop()
 ├─ if (RenderingPipelineManager.ApplyPendingPipelineSwitch()) return;   // :162 管线热切换
 ├─ UpdateRealtime()                                     // :167
 ├─ UpdateSimulation()                                   // :168
 ├─ RenderGame()                                         // :169 → §4.4
 └─ if (Config.LimitFPS) LimitFrameRate();               // :171-173（Stopwatch 自旋 + Sleep）
```

`UpdateRealtime()`（:199-347）：

- 推进 `Now = Time.Now`；`ActiveScene?.OnMouseMove(鼠标)`（:203-204）；
- 每秒统计 FPS/DPS（:206-215）；`Connection?.Process()`（:217）——**网络泵每帧驱动**；
- DebugLabel/PingLabel 文本与显隐维护（:220-287）；
- HintLabel（悬浮提示）位置计算与出屏夹取（:289-334：TopLeft/BottomLeft/FixedY/Fluid
  四种 `HintPosition`，随后左右下越界修正）。

`UpdateSimulation()`（:336-347）：

- 节拍器 `SimulationStepTicks`（默认 100ms）：到点才 `ActiveScene?.Process()`（:346）——
  **模拟逻辑与渲染帧率解耦**。

`RenderGame()`（:349-361）：见 §4.4。

### 5.2 场景 Process 抽样

- `GameScene.Process()` 实测位置 `Client/Scenes/GameScene.cs:1055-1130`：
  - `MoveTime 100ms` 节拍（:1059-1061）→ `MapControl.Animation++`、`MoveFrame`；
  - `MapControl.CheckCursor`（悬停目标检测）；
  - Ctrl+鼠标物品检测；
  - Equipment/Inventory/Companion 过期道具 `ExpireTime` 递减。
- `LoginScene.Process()`（`Client/Scenes/LoginScene.cs:318-338`）重连逻辑：
  每帧检查 `Connection != null && ServerConnected`；断线后按 `ConnectionTime`
  （加 5 秒）重连；连接参数 `Config.UseNetworkConfig ? IPAddress:Port :
  DefaultIPAddress:DefaultPort`（:342-345）。登录成功后该分支优先 return，不再重连。
- 控件树：`DXControl`（`Client/Controls/DXControl.cs`，~2400 行，类 :23）是 UI 根类，
  管理 Parent/Children、Visible 级联（`CheckIsVisible`）、鼠标穿透（`PassThrough`）、
  拖拽/缩放（AllowDragOut、ResizeBuffer）、纹理缓存注册（建 RenderTarget 并入管线缓存）。

### 5.3 控件体系速查（Client/Controls/）

| 控件 | 用途 | 备注 |
|---|---|---|
| `DXControl` | 一切 UI 根类 | 含 Draw 模板方法（§4.4）、鼠标/键盘事件路由 |
| `DXScene` | 场景基类 | 3 个场景都继承它 |
| `DXWindow` | 可拖拽窗口 | 大部分对话框基类 |
| `DXLabel` | 文本 | GDI `TextRenderer.DrawText`（**top 语义**，§11 坑 3） |
| `DXButton/DXCheckBox/DXComboBox` | 按钮/勾选/下拉 | |
| `DXTextBox/DXNumberTextBox/DXNumberBox` | 文本/数字输入 | 焦点管理 + ActiveScene 键盘路由（DXTextBox.cs:640/673） |
| `DXListBox/DXTreeControl/DXTabControl` | 列表/树/页签 | |
| `DXImageControl` | 贴图控件（常用作对话框根） | |
| `DXItemGrid/DXItemCell` | 物品格/网格 | 背包、仓库、装备格、商店全用它 |
| `DXMessageBox/DXInputWindow/DXItemAmountWindow` | 模态弹窗 | `Parent = ActiveScene` + `MessageBoxList` |
| `DXSliderBar/DXScrollBar/DXHScrollBar/DXVScrollBar` | 滑条/滚动条 | |
| `DXConfigWindow` | 设置窗 | 全屏/边框/分辨率/渲染管线切换（DXConfigWindow.cs:91-95） |
| `DXMapInfoControl` | 地图名提示 | |
| `DXSoundBar` | 音量条 | |

窗口持久化：`DXWindow.Windows` 全局注册表 + `window.LoadSettings()` 保存分辨率到
`WindowSetting` 集合（DB 模型，§8）。

## 6. 网络层与协议

### 6.1 分层

```
CConnection（Client/Envir/CConnection.cs，5109 行）——客户端连接门面
   └─ BaseConnection（LibraryCore/Network/BaseConnection.cs，472 行）——TCP 抽象基类
        ├─ 收发缓冲 + 半包拼接（:69-127）
        ├─ Process 主循环（:311-394）
        └─ 反射分发 ProcessPacket（:396-441）
   └─ Packet（LibraryCore/Network/Packet.cs，446 行）——序列化基类
        ├─ 静态反射包 ID 表（:23-119）
        ├─ ReceivePacket 帧解码（:121-164）
        ├─ GetPacketBytes（:165-187）
        ├─ WriteObject/ReadObject（:189-299/:300-432）
        └─ 忽略/完成特性（:434-446）
```

### 6.2 包类型与计数

| 包族 | 文件 | 数量 | 方向 |
|---|---|---|---|
| C→S | `LibraryCore/Network/ClientPackets.cs` | **153 个 sealed class**（7-861 行） | 客户端 → 服务端 |
| S→C | `LibraryCore/Network/ServerPackets.cs` | **216 个 sealed class**（9-1494 行） | 服务端 → 客户端 |
| 双向 | `LibraryCore/Network/GeneralPackets.cs` | **7 个**（26 行）：Connected/Ping/CheckVersion/Version/GoodVersion/PingResponse/Disconnect | 握手 |

**协议细节**：

- 包 ID 由 `Packet.cs` 静态构造反射 `Assembly.GetExecutingAssembly()` 中
  `BaseType == typeof(Packet)` 的类型并**排序**（GeneralPackets 置顶），构建
  `TypeWrite`/`TypeRead` 字典（:23-119）。**新增/删除包 = 改包类文件，无需手工注册**。
- 收发缓冲与半包拼接（:69-127）；`BeginReceive`（:69）→ `ReceiveData` 循环
  `Packet.ReceivePacket` → ReceiveList（:77-92）。
- `BaseConnection.ProcessPacket`（:396-441）：按 `(ConnectionType, PacketType)` 反射找
  `Process(XxxPacket p)` 方法分发——**继承 CConnection 后只需为每个 S→C 包写
  `public void Process(S.Xxx p)`**（CConnection.cs 内 300+ 个 Process 方法与之一一对应）。
- 发送：`SendList` 合并为 `List<byte>` 后 `BeginSend`（:355-370）；断线缓冲（:273-277）；
  超时 → `TrySendDisconnect(DisconnectReason.TimedOut)`（:349-353）。
- 客户端入口 `CEnvir.Enqueue(Packet)`（`CEnvir.cs:554-557`）→ `Connection?.Enqueue(packet)`。

### 6.3 关键 S→C 处理（会话生命周期）

| 事件 | CConnection.Process | 行为 |
|---|---|---|
| 连接建立 | `G.Connected` :131-135 | 回包 |
| 版本校验 | `G.CheckVersion` :136-143 | SHA256(与 Zircon.exe 同名的 .dll) → `G.Version{ClientHash}` |
| 数据库密钥 | `G.GoodVersion` :147-154 | `Encryption.SetKey` + `scene.LoadDatabase()` + 发 SelectLanguage |
| 心跳 | `G.Ping` :158 / `G.PingResponse` :162 | 回 Ping / 记 `Ping = p.Ping` |
| 登录成功 | `S.LoginSuccessful` :565-573 | 建 SelectScene（含登录音效 :565、角色排序 :570） |
| 进游戏 | `S.StartGame` :755-771 | 建 GameScene、赋 MapInfo/InstanceInfo/User |
| 服务端踢出 | `G.Disconnect` :78-125 | 按 DisconnectReason 弹窗/退款邮件提示 |
| 断线 | `Disconnect()` :51-70 | 清 CEnvir.Connection，抛 Disconnected 事件 |

**数值注意**：`BaseConnection.cs` 缓冲区 `BufferSize = 2048`（:30 附近）、
`Packet.CompressionThreshold = 64`；`BackupBuffer = new byte[BufferSize]`（:54）。
协议中 2048 字节为帧缓冲上限之常规值——凡涉及大批量对象（如 216 包中的
NPC/物品列表包）注意分帧。

## 7. 关键文件地图（Client/ 逐目录）

### 7.1 顶层与 Envir/

| 文件 | 行数 | 职责 |
|---|---|---|
| `Client/Program.cs` | ~90 | 入口/Init/管线上下文/消息循环（§3.1） |
| `Client/TargetForm.cs` | 265 | WinForms 宿主窗体（KeyDown 转发、DragDrop） |
| `Client/Envir/CEnvir.cs` | 1102 | 全局静态状态、GameLoop、DB、翻译、音频 |
| `Client/Envir/CConnection.cs` | 5109 | 客户端网络门面（300+ Process 分发） |
| `Client/Envir/Config.cs` | ~250 | `Zircon.ini` 配置（`[ConfigPath(@".\Zircon.ini")]`；IntroSceneSize=1024×768；MapPath=@".\Map\"；Language="English"；RenderingPipeline=SilkDXD3D11；FontName="MS Sans Serif"） |
| `Client/Envir/DXSound.cs` / `DXSoundManager.cs` | 12+ | ISoundCacheItem 实现 / 音频管理 |
| `Client/Envir/Translations/*` | — | 757 键三语言（§8.4） |
| `Client/Models/` | 26 文件 | 客户端独有显示模型（UserObject/MapObjectObject/用户界面状态等）+ 存盘 `SaveFile.cs` |
| `Client/Components/` | — | 客户端组件（战斗/角色/场景装饰等） |

### 7.2 Scenes/

| 文件 | 行数 | 职责 |
|---|---|---|
| `Client/Scenes/LoginScene.cs` | 2562 | 登录/版本检查/重连/DatabaseVersion 提示 |
| `Client/Scenes/SelectScene.cs` | ~1290 | 选角色（CharacterList :446、`CanStartGame/StartGameAttempted` 防重入 :446/:453-469） |
| `Client/Scenes/GameScene.cs` | 5051 | 游戏主场景（User/MapControl/全部窗口装配 :842-932 SetDefaultLocations，§11 坑 1） |
| `Client/Scenes/GameScene.AutoPath.cs` | — | 自动寻路（A*） |
| `Client/Scenes/Views/` | 44 文件 | 对话框 UI（见下） |

### 7.3 Views/ 全清单（44 + Character/3 + AutoPath = 48 文件，≈42,000 行）

按文件名（行数为实测；★ = 关键大文件）：

```
AutoPathRouteControl 267   AutoPotionDialog 453   BeltDialog 160
BigMapDialog 1361★        BuffDialog 552        BundleDialog 278
CaptionDialog 168         CharacterDialog 3683★  ChatOptionsDialog 647
ChatTab 804★              ChatTextBox 361★      CommunicationDialog 2005★
CompanionDialog 1478      ConsignmentDialog 1600★ CurrencyDialog 598
DungeonFinderDialog 750   EditCharacterDialog 852 ExitDialog 100
FilterDropDialog 100      FishingDialog 745      FortuneCheckerDialog 566
GameStoreDialog 1236      GroupDialog 1303       GuildDialog …
GuildMessageDialog …（尾部清单以 `ls` 实测 44 为准，见下）
```

**要点**：

- `NPCDialog.cs`（8498 行）内含 **22 个命名空间级平级类**（NPC 对话体系全家桶）：
  `NPCDialog` 类在 :21；其余（脚本解析、买卖、存仓、修理、仓库等）同文件平级声明。
  移植/拆分时按类拆文件即可，类间耦合低。
- 对话框普遍模式：ctor 收 `GameScene`/`UserObject` → `SetDefaultLocations` 摆位 →
  `Open()`/`Close()`/`Process()`/`Draw()` 四件套；键盘路由在
  `GameScene.Process()`/`DXControl.KeyDown` 走 `NotNullControl` 链。
- `GameScene.cs:842-932` `SetDefaultLocations` 集中摆位 60+ 窗口（NPC 系列
  `(0, NPCBox.Size.Height)` 连环下挂、InspectBox `(CharacterBox.Size.Width,0)` 右侧挂）。

### 7.4 Models/（26 文件）

- `UserObject.cs`（玩家对象，含 HP/MP/Exp、装备、技能、状态、Buff 等）；
- `MapObjectObject.cs`（地图对象基类：NPC/怪物/掉落/玩家统一模型，动画帧、移动、Attack 状态机）；
- `SaveFile.cs`（窗口布局持久化）；展示层模型参照 `LibraryCore/SystemModels/`。

## 8. 资源格式：.map / .Zl / System.db / 翻译

### 8.1 .map 地图文件（`Client/Scenes/Views/MapControl.cs`）

`MapControl.LoadMap`（:484-540+，实测核心区 484-540）：

```
文件路径 = Config.MapPath + MapInfo.FileName + ".map"
Seek(22) → Width/Height（Int16）              // 表头 22 字节后是尺寸
Seek(28) → Cells = new Cell[Width, Height]    // Cells 初始化
Back 层：循环 Width/2 × Height/2 次           // 1/4 密度（大格）
   BackFile(byte) + BackImage(UInt16)
Front 层：每格
   flag 字节 → MiddleAnimationFrame（动帧偏移）
   value 字节 & 0x8F                          // 原注释 "Probably a Blend Flag"
   FrontFile(byte) + MiddleFile(byte)
   MiddleImage = ReadUInt16() + 1             // :516 —— **帧号 +1 存储约定**
   FrontImage  = ReadUInt16() + 1             // :517 —— **帧号 +1 存储约定**
   3 字节跳过
   Light = ReadByte() & 0x0F * 2              // 灯光值
   1 字节跳过
渲染：cell.MiddleImage-1（:1523）、cell.FrontImage-1（:1537）⇔ 存储 +1 互为逆运算
```

- **+1 约定**：-1 = 空（无贴图）。存 0 → 渲染 -1 空；存 N → 帧 N-1。任何改写 .map
  的工具必须遵守（服务端 `Server/Views/MapViewer.cs:370` 同约定）。
- KROrder 使用点：`MapControl.cs:359/:362/:1499/:1521/:1535`（`file != LibraryFile.Tilesc`
  时走 `CEnvir.LibraryList` 动态换图库）；`Tilesc` 为默认地表库。
- 灯光：`Light = ReadByte() & 0x0F * 2` 的 `*2` 把 0-15 标度映射到 0-30 的
  `MapLight` 枚举段（`MapControl.cs` Draw 中 `MapLight` 应用）。

### 8.2 .Zl 图库（加载真实位置：RenderingCore）

| 事实 | 位置 |
|---|---|
| 图库索引声明 | `LibraryCore/Libraries.cs:8-435+`（`LibraryList` 路径字典）+ `LibraryFile` 枚举 :463-835（约 150 值，**枚举不是类**） |
| KROrder 映射表 | `Libraries.cs:385`（`Dictionary<int, LibraryFile>`，`[0] = Tilesc`） |
| .Zl 加载/解码 | `RenderingCore/Library/MirLibrary.cs`（1885 行）：旧格式 v0/1 头 :54-105、ZL2 容器 `TryReadCompressedContainer` :107-153、`CreateImage` :224、`TryGetTexture` :252、`GetRenderTexture` :268、`Draw` :453 |
| 帧元数据 | `RenderingCore/LibraryFormat/ZlImageMetadata.cs`（99 行）：宽高/偏移/影子/Overlay/codec/BC7 备用段 :20-40；version 0=Dxt1、1=Dxt5 归因 :50-53 |
| 容器/编码 | `RenderingCore/LibraryFormat/ZlFormat.cs` |

**易错点**：`LibraryCore` 中**不存在** `LibraryImage.cs`/`LibraryFile.cs` 类（grep
双策略确认）；`LibraryFile` 是 `Libraries.cs` 里的枚举。先前交接文本
「LibraryCore/MirLibrary.cs」为笔误——真实加载器在 RenderingCore。

### 8.3 System.db（MirDB）

- 客户端唯一 `new Session` 在 `CEnvir.cs:386`：`SessionMode.Users` + `@".\Data\"`；
  `:388-391` Initialize(ItemInfo 与 WindowSetting 程序集)；`:398-417`
  `GetCollection` 灌 21 类模型列表。
- `LibraryCore/MirDB/Session.cs`（605 行）：`SystemPath :43` = Root+System.db；
  ctor :61-89/:91-102；`Initialize :104-146`。
- `Encryption.cs:89` `SetKey`（AES-256，密钥来自 `CConnection.cs:147`）——
  仅用于读写本地 System.db 的敏感字段；**网络包不加密**。
- 模型：`LibraryCore/SystemModels/` 39 文件 13,611 行；`DBObject.cs` 333 行、
  `DBValue.cs` 257 行；`Stat.cs` 931 行（Stat 枚举 :507-895 共 114 值）；
  `Enum.cs` 3439 行（SpellKey :1592-1654、MonsterFlag :1656-1755、
  MilestoneType :1796-1945、Packet Enums :2051-2350 区间）。

### 8.4 翻译（Client/Envir/Translations/）

- 三件套：`EnglishMessages/ChineseMessages/JapaneseMessages/StringMessages/`
  （757 键；每日消息 + 游戏提示 + UI 文案）。
- 加载：`CEnvir.cs:98-108` `LoadLanguage` —— **switch 无 default**（§11 坑 4）。
- Godot 侧同构：`GodotClient/Translations/` 四语言（含 JapaneseMessages、StringMessages）。

## 9. 周边工程（更新链 / 工具 / BotRunner）

### 9.1 更新链（Launcher → Patcher → PatchManager）

| 工程 | 角色 | 要点 |
|---|---|---|
| `Launcher/` | 启动器（WinForms，HTTP 更新） | `LMain.cs:22` ClientFileName=Zircon.exe；启动前比较版本 → 下载列表 + 内嵌 `Patcher.exe` 自更新（Patcher 写盘后重新拉起） |
| `Patcher/` | 补丁下载器 | DevExpress WaitForm：下载补丁 → **原子替换**（临时文件 + Move）→ 重启 Launcher |
| `PatchManager/` | 打包/上传（WinSCP 自动化） | 把 `Debug/Client/` 增量打进补丁包 → FTP/WinSCP 传 `mirfiles.com`；日志/清单在 PatchManager 目录 |

### 9.2 工具（ImageManager / LibraryEditor）

- `ImageManager/`（8 文件 1282 行）：图库批量导入导出、格式转换；依赖
  `ManagedSquish`（DXT 软解/软编，`Components/`）。
- `LibraryEditor/`（26 文件 10,734 行）：WIL/.Zl 图库编辑器（浏览/替换/导出）；
  `NativeSquish_x64` 用于快速 DXT 编解码。
- 二者均**独立小工具**，不参与游戏运行；改动需自测 DXT1/5 回读一致性。

### 9.3 BotRunner（协议级机器人，13 文件 4,630 行）

- net10.0 控制台；直连 7000，发送真实 `ClientPackets`，解析 `ServerPackets`。
- 用途：压测/挂机模拟/自动刷怪；GodotClient 单机模式启动时经
  `SinglePlayerLauncher` 主动拉起（§10）。
- 服务端部署组件之一（82 服务器使用）。

### 9.4 插件体系（PluginCore / PluginStandalone）

- `PluginCore/`（14 文件 484 行）：插件接口（`IPlugin`/`IPluginHost`）。
- `PluginStandalone/`（4 文件 184 行）：插件宿主示例。
- **仅服务端**：`Server/SMain.cs:41-45` 加载插件目录；客户端零引用。

## 10. Godot 对照与移植要点

### 10.1 目录对位

| 原版 | GodotClient | 说明 |
|---|---|---|
| `Client/`（WinForms + 控件树） | `GodotClient/Scenes/`, `Scripts/` | 场景/脚本重写（Godot 节点树） |
| `LibraryCore/Network/*.cs` | 协议沿用 | 包结构/ID 反射表未变（Godot 侧序列化器对位） |
| `Client/Envir/Translations/` | `GodotClient/Translations/` | 四语言同构（JapaneseMessages 新增） |
| `RenderingCore/` | `GodotClient/Shaders/` | Silk 绘制 → Godot Shader（按 Blend 模式对位） |
| `BotRunner/` | `GodotClient` 单机模式 | 单机启动器拉起 BotRunner 当服务端 |
| `Client/Scenes/Views/*.cs` | `GodotClient/UI/**` | 对话框逐一对位（见 §7.3 表） |

### 10.2 已确认的移植行为差异源（详见 §11）

1. **布局**：原版 `SetDefaultLocations` 连环挂载（GameScene.cs:842-932）→ Godot
   HUD 分项叠加——**不是单一全局偏移**，逐窗口对照。
2. **字号**：原版 pt→逻辑像素沿用 → Godot 天然小约 25%（`MirSkin.FontScale=4/3`）
   ——移植字号按 4/3 缩放。
3. **文本基线**：GDI TextRenderer 是 top 语义，Godot 是 baseline——两套 Y 坐标
   需换算，否则行高/垂直居中错位。
4. **图库**：.Zl 帧索引 +1 约定（§8.1）在 Godot 侧 AssetLoader 同样遵守（对照
   `GodotClient/Scripts/` 加载器实现）。

## 11. 已知坑与「别做什么」

> 9 项必读坑（均来自 2026-08 实际踩坑/审计记录，行号为当日核对）：

1. **SetDefaultLocations 双层层叠**（`GameScene.cs:842-932` 集中摆位，:417/:816
   调用 + LoadSettings 从 DB 覆盖）：NPC 系列 `(0, NPCBox.Size.Height)` 连环下挂、
   InspectBox `(CharacterBox.Size.Width,0)` 右挂。Godot 侧 HUD 上移是**分项叠加**
   而非全局偏移——排查布局先看该函数与 `WindowSetting` 持久化。
2. **pt→px 字号**：`CEnvir.FontSize` 用 pt，新版逻辑像素直接沿用旧数值 → 天然小
   ~25%；修复：`MirSkin.FontScale = 4/3`。对照表：8→11 / 9→12 / 10→13 / 12→16 /
   13→17（px）。
3. **GDI top vs Godot baseline Y 语义**：`DXLabel` 用 `TextRenderer.DrawText`
   （top 锚），Godot Label 用 baseline 锚——同一 Y 在 Godot 偏高半个行高；所有
   Label 垂直位置需换算（行高 ≈ 字号 × 1.14）。
4. **翻译双层层叠 + 无 default**：`Translation` 启用开关与语言选择两层叠加（某层
   为 null 则整体失效）；`CEnvir.cs:98-108` switch 无 default → 未知语言直接
   null 崩溃。改语言配置/加语言必须同时改 `Config.cs` 默认与 `LoadLanguage`。
5. **NPCDialog.cs 单文件 22 类**（:21 起）：改 NPC 对话框先 `grep -n "class "`；
   新增类建议直接拆文件，勿再往 8498 行文件里堆。
6. **NoColourKey 机制**：原版 `MapControl`/`Draw` 无颜色键；Godot 侧
   `NoColourKey` 开关在 `GodotClient/Scripts/MagicEffectTable.cs:99-100,153-154`
   与 `GameScene.cs:3852/3872/3896/3919`、`DataLayer.cs:320`——**魔法特效贴图带
   黑底时打开；会忽略颜色键并以黑色替换**，逐处核对是黑底还是透明底。
7. **.map 帧号 +1**（§8.1）：写 .map 工具必须存 UInt16+1、渲染时 -1；曾因
   「+1/-1 双重偏移」导致墙体缺口（2026-08-11 沙巴克移植事故，见
   `Docs/Sabak_Map_Migration_Audit.md`）。校验工具不得复用生产解析逻辑（AGENTS.md
   强制规则 4）。
8. **SharpDX 遗留陷阱**：`RenderingCore/Rendering/SharpDXD3D9|11/` 被
   `RenderingCore.csproj:17-18` 排除编译，`Client.csproj:52-53` 却仍引用 SharpDX
   4.2.0 包——**源码搜索能搜到但运行时不执行**；修改这些目录或误启用会破坏
   `RenderingPipelineManager` 会话模型。
9. **图库缺失不崩**：`Program.cs:53-57` 加载 .Zl 用 `File.Exists` 过滤，缺失库
   静默跳过；引用缺失库的贴图区域空渲染——排查「某 UI 空白」先查
   `CEnvir.LibraryList` 与实际 `Debug/Client/Data/` 文件集。

**「别做什么」红线**：

- 不要改 `Server/` `ServerCore/` `ServerLibrary/`、`GodotClient/`（各自另文档/交接）；
- 不要试图在 Linux 上完整构建 WinForms 客户端（依赖 WinForms + SharpDX + DevExpress，
  必然失败）；验证用 `dotnet build GodotClient/ZirconClient.csproj` 或 Godot 运行；
- 不要触碰 `Components/SlimDX.dll`（死资产）与 `docs/reviews/`（未提交工作区）；
- 不要在运行中的服务端写 System.db（双库纪律见 AGENTS.md）。

## 12. 延伸资料

### 仓库内

- `docs/codebase/_index.md` + `docs/codebase/`（23 篇原版代码深度文档：战斗公式/
  怪物 AI/协议/玩法/基础设施——**移植任何功能前先查这里**）
- `docs/codebase/protocol/packets-c2s.md`（153+7）、`packets-s2c.md`（216+7）——
  包计数与本文 §6 完全一致
- `Docs/Sabak_Map_Migration_Audit.md`（+1 坑事故复盘）、`MAP_FORMAT_COMPARISON.md`
  （.Zl vs NAS .wil 格式对比）、`MAGIC_FULL_AUDIT.md`、`MAGIC_GROUND_EFFECT_FIXES.md`
- `docs/handoffs/`（本系列交接文档）

### Mir3-Research（`/home/tetsuya/development/Mir3-Research/`）

- `docs/research/ei-ui-layout/README.md`（UI 布局取证）、`docs/`（逆向研究全集）、
  `Tools/TOOL_INDEX.md`（解码/审计工具索引）、`Tools/common/wilsdk.py`（WIL/WIX 解码）
- 环境变量：`MIR3_EI_ROOT`、`MIR3_MUD3_ROOT`、`MIR3_ZIRCON_ROOT`

### 测试与部署

- 测试账号：`test@test.com` / `test123` / 角色 `TestHero`（Admin=True 永久 GM，
  支持 `@` 管理命令、`MasterPassword` 机制）
- 端口表：7000 游戏 / 8810 dbeditor / 8820 uieditor / 8822 webclient / 8800 dbviewer /
  8899 mapviewer / 8765 wilviewer / 8830 yomu / 8831 fudoki / 80 svc-dashboard
- 无头验证配方：Xvfb :100 + openbox + godot-mono + scrot；4K 缩放测试
  `ZIRCON_UI_SCALE=2`；构建命令 `dotnet build GodotClient/ZirconClient.csproj`

## 附：自检（本文档核查记录）

### 目标三问答

1. **本文档目标**：让新接手者在不读原版 21.8 万行源码的前提下，能定位任意客户端
   行为对应的文件/行号、理解启动→渲染→网络→资源全链路、避开 9 个已知坑。
2. **为什么写**：原版客户端是 WinForms + 自绘管线 + 明文协议的 20 万行遗产，
   无文档时定位成本极高；Godot 移植需要原版行为作为对照基准（AGENTS.md 验证深度约定）。
3. **口径差异**：任务描述「DXManager」已不存在 → `RenderingPipelineManager.cs`;
   SharpDX 管线目录 csproj 排除；唯一同名类在 `Server/Views/MapViewer.cs:370`（GDI+）。

### 20 处引用抽查（行号 ↔ 源码）

| # | 引用 | 核验结果 |
|---|---|---|
| 1 | `Program.cs:68` new LoginScene | ✔ :68 `DXControl.ActiveScene = new LoginScene` |
| 2 | `Program.cs:70` RunMessageLoop | ✔ :70 |
| 3 | `CEnvir.cs:160` GameLoop | ✔ :160 方法起始 |
| 4 | `CEnvir.cs:162` ApplyPendingPipelineSwitch | ✔ :162（先前草稿写作 :164，已更正） |
| 5 | `CEnvir.cs:346` ActiveScene.Process | ✔ :346 `ActiveScene?.Process()` |
| 6 | `CEnvir.cs:362-372` ReturnToLogin | ✔ :363 声明，:364-371 重建 LoginScene（区间 362-372 覆盖） |
| 7 | `CEnvir.cs:554-556` Enqueue | ✔ :554-557 |
| 8 | `CEnvir.cs:98` LoadLanguage | ✔ :98 方法起始 |
| 9 | `Config.cs:13-14` DefaultIPAddress/Port | ✔ 127.0.0.1/7000 |
| 10 | `BaseConnection.cs:69/77/119` BeginReceive 链 | ✔ :69 BeginReceive、:77 递归、:119 检查 |
| 11 | `Packet.cs:23-119` 反射 ID 表 | ✔ 静态构造区间 |
| 12 | `CConnection.cs:147` Encryption.SetKey | ✔ GoodVersion 分支 |
| 13 | `CConnection.cs:569-571` SelectScene 创建 | ✔ :565-573（包处理 + 场景赋值，含音效/排序） |
| 14 | `CConnection.cs:755-771` GameScene 创建 | ✔ :755-771 |
| 15 | `RenderingPipelineManager.cs:11` 静态类 | ✔ :11 |
| 16 | `RenderingPipelineManager.cs:14-19` 工厂注册 | ✔ 仅 SilkD3D11+SilkVulkan |
| 17 | `RenderingPipelineManager.cs:280-295` fallback | ✔ |
| 18 | `RenderingCore.csproj:17-18` Compile Remove | ✔ SharpDXD3D9/11 两目录 |
| 19 | `MapControl.cs:516-517` `ReadUInt16()+1` | ✔ :516/:517 帧号 +1 |
| 20 | `MapControl.cs:1523/1537` 渲染 `-1` | ✔ :1523 MiddleImage-1、:1537 FrontImage-1 |

### 事实性备注

- 端到端登录/渲染**未在本机跑通**（Linux 无 WinForms/SharpDX 运行环境，AGENTS.md
  红线）；行号均为源码级 `grep`/`read` 核验，行为结论来自代码阅读与既有审计文档，
  非运行取证。
- 包计数 153/216/7 与 `docs/codebase/protocol/*.md` 一致（三方交叉：源码反射表 +
  zdocs + 本文）。
- 20 处抽查全部通过；草稿期 2 处错误（`:164`→`:162`；「LibraryCore/MirLibrary.cs」
  笔误 → 真实在 RenderingCore）已在本版修正并标注。