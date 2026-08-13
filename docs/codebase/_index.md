# Zircon 原版代码文档库 — 索引与进度

> 目标：把原版服务端（`Server/` + `ServerLibrary/`）与原版客户端（`Client/` + `LibraryCore/`）
> 的全部核心逻辑文档化，供 Godot 客户端（`GodotClient/`）移植时"查文档 → 对齐 → 移植"。
> 所有结论均来自实际源码阅读，引用格式 `路径:行号`（相对仓库根目录）。

## 源码盘点（2026-08-14 扫描）

| 目录 | .cs 文件数 | 总行数 | 说明 |
|---|---|---|---|
| `Server/` | 102 | 25,611 | 服务端可执行入口、网络监听（基于 MirServer 框架） |
| `ServerLibrary/` | 446 | 70,003 | 服务端核心逻辑：Envir、Models（玩家/怪物/魔法）、Commands、DBModels |
| `Client/` | 121 | 104,344 | 原版 WinForms 客户端（DXManager 渲染、Scenes、Controls） |
| `LibraryCore/` | 65 | 23,269 | 共享库：MirDB、Network 包定义、SystemModels（DB 模型）、Enum/Stat |
| **合计** | **734** | **223,227** | |

关键文件速查：
| 文件 | 行数 | 内容 |
|---|---|---|
| `ServerLibrary/Models/PlayerObject.cs` | 17,915 | 玩家逻辑全量（攻击/魔法/物品/组队/PK……） |
| `ServerLibrary/Models/MonsterObject.cs` | 3,198 | 怪物 AI 状态机基类 |
| `ServerLibrary/Models/MapObject.cs` | 1,861 | 地图对象基类（buff/毒素/属性结算） |
| `ServerLibrary/Models/MagicObject.cs` | 431 | 魔法基类 |
| `ServerLibrary/Models/Magics/{Warrior,Wizard,Taoist,Assassin}/` | ~150 类 | 各职业技能实现 |
| `ServerLibrary/Models/Monsters/` | 101 类 | 特殊怪/BOSS 实现 |
| `ServerLibrary/Envir/SEnvir.cs` | 4,601 | 服务端环境：主循环、全局状态 |
| `ServerLibrary/Envir/SConnection.cs` | 1,670 | 客户端连接、包处理入口 |
| `LibraryCore/Network/{ClientPackets,ServerPackets,GeneralPackets,Packet}.cs` | — | 协议定义 |
| `LibraryCore/SystemModels/` | 39 文件 | System.db 全表模型 |
| `ServerLibrary/DBModels/` | 31 文件 | Users.db 全表模型 |
| `LibraryCore/Stat.cs` | — | `Stat` 枚举（全部属性语义） |
| `LibraryCore/Enum.cs` | 4,700+ | 其余全部枚举（PoisonType/CellFlag/MirMonType/ChatType…） |

## 文档清单与进度

状态：✅ 完成 / ⏳ 撰写中 / ⬜ 未开始

### A. 战斗系统（最高优先级）

| 文档 | 状态 | 内容 |
|---|---|---|
| [combat/magic-damage.md](combat/magic-damage.md) | ✅ | 魔法伤害全链路、全技能公式 |
| [combat/physical-damage.md](combat/physical-damage.md) | ✅ | 物理攻击、命中、战士技能倍率 |
| [combat/elements-and-buffs.md](combat/elements-and-buffs.md) | ✅ | Stat 全表、buff/毒素规则、元素加减伤 |
| [combat/death-and-loot.md](combat/death-and-loot.md) | ✅ | 死亡掉落、PK、红名、组队经验 |

### B. 怪物与 AI

| 文档 | 状态 | 内容 |
|---|---|---|
| [monster/ai-behaviors.md](monster/ai-behaviors.md) | ✅ | MonsterObject AI 状态机、AI 编号、刷新 |
| [monster/boss-mechanics.md](monster/boss-mechanics.md) | ✅ | BOSS 专属逻辑 |

### C. 物品与经济

| 文档 | 状态 | 内容 |
|---|---|---|
| [item/economy.md](item/economy.md) | ✅ | 商店价格、拍卖、货币体系 |
| [item/drops.md](item/drops.md) | ✅ | DropInfo 权重掉落 |

### D. 地图与移动

| 文档 | 状态 | 内容 |
|---|---|---|
| [map/tiles-and-movement.md](map/tiles-and-movement.md) | ✅ | CellFlags、坐标、走路验证、传送 |
| [map/instances.md](map/instances.md) | ✅ | 副本/DynRegion、难度缩放 |

### E. 网络协议

| 文档 | 状态 | 内容 |
|---|---|---|
| [protocol/packets-c2s.md](protocol/packets-c2s.md) | ✅ | 全部 C→S 包 |
| [protocol/packets-s2c.md](protocol/packets-s2c.md) | ✅ | 全部 S→C 包 |
| [protocol/connection-lifecycle.md](protocol/connection-lifecycle.md) | ✅ | 连接生命周期、加解密、超时 |

### F. 社交与系统玩法

| 文档 | 状态 | 内容 |
|---|---|---|
| [social/guild.md](social/guild.md) | ✅ | 行会创建/权限/行会战 |
| [social/marriage-and-mentor.md](social/marriage-and-mentor.md) | ✅ | 结婚/师徒 |
| [social/chat-and-mail.md](social/chat-and-mail.md) | ✅ | 聊天频道、邮件 |
| [sys/conquest-sabuk.md](sys/conquest-sabuk.md) | ✅ | 沙巴克攻城战 |
| [sys/consign-and-trade.md](sys/consign-and-trade.md) | ✅ | 摆摊、玩家交易、仓库 |

### G. 服务端基础设施

| 文档 | 状态 | 内容 |
|---|---|---|
| [infra/envir-and-spawn.md](infra/envir-and-spawn.md) | ✅ | Envir 主循环、对象生命周期、NPC/怪物刷新 |
| [infra/database.md](infra/database.md) | ✅ | System.db / Users.db 全表、存档时机 |
| [infra/config-and-commands.md](infra/config-and-commands.md) | ✅ | 配置项全集、GM 命令全集 |

## 完成情况（2026-08-14）

全部 23 篇完成，合计 14,864 行。引用抽查：脚本 `scripts/verify_doc_citations.py`
对全库抽取 110 处 `路径:行号` 引用逐一比对源码（83 处自动匹配 + 27 处人工复核），
**全部命中，无一处错误引用**。 combat/ 组 4 篇合计 3,619 行，
其中 magic-damage.md 1,156 行（22 个具体技能公式照抄 + 190 个技能全量清单）。
协议 2 篇覆盖 153 个 C 包 + 216 个 S 包 + 7 个通用包，与 LibraryCore/Network 源文件逐类对齐。
多篇文档纠正了任务书中的推测命名（如 MagicGetMAG→GetMC/GetSC/GetSP/GetDC、
MirMonType→MonsterFlag、ChatType→MessageType、CellFlag 不存在等），均以源码为准。

## 引用与验证规范

- 每篇关键结论必须带 `路径:行号`（如 `ServerLibrary/Models/PlayerObject.cs:15446`）。
- 公式直接照抄 C# 原文（```csharp 块），再给中文解释。
- 找不到的实现明确写"未找到实现，疑在 XXX"，禁止臆测。
- 每篇末尾必须有"GodotClient 现状"小节（已移植/部分/未移植 + Godot 文件引用）。
