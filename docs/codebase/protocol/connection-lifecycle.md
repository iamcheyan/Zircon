# 连接生命周期与网络传输机制（监听 → 握手 → 序列化 → 心跳 → 断线/顶号）

## TL;DR 速查表

- 监听不在 `Server/`（那是 WinForms 管理端），而在 **`ServerLibrary/Envir/SEnvir.cs:84-106`**：`TcpListener(IPAddress, Port=7000)` + 第二个 `UserCountPort=3000` 人数查询口。
- 单线程游戏循环 `SEnvir.EnvirLoop`（`SEnvir.cs:1373`）逐帧调 `SConnection.Process()`；**新连接先入 `NewConnections` 队列，积压 ≥15 时暂停 accept**（`SEnvir.cs:172-173`）。
- 握手链：服务端连上即发 `G.Connected`（`SConnection.cs:63`）→ 客户端回 `G.Connected` →（`CheckVersion=true` 时）`G.CheckVersion` → 客户端回 `G.Version{ClientHash=SHA256(自身 dll)}` → `G.GoodVersion{DatabaseKey, SystemDatabaseVersion}` → 阶段进入 `Login`。
- 阶段机 `GameStage`：`None → Login → Select → Game →（Observer）→ Disconnected`（`SConnection.cs:1661-1669`），所有 C 包处理器都有 `if (Stage != GameStage.X) return;` 守卫。
- **网络层无加密、无压缩**：AES（`LibraryCore/Encryption.cs`）只用于本地数据库文件，密钥经 `G.GoodVersion.DatabaseKey` 下发且 `Config.EncryptionEnabled` 默认 `false`。字节序小端，字符串 = BinaryWriter 的 7 位前缀长度 UTF-8。
- 帧 = `[int32 小端 总长度(含 4 字节自身)][int16 包 ID][反射序列化属性]`，最小 6B、最大 64MB（`Packet.cs:127-136`）；包 ID 是 LibraryCore 反射排序索引，**C/S 端必须同一程序集**。
- 心跳：服务端每 `PingDelay=2s` 发 `G.Ping`，客户端原样回 `G.Ping`，RTT/2 回填 `G.PingResponse`（`SConnection.cs:171-312`）；空闲超时 `TimeOut=20s`（服务端）/`15s`（WinForms 客户端）/`30s`（Godot 客户端）→ `G.Disconnect{TimedOut}`。
- 断线角色保留：战斗中不可立即断开（`CombatTime+10s` 缓冲，`SConnection.cs:89-132`）；重登冷却 `RelogDelay=10s`（`Config.cs:60`）。
- 顶号：同账号重复登录按 IP+CheckSum 分三档（顶号/管理员顶号/异地异机重置密码），`SEnvir.Login`（`SEnvir.cs:3348-3384`）。**无每 IP 连接数上限**（`IPCount` 只统计）。
- GodotClient 现状：轮询式收包 + 30s 超时 + `checksum.bin` 充当 CheckSum，**缺 `G.CheckVersion/G.Version` 处理器**，只能连关闭版本校验的服务端。

## 职责概述

连接生命周期由四层协作：`SEnvir`（监听/接受/主循环/全局 IP 封禁）、`BaseConnection`（`LibraryCore/Network/BaseConnection.cs`，TCP 异步收发、半包/分包缓冲、超时、反射分发）、`SConnection`（服务端连接语义：阶段机、Ping、观察者、反滥用）、`Packet`（`LibraryCore/Network/Packet.cs`，纯反射序列化）。客户端对偶是 `Client/Envir/CConnection.cs`（WinForms）与 `GodotClient/Network/ServerConnection.cs` + `NetworkManager.cs`。`Server/` 目录只是管理界面（Views/ 各种编辑器），**不参与游戏连接**。

## 关键类/文件清单

| 路径 | 行数/范围 | 职责 |
|---|---|---|
| `ServerLibrary/Envir/SEnvir.cs` | 4602 行 | 监听 `StartNetwork`(84-106)、接受 `Connection`(144-178)、人数口 `CountConnection`(180-198)、主循环 `EnvirLoop`(1373-1643)、关停 `StopNetwork`(107-142)、登录/顶号 `Login`(3262-3419) |
| `ServerLibrary/Envir/SConnection.cs` | 1671 行 | 构造(43-64)、阶段机+枚举(29, 1661-1669)、Ping(171-178, 302-312)、反滥用(180-204)、观察者复制(209-217)、断线缓冲(89-132)、CleanUp(150-169) |
| `LibraryCore/Network/BaseConnection.cs` | 473 行 | 异步收包+半包(69-127)、分块发送(128-198)、断线发送(229-309)、`Process()` 批处理+超时(311-394)、反射分发(396-441)、`UpdateTimeOut`(448-453) |
| `LibraryCore/Network/Packet.cs` | 447 行 | 包 ID 反射表(23-119)、`ReceivePacket`(121-164)、`GetPacketBytes`(165-187)、`WriteObject`(189-299)、`ReadObject`(300-432) |
| `LibraryCore/Network/GeneralPackets.cs` | 27 行 | 7 个双向通用包 |
| `LibraryCore/Encryption.cs` | — | AES-256，仅本地数据库加解密(9-73) |
| `ServerLibrary/Envir/Config.cs` | — | `Port=7000`(13)、`TimeOut=20s`(14)、`PingDelay=2s`(15)、`UserCountPort=3000`(16)、`MaxPacket=50`(17)、`PacketBanTime=5min`(18)、`CheckVersion=true`(22)、`EncryptionEnabled=false`(38)、`RelogDelay=10s`(60) |
| `Client/Envir/CConnection.cs` | 5110 行 | 客户端连接：握手(128-163)、断线弹窗(74-126)、`TimeOutDuration=15s`(25) |
| `Client/Envir/CEnvir.cs` | 1103 行 | 主循环调 `Connection.Process()`(216)、统一发包入口(556) |
| `Client/Scenes/LoginScene.cs` | 3509 行 | `BeginConnect` 重连循环(283-349)、握手回调 `Connecting`(374-396) |
| `ServerLibrary/Envir/Commands/Command/Admin/Kick.cs` | 33 行 | GM 踢人(29) |
| `GodotClient/Network/NetworkManager.cs` | 101 行 | Godot 连接管理：同步轮询(25-67)、`Connect`(69-90) |
| `GodotClient/Network/ServerConnection.cs` | 1132 行 | Godot 版 `BaseConnection` 子类：30s 超时(20)、checksum.bin(18-32)、事件通知 |
| `GodotClient/Network/SinglePlayerLauncher.cs` | 214 行 | 单机模式：拉起本地 ServerCore/BotRunner(27-39) |

## 核心流程

### 1. 监听与接受（服务端）

```csharp
// ServerLibrary/Envir/SEnvir.cs:84-106
private static void StartNetwork(bool log = true)
{
    NewConnections = new ConcurrentQueue<SConnection>();

    _listener = new TcpListener(IPAddress.Parse(Config.IPAddress), Config.Port);
    _listener.Start();
    _listener.BeginAcceptTcpClient(Connection, null);

    _userCountListener = new TcpListener(IPAddress.Parse(Config.IPAddress), Config.UserCountPort);
    _userCountListener.Start();
    _userCountListener.BeginAcceptTcpClient(CountConnection, null);
    ...
}
```

- 游戏端口默认 **7000**（`Config.cs:13`），人数查询端口 **3000**（`Config.cs:16`）：任何 TCP 客户端连上 3000 都会收到一条 `c;/Zircon/{Connections.Count}/;` ASCII 文本然后被关闭（`SEnvir.cs:180-198`）——这是给启动器/服务器列表用的独立小协议，与包协议无关。
- 接受回调 `Connection`（`SEnvir.cs:144-178`）：先查 `IPBlocks`（封禁到期自动放行，154），通过则 `new SConnection(client)` 入 `NewConnections` 队列；finally 里**队列积压 ≥15 时 `Thread.Sleep(1)` 背压**，然后继续 `BeginAcceptTcpClient` 接受下一个（172-176）。
- 主循环 `EnvirLoop`（`SEnvir.cs:1373-1643`）每帧：把 `NewConnections` 全部搬进 `Connections` 并 `IPCount[ip]++`（1404-1414，仅统计）→ 逆序逐个 `connection.Process()`（1419-1428）→ `Players[i].StartProcess()`（1434-1435）→ 在 1ms 时间片内轮转 `ActiveObjects`（1448-1478）。**所有包处理都发生在这个单线程里**，处理器内无需加锁。

### 2. SConnection 构造与握手（第一包验证/版本检查）

```csharp
// ServerLibrary/Envir/SConnection.cs:43-64
public SConnection(TcpClient client) : base(client)
{
    IPAddress = client.Client.RemoteEndPoint.ToString().Split(':')[0];
    SessionID = ++SessionCount;                      // 进程内递增会话号
    Language = (StringMessages)ConfigReader.ConfigObjects[typeof(EnglishMessages)];
    OnException += ...;                              // 崩溃写 Errors.txt
    SEnvir.Log(string.Format("[Connection] IP Address:{0}", IPAddress));
    UpdateTimeOut();                                 // TimeOutTime = Now + 20s
    BeginReceive();                                  // 启动异步收包
    Enqueue(new G.Connected());                      // 第一包：服务端主动打招呼
}
```

握手与阶段推进：

```csharp
// ServerLibrary/Envir/SConnection.cs:270-301
public void Process(G.Connected p)          // 客户端回显 G.Connected 触发
{
    if (Config.CheckVersion)
    {
        Enqueue(new G.CheckVersion());      // 要求客户端上报版本哈希
        return;
    }
    Stage = GameStage.Login;
    Enqueue(new G.GoodVersion
    {
        DatabaseKey = Config.EncryptionEnabled ? SEnvir.CryptoKey : null,
        SystemDatabaseVersion = SEnvir.Session?.RefreshSystemVersion(),
    });
}
public void Process(G.Version p)
{
    if (Stage != GameStage.None) return;
    if (!Functions.IsMatch(Config.ClientHash, p.ClientHash))   // 全字节比对
    {
        SendDisconnect(new G.Disconnect { Reason = DisconnectReason.WrongVersion });
        return;
    }
    Stage = GameStage.Login;                // 版本通过才放行
    Enqueue(new G.GoodVersion { ... });
}
```

- `Config.ClientHash` 是服务端启动时对 `./Zircon.dll`（`VersionPath`，`Config.cs:23`）算的 SHA256（`Config.cs:174-177`）；客户端用 SHA256 对**自身可执行文件同名 .dll** 计算（`Client/Envir/CConnection.cs:134-144`）。两端二进制不一致即断线。
- `DatabaseKey` 是数据库 AES 密钥（仅 `EncryptionEnabled=true` 时下发），客户端收到后 `Encryption.SetKey(p.DatabaseKey)` 再 `LoadDatabase()`（`CConnection.cs:145-155`）。
- 阶段划分：`GameStage { None, Login, Select, Game, Observer, Disconnected }`（`SConnection.cs:1661-1669`）。登录成功 `Stage=Select`（`SEnvir.cs:3391`）；进游戏 `Stage=Game`（`PlayerObject.cs:1053` OnSpawned）。**每个 `Process(C.*)` 都先检查阶段**（如 `SConnection.cs:316` `if (Stage != GameStage.Login) return;`、405 行起统一 `GameStage.Game` 守卫），阶段不符静默丢弃。

### 3. 序列化机制（Packet.cs 全解）★

#### 3.1 包 ID 表：运行时反射排序

```csharp
// LibraryCore/Network/Packet.cs:23-48（节选）
static Packet()
{
    Packets = new List<Type>();
    foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
    {
        if (type.BaseType != typeof(Packet)) continue;
        Packets.Add(type);
    }
    Packets.Sort((x1, x2) =>
    {
        if (String.Compare(x1.Namespace, x2.Namespace, StringComparison.Ordinal) == 0)
            return String.Compare(x1.Name, x2.Name, StringComparison.Ordinal);
        if (string.Compare(x1.Namespace, @"Library.Network.GeneralPackets", StringComparison.Ordinal) == 0)
            return -1;                                   // General 包永远排最前
        ...
        return String.Compare(x1.Name, x2.Name, StringComparison.Ordinal);  // 其余按类名
    });
```

排序结果：GeneralPackets 命名空间的 7 个包按类名占 ID 前段，然后 **ClientPackets 与 ServerPackets 两个命名空间按类名混排**（排序键是 `Name` 而非命名空间）。包 ID = `Packets.IndexOf(GetType())`（`Packet.cs:172`）。两端引用同一个 LibraryCore 程序集，表天然一致——**这就是为什么 Godot 客户端必须链同一份 LibraryCore，而不是重新实现协议**。

#### 3.2 帧格式与解码

```csharp
// LibraryCore/Network/Packet.cs:121-164（节选）
public static Packet ReceivePacket(byte[] rawBytes, out byte[] extra)
{
    if (rawBytes.Length < 4) return null; //4Bytes: Packet Size |
    int length = rawBytes[3] << 24 | rawBytes[2] << 16 | rawBytes[1] << 8 | rawBytes[0];  // 小端

    const int minimumPacketLength = 6;
    const int maximumPacketLength = 64 * 1024 * 1024;
    if (length < minimumPacketLength || length > maximumPacketLength)
        throw new InvalidDataException($"Invalid packet length: {length}");

    if (length > rawBytes.Length) return null;          // 半包：等更多数据

    extra = new byte[rawBytes.Length - length];         // 剩余字节 = 下一个包的开头
    Buffer.BlockCopy(rawBytes, length, extra, 0, rawBytes.Length - length);
    ...
    stream.Seek(4, SeekOrigin.Begin);
    short id = reader.ReadInt16();
    p = (Packet)Activator.CreateInstance(Packets[id]);  // 按 ID 建实例
    p.PacketType = Packets[id];
    ReadObject(reader, p);                              // 反射逐属性读
}
// LibraryCore/Network/Packet.cs:165-187（编码）
public byte[] GetPacketBytes()
{
    writer.Write((short)Packets.IndexOf(GetType()));    // 2B 包 ID
    WriteObject(writer, this);                          // 属性序列化
    ...
    writer.Write(packet.Length + 4); //| 4Bytes: Packet Size | Data... |
    writer.Write(packet);
}
```

帧 = `4B 长度（含自身，小端） + 2B 包 ID + 载荷`。长度非法（<6 或 >64MB）直接抛 `InvalidDataException`，上层会断开连接。

#### 3.3 字段读写规则（WriteObject/ReadObject）

```csharp
// LibraryCore/Network/Packet.cs:189-206 / 300-316（节选合并）
private static void WriteObject(BinaryWriter writer, object ob)
{
    PropertyInfo[] properties = ob.GetType().GetProperties();      // 只看属性，公开字段不序列化
    foreach (PropertyInfo item in properties)
    {
        if (item.GetCustomAttribute<IgnorePropertyPacket>() != null) continue;
        if (!TypeWrite.TryGetValue(item.PropertyType, out writeAction))
        {
            if (item.PropertyType.IsClass)
            {
                object value = item.GetValue(ob);
                writer.Write(value != null);        // 类类型前缀 1B null 标志
                if (value == null) continue;
            }
            if (item.PropertyType.IsEnum)
                TypeWrite[item.PropertyType.GetEnumUnderlyingType()](item.GetValue(ob), writer);  // 枚举按底层类型
            else if (/* List<> */)
            {
                writer.Write(list.Count);           // int32 元素数
                foreach (object x in list) { writer.Write(x != null); if (x != null) WriteObject(writer, x); }
            }
            else if (/* Dictionary<,> / SortedDictionary<,> */)
            {
                writer.Write(dictionary.Count);     // int32 + [key][null 标志+value]×N
                ...
            }
        }
        else
            writeAction(item.GetValue(ob), writer); // 基元类型直接走表
    }
}
```

内建类型表（`Packet.cs:52-86` / 92-115）关键映射：

| 类型 | 线上格式 |
|---|---|
| Boolean/Byte/SByte/Int16-64/UInt16-64/Single/Double/Decimal/Char | BinaryWriter 原生小端编码 |
| String | `BinaryWriter.Write(String)`：7 位变长前缀 + UTF-8（`Packet.cs:81/110`） |
| Color | `ToArgb()` int32（62/98） |
| DateTime | `ToBinary()` int64（63/99） |
| TimeSpan | `.Ticks` int64（82/111） |
| Point | 两个 int32 X,Y（69-73/106） |
| Size | 两个 int32（76-80/109） |
| Byte[] | int32 长度 + 原始字节（56-60/96） |
| 枚举 | 底层类型（默认 int32） |

`ReadObject` 读完后扫描 `[CompleteObject]` 方法并 `Invoke`（`Packet.cs:424-431`），用于把 `InfoIndex` 之类的主键回查成 `Info` 对象（如 `MagicCooldown.Complete`）。`[IgnorePropertyPacket]`（436-439）标记的属性跳过序列化。

### 4. TCP 收发与粘包处理

```csharp
// LibraryCore/Network/BaseConnection.cs:86-127（收包：累积 + 循环切包）
private void ReceiveData(IAsyncResult result)
{
    int dataRead = Client.Client.EndReceive(result);
    if (dataRead == 0) { Disconnecting = true; return; }    // TCP EOF = 断线
    TotalBytesReceived += dataRead;
    UpdateTimeOut();                                        // 每次收包刷新超时

    byte[] temp = _rawData;
    _rawData = new byte[dataRead + temp.Length];            // 半包残留 + 新数据
    Buffer.BlockCopy(temp, 0, _rawData, 0, temp.Length);
    Buffer.BlockCopy(rawBytes, 0, _rawData, temp.Length, dataRead);

    Packet p;
    while ((p = Packet.ReceivePacket(_rawData, out _rawData)) != null)  // 切出所有完整包
    {
        ReceiveList.Enqueue(p);
        TotalPacketsProcessed++;
    }
    BeginReceive();                                         // 继续异步读
}
```

发送侧：`Process()`（`BaseConnection.cs:311-394`）把 `SendList`（`ConcurrentQueue<Packet>`）一次性排空拼接成单个 `List<byte>`，经 `BeginSend` → `_sendBuffer/_sendOffset` **分块续发**（`BaseConnection.cs:128-198`）——源码注释（49-52）明确记录了不分块时登录后大 `StartGame` 包被截断、客户端永久卡选人界面的教训。断线前的最后一包走独立的 `_disconnectSendBuffer` 通道（229-309），发完即弃。

`Client.NoDelay = true`（`BaseConnection.cs:61`）关闭 Nagle，保证小包即时性。

### 5. 加密与压缩的真相

- **网络流量无任何加密与压缩**。`LibraryCore/Network/` 全目录无加密代码；帧内就是明文 BinaryWriter 输出。
- `LibraryCore/Encryption.cs`（9-73 行，`Aes.Create()` + `KeySize=256` + 随机 IV）只服务于**本地数据库文件**（System.db/Users.db 的读写流封装），密钥 `SEnvir.CryptoKey`（`SEnvir.cs:381`）仅在 `Config.EncryptionEnabled`（默认 **false**，`Config.cs:38`）时通过 `G.GoodVersion.DatabaseKey` 下发给客户端（`SConnection.cs:281/298`）。
- 字节序：长度字段手工小端拼装（`Packet.cs:129`），其余交给 `BinaryWriter/BinaryReader`（x86/x64 小端）。字符串编码 = .NET 7 位变长长度前缀 + UTF-8。

### 6. 心跳（KeepAlive）与超时

```csharp
// ServerLibrary/Envir/SConnection.cs:171-178 —— 服务端每帧检查
if (SEnvir.Now >= PingTime && !PingSent && Stage != GameStage.None)
{
    PingTime = SEnvir.Now;
    PingSent = true;
    Enqueue(new G.Ping { ObserverPacket = false });
}
// ServerLibrary/Envir/SConnection.cs:302-312 —— 客户端回包
public void Process(G.Ping p)
{
    if (Stage == GameStage.None) return;
    int ping = (int)(SEnvir.Now - PingTime).TotalMilliseconds / 2;   // RTT/2
    PingSent = false;
    PingTime = SEnvir.Now + Config.PingDelay;                          // 下一轮 = 2s 后
    Ping = ping;
    Enqueue(new G.PingResponse { Ping = Ping, ObserverPacket = false });
}
```

- 客户端对 `G.Ping` 只做回显：`Enqueue(new G.Ping())`（`Client/Envir/CConnection.cs:156-159`）。
- 超时机制在 `BaseConnection.Process()` 收口：

```csharp
// LibraryCore/Network/BaseConnection.cs:340-351
if (Time.Now >= TimeOutTime)
{
    if (!Disconnecting)
        TrySendDisconnect(new G.Disconnect { Reason = DisconnectReason.TimedOut });
    else
        TryDisconnect();
    return;
}
...
if (!Disconnecting && Sending)
    UpdateTimeOut();               // 发送中也算活跃
```

`TimeOutDelay` 三端取值：服务端 `Config.TimeOut = 20s`（`Config.cs:14`，`SConnection.cs:23`）、WinForms 客户端 `Config.TimeOutDuration = 15s`（`Client/Envir/Config.cs:20`，`CConnection.cs:25`）、Godot 客户端硬编码 `30s`（`ServerConnection.cs:20`）。`UpdateTimeOut()` 在每次收包（102）、发送完成（138/189）时刷新；`Disconnecting` 置位后超时被改写为 `Now + 2s`（34-44），保证断线包发完就收尾。

### 7. 断线、踢下线与角色保留

- **战斗保护断线**（`SConnection.cs:89-132`）：`TryDisconnect/TrySendDisconnect` 在 `Game` 阶段时，若 `SEnvir.Now < Player.CombatTime + 10s` 则不立即断——设 `Disconnecting=true` 并把 `TimeOutTime` 延到 `Now+10s`，等战斗状态过期再真正断开。防止网络闪断在 PK 中直接吞角色。
- **CleanUp**（`SConnection.cs:150-169`）：`Stage=Disconnected` → 清 `Account.Connection/TempAdmin` → `Player?.StopGame()` → 解除观察关系。
- **StopGame**（`ServerLibrary/Models/PlayerObject.cs:975-1039`）：记录 `Character.LastLogin`、通知公会成员下线、关交易、去 BUFF、宠物/法术/伴侣 Despawn、`UpdateOnlineState(true)`。**角色数据即时落内存 DB，无"角色残留战场"阶段**——对象当场 `Despawn()`。
- **重登冷却**：`Config.RelogDelay = 10s`（`Config.cs:60`），在 `SEnvir.StartGame` 检查 `Now - character.LastLogin < RelogDelay` 时回 `S.StartGame { Result = StartGameResult.Delayed, Duration }`（`SEnvir.cs:4017-4023`）。这就是"断线后角色保留时长"的实际语义：**世界内不留尸，只是 10 秒内不能再进**。
- **踢下线**：GM 命令 `KICK` → `character.Account.Connection.SendDisconnect(new G.Disconnect { Reason = DisconnectReason.Kicked })`（`ServerLibrary/Envir/Commands/Command/Admin/Kick.cs:29`）。客户端按 `DisconnectReason` 分支弹窗（`CConnection.cs:94-122`：TimedOut/ServerClosing/AnotherUser/AnotherUserAdmin/Banned/Kicked/Crashed）。
- **服务器关停**：`StopNetwork` 向所有连接 `SendDisconnect(G.Disconnect{ServerClosing})`，`Thread.Sleep(200)` 后统一 `Disconnect()`（`SEnvir.cs:122-136`）。
- **客户端主动断**：`C.Logout` 在 `Game` 阶段同样受 10 秒战斗冷却限制（`SConnection.cs:370` `if (SEnvir.Now < Player.CombatTime.AddSeconds(10)) return;`）。
- **反滥用**（`SConnection.Process`，180-204）：①一个包都没处理成功却收到 >1024 字节（垃圾流攻击）→ 立即断开 + `IPBlocks[ip] = Now + PacketBanTime(5min)` 并踢掉该 IP 全部连接；②`ReceiveList.Count > MaxPacket(50)`（处理不过来积压）→ 同上。

### 8. 多开与顶号

- **同 IP 多连接没有代码上限**：`IPCount` 只递增统计（`SEnvir.cs:1409-1411`），无比较上限的逻辑；封禁只来自反滥用规则。多开客户端天然允许。
- **同账号顶号**（`SEnvir.Login`，`SEnvir.cs:3348-3384`）：
  1. 新登录带 MasterPassword（admin 通道）：直接回 `AlreadyLoggedIn` 并 `TrySendDisconnect(G.Disconnect{AnotherUser})` 踢旧连接；
  2. 旧连接是 `TempAdmin`：只回 `AlreadyLoggedInAdmin`，不踢；
  3. **IP 与 CheckSum 都变了**（疑似盗号）：踢旧连接 + 生成随机新密码 + 发密码重置邮件 + 回 `AlreadyLoggedInPassword`（3366-3378）；
  4. 其余（同 IP 或同 CheckSum）：回 `AlreadyLoggedIn` 并踢旧连接（3381-3383）——**正常顶号语义**。
  `CheckSum` 是客户端登录包里的自报指纹（WinForms 客户端同样传 `C.Login.CheckSum`；Godot 客户端用持久化随机串 `user://checksum.bin`，`ServerConnection.cs:23-31`、`895`），服务端记录 `account.LastIP/LastSum`（`SEnvir.cs:3414-3415`）用于上述判定。
- **同角色双开不存在**：角色在线 = `character.Player != null`，顶号发生在账号层；旧连接断开时 `StopGame` 已把 `Player` 置空，新连接经 `RelogDelay` 冷却后 `StartGame` 重建 `PlayerObject`（`SEnvir.cs:4025`）。

### 9. 客户端侧连接实现

#### WinForms 原版

- 连接发起在 `LoginScene.Process()`（每帧）：无连接且到达 `ConnectionTime` 就 `new TcpClient()` + `BeginConnect(Config.IPAddress, Config.Port, Connecting, ...)`，5 秒超时重试（`Client/Scenes/LoginScene.cs:338-348`）。
- 回调 `Connecting`（374-396）：`EndConnect` 成功后 `ConnectionTime = Now + 5s`（给握手留余量），`CEnvir.Connection = new CConnection(client)`。
- `CConnection` 构造即 `BeginReceive()`（`Client/Envir/CConnection.cs:40`）；主循环每帧 `Connection?.Process()`（`Client/Envir/CEnvir.cs:216`）——与服务器同一套 `BaseConnection` 粘包/发送机制。
- 断线：`CConnection.Disconnect` 清空 `CEnvir.Connection` 并弹"Disconnected from server / Connection timed out"（47-68）；服务器踢的按 Reason 分支（74-126）。
- 统一发包入口 `CEnvir.Enqueue(packet)` → `Connection.Enqueue`（`Client/Envir/CEnvir.cs:556`）。

#### GodotClient

- `NetworkManager.Connect(host, port)`（`GodotClient/Network/NetworkManager.cs:69-90`）：**同步** `TcpClient.Connect`，然后**不复用上次半包残留**（`_rawData = Array.Empty<byte>()`，74 行注释：旧数据会污染新连接首包长度字段导致登录/选角随机卡死）。
- 收包改**同步轮询**：`_Process(double)` 里每帧收 8KB 并切包（25-67，注释说明 Godot 环境下异步回调可能不触发）。
- `ServerConnection : BaseConnection`（`ServerConnection.cs:16`）：30s 超时（20）、断线事件只通知一次（43-55）、未处理包打印日志而非抛异常（63-67）。
- 版本握手缺口：**没有 `Process(G.CheckVersion)`/`Process(G.Version)`**（grep 全文件确认）——服务端开 `CheckVersion=true`（默认）时 Godot 客户端无法完成握手。单机模式由 `SinglePlayerLauncher` 拉起本地 ServerCore（`SinglePlayerLauncher.cs:27-39`），其配置需关闭版本校验；联网模式同样要求服务端关闭。

## 数据结构/协议细节

### 连接相关包字段（GeneralPackets.cs 全文级）

| 包 | 字段 | 方向与用途 |
|---|---|---|
| Connected | （空） | S→C 连接建立即发（`SConnection.cs:63`）；C→S 回显触发版本检查（`CConnection.cs:128-133`） |
| CheckVersion | （空） | S→C 要求版本（`SConnection.cs:274`） |
| Version | `byte[] ClientHash` | C→S SHA256 哈希（`CConnection.cs:134-144`） |
| GoodVersion | `byte[] DatabaseKey; string SystemDatabaseVersion` | S→C 放行 + 数据库密钥/版本（`SConnection.cs:279-283`） |
| Ping / PingResponse | （空）/ `int Ping` | 心跳对（见第 6 节） |
| Disconnect | `DisconnectReason Reason` | 双向：超时/踢人/顶号/关服（`BaseConnection.cs:343`、`Kick.cs:29`、`SEnvir.cs:124`） |

### SConnection 状态字段速查

| 字段 | 类型 | 含义 |
|---|---|---|
| `Stage` | `GameStage` | None/Login/Select/Game/Observer/Disconnected（`SConnection.cs:29`） |
| `Account` / `Player` | `AccountInfo`/`PlayerObject` | 当前账号/角色 |
| `IPAddress` / `SessionID` | string/int | 连接对端 IP（45）、进程内递增会话号（46） |
| `Observed` / `Observers` | SConnection/List | 观战双向链（35-36） |
| `PingTime` / `PingSent` / `Ping` | DateTime/bool/int | 心跳状态（25-27） |
| `TimeOutTime` | DateTime | 空闲断线时刻（继承自 `BaseConnection.cs:32`） |

### 关键配置默认值（ServerLibrary/Envir/Config.cs）

| 配置 | 默认 | 行号 |
|---|---|---|
| `Port` | 7000 | 13 |
| `TimeOut` | 20s | 14 |
| `PingDelay` | 2s | 15 |
| `UserCountPort` | 3000 | 16 |
| `MaxPacket` | 50 | 17 |
| `PacketBanTime` | 5min | 18 |
| `CheckVersion` | true | 22 |
| `EncryptionEnabled` | false | 38 |
| `RelogDelay` | 10s | 60 |

## GodotClient 现状

| 功能 | 状态 | 证据 |
|---|---|---|
| TCP 连接/重连 | **已移植** | `GodotClient/Network/NetworkManager.cs:69-90`（含半包残留清理）；`Scripts/LoginScene.cs:84` 起连接流程 |
| 收包循环 | **已移植（改为轮询）** | `NetworkManager.cs:25-67` 用 `_Process` 同步收包替代 `BeginReceive` 异步回调 |
| 粘包/半包 | **已移植** | 直接继承 `LibraryCore/Network/BaseConnection.cs:86-127` 的累积切包逻辑 |
| G 握手 | **部分移植** | `Process(G.Connected/GoodVersion/Disconnect/Ping/PingResponse)` 齐全（`ServerConnection.cs:364-380`）；**缺 `G.CheckVersion/G.Version` 处理器**，需服务端关 `CheckVersion` |
| 心跳/超时 | **已移植（参数不同）** | `ServerConnection.cs:20` 硬编码 30s（服务端 20s）；`G.Ping` 回显 379 行 |
| 断线通知 | **已移植** | `NotifyDisconnected` 保证事件只发一次（`ServerConnection.cs:43-55`） |
| 断线重连 UI | **部分移植** | `LoginScene.cs` 有连接失败重试日志；GameScene 内掉线回登录的完整流程未见独立实现（[INFERENCE]） |
| 战斗断线缓冲/RelogDelay | 未移植（客户端无需实现，服务端语义） | — |
| 单机模式 | **已移植（新增）** | `SinglePlayerLauncher.cs:27-39` 拉起本地 ServerCore + BotRunner，端口探测 15s（156-171） |
| 数据库加载 | **已移植** | `Network/DatabaseLoader.cs`（70 行），`NetworkManager._Ready` 调用（21-23） |

## 移植注意事项

1. **包表耦合是生死线**：Godot 客户端已经正确复用 LibraryCore（继承 `BaseConnection`）。任何"自己定义协议常量"的重写都会因反射排序 ID 错位而全线崩溃；新增包类也必须两端同时编译。
2. **CheckVersion 缺口必须补**：给 `GodotClient/Network/ServerConnection.cs` 补 `Process(G.CheckVersion)`（回 `G.Version{ClientHash = SHA256(某个双方约定的文件)}`），否则永远只能连关闭版本校验的服务端。原版逻辑参考 `Client/Envir/CConnection.cs:134-144`。
3. **半包残留是复用连接的大坑**：Godot 侧已经在 `Connect` 里清 `_rawData`（`NetworkManager.cs:72-74` 注释），重写网络层时必须保留——否则重连后第一个长度字段被旧数据污染。
4. **发送分块不能省**：`BaseConnection.BeginSendChunk`（149-164）修复过 `StartGame` 大包截断；若在 Godot 里改用 `Socket.SendAsync` 也要循环发到缓冲区清空。
5. **单线程处理语义**：服务端所有包在 `EnvirLoop` 单线程处理；客户端 `Process` 也在主线程调用。Godot 的 `_Process` 轮询天然满足，但**不要**把收包搬到独立线程后直接改 UI 状态。
6. **超时三方不一致**：服务端 20s / WinForms 15s / Godot 30s。Godot 30s 意味着拔线后要等更久才感知断线；如需更快反馈应实现应用层 ping 监测（服务端 `PingDelay=2s` 已在跑，客户端可对 `PingResponse` 超时判断）。
7. **`Disconnecting` 的 2 秒宽限**（`BaseConnection.cs:34-44`）是为了让最后一个包（如 `G.Disconnect{Reason}`）发出去；移植断线流程时先发后断的顺序不能反。
8. **UserCountPort 是独立协议**：`c;/Zircon/{count}/;` 明文行（`SEnvir.cs:188`）。做启动器/服务器浏览器时用它，别把它混进包协议。
9. **数据库密钥通道**：`G.GoodVersion.DatabaseKey` 是唯一密钥下发点且默认关闭；若开启 `EncryptionEnabled`，Godot 侧需要等价于 `Encryption.SetKey`（`CConnection.cs:147`）的初始化，否则读不了加密 System.db。
