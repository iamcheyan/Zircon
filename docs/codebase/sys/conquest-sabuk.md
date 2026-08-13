# 沙巴克攻城战（CastleInfo + ConquestWar + CastleGate/Guard/Flag/Lord 全流程）

## TL;DR 速查表

- 一座城堡 = `CastleInfo`（System.db，LibraryCore/SystemModels/CastleInfo.cs:6），通过 `MapInfo.Castles` 关联绑定到地图（MapInfo.cs:519-520）；沙巴克地图（3 号图）与 `CastleInfo.Map` 直连。
- 宣战入口：`PlayerObject.GuildConquest(int index)`（PlayerObject.cs:5066-5149），需 **GuildPermission.Leader**、本会无城堡、无待定申请、当前无任何进行中的攻城（`SEnvir.ConquestWars.Count > 0` 即拒绝）；若 `CastleInfo.Item` 非空还需上交 1 个该物品。
- 宣战日期公式：至少 **2 天后**开战；若当前时刻已过当日 `Castle.StartTime` 再顺延 1 天（PlayerObject.cs:5117-5121）。
- 开战调度：主循环里 `nextCount.TimeOfDay >= info.StartTime && Now.TimeOfDay <= info.StartTime` 时 `StartConquest(info, false)`（SEnvir.cs:1607-1613）；`EndTime = Now + Castle.Duration`（SEnvir.cs:2070）。
- 占领判定两条路径：**打倒 CastleLord**（AI 1000，`EXPOwner` 所在攻方行会夺得城堡，CastleLord.cs:255-303）或 **CastleFlag 插旗读条 30 秒**（AI 1001，无人干扰即易主，CastleFlag.cs:20-21、113-206）。
- CastleLord 死后战争**不结束**：清 EXPOwner → 重 Ping → 重生 Boss → 剩余不足 15 分钟则 `EndTime = Now + 15min` → 全服玩家 `ApplyCastleBuff()`（CastleLord.cs:288-299）。
- 战争结束 `EndWar()`：恢复目标区 NPC、DespawnBoss、广播 `S.GuildConquestFinished`，城堡归属即当前 `Guild.Castle == Castle` 的行会（ConquestWar.cs:63-120）；**没有税收结算**——行会税是常驻机制（金币拾取抽成），Castle buff（Exp/Drop/Gold +10）也是常驻的城主福利。
- PK 豁免：攻城期间在攻城地图上 `AtWar()` 对非同会玩家恒真（PlayerObject.cs:5499-5521）→ 不棕名不红名；城门/守卫/领主只吃攻方参与者的伤害。
- 城门（AI 1002）平时对守方行会 4 格内自动开关、攻城时 `War != null` 停止自动开；被拆后 `DeadTime = Now.AddYears(1)`，只能靠行会资金 `RepairGate` 复活。
- GodotClient：攻城协议层（宣战/城主/日程/开战/结束包）、行会窗体战争页+城堡页、沙巴克地图与图库（`3.map` Zircon 原版资源）、沙巴克城门/旗帜渲染均已就位；`UserConquestStats` 战报统计 UI 未移植。

## 职责概述

本文覆盖 Zircon 引擎的攻城战（沙巴克 Sabuk）子系统完整规则，是后续向 Godot 客户端移植攻城玩法的对齐基准：

1. **静态配置（System.db）**：`CastleInfo`（城堡：地图、开战时刻 StartTime、持续时长 Duration、城堡区/目标区/攻方出生区三个 MapRegion、宣战物品 Item、Boss 怪 MonsterInfo、Flags/Gates/Guards 子表）与 `CastleGateInfo`/`CastleGuardInfo`/`CastleFlagInfo`（门/守卫/旗帜的 MonsterInfo + 坐标 + 修理费）。
2. **动态数据（Users.db）**：`UserConquest`（行会的攻城申请：Guild + Castle + WarDate）、`UserConquestStats`（每场战争每角色的战报统计）。
3. **战争运行时**：`ConquestWar`（ServerLibrary/Models/ConquestWar.cs，199 行）——开始/进行/结束三阶段 + Boss 生成/回收 + 参战者踢出（PingPlayers）+ 统计获取；`SEnvir.StartConquest` 组装参战名单；主循环定时触发。
4. **攻城对象 AI**：`CastleObject` 基类（归属行会自动刷新）与 `CastleGate`/`CastleGuard`/`CastleFlag`/`CastleLord` 四个怪物子类（ServerLibrary/Models/Monsters/）。
5. **攻城期规则**：PK 豁免（AtWar）、入口控制（PingPlayers 传送）、守卫重生（RepairCastleGuards）、行会/全服公告（ReceiveChat 广播）。
6. **地图绑定**：`MapInfo.Castles` 关联、`Map.Setup()` 在服务器启动时生成旗帜/城门/守卫、`Map.RefreshFlags()` 刷新旗帜外观。
7. **GodotClient 现状**：协议与 UI 的移植程度、沙巴克地图资源迁移记录（引用 docs/ 内既有审计文档）。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| LibraryCore/SystemModels/CastleInfo.cs | 6-181 | 城堡静态模型：Name(9)/Map(25)/StartTime(40)/Duration(55)/CastleRegion(70)/ObjectiveRegion(85)/AttackSpawnRegion(100)/Item(115)/Monster(130)/Discount(145-158，**全库无人读取**)/Flags·Gates·Guards(160-167)/WarDate 运行时字段(169)/OnLoaded 里 ObjectiveRegion 缺省回落 CastleRegion(171-179) |
| LibraryCore/SystemModels/CastleGateInfo.cs | 5-87 | 城门配置：Castle+Monster+X+Y（联合身份），RepairCost(72-85) |
| LibraryCore/SystemModels/CastleGuardInfo.cs | 5-101 | 守卫配置：同上外加 Direction(72-85)、RepairCost(87-100) |
| LibraryCore/SystemModels/CastleFlagInfo.cs | 5-71 | 旗帜配置：Castle+Monster+X+Y，无修理费 |
| LibraryCore/SystemModels/MapInfo.cs | 518-520 | `[Association("Castles", true)] DBBindingList<CastleInfo> Castles`——地图↔城堡绑定 |
| ServerLibrary/DBModels/UserConquest.cs | 8-71 | 攻城申请：Guild(11-24)/Castle(26-39)/WarDate(41-54)；OnDeleted 清关联(62-68) |
| ServerLibrary/DBModels/UserConquestStats.cs | 9-237 | 战报统计：Character(10)/WarStartDate(25)/CastleName(40)/CharacterName(56)/GuildName(71)/Level(86)/Class(101)/BossDamageTaken(118)/BossDamageDealt(133)/BossDeathCount(148)/BossKillCount(163)/PvPDamageTaken(178)/PvPDamageDealt(193)/PvPKillCount(208)/PvPDeathCount(223) |
| ServerLibrary/DBModels/GuildInfo.cs | 213-227 | `Conquest` 关联（本会的攻城申请）；229-242 `Castle`（本会拥有的城堡）；340-345 OnDeleted 时释放城堡 |
| ServerLibrary/Models/ConquestWar.cs | 13-197 | 攻城战运行时对象（下文全流程逐段照抄） |
| ServerLibrary/Envir/SEnvir.cs | 2035-2075 | `StartConquest(info, forced)`：收集参战行会 + 组装 ConquestWar |
| ServerLibrary/Envir/SEnvir.cs | 1607-1613 | 主循环触发：到达 StartTime 自动开战 |
| ServerLibrary/Envir/SEnvir.cs | 1575-1576 | 每帧调用 `ConquestWars[i].Process()` |
| ServerLibrary/Envir/SEnvir.cs | 4388-4398 | `GetConquestStats(player)`：按所在攻城地图取/建统计 |
| ServerLibrary/Envir/SEnvir.cs | 337, 1357 | `ConquestWars` 静态列表 |
| ServerLibrary/Models/PlayerObject.cs | 5066-5149 | `GuildConquest`：宣战申请全流程 + 开战日期公式 |
| ServerLibrary/Models/PlayerObject.cs | 5499-5521 | `AtWar`：攻城期 PK 豁免判定 |
| ServerLibrary/Models/PlayerObject.cs | 5201-5242 | `GuildToggleCastleGates`：城主开关全部城门 |
| ServerLibrary/Models/PlayerObject.cs | 5244-5313 | `GuildRepairCastleGates`：修理费公式 + 复活/修复 |
| ServerLibrary/Models/PlayerObject.cs | 5315-5384 | `GuildRepairCastleGuards`：同上（守卫版） |
| ServerLibrary/Models/PlayerObject.cs | 9355-9368 | `ApplyCastleBuff`：城主行会 Exp/Drop/Gold +10 |
| ServerLibrary/Models/PlayerObject.cs | 1169-1171, 1285-1287 | 登录时补发进行中的 `S.GuildConquestStarted` |
| ServerLibrary/Models/Monsters/CastleObject.cs | 9-50 | 攻城对象基类：Castle/War/Guild 字段、OnSpawned 解析归属行会、Process→RefreshGuild 归属变更刷新 |
| ServerLibrary/Models/Monsters/CastleGate.cs | 12-278 | 城门（AI 1002）：开关+阻挡+伤害档位+修理 |
| ServerLibrary/Models/Monsters/CastleGuard.cs | 12-169 | 守卫（AI 1003）：定点远程箭塔 |
| ServerLibrary/Models/Monsters/CastleFlag.cs | 14-306 | 旗帜（AI 1001）：30 秒读条占领 + 行会旗色渲染 |
| ServerLibrary/Models/Monsters/CastleLord.cs | 12-305 | 攻城 Boss（AI 1000）：打死即夺城 + 15 分钟延长规则 |
| ServerLibrary/Models/MonsterObject.cs | 638-643 | `GetMonster` 按 MonsterInfo.AI 映射：1001→CastleFlag、1002→CastleGate、1003→CastleGuard（CastleLord 由 ConquestWar.SpawnBoss 直接 new） |
| ServerLibrary/Models/Map.cs | 33-36 | `CastleFlags/CastleGates/CastleGuards` 地图级列表 |
| ServerLibrary/Models/Map.cs | 91-102, 125-184 | `Setup()` 启动时生成旗帜/城门/守卫 |
| ServerLibrary/Models/Map.cs | 209-215 | `RefreshFlags()`：改旗色/旗号后刷新旗帜外观 |
| ServerLibrary/Models/MapObject.cs | 568-578 | 攻城地图上 HuntGold 直接 +1（跳过上限累积） |
| ServerLibrary/Envir/Commands/Command/Admin/StartConquest.cs | 22-27 | GM 命令强制开战（forced=true，跳过参战收集） |
| ServerLibrary/Envir/Commands/Command/Admin/EndConquest.cs | 22-26 | GM 命令强制结束 |
| ServerLibrary/Envir/Translations/ChineseMessages.cs | 60-67, 204-210 | 攻城全部中文播报文案 |
| Client/Scenes/Views/GuildDialog.cs | 415, 566-575 | 原版客户端：城堡页只在"本会拥有城堡"时显示；GuildCastleInfo 驱动 |

## 核心流程

### 1. 宣战（PlayerObject.GuildConquest，PlayerObject.cs:5066-5149）

```csharp
public void GuildConquest(int index)
{
    if (Character.Account.GuildMember == null)
    { ... return; }

    if ((Character.Account.GuildMember.Permission & GuildPermission.Leader) != GuildPermission.Leader)
    { ... return; }

    if (Character.Account.GuildMember.Guild.Castle != null)
    {
        // "你已经是城主，无法申请攻城战"
        ... return;
    }

    if (Character.Account.GuildMember.Guild.Conquest != null)
    {
        // "你已经申请攻城战"
        ... return;
    }

    CastleInfo castle = SEnvir.CastleInfoList.Binding.FirstOrDefault(x => x.Index == index);

    if (castle == null)
    { ... return; }

    if (SEnvir.ConquestWars.Count > 0)
    {
        // "攻城战期间无法提交申请攻城战" —— 全服同时只能有一场攻城
        ... return;
    }

    if (castle.Item != null)
    {
        if (GetItemCount(castle.Item) == 0)
        {
            // "你需要{0}来提交申请{1}攻城战"
            ... return;
        }

        TakeItem(castle.Item, 1);
    }

    DateTime now = SEnvir.Now;
    DateTime date = new DateTime(now.Ticks - now.TimeOfDay.Ticks + TimeSpan.TicksPerDay * 2);

    if (now.TimeOfDay.Ticks >= castle.StartTime.Ticks)
        date = date.AddTicks(TimeSpan.TicksPerDay);

    UserConquest conquest = SEnvir.UserConquestList.CreateNewObject();
    conquest.Guild = Character.Account.GuildMember.Guild;
    conquest.Castle = castle;
    conquest.WarDate = date;

    GuildInfo ownerGuild = SEnvir.GuildInfoList.Binding.FirstOrDefault(x => x.Castle == castle);

    if (ownerGuild != null)
    {
        foreach (GuildMemberInfo member in ownerGuild.Members)
        {
            if (member.Account.Connection?.Player == null) continue; //Offline

            member.Account.Connection.ReceiveChat(member.Account.Connection.Language.GuildConquestSuccess, MessageType.System);
            member.Account.Connection.Enqueue(new S.GuildConquestDate { Index = castle.Index, WarTime = (date + castle.StartTime) - SEnvir.Now, ObserverPacket = false });
        }
    }

    //Send War Date to guild.
    foreach (GuildMemberInfo member in Character.Account.GuildMember.Guild.Members)
    {
        if (member.Account.Connection?.Player == null) continue; //Offline

        member.Account.Connection.ReceiveChat(string.Format(member.Account.Connection.Language.GuildConquestDate, castle.Name), MessageType.System);
        member.Account.Connection.Enqueue(new S.GuildConquestDate { Index = castle.Index, WarTime = (date + castle.StartTime) - SEnvir.Now, ObserverPacket = false });
    }
}
```

**宣战条件汇总**：①有行会；②`GuildPermission.Leader`；③本会不拥有任何城堡；④本会没有待开战的申请；⑤城堡存在；⑥**全服当前没有进行中的攻城**（多城堡也不能同时打）；⑦若城堡配置了 `Item`（攻城申请书类物品）需消耗 1 个。

**开战日期公式解读**（5117-5121）：`now.Ticks - now.TimeOfDay.Ticks` 是今日 0 点，+2 天 = 后天 0 点；若现在已过当日 StartTime（例如配置 20:00 开战、现在 21:00），则顺延到第 3 天。即**宣战日 + 2 或 3 天的那一天的开战时刻**。实际开战钟点 = WarDate（日期）+ Castle.StartTime（时刻），与 EndWar/SendGuildInfo 里的 `(conquest.WarDate + conquest.Castle.StartTime) - SEnvir.Now`（ConquestWar.cs:99、PlayerObject.cs:5553）一致。

`S.GuildConquestDate { Index, WarTime }` 中 `WarTime == TimeSpan.MinValue` 表示该城堡无本会相关的日程（PlayerObject.cs:5551；Godot 端 OnGuildConquestDate 同样按 MinValue 归零，GameScene.cs:2645）。

### 2. 开战（SEnvir 主循环 → StartConquest，SEnvir.cs:1607-1613 / 2035-2075）

主循环慢速计数里（每天一次的检查窗口）：

```csharp
foreach (CastleInfo info in CastleInfoList.Binding)
{
    if (nextCount.TimeOfDay < info.StartTime) continue;
    if (Now.TimeOfDay > info.StartTime) continue;

    StartConquest(info, false);
}
```

即"上一次计数在 StartTime 之前、这一次已过 StartTime"的跨越时刻触发。`StartConquest`：

```csharp
public static void StartConquest(CastleInfo info, bool forced)
{
    List<GuildInfo> participants = new List<GuildInfo>();

    if (!forced)
    {
        for (int i = UserConquestList.Binding.Count - 1; i >= 0; i--)
        {
            var conquest = UserConquestList.Binding[i];
            if (conquest.Guild == null)
            {
                conquest.Delete();
                continue;
            }

            if (conquest.Castle != info) continue;
            if (conquest.WarDate > Now.Date) continue;

            participants.Add(conquest.Guild);
        }

        if (participants.Count == 0) return;

        foreach (GuildInfo guild in GuildInfoList.Binding)
        {
            if (guild.Castle != info) continue;

            participants.Add(guild);
        }
    }

    ConquestWar War = new ConquestWar
    {
        Castle = info,
        Participants = participants,
        EndTime = Now + info.Duration,
        StartTime = Now.Date + info.StartTime,
    };

    War.StartWar();
}
```

- 参战名单 = 所有 `WarDate <= 今天` 且指向本城堡的申请行会 + **现任城主行会**（守方自动参战，2058-2063）。
- GM 命令 `@StartConquest` 传 `forced=true`：跳过名单收集（Participants 为空列表，所有非守方玩家都算攻方——见 CastleGuard/CastleLord 的 `Participants.Count > 0` 条件）。
- `EndTime = Now + Castle.Duration`——Duration 由 System.db 配置（TimeSpan），开战即锁定结束时刻；CastleLord 死亡时可延长（见 §5）。

### 3. ConquestWar.StartWar（ConquestWar.cs:26-53）

```csharp
public void StartWar()
{
    foreach (SConnection con in SEnvir.Connections)
        con.ReceiveChat(string.Format(con.Language.ConquestStarted, Castle.Name), MessageType.System);   // "{0}攻城战开始了"

    Map = SEnvir.GetMap(Castle.Map);

    for (int i = Map.NPCs.Count - 1; i >= 0; i--)
    {
        NPCObject npc = Map.NPCs[i];
        if (!Castle.ObjectiveRegion.PointList.Contains(npc.CurrentLocation)) continue;

        npc.Visible = false;
        npc.RemoveAllObjects();
    }

    foreach (GuildInfo guild in Participants)
        guild.Conquest?.Delete();

    SEnvir.Broadcast(new S.GuildConquestStarted { Index = Castle.Index });

    PingPlayers();

    SpawnBoss();

    SEnvir.ConquestWars.Add(this);
}
```

- 全服聊天播报开战；**目标区（ObjectiveRegion）内的 NPC 隐藏**（防止攻方被守方 NPC 干扰/堵门）。
- 删除所有参战行会的 `UserConquest` 申请（开打即消费）。
- `S.GuildConquestStarted` 全服广播（客户端据此把日程标为"进行中"）。
- `PingPlayers()`：把城堡地图上所有非守方玩家传送到 `AttackSpawnRegion`（攻方出生区）——即**入口/洗人控制**。
- `SpawnBoss()`：在目标区生成 CastleLord 或 CastleFlag（按 `Castle.Monster.AI`）。

### 4. PingPlayers / SpawnBoss / Process（ConquestWar.cs:122-172, 55-60）

```csharp
public void PingPlayers()
{
    foreach (PlayerObject player in Map.Players)
    {
        //if (!Castle.CastleRegion.PointList.Contains(player.CurrentLocation)) continue;

        if (player.Character.Account.GuildMember?.Guild?.Castle == Castle) continue;

        player.Teleport(Castle.AttackSpawnRegion, null, 0);
    }
}
```

注意 CastleRegion 判定被注释掉了——**只要在城堡所在地图、且不属于守方行会，一律传送到攻方出生区**（开战、结束、Lord 死后三个时机执行）。战争期间玩家可以再走进城堡区，不会被持续踢出（没有逐帧区域检查）。

```csharp
public void SpawnBoss()
{
    if (Castle.Monster != null)
    {
        switch (Castle.Monster.AI)
        {
            case 1000: //CastleLord
                CastleTarget = new CastleLord
                {
                    MonsterInfo = Castle.Monster,
                    War = this,
                    Castle = Castle
                };

                CastleTarget.Spawn(Castle.ObjectiveRegion, null, 0);
                break;
            case 1001: //CastleFlag
                CastleTarget = new CastleFlag
                {
                    MonsterInfo = Castle.Monster,
                    War = this,
                    Castle = Castle
                };

                CastleTarget.Spawn(Castle.ObjectiveRegion, null, 0);
                break;
        }
    }
}
```

```csharp
public void Process()
{
    if (SEnvir.Now < EndTime) return;

    EndWar();
}
```

Process 由主循环每帧调用（SEnvir.cs:1575-1576）；到点即 EndWar。

### 5. 占领判定一：CastleLord（CastleLord.cs:255-303）

CastleLord 是可移动的近战 Boss（攻击：1/3 概率 3 格直线 `LineAttack`，否则正面单体；血量过半后每 15 秒对视野目标放 `DeathCloud`，CastleLord.cs:31-104）。**每次受击只扣 1 点血**——它的血量其实是"被打次数"计数器：

```csharp
// CastleLord.cs:143-188（节选）
public override int Attacked(MapObject attacker, int power, Element element, ...)
{
    if (attacker == null || attacker.Race != ObjectType.Player) return 0;

    PlayerObject player = (PlayerObject)attacker;

    if (War == null) return 0;

    if (player.Character.Account.GuildMember == null) return 0;

    if (player.Character.Account.GuildMember.Guild.Castle != null) return 0;      // 已有城堡的行会(守方)打不动

    if (War.Participants.Count > 0 && !War.Participants.Contains(player.Character.Account.GuildMember.Guild)) return 0;  // 非参战方打不动

    int result = base.Attacked(attacker, 1, element, canReflect, ignoreShield, canCrit);   // 注意 power 固定为 1

    // Conquest Stats: 攻击者 BossDamageDealt += result（宠物伤害记到主人头上）
    ...
}
```

死亡结算：

```csharp
public override void Die()
{
    if (War != null)
    {
        if (EXPOwner?.Node == null) return;                                          // 无归属则不判占领
        if (EXPOwner.Character.Account.GuildMember == null) return;
        if (EXPOwner.Character.Account.GuildMember.Guild.Castle != null) return;     // 守方不能拿
        if (War.Participants.Count > 0 && !War.Participants.Contains(EXPOwner.Character.Account.GuildMember.Guild)) return;

        #region Conquest Stats
        UserConquestStats conquest = SEnvir.GetConquestStats((PlayerObject)EXPOwner);
        if (conquest != null)
            conquest.BossKillCount++;
        #endregion

        GuildInfo ownerGuild = SEnvir.GuildInfoList.Binding.FirstOrDefault(x => x.Castle == War.Castle);

        if (ownerGuild != null)
            ownerGuild.Castle = null;

        EXPOwner.Character.Account.GuildMember.Guild.Castle = War.Castle;

        foreach (SConnection con in SEnvir.Connections)
            con.ReceiveChat(string.Format(con.Language.ConquestCapture, EXPOwner.Character.Account.GuildMember.Guild.GuildName, War.Castle.Name), MessageType.System);   // "{0}占领了{1}"

        SEnvir.Broadcast(new S.GuildCastleInfo { Index = War.Castle.Index, Owner = EXPOwner.Character.Account.GuildMember.Guild.GuildName });

        War.CastleTarget = null;

        War.PingPlayers();
        War.SpawnBoss();

        if (War.EndTime - SEnvir.Now < TimeSpan.FromMinutes(15))
            War.EndTime = SEnvir.Now.AddMinutes(15);

        foreach (PlayerObject player in SEnvir.Players)
            player.ApplyCastleBuff();

        War = null;
    }

    base.Die();
}
```

**EXPOwner（尾刀/最高仇恨归属者）所在行会夺城**；旧城主 `Castle = null`、新城主 `Castle = War.Castle`，全服广播 `S.GuildCastleInfo { Index, Owner }`。随后：洗一次人 → **立即重生 Boss**（下一任守方继续守）→ **保底 15 分钟**：若距 EndTime 不足 15 分钟则延长到 15 分钟（攻方刚夺城至少有 15 分钟防守窗口）→ 全服玩家重算 Castle buff。

### 6. 占领判定二：CastleFlag（CastleFlag.cs:14-206）

旗帜不可移动、免伤（`Attacked` 直接 return 0，CastleFlag.cs:108-111）、免疫毒（208-211）；它把自己当"目标探测器"用——`ShouldAttackTarget` 只认"非守方行会玩家"（224-255），`Attack()` 即读条逻辑：

```csharp
// CastleFlag.cs:20-21
private static int _takeDuration = 30;
public TimeSpan ContesterDelay = TimeSpan.FromSeconds(_takeDuration);
```

```csharp
// CastleFlag.cs:113-206（Attack 节选）
protected override void Attack()
{
    UpdateAttackTime();

    var target = (PlayerObject)Target;

    if (target.Character.Account.GuildMember == null) return;
    if (target.Character.Account.GuildMember.Guild.Castle != null) return;          // 守方摸旗无效
    if (War.Participants.Count > 0 && !War.Participants.Contains(target.Character.Account.GuildMember.Guild)) return;

    if (Contester == null)
    {
        Contester = target.Character.Account.GuildMember.Guild;

        //Start 30 seconds timer
        ContesterTime = SEnvir.Now.Add(ContesterDelay);

        foreach (SConnection con in SEnvir.Connections)
            con.ReceiveChat(string.Format(con.Language.ConquestTakingFlag, Contester.GuildName, War.Castle.Name, _takeDuration), MessageType.System);

        return;
    }
    else
    {
        bool contestGuildNear = false;

        //check if any other guild nearby
        foreach (PlayerObject player in CurrentMap.Players)
        {
            int distance;

            distance = Functions.Distance(player.CurrentLocation, CurrentLocation);

            if (distance > ViewRange) continue;

            if (player.Character.Account.GuildMember == null) continue;
            if (War.Participants.Count > 0 && !War.Participants.Contains(player.Character.Account.GuildMember.Guild)) return;

            //if guild near, add to timer
            if (player.Character.Account.GuildMember.Guild != Contester)
            {
                //Another guild near flag - reset contest time
                ContesterTime = SEnvir.Now.Add(ContesterDelay);
                ...  // "另一行会正在阻止 {Contester} 占旗"
                return;
            }
            else
            {
                contestGuildNear = true;
            }
        }

        if (!contestGuildNear)
        {
            // Contester 全员离开视野 → 读条作废
            Contester = null;
            ContesterTime = DateTime.MaxValue;
            return;
        }
        else
        {
            var difference = (ContesterTime - SEnvir.Now).Seconds;
            ...  // "还剩 {difference} 秒"
        }
    }

    if (ContesterTime > SEnvir.Now) return;

    //Remove current guild from castle
    GuildInfo ownerGuild = SEnvir.GuildInfoList.Binding.FirstOrDefault(x => x.Castle == War.Castle);

    if (ownerGuild != null)
        ownerGuild.Castle = null;

    //Update new guild with castle
    Contester.Castle = War.Castle;

    foreach (SConnection con in SEnvir.Connections)
        con.ReceiveChat(string.Format(con.Language.ConquestCapture, Contester.GuildName, War.Castle.Name), MessageType.System);

    SEnvir.Broadcast(new S.GuildCastleInfo { Index = War.Castle.Index, Owner = Contester.GuildName });

    Contester = null;
}
```

规则：攻方行会持续待在旗帜 ViewRange 内 **30 秒**且期间无其他参战行会靠近 → 夺城（旧城主被摘、广播同 CastleLord）。有敌对参战行会进入视野就**重置计时**；Contester 行会全部离开视野则作废（Process 66-81 也会在失去目标时作废并播报）。

旗帜同时是"城主行会徽记"的展示物：`Refresh()` 把 `_flag/_colour` 设为当前归属行会的 Flag/Colour 并重下发（83-95），`GetInfoPacket` 通过 `Extra1 = _flag, Colour = _colour` 带给客户端（285-304）。会长改行会旗/色后 `GuildFlag/GuildColour`（PlayerObject.cs:5163-5167、5189-5193）会调用 `map.RefreshFlags()`（Map.cs:209-215）实时换旗。

### 7. 结束（ConquestWar.EndWar，ConquestWar.cs:63-120）

```csharp
public void EndWar()
{
    foreach (SConnection con in SEnvir.Connections)
        con.ReceiveChat(string.Format(con.Language.ConquestFinished, Castle.Name), MessageType.System);   // "{0}攻城战结束了"

    Ended = true;

    for (int i = Map.NPCs.Count - 1; i >= 0; i--)
    {
        NPCObject npc = Map.NPCs[i];
        if (!Castle.ObjectiveRegion.PointList.Contains(npc.CurrentLocation)) continue;

        npc.Visible = true;
        npc.AddAllObjects();
    }

    PingPlayers();

    DespawnBoss();

    SEnvir.ConquestWars.Remove(this);

    SEnvir.Broadcast(new S.GuildConquestFinished { Index = Castle.Index });

    GuildInfo ownerGuild = SEnvir.GuildInfoList.Binding.FirstOrDefault(x => x.Castle == Castle);

    if (ownerGuild != null)
    {
        foreach (SConnection con in SEnvir.Connections)
            con.ReceiveChat(string.Format(con.Language.ConquestOwner, ownerGuild.GuildName, Castle.Name), MessageType.System);   // "{0}是{1}的占领者"

        UserConquest conquest = SEnvir.UserConquestList.Binding.FirstOrDefault(x => x.Castle == Castle && x.Castle == ownerGuild?.Castle);

        TimeSpan warTime = TimeSpan.MinValue;

        if (conquest != null)
            warTime = (conquest.WarDate + conquest.Castle.StartTime) - SEnvir.Now;

        foreach (GuildMemberInfo member in ownerGuild.Members)
        {
            if (member.Account.Connection?.Player == null) continue; //Offline

            member.Account.Connection.Enqueue(new S.GuildConquestDate { Index = Castle.Index, WarTime = warTime, ObserverPacket = false });
        }
    }

    foreach (GuildInfo participant in Participants)
    {
        if (participant == ownerGuild) continue;

        foreach (GuildMemberInfo member in participant.Members)
        {
            if (member.Account.Connection?.Player == null) continue; //Offline

            member.Account.Connection.Enqueue(new S.GuildConquestDate { Index = Castle.Index, WarTime = TimeSpan.MinValue, ObserverPacket = false });
        }
    }
}
```

**结算语义**：战争到时后，城堡归属就是当时 `Guild.Castle == Castle` 的行会（占领在战中即时生效，EndWar 只做收尾）；恢复目标区 NPC、回收 Boss、移出 ConquestWars、广播结束包；城主行会成员收到下一场针对本城的日程（若有新申请），败方成员收到 `WarTime = TimeSpan.MinValue`（日程清零）。`DespawnBoss()`（134-143）把 CastleTarget `EXPOwner=null; War=null; Die(); Despawn()`。**没有金币/税收结算**：城主收益是常驻的 Castle buff（见 §9）与行会税抽成（见 social/guild.md，与城堡无直接绑定）。

### 8. 攻城对象：城门与守卫

#### CastleGate（AI 1002，CastleGate.cs:12-278）

- 构造即 `Closed = true`（31-37）；`Blocking => Closed && base.Blocking`（29）——只有关门时挡路；`CanMove/CanAttack` 恒 false（25-27）；**不回血**（`ProcessRegen()` 空，61-64）。
- 出生时按 `MonsterInfo.FaceImage`（1=西门/2=南门/3=东门朝向）生成 5 个 `MonsterFlag.Blocker` 隐形阻挡物补齐墙体缺口（66-131，BlockArray 坐标 71-99）；`ActiveDoorWall(closed)` 随门开关 Show/Hide 阻挡物（266-276）。
- **平时自动门**：`ProcessSearch`（153-177）——非战争期（`War == null`）城主行会成员走近 4 格自动开门，10 秒后无人再自动关门（ProcessAI 133-151）。**战争期间停止自动开门**（161 判 `War == null`），城主可用 `GuildToggleCastleGates` 手动开关（PlayerObject.cs:5201-5242，无权限检查但有 Castle 归属检查，开关播报 GuildGateClosed/GuildGateOpened）。
- **只有关门状态可被打**（`Attacked` 181 行 `if (!Closed) return 0;`；TODO 注释表明"仅攻方行会可打"未实现，任何人都能拆）；伤害档位公式：

```csharp
// CastleGate.cs:246-253
protected int GetDamageLevel()
{
    int level = (int)Math.Round((double)(3 * CurrentHP) / Stats[Stat.Health]);

    if (level < 1) level = 1;

    return level;
}

// CastleGate.cs:255-264  档位映射到 Direction，客户端按朝向换贴图（3=完好 → 1=残血）
public void CheckDirection()
{
    MirDirection newDirection = (MirDirection)(3 - GetDamageLevel());

    if (newDirection != Direction)
    {
        Direction = newDirection;
        Broadcast(new S.ObjectTurn { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
    }
}
```

- 被拆：`Die()` 里 `ActiveDoorWall(false)` 放行 + `DeadTime = SEnvir.Now.AddYears(1)`（196-203）——**不会自然重生**，必须花钱修。`RepairGate()`（230-244）复活并满血。

#### 城门/守卫修理（PlayerObject.cs:5244-5313 / 5315-5384）

```csharp
// GuildRepairCastleGates（5264-5282）修理费公式
int cost = 0;

foreach (var gate in castle.Gates)
{
    if (gate.RepairCost <= 0) continue;

    var mob = map.CastleGates.FirstOrDefault(x => x.GateInfo == gate);

    if (mob == null || mob.Dead)
    {
        cost += gate.RepairCost;                       // 已死亡：全额
    }
    else
    {
        var percent = Math.Abs(mob.CurrentHP) * 100 / mob.Stats[Stat.Health];

        cost += (gate.RepairCost * percent / 100);     // 存活：按剩余血量百分比
    }
}

if (cost > Character.Account.GuildMember.Guild.GuildFunds)
{ ... return; }

Character.Account.GuildMember.Guild.GuildFunds -= cost;
Character.Account.GuildMember.Guild.DailyGrowth -= cost;
```

注意：存活门的费用按**剩余血量百分比**计（血越少越便宜、满血门修理收全款），与直觉的"按损毁程度收费"相反——疑似原版笔误（应为 `100 - percent`），移植时按目标设计自行取舍，但要意识到 Zircon 原服行为如此。守卫版（5335-5353）公式完全相同。付款后对每扇门 `RepairGate()`（死者原地复活 `S.ObjectRevive`）或补生成；修理需 `GuildPermission.Leader`。

#### CastleGuard（AI 1003，CastleGuard.cs:12-169）

- 定点远程箭塔：`CanMove => false`（16）、`AttackRange = 15`（18）、不可移动（MoveTo 抛 NotSupportedException，49-52）。
- 目标选择：**只在攻城期间活动**（`War == null` 一律 false，56/88 行），只打"非本城城主行会"的玩家（`player.Character.Account.GuildMember?.Guild.Castle != War.Castle`，81/104）。
- 开火条件（Attack 110-129）：目标有行会、目标行会无城堡、（有参战名单时）目标是参战行会：

```csharp
protected override void Attack()
{
    var target = (PlayerObject)Target;

    if (target.Character.Account.GuildMember?.Guild.Castle != null) return;

    if (War.Participants.Count > 0 && !War.Participants.Contains(target.Character.Account.GuildMember.Guild)) return;

    Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Targets = new List<uint> { Target.ObjectID } });

    UpdateAttackTime();

    ActionList.Add(new DelayedAction(
                       SEnvir.Now.AddMilliseconds(400 + Functions.Distance(CurrentLocation, Target.CurrentLocation) * Globals.ProjectileSpeed),
                       ActionType.DelayAttack,
                       Target,
                       GetDC(),
                       AttackElement));
}
```

（弹道延迟 = 400ms + 距离 × ProjectileSpeed，箭矢飞行时间。）

- 受击（Attacked 131-146）：只吃**玩家**、非本方（`attackerGuild == Guild` 拒绝——Guild 是守方行会）、且是参战攻方的伤害；`War == null` 时无敌。
- 死亡后同样不回血不重生（`ProcessRegen` 未覆写但 MonsterObject 通用回血……实际覆写于 148-158 的 `RepairGuard` 只在修理时调用）；靠行会资金复活。
- 服务器启动时 `Map.Setup() → CreateCastleGuards()`（Map.cs:164-184）按 `Castle.Guards` 配置全量生成；城门同理（143-163），已存在的（如修理后复活的）走 `RepairGate/RepairGuard` 满血。

### 9. 攻城期规则汇总

1. **PK 豁免**：`AtWar()`（PlayerObject.cs:5499-5521）——当前地图存在 ConquestWar 时，无行会者对所有人开战、有行会者对异会者开战。效果：攻击不棕名（16631-16634 提前 return）、WarRedBrown 攻击模式放行（16014-16016）、死亡只发行会战播报不计红名（16344-16358）。**离开攻城地图即恢复正常 PK 规则。**
2. **入口控制**：开战/结束/Lord 死亡三个时机 `PingPlayers()` 把地图上非守方玩家传到 `AttackSpawnRegion`；战中没有持续区域驱逐。
3. **怪物刷新**：守卫不自动重生，城主行会战后花钱修（RepairCastleGuards）；CastleTarget Boss 在每次占领后由 `War.SpawnBoss()` 立即重生；EndWar 时 `DespawnBoss()` 移除。城门城墙由 `Map.Setup()` 在启动时生成。
4. **行会/全服公告**：开战/结束/占领/读条/阻止读条全部 `foreach (SConnection con in SEnvir.Connections) con.ReceiveChat(...)` 全服播报（中文文案见 ChineseMessages.cs:60-67）；守方专属提醒（GuildGateOpened/Closed、被宣战 GuildConquestSuccess）只发行会成员。
5. **攻城经济**：宣战物品（`CastleInfo.Item`）；城门/守卫修理费（GuildFunds）；攻城地图上玩家的 HuntGold buff 每 tick 直接 +1 余额不设上限累积（MapObject.cs:568-578）。
6. **城主福利**：`ApplyCastleBuff()`（PlayerObject.cs:9355-9368）——

```csharp
public void ApplyCastleBuff()
{
    BuffRemove(BuffType.Castle);

    if (Character.Account.GuildMember?.Guild.Castle == null) return;

    Stats stats = new Stats();

    stats[Stat.ExperienceRate] += 10;
    stats[Stat.DropRate] += 10;
    stats[Stat.GoldRate] += 10;

    BuffAdd(BuffType.Castle, TimeSpan.MaxValue, stats, false, false, TimeSpan.Zero);
}
```

   触发点：登录（PlayerObject.cs:1145）、入会/退会/踢人（5453-5454、5495-5496、5592-5593）、CastleLord 死亡夺城时**全服玩家**刷新（CastleLord.cs:296-297）。`CastleInfo.Discount` 字段（CastleInfo.cs:145-158）全库无读取点，NPC 折扣未实装。
7. **战报统计**：`ConquestWar.GetStat`（174-196）懒创建 `UserConquestStats`（记录开战时间/城堡/行会/角色快照）；伤害/击杀在 `PlayerObject.Attacked/Death`（15852-15880、16241-16268）、`MapObject.ProcessPoison`（MapObject.cs:307-360）、`CastleLord.Attacked/Die`（164-178、269-272）等处累积 Boss/PvP 分类统计。

### 10. 与沙巴克地图的关系

- `CastleInfo.Map`（CastleInfo.cs:25）指向沙巴克地图的 MapInfo；`MapInfo.Castles`（MapInfo.cs:519-520）是反向关联。`CastleRegion`（城堡本体区域，用于城主开关门遍历）、`ObjectiveRegion`（宫殿/目标区，Boss 出生点 + NPC 隐藏区，缺省回落 CastleRegion，171-179）、`AttackSpawnRegion`（攻方出生区）都是该地图上的 MapRegion。
- 服务器启动时 `Map.Setup()`（Map.cs:91-102）按城堡配置生成 `CastleFlags/CastleGates/CastleGuards` 三个地图级列表（34-36），之后 `SEnvir.GetMap(Castle.Map)` 直接取用。
- 本仓沙巴克地图资源结论（来自仓内审计文档，非本次源码读取）：`docs/Sabak_Map_Migration_Audit_2026-08-11.md` 记录沙巴克 3 号图保留 **Zircon 原版** `.map` 资源与 `Sabak.Zl` 图库移植；`docs/MINIMAP_EI_MIGRATION.md:87-93` 记录 3 号图小地图不使用 EI 迁移表、`MiniMap.Zl` frame 7 替换为 Zircon 原版 Sabuk Keep 小地图（800×600）；`docs/BGM_AUDIT_AND_BACKFILL_2026-08-13.md:36,90-91` 记录沙巴克/行会 BGM 使用 B200 曲组；`docs/EI_ALIGNMENT_2026-08-11.md:103` 明确"沙巴克图 3 不换，保持 Zircon 原版"。

## 数据结构/协议细节

### 攻城相关包（LibraryCore/Network）

| 包 | 方向 | 字段 | 用途 |
|---|---|---|---|
| C.GuildRequestConquest（ClientPackets.cs:599-602） | C→S | Index | 城堡宣战申请（→ PlayerObject.GuildConquest） |
| C.GuildToggleCastleGates / GuildRepairCastleGates / GuildRepairCastleGuards（614-627） | C→S | — | 城门开关/修理（SConnection.cs:1273-1290 分发） |
| S.GuildConquestDate（ServerPackets.cs:1124-1127） | S→C | Index, WarTime | 城堡开战倒计时（`TimeSpan.MinValue`=无日程）；宣战成功/结束结算/登录全量（PlayerObject.cs:5546-5556）均发 |
| S.GuildConquestStarted（1146-1149） | S→C | Index | 开战全服广播；客户端把日程标为"进行中" |
| S.GuildConquestFinished（1151-1154） | S→C | Index | 结束全服广播；日程清零 |
| S.GuildCastleInfo（1140-1143） | S→C | Index, Owner | 城主变更（Lord 死/旗读条完成）；客户端维护 CastleOwners 映射 |

### MonsterInfo.AI 分派（MonsterObject.cs:638-643 + ConquestWar.cs:148-170）

| AI | 类 | 生成方式 |
|---|---|---|
| 1000 | CastleLord | ConquestWar.SpawnBoss 直接 `new CastleLord`（Map.Setup 不生成） |
| 1001 | CastleFlag | GetMonster 映射；Map.Setup 生成城徽旗 + SpawnBoss 生成目标旗（同一个类，目标旗带 War） |
| 1002 | CastleGate | GetMonster 映射；Map.Setup / GuildRepairCastleGates 生成 |
| 1003 | CastleGuard | GetMonster 映射；Map.Setup / GuildRepairCastleGuards 生成 |

`MonsterFlag`：`CastleObjective = 10`、`CastleDefense = 11`、`Blocker = 20`（Enum.cs:2157-2160）——Blocker 即城门隐形墙。

### ConquestWar 运行时字段（ConquestWar.cs:15-24）

```csharp
public DateTime StartTime, EndTime;
public CastleInfo Castle;

public List<GuildInfo> Participants;
public Map Map;

public CastleObject CastleTarget;
public bool Ended;

public Dictionary<CharacterInfo, UserConquestStats> Stats = new Dictionary<CharacterInfo, UserConquestStats>();
```

守方判定：全库统一用 `guild.Castle == castle`（GuildInfo.Castle 持有即守方）；攻方判定：`War.Participants.Contains(guild)`（宣战申请行会；forced 开战时 Participants 为空 → 所有非守方行会都是有效攻方）。

## GodotClient 现状

（以下结论均基于本次对 GodotClient/ 与 docs/ 的 glob/grep 实际检索）

| 功能 | 状态 | 依据 |
|---|---|---|
| 协议层：宣战/城主/日程/开战/结束 C&S 包 | **已移植** | GodotClient/Network/ServerConnection.cs:1096-1099（SendGuildWar/ToggleCastleGates/RepairCastleGates/RepairCastleGuards）、1105（SendGuildRequestConquest）、256-259/566-569（GuildCastleInfo/ConquestDate/ConquestStarted/ConquestFinished 事件与 Process） |
| 城主与开战日程状态维护 | **已移植** | GodotClient/Scripts/GameScene.cs:41-42（`CastleOwners`、`GetCastleWarDate`）、2636-2659（OnGuildCastleInfo/OnGuildConquestDate/OnGuildConquestStarted/OnGuildConquestFinished，含 `WarTime == TimeSpan.MinValue → DateTime.MinValue`、倒计时换算 `DateTime.Now + packet.WarTime`） |
| 行会窗体"战争页"（城堡列表：城主/日程/宣战申请按钮） | **已移植** | GodotClient/Controls/GuildDialog.cs:335-368（BuildWarPage：遍历 `Globals.MapInfoList → map.Castles`，显示 `{0}\nLord: {1}    Siege: {2}`，按钮 SendGuildRequestConquest/ToggleCastleGates/RepairCastleGates/RepairCastleGuards） |
| 行会窗体"城堡页"（城主维护：开关门/修门/修守卫） | **已移植** | GuildDialog.cs:497-517（BuildCastlePage，修理带 ConfirmDialog 确认弹窗） |
| 城堡页可见性（仅城主行会显示） | **已移植** | GuildDialog.cs:84（`if (_guild == null && page > 0) return;`）+ 战争页常驻；对照原版 Client/Scenes/Views/GuildDialog.cs:415（CastleTab 仅城主显示） |
| 登录补发攻城进行中状态 | **已移植**（协议侧自动） | 服务端 PlayerObject.cs:1169-1171/1285-1287 发 S.GuildConquestStarted；Godot GameScene 2654-2659 处理 |
| 沙巴克城门渲染（SabukGate 四朝向 Mon_54） | **已移植** | GodotClient/Formats/MonsterLookup.cs:304-308（`SabukGateSouth/North/East/West → (Mon_54, 0/1/2/3)`）；GodotClient/Scripts/ObjectRenderer.cs:411-416（SabukGate* 走特殊分支）；LibraryCore/FrameSet.cs:1063-1066（SabukGate Standing/Struck 帧） |
| 城堡旗帜渲染（行会旗号+旗色染色） | **已移植** | MonsterLookup.cs:12/304（CastleFlag 用 `LibraryFile.CastleFlag` 专库）；ObjectRenderer.cs:355-356（CastleFlag DrawOverlay）；GuildDialog.cs:301-305（预览用 `Index = Flag*100` 双层染色） |
| 守卫/防御怪隐藏逻辑（CastleDefense 朝向 UpLeft 跳过） | **已移植** | GameScene.cs:4663-4666 |
| Castle/Guild buff 图标与名称 | **已移植** | GodotClient/Controls/BuffDialog.cs:130（Castle→"Castle Lord"）、149（图标 242）、165（Guild 图标 140） |
| 沙巴克地图本体（3 号图 + 图库 + 小地图 + BGM） | **已迁移（资源侧）** | docs/Sabak_Map_Migration_Audit_2026-08-11.md（Sabak.Zl 移植与贴图偏移审计）；docs/MINIMAP_EI_MIGRATION.md:87-93（小地图 frame 7 = Zircon Sabuk Keep）；docs/BGM_AUDIT_AND_BACKFILL_2026-08-13.md:36,90-91（B200 沙巴克曲）；docs/EI_ALIGNMENT_2026-08-11.md:103（图 3 保持 Zircon 原版） |
| 攻城战报统计（UserConquestStats）展示 UI | **未移植** | GodotClient 全库 grep 无 UserConquestStats/ConquestStats 引用（检索 Conquest|Sabuk|Castle 仅命中上述文件）；原版 WinForms 客户端亦未见对应面板（未找到实现，疑为服务端统计数据供 Web/查询用） |
| 攻城地图内战况 HUD（如"占领读条/剩余时间"） | **未移植（原版也无专用 HUD）** | 战况全靠聊天播报（ConquestTakingFlag 等全服 ReceiveChat）；Godot ChatLogPanel 已能显示 System 频道 |
| 攻方出生区/城门开关的网络表现 | **已移植（表现层）** | 城门开关靠 S.ObjectAttack/ObjectRangeAttack 广播（CastleGate.cs:213/225），Godot ObjectRenderer 已处理 SabukGate 动画 |

## 移植注意事项

1. **占领是"战中即时生效"**：CastleLord 尾刀/旗帜读条完成那一刻 `Guild.Castle` 就换主 + `S.GuildCastleInfo` 广播，EndWar 不再判定归属。Godot 端任何"城主"显示（名字染色、城堡页按钮、旗帜）都应订阅 `S.GuildCastleInfo` 即时更新，而不是等结束包。
2. **EXPOwner 语义要移植对**：Lord 的占领判给 `EXPOwner`（最后归属者，非必然尾刀者），且守方/非参战方当 EXPOwner 时**直接 return 不死不换主**（CastleLord.cs:259-265 提前 return 连 base.Die() 都不执行）——Godot 端做 Boss 血条时注意它可能"打到 0 血不死"。
3. **CastleLord 血量=受击次数**：`base.Attacked(attacker, 1, ...)` 每击固定 1 伤害（CastleLord.cs:157），MonsterInfo 里配的血量就是"需要被打多少下"。客户端不要按普通怪物的 DC 期望估算拆 Boss 时间。
4. **15 分钟保底**：占领后 `War.EndTime = max(EndTime, Now+15min)`（CastleLord.cs:293-294），但 `StartTime` 不变——Godot 端若自建倒计时，收到占领播报后要允许结束时间被服务端延长（以 S.GuildConquestFinished 为准收尾）。
5. **旗帜双读条互相干扰**：30 秒读条被"其他参战行会进入 ViewRange"重置、被"Contester 全员离开视野"作废（CastleFlag.cs:151-179）。ViewRange 是 MonsterInfo 配置的视野，移植时 Godot 端提示文案需要服务端聊天播报驱动（原版就这么做）。
6. **城门自动门逻辑在服务端**（ProcessSearch/ProcessAI），客户端只吃 `S.ObjectAttack/S.ObjectRangeAttack/S.ObjectTurn` 表现开/关/损伤档位；损伤档位通过 Direction（3→1）编码，Godot ObjectRenderer 的 SabukGate 帧映射（Mon_54 shape 0-3）必须与之对齐。
7. **修理费公式的"反直觉"**：存活建筑按剩余血量百分比收费（越残越便宜），满血修理=全款（PlayerObject.cs:5278-5281）。若在 Godot 端做修理确认弹窗的预估费用，需复刻同一公式（含 `Math.Abs(CurrentHP)`，门可能负血）。
8. **Participants.Count == 0 的含义**：GM forced 开战或配置无申请时，`Participants.Count > 0` 的守门条件全部放行——任何非守方行会都能打 Lord/摸旗/拆门。移植测试时可用 `@StartConquest` 命令（StartConquest.cs:26）快速起战。
9. **攻城期间 NPC 隐藏**是 ObjectiveRegion 级别（ConquestWar.cs:34-41/70-77），包括商店/传送 NPC；Godot 端不需要特殊处理（NPC 不可见即不下发），但要理解攻城期"进不了宫殿买药"是原版行为。
10. **城堡 Discount 字段是死配置**（CastleInfo.cs:145-158 全库无读取），Godot 端不要按"城主购物打折"做 UI；实际城主福利只有 Castle buff +10 与行会税。
11. **与行会系统的耦合点**：`GuildInfo.Castle/Conquest`（GuildInfo.cs:213-242）是攻城与行会两系统的唯一桥；行会解散 `OnDeleted → Castle = null`（340-345）即无主城。查文档 social/guild.md 可补全行会侧上下文。
12. **沙巴克地图资源已定为 Zircon 原版**（docs/EI_ALIGNMENT_2026-08-11.md:103），坐标/区域（CastleRegion/ObjectiveRegion/AttackSpawnRegion）配置在 System.db 的 MapRegion 表，移植攻城时先在 GodotClient 里核对这三块区域与 3 号图实际布局的一致性（docs/Sabak_Map_Migration_Audit_2026-08-11.md 记录过贴图索引偏移问题，区域点集同理需要核对）。
