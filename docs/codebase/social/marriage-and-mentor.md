# 社交系统：结婚（Marriage）与师徒（Mentor）

## TL;DR 速查表

- **结婚系统已完整实现**：求婚（NPC 触发）→ 弹窗应答（`C.MarriageResponse`）→ 双方各扣 500,000 金币互设 `CharacterInfo.Partner` → 婚戒（`UserItemFlags.Marriage`）+ 夫妻传送（`C.MarriageTeleport`，120 秒 CD）。
- 师徒系统：**未找到实现**。全仓库搜 `Mentor`/`Apprentice`/`Master-Apprentice` 无任何逻辑代码；`MasterPassword` 是 GM 登录后门（ServerLibrary/Envir/Config.cs:27），与师徒无关。
- 求婚硬条件（PlayerObject.cs:2915-2991）：双方 ≥22 级、各持 50 万金币、面对面（对方朝向 = `Functions.ShiftDirection(Direction, 4)`）、双方存活、双方未婚、目标无待处理邀请。
- 离婚是**单方面即时生效**（NPC `NPCActionType.Divorce` → `MarriageLeave()`，PlayerObject.cs:3033-3064）：配偶离线时直接遍历其背包 `Items` 清 `RingL` 的 Marriage 标志，无需对方确认。
- 夫妻传送条件链（PlayerObject.cs:3094-3163）：佩戴婚戒 + 非死亡 + PK 点 < `Config.RedPoint` + 120 秒 CD + 对方在线且存活 + 对方地图 `CanMarriageRecall` + 双方地图 `AllowTT`/副本 `AllowTeleport`。
- 婚戒一旦打上 `UserItemFlags.Marriage`（Enum.cs:1900）即**不可卖店/寄售/邮寄/交易/存仓/修理/分解**（Client/Controls/DXItemCell.cs 全套 Grid 检查）。
- 结婚/离婚写入里程碑 `MilestoneType.Marry=6 / Divorce=7`（Enum.cs:2003-2004）。
- 数据层：`CharacterInfo.Partner` 是 `[Association("Marriage")]` 双向自关联（CharacterInfo.cs:696-710），`MarriageTeleportTime` 存 CD（CharacterInfo.cs:511-524）；`MapInfo.CanMarriageRecall` 控制目标地图（MapInfo.cs:198-211）。
- GodotClient：结婚**已基本完整移植**（求婚弹窗、做戒 NPC 面板、夫妻传送、5 个 S 包处理、婚戒物品限制全套）；师徒**未移植**（本来就没有服务端实现）。

## 职责概述

本文覆盖 Zircon 引擎中"玩家—玩家长期契约关系"的两个子系统：

1. **结婚系统**（已实现）：求婚/应答、婚礼金结算、婚戒绑定（`MarriageMakeRing`）、离婚（`MarriageLeave`）、夫妻传送（`MarriageTeleport`）、伴侣上下线通知（`S.MarriageOnlineChanged`）、NPC 脚本集成（`NPCActionType.Marriage/Divorce/RemoveWeddingRing`、`NPCCheckType.Marriage/WeddingRing`、`NPCDialogType.WeddingRing`）、婚戒物品流通限制。
2. **师徒系统**（未实现）：本节记录搜索过程、结论，以及若要自行实现时的 DB/协议/NPC 扩展位置建议。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| ServerLibrary/Models/PlayerObject.cs | 2913-3179 | `#region Marriage`：MarriageRequest/Join/Leave/MakeRing/Teleport/RemoveRing/GetMarriageInfo 全部逻辑 |
| ServerLibrary/Models/PlayerObject.cs | 160 | `MarriageInvitation` 字段（待处理求婚者，与 GroupInvitation/GuildInvitation 并列） |
| ServerLibrary/Models/PlayerObject.cs | 309 | 每帧清理：求婚者掉线则邀请作废 |
| ServerLibrary/Models/PlayerObject.cs | 1027-1028 | StopGame 时通知配偶自己下线（无 ObjectID 的 `S.MarriageOnlineChanged`） |
| ServerLibrary/Models/PlayerObject.cs | 1138-1141 | StartGame 时下发 `S.MarriageInfo` 并通知配偶上线（带 ObjectID） |
| ServerLibrary/Models/PlayerObject.cs | 1254 | SetUpObserver：观察者连接同样能拿到 `S.MarriageInfo` |
| ServerLibrary/DBModels/CharacterInfo.cs | 511-524 | `MarriageTeleportTime`（夫妻传送 CD，Users.db 持久化） |
| ServerLibrary/DBModels/CharacterInfo.cs | 696-710 | `Partner`：`[Association("Marriage")]` 双向自关联（互为对方的 Partner） |
| ServerLibrary/Models/NPCObject.cs | 138-146 | NPC 动作分发：`Marriage→MarriageRequest()`、`Divorce→MarriageLeave()`、`RemoveWeddingRing→MarriageRemoveRing()` |
| ServerLibrary/Models/NPCObject.cs | 556-575 | NPC 条件检查：`NPCCheckType.Marriage`（是否已婚）、`NPCCheckType.WeddingRing`（RingL 是否为婚戒） |
| ServerLibrary/Envir/SConnection.cs | 1349-1369 | `Process(C.MarriageResponse/MarriageMakeRing/MarriageTeleport)` 入口 |
| ServerLibrary/Envir/Translations/StringMessages.cs | 100-121 | `[ConfigSection("Marriage")]` 22 条文案键（MarryAlreadyMarried…MarryTeleportMapEscape） |
| ServerLibrary/Envir/Commands/Command/Admin/ChatBan.cs | — | （对照用）GM 禁言指令，见 chat-and-mail.md |
| LibraryCore/Enum.cs | 1900 | `UserItemFlags.Marriage = 128` |
| LibraryCore/Enum.cs | 2003-2004 | `MilestoneType.Marry = 6`、`Divorce = 7` |
| LibraryCore/Enum.cs | 571-572 | `NPCDialogType.WeddingRing`（NPC 戒指对话框类型） |
| LibraryCore/SystemModels/NPCInfo.cs | 1014-1015 | `NPCCheckType.Marriage=11 / WeddingRing=12` |
| LibraryCore/SystemModels/NPCInfo.cs | 1055-1057 | `NPCActionType.Marriage=8 / Divorce=9 / RemoveWeddingRing=10` |
| LibraryCore/SystemModels/MapInfo.cs | 198-211 | `MapInfo.CanMarriageRecall`（默认 true，MapInfo.cs:533） |
| LibraryCore/Network/ClientPackets.cs | 673-686 | `C.MarriageResponse/MarriageMakeRing/MarriageTeleport` |
| LibraryCore/Network/ServerPackets.cs | 1224-1244 | `S.MarriageInvite/MarriageInfo/MarriageRemoveRing/MarriageMakeRing/MarriageOnlineChanged` |
| Client/Envir/CConnection.cs | 4341-4386 | 客户端 Process(S.Marriage*)：求婚弹窗（Yes/No→C.MarriageResponse）、婚戒标志位同步 |
| Client/Scenes/Views/NPCDialog.cs | 4945-5007 | `NPCWeddingRingDialog`：单格 GridType.WeddingRing + Bind→`C.MarriageMakeRing{Slot}` |
| Client/Scenes/GameScene.cs | 351-355/4069-4074 | `Partner` 属性 setter→`MarriageChanged()`：角色框婚戒图标与配偶名 |
| Client/Scenes/Views/CharacterDialog.cs | 38-39/2895-2897 | `MarriageIcon/MarriageLabel` 显示 |
| Client/UserModels/KeyBindInfo.cs | 234-235 | 快捷键 `PartnerTeleport`（"Wedding Teleport"）→ `C.MarriageTeleport`（GameScene.cs:1380） |
| Client/Controls/DXItemCell.cs | 908/1225-1334/1335-1338 | 婚戒禁止落入 Repair/Storage/PartsStorage/各精炼格/SendMail/TradeUser/GuildStorage/WeddingRing 等 Grid |

## 核心流程

### 1. 求婚（NPC 动作触发，服务端校验）

```csharp
public void MarriageRequest()                        // PlayerObject.cs:2915
{
    if (Character.Partner != null) { ...MarryAlreadyMarried...; return; }
    if (Level < 22)                    { ...MarryNeedLevel...;    return; }
    if (Gold.Amount < 500000)          { ...MarryNeedGold...;     return; }

    Cell cell = CurrentMap.GetCell(Functions.Move(CurrentLocation, Direction));
    if (cell?.Objects == null) { ...MarryNotFacing...; return; }

    PlayerObject player = null;
    foreach (MapObject ob in cell.Objects)
    {
        if (ob.Race != ObjectType.Player) continue;
        player = (PlayerObject)ob;
        break;
    }

    if (player == null || player.Direction != Functions.ShiftDirection(Direction, 4))
        { ...MarryNotFacing...; return; }             // 对方必须正面朝自己
    if (player.Character.Partner != null) { ...MarryTargetAlreadyMarried...; return; }
    if (player.MarriageInvitation != null) { ...MarryTargetHasProposal...; return; }
    if (player.Level < 22)        { ...MarryTargetNeedLevel...; return; }
    if (player.Gold.Amount < 500000) { ...MarryTargetNeedGold...; return; }
    if (player.Dead || Dead)      { ...MarryDead...; return; }

    player.MarriageInvitation = this;
    player.Enqueue(new S.MarriageInvite { Name = Name });   // PlayerObject.cs:2989-2990
}
```

要点：求婚**不立刻扣钱**，只做资格预检并登记 `MarriageInvitation`；金币在应答（`MarriageJoin`）时才二次校验并扣除。

### 2. 应答成婚（C.MarriageResponse → MarriageJoin）

```csharp
public void Process(C.MarriageResponse p)              // SConnection.cs:1349
{
    if (Stage != GameStage.Game) return;

    if (p.Accept)
        Player.MarriageJoin();

    Player.MarriageInvitation = null;                  // 拒绝/接受后都清空邀请
}
```

```csharp
public void MarriageJoin()                             // PlayerObject.cs:2992
{
    if (MarriageInvitation != null && MarriageInvitation.Node == null) MarriageInvitation = null;  // 求婚者已下线

    if (MarriageInvitation == null || Character.Partner != null || MarriageInvitation.Character.Partner != null) return;

    const int cost = 500000;

    if (Gold.Amount < cost) { ...; return; }
    if (MarriageInvitation.Gold.Amount < cost) { ...; return; }

    Character.Partner = MarriageInvitation.Character;  // 单向赋值，Association 自动建立反向

    ...MarryComplete 双方提示...

    Gold.Amount -= cost;                                // 双方各扣 50 万
    MarriageInvitation.Gold.Amount -= cost;

    LogMilestone(MilestoneType.Marry, 1);               // 里程碑
    MarriageInvitation.LogMilestone(MilestoneType.Marry, 1);

    GoldChanged();
    MarriageInvitation.GoldChanged();

    AddAllObjects();

    Enqueue(GetMarriageInfo());                          // 双方刷新 S.MarriageInfo
    MarriageInvitation.Enqueue(MarriageInvitation.GetMarriageInfo());
}
```

### 3. 离婚（NPC 动作，单方面即时生效）

```csharp
public void MarriageLeave()                            // PlayerObject.cs:3033
{
    if (Character.Partner == null) return;

    CharacterInfo partner = Character.Partner;

    Character.Partner = null;                           // 关系立即解除，无需对方同意

    MarriageRemoveRing();                               // 自己 RingL 去掉 Marriage 标志
    ...MarryDivorce 提示...

    Enqueue(GetMarriageInfo());

    LogMilestone(MilestoneType.Divorce, 1);

    if (partner.Player != null)                         // 配偶在线：走同一套下线流程
    {
        partner.Player.MarriageRemoveRing();
        ...配偶收 MarryDivorce 提示...
        partner.Player.Enqueue(partner.Player.GetMarriageInfo());
        partner.Player.LogMilestone(MilestoneType.Divorce, 1);
    }
    else
    {
        foreach (UserItem item in partner.Items)        // 配偶离线：直接扫其物品栏
        {
            if (item.Slot != Globals.EquipmentOffSet + (int)EquipmentSlot.RingL) continue;

            item.Flags &= ~UserItemFlags.Marriage;      // 把对方身上的婚戒标志清掉
        }
    }
}
```

注意：离婚**不退婚礼金**，也没有冷静期/确认弹窗——NPC 脚本层若要二次确认需自行用 `NPCCheckType` 页面跳转实现。

### 4. 制作婚戒（NPC WeddingRing 对话框）

```csharp
public void MarriageMakeRing(int index)                // PlayerObject.cs:3065
{
    if (Character.Partner == null) return; // Not Married

    if (Equipment[(int)EquipmentSlot.RingL] != null && (Equipment[(int)EquipmentSlot.RingL].Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage) return;

    if (index < 0 || index >= Inventory.Length) return;

    UserItem ring = Inventory[index];

    if (ring == null || ring.Info.ItemType != ItemType.Ring) return;   // 必须是戒指

    if (!(CanWearItem(ring, EquipmentSlot.RingL) || CanWearItem(ring, EquipmentSlot.RingR))) return;  // 必须戴得上

    ring.Flags |= UserItemFlags.Marriage;               // 打标志 → 从此不可流通

    Inventory[index] = Equipment[(int)EquipmentSlot.RingL];   // 与当前左戒互换
    if (Inventory[index] != null)
        Inventory[index].Slot = index;

    Equipment[(int)EquipmentSlot.RingL] = ring;
    ring.Slot = Globals.EquipmentOffSet + (int)EquipmentSlot.RingL;

    Enqueue(new S.ItemMove { FromGrid = GridType.Inventory, FromSlot = index, ToGrid = GridType.Equipment, ToSlot = (int)EquipmentSlot.RingL, Success = true });
    Enqueue(new S.MarriageMakeRing());
    RefreshStats();
    Enqueue(new S.NPCClose());
}
```

### 5. 夫妻传送（快捷键触发）

```csharp
public void MarriageTeleport()                         // PlayerObject.cs:3094
{
    if (Character.Partner == null) { ...NotMarried...; return; }

    if (Equipment[(int)EquipmentSlot.RingL] == null || (Equipment[(int)EquipmentSlot.RingL].Flags & UserItemFlags.Marriage) != UserItemFlags.Marriage)
        { ...MarryNotRing...; return; }                 // 必须戴着婚戒

    if (Dead)                       { ...MarryTeleportDead...;         return; }
    if (Stats[Stat.PKPoint] >= Config.RedPoint) { ...MarryTeleportPK...; return; }

    if (SEnvir.Now < Character.MarriageTeleportTime)   // 120 秒 CD
        { ...MarryTeleportDelay(剩余时间)...; return; }

    if (Character.Partner.Player?.Node == null)  { ...MarryTeleportOffline...;      return; }
    if (Character.Partner.Player.Dead)           { ...MarryTeleportPartnerDead...;  return; }
    if (!Character.Partner.Player.CurrentMap.Info.CanMarriageRecall) { ...MarryTeleportMap...; return; }
    if (Character.Partner.Player.CurrentMap.Instance != null && !Character.Partner.Player.CurrentMap.Instance.AllowTeleport) { ...MarryTeleportMap...; return; }
    if (CurrentMap.Instance != null && !CurrentMap.Instance.AllowTeleport) { ...MarryTeleportMapEscape...; return; }
    if (!CurrentMap.Info.AllowTT)                      { ...MarryTeleportMapEscape...; return; }

    if (Teleport(Character.Partner.Player.CurrentMap, Character.Partner.Player.CurrentMap.GetRandomLocation(Character.Partner.Player.CurrentLocation, 10)))
        Character.MarriageTeleportTime = SEnvir.Now.AddSeconds(120);   // CD 写入 DB 字段
}
```

传送落点是**配偶周围 10 格内的随机点**（`GetRandomLocation(..., 10)`），不是精确贴身。

### 6. 伴侣信息与上下线通知

```csharp
public S.MarriageInfo GetMarriageInfo()                // PlayerObject.cs:3172
{
    return new S.MarriageInfo
    {
        Partner = new ClientPlayerInfo { Name = Character.Partner?.CharacterName, ObjectID = Character.Partner?.Player != null ? Character.Partner.Player.ObjectID : 0 }
    };
}
```

- 上线（StartGame，PlayerObject.cs:1138-1141）：`Enqueue(GetMarriageInfo())` + 向配偶发 `S.MarriageOnlineChanged { ObjectID = ObjectID }`（对方 ObjectID ≠ 0 → 客户端显示"上线"）。
- 下线（StopGame，PlayerObject.cs:1027-1028）：`new S.MarriageOnlineChanged()`（ObjectID=0 → 显示"下线"）。
- 观察者接入（SetUpObserver，PlayerObject.cs:1254）：观察者连接同样收到 `S.MarriageInfo`。

## 数据结构/协议细节

### 包一览

| 包 | 字段 | 时机 |
|---|---|---|
| `C.MarriageResponse`（ClientPackets.cs:673-676） | `bool Accept` | 求婚弹窗 Yes/No |
| `C.MarriageMakeRing`（:678-681） | `int Slot`（背包格） | NPC 婚戒对话框 Bind 按钮 |
| `C.MarriageTeleport`（:683-686） | — | 快捷键 PartnerTeleport |
| `S.MarriageInvite`（ServerPackets.cs:1224-1227） | `string Name` | 有人向自己求婚 |
| `S.MarriageInfo`（:1228-1231） | `ClientPlayerInfo Partner`（Name+ObjectID） | 登录/结婚/离婚后刷新 |
| `S.MarriageRemoveRing`（:1232-1235） | — | 服务端清除 RingL 婚戒标志 |
| `S.MarriageMakeRing`（:1236-1239） | — | 服务端打上婚戒标志 |
| `S.MarriageOnlineChanged`（:1241-1244） | `uint ObjectID`（0=下线） | 配偶上/下线 |

### DB 层（Users.db）

- `CharacterInfo.Partner`（CharacterInfo.cs:696-710）：`[Association("Marriage")]` 自关联。赋值单向（`Character.Partner = 对方`），MirDB Association 自动维护对方反向引用，因此读回时双方都能取到彼此。
- `CharacterInfo.MarriageTeleportTime`（CharacterInfo.cs:511-524）：`DateTime`，夫妻传送 CD 到期时刻（服务器重启也会保留）。
- 查看（Inspect）其他玩家时 `S.Inspect` 载荷也带 `Partner = target.Partner?.CharacterName`（PlayerObject.cs:1970，Inspect 方法 1959-2012）——陌生人查看时的婚戒图标就靠它，对应 Client/Scenes/Views/CharacterDialog.cs:2895-2897 与 GodotClient/Controls/CharacterDialog.cs:333 的观察面板刷新。
- 婚戒不落独立表：只是 `UserItem.Flags` 上的 `UserItemFlags.Marriage = 128` 位（Enum.cs:1900）。
- NPC 脚本侧（System.db）：`NPCCheckType.Marriage=11 / WeddingRing=12`（NPCInfo.cs:1014-1015）、`NPCActionType.Marriage=8 / Divorce=9 / RemoveWeddingRing=10`（:1055-1057）、`NPCDialogType.WeddingRing`（Enum.cs:572）；地图开关 `MapInfo.CanMarriageRecall`（MapInfo.cs:198-211，默认 true）。

### 客户端婚戒流通限制（WinForms 原版参考）

`Client/Controls/DXItemCell.cs` 中所有涉及物品外流的 Grid 目标都先拦 `(Item.Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage`：Repair（:1225）、PartsStorage（:1231）、Storage（:1238）、各精炼石/碎片格（:1246-1303）、Consign 寄售（:1310）、SendMail 邮件（:1319）、TradeUser 交易（:1324）、GuildStorage（:1329）、AccessoryRefine（:1379）、MasterRefine 系列（:1437+）；拖出装备格也拦（:908）。物品提示里显示紫色 "Wedding Ring."（Client/Scenes/GameScene.cs:1768-1772）。

## GodotClient 现状

| 功能 | 状态 | GodotClient 证据 |
|---|---|---|
| 求婚弹窗（S.MarriageInvite → Yes/No） | 已移植 | GodotClient/Controls/GuildDialog.cs:520-529 `ShowMarriageInvite`（自绘面板，接/拒分别发 `SendMarriageResponse(true/false)`）；GodotClient/Scripts/GameScene.cs:2812 `OnMarriageInvite` |
| 求婚应答发送 | 已移植 | GodotClient/Network/ServerConnection.cs:1084 `SendMarriageResponse`；GameScene.cs:417-421 |
| 婚戒制作 NPC 面板 | 已移植 | GodotClient/Controls/NPCAdvancedPanels.cs:211-212（`NPCDialogType.WeddingRing → BuildWeddingRing`）、:775-782（GridType.WeddingRing 单格 + Submit）、:1016-1018（`SendMarriageMakeRing(link.Slot)`） |
| 婚戒制作发送 | 已移植 | ServerConnection.cs:1085 `SendMarriageMakeRing`；GameScene.cs:422-426 |
| 夫妻传送 | 已移植 | GameScene.cs:6083-6087 `SendMarriageTeleport`（发 `C.MarriageTeleport`）；快捷键 GameScene.cs:1972-1973（`KeyBindAction.PartnerTeleport`）；点击装备格婚戒也触发（Controls/DXItemCell.cs:479-480/519-521） |
| S.MarriageInfo（配偶名显示） | 已移植 | GameScene.cs:2281-2285 `OnMarriageInfo` → `_characterDialog.SetPartner` + 系统聊天；Controls/CharacterDialog.cs:696-706 `SetPartner/RefreshMarriageAndGuild`（婚戒图标+配偶名） |
| S.MarriageMakeRing / S.MarriageRemoveRing | 已移植 | GameScene.cs:2287-2299：直接改本地 `Equipment[RingL].Flags` 并重绘 |
| S.MarriageOnlineChanged（配偶上下线提示） | 已移植 | GameScene.cs:2301-2304（`ObjectID==0 → Lang.GameOfflineLabel` 下线提示，否则上线提示） |
| 婚戒物品流通限制 | 已移植 | Controls/DXItemCell.cs:709（不可穿戴卸下判断）、:798（寄售）、:850-851/979-980（SendMail）、:916-926（仓库/行会仓）、:946/981（WeddingRing 目标格规则与原版一致）；InventoryDialog.cs:213/323；TradeDialog.cs:200；CommunicationDialog.cs:795；ConsignmentDialog.cs:216；NPCGoodsPanel.cs:100；NPCRepairPanel.cs:146 |
| 离婚流程（客户端侧） | 已移植 | 无需独立 UI：`NPCActionType.Divorce` 由服务端 NPC 脚本触发，客户端只需处理 `S.MarriageInfo`（Partner.Name 为空 → 图标隐藏，CharacterDialog.cs:703-706）与 `S.MarriageRemoveRing` |
| 师徒系统 | 未移植 | 全仓库无 Mentor/Apprentice 代码（仅 translations/db_names.json:919/1739/4015/6223 的物品/怪物译名，如"Taoist Mentor"→"道士导师"） |

事件注册/反注册齐套：GodotClient/Scripts/GameScene.cs:1103-1106、1162、1662-1665、1631（订阅与释放均成对）。

## 移植注意事项

1. **求婚邀请是 PlayerObject 内存态**（`MarriageInvitation`，PlayerObject.cs:160）：服务端每帧在 :309 清理掉线求婚者。Godot 客户端无需关心，但要注意拒绝/接受之外**没有任何超时包**——弹窗挂机时邀请会一直有效直到任一方下线。
2. `MarriageJoin` 里金币二次校验发生在**应答方**连接上，但检查的是**双方**余额（PlayerObject.cs:3000-3012）；移植服务端逻辑时别只查应答方。
3. 离线配偶离婚走的是"直接改对方 `UserItem` 标志"这条捷径（PlayerObject.cs:3057-3062），依赖 `Globals.EquipmentOffSet + (int)EquipmentSlot.RingL` 这个槽位偏移约定；若客户端和服务端的 `EquipmentOffSet` 不一致会清错格。
4. `S.MarriageOnlineChanged` 用 `ObjectID==0` 表达"下线"是隐式协议（PlayerObject.cs:1028 无参构造 vs :1141 带 ObjectID）；Godot 端已按此处理（GameScene.cs:2303），新客户端务必保持该语义。
5. 婚戒互换槽位逻辑（MarriageMakeRing，PlayerObject.cs:3081-3087）会产生一次 `S.ItemMove`，客户端靠它同步背包格；自研客户端若只监听 `S.MarriageMakeRing` 会出现背包/装备不同步。
6. 夫妻传送 CD 写在 `CharacterInfo.MarriageTeleportTime`（DB 持久化）而非内存字段——换角色/下线重登 CD 不重置。

---

## 师徒系统（Mentor / Master-Apprentice）：未找到实现

### 搜索过程与结论

对 ServerLibrary/、Client/、LibraryCore/、GodotClient/ 四个目录执行了以下搜索（本会话实际执行）：

1. `Mentor` → **0 个匹配**（ServerLibrary/Client/LibraryCore）。
2. `Apprentice|MasterApprentice|师父|徒弟|Shifu|Tudi` → **0 个逻辑代码匹配**；仅 GodotClient/translations/db_names.json 出现物品/怪物译名（"Apprentice's Mask" 学徒面罩 :919、"Apprentice's Hand Blade" 学徒手刃 :1739、"PVP Apprentice" :4015、"Taoist Mentor" 道士导师 :6223——均为名字表，无系统逻辑）。
3. `CharacterInfo.cs` 内搜 `Master` → **0 个匹配**：数据库模型连师徒雏形字段都没有（对比结婚有 `Partner`/`MarriageTeleportTime`）。
4. 排除混淆项：`MasterPassword` 是**服务器主密码/GM 登录后门**（ServerLibrary/Envir/Config.cs:27 定义；ServerLibrary/Envir/SEnvir.cs:3266-3269 用它以任意邮箱直接登录目标账号并置 `admin=true`），与师徒无关。`NPCMasterRefine`（PlayerObject.cs:12796）、`GridType.MasterRefineFragment*` 是"大师精炼"（道具精炼 NPC），也不是师徒。

**结论：Zircon 引擎没有师徒系统，且没有任何半成品残留（无 DB 字段、无包、无 NPC 枚举值）。**

### 原版 Mir3 师徒行为的空白点（移植时需自行设计）

原版传奇 3 客户端通常有"拜师/出师"玩法，本引擎全部缺失，以下行为均需从零设计：

- 拜师条件（等级窗口、职业限制、师生双方数量上限）与仪式（NPC 对话 or 面对面请求，参考 MarriageRequest 的面对面校验）。
- 师徒收益（徒弟升级时师傅获得声望/经验加成、师傅带徒弟的组队经验分成）。
- 出师条件与出师奖励（原版为徒弟达到某等级自动出师）。
- 师徒传送（参考 `MarriageTeleport`：条件链 + CD 字段 + 地图开关）。
- 师徒关系 UI（角色信息面板显示师傅名，类比 `MarriageIcon/MarriageLabel`）。

### DB / 协议层扩展位置建议

若要在本引擎补师徒系统，建议按下述位置扩展（全部带现成范式可抄）：

1. **DB 模型**：新建 `ServerLibrary/DBModels/MentorInfo.cs`，结构照抄 `FriendInfo.cs:6-74`（两个 `CharacterInfo` 关联 + 冗余名称字符串 + `ToClientInfo()`）；或在 `CharacterInfo` 上加自关联属性，范式照抄 `Partner`（CharacterInfo.cs:696-710，`[Association("Mentor")]`）。CD 类字段照抄 `MarriageTeleportTime`（CharacterInfo.cs:511-524）。
2. **协议**：`C.*` 加在 LibraryCore/Network/ClientPackets.cs:673-686（Marriage 三包之后）；`S.*` 加在 LibraryCore/Network/ServerPackets.cs:1224-1244（Marriage 五包之后），命名建议 `C/S.MentorInvite/MentorResponse/MentorLeave/MentorInfo`。
3. **连接层分发**：`ServerLibrary/Envir/SConnection.cs:1349-1369`（`Process(C.Marriage*)` 系列之后）平行添加 `Process(C.Mentor*)`。
4. **玩法逻辑**：`ServerLibrary/Models/PlayerObject.cs:2913-3179` 的 `#region Marriage` 旁新建 `#region Mentor`，方法范式直接复刻 MarriageRequest/Join/Leave（邀请字段加到 :160 的 `GroupInvitation, GuildInvitation, MarriageInvitation` 一行，掉线清理加到 :309）。
5. **NPC 集成**：`NPCActionType`（LibraryCore/SystemModels/NPCInfo.cs:1055-1057 Marriage/Divorce 之后）加 `Mentor`/`DismissMentor`；`NPCCheckType`（:1014-1015）加 `Mentorship`；分发处照抄 ServerLibrary/Models/NPCObject.cs:138-146。
6. **客户端（Godot）**：事件注册范式照抄 GodotClient/Network/ServerConnection.cs:106-109 + :453-456；订阅/退订照抄 GodotClient/Scripts/GameScene.cs:1103-1106/1662-1665。
