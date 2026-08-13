# 魔法伤害全链路（magic-damage）

## TL;DR 速查表

- 施法链路：`C.Magic` 包（Client/Models/UserObject.cs:664）→ `BaseConnection.ProcessPacket` 反射分发（LibraryCore/Network/BaseConnection.cs:396）→ `SConnection.Process(C.Magic)`（ServerLibrary/Envir/SConnection.cs:518）→ `PlayerObject.Magic`（PlayerObject.cs:14815）→ `magicObject.MagicCast()` 排 `ActionType.DelayMagic` 延迟动作 → `MagicComplete()` → `PlayerObject.MagicAttack()`（PlayerObject.cs:15446）。
- 魔法基础伤害公式（PlayerObject.cs:15461-15540）：`power = Σ ModifyPowerAdditionner` → `Σ ModifyPowerMultiplier` → `− ob.GetMR()` → `+ GetElementPower(元素攻) × 2` → `− power × 元素抗 / 10` → `ob.Attacked()` 内再算暴击/魔盾/红毒/转生增伤。
- 技能威力：`UserMagic.GetPower()`（UserMagic.cs:179）= `Random(Info.MinBasePower + Level×MinLevelPower/3, Info.MaxBasePower + Level×MaxLevelPower/3 + 1)`；耗蓝 `Cost`（UserMagic.cs:168）= `BaseCost + Level×LevelCost/3`。
- **注意**：任务里写的 `MagicGetMAG` 不存在；真实函数是 `GetMC()`（魔法攻击，MapObject.cs:1754）/`GetSC()`（道术）/`GetSP()`（MC、SC 取小，MapObject.cs:1798）/`GetDC()`（物理）。`MagicObject` 的三段回调是 `MagicCast`/`MagicComplete`/`MagicAttackSuccess`（MagicObject.cs:97/102/319）。
- 元素枚举叫 `Element`（LibraryCore/Enum.cs:615，None/Fire/Ice/Lightning/Wind/Holy/Dark/Phantom），**没有** `ObjectElement`；元素攻防 Stat 为 `FireAttack`…`PhantomAttack` / `FireResistance`…`PhysicalResistance`（Stat.cs:554-587, 805-806）。
- 幸运（`Stat.Luck`，Stat.cs:551-552）影响 `GetMC/GetSC/GetSP/GetElementPower/GetLotusMana` 的取值倾向：luck≥10 恒取 max，luck>0 时 `Random(10)<luck` 取 max，luck<0 时可能取 min（诅咒效果）。
- 暴击拼写是 `CriticalChance`/`CriticalDamage`（Stat.cs:639-640；MonsterObject.cs:2456-2458），无 `Critial`/`CursePow`。
- 本服无全局 PVP 减伤系数；PVP 差异分散在：暴击玩家×1.3/怪物×2.0（PlayerObject.cs:15747-15754）、元素攻对玩家走随机 roll（PlayerObject.cs:16684-16705）、个别技能 `power /= 2`（CrushingWave/SeismicSlam）。
- 战士/刺客的"攻击系魔法"（`AttackSkill=true`）不走 `MagicAttack`，而是搭 `PlayerObject.Attack`（PlayerObject.cs:15205）的物理管线，经 `ModifyPowerAdditionner` 改写物理伤害。
- GodotClient 的施法输入/渲染链路已基本移植（发包、ObjectMagic 特效、冷却、UI），伤害公式全部在服务端、客户端无需移植公式。

## 职责概述

本文覆盖 Zircon 传奇3 服务端的**魔法伤害全链路**：从原版 WinForms 客户端（Client/）发出 `C.Magic` 包，到服务端 `SConnection` 分发、`PlayerObject.Magic` 施法入口、`MagicObject` 子类的 `MagicCast → MagicComplete` 延迟回调、`PlayerObject.MagicAttack` 的统一伤害结算、受击方 `PlayerObject.Attacked`/`MonsterObject.Attacked` 的暴击与护盾结算，以及毒素（DoT）在 `MapObject.ProcessPoison` 的周期结算。同时给出 `MagicInfo` 全字段语义、`UserMagic.Level`（技能等级 0-3）对威力/耗蓝的加成、元素克制公式、暴击/幸运结算顺序、PVP 差异，以及 `ServerLibrary/Models/Magics/` 四职业目录 **190 个技能类**的全量清单（含自定义伤害公式标注）。文末为 GodotClient 移植现状与移植注意事项，供向 Godot 新客户端移植技能时"查文档 → 对齐 → 移植"。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
| --- | --- | --- |
| Client/Models/UserObject.cs | 658-665 | 客户端 `MirAction.Spell` 动作帧到达时发送 `C.Magic` 包 |
| Client/Scenes/GameScene.cs | 3181, 3205 | 特殊技能（无目标吟唱/瞬发类）直接 `Enqueue(new C.Magic …)` |
| LibraryCore/Network/ClientPackets.cs | 159-166 | `C.Magic` 包定义（Direction/Action/Type/Target/Location） |
| LibraryCore/Network/ServerPackets.cs | 206-220 | `S.ObjectMagic` 广播包（施法结果回显） |
| LibraryCore/Network/BaseConnection.cs | 396-420 | `ProcessPacket` 按包类型反射查找 `Process(C.Magic)` 并调用 |
| ServerLibrary/Envir/SConnection.cs | 518-525 | 服务端 `Process(C.Magic p)` → `Player.Magic(p)` |
| ServerLibrary/Models/PlayerObject.cs | 14815-14919 | `Magic(C.Magic p)`：冷却/消耗校验、调 `MagicCast`、广播 `S.ObjectMagic` |
| ServerLibrary/Models/PlayerObject.cs | 15446-15676 | `MagicAttack(...)`：魔法伤害统一结算（本文核心） |
| ServerLibrary/Models/PlayerObject.cs | 15678-15955 | `PlayerObject.Attacked(...)`：玩家受击结算（闪避/魔免/暴击/魔盾/反伤） |
| ServerLibrary/Models/PlayerObject.cs | 15205-15444 | `Attack(...)`：物理攻击管线（AttackSkill 魔法在此合流） |
| ServerLibrary/Models/PlayerObject.cs | 355-436 | `ProcessAction`：`DelayMagic → MagicComplete`、`DelayedMagicDamage → MagicAttack` |
| ServerLibrary/Models/PlayerObject.cs | 16117-16162 | `LevelMagic`：技能经验/升级（0→1→2→3） |
| ServerLibrary/Models/PlayerObject.cs | 16682-16706 | `GetElementPower`：元素攻击取值（对玩家目标带幸运 roll） |
| ServerLibrary/Models/MagicObject.cs | 14-352 | 魔法基类：`MagicCast/MagicComplete/ModifyPower*/MagicConsume/MagicCooldown` 等全部虚方法 |
| ServerLibrary/Models/MapObject.cs | 1754-1847 | `GetMC/GetSC/GetSP/GetAC/GetMR`：攻防取值函数（幸运影响） |
| ServerLibrary/Models/MapObject.cs | 209-297 | `ProcessPoison`：毒素周期结算（Green/HellFire/Burn 等 DoT） |
| ServerLibrary/Models/MonsterObject.cs | 2407-2496 | `MonsterObject.Attacked`：怪物受击结算（暴击×2+CriticalDamage） |
| LibraryCore/SystemModels/MagicInfo.cs | 6-314 | System.db 技能静态数据（威力/耗蓝/需求等级/经验/冷却） |
| ServerLibrary/DBModels/UserMagic.cs | 10-209 | Users.db 玩家技能（Level/Experience/GetPower/Cost） |
| LibraryCore/Stat.cs | 551-587, 639-652 | Luck、七系元素攻防、CriticalChance、MagicShield 等 Stat |
| LibraryCore/Enum.cs | 591-626 | `MagicSchool`、`Element`、`MagicType` 枚举 |
| LibraryCore/Globals.cs | 92-93, 139-141, 311-313 | MagicRange=10、MagicMaxLevel=4、PoisonRate、CastTime/MagicDelay |
| ServerLibrary/Models/Magics/{Warrior,Wizard,Taoist,Assassin}/ | 全目录 | 190 个技能类（见文末全量清单） |

## 核心流程

### 1. 客户端发起（原版 Client/）

原版客户端在玩家施法动作（`MirAction.Spell`）的动作帧处理中发包：

```csharp
// Client/Models/UserObject.cs:658-665
case MirAction.Spell:
    NextMagicTime = CEnvir.Now + Globals.MagicDelay;

    if (BagWeight > Stats[Stat.BagWeight] || (Poison & PoisonType.Neutralize) == PoisonType.Neutralize)
        NextMagicTime += Globals.MagicDelay;

    CEnvir.Enqueue(new C.Magic { Direction = action.Direction, Action = action.Action, Type = MagicType, Target = AttackTargets?.Count > 0 ? AttackTargets[0].ObjectID : 0, Location = MagicLocations?.Count > 0 ? MagicLocations[0] : Point.Empty });
    GameScene.Game.CanRun = false;
    break;
```

要点：目标取 `AttackTargets[0]`、落点取 `MagicLocations[0]`（由客户端技能逻辑填充）；超重/中和毒会延长下一次施法间隔。部分特殊技能在 `GameScene.UseMagic` 里直接发包（Client/Scenes/GameScene.cs:3181、3205）：

```csharp
// Client/Scenes/GameScene.cs:3181
CEnvir.Enqueue(new C.Magic { Direction = direction, Action = MirAction.Spell, Type = magic.Info.Magic });
// Client/Scenes/GameScene.cs:3205
CEnvir.Enqueue(new C.Magic { Action = MirAction.Spell, Type = magic.Info.Magic, Target = target.ObjectID });
```

### 2. 网络分发（Server/ + ServerLibrary/）

`BaseConnection` 收包后按 `(连接类型, 包类型)` 反射查找同名 `Process` 重载（结果缓存在静态 `PacketMethods` 字典）：

```csharp
// LibraryCore/Network/BaseConnection.cs:396-420（节选）
private void ProcessPacket(Packet p)
{
    ...
    MethodInfo info;
    lock (PacketMethodsLock)
    {
        if (!PacketMethods.TryGetValue(key, out info))
        {
            info = connectionType.GetMethod("Process", new[] { p.PacketType });
            if (info != null)
                PacketMethods[key] = info;
        }
    }
    ...
}
```

`SConnection` 侧入口（校验游戏阶段与方向合法性后转交 PlayerObject）：

```csharp
// ServerLibrary/Envir/SConnection.cs:518-525
public void Process(C.Magic p)
{
    if (Stage != GameStage.Game) return;

    if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;

    Player.Magic(p);
}
```

### 3. PlayerObject.Magic 施法入口（ServerLibrary/Models/PlayerObject.cs:14815）

```csharp
// ServerLibrary/Models/PlayerObject.cs:14815-14918（节选）
public void Magic(C.Magic p)
{
    if (!GetMagic(p.Type, out MagicObject magicObject))     // 未学/不可用 → 拉回位置
    { Enqueue(new S.UserLocation { ... }); return; }

    if (SEnvir.Now < ActionTime || SEnvir.Now < MagicTime || SEnvir.Now < magicObject.Magic.Cooldown)
    {
        // 动作排队（一次）或拒绝，防止刷包
        if (!PacketWaiting) { ActionList.Add(new DelayedAction(ActionTime, ActionType.Magic, p)); PacketWaiting = true; }
        else Enqueue(new S.UserLocation { ... });
        return;
    }

    if (!CanCast) { ... return; }        // MapObject.cs:88：死亡/麻痹/恐惧/沉默/龙推禁施法；PlayerObject.cs:133 加上马/钓鱼禁施法
    if (!magicObject.CheckCost()) { ... return; }   // MP/FP 不足（MagicObject.cs:72-95）

    MapObject ob = VisibleObjects.FirstOrDefault(x => x.ObjectID == p.Target);
    if (ob != null && !Functions.InRange(CurrentLocation, ob.CurrentLocation, Globals.MagicRange))  // MagicRange=10
        ob = null;

    var element = magicObject.GetElement(Element.None);      // 取技能元素
    var castObject = magicObject.MagicCast(ob, p.Location, p.Direction);  // ← 各技能自己的目标/落点/延迟逻辑

    magicObject.MagicConsume();       // 扣 MP（或 FP）
    magicObject.MagicFinalise();      // 施法后移除隐身类 buff（MagicObject.cs:123-131）
    magicObject.ResetCombatTime();
    if (cast) magicObject.MagicCooldown();   // 写 Cooldown 并发 S.MagicCooldown

    ActionTime = SEnvir.Now + Globals.CastTime;      // 600ms
    MagicTime  = SEnvir.Now + Globals.MagicDelay;    // 2000ms
    // 超重/中和毒再 +MagicDelay；减速毒 +Value*100ms（14895-14905）

    Broadcast(new S.ObjectMagic
    {
        ObjectID = ObjectID, Direction = Direction, CurrentLocation = CurrentLocation,
        Type = p.Type, Targets = targets, Locations = locations,
        Cast = cast, Slow = slow, AttackElement = element
    });
}
```

注意：**施法包本身不造成伤害**。伤害发生在 `MagicCast` 排入的 `ActionType.DelayMagic` 延迟动作到点后。

### 4. MagicObject 生命周期与延迟回调

```csharp
// ServerLibrary/Models/PlayerObject.cs:401-410
case ActionType.DelayMagic:
    {
        type = (MagicType)action.Data[0];

        if (GetMagic(type, out MagicObject magicObject))
        {
            magicObject.MagicComplete(action.Data);   // ← 每技能的命中结算
        }
    }
    return;
// ServerLibrary/Models/PlayerObject.cs:420-428
case ActionType.DelayedMagicDamage:
    {
        ob = (MapObject)action.Data[1];
        if (!CanAttackTarget(ob)) return;
        MagicAttack((List<MagicType>)action.Data[0], ob, (bool)action.Data[2], (Stats)action.Data[3], (int)action.Data[4]);
    }
    return;
```

`MagicObject` 基类提供全部可覆写钩子（MagicObject.cs:25-48 的魔法变量、222-230 的威力改写、319-332 的命中后回调）：

| 钩子 | 行号 | 作用 |
| --- | --- | --- |
| `Element Element`（抽象） | 25 | 技能元素，决定 MagicAttack 元素分支 |
| `Slow/SlowLevel/Repel/Silence/Shock/Burn/BurnLevel` | 31-37 | 附带控制/燃烧参数，`GetXXX`（245-299）聚合进 MagicAttack |
| `CanStruck` | 30 | 命中是否触发受击硬直（FireWall/Asteroid/Tempest 为 false） |
| `MagicCast(target, location, direction)` | 97-100 | 施法瞬间：锁定目标/落点、排 DelayedAction、返回 `MagicCast` 结果 |
| `MagicComplete(params object[] data)` | 102-105 | 弹道到点：调 `Player.MagicAttack` 或自算效果 |
| `MagicConsume()` | 107-121 | 扣蓝：`ChangeMP(-Magic.Cost)`，Discipline 系扣 FP，Superman 免费 |
| `MagicCooldown()` | 339-351 | `Cooldown = Now + Info.Delay`，发包 `S.MagicCooldown` |
| `ModifyPowerAdditionner(primary, power, ob, stats, extra)` | 222-225 | 加法改威力（第一轮遍历） |
| `ModifyPowerMultiplier(primary, power, stats, extra)` | 227-230 | 乘法改威力（第二轮遍历） |
| `GetElement(element)` | 237-243 | 覆盖/透传元素（ElementalHurricane/Spiritualism 动态选元素） |
| `MagicAttackSuccess(ob, damageDealt)` | 319-322 | 命中且 damage>0 后回调（默认 `LevelMagic`） |
| `GetAugmentedSkill(MagicType)` | 301-312 | 取可用强化技（Burning/Shocked/Augment 系列）的 UserMagic |
| `GetDelayFromDistance(start, target)` | 208-220 | 弹道延迟 = start + 距离×48ms |

技能实例在登录时反射注册（PlayerObject.cs:247-280）：`SEnvir.MagicTypes` 里按 `[MagicType(MagicType.X)]` 特性匹配 `MagicType`，`Activator.CreateInstance` 构建 `MagicObject` 存入 `MagicObjects`（`MagicList`，PlayerObject.cs:168）。

### 5. MagicAttack 结算主流程（PlayerObject.cs:15446-15676）

```csharp
// ServerLibrary/Models/PlayerObject.cs:15446-15540（节选，公式逐行保留）
public int MagicAttack(List<MagicType> types, MapObject ob, bool primary = true, Stats stats = null, int extra = 0)
{
    if (ob?.Node == null || ob.Dead) return 0;
    // 宠物仇恨转移（15450-15459）

    int power = 0;
    int slow = 0, slowLevel = 0, repel = 0, silence = 0;
    int shock = 0, burn = 0, burnLevel = 0;
    bool canStruck = true;
    Element element = Element.None;

    foreach (MagicType type in types)
    {
        if (GetMagic(type, out MagicObject magicObject))
        {
            power = magicObject.ModifyPowerAdditionner(primary, power, ob, stats, extra);  // 第一轮：加法
            slow = magicObject.GetSlow(slow, stats);          // 附带控制聚合
            ...
            canStruck = magicObject.CanStruck;
            element = magicObject.GetElement(element);        // 后者覆盖前者
        }
    }

    foreach (MagicType type in types)
    {
        if (GetMagic(type, out MagicObject magicObject))
            power = magicObject.ModifyPowerMultiplier(primary, power, stats, extra);       // 第二轮：乘法
    }

    power -= ob.GetMR();                                       // ① 魔防直减

    switch (element)                                           // ② 元素攻加成 & 元素抗减伤
    {
        case Element.None:
            power -= power * ob.Stats[Stat.PhysicalResistance] / 10;
            break;
        case Element.Fire:
            power += GetElementPower(ob.Race, Stat.FireAttack) * 2;
            power -= power * ob.Stats[Stat.FireResistance] / 10;
            break;
        // Ice/Lightning/Wind/Holy/Dark/Phantom 同构（15508-15531）
    }

    if (power <= 0)
    {
        ob.Blocked();
        return 0;                                              // ③ 归零 = 格挡
    }

    int damage = ob.Attacked(this, power, element, false, false, true, canStruck);  // ④ 受方结算
    if (damage <= 0) return damage;

    // ⑤ 命中后效果：Shock（震慑）、Burn 燃烧毒（Value = damage * burnLevel / 10，15554-15564）
    //    魔法触发型毒素：ParalysisChance/SlowChance/SilenceChance 按 MagicalPoisonRate(100) roll（15566-15605）
    //    目标等级 ≥250 时概率除以 10（15568-15571）
    // ⑥ MagicAttackSuccess / MagicAttackSuccessPassive 回调（15607-15617）
    // ⑦ 对怪附加：slow 毒（BOSS 免疫）、repel 推位、silence 毒（15619-15671）
    CheckBrown(ob);   // PVP 褐名判定
    return damage;
}
```

结算顺序总结：`基础 power(0) → +技能/面板加法 → ×技能乘法 → −GetMR() → +元素攻×2 → ×(1−元素抗/10) → 0 则格挡 → Attacked()（闪避/魔免/红毒/转生/暴击/魔盾/HP扣减）→ 毒素/控制附加 → 升级回调`。

### 6. 受方结算：PlayerObject.Attacked（15678-15955）

```csharp
// ServerLibrary/Models/PlayerObject.cs:15680-15757（节选）
if (element != Element.None)
{
    if (SEnvir.Random.Next(attacker.Race == ObjectType.Player ? 200 : 100) <= Stats[Stat.EvasionChance])
    { DisplayMiss = true; return 0; }                      // 闪避：玩家攻击 1/200 基准，怪物 1/100

    if (GetMagic(MagicType.MagicImmunity, out MagicImmunity magicImmunity))
    {
        power -= power * magicImmunity.Magic.GetPower() / 100;   // 魔法免疫百分比减伤
        if (power <= 0) { DisplayMiss = true; return 0; }
        DisplayResist = true;
    }
}
...
if ((Poison & PoisonType.Red) == PoisonType.Red)
    power = (int)(power * 1.2F);                            // 红毒增伤 20%

for (int i = 0; i < attacker.Stats[Stat.Rebirth]; i++)
    power = (int)(power * 1.2F);                            // 攻方转生每层 +20%

if (SEnvir.Random.Next(100) < attacker.Stats[Stat.CriticalChance] && canCrit)
{
    if (!canReflect)
        power = (int)(power * 1.2F);
    else if (attacker.Race == ObjectType.Player)
        power = (int)(power * 1.3F);                        // PVP 暴击 ×1.3
    else
        power += power;                                     // PVE 暴击 ×2.0
    Critical();
}
...
if (!ignoreShield)
{
    if (Buffs.Any(x => x.Type == BuffType.Cloak))
        power -= power / 2;                                 // 隐身衣减半
    buff = Buffs.FirstOrDefault(x => x.Type == BuffType.MagicShield);
    if (buff != null)
    {
        buff.RemainingTime -= TimeSpan.FromMilliseconds(power * 25);  // 魔盾吸时
        ...
    }
    power -= power * Stats[Stat.MagicShield] / 100;         // 魔盾百分比减伤
}
...
// SuperiorMagicShield 存量吸收（15890-15902）；否则 ChangeHP(-power)
// 反伤：ReflectDamage（15916-15922）、JudgementOfHeaven 雷反（15924-15934）
```

### 7. 受方结算：MonsterObject.Attacked（2407-2496）

```csharp
// ServerLibrary/Models/MonsterObject.cs:2442-2460（节选）
if ((Poison & PoisonType.Red) == PoisonType.Red)
    power = (int)(power * 1.2F);

for (int i = 0; i < attacker.Stats[Stat.Rebirth]; i++)
    power = (int)(power * 1.5F);                            // 怪物受击：转生每层 +50%（玩家是 20%）

buff = Buffs.FirstOrDefault(x => x.Type == BuffType.MagicShield);
if (buff != null)
    buff.RemainingTime -= TimeSpan.FromMilliseconds(power * 10);   // 怪物魔盾吸时 10ms/点（玩家 25ms）
power -= power * Stats[Stat.MagicShield] / 100;

if (SEnvir.Random.Next(100) < attacker.Stats[Stat.CriticalChance] && canCrit && power > 0)
{
    power += power + (power * attacker.Stats[Stat.CriticalDamage] / 100);  // 暴击 = ×2 再 + CriticalDamage%
    Critical();
}
```

之后 `SuperiorMagicShield` 吸收或 `ChangeHP(-power)`；`Chain` 毒持有者回吸伤害（2473-2481）；死亡走掉落。

### 8. 毒素（DoT）周期结算（MapObject.ProcessPoison，MapObject.cs:209-297）

```csharp
// ServerLibrary/Models/MapObject.cs:234-284（节选）
switch (poison.Type)
{
    case PoisonType.Green:      damage += poison.Value; break;             // 绿毒：每 tick 扣 Value
    case PoisonType.WraithGrip:                                         // 缠魂：每 tick 扣 MP，Owner 回 MP×(强化技Level+1)
        ChangeMP(-poison.Value);
        if (poison.Extra != null)
            poison.Owner.ChangeMP(poison.Value * (((UserMagic)poison.Extra).Level + 1));
        break;
    case PoisonType.HellFire:   damage += poison.Value; break;             // 狱火 DoT
    case PoisonType.Parasite:   damage += poison.Value; ... break;         // 寄生 + 到期爆炸
    case PoisonType.Burn:       damage += poison.Value; break;             // 燃烧（FireBall 系附带）
    case PoisonType.Containment: damage += poison.Value; break;
    case PoisonType.Chain:      damage += Chain.PoisonTick(this); break;
    case PoisonType.Hemorrhage: damage += poison.Value; break;
    case PoisonType.Binding:    damage += poison.Value; break;
}
...
if (Race == ObjectType.Monster && ((MonsterObject)this).MonsterInfo.IsBoss)
    damage = 0;                                   // BOSS 免疫毒素伤害
else if (!poison.CanKill)
    damage = Math.Min(CurrentHP - 1, damage);     // 默认毒不致死，最多扣到 1 HP
```

## 数据结构/协议细节

### MagicInfo 全字段语义（LibraryCore/SystemModels/MagicInfo.cs）

System.db 的技能静态表。所有 int 字段无初始化器，**默认值均为 0**；实际数值由 DB 数据决定（服务端另有 System.db 初始化数据，不在源码内）。

| 字段 | 行号 | 语义 | 默认值 |
| --- | --- | --- | --- |
| `Name` | 8-21 | 技能名（IsIdentity，显示用） | null |
| `Magic` | 23-36 | `MagicType` 枚举值，与技能类的 `[MagicType]` 特性匹配 | 0 (None) |
| `Class` | 38-51 | 所属职业 `MirClass`（Warrior/Wizard/Taoist/Assassin） | 0 |
| `School` | 53-66 | `MagicSchool`（Enum.cs:591-613：Passive=1/Active/Toggle/Fire/Ice/Lightning/Wind/Holy/Dark/Phantom/Physical/Atrocity/Kill/Assassination/Horse/Discipline=20） | 0 (None) |
| `Property` | 68-81 | `MagicProperty`（none/attack/aura/buff 等，客户端 UI 分类） | 0 |
| `Icon` | 83-96 | 客户端图标索引 | 0 |
| `MinBasePower` | 98-111 | 0 级技能威力下限 → `GetPower()` 的 min 基数 | 0 |
| `MaxBasePower` | 113-126 | 0 级技能威力上限 → `GetPower()` 的 max 基数 | 0 |
| `MinLevelPower` | 128-141 | 每级威力下限增量（按 `Level×/3` 折算） | 0 |
| `MaxLevelPower` | 143-156 | 每级威力上限增量（按 `Level×/3` 折算） | 0 |
| `BaseCost` | 158-171 | 0 级耗蓝基数 | 0 |
| `LevelCost` | 163-186 | 每级耗蓝增量（按 `Level×/3` 折算） | 0 |
| `NeedLevel1/2/3` | 189-232 | 技能升到 1/2/3 级所需人物等级（`LevelMagic` 与 `CanUseMagic` 双重校验） | 0 |
| `Experience1/2/3` | 234-277 | 0→1、1→2、2→3 级所需技能经验上限（**没有** `ExperienceBase`，任务中的名称不准确） | 0 |
| `Delay` | 280-293 | 冷却毫秒数，`MagicCooldown` 直接使用 | 0 |
| `Description` | 295-308 | 客户端技能说明文本 | null |

### UserMagic：技能等级如何加成伤害/耗蓝（ServerLibrary/DBModels/UserMagic.cs）

```csharp
// ServerLibrary/DBModels/UserMagic.cs:165-188
public DateTime Cooldown;

[IgnoreProperty]
public int Cost => Info.BaseCost + Level * Info.LevelCost / 3;   // 耗蓝 = 基数 + 等级×每级/3（整数除法）

public int GetPower()
{
    int min = Info.MinBasePower + Level * Info.MinLevelPower / 3;
    int max = Info.MaxBasePower + Level * Info.MaxLevelPower / 3;

    if (min < 0) min = 0;
    if (min >= max) return min;

    return SEnvir.Random.Next(min, max + 1);   // 闭区间 [min, max]
}
```

- `Level`（UserMagic.cs:103-116）：0-3 级。升级在 `PlayerObject.LevelMagic`（PlayerObject.cs:16117-16162）：每次命中 `experience = Random(Config.SkillExp)+1`，再 `× Stats[Stat.SkillRate]`；达到 `Experience1/2/3` 升级并 `RefreshStats()`（技能被动属性随级刷新）。`MagicMaxLevel = 4`（Globals.cs:93）对应内部 4 档（0-3）。
- `Experience`（UserMagic.cs:118-131）：long 型当前经验。
- `ItemRequired`（UserMagic.cs:133-147）：戒指技能标记，`CanUseMagic`（MagicObject.cs:56-70）要求佩戴 `ItemEffect.MagicRing` 且 `Shape == Info.Index` 的戒指；否则按 `Player.Level < NeedLevel1` 校验。
- `Set1Key`-`Set4Key`（43-101）：四组快捷键位，客户端 `C.MagicKey` 持久化（ClientPackets.cs:344-351）。
- `Discipline`（149-163）：心法归属（`MagicSchool.Discipline` 的技能耗 FP 而非 MP，见 MagicObject.cs:114-117）。

### 元素克制：Element、Stat 与减伤公式

- 元素枚举（**无 `ObjectElement`，实际叫 `Element`**）：

```csharp
// LibraryCore/Enum.cs:615-626
public enum Element : byte
{
    None,
    Fire, Ice, Lightning, Wind, Holy, Dark, Phantom,
}
```

- Stat 侧成对定义（Stat.cs:554-587）：`FireAttack/FireResistance`、`IceAttack/IceResistance`、`LightningAttack/LightningResistance`、`WindAttack/WindResistance`、`HolyAttack/HolyResistance`、`DarkAttack/DarkResistance`、`PhantomAttack/PhantomResistance`，另有 `PhysicalResistance`（Stat.cs:805-806，Mode=ElementResistance）。`Stats.GetResistanceValue(element)`（Stat.cs:477-499）把 Element 映射到对应抗性（None→PhysicalResistance）。
- 魔法链路元素公式（见第 5 节 ②）：`power += GetElementPower(ob.Race, 对应元素Attack) * 2; power -= power * ob.Stats[对应元素Resistance] / 10;` —— 攻击端翻倍加成、抗性按 10 分率减伤（抗 10 = 减 100%，可溢出归零触发 Blocked）。
- `GetElementPower`（PlayerObject.cs:16682-16706）：目标非玩家时直接返回面板 `Stats[element]`；**目标是玩家时**在 `[0, Stats[element]]` 区间 roll，幸运偏向 max、诅咒偏向 min（与 `GetMC` 同构）——这是 PVP 元素削弱的隐性来源：

```csharp
// ServerLibrary/Models/PlayerObject.cs:16682-16705（节选）
public int GetElementPower(ObjectType race, Stat element)
{
    if (race != ObjectType.Player) return Stats[element];

    int min = 0;
    int max = Stats[element];
    int luck = Stats[Stat.Luck];

    if (min < 0) min = 0;
    if (min >= max) return max;

    if (luck > 0)
    {
        if (luck >= 10) return max;
        if (SEnvir.Random.Next(10) < luck) return max;
    }
    else if (luck < 0)
    {
        if (luck < -SEnvir.Random.Next(10)) return min;
    }
    ...
}
```

- 物理链路的元素公式不同（PlayerObject.cs:15293-15313）：逐元素 `power += GetElementValue(ele)`，抗性减伤 `res <= 0 时 power -= value * res * 3 / 10`（负抗反而加成 30%）、`res > 0 时 power -= value * res * 2 / 10`（仅对元素增量部分生效，与魔法链路对总伤减伤不同）。
- 道士符系亲和（DarkAffinity/HolyAffinity）：`ExplosiveTalisman`/`EvilSlayer` 在 `ModifyPowerMultiplier` 里检查 `stats[Stat.DarkAffinity|HolyAffinity] >= 1` 追加 30%/60% 增伤（见下文技能公式）。

### 暴击/幸运/诅咒在魔法链路的结算顺序

1. **幸运（Luck）先于一切**：`GetMC/GetSC/GetSP`（MapObject.cs:1754-1819）与 `GetElementPower`、`GetLotusMana` 在取面板区间时 roll——`luck ≥ 10` 恒 max；`luck > 0` 时 `Random(10) < luck` 取 max；`luck < 0`（诅咒）时 `luck < -Random(10)` 取 min。即幸运决定威力区间取上限的概率，诅咒决定取下限的概率。
2. **威力区间 roll**（`UserMagic.GetPower`）在 `ModifyPowerAdditionner` 内发生（多数技能第一行 `power += Magic.GetPower() + Player.GetMC()`）。
3. **MR/元素减伤**（MagicAttack ①②）。
4. **暴击最后在受方**（`Attacked` 内，PlayerObject.cs:15747-15757 / MonsterObject.cs:2456-2460）：`Random(100) < attacker.Stats[CriticalChance]` 才 roll，倍率 PVP ×1.3、PVE ×2（怪物端再叠 `CriticalDamage%`）。
5. **诅咒相关命名**：无 `CursePow`。PK 诅咒配置为 `Config.PvPCurseDuration = 60min`、`Config.PvPCurseRate = 4`（Config.cs:116-117），用于红名惩罚（PlayerObject.cs:16367：`attacker.Stats[Stat.PKPoint] >= Config.RedPoint && Random(Config.PvPCurseRate) == 0` 时置诅咒 rate/time）。

### PVP 减免的真实逻辑（无全局系数）

全库搜索未发现 `isPvP`/全局 PVP 伤害倍率；PVP 差异由以下分散点构成：

| 位置 | 差异 |
| --- | --- |
| PlayerObject.cs:15684 | 闪避 roll：玩家攻击者 `Random(200)`，怪物 `Random(100)` —— 玩家打人更易被闪避 |
| PlayerObject.cs:15747-15754 | 暴击：玩家攻击 ×1.3，怪物 ×2.0（canReflect=false 时统一 ×1.2） |
| PlayerObject.cs:16684-16705 | 元素攻对玩家目标走 `[0,面板]` 随机 roll（期望约一半），对怪物全额 |
| PlayerObject.cs:15926-15927 | JudgementOfHeaven 反击：`damagePvP = Math.Min(50, GetMC()/5 + 元素/2)` 封顶 50 |
| CrushingWave.cs:79-80 / SeismicSlam.cs:63-64 | `if (ob.Race == ObjectType.Player) power /= 2;` 技能级 PVP 减半 |
| ExplosiveTalisman.cs:124-125 | 注释掉的 PVP 减半（`//power = (int)(power * 0.5F)`），说明曾计划全局化 |
| PoisonDust.cs:109 / WraithGrip.cs:69,80 | 毒值/PVP 时 tick 数打折（`duration * 7 / 10`、`duration * 3 / 10`） |

### 关键协议包

```csharp
// LibraryCore/Network/ClientPackets.cs:159-166
public sealed class Magic : Packet
{
    public MirDirection Direction { get; set; }
    public MirAction Action { get; set; }
    public MagicType Type { get; set; }
    public uint Target { get; set; }
    public Point Location { get; set; }
}

// LibraryCore/Network/ServerPackets.cs:206-220
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

相关包：`C.MagicToggle`（ClientPackets.cs:354-358，开关型技能）、`C.MagicKey`（344-351，键位）、`S.ObjectProjectile`（ServerPackets.cs:221-231，链电/连闪等二段投射）、`S.MagicCooldown`（489-492）、`S.MagicLeveled`、`S.NewMagic`、`S.ObjectSpell`（386+，地面 SpellObject 生成，FireWall/Tempest 用）。

## 具体技能伤害公式（21 例，四职业覆盖）

### Wizard

**1. FireBall（火球）** — ServerLibrary/Models/Magics/Wizard/FireBall.cs:82-87
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetMC();
    return power;
}
```
标准模板：`威力 = 技能威力 roll + MC roll`，之后统一过 MR → 火攻×2 → 火抗。命中后若有 Burning 强化技，附加 Burn 毒（`GetBurn` 返回 `burning.GetPower()`，MagicAttack 15560 处 `Value = damage * burnLevel / 10`，burnLevel = Burning.Level+1，FireBall.cs:18-40）。

**2. ThunderBolt（雷电术）** — ServerLibrary/Models/Magics/Wizard/ThunderBolt.cs:61-66
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetMC();
    return power;
}
```
600ms 固定延迟单体雷（ThunderBolt.cs:47）；Shocked 强化技按 `Random(MagicMaxLevel) <= shocked.Level` 概率附加震慑（19-29）。

**3. ChainLightning（连环闪电）** — ServerLibrary/Models/Magics/Wizard/ChainLightning.cs:145-157
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetMC();
    return power;
}

public override int ModifyPowerMultiplier(bool primary, int power, Stats stats = null, int extra = 0)
{
    power = power * 5 / (extra + 5);
    return power;
}
```
`extra` = powerDivisor，每跳链 `++powerDivisor`（141 行）：第 1 跳 ×5/5=100%，第 2 跳 ×5/6≈83%，第 3 跳 ×5/7≈71%……且只链怪物（74、111 行 `ob.Race != ObjectType.Monster` 过滤）。链的扩散是 `Random(powerDivisor) == 0` 抽签（77 行）。

**4. FireWall（火墙）** — ServerLibrary/Models/Magics/Wizard/FireWall.cs:124-136
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetMC();
    return power;
}

public override int ModifyPowerMultiplier(bool primary, int power, Stats stats = null, int extra = 0)
{
    power = (int)(power * 0.60F);
    return power;
}
```
十字 5 格生成 `SpellObject`（TickCount = (Level+2)×5，每 2s tick，FireWall.cs:102-110），每次 tick 对格上敌人走 MagicAttack 管线（SpellObject 侧调用，本文第 5 节公式），单 tick 打 6 折。攻城战同图旧火墙会被清掉（57-69）。

**5. MeteorShower（陨石雨）** — ServerLibrary/Models/Magics/Wizard/MeteorShower.cs:94-99
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetMC();
    return power;
}
```
无乘法折减；目标数 = `6 + Magic.Level`（53 行），从落点 3 格内随机抽，逐个 `GetDelayFromDistance(500, ob)` 弹道。

**6. FrozenEarth（冰冻大地）** — ServerLibrary/Models/Magics/Wizard/FrozenEarth.cs:79-92
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetMC();
    return power;
}

public override int ModifyPowerMultiplier(bool primary, int power, Stats stats = null, int extra = 0)
{
    if (!primary)
        power = (int)(power * 0.3F);
    return power;
}
```
方向直线 8 格 + 两翼副格（副格 `primary=false` 打 3 折）；自带 `Slow=10 / SlowLevel=3`（13-14 行），命中经 MagicAttack ⑥ 对怪上减速毒。

**7. FrostBite（寒冰刺）** — ServerLibrary/Models/Magics/Wizard/FrostBite.cs:42-78
```csharp
// 施法：给自己上 FrostBite buff（42-48）
Stats buffStats = new Stats
{
    [Stat.FrostBiteDamage] = Player.GetMC() + Magic.GetPower() + Player.Stats[Stat.IceAttack] * 2,
    [Stat.FrostBiteChance] = 5 + (Magic.Level * 5)
};
// buff 期间每次被击中累积 FrostBiteDamage（PlayerObject.cs:15764-15769）
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Math.Min(stats[Stat.FrostBiteDamage], Player.Stats[Stat.MaxMC] * 50 + Player.Stats[Stat.IceAttack] * 70);
    return power;
}
```
反击型：被攒的伤害爆发时经 `ActionType.DelayedMagicDamage` 走 MagicAttack，伤害上限 `MaxMC×50 + 冰攻×70`；只爆怪不爆 BOSS（59-67）。

**8. LightningStrike（连环闪电突）** — ServerLibrary/Models/Magics/Wizard/LightningStrike.cs:115-129
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int strikesRemaining = 0)
{
    power += Player.GetMC() + Magic.GetPower();
    return power;
}

public override int ModifyPowerMultiplier(bool primary, int power, Stats stats = null, int strikesRemaining = 0)
{
    var multiplier = (int)((MaxStrike - strikesRemaining) * (1 / MaxStrike));
    power += (power * multiplier);
    return power;
}
```
注意 `1 / MaxStrike` 是整数除法，`MaxStrike = Magic.Level + 2 ≥ 2` 时恒为 0 → multiplier=0，乘法分支实际无效（每次连击同威力）。这是照抄源码的**现状行为**，移植对齐时勿"顺手修复"。

### Taoist

**9. ExplosiveTalisman（爆裂符）** — ServerLibrary/Models/Magics/Taoist/ExplosiveTalisman.cs:109-129
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetSC();
    return power;
}

public override int ModifyPowerMultiplier(bool primary, int power, Stats stats = null, int extra = 0)
{
    if (stats != null && stats[Stat.DarkAffinity] >= 1)
        power += (int)(power * 0.3F);

    if (!primary)
    {
        power = (int)(power * 0.65F);
        //  if (ob.Race == ObjectType.Player)
        //      power = (int)(power * 0.5F);
    }
    return power;
}
```
暗符（Element.Dark）：用暗符纸（`UseAmulet`，58 行）获得 `stats`，带 DarkAffinity 加成 30%；强化技 `AugmentExplosiveTalisman` 扩散目标的主/副目标分流——副目标（primary=false）×0.65。耗一张符打一次。

**10. EvilSlayer（圣言符/降妖除魔）** — ServerLibrary/Models/Magics/Taoist/EvilSlayer.cs:116-136
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetSC();
    return power;
}

public override int ModifyPowerMultiplier(bool primary, int power, Stats stats = null, int extra = 0)
{
    if (stats != null && stats[Stat.HolyAffinity] >= 1)
        power += (int)(power * 0.3F);
    if (!primary)
        power = (int)(power * 0.65F);
    return power;
}
```
圣符（Element.Holy）：仅佩戴 HolyAffinity 符纸时消耗（63-66 行的条件 `UseAmulet`）。进阶版 GreaterEvilSlayer 亲和加成升到 `0.6F`（GreaterEvilSlayer.cs:120-126，强化技扩散副目标同折减）。

**11. PoisonDust（施毒术，DoT 公式）** — ServerLibrary/Models/Magics/Taoist/PoisonDust.cs:105-114
```csharp
public override void MagicComplete(params object[] data)
{
    ...
    int duration = Magic.GetPower() + Player.GetSC() + Player.Stats[Stat.DarkAttack] * 2;

    ob.ApplyPoison(new Poison
    {
        Value = Magic.Level + 1 + Player.Level / 14,
        Type = type,                                    // shape==0 ? Green : Red（77 行，取决于毒药包形状）
        Owner = Player,
        TickCount = duration / 2,
        TickFrequency = TimeSpan.FromSeconds(2),
    });
    ...
}
```
毒伤 Value 每跳 = `Level+1+人物等级/14`；持续 = `(技能威力+SC+暗攻×2)/2` 个 2 秒 tick（不走 MagicAttack，无暴击/MR）。绿毒掉血、红毒增伤 20%（受方 Attacked 的 `power * 1.2F`）。强化技 `AugmentPoisonDust`（类名 GreaterPoisonDust.cs，MagicType.AugmentPoisonDust）扩撒最多 `GetPower()+1` 个目标。

**12. SearingLight（圣光）** — ServerLibrary/Models/Magics/Taoist/SearingLight.cs:63-68 + 49-60
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetSC();
    return power;
}
// MagicComplete：命中且 target.Level <= Player.Level + 2 时 1/3 概率上 Fear 毒
if (SEnvir.Random.Next(3) == 0)
{
    target.ApplyPoison(new Poison
    {
        Type = PoisonType.Fear,
        TickCount = 1,
        TickFrequency = TimeSpan.FromSeconds(Magic.Level + 2),
        Owner = Player,
    });
}
```

**13. HeavenlySky（天空之力，SC×威力乘算特例）** — ServerLibrary/Models/Magics/Taoist/HeavenlySky.cs:56-61
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() * Player.GetDC();
    return power;
}
```
以自身为中心 2 格范围雷（Element.Lightning），威力是**技能威力 × DC**（乘算而非加算），道士的物魔混伤特例。

**14. SoulResonance（灵魂共振，无伤害特例）** — ServerLibrary/Models/Magics/Taoist/SoulResonance.cs:52-64
```csharp
Stats ownerStats = new() { [Stat.SoulResonance] = target.Character.Index };
Stats targetStats = new()
{
    [Stat.HealthPercent] = Magic.GetPower(),
    [Stat.SoulResonance] = Player.Character.Index
};
Player.BuffAdd(BuffType.SoulResonance, TimeSpan.MaxValue, ownerStats, false, false, TimeSpan.Zero);
target.BuffAdd(BuffType.SoulResonance, TimeSpan.MaxValue, targetStats, false, false, TimeSpan.Zero);
```
队友绑命：一方死亡双方同死（`Activate`，88-113 行）。列出以说明：无 `ModifyPower*`、不走 `MagicAttack` 的"魔法"也存在。

### Warrior

**15. HundredFist（百拳/百裂拳，冲撞挤压伤害）** — ServerLibrary/Models/Magics/Warrior/HundredFist.cs:124-145
```csharp
// MagicComplete → TargetPush：目标被撞且未撞满距离时结算
var damage = Player.MagicAttack(new List<MagicType> { Type }, ob, extra: pushed);   // 129 行

public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + (Player.GetDC() * extra);   // extra = 实际被推动格数
    return power;
}
```
罕见地**战士技能走 MagicAttack**（元素 None → PhysicalResistance 分支）：推动格数越多伤害越高。位移本体现身目标背后再推（62-100 行）。

**16. CrushingWave（破空斩，物理管线合流）** — ServerLibrary/Models/Magics/Warrior/CrushingWave.cs:62-83
```csharp
public override void MagicComplete(params object[] data)
{
    ...
    Player.Attack(cell.Objects[i], new List<MagicType> { Type }, (bool)data[2], 0);   // 物理 Attack 管线
}

public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    if (!primary)
        power = power * Magic.GetPower() / 100;    // 副格：物理 power × 技能威力%

    if (ob.Race == ObjectType.Player)
        power /= 2;                                 // PVP 减半

    return power;
}
```
12 格直线波（26-57 行逐格延迟 `400+i*60ms`），`Player.Attack` 起点伤害为 `GetDC()`（PlayerObject.cs:15213），主格全额、副格按威力百分比。**注意主格 power 不乘技能威力**——只对副格折减。

**17. SeismicSlam（震地击）** — ServerLibrary/Models/Magics/Warrior/SeismicSlam.cs:59-67
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power = power * Magic.GetPower() / 100;    // 全格 DC × 技能威力%

    if (ob.Race == ObjectType.Player)
        power /= 2;

    return power;
}
```
朝向 3 格前方的 3×3 区域（29 行），全部走 `Player.Attack`；命中后 AttackComplete 追加麻痹/缠魂/沉默三毒（69-96 行）。

**18. BladeStorm（旋风剑，充能攻击技）** — ServerLibrary/Models/Magics/Warrior/BladeStorm.cs:79-84
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power = power * Magic.GetPower() / 100;
    return power;
}
```
`Toggle` 充能 12 秒窗口（34-61 行），下一次普攻经 `AttackCast`（63-77 行）合流进 `Player.Attack`；物理管线里 BladeStorm 还有二次折半 + 300ms 延迟二段伤（PlayerObject.cs:15344-15347）。

### Assassin

**19. FlamingDaggers（飞刀/火刃投掷）** — ServerLibrary/Models/Magics/Assassin/FlamingDaggers.cs:49-54 + 11-13
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetSP();    // SP = min(MC, SC)（MapObject.cs:1798-1819）
    return power;
}
// protected override int Burn => 10;      // 燃烧毒 tick 数
// protected override int BurnLevel => 2;  // Value = damage * 2 / 10 = 20% 伤害/跳
```
火元素弹道（`GetDelayFromDistance(1000, target)`），命中经 MagicAttack 标准管线 + Burn 毒（10 跳、每 2 秌、每跳 20% 伤害）。

**20. WraithGrip（缠魂术，MP 榨取）** — ServerLibrary/Models/Magics/Assassin/WraithGrip.cs:58-72
```csharp
int power = Player.GetSP();
int duration = Magic.GetPower();

UserMagic touchOfTheDeparted = GetAugmentedSkill(MagicType.TouchOfTheDeparted);

ob.ApplyPoison(new Poison
{
    Value = power,
    Type = PoisonType.WraithGrip,
    Owner = Player,
    TickCount = ob.Race == ObjectType.Player ? duration * 7 / 10 : duration,   // PVP 打 7 折
    TickFrequency = TimeSpan.FromSeconds(1),
    Extra = touchOfTheDeparted,
});
```
每秒榨 `SP` 点 MP；强化技 TouchOfTheDeparted 存在时另上麻痹毒（PVP `duration*3/10`，76-85 行）且施法者回蓝 `Value×(Level+1)`（MapObject.cs:240-244）。等级压制：玩家目标 `target.Level >= Player.Level` 直接免疫（34 行）。

**21. HellFire（狱火符）** — ServerLibrary/Models/Magics/Assassin/HellFire.cs:50-73
```csharp
if (Player.MagicAttack(new List<MagicType> { Type }, ob, true) <= 0) return;

int power = Math.Min(Player.GetSC(), Player.GetMC()) / 2;
int duration = Magic.GetPower();

ob.ApplyPoison(new Poison
{
    Value = power,
    Type = PoisonType.HellFire,
    Owner = Player,
    TickCount = duration / 2,
    TickFrequency = TimeSpan.FromSeconds(2),
});
...
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += Magic.GetPower() + Player.GetDC();   // 直击段用 DC（刺客物魔混搭）
    return power;
}
```
双段：直击（火元素 MagicAttack，威力=技能+DC）+ 狱火 DoT（`min(SC,MC)/2` 每跳，`duration/2` 跳）。

**22. RedLotus（红莲，莲系终结技）** — ServerLibrary/Models/Magics/Assassin/RedLotus.cs:72-104
```csharp
public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    bool hasStone = Player.Equipment[(int)EquipmentSlot.Amulet]?.Info.ItemType == ItemType.DarkStone;

    int bonus = Player.GetLotusMana(ob.Race) * Magic.GetPower() / 1000;
    int res;

    power = Math.Max(0, power - ob.GetAC() + Player.GetDC());

    if (Player.Buffs.Any(x => x.Type == BuffType.WhiteLotus))
    {
        bonus *= 3;
        power += Math.Max(0, Player.Stats[Stat.MaxDC] - 100);
    }

    power += Math.Max(0, bonus - ob.GetMR());

    if (ob.Race == ObjectType.Player)
        res = ob.Stats.GetResistanceValue(hasStone ? Player.Equipment[(int)EquipmentSlot.Amulet].Info.Stats.GetAffinityElement() : Element.None);
    else
        res = ob.Stats.GetResistanceValue(Element.None);

    if (res > 0)
        power -= power * res / 10;
    else if (res < 0)
        power -= power * res / 5;

    Player.BuffRemove(BuffType.WhiteLotus);
    Player.BuffAdd(BuffType.RedLotus, TimeSpan.FromSeconds(15), null, false, false, TimeSpan.Zero);
    ob.Broadcast(new S.ObjectEffect { ObjectID = ob.ObjectID, Effect = Effect.RedLotus });

    return power;
}
```
莲系三连（FullBloom → WhiteLotus → RedLotus）终结技：物理管线（AttackSkill，`power` 起点 DC），`GetLotusMana`（PlayerObject.cs:16657-16681，带幸运 roll 的法力值）按 `×威力/1000` 折成 bonus，WhiteLotus 状态下 bonus×3 且追加 `MaxDC−100`；自带独立抗性结算（正抗 /10、负抗反增 /5）并消耗 WhiteLotus。WhiteLotus/SweetBrier/FullBloom 同款公式框架（WhiteLotus.cs:71-75、SweetBrier.cs:72-76、FullBloom.cs:70-74 均以 hasStone 开头）。

## 全量技能清单（ServerLibrary/Models/Magics/，190 类）

标注说明：**★** = 覆盖 `ModifyPowerAdditionner`/`ModifyPowerMultiplier`（伤害公式自定义，本文已给行号）；**△** = 无 ModifyPower 覆盖但在 `MagicComplete`/其他位置自算伤害或毒值；空白 = 非直接伤害技（buff/位移/召唤/被动，经 grep 全目录核实无 `Player.MagicAttack/Player.Attack` 调用且无 ModifyPower 覆盖）。元素列来自各类 `protected override Element Element`。

### Warrior（37 类）

| 类名 | MagicType | 元素 | 伤害机制 | 标注 |
| --- | --- | --- | --- | --- |
| AdvancedPotionMastery | AdvancedPotionMastery | None | 非伤害（药水被动，无伤害调用） | |
| Assault | Assault | None | 非伤害（冲锋位移类，无伤害调用） | |
| AugmentDefiance | AugmentDefiance | None | 非伤害（强化技：延长 Defiance buff） | |
| AugmentDestructiveSurge | AugmentDestructiveSurge | None | AttackSkill；`power += Magic.GetPower()`（18-21 行） | ★ |
| AugmentReflectDamage | AugmentReflectDamage | None | 非伤害（强化技：反伤相关） | |
| Beckon | Beckon | None | 诱惑怪为宠物；附 Paralysis 毒（91-94 行） | △ |
| BladeStorm | BladeStorm | None | AttackSkill 充能 12s；`power = power * Magic.GetPower() / 100`（79-84 行） | ★ |
| CrushingWave | CrushingWave | None | 直线 12 格物理波；副格 `power × 威力%`、PVP 减半（74-83 行） | ★ |
| DefensiveBlow | DefensiveBlow | None | AttackSkill；命中上 DefensiveBlow buff（76 行），无 ModifyPower | |
| DefensiveMastery | DefensiveMastery | None | 被动：`Stat.DefensiveMastery` 供 GetAC 幸运取 max（MapObject.cs:1829-1834） | |
| Defiance | Defiance | None | 非伤害（防御 buff，66 行） | |
| DestructiveSurge | DestructiveSurge | None | AttackSkill；副格 `power × 威力%`（79-84 行） | ★ |
| DragonRise | DragonRise | None | AttackSkill 充能；`power = power * Magic.GetPower() / 100`（88-91 行） | ★ |
| ElementalSwords | ElementalSwords | None | 元素剑 buff + 投射攻击：`power += Magic.GetPower()`（102-105 行），命中 MagicAttack（92 行） | ★ |
| Endurance | Endurance | None | 非伤害（10+Level×5 秒免推 buff，37 行） | |
| Fetter | Fetter | None | 束缚毒（Value=(3+Level)×2，55-58 行） | △ |
| FireSword | FireSword | Fire | 火剑投射：`power += Magic.GetPower() + Player.GetDC()`（64-67 行） | ★ |
| FlamingSword | FlamingSword | None | AttackSkill 充能；`power = power × 威力%`（80-83 行） | ★ |
| HalfMoon | HalfMoon | None | AttackSkill 半月；副格 `power × 威力%`（60-64 行） | ★ |
| HundredFist | HundredFist | None | 冲撞挤压：`power += Magic.GetPower() + DC × 推动格数`（140-145 行），走 MagicAttack | ★ |
| Interchange | Interchange | None | 换位（Teleport，78-79 行），非伤害 | |
| Invincibility | Invincibility | None | 非伤害（无敌 buff，43 行） | |
| MagicImmunity | MagicImmunity | None | 被动：受法伤时 `power -= power × GetPower()/100`（PlayerObject.cs:15693-15705） | △ |
| MassBeckon | MassBeckon | None | 群体诱惑，附 Paralysis 毒（50-53 行） | △ |
| Might | Might | None | 非伤害（DC buff，55 行） | |
| OffensiveBlow | OffensiveBlow | None | AttackSkill；命中上 Paralysis+Silenced 毒（79-90 行），`power = power × 威力%`（68-71 行） | ★ |
| PhysicalImmunity | PhysicalImmunity | None | 被动：受物伤时百分比减伤（PlayerObject.cs:15715-15727） | △ |
| PotionMastery | PotionMastery | None | 非伤害（药水被动） | |
| ReflectDamage | ReflectDamage | ReflectDamage 处 None | 非伤害（反伤 buff，55 行） | |
| SeismicSlam | SeismicSlam | None | 3×3 物理震：`power × 威力%`、PVP 减半（59-67 行）+ 三毒（69-96 行） | ★ |
| ShoulderDash | ShoulderDash | None | 猛冲位移；撞人附 Paralysis/Silenced 毒（222-233 行） | △ |
| Shuriken | Shuriken | None | 投掷被动标记（IgnoreAccuracy；伤害在 RangeAttack/Attack 管线合并结算） | |
| Slaying | Slaying | None | AttackSkill（攻杀剑术）；`power += Magic.GetPower()`（41-44 行） | ★ |
| SwiftBlade | SwiftBlade | None | AttackSkill 群体斩；`power = power × 威力%`（72-75 行） | ★ |
| Swordsmanship | Swordsmanship | None | AttackSkill 基础剑法（无 ModifyPower，命中升级） | |
| TaecheonSword | TaecheonSword | Fire | 太宙剑范围火：`power` 随离中心距离衰减（`multiplier = Max(0, 4-extra)`，58-61 行） | ★ |
| Thrusting | Thrusting | None | AttackSkill 刺杀；副格 `power × 威力%`（57-61 行） | ★ |

### Wizard（46 类）

| 类名 | MagicType | 元素 | 伤害机制 | 标注 |
| --- | --- | --- | --- | --- |
| AdamantineFireBall | AdamantineFireBall | Fire | 金刚火球：`power += GetPower() + GetMC()`（82-85 行）+ Burning 燃烧 | ★ |
| Asteroid | Asteroid | Fire | 陨石范围（CanStruck=false）；`power += GetPower() + GetMC()`（123-126 行） | ★ |
| BlowEarth | BlowEarth | Wind | 击退地刺（Repel=10）；`power += GetPower()+MC`，副格 ×0.3（85-95 行） | ★ |
| Burning | Burning | Fire | 燃烧强化技（无直接施放伤害；GetPower 供 FireBall/FireWall/FireStorm/MeteorShower 的 Burn 毒） | △ |
| ChainLightning | ChainLightning | Lightning | 链电：加法标准 + `power × 5/(extra+5)` 逐跳衰减（145-157 行），只链怪 | ★ |
| Cyclone | Cyclone | Wind | 旋风（Repel=5）；`power += GetPower()+MC`（50-53 行） | ★ |
| DragonTornado | DragonTornado | Wind | 龙卷（Repel=5）；`power += GetPower()+MC`（64-67 行） | ★ |
| ElectricShock | ElectricShock | None | 非伤害（麻痹怪 buff 类，UpdateCombatTime=false） | |
| ElementalHurricane | ElementalHurricane | 动态 | 元素风暴：`GetElement` 动态返回最高元素（17 行起）；`power += GetPower()+MC`，副格 ×0.3M（89-99 行） | ★ |
| ExpelUndead | ExpelUndead | None | 驱退不死系（无伤害调用） | |
| FireBall | FireBall | Fire | 火球：`power += GetPower()+MC`（82-87 行）+ Burning | ★ |
| FireBounce | FireBounce | Fire | 弹跳火球：`power += GetPower()+MC`（bounce 计数在 extra，132-135 行） | ★ |
| FireStorm | FireStorm | Fire | 火风暴：`power += GetPower()+MC`（93-96 行）+ Burning | ★ |
| FireWall | FireWall | Fire | 火墙 SpellObject：加法标准 + ×0.60F（124-136 行）；TickCount=(Level+2)×5 | ★ |
| FrostBite | FrostBite | Ice | 反击蓄力：`power += Min(FrostBiteDamage, MaxMC×50+冰攻×70)`（73-78 行） | ★ |
| FrozenDragon | FrozenDragon | Ice | 冰龙（Slow=2/SlowLevel=5）；`power += GetPower()+MC`（63-66 行） | ★ |
| FrozenEarth | FrozenEarth | Ice | 直线冰地（Slow=10/3）；副格 ×0.3F（79-92 行） | ★ |
| GeoManipulation | GeoManipulation | None | 非伤害（地图传送，53 行） | |
| GreaterFrozenEarth | GreaterFrozenEarth | Ice | 大冰地（Slow=5/5）；副格 ×0.3F（82-92 行） | ★ |
| GustBlast | GustBlast | Wind | 疾风（Repel=10）；`power += GetPower()+MC`（50-53 行） | ★ |
| IceAura | IceAura | Ice | 冰灵气 SpellObject（ModifyPower 代码整段被注释，75-86 行） | |
| IceBlades | IceBlades | Ice | 冰刃（Slow=5/5）；`power += GetPower()+MC`（50-53 行） | ★ |
| IceBolt | IceBolt | Ice | 冰箭（Slow=10/3）；`power += GetPower()+MC`（50-53 行） | ★ |
| IceBreaker | IceBreaker | Ice | 冰破（Slow=5/5）；`power += GetPower()+MC`（60-63 行） | ★ |
| IceDragon | IceDragon | Ice | 冰龙弹（Slow=2/3）；`power += GetPower()+MC`（50-53 行） | ★ |
| IceRain | IceRain | Ice | 冰雨随机格；`power += GetPower()+MC`（71-74 行） | ★ |
| IceStorm | IceStorm | Ice | 冰风暴（Slow=5/5）；`power += GetPower()+MC`（61-64 行） | ★ |
| JudgementOfHeaven | JudgementOfHeaven | None | 被击雷反 buff：反伤 `GetMC()/5 + 雷攻×2`，PVP 封顶 50（PlayerObject.cs:15924-15934） | △ |
| LightningBall | LightningBall | Lightning | 雷球：`power += GetPower()+MC`（61-64 行） | ★ |
| LightningBeam | LightningBeam | Lightning | 光束：加法标准，副格 ×0.3F（90-100 行） | ★ |
| LightningStrike | LightningStrike | Lightning | 连闪：加法标准 + 整数除法致乘法恒 0 的 multiplier（115-129 行，见技能公式 8） | ★ |
| LightningWave | LightningWave | Lightning | 雷波：`power += GetPower()+MC`（74-77 行） | ★ |
| MagicShield | MagicShield | None | 非伤害（魔盾 buff：30+Level×20+MC/2+幻攻×2 秒，44 行） | |
| MeteorShower | MeteorShower | Fire | 陨石雨：加法标准（94-99 行）；目标 6+Level 个 + Burning | ★ |
| MirrorImage | MirrorImage | None | 非伤害（镜像分身召唤，82 行） | |
| Renounce | Renounce | None | 非伤害（血换蓝 buff，56-59 行） | |
| Repulsion | Repulsion | None | 非伤害（抗性/推离 buff） | |
| ScortchedEarth | ScortchedEarth | Fire | 焦土：加法标准，副格 ×0.3F（111-121 行） | ★ |
| Shocked | Shocked | Lightning | 麻痹强化技（GetPower 供 ThunderBolt/ChainLightning 的 Shock 值） | △ |
| Storm | Storm | Wind | 风暴（无 ModifyPower，SpellObject 型范围） | |
| SuperiorMagicShield | SuperiorMagicShield | None | 非伤害（圣言盾吸收 buff，46 行） | |
| Teleportation | Teleportation | None | 非伤害（随机传送，53 行） | |
| Tempest | Tempest | Wind | 风暴 SpellObject（Repel=5，CanStruck=false）；加法标准 + ×0.80F（98-108 行） | ★ |
| ThunderBolt | ThunderBolt | Lightning | 雷电术：加法标准（61-66 行）+ Shocked 震慑 | ★ |
| ThunderStrike | ThunderStrike | Lightning | 迅雷：加法标准再 `power += power / 2`（×1.5，80-84 行） | ★ |
| Tornado | Tornado | Wind | 非直接伤害（龙卷 SpellObject 召唤 + regen buff，76-78 行） | |

### Taoist（51 类）

| 类名 | MagicType | 元素 | 伤害机制 | 标注 |
| --- | --- | --- | --- | --- |
| AugmentCelestialLight | AugmentCelestialLight | None | 非伤害（强化技：天灯延展） | |
| AugmentEvilSlayer | AugmentEvilSlayer | None | 强化技（扩散目标数 = GetPower()+1；无自身 ModifyPower） | |
| AugmentExplosiveTalisman | AugmentExplosiveTalisman | None | 强化技（同上扩散机制） | |
| AugmentNeutralize | AugmentNeutralize | None | 非伤害（强化解毒） | |
| AugmentPurification | AugmentPurification | None | 非伤害（强化净化） | |
| AugmentResurrection | AugmentResurrection | None | 非伤害（强化复活） | |
| BindingTalisman | BindingTalisman | Holy | 定身符：`power += Player.GetSC()`（87-90 行）+ Binding 束缚毒（77-80 行） | ★ |
| BloodLust | BloodLust | None | 非伤害（嗜血 buff，64 行） | |
| BrainStorm | BrainStorm | Holy | 圣暴：`power += Player.GetSC() × (Level+1)`（88-91 行） | ★ |
| CelestialLight | CelestialLight | None | 非伤害（天灯 buff，70 行） | |
| CombatKick | CombatKick | None | 战踢：走 `Player.Attack`（51 行），无 ModifyPower | |
| CorpseExploder | CorpseExploder | None | 尸爆：对尸体范围怪 `MagicAttack`（77 行）；`power += GetPower()+SC`（82-85 行） | ★ |
| CursedDoll | CursedDoll | None | 诅咒娃娃召唤物（Spawn，70-71 行），伤害在娃娃 AI | |
| DarkSoulPrison | DarkSoulPrison | Dark | 黑暗监狱 SpellObject：`power += GetPower()+SP`，×0.40M（83-93 行） | ★ |
| DemonExplosion | DemonExplosion | Phantom | 恶魔爆炸：`power = extra`（105-108 行，威力=召唤物传入值），SpellObject 爆炸（101 行） | ★ |
| DemonicRecovery | DemonicRecovery | None | 非伤害（恶魔回复） | |
| ElementalSuperiority | ElementalSuperiority | None | 非伤害（元素优越 buff，116 行） | |
| EmpoweredHealing | EmpoweredHealing | None | 非伤害（强化治疗被动） | |
| EvilSlayer | EvilSlayer | Holy | 圣言符：`GetPower()+SC`；HolyAffinity +30%、副格 ×0.65（116-136 行） | ★ |
| ExplosiveTalisman | ExplosiveTalisman | Dark | 爆裂符：`GetPower()+SC`；DarkAffinity +30%、副格 ×0.65（109-129 行） | ★ |
| GreaterEvilSlayer | GreaterEvilSlayer | Holy | 大圣言：同 EvilSlayer 但亲和 +60%（113-126 行） | ★ |
| GreaterPoisonDust（类名） | AugmentPoisonDust | None | 施毒强化技（扩散毒目标，无自身伤害） | |
| Heal | Heal | None | 非伤害（治疗，UpdateCombatTime=false） | |
| HeavenlySky | HeavenlySky | Lightning | 天空之力：`power += GetPower() × GetDC()`（56-61 行，乘算特例） | ★ |
| ImprovedExplosiveTalisman | ImprovedExplosiveTalisman | Dark | 强爆符：亲和 +60%、副格 ×0.65（108-118 行） | ★ |
| Infection | Infection | None | 感染（配合 Parasite 爆发，ProcessPoison 258-265 行回调） | △ |
| Invisibility | Invisibility | None | 非伤害（隐身 buff） | |
| LifeSteal | LifeSteal | None | 非伤害（吸血被动 buff） | |
| MagicResistance | MagicResistance | None | 非伤害（魔抗 buff） | |
| MassHeal | MassHeal | None | 非伤害（群体治疗） | |
| MassInvisibility | MassInvisibility | None | 非伤害（群体隐身） | |
| Neutralize | Neutralize | None | 非伤害（中和毒） | |
| Parasite | Parasite | None | 寄生毒：`power += GetPower() + SC/2`（62-65 行），到期爆炸 | ★ |
| PoisonCloud | PoisonCloud | None | 毒云 SpellObject（范围上毒，无 ModifyPower） | |
| PoisonDust | PoisonDust | None | 施毒：毒值 `Level+1+人物等级/14`/跳，`(GetPower()+SC+暗攻×2)/2` 跳（105-114 行） | △ |
| Purification | Purification | None | 非伤害（净化） | |
| Resilience | Resilience | None | 非伤害（韧性 buff） | |
| Resurrection | Resurrection | None | 非伤害（复活） | |
| SearingLight | SearingLight | Holy | 圣光：`GetPower()+SC`（63-68 行）+ 1/3 Fear 毒（51-60 行） | ★ |
| SoulResonance | SoulResonance | None | 绑命 buff（HealthPercent=GetPower()，52-64 行），无伤害 | |
| SpiritSword | SpiritSword | None | AttackSkill（精神剑被动，无 ModifyPower） | |
| Spiritualism | Spiritualism | None | 非伤害（空间移动；覆写 GetElement 但无伤害） | |
| StrengthOfFaith | StrengthOfFaith | None | 非伤害（信仰 buff，50 行） | |
| SummonDead | SummonDead | None | 召唤亡灵（Spawn+SetHP，96-99 行） | |
| SummonDemonicCreature | SummonDemonicCreature | None | 召唤恶魔（84-87 行） | |
| SummonJinSkeleton | SummonJinSkeleton | None | 召唤真骨架（84-87 行） | |
| SummonShinsu | SummonShinsu | None | 召唤神兽（84-87 行） | |
| SummonSkeleton | SummonSkeleton | None | 召唤骷髅（86-89 行） | |
| ThunderKick | ThunderKick | None | 雷踢：走 `Player.Attack`（67 行），无 ModifyPower | |
| Transparency | Transparency | None | 非伤害（透明 buff；PVP 后 30s 内限时 20s，66 行） | |
| TrapOctagon | TrapOctagon | None | 八卦陷阱 SpellObject（Spawn+震慑，88-91 行） | |

### Assassin（56 类）

| 类名 | MagicType | 元素 | 伤害机制 | 标注 |
| --- | --- | --- | --- | --- |
| Abyss | Abyss | None | 深渊毒（Value=power，65-68 行） | △ |
| AdventOfDemon | AdventOfDemon | None | 被动：受物伤时升级（PlayerObject.cs:15945-15946） | |
| AdventOfDevil | AdventOfDevil | None | 被动：受法伤时升级（PlayerObject.cs:15948-15949） | |
| ArtOfShadows | ArtOfShadows | None | 非伤害（影术被动，无伤害调用） | |
| BloodyFlower | BloodyFlower | None | AttackSkill（血花被动，无 ModifyPower） | |
| BurningFire | BurningFire | Fire | 火地 SpellObject + tick `MagicAttack`：`power += GetPower()+SP`（96-99 行） | ★ |
| CalamityOfFullMoon | CalamityOfFullMoon | None | AttackSkill：`power += Magic.GetPower()`（45-48 行） | ★ |
| Chain | Chain | None | 锁链毒（PoisonType.Chain，122-125 行；伤害经 Chain.PoisonTick 与 SiphonDamage） | △ |
| ChainOfFire | ChainOfFire | None | 火链：`power += GetMC() × GetPower()/100`（43-46 行），副击 primary=false（38 行） | ★ |
| ChangeOfSeasons | ChangeOfSeasons | None | 非伤害（四季被动） | |
| Cloak | Cloak | None | 非伤害（隐身衣 buff；以 HP 的 Cost/1000 为代价，110 行） | |
| Concentration | Concentration | None | 非伤害（专注 buff，40 行） | |
| Containment | Containment | None | 收容毒（Value 型 DoT，68-71 行；ProcessPoison 272-274） | △ |
| CrescentMoon | CrescentMoon | None | 新月：`power += GetPower() × GetSP()`（56-59 行，乘算） | ★ |
| DanceOfSwallow | DanceOfSwallow | None | 燕舞：`power += Player.GetDC()`（95-98 行）+ Silenced/Paralysis 毒（107-118 行） | ★ |
| DarkConversion | DarkConversion | None | 非伤害（暗转 buff，58 行） | |
| Discipline | Discipline | None | AttackSkill（心法系，无 ModifyPower） | |
| DragonBlood | DragonBlood | None | 龙血：Green 毒（69-72 行） | △ |
| DragonRepulse | DragonRepulse | Lightning | 龙推：`power = GetDC() × GetPower()/100 + Level`（53-56 行）+ 推位（Repel=5） | ★ |
| DragonWave | DragonWave | None | 攻击被动：`power += power × GetPower()/100`（17-20 行） | ★ |
| DualWeaponSkills | DualWeaponSkills | None | 双武器被动：`power += power × GetPower()/100`（29-32 行） | ★ |
| ElementalPuppet | ElementalPuppet | None | 元素傀儡：按 stats 注入（24-27 行，`if (stats == null \|\| stats.Count == 0) return power;`） | ★ |
| Evasion | Evasion | None | 非伤害（闪避 buff，45 行；被法术闪避时升级 PlayerObject.cs:15686-15687） | |
| FatalBlow | FatalBlow | None | AttackSkill：目标 HP<30% 时增伤（30-31 行条件） | ★ |
| FlameSplash | FlameSplash | None | AttackSkill 烈焰扩散：副格 `power × 威力%`（108-111 行） | ★ |
| FlamingDaggers | FlamingDaggers | Fire | 火刃投掷：`GetPower()+SP` + Burn 毒 10 跳×20%（49-54 行） | ★ |
| FlashOfLight | FlashOfLight | None | 闪光：`power += GetDC() × GetPower()/100`（72-75 行） | ★ |
| FourWheels | FourWheels | None | 四轮：`power += GetPower() × GetSP()`（56-59 行，乘算） | ★ |
| FullBloom | FullBloom | None | 莲一起手：hasStone 框架公式（71-74 行起） | ★ |
| GhostWalk | GhostWalk | None | 非伤害（魅影步移速 buff） | |
| HellFire | HellFire | Fire | 狱火符：直击 `GetPower()+DC` + DoT `min(SC,MC)/2`（68-73、52-63 行） | ★ |
| Hemorrhage | Hemorrhage | None | 出血：`power += Player.GetSP()`（65-68 行）+ Hemorrhage 毒（54-57 行） | ★ |
| Karma | Karma | None | 因果报应（AttackSkill；直伤在 Player.Attack：`ob.CurrentHP × GetPower()/100`，PlayerObject.cs:15350-15363；ModifyPower 另加 DC，81-84 行） | ★ |
| LastStand | LastStand | None | 非伤害（背水 buff，41 行） | |
| MagicCombustion | MagicCombustion | None | 魔燃：`power += Magic.GetPower()`（50-53 行） | ★ |
| Massacre | Massacre | None | 屠戮被动（HasMassacre；处决逻辑在 Player.Attack 的 extra 分支，PlayerObject.cs:15321-15334） | |
| PledgeOfBlood | PledgeOfBlood | None | 非伤害（血誓 buff） | |
| PoisonousCloud | PoisonousCloud | None | 毒云 SpellObject（Spawn，55 行） | |
| RagingWind | RagingWind | None | 非伤害（狂风 buff，40 行） | |
| Rake | Rake | None | 爪击（Slow=1/SlowLevel=10）：`power += GetDC() × GetPower()/100`（61-64 行） | ★ |
| RedLotus | RedLotus | None | 莲三终结：完整公式见技能公式 22 | ★ |
| Rejuvenation | Rejuvenation | None | 非伤害（回春被动） | |
| Release | Release | None | 非伤害（解脱） | |
| Resolution | Resolution | None | 非伤害（Karma 辅助：命中/破防，PlayerObject.cs:15240-15243、15270-15276） | |
| Shredding | Shredding | Fire | 撕裂：`GetPower()+SP` + Burn 毒（11-13、49-52 行） | ★ |
| Stealth | Stealth | Stealth 处 None | 非伤害（潜行） | |
| SummonPuppet | SummonPuppet | Fire | 傀儡召唤（Spawn+Pets，97-100 行；ModifyPower 加 DC×威力% 供傀儡爆炸，163-166 行） | ★ |
| SweetBrier | SweetBrier | None | 荆棘（莲系同款 hasStone 框架，72-76 行） | ★ |
| TheNewBeginning | TheNewBeginning | None | 非伤害（新的开始：重置冷却 buff，47 行） | |
| TouchOfTheDeparted | TouchOfTheDeparted | None | 非伤害（逝者之触：强化 WraithGrip 的 Extra 参数） | |
| VineTreeDance | VineTreeDance | None | AttackSkill（藤舞被动，无 ModifyPower） | |
| Vitality | Vitality | None | 非伤害（活力：低血触发，10-12 行 LowHP） | |
| WaningMoon | WaningMoon | None | AttackSkill：`power += Magic.GetPower()`（45-48 行） | ★ |
| WhiteLotus | WhiteLotus | None | 莲二：hasStone 框架公式（71-75 行起），为 RedLotus 蓄能（99-100 行） | ★ |
| WillowDance | WillowDance | None | 非伤害（柳舞闪避 buff） | |
| WraithGrip | WraithGrip | None | 缠魂：MP 榨取毒 `Value=SP`，PVP 7 折（58-72 行） | △ |

（类名与 MagicType 不一致的只有 `GreaterPoisonDust` 类 → `MagicType.AugmentPoisonDust`、`Shocked`/`Burning` 等强化技——反射匹配以 `[MagicType]` 特性为准。）

## GodotClient 现状

以下结论均来自对 GodotClient/ 的实际 glob/grep：

| 功能 | 状态 | 证据 |
| --- | --- | --- |
| 施法输入→`C.Magic` 发包 | **已移植** | GodotClient/Scripts/GameScene.cs:9634-9796 `UseMagicSlot`：目标解析（锁定>悬停>选中，9705-9726）、超距检查（9735-9741）、`new C.Magic{...}` 构建（9775-9782）、`_net.Connection.Enqueue(packet)`（9795） |
| 移动中施法排队 | **已移植** | GameScene.cs:801-802（`_pendingMagicPacket`）、9784-9793（移动动画中排队，`IsWalkAnimation` 1510-1512）、8083-8087（到点补发） |
| Toggle 技能 `C.MagicToggle` | **已移植** | GodotClient/Network/ServerConnection.cs:1089 `SendMagicToggle`；GameScene.cs:9759-9768 Toggle 分支 |
| `S.ObjectMagic` 渲染 | **已移植** | ServerConnection.cs:186、630-634（事件+缓冲队列）；GameScene.cs:3138-3191 `OnObjectMagic`（抬手无条件、`!cast` 跳过释放段） |
| 施法特效/弹道表 | **部分移植** | GodotClient/Scripts/MagicEffectTable.cs:19-54 `OriginalSpellCases` 白名单（约 70 项），未覆盖技能"不伪造特效、记诊断"（12 行注释）；持续补齐中 |
| 施法音效 | **已移植** | GodotClient/Scripts/MagicSoundCatalog.cs:19-22（Start/End/Duration 三段式，含 `!MagicCast` 门控语义） |
| 释放关键帧时序 | **已移植** | GodotClient/Scripts/PlayerRenderer.cs:114-118 `SpellReleaseDelayMs`（抬手第 4 逻辑帧释放） |
| `S.ObjectProjectile`（链电二段） | **已移植** | ServerConnection.cs:187、GameScene.cs:1285 订阅 |
| `S.ObjectSpell`/`ObjectSpellChanged`（火墙等地面物） | **已移植** | GameScene.cs:1286-1287 订阅；ServerConnection.cs:188-189 |
| `S.MagicCooldown`/`MagicLeveled`/`MagicToggle`/`NewMagic` | **已移植** | ServerConnection.cs:1113-1124（事件）、1116-1121（PendingNewMagics）；GameScene.cs:1272-1274 订阅、7665 `OnMagicCooldown` |
| `C.MagicKey` 键位绑定 | **已移植** | ServerConnection.cs:1126-1130 `SendMagicKey` |
| 技能栏/技能窗 UI | **已移植** | GodotClient/Controls/MagicBar.cs:10-43（12 列 24 槽、4 组键位）、GodotClient/Controls/MagicDialog.cs:13-43（职业页签+经验条+滚动） |
| 伤害飘字/受击 | **已移植** | GameScene.cs:4065、4080、4093 `SpawnDamagePopup`（miss/block/critical/resist 分类）；4174-4177 `OnObjectStruck` |
| 目标锁定记忆（原版 MagicObject 记住首目标） | **部分移植** | GameScene.cs:9706-9726 注释明确对齐原版语义，用 `_magicLockTargetObjectId`（9753-9757）自实现 |
| 莲系/充能技的服务器驱动 UI（S.MagicToggle 驱动图标状态） | **部分移植** | 事件链路在（OnMagicToggle），逐技能图标状态细节未逐一核对 |
| 伤害公式本身 | **无需移植** | 全部结算在服务端（本文第 5-7 节），客户端只消费 `S.ObjectStruck`/HP 变化飘字 |

## 移植注意事项

1. **不要在客户端复算伤害**。所有威力/暴击/抗性结算都在服务端 `MagicAttack → Attacked`；Godot 端只需表现（弹道、命中特效、飘字）。飘字的 critical/miss/block/resist 分类信号来自服务端包（GameScene.cs:4065 已实现），不要用客户端随机数模拟。
2. **延迟时序是权威数据**。弹道延迟统一为 `500ms + 距离×48ms`（`GetDelayFromDistance`，MagicObject.cs:208-220；EvilSlayer 等符系为 `500 + 距离×48`，见 EvilSlayer.cs:76）。移植新技能特效时按此对齐落点时间，而不是自定常数。FrozenEarth/ChainLightning 等多段技能的每段延迟都在各自 `MagicCast` 里（如 ChainLightning.cs:52 `primary?600:200ms`）。
3. **`S.ObjectMagic.Cast=false` 的语义**：施法失败/被拒但仍广播动作——客户端必须播抬手、跳过释放段（服务端 PlayerObject.cs:14864-14869 `Return`、14882-14885 条件冷却；GodotClient GameScene.cs:3165-3169 已对齐）。
4. **元素攻防的 PVP/PVE 分叉**（`GetElementPower` 对玩家目标 roll、对怪全额）在服务端；Godot 端若做元素面板显示，注意 `Stats.GetResistanceValue`（Stat.cs:477-499）的正抗减伤、负抗增伤（物理链路负抗 `×3/10` 反加成）与魔法链路不同。
5. **技能等级只有 0-3 级**，`/3` 整数除法出现在 `GetPower`/`Cost` 两处（UserMagic.cs:168、181-182）；`MagicMaxLevel=4` 是内部档位数，别当成 4 级。升级门槛双校验：人物等级 ≥ NeedLevel1/2/3 且经验满（LevelMagic，PlayerObject.cs:16126-16152）。
6. **幸运/诅咒影响的是"取区间端点"**，不是最终乘数；luck≥10 恒上限是硬编码（MapObject.cs:1763-1768 等五处同构）。做属性面板时要说明 Luck 的真实语义。
7. **`LightningStrike` 的整数除法 bug**（multiplier 恒 0，LightningStrike.cs:124）与 `IceAura` 被注释掉的 ModifyPower（75-86 行）是**现状**；移植对齐时保持一致，否则客户端表现与服务端伤害不符。
8. **物理/魔法双管线**：战士充能技（BladeStorm/FlamingSword 等）和刺客莲系/AttackSkill 走 `Player.Attack`（元素来自武器/暗石，走 `GetElementValue` 逐元素累加公式，PlayerObject.cs:15293-15313）；法师道士直伤走 `MagicAttack`（单一元素、元素攻×2 公式）。查公式时先确认技能类 `AttackSkill` 标志再选管线。
9. **毒素不走暴击/MR**：DoT 值在 `ApplyPoison` 时定死，`ProcessPoison` 每 tick 直扣（BOSS 免疫、非 CanKill 毒最多扣到 1 HP，MapObject.cs:289-297）。客户端毒素特效时长应按 `TickCount × TickFrequency` 对齐。
10. **`UserMagic.Cost` 与 `CheckCost`**：Discipline 系技能耗 FP（MagicObject.cs:114-117）；Superman（GM 无敌）免费施法（72-77、107-110、339-344）。客户端蓝量预检（GodotClient GameScene.cs:9770 `magic.Cost > _currentMP`）与服务端 `CheckCost` 一致即可，不做二次扣蓝。
11. **190 个技能类的注册靠反射**（`SEnvir.MagicTypes` + `[MagicType]` 特性，PlayerObject.cs:247-280）。Godot 端不需要对应类，但若做技能数据库工具/校验，可用同一特性表核对 System.db 的 Magic 字段与类的一一对应（唯一不一致见清单末尾注记）。
12. **冷却**：`MagicCooldown` 以 `Info.Delay`（ms）写 `UserMagic.Cooldown` 并广播；客户端 `ClientUserMagic.Cooldown` 是剩余 TimeSpan（UserMagic.cs:206）。移动/减速（Slow 毒 `Value×100ms`，PlayerObject.cs:14898-14905）会推迟 ActionTime/MagicTime，但**不推迟** Cooldown——两者是独立计时器，移植时别合并。
