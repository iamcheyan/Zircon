# 讨论记录 13：M4 周围物体——怪物、NPC、物品从"数据"到"画面"

> 日期：2026-08-06
> 关联：讨论 12 让玩家能走路；本篇让玩家周围的世界**有活物**：服务端广播的怪物（鸡/猪/鹿/守卫）、NPC（洛依/艾米等）真正画在地图上，位置随玩家移动、随地图滚动。
> 结论：M4 完成。27 个对象（22 怪物 + 5 NPC）全部正确渲染，0 解码失败、0 异常、0 断连；窗口截图确认怪物/NPC 在正确格子、地形全屏、玩家居中。

---

## 1. 目标与数据链路

```
服务端定时广播 (玩家周围 12 格内的对象)
  → S.ObjectMonster / S.ObjectNPC / S.ObjectItem   (对象出现)
  → S.ObjectMove / S.ObjectTurn                     (对象移动/转向)
  → S.ObjectRemove                                  (对象消失)
  → 客户端 ObjectRenderer: 怪物帧表 + Zl 图库帧 → 画在地图格子上
```

客户端三个渲染器分工：

| 渲染器 | 绘制内容 | 数据源 |
|---|---|---|
| `MapView` | 地形格子（背景/中层/前景三层） | `.map` + Tilesc/Tiles 图库 |
| `PlayerRenderer` | 玩家自己（身体/头/武器分层） | StartInformation + 装备 |
| `ObjectRenderer` | 周围怪物/NPC/物品（通用三合一） | ObjectMonster/NPC/Item 包 |

## 2. 本轮的三个真 bug（按发现顺序）

### Bug 一：突发包在订阅前被丢弃 → 进游戏一片空白

**现象**：日志显示 27 个 `ObjectMonster/NPC` 包都"入队"了，但 `subscribers=0`，事件没人接。

**根因**：`NetworkManager` 是 autoload，`_Process` 在主线程**先于**场景节点运行；`StartGame` 成功后同帧内后续的 75 个对象包**全部在 `GameScene._Ready` 完成订阅之前**被 `Process` 处理掉了。M3 的 `ObjectMove` 能用，是因为玩家移动发生在进游戏**之后**（订阅早已生效）。

**修复**：`ServerConnection` 给每个对象事件加**积压队列**（`PendingMoves/Monsters/NPCs/Items/Removes/Turns`），`Process` 里**双发**（入队 + 触发事件）；`GameScene._Ready` 订阅后调 `DrainPendingObjects()` 一次性排空（顺序：Moves→Turns→Monsters→NPCs→Items→Removes）。`_Ready` 之后事件接管实时包，队列不再读，无重复。

### Bug 二：BcnDecoder 越界 → 非 4 倍数尺寸图片解码崩溃

**现象**：部分怪物帧（如 w=16,h=50 的 Dxt1）解码抛异常，`ObjectRenderer` 吞掉后该对象整帧不画。

**根因**：Zl 的 DXT 数据按 `floor(w/4)×floor(h/4)` 块存储（`GetDataSize` 整数除法），而 `BCnEncoder.DecodeRaw` 按 `ceil` 块校验 → 尺寸非 4 倍数的图片 `len < expectBytes` 越界。

**修复**：`BcnDecoder.Decode` 解码前把缓冲补零到 `ceil(w/4)×ceil(h/4)` 块大小（Bc1=8 字节/块，其余 16 字节/块），边缘块是垃圾数据但能正常解码；视觉影响仅 1–3 像素边缘。修复后全量 0 解码失败。

### Bug 三：`LoadPlayerMap` 把刚排空的对象全清了 → 对象画完即消失

**现象**：红框调试证明对象确实创建、`_Draw` 也执行了，但**只有 1 个对象落在可见区域**；打印 `UpdateObjectPositions` 发现 `objects=0`。

**根因**：`_Ready` 里 `DrainPendingObjects()` 先把 27 个对象加进字典，紧接着 `ShowStartGameResult → LoadPlayerMap()` 里的"换图清空"逻辑（`_objects.Clear()`）把**刚排空的对象**全 `QueueFree` 了——首帧画完后全消失。

**修复**：`LoadPlayerMap` 增加 `clearObjects` 参数——首次进图（`ShowStartGameResult`）传 `false`，真正换图（`OnMapChanged`）才清空。

### 附带修复：视野范围随窗口自适应

`ViewRangeX/Y` 原来是固定 12×15（为 headless 64×64 视口设计），窗口 1492×1940 下地形只画左上角一小块、其余是灰色。`GameScene` 每帧按视口尺寸重算：`VRx = ⌈W/96⌉+1, VRy = ⌈H/64⌉+1`（窗口下 17×32），灰色区从 ~90% 降到 0.2%。

## 3. 调试方法论（红框定位法）

对象位置错乱时没有猜坐标，而是给每个 `ObjectRenderer` 画一个临时 48×32 调试框（红→绿），窗口自动截图后用脚本统计框的数量与位置：

```
红色像素数=7 → 只有 1 个对象落在屏内   (对象被清空, 只剩残留)
绿色像素数=3653, 23 个框              (清空修复后, 对象全部到位)
```

一次截图同时回答"画没画、画在哪、画了几个"三个问题。

## 4. 验证

### headless 回归

```
[Game] 添加物体: Monster 'Chicken' ObjectID=150 Cell=(143,233)
[Game] 添加物体: Monster 'Guard'  ObjectID=5   Cell=(144,246)
[Game] 添加物体: NPC 'Amy' ObjectID=20 Cell=(142,223)
...共 25~27 个对象(服务器刷怪有随机性), 5 个 NPC 稳定出现
[BcnDecoder] 解码失败: 0 次
Exception/异常: 0
断连/Disconnect: 0
```

### 窗口验证（自动截图 + inspect_image）

- 23 个调试框每个都对应一个精灵：鸡、猪、鹿、守卫、NPC（洛依等）✅
- 玩家 TestHero 居中无框（PlayerRenderer 正常）✅
- 地形全屏（沙地、草地、木车、栅栏、土路），灰色未加载区从 ~90% → 0.2% ✅

## 5. 验收标准

| 项 | 结果 |
|---|---|
| ObjectMonster/NPC/Item 创建与渲染 | ✅ 25~27 对象, 0 解码失败 |
| 对象屏幕定位正确（红框/绿框验证） | ✅ 23 框全部对应精灵 |
| 突发包不丢（订阅前积压 Drain） | ✅ |
| 视野随视口自适应、地形全屏 | ✅ 灰区 0.2% |
| 对象随地图滚动（与玩家同坐标公式） | ✅ |
| 回归：异常 0、断连 0 | ✅ |

---

**下一步**：M5（可选）——攻击动画、魔法特效、怪物血条；以及物品包（服务器当前地面无物品，`ObjectItem` 分支已写好待真实数据）。
