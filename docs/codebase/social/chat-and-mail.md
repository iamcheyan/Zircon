# 社交系统：聊天频道（Chat）/ 喇叭公告 / 邮件（Mail）/ 好友与屏蔽

## TL;DR 速查表

- 频道枚举**叫 `MessageType` 不叫 ChatType**（LibraryCore/Enum.cs:546-562），共 14 个值；`PlayerObject.Chat` 里出现的 `ChatType.System`（PlayerObject.cs:9791/9797）是 9785-9802 **块注释里的死代码**，勿据此建模。
- 聊天入口只有一个包 `C.Chat{Text, LinkedItemIndexes}`，服务端 `PlayerObject.Chat`（PlayerObject.cs:1596-1828）**按文本前缀路由**：`/`私聊、`!!`组队、`!~`行会、`!@`全服喇叭、`!`喊话、`@!`GM 公告、`@`命令、`#`观察者、无前缀普通。
- 限制：喊话 2 级 + 10 秒 CD（`Config.ShoutDelay`，Config.cs:101）；全服喇叭 33 级（或 `Stat.GlobalShout`）+ 30 秒 CD（PlayerObject.cs:1714 写死 AddSeconds(30)）；私聊/组队/喊话/普通频道受账号级禁言 `AccountInfo.ChatBanExpiry` 管控（GM 指令 `CHATBAN`，默认 365 天）。
- **敏感词过滤：未找到实现**；只有聊天落盘日志 `SEnvir.LogChat`（SEnvir.cs:61-64）。
- 邮件系统已完整实现（`MailInfo` 挂在 `AccountInfo.Mail`），支持文字 + 5 格附件 + 随信金币；**付费邮件（COD，收件人付款取件）未找到实现**——`C.MailSend.Gold` 是寄件人自己贴金币，不是货到付款。
- 邮件配额：收件人附件总格数 `Globals.MaxMailStorage = 50`（Globals.cs:106）；发信 10 秒节流（`MailTime`，PlayerObject.cs:3749-3751）；主题 ≤30 字、正文 ≤300 字（:3790-3798）。
- 拍卖行成交/下架、商城赠送等**系统邮件**由服务端自动生成（Sender="Market Place"/"System"）并推 `S.MailNew`。
- 好友 `FriendInfo` 挂在**角色**上（CharacterInfo.Friends），屏蔽 `BlockInfo` 挂在**账号**上（AccountInfo.BlockingList）；屏蔽判定 `SEnvir.IsBlocking` 是**双向**的（SEnvir.cs:4040-4046），拦截私聊/组队/行会/喇叭/喊话/普通/邮件全部频道。
- 好友在线状态推送：`PlayerObject.UpdateOnlineState`（PlayerObject.cs:17315-17330）遍历 `FriendedBy` 发 `S.FriendUpdate` + 系统聊天提示。
- GodotClient：聊天（含频道过滤/物品链接）、邮件、好友、屏蔽**均已移植**。

## 职责概述

本文覆盖 Zircon 引擎"玩家间异步与即时通讯"管线，供 Godot 客户端对齐 UI 与包时序：

1. **聊天频道**：`MessageType` 枚举全表、`C.Chat→PlayerObject.Chat` 前缀路由、各频道的等级/CD/禁言（ChatBan）限制、GM 标记（`TempAdmin`/`GMWhisperIn`）、物品链接注入。
2. **喇叭/全服公告**：`!@` Global 与 `@!` Announcement 的触发与广播范围。
3. **邮件系统**：`MailInfo` 模型、`C.MailSend/MailOpened/MailGetItem/MailDelete` 与 `S.MailList/MailNew/MailDelete/MailItemDelete/MailSend` 的完整时序、附件/金币/系统邮件、存储时机。
4. **好友/屏蔽**：`FriendInfo/BlockInfo`、增删包、在线状态通知、屏蔽的双向拦截范围。
5. **Client UI 对照**：ChatTextBox/ChatTab/ChatOptionsDialog/CommunicationDialog 的关键控件行为。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| LibraryCore/Enum.cs | 546-562 | `MessageType` 枚举全部 14 值（本仓库的"ChatType"） |
| LibraryCore/Globals.cs | 85-86 | `MaxChatLength=120`、`MaxChatItemLinks=10` |
| LibraryCore/Globals.cs | 106 | `MaxMailStorage=50`（收件人附件格上限） |
| LibraryCore/Network/ClientPackets.cs | 237-241 | `C.Chat`（Text + LinkedItemIndexes） |
| LibraryCore/Network/ClientPackets.cs | 492-512 | `C.MailOpened/MailGetItem/MailDelete/MailSend` |
| LibraryCore/Network/ServerPackets.cs | 641-648 | `S.Chat`（ObjectID/Text/Type/LinkedItems/OverheadOnly） |
| LibraryCore/Network/ServerPackets.cs | 916-935 | `S.MailList/MailNew/MailDelete/MailItemDelete/MailSend` |
| LibraryCore/Network/ServerPackets.cs | 25-43 | `S.Login`（登录随包下发 `BlockList` 与 `IsGM`） |
| ServerLibrary/Envir/Config.cs | 101-102 | `ShoutDelay=10s`、`GlobalDelay=60s`（仅 observer 全服用） |
| ServerLibrary/Envir/SEnvir.cs | 61-64 | `LogChat`：所有聊天写入 `ChatLogs` 队列（无敏感词过滤） |
| ServerLibrary/Envir/SEnvir.cs | 4040-4046 | `IsBlocking(account1, account2)`：双向屏蔽判定 |
| ServerLibrary/Envir/SConnection.cs | 219-251 | `ReceiveChat/ReceiveChatWithObservers`：统一封装 `S.Chat` 下发（含 Observers 观察连接） |
| ServerLibrary/Envir/SConnection.cs | 599-608 | `Process(C.Chat)`：长度校验后转 `Player.Chat` 或 `ObserverChat` |
| ServerLibrary/Envir/SConnection.cs | 1075-1102 | `Process(C.MailOpened/MailGetItem/MailDelete/MailSend)` |
| ServerLibrary/Envir/SConnection.cs | 1371-1411 | `Process(C.BlockAdd/BlockRemove)`：屏蔽名单增删 |
| ServerLibrary/Envir/SConnection.cs | 1532-1575 | `Process(C.FriendAdd/FriendRemove)`：好友增删 |
| ServerLibrary/Envir/Commands/Command/Admin/ChatBan.cs | 6-28 | GM 指令 `CHATBAN 名字 [分钟]`（默认 1440×365 分钟=365 天） |
| ServerLibrary/Envir/Commands/Command/Admin/GlobalShoutBan.cs | 24-26 | GM 指令 `GLOBALSHOUTBAN`：单独禁全服喇叭 |
| ServerLibrary/DBModels/AccountInfo.cs | 270-283 | `ChatBanExpiry`（账号级禁言到期时刻，DB 持久化） |
| ServerLibrary/DBModels/AccountInfo.cs | 416-421 | `GlobalShoutExpiry`（全服喇叭 CD，DB 持久化） |
| ServerLibrary/DBModels/AccountInfo.cs | 573-589 | `Mail`（DBBindingList\<MailInfo\>）、`BlockingList/BlockedByList`（BlockInfo 双向） |
| ServerLibrary/DBModels/CharacterInfo.cs | 669-673 | `Friends/FriendedBy`（FriendInfo 双向关联，挂在角色上） |
| ServerLibrary/DBModels/MailInfo.cs | 9-157 | 邮件模型：Account/Sender/Date/Subject/Message/Opened/HasItem/Items + ToClientInfo |
| ServerLibrary/DBModels/FriendInfo.cs | 6-74 | 好友模型：Character↔FriendedCharacter + FriendName；ToClientInfo 带 OnlineState |
| ServerLibrary/DBModels/BlockInfo.cs | 6-73 | 屏蔽模型：Account↔BlockedAccount + BlockedName |
| ServerLibrary/Models/PlayerObject.cs | 114 | `ShoutExpiry/MailTime` 等内存计时字段 |
| ServerLibrary/Models/PlayerObject.cs | 1596-1828 | `Chat(string, List<int>)`：前缀路由总入口 |
| ServerLibrary/Models/PlayerObject.cs | 1829-1957 | `ObserverChat(SConnection, string)`：观察者连接的平行路由（复用同套前缀） |
| ServerLibrary/Models/PlayerObject.cs | 3687-3921 | `#region Mail`：MailGetItem/MailDelete/MailSend |
| ServerLibrary/Models/PlayerObject.cs | 17315-17330 | `UpdateOnlineState`：向 `FriendedBy` 推 `S.FriendUpdate` |
| ServerLibrary/Models/PlayerObject.cs | 851-852 | StartGame 的 `ClientPlayerInfo` 载荷携带 `Friends` 列表与 OnlineState |
| ServerLibrary/Models/Monsters/Companion.cs | 366-369 | `OverheadOnly=true` 唯一使用处（伙伴头顶气泡，不进聊天栏） |
| Client/Scenes/Views/ChatTextBox.cs | 142-297 | 聊天输入框：Enter 发 `C.Chat`、快捷键预填前缀、LastPM、LinkItem |
| Client/Scenes/Views/ChatTextBox.cs | 344-353 | `ChatMode` 枚举（Local/Whisper/Group/Guild/Shout/Global/Observer，7 值循环） |
| Client/Scenes/Views/ChatTab.cs | 324-365 | 每个聊天页按 MessageType 复选框过滤 |
| Client/Scenes/Views/ChatTab.cs | 490-548 | 各 MessageType 的前景/背景色（Config.*TextColour） |
| Client/Scenes/Views/ChatOptionsDialog.cs | — | 聊天页选项（过滤器/透明度等） |
| Client/Scenes/Views/CommunicationDialog.cs | 347-463 | 好友/收件/发件/屏蔽四个 DXTab |
| Client/Scenes/Views/CommunicationDialog.cs | 697-762 | 收件页：打开即 `C.MailOpened`、CollectAll 逐件 `C.MailGetItem`+`C.MailDelete` |
| Client/Scenes/Views/CommunicationDialog.cs | 873-914/1282 | 发件页：GridType.SendMail 5 格 + 金币输入 + `C.MailSend` |
| Client/Scenes/GameScene.cs | 4076-4083 | `ReceiveChat`：分发到所有 ChatTab（含本地 Config.LogChat 落盘） |
| Client/UserModels/ChatTabControlSetting.cs、ChatTabPageSetting.cs | — | 聊天页布局持久化 |

## 核心流程

### 1. MessageType 全表（"ChatType"）

```csharp
public enum MessageType              // LibraryCore/Enum.cs:546-562
{
    Normal,        // 0  普通（当前视野内）
    Shout,         // 1  喊话（!，本地图）
    WhisperIn,     // 2  收到的私聊
    GMWhisperIn,   // 3  GM 发来的私聊（TempAdmin 时替换 WhisperIn）
    WhisperOut,    // 4  自己发出的私聊
    Group,         // 5  组队（!!）
    Global,        // 6  全服喇叭（!@）
    Hint,          // 7  提示（无对应发言入口，纯服务端下发）
    System,        // 8  系统
    Announcement,  // 9  公告（@! GM 全服公告 / 欢迎语）
    Combat,        // 10 战斗/获得信息（掉落、经验等）
    ObserverChat,  // 11 观察者（#，被观察者与观察者间）
    Guild,         // 12 行会（!~）
    Debug          // 13 调试
}
```

> 本仓库不存在名为 `ChatType` 的枚举。`PlayerObject.cs:9791/9797` 的 `ChatType.System` 位于 9785-9802 的 `/* ... */` 块注释（废弃的行会仓库代码）内，不参与编译语义。

### 2. 发送链：C.Chat → Process → PlayerObject.Chat

```csharp
public void Process(C.Chat p)        // SConnection.cs:599
{
    if (string.IsNullOrEmpty(p.Text) || p.Text.Length > Globals.MaxChatLength) return;  // ≤120 字

    if (Stage == GameStage.Game)
        Player.Chat(p.Text, p.LinkedItemIndexes);

    if (Stage == GameStage.Observer)
        Observed.Player.ObserverChat(this, p.Text);   // 观察者连接有平行入口
}
```

`PlayerObject.Chat`（PlayerObject.cs:1596-1828）先做**物品链接注入**（把文本里的 `[物品名]` 改写为 `[物品名:ItemIndex]` 并收集 `LinkedItems`，上限 `MaxChatItemLinks=10`，物品按 Inventory→Storage→Equipment→Companion.Inventory 顺序查找，:1605-1630），再按前缀路由：

```csharp
// —— 私聊 "/"（:1632-1668）
if (text.StartsWith("/"))
{
    if (SEnvir.Now < Character.Account.ChatBanExpiry) return;      // 禁言：私聊直接吞

    text = text.Remove(0, 1);
    parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) return;

    SConnection con = SEnvir.GetConnectionByCharacter(parts[0]);    // 第一个词=角色名

    if (con == null || (con.Stage != GameStage.Observer && con.Stage != GameStage.Game) || SEnvir.IsBlocking(Character.Account, con.Account))
    {
        Connection.ReceiveChatWithObservers(con => string.Format(con.Language.CannotFindPlayer, parts[0]), MessageType.System, linkedItems);
        return;
    }

    if (!Character.Account.TempAdmin)                               // GM 不受"拒听私聊"限制
    {
        if (BlockWhisper) { ...BlockingWhisper...; return; }        // 自己关了私聊
        if (con.Player != null && con.Player.BlockWhisper) { ...PlayerBlockingWhisper...; return; }
    }

    Connection.ReceiveChat($"/{text}", MessageType.WhisperOut, linkedItems);
    con.ReceiveChat($"{Name}=> {text.Remove(0, parts[0].Length)}",
        Character.Account.TempAdmin ? MessageType.GMWhisperIn : MessageType.WhisperIn, linkedItems);  // GM 私聊换类型
}
// —— 组队 "!!"（:1669-1683）：GroupMembers 广播，跳过屏蔽者；禁言者发言他人收不到（只显给自己）
else if (text.StartsWith("!!"))
{
    if (GroupMembers == null) return;
    text = $"{Name}: {text.Remove(0, 2)}";
    foreach (PlayerObject member in GroupMembers)
    {
        if (SEnvir.IsBlocking(Character.Account, member.Character.Account)) continue;
        if (member != this && SEnvir.Now < Character.Account.ChatBanExpiry) continue;
        member.Connection.ReceiveChat(text, MessageType.Group, linkedItems);
    }
}
// —— 行会 "!~"（:1684-1698）：Guild.Members 全员，跳过离线/屏蔽
else if (text.StartsWith("!~"))
{
    if (Character.Account.GuildMember == null) return;
    text = $"{Name}: {text.Remove(0, 2)}";
    foreach (GuildMemberInfo member in Character.Account.GuildMember.Guild.Members)
    {
        if (member.Account.Connection == null) continue;
        if (member.Account.Connection.Stage != GameStage.Game && member.Account.Connection.Stage != GameStage.Observer) continue;
        if (SEnvir.IsBlocking(Character.Account, member.Account)) continue;
        member.Account.Connection.ReceiveChat(text, MessageType.Guild, linkedItems);
    }
}
// —— 全服喇叭 "!@"（:1699-1732）
else if (text.StartsWith("!@"))
{
    if (!Character.Account.TempAdmin)
    {
        if (SEnvir.Now < Character.Account.GlobalShoutExpiry) { ...GlobalDelay(剩余秒)...; return; }
        if (Level < 33 && Stats[Stat.GlobalShout] == 0) { ...GlobalLevel...; return; }

        Character.Account.GlobalShoutExpiry = SEnvir.Now.AddSeconds(30);   // 30 秒 CD（DB 字段）
    }

    text = string.Format("(!@){0}: {1}", Name, text.Remove(0, 2));

    foreach (SConnection con in SEnvir.Connections)
        switch (con.Stage)
        {
            case GameStage.Game:
            case GameStage.Observer:
                if (SEnvir.IsBlocking(Character.Account, con.Account)) continue;
                con.ReceiveChat(text, MessageType.Global, linkedItems);
                break;
            default: continue;
        }
}
// —— 喊话 "!"（:1733-1767）：本地图
else if (text.StartsWith("!"))
{
    if (!Character.Account.TempAdmin)
    {
        if (SEnvir.Now < ShoutExpiry) { ...ShoutDelay(剩余秒)...; return; }   // ShoutExpiry：内存字段
        if (Level < 2) { ...ShoutLevel...; return; }
    }

    text = string.Format("(!){0}: {1}", Name, text.Remove(0, 1));
    ShoutExpiry = SEnvir.Now + Config.ShoutDelay;                            // Config.ShoutDelay = 10s

    foreach (PlayerObject player in CurrentMap.Players)                       // 全地图玩家
    {
        if (player != this && SEnvir.Now < Character.Account.ChatBanExpiry) continue;
        if (!SEnvir.IsBlocking(Character.Account, player.Character.Account))
            player.Connection.ReceiveChat(text, MessageType.Shout, linkedItems);
        foreach (SConnection observer in player.Connection.Observers)        // 连带观察者
        {
            if (SEnvir.IsBlocking(Character.Account, observer.Account)) continue;
            observer.ReceiveChat(text, MessageType.Shout, linkedItems);
        }
    }
}
// —— GM 公告 "@!"（:1768-1785）：TempAdmin 专属，全服 Announcement，无视屏蔽
else if (text.StartsWith("@!"))
{
    if (!Character.Account.TempAdmin) return;
    text = string.Format("{0}: {1}", Name, text.Remove(0, 2));
    foreach (SConnection con in SEnvir.Connections)
        switch (con.Stage)
        {
            case GameStage.Game:
            case GameStage.Observer:
                con.ReceiveChat(text, MessageType.Announcement, linkedItems);
                break;
            default: continue;
        }
}
// —— 命令 "@"（:1786-1794）：转 GM/普通指令处理器
else if (text.StartsWith("@"))
{
    text = text.Remove(0, 1);
    parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) return;
    SEnvir.CommandHandler.Handle(this, parts);
}
// —— 观察者频道 "#"（:1795-1806）：被观察者+其观察者互通
else if (text.StartsWith("#"))
{
    text = string.Format("(#){0}: {1}", Name, text.Remove(0, 1));
    Connection.ReceiveChat(text, MessageType.ObserverChat, linkedItems);
    foreach (SConnection target in Connection.Observers)
    {
        if (SEnvir.IsBlocking(Character.Account, target.Account)) continue;
        target.ReceiveChat(text, MessageType.ObserverChat, linkedItems);
    }
}
// —— 普通发言（:1808-1827）：视野内 SeenByPlayers + MaxViewRange
else
{
    text = string.Format("{0}: {1}", Name, text);
    foreach (PlayerObject player in SeenByPlayers)
    {
        if (!Functions.InRange(CurrentLocation, player.CurrentLocation, Config.MaxViewRange)) continue;
        if (player != this && SEnvir.Now < Character.Account.ChatBanExpiry) continue;
        if (!SEnvir.IsBlocking(Character.Account, player.Character.Account))
            player.Connection.ReceiveChat(text, MessageType.Normal, linkedItems, ObjectID);   // ObjectID→头顶气泡
        foreach (SConnection observer in player.Connection.Observers)
        {
            if (SEnvir.IsBlocking(Character.Account, observer.Account)) continue;
            observer.ReceiveChat(text, MessageType.Normal, linkedItems, ObjectID);
        }
    }
}
```

`ObserverChat`（PlayerObject.cs:1829-1957）是被观察者连接上"观察者发言"的平行实现，前缀语义相同（`/`私聊 :1846-1871、`!~`行会 :1873-1887、`!@`全服 :1889-1921、`#`观察 :1947-1955），差异点：以 `con.Account.LastCharacter` 身份发言、全服 CD 用 `GlobalShoutExpiry = SEnvir.Now.AddSeconds(30)`（:1905）、私聊目标收 `GMWhisperIn/WhisperIn`（:1871）。

### 3. 下发链：ReceiveChat → S.Chat

```csharp
public void ReceiveChat(string text, MessageType type, List<ClientUserItem> linkedItems = null, uint objectID = 0)   // SConnection.cs:219
{
    switch (Stage)
    {
        case GameStage.Game:
        case GameStage.Observer:
            Enqueue(new S.Chat
            {
                Text = text,
                Type = type,
                ObjectID = objectID, // && type != guild
                LinkedItems = linkedItems,
                ObserverPacket = false,
            });
            break;
        default:
            return;
    }
}
```

`ReceiveChatWithObservers`（:240-246）额外把同一条消息按**每个观察连接自己的语言**（`Func<SConnection,string>`）重算后下发——所有双语提示（含全部 Marry*/Mail*/Guild* 文案）都走它。

`S.Chat.OverheadOnly` 在玩家聊天链路恒为 false，唯一 `true` 来源是伙伴（Companion）头顶说话（ServerLibrary/Models/Monsters/Companion.cs:366-369）——只冒气泡、不进聊天栏。

### 4. 禁言（ChatBan）与 GM 标记

- 字段：`AccountInfo.ChatBanExpiry`（AccountInfo.cs:270-283，账号级、DB 持久化、全角色共享）。
- GM 指令：`CHATBAN 角色名 [分钟]`（ChatBan.cs:12-26），不传分钟默认 `1440 * 365`（一年）；另有 `GLOBALSHOUTBAN`（GlobalShoutBan.cs:24-26）单独封全服喇叭。
- 效果（PlayerObject.Chat 内）：
  - 私聊 `/`：**整条吞掉**（:1634，发送者本人也看不到"已发送"回显）。
  - 组队 `!!`/喊话 `!`/普通：**他人收不到、自己能看到**（`member != this && banned → continue` 模式，:1679/:1755/:1815）。
  - 行会/全服/`@!`/`@`/`#`：**不受 ChatBan 限制**。
- GM 标记：`Character.Account.TempAdmin`（用 MasterPassword 或 Admin 登录后置位）豁免喊话/全服 CD 与等级、私聊绕过 `BlockWhisper`、收私聊时类型改为 `GMWhisperIn`（:1667）；`@!` 公告仅 TempAdmin 可发（:1770）。
- 敏感词/文本过滤：**未找到实现**。仅有 `SEnvir.LogChat($"{Name}: {text}")`（PlayerObject.cs:1599；实现 SEnvir.cs:61-64，格式 `[时间]: 内容` 入 `ChatLogs` 队列）。

### 5. 邮件：模型与登录下发

```csharp
[UserObject]
public sealed class MailInfo : DBObject          // ServerLibrary/DBModels/MailInfo.cs:9
{
    [Association("Mail")]
    public AccountInfo Account { ... }           // 收件账号（AccountInfo.Mail 反向列表 :573-574）
    public string Sender { ... }                 // 发件人名（玩家名或 "Market Place"/"System"）
    public DateTime Date { ... }                 // OnCreated 时写 SEnvir.Now（:130-135）
    public string Subject { ... }
    public string Message { ... }
    public bool Opened { ... }                   // 已读标记（C.MailOpened 置位）
    public bool HasItem { ... }                  // 有附件（发信完成时按 Items.Count 回写）
    [Association("Mail")]
    public DBBindingList<UserItem> Items { get; set; }   // 附件（含金币 UserItem）

    public ClientMailInfo ToClientInfo()         // :137-150
    {
        return new ClientMailInfo
        {
            Index = Index, Subject = Subject, Sender = Sender, Date = Date,
            Message = Message, HasItem = HasItem, Opened = Opened,
            Items = Items.Select(x => x.ToClientInfo()).ToList()
        };
    }
}
```

登录（StartGame）全量下发：`Enqueue(new S.MailList { Mail = Character.Account.Mail.Select(x => x.ToClientInfo()).ToList() })`（PlayerObject.cs:1114）。注意邮件挂在**账号**上——任意角色登录都看到同一信箱。

### 6. 发信：MailSend 校验与落库

```csharp
public void MailSend(C.MailSend p)               // PlayerObject.cs:3745
{
    Enqueue(new S.MailSend { ObserverPacket = false });     // ① 请求回执（只表示"已入队"，不是成功！）

    if (MailTime > SEnvir.Now) return;                       // ② 10 秒节流
    MailTime = SEnvir.Now.AddSeconds(10);

    S.ItemsChanged result = new S.ItemsChanged { Links = p.Links };
    Enqueue(result);                                         // ③ 先回 ItemsChanged（Success 待定）

    if (!ParseLinks(p.Links, 0, 5)) return;                  // ④ 附件最多 5 格

    if (p.Recipient == null || p.Recipient.Length > Globals.MaxCharacterNameLength) return;

    AccountInfo account = SEnvir.GetCharacter(p.Recipient)?.Account;
    if (account == null || SEnvir.IsBlocking(Character.Account, account)) { ...MailNotFound...; return; }   // ⑤ 被对方屏蔽=查无此人
    if (account == Character.Account && !Character.Account.TempAdmin) { ...MailSelfMail...; return; }       // ⑥ 不能寄给自己（GM 除外）
    if (p.Links.Count > 0 && account.Mail.Sum(x => x.Items.Count) >= Globals.MaxMailStorage) { ...MailStorageFull...; return; }  // ⑦ 对方附件格 ≥50
    if (p.Gold < 0 || p.Gold > Gold.Amount) { ...MailMailCost...; return; }                                 // ⑧ 金币校验
    if (p.Subject == null || p.Subject.Length > 30) return;                                                 // ⑨ 主题≤30
    if (p.Message == null || p.Message.Length > 300) return;                                                //    正文≤300

    UserItem item;
    foreach (CellLinkInfo link in p.Links)                   // ⑩ 逐格校验来源与合法性
    {
        UserItem[] fromArray;
        switch (link.GridType)
        {
            case GridType.Inventory:
                if (!InSafeZone && !Character.Account.TempAdmin) { ...MailSendSafeZone...; return; }   // 背包来源需安全区
                fromArray = Inventory; break;
            case GridType.PartsStorage: fromArray = PartsStorage; break;
            case GridType.Storage:      fromArray = Storage; break;
            case GridType.CompanionInventory:
                if (Companion == null) return;
                if (!InSafeZone && !Character.Account.TempAdmin) { ...MailSendSafeZone...; return; }
                fromArray = Companion.Inventory; break;
            default: return;
        }

        if (link.Slot < 0 || link.Slot >= fromArray.Length) return;
        item = fromArray[link.Slot];
        if (item == null || link.Count > item.Count) return;
        if (((item.Flags & UserItemFlags.Bound) == UserItemFlags.Bound || !item.Info.CanTrade) && !account.IsAdmin(true) && !Character.Account.IsAdmin(true)) return;  // 绑定/禁交易（除非收发任一方是 GM）
        if ((item.Flags & UserItemFlags.Marriage) == UserItemFlags.Marriage) return;   // 婚戒不可邮寄
    }

    MailInfo mail = SEnvir.MailInfoList.CreateNewObject();   // ⑪ 落库（Date 自动置 Now）

    mail.Account = account;
    mail.Sender = Name;
    mail.Subject = p.Subject;
    mail.Message = p.Message;

    result.Success = true;                                   // ⑫ 现在才把 ItemsChanged 标记成功

    if (p.Gold > 0)                                          // ⑬ 金币→专门铸造一个 Gold UserItem 附件
    {
        Gold.Amount -= p.Gold;
        GoldChanged();
        item = SEnvir.CreateFreshItem(SEnvir.GoldInfo);
        item.Count = p.Gold;
        item.Slot = mail.Items.Count;
        item.Mail = mail;
    }

    foreach (CellLinkInfo link in p.Links)                   // ⑭ 物品真移除（整格拿走或拆堆）
    {
        ...
        item = fromArray[link.Slot];
        if (link.Count == item.Count) { RemoveItem(item); fromArray[link.Slot] = null; }
        else { item.Count -= link.Count; item = SEnvir.CreateFreshItem(item); item.Count = link.Count; }
        item.Slot = mail.Items.Count;
        item.Mail = mail;                                    // UserItem 归属切到邮件
    }

    if (p.Links.Count > 0) { Companion?.RefreshWeight(); RefreshWeight(); }

    mail.HasItem = mail.Items.Count > 0;                     // ⑮ 回写 HasItem

    if (account.Connection?.Player != null)                  // ⑯ 收件人在线 → 实时推 S.MailNew
        account.Connection.Enqueue(new S.MailNew { Mail = mail.ToClientInfo(), ObserverPacket = false });

    LogMilestone(MilestoneType.MailSend, 1);
}
```

**付费邮件（COD）未找到实现**：`C.MailSend.Gold` 是寄件人**随信附带**的金币（收件人白拿），不存在"收件人付款后才取件"的流程；全仓库搜 `COD|CashOnDelivery|付费邮件` 无结果。若需要 COD，须自行扩展（建议在 `MailInfo` 加 `long CODAmount`，`MailGetItem` 里做扣款交换）。

### 7. 读信/取附件/删信

- `Process(C.MailOpened)`（SConnection.cs:1075-1084）：按 Index 找到邮件置 `mail.Opened = true`（仅 DB 标记，无回包）。
- `MailGetItem`（PlayerObject.cs:3689-3723）：需安全区（GM 豁免）+ 背包可容纳（`CanGainItems`）；成功后 `item.Mail = null; GainItem(item)` 并回 `S.MailItemDelete{Index,Slot}`；邮件/物品不存在也回 `S.MailItemDelete` 让客户端清格。
- `MailDelete`（:3724-3744）：**有附件的邮件禁止删除**（`mail.Items.Count > 0 → MailHasItems 提示`）；删除即 `mail.Delete()`（OnDisconnected 把 Account 置空，MailInfo.cs:123-128），回 `S.MailDelete`。

### 8. 系统邮件（服务端自动生成，Sender 固定）

| 场景 | 位置 | Sender/Subject |
|---|---|---|
| 拍卖成交→卖家收款（含税单明细+金币附件） | PlayerObject.cs:4240-4260 | "Market Place"（含 Buyer/Item/Price/Tax/Total 明细，:4240-4246） |
| 拍卖成交→买家不在安全区/包满→物品转邮件 | PlayerObject.cs:4267-4283 | Subject="Item Purchase" |
| 卖家撤销挂牌但无法回收物品 | PlayerObject.cs:4103-4117 | Subject="Listing Cancelled" |
| GM 批量撤销挂牌（道具变更）退款退物 | PlayerObject.cs:4604-4618 | Sender="System"，Subject="Listing Cancelled" |
| 商城赠送（GameStoreGift） | PlayerObject.cs:4536-4540 | 物品直接进收件人邮件 |
| 玩家发信 | PlayerObject.cs:3846-3918 | Sender=玩家名 |

### 9. 好友与屏蔽

**好友**（角色维度，`CharacterInfo.Friends/FriendedBy`，CharacterInfo.cs:669-673）：

```csharp
public void Process(C.FriendAdd p)             // SConnection.cs:1532
{
    if (Stage != GameStage.Game) return;
    CharacterInfo info = SEnvir.GetCharacter(p.Name);
    if (info == null) { ...CannotFindPlayer...; return; }
    foreach (FriendInfo friendInfo in Player.Character.Friends)
        if (friendInfo.FriendedCharacter == info) { ...AlreadyFriended...; return; }

    FriendInfo friend = SEnvir.FriendInfoList.CreateNewObject();
    friend.Character = Player.Character;        // 单向添加，无需对方确认
    friend.FriendedCharacter = info;
    friend.FriendName = info.CharacterName;

    Player.LogMilestone(MilestoneType.FriendAdd, Player.Character.Friends.Count, true);
    Enqueue(new S.FriendAdd { Info = friend.ToClientInfo(), ObserverPacket = false });
}
```

- 好友是**单向**的（对方列表里不会有你，除非他也加你）；`FriendedBy` 只是反向查询表。
- `ToClientInfo`（FriendInfo.cs:64-72）：`State = FriendedCharacter.Player == null ? OnlineState.Offline : FriendedCharacter.OnlineState`——在线状态实时计算。
- 登录随 `S.StartGame` 的玩家信息载荷下发（PlayerObject.cs:851-852）；上下线时 `UpdateOnlineState`（:17315-17330）遍历 `FriendedBy` 发 `S.FriendUpdate`，且 `sendMessage=true` 时给在线好友补一条系统聊天 `FriendStateChanged`。
- `Process(C.OnlineState)`（SConnection.cs:1526-1529）允许玩家切换自己的在线状态显示（在线/隐身等 `OnlineState` 枚举）。

**屏蔽**（账号维度，`AccountInfo.BlockingList/BlockedByList`，AccountInfo.cs:585-589）：

```csharp
public void Process(C.BlockAdd p)              // SConnection.cs:1371
{
    if (Stage != GameStage.Game && Stage != GameStage.Observer) return;
    CharacterInfo info = SEnvir.GetCharacter(p.Name);
    if (info == null) { ...CannotFindPlayer...; return; }
    foreach (BlockInfo blockInfo in Account.BlockingList)
        if (blockInfo.BlockedAccount == info.Account) { ...AlreadyBlocked...; return; }

    BlockInfo block = SEnvir.BlockInfoList.CreateNewObject();
    block.Account = Account;
    block.BlockedAccount = info.Account;
    block.BlockedName = info.CharacterName;
    Enqueue(new S.BlockAdd { Info = block.ToClientInfo(), ObserverPacket = false });
}
```

- 屏蔽名单**登录时随 `S.Login.BlockList` 下发**（ServerPackets.cs:35；服务端填充见登录流程）。
- 拦截判定 `SEnvir.IsBlocking`（SEnvir.cs:4040-4046）**双向生效**（任一方拉黑即拦截），覆盖：私聊（:1644）、组队（:1677）、行会（:1694）、全服（:1725）、喊话（:1757/1762）、普通（:1817/1822）、观察者频道（:1803）、邮件（:3766）。
- 注意：`!@` GM 公告（:1774-1784）与 `@` 命令**不受屏蔽影响**。

### 10. Client（WinForms 原版）聊天/邮件 UI 关键行为

- **ChatTextBox**（Client/Scenes/Views/ChatTextBox.cs）：Enter 发 `C.Chat{Text, LinkedItemIndexes}`（:150-154）；`/` 开头记录 `LastPM`，按 `/` 键自动补上次私聊对象（:156-161/:209-218）；`@`/`!` 键预填命令/喊话前缀（:187-203，受 `Config.ShiftOpenChat` 控制）；ChatMode 按钮 7 值循环（Local/Whisper/Group/Guild/Shout/Global/Observer，:99/:344-353），切模式自动填前缀 `!`/`LastPM`/`!!`/`!~`/`!@`/`#`（:251-273）；`LinkItem` 在文本尾插 `[物品名]` 并登记 Index（:288-297）。
- **ChatTab**（Client/Scenes/Views/ChatTab.cs）：每个聊天页一组频道复选框，`ReceiveChat` 按 `MessageType` 逐项过滤（:324-365，Announcement/Debug 不过滤）；每种 MessageType 有独立前/背景色（:490-548）。
- **CommunicationDialog**（Client/Scenes/Views/CommunicationDialog.cs）：四 Tab——FriendTab（好友列表+在线状态过滤下拉 :347-373/:480-524）、ReceivedTab（收件列表，打开即发 `C.MailOpened` :697、CollectAll 逐件 `C.MailGetItem`+`C.MailDelete` :723-762）、SendTab（收件人/主题/正文≤300/5 格 `GridType.SendMail`/金币输入 :785-914，发送 :1282 `C.MailSend{Links,Recipient,Subject,Message,Gold}`）、BlockTab（屏蔽名单 :436-463）；ReadTab 读信视图含 7 格附件（点格取件 :1117-1119）与删除（:1164-1167）。
- **GameScene.ReceiveChat**（Client/Scenes/GameScene.cs:4076-4083）：本地 `Config.LogChat` 落盘后分发到所有 `ChatTab.Tabs`。
- 布局持久化：Client/UserModels/ChatTabControlSetting.cs / ChatTabPageSetting.cs。

## 数据结构/协议细节

### 聊天包

```csharp
public sealed class Chat : Packet              // LibraryCore/Network/ClientPackets.cs:237
{
    public string Text { get; set; }
    public List<int> LinkedItemIndexes { get; set; }   // 背包物品 Index 列表（≤10）
}

public sealed class Chat : Packet              // LibraryCore/Network/ServerPackets.cs:641
{
    public uint ObjectID { get; set; }                 // 说话者（普通/喊话频道做头顶气泡）
    public string Text { get; set; }
    public MessageType Type { get; set; }
    public List<ClientUserItem> LinkedItems { get; set; }  // 服务端解析好的可点击物品
    public bool OverheadOnly { get; set; }             // 仅伙伴气泡用
}
```

### 邮件包

```csharp
public sealed class MailOpened : Packet { public int Index; }                    // ClientPackets.cs:492
public sealed class MailGetItem : Packet { public int Index; public int Slot; }  // :496
public sealed class MailDelete  : Packet { public int Index; }                   // :501
public sealed class MailSend    : Packet                                          // :505
{
    public List<CellLinkInfo> Links;   // 附件（≤5）
    public string Recipient; public string Subject; public string Message;
    public long Gold;                  // 寄件人随信金币（非 COD）
}

public sealed class MailList      : Packet { public List<ClientMailInfo> Mail; }   // ServerPackets.cs:916
public sealed class MailNew       : Packet { public ClientMailInfo Mail; }         // :920
public sealed class MailDelete    : Packet { public int Index; }                   // :924
public sealed class MailItemDelete: Packet { public int Index; public int Slot; }  // :928
public sealed class MailSend      : Packet { }                                     // :933 仅请求回执
```

### 关键时序（发信）

```
客户端                          服务端
  └─ C.MailSend ──────────────────► MailSend()
  ◄──────────── S.MailSend ──────── ① 请求已入队（≠成功）
  ◄──────────── S.ItemsChanged ──── ③ Success=false 先发；校验全过才置 true
       （附件格保持锁定，直到 ItemsChanged.Success 明确）
  收件人在线 ◄─── S.MailNew ──────── ⑯ 实时新邮件
  收件人离线：无推送，下次登录 S.MailList 全量
```

存储时机：`MailInfo`/附件 `UserItem` 在校验通过后**立即** `SEnvir.MailInfoList.CreateNewObject()` 落 Users.db（PlayerObject.cs:3846）；金币先扣（:3857）再铸附件（:3860-3863）。

## GodotClient 现状

| 功能 | 状态 | GodotClient 证据 |
|---|---|---|
| 聊天发送（C.Chat + 物品链接） | 已移植 | GodotClient/Scripts/GameScene.cs:292-297（`SendChat`→`new C.Chat{Text, LinkedItemIndexes}`） |
| 聊天接收（S.Chat → 聊天栏） | 已移植 | GameScene.cs:315-316 `ReceiveChat`→`_chatLog.AddMessage` |
| 聊天输入框（ChatMode 循环/前缀自动填充/LastPM） | 已移植 | GodotClient/Controls/ChatTextBox.cs:14-17（ChatMode 枚举）、:107-112（`!`/LastPM/`!!`/`!~`/`!@`/`#` 前缀映射，与原版 ChatTextBox.cs:251-273 一致） |
| 聊天日志面板 + 频道过滤 | 已移植 | GodotClient/Controls/ChatLogPanel.cs（`IsTypeEnabled/SetTypeEnabled`，GameScene.cs:357-360）；Controls/ChatOptionsDialog.cs:221-234（过滤按钮开/关） |
| 多聊天页（Tab） | 已移植 | GameScene.cs:359-360 `AddChatTab/ResetTabs` |
| 私聊发起（StartPM） | 已移植 | GameScene.cs:318 `StartPrivateMessage`→`_chatTextBox.StartPM` |
| GM 公告/命令/观察者频道前缀 | 已移植 | 前缀路由在服务端；客户端仅需透传文本与显示（ChatMode.Observer 含 `#`，ChatTextBox.cs:112） |
| 邮件列表/新邮件/删件/取附件回包 | 已移植 | GameScene.cs:2764-2786（OnMailList/OnMailNew/OnMailDelete/OnMailItemDelete → CommunicationDialog + 主面板未读角标 `SetMailIndicator`）；事件接线 ServerConnection.cs:585-588 |
| 发信（含附件格、金币、失败保留表单） | 已移植 | GameScene.cs:6484-6495 `SendMail`（C.MailSend）；Controls/CommunicationDialog.cs:381-388 `MailSendResult`（S.MailSend 只算请求阶段，失败保留输入）、:390+ `PrepareMailSend`（pending 链接锁定）、:703-705 SendMail 格 |
| 读信/取附件/删信请求 | 已移植 | GameScene.cs:6472-6483（SendMailOpened/SendMailGetItem/SendMailDelete）；CommunicationDialog.cs:148-151（CollectAll）、:602-609（打开发 MailOpened）、:642-643（点格取件）、:674-677（删信前检查附件） |
| 邮件快捷键 | 已移植 | GodotClient/KeyBindManager.cs:39-40/132-133（MailBoxWindow=`,`、MailSendWindow=`.`） |
| 好友增删/在线状态 | 已移植 | GameScene.cs:409-412（SendFriendAdd/SendFriendRemove）；:2804-2806（OnFriendUpdate/OnFriendAdd/OnFriendRemove→CommunicationDialog）；ServerConnection.cs:165-167/589-591 |
| 屏蔽增删/登录名单 | 已移植 | GameScene.cs:413-414（SendBlockAdd/SendBlockRemove）；:2807-2809（OnBlockList/OnBlockAdded/OnBlockRemoved）；ServerConnection.cs:385-391（S.Login 内 BlockList）、:397-398 |
| 通信窗快捷键 | 已移植 | KeyBindManager.cs:41/134（BlockListWindow=Ctrl+B） |
| 新邮件系统提示文案 | 已移植 | GodotClient/Translations/ChineseMessages.cs:88 `MailNew="你收到了来自 {0} 的新邮件。"`（含英/日三语） |
| 喊话/全服 CD 与禁言的服务端限制 | 无需客户端 | 纯服务端校验（PlayerObject.cs:1699-1751），客户端只需展示 System 回显 |

## 移植注意事项

1. **`S.MailSend` 不等于发送成功**——它只是"请求入队"回执，真正的成败由随后的 `S.ItemsChanged.Success` 表达（PlayerObject.cs:3747/3853）。Godot 端已按此实现（GameScene.cs:2306-2311 注释明确），新客户端若在 `S.MailSend` 时清空表单，失败后用户输入会丢失。
2. 发信校验失败大多是**静默 return**（收件人超长、主题 >30、正文 >300、附件非法等只回 `ItemsChanged.Success=false`，无文案，PlayerObject.cs:3759-3844）——客户端必须做同等长度预校验，否则玩家会以为"点了没反应"。
3. 邮件是**账号级**信箱：切换角色看到的邮件相同；好友是**角色级**（`Friends` 挂 CharacterInfo）；屏蔽是**账号级**。三者维度不同，别混建模型。
4. 物品链接的文本协议：客户端只发 `LinkedItemIndexes`（物品 Index），**服务端**负责把 `[物品名]` 改写成 `[物品名:Index]` 并回传 `LinkedItems`（PlayerObject.cs:1605-1630）。客户端渲染点击需要 `Index` 对应回 `LinkedItems` 条目。
5. 禁言的三个行为差异（私聊全吞 vs 群频道只显给自己 vs 行会/全服不受限）是服务端实现细节，但直接决定客户端"为什么我说话别人看不见"的客服问题；移植服务端时保持一致。
6. `MessageType` 与 WinForms `ChatMode`（客户端输入侧 7 值）是两套东西：ChatMode 只决定**输入框自动填什么前缀**，真正路由在前缀。
7. `S.Chat.ObjectID` 仅在普通/喊话频道携带说话者（头顶气泡用）；Godot 端 `ReceiveChat` 目前统一走聊天栏（GameScene.cs:315-316），若要做头顶气泡需补 ObjectID 分支（原版参考 Client 的 MapControl 气泡渲染）。
8. 好友在线状态不是推送缓存：`ToClientInfo` 每次现算（FriendInfo.cs:70），`S.FriendUpdate` 只在有人 `UpdateOnlineState` 时发（上下线/手动切换 OnlineState）。登录时的全量快照在 StartGame 载荷里（PlayerObject.cs:851-852），Godot 端从 `StartInfo.Friends` 初始化。
9. 若要补 COD（付费邮件）：`C.MailSend` 已有 `Gold` 字段可反向复用思路，但需在 `MailInfo`（MailInfo.cs:9-157）加金额字段、在 `MailGetItem`（PlayerObject.cs:3689-3723）加"扣款→放行"原子段，并在 `ClientMailInfo`（LibraryCore）同步扩展，否则客户端无法显示应付金额。
