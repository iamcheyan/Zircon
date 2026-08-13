# 服务端配置与命令系统（Server.ini / Config / GM 与玩家命令）

## TL;DR 速查表

- 服务端配置**不是** json/App.config，而是静态类 `Server.Envir.Config` + `[ConfigPath("./Server.ini")]` + `[ConfigSection(节)]`，由 `LibraryCore/ConfigReader` 反射读写（Server/Program.cs:22,38）；**退出时以 UTF-16 整体重写 ini**。
- 配置共 10 节 90+ 项：Network/System/Control/Mail/WebServer/Players/Monsters/Items/Rates/Fishing；全服倍率在 `[Rates]`（ExperienceRate/DropRate/GoldRate/SkillRate/CompanionRate，默认全 0），**System.db 中没有 Settings 类**，地图级倍率在 `MapInfo` 上。
- 运行时改配置的入口是管理端 `Server/Views/ConfigView`（LoadSettings/SaveSettings 直接读写 `Config.*` 静态属性，关窗时保存）。
- 命令入口：聊天框 `@命令 参数`（PlayerObject.cs:1786-1794）→ `SEnvir.CommandHandler`（SEnvir.cs:229-232，ErrorHandlingCommandHandler 装饰 Player+Admin 两个 handler）。
- 命令注册零配置：`AbstractCommandHandler` 构造函数反射扫描程序集里 `AbstractCommand<T>`/`AbstractParameterizedCommand<T>` 子类（AbstractCommandHandler.cs:17-25），按 `VALUE`（全大写）**大小写不敏感精确匹配，没有别名机制**。
- GM 权限 = `AccountInfo.Admin || AccountInfo.TempAdmin`（AdminCommandHandler.cs:9-12）；`Admin` 落库、`TempAdmin` 是运行时公共字段不落库（AccountInfo.cs:447,553）。
- Admin 命令 34 条、玩家命令 11 条 + 宠物档位 7 条（`ENABLELEVEL3/5/7/10/11/13/15`）。
- 仓内 AGENTS.md 的命令表有 4 处与源码不符：`@spawn`→实为 `@MONSTER`、`@toggleGM`→`@GAMEMASTER`、`@toggleSuperman`→`@SUPERMAN`、`@toggleObserver`→`@OBSERVER`（大小写无所谓）；GodotClient 的测试脚本用的是正确的 `@monster`（GameScene.cs:9215）。
- `@REBOOT` **不是重启服务器**，是取消自己在拍卖行的超级寄售（Reboot.cs:9-11）。
- Godot 客户端无任何服务端配置/命令逻辑需移植：命令=聊天文本 `@` 开头原样发给服务端（GameScene.cs:1964 的 `SendChat("@AllowTrade")` 即玩家命令入口）。

## 职责概述

1. **配置装载**：`ConfigReader` 是一个小型 ini 反射读写器。启动时把 `./Server.ini`（相对 exe 目录）读进 `Config` 静态属性；文件不存在则用 C# 默认值；进程退出时把当前值整体写回。
2. **配置消费**：`SEnvir`/`SConnection`/各系统直接读 `Config.XXX` 静态属性（无快照/无热更新广播——改了就是改了，各处下次读取生效）。
3. **运行时编辑**：管理端 ConfigView 提供图形界面，关窗写回静态属性 + `ConfigReader.Save`。
4. **命令系统**：聊天 `@` 前缀进入命令分发；两个 handler（玩家=恒允许，管理=Admin/TempAdmin）按注册顺序取第一个命中；错误经 `UserCommandException` 回显给执行者及其观战连接。

## 关键类/文件清单

| 路径 | 行号 | 职责 |
|---|---|---|
| ServerLibrary/Envir/Config.cs | 8-165 | 全部服务端配置项（10 节）+ LoadVersion（:167-186） |
| LibraryCore/ConfigReader.cs | 12-15, 22-39, 84-131, 135-192 | ini 解析（Header/Entry 正则）、反射读写、Unicode 写回；ConfigPath/ConfigSection/ConfigPropertyIgnore 特性（:678-708） |
| Server/Program.cs | 18-39 | 入口：ConfigReader.Load → Config.LoadVersion → WinForms → ConfigReader.Save |
| Server/Views/ConfigView.cs | 63-186, 187-315 | 运行时配置编辑（LoadSettings/SaveSettings）；本地 System.db 分发（:37-48） |
| ServerLibrary/Envir/SEnvir.cs | 229-232 | `CommandHandler` 组合根 |
| ServerLibrary/Envir/Commands/Handler/AbstractCommandHandler.cs | 11-55 | 反射注册 + `Handle` 分发 + 默认 `IsAllowedByPlayer=false` |
| ServerLibrary/Envir/Commands/AdminCommandHandler.cs | 7-14 | GM 权限判定（Admin‖TempAdmin） |
| ServerLibrary/Envir/Commands/PlayerCommandHandler.cs | 9-12 | 玩家命令恒允许 |
| ServerLibrary/Envir/Commands/ErrorHandlingCommandHandler.cs | 20-53 | 多 handler 选择 + UserCommandException/Fatal 兜底 |
| ServerLibrary/Envir/Commands/Command/AbstractCommand.cs | 8-9 | 无参命令基类（VALUE + Action(player)） |
| ServerLibrary/Envir/Commands/Command/AbstractParameterizedCommand.cs | 9-16 | 带参命令基类（VALUE + PARAMS_LENGTH + Action(player, vals) + ThrowNewInvalidParametersException） |
| ServerLibrary/Envir/Commands/Command/Admin/ | 35 文件 | 34 条 GM 命令 + IAdminCommand 标记接口 |
| ServerLibrary/Envir/Commands/Command/Player/ | 11 文件 + Companion/ 8 文件 | 11 条玩家命令 + IPlayerCommand + 7 条宠物档位命令 |
| ServerLibrary/Models/PlayerObject.cs | 1786-1794 | 聊天 `@` 前缀 → CommandHandler.Handle |

## 核心流程

### 1. 配置加载链

```csharp
// Server/Program.cs:19-38
static void Main()
{
    var assembly = Assembly.GetAssembly(typeof(Config));
    ConfigReader.Load(assembly);      // 读 ./Server.ini

    Config.LoadVersion();             // 对 VersionPath 算 SHA-256 → ClientHash
    ...
    Application.Run(new SMain());
    ConfigReader.Save(typeof(Config).Assembly);   // 退出时整体写回
}
```

`ConfigReader.Load` 找到带 `[ConfigPath]` 的类型（即 `Config`，Config.cs:8 `[ConfigPath("./Server.ini")]`），`AdjustPath` 把相对路径拼到 exe 目录；`ReadConfig`（ConfigReader.cs:84-131）用 `HeaderRegex ^\[(?<Header>.+)\]$` 与 `EntryRegex ^(?<Key>.*?)=(?<Value>.*)$` 逐行解析，属性按「最近一个 `[ConfigSection]`」归属到节，再反射写回（支持 Boolean~Color 等 11 种类型的 Read 重载）。**文件不存在→静默用默认值**；`ConfigReader.Save`→`SaveConfig`（:135-180）以 `Encoding.Unicode` 重写全部键值。

`Config.LoadVersion`（Config.cs:167-186）：对 `VersionPath`（默认 `./Zircon.dll`）算 SHA-256 存 `ClientHash`，登录时用于客户端版本校验（`CheckVersion`）。

### 2. 命令分发链

```csharp
// ServerLibrary/Models/PlayerObject.cs:1786-1794（Chat 方法内）
else if (text.StartsWith("@"))
{
    text = text.Remove(0, 1);
    parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    if (parts.Length == 0) return;

    SEnvir.CommandHandler.Handle(this, parts);
}
```

```csharp
// ServerLibrary/Envir/SEnvir.cs:229-232
public static ICommandHandler CommandHandler = new ErrorHandlingCommandHandler(
    new PlayerCommandHandler(),
    new AdminCommandHandler()
);
```

```csharp
// ServerLibrary/Envir/Commands/Handler/AbstractCommandHandler.cs:17-25（反射注册）
public AbstractCommandHandler()
{
    this.Commands = Assembly.GetAssembly(typeof(CommandType)).GetTypes()
        .Where(type => type.IsClass)
        .Where(type => !type.IsAbstract)
        .Where(type => type.IsSubclassOf(typeof(AbstractCommand<CommandType>)) || type.IsSubclassOf(typeof(AbstractParameterizedCommand<CommandType>)))
        .Select(type => (ICommand)Activator.CreateInstance(type))
        .ToList();
}
```

关键：`CommandType` 是封闭泛型标记（`IAdminCommand`/`IPlayerCommand`），`IsSubclassOf` 对 `AbstractCommand<IAdminCommand>` 是精确类型匹配，因此 Admin/Player 两套命令**互不串仓**；新增命令只需建类，不需要注册表。

```csharp
// ServerLibrary/Envir/Commands/Handler/AbstractCommandHandler.cs:32-53（匹配与执行）
public virtual bool CommandExists(string command)
{
    return Commands.Exists(userCommand => userCommand.VALUE.Equals(command.ToUpper()));
}
public virtual void Handle(PlayerObject player, string[] input)
{
    if (IsAllowedByPlayer(player))
    {
        string CommandInput = input[0].ToUpper();
        ICommand command = Commands.Find(userCommand => userCommand.VALUE.Equals(CommandInput));
        if (command == null)
            throw new UserCommandException(string.Format("Command @{0} does not exist.", CommandInput));
        if (command is AbstractParameterizedCommand<CommandType>)
            (command as AbstractParameterizedCommand<CommandType>).Action(player, input);
        else if (command is AbstractCommand<CommandType>)
            (command as AbstractCommand<CommandType>).Action(player);
    }
}
```

`VALUE.Equals(command.ToUpper())`（Ordinal）——**大小写不敏感但无别名**；`@move`/`@MOVE`/`@Move` 等价。`ErrorHandlingCommandHandler.Handle`（:24-28）按构造顺序取第一个「权限通过且存在该命令」的 handler：玩家命令优先于同名 Admin 命令；`UserCommandException` 消息回显执行者并转发其 `Connection.Observers`（:39-47），其它异常记 `FatalCommandError` 日志。

### 3. GM 权限模型

```csharp
// ServerLibrary/Envir/Commands/AdminCommandHandler.cs:9-12
public override bool IsAllowedByPlayer(PlayerObject player)
{
    return player.Character.Account.Admin || player.Character.Account.TempAdmin;
}
```

| 字段 | 位置 | 持久化 | 说明 |
|---|---|---|---|
| `AccountInfo.Admin` | ServerLibrary/DBModels/AccountInfo.cs:447-460（属性 + `_Admin`） | ✅ Users.db | 永久 GM。仓内测试账号 `test@test.com` 即 `Admin=true`（AGENTS.md:45） |
| `AccountInfo.TempAdmin` | AccountInfo.cs:553（**公共字段**，非属性） | ❌ 不落库（MirDB 只扫属性） | 临时 GM：用「非邮箱格式登录名 + `Config.MasterPassword`」登录获得（AGENTS.md:46-47 指向 SEnvir Login 逻辑） |
| `AccountInfo.Observer` | AccountInfo.cs:538-551（属性） | ✅ | 观战标记；**不参与 GM 命令权限**（IsAllowedByPlayer 不读它）。仓库无 `Observership` 字段 |
| `AccountInfo.IsAdmin(bool includeTemp)` | AccountInfo.cs:696-699 | — | `Admin || (includeTemp && TempAdmin)`，命令体系之外的便捷判定 |

另有聊天特权：`@!` 全服公告仅要求 `TempAdmin`（PlayerObject.cs:1771-1785 附近分支）。

## 数据结构 / 协议细节：配置项全集（Server.ini）

紧跟 `[ConfigSection("X")]` 之后的属性都属于节 X。`ClientHash` 是普通静态字段（Config.cs:26）不入 ini。

### [Network]（Config.cs:11）

| 字段 | 类型 | 默认值 | 作用 | 行 |
|---|---|---|---|---|
| IPAddress | string | `127.0.0.1` | 监听 IP（游戏端口与人数端口共用） | 12 |
| Port | ushort | `7000` | 游戏主端口 | 13 |
| TimeOut | TimeSpan | `00:00:20` | 连接超时 | 14 |
| PingDelay | TimeSpan | `00:00:02` | 心跳间隔 | 15 |
| UserCountPort | ushort | `3000` | 在线人数查询端口 | 16 |
| MaxPacket | int | `50` | 每循环单连接最大处理包数（超则封禁） | 17 |
| PacketBanTime | TimeSpan | `00:05:00` | 超包封禁时长 | 18 |
| SyncRemotePreffix | string | `http://127.0.0.1:80/Command/` | 远程 SystemDB 同步服务前缀 | 19 |

### [System]（:21）

| 字段 | 类型 | 默认值 | 作用 | 行 |
|---|---|---|---|---|
| CheckVersion | bool | `true` | 校验客户端哈希 | 22 |
| VersionPath | string | `./Zircon.dll` | 版本文件（LoadVersion 算 ClientHash） | 23 |
| MapPath | string | `Debug/ServerCore/Map/` | 服务端地图目录 | 25 |
| ClientHash | byte[]（字段） | `null` | 运行时 SHA-256，不入 ini | 26 |
| MasterPassword | string | `REDACTED`（仓内脱敏） | 主密码（TempAdmin 登录） | 27 |
| SyncKey | string | `REDACTED` | 远程同步密钥 | 28 |
| ClientPath | string | `null` | 客户端目录（本地 DB 分发目标） | 29 |
| ReleaseDate | DateTime | `2017-12-22 18:00 UTC` | 版本发布时间 | 30 |
| TestServer | bool | `false` | 测试服标记 | 31 |
| StarterGuildName | string | `Starter Guild` | 新手行会名 | 32 |
| LazyLoadMaps | bool | `true` | 地图懒加载（false 启动全量加载） | 33 |
| EasterEventEnd | DateTime | `2018-04-09` | 复活节活动截止 | 34 |
| HalloweenEventEnd | DateTime | `2018-11-07` | 万圣节活动截止 | 35 |
| ChristmasEventEnd | DateTime | `2019-01-03` | 圣诞活动截止 | 36 |
| DBSaveDelay | TimeSpan | `00:05:00` | **数据库自动保存间隔**（SEnvir.cs:1376） | 37 |
| EncryptionEnabled | bool | `false` | 数据库加密开关 | 38 |
| EncryptionKey | string | `""`（Base64 32 字节） | 数据库加密密钥 | 39 |

### [Control]（:41）

| 字段 | 类型 | 默认值 | 作用 | 行 |
|---|---|---|---|---|
| AllowLogin | bool | `true` | 允许登录 | 42 |
| AllowNewAccount | bool | `true` | 允许注册 | 43 |
| AllowChangePassword | bool | `true` | 允许改密 | 44 |
| AllowRequestPasswordReset | bool | `true` | 允许请求重置密码 | 46 |
| AllowWebResetPassword | bool | `true` | 允许网页重置 | 47 |
| AllowManualResetPassword | bool | `true` | 允许手动重置 | 48 |
| AllowDeleteAccount | bool | `true` | 允许删号 | 50 |
| AllowManualActivation | bool | `true` | 允许手动激活 | 52 |
| AllowWebActivation | bool | `true` | 允许网页激活 | 53 |
| AllowRequestActivation | bool | `true` | 允许请求激活邮件 | 54 |
| AllowSystemDBSync | bool | `false` | 允许系统库远程同步 | 55 |
| AllowNewCharacter | bool | `true` | 允许建角色 | 57 |
| AllowDeleteCharacter | bool | `true` | 允许删角色 | 58 |
| AllowStartGame | bool | `false`（C# 默认） | 允许进入游戏（封服开关） | 59 |
| RelogDelay | TimeSpan | `00:00:10` | 重登延迟 | 60 |
| AllowWarrior/AllowWizard/AllowTaoist/AllowAssassin | bool×4 | `true` | 职业开放开关 | 61-64 |

### [Mail]（:66）

MailServer(`smtp.gmail.com`)/MailPort(587)/MailUseSSL(true)/MailAccount/MailPassword/MailFrom/MailDisplayName —— 注册激活与找回密码邮件（:67-73）。

### [WebServer]（:75）

EnableWebServer(false)/WebPrefix(`http://*:80/Command/`)/WebCommandLink；激活与重置的 6 个跳转链接（:80-85）；BuyPrefix/BuyAddress/IPNPrefix/ReceiverEMail（PayPal 充值，:87-90）；ProcessGameGold(true)/AllowBuyGameGold(true)（:91-92）。

### [Players]（:95）

| 字段 | 类型 | 默认值 | 作用 | 行 |
|---|---|---|---|---|
| MaxViewRange | int | `18` | 视野范围 | 96 |
| NPCInteractionRange | int | `2` | NPC 交互距离（可见≠可交互，源码注释 :97-99） | 100 |
| ShoutDelay | TimeSpan | `00:00:10` | 喊话冷却 | 101 |
| GlobalDelay | TimeSpan | `00:01:00` | 全服喊话冷却 | 102 |
| MaxLevel | int | `10` | 等级上限 | 103 |
| SinglePlayerDev | bool | `false` | 单机开发模式（`--singleplayer-dev` 注入满级测试数据，中文注释 :104-105） | 106 |
| DayCycleCount | int | `3` | 昼夜循环 | 107 |
| SkillExp | int | `3` | 技能经验倍率 | 108 |
| AllowObservation | bool | `true` | 允许观战 | 109 |
| AllowWaypoints / MaxWaypoints | bool/int | `true`/`5` | 传送点 | 110-111 |
| BrownDuration | TimeSpan | `00:01:00` | 棕名时长 | 112 |
| PKPointRate | int | `50` | PK 点倍率 | 113 |
| PKPointTickRate | TimeSpan | `00:01:00` | PK 点结算周期 | 114 |
| RedPoint | int | `200` | 红名阈值 | 115 |
| PvPCurseDuration / PvPCurseRate | TimeSpan/int | `01:00:00`/`4` | PK 诅咒 | 116-117 |
| AutoReviveDelay | TimeSpan | `00:10:00` | 自动复活延迟 | 118 |
| RankChangeResetDelay | TimeSpan | `1.00:00:00` | 排名重置延迟 | 119 |
| EnableStruck | bool | `false` | 攻杀顿点 | 120 |
| EnableHermit | bool | `false` | 隐士点功能 | 121 |

### [Monsters]（:123）

DeadDuration(`00:01:00` 尸体存留)/HarvestDuration(`00:05:00` 割取)/MysteryShipRegionIndex(0)/LairRegionIndex(0)（:124-127）。

### [Items]（:129）

DropDuration(`01:00:00`)/DropDistance(5)/DropLayers(5)/TorchRate(10)/MaxGemPurity(100)/SpecialRepairDelay(`08:00:00`)/MaxLuck(7)/MaxCurse(-10)/CurseRate(20)/LuckRate(10)/MaxStrength(5)/StrengthAddRate(10)/StrengthLossRate(20)/DropVisibleOtherPlayers(true)/EnableFortune(true)/AdminStartInGamemasterMode(true)/AdminStartInObserverMode(true)/AdminStartInSupermanMode(true)（:130-149）。后三项决定 GM 账号进游戏的默认模式。

### [Rates]（:151）—— 全服倍率（重点）

| 字段 | 类型 | 默认值 | 作用 | 行 |
|---|---|---|---|---|
| ExperienceRate | int | `0` | 经验加成（百分点） | 152 |
| DropRate | int | `0` | 掉率加成 | 153 |
| GoldRate | int | `0` | 金币加成 | 154 |
| SkillRate | int | `0` | 技能经验加成 | 155 |
| CompanionRate | int | `0` | 宠物经验加成 | 156 |

**System.db 中不存在 Settings 类**（LibraryCore/SystemModels/ 全目录 grep 无 ExpRate/DropRate/MobDelay/Settings 类命中）；倍率体系分三层：

1. 全服层：`Config.*Rate`（上表）。
2. 地图层：`MapInfo.DropRate/ExperienceRate/GoldRate` 及 Max 上限（MapInfo.cs:335,350,365,410,425,440；源码在 MonsterHealth 区标注 `//DO NOT USE`）。怪物进场时按「基础值+偏移」随机 roll 出 `MapHealthRate/MapDamageRate/MapExperienceRate/MapDropRate/MapGoldRate`（MonsterObject.cs:682-689），活动怪再乘系数（HalloweenMonster.cs:47-49 `MapDropRate *= 10`）。
3. 玩家层：`Stat.DropRate/BaseDropRate/GoldRate` 等装备/转生属性（MonsterObject.cs:2692-2698 掉落结算 `rate *= 1M + owner.Stats[Stat.DropRate] / 100M`；PlayerObject.cs:2456-2457 转生 `+20%/次`）。

**MobDelay 不存在**——怪物节奏由 `MonsterInfo.AttackDelay/MoveDelay`（每怪模板值，MonsterInfo.cs OnCreated 默认 2500/1800ms）与 `MapInfo.SkillDelay` 控制。任务提示中的「MobDelay」在仓库无命中，疑为对 `MonsterInfo` 逐怪延迟的误记。

### [Fishing]（:158）

FishEnablePerfectCatch(true)/FishNibbleChanceBase(10)/FishPointsRequired(50)/FishPointSuccessRewardMin/Max(2/5)/FishPointFailureRewardMin/Max(0/5)（:159-165）。

### 运行时配置入口：ConfigView

`Server/Views/ConfigView.cs` 的 `LoadSettings`（:63-186）把控件值读自 `Config.*`，`SaveSettings`（:187-315）关窗时反向写回静态属性（再由退出时 ConfigReader.Save 落 ini）。窗口还带数据库加密入口与 System.db 本地分发（:25-48）。即：**运行期改配置 = 改静态属性；落盘 = 关服/关窗时**。

## GM 命令全集（ServerLibrary/Envir/Commands/Command/Admin/，34 条）

「别名」列 = VALUE 与类名/文件名不一致的命令。参数列照抄源码解析（vals[0]=命令本身）。大小写不敏感。

| 命令 VALUE | 别名（类名） | 参数 | 作用 | 关键行号 |
|---|---|---|---|---|
| ADDSTAT | — | `<槽位EquipmentSlot> <属性Stat> <数值int>`（PARAMS_LENGTH=4；示例 `AddStat Weapon MaxDC 50`） | 给自己指定装备槽的穿戴物品加 `StatSource.Added` 属性并刷新属性/外形；槽空则静默 | AddStat.cs:10-16 |
| BAN | — | `<角色名> [分钟int=525600(365天)]`（PARAMS_LENGTH=2） | 封目标账号：Banned/BanReason/BanExpiry，在线立即踢下线 | Ban.cs:11-19 |
| CHATBAN | — | `<角色名> [分钟int]` | 禁言：设账号 ChatBanExpiry | ChatBan.cs:9-15 |
| CLEARIPBLOCKS | — | 无参 | 清空 SEnvir.IPBlocks | ClearIPBlocks.cs:7-9 |
| CREATEGUILD | — | `<行会名>` 或 `<角色名> <行会名>`（PARAMS_LENGTH=2，vals.Length<3 时给自己建） | GM 代建行会 + 会长成员记录（Leader 权限） | CreateGuild.cs:12-20 |
| ENDCONQUEST | — | `<城堡名>`（去空格+忽略大小写） | 立即结束该城堡攻城战（ConquestWar.EndTime=MinValue） | EndConquest.cs:11-19 |
| GCOLLECT | 类名 ForceGarbageCollection | 无参 | 强制完整 GC 并回显耗时 | ForceGarbageCollection.cs:9-11 |
| GIVEGAMEGOLD | — | `<角色名> <数量int>`（PARAMS_LENGTH=3；**无长度预检**，直接索引 vals[1]/vals[2]） | 给账号加 GameGold；有推荐人则 HuntGold += count/10 | GiveGameGold.cs:12-15 |
| GIVEHORSE | — | `<角色名> <HorseType枚举>`（PARAMS_LENGTH=2 但读 vals[2]，实际需 3 段） | 发坐骑：在线 GiveHorse / 离线写 Account.Horse | GiveHorse.cs:11-19 |
| GIVESKILLS | — | `<在线角色名>`（PARAMS_LENGTH=2） | 补齐该角色职业全部可学技能并按等级拉到 1/2/3 级 | GiveSkills.cs:12-20 |
| GLOBALBAN | 类名 GlobalShoutBan | `<角色名> [分钟int]` | 全球喊话禁令 GlobalShoutExpiry | GlobalShoutBan.cs:9-15 |
| GOTO | — | `<在线角色名>` | 自己传送到目标所在地图坐标 | Goto.cs:8-16 |
| KICK | — | `<角色名>`（不能是自己） | SendDisconnect(Kicked) 强踢 | Kick.cs:11-19 |
| LEVEL | — | `<等级int>`（自己）或 `<角色名> <等级int>`（vals.Length≥3 走目标分支） | 直接设等级并触发 LevelUp() | Level.cs:8-27 |
| LEVELSKILL | — | `<角色名> <技能名> <等级int>`（PARAMS_LENGTH=4；技能名全大写匹配） | 设技能等级、清经验、发 MagicLeveled | LevelSkill.cs:12-20 |
| LEVELWEAPON | — | 无参（仅自己） | 手持武器炼制 +1（打 Refinable 标记、发 ItemExperience） | LevelWeapon.cs:10-12 |
| MAKE | — | `<物品名> [数量=1] [收货角色]`（PARAMS_LENGTH=2；vals>3 时 vals[2]=数量 vals[3]=玩家） | 造物：货币走余额（含溢出保护+里程碑），普通物品按 StackSize 分批、打 GameMaster 标记 | Make.cs:13-21 |
| MOVE | 类名 MapMove | `<地图FileName> [x] [y]`（PARAMS_LENGTH=2；恰 4 段才解析坐标并校验边界） | 传送：带合法坐标定点，否则地图随机点（按 FileName 忽略大小写） | MapMove.cs:12-20 |
| PROMOTEFAME | — | `[在线角色名]`（裸命令=自己） | 提升声望段位 | PromoteFame.cs:8-15 |
| REBOOT | — | 无参 | **取消自己在拍卖行的超级寄售**（MarketPlaceCancelSuperior）并回显耗时，非重启服务器 | Reboot.cs:9-11 |
| RECALL | 类名 RecallPlayer | `<在线角色名>`（PARAMS_LENGTH=2） | 把目标传到自己面前一格 | RecallPlayer.cs:8-16 |
| REMOVECAPTION | 类名 RemovePlayerCaption | `<角色名>`（PARAMS_LENGTH=1 但读 vals[1]，实际需 2 段） | 清除角色自我介绍并落库 | RemoveCaption.cs:10-19 |
| REMOVEPKPOINTS | — | `[在线角色名]` | 移除 BuffType.PKPoint 增益 | RemovePKPoints.cs:11-14 |
| RESETDISCIPLINE | — | `[角色名]`（PARAMS_LENGTH=2；可离线） | 移除武学全部技能、删 UserDiscipline、发 DisciplineUpdate{null} | ResetDiscipline.cs:10-19 |
| SETCOMPANIONLEVEL | — | `<等级1-15>`（仅自己） | 设出战宠物等级（发 CompanionUpdate + CheckSkills） | SetCompanionLevel.cs:9-17 |
| SETCOMPANIONSTAT | — | `<档位3/5/7/10/11/13/15> <Stat枚举**大小写敏感**> <数值>` | 直接改写宠物该档成长属性块并刷新 | SetCompanionStat.cs:11-14 |
| SETHERMITSTAT | — | `<属性Stat(ignoreCase)> <数值int>` | 设角色隐士点 HermitStats + RefreshStats | SetHermitStat.cs:9-17 |
| MONSTER | 类名 SpawnMob（文件 SpawnMob.cs） | `<怪物名> [数量=1]`（PARAMS_LENGTH=2） | 在自己面前刷怪（GetMonsterInfo 循环 Spawn） | SpawnMob.cs:9-17 |
| STARTCONQUEST | — | `<城堡名>` | 手动开攻城战（已在战中抛异常） | StartConquest.cs:11-19 |
| TAKECASTLE | — | `<城堡名>` | 无行会→解除占领；有行会→判给自己行会；全服广播+ApplyCastleBuff | TakeCastle.cs:14-22 |
| TAKEGAMEGOLD | — | `<角色名> <数量int>`（PARAMS_LENGTH=3） | 扣 GameGold（无下限保护，可为负） | TakeGameGold.cs:12-20 |
| GAMEMASTER | 类名 ToggleGameMaster | 无参 | 切换 GameMaster（怪物/玩家不选中自己） | ToggleGameMaster.cs:11-16 |
| OBSERVER | 类名 ToggleObserver | 无参 | 切换 Observer 隐身并重算视野对象 | ToggleObserver.cs:11-18 |
| SUPERMAN | 类名 ToggleSuperman | 无参 | 切换 Superman 无敌 | ToggleSuperman.cs:11-16 |

**取参缺陷备注**（参数不足时不会被 `ThrowNewInvalidParametersException` 拦下，而是 IndexOutOfRangeException 落入 FatalCommandError 兜底）：GIVEHORSE（PARAMS_LENGTH=2 但读 vals[2]）、REMOVECAPTION（=1 但读 vals[1]）、GIVEGAMEGOLD（完全无预检）、玩家侧 EVENT（=1 但读 vals[1]）。

**与 AGENTS.md 命令表核对**（AGENTS.md:49-100，以源码为准）：
- `@move/@goto/@recall/@level/@addstat/@giveSkills/@levelSkill/@levelWeapon/@resetDiscipline/@removePKPoints/@make/@giveHorse/@kick/@ban/@chatban/@clearIPBlocks/@createGuild/@giveGameGold/@takeGameGold/@promoteFame/@setCompanionLevel/@setCompanionStat/@setHermitStat` —— 大小写不同但 VALUE 匹配，**有效**。
- `@spawn` ✗ → 应为 `@MONSTER`（SpawnMob.cs:9）；`@toggleGM` ✗ → `@GAMEMASTER`（ToggleGameMaster.cs:11）；`@toggleSuperman` ✗ → `@SUPERMAN`；`@toggleObserver` ✗ → `@OBSERVER`。
- `@reboot` 名义上"重启服务器"是**误导**：实际只取消自己拍卖行超级寄售（Reboot.cs:9-11）。
- GodotClient 审计脚本用的是正确的 `@monster TigerSnake 3`（GameScene.cs:9215,9246），可交叉印证。

## 玩家命令全集（Commands/Command/Player/，人人可用）

| 命令 VALUE | 文件 | 参数 | 作用 | 关键行号 |
|---|---|---|---|---|
| BLOCKWHISPER | BlockWhisper.cs | 无参 | 切换拒收私聊 | BlockWhisper.cs:10 |
| CLEARBELT | ClearBelt.cs | 无参 | 清空快捷栏（逐个 Delete BeltLinks） | ClearBelt.cs:9 |
| EVENT | Event.cs | `<任意串>`（PARAMS_LENGTH=1，读 vals[1]） | 记录 `LastCommand[player.Name]` 并触发 `SEnvir.EventHandler.Process(player, "PLAYERCOMMAND")` 事件脚本 | Event.cs:8-18 |
| RECALLGROUP | GroupRecall.cs | 无参 | 队长召回组员（需 RecallSet 套装+队长身份+地图许可+3 分钟冷却，冷却 :28/:52） | GroupRecall.cs:9 |
| ROLL | GroupRoll.cs | 无参 | 组队掷骰 1-6 群播 | GroupRoll.cs:8 |
| LEAVEGUILD | LeaveGuild.cs | 无参 | 退出行会 GuildLeave() | LeaveGuild.cs:7 |
| EXTRACTORLOCK | ToggleExtractorLock.cs | 无参 | 切换提取器锁 | ToggleExtractorLock.cs:10 |
| ALLOWRECALL | ToggleGroupRecall.cs | 无参 | 切换账号 AllowGroupRecall（是否可被召回） | ToggleGroupRecall.cs:8 |
| ALLOWGUILD | ToggleGuildInvite.cs | 无参 | 切换账号 AllowGuild（是否接受行会邀请） | ToggleGuildInvite.cs:8 |
| ALLOWTRADE | ToggleTrade.cs | 无参 | 切换账号 AllowTrade（是否允许交易） | ToggleTrade.cs:10 |
| ENABLELEVEL3/5/7/10/11/13/15 | Player/Companion/ToggleCompanion{N}.cs（基类 AbstractToggleCompanion.cs） | 无参 | 切换宠物对应等级档技能锁（CompanionLevelLock3/5/7/10/11/13/15）；VALUE 由 `string.Format(base.VALUE, N)` 生成（AbstractToggleCompanion.cs:11，子类如 ToggleCompanion3.cs:5） | AbstractToggleCompanion.cs:11-16 |

注册与 GM 侧完全同一套机制（`IPlayerCommand` 锚点 + 反射扫描）；`PlayerCommandHandler.IsAllowedByPlayer` 恒 true（PlayerCommandHandler.cs:9-12）。玩家命令类放在 `Command/Player/` 及其 `Companion/` 子目录，命名空间不影响扫描（基类捕获所有 `AbstractCommand<IPlayerCommand>` 子类）。

## GodotClient 现状

命令与配置的服务端逻辑**无需也不应在客户端移植**（命令在服务端解析执行）。客户端相关现状逐项：

| 功能 | 状态 | 依据（GodotClient 实际文件） |
|---|---|---|
| `@` 命令发送链路 | **已移植** | GodotClient/Scripts/GameScene.cs:1964 `SendChat("@AllowTrade")`（快捷键 TradeAllowSwitch 直接映射玩家命令）；GameScene.cs:9215,9246 审计脚本 `SendChat("@monster TigerSnake 3")`——客户端只需把 `@` 开头文本当聊天发出去 |
| GM 命令辅助 UI/面板 | 未移植 | GodotClient/ 无任何 GM 工具窗（grep `CommandHandler|@toggleGM` 等无命中；原版 Client/ 也无 GM 面板，GM 命令历来靠手打） |
| 服务端配置 UI（ConfigView 等价物） | 未移植（也不该移植进游戏客户端） | GodotClient/Controls/ConfigDialog.cs 是**客户端图形/音效设置**，与服务端 Server.ini 无关 |
| 命令行启动参数 | 已移植（Godot 特有） | GodotClient/Scripts/AutoLoginArgs.cs:84 起：`--server/--port/--user/--pass/--char/--window` 解析（AGENTS.md:35-38 的启动命令即用它们） |
| 倍率/运行时配置的显示 | 未移植 | 无对 Config.Rates 或 MapInfo 倍率的 UI 展示 |
| TempAdmin `@!` 公告等聊天特权 | 部分 | 聊天输入框已支持任意文本（含 `@!`）原样发送；无专门 UI [INFERENCE] |

## 移植注意事项

1. **命令大小写**：匹配是 `input[0].ToUpper()` 与全大写 VALUE 的 Ordinal 相等——Godot 侧做命令补全/快捷键映射时统一转大写再匹配即可；不存在别名表，别抄 AGENTS.md 的 `@spawn/@toggleGM` 写法。
2. **命令协议即聊天文本**：客户端 → 服务端只有普通聊天包（`C.Chat`），`@` 前缀在 PlayerObject.Chat 内分流（PlayerObject.cs:1786）。Godot 客户端做「命令面板」也只需拼聊天文本发送，不要发明新的 C2S 包。
3. **权限判定在服务端**：`Admin`（落库）与 `TempAdmin`（内存）都在 AccountInfo 上；Godot 侧若做 GM 按钮组，应依据服务端下发的能力标记（或干脆全部可见、由服务端拒绝），不要在客户端硬编码权限。
4. **新增命令的成本极低**：建一个类继承 `AbstractCommand<IAdminCommand>`/`AbstractParameterizedCommand<IAdminCommand>` 即被自动注册；同理 Godot 版服务端如保留此机制，注意 `IsSubclassOf` 对封闭泛型的精确匹配——继承错泛型参数（如 IPlayerCommand）命令会注册到另一个 handler。
5. **PARAMS_LENGTH 是「段数含命令本身」**且部分命令声明值与实际取参不符（见上文缺陷备注）；移植时按实际 `vals[n]` 用量校验，别照抄 PARAMS_LENGTH。
6. **配置是静态属性直读**：没有变更通知。`DBSaveDelay` 等在下次读取处生效；`Port/IPAddress` 等只在启动时消费一次（SEnvir.StartNetwork）。Godot 版若做热更新需自己加事件。
7. **Server.ini 退出重写**：ConfigReader.Save 用 `Encoding.Unicode` 整体重写（ConfigReader.cs:174 附近），手工编辑 ini 后若服务端正常退出会被属性当前值覆盖——改 ini 要在停服状态改，或用 ConfigView。
8. **倍率三层叠加**：全服 `Config.*Rate`（百分点）→ 地图 `MapInfo.*Rate`（进场 roll，含 Max 上限）→ 玩家 `Stat.*Rate`（乘法叠加，`rate *= 1M + x/100M`）。移植爆率/经验时必须三层都搬，漏掉任何一层数值都会偏离原服手感。
9. **`@REBOOT` 陷阱**：命名与行为不符（拍卖行操作），文档/工具里别做成「重启服务器」按钮；服务端真正的停服路径是 SMain 的 Stop 按钮（`SEnvir.Started = false`，SMain.cs:259-263）。
10. **观战错误回显**：`UserCommandException` 会转发给执行者的 `Connection.Observers`（ErrorHandlingCommandHandler.cs:39-47）；Godot 客户端实现观战（Observe）时要同步处理这些被转发的系统消息，否则观战者看不到 GM 命令报错。
