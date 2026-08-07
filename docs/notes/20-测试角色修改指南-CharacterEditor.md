# 讨论记录 20：测试角色修改指南 —— CharacterEditor 工具

> 日期：2026-08-06
> 数据来源：`LibraryCore/MirDB/Session.cs`、`ServerLibrary/DBModels/{AccountInfo,CharacterInfo,UserItem,UserMagic,UserCurrency}.cs`、`LibraryCore/Enum.cs`（EquipmentSlot/GridType）、`ServerLibrary/Models/PlayerObject.cs`（物品装载/施法校验），全部源码核实。
> 关联：笔记 05（Godot 客户端登录/选角色全流程）、`Tools/AccountSetup`（自动建号）、`Tools/SystemDbProbe`（System.db 只读探查）。
> 结论：**测试角色数据存在 `Users.db`（账号+角色+物品+技能），用新工具 `Tools/CharacterEditor` 改；改完必须重启服务端才生效。** 本次已把测试角色 TestHero 升到 70 级、穿上 10 件 40-55 级装备、背包塞满大药和回城卷、学了全 32 个战士技能（Lv.3）。

---

## 一、这次改了什么（TestHero 现状）

角色 `TestHero`（战士 / 男 / 创建于 2026-08-06）当前数据：

| 项目 | 值 |
|---|---|
| 等级 | **70**（HP 3860 / MP 299，按 Lv.70 BaseStat 同步） |
| 金币 | 10,000,000 |
| 武器 | Nemesis, The Blade of Betrayal（战士，价 800000） |
| 衣服 | Wyvern Armour Of Protection (M) |
| 头盔 | Lupine Headgear |
| 项链 | Amulet Of The Dragon Lord |
| 手镯 ×2 | Glowing Armguard Of Crescent ×2 |
| 戒指 ×2 | Dragon Ring Of Revival ×2 |
| 鞋子 | Argent Sabatons Of Comet |
| 护身符 | Talisman Of Soul |
| 背包 | Elixir Of Life (V) ×100、Elixir Of Mana (V) ×100、Scroll Of Town Portal ×20 |
| 技能 | 全 32 个战士技能（Swordsmanship → Elemental Swords），全部 Lv.3 经验 100 万 |

原来新手装（Wood Sword / Commoner Outfit / Candle）保留在背包 0-2 槽。

## 二、工具用法

`Tools/CharacterEditor`（C# / net10.0，引用 LibraryCore + ServerLibrary）。所有命令第一个参数都是**数据库根目录**（`<db-root>` 指 `Users.db`、`System.db`、`Backup/` 所在目录，本机为 `/tmp/zircon-server/Database`）。

```bash
# 1. 列出所有账号/角色/物品/技能（可带账号邮箱、角色名过滤）
dotnet run --project Tools/CharacterEditor -- list /tmp/zircon-server/Database
dotnet run --project Tools/CharacterEditor -- list /tmp/zircon-server/Database test@test.com TestHero

# 2. 一键增强：改等级+金币+补装备+补技能（装备按等级挑最贵、职业匹配）
dotnet run --project Tools/CharacterEditor -- boost /tmp/zircon-server/Database --char TestHero --level 70 --gold 10000000
#    只改等级不动装备技能：加 --no-items --no-magics（升完级装备技能已存在时用它）

# 3. 查物品模板（挑装备/看属性）
dotnet run --project Tools/CharacterEditor -- items /tmp/zircon-server/Database --type Weapon --class Warrior --min 40 --max 55

# 4. 查技能模板
dotnet run --project Tools/CharacterEditor -- magics /tmp/zircon-server/Database --class Warrior

# 5. 查各职业/等级的基础 HP/MP 档位（BaseStat）
dotnet run --project Tools/CharacterEditor -- basestat /tmp/zircon-server/Database --class Warrior
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

```bash
# 1) 停服务端
systemctl --user stop zircon-server
# 2) 备份（工具保存时也会自动备份一份到 Backup/，但手动一份更稳）
cp /tmp/zircon-server/Database/Users.db /tmp/zircon-server/Backup/Users.db.手动备份-$(date +%F)
# 3) 修改（见下）
# 4) 重启
systemctl --user start zircon-server
```

### 4.2 改等级

```bash
dotnet run --project Tools/CharacterEditor -- boost /tmp/zircon-server/Database --char TestHero --level 80 --no-items --no-magics
```

- 等级范围参考 `basestat` 子命令（战士到 90+ 都有档位）。改多高取决于你想放多高级的技能：**服务端施法时校验 `Player.Level >= MagicInfo.NeedLevel1`**（`MagicObject.CanUseMagic`），所以：
  - 70 级能放：Defensive Mastery(70)、Invincibility(65) 及以下全部
  - 80 级解锁：Physical/Magic Immunity、Advanced Defiance
  - 83/86/90/95 分别解锁：Seismic Slam、Defensive Blow、Crushing Wave、Elemental Swords

### 4.3 改金币

```bash
dotnet run --project Tools/CharacterEditor -- boost /tmp/zircon-server/Database --char TestHero --gold 999999999 --no-items --no-magics
```

金币存在**账号级** `Currencies`（不是角色级），`boost --gold` 会改账号金币。

### 4.4 换装备 / 加背包物品

```bash
# 先看候选（按职业/类型/等级筛，带属性摘要）
dotnet run --project Tools/CharacterEditor -- items /tmp/zircon-server/Database --type Weapon --class Warrior --min 50 --max 60
```

`boost`（不带 `--no-items`）会自动：给每个空装备槽按「职业匹配 + 需求等级 <= 当前等级 + 价格最高」挑一件；背包塞大血瓶/大蓝瓶/回城卷。已有物品的槽会跳过。

想精确指定某件装备？工具目前没做"指定物品"参数——两种办法：
- 改 `Tools/CharacterEditor/Program.cs` 的 `BoostItems()` 里的 `pool` 筛选条件（或加一个 `--item "名字"` 参数），重新 `dotnet build`。
- 或者直接把想要的物品 `ItemName` 写死在工具里（最简单：把 `pool` 换成 `all.Where(x => x.ItemName == "Odyn Son")`）。

### 4.5 改技能

```bash
# 看该职业全部技能
dotnet run --project Tools/CharacterEditor -- magics /tmp/zircon-server/Database --class Warrior
# 一键补全套（只加没有的，Lv.3）
dotnet run --project Tools/CharacterEditor -- boost /tmp/zircon-server/Database --char TestHero --no-items   # 只补技能
```

注意 `boost` 的 `--no-items`/`--no-magics` 是独立的：`--no-items` = 只改等级/金币/技能；`--no-magics` = 只改等级/金币/装备。

### 4.6 更多定制（直接改代码）

`Tools/CharacterEditor/Program.cs` 里 `BoostItems()` / `BoostMagics()` / `Boost()` 三段就是全部逻辑，看注释改即可。工具会 `Session.Save(true)` 提交，保存时自动备份到 `<db-root>/../Backup/`。

## 五、注意事项

1. **必须停服务端再改**：服务端启动时把 Users.db 整个读进内存，运行中你改了盘上的文件，它不会感知；下次 `Save` 会把内存旧数据**覆盖写回**，你的修改就丢了。
2. **改完必须重启服务端**，新数据才生效。
3. 备份文件位置：`<db-root>/../Backup/`（Session 自动）+ 手动副本。
4. 物品 `Slot` 重复会导致装载错乱（服务端按 Slot 分数组），工具用 `ownedSlots` 集合保证不冲突；手工加物品务必避开已用 Slot。
5. 本机服务端是 `systemd-run --user --unit=zircon-server` 启动的（cwd=`/tmp/zircon-server`），日常启停：
   ```bash
   systemctl --user stop|start|restart zircon-server
   systemctl --user status zircon-server
   ```
6. 想重建一个干净角色：先删掉 Users.db（或整个 Database 目录）再启动服务端，它会重建空库；然后用 `Tools/AccountSetup` 建号（test@test.com / test123 / TestHero）。
