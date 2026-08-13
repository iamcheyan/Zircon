# 掉落系统：DropInfo / 掉落算法 / 运势补偿(UserDrop) / 金币与部件 / 采集掉落 / 掉落归属

## TL;DR 速查表

- 掉落配置表 `DropInfo`（Monster+Item 双主键 + Chance/Amount/DropSet/PartOnly/EasterEvent），挂在 `MonsterInfo.Drops`（MonsterInfo.cs:262-263）；`Chance` 是"N 分之一"分母（Chance=0 永不掉）。
- 核心判定：金币 `chance = int.MaxValue / drop.Chance`（不除人数，金额除人数）；物品 `chance = (long)(int.MaxValue / (drop.Chance * players) * rate)`；成功当且仅当 `roll <= chance`（MonsterObject.cs:2717-2739/2761-2765）。
- `rate` 由 Stat.DropRate、Stat.BaseDropRate、地图 MonsterDrop 随机值、怪物 GrowthLevel 连乘（MonsterObject.cs:2693-2701）；地图掉率在怪物出生时掷一次（MonsterObject.cs:682-689）。
- `DropSet` 是位掩码互斥/分层：怪物出生时从 `MapInfo.DropSet` 拷贝（Map.cs:469），掉落行要求 `(DropSet & drop.DropSet) == drop.DropSet` 才生效（MonsterObject.cs:2710）。
- **UserDrop 是运势(保底)系统**：每次判定把期望产量累进 `Progress`，实际产量累进 `DropCount`；当随机失败但 `Progress > DropCount` 时仍掉落（ fortune 兜底），甚至一次性补发差额（MonsterObject.cs:2742-2759/2765/2827-2833）。`Config.EnableFortune` 开关（Config.cs:146）。
- 未命中/PartOnly 的行有机会转掉"部件"（ItemPart），部件判定 `partRoll <= chance * Item.PartCount`（MonsterObject.cs:2767-2780）。
- **没有 ItemGrade/金爆枚举**：品质只有 `Rarity{Common,Superior,Elite}`（Enum.cs:336-341）；掉落物生成 `SEnvir.CreateDropItem` 时随机 1/15 概率追加随机属性（非 Common 减半成 1/30），颜色为纯随机 RGB（SEnvir.cs:2149-2228）。
- 金币是特殊 DropInfo 行（Item=GoldInfo）：`IsUndroppableCurrencyItem` 时不落地直接入包，先扣行会税（MonsterObject.cs:2862-2876）。
- 归属：`ItemObject.Account` 记归属账号；`Config.DropVisibleOtherPlayers=true` 时他人 10 分钟/同会 5 分钟/同队 2 分钟后才可捡（ItemObject.cs:53-80）；组队**各自独立判定**（每人都跑一遍完整 Drop，Chance 与金币金额按人数摊薄）。
- 玩家采集三通道：怪物尸体 Harvest（Drops 字典按账号隔离，MonsterObject.cs:3022-3027）、钓鱼 FishingDropInfo（PerfectCatch/ThrowQuality 过滤）、挖矿 MineInfo（Chance+Region+限量 Restock）。

## 职责概述

本文覆盖服务端"怪物死亡 → 物品/金币产出 → 落地/归属 → 拾取"的全链路，以及玩家侧采集（挖肉/钓鱼/挖矿）掉落，供 Godot 客户端对齐地面物渲染、拾取交互与运势面板：

1. **配置层**：`DropInfo`（每怪物×每物品一行）+ `MonsterInfo.Drops` 关联 + `DropSet` 位掩码 + 节日事件行（EasterEvent）。
2. **执行层**：`MonsterObject.Die → YieldReward → Drop(owner, players, rate)`，含组队分配（dPlayers/dRate）、金币通道、部件通道、运势(UserDrop)通道、任务掉落通道、采集尸体通道。
3. **生成层**：`SEnvir.CreateDropItem`——随机属性掷点（UpgradeWeapon 等）、随机颜色、随机初始耐久。
4. **落地层**：`MapObject.GetDropLocation`（环形扫描找空格/最少层格）+ `ItemObject` 生命周期（60 分钟过期、归属保护、行会税、宠物自动拾取）。
5. **玩家层**：`PlayerObject.PickUp`（范围拾取）、`Harvest`（挖肉）、钓鱼/挖矿掉落、`FortuneCheck`（运势查询）。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| LibraryCore/SystemModels/DropInfo.cs | 5-122 | 掉落行：Monster[IsIdentity]/Item[IsIdentity]/Chance/Amount/DropSet/PartOnly/EasterEvent |
| LibraryCore/SystemModels/MonsterInfo.cs | 262-263 | `[Association("Drops", true)] DBBindingList<DropInfo> Drops` |
| LibraryCore/SystemModels/FishingInfo.cs | 5-119 | `FishingInfo`（Name/Region/Drops）+ `FishingDropInfo`（Item/Chance/ThrowQuality/PerfectCatch） |
| LibraryCore/SystemModels/MineInfo.cs | 6-110 | `MineInfo`（Map+Item 主键/Chance/Region/Quantity/RestockTimeInMinutes + 运行时 RemainingQuantity/NextRestock） |
| LibraryCore/Enum.cs | 336-341 | `Rarity : byte { Common, Superior, Elite }`（全库无 ItemGrade） |
| ServerLibrary/DBModels/UserDrop.cs | 7-79 | 运势记录：Account/Item/Progress(期望产量)/DropCount(实际产量) |
| ServerLibrary/DBModels/AccountInfo.cs | 576-577 | `[Association("UserDrops")] DBBindingList<UserDrop> UserDrops` |
| ServerLibrary/DBModels/AccountInfo.cs | 492-520 | GoldBot/ItemBot 反挂机标记（影响掉落判定） |
| ServerLibrary/Models/MonsterObject.cs | 32/79 | `DropSet` 位掩码字段；`Dictionary<AccountInfo, List<UserItem>> Drops`（采集尸体库存） |
| ServerLibrary/Models/MonsterObject.cs | 658-659 | 出生掷 EasterEventMob（`Random.Next(EasterEventChance) == 0`） |
| ServerLibrary/Models/MonsterObject.cs | 682-689 | 出生掷地图掉率/金币率（BuffStats MonsterDrop/MonsterGold 区间随机） |
| ServerLibrary/Models/MonsterObject.cs | 744-758 | GrowthLevel（成长怪）属性放大 |
| ServerLibrary/Models/MonsterObject.cs | 2513-2514 | `Die()` 末尾调用 `YieldReward()` |
| ServerLibrary/Models/MonsterObject.cs | 2549-2689 | `YieldReward()`：经验分配 + dPlayers 收集 + dRate 职业组合加成 + 逐玩家 `Drop()` |
| ServerLibrary/Models/MonsterObject.cs | 2691-3028 | `Drop()`：掉落主算法（本文核心） |
| ServerLibrary/Models/MonsterObject.cs | 3145-3168 | 采集状态同步（HarvestChanged/Skeleton 判定） |
| ServerLibrary/Models/Monsters/VoraciousGhost.cs | 37-42 | `Drop()` override 示例：复活次数耗尽才掉落 |
| ServerLibrary/Models/Map.cs | 469 | `mob.DropSet = Info.DropSet`（掉落掩码来自地图） |
| ServerLibrary/Models/MapObject.cs | 1162-1221 | `GetDropLocation(distance, player)`：环形扫描、无阻挡、最少叠层 |
| ServerLibrary/Models/ItemObject.cs | 11-21 | 地面物品：ExpireTime/Item/Account(归属)/MonsterDrop |
| ServerLibrary/Models/ItemObject.cs | 23-51 | 过期 Despawn + 临时物品清理 |
| ServerLibrary/Models/ItemObject.cs | 53-80 | `CanPickUpItem`：归属保护宽限期（10/5/2 分钟） |
| ServerLibrary/Models/ItemObject.cs | 82-144 | `PickUpItem(Player/Companion)`：行会税扣除 + GainItem |
| ServerLibrary/Models/ItemObject.cs | 160-171/185-194 | `CanBeSeenBy`（他人掉落可见性）；`OnSpawned` 设 `ExpireTime = Now + Config.DropDuration` |
| ServerLibrary/Models/PlayerObject.cs | 8582-8616 | `PickUp()`：按 PickUpRadius 环形扫描拾取 |
| ServerLibrary/Models/PlayerObject.cs | 13968-14111 | `Harvest()`：从怪物 Drops 字典取采集产物 |
| ServerLibrary/Models/PlayerObject.cs | 14338-14362 | 钓鱼掉落：FishingDropInfo 判定 |
| ServerLibrary/Models/PlayerObject.cs | 14985-15035 | 挖矿掉落：MineInfo 判定 + 限量/补货 |
| ServerLibrary/Models/PlayerObject.cs | 9223-9272 | `FortuneCheck()`：消耗运势查询道具，快照 UserDrop 进度 |
| ServerLibrary/Envir/SEnvir.cs | 519 | `GoldInfo = CurrencyInfoList.First(Type==Gold).DropItem`（金币物品单例） |
| ServerLibrary/Envir/SEnvir.cs | 2131-2228 | `CreateDropItem(ItemCheck/ItemInfo)`：随机属性/颜色/耐久 |
| ServerLibrary/Envir/Config.cs | 130-132/144-146 | DropDuration=60min/DropDistance=5/DropLayers=5；DropVisibleOtherPlayers=true/EnableFortune=true |
| ServerLibrary/Envir/Events/Triggers/MonsterDie.cs | 15-19 | 事件触发器同样按 DropSet 掩码过滤 |

## 核心流程

### 1. 触发时机：Die → YieldReward → 逐玩家 Drop

`MonsterObject.Die()`（2513-2514）末尾调 `YieldReward()`（2549-2689）。它先算经验，再收集**掉落受益人**：

```csharp
if (EXPOwner.GroupMembers != null)
{
    // ...（2559-2606）遍历组队成员：
    if (ob.CurrentMap != CurrentMap || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Config.MaxViewRange)) continue;
    ...
    dPlayers.Add(ob);        // 2584 视野内的队员（含死者）都参与掉落
    if (ob.Dead) continue;
    ...
    ePlayers.Add(ob);        // 2604 只有活着的拿经验
}
```

职业组合掉率加成（2608-2619）：

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
```

最后逐人独立跑掉落（2679-2688）——**组队不是分配同一堆掉落，而是每个队员各自完整判定一遍**：

```csharp
if (dPlayers.Count == 0)
{
    if (!EXPOwner.Dead && ...)
        Drop(EXPOwner, 1, dRate);
}
else
{
    foreach (PlayerObject player in dPlayers)
        Drop(player, dPlayers.Count, dRate);
}
```

`EXPOwner` 是"最后一击归属者"（20 秒衰减，MonsterObject.cs:56-69），宠物击杀（PetOwner!=null）不发奖励（2551）。

### 2. 掉落主算法：MonsterObject.Drop（2691-3028）

#### 2.1 掉率系数 rate（2693-2701）

```csharp
rate *= 1M + owner.Stats[Stat.DropRate] / 100M;

rate *= 1M + owner.Stats[Stat.BaseDropRate] / 100M;

if (PetOwner == null && CurrentMap != null)
    rate *= 1M + MapDropRate / 100M;

if (GrowthLevel > 0)
    rate *= 1M + (GrowthLevel * 10) / 100M;
```

`MapDropRate` 在怪物出生时从地图 BuffStats（Stat.MonsterDrop~Stat.MaxMonsterDrop 区间）随机一次（MonsterObject.cs:682-689），即同一地图每只怪的地图掉率略有不同。

#### 2.2 行过滤与数量（2710-2714）

```csharp
foreach (DropInfo drop in MonsterInfo.Drops)
{
    if (drop?.Item == null || drop.Chance == 0 || (DropSet & drop.DropSet) != drop.DropSet) continue;

    if (drop.EasterEvent && !EasterEventMob) continue;

    long amount = Math.Max(1, drop.Amount / 2 + SEnvir.Random.Next(drop.Amount));
```

- `DropSet` 掩码：怪物出生时 `mob.DropSet = Info.DropSet`（Map.cs:469，来自 MapInfo）。**必须 `(DropSet & drop.DropSet) == drop.DropSet`**——用于"同一怪物在不同地图/副本出不同掉落"或分档（普通/BOSS 档）。
- `EasterEvent` 行只有复活节怪（出生时按 `SpawnInfo.Info.EasterEventChance` 掷出，658-659）才生效。
- 数量公式：**每件数量 = Amount/2 + rnd[0,Amount)**，即期望 75% Amount，下限 1。

#### 2.3 金币分支（2717-2735）

```csharp
if (drop.Item == SEnvir.GoldInfo)
{
    if (owner.Character.Account.GoldBot && Level < owner.Level) continue;   // 反金币挂机

    chance = int.MaxValue / drop.Chance;        // 注意：不除以 players

    amount /= players;                          // 金额按人数摊薄

    amount += (int)(amount * owner.Stats[Stat.GoldRate] / 100M);
    amount += (int)(amount * owner.Stats[Stat.BaseGoldRate] / 100M);

    if (PetOwner == null && CurrentMap != null)
        amount += (int)(amount * MapGoldRate / 100M);

    if (GrowthLevel > 0)
        amount += (int)(amount * (GrowthLevel * 10) / 100M);

    if (amount == 0) continue;
}
```

#### 2.4 普通物品分支（2737-2740）

```csharp
else
{
    chance = (long)(int.MaxValue / (drop.Chance * players) * rate);
}
```

`SEnvir.Random.Next()` 返回 `[0, int.MaxValue)`，因此 **`roll <= chance` 的概率 ≈ 1/Chance × rate/players**（对金币是 1/Chance，不随人数降低概率）。

#### 2.5 运势（UserDrop）累积（2742-2759）

```csharp
UserDrop userDrop = owner.Character.Account.UserDrops.FirstOrDefault(x => x.Item == drop.Item);

if (userDrop == null)
{
    userDrop = SEnvir.UserDropList.CreateNewObject();
    userDrop.Item = drop.Item;
    userDrop.Account = owner.Character.Account;
}

if (Config.EnableFortune)
{
    decimal progress = chance / (decimal)int.MaxValue;

    progress *= amount;

    if (!drop.PartOnly)
        userDrop.Progress += progress;
}
```

`progress = 概率 × 数量` 即"本次判定的期望产量"，**每次杀怪都累加**（无论成败）；PartOnly 行不累积。

#### 2.6 判定与三向分支（2761-2765）

```csharp
var roll = SEnvir.Random.Next();

//(drop is partOnly) OR
//(roll has failed OR ItemBot) AND (fortune progress not reached)
if (drop.PartOnly || ((roll > chance || owner.Character.Account.ItemBot) && ((long)userDrop.Progress <= userDrop.DropCount)))
```

- **roll 成功**（`roll <= chance`）且非 ItemBot → 走 2.8 掉落真身；
- **roll 失败但运势已超额**（`Progress > DropCount`）→ 同样走 2.8（保底触发）；ItemBot 账号只有运势超额才掉（反挂机）；
- **roll 失败且运势未超额**（或 PartOnly 行）→ 走 2.7 部件分支。

#### 2.7 部件（ItemPart）分支（2767-2825）

```csharp
if (SEnvir.ItemPartInfo == null || drop.Item.PartCount <= 1 || SEnvir.IsCurrencyItem(drop.Item)) continue;

var partRoll = SEnvir.Random.Next();

if (drop.PartOnly)
{
    //part roll failed
    if (partRoll > chance) continue;
}
else
{
    //part roll for non partOnly drop failed
    if (partRoll > chance * drop.Item.PartCount) continue;
}
```

- PartOnly 行：部件判定概率 = 1/Chance（与真身同概率，只是产物永远是部件）。
- 非 PartOnly：部件判定概率 = PartCount/Chance（更容易），`PartCount` 是 ItemInfo 上"多少个部件合成一件"的数量。
- 产物：`SEnvir.CreateDropItem(SEnvir.ItemPartInfo)` + `AddStat(Stat.ItemIndex, drop.Item.Index, Added)`（部件携带原物品索引，2784-2787）。
- `NeedHarvest` 怪（可挖肉尸体）的部件进 `Drops` 字典而非落地（2791-2803），且非 Common 稀有度会聊天提示（2796-2799）。

#### 2.8 掉落真身 + 运势差额补发（2827-2903）

```csharp
if (Config.EnableFortune)
{
    if (!SEnvir.IsCurrencyItem(drop.Item) && (Math.Floor(userDrop.Progress) > userDrop.DropCount + amount))
        amount = (long)(userDrop.Progress - userDrop.DropCount);
}

userDrop.DropCount += amount;

result = true;
while (amount > 0)
{
    UserItem item = SEnvir.CreateDropItem(drop.Item);
    if (companionAutoCollect && drop.Item == SEnvir.GoldInfo)
        item.Count = amount;
    else
        item.Count = Math.Min(drop.Item.StackSize, amount);

    amount -= item.Count;

    item.SetTemporary(true); //REMOVE ON Gain
    ...
```

- 运势补发：期望产量比实际产量多出超过本次数量时，**一次性把差额并入本次 amount**（长非酋会在某次出货时连本带利）。
- 超过 StackSize 自动拆多件。
- `NeedHarvest` → 进采集字典（2848-2860）。
- **货币物品（仅金币）不落地**：先扣行会税再 `owner.GainItem(item)` 直接入包（2862-2876）：

```csharp
if (SEnvir.IsUndroppableCurrencyItem(drop.Item))
{
    //Only gold
    long taxableAmount = owner.Character.Account.GuildMember?.Guild?.CalculateGuildTax(item) ?? 0;

    if (taxableAmount > 0)
    {
        item.Count -= taxableAmount;

        owner.Character.Account.GuildMember.Contribute(taxableAmount);
    }

    owner.GainItem(item);
    continue;
}
```

- 普通物品 `ItemObject{Item, Account=owner 账号, MonsterDrop=true}` 落地（2878-2887）；有宠物且 `Stat.CompanionCollection>0` 时尝试自动拾取（含金币税计算，2889-2902）。

#### 2.9 任务掉落通道（2906-3017）

对玩家每个未完成任务的 `QuestTaskMonsterDetails`：同怪/同地图、`Random.Next(details.Chance) > 0`（即 1/Chance）、DropSet 掩码匹配 → `KillMonster` 计数或 `GainItem` 产出任务物品（打 `UserItemFlags.QuestItem`，普通掉落同款落地/入包流程，2943-3010）。

#### 2.10 采集尸体收尾（3019-3027）

```csharp
if (result && owner.Companion != null)
    owner.Companion.SearchTime = DateTime.MinValue;

if (!NeedHarvest) return;

if (Drops == null)
    Drops = new Dictionary<AccountInfo, List<UserItem>>();

Drops[owner.Character.Account] = drops;
```

`Drops` 按**账号**分桶——每个猎人只能挖到自己那份（PlayerObject.Harvest 用 `ob.Drops.TryGetValue(Character.Account, ...)` 取件，PlayerObject.cs:14035）。

### 3. 掉落物生成：SEnvir.CreateDropItem（2131-2228）

```csharp
public static UserItem CreateDropItem(ItemInfo info, int chance = 15)
{
    UserItem item = UserItemList.CreateNewObject();

    item.Info = info;
    item.MaxDurability = info.Durability;

    ItemSetup(item);

    item.Colour = Color.FromArgb(Random.Next(256), Random.Next(256), Random.Next(256));

    if (item.Info.Rarity != Rarity.Common)
        chance *= 2;

    if (Random.Next(chance) == 0)
    {
        switch (info.ItemType)
        {
            case ItemType.Weapon:
                UpgradeWeapon(item);
                break;
            ...
            case ItemType.SocketGem:
                UpgradeSocketGem(item);
                break;
        }
        item.StatsChanged();
    }
    ...
```

要点：

- **随机属性概率 1/15**；非 Common（Superior/Elite）`chance *= 2` → **1/30**（稀有基础物品更少随机词条，词条价值预期由稀有度本身承载）。
- `Colour` 是纯随机 RGB——引擎没有"金爆/暗金"颜色判定；名字颜色/品质显示全部由 `Rarity` 驱动。**全库搜索 Grade 仅命中 `MilestoneGrade`（成就难度，Enum.cs:1988-1991），与掉落无关。**
- 耐久随机化（2198-2225）：装备 `Random.Next(Durability)+1000`、肉 `×2+2000`、矿 `×3+3000`、书 `Random.Next(96)+5`。
- `ItemSetup`：Bundle/LootBox 类型立即展开内容物（2230-2241）。
- 客户端 `Make` 命令用 `CreateDropItem(item, 0)` 造 GM 物（随机 `Next(0)==0` 恒真，必带词条，Commands/Command/Admin/Make.cs:62）。

### 4. 落地位置：MapObject.GetDropLocation（1162-1221）

环形扩散扫描（与 PickUp 同款环遍历），条件：格子无 Movements、无 Blocking 对象；优先**完全无物品的格子**（`count == 0` 即返回），否则记录叠层最少的 bestCell，超过 `Config.DropLayers`（=5，Config.cs:132）层返回 null（怪物脚下溢出时回退 `CurrentCell`，MonsterObject.cs:2805/2878）。`player != null` 时统计层数会跳过该玩家看不见的物品（1200）。

### 5. 掉落归属与拾取

`ItemObject`（ItemObject.cs:11-21）：

- `Account` 记录归属**账号**（防掉线丢失，注释 19 行）；`MonsterDrop=true` 表示怪物掉落（玩家主动丢弃的 Bound 物也写 Account，PlayerObject.cs:8480-8481）。
- 过期：`ExpireTime = SEnvir.Now + Config.DropDuration`（60 分钟，Config.cs:130；OnSpawned 189 行），到点 Despawn，临时物品直接删除（46-47）。

**归属保护**（CanPickUpItem，ItemObject.cs:53-80）：

```csharp
if (Account != null && Account != ob.Character.Account)
{
    if (Config.DropVisibleOtherPlayers)
    {
        var isSameGuild = ...;
        var isSameGroup = ...;
        var spawnElapsed = (int)Math.Floor((SEnvir.Now - SpawnTime).TotalMinutes);

        if (spawnElapsed >= 10)
            return true;
        else if (isSameGuild && spawnElapsed >= 5)
            return true;
        else if (isSameGroup && spawnElapsed >= 2)
            return true;
    }

    return false;
}

return true;
```

即：`DropVisibleOtherPlayers=false`（继承服风格）时他人**永远不可见不可捡**（CanBeSeenBy 162-168 直接不发送）；`=true` 时按 10 分钟（任何人）/5 分钟（同行会）/2 分钟（同队伍）梯度开放。没有名为 `PickupRight` 的字段/概念——归属权就是上述宽限期矩阵。

拾取执行（PickUpItem，82-144）：再次校验 CanPickUpItem → 计算行会税（`CalculateGuildTax`，金币才>0）→ `CanGainItems` 容量检查 → 扣税、`GainItem`、Despawn。宠物拾取走同构重载（114-144）。

玩家侧拾取入口 `C.PickUp`（SConnection.cs:565-570）→ `PlayerObject.PickUp()`（8582-8616）：以玩家为中心、`Stat.PickUpRadius`（默认 1，2576）为半径环形扫描**距离近者优先**，捡到第一件即返回。

### 6. 采集掉落之一：怪物尸体 Harvest

`NeedHarvest` 怪（尸体可挖）死时掉落进 `Drops` 字典（2.10 节），尸体存留时间加长 `DeadTime += Config.HarvestDuration`（2528-2530）。玩家 `Harvest(direction)`（PlayerObject.cs:13968-14111）：

- 面向前方格 + PickUpRadius 范围找怪（14009-14029）；
- `ob.Drops.TryGetValue(Character.Account, out items)`——**没有自己那份时提示 HarvestOwner**（14035-14039）；
- `ob.HarvestCount > 0` 时先消耗次数（14041-14045）；
- 未完成任务的物品剔除删除（14051-14062）；
- `CanGainItems` 通过的逐件 GainItem，拿不完提示 HarvestCarry（14077-14099）。

### 7. 采集掉落之二：钓鱼 FishingDropInfo

钓鱼成功收线时（PlayerObject.cs:14338-14362）：

```csharp
var zone = Functions.FishingZone(SEnvir.FishingInfoList, CurrentMap.Info, CurrentMap.Width, CurrentMap.Height, floatLocation);

foreach (FishingDropInfo info in zone.Drops.OrderByDescending(x => x.Chance))
{
    if (info.Item == null) continue;

    if (info.PerfectCatch && !perfectCatch) continue;

    if (info.ThrowQuality != 0 && info.ThrowQuality != FishThrowQuality) continue;

    if (SEnvir.Random.Next(info.Chance) > 0) continue;

    ItemCheck check = new ItemCheck(info.Item, 1, UserItemFlags.Bound, TimeSpan.Zero);

    if (!CanGainItems(false, check)) continue;

    UserItem item = SEnvir.CreateDropItem(check);
    GainItem(item);
    ...
    break; //One item gained, so stop rewarding any more
}
```

- 掉落表挂在**钓鱼区**（FishingInfo.Region 命中 floatLocation），非怪物。
- `Chance` 同"N 分之一"（`Next(Chance) > 0` 即失败）。
- `PerfectCatch`（一次不失误，14328-14335）与 `ThrowQuality`（抛竿质量档位）是钓鱼专属过滤条件。
- **每次钓鱼最多 1 件**，物品 Bound。排序 `OrderByDescending(Chance)` 意味着 Chance 小（越稀有）的行**先判定**。

### 8. 采集掉落之三：挖矿 MineInfo

`PlayerObject`（14985-15035，挖矿动作在攻击管线内）：

```csharp
foreach (MineInfo info in CurrentMap.Info.Mining)
{
    if (SEnvir.Random.Next(info.Chance) > 0) continue;

    if (info.Region != null)
    {
        if (info.Region.PointList == null)
            info.Region.CreatePoints(CurrentMap.Width);

        if (!info.Region.PointList.Contains(front)) continue;
    }

    if (info.Quantity == 0) continue;

    if (info.Quantity > 0 && info.RemainingQuantity == 0)
    {
        if (info.NextRestock > SEnvir.Now) continue;

        info.RemainingQuantity = info.Quantity;
    }

    ItemCheck check = new ItemCheck(info.Item, 1, UserItemFlags.Bound, TimeSpan.Zero);
    ...
    if (info.Quantity > 0)
    {
        info.RemainingQuantity--;

        if (info.RemainingQuantity == 0 && info.RestockTimeInMinutes >= 0)
        {
            info.NextRestock = SEnvir.Now.AddMinutes(info.RestockTimeInMinutes);
        }
    }
}
```

- 前置：地图 `CanMine`、面前格无 Cell（矿点）、武器 `ItemEffect.PickAxe` 且耐久>0（14985-14991，武器每挖损耗 4 点耐久）。
- 掉落表挂地图（`MapInfo.Mining`），Region 限定矿区；`Quantity>0` 时全服限量开采，耗尽后按 `RestockTimeInMinutes` 补货（-1 永不补，MineInfo.cs:86-102）。
- 产物 Bound 直接入包（不落地）；同时生成/叠加 Rubble（碎石堆）SpellObject（15037-15069）。
- 注意挖矿与钓鱼不同：**没有 break，一次可命中多行**（矿表通常一行）。

### 9. 运势查询：FortuneCheck（PlayerObject.cs:9223-9272）

消耗 `SEnvir.FortuneCheckerInfo`（运势查询道具，需先持有，9227-9233），把某物品的 `UserDrop.Progress/DropCount` 快照进 `UserFortuneInfo` 并回 `S.FortuneUpdate`（9260-9271）。客户端 FortuneCheckerDialog 显示"掉落数量 / 距运势掉落 / 上次查询"。`Config.EnableFortune=false` 时整个运势层静默（9225，Drop 里也不累积）。

## 数据结构/协议细节

### DropInfo 字段全表（LibraryCore/SystemModels/DropInfo.cs）

| 字段 | 类型 | 语义 |
|---|---|---|
| Monster | MonsterInfo [IsIdentity] | 所属怪物（Association "Drops"） |
| Item | ItemInfo [IsIdentity] | 掉落物品；=SEnvir.GoldInfo 时走金币通道 |
| Chance | int | "N 分之一"分母；0 = 永不掉落（2710 直接 continue） |
| Amount | int | 产量基数；实际 = `Amount/2 + rnd[0,Amount)`，金币再除以人数 |
| DropSet | int | 位掩码；需 `(怪物DropSet & drop.DropSet) == drop.DropSet`；怪物 DropSet 出生于 MapInfo.DropSet（Map.cs:469） |
| PartOnly | bool | true=只掉部件（判定概率=1/Chance）；false=未命中时按 PartCount/Chance 掉部件 |
| EasterEvent | bool | 仅复活节怪可出（EasterEventMob 出生掷点，MonsterObject.cs:658-659） |

### UserDrop（运势）机制总结

- `Progress`（decimal）：**期望产量累计**——每次对该物品判定加 `概率×数量`，不管成败（2751-2759）。
- `DropCount`（long）：**实际产量累计**——只有真出货才加（2833）。
- 出货条件：`roll <= chance`（正常）或 `Progress > DropCount`（保底）；补发 `Progress − DropCount` 差额（2829-2830）。
- 本质是**账号级、按物品的确定性产量追踪**（伪随机补偿），非幸运值加成——`rate` 与 UserDrop 无关。
- 金币不参与运势（IsCurrencyItem 跳过，2757/2829）。

### 相关封包

| 包 | 方向 | 说明 |
|---|---|---|
| S.ObjectItem（ItemObject.cs:195-203） | S→C | 地面物品出现（带完整 ClientUserItem） |
| S.DataObjectItem（204-215） | S→C | 小地图/数据通道（ItemIndex+位置） |
| S.ObjectHarvest / S.ObjectHarvested（MonsterObject.cs:3145-3152） | S→C | 采集动作/尸体状态（无自己掉落者收到 Harvested） |
| S.FortuneUpdate（PlayerObject.cs:9271） | S→C | 运势查询结果 |
| C.PickUp（ClientPackets.cs:235） | C→S | 范围拾取（无参，服务端按 PickUpRadius 扫） |
| C.CurrencyDrop（201-204） | C→S | 丢货币 |
| C.Harvest / C.FishingCast / 攻击（挖矿走攻击包） | C→S | 三类采集入口 |

## GodotClient 现状

| 功能 | 状态 | GodotClient 证据 |
|---|---|---|
| 地面物品渲染（含货币分段图标） | 已移植 | Scripts/GameScene.cs:2514-2517 OnObjectItem → ObjectRenderer.CreateItem（Scripts/ObjectRenderer.cs:128-131）；货币图标 CurrencyImage（244-251）；小地图 DataObjectItem（GameScene.cs:2366-2369）；Network/ServerConnection.cs:536-540 含启动缓冲 PendingItems |
| 拾取发包 | 已移植 | Network/ServerConnection.cs:1003-1006 SendPickUp；Tab 键绑定 Controls/KeyBindManager.cs:30/123；250ms 节流与状态闸门 GameScene.cs:734-739；Shift 点击脚下格不拾取 Scripts/CombatController.cs:86-88/337-339 |
| 拾取范围/掉落率/金币率属性显示 | 已移植 | Controls/CharacterDialog.cs:501-504（PickUpRadius/GoldRate/DropRate 三行） |
| 掉落名称过滤 | 已移植 | Controls/FilterDropDialog.cs:8-16（10 条过滤词）；Ctrl+F 绑定 KeyBindManager.cs:28/121；GameScene.cs:1914-1917 装载 DropFilters |
| 货币丢弃（CurrencyDrop） | 已移植 | ServerConnection.cs:1095；GameScene.cs:9956-9962 丢货币弹窗（预览格实时刷新数量） |
| 运势查询（Fortune） | 已移植 | Controls/FortuneCheckerDialog.cs:13-33（检索+逐项查询）、161-190 行显示进度并发 C.FortuneCheck；S.FortuneUpdate 接线 ServerConnection.cs:132/515、GameScene.cs:1223/6427-6434（_fortunes 字典缓存）；使用运势道具入口 DXItemCell.cs:1237-1239 |
| 挖肉采集（Harvest） | 已移植 | ServerConnection.cs:1093 SendHarvest；S.ObjectHarvest/Harvested 动画 GameScene.cs:2908-2922/2211-2215；PlayerRenderer.cs:343 PlayHarvest |
| 挖矿 | 已移植 | 状态机 GameScene.cs:83-90/752-762（CanMineNow：地图可挖/武器槽 PickAxe/耐久/矿点 Flag/相邻/无马）；间隔公式 ComputeMiningIntervalMs（1496-1499）；S.ObjectMining 动画 GameScene.cs:2981-2990、PlayerRenderer.cs:346-351 |
| 钓鱼 | 已移植 | Controls/FishingDialog.cs:11-（装备五格）+ FishingCatchDialog.cs:51-165（抛竿/收线/进度/自动抛竿）；SendFishingCast ServerConnection.cs:1034-1037；S.FishingStats/ObjectFishing 接线（138-139/521-522） |
| 金币拾取音效 | 已移植 | SoundCatalog.cs:93-94 GoldPickUp(120.wav)/GoldGained(122.wav)；MiningHit 65-66 |
| 部件（ItemPart）渲染 | 已移植 | Controls/DXItemCell.cs:246-251（ItemEffect.ItemPart 按 AddedStats[ItemIndex] 显示原物品图） |
| 服务器掉落概率/归属判定 | 无需移植 | 全部在服务端（MonsterObject.Drop/ItemObject.CanPickUpItem），客户端只表现 |
| 掉落公告（HarvestRare 稀有提示） | 未移植（未找到） | 服务端在 NeedHarvest 稀有件时发 System 聊天（MonsterObject.cs:2796-2799）；GodotClient 未找到对应专门 UI（走通用聊天通道即可，非缺口） |

## 移植注意事项

1. **Chance 语义是"N 分之一"**：所有掉落/钓鱼/挖矿判定都是 `Random.Next(chance) > 0` 即失败。给 Godot 做掉落模拟器/攻略页时不要把 Chance 当"百万分率"。
2. **组队掉落是每人独立判定**：UI 上"队长分配/掷点"并不存在； Chance÷人数、金币金额÷人数是唯一的组队摊薄。10/5/2 分钟归属宽限期（ItemObject.cs:66-73）需要客户端用 SpawnTime 本地估算提示"暂时不可拾取"，但**判定权在服务端**，CanPickUpItem 失败时客户端没有任何回包——不要指望错误提示。
3. `DropVisibleOtherPlayers=false` 时服务端根本不给他人生成 S.ObjectItem（CanBeSeenBy 162-168）——Godot 端"看不到别人的掉落"是网络层结果，不是渲染过滤。
4. **部件与真身共享 Chance**：非 PartOnly 行的部件判定概率是 `chance × PartCount`（2779），移植掉率计算器时两条通路都要算。
5. `CreateDropItem` 的随机属性/颜色/耐久全部服务端生成（S.ObjectItem 带全量 ClientUserItem），客户端禁止自行随机——Godot 的 DXItemCell 渲染 `Colour` 字段即可，不要本地生成。
6. 钓鱼掉落**每次最多 1 件且先判定小 Chance**（14340 OrderByDescending 后 `Random.Next(info.Chance)`——Chance 数值越小越稀有、排越前），挖矿**可多行命中**：两段循环结构不同，别合并成一个通用"采集掉落"组件。
7. 运势（UserDrop）是账号级补偿，跨角色共享；`FortuneCheck` 需要消耗道具，且 `Config.TestServer` 下非货币物品直接 return（PlayerObject.cs:9239）。
8. 金币三特性：不落地（直入包）、先扣行会税、`GoldBot` 账号打低于自身等级的怪不掉金币（2719）——做离线统计/日志页时金币与物品要分开统计。
9. 地面物品 60 分钟过期（Config.DropDuration）、同格最多 5 层（DropLayers）；溢出时掉在怪物脚下（`?? CurrentCell`），客户端不要对"叠很多件"的格子做布局假设。
10. `MonsterObject.Drop` 是 virtual（VoraciousGhost.cs:37-42 按 ReviveCount 拦截），查特殊怪掉落逻辑时先确认有无 override。
