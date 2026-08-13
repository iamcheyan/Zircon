# BotRunner 机器人拟真行为系统（像真人一样活着）— 完整任务目标

## 一、任务背景与终极目标

Zircon 私服的 BotRunner（`~/development/zircon/BotRunner/`，纯协议级机器人，2768 行）目前的行为是
"巡逻+打怪+买东西"的脚本循环——用户明确不满意：**"他并不像真人，只是固定了一些行为，好多功能缺失"**。

**终极验收（用户原话）**：进游戏看这些机器人，感觉他们就像真人一样——
- 有的人在打怪（野外，按自己的等级找合适的怪）
- 有的人在安全区练级/练技能：**道士站在大刀（守卫）旁边召唤骷髅/神兽练召唤**；
  有人放精神力战法/辅助 buff 给别人，练技能熟练度
- 他们会组队（互相邀请、跟着队长走）
- 他们有装备成长（捡到装备穿上、买装备、武器升级）
- 路线像真人（进城→商店→出城→打怪区→回城补给，不是随机游走）

本 goal = 把 BotRunner 从"巡逻脚本"升级为**拟真行为系统**。核心设计指导：每个机器人有
"人格档案"（职业/作息/目标），由一个**行为调度器**（utility-based）驱动多类行为，
安全区练技能、组队协同、装备成长全都要落地。

## 二、现状（已核实，直接引用）

- `BotAgent.cs` 2246 行：协议处理完整（S.Object*/Item*/Group*/Trade* 等 30+ 包），
  行为有 TryPvPBehavior / Patrol / MoveToward / TryProfessionPreparation（已有雏形：
  法师/道士自动补盾、道士 SummonSkeleton/Shinsu/DemonicCreature 召唤已实现!）/ 选技能攻击
- 已发协议：C.Magic（Spell）、C.ItemUse/ItemSort/ItemMove、C.GroupResponse(Accept=true)、
  C.TradeRequestResponse、C.TownRevive、C.Mount、C.AutoPathWaypoint、C.JoinStarterGuild
- `BotWorld.cs`：World 状态（位置/血蓝/背包/怪物/NPC/玩家/安全区标记）已维护
- `BotMap.cs` 50 行：只有基础地图信息——**没有寻路**（MoveToward 是直线步进）
- 挂机模式：BotConfig 有 HomeMapIndex/FieldTrip/Chat 等 30+ 参数
- **JoinStarterGuild 已在发**（starter 公会机制存在）

## 三、数据资产（全部就绪，直接用）

- **MagicInfo 174 条**（System.db）: 每个技能的 Job（职业）/NeedLevel/MagicType——
  决定各职业能练什么（道士: 召唤骷髅/神兽/圣兽、精神力战法 SpiritSword、施毒、治愈;
  法师: 各系魔法+魔法盾; 战士: 攻杀/刺杀/半月/烈火）
- **GuardInfo 94 条**: 守卫（大刀）坐标——Map Index + X/Y（比奇 Map1 有 Guard）。
  **"道士在大刀旁召唤骷髅"= 找本图 GuardInfo 坐标 → 走到附近 → 循环放召唤技能**
  （大刀会帮忙杀骷髅？不——骷髅是自己的宠不会被杀；真人是站在大刀旁因为安全。就是站位语义）
- **MonsterInfo 434 + RespawnInfo 2475**: 怪物等级/攻击/AI + 每图刷怪点——
  机器人按自己等级（CharacterInfo level）选打怪区：`RespawnInfo.Where(r => 合适等级差 && 距城近)` 
- **SafeZoneInfo 17**: 安全区区域（城镇）——练技能行为的场地
- **ItemInfo 1078**: 装备判定（RequiredClass/RequiredLevel/Type）+ DropInfo 掉落
- 地图可行走数据：`Debug/Client/Map/*.map`（BotMap 可扩展解析 cell 可行走位，
  Mir3-Research/Tools/maps/ 有现成 .map 解析 Python 代码可参考逻辑）
- 导出快照 `/tmp/dbviewer_data/*.json`（注意写库相关不需要——本任务**只改 BotRunner 代码，不动数据库**）

## 四、行为系统设计（必须实现）

### 4.1 人格档案（BotProfile）
每个 bot 按序号生成稳定人格（当天不变）：
- 职业（已有 NewCharacter 时 Warrior——改成随机/轮转 4 职业）
- **作息类型**：早鸟/夜猫/全天（影响在线时段，可选简化为全程在线但行为节奏不同）
- **性格权重**：勤奋打怪型(50%) / 社交组队型(25%) / 悠闲挂机型(15%) / 商人型(10%)
- 目标等级段：跟随自身等级滚动（升级后换打怪区）

### 4.2 行为调度器（替换现有 Patrol 单循环）
Utility-based：每 Tick 对各行为打分取最高执行（冷却互斥）：

| 行为 | 触发条件（分数要素） | 动作 |
|---|---|---|
| **SafeZoneTraining 安全区练技能** ⭐核心新行为 | 自己在城镇安全区 && 有可练技能（召唤系 CD 好/辅助技能熟练度未满） | 道士: 走到本图 Guard 附近(±5格) 放召唤→等 CD→再放; 法师: 练魔法盾/瞬移; 辅助型: 给周围玩家放 heal/buff(精神力战法给队友) |
| **GrindFarming 打怪** | 等级落后/勤奋型/白天 | 寻路到选好的 Respawn 区→选怪→攻击循环(技能+普攻)→捡掉落→背包满/血低→回城 |
| **GroupPlay 组队** | 社交型 && 附近有其他 bot/玩家 | 发 C.GroupRequest（需补发这个包!现在只会 Accept）→ 跟随队长（队长移动时保持 3-5 格）→ 队长打怪时助攻 |
| **ShoppingTownTrip 回城补给** | 药水<30% / 背包满 / 武器耐久低 | 回城→找对应商店 NPC（已知 NPC 坐标数据!）→ C.NPCBuy 药水→修装备 C.NPCRepair→继续 |
| **EquipUpgrade 装备成长** | 捡到/买到更好装备(RequiredClass/Level 匹配+评分更高) | C.ItemMove 到装备栏; 攒钱到阈值→买武器→（可选）C.WeaponLevelUpgrade |
| **RestIdle 休息** | 悠闲型/夜间 | 安全区站着/小范围走动/偶尔坐下(C.Mount 下马/聊天) |
| **ChatSocial 聊天**（已有 ChatPrefix） | 随机间隔 | 频道说话（扩中文语料：打怪抱怨/求组/交易喊话，10+ 句式模板） |
| **PatrolFallback 巡逻** | 无更高分行为 | 现有逻辑兜底 |

### 4.3 组队协同（重点）
- 主动发组队：C.GroupRequest（查 ServerConnection.cs 客户端怎么发的，BotRunner 补对称包）
- 队长模式：队长选打怪区，队员 follow（记录队长 ObjectID，每 Tick 朝队长位置走）
- 队伍打怪：攻击队长当前目标（World.Monsters 里锁同一 ObjectID）
- 治愈：道士队员血低的队友 → 放治愈术（Target=队友 ObjectID）

### 4.4 技能练习细节（用户点名的场景）
- **道士召唤**: SummonSkeleton(7级)/SummonShinsu/SummonDemonicCreature——已有实现，
  扩展为"到大刀旁练"：找 GuardInfo 本图坐标→MoveToward→距离≤5 后循环施放（等技能 CD）
- **精神力战法(SpiritSword)/辅助 buff**: 给自己或范围内玩家放（Target=ObjectID）
- **熟练度**: 服务端 MagicObject 会按施放加经验（UserMagic.Experience）——机器人只管循环施放，
  熟练度自然涨（在 activity report 里输出 magics 熟练度变化验证）
- **法师**: 魔法盾维持(已有)+火墙/冰咆哮对空地放（安全区外练）

### 4.5 寻路（MoveToward 升级）
- BotMap 解析 .map 的 cell 可行走标志 → BFS/A* 网格寻路（地图 ≤1000x800，缓存路径）
- 跨图：Respawn 点/城镇间的图连接用 MovementInfo（System.db 1039 条传送关系）算图级路径
- 卡死检测：N tick 位置不变 → 重算路径 → 再卡 → 随机跳步/回城

### 4.6 装备与外观成长
- 捡到装备评分（ItemInfo Stats 求和）> 当前穿着 → 穿上（C.ItemMove 到 Equipment 格）
- 金币 > 5000 且武器评分低 → 找武器店 NPC 买（C.NPCBuy——查包结构补全）
- 定期 C.ItemSort 整理背包（已有）

### 4.7 反检测细节（拟真的关键）
- 行为间隔加随机抖动（±20%）；移动偶尔停顿 0.5-2s（"看风景"）
- 技能 CD 之间偶尔走两步；不完美直线（沿路径 ±1 格扰动）
- 聊天按句式模板+变量（地图名/怪物名/职业）生成，避免全服同一句

## 五、实施要求

1. **代码结构**：BotRunner/ 下新添 `BotProfile.cs`（人格）、`BotBehavior*.cs`（各行为类，
   统一接口 `IBotBehavior { double Score(BotAgent, DateTime); void Execute(BotAgent, DateTime); }`）、
   `BotPathfinder.cs`（寻路）。BotAgent.Tick 里换成调度器（保留现有协议处理与兜底）
2. **配置扩展**：BotConfig 加 BehaviorWeights/EnableSkillTraining/EnableGrouping 等开关（默认开）
3. **不破坏现有**：PvP/商店/挂机往返逻辑保留为行为之一；编译 `dotnet build BotRunner/BotRunner.csproj -m:2`
4. **每次大改动后编译**；最终整体 `dotnet build` 0 错误
5. **测试**：写库不需要。服务端测试：起 ServerCore（Debug/ServerCore，若 7000 被占等它空闲）
   + `dotnet BotRunner/bin/Debug/net10.0/BotRunner.dll BotRunner.single.json 8`（8 个 bot），
   跑 10 分钟看日志：应有"skill: summon/练技能""group: 邀请/跟随""equip: 换装"等行为日志，
   各行为至少触发一次
6. **游戏内截图验收**（无头 :101+openbox）：TestHero 进比奇城守卫附近截图——
   **能看到 2+ 个道士带骷髅站在大刀旁**（召唤行为可视证据）；野外截图有 bot 在打怪。
   截图存 `~/development/zircon/screenshots/`（编号续接）push
7. 文档：`~/development/zircon/docs/BOT_BEHAVIOR_SYSTEM.md`（行为清单/配置说明/测试方法/已知限制）
8. 提交：zircon 仓库（BotRunner 代码+文档+截图），中文 commit

## 六、边界与务实约束

- **不改服务端**（纯客户端协议级机器人；服务端技能 CD/熟练度机制已有）
- 不动 System.db/Users.db（bot 账号自动注册已有 AutoCreateAccount）
- 机器 4核15GB：bot 数测试用 5-8 个（BotRunner.single.json MaxBots=8），别 20 个全开
- 若某行为依赖的包实在缺（如 C.NPCBuy 结构不明），从 zircon GodotClient/Network/ServerConnection.cs
  找对应 Send 方法抄参数结构——客户端能发的机器人都能发
- **先跑通骨架（调度器+练技能+组队）再打磨细节**；时间不够时优先级：
  练技能(大刀旁召唤) > 组队 > 装备成长 > 寻路优化 > 聊天语料

## 七、验收标准（全部满足）

1. ✅ 编译 0 错误；8 bot 连跑 10 分钟无 Fail 退出
2. ✅ 日志证据：召唤练习 ≥20 次、组队形成 ≥1 队、换装 ≥3 次、买药 ≥5 次、BFS 寻路无卡死（卡死自愈 ≥1 次演示）
3. ✅ 游戏内截图：大刀旁道士+骷髅 ≥2 个；野外打怪 bot ≥1 个
4. ✅ 文档+提交完成
5. ✅ 最终报告（中文）：行为架构图、各行为触发统计（10 分钟跑的数据）、配置手册、
   已知限制与后续路线（如 PvP 策略/行会战/经济行为）

## 八、参考文件

- BotRunner 现有代码（本仓库 BotRunner/）
- 客户端发包大全：`GodotClient/Network/ServerConnection.cs`（所有 C.* 包的构造）
- 技能数据：/tmp/dbviewer_data/MagicInfo.json（174 技能: Job/NeedLevel/MagicType/CD）
- 怪物分布：/tmp/dbviewer_data/RespawnInfo.json + MonsterInfo.json
- 守卫坐标：/tmp/dbviewer_data/GuardInfo.json（94 条: Map/X/Y）
- 安全区：/tmp/dbviewer_data/SafeZoneInfo.json
- 地图连接：/tmp/dbviewer_data/MovementInfo.json
- 走格解析参考：Mir3-Research/Tools/maps/（Python .map 解析）
- 无头测试方法：Mir3-Research skill "GodotClient 无头验证"（Xvfb:101+openbox+scrot）
