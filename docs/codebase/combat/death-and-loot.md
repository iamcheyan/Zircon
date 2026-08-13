# 死亡、掉落、PK 惩罚与复活（death-and-loot）

## TL;DR 速查表

| 结论 | 出处 |
|---|---|
| HP 归零先检查 BuffType.CelestialLight（免死），再检查 Stat.ItemReviveTime（装备自动复活），最后才 Die() | `ServerLibrary/Models/MapObject.cs:1229-1243` |
| 玩家死亡后 `RevivalTime = Now + 10 分钟`（Config.AutoReviveDelay），到期自动 TownRevive 回城 | `ServerLibrary/Models/PlayerObject.cs:16210`、`ServerLibrary/Models/PlayerObject.cs:340-341` |
| 回城复活 C.TownRevive **无冷却校验**（只查 `!Dead`），原版客户端死亡后在聊天框出可点击标签 | `ServerLibrary/Models/PlayerObject.cs:1443-1445`、`Client/Scenes/Views/ChatTab.cs:429-443` |
| 背包掉落：每格 10% 概率（`Next(10) > 0` 跳过）；伴侣背包 1/7；装备 1%（挂机打金号 10%） | `ServerLibrary/Models/PlayerObject.cs:16436`、`:16487`、`:16530` |
| 免掉标记：`Info.CanDeathDrop == false`、`UserItemFlags.Bound / Marriage / Worthless` 直接跳过 | `ServerLibrary/Models/PlayerObject.cs:16432-16435` |
| 杀白名（PKPoint < 200 且非 Brown）者 +50 PK 点（Config.PKPointRate）；PK 点每 60 秒自然 -1 | `ServerLibrary/Models/PlayerObject.cs:16388`、`ServerLibrary/Models/MapObject.cs:546` |
| 红名线 RedPoint=200：红名被怪杀死时全身装备各损失 `Durability/10` 耐久；守卫主动攻击红名 | `ServerLibrary/Models/PlayerObject.cs:16400-16409`、`ServerLibrary/Models/Monsters/Guard.cs:60` |
| 组队经验：`exp += exp * 0.06M * 成员数`，再按 `player.Level / totalLevels` 加权分配；四职业齐加成最高 eRate×1.5 / dRate×1.3 | `ServerLibrary/Models/MonsterObject.cs:2661`、`:2665`、`:2608-2632` |
| 复活术成功率 `Next(100) > 25 + Level*25` 则失败；成功按 `Magic.GetPower()`% 恢复 HP/MP；**无经验惩罚** | `ServerLibrary/Models/Magics/Taoist/Resurrection.cs:101-107` |
| GodotClient：死亡动画/复活包/计时器/红棕名色均已移植；**死亡后点击复活 UI 未做**（SendTownRevive 无调用方） | `GodotClient/Scripts/GameScene.cs:7460`、`:437` |

## 职责概述

本文覆盖服务端"单位死亡"完整链路：死亡判定与入口（`MapObject.SetHP/ChangeHP`）、玩家死亡副作用（`PlayerObject.Die`：仇恨清除、宠物/法术清场、Rebirth 特殊处理、PVP 判责、PK 点与诅咒、死亡掉落）、PK 惩罚体系（Brown 灰名、PKPoint 红名、PvPCurse 诅咒、衰减与清洗道具）、怪物死亡的奖励发放框架（`MonsterObject.Die → YieldReward`，掉落明细另见 `docs/codebase/item/drops.md`）、经验分配公式（单人/组队）、全部复活途径（自动回城、点击回城、复活术、轮回丹、装备自动复活、招魂术拉尸）。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| ServerLibrary/Models/MapObject.cs | 1665-1684 | `Die()` 基类：置 Dead、移除特定 buff、清毒素、广播 `S.ObjectDied` |
| ServerLibrary/Models/MapObject.cs | 1223-1284 | `SetHP/ChangeHP`：HP≤0 时的免死检查（CelestialLight → ItemRevive → Die） |
| ServerLibrary/Models/MapObject.cs | 1318-1323 | `ItemRevive()`：装备自动复活，恢复满 HP/MP 并进入冷却 |
| ServerLibrary/Models/MapObject.cs | 537-557 | PK 点 buff 的定时衰减：每 `PKPointTickRate`（60s）`Stat.PKPoint--` |
| ServerLibrary/Models/MapObject.cs | 1726-1730 | `InGroup(ob)`：共享同一 `GroupMembers` 引用即同队 |
| ServerLibrary/Models/PlayerObject.cs | 16208-16423 | `PlayerObject.Die()`：玩家死亡全流程（见下文核心流程） |
| ServerLibrary/Models/PlayerObject.cs | 16425-16584 | `DeathDrop()`：玩家死亡掉落（背包/伴侣背包/装备三段） |
| ServerLibrary/Models/PlayerObject.cs | 1443-1464 | `TownRevive()`：回城复活（随机绑定点，满血） |
| ServerLibrary/Models/PlayerObject.cs | 340-341 | 主循环：`Dead && Now >= RevivalTime` 时自动回城 |
| ServerLibrary/Models/PlayerObject.cs | 2036-2108 | `GainExperience()`：经验入账倍率与升级 |
| ServerLibrary/Models/PlayerObject.cs | 16595-16635 | `CheckBrown()`：攻击白名玩家时给自己加灰名 |
| ServerLibrary/Models/PlayerObject.cs | 16637-16655 | `IncreasePKPoints()`：累加 PK 点 + 红名换绑定点 |
| ServerLibrary/Models/PlayerObject.cs | 761-775 | `ProcessNameColour()`：红/黄/棕/粉名颜色 |
| ServerLibrary/Models/PlayerObject.cs | 6641-6653 | 轮回丹（Reincarnation Pill）：死亡状态下原地满血复活 |
| ServerLibrary/Models/PlayerObject.cs | 6523-6538 | 忏悔药水（Potion of Repentence）：按道具 PKPoint 值削减 PK 点 |
| ServerLibrary/Models/PlayerObject.cs | 6539-6559 | 赎罪基石（Redemption Key Stone）：Redemption buff + 诅咒减半 |
| ServerLibrary/Models/PlayerObject.cs | 5600-5749 | 组队：GroupSwitch/GroupRemove/GroupInvite |
| ServerLibrary/Models/MonsterObject.cs | 2510-2547 | `MonsterObject.Die()`：YieldReward + 主从关系清理 + 尸体计时 |
| ServerLibrary/Models/MonsterObject.cs | 2549-2689 | `YieldReward()`：经验与掉落的组队分配（核心公式） |
| ServerLibrary/Models/MonsterObject.cs | 2691-2790 | `Drop()`：按 DropInfo 掷骰（详见 item/drops.md） |
| ServerLibrary/Models/MonsterObject.cs | 2428-2432 | EXPOwner 归属标签：首个攻击者 + 20s 延迟 |
| ServerLibrary/Models/Monsters/Guard.cs | 60, 68 | 守卫 AI：主动攻击红名（Redemption buff 除外） |
| ServerLibrary/Models/Magics/Taoist/Resurrection.cs | 22-120 | 复活术：目标筛选、成功率、恢复量 |
| ServerLibrary/Models/Magics/Taoist/SummonDead.cs | 73-75 | 招魂术对玩家尸体直接触发 TownRevive |
| ServerLibrary/Envir/Config.cs | 96, 112-118 | MaxViewRange=18、BrownDuration=60s、PKPointRate=50、PKPointTickRate=60s、RedPoint=200、PvPCurseDuration=60min、PvPCurseRate=4、AutoReviveDelay=10min |
| ServerLibrary/Envir/SConnection.cs | 405-410 | `Process(C.TownRevive)` → `Player.TownRevive()` |
| ServerLibrary/Envir/Commands/Command/Admin/RemovePKPoints.cs | 9-27 | GM 命令 REMOVEPKPOINTS：直接移除 PKPoint buff |
| LibraryCore/Enum.cs | 1889-1902 | `UserItemFlags`：Bound=2、Worthless=4、Marriage=128 等 |
| LibraryCore/Enum.cs | 239-242 | BuffType：Brown=4、PKPoint=5、PvPCurse=6、Redemption=7 |
| LibraryCore/Stat.cs | 635, 664, 704, 803 | Stat.Redemption / PKPoint / ItemReviveTime / DeathDrops |
| LibraryCore/Network/ClientPackets.cs | 87 | `C.TownRevive`（空包体） |
| LibraryCore/Network/ServerPackets.cs | 461-464, 543-546, 1156-1159 | `S.ObjectRevive` / `S.ObjectDied` / `S.ReviveTimers` |
| ServerLibrary/DBModels/CharacterInfo.cs | 481-497 | 持久化 `ItemReviveTime`、`ReincarnationPillTime` |

## 核心流程

### 1. 死亡判定入口（谁调用 Die）

`SetHP` 与 `ChangeHP` 在 HP 归零时按顺序尝试免死手段，全部失败才 `Die()`（`ServerLibrary/Models/MapObject.cs:1229-1243`，`ChangeHP` 分支同构于 `:1269-1283`）：

```csharp
if (CurrentHP <= 0 && !Dead)
{
    if (Buffs.Any(x => x.Type == BuffType.CelestialLight))
    {
        CelestialLightActivate();
        return;
    }
    if (Stats[Stat.ItemReviveTime] > 0 && SEnvir.Now >= ItemReviveTime)
    {
        ItemRevive();
        return;
    }

    Die();
}
```

- **天罡护体（CelestialLight）**：有该 buff 则触发免死护盾结算，不进入死亡。
- **装备自动复活（ItemRevive）**：身上装备提供 `Stat.ItemReviveTime > 0`（复活冷却秒数）且冷却已过，则原地满血（`ServerLibrary/Models/MapObject.cs:1318-1323`）：

```csharp
public virtual void ItemRevive()
{
    CurrentHP = Stats[Stat.Health];
    CurrentMP = Stats[Stat.Mana];
    ItemReviveTime = SEnvir.Now.AddSeconds(Stats[Stat.ItemReviveTime]);
}
```

玩家侧重写把冷却时间持久化到 `Character.ItemReviveTime` 并下发 `S.ReviveTimers`（`ServerLibrary/Models/PlayerObject.cs:2018-2025`）。

### 2. MapObject.Die（基类）

`ServerLibrary/Models/MapObject.cs:1665-1684`：

```csharp
public virtual void Die()
{
    Dead = true;

    BuffRemove(BuffType.Heal);
    BuffRemove(BuffType.DragonRepulse);
    BuffRemove(BuffType.ElementalHurricane);
    BuffRemove(BuffType.DefensiveBlow);

    var p = PoisonList.FirstOrDefault(x => x.Type == PoisonType.Chain);

    if (p != null && p.Owner is PlayerObject owner && owner.GetMagic(MagicType.Chain, out Chain chain))
    {
        chain.Explode(this);
    }

    PoisonList.Clear();

    Broadcast(new S.ObjectDied { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
}
```

要点：死亡移除治疗/龙振/元素风暴/防御反击 buff；身上若有连环锁链（Chain）毒素且施放者在场则引爆；清空全部毒素；向视野内玩家广播 `S.ObjectDied`。

### 3. PlayerObject.Die（玩家死亡全流程）

`ServerLibrary/Models/PlayerObject.cs:16208-16423`，按执行顺序：

1. **复活定时**：`RevivalTime = SEnvir.Now + Config.AutoReviveDelay;`（默认 10 分钟，`ServerLibrary/Envir/Config.cs:118`）。主循环 `ServerLibrary/Models/PlayerObject.cs:340-341` 到期自动回城：

```csharp
if (Dead && SEnvir.Now >= RevivalTime)
    TownRevive();
```

2. **清场**：下马、关闭交易；把自己标记过的所有怪物 `EXPOwner = null`（清仇恨归属，`16216-16221`）；调 `base.Die()`；逐个 Despell（SpellList）并让宠物全部 `Die()`（`16225-16231`）。
3. **魂共鸣**：有 `BuffType.SoulResonance` 则触发 `SoulResonance.Activate(this)`（`16233-16234`）。
4. **事件/战绩**：LogMilestone、`SEnvir.EventHandler.Process(this, "PLAYERDIE")`；攻城战统计 PvPDeathCount/PvPKillCount（宠物击杀记到 PetOwner 头上，`16240-16273`）。
5. **短路返回**：地图 Fight 设置为 Safe/Fight（`16275-16280`）或自己站安全区（`16282`）时，**不做**后续 PK 判责与掉落。
6. **击杀者归因**：LastHitter 为玩家 → attacker=该玩家；为怪物 → attacker=怪物的 PetOwner（宠物杀人算主人，`16284-16303`）。
7. **Rebirth（转生）特殊死亡惩罚**（`16305-16336`）：仅当 `Stats[Stat.Rebirth] > 0` 且不是被玩家所杀——当前经验条 **清零**，清掉的Experience 随机送给全服一个"未转生且等级 < 86"的在线玩家并全服公告：

```csharp
decimal expbonus = Experience;
Enqueue(new S.GainedExperience { Amount = -expbonus });
Experience = 0;
...
target = targets[SEnvir.Random.Next(targets.Count)];
target.GainExperience(expbonus, false, int.MaxValue, false);
SEnvir.Broadcast(new S.Chat { Text = $"{Name} has died and lost {expbonus:##,##0} Experience, {target?.Name ?? "No one"} has won the experience.", Type = MessageType.System });
```

> 普通玩家死亡 **没有** 经验惩罚实现（全库仅此一处 `GainedExperience { Amount = -… }`，另一处是遗忘药水洗点扣经验 `ServerLibrary/Models/PlayerObject.cs:6513-6518`）。

8. **PVP 判责**（详见下节 PK）：与击杀者行会交战（AtWar）→ 仅双向公告 GuildWarDeath；否则按双方红/棕名分三支。
9. **死亡掉落**：`if (Stats[Stat.DeathDrops] > 0) DeathDrop();`（`16421-16422`）。`Stat.DeathDrops` 是地图/实例开关（Stat.cs:803 "Death Drops Enabled."），普通地图默认 0 → 不掉。

### 4. 玩家死亡掉落 DeathDrop（公式照抄）

`ServerLibrary/Models/PlayerObject.cs:16425-16584`，分三段。**背包**（每格独立判定）：

```csharp
for (int i = 0; i < Inventory.Length; i++)
{
    UserItem item = Inventory[i];

    if (item == null) continue;
    if (!item.Info.CanDeathDrop) continue;
    if ((item.Flags & UserItemFlags.Bound) == UserItemFlags.Bound) continue;
    if ((item.Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage) continue;
    if ((item.Flags & UserItemFlags.Worthless) == UserItemFlags.Worthless) continue;
    if (SEnvir.Random.Next(10) > 0) continue;

    Cell cell = GetDropLocation(4, null);

    if (cell == null) break;

    long count;

    count = 1 + SEnvir.Random.Next((int)item.Count);
    ...
}
```

- 免掉条件四连：`Info.CanDeathDrop == false`（ItemInfo 级开关）、`UserItemFlags.Bound`（绑定）、`Marriage`（结婚戒指）、`Worthless`（无价值）。**没有单独的 "Insurance" 类**——保险机制即由这四个标记构成；`UserItemFlags` 全集见 `LibraryCore/Enum.cs:1889-1902`（Locked=1, Bound=2, Worthless=4, Refinable=8, Expirable=16, QuestItem=32, GameMaster=64, Marriage=128, NonRefinable=256）。
- 掉落概率：`SEnvir.Random.Next(10) > 0` → **每格 10%** 掉。
- 掉落数量：`count = 1 + SEnvir.Random.Next((int)item.Count);`（可堆叠物品随机掉一部分；全掉则整格移除，否则 `SEnvir.CreateFreshItem` 复制一份再扣源数量，`16446-16461`）。
- 掉落点：`GetDropLocation(4, null)` 找尸体周围 4 格内空格；找不到直接 `break`（`16438-16440`）。
- 掉出的物品 `dropItem.SetTemporary(true)` 后生成 `ItemObject` 落地，并回发 `S.ItemChanged` 同步背包（`16463-16473`）。

**伴侣背包**（`16477-16526`）：过滤少一个 Marriage 检查，概率改为 `if (SEnvir.Random.Next(7) > 0) continue;` → **每格 1/7 ≈ 14.3%**。

**装备**（`16528-16580`）：

```csharp
bool botter = Character.Account.ItemBot || Character.Account.GoldBot;

if (SEnvir.Random.Next((botter ? 10 : 100)) == 0)
{
    List<int> dropList = new List<int>();

    for (int i = 0; i < Equipment.Length; i++)
    {
        UserItem item = Equipment[i];

        if (item == null) continue;
        if (!item.Info.CanDeathDrop) continue;
        if ((item.Flags & UserItemFlags.Bound) == UserItemFlags.Bound) continue;
        if ((item.Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage) continue;
        if ((item.Flags & UserItemFlags.Worthless) == UserItemFlags.Worthless) continue;

        dropList.Add(i);

        if (botter && dropList.Count > 0) break;
    }

    if (dropList.Count > 0)
    {
        int index = dropList[SEnvir.Random.Next(dropList.Count)];
        ...
    }
}
```

- 装备掉落：普通号 **1%**（Next(100)==0）随机掉一件合格装备（整件，不拆数量）；被标记为打金/挂机号（ItemBot/GoldBot）概率升到 **10%**。收尾 `RefreshWeight(); RefreshStats();`（`16582-16583`）。

### 5. PK 惩罚体系

#### 5.1 灰名（Brown）——先动手者

`CheckBrown`（`ServerLibrary/Models/PlayerObject.cs:16595-16635`）：对自己下毒/攻击的目标若是"白名玩家"（非 Brown、PKPoint < RedPoint、无战争关系），攻击方获得 60 秒 Brown buff。安全区与 Fight 地图豁免。

```csharp
if (player.Stats[Stat.Brown] > 0 || player.Stats[Stat.PKPoint] >= Config.RedPoint) return;

if (AtWar(player)) return;

BuffAdd(BuffType.Brown, Config.BrownDuration, new Stats { [Stat.Brown] = 1 }, false, false, TimeSpan.Zero);
```

`Config.BrownDuration = TimeSpan.FromSeconds(60)`（`ServerLibrary/Envir/Config.cs:112`）。棕名只是"可被正当反击"标记：**杀棕名者不加 PK 点**（见 5.2 的 Brown==0 条件）。

#### 5.2 杀人加 PK 点

`PlayerObject.Die` 判责分支（`ServerLibrary/Models/PlayerObject.cs:16361-16396`）——受害者是白名时：

```csharp
if (Stats[Stat.PKPoint] < Config.RedPoint && Stats[Stat.Brown] == 0)
{
    Connection.ReceiveChatWithObservers(con => string.Format(con.Language.MurderedBy, attacker.Name), MessageType.System);

    //PvP death

    if (attacker.Stats[Stat.PKPoint] >= Config.RedPoint && SEnvir.Random.Next(Config.PvPCurseRate) == 0)
    {
        rate = -1;
        time = Config.PvPCurseDuration;
        buff = Buffs.FirstOrDefault(x => x.Type == BuffType.PvPCurse);

        if (buff != null)
        {
            rate += buff.Stats[Stat.Luck];
            time += buff.RemainingTime;
        }

        attacker.BuffAdd(BuffType.PvPCurse, time, new Stats { [Stat.Luck] = rate }, false, false, TimeSpan.Zero);

        attacker.Connection.ReceiveChatWithObservers(con => string.Format(con.Language.Curse, Name), MessageType.System);
    }
    else
    {
        attacker.Connection.ReceiveChatWithObservers(con => string.Format(con.Language.Murdered, Name), MessageType.System);
    }

    attacker.IncreasePKPoints(Config.PKPointRate);
}
else
{
    attacker.Connection.ReceiveChatWithObservers(con => con.Language.Protected, MessageType.System);

    Connection.ReceiveChatWithObservers(con => string.Format(con.Language.Killed, attacker.Name), MessageType.System);
}
```

- **只有杀"白名"（PKPoint < 200 且无 Brown）才 +50 点**（`Config.PKPointRate = 50`，`Config.cs:113`）。杀红名/棕名/战争对手不加。
- **杀人诅咒 PvPCurse**：凶手已是红名且 `Next(PvPCurseRate=4) == 0`（25%）时叠加 Luck-1、60 分钟（`Config.cs:116-117`）的诅咒 buff，可叠乘（rate 从 -1 起累加旧值、时长累加）。
- `IncreasePKPoints`（`ServerLibrary/Models/PlayerObject.cs:16637-16655`）——PK 点以 **PKPoint buff** 为载体（永续 buff，tick 衰减），且攒够红名时强制把回城绑定点换成红名专区：

```csharp
public void IncreasePKPoints(int count)
{
    BuffInfo buff = Buffs.FirstOrDefault(x => x.Type == BuffType.PKPoint);

    if (buff != null)
        count += buff.Stats[Stat.PKPoint];

    if (count >= Config.RedPoint && !Character.BindPoint.RedZone)
    {
        SafeZoneInfo info = SEnvir.SafeZoneInfoList.Binding.FirstOrDefault(x => x.RedZone && x.ValidBindPoints.Count > 0);

        if (info != null)
            Character.BindPoint = info;
    }

    LogMilestone(MilestoneType.PKPoint, count, true);

    BuffAdd(BuffType.PKPoint, TimeSpan.MaxValue, new Stats { [Stat.PKPoint] = count }, false, false, Config.PKPointTickRate);
}
```

其他 PK 点来源：足球哨子类道具 `IncreasePKPoints(item.Info.Stats[Stat.PKPoint])`（`ServerLibrary/Models/PlayerObject.cs:6813`）。

#### 5.3 红名判定与惩罚（RedPoint=200）

- 判定：`Stats[Stat.PKPoint] >= Config.RedPoint`（200，`Config.cs:115`）。
- 名字颜色（`ServerLibrary/Models/PlayerObject.cs:761-775`）：Rebirth 粉名 > 红名 > 棕名 > **PKPoint≥50 黄名** > 白名：

```csharp
if (Stats[Stat.PKPoint] >= Config.RedPoint)
    NameColour = Globals.RedNameColour;
else if (Stats[Stat.Brown] > 0)
    NameColour = Globals.BrownNameColour;
else if (Stats[Stat.PKPoint] >= 50)
    NameColour = Color.Yellow;
```

- **红名死亡惩罚（被怪杀）**：全身装备每件掉耐久 `item.Info.Durability / 10`（`ServerLibrary/Models/PlayerObject.cs:16400-16416`）：

```csharp
if (Stats[Stat.PKPoint] >= Config.RedPoint)
{
    bool update = false;
    for (int i = 0; i < Equipment.Length; i++)
    {
        UserItem item = Equipment[i];
        if (item == null) continue;

        update = DamageItem(GridType.Equipment, i, item.Info.Durability / 10) || update;
    }
    ...
}
```

> 找不到"红名被杀掉经验"的实现——本引擎红名死亡惩罚是耐久损毁，不是经验损失（经典 Mir3 的红名掉经验在此未实现）。

- **红名被玩家杀**：击杀者走 `Protected`（保护正当）分支，不加 PK 点（`16390-16395`）。
- **守卫攻击红名**（`ServerLibrary/Models/Monsters/Guard.cs:60`，宠物同理 `:68`）：

```csharp
return ob.Stats[Stat.PKPoint] >= Config.RedPoint && ob.Stats[Stat.Redemption] == 0;
```

- **红名活动限制**：不能结婚传送（`PlayerObject.cs:3114-3117`）、绑定点被强制为 RedZone 安全区（见 5.2）、NPC 可用 `NPCCheckType.PKPoints` 拦截（`ServerLibrary/Models/NPCObject.cs:549-552`）。
- **红名换回普通绑定点**：`PlayerObject.cs:14702-14704` 要求 `Stats[Stat.PKPoint] < Config.RedPoint` 才能用职业初始绑定点。

#### 5.4 PK 值清洗（时间衰减 + 道具 + GM）

- **自然衰减**：PKPoint buff 的 tick 处理（`ServerLibrary/Models/MapObject.cs:537-557`）——**每 60 秒 -1 点**（`Config.PKPointTickRate`，`Config.cs:114`），减到 0 移除 buff：

```csharp
case BuffType.PKPoint:
    buff.TickTime -= ticks;

    if (buff.TickTime > TimeSpan.Zero) continue;

    buff.TickFrequency = Config.PKPointTickRate;

    buff.TickTime += buff.TickFrequency;

    buff.Stats[Stat.PKPoint]--;

    RefreshStats();

    if (buff.Stats[Stat.PKPoint] <= 0)
        expiredBuffs.Add(buff);
    else
    {
        if (Race == ObjectType.Player)
            ((PlayerObject)this).Enqueue(new S.BuffChanged { Index = buff.Index, Stats = buff.Stats });
    }
    break;
```

- **忏悔药水（Potion of Repentence，item case 8）**：`buff.Stats[Stat.PKPoint] = Math.Max(0, buff.Stats[Stat.PKPoint] + item.Info.Stats[Stat.PKPoint]);`（道具 PKPoint 为负值，`ServerLibrary/Models/PlayerObject.cs:6523-6538`）。
- **赎罪基石（Redemption Key Stone，item case 9）**：给 Redemption（"Temporary Innocence"）buff——NPC 与守卫判定把有 Redemption 者当白名；同时把 PvPCurse 剩余时间减半（`ServerLibrary/Models/PlayerObject.cs:6539-6559`，`buff.RemainingTime = TimeSpan.FromTicks(buff.RemainingTime.Ticks / 2)`）。
- **GM 命令**：`REMOVEPKPOINTS <玩家名>` 直接移除 PKPoint buff（`ServerLibrary/Envir/Commands/Command/Admin/RemovePKPoints.cs:14-26`）。

### 6. 怪物死亡（掉落只列框架，明细见 item/drops.md）

`MonsterObject.Die`（`ServerLibrary/Models/MonsterObject.cs:2510-2547`）：`base.Die()` → `YieldReward()`（经验+掉落，见下节）→ 解除 Master/PetOwner 关系、清 MinionList → `DeadTime = SEnvir.Now + Config.DeadDuration`（有可割尸体 Drops 时追加 `Config.HarvestDuration`）→ 触发 MONSTERDIE/MONSTERCLEAR 事件 → `EXPOwner = null`。

掉落生成入口在 `MonsterObject.Drop`（`ServerLibrary/Models/MonsterObject.cs:2691-2790`）：遍历 `MonsterInfo.Drops`（`DropInfo`：Monster/Item/Chance/Amount/DropSet/PartOnly/EasterEvent，`LibraryCore/SystemModels/DropInfo.cs:5-122`），按 `chance = (long)(int.MaxValue / (drop.Chance * players) * rate)` 掷骰，失败还可能掉"部件"（ItemPart），带 Fortune（Progress/DropCount）保底系统。**掉落概率、金币分支、掉落归属与拾取详见 `docs/codebase/item/drops.md`**。

### 7. 经验分配（YieldReward 公式照抄）

触发条件：`if (EXPOwner == null || PetOwner != null) return;`（`ServerLibrary/Models/MonsterObject.cs:2551`）——宠物击杀无奖励，EXPOwner 是首个打中的玩家（`MonsterObject.cs:2428-2432`，`EXPOwnerDelay = TimeSpan.FromSeconds(20)`，`MonsterObject.cs:27`）。

#### 7.1 组队收集（ePlayers=可得分者，dPlayers=可得掉落者）

`ServerLibrary/Models/MonsterObject.cs:2559-2606`：

```csharp
foreach (PlayerObject ob in EXPOwner.GroupMembers)
{
    if (ob.CurrentMap != CurrentMap || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Config.MaxViewRange)) continue;
    ...
    dPlayers.Add(ob);

    if (ob.Dead) continue;
    ...
    ePlayers.Add(ob);
    totalLevels += ob.Level;
}
```

- **范围判定**：同地图 且 距离 ≤ `Config.MaxViewRange`（=18，`Config.cs:96`）。
- **死者仍分掉落（dPlayers）但不分经验（ePlayers）**。
- 无组队时 EXPOwner 本人即 dPlayers/ePlayers（成员列表为空走 `2643-2657` 单人分支）。

#### 7.2 职业多样性与地图倍率

`ServerLibrary/Models/MonsterObject.cs:2608-2641`：

```csharp
switch (Math.Min(dWarrior, Math.Min(dWizard, Math.Min(dTaoist, dAssassin))))
{
    case 1:
        dRate *= 1.1M;
        break;
    case 2:
        dRate *= 1.2M;
        break;
    case 3:
        dRate *= 1.3M;
        break;
}

switch (Math.Min(eWarrior, Math.Min(eWizard, Math.Min(eTaoist, eAssassin))))
{
    case 1:
        eRate *= 1.1M;
        break;
    case 2:
        eRate *= 1.25M;
        break;
    case 3:
        eRate *= 1.5M;
        break;
}

if (PetOwner == null && CurrentMap != null)
    eRate *= 1M + MapExperienceRate / 100M;

if (GrowthLevel > 0)
    eRate *= 1M + (GrowthLevel * 10) / 100M;

decimal exp = Math.Min(Experience * eRate, 500000000);
```

- dRate（掉落侧）四职业各≥1/2/3 人 → ×1.1/×1.2/×1.3；eRate（经验侧）→ ×1.1/×1.25/×1.5。
- `eRate` 初值 `1M + ExtraExperienceRate`（`MonsterObject.cs:2553`，副本/脚本注入的额外倍率）；再乘地图经验倍率与成长等级加成；单只经验封顶 **500,000,000**。

#### 7.3 分配公式

`ServerLibrary/Models/MonsterObject.cs:2660-2676`：

```csharp
if (ePlayers.Count > 1)
    exp += exp * 0.06M * ePlayers.Count; //6% per nearby member.

foreach (PlayerObject player in ePlayers)
{
    decimal expfinal = exp * player.Level / totalLevels;

    if (player.Stats[Stat.Rebirth] > 0 && ExtraExperienceRate > 0)
        expfinal /= ExtraExperienceRate;

    player.GainExperience(expfinal, PlayerTagged, Level);
    ...
}
```

- **组队规模奖励**：每多一名在场成员总池 +6%（含自己在内的 ePlayers.Count）。
- **按等级加权**：`expfinal = exp * player.Level / totalLevels`（等级高者分得多）。
- **转生惩罚**：转生玩家如果吃了 ExtraExperienceRate 加成，则整除回去（不给转生号吃副本倍率）。

#### 7.4 玩家侧入账 GainExperience

`ServerLibrary/Models/PlayerObject.cs:2036-2046`：

```csharp
public void GainExperience(decimal amount, bool huntGold, int gainLevel = Int32.MaxValue, bool rateEffected = true)
{
    if (rateEffected)
    {
        amount *= 1M + Stats[Stat.ExperienceRate] / 100M;

        amount *= 1M + Stats[Stat.BaseExperienceRate] / 100M;

        for (int i = 0; i < Character.Rebirth; i++)
            amount *= 0.5M;
    }
    ...
```

- 个人经验率（Stat.ExperienceRate + Stat.BaseExperienceRate）乘算；**每转生一次经验减半**。
- `PlayerTagged`（怪物被玩家而非 NPC/脚本打过，`MonsterObject.cs:49`，`:2416`，`:2505`）作为 huntGold 参数，用于挂机打金（HuntGold）累积（`PlayerObject.cs:2083-2094`）。
- 武器练级：`weapon.Experience += amount / 10;`（`PlayerObject.cs:2071`）。
- **等级差惩罚已禁用**：`gainLevel` 参数（传入怪物 Level）原本用于压低高等级玩家打低级怪的经验，现整段被注释（`PlayerObject.cs:2048-2060`）：

```csharp
/*
if (Level >= 60)
{

    if (Level > gainLevel)
        amount -= Math.Min(amount, amount * Math.Min(0.9M, (Level - gainLevel) * 0.10M));
}
else
{
    if (Level > gainLevel)
        amount -= Math.Min(amount, amount * Math.Min(0.3M, (Level - gainLevel) * 0.06M));
}
*/
```

（若重新启用：≥60 级每差 1 级 -10% 至多 -90%；<60 级每差 1 级 -6% 至多 -30%。）

### 8. 复活方式全集与经验惩罚

| 方式 | 触发 | 恢复 | 位置 | 经验惩罚 |
|---|---|---|---|---|
| 自动回城 | 死亡 10 分钟后主循环（`PlayerObject.cs:340-341`） | 满 HP/MP（`SetHP(Stats[Stat.Health])` 等） | 绑定点随机格（红名=RedZone 绑定点） | 无 |
| 手动回城 | 客户端发 `C.TownRevive`（`SConnection.cs:405-410`）→ `TownRevive()`（`PlayerObject.cs:1443-1464`） | 满 HP/MP/FP=0 | 同上 | 无 |
| 登录复活 | 上线时 `CurrentHP <= 0` → 直接 TownRevive（`PlayerObject.cs:1070-1074`） | 满 | 绑定点 | 无 |
| 复活术 Resurrection | 队友/会友对尸体施法（见下） | 按 `Magic.GetPower()`% HP/MP | **原地** | **无** |
| 招魂术 SummonDead | 对玩家尸体施法成功（`SummonDead.cs:73-75`） | TownRevive 流程（满血） | 绑定点 | 无 |
| 轮回丹 Reincarnation Pill | 死亡状态下使用道具 case 15（`PlayerObject.cs:6641-6653`） | 满 HP/MP | **原地** | 无 |
| 装备自动复活 ItemRevive | HP 归零且 `Stat.ItemReviveTime` 装备在身、冷却已过（`MapObject.cs:1236-1240`） | 满 HP/MP | 原地 | 无 |

```csharp
// TownRevive 核心（PlayerObject.cs:1443-1464）
public void TownRevive()
{
    if (!Dead) return;

    Map bindMap = SEnvir.GetMap(Character.BindPoint.BindRegion.Map);
    if (bindMap == null) return;

    Cell cell = bindMap.GetCell(Character.BindPoint.ValidBindPoints[SEnvir.Random.Next(Character.BindPoint.ValidBindPoints.Count)]);

    CurrentCell = cell.GetMovement(this);

    RemoveAllObjects();
    AddAllObjects();

    Dead = false;
    SetHP(Stats[Stat.Health]);
    SetMP(Stats[Stat.Mana]);
    SetFP(0);

    Broadcast(new S.ObjectRevive { ObjectID = ObjectID, Location = CurrentLocation, Effect = true });
}
```

**注意：`TownRevive` 不校验 `RevivalTime`**——服务端不锁冷却，靠客户端自觉（原版 WinForms 客户端只在死亡后给出可点击的聊天标签 `Client/Scenes/Views/ChatTab.cs:429-443`，点击即 `CEnvir.Enqueue(new C.TownRevive())`；触发点在 `Client/Envir/CConnection.cs:1464-1465` 收到自己 ObjectDied 时）。

**复活术**（`ServerLibrary/Models/Magics/Taoist/Resurrection.cs`）：
- 目标条件：`(Player.InGroup(target) || Player.InGuild(target)) && target.Dead`（`:31`）；强化复活术（AugmentResurrection，被动强化）可扩量为周围 3 格内多个死者（`:42-60`），每个目标消耗 `UseAmulet(1, 1)`（护身符，`:69-70`）。
- 成功率与恢复量（`MagicComplete`，`:93-120`）：

```csharp
if (SEnvir.Random.Next(100) > 25 + Magic.Level * 25) return;

int power = Magic.GetPower();

ob.Dead = false;
ob.SetHP(ob.Stats[Stat.Health] * power / 100);
ob.SetMP(ob.Stats[Stat.Mana] * power / 100);

Player.Broadcast(new S.ObjectRevive { ObjectID = ob.ObjectID, Location = ob.CurrentLocation, Effect = false });
Player.LevelMagic(Magic);

MagicCooldown(null, 20000);
```

- 成功率 = `25 + 技能等级 × 25`%（0 级 25% → 3 级 100%）；恢复 = `Magic.GetPower()`%；施法者 20 秒冷却；广播 `S.ObjectRevive`（Effect=false，无光柱）。
- **复活术无任何经验惩罚实现**——经典 Mir3"复活掉经验"在本引擎未实现；全库死亡相关经验惩罚只有 Rebirth 死亡清空经验条一条（见第 3 节第 7 步）。

## 数据结构/协议细节

### 参与死亡的协议包

```csharp
// LibraryCore/Network/ClientPackets.cs:87
public sealed class TownRevive : Packet { }

// LibraryCore/Network/ServerPackets.cs:461-464
public sealed class ObjectRevive : Packet
{
    public uint ObjectID { get; set; }
    public Point Location { get; set; }
    public bool Effect { get; set; }   // true=回城/道具复活(有特效) false=复活术
}

// LibraryCore/Network/ServerPackets.cs:543-546
public sealed class ObjectDied : Packet
{
    public uint ObjectID { get; set; }
    public MirDirection Direction { get; set; }
    public Point Location { get; set; }
}

// LibraryCore/Network/ServerPackets.cs:1156-1159
public sealed class ReviveTimers : Packet
{
    public TimeSpan ItemReviveTime { get; set; }
    public TimeSpan ReincarnationPillTime { get; set; }
}
```

- `S.ObjectDied`：任何单位死亡广播；客户端区分"自己死亡"（WinForms：`CConnection.cs:1464-1465` 弹复活标签）。
- `S.ObjectRevive.Effect`：回城/轮回丹复活带特效；复活术不带。
- `S.ReviveTimers`：装备复活与轮回丹的剩余冷却（秒），登录（`PlayerObject.cs:1134`）与使用时（`:2027-2033`、`:6651`）下发。
- 死亡掉落用通用 `S.ItemChanged`（GridType.Inventory/CompanionInventory/Equipment, Count）同步格子；地面物由 `ItemObject` 走标准掉落物协议。

### 关键持久化字段

- `CharacterInfo.ItemReviveTime`（`ServerLibrary/DBModels/CharacterInfo.cs:481-494`）：装备复活冷却存档。
- `CharacterInfo.ReincarnationPillTime`（`:496-497`）：轮回丹冷却存档。
- PK 点**不落库**——以账号 Buff（`Character.Account.Buffs`，登录时 `Buffs.AddRange`，`PlayerObject.cs:221`）形式持久，衰减实时发生在 buff tick。

## GodotClient 现状

| 功能 | 状态 | 证据 |
|---|---|---|
| S.ObjectDied 死亡表现 | **已移植** | `GodotClient/Scripts/GameScene.cs:7460-7485`：自己（`7461-7465` `_player.PlayDie()`）、其他玩家（`7466-7470`）、怪物/对象（`7471-7484` 设 Dead+Die 动画+死亡音效，目标死亡保留选中 D15）。启动积压包经 `PendingDeaths` 队列（`GodotClient/Network/ServerConnection.cs:696-700`），排空在 `GameScene.cs:7558-7559` |
| S.ObjectRevive 复活表现 | **已移植** | `GodotClient/Scripts/GameScene.cs:2168-2189`：清 Dead、瞬移坐标、Standing 动画；订阅/退订 `ServerConnection.cs:96,443`、`GameScene.cs:1093,1652` |
| S.ReviveTimers 复活冷却 | **已移植** | `GodotClient/Scripts/GameScene.cs:2743-2749` 存入 `ItemReviveUntilMs/ReincarnationPillUntilMs`（字段 `:775-776`）；道具 tooltip 显示 "Revival ready"（`:5662-5667`） |
| C.TownRevive 回城复活 | **部分移植** | 网络层完整：`GodotClient/Network/ServerConnection.cs:1092` SendTownRevive、`GameScene.cs:437` 包装。但**全库无任何 UI 调用 SendTownRevive**（grep 仅 2 处定义）；对照原版 `Client/Scenes/Views/ChatTab.cs:429-443` 的可点击复活标签——死亡后 Godot 客户端**没有手动复活入口**，只能等服务端 10 分钟自动回城 |
| 死亡自身 UI（灰屏/禁止操作/复活提示） | **部分移植** | `GameScene.cs:7461-7465` 仅播 Die 动画；未见死亡遮罩/复活倒计时 UI（`OnObjectDied` 对自身直接 return） |
| 红/棕/黄名颜色 | **已移植** | S.ObjectNameColour → `GodotClient/Scripts/GameScene.cs:2138-2152`（ObjectRenderer+PlayerRenderer NameColour）；入包字段 `ObjectRenderer.cs:80`、`PlayerRenderer.cs:147` |
| PK 点/灰名/诅咒 buff 图标 | **部分移植** | `GodotClient/Controls/BuffDialog.cs:152-153`：Brown=229、PKPoint=266 有图标映射；PvPCurse/Redemption 未见图标条目（grep 仅 Brown/PKPoint 命中） |
| 经验增减显示（含负经验） | **已移植** | S.GainedExperience → `GodotClient/Scripts/GameScene.cs:5064-5067`（`_playerExperience += amount` 支持负值）；文案 `GodotClient/Translations/ChineseMessages.cs:47-48` "获得/失去经验值" |
| 组队（分经验前提） | **已移植** | `GodotClient/Controls/GroupDialog.cs:62-70,155-156`（邀请/允许开关）、`GameScene.cs:6447-6450`（SendGroupSwitch/SendGroupInvite）、`:2546-2550`（四个组队包处理）、`:1960-1961`（快捷键对选中玩家邀请） |
| 复活术 Resurrection | **部分移植** | 通用施法通道可用（`GameScene.cs:9775+` C.Magic 组包）；特效/音效已登记：`GodotClient/Scripts/MagicEffectTable.cs:530`、`MagicSoundCatalog.cs:115,206`；但未见针对"尸体目标"的复活术目标选择/结果处理的专门逻辑（依赖通用目标系统对 Dead 对象的兼容性，未验证） |
| 玩家死亡掉落（S.ItemChanged 同步） | **已移植** | 死亡掉落复用通用 ItemChanged 通道；物品格同步属通用背包系统（见 inventory 相关文档） |

## 移植注意事项

1. **手动复活 UI 是最大缺口**：服务端 `TownRevive` 不校验冷却，协议只要 `C.TownRevive` 一个空包。Godot 端补一个死亡后的"回城复活"按钮/可点击聊天条（对齐原版 `ChatTab.cs:429-443`）即可闭环；同时建议客户端本地显示 10 分钟自动复活倒计时（服务端不单独下发，需用 `S.ObjectDied` 时间戳 + `Config.AutoReviveDelay` 推算，或读 `S.ReviveTimers` 之外的自身状态）。
2. **死亡表现顺序**：`S.ObjectDied` 广播先于服务端所有掉落/PK 判责完成，客户端不应在收到 ObjectDied 时立即移除对象——尸体保留由 Dead 标志 + 后续 `S.ObjectRemove` 驱动（Godot 已按此实现，D15 保留选中逻辑勿破坏）。
3. **复活术的 `S.ObjectRevive.Effect=false`**：Godot 端目前不区分 Effect 分支（`GameScene.cs:2168-2189` 未读 packet.Effect），移植光柱特效时需补。
4. **PK 点是 buff 不是角色字段**：客户端显示 PK 值应从 PKPoint buff 的 Stats 读取（BuffDialog 已映射图标 266）；红/黄名颜色以服务端 `S.ObjectNameColour` 为准（PKPoint≥50 即黄名这条阈值只在服务端 `PlayerObject.cs:773`，客户端不要自行复算）。
5. **死亡掉落的免掉四标记**（CanDeathDrop/Bound/Marriage/Worthless）与 10%/1/7/1% 概率均为服务端权威，客户端只需处理 `S.ItemChanged`；但 Godot 的物品 tooltip 可参考 `UserItemFlags` 展示"绑定/已绑定"提示（现有 tooltip 已处理 Expirable，`GameScene.cs:5656-5658`）。
6. **组队经验的可视化**：+6%/人、职业多样性加成、等级加权全在服务端 `YieldReward`，客户端无法复算；聊天栏经验数字来自 `S.GainedExperience`（负值代表 Rebirth 死亡清经验/洗点药），必须支持负数显示。
7. **红名专区绑定**：`IncreasePKPoints` 会在攒够 200 点时把 `Character.BindPoint` 换成 RedZone 安全区，红名玩家回城复活点因此不同——移植地图/绑定点系统时必须保留 `SafeZoneInfo.RedZone` 分支。
8. **召唤物/傀儡的 Die** 有大量 override（`ServerLibrary/Models/Monsters/` 下 CastleGate、Doll、Puppet 等 16 处，见 grep 结果），做通用死亡流程时不要假设 `MonsterObject.Die` 是唯一入口。
