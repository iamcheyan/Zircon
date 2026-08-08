# Zircon 客户端技能系统全面分析与修复方案

本文档对 Zircon 客户端中 **所有技能体系**（包括远程投射物技能、瞬发/目标落雷技能、地表/范围持续技能、身体挂载/Buff技能以及链式技能）的实现机制进行了全盘梳理与深入审计，总结了视觉表现、播放动画、碰撞截停与对齐上的全部缺陷，并提供了统一且完备的修复方案。

---

## 一、 全技能体系分类与问题审计汇总

客户端所有 174 种魔法技能在原版 C# 架构下共分为 **5 大特效播放类别**。当前 Godot 移植版在不同类别中存在不同的缺陷表现：

| 技能类别 | 典型代表技能 | 原版 C# 实现机制 | Godot 移植版当前缺陷 |
| :--- | :--- | :--- | :--- |
| **1. 远程投射物技能** | 火球术, 灵魂火符, 冰箭术, 毒药弹道, 怪物飞斧/箭矢 | 创建 `MirProjectile`，从施法者 `Origin` 插值移动向目标；到达后触发 `CompleteAction` 创建 `Impact` 并播放音效。 | ① 无目标时 `flyPast` 强制跳过 `CompleteAction`，火球飞出屏幕不消亡且不爆炸。<br>② `GameScene` 中 `destCells` 与 `targets` 重复生成双重火球。<br>③ 锚点偏在脚底，未对齐 `Magic.Zl` 内置 `OffSetX/Y`，导致脱手偏下。<br>④ 收到包立即生成，未等待抬手挥出帧。 |
| **2. 瞬发 / 目标落雷技能** | 雷电术 (`ThunderBolt`), 疾光电影, 地狱火, 冰咆哮, 冰霜雪雨 | 不创建投射物，直接在 `AttackTargets` 或 `MagicLocations` 位置挂载 `MirEffect` (或 `SpawnImpactTarget`) 播放特效。 | ① 目标死后/节点无效时直接丢弃特效，未在地图目标坐标保底播放。<br>② 方向性瞬发技能（如疾光电影、地狱火）方向偏转基线计算错误。<br>③ 夜间局部照明 (`FrameLight`) 未生效。 |
| **3. 地面 / 范围持续技能** | 火墙 (`FireWall`), 魔法阵, 冰霜地表 | 创建 `DrawType = Floor` 的持续 `MirEffect`，指定 `Loop = true` 或较长 `DelayMs`，挂载在地图格子中。 | ① Z轴排序错乱：地面烟雾/火墙层级压过了角色或被地砖盖住。<br>② 持续时间结束时未正确触发清理。 |
| **4. 身体挂载 / Buff / 恢复技能** | 魔法盾, 治愈术, 幽灵盾, 神圣战甲术 | 创建 `Target = player` 的 `MirEffect`，每一帧实时同步 `Position = Target.Position`（跟随角色移动）。 | ① 曾因减去 `32px` 导致魔法盾浮在头顶（已修复，但部分技能仍有偏移）。<br>② 多段循环 Buff 特效在状态消除时没有及时被 `QueueFree` 清除。 |
| **5. 链式 / 连线技能** | 链式闪电 (`ChainLightning`), 恶魔之铃 | 使用 `MirLineEffectNode` 按两点间距分节实时网格计算并渲染链条。 | 链条起止锚点偏离了施法者与目标的手部/胸口高度。 |

---

## 二、 核心缺陷深度剖析

### 1. 远程投射物类（火球术/火符等）三大致命问题

#### A. 判定条件漏洞与 `CompleteAction` 被跳过
在 `MirProjectileNode.cs` 中：
```csharp
bool flyPast = _targetNode == null && _target == null && !Explode;
```
只要没有传入 `Node2D` 实体（即使有地图目标坐标 `toX, toY`），`flyPast` 就为 `true`。当飞行时间达到 `duration` 时：
```csharp
if (elapsed >= duration)
{
    if (flyPast && IsProjectileVisible())
    {
        QueueRedraw();
        return; // 直接 return，跳过了下面的 CompleteAction?.Invoke()！
    }
    ...
}
```
这导致投射物到目标点后既不停止、也不播放爆炸动画，而是穿过目标飞出屏幕。

#### B. `GameScene.cs` 双重投射物生成
`OnObjectMagic` 广播处理时，同时在 `destCells` 循环和 `targets` 循环里分别调用了 `SpawnProjectile` 与 `SpawnProjectileTarget`，导致一次施法生成了两个火球（一个带目标，一个不带目标），视觉效果严重紊乱。

#### C. 手部锚点偏下与时间轴脱节
* **空间**：`ComputeEffectScreenPos` 锚点在脚底/格子基线，没有融合 `Magic.Zl` 官方调优的 `OffSetX/Y` 和手臂高度，火球从腰/脚部发空。
* **时间**：网络包一到立刻生成火球，此时玩家施法抬手动画刚开始，手还没挥完火球已飞走。

---

### 2. 瞬发落雷与 Buff 类技能缺陷

* **目标丢失抛异常/丢特效**：当目标怪物在收到魔法广播瞬间死亡并被销毁时，`GetMagicTargetNode` 返回 `null`，导致原本应该在怪物死前脚下播放的受击落雷特效被直接丢弃。
* **地面层级 (`Floor`) 错位**：火墙等地面特效应该落在背景地砖之上、角色脚底之下。如果 `DrawType` 未严格设为 `Floor` 并赋予独立的 `ZIndex`，会出现覆盖角色的情况。

---

## 三、 全技能统一修复方案与实施步骤

### 第一阶段：通用投射物节点 (`MirProjectileNode.cs`) 彻底修复

1. **修正 `flyPast` 截停与爆炸条件**：
   * 只有在既无目标实体、又无地图落点目标，且配置为无限穿透出屏的弹道时，才允许 `flyPast`。
   * 当 `elapsed >= duration` 且到达了指定的 `_targetCellX, _targetCellY` 时，**必须强制执行 `CompleteAction?.Invoke()`**（生成爆炸 `SpawnImpact` 并播放击中音效），随后 `QueueFree()` 销毁节点。

2. **修正起点手部 Anchor 与偏移量**：
   * 起点坐标加上玩家手臂高度偏置（约 `Vector2(0, -40)`），使火球生成位置完美对齐 `Magic.Zl` 的 `OffSetX/Y`。

3. **加入起手挥出时间轴延迟 (`StartDelayMs`)**：
   * 根据施法动作动画帧长，为投射物增加 `100~150ms` 的启动延迟，确保“手挥到最高点”时火球才亮起飞出。

---

### 第二阶段：场景技能分发器 (`GameScene.cs`) 重写

1. **消除重复生成**：
   * 改造 `OnObjectMagic` 派发逻辑。判断技能定义：如果有目标实体 `targets`，只触发一次 `SpawnProjectileTarget`；对于无目标的地点技能，只在 `destCells` 中触发一次 `SpawnProjectile`。

2. **落雷/瞬发技能保底渲染**：
   * 当 `GetMagicTargetNode(tid)` 为 `null`（目标已死/失效）时，自动降级回退到目标最后记录的地图坐标 `cellX, cellY` 播放 `SpawnImpact`，确保落雷/受击动画不丢失。

---

### 第三阶段：所有技能类别全面覆盖校验

1. **地面技能 (`Floor`) 层级统一**：
   * 确保 `FireWall` 等地面技能的 `DrawType` 设置为 `Floor`，并统一设置 `ZIndex = RenderOrder.FloorEffect`（介于地砖与角色之间）。

2. **身体挂载 Buff Follow 逻辑优化**：
   * 统一所有 `SetupTarget` 的节点跟随，实时同步 `Position = Target.Position`（对齐对象基线），保证魔法盾、幽灵盾不发生上下漂移。

3. **链式技能 (`MirLineEffectNode`) 锚点对齐**：
   * 调整链条两端 `_source` 与 `_target` 的 Anchor 偏移，从中心胸口高度连线，避免链条贴地。

---

## 四、 预期修复效果

完成上述统一修复后：
1. **火球术 / 灵魂火符 / 毒药**：从手上随挥手动作飞出，精准飞行并打中目标，在目标身上停止并播放爆炸动画与音效。
2. **雷电术 / 冰咆哮**：目标死后落雷特效依然会在怪物尸体/地上正常落下播放。
3. **火墙 / 地面阵法**：平铺在地面上，层级正确，不再掩盖角色。
4. **魔法盾 / Buff**：紧贴角色身体随动，不浮空、不滞后。
