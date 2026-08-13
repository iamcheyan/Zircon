# C→S 客户端发包协议全量文档（ClientPackets.cs + GeneralPackets.cs）

## TL;DR 速查表

- C→S 包共 **153 个**（`LibraryCore/Network/ClientPackets.cs`），双向通用包 **7 个**（`LibraryCore/Network/GeneralPackets.cs`），合计 160 个类，本文全覆盖。
- **没有 `PacketType` 枚举**（`LibraryCore/Enum.cs` 里搜不到 `enum Packet`）。包 ID 是运行时反射排序索引：General 包按类名排最前（ID 0–6），其余 C/S 包按类名混排。**移植时必须复用同一个 LibraryCore 程序集，绝不能手写 ID**。
- 帧格式：`[int32 小端 长度(含自身4字节)][int16 包ID][字段序列化数据]`，最小 6 字节、最大 64MB（`LibraryCore/Network/Packet.cs:133-136`）。
- 服务端分发：`BaseConnection.ProcessPacket`（`LibraryCore/Network/BaseConnection.cs:396-441`）反射查找 `SConnection.Process(C.XXX)` 方法；**找不到处理方法会抛 NotImplementedException 并断线**（`BaseConnection.cs:443-446`）——`C.GameGoldRecharge` 就是这种死包（无处理、无发送处）。
- 几乎所有游戏内包都有 `if (Stage != GameStage.Game) return;` 阶段守卫，阶段不符时**静默丢弃**（`ServerLibrary/Envir/SConnection.cs:405` 起的统一模式）。
- 登录链：`G.Connected → G.CheckVersion → G.Version → G.GoodVersion → C.SelectLanguage → C.Login → C.NewCharacter/C.DeleteCharacter/C.StartGame`。
- 原版客户端统一入口 `CEnvir.Enqueue(packet)`（`Client/Envir/CEnvir.cs:554-557`）；Godot 客户端对应 `ServerConnection.Enqueue`/`_net.Connection.Enqueue`。
- `CellLinkInfo`（`LibraryCore` 内的格子引用结构）是物品类包的通用货币：`GridType + Slot + Count` 三件套。
- GodotClient 现状：**152/153 个 C 包已有发送路径**（唯一缺的 `GameGoldRecharge` 是原版也没有发送处的死包），实现集中在 `GodotClient/Network/ServerConnection.cs:893-1130` 与 `GodotClient/Scripts/GameScene.cs` 的 Send* 包装方法。

## 职责概述

`LibraryCore/Network/ClientPackets.cs` 定义客户端→服务端的全部请求包（C 前缀命名空间 `Library.Network.ClientPackets`，源码里用 `using C = Library.Network.ClientPackets` 别名引用，见 `ServerLibrary/Envir/SConnection.cs:13`）。每个包是一个 `sealed class XXX : Packet`，只含自动属性（property），由 `Packet.WriteObject/ReadObject` 按属性反射序列化。`GeneralPackets.cs` 定义连接层双向通用包（握手/版本/Ping/断开）。

服务端 `SConnection` 为每个 C 包实现一个 `public void Process(C.XXX p)` 重载，由 `BaseConnection.ProcessPacket` 反射分发；多数处理只是一行转发到 `PlayerObject` 的同名/相关方法。

本文逐包给出：字段定义（照抄）、服务端处理入口（路径:行号 + 逻辑）、原版客户端触发场景（路径:行号）。

## 关键类/文件清单

| 路径 | 行数/范围 | 职责 |
|---|---|---|
| `LibraryCore/Network/ClientPackets.cs` | 862 行 | 153 个 C→S 包定义（7-861 行） |
| `LibraryCore/Network/GeneralPackets.cs` | 27 行 | 7 个双向通用包 |
| `LibraryCore/Network/Packet.cs` | 447 行 | 序列化基类：反射 ID 表（23-119）、帧解码 `ReceivePacket`（121-164）、编码 `GetPacketBytes`（165-187）、`WriteObject`（189-299）/`ReadObject`（300-432） |
| `LibraryCore/Network/BaseConnection.cs` | 473 行 | TCP 收发缓冲、`Process()` 主循环（311-394）、反射分发 `ProcessPacket`（396-441） |
| `ServerLibrary/Envir/SConnection.cs` | 1671 行 | 全部 `Process(C.*)` 处理器（253-1657）；`Process()` 覆写含 Ping/防攻击检查（171-207） |
| `ServerLibrary/Envir/SEnvir.cs` | 4602 行 | 登录/建角色/删角色/进游戏四大大厅逻辑：`Login`(3262)、`NewCharacter`(3814)、`DeleteCharacter`(3971)、`StartGame`(3998) |
| `ServerLibrary/Models/PlayerObject.cs` | 17916 行 | 游戏内动作真正实现：`Turn`(13926)、`Move`(14577)、`Attack`(14714)、`Magic`(14815)、`PickUp`(8582)、`ItemMove`(7435) 等 |
| `Client/Envir/CConnection.cs` | 5110 行 | 原版客户端连接（`BaseConnection` 子类，23 行起），处理 G 包握手 |
| `Client/Envir/CEnvir.cs` | — | `static Enqueue(Packet)`（554-557）转发到 `Connection.Enqueue`，所有客户端发包的统一入口 |
| `GodotClient/Network/ServerConnection.cs` | 1132 行 | Godot 版连接（`BaseConnection` 子类），S→C 用事件通知；C→S 发包包装方法集中在 893-1130 |
| `GodotClient/Network/NetworkManager.cs` | 101 行 | Godot 版连接管理：同步轮询接收替代异步 BeginReceive（25-67） |

## 核心流程

### 1. 连接与登录全链（握手 → 选角 → 进游戏）

```csharp
// 服务端：连接建立即发 G.Connected（SConnection 构造函数，ServerLibrary/Envir/SConnection.cs:63）
Enqueue(new G.Connected());

// 客户端回应（Client/Envir/CConnection.cs:128-133）
public void Process(G.Connected p)
{
    Enqueue(new G.Connected());
    ServerConnected = true;
}

// 服务端：要求校验版本（ServerLibrary/Envir/SConnection.cs:270-284）
public void Process(G.Connected p)
{
    if (Config.CheckVersion)
    {
        Enqueue(new G.CheckVersion());
        return;
    }
    Stage = GameStage.Login;
    Enqueue(new G.GoodVersion
    {
        DatabaseKey = Config.EncryptionEnabled ? SEnvir.CryptoKey : null,
        SystemDatabaseVersion = SEnvir.Session?.RefreshSystemVersion(),
    });
}

// 客户端：回 SHA256(exe.dll)（Client/Envir/CConnection.cs:134-144）
public void Process(G.CheckVersion p)
{
    byte[] clientHash;
    using (SHA256 sha256 = SHA256.Create())
    {
        using (FileStream stream = File.OpenRead(Path.ChangeExtension(Application.ExecutablePath, ".dll")))
            clientHash = sha256.ComputeHash(stream);
    }
    Enqueue(new G.Version { ClientHash = clientHash });
}

// 服务端：校验哈希，通过则 Stage=Login 并下发数据库密钥（ServerLibrary/Envir/SConnection.cs:285-301）
public void Process(G.Version p)
{
    if (Stage != GameStage.None) return;
    if (!Functions.IsMatch(Config.ClientHash, p.ClientHash))
    {
        SendDisconnect(new G.Disconnect { Reason = DisconnectReason.WrongVersion });
        return;
    }
    Stage = GameStage.Login;
    Enqueue(new G.GoodVersion { ... });
}

// 客户端：收到 GoodVersion 后设置加密密钥并上报语言（Client/Envir/CConnection.cs:145-155）
Enqueue(new C.SelectLanguage { Language = Config.Language });
```

之后走账号流程（Login → 选角场景 → NewCharacter/DeleteCharacter/StartGame → 游戏内）。

### 2. 登录（C.Login → SEnvir.Login）

```csharp
// ServerLibrary/Envir/SConnection.cs:350-355
public void Process(C.Login p)
{
    if (Stage != GameStage.Login) return;
    SEnvir.Login(p, this);
}
```

`SEnvir.Login`（`ServerLibrary/Envir/SEnvir.cs:3262-3419`）要点：

- 管理员后门：EMail 不是合法邮箱但密码等于 `Config.MasterPassword` 时，按角色名查账号直接进入（3266-3271）。
- 依次校验 `AllowLogin`、邮箱正则、密码正则、账号存在、已激活、封禁状态（3274-3324）。
- 密码错 5 次自动封号 1 分钟（3330-3338）；密码用 PBKDF2-SHA256 校验（`PasswordMatch`，`SEnvir.cs:4068-4078`）。
- 成功后 `Stage = GameStage.Select`，回 `S.Login { Characters = account.GetSelectInfo() }`。

### 3. 进游戏（C.StartGame → SEnvir.StartGame → PlayerObject）

```csharp
// ServerLibrary/Envir/SEnvir.cs:3998-4032
public static void StartGame(C.StartGame p, SConnection con)
{
    if (!Config.AllowStartGame) { con.Enqueue(new S.StartGame { Result = StartGameResult.Disabled }); return; }
    foreach (CharacterInfo character in con.Account.Characters)
    {
        if (character.Index != p.CharacterIndex) continue;
        if (character.Deleted) { ... Deleted ... }
        TimeSpan duration = Now - character.LastLogin;
        if (duration < Config.RelogDelay) { ... Delayed ... }
        PlayerObject player = new PlayerObject(character, con);   // 4025
        player.StartGame();                                        // 4026
        return;
    }
    con.Enqueue(new S.StartGame { Result = StartGameResult.NotFound });
}
```

### 4. 服务端分发机制（对全部 C 包生效）

```csharp
// LibraryCore/Network/BaseConnection.cs:396-446（节选）
private void ProcessPacket(Packet p)
{
    ...
    info = connectionType.GetMethod("Process", new[] { p.PacketType });
    ...
    if (info == null) { ProcessUnhandledPacket(p); return; }
    info.Invoke(this, new object[] { p });
}
protected virtual void ProcessUnhandledPacket(Packet p)
{
    throw new NotImplementedException($"Not Implemented Exception: Method Process({p.PacketType}).");
}
```

`NotImplementedException` 在 `BaseConnection.Process()` 主循环里被捕获并置 `Disconnecting = true`（`BaseConnection.cs:328-332`）→ **发一个未实现包 = 被踢**。Godot 客户端覆写为只打日志（`GodotClient/Network/ServerConnection.cs:63-67`），不影响 C→S 方向。

另外 `SConnection.Process()` 覆写（`SConnection.cs:171-207`）有两道防攻击闸门：

- 收到 >1024 字节却 0 个完整包 → 判定超大包，封 IP（180-191）；
- 待处理队列 > `Config.MaxPacket` → 封 IP（193-204）。

### 5. 移动（C.Move → PlayerObject.Move）

```csharp
// ServerLibrary/Envir/SConnection.cs:427-440
public void Process(C.Move p)
{
    if (Stage != GameStage.Game) return;
    if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;
    Player.Move(p.Direction, p.Distance);   // PlayerObject.cs:14577
}
```

`Distance`：客户端 `UserObject` 在跑步时传 2、走路传 1（`Client/Models/UserObject.cs:633`，`MoveDistance` 字段）。服务端 `PlayerObject.Move`（`ServerLibrary/Models/PlayerObject.cs:14577`）按 `ActionTime/MoveTime` 冷却、障碍、负重校验后广播 `S.ObjectMove`。

### 6. 拾取（C.PickUp → PlayerObject.PickUp）

```csharp
// ServerLibrary/Models/PlayerObject.cs:8582-8616（节选）
public void PickUp()
{
    if (Dead) return;
    int range = Stats[Stat.PickUpRadius];
    for (int d = 0; d <= range; d++)
        for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
            for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
            {
                ...
                foreach (MapObject cellObject in cell.Objects)
                {
                    if (cellObject.Race != ObjectType.Item) continue;
                    ItemObject item = (ItemObject)cellObject;
                    if (item.PickUpItem(this)) return;   // 拾取一件即停
                }
            }
}
```

从脚下（d=0）按环形向外扫描到 `Stat.PickUpRadius`，碰到第一个可拾 `ItemObject` 即返回。

### 7. 交易五包流程

```
发起方                         接收方                       服务端
C.TradeRequest  ──────────────────────────────▶ Player.TradeRequest()      (SConnection.cs:1151-1156)
                                     S.TradeRequest ──▶ 对方弹窗
               ◀──（对方点确定）──  C.TradeRequestResponse{Accept}
双方各自:  C.TradeAddItem{Cell} ×N  ─▶ Player.TradeAddItem  (SConnection.cs:1172-1177)
          C.TradeAddGold{Gold}    ─▶ Player.TradeAddGold  (SConnection.cs:1178-1183)
          C.TradeConfirm          ─▶ Player.TradeConfirm  (SConnection.cs:1184-1189)
          C.TradeClose            ─▶ Player.TradeClose    (SConnection.cs:1166-1171)
```

## 数据结构/协议细节

### 帧格式（`LibraryCore/Network/Packet.cs:165-187, 121-164`）

```
[int32 长度 N（小端，含这 4 字节自身）][int16 包 ID][属性序列化字节 ...] × 重复（TCP 粘包循环解析）
```

- 解码循环：`BaseConnection.ReceiveData` 把 socket 数据拼接进 `_rawData`，`while ((p = Packet.ReceivePacket(_rawData, out _rawData)) != null) ReceiveList.Enqueue(p)`（`BaseConnection.cs:111-117`）。
- 长度非法（<6 或 >64MB）抛 `InvalidDataException`（`Packet.cs:133-136`）。
- 半包返回 null 留在缓冲区（`Packet.cs:138`）。

### 包 ID 分配机制（重要：没有 PacketType 枚举）

`Packet` 静态构造（`Packet.cs:23-48`）用反射收集**程序集内全部** `Packet` 子类（153 C + 216 S + 7 G = 376 个），排序规则：

```csharp
Packets.Sort((x1, x2) =>
{
    if (String.Compare(x1.Namespace, x2.Namespace, StringComparison.Ordinal) == 0)
        return String.Compare(x1.Name, x2.Name, StringComparison.Ordinal);
    if (string.Compare(x1.Namespace, @"Library.Network.GeneralPackets", StringComparison.Ordinal) == 0)
        return -1;   // General 包置顶
    if (string.Compare(x2.Namespace, @"Library.Network.GeneralPackets", StringComparison.Ordinal) == 0)
        return 1;
    return String.Compare(x1.Name, x2.Name, StringComparison.Ordinal);  // C/S 混排只比类名
});
```

- General 7 个按类名排序占据 **ID 0–6（确定）**：`CheckVersion=0, Connected=1, Disconnect=2, GoodVersion=3, Ping=4, PingResponse=5, Version=6`。
- 其余 369 个 C/S 包按**类名序数排序**混排。注意 153 个 C 包中有 **73 个与 S 包同名**（如 `C.Login`/`S.Login`），比较器对它们返回 0，而 `List.Sort` 非稳定 → 同名对的先后顺序理论上未定义。**因此任何静态 ID 表都不可靠，唯一安全做法是两端复用同一 LibraryCore 程序集**（GodotClient 正是这么做的，`GodotClient/Network/NetworkManager.cs:75` 设置 `Packet.IsClient = true` 后直接用 `Packet.GetPacketBytes()/ReceivePacket()`）。
- 推论：增/删/改名 LibraryCore 里**任何**包类都会平移其后所有包的 ID → 客户端与服务端（含 System.db 版本握手）必须同步发版。

### 字段序列化规则（`Packet.cs:52-115, 189-332`）

| C# 类型 | 线上格式 |
|---|---|
| `bool`/`byte`/`char`/`short`/`int`/`long`/`float`/`double`/`decimal` 等基元 | `BinaryWriter.Write` 原生宽度（小端） |
| `string` | 7-bit 长度前缀 + UTF-8（`BinaryWriter.WriteString`/`ReadString`），null 写成空串（`Packet.cs:81,110`） |
| 枚举 | 按底层类型（默认 `int`，4 字节）：写侧 `TypeWrite[item.PropertyType.GetEnumUnderlyingType()]`（`Packet.cs:208-209`），读侧 `TypeRead[...GetEnumUnderlyingType()]`（`Packet.cs:318-319`） |
| `Color` | `int32 ToArgb()`（`Packet.cs:62,98`） |
| `DateTime` | `int64 ToBinary()`（`Packet.cs:63,99`） |
| `Point` | 两个 `int32` X,Y（`Packet.cs:69-73,106`） |
| `byte[]` | `int32` 长度 + 原始字节（`Packet.cs:56-60,96`） |
| `List<T>` | `int32` count + 逐项；基元项直接写，复杂对象项先写 1 字节非空标记再递归 `WriteObject`（`Packet.cs:210-244, 322-354`） |
| 自定义类属性（如 `CellLinkInfo`） | 1 字节非空标记（`bool`）+ 递归属性序列化（`Packet.cs:201-206, 289-290, 312-316`） |

- 属性顺序即 `GetType().GetProperties()` 顺序（声明顺序）；`[IgnorePropertyPacket]` 标记的属性跳过（`Packet.cs:195,307`）。
- `Packet.PacketType/Length/ObserverPacket` 是**字段**（`Packet.cs:18-21`），不参与序列化。
- `ObserverPacket = true`（默认）的服务端发包会转发给观察者连接（`SConnection.Enqueue` 覆写，`SConnection.cs:209-217`）。

### 通用结构 CellLinkInfo

物品类包大量引用 `CellLinkInfo`（定义于 `LibraryCore`，字段为 `GridType`（网格类型枚举：Inventory/Equipment/Storage/Trade/GuildStorage…）、`Slot`（格子序号）、`Count`（数量，用于堆叠物品部分操作））。客户端构造示例：`new CellLinkInfo { GridType = GridType, Slot = Slot, Count = 1 }`（`Client/Controls/DXItemCell.cs:1662`）。

---

# 按功能分组的包明细

> 表格列「服务端处理」格式：`SConnection.cs:行号 → 转发目标`。所有游戏内包（除特别注明）均有 `if (Stage != GameStage.Game) return;` 守卫；方向类包另有 `if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;` 合法性检查。为省篇幅，表中不重复这两句。

## 一、通用/握手包（GeneralPackets.cs，7 个）

流程：见「核心流程 1」。双向包按实际方向标注。

| 包名 | 字段（照抄） | 方向 | 处理/触发 |
|---|---|---|---|
| `Connected` | （无字段）`public sealed class Connected : Packet { }` | C→S | 服务端 `SConnection.cs:270`（版本开关分流）；客户端收到 G.Connected 后回发，`Client/Envir/CConnection.cs:130`、`GodotClient/Network/ServerConnection.cs:367` |
| `Ping` | （无字段） | C→S | 服务端 `SConnection.cs:302-312`：计算往返延迟、复位 PingSent、回 `G.PingResponse`；客户端被动应答 `CConnection.cs:156-159`、`GodotClient/.../ServerConnection.cs:379`。服务端在 `Process()` 里主动发起（`SConnection.cs:173-178`） |
| `CheckVersion` | （无字段，空类体） | S→C | 服务端发送（`SConnection.cs:274`）；客户端应答见 `Version` |
| `Version` | `public byte[] ClientHash { get; set; }` | C→S | 客户端在 `Process(G.CheckVersion)` 里算 exe.dll 的 SHA256 回发（`Client/Envir/CConnection.cs:134-144`）；服务端 `SConnection.cs:285-301` 校验失败即以 `WrongVersion` 断开 |
| `GoodVersion` | `public byte[] DatabaseKey { get; set; }`<br>`public string SystemDatabaseVersion { get; set; }` | S→C | 服务端发送（`SConnection.cs:279-283, 296-300`）；客户端 `CConnection.cs:145-155` 设置加密密钥并触发 `C.SelectLanguage` |
| `PingResponse` | `public int Ping { get; set; }` | S→C | 服务端发送（`SConnection.cs:311`）；客户端仅更新 Ping 值（`CConnection.cs:160-163`） |
| `Disconnect` | `public DisconnectReason Reason { get; set; }` | 双向 | 客户端发→服务端置 `Disconnecting = true`（`SConnection.cs:266-269`）；服务端发→客户端断开（如 TimedOut，`BaseConnection.cs:343`） |

## 二、账号与登录（9 个）

流程：LoginScene 各表单校验后组包 → `CEnvir.Enqueue` → 服务端 `Stage==Login` 守卫 → `SEnvir` 静态方法处理 → 回 `S.XXX` 结果包。`CheckSum` 字段是客户端指纹（原版取 `CEnvir.C`，Godot 版为 `user://checksum.bin` 持久化 GUID 前 20 位，`GodotClient/Network/ServerConnection.cs:23-31`），服务端仅记日志不打断。

### C.NewAccount（注册，字段逐个说明）

```csharp
public sealed class NewAccount : Packet     // ClientPackets.cs:7-15
{
    public string EMailAddress { get; set; }   // 注册邮箱（登录账号）
    public string Password { get; set; }       // 明文密码（服务端 PBKDF2 落库）
    public DateTime BirthDate { get; set; }    // 生日（仅记录）
    public string RealName { get; set; }       // 真实姓名（仅记录）
    public string Referral { get; set; }       // 推荐人
    public string CheckSum { get; set; }       // 客户端指纹
}
```

- 服务端：`SConnection.cs:314-319` → `SEnvir.NewAccount`（Stage 守卫为 `GameStage.Login`）。
- 客户端：`Client/Scenes/LoginScene.cs:1536-1541`（注册对话框确认按钮）。

### C.ChangePassword（改密）

```csharp
public sealed class ChangePassword : Packet  // ClientPackets.cs:17-23
{
    public string EMailAddress { get; set; }
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public string CheckSum { get; set; }
}
```

服务端 `SConnection.cs:320-325`；客户端 `Client/Scenes/LoginScene.cs:2111-2115`。

### 其余账号包（琐碎，表格化）

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `RequestPasswordReset` | `string EMailAddress`、`string CheckSum`（ClientPackets.cs:25-29） | `SConnection.cs:326-331` → SEnvir 发重置邮件 | `Client/Scenes/LoginScene.cs:2468-2472` |
| `ResetPassword` | `string ResetKey`、`string NewPassword`、`string CheckSum`（31-36） | `SConnection.cs:332-337` → 用邮件里的 Key 改密 | `Client/Scenes/LoginScene.cs:2835-2839` |
| `Activation` | `string ActivationKey`、`string CheckSum`（38-42） | `SConnection.cs:338-343` → 账号激活 | `Client/Scenes/LoginScene.cs:3152-3156` |
| `RequestActivationKey` | `string EMailAddress`、`string CheckSum`（44-48） | `SConnection.cs:344-349` → 补发激活邮件 | `Client/Scenes/LoginScene.cs:3410-3414` |
| `Login` | `string EMailAddress`、`string Password`、`string CheckSum`（55-60） | `SConnection.cs:350-355` → `SEnvir.Login`（SEnvir.cs:3262，详见核心流程 2） | `Client/Scenes/LoginScene.cs:926-930` |
| `Logout` | 无字段（62） | `SConnection.cs:356-384`：Select 阶段回登录页；Game 阶段需脱离战斗 10 秒（370）后 `Player.StopGame()` 回选角；Observer 阶段结束观察 | `Client/Scenes/SelectScene.cs:562`（返回按钮）、`Client/Scenes/Views/ExitDialog.cs:78`（退出对话框） |
| `SelectLanguage` | `string Language`（50-53） | `SConnection.cs:253-265`：按 "ENGLISH"/"CHINESE" 切换服务端消息语言 | `Client/Envir/CConnection.cs:154`（GoodVersion 后）、`Client/Controls/DXConfigWindow.cs:530`（设置界面切换） |

## 三、角色管理（15 个）

流程：选角场景（SelectScene）负责建/删/进游戏；进游戏后的外观修改在 EditCharacterDialog，属性加点（Hermit）与修炼（IncreaseDiscipline）在 CharacterDialog。

### C.NewCharacter（建角色，逐字段）

```csharp
public sealed class NewCharacter : Packet    // ClientPackets.cs:65-74
{
    public string CharacterName { get; set; }      // 角色名（服务端查重/敏感词/长度）
    public MirClass Class { get; set; }            // 职业（Warrior/Wizard/Taoist/Assassin/Archer）
    public MirGender Gender { get; set; }          // 性别
    public int HairType { get; set; }              // 发型编号
    public Color HairColour { get; set; }          // 发色（ARGB int32 上线）
    public Color ArmourColour { get; set; }        // 初始衣服颜色
    public string CheckSum { get; set; }           // 客户端指纹
}
```

- 服务端：`SConnection.cs:386-391` → `SEnvir.NewCharacter`（`SEnvir.cs:3814-3970`，校验 `Config.AllowNewCharacter`、名字规则、角色数上限，成功追加 `SelectInfo`）。
- 客户端：`Client/Scenes/SelectScene.cs:1255-1259`（建角对话框确认）。

### 其余角色包

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `DeleteCharacter` | `int CharacterIndex`、`string CheckSum`（76-80） | `SConnection.cs:392-397` → `SEnvir.DeleteCharacter`（SEnvir.cs:3971） | `Client/Scenes/SelectScene.cs:596`（删除确认框） |
| `StartGame` | `int CharacterIndex`（82-85） | `SConnection.cs:398-404` → `SEnvir.StartGame`（SEnvir.cs:3998，详见核心流程 3） | `Client/Scenes/SelectScene.cs:554-557`（双击角色/开始按钮） |
| `Inspect` | `int Index`、`bool Ranking`（398-402） | `SConnection.cs:808-815` → `PlayerObject.Inspect`（PlayerObject.cs:1959）：查玩家/排行数据回 `S.Inspect`；Observer 阶段也可用 | `Client/Scenes/Views/MapControl.cs:785`（右键玩家）、`Client/Scenes/Views/RankingDialog.cs:1161,1745`（排行榜行） |
| `RankRequest` | `RequiredClass Class`、`bool OnlineOnly`、`int StartIndex`（404-409） | `SConnection.cs:816-822` → 分页拉排行榜（Game/Observer/Login 三阶段均允许） | `Client/Scenes/Views/RankingDialog.cs:1096-1100` |
| `RankSearch` | `string Name`（411-414） | `SConnection.cs:823-877`（大段：按名查排行并回包） | `Client/Scenes/Views/RankingDialog.cs:776`（搜索框） |
| `HelmetToggle` | `bool HideHelmet`（697-700） | `SConnection.cs:1413-1419` → 切换是否显示头盔 | `Client/Controls/DXConfigWindow.cs:581`（显示头盔复选框） |
| `GenderChange` | `MirGender Gender`、`int HairType`、`Color HairColour`（702-707） | `SConnection.cs:1420-1426` → 变性（消耗道具/等级判定在 PlayerObject） | `Client/Scenes/Views/EditCharacterDialog.cs:549` |
| `HairChange` | `int HairType`、`Color HairColour`（709-713） | `SConnection.cs:1427-1433` → 改发型 | `Client/Scenes/Views/EditCharacterDialog.cs:552` |
| `ArmourDye` | `Color ArmourColour`（715-718） | `SConnection.cs:1434-1439` → 染色 | `Client/Scenes/Views/EditCharacterDialog.cs:555` |
| `NameChange` | `string Name`（720-723） | `SConnection.cs:1440-1446` → 改名（检查重名） | `Client/Scenes/Views/EditCharacterDialog.cs:558` |
| `CaptionChange` | `string Caption`（725-728） | `SConnection.cs:1447-1453` → 改称号 | `Client/Scenes/Views/CaptionDialog.cs:92` |
| `Hermit` | `Stat Stat`（426-429） | `SConnection.cs:908-914` → 潜能点加到指定属性（MaxAC/MaxMR/Health/Mana/MaxDC/MaxMC/MaxSC/WeaponElement 八个按钮） | `Client/Scenes/Views/CharacterDialog.cs:1926,1931,1953,1958,1982,1987,2009,2014,2038,2043,2065,2070,2094,2099,2121,2126` |
| `IncreaseDiscipline` | 无字段（804-806） | `SConnection.cs:1577-1583` → 提升修炼等级（消耗修炼点） | `Client/Scenes/Views/CharacterDialog.cs:2477` |
| `ChangeOnlineState` | `OnlineState State`（790-793） | `SConnection.cs:1523-1531` → 切换在线状态（Online/Busy/Away），影响好友列表显示 | `Client/Scenes/Views/CommunicationDialog.cs:508` |

## 四、观察模式（2 个）

流程：排行/观战入口 `ObserverRequest` 附着到目标玩家连接（Observers 列表），此后服务端 `Enqueue` 时自动复制包给观察者（`SConnection.cs:209-217`）；被观察者可用 `ObservableSwitch` 拒绝。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `ObserverRequest` | `string Name`（416-419） | `SConnection.cs:878-900`：找在线玩家 → 校验 `Config.AllowObservation`/目标 `Observable` → 停自己的游戏 → `player.SetUpObserver(this)` | `Client/Scenes/Views/RankingDialog.cs:802`（观战按钮） |
| `ObservableSwitch` | `bool Allow`（421-424） | `SConnection.cs:901-906` → 开关"允许观战" | `Client/Controls/DXConfigWindow.cs:793`（设置复选框） |

## 五、移动与传送（10 个）

流程：客户端 `UserObject.AttemptAction`（`Client/Models/UserObject.cs:620-702`）按动作类型统一发包——Turn/Harvest/Move/Attack/RangeAttack/Magic/FishingCast/Mining/Taming 全在这一个 switch 里，发完设置 `NextActionTime` 冷却；服务端 `PlayerObject` 对应方法再校验冷却/碰撞后广播。自动寻路由 `AutoPathService`（服务端）驱动，客户端只发四个控制包。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `Turn` | `MirDirection Direction`（89-92） | `SConnection.cs:411-418` → `PlayerObject.Turn`（PlayerObject.cs:13926） | `Client/Models/UserObject.cs:621` |
| `Move` | `MirDirection Direction`、`int Distance`（99-103） | `SConnection.cs:427-440` → `PlayerObject.Move`（PlayerObject.cs:14577）；Distance=1 走/2 跑（见核心流程 5） | `Client/Models/UserObject.cs:633` |
| `Mount` | 无字段（105） | `SConnection.cs:473-478` → 上/下马（有马匹时） | `Client/Scenes/GameScene.cs:1389`（按键 MountToggle） |
| `AutoPathStart` | `int NPCIndex`（107-110） | `SConnection.cs:441-453`：按 NPC 索引找 NPCInfo，`AutoPathService.Instance.TryStart(Player, npc)` 启动服务端寻路 | `Client/Scenes/Views/BigMapDialog.cs:564,727`（大地图点 NPC） |
| `AutoPathWaypoint` | `int MapIndex`、`Point Location`（112-116） | `SConnection.cs:454-460` → `AutoPathService.TryAddWaypoint` 添加途经点 | `Client/Scenes/Views/BigMapDialog.cs:640-644`（大地图点格子） |
| `AutoPathCancel` | 无字段（118） | `SConnection.cs:461-466` → `AutoPathService.Cancel` | `Client/Scenes/GameScene.AutoPath.cs:110` |
| `AutoPathMoveStarted` | 无字段（120） | `SConnection.cs:467-472` → `AutoPathService.MoveStarted`（通知服务端客户端动画已开始） | `Client/Models/UserObject.cs:434,443`（跑步开始时） |
| `TeleportRing` | `Point Location`、`int Index`（735-739） | `SConnection.cs:1461-1467` → 传送戒指：飞到 Index 地图的 Location | `Client/Scenes/Views/BigMapDialog.cs:630`（大地图传送） |
| `MarriageTeleport` | 无字段（683-686） | `SConnection.cs:1364-1370` → 传送到配偶身边 | `Client/Scenes/GameScene.cs:1380`（按键）、`Client/Controls/DXItemCell.cs:2506,2571`（右键结婚戒指） |
| `JoinInstance` | `int Index`（779-782） | `SConnection.cs:1509-1515` → 进入副本（单人/组队/攻城类型在客户端分流） | `Client/Scenes/Views/DungeonFinderDialog.cs:377,403,416,426` |

## 六、战斗与动作（12 个）

流程：客户端 `CombatController`/`UserObject` 判定攻击间隔后发 `Attack`；服务端 `PlayerObject.Attack`（`ServerLibrary/Models/PlayerObject.cs:14714`）做冷却/坐骑/姿势校验，取武器攻击范围格子，经 `Attack(MapObject, types, primary, extra)`（PlayerObject.cs:15205）结算伤害并广播 `S.ObjectAttack`。魔法走 `C.Magic` → `PlayerObject.Magic`（PlayerObject.cs:14815）→ 按 `MagicType` 派发 `MagicObject` 子类。

### C.Attack（近战，逐字段）

```csharp
public sealed class Attack : Packet          // ClientPackets.cs:142-147
{
    public MirDirection Direction { get; set; }     // 朝向（8 方向）
    public MirAction Action { get; set; }           // 动作类型（客户端预演的挥击动作）
    public MagicType AttackMagic { get; set; }      // 攻击附带的技能（如烈火剑气; 普攻为 None）
}
```

- 服务端：`SConnection.cs:501-508` → `PlayerObject.Attack(p.Direction, p.AttackMagic)`（PlayerObject.cs:14714）。
- 客户端：`Client/Models/UserObject.cs:644`（`MirAction.Attack` 分支）。

### C.Magic（施法，逐字段）

```csharp
public sealed class Magic : Packet           // ClientPackets.cs:159-166
{
    public MirDirection Direction { get; set; }   // 施法朝向
    public MirAction Action { get; set; }         // 固定 MirAction.Spell
    public MagicType Type { get; set; }           // 技能类型（决定 MagicObject 分发）
    public uint Target { get; set; }              // 目标 ObjectID（指向技能; 无目标传 0）
    public Point Location { get; set; }           // 落点（范围技能; Point.Empty 表示无）
}
```

- 服务端：`SConnection.cs:518-525` → `PlayerObject.Magic(p)`（PlayerObject.cs:14815：`GetMagic(p.Type)` 找不到直接忽略）。
- 客户端：`Client/Models/UserObject.cs:664`（AttemptAction 分支，带目标/落点）、`Client/Scenes/GameScene.cs:3181,3205`（快捷键施法：无目标技能只传 Direction+Action+Type）。

### 其余战斗包

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `RangeAttack` | `MirDirection Direction`、`uint Target`（149-153） | `SConnection.cs:510-517` → `PlayerObject.RangeAttack`（PlayerObject.cs:15116，持弓/暗器远程普攻） | `Client/Models/UserObject.cs:655` |
| `MagicToggle` | `MagicType Magic`、`bool CanUse`（354-358） | `SConnection.cs:526-531` → `PlayerObject.MagicToggle`（PlayerObject.cs:14921）：切换攻杀剑术/半月/刺杀等被动开关 | `Client/Scenes/GameScene.cs:3012,3017,3022,3032,3037`（各技能开关键） |
| `Mining` | `MirDirection Direction`（154-157） | `SConnection.cs:532-539` → `PlayerObject.Mining`（PlayerObject.cs:14935，挖矿有独立冷却与超重惩罚） | `Client/Models/UserObject.cs:690` |
| `Harvest` | `MirDirection Direction`（94-97） | `SConnection.cs:419-426` → `PlayerObject.Harvest`（PlayerObject.cs:13968，采集尸体） | `Client/Models/UserObject.cs:627` |
| `FishingCast` | `FishingState State`、`MirDirection Direction`、`Point FloatLocation`、`bool CaughtFish`（122-128） | `SConnection.cs:480-485` → `PlayerObject.FishingCast`：钓鱼状态机（Cast/Wait/Catch/Cancel），CaughtFish 由服务端通知后客户端确认 | `Client/Models/UserObject.cs:675` |
| `Taming` | `TamingState State`、`uint ObjectID`、`MirDirection Direction`（130-135） | `SConnection.cs:487-492` → `PlayerObject.Taming`：套索驯马状态机 | `Client/Models/UserObject.cs:699` |
| `TamingSuccess` | `uint ObjectID`（137-140） | `SConnection.cs:494-499` → `PlayerObject.TamingSuccess`：驯服成功确认 | `Client/Scenes/Views/HorseTameDialog.cs:287`（小游戏完成） |
| `TownRevive` | 无字段（87） | `SConnection.cs:405-410` → `PlayerObject.TownRevive`（PlayerObject.cs:1443）：死亡后回城复活（扣经验/等待时间判定在 PlayerObject） | `Client/Scenes/Views/ChatTab.cs:440`（聊天窗"回城复活"链接） |
| `ChangeAttackMode` | `AttackMode Mode`（515-518） | `SConnection.cs:1104-1119`：合法值（Peace/Group/Guild/WarRedBrown/All）存 `Player.AttackMode` 并回 `S.ChangeAttackMode` | `Client/Scenes/GameScene.cs:1335`（按键循环切换） |
| `ChangePetMode` | `PetMode Mode`（519-522） | `SConnection.cs:1120-1136`：合法值（Both/Move/Attack/PvP/None）存 `Player.PetMode` 并回 `S.ChangePetMode` | `Client/Scenes/GameScene.cs:1341` |

## 七、物品与背包（19 个）

流程：所有格子操作在 `DXItemCell`（`Client/Controls/DXItemCell.cs`）拖拽/右键处理里发 `ItemMove/ItemSplit/ItemLock`；使用在 `UseItem`（同文件 1661-1702 的 ItemType 分支）分流为 `ItemUse`（消耗品/书籍）或 `BundleOpen/LootBoxOpen` 二段式开箱。服务端 `PlayerObject.ItemMove`（PlayerObject.cs:7435）构造 `S.ItemMove` 结果统一回执。`PickUp` 见核心流程 6。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `ItemMove` | `GridType FromGrid`、`GridType ToGrid`、`int FromSlot`、`int ToSlot`、`bool MergeItem`（168-175） | `SConnection.cs:541-546` → `PlayerObject.ItemMove`（PlayerObject.cs:7435）：跨网格搬动/合并/交换，回 `S.ItemMove` | `Client/Controls/DXItemCell.cs:917,946,1078,1106`（拖拽四种落点） |
| `ItemSort` | `GridType Grid`（177-180） | `SConnection.cs:547-552` → 服务端整理背包/仓库 | `Client/Scenes/Views/InventoryDialog.cs:352`、`Client/Scenes/Views/StorageDialog.cs:329`（整理按钮） |
| `ItemDelete` | `GridType Grid`、`int Slot`（182-186） | `SConnection.cs:553-558` → 销毁物品（有确认） | `Client/Scenes/Views/InventoryDialog.cs:370` |
| `ItemSplit` | `GridType Grid`、`int Slot`、`long Count`（188-193） | `SConnection.cs:1137-1142` → 拆分堆叠 | `Client/Controls/DXItemCell.cs:1879`（Shift 拖拽输入数量） |
| `ItemDrop` | `CellLinkInfo Link`、`int Slot`（195-199） | `SConnection.cs:559-564` → 丢物品到地面 | `Client/Scenes/Views/MapControl.cs:646-649`（拖到地图上） |
| `CurrencyDrop` | `int CurrencyIndex`、`long Amount`（201-205） | `SConnection.cs:571-576` → 丢弃货币 | `Client/Scenes/Views/MapControl.cs:670-674` |
| `ItemUse` | `CellLinkInfo Link`（207-210） | `SConnection.cs:577-585` → `PlayerObject.ItemUse`（PlayerObject.cs:6335）：`ParseLinks` 校验归属后按 ItemType 生效（吃药/穿装备/学书） | `Client/Controls/DXItemCell.cs:1662,1699`（双击消耗品/书） |
| `ItemLock` | `GridType GridType`、`int SlotIndex`、`bool Locked`（212-217） | `SConnection.cs:1143-1150` → 物品上锁（防误卖/误丢） | `Client/Controls/DXItemCell.cs:1898,2593`（Ctrl+点击） |
| `PickUp` | 无字段（235） | `SConnection.cs:565-570` → `PlayerObject.PickUp`（PlayerObject.cs:8582，见核心流程 6） | `Client/Scenes/GameScene.cs:1373`（按键，250ms 节流）、`Client/Scenes/Views/MapControl.cs:978`（点击地面物品） |
| `BeltLinkChanged` | `int Slot`、`int LinkIndex`、`int LinkItemIndex`（219-224） | `SConnection.cs:586-591` → 更新快捷栏绑定（持久化） | `Client/Controls/DXItemCell.cs:995,1002,1034`、`Client/Scenes/Views/MapControl.cs:620`、`Client/Envir/CConnection.cs:2190` 等十余处（凡 belt 格变动即发） |
| `AutoPotionLinkChanged` | `int Slot`、`int LinkIndex`、`int Health`、`int Mana`、`bool Enabled`（226-233） | `SConnection.cs:592-598` → `PlayerObject.AutoPotionLinkChanged`（PlayerObject.cs:8551）：自动喝药阈值/开关 | `Client/Scenes/Views/AutoPotionDialog.cs:356` |
| `FortuneCheck` | `int ItemIndex`（730-733） | `SConnection.cs:1454-1460` → 查物品财运掉率信息 | `Client/Scenes/Views/FortuneCheckerDialog.cs:476` |
| `LootBoxOpen` | `int Slot`（808-811） | `SConnection.cs:1584-1590` → 开盲盒（第一段：请求生成选项） | `Client/Controls/DXItemCell.cs:1688` |
| `LootBoxReroll` | `int Slot`（813-816） | `SConnection.cs:1591-1597` → 重摇选项 | `Client/Scenes/Views/LootBoxDialog.cs:137` |
| `LootBoxConfirmSelection` | `int Slot`（818-821） | `SConnection.cs:1598-1604` → 确认选项锁定 | `Client/Scenes/Views/LootBoxDialog.cs:183` |
| `LootBoxReveal` | `int Slot`、`int Choice`（823-827） | `SConnection.cs:1605-1611` → 翻牌揭示 | `Client/Scenes/Views/LootBoxDialog.cs:331,336` |
| `LootBoxTakeItems` | `int Slot`、`int Choice`（829-833） | `SConnection.cs:1612-1618` → 领取盲盒物品 | `Client/Scenes/Views/LootBoxDialog.cs:161` |
| `BundleOpen` | `int Slot`（835-838） | `SConnection.cs:1619-1625` → 打开礼包（选一） | `Client/Controls/DXItemCell.cs:1675` |
| `BundleConfirm` | `int Slot`、`int Choice`（840-844） | `SConnection.cs:1626-1632` → 确认礼包选择 | `Client/Scenes/Views/BundleDialog.cs:106` |

## 八、技能（1 个）

`MagicToggle` 已列于战斗组；此处是技能键位持久化包。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `MagicKey` | `MagicType Magic`、`SpellKey Set1Key`、`SpellKey Set2Key`、`SpellKey Set3Key`、`SpellKey Set4Key`（344-352） | `SConnection.cs:722-749`：先把所有技能上与新键冲突的键位清成 `SpellKey.None`，再把四个键位写到该技能（防止一键多技能） | `Client/Scenes/Views/CharacterDialog.cs:3442,3598`、`Client/Scenes/Views/MagicDialog.cs:623,781`（绑键对话框确认） |

## 九、聊天（1 个）

### C.Chat（逐字段）

```csharp
public sealed class Chat : Packet            // ClientPackets.cs:237-241
{
    public string Text { get; set; }                    // 聊天文本；'@' 开头为命令、'/' 开头为私聊、'!' 组队、'@#' 行会（服务端 Player.Chat 分流）
    public List<int> LinkedItemIndexes { get; set; }    // 超链接物品的 ItemInfo.Index 列表（Shift+点击物品展示）
}
```

- 服务端：`SConnection.cs:599-608`：空文本或长度 > `Globals.MaxChatLength` **静默丢弃**（601）；Game 阶段走 `Player.Chat(text, linkedItems)`，Observer 阶段走 `Observed.Player.ObserverChat`。
- 客户端：`Client/Scenes/Views/ChatTextBox.cs:150-154`（回车发送）；`Client/Scenes/GameScene.cs:1363`（`@AllowTrade` 等命令也走此包）。

## 十、组队（7 个）

流程：队长侧 `GroupInvite`（按键或组队面板）→ 对方 `CConnection` 弹窗（`Client/Envir/CConnection.cs:3451-3462`）→ `GroupResponse` 应答 → 服务端 `GroupJoin/GroupDecline`。LFG（寻找队伍）频道由 `GroupNotify` 订阅、`GroupLFGUpdate` 发布、`GroupRequest` 申请入队。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `GroupSwitch` | `bool Allow`（360-363） | `SConnection.cs:751-756` → `PlayerObject.GroupSwitch`：允许/拒绝组队邀请 | `Client/Scenes/Views/GroupDialog.cs:263` |
| `GroupInvite` | `string Name`（365-368） | `SConnection.cs:757-762` → `PlayerObject.GroupInvite`（PlayerObject.cs:5667）：向目标发邀请 | `Client/Scenes/GameScene.cs:1353`（按键）、`Client/Scenes/Views/GroupDialog.cs:327`、`Client/Envir/CConnection.cs:3461`（弹窗"邀请"）、`Client/Scenes/Views/GuildDialog.cs:2666`（行会成员右键） |
| `GroupRemove` | `string Name`（370-373） | `SConnection.cs:763-768` → `PlayerObject.GroupRemove`：踢人/退队 | `Client/Scenes/Views/GroupDialog.cs:344` |
| `GroupResponse` | `string Name`、`bool Accept`（375-379） | `SConnection.cs:770-780`：Accept→`GroupJoin`，否则 `GroupDecline(p.Name)`；清空 `GroupInvitation` | `Client/Envir/CConnection.cs:3451-3452,3462`（组队邀请弹窗） |
| `GroupRequest` | `string Name`（380-383） | `SConnection.cs:794-799` → `PlayerObject.GroupRequest`：申请加入 LFG 队伍 | `Client/Scenes/Views/GroupDialog.cs:506` |
| `GroupLFGUpdate` | `bool Enabled`、`string Name`、`string Type`、`int MaxCount`（385-391） | `SConnection.cs:801-806` → `PlayerObject.LFGUpdate`：发布/撤销招募 | `Client/Scenes/Views/GroupDialog.cs:374,378`（发布/停用按钮） |
| `GroupNotify` | `bool Receive`（393-396） | `SConnection.cs:782-792` → 设置 `LFGSettings.ReceiveUpdates`，开启即下发当前 LFG 列表 | `Client/Scenes/Views/GroupDialog.cs:153`（面板显隐切换） |

## 十一、交易（6 个）

流程见「核心流程 7」。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `TradeRequest` | 无字段（527-529） | `SConnection.cs:1151-1156` → `PlayerObject.TradeRequest`：面向目标发起交易（目标须开启允许交易） | `Client/Scenes/GameScene.cs:1358`（按键） |
| `TradeRequestResponse` | `bool Accept`（530-533） | `SConnection.cs:1157-1165`：Accept→`TradeAccept`（双方接受才开窗）；清 `TradePartnerRequest` | `Client/Envir/CConnection.cs:3775-3776`（交易邀请弹窗） |
| `TradeClose` | 无字段（534-537） | `SConnection.cs:1166-1171` → `PlayerObject.TradeClose`：中断交易 | `Client/Scenes/Views/TradeDialog.cs:32`（关窗） |
| `TradeAddItem` | `CellLinkInfo Cell`（542-545） | `SConnection.cs:1172-1177` → `PlayerObject.TradeAddItem`：放物品进交易栏 | `Client/Scenes/Views/TradeDialog.cs:125-128` |
| `TradeAddGold` | `long Gold`（538-541） | `SConnection.cs:1178-1183` → `PlayerObject.TradeAddGold`：放金币 | `Client/Scenes/Views/TradeDialog.cs:256` |
| `TradeConfirm` | 无字段（546-549） | `SConnection.cs:1184-1189` → `PlayerObject.TradeConfirm`：双方确认后服务端原子交换 | `Client/Scenes/Views/TradeDialog.cs:240` |

## 十二、行会（17 个）

流程：无行会时 `JoinStarterGuild`（免费新手行会）或 `GuildCreate`（付费）；管理层通过 `GuildEditNotice/GuildEditMember/GuildInviteMember/GuildKickMember` 管理；攻城相关 `GuildRequestConquest/GuildToggleCastleGates/GuildRepairCastleGates/GuildRepairCastleGuards`。邀请应答 `GuildResponse`（弹窗在 `Client/Envir/CConnection.cs:4058-4060`）。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `GuildCreate` | `string Name`、`bool UseGold`、`int Members`、`int Storage`（551-557） | `SConnection.cs:1191-1196` → 建会（UseGold=金币/宝石，Members/Storage 为初始容量档位） | `Client/Scenes/Views/GuildDialog.cs:854-858` |
| `GuildEditNotice` | `string Notice`（558-561） | `SConnection.cs:1197-1202` → 改公告 | `Client/Scenes/Views/GuildDialog.cs:1006` |
| `GuildEditMember` | `int Index`、`string Rank`、`GuildPermission Permission`（562-568） | `SConnection.cs:1203-1208` → 改成员职位/权限 | `Client/Scenes/Views/GuildDialog.cs:2934` |
| `GuildInviteMember` | `string Name`（569-572） | `SConnection.cs:1227-1232` → 邀请入会 | `Client/Scenes/Views/GuildDialog.cs:1313` |
| `GuildKickMember` | `int Index`（573-576） | `SConnection.cs:1233-1238` → 踢人 | `Client/Scenes/Views/GuildDialog.cs:2952` |
| `GuildTax` | `long Tax`（577-580） | `SConnection.cs:1209-1214` → 设置成员日税率 | `Client/Scenes/Views/GuildDialog.cs:1190` |
| `GuildIncreaseMember` | 无字段（581-584） | `SConnection.cs:1215-1220` → 扩容成员上限（消耗资金） | `Client/Scenes/Views/GuildDialog.cs:1351` |
| `GuildIncreaseStorage` | 无字段（585-588） | `SConnection.cs:1221-1226` → 扩容仓库 | `Client/Scenes/Views/GuildDialog.cs:1526` |
| `GuildResponse` | `bool Accept`（589-592） | `SConnection.cs:1239-1247` → 入会邀请应答 | `Client/Envir/CConnection.cs:4058-4060`（弹窗三个按钮） |
| `GuildWar` | `string GuildName`（594-597） | `SConnection.cs:1248-1253` → 宣战 | `Client/Scenes/Views/GuildDialog.cs:1640` |
| `GuildRequestConquest` | `int Index`（599-602） | `SConnection.cs:1254-1260` → 申请攻打城堡 | `Client/Scenes/Views/GuildDialog.cs:3201` |
| `GuildColour` | `Color Colour`（604-607） | `SConnection.cs:1261-1266` → 改会徽底色 | `Client/Scenes/Views/GuildDialog.cs:1739` |
| `GuildFlag` | `int Flag`（609-612） | `SConnection.cs:1267-1272` → 改会徽图案 | `Client/Scenes/Views/GuildDialog.cs:1777,1794` |
| `GuildToggleCastleGates` | 无字段（614-617） | `SConnection.cs:1273-1278` → 开/关城门 | `Client/Scenes/Views/GuildDialog.cs:1857` |
| `GuildRepairCastleGates` | 无字段（619-622） | `SConnection.cs:1279-1284` → 修城门 | `Client/Scenes/Views/GuildDialog.cs:1880` |
| `GuildRepairCastleGuards` | 无字段（624-627） | `SConnection.cs:1285-1291` → 修守卫 | `Client/Scenes/Views/GuildDialog.cs:1904` |
| `JoinStarterGuild` | 无字段（741-744） | `SConnection.cs:1468-1473` → 加入新手行会 | `Client/Scenes/Views/GuildDialog.cs:845-847` |

## 十三、NPC 交互/商店/宠物（28 个）

流程：点击 NPC → `NPCCall`（服务端 NPC 对话页 `S.NPCResponse`）→ 对话按钮 `NPCButton` → 按页面类型分流：商店 `NPCBuy/NPCSell/NPCRepair`、碎片 `NPCFragment`、精炼系（`NPCRefine/NPCMasterRefine/NPCRefinementStone/NPCAccessory*`）、宝石镶嵌 `NPCSocketItem/NPCSocketCombine`、武器打造 `NPCWeaponCraft`、摇骰 `NPCRoll/NPCRollResult`、宠物领养 `Companion*`。`NPCClose` 关闭对话。所有 NPC 包共享"当前对话页"上下文（`Player.NPC`/`Player.NPCPage`，`SConnection.cs:614,645` 的日志可见），离开 NPC 后发这些包会被 PlayerObject 侧拒绝。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `NPCCall` | `uint ObjectID`（243-246） | `SConnection.cs:610-619` → `PlayerObject.NPCCall`：打开 NPC 对话首页 | `Client/Scenes/Views/MapControl.cs:767`（点击 NPC） |
| `NPCButton` | `int ButtonID`（248-251） | `SConnection.cs:621-626` → `PlayerObject.NPCButton`：点对话按钮翻页/执行脚本动作 | `Client/Scenes/Views/NPCDialog.cs:610` |
| `NPCRoll` | `int Type`（253-256） | `SConnection.cs:628-633` → `PlayerObject.NPCRoll`：掷骰（0=骰子 1=尤茨） | `Client/Scenes/Views/NPCDialog.cs:487,491` |
| `NPCRollResult` | 无字段（258-260） | `SConnection.cs:634-639` → `PlayerObject.NPCRollResult`：客户端动画结束确认 | `Client/Scenes/Views/NPCDialog.cs:8477` |
| `NPCBuy` | `int Index`、`long Amount`、`bool GuildFunds`（262-267） | `SConnection.cs:641-648` → `PlayerObject.NPCBuy`：买商品（GuildFunds=用行会资金） | `Client/Scenes/Views/NPCDialog.cs:932,950`（双击/确认购买） |
| `NPCSell` | `List<CellLinkInfo> Links`（269-273） | `SConnection.cs:649-654` → `PlayerObject.NPCSell`：批量卖物 | `Client/Scenes/Views/InventoryDialog.cs:450` |
| `NPCFragment` | `List<CellLinkInfo> Links`（275-279） | `SConnection.cs:703-708` → 分解装备成碎片 | `Client/Scenes/Views/NPCDialog.cs:5557` |
| `NPCRepair` | `List<CellLinkInfo> Links`、`bool Special`、`bool GuildFunds`（281-286） | `SConnection.cs:655-660` → 修理（Special=特殊修理保强化） | `Client/Scenes/Views/NPCDialog.cs:1615` |
| `NPCRefine` | `RefineType RefineType`、`RefineQuality RefineQuality`、`List<CellLinkInfo> Ores`、`List<CellLinkInfo> Items`、`List<CellLinkInfo> Specials`（288-295） | `SConnection.cs:667-672` → 提交武器精炼材料 | `Client/Scenes/Views/NPCDialog.cs:2071` |
| `NPCMasterRefine` | `RefineType RefineType`、`List<CellLinkInfo> Fragment1s`、`Fragment2s`、`Fragment3s`、`Stones`、`Specials`（296-304） | `SConnection.cs:679-684` → 大师精炼（三档碎片+石头+特殊材料） | `Client/Scenes/Views/NPCDialog.cs:6107` |
| `NPCMasterRefineEvaluate` | 同 `NPCMasterRefine`（305-313） | `SConnection.cs:685-690` → 只评估成功率不实际精炼 | `Client/Scenes/Views/NPCDialog.cs:6009` |
| `NPCRefinementStone` | `List<CellLinkInfo> IronOres`、`SilverOres`、`DiamondOres`、`GoldOres`、`Crystal`、`long Gold`（314-322） | `SConnection.cs:661-666` → 合成精炼石 | `Client/Scenes/Views/NPCDialog.cs:5326` |
| `NPCClose` | 无字段（324-326） | `SConnection.cs:691-702` → `PlayerObject.NPCClose`：关闭对话清上下文 | `Client/Scenes/Views/NPCDialog.cs:127` |
| `NPCRefineRetrieve` | `int Index`（328-331） | `SConnection.cs:673-678` → 取回精炼完成的武器 | `Client/Scenes/Views/NPCDialog.cs:2403` |
| `NPCAccessoryLevelUp` | `CellLinkInfo Target`、`List<CellLinkInfo> Links`（333-337） | `SConnection.cs:709-714` → 饰品升级（吞同名饰品） | `Client/Scenes/Views/NPCDialog.cs:6977` |
| `NPCAccessoryUpgrade` | `CellLinkInfo Target`、`RefineType RefineType`（338-342） | `SConnection.cs:715-721` → 饰品精炼升级 | `Client/Scenes/Views/NPCDialog.cs:6669` |
| `NPCAccessoryReset` | `CellLinkInfo Cell`（746-749） | `SConnection.cs:1474-1480` → 饰品精炼重置 | `Client/Scenes/Views/NPCDialog.cs:7130` |
| `NPCWeaponCraft` | `RequiredClass Class`、`CellLinkInfo Template`、`Yellow`、`Blue`、`Red`、`Purple`、`Green`、`Grey`（750-760） | `SConnection.cs:1481-1487` → 武器打造（模板+七彩宝石配方） | `Client/Scenes/Views/NPCDialog.cs:7529` |
| `NPCAccessoryRefine` | `CellLinkInfo Target`、`CellLinkInfo OreTarget`、`List<CellLinkInfo> Links`、`RefineType RefineType`（761-767） | `SConnection.cs:1488-1494` → 新版饰品精炼（含矿石） | `Client/Scenes/Views/NPCDialog.cs:8183` |
| `NPCSocketItem` | `CellLinkInfo Target`、`CellLinkInfo Gem`（768-772） | `SConnection.cs:1495-1501` → 镶嵌宝石 | `Client/Scenes/Views/NPCSocketDialog.cs:411` |
| `NPCSocketCombine` | `CellLinkInfo Gem1`、`Gem2`、`Gem3`（773-778） | `SConnection.cs:1502-1508` → 宝石合成 | `Client/Scenes/Views/NPCSocketCombineDialog.cs:349` |
| `CompanionUnlock` | `int Index`（650-653） | `SConnection.cs:1317-1322` → 解锁宠物外形 | `Client/Scenes/Views/NPCDialog.cs:4221` |
| `CompanionAdopt` | `int Index`、`string Name`（654-658） | `SConnection.cs:1323-1328` → 领养宠物 | `Client/Scenes/Views/NPCDialog.cs:4197` |
| `CompanionRetrieve` | `int Index`（660-663） | `SConnection.cs:1329-1334` → 取出宠物随行 | `Client/Scenes/Views/NPCDialog.cs:4761` |
| `CompanionRelease` | `int Index`（665-668） | `SConnection.cs:1335-1341` → 放生宠物 | `Client/Scenes/Views/NPCDialog.cs:4770` |
| `CompanionStore` | `int Index`（669-672） | `SConnection.cs:1342-1348` → 收回宠物入库 | `Client/Scenes/Views/NPCDialog.cs:4756` |
| `SendCompanionFilters` | `List<MirClass> FilterClass`、`List<Rarity> FilterRarity`、`List<ItemType> FilterItemType`（784-789） | `SConnection.cs:1516-1522` → 宠物自动拾取过滤规则 | `Client/Scenes/Views/CompanionDialog.cs:653` |
| `GameGoldRecharge` | 无字段（524-525） | **未找到实现**：`SConnection.cs` 无 `Process(C.GameGoldRecharge)`，按 `BaseConnection.ProcessPacket` 规则发送即抛 NotImplementedException 被踢；原版客户端也无发送处（全库 grep `new C.GameGoldRecharge` 仅命中包定义本身）。疑为商城充值回调残留，**死包，勿实现** | 无 |

## 十四、拍卖行与商城（9 个）

流程：寄售行 ConsignmentDialog —— `MarketPlaceSearch`（搜索，支持物品类型过滤与排序）→ 结果懒加载 `MarketPlaceSearchIndex`（服务端只对可见行下发 `ClientMarketPlaceInfo` 明细）→ `MarketPlaceBuy` 成交；卖家 `MarketPlaceConsign` 上架、`MarketPlaceCancelConsign` 撤销、`MarketPlaceHistory` 查行情。游戏商城 GameStoreDialog 走 `MarketPlaceStoreBuy`（直接购买）与 `GameStoreFavouriteToggle/GameStoreGift`。

### C.MarketPlaceSearch（逐字段）

```csharp
public sealed class MarketPlaceSearch : Packet   // ClientPackets.cs:447-455
{
    public string Name { get; set; }              // 搜索关键字（空=全部）
    public bool ItemTypeFilter { get; set; }      // 是否启用物品类型过滤
    public ItemType ItemType { get; set; }        // 过滤的物品类型
    public MarketPlaceSort Sort { get; set; }     // 排序：Newest/Oldest/HighestPrice/LowestPrice
}
```

服务端 `SConnection.cs:951-1028`：允许 Game/Observer 阶段；按关键字/类型过滤 `AuctionInfo`，再按 `p.Sort` 排序（995-1009 的 switch），缓存进连接级 `MPSearchResults`（`SConnection.cs:38`）。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `MarketPlaceHistory` | `int Index`、`int Display`、`int PartIndex`（432-437） | `SConnection.cs:915-943` → 查成交历史（PartIndex 用于部件物品） | `Client/Scenes/Views/ConsignmentDialog.cs:901` |
| `MarketPlaceConsign` | `CellLinkInfo Link`、`int Price`、`string Message`、`bool GuildFunds`（438-446） | `SConnection.cs:944-950` → 上架（GuildFunds=行会资金托管） | `Client/Scenes/Views/ConsignmentDialog.cs:1229-1234` |
| `MarketPlaceSearchIndex` | `int Index`（456-459） | `SConnection.cs:1029-1043` → 懒加载某条结果的完整信息 | `Client/Scenes/Views/ConsignmentDialog.cs:605` |
| `MarketPlaceCancelConsign` | `int Index`、`long Count`（460-464） | `SConnection.cs:1044-1049` → 撤销上架（Count 可部分撤回） | `Client/Scenes/Views/ConsignmentDialog.cs:659` |
| `MarketPlaceBuy` | `long Index`、`long Count`、`bool GuildFunds`（465-470） | `SConnection.cs:1050-1055` → 购买玩家寄售品 | `Client/Scenes/Views/ConsignmentDialog.cs:644` |
| `MarketPlaceStoreBuy` | `int Index`、`long Count`、`bool UseHuntGold`（471-476） | `SConnection.cs:1056-1061` → 商城直购（UseHuntGold=用狩猎币） | `Client/Scenes/Views/GameStoreDialog.cs:983-987` |
| `GameStoreFavouriteToggle` | `int Index`（478-481） | `SConnection.cs:1062-1067` → 商城收藏开关 | `Client/Scenes/Views/GameStoreDialog.cs:870` |
| `GameStoreGift` | `int Index`、`long Count`、`bool UseHuntGold`、`string Recipient`（483-489） | `SConnection.cs:1068-1074` → 商城赠送 | `Client/Scenes/Views/GameStoreDialog.cs:1007-1012` |

（`MarketPlaceSearch` 本体见上文逐字段小节。）

## 十五、邮件（4 个）

流程：CommunicationDialog 收件箱 —— 打开邮件即 `MailOpened`（服务端标已读），附件逐格 `MailGetItem`，删除 `MailDelete`；写信页 `MailSend`（物品 Links + 金币 + 标题/正文）。服务端处理均为一行转发 `PlayerObject` 同名方法（`SConnection.cs:1075-1103`）。

### C.MailSend（逐字段）

```csharp
public sealed class MailSend : Packet        // ClientPackets.cs:505-512
{
    public List<CellLinkInfo> Links { get; set; }   // 附件物品（格子引用）
    public string Recipient { get; set; }           // 收件人角色名
    public string Subject { get; set; }             // 标题
    public string Message { get; set; }             // 正文
    public long Gold { get; set; }                  // 附带金币
}
```

客户端发送处：`Client/Scenes/Views/CommunicationDialog.cs:1282`；服务端 `SConnection.cs:1097-1103` → `PlayerObject.MailSend`（含寄送费/屏蔽校验）。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `MailOpened` | `int Index`（492-495） | `SConnection.cs:1075-1084` → 标记已读 | `Client/Scenes/Views/CommunicationDialog.cs:697,724` |
| `MailGetItem` | `int Index`、`int Slot`（496-500） | `SConnection.cs:1085-1090` → 取指定附件格 | `Client/Scenes/Views/CommunicationDialog.cs:735,1118` |
| `MailDelete` | `int Index`（501-504） | `SConnection.cs:1091-1096` → 删邮件 | `Client/Scenes/Views/CommunicationDialog.cs:760,1165` |

## 十六、任务与里程碑（7 个）

流程：NPC 对话页接任务 `QuestAccept` → 任务页追踪开关 `QuestTrack`（QuestDialog）→ 完成时 NPC 页 `QuestComplete`（可选奖励 `ChoiceIndex`）；放弃 `QuestAbandon`。里程碑（成就）在 QuestDialog 里程碑页签：`MilestoneNotify` 订阅、`MilestoneActive` 设激活、`MilestoneClaim` 领奖。

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `QuestAccept` | `int Index`（629-632） | `SConnection.cs:1292-1297` → 接任务 | `Client/Scenes/Views/NPCDialog.cs:3550` |
| `QuestComplete` | `int Index`、`int ChoiceIndex`（633-638） | `SConnection.cs:1298-1303` → 交任务（ChoiceIndex=多选奖励的索引，0=默认） | `Client/Scenes/Views/NPCDialog.cs:3573` |
| `QuestTrack` | `int Index`、`bool Track`（639-644） | `SConnection.cs:1304-1309` → 追踪开关（决定服务端推送进度） | `Client/Scenes/Views/QuestDialog.cs:1287` |
| `QuestAbandon` | `int Index`（645-648） | `SConnection.cs:1310-1316` → 放弃任务 | `Client/Scenes/Views/QuestDialog.cs:908` |
| `MilestoneNotify` | `bool Receive`（846-849） | `SConnection.cs:1633-1646` → 订阅/退订里程碑推送 | `Client/Scenes/Views/QuestDialog.cs:1745`（页签显隐） |
| `MilestoneActive` | `int Index`、`bool Active`（851-855） | `SConnection.cs:1647-1653` → 设置当前激活里程碑 | `Client/Scenes/Views/QuestDialog.cs:1848,2368` |
| `MilestoneClaim` | `int Index`（857-860） | `SConnection.cs:1654-1657` → `PlayerObject.MilestoneClaim` 领奖 | `Client/Scenes/Views/QuestDialog.cs:2435` |

## 十七、社交：好友/屏蔽/婚姻（6 个）

| 包名 | 字段（照抄） | 服务端处理 | 客户端触发 |
|---|---|---|---|
| `FriendAdd` | `string Name`（795-798） | `SConnection.cs:1532-1563` → 加好友（双向在线提醒） | `Client/Scenes/Views/CommunicationDialog.cs:587` |
| `FriendRemove` | `int Index`（799-802） | `SConnection.cs:1564-1576` → 删好友 | `Client/Scenes/Views/CommunicationDialog.cs:610` |
| `BlockAdd` | `string Name`（688-691） | `SConnection.cs:1371-1399` → 拉黑（Game/Observer 阶段均可） | `Client/Scenes/Views/CommunicationDialog.cs:963` |
| `BlockRemove` | `int Index`（692-695） | `SConnection.cs:1400-1412` → 取消拉黑 | `Client/Scenes/Views/CommunicationDialog.cs:985` |
| `MarriageResponse` | `bool Accept`（673-676） | `SConnection.cs:1349-1357` → 求婚应答 | `Client/Envir/CConnection.cs:4346-4348`（求婚弹窗三按钮） |
| `MarriageMakeRing` | `int Slot`（678-681） | `SConnection.cs:1358-1363` → 把背包格 Slot 的戒指制成结婚戒指 | `Client/Scenes/Views/NPCDialog.cs:5004` |

（`MarriageTeleport` 见移动组。）

---

# GodotClient 现状

实测方式：对 `GodotClient/` 全目录 `grep 'new C\.'`（发送处）+ 通读 `GodotClient/Network/ServerConnection.cs:893-1130`（Send 包装层）与 `GodotClient/Scripts/GameScene.cs` 的 Send* 方法。Godot 客户端**没有**独立的包定义文件——直接复用 `LibraryCore` 的 C 包类，经 `BaseConnection.Enqueue` 走同一序列化，因此线协议天然兼容。

| 功能域 | 状态 | 证据（GodotClient 路径:行号） |
|---|---|---|
| 网络层（连接/握手/收发） | 已移植 | `Network/NetworkManager.cs:69-90`（Connect）、`Network/ServerConnection.cs:16-67`（BaseConnection 子类、ProcessUnhandledPacket 只打日志不断线）；区别：用 `_Process` 同步轮询代替异步 `BeginReceive`（`NetworkManager.cs:25-67`） |
| CheckSum 指纹 | 已移植（等价实现） | `Network/ServerConnection.cs:23-31`：`user://checksum.bin` 持久化 GUID 前 20 位（原版为 `CEnvir.C`） |
| 登录/账号 9 包 | 已移植 | `Network/ServerConnection.cs:893-944`（SendLogin/NewAccount/NewCharacter/StartGame/DeleteCharacter 等全套）、`Scripts/LoginScene.cs:207`（自动登录）、GoodVersion 后自动发 SelectLanguage（`Network/ServerConnection.cs:371`） |
| 通用握手 G 包 | 已移植 | `Network/ServerConnection.cs:364-380`（Connected/GoodVersion/Disconnect/Ping/PingResponse） |
| 移动/传送 10 包 | 已移植 | `Scripts/GameScene.cs:1002`（Move）、`:1475`（Turn）、`:1945`（Mount）、`:6086`（MarriageTeleport）；AutoPath 四包 `Network/ServerConnection.cs:1039-1042`；TeleportRing `:1086`；JoinInstance `:1045` |
| 战斗 12 包 | 已移植 | `Scripts/GameScene.cs:994`（Attack）、`:9775`（Magic）、`:10224`（Mining）；RangeAttack/Harvest/TownRevive `Network/ServerConnection.cs:1092-1094`；FishingCast `:1034`；Taming/TamingSuccess `:1043-1044`；MagicToggle `:1089`；ChangeAttackMode/ChangePetMode `Scripts/GameScene.cs:5311,5318` |
| 拾取 | 已移植 | `Network/ServerConnection.cs:1003-1006`（SendPickUp）+ 250ms 节流复刻 `Scripts/GameScene.cs:6439-6446` |
| 物品/背包 19 包 | 已移植 | `Network/ServerConnection.cs:948-1011`（ItemMove/Use/Split/Lock/Sort/Delete/Drop/CurrencyDrop/PickUp/BeltLinkChanged）；LootBox×5/Bundle×2/FortuneCheck `Scripts/GameScene.cs:6130-6137`；AutoPotionLinkChanged `:6120`；ItemUse 分流 `Controls/DXItemCell.cs:1200,1209` |
| 技能 MagicKey | 已移植 | `Network/ServerConnection.cs:1127-1130`（SendMagicKey，注释对标原版 Image_KeyDown） |
| 聊天 | 已移植 | `Scripts/GameScene.cs:290-297`（SendChat 含 LinkedItemIndexes）、`Controls/ChatTextBox.cs:162` |
| 组队 7 包 | 已移植 | `Scripts/GameScene.cs:6447-6471`（GroupSwitch/Invite/Remove/Request/Response）、`Network/ServerConnection.cs:1087-1089`（LFGUpdate/Notify） |
| 交易 6 包 | 已移植 | `Scripts/GameScene.cs:6496-6519`（TradeClose/Confirm/AddGold/AddItem/RequestResponse）、`:1957`（TradeRequest）、`Network/ServerConnection.cs:995` |
| 行会 17 包 | 已移植 | `Network/ServerConnection.cs:1064-1106` + `Scripts/GameScene.cs:6398-6426`（GuildEditMember/KickMember 等） |
| NPC/商店/宠物 28 包 | 已移植（除 GameGoldRecharge） | `Scripts/GameScene.cs:6259-6535`（NPCSocket/Fragment/Refine 系/Accessory 系/WeaponCraft/Roll/Button/Close/Buy/Sell/Repair/Companion×5/SendCompanionFilters:6368）、`:10191`（NPCCall，带 1 秒防抖） |
| 拍卖/商城 9 包 | 已移植 | `Network/ServerConnection.cs:1013-1032`（Search/SearchIndex/History/Buy/Cancel/Consign）、`:1058-1063`（StoreBuy/Favourite/Gift） |
| 邮件 4 包 | 已移植 | `Scripts/GameScene.cs:6472-6495`（MailOpened/MailGetItem/MailDelete/MailSend）、`Network/ServerConnection.cs:985-993` |
| 任务/里程碑 7 包 | 已移植 | `Scripts/GameScene.cs:392-408`（Quest 四包）、`:6169-6172`（Milestone 三包） |
| 角色/外观/观察 17 包 | 已移植 | `Network/ServerConnection.cs:922-944`（NewCharacter 等）、`:1046-1055`（Gender/Hair/Armour/Name/Milestone）、`:1090-1091`（Hermit/Observable）、`:1082`（IncreaseDiscipline）、`:1078-1081`（OnlineState/Helmet/Block×2）、`Scripts/GameScene.cs:464`（ObserverRequest）、`:467`（Inspect） |
| 排行 | 已移植 | `Network/ServerConnection.cs:1075-1077`（RankSearch/RankRequest）、`Scripts/GameScene.cs:386` |
| `GameGoldRecharge` | 未移植（死包，原版也无发送处，见第十三组） | 无对应代码 |
| S→C 接收侧架构差异（供参考） | 已移植但结构不同 | 原版 `CConnection.Process(S.*)` 巨类（5110 行）；Godot 用 C# event + `Pending*` 队列缓冲 StartGame 突发包（`Network/ServerConnection.cs:289-362`），观察者模式复刻 `ObserverPacket` 语义 |

**结论：152/153 个 C 包在 GodotClient 已有发送路径**（唯一例外 `GameGoldRecharge` 为双方共同死包）。

# 移植注意事项

1. **绝不要手写包 ID**。ID 是反射排序索引（General 前 7 个确定，其余 C/S 按类名混排，且 73 对 C/S 同名包顺序受非稳定排序影响）。Godot 客户端复用 LibraryCore（`NetworkManager.cs:75` 起），新客户端也应引用同一程序集；若要自实现协议层，必须连 ID 生成算法一起移植并锁死 LibraryCore 版本（任何包类的增删改名都会平移 ID）。
2. **字段宽度必须逐字节照抄**。`long`（8B）与 `int`（4B）混用（如 `MarketPlaceBuy.Index` 是 `long`、`GameStoreFavouriteToggle.Index` 是 `int`）会导致后续字段错位且难排查。
3. **属性即协议**：序列化遍历的是公开属性（声明顺序），加字段必须两端同步；`[IgnorePropertyPacket]` 可排除。`Packet.PacketType/Length/ObserverPacket` 是字段不参与序列化。
4. **未实现包 = 踢线**：服务端 `ProcessUnhandledPacket` 抛 `NotImplementedException`（`BaseConnection.cs:443-446`）。调试期误发 `GameGoldRecharge` 这类包会直接断线，报错形态是 "Not Implemented Exception: Method Process(...)"。
5. **静默丢弃语义**：阶段守卫（`Stage != Game` 等）、方向合法域（`Direction` 超 `[Up, UpLeft]`）、聊天超长（`SConnection.cs:601`）都是**无回包的静默 return**。Godot 侧排查"发包没反应"时应先对照这三个闸门。
6. **冷却在服务端**：Move/Attack/Magic 等的间隔判定在 `PlayerObject`（`ActionTime/MoveTime/AttackTime`），客户端节流（如 Godot 的 250ms 拾取节流、`ComputeAttackIntervalMs`）只是省流量，不是权威。
7. **观察者镜像**：`ObserverPacket`（默认 true）的服务端→被观察者包会复制给观察者（`SConnection.cs:209-217`）。做观战/录像功能时注意哪些包带了 `ObserverPacket = false`（如 Ping、聊天 observer 变体）不转发。
8. **反作弊闸门**：>1024 字节 0 包、队列超 `Config.MaxPacket` 都会封 IP（`SConnection.cs:180-204`）。批量功能（如整理仓库、批量上架）不要在单帧内塞包。
9. **CellLinkInfo 归属校验**：所有带 `CellLinkInfo`/`Links` 的包在 PlayerObject 侧先 `ParseLinks` 验证格子确实属于该玩家（`PlayerObject.cs:6337`），Godot 移植物品功能时照抄该假设即可，不必在客户端预校验。
10. **两条接收路径不可混用**：Godot 侧用同步轮询（`NetworkManager.cs:30` 注释：异步回调在 Godot 环境可能不触发），不要在 Godot 里调用 `StartReceive()`/`BeginReceive`。

# 包计数对账

| 来源 | 类数 | 本文覆盖 |
|---|---|---|
| `LibraryCore/Network/ClientPackets.cs`（`grep -c "public sealed class"`） | 153 | 153（分组：账号 9 + 角色 15 + 观察 2 + 移动传送 10 + 战斗 12 + 物品背包 19 + 技能 1 + 聊天 1 + 组队 7 + 交易 6 + 行会 17 + NPC/商店/宠物 28 + 拍卖商城 9 + 邮件 4 + 任务里程碑 7 + 社交 6 = 153） |
| `LibraryCore/Network/GeneralPackets.cs` | 7 | 7（通用/握手组） |
| 合计 | 160 | 160 |
| 参考：`LibraryCore/Network/ServerPackets.cs` | 216 | 不在本文范围（S→C 见 packets-s2c.md） |
