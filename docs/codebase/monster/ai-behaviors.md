# 怪物 AI 行为全解（MonsterObject 状态机 / 仇恨 / 回血逃跑援护 / AI 编号映射 / MonsterInfo / 刷新与经验）

## TL;DR 速查表

- 怪物 AI 没有显式状态枚举，而是**每 tick 顺序执行五个子过程**：`ProcessAI() = ProcessRegen → ProcessSearch → ProcessRoam → ProcessTarget`（`ServerLibrary/Models/MonsterObject.cs:965-989`），"状态"由 `Target == null` 与各时间戳（`SearchTime/RoamTime/ActionTime/AttackTime`）隐式表达。
- 调度入口：`SEnvir` 主循环 1ms 时间片调 `ActiveObjects` 里每个对象的 `StartProcess()` → `MonsterObject.Process()`（`ServerLibrary/Envir/SEnvir.cs:1448-1478`、`MonsterObject.cs:928-954`）。
- 仇恨：`ProcessSearch` 每 3s（`SearchDelay`）选**视野内最近的玩家/宠物**（距离并列随机取一），`Attacked()` 被打时立即反击锁定攻击者（`MonsterObject.cs:1053-1114`、`2491-2492`）。
- 追击/攻击：基础 `InAttackRange()` = 相邻 1 格；`MoveTo` 用"主方向 + 旋转 8 向"试探绕路（`MonsterObject.cs:1308-1313`、`3101-3122`）。
- 回血：每 10s（`RegenDelay`，`MapObject.cs:116`）回 `max(1, HP*2%)`，中 Hemorrhage 毒时不回（`MonsterObject.cs:1038-1051`）。
- 脱战回巢：`ProcessRoam` 发现自己走出刷怪区域（`RoamDistance` stat）超界时，走向区域**边缘最近点**（`MonsterObject.cs:1180-1241`）。
- AI 编号：`MonsterObject.GetMonster(MonsterInfo)` 里一个巨型 `switch (monsterInfo.AI)`（`MonsterObject.cs:122-647`），-1=Guard 城卫、0/32/51/55/73 与 default=普通主动怪、1/2=被动可收割……全表见下文。
- `MirMonType` 枚举**在本仓库不存在**（全库 grep 无匹配）；策划文档里说的"特殊怪类型"实际是 `MonsterFlag`（`LibraryCore/Enum.cs:2140-2211`）。
- 刷新：每秒 `SpawnInfo.DoSpawn` 补足到 `Count`；`Delay` 单位=分钟，`Announce=true` 固定间隔+全服公告，`Delay >= 1000000` 表示每日定点（`ServerLibrary/Models/Map.cs:391-472`）。
- 经验：`YieldReward()` 按 EXPOwner（首次攻击者，20s 归属权）及其同图队伍按等级比例分经验；**玩家与怪物等级差惩罚代码已被注释禁用**（`MonsterObject.cs:2549-2689`、`PlayerObject.cs:2048-2060`）。

## 职责概述

`MonsterObject`（`ServerLibrary/Models/MonsterObject.cs`，3,199 行）是全部怪物的运行时对象：从 `MonsterInfo`（System.db）实例化、挂到地图 Cell、被 `SEnvir.ActiveObjects` 调度执行 AI、与玩家/宠物战斗、死亡后结算经验与掉落（`YieldReward/Drop`）、尸体到期 `Despawn` 归还刷怪点计数。约 150 个子类（`ServerLibrary/Models/Monsters/`）通过 override `InAttackRange/ProcessTarget/Attack/Process` 等钩子组合出远程、召唤、狂暴、瞬移等行为；子类的挑选由 `MonsterInfo.AI` 编号在 `GetMonster` 工厂里决定。本文覆盖：AI 状态机全流程、仇恨/搜索/绕路、回血/逃跑/援护、AI 编号→类映射全表、`MonsterInfo`/`MonsterInfoStat` 字段、`MonsterFlag`、刷新与经验规则。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| `ServerLibrary/Models/MonsterObject.cs` | 17-3198 | 怪物基类：AI 五过程、攻击结算、召唤、经验/掉落 |
| `ServerLibrary/Models/MonsterObject.cs` | 122-647 | `GetMonster()`：AI 编号 → 行为类工厂（唯一映射来源） |
| `ServerLibrary/Models/MonsterObject.cs` | 928-989 | `Process()`/`ProcessAI()`：tick 入口与调度顺序 |
| `ServerLibrary/Models/MonsterObject.cs` | 1038-1051 | `ProcessRegen()` 回血 |
| `ServerLibrary/Models/MonsterObject.cs` | 1053-1162 | `ProcessSearch()`/`ProperSearch()` 索敌 |
| `ServerLibrary/Models/MonsterObject.cs` | 1164-1241 | `ProcessRoam()` 巡逻 + 回巢 |
| `ServerLibrary/Models/MonsterObject.cs` | 1243-1276 | `ProcessTarget()` 追击/攻击 |
| `ServerLibrary/Models/MonsterObject.cs` | 1490-1688 | `ShouldAttackTarget()` 仇恨过滤（隐身/潜行/GM 等） |
| `ServerLibrary/Models/MonsterObject.cs` | 1689-1779 | `Attack()` 动作播包 + `Attack(ob,power,element)` 伤害结算 |
| `ServerLibrary/Models/MonsterObject.cs` | 2359-2405 | `UpdateAttackTime()`/`UpdateMoveTime()` 攻速/移速节流 |
| `ServerLibrary/Models/MonsterObject.cs` | 2407-2496 | `Attacked()` 受击（EXPOwner 归属、反击锁定） |
| `ServerLibrary/Models/MonsterObject.cs` | 2510-2689 | `Die()`/`YieldReward()` 死亡结算与经验分配 |
| `ServerLibrary/Models/MonsterObject.cs` | 2691-3028 | `Drop()` 掉落掷骰 + 任务计数 |
| `ServerLibrary/Models/MonsterObject.cs` | 3041-3122 | `Walk()`/`MoveTo()` 移动与绕路 |
| `ServerLibrary/Models/MapObject.cs` | 83-88 | 基础计时器与 `CanMove/CanAttack/CanCast` 判定 |
| `ServerLibrary/Models/MapObject.cs` | 98,116 | `RegenDelay` 默认 10 秒 |
| `ServerLibrary/Models/MapObject.cs` | 1665-1684 | 基类 `Die()` |
| `ServerLibrary/Models/Map.cs` | 374-473 | `SpawnInfo`：运行时刷怪点（NextSpawn/AliveCount/DoSpawn） |
| `ServerLibrary/Envir/SEnvir.cs` | 1448-1478 | ActiveObjects 1ms 时间片调度 AI |
| `ServerLibrary/Envir/SEnvir.cs` | 1572-1573 | 每秒相位调 `spawn.DoSpawn(false)` |
| `ServerLibrary/Envir/Config.cs` | 96,124-125,131 | `MaxViewRange=18`、`DeadDuration=1min`、`HarvestDuration=5min`、`DropDistance=5` |
| `LibraryCore/SystemModels/MonsterInfo.cs` | 6-305 | 怪物静态数据（AI/Level/ViewRange/CoolEye/IsBoss…） |
| `LibraryCore/SystemModels/MonsterInfoStat.cs` | 5-55 | 怪物属性三元组（Monster+Stat+Amount） |
| `LibraryCore/SystemModels/RespawnInfo.cs` | 6-155 | 刷怪点持久化模型 |
| `LibraryCore/Enum.cs` | 2140-2211 | `MonsterFlag`（注意：`MirMonType` 不存在） |
| `ServerLibrary/Models/PlayerObject.cs` | 2036-2108 | `GainExperience()`（等级差惩罚已注释） |
| `ServerLibrary/Models/Monsters/Guard.cs` | 8-128 | AI=-1 城卫：不可移动、攻击红名/PK |
| `ServerLibrary/Models/Monsters/SkeletonAxeThrower.cs` | 9-84 | 远程风筝怪原型（FearTime 逃跑） |
| `ServerLibrary/Models/Monsters/ZumaGuardian.cs` | 9-79 | 雕像潜伏 + `WakeAll` 同族报警 |
| `ServerLibrary/Models/Monsters/HealerAnt.cs` | 10-63 | 治疗援护怪（攻击目标=受伤友军） |

## 核心流程：AI 状态机

### 1. 调度链

`SEnvir.EnvirLoop` 每轮给 ActiveObjects **1ms 时间片**（`SEnvir.cs:1448-1478`）：从尾部游标逐个调 `ob.StartProcess()`（玩家已在前面单独处理，1462 行跳过），单对象异常只把它移出激活列表（1469-1477）。怪物只有在**附近有玩家 / 是 BOSS / 是宠物**时才激活（`MonsterObject.Activate/DeActivate`，`MonsterObject.cs:881-898`）：

```csharp
public override void Activate()
{
    if (Activated) return;

    if (NearByPlayers.Count == 0 && (MonsterInfo.ViewRange <= Config.MaxViewRange || CurrentMap.Players.Count == 0) && !MonsterInfo.IsBoss && PetOwner == null) return;

    Activated = true;
    SEnvir.ActiveObjects.Add(this);
}
```

（`MonsterObject.cs:881-889`；`DeActivate` 在 894 行同理反向判断——BOSS 和宠物永不休眠。）

### 2. Process()：每 tick 固定动作（`MonsterObject.cs:928-954`）

```csharp
public override void Process()
{
    base.Process();

    if (Dead)                                   // ① 尸体计时
    {
        Target = null;
        if (SEnvir.Now > DeadTime)
        {
            Despawn();                          // 到期消失（Die 时 DeadTime = Now + Config.DeadDuration）
            return;
        }
    }

    if (Target?.Node == null || Target.Dead || Target.CurrentMap != CurrentMap
        || !Functions.InRange(CurrentLocation, Target.CurrentLocation, Config.MaxViewRange)
        || ((Poison & PoisonType.Abyss) == PoisonType.Abyss && !Functions.InRange(CurrentLocation, Target.CurrentLocation, ViewRange))
        || !CanAttackTarget(Target))            // ② 目标失效检查：死亡/换图/超 18 格/Abyss 致盲/关系变化
        Target = null;

    if (Target != null && Target.Buffs.Any(x => x.Type == BuffType.Cloak)
        && !Functions.InRange(CurrentLocation, Target.CurrentLocation, 2)
        && Stats[Stat.IgnoreStealth] == 0)      // ③ 目标潜行且距离 >2 → 丢失
        Target = null;

    if (Target != null && Target.Buffs.Any(x => x.Type == BuffType.Transparency))
        Target = null;                          // ④ 目标透明化 → 丢失

    ProcessAI();
}
```

### 3. ProcessAI()：五过程顺序（`MonsterObject.cs:965-989`）

```csharp
public virtual void ProcessAI()
{
    if (Dead) return;

    if (PetOwner?.Node != null)                 // 宠物专属修正
    {
        if (Target != null)
        {
            if (PetOwner.PetMode == PetMode.PvP && Target.Race != ObjectType.Player) Target = null;
            if (PetOwner.PetMode == PetMode.None || PetOwner.PetMode == PetMode.Move) Target = null;
        }
        if (SEnvir.Now > TameTime) UnTame();    // 驯服到期 → 脱离主人（HP 砍到 1/10，MonsterObject.cs:1013-1026）
        else if (Visible && !PetOwner.VisibleObjects.Contains(this)
            && (PetOwner.PetMode == PetMode.Both || PetOwner.PetMode == PetMode.Move || PetOwner.PetMode == PetMode.PvP))
            PetRecall();                        // 掉队 → 传送回主人背后（MonsterObject.cs:1027-1037）
    }

    ProcessRegen();     // 回血
    ProcessSearch();    // 索敌（无目标时）
    ProcessRoam();      // 巡逻/回巢
    ProcessTarget();    // 追击/攻击（有目标时）
}
```

### 4. 完整状态机伪代码（巡逻 → 发现 → 追击 → 攻击 → 脱战回巢）

```
每 tick（被激活时）:
    若已死亡:
        清空 Target; 若 Now > DeadTime → Despawn（尸体 1 分钟，可收割怪 +5 分钟）
        return
    失效检查: 目标死亡/换图/超 MaxViewRange(18)/Abyss 致盲/潜行/透明 → Target = null

    ProcessRegen():
        每 RegenDelay(10s) 且 HP<Max 且未中 Hemorrhage 毒:
            HP += max(1, MaxHP * 2%)

    ProcessSearch():                            # —— 巡逻/发现 ——
        宠物 → ProperSearch()（环形扫描，见下）
        有目标时: 隐身怪等 SearchTime；完全不能动打则跳过
        无目标且 Now < SearchTime(3s 一次) 或 本图无玩家 → return
        遍历本图所有玩家及其宠物:
            d = Chebyshev 距离; d > ViewRange → 跳过
            ShouldAttackTarget(对方) 为假 → 跳过
            d < bestDistance → 清空候选列表; d == bestDistance → 追加（并列随机）
        候选非空 → Target = random(closest)      # 发现！进入追击

    ProcessRoam():                              # —— 无目标时的游走 ——
        不能动 → return; 宠物无目标 → 跟随主人背后格
        每 RoamDelay(2s) 且 被玩家看见:
            走出刷怪区域(RoamDistance stat) → MoveTo(区域边缘最近点)   # 回巢
            被别的对象挡在当前格 → 随机方向+旋转 8 向走一步让位
            无目标且 90% 概率 → return（站立）
            2/3 概率朝当前朝向走 1 格，1/3 概率随机转向           # 巡逻

    ProcessTarget():                            # —— 追击/攻击 ——
        无目标 → return
        中 Fear 毒 → 朝目标反方向走 1 格（逃跑）return
        不在攻击距离:
            与目标同格 → 随机方向旋转试走脱身
            否则 MoveTo(Target)                 # 追击（主方向+旋转 8 向绕路）
            return
        CanAttack 为假（攻击冷却/麻痹等）→ return
        Attack()                                # 播 ObjectAttack 包，400ms 后结算伤害
```

### 5. 移动与卡位绕路（`MonsterObject.cs:3041-3122`）

`Walk(direction)`：检查 `CanMove`（基础版要求 `SEnvir.Now >= ActionTime && >= MoveTime && > ShockTime` 且无麻痹/锁足类毒，`MapObject.cs:86`；怪物版再叠加沉默毒与宠物模式，`MonsterObject.cs:119`）→ 目标格 `IsBlocking` 检查 → 可选 `AvoidFireWall`（绕开敌方火墙/风暴格，3060-3080）→ 走入并广播 `S.ObjectMove`。

`MoveTo(target)` 是**贪心 + 旋转**的局部绕路（无寻路算法）：

```csharp
protected virtual void MoveTo(Point target)
{
    if (CurrentLocation == target) return;

    if (Functions.InRange(target, CurrentLocation, 1))
    {
        Cell cell = CurrentMap.GetCell(target);
        if (cell == null || cell.IsBlocking(this, false)) return;
    }

    MirDirection direction = Functions.DirectionFromPoint(CurrentLocation, target);

    int rotation = SEnvir.Random.Next(2) == 0 ? 1 : -1;

    for (int d = 0; d < 8; d++)
    {
        if (Walk(direction)) return;

        direction = Functions.ShiftDirection(direction, rotation);  // 卡住 → 顺/逆时针换向重试
    }
}
```

（`MonsterObject.cs:3101-3122`。八个方向全堵则本 tick 放弃——这就是玩家"卡位"怪物的原理。）

### 6. 攻击节流与结算

- 每次攻击/移动后 `UpdateAttackTime()/UpdateMoveTime()` 重置冷却（`MonsterObject.cs:2359-2405`）：`AttackTime = Now + AttackDelay(ms)`，且 Slow 毒每点 +100ms、Chain 毒 +100ms、Neutralize 毒直接翻倍。
- 基础近战 `Attack()`（`MonsterObject.cs:1689-1702`）：广播 `S.ObjectAttack`，把 `(Target, GetDC(), AttackElement)` 塞进 `ActionList` 延迟 **400ms** 结算——客户端动画前摇与服务端结算解耦。
- 结算 `Attack(ob, power, element)`（`MonsterObject.cs:1704-1779`）：Abyss 毒 50% miss；物理走命中/敏捷判定（`Random(Agility) > Accuracy` 即躲闪）后减 AC，元素直接减 MR；抗性 `res>0: damage -= damage*res/10`，`res<0: damage -= damage*res/5`（负抗增伤）；吸血 `LifeSteal`；最后按 `PoisonRate`（默认 10，即 1/10 概率）附毒。
- `GetDC()`（`MapObject.cs:1732-1753`）：`[MinDC, MaxDC]` 随机，幸运 ≥10 必最大、正幸运 10 分之概率取最大、负幸运偏向最小。

## 仇恨机制

### ProcessSearch：选最近（`MonsterObject.cs:1053-1114`）

- 触发条件：无目标时受 `SearchTime`（默认 `SearchDelay = 3s`，`MonsterObject.cs:25`）节流，且本图必须有玩家；有目标时只有隐身怪等冷却或"还能动/打"才继续搜索（便于换目标）。
- 遍历 `CurrentMap.Players`，**先查每个玩家的宠物再查玩家本人**（1083-1108），`Functions.Distance` 为 Chebyshev（棋盘）距离；只保留 `distance <= ViewRange` 且 `ShouldAttackTarget` 通过的最近一层，**并列时随机取一个**。
- `ViewRange`（`MonsterObject.cs:101-104`）：取 `MonsterInfo.ViewRange`，但中 Abyss 毒时强制降为 2：

```csharp
public int ViewRange
{
    get { return PoisonList.Any(x => x.Type == PoisonType.Abyss) ? 2 : MonsterInfo.ViewRange; }
}
```

- 宠物与部分特化怪用 `ProperSearch()`（`MonsterObject.cs:1115-1162`）：从 d=0 到 ViewRange **逐环扫描**格子（环形边界迭代），第一环内所有可攻击对象随机取一——不区分远近，适合治疗/守卫。

### ShouldAttackTarget：仇恨过滤（`MonsterObject.cs:1490-1688`）

按序过滤：`Passive`（被动怪永不主动仇恨）→ 自身/死亡/不可见/Guard/CastleLord → Item/NPC/Spell → **Invisibility 隐身**（无 `CoolEye` 则无视）→ **Cloak 潜行**（需 CoolEye 且距离 ≤2 且 `ob.Level < Level`）→ **Transparency 透明**（需中 Parasite 毒且自身 ≥100 级）。玩家侧再叠加：GameMaster、`ClearRing` 戒指、安全区（宠物）、宠物模式/攻击模式（Peace/Group/Guild/WarRedBrown）。野生怪对野生怪：仅 `SEnvir.Now < RageTime`（狂暴期）才互相攻击（1590 行）——`RageTime` 由道士 ElectricShock（圣言）驯服失败时设置 10-30s（`Magics/Wizard/ElectricShock.cs:81`）。

### 被打反击与 EXPOwner（`MonsterObject.cs:2407-2496`）

```csharp
if (EXPOwner == null && PetOwner == null)
    EXPOwner = player;                       // 首次命中者拿到经验归属

if (EXPOwner == player && player != null)
    EXPOwnerTime = SEnvir.Now + EXPOwnerDelay;   // 20s 归属权刷新

...
if (CanAttackTarget(attacker) && PetOwner == null || Target == null)
    Target = attacker;                       // 被打立即锁定攻击者（嘲讽）
```

EXPOwner 归属过期由玩家侧清理：`PlayerObject.cs:329-332` 每次遍历 `TaggedMonsters`，`SEnvir.Now >= ob.EXPOwnerTime` 即从归属列表移除。中毒也会拉仇恨：`ApplyPoison` override 里 `CanAttackTarget(p.Owner) && Target == null → Target = p.Owner`（`MonsterObject.cs:2497-2508`）。

### 攻击距离判定

基类 `InAttackRange()`（`MonsterObject.cs:1308-1313`）= 同图 && 不同格 && Chebyshev 距离 ≤1（近战贴身）。远程系子类 override：如 `YumgonWitch.AttackRange = 10`（`YumgonWitch.cs:8-17`）、`DragonLord.AttackRange = 12`（`DragonLord.cs:12-17`）、`JinchonDevil` 十字 3 格（`JinchonDevil.cs:14-26`）。

## 回血 / 逃跑 / 援护

### 回血 ProcessRegen（`MonsterObject.cs:1038-1051`）

```csharp
public virtual void ProcessRegen()
{
    if (SEnvir.Now < RegenTime) return;

    RegenTime = SEnvir.Now + RegenDelay;      // RegenDelay 默认 10s（MapObject.cs:116）

    if (CurrentHP >= Stats[Stat.Health]) return;

    if ((Poison & PoisonType.Hemorrhage) == PoisonType.Hemorrhage) return;   // 出血毒禁疗

    int regen = (int)Math.Max(1, Stats[Stat.Health] * 0.02F); //2% every 10 seconds aprox

    ChangeHP(regen);
}
```

特例：`QuartzTree.ProcessRegen()` 为空——不回血（`Monsters/QuartzTree.cs:64-66`）。

### 逃跑

- **Fear 毒**：`ProcessTarget` 开头 `(Poison & PoisonType.Fear) == Fear → 朝目标反方向 Walk`（`MonsterObject.cs:1247-1251`）。
- **远程风筝怪**（`SkeletonAxeThrower.cs:22-64`）：目标进入 `AttackRange-1` 以内时背向目标旋转 8 向走位拉开距离（48-60 行）；攻击时按 `FearRate`（默认 6，即 1/6）概率进入 `FearTime = FearDuration + Random(4)` 秒的"恐慌"，期间不攻击（61、73-74 行）。`NumaStoneThrower` 同款（`NumaStoneThrower.cs:18-19`）。
- 未找到"HP 低于阈值整体逃跑（RUN）"的通用基类实现；逐 HP 阈值触发的都是 BOSS/特化怪行为（如 `JinchonDevil` HP≤1/2 必放全屏毒云，见 boss-mechanics.md）。

### 援护（同族报警 / 治疗 / 召唤）

- **同族报警**：祖玛雕像 `ZumaGuardian`（`ZumaGuardian.cs:9-79`）——出生 `Visible=false`（雕像态，不可选/免伤/免毒，29-33、64-69 行）；自身被锁定且目标进入 3 格内 `Wake()` 并 `WakeAll(WakeRange=7)`：把周围 7 格内所有未显形的 ZumaGuardian 全部唤醒并**共享同一个 Target**（35-61 行）。`ZumaKing.Wake` 额外延迟 2s（`ZumaKing.cs:24-29`）。
- **治疗援护**：`HealerAnt`（`HealerAnt.cs:10-63`）把"攻击目标"重定义为"受伤且无 Heal buff 的友军"（12-24 行），攻击动作变成 `BuffAdd(BuffType.Heal)`（43-48 行）。
- **召唤物互相支援**：`SpawnMinions(fixedCount, randomCount, target)`（`MonsterObject.cs:1278-1300`）上限 `MaxMinions`（默认 20），召唤时直接 `mob.Target = target; mob.Master = this`——小怪出生即共享仇恨。
- 未找到"被攻击时向全屏同族广播仇恨"的通用 Recall 实现；最接近的是 `WakeAll` 与 BOSS 召唤链。

## MonsterInfo.AI 编号 → 行为类映射（完整）

工厂在 `MonsterObject.GetMonster(MonsterInfo)`（`ServerLibrary/Models/MonsterObject.cs:122-647`）。`default` 分支（644-645 行）= 纯 `MonsterObject`（主动近战怪，无收割/无毒素）。**编号 0、32、51、55、73 没有 case**，同样落入 default。

| AI | 行为类 | 备注（行号为 MonsterObject.cs） |
|---|---|---|
| -1 | `Guard` | 城卫：不动、只攻击 PK/红名（126-127） |
| 0 | `MonsterObject`（default） | 主动近战 |
| 1 | `MonsterObject{Passive,NeedHarvest,HarvestCount=2}` | 被动+可收割（128-129） |
| 2 | `MonsterObject{Passive,NeedHarvest,HarvestCount=3}` | 被动+可收割（130-131） |
| 3 | `MonsterObject{NeedHarvest,HarvestCount=3}` | 主动+可收割（132-133） |
| 4 | `TreeMonster` | 树怪（134-135） |
| 5 | `CarnivorousPlant{NeedHarvest=2}` | 食人花（136-137） |
| 6 | `SpittingSpider{Green 毒,NeedHarvest=2}` | 毒液喷吐（138-139） |
| 7 | `SkeletonAxeThrower` | 远程掷斧（140-141） |
| 8 | `MonsterObject{Paralysis 毒,NeedHarvest=2}` | 麻痹近战（142-143） |
| 9 | `GhostSorcerer` | 巫妖（144-145） |
| 10 | `GhostMage` | 幽灵法师（146-147） |
| 11 | `VoraciousGhost` | 贪吃鬼：可复活（148-149） |
| 12 | `HealerAnt` | 治疗蚁（150-151） |
| 13 | `LordNiJae` | 尼才领主 BOSS（152-153） |
| 14 | `SpittingSpider{Green 毒}` | 154-155 |
| 15 | `MonsterObject` | 主动近战（156-157） |
| 16 | `UmaKing` | 蛟王：远程 AoE（158-159） |
| 17 | `ArachnidGrazer{SpawnList=Larva×1}` | 产幼虫（160-165） |
| 18 | `Larva{Green 毒}` | 166-167 |
| 19 | `RedMoonTheFallen` | 168-169 |
| 20 | `SkeletonAxeThrower{FearRate=2,FearDuration=4}` | 高频逃跑（170-171） |
| 21 | `ZumaGuardian` | 潜伏雕像（172-173） |
| 22 | `ZumaKing{SpawnList=祖玛系}` | 祖玛教主（174-185） |
| 23/24 | `Monkey{Green 毒}/{Red 毒}` | 186-189 |
| 25 | `EvilElephant` | 190-191 |
| 26/36 | `NumaMage` | 192-193、210-211 |
| 27 | `GhostMage` | 194-195 |
| 28 | `WindfurySorcerer` | 196-197 |
| 29 | `SkeletonAxeThrower` | 198-199 |
| 30 | `NetherworldGate` | 200-201 |
| 31/48 | `SonicLizard{IgnoreShield(48)}` | 202-203、249-250 |
| 32 | （无 case → default） | |
| 33 | `GiantLizard{AttackRange=9,IgnoreShield}` | 204-205 |
| 34 | `SkeletonAxeThrower{AttackRange=9}` | 206-207 |
| 35/37/39/40 | `MonsterObject` | 208-209、212-213、216-219 |
| 38 | `BanyaLeftGuard` | 214-215 |
| 41 | `EmperorSaWoo` | 沙悟大帝 BOSS（220-221） |
| 42 | `SpittingSpider` | 222-223 |
| 43 | `ArchLichTaedu{SpawnList=骨系}` | 大法老 BOSS（224-236） |
| 44 | `WedgeMothLarva{SpawnList}` | 237-242 |
| 45 | `RazorTusk` | 243-244 |
| 46 | `SpittingSpider{Red 毒,Rate=25}` | 245-246 |
| 47 | `SpittingSpider{Green 毒,Ticks=7,Rate=15}` | 247-248 |
| 49 | `GiantLizard{Range=8,Paralysis 毒}` | 251-252 |
| 50 | `GiantLizard{Range=8}` | 253-254 |
| 51 | （无 case → default） | |
| 52 | `WhiteBone` | 255-256 |
| 53 | `Shinsu` | 神兽（257-258） |
| 54 | `GiantLizard{RangeCooldown=5s}` | 259-260 |
| 55 | （无 case → default） | |
| 56 | `CorrosivePoisonSpitter{Green 毒,IgnoreShield}` | 261-262 |
| 57 | `CorrosivePoisonSpitter` | 263-264 |
| 58 | `Stomper` | 265-266 |
| 59 | `CrimsonNecromancer` | 267-268 |
| 60 | `ChaosKnight` | 269-270 |
| 61 | `PachontheChaosbringer` | 混沌帕坤 BOSS（271-272） |
| 62 | `NumaHighMage` | 273-274 |
| 63 | `NumaStoneThrower` | 275-276 |
| 64 | `Monkey` | 277-278 |
| 65 | `IcyGoddess{FindRange=3}` | 279-280 |
| 66 | `IcySpiritWarrior{Paralysis 毒,Rate=25}` | 281-282 |
| 67 | `IcySpiritGeneral{IgnoreShield}` | 283-288 |
| 68 | `Warewolf{IgnoreShield}` | 289-294 |
| 69 | `JinamStoneGate` | 295-296 |
| 70 | `FrostLordHwa` | 冰雪之主 BOSS（297-298） |
| 71 | `BanyoWarrior` | 299-300 |
| 72 | `BanyoCaptain` | 301-302 |
| 73 | （无 case → default） | |
| 74 | `BanyoLordGuzak{SpawnList=BanyoCaptain×2}` | 鼠王古扎克 BOSS（303-311） |
| 75/76 | `DepartedMonster{MatureEarwig/GoldenArmouredBeetle}` | 312-323 |
| 77 | `EnragedLordNiJae{Millipede,MaxMinions=200}` | 狂暴尼才 BOSS（324-330） |
| 78 | `JinchonDevil` | 真天魔 BOSS（331-332） |
| 79 | `GiantLizard{Range=10,Cooldown=5s}` | 333-334 |
| 80 | `SunFeralWarrior{FlameDemon 系}` | 335-344 |
| 81 | `MoonFeralWarrior` | 345-349 |
| 82 | `OxFeralGeneral{IgnoreShield}` | 350-355 |
| 83 | `FlameDemon{Min=-2,Max=2}` | 356-362 |
| 84 | `WingedHorror{RangeChance=1}` | 有翼恐魔 BOSS（363-368） |
| 85 | `EmperorSaWoo{Paralysis 毒,Rate=8}` | 369-370 |
| 86 | `FlameDemon{Passive,Min=0,Max=8}` | 371-378 |
| 87 | `OmaWarlord{Abyss 毒}` | 379-387 |
| 88 | `GoruSpearman` | 388-392 |
| 89 | `GoruArcher{Silenced 毒}` | 393-402 |
| 90 | `OmaWarlord{Paralysis 毒,Rate=25}` | 403-411 |
| 91 | `EnragedArchLichTaedu{Red 毒,SpawnList=Goru 系}` | 狂暴大法老 BOSS（412-431） |
| 92 | `GiantLizard{Range=9}` | 432-433 |
| 93 | `EscortCommander` | 434-435 |
| 94/95 | `FieryDancer{95:Paralysis 毒}` | 436-446 |
| 96 | `QueenOfDawn` | 黎明女王 BOSS（447-448） |
| 97 | `SonicLizard{IgnoreShield,Range=5}` | 449-450 |
| 98/100 | `YumgonWitch{98:Lightning AoE}` | 451-456、466-470 |
| 99 | `JinhwanSpirit{SpawnList=自身×1}` | 457-465 |
| 101 | `DragonQueen{DragonLord 绑定,8 类小怪}` | 龙后 BOSS（471-488） |
| 102 | `DragonLord{8 类×10000+DragonLord×1}` | 龙王 BOSS（489-506） |
| 103 | `InfernalSoldier{Range=5}` | 507-508 |
| 104 | `FerociousIceTiger` | 509-510 |
| 105 | `GiantLizard{Range=5,IgnoreShield,CanPvPRange}` | 511-512 |
| 106 | `GiantLizard{Range=7,CanPvPRange}` | 513-514 |
| 107-110 | `SamaFireGuardian`/`Ice`/`Lightning`/`Wind` | 沙玛守护（515-522） |
| 111-114 | `SamaPhoenix`/`Black`/`Blue`/`White` | 沙玛四色（523-530） |
| 115 | `SamaProphet{SpawnList=SamaSorcerer}` | 沙玛先知 BOSS（531-539） |
| 116 | `SamaScorcer` | 540-544 |
| 117 | `BanyoWarrior{DoubleDamage}` | 545-546 |
| 118 | `OmaMage` | 547-548 |
| 119 | `MonsterObject{Silenced 毒}` | 549-558 |
| 120 | `DoomClaw` | 559-563 |
| 121 | `PinkBat` | 564-565 |
| 122 | `QuartzTurtleSub{MiniTurtle×2}` | 566-574 |
| 123 | `Larva{Range=3}` | 575-580 |
| 124 | `QuartzTree{SubBoss+蝙蝠/水晶}` | 石英树 BOSS（581-593） |
| 125 | `CarnivorousPlant{HideRange=1,FindRange=1}` | 594-595 |
| 126 | `MonasteryBoss{Sacrifice×1}` | 596-604 |
| 127 | `JinchonDevil{CastDelay=8s,死亡毒云加时}` | 605-606 |
| 128 | `Doll` | 607-608 |
| 129 | `Tornado{Passive}` | 609-610 |
| 130 | `UndeadSoul` | 611-612 |
| 131/132 | `Terracotta{132:CanPhase}` | 兵马俑（613-616） |
| 133 | `TerracottaSub{Paralysis 毒}` | 617-625 |
| 134 | `TerracottaBoss{Paralysis 毒}` | 626-634 |
| 135 | `MonsterObject{Passive}` | 野马（635-636） |
| 1001/1002/1003 | `CastleFlag`/`CastleGate`/`CastleGuard` | 攻城物件（638-643） |

（SpawnList 权重即随机权重；`SEnvir.GetMonsterInfo(SpawnList)` 按权重抽怪，见 `MonsterObject.cs:1284`。）

## MonsterInfo 全字段（`LibraryCore/SystemModels/MonsterInfo.cs:6-305`）

| 字段 | 行号 | 类型 | 语义 |
|---|---|---|---|
| `MonsterName` | 8-22 | string `[IsIdentity]` | 怪名（表主键标识） |
| `Image` | 24-37 | `MonsterImage` | 客户端图库形象（决定渲染库+Shape） |
| `AI` | 39-52 | int | 行为编号（上表） |
| `Level` | 54-67 | int | 等级（命中潜行判定、经验、驯服） |
| `ViewRange` | 69-82 | int | 视野（默认 7，`OnCreated` 281 行） |
| `CoolEye` | 84-97 | int | 0-100 概率"火眼"：出生掷骰（`MonsterObject.cs:703`），命中者可看破隐身/潜行 |
| `Experience` | 99-112 | decimal | 基础经验值 |
| `Undead` | 114-127 | bool | 亡灵（受圣言/圣系加成，客户端图标） |
| `CanPush` | 130-143 | bool | 可否被推挤（默认 true，279 行） |
| `CanTame` | 145-158 | bool | 可否驯服 |
| `AttackDelay` | 161-174 | int | 攻击间隔 ms（默认 2500） |
| `MoveDelay` | 176-189 | int | 移动间隔 ms（默认 1800） |
| `IsBoss` | 191-204 | bool | BOSS 标志（详见 boss-mechanics.md） |
| `Flag` | 206-219 | `MonsterFlag` | 特殊怪标记（SpawnList 绑定用） |
| `FaceImage` | 221-234 | int | 对话头像图 |
| `MonsterInfoStats` | 254-255 | DBBindingList | 属性明细（下） |
| `Respawns` | 257-259 | DBBindingList | 关联刷怪点 |
| `Drops` | 261-263 | DBBindingList | 掉落表 |
| `Events` | 265-267 | DBBindingList | 怪物事件触发器 |
| `QuestDetails` | 269-271 | DBBindingList | 任务击杀明细 |
| `Stats` | 273 | `Stats` | 由 `StatsChanged()`（293-298）把 MonsterInfoStats 累加而成 |

`MonsterInfoStat`（`LibraryCore/SystemModels/MonsterInfoStat.cs:5-55`）：`Monster`（关联）+ `Stat`（`LibraryCore/Stat.cs` 枚举，Health/MinDC/Agility/…) + `Amount` 三元组；怪物 `RefreshStats()` 第一行就 `Stats.Add(MonsterInfo.Stats)`（`MonsterObject.cs:713-714`），随后叠加召唤等级 10%/级（721-742）、成长等级 10%/级（744-758）、buff、地图倍率（818-824）等。

## MonsterFlag（原计划文档称 MirMonType —— 不存在）

全库检索 `MirMonType` 仅命中 `docs/` 下的规划文档与 `_index.md`，**代码中无此枚举**；实际生效的是 `MonsterFlag`（`LibraryCore/Enum.cs:2140-2211`）。它不是"主动/被动/不移动"这类 AI 开关——AI 行为全部由 `MonsterInfo.AI` 编号决定；`MonsterFlag` 的唯一用途是给 `GetMonster` 工厂里 BOSS 的 `SpawnList` **按标志查 MonsterInfo**（如 `[SEnvir.MonsterInfoList.Binding.First(x => x.Flag == MonsterFlag.Larva)] = 1`，`MonsterObject.cs:161-165`），以及 `SamaProphet` 判断 SamaSorcerer/BloodStone 是否在场（`SamaProphet.cs:39、72`）。全部取值：None=0、Skeleton=1、JinSkeleton=2、Shinsu=3、InfernalSoldier=4、CursedDoll=5、SummonPuppet=6、MirrorImage=7、Tornado=8、UndeadSoul=9、CastleObjective=10、CastleDefense=11、Blocker=20、Larva=100、LesserWedgeMoth=110、ZumaArcher/Guardian/Fanatic/Keeper=120-123、BoneArcher/Captain/Bladesman/Soldier/SkeletonEnforcer=130-134、MatureEarwig/GoldenArmouredBeetle/Millipede=140-142、FerociousFlameDemon/FlameDemon=150-151、GoruSpearman/Archer/General=160-162、DragonLord=170、OYoungBeast/YumgonWitch/MaWarden/MaWarlord/JinhwanSpirit/JinhwanGuardian/OyoungGeneral/YumgonGeneral=171-178、BanyoCaptain=180、SamaSorcerer/BloodStone=190-191、Quartz 系=200-205、Sacrifice=210（`Enum.cs:2142-2210`）。

被动/主动/不移动等语义的真实落点：`Passive` 字段由 AI 分支注入（AI 1/2/86/129/135）；`CanMove` 由类决定（`Guard.CanMove => false`，`Guard.cs:11`；`ZumaGuardian.CanMove => base.CanMove && Visible`，`ZumaGuardian.cs:11`）。

## 经验与刷新

### 刷新：RespawnInfo → SpawnInfo.DoSpawn（`ServerLibrary/Models/Map.cs:391-472`）

`RespawnInfo`（`LibraryCore/SystemModels/RespawnInfo.cs:6-155`）字段：`Monster`+`Region`（联合主键）、`EventSpawn`（仅事件触发）、`Delay`（分钟）、`Count`（数量）、`DropSet`（掉落组掩码）、`Announce`（BOSS 公告刷新）、`EasterEventChance`、`RespawnIndex`（实例刷怪槽位对齐）。

每秒一次的 `DoSpawn(false)`：

```csharp
if (CurrentMap.RespawnIndex != Info.RespawnIndex) return;      // 实例槽位不匹配不刷

if (!eventSpawn)
{
    if (Info.EventSpawn || SEnvir.Now < NextSpawn) return;

    if (Info.Delay >= 1000000)                                  // 每日定点：1000000 + 一天中的分钟数
    {
        TimeSpan timeofDay = TimeSpan.FromMinutes(Info.Delay - 1000000);
        if (LastCheck.TimeOfDay >= timeofDay || SEnvir.Now.TimeOfDay < timeofDay) { ...return; }
    }
    else
    {
        if (Info.Announce)
            NextSpawn = SEnvir.Now.AddSeconds(Info.Delay * 60);                         // 公告怪：固定间隔
        else
            NextSpawn = SEnvir.Now.AddSeconds(SEnvir.Random.Next(Info.Delay * 60) + Info.Delay * 30); // 普通：随机 50%~150% 间隔
    }
}

int spawnCount = Info.Count;
if (!Info.Monster.IsBoss && CurrentMap.Info.Dungeon != null)    // 副本非 BOSS 怪按 SpawnMultiplier 增量
    spawnCount = (int)Math.Ceiling(Math.Clamp(Info.Count * CurrentMap.Info.Dungeon.SpawnMultiplier, 0M, int.MaxValue));

for (int i = AliveCount; i < spawnCount; i++)                   // 只补差额
{
    MonsterObject mob = MonsterObject.GetMonster(Info.Monster);
    if (!Info.Monster.IsBoss) { /* 万圣/圣诞活动怪替换 */ }
    mob.SpawnInfo = this;
    if (!mob.Spawn(Info.Region, CurrentMap.Instance, CurrentMap.InstanceSequence)) { mob.SpawnInfo = null; continue; }

    if (Info.Announce)   // 全服公告（Map.cs:455-467）
    {
        if (Info.Delay >= 1000000)
            foreach (SConnection con in SEnvir.Connections)
                con.ReceiveChat($"{mob.MonsterInfo.MonsterName} has appeared.", MessageType.System);
        else
            foreach (SConnection con in SEnvir.Connections)
                con.ReceiveChat(string.Format(con.Language.BossSpawn, CurrentMap.Info.Description), MessageType.System);
    }
    mob.DropSet = Info.DropSet;
    AliveCount++;
}
```

死亡侧闭环：`Die()` 里 `SpawnInfo.AliveCount--`，`AliveCount==0` 触发 `MONSTERCLEAR` 事件（`MonsterObject.cs:2534-2542`）；`Process()` 里 `DeadTime`（`Die` 时 = `Now + Config.DeadDuration` 即 1 分钟；有掉落待收割再 + `HarvestDuration` 5 分钟，2527-2530）到期 `Despawn`。

### 经验：YieldReward（`MonsterObject.cs:2549-2689`）

- 前提：`EXPOwner != null && PetOwner == null`（宠物杀怪不给经验归属结算）。
- 单人：`exp = min(MonsterInfo.Experience × eRate, 500000000)`，`eRate = 1 + ExtraExperienceRate` 再乘地图经验倍率 `1 + MapExperienceRate/100` 与成长等级 `1 + GrowthLevel*10/100`（2635-2641）。
- 组队：只统计**同图 18 格内**的队友；四职业均衡加成——掉落率 dRate 按"最稀有职业数"×1.1/1.2/1.3，经验 eRate 按"存活最稀有职业数"×1.1/1.25/1.5（2608-2632）；`exp += exp * 0.06M * ePlayers.Count`（每人 +6%，2661），按 `player.Level / totalLevels` 加权分配（2665）。
- 掉落同函数末尾调用 `Drop(EXPOwner/dPlayers...)`（2679-2688）。
- **等级差惩罚已禁用**：`GainExperience(exp, PlayerTagged, Level)` 的第三参 `gainLevel` 对应的惩罚逻辑整段被注释（`PlayerObject.cs:2048-2060`，注释内为 60 级以上每级 -10% 上限 90% / 60 级以下每级 -6% 上限 30%）——现行版本高低打低级怪**不衰减**。实际加成只有：经验率 stat、转生每次减半（2044-2045）。

## GodotClient 现状

怪 AI 本身是服务端权威，客户端只消费广播包；以下为 Godot 客户端对怪物相关协议/表现的移植情况（均已实际检索 `GodotClient/`）：

| 功能 | 状态 | 依据 |
|---|---|---|
| 怪物对象生成（S.ObjectMonster） | 已移植 | `GodotClient/Scripts/GameScene.cs:1136、2495-2500`：`OnObjectMonster` → `ObjectRenderer.CreateMonster(p)`，按 `MonsterIndex` 查 `MonsterInfo` 再按 `Image` 查渲染库 |
| MonsterImage→图库映射 | 已移植 | `GodotClient/Formats/MonsterLookup.cs:14-156`（含 Shinsu 的 Extra 双形态注释 13、153） |
| 怪物移动/转向（S.ObjectMove/ObjectTurn） | 已移植 | `GodotClient/Network/ServerConnection.cs:291、298`（PendingMoves/PendingTurns 队列）、`GameScene.cs:1527` 订阅 |
| 攻击动画（S.ObjectAttack/ObjectRangeAttack/ObjectMagic 含怪物施法） | 已移植 | `ServerConnection.cs:184-186`、`GameScene.cs:1282-1284、3000、3059、3138` |
| 雕像唤醒显隐（S.ObjectShow/ObjectHide，ZumaGuardian 雕像态） | 已移植 | `GameScene.cs:1088、2117-2118`（`SetObjectVisibility`）；`ObjectRenderer.cs:284-286` 按 `MonsterExtra` 选 StoneStanding/Standing 动画 |
| 怪物血条（受击 5 秒显示 + 开关） | 已移植 | `GodotClient/Scripts/MapObjectNode.cs:30-33、313-319`；`GameScene.cs:4052-4088`（OnHealthChanged）；`GameScene.cs:4153-4157`（DataObjectMonster 权威血量） |
| 悬停怪物信息窗（等级/血量/属性/元素/驯服/亡灵图标） | 已移植 | `GodotClient/Controls/MonsterDialog.cs:7-8、81-166` |
| AI 编号的客户端用途（Guard 不可攻击选中） | 已移植 | `GodotClient/Scripts/CombatController.cs:79、267`：`target.MonsterInfo?.AI >= 0` 才允许攻击（AI=-1 城卫排除） |
| 宠物归属显示（S.ObjectPetOwnerChanged） | 已移植 | `GameScene.cs:1091、2154-2159`；`ObjectRenderer.cs:583-584` 头顶 `(主人名)` |
| 屠宰（S.ObjectHarvest/ObjectHarvested、C.Harvest） | 已移植 | `GameScene.cs:1095、1277、2211-2221、2908-2922`；`ServerConnection.cs:1093` |
| BOSS 刷新公告（聊天 Announcement/System） | 部分移植 | 服务端公告走聊天通道（`Map.cs:459-465`）；客户端 `GameScene.cs:315-316` `ReceiveChat` → `ChatLogPanel`（`ChatLogPanel.cs:82-83` 保留系统/公告消息）可显示，但未做 BOSS 专属 UI（如大图标/音效） |
| 怪物音效 | 已移植（目录存在） | `GodotClient/Scripts/MonsterSoundCatalog.cs`（glob 可见；本文未逐行核对内容） |
| 怪物 GrowthLevel/成长显示 | 部分移植 | `MonsterDialog.cs:143-144` 成长图标；服务端 `Stats[Stat.GrowthLevel]` 路径（`MonsterObject.cs:744`）客户端仅在悬停窗展示 |
| 客户端本地 AI（巡逻/仇恨预测） | 未移植（本就不需要） | GodotClient 全库无 MonsterObject/ProcessSearch 等服务端逻辑；战斗解算全部信任服务端包 |

## 移植注意事项

1. **不要在客户端复刻 AI**：状态机是隐式的（时间戳+Target 判空），客户端只需要正确消费 `ObjectAttack/ObjectRangeAttack/ObjectMagic/ObjectShow/ObjectHide` 广播并按 `MonsterInfo.Image` 播动画；伤害一律等 `HealthChanged` 系列包。
2. **400ms/500ms 延迟结算**：服务端 `ActionList` 延迟打击（近战 400ms、弹道 400+距离×48ms，`MonsterObject.cs:1696-1701、1791-1796`）意味着客户端看到抬手后目标可能已移出——原版允许落空（`Attack` 结算 1709-1711 会因距离/死亡取消），Godot 端不要在抬手瞬间扣血。
3. **ViewRange 与 MaxViewRange 是两个量**：索敌用 `MonsterInfo.ViewRange`（默认 7），目标丢失/攻击校验用 `Config.MaxViewRange=18`；Abyss 毒会把视野压到 2（`MonsterObject.cs:101-104`）——做 debuff UI 时要体现"致盲"。
4. **AI 编号是内容约定**：新增怪物不写代码时走 default（普通主动怪）；`MonsterInfo.AI` 同时被 Godot 客户端 `CombatController.cs:79/267` 用来排除城卫（AI<0），改动编号语义会破坏两端契约。
5. **MoveTo 无寻路**：只做 8 向贪心旋转；地图设计（窄门/拐角）天然形成"卡怪"玩法，Godot 端做怪物移动预测时同样只允许 1 格直线步进，否则会与服务端位置漂移。
6. **经验公式以 `YieldReward` 为准**：组队四职业加成、+6%/人、等级加权、500M 单次上限都在服务端；等级差衰减已注释禁用（`PlayerObject.cs:2048-2060`），不要按旧传奇公式在客户端脑补显示经验。
7. **复活类怪**（`VoraciousGhost`：经验按复活次数减半、只有最后一次死亡才掉落，`VoraciousGhost.cs:14、37-42`）死亡包与真正消失之间有 3-8s 复活窗口，客户端 `ObjectDied` 后不要立刻回收节点资源，需等服务端 `Despawn` 移除广播。
8. **`MirMonType` 是文档侧误称**：检索 GodotClient/ServerLibrary 均无此枚举；对齐内容库时请以 `MonsterFlag` + `MonsterInfo.AI` 双键为准。
