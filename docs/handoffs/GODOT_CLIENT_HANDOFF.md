# Godot 客户端接手文档（GODOT_CLIENT_HANDOFF）

> 覆盖范围：Zircon **Godot 4 客户端**（`GodotClient/`，C#，Godot.NET.Sdk 4.6.3）。
> 读者假设：已具备服务端（`ServerLibrary`/`ServerCore`）经验，需要接手或修改客户端；或需要跑客户端验证服务端改动。
> 事实边期：2026-08-17（`master` @ `/home/tetsuya/development/zircon`）。文中所有 `文件:行号` 以当日工作区为准，改动前先 grep 复核。
> 姊妹文档：`docs/handoffs/SERVER_HANDOFF.md`（服务端，1309 行）——结构对齐，内容互补。

## 目录

1. 这是什么
2. 怎么跑起来
3. 无头验证配方
4. 架构总览
5. 场景与启动流程
6. 网络层
7. 战斗与地图交互
8. UI 体系与缩放
9. 翻译与本地化
10. Web 移植线证据
11. 常见任务与陷阱

---

## 1. 这是什么

Zircon 的 Godot 4 客户端：用 C#（`Godot.NET.Sdk/4.6.3`，`GodotClient/ZirconClient.csproj`；`project.godot` `config_version=5`）重写的原版《传奇3》客户端。核心是三大块：

- **自绘 UI（DXControl 体系）**：不用 Godot 的 Control 皮肤，而是用 `.Zl` 图库贴图自绘全套窗口/控件（背包、技能、聊天、配置等 46+ 窗口），`GodotClient/Controls/` 下 60 个文件约 21,855 行。
- **实时网络客户端**：TCP 连 Zircon 服务端（ServerCore :7000），包协议复用 `LibraryCore`（与服务端同一套 Packet 定义），`GodotClient/Network/ServerConnection.cs` 有 220 个 `public void Process(...)` 分发器。
- **地图与战斗渲染**：自研 `.map` 读取器（`GodotClient/Formats/MapReader.cs`）、`Zl` 图库解码（`GodotClient/Formats/ZlReader.cs`）、`MapView`（`GodotClient/Scripts/MapView.cs`）逐帧 `_Draw` 绘制，`GameScene`（`GodotClient/Scripts/GameScene.cs`，10323 行）承载全部游戏逻辑。

与原版 `Client/` 的关系：**并行重写，不是移植编译**。原版行为是唯一参照（预测/回拉公式、帧表、坐标约定均以原版源码为准，如 `ComputeAttackIntervalMs` 注释直引原版 `UserObject.cs:638-653`，GameScene.cs:1498）；不要触碰原版 `Client/` 源码（除非通过 `NoColourKey` 机制等明确手段，见 `AGENTS.md`）。协议层直接复用 `LibraryCore`（客户端与服务端同一仓库、同一 Packet 类），因此客户端的 `Enqueue(new C.XXX)` 与服务端 `Process(C.XXX)` 天然对齐。

代码规模实测（2026-08-17）：`GodotClient/` 138 个 .cs 文件约 54,682 行；其中 `Controls/`（自绘控件+窗口）21,855 行、`Scripts/`（场景逻辑，含 GameScene 10,323 行）为主力。

## 2. 怎么跑起来

### 2.1 构建

```bash
cd /home/tetsuya/development/zircon
dotnet build GodotClient/ZirconClient.csproj
# 增量缓存有坑时：
dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental
```

必须在仓库根目录执行（`~` 下找不到项目文件）。构建产物由 Godot.NET.Sdk 接管，`godot-mono` 运行时直接加载。

### 2.2 连服务器跑（有窗口）

```bash
godot-mono --path /home/tetsuya/development/Zircon/GodotClient -- \
  --server 127.0.0.1 --port 7000 --user test@test.com --pass test123 --char TestHero --window
# 连 82 远程：--server 192.168.3.82 --port 7000（其余同）
```

测试账号 `test@test.com` / `test123` / 角色 `TestHero`，该账号 `Admin = True`（永久 GM，游戏内可用全部 `@` 命令，命令表见 `AGENTS.md`）。

### 2.3 单机模式（自动拉起服务端）

不传 `--server` 且 7000 端口无监听时，`SinglePlayerLauncher`（`GodotClient/Network/SinglePlayerLauncher.cs`，214 行）自动拉起 `dotnet ServerCore.dll --singleplayer-dev` 并做进程生命周期绑定：

- `EnsureServerRunning`：`IsPortOpen` 500ms 探测；无监听且未指定 `--server` 才拉起。
- `WaitForServer`：15s / 250ms 轮询等端口就绪。
- `Shutdown`：只杀自己拉起的 PID（不误杀别人的服务端）；stdin EOF 补丁防僵尸。
- `_Notification` 钩住 WM 关闭 / `ExitTree` 时同步关服。

单机模式给测试账号注入满级/全技能/全装备（`ServerLibrary/Envir/DevSinglePlayer.cs:12`；`ServerCore/Program.cs:17-22` 解析 `--singleplayer-dev`）。服务由 `hub` 管理：`zircon-dev`（单机）、`zircon-server`（常驻）。

### 2.4 数据依赖（客户端跑起来的前提）

| 数据 | 路径 | 装载代码 | 缺失症状 |
|---|---|---|---|
| System.db | `../Debug/Client/Data` | `NetworkManager._Ready` → `DatabaseLoader.Load()`（`GodotClient/Network/DatabaseLoader.cs:19` 签名；root=`../Debug/Client/Data` 组装在 :24） | `ClientUserItem.Complete()` NRE → 进游戏即断线 |
| 帧表/特效/音效表 | `ClientData/`（json） | `NetworkManager._Ready` 首位 `Scripts.DataLayer.LoadAll()`（frame-formulas.json→FrameSet 94 表 / magic-effects.json / sounds.json；目录探测 `ResolveClientDataDir`：`ZIRCON_CLIENT_DATA` 环境变量 → `res://ClientData` → 工程上级 `zircon/ClientData` → cwd） | 动作帧错乱、音效全无 |
| 地图 | `../Debug/Client/Map` | `MapView._mapPath`（`Scripts/MapView.cs:48`）；MapTestScene 默认 `/home/tetsuya/development/zircon/Debug/Client/Map/` | 黑屏/地图加载失败 |
| 图库 .Zl | `../Debug/Client/Data` | `MirSkin.DataPath`（`Controls/MirSkin.cs:21`，相对 res:// 探测，回退两个硬编码 /home/tetsuya 路径） | UI/怪物全白框 |
| 音频 | `res://../Debug/Client/Sound/` | `SoundPlayback.Play`（`Scripts/SoundPlayback.cs:14`） | 无声 |
| 翻译 | `Translations/`、`translations/db_names.json` | `Lang.Reload` / `LocalizedName.EnsureLoaded`（`Scripts/LocalizedName.cs:28-42`） | 文案回落英文 |

### 2.5 CLI 参数（`GodotClient/Scripts/AutoLoginArgs.cs`）

`User` :21、`Password` :24、`Character` :27、`ServerAddress` :34、`ServerPort` :39-49、`RunningTest` :51、`RightRunTest` :52、`Language` :54-55、`OfflineMovementTest` :60、`InteractionAudit` :61、`OperationAudit` :62、`OperationAuditExt` :63、`ScreenshotAfterEnter` :64、`UiDiagnosticBorders` :67、`Window` :75、`WindowSize` :78+。

**陷阱**：裸 `--window` 会吞掉下一个参数（GetValue 实现缺陷），要开窗口请写 `--window=WxH`。

## 3. 无头验证配方

### 3.1 总原则

`dotnet build` 通过只证明语法正确（AGENTS.md 验证深度约定）。涉及登录/进游戏/地图渲染的改动必须跑真实流程。工作区直跑可能被登录场景/autoload/其他 Godot 进程干扰，**优先用隔离目录** `/tmp/zircon-audit/`（`GodotClient` 为副本、`Debug` 软链回工作区数据）。

### 3.2 headless 场景审计

```bash
# 构建
dotnet build GodotClient/ZirconClient.csproj --no-restore --no-incremental

# 地图渲染审计（隔离目录）
godot-mono --path /tmp/zircon-audit/GodotClient --headless \
  --rendering-method gl_compatibility Scenes/MapTestScene.tscn -- --render-audit

# UI 全家桶（30+ 审计 flag，约 3s 全 PASS）
timeout 15s godot-mono --headless --quit-after 8 --path GodotClient \
  res://Scenes/UITestScene.tscn -- \
  --ui-audit --npc-audit --communication-audit --magic-audit \
  --ranking-audit --monster-audit --quest-audit --chat-audit \
  --character-audit --storage-audit --minimap-audit
```

**`--quit-after` 必须放在 `--` 之前**（引擎参数）：放在 `--` 后会被 `OS.GetCmdlineUserArgs()` 当用户参数，引擎不退出，表现为"PASS 后挂起"。

审计 flag 两张表：

- `UITestScene`（`Scripts/UITestScene.cs:51-67`）：`--ui-export` / `--ui-audit` / `--npc-audit` / `--communication-audit` / `--magic-audit` / `--ranking-audit` / `--monster-audit` / `--quest-audit` / `--chat-audit` / `--character-audit` / `--storage-audit` / `--minimap-audit`（另有 fortune/fishing/companion/group/guild/gamestore/consignment/currency/help/horse/dungeon/edit-character/auto-potion/group-lfg/config/keybind/window-chrome 等）。
- `MapTestScene`（`Scripts/MapTestScene.cs:113-122`）：`--render-audit` / `--action-audit` / `--map-audit` / `--shadow-audit` / `--pixel-audit` / `--projectile-audit` / `--dead-target-audit` / `--player-matrix-audit` / `--light-audit` / `--light-render-audit`（还有 `--network-audit` / `--cursor-audit` / `--combat-audit` / `--weather-render-audit` / `--map-family-render-audit`）。

注意命名：只有 `--ui-audit` 带 `ui-` 前缀，其余 UI 审计是 `--npc-audit` 不是 `--ui-npc-audit`。

### 3.3 有窗口 X11 截图验收（Xvfb + openbox + scrot）

```bash
Xvfb :100 & openbox --display :100 &
DISPLAY=:100 /tmp/godot-mono --path /home/tetsuya/development/Zircon/GodotClient \
  -- --server 127.0.0.1 --port 7000 --user test@test.com --pass test123 --char TestHero --window
DISPLAY=:100 scrot /tmp/shot.png
```

- 无 WM 的裸 Xvfb 视口会固定在 1024x768——要测大分辨率必须配 openbox。
- 4K 缩放验证用 `ZIRCON_UI_SCALE=2`（注意：该环境变量只对 UiScaler 生效，见 §8.2）。
- 历史有效截图：`/tmp/ui_test.png`、`/tmp/zircon-render-audit.png`、`/tmp/zircon-projectile-audit.png`、`/tmp/zircon-weather-rain-fog-lightning.png`、`/tmp/zircon-map-family-0.png`。
- 带窗口的 Vulkan 截图审计（`--light-render-audit`/`--weather-render-audit` 等）比 headless 更有价值。

### 3.4 游戏内人工验证

无头/截图只能覆盖静态面；登录链、切图、战斗手感这类必须真人进游戏（测试账号见 §2.2）。GM 命令速查（完整表在 `AGENTS.md`）：`@move D201`（矿区）、`@level 50`、`@giveSkills`、`@spawn GhostSorcerer 5`、`@toggleGM`。

## 4. 架构总览

### 4.1 分层

```
GodotClient/
├── project.godot / Scenes/*.tscn        # 场景定义（.tscn 在 Scenes/，代码全部在 Scripts/！）
├── Scripts/                             # 场景逻辑 + 全局系统
│   ├── GameScene.cs (10323 行)          # 主游戏场景：HUD、输入、战斗、地图装载、网络消费
│   ├── LoginScene.cs / SelectScene.cs   # 登录/选人
│   ├── UITestScene.cs / MapTestScene.cs # 审计场景（30+ flag）
│   ├── MapView.cs                       # 地图渲染（_Draw 逐帧）
│   ├── UiScaler.cs / ClientSettings.cs  # 缩放与持久化
│   ├── ObjectRenderer.cs / PlayerRenderer.cs / MouseWalker.cs  # 对象渲染与移动
│   ├── CombatController.cs              # 攻击节奏
│   ├── Lang.cs / LocalizedName.cs       # 翻译
│   └── DataLayer.cs                     # json 数据表装载
├── Controls/                            # 自绘 UI（约 21855 行）
│   ├── DXControl.cs / DXWindow.cs       # 自绘控件基类 / 窗口基类
│   ├── WindowManager.cs                 # Z 序管理
│   ├── MirSkin.cs                       # .Zl 贴图供给
│   ├── KeyBindManager.cs / UiOverlay.cs # 键位 / 布局热覆盖
│   └── InventoryDialog.cs 等约 50 个窗口
├── Network/
│   ├── NetworkManager.cs (103 行)       # Godot Node：同步轮询驱动 BaseConnection
│   ├── ServerConnection.cs              # 220 个 Process 分发器 + Pending 队列族
│   ├── SinglePlayerLauncher.cs          # 单机模式拉服
│   └── DatabaseLoader.cs                # System.db 只读装载
├── Formats/
│   ├── MapReader.cs (class MirMap)      # .map 地图解析
│   ├── ZlReader.cs (class ZlLibrary)    # .Zl 图库解码 + 纹理缓存
│   └── LibraryCache.cs                  # ZlLibrary 实例缓存
├── Translations/                        # UI 字符串（StringMessages + 三语言）
└── translations/db_names.json           # 数据层名称映射（zh/ja 查表）
LibraryCore/                             # 与服务端共享：Packet 定义、BaseConnection、SystemModels
```

**路径陷阱**（多处直觉路径是错的，引用前核对）：`MapView` 在 `Scripts/` 不在 `Controls/`；`ClientSettings` 在 `Scripts/` 不在 `LibraryCore/Data/`；`MirSkin` 在 `Controls/` 不在 `Scripts/`；`KeyBindManager` 在 `Controls/`（类定义 :105）；场景 .cs 全在 `Scripts/`，`Scenes/` 只放 .tscn。

### 4.2 一帧的时序

1. `NetworkManager._Process`（`Network/NetworkManager.cs`）：同步轮询 `TcpClient.Available`（反射取 `BaseConnection.Client`）读字节 → `Packet.ReceivePacket` 拼半包 → 包入 `ReceiveList` → `Process()` 排空 ReceiveList 逐包反射调用 `Process(包类型)`（ServerConnection.cs）→ 事件/Pending 队列两条出口。
2. `GameScene._Process`（`Scripts/GameScene.cs:8103-8115`）：HUD viewport/scale 变化才 `RefreshUiScale`+`LayoutHud`；`_pendingMagicPacket` 在动作边界释放（:8137-8147）；`TryContinueMining` / `ProcessPendingAutoPathMove`。
3. `MapView._Draw`（`Scripts/MapView.cs:89` 起）：地面层不加格、中/前景对象加一格（Y 基线差异，注释 :99-100）。
4. `GameScene._Input`（:9882-9970）/ `_UnhandledInput`（:9986 起）：见 §7.4。

### 4.3 数据流三通道

- **事件**：`ServerConnection` 的 220 个 `public event`（LoginResultEvent/StartGameResultEvent/ObjectMoveEvent…），GameScene/LoginScene 订阅，回调里必须 `CallDeferred` 回主线程。
- **Pending 队列**（`ServerConnection.cs:291-308`）：`PendingMoves/PendingTurns/PendingPlayers/PendingMonsters/PendingNPCs/PendingItems/PendingChats/PendingRemoves/PendingAttacks/PendingMagics`——启动缓冲期（`BufferPendingPackets` 默认 true :311）到达的世界包先进队列，`DrainPendingObjects()`（GameScene.cs:7533）在订阅生效后按服务器顺序排空（顺序注释在 :7532）。
- **直发**：登录前包（版本握手、Login）直接走事件。

## 5. 场景与启动流程

### 5.1 场景清单（Scenes/*.tscn，代码在 Scripts/ 同名 .cs）

| 场景 | 用途 | 关键代码 |
|---|---|---|
| LoginScene.tscn | 登录 | `Scripts/LoginScene.cs`（_Ready :50-90，OnLoginResult :165-200） |
| SelectScene.tscn | 选人 | `Scripts/SelectScene.cs`（同 UiScaleLayer 模式 :68/:71/:73） |
| GameScene.tscn | 主游戏 | `Scripts/GameScene.cs`（_Ready :918） |
| UITestScene.tscn | UI 审计 | `Scripts/UITestScene.cs`（`--ui-export` :51-54，SelfCheck ~:1120，截图 /tmp/ui_test.png） |
| MapTestScene.tscn | 地图审计 | `Scripts/MapTestScene.cs`（2736 行，60 处 GetCmdlineUserArgs） |

### 5.2 登录到进游戏全链

1. **LoginScene._Ready**（:50-90）：自建 `UiScaleLayer` CanvasLayer → `BuildLegacyLoginUi` → `UiScaler.UpdateScale`（:65）→ `ZIRCON_UI_AUDIT` 审计（:68）→ 订阅 `GetViewport().SizeChanged`（:71）。
2. 网络握手（ServerConnection）：TCP 连上 → 服务端 `G.Connected` 处理发 `G.CheckVersion`（若 `Config.CheckVersion`，`ServerLibrary/Envir/SConnection.cs:272-274`；本仓库 `Debug/ServerCore/Server.ini:12` 已设 `CheckVersion=False`）→ 客户端 `Process(G.Connected)`（ServerConnection.cs:364-368）回 `G.Connected` → 服务端发 `G.GoodVersion` → 客户端 `Process(G.GoodVersion)`（:369-373）**先发 `C.SelectLanguage{Language="Chinese"}`** 再触发 `VersionOK` 事件。
3. LoginScene.OnVersionOK（:200）→ `CallDeferred(ShowVersionOK)` → 用户提交 → `SendLogin`（ServerConnection.cs:893）→ `LoginResultEvent` → `OnLoginResult`（LoginScene.cs:165-200）Success → 切 SelectScene.tscn。
4. SelectScene → `SendStartGame(int)`（ServerConnection.cs，内含 `[Net] SendStartGame` 日志）→ 服务端回 `S.StartGame` → `Process(S.StartGame)`（:415-419，打印 Magics/Set1Key 调试日志）→ `StartGameResultEvent`。
5. **GameScene._Ready**（:918）：首段 `ClientSettings.Load` → `KeyBindManager.Load` → `UiOverlay.Load` → ApplyDisplay/AudioSettings → `MouseFilter.Ignore` → `Scale=2` → 建 MapView → 光照 CanvasLayer Layer=1 → `_mouseWalker`（:976）→ CombatController。
6. **启动排空**（:1430-1470）：`DrainPendingObjects()` + `_net?.Connection?.StopPendingPacketBuffering()`（关缓冲，ServerConnection.cs:313-316）在 StartInfo 分支**之前**；StartInfo 非空 → 主线程直接 `ShowStartGameResult()`（:1756-1800：应用 StartInfo、`_canRun=true`、`InitHudData`、`_startGameShown=true`、`LoadPlayerMap(clearObjects:false)` + `_waitingStartupMap=true` + 2s 兜底 `StartupMapFallbackDelaySeconds=2.0` :903）。
7. **切图**：`Process(S.MapChanged)`（ServerConnection.cs:420-426）——启动阶段 MapChanged 只通知不清队列（注释原文明说）；运行态切图才 `ClearPendingWorldPackets()`（:319-330，反射清空所有 Pending 字段，防旧世界包在新图复活）→ `MapChangedEvent` → `OnMapChanged`（GameScene.cs:1805-1815，`_startGameShown` 才 CallDeferred）→ `FinalizeStartupMap`（:1818-1824）。

### 5.3 地图装载

`LoadPlayerMap`（GameScene.cs:7976-8000；带 `clearObjects` 参数版本 :7978）：`Globals.MapInfoList` 查 Index → `MapView.LoadMap`（MapView.cs:70-90：设标题 + SyncTerrainRows + 首帧诊断）→ `LightLayer.SetMap/SetDayTime` → WeatherLayer → 小地图。`.map` 格式细节见 §7.2。

## 6. 网络层

### 6.1 驱动模型：同步轮询替代异步回调（关键差异）

`LibraryCore/Network/BaseConnection.cs` 保留了原版异步 API（`BeginReceive` :69-77，8KB 缓冲、回调 `ReceiveData` :86 起：收→拼 `_rawData`→`Packet.ReceivePacket` 循环入队→再 BeginReceive），**但 Godot 端从不调用 BeginReceive**——Godot 环境里异步回调可能不触发。替代：`NetworkManager._Process`（`Network/NetworkManager.cs`，103 行）：

- `_Ready` :23 先 `Scripts.DataLayer.LoadAll()` 再 :24 `DatabaseLoader.Load()`（System.db）。
- `_Process` 同步轮询 `TcpClient.Available`（反射取 `BaseConnection.Client` 受保护字段）→ 手动读字节 → `Packet.ReceivePacket` 拼 `_rawData` 半包 → 包入 ReceiveList → 调 `Process()`。
- 连接重建时清 `_rawData` 防旧半包污染新连接。
- 任何异常 → `NotifyDisconnected`。

发送侧沿用 BaseConnection：`Enqueue(Packet)`（BaseConnection.cs:199-204，入 `SendList`）→ `Process()`（:311-394）排空 ReceiveList → 超时检查 → `SendList` 批量 `GetPacketBytes()` 拼一块 → `BeginSend`（分块续发 `BeginSendChunk` :150-167；**TCP 不保证一次发完**，`_sendBuffer/_sendOffset` 注释 :52-54：大 StartGame 包截断会永久停在选人界面）。超时 30s（`ServerConnection.TimeOutDelay` :20）。心跳：`Process(G.Ping)` 回声（:379），`PingResponse` 记 `Ping` 属性（:380）。

### 6.2 ServerConnection：220 个 Process 分发器

反射分发表在 `BaseConnection.PacketMethods`（BaseConnection.cs:16-18，`(ConnectionType, PacketType) → MethodInfo`）。客户端包处理形态三种：

```csharp
public void Process(G.Connected p) { ConnectedEvent?.Invoke(); Enqueue(new G.Connected()); }
public void Process(S.StartGame p) { GD.Print(...); StartGameResultEvent?.Invoke(p.Result, p.StartInformation); }
public void Process(S.ObjectMove p) {
    if (BufferPendingPackets) PendingMoves.Enqueue(p);      // 缓冲期入队
    ObjectMoveEvent?.Invoke(...);                            // 同时发事件
}
```

未订阅处理的包 → `ProcessUnhandledPacket`（ServerConnection.cs:63-67，打印 `[Net] 未处理包: 类型名` + `UnhandledPacket` 事件）。

### 6.3 Pending 缓冲机制（防启动丢包）

问题：订阅生效前到达的 `S.StartGame`/`S.ObjectPlayer` 等世界包会被事件丢弃。
方案：`BufferPendingPackets` 默认 true（:311）。世界包双写（队列+事件），GameScene `_Ready` 末段（:1430-1470）先 `DrainPendingObjects()`（:7533-7568，按服务器顺序 Move/Turn/Player/Monster/NPC/Item/Remove/Attack/Magic 排空）再 `StopPendingPacketBuffering()`。切图时 `ClearPendingWorldPackets()`（:319）反射清空全部 `Pending*` 字段队列。

### 6.4 常用发包 API（GameScene 门面）

`SendSelectLanguage`（GameScene.cs:6218）、`SendMagicToggle`（:444）、`SendMagicKey`（:6591）、攻击链（§7.1）、`SendMouseMove`（:7876-7905）。更多见 ServerConnection.cs 95 个 `public void Send*`。

## 7. 战斗与地图交互

### 7.1 攻击节奏与冷却公式

`ComputeAttackIntervalMs`（GameScene.cs:1503-1509；注释 :1497-1502 引原版 UserObject.cs:638-653）：

```
interval = max(800, AttackDelay - AttackSpeed * ASpeedRate)   // ms
超重(overweight) 或 Neutralize(定身) → ×2
```

`ComputeMiningIntervalMs`（:1515-1522）：超重 ×3 / Neutralize ×2。`CombatController.GetAttackInterval`（`Scripts/CombatController.cs:417`）委托 GameScene 公式，自身仅 800ms 兜底（:33）。

发送链：`CombatController` 判定 → GameScene.cs:1006（action==Attack → `PlayCombat`）→ :1013 `Enqueue(new C.Attack{...})`；远程 :1029 `SendRangeAttack`。实服审计基线：攻击 gap≈1359ms 与公式 1359ms 吻合（Mir3-Research/docs/AGENT_HANDOFF_PARITY_GOAL.md:365）。

魔法：`_pendingMagicPacket`（GameScene.cs:813）——施法包延迟到"动作边界"再发，复刻原版 MagicAction 队列（注释 :1527-1528）。释放逻辑 `_Process` 内（:8137-8147）：`CanPlayerTurn()` 且（当前帧不在移动动画 或 超过走完期限）才 `Enqueue`；断线则打日志丢包。

### 7.2 .map 格式（Formats/MapReader.cs，class MirMap）

实测布局：22 字节头跳过 → `ReadInt16` Width/Height → `Seek(28)` → 背景层（**半分辨率**：只存偶数格）→ 全分辨率每格 14 字节：flag/animation/value/frontFile/middleFile + frontAnimationFrame（255→0 再 &0x8F）+ middle/frontImage 各 **+1**（原版 MapControl 的 +1 存储约定）+ 跳 3 字节 + light 低 4 位 ×2 + 跳 1 字节。`MapCell` struct：BackFile/BackImage/MiddleFile/MiddleImage/FrontFile/FrontImage/MiddleAnimationFrame/FrontAnimationFrame/Light/Flag。

**教训**（AGENTS.md）：沙巴克移植曾因中层/前景索引 `-1/+1` 双重偏移导致墙体缺口——写转换工具前先读原版 `MapControl.cs` 确认约定，且校验工具不得与生产工具共用同一解析逻辑（错误会自洽掩盖）。

### 7.3 移动：预测 + 纠正双路径

- **发送**：`SendMouseMove`（GameScene.cs:7876-7905）原版复刻预测——本地记录 `_playerLocation/_pendingDistance`，不等服务器。
- **接收**：`ShowUserLocation`（:7744-7830）。预判命中（`_playerLocation==(x,y) && _pendingDistance==distance`）只补方向不重插值；纠正路径（服务端≠预判）重跳 `_moveFrom/_moveStartMs` + 反向 Offset + 重启插值。
- **对象移动**：`OnObjectMove`（:2040-2125）玩家分支（离线测试忽略 / 自动寻路待办 / `MouseWalker.AddMoveDelay` + CallDeferred）、其他玩家走 `_otherPlayers`+`_objects` 双字典代理。
- **手感门控**：`MouseWalker`（Scripts/，WalkIntervalMs=RunIntervalMs=600 :66-67、`SuspendUntilInputRelease` :60、`AddMoveDelay` :62、AutoRun :54；CellWidth/CellHeight 常量复制自 MapView :23-25；_sendMove/_sendTurn/_cellBlocked/_awaitingServer/_playerCell 委托注入 :33-55）。移动许可：`CanPlayerTurn/CanPlayerMove/SuspendMovementForMagic`（GameScene.cs:4616-4645）。UI 悬停屏蔽：`IsMouseOverUi`（:4592-4615，GuiGetHoveredControl 上溯到 `_uiLayer` 即算 UI）。
- **动画**：`PlayerRenderer.BeginMove`（:564-578，distance≥2 且非隐身 → Running/HorseRunning；隐身 CreepWalk）、`StartMove` 远端平滑回拉（:581+）、`PlayCombat`（:295）、`SetAnimation`/`ApplyAnimation`（:246/:262）。

### 7.4 输入路由（GameScene）

- `_Input`（:9882-9970）顺序：`_chatTextBox.HandleGlobalKey` → **F12 在 KeyBindManager 之前**（UiOverlay 热重载优先；默认键位 SpellUse12=F12 会被遮蔽，改键位时注意）→ Esc → `WindowManager.CloseTop`（`_escapeCloseAll` 时连关）→ `KeyBindManager.GetAction` → 功能键：M 上下马、D 自动跑（Ctrl/Alt/Shift 排除）、T 交易请求。**未连接时直接 return**。
- `_UnhandledInput`（:9986 起；头段 :9990+）：观察模式拒绝鼠标事件；NPC 拾起点击（MouseUp + `_pendingNpcClickObjectId` 匹配 MouseObject，:9993-9998 注释保留原版 PickUp 重叠场景）；右键先取消 `DXItemCell.SelectedCell`/`_selectedCurrency`；左/右键 CancelAutoPath + Alt 采集取消分支（Fishing/Taming）；货币掉落 ItemAmountDialog。

### 7.5 渲染相关常量

`MapView`（Scripts/MapView.cs）：CellWidth=48 :23、CellHeight=32 :24、WorldScale=2 :25、ViewRangeX=12 :30、ViewRangeY=15 :31、ManualHeightOffset=34 :43、`CellToScreen` :352、`_Draw` :89 起（首帧诊断 :94-97；地面不加格/对象加一格的 Y 基线注释 :99-100）。`GameScene`：UiScale 默认 2f :24、UiScaleBaseHeight 768 :25、WorldScale 2 :26、`_uiLayer` CanvasLayer Layer=10 :1054、`_coverLayer` Layer=100 :1085。

`ZlReader`（Formats/ZlReader.cs，内含 class ZlLibrary）：8 个 ImageTexture 缓存字典；纹理 API `GetEffectTexture` :219 / `GetWeatherTexture` :224 / `GetFogTexture` :232 / `GetShadowTexture` :237 / `GetOverlayTexture` :242 / `GetPartTexture` :281-330 / `ClearAuditEffectTextureCache` :259-268；透明键容忍度 Effect=32 / Weather=96 / Fog=192；支持旧格式 version 0/1 与 ZL2（Deflate 按 entry 索引）。图库实例缓存 `Formats/LibraryCache.cs`（DataPath 相对 res:// 探测 :22）。

## 8. UI 体系与缩放

### 8.1 DXControl 自绘体系

`Controls/DXControl.cs`：平行于 Godot Control 树的**自绘逻辑树**——每个 DXControl 有 `Controls` 子列表、`Location`、`Enabled/IsControl/PassThrough`；静态 `MouseControl`/`FocusControl`（:12-13）跟踪鼠标/焦点；`MessageBoxList`（:18）。绘制由 MirSkin 供贴图，不走 Godot 主题。

`Controls/DXWindow.cs`：窗口基类。Windows 静态表（窗口注册）、TitleHeight=24（拖拽判定用 32px 区域）、`ShowWindow`（无父时 AddChild+Visible+BringToFront）/`Close`；`ApplyResize`（:210-215）内 `viewport / GameScene.UiScale` 换算回逻辑坐标再钳制；`GetAcceptableResize` 子类格子吸附；`ApplyUiOverlay` deferred 钩子（:133-141，`_Ready` 里 `UiOverlay.HasOverrides ? CallDeferred(ApplyUiOverlay)`）。

`Controls/WindowManager.cs`：静态 `OpenWindows` 按打开顺序排 Z，`BaseZ=100`；`BringToFront`/`RefreshZOrder`；`CloseTop`（:43-57）顺带清理不可见残留。**只管 Z 序，不参与布局**（布局在 LayoutHud）。

### 8.2 缩放：UiScaler 公式

`Scripts/UiScaler.cs`：

- `ComputeScale`（:27-37）：`Clamp(Min(viewport.Y/768, viewport.X/1024), 1, 2)` —— 逻辑画布 1024x768，等比取小者，下限 1 上限 2（视口非法时回退 WindowGetSize / 2f）。
- `UpdateScale`（:45-62）：缩放后居中偏移 `(vp - Base*scale)/2` 且 clamp≥0（:57-59，不出现负偏移；登录/选人整幅画布需居中，GameScene HUD 贴边不需要）。
- `ZIRCON_UI_SCALE` 环境变量 force 钩子（:51-56）——**只在 UiScaler 有**。
- `ZIRCON_UI_AUDIT=1` 溢出审计（`AuditOverflow`/`Walk`）。

应用面双轨：

- LoginScene/SelectScene：自建 `CanvasLayer("UiScaleLayer")`（LoginScene.cs:65）。
- GameScene：内置 `_uiLayer`（:1054，Layer=10）+ `RefreshUiScale`（:4505-4520；`GetHudViewportSize` :4488 优先 `GetViewportRect()`）。公式与 UiScaler 一致但**无 ZIRCON_UI_SCALE 钩子**——无头验 4K 只能用 Xvfb 大分辨率或登录场景。

### 8.3 布局：LayoutHud 与审计

`LayoutHud`（GameScene.cs:4704-4730）：窗口按 1024x768 逻辑画布锚定——Center 辅助、主面板贴底居中（按钮位 650,23/923,23）、聊天框贴底、MagicBar 锚定公式。viewport/scale 变化时 `_Process`（:8103-8115）触发重排。

`RunUiLayoutAudit`（GameScene.cs:4530+，flag `--ui-layout-audit`，常量 `UiAuditArgument` :27）：检查 Transform 缩放==UiScale、主面板按钮位置、MouseFilter.Stop。

### 8.4 UiOverlay 热重载（uieditor 联动）

链路：游戏 `--ui-export`（UITestScene.cs:51-54）→ `UiTreeExporter.Run` 导出 `ui_tree.json`（649,587 字节、46 窗口/2398 控件、坐标全按 1024x768 逻辑基准）→ 浏览器 uieditor（:8820，`Mir3-Research/Tools/uieditor/`）拖拽 → 保存 `GodotClient/UI/ui_overlay.json`（只存 diff：`{窗口类名: {控件path: {location:[x,y]}}}`，**不在 git 仓库，缺失零副作用**）→ 游戏内生效。

`Controls/UiOverlay.cs`（238 行）：`Load` :42 / `ReloadAll` :93 / `ApplyAll` :103+ / `HasOverrides` :34 / `OverlayPath` :39 / `LastAppliedCount` :37。`ApplyWindow` 按类名查表 → `ResolveByPath`（路径形如 `"WindowClass/0/3/1"`，首段类名可省，其余为 Controls 子索引链）→ `ApplyProps`（location 等属性 switch）。

调用点（GameScene.cs）：:924（`_Ready` 首段 Load）、:4452（CreateHud 内 **LayoutHud() 之后** ApplyAll——窗口级 location 覆盖不被默认布局冲掉）、:9896（F12 ReloadAll）+ :9897（DumpVisibleWindowRects，定义 :9962）、:296-298（聊天命令 `@uiReload`，本地拦截不发送）。

### 8.5 键位：KeyBindManager

`Controls/KeyBindManager.cs`（类 :105）：枚举 `KeyBindAction`（1-76）；默认表 KeyBinds（N/H/O/Q/W/E/X/S/Z/Ctrl+P/Ctrl+C/Ctrl+F/Ctrl+R/Tab/L/V/B/R/Y/U/P/G/,/. 等）；`Load/Save` 到 `user://ZirconKeyBinds.ini`（ConfigFile，section=Action 名，Key1/Key2+四修饰）；`GetAction` 归一化 Keycode + **双键位+修饰**匹配；`GetKeyBindLabel` 中文可读键名。审计 flag：`--keybind-audit`（断言 557+70 条）。

### 8.6 持久化：ClientSettings

`Scripts/ClientSettings.cs`（**不在 LibraryCore/Data/**）：`FilePath = user://Zircon.ini` :9；`Load()` :135（先 `Lang.Reload()` 再读文件——ini 缺失提前 return 语言也不丢）；`Save()` :222；`ApplyAudioSettings()` :290（BusFor 按音效类别）；`ApplyDisplaySettings()` :314；`Language` 默认 "CHINESE" :62；`UseNetworkConfig` :59 / `IPAddress` 127.0.0.1 :60 / `Port` 7000 :61；`LoadColours` :356 / `SaveColours` :393 / `ReadColour` :430 / `Read<T>` :436 / `ReadVector2I` :446（**320x240 下限旧坑**，:165 注释——小坐标必须用 v2 的 `ReadPoint` :453）/ `Write` :463。

## 9. 翻译与本地化

### 9.1 三层结构

1. **UI 字符串**：`GodotClient/Translations/`——`StringMessages.cs`（键定义）+ `ChineseMessages.cs`/`EnglishMessages.cs`/`JapaneseMessages.cs`（三语实现）。装载：`Lang.Reload`（`Scripts/Lang.cs:20-37`）。
2. **数据层名称**（物品/怪物/NPC/魔法/地图名）：`GodotClient/translations/db_names.json`（小写目录！）——`{"items": {"Gold": {"zh":"Gold","ja":"金貨"}, ...}}` 分 items/monsters/npcs/magics/maps 五段。消费：`Scripts/LocalizedName.cs`（方案 B：不动数据库，客户端查表；`EnsureLoaded` :26-42 FileAccess 读 `res://translations/db_names.json`，失败全空表不崩；`LangCode` 映射 Lang.Current→EN/CN/JA；扩展方法 `ItemInfo.Local()`/`MonsterInfo.Local()`/`NPCInfo.Local()`/`MagicInfo.Local()`/`MapInfo.Local()`/`MirClass.Local()`/`RequiredClass.Local()`/`MirGender.Local()`，查不到回退英文原名）。
3. **服务端通知**：登录后发语言码，服务端按语言发聊天/系统消息。

### 9.2 语言决定顺序

`AutoLoginArgs.Language ?? ClientSettings.Language ?? "CHINESE"`（Lang.cs:23）。语言包随 `ClientSettings.Load()` 首段装载。

### 9.3 游戏内切换

ConfigDialog（`Controls/ConfigDialog.cs`）语言下拉（~:195-215）：SelectedChanged → 写 `ClientSettings.Language`（:209）→ `Save()` → `Lang.Reload()`（:210 注释"UI 文本即时切换"）→ `GameScene.Game?.SendSelectLanguage(...)`（:211）。`SendSelectLanguage` 门面在 GameScene.cs:6218。

### 9.4 协议层语言

`Process(G.GoodVersion)`（ServerConnection.cs:369-373）握手阶段就发 `C.SelectLanguage{Language="Chinese"}`——**先于 VersionOK 事件**。即：连接一建立语言就定了，之后的 SendSelectLanguage 是切换。

## 10. Web 移植线证据

结论（`docs/WEB_PORT_SPIKE_REPORT.md`，2026-08-14 实测）：**C# 同工程导 Web 不可行；GDScript 壳路线可行**。

三道关卡：

1. **Godot 4.6.3 mono Web 导出 ❌**：官方 mono 导出模板（`Godot_v4.6.3-stable_mono_export_templates.tpz`，1.1GB）27 个条目无任何 `web_*.zip`（报告 :23-24）；编辑器对 C#+Web preset 无条件拒绝（`editor/editor_node.cpp:1332`，报告 :26-34）；上游 C# Web 支持仅 Draft PR godot#106125，无发布承诺（:41）。判定：4.6.3 官方工具链产品边界，除非自编译引擎（:43）。
2. **资源瘦身 ✅**：Interface.Zl 4.71MB→lossless WebP 2.05MB；全量 8.0GB→3.7GB（lossless）/~2.1GB（q90）；音频 OGG 化 10.5×（报告 :12）。
3. **WebSocket 网关 ✅**：独立 WS:7001→TCP:7000 透传网关实测通过，登录包被服务器接受（报告 :13）。

推荐路线（报告 :81）：**方案 B「GDScript 渲染壳（Web/桌面同一份）+ C# 逻辑库」**——桌面壳原生导出成熟，`System.db`/`ui_overlay.json`/WebP manifest 双端共用单一数据源。

对本仓库的意义：**ui_tree.json/ui_overlay.json 是引擎无关的**——uieditor（:8820）产出的布局 diff 未来直接喂 Web 壳。这就是 §8.4 整条链的长期价值。

## 11. 常见任务与陷阱

### 11.1 任务速查

| 任务 | 入口 |
|---|---|
| 加/改一个窗口 | 继承 `DXWindow`（Controls/），注册 Windows 表；布局参考同类窗口；uieditor 微调位置 |
| 改键位默认值 | `KeyBindManager` 默认表（Controls/KeyBindManager.cs，KeyBinds） |
| 改 UI 文案 | `Translations/ChineseMessages.cs` 等三语同步改 |
| 加物品/怪物中文名 | `translations/db_names.json` 对应段（工具生成，可热更） |
| 改缩放行为 | `UiScaler.ComputeScale`（登录/选人）与 `GameScene.RefreshUiScale`（游戏内）**两处同步** |
| 新网络包 | 服务端 `ServerLibrary` 加 Packet → 客户端 `ServerConnection` 加 `Process(S.XXX)` + 事件/Pending |
| 跑审计 | §3.2 两张 flag 表；隔离目录优先 |
| 截图验证 | §3.3 Xvfb 配方 |

### 11.2 陷阱清单（历史教训）

1. **注释吞代码**：`SelectScene.cs` 曾把 `if` 和注释写同一行，自动登录整段被注释，`dotnet build` 照样过，客户端停在选人。含注释代码写完扫 `grep -n "//.*if\|//.*{"`。
2. **`--window` 裸参会吞下一个参数**（AutoLoginArgs.GetValue 缺陷），用 `--window=WxH`。
3. **`--quit-after` 放 `--` 后不生效**（引擎参数 vs 用户参数，§3.2）。
4. **F12 早于 KeyBindManager**：默认 SpellUse12=F12 被 UiOverlay 热重载遮蔽（§7.4）。
5. **ZIRCON_UI_SCALE 只作用于 UiScaler**：GameScene.RefreshUiScale 无此钩子（§8.2）；无头验 4K 用 Xvfb 大分辨率。
6. **验证工具不得与生产工具共错**：沙巴克中层/前景 `-1/+1` 双重偏移事故（§7.2）。校验必须独立实现或对照真实游戏。
7. **数据约定先读原版再写工具**：.map 帧索引 `+1`、frontAnimationFrame 255→0 &0x8F 等（§7.2）。
8. **路径反直觉**：MapView/ClientSettings 在 Scripts/、MirSkin/KeyBindManager 在 Controls/（§4.1）。
9. **`ReadVector2I` 有 320x240 下限**：小坐标旧坑，用 `ReadPoint`（§8.6）。
10. **启动顺序**：Pending 缓冲必须在 `DrainPendingObjects()` 之后才关（§6.3）；ui_overlay 的 ApplyAll 必须在 LayoutHud 之后（§8.4）；`UiOverlay.Load` 在 `_Ready` 首段（:924）。
11. **`Config.CheckVersion`**：服务端默认 true 会等 `G.Version` 包，但 Godot 客户端**从不发送**它——本仓库 `Debug/ServerCore/Server.ini:12` 设 False 才握手通过。换新服务必检查。
12. **切图丢对象/复活旧包**：用 `ClearPendingWorldPackets()`（ServerConnection.cs:319），勿手写半套清理。
13. **服务端运行中绝不写 System.db**；双库（服务端+客户端）同步写；写前备份；round-trip 读回验证（AGENTS.md 写库纪律）。
14. **行为验证 ≥ 编译验证**（AGENTS.md 强制规则）：登录/地图/数据转换类改动必须跑真实流程。

### 11.3 服务端口表（AGENTS.md）

7000 ServerCore / 8810 dbeditor / 8820 uieditor / 8822 webclient / 8800 dbviewer / 8899 mapviewer / 8765 wilviewer / 8830 yomu / 8831 fudoki / 80 svc-dashboard / 7001 wsgateway（Web 移植线）。

### 11.4 环境变量速查

| 变量 | 作用 | 生效处 |
|---|---|---|
| `ZIRCON_UI_SCALE` | 强制 UI 缩放 | 仅 UiScaler（登录/选人），GameScene 无 |
| `ZIRCON_UI_AUDIT=1` | UI 溢出审计 | UiScaler.AuditOverflow/Walk |
| `ZIRCON_CLIENT_DATA` | DataLayer 数据目录 | `DataLayer.ResolveClientDataDir`（Scripts/DataLayer.cs:46-67） |
| `MIR3_EI_ROOT` 等 | Mir3-Research 工具脚本 | 见 AGENTS.md |

---

## 自检与交接结论

### 抽查表（20 处引用复核）

| # | 引用 | 原文要点 | 判定 |
|---|---|---|---|
| 1 | ServerConnection.cs:311 | `BufferPendingPackets { get; set; } = true` | ✅ |
| 2 | ServerConnection.cs:319 | `ClearPendingWorldPackets()` 反射清队列 | ✅ |
| 3 | ServerConnection.cs:364-368 | `Process(G.Connected)` 回发 G.Connected | ✅ |
| 4 | ServerConnection.cs:369-373 | GoodVersion → 先发 SelectLanguage "Chinese" | ✅ |
| 5 | ServerConnection.cs:415-419 | `Process(S.StartGame)` + 调试日志 | ✅ |
| 6 | ServerConnection.cs:420-426 | 启动期 MapChanged 不清队列注释 | ✅ |
| 7 | BaseConnection.cs:199-204 | `Enqueue` 入 SendList | ✅ |
| 8 | BaseConnection.cs:311-394 | `Process()` 收→超时→批量发 | ✅ |
| 9 | GameScene.cs:7533-7568 | DrainPendingObjects 排空顺序 | ✅ |
| 10 | GameScene.cs:8137-8147 | _pendingMagicPacket 动作边界释放 | ✅ |
| 11 | GameScene.cs:1503-1509 | ComputeAttackIntervalMs 公式 | ✅ |
| 12 | GameScene.cs:4704-4730 | LayoutHud 锚定 | ✅ |
| 13 | UiScaler.cs:27-37/:51-56 | ComputeScale 公式 / force 钩子 | ✅ |
| 14 | DXWindow.cs:133-141 | deferred ApplyUiOverlay | ✅ |
| 15 | WindowManager.cs:43-57 | CloseTop 清残留 | ✅ |
| 16 | ConfigDialog.cs:209-211 | 语言切换四连（Save/Reload/SendSelectLanguage） | ✅ |
| 17 | LocalizedName.cs:26-42 | db_names.json 五段装载 | ✅ |
| 18 | SConnection.cs:272-274 | 服务端 CheckVersion 分支 | ✅ |
| 19 | Debug/ServerCore/Server.ini:12 | `CheckVersion=False` | ✅ |
| 20 | WEB_PORT_SPIKE_REPORT.md:23-43 | mono 无 web 模板 + 编辑器拒绝 | ✅ |

### 结论

| 维度 | 状态 |
|---|---|
| 可构建可运行 | `dotnet build` + godot-mono 直跑；单机模式零配置 |
| 验证覆盖 | headless 审计 30+ flag 全 PASS 基线 + Xvfb 截图配方 + 实服 GM 验证三层 |
| 文档可信度 | 20/20 抽查复核通过（见上表）；行号以 2026-08-17 为准 |
| 主要风险 | 行号随改动漂移（用前 grep 复核）；Web 线依赖 GDScript 壳路线落地 |

### 未验证项

- `translations/db_names.json` 与 `Translations/` 三语文件的**覆盖率**（哪些键缺失回落英文）未逐一盘点——有工具生成，缺漏以运行时日志 `[Localize]` 为准。
- 远程 82 服务器（192.168.3.82:7000）当前可达性未实测。
- §3.3 Xvfb 配方在本文撰写当日未重跑（引用 AGENTS.md 与 Mir3-Research/docs 既有记录）。
- 46 窗口之外的 `--*-audit` 全家桶（fortune/fishing 等 17 项）只列名未逐个复跑，基线见 Mir3-Research/docs/GODOT_ORIGINAL_MIGRATION_HANDOFF_2026-08-08.md:100-140。

（完）
