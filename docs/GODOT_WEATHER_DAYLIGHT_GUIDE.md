# Godot 天气、昼夜与环境光说明

本文记录 Godot 客户端当前天气、地图环境光、昼夜循环和局部光源的完整关系，并注明与旧版客户端的对应位置。

## 1. 结论摘要

- 天气由 `MapInfo.Weather` 决定，不会因为进入夜晚而自动出现。
- 当前数据库中的 244 张地图 `Weather` 全部是 `None`；Godot 测试模式会对 5 个主要城镇临时随机覆盖天气，方便验证天气效果。
- 环境光由 `MapInfo.Light` 和服务器下发的 `DayTime` 共同决定。
- `TimeOfDay` 主要表示 Dawn/Day/Dusk/Night 阶段和小地图图标；客户端环境亮度实际使用 `LightSetting` 或 `DayTime`。
- 夜间局部光源来自玩家、其他玩家、带光照属性的对象、地图格子光和技能/特效光。

## 2. 天气类型

天气定义在 `LibraryCore/Enum.cs` 的 `[Flags] Weather`：

| 名称 | 数值 | 含义 |
|---|---:|---|
| `None` | 0 | 无天气 |
| `Rain` | 1 | 雨 |
| `Snow` | 2 | 雪 |
| `Fog` | 4 | 雾 |
| `Lightning` | 8 | 闪电 |

天气是位标志，可以组合。例如：

| 组合 | 数值 |
|---|---:|
| `SnowFog` | 6 |
| `RainLightning` | 9 |
| `FogLightning` | 12 |
| `RainFogLightning` | 13 |

客户端使用位判断，因此组合天气可以同时生成多种粒子：

```csharp
Has(Weather.Rain)
Has(Weather.Snow)
Has(Weather.Fog)
Has(Weather.Lightning)
```

地图天气配置位于服务端数据库的 `MapInfo.Weather` 字段，管理界面对应 `Server/Views/MapInfoView.cs` 的 Weather 下拉框。

## 3. 四种天气的旧端参数

天气素材来自 `ProgUse.Zl`，当前 Godot 实现位于 `GodotClient/Scripts/MapWeatherLayer.cs`。

### 雨

- 生成间隔：10ms
- 生成位置：80% 从顶部，20% 从右侧
- 初始速度：`(-1, 5)`
- 缩放：旧端 `1..2`
- 初始角度：`0.4`
- 初始生命周期：500～2000ms
- 素材：509
- 到期后停止移动，播放水花 510、511、512、513、514
- 水花每帧 100ms
- 水花播放完毕后移除

### 雪

- 最大数量：500
- 生成间隔：20ms
- 生成位置：屏幕顶部
- 初始速度：X 为 `-1` 或 `0`，Y 为 `1`
- 缩放：`0..1.5`
- 旋转速度：`0.1` 个旧端逻辑 tick
- 生命周期：4000～10000ms
- 素材：500
- 到期后停止移动、停止旋转
- 之后以 `ScaleRate=-0.01`、`FadeRate=0.01` 消散

### 雾

- 最大数量：4
- 初始生成，不按时间间隔补充
- 素材：550
- 缩放：4
- 速度：`(1, 0)`
- 生命周期：1小时
- 颜色：`DarkGray`
- 多张雾图按素材宽度连续排列，形成循环雾带

### 闪电

- 最大数量：3
- 生成间隔：随机1000～5000ms
- 生成位置：屏幕顶部随机 X
- 速度：0
- 缩放：1～3
- 生命周期：100～200ms
- 素材：540
- 到期后淡出，淡出速度对应旧端 `FadeRate=0.1`

## 4. 天气的绘制顺序

当前世界绘制顺序为：

```text
地图/对象/技能
    ↓
天气层 Z=850
    ↓
环境光层 Z=900
    ↓
UI、小地图等界面层
```

这样夜间环境光会同时压暗地图、人物、技能和天气。天气不会覆盖夜间黑暗层。

地图切换时 `MapWeatherLayer.SetWeather()` 会：

1. 更新天气位标志
2. 清空旧地图残留粒子
3. 重置生成计时器
4. 重新生成雾
5. 按新地图天气位生成雨、雪、闪电

## 5. 当前地图天气配置与城镇测试随机天气

当前导出的 `MapInfo` 数据共 244 张地图：

| Weather | 地图数量 |
|---|---:|
| `None` | 244 |
| 其他天气或组合 | 0 |

数据库没有天气的主要城镇目前由 Godot 测试模式临时覆盖。以下地图每次进入或切换时，会从 `1..15` 随机选择一个非空天气位组合：

| 文件名 | 地图 |
|---|---|
| `0` | Bichon Town |
| `1` | Lost Paradise |
| `2` | Banya Village |
| `3` | Sabuk Keep |
| `4` | Numa Village |

随机范围包含单天气和组合天气，但不包含 `None`，因此进入这些城镇一定能看到至少一种天气。其他地图仍使用数据库原始配置，服务端数据库不会被修改。

关闭测试覆盖时，将 `GameScene.cs` 中的 `TownWeatherTestMode` 改为 `false`。

不使用 Godot 测试覆盖时，也可以在 MapInfo 中把某张地图改成例如：

- `Rain`
- `Snow`
- `Fog`
- `RainFogLightning`

保存数据库后重新进入地图，客户端会在日志中输出：

```text
[Light] map=... setting=... weather=... dayTime=...
```

其中 `weather=None` 就表示该地图没有配置天气，不是客户端没有加载成功。

## 6. 环境光四种模式

定义在 `LibraryCore/Enum.cs` 的 `LightSetting`：

| 模式 | 环境亮度 | 旧端实现 | Godot 实现 |
|---|---:|---|---|
| `Light` | 100% | `(255,255,255)` | `1.0` |
| `Night` | 使用 Twilight | `(15,15,15)`（旧端） | `100/255` |
| `Twilight` | 约39.2% | `(100,100,100)` | `0.39` |
| `Default` | 使用 `DayTime` | `255 * DayTime` | `DayTime` |

当前 Godot 客户端不再使用旧端最黑的视觉档。`Night` 会使用第三档 `Twilight` 的亮度；`Default` 地图即使服务器下发 `DayTime=0`，也会被限制到同样的 Twilight 下限。

固定模式优先级如下：

```text
MapInfo.Light == Light  -> 100%
MapInfo.Light == Night  -> Twilight 下限
MapInfo.Light == Twilight -> 约39.2%
MapInfo.Light == Default -> 使用服务器 DayTime
```

所以进入一个 `Night` 地图时会使用 Twilight 亮度；进入 `Default` 地图时仍随服务器昼夜循环变化，但不会低于 Twilight 亮度。

当前 244 张地图的环境光配置统计：

| LightSetting | 地图数量 |
|---|---:|
| `Default` | 67 |
| `Light` | 10 |
| `Night` | 165 |
| `Twilight` | 2 |

## 7. 服务器昼夜循环

昼夜逻辑位于 `ServerLibrary/Envir/SEnvir.cs:CalculateLights()`。

服务器配置：

```csharp
DayCycleCount = 3
```

服务器将现实时间乘以 `DayCycleCount`，再折算为游戏时间。因此当前配置下：

- 现实 8 小时 = 游戏 24 小时
- 游戏 1 小时 = 现实 20 分钟

游戏时间阶段：

| 游戏时间 | `TimeOfDay` | `DayTime` |
|---|---|---:|
| 00:00～04:59 | `Night` | 0 |
| 05:00～07:59 | `Dawn` | 0→1 线性增加 |
| 08:00～16:59 | `Day` | 1 |
| 17:00～19:59 | `Dusk` | 1→0 线性降低 |
| 20:00～23:59 | `Night` | 0 |

注意：`TimeOfDay` 和 `DayTime` 是服务器分别计算、分别广播的两个值。客户端不会自行根据本地电脑时间计算昼夜。

## 8. 网络切换流程

### 进入游戏

`StartInformation` 同时带有：

- `DayTime`
- `TimeOfDay`
- `TimeOfDayLabel`
- 当前地图索引

Godot 在进入游戏时保存这些值，然后加载地图。

### 游戏运行中

服务器变化时发送：

```text
S.DayChanged
S.TimeOfDayChanged
```

Godot 网络层处理位置：

- `GodotClient/Network/ServerConnection.cs`

场景层处理位置：

- `GameScene.OnDayTimeChanged()`：更新环境光层
- `GameScene.OnTimeOfDayChanged()`：更新时间阶段和小地图图标

`DayTime` 的变化会立即调用：

```csharp
_lightLayer.SetDayTime(DayTime);
```

## 9. 夜间局部光源

环境光变暗后，局部光源会把附近区域提亮。

当前光源来源：

| 来源 | 无装备/无属性时 | 有光照属性时 |
|---|---:|---:|
| 本地玩家 | 半径3微光 | 使用玩家 `Stat.Light` |
| 其他玩家 | 半径3微光 | 使用玩家 `Light` |
| NPC/怪物/物体 | 无 | 使用对象 `Light` |
| 地图格子 | 无 | 使用 `.map` 格子 Light |
| 普通技能特效 | 无 | 使用 `FrameLight` |
| 投射物 | 无 | 使用 `ProjectileDef.FrameLight` |

人物和对象光源使用旧端的物体光公式；技能特效额外除以5，避免技能光圈放大过度。

局部光源只在环境光小于100%时可见。白天 `LightSetting.Light` 或 `Default + DayTime=1` 时，整张地图已经全亮，局部光源不会产生明显视觉差异。

## 10. 常见问题排查

### 看不到天气

按顺序检查：

1. 日志中的 `weather` 是否为 `None`
2. 当前地图的 `MapInfo.Weather` 是否配置了对应位
3. `ProgUse.Zl` 是否存在且成功加载
4. 是否在地图切换后等待粒子生成时间
5. 配置中的天气绘制开关是否关闭

### 整张地图很黑

检查日志：

```text
[Light] map=... setting=... weather=... dayTime=...
```

- `setting=Night`：当前客户端使用 Twilight 亮度，不再是旧端约5.9%
- `setting=Default dayTime=0`：服务器正处于夜晚，但客户端会使用 Twilight 下限
- `setting=Default dayTime` 接近0但 `TimeOfDay` 不合理：检查服务器昼夜广播
- `setting=Light`：不应出现环境黑暗层

### 只有本地玩家发光

当前版本已经为其他未死亡玩家增加半径3的基础微光；如果仍看不到，应检查：

- 该地图是否为 `Light` 或白天满亮
- 远端玩家是否已进入 `_otherPlayers`
- 远端玩家是否被标记为 `Dead`
- `[Light]` 日志和运行时光源位置是否正常

## 11. 主要代码和数据索引

- 天气枚举：`LibraryCore/Enum.cs`
- 环境光枚举：`LibraryCore/Enum.cs`
- 地图天气/光照字段：`LibraryCore/SystemModels/MapInfo.cs`
- 服务器昼夜计算：`ServerLibrary/Envir/SEnvir.cs`
- 网络数据包：`LibraryCore/Network/ServerPackets.cs`
- Godot 天气：`GodotClient/Scripts/MapWeatherLayer.cs`
- Godot 环境光：`GodotClient/Scripts/MapLightLayer.cs`
- Godot 场景接线：`GodotClient/Scripts/GameScene.cs`
- 旧端天气：`Client/Models/Particles/Weather/`
- 旧端环境光：`Client/Scenes/Views/MapControl.cs`
- 当前地图数据：`docs/research/ei2-research/data/MapInfo.md`
