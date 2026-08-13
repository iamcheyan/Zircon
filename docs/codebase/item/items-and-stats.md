# 物品系统与属性（ItemInfo / UserItem / 随机属性 / 精炼强化 / 耐久修理 / 绑定时效 / 佩带条件 / 套装）

## TL;DR 速查表

- 物品静态模板 = `ItemInfo`（System.db），玩家实例 = `UserItem`（Users.db），二者靠 `UserItem.Info` 关联；客户端只收 `ClientUserItem` 快照（`LibraryCore/Globals.cs:454`）。
- 随机词条：掉落生成时 `SEnvir.CreateDropItem` 以 `chance=15`（非常规稀有度 ×2）抽取一次 `UpgradeWeapon/Shield/Armour/...`（`ServerLibrary/Envir/SEnvir.cs:2149-2228`）。**本仓库不存在** `RandomItemStat` / `GenerateItem` / `ExtraGenRate` 这类函数名。
- 武器精炼成功率：`maxChance = 90 - weapon.Level + special`（品质 −5~+20，封顶 100）；`chance = 60 - weapon.Level*5 + ore/2000 + items/6 + quality*25`，再 `min(maxChance, chance)`（`ServerLibrary/Models/PlayerObject.cs:12389-12419`）。
- 饰品精炼成功率：`chance = 100 - ore.CurrentDurability/1000`，`success = 30`（非常规稀有度 40），`Random.Next(chance) < success`（`ServerLibrary/Models/PlayerObject.cs:11508-11514`）。
- 耐久磨损入口统一是 `PlayerObject.DamageItem`（`PlayerObject.cs:8645`）：武器每次命中 1~2 点、受击全身 1~2 点、红名死亡掉 `Durability/10`；普通修理每次损失 `(MaxDurability-CurrentDurability)/15` 上限耐久（`Globals.DuraLossRate=15`），特修不损但收费 ×2 且有冷却。
- 绑定 = `UserItemFlags.Bound`（无 `BindMode` 字段）；时效 = `UserItemFlags.Expirable + ExpireTime`，只在**非安全区**倒计时（`PlayerObject.cs:577-596`）。**本仓库无"拾取即绑定"逻辑**，Bound 来自商店/任务/里程碑发放。
- 佩带判定三连：`CanUseItem`（性别/职业/RequiredType）→ `CanWearItem`（槽位 + 负重）；套装加成要求集齐 `SetInfo.Items` 全部且耐久 >0（`PlayerObject.cs:2233-2247, 2349-2375`）。
- 打造 = `NPCWeaponCraft`（武器模板 + 6 色宝石，词条池 `WeaponCraftStatInfo` 按 Weight 加权抽取，`PlayerObject.cs:13868-13917`）。类名是 `WeaponCraftStatInfo`，文件名是 `WeaponCraftStatsInfo.cs`（带 s），全库搜 `WeaponCraftStatsInfo`（类名）为 0 命中。
- GodotClient 物品 UI 移植度很高：格子/背包/仓库/修理/全部精炼面板/打造/碎片/悬停提示均已移植，tooltip 为单色简化版（内容对齐）。

## 职责概述

本文覆盖 Zircon（Mir3）引擎中"物品"从模板到实例的完整数据链路与规则引擎：

1. **模板层**：`ItemInfo`（System.db 的物品百科：类型、需求、形状、价格、堆叠、各种许可位）与 `ItemInfoStat`（模板固有属性）。
2. **实例层**：`UserItem`（玩家实际持有的一件物品：耐久、数量、等级、经验、颜色、词条、插槽、归属、标志、时效）与 `UserItemStat`（实例附加词条，带 `StatSource` 来源标记）。
3. **生成**：掉落物的随机词条生成（`SEnvir.Upgrade*` 系列）、初始耐久、颜色随机。
4. **成长**：武器经验/等级（`WeaponExperienceList`）、武器精炼（`RefineInfo`/`NPCRefine`/`NPCRefineRetrieve`）、饰品升级/精炼/重置、大师精炼、武器打造（`NPCWeaponCraft`）。
5. **消耗与维护**：耐久磨损（`DamageItem`）、修理/特修（`NPCRepair`、修理油 `SpecialRepair`）、价格与修理费公式、碎片化（`FragmentCost/FragmentCount`）。
6. **限制**：佩带/使用条件（`CanUseItem`/`CanWearItem`）、绑定（`UserItemFlags`）、时效（`ProcessItemExpire`）、套装（`SetInfo`/`SetInfoStat`）。
7. **协议**：服务器通过 `UserItem.ToClientInfo()` → `ClientUserItem` 下发；耐久/经验/词条变化分别走 `S.ItemDurability` / `S.ItemExperience` / `S.ItemStatsChanged`。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| LibraryCore/SystemModels/ItemInfo.cs | 7-497 | 物品模板（System.db），全部静态字段 + `Stats` 聚合 |
| LibraryCore/SystemModels/ItemInfoStat.cs | 5-55 | 模板固有属性三元组：Item/Stat/Amount |
| LibraryCore/SystemModels/SetInfo.cs | 6-30 | 套装定义（名称 + Items + SetStats） |
| LibraryCore/SystemModels/SetInfoStat.cs | 5-95 | 套装词条（Set/Stat/Amount/Class/Level 均为 Identity） |
| LibraryCore/SystemModels/WeaponCraftStatsInfo.cs | 5-86 | 打造词条池：RequiredClass/Stat/MinValue/MaxValue/Weight |
| LibraryCore/Enum.cs | 64-87 | RequiredClass / RequiredGender（Flags 枚举） |
| LibraryCore/Enum.cs | 317-341 | RequiredType / Rarity |
| LibraryCore/Enum.cs | 410-461 | ItemType 全量取值 |
| LibraryCore/Enum.cs | 1728-1857 | ExteriorEffect / ItemEffect |
| LibraryCore/Enum.cs | 1888-1902 | UserItemFlags（绑定/锁定/时效等标志位） |
| LibraryCore/Stat.cs | 507-895, 897-930 | Stat 枚举（属性种类）、StatSource、StatType、StatDescription |
| LibraryCore/Globals.cs | 257-294 | WeaponExperienceList / AccessoryExperienceList |
| LibraryCore/Globals.cs | 296-302 | InventorySize/EquipmentSize/StorageSize 等网格常量 |
| LibraryCore/Globals.cs | 318-325 | RefineTimes：精炼品质对应等待时长 |
| LibraryCore/Globals.cs | 454-791 | ClientUserItem（协议侧实例快照 + 客户端镜像的 Price/RepairCost/Fragment 公式） |
| ServerLibrary/DBModels/UserItem.cs | 12-828 | 玩家物品实例（Users.db）：全部存档字段 + 归属互斥 + 价格/修理/碎片公式 |
| ServerLibrary/DBModels/UserItemStat.cs | 7-78 | 实例词条：Item/Stat/Amount/StatSource |
| ServerLibrary/DBModels/RefineInfo.cs | 9-142 | 武器精炼任务单：Weapon/Quality/Type/RetrieveTime/Chance/MaxChance |
| ServerLibrary/Envir/SEnvir.cs | 2131-2228 | CreateDropItem ×2：掉落实例化 + 随机词条入口 + 初始耐久 |
| ServerLibrary/Envir/SEnvir.cs | 2297-3223 | UpgradeWeapon/Shield/Armour/Helmet/Necklace/Bracelet/Ring/Shoes 随机词条算法 |
| ServerLibrary/Models/PlayerObject.cs | 577-696 | ProcessItemExpire：时效物品倒计时与销毁 |
| ServerLibrary/Models/PlayerObject.cs | 2036-2081 | GainExperience：武器经验（amount/10）与升级、Refinable 标志 |
| ServerLibrary/Models/PlayerObject.cs | 2233-2247, 2349-2375 | RefreshStats：装备词条汇总与套装判定 |
| ServerLibrary/Models/PlayerObject.cs | 7289-7411 | CanStartWith / CanUseItem（佩带条件判定核心） |
| ServerLibrary/Models/PlayerObject.cs | 8618-8714 | CanWearItem（槽位+负重）、DamageItem / DamageDarkStone（耐久磨损） |
| ServerLibrary/Models/PlayerObject.cs | 10541-10981 | NPCAccessoryLevelUp / Upgrade / Reset（饰品升级链） |
| ServerLibrary/Models/PlayerObject.cs | 11343-11660 | NPCAccessoryRefine（饰品精炼，成功率公式） |
| ServerLibrary/Models/PlayerObject.cs | 11673-11852 | NPCRepair（普通修理/特修/公会资金） |
| ServerLibrary/Models/PlayerObject.cs | 12198-12702 | NPCRefine / NPCRefineRetrieve / NPCResetWeapon（武器精炼全流程） |
| ServerLibrary/Models/PlayerObject.cs | 13553-13922 | NPCWeaponCraft（武器打造，加权词条抽取） |
| ServerLibrary/Models/ItemObject.cs | 16-30, 189 | 地面掉落物的地面时效（Config.DropDuration） |
| ServerLibrary/Models/MonsterObject.cs | 2785-2789 | 掉落 ItemPart 时写入 `Stat.ItemIndex` 指向原件 |

## 核心流程

### 1. 物品从模板到实例：模板层 ItemInfo

`ItemInfo` 是 System.db 里的静态模板。固有属性不直接存字段，而是通过 `ItemInfoStat` 列表（`ItemStats`）挂接，并在加载/修改后折叠进内存聚合 `Stats`：

```csharp
// LibraryCore/SystemModels/ItemInfo.cs:485-490
public void StatsChanged()
{
    Stats.Clear();
    foreach (ItemInfoStat stat in ItemStats)
        Stats[stat.Stat] += stat.Amount;
}
```

`OnLoaded()` 时自动调用（`ItemInfo.cs:479-483`）。新建物品的默认值（`ItemInfo.cs:462-478`）：`StackSize=1`、`RequiredGender=None`、`RequiredClass=All`、`SellRate=0.5M`、`CanRepair/CanSell/CanStore/CanTrade/CanDrop=true`。

#### ItemInfo 全字段语义

| 字段（行号） | 类型 | 语义与取值 |
|---|---|---|
| `ItemName`（ItemInfo.cs:10-23） | string，`[IsIdentity]` | 物品名，模板唯一标识之一；客户端本地化用它查 `translations/db_names.json` |
| `ItemType`（ItemInfo.cs:25-38） | ItemType | 物品大类，决定行为分支（见下表"ItemType 枚举"） |
| `RequiredClass`（ItemInfo.cs:40-53） | RequiredClass（Flags） | 允许使用的职业位掩码；判定用 `(info.RequiredClass & RequiredClass.Warrior) != RequiredClass.Warrior`（PlayerObject.cs:7306） |
| `RequiredGender`（ItemInfo.cs:55-68） | RequiredGender（Flags） | Male=1/Female=2/None=3（两者皆可）；判定见 PlayerObject.cs:7293-7300 |
| `RequiredType`（ItemInfo.cs:70-83） | RequiredType | 需求的**维度**（等级/属性上限/转生等），配合 `RequiredAmount` 使用（见"佩带条件"节） |
| `RequiredAmount`（ItemInfo.cs:85-98） | int | 需求阈值。注意它还是多个公式的输入：碎片费 `RequiredAmount*10000/9`（UserItem.cs:665）、Common 碎片条件 `>15`（UserItem.cs:627）、饰品升级经验合并（PlayerObject.cs:12341）、打造物品部件判定（PlayerObject.cs:4597-4598） |
| `Shape`（ItemInfo.cs:100-113） | int | **多义子形状**，按 ItemType/ItemEffect 解释：RefineSpecial Shape==1 为精炼特殊件（PlayerObject.cs:12372）、Shape==5 为大师精炼特殊件（GodotClient/Controls/DXItemCell.cs:970）；DarkStone 的 Shape 是图标起始偏移（DXItemCell.cs:289）；LootBox 的 Shape=LootBoxInfo.Index（PlayerObject.cs:17513）；MagicRing 的 Shape=MagicInfo 序号（PlayerObject.cs:2276）；打造宝石的 Shape=提供的词条条数 statCount（PlayerObject.cs:13623）；修理油 Shape 区分 6=战神油/12=饰品油等（PlayerObject.cs:6500, 6585-6602） |
| `Effect`（ItemInfo.cs:115-131） | ItemEffect | 已废弃：`[Obsolete("Use ItemEffect instead")]`，setter 顺带写 `ItemEffect` |
| `ItemEffect`（ItemInfo.cs:133-146） | ItemEffect | 物品效果子类型（矿石/药剂/宝石槽/鱼竿/武器模板等，见 Enum.cs:1788-1857）。大量逻辑以它为开关：`ItemEffect.BlackIronOre` 才能当精炼矿石（PlayerObject.cs:12296），`WeaponTemplate` 是打造模板（PlayerObject.cs:13584） |
| `ExteriorEffect`（ItemInfo.cs:148-161） | ExteriorEffect | 外观特效（翅膀/光环等，Enum.cs:1728-1786），纯表现层 |
| `Image`（ItemInfo.cs:163-176） | int | 图标/贴图索引（客户端 `info.Image` 直接作绘制索引，DXItemCell.cs:256） |
| `Durability`（ItemInfo.cs:178-191） | int | 基准耐久（=掉落时的 MaxDurability，SEnvir.cs:2154）。非装备语义复用：书本 Durability=学习成功率 `Random.Next(100) >= item.CurrentDurability`（PlayerObject.cs:7189）、肉/矿的纯度值（SEnvir.cs:2210-2215）、部分使用型物品的使用间隔毫秒（PlayerObject.cs:6634, 7136） |
| `Price`（ItemInfo.cs:193-206） | int | 基准价格（商店/修理/出售公式的基数） |
| `Weight`（ItemInfo.cs:208-221） | int | 重量（负重判定用，PlayerObject.cs:8628-8638） |
| `StackSize`（ItemInfo.cs:223-236） | int | 最大堆叠数；1=不可堆叠 |
| `StartItem`（ItemInfo.cs:238-251） | bool | 是否新手起始物品 |
| `SellRate`（ItemInfo.cs:253-266） | decimal | 出售折价率，默认 0.5（NPC 回收价 = Price×SellRate，UserItem.cs:607） |
| `CanRepair`（ItemInfo.cs:268-281） | bool | 能否修理（NPCRepair 入口检查，PlayerObject.cs:11717） |
| `CanSell`（ItemInfo.cs:283-296） | bool | 能否卖给 NPC（客户端过滤 DXItemCell/InventoryDialog.cs:327） |
| `CanStore`（ItemInfo.cs:298-311） | bool | 能否放入仓库 |
| `CanTrade`（ItemInfo.cs:313-326） | bool | 能否交易/邮寄/寄售（PlayerObject.cs:3841、ConsignmentDialog.cs:218） |
| `CanDrop`（ItemInfo.cs:328-341） | bool | 能否主动丢弃；`CanDeathDrop`（ItemInfo.cs:343-356）= 死亡时能否掉落 |
| `Description`（ItemInfo.cs:358-371） | string | 物品描述文本 |
| `Rarity`（ItemInfo.cs:373-386） | Rarity | Common/Superior/Elite；影响随机词条概率 ×2（SEnvir.cs:2160-2161）、碎片公式（UserItem.cs:651+）、饰品精炼成功率 30→40（PlayerObject.cs:11510-11513）、打造费用（PlayerObject.cs:13596-13607） |
| `CanAutoPot`（ItemInfo.cs:388-401） | bool | 能否被自动喝药引用（AutoPotionLink） |
| `BuffIcon`（ItemInfo.cs:403-416） | int | 作为 Buff 来源时的图标索引（GodotClient/Controls/BuffDialog.cs:157） |
| `PartCount`（ItemInfo.cs:418-431） | int | 碎片合成所需部件数（部件拼回原件） |
| `Set`（ItemInfo.cs:433-447） | SetInfo | 所属套装（可空） |
| `ItemStats`（ItemInfo.cs:449-450） | DBBindingList\<ItemInfoStat\> | 模板固有属性列表 |
| `Drops`（ItemInfo.cs:452-454） | DBBindingList\<DropInfo\> | 该物品的掉落来源（FortuneChecker 用，GodotClient/Controls/FortuneCheckerDialog.cs:88-89） |
| `ShouldLinkInfo`（ItemInfo.cs:456-458） | 计算属性 | `StackSize > 1 || ItemType == Consumable || Scroll` —— 可堆叠/消耗品聊天链接时只带 InfoIndex |
| `Stats`（ItemInfo.cs:460） | Stats（内存） | 固有属性聚合，`OnLoaded` 折叠，不落库 |

**任务点名但本仓库不存在的字段**：`Unique`、`Slots`、`BindMode` —— `ItemInfo.cs` 全文（本次已读 1-497 行）无这三个属性，全库 `grep BindMode` 0 命中。对应概念的实际实现：唯一性 → 无专属字段（用 `Rarity`/`StackSize=1` 约定）；插槽 → 实例层 `UserItem.Sockets`（UserItem.cs:317-318，UserItemSocket 表）；绑定 → `UserItemFlags.Bound`（Enum.cs:1894）。`WeaponCraftStatsInfo` 作为**类名**不存在，存在的是文件 `WeaponCraftStatsInfo.cs` 里的**类** `WeaponCraftStatInfo`（WeaponCraftStatsInfo.cs:5）。

#### ItemType 枚举（Enum.cs:410-461）

| 值 | 名称 | 说明 |
|---|---|---|
| 0 | Nothing | 占位 |
| 1 | Consumable | 消耗品（喝药逻辑走 ItemUse 的 Shape 分支） |
| 2 | Weapon | 武器（可精炼/打造/武器经验） |
| 3 | Armour | 防具 |
| 4 | Torch | 火把（光照，持续磨损 Config.TorchRate） |
| 5 | Helmet | 头盔 |
| 6 | Necklace | 项链（饰品升级/精炼/重置） |
| 7 | Bracelet | 手镯（同上） |
| 8 | Ring | 戒指（同上，另有 WeddingRing 用途） |
| 9 | Shoes | 鞋 |
| 10 | Poison | 毒药（Amulet 槽，重量不乘 Count） |
| 11 | Amulet | 护身符（Amulet 槽可放 DarkStone） |
| 12 | Meat | 肉（Durability=品质，`Random.Next(D*2)+2000`） |
| 13 | Ore | 矿石（Durability=纯度，`Random.Next(D*3)+3000`） |
| 14 | Book | 技能书（Durability=成功率 5~100） |
| 15 | Scroll | 卷轴 |
| 16 | DarkStone | 黑暗石（Amulet 槽元素石，图标用 Shape 偏移） |
| 17 | RefineSpecial | 精炼特殊件（Shape=1 普通 / 5 大师，提供 MaxRefineChance） |
| 18 | HorseArmour | 马铠（需先有马） |
| 19 | Flower | 花 |
| 20-23 | CompanionFood/Bag/Head/Back | 伙伴食物/背包/头饰/背饰 |
| 24 | System | 系统物品 |
| 25 | ItemPart | 物品部件（词条 `Stat.ItemIndex` 指向原件，MonsterObject.cs:2786） |
| 26 | Emblem | 徽章 |
| 27 | Shield | 盾（可随机词条 UpgradeShield） |
| 28 | Costume | 时装 |
| 29-33 | Hook/Float/Bait/Finder/Reel | 钓鱼五件套（需手持 FishingRod） |
| 34 | Currency | 货币物品（CurrencyInfo.DropItem） |
| 35 | Bundle | 礼包（打开逐格领） |
| 36 | LootBox | 战利品箱（Shape=LootBoxInfo.Index，CurrentDurability 当已揭示位掩码，PlayerObject.cs:17522-17536） |
| 37 | SocketGem | 宝石（可镶嵌/可随机词条 UpgradeSocketGem） |

#### 相关 Flags/枚举速览

```csharp
// LibraryCore/Enum.cs:64-79
[Flags]
public enum RequiredClass : byte
{
    None = 0,
    Warrior = 1,
    Wizard = 2,
    Taoist = 4,
    Assassin = 8,
    [Description("Warrior, Wizard, Taoist")]
    WarWizTao = Warrior | Wizard | Taoist,
    [Description("Wizard, Taoist")]
    WizTao = Wizard | Taoist,
    [Description("Warrior, Assassin")]
    AssWar = Warrior | Assassin,
    All = WarWizTao | Assassin
}

// LibraryCore/Enum.cs:81-87
[Flags]
public enum RequiredGender : byte
{
    Male = 1,
    Female = 2,
    None = Male | Female
}

// LibraryCore/Enum.cs:317-334
public enum RequiredType : byte
{
    Level, MaxLevel, AC, MR, DC, MC, SC, Health, Mana, Accuracy, Agility,
    CompanionLevel, MaxCompanionLevel, RebirthLevel, MaxRebirthLevel,
}

// LibraryCore/Enum.cs:336-341
public enum Rarity : byte { Common, Superior, Elite }

// LibraryCore/Enum.cs:1888-1902
[Flags]
public enum UserItemFlags
{
    None = 0,
    Locked = 1,        // 客户端锁定（操作中/精炼后）
    Bound = 2,         // 绑定：不可交易/邮寄/寄售
    Worthless = 4,     // 无价值：出售价 0
    Refinable = 8,     // 已攒满经验，可精炼/升级
    Expirable = 16,    // 时效物品（ExpireTime 倒计时）
    QuestItem = 32,
    GameMaster = 64,
    Marriage = 128,    // 结婚戒指（禁修理/精炼/出售）
    NonRefinable = 256 // 永不可精炼
}

// LibraryCore/Stat.cs:897-904
public enum StatSource
{
    None,
    Added,        // 掉落随机词条 / 饰品精炼
    Refine,       // 武器精炼词条（可被 Reset 转换）
    Enhancement, //Temporary Buff!?
    Other,
}
```

### 2. ItemInfoStat：模板加成机制

`ItemInfoStat`（ItemInfoStat.cs:5-55）只有三个字段：`Item`（[IsIdentity]，Association "ItemStats"）、`Stat`（[IsIdentity]）、`Amount`。`Stat` 取 `LibraryCore/Stat.cs:507` 起的 Stat 枚举（MinDC/MaxDC/Health/Mana/FireAttack/…/SaleBonus20/WeaponElement/ItemIndex 等数百项，含 `Duration=10000` 特殊段 Stat.cs:893-894）。

- **同一 (Item, Stat) 可多条**：折叠时 `Stats[stat.Stat] += stat.Amount` 累加（ItemInfo.cs:489）。
- **进入玩家面板的路径**：`PlayerObject.RefreshStats` 里 `Stats.Add(item.Info.Stats, item.Info.ItemType != ItemType.Weapon)`（PlayerObject.cs:2251）—— 注意第二参数：武器外装备按"百分比类词条不折算"的规则并入（与 2252 行的实例词条并入方式一致）。
- **特殊 Stat**：`Stat.SaleBonus5/10/15/20` 影响出售价倍率（UserItem.cs:598-605）；`Stat.MaxRefineChance` 是 RefineSpecial 提供的精炼率加成（PlayerObject.cs:12377）；`Stat.ItemIndex` 用于 ItemPart 指路；`Stat.WeaponElement`（1=火…7=幻影）标记武器当前元素（UserItem.MergeRefineElements，UserItem.cs:790-820）。

### 3. 随机属性生成（掉落词条）

入口在 `SEnvir.CreateDropItem(ItemInfo info, int chance = 15)`（SEnvir.cs:2149-2228）：

```csharp
// ServerLibrary/Envir/SEnvir.cs:2149-2196（节选）
public static UserItem CreateDropItem(ItemInfo info, int chance = 15)
{
    UserItem item = UserItemList.CreateNewObject();

    item.Info = info;
    item.MaxDurability = info.Durability;

    ItemSetup(item);   // Bundle/LootBox 特殊初始化

    item.Colour = Color.FromArgb(Random.Next(256), Random.Next(256), Random.Next(256));

    if (info.Rarity != Rarity.Common)
        chance *= 2;                       // 非常规稀有度概率翻倍

    if (Random.Next(chance) == 0)          // 默认 1/15；Superior/Elite 为 1/30
    {
        switch (info.ItemType)
        {
            case ItemType.Weapon:    UpgradeWeapon(item);    break;
            case ItemType.Shield:    UpgradeShield(item);    break;
            case ItemType.Armour:    UpgradeArmour(item);    break;
            case ItemType.Helmet:    UpgradeHelmet(item);    break;
            case ItemType.Necklace:  UpgradeNecklace(item);  break;
            case ItemType.Bracelet:  UpgradeBracelet(item);  break;
            case ItemType.Ring:      UpgradeRing(item);      break;
            case ItemType.Shoes:     UpgradeShoes(item);     break;
            case ItemType.SocketGem: UpgradeSocketGem(item); break;
        }
        item.StatsChanged();
    }
    ...
```

初始耐久（SEnvir.cs:2198-2225）：装备类（Weapon/Shield/Armour/Helmet/Necklace/Bracelet/Ring/Shoes）= `Min(Random.Next(info.Durability) + 1000, MaxDurability)`；Meat=`Random.Next(D*2)+2000`；Ore=`Random.Next(D*3)+3000`；Book=`Random.Next(96)+5`；SocketGem=`Random.Next(MaxDurability)`；其余=满值 `info.Durability`。

`Random.Next(chance) == 0` 是"命中才生成词条"，因此词条物品本身就稀有；每个 `Upgrade*` 内部**各词条独立再掷骰**，可能一无所获也可能多项叠加。

#### UpgradeWeapon（SEnvir.cs:2297-2370，照抄）

```csharp
public static void UpgradeWeapon(UserItem item)
{
    if (Random.Next(5) == 0)                       // 20%: MaxDC
    {
        int value = 1;

        if (Random.Next(50) == 0)                  // 2%: +1
            value += 1;

        if (Random.Next(250) == 0)                 // 0.4%: 再 +1
            value += 1;

        item.AddStat(Stat.MaxDC, value, StatSource.Added);
    }

    if (Random.Next(5) == 0)                       // 20%: MaxMC/MaxSC（跟随模板取向）
    {
        int value = 1;

        if (Random.Next(50) == 0)
            value += 1;

        if (Random.Next(250) == 0)
            value += 1;

        //No perticular Magic Power
        if (item.Info.Stats[Stat.MinMC] == 0 && item.Info.Stats[Stat.MaxMC] == 0 && item.Info.Stats[Stat.MinSC] == 0 && item.Info.Stats[Stat.MaxSC] == 0)
        {
            item.AddStat(Stat.MaxMC, value, StatSource.Added);
            item.AddStat(Stat.MaxSC, value, StatSource.Added);
        }

        if (item.Info.Stats[Stat.MinMC] > 0 || item.Info.Stats[Stat.MaxMC] > 0)
            item.AddStat(Stat.MaxMC, value, StatSource.Added);

        if (item.Info.Stats[Stat.MinSC] > 0 || item.Info.Stats[Stat.MaxSC] > 0)
            item.AddStat(Stat.MaxSC, value, StatSource.Added);

    }

    if (Random.Next(5) == 0)                       // 20%: Accuracy（1/250、1/1250 追加）
    {
        int value = 1;

        if (Random.Next(250) == 0)
            value += 1;

        if (Random.Next(1250) == 0)
            value += 1;

        item.AddStat(Stat.Accuracy, value, StatSource.Added);
    }

    List<Stat> Elements = new List<Stat>
    {
        Stat.FireAttack, Stat.IceAttack, Stat.LightningAttack, Stat.WindAttack,
        Stat.HolyAttack, Stat.DarkAttack,
        Stat.PhantomAttack,
    };


    if (Random.Next(3) == 0)                       // 33%: 随机元素攻击
    {
        int value = 1;

        if (Random.Next(5) == 0)
            value += 1;

        if (Random.Next(25) == 0)
            value += 1;

        item.AddStat(Elements[Random.Next(Elements.Count)], value, StatSource.Added);
    }
}
```

#### 其余部位词条池概率总表（均 `StatSource.Added`）

| 函数（SEnvir.cs 行号） | 词条与概率 |
|---|---|
| `UpgradeShield`（2371-2525） | 10% DCPercent；10% MCPercent+SCPercent；10% BlockChance；10% EvasionChance；10% PoisonResistance（值均为 1，1/50、1/250 各可 +1）；10% 元素抗性 ±2（Fire/Ice/Lightning/Wind/Holy/Dark/Phantom/Physical，先 +2 再 50% 另一系 −2，1/45、1/60 可再叠一轮，else 分支 10% 单独 −2） |
| `UpgradeArmour`（2526-2639） | 50% MaxAC +1（1/15、1/150 追加）；50% MaxMR +1（同上）；10% 元素抗性 ±2（同 Shield 结构） |
| `UpgradeHelmet`（2640-2753） | 20% MaxAC；20% MaxMR（值 1，1/25、1/250 追加）；10% 元素抗性 ±1 |
| `UpgradeNecklace`（2754-2842） | 20% MaxDC；20% MaxMC/MaxSC（同武器取向逻辑）；20% Accuracy；20% Agility；33% 攻击元素 +1（1/5、1/25 追加 1 点，且可不同元素叠加） |
| `UpgradeBracelet`（2843-3022） | 20% MaxAC（1/15、1/150）；20% MaxMR；20% MaxDC（1/25、1/250）；20% MaxMC/MaxSC；20% Accuracy；20% Agility；10% 元素抗性 ±1（1/30、1/40 深度三层） |
| `UpgradeRing`（3023-3096） | 20% MaxDC；20% MaxMC/MaxSC；33% PickUpRadius（1/15、1/150 追加）；33% 攻击元素 +1×(1/5、1/25) |
| `UpgradeShoes`（3097-3223） | 20% MaxAC（1/15、1/150）；20% MaxMR；20%（1/25、1/250）词条（本次读取范围 3125 行后截断，词条种类未完读；结构与其余部位一致） |

要点：
- 词条值域几乎都是 1~3（两级追加概率 1/50、1/250 或 1/25、1/250）；抗性类为 ±1/±2 且**可能出负数**（`AddStat(element, -2, ...)`，SEnvir.cs:2461）。
- 元素池分两套：攻击系（Fire/Ice/Lightning/Wind/Holy/Dark/Phantom Attack）与抗性系（8 系 Resistance 含 Physical）。
- `UserItem.AddStat(stat, amount, source)`（UserItem.cs:520-539）会**合并同 Stat 同 Source 的既有词条**，否则新建 `UserItemStat` 挂到 `SEnvir.UserItemStatsList`。
- ItemPart 掉落时额外写 `AddStat(Stat.ItemIndex, drop.Item.Index, StatSource.Added)` 并 `SetTemporary(true)`（MonsterObject.cs:2786-2789）。

### 4. 武器精炼（Refine）与打造

#### 4.1 武器经验与 Refinable 标志

武器通过角色打怪经验练级（PlayerObject.cs:2067-2081）：

```csharp
// ServerLibrary/Models/PlayerObject.cs:2067-2081
UserItem weapon = Equipment[(int)EquipmentSlot.Weapon];

if (weapon != null && weapon.Info.ItemEffect != ItemEffect.PickAxe && (weapon.Flags & UserItemFlags.Refinable) != UserItemFlags.Refinable && (weapon.Flags & UserItemFlags.NonRefinable) != UserItemFlags.NonRefinable && weapon.Level < Globals.WeaponExperienceList.Count && rateEffected)
{
    weapon.Experience += amount / 10;                    // 武器经验 = 角色经验的 1/10

    if (weapon.Experience >= Globals.WeaponExperienceList[weapon.Level])
    {
        weapon.Experience = 0;
        weapon.Level++;

        if (weapon.Level < Globals.WeaponExperienceList.Count)
            weapon.Flags |= UserItemFlags.Refinable;     // 满级才可精炼
    }
}
```

`WeaponExperienceList`（Globals.cs:257-278）：索引 1~16 级依次 300000, 350000, …, 750000(10), 800000, 850000, 900000, 1000000, 1300000, 2000000。饰品用 `AccessoryExperienceList`（Globals.cs:280-294）：0, 5, 20, 80, 350, 1500, 6200, 26500, 114000, 490000, 2090000（满 10 级）。

#### 4.2 提交精炼：NPCRefine（PlayerObject.cs:12198-12532）

前置：页面 `NPCDialogType.Refine`；品质白名单 Rush/Quick/Standard/Careful/Precise（12210-12223）、类型白名单 Durability/DC/SpellPower/7 元素（12225-12243），违规直接封号（BanReason="Attempted to Exploit refine, Weapon Refine Quality/Type"，12219-12222）。固定费用 `RefineCost = 50000`（12253）。武器必须佩带中且带 `Refinable`、无 `NonRefinable`（12255-12259）。

材料三组（各限 5/3/1 个链接，12249-12251）：
- **矿石**：必须 `ItemEffect == BlackIronOre`，累计 `ore += item.CurrentDurability`（纯度）——12296-12300；
- **首饰**：Necklace/Bracelet/Ring，`items += item.Info.RequiredAmount`，非常规稀有度 `quality++`（12331-12344）；
- **特殊件**：`ItemType==RefineSpecial && Shape==1`，`special += Info.Stats[Stat.MaxRefineChance]`（12370-12377）。

成功率公式（照抄 PlayerObject.cs:12381-12419）：

```csharp
/*
 * BaseChance  90% - Weapon Level
 * Max Chance  -5% | 0% | +5% | +10% | +20% = (Rush | Quick | Standard | Careful | Precise)  
 * 5 Ore 1% per 2 Dura Max
 * Items 1% per 6 Item Levels, 5% for Quality
 * Base Chance = 60% -Weapon Level  * 5%
 */

int maxChance = 90 - weapon.Level + special;
int chance = 60 - weapon.Level * 5;

switch (p.RefineQuality)
{
    case RefineQuality.Rush:     maxChance -= 5;  break;
    case RefineQuality.Quick:                    break;
    case RefineQuality.Standard: maxChance += 5;  break;
    case RefineQuality.Careful:  maxChance += 10; break;
    case RefineQuality.Precise:  maxChance += 20; break;
    default:
        return;
}

//Special + Max Chance

chance += ore / 2000;
chance += items / 6;
chance += quality * 25;

maxChance = Math.Min(100, maxChance);
chance = Math.Min(maxChance, chance);
```

即：实际上限 `min(100, 90 − 武器等级 + special + 品质修正)`；基础 `60 − 武器等级×5 + 矿石纯度/2000 + 首饰需求等级合计/6 + 非常规件数×25`，最终取两者较小。提交后武器从装备栏摘除，材料全部销毁，创建 `RefineInfo`（12517-12525）：

```csharp
RefineInfo info = SEnvir.RefineInfoList.CreateNewObject();
info.Character = Character;
info.Weapon = weapon;
info.Chance = chance;
info.MaxChance = maxChance;
info.Quality = p.RefineQuality;
info.Type = p.RefineType;
info.RetrieveTime = SEnvir.Now + Globals.RefineTimes[p.RefineQuality];
```

等待时长（Globals.cs:318-325）：Rush=1 分钟、Quick=30 分钟、Standard=1 小时、Careful=6 小时、Precise=1 天。

#### 4.3 取回与结算：NPCRefineRetrieve（PlayerObject.cs:12533-12676）

到点后在 RefineRetrieve 页面取回，判定一行：`if (SEnvir.Random.Next(100) < info.Chance)`（12557）。成功效果（12559-12655）：

- `RefineType.Durability` → `weapon.MaxDurability += 2000`（12561-12563）；
- `DC` → `AddStat(Stat.MaxDC, 1, StatSource.Refine)`；`SpellPower` 按模板取向给 MaxMC/MaxSC（同随机词条的取向逻辑，12567-12579）；
- 七元素 → `AddStat(Stat.XxxAttack, 1, Refine)` 且把武器元素切到对应系：`AddStat(Stat.WeaponElement, n - weapon.Stats[Stat.WeaponElement], StatSource.Refine)`，n=1火/2冰/3雷/4风/5圣/6暗/7幻（12580-12607）；
- `RefineType.Reset` → `weapon.Level=1`、`ResetCoolDown=Now+14天`；先把全部 Refine 元素攻击词条合并到当前元素（`MergeRefineElements`，UserItem.cs:790-820），再把每条 Refine 词条按 `amount/5` 折算成 `StatSource.Enhancement` 词条，并设上限：MaxDC/MC/SC 与 7 系元素攻击 ≤200，EvasionChance/BlockChance ≤10（12608-12654）。这是"满炼武器转永久强化"的通道。

无论成败：`weapon.Flags &= ~UserItemFlags.Refinable; weapon.Flags |= UserItemFlags.Locked;`（12665-12667），然后 `GainItem(weapon)` 归还。

`NPCResetWeapon`（12677-12702）走同一条流水线：直接创建 `Type=RefineType.Reset, Quality=Precise, MaxChance=100` 的 RefineInfo（12688-12696）。

武器还有两条 Luck 调整入口（8824-8835、8866-8875）：`AddStat(Stat.Luck, ±1, StatSource.Enhancement)`，随 Shape 对应的修理油/道具触发。

大师精炼 `NPCMasterRefine`（12796 起）用 Fragment1/2/3 + RefinementStone + Shape==5 的特殊件（GodotClient/Controls/DXItemCell.cs:970），类型白名单 12809-12826，无 Durability/Reset。

#### 4.4 饰品升级/精炼/重置（PlayerObject.cs:10541-10981, 11343-11660）

- `NPCAccessoryLevelUp`：把同 Info 的材料饰品喂给目标，经验合并规则：目标非常规稀有度或已 1 级时 `+link.Count*5` 否则 `+link.Count`；材料等级 >1 且目标 Common 时 −4；再按 `AccessoryExperienceList` 逐级折算材料等级，最后并入材料自身经验（10639-10654）。满级 `Level++` 并打 `Refinable`（10658-10663）。
- `NPCAccessoryUpgrade`（10693-10882）：要求 `Refinable` 且非 `NonRefinable`、类型 ∈ {Ring, Bracelet, Necklace}（10728-10738）。按 `p.RefineType` **必定** +1（DC/SpellPower/Health+10/Mana+10/DCPercent/SPPercent/HealthPercent/ManaPercent/7 元素/AC(Min+Max)/MR(Min+Max)/Accuracy/Agility，10743-10860，词条来源 `StatSource.Refine`），非法值封号（10861-10865）。完成后清除 `Refinable`（10868），若经验又满则升级回 `Refinable`（10873-10879）。
- `NPCAccessoryRefine`（11343-11660）：成功率公式（照抄 11508-11524）：

```csharp
int chance = 100 - (oretargetItem.CurrentDurability / 1000);   // 矿石纯度越高 chance 越小（越稳）
int success = 30;
if (targetItem.Info.Rarity != Rarity.Common)
{
    success = 40;
}
if (SEnvir.Random.Next(chance) < success)
```

  成功时额外 `if (SEnvir.Random.Next(chance) == 0) amount = 2;`（11521-11524），词条走 `StatSource.Added`（11525-11647）；失败则**目标饰品销毁**（11654-11658：`targetArray[targetItem.Slot] = null; RemoveItem(targetItem)`）。注意 `Random.Next(chance) < success` 在 chance < success 时必成功（纯度 ≥70000 才可能让 chance<30，常规数值下接近 30%~40% 成功率）。
- `NPCAccessoryReset`（10883-10981）：降级回收经验（10967-10975）。

#### 4.5 武器打造：NPCWeaponCraft（PlayerObject.cs:13553-13922）

模板位放 `ItemEffect==WeaponTemplate`（生成职业白板武器）或普通武器（重铸：等级清 1、清空非 Enhancement 词条，13888-13899）。六个宝石槽位各需对应 `ItemEffect.YellowSlot/BlueSlot/RedSlot/PurpleSlot/GreenSlot/GreySlot` 且 `Count==1`，每颗宝石按 `Info.Shape` 提供 `statCount` 条词条（13613-13708）。费用按稀有度取 `Globals.*CraftWeaponPercentCost`（13592-13608）。

词条抽取（照抄 13868-13917）：

```csharp
int total = 0;

foreach (WeaponCraftStatInfo stat in SEnvir.WeaponCraftStatInfoList.Binding)
{
    if ((stat.RequiredClass & p.Class) != p.Class) continue;

    total += stat.Weight;
}
...
for (int i = 0; i < statCount; i++)
{
    int value = SEnvir.Random.Next(total);

    foreach (WeaponCraftStatInfo stat in SEnvir.WeaponCraftStatInfoList.Binding)
    {
        if ((stat.RequiredClass & p.Class) != p.Class) continue;

        value -= stat.Weight;

        if (value >= 0) continue;

        item.AddStat(stat.Stat, SEnvir.Random.Next(stat.MinValue, stat.MaxValue + 1), StatSource.Added);
        break;
    }
}

item.StatsChanged();
```

词条池 `WeaponCraftStatInfo`（WeaponCraftStatsInfo.cs:5-86）：`RequiredClass`+`Stat`+`Weight` 为 Identity，`MinValue/MaxValue` 决定均匀取值区间。即**按权重有放回抽 statCount 次**，每次数值 `Uniform[MinValue, MaxValue]`。

### 5. 耐久 / 修理

#### 5.1 磨损：DamageItem（PlayerObject.cs:8645-8696，照抄核心）

```csharp
public bool DamageItem(GridType grid, int slot, int rate = 1, bool delayStats = false)
{
    ...
    if (item == null || item.Info.Durability == 0 || item.CurrentDurability == 0) return false;

    if ((item.Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage) return false;

    switch (item.Info.ItemType)
    {
        case ItemType.Nothing:
        case ItemType.Consumable:
        case ItemType.Poison:
        case ItemType.Amulet:
        case ItemType.Scroll:
            return false;
        case ItemType.Weapon:
            if (SEnvir.Random.Next(Stats[Stat.Strength]) > 0) return false;   // 武器：1/Strength 概率掉
            break;
        default:
            if (SEnvir.Random.Next(3) == 0 && SEnvir.Random.Next(Stats[Stat.Strength]) > 0) return false;  // 其他：1/3 × 1/Strength
            break;
    }

    item.CurrentDurability = Math.Max(0, item.CurrentDurability - rate);

    Enqueue(new S.ItemDurability { GridType = grid, Slot = slot, CurrentDurability = item.CurrentDurability });

    if (item.CurrentDurability == 0)
    {
        SendShapeUpdate();
        RefreshStats();
        return true;
    }
    return false;
}
```

磨损调用点（rate 值）：

| 场景 | 位置 | rate |
|---|---|---|
| 近战命中 | PlayerObject.cs:15372 | `SEnvir.Random.Next(2) + 1`（1~2） |
| 远程(手里剑)攻击 | PlayerObject.cs:15202 | 1 |
| 被击中（全身装备，跳过 Amulet/Poison/Torch） | PlayerObject.cs:15829-15841 | `Random.Next(2)+1`，`delayStats=true` |
| 红名玩家死亡（全身） | PlayerObject.cs:16403-16409 | `item.Info.Durability / 10` |
| 火把（周期） | PlayerObject.cs:444 | `Config.TorchRate` |
| 挖矿挥镐 | PlayerObject.cs:14991 | 4 |
| 钓鱼（Hook/Float/Finder/Reel） | PlayerObject.cs:14272-14275 | 4 |
| 钓鱼失败 | PlayerObject.cs:14490 | 100 |
| 黑暗石（Amulet 槽 DarkStone，元素攻击消耗） | PlayerObject.cs:15315-15316 → DamageDarkStone 8697-8699 | 1（DamageDarkStone 且归零即销毁，8703-8706） |
| 召唤傀儡消耗黑暗石 | ServerLibrary/Models/Magics/Assassin/SummonPuppet.cs:63 | 10 |

**损坏判定**：`CurrentDurability == 0` 即"损坏"——该装备在 `RefreshStats` 中被跳过（`item.CurrentDurability == 0 && item.Info.Durability > 0` 则不计属性，PlayerObject.cs:2237），武器耐久 0 还会触发摘除逻辑（火把：PlayerObject.cs:446-450 直接消失；钓鱼武器 0：14492-14493）。

#### 5.2 修理费用公式（UserItem.cs:578-619，照抄）

```csharp
public long Price(long count)   // 出售价
{
    if (Info == null) return 0;
    if ((Flags & UserItemFlags.Worthless) == UserItemFlags.Worthless) return 0;

    decimal p = Info.Price;

    if (Info.Durability > 0)
    {
        decimal r = Info.Price / 2M / Info.Durability;

        p = MaxDurability * r;

        r = MaxDurability > 0 ? CurrentDurability / (decimal)MaxDurability : 0;

        p = Math.Floor(p / 2M + p / 2M * r + Info.Price / 2M);
    }

    p = p * (Stats.Count * 0.1M + 1M);          // 每条随机词条 +10% 价值

    if (Info.Stats[Stat.SaleBonus20] > 0 && Info.Stats[Stat.SaleBonus20] <= count)
        p *= 1.2M;
    else if (Info.Stats[Stat.SaleBonus15] > 0 && Info.Stats[Stat.SaleBonus15] <= count)
        p *= 1.15M;
    else if (Info.Stats[Stat.SaleBonus10] > 0 && Info.Stats[Stat.SaleBonus10] <= count)
        p *= 1.1M;
    else if (Info.Stats[Stat.SaleBonus5] > 0 && Info.Stats[Stat.SaleBonus5] <= count)
        p *= 1.05M;

    return (long)(p * count * Info.SellRate);
}
public long RepairCost(bool special)
{
    if (Info.Durability == 0 || CurrentDurability >= MaxDurability) return 0;

    int rate = special ? 2 : 1;                 // 特修费 = 普修 ×2

    decimal p = Math.Floor(MaxDurability * (Info.Price / 2M / Info.Durability) + Info.Price / 2M);
    p = p * (Stats.Count * 0.1M + 1M);

    return (long)(p * Count - Price(Count)) * rate;
}
```

注意 `Stats.Count` 是词条**条数**（KeyValuePair 数），不是数值和；`Price(Count)` 已含 SellRate，故修理费 = 满耐久估值 − 当前残值，可为负数时按 long 截断。

#### 5.3 NPC 修理：NPCRepair（PlayerObject.cs:11673-11852）

- 入口条件：`Info.CanRepair`、`Info.Durability>0`、非 Marriage、类型 ∈ {Weapon,Armour,Helmet,Necklace,Bracelet,Ring,Shoes,Shield}（11717-11734），且 NPC 页面 `Types` 包含该 ItemType（11741-11745）。
- 特修额外检查冷却：`p.Special && SEnvir.Now < item.SpecialRepairCoolDown` 拒绝（11746-11751）。
- 计费：`cost += item.RepairCost(p.Special)`（11755）；可用公会资金（需 `GuildPermission.FundsRepair`，11758-11784）。
- 结算（11810-11826）：

```csharp
if (p.Special)
{
    item.CurrentDurability = item.MaxDurability;          // 特修：回满且不损上限

    if (item.Info.ItemType != ItemType.Weapon)            // 武器无特修冷却
        item.SpecialRepairCoolDown = SEnvir.Now + Config.SpecialRepairDelay;
}
else
{
    item.MaxDurability = Math.Max(0, item.MaxDurability - (item.MaxDurability - item.CurrentDurability) / Globals.DuraLossRate);
    item.CurrentDurability = item.MaxDurability;          // 普修：回满但上限磨损 1/15
}
```

`Globals.DuraLossRate = 15`（Globals.cs:99）。

- 道具修理油（WarGod 油 case 6、Superior/Armour/Accessory 油 case 11/12 等，PlayerObject.cs:6500-6602）走 `SpecialRepair(EquipmentSlot slot)`（8881-8895）：免费特修对应槽位，同样跳过 Marriage，条件 `CurrentDurability >= MaxDurability || !CanRepair` 返回 false。

#### 5.4 碎片化（反向分解）

`CanFragment`（UserItem.cs:620-650）：非 Worthless/NonRefinable；Common 需 `RequiredAmount > 15`；类型限 7 大件。`FragmentCost`：Common/Superior = `RequiredAmount*10000/9` / `*10000/2`；Elite 按部位 250000(武/甲) 50000(盔) 150000(饰品) 30000(鞋)（651-715）。`FragmentCount`：Common/Superior = `Max(1, RequiredAmount/2+5)`；Elite 50/5/10/3（716-788）。客户端在 `GodotClient/Controls/NPCAdvancedPanels.cs:355-381` 有完整对应 UI。

### 6. 绑定（UserItemFlags）与时效（ExpireTime）

**绑定来源**（全部显式发放，无拾取绑定钩子）：

- 商店用 HuntGold 购买或收件人最高等级 <40：`flags |= UserItemFlags.Bound`（PlayerObject.cs:4350-4351、4497-4498）；
- 任务奖励 `reward.Bound`（PlayerObject.cs:3622-3623）；
- 里程碑奖励 `UserItemFlags.Bound | UserItemFlags.Worthless`（ServerLibrary/Models/PlayerObject.Milestone.cs:182、PlayerObject.cs:1330）；
- 单机开发模式发放（ServerLibrary/Envir/DevSinglePlayer.cs:95）。

**Bound 的拦截点**：邮寄/交易检查 `((item.Flags & UserItemFlags.Bound) == Bound || !item.Info.CanTrade)`（PlayerObject.cs:3841）；寄售同（3975 附近逻辑 + GodotClient/Controls/ConsignmentDialog.cs:216-218）；堆叠合并要求 Bound/Worthless/NonRefinable/Expirable 全一致（PlayerObject.cs:6176-6179、6291-6295；客户端镜像 DXItemCell.cs:393-397）。`Worthless` 使 `Price()` 直接 0（UserItem.cs:581）。

**时效物品**：`Expirable` 标志 + `UserItem.ExpireTime`（TimeSpan）。来源：商店 `StoreInfo.Duration>0` 时 `flags |= Expirable; ExpireTime = TimeSpan.FromSeconds(Duration)`（客户端预览 GodotClient/Controls/GameStoreDialog.cs:277-281；服务器在 CreateDropItem(ItemCheck) 中 `item.ExpireTime = check.ExpireTime`，SEnvir.cs:2103-2104/2135-2136）。

倒计时在 `ProcessItemExpire`（PlayerObject.cs:577-696），四个容器（Equipment/Inventory/Companion.Inventory/Companion.Equipment）统一处理：

```csharp
// ServerLibrary/Models/PlayerObject.cs:577-595（Equipment 段，其余容器同构）
public void ProcessItemExpire()
{
    if (ItemTime.AddSeconds(1) > SEnvir.Now) return;

    TimeSpan ticks = SEnvir.Now - ItemTime;
    ItemTime = SEnvir.Now;

    if (InSafeZone) return;                 // 安全区不倒计时！
    bool refresh = false;

    for (int i = 0; i < Equipment.Length; i++)
    {
        UserItem item = Equipment[i];
        if (item == null) continue;
        if ((item.Flags & UserItemFlags.Expirable) != UserItemFlags.Expirable) continue;

        item.ExpireTime -= ticks;

        if (item.ExpireTime > TimeSpan.Zero) continue;

        Connection.ReceiveChatWithObservers(con => string.Format(con.Language.Expired, item.Info.ItemName), MessageType.System);

        RemoveItem(item);
        Equipment[i] = null;
        item.Delete();
        ...
```

地面掉落物另有独立时效：`ItemObject.ExpireTime = SEnvir.Now + Config.DropDuration`，超时 `Despawn()`（ServerLibrary/Models/ItemObject.cs:16-30、189）。**不存在 `ExpireInfo` 类型**（全库无此名）；时效语义由 `UserItem.ExpireTime` + `ItemObject.ExpireTime` 两个 TimeSpan 承担。

### 7. UserItem / UserItemStat 存档字段

`UserItem`（ServerLibrary/DBModels/UserItem.cs:12-828），`[UserObject]`（进 Users.db）：

| 字段（行号） | 类型 | 语义 |
|---|---|---|
| `Info`（14-27） | ItemInfo | 模板引用（存 Index） |
| `CurrentDurability`（29-42） | int | 当前耐久/书本学习率/矿石纯度/肉品质（按 ItemType 复用）；LootBox 当已揭示槽位掩码 |
| `MaxDurability`（44-57） | int | 耐久上限（特修/精炼 Durability +2000 会抬高） |
| `Count`（59-72） | long | 堆叠数量 |
| `Slot`（74-87） | int | 所在格子（-1 未放置） |
| `Level`（89-102） | int | 物品等级（武器练级/饰品升级），OnCreated=1（485-492） |
| `Experience`（104-117） | decimal | 物品经验 |
| `Colour`（119-132） | Color | 染色/随机色（掉落生成时随机 RGB，SEnvir.cs:2158） |
| `SpecialRepairCoolDown`（134-147） | DateTime | 特修冷却截止 |
| `ResetCoolDown`（149-162） | DateTime | 武器 Reset 精炼冷却（+14 天，12610） |
| `UserTask`（166） | UserQuestTask | 非存档运行时字段（任务物品关联） |
| `Character/Account/Guild/Companion`（169-231） | 各 Info | 归属方（Association "Items"），**互斥**：`OnChanged` 里任一置位即清空其余全部归属 + Refine/Auction/Mail/Socket（354-458） |
| `Refine`（234-248） | RefineInfo | 该物品正挂在哪个精炼任务上 |
| `Auction`（250-264） | AuctionInfo | 挂拍卖行 |
| `Mail`（266-280） | MailInfo | 在邮件里 |
| `Flags`（282-295） | UserItemFlags | 见上文枚举 |
| `ExpireTime`（297-310） | TimeSpan | 时效剩余 |
| `AddedStats`（314-315） | DBBindingList\<UserItemStat\> | 附加词条 |
| `Sockets`（317-318） | DBBindingList\<UserItemSocket\> | 插槽；`Socket`（320-334）= 该物品作为宝石被镶嵌在哪个 UserItemSocket 里 |
| `Weight`（336-350） | 计算属性 | Poison/Amulet 不乘 Count，其余 `Info.Weight * Count` |
| `Stats`（352） | Stats | 内存聚合，OnLoaded 折叠（493-498） |

方法：`StatsChanged`（500-506）、`SetTemporary`（507-519，连同词条/插槽/宝石打临时标记）、`AddStat`（520-539，同 Stat+Source 合并）、`ToClientInfo`（541-576，冷却转剩余 TimeSpan、`AddedStats = new Stats(Stats, true)`）、`Price/RepairCost/CanFragment/FragmentCost/FragmentCount`（578-788）、`MergeRefineElements`（790-820，精炼 Reset 时合并元素）。`OnDeleted`（460-483）级联删词条与插槽并断开全部归属。

`UserItemStat`（ServerLibrary/DBModels/UserItemStat.cs:7-78）：`Item`（Association "AddedStats"）、`Stat`、`Amount`、`StatSource`。同一 (Stat, StatSource) 在 `UserItem.AddStat` 中合并为单条记录。

协议侧 `ClientUserItem`（LibraryCore/Globals.cs:454-791）字段与 UserItem 一一对应，另有客户端专用：`New`（新获得标记）、`NextSpecialRepair/NextReset`（`Complete()` 时由剩余 TimeSpan 换算的本地时钟，503-514）、`CanAccessoryUpgrade/CanFragment/FragmentCost/FragmentCount` 的客户端镜像（608-790，公式与服务端一致，RepairCost 返回 int 而 UserItem 返回 long，604-605）。`ClientUserItemSocket`（793-807）与 `ClientRefineInfo`（881+）。

### 8. 佩带条件判定与套装

#### 8.1 CanUseItem（PlayerObject.cs:7325-7411，核心 switch 照抄）

先查性别（7327-7337）、职业（7339-7357），再查 `RequiredType`：

```csharp
// ServerLibrary/Models/PlayerObject.cs:7360-7411
switch (item.Info.RequiredType)
{
    case RequiredType.Level:
        if (Level < item.Info.RequiredAmount && Stats[Stat.Rebirth] == 0) return false;
        break;                                                // 转生后无视等级需求
    case RequiredType.MaxLevel:
        if (Level > item.Info.RequiredAmount || Stats[Stat.Rebirth] > 0) return false;
        break;
    case RequiredType.CompanionLevel:
        if (Companion == null) return false;

        if (Companion.UserCompanion.Level < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.MaxCompanionLevel:
        if (Companion == null) return false;

        if (Companion.UserCompanion.Level > item.Info.RequiredAmount) return false;
        break;
    case RequiredType.AC:
        if (Stats[Stat.MaxAC] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.MR:
        if (Stats[Stat.MaxMR] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.DC:
        if (Stats[Stat.MaxDC] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.MC:
        if (Stats[Stat.MaxMC] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.SC:
        if (Stats[Stat.MaxSC] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.Health:
        if (Stats[Stat.Health] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.Mana:
        if (Stats[Stat.Mana] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.Accuracy:
        if (Stats[Stat.Accuracy] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.Agility:
        if (Stats[Stat.Agility] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.RebirthLevel:
        if (Stats[Stat.Rebirth] < item.Info.RequiredAmount) return false;
        break;
    case RequiredType.MaxRebirthLevel:
        if (Stats[Stat.Rebirth] > item.Info.RequiredAmount) return false;
        break;
}
```

`CanWearItem`（PlayerObject.cs:8618-8643）再叠加：`Functions.CorrectSlot(ItemType, slot)` 槽位匹配 + 负重（武器/火把/盾算 HandWeight；钓鱼件要求手持 FishingRod；其余算 WearWeight）。`CanStartWith`（7289-7324）是建号起始物品的性别/职业过滤。

#### 8.2 套装：SetInfo / SetInfoStat

`SetInfo`：`SetName`（Identity）+ `Items`（反向 Association "Set"，即 `ItemInfo.Set` 指向它）+ `SetStats`。`SetInfoStat` 五元组（Set/Stat/Amount/Class/Level 全 Identity），表示"等级 ≥ Level 且职业匹配时该词条生效"。

判定与生效（PlayerObject.cs:2233-2247 收集、2349-2375 生效，照抄生效段）：

```csharp
// 收集：装备中同 Set 的不同 ItemInfo（耐久为 0 的不算）
Dictionary<SetInfo, List<ItemInfo>> sets = new Dictionary<SetInfo, List<ItemInfo>>();

foreach (UserItem item in Equipment)
{
    if (item == null || (item.CurrentDurability == 0 && item.Info.Durability > 0)) continue;

    if (item.Info.Set != null)
    {
        List<ItemInfo> items;
        if (!sets.TryGetValue(item.Info.Set, out items))
            sets[item.Info.Set] = items = new List<ItemInfo>();

        if (!items.Contains(item.Info))
            items.Add(item.Info);
    }
    ...
```

```csharp
// 生效：必须集齐套装全部部件
foreach (KeyValuePair<SetInfo, List<ItemInfo>> pair in sets)
{
    if (pair.Key.Items.Count != pair.Value.Count) continue;   // 缺一件即无加成

    foreach (SetInfoStat stat in pair.Key.SetStats)
    {
        if (Level < stat.Level) continue;

        switch (Class)
        {
            case MirClass.Warrior:
                if ((stat.Class & RequiredClass.Warrior) != RequiredClass.Warrior) continue;
                break;
            ... // Wizard/Taoist/Assassin 同构
        }

        Stats[stat.Stat] += stat.Amount;
    }
}
```

词条并入的总量控制（RefreshStats 尾部，PlayerObject.cs:2392-2416）：元素抗性 ≤5、Comfort ≤20、AttackSpeed ≤15，随后 Health/Mana/DC/MC/SC 按 Percent 词条折算（`base += base * Percent / 100`）。

## 数据结构/协议细节

- **网络包**（LibraryCore/Network/ServerPackets.cs，本文按调用点引用）：物品增删改 `S.ItemChanged/S.ItemsChanged`；词条变化 `S.ItemStatsChanged`（NPCAccessoryUpgrade）、`S.ItemStatsRefreshed`（LootBox/StatExtractor 后全量刷新）、`S.ItemAcessoryRefined`（饰品精炼结果）；耐久 `S.ItemDurability`（PlayerObject.cs:8682-8687，随磨损即时下发）；物品等级 `S.ItemExperience`（PlayerObject.cs:10690、10881、10980）；精炼 `S.RefineList`（1106-1110、12531）、`S.NPCRefine/S.NPCRefineRetrieve/S.NPCRefinementStone/S.NPCMasterRefine/S.NPCAccessoryLevelUp/S.NPCAccessoryUpgrade/S.NPCAccessoryRefine/S.NPCWeaponCraft`。
- **ItemCheck**（ServerLibrary/Models/ItemCheck.cs:14-27）：发放物品的中间载体 `{Info, Count, Flags, ExpireTime, Stats}`；`SEnvir.CreateDropItem(ItemCheck)`（SEnvir.cs:2131-2148）把 check 的 Flags/ExpireTime 灌进新实例并做货币/经验物品的特殊堆叠。
- **归属互斥状态机**：`UserItem.OnChanged`（UserItem.cs:354-458）保证一件物品同一时刻只属于 Character/Account/Guild/Companion/Refine/Auction/Mail/Socket 之一——移植时必须等价实现，否则会出现"邮件里的武器同时被精炼"这类复制漏洞。
- **格子常量**（Globals.cs:296-302）：`InventorySize=48, EquipmentSize=22, CompanionInventorySize=30, CompanionEquipmentSize=4, EquipmentOffSet=1000, StorageSize=100, PartsStorageOffset=2000`。
- **Sockets/Gem 结构**：`UserItem.Sockets`（DBBindingList\<UserItemSocket\>，含 Slot 与 Gem(UserItem)），宝石自身 `Socket` 反指宿主孔；`ToClientInfo` 导出为 `List<ClientUserItemSocket>`（UserItem.cs:570-574）。元素加成：孔内宝石属性按 `Stats.Add(socketStats, 非武器)` 并入（PlayerObject.cs:2254-2261）。
- **武器元素归一**：RefreshStats 中若武器带元素，`Stats[ele] += item.Stats.GetWeaponElementValue() + item.Info.Stats.GetWeaponElementValue()`（PlayerObject.cs:2263-2272）。

## GodotClient 现状

以下结论均来自对 `GodotClient/` 的实际 grep/read（本次会话）：

| 功能 | 状态 | 证据（GodotClient 内路径:行号） |
|---|---|---|
| 物品格子/网格控件 | 已移植 | Controls/DXItemCell.cs:54-152（ItemGrid/Slot/Item 直读数组）、Controls/DXItemGrid.cs:80-84 |
| 物品图标/部件/货币绘制、Locked/不可用徽章 | 已移植 | DXItemCell.cs:242-278（ItemPart 经 `AddedStats[Stat.ItemIndex]` 换源图标 253；Locked 徽章 276；不可用徽章 278） |
| 堆叠合并规则（四标志+词条+ExpireTime 一致） | 已移植 | DXItemCell.cs:390-398 `CanMergeItems`；GameScene.cs:6733-6745、6791-6802 背包/伙伴合并 |
| 背包/仓库/装备/纸娃娃 | 已移植 | Controls/InventoryDialog.cs、StorageDialog.cs、CharacterDialog.cs:43-46、PaperDoll.cs、Controls/FishingDialog.cs:34 |
| 出售过滤（CanSell/Worthless/Locked/Marriage） | 已移植 | InventoryDialog.cs:322-327、372-377、213 |
| 修理/特修（含冷却与公会资金） | 已移植 | Controls/NPCRepairPanel.cs:91-111（回包处理特修冷却 `NextSpecialRepair`）、142-151（CanAcceptSource：CanRepair+耐久+冷却）、242（`RepairCost(_special)` 客户端计价）、213（SendNPCRepair）；NPCDialog.cs:101-104、131 |
| 精炼提交/取回/精炼石/大师精炼 | 已移植 | Controls/NPCAdvancedPanels.cs:433-441（Master Refine 五格）、NPCDialog.cs:150-152（SetRefineList/RemoveRefine）；Scripts/GameScene.cs:1117-1125、2695-2698、6318-6315（senders）；Network/ServerConnection.cs:503-512 |
| 饰品升级/精炼/重置 | 已移植 | NPCAdvancedPanels.cs:193-209（BuildItemFragment/AccessoryReset/WeaponCraft 分支）、675-684（BuildAccessoryReset）；DXItemCell.cs:983-984（AccessoryReset 投放条件含 `Level < Globals.AccessoryExperienceList.Count`）；GameScene.cs:6334-6338 |
| 武器打造 | 已移植 | NPCAdvancedPanels.cs:801-809（BuildWeaponCraft 六色槽）；DXItemCell.cs:971-977（WeaponCraftTemplate/六色宝石投放规则）；GameScene.cs:6347-6355（SendNPCWeaponCraft）、2721-2724 |
| 碎片化 | 已移植 | NPCAdvancedPanels.cs:355-381（费用合计 `FragmentCost()`、SendNPCFragment）；GameScene.cs:5681-5686（悬停显示碎片费/数量） |
| 物品悬停 tooltip（元数据/属性/需求/插槽/套装/时效/锁定/碎片/特修） | 部分移植（单色简化版，内容与顺序对齐旧版） | GameScene.cs:5481-5485（注释自述"新版单色标签无法复刻旧版每行颜色"）、5521-5526（Expirable/Locked 行）、5657-5658、5672-5673、5945-5981（AppendSetInfo 套装段）、5730-5735（武器元素归一显示） |
| 使用/穿戴校验客户端镜像 | 已移植（注释自述"移植自原版 CanUseItem/CanWearItem"） | GameScene.cs:7311-7312、7397-7400；DXItemCell.cs:711、982 |
| 耐久/经验/词条变更包处理 | 已移植 | GameScene.cs:7155-7190（OnItemStatsChanged/OnItemDurability 含归零提示/OnItemExperience）、7197 ApplyItemExperience；ServerConnection.cs:843-866（含 BufferPendingPackets 队列 354-359） |
| 时效物品倒计时（安全区暂停规则） | 部分移植（仅显示剩余时间，倒计时权威在服务端） | GameScene.cs:5521-5523、5657-5658 显示 `ExpireTime`；客户端无 ProcessItemExpire 等价物（服务端 S.ItemChanged 驱动消失，GameScene.cs:605-610 区域未读，此处仅按已读证据标注显示层） |
| 绑定/交易限制 UI | 已移植 | ConsignmentDialog.cs:216-218、CommunicationDialog.cs:795-796、GuildDialog.cs:390-392、InventoryDialog.cs:213 |
| 商店 Expirable 预览 | 已移植 | GameStoreDialog.cs:274-282、559-569 |
| 掉落来源查询（FortuneChecker） | 已移植 | FortuneCheckerDialog.cs:88-91（按 `Drops` 过滤） |
| 战利品箱/礼包 | 已移植 | LootBoxDialog.cs、BundleDialog.cs:37-55 |
| 套装判定显示 | 已移植 | GameScene.cs:5945-5981（AppendSetInfo，含 Level/Class 过滤逻辑镜像） |
| 物品名本地化 | 已移植 | translations/db_names.json（如 "Refinement Stone"→"精炼石" 3047-3050）；Scripts/LocalizedName.cs |
| 词条来源（StatSource）在 UI 上区分显示 | 未找到实现 | GodotClient 全库 grep `StatSource` 仅命中 DXItemCell/状态窗口的间接使用（本次检索未见"按来源分色显示词条"的等价物；未找到实现，疑在 tooltip 颜色简化时被省略，GameScene.cs:5482-5485 注释可佐证） |
| 佩带时 BagWeight/WearWeight/HandWeight 负重 UI | 部分移植 | GameScene.cs:7397-7400 CanWearItem 镜像含负重；独立的重量条控件未在本次检索范围内确认（StatusWindow.cs 存在但未逐行核对） |

## 移植注意事项

1. **随机数必须服务端权威**：所有概率（CreateDropItem 词条、精炼 Chance、DamageItem 磨损）都在服务器掷骰后仅回发结果。GodotClient 里已有 `Library.Time.Now` 之类的本地时钟，但不要把 `ProcessItemExpire` 的销毁时机搬到客户端——安全区暂停规则（PlayerObject.cs:584）容易在客户端复刻出不一致。
2. **公式有 int/decimal 混算与取整陷阱**：`RepairCost` 的 `(long)(p * Count - Price(Count)) * rate`——`rate` 乘在 long 截断**之后**；客户端镜像版本是 `(int)(...) * rate`（Globals.cs:604-605）。移植到 Godot（C#）保持同样写法，勿"顺手"重排运算。
3. **AddStat 合并语义**：同 (Stat, StatSource) 合并累加（UserItem.cs:520-539）。`StatSource` 区分 Added（掉落/饰品精炼）/Refine（武器精炼，可 Reset 转 Enhancement）/Enhancement（Reset 后的永久上限词条 ≤200/≤10）。Godot 端 tooltip 若要复刻原版"蓝字=固有、绿字=附加、橙字=精炼"必须保留该来源维度——目前 GodotClient 未区分。
4. **归属互斥**：UserItem 八个归属引用互斥清空（UserObject.cs OnChanged 354-458）。Godot 端如果引入本地物品缓存，同样只能有单一容器属主，否则邮件/拍卖/精炼流转会出现双持。
5. **Shape 是多义字段**：同一 int 在 RefineSpecial/DarkStone/LootBox/MagicRing/修理油/打造宝石/书本里语义完全不同。移植时按 `(ItemType, ItemEffect)` 分派解释器，不要做单一映射表。
6. **耐久非装备复用**：Book 的 Durability 是学习成功率、Meat/Ore 是纯度（还参与精炼 ore/2000 公式）、使用型物品当毫秒间隔、LootBox 当位掩码。Godot 端显示层需同套分派。
7. **精炼武器离线等待**：`RefineInfo.RetrieveTime` 用服务器时钟（`SEnvir.Now + Globals.RefineTimes[quality]`），客户端 `ClientRefineInfo` 拿到的已是换算后的剩余时间；重连时通过 `S.RefineList` 全量补发（PlayerObject.cs:1104-1110）。
8. **武器元素切换**：元素精炼成功会写 `Stat.WeaponElement` 的**差值**（`n - weapon.Stats[Stat.WeaponElement]`，PlayerObject.cs:12582），保证元素唯一；`MergeRefineElements`（UserItem.cs:790-820）在 Reset 时把散落元素合并回当前元素。Godot 端伤害计算要照抄 `Stats.GetWeaponElement()/GetWeaponElementValue()` 组合（PlayerObject.cs:2265-2271、GameScene.cs:5730-5735 已有镜像）。
9. **防作弊封号分支**：精炼类型/品质非法值直接 `Banned = true`（PlayerObject.cs:10861-10865、12219-12242、12821-12825）。Godot 单机版若直连 ServerLibrary 逻辑可保留；若重写网络层，务必保留服务端白名单校验而不是信任客户端下拉框。
10. **数据兼容**：System.db 的 ItemInfo 字段名即 MirDB 列名；新增字段需同步 `GodotClient/Network/DatabaseLoader.cs` 加载的表结构。`ItemInfo.Effect` 已 [Obsolete]，新代码一律用 `ItemEffect`（ItemInfo.cs:115-131）。
