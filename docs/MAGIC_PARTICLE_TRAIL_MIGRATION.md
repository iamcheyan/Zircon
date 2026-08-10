# 魔法飞行物粒子拖尾系统迁移指南

## 问题现象

Godot 客户端移植自原版 Windows 客户端（`Client/`）。用户报告：

- **火球术 (FireBall)**：能看到飞行轨迹 ✅
- **冰魂魄 (IceBolt)**：只能看到命中爆炸，**看不到飞行轨迹** ❌
- **爆裂火焰类**：只能看到部分效果 ❌

伤害正常，纯视觉缺失。

## 根因

**Godot 客户端没有移植原版的「飞行物粒子拖尾系统」。**

原版每个飞行物（`MirProjectile`）在构造时可传入一个 `ParticleEmitter` 类型，每帧沿飞行路径生成粒子，形成拖尾。冰魂魄的轨迹视觉**主要靠 120 个冰晶粒子**表现，主 sprite 只有 32×64 的小冰弹——没有粒子就基本看不到轨迹。火球术的主 sprite 是 128×128 的大火球，所以没有粒子也能看到。

## 证据

### 原版粒子系统集成点

**`Client/Models/MirProjectile.cs:20-33`** — 构造函数接收 `Type particleEmitter`：
```csharp
public MirProjectile(int startIndex, int frameCount, TimeSpan frameDelay, LibraryFile file,
    int startlight, int endlight, Color lightColour, Point origin, Type particleEmitter = null)
    : base(...)
{
    if (Config.DrawParticles && particleEmitter != null)
    {
        _particleEmitter = (ParticleEmitter)Activator.CreateInstance(particleEmitter, this);
        GameScene.Game.MapControl.ParticleEffects.Add(_particleEmitter);
    }
}
```

**`Client/Models/MirProjectile.cs:87-92`** — 每帧更新粒子位置：
```csharp
if (_particleEmitter != null)
{
    Point offset = Library.GetOffSet(DrawFrame);
    _particleEmitter.SetLocation(Direction16, DrawX + offset.X, DrawY + offset.Y);
}
```

### 原版粒子拖尾定义

4 个飞行物拖尾（`Client/Models/Particles/Spells/`）：

| 文件 | 技能 | 粒子类型 | MaxCount | 纹理(ProgUse.Zl) | 颜色 |
|---|---|---|---|---|---|
| `FireballTrail.cs` | 火球术/火弹跳 | SmokeParticle | 5 | 帧 530 | DimGray |
| `IceBoltTrail.cs` | 冰魂魄 | IceParticle + SmokeParticle | 120 + 20 | 帧 520-523 / 530 | IceColour + LightGray |
| `IceBladesTrail.cs` | 冰刃 | (见源文件) | (见源文件) | (见源文件) | Ice |
| `GustTrail.cs` | 烈风 | (见源文件) | (见源文件) | (见源文件) | Wind |

### 受影响技能完整清单

原版 `Client/Models/MapObject.cs` 有 **61 处 `new MirProjectile`**，关联 39 个 MagicType。其中**只有 5 个技能带粒子拖尾**（共 10 处构造，每个技能 2 处：目标弹道 + 地面弹道）：

| MagicType | 粒子类型 | 主 sprite 尺寸 | Godot 能否看到轨迹 | 说明 |
|---|---|---|---|---|
| `FireBall` | FireballTrail | 128×128 | ✅ 能看到 | 主 sprite 大，粒子只是辅助 |
| `FireBounce` | FireballTrail | 128×128 | ✅ 应该能看到 | 同上 |
| `IceBolt` | **IceBoltTrail** | **32×64** | ❌ **看不到** | 主 sprite 太小，轨迹靠 120 粒子 |
| `IceBlades` | **IceBladesTrail** | **64×32** | ❌ **很可能看不到** | 主 sprite 小 |
| `GustBlast` | **GustTrail** | **64×128** | ⚠️ **可能部分缺失** | 中等尺寸 |

> **用户报告的冰魂魄正是这个原因。冰刃(IceBlades)和烈风(GustBlast)大概率也有同样问题，用户只是没测到。**

### 为什么其他 34 个无粒子的飞行物不受影响

其余 34 个 MagicType 的 MirProjectile **原版就没有粒子拖尾**（如 LightningBall、EvilSlayer、ExplosiveTalisman 等）。它们的轨迹只靠主 sprite。部分主 sprite 也很小（如 ExplosiveTalisman 16×32、HundredFist 16×32），但那是原版设计如此，不是 Godot 的回归。

## Godot 端当前状态

### 已有的（正确）

- `GodotClient/Scripts/MirProjectileNode.cs` — 飞行物主 sprite 渲染，移植了原版 MirProjectile 的位置插值、方向选帧、flyPast 逻辑。**主 sprite 渲染正确**。
- `GodotClient/Scripts/MagicEffectTable.cs` — 5 个技能都配了 `Projectile`（主 sprite 的 LibraryFile/StartIndex/FrameCount）。
- `GodotClient/Scripts/MapWeatherLayer.cs` — 天气粒子系统已移植（说明 Godot 端有粒子绘制能力可参考）。

### 缺失的

1. `GodotClient/Scripts/MirProjectileNode.cs` **没有任何粒子相关代码**。
2. `MagicEffectTable.ProjectileDef` **没有 `ParticleEmitter` 字段**。
3. `GodotClient/` 下**没有** `Particle.cs` / `ParticleType.cs` / `ParticleEmitter.cs` 的 Godot 移植。
4. `GodotClient/` 下**没有** `Spells/*Trail.cs` 的 Godot 移植。

## 修复方案

### 要做的事

移植原版粒子拖尾系统到 Godot，挂到 `MirProjectileNode`。

### 步骤 1：移植粒子基础类

原版：
- `Client/Models/Particles/Particle.cs`
- `Client/Models/Particles/ParticleType.cs`
- `Client/Models/Particles/ParticleEmitter.cs`

移植到 `GodotClient/Scripts/Particles/`（新建目录）。关键改动：

- 原版用 `System.Drawing` + SharpDX 渲染 → Godot 用 `DrawTextureRect` 或 `GPUParticles2D`。
- 粒子纹理来自 `ProgUse.Zl`（帧 520-523 冰晶、530 烟雾），用 `LibraryCache.Get(LibraryFile.ProgUse).GetImageTexture(index)` 获取。
- 粒子生命周期、位置、速度、缩放、旋转、淡出逻辑保持原版参数。
- `ParticleEmitter.SetLocation(direction16, x, y)` 每帧由 `MirProjectileNode._Process` 调用。
- 粒子节点作为 `MirProjectileNode` 的子节点或挂在 `GameScene` 上（原版挂在 `MapControl.ParticleEffects`），在 `_Draw` 或 `_Process` 中更新和绘制。

参考 `MapWeatherLayer.cs` 的天气粒子实现——它已经是原版粒子的 Godot 移植先例，复用其绘制模式。

### 步骤 2：移植 4 个飞行物拖尾

原版 `Client/Models/Particles/Spells/`：
- `FireballTrail.cs`
- `IceBoltTrail.cs`
- `IceBladesTrail.cs`
- `GustTrail.cs`

移植到 `GodotClient/Scripts/Particles/Spells/`。**保持原版的 MaxCount、SpawnFrequency、Textures（ProgUse 帧索引）、Color、CenterPoint、velocity、ttl、scale、fade 参数不变**。这些参数决定了拖尾的视觉密度和形态。

特别重要的 `IceBoltTrail` 参数（`Client/Models/Particles/Spells/IceBoltTrail.cs`）：
- `IceParticle`: MaxCount=120, SpawnFrequency=5ms, Textures={520,521,522,523}, Color=IceColour, ttl=200-500ms, scale=0.15, opacity=0.5, fade=true fadeRate=0.01
- `SmokeParticle`: MaxCount=20, SpawnFrequency=15ms, Textures={530}, Color=LightGray, ttl=50-100ms
- `CenterPoint[]`: 16 个偏移点（冰晶沿飞行物散布位置，见源文件 :87-108）

### 步骤 3：MagicEffectTable.ProjectileDef 加 Particle 字段

在 `GodotClient/Scripts/MagicEffectTable.cs` 的 `ProjectileDef` 类（:139）加：
```csharp
public Type ParticleEmitter;  // 原版拖尾粒子类型，null 表示无拖尾
```

给 5 个技能的 Projectile 填上：
```csharp
[MagicType.FireBall]   ... Projectile = new ProjectileDef { ..., ParticleEmitter = typeof(FireballTrail) }
[MagicType.FireBounce] ... Projectile = new ProjectileDef { ..., ParticleEmitter = typeof(FireballTrail) }
[MagicType.IceBolt]    ... Projectile = new ProjectileDef { ..., ParticleEmitter = typeof(IceBoltTrail) }
[MagicType.IceBlades]  ... Projectile = new ProjectileDef { ..., ParticleEmitter = typeof(IceBladesTrail) }
[MagicType.GustBlast]  ... Projectile = new ProjectileDef { ..., ParticleEmitter = typeof(GustTrail) }
```

### 步骤 4：MirProjectileNode 集成粒子

在 `GodotClient/Scripts/MirProjectileNode.cs`：

1. `SetupProjectile` 或 `SpawnProjectileDefinition` 处接收并创建 `ParticleEmitter`。
2. `_Process` 每帧调用 `_particleEmitter?.SetLocation(Direction16, Position.x, Position.y)`（用 Godot 屏幕坐标，注意原版 `DrawX/DrawY` 是贴图左上角 + offset，Godot 的 `Position` 是节点位置——需要确认坐标对应关系）。
3. 飞行物 `QueueFree` 时一并释放粒子发射器。
4. `Config.DrawParticles` 门控：原版有这个开关，Godot 端如果有等价配置项也保留；没有就默认开启。

在 `GodotClient/Scripts/GameScene.cs` 的 `SpawnProjectileDefinition`（:3881）传递 `proj.ParticleEmitter` 给 `MirProjectileNode`。

### 步骤 5：坐标对应关系（关键，容易出错）

原版粒子的坐标是 **屏幕像素坐标**（`DrawX/DrawY`，已含相机偏移）。Godot 的 `MirProjectileNode.Position` 也是屏幕坐标（`_Process` 里 `originScreen.Lerp(targetScreen, t)`）。

原版 `MirProjectile.Process` 传给粒子的坐标：
```csharp
_particleEmitter.SetLocation(Direction16, DrawX + offset.X, DrawY + offset.Y);
```
其中 `DrawX/DrawY` 是飞行物屏幕坐标，`offset` 是当前帧的 `Library.GetOffSet(DrawFrame)`。

Godot 移植时：`MirProjectileNode.Position` 已经是屏幕坐标，但粒子如果是其子节点，子节点坐标是相对父节点的——需要决定粒子用全局屏幕坐标还是相对坐标。建议粒子发射器作为**独立节点**挂在 `GameScene` 下，用全局屏幕坐标（和飞行物一致的坐标系），而非挂在 `MirProjectileNode` 下，避免坐标嵌套问题。

## 验证方法

1. 启动 82 服务器（`sudo systemctl start zircon-server`，已在运行）。
2. 本机 Godot 客户端已配置连接 `192.168.3.82:7000`。
3. 登录法师角色，对远处怪物施放**冰魂魄**。
4. 预期：能看到冰晶粒子沿飞行路径拖尾（原版效果）。
5. 同样测试冰刃(IceBlades)、烈风(GustBlast)。
6. 控制台应有 `[Magic] OnObjectMagic type=IceBolt` 日志。

## 注意事项

- **不要改主 sprite 渲染**。`MirProjectileNode._Draw` 的主 sprite 渲染是正确的，问题只在缺粒子。
- **不要动其他 34 个无粒子技能**。它们原版就没粒子。
- `EffectTransparentKeyTolerance=32` 的黑色透明键（`ZlReader.cs:16`）是另一套机制，用于主 sprite 的背景透明，与粒子无关。粒子用 `GetImageTexture`（无透明键）还是 `GetEffectTexture`（有透明键）取决于粒子纹理是否需要去黑底——ProgUse 帧 520-523 冰晶可能有黑底，需要测试确认用哪个。原版粒子的 `ImageType.Image` 在 `MirLibrary.Draw` 里也有 colour-key 透明处理。
- 原版有 `Config.DrawParticles` 开关。如果 Godot 端没有等价配置，默认启用即可。

## 相关源文件索引

原版（参考）：
- `Client/Models/MirProjectile.cs` — 飞行物，粒子集成点
- `Client/Models/MirEffect.cs` — 飞行物基类
- `Client/Models/Particles/Particle.cs` — 粒子
- `Client/Models/Particles/ParticleType.cs` — 粒子类型定义
- `Client/Models/Particles/ParticleEmitter.cs` — 粒子发射器
- `Client/Models/Particles/Spells/FireballTrail.cs` — 火球拖尾
- `Client/Models/Particles/Spells/IceBoltTrail.cs` — 冰魂魄拖尾
- `Client/Models/Particles/Spells/IceBladesTrail.cs` — 冰刃拖尾
- `Client/Models/Particles/Spells/GustTrail.cs` — 烈风拖尾
- `Client/Models/MapObject.cs:768+` — Spell 分支，所有 MirProjectile 构造

Godot（要改的）：
- `GodotClient/Scripts/MirProjectileNode.cs` — 飞行物节点，加粒子集成
- `GodotClient/Scripts/MagicEffectTable.cs` — ProjectileDef 加 ParticleEmitter 字段
- `GodotClient/Scripts/GameScene.cs:3881` — SpawnProjectileDefinition，传递粒子类型
- `GodotClient/Scripts/MapWeatherLayer.cs` — 天气粒子（Godot 粒子移植参考先例）
- `GodotClient/Formats/LibraryCache.cs` — 图库缓存，获取 ProgUse.Zl 纹理

资源：
- `Debug/Client/Data/ProgUse.Zl` — 粒子纹理来源（帧 520-523 冰晶、530 烟雾）