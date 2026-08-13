# 元素攻防 / Buff / 毒素系统（Stat 聚合 + BuffType + PoisonType + 魔法盾）

## TL;DR 速查表

- 属性唯一容器是 `Stats`（`SortedDictionary<Stat,int>`），玩家最终属性由 `PlayerObject.RefreshStats()`（ServerLibrary/Models/PlayerObject.cs:2179）从 基础值→坐骑→装备/宝石→套装→魔法被动→buff 顺序累加，最后统一乘百分比。
- `Stat` 枚举共 ~150 个值（LibraryCore/Stat.cs:507-895），元素攻防 7 系（Fire/Ice/Lightning/Wind/Holy/Dark/Phantom）各一对 `XxxAttack`/`XxxResistance`，玩家抗性上限被硬编码为 ±5（PlayerObject.cs:2392-2399）。
- Buff 入口是 `MapObject.BuffAdd`（MapObject.cs:1403，注意：**没有 AddBuff，方法名是 BuffAdd**）；同类型 buff 先删旧再加新（不叠加，取最新）；`RemainingTime == TimeSpan.MaxValue` 表示永久；过期统一在 `MapObject.ProcessBuff`（MapObject.cs:413）里扣除并 `BuffRemove`。
- 毒素入口 `MapObject.ApplyPoison`（MapObject.cs:1642）：先掷 `Stat.PoisonResistance`（百分比抵抗，注意拼写不是 PoisionResist）；同类型毒旧 Value 更大则新毒被拒绝。
- 绿毒 `PoisonType.Green` 每 tick 掉 `poison.Value` 血（MapObject.cs:236-238）；红毒 `PoisonType.Red` 使受到伤害 ×1.2（PlayerObject.cs:15741-15742）；减速毒 `Slow` 每点 Value 增加 100ms 行动/攻击延迟（PlayerObject.cs:14750-14756）；麻痹 `Paralysis` 通过 `CanMove/CanAttack/CanCast` 位掩码判定（MapObject.cs:86-88）。
- 魔法伤害元素公式：`power += GetElementPower(race, XxxAttack) * 2; power -= power * XxxResistance / 10;`（PlayerObject.cs:15497-15532），负抗性同样走 `/10`（魔法路线）而物理普攻负抗走 `/5` 放大（PlayerObject.cs:15285-15288）。
- 魔法盾：`BuffType.MagicShield` 挂 `Stat.MagicShield = 50`（50% 减伤），被击时按 `power * 25ms`（玩家侧）/`power * 10ms`（怪物侧）扣盾时；`SuperiorMagicShield` 是按 `Mana * (0.25 + Level*0.05)` 的吸收池，吸收完才掉血。
- 冰冻（`BuffType.FrostBite`）：受击把伤害累积进 `Stat.FrostBiteDamage`，到期以储存值 AOE 爆发并对攻击者上 Slow 毒。
- GodotClient：buff 面板/图标、buff·毒特效、S.StatsUpdate、元素攻防显示均已移植；**所有数值公式均在服务端**，客户端只做展示与状态位判断。

## 职责概述

本文覆盖服务端战斗底层的三条公共管线，供 Godot 客户端对齐表现与状态同步：

1. **属性系统**：`LibraryCore/Stat.cs` 定义 `Stat` 枚举（每个值带 `StatDescription` 特性）、`StatType`/`StatSource`，以及 `Stats` 容器的合并规则；`PlayerObject.RefreshStats()` 是唯一的玩家属性聚合点。
2. **Buff 系统**：`BuffType` 枚举 + `BuffInfo`（MirDB 持久化对象）+ `MapObject.BuffAdd/BuffRemove/ProcessBuff` 生命周期，玩家侧 override 负责持久化与包同步。
3. **元素/毒素/减伤系统**：`Element` 七系 + `PoisonType` 位标志 + `ApplyPoison/ProcessPoison`，以及普攻（`PlayerObject.Attack`）、魔法（`PlayerObject.MagicAttack` 管线）、怪物攻击（`MonsterObject.Attack`）三条伤害公式中的元素加减伤、MagicShield/SuperiorMagicShield 减伤。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| LibraryCore/Stat.cs | 10-505 | `Stats` 容器：索引器、`Add()` 合并规则、元素取值工具（GetElementValue/GetResistanceValue/GetWeaponElement…） |
| LibraryCore/Stat.cs | 507-895 | `Stat` 枚举全量定义（含 `StatDescription` 特性） |
| LibraryCore/Stat.cs | 897-918 | `StatSource`（None/Added/Refine/Enhancement/Other）、`StatType`（None/Default/Min/Max/Percent/Text/AttackElement/ElementResistance/SpellPower/Time） |
| LibraryCore/Stat.cs | 920-930 | `StatDescription` 特性：Title/Format/Mode/MinStat/MaxStat/UsageHint/ServerOnly |
| LibraryCore/Enum.cs | 231-315 | `BuffType` 枚举（分段编号：系统 1-22、War 100+、Wiz 200+、Tao 300+、Ass 400+、MagicWeakness 500） |
| LibraryCore/Enum.cs | 615-626 | `Element` 枚举（None + Fire/Ice/Lightning/Wind/Holy/Dark/Phantom） |
| LibraryCore/Enum.cs | 1558-1579 | `PoisonType` [Flags] 位标志枚举，注释即语义 |
| LibraryCore/Globals.cs | 139-141 | `PhysicalPoisonRate = 200`、`MagicalPoisonRate = 100`（毒性触发判定的分母） |
| ServerLibrary/DBModels/BuffInfo.cs | 8-236 | `BuffInfo`：Type/Stats/RemainingTime/TickFrequency/TickTime/ItemIndex/Visible/Pause/Hidden/Extra + Character/Account 双向关联 |
| ServerLibrary/Models/MapObject.cs | 83-126 | buff/毒容器字段（Buffs/PoisonList/Poison）、CanMove/CanAttack/CanCast 位掩码 |
| ServerLibrary/Models/MapObject.cs | 128-154 | `StartProcess()` 每帧调度：ProcessBuff → ProcessPoison → Process → ProcessHPMP |
| ServerLibrary/Models/MapObject.cs | 209-410 | `ProcessPoison()`：毒 tick 伤害、Boss 免疫、CanKill 保护、征服统计 |
| ServerLibrary/Models/MapObject.cs | 413-776 | `ProcessBuff()`：各 buff 的 tick 逻辑 + 到期收集 BuffRemove |
| ServerLibrary/Models/MapObject.cs | 1403-1490 | `BuffAdd()`：互斥替换、IsTemporary 标记、隐形/隐身特殊处理 |
| ServerLibrary/Models/MapObject.cs | 1505-1561 | `BuffRemove(info/type)`：广播、RefreshStats、删除 |
| ServerLibrary/Models/MapObject.cs | 1642-1663 | `ApplyPoison()`：抗性判定 + 同类型替换规则 |
| ServerLibrary/Models/MapObject.cs | 1732-1847 | GetDC/GetMC/GetSC/GetSP/GetAC/GetMR：min-max 随机 + Luck/DefensiveMastery 取极值 |
| ServerLibrary/Models/MapObject.cs | 1850-1860 | `Poison` 类：Owner/Type/Value/TickFrequency/TickCount/TickTime/Extra/CanKill |
| ServerLibrary/Models/PlayerObject.cs | 2179-2481 | `RefreshStats()`：全量属性聚合 |
| ServerLibrary/Models/PlayerObject.cs | 2528-2587 | `AddBaseStats()`：BaseStat 表（按职业/等级）打底 |
| ServerLibrary/Models/PlayerObject.cs | 9500-9584 | BuffAdd/BuffRemove override：持久化 + S.BuffAdd/S.BuffRemove 同步 |
| ServerLibrary/Models/PlayerObject.cs | 14745-14756 | 普攻出手：Slow 毒 → ActionTime 延迟 |
| ServerLibrary/Models/PlayerObject.cs | 15205-15348 | `Attack()`：普攻伤害公式（命中/AC/元素/暗黑石） |
| ServerLibrary/Models/PlayerObject.cs | 15461-15540 | 魔法伤害公共管线（MR + 元素 Attack/Resistance） |
| ServerLibrary/Models/PlayerObject.cs | 15678-15955 | `Attacked()` override：Evasion/Block、红毒、暴击、MagicShield/SuperiorMagicShield、反伤/天判 |
| ServerLibrary/Models/MonsterObject.cs | 1704-1779 | `Attack()`：怪物伤害公式（Abyss 50% miss、AC/MR、抗性） |
| ServerLibrary/Models/MonsterObject.cs | 2363-2403 | 怪物侧 Slow/Neutralize 对 AttackTime/MoveTime 的延迟 |
| ServerLibrary/Models/MonsterObject.cs | 2407-2496 | `Attacked()` override：红毒、MagicShield/SuperiorMagicShield、暴击 |
| ServerLibrary/Models/Magics/Wizard/MagicShield.cs | 35-47 | 魔法盾施放：Stat.MagicShield=50 buff |
| ServerLibrary/Models/Magics/Wizard/SuperiorMagicShield.cs | 35-49 | 高级魔法盾：吸收池计算 |
| ServerLibrary/Models/Magics/Wizard/FrostBite.cs | 38-78 | 冰冻体（FrostBite）：伤害累积与爆发 |
| ServerLibrary/Models/Magics/Taoist/PoisonDust.cs | 93-124 | 绿毒/红毒施毒公式 |

## 核心流程

### 1. 属性聚合：PlayerObject.RefreshStats()

真实方法名为 `PlayerObject.RefreshStats()`（ServerLibrary/Models/PlayerObject.cs:2179，覆盖 `MapObject.RefreshStats()` 虚方法 MapObject.cs:1160）。流程顺序（顺序即优先级，后写的会覆盖/叠加在前面之上）：

```text
RefreshStats()
├─ RefreshEquipmentGemBuffs()          // 装备宝石 buff 预处理 (PlayerObject.cs:2183)
├─ Stats.Clear()                        // 清空重算 (PlayerObject.cs:2185)
├─ AddBaseStats()                       // ① BaseStat 表(职业+等级) + Hermit 点 (PlayerObject.cs:2187, 2528-2587)
├─ ② 坐骑 Horse 加成                    // Brown/White/Red/Black/Unicorn (PlayerObject.cs:2189-2231)
├─ ③ 装备循环 (PlayerObject.cs:2235-2293)
│    ├─ Stats.Add(item.Info.Stats, 非武器才加元素)
│    ├─ Stats.Add(item.Stats, 非武器才加元素)
│    ├─ 宝石 socket：Info.Stats+Item.Stats 合并后 Add
│    └─ 武器：只加单一武器元素 GetWeaponElement() + GetWeaponElementValue()
├─ ④ 8 人均衡组队奖励：HP/MP + BaseHealth/BaseMana 的 1/10 (PlayerObject.cs:2295-2323)
├─ ⑤ 魔法被动：每个可用魔法的 GetPassiveStats() (PlayerObject.cs:2325-2332)
├─ ⑥ Buff 循环（Pause 的跳过；ItemBuff 取物品 Stats）(PlayerObject.cs:2334-2347)
├─ ⑦ 套装 SetInfo（集齐 + 等级 + 职业过滤）(PlayerObject.cs:2349-2375)
├─ ⑧ RagingWind 特例：重算 MinAC/MaxAC=3:7 分割 (PlayerObject.cs:2377-2388)
├─ ⑨ 硬上限：AttackSpeed +Level/15(≤3)，元素抗性 ±5，Comfort≤20，AttackSpeed≤15 (PlayerObject.cs:2390-2402)
│    RegenDelay = 15s - Comfort*650ms (PlayerObject.cs:2404)
├─ ⑩ 百分比乘区（整数除法，向下取整）(PlayerObject.cs:2406-2416)
│    Health *= HealthPercent; Mana *= ManaPercent; DC/MC/SC *= DCPercent/MCPercent/SCPercent
├─ ⑪ MagicWeakness 特例：MinMR=MaxMR=0 (PlayerObject.cs:2421-2425)
├─ ⑫ AC/MR *= PhysicalDefencePercent / MagicDefencePercent (PlayerObject.cs:2427-2431)
├─ ⑬ 下限钳制 Max(0) + Min≤Max 修正 (PlayerObject.cs:2433-2446)
├─ ⑭ 重量 *= WeightRate (PlayerObject.cs:2448-2450)
├─ ⑮ Rebirth/Fame 直写；转生加成 DropRate/GoldRate +20%/级 (PlayerObject.cs:2452-2457)
└─ 发包：S.StatsUpdate(含 HermitStats) + S.DataObjectMaxHealthMana + RefreshWeight() (PlayerObject.cs:2459-2474)
```

要点：**不存在“优先级权重”——除了少数特例（ItemReviveTime 取最小值、元素攻击对非武器装备可选跳过），所有来源一律整数加法叠加；百分比是最后统一乘一次。**

`Stats.Add` 的三条特殊规则（LibraryCore/Stat.cs:63-90）：

```csharp
public void Add(Stats stats, bool addElements = true)
{
    foreach (KeyValuePair<Stat, int> pair in stats.Values)
        switch (pair.Key)
        {
            case Stat.FireAttack:
            case Stat.LightningAttack:
            case Stat.IceAttack:
            case Stat.WindAttack:
            case Stat.HolyAttack:
            case Stat.DarkAttack:
            case Stat.PhantomAttack:
                if (addElements)
                    this[pair.Key] += pair.Value;
                break;
            case Stat.ItemReviveTime:
                if (pair.Value == 0) continue;

                if (this[pair.Key] == 0)
                    this[pair.Key] = pair.Value;
                else
                    this[pair.Key] = Math.Min(this[pair.Key], pair.Value);
                break;
            default:
                this[pair.Key] += pair.Value;
                break;
        }
}
```

中文解释：① 七系元素攻击可被 `addElements=false` 跳过——装备聚合时**非武器部位**的元素攻击不并入（PlayerObject.cs:2251-2252 传 `item.Info.ItemType != ItemType.Weapon`），武器元素单独在 PlayerObject.cs:2263-2272 走 `GetWeaponElement()` 单系注入；② `ItemReviveTime` 多来源取最小（最短复活 CD 生效）；③ 其余全部加法。

基础值来源 `AddBaseStats()`（PlayerObject.cs:2528-2587）：从 `SEnvir.BaseStatList` 里挑“同职业且等级 ≤ 当前等级中最大的一条”，写入 Health/Mana/重量/命中/敏捷/AC/MR/DC/MC/SC，再叠加 Hermit 点数（PlayerObject.cs:2580-2583），最后记录 `BaseHealth/BaseMana` 供组队奖励引用。

### 2. Buff 生命周期

加 buff（MapObject.cs:1403-1490，玩家 override 在 PlayerObject.cs:9500-9548）：

```csharp
public virtual BuffInfo BuffAdd(BuffType type, TimeSpan remainingTicks, Stats stats, bool visible, bool pause, TimeSpan tickRate, bool hidden = false, int extra = 0)
{
    BuffRemove(type);                       // ★ 同类型互斥：先无条件移除旧 buff

    BuffInfo info;

    Buffs.Add(info = SEnvir.BuffInfoList.CreateNewObject());

    info.Type = type;
    info.Visible = visible;
    info.Extra = extra;

    info.RemainingTime = remainingTicks;    // TimeSpan.MaxValue == 永久（任务书中的 InfiniteDuration 即此）
    info.TickFrequency = tickRate;
    info.Pause = pause;
    info.Stats = stats;

    if (info.Stats != null && info.Stats.Count > 0)
        RefreshStats();                     // ★ 带 Stats 的 buff 立即触发重算
    ...
}
```

叠加/互斥/刷新规则汇总（全部来自本次实读）：

- **互斥**：`BuffAdd` 第一行 `BuffRemove(type)`（MapObject.cs:1405）——同 `BuffType` 永不叠加，永远“以新换旧”。
- **跨类型互斥**：`SuperiorMagicShield` 施放时主动 `BuffRemove(BuffType.MagicShield)`（SuperiorMagicShield.cs:39）；两者不能共存（MagicShield.cs:37 也拒绝在 SuperiorMagicShield 存在时施放）。
- **叠加（唯一例外）**：毒类不在 Buffs 里而在 `PoisonList`，`ApplyPoison` 允许不同 `PoisonType` 并存；`DragonBlood`（刺客）通过 `poison.Extra` 记录层数手动叠绿毒（Magics/Assassin/DragonBlood.cs:58-75）。
- **刷新**：没有“续时间”API，续时 = 重新 `BuffAdd`（旧 buff 删除重建）。
- **暂停**：`buff.Pause == true` 时 `ProcessBuff` 直接 `continue`（MapObject.cs:425），且 `RefreshStats` 跳过该 buff 的 Stats（PlayerObject.cs:2336）；玩家进安全区时 ItemBuff 自动暂停（PlayerObject.cs:9508-9510）。
- **永久**：`RemainingTime == TimeSpan.MaxValue` 走“不衰减”分支（MapObject.cs:763、680-686）。
- **临时（不落库）**：`IsTemporary = true` 的类型清单见 MapObject.cs:1423-1436 与 PlayerObject.cs:9520-9545（Server/MapEffect/Guild/Ranking/SuperiorMagicShield 等）。
- **可见性**：`info.Visible` 控制是否广播 `S.ObjectBuffAdd/S.ObjectBuffRemove` 给周围玩家（MapObject.cs:1485-1488、1507-1508）；`info.Hidden` 控制是否给本人发 `S.BuffAdd`（PlayerObject.cs:9515-9518）。任务书里的 `VisibleToPlayer` 实际字段名即 `Visible`/`Hidden` 这一对。
- **隐身联动**：加/删 `Cloak/Transparency/Invisibility` 会清全图怪物仇恨并重算可见对象集（MapObject.cs:1441-1481、1536-1552）；`CanBeSeenBy` 里 Cloak/Transparency 目标对“等级低于自己或超出 `Globals.CloakRange=3`”的观察者不可见（MapObject.cs:1072-1090，Globals.cs:108）。

buff 过期处理（MapObject.cs:413-776 `ProcessBuff`，每帧由 `StartProcess` 调用 MapObject.cs:142）：

```csharp
public virtual void ProcessBuff()
{
    TimeSpan ticks = SEnvir.Now - BuffTime;   // 距上帧真实流逝时间

    BuffTime = SEnvir.Now;
    List<BuffInfo> expiredBuffs = new List<BuffInfo>();

    foreach (BuffInfo buff in Buffs)
    {
        if (buff.Pause) continue;

        switch (buff.Type)
        {
            // Companion/Heal/Cloak/DarkConversion/PKPoint/HuntGold/DragonRepulse/
            // FrostBite/ElementalHurricane 各有专属 tick 逻辑
            ...
            default:
                if (buff.RemainingTime == TimeSpan.MaxValue) continue;

                buff.RemainingTime -= ticks;          // ★ 永久 buff 不减时

                if (buff.RemainingTime > TimeSpan.Zero) continue;

                expiredBuffs.Add(buff);               // ★ 到期收集
                break;
        }
    }

    foreach (BuffInfo buff in expiredBuffs)
        BuffRemove(buff);                             // ★ 统一移除
}
```

中文解释：`ProcessBuff` 用“真实帧间隔 ticks”同时驱动两件事——(1) 各 buff 的 `TickTime` 周期效果（如 Heal 每 `TickFrequency` 回 `min(Healing, HealingCap)` 血，MapObject.cs:473-493）；(2) `RemainingTime` 倒计时，到 0 进 `expiredBuffs` 列表，循环外统一 `BuffRemove`。带 Stats 的 buff 移除时会再次 `RefreshStats()`（MapObject.cs:1512-1513）。玩家 override 只改持久化/发包，不动计时；`ItemObject/SpellObject` 覆写为空（ItemObject.cs:153-155、SpellObject.cs:334-336）。

`BuffRemove`（MapObject.cs:1505-1553）：广播 → `Buffs.Remove` → （有 Stats 则 RefreshStats）→ `info.Delete()`（MirDB 删除）→ Cloak/Transparency 特殊重算可见性 → 隐身类移除后强制全图怪物 `SearchTime = DateTime.MinValue` 立即重索敌。

### 3. 毒素系统

`PoisonType`（LibraryCore/Enum.cs:1558-1579，[Flags] 注释即官方语义，照抄）：

```csharp
[Flags]
public enum PoisonType
{
    None = 0,

    Green = 1 << 0,         //Tick damage, displays green
    Red = 1 << 1,           //Increases damage received by 20%, displays red
    Slow = 1 << 2,          //Reduces attackTime, actionTime, 100ms per value, displays blue
    Paralysis = 1 << 3,     //Stops movement, physical and magic attacks (all races), displays grey
    WraithGrip = 1 << 4,    //Stops shoulderdash, movement, displays effect (needs code revisiting)
    HellFire = 1 << 5,      //Tick damage, no colour
    Silenced = 1 << 6,      //Stops movement (all races), physical and magic attacks (monster), displays effect
    Abyss = 1 << 7,         //Reduces monster viewrange, displays blinding effect (player)
    Parasite = 1 << 8,      //Tick damage, explosion, ignores transparency (monster), displays effect
    Neutralize = 1 << 9,    //Stops attackTime, slows actionTime, displays effect (needs code revisiting)
    Fear = 1 << 10,         //Stops attack (monster), forces runaway (monster), displays effect
    Burn = 1 << 11,         //Tick damage, displays effect
    Containment = 1 << 12,  //Tick damage, stops movement, displays effect
    Chain = 1 << 13,        //Tick damage, limits movement, displays effect
    Hemorrhage = 1 << 14,   //Tick damage, stops recovery, displays effect
    Binding = 1 << 15,      //Tick damage, stops movement, displays effect
}
```

> 传奇3 老玩家口径对照：经典“绿毒”（持续掉血）= `Green`；经典“黄毒/红毒”（弱化）在本引擎里是 `Red`，语义是**受到伤害 ×1.2**而不是减防——减防语义未找到实现；经典“麻痹”= `Paralysis`；“冰冻/减速”拆成 `Slow`（减速）+ `BuffType.FrostBite`（寒冰蓄爆）。

`Poison` 数据结构（MapObject.cs:1850-1860）：`Owner/Type/Value/TickFrequency/TickCount/TickTime/Extra,Extra1,Extra2/CanKill`。`TickTime` 是下次触发时刻，`TickCount` 是剩余次数（`ProcessPoison` 中 `poison.TickCount-- <= 0` 时移除，MapObject.cs:226-227）。

上毒入口 `ApplyPoison`（MapObject.cs:1642-1663；怪物 override 额外把施毒者设为 Target，MonsterObject.cs:2497-2508）：

```csharp
public virtual bool ApplyPoison(Poison p)
{
    if (Dead) return false;

    if (SEnvir.Random.Next(100) < Stats[Stat.PoisonResistance]) return false;   // ★ 毒抗（拼写是 PoisonResistance）

    foreach (Poison poison in PoisonList)
    {
        if (poison.Type != p.Type) continue;

        if (poison.Value > p.Value) return false;   // ★ 旧毒更强 → 拒绝新毒

        PoisonList.Remove(poison);
        break;
    }

    //Check Pets target

    PoisonList.Add(p);

    return true;
}
```

中文解释：同类型毒“强替弱”——只有当新毒 `Value >=` 旧毒时才替换（相等也替换，等于刷新时长）。不同类型毒共存（`Poison` 位掩码聚合后广播 `S.ObjectPoison`，MapObject.cs:406-409）。

毒 tick 结算 `ProcessPoison`（MapObject.cs:209-410）核心：

```csharp
case PoisonType.Green:
    damage += poison.Value;
    break;
case PoisonType.WraithGrip:
    ChangeMP(-poison.Value);

    if (poison.Extra != null)
        poison.Owner.ChangeMP(poison.Value * (((UserMagic)poison.Extra).Level + 1));
    break;
...
case PoisonType.Parasite:
    {
        damage += poison.Value;

        if (poison.TickCount < 0)
        {
            explode = true;
        }
        ...
    }
    break;
...
if (Stats[Stat.Invincibility] > 0)
    damage = 0;

if (damage > 0)
{
    if (Race == ObjectType.Monster && ((MonsterObject)this).MonsterInfo.IsBoss)
        damage = 0;
    else
    {
        if (!poison.CanKill)
            damage = Math.Min(CurrentHP - 1, damage);   // ★ CanKill=false 的毒最多打到 1 HP
    }
    ...
    ChangeHP(-damage);
}
```

中文解释：绿毒每 tick 固定掉 `Value` 点；WraithGrip 吸蓝并按施法者魔法等级回蓝；Parasite 到期 `explode` 触发 `Parasite.Explode`；Boss 免疫一切毒 tick 伤害；`CanKill=false` 的毒永远打不死目标（保留 1 HP）。另外：施毒者离场/死亡/换图/超视距时毒被直接移除（MapObject.cs:216-220）；`Hemorrhage` 毒会停掉 HP 自然回复（MonsterObject.cs:1046、PlayerObject.cs:468）。

典型上毒公式：

- 道士 `PoisonDust`（PoisonDust.cs:105-114）：
```csharp
int duration = Magic.GetPower() + Player.GetSC() + Player.Stats[Stat.DarkAttack] * 2;

ob.ApplyPoison(new Poison
{
    Value = Magic.Level + 1 + Player.Level / 14,
    Type = type,                     // shape==0 → Green, shape==1 → Red (PoisonDust.cs:77)
    Owner = Player,
    TickCount = duration / 2,
    TickFrequency = TimeSpan.FromSeconds(2),
});
```
中文解释：毒每 2 秒跳一次，总时长 ≈ `duration` 秒（TickCount = duration/2 次 × 2s），绿毒每跳伤害 `Value = 技能等级+1+人物等级/14`；红毒 `Value` 不参与伤害只做标记（伤害放大在 Attacked 里）。
- 刺客 `DragonBlood`（DragonBlood.cs:58-75）：绿毒 `Value = Player.GetSP() * Magic.GetPower() / 100`，`TickCount = 10`、2s/跳，`Extra` 记录层数最多 `MaxStack`。
- 魔法附带毒（PlayerObject.cs:15414-15419 出手 Slow、15554-15605 burn/paralysis/slow/silence）：以 `Globals.PhysicalPoisonRate=200 / MagicalPoisonRate=100` 为分母掷 `Stat.ParalysisChance/SlowChance/SilenceChance`（目标 ≥250 级时分母 ×10），命中后 `Slow: Value=20, 5s, 1 跳`、`Paralysis: 2s, 1 跳`、`Silenced: 5s, 1 跳`、`Burn: Value=damage*burnLevel/10`。

毒素对行动的限制（“麻痹/冰冻”手感来源）：

```csharp
// MapObject.cs:86-88
public virtual bool CanMove => !Dead && SEnvir.Now >= ActionTime && SEnvir.Now >= MoveTime && SEnvir.Now > ShockTime && (Poison & PoisonType.Paralysis) != PoisonType.Paralysis && (Poison & PoisonType.WraithGrip) != PoisonType.WraithGrip && (Poison & PoisonType.Containment) != PoisonType.Containment && (Poison & PoisonType.Binding) != PoisonType.Binding && Buffs.All(x => x.Type != BuffType.DragonRepulse);
public virtual bool CanAttack => !Dead && ... (Poison & PoisonType.Paralysis) != PoisonType.Paralysis && (Poison & PoisonType.Fear) != PoisonType.Fear && ...;
public virtual bool CanCast => !Dead && ... (Poison & PoisonType.Paralysis) != PoisonType.Paralysis && (Poison & PoisonType.Fear) != PoisonType.Fear && (Poison & PoisonType.Silenced) != PoisonType.Silenced && ...;
```

```csharp
// PlayerObject.cs:14745-14756（普攻出手）Slow 毒：每点 Value = 100ms 延迟
AttackTime = SEnvir.Now.AddMilliseconds(attackDelay);

if (BagWeight > Stats[Stat.BagWeight] || (Poison & PoisonType.Neutralize) == PoisonType.Neutralize)
    AttackTime += TimeSpan.FromMilliseconds(attackDelay);

Poison poison = PoisonList.FirstOrDefault(x => x.Type == PoisonType.Slow);
TimeSpan slow = TimeSpan.Zero;
if (poison != null)
{
    slow = TimeSpan.FromMilliseconds(poison.Value * 100);
    ActionTime += slow;
}
```

怪物侧等价逻辑在 MonsterObject.cs:2363-2403（Slow 延迟 AttackTime/MoveTime，Neutralize 直接加一整个 delay）。红毒伤害放大：

```csharp
// PlayerObject.cs:15741-15742（玩家受击）；MonsterObject.cs:2442-2443 同式
if ((Poison & PoisonType.Red) == PoisonType.Red)
    power = (int)(power * 1.2F);
```

解毒 `Purify`（MapObject.cs:1325-1401）：帮友方清除全部毒（Parasite 除外）+ 净化 MagicWeakness/DefensiveBlow；对敌方（等级压制或 42% 随机）剥离 Heal/MagicShield/SuperiorMagicShield/Cloak 等一大串增益 buff。死亡时 `Die()` 清空全部毒并可能引爆 Chain 毒（MapObject.cs:1665-1684）。

### 4. 神圣/暗黑/幻影（Holy/Dark/Phantom）元素攻防

七系元素在 `Element` 枚举（Enum.cs:615-626）里平权；Holy/Dark/Phantom 无独立公式，只是七系 switch 的三个分支。以下三条管线全部照抄：

**(a) 玩家普攻**（PlayerObject.cs:15262-15333，Attack 方法内）：

```csharp
Element element = Element.None;

if (!hasMassacre)
{
    if (!ignoreDefense)
    {
        var resistance = ob.GetAC();
        ...
        power -= resistance;

        if (ob.Race == ObjectType.Player)
            res = ob.Stats.GetResistanceValue(hasStone ? Equipment[(int)EquipmentSlot.Amulet].Info.Stats.GetAffinityElement() : Element.None);
        else
            res = ob.Stats.GetResistanceValue(Element.None);

        if (res > 0)
            power -= power * res / 10;
        else if (res < 0)
            power -= power * res / 5;
    }

    if (power < 0) power = 0;

    for (Element ele = Element.Fire; ele <= Element.Phantom; ele++)
    {
        if (hasFlameSplash && ele > Element.Fire) break;

        int value = Stats.GetElementValue(ele);

        if (hasStone)
        {
            value += Equipment[(int)EquipmentSlot.Amulet].Info.Stats.GetAffinityValue(ele);
            element = ele;
        }

        power += value;

        res = ob.Stats.GetResistanceValue(ele);

        if (res <= 0)
            power -= value * res * 3 / 10;
        else
            power -= value * res * 2 / 10;
    }
    ...
}
```

中文解释：普攻先扣目标 `GetAC()`（AC min-max 随机 + Luck/DefensiveMastery，MapObject.cs:1820-1837）；再对“武器主元素抗性”结算——正抗每点减 10% 主体伤害，负抗每点**放大 20%**（`power -= power * res / 5`，res 为负即加伤）；然后七系元素攻击逐系加值，并按该系抗性衰减：正抗每点削掉该系元素值的 20%，负抗每点放大 30%。暗黑石（Amulet 槽 `ItemType.DarkStone`）会把石头的 `Affinity` 值并入对应元素并锁定 `element` 用于受击方计算。

**(b) 玩家魔法**（PlayerObject.cs:15461-15540，MagicAttack 公共管线）：

```csharp
power -= ob.GetMR();

switch (element)
{
    case Element.None:
        power -= power * ob.Stats[Stat.PhysicalResistance] / 10;
        break;
    case Element.Fire:
        power += GetElementPower(ob.Race, Stat.FireAttack) * 2;
        power -= power * ob.Stats[Stat.FireResistance] / 10;
        break;
    case Element.Ice:
        power += GetElementPower(ob.Race, Stat.IceAttack) * 2;
        power -= power * ob.Stats[Stat.IceResistance] / 10;
        break;
    case Element.Lightning:
        power += GetElementPower(ob.Race, Stat.LightningAttack) * 2;
        power -= power * ob.Stats[Stat.LightningResistance] / 10;
        break;
    case Element.Wind:
        power += GetElementPower(ob.Race, Stat.WindAttack) * 2;
        power -= power * ob.Stats[Stat.WindResistance] / 10;
        break;
    case Element.Holy:
        power += GetElementPower(ob.Race, Stat.HolyAttack) * 2;
        power -= power * ob.Stats[Stat.HolyResistance] / 10;
        break;
    case Element.Dark:
        power += GetElementPower(ob.Race, Stat.DarkAttack) * 2;
        power -= power * ob.Stats[Stat.DarkResistance] / 10;
        break;
    case Element.Phantom:
        power += GetElementPower(ob.Race, Stat.PhantomAttack) * 2;
        power -= power * ob.Stats[Stat.PhantomResistance] / 10;
        break;
}

if (power <= 0)
{
    ob.Blocked();
    return 0;
}

int damage = ob.Attacked(this, power, element, false, false, true, canStruck);
```

中文解释：魔法伤害 = 魔法基础 power（各魔法 `ModifyPowerAdditionner/ModifyPowerMultiplier` 产出）− 目标 `GetMR()`，再按魔法元素把自身该系元素攻击 `GetElementPower() * 2` 加进 power，然后对该系抗性做 `power * res / 10` 衰减（**正负抗同式**，负抗即增伤；注意这与普攻的 `/5`、`*3/10` 不同）。`Element.None` 的魔法吃 `PhysicalResistance`。

`GetElementPower`（PlayerObject.cs:16682-16706）：对怪物直接取 `Stats[element]` 满值；对玩家（PvP）则在 `[0, Stats[element]]` 区间随机并受 Luck 影响取极值——即 PvP 里元素攻击有随机浮动。

**(c) 怪物攻击**（MonsterObject.cs:1721-1751）：

```csharp
if (element == Element.None)
{
    int accuracy = Stats[Stat.Accuracy];

    if (SEnvir.Random.Next(ob.Stats[Stat.Agility]) > accuracy)
    {
        ob.Dodged();
        return 0;
    }

    damage = power - ob.GetAC();
}
else
{
    damage = power - ob.GetMR();
}

int res = ob.Stats.GetResistanceValue(element);

if (res > 0)
    damage -= damage * res / 10;
else if (res < 0)
    damage -= damage * res / 5;
```

中文解释：怪物物理攻击可被 `Accuracy vs Agility` 闪避；元素攻击直接吃 `GetMR()`；元素抗性结算与玩家普攻受击一致（正抗 /10、负抗 /5 放大）。`GetResistanceValue` 把 `Element.None` 映射到 `Stat.PhysicalResistance`（Stat.cs:477-500）。

**抗性来源与上限**：`Stats.GetElementValue/GetResistanceValue`（Stat.cs:431-500）是七系查表；玩家八系抗性在 RefreshStats 末尾统一钳制 `Math.Min(5, ...)`（PlayerObject.cs:2392-2399）——**玩家抗性最高 +5（-50% 魔法伤害）**；怪物无此钳制（MonsterInfo.Stats 直读）。`HasElementalWeakness()`（Stat.cs:374-380）判定八系全 ≤0。

### 5. 魔法盾 / 护体类减伤

**MagicShield（魔法盾）施放**（Magics/Wizard/MagicShield.cs:35-47）：

```csharp
public override void MagicComplete(params object[] data)
{
    if (Player.Buffs.Any(x => x.Type == BuffType.MagicShield) || Player.Buffs.Any(x => x.Type == BuffType.SuperiorMagicShield)) return;

    Stats buffStats = new Stats
    {
        [Stat.MagicShield] = 50
    };

    Player.BuffAdd(BuffType.MagicShield, TimeSpan.FromSeconds(30 + Magic.Level * 20 + Player.GetMC() / 2 + Player.Stats[Stat.PhantomAttack] * 2), buffStats, true, false, TimeSpan.Zero);

    Player.LevelMagic(Magic);
}
```

中文解释：魔法盾 = 一个 `Stat.MagicShield = 50`（50% 减伤）的 buff，时长 `30 + 技能等级×20 + MC/2 + 幻影攻击×2` 秒。

**MagicShield 受击结算（玩家侧）**（PlayerObject.cs:15785-15799）：

```csharp
if (!ignoreShield)
{
    if (Buffs.Any(x => x.Type == BuffType.Cloak))
        power -= power / 2;

    buff = Buffs.FirstOrDefault(x => x.Type == BuffType.MagicShield);

    if (buff != null)
    {
        buff.RemainingTime -= TimeSpan.FromMilliseconds(power * 25);
        Enqueue(new S.BuffTime { Index = buff.Index, Time = buff.RemainingTime });
    }

    power -= power * Stats[Stat.MagicShield] / 100;
}
```

中文解释：Cloak（潜行）状态受击先半减；魔法盾把本次伤害 `power * 25ms` 折算成持续时间扣掉（盾按受击量消耗）；最后 `Stat.MagicShield`（50）做百分比减伤。`ignoreShield=true` 的攻击（如怪物 `IgnoreShield`，MonsterObject.cs:1751）跳过全部三层。

**MagicShield 受击结算（怪物侧）**（MonsterObject.cs:2449-2454）：同式但每点伤害扣 `power * 10ms`（怪物盾消耗更快）：

```csharp
BuffInfo buff = Buffs.FirstOrDefault(x => x.Type == BuffType.MagicShield);

if (buff != null)
    buff.RemainingTime -= TimeSpan.FromMilliseconds(power * 10);

power -= power * Stats[Stat.MagicShield] / 100;
```

**SuperiorMagicShield（高级魔法盾/护体）施放**（Magics/Wizard/SuperiorMagicShield.cs:35-49）：

```csharp
public override void MagicComplete(params object[] data)
{
    if (Player.Buffs.Any(x => x.Type == BuffType.SuperiorMagicShield)) return;

    Player.BuffRemove(BuffType.MagicShield);

    Stats buffStats = new Stats
    {
        [Stat.SuperiorMagicShield] = (int)(Player.Stats[Stat.Mana] * (0.25F + Magic.Level * 0.05F))
    };

    Player.BuffAdd(BuffType.SuperiorMagicShield, TimeSpan.MaxValue, buffStats, true, false, TimeSpan.Zero);

    Player.LevelMagic(Magic);
}
```

**SuperiorMagicShield 受击（玩家侧）**（PlayerObject.cs:15890-15904）：

```csharp
if (!ignoreShield && Buffs.Any(x => x.Type == BuffType.SuperiorMagicShield))
{
    buff = Buffs.FirstOrDefault(x => x.Type == BuffType.SuperiorMagicShield);

    if (buff != null)
    {
        buff.Stats[Stat.SuperiorMagicShield] -= power;
        if (buff.Stats[Stat.SuperiorMagicShield] <= 0)
            BuffRemove(buff);
        else
            Enqueue(new S.BuffChanged() { Index = buff.Index, Stats = new Stats(buff.Stats) });
    }
}
else
    ChangeHP(-power);
```

怪物侧同式（MonsterObject.cs:2462-2471）。中文解释：高级魔法盾是**吸收池**（`Mana × (0.25 + 等级×0.05)`），存在期间伤害全额由池吸收、不掉 HP，池空即 buff 消失；注意它不做百分比减伤，且与 MagicShield 互斥（施放即移除对方）。

**其它护体类**：

- `Stat.ProtectionRing`（保护戒指）：受击时优先扣 MP（MapObject.cs:1249-1262，`ChangeHP` 内实现）。
- `BuffType.Cloak`：受击伤害减半（PlayerObject.cs:15787-15788）。
- `BuffType.FrostBite`（冰冻体，Wizard）：施放 `FrostBiteDamage = GetMC() + Magic.GetPower() + IceAttack*2`、`FrostBiteChance = 5 + 等级*5`、时长 `3 + 等级*3` 秒（FrostBite.cs:42-48）；受击时把伤害累积进 `FrostBiteDamage` 并按 `FrostBiteChance/200` 概率对攻击者上 `Slow` 毒（PlayerObject.cs:15764-15781）；到期 `FrostBiteEnd` 以储存值（上限 `MaxMC*50 + IceAttack*70`，FrostBite.cs:75）对半径 3 内非 Boss 怪爆发（FrostBite.cs:53-71 + MapObject.cs:660-676）。
- `Stat.Invincibility`：直接免疫一切伤害（PlayerObject.cs:15680、MonsterObject.cs:2409、MapObject.cs:286-287）。
- `BuffType.CelestialLight`：HP 归零时按 `Stats[Stat.CelestialLight]%` 复活（MapObject.cs:1312-1316）；`Stat.ItemReviveTime` 复活戒指（MapObject.cs:1318-1323）。

## 数据结构/协议细节

### Stat 枚举完整语义表（LibraryCore/Stat.cs:507-895）

说明：`模式` 列为 `StatDescription.Mode`（StatType）；Min/Max 成对值共用 Title（如 AC/MR/DC/MC/SC）；`ServerOnly=true` 的 Stat 不进客户端 `Stats(Stats, client:true)` 拷贝（Stat.cs:42-55）。分组仅为本表组织方式，源码为单一枚举连续编号（在 `ThrowDistance = 200` 与 `Random1 = 250`、`Duration = 10000` 处有显式跳号）。

**基础/生命魔法（血/蓝）**

| Stat（行号） | Title | 模式 | 语义 |
|---|---|---|---|
| BaseHealth (510) | Base Health | None | 基础 HP 快照（AddBaseStats 写入，组队奖励引用） |
| BaseMana (512) | Base Mana | None | 基础 MP 快照 |
| Health (515) | Health | Default | 最大生命 |
| Mana (517) | Mana | Default | 最大魔法 |
| HealthPercent (637) | Health | Percent | 最大生命百分比加成 |
| ManaPercent (737) | Mana | Percent | 最大魔法百分比加成 |
| Focus (840) | Focus | Default | 集中值（FP）上限，来自 Discipline（PlayerObject.cs:2551） |
| RenounceHPLost (685) | HP Recovery | Default | Renounce（神授）到期反扣的 HP 记录 |
| CelestialLight (679) | HP Recovery | Percent | 天光复活百分比 |
| Invincibility (849) | You are immune to all damage. | Text | 无敌标记（>0 免疫伤害） |
| SoulResonance (858) | You are soulbound to another player. | Text | 灵魂共鸣绑定标记 |

**攻防（AC/MR/DC/MC/SC）**

| Stat（行号） | Title | 模式 | 语义 |
|---|---|---|---|
| MinAC (520) / MaxAC (522) | AC | Min/Max | 物理防御区间（GetAC 随机） |
| MinMR (524) / MaxMR (526) | MR | Min/Max | 魔法防御区间（GetMR 随机） |
| MinDC (528) / MaxDC (530) | DC | Min/Max | 物理攻击区间（GetDC） |
| MinMC (532) / MaxMC (534) | MC | SpellPower | 魔法攻击区间（GetMC；MC=SC 时 UI 合并显示 "Spell Power"，Stat.cs:136-144） |
| MinSC (536) / MaxSC (538) | SC | SpellPower | 道术攻击区间（GetSC） |
| PhysicalDefencePercent (735) | Physical Defence | Percent | AC 百分比加成 |
| MagicDefencePercent (733) | Magic Defence | Percent | MR 百分比加成 |
| DCPercent (713) / MCPercent (670) / SCPercent (715) | DC/MC/SC | Percent | 三系攻击百分比加成 |
| ReflectDamage (630) | Reflect Damage | Percent | 反弹伤害百分比（PlayerObject.cs:15916-15918） |
| DefensiveMastery (855) | Defensive Mastery | Percent | AC 取 Max 值的概率（每点 10%，GetAC，MapObject.cs:1824-1834） |
| MagicShield (652) | Magic Shield | Percent | 魔法盾百分比减伤（50=50%） |
| SuperiorMagicShield (852) | Absorbing Power | Default | 高级魔法盾剩余吸收量 |
| ProtectionRing (763) | Protection Ring | Text | 保护戒指：受伤先扣 MP |

**命中/闪避/速度/幸运**

| Stat（行号） | Title | 模式 | 语义 |
|---|---|---|---|
| Accuracy (541) | Accuracy | Default | 命中（vs 目标 Agility，PlayerObject.cs:15246） |
| Agility (543) | Agility | Default | 闪避 |
| AttackSpeed (545) | Attack Speed | Default | 攻速加成（上限 15，PlayerObject.cs:2402） |
| BlockChance (824) | Block Chance | Percent | 物理攻击格挡概率（PlayerObject.cs:15709） |
| EvasionChance (826) | Evasion Chance | Percent | 魔法攻击闪避概率（PlayerObject.cs:15684） |
| CriticalChance (640) | Critical Chance | Default | 暴击概率（%） |
| CriticalDamage (797) | Critical Dmg (PvE) | Percent | 暴击伤害加成（MonsterObject.cs:2458） |
| Luck (552) | Luck | Default | 幸运：GetDC/MC/SC/SP/ElementPower 取上限的概率（≥10 必出 Max，负值取 Min） |
| Strength (550) | Strength | Default | 注释 "Also known as Inten (Intensity)" |
| Flexibility (874)/FloatStrength (876)/ReelBonus (878)/NibbleChance (880)/FinderChance (882)/ThrowDistance (870) | 钓鱼相关 | Default/Percent | 钓鱼小游戏参数（1-4 投掷距离等） |

**元素攻击/抗性（七系 × 2）**

| Stat（行号） | Title | 模式 | 语义 |
|---|---|---|---|
| FireAttack (555) / FireResistance (557) | Fire | AttackElement / ElementResistance | 火 |
| IceAttack (560) / IceResistance (562) | Ice | 同上 | 冰 |
| LightningAttack (565) / LightningResistance (567) | Lightning | 同上 | 雷 |
| WindAttack (570) / WindResistance (572) | Wind | 同上 | 风 |
| HolyAttack (575) / HolyResistance (577) | Holy | 同上 | 神圣 |
| DarkAttack (580) / DarkResistance (582) | Dark | 同上 | 暗黑 |
| PhantomAttack (585) / PhantomResistance (587) | Phantom | 同上 | 幻影 |
| PhysicalResistance (806) | Physical | ElementResistance | 物理抗性（Element.None 系） |
| FireAffinity (615)…PhantomAffinity (627) | Affinity: X | Text | 暗黑石/元素石亲和值（七系） |
| WeaponElement (633) | —（None） | None | 武器元素种类标记（存 (Element)int） |
| ElementalSwords (864) | Elemental Swords | Text | 元素剑剩余数量 |
| PoisonResistance (834) | Poison Resistance | Percent | 毒抗（ApplyPoison 判定，MapObject.cs:1646） |
| ParalysisChance (818)/SlowChance (820)/SilenceChance (822) | Paralysis/Slow/Silence Chance | Percent | 魔法附带麻痹/减速/沉默概率 |
| FrostBiteDamage (779)/FrostBiteChance (815) | Frost Bite Damage/Chance | Default/Percent | 冰冻体蓄伤/触发概率 |

**回复/吸血/恢复**

| Stat（行号） | Title | 模式 | 语义 |
|---|---|---|---|
| Comfort (590) | Comfort | Default | 回复加速：RegenDelay=15s−Comfort×650ms（上限 20，PlayerObject.cs:2401-2404） |
| LifeSteal (592) | Life Steal | Percent | 吸血百分比（MonsterObject.cs:1755） |
| Healing (607) / HealingCap (609) | Total Healing / Max Heal per Tick | Default | Heal buff 总量/每跳上限（MapObject.cs:480） |
| DarkConversion (682) | MP Conversion | Default | 暗黑转化每跳 MP 消耗（转 HP ×2，MapObject.cs:518-527） |
| CloakDamage (656) | Cloak Damage | Default | Cloak 每跳自伤（MapObject.cs:501-509） |

**经验/掉落/金币/商店**

| Stat（行号） | Title | 模式 | 语义 |
|---|---|---|---|
| ExperienceRate (595) | Experience Rate | Percent | 经验加成 |
| DropRate (597) | Drop Rate | Percent | 掉率加成 |
| GoldRate (695) | Gold Rate | Percent | 金币加成 |
| SkillRate (601) | Skill Rate | Default | 技能触发率（默认 1） |
| SaleBonus5/10/15/20 (643-649) | x% more profit when selling | Default | 商店出售利润门槛 |
| BaseExperienceRate (770)/BaseGoldRate (773)/BaseDropRate (776) | Base x Rate | Percent | 基础三率加成（与上面叠加） |
| MonsterExperience (743)/MonsterGold (746)/MonsterDrop (749)/MonsterDamage (752)/MonsterHealth (755) | Regular Monster's Base x | Percent | 普通怪基础五项百分比修正 |
| MaxMonsterExperience (782)/MaxMonsterGold (785)/MaxMonsterDrop (788)/MaxMonsterDamage (791)/MaxMonsterHealth (794) | Max Regular Monster's Base x | Percent | 上面五项的上限 |
| Experience (800) | Experience | Default | 经验值直写 |
| FragmentRate (809) | Success Rate Per Fragment | Percent | 碎片分解成功率 |
| MaxRefineChance (706) | Max Refine Chance | Percent | 精练成功率上限 |

**负重/拾取/宠物/伙伴**

| Stat（行号） | Title | 模式 | 语义 |
|---|---|---|---|
| BagWeight (688)/WearWeight (690)/HandWeight (692) | Inventory/Wear/Hand Weight | Default | 三种负重 |
| WeightRate (731) | Weight Rate | Default | 负重倍率（PlayerObject.cs:2448-2450） |
| PickUpRadius (604) | Pick Up Range | Default | 拾取半径（默认 1） |
| PetDCPercent (720) | Pet's DC | Percent | 宠物 DC 百分比 |
| CompanionInventory (709)/CompanionBagWeight (711)/CompanionHunger (717)/CompanionRate (728)/CompanionCollection (761) | Companion x | Default | 伙伴背包/负重/饥饿/经验率/拾取 |
| Light (548) | Light Radius | Default | 视野光照半径 |
| SizePercent (843) | Size Percent | None | 体型缩放 |
| GrowthLevel (846) | Growth Level | Default | 成长等级 |

**PK/社交/系统标记（多为 Text/None）**

| Stat（行号） | Title | 模式 | 语义 |
|---|---|---|---|
| Invisibility (612) | Invisibility | Text | 隐形（怪物不索敌，MapObject.cs:1441-1455） |
| Redemption (635) | Temporary Innocence. | Text | 赎罪（临时白名） |
| Cloak (654) | Invisible | Text | 潜行标记 |
| Transparency (676) | Transparency | Text | 透明（道士） |
| TheNewBeginning (659) | New Beginning Charges | Default | 新起点剩余次数（MapObject.cs:1492-1504） |
| Brown (662) | Brown, People can attack you freely | Text | 褐名 |
| PKPoint (664) | PK Points | Default | PK 点（buff 每 tick −1，MapObject.cs:537-557） |
| GlobalShout (667) | Global Shout no level restriction | Text | 全服喊话 |
| JudgementOfHeaven (673) | Chance of Judgement | Percent | 天判触发率（PlayerObject.cs:15924-15934） |
| RecallSet (740) | Recall Command: @GroupRecall | Text | 组队召回权限 |
| DeathDrops (803) | Death Drops Enabled. | Text | 死亡掉落标记 |
| MapSummoning (812) | Chance to summon map | Text | 召唤地图标记 |
| AvailableHuntGold (700)/AvailableHuntGoldCap (702) | Available/Maximum Available Hunt Gold | Default | 狩猎金余额/上限（HuntGold buff，MapObject.cs:558-587） |
| ItemReviveTime (704) | Revive Cool Down | Time | 复活戒指 CD 秒数（多来源取最小，Stat.cs:78-85） |
| OldDuration (698) | OldDuration | Time | 注释 UNUSED |
| Rebirth (837) | Rebirth | Default | 转生次数（受击方转生使伤害 ×1.2^N） |
| Fame (861) | —（None, ServerOnly） | None | 名望（直写 Character.Fame） |
| ItemIndex (758) | —（None） | None | 关联物品索引 |
| ClearRing (765)/TeleportRing (767) | —（None） | None | 清除/传送戒指标记 |
| BossTracker (723)/PlayerTracker (725) | Locates Boss/Players on the Map | Text | 小地图追踪 |
| IgnoreStealth (829) | —（None） | None | 无视隐身（MonsterObject.cs:947） |
| FootballArmourAction (831) | —（None） | None | 足球活动盔甲动作 |
| AutoCast (872) | Auto Cast | Text | 自动施法标记 |
| None (599) | Blank Stat | None | 空位占位 |
| RoamDistance (867) | —（None, ServerOnly） | None | 怪物游荡距离 |
| Random1 (885)/Random2 (887) | —（None, ServerOnly / None） | None | 预留随机位（Random1 是 ServerOnly） |
| Counter1 (889)/Counter2 (891) | —（None） | None | 预留计数位 |
| Duration (894) | Duration | Time | 通用时长位（=10000） |

### StatSource / StatType（LibraryCore/Stat.cs:897-918）

```csharp
public enum StatSource
{
    None,
    Added,
    Refine,
    Enhancement, //Temporary Buff!?
    Other,
}

public enum StatType
{
    None,
    Default,
    Min,
    Max,
    Percent,
    Text,
    AttackElement,
    ElementResistance,
    SpellPower,
    Time,
}
```

中文解释：`StatSource` 标识属性来源类别（装备加值/精练/临时强化等，主要用于物品强化链路）；`StatType` 决定 UI 展示行为——Min/Max 成对合并显示区间、Percent 除以 100 显示、SpellPower 在 MC=SC 时合并为 "Spell Power"、Time 显示为时长（负值显示 Permanent，Stat.cs:221-225）。

### BuffType 枚举（LibraryCore/Enum.cs:231-315）

分段编号即分组（照抄注释 //War //Wiz //Tao //Ass）：

| 段 | 值 |
|---|---|
| 系统 (1-22) | Server=1, HuntGold=2, Observable=3, Brown=4, PKPoint=5, PvPCurse=6, Redemption=7, Companion=8, Castle=9, ItemBuff=10, ItemBuffPermanent=11, Ranking=12, Developer=13, Veteran=14, MapEffect=15, InstanceEffect=16, Guild=17, DeathDrops=18, Fame=19, RedGem=20, BlueGem=21, CursedGem=22 |
| //War (100+) | Defiance=100, Might=101, Endurance=102, ReflectDamage=103, Invincibility=104, DefensiveBlow=105, Dash=106, ElementalSwords=107 |
| //Wiz (200+) | Renounce=200, MagicShield=201, JudgementOfHeaven=202, ElementalHurricane=203, SuperiorMagicShield=204, FrostBite=205, Tornado=206 |
| //Tao (300+) | Heal=300, Invisibility=301, MagicResistance=302, Resilience=303, ElementalSuperiority=304, BloodLust=305, StrengthOfFaith=306, CelestialLight=307, Transparency=308, LifeSteal=309, Spiritualism=310, SoulResonance=311 |
| //Ass (400+) | PoisonousCloud=400, FullBloom=401, WhiteLotus=402, RedLotus=403, Cloak=404, GhostWalk=405, TheNewBeginning=406, DarkConversion=407, DragonRepulse=408, Evasion=409, RagingWind=410, LastStand=411, Concentration=412 |
| 其它 | MagicWeakness=500 |

### BuffInfo 字段（ServerLibrary/DBModels/BuffInfo.cs:8-236）

| 字段 | 类型 | 语义 |
|---|---|---|
| Character / Account | 关联 | 持久化归属（二者互斥，OnChanged 强制，BuffInfo.cs:202-219） |
| Type | BuffType | buff 种类 |
| Stats | Stats | 该 buff 携带的属性块（RefreshStats 时并入） |
| RemainingTime | TimeSpan | 剩余时长；TimeSpan.MaxValue=永久 |
| TickFrequency / TickTime | TimeSpan | tick 周期 / 下次 tick 时刻 |
| ItemIndex | int | ItemBuff 关联的物品索引 |
| Visible | bool | 是否对周围玩家广播 |
| Pause | bool | 暂停计时与属性生效 |
| Hidden | bool | 不向本人发送 S.BuffAdd |
| Extra | int | 附加参数（如 ObjectBuffAdd 的 Extra） |
| IsTemporary | （MirDB 基类标记） | true = 不落库，重启即消失 |

### 相关网络协议（LibraryCore/Network/ServerPackets.cs 定义的包，服务端发送点已核实）

| 包 | 发送点 | 内容 |
|---|---|---|
| S.ObjectBuffAdd / S.ObjectBuffRemove | MapObject.cs:1487/1508 | 周围玩家可见的 buff 增减（ObjectID+Type+Extra） |
| S.BuffAdd / S.BuffRemove | PlayerObject.cs:9517/9558 | 本人 buff 列表同步（ClientBuffInfo） |
| S.BuffChanged / S.BuffTime / S.BuffPaused | MapObject.cs:488/15795 等 | buff 内部 Stats 变化 / 剩余时间 / 暂停 |
| S.ObjectPoison | MapObject.cs:409 | 毒位掩码聚合广播 |
| S.StatsUpdate | PlayerObject.cs:2459 | 完整属性 + Hermit 信息 |
| S.HealthChanged / S.ManaChanged / S.FocusChanged | MapObject.cs:172/187/196 | HP/MP/FP 变化（含 Critical/Miss/Block/Resist 标志） |

## GodotClient 现状

以下结论均来自本次对 GodotClient/ 的 glob/grep 实测：

| 功能 | 状态 | 证据（GodotClient 相对路径:行号） |
|---|---|---|
| 属性同步（S.StatsUpdate） | **已移植** | Network/ServerConnection.cs:708-712（Process + StatsUpdateEvent）；Scripts/GameScene.cs:5015-5018（`_playerStats = p.Stats` 并 `_mainPanel?.SetStats`）；Scripts/GameScene.cs:4164-4168（MaxHealth/MaxMana 提取） |
| Buff 面板 UI（图标/名称/剩余时间/暂停/永久合并） | **已移植** | Controls/BuffDialog.cs:48-73（列表过滤 + ItemBuffPermanent 合并）、125-138（名称映射）、144-210（GetBuffIcon 全量图标表，含 MagicShield=100、SuperiorMagicShield=161） |
| 本人 buff 增减/变化/暂停包 | **已移植** | Network/ServerConnection.cs:210-215、760-770（BuffRemove/BuffChanged/BuffTime/BuffPaused）；Scripts/GameScene.cs:5098-5132（OnBuffAdd/OnBuffRemove + Cloaked/GhostWalking/DragonRepulsed/ElementalHurricane 状态位） |
| 他人 buff 增减（S.ObjectBuffAdd/Remove） | **已移植** | Network/ServerConnection.cs:190-191、654-655；Scripts/GameScene.cs:1290-1291、1549-1550 |
| Buff 持续特效（MagicShield/SuperiorMagicShield/CelestialLight 等） | **部分移植** | Scripts/GameScene.cs:5179-5184（ImpactDef 表覆盖 MagicShield、SuperiorMagicShield、CelestialLight、DefensiveBlow、ReflectDamage 等；未见全量 BuffType 覆盖清单，无法确认每种 buff 都有特效） |
| 魔法盾施法动画/音效 | **已移植** | Scripts/MagicEffectTable.cs:364-365（MagicShield/SuperiorMagicShield CastEffect）；Scripts/MagicSoundCatalog.cs:84-85、118（复用 MagicShieldStart 音效） |
| 毒状态包与毒特效（S.ObjectPoison） | **已移植** | Network/ServerConnection.cs:192、656；Scripts/GameScene.cs:5243-5264（按 PoisonType 位选 WraithGrip/HellFire/Burn/Silenced/Abyss/Parasite/Neutralize/Fear/Containment 特效）；Scripts/ObjectRenderer.cs:39、585-586（头顶毒点） |
| 毒对本人移动/攻击限制（客户端预判） | **部分移植** | Scripts/GameScene.cs:4584-4585（Paralysis/Containment 禁攻击）、4591-4592（WraithGrip 禁移动）、1005-1006（Neutralize 计入攻速）——未见表态 Slow/Binding/Fear 的客户端限制；服务端仍会二次校验 |
| 元素攻击/抗性展示（角色窗） | **已移植** | Controls/CharacterDialog.cs:509-513（七系 Attack）、517-523（七系+Physical Resistance，含正负分页 AddElementPage） |
| 怪物抗性展示 | **已移植** | Controls/MonsterDialog.cs:63-71、120-131（八系 Resistance 行 + 颜色） |
| buff/毒/元素的数值计算（伤害、减伤、衰减公式） | **未移植（也无需）** | 全部公式在服务端（本文第 4/5 节）；GodotClient 中未发现任何 `GetResistanceValue`/`power * res / 10` 类计算，客户端只消费 S.HealthChanged 的最终数值（Network/ServerConnection.cs:195 HealthChangedEvent 注释 id, change, miss, block, critical, resist） |
| BuffDialog 的 ItemBuff 物品 Stats 查询 | **已移植** | Controls/BuffDialog.cs:54-57（用 Globals.ItemInfoList 查 ItemIndex） |

## 移植注意事项

1. **命名对齐**：服务端方法名是 `BuffAdd`/`BuffRemove`（不是 AddBuff/RemoveBuff）；字段是 `Visible`/`Hidden`/`Pause`/`RemainingTime`/`TickFrequency`/`TickTime`（没有 VisibleToPlayer/InfiniteDuration/Ctick，永久 = `RemainingTime == TimeSpan.MaxValue`）；毒抗拼写是 `Stat.PoisonResistance`（没有 PoisionResist）。
2. **buff 不叠加**：同 BuffType 一定先删后加；Godot 端若做本地预测，重复收到 ObjectBuffAdd 就是“刷新”而非叠层；唯一叠层机制在毒（DragonBlood 的 Extra 计层）。
3. **`Stat` 枚举有跳号**：`ThrowDistance = 200`、`Random1 = 250`、`Duration = 10000`（Stat.cs:870/885/894）。Godot 端若用数组按枚举下标存 Stat 会浪费/越界，务必用字典（现有 GodotClient `Stats` 实现即沿用 SortedDictionary）。
4. **ServerOnly Stat**（RoamDistance/Random1）不会出现在发给客户端的 Stats 拷贝里（Stat.cs:50）；`Stats(Stats, client:true)` 构造时被过滤，Godot 端不要期望收到它们。
5. **抗性上限 ±5、负值合法**：负抗是“元素弱点”，普攻受击按 `/5` 放大、元素值按 `*3/10` 放大、魔法按 `/10`；客户端展示需支持负数（CharacterDialog 已做正负分页，GodotClient/Controls/CharacterDialog.cs:522-523）。
6. **整数除法截断**：所有百分比公式都是 int 运算（`power * res / 10`），Godot 端若做伤害飘字预估必须用同一套整数截断，否则与服务器对不上。更稳妥的做法是直接信任 `S.HealthChanged.Change`（GodotClient 已如此）。
7. **MagicShield 双写**：`Stat.MagicShield` 既在 `Stats`（RefreshStats 并入 buff 的 50）也在 buff.Stats 里；玩家/怪物受击扣时系数不同（25ms vs 10ms/点伤害），移植观感（盾消失速度）时要区分。
8. **毒的存活条件**：施毒者死亡/离屏（>MaxViewRange）/换图，毒直接被删（MapObject.cs:216-220）；Godot 端不要在本地长期保留毒图标，以 `S.ObjectPoison`（位掩码聚合）为准。
9. **Element.None ≠ 物理**：`GetResistanceValue(Element.None)` 返回 `PhysicalResistance`（Stat.cs:495-496），但魔法管线里 `Element.None` 分支显式用 `PhysicalResistance / 10`（PlayerObject.cs:15501-15503）——魔法无元素时也吃物理抗性而非无抗性。
10. **`Pause` 的双重含义**：暂停既停计时也停属性（RefreshStats 跳过 Pause buff，PlayerObject.cs:2336）。安全区自动暂停 ItemBuff（PlayerObject.cs:9508-9510），Godot 端进安全区看到药 buff “冻结”属预期。
