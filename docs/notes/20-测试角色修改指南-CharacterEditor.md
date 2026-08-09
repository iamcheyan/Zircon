# 讨论记录 20：测试角色修改指南 —— CharacterEditor 工具

> 日期：2026-08-06
> 数据来源：`LibraryCore/MirDB/Session.cs`、`ServerLibrary/DBModels/{AccountInfo,CharacterInfo,UserItem,UserMagic,UserCurrency}.cs`、`LibraryCore/Enum.cs`（EquipmentSlot/GridType）、`ServerLibrary/Models/PlayerObject.cs`（物品装载/施法校验），全部源码核实。
> 关联：笔记 05（Godot 客户端登录/选角色全流程）、`Tools/AccountSetup`（自动建号）、`Tools/SystemDbProbe`（System.db 只读探查）。
> 结论：**测试角色数据存在 `Users.db`（账号+角色+物品+技能），用新工具 `Tools/CharacterEditor` 改；改完必须重启服务端才生效。** 本次已把测试角色 TestHero 升到 70 级、穿上 10 件 40-55 级装备、背包塞满大药和回城卷、学了全 32 个战士技能（Lv.3）。

---

> ## ⚠️ 2026-08-09 重要更正（此文档之前的路径/停服方式是错的，照着做会"改完又丢"）
>
> 反复出现"TestHero 魔法又只剩 12 个"的根因，是 **`CharacterEditor` 改错了库 + 停错了服务**：
>
> 1. **数据库根目录是 `Debug/ServerCore/Database`，不是 `/tmp/zircon-server/Database`。**
>    `ServerCore.csproj` 的 `OutputPath=..\..\Debug\ServerCore\`，服务端构建产物直接输出到 `Debug/ServerCore/` 并从那里运行，`Session.Root` 解析到 `./Database/` → **实际读取 `Debug/ServerCore/Database/Users.db`**。
>    `/tmp/zircon-server/Database` 是旧版 `scripts/start_server.sh` 的软链约定（指向 `Debug/Server/Database`），早已不用于运行。早期 boost 全打在 `Debug/Server/Database/Users.db`（旧约定目录），重启后服务端从 `Debug/ServerCore/Database/Users.db` 加载——只有 12 个道士技能，于是"改了好几次都像被重置"。
> 2. **存在两份 `Users.db`**：`Debug/ServerCore/Database/Users.db`（**运行时真实读取，规范副本**）与 `Debug/Server/Database/Users.db`（旧约定镜像，现与规范副本同步、md5 一致）。**CharacterEditor 一律改 `Debug/ServerCore/Database`**；改完用 `cp` 同步镜像（见 4.7）。
> 3. **停服/重启用 omp hub，不是 systemctl**。服务端由 hub 托管：名称 `zircon-farm-server`，`detached + restart=on-failure`——**直接 `kill` 会被自动拉起**，必须 `hub stop` 显式停止（不触发重启策略），改完 `hub start` 恢复（命令见 4.1）。`systemctl --user stop|start zircon-server` 是旧方式，当前不适用。
> 4. **2026-08-09 已修复并验证**：TestHero 魔法书 = 全部 **174 个魔法**（`--all-magics`，Lv.3 / Exp 1,000,000，含 `_Blank_`/Unused 占位——客户端自动忽略），账号 `Admin=True` 保持（GM 权限服务端判定 `Account.Admin || TempAdmin`）。headless 自动登录冒烟通过。

---

## 一、这次改了什么（TestHero 现状）

角色 `TestHero`（**道士** / 男 / 创建于 2026-08-06）当前数据：

| 项目 | 值 |
|---|---|
| 等级 | **70**（HP 3106 / MP 1391，按 Lv.70 道士 BaseStat 同步） |
| 职业 | **道士**（原战士，2026-08-07 改；战士技能保留） |
| 金币 | 10,000,000 |
| 武器 | Odyn Elemental（道士，道术 105） |
| 衣服 | Wyvern Armour Of Protection (M) |
| 头盔 | Lupine Headgear |
| 项链 | Amulet Of The Dragon Lord |
| 手镯 ×2 | Glowing Armguard Of Crescent ×2 |
| 戒指 ×2 | Dragon Ring Of Revival ×2 |
| 鞋子 | Argent Sabatons Of Comet |
| 护身符 | **Talisman ×200**（普通护身符，**Shape=0**，召唤魔法消耗用） |
| 背包 | Elixir Of Life (V) ×200、Elixir Of Mana (V) ×200、Scroll Of Town Portal ×40 |
| 技能 | **174 个**（四职业全魔法，Lv.3 / Exp 100 万，2026-08-09 `--all-magics` 补齐；含原 34 个道士/战士技能） |

原来新手装（Wood Sword / Commoner Outfit / Candle）保留在背包 0-2 槽。

### 召唤测试提示

- 两个召唤魔法都在魔法书里（Lv.3）：
  - **Summon Skeleton** 消耗 1 个护身符，召唤白骷髅（`MonsterFlag.Skeleton`）
  - **Summon Shinsu** 消耗 5 个护身符，召唤神兽（`MonsterFlag.Shinsu`）
- 施法实现（`ServerLibrary/Models/Magics/Taoist/SummonShinsu.cs:33`）要求护身符 **`ItemType.Amulet` 且 `Shape == 0`**（`UseAmulet` 校验），所以 Amulet 槽放的是普通 **Talisman**（Shape=0）而非 Talisman Of Soul（Shape=1）；Talisman 共 200 个可消耗，放完 Amulet 槽的 `--amulet-count` 再补。
- 召唤物是 `Player.Pets`（同一玩家最多 2 只），会自动跟随并攻击附近目标，用于测试攻击流程。
- 召唤不需要职业校验（`CanUseMagic` 只查等级），但建议保持道士职业使用（魔法书按职业显示）。

## 二、工具用法

`Tools/CharacterEditor`（C# / net10.0，引用 LibraryCore + ServerLibrary）。所有命令第一个参数都是**数据库根目录**（`<db-root>` 指 `Users.db`、`System.db`、`Backup/` 所在目录，**本机为 `Debug/ServerCore/Database`**——即服务端实际读取的目录，见顶部更正；在仓库根目录下执行）。

```bash
# 1. 列出所有账号/角色/物品/技能（可带账号邮箱、角色名过滤）
dotnet run --project Tools/CharacterEditor -- list Debug/ServerCore/Database
dotnet run --project Tools/CharacterEditor -- list Debug/ServerCore/Database test@test.com TestHero

# 2. 一键增强：改等级+金币+补装备+补技能（装备按等级挑最贵、职业匹配）
dotnet run --project Tools/CharacterEditor -- boost Debug/ServerCore/Database --char TestHero --level 70 --gold 10000000
#    只改等级不动装备技能：加 --no-items --no-magics（升完级装备技能已存在时用它）

# 3. 改职业 / 指定技能 / 换武器 / 补护身符
dotnet run --project Tools/CharacterEditor -- boost Debug/ServerCore/Database --char TestHero \
    --class Taoist --magic SummonSkeleton --magic SummonShinsu \
    --weapon "Odyn Elemental" --amulet-count 200 --no-items --no-magics
#    --class 改职业(HP/MP 按新职业 BaseStat 重算); --magic 可重复, 按名字加指定技能(给了就跳过职业全套)
#    --weapon 强制替换武器槽; --amulet-count 把 Amulet 槽换成 Shape=0 普通护身符并设数量(召唤魔法消耗用)

# 4. 查物品模板（挑装备/看属性）
dotnet run --project Tools/CharacterEditor -- items Debug/ServerCore/Database --type Weapon --class Taoist --min 40 --max 55

# 5. 查技能模板
dotnet run --project Tools/CharacterEditor -- magics Debug/ServerCore/Database --class Taoist

# 6. 查各职业/等级的基础 HP/MP 档位（BaseStat）
dotnet run --project Tools/CharacterEditor -- basestat Debug/ServerCore/Database --class Taoist
```

## 三、数据库结构（改之前必须懂）

### 3.1 两个库文件

| 文件 | 内容 | 修改工具 |
|---|---|---|
| `System.db` | 物品/技能/地图/怪物等**模板**（1078 物品、174 魔法、244 地图…） | 只读（SystemDbProbe / 本工具 items、magics 子命令） |
| `Users.db` | **账号 + 角色 + 角色物品 + 技能**（本工具读写对象） | CharacterEditor |

两个文件都不是 SQLite，是 MirDB 自定义二进制格式（`Library.Encryption` 混淆 + 分块存储），**不能直接改字节**，只能通过 `Session` 对象模型读写。

### 3.2 数据模型（Users.db）

```
AccountInfo (test@test.com)
├── Currencies (UserCurrency)        金币/商城币/HuntGold（Amount 字段）
├── StorageSize                      仓库格子数（默认 100）
├── Characters (CharacterInfo)
│   ├── Level / CurrentHP / CurrentMP / Class / Gender
│   ├── Items (UserItem)             ← 背包+装备 都在这一个表，用 Slot 区分
│   │      Slot < 1000       → 背包（0..47 对应 InventorySize 48）
│   │      Slot = 1000+槽位   → 装备（见 EquipmentSlot 表）
│   │      Slot >= 2000      → 材料包（PartsStorage，本工具未实现）
│   ├── Magics (UserMagic)           Info/Level/Experience/Set1-4Key
│   ├── BeltLinks (CharacterBeltLink) 快捷栏绑定（与物品分开存）
│   ├── Buffs / Quests / Refines / Friends / Milestones …
└── Mail / Auctions / Guild / Companions …
```

### 3.3 EquipmentSlot 槽位表（`EquipmentSlot` 枚举，装备 Slot = 1000 + 值）

| 值 | 槽位 | 值 | 槽位 |
|---|---|---|---|
| 0 | Weapon 武器 | 11 | Amulet 护身符 |
| 1 | Armour 衣服 | 12 | Flower 花 |
| 2 | Helmet 头盔 | 13 | HorseArmour 马甲 |
| 3 | Torch 火把 | 14 | Emblem 徽章 |
| 4 | Necklace 项链 | 15 | Shield 盾牌 |
| 5 | BraceletL 左手镯 | 16 | Costume 时装 |
| 6 | BraceletR 右手镯 | 17-21 | 钓鱼装备（Hook/Float/Bait/Finder/Reel） |
| 7 | RingL 左戒指 | | |
| 8 | RingR 右戒指 | | |
| 9 | Shoes 鞋子 | | |
| 10 | Poison 毒药 | | |

注意：腰带**不是**独立 ItemType/槽位，腰带类物品归属 `ItemType.Bracelet`。

### 3.4 关键字段

- `UserItem`：`Info`（指向 System.db 模板）、`Slot`、`Count`、`CurrentDurability/MaxDurability`、`Level`（物品强化等级，默认 1）、`AddedStats`（随机附加属性）。
- `UserMagic`：`Level`（1-3）、`Experience`、`Set1Key..Set4Key`（快捷栏键位，`SpellKey` 枚举）。
- `CharacterInfo.CurrentHP/CurrentMP`：**登录时会用 BaseStat 重算上限**（`RefreshStats`），如果 HP<=0 会直接城镇复活；所以改等级时要把 HP/MP 一起同步到该级 BaseStat（工具已自动做）。

## 四、之后想改什么，怎么改

### 4.1 标准流程（任何修改）

> **先 `hub stop`，不要 `kill`**：服务端由 omp hub 托管（`zircon-farm-server`，detached + `restart=on-failure`），直接 `kill` 会被自动拉起，改库期间又写回旧内存态。

```bash
# 1) 停服务端（hub 托管，见顶部更正）
hub stop  name=zircon-farm-server   # 用 omp 的 hub 工具执行；停完确认 pgrep -af ServerCore.dll 无输出
# 2) 备份（工具保存时也会自动备份一份到 Backup/，但手动一份更稳）
cp Debug/ServerCore/Database/Users.db Debug/ServerCore/Backup/Users.db.手动备份-$(date +%F)
# 3) 修改（见下）
# 4) 重启（恢复原运行方式：cwd=/tmp/zircon-server + dotnet Debug/ServerCore/ServerCore.dll）
hub start  name=zircon-farm-server  application=dotnet \
  args=["/home/tetsuya/development/Zircon/Debug/ServerCore/ServerCore.dll"] \
  cwd=/tmp/zircon-server detached=true restart=on-failure ready.port=7000
# 5) 验证：ss -tlnp | grep 7000；headless 冒烟：timeout 75s godot-mono --headless --path GodotClient -- --auto-login
#    应见 [Game] 进入游戏! 玩家: TestHero
```

### 4.2 改等级

```bash
dotnet run --project Tools/CharacterEditor -- boost Debug/ServerCore/Database --char TestHero --level 80 --no-items --no-magics
```

- 等级范围参考 `basestat` 子命令（战士到 90+ 都有档位）。改多高取决于你想放多高级的技能：**服务端施法时校验 `Player.Level >= MagicInfo.NeedLevel1`**（`MagicObject.CanUseMagic`），所以：
  - 70 级能放：Defensive Mastery(70)、Invincibility(65) 及以下全部
  - 80 级解锁：Physical/Magic Immunity、Advanced Defiance
  - 83/86/90/95 分别解锁：Seismic Slam、Defensive Blow、Crushing Wave、Elemental Swords

### 4.3 改金币

```bash
dotnet run --project Tools/CharacterEditor -- boost Debug/ServerCore/Database --char TestHero --gold 999999999 --no-items --no-magics
```

金币存在**账号级** `Currencies`（不是角色级），`boost --gold` 会改账号金币。

### 4.4 换装备 / 加背包物品

```bash
# 先看候选（按职业/类型/等级筛，带属性摘要）
dotnet run --project Tools/CharacterEditor -- items Debug/ServerCore/Database --type Weapon --class Warrior --min 50 --max 60
```

`boost`（不带 `--no-items`）会自动：给每个空装备槽按「职业匹配 + 需求等级 <= 当前等级 + 价格最高」挑一件；背包塞大血瓶/大蓝瓶/回城卷。已有物品的槽会跳过。

想精确指定某件装备？工具目前没做"指定物品"参数——两种办法：
- 改 `Tools/CharacterEditor/Program.cs` 的 `BoostItems()` 里的 `pool` 筛选条件（或加一个 `--item "名字"` 参数），重新 `dotnet build`。
- 或者直接把想要的物品 `ItemName` 写死在工具里（最简单：把 `pool` 换成 `all.Where(x => x.ItemName == "Odyn Son")`）。

### 4.5 改技能

```bash
# 看该职业全部技能
dotnet run --project Tools/CharacterEditor -- magics Debug/ServerCore/Database --class Warrior
# 一键补全套（只加没有的，Lv.3）
dotnet run --project Tools/CharacterEditor -- boost Debug/ServerCore/Database --char TestHero --no-items   # 只补技能
# 补全 174 个魔法（四职业全技能，Lv.3 / Exp 1,000,000；测试角色应保持这个状态）
dotnet run --project Tools/CharacterEditor -- boost Debug/ServerCore/Database --char TestHero --all-magics --no-items
```

- `boost --all-magics` 是幂等的：已学的跳过、只追加缺的，不会删现有技能；会包含 `_Blank_`(Magic=Unused) 占位条目，客户端加载时自动忽略，无副作用。
- 测试角色 TestHero 的**期望状态**：魔法书 174 个 + 账号 `Admin=True`（GM 权限）。改完用 `list` 验证：
  ```bash
  dotnet run --project Tools/CharacterEditor -- list Debug/ServerCore/Database test@test.com TestHero
  # 应见: 账号 ... (Admin=True ...)  与  魔法书 (174):
  ```

注意 `boost` 的 `--no-items`/`--no-magics` 是独立的：`--no-items` = 只改等级/金币/技能；`--no-magics` = 只改等级/金币/装备。

### 4.6 更多定制（直接改代码）

`Tools/CharacterEditor/Program.cs` 里 `BoostItems()` / `BoostMagics()` / `Boost()` 三段就是全部逻辑，看注释改即可。工具会 `Session.Save(true)` 提交，保存时自动备份到 `<db-root>/../Backup/`（即 `Debug/ServerCore/Backup/`）。

### 4.7 同步镜像库（改完必做）

存在**两份** `Users.db`：运行时读 `Debug/ServerCore/Database/Users.db`（规范副本）；`Debug/Server/Database/Users.db` 是旧约定镜像（历史遗留，某些脚本仍可能引用）。改完规范副本后**必须同步**，否则两份漂移、下次又"看起来像被重置"：

```bash
cp Debug/ServerCore/Database/Users.db Debug/Server/Database/Users.db
md5sum Debug/Server/Database/Users.db Debug/ServerCore/Database/Users.db   # 两个 md5 应一致
```

> 判断哪个是规范副本的硬证据：`ServerCore.csproj` 的 `OutputPath=..\..\Debug\ServerCore\`；每小时备份出现在 `Debug/ServerCore/Backup/Users/`（如 `Users 2026-08-09 01-00.db.gz`）；服务端进程 cwd 的 `Database/` 即其 `Session.Root`（`LibraryCore/MirDB/Session.cs`：`Root + "Users" + Extension`）。

## 五、注意事项

1. **必须停服务端再改**：服务端启动时把 Users.db 整个读进内存，运行中你改了盘上的文件，它不会感知；下次 `Save` 会把内存旧数据**覆盖写回**，你的修改就丢了。
2. **改完必须重启服务端**，新数据才生效。
3. 备份文件位置：`<db-root>/../Backup/`（Session 自动）+ 手动副本。
4. 物品 `Slot` 重复会导致装载错乱（服务端按 Slot 分数组），工具用 `ownedSlots` 集合保证不冲突；手工加物品务必避开已用 Slot。
5. 本机服务端由 **omp hub 托管**（名称 `zircon-farm-server`，detached + `restart=on-failure`；cwd=`/tmp/zircon-server`、`dotnet Debug/ServerCore/ServerCore.dll`），日常启停用 hub 工具：
   ```text
   hub stop  name=zircon-farm-server            # 优雅停止，不触发自动重启
   hub start name=zircon-farm-server application=dotnet \
     args=["/home/tetsuya/development/Zircon/Debug/ServerCore/ServerCore.dll"] \
     cwd=/tmp/zircon-server detached=true restart=on-failure ready.port=7000
   ```
   **不要 `kill` 进程**——`on-failure` 会自动拉起。`systemctl --user stop|start zircon-server` 是历史遗留的旧启动方式，当前不适用（对应旧软链约定）。
6. 想重建一个干净角色：先删掉 `Debug/ServerCore/Database/Users.db`（规范副本；镜像 `Debug/Server/Database/Users.db` 一并删或同步）再启动服务端，它会重建空库；然后用 `Tools/AccountSetup` 建号（test@test.com / test123 / TestHero）。
