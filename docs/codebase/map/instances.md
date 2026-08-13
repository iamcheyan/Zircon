# 副本系统（InstanceInfo 槽位模型 / 创建与回收 / 难度缩放 / 事件地图 / 副本内规则）

## TL;DR 速查表

- 副本 = `InstanceInfo`（System.db）定义"一种副本"，运行时形态是**同一批 `MapInfo` 的第 N 份拷贝**：`SEnvir.Instances[instance][instanceSequence] = Dictionary<MapInfo, Map>`（`ServerLibrary/Envir/SEnvir.cs:326-327, 736-741`）；槽位数 = `InstanceInfo.MaxInstances`（0 → `byte.MaxValue`）。
- 主世界地图字典是 `Maps`（懒加载，`SEnvir.GetMap`，`SEnvir.cs:4240-4286`）；副本槽必须先 `LoadInstance` 整体创建（`SEnvir.cs:4296-4314`），不存在"半加载"。
- 进入副本四条路：副本查找器 `C.JoinInstance`（`PlayerObject.cs:16845-16905`）、走路踩 `MovementInfo.NeedInstance` 传送点（`Map.cs:601-621`）、NPC 脚本 Teleport（`NPCObject.cs:72-94`）、事件动作 PlayerTeleport。分配槽位的统一入口是 `PlayerObject.GetInstance`（`PlayerObject.cs:16907-17147`），按 `InstanceType`（Player/Group/Guild/Castle，`Enum.cs:367-373`）分支。
- 回收：每秒扫描，`InstanceExpiry`（= 创建时刻 + `TimeLimitInMinutes`，`Map.cs:54`）到期或**全部地图 LastPlayer 超时 `Globals.InstanceUnloadTimeInMinutes=5` 分钟**（`SEnvir.cs:1546-1570, Globals.cs:97`）→ `UnloadInstance`：逐个把玩家传出去（ReconnectRegion → ReconnectMap → BindPoint 三级 fallback，`SEnvir.cs:4320-4345`）、移除刷怪、清 EventLogs、清 UserRecord 并写入冷却。
- "难度缩放"真身：① `InstanceInfoStat` → `Stats` → 玩家进图加 `BuffType.InstanceEffect` buff（`PlayerObject.cs:9291-9294`）；② `DungeonInfo.SpawnMultiplier` → `SpawnInfo.DoSpawn` 按倍率增刷怪数（`Map.cs:423-427`）。**未找到按人数/动态等级的 raid scaling 实现**（全仓库无 difficulty/RaidScaling 命中）。
- **DynRegion / 动态地图生成不存在**：`grep DynRegion|DynamicMap` 全仓库仅命中文档计划本身。副本的"动态性"只体现在：槽位按需创建/销毁 + 事件系统按玩家/实例动态刷怪（`Events/Actions/MonsterPlayerSpawn.cs`）。
- 事件系统（WorldEvent/PlayerEvent/MonsterEvent）与副本通过 `EventTrackingType.Instance` 关联——EventLog 以 `Instance:{index}:{sequence}` 为键（`EventInfoHandler.cs:400-404`），实例卸载时清日志（`SEnvir.cs:4349`）。
- 副本内特殊规则：`AllowTeleport` 拦回城卷/传送戒指、`AllowRejoin`/`SavePlace`/`MaxPlayerCount` 控人数与重进、死亡无原地复活（10 分钟自动回 BindPoint 出副本）、副本切图发 `S.MapChanged { MapIndex, InstanceIndex }`、时限副本发 `S.SetTimer("Map", ...)` 倒计时。
- Godot 客户端**已移植**副本查找器 UI、JoinInstance 协议、S.MapChanged 的 InstanceIndex 跟踪、副本 buff 图标、副本倒计时对话框；大/小地图的副本内出口过滤也已实现。

## 职责概述

本文覆盖 Zircon 的多副本（Instance）体系：静态数据模型（`InstanceInfo`/`InstanceMapInfo`/`InstanceType`，以及与之关联的 `DungeonInfo`/`DungeonMapInfo` 地城编组）、服务端 `SEnvir` 中实例字典的创建/懒加载/整装/回收全流程、玩家进入副本的四条路径与槽位分配算法（按 Player/Group/Guild/Castle 四种类型）、难度缩放的真实实现位置（实例 buff + 地城刷怪倍率）、"动态地图"概念的现状核实（不存在 DynRegion）、事件系统（`Envir/Events/`）与副本的联动，以及副本内死亡/传送/人数等特殊规则。地图格与移动本身的机制另见 `map/tiles-and-movement.md`。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| `LibraryCore/SystemModels/InstanceInfo.cs` | 8-340 | 副本定义：Type/MaxInstances/等级与人数门槛/RequiredItem/ConnectRegion/ReconnectRegion/冷却与时限/UserRecord 字典/Stats |
| `LibraryCore/SystemModels/InstanceInfo.cs` | 342-391 | `InstanceMapInfo`：副本包含的地图 + 每图 `RespawnIndex`（刷怪方案分套） |
| `LibraryCore/SystemModels/InstanceInfo.cs` | 393-442 | `InstanceInfoStat`：实例级属性加成（→ BuffType.InstanceEffect） |
| `LibraryCore/SystemModels/DungeonInfo.cs` | 8-103 | 地城编组：SpawnMultiplier、AverageMonsterLevel/Experience（统计只读） |
| `LibraryCore/SystemModels/DungeonInfo.cs` | 105-177 | `DungeonMapInfo`：Dungeon↔Map 关联 + Floor 层数 + `DungeonMapRole` |
| `LibraryCore/SystemModels/EventInfo.cs` | 8-1023 | WorldEvent/PlayerEvent/MonsterEvent 三族事件（计数器 + 阈值动作）与枚举 |
| `LibraryCore/Enum.cs` | 367-385 | `InstanceType`（Player/Group/Guild/Castle）、`DungeonMapRole`（Entrance/Lobby/Floor/…/BossFloor） |
| `LibraryCore/Enum.cs` | 2364-2383 | `InstanceResult`（17 种进本失败原因 + Success） |
| `ServerLibrary/Envir/SEnvir.cs` | 242-246, 326-328 | DBCollection 与 `Maps`/`Instances` 运行时字典 |
| `ServerLibrary/Envir/SEnvir.cs` | 731-741 | 启动时按 `MaxInstances` 预建空槽数组 |
| `ServerLibrary/Envir/SEnvir.cs` | 1043-1242 | CreateSafeZones/CreateSpawns/RemoveSpawns（均带 instance 参数，副本复用） |
| `ServerLibrary/Envir/SEnvir.cs` | 1546-1570 | 每秒实例扫描：到期/无人 → UnloadInstance |
| `ServerLibrary/Envir/SEnvir.cs` | 4216-4238 | `FinaliseMapLoad()`：单图加载收尾（含实例分支日志） |
| `ServerLibrary/Envir/SEnvir.cs` | 4240-4286 | `GetMap(info, instance, sequence)`：主世界懒加载 / 实例内按需建图 |
| `ServerLibrary/Envir/SEnvir.cs` | 4288-4314 | `GetInstance(info)` 取槽数组、`LoadInstance()` 整槽装载 |
| `ServerLibrary/Envir/SEnvir.cs` | 4316-4386 | `UnloadInstance()`：迁出玩家、删刷怪/事件日志、清记录、写冷却 |
| `ServerLibrary/Models/Map.cs` | 18-21, 45-56 | `Map.Instance/InstanceSequence/RespawnIndex` 与 `InstanceExpiry`（时限） |
| `ServerLibrary/Models/Map.cs` | 423-427 | `SpawnInfo.DoSpawn` 应用 `Dungeon.SpawnMultiplier` |
| `ServerLibrary/Models/PlayerObject.cs` | 16843-16905 | `JoinInstance(C.JoinInstance)`：副本查找器入口（组队批量传送） |
| `ServerLibrary/Models/PlayerObject.cs` | 16907-17147 | `GetInstance()`：槽位分配核心（四类型分支 + 门槛 + 冷却） |
| `ServerLibrary/Models/PlayerObject.cs` | 17149-17192 | `CheckInstanceFreeSpace()`：人数上限（SavePlace 按 UserRecord、否则按在线人数） |
| `ServerLibrary/Models/PlayerObject.cs` | 17201-17288 | `SendInstanceMessage()`：InstanceResult → 本地化提示 |
| `ServerLibrary/Models/PlayerObject.cs` | 6454-6460, 2889 | `AllowTeleport` 拦截回城卷 / TeleportRing |
| `ServerLibrary/Models/PlayerObject.cs` | 9279-9295 | `ApplyMapBuff()`：MapEffect + InstanceEffect 双 buff |
| `ServerLibrary/Models/PlayerObject.cs` | 1527-1534 | 时限副本发 `SetTimer("Map", InstanceExpiry)` 倒计时 |
| `ServerLibrary/Models/Map.cs` | 599-621 | `Cell.GetMovement` 的 NeedInstance 进/出副本分支 |
| `ServerLibrary/Models/NPCObject.cs` | 68-108 | NPC 脚本传送进副本 |
| `ServerLibrary/Envir/Events/EventInfoHandler.cs` | 10-412 | 事件总线（反射注册 Trigger/Action、EventLog 键） |
| `ServerLibrary/Envir/Events/Actions/MonsterPlayerSpawn.cs` | 1-85 | 按玩家/实例动态刷怪（事件侧"动态地图"） |
| `ServerLibrary/Envir/Events/Actions/PlayerTeleport.cs` | 1-75 | 事件传送（保留当前实例） |
| `LibraryCore/Globals.cs` | 97 | `InstanceUnloadTimeInMinutes = 5` |
| `LibraryCore/Network/ClientPackets.cs` | 779-782 | `C.JoinInstance { Index }` |
| `LibraryCore/Network/ServerPackets.cs` | 1412-1415 | `S.JoinInstance { Result, Success }` |
| `Client/Scenes/Views/DungeonFinderDialog.cs` | 349-428 | 旧客户端副本查找器（按类型确认后发包） |

## 核心流程

### 1. 数据模型：一种副本、一排槽位

`InstanceInfo`（`LibraryCore/SystemModels/InstanceInfo.cs:8-340`）字段速览（行号为字段起始行）：

| 字段 | 行号 | 语义 |
|---|---|---|
| `Name` / `Type` | 11 / 26 | 名称 / `InstanceType`（Player=单人、Group=组队、Guild=行会、Castle=城堡） |
| `MaxInstances` | 41 | 最大并行槽位（byte；0 视为 255，`SEnvir.cs:738`） |
| `ShowOnDungeonFinder` | 56 | 是否出现在副本查找器 UI |
| `SafeZoneOnly` | 71 | 只能从安全区内打开查找器进入 |
| `AllowRejoin` | 86 | 离开后是否允许重进自己的槽位（UserRecord 判定） |
| `AllowTeleport` | 101 | 副本内是否允许回城卷/传送戒指 |
| `SavePlace` | 116 | 人数上限按"登记名单"而不是在线人数计 |
| `MinPlayerLevel` / `MaxPlayerLevel` | 131 / 146 | 等级门槛 |
| `MinPlayerCount` / `MaxPlayerCount` | 161 / 176 | 组队人数下限/上限 |
| `RequiredItem` / `RequiredItemSingleUse` | 191 / 206 | 进本消耗品 |
| `ConnectRegion` | 222 | 进本落地区域 |
| `ReconnectRegion` | 237 | 副本销毁时玩家的迁出区域 |
| `CooldownTimeInMinutes` | 252 | 出本后冷却（写 UserCooldown / GuildCooldown） |
| `TimeLimitInMinutes` | 267 | 副本时限（0=不限时） |
| `ShowTimer` | 282 | 是否给玩家显示倒计时 |
| `Maps` (`InstanceMapInfo`) | 299 | 副本包含的地图及各自 `RespawnIndex` |
| `BuffStats` (`InstanceInfoStat`) | 302 | 实例属性加成（难度缩放载体之一） |
| `UserRecord` / `UserCooldown` / `GuildCooldown` | 306-314 | 运行时（非持久化）：玩家→槽位映射、个人/行会冷却 |

`InstanceType`（`LibraryCore/Enum.cs:367-373`）：

```csharp
public enum InstanceType : byte
{
    Player = 0,   // 个人副本：每人/每槽独立
    Group = 1,    // 组队副本：整队同槽
    Guild = 2,    // 行会副本：全行会同槽
    Castle = 3    // 城堡（攻城）副本
}
```

`Map` 运行时知道自己属于哪个槽（`ServerLibrary/Models/Map.cs:18-21`），构造时算出过期时间（`Map.cs:45-56`）：

```csharp
public Map(MapInfo info, InstanceInfo instance = null, byte instanceSequence = 0, int respawnIndex = 0)
{
    ...
    if (instance != null)
    {
        Instance = instance;
        InstanceSequence = instanceSequence;
        InstanceExpiry = instance.TimeLimitInMinutes > 0 ? SEnvir.Now.AddMinutes(instance.TimeLimitInMinutes) : DateTime.MinValue;
    }
}
```

### 2. SEnvir：实例字典的创建与回收

启动时预建空槽（`SEnvir.cs:736-741`）：

```csharp
for (int i = 0; i < InstanceInfoList.Count; i++)
{
    int count = InstanceInfoList[i].MaxInstances > 0 ? InstanceInfoList[i].MaxInstances : byte.MaxValue;
    Instances[InstanceInfoList[i]] = new Dictionary<MapInfo, Map>[count];
}
```

`GetMap` 的实例分支（`SEnvir.cs:4240-4286`）——注意实例图**不能**通过 GetMap 凭空创建槽，槽必须已 Load：

```csharp
if (!Instances.TryGetValue(instance, out var instanceMaps)) return null;

if (instanceSequence >= instanceMaps.Length || instanceMaps[instanceSequence] == null) return null;  // 槽未装载 → null

if (instanceMaps[instanceSequence].TryGetValue(info, out Map instanceMap)) return instanceMap;

var instanceMapInfo = instance.Maps.FirstOrDefault(x => x.Map == info);
if (instanceMapInfo == null) return null;    // 该副本不含此图

lock (MapLoadLock)   // 槽内单图懒加载（副本图大时按需读文件）
{
    ...
    instanceMap = new Map(info, instance, instanceSequence, instanceMapInfo.RespawnIndex);
    instanceMaps[instanceSequence][info] = instanceMap;
    FinaliseMapLoad(instanceMap);            // 安全区/传送点/NPC/刷怪/任务区域
}
```

`LoadInstance`（`SEnvir.cs:4296-4314`）一次装载全部地图（`JoinInstance` 路径）：

```csharp
public static byte? LoadInstance(InstanceInfo instance, byte instanceSequence)
{
    var mapInstance = Instances[instance];
    mapInstance[instanceSequence] = new Dictionary<MapInfo, Map>();

    for (int i = 0; i < instance.Maps.Count; i++)
    {
        var mapInfo = instance.Maps[i];
        Map map = new Map(mapInfo.Map, instance, instanceSequence, mapInfo.RespawnIndex);
        mapInstance[instanceSequence][mapInfo.Map] = map;
        FinaliseMapLoad(map);
    }
    Log($"Loaded Instance {instance.Name} at index {instanceSequence}");
    return instanceSequence;
}
```

回收扫描（主循环每秒段，`SEnvir.cs:1546-1570`）：

```csharp
foreach (var instance in Instances)
{
    for (byte instanceSequence = 0; instanceSequence < instance.Value.Length; instanceSequence++)
    {
        bool expired = false;
        if (instance.Value[instanceSequence] == null) continue;

        foreach (KeyValuePair<MapInfo, Map> pair in instance.Value[instanceSequence])
        {
            pair.Value.Process();
            if (pair.Value.InstanceExpiry != DateTime.MinValue && pair.Value.InstanceExpiry < Now)
                expired = true;                                   // 任一图到期 → 整槽过期
        }

        if (expired || instance.Value[instanceSequence].Values.All(x => x.LastPlayer.AddMinutes(Globals.InstanceUnloadTimeInMinutes) < DateTime.UtcNow))
            UnloadInstance(instance.Key, instanceSequence);       // 全图 5 分钟无人 → 回收
    }
}
```

`UnloadInstance`（`SEnvir.cs:4316-4386`）按顺序：① 逐图把玩家迁出（`instance.ReconnectRegion` → 该图 `MapInfo.ReconnectMap` → `Character.BindPoint` 三级 fallback）；② `RemoveSpawns(instance, sequence)`（`SEnvir.cs:1239-1242`）；③ 删本槽 EventLogs；④ 置空槽位；⑤ 清 `UserRecord` 中指向该槽的玩家并按 `CooldownTimeInMinutes` 写入 `UserCooldown`（Guild 类型写 `GuildCooldown`）。

### 3. 进入副本：槽位分配算法 `GetInstance`

`C.JoinInstance`（`PlayerObject.cs:16845-16905`）入口规则：**已身处副本则拒绝跨副本**（`PlayerObject.cs:16854-16858`）；组队类型且目标槽无人时**整队一起传送**（`PlayerObject.cs:16876-16891`）。

`GetInstance(instance, checkOnly, dungeonFinder, walkOn)`（`PlayerObject.cs:16907-17147`）统一闸门：

```csharp
if (instance.ConnectRegion == null && !walkOn)
    return (null, InstanceResult.ConnectRegionNotSet);

if (instance.MinPlayerLevel > 0 && Level < instance.MinPlayerLevel
    || instance.MaxPlayerLevel > 0 && Level > instance.MaxPlayerLevel)
    return (null, InstanceResult.InsufficientLevel);

if (dungeonFinder && instance.SafeZoneOnly && !InSafeZone)
    return (null, InstanceResult.SafeZoneOnly);

if (instance.UserRecord.ContainsKey(Name) && !instance.AllowRejoin)
    return (null, InstanceResult.NoRejoin);
```

四类型分支（`PlayerObject.cs:16926-17108`）：
- **Player**：个人冷却检查 → 若 `UserRecord` 里有自己的槽且 `CheckInstanceFreeSpace` 通过则回自己的槽（并清冷却）→ 否则线性找第一个有空位的槽 `i`，写入 `UserRecord[Name] = i`。
- **Group**：冷却 → 必须有队（`NotInGroup`）→ `MinPlayerCount > 1 && GroupMembers.Count < MinPlayerCount` → `TooFewInGroup`；`MaxPlayerCount > 1 && Count > MaxPlayerCount` → `TooManyInGroup` → 优先加入队友所在的槽（`member.CurrentMap.Instance == instance`）→ 回自己登记的槽 → 查找器路径要求队长（`NotGroupLeader`）。
- **Guild / Castle**：行会检查（`NotInGuild`，Castle 还要求 `Guild.Castle != null` 即 `NotInCastle`）→ 行会冷却 → 优先加入本行会成员所在槽 → 回自己登记的槽。

兜底新建槽（`PlayerObject.cs:17110-17144`）：

```csharp
byte? instanceSequence = null;
for (byte i = 0; i < mapInstance.Length; i++)
    if (mapInstance[i] == null) { instanceSequence = i; break; }

if (instanceSequence == null)
    return (null, InstanceResult.NoSlots);            // 槽满

if (instance.RequiredItem != null)
{
    if (GetItemCount(instance.RequiredItem) == 0)
        return (null, InstanceResult.MissingItem);
    if (instance.RequiredItemSingleUse && !checkOnly)
        TakeItem(instance.RequiredItem, 1);
}

if (!checkOnly)
{
    SEnvir.LoadInstance(instance, instanceSequence.Value);   // 整槽装载
    ...UserRecord[Name] = instanceSequence.Value...
}
```

人数上限判定（`PlayerObject.cs:17149-17192`）：

```csharp
if (instance.MaxPlayerCount > 0)
{
    if (instance.SavePlace)
    {
        // 按 UserRecord 登记名单计（掉线/出本也占位）
        if (!instanceUserRecord.Contains(Name) && instanceUserRecord.Count >= instance.MaxPlayerCount)
            return false;
    }
    else
    {
        var playersOnInstance = maps.Values.SelectMany(x => x.Players);
        if (playersOnInstance.Count() >= instance.MaxPlayerCount)
            return false;
    }
}
```

其余两条进入路径：
- **走路踩点**：`Cell.GetMovement` 里 `movement.NeedInstance != null` 分支（`ServerLibrary/Models/Map.cs:601-621`）——在主世界触发则 `GetInstance(movement.NeedInstance, walkOn: true)` 进本；在副本内触发则回主世界 `GetMap(..., null, 0)`。
- **NPC 脚本**：`NPCObject.DoActions` 的 `NPCActionType.Teleport`（`NPCObject.cs:72-94`）——`GetInstance(action.InstanceParameter1)` 成功后 `ob.Teleport(instance.ConnectRegion, instance, index.Value)`。

### 4. 难度缩放的真实实现

**(a) 实例属性 buff**：`InstanceInfo.BuffStats`（`InstanceInfoStat` 列表）在 `OnLoaded` 聚合进 `Stats`（`InstanceInfo.cs:318-334`）；玩家切图时统一施加（`PlayerObject.cs:9279-9295`）：

```csharp
public void ApplyMapBuff()
{
    BuffRemove(BuffType.MapEffect);
    BuffRemove(BuffType.InstanceEffect);

    if (CurrentMap == null) return;

    if (CurrentMap.Info.Stats.Count != 0)
        BuffAdd(BuffType.MapEffect, TimeSpan.MaxValue, CurrentMap.Info.Stats, false, false, TimeSpan.Zero);

    if (CurrentMap.Instance != null && CurrentMap.Instance.Stats.Count != 0)
        BuffAdd(BuffType.InstanceEffect, TimeSpan.MaxValue, CurrentMap.Instance.Stats, false, false, TimeSpan.Zero);
}
```
即"难度"= 给玩家（而不是给怪）加减益/增益属性（例如掉宝率、经验率、防御削减），随进出副本自动增删。

**(b) 地城刷怪倍率**：`DungeonInfo.SpawnMultiplier`（`LibraryCore/SystemModels/DungeonInfo.cs:41-57`，decimal）。`MapInfo.DungeonMap`/`Dungeon` 把普通地图归入某个地城（`MapInfo.cs:473-493`）；刷怪时非 Boss 按 `Math.Clamp(Info.Count * spawnMultiplier, 0, int.MaxValue)` 向上取整（`ServerLibrary/Models/Map.cs:423-427`）：

```csharp
int spawnCount = Info.Count;

if (!Info.Monster.IsBoss && CurrentMap.Info.Dungeon != null)
{
    decimal spawnMultiplier = CurrentMap.Info.Dungeon.SpawnMultiplier;
    spawnCount = (int)Math.Ceiling(Math.Clamp(Info.Count * spawnMultiplier, 0M, int.MaxValue));
}
```
`DungeonInfo.AverageMonsterLevel/AverageMonsterExperience`（`DungeonInfo.cs:62-82`）是遍历全部 Respawn 算平均值的只读统计属性（供 UI/策划参考），不参与运行时缩放。`DungeonMapInfo.Floor/Role`（`DungeonInfo.cs:141-176`，`DungeonMapRole` 枚举 `Enum.cs:375-385`：Entrance/Lobby/Floor/SideRoom/Transition/Hub/Maze/BossFloor）目前**只是编辑器元数据**，服务端逻辑中除 `SpawnMultiplier` 与统计外未发现按 Floor/Role 分层的行为。

**(c) raid scaling（按队内人数/平均等级动态调难度）：未找到实现**。全仓库 `grep -i "difficulty|raid scaling"` 无代码命中；实例难度只有上述 (a)(b) 两条静态配置通道。

### 5. 动态地图（DynRegion）：现状核实

- `DynRegion`、`DynamicMap` 关键字在全仓库 `grep` 仅命中 `docs/` 下的文档计划（`docs/ZIRCON_CODEBASE_DOCS_GOAL.md:46`、`docs/codebase/_index.md:68`），**源码中不存在任何动态区域/程序化地图生成实现**。
- 现有体系里最接近"动态"的三个机制：
  1. **槽位生命周期**：同一 `MapInfo` 因 `LoadInstance`/`UnloadInstance` 被实例化/销毁成多份独立 `Map`（怪物、掉落、事件状态全隔离）——这是 Zircon 副本的本质；
  2. **事件动态刷怪/传送**：`Events/Actions/` 下的 `MonsterPlayerSpawn`（在触发玩家身边 10 格内动态 spawn，`MonsterPlayerSpawn.cs:35-42`）、`MonsterSpawn`、`ItemDrop`、`PlayerTeleport`（`PlayerTeleport.cs:29-50`，保留玩家当前 instance/sequence）；
  3. **单图懒加载**：主世界 `Maps` 与实例槽内单图都按需 `GetMap` 触发 `FinaliseMapLoad`（`SEnvir.cs:4244-4283`），配合 `Config.LazyLoadMaps`（`Config.cs:33`）。
- 结论：移植 Godot 客户端时**无需**为"动态地图"预留生成管线；需要复刻的是"同一 FileName 的 .map 在不同 InstanceIndex 下是独立世界"这一语义。

### 6. 事件地图（EventInfo / Envir/Events）与副本的关系

事件数据模型（`LibraryCore/SystemModels/EventInfo.cs`，类起始行）：`WorldEventInfo:8`（全局计数器，`MaxValue/ResetWhenMax` + Triggers/Actions）、`PlayerEventInfo:219`、`MonsterEventInfo:505`（各自 Trigger/Action/TriggerStat）；`BaseEventAction:807`（公共参数：MapParameter1/RegionParameter1/MonsterParameter1/InstanceParameter1/Restrict…）；枚举 `EventTrackingType:966`（Global/Player/Group/Guild/Instance）、`EventActionType:1000`（MonsterSpawn/MonsterPlayerSpawn/PlayerTeleport/PlayerBuffAdd/ItemDrop/TimerStart…）。

`EventInfoHandler`（`ServerLibrary/Envir/Events/EventInfoHandler.cs:10-412`）反射扫描 `IEventTrigger`/`IEventAction` 实现注册（`EventInfoHandler.cs:32-75`），对外三个 `Process(string/player/monster, eventName)` 入口按事件名（`"PLAYERDIE"`、`"MONSTERDIE"`、`"TIMERMINUTE"`、`"PLAYERMOVEMAP"` 等）驱动。触发器目录：`Events/Triggers/`（MonsterClear/MonsterDie/PlayerCommand/PlayerDie/PlayerMoveMap/PlayerMoveRegion/TimerMinute/WorldTimeOfDay）。

**与副本的关联点**：
1. **实例作用域的进度**：EventLog 按 `EventTrackingType` 生成键（`EventInfoHandler.cs:374-409`）：

```csharp
case EventTrackingType.Instance:
    var instance = player.CurrentMap.Instance;
    if (instance == null) return null;
    return $"Instance:{instance.Index}:{player.CurrentMap.InstanceSequence}";
```
即"每个副本槽一份事件进度"——副本内杀怪/清怪的 MonsterEvent 计数按槽隔离。
2. **动作可绑定副本**：`BaseEventAction.InstanceParameter1` 与触发者当前实例比对（如 `MonsterPlayerSpawn.cs:33`：`if (action.InstanceParameter1 != triggerPlayer?.CurrentMap.Instance) return;`），并且 `GetTargetPlayers` 的 `EventTrackingType.Instance` 分支按 `Instance + InstanceSequence` 双重过滤目标（`MonsterPlayerSpawn.cs:80`、`PlayerTeleport.cs:69`）。
3. **实例销毁清进度**：`UnloadInstance` 中 `EventLogs.RemoveAll(x => x.InstanceInfo == instance && x.InstanceSequence == instanceSequence);`（`SEnvir.cs:4349`）。
4. **事件不创建地图**：EventInfo 没有地图字段；事件要么作用于既有地图/区域（MapParameter1/RegionParameter1），要么借用副本槽。所谓"事件地图"实际是"配置了事件触发器的普通地图或副本图"（例：`Map.cs:435-444` 的万圣节/圣诞节怪物替换是硬编码时间窗，不走 EventInfo）。

### 7. 副本内特殊规则汇总

| 规则 | 实现位置 |
|---|---|
| 禁止从副本进另一个副本（查找器/NPC 双闸） | `PlayerObject.cs:16854-16858`、`NPCObject.cs:74-78` |
| 回城卷被 `InstanceInfo.AllowTeleport=false` 拦截 | `PlayerObject.cs:6456-6460` |
| TeleportRing 同样被拦 + 目标图 AllowRT/AllowTT 双检 | `PlayerObject.cs:2889-2893` |
| NPC 普通传送保留当前实例（不出副本乱跳） | `NPCObject.cs:97` |
| 传送点跨副本只在 `NeedInstance` 分支发生 | `Map.cs:599-621` |
| 死亡：无原地复活。`Die()` 设 `RevivalTime = Now + Config.AutoReviveDelay`（10 分钟，`Config.cs:118`），到点 `TownRevive()` 回 `Character.BindPoint`（主世界）——**等于死亡最终会把玩家送出副本** | `PlayerObject.cs:16208-16212, 340-341, 1443-1464` |
| 人数上限：SavePlace 按登记名单 / 否则按在线人数 | `PlayerObject.cs:17164-17189` |
| 组队人数上下限（TooFew/TooMany） | `PlayerObject.cs:16982-16986` |
| 重进：`AllowRejoin=false` 时 UserRecord 命中即拒（NoRejoin） | `PlayerObject.cs:16918-16919` |
| 时限：`InstanceExpiry` 到期整槽回收；进图时 `SetTimer("Map", InstanceExpiry)`（`ShowTimer` 或主世界复位） | `Map.cs:54`、`SEnvir.cs:1558-1561`、`PlayerObject.cs:1527-1534` |
| 冷却：出本（槽回收）时写 `UserCooldown`（Guild 类型写 `GuildCooldown`）；进本成功即清 | `SEnvir.cs:4361-4382`、`PlayerObject.cs:16941-16964` |
| 切图协议带实例索引：`S.MapChanged { MapIndex, InstanceIndex(-1=主世界) }`；`S.ObjectMove.MapChanged` 标记踩点跨图 | `PlayerObject.cs:1481-1485`、`PlayerObject.cs:14694` |
| 实例 buff：进本自动加 `BuffType.InstanceEffect`，出本 `ApplyMapBuff` 先移除再加新图 buff | `PlayerObject.cs:9279-9295`（OnMapChanged 调用点 `PlayerObject.cs:1490`） |
| 骑马限制照常按图生效（`CanHorse=false` 自动下马） | `PlayerObject.cs:1487-1488` |
| 掉线重连：若当前图有 `ReconnectMap` 且不在安全区 → 传回重连图（脱离副本） | `PlayerObject.cs:1129-1132` |
| 副本销毁迁出链：ReconnectRegion → ReconnectMap → BindPoint | `SEnvir.cs:4320-4345` |

## 数据结构/协议细节

**协议**：

| 包 | 字段 | 说明 |
|---|---|---|
| `C.JoinInstance`（`ClientPackets.cs:779-782`） | Index（InstanceInfo.Index） | 副本查找器进入请求 |
| `S.JoinInstance`（`ServerPackets.cs:1412-1415`） | Result（InstanceResult）、Success | 进入结果（失败原因本地化由 `SendInstanceMessage` 另发聊天） |
| `S.MapChanged`（`ServerPackets.cs:91-94`） | MapIndex、InstanceIndex | 副本切图；InstanceIndex=-1 表示回到主世界 |
| `S.SetTimer`（`ServerPackets.cs:1455-1458`） | Key("Map")、Type、Seconds | 副本倒计时（`PlayerObject.cs:1532-1533`） |
| `S.ObjectMove.MapChanged`（`ServerPackets.cs:156`） | bool | 走路踩传送点跨图标记（客户端须立即换图渲染） |

**`InstanceResult` 全枚举**（`LibraryCore/Enum.cs:2364-2383`）：`Invalid, InsufficientLevel, SafeZoneOnly, NotInGroup, NotInGuild, NotInCastle, TooFewInGroup, TooManyInGroup, ConnectRegionNotSet, NoSlots, NoRejoin, NotGroupLeader, UserCooldown, GuildCooldown, MissingItem, NoMap, Success`。

**运行时字典**（`SEnvir.cs:326-327`）：

```csharp
private static Dictionary<MapInfo, Map> Maps = [];
private static Dictionary<InstanceInfo, Dictionary<MapInfo, Map>[]> Instances = [];
```
- 主世界：`Maps` 按需增长，永不卸载。
- 副本：`Instances[info]` 是定长槽数组；`null` = 空槽可分配；槽内 `Dictionary<MapInfo, Map>` 按需单图补装（`GetMap` 实例分支）。
- 所有"当前在哪"的派生判定都基于 `Map.Instance`（InstanceInfo 引用）与 `Map.InstanceSequence`（byte 槽号）二元组——对象可见性、事件键、刷怪隔离（`RespawnIndex` 另见 `SpawnInfo`，`Map.cs:384-393`）。

**`InstanceMapInfo.RespawnIndex`**（`InstanceInfo.cs:377-390`）：同一副本可为每张图指定 `RespawnIndex`，`SpawnInfo.DoSpawn` 开头 `if (CurrentMap.RespawnIndex != Info.RespawnIndex) return;`（`Map.cs:393`）——同一 `RespawnInfo` 在不同副本配置下选择性生效，实现"同图不同刷怪方案"。

## GodotClient 现状

| 功能 | 状态 | GodotClient 证据 |
|---|---|---|
| InstanceInfo 数据加载 | **已移植** | `GodotClient/Network/DatabaseLoader.cs:42`（`Globals.InstanceInfoList = Session.GetCollection<InstanceInfo>()`） |
| 副本查找器 UI（列表/过滤/等级与人数列/进入按钮） | **已移植** | `GodotClient/Controls/DungeonFinderDialog.cs:14-16, 101-145`（`ShowOnDungeonFinder` 过滤、`GetLevel/GetPlayerCount` 显示、`_join` 触发 `SendJoinInstance`）、行控件 `SetInstance:180-194` |
| C.JoinInstance 发包 / S.JoinInstance 收包 | **已移植** | `GodotClient/Network/ServerConnection.cs:1045`（SendJoinInstance）、`ServerConnection.cs:525`（Process + JoinInstanceEvent:142）；结果提示 `GodotClient/Scripts/GameScene.cs:6203-6206`（成功/失败 + `p.Result` 本地化） |
| S.MapChanged 的 InstanceIndex 跟踪 | **已移植** | `GameScene.cs:704-708`（`_playerInstanceIndex`、`CurrentInstanceInfo` 按 Index 查 InstanceInfo）、切图处理 `GameScene.cs:1789-1830`（`[Game] 地图切换: MapIndex=... InstanceIndex=...` 日志）、启动信息 `GameScene.cs:1743-1747` |
| 副本 buff 图标（InstanceEffect） | **已移植** | `GodotClient/Controls/BuffDialog.cs:134, 164`（BuffType.InstanceEffect → 图标 76/文案） |
| 副本倒计时（S.SetTimer "Map"） | **已移植** | `ServerConnection.cs:137, 520`（SetTimerEvent）+ `GameScene.cs:1236-1238` 接线 + `GodotClient/Controls/TimerDialog.cs:10-76`（只显示最近到期计时器，AddTimer 按秒刷新） |
| 大/小地图副本内出口过滤 | **已移植** | `GodotClient/Controls/MiniMapDialog.cs:245-254`、`BigMapDialog.cs:283-292`（`CurrentInstanceInfo` 判断源/目标是否都在本副本内，跨副本且无 NeedInstance 的传送点不画） |
| 走路进副本（NeedInstance 传送点踩踏） | **已移植（服务端驱动）** | 服务端 `Cell.GetMovement` 改写落点后经 `S.ObjectMove { MapChanged=true }`/`S.MapChanged` 通知；Godot 收包换图链路同上 |
| NPC 对话进副本 | **已移植（走通用 NPC 通道）** | NPC 对话/动作由服务端结算，客户端仅收 `S.MapChanged`；`GodotClient/Controls/NPCDialog.cs` 系列已接通 |
| 副本内死亡/回城 | **部分移植** | 复活回城依赖 `C.TownRevive`/`S.ObjectRevive` 链路；Godot 侧死亡处理见 `GameScene.cs` 的 `ItemReviveUntilMs/ReincarnationPillUntilMs`（`GameScene.cs:775-776`）。未找到副本专用死亡 UI 差异（本就无副本特判，与主世界同流程） |
| 副本冷却/剩余时间显示（查找器列） | **部分移植** | `DungeonFinderDialog.cs` 未实现冷却倒计时列（旧客户端 `Client/Scenes/Views/DungeonFinderDialog.cs:349-428` 也仅发包）；服务端冷却只通过聊天消息提示 |
| 动态地图生成 | **不适用** | 服务端不存在 DynRegion；Godot 侧无需实现 |

## 移植注意事项

1. **槽位语义**：副本不是"新地图文件"而是 `Map` 对象的第 N 份实例。客户端唯一需要的状态是 `(MapIndex, InstanceIndex)` 二元组——Godot 已用 `_playerMapIndex + _playerInstanceIndex` 维护（`GameScene.cs:704-708`）。对象去重/可见性如果只按 ObjectID 全局缓存，会在两个副本槽间串台（服务端广播本来就只进本 Map，`Map.cs:359-371`）。
2. **进本失败要按 `InstanceResult` 全量本地化**：服务端聊天（`SendInstanceMessage`，`PlayerObject.cs:17201-17288`）+ `S.JoinInstance.Result` 双通道；Godot 的 `Lang.DungeonAlreadyInInstance` 等文案键已备（`GodotClient/Scripts/Lang.cs:120-123`），注意 `NoMap/NotGroupLeader/NotInCastle` 等也要覆盖。
3. **`S.ObjectMove.MapChanged`**：踩传送点跨图时同一包既是移动又是换图（`PlayerObject.cs:14671-14695`）；客户端必须在该包上立即切换地图渲染而不是等 `S.MapChanged`，否则会先在旧图画一步。Godot 的 `PendingMoves` 积压重放（`ServerConnection.cs:290-294`）已处理切图清积压（`MapTestScene.cs:821-825` 审计覆盖）。
4. **倒计时键名**：副本计时器固定 `Key="Map"`（`PlayerObject.cs:1533`），回主世界时 `SetTimer` 秒数为 0 即删除（`TimerDialog.cs:76` 的 `Seconds <= 0 → Remove` 语义要保住）。
5. **难度缩放走 buff 通道**：不要在客户端复算副本难度——`InstanceInfoStat` 全部由服务端以 `BuffType.InstanceEffect` 下发（`PlayerObject.cs:9291-9294`），客户端只需正确显示 buff（Godot `BuffDialog.cs:134` 已做）。
6. **人数/冷却判定全在服务端**：`SavePlace` 与在线人数两种口径（`PlayerObject.cs:17164-17189`）容易在客户端做错镜像；UI 只展示 `MinPlayerCount - MaxPlayerCount`（`DungeonFinderDialog.cs:193-194`），判定一律信服务端回包。
7. **事件-副本进度按槽隔离**：若 Godot 侧将来做事件进度 UI，键必须是 `Instance:{index}:{sequence}`（`EventInfoHandler.cs:400-404`），且槽回收后进度作废（`SEnvir.cs:4349`）。
8. **别等"动态地图"**：仓库不存在 DynRegion/程序化生成；`DungeonMapRole` 只是编辑器标注。Godot 移植范围应是：副本查找器 + JoinInstance + (MapIndex, InstanceIndex) 跟踪 + 倒计时 + 出口过滤，全部已存在，缺的只是细节打磨（冷却显示等）。
