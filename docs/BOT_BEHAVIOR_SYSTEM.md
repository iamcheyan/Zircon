# BotRunner 拟真行为系统

从"巡逻脚本"升级为拟真行为系统：人格档案 + 行为调度 + 本地 A* 寻路 + 城镇服务链。
机器人表现出真人式活动循环：进城 → 商店补给/卖货 → 出城打怪 → 回城休整/练技能，
道士在大刀（守卫）旁练召唤，玩家互组队跟随助攻。

## 架构

```
Tick
 ├── BackgroundReactions   喝药/自疗/职业准备/整理背包(每 tick)
 ├── 破围层 TryBreakout    被怪围死时先钻空格、全堵死才砍最近怪开路
 ├── 跨图滞留兜底           非主城 10 分钟回不去 → 回城卷/寻路
 ├── _autoPathActive       跨图服务端寻路走线(短路行为层)
 ├── _travelActive         本图长距离行程状态机(本地 A* 驱动)
 ├── 城镇服务链             修装 → 任务 → 卖货(TrySell) → 补给(TrySupply)
 └── 行为调度器 _scheduler  7 行为按 Score 竞争 + 滞回(1.3x+5)
```

### 文件

| 文件 | 职责 |
|---|---|
| `BotProfile.cs` | 人格档案（Social/Merchant/Grinder/Idle/NightOwl），权重 + 队长确定性轮转 `index % 4 == 2` |
| `BotBehaviors.cs` | `IBotBehavior` + 调度器 + 7 行为实现 |
| `BotPathfinder.cs` | 8 向 A*（MaxExpandNodes=60000，MaxPathLength=400，斜穿墙角禁止，运行时黑名单） |
| `BotAgent.cs` | Tick 编排、移动/战斗/交易原语、~40 个行为辅助 API |
| `BotConfig.cs` | 行为开关与节奏参数 |
| `BotWorld.cs` | 世界镜像（玩家/怪/NPC/背包/魔法），死亡与位置维护 |

### 行为与评分（要点）

| 行为 | 触发要点 |
|---|---|
| SafeZoneTraining ⭐ | 道士在大刀旁练召唤（75 分）。道士不卡补给门；城内道士优先训练、供给链推迟 60s |
| EquipUpgrade | 背包有评分高 5+ 的装备 → 80 分；服务端 ItemMove 原子交换，单包换装；卖货时顺手在装备店买升级件（`shop: buy upgrade`，3 分钟节流） |

## 数据集事实（踩坑记录）

- **本数据集所有地图 `CanAutoPath=False`**：服务端 AutoPath 不可用，本图移动全部走本地 A*
  （跨图仍发 `C.AutoPathWaypoint`）。
- **商店区在比奇东南角**：David (397,363)、Mr. Kang (402,356)、Amy (414,349)、Isaac (470,424)、
  **Lennard (450,413) 卖护身符（道士召唤必需）**。距城中心 (159,233) 约 250 格。
- **大刀（守卫）位置**（map1，共 23 个）：城镇区 (122,230) (128,241) (135,215) (143,204) (144,246) (147,259) 一带。
- **比奇野外刷怪区密布攻击性怪**（Oma/Claw Cat/Tusk Lord/Tiger Snake/Wolf 等），长距行走会
  被围攻；A* 长路保留**头部** 400 格分段走。

## 供给链

- 可见供给 NPC：接近（≤2 格）→ `C.NPCCall` → BuySell 页按需购买（药水 x20 / 回城卷 x3 /
  道士护身符 x50）。缺护身符的道士优先选 Lennard（唯一本图护符商）。
- 护身符（召唤必需）：**必须装备到符槽**（服务端 `UseAmulet` 要求 `Equipment[Amulet]`
  且 Shape=0）；背景层 `TryEquipAmulet` 自动穿戴。
- 无可见 NPC：走向 NPC 出生 region 内**可走点**（60s 缓存防目标抖动；店 interior 在
  客户端地图不可走，单点 region 就近 10 格找可走点兜底）。
- 卖货：非精英装备/矿/肉按商店 Types 卖出。

## 移动可靠性（多层自愈）

1. **服务端拒收检测**：朝 `_rejectTracker[goal]` 步格走 >2.5s 位置不变 → 该格拉黑
   （`BotPathfinder.RuntimeBlocked`，按目标追踪，多调用方交替不重置计时）→ A* 绕行。
2. **活物避让**：`ChooseWalkDirection` 优先选"静态可走且无玩家/怪/NPC 占位"的邻格；
   `CellOccupied` 跳过死亡怪（S.ObjectDied 的尸体仍在字典里）。
3. **破围**：5+ 急怪围身 → 先钻空格；8 邻全堵 → 砍最近怪开路（真实玩家行为）。
4. **死亡自愈**：`S.ObjectDied` → 自动 `C.TownRevive` 回城。
5. **A* 失败节流**：8s 内不重算，期间 8 向避障贪心直走（服务端会纠正撞墙）。

## 运行


```bash
dotnet build BotRunner/BotRunner.csproj -m:2
dotnet BotRunner/bin/Debug/net10.0/BotRunner.dll BotRunner.single.json   # 8 bot
```

日志统计（验收口径）：

```bash
# 召唤练习 ≥20
... | grep "train: summon"
# 组队 ≥1 队
... | grep "group: formed"
# 换装 ≥3
... | grep -i "equip"
# 买药 ≥5
... | grep "shop: buy"
# 卡死自愈 ≥1
... | grep -E "server-blocked|breakout"
```
