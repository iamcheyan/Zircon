# 讨论记录 03：服务端在 Linux 跑通 —— 路径分隔符坑与修复

> 日期：2026-08-06
> 关联：讨论 02 决定走"在线客户端"方案，本篇记录把 ServerCore 真正在 Linux 跑起来的过程。
> 结论：**ServerCore 已在 Linux 跑通**，监听 127.0.0.1:7000，TCP 连通验证通过。

---

## 1. 目标

验证讨论 02 的核心前提：**ServerCore（无头服务端）能在 Linux 上直接跑起来、监听端口、接受连接。** 这是整个"在线客户端"方案的地基——如果服务端跑不起来，方案 A 就不成立。

## 2. 过程

### 2.1 编译 ServerCore

```
dotnet build ServerCore/ServerCore.csproj -c Debug
→ 已成功生成。0 个警告 0 个错误
→ 输出：/home/tetsuya/development/Debug/ServerCore/ServerCore.dll
```

`ServerCore.csproj` 是 `net10.0` / `Exe`，只引用 LibraryCore + ServerLibrary + autofac，无 Windows 依赖。编译没问题。

> 注意输出路径：csproj 里 `<OutputPath>..\..\Debug\ServerCore\</OutputPath>`，相对于 `ServerCore/` 目录算到了**仓库外**的 `/home/tetsuya/development/Debug/ServerCore/`，不在仓库内的 `Debug/`。

### 2.2 第一次启动：崩溃

```
Unhandled exception. System.InvalidOperationException:
  Sequence contains no matching element
   at SEnvir.LoadDatabase() SEnvir.cs:line 519
```

第 519 行：
```csharp
GoldInfo = CurrencyInfoList.Binding.First(x => x.Type == CurrencyType.Gold).DropItem;
```

`First()` 在空集合上抛异常——`CurrencyInfoList.Binding` 没有数据。

### 2.3 诊断：不是数据库缺数据，是路径找不到数据库

**先排除"数据库缺 Gold 货币"这个红鲱鱼：**

用 `Tools/SystemDbProbe` 读库，`CurrencyInfo` 有 5 条记录，#1 就是 `Name=Gold, Type=Gold, DropItem=Gold`。数据完全正常。

再用 `Tools/ServerProbe`（本次新建的复现工具，用和 SEnvir 完全相同的调用方式）验证：
```
dotnet run --project Tools/ServerProbe -- /tmp/zircon-server/Database/
→ CurrencyInfo Binding.Count=5
  #1 Name=Gold Type=Gold DropItem=Gold
```

**用绝对路径就能读出来。** 那服务端为什么读不到？

**真根因：MirDB 路径硬编码了 Windows 反斜杠。**

`SEnvir.LoadDatabase`（SEnvir.cs:440）这样创建 Session：
```csharp
Session = new Session(SessionMode.Users)  // 第二个参数 root 用默认值
```

`Session` 构造（Session.cs:75）的默认值是：
```csharp
string root = @".\Database\"
```

然后：
```csharp
Root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, root));
```

**问题**：`Path.Combine` 在 Linux 上不认识反斜杠 `\`，把它当字面字符。所以解析出来的路径是：
```
/tmp/zircon-server/.\Database\    ← Linux 下这是个不存在的"文件名"
```

而不是预期的 `/tmp/zircon-server/Database/`。于是 `System.db exists=False`，集合全空，`First()` 崩溃。

用 ServerProbe 复现确认：
```
dotnet run --project Tools/ServerProbe --   # 不传参数，用默认 .\Database\
→ Root=/tmp/zircon-server/.\Database\
→ System.db exists=False
→ CurrencyInfo Binding.Count=0   ← 复现成功
```

### 2.4 修复：路径规范化（跨平台，3 处改动）

根因层修复——在 `Session` 构造里把反斜杠规范化成平台分隔符。Windows 下 `\` 本来就是分隔符，规范化后不变；Linux/macOS 下 `\` 被替换成 `/`。

**改动 1：`LibraryCore/MirDB/Session.cs`**

加两个静态 helper：
```csharp
private static string NormalizePath(string p) =>
    string.IsNullOrEmpty(p) ? p : p.Replace('\\', System.IO.Path.DirectorySeparatorChar);
private static string ResolvePath(string root) =>
    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NormalizePath(root)));
```

构造函数改用 `ResolvePath`：
```csharp
Root = ResolvePath(root);
BackupRoot = ResolvePath(backup);
```

连接串构造也规范化（防止 ini 里写反斜杠）：
```csharp
Root = NormalizePath(args["ROOT"]);
BackupRoot = NormalizePath(args["BACKUP"]);
```

两个 BackupPath 属性用平台分隔符：
```csharp
public string SystemBackupPath => BackupRoot + "System" + System.IO.Path.DirectorySeparatorChar;
public string UsersBackupPath => BackupRoot + "Users" + System.IO.Path.DirectorySeparatorChar;
```

**改动 2：`ServerLibrary/Envir/Config.cs`**

服务端还直接用了几条 Windows 路径，默认值改成正斜杠（Windows 也接受正斜杠）：
```csharp
[ConfigPath("./Server.ini")]                    // 原 @".\Server.ini"
public static string VersionPath = "./Zircon.dll";  // 原 @".\Zircon.dll"
public static string MapPath = "./Map/";            // 原 @".\Map\"
```

### 2.5 重新启动：成功

```
[13:08:57]: Experience List Loaded.
[13:08:57]: Map loaded: Bichon Town [0]
[13:08:57]: Map loaded: Banya Village [2]
[13:08:58]: Map loaded: Lost Paradise [1]
[13:08:58]: Map loaded: Infernal Island [7]
[13:08:58]: Map loaded: Assassin's Hideout [14_000]
[13:08:58]: Network Started. Listen: 127.0.0.1:7000
[13:08:58]: Loading Time: less than a second
```

### 2.6 TCP 连通验证

```
exec 3<>/dev/tcp/127.0.0.1/7000
→ TCP 127.0.0.1:7000 CONNECTED OK
```

服务端日志记录了连接：
```
[13:09:00]: [Connection] IP Address:127.0.0.1
```

**端到端链路通：服务端监听 → 接受 TCP 连接 → 记录连接。**

## 3. 怎么跑服务端（操作手册）

### 3.1 编译

```bash
cd /home/tetsuya/development/Zircon
dotnet build ServerCore/ServerCore.csproj -c Debug
```

### 3.2 准备工作目录

服务端运行时需要在工作目录下找到 `Database/System.db`、`Map/*.map`、`Config/`。建一个独立工作目录：

```bash
mkdir -p /tmp/zircon-server && cd /tmp/zircon-server
cp /home/tetsuya/development/Debug/ServerCore/* .               # 二进制
ln -s /home/tetsuya/development/Zircon/Debug/Server/Database Database  # 数据库
ln -s /home/tetsuya/development/Zircon/Debug/Client/Map Map           # 地图
```

### 3.3 启动

```bash
cd /tmp/zircon-server
dotnet ServerCore.dll
```

看到 `Network Started. Listen: 127.0.0.1:7000` 就成功了。Ctrl+C 停止。

> 本项目用 `hub start zircon-server` 把它作为长驻进程管理（见讨论 04 及以后）。

## 4. 踩坑总结

| 坑 | 现象 | 根因 | 修复 |
|---|---|---|---|
| 路径分隔符 | `First()` 崩溃，看似"数据库缺数据" | `@".\Database\"` 在 Linux 被 `Path.Combine` 当字面字符，找不到 System.db | Session 构造规范化路径 + Config 默认值改正斜杠 |
| 红鲱鱼 | 先怀疑"System.db 版本旧、缺 Gold 货币" | 库里其实有 5 条 CurrencyInfo，#1 就是 Gold | 用 ServerProbe 传绝对路径验证，数据正常 → 排除 |

**教训**：跨平台移植 .NET 项目时，`@".\xxx\"` 这种 Windows 路径字面量是隐形地雷。`Path.Combine` 不会自动转换分隔符。根因层修复（规范化函数）比逐个改默认值更稳，但两者都做最保险。

## 5. 涉及的代码改动

| 文件 | 改动 | 性质 |
|---|---|---|
| `LibraryCore/MirDB/Session.cs` | 加 `NormalizePath`/`ResolvePath`，构造函数 + BackupPath 用平台分隔符 | 跨平台修复（Windows 无影响） |
| `ServerLibrary/Envir/Config.cs` | `ConfigPath`/`VersionPath`/`MapPath` 默认值 `.\` → `./` | 跨平台修复（Windows 也接受 `/`） |
| `Tools/ServerProbe/`（新增） | 复现 SEnvir 加载数据库的最小工具，用于诊断 | 诊断工具，可保留 |

## 6. 下一步

第 1 步剩余：写个最小 .NET 客户端，复用 `LibraryCore/BaseConnection`，发 `CheckVersion`/`Login` 包，验证**协议链路**端到端通（不只是 TCP 连通）。然后进第 2 步：Godot 客户端骨架。