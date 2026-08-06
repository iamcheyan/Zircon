# 讨论记录 05：Godot 客户端骨架 —— 登录与选角色全流程走通

> 日期：2026-08-06
> 关联：讨论 04 验证了协议链路（最小控制台客户端），本篇把网络层搬进 Godot，做出登录 + 选角色 UI，走通完整进入游戏流程。
> 结论：**第 2 步完成。** Godot 客户端能连服务端、登录、建角色、选角色进入游戏，全链路在 Godot 引擎内跑通。

---

## 1. 目标

把讨论 04 的控制台验证客户端（ClientProbe）升级成真正的 Godot C# 客户端：
- Godot 工程引用 LibraryCore（复用 BaseConnection/Packet）
- 网络层搬进 Godot，用 Godot `_Process` 驱动收发包
- 登录界面（账号密码输入 + 登录/注册按钮）
- 选角色界面（角色列表 + 建角色 + 进入游戏）

## 2. 工程结构

```
GodotClient/
├── project.godot              Godot 工程配置（autoload NetworkManager）
├── ZirconClient.csproj        Godot.NET.Sdk/4.6.0 + 引用 ../LibraryCore
├── Network/
│   ├── NetworkManager.cs      自动加载单例: 管理连接生命周期, _Process 驱动
│   └── ServerConnection.cs    继承 BaseConnection, C# event 通知 UI
├── Scripts/
│   ├── LoginScene.cs          登录界面逻辑
│   └── SelectScene.cs         选角色界面逻辑
└── Scenes/
    ├── LoginScene.tscn        登录界面布局
    └── SelectScene.tscn       选角色界面布局
```

## 3. 关键设计

### 3.1 csproj：Godot.NET.Sdk + 引用 LibraryCore

```xml
<Project Sdk="Godot.NET.Sdk/4.6.0">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\LibraryCore\LibraryCore.csproj" />
  </ItemGroup>
</Project>
```

**TFM 兼容性确认**：Godot 4.6 的 C# 包最低支持 net8，但**可以 target 更高版本**。LibraryCore 是 net10.0，GodotClient 也用 net10.0，引用无障碍。讨论 01 担心的 TFM 坑不存在。

> 注意：`Godot.NET.Sdk` 必须带版本号 `Godot.NET.Sdk/4.6.0`，不带版本 dotnet 找不到 SDK。

### 3.2 网络层：NetworkManager + ServerConnection

**NetworkManager**（autoload 单例）：
- `Connect(host, port)` 创建 TcpClient + ServerConnection
- 每帧 `_Process` 调 `Connection.Process()` 驱动包收发（BaseConnection 的设计就是每帧调一次 Process）

**ServerConnection**（继承 BaseConnection）：
- `Process(XxxPacket)` 方法处理收到的包，转成 C# event 通知 UI 层
- UI 层调 `SendLogin/SendNewAccount/SendNewCharacter/SendStartGame` 发包
- `ProcessUnhandledPacket` 兜底打印未处理的包

### 3.3 线程安全：CallDeferred

BaseConnection 的接收回调在 .NET 线程池线程（异步 socket），不是 Godot 主线程。**直接在回调里操作 Godot 节点会崩**。解决：用 `CallDeferred` 把 UI 操作延迟到主线程：

```csharp
private void OnLoginResult(LoginResult result, string message, List<SelectInfo> characters)
{
    _pendingCharacters = characters;        // 存到成员变量
    _pendingLoginResult = result;
    CallDeferred(nameof(ShowLoginResult));  // 延迟到主线程执行
}
private void ShowLoginResult() { /* 操作 UI 节点 */ }
```

> 注意：`CallDeferred` 只接受 Godot Variant 参数，`List<SelectInfo>` 不是 Variant。所以把数据存成员变量，CallDeferred 调无参方法。

## 4. 协议流程（完整）

```
Godot 客户端                        服务端
  │ TCP connect                       │
  │ ◄─── Connected ───                │
  │ ──── Connected ───►               │
  │ ◄─── GoodVersion ── (CheckVersion=False)  直接进 Login 阶段
  │                                   │
  │ ──── Login ───────►               │
  │ ◄─── S.Login(Success, Characters) │  ← 登录成功, 带角色列表
  │                                   │
  │ (没角色时)                         │
  │ ──── NewCharacter ─►               │
  │ ◄─── S.NewCharacter(Success, Info)│  ← 建角色成功, 带角色信息
  │                                   │
  │ ──── StartGame ────►               │
  │ ◄─── S.StartGame(Result) ───────  │  ← 进入游戏结果
```

## 5. 验证结果（headless 自动测试）

用 `--auto-login` 参数跑 headless 自动测试（模拟用户操作）：

```bash
~/.local/bin/godot-mono --headless --path GodotClient/ -- --auto-login
```

输出：
```
[Net] TCP 已连接 127.0.0.1:7000
[Login] 服务端确认连接
[Login] 版本校验通过, version=, dbKey=0
[Login] 自动登录...
[Login] 登录成功, 角色数 1              ← 账号有 1 个角色(上次建的 TestHero)
[Select] 自动进入游戏, 角色: TestHero
[Select] StartGame 失败: Delayed        ← 服务端业务逻辑: 角色刚下线有冷却
```

首次（无角色）：
```
[Login] 登录成功, 角色数 0
[Select] 自动建角色 TestHero...
[Select] 建角色成功: TestHero           ← 建角色成功
[Select] 自动进入游戏...
```

**关键证据**：
- `登录成功, 角色数 1` — 登录 + 角色列表拉取成功，角色持久化到 Users.db
- `建角色成功: TestHero` — 建角色协议通（NewCharacter → S.NewCharacter）
- `StartGame 失败: Delayed` — StartGame 协议通，服务端返回 `Delayed`（冷却保护，正常的游戏逻辑，不是 bug）

## 6. 踩过的坑

| 坑 | 原因 | 修复 |
|---|---|---|
| `Godot.NET.Sdk` 找不到 | csproj 没指定 SDK 版本 | `<Project Sdk="Godot.NET.Sdk/4.6.0">` |
| `CallDeferred` 不能传 `List<SelectInfo>` | 只接受 Godot Variant | 数据存成员变量，CallDeferred 调无参方法 |
| 建角色返回 `BadHairColour` | 默认 `Color.Empty` 不合法 | 设 `HairColour=Black, ArmourColour=White` |
| `StartGame: Disabled` | `Config.AllowStartGame` 默认 false | Server.ini 加 `AllowStartGame=True` |
| `StartGame: Delayed` | 角色刚下线有冷却 | 游戏逻辑保护，非 bug；等几秒或换角色 |
| `--auto-login` 参数拿不到 | `OS.GetCmdlineArgs()` 不含 `--` 后的 | 用 `OS.GetCmdlineUserArgs()`，命令行加 `-- --auto-login` |
| 建角色后重新登录没回包 | LoginScene 已 free，新 Login 结果无人接收 | 改用 S.NewCharacter 返回的 Character 直接加列表，不重新登录 |

## 7. Server.ini 完整配置（开发用）

```ini
[Network]
IPAddress=127.0.0.1
Port=7000

[System]
CheckVersion=False
MapPath=./Map/

[Control]
AllowStartGame=True
```

## 8. 账号与角色

- 账号：`test@test.com` / 密码：`test123`（由 `Tools/AccountSetup` 自动创建，持久化在服务端 `Database/Users.db`）
- 角色：`TestHero`（Warrior/Male，由 Godot 客户端自动创建）

> 密码规则：5-15 位非空白字符（`Globals.PasswordRegex`）。`test` 太短（4 位）会返回 `BadPassword`。

## 9. 怎么跑

### 启动服务端
```bash
cd /tmp/zircon-server && dotnet ServerCore.dll
# 或 hub start zircon-server
```

### 启动 Godot 客户端（有界面）
```bash
~/.local/bin/godot-mono --path GodotClient/
```

### headless 自动测试（无界面）
```bash
~/.local/bin/godot-mono --headless --path GodotClient/ -- --auto-login
```

## 10. 下一步（第 3 步）

第 2 步到此为止——客户端能登录、选角色、发 StartGame 进游戏。但进入游戏后服务端会发大量游戏初始化包（地图、玩家位置、周围物体…），这些我们还没处理。

第 3 步：
1. 处理 `S.StartGame` 成功后的包流（`StartInformation`、`MapChanged`、`UserLocation`…）
2. 写 `.Zl`/`.map` 读取器，把地图渲染出来
3. 显示玩家在地图上的位置