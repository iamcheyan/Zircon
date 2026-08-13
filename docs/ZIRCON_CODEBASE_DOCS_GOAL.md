# Zircon 原版服务端+客户端 全代码深度文档化 — 完整任务目标

## 一、任务背景与终极目标

Zircon 是 C# 传奇3（Mir3）私服引擎：**原版客户端**（`~/development/zircon/Client/`，WinForms + DXManager）
与**服务端**（`~/development/zircon/Server/` + `ServerLibrary/`）。我们正在把功能逐块移植到
**Godot 新客户端**（`~/development/zircon/GodotClient/`），旧客户端部分功能也终将被 Godot 端替代。

**痛点：移植时每次都要现读原版源码找逻辑**（魔法伤害公式、buff 结算、AI 行为、行会战、
物品掉落……），没有系统文档。本 goal 产出一套**超级详细的代码文档库**，之后移植任何功能
都是"查文档 → 对齐 → 移植"，不再翻源码。

**用户明确要求覆盖"各种各样的，涵盖所有代码"，魔法伤害这类核心战斗公式只是其中一例。**

## 二、文档库结构（产出到 ~/development/zircon/docs/codebase/）

每篇文档统一格式：**职责概述 → 关键类/文件清单（含路径+行号）→ 核心流程（带伪代码）→
数据结构/协议 → 与 Godot 端现状对比（已移植/部分/未移植）→ 移植注意事项**。

### A. 战斗系统（最核心，优先）
- **combat/magic-damage.md** —— 魔法伤害全链路：MagicInfo 字段语义（MinBasePower/MaxBasePower/
  MinLevelPower/MaxLevelPower/BaseCost/LevelCost…）、SkillLevel 加成、元素克制（ObjectElement），
  服务端 `ServerLibrary/Envir/Magic/` 下所有魔法类的 Execute() 伤害计算（每个职业每个技能的公式差异），
  暴击/幸运/诅咒结算顺序、PVP 减免、Math 公式原文照抄
- **combat/physical-damage.md** —— 物理攻击：命中判定（Accuracy vs Agility）、DC 上下限、
  攻杀/刺杀/半月/烈火等战士技能倍率、双持、武器幸运诅咒
- **combat/elements-and-buffs.md** —— 全部 Stat 枚举语义、buff 叠加/互斥规则、毒素（中毒/麻痹/冰冻）、
  神圣/暗黑/幻影系加减伤
- **combat/death-and-loot.md** —— 死亡掉落规则、PK 惩罚、红名、经验分配（组队分经验公式）

### B. 怪物与 AI
- **monster/ai-behaviors.md** —— MonsterObject 完整 AI 状态机（巡逻/仇恨/追击/回血/逃跑/援护），
  AI 编号含义（MonsterInfo.AI 字段，-1/0/1…各是什么行为）、MirMonType 特殊怪（主动攻击玩家/
  掉落专属/不移动）、经验/刷新（Respawn）规则
- **monster/boss-mechanics.md** —— BOSS 专属逻辑（IsBoss）：狂暴、召唤小怪、技能循环

### C. 物品与经济
- **item/items-and-stats.md** —— ItemInfo 全字段、ItemInfoStat 加成、随机属性生成（GenerateItem）、
  强化/升级（武器升级成功率公式）、耐久/修理、绑定
- **item/economy.md** —— 商店买卖价格公式（Price/SellRate）、拍卖/摆摊、货币体系（CurrencyInfo）
- **item/drops.md** —— DropInfo 权重掉落、几率物品、金色装备判定

### D. 地图与移动
- **map/tiles-and-movement.md** —— CellFlags（可行走/不可走/高台/水）、坐标系统、
  走路发包节流（Run/Walk 验证）、传送点/回城
- **map/instances.md** —— 副本/DynRegion 生成、难度缩放

### E. 网络协议（旧客户端↔服务端）
- **protocol/packets-c2s.md** / **protocol/packets-s2c.md** —— 全部 C./S. 包：字段、触发时机、
  服务端处理函数。按功能分组（登录/角色/移动/战斗/聊天/交易/行会/商店/技能）
- **protocol/connection-lifecycle.md** —— 连接建立、加解密（如有）、断线重连、超时

### F. 社交与系统玩法
- **social/guild.md** —— 行会创建/解散/权限（GuildRank）、行会战（攻城）完整流程、行会buff
- **social/marriage-and-mentor.md** —— 结婚/师徒（如有实现）
- **social/chat-and-mail.md** —— 聊天频道、喇叭、邮件系统
- **quest/quest-system.md** —— 任务系统：QuestInfo 结构、进度状态机、奖励发放（结合
  Mir3-Research 的任务设计文档交叉引用）
- **sys/consign-and-trade.md** —— 摆摊、玩家交易、仓库
- **sys/conquest-sabuk.md** —— 沙巴克攻城战完整规则（我们地图已定用 Z 版沙巴克，此篇重点）

### G. 服务端基础设施
- **infra/envir-and-spawn.md** —— Envir 主循环 tick、对象管理（MapObject 生命周期）、
  NPC 生成（Merchant/Region）、怪物刷新
- **infra/database.md** —— System.db 全表关系（结合 dbeditor 的 meta.json）、Users.db 账号/角色存档结构、
  存档时机
- **infra/config-and-commands.md** —— 服务端配置项全集、GM 命令全集（含用法和参数）

## 三、执行方式

1. **先扫描盘点**：`find Server ServerLibrary Client LibraryCore -name '*.cs' | wc -l` + 每目录
   文件数/行数统计，列出清单到 `docs/codebase/_index.md`（索引+进度表）
2. **按 A→G 顺序逐篇写**（A 战斗优先，是用户点名的重点）。每篇写完在 _index.md 打 ✅
3. **源码引用规范**：关键结论必须带 `路径:行号`（如 `ServerLibrary/Envir/Magic/FireBolt.cs:42`），
   公式直接抄 C# 原文再中文解释；伪代码块用 ```csharp
4. **Godot 对比列**：每篇末尾"GodotClient 现状"小节——已移植（引 Godot 文件）/缺失，
   这是我们后续移植的 checklist
5. **不要猜**：文档中所有结论必须来自读到的代码，找不到的实现写"未找到实现，疑在 XXX"
6. 篇幅不设上限，宁多勿漏；但每篇开头给 10 行内的 TL;DR 速查表

## 四、验收标准

1. `docs/codebase/` 下 ≥20 篇 .md，覆盖 A-G 全部 7 组主题，_index.md 索引齐全
2. 战斗组 4 篇必须完成且质量最高（magic-damage.md 单篇 ≥400 行，含 ≥15 个具体技能的伤害公式）
3. 每篇文档抽查 3 处 `路径:行号` 引用，与实际源码一致
4. Godot 对比列覆盖全部 20+ 篇
5. git commit+push 到 zircon 仓库（docs/ 目录，中文提交信息）

## 五、边界与约束

- **只读 Server/ServerLibrary/Client/LibraryCore，产出只写 docs/codebase/**——绝不改任何 .cs 源码
- GodotClient 只读参考（用于对比列）
- 单 goal 内完成；如果 token 紧张，优先保证 A 组 4 篇 + B 组 + protocol 2 篇的质量
- 机器资源：4核15GB，无并发编译需求，纯读+写文档
