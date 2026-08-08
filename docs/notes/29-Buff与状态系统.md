# 29 Buff 与状态系统:BuffType 64 全清单、持续法术与特效映射、Poison 链路

> 调研对象:`~/development/Zircon`(客户端 `Client/` + 服务端 `ServerLibrary/` + 公共库 `LibraryCore/`)。全部行号为 2026-08-07 实测,格式 `文件:行号`。
> 本文覆盖:① BuffType 64 项全清单与 BuffInfo 数据模型、BuffAdd/ProcessBuff/BuffRemove 生命周期(§1-§2);② 持续法术 SpellObject 与 CreateMagicEffect 特效映射全表(§3-§4);③ PoisonType 16 种 flag、ApplyPoison/ProcessPoison 结算与中毒外观三通道、协议包(§5-§6)。
> 与文档 28 衔接:技能挂 Buff 走 `BuffAdd`(`MapObject.cs:1402`),持续法术由技能 MagicCast 里 `Spawn` SpellObject(如 FireWall.cs/Tempest.cs/IceAura.cs),毒由 `ApplyPoison`(`MapObject.cs:1641`)。

---

## 1. BuffType 枚举:64 项全清单(LibraryCore/Enum.cs:231-316)

`public enum BuffType`(`Enum.cs:231`)按 5 个区块排列:None(0)+ 通用(1-22)+ 战士(100-107)+ 法师(200-206)+ 道士(300-311)+ 刺客(400-412)+ 特殊(500)。**枚举本身无 `[Description]`,仅区块注释,客户端显示名与图标走 ClientBuffInfo 数据(§2.4)**。实测全表:

| 值 | 枚举名 | 归属 | 说明(源码用途) |
|---|---|---|---|
| 0 | None | 通用 | 空位 |
| 1 | Server | 通用 | 服务端标志(不可移除类) |
| 2 | HuntGold | 通用 | 狩猎币加成(ProcessBuff 557 每 Tick 结算) |
| 3 | Observable | 通用 | 被观察标志 |
| 4 | Brown | 通用 | 棕色名(惩罚) |
| 5 | PKPoint | 通用 | 红名 PK 值(ProcessBuff 536) |
| 6 | PvPCurse | 通用 | PvP 诅咒 |
| 7 | Redemption | 通用 | 洗红名 |
| 8 | Companion | 通用 | 同伴骑乘(ProcessBuff 428 控制上下马) |
| 9 | Castle | 通用 | 城堡占领 |
| 10 | ItemBuff | 通用 | 物品临时增益 |
| 11 | ItemBuffPermanent | 通用 | 物品永久增益 |
| 12 | Ranking | 通用 | 排名头衔 |
| 13 | Developer | 通用 | 开发者标志 |
| 14 | Veteran | 通用 | 老玩家 |
| 15 | MapEffect | 通用 | 地图全局效果 |
| 16 | InstanceEffect | 通用 | 副本效果 |
| 17 | Guild | 通用 | 行会 |
| 18 | DeathDrops | 通用 | 死亡掉落保护 |
| 19 | Fame | 通用 | 声望 |
| 20 | RedGem | 通用 | 红宝石 |
| 21 | BlueGem | 通用 | 蓝宝石 |
| 22 | CursedGem | 通用 | 诅咒宝石 |
| 100 | Defiance | 战士 | 破釜沉舟(防御姿态) |
| 101 | Might | 战士 | 力量 |
| 102 | Endurance | 战士 | 耐力(防 Beckon) |
| 103 | ReflectDamage | 战士 | 伤害反弹 |
| 104 | Invincibility | 战士 | 无敌(5+Level 秒) |
| 105 | DefensiveBlow | 战士 | 防御反击 |
| 106 | Dash | 战士 | 冲撞(ShoulderDash,hidden:true) |
| 107 | ElementalSwords | 战士 | 元素剑 |
| 200 | Renounce | 法师 | 牺牲(损血换攻) |
| 201 | MagicShield | 法师 | 魔法盾 |
| 202 | JudgementOfHeaven | 法师 | 天堂审判 |
| 203 | ElementalHurricane | 法师 | 元素飓风 |
| 204 | SuperiorMagicShield | 法师 | 强化魔法盾 |
| 205 | FrostBite | 法师 | 冰霜咬(迟缓) |
| 206 | Tornado | 法师 | 龙卷风 |
| 300 | Heal | 道士 | 治愈(Tick 型,§2.2) |
| 301 | Invisibility | 道士 | 隐身 |
| 302 | MagicResistance | 道士 | 魔法抵抗 |
| 303 | Resilience | 道士 | 韧性 |
| 304 | ElementalSuperiority | 道士 | 元素克制 |
| 305 | BloodLust | 道士 | 血欲(攻速) |
| 306 | StrengthOfFaith | 道士 | 信仰之力(宠物强化) |
| 307 | CelestialLight | 道士 | 圣光 |
| 308 | Transparency | 道士 | 透明 |
| 309 | LifeSteal | 道士 | 吸血 |
| 310 | Spiritualism | 道士 | 通灵 |
| 311 | SoulResonance | 道士 | 灵魂共鸣 |
| 400 | PoisonousCloud | 刺客 | 毒雾 |
| 401 | FullBloom | 刺客 | 满月绽放(莲花) |
| 402 | WhiteLotus | 刺客 | 白莲 |
| 403 | RedLotus | 刺客 | 红莲 |
| 404 | Cloak | 刺客 | 斗篷(隐身) |
| 405 | GhostWalk | 刺客 | 幽灵行走 |
| 406 | TheNewBeginning | 刺客 | 新开始 |
| 407 | DarkConversion | 刺客 | 黑暗转化 |
| 408 | DragonRepulse | 刺客 | 龙之驱逐(禁施法) |
| 409 | Evasion | 刺客 | 闪避 |
| 410 | RagingWind | 刺客 | 狂暴风 |
| 411 | LastStand | 刺客 | 背水一战 |
| 412 | Concentration | 刺客 | 集中 |
| 500 | MagicWeakness | 特殊 | 魔法弱化(装备/怪物施放) |

> 计数核对:通用 22(1-22)+ 战士 8(100-107)+ 法师 7(200-206)+ 道士 12(300-311)+ 刺客 13(400-412)+ 特殊 1(500)+ None = **64 项**,与枚举文件 231-316 行区间逐一吻合。

### 1.1 Buff 移除豁免名单(ProcessBuff 之外的永久项)

`MapObject.cs:1347-1432` 的 Buff 清理 switch 中,`PoisonousCloud/Observable/TheNewBeginning/Server/MapEffect/InstanceEffect/Castle/Guild/Veteran/Fame`(1424-1433)不随死亡/下线清理;`Invisibility/Cloak/Transparency`(1440-1455)等按场景保留。**Buff 与毒是两套独立状态**:Buff 存 `Buffs` 列表(BuffInfo),毒存 `Poison`(PoisonType 位标志)+ `PoisonList`(`MapObject.cs:106-107`)。

---

## 2. Buff 数据模型与生命周期

### 2.1 BuffInfo 服务端模型(ServerLibrary/DBModels/BuffInfo.cs:8-203)

`sealed class BuffInfo : DBObject` 全字段(实测):

| 字段 | 行号 | 说明 |
|---|---|---|
| Character / Account | 11-40 | 归属角色/账号(双归属,互斥) |
| Type | 42-55 | BuffType |
| Stats | 57-70 | **Buff 提供的属性面板**(攻/防/速等,合并进 RefreshStats) |
| RemainingTime | 72-85 | 剩余时长 |
| TickFrequency | 87-100 | Tick 周期(如 Heal 1s) |
| TickTime | 102-115 | 下次 Tick 时间 |
| ItemIndex | 117-130 | 来源物品(ItemBuff) |
| Visible | 133-146 | 客户端是否显示图标 |
| Pause | 148-161 | 暂停(如进城暂停) |
| Hidden | 163-176 | 隐藏(ShoulderDash 的 Dash buff) |
| Extra | 178-191 | **层数/数值**(客户端 VisibleBuffs 的 int 值,如 Heal 的 Healing 量) |

客户端镜像 `ClientBuffInfo`(LibraryCore/Globals.cs)同构。

### 2.2 BuffAdd 与 ProcessBuff(ServerLibrary/Models/MapObject.cs)

- **入口** `BuffAdd(BuffType, TimeSpan remainingTicks, Stats, bool visible, bool pause, TimeSpan tickRate, bool hidden=false, int extra=0)`:`MapObject.cs:1402` —— 先 `BuffRemove(type)` 去重,再新建 BuffInfo 加入 `Buffs`(1409-1421),Stats 非空则 `RefreshStats()`(1426);`PoisonousCloud/Observable/TheNewBeginning/Server/MapEffect/InstanceEffect/Castle/Guild/Veteran/Fame` 标 `IsTemporary=true`(1428-1442,临时不落库);Visible=true 才 `Broadcast(S.ObjectBuffAdd{ ObjectID, Type, Extra })`(1486-1487)。`BuffAdd` 返回 BuffInfo(Heal.cs:60 等复用返回值设 TickFrequency)。
- **每帧** `ProcessBuff()`:`MapObject.cs:412-775` —— 遍历 Buffs,`TickTime` 到期的执行各类型逻辑:
  - `Companion`(428):骑乘/下马位移控制;
  - `Heal`(472):每 Tick 回 `Stats[Healing]` 点血,超过 `HealingCap` 上限移除(Heal buff 是"持续回血"而非瞬间加血);
  - `Cloak`(493):隐身状态管理(受击显形计时);
  - `DarkConversion`(510):HP↔MP 转化;
  - `PKPoint`(536):红名值衰减;`HuntGold`(557):狩猎币每 Tick 结算;
  - `DragonRepulse`(587):禁施法/禁移动的定时;
  - `FrostBite`(659):迟缓附加;`ElementalHurricane`(676):元素飓风持续伤害。
  - 到期后统一 `BuffRemove(type)` → `RefreshStats()` + `Broadcast(S.ObjectBuffRemove{ ObjectID, Type })`(`MapObject.cs:1572-1600`);永久 Buff(ItemBuffPermanent/Server 等)不随到期移除(§1.1 名单)。
- **Buff 提供属性合并**:`GetStats()`/`RefreshStats()` 遍历 `Buffs` 累加 `Stats`(PlayerObject.cs:11045-11075 附近),**Buff 属性是玩家面板的动态来源之一**。

### 2.3 Buff 协议双通道(自身 vs 广播)

| 通道 | 包 | 行号 | 字段 | 客户端处理 |
|---|---|---|---|---|
| **自身**(精确状态,驱动 BuffDialog) | S.BuffAdd | ServerPackets.cs:760-763 | Buff(ClientBuffInfo) | CConnection.cs:3477-3481 `User.AddBuff` + BuffBox.BuffsChanged |
| | S.BuffRemove | 764-767 | Index | 3483-3493 删 User.Buffs + VisibleBuffs |
| | S.BuffChanged | 768-772 | Index/Stats | 3495-3500 更新 Stats |
| | S.BuffTime | 773-777 | Index/Time | 3502-3507 更新 RemainingTime |
| | S.BuffPaused | 778-781 | Index/Paused | 3509-3513 更新 Pause |
| **广播**(外观,视野内所有人) | S.ObjectBuffAdd | 271-280 | ObjectID/Type/Extra | CConnection.cs:1794-1812 `VisibleBuffs[Type]=Extra`;SuperiorMagicShield 先 EndMagicEffect 收尾旧盾 |
| | S.ObjectBuffRemove | 282-291 | ObjectID/Type | 1814-1822 `VisibleBuffs.Remove` |

服务端发送点(实测):`S.BuffAdd` PlayerObject.cs:9409/9434(BuffAdd 内);`S.BuffRemove` 9475;`S.BuffChanged` 2010/3403/6446;`S.BuffTime` 6468/9392(进安全区暂停等)。**自身通道精确同步(含 Pause),广播通道只做外观(Extra=层数)**。

### 2.4 BuffDialog 显示(Client/Scenes/Views/BuffDialog.cs:14-17)

`Icons` 字典(Dictionary<ClientBuffInfo, DXImageControl>);`Process()` 每帧遍历 `User.Buffs`,对 Visible 项建图标、到期销毁 —— **只显示 Visible=true 的 Buff**(Dash/Cloak 部分隐藏)。

### 2.5 服务端包字段(实测)

| 包 | 行号 | 字段 |
|---|---|---|
| S.ObjectBuffAdd | ServerPackets.cs:271-280 | ObjectID / Type(BuffType)/ Extra(int) |
| S.ObjectBuffRemove | ServerPackets.cs:282-291 | ObjectID / Type |
| S.ObjectPoison | ServerPackets.cs:293-300 | ObjectID / Poison(PoisonType) |

---

## 3. 持续法术 SpellObject(服务端与客户端)

### 3.1 服务端(ServerLibrary/Models/SpellObject.cs:16-330)

`public sealed class SpellObject : MapObject`(`SpellObject.cs:16`),`Race => ObjectType.Spell`(18),`Blocking => false`(20)。字段:

| 字段 | 行号 | 说明 |
|---|---|---|
| DisplayLocation | 22 | 显示格(客户端对齐用) |
| Effect | 23 | **SpellEffect**(持续法术类型,§3.3) |
| TickCount / TickFrequency / TickTime | 24-26 | 持续伤害的剩余次数/周期/下次时间 |
| Owner | 27 | 施法者(伤害归属) |
| Magic | 28 | 施法 UserMagic(等级/威力) |
| Power | 29 | 单次伤害威力 |
| Targets | 31 | 已锁定目标列表 |

- `Process()`(38-131):每帧对 `Targets` 内仍在范围内的对象按 `TickFrequency` 造成 `Power` 伤害(ApplyAttack 走 Owner 的伤害管线),`TickCount` 归零 → `Despawn()`。
- `ProcessSpell(MapObject ob)`(133-262):目标进入格子的加入判定(TrapOctagon 触发、FireWall 点火、PoisonousCloud 落毒等)。
- `GetInfoPacket`(293)/`GetDataPacket`(304):入视野发 S.ObjectSpell,更新发 S.ObjectSpellChanged(见 §3.2 字段)。
- **生成途径**:技能 MagicCast 里 `Spawn`/`CurrentMap.Spawn`(FireWall.cs:33-48、Tempest.cs:40-58、IceAura.cs、PoisonousCloud.cs、TrapOctagon.cs 等),部分技能直接对 SpellObject 调用 `ProcessSpell` 做首帧结算。

### 3.2 客户端(Client/Models/SpellObject.cs:13-204)

- 构造 `new SpellObject(S.ObjectSpell p)`(27):按 `p.Effect` 调 `UpdateLibraries()` 选图库与帧动画;`Draw()`(191)按 `BlendRate` 混合叠加;`SetAnimation`(184)常驻 Standing 循环。
- `UpdateLibraries()`(65-183)特效→素材映射(部分,完整见 §4 旁注):
  - `SafeZone`:Magic 库 649 帧,Blend 0.3,365 天(安全区光圈);
  - `FireWall`:Magic 920 帧×5,150ms/帧,Blend 0.55,`Light=15`,Fire 色(火墙);
  - `Tempest`:MagicEx2 920 帧×10,150ms/帧,Wind 色(风暴);
  - `IceAura`:MagicEx5 2600 帧×10,150ms/帧,Ice 色(冰环);
  - `TrapOctagon`:Magic 图库 6×6 八卦阵帧(200 起);
  - `DarkSoulPrison`:MagicEx1 暗色囚笼帧;
  - `PoisonousCloud`/`BurningFire`/`MonsterDeathCloud`/`ZombieHole` 各有独立帧段。
- **S.ObjectSpell 客户端处理** `Client/Envir/CConnection.cs:865-868`:`new SpellObject(p)`;`S.ObjectSpellChanged`(869-880):更新 `spell.Power` 并重刷 `UpdateLibraries()`(伤害升级时换帧)。

### 3.3 SpellEffect 枚举 12 种(LibraryCore/Enum.cs:1524-1546)

| 值 | 枚举名 | 说明 |
|---|---|---|
| 0 | None | 无 |
| 1 | SafeZone | 安全区光环 |
| 2 | FireWall | 火墙 |
| 3 | Tempest | 风暴 |
| 4 | IceAura | 冰霜光环 |
| 5 | TrapOctagon | 八卦困魔阵 |
| 6 | DarkSoulPrison | 黑暗之魂囚笼 |
| 7 | PoisonousCloud | 毒雾 |
| 8 | BurningFire | 燃烧之火 |
| 9 | Rubble | 碎石 |
| 10 | MonsterDeathCloud | 怪物死亡之云 |
| 11 | ZombieHole | 僵尸洞 |

---

## 4. CreateMagicEffect:客户端魔法特效映射全表(Client/Models/MapObject.cs:5657-6101)

`public List<MirEffect> CreateMagicEffect(MagicEffect magic)`(`MapObject.cs:5657`)——**buff/受击/状态时客户端本地挂的特效**(与 S.ObjectMagic 的施法动画互补);`MagicEffects` 字典(`MapObject.cs:273`)缓存已生成特效,重复查询直接返回;特效循环播放直到 `EndMagicEffect`(如 S.ObjectBuffAdd 换盾时调用)。`MagicEffect` 枚举 29 项 0-28(Enum.cs:1548-1581)。**全 28 case 实测**:

| MagicEffect(枚举值 0-28) | 行号 | 特效构成 |
|---|---|---|
| ReflectDamage(0) | 5851 | 反弹光环 |
| Assault(1) | 5785 | 突进残影 |
| ElementalSwords(2) | 5796 | 元素剑环绕 |
| DefensiveBlow(3) | 5834 | 防御反击 |
| HundredFist(4) | 6067 | 百裂拳残影 |
| MagicShield(5) | 5686 | 魔法盾罩(Magic 库 580 起,绿光罩) |
| MagicShieldStruck(6) | 5704 | 护盾受击闪白(**复用 MagicShield 镜效**,加受击帧) |
| SuperiorMagicShield(7) | 5719 | 强化盾罩(不同色/帧段) |
| SuperiorMagicShieldStruck(8) | 5737 | 强化盾受击(同上复用) |
| ElementalHurricane(9) | 6036 | 元素飓风环绕 |
| FrostBite(10) | 5872 | 冰霜减速 |
| Burn(11) | 6057 | 灼烧火焰 |
| CelestialLight(12) | 5752 | 圣光柱 |
| CelestialLightStruck(13) | 5770 | 圣光受击 |
| Parasite(14) | 5925 | 寄生 |
| Neutralize(15) | 5936 | 中和 |
| WraithGrip(16) | 5668 | 幽魂之握缠绕特效 |
| LifeSteal(17) | 5861 | 吸血红线 |
| Silence(18) | 5882 | 沉默(头顶禁言) |
| Blind(19) | 5892 | 致盲 |
| Fear(20) | 5914 | 恐惧 |
| Abyss(21) | (无 case) | 深渊无独立特效,视觉走 Poison 通道(§6.4 ②③) |
| DragonRepulse(22) | 5947 | 龙之驱逐 |
| Containment(23) | 5998 | 禁锢 |
| Chain(24) | 6008 | 锁链 |
| Hemorrhage(25) | 6026 | 大出血 |
| Binding(26) | 6078 | 束缚 |
| Ranking(27) | 6088 | 排名头衔光环 |
| Developer(28) | 6099 | 开发者光环 |
> 客户端调用点:`ProcessObjectBuffAdd`(CConnection.cs:1794-1812)对 `SuperiorMagicShield`/`ElementalSwords` 调 `EndMagicEffect`(旧特效收尾);中毒/受击在 MapObject.Process 里按 Poison/状态调 `CreateMagicEffect`(MapObject.cs:407-425 附近)。特效实体 `MirEffect`(Client/Models/MirEffect.cs)负责帧动画/混合/光影。

---

## 5. PoisonType:16 种位标志毒(Enum.cs:1502-1522)

`public enum PoisonType`(`Enum.cs:1502`)是 **`[Flags]` 位标志**,对象可同时中多种毒;存于 `MapObject.Poison`(`MapObject.cs:106`,位或合并)。全表:

| 值 | 枚举名 | 说明(源码语义) |
|---|---|---|
| 0 | None | 无 |
| 1 | Green | 绿毒(每 Tick 扣血,PoisonCloud.cs:60-64) |
| 2 | Red | 红毒(受击伤害 +20%,ApplyAttack 中判定) |
| 4 | Slow | 减速(attackTime/actionTime 每点 -100ms,显示蓝) |
| 8 | Paralysis | 麻痹(全种族定身:CanMove/CanAttack/CanCast 全禁,MapObject.cs:86-88) |
| 16 | WraithGrip | 幽魂之握(禁 Dash+禁移动,ShoulderDash.cs:20-25 校验) |
| 32 | HellFire | 地狱火(Tick 伤害,无色) |
| 64 | Silenced | 沉默(禁移动全种族;禁物理/魔法攻击限怪物,显示特效) |
| 128 | Abyss | 深渊(缩小怪物视野;玩家显示致盲特效) |
| 256 | Parasite | 寄生(Tick 伤害+爆炸,无视透明) |
| 512 | Neutralize | 中和(禁攻击、拖慢 actionTime,客户端 MagicDelay 加倍) |
| 1024 | Fear | 恐惧(怪物专属:停攻击+逃跑) |
| 2048 | Burn | 灼烧(Tick 伤害) |
| 4096 | Containment | 禁锢(Tick 伤害+禁移动) |
| 8192 | Chain | 锁链(Tick 伤害+限制移动) |
| 16384 | Hemorrhage | 大出血(Tick 伤害+停止恢复) |
| 32768 | Binding | 束缚(Tick 伤害+禁移动) |

> 与动作层联动:CanMove/CanAttack/CanCast 三闸门(`MapObject.cs:86-88`)分别检查 Paralysis/WraithGrip/Containment/Binding(移动)、Paralysis/Fear(攻击)、Paralysis/Fear/Silenced(施法)——**毒是玩家控制权的核心裁判**。

---

## 6. 毒结算链路:ApplyPoison → ProcessPoison → S.ObjectPoison

### 6.1 Poison 结构(ServerLibrary/Models/MapObject.cs:1849-1861)

```csharp
public class Poison
{
    public MapObject Owner;      // 施毒者(伤害归属/练级)
    public PoisonType Type;      // 毒类型(位标志)
    public int Value;            // 单次伤害/强度
    public TimeSpan TickFrequency; // Tick 周期(绿毒 2s)
    public int TickCount;        // 剩余次数(绿毒 = 威力/2)
    public DateTime TickTime;    // 下次结算时间
    public object Extra, Extra1, Extra2; // 附加参数
    public bool CanKill;         // 能否致死
}
```

### 6.2 ApplyPoison 入口(MapObject.cs:1641-1700)

- 同类型毒存在时**取更强覆盖**:新毒 Value 更大 → 替换(TickCount/TickFrequency 重置);否则跳过。
- 通用免疫判定:`SEnvir.Random.Next(100) < Stats[Stat.PoisonResistance]` 直接拒绝(MapObject.cs:1644);怪物另有 `ApplyPoison` 覆写(MonsterObject)对指定毒免疫。
- 成功后 `PoisonList.Add`、重算 `Poison` 位标志(逐项 OR,`MapObject.cs:405-408`)、`Broadcast(S.ObjectPoison{ ObjectID, Poison })`(408)——**毒名/叠加完全由位标志承载,客户端收到一个 int**。

### 6.3 ProcessPoison 每帧结算(MapObject.cs:208-260)

`ProcessPoison()`(`MapObject.cs:208`)在 `Process()` 里逐毒检查:`TickTime` 到期 → `ApplyAttack(Owner, 该毒 Value, 无视防御...)` 造成伤害(CanKill=false 时死亡保护,最低 1 血);`TickCount--`,归零移除并重算 `Poison` 标志、广播 `S.ObjectPoison`。**绿毒 2s/次、红毒只改受伤倍率不扣血**。

### 6.4 客户端中毒外观三通道

| 通道 | 机制 | 位置 |
|---|---|---|
| ① 对象头顶毒名/图标 | `ob.Poison` 位标志 → 名字旁画毒标 | MapObject.Draw(Client/Models/MapObject.cs) |
| ② 身体着色 | Green→绿染/Red→红染/Paralysis→电光 等,`Poison & PoisonType.X` 逐位判色 | Client/Models/MapObject.cs:329-345(着色分支) |
| ③ 魔法特效 | 毒类状态调 `CreateMagicEffect`(§4:Silence/Blind/Fear/Parasite/Neutralize/Burn 等) | MapObject.cs:407-425 |
| ④ 受击修正 | 服务端 ApplyAttack 里 `Poison & Red` → `damage += damage*20/100` | ServerLibrary/Models/MapObject.cs:1300-1320(ApplyAttack 内) |

### 6.5 毒的施加方(技能侧,文档 28 交叉)

- `ApplyPoison` 直接调用:PoisonCloud.cs:60-64(绿毒,威力/2 次,2s 周期)、PoisonDust.cs、WraithGrip.cs(定身)、HellFire.cs、Abyss.cs、Silence.cs、Neutralize.cs、Burn.cs、Fear.cs 等(每文件 MagicComplete 内)。
- **毒与 Buff 是两条独立管线**:毒走 `PoisonList`+位标志,状态走 `Buffs`(BuffInfo)列表;但效果(减速/定身/禁施)在 CanMove/CanAttack/CanCast 里按位判断(§5 表),客户端显示走毒标(6.4 ①②)而非 BuffDialog。

---

## 7. 客户端 Buff/毒显示汇总

| UI | 文件:行号 | 机制 |
|---|---|---|
| BuffDialog 图标栏 | Client/Scenes/Views/BuffDialog.cs:14-17 | `Icons` 字典,Visible=true 才画 |
| MagicBarDialog CD | Client/Scenes/Views/MagicBarDialog.cs:52,311-326 | Cooldowns 字典,NextCast 秒数(见文档 28 §9.3) |
| 毒名/着色 | Client/Models/MapObject.cs Draw 分支 | 按 `Poison` 位标志 |
| 状态特效 | Client/Models/MapObject.cs:5657+ | CreateMagicEffect 28 case(§4) |

## 8. 与文档 28/30 的衔接

- 文档 28 §7 技能剖析:Buff 来源(Heal/MagicShield/Invincibility/Cloak)与毒来源(PoisonCloud/PoisonDust)均在本文件 §1-§6 有完整数据模型与结算;SpellObject 由技能 Spawn(文档 28 §4.2 MagicCast 模板)。
- 文档 30 物品:ItemBuff/ItemBuffPermanent(§1 表 10-11)由物品触发(SetInfo 装备/药品);RedGem/BlueGem/CursedGem(20-22)是宝石 Buff;MagicWeakness(500)由装备/怪物施加。
