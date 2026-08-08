# 技能施法轨迹分类与 Godot 迁移

## 结论

原版并不是“释放技能后在玩家身上播放一个特效”。客户端发送 `C.Magic` 的方向、目标和地图落点，服务端的 `MagicObject.MagicCast` 决定 `S.ObjectMagic` 中的 `Locations` 与 `Targets`，客户端再按这两个结果播放轨迹。

当前 Godot 的首要错误是：没有锁定目标时仍把玩家格作为 `Location`，所以火球、冰箭等没有目标时会被服务端判定为落在施法者脚下。其次，所有技能都被压缩成了“一个落点特效/一个通用爆炸”，没有区分投射、地面、范围和延迟效果。

## 原版协议与输入语义

| 输入 | `Target` | `Location` | `Direction` |
|---|---:|---|---|
| 鼠标指向空地 | 0 | 鼠标所在地图格 | 玩家格到鼠标格 |
| 点击并锁定生物 | 目标 ObjectID | 目标格；范围技能仍使用鼠标格 | 玩家格到目标格 |
| 自身/无目标技能 | 0 | 原版技能自己的规则 | 鼠标方向或玩家当前方向 |

服务端返回的 `Locations` 是地面格/路径格，`Targets` 是对象命中结果。两者不能互相替代：有 `Locations` 的直线技能必须从 `CurrentLocation` 移动到每个格；有 `Targets` 的锁定技能必须以目标对象当前坐标为终点。

## 原版技能轨迹分类

### 1. 方向直线投射

火球、冰箭、雷电球、风刃、火焰反弹、冰刃、雷电术、冰龙等。表现为施法动作 → 投射物从 `CurrentLocation` 沿方向飞行 → `MapTarget` 或目标对象处命中 → 命中特效/伤害。

代表资源：FireBall `420 -> 580`、IceBolt `2700 -> 2860`、LightningBall `3070 -> 3230`、GustBlast `430 -> 590`、FireBounce `1640 -> 1800`、IceBlades `2960 -> 2970`。

### 2. 锁定目标投射

投射物终点是 `Target` 对象，而不是释放瞬间缓存的玩家格。目标移动时，表现层应使用服务端广播的目标或最终命中格；不能在创建特效时直接在玩家脚下爆炸。

### 3. 鼠标落点地面技能

雷击、火墙、焦土、龙卷风、冰风暴、陨石、冰雨等。它们使用鼠标地图格或服务端返回的一组 `Locations`，没有“从玩家飞到目标”的单一投射物；多格技能还要按路径距离延迟每个落点。

### 4. 即时目标/范围技能

电击、排斥、旋风、链式效果及部分驱散/攻击技能直接在目标或目标集合上产生效果。仍然必须等待 `Targets`/`Locations`，不能统一套用火球轨迹。

### 5. 自身、增益与召唤技能

护盾、隐身、传送、镜像、召唤、飓风蓄力等以施法者为中心，播放对应施法动作和自身特效；它们不应生成方向投射物。

### 6. 多段与延迟技能

焦土、冰雨、陨石、链式雷击、冰龙等可能返回多个落点，原版用 `DelayMagic`/按距离的 `StartTime` 排列效果。Godot 应保留每个落点的顺序和延迟，而不是把结果合并成一次爆炸。

## Godot 迁移规则

1. `UseMagicSlot` 必须把鼠标屏幕坐标逆变换为地图格，并计算玩家到鼠标/目标的方向。
2. `ObjectMagic` 先播放施法动作，再分别处理 `Locations`、`Targets`。
3. `Projectile` 只用于原版确实存在的投射技能；起点固定为 `CurrentLocation`，终点为服务端格或目标对象。
4. `Impact`/地面特效使用原版资源、颜色、Blend、Opacity 和延迟；资源缺失只能记录诊断，不能静默换成通用爆炸。
5. `MagicEffectTable` 需要按上述分类继续补齐 Wizard 201–246、Taoist/Archer/Assassin 和怪物技能；每个未覆盖的技能都要在日志中显示类型和落点数据。

## 当前修复范围

本次先修复最直接的协议错误：鼠标落点、目标落点和方向的发送；并保留现有 `Locations/Targets` 投射渲染链。随后以本表为基准扩展效果资源和延迟/多段表现，直到静态技能审计不再出现未分类技能。

## 逐技能 A/B 核对表（原版 `Client/Models/MapObject.cs` Spell 分支 ↔ Godot `MagicEffectTable`）

已按原版 Spell 主开关（~770–2200 目标/落点特效）与施法自身子开关（~3770–4500）逐技能核对。以下行均为已比对条目；未列出的技能条目已在表内确认与原文一致。

| 技能 | 施法动作(自身) | 发射点 | 直线/追踪/落点 | 飞行时间 | 爆炸帧 | 方向 | 阻塞移动 | 结束状态 |
|---|---|---|---|---|---|---|---|---|
| FireBall | 1820/8/70ms `Target=this` | `CurrentLocation` | 直线投射→落点 | `Distance(p1,p2)ms` (Chebyshev) | 420/5/100ms | 每帧重算 `DirectionFromPoint` | 不阻塞 | `Explode=true` 到达即结束；音效 FireBallStart/Travel/End |
| LightningBall | 同 cast-self 1820 族 | `CurrentLocation` | 直线投射→落点 | 同上 | 3070/5/100ms | 同上 | 不阻塞 | 同 FireBall |
| IceBolt | 2620/6/80ms（IceAura/IceDragon 同族） | `CurrentLocation` | 直线投射→落点 | 同上 | 2700/3 | 同上 | 不阻塞 | 到达结束 |
| GustBlast | 1820 族 | `CurrentLocation` | 直线投射 | 同上 | 430/5/100ms | 同上 | 不阻塞 | 到达结束 |
| FireBounce/AdamantineFireBall | 1560/9/65ms | `CurrentLocation` | 直线投射 | 同上 | 1640/6 → 命中 1800/10 | 同上 | 不阻塞 | 命中结束 |
| MeteorShower | 1560/9/65ms | 落点 | 落点陨石（无玩家→落点直线） | 同上 | 1640/6 → 1800/10 | 同上 | 不阻塞 | 落点 Explode |
| **Asteroid** | 无自身特效 | 落点+(4,-10) | **仅落点直落**，`Skip=0`，对 Targets **无任何魔法特效** | 同上 | 1320/8 (MapTarget) | 直落 | 不阻塞 | 落点 Explode |
| FireStorm | 940/10/60ms | 落点 | 地面 AoE | — | 950/7 | — | 不阻塞 | 落点特效 |
| IceStorm/DragonTornado/LightningWave/IceDragon/Cyclone/GreaterFrozenEarth | cast 条目一致 | 落点/自身 | 地面 AoE | — | 表内一致 | — | 不阻塞 | 落点特效 |
| ChainLightning | 1430/12/50ms + 470/10 MagicEx2 链 | 目标链 | 链式（服务端 Targets 顺序） | — | 链段各自 | — | 不阻塞 | 链段依次命中 |
| **LightningBeam** | 1970/10/30ms `Target=this` | **施法者** | **光束 1180/4/100ms 挂在施法者身上、每个 MagicLocation 各播一次**，方向=施法者格→格；目标上无特效 | — | 1180/4 | `DirectionFromPoint(施法者,格)` | 不阻塞 | 光束播完即止 |
| **ThunderBolt/ThunderStrike** | **1430/12/50ms 自身上段**（light 10/35） | 目标/落点 | 落点命中 | — | **1450/3/150ms**（light 150/50） | — | 不阻塞 | 命中结束 |
| Heal | 660/10/60ms | 目标 | 即时目标 | — | 610/10、670/7 双段 | — | 不阻塞 | 目标特效 |
| MassHeal | 660 族 cast | 目标群 | 即时目标群 | — | 610/10、670/7 | — | 不阻塞 | 目标特效 |
| Shuriken (`RangeAttack`) | 挥击动作 | 施法者格 | 每目标一枚 `1270/3/100ms` MagicEx | `Distance*2`（`Delay=2` 原始倍率） | 1270/3 | `Has16Directions=true` | 不阻塞 | `Explode=true` 到达结束，**Blend=false** |

### 已核对的公式（与原版 `MirProjectile.Process()` 逐行一致）

- 坐标换算：`p = (x, y/32*48)`——Godot 本地地图格空间 == 原版 48/32 px，换算后直接进入公式。
- 飞行时间：`duration = Functions.Distance(p1, p2)`（**Chebyshev**：`max(|dx|,|dy|)`，与共享库 `Functions.Distance` 一致）；`if (Delay > 0) duration *= Delay`（原始倍率，Shuriken `Delay=2` → 2 倍慢）。
- 到达判定：`location == Origin` 立即结束（`duration == 0` 分支）；`t = elapsed/duration` 每帧更新位置。
- 爆炸帧：`GetProjectileFrame` = `elapsed % total` 走 `Delays`（与原版 `GetFrame` 相同）。
- 方向：每帧 `DirectionFromPoint`/`Direction16`；`!Has16Directions` 时 `Direction16 /= 2`。
- 结束状态：`Target==null && !Explode` 且精灵仍在屏内 → **继续穿屏飞行**，出屏才结束；否则在 `duration` 处结束。Godot 侧 `MirProjectileNode` 用 `flyPast` 标志复现。
- 特效帧：`DrawFrame = FrameIndex + StartIndex + (int)Direction * Skip`（`Skip` 默认 10），与原版 `MirEffect.Draw` 相同。

### 本次 A/B 修复（均有原版出处）

1. **飞行时间回归**：`6bc6bf3` 把原版公式换成 `distancePx*1.5 + Delay/10 + 50ms 下限`，飞行慢 ~1.5 倍；已还原为 `Functions.Distance` + 原始 `Delay`（`MirProjectileNode._Process`）。
2. **穿屏飞行**：无目标且非 Explode 的投射物（火球落点弹道等）到点后继续飞行直到出屏，不再截停在落点。
3. **ThunderBolt/ThunderStrike**：补上原版缺失的 1430/12/50ms 施法自身段；命中段 1450/3/150ms 挂落点。
4. **LightningBeam**：光束移到施法者身上按 `MagicLocation` 逐格播（`CastEffect.SourcePerLocation`），目标上不再出现错误光束（`NoTargetVisual=true`）。
5. **Asteroid**：关闭 targets 循环的虚假追踪弹（`NoTargetVisual=true`），只保留落点直落陨石。
6. **Shuriken**：`OnObjectRangeAttack` 补上每目标一枚 1270/3/100ms 飞行物（Blend=false、Explode、Delay=2、16 方向），原端只播挥击/受击。

### 已知未阻塞项

- MassHeal 原版命中光效 (40,60)，Godot `ImpactDef.FrameLight` 默认 10——不影响轨迹，留待视觉打磨。
- 阻塞移动：原版投射物本身不阻塞移动；施法阻塞属服务端 `CanMove`/动作状态机，本表不涉及。

## 验证

- `--projectile-audit`（Vulkan 窗口化）：`PASS duration=194-197ms≈192ms`（3 次运行），
  travel≈200px——(0,0)→(4,2) 目标格换算 `(192,96)`，Chebyshev 期望 192ms；旧公式
  （304ms）会 FAIL。截图在 CompleteAction 内异步保存，避免读回阻塞污染时长测量。
- `--render-audit`、`--blend-audit`（材质 9/9）回归通过。
- 原版客户端无法在本机运行（P-002，Windows/SharpDX），技能 A/B 为代码级比对。
