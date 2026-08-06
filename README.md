# Zircon — Legend of Mir 3（传奇 3）

原项目：[Suprcode/Zircon](https://github.com/Suprcode/Zircon) · 社区：[LOMCN 论坛](http://www.lomcn.org/forum/forumdisplay.php?735)

传奇 3 的 C# 服务端 + 客户端代码库。本仓库为 fork，当前目标：**用 Godot 重写跨平台客户端，连接原版服务端**（先本机 127.0.0.1，后上线/web）。

> 📚 **学习笔记**：[`docs/notes/`](docs/notes/) 记录每次讨论的结论（架构决策、服务端跑通实战、复用边界等），新手向，建议按序号阅读。数据库内容自动生成的文档在 [`docs/database/`](docs/database/)。

---

## 一、为什么用 Godot 重构

原客户端**无法跨平台复用**：

- `Client/`：`net10.0-windows8.0` + `UseWindowsForms`，UI 是 WinForms（DXButton/DXWindow/DXTabControl…），渲染走 SharpDX/D3D11，全部钉死在 Windows 上，在 Godot/Linux 上不存在。
- 客户端渲染逻辑与游戏逻辑纠缠（例如 `Process(S.ObjectMoved)` 里既改坐标又加特效），无法整块搬进 Godot。

但**服务端逻辑是纯 .NET、跨平台、久经线上验证的**，且客户端资产（图库/地图/声音/数据库）已完整下载。因此最优路线不是用 GDScript 重写规则，而是：

> **ServerCore（无头服务端）原样在本机跑，Godot(C#) 客户端通过 TCP 连接它。服务端一行不改，Godot 只负责渲染、输入、UI。** 协议层（packet 序列化 + TCP 收发）已在 `LibraryCore/BaseConnection` 现成跨平台，客户端直接复用。

## 二、复用边界

| 层 | 处置 | 说明 |
|---|---|---|
| `ServerLibrary/` | ✅ 原样复用 | 纯 `net10.0`，只引用 LibraryCore；`System.Drawing` 仅用 Point/Size/Color 值类型（SEnvir.cs 实测：45 Point / 18 Size / 2 Color，无 Bitmap/Graphics），跨平台无碍 |
| `LibraryCore/` | ✅ 原样复用 | MirDB（自研二进制 ORM）、SystemModels、网络 packet 定义 |
| `ServerCore/` | ✅ 原样复用 | 纯 `net10.0` + autofac，ServerLibrary 的无头 host。**已验证 Linux 可跑**，监听 127.0.0.1:7000（见 docs/notes/03） |
| `Server/` | ⛔ 不碰 | DevExpress 可视化编辑器（策划改数值用），`net10.0-windows8.0` + WinForms，仅 Windows |
| `Client/` | ♻️ 部分复用 + 重写 | 网络收发层（继承 BaseConnection）参考复用；WinForms + SharpDX 渲染层在 Godot 全重写 |
| `System.db` | ✅ 直接读 | 未加密（明文头，EncryptionEnabled=false），已下载于 `Debug/Server/Database/System.db` |
| `Data/*.Zl` 图库 | ✅ 直接读 | 已下载于 `Debug/Client/Data/`，格式读取器在 LibraryEditor（WeMadeLibrary/WTLLibrary） |
| `Map/*.map` | ✅ 直接读 | 已下载 |
| `Sound/*.wav` | ✅ 直接用 | 已下载 |

## 三、架构：客户端-服务端分离（本机开发）

原架构就是 TCP 传 packet，我们保持原样，不改服务端：

```
┌──────────────────┐   TCP 127.0.0.1:7000   ┌────────────────────┐
│  ServerCore       │ ◄────────────────────► │  Godot 客户端       │
│  无头服务端进程    │   packet (S.*/C.*)     │  (跨平台, 我们要写)  │
│  原样跑, 零改动    │                        │  渲染/UI/输入全重写  │
└──────────────────┘                        └────────────────────┘
```

- **服务端**：`ServerCore/` 原样跑，监听 7000 端口。本机跑 = 开发体验等同单机，以后上线只改 IP。
- **客户端网络层**：继承 `LibraryCore/BaseConnection`（TCP 收发 + packet 序列化，跨平台现成），参考原 `CConnection` 的收发循环，几十行。
- **客户端渲染/UI/输入**：Godot 全重写。原 `CConnection.Process(S.*)` 把包应用到 WinForms 画面的部分，改写成应用到 Godot 场景节点。
- **登录流程**：原协议现成（Connect → CheckVersion → Login → SelectCharacter → StartGame）。

> 为什么不做单机化（LocalBridge）？因为它反而要动服务端（去 `sealed`、跳网络、线程同步），而在线方案服务端零改动。两者共享 90% 代码（渲染 + 包处理），随时可互转。详见 [`docs/notes/02`](docs/notes/02-架构方向修正-从单机改为在线客户端.md)。

## 四、路线图

| 步骤 | 内容 | 状态 |
|---|---|---|
| 第 0 步 | Linux 编译 ServerLibrary + 读 System.db（验证逻辑层跨平台、数据可读） | ✅ **已完成**：0 警告 0 错误；`Tools/SystemDbProbe` 读出 244 地图 / 309 怪物 / 1078 物品 / 174 魔法 / 1471 刷新点 |
| 第 1 步 | 服务端本机跑起来 + 验证连接 | ✅ **服务端已跑通**：ServerCore 监听 127.0.0.1:7000，TCP 连通验证通过（修复了 MirDB 路径分隔符跨平台坑，见 [`docs/notes/03`](docs/notes/03-服务端在Linux跑通-路径分隔符坑与修复.md)）。剩余：最小客户端走一遍登录协议 |
| 第 2 步 | Godot 客户端骨架：连接服务端 + 登录/选角色 UI（复用 BaseConnection） | ⏳ 未开始 |
| 第 3 步 | Godot 写 `.Zl` / `.map` 读取器（移植自 LibraryEditor），地图与图库渲染 | ⏳ 未开始 |
| 第 4 步 | 逐 packet 接渲染：走路 → 攻击 → 背包 → 魔法，直至可玩 | ⏳ 未开始 |
| 第 5 步（远期） | 客户端导出 web，连远程服务端 | 💤 规划中 |

## 五、环境要求

- **.NET 10 SDK**：`sudo dnf install dotnet-sdk-10.0`（本机已装 10.0.110）
- **Godot 4.x .NET 版**：Fedora 源的是标准版（无 C# 支持），需从官网下载 `_mono_linux_arm64.zip`（本机已装 4.6.3 mono，软链 `~/.local/bin/godot-mono`）。**不要**用标准版打开 C# 工程。
- **ServerDb 探测工具**：`dotnet run --project Tools/SystemDbProbe -- <Database目录>`（验证逻辑层 + 数据可读）
- **启动服务端**：`dotnet build ServerCore/ServerCore.csproj` → 在工作目录放好 `Database/`、`Map/` → `dotnet ServerCore.dll`（详见 docs/notes/03 §3）
- **资产下载**：`bash Tools/download_zircon_assets.sh [目标目录]`（需要 curl；建议装 aria2c 并行下载）

## 六、仓库结构

Client/          原 WinForms 客户端（网络层参考复用，渲染层在 Godot 重写）
Server/          DevExpress 可视化编辑器（Windows only，不参与重构）
ServerLibrary/   服务端核心逻辑 —— 原样复用
ServerCore/      无头服务端 host（Linux 可跑，已验证）
LibraryCore/     共享库：MirDB / SystemModels / 网络 packet（BaseConnection 跨平台）
LibraryEditor/   .Zl 图库编辑器（读取器可移植到 Godot）
RenderingCore/   客户端渲染（Windows only）
Tools/           资产下载脚本 + SystemDbProbe（数据探测）+ ServerProbe（加载复现）
docs/notes/      讨论笔记（学习材料，人工整理）
docs/database/   数据库内容文档（SystemDbProbe 自动生成）
Debug/           构建输出 + 已下载资产（System.db / .Zl / .map / .wav）
```

---

*维护说明：本 README 由中文编写；后续架构决策请同步更新本文档的"复用边界"与"路线图"两节。*
