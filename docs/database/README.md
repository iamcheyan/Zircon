# Zircon 游戏数据库文档（System.db）

本文档由 [Tools/SystemDbProbe](../../Tools/SystemDbProbe/Program.cs) 直接读取 `Debug/Server/Database/System.db` 生成，
相当于一个**离线数据库查看器**：每一张地图、每一只怪物、每一件物品……的每个字段都能查到实际值。

> **想快速浏览（不看字段，只看内容）** → 直接进 [**玩家视图**](views/README.md)：
> 技能/怪物/物品/地图/任务/NPC 分类整理成直观条目（属性、刷新、掉落、奖励一目了然）。

- **集合一览（全部表 + 记录数 + 文档入口）** → [_summary.md](_summary.md)
- **枚举字典**（所有字段中出现的枚举成员、数值、说明） → [data/enums.md](data/enums.md)
- **属性字典**（`Stats` 字段中出现的属性名含义） → [data/stats.md](data/stats.md)

---

## 1. 数据源

| 项 | 值 |
|---|---|
| 文件 | `Debug/Server/Database/System.db`（MirDB 二进制格式，`Library.Encryption` 加密容器） |
| 模型程序集 | `LibraryCore/SystemModels`（所有 `DBObject` 子类，不含 `[UserObject]` 的用户数据） |
| 读取方式 | `Session(SessionMode.Users, root)` + `session.Initialize(...)`，与服务器共用同一套 MirDB 加载代码 |
| 数据库版本 | 见各次运行输出的 `版本` 字段 |

> System.db 存的是**游戏静态配置**（地图/怪物/物品/魔法/NPC/任务……）；
> 玩家角色、行会、拍卖等动态数据在 `Users.db`（`ServerLibrary/DBModels`，标注 `[UserObject]`），不在本文档范围内。

### 重新生成

```bash
# 只打印各表记录数
dotnet run --project Tools/SystemDbProbe
# 全量生成 Markdown 文档到 docs/database/
dotnet run --project Tools/SystemDbProbe -- --dump docs/database
# 生成玩家视图（分类浏览，直观条目）到 docs/database/views/
dotnet run --project Tools/SystemDbProbe -- --view docs/database/views
```

生成规则：每个集合（表）一个文件，放在 `data/`；记录数超过 300 时自动按 Index 分段
（`DropInfo.1.md`、`DropInfo.2.md` …）。`data/*.md` 与 `_summary.md` 全部自动生成，**请勿手改**；
`README.md`（本文件）为手工维护的字段释义。

---

## 2. 阅读指南（值格式约定）

每个集合文件的结构：

```
# 地图（MapInfo）                     ← 集合名 + 中文名
> 记录 #0 – #243，共 244 条。         ← 范围（#N 即数据库 Index，索引键）
## 快速浏览                           ← 概览表：一行一条记录的关键字段（主要集合都有；魔法另有「按职业分组」速查）
### #1 · 0                           ← 单条记录：Index + 标识字段（IsIdentity）
| 字段 | 值 |                         ← 该记录的全部字段
```

字段值的显示规则：

| 类型 | 显示方式 | 示例 |
|---|---|---|
| 整数/小数/字符串/bool | 原样 | `Price 1000`、`CanSell true` |
| 枚举 | 成员名（含义见 [enums.md](data/enums.md)） | `ItemType Weapon`、`Light Night` |
| 引用（DBObject） | `名称 (#Index)`，可跳转到对应集合文件 | `Monster Oma (#22)` |
| 列表（DBBindingList） | `类型 × N 条（明细见 xxx.md）` | `Drops × 3 条` |
| `Stats` 字段 | `属性名 数值, ...`（含义见 [stats.md](data/stats.md)） | `Health 500, MinAC 1` |
| `byte[]` | 前 16 字节 HEX + 总长度 | `` `A1B2...`（共 2048 字节） `` |
| `DateTime` | `yyyy-MM-dd HH:mm:ss` | `2024-01-01 12:00:00` |
| 空值 | 该行省略；空字符串显示 `—` | |

---

## 3. 字段释义（Glossary）

### 3.1 地图 MapInfo（244 条）

| 字段 | 含义 |
|---|---|
| `FileName` | 地图资源文件名（`0`=比奇、`0_000`=议事厅、`D1001`=半兽人寺庙……），唯一标识 |
| `Description` | 地图显示名，如 `Bichon Town` |
| `MiniMap` | 小地图编号 |
| `Light` | 光照设置（`Default`/`Light`/`Night` 等） |
| `Weather` | 天气（`None`/`Rain` 等） |
| `Fight` | 战斗设置（`None`/`Safe` 等） |
| `AllowRT` | 允许随机传送（回城/随机卷） |
| `SkillDelay` | 技能延迟系数 |
| `CanHorse` | 允许骑马 |
| `CanAutoPath` | 允许自动寻路 |
| `AllowTT` | 允许地牢传送（传送门/队友召唤） |
| `CanMine` | 允许挖矿 |
| `CanMarriageRecall` | 允许夫妻召唤 |
| `AllowRecall` | 允许被召唤 |
| `MinimumLevel` / `MaximumLevel` | 进入地图的等级限制（0 = 不限） |
| `ReconnectMap` | 断线重连回到的地图 |
| `Music` | 背景音乐（`SoundIndex` 枚举） |
| `Background` | 背景图编号 |
| `MonsterHealth` … `MaxGoldRate` | **已废弃（代码标注 DO NOT USE）**，怪物血量/伤害/爆率/经验/金币倍率由 Stats 属性实现 |
| `Instance` | 所属副本（关联 `InstanceInfo`） |
| `DungeonMap` / `Dungeon` | 所属地下城（关联 `DungeonMapInfo`/`DungeonInfo`） |
| `RequiredClass` | 进入所需职业 |
| `Guards` | 该地图的守卫（明细见 `GuardInfo`） |
| `Regions` | 该地图的区域（明细见 `MapRegion`：安全区/刷新区/传送区等） |
| `Mining` | 矿点（明细见 `MineInfo`） |
| `Castles` | 沙巴克城（明细见 `CastleInfo`） |
| `BuffStats` / `Stats` | 地图加成属性（如全局经验倍率），`MapInfoStat` 明细 + 汇总 |

### 3.2 怪物 MonsterInfo（309 条）

| 字段 | 含义 |
|---|---|
| `MonsterName` | 怪物名，唯一标识（`Guard` 等 # 开头为特殊单位） |
| `Image` | 怪物形象（`MonsterImage` 枚举，决定用哪套帧动画） |
| `AI` | AI 编号（-1 = 特殊 AI / 守卫） |
| `Level` | 等级（250 = Boss 级） |
| `ViewRange` | 视野范围（格） |
| `CoolEye` | 反隐形/反伪装能力（格） |
| `Experience` | 击杀经验（小数，乘以地图经验倍率） |
| `Undead` | 是否亡灵（受神圣/驱魔加成） |
| `CanPush` | 能否被推动 |
| `CanTame` | 能否被驯服 |
| `AttackDelay` | 攻击间隔（毫秒，默认 2500） |
| `MoveDelay` | 移动间隔（毫秒，默认 1800） |
| `IsBoss` | 是否 Boss |
| `Flag` | 怪物标记（`MonsterFlag` 位标志，如复活/闪电攻击等） |
| `FaceImage` | 头像编号 |
| `MonsterInfoStats` / `Stats` | 怪物属性（血/攻/防/元素……），明细 + 汇总 |
| `Respawns` | 刷新点（明细见 `RespawnInfo`） |
| `Drops` | 掉落表（明细见 `DropInfo`） |
| `Events` | 事件触发器（明细见 `MonsterEventTrigger`） |
| `QuestDetails` | 被哪些任务引用（明细见 `QuestTaskMonsterDetails`） |

### 3.3 物品 ItemInfo（1078 条）

| 字段 | 含义 |
|---|---|
| `ItemName` | 物品名，唯一标识 |
| `ItemType` | 物品类型（`Weapon`/`Armour`/`Consumable`/`Book`/`Ring`…… 见 enums.md） |
| `RequiredClass` | 职业限制（`All`/`Warrior`/`WizTao`/`Assassin`……） |
| `RequiredGender` | 性别限制（`None`/`Male`/`Female`） |
| `RequiredType` / `RequiredAmount` | 装备前提：需要装备某类物品 + 数量 |
| `Shape` | 外观形状编号 |
| `ItemEffect` | 装备特效（`ItemEffect` 枚举，如自动吃药/复活） |
| `ExteriorEffect` | 外观特效（翅膀/光环等，见 enums.md） |
| `Image` | 图标编号 |
| `Durability` | 耐久 |
| `Price` | 基础价格（金币） |
| `Weight` | 重量 |
| `StackSize` | 堆叠上限（1 = 不可堆叠） |
| `StartItem` | 是否新手初始物品 |
| `SellRate` | 卖给商店的折价率（默认 0.5） |
| `CanRepair` / `CanSell` / `CanStore` / `CanTrade` / `CanDrop` / `CanDeathDrop` | 是否可修理/出售/存仓库/交易/丢弃/死亡掉落 |
| `Description` | 物品描述文本 |
| `Rarity` | 品质（`Common`/`Elite`/`Legendary`……） |
| `CanAutoPot` | 是否可自动喝药（自动回复） |
| `BuffIcon` | Buff 图标编号 |
| `PartCount` | 部件数（多部件装备） |
| `Set` | 所属套装（关联 `SetInfo`，凑齐触发 `SetInfoStat`） |
| `ItemStats` / `Stats` | 装备属性（AC/DC/MC/SC/元素……），明细 + 汇总 |
| `Drops` | 被哪些怪物掉落（明细见 `DropInfo`） |

### 3.4 魔法 MagicInfo（174 条）

> 文件头部有「快速浏览」全表 + 「按职业分组」技能速查（战士 32 / 法师 42 / 道士 47 / 刺客 53），按职业浏览技能最直观；单条记录仍是完整字段表。

| 字段 | 含义 |
|---|---|
| `Name` | 魔法名，唯一标识 |
| `Magic` | 魔法类型（`MagicType` 枚举） |
| `Class` | 所属职业（`Warrior`/`Wizard`/`Taoist`/`Assassin`） |
| `School` | 魔法流派（`MagicSchool`：元素/召唤/……） |
| `Property` | 属性（`MagicProperty`：火/冰/雷/风/神圣/暗影……） |
| `Icon` | 图标编号 |
| `MinBasePower` / `MaxBasePower` | 基础威力区间 |
| `MinLevelPower` / `MaxLevelPower` | 每级成长威力区间 |
| `BaseCost` / `LevelCost` | 基础耗蓝 / 每级耗蓝 |
| `NeedLevel1`/`NeedLevel2`/`NeedLevel3` | 升 1/2/3 级所需角色等级 |
| `Experience1`/`Experience2`/`Experience3` | 升 1/2/3 级所需熟练度 |
| `Delay` | 施法间隔（毫秒） |
| `Description` | 技能描述 |

### 3.5 NPC 体系（125 NPC + 页面/商品/检查/动作/按钮）

| 集合 | 字段 | 含义 |
|---|---|---|
| `NPCInfo` | `Region` | 所在区域（`MapRegion`） |
| | `NPCName` | NPC 名，唯一标识 |
| | `Image` / `FaceImage` | 站姿形象 / 头像 |
| | `GoodsIndex` | 商品栏编号（对应 `NPCGood.GoodsIndex`） |
| | `MapIcon` | 地图上显示的图标（`MapIcon` 枚举） |
| | `EntryPage` | 对话入口页面（`NPCPage`） |
| `NPCPage` | `Description` | 页面显示文本 |
| | `DialogType` | 对话框类型（`NPCDialogType`） |
| | `Say` | 开场白 |
| | `SuccessPage` / `FailPage` | 检查通过/失败后的跳转页面 |
| | `Arguments` | 附加参数 |
| | `Currency` | 交易用货币（`CurrencyInfo`） |
| | `Goods` / `Types` / `Checks` / `Actions` / `Buttons` / `Values` | 子项列表 |
| `NPCGood` | `Item` / `Rate` / `GoodsIndex` / `BaseCost` | 出售物品 / 价格倍率 / 商品栏 / 实际售价（计算值） |
| `NPCType` | `ItemType` | 该页面可处理/展示的物品类型 |
| `NPCCheck` | `CheckType` + `Operator` + 参数 | 条件判断（等级/金币/持有物品/PK值……），参数为 `StringParameter1`/`IntParameter1`/`IntParameter2`/`ItemParameter1`/`StatParameter1` |
| `NPCAction` | `ActionType` + 参数 | 执行动作（给物品/扣钱/传送……），参数同上 + `MapParameter1`/`InstanceParameter1` |
| `NPCButton` | `ButtonID` / `DestinationPage` | 按钮编号 → 跳转页面 |
| `NPCRequirement` | `Requirement` + `IntParameter1`/`QuestParameter`/`Class`/`DaysOfWeek` | 使用 NPC 功能的前置条件（等级/任务/职业/星期） |
| `NPCValue` | `ValueID`/`ValueType`/`DataCategory`/`DataType`/`FieldType` | 脚本变量定义 |

### 3.6 刷新点 RespawnInfo（1471 条）

| 字段 | 含义 |
|---|---|
| `Monster` | 刷出的怪物（`MonsterInfo`） |
| `Region` | 刷新区域（`MapRegion`，通常是一块区域而非单点） |
| `EventSpawn` | 是否事件刷怪 |
| `Delay` | 刷新间隔（毫秒） |
| `Count` | 刷新数量 |
| `DropSet` | 掉落组编号（与 `DropInfo.DropSet` 对应，支持同怪多套掉落） |
| `Announce` | 刷新时是否全服公告 |
| `EasterEventChance` | 复活节事件触发概率 |
| `RespawnIndex` | 刷新组编号（同一组 = 同一种怪在多地点的组合） |

### 3.7 掉落 DropInfo（10382 条）

| 字段 | 含义 |
|---|---|
| `Monster` / `Item` | 哪只怪掉哪件物品 |
| `Chance` | 概率（1/N 形式） |
| `Amount` | 掉落数量 |
| `DropSet` | 掉落组编号（与刷新点/怪物分组对应） |
| `PartOnly` | 是否只在对应刷新组的怪身上掉 |
| `EasterEvent` | 是否复活节限定掉落 |

### 3.8 任务 QuestInfo（34 条）

| 集合 | 字段 | 含义 |
|---|---|---|
| `QuestInfo` | `QuestName` | 任务名，唯一标识 |
| | `QuestType` | 任务类型（主线/支线/每日……） |
| | `AcceptText`/`ProgressText`/`CompletedText`/`ArchiveText` | 接取/进行中/完成/归档文案 |
| | `StartNPC` / `FinishNPC` | 接任务/交任务的 NPC |
| `QuestReward` | `Item`/`Amount`/`Choice`/`Bound`/`Duration`/`Class` | 奖励物品/数量/是否多选一/是否绑定/限时/限定职业 |
| `QuestRequirement` | `Requirement` + `IntParameter1`/`QuestParameter`/`Class` | 接任务前置（等级/职业/前置任务） |
| `QuestTask` | `Task` + `ItemParameter`/`RegionParameter`/`MobDescription`/`Amount`/`MonsterDetails` | 任务步骤（杀怪/收集/到达区域） |
| `QuestTaskMonsterDetails` | `Monster`/`Map`/`Chance`/`Amount` | 杀怪目标明细（哪些怪、在哪张图、概率） |

### 3.9 副本 InstanceInfo（数据中当前为空）

`Name`、`Type`（`InstanceType`）、`MaxInstances`（最大并行实例数）、`ShowOnDungeonFinder`、
`SafeZoneOnly`、`AllowRejoin`、`AllowTeleport`、`SavePlace`、`MinPlayerLevel`/`MaxPlayerLevel`、
`MinPlayerCount`/`MaxPlayerCount`、`RequiredItem`/`RequiredItemSingleUse`（门票）、
`ConnectRegion`/`ReconnectRegion`、`CooldownTimeInMinutes`、`TimeLimitInMinutes`、`ShowTimer`；
子表 `InstanceMapInfo`（`Map`/`RespawnIndex`）、`InstanceInfoStat`（`Stat`/`Amount`）。

### 3.10 沙巴克 CastleInfo（1 条）

`Name`、`Map`（所在地图）、`StartTime`/`Duration`（攻城时间窗）、`CastleRegion`/`ObjectiveRegion`/`AttackSpawnRegion`
（城堡区/目标区/攻方出生区）、`Item`/`Monster`/`Discount`（占领奖励物品/守城怪/商店折扣）；
子表 `CastleFlagInfo`（旗帜：`Monster`/`X`/`Y`）、`CastleGateInfo`（城门：`Monster`/`X`/`Y`/`RepairCost`）、
`CastleGuardInfo`（守卫：`Monster`/`X`/`Y`/`Direction`/`RepairCost`）。

### 3.11 其他集合

| 集合 | 字段与含义 |
|---|---|
| `BaseStat`（360） | 各职业各等级基础属性：`Class`/`Level` + `Health`/`Mana`/`BagWeight`/`WearWeight`/`HandWeight`/`Accuracy`/`Agility`/`MinMax AC·MR·DC·MC·SC` |
| `MapRegion`（1666） | 地图区域：`Map`/`Description`/`RegionType`（安全区/刷新/传送/……）/`BitRegion`/`PointRegion`/`Size` |
| `MovementInfo`（554） | 传送点：`SourceRegion`/`DestinationRegion`/`Icon`/`NeedItem`/`NeedSpawn`/`NeedHole`/`NeedInstance`/`Effect`/`RequiredClass`/`SkipValidation` |
| `MineInfo`（20） | 矿点：`Map`/`Item`（产出矿物）/`Chance`/`Region`/`Quantity`/`RestockTimeInMinutes` |
| `SafeZoneInfo`（13） | 安全区：`Region`/`BindRegion`（复活点）/`StartClass`/`RedZone`/`Border` |
| `GuardInfo`（68） | 守卫：`Map`/`Monster`/`X`/`Y`/`Direction` |
| `SetInfo`（30）+ `SetInfoStat`（200） | 套装：`SetName`；套装属性：`Set`/`Stat`/`Amount`/`Class`/`Level`（件数档位） |
| `CurrencyInfo`（5） | 货币：`Name`/`Abbreviation`/`Type`（金币/游戏点/狩猎金币……）/`Category`/`DropItem`/`ExchangeRate`；`CurrencyInfoImage`（`Image`/`Amount` 面额） |
| `CompanionInfo`（10） | 宠物：`MonsterInfo`/`Description`/`Price`/`Currency`/`Available`/`UnlockItem`；`CompanionLevelInfo`（`Level`/`MaxExperience`/`InventorySpace`/`InventoryWeight`/`MaxHunger`）；`CompanionSkillInfo`（`Level`/`StatType`/`MaxAmount`/`Weight`）；`CompanionSpeech`（`Action`/`Speech`） |
| `StoreInfo`（92） | 商城：`Item`/`Price`/`HuntGoldPrice`/`Filter`/`Available`/`Duration` |
| `WeaponCraftStatInfo`（110） | 武器锻造属性池：`RequiredClass`/`Stat`/`MinValue`/`MaxValue`/`Weight`（随机权重） |
| `DisciplineInfo`（4） | 修炼等级：`Level`/`RequiredLevel`/`RequiredExperience`/`RequiredGold`/`FocusPoints` |
| `FameInfo`（9） | 声望称号：`Name`/`Shape`/`Description`/`Cost`/`Order`；`FameInfoStat`（`Stat`/`Amount`）；`FameInfoReward`（`Item`/`Amount`） |
| `FishingInfo` + `FishingDropInfo` | 钓鱼：`Name`/`Region`；渔获：`Item`/`Chance`/`ThrowQuality`/`PerfectCatch` |
| `LootBoxInfo` + `LootBoxItemInfo` | 宝箱：`Description`/`Currency`；内容：`Item`/`Amount` |
| `HelpInfo`/`HelpPageInfo`/`HelpItemInfo` | 帮助系统：`Title`/`Order`/`Description`/`Content` |
| `MilestoneInfo` + `MilestoneInfoTask` | 成就里程碑：`Title`/`Category`/`Grade`/`Description`/`Task`/`ShowCount`/`RequiredClass`/`Reward`/`RewardAmount`；目标：`Type` + `Item`/`Monster`/`Currency`/`Region`/`Instance`/`Quest`/`Magic`/`Class` |
| `BundleInfo` + `BundleItemInfo` | 礼包：`Description`/`Type`（`BundleType`）/`SlotSize`/`AutoOpen`/`LootBox`；内容：`Item`/`Amount` |
| `DungeonInfo` + `DungeonMapInfo` | 地下城：`Name`/`Description`/`SpawnMultiplier`；楼层：`Map`/`Floor`/`Role`（`DungeonMapRole`） |
| 事件系列 | `WorldEventInfo`（`Description`/`MaxValue`/`ResetWhenMax`）+ `WorldEventTrigger`（`Type`/`Value`/`MaxTriggers`）+ `WorldEventAction`/`WorldEventInfoTriggerStat`；玩家事件 `PlayerEventInfo`/`PlayerEventTrigger`；怪物事件 `MonsterEventInfo`/`MonsterEventTrigger` |
| `SystemDatabaseInfo` | 数据库自身信息：`Name`/`Version`（`2026.08.06.N` 格式，读档时显示） |

---

## 4. 常用属性速查（Stat，完整见 [data/stats.md](data/stats.md)）

`Stats` 字段（物品/怪物/地图的加成）里出现的属性键：

- **战斗**：`MinAC`/`MaxAC` 物防、`MinMR`/`MaxMR` 魔防、`MinDC`/`MaxDC` 物攻、`MinMC`/`MaxMC` 魔法、`MinSC`/`MaxSC` 道术、`Accuracy` 命中、`Agility` 敏捷、`AttackSpeed` 攻速
- **元素**：`Fire/Ice/Lightning/Wind/Holy/Dark/Phantom` 的 `Attack`（攻击）与 `Resistance`（抵抗）
- **生命法力**：`Health`/`Mana` 及百分比版 `HealthPercent`/`ManaPercent`、`HealthPercent`、`BaseHealth`/`BaseMana`
- **成长**：`ExperienceRate` 经验倍率、`DropRate` 爆率倍率、`GoldRate` 金币倍率（百分比）
- **实用**：`BagWeight`/`WearWeight`/`HandWeight` 负重、`PickUpRadius` 拾取范围、`CriticalChance` 暴击率、`CriticalDamage` 暴击伤害、`Luck` 幸运、`Strength` 强度、`Light` 光照半径
- **特殊**：`LifeSteal` 吸血、`ReflectDamage` 反伤、`Comfort` 回复、`SkillRate` 技能熟练度倍率、`MagicShield` 魔法盾、`Invisibility` 隐身、`PKPoint`、`ItemReviveTime` 复活冷却
- 完整 150+ 属性 → [data/stats.md](data/stats.md)

---

## 5. 典型查询路线

- **查一张地图**：`_summary.md` → `data/MapInfo.md` → 快速浏览找 `FileName` → 点进详情看 `Regions`/`Respawns`/`Guards` 关联数 → 跳 `MapRegion.md`/`RespawnInfo.md` 看具体区域与刷怪。
- **查一只怪掉什么**：`data/MonsterInfo.md` → 详情 `Drops × N 条` → `data/DropInfo.md` 按 `Monster` 过滤。
- **查一件装备**：`data/ItemInfo.N.md` → 详情看 `ItemStats`/`Stats`（属性）、`Set`（套装）、`Drops`（哪里掉）。
- **查任务**：`data/QuestInfo.md` → `QuestTask`/`QuestTaskMonsterDetails` 看杀什么怪、`QuestReward` 看给什么。
- **查技能**：`data/MagicInfo.md` → 「按职业分组」看某职业全部技能与等级门槛 → 点进单条看威力/耗蓝/熟练度明细。
