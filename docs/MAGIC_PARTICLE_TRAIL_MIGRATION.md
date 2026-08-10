# 魔法飞行物粒子拖尾系统迁移指南

> 本文档的证据全部来自对仓库源码的逐行核对与对实际素材文件（`Debug/Client/Data/*.Zl`）的帧元数据解析。
> 最后核对时间：2026-08-10。

## 问题现象

Godot 客户端移植自原版 Windows 客户端（`Client/`）。用户报告：

- **火球术 (FireBall)**：能看到飞行轨迹 ✅
- **冰魂魄 (IceBolt)**：只能看到命中爆炸，**看不到飞行轨迹** ❌
- **爆裂火焰类**：只能看到部分效果 ❌

伤害正常，纯视觉缺失。

## 根因

**Godot 客户端没有移植原版的「飞行物粒子拖尾系统」。**

原版每个飞行物（`MirProjectile`）在构造时可传入一个 `ParticleEmitter` 类型，每帧沿飞行路径生成粒子，形成拖尾。冰魂魄的轨迹视觉**主要靠 120 个冰晶粒子**表现，主 sprite 只有 32×64 的小冰弹——没有粒子就基本看不到轨迹。火球术的主 sprite 是 128×128 的大火球，所以没有粒子也能看到。

## 证据链

### 一、原版粒子系统集成点（源码行号）

**`Client/Models/MirProjectile.cs:20-33`** — 构造函数接收 `Type particleEmitter` 参数，受 `Config.DrawParticles` 门控：

```csharp
public MirProjectile(int startIndex, int frameCount, TimeSpan frameDelay, LibraryFile file,
    int startlight, int endlight, Color lightColour, Point origin, Type particleEmitter = null)
    : base(...)
{
    ...
    if (Config.DrawParticles && particleEmitter != null)      // :29
    {
        _particleEmitter = (ParticleEmitter)Activator.CreateInstance(particleEmitter, this);  // :30
        GameScene.Game.MapControl.ParticleEffects.Add(_particleEmitter);                     // :32
    }
}
```

**`Client/Models/MirProjectile.cs:87-91`** — 每帧更新粒子位置（关键：传入的是**屏幕坐标 + 当前帧的图库 OffSet**）：

```csharp
if (_particleEmitter != null)                                  // :87
{
    Point offset = Library.GetOffSet(DrawFrame);               // :89
    _particleEmitter.SetLocation(Direction16, DrawX + offset.X, DrawY + offset.Y);  // :91
}
```

**`Client/Models/MirProjectile.cs:129`** — 移除时停止粒子生成：

```csharp
public override void Remove()
{
    _particleEmitter?.StopGeneration();   // :129
    base.Remove();
}
```

**粒子发射器的宿主容器**：
- `Client/Models/MirEffect.cs:45` — `public ParticleEmitter _particleEmitter;`（基类字段）
- `Client/Scenes/Views/MapControl.cs:176` — `public List<Models.Particles.ParticleEmitter> ParticleEffects = new();`
- `Client/Scenes/GameScene.cs:1132-1133` — 每帧 `MapControl.ParticleEffects[i].Process();`
- `Client/Scenes/Views/MapControl.cs:469` — 每帧绘制 `ParticleEffects`
- `Client/Envir/Config.cs:69` — `public static bool DrawParticles { get; set; } = false;`（**默认关闭**，运行时由选项开关打开）

**粒子类结构**（`Client/Models/Particles/`）：
- `Particle.cs` — 单个粒子：位置、速度、角度、缩放（Scale/ScaleRate/MaxScale）、TTL、淡出（Fade/FadeRate）、透明度、16 方向、纹理帧号；`Update()` 推进物理，`Draw()` 用 `Library.DrawBlendCentered(..., ImageType.Image, ...)` 绘制。
- `ParticleType.cs` — 粒子类型模板：`MaxCount`、`SpawnFrequency`、`Textures`（ProgUse 帧索引列表）、`Color`、`CreateParticle(emitterLocation, direction, angle)` 工厂。
- `ParticleEmitter.cs` — 发射器：持有 `ParticleTypes` 列表与 `CenterPoint[16]`（16 方向的发射点偏移）；`SetLocation(direction16, x, y)` 按方向选 CenterPoint；`Process()` 按 `MaxCount`/`SpawnFrequency` 生成粒子并 `Update()` 每个粒子，`Remove` 时回收。

### 二、遍历结果：61 处 `new MirProjectile`

用脚本按 `case MagicType.X:` 分组 + `break;` 结束逐行归并，遍历 `Client/Models/MapObject.cs`（共 6160 行）：

- **全文件共 61 处 `new MirProjectile`**，分布在 3 个 `switch` 内：
  - 施法特效 switch（`case MirAction.Spell:` 内嵌 `switch (MagicType)`，行 **771–3202**）：**59 处**
  - 攻击动作 switch（行 **3246–3255**，`case MirAction.RangeAttack:` 行 3287）：**1 处**（HundredFist，行 3308）
  - 另一施法 switch（行 **3621–5136**）：**1 处**（DoomClawSpit，行 5114）
- 涉及 **38 个 MagicType**（注：不是 39——61 处中 36 个类型在施法 switch 内、HundredFist 与 DoomClawSpit 各 1 个）。
- 其中**带粒子拖尾的构造共 10 处**，对应 **4 个拖尾类、7 个技能**：

| 构造行号 | MagicType（case 组） | 拖尾类 | 主 sprite（StartIndex, 帧数） |
|---|---|---|---|
| 845, 855 | `FireBall` | `FireballTrail` | Magic.Zl 420, 5 帧 |
| 930, 940 | `IceBolt` | `IceBoltTrail` | Magic.Zl 2700, 3 帧 |
| 972, 982 | `GustBlast` | `GustTrail` | MagicEx.Zl 430, 5 帧 |
| 1046, 1056 | `AdamantineFireBall` + `MeteorShower` + `FireBounce`（共用一个 case 组） | `FireballTrail` | Magic.Zl 1640, 6 帧 |
| 1117, 1129 | `IceBlades` | `IceBladesTrail` | Magic.Zl 2960, 6 帧（`Skip=0, BlendRate=1F`） |

> **对最初"5 个技能带粒子"的修正**：`AdamantineFireBall` 与 `MeteorShower` 与 `FireBounce` 共用同一个 `case` 组（MapObject.cs:1040-1042 的三个 `case MagicType.X:` 标签连写），所以 `FireballTrail` 实际覆盖 **4 个技能**。带粒子的技能总数是 **7**（FireBall、FireBounce、AdamantineFireBall、MeteorShower、IceBolt、IceBlades、GustBlast），构造点 10 处。

### 三、4 个拖尾类的完整粒子参数（逐行核对）

全部纹理来自 `LibraryFile.ProgUse`（`ProgUse.Zl`）。

#### FireballTrail（`Client/Models/Particles/Spells/FireballTrail.cs`）

| 粒子类型 | MaxCount | SpawnFrequency | Textures | Color | opacity | scale / rate / max | ttl | fade / fadeRate |
|---|---|---|---|---|---|---|---|---|
| `EmberParticle` | **120** | 5ms | 520,521,522,523 | `Globals.FireColour` | 0.5 | 0.3 / 0 / – | 200–300ms | true / 0.01 |
| `SmokeParticle` | **5** | 15ms | 530 | DimGray | 0.05 | 0.3 / 0 / – | 100–200ms | false |

`ParticleTypes = [EmberParticle, SmokeParticle]`；`CenterPoint[16]`（声明 :86，16 个点，如 (34,31)、(51,32)、…(46,32)）。

> **对最初"火球术只有 5 个烟雾"的修正**：FireballTrail 实际是 **120 火星 + 5 烟雾**。火球术在 Godot 能看到轨迹纯粹因为主 sprite 128×128 够大，粒子只是点缀——这一结论不变。

#### IceBoltTrail（`Client/Models/Particles/Spells/IceBoltTrail.cs`）

| 粒子类型 | MaxCount | SpawnFrequency | Textures | Color | opacity | scale / rate / max | ttl | fade / fadeRate |
|---|---|---|---|---|---|---|---|---|
| `IceParticle` | **120** | 5ms | 520,521,522,523 | `Globals.IceColour` | 0.5 | 0.15 / 0 / – | 200–500ms | true / 0.01 |
| `SmokeParticle` | **20** | 15ms | 530 | LightGray | 0.5 | 0.1 / 0 / – | 50–100ms | true / 0.5 |

`ParticleTypes = [IceParticle, SmokeParticle]`；`CenterPoint[16]`（声明 :87，16 个点，(9,13)、(22,12)、…(12,17)）。
`IceParticle` 的 `angularVelocity = ±4 rad/s`（旋转的冰晶碎片）——这是拖尾"飘散"观感的主要来源。

#### IceBladesTrail（`Client/Models/Particles/Spells/IceBladesTrail.cs`）

| 粒子类型 | MaxCount | SpawnFrequency | Textures | Color | opacity | scale / rate / max | ttl | fade / fadeRate |
|---|---|---|---|---|---|---|---|---|
| `EmberParticle` | **150** | 10ms | 520,521,522,523 | RoyalBlue | 1.0 | 0.2 / 0 / – | 500–700ms | true / 0.5 |
| `SmokeParticle` | **10**（源码注释原为 20） | 70ms | 530 | CornflowerBlue | 0.7 | 0.2 / 0.09 / **1.7**（持续放大） | 300–400ms | true / 0.1 |

`ParticleTypes = [EmberParticle, SmokeParticle]`；`CenterPoint[16]` 全为 **(22,15)**（无方向差异）。

#### GustTrail（`Client/Models/Particles/Spells/GustTrail.cs`）

| 粒子类型 | MaxCount | SpawnFrequency | Textures | Color | opacity | scale / rate / max | ttl | fade / fadeRate |
|---|---|---|---|---|---|---|---|---|
| `SmokeParticle` | **10** | 15ms | 530 | `Globals.WindColour` | 0.5 | 0.1 / 0.06 / –（持续放大） | 50–150ms | true / 0.5 |

`ParticleTypes = [SmokeParticle]`（**只有烟雾，无晶粒**）；`CenterPoint[16]`（声明 :51，16 个点，(26,14)、(40,19)、…(27,15)）。

### 四、帧尺寸对比（直接解析 `Debug/Client/Data/*.Zl` 元数据）

从 `.Zl` 文件头的元数据块逐帧读出 `Width/Height/OffSetX/OffSetY`（Godot 端 `GodotClient/Formats/ZlReader.cs` 的 `ZlImage.Read` 同款布局）：

| 素材 | 帧 | 尺寸 | OffSet | 说明 |
|---|---|---|---|---|
| `Magic.Zl` | 420–424 | **128×128** | (0,-86) | 火球术主 sprite（大） |
| `Magic.Zl` | 1640–1645 | **128×128** | (-16,-84)~(-19,-86) | 火弹跳/Adamantine/Meteor 主 sprite（大） |
| `Magic.Zl` | 2700–2702 | **32×64** | (14,-40) | 冰魂魄主 sprite（**小**） |
| `Magic.Zl` | 2960–2965 | **64×32** | (-15,-37)~(17,-25) | 冰刃主 sprite（**小**） |
| `MagicEx.Zl` | 430–434 | **64×128** | (3,-63) | 烈风主 sprite（中） |
| `ProgUse.Zl` | 520–523 | **32×32** | (-24,-16) | 冰晶/火星碎片粒子纹理 |
| `ProgUse.Zl` | 530 | **128×128** | (-24,-16) | 烟雾粒子纹理（绘制时按 scale 缩放，实际很小） |

**推论**：冰魂魄（32×64）、冰刃（64×32）的主 sprite 远小于火球术（128×128），其轨迹视觉完全依赖粒子（冰魂魄 120 冰晶、冰刃 150 晶粒）。粒子缺失时只看到小弹体 + 命中爆炸——正是"只能看到一部分效果，伤害有"。

## 受影响技能完整清单（7 个）

| MagicType | 拖尾类 | 粒子构成 | 主 sprite 尺寸 | Godot 现状 |
|---|---|---|---|---|
| `FireBall` | FireballTrail | 120 火星 + 5 烟雾 | 128×128 大 | ✅ 能看到（主 sprite 够大） |
| `FireBounce` | FireballTrail | 120 火星 + 5 烟雾 | 128×128 大 | ✅ 应该能看到 |
| `AdamantineFireBall` | FireballTrail | 120 火星 + 5 烟雾 | 128×128 大 | ✅ 应该能看到 |
| `MeteorShower` | FireballTrail | 120 火星 + 5 烟雾 | 128×128 大 | ✅ 应该能看到 |
| `IceBolt` | **IceBoltTrail** | **120 冰晶 + 20 烟雾** | **32×64 小** | ❌ **看不到轨迹** |
| `IceBlades` | **IceBladesTrail** | **150 晶粒 + 10 蓝烟** | **64×32 小** | ❌ **很可能看不到** |
| `GustBlast` | **GustTrail** | **10 烟雾** | 64×128 中 | ⚠️ 可能部分缺失 |

> **用户报告的冰魂魄正是这个原因**。冰刃（IceBlades）和烈风（GustBlast）大概率也有同样问题，用户只是没测到。烈风的拖尾只有 10 个烟雾粒子（无晶粒），视觉分量最轻，缺失时观感差异最小。

## 为什么其余 31 个无粒子的飞行物不受影响

38 个有飞行物的 MagicType 中，**31 个原版就没有粒子拖尾**（如 LightningBall、EvilSlayer、ExplosiveTalisman、HundredFist 等）。它们的轨迹只靠主 sprite。部分主 sprite 也很小（如 ExplosiveTalisman 16×32、HundredFist 16×32），但那是原版设计如此，不是 Godot 的回归。

## Godot 端当前状态

### 已有的（正确，不要动）

- `GodotClient/Scripts/MirProjectileNode.cs` — 飞行物主 sprite 渲染，移植了原版 MirProjectile 的位置插值、方向选帧、flyPast 逻辑。**主 sprite 渲染正确**。
- `GodotClient/Scripts/MagicEffectTable.cs` — 7 个技能都配了 `Projectile`（主 sprite 的 LibraryFile/StartIndex/FrameCount/DelayMs 等）。
- `GodotClient/Scripts/GameScene.cs:3881` `SpawnProjectileDefinition` / `:3920` `SpawnProjectileDefinitionTarget` — 创建 `MirProjectileNode` 并配置。
- `GodotClient/Scripts/MapWeatherLayer.cs` — 天气粒子系统已移植（`WeatherParticle` 类 + `GetImageTexture` + `DrawSetTransform`/`DrawTextureRectRegion` 居中绘制，注释明确参考原版 `Particle.DrawBlendCentered`）。**这是本迁移的现成移植先例**。

### 缺失的

1. `GodotClient/Scripts/MirProjectileNode.cs` **没有任何粒子相关代码**。
2. `MagicEffectTable.ProjectileDef`（`MagicEffectTable.cs:139-159`）**没有粒子发射器字段**。
3. `GodotClient/` 下**没有** `Particle.cs` / `ParticleType.cs` / `ParticleEmitter.cs` 的 Godot 移植。
4. `GodotClient/` 下**没有** `Spells/*Trail.cs` 的 Godot 移植。

## 修复方案（5 步）

### 步骤 1：移植粒子基础类

原版：
- `Client/Models/Particles/Particle.cs`
- `Client/Models/Particles/ParticleType.cs`
- `Client/Models/Particles/ParticleEmitter.cs`

移植到 `GodotClient/Scripts/Particles/`（新建目录）。关键改动与依据：

- **渲染**：原版用 `System.Drawing` + DirectX，`Particle.Draw()` 调用 `Library.DrawBlendCentered(TextureIndex, Scale, Color, x, y, Angle, Opacity, ImageType.Image, ...)`。Godot 用 `Node2D` + `DrawTextureRectRegion`（参考 `MapWeatherLayer.cs:181-189` 的写法：`DrawSetTransform(p.Position, p.Rotation, scale)` + 以纹理中心为锚的 `Rect2(-w/2, -h/2, w, h)`），并设 `ZIndex` 与主 sprite 同层（`RenderOrder.ObjectEffect(renderY)`）。
- **纹理获取**：`LibraryCache.Get(LibraryFile.ProgUse).GetImageTexture(frame)`。注意 **`GetImageTexture`（`ZlReader.cs:212`）对应原版 `ImageType.Image`，不做透明键抠除——这是原版粒子的确切语义**，不要用 `GetEffectTexture`（那是 `ImageType.Effect` 的抠黑底路径，用于其他特效）。
- **物理/生命周期**：`Update()` 的位置 += 速度、角度 += 角速度、Scale 增长钳制到 MaxScale、TTL 到期后 Fade 衰减或直接移除——全部保持原版参数。
- **发射器**：`CenterPoint[16]`、`SetLocation(direction16, x, y)`、`Process()` 的生成节流（`MaxCount` + `SpawnFrequency` + `NextSpawn`）与粒子回收照搬。
- **宿主**：原版发射器挂在 `MapControl.ParticleEffects`（屏幕空间列表）。Godot 端**建议把粒子发射器做成 `GameScene` 下的独立 `Node2D`**（与 `MirProjectileNode` 同级），由 `MirProjectileNode._Process` 每帧 `SetLocation`，`GameScene` 每帧 `Process()` + 自动绘制；`MirProjectileNode` 释放时对发射器调用 `StopGeneration()` 并延迟清理。

### 步骤 2：移植 4 个飞行物拖尾

原版 `Client/Models/Particles/Spells/` 的 4 个文件照搬逻辑到 `GodotClient/Scripts/Particles/Spells/`：

- `FireballTrail.cs` → `GodotClient/Scripts/Particles/Spells/FireballTrail.cs`
- `IceBoltTrail.cs` → `GodotClient/Scripts/Particles/Spells/IceBoltTrail.cs`
- `IceBladesTrail.cs` → `GodotClient/Scripts/Particles/Spells/IceBladesTrail.cs`
- `GustTrail.cs` → `GodotClient/Scripts/Particles/Spells/GustTrail.cs`

**保持上文的 MaxCount、SpawnFrequency、Textures（ProgUse 帧索引）、Color、CenterPoint、velocity、ttl、scale、fade 参数逐一不变**——它们决定拖尾的视觉密度与形态，任何"顺手优化"都会改变观感。

### 步骤 3：`ProjectileDef` 加粒子发射器字段

`GodotClient/Scripts/MagicEffectTable.cs` 的 `ProjectileDef`（:139）新增：

```csharp
public Type ParticleEmitter;  // 原版拖尾粒子类型；null = 无拖尾
```

在 `CastEffect` 表里给 7 个技能的 `Projectile` 填上（各技能 `Projectile` 字段的行号：`FireBall` :278、`FireBounce` :330、`MeteorShower` :338、`AdamantineFireBall` :397、`IceBolt` :294、`GustBlast` :302、`IceBlades` :346）：

```csharp
// FireBall :278, FireBounce :330, MeteorShower :338, AdamantineFireBall :397
Projectile = new ProjectileDef { ..., ParticleEmitter = typeof(FireballTrail) },
// IceBolt :294
Projectile = new ProjectileDef { ..., ParticleEmitter = typeof(IceBoltTrail) },
// IceBlades :346
Projectile = new ProjectileDef { ..., ParticleEmitter = typeof(IceBladesTrail) },
// GustBlast :302
Projectile = new ProjectileDef { ..., ParticleEmitter = typeof(GustTrail) },
```

> 用 `Type` + `Activator.CreateInstance` 是原版同构做法；若想避免反射，也可换成 `enum TrailKind` + switch 工厂——两者都行，推荐与原版一致的 `Type`，改造成本最低。
>
> **顺带发现（不在本次迁移范围内）**：`AdamantineFireBall` 的 `Projectile`（:397）在 Godot 表里用 `StartIndex = 420`（火球 sprite），而原版 MapObject.cs:1046 用 `1640`。这是既有的主 sprite 映射差异，与粒子拖尾无关——如需处理请另行评估，**不要在本迁移中顺手改动主 sprite**。

### 步骤 4：`MirProjectileNode` 集成粒子

`GodotClient/Scripts/MirProjectileNode.cs`：

1. 新增字段 `private ParticleEmitter _particleEmitter;` 与 `public Type ParticleEmitterType;`。
2. `GameScene.SpawnProjectileDefinition`（:3881）与 `SpawnProjectileDefinitionTarget`（:3920）在配置 `pn` 时传 `pn.ParticleEmitterType = proj.ParticleEmitter;`。
3. `_Process` 每帧（在原版对应处、即 `Position` 更新之后）调用：
   ```csharp
   if (_particleEmitter == null && ParticleEmitterType != null)
       _particleEmitter = (ParticleEmitter)Activator.CreateInstance(ParticleEmitterType, this);
   _particleEmitter?.SetLocation(Direction16, ...);
   ```
   并将发射器节点 `AddChild` 到与 `MirProjectileNode` 相同的父级（见步骤 5 的坐标说明）。
4. 飞行物完成/释放路径（`CompleteAction` 触发处与 `QueueFree`）调用 `_particleEmitter?.StopGeneration()`，粒子全部过期后回收（参考原版 `Remove()` :129 + `ParticleEmitter.Remove()` 从容器移除）。
5. 门控：原版有 `Config.DrawParticles`（默认 false，运行时开关）。Godot 端若有等价设置项则接入；没有则**默认启用**（否则迁移等于没做）。

### 步骤 5：坐标对应关系（关键，最容易出错）

**两边都是全局屏幕坐标，直接对应**：

- 原版 `MirProjectile.Process()` 的 `DrawX/DrawY` = 已含相机偏移的**绝对屏幕坐标**；传给粒子的点是 `DrawX + Library.GetOffSet(DrawFrame).X, DrawY + GetOffSet(DrawFrame).Y`（当前帧的图库 OffSet 叠加）。
- Godot 的 `MirProjectileNode.Position` = `_cameraFnByCell(...)` = `GameScene.ComputeEffectScreenPos(cx, cy)` = `_mapView.CellToScreen(cellX, cellY, false)`（:9009-9013），同样是**绝对屏幕坐标**；`MirProjectileNode._Draw`（:249-252）用 `img.OffSetX/OffSetY` 把 sprite 摆到位。

因此粒子发射点取：

```csharp
var frame = _frameIndex + StartIndex + Direction16 * Skip;   // 当前 DrawFrame
var img = _lib.Images[frame];
_particleEmitter.SetLocation(Direction16, (int)Position.X + img.OffSetX, (int)Position.Y + img.OffSetY);
```

**约束**：粒子节点必须与 `MirProjectileNode` 同级（都挂在 `GameScene` 下），坐标系才一致。**不要**把粒子节点做成 `MirProjectileNode` 的子节点然后用局部坐标——会引入双重偏移，且子节点会随 `QueueFree` 被一起销毁，无法实现原版"飞行物消失后拖尾继续飘散"的效果。

## 关键注意事项

- **不要改主 sprite 渲染**。`MirProjectileNode._Draw` 的主 sprite 渲染是正确的，问题只在缺粒子。
- **不要动 31 个无粒子技能**。它们原版就没粒子（LightningBall、EvilSlayer、ExplosiveTalisman、HundredFist 等），不是 bug。
- **透明键是另一套机制**：`ZlReader.cs:16` 的 `EffectTransparentKeyTolerance=32` 黑色透明键用于 `ImageType.Effect` 特效抠底（`GetEffectTexture`）；**原版粒子走 `ImageType.Image`（`GetImageTexture`），不做抠底**——粒子纹理（ProgUse 520-523 冰晶、530 烟雾）本身带 alpha，直接用即可。不要给粒子接 `GetEffectTexture`。
- **坐标用全局屏幕坐标**：发射点 = `MirProjectileNode.Position + 当前帧 OffSet`（步骤 5），粒子节点与飞行物同级。
- **参数逐字照搬**：MaxCount/SpawnFrequency/ttl/scale/fade 等任何"优化"都会改变观感，先原样移植，再谈调参。
- `Config.DrawParticles` 原版默认 false；Godot 端决定是否保留等价开关，无开关则默认启用。

## 验证方法

1. 启动服务器（`sudo systemctl start zircon-server`，已在运行）。
2. 本机 Godot 客户端已配置连接 `192.168.3.82:7000`。
3. 登录法师角色，对远处怪物施放**冰魂魄**。
   - 预期：飞行路径上出现 120 个旋转冰晶碎片 + 灰白烟雾拖尾（原版效果），命中时冰爆（Impact，`Magic.Zl` 2860 起 10 帧，首帧 128×64、最大 128×128）照常。
   - 对照：施放前确认拖尾随弹体移动、停止施法后拖尾残留粒子继续飘散至 TTL 耗尽。
4. 同样测试**冰刃（IceBlades）**：150 蓝色晶粒 + 10 蓝色放大烟雾。
5. 同样测试**烈风（GustBlast）**：10 个放大烟雾（视觉最轻，缺失时最难察觉）。
6. 回归火球术（FireBall）/火弹跳（FireBounce）：应无肉眼可见变化（主 sprite 主导，粒子只是点缀）。
7. 控制台应有 `[Magic] OnObjectMagic type=IceBolt` 日志（`GameScene.cs` 现有日志）。
8. 顺手抽查 1-2 个无粒子技能（如 LightningBall）确认未被误改。

## 相关源文件索引

原版（参考，只读不改）：
- `Client/Models/MirProjectile.cs` — 飞行物，粒子集成点（构造 :20-33、SetLocation :87-91、Remove :129）
- `Client/Models/MirEffect.cs` — 飞行物/特效基类（`_particleEmitter` 字段 :45）
- `Client/Models/Particles/Particle.cs` — 单个粒子（物理、绘制）
- `Client/Models/Particles/ParticleType.cs` — 粒子类型模板（MaxCount/SpawnFrequency/Textures）
- `Client/Models/Particles/ParticleEmitter.cs` — 发射器（CenterPoint/SetLocation/Process/StopGeneration）
- `Client/Models/Particles/Spells/FireballTrail.cs` — 火球拖尾（120 火星 + 5 烟雾）
- `Client/Models/Particles/Spells/IceBoltTrail.cs` — 冰魂魄拖尾（120 冰晶 + 20 烟雾）
- `Client/Models/Particles/Spells/IceBladesTrail.cs` — 冰刃拖尾（150 晶粒 + 10 蓝烟）
- `Client/Models/Particles/Spells/GustTrail.cs` — 烈风拖尾（10 烟雾）
- `Client/Models/MapObject.cs` — 61 处 `new MirProjectile`（施法 switch :771-3202、HundredFist :3308、DoomClawSpit :5114）；带粒子的 10 处在 845/855/930/940/972/982/1046/1056/1117/1129
- `Client/Scenes/Views/MapControl.cs` — `ParticleEffects` 容器（:176、绘制 :469）
- `Client/Scenes/GameScene.cs` — 粒子每帧 Process（:1132-1133）
- `Client/Envir/Config.cs` — `DrawParticles` 开关（:69）

Godot（要改的）：
- `GodotClient/Scripts/MirProjectileNode.cs` — 飞行物节点，加粒子集成（步骤 4）
- `GodotClient/Scripts/MagicEffectTable.cs` — `ProjectileDef`（:139）加 `ParticleEmitter` 字段（步骤 3）
- `GodotClient/Scripts/GameScene.cs` — `SpawnProjectileDefinition`（:3881）/`SpawnProjectileDefinitionTarget`（:3920）传递粒子类型；`ComputeEffectScreenPos`（:9009）坐标基准
- `GodotClient/Scripts/Particles/`（新建）— Particle/ParticleType/ParticleEmitter 移植（步骤 1）
- `GodotClient/Scripts/Particles/Spells/`（新建）— 4 个 Trail 移植（步骤 2）
- `GodotClient/Scripts/MapWeatherLayer.cs` — 天气粒子（Godot 粒子移植参考先例，`WeatherParticle` + 居中绘制）
- `GodotClient/Formats/LibraryCache.cs` — 图库缓存：`Get(LibraryFile)` → `ZlLibrary`
- `GodotClient/Formats/ZlReader.cs` — `GetImageTexture`（:212，原版 `ImageType.Image` 语义）、`ZlImage` 帧元数据（OffSet 读取）

资源：
- `Debug/Client/Data/ProgUse.Zl` — 粒子纹理来源（帧 520-523 冰晶 32×32、530 烟雾 128×128）
- `Debug/Client/Data/Magic.Zl` / `MagicEx.Zl` — 主 sprite 与命中特效（帧尺寸见"证据链·四"）
