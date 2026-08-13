# 交易、寄售拍卖、个人仓库与丢弃拾取（Consign & Trade）

## TL;DR 速查表

- 面对面交易 6 个 C 包：`C.TradeRequest / TradeRequestResponse / TradeClose / TradeAddItem / TradeAddGold / TradeConfirm`（`LibraryCore/Network/ClientPackets.cs:527-533` 等），服务端入口 `ServerLibrary/Envir/SConnection.cs:1151-1189`。
- 交易邀请的硬条件：双方面对面（距离 1 格 + 朝向相反，`Functions.ShiftDirection(Direction, 4)`），见 `ServerLibrary/Models/PlayerObject.cs:9669-9685`、`9729-9731`。
- 防作弊核心：确认阶段重新校验**源格子物品引用未变**（`fromArray[pair.Value.Slot] != pair.Key` 即中断，`ServerLibrary/Models/PlayerObject.cs:9925-9932`）+ 双向 `CanGainItems` 容量预检 + 金币差额可为负校验。
- 寄售 = `AuctionInfo`（`ServerLibrary/DBModels/AuctionInfo.cs:8`），按**账号**挂单；上架手续费 `Globals.MarketPlaceFee = 0`（现配置免费），成交税 `Globals.MarketPlaceTax = 0.07M`（7%），货款以金币**邮件**交付（`ServerLibrary/Models/PlayerObject.cs:4221-4253`）。
- 本引擎**没有摆摊/个人商店**——所有玩家间寄售走全局拍卖行（MarketPlace），商城是独立的 `MarketPlaceStoreBuy`（GameStore）。
- 个人仓库是**账号级**：`PlayerObject.Storage = new UserItem[1000]`（`ServerLibrary/Models/PlayerObject.cs:153`），初始可用 `Account.StorageSize = Globals.StorageSize = 100`（`ServerLibrary/DBModels/AccountInfo.cs:630`、`LibraryCore/Globals.cs:301`），用道具"扩容券"（ItemEffect 17）+10 扩容（`ServerLibrary/Models/PlayerObject.cs:6791-6803`），存取走通用 `C.ItemMove` 且**必须安全区**。
- 丢弃 `C.ItemDrop`（绑定物品丢弃后只有本人能捡）、拾取 `C.PickUp`（环形扫描 `Stat.PickUpRadius`）；地面归属规则在 `ItemObject.CanPickUpItem`（`ServerLibrary/Models/ItemObject.cs:53-80`）：本人随时、同组 2 分钟、同会 5 分钟、他人 10 分钟。

## 职责概述

本文覆盖玩家间物品/金币流转的四个子系统：

1. **面对面交易（Trade）**：两名玩家互相锁定/确认后原子交换物品与金币，状态全部保存在 `PlayerObject` 内存字段（`TradePartner/TradeItems/TradeGold/TradeConfirmed`，`ServerLibrary/Models/PlayerObject.cs:163-166`），不落库、断线即作废。
2. **寄售拍卖（MarketPlace / Auction）**：`AuctionInfo`（Users.db，按 `AccountInfo.Auctions` 关联）是全服共享拍卖行；挂牌从背包/仓库/宠物背包扣物品，成交走金币邮件+税收，取回失败也走邮件。搜索结果缓存在 `SConnection.MPSearchResults`（`ServerLibrary/Envir/SConnection.cs:38`）。
3. **个人仓库（Storage/PartsStorage）**：账号级 1000 格数组，`StorageSize` 控制可用上限；碎片仓库 `PartsStorage` 用 `Globals.PartsStorageOffset = 2000` 的 Slot 偏移共用同一数组（`ServerLibrary/Models/PlayerObject.cs:197-205`）。另有独立的行会仓库 `GridType.GuildStorage`（本文只附带提及）。
4. **丢弃/拾取（ItemDrop/PickUp）**：地面物品是 `ItemObject`（MapObject），带 `Account` 归属、`MonsterDrop` 标记、`ExpireTime` 过期时间，归属宽限期按组/会递进开放。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| `ServerLibrary/Models/PlayerObject.cs` | 163-166 | 交易内存状态字段 |
| `ServerLibrary/Models/PlayerObject.cs` | 9634-9654 | TradeClose：清理双方状态 |
| `ServerLibrary/Models/PlayerObject.cs` | 9656-9726 | TradeRequest：邀请与全部前置校验 |
| `ServerLibrary/Models/PlayerObject.cs` | 9727-9738 | TradeAccept：双向绑定开窗 |
| `ServerLibrary/Models/PlayerObject.cs` | 9740-9825 | TradeAddItem：放物品上桌（15 格上限） |
| `ServerLibrary/Models/PlayerObject.cs` | 9826-9848 | TradeAddGold：放金币 |
| `ServerLibrary/Models/PlayerObject.cs` | 9850-10154 | TradeConfirm：确认→校验→原子交换 |
| `ServerLibrary/Envir/SConnection.cs` | 1151-1189 | 6 个交易 C 包分发 |
| `ServerLibrary/DBModels/AuctionInfo.cs` | 8-137 | 拍卖挂单模型 + ToClientInfo |
| `ServerLibrary/DBModels/AuctionHistoryInfo.cs` | — | 成交历史（均价数组） |
| `ServerLibrary/Models/PlayerObject.cs` | 3927-4068 | MarketPlaceConsign：挂牌 |
| `ServerLibrary/Models/PlayerObject.cs` | 4069-4129 | MarketPlaceCancelConsign：取回 |
| `ServerLibrary/Models/PlayerObject.cs` | 4131-4315 | MarketPlaceBuy：购买全流程 |
| `ServerLibrary/Models/PlayerObject.cs` | 4589-4623 | MarketPlaceCancelSuperior：道具调整批量下架 |
| `ServerLibrary/Envir/SConnection.cs` | 951-1049 | 拍卖搜索（服务端过滤+排序+分页） |
| `ServerLibrary/Models/PlayerObject.cs` | 152-154, 197-205 | Storage/PartsStorage 数组与加载 |
| `ServerLibrary/DBModels/AccountInfo.cs` | 462-475, 570-571 | StorageSize 字段与 Auctions 关联 |
| `ServerLibrary/Models/PlayerObject.cs` | 6791-6803 | 道具扩容仓库（ItemEffect 17） |
| `ServerLibrary/Models/PlayerObject.cs` | 7435-7974 | ItemMove：跨格移动（仓库存取共用） |
| `ServerLibrary/Models/PlayerObject.cs` | 8411-8484 | ItemDrop：丢弃 |
| `ServerLibrary/Models/PlayerObject.cs` | 8582-8616 | PickUp：环形扫描拾取 |
| `ServerLibrary/Models/ItemObject.cs` | 11-218 | 地面物品：过期/归属/拾取/可见性 |
| `ServerLibrary/Envir/Config.cs` | 129-145 | DropDuration/DropDistance/DropVisibleOtherPlayers |
| `LibraryCore/Globals.cs` | 108-125, 300-302 | MarketPlaceFee/MarketPlaceTax/StorageSize 常量 |
| `LibraryCore/Network/ClientPackets.cs` | 527-533, 438-475, 195-198 | 交易/拍卖/丢弃包定义 |
| `LibraryCore/Network/ServerPackets.cs` | 965-996 | 8 个交易 S 包 |
| `Client/Scenes/Views/TradeDialog.cs` | — | 原版交易窗口 |

## 核心流程

### 1. 面对面交易：邀请 → 开窗 → 上桌 → 确认 → 交换

#### 1.1 邀请（TradeRequest，照抄）

```csharp
// ServerLibrary/Models/PlayerObject.cs:9656-9726
public void TradeRequest()
{
    if (TradePartner != null)
    {
        Connection.ReceiveChatWithObservers(con => con.Language.TradeAlreadyTrading, MessageType.System);
        return;
    }
    if (TradePartnerRequest != null)
    {
        Connection.ReceiveChatWithObservers(con => con.Language.TradeAlreadyHaveRequest, MessageType.System);
        return;
    }

    Cell cell = CurrentMap.GetCell(Functions.Move(CurrentLocation, Direction));

    if (cell?.Objects == null) return;

    PlayerObject player = null;
    foreach (MapObject ob in cell.Objects)
    {
        if (ob.Race != ObjectType.Player) continue;
        player = (PlayerObject)ob;
        break;
    }

    if (player == null || player.Direction != Functions.ShiftDirection(Direction, 4))
    {
        Connection.ReceiveChatWithObservers(con => con.Language.TradeNeedFace, MessageType.System);
        return;
    }

    if (SEnvir.IsBlocking(Character.Account, player.Character.Account))
    {
        Connection.ReceiveChatWithObservers(con => string.Format(con.Language.TradeTargetNotAllowed, player.Character.CharacterName), MessageType.System);
        return;
    }

    if (player.TradePartner != null)
    {
        Connection.ReceiveChatWithObservers(con => string.Format(con.Language.TradeTargetAlreadyTrading, player.Character.CharacterName), MessageType.System);
        return;
    }

    if (player.TradePartnerRequest != null)
    {
        Connection.ReceiveChatWithObservers(con => string.Format(con.Language.TradeTargetAlreadyHaveRequest, player.Character.CharacterName), MessageType.System);
        return;
    }

    if (!player.Character.Account.AllowTrade)
    {
        Connection.ReceiveChatWithObservers(con => string.Format(con.Language.TradeTargetNotAllowed, player.Character.CharacterName), MessageType.System);
        player.Connection.ReceiveChatWithObservers(con => string.Format(con.Language.TradeNotAllowed, Character.CharacterName), MessageType.System);
        return;
    }

    if (player.Dead || Dead)
    {
        Connection.ReceiveChatWithObservers(con => con.Language.TradeTargetDead, MessageType.System);
        return;
    }

    player.TradePartnerRequest = this;
    player.Enqueue(new S.TradeRequest { Name = Name, ObserverPacket = false });

    Connection.ReceiveChatWithObservers(con => string.Format(con.Language.TradeRequested, player.Character.CharacterName), MessageType.System);
}
```

邀请目标 = **面前一格**（`Functions.Move(CurrentLocation, Direction)`）里的第一个玩家；`Functions.ShiftDirection(Direction, 4)` 即反方向，要求对方面朝自己。黑名单（`SEnvir.IsBlocking`）与对方账号的 `AllowTrade` 开关都会拦截。

#### 1.2 接受（TradeRequestResponse → TradeAccept，照抄）

```csharp
// ServerLibrary/Envir/SConnection.cs:1157-1165
public void Process(C.TradeRequestResponse p)
{
    if (Stage != GameStage.Game) return;

    if (p.Accept)
        Player.TradeAccept();

    Player.TradePartnerRequest = null;    // 无论接受与否都清邀请
}

// ServerLibrary/Models/PlayerObject.cs:9727-9738
public void TradeAccept()
{
    if (TradePartnerRequest?.Node == null || TradePartnerRequest.TradePartner != null || TradePartnerRequest.Dead ||
        Functions.Distance(CurrentLocation, TradePartnerRequest.CurrentLocation) != 1 || TradePartnerRequest.Direction != Functions.ShiftDirection(Direction, 4))
        return;

    TradePartner = TradePartnerRequest;
    TradePartnerRequest.TradePartner = this;

    TradePartner.Enqueue(new S.TradeOpen { Name = Name });
    Enqueue(new S.TradeOpen { Name = TradePartner.Name });
}
```

接受时**二次校验**：邀请者仍在线（`Node != null`）、未和别人交易、未死、距离仍为 1、朝向仍相对——防止邀请后跑开的时序作弊。双方 `TradePartner` 互相绑定，各发 `S.TradeOpen`。

#### 1.3 上桌：物品（TradeAddItem，照抄）

```csharp
// ServerLibrary/Models/PlayerObject.cs:9740-9825（网格 switch 节选，含 Storage 安全区检查 9766-9785）
public void TradeAddItem(CellLinkInfo cell)
{
    S.TradeAddItem result = new S.TradeAddItem
    {
        Cell = cell,
    };

    Enqueue(result);

    if (!ParseLinks(cell) || TradePartner == null || TradeItems.Count >= 15) return;

    UserItem[] fromArray;

    switch (cell.GridType)
    {
        case GridType.Inventory:       fromArray = Inventory;             break;
        case GridType.Equipment:       fromArray = Equipment;             break;
        case GridType.CompanionInventory:
            if (Companion == null) return;
            fromArray = Companion.Inventory;
            break;
        case GridType.PartsStorage:
            if (!InSafeZone && !Character.Account.TempAdmin)
            {
                Connection.ReceiveChatWithObservers(con => con.Language.StorageSafeZone, MessageType.System);
                return;
            }
            fromArray = PartsStorage;
            break;
        case GridType.Storage:
            if (!InSafeZone && !Character.Account.TempAdmin)
            {
                Connection.ReceiveChatWithObservers(con => con.Language.StorageSafeZone, MessageType.System);
                return;
            }
            fromArray = Storage;
            break;
        default:
            return;
    }

    if (cell.Slot < 0 || cell.Slot >= fromArray.Length) return;

    UserItem fromItem = fromArray[cell.Slot];

    if (fromItem == null || cell.Count > fromItem.Count || (!TradePartner.Character.Account.IsAdmin(true) && !Character.Account.IsAdmin(true) && ((fromItem.Flags & UserItemFlags.Bound) == UserItemFlags.Bound || !fromItem.Info.CanTrade))) return;
    if ((fromItem.Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage) return;

    if (TradeItems.ContainsKey(fromItem)) return;

    //All is Well
    result.Success = true;
    TradeItems[fromItem] = cell;
    S.TradeItemAdded packet = new S.TradeItemAdded
    {
        Item = fromItem.ToClientInfo()
    };
    packet.Item.Count = cell.Count;
    TradePartner.Enqueue(packet);
}
```

要点：上限 **15 件**；绑定（`Bound`）/不可交易（`!CanTrade`）/结婚戒指（`Marriage`）物品禁止上桌（管理员豁免）；从仓库上桌必须安全区。注意此阶段物品**不转移、不锁定**，只记录 `UserItem 引用 → CellLinkInfo` 映射——真正的防调包在确认阶段。

#### 1.4 上桌：金币（TradeAddGold，照抄）

```csharp
// ServerLibrary/Models/PlayerObject.cs:9826-9848
public void TradeAddGold(long gold)
{
    S.TradeAddGold p = new S.TradeAddGold
    {
        Gold = TradeGold,
    };
    Enqueue(p);

    if (TradePartner == null || TradeGold >= gold) return;   // 只增不减；降额要先 TradeClose

    if (gold <= 0 || gold > Gold.Amount) return;

    TradeGold = gold;
    p.Gold = TradeGold;

    //All is Well
    S.TradeGoldAdded packet = new S.TradeGoldAdded
    {
        Gold = TradeGold,
    };

    TradePartner.Enqueue(packet);
}
```

金币同样**不预扣**，只记录意向值 `TradeGold`，余额校验在确认阶段。

#### 1.5 确认与原子交换（TradeConfirm，照抄核心）

```csharp
// ServerLibrary/Models/PlayerObject.cs:9850-9891（第一段：双向确认 + 金币余额）
public void TradeConfirm()
{
    if (TradePartner == null) return;

    TradeConfirmed = true;

    if (!TradePartner.TradeConfirmed)      // 先确认的一方等待
    {
        Connection.ReceiveChatWithObservers(con => con.Language.TradeWaiting, MessageType.System);
        TradePartner.Connection.ReceiveChatWithObservers(con => con.Language.TradePartnerWaiting, MessageType.System);
        return;
    }

    long gold = Gold.Amount;
    gold += TradePartner.TradeGold - TradeGold;

    if (gold < 0)                          // 自己付不出差额
    {
        Connection.ReceiveChatWithObservers(con => con.Language.TradeNoGold, MessageType.System);
        TradePartner.Connection.ReceiveChatWithObservers(con => con.Language.TradePartnerNoGold, MessageType.System);
        TradeClose();
        return;
    }

    gold = TradePartner.Gold.Amount;
    gold += TradeGold - TradePartner.TradeGold;

    if (gold < 0)                          // 对方付不出差额
    {
        ...TradeClose();
        return;
    }
```

```csharp
// ServerLibrary/Models/PlayerObject.cs:9889-9967（第二段：逐件防调包校验 + 对方容量预检）
    List<ItemCheck> checks = new List<ItemCheck>();

    foreach (KeyValuePair<UserItem, CellLinkInfo> pair in TradeItems)
    {
        UserItem[] fromArray;
        switch (pair.Value.GridType) { /* Inventory/Equipment/PartsStorage/Storage/CompanionInventory 同上 */ }

        if (fromArray[pair.Value.Slot] != pair.Key || pair.Key.Count < pair.Value.Count)
        {                                  // ★ 格子里的物品引用变了（被调包/移动）→ 整单终止
            Connection.ReceiveChatWithObservers(con => con.Language.TradeFailedItemsChanged, MessageType.System);
            TradePartner.Connection.ReceiveChatWithObservers(con => string.Format(con.Language.TradeFailedPartnerItemsChanged, Name), MessageType.System);
            TradeClose();
            return;
        }

        UserItem item = fromArray[pair.Value.Slot];

        if ((item.Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage) return;
        ... // 同 Info/同 Flags 的可堆叠物品合并进一个 ItemCheck
        checks.Add(new ItemCheck(item, pair.Value.Count, item.Flags, item.ExpireTime));
    }

    if (!TradePartner.CanGainItems(false, checks.ToArray()))
    {                                      // 对方装不下 → 解锁对方让其腾格子
        Connection.ReceiveChatWithObservers(con => con.Language.TradeWaiting, MessageType.System);
        TradePartner.Connection.ReceiveChatWithObservers(con => con.Language.TradeNotEnoughSpace, MessageType.System);
        TradePartner.TradeConfirmed = false;
        TradePartner.Enqueue(new S.TradeUnlock());
        return;
    }
```

```csharp
// ServerLibrary/Models/PlayerObject.cs:10049-10153（第三段：交换执行 + 金币差额结算）
    Enqueue(new S.ItemsChanged { Links = TradeItems.Values.ToList(), Success = true });

    //Deal Successful, Both can accept items without issues so send away
    UserItem tempItem;

    foreach (KeyValuePair<UserItem, CellLinkInfo> pair in TradeItems)
    {
        if (pair.Key.Count > pair.Value.Count)          // 部分数量：拆一叠新的给对方
        {
            pair.Key.Count -= pair.Value.Count;
            tempItem = SEnvir.CreateFreshItem(pair.Key);
            tempItem.Count = pair.Value.Count;
            TradePartner.GainItem(tempItem);
            continue;
        }

        UserItem[] fromArray;
        switch (pair.Value.GridType) { /* 同上 */ }

        fromArray[pair.Value.Slot] = null;
        RemoveItem(pair.Key);
        TradePartner.GainItem(pair.Key);                // 整件转移
    }
    TradePartner.Enqueue(new S.ItemsChanged { Links = TradePartner.TradeItems.Values.ToList(), Success = true });

    foreach (KeyValuePair<UserItem, CellLinkInfo> pair in TradePartner.TradeItems)
    { ... GainItem(pair.Key); ... }                     // 反方向同理

    RefreshStats();
    SendShapeUpdate();
    TradePartner.RefreshStats();
    TradePartner.SendShapeUpdate();

    Gold.Amount += TradePartner.TradeGold - TradeGold;   // 金币净额结算（手续费为零）
    GoldChanged();

    TradePartner.Gold.Amount += TradeGold - TradePartner.TradeGold;
    TradePartner.GoldChanged();

    LogMilestone(MilestoneType.Trade, 1);
    TradePartner.LogMilestone(MilestoneType.Trade, 1);

    Connection.ReceiveChatWithObservers(con => con.Language.TradeComplete, MessageType.System);
    TradePartner.Connection.ReceiveChatWithObservers(con => con.Language.TradeComplete, MessageType.System);

    TradeClose();
}
```

**防作弊清单**（全部在确认阶段二次校验）：
1. 距离/朝向：`TradeAccept` 里 `Functions.Distance(...) != 1` + 反向朝向（`ServerLibrary/Models/PlayerObject.cs:9729-9731`）。
2. 调包：`fromArray[pair.Value.Slot] != pair.Key`（9925-9932 与 10006-10013 双向各查一次）——上桌后把格子里的物品换掉/叠掉都会让引用失配，整单 `TradeClose`。
3. 数量不足：`pair.Key.Count < pair.Value.Count`。
4. 金币余额：双向 `gold < 0` 模拟（9864-9887）。
5. 容量：`CanGainItems(false, checks)` 双向预检（9959 与 10039），不足则单侧解锁重试。
6. 黑名单/AllowTrade/死亡在邀请时拦截。

#### 1.6 关闭（TradeClose，照抄）

```csharp
// ServerLibrary/Models/PlayerObject.cs:9636-9654
public void TradeClose()
{
    if (TradePartner == null) return;

    Enqueue(new S.TradeClose());

    if (TradePartner?.Node != null)
        TradePartner.Enqueue(new S.TradeClose());

    TradePartner.TradePartner = null;
    TradePartner.TradeItems.Clear();
    TradePartner.TradeGold = 0;
    TradePartner.TradeConfirmed = false;

    TradePartner = null;
    TradeItems.Clear();
    TradeGold = 0;
    TradeConfirmed = false;
}
```

任何一方关窗/断线都会走 `TradeClose`；因为物品金币从未预转移，关闭即无损回滚。

### 2. 寄售拍卖（MarketPlace / AuctionInfo）

#### 2.1 挂牌（MarketPlaceConsign，照抄核心）

```csharp
// ServerLibrary/Models/PlayerObject.cs:3927-3968（来源网格与安全区）
public void MarketPlaceConsign(C.MarketPlaceConsign p)
{
    S.ItemChanged result = new S.ItemChanged { Link = p.Link };
    Enqueue(result);

    if (!ParseLinks(p.Link)) return;

    if (p.Message != null && p.Message.Length > 150) return;

    UserItem[] array;
    switch (p.Link.GridType)
    {
        case GridType.Inventory:
            array = Inventory;
            if (!InSafeZone && !Character.Account.TempAdmin)
            {
                Connection.ReceiveChatWithObservers(con => con.Language.ConsignSafeZone, MessageType.System);
                return;
            }
            break;
        case GridType.PartsStorage: array = PartsStorage; break;
        case GridType.Storage:      array = Storage;      break;
        case GridType.CompanionInventory:
            if (Companion == null) return;
            array = Companion.Inventory;
            if (!InSafeZone && !Character.Account.TempAdmin) { ...ConsignSafeZone... return; }
            break;
        default: return;
    }
```

```csharp
// ServerLibrary/Models/PlayerObject.cs:3970-3987（物品校验 + 挂单量上限）
    if (p.Link.Slot < 0 || p.Link.Slot >= array.Length) return;
    UserItem item = array[p.Link.Slot];

    if (item == null || p.Link.Count > item.Count) return; //trying to sell more than owned.

    if ((item.Flags & UserItemFlags.Bound) == UserItemFlags.Bound) return;
    if ((item.Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage) return;
    if ((item.Flags & UserItemFlags.NonRefinable) == UserItemFlags.NonRefinable) return;

    if (p.Price <= 0) return; // Buy Out Less than 1

    int cost = Globals.MarketPlaceFee;

    if (Character.Account.Auctions.Count >= Character.Account.HighestLevel() * 3 + Character.Account.StorageSize - Globals.StorageSize)
    {
        Connection.ReceiveChatWithObservers(con => con.Language.ConsignLimit, MessageType.System);
        return;
    }
```

```csharp
// ServerLibrary/Models/PlayerObject.cs:4017-4027（手续费扣款）+ 4029-4067（扣物建档）
    else
    {
        if (cost > Gold.Amount) { ...ConsignCost... return; }

        Gold.Amount -= cost;
        GoldChanged();
    }

    UserItem auctionItem;

    if (p.Link.Count == item.Count)
    {
        auctionItem = item;
        RemoveItem(item);
        array[p.Link.Slot] = null;
        result.Link.Count = 0;
    }
    else
    {
        auctionItem = SEnvir.CreateFreshItem(item);   // 部分数量拆叠
        auctionItem.Count = p.Link.Count;
        item.Count -= p.Link.Count;
        result.Link.Count = item.Count;
    }

    RefreshWeight();
    Companion?.RefreshWeight();

    AuctionInfo auction = SEnvir.AuctionInfoList.CreateNewObject();

    auction.Account = Character.Account;
    auction.Price = p.Price;
    auction.ConsignDate = SEnvir.Now;
    auction.Item = auctionItem;
    auction.Character = Character;
    auction.Message = p.Message ?? string.Empty;

    result.Success = true;

    LogMilestone(MilestoneType.MarketConsign, auctionItem.Count, item: auctionItem.Info);

    Enqueue(new S.MarketPlaceConsign { Consignments = new List<ClientMarketPlaceInfo> { auction.ToClientInfo(Character.Account) }, ObserverPacket = false });
    Connection.ReceiveChatWithObservers(con => con.Language.ConsignComplete, MessageType.System);
}
```

- 手续费 `Globals.MarketPlaceFee = 0`（`LibraryCore/Globals.cs:109`，即当前配置上架免费），可走行会资金（需 `GuildPermission.FundsMarket`，4011-4015 全会广播）。
- 挂单量上限 = `最高角色等级 × 3 + (StorageSize − 100)`——**仓库扩容同时提高拍卖行容量**（3983）。
- `AuctionInfo` 按账号挂（`auction.Account`），记录 `Character`（卖家角色名展示用）、`Price`（单价 int）、`ConsignDate`、`Message`（≤150 字备注）（`ServerLibrary/DBModels/AuctionInfo.cs:57-100`）。

#### 2.2 购买（MarketPlaceBuy，照抄核心）

```csharp
// ServerLibrary/Models/PlayerObject.cs:4131-4166（定位挂单 + 防自买）
public void MarketPlaceBuy(C.MarketPlaceBuy p)
{
    if (p.Count <= 0) return;
    ...
    AuctionInfo info = Connection.MPSearchResults.FirstOrDefault(x => x.Index == p.Index);

    if (info == null) return;

    if (info.Item == null) { ...ConsignAlreadySold... return; }

    if (info.Account == Character.Account && !Character.Account.TempAdmin)
    {
        Connection.ReceiveChatWithObservers(con => con.Language.ConsignBuyOwnItem, MessageType.System);
        return;
    }

    if (info.Item.Count < p.Count) { ...ConsignNotEnough... return; }

    long cost = p.Count;

    cost *= info.Price;
```

```csharp
// ServerLibrary/Models/PlayerObject.cs:4204-4225（扣款 + 税）→ 4248-4260（货款金币邮件）
    Gold.Amount -= cost;                       // 或行会资金分支 4168-4195
    GoldChanged();

    UserItem item = info.Item;

    if (info.Item.Count > p.Count)
    {
        info.Item.Count -= p.Count;
        item = SEnvir.CreateFreshItem(info.Item);
        item.Count = p.Count;
    }
    else
        info.Item = null;

    MailInfo mail = SEnvir.MailInfoList.CreateNewObject();
    mail.Account = info.Account;

    long tax = (long)(cost * Globals.MarketPlaceTax);   // 7%

    mail.Subject = "Listing Sale";
    mail.Sender = "Market Place";
    ... // 邮件正文含买家/单价/小计/税/净额

    UserItem gold = SEnvir.CreateFreshItem(SEnvir.GoldInfo);
    gold.Count = (long)(cost - tax);

    gold.Mail = mail;
    gold.Slot = 0;
    mail.HasItem = true;
```

```csharp
// ServerLibrary/Models/PlayerObject.cs:4263-4288（买家收货：锁绑定 + 背包不足转邮件）
    item.Flags |= UserItemFlags.Locked;

    if (!InSafeZone || !CanGainItems(false, new ItemCheck(item, item.Count, item.Flags, item.ExpireTime)))
    {
        mail = SEnvir.MailInfoList.CreateNewObject();      // 非安全区或背包满 → 物品邮件
        mail.Account = Character.Account;
        mail.Subject = "Item Purchase";
        ...
    }
    else
    {
        GainItem(item);
    }
```

```csharp
// ServerLibrary/Models/PlayerObject.cs:4290-4314（结果包 + 成交历史 + 下架）
    result.Index = info.Index;
    result.Count = info.Item?.Count ?? 0;
    result.Success = true;

    LogMilestone(MilestoneType.MarketPurchase, p.Count, item: itemInfo);
    SEnvir.LogMilestone(info.Character, MilestoneType.MarketSell, p.Count, item: itemInfo);

    AuctionHistoryInfo history = SEnvir.AuctionHistoryInfoList.Binding.FirstOrDefault(x => x.Info == itemInfo.Index && x.PartIndex == partIndex) ?? SEnvir.AuctionHistoryInfoList.CreateNewObject();
    history.Info = itemInfo.Index;
    history.PartIndex = partIndex;
    history.SaleCount += p.Count;
    history.LastPrice = info.Price;

    for (int i = history.Average.Length - 2; i >= 0; i--)
        history.Average[i + 1] = history.Average[i];

    history.Average[0] = info.Price; //Only care about the price per transaction

    if (info.Account.Connection?.Player != null)
        info.Account.Connection.Enqueue(new S.MarketPlaceConsignChanged { Index = info.Index, Count = info.Item?.Count ?? 0, ObserverPacket = false, });

    if (info.Item == null)
        info.Delete();
```

购买关键事实：
- 只能买**自己搜索结果**里的挂单（`Connection.MPSearchResults`），防凭空索引购买。
- 卖家货款 = `单价×数量×(1−7%)` 以金币物品邮件发放；买家买到的物品加 `UserItemFlags.Locked`。
- `AuctionHistoryInfo` 滚动记录成交均价数组，供市场历史查询（`C.MarketPlaceHistory`，`ServerLibrary/Envir/SConnection.cs:915-943`）。

#### 2.3 取回（MarketPlaceCancelConsign，照抄核心）

```csharp
// ServerLibrary/Models/PlayerObject.cs:4069-4129
public void MarketPlaceCancelConsign(C.MarketPlaceCancelConsign p)
{
    if (p.Count <= 0) return;

    AuctionInfo info = Character.Account.Auctions?.FirstOrDefault(x => x.Index == p.Index);

    if (info == null) return;

    if (info.Item == null) { ...ConsignAlreadySold... return; }

    if (info.Item.Count < p.Count) { ...ConsignNotEnough... return; }

    UserItem item = info.Item;

    if (info.Item.Count > p.Count)
    {
        info.Item.Count -= p.Count;
        item = SEnvir.CreateFreshItem(info.Item);
        item.Count = p.Count;
    }
    else
        info.Item = null;

    if (!InSafeZone || !CanGainItems(false, new ItemCheck(item, item.Count, item.Flags, item.ExpireTime)))
    {
        MailInfo mail = SEnvir.MailInfoList.CreateNewObject();   // 非安全区/背包满 → 邮件
        mail.Account = Character.Account;
        mail.Subject = "Listing Cancelled";
        ...
    }
    else
    {
        GainItem(item);
    }

    if (info.Item == null)
        info.Delete();

    Enqueue(new S.MarketPlaceConsignChanged { Index = info.Index, Count = info.Item?.Count ?? 0, ObserverPacket = false, });
}
```

另有管理侧批量下架 `MarketPlaceCancelSuperior()`（`ServerLibrary/Models/PlayerObject.cs:4589-4623`）：道具系统调整后，把指定等级段（`RequiredType.Level` 且 `RequiredAmount` 40-56，非部件）的旧挂单全部取消并邮件退回。

#### 2.4 搜索（服务端，SConnection）

`Process(C.MarketPlaceSearch)`（`ServerLibrary/Envir/SConnection.cs:951-1028`）：清空 `MPSearchResults/VisibleResults` → 按名称/物品类型过滤 `SEnvir.AuctionInfoList`（只收 `info.Item != null` 的在售单）→ 按 `MarketPlaceSort`（Newest/Oldest/HighestPrice/LowestPrice，997-1008）排序 → 先回 `S.MarketPlaceSearch`（`Count` + 前 9 条懒加载条目）。滚动时客户端逐条发 `C.MarketPlaceSearchIndex`，服务端回 `S.MarketPlaceSearchIndex` 填充详情（1029-1043）——**分页是懒加载协议**，客户端必须按服务器索引占位，不能压缩空位（GodotClient `ConsignmentDialog.cs:337-341` 注释已踩过此坑）。

#### 2.5 摆摊/个人商店

**未找到实现**。全引擎检索只有 `AuctionInfo`（全局拍卖行）与 `MarketPlaceStoreBuy`（官方商城 `GameStore`，`ServerLibrary/Models/PlayerObject.cs:4317` 起，用 GameShop 硬币结算，与玩家交易无关）；没有 Mir3 传统的个人摆摊（如"比奇省摆摊"）机制。若 Godot 客户端要做摆摊，需要全新设计服务端。

### 3. 个人仓库（Storage）

**数据模型**：仓库挂在**账号**上，不是角色。`PlayerObject` 初始化两个 1000 格数组（`ServerLibrary/Models/PlayerObject.cs:152-154`）：

```csharp
Equipment = new UserItem[Globals.EquipmentSize],
Storage = new UserItem[1000],
PartsStorage = new UserItem[1000];
```

加载时按 Slot 偏移拆分（`ServerLibrary/Models/PlayerObject.cs:197-205`）：`item.Slot >= Globals.PartsStorageOffset(2000)` 的是碎片（`PartsStorage[Slot−2000]`），否则进 `Storage[Slot]`——即 Users.db 里 `UserItem.Account` 关联 + Slot 一个字段编码两个仓库。

**容量**：`AccountInfo.StorageSize`（`ServerLibrary/DBModels/AccountInfo.cs:462-475`）建号时初始化为 `Globals.StorageSize = 100`（`ServerLibrary/DBModels/AccountInfo.cs:630`，常量在 `LibraryCore/Globals.cs:301`）；登录下发给客户端（`S.StartInfo.StorageSize`，`ServerLibrary/Models/PlayerObject.cs:889`）。扩容靠道具：

```csharp
// ServerLibrary/Models/PlayerObject.cs:6791-6803（物品效果 17：Storage Increase）
case 17: //Storage Increase
    int size = Character.Account.StorageSize + 10;

    if (size >= Storage.Length)
    {
        Connection.ReceiveChatWithObservers(con => con.Language.StorageLimit, MessageType.System);
        return;
    }

    Character.Account.StorageSize = size;
    Enqueue(new S.StorageSize { Size = Character.Account.StorageSize });
    break;
```

每次 +10，上限 1000（数组物理上限）；`S.StorageSize` 实时通知客户端扩容。

**存取协议**：仓库没有专用包，全部复用 `C.ItemMove`（`LibraryCore/Network/ClientPackets.cs:168`），`ItemMove`（`ServerLibrary/Models/PlayerObject.cs:7435-7974`）中对 `GridType.Storage` 的关键分支：

```csharp
// ServerLibrary/Models/PlayerObject.cs:7471-7481（取 FromGrid=Storage）
case GridType.Storage:
    if (!InSafeZone && !Character.Account.TempAdmin)
    {
        Connection.ReceiveChatWithObservers(con => con.Language.StorageSafeZone, MessageType.System);
        return;
    }

    fromArray = Storage;

    if (p.FromSlot >= Character.Account.StorageSize) return;   // 越过已扩容上限
    break;

// ServerLibrary/Models/PlayerObject.cs:7543-7554（存 ToGrid=Storage）
case GridType.Storage:
    if (!InSafeZone && !Character.Account.TempAdmin) { ...StorageSafeZone... return; }
    toArray = Storage;
    if (p.ToSlot >= Character.Account.StorageSize) return;
    break;
```

即：**必须安全区**（`InSafeZone`，管理员豁免）+ 槽位必须 < `StorageSize`。移动落位时 `fromItem.Slot = p.ToSlot; fromItem.Account = Character.Account;`（7923-7926）。排序包 `C.ItemSort` 对仓库限制 `length = Math.Min(array.Length, Character.Account.StorageSize)`（7998-7999）。碎片仓库 `PartsStorage` 同样要求安全区（7462-7470），但无 StorageSize 上限（固定 1000）。行会仓库 `GuildType.GuildStorage` 需 `GuildPermission.Storage` 权限且安全区（7482-7500）。

### 4. 丢弃与拾取

#### 4.1 丢弃（C.ItemDrop，照抄）

```csharp
// ServerLibrary/Models/PlayerObject.cs:8411-8484
public void ItemDrop(C.ItemDrop p)
{
    S.ItemChanged result = new S.ItemChanged { Link = p.Link };
    Enqueue(result);

    if (Dead || !ParseLinks(p.Link)) return;

    UserItem[] fromArray;

    switch (p.Link.GridType)
    {
        case GridType.Inventory: fromArray = Inventory; break;
        case GridType.CompanionInventory:
            if (Companion == null) return;
            fromArray = Companion.Inventory;
            break;
        default: return;                      // 仓库/装备不能直接丢
    }

    if (p.Link.Slot < 0 || p.Link.Slot >= fromArray.Length) return;

    UserItem fromItem = fromArray[p.Link.Slot];

    if (fromItem == null || p.Link.Count > fromItem.Count || !fromItem.Info.CanDrop || (fromItem.Flags & UserItemFlags.Locked) == UserItemFlags.Locked) return;

    if ((fromItem.Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage) return;
    Cell cell = GetDropLocation(1, null);     // 脚下 1 格内找落点

    if (cell == null) return;

    result.Success = true;

    UserItem dropItem;

    if (p.Link.Count == fromItem.Count)
    {
        dropItem = fromItem;
        RemoveItem(fromItem);
        fromArray[p.Link.Slot] = null;
        result.Link.Count = 0;
    }
    else
    {
        dropItem = SEnvir.CreateFreshItem(fromItem);   // 拆叠丢弃
        dropItem.Count = p.Link.Count;
        fromItem.Count -= p.Link.Count;
        result.Link.Count = fromItem.Count;
    }

    RefreshWeight();
    Companion?.RefreshWeight();
    dropItem.SetTemporary(true);              // 丢弃物是临时物，消失即删档

    ItemObject ob = new ItemObject { Item = dropItem, };

    if ((fromItem.Flags & UserItemFlags.Bound) == UserItemFlags.Bound)
        ob.Account = Character.Account;       // ★ 绑定物品丢弃后仅本人可捡

    ob.Spawn(CurrentMap, cell.Location);
}
```

`!CanDrop`、`Locked`（拍卖购入锁定）、`Marriage` 物品禁丢；货币丢弃走 `C.CurrencyDrop`（8485-8516，按货币配置的 DropItem 形态落地）。

#### 4.2 拾取（C.PickUp，照抄）

```csharp
// ServerLibrary/Models/PlayerObject.cs:8582-8616
public void PickUp()
{
    if (Dead) return;

    int range = Stats[Stat.PickUpRadius];

    for (int d = 0; d <= range; d++)          // 环形扫描：0=脚下，1=八邻……
    {
        for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
        {
            if (y < 0) continue;
            if (y >= CurrentMap.Height) break;

            for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
            {
                ... // 边界检查后取 Cell
                foreach (MapObject cellObject in cell.Objects)
                {
                    if (cellObject.Race != ObjectType.Item) continue;

                    ItemObject item = (ItemObject)cellObject;

                    if (item.PickUpItem(this)) return;   // 捡到第一件就结束
                }
            }
        }
    }
}
```

`Stat.PickUpRadius` 基础为 1（`ServerLibrary/Models/PlayerObject.cs:2576`），宠物可代捡（`Companion.PickUpItem`，`ServerLibrary/Models/ItemObject.cs:114-144`）。

#### 4.3 ItemObject 归属规则（照抄）

```csharp
// ServerLibrary/Models/ItemObject.cs:53-80
public bool CanPickUpItem(PlayerObject ob)
{
    if (Account != null && Account != ob.Character.Account)
    {
        if (Config.DropVisibleOtherPlayers)
        {
            var isSameGuild = Account.GuildMember != null
                && ob.Character.Account.GuildMember != null
                && Account.GuildMember.Guild == ob.Character.Account.GuildMember.Guild;

            var isSameGroup = ob.GroupMembers != null
                && Account.Connection?.Player.GroupMembers == ob.GroupMembers;

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
}
```

归属宽限期：**本人随时 → 同队 2 分钟 → 同会 5 分钟 → 所有人 10 分钟**（前提 `Config.DropVisibleOtherPlayers = true`，`ServerLibrary/Envir/Config.cs:145`；若关闭则除本人外一律不可见，见 `CanBeSeenBy` 160-171——服务端直接不给别人发这个物品对象）。怪物掉落一律带 `Account = 击杀者账号`（`ServerLibrary/Models/MonsterObject.cs:2807-2812`）。地面物品生命周期：`ExpireTime = SEnvir.Now + Config.DropDuration`（默认 60 分钟，`ServerLibrary/Models/ItemObject.cs:189`、`ServerLibrary/Envir/Config.cs:130`），过期 `Despawn` 并删除临时物品；任务物品 despawn 时解除 `UserTask` 绑定（35-51）。

拾取执行（含行会税）：

```csharp
// ServerLibrary/Models/ItemObject.cs:82-112
public bool PickUpItem(PlayerObject ob)
{
    if (!CanPickUpItem(ob))
        return false;

    long taxableAmount = Account?.GuildMember?.Guild?.CalculateGuildTax(Item) ?? 0;

    ItemCheck check = new ItemCheck(Item, Item.Count - taxableAmount, Item.Flags, Item.ExpireTime);

    if (ob.CanGainItems(false, check))
    {
        if (taxableAmount > 0)
        {
            Item.Count -= taxableAmount;
            Account.GuildMember.Contribute(taxableAmount);   // 拾取行会税
        }
        ...
        ob.GainItem(Item);
        Despawn();
        return true;
    }
    return false;   // 背包满（部分拾取未实现，见源码注释 107-110）
}
```

## 数据结构/协议细节

### 交易包一览

| 包 | 方向 | 定义 | 载荷/说明 |
|---|---|---|---|
| `C.TradeRequest` | C→S | `LibraryCore/Network/ClientPackets.cs:527-529` | 空载荷（目标=面前玩家） |
| `C.TradeRequestResponse` | C→S | 同上 530-533 | `Accept` |
| `C.TradeClose` | C→S | `ServerLibrary/Envir/SConnection.cs:1166-1171`（分发） | 空载荷 |
| `C.TradeAddItem` | C→S | 同上 1172-1177 | `Cell`（CellLinkInfo） |
| `C.TradeAddGold` | C→S | 同上 1178-1183 | `Gold` |
| `C.TradeConfirm` | C→S | 同上 1184-1189 | 空载荷 |
| `S.TradeRequest` | S→C | `LibraryCore/Network/ServerPackets.cs:965-968` | `Name`（邀请者名） |
| `S.TradeOpen` | S→C | 同上 969-972 | `Name`（对方名） |
| `S.TradeClose` | S→C | 同上 974 | — |
| `S.TradeAddItem` | S→C | 同上 976-980 | `Cell`+`Success`（自己上桌回执） |
| `S.TradeItemAdded` | S→C | 同上 987-990 | `Item`（对方上桌广播给己方） |
| `S.TradeAddGold` | S→C | 同上 982-985 | `Gold`（自己金币回执） |
| `S.TradeGoldAdded` | S→C | 同上 992-995 | `Gold`（对方金币广播） |
| `S.TradeUnlock` | S→C | 同上 996 | 容量不足解锁重确认 |

注意配对关系：自己操作回 `S.TradeAddItem/S.TradeAddGold`，对方收到的是 `S.TradeItemAdded/S.TradeGoldAdded`——两组包不能混。

### 拍卖包一览

| 包 | 方向 | 定义 | 说明 |
|---|---|---|---|
| `C.MarketPlaceConsign` | C→S | `LibraryCore/Network/ClientPackets.cs:438-446` | `Link`/`Price`/`GuildFunds`/`Message` |
| `C.MarketPlaceSearch` | C→S | 同上 447-455 | `Name`/物品过滤/`Sort` |
| `C.MarketPlaceSearchIndex` | C→S | 同上 456-459 | 懒加载第 N 条 |
| `C.MarketPlaceCancelConsign` | C→S | 同上 460-464 | `Index`/`Count`（可部分取回） |
| `C.MarketPlaceBuy` | C→S | 同上 465-470 | `Index`/`Count`/`GuildFunds` |
| `C.MarketPlaceStoreBuy` | C→S | 同上 471-475 | 商城（GameStore 硬币） |
| `S.MarketPlaceConsign` | S→C | 登录全量 `PlayerObject.cs:1112`；挂牌增量 `4066` | `Consignments: List<ClientMarketPlaceInfo>` |
| `S.MarketPlaceSearch` / `SearchIndex` / `SearchCount` | S→C | `SConnection.cs:1022/1042` | 搜索结果/懒加载详情/计数 |
| `S.MarketPlaceConsignChanged` | S→C | `PlayerObject.cs:4128, 4311` | 挂单余量变化（部分售出/取消） |
| `S.MarketPlaceBuy` | S→C | `PlayerObject.cs:4135-4140, 4290-4292` | `Index`/`Count`/`Success` |
| `S.MarketPlaceHistory` | S→C | `SConnection.cs:915-943` | 成交均价历史 |

### AuctionInfo 全字段（ServerLibrary/DBModels/AuctionInfo.cs:8-137）

| 字段 | 类型 | 行号 | 说明 |
|---|---|---|---|
| `Account` | AccountInfo | 10-24 | 卖家账号（Association "Auctions"） |
| `Item` | UserItem | 26-40 | 在售物品（Association "Auction"）；售罄置 null 后 Delete |
| `Character` | CharacterInfo | 42-55 | 挂单角色（展示卖家名） |
| `Price` | int | 57-70 | 单价 |
| `ConsignDate` | DateTime | 72-85 | 挂牌时间 |
| `Message` | string | 87-100 | 卖家备注（≤150） |

`ToClientInfo(AccountInfo)`（112-129）把是否本人挂单等信息折算成 `ClientMarketPlaceInfo` 下发。

### 仓库/物品包速查

| 包 | 方向 | 说明 |
|---|---|---|
| `C.ItemMove`（`ClientPackets.cs:168-175`） | C→S | 通用跨格移动：`FromGrid/FromSlot/ToGrid/ToSlot/MergeItem`；仓库存取、背包整理、穿装备共用 |
| `C.ItemSort`（177-180）/ `C.ItemSplit`（188-…） | C→S | 排序 / 拆叠 |
| `S.ItemsChanged`（`ServerPackets.cs:664-668`） | S→C | 链接型操作的统一回执（`Links`+`Success`） |
| `C.ItemDrop`（195-198）/ `C.PickUp` / `C.CurrencyDrop` | C→S | 丢弃 / 拾取 / 货币丢弃（分发 `SConnection.cs:559-576`） |
| `S.StorageSize`（`PlayerObject.cs:6802`） | S→C | 仓库扩容实时同步 |

### 交叉引用：研究文档

- 【研究文档】`/home/tetsuya/development/Mir3-Research/docs/OPERATION_PARITY_HANDOFF.md:133-140, 218-219`：GodotClient 交易/寄售契约审计状态——`UITradeAudit`（双向金币路由/余额边界）与 `UIConsignmentAudit` PASS；并注明"真实服务器交易断线、响应顺序、寄售计数刷新……仍未完成"的遗留项。
- 【研究文档】`/home/tetsuya/development/Mir3-Research/docs/ORIGINAL_GODOT_PARITY_AUDIT.md:146, 331`：寄售行 Interface 301/302/303-306 底图与 `ConsignItemDialog`（调价+行会资金选项）的移植记录；30 组 UI 审计全 PASS 清单（含 `--consignment-audit --storage-audit --gamestore-audit`）。
- 未找到专门讨论摆摊/交易数值平衡的研究文档（`Mir3-Research/docs/` 全目录 grep trade/consign/auction/market 仅命中上述两份移植记录与 quest-design 内容设计文档）。

## GodotClient 现状

| 功能 | 状态 | Godot 文件与证据 |
|---|---|---|
| 交易窗口（上桌/金币/确认/关闭） | 已移植 | `GodotClient/Controls/TradeDialog.cs:39-67`（`SendTradeItem`/`SendTradeGold`/`SendTradeConfirm`，确认后禁用按钮防重复）；关窗 `CloseTrade` 仅非观察者发 `TradeClose`（206-211） |
| 交易邀请弹窗 | 已移植 | `GodotClient/Controls/TradeDialog.cs:79-83`（`SendTradeRequestResponse(true/false)`）；快捷键 `TradeRequest`（`GodotClient/Controls/KeyBindManager.cs:47`） |
| 8 个交易 S 包处理 | 已移植 | `GodotClient/Network/ServerConnection.cs:113-114, 168-174`（事件）+ `460-461`（Process）；`GodotClient/Scripts/GameScene.cs:1110-1111, 1163-1200`（TradeOpen/TradeRequest/TradeClose/TradeUnlock 全接线） |
| 交易防作弊语义复刻 | 已移植 | `GodotClient/Controls/TradeDialog.cs:135-205`（`ApplyTradeAddItem` 校验链接、`ShouldUnlockTradeSource`）+ `GodotClient/Scripts/GameScene.cs` 的 `CanSendTradeGold`（`GodotClient/UITestScene.cs:269-273` 验证余额边界） |
| 寄售行（搜索/懒加载/购买/我的挂单） | 已移植 | `GodotClient/Controls/ConsignmentDialog.cs`（搜索排序 88-92、懒加载占位 337-341、`S.ItemChanged`→`S.MarketPlaceConsign` 双包时序注释 242-246）；网络接线 `GodotClient/Scripts/GameScene.cs:1239-1253` |
| 挂牌弹窗（调价/行会资金） | 已移植 | 【研究文档】`ORIGINAL_GODOT_PARITY_AUDIT.md:146` 记录 Interface 303-306 `ConsignItemDialog`；现名整合于 `ConsignmentDialog.cs` |
| 市场历史 | 已移植 | `GodotClient/Controls/MarketHistoryDialog.cs`（`S.MarketPlaceHistory` 事件 `ServerConnection.cs:151, 534`） |
| 商城（GameStore） | 已移植 | `GodotClient/Controls/GameStoreDialog.cs`、`GameStoreGiftDialog.cs`（`MarketPlaceStoreBuy` 独立于拍卖） |
| 个人仓库窗口（主仓库/碎片双页） | 已移植 | `GodotClient/Controls/StorageDialog.cs:9-83`（Interface 121 底图、10 列滚动、StorageSize 决定行数、PartsStorage 切页）；`S.StorageSize` 处理 `GodotClient/Network/ServerConnection.cs:886-889` |
| 仓库存取（C.ItemMove） | 已移植 | `GodotClient/Controls/DXItemCell.cs:721, 738, 938, 1070` 统一 `SendItemMove`；`GodotClient/Scripts/GameScene.cs:6009-6011` 转发 |
| 丢弃物品 | 已移植 | `GodotClient/Network/ServerConnection.cs:998-1001`（`SendItemDrop`）；`GodotClient/Scripts/GameScene.cs:741-743`（`CanBeginItemDrop` 只允许背包/宠物背包） |
| 拾取 | 已移植 | `GodotClient/Network/ServerConnection.cs:1003-1006`（`SendPickUp`）；Tab 键位（`KeyBindManager.cs:30`）+ 250ms 节流语义（`GameScene.cs:738-739`） |
| 面对面交易的朝向/距离提示 | 部分移植 | Godot 侧未见原版 `TradeNeedFace` 之类的失败文案专门处理（服务端会照常发 System 聊天，走通用聊天渲染即可）；未找到独立的朝向预检 UI |
| 摆摊/个人商店 | 不适用 | 原版引擎无此机制（本文 2.5 节），Godot 亦无对应文件 |

## 移植注意事项

1. **交易物品"上桌"不转移所有权**——`TradeItems` 只是 `UserItem→CellLinkInfo` 映射，交换发生在双方都确认后的一次性循环里。Godot 客户端必须同样把"上桌"做成纯 UI 状态（发 `C.TradeAddItem` + 等 `S.TradeAddItem.Success`），不要本地扣格子。
2. `S.TradeAddItem`（自己）与 `S.TradeItemAdded`（对方）是两对包；金币同理（`TradeAddGold`/`TradeGoldAdded`）。`GodotClient/Controls/TradeDialog.cs:93-96` 注释明确区分，别接错事件。
3. 本引擎**没有"变更上桌内容自动解锁对方"的逻辑**：`TradeAddItem/TradeAddGold` 不重置任何一方的 `TradeConfirmed`，变更检测只在双方都确认后的 `TradeConfirm` 里做引用比对（失配即 `TradeClose`，`PlayerObject.cs:9925-9932`）；`S.TradeUnlock` 只在容量预检失败时发给缺空间的一方（`PlayerObject.cs:9964-9966, 10044-10046`）。因此客户端在对方已确认后若再允许本地变更上桌内容，结局就是整单被服务端强制关闭——Godot 已按此语义在确认后禁用按钮（`TradeDialog.cs:65`），建议进一步在收到对方确认信号时也锁住上桌交互。
4. 仓库必须在安全区操作：客户端应在非安全区直接置灰仓库格子而非等服务器拒绝（服务端语言键 `StorageSafeZone`）。`StorageSize` 之外的格子（100-1000）要显示为锁定/未扩容，槽位校验在服务端 `p.FromSlot >= Character.Account.StorageSize`（`PlayerObject.cs:7480, 7554`）。
5. 拍卖搜索是**懒加载分页**：`S.MarketPlaceSearch` 只带前 9 条 + 总数，滚动时逐个 `C.MarketPlaceSearchIndex` 拉详情；列表必须按服务器索引占位（`GodotClient/Controls/ConsignmentDialog.cs:337-341` 已写明该坑）。
6. 购买结算走**邮件**（卖家货款金币邮件、买家背包满转物品邮件），UI 上交易完成≠背包立即到货；`item.Flags |= UserItemFlags.Locked` 意味着拍卖购入物**不能再次丢弃**（`ItemDrop` 拒绝 Locked），转卖需等解锁逻辑（本引擎未见自动解锁——移植时如需"绑定时长"需自研）。
7. 挂单量上限公式 `HighestLevel()*3 + (StorageSize-100)` 把仓库扩容和拍卖行容量耦合，做 UI 提示时两处要联动（`PlayerObject.cs:3983`）。
8. 地面物品可见性由服务端控制（`Config.DropVisibleOtherPlayers=false` 时别人的掉落根本不下发），客户端不要自己做"灰显不可捡"判断；拾取失败（归属宽限期内）服务端静默返回 false，客户端捡不到就没有任何反馈——按原版行为保持静默即可。
