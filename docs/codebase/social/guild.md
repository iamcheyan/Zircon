# 行会系统（GuildInfo 数据模型 + PlayerObject.Guild* 全流程 + 行会战/行会税/行会仓库）

## TL;DR 速查表

- 行会主体就是 DB 模型 `GuildInfo`（ServerLibrary/DBModels/GuildInfo.cs:14），ServerLibrary 里**没有独立的运行时 `class Guild`**；所有行会操作都是 `PlayerObject` 上的 `Guild*` 方法直接改 `GuildInfo`/`GuildMemberInfo`。
- 建会费用公式：`cost = p.Members * Globals.GuildMemberCost + p.Storage * Globals.GuildStorageCost`，金币建会再加 `Globals.GuildCreationCost`（PlayerObject.cs:4639-4642）；常量：创建 7,500,000、扩员 1,000,000/人、扩仓 350,000/格、宣战 200,000（LibraryCore/Globals.cs:129-133）。
- 权限是 `[Flags] enum GuildPermission`（LibraryCore/Enum.cs:1923-1938）：`Leader = -1`（全权限位），其余 `EditNotice=1 / AddMember=2 / RemoveMember=4 / Storage=8 / FundsRepair=16 / FundsMerchant=32 / FundsMarket=64 / StartWar=128`。
- 成员上限/仓库上限扩容只能用行会资金（GuildFunds），且同时扣 DailyGrowth；上限分别 100 人 / 500 格（PlayerObject.cs:4889, 4924）。
- 行会税：会长设置 0~100，`GuildTax = p.Tax / 100M`（PlayerObject.cs:4866-4868）；成员**拾取金币**时按 `Ceiling(Count * GuildTax)` 抽税进 GuildFunds 并记贡献（GuildInfo.cs:347-356，ItemObject.cs:87-98）。
- 行会 buff 不是"行会技能"：`ApplyGuildBuff()` 按在线成员数分档给经验/掉落/金币率加成（≤15 人 +30、≤30 人 +23、≤45 人 +18、更多 +13；新手会 Level<50 时 +50、≥50 时 -50，PlayerObject.cs:9369-9415）。
- 行会战宣战：需要 `GuildPermission.StartWar`，扣 200,000 行会资金，`GuildWarInfo.Duration = TimeSpan.FromHours(2)`，到期由 `SEnvir.CheckGuildWars()` 删除（PlayerObject.cs:5038-5053，SEnvir.cs:1859-1880）。
- 宣战/攻城中 `AtWar()` 为真则攻击**不棕名不红名**（PlayerObject.cs:16631-16634），死亡只发行会战播报不发谋杀提示（PlayerObject.cs:16344-16358）。
- 行会仓库是 `GuildInfo.Storage`（UserItem[1000] 数组，实际可用格数 = StorageSize），存取需要 `GuildPermission.Storage` 且在安全区（PlayerObject.cs:7482-7500）；不可交易/绑定物品禁止放入（PlayerObject.cs:7793-7805）。
- `GuildMemberInfo.Contribute()` 里贡献值**重复累加了两次**（GuildMemberInfo.cs:136-140），是照抄原样的疑似 bug，移植对齐时需决定是否复刻。

## 职责概述

本文覆盖 Zircon 服务端的行会子系统全量逻辑，供 Godot 客户端对齐行会 UI 与状态同步：

1. **数据模型**：`GuildInfo`（行会本体 + 仓库数组 + 成员/物品关联）、`GuildMemberInfo`（账号级行会成员关系 + 权限 + 贡献）、`GuildWarInfo`（行会战宣战关系，2 小时计时）。全部是 MirDB `DBObject`，存 Users.db。
2. **操作入口**：`PlayerObject` 的 `#region Guild`（PlayerObject.cs:4627 起）——创建/公告/成员管理/踢人/税/扩容/邀请/宣战/攻城申请/颜色/旗帜/城门开关/修理/入会/退会，以及 `SConnection.Process(C.Guild*)` 的包分发。
3. **同步协议**：`S.GuildInfo`（全量）、`S.GuildUpdate`（增量）、`S.GuildMemberOnline/Offline`（成员在线状态）、`S.GuildMemberContribution`、`S.GuildFundsChanged`、`S.GuildKick`、`S.GuildDayReset`、`S.GuildChanged`（头顶行会名广播）等。
4. **行会战与 PK 互免**：`GuildWarInfo` 的宣战/到期流程 + `PlayerObject.AtWar()` 在攻击模式（WarRedBrown）与棕名判定中的豁免作用。
5. **行会仓库**：`GridType.GuildStorage` 的 ItemMove 分支、权限与安全区限制、存入物品过滤。
6. **行会 buff**：`BuffType.Guild` / `BuffType.Castle` 的 `ApplyGuildBuff/ApplyCastleBuff`（城堡 buff 归属攻城系统，见 sys/conquest-sabuk.md，此处一并给出代码）。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| ServerLibrary/DBModels/GuildInfo.cs | 14-390 | 行会 DB 模型：名称/上限/资金/等级/公告/税率/贡献/默认职务权限/新手会标记/攻城申请/城堡/颜色/旗帜 + Storage 数组 + Members/Items 关联 |
| ServerLibrary/DBModels/GuildInfo.cs | 283-311 | `ToClientInfo()`：转 `ClientGuildInfo` 下发 |
| ServerLibrary/DBModels/GuildInfo.cs | 329-338 | `OnCreated()`：默认 Rank="New Member"、随机颜色、随机旗帜 0-8 |
| ServerLibrary/DBModels/GuildInfo.cs | 347-356 | `CalculateGuildTax(item)`：只对金币（SEnvir.GoldInfo）征收 |
| ServerLibrary/DBModels/GuildInfo.cs | 358-384 | `GetUpdatePacket()`：S.GuildUpdate（Members 列表留空调用方补） |
| ServerLibrary/DBModels/GuildMemberInfo.cs | 11-174 | 成员 DB 模型：Guild/Rank/Account/JoinDate/贡献/Permission |
| ServerLibrary/DBModels/GuildMemberInfo.cs | 129-148 | `Contribute(amount)`：资金入账 + 双重贡献累加（疑似 bug）+ 全员广播 |
| ServerLibrary/DBModels/GuildMemberInfo.cs | 150-173 | `ToClientInfo()`：在线时 `Online=TimeSpan.MinValue` 表示"在线" |
| ServerLibrary/DBModels/GuildWarInfo.cs | 8-54 | 行会战关系：Guild1/Guild2/Duration（宣战时固定 2 小时） |
| LibraryCore/Enum.cs | 1923-1938 | `[Flags] GuildPermission` 权限位枚举 |
| LibraryCore/Globals.cs | 47 | `GuildNameRegex`：`^[A-Za-z0-9]{n,m}$`（仅字母数字） |
| LibraryCore/Globals.cs | 129-133 | 行会费用常量（创建/扩员/扩仓/宣战） |
| LibraryCore/Globals.cs | 946-1002 | `ClientGuildInfo` / `ClientGuildMemberInfo` 下发结构（含 `Permission` 派生属性、`Complete()` 把 Online 换算 LastOnline） |
| LibraryCore/Network/ServerPackets.cs | 999-1154 | 行会全部 S 包（GuildCreate/GuildInfo/GuildUpdate/GuildKick/GuildInvite/GuildMemberOnline/Offline/Contribution/DayReset/FundsChanged/GuildChanged/GuildWar*/GuildConquest*/GuildCastleInfo） |
| LibraryCore/Network/ClientPackets.cs | 551-627, 741-744 | 行会全部 C 包（GuildCreate/EditNotice/EditMember/InviteMember/KickMember/Tax/IncreaseMember/IncreaseStorage/Response/War/RequestConquest/Colour/Flag/ToggleCastleGates/RepairCastleGates/RepairCastleGuards/JoinStarterGuild） |
| ServerLibrary/Models/PlayerObject.cs | 4627-5596 | `#region Guild` 全部操作实现 |
| ServerLibrary/Models/PlayerObject.cs | 4629-4728 | `GuildCreate`：建会校验 + 费用公式 + 蚂蚁角（UmaKingHorn）替代创建费 |
| ServerLibrary/Models/PlayerObject.cs | 5499-5521 | `AtWar(player)`：攻城图内全员开战 / 行会战互免判定 |
| ServerLibrary/Models/PlayerObject.cs | 5523-5557 | `SendGuildInfo()`：登录/入会后全量下发 + 在打行会战 + 攻城日程 |
| ServerLibrary/Models/PlayerObject.cs | 9355-9430 | `ApplyCastleBuff` / `ApplyGuildBuff`：行会与城堡增益 |
| ServerLibrary/Models/PlayerObject.cs | 979-987 / 1078-1092 | 下线广播 `S.GuildMemberOffline` / 上线广播 `S.GuildMemberOnline` |
| ServerLibrary/Models/PlayerObject.cs | 7454-7805 | `ItemMove` 的 `GridType.GuildStorage` 分支（权限/安全区/槽位上限/过滤） |
| ServerLibrary/Models/PlayerObject.cs | 15988-16016, 16631-16634 | `AtWar` 在 WarRedBrown 攻击模式与棕名豁免中的调用 |
| ServerLibrary/Envir/SEnvir.cs | 1859-1880 | `CheckGuildWars()`：主循环里扣减 Duration，到期删除并广播 GuildWarFinished |
| ServerLibrary/Envir/SEnvir.cs | 1588-1605 | 跨天重置：DailyContribution/DailyGrowth 清零 + `S.GuildDayReset` |
| ServerLibrary/Models/ItemObject.cs | 82-98, 114-137 | 地面金币拾取征收行会税（玩家/宠物两条路径） |
| ServerLibrary/Models/MonsterObject.cs | 2871, 2977 | 怪物掉落金币由主人拾取时同样走 `Contribute(taxableAmount)` |
| ServerLibrary/Envir/SConnection.cs | 1191-1290, 1239-1247, 1468-1473 | `Process(C.Guild*)` 包分发（含 GuildResponse→GuildJoin、JoinStarterGuild） |
| ServerLibrary/Envir/Commands/Command/Admin/CreateGuild.cs | 45-47 | 管理员建会（同样只设 GuildLevel=1） |
| Client/Scenes/Views/GuildDialog.cs | 20-2430 | 原版 WinForms 行会窗体（7 页签：Create/Home/Member/Storage/War/Style/Castle） |

## 核心流程

### 1. 创建行会（PlayerObject.GuildCreate，PlayerObject.cs:4629-4728）

```csharp
public void GuildCreate(C.GuildCreate p)
{
    Enqueue(new S.GuildCreate { ObserverPacket = false });

    if (Character.Account.GuildMember != null) return;

    if (string.IsNullOrWhiteSpace(p.Name)) return;
    if (p.Members < 0 || p.Members > 100) return;
    if (p.Storage < 0 || p.Storage > 500) return;

    long cost = p.Members * Globals.GuildMemberCost + p.Storage * Globals.GuildStorageCost;

    if (p.UseGold)
        cost += Globals.GuildCreationCost;
    else
    {
        bool result = false;
        for (int i = 0; i < Inventory.Length; i++)
        {
            if (Inventory[i] == null || Inventory[i].Info.ItemEffect != ItemEffect.UmaKingHorn) continue;

            result = true;
            break;
        }

        if (!result)
        {
            Connection.ReceiveChatWithObservers(con => con.Language.GuildNeedHorn, MessageType.System);
            return;
        }
    }

    if (cost > Gold.Amount)
    {
        Connection.ReceiveChatWithObservers(con => con.Language.GuildNeedGold, MessageType.System);
        return;
    }

    if (!Globals.GuildNameRegex.IsMatch(p.Name))
    { ... return; }

    GuildInfo info = SEnvir.GuildInfoList.Binding.FirstOrDefault(x => string.Compare(x.GuildName, p.Name, StringComparison.OrdinalIgnoreCase) == 0);

    if (info != null)
    { ... return; }  // 重名

    info = SEnvir.GuildInfoList.CreateNewObject();

    info.GuildName = p.Name;
    info.MemberLimit = 10 + p.Members;
    info.StorageSize = 10 + p.Storage;
    //info.GuildFunds = Globals.GuildCreationCost;
    info.GuildLevel = 1;

    GuildMemberInfo memberInfo = SEnvir.GuildMemberInfoList.CreateNewObject();

    memberInfo.Account = Character.Account;
    memberInfo.Guild = info;
    memberInfo.Rank = "Guild Leader";
    memberInfo.JoinDate = SEnvir.Now;
    memberInfo.Permission = GuildPermission.Leader;

    if (!p.UseGold)
    {
        // 背包里删除一个 ItemEffect.UmaKingHorn（蚂蟥王角/沃玛号角）
        ...
    }

    LogMilestone(MilestoneType.GuildCreate, 1);

    Gold.Amount -= cost;
    GoldChanged();

    SendGuildInfo();
}
```

要点：
- 两种创建方式：**金币**（7,500,000 + 扩容费）或**沃玛号角**（`ItemEffect.UmaKingHorn`，免 750 万但扩容费照付）。
- 基础容量：成员 10 + p.Members、仓库 10 + p.Storage；`GuildLevel` 固定 1，**全引擎没有任何升级 GuildLevel 的代码**（仅 GuildInfo.cs:76 定义、PlayerObject.cs:4688 与 CreateGuild.cs:46 写 1、GetUpdatePacket 回显），"行会等级"是未实装的占位字段。
- 创建后创建者自动成为 `Rank = "Guild Leader"`、`Permission = GuildPermission.Leader`。

### 2. 入会 / 邀请（GuildInviteMember:4945-4998 + GuildJoin:5386-5455）

邀请校验链（任一失败即返回）：`Permission & AddMember` → 目标在线 → 目标无行会 → 目标未被邀请中 → 无屏蔽 → 目标 `AllowGuild` 开启 → 成员数 < MemberLimit。通过后 `player.GuildInvitation = this` 并发 `S.GuildInvite`。

被邀者回 `C.GuildResponse { Accept }` → `SConnection.Process(C.GuildResponse)`（SConnection.cs:1239-1247）→ `Player.GuildJoin()`：

```csharp
// PlayerObject.cs:5422-5449（节选）
GuildMemberInfo memberInfo = SEnvir.GuildMemberInfoList.CreateNewObject();

memberInfo.Account = Character.Account;
memberInfo.Guild = GuildInvitation.Character.Account.GuildMember.Guild;
memberInfo.Rank = GuildInvitation.Character.Account.GuildMember.Guild.DefaultRank;
memberInfo.JoinDate = SEnvir.Now;
memberInfo.Permission = GuildInvitation.Character.Account.GuildMember.Guild.DefaultPermission;

SendGuildInfo();
...
Broadcast(new S.GuildChanged { ObjectID = ObjectID, GuildName = memberInfo.Guild.GuildName, GuildRank = memberInfo.Rank });
AddAllObjects();

S.GuildUpdate update = memberInfo.Guild.GetUpdatePacket();
update.Members.Add(memberInfo.ToClientInfo());

foreach (GuildMemberInfo member in memberInfo.Guild.Members)
{
    if (member == memberInfo || member.Account.Connection?.Player == null) continue;
    member.Account.Connection.ReceiveChat(string.Format(member.Account.Connection.Language.GuildMemberJoined, GuildInvitation.Name, Name), MessageType.System);
    member.Account.Connection.Player.Enqueue(update);
    member.Account.Connection.Player.AddAllObjects();
    member.Account.Connection.Player.ApplyGuildBuff();
}

LogMilestone(MilestoneType.GuildJoin, 1);

ApplyCastleBuff();
ApplyGuildBuff();
```

新成员用的是行会 `DefaultRank`/`DefaultPermission`（GuildEditMember 传 Index=0 时改的就是默认值，PlayerObject.cs:4788-4797）。

### 3. 踢人 / 退会（GuildKickMember:4799-4853 / GuildLeave:5457-5497）

- 踢人需要 `GuildPermission.Leader`，不能踢自己；被踢者 `Account.GuildTime = SEnvir.Now.AddDays(1)`（24 小时冷却不能再入会），成员对象置空并 `Delete()`；被踢者收到 `S.GuildInfo`（空）+ 头顶广播 `S.GuildChanged`（无参=清除），其余成员收 `S.GuildKick { Index }`。
- 退会：**会长且行会还有其他成员且没有另一个 Leader 时禁止退会**（PlayerObject.cs:5463-5467）；非新手会退会同样 24 小时冷却（`!guild.StarterGuild` 才设 GuildTime，PlayerObject.cs:5476-5477）。
- 注意 `GuildKickMember` 的权限判断用的是 `GuildPermission.Leader` 而不是 `RemoveMember` 位——**`RemoveMember = 4` 这个枚举位在服务端从未被检查**（全库 grep 仅见枚举定义），踢人事实上只有会长能做。

### 4. 行会税与资金（GuildTax:4854-4874 + 拾取抽税）

```csharp
// PlayerObject.cs:4866-4868  会长设税（需 Leader 权限）
if (p.Tax < 0 || p.Tax > 100) return;

Character.Account.GuildMember.Guild.GuildTax = p.Tax / 100M;
```

```csharp
// GuildInfo.cs:347-356  只有金币会被抽税
public long CalculateGuildTax(UserItem item)
{
    if (GuildTax <= 0) return 0;

    if (item == null || item.Info != SEnvir.GoldInfo) return 0;

    long amount = (long)Math.Ceiling(item.Count * GuildTax);

    return amount;
}
```

```csharp
// ItemObject.cs:87-98  地面金币拾取时入账（宠物拾取路径 114-137 相同）
long taxableAmount = Account?.GuildMember?.Guild?.CalculateGuildTax(Item) ?? 0;

ItemCheck check = new ItemCheck(Item, Item.Count - taxableAmount, Item.Flags, Item.ExpireTime);

if (ob.CanGainItems(false, check))
{
    if (taxableAmount > 0)
    {
        Item.Count -= taxableAmount;
        Account.GuildMember.Contribute(taxableAmount);
    }
    ...
}
```

注意抽税主体是**掉落归属者**（`Account`，即 `CanPickUpItem` 判定后的拾取人账号），税从拾取金币里直接扣除进 GuildFunds。怪物掉落金币直接入包（MonsterObject.cs:2871、2977）也走 `Contribute(taxableAmount)`。

`Contribute`（GuildMemberInfo.cs:129-148）：

```csharp
public void Contribute(long amount)
{
    if (amount <= 0) return;

    Guild.GuildFunds += amount;
    Guild.DailyGrowth += amount;

    DailyContribution += amount;
    TotalContribution += amount;

    DailyContribution += amount;
    TotalContribution += amount;

    foreach (GuildMemberInfo member in Guild.Members)
    {
        if (member.Account.Connection?.Player == null) continue;
        member.Account.Connection.Enqueue(new S.GuildMemberContribution { Index = Index, Contribution = amount, ObserverPacket = false });
    }
}
```

**资金/Growth 只加一次，但成员个人贡献 Daily/Total 各加两次**（136-137 与 139-140 重复）——客户端显示的个人贡献是双倍实际值，移植时要么复刻要么修掉并同步显示预期。

资金消耗点：宣战（-200,000）、扩员（-1,000,000）、扩仓（-350,000）、修城门/守卫（见 conquest-sabuk.md）；所有消耗同时 `DailyGrowth -= cost`。**没有"会长取出行会资金"的接口**——GuildFunds 只进不出（对玩家而言），NPC 购买/修理/寄卖勾选"使用行会资金"时在服务端直接从 GuildFunds 扣款（客户端入口见 GodotClient NPCGoodsPanel.cs:43、NPCRepairPanel.cs:46、ConsignmentDialog.cs:132-166）。

### 5. 扩容（GuildIncreaseMember:4875-4910 / GuildIncreaseStorage:4911-4944）

```csharp
// GuildIncreaseMember（节选）
if (guild.MemberLimit >= 100) { ... return; }          // 上限 100
if (guild.GuildFunds < Globals.GuildMemberCost) { ... return; }

guild.GuildFunds -= Globals.GuildMemberCost;
guild.DailyGrowth -= Globals.GuildMemberCost;
Character.Account.GuildMember.Guild.MemberLimit++;
```

仓库同理：上限 500，每格 350,000。扩容后广播 `GetUpdatePacket()`。

### 6. 行会战宣战（GuildWar:4999-5065）

```csharp
if ((Character.Account.GuildMember.Permission & GuildPermission.StartWar) != GuildPermission.StartWar)
{
    Connection.ReceiveChatWithObservers(con => con.Language.GuildWarPermission, MessageType.System);
    return;
}
...
if (SEnvir.GuildWarInfoList.Binding.Any(x => (x.Guild1 == guild && x.Guild2 == Character.Account.GuildMember.Guild) ||
                                             (x.Guild2 == guild && x.Guild1 == Character.Account.GuildMember.Guild)))
{ ... return; }  // 已在交战

if (Globals.GuildWarCost > Character.Account.GuildMember.Guild.GuildFunds)
{ ... return; }

result.Success = true;

Character.Account.GuildMember.Guild.GuildFunds -= Globals.GuildWarCost;
Character.Account.GuildMember.Guild.DailyGrowth -= Globals.GuildWarCost;

GuildWarInfo warInfo = SEnvir.GuildWarInfoList.CreateNewObject();

warInfo.Guild1 = Character.Account.GuildMember.Guild;
warInfo.Guild2 = guild;
warInfo.Duration = TimeSpan.FromHours(2);
```

宣战后双方所有在线成员收 `S.GuildWarStarted { GuildName, Duration }`；到期由主循环 `SEnvir.CheckGuildWars()`（SEnvir.cs:1859-1880，主循环调用点 1541）扣减 Duration，耗尽后双方广播 `S.GuildWarFinished` 并删除记录。

### 7. PK 互免：AtWar（PlayerObject.cs:5499-5521）

```csharp
public bool AtWar(PlayerObject player)
{
    foreach (ConquestWar conquest in SEnvir.ConquestWars)
    {
        if (conquest.Map != CurrentMap) continue;

        if (Character.Account.GuildMember == null || player.Character.Account.GuildMember.Guild == null) return true;

        return Character.Account.GuildMember.Guild != player.Character.Account.GuildMember.Guild;
    }

    if (player.Character.Account.GuildMember == null) return false;
    if (Character.Account.GuildMember == null) return false;

    foreach (GuildWarInfo warInfo in SEnvir.GuildWarInfoList.Binding)
    {
        if (warInfo.Guild1 == Character.Account.GuildMember.Guild && warInfo.Guild2 == player.Character.Account.GuildMember.Guild) return true;
        if (warInfo.Guild2 == Character.Account.GuildMember.Guild && warInfo.Guild1 == player.Character.Account.GuildMember.Guild) return true;
    }

    return false;
}
```

语义：
- **攻城期间在攻城地图上**：无行会者与所有人交战；有行会者与不同行会的人交战（同行会免疫）。注意这是"当前地图任一 ConquestWar"即触发，没有限定城堡区域。
- **平时**：双方都有行会且存在 `GuildWarInfo` 关系（Guild1/Guild2 任意方向）才交战。

消费点：
1. 攻击模式 `AttackMode.WarRedBrown`：`if (player.Stats[Stat.Brown] == 0 && player.Stats[Stat.PKPoint] < Config.RedPoint && !AtWar(player)) return false;`（PlayerObject.cs:16014-16016，宠物版 15988-15990）——该模式下只能打棕名/红名/交战目标。
2. 棕名豁免：`if (AtWar(player)) return;` 在 `player.BuffAdd(BuffType.Brown, ...)` 之前（PlayerObject.cs:16631-16634）——攻击交战目标不棕名，进而无 PK 点。
3. 死亡播报分流：`AtWar(attacker)` 为真时给两边行会全员发 `GuildWarDeath`（"{3}行会的{2}在行会战里击败{1}行会的{0}"），否则才走"被谋杀"红名惩罚分支（PlayerObject.cs:16344-16396）。

### 8. 行会成员在线同步

- 上线（PlayerObject.cs:1078-1092）：给行会其他在线成员发 `S.GuildMemberOnline { Index, Name, ObjectID }`；下线（StopGame，PlayerObject.cs:979-987）发 `S.GuildMemberOffline { Index }`。
- 变更（入会/踢人/退会/改权限）：`S.GuildUpdate`（可带单个 `Members` 项）+ `S.GuildKick { Index }` + 头顶 `S.GuildChanged { ObjectID, GuildName, GuildRank }`（周围玩家可见），并 `AddAllObjects/RemoveAllObjects` 强制重发外观。
- 跨天（SEnvir.cs:1588-1605）：行会与成员的 DailyContribution、行会 DailyGrowth 清零，在线成员收 `S.GuildDayReset`。
- 全量：`SendGuildInfo()`（PlayerObject.cs:5523-5557）在登录（1169-1171 附近）、建会、入会时发送 `S.GuildInfo { Guild = ToClientInfo(), UserIndex }`，并补发进行中的 `S.GuildWarStarted` 和每座城堡的 `S.GuildConquestDate`。

### 9. 行会 buff（ApplyGuildBuff / ApplyCastleBuff，PlayerObject.cs:9355-9430）

```csharp
public void ApplyGuildBuff()
{
    BuffRemove(BuffType.Guild);

    if (Character.Account.GuildMember == null) return;

    Stats stats = new Stats();

    if (Character.Account.GuildMember.Guild.StarterGuild)
    {
        if (Level < 50)
        {
            stats[Stat.ExperienceRate] += 50;
            stats[Stat.DropRate] += 50;
            stats[Stat.GoldRate] += 50;
        }
        else
        {
            stats[Stat.ExperienceRate] -= 50;
            stats[Stat.DropRate] -= 50;
            stats[Stat.GoldRate] -= 50;
        }
    }
    else if (Character.Account.GuildMember.Guild.Members.Count <= 15)
    {
        stats[Stat.ExperienceRate] += 30;
        stats[Stat.DropRate] += 30;
        stats[Stat.GoldRate] += 30;
    }
    else if (Character.Account.GuildMember.Guild.Members.Count <= 30)
    {
        stats[Stat.ExperienceRate] += 23;
        stats[Stat.DropRate] += 23;
        stats[Stat.GoldRate] += 23;
    }
    else if (Character.Account.GuildMember.Guild.Members.Count <= 45)
    {
        stats[Stat.ExperienceRate] += 18;
        stats[Stat.DropRate] += 18;
        stats[Stat.GoldRate] += 18;
    }
    else
    {
        stats[Stat.ExperienceRate] += 13;
        stats[Stat.DropRate] += 13;
        stats[Stat.GoldRate] += 13;
    }
    ...
    BuffAdd(BuffType.Guild, TimeSpan.MaxValue, stats, false, false, TimeSpan.Zero);
}
```

触发时机：登录 buff 初始化（PlayerObject.cs:1144-1146）、入会/退会/踢人、组队变化（5875-5880 等）。**没有"行会技能树/行会升级学技能"系统**；`GuildLevel` 字段如上所述是占位。城堡 buff（`BuffType.Castle`，Exp/Drop/Gold 各 +10）见 sys/conquest-sabuk.md。

### 10. 行会仓库存取（ItemMove 的 GuildStorage 分支）

取出（FromGrid = GuildStorage，PlayerObject.cs:7482-7500）：

```csharp
case GridType.GuildStorage:
    if (Character.Account.GuildMember == null) return;

    if ((Character.Account.GuildMember.Permission & GuildPermission.Storage) != GuildPermission.Storage)
    {
        Connection.ReceiveChatWithObservers(con => con.Language.GuildStoragePermission, MessageType.System);
        return;
    }

    if (!InSafeZone && !(p.ToGrid == GridType.Storage || p.ToGrid == GridType.PartsStorage))
    {
        Connection.ReceiveChatWithObservers(con => con.Language.GuildStorageSafeZone, MessageType.System);
        return;
    }

    fromArray = Character.Account.GuildMember.Guild.Storage;

    if (p.FromSlot >= Character.Account.GuildMember.Guild.StorageSize) return;
    break;
```

放入（ToGrid = GuildStorage，PlayerObject.cs:7556-7574）同样校验 `Storage` 权限 + 安全区 + `p.ToSlot >= StorageSize` 上限。物品过滤（PlayerObject.cs:7793-7805）：**放入行会仓库的物品必须 `CanTrade` 且非绑定（Bound）**；从行会仓库取出目标格非空且目标不是行会仓时拒绝（强制合并语义）。婚姻标记物品（`UserItemFlags.Marriage`）任何方向都禁移（7521）。

仓库与客户端的增量同步：`S.GuildNewItem { Slot, Item }` / `S.GuildGetItem { Grid, Slot }`（ServerPackets.cs:1011-1022），由 ItemMove 的 guildpacket 分支（PlayerObject.cs:7724-7760）发给全行会在线成员。

## 数据结构/协议细节

### GuildPermission（LibraryCore/Enum.cs:1923-1938）

```csharp
[Flags]
public enum GuildPermission
{
    None = 0,

    Leader = -1,

    EditNotice = 1,
    AddMember = 2,
    RemoveMember = 4,
    Storage = 8,
    FundsRepair = 16,
    FundsMerchant = 32,
    FundsMarket = 64,
    StartWar = 128,
}
```

### 操作 × 权限矩阵（全部来自 PlayerObject.Guild* 的实际判断）

| 操作 | 方法（PlayerObject.cs） | 要求权限 | 其他条件 |
|---|---|---|---|
| 建会 | GuildCreate:4629 | 无 | 无行会、名字合法且未占用、金币或号角 |
| 改公告 | GuildEditNotice:4729 | `EditNotice` | 长度 ≤ MaxGuildNoticeLength |
| 改成员职务/权限 | GuildEditMember:4748 | `Leader` | 不能改自己的 Permission（4775-4776） |
| 设默认职务/权限 | GuildEditMember（Index=0）:4788 | `Leader` | — |
| 踢人 | GuildKickMember:4799 | `Leader` | 不能踢自己；被踢者 24h 冷却 |
| 设行会税 | GuildTax:4854 | `Leader` | 0-100 |
| 扩成员上限 | GuildIncreaseMember:4875 | `Leader` | GuildFunds ≥ 1,000,000，上限 100 |
| 扩仓库上限 | GuildIncreaseStorage:4911 | `Leader` | GuildFunds ≥ 350,000，上限 500 |
| 邀请成员 | GuildInviteMember:4945 | `AddMember` | 目标在线/无会/未被邀/AllowGuild/有人数空位 |
| 宣行会战 | GuildWar:4999 | `StartWar` | 未与对方交战、GuildFunds ≥ 200,000 |
| 申请攻城 | GuildConquest:5066 | `Leader` | 见 sys/conquest-sabuk.md |
| 改行会颜色 | GuildColour:5151 | `Leader` | 改后刷新城堡旗帜 |
| 改行会旗帜 | GuildFlag:5175 | `Leader` | flag 0-9 |
| 行会仓库存取 | ItemMove:7482/7556 | `Storage` | 安全区（转移到个人仓库/配件仓除外） |
| 使用行会资金购物/修理 | NPC 相关 | （服务端扣款处未见权限位校验，客户端按 HasGuild 灰化） | — |

注：`RemoveMember`、`FundsRepair`、`FundsMerchant`、`FundsMarket` 四个位在服务端源码中没有出现任何 `(Permission & Xxx)` 判断（本次全库检索确认），是预留给客户端 UI 展示/未来校验的"哑权限位"；`FundsMarket` 等的语义仅体现在客户端勾选框把请求发给服务端后由服务端直接扣 GuildFunds。

### ClientGuildInfo / ClientGuildMemberInfo（LibraryCore/Globals.cs:946-1002）

- `ClientGuildInfo`：GuildName/Notice/MemberLimit/GuildFunds/DailyGrowth/Total·DailyContribution/UserIndex/StorageLimit/Tax(=int, 百分比)/DefaultRank/DefaultPermission/Colour/Flag/Members/Storage。`Permission` 是 `[IgnorePropertyPacket]` 派生属性（976）：按 UserIndex 从 Members 里找自己的权限。
- `ClientGuildMemberInfo`：Index/Name/Rank/贡献/`Online`（`TimeSpan.MinValue` 表示在线）/Permission/LastOnline（Complete() 用 `Time.Now - Online` 换算，996-1000）/ObjectID（在线玩家的对象 ID，可用于组队邀请等）。

### 费用常量（LibraryCore/Globals.cs:129-133）

```csharp
public static long
    GuildCreationCost = 7500000,
    GuildMemberCost = 1000000,
    GuildStorageCost = 350000,
    GuildWarCost = 200000;
```

### 新手行会（StarterGuild）

`GuildInfo.StarterGuild` 标记；玩家可用 `C.JoinStarterGuild`（ClientPackets.cs:741-744，处理 SConnection.cs:1468-1473 → PlayerObject.JoinStarterGuild:5559-5594）免条件加入系统配置的新手会。新手会 buff：50 级以下 +50% 三率、50 级及以上 **-50%** 三率（ApplyGuildBuff）；退新手会不会触发 24h 冷却（PlayerObject.cs:5476-5477）。

## GodotClient 现状

（以下结论均基于本次对 GodotClient/ 的 glob/grep 实际检索）

| 功能 | 状态 | 依据 |
|---|---|---|
| 行会主窗体（7 页签：创建/主页/成员/仓库/战争/样式/城堡） | **已移植** | GodotClient/Controls/GuildDialog.cs:59-61（`tabs = { CreateLabel, MembersTab, StorageTab, WarTab, Ui292, CastleTab }`）；对照原版 Client/Scenes/Views/GuildDialog.cs:387-399 |
| 建会页（金币/号角二选一、扩容预览、实时总价） | **已移植** | GuildDialog.cs:178-228；费用公式 `Globals.GuildCreationCost + Members*GuildMemberCost + Storage*GuildStorageCost`（206-212）与服务端一致 |
| 加入新手会 | **已移植** | GuildDialog.cs:225-226 → `SendJoinStarterGuild()`（ServerConnection.cs:1070） |
| 邀请入会 + 接受/拒绝弹层 | **已移植** | GuildDialog.cs:232-244 `ShowInvite`；GameScene.cs:2593-2598 `OnGuildInvite` |
| 公告查看/编辑/保存 | **已移植** | GuildDialog.cs:246-274；`SendGuildEditNotice`（GameScene.cs:6403） |
| 成员列表（在线状态/职务/贡献）+ 成员管理弹窗（改职务/权限/踢人） | **已移植** | GuildDialog.cs:117-142；GuildMemberDialog.cs:8-61（权限循环只覆盖 None→AddMember→Storage→FundsMerchant→None 四档，比原版少，属部分简化） |
| 行会仓库网格（11 列 × 动态行数、滚动、存取路由） | **已移植** | GuildDialog.cs:144-171；GameScene.cs:658 `GuildStorageItemCells`；DXItemCell.cs:922-928 安全区/绑定过滤与服端一致 |
| 行会资金显示与变更 | **已移植** | GuildDialog.cs:36 `GuildFunds`；GameScene.cs:2592 `OnGuildFundsChanged` |
| 成员在线/离线/贡献增量同步 | **已移植** | GameScene.cs:2589-2591（OnGuildMemberOffline/Online/Contribution） |
| 设税 | **已移植** | GuildDialog.cs:289-292（主页页脚输入框 + `SendGuildTax`） |
| 扩员/扩仓按钮 | **已移植** | GuildDialog.cs:68-71（`SendGuildIncreaseMember/Storage`） |
| 样式页（旗帜 0-9 预览 + 颜色） | **已移植** | GuildDialog.cs:296-325（用 `LibraryFile.CastleFlag` 图库 `Index = Flag*100` 双层染色）；`SendGuildColour/Flag`（ServerConnection.cs:1071-1072） |
| 行会战宣战按钮（输入对方行会名） | **已移植（入口在战争页）** | GuildDialog.cs:335-368 BuildWarPage（城堡列表+宣战攻城申请按钮）；`SendGuildWar`（ServerConnection.cs:1096）；行会对行会的普通宣战输入窗在 GodotClient 中未见独立入口，`_guildWars` 状态与 GuildWarStarted/Finished 已处理（GameScene.cs:2262-2279） |
| 行会聊天（`!~` 前缀） | **已移植** | GodotClient/Controls/ChatTextBox.cs:109-111（`ChatMode.Guild => "!~"`）；ChatLogPanel.cs:510-511、ChatOptionsDialog.cs:33 过滤 |
| 行会 buff 图标（BuffType.Guild/Castle） | **已移植** | GodotClient/Controls/BuffDialog.cs:130-131、164-165（Guild 图标 140、Castle 242） |
| 行会旗帜/颜色渲染（头顶、查看角色面板） | **已移植** | CharacterDialog.cs:21-22/47-48/331-333（`_guildFlagBase/_guildFlagOverlay` 两层）；RankingDialog.cs:52-53 |
| 使用行会资金（NPC 购买/修理/寄卖） | **已移植** | NPCGoodsPanel.cs:43-84、NPCRepairPanel.cs:46-54/210-246、ConsignmentDialog.cs:132-166 |
| 服务器连接层行会 C/S 包 | **已移植** | ServerConnection.cs:102-105、244-259、272-273（事件）、564-569、1064-1083、1096-1105（发送） |
| 行会战/攻城状态（GuildWarStarted/Finished、GuildConquestDate/Started/Finished、GuildCastleInfo） | **已移植** | GameScene.cs:1389-1402 订阅；2636-2659 处理并刷新战争页 |
| 会长限制类 UI 灰化（按权限位禁用按钮） | **部分移植** | GuildDialog.cs:100-105 仅按"有无行会"显隐管理按钮，未按 GuildPermission 位灰化（原版按 `GameScene.Game.GuildInfo.Permission` 控制），移植攻城/管理功能时需补 |

## 移植注意事项

1. **没有运行时 Guild 类**：所有状态都在 `GuildInfo`/`GuildMemberInfo` 上，客户端要的每个字段都在 `ClientGuildInfo`/`ClientGuildMemberInfo`（Globals.cs:946-1002）里，Godot 端不需要自建行会模型，直接吃 `S.GuildInfo`/`S.GuildUpdate` 即可。
2. **"在线"编码**：`Online == TimeSpan.MinValue` 表示在线（GuildMemberInfo.cs:163-169），离线时 `Online = Now - 最后登录时间`；`ClientGuildMemberInfo.Complete()` 再把它换算回 `LastOnline`。Godot 端解析时不要把它当普通 TimeSpan 排序。
3. **贡献双计数**：`Contribute()` 把个人 Daily/Total 贡献加两次（GuildMemberInfo.cs:136-140）。若 Godot 端自行按 `S.GuildMemberContribution` 累加显示，会与服务端下发的全量值一致（都是双倍）；若修 bug 需两端（其实只有服务端）一起改，否则增量与全量不一致。
4. **权限位语义空转**：`RemoveMember/FundsRepair/FundsMerchant/FundsMarket` 服务端不校验；Godot UI 若按这些位灰化按钮，实际安全边界仍在服务端的 `Leader`/`Storage`/`AddMember`/`StartWar`/`EditNotice` 五个位上。
5. **24 小时入会冷却**：踢出/退会后 `Account.GuildTime = Now.AddDays(1)`，`GuildJoin` 会拒绝（5398-5402）；新手会例外。UI 上被踢后立刻搜邀请会得到 "你不能加入另一个行会" 提示，属预期。
6. **会长离会保护**：唯一 Leader 且还有成员时禁止退会（5463-5467）。要解散行会必须先踢光成员（没有显式"解散"指令；管理员有 CreateGuild 命令但无 Disband）。
7. **行会仓库安全区例外**：从行会仓取出转到个人仓库（Storage/PartsStorage）允许在非安全区做（7491），其余方向必须在安全区；放入行会仓的物品必须可交易且非绑定——DXItemCell.cs:922-928 已按同样规则做过客户端预过滤，改动服务端规则时记得同步。
8. **行会税只作用于金币拾取**（`item.Info == SEnvir.GoldInfo`），商店/NPC 交易不收税；Castle buff 与行会 buff 叠加（两个独立 BuffType），总加成简单相加。
9. **跨天重置包**：`S.GuildDayReset` 只在日期变更的那次主循环计数里发（SEnvir.cs:1588-1605），Godot 端收到后应清零所有 Daily 字段并刷新成员列表。
10. **行会名正则**：`^[A-Za-z0-9]{n,m}$`（Globals.cs:47）——**中文名会被拒绝**。Godot 输入框应做同样的预校验（GuildDialog.cs:221 已做）。
