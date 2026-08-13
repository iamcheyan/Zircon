# S→C 服务端发包协议全量文档（ServerPackets.cs + GeneralPackets.cs）

## TL;DR 速查表

- S→C 包共 **216 个**（`LibraryCore/Network/ServerPackets.cs`，1495 行），另有 7 个双向通用包（`LibraryCore/Network/GeneralPackets.cs`），本文全覆盖并附计数。
- 任务书里提到的 `S.ObjectInformation / S.MapInformation / S.NewMapInformation / S.StatInfo` **在本仓库不存在**（已全文检索确认）。实际对应物：对象出现=`S.ObjectPlayer/ObjectMonster/ObjectNPC/ObjectItem/ObjectSpell`，进图全量数据=`S.StartGame.StartInformation`（`LibraryCore/Globals.cs:345-452`），切图通知=`S.MapChanged`，自身属性=`S.StatsUpdate`、他人属性=`S.ObjectStats`。
- **对象同步是三层模型**：可视层（`S.Object*`，Broadcast 给 `SeenByPlayers`）、数据层（`S.DataObject*`，发给 `DataSeenByPlayers`，用于组队/追踪的血条）、近旁层（`NearByPlayers`，只激活 AI 不发包）。见「对象同步模型」。
- 服务端发包统一走 `PlayerObject.Enqueue(Packet)`（`ServerLibrary/Models/PlayerObject.cs:16711`）→ `SConnection.Enqueue`（观察者复制，`ServerLibrary/Envir/SConnection.cs:209-217`）；广播走 `MapObject.Broadcast`（`ServerLibrary/Models/MapObject.cs:1689-1693`）。
- 客户端处理**不是 switch-case**，而是 `CConnection` 里的 `public void Process(S.XXX p)` 方法族，由 `BaseConnection.ProcessPacket` 反射分发（`LibraryCore/Network/BaseConnection.cs:396-441`）。213/216 个 S 包有处理器。
- **5 个死包**（服务端从不构造）：`GuildStats`、`MarketPlaceSearchCount`、`NPCAccessoryUpgrade`、`NPCRefinementStone`、`ObjectStats` 的服务端发送点未找到；其中 `GuildStats/NPCAccessoryUpgrade/NPCRefinementStone` 连客户端处理器也没有（`ObjectStats/MarketPlaceSearchCount` 客户端有处理，属"半死"包）。
- 大包 `S.StartGame`（携带 `StartInformation`：装备/魔法/BUFF/任务/货币全套）是客户端进游戏的唯一入口，随后 `S.MapChanged` + 一串 `S.Object*` 完成世界构建。
- 帧格式与序列化机制见 `docs/codebase/protocol/connection-lifecycle.md`（4 字节小端长度 + 2 字节包 ID + 反射序列化字段）。
- GodotClient 现状：**215/216 个 S 包已接事件**（唯一缺 `GuildStats`，它本来就是死包），见 `GodotClient/Network/ServerConnection.cs:364-706`；但多数 UI/渲染消费仍是部分移植（详见「GodotClient 现状」）。

## 职责概述

`LibraryCore/Network/ServerPackets.cs` 定义服务端→客户端的全部下行包（命名空间 `Library.Network.ServerPackets`，源码以 `using S = Library.Network.ServerPackets` 别名引用，见 `ServerLibrary/Envir/SConnection.cs:15`）。每个包是 `sealed class XXX : Packet`，只含自动属性，由 `Packet.WriteObject/ReadObject` 按属性反射序列化（`LibraryCore/Network/Packet.cs:189-432`）；个别包带 `[CompleteObject]` 方法（如 `MagicLeveled.Complete`，`ServerPackets.cs:483-488`）在客户端反序列化后把 `InfoIndex` 解析回 `MagicInfo` 对象。

服务端发送点分散在 `SEnvir`（大厅逻辑）、`SConnection`（连接级）、`PlayerObject`（玩家动作主体）、`MapObject/MonsterObject/NPCObject/ItemObject/SpellObject`（世界对象）、各 `Magics/*`（技能特效）与 `Envir/Commands/*`（GM 命令）。客户端在 `Client/Envir/CConnection.cs`（5110 行）逐一实现 `Process(S.XXX)`，把包内容写入 `GameScene.Game.MapControl.Objects` 的 `MapObject` 模型或各 UI 控件。

本文逐包给出：字段定义（照抄）、服务端典型发送点（路径:行号）、原版客户端处理处（`Client/Envir/CConnection.cs:行号`）。

## 关键类/文件清单

| 路径 | 行数/范围 | 职责 |
|---|---|---|
| `LibraryCore/Network/ServerPackets.cs` | 1495 行 | 216 个 S→C 包定义 |
| `LibraryCore/Network/GeneralPackets.cs` | 27 行 | 7 个双向通用包（Connected/Ping/PingResponse/CheckVersion/Version/GoodVersion/Disconnect） |
| `LibraryCore/Network/Packet.cs` | 447 行 | 序列化基类：ID 反射表（23-119）、`ReceivePacket`（121-164）、`GetPacketBytes`（165-187）、`WriteObject`（189-299）/`ReadObject`（300-432） |
| `LibraryCore/Network/BaseConnection.cs` | 473 行 | TCP 收发/半包缓冲（69-127）、`Process()` 批量发送（311-394）、反射分发（396-441） |
| `ServerLibrary/Envir/SConnection.cs` | 1671 行 | 连接状态机（GameStage）、观察者复制 `Enqueue`（209-217）、大厅类 C 包处理 |
| `ServerLibrary/Envir/SEnvir.cs` | 4602 行 | 监听（84-106）、`Login`（3262-3419）、`StartGame`（3998-4032） |
| `ServerLibrary/Models/MapObject.cs` | 1862 行 | 三层可见性（965-1004）、`GetInfoPacket/GetDataPacket` 抽象（1686-1687）、`Broadcast`（1689-1693）、HP/MP 变化批量包（161-208） |
| `ServerLibrary/Models/PlayerObject.cs` | 17916 行 | `AddObject/RemoveObject/AddDataObject/RemoveDataObject`（2782-2856）、`GetInfoPacket`（16712-16768）、`StartGame`（902-973）、`StopGame`（975-1039） |
| `ServerLibrary/Models/MonsterObject.cs` | 3199 行 | `GetInfoPacket`（3153-3178）/`GetDataPacket`（3179-3196）、怪物攻击/施法广播（1692、1787 等） |
| `LibraryCore/Globals.cs` | 1309 行 | `SelectInfo`（333-343）、`StartInformation`（345-452）、`ClientUserItem`（454 起）等嵌套数据结构 |
| `Client/Envir/CConnection.cs` | 5110 行 | 原版客户端全部 `Process(S.*)` 处理器 |
| `Client/Models/PlayerObject.cs` | 1615 行 | `PlayerObject(S.ObjectPlayer)` 构造（274-340）：包 → 渲染模型 |
| `Client/Scenes/Views/MapControl.cs` | — | `AddObject/RemoveObject`（1292/1300）：渲染对象注册表 |
| `GodotClient/Network/ServerConnection.cs` | 1132 行 | Godot 版 S 包处理 + C# 事件通知（364-706），含 StartGame 前的 Pending 队列缓冲（289-362） |
| `GodotClient/Scripts/GameScene.cs` | 10233 行 | 订阅 `ServerConnection` 事件驱动渲染（1086-1296 行订阅区，`OnObjectPlayer` 2403） |

## 核心流程

### 1. 对象同步模型（MapObject → S.Object* → 客户端渲染）★重点

服务端把"谁能看到我"分成三张名单（`ServerLibrary/Models/MapObject.cs`，由 `PlayerObject` 持有对偶集合）：

```csharp
// ServerLibrary/Models/MapObject.cs:965-1004
public virtual void AddAllObjects()
{
    foreach (PlayerObject ob in CurrentMap.Players)
    {
        if (CanBeSeenBy(ob))
            ob.AddObject(this);          // 可视层：进入 SeenByPlayers

        if (IsNearBy(ob))
            ob.AddNearBy(this);          // 近旁层：只激活 AI（Activate），不发包
    }

    foreach (PlayerObject ob in SEnvir.Players)
    {
        if (CanDataBeSeenBy(ob))
            ob.AddDataObject(this);      // 数据层：跨图可见（组队/配偶/追踪）
    }
}
```

- **可视层**（`SeenByPlayers`）：进入视野的瞬间由对方 `PlayerObject.AddObject` 调 `Enqueue(ob.GetInfoPacket(this))` 发一条 `S.ObjectPlayer/ObjectMonster/ObjectNPC/ObjectItem/ObjectSpell`（`ServerLibrary/Models/PlayerObject.cs:2782-2790`）；此后该对象的一切动作经 `Broadcast(p)` 复制给名单内所有人（`MapObject.cs:1689-1693`）；离开视野发 `S.ObjectRemove`（`PlayerObject.cs:2839`）。
- **数据层**（`DataSeenByPlayers`）：`CanDataBeSeenBy` 判定同副本 + （组队 / 同公会 / 配偶 / PlayerTracker/BossTracker 追踪属性）（`MapObject.cs:1024-1053`），进入时发 `S.DataObjectPlayer/Monster/Item`（`PlayerObject.cs:2800-2808`），此后位置/血量变化发 `S.DataObjectLocation/HealthMana/MaxHealthMana`，移除发 `S.DataObjectRemove`——WinForms 客户端把它写进 `ClientObjectData` 字典供大地图/组队血条用（`Client/Envir/CConnection.cs:4393-4530`）。
- **近旁层**（`NearByPlayers`）：只决定怪物/对象是否进入 `SEnvir.ActiveObjects` 参与逻辑循环（`MapObject.cs:1005-1022`），不直接产生包。

```csharp
// ServerLibrary/Models/PlayerObject.cs:2782-2790 —— 视野进入
public void AddObject(MapObject ob)
{
    if (ob.SeenByPlayers.Contains(this)) return;
    ob.SeenByPlayers.Add(this);
    VisibleObjects.Add(ob);
    Enqueue(ob.GetInfoPacket(this));   // 多态：Player→S.ObjectPlayer, Monster→S.ObjectMonster ...
}
// ServerLibrary/Models/MapObject.cs:1689-1693 —— 广播
public void Broadcast(Packet p)
{
    foreach (PlayerObject player in SeenByPlayers)
        player.Enqueue(p);
}
```

`SConnection.Enqueue` 会把标记了 `ObserverPacket=true`（默认）的包额外复制给观察者连接（观战系统），见 `ServerLibrary/Envir/SConnection.cs:209-217`。

#### 客户端如何驱动渲染（WinForms 原版）

1. `CConnection.Process(S.ObjectPlayer p)` → `new PlayerObject(p)`（`Client/Envir/CConnection.cs:847-850`）；
2. 构造函数把全部字段写入渲染模型，按 shape 选择贴图库并设初始动画帧：
```csharp
// Client/Models/PlayerObject.cs:274-340（节选）
public PlayerObject(S.ObjectPlayer info)
{
    ObjectID = info.ObjectID;
    Name = info.Name;  Class = info.Class;  Gender = info.Gender;
    ArmourShape = info.Armour;  ArmourColour = info.ArmourColour;
    LibraryWeaponShape = info.Weapon;  HelmetShape = info.Helmet; ...
    UpdateLibraries();   // 按 Weapon/Armour/Horse shape 加载 MirLibrary 贴图
    SetFrame(new ObjectAction(!Dead ? MirAction.Standing : MirAction.Dead, MirDirection.Up, CurrentLocation));
    GameScene.Game.MapControl.AddObject(this);   // 注册进渲染列表
}
```
3. `MapControl.AddObject` 把对象塞进 `Objects` 列表（`Client/Scenes/Views/MapControl.cs:1292-1294`），绘制循环按 Y 坐标排序渲染。
4. 后续动作包不直接改坐标，而是向对象的 `ActionQueue` 压入 `ObjectAction(MirAction.XXX, ...)`，由模型自己的帧动画逐步播放：
```csharp
// Client/Envir/CConnection.cs:1001-1024 —— S.ObjectMove
if (MapObject.User.ObjectID == p.ObjectID && !GameScene.Game.Observer)
{   // 本人：位置不符则强制回拉（Displacement），并叠加服务器下发的 Slow
    if (MapObject.User.CurrentLocation != p.Location || MapObject.User.Direction != p.Direction)
        GameScene.Game.Displacement(p.Direction, p.Location);
    MapObject.User.ServerTime = DateTime.MinValue;
    MapObject.User.NextActionTime += p.Slow;
    return;
}
foreach (MapObject ob in GameScene.Game.MapControl.Objects)
{
    if (ob.ObjectID != p.ObjectID) continue;
    ob.ActionQueue.Add(new ObjectAction(MirAction.Moving, p.Direction, p.Location, p.Distance, MagicType.None));
    return;
}
```
   - 本人路径 vs 他人路径的分歧是全族的统一模式（Turn/Harvest/Attack/Magic/Mining 同构，`CConnection.cs:929-1351`）：本人做**位置校验+回拉**（客户端预测失败时以服务器为准），他人只排队动画。
   - `S.ObjectAttack/ObjectMagic` 还带 `AttackMagic/Type(MagicType)` 与 `AttackElement/TargetID/Targets/Locations`，客户端据此选择攻击动画与技能特效（`CConnection.cs:1254-1351`）。

### 2. 登录→进游戏的 S→C 包序列

```text
G.Connected                      SConnection 构造即发（SConnection.cs:63）
  ↓（Config.CheckVersion=true 时）
G.CheckVersion                   Process(G.Connected)（SConnection.cs:270-284）
  ↓ 客户端回 G.Version(ClientHash)
  ↓ 校验失败 → G.Disconnect{WrongVersion}（SConnection.cs:285-301）
G.GoodVersion{DatabaseKey, SystemDatabaseVersion}   （SConnection.cs:279-283）
  ↓ 客户端 C.Login
S.Login{Result, Characters, Items, BlockList, Address, TestServer, IsGM}  SEnvir.Login（SEnvir.cs:3396-3408）
  ↓ 选角 C.StartGame
S.StartGame{Result, StartInformation}     PlayerObject.OnSpawned（PlayerObject.cs:1059）
S.MapChanged{MapIndex, InstanceIndex}     切图时（PlayerObject.cs:1481）
S.ObjectPlayer/Monster/NPC/Item/Spell ×N  AddAllObjects 涌入
S.StatsUpdate / S.InformMaxExperience / S.WeightUpdate / S.DayChanged ...
```

## 数据结构/协议细节

帧格式 `[int32 小端长度(含自身)][int16 包ID][反射序列化字段...]`、包 ID 由 LibraryCore 反射排序决定、null 类字段前缀 1 字节 bool、List/Dictionary 前缀 int32 count——机制全解见 `docs/codebase/protocol/connection-lifecycle.md` 第 3 节。以下照抄重点包原文。

### S.ObjectPlayer（定义 `ServerPackets.cs:290-339`）——玩家出现在视野

```csharp
public sealed class ObjectPlayer : Packet
{
    public int Index { get; set; }              // CharacterInfo.Index（角色库主键）

    public uint ObjectID { get; set; }          // 世界对象唯一 ID（MapObject.ObjectID）
    public string Name { get; set; }

    public string Caption { get; set; }         // 称号（里程碑 Title）
    public Color CaptionOutlineColour { get; set; }
    public Color NameColour { get; set; }       // PK 颜色（白/黄/红）
    public string GuildName { get; set; }

    public MirDirection Direction { get; set; }
    public Point Location { get; set; }

    public MirClass Class { get; set; }
    public MirGender Gender { get; set; }

    public int HairType { get; set; }
    public Color HairColour { get; set; }
    public int Weapon { get; set; }             // Info.Shape，-1=空手
    public int Shield { get; set; }
    public int Armour { get; set; }
    public int Costume { get; set; }
    public Color ArmourColour { get; set; }     // 染色
    public ExteriorEffect ArmourEffect { get; set; }
    public ExteriorEffect EmblemEffect { get; set; }
    public ExteriorEffect WeaponEffect { get; set; }
    public ExteriorEffect ShieldEffect { get; set; }

    public int Light { get; set; }              // Stat.Light 光照半径
    public int SizePercent { get; set; }        // 体型缩放

    public bool Dead { get; set; }
    public PoisonType Poison { get; set; }

    public Dictionary<BuffType, int> Buffs { get; set; }  // 仅 Visible BUFF

    public HorseType Horse { get; set; }

    public int Helmet { get; set; }

    public int HorseShape { get; set; }

    public string FiltersClass;
    public string FiltersRarity;
    public string FiltersItemType;

    public bool HideHead;
}
```

服务端填充处 `PlayerObject.GetInfoPacket`（`ServerLibrary/Models/PlayerObject.cs:16712-16768`，注意 `if (ob == this) return null;` 自己不发自己）。客户端处理 `CConnection.cs:847-850` → `new PlayerObject(p)`。

### S.ObjectMonster（`ServerPackets.cs:340-367`）——怪物出现

```csharp
public sealed class ObjectMonster : Packet
{
    public uint ObjectID { get; set; }
    public int MonsterIndex { get; set; }       // MonsterInfo.Index，客户端查 Globals.MonsterInfoList
    public string CustomName { get; set; }
    public Color NameColour { get; set; }
    public string PetOwner { get; set; }        // 非空=玩家宠物

    public MirDirection Direction { get; set; }
    public Point Location { get; set; }

    public bool Dead { get; set; }
    public bool Skeleton { get; set; }          // 尸体已被采集

    public PoisonType Poison { get; set; }

    public bool EasterEvent { get; set; }
    public bool HalloweenEvent { get; set; }
    public bool ChristmasEvent { get; set; }

    public Dictionary<BuffType, int> Buffs { get; set; }
    public bool Extra { get; set; }

    public int Extra1 { get; set; }
    public Color Colour { get; set; }

    public ClientCompanionObject CompanionObject { get; set; }  // 伴侣外观
}
```

填充处 `MonsterObject.GetInfoPacket`（`ServerLibrary/Models/MonsterObject.cs:3153-3178`）。客户端 `CConnection.cs:855-859` → `new MonsterObject(p)`。

### S.StartGame + StartInformation（`ServerPackets.cs:82-90`；`LibraryCore/Globals.cs:345-452`）——进游戏全量快照

```csharp
public sealed class StartGame : Packet
{
    public StartGameResult Result { get; set; }
    public string Message { get; set; }
    public TimeSpan Duration { get; set; }
    public StartInformation StartInformation { get; set; }
}
```

`StartInformation`（`LibraryCore/Globals.cs:345-452`）字段全表：`Index/ObjectID/Name/Caption/CaptionOutlineColour/NameColour/GuildName/GuildRank/Class/Gender/Location/Direction/MapIndex/InstanceIndex/Level/HairType/HairColour/Weapon/Armour/Costume/Shield/ArmourColour/ArmourEffect/EmblemEffect/WeaponEffect/ShieldEffect/Experience/CurrentHP/CurrentMP/CurrentFP/AttackMode/PetMode/OnlineState/Discipline/HermitPoints/DayTime/TimeOfDay/TimeOfDayLabel/AllowGroup/AllowTrade/Friends/Items/BeltLinks/AutoPotionLinks/Milestones/Magics/Buffs/Currencies/Poison/InSafeZone/Observable/Dead/Horse/HelmetShape/HorseShape/HideHead/Quests/CompanionUnlocks/AvailableCompanions([CompleteObject] 解析)/Companions/Companion/StorageSize/FiltersClass/FiltersRarity/FiltersItemType/StruckEnabled/HermitEnabled/MaxGemPurity`。发送点 `PlayerObject.OnSpawned`（`PlayerObject.cs:1059`）。客户端 `CConnection.cs:702-792` 构建 `GameScene` 并 `FillStorage`。

### S.StatsUpdate（`ServerPackets.cs:502-507`）——自身属性总刷新

```csharp
public sealed class StatsUpdate : Packet
{
    public Stats Stats { get; set; }          // LibraryCore.Stat 枚举字典
    public Stats HermitStats { get; set; }    // 已分配的潜能点
    public int HermitPoints { get; set; }     // 剩余潜能点
}
```

发送点 `ServerLibrary/Models/PlayerObject.cs:1215`（`RefreshStats` 后）。客户端 `CConnection.cs:2508-2513` 写 `MapObject.User.Stats`。他人属性变化用 `S.ObjectStats`（`ServerPackets.cs:517-521`，客户端 `CConnection.cs:1847`；**服务端发送点未找到实现**，疑为遗留）。

### 战斗数值四连（定义行见分组表）

```csharp
// ServerPackets.cs:508-516 / 523-527 / 529-533
public sealed class HealthChanged : Packet
{
    public uint ObjectID { get; set; }
    public int Change { get; set; }           // 正=伤害负值/负=回复（CurrentHP - DisplayHP）
    public bool Miss { get; set; }
    public bool Block { get; set; }
    public bool Critical { get; set; }
    public bool Resist { get; set; }
}
```

由 `MapObject.ProcessHPMP` 每 200ms 批量 flush（`ServerLibrary/Models/MapObject.cs:161-208`）：HP/MP/FP 变化分别广播 `S.HealthChanged/S.ManaChanged/S.FocusChanged`，同时给数据层发 `S.DataObjectHealthMana`。客户端 `CConnection.cs:1863/1970/1982` 弹伤害数字。

### 动作包公共骨架

```csharp
// ServerPackets.cs:105-111（Turn）/ 149-157（Move）/ 180-193（Attack）/ 206-220（Magic）
public sealed class ObjectMove : Packet
{
    public uint ObjectID { get; set; }
    public MirDirection Direction { get; set; }
    public Point Location { get; set; }
    public int Distance { get; set; }         // 2=跑步/骑乘
    public TimeSpan Slow { get; set; }        // 服务器判定额外延迟（负重/减速）
    public bool MapChanged { get; set; }
}
public sealed class ObjectAttack : Packet
{
    public uint ObjectID { get; set; }
    public MirDirection Direction { get; set; }
    public Point Location { get; set; }
    public MagicType AttackMagic { get; set; }
    public Element AttackElement { get; set; }
    public uint TargetID { get; set; }
    public TimeSpan Slow { get; set; }
}
public sealed class ObjectMagic : Packet
{
    public uint ObjectID { get; set; }
    public MirDirection Direction { get; set; }
    public Point CurrentLocation { get; set; }
    public MagicType Type { get; set; }
    public List<uint> Targets { get; set; } = new List<uint>();
    public List<Point> Locations { get; set; } = new List<Point>();
    public bool Cast { get; set; }
    public Element AttackElement { get; set; }
    public TimeSpan Slow { get; set; }
}
```

玩家普攻广播 `PlayerObject.cs:14812`，施法 `PlayerObject.cs:14907`，怪物攻击/施法 `MonsterObject.cs:1692/1787`。

### DataObject* 家族（`ServerPackets.cs:1246-1323`）

`DataObjectRemove{ObjectID}` / `DataObjectPlayer{ObjectID,MapIndex,CurrentLocation,Name,Health,Mana,Dead,MaxHealth,MaxMana}` / `DataObjectMonster{ObjectID,MapIndex,CurrentLocation,MonsterInfo([CompleteObject] 由 MonsterIndex 解析),MonsterIndex,PetOwner,Health,Stats,Dead}` / `DataObjectItem{ObjectID,MapIndex,CurrentLocation,ItemInfo([CompleteObject]),ItemIndex}` / `DataObjectLocation{ObjectID,MapIndex,CurrentLocation}` / `DataObjectHealthMana{ObjectID,Health,Mana,Dead}` / `DataObjectMaxHealthMana{ObjectID,MaxHealth,MaxMana,Stats}`。发送点：`PlayerObject.cs:2855`（Remove）、`16771`（Player）、`MapObject.cs:887`（Location）、`204`（HealthMana）、`MonsterObject.cs:3181`（Monster）、`ItemObject.cs:206`（Item）。客户端处理 `CConnection.cs:4393-4530`（统一写 `ClientObjectData` 字典）。

### S.Login（`ServerPackets.cs:25-43`）

```csharp
public sealed class Login : Packet
{
    public LoginResult Result { get; set; }
    public string Message { get; set; }
    public TimeSpan Duration { get; set; }
    public List<SelectInfo> Characters { get; set; }
    public List<ClientUserItem> Items { get; set; }     // 账号仓库
    public List<ClientBlockInfo> BlockList { get; set; }
    public string Address { get; set; }                 // 商城跳转 URL + Key
    public bool TestServer { get; set; }
    /// <summary>GM 权限 (Account.Admin 或 TempAdmin): 小地图点击传送等 GM 功能据此启用</summary>
    public bool IsGM { get; set; }
}
```

发送点 `SEnvir.Login`（`SEnvir.cs:3396-3408`；失败分支见 3276-3384，含顶号逻辑）。客户端 `CConnection.cs:475-587`。

### 全量分组清单

> 列格式：包名（`ServerPackets.cs` 定义行）｜字段｜服务端典型发送点｜客户端处理（`Client/Envir/CConnection.cs:行号`）。
> 发送点标注 ★ 的为本文正文已核读的代码；其余来自全库 `grep "new S.XXX"` 的实际命中（本会话输出）。

#### A. 连接/通用（GeneralPackets.cs，双向）

| 包 | 字段 | 发送点 | 客户端处理 |
|---|---|---|---|
| Connected | （空） | ★ `SConnection.cs:63` | 128-133 |
| Ping | （空） | ★ `SConnection.cs:177` | 156-159（回 G.Ping） |
| PingResponse | `int Ping` | ★ `SConnection.cs:311` | 160-163 |
| CheckVersion | （空） | ★ `SConnection.cs:274` | 134-144（回 SHA256 of 自身 dll） |
| Version | `byte[] ClientHash` | 客户端发（C 侧） | — |
| GoodVersion | `byte[] DatabaseKey; string SystemDatabaseVersion` | ★ `SConnection.cs:279/296` | 145-155（`Encryption.SetKey` 后 `LoadDatabase`） |
| Disconnect | `DisconnectReason Reason` | ★ `BaseConnection.cs:343`（超时）/`Kick.cs:29`/`SEnvir.cs:124/3353/3368/3382` | 74-126（按 Reason 弹框） |

#### B. 登录/账户（9 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| NewAccount(14) | `NewAccountResult Result` | SEnvir.cs:3424 | 165 |
| ChangePassword(18) | `ChangePasswordResult Result; string Message; TimeSpan Duration` | SEnvir.cs:3574 | 217 |
| Login(25) | 见上文详解 | ★ SEnvir.cs:3396 | 475 |
| RequestPasswordReset(44) | `RequestPasswordResetResult Result; string Message; TimeSpan Duration` | SEnvir.cs:3657 | 284 |
| ResetPassword(50) | `ResetPasswordResult Result` | SEnvir.cs:3702 | 365 |
| Activation(54) | `ActivationResult Result` | SEnvir.cs:3745 | 393 |
| RequestActivationKey(58) | `RequestActivationKeyResult Result; TimeSpan Duration` | SEnvir.cs:3774 | 417 |
| SelectLogout(63) | （空） | ★ SConnection.cs:147/366 | 588 |
| GameLogout(66) | `List<SelectInfo> Characters` | ★ SConnection.cs:142/376 | 593 |

#### C. 角色/选角/信息（12 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| NewCharacter(70) | `NewCharacterResult Result; SelectInfo Character` | SEnvir.cs:3818 | 616 |
| DeleteCharacter(76) | `DeleteCharacterResult Result; int DeletedIndex` | ★ SEnvir.cs:3990/3996 | 673 |
| StartGame(82) | 见上文详解 | ★ PlayerObject.cs:1059（失败分支 SEnvir.cs:4003） | 702 |
| UserLocation(96) | `MirDirection Direction; Point Location` | ★ SConnection.cs:435（非法移动回拉） | 814（Displacement） |
| Inspect(795) | `Name; GuildName; GuildRank; GuildFlag; GuildColour; Partner; Class; Level; Gender; Items; Hair; HairColour; Fame; Ranking` | PlayerObject.cs:1967 | 3525 |
| Rankings(820) | `OnlineOnly; Class; StartIndex; Total; AllowObservation; List<RankInfo> Ranks` | SEnvir.cs:4136 | 3533 |
| RankSearch(831) | `RankInfo Rank; int StartIndex` | ★ SConnection.cs:875 | 3539 |
| StartObserver(837) | `StartInformation StartInformation; List<ClientUserItem> Items` | PlayerObject.cs:1187 | 3545 |
| ObservableSwitch(842) | `bool Allow` | PlayerObject.cs:1303 | 3595 |
| HelmetToggle(1334) | `bool HideHelmet` | PlayerObject.cs:1160 | 4597 |
| StorageSize(1339) | `int Size` | PlayerObject.cs:6802 | 4602 |
| PlayerChangeUpdate(1344) | `ObjectID; Name; Caption; CaptionOutlineColour; Gender; HairType; HairColour; ArmourColour` | ★ PlayerObject.cs:16824 | 4606 |

#### D. 地图/环境/系统（8 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| MapChanged(91) | `int MapIndex; int InstanceIndex` | PlayerObject.cs:1481 | 793 |
| DayChanged(435) | `float DayTime` | SEnvir.cs:377 | 805 |
| TimeOfDayChanged(440) | `TimeOfDay TimeOfDay; string TimeOfDayLabel` | SEnvir.cs:363 | 809 |
| MapEffect(268) | `Point Location; Effect Effect; MirDirection Direction` | Magics/Assassin/BurningFire.cs:123 等地面特效 | 1694 |
| SafeZoneChanged(787) | `bool InSafeZone` | PlayerObject.cs:1521 | 3515 |
| JoinInstance(1412) | `InstanceResult Result; bool Success` | ★ PlayerObject.cs:16860 | 5038（空实现） |
| AutoPathChanged(9) | `List<AutoPathRoute> Routes` | Models/Players/AutoPathService.cs:145 | 801 |
| SetTimer(1455) | `string Key; byte Type; int Seconds` | PlayerObject.cs:17198 | 4592 |

#### E. 对象同步（可视层，26 个）★核心

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| ObjectPlayer(290) | 详见上文 | ★ PlayerObject.cs:16716（GetInfoPacket） | 847 |
| ObjectMonster(340) | 详见上文 | ★ MonsterObject.cs:3155 | 855 |
| ObjectNPC(369) | `ObjectID; NPCIndex; CurrentLocation; Direction` | NPCObject.cs:718 | 860 |
| ObjectItem(378) | `ObjectID; ClientUserItem Item; Point Location` | ItemObject.cs:197 | 851 |
| ObjectSpell(386) | `ObjectID; Direction; Location; SpellEffect Effect; int Power` | SpellObject.cs:295 | 865 |
| ObjectSpellChanged(395) | `ObjectID; int Power` | PlayerObject.cs:15049 | 869 |
| ObjectRemove(101) | `uint ObjectID` | ★ PlayerObject.cs:2839 | 818 |
| ObjectTurn(105) | `ObjectID; Direction; Location; TimeSpan Slow` | ★ PlayerObject.cs:13966 / MapObject.cs:957 | 929 |
| ObjectHarvest(112) | `ObjectID; Direction; Location; Slow` | PlayerObject.cs:14003 | 950 |
| ObjectMove(149) | 详见上文 | ★ PlayerObject.cs:14687 / MonsterObject.cs:3096 | 1001 |
| ObjectDash(158) | `ObjectID; Direction; Location; int Distance; MagicType Magic` | Magics/Warrior/ShoulderDash.cs:258 | 1218 |
| ObjectPushed(166) | `ObjectID; Direction; Location` | ★ MapObject.cs:1717 | 1025 |
| ObjectIdle(172) | `ObjectID; Direction; Location; int Type` | Monsters/Companion.cs:289 | 1243 |
| ObjectShow(248) | `ObjectID; Direction; Location` | Monsters/ArchLichTaedu.cs:45 | 970 |
| ObjectHide(255) | `ObjectID; Direction; Location` | Monsters/BlockingObject.cs:48 | 991 |
| ObjectEffect(262) | `ObjectID; Effect Effect` | ★ MapObject.cs:949/960（传送） | 1483 |
| ObjectNameColour(400) | `ObjectID; Color Colour` | ★ MapObject.cs:152 | 1035 |
| ObjectPetOwnerChanged(243) | `ObjectID; string PetOwner` | Magics/Wizard/ElectricShock.cs:132 | 1834 |
| ObjectLeveled(457) | `uint ObjectID` | PlayerObject.cs:12715 | 2040 |
| ObjectRevive(461) | `ObjectID; Point Location; bool Effect` | Magics/Taoist/Resurrection.cs:109 | 2056 |
| ObjectHarvested(549) | `ObjectID; Direction; Location` | MonsterObject.cs:3151 | 1470 |
| PlayerUpdate(406) | `ObjectID; Weapon; Shield; Armour; Costume; ArmourColour; ArmourEffect; EmblemEffect; WeaponEffect; ShieldEffect; HorseArmour; Helmet; Light; SizePercent; HideHead` | ★ PlayerObject.cs:16791（SendShapeUpdate） | 881 |
| ObjectMount(119) | `ObjectID; HorseType Horse` | PlayerObject.cs:14161 | 1045 |
| ObjectFishing(124) | `ObjectID; FishingState State; Direction; Point FloatLocation; bool FishFound` | PlayerObject.cs:14392 | 1075 |
| FishingStats(132) | `CanAutoCast; CurrentPoints; ThrowQuality; RequiredPoints; MovementSpeed; RequiredAccuracy` | PlayerObject.cs:14375 | 1113 |
| ObjectTaming(142) | `ObjectID; TamingState State; Direction; uint TamingObjectID` | PlayerObject.cs:14506 | 1118 |

#### F. 战斗（19 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| ObjectAttack(180) | 详见上文 | ★ PlayerObject.cs:14812 / MonsterObject.cs:1692 | 1254 |
| ObjectRangeAttack(194) | `ObjectID; Direction; Location; MagicType AttackMagic; Element AttackElement; List<uint> Targets` | PlayerObject.cs:15188 / Monsters/ArchLichTaedu.cs:96 | 1298 |
| ObjectMagic(206) | 详见上文 | ★ PlayerObject.cs:14907 / MonsterObject.cs:1787 | 1317 |
| ObjectProjectile(221) | `ObjectID; Direction; CurrentLocation; MagicType Type; List<uint> Targets; List<Point> Locations` | Magics/Warrior/ElementalSwords.cs:76 | 1352 |
| ObjectMining(233) | `ObjectID; Direction; Location; Slow; bool Effect` | PlayerObject.cs:15077 | 1276 |
| ObjectStruck(535) | `ObjectID; Direction; Location; uint AttackerID; Element Element` | PlayerObject.cs:15826 / MonsterObject.cs:2439 | 1164 |
| ObjectDied(543) | `ObjectID; Direction; Location` | ★ MapObject.cs:1683（Die） | 1455 |
| HealthChanged(508) / ManaChanged(523) / FocusChanged(529) | 见上文 / `ObjectID; int Change` ×2 | ★ MapObject.cs:172/187/196 | 1863/1970/1982 |
| ObjectPoison(285) | `ObjectID; PoisonType Poison` | ★ MapObject.cs:409（ProcessPoison 后广播） | 1824 |
| ObjectBuffAdd(274) | `ObjectID; BuffType Type; int Extra` | ★ MapObject.cs:1487 | 1794 |
| ObjectBuffRemove(280) | `ObjectID; BuffType Type` | ★ MapObject.cs:1508 | 1814 |
| CombatTime(791) | （空） | PlayerObject.cs:314 | 3520 |
| MagicToggle(428) | `MagicType Magic; bool CanUse` | Magics/Assassin/CalamityOfFullMoon.cs:28 | 1906 |
| ChangeAttackMode(937) / ChangePetMode(941) | `AttackMode Mode` / `PetMode Mode` | ★ SConnection.cs:1116/1132 | 3748/3755 |
| MountFailed(952) | `HorseType Horse` | PlayerObject.cs:14131 | 1069 |
| ReviveTimers(1156) | `TimeSpan ItemReviveTime; TimeSpan ReincarnationPillTime` | PlayerObject.cs:2029 | 4187 |

#### G. 成长/数值（12 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| StatsUpdate(502) | 见上文详解 | ★ PlayerObject.cs:1215 | 2508 |
| ObjectStats(517) | `ObjectID; Stats Stats` | **未找到服务端发送点** | 1847 |
| LevelChanged(451) | `int Level; decimal Experience; decimal MaxExperience` | PlayerObject.cs:12714 | 1998 |
| GainedExperience(467) | `decimal Amount` | PlayerObject.cs:16309 | 2009 |
| InformMaxExperience(446) | `decimal MaxExperience` | ★ PlayerObject.cs:1075 | 1994 |
| WeightUpdate(957) | `int BagWeight; int WearWeight; int HandWeight` | ★ PlayerObject.cs:1217 | 3762 |
| DisciplineUpdate(1439) | `ClientUserDiscipline Discipline` | Commands/Admin/ResetDiscipline.cs:35 | 4568 |
| DisciplineExperienceChanged(1444) | `long Experience` | PlayerObject.cs:17343 | 4580 |
| NewMagic(472) | `ClientUserMagic Magic` | Commands/Admin/GiveSkills.cs:38 | 1877 |
| MagicLeveled(476) | `int InfoIndex; MagicInfo Info; int Level; long Experience`（[CompleteObject] 回查 Info） | GiveSkills.cs:54 | 1890 |
| MagicCooldown(489) | `int InfoIndex; int Delay; MagicInfo Info`（[CompleteObject]） | Models/MagicObject.cs:350 | 1902 |
| CurrencyChanged(946) | `int CurrencyIndex; long Amount` | PlayerObject.cs:8396 | 2242 |

#### H. 物品（19 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| ItemsGained(558) | `List<ClientUserItem> Items` | PlayerObject.cs:11169 | 2079 |
| ItemMove(562) | `GridType FromGrid; GridType ToGrid; int FromSlot; int ToSlot; bool MergeItem; bool Success` | PlayerObject.cs:3089 | 2102 |
| ItemSort(573) | `GridType Grid; List<ClientUserItem> Items; bool Success` | PlayerObject.cs:8097（`S.ItemSort result = new()` 目标类型 new） | 2626 |
| ItemSplit(580) | `GridType Grid; int Slot; long Count; int NewSlot; bool Success` | PlayerObject.cs:8273 | 2518 |
| ItemDelete(590) | `GridType Grid; int Slot; bool Success` | PlayerObject.cs:8108 | 2669 |
| ItemLock(597) | `GridType Grid; int Slot; bool Locked` | PlayerObject.cs:8261 | 2587 |
| ItemUseDelay(605) | `TimeSpan Delay` | Magics/Assassin/SummonPuppet.cs:112 | 2514 |
| ItemChanged(609) | `CellLinkInfo Link; bool Success` | Monsters/Companion.cs:458 | 2254 |
| ItemsChanged(664) | `List<CellLinkInfo> Links; bool Success` | PlayerObject.cs:10049 | 2318 |
| ItemStatsChanged(615) / ItemStatsRefreshed(621) | `GridType GridType; int Slot; Stats NewStats` | NPCObject.cs:117 / Commands/Admin/AddStat.cs:33 | 2382/2436 |
| ItemDurability(627) | `GridType GridType; int Slot; int CurrentDurability` | PlayerObject.cs:17538 | 2469 |
| ItemExperience(633) | `CellLinkInfo Target; decimal Experience; int Level; UserItemFlags Flags` | PlayerObject.cs:10690 | 2709 |
| ItemAcessoryRefined(1405) | `GridType GridType; int Slot; Stats NewStats` | PlayerObject.cs:11518 | 4996 |
| FortuneUpdate(1359) | `List<ClientFortuneInfo> Fortunes` | PlayerObject.cs:1174 | 4627 |
| LootBoxOpen(1462) | `int Slot; List<ClientLootBoxItemInfo> Items` | PlayerObject.cs:17600 | 5059 |
| LootBoxClose(1468) | （空） | PlayerObject.cs:17687 | 5066 |
| BundleOpen(1473) | `int Slot; List<ClientBundleItemInfo> Items` | PlayerObject.cs:17759 | 5047 |
| BundleClose(1479) | （空） | PlayerObject.cs:17819 | 5054 |

#### I. NPC 交互（15 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| NPCResponse(650) | `uint ObjectID; int Index; List<ClientNPCValues> Values; NPCPage Page`（[CompleteObject] 回查 Page） | NPCObject.cs:57 | 2766 |
| NPCClose(703) | （空） | ★ SConnection.cs:700 | 3352 |
| NPCRepair(669) | `List<CellLinkInfo> Links; bool Special; bool Success; TimeSpan SpecialRepairDelay` | PlayerObject.cs:11675 | 2770 |
| NPCRefinementStone(676) | `List<CellLinkInfo> IronOres; SilverOres; DiamondOres; GoldOres; Crystal`（5 个列表） | **未找到服务端发送点** | **无处理器** |
| NPCRefine(684) | `RefineType RefineType; RefineQuality RefineQuality; List<CellLinkInfo> Ores; Items; Specials; bool Success` | PlayerObject.cs:12200 | 2831 |
| NPCMasterRefine(693) | `List<CellLinkInfo> Fragment1s; Fragment2s; Fragment3s; Stones; Specials; bool Success` | PlayerObject.cs:12798 | 3027 |
| NPCAccessoryLevelUp(707) | `CellLinkInfo Target; List<CellLinkInfo> Links` | PlayerObject.cs:10543 | 3358 |
| NPCAccessoryUpgrade(713) | `CellLinkInfo Target; RefineType RefineType; bool Success` | **未找到服务端发送点** | **无处理器** |
| NPCRefineRetrieve(721) | `int Index` | PlayerObject.cs:12669 | 3340 |
| RefineList(725) | `List<ClientRefineInfo> List` | PlayerObject.cs:1110 | 3336 |
| NPCWeaponCraft(1364) | `CellLinkInfo Template; Yellow; Blue; Red; Purple; Green; Grey（7 个）; bool Success` | PlayerObject.cs:13555 | 4648 |
| NPCAccessoryRefine(1377) | `CellLinkInfo Target; OreTarget; List<CellLinkInfo> Links; RefineType RefineType; bool Success` | PlayerObject.cs:11345 | 4956 |
| NPCSocketItem(1385) | `GridType GridType; int Slot; int SocketSlot; int GemShape; ClientUserItem Item; bool Success; string Message` | PlayerObject.cs:10985 | 5090 |
| NPCSocketCombine(1395) | `List<int> ClearedSlots; List<ClientUserItem> Items; int ResultSlot; bool Accepted; bool Success; string Message` | PlayerObject.cs:11224 | 5095 |
| NPCRoll(1449) | `int Type; int Result` | PlayerObject.cs:10198 | 4587 |

#### J. 社交：聊天/组队/交易/婚姻/好友/黑名单（31 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| Chat(641) | `uint ObjectID; string Text; MessageType Type; List<ClientUserItem> LinkedItems; bool OverheadOnly` | ★ SConnection.cs:225（ReceiveChat）/ Events/Actions/PlayerMessage.cs:39 | 2745 |
| GroupSwitch(730) | `bool Allow` | PlayerObject.cs:5615 | 3396 |
| GroupMember(734) | `uint ObjectID; string Name` | PlayerObject.cs:1265 | 3400 |
| GroupRemove(739) | `uint ObjectID` | PlayerObject.cs:5916 | 3414 |
| GroupInvite(743) | `string Name` | PlayerObject.cs:5748 | 3447 |
| GroupRequest(747) | `string Name; int Level; MirClass Class` | PlayerObject.cs:5821 | 3457 |
| GroupLFG(754) | `List<ClientLookingForGroup> List` | PlayerObject.cs:6023 | 3467 |
| GroupUpdate(759) | `ClientLookingForGroup Group` | PlayerObject.cs:6004 | 3472 |
| BuffAdd(764) | `ClientBuffInfo Buff` | PlayerObject.cs:9492 | 3477 |
| BuffRemove(768) | `int Index` | PlayerObject.cs:9558 | 3483 |
| BuffChanged(772) | `int Index; Stats Stats` | Magics/Assassin/FlashOfLight.cs:88 | 3496 |
| BuffTime(777) | `int Index; TimeSpan Time` | PlayerObject.cs:15795 | 3502 |
| BuffPaused(782) | `int Index; bool Paused` | PlayerObject.cs:9626 | 3508 |
| TradeRequest(965) | `string Name` | PlayerObject.cs:9723 | 3771 |
| TradeOpen(969) | `string Name` | PlayerObject.cs:1223 | 3779 |
| TradeClose(974) | （空） | PlayerObject.cs:9640 | 3785 |
| TradeAddItem(976) | `CellLinkInfo Cell; bool Success` | PlayerObject.cs:1229 | 3790 |
| TradeAddGold(982) | `long Gold` | PlayerObject.cs:1226 | 3851 |
| TradeItemAdded(987) | `ClientUserItem Item` | PlayerObject.cs:1236 | 3840 |
| TradeGoldAdded(992) | `long Gold` | PlayerObject.cs:1232 | 3856 |
| TradeUnlock(996) | （空） | PlayerObject.cs:10045 | 3861 |
| MarriageInvite(1224) | `string Name` | PlayerObject.cs:2990 | 4341 |
| MarriageInfo(1228) | `ClientPlayerInfo Partner` | PlayerObject.cs:3174 | 4353 |
| MarriageRemoveRing(1232) / MarriageMakeRing(1236) | （空） | PlayerObject.cs:3170/3090 | 4367/4373 |
| MarriageOnlineChanged(1241) | `uint ObjectID` | PlayerObject.cs:1028 | 4379 |
| FriendAdd(1430)/FriendRemove(1434)/FriendUpdate(1425) | `ClientFriendInfo Info` / `int Index` / `ClientFriendInfo Info` | ★ SConnection.cs:1561/1574、PlayerObject.cs:17323 | 4547/4552/4561 |
| BlockAdd(1324)/BlockRemove(1329) | `ClientBlockInfo Info` / `int Index` | ★ SConnection.cs:1398/1410 | 4532/4538 |

#### K. 公会（26 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| GuildCreate(999) | （空） | PlayerObject.cs:4631 | 3866 |
| GuildInfo(1003) | `ClientGuildInfo Guild` | PlayerObject.cs:4837 | 3870 |
| GuildNoticeChanged(1007) | `string Notice` | PlayerObject.cs:4745 | 3900 |
| GuildNewItem(1011) | `int Slot; ClientUserItem Item` | PlayerObject.cs:7943 | 3945 |
| GuildGetItem(1017) | `GridType Grid; int Slot; ClientUserItem Item` | PlayerObject.cs:7866 | 3908 |
| GuildUpdate(1023) | `MemberLimit; StorageLimit; GuildFunds; DailyGrowth; GuildLevel; Tax; TotalContribution; DailyContribution; DefaultRank; DefaultPermission; Colour; Flag; List<ClientGuildMemberInfo> Members` | DBModels/GuildInfo.cs:360 | 3950 |
| GuildKick(1045) | `int Index` | ★ PlayerObject.cs:4848/5489 | 4006 |
| GuildTax(1049)/GuildIncreaseMember(1053)/GuildIncreaseStorage(1057)/GuildInviteMember(1061) | （空） | PlayerObject.cs:4856/4877/4913/4947 | 4037/4022/4027/4032 |
| GuildInvite(1065) | `string Name; string GuildName` | PlayerObject.cs:4997 | 4053 |
| GuildStats(1070) | `int Index; Stats Stats` | **未找到服务端发送点** | **无处理器** |
| GuildMemberOffline(1077) | `int Index` | PlayerObject.cs:985 | 4042 |
| GuildMemberOnline(1081) | `int Index; string Name; uint ObjectID` | PlayerObject.cs:1084 | 4065 |
| GuildMemberContribution(1088) | `int Index; long Contribution` | DBModels/GuildMemberInfo.cs:146 | 4076 |
| GuildDayReset(1094) | （空） | SEnvir.cs:1600 | 4092 |
| GuildFundsChanged(1098) | `long Change` | PlayerObject.cs:10297 | 4104 |
| GuildChanged(1102) | `uint ObjectID; string GuildName; string GuildRank` | PlayerObject.cs:4786 | 4113 |
| GuildWarFinished(1109) | `string GuildName` | SEnvir.cs:1873 | 4140 |
| GuildWar(1114) | `bool Success` | PlayerObject.cs:5001 | 4125（空） |
| GuildWarStarted(1119) | `string GuildName; TimeSpan Duration` | PlayerObject.cs:1273 | 4130 |
| GuildConquestDate(1124) | `int Index; TimeSpan WarTime; DateTime WarDate`（[CompleteObject] Update 把 WarTime 换算成本地 WarDate） | Models/ConquestWar.cs:105 | 4180 |
| GuildCastleInfo(1140) | `int Index; string Owner` | Commands/Admin/TakeCastle.cs:46 | 4168 |
| GuildConquestStarted(1146)/GuildConquestFinished(1151) | `int Index` | ConquestWar.cs:46/85 | 4150/4157 |

#### L. 商城/寄售（12 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| MarketPlaceHistory(847) | `int Index; long SaleCount; long LastPrice; long AveragePrice; int Display` | ★ SConnection.cs:920 | 3600 |
| MarketPlaceConsign(856) | `List<ClientMarketPlaceInfo> Consignments` | PlayerObject.cs:1112 | 3605 |
| MarketPlaceSearch(861) | `int Count; List<ClientMarketPlaceInfo> Results` | ★ SConnection.cs:1022（首屏最多 9 条） | 3609 |
| MarketPlaceSearchCount(866) | `int Count` | **未找到服务端发送点** | 3613 |
| MarketPlaceSearchIndex(871) | `int Index; ClientMarketPlaceInfo Result` | ★ SConnection.cs:1042（按需下发） | 3617 |
| MarketPlaceBuy(877) | `int Index; long Count; bool Success` | PlayerObject.cs:4135 | 3625 |
| MarketPlaceStoreBuy(883) | （空） | PlayerObject.cs:4321 | 3629（空） |
| MarketPlaceConsignChanged(909) | `int Index; long Count` | PlayerObject.cs:4128 | 3621 |
| GameStoreData(887) | `List<int> Favourites; List<int> TopItems` | PlayerObject.cs:1115 | 3634 |
| GameStoreTopItems(893) | `List<int> Items` | PlayerObject.cs:4586 | 3640 |
| GameStoreFavouriteChanged(898) | `int Index; bool Favourited` | PlayerObject.cs:4433 | 3645 |
| GameStoreGift(904) | `GameStoreGiftResult Result` | PlayerObject.cs:4547 | 3650 |

#### M. 邮件（5 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| MailList(916) | `List<ClientMailInfo> Mail` | PlayerObject.cs:1114 | 3681 |
| MailNew(920) | `ClientMailInfo Mail` | PlayerObject.cs:3914 | 3687 |
| MailDelete(924) | `int Index` | PlayerObject.cs:3695 | 3695 |
| MailItemDelete(928) | `int Index; int Slot` | PlayerObject.cs:3703 | 3709 |
| MailSend(933) | （空） | PlayerObject.cs:3747 | 3743 |

#### N. 任务/伴侣/里程碑/其他（15 个）

| 包（行） | 字段 | 发送点 | 客户端 |
|---|---|---|---|
| QuestChanged(1162) | `ClientUserQuest Quest` | Models/Map.cs:575 | 4193 |
| QuestCancelled(1167) | `int Index` | PlayerObject.cs:3682 | 4221 |
| CompanionUnlock(1172) | `int Index` | PlayerObject.cs:3185 | 4234 |
| CompanionAdopt(1176) | `ClientUserCompanion UserCompanion` | PlayerObject.cs:3255 | 4243 |
| CompanionRetrieve(1180)/CompanionRelease(1184) | `int Index` | PlayerObject.cs:3336/3367 | 4260/4264 |
| CompanionStore(1188) | （空） | PlayerObject.cs:3335 | 4253 |
| CompanionWeightUpdate(1191) | `int BagWeight; int MaxBagWeight; int InventorySize` | Monsters/Companion.cs:146 | 4274 |
| CompanionShapeUpdate(1197) | `uint ObjectID; int HeadShape; int BackShape` | Monsters/Companion.cs:168 | 4282 |
| CompanionItemsGained(1203) | `List<ClientUserItem> Items` | Monsters/Companion.cs:746 | 4305 |
| CompanionUpdate(1207) | `int Level; int Experience; int Hunger` | Commands/Admin/SetCompanionLevel.cs:25 | 4297 |
| CompanionSkillUpdate(1213) | `Stats Level3; Level5; Level7; Level10; Level11; Level13; Level15` | Commands/Admin/SetCompanionStat.cs:61 | 4328 |
| SendCompanionFilters(1418) | `List<MirClass> FilterClass; List<Rarity> FilterRarity; List<ItemType> FilterItemType` | PlayerObject.cs:3312 | 5042（空） |
| UserMilestones(1484) | `List<ClientUserMilestone> Milestones` | ★ SConnection.cs:1643 | 5071 |
| MilestoneEarned(1489) | `int Index` | Models/PlayerObject.Milestone.cs:117 | 5100 |
| DataObject* 7 个(1246-1323) | 见「DataObject* 家族」 | 见上 | 4393-4530 |

（DataObject* 家族已在协议细节一节单列，这里不重复。）

## GodotClient 现状

已到 `GodotClient/` 实测（glob 全部 134 个 .cs + grep `Process(S.`）：

| 功能 | 状态 | 证据 |
|---|---|---|
| S 包处理面 | **已移植（215/216）**：`GodotClient/Network/ServerConnection.cs:364-706` 为每个 S 包实现 `Process(S.X)` 并转成 C# 事件（事件声明 71-296）。唯一缺失 `GuildStats`——服务端也从不发（死包），无影响 |
| 握手 G 包 | **部分移植**：`Process(G.Connected/GoodVersion/Disconnect/Ping/PingResponse)` 齐全（364-380），但**没有 `Process(G.CheckVersion)/Process(G.Version)`**——若服务端 `Config.CheckVersion=true`（默认 true，`ServerLibrary/Envir/Config.cs:22`）握手会卡死；单机 ServerCore 需关闭版本检查 |
| StartGame 时序问题 | **已移植（自研方案）**：`BufferPendingPackets` + 17 个 `Pending*` 队列（289-362），GameScene 订阅事件前排空重放（`GameScene.cs:7503` 起的排空调用），切图 `ClearPendingWorldPackets`（319-329） |
| 对象出现/移除渲染 | **已移植**：`GameScene.cs:1136-1137` 订阅 `ObjectMonsterEvent/ObjectPlayerEvent`，`OnObjectPlayer`（2403）走 `ObjectRenderer`/`MapObjectNode` 建节点 |
| 动作/战斗包 | **已移植（网络层）**：`ObjectMove/ObjectTurn/ObjectAttack/ObjectMagic/ObjectProjectile/ObjectSpell` 等全部有处理与事件（432-652）；渲染侧由 `CombatController.cs`（559 行）、`DamagePopupNode`、`MirProjectileNode` 等消费，动画帧系统与原版 ActionQueue 等价性未逐帧核对（[INFERENCE] 部分移植） |
| UI 类包（背包/魔法/公会/邮件/商城…） | **部分移植**：ServerConnection 事件齐全，但消费方只有约 50 个 Controls/*.cs（如 `InventoryDialog/BuffDialog/TradeDialog`），覆盖面窄于原版 WinForms 的全部对话框，逐包核对属各 UI 文档范围 |
| 观战 | **部分移植**：`StartObserverEvent`（477 行）存在，未见对应观战 UI 消费（grep GodotClient 无 Observer 专属控件） |
| 死包 | `GuildStats/NPCAccessoryUpgrade/NPCRefinementStone`：Godot 侧反而实现了处理器（505/509 行），服务端不发，属无害预留 |

## 移植注意事项

1. **包 ID 绝不能手写**：ID 是 LibraryCore 反射排序索引（General 包最前），Godot 客户端必须与服务端用同一份 LibraryCore 编译产物，否则全部包错位。详见 connection-lifecycle.md。
2. **三层可见性必须照抄**：只移植 `S.Object*` 而漏掉 `S.DataObject*` 会导致组队血条、大地图玩家点、Boss 追踪全坏；`CanDataBeSeenBy` 的判定（同副本+组队/公会/配偶/Tracker）在 `MapObject.cs:1024-1053`。
3. **本人/他人双路径**：所有动作包对 `MapObject.User`（本人）做位置校验回拉 + `Slow` 叠加，对他人仅排队动画。移植到 Godot 时若统一处理会造成本人卡顿或位置漂移；`Observer` 模式下本人也走他人路径（`CConnection.cs:931` 的 `!GameScene.Game.Observer`）。
4. **HP/MP 是 200ms 批量 flush**：`ProcessHPMP`（`MapObject.cs:161-208`）节流，`HealthChanged.Change` 是差值不是绝对值；绝对值在 `DataObjectHealthMana`。别把两者混用。
5. **观察者复制靠 `ObserverPacket` 标志**：发私有效果（如 `MarketPlaceSearch`、`BlockAdd`）时必须设 `ObserverPacket = false`（`SConnection.cs:920/1398` 多处如此），否则观战者会收到不该看的包。
6. **`[CompleteObject]` 是客户端钩子**：`MagicLeveled/MagicCooldown/NPCResponse/DataObjectMonster/DataObjectItem/GuildConquestDate` 反序列化后立即执行 Complete 回查 `Globals.*InfoList`——Godot 侧必须先加载 System.db 数据表再处理这些包。
7. **字段是属性不是字段**：序列化只反射**属性**（`GetProperties`），`ObjectPlayer.FiltersClass` 等公开**字段**（无 `{ get; set; }`）**不会被序列化**（`Packet.cs:191`）。这是隐蔽坑：给包加字段必须写成属性，或接受其不在线缆上传输（FiltersClass 系列实际从未上线——客户端处理器也没用它）。
8. **死包清单**：服务端不发送 `GuildStats/MarketPlaceSearchCount/NPCAccessoryUpgrade/NPCRefinementStone/ObjectStats`（前三个客户端也不处理），移植时可安全跳过，但保留类定义以免打乱包 ID 排序。
9. **大包注意**：`S.StartGame`（含全部物品/魔法/任务）与 `S.Login`（账号仓库）是最大的两个包，`maximumPacketLength = 64MB`（`Packet.cs:134`）；BaseConnection 已修过 StartGame 截断 bug（`BaseConnection.cs:49-52` 注释），Godot 移植时要保留分块发送逻辑。

## 计数

- `ServerPackets.cs` 内 `public sealed class` 计数：**216**（本会话 grep 实测）；`GeneralPackets.cs` 双向通用包 **7** 个。
- 分组核对（正文详解与分组表双重出现的包只计一次，以分组表为准）：
  B 登录 9 + C 角色 12 + D 地图/环境 8 + E 对象同步 26 + F 战斗 19 + G 成长 12 + H 物品 19 + I NPC 15 + J 社交 31 + K 公会 26 + L 商城 12 + M 邮件 5 + N 任务/伴侣/里程碑 15 + DataObject* 7 = **216** ✓
- 客户端处理器：**213/216**（缺 `GuildStats`/`NPCAccessoryUpgrade`/`NPCRefinementStone`，恰好都是服务端也不发送的死包）。
- GodotClient 处理器：**215/216**（缺 `GuildStats`）。
- 服务端无发送点的包：**5**（`GuildStats`、`MarketPlaceSearchCount`、`NPCAccessoryUpgrade`、`NPCRefinementStone`、`ObjectStats`）。
