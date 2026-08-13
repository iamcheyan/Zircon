# 单机模式（Single Player Mode）实现文档（2026-08-13）

## 1. 目标

无需手动启动服务器，双击客户端即可进入游戏做本地 UI 测试：
- 客户端启动时自动拉起本地 ServerCore（进程生命周期绑定）；
- 客户端退出时自动关闭它拉起的 ServerCore；
- 测试账号 TestHero 自动获得满级 + 全部技能 + 全部装备 + 大量金币。

## 2. 架构

```text
客户端 (Godot)
  └─ SinglePlayerLauncher (autoload 节点)
       ├─ EnsureServerRunning(host, port)   // 端口探测 + 拉起 ServerCore
       ├─ WaitForServer(host, port)         // 轮询等待端口就绪 (最多15s)
       └─ Shutdown()                        // 只杀自己拉起的进程 (记 PID)

服务端 (ServerCore)
  └─ --singleplayer-dev 命令行参数
       ├─ Config.SinglePlayerDev = true
       └─ DevSinglePlayer.Apply(player)     // PlayerObject 构造时注入
```

## 3. 客户端改动

### 3.1 `GodotClient/Network/SinglePlayerLauncher.cs`（新增）

| 方法 | 说明 |
|---|---|
| `EnsureServerRunning` | 端口有监听→直连不拉起；无监听→`dotnet ServerCore.dll --singleplayer-dev` 拉起（重定向 stdin 防 Console.ReadLine 阻塞）；远程 `--server` 指定时不触发 |
| `WaitForServer` | 每 250ms 探测端口，最多 15s；进程提前退出则报错 |
| `Shutdown` | 仅杀掉本启动器拉起的进程（`Kill(entireProcessTree)`），外部进程不误杀 |
| `_Notification` | `WMCloseRequest / Predelete / ExitTree` 时自动 Shutdown |

### 3.2 `project.godot`

```text
[autoload]
SinglePlayerLauncher="*res://Network/SinglePlayerLauncher.cs"
```

### 3.3 `LoginScene.cs`

`_Ready` 连接前调用 `EnsureServerRunning` + `WaitForServer`（失败提示"单机服务端启动失败"）。

## 4. 服务端改动

### 4.1 `ServerCore/Program.cs`

解析 `--singleplayer-dev`：启用 `Config.SinglePlayerDev`，并把 `Config.MaxLevel` 提到
`DevSinglePlayer.DevLevel`（255），让 255 级合法。

### 4.2 `ServerLibrary/Envir/Config.cs`

新增 `public static bool SinglePlayerDev = false;`（默认关闭，联机不受影响）。

### 4.3 `ServerLibrary/Envir/DevSinglePlayer.cs`（新增）

`PlayerObject` 构造函数末尾调用 `DevSinglePlayer.Apply(this)`：

1. **等级**：`Level = 255`（走 Level setter + RefreshStats + SetHP/MP/FP）；
2. **全技能**：遍历 `SEnvir.MagicInfoList`，职业可学魔法全部加入
   `Character.Magics` 并 `Level = MagicMaxLevel`（4）；
3. **全装备**：遍历物品库，可穿戴物品（排除 System/Currency/Bundle/LootBox/
   ItemPart/Emblem）全部 `GainItem` 注入背包（GainItem 自动发 S.ItemsGained）；
4. **金币**：`100,000,000`。

幂等：`Level >= 255` 时跳过（防止重复注入）。异常捕获并记日志。

## 5. 实测验证（Xvfb 无头环境）

```text
[Single] 未检测到服务端，启动单机模式：拉起本地 ServerCore ...
[Single] ServerCore 已启动 PID=3541162
[Srv] [SingleDev] 单机开发模式启用: 测试账号将注入满级数据
[Srv] Network Started. Listen: 127.0.0.1:7000
[Login] TCP 已连接 127.0.0.1:7000
[Net] 入队: ItemsGained × N        ← 装备批量注入
```

- 客户端只启动一个进程（无手动服务器）→ 自动拉起 ServerCore ✅
- 登录 TestHero → **LV 255 / HP 9100 / MP 3830 / 金币 100,000,000** ✅
- 背包 6×8 全满（装备+药水+卷轴）✅
- 杀客户端 → ServerCore 进程退出 + 端口释放 ✅

## 6. 使用

```bash
# 方式一：双击客户端（本机无服务端时自动单机）
godot-mono --path GodotClient -- --user test@test.com --pass test123 --char TestHero --window

# 方式二：远程服务器（不触发单机）
godot-mono --path GodotClient -- --server 192.168.3.82 --port 7000 --user ... --pass ... --char ...
```

注意：单机模式修改 TestHero 数据（等级/装备/金币）会持久化到 Users.db，
正常联机登录同一账号时这些改动仍在（如需还原请用 Users.db.empty-backup 恢复）。

## 7. 变更文件

| 文件 | 改动 |
|---|---|
| `GodotClient/Network/SinglePlayerLauncher.cs` | 新增：单机启动器 |
| `GodotClient/project.godot` | autoload 注册 |
| `GodotClient/Scripts/LoginScene.cs` | 连接前拉起+等待 |
| `ServerCore/Program.cs` | `--singleplayer-dev` 解析 |
| `ServerLibrary/Envir/Config.cs` | SinglePlayerDev 标志 |
| `ServerLibrary/Envir/DevSinglePlayer.cs` | 新增：满级注入 |
| `ServerLibrary/Models/PlayerObject.cs` | 构造末尾调用注入 |

## 8. 已知限制

- 单机模式仍是"本机真实服务器"（非进程内模拟）：客户端-服务端走 TCP 7000；
  好处是怪物 AI/掉落/经验全真实，代价是 248MB 内存 + 4 秒启动。
- 若需彻底无服务端进程（演示/分发），需 ServerCore 拆核进程内模拟（另立项目）。
