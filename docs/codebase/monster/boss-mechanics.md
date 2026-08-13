# BOSS 机制全解（IsBoss 效果 / 代表 BOSS 技能循环 / 召唤 / 公告 / 掉落与复活）

## TL;DR 速查表

- `MonsterInfo.IsBoss`（`LibraryCore/SystemModels/MonsterInfo.cs:191-204`）不是"更强"的数值开关，而是**一组规则豁免/豁免免疫**：免毒素 DoT、免疫多种控制魔法、永不休眠、不吃副本刷怪倍率与活动怪替换、进 `SEnvir.BossList` 与 `Map.Bosses`。
- BOSS 行为没有统一基类：每个 BOSS 是 `MonsterObject`（或某中间类）的子类，靠 override `Process/ProcessTarget/Attack/Attacked/InAttackRange` + 时间戳字段（`SlaveTime/CastTime/TeleportTime…`）实现技能循环；技能原语（`LineAoE/FireWall/MassLightningBall/MassThunderBolt/MassCyclone/DeathCloud/DragonRepulse…`）全在 `MonsterObject` 基类（`ServerLibrary/Models/MonsterObject.cs:1783-2357`）。
- 阶段狂暴的通用范式是**血量分段**：`if (CurrentHP * MaxStage / Stats[Stat.Health] >= Stage) return; Stage--;`（`ArchLichTaedu.cs:39`、`ZumaKing.cs:37`）——掉一阶召一波小怪。
- 召唤的通用范式：定时（15-60s）先**处决跑远的小怪**（`mob.EXPOwner = null; mob.Die(); mob.Despawn();`）再 `SpawnMinions(保持数 - 现存数, 随机数, Target)` 补齐。
- BOSS 刷新公告由 `RespawnInfo.Announce` 驱动（与 IsBoss 无硬绑定）：固定间隔 + 全服 `BossSpawn` 聊天播报（`ServerLibrary/Models/Map.cs:413-414、455-467`）；`Delay >= 1000000` 表示每日定点刷新并播报怪名。
- 未找到"BOSS 死亡全服公告"实现：`Die()` 只触发 `MONSTERDIE`/`MONSTERCLEAR` 事件（`MonsterObject.cs:2532-2542`），无广播聊天。
- 掉落本身**没有 IsBoss 加成**：掉落概率公式对 BOSS 与普通怪完全一致（`MonsterObject.cs:2739`）；BOSS 的"专属掉落"来自 DB 掉落表 + `IsBoss && Drops.Count > 0` 进 `BossList`（`SEnvir.cs:562-568`）。
- 复活规则三条路径：`VoraciousGhost` 自身多次复活（经验减半、末次才掉落）；`DragonQueen` 死亡原地召唤 `DragonLord`（二阶段）；`QuartzTree` 血量 <1/4 召出子 BOSS。

## 职责概述

BOSS 是 `ServerLibrary/Models/Monsters/` 下约 101 个特殊怪类的子集，本文聚焦其中机制最典型的 14 个类/族。服务端把"BOSS 性"拆成两层：① `MonsterInfo.IsBoss` 布尔——触发全局规则（免伤/免疫/调度/追踪）；② 每个类的专属 override——技能循环、阶段切换、召唤、瞬移。本文先穷举 IsBoss 的全部服务端用法，再逐 BOSS 照抄技能公式，最后给公告/掉落/复活规则与 GodotClient 现状。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| `LibraryCore/SystemModels/MonsterInfo.cs` | 191-204 | `IsBoss` 字段定义 |
| `ServerLibrary/Envir/SEnvir.cs` | 316、562-568 | `BossList`（IsBoss 且有掉落）构建 |
| `ServerLibrary/Models/Map.cs` | 242-244、265-267 | `Map.Bosses` 增删 |
| `ServerLibrary/Models/Map.cs` | 391-472 | `SpawnInfo.DoSpawn`：BOSS 刷怪豁免 + Announce 公告 |
| `ServerLibrary/Models/MapObject.cs` | 289-297 | BOSS 免疫毒素 DoT 伤害 |
| `ServerLibrary/Models/MapObject.cs` | 1047-1052 | `BossTracker` stat 跨屏看 BOSS 数据 |
| `ServerLibrary/Models/MonsterObject.cs` | 881-898 | BOSS 永不 DeActivate |
| `ServerLibrary/Models/MonsterObject.cs` | 1278-1300 | `SpawnMinions` 召唤原语 |
| `ServerLibrary/Models/MonsterObject.cs` | 1783-2357 | BOSS 共用技能原语区 |
| `ServerLibrary/Models/Monsters/DragonLord.cs` | 8-105 | 龙王：15s 召唤潮 + 全屏随机落雷溅射 |
| `ServerLibrary/Models/Monsters/DragonQueen.cs` | 8-56 | 龙后：死亡转阶段召唤龙王 |
| `ServerLibrary/Models/Monsters/BanyoLordGuzak.cs` | 6-48 | 鼠王：群体净化 + 45s 卫队长 |
| `ServerLibrary/Models/Monsters/PachontheChaosbringer.cs` | 6-56 | 混沌帕坤：瞬移贴脸 + 龙震 |
| `ServerLibrary/Models/Monsters/EmperorSaWoo.cs` | 8-92 | 沙悟大帝：近远混招 + 龙卷 |
| `ServerLibrary/Models/Monsters/ArchLichTaedu.cs` | 9-106 | 大法老：7 阶段召唤 |
| `ServerLibrary/Models/Monsters/EnragedArchLichTaedu.cs` | 8-83 | 狂暴大法老：残血瞬移 + 背刺双倍 |
| `ServerLibrary/Models/Monsters/LordNiJae.cs` | 9-72 | 尼才：全屏藤蔓 + 双毒 |
| `ServerLibrary/Models/Monsters/EnragedLordNiJae.cs` | 6-37 | 狂暴尼才：30s 千足虫海（MaxMinions=200） |
| `ServerLibrary/Models/Monsters/ZumaKing.cs` | 6-95 | 祖玛教主：雕像唤醒 + 7 阶段 + 火墙/焦土 |
| `ServerLibrary/Models/Monsters/ZumaGuardian.cs` | 9-79 | 祖玛雕像潜伏基类（WakeAll 同族报警） |
| `ServerLibrary/Models/Monsters/SamaProphet.cs` | 7-94 | 沙玛先知：护体无敌 + 传送玩家 + 三系 AoE |
| `ServerLibrary/Models/Monsters/JinchonDevil.cs` | 9-107 | 真天魔：残血全屏死亡毒云 |
| `ServerLibrary/Models/Monsters/FrostLordHwa.cs` | 7-141 | 冰雪之主：按星期换属性 + 每分钟放逐 |
| `ServerLibrary/Models/Monsters/QueenOfDawn.cs` | 9-105 | 黎明女王：每分钟全图散布 + 瞬移背刺 |
| `ServerLibrary/Models/Monsters/QuartzTree.cs` | 9-68 | 石英树：1/4 血召子 BOSS + 不回血 |
| `ServerLibrary/Models/Monsters/UmaKing.cs` | 6-62 | 蛟王族基类：1/5 概率远程技 |
| `ServerLibrary/Models/Monsters/WingedHorror.cs` | 6-25 | 有翼恐魔：全远程化蛟王 |
| `ServerLibrary/Models/Monsters/VoraciousGhost.cs` | 8-53 | 复活怪原型（BOSS 复活规则参考） |

## IsBoss 标志的全部效果（ServerLibrary 全量检索）

### 1. 免疫与豁免

| 效果 | 位置 | 原文 |
|---|---|---|
| **免疫毒素 DoT** | `MapObject.cs:289-297` | `if (Race == ObjectType.Monster && ((MonsterObject)this).MonsterInfo.IsBoss) damage = 0;`——所有毒 tick 伤害直接归零（普通怪尚可被毒磨死，BOSS 不行） |
| 免疫刺客·深渊（Abyss 缠绕） | `Magics/Assassin/Abyss.cs:34` | `target.Race == ObjectType.Monster && ((MonsterObject)target).MonsterInfo.IsBoss` → 提示无效 |
| 免疫Chain 锁链 | `Magics/Assassin/Chain.cs:33、58` | 主目标与链式传播均跳过 BOSS |
| 免疫 Hemorrhage 出血 | `Magics/Assassin/Hemorrhage.cs:50` | BOSS 直接 return |
| 免疫 Massacre 屠杀 | `Magics/Assassin/Massacre.cs:36` | 范围内 BOSS 跳过 |
| 免疫诅咒娃娃（CursedDoll） | `Magics/Taoist/CursedDoll.cs:30` | `mon.MonsterInfo.IsBoss` 不可选为目标 |
| 免疫 Infection 感染 | `Magics/Taoist/Infection.cs:27` | 寄生毒不上 BOSS |
| 免疫战士·Beckon 拉拽 | `Magics/Warrior/Beckon.cs:65` | `mob.MonsterInfo.IsBoss || !mob.MonsterInfo.CanPush` → 不拉 |
| 免疫圣言驯服（ElectricShock） | `Magics/Wizard/ElectricShock.cs:51` | `if (ob.MonsterInfo.IsBoss) return;`——BOSS 不可驯 |
| 免疫 ExpelUndead 驱逐 | `Magics/Wizard/ExpelUndead.cs:44` | `ob.MonsterInfo.IsBoss || ob.Level >= 70` |
| 免疫 FrostBite 霜咬 | `Magics/Wizard/FrostBite.cs:67` | 范围内 BOSS 跳过 |
| 免疫玩家的减速/沉默触发 | `PlayerObject.cs:15624、15659` | `!((MonsterObject)ob).MonsterInfo.IsBoss` 才会上 Slow/Silenced 毒 |
| 因果 Karma 伤害特判 | `PlayerObject.cs:15355-15357` | 对 BOSS `karmaDamage = karma.Magic.GetPower() * 20;`（否则 `karmaDamage /= 4`）——Karma 反而对 BOSS 是强化 |
| 活动怪替换/清场豁免 | `Map.cs:433`、`ChristmasMonster.cs:74`、`HalloweenMonster.cs:69`、`DragonLord.cs:32` | 万圣/圣诞活动怪不替换 BOSS；BOSS 清小怪时跳过其他 BOSS |

### 2. 调度与追踪

- **永不休眠**：`Activate/DeActivate`（`MonsterObject.cs:885、894`）两个条件里都有 `MonsterInfo.IsBoss`——即便全图无玩家，BOSS 仍留在 `SEnvir.ActiveObjects` 每 tick 处理（保证召唤/阶段技能在外人不在场时也推进）。
- **`Map.Bosses` 列表**：`Map.AddObject/RemoveObject`（`Map.cs:242-244、265-267`）维护本图活着的 BOSS 集合（未见游戏逻辑消费此列表，疑为预留/调试用途——`未找到实现，疑在攻城或后续功能`）。
- **`SEnvir.BossList`**：开服 `LoadDatabase` 收集 `IsBoss && Drops.Count > 0` 的 MonsterInfo（`SEnvir.cs:562-568`）。消费点：①"怪物召唤令"类物品 `Stat.MapSummoning` 随机召一只 `<300` 级 BOSS（`PlayerObject.cs:6843-6853`）；②万圣/圣诞活动怪变形查表（`HalloweenMonster.cs:151`）。
- **BossTracker 追踪**：玩家带 `Stat.BossTracker > 0`（物品 stat）即可在 `CanDataBeSeenBy` 里看到同图 BOSS 的 `DataObjectMonster` 血量数据（`MapObject.cs:1047-1052`）——BOSS 猎人戒指类道具的实现。
- **副本倍率豁免**：`DoSpawn` 里 `!Info.Monster.IsBoss && Dungeon != null` 才应用 `SpawnMultiplier`（`Map.cs:423`）——BOSS 在副本中不翻倍。

## 通用技能原语（MonsterObject 基类，`MonsterObject.cs:1783-2357`）

BOSS 子类全部复用这些方法，签名与关键参数：

| 方法 | 行号 | 效果 |
|---|---|---|
| `AttackMagic(magic, element, travel, damage)` | 1783-1797 | 单目标施法包，延迟 `500ms + (travel ? 距离×48ms : 0)` 结算 |
| `AttackAoE(radius, magic, element, damage)` | 1799-1818 | 以 Target 为中心 radius 格内所有可攻击目标，各延迟 500ms |
| `SamaGuardianFire()` | 1819-1838 | 以 Target 为中心 5 格火 AoE |
| `LineAoE(distance, min, max, magic, element)` | 1839-1962 | 以自身朝向为轴，偏移 min..max 条线各 distance 格；主线全额 DC，两侧邻格 `GetDC() / 2`；逐格延迟 `500 + i*75ms`（波推进感） |
| `FireWall()` | 1964-2010 | 在 20 格内随机目标脚下 + 四方向共 5 格铺 FireWall SpellObject（15 tick × 2s） |
| `DeathCloud(location)` | 2042-2057 | location 周围 0-2 格铺 MonsterDeathCloud（每格延迟 500ms，只有中心 visible） |
| `MassLightningBall()` | 2075-2111 | 自身 ±20 格四条边随机落点 + 全视野目标延迟打击（闪电球雨） |
| `MassThunderBolt()` | 2112-2150 | 全视野每格 50% 概率雷击目标（空格 1/50 概率播特效点） |
| `MonsterThunderStorm(damage)` | 2166-2184 | 自身周围 2 格雷 AoE |
| `MassCyclone()` | 2229-2266 | 全视野 3/4 概率风系打击（龙卷风） |
| `PoisonousCloud()` | 2293-2322 | 自身周围 2 格毒云 SpellObject（20s tick） |
| `DragonRepulse()` | 2324-2357 | 给自己挂 6s DragonRepulse buff（每 0.5s tick：攻击+推离周围目标） |
| `SpawnMinions(fixed, random, target)` | 1278-1300 | `count = min(MaxMinions - MinionList.Count, Random(random+1) + fixed)`；按 `SpawnList` 权重抽 MonsterInfo → `GetMonster` → `SpawnMinion` → `mob.Target = target; mob.Master = this` |

## 代表 BOSS 逐个解析

### 1. DragonLord 龙王（AI 102，`DragonLord.cs:8-105`）

- **攻击距离**：`AttackRange = 12`，`InAttackRange = 同图 && Chebyshev ≤ 12`（12-17 行）——几乎全屏即"近战"。
- **召唤潮（15s 循环）**（`Process`，19-40 行）：先处决 15 格外的非 BOSS 小怪（27-37 行），再 `SpawnMinions(10 - MinionList.Count, 0, Target)` 保持 10 只（SpawnList 为 8 族小怪各权重 10000 + 自身 ×1，`MonsterObject.cs:489-506`）；在自身 10 格内随机点落地（42-45 行）。
- **普攻 = 全屏随机溅射**（`Attack`，75-103 行）：

```csharp
foreach (MapObject target in GetTargets(CurrentMap, CurrentLocation, AttackRange))
{
    if (SEnvir.Random.Next(10) > 3) continue;          // 40% 选中

    packet.Targets.Add(target.ObjectID);

    foreach (MapObject attackTarget in GetTargets(CurrentMap, target.CurrentLocation, 2))   // 命中点周围 2 格溅射
    {
        ActionList.Add(new DelayedAction(
            SEnvir.Now.AddMilliseconds(1000),          // 1s 后落地
            ActionType.DelayAttack, attackTarget, GetDC(), AttackElement));
    }
}
```

### 2. DragonQueen 龙后（AI 101，`DragonQueen.cs:8-56`）

- 继承 `YumgonWitch`（远程 10 格 + 1/5 AoE），`AoEElement` 固化为自身攻击元素（13-17 行）。
- 同款 15s 召唤潮保持 10 只（20-43 行），SpawnList 8 族各权重 2（`MonsterObject.cs:471-488`）。
- **死亡转阶段**（`Die`，45-54 行）：死后原地 `GetMonster(DragonLordInfo).Spawn(CurrentMap, CurrentLocation)`——龙后倒下龙王立刻现身，公告绑定在各自的 RespawnInfo 上。

### 3. BanyoLordGuzak 鼠王古扎克（AI 74，`BanyoLordGuzak.cs:6-48`）

- 继承 `PachontheChaosbringer`（瞬移 + 龙震，见下）。
- **群体净化（20s）**（17-23 行）：`foreach (GetTargets(CurrentMap, CurrentLocation, Config.MaxViewRange)) Purify(ob);`——给 18 格内所有敌对目标上 Purification（驱散它们身上的增益，走 `MonsterObject.cs:2186-2199` 的 DelayMagic）。
- **卫队长（45s）**（25-41 行）：处决 10 格外小怪后 `SpawnMinions(5 - MinionList.Count, 0, Target)`；SpawnList 只有 `BanyoCaptain ×2`（`MonsterObject.cs:303-311`）。
- **父类 PachontheChaosbringer**（`PachontheChaosbringer.cs:18-54`）：目标距离 >8 且 10s 冷却到 → 找目标相邻 8 向空格**瞬移贴脸**（22-45 行）；`InAttackRange`（≤2 格）且 20s 冷却到 → `DragonRepulse()` 龙震（46-51 行）。

### 4. EmperorSaWoo 沙悟大帝（AI 41/85，`EmperorSaWoo.cs:8-92`）

- `ProcessTarget`（11-48 行）：不在攻击范围时 `1/3` 概率 `RangeAttack()`（19-21 行）；在范围内时 `4/5` 概率普攻、`1/5` RangeAttack（42-47 行）。
- `RangeAttack`（67-90 行）：`1/3` 概率 `MassCyclone()` 全屏龙卷；否则对**目标周围 2 格**群体延迟 400ms 打击。
- AI 85 变体附带麻痹毒（`PoisonType.Paralysis, PoisonTicks=1, PoisonFrequency=5, PoisonRate=8`，`MonsterObject.cs:369-370`）。

### 5. ArchLichTaedu 大法老（AI 43，`ArchLichTaedu.cs:9-106`）

- **7 阶段血量分段召唤**（11-15、32-48 行）：

```csharp
if (CurrentHP * MaxStage / Stats[Stat.Health] >= Stage || Stage <= 0) return;

Stage--;                                              // 每掉 1/7 血触发一次

ActionTime += TimeSpan.FromSeconds(1);                // 自我硬直 1s（可被打）

Broadcast(new S.ObjectShow { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });  // 换形态广播

SpawnMinions(MinSpawn, RandomSpawn, Target);          // 20 固定 + 0~5 随机（MaxMinions=50）
```

- 构造器 `AvoidFireWall = false; MaxMinions = 50;`（18-22 行）——大法老**不躲火墙**，站撸法师。
- SpawnList 为骨系五族（BoneArcher 90 / BoneSoldier、BoneBladesman、BoneCaptain 各 15 / SkeletonEnforcer 1，`MonsterObject.cs:224-236`）。
- `ProcessTarget`（51-88 行）：远距离 `1/2` 概率单发 RangeAttack（400ms 延迟），近身 `4/5` 普攻。

### 6. EnragedArchLichTaedu 狂暴大法老（AI 91，`EnragedArchLichTaedu.cs:8-83`）

- **残血瞬移脱身（一次性）**（11-22 行）：`Attacked` 里 `CurrentHP > Stats[Stat.Health] / 2` 之外（即血量 ≤50%）且 `CanTeleport` → `TeleportNearby(7, 12)` 跳出包围圈。
- **瞬移背刺**（`ProcessTarget`，27-61 行）：与目标距离 >1 时每秒 `1/7` 概率瞬移到目标相邻格并 `Bonus = true`；`Attack`（62-81 行）里 `if (Bonus) damage *= 2;` 后清零——瞬移后的下一击双倍。
- GetMonster 注入（`MonsterObject.cs:412-431`）：`MinSpawn=5, RandomSpawn=5`（阶段召唤量比普通版少）、Red 毒（TickFrequency 25s）、SpawnList = Goru 三族。

### 7. LordNiJae / EnragedLordNiJae 尼才族（AI 13/77）

- `LordNiJae`（`LordNiJae.cs:9-72`）继承 `CarnivorousPlant`（植物系原地怪），`InAttackRange = 同图 && ≤ Globals.MagicRange`（17-20 行）；`Attack`（22-39 行）对 MagicRange 内**所有**目标按距离逐个延迟 `500ms + 距离×48ms` 打击（藤蔓从脚下依次炸开）。
- **攻击附毒**（41-70 行）：命中后 `1/5` 绿毒（Value=GetSC()，2s×10 tick）、`1/10` 麻痹毒（5s×1）。
- `EnragedLordNiJae`（`EnragedLordNiJae.cs:11-35`）叠加 **30s 召唤潮**保持 5 只千足虫（Millipede），`MaxMinions = 200`（`MonsterObject.cs:324-330`）——配合阶段掉血可以铺满全屏。

### 8. ZumaKing 祖玛教主（AI 22，`ZumaKing.cs:6-95`）

- **雕像潜伏**：继承 `ZumaGuardian`——出生 `Visible=false`、免伤免毒、目标进入 3 格才 `Wake()` 并 `WakeAll(7)` 唤醒周围同族共享仇恨（`ZumaGuardian.cs:16-28、35-61`）；`ZumaKing.Wake` 额外 `ActionTime = Now + 2s`（24-29 行）。
- **7 阶段召唤**（31-41 行）：`if (CurrentHP * MaxStage / Stats[Stat.Health] >= Stage || Stage <= 0) return; Stage--; SpawnMinions(4, 8, Target);`（每掉 1/7 血召 4-12 只）。
- `RangeAttack`（82-93 行）：`1/3` 概率 `FireWall()`（目标区 5 格火海）；否则 `LineAoE(12, -2, 2, MagicType.MonsterScortchedEarth, Element.Fire)`——以朝向为中心 ±2 条线共 5 条、每条 12 格的焦土火浪。
- 构造器 `AvoidFireWall = false`（11-14 行）。

### 9. SamaProphet 沙玛先知（AI 115，`SamaProphet.cs:7-94`）

- **出生即带护法**：`OnSpawned` → `SpawnMinions(1, 0, Target)`（17-22 行，在自身左下 5 格内）。
- **护体无敌**（`Attacked`，64-76 行）：场上只要还有 `MonsterFlag.BloodStone` 或 `SamaSorcerer`，一切伤害 return 0——必须先杀光召唤物。
- **召回护法（10s）**（24-46 行）：扫描全图 Objects（注释自嘲 Expensive），把跑出 `ViewRange-3` 的 SamaSorcerer 传送回左下 2 格内。
- **抓人**（`ProcessTarget`，52-62 行）：`1/5` 概率把 5 格外的目标**传送到自己身边**再 RangeAttack。
- `RangeAttack`（78-92 行）：`1/3` 各系 AoE——`AttackAoE(15, SamaProphetFire, Fire)` / `(15, SamaProphetWind, Ice)` / `(15, SamaProphetLightning, Lightning)`。

### 10. JinchonDevil 真天魔（AI 78/127，`JinchonDevil.cs:9-107`）

- 攻击距离为**十字形 3 格**（14-26 行）：`|dx|<=3 && |dy|<=3 && (x==0 || x==y || y==0)`。
- **残血全屏死亡毒云**（`ProcessTarget`，33-52 行）：

```csharp
if (CanAttack && SEnvir.Now > CastTime)
{
    List<MapObject> targets = GetTargets(CurrentMap, CurrentLocation, ViewRange);
    if (targets.Count > 0)
    {
        foreach (MapObject ob in targets)
        {
            if (CurrentHP > Stats[Stat.Health] / 2 && SEnvir.Random.Next(2) > 0) continue;
            // 血量 > 1/2 时只对一半目标放；≤ 1/2 时全放（狂暴阈值）

            DeathCloud(ob.CurrentLocation);            // 每个目标脚下 3×3 毒云
        }
        ...
        CastTime = SEnvir.Now + CastDelay;             // 15s（AI 127 变体 8s，且死亡毒云持续 2-7s，MonsterObject.cs:605-606）
    }
}
```

- 普攻（78-104 行）：`1/3` 概率或目标距离 >2 时 `LineAttack(3)` 直线喷毒，否则近战单击。

### 11. FrostLordHwa 冰雪之主（AI 70，`FrostLordHwa.cs:7-141`）

- **按星期切换属性**（13-39 行）：构造器按 `SEnvir.Now.DayOfWeek` 把周一~周日映射到 Fire/Ice/Lightning/Wind/Holy/Dark/Phantom Affinity，`ApplyBonusStats` 里 `Stats[Affinity] = 1`（41-46 行）。
- **每分钟放逐**（`ProcessAI`，48-99 行）：60s 一次，把 18 格内所有目标**随机传送到自己周围 18 格**并连上 4 种毒：Abyss（10s，致盲视野→2）+ Silenced（10s）+ Red（10s，受伤 ×1.2）+ WraithGrip（5s，锁足）。
- **瞬移贴脸**（101-128 行）：目标 3 格外 5s 冷却瞬移到其相邻格。
- **秒杀召唤物**（131-135 行）：`if (Target.Race == ObjectType.Monster) { Target.SetHP(0); return; }`——对玩家的宝宝直接处决。

### 12. QueenOfDawn 黎明女王（AI 96，`QueenOfDawn.cs:9-105`）

- **每分钟全图散布**（`ProcessAI`，15-34 行）：`foreach (CurrentMap.Objects) if (CanAttackTarget(ob)) ob.Teleport(CurrentMap, CurrentMap.GetRandomLocation());`——把全场敌对目标（含玩家）随机撒到全图。
- **瞬移背刺**（35-66 行）：距离 >1 时每秒 `1/10` 概率瞬移到目标相邻格并 `Bonus = true`（注意：`Attack` 71-103 行并未消费 `Bonus`，疑未完成/预留）。
- 攻击（71-103 行）：`4/5` 单体 400ms 打击；`1/5` 目标为中心 2 格 AoE。

### 13. QuartzTree 石英树（AI 124，`QuartzTree.cs:9-68`）

- **1/4 血量召子 BOSS（一次性）**（22-26 行）：`if (!SubSpawned && CurrentHP < Stats[Stat.Health] / 4) { SubSpawned = true; SpawnSub(); }`——`SpawnSub` 用 `SubBossInfo`（QuartzTurtleSub）生成子 BOSS 并共享 Target（48-57 行）。
- **60s 召唤潮**保持 10 只（28-46 行，处决 30 格外小怪，比别的 BOSS 宽容），`ObjectShow` 广播换形态。
- **不回血**：`public override void ProcessRegen() { }`（64-66 行）——磨血战术有效。

### 14. UmaKing / WingedHorror 蛟王族（AI 16/84）

- `UmaKing`（`UmaKing.cs:6-62`）：`RangeChance = 5`；不在攻击距离时 `1/5` 概率先手远程（16-20 行）；近身时 `4/5` 普攻、`1/5` `RangeAttack()`（39-46 行）。`RangeAttack` = `1/3` `MassLightningBall()`（四边闪电球雨）否则 `MassThunderBolt()`（全图随机雷击）（49-60 行）。
- `WingedHorror`（`WingedHorror.cs:6-25`）：override `RangeAttack` 三选一——`MassLightningBall()` / `LineAoE(10, 0, 8, MagicType.LightningBeam, Element.Lightning)`（9 条 10 格雷电扇面）/ `MassThunderBolt()`；GetMonster 注入 `RangeChance = 1`（`MonsterObject.cs:363-368`）→ **每次都走远程分支**。

## BOSS 公告 / 掉落 / 复活规则

### 公告（刷新有、死亡无）

- 刷新公告完全由 `RespawnInfo.Announce` 控制（`Map.cs:455-467`）：`Delay < 1000000` 时 `NextSpawn = Now + Delay×60s` 固定间隔，每次刷新对全服 `con.ReceiveChat(string.Format(con.Language.BossSpawn, CurrentMap.Info.Description), MessageType.System)`（地图名模板）；`Delay >= 1000000`（每日定点）时播报 `$"{MonsterName} has appeared."`（460 行）。
- **未找到 BOSS 死亡公告实现**：`MonsterObject.Die()`（2510-2547 行）只有 `SEnvir.EventHandler.Process(this, "MONSTERDIE")` 与 `AliveCount==0` 时的 `"MONSTERCLEAR"`——死亡广播只能靠事件系统（`EventInfoHandler`）在 DB 里配动作，代码里没有硬编码。

### 掉落：公式与普通怪一致

`Drop(owner, players, rate)`（`MonsterObject.cs:2691-3028`）核心掷骰（2714-2765 行）：

```csharp
long amount = Math.Max(1, drop.Amount / 2 + SEnvir.Random.Next(drop.Amount));
...
chance = (long)(int.MaxValue / (drop.Chance * players) * rate);
...
var roll = SEnvir.Random.Next();
if (drop.PartOnly || ((roll > chance || owner.Character.Account.ItemBot) && ((long)userDrop.Progress <= userDrop.DropCount)))
{   // 未中奖 → 按物品碎片规则折算（PartCount、PartOnly）
```

对 BOSS 的间接"加成"只有三点：① 组队职业均衡 `dRate ×1.1/1.2/1.3`（`YieldReward`，2608-2619 行）；② `IsBoss && Drops.Count > 0` 进 `BossList` 供召唤令抽取；③ BOSS 通常配 `Announce` + `DropSet` 掉落组。**没有"BOSS 掉率翻倍"之类的代码路径**。

### 复活规则

| 类型 | 实现 | 位置 |
|---|---|---|
| 自身多次复活 | `VoraciousGhost`：出生随机 `ReviveCount = Next(4)`；死后 3-8s 原地复活，HP = `Max / 2^DeathCount`，经验 = `Experience / 2^ReviveCount`，只有 `ReviveCount == 0` 的最终死亡才 `Drop` | `VoraciousGhost.cs:14、22-52` |
| 死亡召唤下阶段 | `DragonQueen.Die()` 原地生成 `DragonLord` | `DragonQueen.cs:45-54` |
| 血量阈值召子 BOSS | `QuartzTree` HP<1/4 一次性 `SpawnSub()` | `QuartzTree.cs:22-26、48-57` |
| 常规重刷 | 尸体 1 分钟（可收割 +5 分钟）后 Despawn，`AliveCount--`，下一轮 `DoSpawn` 按间隔补刷 | `MonsterObject.cs:2527-2544`、`Map.cs:429-471` |

## GodotClient 现状

BOSS 没有客户端专属协议——一切表现复用普通怪通道；以下为 Godot 客户端对 BOSS 相关表现的覆盖情况（均实际检索 `GodotClient/`）：

| 功能 | 状态 | 依据 |
|---|---|---|
| BOSS 对象生成与渲染（S.ObjectMonster，按 Image 查库） | 已移植 | `GodotClient/Scripts/GameScene.cs:2495-2500`；`GodotClient/Scripts/ObjectRenderer.cs:52-77`（MonsterIndex→MonsterInfo→MonsterLookup） |
| BOSS 攻击/施法动画（ObjectAttack/ObjectRangeAttack/ObjectMagic） | 已移植 | `GodotClient/Network/ServerConnection.cs:184-186`；`GameScene.cs:1282-1284、3000、3059、3138`（含魔法特效表 `MagicEffectTable`） |
| 阶段切换播报（ObjectShow——大法老/祖玛教主换阶段、石英树召潮时广播） | 已移植 | `GameScene.cs:1088、2117-2118` `SetObjectVisibility`（BOSS 借此重设朝向/位置并重绘） |
| 祖玛雕像潜伏态（packet.Extra→StoneStanding 动画） | 已移植 | `ServerConnection.cs` ObjectShow 通道 + `ObjectRenderer.cs:284-286`（`MonsterExtra ? Standing : StoneStanding`） |
| BOSS 血条（受击 5 秒头顶条 + DataObjectMonster 权威血量） | 已移植 | `MapObjectNode.cs:313-319`；`GameScene.cs:4153-4157`；开关 `ClientSettings.ShowMonsterHealth`（`MapObjectNode.cs:318`） |
| BOSS 悬停属性窗（等级/血量/AC/MR/DC/元素/抗性/驯服/亡灵） | 已移植 | `GodotClient/Controls/MonsterDialog.cs:81-166` |
| BOSS 刷新公告（聊天框显示 BossSpawn 文本） | 部分移植 | 服务端文本经 `ReceiveChat`→`ChatLogPanel`（`GameScene.cs:315-316`、`ChatLogPanel.cs:82-83` 保留 System/Announcement）；无 BOSS 专属 UI（全屏横幅/音效/大血条均未实现） |
| BOSS 名字颜色（ObjectNameColour：Shock 棕 / Rage 红） | 已移植 | `GameScene.cs:1090` 订阅；服务端着色逻辑 `MonsterObject.cs:955-963` |
| 每日定点播报（"XXX has appeared."英文原文） | 未移植（无专属处理） | 直接走聊天通道显示英文原文，未本地化 |
| BossTracker 追踪（跨屏看 BOSS 血量） | 未移植 | GodotClient 无 `Stat.BossTracker` 消费点（grep 无匹配） |
| 死亡转阶段表现（龙后死→龙王现身的连续演出） | 部分移植 | 依赖通用 ObjectDied + 新 ObjectMonster 生成（`GameScene.cs:7504-7506` 缓冲队列），无专场过场演出 |

## 移植注意事项

1. **IsBoss 是服务端规则位，不是渲染位**：Godot 端不要按 IsBoss 改表现（除了可选的大血条/名称描边）；真正影响客户端的是 `Announce`（公告）与各类广播包。若要做"BOSS 大血条"，数据源用 `DataObjectMonster`（`GameScene.cs:4154-4157` 已就绪）+ `MonsterInfo.IsBoss`（`ObjectRenderer.cs:34` 持有 MonsterInfo，可直接判）。
2. **阶段技能的时序全部服务端驱动**：`ObjectShow`（换阶段）、`ObjectMagic`（AoE）、`ObjectSpell`（火墙/毒云地面物）按到达顺序播放即可，不要在客户端预测"血量到 X% 该放技能"——活动怪倍率、`ActionTime` 硬直都会改变服务端节奏。
3. **免伤规则在客户端表现为"数字不跳"**：BOSS 免毒 DoT（`MapObject.cs:291-292`）与 SamaProphet 护体（`SamaProphet.cs:72`）都是服务端 return 0，客户端只会看到 Miss/Block 或完全没有 HealthChanged——不要在客户端自行计算毒伤显示。
4. **召唤潮的小怪删除是三连**：`EXPOwner = null → Die() → Despawn()`（如 `DragonLord.cs:33-36`）会产生 ObjectDied + 对象移除两条流；Godot 端对象回收要以"从 `_objects` 移除"为准，不要在 ObjectDied 时立刻释放节点（ VoraciousGhost 类复活怪会 3-8s 后同 ObjectID 重现）。
5. **`Delay >= 1000000` 的每日定点刷新**：`Info.Delay - 1000000` = 一天中的分钟数，跨午夜判定用 `LastCheck.TimeOfDay >= timeofDay || Now.TimeOfDay < timeofDay`（`Map.cs:399-410`）。若 Godot 单机模式（`SinglePlayerLauncher` 拉真服务端）则自动继承；若自建简化服务端必须保留该语义，否则定点 BOSS 会每天多刷一次。
6. **群体技能的延迟梯度是设计的一部分**：`LineAoE` 的 `500 + i*75ms`、藤蔓的 `500 + 距离×48ms`（`MonsterObject.cs:1875、LordNiJae.cs:33`）制造"波浪推进"视觉；Godot 端若用统一帧触发会丢失躲技能的读招窗口。
7. **PachontheChaosbringer/EnragedArchLichTaedu 的"瞬移贴脸"模式**：瞬移 = `Teleport()`（服务端直接改 CurrentCell 并广播 ObjectRemove+ObjectAdd 等价物流），客户端收到后应瞬时跳位而非插值走路，否则会出现 BOSS"滑行穿墙"的视觉假象。
