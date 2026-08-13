# 物理攻击全链路（physical-damage）

## TL;DR 速查表

- 入口链：客户端 `C.Attack`（`LibraryCore/Network/ClientPackets.cs:142`）→ `SConnection.Process(C.Attack)`（`ServerLibrary/Envir/SConnection.cs:501`）→ `PlayerObject.Attack(MirDirection, MagicType)`（`ServerLibrary/Models/PlayerObject.cs:14714`）→ `AttackLocation` 排 300ms 延迟动作 → `Attack(MapObject, List<MagicType>, bool, int)`（`ServerLibrary/Models/PlayerObject.cs:15205`）算伤害 → `ob.Attacked(...)` 落血 → `Broadcast(S.ObjectAttack)`（`ServerLibrary/Models/PlayerObject.cs:14812`）。
- 命中判定：`SEnvir.Random.Next(目标 Agility) > 攻击方 Accuracy` → 闪避（`ServerLibrary/Models/PlayerObject.cs:15246`）；无显式命中率上下限钳制，边界由整数随机自然形成。
- 基础伤害：`GetDC()` 在 `[MinDC, MaxDC]` 取随机值，Luck>0 时 `Random.Next(10) < luck` 概率直接取 max，Luck<0（诅咒）时概率取 min（`ServerLibrary/Models/MapObject.cs:1732`）。
- 攻速节流：`attackDelay = Math.Max(800, 1500 - AttackSpeed*47)`；超重/Neutralize 毒再翻倍（`ServerLibrary/Models/PlayerObject.cs:14742-14748`，常量在 `LibraryCore/Globals.cs:304-313`）。
- 战士技能倍率全部走 `MagicObject.ModifyPowerAdditionner`：Slaying 加算 `+GetPower()`，FlamingSword/DragonRise/BladeStorm/OffensiveBlow 乘算 `power*GetPower()/100`，Thrusting/HalfMoon/DestructiveSurge 仅副格 `power*GetPower()/100`。
- 蓄力技（烈火/龙翔/风暴/攻守双拳）共享 12 秒就绪窗口，相互把对方冷却推后 2 秒。
- 双持：无 OffHand 装备槽；`ItemEffect.DualWield`（武器标记）+ 刺客被动 `DualWeaponSkills` 每刀 `power += power*GetPower()/100`。
- 结算顺序：闪避(Agility) → 技能加值 → 减 AC → 物理抗性 → 7 系元素加值与抗性 → `power<=0` 格挡 → `Attacked`（Evasion/Block 掷骰 → 免疫 → 红毒/转生增伤 → 暴击 → 盾减 → 扣血 → 反伤/天罚）。
- 持续伤害型毒（麻痹/减速/沉默）由物理攻击按 `Random.Next(200) < 对应 Chance` 触发，目标等级 ≥250 时分母 ×10（`ServerLibrary/Models/PlayerObject.cs:15394-15431`）。
- GodotClient 已移植平砍全链路与攻速公式；蓄力技能触发与攻杀自动选择未移植（详见「GodotClient 现状」）。

## 职责概述

物理攻击是近战（含战士全部武器技能、刺客部分攻击技能、怪物普通攻击）的核心管线。职责划分：

- **客户端（Client/）**：决定本次挥砍携带哪个 `MagicType`（攻杀自动触发、开关技能判定、蓄力技就绪优先级），按与服务端相同的公式做本地攻速预测，发 `C.Attack`；技能开关/蓄力通过 `C.MagicToggle` 上行。
- **服务端网络层（SConnection）**：校验方向枚举后直接转交 `PlayerObject.Attack`。
- **PlayerObject**：双时间戳（`ActionTime`/`AttackTime`）节流与动作缓冲；遍历所有 `AttackSkill` 魔法对象装配本次攻击的 `List<MagicType>`；主格命中排延迟动作；广播动画。
- **MagicObject 体系**：每个武器技能以 `AttackCast`（触发条件）+ `SecondaryAttackLocation`（追加格子）+ `ModifyPowerAdditionner`（伤害修正）+ `AttackComplete`（命中后效果）四个钩子切入管线。
- **MapObject/MonsterObject/PlayerObject.Attacked**：防御侧结算（闪避率、格挡率、免疫、暴击、护盾、反伤）与最终扣血。
- **协议层（LibraryCore/Network）**：`C.Attack`/`C.RangeAttack` 上行，`S.ObjectAttack`/`S.ObjectRangeAttack`/`S.ObjectStruck`/`S.ObjectHealthChanged` 下行。

本篇不覆盖纯法术管线 `PlayerObject.MagicAttack`（`ServerLibrary/Models/PlayerObject.cs:15446`，见 combat/magic 文档），但战士中以 `MagicCast→DelayMagic→Player.Attack/MagicAttack` 实现的技能（如 SwiftBlade、SeismicSlam、CrushingWave、TaecheonSword、FireSword）因其伤害走物理 `Attack`/元素 `MagicAttack`，在技能表中一并给出。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| `LibraryCore/Network/ClientPackets.cs` | 142-153 | `C.Attack`（Direction/Action/AttackMagic）与 `C.RangeAttack` 定义 |
| `LibraryCore/Network/ServerPackets.cs` | 180-205 | `S.ObjectAttack`（含 AttackMagic/AttackElement/Slow）与 `S.ObjectRangeAttack` |
| `ServerLibrary/Envir/SConnection.cs` | 501-517 | `Process(C.Attack)`/`Process(C.RangeAttack)`，方向枚举校验后转发 |
| `ServerLibrary/Models/PlayerObject.cs` | 14714-14813 | `Attack(MirDirection, MagicType)`：节流、技能装配、主/副格、广播 |
| `ServerLibrary/Models/PlayerObject.cs` | 15091-15114 | `AttackLocation`：对格子内每个可攻击对象排 `DelayAttack`（300ms，DragonRise 600ms） |
| `ServerLibrary/Models/PlayerObject.cs` | 15116-15203 | `RangeAttack`：飞镖（Shuriken）远程物理，投射延迟=距离×50ms |
| `ServerLibrary/Models/PlayerObject.cs` | 15205-15444 | `Attack(MapObject, types, primary, extra)`：命中判定+伤害计算+落地结算 |
| `ServerLibrary/Models/PlayerObject.cs` | 355-436 | `ProcessAction`：`DelayAttack`/`DelayedAttackDamage` 等延迟动作分发 |
| `ServerLibrary/Models/PlayerObject.cs` | 15678-15955 | `PlayerObject.Attacked`：玩家作为受击方的防御侧结算 |
| `ServerLibrary/Models/MapObject.cs` | 87 | `CanAttack` 基类门槛（活着/时间戳/麻痹/恐惧/DragonRepulse） |
| `ServerLibrary/Models/MapObject.cs` | 1732-1847 | `GetDC/GetMC/GetSC/GetSP/GetAC/GetMR`：Min/Max 随机与 Luck/DefensiveMastery 取极值 |
| `ServerLibrary/Models/MonsterObject.cs` | 1689-1779 | 怪物普攻：400ms 队列 + `Attack(ob, power, element)` 命中/减防计算 |
| `ServerLibrary/Models/MonsterObject.cs` | 2407-2496 | `MonsterObject.Attacked`：怪物受击结算（无 Evasion/Block 掷骰） |
| `ServerLibrary/Models/MagicObject.cs` | 40-48, 156-251 | 攻击技能标志（AttackSkill/IgnoreAccuracy/…）与四个攻击钩子定义 |
| `ServerLibrary/DBModels/UserMagic.cs` | 165-188 | `Cost` 与 `GetPower()`（技能威力随等级的取值公式） |
| `LibraryCore/Globals.cs` | 92-141, 304-313 | MagicRange=10、PhysicalPoisonRate=200、AttackDelay=1500、ASpeedRate=47、AttackTime=600ms 等 |
| `LibraryCore/Stat.cs` | 431-500 | `GetElementValue`/`GetAffinityValue`/`GetResistanceValue` 元素取值映射 |
| `LibraryCore/Functions.cs` | 21-68 | `GetAttackElement`：取最高元素攻击_stat 决定攻击元素 |
| `ServerLibrary/Models/Magics/Warrior/` | 全目录 37 文件 | 战士技能实现（见技能全表） |
| `ServerLibrary/Models/Magics/Assassin/DualWeaponSkills.cs` | 17-34 | 双持武器（ItemEffect.DualWield）伤害加成 |
| `Client/Models/UserObject.cs` | 467-646 | 客户端 attackMagic 选择链与本地攻速预测、`C.Attack` 发送 |
| `Client/Scenes/GameScene.cs` | 3002-3066 | 技能热键：开关技与蓄力技经 `C.MagicToggle` 上行 |
| `Client/Envir/CConnection.cs` | 1955-1956 | 蓄力技被服务端消耗后清除本地 `AttackMagic` |
| `GodotClient/Scripts/CombatController.cs` | 1-559 | Godot 平砍/追击/Shift 攻击/飞镖输入（对应原版 MapControl.ProcessInput） |
| `GodotClient/Scripts/GameScene.cs` | 976-1011, 3000-3057, 1484-1490 | Godot 发包、S.ObjectAttack 处理、攻速公式 |
| `GodotClient/Network/ServerConnection.cs` | 299, 619-627, 1089-1094 | Godot 包收发与 PendingAttacks 缓冲 |

## 核心流程

### 1. 客户端发起（原版 WinForms Client）

玩家输入产生 `MirAction.Attack` 后，客户端在 `UserObject.SetAction` 里先决定本次攻击携带的 `attackMagic`（选择链见下节），再用与服务端完全相同的公式设置本地冷却，然后发包：

```csharp
// Client/Models/UserObject.cs:636-645
case MirAction.Attack:
    attackDelay = Globals.AttackDelay - Stats[Stat.AttackSpeed] * Globals.ASpeedRate;
    attackDelay = Math.Max(800, attackDelay);
    AttackTime = CEnvir.Now + TimeSpan.FromMilliseconds(attackDelay);

    if (BagWeight > Stats[Stat.BagWeight] || (Poison & PoisonType.Neutralize) == PoisonType.Neutralize)
        AttackTime += TimeSpan.FromMilliseconds(attackDelay);

    CEnvir.Enqueue(new C.Attack { Direction = action.Direction, Action = action.Action, AttackMagic = MagicType });
```

客户端 `attackMagic` 选择链（`Client/Models/UserObject.cs:467-609`，顺序即优先级）：

1. `AttackMagic` 字段（刺客 toggle 系 FullBloom/Karma 等 + 手动蓄力）；
2. `CanPowerAttack`（攻杀 Slaying 就绪，需有 `TargetObject`）→ `MagicType.Slaying`（488-499）；
3. `CanThrusting && MapControl.CanEnergyBlast(direction)` → `Thrusting`（501-512）；
4. `CanHalfMoon && (有目标 || CanHalfMoon 格子判定)` → `HalfMoon`（514-526）；
5. `CanDestructiveSurge && ...` → `DestructiveSurge`（529-541）；
6. 刺客系 DragonBlood/FlameSplash/WaningMoon/CalamityOfFullMoon（543-593）；
7. 蓄力系（由 `S.MagicToggle` 置位）：`CanDefensiveBlow`（需面前格有目标）→ `CanBladeStorm` → `CanDragonRise` → `CanFlamingSword` → `CanOffensiveBlow`（需面前格有目标）（595-606）。

开关/蓄力上行（`Client/Scenes/GameScene.cs:3002-3033`）：

```csharp
// Client/Scenes/GameScene.cs:3024-3033
case MagicType.FlamingSword:
case MagicType.DragonRise:
case MagicType.BladeStorm:
case MagicType.DemonicRecovery:
case MagicType.DefensiveBlow:
case MagicType.OffensiveBlow:
    if (CEnvir.Now < magic.NextCast || magic.Cost > User.CurrentMP) return;
    magic.NextCast = CEnvir.Now.AddSeconds(0.5D); //Act as an anti spam
    CEnvir.Enqueue(new C.MagicToggle { Magic = magic.Info.Magic });
    return;
```

`Thrusting/HalfMoon/DestructiveSurge/FlameSplash` 则是 `C.MagicToggle { CanUse = !当前状态 }` 的持久开关（3009-3037）。蓄力被服务端消耗后 `CConnection` 清除本地标志（`Client/Envir/CConnection.cs:1955-1956`）。

### 2. 服务端入口与节流

```csharp
// ServerLibrary/Envir/SConnection.cs:501-508
public void Process(C.Attack p)
{
    if (Stage != GameStage.Game) return;

    if (p.Direction < MirDirection.Up || p.Direction > MirDirection.UpLeft) return;

    Player.Attack(p.Direction, p.AttackMagic);
}
```

`PlayerObject.Attack` 主入口（节流+装配）：

```csharp
// ServerLibrary/Models/PlayerObject.cs:14714-14756（节选）
public void Attack(MirDirection direction, MagicType attackMagic)
{
    if (SEnvir.Now < ActionTime || SEnvir.Now < AttackTime)
    {
        if (!PacketWaiting)
        {
            ActionList.Add(new DelayedAction(ActionTime, ActionType.Attack, direction, attackMagic));
            PacketWaiting = true;
        }
        else
            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });

        return;
    }

    if (!CanAttack)
    {
        Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
        return;
    }

    CombatTime = SEnvir.Now;

    if (Stats[Stat.Comfort] < 15)
        RegenTime = SEnvir.Now + RegenDelay;
    Direction = direction;
    ActionTime = SEnvir.Now + Globals.AttackTime;

    int aspeed = Stats[Stat.AttackSpeed];
    int attackDelay = Globals.AttackDelay - aspeed * Globals.ASpeedRate;
    attackDelay = Math.Max(800, attackDelay);
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

要点：

- **双时间戳**：`ActionTime`（通用动作门，攻击设为 `Globals.AttackTime`=600ms，减速毒只加在这里）与 `AttackTime`（攻击专用门，随 AttackSpeed 缩短）。任一未到即拒绝。
- **缓冲而非丢弃**：首个提前包转为一条 `DelayedAction(ActionType.Attack)` 排到 `ActionTime` 到点重放；`PacketWaiting=true` 期间再来包直接回 `S.UserLocation` 强制纠正（防包泛滥）。
- **`CanAttack`**：基类 `MapObject.cs:87` 要求未死、双时间戳已过、无麻痹/恐惧毒、无 DragonRepulse buff；玩家覆写再要求 **未骑马**（`ServerLibrary/Models/PlayerObject.cs:132`：`public override bool CanAttack => base.CanAttack && Horse == HorseType.None;`）。
- 攻击同时打断回血（Comfort<15 时重置 `RegenTime`）并移除 Transparency/Cloak 隐身（14805-14810）。

### 3. 攻击技能（AttackSkill）装配

```csharp
// ServerLibrary/Models/PlayerObject.cs:14758-14786
MagicType validMagic = MagicType.None;
List<MagicType> magics = new List<MagicType>();

foreach (var key in MagicObjects.OrderedKeys)
{
    var magicObject = MagicObjects[key];

    if (magicObject.AttackSkill)
    {
        if (!magicObject.CanUseMagic())
        {
            continue;
        }

        var response = magicObject.AttackCast(attackMagic);

        if (response.Cast)
            validMagic = magicObject.Type;

        magics.AddRange(response.Magics);
    }
}

if (attackMagic != validMagic)
{
    SEnvir.Log($"[ERROR] {Name} requested Attack Skill '{attackMagic}' but valid magic was '{validMagic}'.");
    Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
    return;
}
```

- 遍历顺序 = `MagicType` 枚举值升序（`ServerLibrary/Models/MagicObject.cs:358-365` `OrderBy(key => this[key].Type)`），因此被动型（Swordsmanship、DualWeaponSkills 等 `AttackCast` 无条件加入自身的）永远先入列，伤害修正按此顺序叠加。
- **服务端权威**：客户端请求的 `attackMagic` 必须恰好等于某个 `response.Cast==true` 的技能（蓄力未就绪/MP 不足/开关关闭时 `AttackCast` 不 Cast），否则按作弊记录日志并纠正位置。
- 攻击元素（仅用于显示与 `S.ObjectAttack.AttackElement`）：`Functions.GetAttackElement(Stats)` 取 7 系元素攻击中最高的非零项（`LibraryCore/Functions.cs:21-68`）；护符槽为 DarkStone 时改用其 affinity 元素（14790-14793）。

### 4. 主格与副格（SecondaryAttackLocation）

```csharp
// ServerLibrary/Models/PlayerObject.cs:14795-14803
bool attackSuccess = AttackLocation(Functions.Move(CurrentLocation, Direction), magics, true);

if (GetMagic(attackMagic, out MagicObject attackMagicObject))
{
    if (attackSuccess)
        attackMagicObject.AttackLocationSuccess(attackDelay);

    attackMagicObject.SecondaryAttackLocation(magics);
}
```

```csharp
// ServerLibrary/Models/PlayerObject.cs:15091-15114
public bool AttackLocation(Point location, List<MagicType> types, bool primary)
{
    Cell cell = CurrentMap.GetCell(location);

    if (cell?.Objects == null) return false;

    bool result = false;

    foreach (MapObject ob in cell.Objects)
    {
        if (!CanAttackTarget(ob)) continue;

        int delay = 300;

        if (types.Contains(MagicType.DragonRise))
            delay = 600;

        ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds(delay), ActionType.DelayAttack, ob, types, primary, 0));

        result = true;
    }

    return result;
}
```

- 只有面前主格 `primary=true`；技能的 `SecondaryAttackLocation` 负责追加副格（Thrusting 第 2 格、HalfMoon/DragonRise 两侧+背后、DestructiveSurge 周身 8 格）。
- `ActionType.DelayAttack` 在 `ProcessAction` 中分发为真正的伤害调用（`ServerLibrary/Models/PlayerObject.cs:398-400`）：`Attack((MapObject)action.Data[0], (List<MagicType>)action.Data[1], (bool)action.Data[2], (int)action.Data[3]);`

最后广播动画（含减速时长与攻击元素）：

```csharp
// ServerLibrary/Models/PlayerObject.cs:14812
Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Slow = slow, AttackMagic = validMagic, AttackElement = element });
```

### 5. 命中判定（Accuracy vs Agility）

```csharp
// ServerLibrary/Models/PlayerObject.cs:15234-15250
int accuracy = Stats[Stat.Accuracy];

int res;

if (types.Contains(MagicType.Karma))
{
    if (GetMagic(MagicType.Resolution, out Resolution resolution))
    {
        accuracy += (accuracy * resolution.Magic.GetPower() / 100);
    }
}

if (!ignoreAccuracy && SEnvir.Random.Next(ob.Stats[Stat.Agility]) > accuracy)
{
    ob.Dodged();
    return;
}
```

- `SEnvir.Random.Next(n)` 返回 `[0, n)`。掷骰值 **大于** 命中才闪避 ⇒ 命中率 ≈ `(accuracy + 1) / agility`。
- **无显式上下限钳制**：`accuracy >= agility - 1` 时必中（掷骰最大值 `agility-1` 不可能 `>` accuracy）；`agility <= 0` 时 `Next(0)==0` 恒不大于非负 accuracy，同样必中。反之 accuracy=0 时命中率仅 `1/agility`，无保底。
- `ignoreAccuracy`：技能覆写 `IgnoreAccuracy => true` 时跳过闪避掷骰（战士 SwiftBlade/SeismicSlam/Shuriken，刺客 FullBloom 等；见 `ServerLibrary/Models/MagicObject.cs:41`）。
- 刺客 Karma（业火）攻击可被 Resolution（洞察）按百分比提高 accuracy。
- 闪避时调用 `ob.Dodged()`（`MapObject.cs:1629` 虚方法，广播闪避动画），本次攻击结束，**不计武器耐久、不吸血流血**。

怪物攻击玩家用同一公式（`ServerLibrary/Models/MonsterObject.cs:1721-1729`）：`SEnvir.Random.Next(ob.Stats[Stat.Agility]) > accuracy`（此处 accuracy 为怪物 Stat.Accuracy）。

### 6. 伤害计算（Attack 核心全文）

```csharp
// ServerLibrary/Models/PlayerObject.cs:15213-15260
int power = GetDC();

bool ignoreAccuracy = false, ignoreDefense = false, hasFlameSplash = false;
bool hasMassacre = false;

int maxLifeSteal = 750;

foreach (MagicType type in types)
{
    if (GetMagic(type, out MagicObject magicObject))
    {
        if (magicObject.IgnoreAccuracy) ignoreAccuracy = true;
        if (magicObject.IgnorePhysicalDefense) ignoreDefense = true;

        if (magicObject.MaxLifeSteal > maxLifeSteal) maxLifeSteal = magicObject.MaxLifeSteal;

        if (magicObject.HasMassacre) hasMassacre = true;
        if (magicObject.HasFlameSplash(primary)) hasFlameSplash = true;
    }
}

int accuracy = Stats[Stat.Accuracy];
// …命中判定（见上节）…

bool hasStone = Equipment[(int)EquipmentSlot.Amulet]?.Info.ItemType == ItemType.DarkStone;

foreach (MagicType type in types)
{
    if (GetMagic(type, out MagicObject magicObject))
    {
        power = magicObject.ModifyPowerAdditionner(primary, power, ob, null, extra);
    }
}
```

减防与抗性（`hasMassacre` 为刺客屠杀系固定伤害时跳过）：

```csharp
// ServerLibrary/Models/PlayerObject.cs:15262-15334（节选）
Element element = Element.None;

if (!hasMassacre)
{
    if (!ignoreDefense)
    {
        var resistance = ob.GetAC();

        if (types.Contains(MagicType.Karma))
        {
            if (GetMagic(MagicType.Resolution, out Resolution resolution))
            {
                resistance -= (resistance * resolution.Magic.GetPower() / 100);
            }
        }

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

    if (hasStone && (!hasFlameSplash || element == Element.Fire))
        DamageDarkStone();

    if (hasFlameSplash)
        element = Element.Fire;
}
else
{
    power = extra;
    // …hasMassacre 时只结算 Element.None 抗性，公式同上 res>0: /10, res<0: /5
}
```

要点：

- **防御**：`power -= ob.GetAC()`——`GetAC()` 同样是 `[MinAC, MaxAC]` 随机，`Stat.DefensiveMastery`（战士被动，`ServerLibrary/Models/Magics/Warrior/DefensiveMastery.cs:16-24` 注入）在此扮演"防御版 Luck"：`Random.Next(10) < defensiveMastery` 概率直接取 MaxAC（`ServerLibrary/Models/MapObject.cs:1820-1837`）。
- **物理抗性**（`Element.None → Stat.PhysicalResistance`，`LibraryCore/Stat.cs:495-496`）：正抗性每点 -10% 当前 power；**负抗性每点 +20%**（`power -= power * res / 5`，res 为负即加伤）。
- **元素加值**：注意循环把 **Fire~Phantom 全部 7 系** 元素攻击 stat 都加进 power（不只是"攻击元素"那一系）；元素抗性正负不对称方向与物理抗性相反：正抗每点 -20%（`value*res*2/10`），负抗每点 +30%（`value*res*3/10`）。DarkStone（暗石护符）额外把 affinity 值累加并承受耐久消耗（`DamageDarkStone()`，`ServerLibrary/Models/PlayerObject.cs:8697`）。
- **格挡判定**：以上全部扣完后 `power <= 0` ⇒ `ob.Blocked()` 并返回（`ServerLibrary/Models/PlayerObject.cs:15336-15340`），表现为"格挡"而非伤害 0。

特殊分支：

```csharp
// ServerLibrary/Models/PlayerObject.cs:15344-15348 —— BladeStorm 半剑气二段
if (types.Contains(MagicType.BladeStorm) && GetMagic(MagicType.BladeStorm, out BladeStorm _))
{
    power /= 2;
    ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds(300), ActionType.DelayedAttackDamage, ob, power, element, true, true, ob.Stats[Stat.MagicShield] == 0, true));
}

// ServerLibrary/Models/PlayerObject.cs:15350-15364 —— Karma 百分比斩击
if (types.Contains(MagicType.Karma) && GetMagic(MagicType.Karma, out Karma karma))
{
    var karmaDamage = ob.CurrentHP * karma.Magic.GetPower() / 100;

    if (ob.Race == ObjectType.Monster)
    {
        if (((MonsterObject)ob).MonsterInfo.IsBoss)
            karmaDamage = karma.Magic.GetPower() * 20;
        else
            karmaDamage /= 4;
    }

    if (karmaDamage > 0)
        damage += ob.Attacked(this, karmaDamage, Element.None, false, true, false);
}

damage += ob.Attacked(this, power, element, true, false, !hasMassacre);
```

### 7. 落地结算（受击方 Attacked）

玩家受击（`ServerLibrary/Models/PlayerObject.cs:15678-15955`，关键顺序）：

```csharp
// ServerLibrary/Models/PlayerObject.cs:15678-15728（门槛与闪避/格挡掷骰）
if (attacker?.Node == null || power == 0 || Dead || attacker.CurrentMap != CurrentMap || !Functions.InRange(attacker.CurrentLocation, CurrentLocation, Config.MaxViewRange) || Stats[Stat.Invincibility] > 0) return 0;

if (element != Element.None)
{
    if (SEnvir.Random.Next(attacker.Race == ObjectType.Player ? 200 : 100) <= Stats[Stat.EvasionChance])// 4 + magic.Level * 2)
    {
        // …Evasion buff 升级 / DisplayMiss…
        return 0;
    }

    if (GetMagic(MagicType.MagicImmunity, out MagicImmunity magicImmunity))
    {
        power -= power * magicImmunity.Magic.GetPower() / 100;

        if (power <= 0) { /* DisplayMiss */ return 0; }
        DisplayResist = true;
        // LevelMagic…
    }
}
else
{
    if (SEnvir.Random.Next(attacker.Race == ObjectType.Player ? 200 : 100) <= Stats[Stat.BlockChance])
    {
        DisplayMiss = true;
        return 0;
    }

    if (GetMagic(MagicType.PhysicalImmunity, out PhysicalImmunity physicalImmunity))
    {
        power -= power * physicalImmunity.Magic.GetPower() / 100;

        if (power <= 0) { /* DisplayMiss */ return 0; }
        DisplayResist = true;
        // LevelMagic…
    }
}
```

- **EvasionChance（元素攻击）/ BlockChance（物理攻击）二次闪避/格挡**：分母 `attacker 是玩家 ? 200 : 100` ⇒ PvP 下这些几率减半。这是独立于攻击方 Agility 掷骰的受击方防御属性。
- 战士 `MagicImmunity`/`PhysicalImmunity`（空类，逻辑内联于此）分别按 `GetPower()%` 减免元素/物理伤害。

继续（增伤与暴击）：

```csharp
// ServerLibrary/Models/PlayerObject.cs:15741-15757
if ((Poison & PoisonType.Red) == PoisonType.Red)
    power = (int)(power * 1.2F);

for (int i = 0; i < attacker.Stats[Stat.Rebirth]; i++)
    power = (int)(power * 1.2F);

if (SEnvir.Random.Next(100) < attacker.Stats[Stat.CriticalChance] && canCrit)
{
    if (!canReflect)
        power = (int)(power * 1.2F);
    else if (attacker.Race == ObjectType.Player)
        power = (int)(power * 1.3F);
    else
        power += power;

    Critical();
}
```

- 红毒（中毒加深）受击 +20%；攻击方每层 Rebirth（转生）再 ×1.2。
- **暴击**：`Random.Next(100) < CriticalChance`；二段伤害（`canReflect==false`，如 BladeStorm 二段）×1.2，PvP 主伤害 ×1.3，怪物/其他 ×2。

护盾与硬直（15785-15849）：`ignoreShield==false` 时 Cloak 隐身减半、MagicShield buff 剩余时间扣 `power*25ms`、`power -= power * Stats[Stat.MagicShield] / 100`；`StruckTime` 距上次 >500ms 且 `canStruck` 时广播 `S.ObjectStruck` 并磨损全身装备（每件 `Random.Next(2)+1` 耐久），`Config.EnableStruck` 时硬直把 `ActionTime` 推到 `StruckTime+300ms`。

扣血与反伤（15888-15934）：

```csharp
// ServerLibrary/Models/PlayerObject.cs:15888-15904, 15916-15930
LastHitter = attacker;

if (!ignoreShield && Buffs.Any(x => x.Type == BuffType.SuperiorMagicShield))
{
    // SuperiorMagicShield buff 吸收量扣减，耗尽移除 buff
}
else
    ChangeHP(-power);
// …
if (canReflect && CanAttackTarget(attacker) && attacker.Race != ObjectType.Player)
{
    attacker.Attacked(this, power * Stats[Stat.ReflectDamage] / 100, Element.None, false);
    // …ReflectDamage 技能升级…
}

if (canReflect && CanAttackTarget(attacker) && SEnvir.Random.Next(100) < Stats[Stat.JudgementOfHeaven] && !(attacker is CastleLord))
{
    int damagePvE = GetMC() / 5 + GetElementPower(ObjectType.Monster, Stat.LightningAttack) * 2;
    int damagePvP = Math.Min(50, GetMC() / 5 + GetElementPower(ObjectType.Monster, Stat.LightningAttack) / 2);

    Broadcast(new S.ObjectEffect { ObjectID = attacker.ObjectID, Effect = Effect.ThunderBolt });
    ActionList.Add(new DelayedAction(SEnvir.Now.AddMilliseconds(300), ActionType.DelayedAttackDamage, attacker, attacker.Race == ObjectType.Player ? damagePvP : damagePvE, Element.Lightning, false, false, true, true));
    // …JudgementOfHeaven（道士天罚）…
}
```

- **反伤（ReflectDamage）只对非玩家攻击者生效**：`power * Stat.ReflectDamage / 100`，且 `canReflect=false`（反射不再反射）。
- 怪物受击方（`ServerLibrary/Models/MonsterObject.cs:2407-2496`）没有 Evasion/Block 掷骰与反伤：红毒 ×1.2、攻击方 Rebirth 每层 ×1.5、MagicShield、暴击 `power += power + (power * attacker.Stats[Stat.CriticalDamage] / 100)`、SuperiorMagicShield 吸收或 `ChangeHP(-power)`，死亡则走掉落。

### 8. 命中后的收尾（攻击方）

```csharp
// ServerLibrary/Models/PlayerObject.cs:15368-15443（节选）
if (damage <= 0) return;

CheckBrown(ob);   // PK 灰名/棕名判定

DamageItem(GridType.Equipment, (int)EquipmentSlot.Weapon, SEnvir.Random.Next(2) + 1);  // 武器耐久 1-2

decimal lifestealAmount = damage * Stats[Stat.LifeSteal] / 100M;

foreach (MagicType type in types)
{
    if (GetMagic(type, out MagicObject magicObject))
    {
        lifestealAmount = magicObject.LifeSteal(primary, lifestealAmount);
    }
}

if (primary || Class == MirClass.Warrior || hasFlameSplash)
    LifeSteal += lifestealAmount;

if (LifeSteal > 1)
{
    int heal = (int)Math.Floor(LifeSteal);
    LifeSteal -= heal;
    ChangeHP(Math.Min(maxLifeSteal, heal));
}

int psnRate = Globals.PhysicalPoisonRate;

if (ob.Level >= 250)
    psnRate = Globals.PhysicalPoisonRate * 10;

if (SEnvir.Random.Next(psnRate) < Stats[Stat.ParalysisChance])
{
    ob.ApplyPoison(new Poison { Owner = this, Type = PoisonType.Paralysis, TickFrequency = TimeSpan.FromSeconds(3), TickCount = 1, });
}

if (ob.Race != ObjectType.Player && SEnvir.Random.Next(psnRate) < Stats[Stat.SlowChance])
{
    ob.ApplyPoison(new Poison { Owner = this, Type = PoisonType.Slow, Value = 20, TickFrequency = TimeSpan.FromSeconds(5), TickCount = 1, });
}

if (SEnvir.Random.Next(psnRate) < Stats[Stat.SilenceChance])
{
    ob.ApplyPoison(new Poison { Owner = this, Type = PoisonType.Silenced, TickFrequency = TimeSpan.FromSeconds(5), TickCount = 1, });
}

foreach (var type in MagicObjects.Keys)
{
    var magicObject = MagicObjects[type];

    if (types.Contains(type))
    {
        magicObject.AttackComplete(ob);   // 命中后效果（OffensiveBlow 推人/DefensiveBlow 减防/SeismicSlam 上毒）
    }

    magicObject.AttackCompletePassive(ob, types);
}
```

- **吸血**：小数 `decimal` 跨挥砍累积（副格伤害只有 `primary || Class==Warrior || hasFlameSplash` 才累积）；单次治疗上限 `maxLifeSteal`（默认 750，技能可覆写，取攻击携带技能的最大值）。
- **物理毒**：麻痹/减速（仅 PvE）/沉默按 `Random.Next(200) < Chance` 触发；目标等级 ≥250 时分母变 2000（高级怪显著降低控制覆盖率）。
- 命中即磨损武器 1-2 点耐久；`AttackComplete` 默认实现为 `Player.LevelMagic(Magic)`（技能熟练度成长，`ServerLibrary/Models/MagicObject.cs:194-197`）。

## 数据结构/协议细节

### C.Attack / C.RangeAttack（上行）

```csharp
// LibraryCore/Network/ClientPackets.cs:142-153
public sealed class Attack : Packet
{
    public MirDirection Direction { get; set; }
    public MirAction Action { get; set; }
    public MagicType AttackMagic { get; set; }
}

public sealed class RangeAttack : Packet
{
    public MirDirection Direction { get; set; }
    public uint Target { get; set; }
}
```

### S.ObjectAttack / S.ObjectRangeAttack（下行广播）

```csharp
// LibraryCore/Network/ServerPackets.cs:180-205
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
```

- 玩家攻击路径（`PlayerObject.cs:14812`）与怪物普攻（`MonsterObject.cs:1692`）都**不填 `TargetID`**（保持 0）；GodotClient 的 `OnObjectAttack` 对 `TargetID != 0` 的受击预测分支实际收不到该字段（受击表现靠独立的 `S.ObjectStruck`）。
- `Slow` 为本次攻击施加给自身的减速时长（客户端用于动画减速表现）；`AttackMagic` 用于客户端选择攻击动画/音效；`AttackElement` 原版客户端用于受击颜色，GodotClient 目前未使用（grep 无 `AttackElement` 引用）。

### Stat 相关取值

```csharp
// LibraryCore/Stat.cs:431-452（GetElementValue）447-500（GetResistanceValue 节选）
case Element.Phantom:
    return this[Stat.PhantomAttack];
// …
case Element.None:
    return this[Stat.PhysicalResistance];
```

元素攻击 stat（FireAttack…PhantomAttack）与元素抗性（FireResistance…PhantomResistance + PhysicalResistance）一一映射；affinity（FireAffinity…）为 DarkStone 附加伤害来源。

### 攻速与时间常量

```csharp
// LibraryCore/Globals.cs:304-313
public const int AttackDelay = 1500,
                 ASpeedRate = 47,
                 ProjectileSpeed = 48;

public static TimeSpan TurnTime = TimeSpan.FromMilliseconds(300),
                       HarvestTime = TimeSpan.FromMilliseconds(600),
                       MoveTime = TimeSpan.FromMilliseconds(600),
                       AttackTime = TimeSpan.FromMilliseconds(600),
                       CastTime = TimeSpan.FromMilliseconds(600),
                       MagicDelay = TimeSpan.FromMilliseconds(2000);
```

```csharp
// LibraryCore/Globals.cs:139-141
public static int
    PhysicalPoisonRate = 200,
    MagicalPoisonRate = 100;
```

- 攻击间隔下限 800ms ⇒ AttackSpeed 理论有效上限 `(1500-800)/47 ≈ 14`（再堆只吃超重惩罚前的地板）。
- 减速毒（PoisonType.Slow）每点 Value 增加 100ms，只加 `ActionTime`（影响走位/转身），不加 `AttackTime`；但 `S.ObjectAttack.Slow` 会广播。
- 飞镖 `RangeAttack`（`PlayerObject.cs:15116-15203`）：仅当武器 `Info.Shape == Globals.ShurikenLibraryWeaponShape(33)`；投射延迟 `Math.Max(100, Math.Min(750, Functions.Distance(...)*50))`；命中走 `DelayAttack`，`extra=50`；每次投掷磨损武器。

### GetDC 与 Luck/诅咒

```csharp
// ServerLibrary/Models/MapObject.cs:1732-1753
public int GetDC()
{
    int min = Stats[Stat.MinDC];
    int max = Stats[Stat.MaxDC];
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

    return SEnvir.Random.Next(min, max + 1);
}
```

- `GetMC`（1754-1775）/`GetSC`（1776-1797）/`GetSP`（1798-1819）公式完全相同，仅 Min/Max stat 不同；`GetAC`（1820-1837）把 luck 换成 `Stat.DefensiveMastery` 且取 max 时置 `DisplayResist = true`；`GetMR`（1838-1847）无 luck。
- **幸运**：`luck >= 10` 必取 MaxDC；否则 `luck/10` 概率取 MaxDC。**诅咒**（负 Luck）：`|luck|/10` 概率取 MinDC（`luck < -Random.Next(10)`）。
- 武器幸运来源：祝福油 `UseOilOfBenediction`（`ServerLibrary/Models/PlayerObject.cs:8802-8841`）——对武器 `StatSource.Enhancement` 的 Luck 累计值判定：`luck >= Config.MaxLuck(7)` 直接失败；`luck > Config.MaxCurse(-10)` 时 `Random.Next(Config.CurseRate=20) == 0` 概率 **-1 诅咒**；否则 `luck <= 0 || Random.Next(luck * Config.LuckRate=10) == 0` 时 **+1 幸运**（幸运越高越难点，负幸运必成功）。配置见 `ServerLibrary/Envir/Config.cs:136-139`。

### 技能威力 GetPower（倍率/加值的公共来源）

```csharp
// ServerLibrary/DBModels/UserMagic.cs:179-188
public int GetPower()
{
    int min = Info.MinBasePower + Level * Info.MinLevelPower / 3;
    int max = Info.MaxBasePower + Level * Info.MaxLevelPower / 3;

    if (min < 0) min = 0;
    if (min >= max) return min;

    return SEnvir.Random.Next(min, max + 1);
}
```

`Cost`（同文件 165-168）：`Info.BaseCost + Level * Info.LevelCost / 3`。倍率型技能 `power * Magic.GetPower() / 100` 中 GetPower 即由此产生（Integer 除法截断）。

## 战士武器技能全表（ServerLibrary/Models/Magics/Warrior/ 全目录 37 文件）

按管线角色分组。`AttackSkill=true` 的技能参与 `AttackCast` 装配；其余为主动法术（`Magic()` 入口）或纯被动。伤害公式一律照抄。

### A. 平砍管线技能（AttackSkill，随 C.Attack 触发）

| 技能 | 文件:行 | 触发方式 | 伤害/效果公式（照抄） |
|---|---|---|---|
| **Slaying 攻杀剑法** | Slaying.cs:20-58 | 服务器随机 1/5 就绪（`SEnvir.Random.Next(5) == 0` 置 `CanPowerAttack`），客户端请求时消耗 | `power += Magic.GetPower();`（41-46）；被动 `GetPassiveStats`：`Accuracy/MinDC/MaxDC 各 + Magic.Level*2`（48-58） |
| **Swordsmanship 基本剑术** | Swordsmanship.cs:17-34 | 每次 `AttackCast` 无条件加入 magics（不耗蓝、无消耗），故每次平砍都吃被动 | 被动 `[Stat.Accuracy] = Magic.GetPower()`（26-34）——命中核心来源 |
| **Thrusting 刺杀剑法** | Thrusting.cs:31-63 | 持久开关（`Character.CanThrusting`），MP 够时 Cast 并扣蓝 | 副格穿透第 2 格：`Player.AttackLocation(Functions.Move(CurrentLocation, Player.Direction, 2), magics, false)`（52-55）；副格伤害 `if (!primary) power = power * Magic.GetPower() / 100;`（57-63） |
| **HalfMoon 半月剑法** | HalfMoon.cs:32-66 | 持久开关（`Character.CanHalfMoon`），扣蓝 | 副格三向（自身为基准 dir±1、dir+2 即背后）：`ShiftDirection(Direction, -1)/(+1)/(+2)`（53-58）；副格 `if (!primary) power = power * Magic.GetPower() / 100;`（60-66） |
| **DestructiveSurge 破空斩（旋风）** | DestructiveSurge.cs:33-96 | 持久开关（`Character.CanDestructiveSurge`），扣蓝；`Magic.Level >= 3` 且有 Augment 时追加 aug 入列 | 副格周身 8 格：`for (int i = 1; i < 8; i++) AttackLocation(Functions.Move(CurrentLocation, Functions.ShiftDirection(Direction, i)), magics, false);`（64-67）；Augment 再加外圈 8 格（距离 2，69-76）；副格 `power * Magic.GetPower() / 100`（79-85）；吸血封顶：副格 `lifestealAmount = Math.Min(lifestealAmount, 750 - DestructiveSurgeLifeSteal);` 累计（87-96） |
| **AugmentDestructiveSurge** | AugmentDestructiveSurge.cs:18-23 | 仅由 DestructiveSurge 附带入列 | `power += Magic.GetPower();`（加算威力） |
| **FlamingSword 烈火剑法** | FlamingSword.cs:33-85 | 蓄力：`C.MagicToggle` → `Toggle()`，12 秒就绪窗（`FlamingSwordTime = SEnvir.Now.AddSeconds(12)`，46-48），期间攻击消耗 | 主格倍率 `power = power * Magic.GetPower() / 100;`（80-85）；互斥：蓄力把 DragonRise/BladeStorm 冷却推后 2 秒（51-61） |
| **DragonRise 龙翔/翔空剑法** | DragonRise.cs:34-93 | 蓄力 12 秒窗（47-49），互斥同上 | 倍率 `power = power * Magic.GetPower() / 100;`（88-93）；副格三向同 HalfMoon（81-86）；**含 DragonRise 的 AttackLocation 命中延迟 600ms**（`PlayerObject.cs:15105-15106`） |
| **BladeStorm 剑气风暴** | BladeStorm.cs:34-84 | 蓄力 12 秒窗（47-49），互斥同上 | 倍率 `power = power * Magic.GetPower() / 100;`（79-84）；管线下半场强制 `power /= 2` 并排 300ms 二段 `DelayedAttackDamage`（`PlayerObject.cs:15344-15348`），二段 `canReflect=true, canCrit=true` 且 `canStruck = ob.Stats[Stat.MagicShield]==0` |
| **OffensiveBlow 攻击之锤（蓄力拳）** | OffensiveBlow.cs:33-97 | 蓄力 12 秒窗（46-48） | 倍率 `power = power * Magic.GetPower() / 100;`（68-73）；命中后 `AttackComplete`：`if (target != null && TryPush(target, Magic.Level + 3))` 推 `Magic.Level+3` 格并上 Paralysis+Silenced 各 3 秒毒（75-97） |
| **DefensiveBlow 防御之锤（蓄力拳）** | DefensiveBlow.cs:33-79 | 蓄力 12 秒窗（46-49） | **无伤害修正**；命中后给目标 debuff 10 秒：`[Stat.MagicDefencePercent] = Magic.GetPower() * -1, [Stat.PhysicalDefencePercent] = Magic.GetPower() * -1`（68-79） |
| **Shuriken 飞镖** | Shuriken.cs:6-20 | `RangeAttack` 管线（武器 Shape==33），非平砍 | `IgnoreAccuracy => true`（11）；伤害即 `Attack(extra=50)` 主流程（`PlayerObject.cs:15200`） |

蓄力技共同模式（以 FlamingSword 为例）：

```csharp
// ServerLibrary/Models/Magics/Warrior/FlamingSword.cs:22-31（超时回收）
public override void Process()
{
    if (CanFlamingSword && SEnvir.Now >= FlamingSwordTime)
    {
        CanFlamingSword = false;
        Player.Enqueue(new S.MagicToggle { Magic = Type, CanUse = CanFlamingSword });

        Player.Connection.ReceiveChatWithObservers(con => string.Format(con.Language.ChargeExpire, Magic.Info.Name), MessageType.System);
    }
}
```

### B. 主动法术但走物理/元素伤害管线的技能

| 技能 | 文件:行 | 落点方式 | 伤害/效果公式（照抄） |
|---|---|---|---|
| **SwiftBlade 疾风刃** | SwiftBlade.cs:23-80 | 目标点 3×3（`GetCells(location, 0, 3)`），900ms 后逐格 `Player.Attack(cell.Objects[i], {Type}, true, 0)`（38-61） | `IgnoreAccuracy => true`（15）；`power = power * Magic.GetPower() / 100; if (ob.Race == ObjectType.Player) power /= 2;`（72-80）；吸血封顶 2000/次施法：`lifestealAmount = Math.Min(lifestealAmount, 2000 - SwiftBladeLifeSteal);`（64-70） |
| **SeismicSlam 震地** | SeismicSlam.cs:22-96 | 面朝方向 3 格为中心 3×3（29），600ms 后逐格 `Player.Attack(..., true, 0)` | `IgnoreAccuracy => true`（14）；倍率+PvP 减半同 SwiftBlade（59-67）；`AttackComplete` 必上 Paralysis 3s + WraithGrip 1.5s + Silenced 5s 三毒（69-96） |
| **CrushingWave 破空波** | CrushingWave.cs:19-83 | 直线 12 格，`400+i*60ms` 主格 + 两侧 `200+i*60ms`（非 primary） | `if (!primary) power = power * Magic.GetPower() / 100; if (ob.Race == ObjectType.Player) power /= 2;`（74-83） |
| **HundredFist 百拳/百裂拳** | HundredFist.cs:21-163 | 冲到目标背后，把目标推 `travelled*2` 格（102-110），撞墙/撞人时 `MagicAttack({Type}, ob, extra: pushed)`（129） | `power += Magic.GetPower() + (Player.GetDC() * extra);`（140-145，extra=实际推动格数）；推动判定 `SEnvir.Random.Next(Globals.MagicMaxLevel + 12) >= 6 + magic.Level * 3 + Player.Level - ob.Level` 失败（147-158） |
| **TaecheonSword 太天剑（韩服名音译，中文定名未知 [INFERENCE]）** | TaecheonSword.cs:20-65 | 自身为中心 5×5（`GetCells(CurrentLocation, 0, 2)`），1500ms 后逐格 `MagicAttack({Type}, ob, extra: distanceFromCentre)` | 元素 `Element.Fire`（13）；`var multiplier = Math.Max(0, 4 - extra); power += Magic.GetPower() + (Player.GetDC() * multiplier);`（58-65——离中心越近倍率越高，中心 ×4） |
| **FireSword 火剑** | FireSword.cs:21-69 | 自身 5×5 按"同心环逆时针"排序逐格 1300+100*count ms（32-43），逐格 `MagicAttack({Type}, ob)` | 元素 `Element.Fire`（14）；`power += Magic.GetPower() + Player.GetDC();`（64-69） |
| **ElementalSwords 元素剑** | ElementalSwords.cs:25-107 | 头顶 5 把剑 5 秒；`Process()` 每 5 秒锁定 5 格内正在攻击自己的目标随机一只飞剑（46-86），`MagicAttack({Type}, target)` | `power += Magic.GetPower();`（102-107）；击杀时 1/4 回蓝 `(Stats[Stat.Mana] - CurrentMP) * (10 + Magic.Level * 10) / 100`（94-99） |
| **ShoulderDash 野蛮冲撞** | ShoulderDash.cs:22-268 | `distance = Magic.GetPower();` 每格 300ms 递归 `MagicComplete`，推不开就停（55-190） | 本体无伤害；推动判定 `SEnvir.Random.Next(Globals.MagicMaxLevel + 12) >= 6 + magic.Level * 3 + Player.Level - ob.Level` 失败（192-203）；`Magic.Level >= 3` 可连推叠怪（126-183）；Augment（Assault）命中推到的目标上 Paralysis `300+GetPower()ms` + Silenced `300+GetPower()*2ms`（210-246） |

### C. 控制/位移类

| 技能 | 文件:行 | 效果（照抄关键判定） |
|---|---|---|
| **Beckon 擒拿手（拉人）** | Beckon.cs:40-107 | 拉到面前：玩家 `SEnvir.Random.Next(Globals.MagicMaxLevel + 6) > 4 + Magic.Level` 失败（58）；怪 `Next(MagicMaxLevel + 5) > 2 + Magic.Level * 2` 失败（67）；PvP 后 30 秒内冷却 ×10（100-104） |
| **MassBeckon 群体擒拿** | MassBeckon.cs:35-60 | 9 格内怪随机传送到自身 3 格内并上 Paralysis `1+Level` 秒；`Next(MagicMaxLevel + 5) > 2 + Magic.Level * 2` 失败（46） |
| **Interchange 移形换位** | Interchange.cs:42-95 | 与目标互换位置；`Next(MagicMaxLevel + 5) > 2 + Magic.Level * 2` 失败（64）；PvP 冷却 ×10（88-92） |
| **Fetter 束缚/定身** | Fetter.cs:38-68 | 自身 5×5 上 Slow 毒：`Value = (3 + Magic.Level) * 2`，持续 `5 + Magic.Level * 3` 秒；`ob.Level > Player.Level + 15` 无效（53） |

### D. 自身增益（buff 系）

| 技能 | 文件:行 | 效果（照抄） |
|---|---|---|
| **Might 蛮力** | Might.cs:37-58 | 互斥移除 Defiance；`duration = TimeSpan.FromSeconds(60 + Magic.Level * 30); amount = 5 + Magic.Level * 5;` buff：`DCPercent=+amount, MagicDefencePercent=-amount, PhysicalDefencePercent=-amount` |
| **Defiance 铁布衫** | Defiance.cs:36-69 | 互斥移除 Might；`duration = 60+Level*30s`；buff `PhysicalDefencePercent/MagicDefencePercent = 5 + Magic.Level * 5`；`DCPercent = -offence`，`offence` 默认 20，有 Augment 时 `Math.Max(0, 20 - Magic.Level * 5)` 且时长 `+10+Level*10s` |
| **Endurance 金钟罩（免推）** | Endurance.cs:35-40 | `BuffAdd(BuffType.Endurance, TimeSpan.FromSeconds(10 + Magic.Level * 5), null, ...)`——所有 `CanPushTarget` 判定见此 buff 即拒推 |
| **ReflectDamage 反伤** | ReflectDamage.cs:35-58 | `damage = 5 + Magic.Level * 3; duration = 15+Level*10s`，buff `Stat.ReflectDamage = damage`；Augment：`damage += augmentReflectDamage.GetPower(); duration += 5+aug.Level*5s` |
| **Invincibility 无敌** | Invincibility.cs:36-46 | buff `Stat.Invincibility = 1` 持续 `5 + Magic.Level` 秒（`Attacked` 入口直接 return 0） |

### E. 纯被动/空壳

| 技能 | 文件:行 | 说明 |
|---|---|---|
| **DefensiveMastery 防御专精** | DefensiveMastery.cs:16-24 | `GetPassiveStats(): [Stat.DefensiveMastery] = Magic.GetPower()`——参与 `GetAC()` 的"取 MaxAC 概率"（见 Luck 节） |
| **MagicImmunity / PhysicalImmunity** | MagicImmunity.cs / PhysicalImmunity.cs（各 16 行） | 空类；实际减免内联在 `PlayerObject.Attacked`（15693/15715）按 `GetPower()%` |
| **PotionMastery / AdvancedPotionMastery** | PotionMastery.cs / AdvancedPotionMastery.cs（各 16 行） | 空类（药水相关，注释 Custom Skill） |
| **Assault 突袭** | Assault.cs:6-16 | 空类，仅作 ShoulderDash 的 Augment 冷却载体（`ShoulderDash.ApplyAugment`） |
| **AugmentDefiance / AugmentReflectDamage** | 各 16 行 | 空类，效果内联在 Defiance/ReflectDamage 的 `MagicComplete` |

### F. 跨职业常一起出现在物理管线里的技能（对照）

- 刺客 **FlameSplash（火焰飞溅）**：`HasFlameSplash(primary)` 为真时，元素循环只结算 Fire 一系并把 element 强制为 Fire（`PlayerObject.cs:15295, 15315-15319`）。
- 刺客 **Massacre（屠杀）**：`HasMassacre => true` 时 `power = extra`（固定伤害），跳过 AC 减免与元素加值，仅结算物理抗性（`PlayerObject.cs:15321-15334`），且主 `Attacked` 调用 `canCrit: !hasMassacre`。
- 刺客 **Karma（业火）**：命中后额外 `CurrentHP * GetPower()/100` 斩击（BOSS `GetPower()*20`，普通怪 `/4`），并可被 Resolution 强化命中/穿防（15238-15276）。

## 双持（DualWield）

**本引擎没有 OffHand 副手武器槽。** `EquipmentSlot` 枚举只有 `Weapon = 0` 与 `Shield = 15`（盾）等（`LibraryCore/Enum.cs:89-114`），全库 grep 无 `OffHand`。双持实现为武器标记 + 被动：

```csharp
// LibraryCore/Enum.cs:1855
DualWield = 100,   // ItemEffect 枚举值
```

```csharp
// ServerLibrary/Models/Magics/Assassin/DualWeaponSkills.cs:17-34（刺客 448 号技能）
public override AttackCast AttackCast(MagicType attackType)
{
    var response = new AttackCast();

    if (Player.Equipment[(int)EquipmentSlot.Weapon]?.Info.ItemEffect == ItemEffect.DualWield)
    {
        response.Magics.Add(Type);
    }

    return response;
}

public override int ModifyPowerAdditionner(bool primary, int power, MapObject ob, Stats stats = null, int extra = 0)
{
    power += power * Magic.GetPower() / 100;

    return power;
}
```

即：只要武器 `Info.ItemEffect == ItemEffect.DualWield` 且学了该技能，**每一次平砍**（无 MP 消耗、无需开关）自动附加 `power * GetPower() / 100` 的乘算加值。战士职业技能表中无双持技能。

## 攻击速度（AttackSpeed）与延迟节流汇总

1. **入口闸门**（`PlayerObject.cs:14716`）：`SEnvir.Now < ActionTime || SEnvir.Now < AttackTime` 时缓冲/纠正，不执行。
2. **ActionTime**：攻击时设为 `Now + 600ms`（`Globals.AttackTime`）；减速毒（Slow）额外加 `Value*100ms`；被击硬直（`Config.EnableStruck`）会推到 `StruckTime+300ms`（`PlayerObject.cs:15823`）。
3. **AttackTime**：`Now + max(800, 1500 - AttackSpeed*47)ms`；**超重（BagWeight > Stat.BagWeight）或 Neutralize 毒时再加一整段 attackDelay（等效 ×2）**（14747-14748）。
4. 客户端镜像同一公式做本地预测（`Client/Models/UserObject.cs:637-642`），服务端时间戳为最终权威（服务端拒绝时回 `S.UserLocation` 强制同步）。
5. 命中延迟：`AttackLocation` 排 300ms（DragonRise 600ms）；飞镖按距离 `50ms/格`（100-750ms 钳制）；BladeStorm 二段 300ms。
6. 攻击间隔地板 800ms ⇒ `AttackSpeed` 数值的有效区间约 0-14。

## 反伤/格挡/闪避结算顺序（总表）

攻击方视角（`PlayerObject.Attack` → 受击方 `Attacked`）：

1. 目标有效性/可见性（`CanAttackTarget`，`PlayerObject.cs:15957`：死/隐身/Guard/攻城规则/PK 模式过滤）。
2. **闪避掷骰**：`Random.Next(目标Agility) > 攻击方Accuracy`（15246）→ `Dodged()` 终结。
3. 技能伤害加值（`ModifyPowerAdditionner` 按枚举序叠加，15254-15260）。
4. **减防**：`power -= ob.GetAC()`（GetAC 内含 DefensiveMastery 概率取 Max，15268/15278）。
5. **物理抗性**：`res>0: power -= power*res/10；res<0: power -= power*res/5`（15285-15288）。
6. **7 系元素加值与元素抗性**（15293-15313；正抗 -20%/点，负抗 +30%/点）。
7. **格挡判定**：`power <= 0` → `Blocked()` 终结（15336-15340）。
8. 特殊分支：BladeStorm 二段（15344）/ Karma 斩击（15350）。
9. **受击方 Attacked**（PvP `PlayerObject.cs:15678`）：
   1. 门槛（死亡/距离/`Stat.Invincibility`）直接免伤；
   2. **EvasionChance（元素）/BlockChance（物理）掷骰**（分母 PvP 200 / PvE 100）→ `DisplayMiss` 终结；
   3. MagicImmunity/PhysicalImmunity 百分比减免（`power<=0` 终结）；
   4. 红毒 ×1.2、攻击方 Rebirth 每层 ×1.2（PvE 受击为 ×1.5，`MonsterObject.cs:2445-2446`）；
   5. **暴击**掷骰 `Random.Next(100) < CriticalChance`（PvP 主 ×1.3 / 二段 ×1.2 / 怪物攻击 ×2；PvE 怪受击 `power += power + power*CriticalDamage/100`）；
   6. FrostBite 吸收、（`!ignoreShield`）Cloak 减半 + MagicShield 减免；
   7. Struck 广播 + 全身装备耐久磨损（>500ms 节流）；
   8. SuperiorMagicShield 吸收或 **`ChangeHP(-power)` 扣血**；
   9. **反伤**（仅受击者为玩家且攻击者非玩家）：`attacker.Attacked(this, power*ReflectDamage/100, Element.None, false)`（15916-15918）；
   10. JudgementOfHeaven 天罚反击闪电（15924-15930）。
10. 回到攻击方：`damage>0` 才算命中成功 → CheckBrown、武器耐久、吸血累积、物理毒触发、`AttackComplete`（15368-15443）。

注意：**没有独立的"招架/挡格动作"机制**——"格挡"只有两种表现：攻击方算完 power≤0 的 `Blocked()`，和受击方 `BlockChance` 掷骰的免伤（也走 `DisplayMiss`/Block 飘字）。

## GodotClient 现状

基于对 `GodotClient/` 的实际搜索（grep/glob）逐功能核对：

| 功能 | 状态 | GodotClient 依据 |
|---|---|---|
| 平砍输入→`C.Attack` 发包（点选目标自动攻击、Shift 原地攻击、追击接近） | 已移植 | `GodotClient/Scripts/CombatController.cs:18-26`（职责注释对应原版 `MapControl.ProcessInput`）、`:402`（TryAttack）；发包回调 `GodotClient/Scripts/GameScene.cs:976-995`（`_net.Connection.Enqueue(new C.Attack {...})` 在 994 行） |
| 攻速节流公式（`max(800, AttackDelay-AS*47)`、超重/Neutralize ×2） | 已移植 | `GodotClient/Scripts/GameScene.cs:1484-1490`（`ComputeAttackIntervalMs`，注释直引原版 UserObject.cs:638-653）；接线于 1004-1006、自动攻击 9195/9384 |
| 骑马禁止攻击门控 | 已移植 | `GodotClient/Scripts/GameScene.cs:983`（`if (_playerHorse != HorseType.None) return;`） |
| 飞镖 `C.RangeAttack`（Shape==33 判定、超距提示、冷却清目标） | 已移植 | `GodotClient/Scripts/CombatController.cs:109-119`（ShurikenClickResult 状态机）、`GameScene.cs:1007-1011`、`GodotClient/Network/ServerConnection.cs:1094`（SendRangeAttack） |
| `S.ObjectAttack` 接收（缓冲队列+动画+攻击特效+受击预测） | 已移植 | `GodotClient/Network/ServerConnection.cs:299`（PendingAttacks）、`:619-623`（Process）；`GodotClient/Scripts/GameScene.cs:3000-3057`（OnObjectAttack：PlayCombat + `MagicEffectTable.GetAttack`）；缓冲排空 7514-7518 |
| `S.ObjectStruck` 受击动画 | 已移植 | `GodotClient/Scripts/GameScene.cs:7560-7564`（PendingStrucks → OnObjectStruck）、订阅 1298 |
| 伤害飘字（数值/MISS/BLOCK/CRITICAL/RESIST） | 已移植 | `GodotClient/Scripts/DamagePopupNode.cs:6-13`（`Setup(value, miss, block, critical, resist)`）；由 `S.ObjectHealthChanged` 驱动 `GameScene.cs:4065-4103` |
| 开关技能（Thrusting/HalfMoon/DestructiveSurge/FlameSplash）`C.MagicToggle` | 已移植 | `GodotClient/Scripts/GameScene.cs:9683-9693`；`ServerConnection.cs:1089`（SendMagicToggle） |
| `S.MagicToggle` 接收 | 部分移植 | `GodotClient/Scripts/GameScene.cs:7675-7681`（OnMagicToggle 仅聊天提示+UI 刷新，`ServerConnection.cs:1115-1124`）——**不维护** `CanPowerAttack`/`CanFlamingSword` 等就绪标志（grep 全库无 CanPowerAttack/CanFlamingSword/CanBladeStorm） |
| 蓄力技能触发（FlamingSword/DragonRise/BladeStorm/DefensiveBlow/OffensiveBlow 的 `C.MagicToggle` 蓄力，原版 `Client/Scenes/GameScene.cs:3024-3033`） | 未移植 | Godot 热键分支 `GameScene.cs:9676-9703` 仅有 4 个开关技 case + 刺客 toggle case，无蓄力技分支 |
| 攻杀（Slaying）服务器就绪后客户端自动带 MagicType（原版 `Client/Models/UserObject.cs:488-499`） | 未移植 | Godot 攻击时 `_attackMagic` 仅刺客 toggle 会设置（`GameScene.cs:125, 990, 9701`），无 Slaying 选择链 |
| 平砍 attackMagic 自动选择链（Thrusting 的 CanEnergyBlast/格判定、HalfMoon/DestructiveSurge 周边目标判定、蓄力技就绪优先级，原版 `Client/Models/UserObject.cs:501-606`） | 未移植 | Godot 平砍固定 `magic != MagicType.None ? magic : _attackMagic`（`GameScene.cs:990`），无周边格子判定逻辑 |
| `S.ObjectAttack.AttackElement` 受击颜色 | 未移植 | grep `AttackElement` 在 GodotClient/ 无匹配 |
| 战士技能表现层资源（音效/特效表） | 已移植 | `GodotClient/Scripts/SoundCatalog.cs:101-251`（Slaying/HalfMoon/FlamingSword/DragonRise/BladeStorm/TaecheonSword/FireSword/SeismicSlam…wav 映射）；`GodotClient/Scripts/MagicEffectTable.cs:237+`（MagicType→特效帧）；攻击动画 `PlayerRenderer.cs:300/314/349`（`Functions.GetAttackAnimation`） |

## 移植注意事项

1. **命中/格挡/暴击全部服务端权威**：客户端从不算伤害。Godot 的飘字数据源是 `S.ObjectHealthChanged`（含 Miss/Block/Critical/Resist 位）而非本地预测，移植时不要在客户端复刻 `Random.Next(Agility)`。
2. **攻速公式的三个细节**：地板 800ms、超重/Neutralize 是"再加一整段"（×2）而非 +固定值、减速毒只影响 `ActionTime`（移动/转身）不影响 `AttackTime`。Godot 已按 1:1 复刻（`ComputeAttackIntervalMs`），改动需同步 `Client/Models/UserObject.cs` 与 `ServerLibrary/Models/PlayerObject.cs` 三处。
3. **服务端有 `PacketWaiting` 单缓冲 + `S.UserLocation` 纠正**机制：过快的攻击包第二个开始会被强制拉回坐标。Godot 客户端没有也不需要该队列，但调试"攻击丢失"时要意识到服务端可能把包缓冲到 `ActionTime` 才执行（命中延迟 300/600ms 也来自这里）。
4. **元素伤害是 7 系全加**而非只加"攻击元素"那一系；`S.ObjectAttack.AttackElement` 只用于表现。负抗性放大系数（物理 /5、元素 /3×10）与正抗（/10、/2×10）不对称，数值对齐时别合并成单一百分比。
5. **蓄力技互斥**：FlamingSword/DragonRise/BladeStorm 两两把对方冷却推 2000ms，且 12 秒就绪窗由 `Process()` 轮询超时回收并发 `S.MagicToggle{CanUse=false}`——移植 Godot 蓄力触发时，还要消费这个"超时回收"包，否则 UI 会永久显示可用。
6. **Slaying 是服务器随机就绪**（1/5 每刀，`S.MagicToggle` 通知），客户端只负责在就绪且有目标时把它放进 `C.Attack.AttackMagic`；Godot 目前既不维护就绪状态也不自动携带，补齐需在 `OnMagicToggle` 里按 MagicType 存布尔。
7. **双持无副手槽**：Godot 装备 UI 不要发明 OffHand；双持=武器 `ItemEffect.DualWield` 标记，加成走 DualWeaponSkills 被动。
8. **`Stat.Luck` 是全局 stat**（武器 Enhancement Luck 直接加进 `Stats[Stat.Luck]`，见 UseOilOfBenediction），同时影响 DC/MC/SC/SP 取值与玩家 `GetLotusMana/GetElementPower`；诅咒为负值，`|luck|/10` 概率取下限。
9. **吸血是小数跨刀累积**（`decimal LifeSteal`），单次治疗钳制 `maxLifeSteal`（默认 750，SwiftBlade 场景 2000，DestructiveSurge 副格 750/挥）；且只有主格/战士/FlameSplash 伤害参与累积——移植 HUD 时不要按"每刀立即回血"理解。
10. **怪物侧公式与玩家侧不同**：`MonsterObject.Attack` 自己减 `GetAC/GetMR`，`MonsterObject.Attacked` 无 Evasion/Block 掷骰、Rebirth 增伤系数 1.5（玩家受击 1.2）、暴击带 `CriticalDamage` 项、无反伤；不要把两条管线混写。
11. **`ob.Level >= 250` 的毒分母 ×10**（PhysicalPoisonRate 200→2000）是高级地图控制免疫的关键参数，移植数值表时别漏。
12. **`S.ObjectAttack.TargetID` 服务端从不填充**（玩家/怪物路径均不设置），Godot `OnObjectAttack` 里的 TargetID 受击分支当前是死代码，受击表现应以 `S.ObjectStruck`/`S.ObjectHealthChanged` 为准。
