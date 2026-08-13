# 地图格与移动系统（.map 格式 / Cell 阻挡语义 / 坐标方向 / 走跑验证与节流 / 传送 / 安全区）

## TL;DR 速查表

- 地图 = `MapInfo`（System.db 元数据）+ `.map` 二进制文件（`Config.MapPath` 下）。服务端 `Map.Load()` 只解析宽高和每格 flag 字节，**仅 bit0=1 且 bit1=1 的格子才创建 `Cell`**（`ServerLibrary/Models/Map.cs:58-90`）；客户端解析全部 14 字节/格的渲染字段（`Client/Scenes/Views/MapControl.cs:484-547`）。
- **本仓库不存在 `CellFlag` 枚举**（`grep CellFlag` 全仓库仅命中文档计划）；"可行走/高台/水"等语义只剩两个原始位：`(flag & 0x01) == 1 && (flag & 0x02) == 2` = 可行走格，客户端 `Cell.Flag=true` 表示**阻挡**（`MapControl.cs:535`）。安全区不是格 flag，而是 `SafeZoneInfo` 区域在启动时绑到 `Cell.SafeZone`（`SEnvir.cs:1112`）。
- 坐标是网格坐标 `Point(X,Y)`，8 方向 `MirDirection`（`LibraryCore/Enum.cs:48-62`），位移统一走 `Functions.Move(location, direction, distance)`（`LibraryCore/Functions.cs:490-520`）。渲染像素比 48×32（`MapControl.cs:178`）。
- 移动包是 **`C.Move`（带 Distance 1/2/3）与 `C.Turn`**（`LibraryCore/Network/ClientPackets.cs:89-103`）——没有独立的 C.Walk/C.Run 包，走/跑共用 `C.Move`，Distance 区分。
- 服务端节流公式：`ActionTime = SEnvir.Now + Globals.MoveTime`（600ms）、`TurnTime`（300ms）；Slow 毒每点 +100ms（`PlayerObject.cs:13953-13964, 14665-14685`）。非法/过早移动一律回 `S.UserLocation` 纠正。
- 逐格验证：`Cell.IsBlocking`（对象阻挡 + CellTime 幽灵格）+ `Cell.GetMovement`（踩传送点 `MovementInfo`，最多 5 次随机尝试，可跳图/进副本）（`Map.cs:527-545, 547-733`）。
- 回城卷 = 消耗品 `case 2: //Town Teleport`（`PlayerObject.cs:6454-6479`），受 `MapInfo.AllowTT` 与 `InstanceInfo.AllowTeleport` 双重限制；死亡回城 `TownRevive()` 回 `Character.BindPoint`（`PlayerObject.cs:1443-1464`）。
- 安全区：`SafeZoneInfo.Region` 绑格 + `BindRegion` 决定回城点；效果 = 禁 PvP/宠物攻击、禁掉耐久、仓库/邮件/寄售限定、`S.SafeZoneChanged` 通知。
- Godot 客户端**已完整移植** .map 解析（`GodotClient/Formats/MapReader.cs`）、地图渲染（`MapView.cs`）、鼠标走/跑（`MouseWalker.cs`）与移动收包校正；安全区状态、大/小地图传送点图标也已接通。

## 职责概述

本文覆盖"一块地图如何被表示、玩家如何在其上移动与被验证、如何从 A 点到 B 点（走/跑/传送点/回城/戒指传送）以及安全区的判定与效果"。涉及：服务端 `Map`/`Cell` 运行时结构（`ServerLibrary/Models/Map.cs`）、静态数据 `MapInfo`/`MapRegion`/`MovementInfo`/`SafeZoneInfo`（`LibraryCore/SystemModels/`）、`.map` 二进制格式的双侧解析、移动协议（`C.Turn`/`C.Move`/`S.ObjectTurn`/`S.ObjectMove`/`S.UserLocation`）、`PlayerObject` 的移动/转身处理与反作弊节流、传送全家桶（区域传送点、回城卷、随机传送卷、死亡回城、夫妻召回、TeleportRing、NPC 对话传送）、安全区绑定与规则、以及旧 WinForms 客户端的渲染/输入侧对照。副本（Instance）的创建与生命周期另见 `map/instances.md`。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| `ServerLibrary/Models/Map.cs` | 16-372 | 运行时 `Map`：Cells 网格、ValidCells、对象列表（Players/Bosses/NPCs…）、GetCell/GetCells/GetRandomLocation、Broadcast |
| `ServerLibrary/Models/Map.cs` | 58-90 | `Load()` 服务端 `.map` 解析（只读宽高 + flag 字节，双 bit 判定有效格） |
| `ServerLibrary/Models/Map.cs` | 475-734 | `Cell`：格级对象列表、SafeZone/Movements/Regions 挂载、`IsBlocking`、`GetMovement`（踩点触发链） |
| `ServerLibrary/Models/PlayerObject.cs` | 13926-13967 | `Turn()` 转身处理（节流 + 广播 S.ObjectTurn） |
| `ServerLibrary/Models/PlayerObject.cs` | 14577-14697 | `Move()` 走/跑处理（距离/逐格阻挡/安全区绑定/S.ObjectMove 广播） |
| `ServerLibrary/Models/PlayerObject.cs` | 1443-1464 | `TownRevive()` 死亡回城 |
| `ServerLibrary/Models/PlayerObject.cs` | 2859-2909 | `Teleport()` 覆写 + `TeleportRing()`（GM/特殊戒指传送） |
| `ServerLibrary/Models/PlayerObject.cs` | 6454-6490 | 回城卷（case 2）/随机传送卷（case 3）物品效果 |
| `ServerLibrary/Models/MapObject.cs` | 86-88 | `CanMove`/`CanAttack`/`CanCast` 总闸（死亡/时序/麻痹/恐惧等） |
| `ServerLibrary/Models/MapObject.cs` | 916-963 | `Teleport()` 三个重载（任意传送的公共落点逻辑） |
| `ServerLibrary/Models/NPCObject.cs` | 62-108 | NPC 脚本 `NPCActionType.Teleport`（普通图/进副本两分支） |
| `ServerLibrary/Envir/SConnection.cs` | 411-440 | `Process(C.Turn)`/`Process(C.Move)` 入口（方向枚举合法性校验） |
| `ServerLibrary/Envir/SEnvir.cs` | 776-892 | `CreateMovements()`：把 `MovementInfo` 绑到源 Cell.Movements |
| `ServerLibrary/Envir/SEnvir.cs` | 1043-1181 | `CreateSafeZones()`：SafeZone 绑格、Border 光效、ValidBindPoints |
| `ServerLibrary/Envir/Config.cs` | 25, 33, 96, 118 | MapPath、LazyLoadMaps、MaxViewRange=18、AutoReviveDelay=10min |
| `LibraryCore/SystemModels/MapInfo.cs` | 7-557 | 地图元数据（FileName/Light/Weather/AllowRT/AllowTT/CanMine/等级限制/ReconnectMap/Stats…） |
| `LibraryCore/SystemModels/MapRegion.cs` | 9-220 | 区域（BitArray 或点列）、RegionType、GetPoints/CreatePoints/边缘点 |
| `LibraryCore/SystemModels/MovementInfo.cs` | 5-176 | 传送点（Source/Destination 区域 + NeedItem/NeedSpawn/NeedHole/NeedInstance/Effect） |
| `LibraryCore/SystemModels/SafeZoneInfo.cs` | 7-93 | 安全区（Region/BindRegion/StartClass/RedZone/Border/ValidBindPoints） |
| `LibraryCore/Globals.cs` | 308-313 | TurnTime=300ms / HarvestTime=600ms / MoveTime=600ms / AttackTime / CastTime |
| `LibraryCore/Functions.cs` | 429-520 | InRange / ShiftDirection / Move(Point,MirDirection,distance) |
| `LibraryCore/Enum.cs` | 48-62, 343-397 | MirDirection / LightSetting / FightSetting / RegionType |
| `LibraryCore/Network/ClientPackets.cs` | 89-103 | `C.Turn { Direction }`、`C.Move { Direction, Distance }` |
| `LibraryCore/Network/ServerPackets.cs` | 91-94, 105-111, 149-157, 787-790 | S.MapChanged / S.ObjectTurn / S.ObjectMove / S.SafeZoneChanged |
| `Client/Scenes/Views/MapControl.cs` | 484-547 | 旧客户端 `.map` 完整解析（14 字节/格） |
| `Client/Scenes/Views/MapControl.cs` | 1247-1290, 1373-1378 | `CanMove(dir,distance)` / `MouseDirection()`（22.5° 八分） / `ValidCell` |
| `Client/Scenes/Views/MapControl.cs` | 860-1169 | `ProcessInput()` 走（左键）/跑（右键）输入分派、`Run()` 步数计算 |
| `Client/Scenes/Views/MapControl.cs` | 1865-1937 | 客户端 `Cell` 渲染结构（Flag/动画位标记/Light）+ `Blocking()` |
| `Client/Models/UserObject.cs` | 401-451, 614-709 | 客户端动作闸（ServerTime 单飞包门控、NextActionTime 帧时长节流） |
| `Client/Envir/CConnection.cs` | 814-817, 929-949, 1001-1024 | S.UserLocation→Displacement、S.ObjectTurn/S.ObjectMove 动作入队 |
| `docs/MAP_FORMAT_COMPARISON.md` | 全文 | .map 逐字节布局（与 NAS 版对比） |

## 核心流程

### 1. `.map` 文件格式与「CellFlag」真实语义

`.map` 布局（服务端 `Map.cs:58-90` 与客户端 `MapControl.cs:484-547` 一致，详见 `docs/MAP_FORMAT_COMPARISON.md:23-44`）：

```
偏移 0-21:  22 字节头（跳过）
偏移 22-23: Width  (Int16 小端；服务端写法 fileBytes[23]<<8 | fileBytes[22])
偏移 24-25: Height (Int16 小端)
偏移 26-27: 跳过
偏移 28+:   背景层 Width/2 × Height/2 × 3 字节（1 byte backFile + 2 bytes backImage）
            全分辨率层 Width × Height × 每格 14 字节
```

每格 14 字节（`MapControl.cs:513-536`）：

```
byte 0:  flag —— 阻挡标志（见下）
byte 1:  middleAnimationFrame
byte 2:  frontAnimationFrame（255→0，再 &= 0x8F）
byte 3:  frontFile（图库索引，经 Libraries.KROrder 映射）
byte 4:  middleFile
bytes 5-6: middleImage (+1)
bytes 7-8: frontImage (+1)
bytes 9-11: 跳过
byte 12: light（低 4 位 ×2）
byte 13: 跳过
```

**flag 字节的实际语义**（这是任务里"CellFlag 全枚举表"的真身——本仓库没有枚举，只有两个位的裸检查）：

| 位/值 | 服务端判定（`Map.cs:80-84`） | 客户端判定（`MapControl.cs:535`） | 语义 |
|---|---|---|---|
| `flag & 0x01 == 1` 且 `flag & 0x02 == 2` | 创建 `Cell` 并加入 `ValidCells` | `Cell.Flag = false` → `ValidCell()` 返回 true | **可行走格** |
| 其它任何组合 | **格子不存在**（`Cells[x,y]` 保持 null，`GetCell` 返回 null → 天然不可走、不可站、不可刷怪） | `Cell.Flag = true` → 阻挡 | 不可行走格（墙体/悬崖等） |

服务端原文：

```csharp
byte flag = fileBytes[offSet + (x * Height + y) * 14];

if ((flag & 0x02) != 2 || (flag & 0x01) != 1) continue;

ValidCells.Add(Cells[x, y] = new Cell(new Point(x, y)) { Map = this });
```
（`ServerLibrary/Models/Map.cs:80-84`；`offSet = 28 + Width * Height / 4 * 3`，`Map.cs:75`）

客户端原文：

```csharp
Cells[x, y].Flag = ((flag & 0x01) != 1) || ((flag & 0x02) != 2);
```
（`Client/Scenes/Views/MapControl.cs:535`；`Flag == true` = 阻挡）

要点：
- 经典 Mir3 `.map` 格式里 flag 的其余位（高台/水面等）在**本仓库双侧代码里都没有任何读取**——"高台/水"不参与任何行走判定，渲染高低差完全由贴图层（Middle/Front 图库）表现。未找到实现，若新客户端需要高低台语义需自行扩展解析。
- 采矿是唯一把 `Flag == true` 当"正面特性"用的地方：挖矿目标格必须 `Cells[x,y].Flag == true`（即墙面/矿石格，`MapControl.cs:990, 1051`）。
- 客户端动画帧字节的位编码：`FrontAnimationFrame & 0x0F` = 帧数、`& 0x80` = 混合标志（`MapControl.cs:1878-1896`）。
- 安全区、传送点、任务区域都不是格 flag，而是启动时由 `SEnvir.CreateSafeZones/CreateMovements/CreateQuestRegions` 把 `SafeZoneInfo`/`MovementInfo`/`QuestTask` 挂到对应 `Cell` 上（`Map.cs:484-490`）。

### 2. 服务端地图加载链

`SEnvir.GetMap(info)`（`SEnvir.cs:4240-4286`）懒加载：新建 `Map(info)` → `FinaliseMapLoad`（`SEnvir.cs:4216-4238`）→ `map.Load()`（读文件）→ `CreateSafeZones/CreateMovements/CreateNPCs/CreateSpawns/CreateQuestRegions`（均带 `instance/instanceSequence/targetMap` 参数，副本复用同一套）。`Map.Setup()` 再生成守卫/城堡旗/城门/守卫与 `CreateCellRegions()`（只挂 `RegionType.Area` 的区域，`Map.cs:186-207`）。

### 3. 坐标系统与方向

- 网格坐标：`System.Drawing.Point`，X 向右、Y 向下，服务端与客户端同构；越界即 `GetCell` 返回 null。
- 方向：`MirDirection : byte`，顺时针从上开始（`LibraryCore/Enum.cs:48-62`）：`Up=0, UpRight=1, Right=2, DownRight=3, Down=4, DownLeft=5, Left=6, UpLeft=7`。`Functions.ShiftDirection(dir, i) = (MirDirection)(((int)dir + i + 8) % 8)`（`Functions.cs:486-489`）。
- 位移公式（走 1 格 = X/Y 各偏 1；对角走 X、Y 同时偏）：

```csharp
public static Point Move(Point location, MirDirection direction, int distance = 1)
{
    switch (direction)
    {
        case MirDirection.Up:       location.Offset(0, -distance);  break;
        case MirDirection.UpRight:  location.Offset(distance, -distance); break;
        case MirDirection.Right:    location.Offset(distance, 0);  break;
        case MirDirection.DownRight:location.Offset(distance, distance);  break;
        case MirDirection.Down:     location.Offset(0, distance);  break;
        case MirDirection.DownLeft: location.Offset(-distance, distance); break;
        case MirDirection.Left:     location.Offset(-distance, 0); break;
        case MirDirection.UpLeft:   location.Offset(-distance, -distance); break;
    }
    return location;
}
```
（`LibraryCore/Functions.cs:490-520`）

- 距离判定：`Functions.InRange = |dx| <= i && |dy| <= i`（切比雪夫，`Functions.cs:429-436`）；广播视野用 `Config.MaxViewRange = 18`（`Config.cs:96`、`Map.cs:359-366`）。
- 客户端像素映射：逻辑格 48×32（`MapControl.cs:178`），玩家恒居屏幕中心，偏移：

```csharp
OffSetX = Size.Width / 2 / CellWidth;
OffSetY = Size.Height / 2 / CellHeight;
PixelOffsetX = (Size.Width - CellWidth) / 2 - OffSetX * CellWidth;
PixelOffsetY = (Size.Height - CellHeight) / 2 - OffSetY * CellHeight - ManualHeightOffset;
```
（`MapControl.cs:145-148`，`ManualHeightOffset = 34`）

鼠标 → 格坐标（`UpdateMapLocation`，`MapControl.cs:1329-1335`）：

```csharp
MapLocation = new Point((MouseLocation.X - GameScene.Game.Location.X - PixelOffsetX) / CellWidth - OffSetX + User.CurrentLocation.X,
                        (MouseLocation.Y - GameScene.Game.Location.Y - PixelOffsetY) / CellHeight - OffSetY + User.CurrentLocation.Y);
```

- 鼠标 → 方向（`MouseDirection()`，`MapControl.cs:1261-1290`）：距离 ≤2 格时直接按格差取 8 方向；否则用余弦定理算角度，`angle += 22.5F` 后 `(MirDirection)(angle / 45F)` 得 22.5° 边界的八分方向。

### 4. 移动验证：C.Turn / C.Move → 服务端真实处理

协议（没有独立的 Walk/Run 包，走跑共用 `C.Move`）：

```csharp
public sealed class Turn : Packet
{
    public MirDirection Direction { get; set; }
}
public sealed class Move : Packet
{
    public MirDirection Direction { get; set; }
    public int Distance { get; set; }
}
```
（`LibraryCore/Network/ClientPackets.cs:89-103`）

`SConnection` 入口先做枚举范围校验（防伪造 byte 越界值）：

```csharp
public void Process(C.Turn p)
{
    if (Stage != GameStage.Game) return;
    if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;
    Player.Turn(p.Direction);
}
```
（`ServerLibrary/Envir/SConnection.cs:411-418`；`C.Move` 同款校验在 `SConnection.cs:427-440`。注意其中被注释掉的负重禁止跑步检查——现为死代码。）

**`PlayerObject.Turn`（转身，`PlayerObject.cs:13926-13967`）**——节流 + 延迟队列是核心：

```csharp
public void Turn(MirDirection direction)
{
    if (SEnvir.Now < ActionTime || SEnvir.Now < MoveTime)
    {
        if (!PacketWaiting)
        {
            ActionList.Add(new DelayedAction(ActionTime, ActionType.Turn, direction));
            PacketWaiting = true;
        }
        else
            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });

        return;
    }
    ...
    Direction = direction;

    ActionTime = SEnvir.Now + Globals.TurnTime;              // 300ms

    if ((Poison & PoisonType.Neutralize) == PoisonType.Neutralize)
        ActionTime += Globals.TurnTime;                      // 中和毒：翻倍

    Poison poison = PoisonList.FirstOrDefault(x => x.Type == PoisonType.Slow);
    TimeSpan slow = TimeSpan.Zero;
    if (poison != null)
    {
        slow = TimeSpan.FromMilliseconds(poison.Value * 100); // Slow 毒：每点 +100ms
        ActionTime += slow;
    }

    Broadcast(new S.ObjectTurn { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Slow = slow });
}
```

**`PlayerObject.Move`（走/跑，`PlayerObject.cs:14577-14697`）**：

```csharp
public void Move(MirDirection direction, int distance)
{
    if (SEnvir.Now < ActionTime || SEnvir.Now < MoveTime)          // ① 时序闸：过早包 → 延迟队列或纠正
    { ... 同 Turn 的 PacketWaiting 逻辑 ... }

    if (!CanMove)                                                   // ② 总闸（死亡/麻痹/束缚/恐惧…）
    { Enqueue(new S.UserLocation { ... }); return; }

    if (distance <= 0 || distance > 3)                              // ③ 距离硬上限 3
    { Enqueue(new S.UserLocation { ... }); return; }

    if (distance == 3 && Horse == HorseType.None)                   // ④ 三格=骑马专属
    { Enqueue(new S.UserLocation { ... }); return; }

    Cell cell = null;
    SafeZoneInfo traversedSafeZone = null;

    for (int i = 1; i <= distance; i++)                             // ⑤ 逐格验证（穿过也算）
    {
        cell = CurrentMap.GetCell(Functions.Move(CurrentLocation, direction, i));
        if (cell == null)          { Enqueue(new S.UserLocation { ... }); return; }
        if (cell.IsBlocking(this, true)) { Enqueue(new S.UserLocation { ... }); return; }

        if (CanBindToSafeZone(cell.SafeZone))
            traversedSafeZone = cell.SafeZone;                      // 路过安全区也会改绑回城点
    }

    BuffRemove(BuffType.Invisibility);
    BuffRemove(BuffType.Transparency);

    if (distance > 1)                                               // 跑步额外代价
    {
        if (Stats[Stat.Comfort] < 12)
            RegenTime = SEnvir.Now + RegenDelay;                    // 舒适度<12 推迟回血
        if (!GetMagic(MagicType.Stealth, out Stealth stealth) || !stealth.CheckCloak())
            BuffRemove(BuffType.Cloak);                              // 跑步破隐身斗篷
    }
    ...
    Direction = direction;

    ActionTime = SEnvir.Now + Globals.MoveTime;                     // ⑥ 600ms 节流（走跑同速计时）
    MoveTime = SEnvir.Now + Globals.MoveTime;

    Map previousMap = CurrentMap;

    PreventSpellCheck = true;
    CurrentCell = cell.GetMovement(this);                           // ⑦ 落点可能被传送点改写
    PreventSpellCheck = false;

    UpdateBindPoint(traversedSafeZone);
    RemoveAllObjects();
    AddAllObjects();

    Poison poison = PoisonList.FirstOrDefault(x => x.Type == PoisonType.Slow);
    TimeSpan slow = TimeSpan.Zero;
    if (poison != null)
    {
        slow = TimeSpan.FromMilliseconds(poison.Value * 100);
        ActionTime += slow;
    }

    Broadcast(new S.ObjectMove                                       // ⑧ 广播（同 Map 玩家）
    {
        ObjectID = ObjectID, Direction = direction, Location = CurrentLocation,
        Slow = slow, Distance = distance, MapChanged = previousMap != CurrentMap,
    });
    CheckSpellObjects();
}
```

时间常量（`LibraryCore/Globals.cs:308-313`）：

```csharp
public static TimeSpan TurnTime = TimeSpan.FromMilliseconds(300),
                       HarvestTime = TimeSpan.FromMilliseconds(600),
                       MoveTime = TimeSpan.FromMilliseconds(600),
                       AttackTime = TimeSpan.FromMilliseconds(600),
                       CastTime = TimeSpan.FromMilliseconds(600),
                       MagicDelay = TimeSpan.FromMilliseconds(2000);
```

`CanMove` 总闸（`MapObject.cs:86`）——死亡、时序、惊吓、麻痹/摄魂/禁锢/束缚毒、龙推 buff 全部拦截：

```csharp
public virtual bool CanMove => !Dead && SEnvir.Now >= ActionTime && SEnvir.Now >= MoveTime && SEnvir.Now > ShockTime
    && (Poison & PoisonType.Paralysis) != PoisonType.Paralysis
    && (Poison & PoisonType.WraithGrip) != PoisonType.WraithGrip
    && (Poison & PoisonType.Containment) != PoisonType.Containment
    && (Poison & PoisonType.Binding) != PoisonType.Binding
    && Buffs.All(x => x.Type != BuffType.DragonRepulse);
```

格阻挡（`Map.cs:527-545`）：

```csharp
public bool IsBlocking(MapObject checker, bool cellTime)
{
    if (Objects == null) return false;

    foreach (MapObject ob in Objects)
    {
        if (!ob.Blocking) continue;
        if (cellTime && SEnvir.Now < ob.CellTime) continue;   // 幽灵格：对象刚离开的格子短暂可穿
        if (ob.Stats == null) return true;
        if (ob.Buffs.Any(x => x.Type == BuffType.Cloak || x.Type == BuffType.Transparency)
            && ob.Level > checker.Level && !ob.InGroup(checker)) continue;  // 高等级隐身者不挡路
        return true;
    }
    return false;
}
```

**客户端侧节流（与服务器镜像）**：
- `UserObject.AttemptAction`：`if (CEnvir.Now < NextActionTime || ActionQueue.Count > 0) return; if (CEnvir.Now < ServerTime) return;`（`UserObject.cs:447-450`）。发任何动作后 `ServerTime = CEnvir.Now.AddSeconds(5)`（`UserObject.cs:708`）——**一次只允许一个在途包**，收到 `S.ObjectMove`/`S.ObjectTurn`/`S.UserLocation` 即清零（`CConnection.cs:936, 1011`）。
- `MirAction.Moving` 分支：`MoveTime = CEnvir.Now + Globals.MoveTime;` 后发 `C.Move { Direction, Distance = MoveDistance }`（`UserObject.cs:630-634`）；转身发 `C.Turn`（`UserObject.cs:619-621`）。
- 客户端 `CanMove(dir, distance)`：逐格 `Cells[x,y].Blocking()`（= 地形 Flag 或格上有 Blocking 对象，`MapControl.cs:1247-1259, 1904-1913`）。
- 跑步步数（客户端决定 Distance，服务端只验上限）：`Run()` 中 `steps=1`，`CanRun && 过 NextRunTime && 负重 OK` 时 +1，骑马再 +1（`MapControl.cs:1131-1140`）。
- `S.ObjectMove` 回包：本人 → `Displacement` 校正 + 清 ServerTime + `NextActionTime += p.Slow`；他人 → `ActionQueue.Add(Moving)` 播动画（`CConnection.cs:1001-1024`）。移动插值公式见 `Client/Models/MapObject.cs:630-701`（按帧数或毫秒把 `CellWidth * MoveDistance` 分摊）。
- `S.UserLocation` 是**拒绝/纠正**信号（`CConnection.cs:814-817` → `GameScene.Displacement`）。

### 5. 传送体系

**(a) 区域传送点 `MovementInfo`**（踩格触发，`Cell.GetMovement`，`Map.cs:592-733`）：

```csharp
for (int i = 0; i < 5; i++) //20 Attempts to get movement;   ← 注释写 20，代码实为 5 次
{
    MovementInfo movement = Movements[SEnvir.Random.Next(Movements.Count)];   // 同格多点随机选

    Map map = SEnvir.GetMap(movement.DestinationRegion.Map, Map.Instance, Map.InstanceSequence);

    if (movement.NeedInstance != null)          // 进/出副本传送点
    {
        if (ob.Race != ObjectType.Player) break;
        if (Map.Instance != null)               // 副本 → 主世界
            map = SEnvir.GetMap(movement.DestinationRegion.Map, null, 0);
        else                                    // 主世界 → 副本（走 GetInstance 分配槽位）
        {
            var (index, result) = ((PlayerObject)ob).GetInstance(movement.NeedInstance, walkOn: true);
            if (result != InstanceResult.Success) { ((PlayerObject)ob).SendInstanceMessage(...); break; }
            map = SEnvir.GetMap(movement.DestinationRegion.Map, movement.NeedInstance, index.Value);
        }
    }
    ...
    Point destination = movement.DestinationRegion.PointList[SEnvir.Random.Next(...)];  // 目标区域内随机落点
    Cell cell = map.GetCell(destination);
    if (cell == null) continue;
    // 玩家限定检查：目标图 MinimumLevel / MaximumLevel / RequiredClass（TempAdmin 豁免）
    // NeedSpawn：目标刷怪点必须 AliveCount > 0（"门后没怪不开"）
    // NeedHole：当前格必须有 SpellEffect.ZombieHole（尸洞技能开门）
    // NeedItem：需要并消耗 movement.NeedItem ×1
    // Effect == MovementEffect.SpecialRepair：全装备特修
    return cell.GetMovement(ob);                // 递归：落点又是传送点则继续跳
}
return this;
```

条件字段定义在 `LibraryCore/SystemModels/MovementInfo.cs:41-162`（Icon/NeedItem/NeedSpawn/NeedHole/NeedInstance/Effect/RequiredClass/SkipValidation——SkipValidation 注释：允许源格为无效格，用于"NPC 对话式小地图连接"）。绑定过程 `SEnvir.CreateMovements`（`SEnvir.cs:776-892`）：把每条 `MovementInfo` 加进源区域内**每个点**的 `Cell.Movements`。

**(b) 通用 `MapObject.Teleport`**（`MapObject.cs:925-963`）：

```csharp
public virtual bool Teleport(Map map, Point location, bool leaveEffect = true, bool enterEffect = true)
{
    if (Race == ObjectType.Player && map.Info.MinimumLevel > Level && !((PlayerObject)this).Character.Account.TempAdmin) return false;
    if (Race == ObjectType.Player && map.Info.MaximumLevel > 0 && map.Info.MaximumLevel < Level && !((PlayerObject)this).Character.Account.TempAdmin) return false;

    Cell cell = map?.GetCell(location);
    if (cell == null || cell.Movements != null) return false;   // ← 目标格带传送点则拒绝（防死循环）

    if (leaveEffect) Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = Effect.TeleportOut });
    BuffRemove(BuffType.Dash);
    CurrentCell = cell.GetMovement(this);                       // 仍要走一次传送点链
    RemoveAllObjects();
    AddAllObjects();
    Broadcast(new S.ObjectTurn { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
    if (enterEffect) Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = Effect.TeleportIn });
    return true;
}
```

玩家覆写（`PlayerObject.cs:2861-2877`）：成功后取消自动寻路、移除 Cloak/Transparency、召回同伴。

**(c) 回城卷 / 随机传送卷**（消耗品效果，`PlayerObject.cs:6454-6490`）：

```csharp
case 2: //Town Teleport
    if (CurrentMap.Instance != null && !CurrentMap.Instance.AllowTeleport) { ...CannotTownTeleport...; return; }
    if (!CurrentMap.Info.AllowTT) { ...CannotTownTeleport...; return; }
    var bindMap = SEnvir.GetMap(Character.BindPoint.BindRegion.Map);
    var tpPoint = Character.BindPoint.ValidBindPoints[SEnvir.Random.Next(Character.BindPoint.ValidBindPoints.Count)];
    bool tpRes = Teleport(bindMap, tpPoint);
    ...
case 3: //Random Teleport
    if (!CurrentMap.Info.AllowRT) { ...CannotRandomTeleport...; return; }
    if (!Teleport(CurrentMap, CurrentMap.GetRandomLocation())) return;
```

**(d) 死亡回城**：`Die()` 设 `RevivalTime = SEnvir.Now + Config.AutoReviveDelay`（默认 10 分钟，`PlayerObject.cs:16208-16212`、`Config.cs:118`）；玩家处理循环里 `if (Dead && SEnvir.Now >= RevivalTime) TownRevive();`（`PlayerObject.cs:340-341`）；客户端也可主动发 `C.TownRevive`。`TownRevive()`（`PlayerObject.cs:1443-1464`）回 `Character.BindPoint` 的随机 `ValidBindPoints` 并满状态复活广播 `S.ObjectRevive`。掉线重连时若不在安全区且当前图有 `ReconnectMap`，直接传回重连图（`PlayerObject.cs:1129-1132`）。

**(e) 夫妻召回 `MarriageTeleport`**（`PlayerObject.cs:3094-3163`）：目标图 `CanMarriageRecall == false` 则拒绝（`PlayerObject.cs:3137-3140`），成功后 `MarriageTeleportTime = Now + 120s` 冷却。

**(f) TeleportRing**（GM/特殊道具，`PlayerObject.cs:2879-2909`）：需 `Stat.TeleportRing` 或 Admin；当前图/目标图必须 `AllowRT && AllowTT`、副本需 `AllowTeleport`；1 秒防抖 + 成功后 5 分钟冷却；落点在目标点 10~25 格内随机。

**(g) NPC 对话传送**（`NPCObject.cs:62-108`）：NPC 脚本 Action `NPCActionType.Teleport`——带 `InstanceParameter1` 时走 `ob.GetInstance()` 进副本并传 `ConnectRegion`（副本内则拒绝跨副本移动）；否则传 `MapParameter1`（保留当前 instance/sequence，防止跨副本），`IntParameter1/2` 为 0 时随机落点，否则定点。

### 6. 安全区（SafeZoneInfo）

数据模型（`LibraryCore/SystemModels/SafeZoneInfo.cs:7-93`）：`Region`（安全区本体区域）、`BindRegion`（回城绑定区域）、`StartClass`（新人出生地职业筛选）、`RedZone`（红名村）、`Border`（是否给边缘画 `SpellEffect.SafeZone` 光效）、`ValidBindPoints`（运行时缓存的有效回城点）。

绑定（`SEnvir.CreateSafeZones`，`SEnvir.cs:1043-1149`）：

```csharp
map.HasSafeZone = true;
...
cell.SafeZone = info;                          // 每个区域点绑 SafeZone

if (info.Border)                               // 边缘 8 邻域扩一圈光效
{
    for (int i = 0; i < 8; i++)
    {
        Point test = Functions.Move(point, (MirDirection)i);
        if (info.Region.PointList.Contains(test)) continue;
        if (map.GetCell(test) == null) continue;
        edges.Add(test);
    }
}
...
foreach (Point point in edges)
{
    SpellObject ob = new SpellObject { Visible = true, DisplayLocation = point,
        TickCount = 10, TickFrequency = TimeSpan.FromDays(365), Effect = SpellEffect.SafeZone };
    ob.Spawn(map, point);
}
```
`EnsureSafeZoneBindPoints`（`SEnvir.cs:1151-1181`）把 `BindRegion` 的有效格收集为 `ValidBindPoints`；`CreateStartZones`（`SEnvir.cs:1183-1191`）把 `StartClass != None || RedZone` 的安全区图强制加载。

判定与回城点更新（`PlayerObject.OnLocationChanged`，`PlayerObject.cs:1513-1523`）：

```csharp
UpdateBindPoint(CurrentCell.SafeZone);

if (InSafeZone != (CurrentCell.SafeZone != null))
{
    InSafeZone = CurrentCell.SafeZone != null;
    if (!Spawned) return;
    Enqueue(new S.SafeZoneChanged { InSafeZone = InSafeZone });
    PauseBuffs();
}
```

绑定条件（`PlayerObject.cs:14699-14705`）——红名不可绑、无有效绑定点不可绑：

```csharp
private bool CanBindToSafeZone(SafeZoneInfo safeZone)
{
    return safeZone != null &&
           safeZone.ValidBindPoints.Count > 0 &&
           Stats[Stat.PKPoint] < Config.RedPoint &&
           safeZone.StartClass != RequiredClass.None;
}
```

效果清单（服务端全部 `InSafeZone` 判定）：

| 效果 | 位置 |
|---|---|
| 禁 PvP：`if (InSafeZone || player.InSafeZone) return false;`（对玩家与宠物） | `PlayerObject.cs:15974, 16000` |
| 宠物不主动攻击安全区内目标 | `MonsterObject.cs:1336, 1389, 1524, 1632`；`Tornado.cs:109`；`JinamStoneGate.cs:87`；`NetherworldGate.cs:86` |
| 装备不掉耐久 | `PlayerObject.cs:584` |
| 火把不衰减 | `PlayerObject.cs:440` |
| 仓库/公会仓/零件仓存取限定安全区 | `PlayerObject.cs:7463-7475, 7491-7493` |
| 邮件发送/取件、寄售限定安全区 | `PlayerObject.cs:3707-3708, 3808, 3824, 3944, 3960` |
| 背包满时物品改邮寄（不在安全区才邮） | `PlayerObject.cs:4101, 4265` |
| 观察者模式仅可在安全区开启 | `PlayerObject.cs:1296` |
| 伴随兽在安全区不掉饥饿 | `MapObject.cs:438` |

### 7. 旧客户端渲染与输入侧（简述）

- `MapControl.LoadMap()`（`MapControl.cs:484-547`）读 `.map` 填 `Cell[,]`（渲染字段全量）；`OnMapInfoChanged`（`MapControl.cs:60-77`）换图时重载 + 切 BGM（`DXSoundManager`）+ `UpdateWeather()`（按 `MapInfo.Weather` 挂 Rain/Snow/Fog/Lightning 粒子，`MapControl.cs:549-581`）+ `LLayer.UpdateLights()`。
- 层级：`FLayer`（Floor 地形）+ `LLayer`（Light 光照层，按 `LightSetting` 与昼夜）+ 对象按 `RenderY` 行序绘制（`DrawObjects`，`MapControl.cs:342-482`）。
- 输入：`ProcessInput()`（`MapControl.cs:860-1043`）左键=走（`Moving, distance=1`，面前阻挡用 `MouseDirectionBest` 绕路，绕不动就原地转身 `Standing`）；右键=跑（`Run(direction)`，距玩家 ≤2 格只转身不跑，`MapControl.cs:1033-1040`）；`GameScene.MoveFrame` 与 `PoisonType.WraithGrip` 拦截移动（`MapControl.cs:1011, 1031`）。挖矿（`MapControl.cs:986-995, 1046-1067`）要求鹤嘴锄 + 目标格 `Flag==true`。

## 数据结构/协议细节

**`MapInfo` 关键字段**（`LibraryCore/SystemModels/MapInfo.cs`，行号为字段起始行）：`FileName:10`（.map 文件名）、`Description:25`、`MiniMap:48`、`Light:63`（LightSetting）、`Weather:78`、`Fight:93`（FightSetting None/Safe/Fight）、`AllowRT:108`（允许随机传送卷）、`SkillDelay:123`、`CanHorse:138`、`CanAutoPath:153`、`AllowTT:168`（允许回城卷）、`CanMine:183`、`CanMarriageRecall:198`、`AllowRecall:213`、`MinimumLevel:228`/`MaximumLevel:243`（传送与进图等级门）、`ReconnectMap:258`、`Music:273`、`Background:288`、`Instance:458`、`DungeonMap:476`/`Dungeon:493`、`RequiredClass:495`、`Guards:509`、`Regions:513`、`Mining:516`、`Castles:520`、`BuffStats:523`+`Stats:525`（`MapInfoStat` 聚合，进图时以 `BuffType.MapEffect` 生效，`PlayerObject.cs:9286-9289`）。`MonsterHealth/DropRate/...MaxGoldRate`（`MapInfo.cs:303-454`）标注 `//DO NOT USE`，是废弃字段。默认值：`AllowRT/AllowTT/CanMarriageRecall/AllowRecall = true`（`MapInfo.cs:527-535`）。

**`MapRegion`**（`MapRegion.cs`）：`BitRegion:74`（BitArray 位图，索引 `i → (i % width, i / width)`）或 `PointRegion:89`（点列，来源区域用它）；`RegionType:108`（`Enum.cs:387-397`：None/Area/Connection/Spawn/Npc/SpawnConnection/Path）；`Size:123`；`GetPoints/CreatePoints:144-186` 展开为 `PointList`，`CreateEdgePoints:187-214` 求边缘（8 邻域缺失即边）。

**协议汇总**：

| 包 | 字段 | 触发/含义 |
|---|---|---|
| `C.Turn`（`ClientPackets.cs:89`） | Direction | 客户端转身 |
| `C.Move`（`ClientPackets.cs:99`） | Direction, Distance(1-3) | 走/跑/骑马跑 |
| `C.TownRevive`（`ClientPackets.cs:87`） | — | 死亡主动回城 |
| `C.TeleportRing`（经 `SendTeleportRing`） | Location, Index | GM 大地图传送 |
| `S.ObjectTurn`（`ServerPackets.cs:105`） | ObjectID, Direction, Location, Slow | 转身广播 |
| `S.ObjectMove`（`ServerPackets.cs:149`） | ObjectID, Direction, Location, Distance, Slow, MapChanged | 移动广播（MapChanged=踩传送点跨图） |
| `S.UserLocation`（`ServerPackets.cs:96-100`） | Direction, Location | **纠正包**：非法/过早动作的权威位置 |
| `S.MapChanged`（`ServerPackets.cs:91-94`） | MapIndex, InstanceIndex | 切图（含副本索引，-1=主世界） |
| `S.SafeZoneChanged`（`ServerPackets.cs:787-790`） | InSafeZone | 进出安全区 |

**移动节流公式汇总**（移植必抄）：
- 走/跑：`ActionTime = MoveTime = Now + 600ms`（`PlayerObject.cs:14665-14666`）
- 转身：`ActionTime = Now + 300ms`（`PlayerObject.cs:13953`）
- Slow 毒：`slow = poison.Value * 100ms` 加到 ActionTime 并随包广播（`PlayerObject.cs:13958-13964, 14679-14685`）
- 中和毒（Neutralize）：转身额外 +300ms（`PlayerObject.cs:13955-13956`）
- 过早包：首个进 `ActionList` 延迟队列（`DelayedAction(ActionTime, ...)`），第二个直接纠正（`PlayerObject.cs:13930-13938`）

## GodotClient 现状

| 功能 | 状态 | GodotClient 证据 |
|---|---|---|
| `.map` 解析（14 字节/格，含 Flag） | **已移植** | `GodotClient/Formats/MapReader.cs:9-117`（`MirMap`/`MapCell`，注释标明"移植自 Client/Scenes/Views/MapControl.cs:484-545"；`cellFlag = ((flag & 0x01) != 1) \|\| ((flag & 0x02) != 2)` 在 `MapReader.cs:67`） |
| 地形渲染（Back/Middle/Front 层、动画、混合位） | **已移植** | `GodotClient/Scripts/MapView.cs:12-83`（48×32 逻辑格、WorldScale=2、ManualHeightOffset=34、按行 `MapTerrainRow` 分层重绘）；图库经 `GodotClient/Formats/LibraryCache.cs`/`ZlReader.cs` |
| 光照层（LightSetting + 格子光） | **已移植** | `GodotClient/Scripts/MapLightLayer.cs:105-112`（`AmbientFor(LightSetting, dayTime)` 纯函数，Night=0.25、Twilight=100/255） |
| 天气（Rain/Snow/Fog/Lightning 粒子） | **已移植** | `GodotClient/Scripts/MapWeatherLayer.cs:11-55`（按 ProgUse.Zl 贴图 509/500/550/540 复刻） |
| 鼠标走/跑输入（22.5° 方向、绕路、右键近距转身） | **已移植** | `GodotClient/Scripts/MouseWalker.cs:91-180`（`ComputeDirection:186-215`、`CanMove:218-243` 检查 `MapCell.Flag` + 动态阻挡、`BestWalkDirection:250-266` 复刻 MouseDirectionBest） |
| 单飞包门控（ServerTime 等价物） | **已移植（改进版）** | `GodotClient/Scripts/GameScene.cs:826-830`（`_moveServerLockUntilMs`：发一个 `C.Move` 后锁住直到 `S.ObjectMove`/`S.UserLocation` 回包，`GameScene.cs:993-1003, 1851-1855`） |
| C.Move/C.Turn 发包 | **已移植** | `GameScene.cs:1002`（`C.Move { Direction, Distance }`）、`GameScene.cs:1475`（`C.Turn`，注释说明 Turn 对本人无回包须本地立即应用）；追击节拍按 `Globals.MoveTime`（`CombatController.cs:297-301`） |
| S.ObjectMove/S.UserLocation 收包 | **已移植** | `GodotClient/Network/ServerConnection.cs:427-436`（UserLocation/ObjectMove 事件）、`ServerConnection.cs:290-294`（PendingMoves 积压重放）；移动插值 `GodotClient/Scripts/PlayerRenderer.cs:591-595` |
| 移动动画插值（帧/时间分摊 CellWidth×Distance） | **已移植** | `PlayerRenderer.cs:591-595`（先停在起点再用 Offset 反向回拉补间） |
| 传送点/出口图标（MovementInfo，副本感知） | **已移植** | `GodotClient/Controls/MiniMapDialog.cs:245-254`、`GodotClient/Controls/BigMapDialog.cs:283-292`（`mv.Icon != MapIcon.None` 且过滤跨副本出口） |
| 安全区状态 | **已移植** | `ServerConnection.cs:447`（`S.SafeZoneChanged`）、`GameScene.cs:2230-2235`（`InSafeZone` 属性 + 聊天提示）；仓库/寄售/邮件的 `GameScene.Game.InSafeZone` 门控在 `DXItemCell.cs:400-404, 796-928` 等 |
| 回城卷/随机传送卷 | **已移植（走通用物品通道）** | 物品使用统一 `C.ItemUse`（`ServerConnection.cs:962`），效果由服务端结算，客户端无需特判 |
| GM 大地图传送（TeleportRing） | **已移植** | `GodotClient/Scripts/GameScene.cs:427-431`、`ServerConnection.cs:1086`、`MiniMapDialog.cs:83-148`、`BigMapDialog.cs:81-98` |
| 自动寻路（AutoPath） | **已移植** | `GameScene.cs:6092-6101`（`SendAutoPathStart/Waypoint/MoveStarted`）、`ServerConnection.cs:1039-1042`、`Controls/AutoPathRouteControl.cs` |
| 挖矿（Flag==true 目标格） | **部分移植** | 挖矿 UI/状态（`FishingDialog`/`HorseTameDialog` 同级的挖掘流程）未见独立实现；`MouseWalker` 不处理 Alt+左键收割分支（`MouseWalker.cs:124-129` 注释说明让位给原版分支）。未找到完整 Mining 循环移植，疑在 `GameScene.cs` 物品使用/攻击链内 |
| 走路撞人挤压（CellTime 幽灵格） | **服务端行为，无需客户端移植** | 客户端只用 `MapCell.Flag + 动态阻挡回调`（`MouseWalker.cs:218-225`） |

## 移植注意事项

1. **别造 CellFlag 枚举**：新客户端只需要一个 bool（`Flag`/阻挡）。若要高台/水面语义必须扩展 `.map` 解析并自行定义位含义——服务端现状（`Map.cs:80-84`）永远只看 0x01/0x02 两位，解析再多位也不会被服务端使用。
2. **格子 null ≠ 阻挡对象**：服务端地图上"无效格"根本没有 `Cell` 对象（`GetCell` 返回 null），任何 `GetCell(...)?.xxx` 之外的下标访问都会 NPE。Godot 侧 `MapCell[,]` 是值类型稠密数组，语义等价但要注意 `MirMap.Cells` 无 null。
3. **走/跑不是两个包**：移植网络层时不要发明 `C.Walk/C.Run`；`C.Move.Distance`（1=走、2=跑、3=骑马跑）+ 服务端 `distance == 3 && Horse == None` 拒绝（`PlayerObject.cs:14604-14608`）就是全部。跑步破隐身/延迟回血逻辑挂在 `distance > 1`（`PlayerObject.cs:14635-14644`）。
4. **节流必须在两端都实现**：服务端 600ms MoveTime 硬校验 + `S.UserLocation` 纠正；客户端 `ServerTime` 单飞包门控（Godot 已用 `_moveServerLockUntilMs` 复刻，`GameScene.cs:826-830`）。只做服务端会造成"包被吞"；只做客户端会被纠正包打断动画。
5. **`Slow` 毒的时间要叠加到本地动画**：`S.ObjectMove.Slow` 广播字段就是服务端算出的 `poison.Value * 100ms`（`PlayerObject.cs:14679-14685`），客户端收包后应推迟下一次动作（原版 `NextActionTime += p.Slow`，`CConnection.cs:1013`）。
6. **传送目标格带 Movements 会拒绝 Teleport**（`MapObject.cs:946`）——防止把玩家传到传送点上无限跳；但走路踩点（`GetMovement`）是递归的，落点又是传送点会继续跳（`Map.cs:729`）。
7. **跨副本边界由服务端强制**：NPC 传送保留 `CurrentMap.Instance/InstanceSequence`（`NPCObject.cs:97`）、`JoinInstance` 拒绝从副本进副本（`PlayerObject.cs:16854-16858`）。客户端只需要正确携带/显示 `S.MapChanged.InstanceIndex`。
8. **安全区判定是服务端权威**：客户端 `InSafeZone` 只影响 UI 门控（仓库/寄售/邮件），PvP/耐久等全部在服务端；Godot 端不要自行按区域几何判定安全区。
9. **`MovementInfo` 同格多条是随机挑一条**（5 次尝试，`Map.cs:595-597`），小地图箭头图标（`mv.Icon`）只取展示，不保证与实际跳转一致。
10. **`MapInfo` 的 `MonsterHealth/DropRate` 系列是死字段**（`MapInfo.cs:303-454` 标 `//DO NOT USE`），倍率实际走 `SEnvir` 配置与 `BuffType.MapEffect`（`MapInfoStat` → `Info.Stats` → `ApplyMapBuff`，`PlayerObject.cs:9279-9295`）。
