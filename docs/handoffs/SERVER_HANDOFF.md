# Zircon 服务端接手文档

> **覆盖范围**：`ServerCore/` 当前无头入口、`ServerLibrary/` 服务端核心、`Server/` 旧 WinForms/MirServer 管理入口，以及 `LibraryCore/` 的服务端侧（MirDB、SystemModels、Network、Stat、Enum）。
>
> **读者假设**：第一次接触 Zircon 的开发者或 AI，只读本篇即可定位源码、构建和启动服务端、理解数据目录、完成常见修改，并知道哪些操作会损坏数据库。
>
> **事实边界**：本文事实来自 2026-08-17 对当前工作树的源码、配置、文档和目录实测。所有代码引用均为相对仓库根的 `路径:行号`。文件数/行数是本次实测值，不直接照搬旧盘点。未运行服务端；启动、登录、游戏内验证均标记为“未验证”。

## 目录

1. [这是什么](#1-这是什么)
2. [怎么跑起来](#2-怎么跑起来)
3. [架构总览](#3-架构总览)
4. [关键文件地图](#4-关键文件地图)
5. [数据层与写库纪律](#5-数据层与写库纪律)
6. [常见修改配方](#6-常见修改配方)
7. [与其他组件的接口](#7-与其他组件的接口)
8. [已知坑](#8-已知坑)
9. [别做什么](#9-别做什么)
10. [延伸资料](#10-延伸资料)
11. [自检与交接结论](#11-自检与交接结论)

---

## 1. 这是什么

### 1.1 服务端定位

Zircon 当前服务端由三个层次组成：

- **`ServerCore/`**：当前实际使用的无头、控制台游戏服入口。它只有 `Program.cs` 一个 C# 文件，负责加载配置、解析命令行、设置加密、调用 `SEnvir.StartServer()`，然后保持进程存活。项目输出目录固定为 `Debug/ServerCore/`（Debug 条件下），见 `ServerCore/ServerCore.csproj:11-13`。
- **`ServerLibrary/`**：真实游戏规则和运行时状态。`SEnvir`、`SConnection`、玩家/怪物/地图/魔法对象、命令、事件、邮件、WebServer、Users.db 模型均在这里。`ServerCore` 只是把它启动起来。
- **`Server/`**：旧的 Windows-only WinForms 管理端/旧 MirServer 入口。它也引用 `ServerLibrary`，但额外提供 DevExpress 管理界面、System.db 编辑视图、地图查看器、插件和诊断功能；不是当前 Linux 无头游戏服务端。

共享层 `LibraryCore/` 同时被服务端、旧管理端、客户端和工具使用。服务端侧真正关心的区域是：

- `LibraryCore/MirDB/`：自定义二进制数据库运行时。
- `LibraryCore/SystemModels/`：System.db 的静态模型。
- `LibraryCore/Network/`：C→S、S→C、通用包及 TCP 基类。
- `LibraryCore/Stat.cs`：`Stats` 容器和 `Stat` 枚举。
- `LibraryCore/Enum.cs`：`MagicType`、`ObjectType`、`GameStage`、`MessageType` 等枚举。

当前服务端不是将 Godot 客户端嵌入进程；Godot 单机模式会由客户端的 `SinglePlayerLauncher` 启动一个独立的 `ServerCore` 进程，服务端仍按正常 TCP 游戏服运行。客户端 autoload 注册和启动逻辑在 `GodotClient/project.godot:19-20`、`GodotClient/Network/SinglePlayerLauncher.cs:27`；本篇只记录接口关系，不修改客户端。

### 1.2 技术栈

三个项目的目标框架来自各自 csproj：

| 项目 | TargetFramework | 输出/用途 | 关键引用 |
|---|---|---|---|
| `ServerCore/ServerCore.csproj` | `net10.0` | Console/Exe；Debug 输出 `Debug/ServerCore/` | `LibraryCore`、`ServerLibrary`、Autofac 8.1.1（`ServerCore/ServerCore.csproj:1-20`） |
| `ServerLibrary/ServerLibrary.csproj` | `net10.0` | 服务端核心类库 | `LibraryCore`（`ServerLibrary/ServerLibrary.csproj`） |
| `LibraryCore/LibraryCore.csproj` | `net10.0` | 共享类库 | 无项目引用（`LibraryCore/LibraryCore.csproj`） |
| `Server/Server.csproj` | `net10.0-windows8.0` | `WinExe`、WinForms 旧管理端 | DevExpress、SharpDX、PluginCore、RenderingCore、ServerLibrary、LibraryCore（`Server/Server.csproj`） |

`Server/` 的 Windows-only 性质不是命名推断：csproj 明确使用 `net10.0-windows8.0`、`UseWindowsForms=true`、DevExpress Win 包；`Server/Program.cs:18-36` 又调用 `Application.Run(new SMain())`。

### 1.3 代码规模：本次实测与旧索引差异

本次使用 `find ... -name '*.cs'` 和 `cat ... | wc -l` 排除 `obj/`、`bin/` 后实测：

| 目录 | `.cs` 文件数 | 总行数 | 说明 |
|---|---:|---:|---|
| `ServerCore/` | 1 | 61 | 当前无头入口 |
| `ServerLibrary/` | 444 | 69,977 | 核心逻辑；`SEnvir`、对象、命令、事件、模型 |
| `Server/` | 102 | 25,611 | 旧 WinForms 管理端 |
| `LibraryCore/` | 63 | 23,243 | 共享协议、数据库、模型、枚举 |

`docs/codebase/_index.md:7-15` 的 2026-08-14 盘点为 `ServerLibrary` 446/70,003、`LibraryCore` 65/23,269；当前实测分别少 2 个和 2 个 C# 文件，行数也有差异。接手时以当前工作树为准，不能把索引中的旧数字当作构建契约。

重要单文件规模：

| 文件 | 本次行数 | 作用 |
|---|---:|---|
| `ServerLibrary/Envir/SEnvir.cs` | 4,601 | 网络监听、数据库、地图/对象集合、主循环、登录、保存、全局服务 |
| `ServerLibrary/Envir/SConnection.cs` | 1,670 | 单客户端状态和全部 `Process(C.Xxx)` 包处理重载 |
| `ServerLibrary/Models/PlayerObject.cs` | 17,915 | 玩家状态、属性、攻击、魔法、背包、社交、NPC、死亡、命令 |
| `ServerLibrary/Models/MonsterObject.cs` | 3,198 | 怪物基类、AI、死亡和掉落；`GetMonster` 还包含 AI 映射 |
| `ServerLibrary/Models/MapObject.cs` | 1,861 | 所有场景对象共同生命周期、buff/毒、可见性和受击入口 |
| `ServerLibrary/Models/MagicObject.cs` | 431 | 魔法运行时基类和技能钩子 |
| `LibraryCore/Enum.cs` | 3,439 | 所有共享枚举；`MagicType` 从 `LibraryCore/Enum.cs:628` 开始 |
| `LibraryCore/Stat.cs` | 931 | `Stats` 容器和 `Stat` 枚举；`Stats` 在 `LibraryCore/Stat.cs:10`，`Stat` 在 `:507` |

### 1.4 三个入口的关系和现状

#### 当前入口：ServerCore

`ServerCore/Program.cs:11-16` 的 `Main` 先加载 `Config`，随后在 `:19-24` 解析 `--singleplayer-dev`，在 `:27-39` 处理加密，在 `:41-42` 打开控制台日志并启动 `SEnvir`。入口不创建地图、不处理包、不直接加载 DB；这些工作由 `SEnvir.EnvirLoop()` 完成。

#### 旧入口：Server/

`Server/Program.cs:19-36` 的流程是配置加载、WinForms 初始化、DevExpress 皮肤，然后 `Application.Run(new SMain())`。`SMain_Load` 在 `Server/SMain.cs:79-118` 中创建 `Session(SessionMode.System)`、用 LibraryCore 和 ServerLibrary 两个程序集初始化数据库模型、加载管理界面。点击 Start 按钮时 `Server/SMain.cs:245-257` 调用同一个 `SEnvir.StartServer()`；停止按钮在 `:259-262` 将 `SEnvir.Started=false`。

因此两者共享 `ServerLibrary` 规则，但职责不同：

```text
ServerCore/Program.cs                 Server/Program.cs
        │                                      │
        │                                      └─ WinForms + DevExpress + Views/
        │                                         System.db 管理/地图查看/诊断/插件
        │
        └─ 无头 Console                         两者都调用
           --singleplayer-dev                       │
                                                   ▼
                                           ServerLibrary/SEnvir
                                           ServerLibrary/Models
                                           LibraryCore/MirDB + Network
```

`Server/` 不是 `ServerCore/` 的“旧版本 DLL”；它是同一解决方案中的独立 Windows 管理端。不要为了修当前无头服而改动 `Server/`，除非修改确实针对管理端或 DB 编辑功能。

#### 单机开发入口：--singleplayer-dev

Godot 客户端不是把服务端逻辑链接进 Godot，而是在 `7000` 无监听时拉起独立 ServerCore。服务端只识别一个命令行开关：

1. `ServerCore/Program.cs:19-24` 检测 `--singleplayer-dev`。
2. 设置 `Config.SinglePlayerDev=true`，并将 `Config.MaxLevel` 至少提升到 `DevSinglePlayer.DevLevel`。
3. `DevSinglePlayer.TargetEmail` 是 `test@test.com`，`DevLevel` 是 255（`ServerLibrary/Envir/DevSinglePlayer.cs:21-25`）。
4. 玩家构造完成、`SetupMagic()` 之后，`PlayerObject` 在 `ServerLibrary/Models/PlayerObject.cs:241-245` 调 `DevSinglePlayer.Apply(this)`。
5. `DevSinglePlayer.ApplyCore` 在 `ServerLibrary/Envir/DevSinglePlayer.cs:48-108` 设置等级、全技能、全装备/物品和 100,000,000 金币。

这是开发便利开关，不是生产服权限开关。它会改变 `Users.db` 中 `TestHero` 的持久化状态；`docs/handoffs/GOAL_GODOT_CLIENT_HANDOFF.md:35-37` 也明确提醒会持久化满级数据。当前运行目录中确实存在 `Debug/ServerCore/Database/Users.db.empty-backup-0809-1036`，恢复前必须停服并确认备份来源。

---

## 2. 怎么跑起来

### 2.1 构建

从仓库根目录执行：

```bash
cd /home/tetsuya/development/zircon
dotnet restore ServerCore/ServerCore.csproj
dotnet build ServerCore/ServerCore.csproj -c Debug
```

部署文档也记录了 `dotnet build ServerCore/ServerCore.csproj -c Debug` 和随后重启 `zircon-server` 的流程（`docs/REMOTE_SERVER_AND_CLIENT_SETUP.md:152-160`）。`ServerCore.csproj:11-13` 的 Debug `OutputPath` 是 `..\..\Debug\ServerCore\`，所以期望产物包括：

```text
Debug/ServerCore/ServerCore.dll
Debug/ServerCore/ServerLibrary.dll
Debug/ServerCore/LibraryCore.dll
Debug/ServerCore/ServerCore.runtimeconfig.json
Debug/ServerCore/Server.ini
Debug/ServerCore/Database/
Debug/ServerCore/Map/
Debug/ServerCore/Translations/
```

不要仅构建 `ServerCore` 后把 DLL 运行目录之外的旧 DLL 当成已更新。systemd 运行的是部署目录中的 DLL，`docs/REMOTE_SERVER_AND_CLIENT_SETUP.md:245-250` 明确提醒“修改源码后直接重启”不会更新 `Debug/ServerCore/ServerCore.dll`。

### 2.2 启动

正确的运行目录是 `Debug/ServerCore`：

```bash
cd /home/tetsuya/development/zircon/Debug/ServerCore
dotnet ServerCore.dll
```

等价地，生产部署应把工作目录固定为 `/home/tetsuya/development/Debug/ServerCore`；部署文档在 `docs/REMOTE_SERVER_AND_CLIENT_SETUP.md:89-92` 说明了这一点。本文未启动服务端，以上是源码、运行目录和部署文档一致的启动命令，**启动行为未验证**。

`Config` 和 MirDB 都以程序集基准目录解析其主要配置/数据库路径：

- `Config` 标记为 `[ConfigPath("./Server.ini")]`，见 `ServerLibrary/Envir/Config.cs:8-10`。
- `ConfigReader` 将配置路径合并到 `AppDomain.CurrentDomain.BaseDirectory`，见 `LibraryCore/ConfigReader.cs:66-70`。
- MirDB `Session` 默认 root 是 `.@Database\\`，随后基于 `AppDomain.CurrentDomain.BaseDirectory` 解析，见 `LibraryCore/MirDB/Session.cs:25-28`、`:82-88`。
- 但地图和经验列表仍有相对当前工作目录的使用点：`SEnvir.LoadExperienceList` 直接访问 `./Config/ExperienceList.txt`（`ServerLibrary/Envir/SEnvir.cs:398-434`）；`Map.Load` 直接 `Path.Combine(Config.MapPath, ...)`（`ServerLibrary/Models/Map.cs:59-63`）。因此工作目录仍必须固定为 `Debug/ServerCore`。

### 2.3 端口和关键配置

默认配置定义在 `ServerLibrary/Envir/Config.cs`：

| 配置项 | 默认/当前含义 | 引用 |
|---|---|---|
| `Network:IPAddress` | `127.0.0.1` | `Config.cs:11-13` |
| `Network:Port` | `7000`，玩家游戏 TCP 端口 | `Config.cs:12-14`；监听创建 `SEnvir.cs:90-92` |
| `Network:UserCountPort` | `3000`，旧用户数探针 | `Config.cs:16`；监听创建 `SEnvir.cs:94-96` |
| `Network:TimeOut` | 20 秒 | `Config.cs:14` |
| `Network:PingDelay` | 2 秒 | `Config.cs:15` |
| `System:CheckVersion` | 默认 true；当前 `Server.ini` 实测为 False | `Config.cs:21-24` |
| `System:MapPath` | 代码默认 `Debug/ServerCore/Map/`；部署 `Server.ini` 应为 `Map/` | `Config.cs:25`；部署文档 `REMOTE_SERVER_AND_CLIENT_SETUP.md:127-128` |
| `System:LazyLoadMaps` | 默认 true；只创建起始区，之后按需加载 | `Config.cs:31-37`；`SEnvir.cs:743-774` |
| `System:DBSaveDelay` | 5 分钟 | `Config.cs:37` |
| `System:EncryptionEnabled` | 默认 false | `Config.cs:38-39` |
| `Control:AllowLogin` | 允许登录 | `Config.cs:41-43` |
| `Control:AllowStartGame` | 需在有效运行配置中打开；当前 `Server.ini` 实测为 True | `Config.cs:57-60` |
| `Players:MaxLevel` | 代码默认 10；单机开发模式至少提升到 255 | `Config.cs:103-106` |
| `Players:MaxViewRange` | 18 格 | `Config.cs:95-100` |
| `WebServer:EnableWebServer` | 默认 false；当前不应无意开启 80 端口 | `Config.cs:75-78` |
| `Rates:*` | 经验、掉落、金币、技能、宠物倍率 | `Config.cs:151-156` |

当前 `Debug/ServerCore/Server.ini` 是 UTF-16 little-endian（本次 `file` 实测），不是 UTF-8。配置键值实测包括 `Port=7000`、`MapPath=Map/`、`CheckVersion=False`、`AllowStartGame=True`。编辑时保留原编码；GB18030 是 NAS/Mud3 文本配置的另一类编码坑，见 [8.7](#87-文本编码)。

`SEnvir.StartNetwork()` 启动两个 TCP listener：游戏服 `IPAddress:Port` 和用户数探针 `IPAddress:UserCountPort`；用户数端口返回 ASCII 格式 `c;/Zircon/{count}/`，实现见 `ServerLibrary/Envir/SEnvir.cs:84-106`、`:180-197`。

### 2.4 测试账号和 GM

仓库工作约定提供的本地测试账号：

```text
邮箱：test@test.com
密码：test123
角色：TestHero
```

`AGENTS.md` 说明该账号 `Admin=True`、永久 GM；本文没有通过登录流程验证数据库状态。代码侧的权限链是可验证的：

- 普通账号登录走 `SEnvir.Login` 的邮箱/密码校验；非邮箱格式且密码等于 `Config.MasterPassword` 时进入临时管理登录分支，见 `ServerLibrary/Envir/SEnvir.cs:3262-3271`。
- 登录成功后 `account.TempAdmin = admin`，并在成功包中将 `IsGM = account.Admin || admin`，见 `SEnvir.cs:3387-3408`。
- `AdminCommandHandler` 只允许 `Account.Admin || Account.TempAdmin`，见 `ServerLibrary/Envir/Commands/AdminCommandHandler.cs:7-12`。

不要在文档、日志截图或提交中泄露 `MasterPassword`、SMTP 密码、同步密钥或其他秘密值；`Server.ini` 的秘密项只写键名和作用。

### 2.5 单机开发模式

客户端自动拉起服务端的关系：

```text
GodotClient/LoginScene
      │ 7000 无监听时调用 SinglePlayerLauncher
      ▼
独立 ServerCore.dll --singleplayer-dev
      │
      ├─ Config.SinglePlayerDev = true
      ├─ MaxLevel >= 255
      └─ TestHero 构造后注入全技能/物品/金币
```

服务端侧检查链：

```csharp
// ServerCore/Program.cs:19-24 的语义
--singleplayer-dev
    => Config.SinglePlayerDev = true
    => Config.MaxLevel = Math.Max(Config.MaxLevel, DevSinglePlayer.DevLevel)
```

`DevSinglePlayer.Apply` 首先检查开关、账号邮箱和角色，再调用 `ApplyCore`；已经达到 255 级的角色直接跳过（`ServerLibrary/Envir/DevSinglePlayer.cs:27-46`、`:48-57`）。注入内容来自当前 System.db：遍历 `MagicInfoList` 学会所有非 `MagicSchool.None` 技能并拉满 `Globals.MagicMaxLevel`，遍历可用 `ItemInfo` 创建绑定物品，最后把金币设为 100,000,000，见 `DevSinglePlayer.cs:67-107`。

验证单机模式时必须注意：

1. 停止现有 `7000` 服务，避免客户端连接到错误实例。
2. 备份 `Debug/ServerCore/Database/Users.db`。
3. 退出客户端后确认 `ServerCore` 子进程已经结束；客户端只应杀掉它自己拉起的进程。
4. 若只想恢复普通测试角色，从 `Users.db` 备份恢复，不能通过删除某个角色字段“猜修”。

### 2.6 控制台循环和退出

`ServerCore/Program.cs:44-54` 注册配置保存后的主循环：

```csharp
while (SEnvir.EnvirThread != null)
{
    var command = Console.ReadLine();
}
```

当前实现读取了 `command` 但没有分发逻辑。也就是说，**ServerCore 当前没有可用的交互式控制台命令解析器**；控制台循环的实际作用是保持主进程等待，不能把它误写成“支持 `@move` 的服务端控制台”。GM 命令在游戏聊天包中处理，不在这里处理。

Ctrl+C 走 `Console.CancelKeyPress`，将 `SEnvir.Started=false`，见 `ServerCore/Program.cs:56-59`。`SEnvir.EnvirLoop` 退出循环后停止 Web/网络、等待存档，再执行最后一次保存，见 `ServerLibrary/Envir/SEnvir.cs:1633-1643`。

---

## 3. 架构总览

### 3.1 模块图

```text
                         ┌──────────────────────────────┐
                         │ ServerCore/Program.cs         │
                         │ 配置 / 加密 / --singleplayer │
                         └──────────────┬───────────────┘
                                        │ SEnvir.StartServer()
                                        ▼
┌─────────────────────────────────────────────────────────────────┐
│ ServerLibrary/Envir/SEnvir.cs                                   │
│ StartEnvir → LoadDatabase → CreateMagic → StartNetwork          │
│ EnvirLoop：连接、玩家、对象时间片、地图、刷新、存档、事件      │
└──────────┬───────────────────┬───────────────────┬──────────────┘
           │                   │                   │
           ▼                   ▼                   ▼
┌────────────────┐   ┌────────────────────┐  ┌──────────────────┐
│ SConnection     │   │ MapObject 图对象    │  │ MirDB Session     │
│ BaseConnection  │   │ ├─PlayerObject      │  │ System.db         │
│ C/S 包处理      │   │ ├─MonsterObject     │  │ Users.db          │
└───────┬────────┘   │ ├─MagicObject/Spell │  └────────┬─────────┘
        │            │ ├─NPCObject/Item     │           │
        ▼            │ └─Map/SpawnInfo       │           ▼
LibraryCore/Network  └──────────┬───────────┘  SystemModels + DBModels
C/S/General/Packet                │
                                 ▼
         ┌──────────────────────────────────────────────────────┐
         │ 规则目录                                             │
         │ Magics/Warrior Wizard Taoist Assassin                │
         │ Monsters/ 101 个特殊怪物类                           │
         │ Commands/Admin + Player                              │
         │ Events/Triggers + Actions                             │
         │ WebServer / EmailService / Translations              │
         └──────────────────────────────────────────────────────┘
```

`SEnvir` 的全局集合包括 `Connections`、`Players`、`Objects`、`ActiveObjects`、`Spawns`、`Maps`、`Instances`，定义集中在 `ServerLibrary/Envir/SEnvir.cs:326-341`。对象 ID 是进程内自增的 `uint`，实现是 `SEnvir.cs:330-331`。

### 3.2 SEnvir 启动和主循环

`SEnvir.StartServer()` 只创建后台 `EnvirThread` 并在线程中运行 `EnvirLoop`，见 `ServerLibrary/Envir/SEnvir.cs:390-396`。真正启动顺序在 `EnvirLoop`：

1. `StartEnvir()`：加载 System/Users DB，读取经验列表，建立副本槽位；懒加载开启时只创建起始区，关闭时加载全部地图并并行加载地图文件，见 `SEnvir.cs:731-774`。
2. `StartNetwork()`：创建 `7000` 游戏监听和 `3000` 用户数监听。
3. `WebServer.StartWebServer()`：只有 `Config.EnableWebServer` 为 true 才会绑定 Web 端口。
4. 设置 `Started = NetworkStarted`，启动日志线程。
5. `while (Started)` 中持续处理连接和对象。

主循环的节奏不是“每秒只跑一次”：

- 每轮将 `NewConnections` 移入 `Connections`，见 `SEnvir.cs:1405-1414`。
- 每轮调用 `connection.Process()`，见 `:1419-1428`。
- 每轮调用玩家 `StartProcess()`，见 `:1434-1435`。
- 对 `ActiveObjects` 使用约 1ms 的时间片分批处理非玩家对象；异常对象被移出活跃列表并写 `Errors.txt`，见 `:1448-1478`。
- 每秒阶段保存累计统计、处理每 5 分钟用户数提示、每分钟事件计时器、昼夜光照、攻城战、地图 `Process`、副本过期、刷新、Web 队列和游戏币，见 `:1480-1586`。
- 跨日阶段重置行会日贡献并触发 GC；城堡开始时间到达时启动攻城，见 `:1588-1613`。
- 主循环外层异常会断开全部连接、记录错误并结束服务环境，见 `:1616-1630`。

所有服务端时间应优先使用 `SEnvir.Now`/`Library.Time.Now`，而不是在业务中直接取墙钟。`LibraryCore/Time.cs:6-12` 说明 `Time.Now` 是启动时 UTC 加 `Stopwatch.Elapsed` 的单调时间。

### 3.3 数据库加载和内存表

`LoadDatabase` 创建 `Session(SessionMode.Users)`，设置备份延迟 60 秒，然后必须把 **LibraryCore 和 ServerLibrary 两个程序集** 传给 `Session.Initialize`：

```csharp
Session.Initialize(
    Assembly.GetAssembly(typeof(ItemInfo)),    // LibraryCore
    Assembly.GetAssembly(typeof(AccountInfo))  // ServerLibrary
);
```

源码位置是 `ServerLibrary/Envir/SEnvir.cs:436-448`。随后从同一 Session 获取 `MapInfoList`、`ItemInfoList`、`RespawnInfoList`、`MagicInfoList`、`MonsterInfoList`、`AccountInfoList`、`CharacterInfoList`、`UserItemList`、`UserMagicList`、行会/任务/宠物/商店/里程碑等集合，见 `SEnvir.cs:450-517`。

加载后还会：

- 找到金币、碎片、强化石等特殊 `ItemInfo`，见 `SEnvir.cs:519-530`。
- 找不到 Starter Guild 时创建并设置名称，见 `:531-540`。
- 清理无 QuestInfo/Task 关联的用户任务，见 `:554-560`。
- 缓存带掉落的 Boss，见 `:562-568`。
- 通过反射收集魔法运行时类，见 `:1244-1255`。

### 3.4 连接和包处理

`SConnection` 是 `BaseConnection` 的服务端子类，保存连接阶段、账号、玩家、观察者、语言和市场搜索结果，字段位于 `ServerLibrary/Envir/SConnection.cs:19-41`。构造函数在 `:43-64`：记录远端 IP 和 SessionID，默认英文消息，注册异常日志，开始异步接收，并发送 `GeneralPackets.Connected`。

底层收包链：

```text
TcpClient.BeginReceive
  → BaseConnection.ReceiveData
  → Packet.ReceivePacket
  → ReceiveList.Enqueue
  → SEnvir.EnvirLoop 中 connection.Process()
  → BaseConnection.Process() 逐包取出
  → ProcessPacket() 反射缓存 Process(PacketType)
  → SConnection.Process(C.Xxx)
```

对应源码：`LibraryCore/Network/BaseConnection.cs:69-113`、`:311-319`、`:396-445`。`ProcessPacket` 不是大 switch，而是按连接类和包类型查找/缓存同名 `Process` 方法：`BaseConnection.cs:396-413`。

`SConnection` 的 `Process(C.Xxx)` 方法按连接阶段过滤。例如：

- 语言选择：`SConnection.cs:253-265`。
- 登录：`SConnection.cs:350-355`，最终调用 `SEnvir.Login`。
- 选人进游戏：`SConnection.cs:398-404`，最终调用 `SEnvir.StartGame`。
- 移动：`SConnection.cs:427-440`。
- 攻击：`SConnection.cs:501-509`。
- 魔法：`SConnection.cs:518-525`。
- 聊天：`SConnection.cs:599-608`。
- NPC 调用：`SConnection.cs:610-619`。

协议包类型和序号由 `LibraryCore/Network/Packet.cs` 的静态初始化反射生成：先收集所有直接继承 `Packet` 的类型，再把 `GeneralPackets` 排在前面，其余按命名空间/名称排序，见 `Packet.cs:23-48`。字段按 `BinaryReader/BinaryWriter` 表处理，见 `Packet.cs:52-115`。因此新增、删除或重命名包可能改变全部后续包 ID；不要手算。

### 3.5 对象层级

```text
MapObject
├── PlayerObject              玩家，17,915 行
├── MonsterObject             怪物基类，3,198 行
│   └── Models/Monsters/      101 个特殊怪物类
├── NPCObject                 NPC 对话和交易对象
├── ItemObject                地面掉落/可拾取对象
├── SpellObject               延迟法术/区域效果
└── MagicObject               玩家技能运行时对象（不是 MapObject 子类）
```

`MapObject` 提供所有对象的共同生命周期：

- 可移动/攻击/施法条件：`MapObject.cs:86-88`。
- 每轮 Process、HP/MP、毒素、buff：`MapObject.cs:156-413`。
- 激活/取消激活和可见对象管理：`MapObject.cs:1005-1057`。
- Buff 增加、受击、死亡：`MapObject.cs:1403-1667`。
- 客户端信息包和数据包是抽象方法：`MapObject.cs:1686-1687`。

`PlayerObject` 构造时恢复背包/装备/buff，给管理员设置 GM/Observer/Superman 初始状态，装配魔法，然后调用单机注入，见 `PlayerObject.cs:186-245`。`SetupMagic` 按 `MagicTypeAttribute` 找到具体技能类并用 `Activator.CreateInstance` 创建，见 `PlayerObject.cs:247-279`。

`MonsterObject.GetMonster` 是 AI 编号到运行时类的工厂；它在 `MonsterObject.cs:122-647` 用 `MonsterInfo.AI` switch 返回 `Guard`、`GhostSorcerer`、BOSS 和其他特殊类。`SpawnInfo.DoSpawn` 在 `Map.cs:429-449` 调用这个工厂并执行 Spawn。

怪物 AI 的入口分为 `ProcessAI`、`ProcessSearch`、`ProcessRoam`、`ProcessTarget`，分别在 `MonsterObject.cs:965`、`:1053`、`:1164`、`:1243`。死亡时先触发 `MONSTERDIE`，再处理 SpawnInfo 计数并可能触发 `MONSTERCLEAR`，见 `MonsterObject.cs:2510-2545`。

### 3.6 魔法、怪物、命令、事件和附属服务

#### Magics

`ServerLibrary/Models/Magics/` 目前有四个职业目录：

| 目录 | 文件数 | 行数 |
|---|---:|---:|
| `Warrior/` | 37 | 2,573 |
| `Wizard/` | 46 | 3,757 |
| `Taoist/` | 51 | 4,015 |
| `Assassin/` | 56 | 3,691 |

每个技能类都直接继承 `MagicObject`，并用 `[MagicType(MagicType.Xxx)]` 标识。反射注册只接受 `BaseType == typeof(MagicObject)`，见 `SEnvir.cs:1248-1252`；不要把新技能做成二级继承后再期待自动注册。

`MagicObject` 的公共扩展点包括 `MagicCast`、`MagicComplete`、`AttackCast`、`AttackLocationSuccess`、`SecondaryAttackLocation`、`ModifyPowerAdditionner`、`ModifyPowerMultiplier`、`GetPassiveStats`，见 `ServerLibrary/Models/MagicObject.cs:97-237`。`MagicTypeAttribute` 在 `:422-430`。

`HalfMoon` 是简单样例：声明技能类型和攻击技能属性在 `ServerLibrary/Models/Magics/Warrior/HalfMoon.cs:9-15`，消耗 MP 并返回 `AttackCast` 在 `:32-51`，二次攻击和伤害修正位于 `:53-66`。

#### Commands

游戏聊天文本以 `@` 开头时，`PlayerObject.Chat` 去掉 `@`、按空格切分，然后调用 `SEnvir.CommandHandler.Handle`，见 `ServerLibrary/Models/PlayerObject.cs:1786-1794`。

命令处理器构造时反射扫描程序集，找到 `AbstractCommand<T>` 或 `AbstractParameterizedCommand<T>` 子类并实例化，见 `ServerLibrary/Envir/Commands/Handler/AbstractCommandHandler.cs:17-25`；`Handle` 在 `:37-53` 按 `VALUE` 查找并执行。

`SEnvir.CommandHandler` 是错误处理器包住玩家命令和管理员命令，见 `ServerLibrary/Envir/SEnvir.cs:229-232`。Admin 权限由 `AdminCommandHandler.cs:9-12` 判定。管理员命令位于 `Commands/Command/Admin/`，普通玩家命令位于 `Commands/Command/Player/`。

#### Events

事件不是名为 `MapEvent` 的类；本仓库全局没有该实现。实际体系是 `EventInfoHandler` + System.db 中的事件模型：

- 构造时反射注册 Trigger 和 Action：`ServerLibrary/Envir/Events/EventInfoHandler.cs:26-75`。
- `Process(string eventName)` 根据字符串、DB 触发器、当前 EventLog 和最大次数执行动作，入口从 `EventInfoHandler.cs:109-171` 开始。
- 触发名包括 `PLAYERDIE`、`MONSTERDIE`、`PLAYERCOMMAND`、`MONSTERCLEAR`、`PLAYERMOVEMAP`、`PLAYERMOVEREGION`、`TIMERMINUTE`、`TIMEOFDAY`。
- `TIMEOFDAY` 从 `SEnvir.TimeOfDay` setter 触发，见 `SEnvir.cs:352-364`；`TIMERMINUTE` 在主循环每分钟触发，见 `SEnvir.cs:1527-1537`；怪物死亡两个触发点见 `MonsterObject.cs:2532-2540`。

Triggers/ 和 Actions/ 各自通过属性注册，不要手写一个没有属性的类后期待它可用。

#### WebServer

WebServer 默认关闭；`StartWebServer` 首先检查 `Config.EnableWebServer`，见 `ServerLibrary/Envir/WebServer.cs:63-66`。开启后创建三个 `HttpListener`：

- `Config.WebPrefix`：激活、密码、删号、SystemDB 同步命令。
- `Config.BuyPrefix`：游戏币购买。
- `Config.IPNPrefix`：支付 IPN。

监听器创建和启动在 `WebServer.cs:69-93`；默认前缀在 `Config.cs:75-91`，均是 80 端口。服务端主循环在 `SEnvir.cs:1381` 启动、`:1578-1584` 消费队列、`:1633` 停止。

#### EmailService

`EmailService` 是基于 `SmtpClient` 的静态邮件服务。激活邮件从 `ServerLibrary/Envir/EmailService.cs:14-62` 开始，使用 `Config.MailServer`、`MailPort`、SSL、账号和密码，并用 `Task.Run` 异步发送。其余方法分别处理重发激活、改密通知、密码重置请求和新密码，调用点集中在 `SEnvir.Login/NewAccount/ChangePassword/ResetPassword/Activation`。

#### Translations

服务端消息实现为：

```text
ServerLibrary/Envir/Translations/StringMessages.cs   抽象消息基类，364 行
ServerLibrary/Envir/Translations/EnglishMessages.cs  英文，342 行
ServerLibrary/Envir/Translations/ChineseMessages.cs  中文，354 行
```

配置目录运行时是 `Debug/ServerCore/Translations/EnglishMessages.ini` 和 `ChineseMessages.ini`。连接的 `SelectLanguage` 包会切换 `SConnection.Language`。

### 3.7 一个攻击包从收到到结算

以下是物理攻击主路径；具体伤害公式不要在本篇重写，直接看 [zdocs](#101-zdocs-代码深读文档)。

```text
1. TCP 字节
   │
   ▼
2. BaseConnection.BeginReceive/ReceiveData
   Packet.ReceivePacket → ReceiveList
   │ LibraryCore/Network/BaseConnection.cs:69-113
   ▼
3. SEnvir.EnvirLoop 每轮 connection.Process()
   │ ServerLibrary/Envir/SEnvir.cs:1419-1428
   ▼
4. BaseConnection.ProcessPacket
   反射缓存 Process(C.Attack)
   │ LibraryCore/Network/BaseConnection.cs:396-413
   ▼
5. SConnection.Process(C.Attack)
   阶段必须为 Game，调用 Player.Attack(p.Direction, p.AttackMagic)
   │ ServerLibrary/Envir/SConnection.cs:501-509
   ▼
6. PlayerObject.Attack
   ActionTime/AttackTime 门控；解析启用的攻击技能；计算攻击延迟
   │ ServerLibrary/Models/PlayerObject.cs:14714-14786
   ▼
7. AttackLocation
   从当前坐标和方向找到攻击位置/目标
   │ PlayerObject.cs:15091-15093
   ▼
8. Attack(MapObject,...)
   对目标执行物理攻击结算
   │ PlayerObject.cs:15205-15207
   ▼
9. 目标 Attacked / 受击状态
   MapObject 提供受击抽象入口；PlayerObject 有具体 override
   │ MapObject.cs:1563；PlayerObject.cs:15678
   ▼
10. 目标死亡时
    MonsterObject.Die → MONSTERDIE → SpawnInfo 计数/MONSTERCLEAR
    │ MonsterObject.cs:2510-2545
   ▼
11. 掉落
    MonsterObject.Drop 遍历 MonsterInfo.Drops，处理倍率、概率、Fortune、金币分摊、掉落物
    │ MonsterObject.cs:2691-2783 及后续
   ▼
12. 广播攻击表现
    PlayerObject 广播 S.ObjectAttack
    │ PlayerObject.cs:14805-14813
```

攻击技能 `AttackCast` 的资源消耗、被动技能和二次攻击发生在 `PlayerObject.Attack` 的 `MagicObjects.OrderedKeys` 遍历中，见 `PlayerObject.cs:14758-14803`。攻击速度使用 `Globals.AttackDelay`、`Globals.ASpeedRate`，定义在 `LibraryCore/Globals.cs:304-313`。

---

## 4. 关键文件地图

### 4.1 ServerCore/

| 文件 | 行数 | 职责 |
|---|---:|---|
| `ServerCore/Program.cs` | 61 | 配置、加密、单机开关、启动 `SEnvir`、Ctrl+C 和进程等待 |
| `ServerCore/ServerCore.csproj` | 20 | net10.0、Debug 输出、LibraryCore/ServerLibrary 引用 |

### 4.2 ServerLibrary/Envir/

| 文件 | 行数 | 职责 |
|---|---:|---|
| `SEnvir.cs` | 4,601 | 全局状态、监听、MirDB 集合、主循环、存档、登录和地图 |
| `SConnection.cs` | 1,670 | 单连接阶段、心跳、C→S 包处理重载 |
| `Config.cs` | 185 | `[ConfigPath("./Server.ini")]` 和全部运行配置 |
| `DevSinglePlayer.cs` | 109 | `--singleplayer-dev` 的测试角色注入 |
| `WebServer.cs` | 499 | 三个 HttpListener、Web 命令、购买/IPN |
| `EmailService.cs` | 245 | 激活、改密、密码重置 SMTP 邮件 |
| `Translations/StringMessages.cs` | 364 | 多语言配置属性基类 |
| `Translations/EnglishMessages.cs` | 342 | 英文消息实现 |
| `Translations/ChineseMessages.cs` | 354 | 中文消息实现 |

本次实测 `Envir/` 顶层 6 个 C# 文件共 7,309 行；Translations 3 个文件共 1,060 行。

### 4.3 ServerLibrary/Models/

| 文件 | 行数 | 职责 |
|---|---:|---|
| `PlayerObject.cs` | 17,915 | 玩家全量运行时逻辑 |
| `MonsterObject.cs` | 3,198 | 怪物 AI、死亡、掉落、AI 工厂 |
| `MapObject.cs` | 1,861 | 对象基类、生命周期、buff/毒、受击 |
| `NPCObject.cs` | 796 | NPC 可见性、对话调用入口 |
| `Map.cs` | 735 | 地图载入、cell、广播、`SpawnInfo` |
| `MagicObject.cs` | 431 | 技能运行时基类 |
| `SpellObject.cs` | 341 | 延迟法术/区域效果 |
| `PlayerObject.Milestone.cs` | 317 | 玩家里程碑逻辑分部 |
| `ConquestWar.cs` | 199 | 城堡/攻城战运行时对象 |
| `ItemObject.cs` | 217 | 地面掉落和拾取；`ItemObject.cs:11-21` |
| `ItemCheck.cs` | 40 | 物品检查参数 |
| `DelayedAction.cs` | 40 | 延迟玩家动作 |

### 4.4 ServerLibrary/Models/Magics/

| 目录 | 文件数 | 行数 | 修改入口 |
|---|---:|---:|---|
| `Warrior/` | 37 | 2,573 | 战士 `MagicObject` 子类 |
| `Wizard/` | 46 | 3,757 | 法师 `MagicObject` 子类 |
| `Taoist/` | 51 | 4,015 | 道士 `MagicObject` 子类 |
| `Assassin/` | 56 | 3,691 | 刺客 `MagicObject` 子类 |

总计 190 个技能类、14,036 行。每类直接继承 `MagicObject`；具体属性由 System.db 的 `MagicInfo` 和 `UserMagic` 共同提供。

### 4.5 ServerLibrary/Models/Monsters/

本次实测 101 个 C# 文件、9,283 行。它们是特殊 AI/BOSS 行为类；普通行为和 AI 编号映射仍在 `MonsterObject.GetMonster`（`MonsterObject.cs:122-647`）。新增特殊怪物必须同时考虑：

1. `LibraryCore/SystemModels/MonsterInfo` 的 DB 行。
2. `ServerLibrary/Models/Monsters/Xxx.cs` 的运行时类。
3. `MonsterObject.GetMonster` 中与 `MonsterInfo.AI` 对应的 case。
4. System.db 中已有地图 `RespawnInfo` 是否引用这个 MonsterInfo。

### 4.6 ServerLibrary/Envir/Commands/

本次实测 66 个 C# 文件、1,778 行。目录职责：

```text
Commands/
├── AdminCommandHandler.cs       Admin 权限 handler
├── PlayerCommandHandler.cs      普通玩家 handler
├── ErrorHandlingCommandHandler.cs
├── Command/
│   ├── AbstractCommand.cs
│   ├── AbstractParameterizedCommand.cs
│   ├── Admin/                   管理命令
│   ├── Player/                  普通命令和 Companion 子目录
│   └── Exceptions/
└── Handler/
    ├── AbstractCommandHandler.cs
    ├── ICommandHandler.cs
    └── IValidatingCommandHandler.cs
```

Admin 目录包含 `AddStat`、`Ban`、`ChatBan`、`GiveSkills`、`Level`、`LevelSkill`、`LevelWeapon`、`Make`、`MapMove`、`SpawnMob`、`ToggleGameMaster`、`ToggleObserver`、`ToggleSuperman`、`Reboot` 等。实际命令名以各文件的 `VALUE` 为准，不以文件名猜。

### 4.7 ServerLibrary/Envir/Events/

本次实测 27 个 C# 文件、1,902 行：

- 核心：`EventInfoHandler.cs`（413 行）、`IEventAction.cs`、`IEventTrigger.cs`。
- Attributes：`EventActionType.cs`、`EventTriggerType.cs`。
- Triggers：8 类，覆盖玩家死亡、怪物死亡/清空、玩家移动地图/区域、每分钟和昼夜。
- Actions：14 类，覆盖 buff、消息、给予物品、传送、刷怪、物品掉落和 timer 控制。

事件数据模型在 `LibraryCore/SystemModels/EventInfo.cs`：`WorldEventInfo` 从 `:9`、`PlayerEventInfo` 从 `:219`、`MonsterEventInfo` 从 `:505`、`EventLog` 从 `:1025`。

### 4.8 ServerLibrary/DBModels/

本次实测 **33 个文件、6,540 行**。这是 Users.db 模型所在位置，不是 `LibraryCore/DBModels/`；`LibraryCore` 当前没有 `DBModels/` 目录。主要模型：

| 文件 | 行数 | 用途 |
|---|---:|---|
| `AccountInfo.cs` | 731 | 账号、密码哈希、权限、激活、连接 |
| `CharacterInfo.cs` | 828 | 角色、等级、位置、背包关系、任务/技能 |
| `UserItem.cs` | 828 | 玩家物品及属性/耐久/强化 |
| `GuildInfo.cs` | 391 | 行会存档 |
| `UserMilestone.cs` | 279 | 玩家里程碑 |
| `UserCompanion.cs` | 268 | 玩家宠物 |
| `GameGoldPayment.cs` | 259 | 游戏币支付记录 |
| `UserQuest.cs` | 255 | 玩家任务 |
| `UserConquestStats.cs` | 238 | 沙巴克统计 |
| `BuffInfo.cs` | 237 | 账号/角色 buff |
| `UserMagic.cs` | 210 | 玩家技能等级和经验 |
| `GuildMemberInfo.cs` | 175 | 行会成员 |
| `MailInfo.cs` | 157 | 玩家邮件 |
| `AuctionInfo.cs` | 138 | 拍卖 |
| 其余 19 个 | — | 货币、交易、宠物、朋友、封锁、商店等关系模型 |

### 4.9 ServerLibrary/Converter/

| 文件 | 行数 | 职责 |
|---|---:|---|
| `DBObjectConverter.cs` | 634 | System.Text.Json 与 MirDB `DBObject`/引用/数组之间的读写转换 |
| `ImportReferenceResolver.cs` | 约 150 | JSON 导入时按身份解析 DBObject 引用 |

这部分是管理端/工具导入导出的 JSON 桥，不是 System.db 本身的存储格式。

### 4.10 LibraryCore/SystemModels/

本次实测 **40 个文件、8,897 行**，旧 `_index.md` 的 39 文件已经过时。它们属于 System.db 的静态表/关系模型，核心文件如下：

| 模型 | 作用 |
|---|---|
| `MapInfo.cs` | 地图文件名、描述、等级/副本关系 |
| `MapRegion.cs` | 地图区域和坐标点 |
| `RespawnInfo.cs` | 怪物刷新配置 |
| `MonsterInfo.cs` | 怪物静态属性、AI、掉落关联 |
| `DropInfo.cs` | 单条掉落物、概率、数量、DropSet |
| `ItemInfo.cs` | 物品静态定义 |
| `MagicInfo.cs` | 技能名称、职业、威力、消耗、等级门槛；字段详见 `MagicInfo.cs:6-314` |
| `BaseStat.cs` | 按职业/等级的基础属性 |
| `NPCInfo.cs` | NPC、页面和交易关系 |
| `MovementInfo.cs` | 地图传送点/移动关系 |
| `SafeZoneInfo.cs` | 安全区和起始区 |
| `CurrencyInfo.cs` | 金币等货币 |
| `QuestInfo.cs` | 任务静态定义 |
| `EventInfo.cs` | World/Player/Monster 事件和 EventLog |
| `InstanceInfo.cs`、`DungeonInfo.cs` | 副本和地牢 |
| `CastleInfo.cs`、`CastleGateInfo.cs`、`CastleGuardInfo.cs` | 城堡/攻城配置 |
| `CompanionInfo.cs`、`CompanionLevelInfo.cs`、`CompanionSkillInfo.cs` | 宠物静态表 |
| `BundleInfo.cs`、`LootBoxInfo.cs` | 捆包和箱子 |
| `StoreInfo.cs`、`FameInfo.cs`、`DisciplineInfo.cs` | 商店/声望/修炼 |

### 4.11 LibraryCore/Network/

本次实测 5 个文件、3,299 行：

| 文件 | 行数 | 方向/作用 |
|---|---:|---|
| `BaseConnection.cs` | 472 | 异步 TCP 收发、包队列、反射分发 |
| `Packet.cs` | 446 | 包发现、字段二进制序列化、包收发 |
| `ClientPackets.cs` | 861 | 客户端→服务端；如 Login、Move、Attack、Magic |
| `ServerPackets.cs` | 1,494 | 服务端→客户端；对象、地图、战斗、UI 状态 |
| `GeneralPackets.cs` | 26 | Connected、Ping、Disconnect、版本等通用包 |

`ClientPackets.Attack` 定义从 `LibraryCore/Network/ClientPackets.cs:142` 开始；`Login` 从 `:55`、`StartGame` 从 `:82`、`Move` 从 `:99`、`Magic` 从 `:159` 开始。

### 4.12 LibraryCore 根和 MirDB

| 区域 | 实测行数/规模 | 接手重点 |
|---|---:|---|
| `Enum.cs` | 3,439 | 共享枚举；`MagicType` 从 :628 开始 |
| `Stat.cs` | 931 | `Stats` 从 :10，`Stat` 枚举从 :507 |
| `Globals.cs` | 1,308 | 经验表、攻击/移动/施法时间、聊天限制等常量 |
| `Functions.cs` | 798 | 坐标、随机、通用计算 |
| `ConfigReader.cs` | 710 | `[ConfigPath]`/`[ConfigSection]` 配置读写 |
| `Encryption.cs` | 94 | DB/包加解密辅助 |
| `Time.cs` | 13 | 单调时间 `Time.Now` |
| `MirDB/` | 10 文件、1,766 行 | Session、DBCollection、DBObject、DBValue、关系映射 |

---

## 5. 数据层与写库纪律

### 5.1 两个数据库及模型对应

服务端运行目录：

```text
Debug/ServerCore/Database/System.db   世界静态数据
Debug/ServerCore/Database/Users.db    账号、角色和玩家运行数据
```

当前目录实测 `System.db` 约 11.2 MB，`Users.db` 约 733 KB。System.db 的客户端副本是：

```text
Debug/Client/Data/System.db
```

本次实测客户端副本与服务端副本大小均为 11,776,094 bytes、mtime 同为 2026-08-16 00:48；这证明当前两份文件同步，但不是未来修改后的自动同步机制。

模型对应关系：

| 文件/程序集 | 数据库 | 内容 |
|---|---|---|
| `LibraryCore/SystemModels/*.cs` | `System.db` | 地图、物品、魔法、怪物、NPC、刷新、任务、事件、商店等静态世界数据 |
| `ServerLibrary/DBModels/*.cs` | `Users.db` | Account、Character、UserItem、UserMagic、Guild、Mail、Quest、Companion 等用户存档 |

任务书把“LibraryCore 的 DBModels”作为范围描述，但当前代码事实是：`LibraryCore/` 没有 `DBModels/` 目录；Users.db 模型在 `ServerLibrary/DBModels/`，且当前实测 33 文件。接手时以这个目录为准。

### 5.2 MirDB 的真实格式

System.db/Users.db **不是 SQLite**。更精确地说，当前 `MirDB` 是项目自定义二进制格式，不是字面意义上的 .NET `BinaryFormatter`：

- `Session.InitializeSystem/InitializeUsers` 用 `BinaryReader` 读取头、映射和对象数据，见 `LibraryCore/MirDB/Session.cs:126-231`。
- `DBObject` 从 `RawData` 建立 `MemoryStream`/`BinaryReader`，保存时用 `MemoryStream`/`BinaryWriter`，见 `LibraryCore/MirDB/DBObject.cs:59-147`。
- 当前 `LibraryCore/MirDB/` 没有 `BinaryFormatter` 调用；因此“BinaryFormatter”是旧资料对二进制 DB 的宽泛称呼，不能据此用 Python/SQLite 工具直接打开。

可选加密由 `Library.Encryption.GetReader/GetWriter` 包装，是否启用由 `Config.EncryptionEnabled` 和 `EncryptionKey` 控制；不要把密钥写入文档。

### 5.3 Session root 和程序集初始化

`Session(SessionMode mode, string root = @".\Database\", ...)` 的默认 root 和 backup 见 `LibraryCore/MirDB/Session.cs:82-88`。`ResolvePath` 将相对路径合并到 `AppDomain.CurrentDomain.BaseDirectory`，见 `Session.cs:25-28`。因此从部署目录运行时，默认数据库就是 `Debug/ServerCore/Database/`。

服务端必须这样初始化：

```csharp
Session.Initialize(
    Assembly.GetAssembly(typeof(ItemInfo)),
    Assembly.GetAssembly(typeof(AccountInfo))
);
```

`ItemInfo` 在 LibraryCore，`AccountInfo` 在 ServerLibrary；源码注释也明确写了这一点（`SEnvir.cs:445-448`）。旧管理端相同（`Server/SMain.cs:101-109`）。只传一个程序集会让另一侧的 DB 类型没有映射，`GetCollection<T>()` 可能静默为空；这是最危险的“程序正常启动但表为 0 行”问题之一。

### 5.4 保存行为：全量重写

`Session.Save/Commit` 不是增量 SQL 更新，而是检测变更后重写整个 DB 文件：

1. 写入 `.tmp`。
2. 如开启备份，把旧文件压缩到 `Backup/System/` 或 `Backup/Users/`。
3. 删除旧文件。
4. 将 `.tmp` 移动为正式文件。

实现见 `LibraryCore/MirDB/Session.cs:232-351`。这意味着：

- 写库必须停服；服务端仍持有 Session 或正在 `Save` 时，外部写入会发生覆盖/竞态。
- 必须备份两个库，尤其是 `Users.db`。
- 只改一个静态表也会重写整份 System.db；不能把“文件很大”当成可以中断写入的理由。

运行时自动保存由 `SEnvir.EnvirLoop` 的 DB 时间阶段触发，见 `ServerLibrary/Envir/SEnvir.cs:1480-1490`；真正的 `Save`/后台 commit 入口在 `SEnvir.cs:1794-1814`。日志写入 `Logs.txt` 和 `Chat Logs.txt` 的循环在 `SEnvir.cs:1815-1857`。

### 5.5 外部工具的正确写库路径

推荐链路（`Mir3-Research/Tools/TOOL_INDEX.md:69-83`、`:156-165`）：

```text
1. 停止 ServerCore / 确认 7000 不监听
2. 备份 Debug/ServerCore/Database/System.db 和 Users.db
3. dbeditor :8810 编辑
   └─ 保存只落 Tools/dbeditor/workspace/*.json
4. 对 workspace 做 diff/审查/引用校验
5. 用户显式点击“同步到数据库”
6. dbeditor/sync.sh
   └─ 调 DBImporter
   └─ 校验 → 备份 → 服务端 System.db + 客户端 System.db 双库写入
   └─ round-trip 读回
7. 重新启动服务端
8. 用游戏或独立工具实测
```

工具职责：

| 工具 | 作用 |
|---|---|
| `Tools/SystemDbProbe/` | 只读导出 System.db 为 Markdown/JSON |
| `Tools/dbeditor/` | JSON 缓冲区编辑，不应绕过缓冲区直写 |
| `Tools/DBImporter/` | dbeditor 同步链执行端；`Program.cs`/README 在 Mir3-Research 工具目录 |
| `Tools/dbeditor/sync.sh` | 本次实测存在，负责触发同步链 |
| `Tools/NpcMover/` | NPC 坐标迁移 |
| `Tools/questdata/` | 任务语义映射、manifest、导入 |

严格规则：服务端 `:7000` 正在监听时不写 System.db；`Users.db` 不交给外部工具写；服务端和客户端 System.db 必须同步；写后 round-trip 读回。规则原文见 `TOOL_INDEX.md:156-165`。

### 5.6 工作目录产生的混淆文件

仓库根当前也有一个 `System.db` 和 `Config/ExperienceList.txt`，这是运行/工具从错误工作目录产生不同路径产物的实物证据。不要因为根目录存在 DB 就认为 ServerCore 正在使用它：

- ServerCore 部署 DB 是 `Debug/ServerCore/Database/`。
- 运行目录 `Server.ini` 的 `MapPath=Map/` 只在 `Debug/ServerCore` 工作目录正确。
- `SEnvir.LoadExperienceList` 使用 `./Config/ExperienceList.txt`，其解析基准是进程工作目录。

发现多个 `System.db` 时先检查实际进程 cwd、systemd `WorkingDirectory` 和文件 mtime，再决定哪一份是生产库。

---

## 6. 常见修改配方

每条配方都分“改哪里、关键方法、验证”。本任务没有启动服务端，验证步骤是接手者必须实际执行的行为检查；不能用“编译通过”替代。

### 6.1 加/改一个技能

**改哪里**

1. `LibraryCore/Enum.cs`：如技能没有 `MagicType`，加入枚举；当前枚举起点见 `Enum.cs:628`。
2. `LibraryCore/SystemModels/MagicInfo.cs`：System.db 中创建/修改技能静态行。`MagicInfo` 包括 `Name`、`Magic`、`Class`、`School`、`Property`、威力、消耗、等级门槛、经验、Delay，见 `MagicInfo.cs:6-314`。
3. `ServerLibrary/Models/Magics/{Warrior,Wizard,Taoist,Assassin}/Xxx.cs`：直接继承 `MagicObject`，添加 `[MagicType(MagicType.Xxx)]`。
4. 如果是攻击/被动/开关技能，覆盖 `AttackCast`、`MagicCast`、`ModifyPowerAdditionner`、`ModifyPowerMultiplier`、`SecondaryAttackLocation` 等钩子。

**关键方法**

- `SEnvir.CreateMagic()` 反射收集直接 `MagicObject` 子类，`SEnvir.cs:1244-1255`。
- `PlayerObject.SetupMagic` 按 `MagicTypeAttribute` 实例化，`PlayerObject.cs:247-279`。
- `HalfMoon.cs:9-15`、`:32-66` 是最小攻击技能样例。

**验证**

1. 停服，通过 dbeditor 修改 System.db 的 `MagicInfo`，完成双库同步。
2. 重新构建并确认部署目录 DLL 已更新。
3. 登录目标职业角色，确认技能进入技能列表。
4. 检查客户端收到对应 `S.MagicToggle`/`S.ObjectAttack`/效果包。
5. 实测 MP 消耗、冷却、攻击范围、伤害和升级经验；数值公式查 `docs/codebase/combat/magic-damage.md`，不要凭感觉重写。
6. 如果只有编译没有登录和施法，这条修改仍是“未验证”。

**易错点**：`CreateMagic` 条件是 `type.BaseType == typeof(MagicObject)`（`SEnvir.cs:1248-1252`）。所有当前 190 个技能类直接继承 `MagicObject`；新技能若继承另一个技能类，不会自动进入 `MagicTypes`。

### 6.2 加怪物

**改哪里**

1. `LibraryCore/SystemModels/MonsterInfo.cs` 的 System.db 行：静态名称、等级、属性、AI、掉落关系。
2. 若只需要现有 AI，使用已有 `MonsterInfo.AI` 编号。
3. 若需要新行为，在 `ServerLibrary/Models/Monsters/Xxx.cs` 添加类。
4. 在 `ServerLibrary/Models/MonsterObject.cs:122-647` 的 `GetMonster` 中添加 `MonsterInfo.AI` case，把该 AI 映射到新类。
5. 在 `RespawnInfo` 中把怪物挂到地图区域和数量。

**关键方法**

- `Map.SpawnInfo.DoSpawn` 在 `Map.cs:421-449` 计算数量、调用 `MonsterObject.GetMonster`、设置 SpawnInfo 并 Spawn。
- AI 主阶段是 `MonsterObject.ProcessAI`、`ProcessSearch`、`ProcessRoam`、`ProcessTarget`，见 `MonsterObject.cs:965-1243`。
- 事件/掉落/死亡在 `MonsterObject.cs:2510-3028`。

**验证**

1. 停服修改 MonsterInfo/RespawnInfo，双库同步 System.db。
2. 如新增 C# 类，构建部署 ServerLibrary DLL。
3. 用 GM `@move` 到目标地图，再用 `@spawn MonsterName count` 或等待 RespawnInfo。
4. 独立检查 AI：出生、巡逻、索敌、攻击、死亡、重生和掉落。
5. 不能只在同一个生产刷怪工具里验证索引；地图/AI 索引要用独立查询或真实游戏画面交叉验证。验证深度规则见 `AGENTS.md`。

### 6.3 加 GM 命令

**改哪里**

新建 `ServerLibrary/Envir/Commands/Command/Admin/Xxx.cs`，继承：

- 无参数：`AbstractCommand<IAdminCommand>`。
- 有参数：`AbstractParameterizedCommand<IAdminCommand>`。

**关键方法**

`AbstractCommandHandler` 启动时反射注册全部命令，见 `AbstractCommandHandler.cs:17-25`；执行分发在 `:37-53`。权限由 `AdminCommandHandler.cs:9-12` 判定。

参考 `MapMove.cs:10-18`：`VALUE` 是聊天命令名，`PARAMS_LENGTH` 是最少参数，`Action` 负责解析和抛出 `UserCommandException`。坐标和地图校验样例在 `MapMove.cs:20-45`。

**验证**

1. 构建 ServerLibrary/ServerCore。
2. 重启实际运行的 `Debug/ServerCore/ServerCore.dll`。
3. 用永久 GM 测试账号在游戏聊天框输入 `@命令 参数`。
4. 验证正常参数、缺参数、非法对象和非 GM 账号四条路径。
5. 检查错误是否通过游戏聊天返回，而不是只看服务器控制台。

### 6.4 改掉落

**改哪里**

- 静态掉落：`LibraryCore/SystemModels/DropInfo.cs` 和对应 `MonsterInfo.Drops` System.db 行。
- 运行时掉落：`ServerLibrary/Models/MonsterObject.cs:2691` 的 `Drop(PlayerObject owner, int players, decimal rate)`。

**关键逻辑**

`Drop` 先叠加玩家 DropRate/BaseDropRate、地图掉率和成长等级，再遍历 `MonsterInfo.Drops`；金币按玩家数分摊，普通物品按概率和掉率计算，Fortune 会写 `UserDrop.Progress`，`PartOnly` 另走部件概率。关键区间见 `MonsterObject.cs:2691-2783`，完整掉落机制引用 `docs/codebase/item/drops.md` 和 `death-and-loot.md`。

**验证**

1. 停服改 DropInfo，双库同步。
2. 用固定怪物、固定玩家倍率和多个击杀样本验证：掉落物、数量、金币分摊、绑定/拾取权限、Fortune 进度。
3. 观察死亡事件、地面 ItemObject 和 Users.db 的 UserDrop 变化。
4. 不用一次掉落结果断言概率正确；至少验证普通掉落、金币、PartOnly 和无掉落四类路径。

### 6.5 改经验/属性公式

#### 经验表和经验倍率

- 等级经验表文件由 `SEnvir.LoadExperienceList` 读 `./Config/ExperienceList.txt`，缺失时从 `Globals.ExperienceList` 生成，见 `SEnvir.cs:398-434`。
- `PlayerObject.AddBaseStats` 用 `Globals.ExperienceList[Level]` 设置 `MaxExperience`，见 `PlayerObject.cs:2528-2531`。
- `PlayerObject.GainExperience` 先应用 `Stat.ExperienceRate`、`BaseExperienceRate` 和 Rebirth 衰减，再累计经验，达到 `Config.MaxLevel` 或不足升级条件时返回，见 `PlayerObject.cs:2036-2107`。
- 全局 `ExperienceRate`、`DropRate` 等配置在 `Config.cs:151-156`。

**验证**：备份并在测试账号上使用固定击杀/GM 经验路径，确认当前等级、累计经验、跨级、满级不溢出，重启后读回 Users.db；不要仅比较一个 UI 数字。

#### 基础属性

`PlayerObject.AddBaseStats` 从 `SEnvir.BaseStatList.Binding` 中选择同职业、等级不高于当前等级的最高匹配 `BaseStat`，见 `PlayerObject.cs:2528-2546`，随后设置 Health/Mana/AC/MR/DC/MC/SC 等基础属性，见 `:2548-2587`。装备、buff、宝石和修炼在后续 `RefreshStats` 叠加。

**验证**：用空装备角色和一件装备角色分别重算，验证等级边界（恰好表项、表项之间、超过最大表项）、HP/MP、负属性、重连持久化。完整元素/属性语义引用 `docs/codebase/combat/elements-and-buffs.md`。

### 6.6 改地图刷新

**改哪里**

- `LibraryCore/SystemModels/RespawnInfo.cs` 的 System.db 行：地图区域、怪物、数量、Delay、Announce、EventSpawn、DropSet。
- `ServerLibrary/Models/Map.cs:374-472` 的 `SpawnInfo`：内存刷新状态。

**关键方法**

- 构造函数按 Region 获取地图：`Map.cs:384-389`。
- `DoSpawn` 检查 RespawnIndex、EventSpawn、NextSpawn 和 Delay：`Map.cs:391-419`。
- `Delay >= 1000000` 表示一天中的定点分钟，跨午夜判定在 `Map.cs:399-410`。
- 普通地牢使用 `Dungeon.SpawnMultiplier` 调整数量，`Map.cs:421-427`。
- 生成实际怪物在 `Map.cs:429-470`。

**验证**：

1. 停服修改 RespawnInfo，双库同步。
2. 普通刷新验证首次数量、Delay、Announce、RespawnIndex。
3. 定点刷新至少跨一次目标时间窗口，确认不会跨午夜重复触发。
4. 地牢实例验证倍率、实例序号、卸载后重新加载。
5. 地图渲染和坐标变更必须游戏内查看或独立离线渲染对照，不能只看 DB 行。

---

## 7. 与其他组件的接口

### 7.1 网络协议

协议定义全部在 `LibraryCore/Network/`：

```text
ClientPackets.cs   客户端 → ServerCore
ServerPackets.cs   ServerCore → 客户端
GeneralPackets.cs  双方通用连接/心跳/断开/版本包
Packet.cs          发现、排序、字段读写
```

消费方包括：

- 原 Windows 客户端 `Client/`。
- Godot 客户端 `GodotClient/Network/ServerConnection.cs`。
- `BotRunner/` 模拟客户端。
- Mir3-Research 工具链中的 `wsgateway`/`webport`。

包 ID 不是手写常量表，而是 `Packet` 静态构造按反射排序决定，`Packet.cs:23-48`。`TOOL_INDEX.md:174-175` 特别记录：wsgateway 的 packet id 必须从部署 DLL 反射导出，不能凭源码文件顺序手算。

### 7.2 客户端 System.db 副本

服务端运行：

```text
Debug/ServerCore/Database/System.db
```

客户端读取：

```text
Debug/Client/Data/System.db
```

修改静态表后双库同步是强制接口，不是可选部署步骤。客户端会用 System.db 解析 NPC、物品、怪物、地图、技能等静态内容；服务端若只更新自己的 System.db，客户端可能收到服务端索引但找不到显示模型。

`GodotClient` 也有自己的 `DatabaseLoader`，zdocs 索引在 `docs/codebase/infra/envir-and-spawn.md:802-803` 记录其加载关系。服务端静态数据改变后要同时检查 Godot 读取的字段是否仍兼容；不要把服务端静态模型当成只服务端使用。

### 7.3 Config/、ClientData/ 和运行目录

三个容易混淆的层次：

```text
Debug/ServerCore/Server.ini                  服务端有效配置
Debug/ServerCore/Config/ExperienceList.txt   服务端有效经验表
Debug/ServerCore/Translations/*.ini          服务端语言文件
Debug/ServerCore/Map/*.map                   服务端地图文件
Debug/Client/Data/System.db                  客户端静态库副本
ClientData/*.json                            客户端/Godot 生成或审计产物
Config/ExperienceList.txt                   仓库根工作目录产物，不能自动视为运行时文件
```

`ClientData/` 当前包含 `magic-effects.json`、`frame-formulas.json`、`sounds.json` 等客户端产物；它不是 ServerCore 的配置目录。服务端改魔法逻辑不应顺手修改这些文件，除非客户端表现确实需要配套更新并且任务范围包含它。

### 7.4 Mir3-Research 工具链和 symlink

本次实测：

```text
/home/tetsuya/development/Mir3-Research/LibraryCore  -> /home/tetsuya/development/zircon/LibraryCore
/home/tetsuya/development/Mir3-Research/ServerLibrary -> /home/tetsuya/development/zircon/ServerLibrary
```

工具直接复用 Zircon 的模型类编译；改动 Zircon 的 C# 后，工具可能需要重新 build。工具运行数据一般在 Zircon `Debug/` 下。工具总目录入口是 `/home/tetsuya/development/Mir3-Research/Tools/TOOL_INDEX.md`，数据库工具见其 §二 A，写库纪律见 §五。

### 7.5 与旧 Server/ 管理端的接口

`Server/` 通过 `SMain` 提供 System.db 管理界面、地图查看、缓存、诊断和插件。它创建 `SessionMode.System` 并传入两套程序集，见 `Server/SMain.cs:101-109`；点击 Start 仍调用 `SEnvir.StartServer()`，见 `:245-257`。

因此：

- 修改 `ServerLibrary` 的公开 DB 模型，会同时影响 ServerCore 和旧管理端。
- 修改 ServerCore 的启动逻辑，不会自动改变旧 WinForms 入口。
- 修改 Server/ Views，不应作为当前服务端运行时修复的替代方案。
- `Server/Diagnostics/OrphanDiagnostic.cs` 和 `Server/Helpers/JsonExporter.cs` 是管理端工具，不是生产 ServerCore 运行路径。

---

## 8. 已知坑

### 8.1 MirDB 必须传两个程序集

**来源**：`ServerLibrary/Envir/SEnvir.cs:445-448`、`Server/SMain.cs:106-109`、`Tools/TOOL_INDEX.md:163-164`。

`Session.Initialize` 只传 LibraryCore 时，SystemModels 能发现但 ServerLibrary 的 Users.db 模型不在映射；只传 ServerLibrary 时反过来。症状不是明确异常，而可能是集合为空。所有服务端和 DB 工具都必须传 LibraryCore + ServerLibrary，且使用正确的绝对/基准路径。

### 8.2 服务端运行中绝不写 System.db

**来源**：`TOOL_INDEX.md:156-165`，`AGENTS.md` 写库纪律。

运行中的 Session 会自动 Save；外部 DBImporter 或管理端写库会和服务端全量重写互相覆盖。先停 `zircon-server`，确认 `7000` 不监听，再备份和写入。

### 8.3 System.db/Users.db 是全量重写

**来源**：`LibraryCore/MirDB/Session.cs:274-351`。

没有 SQLite 事务或增量表更新。`.tmp → 备份 → 删除旧文件 → move` 中任何中断都必须从备份恢复，不要手动创建一个空文件“让服务端先启动”。

### 8.4 两份 System.db 必须同步

**来源**：`TOOL_INDEX.md:159-161`，当前 `Debug/ServerCore/Database/System.db` 与 `Debug/Client/Data/System.db` 实测存在且同大小。

只写服务器端会造成服务端能刷怪但客户端没有对应 `ItemInfo`/`MonsterInfo`/`NPCInfo` 的显示或行为。同步后必须 round-trip 读回两份并比较关键行。

### 8.5 Users.db 是玩家存档，不要用静态工具写

**来源**：`TOOL_INDEX.md:165`。

单机注入和游戏内操作会改变 Users.db。不要把测试账号状态写回 System.db，也不要用编辑 System.db 的工具改账号/角色。恢复测试账号时停服、备份、替换 Users.db，再启动。

### 8.6 编译通过不等于逻辑正确

**来源**：`AGENTS.md`“验证深度约定”。该文件记录过：注释吞掉登录代码导致客户端卡在选人界面，编译仍成功；地图索引偏移错误在同一错误工具中自洽，离线验证仍通过。

服务端修改的最低验证按类型不同：

- 登录/进游戏：完整登录到进入地图。
- 地图/贴图/刷新：游戏内或独立解析器对照。
- 协议：实际客户端/独立包检查器验证包 ID 和字段。
- 数据转换：生产工具和校验工具不能共用同一错误解析约定。

### 8.7 文本编码

**来源**：本次 `file Debug/ServerCore/Server.ini` 实测为 UTF-16 LE；`TOOL_INDEX.md:177` 记录 NAS/Mud3 中文文本是 GB18030。

`Server.ini` 和 Translation ini 不要无意转换为 UTF-8；外部 NAS/Mud3 文本也不要用默认 UTF-8 直接读取。出现乱码先检测 BOM/编码，再决定转换。

### 8.8 相对路径和 MapPath

**来源**：`ServerLibrary/Models/Map.cs:60`、`ServerLibrary/Envir/SEnvir.cs:400`、`docs/REMOTE_SERVER_AND_CLIENT_SETUP.md:127-128`。

部署工作目录固定为 `Debug/ServerCore` 时，`Server.ini` 的 `MapPath=Map/` 才能找到 `Debug/ServerCore/Map/*.map`。代码默认值 `Debug/ServerCore/Map/` 在仓库根工作目录才适合；把它和部署配置混用会变成 `Debug/ServerCore/Debug/ServerCore/Map/`。同理，错误 cwd 会生成另一份 `Config/ExperienceList.txt`。

### 8.9 包 ID 会因反射排序漂移

**来源**：`LibraryCore/Network/Packet.cs:23-48`、`TOOL_INDEX.md:174-175`。

新增或重命名 `Packet` 类型会改变排序，可能导致客户端、wsgateway、BotRunner 都读错包。修改协议后必须用部署 DLL 导出包 ID，并同步所有消费者；不能按文件行号或手算编号。

### 8.10 Godot 版本校验缺口

**来源**：`docs/codebase/protocol/connection-lifecycle.md:348-349`；当前运行 `Server.ini` 的 `CheckVersion=False` 是本次实测。

Godot `ServerConnection` 没有完整的 `Process(G.CheckVersion)/Process(G.Version)` 握手实现。若服务端打开默认 `CheckVersion=true`，Godot 客户端可能无法完成握手。修改版本校验前必须同时检查客户端实现和部署配置。

### 8.11 单机开发模式污染 Users.db

**来源**：`ServerLibrary/Envir/DevSinglePlayer.cs:60-107`、`docs/handoffs/GOAL_GODOT_CLIENT_HANDOFF.md:35-37`。

`--singleplayer-dev` 不是内存 mock；等级、技能、物品和金币会通过正常对象关系进入 Users.db。不要在共享测试库上无意识反复启动该模式。

### 8.12 魔法注册只收直接子类

**来源**：`ServerLibrary/Envir/SEnvir.cs:1248-1252`；当前 Magics 实测 190/190 个类直接 `: MagicObject`。

若新技能继承另一个技能类，`type.BaseType == typeof(MagicObject)` 不成立，反射不会把它放入 `MagicTypes`。共用代码请提取辅助方法或组合，而不是用二级技能类继承绕过注册条件。

### 8.13 控制台命令不存在

**来源**：`ServerCore/Program.cs:47-51`。

`Console.ReadLine()` 的结果未使用。不要在运维文档中写 `@move` 是控制台命令；`@move` 是游戏聊天命令，由 `PlayerObject.Chat` → `CommandHandler` 处理。

### 8.14 改 DLL 后必须确认实际进程使用新 DLL

**来源**：`docs/REMOTE_SERVER_AND_CLIENT_SETUP.md:245-250`、`TOOL_INDEX.md:170`。

先确认 systemd/`zircon-server` 的工作目录、PID 和 DLL mtime，再重启。后端源码变更后只 `curl` 或只看源码不代表运行进程已更新。

### 8.15 路径大小写

**来源**：`TOOL_INDEX.md:176`。

仓库目录是小写 `zircon`。工具硬编码大写 `Zircon` 可能静默失败；在跨仓库脚本中使用 `readlink -f` 或明确绝对路径。

### 8.16 事件没有 MapEvent 类

**来源**：本次全仓检索；实际实现为 `EventInfoHandler`，源码注册/处理见 `ServerLibrary/Envir/Events/EventInfoHandler.cs:32-171`。

接手任务时不要按旧名称搜索并臆造 API；用 `EventInfoHandler`、`WorldEventTrigger`、`PlayerEventTrigger`、`MonsterEventTrigger` 和 `EventInfo.cs` 数据模型定位。

### 8.17 Email/WebServer 的秘密和端口

**来源**：`Config.cs:66-91`、`WebServer.cs:63-112`、`EmailService.cs:25-31`。

默认 WebServer 关闭；打开会尝试绑定 80 的三个前缀，SMTP 使用配置凭证。不要为本地测试随意打开 WebServer 或提交真实邮件密码；80 端口冲突时启动会失败并停止全部 Web listener。

### 8.18 注释不能吞代码

**来源**：`AGENTS.md` 注释吞登录代码的事故记录。

写 `if`/`{` 前的注释后必须检查换行；不要把注释和关键语句写在同一行。可用仓库规则中建议的 `grep -n "//.*if\|//.*{"` 做快速审查，但最终应读 diff。

---

## 9. 别做什么

1. **不要修改代码来完成本篇文档任务**；本篇只写文档。服务端修改必须另开明确任务。
2. **不要在运行中的 ServerCore/Server/管理端上写 System.db 或 Users.db**。
3. **不要把 System.db 当 SQLite 或直接用 Python `sqlite3` 打开**；使用 SystemDbProbe/dbeditor/DBImporter。
4. **不要只更新服务端 System.db，不更新 `Debug/Client/Data/System.db`**。
5. **不要删除 `Users.db` 试图“重置登录”**；先备份并确认恢复方案。
6. **不要把 `Server/` 旧 WinForms 管理端当作当前 Linux 入口**；它是 Windows-only 且有 DevExpress/Rendering 依赖。
7. **不要把 `ServerCore` 的空控制台 ReadLine 描述成控制台 GM 命令系统**。
8. **不要在不重启实际部署进程的情况下声称后端修复已生效**。
9. **不要手算包 ID，也不要只改服务端包而不更新客户端、BotRunner、wsgateway/webport 消费者**。
10. **不要用生产转换工具复用同一解析逻辑做唯一验证**，尤其是地图索引、帧索引、`+1/-1` 约定。
11. **不要动 `Client/` 目录来完成服务端接手工作**；客户端是另一条范围。
12. **不要触碰 `docs/reviews/`**；当前工作树该目录有其他未提交工作。
13. **不要删除 `~/immich` 或其他无关目录**；本服务端任务不涉及它们。
14. **不要在 upstream 合并或 rebase 时自行解决业务逻辑冲突**；遇到冲突先保留上下文并询问用户。
15. **不要把秘密值写入文档、提交、日志或示例命令**：MasterPassword、SMTP Password、SyncKey、EncryptionKey 都只描述键名和用途。

---

## 10. 延伸资料

### 10.1 zdocs 代码深读文档

索引：`docs/codebase/_index.md`。它列出 23 篇已完成文档；本篇不重复抄写机制公式，而给接手入口。

服务端接手最常用的篇目：

| 文档 | 用途 |
|---|---|
| `docs/codebase/infra/envir-and-spawn.md` | SEnvir 主循环、对象生命周期、NPC/怪物刷新 |
| `docs/codebase/infra/database.md` | System.db/Users.db 表和保存时机 |
| `docs/codebase/infra/config-and-commands.md` | 配置项和 GM 命令全集 |
| `docs/codebase/protocol/packets-c2s.md` | 全部 C→S 包 |
| `docs/codebase/protocol/packets-s2c.md` | 全部 S→C 包 |
| `docs/codebase/protocol/connection-lifecycle.md` | 连接生命周期、握手、超时、加密 |
| `docs/codebase/combat/physical-damage.md` | 物理攻击/命中/战士技能倍率 |
| `docs/codebase/combat/magic-damage.md` | 魔法伤害和技能公式 |
| `docs/codebase/combat/elements-and-buffs.md` | Stat、buff、毒素、元素 |
| `docs/codebase/combat/death-and-loot.md` | 死亡、掉落、PK、组队经验 |
| `docs/codebase/monster/ai-behaviors.md` | MonsterObject AI 状态机 |
| `docs/codebase/monster/boss-mechanics.md` | BOSS 专属行为 |
| `docs/codebase/item/drops.md` | DropInfo 权重掉落 |
| `docs/codebase/map/tiles-and-movement.md` | Cell、坐标、移动和传送 |
| `docs/codebase/map/instances.md` | 副本/DynRegion/难度缩放 |
| `docs/codebase/social/guild.md` | 行会和行会战 |
| `docs/codebase/social/chat-and-mail.md` | 聊天、邮件 |
| `docs/codebase/sys/conquest-sabuk.md` | 沙巴克攻城 |

`_index.md:94-102` 说明这些文档已有引用抽查和协议覆盖；先查文档，再回源码。

### 10.2 仓库内服务端部署和审计

- `docs/REMOTE_SERVER_AND_CLIENT_SETUP.md`：82 远程服务部署、systemd、WorkingDirectory、MapPath、构建和重启。
- `docs/SINGLE_PLAYER_MODE_2026-08-13.md`：Godot 单机拉起 ServerCore 的客户端侧流程。
- `docs/MAGIC_FULL_AUDIT.md`：魔法数据和客户端表现审计。
- `docs/MAGIC_GROUND_EFFECT_FIXES.md`：魔法地面效果相关修复记录。
- `docs/MAP_FORMAT_COMPARISON.md`：Zircon `.Zl` 与 NAS `.wil`/地图数据差异。
- `docs/MINE_MAP_COMPARISON.md`、`docs/Sabak_Map_Migration_Audit_2026-08-11.md`：地图审计案例，适合学习“工具自洽不等于正确”的风险。

### 10.3 Mir3-Research 工具

总目录：

```text
/home/tetsuya/development/Mir3-Research/Tools/TOOL_INDEX.md
```

服务端直接相关：

- `Tools/SystemDbProbe/`：只读导出。
- `Tools/DBImporter/`：写回执行端。
- `Tools/dbeditor/`：缓冲区编辑器，端口 8810。
- `Tools/NpcMover/`：NPC 坐标迁移。
- `Tools/questdata/`：任务数据导入。
- `Tools/wsgateway/`：WebSocket→TCP，端口 7001；包 ID 必须从部署 DLL 导出。
- `Tools/webport/`：网页客户端真服联调，端口 8823。
- `Tools/webclient/`：静态世界测试台，端口 8822，不代表真实服务端行为。

### 10.4 运维端口速查

```text
7000  ServerCore 游戏 TCP
3000  ServerCore 用户数探针
7001  wsgateway
8810  dbeditor
8822  webclient
8823  webport
80    可选 WebServer / 外部运维服务（默认 ServerCore WebServer 关闭）
```

修改端口前先查 `ss -tlnp`；多个服务共用 80 时，不要只看 ServerCore 配置。

---

## 11. 自检与交接结论

### 11.1 三问自答

**(a) 只读这一篇，能否跑起来？**

能得到完整的构建和启动路径：从仓库根 `dotnet build ServerCore/ServerCore.csproj -c Debug`，进入 `Debug/ServerCore`，执行 `dotnet ServerCore.dll`；知道有效配置、数据库目录、7000 端口、测试账号、单机参数以及当前未做真实启动验证的边界。若要求“只凭本文证明服务端实际能启动”，不能；本次按任务禁止启动服务端，启动行为仍需接手者执行。

**(b) 只读这一篇，知道改哪吗？**

能。技能看 `MagicInfo` + `Models/Magics` + `CreateMagic/SetupMagic`；怪物看 `MonsterInfo` + `Monsters` + `GetMonster`；GM 命令看 `Commands/Command/Admin`；掉落看 `DropInfo` + `MonsterObject.Drop`；经验/属性看 `ExperienceList`/`GainExperience`/`BaseStat`；刷怪看 `RespawnInfo`/`SpawnInfo.DoSpawn`。每条配方都给了关键方法、源码行号和验证路径。

**(c) 只读这一篇，能否不踩坑？**

能覆盖主要高风险坑：MirDB 双程序集、服务端运行时写库、System.db 双库同步、Users.db 污染、全量重写、工作目录/MapPath、UTF-16/GB18030、包 ID 反射漂移、Godot 版本校验、编译不等于行为验证、旧 Server/入口混淆和控制台 ReadLine 误读。任何数据库写入和协议变更仍必须按工具链和真实行为验证执行。

### 11.2 20 处引用抽查记录

完稿后从正文引用中随机抽取以下 20 处，使用 `sed -n` 逐段读取源码核对路径和行号。20/20 处均命中对应声明；以下结果来自本次抽查，不代表构建或运行验证。

| # | 引用 | 核对内容 | 结果 |
|---:|---|---|---|
| 1 | `ServerCore/Program.cs:19-24` | `--singleplayer-dev` 设置单机开关和等级 | 通过：检测参数、设置 `SinglePlayerDev`、提升 `MaxLevel` |
| 2 | `ServerCore/ServerCore.csproj:4` | `TargetFramework=net10.0` | 通过：目标框架匹配 |
| 3 | `ServerLibrary/Envir/Config.cs:8-13` | Server.ini 路径、IP、7000 端口 | 通过：`ConfigPath`、`127.0.0.1`、`7000` 匹配 |
| 4 | `ServerLibrary/Envir/SEnvir.cs:390-396` | `StartServer` 创建后台线程 | 通过：创建并启动 `EnvirThread` |
| 5 | `ServerLibrary/Envir/SEnvir.cs:445-448` | Session 双程序集初始化 | 通过：ItemInfo/AccountInfo 两程序集匹配 |
| 6 | `ServerLibrary/Envir/SEnvir.cs:1373-1384` | EnvirLoop 启动顺序 | 通过：StartEnvir、StartNetwork、WebServer、Started 匹配 |
| 7 | `ServerLibrary/Envir/SEnvir.cs:1480-1490` | DB 保存阶段 | 通过：DBTime、`!Saving`、`Save()` 匹配 |
| 8 | `ServerLibrary/Envir/SConnection.cs:501-509` | Attack 包调用 Player.Attack | 通过：Game 阶段、方向校验、`Player.Attack` 匹配 |
| 9 | `LibraryCore/Network/BaseConnection.cs:396-413` | 反射查找 Process(PacketType) | 通过：`GetMethod("Process", new[] { p.PacketType })` 匹配 |
| 10 | `LibraryCore/Network/Packet.cs:23-48` | 包类型反射排序 | 通过：反射收集、GeneralPackets 优先、名称排序匹配 |
| 11 | `ServerLibrary/Models/PlayerObject.cs:186-245` | 玩家构造和单机注入调用 | 通过：`SetupMagic()` 后调用 `DevSinglePlayer.Apply` |
| 12 | `ServerLibrary/Models/PlayerObject.cs:14714-14813` | 攻击门控、技能、攻击广播 | 通过：时间门控、AttackCast、`S.ObjectAttack` 匹配 |
| 13 | `ServerLibrary/Models/MonsterObject.cs:122-145` | AI 工厂 switch | 通过：`switch (monsterInfo.AI)` 和多个返回类匹配 |
| 14 | `ServerLibrary/Models/MonsterObject.cs:2691-2714` | 掉落倍率和 DropInfo 遍历 | 通过：四类倍率和 `MonsterInfo.Drops` 匹配 |
| 15 | `ServerLibrary/Models/Map.cs:391-410` | SpawnInfo/定点刷新 | 通过：`DoSpawn`、Delay、`>=1000000` 分支匹配 |
| 16 | `ServerLibrary/Envir/Commands/Handler/AbstractCommandHandler.cs:17-25` | 命令反射注册 | 通过：程序集扫描和 `Activator.CreateInstance` 匹配 |
| 17 | `ServerLibrary/Envir/Events/EventInfoHandler.cs:32-75` | Trigger/Action 反射注册 | 通过：属性查找和两个注册表匹配 |
| 18 | `LibraryCore/MirDB/Session.cs:232-351` | Save/Commit 全量重写和备份 | 通过：Save/Commit、tmp、gzip、替换匹配 |
| 19 | `LibraryCore/MirDB/DBObject.cs:59-147` | 自定义 BinaryReader/BinaryWriter | 通过：RawData 读取和 BinaryWriter 保存匹配 |
| 20 | `ServerLibrary/Envir/WebServer.cs:63-87` | WebServer 开关和三个 listener | 通过：开关、Web/Buy/IPN 三 listener 匹配 |

### 11.3 未验证项清单

本篇完成的是读码、目录实测和文档交接；以下没有在本任务中执行：

- 未启动 `ServerCore.dll`，未确认本机端口实际监听。
- 未运行 `dotnet build`；构建命令来自 csproj 和部署文档，当前 DLL/运行目录存在但不是本次构建证明。
- 未使用测试账号完整登录到选人和地图。
- 未在游戏内验证单机满级注入、GM 命令、攻击、掉落、刷新或地图渲染。
- 未写 System.db/Users.db，也未执行 dbeditor 同步；双库文件位置和大小为只读实测。
- 未验证远程 82 systemd 服务当前 PID、配置 mtime 或运行 DLL 是否为当前源码构建。
- 未验证所有 190 个技能和 101 个怪物类的行为；只验证了目录规模、反射入口和样例。
- 未验证外部 SMTP、WebServer 80 端口、PayPal IPN。

**交接结论**：服务端的正确切入点是 `ServerCore/Program.cs → SEnvir.EnvirLoop → SConnection/Models/MirDB`。修改静态数据走 dbeditor 缓冲区和双库同步；修改规则走 ServerLibrary 并重建部署 DLL；修改协议必须同时检查所有消费者；任何“编译通过”之外的行为都要用真实流程或独立工具验证。
