# 经济系统：商店买卖 / 拍卖行（寄售市场）/ 货币体系 / 游戏商城 / 充值

## TL;DR 速查表

- 货币只有一种持久化容器 `UserCurrency`（Info+Amount），全部挂在 `AccountInfo.Currencies` 下（AccountInfo.cs:668-681 为每个账号自动补齐所有 CurrencyInfo）；`Gold/GameGold/HuntGold` 只是按 `CurrencyType` 取值的快捷属性（AccountInfo.cs:331-337）。
- `CurrencyType` 共 6 种：`Gold=0, GameGold=1, HuntGold=2, Other=3, FP=4, CP=5`（LibraryCore/Enum.cs:1869-1877）；物品价格体系以"金币价值"为基准，`CurrencyInfo.ExchangeRate` 是该货币兑金币的汇率（除法换算）。
- NPC 买入价 = `NPCGood.CostFor()`：`BaseCost(Item.Price × Rate) × amount / ExchangeRate` 向上取整（NPCInfo.cs:383-395）；NPC 卖出价 = `item.Price(count) / ExchangeRate`（PlayerObject.cs:10357-10358）。
- 卖出价核心公式在 `UserItem.Price()`（ServerLibrary/DBModels/UserItem.cs:578-608）：耐久折价 + 附加属性 ×(1+0.1×条数) + SaleBonus 批量加成 + `Info.SellRate` 最终乘数；客户端 `ClientUserItem.Price()` 是逐行同构的镜像（LibraryCore/Globals.cs:564-593）。
- 拍卖行（MarketPlace，包名前缀 `C.MarketPlace*` 而非 `C.Auction*`）：只有**一口价**，无竞价/还价；挂牌收 `Globals.MarketPlaceFee`（=0），成交收 7% 税（`Globals.MarketPlaceTax = 0.07M`，Globals.cs:125），货款以**邮件附件金币**发给卖家（PlayerObject.cs:4221-4260）。
- 挂牌**没有到期自动下架**：`AuctionInfo.ConsignDate` 只用于 Newest/Oldest 排序与客户端显示，未找到任何按时间清理的循环；管理员可批量撤销（`PlayerObject.MarketPlaceCancelSuperior`，PlayerObject.cs:4589-4623）。
- **没有玩家摆摊系统**：`Consign*` 系列全部是"把物品挂到全服寄售市场"（MarketPlaceConsign），不存在个人商店/摊位。
- 游戏商城（GameStore）＝拍卖行窗口里的官方商店页，数据源 `StoreInfo`（Price/HuntGoldPrice 双定价），买断用 GameGold 或 HuntGold，购买/赠送记录入 `GameStoreSale`，收藏入 `GameStoreFavourite`。
- GameGold 由 PayPal IPN 充值入账（`WebServer` + `SEnvir.ProcessGameGold`，SEnvir.cs:1645-1792），推荐人返利 10% 以 **HuntGold** 结算（SEnvir.cs:1781）。
- GodotClient：寄售市场（ConsignmentDialog）、成交历史（MarketHistoryDialog）、游戏商城（GameStoreDialog/赠送/收藏）、钱包（CurrencyDialog）、NPC 买卖（NPCGoodsPanel + InventoryDialog SellMode）均已移植。

## 职责概述

本文覆盖 Zircon 服务端全部"价值流转"管线，供 Godot 客户端对齐 UI 与包时序：

1. **NPC 商店买卖**：`NPCGood`（System.db，NPC 页面商品）+ `PlayerObject.NPCBuy/NPCSell`；支持按 `NPCPage.Currency` 指定任意货币结算、行会基金代付（Gold 限定）。
2. **拍卖行/寄售市场**：`AuctionInfo`（挂牌单）+ `AuctionHistoryInfo`（成交历史，滑动平均价）+ `PlayerObject.MarketPlaceConsign/CancelConsign/Buy` + `SConnection.Process(C.MarketPlaceSearch/SearchIndex/History)`。
3. **货币体系**：`CurrencyInfo`（System.db 货币定义，含 DropItem 与 ExchangeRate）+ `UserCurrency`（Users.db 余额）；金币的三条特殊通道（怪物掉落、行会税、邮件附件）。
4. **游戏商城**：`StoreInfo` 双定价 + `GameStoreSale/GameStoreFavourite`（消费记录/收藏）+ 登录/变更时下发的 `S.GameStoreData/GameStoreTopItems`。
5. **充值**：`WebServer`（HttpListener Buy 页 + IPN 回调）→ `SEnvir.ProcessGameGold` → `GameGoldPayment` 入账 + 推荐返利。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| LibraryCore/SystemModels/StoreInfo.cs | 5-97 | 商城商品条目：Item[IsIdentity]/Price/HuntGoldPrice/Filter/Available/Duration |
| LibraryCore/SystemModels/CurrencyInfo.cs | 5-112 | 货币定义：Name/Abbreviation/Type/Category/DropItem/ExchangeRate/Images（金额分段图标） |
| LibraryCore/SystemModels/NPCInfo.cs | 286-443 | `NPCGood`：Rate（价格系数，默认 1）、BaseCost/Cost、CostFor/MaxAmountFor/NormaliseCurrencyPurchaseAmount |
| LibraryCore/SystemModels/ItemInfo.cs | 193/253/283 | `Price`（基准金币价）、`SellRate`（出售系数）、`CanSell`（能否卖店） |
| LibraryCore/Enum.cs | 1869-1886 | `CurrencyType`（Gold/GameGold/HuntGold/Other/FP/CP）、`CurrencyCategory`（Basic/Player/Event/Map/Other） |
| LibraryCore/Globals.cs | 109/125/301 | `MarketPlaceFee=0`、`MarketPlaceTax=0.07M`、`StorageSize=100`（挂牌上限用） |
| LibraryCore/Globals.cs | 454-593 | `ClientUserItem`（含客户端版 Price()，564-593） |
| LibraryCore/Globals.cs | 916-929 / 1192-1195 | `ClientMarketPlaceInfo`、`ClientUserCurrency` |
| LibraryCore/Network/ClientPackets.cs | 262-272 | `C.NPCBuy`（Index/Amount/GuildFunds）、`C.NPCSell`（Links） |
| LibraryCore/Network/ClientPackets.cs | 432-486 | `C.MarketPlaceHistory/Consign/Search/SearchIndex/CancelConsign/Buy/StoreBuy`、`C.GameStoreFavouriteToggle/GameStoreGift` |
| LibraryCore/Network/ServerPackets.cs | 847-912 | `S.MarketPlace*` 全家 + `S.GameStoreData/TopItems/FavouriteChanged/Gift` |
| LibraryCore/Network/ServerPackets.cs | 946-949 | `S.CurrencyChanged`（CurrencyIndex+Amount，所有货币余额变化统一走它） |
| ServerLibrary/DBModels/UserItem.cs | 578-619 | `Price()` 卖店价公式、`RepairCost()` 修理费 |
| ServerLibrary/DBModels/UserCurrency.cs | 8-72 | `UserCurrency`：Info/Amount/Account，ToClientInfo 隐藏金额用 |
| ServerLibrary/DBModels/AuctionInfo.cs | 8-137 | 挂牌单：Account/Item/Character/Price/ConsignDate/Message + ToClientInfo |
| ServerLibrary/DBModels/AuctionHistoryInfo.cs | 6-91 | 成交历史：Info(物品Index)/PartIndex/SaleCount/LastPrice/Average[int[20]] |
| ServerLibrary/DBModels/GameStoreSale.cs | 9-119 | 商城消费记录：Item/Date/Price/Count/Account/HuntGold |
| ServerLibrary/DBModels/GameStoreFavourite.cs | 7-43 | 商城收藏：Account↔StoreInfo |
| ServerLibrary/DBModels/GameGoldPayment.cs | 6-142 | IPN 支付单：RawMessage/TransactionID/Status/Price/GameGoldAmount/Account |
| ServerLibrary/DBModels/AccountInfo.cs | 331-337 | `Gold/GameGold/HuntGold` 快捷属性（按 CurrencyType 从 Currencies 取） |
| ServerLibrary/DBModels/AccountInfo.cs | 626-681 | 账号创建：HuntGold buff（每分钟 tick）+ `AddDefaultCurrencies()` 补全所有货币 |
| ServerLibrary/Envir/Config.cs | 86-92 | BuyPrefix/BuyAddress/IPNPrefix/ReceiverEMail/ProcessGameGold/AllowBuyGameGold |
| ServerLibrary/Envir/SEnvir.cs | 1645-1792 | `ProcessGameGold()`：消费 IPN 队列、去重、入账、推荐返利（HuntGold×10%） |
| ServerLibrary/Envir/SEnvir.cs | 3530 | 新账号最高等级 ≥50 赠 500 HuntGold |
| ServerLibrary/Envir/WebServer.cs | 77-87/346-376 | Buy 页 HttpListener + IPN 监听；BuyConnection 返回充值 HTML |
| ServerLibrary/Envir/SConnection.cs | 38-39 | `MPSearchResults/VisibleResults`（搜索结果缓存，购买只允许买缓存内的单） |
| ServerLibrary/Envir/SConnection.cs | 915-1043 | `C.MarketPlaceHistory/Search/SearchIndex` 处理（过滤/排序/前 9 行/懒加载） |
| ServerLibrary/Models/PlayerObject.cs | 3927-4129 | `MarketPlaceConsign`（挂牌）/`MarketPlaceCancelConsign`（下架） |
| ServerLibrary/Models/PlayerObject.cs | 4131-4315 | `MarketPlaceBuy`（购买：扣款、7% 税、邮件发货、历史记录） |
| ServerLibrary/Models/PlayerObject.cs | 4317-4543 | `MarketPlaceStoreBuy`/`GameStoreFavouriteToggle`/`GameStoreGift` |
| ServerLibrary/Models/PlayerObject.cs | 4550-4623 | `GetGameStoreTopItems`（Top5 热销）/`BroadcastGameStoreTopItems`/`MarketPlaceCancelSuperior` |
| ServerLibrary/Models/PlayerObject.cs | 8394-8409 | `CurrencyChanged/GoldChanged/HuntGoldChanged/GameGoldChanged`（全部发 S.CurrencyChanged） |
| ServerLibrary/Models/PlayerObject.cs | 10208-10417 | `NPCBuy`（买入+行会基金）/`NPCSell`（批量卖店） |
| ServerLibrary/Models/PlayerObject.cs | 17294-17309 | `GetCurrency(ItemInfo)`（按 DropItem 反查）/`GetCurrency(CurrencyInfo)` |
| ServerLibrary/Models/MapObject.cs | 558-587 | HuntGold buff 每分钟 tick：征服战场地图直接 +1 赏金，否则攒 AvailableHuntGold（上限 15） |
| ServerLibrary/Models/PlayerObject.cs | 2083-2094 | 击杀标记怪（huntGold=true）时把 AvailableHuntGold 池转成 HuntGold 余额 |

## 核心流程

### 1. NPC 商店买入：PlayerObject.NPCBuy

入口 `SConnection.Process(C.NPCBuy)`（SConnection.cs:641-648）→ `PlayerObject.NPCBuy`（PlayerObject.cs:10208-10312）。结算货币取 NPC 页面配置，缺省金币：

```csharp
var currency = NPCPage.Currency ?? SEnvir.CurrencyInfoList.Binding.First(x => x.Type == CurrencyType.Gold);  // PlayerObject.cs:10212

long amountToBuy = good.NormaliseCurrencyPurchaseAmount(currency, p.Amount);   // 10222 货币商品按整捆取整

if (!good.IsCurrencyGood && amountToBuy > good.Item.StackSize) return;        // 10224 普通商品不能超堆叠

long cost = good.CostFor(currency, amountToBuy);                              // 10226
```

`NPCGood.CostFor`（LibraryCore/SystemModels/NPCInfo.cs:383-395）原文——**物品价格一律以金币价值存储，结算时除以货币汇率**：

```csharp
public long CostFor(CurrencyInfo currency, long amount)
{
    if (amount <= 0) return 0;

    decimal exchangeRate = currency?.ExchangeRate ?? 1M;

    if (exchangeRate <= 0M)
        exchangeRate = 1M;

    decimal cost = BaseCost * amount / exchangeRate;

    return Math.Max(1L, (long)Math.Ceiling(cost));
}
```

其中 `BaseCost = Item.Price * Rate`（Price≤0 时回退用货币商品自身 ExchangeRate，NPCInfo.cs:360-373）。

行会基金代付仅允许金币货币（10228-10232），且要求 `GuildPermission.FundsMerchant`（10241）；个人付款直接 `userCurrency.Amount -= cost; CurrencyChanged(userCurrency);`（10303-10305）。买入的装备类商品强制 `UserItemFlags.NonRefinable`（不可炼制，10263-10277）。

### 2. NPC 商店卖出：PlayerObject.NPCSell + UserItem.Price

入口 `SConnection.Process(C.NPCSell)`（SConnection.cs:649-654）→ `PlayerObject.NPCSell`（PlayerObject.cs:10314-10417）。

逐件校验（10351-10355）：`item.Info.CanSell`、非 Locked/Marriage/Worthless、且 ItemType 必须在该 BuySell 页的 `NPCPage.Types` 里。

**卖出价公式照抄**（PlayerObject.cs:10357-10358）：

```csharp
decimal exchangeRate = currency.ExchangeRate <= 0M ? 1M : currency.ExchangeRate;
var price = (long)(item.Price(link.Count) / exchangeRate);
```

`UserItem.Price(long count)`（ServerLibrary/DBModels/UserItem.cs:578-608）全文：

```csharp
public long Price(long count)
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

    p = p * (Stats.Count * 0.1M + 1M);

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
```

中文解读：

- `Worthless` 标记（商城购物/NPC 买入低等级号等场景打的）直接 0 价。
- 有耐久的装备：先按"满耐久磨损一半价值"建模 `p = MaxDurability × (Price/2/Durability)`，再按当前耐久比例在 `[p/2, p]` 区间线性插值，最后加上保底 `Price/2`——即**全新 = Price×1.0，耐久为 0 = Price/2**（近似）。
- 附加属性条数每条 +10%（`Stats.Count` 是 UserItem 上 AddedStats 类的条目数）。
- `SaleBonus5/10/15/20`：本次出售数量达到阈值给批量加成（互斥，取最大档）。
- 最终 `× count × Info.SellRate`：**SellRate 是物品级"卖店系数"**（ItemInfo.cs:253-256，System.db 可配，例如消耗品卖原价、任务物品 0 价）。
- 修理费对称公式 `RepairCost = (满耐久总价 × 属性系数 × Count − Price(Count)) × (special?2:1)`（UserItem.cs:609-619）。

客户端镜像 `ClientUserItem.Price()`（LibraryCore/Globals.cs:564-593）逐行相同（用 `AddedStats.Count`），Godot 的 InventoryDialog 卖出模式直接调它预估总价（见 GodotClient 现状）。

卖出成功：删除物品（10391-10398）、`userCurrency.Amount += amount`（10412）、`LogMilestone(ShopSell/CurrencyGain)`（10400/10414）。买/卖都有里程碑埋点 `MilestoneType.ShopPurchase/ShopSell`（PlayerObject.cs:10308/10400）。

### 3. 拍卖行：挂牌 MarketPlaceConsign

`PlayerObject.MarketPlaceConsign`（PlayerObject.cs:3927-4068）：

- 来源格限定 Inventory/Storage/PartsStorage/CompanionInventory；**Inventory 与 Companion 必须在安全区**（3944-3948/3960-3964）。
- 禁挂标记：`Bound / Marriage / NonRefinable`（3975-3977）；`p.Price <= 0` 拒绝——注释明示 **// Buy Out Less than 1**，一口价模式（3979）。
- **挂牌手续费**（3981）：`int cost = Globals.MarketPlaceFee;`（当前配置 = 0，LibraryCore/Globals.cs:109）。可个人金币或行会基金支付（3989-4027）。
- **挂牌数量上限**（3983）：

```csharp
if (Character.Account.Auctions.Count >= Character.Account.HighestLevel() * 3 + Character.Account.StorageSize - Globals.StorageSize)
```

即 最高等级×3 + 扩展仓库格数（StorageSize−100）。

- 部分数量挂牌时 `SEnvir.CreateFreshItem(item)` 克隆出独立 UserItem（4041-4045），随后创建 `AuctionInfo`：Account/Price/ConsignDate=Now/Item/Character/Message（4051-4060），并回 `S.MarketPlaceConsign`（4066）。

### 4. 拍卖行：购买 MarketPlaceBuy（7% 税 + 邮件发货）

`PlayerObject.MarketPlaceBuy`（PlayerObject.cs:4131-4315）：

- 只能买**本连接搜索结果缓存**里的单：`Connection.MPSearchResults.FirstOrDefault(x => x.Index == p.Index)`（4142）；不能买自己的（4152-4156）；库存不足拒绝（4158-4162）。
- 总价与扣款（4164-4206）：`cost = p.Count * info.Price`，个人 Gold 或行会基金（`GuildPermission.FundsMarket`）。
- **成交税**（4225）：

```csharp
long tax = (long)(cost * Globals.MarketPlaceTax);   // MarketPlaceTax = 0.07M（Globals.cs:125）
```

- 卖家收益走**邮件**：主题 "Listing Sale"，正文列 Buyer/Item/Price/Sub Total/Tax/Total，附金币 `gold.Count = cost - tax`（4221-4260）。买到的物品打 `UserItemFlags.Locked`（4263）；买家不在安全区/拿不下时同样转邮件（4265-4288）。
- **成交历史**（4297-4307）：

```csharp
AuctionHistoryInfo history = SEnvir.AuctionHistoryInfoList.Binding.FirstOrDefault(x => x.Info == itemInfo.Index && x.PartIndex == partIndex) ?? SEnvir.AuctionHistoryInfoList.CreateNewObject();

history.Info = itemInfo.Index;
history.PartIndex = partIndex;
history.SaleCount += p.Count;
history.LastPrice = info.Price;

for (int i = history.Average.Length - 2; i >= 0; i--)
    history.Average[i + 1] = history.Average[i];

history.Average[0] = info.Price; //Only care about the price per transaction
```

`Average` 是长度 20 的移位窗口（AuctionHistoryInfo.cs:84-89 `Average = new int[20]`），查询端把非零前缀求平均（SConnection.cs:933-942）。
- 里程碑：买家 `MarketPurchase`、卖家 `MarketSell`（4294-4295）。

**竞价机制：不存在。** 全链路只有 `Price` 一口价；`ConsignDate` 不参与任何过期逻辑（下架只能由卖家 `MarketPlaceCancelConsign`（PlayerObject.cs:4069-4129）或管理员批量 `MarketPlaceCancelSuperior`（4589-4623，撤销 40-56 级非部件装备挂牌）触发）。

### 5. 拍卖行：搜索 MarketPlaceSearch/SearchIndex/History

`SConnection.Process(C.MarketPlaceSearch)`（SConnection.cs:951-1028）：

1. 名称子串匹配（OrdinalIgnoreCase）收集 `matches`（ItemInfo Index 集合，960-974）；部件（`ItemEffect.ItemPart`）按其 `Stats[Stat.ItemIndex]` 指向的原物品匹配（982-990）。
2. 结果存 `MPSearchResults`，按 `MarketPlaceSort.Newest/Oldest/HighestPrice/LowestPrice` 排序（995-1009；Newest 即按 AuctionInfo.Index 倒序，最直观体现"无过期"）。
3. 首包只发前 9 行明细（1011-1020）；其余行由 `C.MarketPlaceSearchIndex` 懒加载（1029-1043，`VisibleResults` 防重）。
4. `C.MarketPlaceHistory` 返回 SaleCount/LastPrice/AveragePrice（915-943）。

### 6. 货币体系：CurrencyInfo / UserCurrency / 三大货币

`CurrencyInfo`（LibraryCore/SystemModels/CurrencyInfo.cs:5-112）字段：

| 字段 | 含义 |
|---|---|
| Name / Abbreviation | 名称/缩写（UI 显示"GG"等） |
| Type | `CurrencyType`；Gold/GameGold/HuntGold 三个被硬编码引用，Other/FP/CP 为扩展位 |
| Category | `CurrencyCategory`：Basic/Player/Event/Map/Other，仅用于钱包窗口分组展示（Godot CurrencyDialog 按 Category 分组，CurrencyDialog.cs:75-91） |
| DropItem | 该货币对应的"落地物品"（金币就是金币物品；可 nil）——货币可以以物品形态掉在地面/进背包 |
| ExchangeRate | 汇率：1 单位该货币 = ExchangeRate 金币价值；NPC 买卖与货币互换都除/乘它 |
| Images | `CurrencyInfoImage` 列表：按金额分段换图标（CurrencyDialog/DXItemCell 渲染用） |

余额存在 `UserCurrency`（ServerLibrary/DBModels/UserCurrency.cs:8-72），按账号隔离；`AccountInfo.AddDefaultCurrencies()` 保证账号拥有每个 CurrencyInfo 的记录（AccountInfo.cs:668-681）。所有余额变动统一以 `S.CurrencyChanged{CurrencyIndex,Amount}` 同步（PlayerObject.cs:8394-8409）；观察者模式下 GameGold 金额对观察者隐藏（PlayerObject.cs:862 `ToClientInfo(... observer)` → Amount=0，UserCurrency.cs:64-71）。

三大货币的获取/消费场景（均来自本会话读到的代码）：

| 货币 | 获取 | 消费 |
|---|---|---|
| Gold 金币 | 怪物掉落（金币直接入包并扣行会税，MonsterObject.cs:2862-2876）、NPC 卖出（10412）、拍卖行卖家邮件附件（4248-4249）、邮件领取、`C.CurrencyDrop` 捡拾 | NPC 买入、拍卖行购买、挂牌费、修理/分解/炼制、行会创建、随从领养 CompanionAdopt（按 CompanionInfo.Currency 结算，PlayerObject.cs:3272-3287） |
| GameGold 游戏币 | PayPal IPN 充值 `ProcessGameGold`（SEnvir.cs:1773-1775）、GM 命令 GiveGameGold | 游戏商城 `MarketPlaceStoreBuy`（4388）、GameStoreGift（4486-4489）、LootBox 重抽（客户端校验后发包） |
| HuntGold 赏金 | 征服战场地图每分钟 +1（MapObject.cs:570-577）；平时每分钟攒 1 点 AvailableHuntGold（上限 15，AccountInfo.cs:637），打"被标记"怪时池→余额（PlayerObject.cs:2083-2094）；推荐人返利 `GameGoldAmount/10`（SEnvir.cs:1781）；新号满 50 级赠 500（SEnvir.cs:3530） | 游戏商城 `UseHuntGold` 分支（4376）、GameStoreGift |

FP（CurrencyType.FP）用于声望/称号晋升消耗（NPCObject.cs:657-665 检查、PlayerObject.cs:12739-12765 扣费）。`CP/Other` 未在服务端找到任何使用点（仅枚举占位）。

**货币落地/捡拾**：`PlayerObject.CurrencyDrop`（8485-8516）把余额转成 `currency.DropItem` 物品丢地上（`CreateFreshItem` + `SetTemporary(true)`，无归属 Account，任何人可捡）；反向地，宠物捡到货币物品时直接折算进余额（Companion.cs:776-780）。

### 7. 游戏商城：MarketPlaceStoreBuy / 收藏 / 赠送 / Top5

`PlayerObject.MarketPlaceStoreBuy`（PlayerObject.cs:4317-4410）：

```csharp
p.Count = Math.Min(p.Count, info.Item.StackSize);      // 4338

long cost = p.Count;

int price = p.UseHuntGold ? (info.HuntGoldPrice == 0 ? info.Price : info.HuntGoldPrice) : info.Price;   // 4342

cost *= price;
```

- **双定价**：HuntGoldPrice=0 时 HuntGold 支付回退用 Price（4342）。
- 商品标记（4347-4356）：基础 `Worthless`（商城货不可卖店）+ `Locked`；`UseHuntGold` 或账号最高等级 <40 → `Bound`（绑定）；`StoreInfo.Duration>0` → `Expirable` 定时过期。
- `Config.TestServer` 下不扣费（4366-4392）。
- 成交写 `GameStoreSale`（Item/Account/Count/Price/HuntGold，4401-4407）并 `BroadcastGameStoreTopItems()`。

`GetGameStoreTopItems`（4550-4579）：按 `GameStoreSale` 分组求和销量降序、最近日期次之，取 5 个仍在售 StoreInfo；不足 5 个用随机在售商品补齐 → `S.GameStoreTopItems` 广播全服。登录时随 `S.GameStoreData`（Favourites+TopItems）一起下发（PlayerObject.cs:1115-1123）。

收藏 `GameStoreFavouriteToggle`（4412-4439）：有则删、无则建 `GameStoreFavourite`，回 `S.GameStoreFavouriteChanged`。
赠送 `GameStoreGift`（4441-4543）：校验收件人（角色名/屏蔽列表/邮箱容量 `Globals.MaxMailStorage`/不能送自己），扣费逻辑与购买一致（4485-4493），物品经邮件送达（4536 附近），同样打 Worthless/Locked/Bound 标记。

### 8. 充值：WebServer + IPN + ProcessGameGold

- `Config.BuyPrefix/BuyAddress/IPNPrefix`（Config.cs:87-89）：`WebServer` 起两个 HttpListener（WebServer.cs:77-87）。Buy 页按 `?Key=&Character=` 返回充值 HTML（346-376）；PayPal 回调进 `WebServer.Messages` 队列。
- `SEnvir.ProcessGameGold()`（SEnvir.cs:1645-1792）每秒消费队列（1582-1584）：校验金额映射 `WebServer.GoldTable[price] → GameGoldAmount`（1759-1762）、按 `TransactionID+Status` 去重（1687-1693）、写 `GameGoldPayment`、`character.Account.GameGold.Amount += payment.GameGoldAmount`（1773），推荐人 `referral.HuntGold.Amount += payment.GameGoldAmount / 10`（1781）。
- `GameGoldPayment` 是唯一充值流水表（含 RawMessage 原始 IPN 报文，GameGoldPayment.cs:8-142）。

## 数据结构/协议细节

### 封包一览（economy 相关）

| 方向 | 包 | 字段 | 处理函数 |
|---|---|---|---|
| C→S | NPCBuy | Index/Amount/GuildFunds（ClientPackets.cs:262-266） | PlayerObject.NPCBuy |
| C→S | NPCSell | Links（CellLinkInfo 列表） | PlayerObject.NPCSell |
| C→S | MarketPlaceConsign | Link/Price/GuildFunds/Message（438-446） | PlayerObject.MarketPlaceConsign |
| C→S | MarketPlaceCancelConsign | Index/Count（460-464） | PlayerObject.MarketPlaceCancelConsign |
| C→S | MarketPlaceBuy | Index/Count/GuildFunds（465-470） | PlayerObject.MarketPlaceBuy |
| C→S | MarketPlaceSearch | Name/ItemTypeFilter/ItemType/Sort（447-455） | SConnection.Process |
| C→S | MarketPlaceSearchIndex | Index（456-459） | SConnection.Process |
| C→S | MarketPlaceHistory | Index/PartIndex/Display（432-437） | SConnection.Process |
| C→S | MarketPlaceStoreBuy | Index/Count/UseHuntGold（471-476） | PlayerObject.MarketPlaceStoreBuy |
| C→S | GameStoreFavouriteToggle / GameStoreGift | Index / Index+Count+UseHuntGold+Recipient（478-486） | PlayerObject 对应方法 |
| C→S | CurrencyDrop | CurrencyIndex/Amount（201-204） | PlayerObject.CurrencyDrop |
| S→C | CurrencyChanged | CurrencyIndex/Amount（ServerPackets.cs:946-949） | 所有货币余额同步 |
| S→C | MarketPlaceSearch/SearchIndex/SearchCount | Count+Results / Index+Result / Count（861-876） | 客户端搜索页 |
| S→C | MarketPlaceConsign / ConsignChanged | Consignments / Index+Count（856-859/909-912） | 我的寄售页 |
| S→C | MarketPlaceBuy | Index/Count/Success（877-882） | 购买结果 |
| S→C | MarketPlaceHistory | Index/Display/SaleCount/LastPrice/AveragePrice（847-854） | 历史价窗口 |
| S→C | GameStoreData / TopItems / FavouriteChanged / Gift | Favourites+TopItems / Items / Index+Favourited / Result（887-907） | 商城页 |

注意：**不存在 `C.Auction*` 命名的包**，拍卖行协议全部叫 `MarketPlace*`（客户端 WinForms 场景 `MarketPlaceScene` 在 Godot 中对应 ConsignmentDialog）。

### 关键 DB 表关系

- `AccountInfo 1—N UserCurrency N—1 CurrencyInfo`（余额按账号，角色共享）。
- `AccountInfo 1—N AuctionInfo 1—1 UserItem`（挂牌中物品从背包移入 AuctionInfo.Item，取消/售出时回背包或邮件）。
- `AuctionHistoryInfo`：以 `(Info=ItemInfo.Index, PartIndex)` 为逻辑主键（部件用 PartIndex 区分），全服共享。
- `GameStoreSale`（Account 1—N，StoreSales 关联，AccountInfo.cs:594-595）只增不删，是 Top5 统计数据源。
- `GameGoldPayment`（Account 1—N，Payments 关联，AccountInfo.cs:591-592）。

## GodotClient 现状

| 功能 | 状态 | GodotClient 证据 |
|---|---|---|
| 寄售市场（搜索/我的寄售/购买/下架/排序） | 已移植 | Controls/ConsignmentDialog.cs:11-18（注释明确"服务器采用原版 MarketPlace* 包"）；搜索发送 GameScene.SendMarketSearch（ConsignmentDialog.cs:294-304）；购买确认→SendMarketBuy（464-473）；下架→SendMarketCancel（514-516）；挂牌弹窗含手续费 `Globals.MarketPlaceFee`（586-588）与"先 S.ItemChanged 再 S.MarketPlaceConsign"的时序处理（243-246）；懒加载 SearchIndex（451-453） |
| 成交历史窗口 | 已移植 | Controls/MarketHistoryDialog.cs:8-11（销量/最近成交/平均价）；事件接线 GameScene.cs:1259-1260 |
| 商城购买/赠送/收藏/Top5/充值入口 | 已移植 | Controls/GameStoreDialog.cs:11-33（分类/搜索/翻页/Top 栏/ HuntGold 切换 66-68）；购买确认 SendGameStoreBuy（322）、赠送 GameStoreGiftDialog（331，Controls/GameStoreGiftDialog.cs:8-33）、收藏 SendGameStoreFavourite（341）；充值按钮 OpenRechargePage（64）；发包 Network/ServerConnection.cs:1058-1063 |
| 钱包（多货币分组/分段图标/丢货币） | 已移植 | Controls/CurrencyDialog.cs:11-46（按 Category 分组折叠）、CurrencyRow 用 DropItem+Amount 渲染并按 Images 分段换图（164-176）；DXItemCell.CurrencyImage（Controls/DXItemCell.cs:417-424）；丢货币 ItemAmountDialog 分支 GameScene.cs:9956-9962 → SendCurrencyDrop（ServerConnection.cs:1095） |
| NPC 商店买 | 已移植 | Controls/NPCGoodsPanel.cs:55-75（SetGoods+货币+行会基金开关）、MaxAmountFor/NormaliseCurrencyPurchaseAmount 客户端预计算（300-333）、SendNPCBuy（336/353）；GameScene.cs:6521-6526 |
| NPC 商店卖（背包 SellMode） | 已移植 | Controls/InventoryDialog.cs:278-315（SellMode/NormalMode）；预估总价用客户端 `Item.Price(count)/ExchangeRate`（360-363，与服务端公式同构）；提交 SendNPCSell（392）；NPCDialog 触发（Controls/NPCDialog.cs:93-99） |
| 货币余额同步 | 已移植 | Network/ServerConnection.cs:286/360 CurrencyChangedEvent + 缓冲队列；GameScene OnCurrencyChanged 更新 Currencies |
| 登录商城数据 | 已移植 | ServerConnection.cs:240-243（GameStoreData/TopItems/FavouriteChanged/Gift 事件） |
| 邮件收货（拍卖/商城发货通道） | 已移植 | ServerConnection.cs:110-151 MailSendEvent 等；CommunicationDialog 金币附件校验（Controls/CommunicationDialog.cs:734-737/784-787） |
| 交易行会基金购买 | 已移植 | ConsignmentDialog.cs:208-209/471（_consignGuildFunds/_buyGuildFunds → SendMarketBuy guildFunds）；NPCGoodsPanel.cs:73-74/294（Gold 货币才显示基金开关） |
| GameGold 充值页面 | 部分移植 | GodotClient 仅有"打开充值页"按钮（GameStoreDialog.cs:63-64 OpenRechargePage）；无 IPN 相关逻辑（本就属服务端/Web） |

## 移植注意事项

1. **价格计算双端同构**：`UserItem.Price()`（服务端）与 `ClientUserItem.Price()`（客户端）必须同步改——Godot 的 InventoryDialog.SaleTotal 直接用客户端版本预估；若只改一端会出现"预估价≠实收价"。`Info == null` 判断只存在于服务端版（UserItem.cs:580），客户端版没有。
2. **购买拍卖行物品必须先搜索**：服务端只认 `Connection.MPSearchResults` 里的 Index（PlayerObject.cs:4142）。Godot ConsignmentDialog 已保留"按服务器索引留空位、不压缩结果数组"的语义（ConsignmentDialog.cs:334-341），移植其它列表 UI 时勿照搬"过滤后重排"。
3. **货币即物品**：任何 `CurrencyInfo.DropItem` 对应的物品都是货币形态，渲染要用 `CurrencyImage()` 分段图标（DXItemCell.cs:417-424、ObjectRenderer.cs:244-251 同款），堆叠合并规则也与普通物品不同（DXItemCell.cs:389 注释）。
4. **观察者模式**下 GameGold 余额被服务端置 0（PlayerObject.cs:862），客户端不要据此显示"余额清零"告警。
5. `MarketPlaceFee=0 / MarketPlaceTax=0.07M` 写死在 `LibraryCore/Globals.cs:109/125`（编译期常量，非 Config），客户端确认弹窗文案（ConsignmentDialog.cs:586-587）引用同一常量，改税率时两端同步。
6. 拍卖行**没有到期机制**：客户端 Newest/Oldest 排序直译为 Index 升降序（SConnection.cs:998-1001），做 UI 时不要自作主张加"剩余时间"列。
7. 邮件是拍卖行/商城的唯一实物交割通道（含金币附件），客户端邮件附件领取流程（HasItem/Items）必须完整，否则卖家收不到钱。
8. `S.CurrencyChanged` 是余额唯一增量同步包；Gold/HuntGold/GameGold 三个帮助方法只是包了一层（PlayerObject.cs:8398-8409），客户端应以 `CurrencyIndex` 为主键维护字典而非特判三货币。
9. 行会基金购买在 NPC 商店与拍卖行是**两套权限位**（`FundsMerchant` vs `FundsMarket`，PlayerObject.cs:10241/4175），UI 提示语也要分开（NPCFundsBuy vs ConsignBuyGuildFundsUsed）。
