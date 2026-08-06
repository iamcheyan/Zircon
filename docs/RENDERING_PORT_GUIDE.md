# Mir3 传奇3 客户端渲染移植指南(硬编码数据全量提取)

> 目标:为 Godot 客户端移植提供**逐条可对照**的客户端渲染硬编码数据(在线方案:Godot 客户端直连 ServerCore;渲染层与包来源无关)。所有表格均直接提取自源码,不含推测。
> 数据由脚本机械解析生成(`/tmp/*.json` 为原始资产)。源文件版本以本仓库 `Client/`、`LibraryCore/`、`Library/` 当前提交为准。

## 数据来源文件清单

| 数据 | 来源文件 |
|---|---|
| 坐标/帧公式、SetAction 特效表(153 魔法/266 特效/191 音效) | `Client/Models/MapObject.cs` |
| MirEffect / MirProjectile 原语 | `Client/Models/MirEffect.cs`、`Client/Models/MirProjectile.cs` |
| 链状/绳状特效 | `Client/Models/MirLineEffect.cs`(含 MirChainEffect、MirRopeEffect) |
| 玩家渲染常量(5 字典、偏移、分层) | `Client/Models/PlayerObject.cs` |
| 怪物图像映射(291 case)、每怪特效(45) | `Client/Models/MonsterObject.cs` |
| NPC / 物品渲染 | `Client/Models/NPCObject.cs`、`Client/Models/ItemObject.cs` |
| 地图绘制、.map 格式、Floor/Light/天气 | `Client/Scenes/Views/MapControl.cs`、`Client/Models/Particles/Weather/*.cs` |
| 帧表 FrameSet(94 表) | `LibraryCore/FrameSet.cs` |
| 库路径 LibraryFile/LibraryList/KROrder | `Library/LibraryFile.cs`、`Library/Libraries.cs` |
| 元素颜色、公共常量 | `LibraryCore/Globals.cs` |

## 提取与校对方法

- 全部表格由脚本机械解析源码生成,再与人工复核交叉验证。
- `MonsterImage` switch 共 291 个 case,机械 diff 确认覆盖 291/294 枚举成员;`Shinsu1`(枚举值 100,注释 "Large")与 `SDMob8`(无 case)为死代码,`None` 无 case。
- `LibraryFile` 枚举 316 成员(含 None);`LibraryList` 314 条路径(None 与 HorseS 无路径条目)。
- 手工读取与 JSON 冲突时以 JSON 为准(例:FrameSet 实为 94 表,非 63)。

---

# 第 1 章 坐标系统与基础原语

## 1.1 网格常量(MapControl.cs)

| 常量 | 值 |
|---|---|
| `CellWidth` | 48 px |
| `CellHeight` | 32 px |
| `ManualHeightOffset` | 34 px |
| `OffSetX` | `Width / 2 / CellWidth`(即视口宽度/96) |
| `OffSetY` | `Height / 2 / CellHeight`(即视口高度/64) |
| `PixelOffsetX` | `(Width - CellWidth) / 2 - OffSetX * CellWidth` |
| `PixelOffsetY` | `(Height - CellHeight) / 2 - OffSetY * CellHeight - ManualHeightOffset` |

## 1.2 对象屏幕坐标(MapObject.cs)

`DrawFrame`(当前应绘制的帧号):

```
DrawFrame = FrameIndex + CurrentFrame.StartIndex + CurrentFrame.OffSet * (int)Direction
```

屏幕坐标(以玩家 `User` 为视口中心;精确公式见 MapObject.cs:371-383):

```
DrawX = (CurrentLocation.X - User.CurrentLocation.X + OffSetX) * CellWidth
        + PixelOffsetX + MovingOffSet.X - User.MovingOffSet.X - User.ShakeScreenOffset.X
DrawY = (CurrentLocation.Y - User.CurrentLocation.Y + OffSetY) * CellHeight
        + PixelOffsetY + MovingOffSet.Y - User.MovingOffSet.Y - User.ShakeScreenOffset.Y
```

### 移动插值(SetFrame,MapObject.cs:640-723)

移动/被推时,帧推进按每方向衰减计算像素偏移:

```
// 每方向:frame 从 0..FrameCount-1
off = ±(int)(CellWidth * MoveDistance / FrameCount * (FrameCount - (frame + 1)))
```

Sum 模式使用 `Frames[CurrentAction].Sum` 的变体;Moving/Pushed 受 `GameScene.Game.MoveFrame` 门控;完成时调用 `DoNextAction()`。

## 1.3 Frame 类(Library)

| 字段 | 语义 |
|---|---|
| `StartIndex` | 起始帧号(库内绝对索引) |
| `FrameCount` | 帧数 |
| `OffSet` | 每方向间隔(8 方向 × OffSet) |
| `Reversed` | 反向播放(倒放) |
| `StaticSpeed` | 静态速度(Pushed 等使用) |
| `Delays` | 每帧延时数组(ms);缺省同 frameDelay;`GetFrame` 按累计延时推进 |
| `Sum` | 移动插值模式标志 |

`GetFrame`:按 `Delays` 累计流逝,结束时返回 `FrameCount`(即"播完")。

## 1.4 MirEffect 原语(MirEffect.cs)

| 字段 | 类型/默认 | 语义 |
|---|---|---|
| `Target` | MapObject | 跟随对象(每帧取其 DrawX/DrawY) |
| `MapTarget` | Point | 无 Target 时的地图格坐标 |
| `Library` | MirLibrary | 由构造参数 `LibraryFile` 解析 |
| `StartTime` | DateTime | 构造时取 `CEnvir.Now` |
| `StartIndex` / `FrameCount` / `Delays` | int / int / TimeSpan[] | 帧区间与逐帧延时 |
| `DrawColour` | Color=White | 绘制着色 |
| `Blend` | bool | 加法混合绘制(`DrawBlend`,BlendRate 默认 0.7F) |
| `Reversed` | bool | 倒放 |
| `Opacity` | float=1F | 透明度 |
| `BlendRate` | float=0.7F | 混合率 |
| `UseOffSet` | bool=true | 是否使用库内 OffSet |
| `Loop` | bool=false | 循环;`CurrentLoopCount` 记录循环次数 |
| `DrawX` / `DrawY` | int | 当前绘制坐标(变更时置 `MapControl.TextureValid=false`) |
| `DrawFrame` | int | 当前绘制帧号(变更时触发 `FrameAction`) |
| `DrawType` | DrawType=Object | 绘制层:Floor / Object / Final |
| `Skip` | int=10 | 构造时固定为 10;方向帧间隔(与 Frame.OffSet 同义) |
| `Direction` | MirDirection | 生效于 `DrawFrame = FrameIndex + StartIndex + (int)Direction * Skip` |
| `StartLight`/`EndLight`/`LightColours` | int/int/Color[] | 光源强度插值与逐帧颜色 |
| `FrameLight` | float | 按流逝插值 `StartLight → EndLight`(Loop 时按总时长取模) |
| `FrameLightColour` | Color | `LightColours[FrameIndex]` |
| `CurrentLocation` | Point | `Target?.CurrentLocation ?? MapTarget` |
| `AdditionalOffSet` | Point | 附加像素偏移 |
| `CompleteAction`/`FrameAction`/`FrameIndexAction` | Action | 完成/换帧/帧索引回调 |

构造参数:`(int startIndex, int frameCount, TimeSpan frameDelay, LibraryFile file, int startLight, int endLight, Color lightColour)`;构造时 `Skip = 10`,`Delays[i] = frameDelay`、`LightColours[i] = lightColour`,并注册到 `MapControl.Effects`。

`Process()`(每帧):

```
if (CEnvir.Now < StartTime) return;
if (Target != null)  DrawX/Y = Target.DrawX/Y + AdditionalOffSet;
else                 按 MapTarget 相对玩家格坐标换算(同 1.2 公式);
frame = GetFrame();
if (frame == FrameCount) { CompleteAction?.Invoke(); Remove(); return; }
if (Reversed) frame = FrameCount - frame - 1;
FrameIndex = frame;
DrawFrame = FrameIndex + StartIndex + (int)Direction * Skip;
```

`Draw()`:Blend 用 `Library.DrawBlend(DrawFrame, DrawX, DrawY, DrawColour, UseOffSet, BlendRate, ImageType.Image)`;否则 `Library.Draw(..., UseOffSet, Opacity, ImageType.Image)`。

`DrawType` 枚举:`Floor`(背景层,先于物体)、`Object`(与物体同层,按 RenderY 排序)、`Final`(最上层,玩家之后)。

## 1.5 MirProjectile(投射物,MirProjectile.cs)

| 字段 | 默认 | 语义 |
|---|---|---|
| `Origin` | Point | 起点(构造传入) |
| `Speed` | 50 | 速度(由 Duration 计算替代) |
| `Explode` | false | 命中后是否爆炸(无 Target 时) |
| `Delay` | 0 | 时长倍率(>0 时 `duration *= Delay`) |
| `Direction16` | int | 16 方向帧偏移 |
| `Has16Directions` | true | 16 方向(否则 `Direction16 /= 2` 转 8 方向) |

构造参数尾附 `Point origin` 与可选 `Type particleEmitter`;`Config.DrawParticles` 开启时实例化粒子发射器。

`Process()` 插值:

```
位置 = Origin 与 (Target?.CurrentLocation ?? MapTarget) 之间,按时间线性插值;
Duration = 距离(网格换算:y/32*48) * 1ms;若 Delay>0 则 *= Delay;
Direction16 = Functions.Direction16(起点, 终点);  !Has16Directions 时 /=2;
DrawFrame = frame + StartIndex + Direction16 * Skip;
DrawX = x + time.Ticks / (duration / x2) + AdditionalOffSet.X + PixelOffsetX;  // Y 同理
粒子发射器:SetLocation(Direction16, DrawX + 库OffSet.X, DrawY + 库OffSet.Y)
超时:无 Target 且 !Explode 且仍在屏幕内则继续显示,否则 CompleteAction + Remove。
```

`GetFrame()` 对 `TotalDuration` 取模(投射物天然循环)。

## 1.6 链状/绳状特效(MirLineEffect.cs)

### MirLineEffect(基类)

| 常量 | 值 |
|---|---|
| `LinkLength` | 30f(每节期望长度) |
| `Gravity` | 0.05f(每 tick 向下力) |
| `SpringStrength` | 0.15f(相邻节间拉力) |
| `Damping` | 0.9f(速度阻尼) |
| `AnchorOffsetY` | 50f(锚点上移) |

构造:`(MapObject source, MapObject target, LibraryFile library, int startIndex, float imageScale = 1F)` → 调基类 `(startIndex, 1, 100ms, library, 0, 0, Color.White)`。

- 链节数 `_linkCount` 按距离自适应:`desiredLinks = max(2, ceil(distance / (LinkLength*ImageScale)) + 1)`。
- `Process()`:锚点固定两端,中间节做 重力+弹簧 仿真;`_owner` 特效消失则自毁。
- `Draw()`:逐节用 `Library.DrawScaled/DrawBlendScaled(StartIndex, stretchX, stretchY, colour, drawX, drawY, angle, Opacity, ImageType.Image, false, 0)`;`stretchY = dist/LinkLength`,`stretchX = ImageScale`;角度 `atan2(dy,dx)+π/2`。
- `ToWorld(obj)`:以玩家为中心换算世界坐标(同 1.2),再减 `AnchorOffsetY`;源/目标各自附加偏移 `SourceOffset()/TargetOffset()`。

### 子类

| 类 | 库 | StartIndex | 说明 |
|---|---|---|---|
| `MirChainEffect` | MagicEx7 | 80 | 纯链条(驯兽绳索前身) |
| `MirRopeEffect` | MagicEx7 | 81, ImageScale=0.5 | 投掷抛物线:LaunchDuration=600ms、ThrowArcHeight=120、OvershootFactor=1.15;先折叠于起点,飞行期 `ComputeThrownTarget`(水平 EaseOutCubic、垂直 EaseOutQuad + sin 弧顶),落地后转 `base.Process()` 弹簧物理 |

`SourceOffset()`(方向表):Up(0,-50) / UpRight(40,-35) / Right(35,-15) / DownRight(27,-7) / Down(0,0) / DownLeft(-17,-10) / Left(-25,-20) / UpLeft(-15,-40),最终 `(-10+dx, 20+dy)`。
`TargetOffset()`(方向表):Up(0,-50) / UpRight(25,-45) / Right(40,-30) / DownRight(25,-10) / Down(-10,10) / DownLeft(-25,-10) / Left(-40,-30) / UpLeft(-25,-45),最终 `(8+dx, 10+dy)`。


## 1.7 渲染相关枚举(全量)

### MirGender(2)

| 名称 | 值 | 备注 |
|---|---|---|
| Male | 0 |  |
| Female | 1 |  |

### MirDirection(8,顺时针)

| 名称 | 值 | 备注 |
|---|---|---|
| Up | 0 |  |
| UpRight | 1 |  |
| Right | 2 |  |
| DownRight | 3 |  |
| Down | 4 |  |
| DownLeft | 5 |  |
| Left | 6 |  |
| UpLeft | 7 |  |

### MirAction(17)

| 名称 | 值 | 备注 |
|---|---|---|
| Standing | None |  |
| Moving | None |  |
| Pushed | None |  |
| Attack | None |  |
| RangeAttack | None |  |
| Spell | None |  |
| Harvest | None |  |
| Struck | None |  |
| Die | None |  |
| Dead | None |  |
| Show | None |  |
| Hide | None |  |
| Mount | None |  |
| Mining | None |  |
| Fishing | None |  |
| Taming | None |  |
| Idle | None |  |

### MirAnimation(46)

| 名称 | 值 | 备注 |
|---|---|---|
| Standing | None |  |
| Walking | None |  |
| CreepStanding | None |  |
| CreepWalkSlow | None |  |
| CreepWalkFast | None |  |
| Running | None |  |
| Pushed | None |  |
| Combat1 | None |  |
| Combat2 | None |  |
| Combat3 | None |  |
| Combat4 | None |  |
| Combat5 | None |  |
| Combat6 | None |  |
| Combat7 | None |  |
| Combat8 | None |  |
| Combat9 | None |  |
| Combat10 | None |  |
| Combat11 | None |  |
| Combat12 | None |  |
| Combat13 | None |  |
| Combat14 | None |  |
| Combat15 | None |  |
| Harvest | None |  |
| Stance | None |  |
| Struck | None |  |
| Die | None |  |
| Dead | None |  |
| Skeleton | None |  |
| Show | None |  |
| Hide | None |  |
| HorseStanding | None |  |
| HorseWalking | None |  |
| HorseRunning | None |  |
| HorseStruck | None |  |
| StoneStanding | None |  |
| DragonRepulseStart | None |  |
| DragonRepulseMiddle | None |  |
| DragonRepulseEnd | None |  |
| ChannellingStart | None |  |
| ChannellingMiddle | None |  |
| ChannellingEnd | None |  |
| FishingCast | None |  |
| FishingWait | None |  |
| FishingReel | None |  |
| TamingCast | None |  |
| TamingWait | None |  |

### ObjectType(6)

| 名称 | 值 | 备注 |
|---|---|---|
| None | None | Error |
| Player | None |  |
| Item | None |  |
| NPC | None |  |
| Spell | None |  |
| Monster | None |  |

### Element(8)

| 名称 | 值 | 备注 |
|---|---|---|
| None | None |  |
| Fire | None |  |
| Ice | None |  |
| Lightning | None |  |
| Wind | None |  |
| Holy | None |  |
| Dark | None |  |
| Phantom | None |  |

### MirClass(4)

| 名称 | 值 | 备注 |
|---|---|---|
| Warrior | 0 |  |
| Wizard | 1 |  |
| Taoist | 2 |  |
| Assassin | 3 |  |

### QuestIcon(4)

| 名称 | 值 | 备注 |
|---|---|---|
| None | 0 |  |
| New | 1 |  |
| Incomplete | 2 |  |
| Complete | 3 |  |

### HorseType(7)

| 名称 | 值 | 备注 |
|---|---|---|
| None | 0 |  |
| Brown | 1 |  |
| White | 2 |  |
| Red | 3 |  |
| Black | 4 |  |
| WhiteUnicorn | 5 |  |
| RedUnicorn | 6 |  |

### PoisonType(17,位标志)

| 名称 | 值 | 备注 |
|---|---|---|
| None | 0 |  |
| Green | 1 << 0 | Tick damage, displays green |
| Red | 1 << 1 | Increases damage received by 20%, displays red |
| Slow | 1 << 2 | Reduces attackTime, actionTime, 100ms per value, displays blue |
| Paralysis | 1 << 3 | Stops movement, physical and magic attacks (all races), displays grey |
| WraithGrip | 1 << 4 | Stops shoulderdash, movement, displays effect (needs code revisiting) |
| HellFire | 1 << 5 | Tick damage, no colour |
| Silenced | 1 << 6 | Stops movement (all races), physical and magic attacks (monster), displays effect |
| Abyss | 1 << 7 | Reduces monster viewrange, displays blinding effect (player) |
| Parasite | 1 << 8 | Tick damage, explosion, ignores transparency (monster), displays effect |
| Neutralize | 1 << 9 | Stops attackTime, slows actionTime, displays effect (needs code revisiting) |
| Fear | 1 << 10 | Stops attack (monster), forces runaway (monster), displays effect |
| Burn | 1 << 11 | Tick damage, displays effect |
| Containment | 1 << 12 | Tick damage, stops movement, displays effect |
| Chain | 1 << 13 | Tick damage, limits movement, displays effect |
| Hemorrhage | 1 << 14 | Tick damage, stops recovery, displays effect |
| Binding | 1 << 15 | Tick damage, stops movement, displays effect |

### BuffType(64)

| 名称 | 值 | 备注 |
|---|---|---|
| None | None |  |
| Server | 1 |  |
| HuntGold | 2 |  |
| Observable | 3 |  |
| Brown | 4 |  |
| PKPoint | 5 |  |
| PvPCurse | 6 |  |
| Redemption | 7 |  |
| Companion | 8 |  |
| Castle | 9 |  |
| ItemBuff | 10 |  |
| ItemBuffPermanent | 11 |  |
| Ranking | 12 |  |
| Developer | 13 |  |
| Veteran | 14 |  |
| MapEffect | 15 |  |
| InstanceEffect | 16 |  |
| Guild | 17 |  |
| DeathDrops | 18 |  |
| Fame | 19 |  |
| RedGem | 20 |  |
| BlueGem | 21 |  |
| CursedGem | 22 |  |
| Defiance | 100 |  |
| Might | 101 |  |
| Endurance | 102 |  |
| ReflectDamage | 103 |  |
| Invincibility | 104 |  |
| DefensiveBlow | 105 |  |
| Dash | 106 |  |
| ElementalSwords | 107 |  |
| Renounce | 200 |  |
| MagicShield | 201 |  |
| JudgementOfHeaven | 202 |  |
| ElementalHurricane | 203 |  |
| SuperiorMagicShield | 204 |  |
| FrostBite | 205 |  |
| Tornado | 206 |  |
| Heal | 300 |  |
| Invisibility | 301 |  |
| MagicResistance | 302 |  |
| Resilience | 303 |  |
| ElementalSuperiority | 304 |  |
| BloodLust | 305 |  |
| StrengthOfFaith | 306 |  |
| CelestialLight | 307 |  |
| Transparency | 308 |  |
| LifeSteal | 309 |  |
| Spiritualism | 310 |  |
| SoulResonance | 311 |  |
| PoisonousCloud | 400 |  |
| FullBloom | 401 |  |
| WhiteLotus | 402 |  |
| RedLotus | 403 |  |
| Cloak | 404 |  |
| GhostWalk | 405 |  |
| TheNewBeginning | 406 |  |
| DarkConversion | 407 |  |
| DragonRepulse | 408 |  |
| Evasion | 409 |  |
| RagingWind | 410 |  |
| LastStand | 411 |  |
| Concentration | 412 |  |
| MagicWeakness | 500 |  |

### MagicEffect(29)

| 名称 | 值 | 备注 |
|---|---|---|
| ReflectDamage | None |  |
| Assault | None |  |
| ElementalSwords | None |  |
| DefensiveBlow | None |  |
| HundredFist | None |  |
| MagicShield | None |  |
| MagicShieldStruck | None |  |
| SuperiorMagicShield | None |  |
| SuperiorMagicShieldStruck | None |  |
| ElementalHurricane | None |  |
| FrostBite | None |  |
| Burn | None |  |
| CelestialLight | None |  |
| CelestialLightStruck | None |  |
| Parasite | None |  |
| Neutralize | None |  |
| WraithGrip | None |  |
| LifeSteal | None |  |
| Silence | None |  |
| Blind | None |  |
| Fear | None |  |
| Abyss | None |  |
| DragonRepulse | None |  |
| Containment | None |  |
| Chain | None |  |
| Hemorrhage | None |  |
| Binding | None |  |
| Ranking | None |  |
| Developer | None |  |

## 1.8 元素颜色常量(Globals.cs:47-59)

| 常量 | Color |
|---|---|
| NoneColour | White |
| FireColour | OrangeRed |
| IceColour | PaleTurquoise |
| LightningColour | LightSkyBlue |
| WindColour | LightSeaGreen |
| HolyColour | DarkKhaki |
| DarkColour | SaddleBrown |
| PhantomColour | Purple |
| BrownNameColour | Brown |
| RedNameColour | Red |
| PlayerLightColour | FromArgb(120, 255, 255, 255) |

其他相关常量:`MagicRange = 10`、`MagicMaxLevel = 4`、`TamingDistance = 9`、`ShurikenLibraryWeaponShape = 33`。


---

# 第 2 章 LibraryFile → 库路径

## 2.1 LibraryFile 枚举(316 成员:None + 315 具名)

全部为隐式赋值(按声明顺序 0,1,2…;None 无值)。下表"序号"为声明顺序。

| 名称 | 序号 |
|---|---|
| None | None |
| Interface1c | None |
| Interface1cExtended | None |
| Interface | None |
| GameInter | None |
| GameInter2 | None |
| Equip | None |
| EquipEffect_UI | None |
| EquipEffect_Part | None |
| EquipEffect_Full | None |
| EquipEffect_FullEx1 | None |
| EquipEffect_FullEx2 | None |
| EquipEffect_FullEx3 | None |
| ProgUse | None |
| StoreItem | None |
| Inventory | None |
| Ground | None |
| NPC | None |
| MiniMap | None |
| MiniMap2 | None |
| MagicIcon | None |
| CBIcon | None |
| QuestIcon | None |
| MiniGames | None |
| CastleFlag | None |
| MiniMapIcon | None |
| Background | None |
| NPCImage | None |
| MonImage | None |
| PEquipB1 | None |
| PEquipH1 | None |
| M_Hum | None |
| M_HumEx1 | None |
| M_HumEx2 | None |
| M_HumEx3 | None |
| M_HumEx4 | None |
| M_HumEx10 | None |
| M_HumEx11 | None |
| M_HumEx12 | None |
| M_HumEx13 | None |
| M_HumCx1 | None |
| M_Hair | None |
| WM_Hum | None |
| WM_HumEx1 | None |
| WM_HumEx2 | None |
| WM_HumEx3 | None |
| WM_HumEx4 | None |
| WM_HumEx10 | None |
| WM_HumEx11 | None |
| WM_HumEx12 | None |
| WM_HumEx13 | None |
| WM_HumCx1 | None |
| WM_Hair | None |
| M_HumA | None |
| M_HumAEx1 | None |
| M_HumAEx2 | None |
| M_HumAEx3 | None |
| M_HumACx1 | None |
| M_HairA | None |
| WM_HumA | None |
| WM_HumAEx1 | None |
| WM_HumAEx2 | None |
| WM_HumAEx3 | None |
| WM_HumACx1 | None |
| WM_HairA | None |
| M_Costume | None |
| M_CostumeA | None |
| M_CostumeEx1 | None |
| WM_Costume | None |
| WM_CostumeA | None |
| WM_CostumeEx1 | None |
| Horse | None |
| HorseS | None |
| HorseIron | None |
| HorseSilver | None |
| HorseGold | None |
| HorseBlue | None |
| HorseDark | None |
| HorseDarkEffect | None |
| HorseRoyal | None |
| HorseRoyalEffect | None |
| HorseBlueDragon | None |
| HorseBlueDragonEffect | None |
| M_Weapon1 | None |
| M_Weapon2 | None |
| M_Weapon3 | None |
| M_Weapon4 | None |
| M_Weapon5 | None |
| M_Weapon6 | None |
| M_Weapon7 | None |
| M_Weapon10 | None |
| M_Weapon11 | None |
| M_Weapon12 | None |
| M_Weapon13 | None |
| M_Weapon14 | None |
| M_Weapon15 | None |
| M_Weapon16 | None |
| WM_Weapon1 | None |
| WM_Weapon2 | None |
| WM_Weapon3 | None |
| WM_Weapon4 | None |
| WM_Weapon5 | None |
| WM_Weapon6 | None |
| WM_Weapon7 | None |
| WM_Weapon10 | None |
| WM_Weapon11 | None |
| WM_Weapon12 | None |
| WM_Weapon13 | None |
| WM_Weapon14 | None |
| WM_Weapon15 | None |
| WM_Weapon16 | None |
| M_WeaponADL1 | None |
| M_WeaponADL2 | None |
| M_WeaponADL6 | None |
| M_WeaponADR1 | None |
| M_WeaponADR2 | None |
| M_WeaponADR6 | None |
| M_WeaponAOH1 | None |
| M_WeaponAOH2 | None |
| M_WeaponAOH3 | None |
| M_WeaponAOH4 | None |
| M_WeaponAOH5 | None |
| M_WeaponAOH6 | None |
| WM_WeaponADL1 | None |
| WM_WeaponADL2 | None |
| WM_WeaponADL6 | None |
| WM_WeaponADR1 | None |
| WM_WeaponADR2 | None |
| WM_WeaponADR6 | None |
| WM_WeaponAOH1 | None |
| WM_WeaponAOH2 | None |
| WM_WeaponAOH3 | None |
| WM_WeaponAOH4 | None |
| WM_WeaponAOH5 | None |
| WM_WeaponAOH6 | None |
| M_Shield1 | None |
| M_Shield2 | None |
| WM_Shield1 | None |
| WM_Shield2 | None |
| M_Helmet1 | None |
| M_Helmet2 | None |
| M_Helmet3 | None |
| M_Helmet4 | None |
| M_Helmet5 | None |
| M_Helmet11 | None |
| M_Helmet12 | None |
| M_Helmet13 | None |
| M_Helmet14 | None |
| M_HelmetCx1 | None |
| WM_Helmet1 | None |
| WM_Helmet2 | None |
| WM_Helmet3 | None |
| WM_Helmet4 | None |
| WM_Helmet5 | None |
| WM_Helmet11 | None |
| WM_Helmet12 | None |
| WM_Helmet13 | None |
| WM_Helmet14 | None |
| WM_HelmetCx1 | None |
| M_HelmetA1 | None |
| M_HelmetA2 | None |
| M_HelmetA3 | None |
| M_HelmetA4 | None |
| M_HelmetACx1 | None |
| WM_HelmetA1 | None |
| WM_HelmetA2 | None |
| WM_HelmetA3 | None |
| WM_HelmetA4 | None |
| WM_HelmetACx1 | None |
| MonMagic | None |
| MonMagicEx | None |
| MonMagicEx2 | None |
| MonMagicEx3 | None |
| MonMagicEx4 | None |
| MonMagicEx5 | None |
| MonMagicEx6 | None |
| MonMagicEx7 | None |
| MonMagicEx8 | None |
| MonMagicEx9 | None |
| MonMagicEx19 | None |
| MonMagicEx20 | None |
| MonMagicEx21 | None |
| MonMagicEx22 | None |
| MonMagicEx23 | None |
| MonMagicEx26 | None |
| Mon_1 | None |
| Mon_2 | None |
| Mon_3 | None |
| Mon_4 | None |
| Mon_5 | None |
| Mon_6 | None |
| Mon_7 | None |
| Mon_8 | None |
| Mon_9 | None |
| Mon_10 | None |
| Mon_11 | None |
| Mon_12 | None |
| Mon_13 | None |
| Mon_14 | None |
| Mon_15 | None |
| Mon_16 | None |
| Mon_17 | None |
| Mon_18 | None |
| Mon_19 | None |
| Mon_20 | None |
| Mon_21 | None |
| Mon_22 | None |
| Mon_23 | None |
| Mon_24 | None |
| Mon_25 | None |
| Mon_26 | None |
| Mon_27 | None |
| Mon_28 | None |
| Mon_29 | None |
| Mon_30 | None |
| Mon_31 | None |
| Mon_32 | None |
| Mon_33 | None |
| Mon_34 | None |
| Mon_35 | None |
| Mon_36 | None |
| Mon_37 | None |
| Mon_38 | None |
| Mon_39 | None |
| Mon_40 | None |
| Mon_41 | None |
| Mon_42 | None |
| Mon_43 | None |
| Mon_44 | None |
| Mon_45 | None |
| Mon_46 | None |
| Mon_47 | None |
| Mon_48 | None |
| Mon_49 | None |
| Mon_50 | None |
| Mon_51 | None |
| Mon_52 | None |
| Mon_53 | None |
| Mon_54 | None |
| Mon_55 | None |
| Mon_56 | None |
| Mon_57 | None |
| Magic | None |
| MagicEx | None |
| MagicEx2 | None |
| MagicEx3 | None |
| MagicEx4 | None |
| MagicEx5 | None |
| MagicEx6 | None |
| MagicEx7 | None |
| MagicEx8 | None |
| MagicEx9 | None |
| MagicEx10 | None |
| MagicEx11 | None |
| Animationsc | None |
| Cliffsc | None |
| Dungeonsc | None |
| Furnituresc | None |
| Housesc | None |
| Innersc | None |
| Object1c | None |
| Object2c | None |
| SmObjectsc | None |
| SmTilesc | None |
| Tiles5c | None |
| Tiles30c | None |
| Tilesc | None |
| Wallsc | None |
| Forest_Animationsc | None |
| Forest_Cliffsc | None |
| Forest_Dungeonsc | None |
| Forest_Furnituresc | None |
| Forest_Housesc | None |
| Forest_Innersc | None |
| Forest_SmObjectsc | None |
| Forest_SmTilesc | None |
| Forest_Tiles5c | None |
| Forest_Tiles30c | None |
| Forest_Tilesc | None |
| Forest_Wallsc | None |
| Sand_Animationsc | None |
| Sand_Cliffsc | None |
| Sand_Dungeonsc | None |
| Sand_Furnituresc | None |
| Sand_Housesc | None |
| Sand_Innersc | None |
| Sand_SmObjectsc | None |
| Sand_SmTilesc | None |
| Sand_Tiles5c | None |
| Sand_Tiles30c | None |
| Sand_Tilesc | None |
| Sand_Wallsc | None |
| Snow_Animationsc | None |
| Snow_Cliffsc | None |
| Snow_Dungeonsc | None |
| Snow_Furnituresc | None |
| Snow_Housesc | None |
| Snow_Innersc | None |
| Snow_SmObjectsc | None |
| Snow_SmTilesc | None |
| Snow_Tiles5c | None |
| Snow_Tiles30c | None |
| Snow_Tilesc | None |
| Snow_Wallsc | None |
| Wood_Animationsc | None |
| Wood_Cliffsc | None |
| Wood_Dungeonsc | None |
| Wood_Furnituresc | None |
| Wood_Housesc | None |
| Wood_Innersc | None |
| Wood_SmObjectsc | None |
| Wood_SmTilesc | None |
| Wood_Tiles5c | None |
| Wood_Tiles30c | None |
| Wood_Tilesc | None |
| Wood_Wallsc | None |

## 2.2 LibraryList:库 → 磁盘路径(314 条)

`Library/Libraries.cs` 的 `LibraryList` 字典,索引语法 `[LibraryFile.X] = @"Data\...Zl"`。
**None 与 HorseS 无路径条目**(HorseS 装载时被跳过)。加载逻辑(`Client/Program.cs:48-55`):遍历 `Libraries.LibraryList`,跳过磁盘缺失文件,`CEnvir.LibraryList[LibraryFile] = new MirLibrary(@".\" + path)`。

| 枚举名 | 路径 |
|---|---|
| Interface1c | `Data\Interface1c.Zl` |
| Interface1cExtended | `Data\Interface1c-Extended.Zl` |
| Interface | `Data\Interface.Zl` |
| GameInter | `Data\GameInter.Zl` |
| Equip | `Data\Equip.Zl` |
| EquipEffect_UI | `Data\EquipEffect-UI.Zl` |
| EquipEffect_Part | `Data\EquipEffect-Part.Zl` |
| EquipEffect_Full | `Data\EquipEffect-Full.Zl` |
| EquipEffect_FullEx1 | `Data\EquipEffect-FullEx1.Zl` |
| EquipEffect_FullEx2 | `Data\EquipEffect-FullEx2.Zl` |
| EquipEffect_FullEx3 | `Data\EquipEffect-FullEx3.Zl` |
| ProgUse | `Data\ProgUse.Zl` |
| StoreItem | `Data\StoreItem.Zl` |
| Inventory | `Data\Inventory.Zl` |
| Ground | `Data\Ground.Zl` |
| NPC | `Data\NPC.Zl` |
| GameInter2 | `Data\GameInter2.Zl` |
| MiniMap | `Data\MiniMap.Zl` |
| MiniMap2 | `Data\MiniMap2.Zl` |
| CastleFlag | `Data\Flag.Zl` |
| MiniMapIcon | `Data\MiniMapIcon.Zl` |
| Background | `Data\Background.Zl` |
| MagicIcon | `Data\MIcon.Zl` |
| CBIcon | `Data\CBIcons.Zl` |
| QuestIcon | `Data\QuestIcons.Zl` |
| MiniGames | `Data\MiniGames.Zl` |
| NPCImage | `Data\NPCface.Zl` |
| MonImage | `Data\MonImg.Zl` |
| PEquipB1 | `Data\PEquipB1.Zl` |
| PEquipH1 | `Data\PEquipH1.Zl` |
| M_Hum | `Data\M-Hum.Zl` |
| M_HumEx1 | `Data\M-HumEx1.Zl` |
| M_HumEx2 | `Data\M-HumEx2.Zl` |
| M_HumEx3 | `Data\M-HumEx3.Zl` |
| M_HumEx4 | `Data\M-HumEx4.Zl` |
| M_HumEx10 | `Data\M-HumEx10.Zl` |
| M_HumEx11 | `Data\M-HumEx11.Zl` |
| M_HumEx12 | `Data\M-HumEx12.Zl` |
| M_HumEx13 | `Data\M-HumEx13.Zl` |
| WM_Hum | `Data\WM-Hum.Zl` |
| WM_HumEx1 | `Data\WM-HumEx1.Zl` |
| WM_HumEx2 | `Data\WM-HumEx2.Zl` |
| WM_HumEx3 | `Data\WM-HumEx3.Zl` |
| WM_HumEx4 | `Data\WM-HumEx4.Zl` |
| WM_HumEx10 | `Data\WM-HumEx10.Zl` |
| WM_HumEx11 | `Data\WM-HumEx11.Zl` |
| WM_HumEx12 | `Data\WM-HumEx12.Zl` |
| WM_HumEx13 | `Data\WM-HumEx13.Zl` |
| M_HumCx1 | `Data\M-HumCx1.Zl` |
| WM_HumCx1 | `Data\WM-HumCx1.Zl` |
| M_Hair | `Data\M-Hair.Zl` |
| WM_Hair | `Data\WM-Hair.Zl` |
| M_HumA | `Data\M-HumA.Zl` |
| M_HumAEx1 | `Data\M-HumAEx1.Zl` |
| M_HumAEx2 | `Data\M-HumAEx2.Zl` |
| M_HumAEx3 | `Data\M-HumAEx3.Zl` |
| WM_HumA | `Data\WM-HumA.Zl` |
| WM_HumAEx1 | `Data\WM-HumAEx1.Zl` |
| WM_HumAEx2 | `Data\WM-HumAEx2.Zl` |
| WM_HumAEx3 | `Data\WM-HumAEx3.Zl` |
| M_HumACx1 | `Data\M-HumACx1.Zl` |
| WM_HumACx1 | `Data\WM-HumACx1.Zl` |
| M_HairA | `Data\M-HairA.Zl` |
| WM_HairA | `Data\WM-HairA.Zl` |
| M_Costume | `Data\M-Costume.Zl` |
| M_CostumeA | `Data\M-CostumeA.Zl` |
| M_CostumeEx1 | `Data\M-CostumeEx1.Zl` |
| WM_Costume | `Data\WM-Costume.Zl` |
| WM_CostumeA | `Data\WM-CostumeA.Zl` |
| WM_CostumeEx1 | `Data\WM-CostumeEx1.Zl` |
| Horse | `Data\Horse.Zl` |
| HorseIron | `Data\Horse_Iron.Zl` |
| HorseSilver | `Data\Horse_Silver.Zl` |
| HorseGold | `Data\Horse_Golden.Zl` |
| HorseBlue | `Data\Horse_Blue.Zl` |
| HorseDark | `Data\Horse_Dark.Zl` |
| HorseDarkEffect | `Data\Horse_DarkEffect.Zl` |
| HorseRoyal | `Data\Horse_Royal.Zl` |
| HorseRoyalEffect | `Data\Horse_RoyalEffect.Zl` |
| HorseBlueDragon | `Data\Horse_BlueDragon.Zl` |
| HorseBlueDragonEffect | `Data\Horse_BlueDragonEffect.Zl` |
| M_Shield1 | `Data\M-Shield1.Zl` |
| M_Shield2 | `Data\M-Shield2.Zl` |
| WM_Shield1 | `Data\WM-Shield1.Zl` |
| WM_Shield2 | `Data\WM-Shield2.Zl` |
| M_Weapon1 | `Data\M-Weapon1.Zl` |
| M_Weapon2 | `Data\M-Weapon2.Zl` |
| M_Weapon3 | `Data\M-Weapon3.Zl` |
| M_Weapon4 | `Data\M-Weapon4.Zl` |
| M_Weapon5 | `Data\M-Weapon5.Zl` |
| M_Weapon6 | `Data\M-Weapon6.Zl` |
| M_Weapon7 | `Data\M-Weapon7.Zl` |
| M_Weapon10 | `Data\M-Weapon10.Zl` |
| M_Weapon11 | `Data\M-Weapon11.Zl` |
| M_Weapon12 | `Data\M-Weapon12.Zl` |
| M_Weapon13 | `Data\M-Weapon13.Zl` |
| M_Weapon14 | `Data\M-Weapon14.Zl` |
| M_Weapon15 | `Data\M-Weapon15.Zl` |
| M_Weapon16 | `Data\M-Weapon16.Zl` |
| WM_Weapon1 | `Data\WM-Weapon1.Zl` |
| WM_Weapon2 | `Data\WM-Weapon2.Zl` |
| WM_Weapon3 | `Data\WM-Weapon3.Zl` |
| WM_Weapon4 | `Data\WM-Weapon4.Zl` |
| WM_Weapon5 | `Data\WM-Weapon5.Zl` |
| WM_Weapon6 | `Data\WM-Weapon6.Zl` |
| WM_Weapon7 | `Data\WM-Weapon7.Zl` |
| WM_Weapon10 | `Data\WM-Weapon10.Zl` |
| WM_Weapon11 | `Data\WM-Weapon11.Zl` |
| WM_Weapon12 | `Data\WM-Weapon12.Zl` |
| WM_Weapon13 | `Data\WM-Weapon13.Zl` |
| WM_Weapon14 | `Data\WM-Weapon14.Zl` |
| WM_Weapon15 | `Data\WM-Weapon15.Zl` |
| WM_Weapon16 | `Data\WM-Weapon16.Zl` |
| M_WeaponADL1 | `Data\M-WeaponADL1.Zl` |
| M_WeaponADL2 | `Data\M-WeaponADL2.Zl` |
| M_WeaponADL6 | `Data\M-WeaponADL6.Zl` |
| M_WeaponADR1 | `Data\M-WeaponADR1.Zl` |
| M_WeaponADR2 | `Data\M-WeaponADR2.Zl` |
| M_WeaponADR6 | `Data\M-WeaponADR6.Zl` |
| M_WeaponAOH1 | `Data\M-WeaponAOH1.Zl` |
| M_WeaponAOH2 | `Data\M-WeaponAOH2.Zl` |
| M_WeaponAOH3 | `Data\M-WeaponAOH3.Zl` |
| M_WeaponAOH4 | `Data\M-WeaponAOH4.Zl` |
| M_WeaponAOH5 | `Data\M-WeaponAOH5.Zl` |
| M_WeaponAOH6 | `Data\M-WeaponAOH6.Zl` |
| WM_WeaponADL1 | `Data\WM-WeaponADL1.Zl` |
| WM_WeaponADL2 | `Data\WM-WeaponADL2.Zl` |
| WM_WeaponADL6 | `Data\WM-WeaponADL6.Zl` |
| WM_WeaponADR1 | `Data\WM-WeaponADR1.Zl` |
| WM_WeaponADR2 | `Data\WM-WeaponADR2.Zl` |
| WM_WeaponADR6 | `Data\WM-WeaponADR6.Zl` |
| WM_WeaponAOH1 | `Data\WM-WeaponAOH1.Zl` |
| WM_WeaponAOH2 | `Data\WM-WeaponAOH2.Zl` |
| WM_WeaponAOH3 | `Data\WM-WeaponAOH3.Zl` |
| WM_WeaponAOH4 | `Data\WM-WeaponAOH4.Zl` |
| WM_WeaponAOH5 | `Data\WM-WeaponAOH5.Zl` |
| WM_WeaponAOH6 | `Data\WM-WeaponAOH6.Zl` |
| M_Helmet1 | `Data\M-Helmet1.Zl` |
| M_Helmet2 | `Data\M-Helmet2.Zl` |
| M_Helmet3 | `Data\M-Helmet3.Zl` |
| M_Helmet4 | `Data\M-Helmet4.Zl` |
| M_Helmet5 | `Data\M-Helmet5.Zl` |
| M_Helmet11 | `Data\M-Helmet11.Zl` |
| M_Helmet12 | `Data\M-Helmet12.Zl` |
| M_Helmet13 | `Data\M-Helmet13.Zl` |
| M_Helmet14 | `Data\M-Helmet14.Zl` |
| WM_Helmet1 | `Data\WM-Helmet1.Zl` |
| WM_Helmet2 | `Data\WM-Helmet2.Zl` |
| WM_Helmet3 | `Data\WM-Helmet3.Zl` |
| WM_Helmet4 | `Data\WM-Helmet4.Zl` |
| WM_Helmet5 | `Data\WM-Helmet5.Zl` |
| WM_Helmet11 | `Data\WM-Helmet11.Zl` |
| WM_Helmet12 | `Data\WM-Helmet12.Zl` |
| WM_Helmet13 | `Data\WM-Helmet13.Zl` |
| WM_Helmet14 | `Data\WM-Helmet14.Zl` |
| M_HelmetCx1 | `Data\M-HelmetCx1.Zl` |
| WM_HelmetCx1 | `Data\WM-HelmetCx1.Zl` |
| M_HelmetA1 | `Data\M-HelmetA1.Zl` |
| M_HelmetA2 | `Data\M-HelmetA2.Zl` |
| M_HelmetA3 | `Data\M-HelmetA3.Zl` |
| M_HelmetA4 | `Data\M-HelmetA4.Zl` |
| WM_HelmetA1 | `Data\WM-HelmetA1.Zl` |
| WM_HelmetA2 | `Data\WM-HelmetA2.Zl` |
| WM_HelmetA3 | `Data\WM-HelmetA3.Zl` |
| WM_HelmetA4 | `Data\WM-HelmetA4.Zl` |
| M_HelmetACx1 | `Data\M-HelmetACx1.Zl` |
| WM_HelmetACx1 | `Data\WM-HelmetACx1.Zl` |
| MonMagic | `Data\MonMagic.Zl` |
| MonMagicEx | `Data\MonMagicEx.Zl` |
| MonMagicEx2 | `Data\MonMagicEx2.Zl` |
| MonMagicEx3 | `Data\MonMagicEx3.Zl` |
| MonMagicEx4 | `Data\MonMagicEx4.Zl` |
| MonMagicEx5 | `Data\MonMagicEx5.Zl` |
| MonMagicEx6 | `Data\MonMagicEx6.Zl` |
| MonMagicEx7 | `Data\MonMagicEx7.Zl` |
| MonMagicEx8 | `Data\MonMagicEx8.Zl` |
| MonMagicEx9 | `Data\MonMagicEx9.Zl` |
| MonMagicEx19 | `Data\MonMagicEx19.Zl` |
| MonMagicEx20 | `Data\MonMagicEx20.Zl` |
| MonMagicEx21 | `Data\MonMagicEx21.Zl` |
| MonMagicEx22 | `Data\MonMagicEx22.Zl` |
| MonMagicEx23 | `Data\MonMagicEx23.Zl` |
| MonMagicEx26 | `Data\MonMagicEx26.Zl` |
| Mon_1 | `Data\Mon-1.Zl` |
| Mon_2 | `Data\Mon-2.Zl` |
| Mon_3 | `Data\Mon-3.Zl` |
| Mon_4 | `Data\Mon-4.Zl` |
| Mon_5 | `Data\Mon-5.Zl` |
| Mon_6 | `Data\Mon-6.Zl` |
| Mon_7 | `Data\Mon-7.Zl` |
| Mon_8 | `Data\Mon-8.Zl` |
| Mon_9 | `Data\Mon-9.Zl` |
| Mon_10 | `Data\Mon-10.Zl` |
| Mon_11 | `Data\Mon-11.Zl` |
| Mon_12 | `Data\Mon-12.Zl` |
| Mon_13 | `Data\Mon-13.Zl` |
| Mon_14 | `Data\Mon-14.Zl` |
| Mon_15 | `Data\Mon-15.Zl` |
| Mon_16 | `Data\Mon-16.Zl` |
| Mon_17 | `Data\Mon-17.Zl` |
| Mon_18 | `Data\Mon-18.Zl` |
| Mon_19 | `Data\Mon-19.Zl` |
| Mon_20 | `Data\Mon-20.Zl` |
| Mon_21 | `Data\Mon-21.Zl` |
| Mon_22 | `Data\Mon-22.Zl` |
| Mon_23 | `Data\Mon-23.Zl` |
| Mon_24 | `Data\Mon-24.Zl` |
| Mon_25 | `Data\Mon-25.Zl` |
| Mon_26 | `Data\Mon-26.Zl` |
| Mon_27 | `Data\Mon-27.Zl` |
| Mon_28 | `Data\Mon-28.Zl` |
| Mon_29 | `Data\Mon-29.Zl` |
| Mon_30 | `Data\Mon-30.Zl` |
| Mon_31 | `Data\Mon-31.Zl` |
| Mon_32 | `Data\Mon-32.Zl` |
| Mon_33 | `Data\Mon-33.Zl` |
| Mon_34 | `Data\Mon-34.Zl` |
| Mon_35 | `Data\Mon-35.Zl` |
| Mon_36 | `Data\Mon-36.Zl` |
| Mon_37 | `Data\Mon-37.Zl` |
| Mon_38 | `Data\Mon-38.Zl` |
| Mon_39 | `Data\Mon-39.Zl` |
| Mon_40 | `Data\Mon-40.Zl` |
| Mon_41 | `Data\Mon-41.Zl` |
| Mon_42 | `Data\Mon-42.Zl` |
| Mon_43 | `Data\Mon-43.Zl` |
| Mon_44 | `Data\Mon-44.Zl` |
| Mon_45 | `Data\Mon-45.Zl` |
| Mon_46 | `Data\Mon-46.Zl` |
| Mon_47 | `Data\Mon-47.Zl` |
| Mon_48 | `Data\Mon-48.Zl` |
| Mon_49 | `Data\Mon-49.Zl` |
| Mon_50 | `Data\Mon-50.Zl` |
| Mon_51 | `Data\Mon-51.Zl` |
| Mon_52 | `Data\Mon-52.Zl` |
| Mon_53 | `Data\Mon-53.Zl` |
| Mon_54 | `Data\Mon-54.Zl` |
| Mon_55 | `Data\Mon-55.Zl` |
| Mon_56 | `Data\Mon-56.Zl` |
| Mon_57 | `Data\Mon-57.Zl` |
| Magic | `Data\Magic.Zl` |
| MagicEx | `Data\MagicEx.Zl` |
| MagicEx2 | `Data\MagicEx2.Zl` |
| MagicEx3 | `Data\MagicEx3.Zl` |
| MagicEx4 | `Data\MagicEx4.Zl` |
| MagicEx5 | `Data\MagicEx5.Zl` |
| MagicEx6 | `Data\MagicEx6.Zl` |
| MagicEx7 | `Data\MagicEx7.Zl` |
| MagicEx8 | `Data\MagicEx8.Zl` |
| MagicEx9 | `Data\MagicEx9.Zl` |
| MagicEx10 | `Data\MagicEx10.Zl` |
| MagicEx11 | `Data\MagicEx11.Zl` |
| Animationsc | `Data\Map Data\Animationsc.Zl` |
| Cliffsc | `Data\Map Data\Cliffsc.Zl` |
| Dungeonsc | `Data\Map Data\Dungeonsc.Zl` |
| Furnituresc | `Data\Map Data\Furnituresc.Zl` |
| Housesc | `Data\Map Data\Housesc.Zl` |
| Innersc | `Data\Map Data\Innersc.Zl` |
| Object1c | `Data\Map Data\Object1c.Zl` |
| Object2c | `Data\Map Data\Object2c.Zl` |
| SmObjectsc | `Data\Map Data\SmObjectsc.Zl` |
| SmTilesc | `Data\Map Data\SmTilesc.Zl` |
| Tiles5c | `Data\Map Data\Tiles5c.Zl` |
| Tiles30c | `Data\Map Data\Tiles30c.Zl` |
| Tilesc | `Data\Map Data\Tilesc.Zl` |
| Wallsc | `Data\Map Data\Wallsc.Zl` |
| Forest_Animationsc | `Data\Map Data\Forest\Animationsc.Zl` |
| Forest_Cliffsc | `Data\Map Data\Forest\Cliffsc.Zl` |
| Forest_Dungeonsc | `Data\Map Data\Forest\Dungeonsc.Zl` |
| Forest_Furnituresc | `Data\Map Data\Forest\Furnituresc.Zl` |
| Forest_Housesc | `Data\Map Data\Forest\Housesc.Zl` |
| Forest_Innersc | `Data\Map Data\Forest\Innersc.Zl` |
| Forest_SmObjectsc | `Data\Map Data\Forest\SmObjectsc.Zl` |
| Forest_SmTilesc | `Data\Map Data\Forest\SmTilesc.Zl` |
| Forest_Tiles5c | `Data\Map Data\Forest\Tiles5c.Zl` |
| Forest_Tiles30c | `Data\Map Data\Forest\Tiles30c.Zl` |
| Forest_Tilesc | `Data\Map Data\Forest\Tilesc.Zl` |
| Forest_Wallsc | `Data\Map Data\Forest\Wallsc.Zl` |
| Sand_Animationsc | `Data\Map Data\Sand\Animationsc.Zl` |
| Sand_Cliffsc | `Data\Map Data\Sand\Cliffsc.Zl` |
| Sand_Dungeonsc | `Data\Map Data\Sand\Dungeonsc.Zl` |
| Sand_Furnituresc | `Data\Map Data\Sand\Furnituresc.Zl` |
| Sand_Housesc | `Data\Map Data\Sand\Housesc.Zl` |
| Sand_Innersc | `Data\Map Data\Sand\Innersc.Zl` |
| Sand_SmObjectsc | `Data\Map Data\Sand\SmObjectsc.Zl` |
| Sand_SmTilesc | `Data\Map Data\Sand\SmTilesc.Zl` |
| Sand_Tiles5c | `Data\Map Data\Sand\Tiles5c.Zl` |
| Sand_Tiles30c | `Data\Map Data\Sand\Tiles30c.Zl` |
| Sand_Tilesc | `Data\Map Data\Sand\Tilesc.Zl` |
| Sand_Wallsc | `Data\Map Data\Sand\Wallsc.Zl` |
| Snow_Animationsc | `Data\Map Data\Snow\Animationsc.Zl` |
| Snow_Cliffsc | `Data\Map Data\Snow\Cliffsc.Zl` |
| Snow_Dungeonsc | `Data\Map Data\Snow\Dungeonsc.Zl` |
| Snow_Furnituresc | `Data\Map Data\Snow\Furnituresc.Zl` |
| Snow_Housesc | `Data\Map Data\Snow\Housesc.Zl` |
| Snow_Innersc | `Data\Map Data\Snow\Innersc.Zl` |
| Snow_SmObjectsc | `Data\Map Data\Snow\SmObjectsc.Zl` |
| Snow_SmTilesc | `Data\Map Data\Snow\SmTilesc.Zl` |
| Snow_Tiles5c | `Data\Map Data\Snow\Tiles5c.Zl` |
| Snow_Tiles30c | `Data\Map Data\Snow\Tiles30c.Zl` |
| Snow_Tilesc | `Data\Map Data\Snow\Tilesc.Zl` |
| Snow_Wallsc | `Data\Map Data\Snow\Wallsc.Zl` |
| Wood_Animationsc | `Data\Map Data\Wood\Animationsc.Zl` |
| Wood_Cliffsc | `Data\Map Data\Wood\Cliffsc.Zl` |
| Wood_Dungeonsc | `Data\Map Data\Wood\Dungeonsc.Zl` |
| Wood_Furnituresc | `Data\Map Data\Wood\Furnituresc.Zl` |
| Wood_Housesc | `Data\Map Data\Wood\Housesc.Zl` |
| Wood_Innersc | `Data\Map Data\Wood\Innersc.Zl` |
| Wood_SmObjectsc | `Data\Map Data\Wood\SmObjectsc.Zl` |
| Wood_SmTilesc | `Data\Map Data\Wood\SmTilesc.Zl` |
| Wood_Tiles5c | `Data\Map Data\Wood\Tiles5c.Zl` |
| Wood_Tiles30c | `Data\Map Data\Wood\Tiles30c.Zl` |
| Wood_Tilesc | `Data\Map Data\Wood\Tilesc.Zl` |
| Wood_Wallsc | `Data\Map Data\Wood\Wallsc.Zl` |

## 2.3 KROrder:地图库索引(62 条)

`.map` 单元格里的 `MiddleFile`/`FrontFile`/`BackFile` 字节(0-255)经 `KROrder` 映射为 LibraryFile。键 0-71 有缺号(变体地图集缺少部分库)。布局:0-14 根级(15 个)、15-29 Wood、30-44 Sand、45-59 Snow、60-71 Forest。

| 键 | 库名 |
|---|---|
| 键 | 库名 |
|---|---|
| 0 | Tilesc |
| 1 | Tiles30c |
| 2 | Tiles5c |
| 3 | SmTilesc |
| 4 | Housesc |
| 5 | Cliffsc |
| 6 | Dungeonsc |
| 7 | Innersc |
| 8 | Furnituresc |
| 9 | Wallsc |
| 10 | SmObjectsc |
| 11 | Animationsc |
| 12 | Object1c |
| 13 | Object2c |
| 15 | Wood_Tilesc |
| 16 | Wood_Tiles30c |
| 17 | Wood_Tiles5c |
| 18 | Wood_SmTilesc |
| 19 | Wood_Housesc |
| 20 | Wood_Cliffsc |
| 21 | Wood_Dungeonsc |
| 22 | Wood_Innersc |
| 23 | Wood_Furnituresc |
| 24 | Wood_Wallsc |
| 25 | Wood_SmObjectsc |
| 26 | Wood_Animationsc |
| 30 | Sand_Tilesc |
| 31 | Sand_Tiles30c |
| 32 | Sand_Tiles5c |
| 33 | Sand_SmTilesc |
| 34 | Sand_Housesc |
| 35 | Sand_Cliffsc |
| 36 | Sand_Dungeonsc |
| 37 | Sand_Innersc |
| 38 | Sand_Furnituresc |
| 39 | Sand_Wallsc |
| 40 | Sand_SmObjectsc |
| 41 | Sand_Animationsc |
| 45 | Snow_Tilesc |
| 46 | Snow_Tiles30c |
| 47 | Snow_Tiles5c |
| 48 | Snow_SmTilesc |
| 49 | Snow_Housesc |
| 50 | Snow_Cliffsc |
| 51 | Snow_Dungeonsc |
| 52 | Snow_Innersc |
| 53 | Snow_Furnituresc |
| 54 | Snow_Wallsc |
| 55 | Snow_SmObjectsc |
| 56 | Snow_Animationsc |
| 60 | Forest_Tilesc |
| 61 | Forest_Tiles30c |
| 62 | Forest_Tiles5c |
| 63 | Forest_SmTilesc |
| 64 | Forest_Housesc |
| 65 | Forest_Cliffsc |
| 66 | Forest_Dungeonsc |
| 67 | Forest_Innersc |
| 68 | Forest_Furnituresc |
| 69 | Forest_Wallsc |
| 70 | Forest_SmObjectsc |
| 71 | Forest_Animationsc |

**根级地图库路径**(在 LibraryList 中):`Data\Map Data\{Tilesc,Tiles30c,Tiles5c,SmTilesc,Housesc,Cliffsc,Dungeonsc,Innersc,Furnituresc,Wallsc,SmObjectsc,Animationsc,Object1c,Object2c}.Zl`;变体:`Data\Map Data\{Wood,Sand,Snow,Forest}\*.Zl`(每个变体 12 个,无 Object1c/Object2c)。

> 映射源说明:`BackFile` 只取偶数格(`x%2==0 && y%2==0`);`MiddleFile`/`FrontFile` 查找后**跳过 Tilesc**(索引 0)。

## 2.4 命名不一致陷阱(源码确认)

| 实际库名 | 枚举名 |
|---|---|
| `Horse_Golden` | `HorseGold` |
| `Mon-N`(连字符) | `Mon_N`(下划线) |
| `Flag` | `CastleFlag` |
| `MIcon` | `MagicIcon` |
| `CBIcons` | `CBIcon` |
| `QuestIcons` | `QuestIcon` |
| `NPCface` | `NPCImage` |


---

# 第 3 章 FrameSet 全帧表(94 表 / 560 条目)

`LibraryCore/FrameSet.cs` 定义 96 个静态帧表字段,**94 张被初始化**(`ShinsuBig`、`LobsterSpawn` 声明但恒为 null,死代码;注释掉的旧 `Assassin` 表亦为死代码)。每张表是 `Dictionary<MirAnimation, Frame>`;`Frame(start, count, offset, delay)` + 可选 `Reversed/StaticSpeed/Delays[]`。
帧号公式:`DrawFrame = FrameIndex + StartIndex + OffSet * (int)Direction`。

## 3.1 通用表

### Players(玩家;含 Delays 覆写)

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 10 | 500 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Running | 160 | 6 | 10 | 100 |  |  |  |
| CreepStanding | 1680 | 4 | 10 | 500 |  |  |  |
| CreepWalkFast | 1760 | 6 | 10 | 100 |  |  |  |
| CreepWalkSlow | 1760 | 6 | 10 | 200 |  |  |  |
| Pushed | 240 | 6 | 10 | 50 | Y | Y |  |
| Stance | 400 | 3 | 10 | 500 |  |  |  |
| Harvest | 480 | 2 | 10 | 300 |  |  |  |
| Combat1 | 560 | 5 | 10 | 100 |  |  | {"1": 200} |
| Combat2 | 640 | 5 | 10 | 100 |  |  | {"3": 200} |
| Combat3 | 720 | 6 | 10 | 100 |  |  |  |
| Combat4 | 800 | 6 | 10 | 100 |  |  |  |
| Combat5 | 880 | 10 | 10 | 60 |  |  |  |
| Combat6 | 960 | 10 | 10 | 60 |  |  |  |
| Combat7 | 1040 | 10 | 10 | 100 |  |  |  |
| Combat8 | 1120 | 6 | 10 | 50 |  | Y |  |
| Combat9 | 1200 | 10 | 10 | 100 |  |  |  |
| Combat10 | 1280 | 10 | 10 | 60 |  |  |  |
| Combat11 | 1360 | 10 | 10 | 60 |  |  |  |
| Combat12 | 1440 | 10 | 10 | 60 |  |  |  |
| Combat13 | 1520 | 6 | 10 | 100 |  |  |  |
| Combat14 | 1600 | 8 | 10 | 100 |  |  |  |
| Combat15 | 400 | 3 | 10 | 200 |  |  |  |
| DragonRepulseStart | 1600 | 6 | 10 | 100 |  |  |  |
| DragonRepulseMiddle | 1605 | 1 | 10 | 1000 |  |  |  |
| DragonRepulseEnd | 1606 | 2 | 10 | 100 |  |  |  |
| Struck | 1840 | 3 | 10 | 100 |  |  |  |
| Die | 1920 | 10 | 10 | 100 |  |  |  |
| Dead | 1929 | 1 | 10 | 1000 |  |  |  |
| FishingCast | 2000 | 8 | 10 | 100 |  |  |  |
| FishingWait | 2080 | 6 | 10 | 120 |  |  |  |
| FishingReel | 2160 | 8 | 10 | 100 |  |  |  |
| HorseStanding | 2240 | 4 | 10 | 500 |  |  |  |
| HorseWalking | 2320 | 6 | 10 | 100 |  |  |  |
| HorseRunning | 2400 | 6 | 10 | 100 |  |  |  |
| HorseStruck | 2480 | 3 | 10 | 100 |  |  |  |
| ChannellingStart | 560 | 4 | 10 | 100 |  |  |  |
| ChannellingMiddle | 563 | 1 | 10 | 1000 |  |  |  |
| ChannellingEnd | 0 | 1 | 10 | 60 |  |  |  |
| TamingCast | 720 | 6 | 10 | 100 |  |  |  |
| TamingWait | 725 | 1 | 10 | 100 |  |  |  |

> `Players[MirAnimation.Combat1].Delays[1] = 200ms`;`Players[MirAnimation.Combat2].Delays[3] = 200ms`(Source 第 108-109 行)。

### DefaultItem / DefaultNPC / DefaultMonster

### DefaultItem

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 1 | 0 | 1000 |  |  |  |

### DefaultNPC

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 0 | 1000 |  |  |  |

### DefaultMonster(怪物默认帧表)

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 10 | 500 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 6 | 10 | 100 |  |  |  |
| Combat2 | 160 | 6 | 10 | 100 |  |  |  |
| Combat3 | 160 | 6 | 10 | 100 |  |  |  |
| Struck | 240 | 2 | 10 | 100 |  |  |  |
| Die | 320 | 10 | 10 | 100 |  |  |  |
| Dead | 329 | 1 | 10 | 1000 |  |  |  |
| Skeleton | 880 | 1 | 10 | 1000 |  |  |  |
| Show | 640 | 10 | 10 | 100 |  |  |  |
| Hide | 640 | 10 | 10 | 100 | Y |  |  |
| StoneStanding | 640 | 1 | 10 | 500 |  |  |  |

## 3.2 全部 94 张表


### Players

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 10 | 500 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Running | 160 | 6 | 10 | 100 |  |  |  |
| CreepStanding | 1680 | 4 | 10 | 500 |  |  |  |
| CreepWalkFast | 1760 | 6 | 10 | 100 |  |  |  |
| CreepWalkSlow | 1760 | 6 | 10 | 200 |  |  |  |
| Pushed | 240 | 6 | 10 | 50 | Y | Y |  |
| Stance | 400 | 3 | 10 | 500 |  |  |  |
| Harvest | 480 | 2 | 10 | 300 |  |  |  |
| Combat1 | 560 | 5 | 10 | 100 |  |  | {"1": 200} |
| Combat2 | 640 | 5 | 10 | 100 |  |  | {"3": 200} |
| Combat3 | 720 | 6 | 10 | 100 |  |  |  |
| Combat4 | 800 | 6 | 10 | 100 |  |  |  |
| Combat5 | 880 | 10 | 10 | 60 |  |  |  |
| Combat6 | 960 | 10 | 10 | 60 |  |  |  |
| Combat7 | 1040 | 10 | 10 | 100 |  |  |  |
| Combat8 | 1120 | 6 | 10 | 50 |  | Y |  |
| Combat9 | 1200 | 10 | 10 | 100 |  |  |  |
| Combat10 | 1280 | 10 | 10 | 60 |  |  |  |
| Combat11 | 1360 | 10 | 10 | 60 |  |  |  |
| Combat12 | 1440 | 10 | 10 | 60 |  |  |  |
| Combat13 | 1520 | 6 | 10 | 100 |  |  |  |
| Combat14 | 1600 | 8 | 10 | 100 |  |  |  |
| Combat15 | 400 | 3 | 10 | 200 |  |  |  |
| DragonRepulseStart | 1600 | 6 | 10 | 100 |  |  |  |
| DragonRepulseMiddle | 1605 | 1 | 10 | 1000 |  |  |  |
| DragonRepulseEnd | 1606 | 2 | 10 | 100 |  |  |  |
| Struck | 1840 | 3 | 10 | 100 |  |  |  |
| Die | 1920 | 10 | 10 | 100 |  |  |  |
| Dead | 1929 | 1 | 10 | 1000 |  |  |  |
| FishingCast | 2000 | 8 | 10 | 100 |  |  |  |
| FishingWait | 2080 | 6 | 10 | 120 |  |  |  |
| FishingReel | 2160 | 8 | 10 | 100 |  |  |  |
| HorseStanding | 2240 | 4 | 10 | 500 |  |  |  |
| HorseWalking | 2320 | 6 | 10 | 100 |  |  |  |
| HorseRunning | 2400 | 6 | 10 | 100 |  |  |  |
| HorseStruck | 2480 | 3 | 10 | 100 |  |  |  |
| ChannellingStart | 560 | 4 | 10 | 100 |  |  |  |
| ChannellingMiddle | 563 | 1 | 10 | 1000 |  |  |  |
| ChannellingEnd | 0 | 1 | 10 | 60 |  |  |  |
| TamingCast | 720 | 6 | 10 | 100 |  |  |  |
| TamingWait | 725 | 1 | 10 | 100 |  |  |  |

### DefaultItem

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 1 | 0 | 1000 |  |  |  |

### DefaultNPC

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 0 | 1000 |  |  |  |

### DefaultMonster

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 10 | 500 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 6 | 10 | 100 |  |  |  |
| Combat2 | 160 | 6 | 10 | 100 |  |  |  |
| Combat3 | 160 | 6 | 10 | 100 |  |  |  |
| Struck | 240 | 2 | 10 | 100 |  |  |  |
| Die | 320 | 10 | 10 | 100 |  |  |  |
| Dead | 329 | 1 | 10 | 1000 |  |  |  |
| Skeleton | 880 | 1 | 10 | 1000 |  |  |  |
| Show | 640 | 10 | 10 | 100 |  |  |  |
| Hide | 640 | 10 | 10 | 100 | Y |  |  |
| StoneStanding | 640 | 1 | 10 | 500 |  |  |  |

### Companion_Pig

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 200 |  |  |  |
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Pushed | 80 | 8 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 6 | 10 | 100 |  |  |  |
| Combat2 | 240 | 5 | 10 | 100 |  |  |  |
| Combat3 | 160 | 6 | 10 | 100 | Y |  |  |
| Combat4 | 320 | 6 | 10 | 100 |  |  |  |
| Combat5 | 400 | 5 | 10 | 100 |  |  |  |
| Combat6 | 320 | 6 | 10 | 100 | Y |  |  |
| Combat7 | 480 | 7 | 10 | 100 |  |  |  |
| Combat8 | 560 | 3 | 10 | 100 |  |  |  |

### Companion_TuskLord

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 240 | 7 | 10 | 100 |  |  |  |
| Combat3 | 320 | 5 | 10 | 100 |  |  |  |

### Companion_SkeletonLord

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 240 | 7 | 10 | 100 |  |  |  |
| Combat3 | 320 | 8 | 10 | 100 |  |  |  |

### Companion_Griffin

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 9 | 10 | 100 |  |  |  |
| Combat2 | 240 | 5 | 10 | 100 |  |  |  |
| Combat3 | 320 | 9 | 10 | 100 |  |  |  |
| Combat4 | 400 | 6 | 10 | 100 |  |  |  |

### Companion_Dragon

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 240 | 10 | 10 | 100 |  |  |  |
| Combat3 | 320 | 10 | 10 | 100 |  |  |  |
| Combat4 | 400 | 6 | 10 | 100 |  |  |  |

### Companion_Donkey

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Combat2 | 240 | 10 | 10 | 100 |  |  |  |
| Combat3 | 320 | 10 | 10 | 100 |  |  |  |
| Combat4 | 400 | 4 | 10 | 100 |  |  |  |

### Companion_Sheep

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 12 | 20 | 100 |  |  |  |
| Combat2 | 240 | 10 | 10 | 100 |  |  |  |

### Companion_BanyoLordGuzak

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 240 | 10 | 10 | 100 |  |  |  |
| Combat3 | 320 | 7 | 10 | 100 |  |  |  |

### Companion_Panda

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 240 | 10 | 10 | 100 |  |  |  |
| Combat3 | 320 | 10 | 10 | 100 |  |  |  |
| Combat4 | 400 | 6 | 10 | 100 |  |  |  |

### Companion_Rabbit

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 7 | 10 | 100 |  |  |  |
| Combat2 | 240 | 8 | 10 | 100 |  |  |  |
| Combat3 | 320 | 8 | 10 | 100 |  |  |  |

### Companion_Dog

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 8 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 240 | 10 | 10 | 100 |  |  |  |
| Combat3 | 320 | 10 | 10 | 100 |  |  |  |

### Companion_Jinchon

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 240 | 10 | 10 | 100 |  |  |  |
| Combat3 | 320 | 10 | 10 | 100 |  |  |  |
| Combat4 | 400 | 7 | 10 | 100 |  |  |  |

### Companion_Dino

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 10 | 200 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 240 | 8 | 10 | 100 |  |  |  |
| Combat3 | 320 | 5 | 10 | 100 |  |  |  |
| Combat4 | 400 | 8 | 10 | 100 |  |  |  |

### ForestYeti

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Die | 320 | 4 | 10 | 100 |  |  |  |
| Dead | 323 | 1 | 10 | 1000 |  |  |  |

### ChestnutTree

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Die | 320 | 9 | 10 | 100 |  |  |  |
| Dead | 328 | 1 | 10 | 1000 |  |  |  |

### CarnivorousPlant

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 0 | 500 |  |  |  |
| Show | 640 | 8 | 0 | 100 | Y |  |  |
| Hide | 640 | 8 | 0 | 100 |  |  |  |

### DevouringGhost

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Show | 400 | 10 | 10 | 100 |  |  |  |

### Larva

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 80 | 6 | 10 | 500 |  |  |  |

### ZumaGuardian

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Show | 640 | 6 | 10 | 100 |  |  |  |

### ZumaKing

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Show | 640 | 20 | 0 | 100 |  |  |  |
| StoneStanding | 640 | 1 | 0 | 500 |  |  |  |

### Monkey

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat2 | 400 | 6 | 10 | 100 |  |  |  |

### NetherWorldGate

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 0 | 200 |  |  |  |

### CursedCactus

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 1 | 10 | 100 |  |  |  |
| Combat1 | 80 | 10 | 10 | 100 |  |  |  |

### NumaMage

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat3 | 480 | 6 | 10 | 100 |  |  |  |

### WestDesertLizard

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat2 | 480 | 6 | 10 | 100 |  |  |  |

### BanyaGuard

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat2 | 400 | 6 | 10 | 100 |  |  |  |
| Combat3 | 400 | 6 | 10 | 100 |  |  |  |

### JinchonDevil

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat1 | 160 | 9 | 10 | 70 |  |  |  |
| Combat2 | 400 | 9 | 10 | 70 |  |  |  |
| Combat3 | 480 | 8 | 10 | 70 |  |  |  |

### EmperorSaWoo

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat2 | 480 | 6 | 10 | 100 |  |  |  |
| Combat3 | 480 | 6 | 10 | 100 |  |  |  |

### ArchLichTaeda

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat2 | 400 | 6 | 10 | 100 |  |  |  |
| Show | 480 | 6 | 10 | 100 |  |  |  |
| Die | 720 | 20 | 20 | 100 |  |  |  |
| Dead | 739 | 1 | 20 | 500 |  |  |  |

### PachonTheChaosBringer

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat3 | 480 | 10 | 10 | 100 |  |  |  |
| DragonRepulseStart | 480 | 7 | 10 | 100 |  |  |  |
| DragonRepulseMiddle | 486 | 1 | 10 | 1000 |  |  |  |
| DragonRepulseEnd | 487 | 3 | 10 | 100 |  |  |  |

### IcySpiritGeneral

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat3 | 400 | 6 | 10 | 100 |  |  |  |

### FieryDancer

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 10 | 500 |  |  |  |
| Walking | 80 | 10 | 10 | 100 |  |  |  |
| Pushed | 80 | 10 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 160 | 10 | 10 | 100 |  |  |  |
| Combat3 | 160 | 10 | 10 | 100 |  |  |  |
| Struck | 240 | 4 | 10 | 100 |  |  |  |

### EmeraldDancer

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 10 | 500 |  |  |  |
| Walking | 80 | 10 | 10 | 100 |  |  |  |
| Pushed | 80 | 10 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 20 | 20 | 100 |  |  |  |
| Combat2 | 320 | 20 | 20 | 100 |  |  |  |
| Combat3 | 320 | 20 | 20 | 100 |  |  |  |
| Struck | 480 | 4 | 10 | 100 |  |  |  |
| Die | 560 | 10 | 10 | 100 |  |  |  |
| Dead | 569 | 1 | 10 | 500 |  |  |  |

### QueenOfDawn

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat2 | 400 | 9 | 10 | 100 |  |  |  |
| Combat3 | 400 | 9 | 10 | 100 |  |  |  |
| Die | 320 | 7 | 10 | 100 |  |  |  |
| Dead | 326 | 1 | 10 | 500 |  |  |  |

### OYoungBeast

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 500 |  |  |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 400 | 10 | 10 | 100 |  |  |  |
| Combat3 | 400 | 10 | 10 | 100 |  |  |  |
| Struck | 240 | 5 | 10 | 100 |  |  |  |

### YumgonWitch

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 10 | 500 |  |  |  |
| Walking | 80 | 10 | 10 | 100 |  |  |  |
| Pushed | 80 | 10 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 400 | 10 | 10 | 100 |  |  |  |
| Combat3 | 400 | 10 | 10 | 100 |  |  |  |
| Struck | 240 | 4 | 10 | 100 |  |  |  |

### JinhwanSpirit

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat2 | 400 | 10 | 10 | 100 |  |  |  |
| Combat3 | 400 | 10 | 10 | 100 |  |  |  |

### ChiwooGeneral

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 10 | 500 |  |  |  |
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Combat2 | 400 | 8 | 10 | 100 |  |  |  |
| Combat3 | 400 | 8 | 10 | 100 |  |  |  |
| Die | 320 | 6 | 10 | 100 |  |  |  |
| Dead | 325 | 1 | 10 | 500 |  |  |  |

### DragonQueen

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 10 | 500 |  |  |  |
| Walking | 80 | 10 | 10 | 100 |  |  |  |
| Pushed | 80 | 10 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 160 | 10 | 10 | 100 |  |  |  |
| Combat3 | 160 | 10 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 8 | 10 | 100 |  |  |  |
| Dead | 327 | 1 | 10 | 500 |  |  |  |

### DragonLord

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 10 | 500 |  |  |  |
| Walking | 80 | 10 | 10 | 100 |  |  |  |
| Pushed | 80 | 10 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Combat2 | 160 | 10 | 10 | 100 |  |  |  |
| Combat3 | 160 | 10 | 10 | 100 |  |  |  |
| Struck | 240 | 4 | 10 | 100 |  |  |  |

### FerociousIceTiger

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 500 |  |  |  |
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 6 | 10 | 100 |  |  |  |
| Dead | 325 | 1 | 10 | 500 |  |  |  |
| Combat1 | 480 | 9 | 10 | 100 |  |  |  |
| Combat2 | 560 | 16 | 0 | 40 |  |  |  |
| Combat3 | 560 | 16 | 0 | 100 |  |  |  |

### SamaFireGuardian

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Combat2 | 240 | 8 | 10 | 100 |  |  |  |
| Struck | 320 | 3 | 10 | 100 |  |  |  |
| Die | 400 | 10 | 10 | 100 |  |  |  |
| Dead | 409 | 1 | 10 | 500 |  |  |  |

### Phoenix

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Combat2 | 240 | 9 | 10 | 100 |  |  |  |
| Combat3 | 320 | 7 | 10 | 100 |  |  |  |
| Struck | 400 | 3 | 10 | 100 |  |  |  |
| Die | 480 | 10 | 10 | 100 |  |  |  |
| Dead | 489 | 1 | 10 | 500 |  |  |  |

### EnshrinementBox

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 1 | 0 | 200 |  |  |  |
| Struck | 0 | 1 | 0 | 200 |  |  |  |
| Die | 80 | 10 | 0 | 100 |  |  |  |
| Dead | 89 | 1 | 0 | 500 |  |  |  |

### BloodStone

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 0 | 200 |  |  |  |
| Struck | 240 | 2 | 0 | 200 |  |  |  |
| Die | 320 | 9 | 0 | 100 |  |  |  |
| Dead | 328 | 1 | 0 | 500 |  |  |  |

### SamaCursedBladesman

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat1 | 160 | 9 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 7 | 10 | 100 |  |  |  |
| Dead | 326 | 1 | 10 | 500 |  |  |  |

### SamaCursedSlave

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 7 | 10 | 100 |  |  |  |
| Dead | 326 | 1 | 10 | 500 |  |  |  |

### SamaProphet

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 50 | 4 | 0 | 500 |  |  |  |
| Combat1 | 130 | 9 | 0 | 100 |  |  |  |
| Combat2 | 210 | 9 | 0 | 100 |  |  |  |
| Combat3 | 290 | 10 | 0 | 100 |  |  |  |
| Struck | 370 | 3 | 0 | 100 |  |  |  |
| Die | 450 | 10 | 0 | 100 |  |  |  |
| Dead | 459 | 1 | 10 | 500 |  |  |  |

### SamaSorcerer

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat1 | 160 | 9 | 10 | 100 |  |  |  |
| Combat2 | 240 | 10 | 10 | 100 |  |  |  |
| Combat3 | 320 | 10 | 10 | 100 |  |  |  |
| Struck | 400 | 3 | 10 | 100 |  |  |  |
| Die | 480 | 10 | 10 | 100 |  |  |  |
| Dead | 489 | 1 | 10 | 500 |  |  |  |

### EasterEvent

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Die | 320 | 6 | 10 | 100 |  |  |  |
| Dead | 325 | 1 | 10 | 500 |  |  |  |
| Show | 0 | 4 | 10 | 100 |  |  |  |
| Hide | 0 | 4 | 10 | 100 | Y |  |  |
| StoneStanding | 0 | 1 | 10 | 500 |  |  |  |
| DragonRepulseStart | 0 | 4 | 10 | 100 |  |  |  |
| DragonRepulseMiddle | 0 | 4 | 10 | 1000 |  |  |  |
| DragonRepulseEnd | 0 | 4 | 10 | 100 |  |  |  |

### OrangeTiger

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Pushed | 80 | 8 | 10 | 50 | Y | Y |  |
| Die | 320 | 6 | 10 | 100 |  |  |  |
| Dead | 325 | 1 | 10 | 500 |  |  |  |

### RedTiger

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Pushed | 80 | 8 | 10 | 50 | Y | Y |  |
| Die | 320 | 6 | 10 | 100 |  |  |  |
| Dead | 325 | 1 | 10 | 500 |  |  |  |
| Combat2 | 400 | 6 | 10 | 100 |  |  |  |
| Combat3 | 400 | 6 | 10 | 100 |  |  |  |

### OrangeBossTiger

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 0 | 500 |  |  |  |
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Pushed | 80 | 8 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Struck | 320 | 3 | 10 | 100 |  |  |  |
| Combat2 | 400 | 7 | 10 | 100 |  |  |  |
| Combat3 | 400 | 7 | 10 | 100 |  |  |  |
| Die | 400 | 7 | 10 | 100 |  |  |  |
| Dead | 406 | 1 | 10 | 500 |  |  |  |

### BigBossTiger

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 0 | 500 |  |  |  |
| Walking | 80 | 10 | 10 | 100 |  |  |  |
| Pushed | 80 | 10 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Struck | 240 | 2 | 10 | 100 |  |  |  |
| Die | 320 | 10 | 10 | 100 |  |  |  |
| Dead | 329 | 1 | 10 | 500 |  |  |  |
| Combat2 | 400 | 7 | 10 | 100 |  |  |  |
| Combat3 | 480 | 6 | 10 | 100 |  |  |  |
| Combat4 | 560 | 10 | 10 | 100 |  |  |  |

### SDMob3

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Show | 640 | 10 | 10 | 100 | Y |  |  |
| Hide | 640 | 10 | 10 | 100 |  |  |  |

### SDMob8

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat2 | 480 | 6 | 10 | 100 |  |  |  |

### SDMob15

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 7 | 10 | 500 |  |  |  |
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Combat2 | 240 | 6 | 10 | 100 |  |  |  |
| Struck | 320 | 4 | 10 | 100 |  |  |  |
| Die | 400 | 10 | 10 | 100 |  |  |  |
| Dead | 409 | 1 | 10 | 500 |  |  |  |

### SDMob16

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 7 | 10 | 100 |  |  |  |
| Walking | 80 | 7 | 10 | 100 |  |  |  |
| Pushed | 80 | 7 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Combat2 | 240 | 9 | 10 | 100 |  |  |  |
| Struck | 320 | 3 | 10 | 100 |  |  |  |
| Die | 400 | 10 | 10 | 100 |  |  |  |
| Dead | 409 | 1 | 10 | 500 |  |  |  |

### SDMob17

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat1 | 160 | 9 | 10 | 100 |  |  |  |
| Combat2 | 240 | 9 | 10 | 100 |  |  |  |
| Struck | 320 | 3 | 10 | 100 |  |  |  |
| Die | 400 | 10 | 10 | 100 |  |  |  |
| Dead | 409 | 1 | 10 | 500 |  |  |  |

### SDMob18

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 9 | 10 | 100 |  |  |  |
| Dead | 328 | 1 | 10 | 500 |  |  |  |

### SDMob19

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 500 |  |  |  |
| Combat1 | 160 | 9 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 7 | 10 | 100 |  |  |  |
| Dead | 326 | 1 | 10 | 500 |  |  |  |
| Show | 640 | 8 | 10 | 100 |  |  |  |
| Hide | 640 | 8 | 10 | 100 | Y |  |  |

### SDMob21

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 500 |  |  |  |
| Combat1 | 160 | 10 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 7 | 10 | 100 |  |  |  |
| Dead | 326 | 1 | 10 | 500 |  |  |  |
| Show | 640 | 8 | 10 | 100 |  |  |  |
| Hide | 640 | 8 | 10 | 100 | Y |  |  |

### SDMob22

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 500 |  |  |  |
| Combat1 | 400 | 10 | 10 | 100 |  |  |  |
| Combat2 | 400 | 10 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 6 | 10 | 100 |  |  |  |
| Dead | 325 | 1 | 10 | 500 |  |  |  |
| Show | 640 | 8 | 10 | 100 |  |  |  |
| Hide | 640 | 8 | 10 | 100 | Y |  |  |

### SDMob23

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 10 | 500 |  |  |  |
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Pushed | 80 | 8 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 70 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 8 | 10 | 100 |  |  |  |
| Dead | 327 | 1 | 10 | 500 |  |  |  |
| Show | 640 | 8 | 10 | 100 |  |  |  |
| Hide | 640 | 8 | 10 | 100 | Y |  |  |

### SDMob24

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 7 | 10 | 500 |  |  |  |
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Pushed | 80 | 8 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 9 | 10 | 70 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Combat2 | 400 | 9 | 10 | 70 |  |  |  |

### SDMob25

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 7 | 10 | 500 |  |  |  |
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Pushed | 80 | 8 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 8 | 10 | 70 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Combat2 | 400 | 10 | 10 | 70 |  |  |  |

### SDMob26

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 7 | 10 | 500 |  |  |  |
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Pushed | 80 | 8 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 10 | 10 | 70 |  |  |  |
| Struck | 240 | 4 | 10 | 100 |  |  |  |
| Combat2 | 400 | 8 | 10 | 70 |  |  |  |
| Die | 320 | 7 | 10 | 100 |  |  |  |
| Dead | 326 | 1 | 10 | 500 |  |  |  |

### LobsterLord

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 20 | 6 | 0 | 500 |  |  |  |
| Combat1 | 30 | 7 | 0 | 100 |  |  |  |
| Combat2 | 40 | 7 | 0 | 100 |  |  |  |
| Combat3 | 60 | 7 | 0 | 100 |  |  |  |
| Combat4 | 70 | 7 | 0 | 100 |  |  |  |
| Combat5 | 80 | 7 | 0 | 100 |  |  |  |
| Combat6 | 110 | 8 | 0 | 100 |  |  |  |
| Combat7 | 120 | 4 | 0 | 100 |  |  |  |
| Struck | 50 | 4 | 0 | 100 |  |  |  |
| Die | 130 | 9 | 0 | 100 |  |  |  |
| Dead | 138 | 1 | 0 | 500 |  |  |  |

### JinamStoneGate

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 1 | 0 | 200 |  |  |  |

### DeadTree

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 1 | 0 | 200 |  |  |  |
| Struck | 0 | 1 | 0 | 200 |  |  |  |
| Die | 0 | 1 | 0 | 200 |  |  |  |
| Dead | 0 | 1 | 0 | 200 |  |  |  |

### MonasteryMon1

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 15 | 20 | 500 |  |  |  |
| Walking | 160 | 7 | 10 | 100 |  |  |  |
| Pushed | 160 | 7 | 10 | 50 | Y | Y |  |
| Combat1 | 240 | 9 | 10 | 100 |  |  |  |
| Combat2 | 320 | 10 | 10 | 100 |  |  |  |
| Combat3 | 320 | 10 | 10 | 100 |  |  |  |
| Struck | 400 | 4 | 10 | 100 |  |  |  |
| Die | 480 | 9 | 10 | 100 |  |  |  |
| Dead | 488 | 1 | 10 | 1000 |  |  |  |

### MonasteryMon3

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 15 | 20 | 500 |  |  |  |
| Walking | 160 | 7 | 10 | 100 |  |  |  |
| Pushed | 160 | 7 | 10 | 50 | Y | Y |  |
| Combat1 | 240 | 9 | 10 | 100 |  |  |  |
| Combat2 | 320 | 9 | 10 | 100 |  |  |  |
| Combat3 | 400 | 10 | 10 | 100 |  |  |  |
| Struck | 480 | 4 | 10 | 100 |  |  |  |
| Die | 560 | 9 | 10 | 100 |  |  |  |
| Dead | 568 | 1 | 10 | 1000 |  |  |  |

### Terracotta1

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 160 | 4 | 10 | 500 |  |  |  |
| Walking | 240 | 6 | 10 | 100 |  |  |  |
| Combat1 | 320 | 8 | 10 | 100 |  |  |  |
| Struck | 400 | 3 | 10 | 100 |  |  |  |
| Show | 0 | 13 | 20 | 100 |  |  |  |
| Die | 480 | 11 | 20 | 100 |  |  |  |
| Dead | 490 | 1 | 20 | 1000 |  |  |  |
| Hide | 0 | 13 | 20 | 100 | Y |  |  |

### Terracotta2

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 160 | 4 | 10 | 500 |  |  |  |
| Walking | 240 | 6 | 10 | 100 |  |  |  |
| Combat1 | 320 | 8 | 10 | 100 |  |  |  |
| Struck | 400 | 3 | 10 | 100 |  |  |  |
| Show | 0 | 13 | 20 | 100 |  |  |  |
| Die | 480 | 12 | 20 | 100 |  |  |  |
| Dead | 491 | 1 | 20 | 1000 |  |  |  |
| Hide | 0 | 13 | 20 | 100 | Y |  |  |

### Terracotta3

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 160 | 4 | 10 | 500 |  |  |  |
| Walking | 240 | 6 | 10 | 100 |  |  |  |
| Combat1 | 320 | 8 | 10 | 100 |  |  |  |
| Struck | 400 | 3 | 10 | 100 |  |  |  |
| Show | 0 | 13 | 20 | 100 |  |  |  |
| Die | 480 | 10 | 10 | 100 |  |  |  |
| Dead | 489 | 1 | 10 | 1000 |  |  |  |
| Hide | 0 | 13 | 20 | 100 | Y |  |  |

### TerracottaSub

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 160 | 4 | 10 | 500 |  |  |  |
| Walking | 240 | 6 | 10 | 100 |  |  |  |
| Combat1 | 320 | 8 | 10 | 100 |  |  |  |
| Combat3 | 400 | 8 | 10 | 100 |  |  |  |
| Struck | 480 | 3 | 10 | 100 |  |  |  |
| Show | 0 | 13 | 20 | 100 |  |  |  |
| Die | 560 | 13 | 20 | 100 |  |  |  |
| Dead | 572 | 1 | 20 | 1000 |  |  |  |
| Hide | 0 | 13 | 20 | 100 | Y |  |  |

### TerracottaBoss

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Combat1 | 240 | 9 | 10 | 100 |  |  |  |
| Combat3 | 160 | 9 | 10 | 100 |  |  |  |
| Struck | 320 | 3 | 10 | 100 |  |  |  |
| Die | 400 | 11 | 20 | 120 |  |  |  |
| Dead | 411 | 1 | 20 | 1000 |  |  |  |

### BobbitWorm

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Show | 400 | 7 | 10 | 100 |  |  |  |
| Hide | 400 | 7 | 10 | 100 | Y |  |  |

### Tornado

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Show | 0 | 10 | 0 | 100 |  |  |  |
| Standing | 10 | 9 | 0 | 100 |  |  |  |
| Walking | 10 | 9 | 0 | 100 |  |  |  |
| Combat1 | 10 | 9 | 0 | 100 |  |  |  |
| Hide | 20 | 7 | 0 | 100 |  |  |  |
| Die | 20 | 7 | 0 | 100 |  |  |  |

### InfernalSoldier

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 4 | 10 | 500 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 6 | 10 | 100 |  |  |  |
| Combat2 | 240 | 6 | 10 | 100 |  |  |  |
| Struck | 320 | 3 | 10 | 100 |  |  |  |
| Die | 400 | 9 | 10 | 100 |  |  |  |
| Dead | 409 | 1 | 10 | 1000 |  |  |  |
| Show | 480 | 9 | 10 | 100 |  |  |  |
| Hide | 480 | 9 | 10 | 100 | Y |  |  |

### SeaHorseCavalry

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 100 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 6 | 10 | 100 |  |  |  |
| Combat2 | 400 | 8 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 9 | 10 | 100 |  |  |  |
| Dead | 329 | 1 | 10 | 1000 |  |  |  |

### Seamancer

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 100 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 6 | 10 | 100 |  |  |  |
| Combat2 | 400 | 6 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 320 | 9 | 10 | 100 |  |  |  |
| Dead | 329 | 1 | 10 | 1000 |  |  |  |

### CoralStoneDuin

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 100 |  |  |  |
| Walking | 80 | 8 | 10 | 100 |  |  |  |
| Pushed | 80 | 8 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Combat2 | 320 | 9 | 10 | 100 |  |  |  |
| Struck | 240 | 5 | 10 | 100 |  |  |  |
| Die | 480 | 10 | 10 | 100 |  |  |  |
| Dead | 490 | 1 | 10 | 1000 |  |  |  |
| Show | 400 | 7 | 10 | 100 |  |  |  |
| Hide | 400 | 7 | 10 | 100 | Y |  |  |

### Brachiopod

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 100 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 8 | 10 | 100 |  |  |  |
| Combat2 | 400 | 9 | 10 | 100 |  |  |  |
| Combat3 | 560 | 6 | 10 | 100 |  |  |  |
| Struck | 240 | 3 | 10 | 100 |  |  |  |
| Die | 400 | 10 | 10 | 100 |  |  |  |
| Dead | 410 | 1 | 10 | 1000 |  |  |  |
| Show | 480 | 10 | 10 | 100 |  |  |  |

### GiantClam

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 8 | 10 | 100 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 9 | 10 | 100 |  |  |  |
| Struck | 240 | 5 | 10 | 100 |  |  |  |
| Die | 320 | 10 | 10 | 100 |  |  |  |
| Dead | 330 | 1 | 10 | 1000 |  |  |  |

### BlueMassif

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 100 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 7 | 10 | 100 |  |  |  |
| Combat2 | 320 | 9 | 10 | 100 |  |  |  |
| Struck | 240 | 7 | 10 | 100 |  |  |  |
| Die | 400 | 10 | 10 | 100 |  |  |  |
| Dead | 410 | 1 | 10 | 1000 |  |  |  |

### Mollusk

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 6 | 10 | 100 |  |  |  |
| Walking | 80 | 6 | 10 | 100 |  |  |  |
| Pushed | 80 | 6 | 10 | 50 | Y | Y |  |
| Combat1 | 160 | 6 | 10 | 100 |  |  |  |
| Struck | 240 | 5 | 10 | 100 |  |  |  |
| Die | 320 | 10 | 10 | 100 |  |  |  |
| Dead | 330 | 1 | 10 | 1000 |  |  |  |

### GiantClam1

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 8 | 10 | 100 |  |  |  |
| Struck | 240 | 5 | 10 | 100 |  |  |  |
| Die | 480 | 8 | 10 | 100 |  |  |  |
| Dead | 488 | 1 | 10 | 1000 |  |  |  |

### CastleFlag

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 10 | 0 | 100 |  |  |  |
| Struck | 0 | 10 | 0 | 100 |  |  |  |

### SabukGate

| 动画 | Start | Count | OffSet | 延时(ms) | Reversed | StaticSpeed | Delays覆写 |
|---|---|---|---|---|---|---|---|
| Standing | 0 | 1 | 10 | 1000 |  |  |  |
| Struck | 240 | 2 | 10 | 100 |  |  |  |
| Combat1 | 640 | 7 | 0 | 100 |  |  |  |
| Combat2 | 640 | 7 | 0 | 100 | Y |  |  |
| Die | 320 | 8 | 0 | 100 |  |  |  |
| Dead | 327 | 1 | 0 | 1000 |  |  |  |


---

# 第 4 章 玩家渲染(PlayerObject.cs)

## 4.1 关键常量

| 常量 | 值 | 语义 |
|---|---|---|
| `FemaleOffSet` | 5000 | 女性版库键偏移 |
| `AssassinOffSet` | 50000 | 刺客版库键偏移 |
| `RightHandOffSet` | 50 | 右手持武器库键偏移 |
| `ArmourShapeOffSet` | 5000(刺客:3000) | `UpdateLibraries` 内,护甲 shape 计算 |
| `WeaponShapeOffSet` | 5000 | 武器 shape 计算 |
| `HairTypeOffSet` | 5000 | 发型帧计算 |
| `BodyOffSet`(怪物) | 1000 | 见第 5 章 |
| `NPC BodyOffSet` | 100 | 见第 6 章 |

## 4.2 装备库字典(5 张,键为装备 Shape)

### ShieldList(4)

| 键 | 库 |
|---|---|
| 0 | `M_Shield1` |
| 1 | `M_Shield2` |
| 0 + FemaleOffSet | `WM_Shield1` |
| 1 + FemaleOffSet | `WM_Shield2` |

### WeaponList(52)

武器库键按 `/11` 得索引(女版 +5000,右手版 +50):

| 键 | 库 |
|---|---|
| 0 | `M_Weapon1` |
| 1 | `M_Weapon2` |
| 2 | `M_Weapon3` |
| 3 | `M_Weapon4` |
| 4 | `M_Weapon5` |
| 5 | `M_Weapon6` |
| 6 | `M_Weapon7` |
| 9 | `M_Weapon10` |
| 10 | `M_Weapon11` |
| 11 | `M_Weapon12` |
| 12 | `M_Weapon13` |
| 13 | `M_Weapon14` |
| 14 | `M_Weapon15` |
| 15 | `M_Weapon16` |
| 0 + FemaleOffSet | `WM_Weapon1` |
| 1 + FemaleOffSet | `WM_Weapon2` |
| 2 + FemaleOffSet | `WM_Weapon3` |
| 3 + FemaleOffSet | `WM_Weapon4` |
| 4 + FemaleOffSet | `WM_Weapon5` |
| 5 + FemaleOffSet | `WM_Weapon6` |
| 6 + FemaleOffSet | `WM_Weapon7` |
| 9 + FemaleOffSet | `WM_Weapon10` |
| 10 + FemaleOffSet | `WM_Weapon11` |
| 11 + FemaleOffSet | `WM_Weapon12` |
| 12 + FemaleOffSet | `WM_Weapon13` |
| 13 + FemaleOffSet | `WM_Weapon14` |
| 14 + FemaleOffSet | `WM_Weapon15` |
| 15 + FemaleOffSet | `WM_Weapon16` |
| 110 | `M_WeaponAOH1` |
| 111 | `M_WeaponAOH2` |
| 112 | `M_WeaponAOH3` |
| 113 | `M_WeaponAOH4` |
| 114 | `M_WeaponAOH5` |
| 115 | `M_WeaponAOH6` |
| 110 + FemaleOffSet | `WM_WeaponAOH1` |
| 111 + FemaleOffSet | `WM_WeaponAOH2` |
| 112 + FemaleOffSet | `WM_WeaponAOH3` |
| 113 + FemaleOffSet | `WM_WeaponAOH4` |
| 114 + FemaleOffSet | `WM_WeaponAOH5` |
| 115 + FemaleOffSet | `WM_WeaponAOH6` |
| 120 | `M_WeaponADL1` |
| 121 | `M_WeaponADL2` |
| 125 | `M_WeaponADL6` |
| 120 + RightHandOffSet | `M_WeaponADR1` |
| 121 + RightHandOffSet | `M_WeaponADR2` |
| 125 + RightHandOffSet | `M_WeaponADR6` |
| 120 + FemaleOffSet | `WM_WeaponADL1` |
| 121 + FemaleOffSet | `WM_WeaponADL2` |
| 125 + FemaleOffSet | `WM_WeaponADL6` |
| 120 + FemaleOffSet + RightHandOffSet | `WM_WeaponADR1` |
| 121 + FemaleOffSet + RightHandOffSet | `WM_WeaponADR2` |
| 125 + FemaleOffSet + RightHandOffSet | `WM_WeaponADR6` |

> 注:WeaponList 键带 `+FemaleOffSet`/`+RightHandOffSet` 的字面形式;实际查找时 `LibraryFile` 枚举值 = 键计算后的数值。

### HelmetList(30,按 `/10` 索引)

| 键 | 库 |
|---|---|
| 0 | `M_Helmet1` |
| 1 | `M_Helmet2` |
| 2 | `M_Helmet3` |
| 3 | `M_Helmet4` |
| 4 | `M_Helmet5` |
| 10 | `M_Helmet11` |
| 11 | `M_Helmet12` |
| 12 | `M_Helmet13` |
| 13 | `M_Helmet14` |
| 20 | `M_HelmetCx1` |
| 0 + FemaleOffSet | `WM_Helmet1` |
| 1 + FemaleOffSet | `WM_Helmet2` |
| 2 + FemaleOffSet | `WM_Helmet3` |
| 3 + FemaleOffSet | `WM_Helmet4` |
| 4 + FemaleOffSet | `WM_Helmet5` |
| 10 + FemaleOffSet | `WM_Helmet11` |
| 11 + FemaleOffSet | `WM_Helmet12` |
| 12 + FemaleOffSet | `WM_Helmet13` |
| 13 + FemaleOffSet | `WM_Helmet14` |
| 20 + FemaleOffSet | `WM_HelmetCx1` |
| 0 + AssassinOffSet | `M_HelmetA1` |
| 1 + AssassinOffSet | `M_HelmetA2` |
| 2 + AssassinOffSet | `M_HelmetA3` |
| 3 + AssassinOffSet | `M_HelmetA4` |
| 20 + AssassinOffSet | `M_HelmetACx1` |
| 0 + AssassinOffSet + FemaleOffSet | `WM_HelmetA1` |
| 1 + AssassinOffSet + FemaleOffSet | `WM_HelmetA2` |
| 2 + AssassinOffSet + FemaleOffSet | `WM_HelmetA3` |
| 3 + AssassinOffSet + FemaleOffSet | `WM_HelmetA4` |
| 20 + AssassinOffSet + FemaleOffSet | `WM_HelmetACx1` |

### ArmourList(30,按 `/10` 索引)

| 键 | 库 |
|---|---|
| 0 | `M_Hum` |
| 1 | `M_HumEx1` |
| 2 | `M_HumEx2` |
| 3 | `M_HumEx3` |
| 4 | `M_HumEx4` |
| 10 | `M_HumEx10` |
| 11 | `M_HumEx11` |
| 12 | `M_HumEx12` |
| 13 | `M_HumEx13` |
| 20 | `M_HumCx1` |
| 0 + FemaleOffSet | `WM_Hum` |
| 1 + FemaleOffSet | `WM_HumEx1` |
| 2 + FemaleOffSet | `WM_HumEx2` |
| 3 + FemaleOffSet | `WM_HumEx3` |
| 4 + FemaleOffSet | `WM_HumEx4` |
| 10 + FemaleOffSet | `WM_HumEx10` |
| 11 + FemaleOffSet | `WM_HumEx11` |
| 12 + FemaleOffSet | `WM_HumEx12` |
| 13 + FemaleOffSet | `WM_HumEx13` |
| 20 + FemaleOffSet | `WM_HumCx1` |
| 0 + AssassinOffSet | `M_HumA` |
| 1 + AssassinOffSet | `M_HumAEx1` |
| 2 + AssassinOffSet | `M_HumAEx2` |
| 3 + AssassinOffSet | `M_HumAEx3` |
| 20 + AssassinOffSet | `M_HumACx1` |
| 0 + AssassinOffSet + FemaleOffSet | `WM_HumA` |
| 1 + AssassinOffSet + FemaleOffSet | `WM_HumAEx1` |
| 2 + AssassinOffSet + FemaleOffSet | `WM_HumAEx2` |
| 3 + AssassinOffSet + FemaleOffSet | `WM_HumAEx3` |
| 20 + AssassinOffSet + FemaleOffSet | `WM_HumACx1` |

### CostumeList(6,时装;刺客 +50000)

| 键 | 库 |
|---|---|
| 0 | `M_Costume` |
| 1 | `M_CostumeEx1` |
| 0 + FemaleOffSet | `WM_Costume` |
| 1 + FemaleOffSet | `WM_CostumeEx1` |
| 0 + AssassinOffSet | `M_CostumeA` |
| 0 + AssassinOffSet + FemaleOffSet | `WM_CostumeA` |

## 4.3 帧号计算(属性表达式,源码原样)

```
HairFrame   => DrawFrame + (HairType - 1) * HairTypeOffSet                          // HairTypeOffSet = 5000
HelmetFrame => DrawFrame + ((HelmetShape - 1) % 10) * ArmourShapeOffSet + ArmourShift
WeaponFrame => DrawFrame + (WeaponShape % 10) * WeaponShapeOffSet                    // WeaponShapeOffSet = 5000
ShieldFrame => DrawFrame + (ShieldShape % 10) * ArmourShapeOffSet + ArmourShift
ArmourFrame => DrawFrame + (CostumeShape >= 0 ? (CostumeShape % 10) : (ArmourShape % 11))
               * ArmourShapeOffSet + ArmourShift                                     // 关键:时装优先,否则 ArmourShape % 11
```

- `ArmourShapeOffSet`:战士/法师/道士 = 5000,刺客 = 3000(`UpdateLibraries` 按职业分支)。
- `ArmourShift`:刺客专用动画帧修正表(PlayerObject.cs `SetFrame`,仅 `MirClass.Assassin` 分支,源码逐 case 确认):
  - 除下表外 `default:` 抛 `ArgumentOutOfRangeException`。

| 动画 | ArmourShift |
|---|---|
| Standing | 0 |
| Walking | 1600 |
| Running | 1600 |
| CreepStanding | 240 |
| CreepWalkSlow | 240 |
| CreepWalkFast | 240 |
| Pushed | 160 |
| Combat1 | -400 |
| Combat2 | 0 |
| Combat3 | 0 |
| Combat4 | 80 |
| Combat5 | 400 |
| Combat6 | 400 |
| Combat7 | 400 |
| Combat8 | 720 |
| Combat9 | -960 |
| Combat10 | -480 |
| Combat11 | -400 |
| Combat12 | -400 |
| Combat13 | -400 |
| Combat14 | 0 |
| DragonRepulseStart | 0 |
| DragonRepulseMiddle | 0 |
| DragonRepulseEnd | 0 |
| Harvest | 160 |
| Stance | 160 |
| Struck | -640 |
| Die | -400 |
| Dead | -400 |
| HorseStanding | 80 |
| HorseWalking | 80 |
| HorseRunning | 80 |
| HorseStruck | 80 |
| FishingCast | 80 |
| FishingWait | 80 |
| FishingReel | 80 |
| TamingCast | 0 |
| TamingWait | 0 |

- `UpdateLibraries()`:按职业/性别分支计算 `BodyLibrary/ArmourShape/WeaponShape/LibraryWeaponShape/HelmetShape/ShieldShape`;库键算术:护甲/头盔 `/10`、武器 `/11`(再映射 WeaponList 索引)。

## 4.4 SetAnimation(动作→帧表映射)

按职业分派:法师/道士/战士走 `Players` 表;刺客走 `AssassinPlayers`(旧表,注释为死代码,当前 `Frames = new Dictionary<MirAnimation, Frame>(FrameSet.Players)` 拷贝)。`SetAnimation(MirAction, Direction, ...)` 内部 `switch(MirAnimation)` 设置 `CurrentAction = Action`、`Frames` 与 `PlayFrame`。

## 4.5 分层绘制(DrawBody,自上而下)

```
1. 马(若骑乘):DrawHorseOverlay / DrawHorseShadow(第 4.7 节)
2. 背部武器/盾(BackWeapon、BackShield:背在身后)
3. 身体(ArmourFrame,染色 DrawColour)
4. 头(HelmetFrame/HairFrame;帽子覆盖发型)
5. 前部武器/盾(FrontWeapon、FrontShield:持在身前)
```

每层独立帧号 = `DrawFrame` 基础上加对应 shape 偏移;`DrawColour` 染色仅在非 White 时生效(`DrawColour` 方法)。

### DrawShadow2(阴影)

剪切变换:`GetScaledTransform(new Matrix3x2(1F, 0F, -0.5F, 0.5F, translateX, translateY))` + 半透明(0.5/set,opacity 0.5)+ scratch texture + `SetTextureFilter(None→Point)` 画黑色影(斜切阴影)。

## 4.6 隐藏武器规则(CostumeShapeHideWeapon)

```
{ 6, 7, 8, 9, 10, 11, 12, 13, 16, 17, 18 }
```

穿这些时装 shape 时隐藏武器显示。

## 4.7 坐骑(HorseObject / DrawHorseOverlay)

| 项 | 值 |
|---|---|
| `HorseFrame` | 由 `HorseType` 与 `HorseFrameSet` 计算 |
| `DrawHorseOverlay` | `DrawBlend(frame, Scale, White, ...)`(混合绘制) |
| `DrawHorseShadow` | 在 `(DrawX + CellWidth/2, DrawY + CellHeight/2)` 画 0.5F 阴影 |

`HorseType` 见 1.7 枚举(7 种:Normal/Big/WarHorse/HorseGold/WhiteHorse/Tiger/TigerGold)。

## 4.8 其他渲染细节

- `DrawHealth`(Interface 库,帧 79/80):三条状态条,每条约 `Draw(80, DrawX, DrawY - off, White)` 背景 + `Draw(79, DrawX+1, DrawY - off + 1, colour, 宽度按 percent 裁剪的 Rectangle)` 填充:
  - 状态条一:off=59,Goldenrod;状态条二(HP):off=55,OrangeRed;状态条三(MP):off=51,DodgerBlue。`percent = clamp(Health/MaxHealth, 0, 1)`。
- 鼠标悬停:`if (mouseOver && Config.ShowTargetOutline)` 画目标轮廓(`levelDiff = 2` 判定),宠物 `PetOwner` 判定。
- `DrawBlend` 用 `RenderingPipelineManager.SetBlend(true, 1F, BlendMode.LIGHTMAP)` 系混合。
- 攻击音效:`PlayCommonSounds` 按武器类型(shape)映射音效(斧/剑/弓等),刺客有变体音效。
- `S.ObjectPlayer` 字段(序列化):`[String Name, UInt16 Colour, MirGender Gender, ...]`——渲染层读取的玩家网络字段。


---

# 第 5 章 怪物渲染(MonsterObject.cs)

## 5.1 绘制公式

```
BodyFrame = DrawFrame + (BodyShape % 10) * BodyOffSet        // BodyOffSet = 1000(类字段)
```

- `GetBodyDrawY` 特例:`ChestnutTree` y 减 `CellHeight`;`NewMob10` y 减 `CellHeight * 4`。
- `DrawShadow` 规则:
  - 无影:`DustDevil`、`Tornado`、`SabukGate` 四门(Down/Up/Left/Right)。
  - `LobsterLord`:三层阴影 `Draw(BodyFrame+0/1000/2000, x, y, White, true, 0.5f, ImageType.Shadow, Scale)`。
  - 默认:单层 `Draw(BodyFrame, x, y, Color.White, true, 0.5f, ImageType.Shadow, Scale)`。
- `DrawBody` 特例:
  - `LobsterLord`:三层主体 `Draw(BodyFrame+0/1000/2000, x, y, DrawColour, true, Opacity, ImageType.Image, Scale)`。
  - `NewMob1`(非 Dead 时):叠加 `MonMagicEx20` 库 `DrawBlend(DrawFrame + 2000, x, y, White, true, 1f)`。
  - 默认:`BodyLibrary.Draw(BodyFrame, x, y, DrawColour, true, Opacity, ImageType.Image, Scale)`。
  - 开头含鼠标悬停 `TargetOutline` 色彩逻辑(`mouseOver && Config.ShowTargetOutline`);尾部 `RenderingPipelineManager.SetBlend(true, ...)` 混合处理。
- `VisiblePixel`:LobsterLord 三帧任一命中即 true。
- `UpdateLibraries` 的 `default:` 分支 = `LibraryFile.Mon_1` + `BodyShape = 0`。
- `SetAnimation` `default:` → `throw new ArgumentOutOfRangeException()`(第 2550 行;2470 行 default → Standing + DragonRepulse buff 检查)。

## 5.2 MonsterImage 枚举(294 成员:None + 293)

`MutatedOctopus = 12` 起;`Shinsu = 99`("//Small")、`Shinsu1 = 100`("//Large",**死代码:switch 中无 case**);33 条 `//NF_` 占位注释(如 NF_StonePillar=10、NF_BlackPumpkinMan=11、NF_StoneBuilding13=13、NF_CrystalPillar=21)填充分布空隙;`SDMob8` 为枚举成员但**无 switch case**(仅以 `FrameSet.SDMob8` 形式出现在 OmaMage case 内);`None` 无 case。

| 名称 | 值 | 注释 |
|---|---|---|
| None | None |  |
| MutatedOctopus | 12 | NF_BlackPumpkinMan = 11, |
| StoneGolem | 14 | NF_StoneBuilding13 = 13, |
| NetherWorldGate | 15 |  |
| LightArmedSoldier | 16 |  |
| AntHealer | 17 |  |
| ArmoredAnt | 18 |  |
| Stomper | 19 |  |
| ChaosKnight | 20 |  |
| CorpseStalker | 22 | NF_CrystalPillar = 21, |
| NumaMage | 23 |  |
| AntSoldier | 24 |  |
| NumaElite | 27 | NF_StoneBuilding26 = 26, |
| Phantom | 28 |  |
| CrimsonNecromancer | 29 |  |
| Chicken | 30 |  |
| Deer | 31 |  |
| Oma | 33 | NF_Man1 = 32, |
| OmaHero | 34 |  |
| SpittingSpider | 35 |  |
| Guard | 36 |  |
| OmaWarlord | 37 |  |
| Scorpion | 38 |  |
| CaveBat | 39 |  |
| ForestYeti | 40 |  |
| CarnivorousPlant | 41 |  |
| Skeleton | 42 |  |
| SkeletonAxeThrower | 43 |  |
| SkeletonAxeMan | 44 |  |
| SkeletonWarrior | 45 |  |
| SkeletonLord | 46 |  |
| CaveMaggot | 47 |  |
| ClawCat | 48 |  |
| Scarecrow | 50 | NF_KoreanFlag = 49, |
| UmaInfidel | 51 |  |
| BloodThirstyGhoul | 52 |  |
| UmaFlameThrower | 53 |  |
| UmaAnguisher | 54 |  |
| UmaKing | 55 |  |
| SpinedDarkLizard | 56 |  |
| Dung | 57 |  |
| GhostSorcerer | 58 |  |
| GhostMage | 59 |  |
| VoraciousGhost | 60 |  |
| DevouringGhost | 61 |  |
| CorpseRaisingGhost | 62 |  |
| GhoulChampion | 63 |  |
| RedSnake | 64 |  |
| WhiteBone | 66 | NF_KatanaGuard = 65, |
| TigerSnake | 67 |  |
| Sheep | 68 |  |
| SkyStinger | 69 |  |
| ShellNipper | 70 |  |
| VisceralWorm | 71 |  |
| Beetle | 73 | NF_KingScorpion = 72, |
| SpikedBeetle | 74 |  |
| Wolf | 75 |  |
| Centipede | 76 |  |
| LordNiJae | 77 |  |
| MutantMaggot | 78 |  |
| Earwig | 79 |  |
| IronLance | 80 |  |
| WaspHatchling | 81 |  |
| ButterflyWorm | 82 |  |
| WedgeMothLarva | 83 |  |
| LesserWedgeMoth | 84 |  |
| WedgeMoth | 85 |  |
| RedBoar | 86 |  |
| BlackBoar | 87 |  |
| TuskLord | 88 |  |
| ClawSerpent | 89 |  |
| EvilSnake | 90 |  |
| ViciousRat | 91 |  |
| ZumaSharpShooter | 92 |  |
| ZumaFanatic | 93 |  |
| ZumaGuardian | 94 |  |
| ZumaKing | 95 |  |
| ArcherGuard | 96 |  |
| Shinsu | 99 | Small |
| Shinsu1 | 100 | Large |
| AquaLizard | 102 | NF_SandGuard = 101, |
| CorrosivePoisonSpitter | 103 |  |
| SandShark | 104 |  |
| CursedCactus | 105 |  |
| AntNeedler | 106 |  |
| WindfurySorceress | 107 |  |
| PhantomSoldier | 109 | NF_NumaMounted = 108, |
| SpiderBat | 111 | NF_FoxWarrior = 110, |
| RedMoonTheFallen | 114 | NF_FoxWizard = 113, |
| Larva | 115 |  |
| ArachnidGazer | 116 |  |
| RedMoonGuardian | 117 |  |
| RedMoonProtector | 118 |  |
| RedMoonRedProtector | 119 |  |
| RedMoonGrayProtector | 120 |  |
| VenomousArachnid | 121 |  |
| DarkArachnid | 122 |  |
| ForestGuard | 123 |  |
| TownGuard | 124 |  |
| SandGuard | 125 |  |
| Pig | 129 | NF_Blank128 = 128, |
| PachonTheChaosBringer | 130 |  |
| Cow | 131 |  |
| ChestnutTree | 137 | NF_WhiteSnake = 136, |
| NumaGrunt | 138 |  |
| NumaWarrior | 139 |  |
| BanyaRightGuard | 140 |  |
| BanyaLeftGuard | 141 |  |
| DecayingGhoul | 142 |  |
| FrostMinotaur | 143 |  |
| ShockMinotaur | 144 |  |
| FuryMinotaur | 145 |  |
| FlameMinotaur | 146 |  |
| Minotaur | 147 |  |
| RottingGhoul | 148 |  |
| EmperorSaWoo | 149 |  |
| BoneCaptain | 150 |  |
| ArchLichTaedu | 151 |  |
| BoneSoldier | 152 |  |
| BoneBladesman | 153 |  |
| BoneArcher | 154 |  |
| MutantFlea | 155 |  |
| PurpleFlea | 156 |  |
| BlasterMutantFlea | 157 |  |
| BlueBlasterMutantFlea | 158 |  |
| PoisonousMutantFlea | 159 |  |
| RazorTusk | 160 |  |
| Monkey | 164 | NF_ChristmasTree = 163, |
| CannibalFanatic | 166 | NF_Santa = 165, |
| EvilFanatic | 167 |  |
| EvilElephant | 168 |  |
| FlameGriffin | 169 |  |
| StoneGriffin | 170 |  |
| MutantCaptain | 171 |  |
| PinkGoddess | 172 |  |
| GreenGoddess | 173 |  |
| JinchonDevil | 174 |  |
| IcyGoddess | 180 | NF_Catapult179 = 179, |
| WildBoar | 181 |  |
| NumaCavalry | 190 | NF_BonePile189 = 189, |
| NumaArmoredSoldier | 191 |  |
| NumaStoneThrower | 193 | NF_NumaAxeSoldier = 192, |
| NumaHighMage | 194 |  |
| NumaRoyalGuard | 195 |  |
| BloodStone | 197 | NF_NumaWarlord = 196, |
| RagingLizard | 201 | NF_Snowman = 200, |
| SawToothLizard | 202 |  |
| MutantLizard | 203 |  |
| VenomSpitter | 204 |  |
| SonicLizard | 205 |  |
| GiantLizard | 206 |  |
| TaintedTerror | 207 |  |
| DeathLordJichon | 208 |  |
| CrazedLizard | 209 |  |
| IcyRanger | 210 |  |
| FerociousIceTiger | 211 |  |
| IcySpiritWarrior | 212 |  |
| IcySpiritGeneral | 213 |  |
| GhostKnight | 214 |  |
| FrostLordHwa | 215 |  |
| IcySpiritSpearman | 216 |  |
| Werewolf | 217 |  |
| Whitefang | 218 |  |
| IcySpiritSolider | 219 |  |
| EscortCommander | 220 |  |
| QueenOfDawn | 221 |  |
| FieryDancer | 222 |  |
| EmeraldDancer | 223 |  |
| ChiwooGeneral | 230 | NF_Blank229 = 229, |
| DragonLord | 231 |  |
| DragonQueen | 232 |  |
| OYoungBeast | 233 |  |
| MaWarlord | 234 |  |
| YumgonGeneral | 235 |  |
| YumgonWitch | 236 |  |
| JinhwanSpirit | 237 |  |
| JinhwanGuardian | 238 |  |
| JinamStoneGate | 239 |  |
| SamaFireGuardian | 250 | Mon24 |
| SamaIceGuardian | 251 |  |
| SamaLightningGuardian | 252 |  |
| SamaWindGuardian | 253 |  |
| Phoenix | 254 |  |
| BlackTortoise | 255 |  |
| BlueDragon | 256 |  |
| WhiteTiger | 257 |  |
| InfernalSoldier | 260 |  |
| SamaCursedBladesman | 270 | NF_Blank269 = 269, |
| SamaCursedSlave | 271 |  |
| SamaCursedFlameMage | 272 |  |
| SamaProphet | 273 |  |
| SamaSorcerer | 274 |  |
| EnshrinementBox | 275 |  |
| UmaMaceInfidel | 278 | NF_AssassinFemale = 277, |
| Salamander | 280 | NF_Blank279 = 279, |
| SandGolem | 281 |  |
| OmaInfant | 284 | NF_SmallSpider = 283, |
| Yob | 285 |  |
| RakingCat | 286 |  |
| UmaTridentInfidel | 287 |  |
| GangSpider | 288 |  |
| VenomSpider | 289 |  |
| SDMob4 | 290 |  |
| SDMob5 | 291 |  |
| SDMob6 | 292 |  |
| SDMob7 | 298 | NF_NumaSoldier = 297, |
| OmaMage | 299 |  |
| WildMonkey | 300 |  |
| FrostYeti | 301 |  |
| SDMob8 | 320 | Mon31 |
| SDMob9 | 321 |  |
| SDMob10 | 325 | NF_VampireSpear = 324, |
| SDMob11 | 326 |  |
| SDMob12 | 327 |  |
| SDMob13 | 328 |  |
| SDMob14 | 329 |  |
| Companion_Pig | 340 | Mon34 |
| Companion_TuskLord | 341 |  |
| Companion_SkeletonLord | 342 |  |
| Companion_Griffin | 343 |  |
| Companion_Dragon | 344 |  |
| Companion_Donkey | 345 |  |
| Companion_Sheep | 346 |  |
| Companion_BanyoLordGuzak | 347 |  |
| Companion_Panda | 348 |  |
| Companion_Rabbit | 349 |  |
| OrangeTiger | 350 |  |
| RegularTiger | 351 |  |
| RedTiger | 352 |  |
| SnowTiger | 353 |  |
| BlackTiger | 354 |  |
| BigBlackTiger | 355 |  |
| BigWhiteTiger | 356 |  |
| OrangeBossTiger | 357 |  |
| BigBossTiger | 358 |  |
| CrystalGolem | 400 | NF_Nameless399 = 399, |
| DustDevil | 411 | NF_Nameless410 = 410, |
| TwinTailScorpion | 412 |  |
| BloodyMole | 413 |  |
| Terracotta1 | 424 | NF_CrystalPillar2 = 423, |
| Terracotta2 | 425 |  |
| Terracotta3 | 426 |  |
| Terracotta4 | 427 |  |
| TerracottaSub | 428 |  |
| TerracottaBoss | 429 |  |
| SDMob19 | 443 | NF_Nameless442 = 442, |
| SDMob20 | 444 |  |
| SDMob21 | 445 |  |
| SDMob22 | 446 |  |
| SDMob23 | 447 |  |
| SDMob24 | 448 |  |
| SDMob25 | 449 |  |
| SDMob26 | 450 |  |
| LobsterLord | 453 |  |
| NewMob1 | 470 | Mon46 |
| NewMob2 | 471 |  |
| NewMob3 | 472 |  |
| NewMob4 | 473 |  |
| NewMob5 | 474 |  |
| NewMob6 | 475 |  |
| NewMob7 | 476 |  |
| NewMob8 | 477 |  |
| NewMob9 | 478 |  |
| NewMob10 | 479 |  |
| MonasteryMon0 | 490 | Mon48 |
| MonasteryMon1 | 491 |  |
| MonasteryMon2 | 492 |  |
| MonasteryMon3 | 493 |  |
| MonasteryMon4 | 494 |  |
| MonasteryMon5 | 495 |  |
| MonasteryMon6 | 496 |  |
| WildBrownHorse | 520 | Mon52 |
| WildWhiteHorse | 521 |  |
| WildBlackHorse | 522 |  |
| WildRedHorse | 524 | NF_Blank523 = 523, |
| SeaHorseCavalry | 530 | MonMagicEx25 |
| Seamancer | 531 |  |
| CoralStoneDuin | 532 |  |
| Brachiopod | 533 |  |
| GiantClam | 534 |  |
| BlueMassif | 535 |  |
| Mollusk | 536 |  |
| Kraken | 537 |  |
| KrakenLeg | 538 |  |
| GiantClam1 | 539 |  |
| SabukGateSouth | 540 | Mon54 |
| SabukGateNorth | 541 |  |
| SabukGateEast | 542 |  |
| SabukGateWest | 543 |  |
| Tornado | 566 | Mon56 |
| Companion_Dog | 570 | Mon57 |
| Companion_Jinchon | 571 |  |
| Companion_Dino | 572 |  |
| CastleFlag | 1000 | Flag |

## 5.3 图像→库/形状/音效映射(291 case 全表)

`UpdateLibraries` 内 `switch (Image)`。三音效列即 case 内 SoundList 的 Attack/Struck/Die 项;帧覆写列 = case 内 `foreach frame in FrameSet.X ... Frames[...] = frame`;Extra 分支标记 Y 的 case 带 `if (Extra) {...} else {...}` 双分支(见 5.4)。

| 怪物 | 库 | BodyShape | Attack音效 | Struck音效 | Die音效 | 帧覆写 | BodyOffSet | Extra分支 |
|---|---|---|---|---|---|---|---|---|
| Chicken | Mon_3 | 0 | ChickenAttack | ChickenStruck | ChickenDie |  |  |  |
| Pig | Mon_12 | 9 | PigAttack | PigStruck | PigDie |  |  |  |
| Deer | Mon_3 | 1 | DeerAttack | DeerStruck | DeerDie |  |  |  |
| Cow | Mon_13 | 1 | CowAttack | CowStruck | CowDie |  |  |  |
| Sheep | Mon_6 | 8 | SheepAttack | SheepStruck | SheepDie |  |  |  |
| SkyStinger | Mon_6 | 9 | SkyStingerAttack | SkyStingerStruck | SkyStingerDie |  |  |  |
| ClawCat | Mon_4 | 8 | ClawCatAttack | ClawCatStruck | ClawCatDie |  |  |  |
| RakingCat | Mon_28 | 6 | ClawCatAttack | ClawCatStruck | ClawCatDie |  |  |  |
| Wolf | Mon_7 | 5 | WolfAttack | WolfStruck | WolfDie |  |  |  |
| ForestYeti | Mon_4 | 0 | ForestYetiAttack | ForestYetiStruck | ForestYetiDie | ForestYeti |  |  |
| ChestnutTree | Mon_13 | 7 |  |  |  | ChestnutTree |  |  |
| CarnivorousPlant | Mon_4 | 1 | CarnivorousPlantAttack | CarnivorousPlantStruck | CarnivorousPlantDie | CarnivorousPlant |  |  |
| Oma | Mon_3 | 3 | OmaAttack | OmaStruck | OmaDie |  |  |  |
| OmaInfant | Mon_28 | 4 | OmaAttack | OmaStruck | OmaDie |  |  |  |
| Yob | Mon_28 | 5 | YobAttack | YobStruck | YobDie |  |  |  |
| TigerSnake | Mon_6 | 7 | TigerSnakeAttack | TigerSnakeStruck | TigerSnakeDie |  |  |  |
| RedSnake | Mon_6 | 4 | TigerSnakeAttack | TigerSnakeStruck | TigerSnakeDie |  |  |  |
| SpittingSpider | Mon_3 | 5 | SpittingSpiderAttack | SpittingSpiderStruck | SpittingSpiderDie |  |  |  |
| Scarecrow | Mon_5 | 0 | ScarecrowAttack | ScarecrowStruck | ScarecrowDie |  |  |  |
| OmaHero | Mon_3 | 4 | OmaHeroAttack | OmaHeroStruck | OmaHeroDie |  |  |  |
| Guard | Mon_3 | 6 |  |  |  |  |  |  |
| ForestGuard | Mon_12 | 3 |  |  |  |  |  |  |
| TownGuard | Mon_12 | 4 |  |  |  |  |  |  |
| SandGuard | Mon_12 | 5 |  |  |  |  |  |  |
| CaveBat | Mon_3 | 9 | CaveBatAttack | CaveBatStruck | CaveBatDie |  |  |  |
| Scorpion | Mon_3 | 8 | ScorpionAttack | ScorpionStruck | ScorpionDie |  |  |  |
| Skeleton | Mon_4 | 2 | SkeletonAttack | SkeletonStruck | SkeletonDie |  |  |  |
| SkeletonAxeMan | Mon_4 | 4 | SkeletonAxeManAttack | SkeletonAxeManStruck | SkeletonAxeManDie |  |  |  |
| SkeletonAxeThrower | Mon_4 | 3 | SkeletonAxeThrowerAttack | SkeletonAxeThrowerStruck | SkeletonAxeThrowerDie |  |  |  |
| SkeletonWarrior | Mon_4 | 5 | SkeletonWarriorAttack | SkeletonWarriorStruck | SkeletonWarriorDie |  |  |  |
| SkeletonLord | Mon_4 | 6 | SkeletonLordAttack | SkeletonLordStruck | SkeletonLordDie |  |  |  |
| CaveMaggot | Mon_4 | 7 | CaveMaggotAttack | CaveMaggotStruck | CaveMaggotDie |  |  |  |
| GhostSorcerer | Mon_5 | 8 | GhostSorcererAttack | GhostSorcererStruck | GhostSorcererDie |  |  |  |
| GhostMage | Mon_5 | 9 | GhostMageAttack | GhostMageStruck | GhostMageDie |  |  |  |
| VoraciousGhost | Mon_6 | 0 | VoraciousGhostAttack | VoraciousGhostStruck | VoraciousGhostDie |  |  |  |
| DevouringGhost | Mon_6 | 1 | VoraciousGhostAttack | VoraciousGhostStruck | VoraciousGhostDie | DevouringGhost |  |  |
| CorpseRaisingGhost | Mon_6 | 2 | VoraciousGhostAttack | VoraciousGhostStruck | VoraciousGhostDie | DevouringGhost |  |  |
| GhoulChampion | Mon_6 | 3 | GhoulChampionAttack | GhoulChampionStruck | GhoulChampionDie |  |  |  |
| ArmoredAnt | Mon_1 | 8 | ArmoredAntAttack | ArmoredAntStruck | ArmoredAntDie |  |  |  |
| AntSoldier | Mon_2 | 4 | ArmoredAntAttack | ArmoredAntStruck | ArmoredAntDie |  |  |  |
| AntHealer | Mon_1 | 7 | ArmoredAntAttack | ArmoredAntStruck | ArmoredAntDie |  |  |  |
| AntNeedler | Mon_10 | 6 | AntNeedlerAttack | AntNeedlerStruck | AntNeedlerDie |  |  |  |
| Beetle | Mon_7 | 3 | KeratoidAttack | KeratoidStruck | KeratoidDie |  |  |  |
| ShellNipper | Mon_7 | 0 | ShellNipperAttack | ShellNipperStruck | ShellNipperDie |  |  |  |
| VisceralWorm | Mon_7 | 1 | VisceralWormAttack | VisceralWormStruck | VisceralWormDie |  |  |  |
| MutantFlea | Mon_15 | 5 | MutantFleaAttack | MutantFleaStruck | MutantFleaDie |  |  |  |
| PurpleFlea | Mon_15 | 6 | MutantFleaAttack | MutantFleaStruck | MutantFleaDie |  |  |  |
| BlasterMutantFlea | Mon_15 | 7 | BlasterMutantFleaAttack | BlasterMutantFleaStruck | BlasterMutantFleaDie |  |  |  |
| BlueBlasterMutantFlea | Mon_15 | 8 | BlasterMutantFleaAttack | BlasterMutantFleaStruck | BlasterMutantFleaDie |  |  |  |
| PoisonousMutantFlea | Mon_15 | 9 | PoisonousMutantFleaAttack | PoisonousMutantFleaStruck | PoisonousMutantFleaDie |  |  |  |
| WaspHatchling | Mon_8 | 1 | WasHatchlingAttack | WasHatchlingStruck | WasHatchlingDie |  |  |  |
| Centipede | Mon_7 | 6 | CentipedeAttack | CentipedeStruck | CentipedeDie |  |  |  |
| ButterflyWorm | Mon_8 | 2 | ButterflyWormAttack | ButterflyWormStruck | ButterflyWormDie |  |  |  |
| MutantMaggot | Mon_7 | 8 | MutantMaggotAttack | MutantMaggotStruck | MutantMaggotDie |  |  |  |
| Earwig | Mon_7 | 9 | EarwigAttack | EarwigStruck | EarwigDie |  |  |  |
| IronLance | Mon_8 | 0 | IronLanceAttack | IronLanceStruck | IronLanceDie |  |  |  |
| LordNiJae | Mon_7 | 7 | LordNiJaeAttack | LordNiJaeStruck | LordNiJaeDie |  |  |  |
| RottingGhoul | Mon_14 | 8 | RottingGhoulAttack | RottingGhoulStruck | RottingGhoulDie |  |  |  |
| DecayingGhoul | Mon_14 | 2 | DecayingGhoulAttack | DecayingGhoulStruck | DecayingGhoulDie |  |  |  |
| BloodThirstyGhoul | Mon_5 | 2 | BloodThirstyGhoulAttack | BloodThirstyGhoulStruck | BloodThirstyGhoulDie |  |  |  |
| SpinedDarkLizard | Mon_5 | 6 | SpinedDarkLizardAttack | SpinedDarkLizardStruck | SpinedDarkLizardDie |  |  |  |
| Dung | Mon_5 | 7 | DungAttack | DungStruck | DungDie |  |  |  |
| UmaInfidel | Mon_5 | 1 | UmaInfidelAttack | UmaInfidelStruck | UmaInfidelDie |  |  |  |
| UmaFlameThrower | Mon_5 | 3 | UmaFlameThrowerAttack | UmaFlameThrowerStruck | UmaFlameThrowerDie |  |  |  |
| UmaTridentInfidel | Mon_28 | 7 | UmaInfidelAttack | UmaInfidelStruck | UmaInfidelDie |  |  |  |
| UmaAnguisher | Mon_5 | 4 | UmaAnguisherAttack | UmaAnguisherStruck | UmaAnguisherDie |  |  |  |
| UmaKing | Mon_5 | 5 | UmaKingAttack | UmaKingStruck | UmaKingDie |  |  |  |
| SpiderBat | Mon_11 | 1 | SpiderBatAttack | SpiderBatStruck | SpiderBatDie |  |  |  |
| ArachnidGazer | Mon_11 | 6 |  | ArachnidGazerStruck | ArachnidGazerDie |  |  |  |
| Larva | Mon_11 | 5 | LarvaAttack | LarvaStruck |  | Larva |  |  |
| RedMoonGuardian | Mon_11 | 7 | RedMoonGuardianAttack | RedMoonGuardianStruck | RedMoonGuardianDie |  |  |  |
| RedMoonProtector | Mon_11 | 8 | RedMoonProtectorAttack | RedMoonProtectorStruck | RedMoonProtectorDie |  |  |  |
| RedMoonRedProtector | Mon_11 | 9 |  |  |  |  |  |  |
| RedMoonGrayProtector | Mon_12 | 0 |  |  |  |  |  |  |
| VenomousArachnid | Mon_12 | 1 | VenomousArachnidAttack | VenomousArachnidStruck | VenomousArachnidDie |  |  |  |
| DarkArachnid | Mon_12 | 2 | DarkArachnidAttack | DarkArachnidStruck | DarkArachnidDie |  |  |  |
| RedMoonTheFallen | Mon_11 | 4 | RedMoonTheFallenAttack | RedMoonTheFallenStruck | RedMoonTheFallenDie |  |  |  |
| ViciousRat | Mon_9 | 1 | ViciousRatAttack | ViciousRatStruck | ViciousRatDie |  |  |  |
| ZumaSharpShooter | Mon_9 | 2 | ZumaSharpShooterAttack | ZumaSharpShooterStruck | ZumaSharpShooterDie |  |  |  |
| ZumaFanatic | Mon_9 | 3 | ZumaFanaticAttack | ZumaFanaticStruck | ZumaFanaticDie | ZumaGuardian |  |  |
| ZumaGuardian | Mon_9 | 4 | ZumaGuardianAttack | ZumaGuardianStruck | ZumaGuardianDie | ZumaGuardian |  |  |
| ZumaKing | Mon_9 | 5 | ZumaKingAttack | ZumaKingStruck | ZumaKingDie | ZumaKing |  |  |
| ArcherGuard | Mon_9 | 6 |  |  |  |  |  |  |
| EvilFanatic | Mon_16 | 7 | EvilFanaticAttack | EvilFanaticStruck | EvilFanaticDie |  |  |  |
| Monkey | Mon_16 | 4 | MonkeyAttack | MonkeyStruck | MonkeyDie | Monkey |  |  |
| EvilElephant | Mon_16 | 8 | EvilElephantAttack | EvilElephantStruck | EvilElephantDie |  |  |  |
| CannibalFanatic | Mon_16 | 6 | CannibalFanaticAttack | CannibalFanaticStruck | CannibalFanaticDie |  |  |  |
| SpikedBeetle | Mon_7 | 4 | SpikedBeetleAttack | SpikedBeetleStruck | SpikedBeetleDie |  |  |  |
| NumaGrunt | Mon_13 | 8 | NumaGruntAttack | NumaGruntStruck | NumaGruntDie |  |  |  |
| NumaWarrior | Mon_13 | 9 | NumaGruntAttack | NumaGruntStruck | NumaGruntDie |  |  |  |
| NumaMage | Mon_2 | 3 | NumaMageAttack | NumaMageStruck | NumaMageDie | NumaMage |  |  |
| NumaElite | Mon_2 | 7 | NumaEliteAttack | NumaEliteStruck | NumaEliteDie |  |  |  |
| Phantom | Mon_2 | 8 |  |  |  |  |  |  |
| SandShark | Mon_10 | 4 | SandSharkAttack | SandSharkStruck | SandSharkDie |  |  |  |
| StoneGolem | Mon_1 | 4 | StoneGolemAttack | StoneGolemStruck | StoneGolemDie |  |  |  |
| WindfurySorceress | Mon_10 | 7 | WindfurySorceressAttack | WindfurySorceressStruck | WindfurySorceressDie |  |  |  |
| CursedCactus | Mon_10 | 5 | CursedCactusAttack | CursedCactusStruck | CursedCactusDie | CursedCactus |  |  |
| NetherWorldGate | Mon_1 | 5 |  |  |  | NetherWorldGate |  |  |
| RagingLizard | Mon_20 | 1 | RagingLizardAttack | RagingLizardStruck | RagingLizardDie |  |  |  |
| SawToothLizard | Mon_20 | 2 | SawToothLizardAttack | SawToothLizardStruck | SawToothLizardDie |  |  |  |
| MutantLizard | Mon_20 | 3 | MutantLizardAttack | MutantLizardStruck | MutantLizardDie |  |  |  |
| VenomSpitter | Mon_20 | 4 | VenomSpitterAttack | VenomSpitterStruck | VenomSpitterDie |  |  |  |
| SonicLizard | Mon_20 | 5 | SonicLizardAttack | SonicLizardStruck | SonicLizardDie | WestDesertLizard |  |  |
| GiantLizard | Mon_20 | 6 | GiantLizardAttack | GiantLizardStruck | GiantLizardDie | WestDesertLizard |  |  |
| CrazedLizard | Mon_20 | 9 | CrazedLizardAttack | CrazedLizardStruck | CrazedLizardDie |  |  |  |
| TaintedTerror | Mon_20 | 7 | TaintedTerrorAttack | TaintedTerrorStruck | TaintedTerrorDie | WestDesertLizard |  |  |
| DeathLordJichon | Mon_20 | 8 | DeathLordJichonAttack | DeathLordJichonStruck | DeathLordJichonDie |  |  |  |
| Minotaur | Mon_14 | 7 | MinotaurAttack | MinotaurStruck | MinotaurDie |  |  |  |
| FrostMinotaur | Mon_14 | 3 | FrostMinotaurAttack | FrostMinotaurStruck | FrostMinotaurDie |  |  |  |
| ShockMinotaur | Mon_14 | 4 | FrostMinotaurAttack | FrostMinotaurStruck | FrostMinotaurDie |  |  |  |
| FlameMinotaur | Mon_14 | 6 | FrostMinotaurAttack | FrostMinotaurStruck | FrostMinotaurDie |  |  |  |
| FuryMinotaur | Mon_14 | 5 | FrostMinotaurAttack | FrostMinotaurStruck | FrostMinotaurDie |  |  |  |
| BanyaLeftGuard | Mon_14 | 1 | BanyaLeftGuardAttack | BanyaLeftGuardStruck | BanyaLeftGuardDie | BanyaGuard |  |  |
| BanyaRightGuard | Mon_14 | 0 | BanyaLeftGuardAttack | BanyaLeftGuardStruck | BanyaLeftGuardDie | BanyaGuard |  |  |
| EmperorSaWoo | Mon_14 | 9 | EmperorSaWooAttack | EmperorSaWooStruck | EmperorSaWooDie | EmperorSaWoo |  |  |
| BoneArcher | Mon_15 | 4 | BoneArcherAttack | BoneArcherStruck | BoneArcherDie |  |  |  |
| BoneBladesman | Mon_15 | 3 | BoneArcherAttack | BoneArcherStruck | BoneArcherDie |  |  |  |
| BoneCaptain | Mon_15 | 0 | BoneCaptainAttack | BoneCaptainStruck | BoneCaptainDie |  |  |  |
| BoneSoldier | Mon_15 | 2 | BoneArcherAttack | BoneArcherStruck | BoneArcherDie |  |  |  |
| ArchLichTaedu | Mon_15 | 1 | ArchLichTaeduAttack | ArchLichTaeduStruck | ArchLichTaeduDie | ArchLichTaeda |  |  |
| WedgeMothLarva | Mon_8 | 3 | WedgeMothLarvaAttack | WedgeMothLarvaStruck | WedgeMothLarvaDie |  |  |  |
| LesserWedgeMoth | Mon_8 | 4 | LesserWedgeMothAttack | LesserWedgeMothStruck | LesserWedgeMothDie |  |  |  |
| WedgeMoth | Mon_8 | 5 | WedgeMothAttack | WedgeMothStruck | WedgeMothDie |  |  |  |
| RedBoar | Mon_8 | 6 | RedBoarAttack | RedBoarStruck | RedBoarDie |  |  |  |
| ClawSerpent | Mon_8 | 9 | ClawSerpentAttack | ClawSerpentStruck | ClawSerpentDie |  |  |  |
| BlackBoar | Mon_8 | 7 | BlackBoarAttack | BlackBoarStruck | BlackBoarDie |  |  |  |
| TuskLord | Mon_8 | 8 | TuskLordAttack | TuskLordStruck | TuskLordDie |  |  |  |
| RazorTusk | Mon_16 | 0 | RazorTuskAttack | RazorTuskStruck | RazorTuskDie |  |  |  |
| PinkGoddess | Mon_17 | 2 | PinkGoddessAttack | PinkGoddessStruck | PinkGoddessDie |  |  |  |
| GreenGoddess | Mon_17 | 3 | GreenGoddessAttack | GreenGoddessStruck | GreenGoddessDie |  |  |  |
| MutantCaptain | Mon_17 | 1 | MutantCaptainAttack | MutantCaptainStruck | MutantCaptainDie | WestDesertLizard |  |  |
| StoneGriffin | Mon_17 | 0 | StoneGriffinAttack | StoneGriffinStruck | StoneGriffinDie | BanyaGuard |  |  |
| FlameGriffin | Mon_16 | 9 | FlameGriffinAttack | FlameGriffinStruck | FlameGriffinDie | BanyaGuard |  |  |
| JinchonDevil | Mon_17 | 4 | JinchonDevilAttack | JinchonDevilStruck | JinchonDevilDie | JinchonDevil |  |  |
| WhiteBone | Mon_6 | 6 | WhiteBoneAttack | WhiteBoneStruck | WhiteBoneDie |  |  |  |
| Shinsu | Mon_10 | 0 | ShinsuBigAttack | ShinsuBigStruck | ShinsuBigDie |  |  | Y |
| CorpseStalker | Mon_2 | 2 | CorpseStalkerAttack | CorpseStalkerStruck | CorpseStalkerDie |  |  |  |
| LightArmedSoldier | Mon_1 | 6 | LightArmedSoldierAttack | LightArmedSoldierStruck | LightArmedSoldierDie |  |  |  |
| CorrosivePoisonSpitter | Mon_10 | 3 | CorrosivePoisonSpitterAttack | CorrosivePoisonSpitterStruck | CorrosivePoisonSpitterDie |  |  |  |
| PhantomSoldier | Mon_10 | 9 | PhantomSoldierAttack | PhantomSoldierStruck | PhantomSoldierDie |  |  |  |
| MutatedOctopus | Mon_1 | 2 | MutatedOctopusAttack | MutatedOctopusStruck | MutatedOctopusDie |  |  |  |
| AquaLizard | Mon_10 | 2 | AquaLizardAttack | AquaLizardStruck | AquaLizardDie |  |  |  |
| Stomper | Mon_1 | 9 | AquaLizardAttack | AquaLizardStruck | AquaLizardDie |  |  |  |
| CrimsonNecromancer | Mon_2 | 9 | CrimsonNecromancerAttack | CrimsonNecromancerStruck | CrimsonNecromancerDie |  |  |  |
| ChaosKnight | Mon_2 | 0 | ChaosKnightAttack | ChaosKnightStruck | ChaosKnightDie | BanyaGuard |  |  |
| PachonTheChaosBringer | Mon_13 | 0 | PachontheChaosbringerAttack | PachontheChaosbringerStruck | PachontheChaosbringerDie | PachonTheChaosBringer |  |  |
| NumaCavalry | Mon_19 | 0 | NumaCavalryAttack | NumaCavalryStruck | NumaCavalryDie | BanyaGuard |  |  |
| NumaHighMage | Mon_19 | 4 | NumaHighMageAttack | NumaHighMageStruck | NumaHighMageDie | BanyaGuard |  |  |
| NumaStoneThrower | Mon_19 | 3 | NumaStoneThrowerAttack | NumaStoneThrowerStruck | NumaStoneThrowerDie |  |  |  |
| NumaRoyalGuard | Mon_19 | 5 | NumaRoyalGuardAttack | NumaRoyalGuardStruck | NumaRoyalGuardDie | EmperorSaWoo |  |  |
| NumaArmoredSoldier | Mon_19 | 1 | NumaArmoredSoldierAttack | NumaArmoredSoldierStruck | NumaArmoredSoldierDie | BanyaGuard |  |  |
| IcyRanger | Mon_21 | 0 | IcyRangerAttack | IcyRangerStruck | IcyRangerDie |  |  |  |
| IcyGoddess | Mon_18 | 0 | IcyGoddessAttack | IcyGoddessStruck | IcyGoddessDie |  |  |  |
| IcySpiritWarrior | Mon_21 | 2 | IcySpiritWarriorAttack | IcySpiritWarriorStruck | IcySpiritWarriorDie | NumaMage |  |  |
| IcySpiritGeneral | Mon_21 | 3 | IcySpiritWarriorAttack | IcySpiritWarriorStruck | IcySpiritWarriorDie | IcySpiritGeneral |  |  |
| GhostKnight | Mon_21 | 4 | GhostKnightAttack | GhostKnightStruck | GhostKnightDie | EmperorSaWoo |  |  |
| IcySpiritSpearman | Mon_21 | 6 | IcySpiritSpearmanAttack | IcySpiritSpearmanStruck | IcySpiritSpearmanDie | BanyaGuard |  |  |
| Werewolf | Mon_21 | 7 | WerewolfAttack | WerewolfStruck | WerewolfDie | BanyaGuard |  |  |
| Whitefang | Mon_21 | 8 | WhitefangAttack | WhitefangStruck | WhitefangDie | BanyaGuard |  |  |
| IcySpiritSolider | Mon_21 | 9 | IcySpiritSoliderAttack | IcySpiritSoliderStruck | IcySpiritSoliderDie | BanyaGuard |  |  |
| WildBoar | Mon_18 | 1 | WildBoarAttack | WildBoarStruck | WildBoarDie |  |  |  |
| JinamStoneGate | Mon_23 | 9 |  |  |  | JinamStoneGate |  |  |
| FrostLordHwa | Mon_21 | 5 | FrostLordHwaAttack | FrostLordHwaStruck | FrostLordHwaDie |  |  |  |
| Companion_Pig | Mon_34 | 0 |  |  |  | Companion_Pig |  |  |
| Companion_TuskLord | Mon_34 | 1 |  |  |  | Companion_TuskLord |  |  |
| Companion_SkeletonLord | Mon_34 | 2 |  |  |  | Companion_SkeletonLord |  |  |
| Companion_Griffin | Mon_34 | 3 |  |  |  | Companion_Griffin |  |  |
| Companion_Dragon | Mon_34 | 4 |  |  |  | Companion_Dragon |  |  |
| Companion_Donkey | Mon_34 | 5 |  |  |  | Companion_Donkey |  |  |
| Companion_Sheep | Mon_34 | 6 |  |  |  | Companion_Sheep |  |  |
| Companion_BanyoLordGuzak | Mon_34 | 7 |  |  |  | Companion_BanyoLordGuzak |  |  |
| Companion_Panda | Mon_34 | 8 |  |  |  | Companion_Panda |  |  |
| Companion_Rabbit | Mon_34 | 9 |  |  |  | Companion_Rabbit |  |  |
| Companion_Dog | Mon_57 | 0 |  |  |  | Companion_Dog |  |  |
| Companion_Jinchon | Mon_57 | 1 |  |  |  | Companion_Jinchon |  |  |
| Companion_Dino | Mon_57 | 2 |  |  |  | Companion_Dino |  |  |
| InfernalSoldier | Mon_26 | 0 |  |  |  | InfernalSoldier |  |  |
| OmaWarlord | Mon_3 | 7 | OmaHeroAttack | OmaHeroStruck | OmaHeroDie |  |  |  |
| EscortCommander | Mon_22 | 0 | EscortCommanderAttack | EscortCommanderStruck | EscortCommanderDie | BanyaGuard |  |  |
| FieryDancer | Mon_22 | 2 | FieryDancerAttack | FieryDancerStruck | FieryDancerDie | FieryDancer |  |  |
| EmeraldDancer | Mon_22 | 3 | EmeraldDancerAttack | EmeraldDancerStruck | EmeraldDancerDie | EmeraldDancer |  |  |
| QueenOfDawn | Mon_22 | 1 | QueenOfDawnAttack | QueenOfDawnStruck | QueenOfDawnDie | QueenOfDawn |  |  |
| OYoungBeast | Mon_23 | 3 | OYoungBeastAttack | OYoungBeastStruck | OYoungBeastDie | OYoungBeast |  |  |
| YumgonWitch | Mon_23 | 6 | YumgonWitchAttack | YumgonWitchStruck | YumgonWitchDie | YumgonWitch |  |  |
| MaWarlord | Mon_23 | 4 | MaWarlordAttack | MaWarlordStruck | MaWarlordDie | OYoungBeast |  |  |
| JinhwanSpirit | Mon_23 | 7 | JinhwanSpiritAttack | JinhwanSpiritStruck | JinhwanSpiritDie | JinhwanSpirit |  |  |
| JinhwanGuardian | Mon_23 | 8 | JinhwanGuardianAttack | JinhwanGuardianStruck | JinhwanGuardianDie | JinhwanSpirit |  |  |
| YumgonGeneral | Mon_23 | 5 | YumgonGeneralAttack | YumgonGeneralStruck | YumgonGeneralDie | OYoungBeast |  |  |
| ChiwooGeneral | Mon_23 | 0 | ChiwooGeneralAttack | ChiwooGeneralStruck | ChiwooGeneralDie | ChiwooGeneral |  |  |
| DragonQueen | Mon_23 | 2 | DragonQueenAttack | DragonQueenStruck | DragonQueenDie | DragonQueen |  |  |
| DragonLord | Mon_23 | 1 | DragonLordAttack | DragonLordStruck | DragonLordDie | DragonLord |  |  |
| FerociousIceTiger | Mon_21 | 1 | FerociousIceTigerAttack | FerociousIceTigerStruck | FerociousIceTigerDie | FerociousIceTiger |  |  |
| SamaFireGuardian | Mon_25 | 0 | SamaFireGuardianAttack | SamaFireGuardianStruck | SamaFireGuardianDie | SamaFireGuardian |  |  |
| SamaIceGuardian | Mon_25 | 1 | SamaIceGuardianAttack | SamaIceGuardianStruck | SamaIceGuardianDie | SamaFireGuardian |  |  |
| SamaLightningGuardian | Mon_25 | 2 | SamaLightningGuardianAttack | SamaLightningGuardianStruck | SamaLightningGuardianDie | SamaFireGuardian |  |  |
| SamaWindGuardian | Mon_25 | 3 | SamaWindGuardianAttack | SamaWindGuardianStruck | SamaWindGuardianDie | SamaFireGuardian |  |  |
| Phoenix | Mon_25 | 4 | PhoenixAttack | PhoenixStruck | PhoenixDie | Phoenix |  |  |
| BlackTortoise | Mon_25 | 5 | BlackTortoiseAttack | BlackTortoiseStruck | BlackTortoiseDie | Phoenix |  |  |
| BlueDragon | Mon_25 | 6 | BlueDragonAttack | BlueDragonStruck | BlueDragonDie | Phoenix |  |  |
| WhiteTiger | Mon_25 | 7 | WhiteTigerAttack | WhiteTigerStruck | WhiteTigerDie | Phoenix |  |  |
| EnshrinementBox | Mon_27 | 5 |  |  |  | EnshrinementBox |  |  |
| BloodStone | Mon_19 | 7 |  |  |  | BloodStone |  |  |
| SamaCursedBladesman | Mon_27 | 0 |  |  |  | SamaCursedBladesman |  |  |
| SamaCursedSlave | Mon_27 | 1 |  |  |  | SamaCursedSlave |  |  |
| SamaCursedFlameMage | Mon_27 | 2 |  |  |  | SamaCursedSlave |  |  |
| SamaProphet | Mon_27 | 3 |  |  |  | SamaProphet |  |  |
| SamaSorcerer | Mon_27 | 4 |  |  |  | SamaSorcerer |  |  |
| UmaMaceInfidel | Mon_27 | 8 | UmaInfidelAttack | UmaInfidelStruck | UmaInfidelDie |  |  |  |
| OrangeTiger | Mon_35 | 0 |  |  |  | OrangeTiger |  |  |
| RegularTiger | Mon_35 | 1 |  |  |  | OrangeTiger |  |  |
| RedTiger | Mon_35 | 2 |  |  |  | RedTiger |  |  |
| SnowTiger | Mon_35 | 3 |  |  |  | OrangeTiger |  |  |
| BlackTiger | Mon_35 | 4 |  |  |  | OrangeTiger |  |  |
| BigBlackTiger | Mon_35 | 5 |  |  |  | OrangeTiger |  |  |
| BigWhiteTiger | Mon_35 | 6 |  |  |  | OrangeTiger |  |  |
| OrangeBossTiger | Mon_35 | 7 |  |  |  | OrangeBossTiger |  |  |
| BigBossTiger | Mon_35 | 8 |  |  |  | OrangeBossTiger |  |  |
| WildMonkey | Mon_30 | 0 | MonkeyAttack | MonkeyStruck | MonkeyDie | Monkey |  |  |
| FrostYeti | Mon_30 | 1 | ForestYetiAttack | ForestYetiStruck | ForestYetiDie | ForestYeti |  |  |
| EvilSnake | Mon_9 | 0 | ClawSerpentAttack | ClawSerpentStruck | ClawSerpentDie |  |  |  |
| Salamander | Mon_28 | 0 |  |  |  |  |  |  |
| SandGolem | Mon_28 | 1 |  |  |  | SDMob3 |  |  |
| SDMob4 | Mon_29 | 0 |  |  |  |  |  |  |
| SDMob5 | Mon_29 | 1 |  |  |  |  |  |  |
| SDMob6 | Mon_29 | 2 |  |  |  |  |  |  |
| SDMob7 | Mon_29 | 8 |  |  |  |  |  |  |
| OmaMage | Mon_29 | 9 |  |  |  | SDMob8 |  |  |
| SDMob9 | Mon_32 | 1 |  |  |  |  |  |  |
| SDMob10 | Mon_32 | 5 |  |  |  |  |  |  |
| SDMob11 | Mon_32 | 6 |  |  |  |  |  |  |
| SDMob12 | Mon_32 | 7 |  |  |  |  |  |  |
| SDMob13 | Mon_32 | 8 |  |  |  |  |  |  |
| SDMob14 | Mon_32 | 9 |  |  |  |  |  |  |
| CrystalGolem | Mon_40 | 0 |  |  |  | SDMob15 |  |  |
| DustDevil | Mon_41 | 1 |  |  |  | SDMob16 |  |  |
| TwinTailScorpion | Mon_41 | 2 |  |  |  | SDMob17 |  |  |
| BloodyMole | Mon_41 | 3 |  |  |  | SDMob18 |  |  |
| SDMob19 | Mon_44 | 3 |  |  |  | SDMob19 |  |  |
| SDMob20 | Mon_44 | 4 |  |  |  | SDMob19 |  |  |
| SDMob21 | Mon_44 | 5 |  |  |  | SDMob21 |  |  |
| SDMob22 | Mon_44 | 6 |  |  |  | SDMob22 |  |  |
| SDMob23 | Mon_44 | 7 |  |  |  | SDMob23 |  |  |
| SDMob24 | Mon_44 | 8 |  |  |  | SDMob24 |  |  |
| SDMob25 | Mon_44 | 9 |  |  |  | SDMob25 |  |  |
| SDMob26 | Mon_45 | 0 |  |  |  | SDMob26 |  |  |
| GangSpider | Mon_28 | 8 |  |  |  |  |  |  |
| VenomSpider | Mon_28 | 9 |  |  |  |  |  |  |
| LobsterLord | Mon_45 | 3 |  |  |  | LobsterLord |  |  |
| NewMob1 | Mon_47 | 0 |  |  |  |  |  |  |
| NewMob2 | Mon_47 | 1 |  |  |  | BobbitWorm |  |  |
| NewMob3 | Mon_47 | 2 |  |  |  |  |  |  |
| NewMob4 | Mon_47 | 3 |  |  |  | LobsterLord |  |  |
| NewMob5 | Mon_47 | 4 |  |  |  | LobsterLord |  |  |
| NewMob6 | Mon_47 | 5 |  |  |  | LobsterLord |  |  |
| NewMob7 | Mon_47 | 6 |  |  |  | LobsterLord |  |  |
| NewMob8 | Mon_47 | 7 |  |  |  | LobsterLord |  |  |
| NewMob9 | Mon_47 | 8 |  |  |  | LobsterLord |  |  |
| NewMob10 | Mon_47 | 9 |  |  |  | DeadTree |  |  |
| MonasteryMon0 | Mon_49 | 0 |  |  |  |  |  |  |
| MonasteryMon1 | Mon_49 | 1 |  |  |  | MonasteryMon1 |  |  |
| MonasteryMon2 | Mon_49 | 2 |  |  |  |  |  |  |
| MonasteryMon3 | Mon_49 | 3 |  |  |  | MonasteryMon3 |  |  |
| MonasteryMon4 | Mon_49 | 4 |  |  |  |  |  |  |
| MonasteryMon5 | Mon_49 | 5 |  |  |  |  |  |  |
| MonasteryMon6 | Mon_49 | 6 |  |  |  |  |  |  |
| Terracotta1 | Mon_42 | 4 | Terracotta1Attack | Terracotta1Struck | Terracotta1Die | Terracotta1 |  |  |
| Terracotta2 | Mon_42 | 5 | Terracotta2Attack | Terracotta2Struck | Terracotta2Die | Terracotta2 |  |  |
| Terracotta3 | Mon_42 | 6 | Terracotta3Attack | Terracotta3Struck | Terracotta3Die | Terracotta3 |  |  |
| Terracotta4 | Mon_42 | 7 | Terracotta4Attack | Terracotta4Struck | Terracotta4Die | Terracotta3 |  |  |
| TerracottaSub | Mon_42 | 8 | TerracottaSubAttack | TerracottaSubStruck | TerracottaSubDie | TerracottaSub |  |  |
| TerracottaBoss | Mon_42 | 9 | TerracottaBossAttack2 | TerracottaBossStruck | TerracottaBossDie | TerracottaBoss |  |  |
| WildBrownHorse | Mon_52 | 0 |  |  |  |  |  |  |
| WildWhiteHorse | Mon_52 | 1 |  |  |  |  |  |  |
| WildBlackHorse | Mon_52 | 2 |  |  |  |  |  |  |
| WildRedHorse | Mon_52 | 4 |  |  |  |  |  |  |
| SeaHorseCavalry | Mon_53 | 0 |  |  |  | SeaHorseCavalry |  |  |
| Seamancer | Mon_53 | 1 |  |  |  | Seamancer |  |  |
| CoralStoneDuin | Mon_53 | 2 |  |  |  | CoralStoneDuin |  |  |
| Brachiopod | Mon_53 | 3 |  |  |  | Brachiopod |  |  |
| GiantClam | Mon_53 | 4 |  |  |  | GiantClam |  |  |
| BlueMassif | Mon_53 | 5 |  |  |  | BlueMassif |  |  |
| Mollusk | Mon_53 | 6 |  |  |  | Mollusk |  |  |
| Kraken | Mon_53 | 7 |  |  |  |  |  |  |
| KrakenLeg | Mon_53 | 8 |  |  |  |  |  |  |
| GiantClam1 | Mon_53 | 9 |  |  |  | GiantClam1 |  |  |
| Tornado | Mon_56 | 6 |  |  |  | Tornado |  |  |
| CastleFlag | CastleFlag | (变量) |  |  |  | CastleFlag | 100 |  |
| SabukGateSouth | Mon_54 | 0 |  |  |  | SabukGate |  |  |
| SabukGateNorth | Mon_54 | 1 |  |  |  | SabukGate |  |  |
| SabukGateEast | Mon_54 | 2 |  |  |  | SabukGate |  |  |
| SabukGateWest | Mon_54 | 3 |  |  |  | SabukGate |  |  |

## 5.4 特殊 case 明细

| 怪物 | 规则 |
|---|---|
| `Shinsu` | `if (Extra)` → `Mon_10`/shape 0 + ShinsuBig 音效;`else` → `Mon_9`/shape 9 + ShinsuSmall 音效 |
| `CastleFlag` | `CastleFlag` 库(即 `Flag`)、`BodyOffSet = 100`(覆写 1000)、`Frames = FrameSet.CastleFlag`、`BodyShape = Extra1`(**变量**,表格中记为"变量") |
| `OmaMage` | `Mon_29`/shape 9;帧覆写 `FrameSet.SDMob8` |
| `SDMob25` | `Mon_44`/shape 9;帧覆写 `FrameSet.SDMob25` |
| `SDMob26` | `Mon_45`/shape 0;帧覆写 `FrameSet.SDMob26` |
| `GangSpider` | `Mon_28`/shape 8 |
| `VenomSpider` | `Mon_28`/shape 9 |
| `LobsterLord` | `Mon_45`/shape 3;三层绘制(见 5.1) |
| `NewMob1/2/3` | `Mon_47`/shape 0/1/…;NewMob1 另有 MonMagicEx20 发光叠加 |
| `NewMob10` | `Mon_47`/shape 9;GetBodyDrawY y 减 4 格 |

**事件覆写**(怪物 case 内的 `if (EasterEvent)` 分支):Easter → `Mon_30`/shape 4 + `FrameSet.EasterEvent` 帧合并;else 分支接 Christmas/Halloween 覆写(帧表如 `FrameSet.ChristmasEvent`/`FrameSet.HalloweenEvent`)。

**帧覆写通用模式**(如 LobsterLord/NewMob2→BobbitWorm 等):

```
foreach (KeyValuePair<MirAnimation, Frame> frame in FrameSet.X) Frames[frame.Key] = frame.Value;
```

未覆写时默认 `Frames = new Dictionary<MirAnimation, Frame>(FrameSet.DefaultMonster)`。

## 5.5 每怪动作/音效/特效(45 条目)

数据来自 `SetAction` 内两个 `switch(Image)`(switch#0 = 48 case,switch#1 = 40 case,位于 `base.SetAction(action)` 前);同名怪物出现在两个动作上下文时合并列出。音效列 = case 内 `DXSoundManager.Play` 的音效名;特效列 = MirEffect/MirProjectile 参数(`特效/投射(帧号,帧数,延时ms,库,光强起→光强止,颜色)` + 对象初始化器关键属性)。

| 怪物 | 动作 | 音效 | 特效 |
|---|---|---|---|

| Shinsu | Hide, Dead, Show, Attack | ShinsuShow, ShinsuBigAttack | 特效(980,6,100ms,LibraryFile.MonMagic,20→40,Globals.PhantomColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(400), Target=this}; 特效(980,6,100ms,LibraryFile.MonMagic,20→40,Globals.PhantomColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(400), Target=this} |
| InfernalSoldier | Dead |  |  |
| CarnivorousPlant |  |  |  |
| LordNiJae | Attack |  | 特效(361,9,100ms,LibraryFile.MonMagic,0→0,Globals.DarkColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(400), Target=this}; 特效(361,9,100ms,LibraryFile.MonMagic,0→0,Globals.DarkColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(400), Target=this} |
| StoneGolem | Show | StoneGolemAppear | 特效(200,1,TimeSpan.FromMinutes(1ms,LibraryFile.ProgUse,0→0,Globals.NoneColour) {Skip=1, DrawType=DrawType.Floor, Direction=Direction, MapTarget=action.Location, Target=action.Location} |
| ZumaFanatic |  |  |  |
| ZumaGuardian | Show |  |  |
| ZumaKing | Show, Attack | ZumaKingAppear | 特效(210,1,TimeSpan.FromMinutes(1ms,LibraryFile.ProgUse,0→0,Globals.NoneColour) {DrawType=DrawType.Floor, MapTarget=action.Location, Target=action.Location}; 特效(720,8,100ms,LibraryFile.MonMagic,0→0,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(720,8,100ms,LibraryFile.MonMagic,0→0,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this} |
| Scarecrow | Die |  | 特效(680,10,100ms,LibraryFile.MonMagic,20→40,Globals.FireColour) {Blend=true, Target=this}; 特效(680,10,100ms,LibraryFile.MonMagic,20→40,Globals.FireColour) {Blend=true, Target=this} |
| Skeleton |  |  |  |
| SkeletonAxeThrower |  |  |  |
| SkeletonWarrior |  |  |  |
| SkeletonLord | Die |  | 特效(1920,10,100ms,LibraryFile.MonMagic,20→40,Globals.FireColour) {Blend=true, Target=this}; 特效(1920,10,100ms,LibraryFile.MonMagic,20→40,Globals.FireColour) {Blend=true, Target=this} |
| GhostSorcerer | Attack, Die |  | 特效(600,6,100ms,LibraryFile.MonMagic,20→40,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(700,8,100ms,LibraryFile.MonMagic,20→40,Globals.LightningColour) {Blend=true, Target=this}; 特效(600,6,100ms,LibraryFile.MonMagic,20→40,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(700,8,100ms,LibraryFile.MonMagic,20→40,Globals.LightningColour) {Blend=true, Target=this} |
| CaveMaggot | Attack |  | 特效(1940,5,100ms,LibraryFile.MonMagic,0→0,Globals.DarkColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this}; 特效(1940,5,100ms,LibraryFile.MonMagic,0→0,Globals.DarkColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this} |
| RottingGhoul | Die |  | 特效(490,10,100ms,LibraryFile.MonMagicEx,20→40,Globals.LightningColour) {Blend=true, Target=this}; 特效(490,10,100ms,LibraryFile.MonMagicEx,20→40,Globals.LightningColour) {Blend=true, Target=this} |
| DecayingGhoul | Attack, Die |  | 特效(310,6,100ms,LibraryFile.MonMagicEx,20→40,Globals.LightningColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(400), Target=this}; 特效(490,10,100ms,LibraryFile.MonMagicEx,20→40,Globals.LightningColour) {Blend=true, Target=this}; 特效(310,6,100ms,LibraryFile.MonMagicEx,20→40,Globals.LightningColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(400), Target=this}; 特效(490,10,100ms,LibraryFile.MonMagicEx,20→40,Globals.LightningColour) {Blend=true, Target=this} |
| UmaFlameThrower | Attack |  | 特效(520,6,100ms,LibraryFile.MonMagic,20→40,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(520,6,100ms,LibraryFile.MonMagic,20→40,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this} |
| UmaKing | Attack |  | 特效(440,6,100ms,LibraryFile.MonMagic,50→80,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(440,6,100ms,LibraryFile.MonMagic,50→80,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this} |
| BanyaLeftGuard | Attack, Die |  | 特效(100,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(200,5,100ms,LibraryFile.MonMagicEx,0→0,Globals.FireColour) {Blend=true, Target=this}; 特效(100,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(200,5,100ms,LibraryFile.MonMagicEx,0→0,Globals.FireColour) {Blend=true, Target=this} |
| BanyaRightGuard | Attack, Die |  | 特效(0,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(90,5,100ms,LibraryFile.MonMagicEx,0→0,Globals.LightningColour) {Blend=true, Target=this}; 特效(0,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(90,5,100ms,LibraryFile.MonMagicEx,0→0,Globals.LightningColour) {Blend=true, Target=this} |
| EmperorSaWoo | Attack |  | 特效(510,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(510,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this} |
| BoneArcher |  |  |  |
| BoneSoldier |  |  |  |
| BoneBladesman | Die |  | 特效(630,8,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Target=this}; 特效(630,8,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Target=this} |
| BoneCaptain | Die |  | 特效(650,10,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(650,10,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this} |
| ArchLichTaedu | RangeAttack, Show, Die |  | 特效(1470,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(1390,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(1630,17,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Skip=20, Direction=action.Direction, Target=this}; 特效(1470,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(1390,6,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(1630,17,100ms,LibraryFile.MonMagicEx,0→0,Globals.NoneColour) {Blend=true, Skip=20, Direction=action.Direction, Target=this} |
| RazorTusk | Attack |  | 特效(1800,6,100ms,LibraryFile.MonMagicEx,20→40,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(1800,6,100ms,LibraryFile.MonMagicEx,20→40,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this} |
| Stomper | Attack |  | 特效(1779,8,100ms,LibraryFile.MonMagic,0→0,Globals.NoneColour) {Blend=true, Target=this}; 特效(1779,8,100ms,LibraryFile.MonMagic,0→0,Globals.NoneColour) {Blend=true, Target=this} |
| PachonTheChaosBringer | Attack, Die |  | 特效(1800,10,100ms,LibraryFile.MonMagic,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(1890,18,100ms,LibraryFile.MonMagic,0→0,Globals.NoneColour) {Blend=true, Target=this}; 特效(1800,10,100ms,LibraryFile.MonMagic,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(1890,18,100ms,LibraryFile.MonMagic,0→0,Globals.NoneColour) {Blend=true, Target=this} |
| JinchonDevil | RangeAttack, Attack |  | 特效(760,9,70ms,LibraryFile.MonMagicEx2,10→35,Globals.DarkColour) {Blend=true, Direction=Direction, Target=this}; 特效(990,9,70ms,LibraryFile.MonMagicEx2,10→35,Globals.DarkColour) {Blend=true, Direction=Direction, Target=this}; 特效(760,9,70ms,LibraryFile.MonMagicEx2,10→35,Globals.DarkColour) {Blend=true, Direction=Direction, Target=this}; 特效(990,9,70ms,LibraryFile.MonMagicEx2,10→35,Globals.DarkColour) {Blend=true, Direction=Direction, Target=this} |
| EmeraldDancer | Attack, RangeAttack |  | 特效(290,20,100ms,LibraryFile.MonMagicEx5,10→35,Globals.DarkColour) {Blend=true, Skip=20, Direction=Direction, Target=this}; 特效(540,20,100ms,LibraryFile.MonMagicEx5,10→35,Globals.DarkColour) {Blend=true, Target=this}; 特效(290,20,100ms,LibraryFile.MonMagicEx5,10→35,Globals.DarkColour) {Blend=true, Skip=20, Direction=Direction, Target=this}; 特效(540,20,100ms,LibraryFile.MonMagicEx5,10→35,Globals.DarkColour) {Blend=true, Target=this} |
| FieryDancer | Attack, RangeAttack |  | 特效(570,10,100ms,LibraryFile.MonMagicEx5,10→35,Globals.FireColour) {Blend=true, Target=this}; 特效(620,10,100ms,LibraryFile.MonMagicEx5,10→35,Globals.FireColour) {Blend=true, Target=this}; 特效(570,10,100ms,LibraryFile.MonMagicEx5,10→35,Globals.FireColour) {Blend=true, Target=this}; 特效(620,10,100ms,LibraryFile.MonMagicEx5,10→35,Globals.FireColour) {Blend=true, Target=this} |
| QueenOfDawn | Attack, RangeAttack |  | 特效(680,6,100ms,LibraryFile.MonMagicEx5,10→35,Globals.HolyColour) {Blend=true, Direction=Direction, Target=this}; 特效(460,11,100ms,LibraryFile.MonMagicEx5,30→80,Globals.HolyColour) {Blend=true, Target=this}; 特效(680,6,100ms,LibraryFile.MonMagicEx5,10→35,Globals.HolyColour) {Blend=true, Direction=Direction, Target=this}; 特效(460,11,100ms,LibraryFile.MonMagicEx5,30→80,Globals.HolyColour) {Blend=true, Target=this} |
| OYoungBeast | RangeAttack |  | 特效(600,10,100ms,LibraryFile.MonMagicEx6,0→0,Globals.NoneColour) {Blend=true, Direction=Direction, Target=this}; 特效(600,10,100ms,LibraryFile.MonMagicEx6,0→0,Globals.NoneColour) {Blend=true, Direction=Direction, Target=this} |
| MaWarlord | Attack |  | 特效(1100,10,100ms,LibraryFile.MonMagicEx6,0→0,Globals.NoneColour) {Blend=true, Direction=Direction, Target=this}; 特效(1100,10,100ms,LibraryFile.MonMagicEx6,0→0,Globals.NoneColour) {Blend=true, Direction=Direction, Target=this} |
| DragonQueen | Attack |  | 特效(500,20,100ms,LibraryFile.MonMagicEx6,10→35,Globals.DarkColour) {Blend=true, Target=this}; 特效(500,20,100ms,LibraryFile.MonMagicEx6,10→35,Globals.DarkColour) {Blend=true, Target=this} |
| FerociousIceTiger | Attack, RangeAttack |  | 特效(700,7,100ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, StartTime=CEnvir.Now.AddMilliseconds(600), MapTarget=Functions.Move(CurrentLocation, Target=Functions.Move(CurrentLocation}; 特效(801,16,40ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, Target=this}; 特效(801,16,40ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, StartTime=CEnvir.Now.AddMilliseconds(150), Target=this}; 特效(801,16,40ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, StartTime=CEnvir.Now.AddMilliseconds(300), Target=this}; 特效(801,16,40ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, StartTime=CEnvir.Now.AddMilliseconds(450), Target=this}; 特效(700,7,100ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, StartTime=CEnvir.Now.AddMilliseconds(600), MapTarget=Functions.Move(CurrentLocation, Target=Functions.Move(CurrentLocation}; 特效(801,16,40ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, Target=this}; 特效(801,16,40ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, StartTime=CEnvir.Now.AddMilliseconds(150), Target=this}; 特效(801,16,40ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, StartTime=CEnvir.Now.AddMilliseconds(300), Target=this}; 特效(801,16,40ms,LibraryFile.MonMagicEx7,0→0,Globals.NoneColour) {Blend=true, StartTime=CEnvir.Now.AddMilliseconds(450), Target=this} |
| NewMob1 | Attack, RangeAttack |  | 特效(1500,7,100ms,LibraryFile.MonMagicEx20,20→40,Color.Purple) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this}; 特效(1500,7,100ms,LibraryFile.MonMagicEx20,20→50,Globals.IceColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this}; 特效(1500,7,100ms,LibraryFile.MonMagicEx20,20→40,Color.Purple) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this}; 特效(1500,7,100ms,LibraryFile.MonMagicEx20,20→50,Globals.IceColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this} |
| MonasteryMon4 | Attack, RangeAttack |  | 特效(2600,7,100ms,LibraryFile.MonMagicEx23,20→40,Color.GreenYellow) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this}; 特效(2600,7,100ms,LibraryFile.MonMagicEx23,20→50,Color.GreenYellow) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this}; 特效(2600,7,100ms,LibraryFile.MonMagicEx23,20→40,Color.GreenYellow) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this}; 特效(2600,7,100ms,LibraryFile.MonMagicEx23,20→50,Color.GreenYellow) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this} |
| NewMob3 | Attack |  | 特效(2700,7,100ms,LibraryFile.MonMagicEx20,20→50,Globals.IceColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this}; 特效(2700,7,100ms,LibraryFile.MonMagicEx20,20→50,Globals.IceColour) {Blend=true, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), Target=this} |
| NewMob10 | Show |  | 特效(3100,18,100ms,LibraryFile.MonMagicEx20,20→90,Color.Purple) {Blend=true, Skip=0, Direction=action.Direction, Target=this}; 特效(3100,18,100ms,LibraryFile.MonMagicEx20,20→90,Color.Purple) {Blend=true, Skip=0, Direction=action.Direction, Target=this} |
| NewMob6 | Attack |  | 特效(2900,6,100ms,LibraryFile.MonMagicEx20,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this}; 特效(2900,6,100ms,LibraryFile.MonMagicEx20,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this} |
| NewMob8 | Show, Attack |  | 特效(3220,10,100ms,LibraryFile.MonMagicEx20,0→0,Globals.NoneColour) {Blend=true, Target=this}; 特效(3200,8,100ms,LibraryFile.MonMagicEx20,0→0,Globals.NoneColour) {Blend=true, Target=this}; 特效(3220,10,100ms,LibraryFile.MonMagicEx20,0→0,Globals.NoneColour) {Blend=true, Target=this}; 特效(3200,8,100ms,LibraryFile.MonMagicEx20,0→0,Globals.NoneColour) {Blend=true, Target=this} |
| NewMob4 |  |  |  |


---

# 第 6 章 NPC / 物品 / 法术对象与特效全表

## 6.1 NPCObject(NPCObject.cs)

| 项 | 值 |
|---|---|
| `Race` | ObjectType.NPC |
| 库 | `LibraryFile.NPC` |
| `BodyOffSet` | 100 |
| `BodyShape` | `NPCInfo.Image` |
| `BodyFrame` | `DrawFrame + BodyShape * BodyOffSet` |
| 阴影 | `Draw(BodyFrame, DrawX, DrawY, White, true, 0.5f, ImageType.Shadow)` 单层 |
| 悬停 | `mouseOver && Config.ShowTargetOutline` → `RenderingPipelineManager.EnableOutlineEffect(TargetNPCColour, 2f)` |
| DrawBlend | `SetBlend(true, 0.20F, BlendMode.HIGHLIGHT)` + DrawBody(mouseOver:true) |

### 图像→帧表 switch

| Image 值 | 帧表 |
|---|---|
| 64, 65, 91, 92, 93, 157, 158, 160, 165, 166, 168, 208, 209, 210, 211, 212, 213, 214, 231, 234 | 单帧 `Standing = Frame(0, 1, 0, 1h)` |
| 56, 57 | `Standing = Frame(0, 12, 0, 200ms)` |
| 156 | `Standing = Frame(0, 16, 0, 200ms)` |
| 其余 | `FrameSet.DefaultNPC`(Frame(0,4,0,1000ms),见第 3 章) |

### 任务图标(UpdateQuests,QuestIcon 库)

```
startIndex 按 QuestType:General=10 / Daily=70 / Weekly=70 / Repeatable=10 / Story=50 / Account=30
startIndex 按 QuestIcon:New += 0 / Incomplete = 0 / Complete += 2
QuestEffect = MirEffect(startIndex, 2, 500ms, QuestIcon, 0, 0, Color.Empty) { Loop, MapTarget=CurrentLocation,
               DrawType=Final, AdditionalOffSet=(0, -80) }
```

### 名称渲染

- `NameChanged()`:Name 含 `_` 时按 `_` 拆为 Title + Name;TitleNameLabel 用 NameColour,NameLabel 有 Title 时用 White。
- `NameColour = Color.Lime`(默认)。
- 静态字典 `NPCs`(NPCInfo → NPCObject)。

## 6.2 ItemObject(ItemObject.cs)

| 项 | 值 |
|---|---|
| `Race` | ObjectType.Item |
| 库 | `LibraryFile.Ground` |
| 帧表 | `FrameSet.DefaultItem`(Frame(0,1,0,1000ms)) |
| `LabelBackColour` | 30, 0, 24, 48 |
| 标题 | QuestItem → `Title = "(Quest)"`;ItemPart → 查 `Globals.ItemInfoList`,`Title = "[Part]"` |
| 绘制 | `BodyLibrary.Draw(drawIndex, DrawX + (CellWidth - size.Width)/2, DrawY + (CellHeight - size.Height)/2, DrawColour, false, 1F, ImageType.Image)`(网格居中) |
| `DrawFocus(layer)` | `DrawX + (48 - w)/2, DrawY - (32 - h/2) + 8 - layer*16`(焦点标签堆叠) |

### Rarity 着色与发光

| Rarity | 名称颜色 | 发光特效(ProgUse 库) |
|---|---|---|
| Common(无追加属性) | White | 无 |
| Common(有追加属性,非 ItemPart) | LightSkyBlue | `MirEffect(110, 10, 100ms, ProgUse, 60, 60, DeepSkyBlue) { Target, Loop, Blend, BlendRate=0.5F }` |
| Superior | PaleGreen | `MirEffect(100, 10, 100ms, ProgUse, 60, 60, PaleGreen) { Target, Loop, Blend, BlendRate=0.5F }` |
| Elite | MediumPurple | `MirEffect(120, 10, 100ms, ProgUse, 60, 60, MediumPurple) { Target, Loop, Blend, BlendRate=0.5F }` |

货币图标:`CEnvir.CurrencyImage(ItemInfo, count)` 查 `Globals.CurrencyInfoList`(货币类物品用专用图标)。

## 6.3 SpellObject(地面法术残留,SpellObject.cs)

`Race = Spell`、`Blocking = false`;帧表按 `SpellEffect` 在 `UpdateLibraries` 中建立;`Draw()` 按 `Blended` 走 `DrawBlend(DrawFrame, DrawX, DrawY, DrawColour, true, BlendRate, ImageType.Image)` 或普通 Draw。

| SpellEffect | 库 | Standing 帧 | 混合 | 颜色/光强/混合率 | 音效 |
|---|---|---|---|---|---|
| SafeZone | — | — | BlendRate=0.3f | — | — |
| FireWall | Magic | (920,5,150ms) | Y | FireColour, Light=15, Rate=0.55 | FireWallDuration(存在时) |
| Tempest | MagicEx2 | (920,10,150ms) | Y | WindColour, Light=15, Rate=0.55 | TempestDuration(存在时) |
| IceAura | MagicEx5 | (2600,10,150ms) | Y | IceColour, Light=15, Rate=0.55 | — |
| TrapOctagon | Magic | (640,10,100ms) | Y | Rate=0.8 | — |
| PoisonousCloud | MagicEx4 | (400,15,100ms) | Y | SaddleBrown, Light=0 | PoisonousCloudStart |
| DarkSoulPrison | MagicEx6 | (700,10,100ms) | Y | Light=0 | DarkSoulPrison |
| BurningFire | MagicEx6 | (1000,8,100ms) | Y | FireColour, Light=15, Rate=1F | — |
| MonsterDeathCloud | MonMagicEx2 | (850,10,100ms) | Y | Light=0, Rate=1F | JinchonDevilAttack3 |
| Rubble | ProgUse | Power>20→234 / >15→233 / >10→232 / >5→231 / else 230,1帧 100ms | — | Light=0 | MiningStruck |
| ZombieHole | ProgUse | (240 + (int)Direction, 1, 100ms) | N | Light=0 | — |

构造随机:FireWall/MonsterDeathCloud `FrameStart -= Random(750ms)`;Tempest `-= Random(1500ms)`。音效 Remove 时按 `ExistingEffects` 判断停止。

## 6.4 CreateMagicEffect 常驻/受击特效(MapObject.cs:5657,28 case)

| MagicEffect | 触发 | 特效(MirEffect 参数 + 属性) |
|---|---|---|
| WraithGrip | 毒:WraithGrip | `(1424,10,100ms,MagicEx4,40,40,None){Blend,Target,Loop,Rate=0.4}` + `(1444,...)` 同 |
| MagicShield | Buff | `(850,3,200ms,Magic,40,40,WindColour){Blend,Target,Loop}`;若 Struck 态已存在则复用 |
| MagicShieldStruck | 受击 | `(853,3,100ms,Magic,40,40,WindColour){Blend,Target}` + CompleteAction 重建 MagicShield |
| SuperiorMagicShield | Buff | `(1920,3,200ms,MagicEx2,40,40,FireColour){Blend,Target,Loop}` |
| SuperiorMagicShieldStruck | 受击 | `(1923,3,100ms,MagicEx2,40,40,FireColour){Blend,Target}` + 重建 |
| CelestialLight | Buff | `(300,3,200ms,MagicEx2,40,40,HolyColour){Blend,Target,Loop}` |
| CelestialLightStruck | 受击 | `(303,3,100ms,MagicEx2,40,40,HolyColour){Blend,Target}` + 重建 |
| Assault | 技能 | `(740,3,100ms,MagicEx2,40,40,None){Blend,Target,Loop,Direction}` |
| ElementalSwords | Buff | 每剑 `(200+i*20,16,100ms,MagicEx10,0,0,None){Blend,Target,AddOffSet(0,-50)}`(仅 swordCount==5 时)+ 循环单帧 `(200+i*20,1,100ms,...){Loop,StartTime=+16ms}`;音效 ElementalSwordsStart |
| DefensiveBlow | Buff | `(880,6,100ms,MagicEx7,40,40,None){Blend,Target,StartTime+300ms}` + `(886,1,100ms){Loop,StartTime+800ms}` |
| ReflectDamage | Buff | `(1240,3,100ms,MagicEx2,40,40,None){Blend,Target,Loop}` |
| LifeSteal | Buff | `(1260,6,150ms,MagicEx2,40,40,DarkColour){Blend,Target,Loop}`(TODO 注释:图错误) |
| FrostBite | Buff | `(600,7,150ms,MagicEx5,40,40,IceColour){Blend,Target,Loop}` |
| Silence | 毒:Silenced | `(680,6,150ms,ProgUse,0,0,None){Blend,Target,Loop}` |
| Blind | 毒:Abyss | `(680,6,150ms,ProgUse,0,0,None){Target,Loop,DrawColour=Black,Opacity=0.8}`;玩家另加 `(2100,19,100ms,MagicEx4){Blend,Loop,AddOffSet(0,-64)}` |
| Fear | 毒:Fear | `(700,15,100ms,ProgUse,0,0,None){Blend,Target,Loop}`(TODO:图错误) |
| Parasite | 毒:Parasite | `(900,7,100ms,MagicEx5,0,0,None){Blend,Target,Loop,Opacity=0.8}` |
| Neutralize | 毒:Neutralize | `(470,6,120ms,MagicEx7,0,0,None){Blend,Target,Loop,Opacity=0.8}` |
| DragonRepulse | Buff/技能 | 光环 `(1011,4,150ms,MagicEx4){Target,Loop,Floor}` + `(1031,4,150ms,MagicEx4,80,80,LightningColour){Blend,Floor}`;FrameIndex==0 时随机(1/7)在 2x1 网格生成 `(1050,7,100ms,MagicEx4){MapTarget,Final,延迟≤300ms}` + `(1060,7){Blend,Final}` |
| Containment | 毒 | `(2040,10,100ms,MagicEx2,40,40,None){Blend,Target,Loop}` |
| Chain | 毒:Chain | `(27,4,100ms,MagicEx7,40,40,None){Blend,Target,Loop}` + 接管所有指向本对象的 MirLineEffect(`SetOwner`) |
| Hemorrhage | 毒 | `(1290,1,100ms,MagicEx7,40,40,None){Blend,Target,Loop}` |
| ElementalHurricane | Buff | `(370,4,140ms,MagicEx3,0,0,LightningColour){Blend,Target,Direction,DrawColour=AttackElement色,Loop}`;FrameIndex==1 时音效 ElementalHurricane |
| Burn | 毒:Burn/HellFire | `(790,6,100ms,MagicEx,10,30,FireColour){Blend,Target,Loop}` |
| HundredFist | 技能 | `(2100,5,200ms,MagicEx5,35,35,FireColour){Blend,Target,Loop,Direction}` |
| Binding | 毒:Binding | `(3100,14,100ms,MagicEx5,40,40,None){Blend,Target,Loop}` |
| Ranking | Buff | `(3420,7,150ms,GameInter,0,0,None){Blend,Target,Loop,AddOffSet(0,-25)}` |
| Developer | Buff | `(3410,7,150ms,GameInter,0,0,None){Blend,Target,Loop,AddOffSet(10,-25)}` |

**触发链**(UpdateEffects):WraithGrip/Silence/Fear/Blind/Parasite/Burn/Containment/Chain/Hemorrhage/Neutralize 按毒;MagicShield/SuperiorMagicShield/Developer/Ranking/ReflectDamage/LifeSteal/CelestialLight/FrostBite/ElementalSwords/DefensiveBlow 按 Buff;Binding 按毒。Developer 优先于 Ranking。

## 6.5 网络包特效映射(CConnection.cs)

### ObjectEffect(对象附加特效)

| Effect | 特效 |
|---|---|
| TeleportOut | `(110,10,100ms,Magic,30,60,White){MapTarget,Blend,Reversed,Rate=0.6}` + TeleportOut |
| TeleportIn | `(110,10,100ms,Magic,30,60,White){Target,Blend,Rate=0.6}` + TeleportIn(玩家另播地图定位动画) |
| ThunderBolt | `(1450,3,150ms,Magic,150,50,LightningColour){Blend,Target}` + LightningStrikeEnd |
| FullBloom | `(1700,4,100ms,MagicEx4,30,60,White){Target,Blend,Rate=0.6}` + FullBloom |
| WhiteLotus | `(1600,12,100ms,MagicEx4,30,60,White){Target,Blend,Rate=0.6}` + WhiteLotus |
| RedLotus | `(1700,12,100ms,MagicEx4,30,60,White){Target,Blend,Rate=0.6}` + RedLotus |
| SweetBrier | `(1900,10,100ms,MagicEx4,30,60,White){Target,Blend,Rate=0.6}` + SweetBrier |
| Karma | `(1800,10,100ms,MagicEx4,30,60,White){Target,Blend,Rate=0.6}` + Karma |
| Puppet | `(820,8,100ms,MagicEx4,30,60,FireColour){Target,Blend,Rate=0.6}` |
| PuppetFire | `(1546,8,100ms,MagicEx4,30,60,FireColour){Target,Blend,Rate=0.6}` |
| PuppetIce | `(2700,10,100ms,MagicEx4,30,60,IceColour){Target,Blend,Rate=0.6}` |
| PuppetLightning | `(2800,10,100ms,MagicEx4,30,60,LightningColour){Target,Blend,Rate=0.6}` |
| PuppetWind | `(2900,10,100ms,MagicEx4,30,60,WindColour){Target,Blend,Rate=0.6}` |
| DanceOfSwallow | `(1300,8,100ms,MagicEx4,20,70,None){Blend,Target}` + DanceOfSwallowsEnd |
| FlashOfLight | `(2400,5,100ms,MagicEx4,20,70,None){Blend,Target}` + FlashOfLightEnd |
| DemonExplosion | `(3300,10,100ms,MonMagicEx8,30,60,PhantomColour){Target,Blend,Rate=0.6}` |
| ParasiteExplode | `(700,7,100ms,MagicEx5,30,60,None){Target,Blend}` + ParasiteExplode |
| FrostBiteEnd | `(700,7,100ms,MagicEx5,30,60,IceColour){Target,Blend,Rate=0.6}` + FireStormEnd |
| ChainOfFireExplode | `(600,12,100ms,MagicEx10,30,60,FireColour){Target,Blend}`;FrameIndex==8 时音效 ChainofFireExplode |
| MirrorImage | `(1280,10,100ms,MagicEx2,30,60,None){MapTarget,Blend}` + SummonSkeletonEnd |
| 其他 | `throw new ArgumentOutOfRangeException()` |

### ObjectProjectile(服务器同步投射物)

| MagicType | 特效 |
|---|---|
| ChainLightning | 命中数>0 时 ChainLightningEnd(客户端不发弹体) |
| LightningStrike | 每目标 `MirProjectile(500,8,100ms,MagicEx6,35,35,LightningColour,CurrentLocation){Blend,Target,Skip=0}` + LightningBeamEnd |
| FireBounce | 每目标 `MirProjectile(1640,6,100ms,Magic,35,35,FireColour,CurrentLocation,FireballTrail){Blend,Target}`;命中 → `MirEffect(1800,10,100ms,Magic,10,35,FireColour){Blend,Target}` + GreaterFireBallEnd;发射时 GreaterFireBallTravel |
| ElementalSwords | `MirEffect(300,5,100ms,MagicEx10,0,0,None){MapTarget,Skip=10,Direction,Blend}`;CompleteAction → `MirProjectile(0,3,100ms,MagicEx10,0,0,None,CurrentLocation){Blend,Target,Has16Directions}` + ElementalSwordsEnd |

### MapEffect(地图格特效)

| Effect | 特效 |
|---|---|
| SummonSkeleton | `(750,10,100ms,Magic,30,60,PhantomColour){MapTarget,Blend}` + SummonSkeletonEnd |
| SummonShinsu | `(9640,10,100ms,Mon_9,30,60,PhantomColour){MapTarget,Direction}` + SummonShinsuEnd |
| CursedDoll | `(700,13,100ms,MagicEx3,30,60,None){MapTarget,Blend}` + CursedDollEnd |
| UndeadSoul | `(3300,10,100ms,MonMagicEx20,35,10,None){MapTarget,Blend}` + `(400,13,100ms,MagicEx10,35,10,None){MapTarget,Blend}` + SummonDeadEnd |
| BurningFireExplode | `(1100,10,100ms,MagicEx6,30,60,FireColour){MapTarget,Blend}` + FireStormEnd |
| FireWallSmoke | `(220,1,3500ms,ProgUse,0,0,None){MapTarget,Opacity=0.8,Floor}` + `(2450+Random*10,10,250ms,Magic,0,0,None){Blend,MapTarget,Floor}` |
| HundredFist | `(2100,5,100ms,MagicEx5,0,0,None){Blend,MapTarget,Direction,Skip=10}` + HundredFist |
| HundredFistStruck | `(2200,6,150ms,MagicEx5,0,0,None){Blend,MapTarget,Direction,Skip=10}` |
| IceAuraEnd | `(2700,11,100ms,MagicEx5,0,0,None){Blend,MapTarget}` + GreaterIceBoltEnd |
| 其他 | `throw new ArgumentOutOfRangeException()` |

### ObjectLeveled / ObjectRevive

| 包 | 特效 |
|---|---|
| ObjectLeveled | `(2030,16,100ms,MagicEx,50,120,DeepSkyBlue){Blend,DrawColour=RosyBrown,Target}` |
| ObjectRevive | `p.Effect` 时 `(1110,25,100ms,MagicEx3,50,90,White){Blend,Target}`;并刷新 FLayer |

## 6.6 Attack / Struck 元素特效

### Attack(SetAction MirAction.Attack,按 MagicType)

| MagicType | 特效 + 音效 |
|---|---|
| None | 仅 玩家 Combat3 且元素非 None:`(1090,6,100ms,MagicEx,10,50,attackColour){Blend,Target,Direction,DrawColour=attackColour}` |
| Slaying | `(1350,6,100ms,Magic,10,50,attackColour){Blend,Target,Direction,DrawColour}` + SlayingMale/SlayingFemale(按性别) |
| Thrusting | `(0,6,100ms,MagicEx3,20,70,attackColour){Blend,Target,Direction,DrawColour}` + EnergyBlast |
| HalfMoon | `(230,6,100ms,Magic,20,70,attackColour){Blend,Target,Direction,DrawColour}` + HalfMoon |
| DestructiveSurge | `(1420,6,100ms,MagicEx2,20,70,attackColour){Blend,Target,DrawColour}` + DestructiveSurge |
| FlamingSword | `(1470,6,100ms,Magic,10,50,FireColour){Blend,Target,Direction}` + FlamingSword |
| DragonRise | `(2185,10,100ms,Magic,20,70,attackColour){Blend,Target,Direction,DrawColour,StartTime+200ms}` + DragonRise |
| BladeStorm | `(1780,10,60ms,MagicEx,20,70,attackColour){Blend,Target,Direction,DrawColour}` + BladeStorm |
| DefensiveBlow | `(800,9,100ms,MagicEx7,10,50,FireColour){Blend,Target,Direction,DrawColour,StartTime+200ms,Floor}` + DefensiveBlow |
| FlameSplash | `(900,8,100ms,MagicEx4,20,70,FireColour){Blend,Target}` + BladeStorm |
| DragonBlood | `(200,7,100ms,MagicEx5,30,60,None){Target,Direction,Blend}` |
| WaningMoon | 仅音效 WaningMoon |
| CalamityOfFullMoon | 仅音效 CalamityOfFullMoon |

### Struck(受击,按元素;Struck() 方法)

| Element | 特效(MagicEx 库,6 帧 100ms,光强 10→30) |
|---|---|
| None | (930,...) |
| Fire | (790,...) |
| Ice | (810,...) |
| Lightning | (830,...) |
| Wind | (850,...) |
| Holy | (870,...) |
| Dark | (890,...) |
| Phantom | (910,...) |

全部 `{Blend = true, Target = this}`,颜色 = `Functions.GetElementColour(element)`。受击同时触发 MagicShieldStruck / SuperiorMagicShieldStruck / CelestialLightStruck(按对应 Buff)。

## 6.7 SetAction 魔法特效总表(153 魔法 / 210 上下文 / 266 特效 / 191 音效引用)

`SetAction` 内 `switch(CurrentAction)` → `case MirAction.Spell`(施放)与攻击命中路径两组;同名魔法两行时第一行为施放(Spell),第二行为命中/结算(Attack 路径),顺序与源码一致。重复 case 不合并。列含义:`特效/投射(帧号,帧数,延时ms,库,起始光强→结束光强,颜色)` + `{对象初始化器关键属性}`。

| 魔法 | 上下文 | 特效(帧号,帧数,延时,库,光强起→止,颜色) + 属性 | 音效 |
|---|---|---|---|
| SwiftBlade | 施放 | 特效(2330,16,100ms,LibraryFile.MagicEx2,10→35,Globals.NoneColour) {Blend=true, MapTarget=point, Target=point} | SwiftBladeEnd |
| TaecheonSword | 施放 | 特效(5000,31,100ms,LibraryFile.MagicEx5,0→50,Globals.FireColour) {Blend=true, MapTarget=point, Target=point} | TaecheonSword |
| FireSword | 施放 | 特效(5100,39,100ms,LibraryFile.MagicEx5,10→35,Globals.FireColour) {Blend=true, MapTarget=point, Target=point} | FireSword |
| FireBall | 施放 | 投射(420,5,100ms,LibraryFile.Magic,35→35,Globals.FireColour,origin=CurrentLocation,粒子=FireballTrail) {Blend=true, MapTarget=point, Target=point}; 投射(420,5,100ms,LibraryFile.Magic,35→35,Globals.FireColour,origin=CurrentLocation,粒子=FireballTrail) {Blend=true, Target=attackTarget}; 特效(580,10,100ms,LibraryFile.Magic,10→35,Globals.FireColour) {Blend=true, Target=attackTarget} | FireBallEnd, FireBallTravel |
| FireBall | 命中/结算 | 特效(1820,8,70ms,LibraryFile.Magic,10→35,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this} | FireBallStart |
| LightningBall | 施放 | 投射(3070,6,100ms,LibraryFile.Magic,35→35,Globals.LightningColour,origin=CurrentLocation) {Blend=true, MapTarget=point, Target=point}; 投射(3070,6,100ms,LibraryFile.Magic,35→35,Globals.LightningColour,origin=CurrentLocation) {Blend=true, Target=attackTarget}; 特效(3230,10,100ms,LibraryFile.Magic,10→35,Globals.LightningColour) {Blend=true, Target=attackTarget} | ThunderBoltEnd, ThunderBoltTravel |
| LightningBall | 命中/结算 | 特效(2990,6,80ms,LibraryFile.Magic,10→35,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this} | ThunderBoltStart |
| IceBolt | 施放 | 投射(2700,3,100ms,LibraryFile.Magic,35→35,Globals.IceColour,origin=CurrentLocation,粒子=IceBoltTrail) {Blend=true, MapTarget=point, Target=point}; 投射(2700,3,100ms,LibraryFile.Magic,35→35,Globals.IceColour,origin=CurrentLocation,粒子=IceBoltTrail) {Blend=true, Target=attackTarget}; 特效(2860,10,100ms,LibraryFile.Magic,10→35,Globals.IceColour) {Blend=true, Target=attackTarget} | IceBoltEnd, IceBoltTravel |
| IceBolt | 命中/结算 | - | - |
| GustBlast | 施放 | 投射(430,5,100ms,LibraryFile.MagicEx,35→35,Globals.WindColour,origin=CurrentLocation,粒子=GustTrail) {Blend=true, MapTarget=point, Target=point}; 投射(430,5,100ms,LibraryFile.MagicEx,35→35,Globals.WindColour,origin=CurrentLocation,粒子=GustTrail) {Blend=true, Target=attackTarget}; 特效(590,10,100ms,LibraryFile.MagicEx,10→35,Globals.WindColour) {Blend=true, Target=attackTarget} | GustBlastEnd, GustBlastTravel |
| GustBlast | 命中/结算 | 特效(350,7,70ms,LibraryFile.MagicEx,10→35,Globals.WindColour) {Blend=true, Direction=action.Direction, Target=this} | GustBlastStart |
| ElectricShock | 施放 | 特效(10,10,100ms,LibraryFile.Magic,10→35,Globals.LightningColour) {Blend=true, MapTarget=point, Target=point}; 特效(10,10,100ms,LibraryFile.Magic,10→35,Globals.LightningColour) {Blend=true, Target=attackTarget} | ElectricShockEnd |
| ElectricShock | 命中/结算 | 特效(0,10,60ms,LibraryFile.Magic,10→35,Globals.LightningColour) {Blend=true, Target=this} | ElectricShockStart |
| AdamantineFireBall | 施放 | - | - |
| AdamantineFireBall | 命中/结算 | - | - |
| MeteorShower | 施放 | - | - |
| MeteorShower | 命中/结算 | - | - |
| FireBounce | 施放 | 投射(1640,6,100ms,LibraryFile.Magic,35→35,Globals.FireColour,origin=CurrentLocation,粒子=FireballTrail) {Blend=true, MapTarget=point, Target=point}; 投射(1640,6,100ms,LibraryFile.Magic,35→35,Globals.FireColour,origin=CurrentLocation,粒子=FireballTrail) {Blend=true, Target=attackTarget}; 特效(1800,10,100ms,LibraryFile.Magic,10→35,Globals.FireColour) {Blend=true, Target=attackTarget} | GreaterFireBallEnd, GreaterFireBallTravel |
| FireBounce | 命中/结算 | 特效(1560,9,65ms,LibraryFile.Magic,10→35,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this} | GreaterFireBallStart |
| ThunderBolt | 施放 | - | - |
| ThunderBolt | 命中/结算 | 特效(1430,12,50ms,LibraryFile.Magic,10→35,Globals.LightningColour) {Blend=true, Target=this} | LightningStrikeStart |
| ThunderStrike | 施放 | 特效(1450,3,150ms,LibraryFile.Magic,150→50,Globals.LightningColour) {Blend=true, MapTarget=point, Target=point}; 特效(1450,3,150ms,LibraryFile.Magic,150→50,Globals.LightningColour) {Blend=true, Target=attackTarget} | LightningStrikeEnd |
| ThunderStrike | 命中/结算 | - | LightningStrikeStart |
| IceBlades | 施放 | 投射(2960,6,50ms,LibraryFile.Magic,35→35,Globals.IceColour,origin=CurrentLocation,粒子=IceBladesTrail) {Blend=true, Skip=0, BlendRate=1F, MapTarget=point, Target=point}; 投射(2960,6,50ms,LibraryFile.Magic,35→35,Globals.IceColour,origin=CurrentLocation,粒子=IceBladesTrail) {Blend=true, Skip=0, BlendRate=1F, Target=attackTarget}; 特效(2970,10,100ms,LibraryFile.Magic,10→35,Globals.IceColour) {Blend=true, Target=attackTarget} | GreaterIceBoltEnd, GreaterIceBoltTravel |
| IceBlades | 命中/结算 | 特效(2880,6,115ms,LibraryFile.Magic,10→35,Globals.IceColour) {Blend=true, Direction=action.Direction, Target=this} | GreaterIceBoltStart |
| Cyclone | 施放 | 特效(1990,5,100ms,LibraryFile.MagicEx,50→80,Globals.WindColour) {Blend=true, MapTarget=point, Target=point}; 特效(2000,8,100ms,LibraryFile.MagicEx,50→80,Globals.WindColour) {Blend=true, MapTarget=point, Target=point}; 特效(1990,5,100ms,LibraryFile.MagicEx,50→80,Globals.WindColour) {Blend=true, Target=attackTarget}; 特效(2000,8,100ms,LibraryFile.MagicEx,50→80,Globals.WindColour) {Blend=true, Target=attackTarget} | CycloneEnd |
| Cyclone | 命中/结算 | 特效(1970,10,60ms,LibraryFile.MagicEx,10→35,Globals.WindColour) {Blend=true, Target=this} | CycloneStart |
| ScortchedEarth | 施放 | 特效(220,1,3500ms,LibraryFile.ProgUse,0→0,Globals.NoneColour) {DrawType=DrawType.Floor, Opacity=0.8F, StartTime=CEnvir.Now.AddMilliseconds(500 + Functions.Distance(point, MapTarget=point, Target=point}; 特效(2450 + CEnvir.Random.Next(5) * 10,10,250ms,LibraryFile.Magic,0→0,Globals.NoneColour) {Blend=true, DrawType=DrawType.Floor, StartTime=CEnvir.Now.AddMilliseconds(500 + Functions.Distance(point, MapTarget=point, Target=point}; 特效(1900,30,50ms,LibraryFile.Magic,20→70,Globals.FireColour) {Blend=true, BlendRate=1F, StartTime=CEnvir.Now.AddMilliseconds(Functions.Distance(point, MapTarget=point, Target=point} | - |
| ScortchedEarth | 命中/结算 | 特效(1820,8,60ms,LibraryFile.Magic,10→35,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this} | LavaStrikeStart |
| LightningBeam | 施放 | 特效(1180,4,100ms,LibraryFile.MagicEx,150→150,Globals.LightningColour) {Blend=true, Direction=Functions.DirectionFromPoint(CurrentLocation, Target=this} | LightningBeamEnd |
| LightningBeam | 命中/结算 | 特效(1970,10,30ms,LibraryFile.Magic,10→35,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this} | ThunderBoltStart |
| FrozenEarth | 施放 | 特效(90,20,50ms,LibraryFile.MagicEx,20→70,Globals.IceColour) {Blend=true, Opacity=0.5F, StartTime=CEnvir.Now.AddMilliseconds(Functions.Distance(point, MapTarget=point, Target=point}; 特效(260,1,2500ms,LibraryFile.ProgUse,0→0,Globals.IceColour) {DrawType=DrawType.Floor, Opacity=0.8F, MapTarget=point, Target=point} | FrozenEarthEnd |
| FrozenEarth | 命中/结算 | 特效(0,10,50ms,LibraryFile.MagicEx,10→35,Globals.IceColour) {Blend=true, Direction=action.Direction, Target=this} | FrozenEarthStart |
| BlowEarth | 施放 | 投射(1990,5,100ms,LibraryFile.MagicEx,50→80,Globals.WindColour,origin=CurrentLocation) {Blend=true, Skip=0, Explode=true, MapTarget=finalPoint, Target=finalPoint}; 特效(2000,8,100ms,LibraryFile.MagicEx,50→80,Globals.WindColour) {Blend=true, MapTarget=finalPoint, Target=finalPoint} | BlowEarthEnd, BlowEarthTravel |
| BlowEarth | 命中/结算 | 特效(1970,10,60ms,LibraryFile.MagicEx,10→35,Globals.WindColour) {Blend=true, Target=this} | BlowEarthStart |
| ExpelUndead | 施放 | 特效(140,10,100ms,LibraryFile.Magic,50→80,Globals.PhantomColour) {Blend=true, Target=attackTarget} | ExpelUndeadEnd |
| ExpelUndead | 命中/结算 | 特效(130,10,60ms,LibraryFile.Magic,10→35,Globals.PhantomColour) {Blend=true, Target=this} | ExpelUndeadStart |
| FireStorm | 施放 | 特效(950,7,100ms,LibraryFile.Magic,10→35,Globals.FireColour) {Blend=true, MapTarget=point, Target=point} | FireStormEnd |
| FireStorm | 命中/结算 | 特效(940,10,60ms,LibraryFile.Magic,10→35,Globals.FireColour) {Blend=true, Target=this} | FireStormStart |
| LightningWave | 施放 | 特效(980,8,100ms,LibraryFile.MagicEx,50→80,Globals.LightningColour) {Blend=true, MapTarget=point, Target=point} | LightningWaveEnd |
| LightningWave | 命中/结算 | 特效(1430,12,50ms,LibraryFile.Magic,10→35,Globals.LightningColour) {Blend=true, Target=this} | LightningWaveStart |
| IceStorm | 施放 | 特效(780,7,100ms,LibraryFile.Magic,10→35,Globals.IceColour) {Blend=true, MapTarget=point, Target=point} | IceStormEnd |
| IceStorm | 命中/结算 | 特效(770,10,60ms,LibraryFile.Magic,10→35,Globals.IceColour) {Blend=true, Target=this} | IceStormStart |
| DragonTornado | 施放 | 特效(1040,16,100ms,LibraryFile.MagicEx,10→35,Globals.WindColour) {Blend=true, MapTarget=point, Target=point} | DragonTornadoEnd |
| DragonTornado | 命中/结算 | 特效(1030,10,60ms,LibraryFile.MagicEx,10→35,Globals.WindColour) {Blend=true, Target=this} | DragonTornadoStart |
| GreaterFrozenEarth | 施放 | 特效(90,20,50ms,LibraryFile.MagicEx,20→70,Globals.IceColour) {Blend=true, Opacity=0.5F, StartTime=CEnvir.Now.AddMilliseconds(Functions.Distance(point, MapTarget=point, Target=point}; 特效(260,1,2500ms,LibraryFile.ProgUse,0→0,Globals.NoneColour) {DrawType=DrawType.Floor, Opacity=0.8F, MapTarget=point, Target=point} | GreaterFrozenEarthEnd |
| GreaterFrozenEarth | 命中/结算 | 特效(0,10,50ms,LibraryFile.MagicEx,10→35,Globals.IceColour) {Blend=true, Direction=action.Direction, Target=this} | GreaterFrozenEarthStart |
| ChainLightning | 施放 | 特效(470,10,100ms,LibraryFile.MagicEx2,50→80,Globals.LightningColour) {Blend=true, MapTarget=point, Target=point} | ChainLightningEnd |
| ChainLightning | 命中/结算 | 特效(1430,12,50ms,LibraryFile.Magic,10→35,Globals.LightningColour) {Blend=true, Target=this} | ChainLightningStart |
| Asteroid | 施放 | 投射(1300,10,100ms,LibraryFile.MagicEx5,50→80,Globals.FireColour,origin=p) {Blend=true, Skip=0, Explode=true, MapTarget=point, Target=point}; 特效(1320,8,100ms,LibraryFile.MagicEx5,100→100,Globals.NoneColour) {Blend=true, MapTarget=eff.MapTarget, Target=eff.MapTarget} | - |
| LightningStrike | 施放 | 投射(500,8,100ms,LibraryFile.MagicEx6,50→50,Globals.LightningColour,origin=CurrentLocation) {Blend=true, Skip=0, MapTarget=point, Target=point}; 投射(500,8,100ms,LibraryFile.MagicEx6,50→50,Globals.LightningColour,origin=CurrentLocation) {Blend=true, Skip=0, Target=attackTarget}; 特效(500,8,100ms,LibraryFile.MagicEx6,10→35,Globals.LightningColour) {Blend=true, Target=attackTarget} | LightningBeamEnd, LightningBeamEnd |
| LightningStrike | 命中/结算 | 特效(400,8,100ms,LibraryFile.MagicEx6,10→35,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this} | ChainLightningStart |
| IceRain | 施放 | 投射(700,7,100ms,LibraryFile.MagicEx7,60→60,Globals.IceColour,origin=p) {Blend=true, Skip=0, Explode=true, StartTime=CEnvir.Now.AddMilliseconds(delay), MapTarget=point, Target=point}; 特效(720,7,100ms,LibraryFile.MagicEx7,100→100,Globals.IceColour) {Blend=true, MapTarget=point, Target=point} | IceBoltEnd |
| IceRain | 命中/结算 | 特效(1430,12,50ms,LibraryFile.Magic,10→35,Globals.IceColour) {Blend=true, Target=this} | LightningStrikeStart |
| IceAura | 施放 | 投射(2500,6,100ms,LibraryFile.MagicEx5,35→35,Globals.IceColour,origin=CurrentLocation) {Blend=true, Has16Directions=false, MapTarget=point, Target=point} | IceAuraTravel |
| IceAura | 命中/结算 | - | - |
| IceDragon | 施放 | 投射(2800,6,100ms,LibraryFile.MagicEx5,35→35,Globals.IceColour,origin=CurrentLocation) {Blend=true, Skip=10, Direction=Direction, Has16Directions=false, MapTarget=point, Target=point}; 投射(2900,6,100ms,LibraryFile.MagicEx5,35→35,Globals.IceColour,origin=CurrentLocation) {Blend=true, Skip=10, Direction=Direction, Has16Directions=false, MapTarget=point, Target=point}; 投射(2800,6,150ms,LibraryFile.MagicEx5,35→35,Globals.IceColour,origin=CurrentLocation) {Blend=true, Skip=10, Has16Directions=false, Target=attackTarget}; 投射(2900,6,150ms,LibraryFile.MagicEx5,35→35,Globals.IceColour,origin=CurrentLocation) {Blend=true, Skip=10, Direction=Direction, Has16Directions=false, Target=attackTarget}; 特效(3000,12,100ms,LibraryFile.MagicEx5,10→35,Globals.IceColour) {Blend=true, Target=attackTarget} | IceDragonBreak, IceDragonTravel |
| IceDragon | 命中/结算 | 特效(2620,6,80ms,LibraryFile.Magic,10→35,Globals.IceColour) {Blend=true, Direction=action.Direction, Target=this} | IceBoltStart |
| IceBreaker | 施放 | 特效(5200,37,100ms,LibraryFile.MagicEx5,10→35,Globals.IceColour) {Blend=true, MapTarget=point, Target=point} | IceBreaker |
| FrozenDragon | 施放 | 特效(5300,41,100ms,LibraryFile.MagicEx5,10→35,Globals.IceColour) {Blend=true, MapTarget=point, Target=point} | FrozenDragon |
| Heal | 施放 | 特效(610,10,100ms,LibraryFile.Magic,10→35,Globals.HolyColour) {Blend=true, Target=attackTarget} | HealEnd |
| Heal | 命中/结算 | 特效(660,10,60ms,LibraryFile.Magic,10→35,Globals.HolyColour) {Blend=true, Target=this} | HealStart |
| PoisonDust | 施放 | - | - |
| PoisonDust | 命中/结算 | 特效(60,10,60ms,LibraryFile.Magic,10→35,Globals.DarkColour) {Blend=true, Target=this} | PoisonDustStart |
| AugmentPoisonDust | 施放 | 特效(70,10,100ms,LibraryFile.Magic,10→35,Globals.DarkColour) {Blend=true, Target=attackTarget} | PoisonDustEnd |
| ExplosiveTalisman | 施放 | 投射(980,3,100ms,LibraryFile.Magic,35→35,Globals.DarkColour,origin=CurrentLocation) {Blend=true, MapTarget=point, Target=point}; 投射(980,3,100ms,LibraryFile.Magic,35→35,Globals.DarkColour,origin=CurrentLocation) {Blend=true, Target=attackTarget}; 特效(1140,10,100ms,LibraryFile.Magic,20→50,Globals.DarkColour) {Blend=true, Target=attackTarget} | ExplosiveTalismanEnd, ExplosiveTalismanTravel |
| ExplosiveTalisman | 命中/结算 | 特效(2080,6,80ms,LibraryFile.Magic,10→35,Globals.DarkColour) {Blend=true, Direction=action.Direction, Target=this} | ExplosiveTalismanStart |
| EvilSlayer | 施放 | 投射(3330,6,100ms,LibraryFile.Magic,35→35,Globals.HolyColour,origin=CurrentLocation) {Blend=true, Skip=0, MapTarget=point, Target=point}; 投射(3330,6,100ms,LibraryFile.Magic,35→35,Globals.HolyColour,origin=CurrentLocation) {Blend=true, Skip=0, Target=attackTarget}; 特效(3340,10,100ms,LibraryFile.Magic,20→50,Globals.HolyColour) {Blend=true, Target=attackTarget} | HolyStrikeEnd, HolyStrikeTravel |
| EvilSlayer | 命中/结算 | 特效(3250,6,80ms,LibraryFile.Magic,10→35,Globals.HolyColour) {Blend=true, Direction=action.Direction, Target=this} | HolyStrikeStart |
| MagicResistance | 施放 | 投射(980,3,100ms,LibraryFile.Magic,35→35,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Explode=true, MapTarget=point, Target=point}; 特效(200,8,100ms,LibraryFile.Magic,20→80,Globals.NoneColour) {Blend=true, MapTarget=point, Target=point} | MagicResistanceEnd, MagicResistanceTravel |
| MagicResistance | 命中/结算 | 特效(2080,6,80ms,LibraryFile.Magic,10→35,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this} | - |
| MassInvisibility | 施放 | 投射(980,3,100ms,LibraryFile.Magic,35→35,Globals.PhantomColour,origin=CurrentLocation) {Blend=true, Explode=true, MapTarget=point, Target=point}; 特效(820,7,100ms,LibraryFile.Magic,20→80,Globals.PhantomColour) {Blend=true, MapTarget=point, Target=point} | MassInvisibilityEnd, MassInvisibilityTravel |
| MassInvisibility | 命中/结算 | 特效(2080,6,80ms,LibraryFile.Magic,10→35,Globals.PhantomColour) {Blend=true, Direction=action.Direction, Target=this} | - |
| GreaterEvilSlayer | 施放 | 投射(3440,6,50ms,LibraryFile.Magic,35→35,Globals.HolyColour,origin=CurrentLocation) {Blend=true, Skip=0, MapTarget=point, Target=point}; 投射(3440,6,50ms,LibraryFile.Magic,35→35,Globals.HolyColour,origin=CurrentLocation) {Blend=true, Skip=0, Target=attackTarget}; 特效(3450,10,100ms,LibraryFile.Magic,20→50,Globals.HolyColour) {Blend=true, Target=attackTarget} | ImprovedHolyStrikeEnd, ImprovedHolyStrikeTravel |
| GreaterEvilSlayer | 命中/结算 | 特效(3360,6,80ms,LibraryFile.Magic,10→35,Globals.HolyColour) {Blend=true, Direction=action.Direction, Target=this} | ImprovedHolyStrikeStart |
| Resilience | 施放 | 投射(980,3,100ms,LibraryFile.Magic,35→35,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Explode=true, MapTarget=point, Target=point}; 特效(170,8,100ms,LibraryFile.Magic,20→80,Globals.NoneColour) {Blend=true, MapTarget=point, Target=point} | ResilienceEnd, ResilienceTravel |
| Resilience | 命中/结算 | 特效(2080,6,80ms,LibraryFile.Magic,10→35,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this} | - |
| TrapOctagon | 施放 | - | ShacklingTalismanEnd |
| TrapOctagon | 命中/结算 | 特效(630,10,60ms,LibraryFile.Magic,10→35,Globals.DarkColour) {Blend=true, Target=this} | ShacklingTalismanStart |
| ElementalSuperiority | 施放 | 投射(980,3,100ms,LibraryFile.Magic,35→35,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Explode=true, MapTarget=point, Target=point}; 特效(1870,10,100ms,LibraryFile.MagicEx,20→80,Globals.NoneColour) {Blend=true, MapTarget=point, Target=point} | BloodLustEnd, BloodLustTravel |
| ElementalSuperiority | 命中/结算 | 特效(2080,6,80ms,LibraryFile.Magic,10→35,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this} | - |
| MassHeal | 施放 | 特效(670,7,100ms,LibraryFile.Magic,40→60,Globals.HolyColour) {Blend=true, MapTarget=point, Target=point} | MassHealEnd |
| MassHeal | 命中/结算 | 特效(660,10,60ms,LibraryFile.Magic,10→35,Globals.HolyColour) {Blend=true, Target=this} | MassHealStart |
| BloodLust | 施放 | 投射(980,3,100ms,LibraryFile.Magic,35→35,Globals.DarkColour,origin=CurrentLocation) {Blend=true, Explode=true, MapTarget=point, Target=point}; 特效(140,7,100ms,LibraryFile.MagicEx,20→80,Globals.DarkColour) {Blend=true, MapTarget=point, Target=point} | BloodLustEnd, BloodLustTravel |
| BloodLust | 命中/结算 | 特效(2080,6,80ms,LibraryFile.Magic,10→35,Globals.DarkColour) {Blend=true, Direction=action.Direction, Target=this} | - |
| Resurrection | 施放 | 特效(320,7,100ms,LibraryFile.MagicEx,60→60,Globals.HolyColour) {Blend=true, Target=attackTarget} | - |
| Resurrection | 命中/结算 | 特效(310,10,60ms,LibraryFile.MagicEx,60→60,Globals.HolyColour) {Blend=true, Target=this} | ResurrectionStart |
| Purification | 施放 | 特效(230,10,100ms,LibraryFile.MagicEx2,20→40,Globals.HolyColour) {Blend=true, Target=attackTarget} | PurificationEnd |
| Purification | 命中/结算 | 特效(220,10,60ms,LibraryFile.MagicEx2,20→40,Globals.HolyColour) {Blend=true, Target=this} | PurificationStart |
| StrengthOfFaith | 施放 | 特效(370,10,100ms,LibraryFile.MagicEx2,20→40,Globals.PhantomColour) {Blend=true, Target=attackTarget} | StrengthOfFaithEnd |
| StrengthOfFaith | 命中/结算 | 特效(360,10,100ms,LibraryFile.MagicEx2,20→40,Globals.PhantomColour) {Blend=true, Target=this} | StrengthOfFaithStart |
| CelestialLight | 施放 | 特效(290,9,100ms,LibraryFile.MagicEx2,20→40,Globals.HolyColour) {Blend=true, Target=attackTarget} | - |
| CelestialLight | 命中/结算 | 特效(280,8,100ms,LibraryFile.MagicEx2,10→35,Globals.HolyColour) {Blend=true, DrawColour=Color.Yellow, Target=this} | MagicShieldStart |
| LifeSteal | 施放 | 特效(2500,10,100ms,LibraryFile.MagicEx2,10→35,Globals.DarkColour) {Blend=true, Target=attackTarget} | HolyStrikeEnd |
| LifeSteal | 命中/结算 | 特效(2410,9,100ms,LibraryFile.MagicEx2,10→35,Globals.DarkColour) {Blend=true, Direction=action.Direction, Target=this} | HolyStrikeStart |
| ImprovedExplosiveTalisman | 施放 | 投射(980,6,100ms,LibraryFile.MagicEx2,35→35,Globals.DarkColour,origin=CurrentLocation) {Blend=true, Has16Directions=false, MapTarget=point, Target=point}; 投射(980,6,100ms,LibraryFile.MagicEx2,35→35,Globals.DarkColour,origin=CurrentLocation) {Blend=true, Has16Directions=false, Target=attackTarget}; 特效(1160,10,100ms,LibraryFile.MagicEx2,20→50,Globals.DarkColour) {Blend=true, Target=attackTarget} | FireStormEnd, ExplosiveTalismanTravel |
| ImprovedExplosiveTalisman | 命中/结算 | 特效(980,6,80ms,LibraryFile.MagicEx2,10→35,Globals.DarkColour) {Blend=true, Direction=action.Direction, Target=this} | ExplosiveTalismanStart |
| Parasite | 施放 | 投射(800,6,100ms,LibraryFile.MagicEx5,35→35,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Has16Directions=false, MapTarget=point, Target=point}; 投射(800,6,100ms,LibraryFile.MagicEx5,35→35,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Has16Directions=false, Target=attackTarget}; 特效(1200,10,100ms,LibraryFile.MagicEx5,20→50,Globals.NoneColour) {Blend=true, Target=attackTarget} | ParasiteTravel |
| Parasite | 命中/结算 | 特效(1000,5,100ms,LibraryFile.MagicEx5,35→35,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this} | FireBallStart |
| Neutralize | 施放 | 投射(300,4,80ms,LibraryFile.MagicEx7,35→35,Globals.FireColour,origin=CurrentLocation) {Blend=true, MapTarget=point, Target=point}; 投射(300,4,80ms,LibraryFile.MagicEx7,35→35,Globals.FireColour,origin=CurrentLocation) {Blend=true, Target=attackTarget}; 特效(460,10,100ms,LibraryFile.MagicEx7,0→0,Globals.FireColour) {Blend=true, Target=attackTarget} | NeutralizeEnd, NeutralizeTravel |
| Neutralize | 命中/结算 | 特效(2080,6,80ms,LibraryFile.Magic,10→35,Globals.DarkColour) {Blend=true, Direction=action.Direction, Target=this} | ExplosiveTalismanStart |
| DarkSoulPrison | 施放 | 特效(600,9,100ms,LibraryFile.MagicEx6,10→35,Globals.DarkColour) {Blend=true, Target=this} | DarkSoulPrison |
| CorpseExploder | 施放 | 投射(300,4,100ms,LibraryFile.MagicEx7,35→35,Globals.FireColour,origin=CurrentLocation) {Blend=true, MapTarget=point, Target=point}; 投射(300,4,100ms,LibraryFile.MagicEx7,35→35,Globals.FireColour,origin=CurrentLocation) {Blend=true, Target=attackTarget}; 特效(1000,17,100ms,LibraryFile.MagicEx7,20→50,Globals.FireColour) {Blend=true, MapTarget=location, Target=location} | CorpseExploderEnd, ExplosiveTalismanTravel |
| CorpseExploder | 命中/结算 | 特效(2080,6,80ms,LibraryFile.Magic,10→35,Globals.DarkColour) {Blend=true, Direction=action.Direction, Target=this} | ExplosiveTalismanStart |
| SearingLight | 施放 | 投射(1210,10,70ms,LibraryFile.MagicEx3,35→35,Globals.HolyColour,origin=CurrentLocation,粒子=null) {Blend=true, Has16Directions=false, MapTarget=point, Target=point}; 投射(1210,10,70ms,LibraryFile.MagicEx3,35→35,Globals.HolyColour,origin=CurrentLocation,粒子=null) {Blend=true, Has16Directions=false, Target=attackTarget}; 特效(1300,10,100ms,LibraryFile.MagicEx3,10→35,Globals.FireColour) {Blend=true, Target=attackTarget} | FireBallEnd, HolyStrikeTravel |
| SearingLight | 命中/结算 | 特效(1190,8,70ms,LibraryFile.MagicEx3,10→35,Globals.HolyColour) {Blend=true, Target=this} | HolyStrikeStart |
| SoulResonance | 施放 | 投射(500,8,100ms,LibraryFile.MagicEx7,35→35,Globals.NoneColour,origin=CurrentLocation,粒子=null) {Blend=true, MapTarget=point, Target=point}; 投射(500,8,100ms,LibraryFile.MagicEx7,35→35,Globals.NoneColour,origin=CurrentLocation,粒子=null) {Blend=true, Has16Directions=true, Target=attackTarget}; 特效(670,9,100ms,LibraryFile.MagicEx7,10→35,Globals.NoneColour) {Blend=true, Target=attackTarget} | FireBallEnd, FireBallTravel |
| BindingTalisman | 施放 | 投射(3600,1,100ms,LibraryFile.MagicEx5,15→15,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Has16Directions=true, MapTarget=point, Target=point}; 投射(3600,1,100ms,LibraryFile.MagicEx5,15→15,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Has16Directions=true, Target=attackTarget} | ExplosiveTalismanTravel |
| BindingTalisman | 命中/结算 | 特效(3500,4,100ms,LibraryFile.MagicEx5,0→0,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this} | ExplosiveTalismanStart |
| BrainStorm | 施放 | 投射(3200,5,100ms,LibraryFile.MagicEx5,35→35,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Has16Directions=true, MapTarget=point, Target=point}; 投射(3200,5,100ms,LibraryFile.MagicEx5,15→15,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Has16Directions=true, Target=attackTarget}; 特效(3400,15,100ms,LibraryFile.MagicEx5,25→25,Globals.NoneColour) {Blend=true, Loop=false, Target=attackTarget} | BrainStorm |
| BrainStorm | 命中/结算 | 特效(4600,10,100ms,LibraryFile.MagicEx5,0→0,Globals.NoneColour) {Blend=true, Skip=20, Direction=action.Direction, Target=this} | BindingTalisman |
| HeavenlySky | 施放 | 特效(5400,39,100ms,LibraryFile.MagicEx5,50→50,Globals.LightningColour) {Blend=true, MapTarget=point, Target=point} | HeavenlySky |
| PoisonCloud | 施放 | 特效(5500,56,100ms,LibraryFile.MagicEx5,50→50,Globals.DarkColour) {Blend=true, MapTarget=point, Target=point} | PoisonCloud |
| WraithGrip | 施放 | 特效(1420,14,100ms,LibraryFile.MagicEx4,60→60,Globals.NoneColour) {Blend=true, DrawType=DrawType.Floor, BlendRate=0.4f, Target=attackTarget}; 特效(1440,14,100ms,LibraryFile.MagicEx4,60→60,Globals.NoneColour) {Blend=true, DrawType=DrawType.Floor, BlendRate=0.4f, Target=attackTarget} | WraithGripEnd |
| WraithGrip | 命中/结算 | 特效(1460,15,60ms,LibraryFile.MagicEx4,60→60,Globals.NoneColour) {Blend=true, DrawType=DrawType.Floor, BlendRate=0.4f, Target=this} | WraithGripStart |
| HellFire | 施放 | 特效(1500,10,100ms,LibraryFile.MagicEx4,60→60,Globals.FireColour) {Blend=true, DrawType=DrawType.Floor, Target=attackTarget} | WraithGripEnd |
| HellFire | 命中/结算 | 特效(1520,15,60ms,LibraryFile.MagicEx4,60→60,Globals.FireColour) {Blend=true, DrawType=DrawType.Floor, Target=this} | WraithGripStart |
| BurningFire | 施放 | 特效(900,10,60ms,LibraryFile.MagicEx6,10→35,Globals.FireColour) {Blend=true, MapTarget=point, Target=point} | FireWallStart |
| MagicCombustion | 施放 | 投射(100,6,100ms,LibraryFile.MagicEx7,0→0,Globals.NoneColour,origin=CurrentLocation) {Blend=true, MapTarget=point, Target=point}; 投射(100,6,100ms,LibraryFile.MagicEx7,0→0,Globals.NoneColour,origin=CurrentLocation) {Blend=true, Explode=true, Target=attackTarget}; 特效(280,10,100ms,LibraryFile.MagicEx7,20→50,Globals.NoneColour) {Blend=true, Target=attackTarget} | ElementalSwordsEnd |
| Hemorrhage | 施放 | 投射(1100,6,100ms,LibraryFile.MagicEx7,35→35,Globals.FireColour,origin=CurrentLocation) {Blend=true, MapTarget=point, Target=point}; 投射(1100,6,100ms,LibraryFile.MagicEx7,35→35,Globals.FireColour,origin=CurrentLocation) {Blend=true, Target=attackTarget}; 特效(1270,10,100ms,LibraryFile.MagicEx7,0→0,Globals.FireColour) {Blend=true, Target=attackTarget} | Hemorrhage |
| Chain | 施放 | 特效(20,7,100ms,LibraryFile.MagicEx7,50→50,Globals.NoneColour) {Blend=true, Target=attackTarget} | Chain |
| Chain | 命中/结算 | 特效(0,7,140ms,LibraryFile.MagicEx7,60→60,Globals.NoneColour) {Blend=true, Target=this} | Chain |
| FlamingDaggers | 施放 | 投射(3900,7,100ms,LibraryFile.MagicEx5,35→35,Globals.FireColour,origin=CurrentLocation) {Blend=true, Has16Directions=true, Target=attackTarget}; 特效(4100,8,100ms,LibraryFile.MagicEx5,10→35,Globals.FireColour) {Blend=true, Target=attackTarget} | FlamingDaggers |
| FlamingDaggers | 命中/结算 | 特效(3800,10,100ms,LibraryFile.MagicEx5,10→35,Globals.FireColour) {Blend=true, Target=this} | - |
| Shredding | 施放 | 投射(4300,5,100ms,LibraryFile.MagicEx5,35→35,Globals.FireColour,origin=CurrentLocation) {Blend=true, Has16Directions=true, Target=attackTarget}; 特效(4500,10,100ms,LibraryFile.MagicEx5,10→35,Globals.FireColour) {Blend=true, Target=attackTarget} | Shredding |
| Shredding | 命中/结算 | 特效(4200,10,100ms,LibraryFile.MagicEx5,10→35,Globals.FireColour) {Blend=true, Target=this} | - |
| FourWheels | 施放 | 特效(5600,35,100ms,LibraryFile.MagicEx5,50→50,Globals.FireColour) {Blend=true, MapTarget=point, Target=point} | FourWheels |
| CrescentMoon | 施放 | 特效(5700,21,100ms,LibraryFile.MagicEx5,50→50,Globals.PhantomColour) {Blend=true, MapTarget=point, Target=point} | CrescentMoon |
| PinkFireBall | 施放 | 投射(1500,6,100ms,LibraryFile.MonMagicEx20,35→35,Color.Purple,origin=CurrentLocation) {Blend=true, Direction=action.Direction, MapTarget=point, Target=point}; 投射(1600,6,100ms,LibraryFile.MonMagicEx20,35→35,Color.Purple,origin=CurrentLocation) {Blend=true, Has16Directions=false, Target=attackTarget}; 特效(1700,10,100ms,LibraryFile.MonMagicEx20,35→35,Color.Purple) {Blend=true, Direction=action.Direction, Target=attackTarget} | FireBallEnd, FireBallTravel |
| GreenSludgeBall | 施放 | 投射(2600,7,100ms,LibraryFile.MonMagicEx23,35→35,Color.GreenYellow,origin=CurrentLocation) {Blend=true, Direction=action.Direction, MapTarget=point, Target=point}; 投射(2600,7,100ms,LibraryFile.MonMagicEx23,35→35,Color.GreenYellow,origin=CurrentLocation) {Blend=true, Has16Directions=false, Target=attackTarget}; 特效(2780,6,100ms,LibraryFile.MonMagicEx23,35→35,Color.GreenYellow) {Blend=true, Direction=action.Direction, Target=attackTarget} | FireBallEnd, FireBallTravel |
| MonsterScortchedEarth | 施放 | 特效(220,1,3000ms,LibraryFile.ProgUse,0→0,Globals.NoneColour) {DrawType=DrawType.Floor, Opacity=0.8F, StartTime=CEnvir.Now.AddMilliseconds(500 + Functions.Distance(point, MapTarget=point, Target=point}; 特效(2450 + CEnvir.Random.Next(5) * 10,10,250ms,LibraryFile.Magic,0→0,Globals.NoneColour) {Blend=true, DrawType=DrawType.Floor, StartTime=CEnvir.Now.AddMilliseconds(500 + Functions.Distance(point, MapTarget=point, Target=point}; 特效(1930,30,50ms,LibraryFile.Magic,20→70,Globals.FireColour) {Blend=true, BlendRate=1F, StartTime=CEnvir.Now.AddMilliseconds(Functions.Distance(point, MapTarget=point, Target=point} | LavaStrikeEnd |
| MonsterScortchedEarth | 命中/结算 | 特效(220,1,3000ms,LibraryFile.ProgUse,0→0,Globals.NoneColour) {DrawType=DrawType.Floor, Opacity=0.8F, StartTime=CEnvir.Now.AddMilliseconds(500 + Functions.Distance(point, MapTarget=point, Target=point}; 特效(2450 + CEnvir.Random.Next(5) * 10,10,250ms,LibraryFile.Magic,0→0,Globals.NoneColour) {Blend=true, DrawType=DrawType.Floor, StartTime=CEnvir.Now.AddMilliseconds(500 + Functions.Distance(point, MapTarget=point, Target=point}; 特效(1930,30,50ms,LibraryFile.Magic,20→70,Globals.FireColour) {Blend=true, BlendRate=1F, StartTime=CEnvir.Now.AddMilliseconds(Functions.Distance(point, MapTarget=point, Target=point} | LavaStrikeEnd |
| MonsterIceStorm | 施放 | 特效(6230,10,100ms,LibraryFile.MonMagicEx3,20→70,Globals.IceColour) {Blend=true, BlendRate=1F, MapTarget=point, Target=point} | - |
| MonsterThunderStorm | 施放 | 特效(650,6,100ms,LibraryFile.MonMagicEx5,20→70,Globals.LightningColour) {Blend=true, BlendRate=1F, MapTarget=point, Target=point} | - |
| SamaGuardianFire | 施放 | 特效(4000,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.FireColour) {Blend=true, MapTarget=point, Target=point} | - |
| SamaGuardianIce | 施放 | 特效(4100,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.IceColour) {Blend=true, MapTarget=point, Target=point} | - |
| SamaGuardianLightning | 施放 | 特效(4200,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.LightningColour) {Blend=true, MapTarget=point, Target=point} | - |
| SamaGuardianWind | 施放 | 特效(4300,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.WindColour) {Blend=true, MapTarget=point, Target=point} | - |
| SamaPhoenixFire | 施放 | 特效(4500,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.FireColour) {Blend=true, MapTarget=point, Target=point} | - |
| SamaBlackIce | 施放 | 特效(4600,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.IceColour) {Blend=true, MapTarget=point, Target=point} | - |
| SamaBlueLightning | 施放 | 特效(4700,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.LightningColour) {Blend=true, MapTarget=point, Target=point} | - |
| SamaWhiteWind | 施放 | 特效(4800,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.WindColour) {Blend=true, MapTarget=point, Target=point} | - |
| SamaProphetFire | 施放 | 特效(5600,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.FireColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | - |
| SamaProphetLightning | 施放 | 特效(5200,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.LightningColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | - |
| SamaProphetWind | 施放 | 特效(5400,10,100ms,LibraryFile.MonMagicEx9,30→80,Globals.WindColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | - |
| Assault | 施放 | - | AssaultStart |
| HundredFist | 施放 | - | - |
| None | 施放 | 特效(1090,6,100ms,LibraryFile.MagicEx,10→50,attackColour) {Blend=true, Direction=action.Direction, DrawColour=attackColour, Target=this} | - |
| Slaying | 施放 | 特效(1350,6,100ms,LibraryFile.Magic,10→50,attackColour) | SlayingMale, SlayingFemale |
| Thrusting | 施放 | 特效(0,6,100ms,LibraryFile.MagicEx3,20→70,attackColour) {Blend=true, Direction=action.Direction, DrawColour=attackColour, Target=this} | EnergyBlast |
| HalfMoon | 施放 | 特效(230,6,100ms,LibraryFile.Magic,20→70,attackColour) | HalfMoon |
| DestructiveSurge | 施放 | 特效(1420,6,100ms,LibraryFile.MagicEx2,20→70,attackColour) {Blend=true, DrawColour=attackColour, Target=this} | DestructiveSurge |
| FlamingSword | 施放 | 特效(1470,6,100ms,LibraryFile.Magic,10→50,Globals.FireColour) {Blend=true, Direction=action.Direction, Target=this} | FlamingSword |
| DragonRise | 施放 | 特效(2185,10,100ms,LibraryFile.Magic,20→70,attackColour) | DragonRise |
| BladeStorm | 施放 | 特效(1780,10,60ms,LibraryFile.MagicEx,20→70,attackColour) {Blend=true, Direction=action.Direction, DrawColour=attackColour, Target=this} | BladeStorm |
| DefensiveBlow | 施放 | 特效(800,9,100ms,LibraryFile.MagicEx7,10→50,Globals.FireColour) {Blend=true, DrawType=DrawType.Floor, Direction=action.Direction, StartTime=CEnvir.Now.AddMilliseconds(200), DrawColour=attackColour, Target=this} | DefensiveBlow |
| FlameSplash | 施放 | 特效(900,8,100ms,LibraryFile.MagicEx4,20→70,Globals.FireColour) {Blend=true, Target=this} | BladeStorm |
| DragonBlood | 施放 | 特效(200,7,100ms,LibraryFile.MagicEx5,30→60,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this} | - |
| WaningMoon | 施放 | - | WaningMoon |
| CalamityOfFullMoon | 施放 | - | CalamityOfFullMoon |
| Interchange | 施放 | 特效(0,9,100ms,LibraryFile.MagicEx2,60→60,Globals.NoneColour) {Blend=true, Target=this} | TeleportationStart |
| Defiance | 施放 | 特效(40,10,100ms,LibraryFile.MagicEx2,60→60,Globals.NoneColour) {Blend=true, Target=this} | DefianceStart |
| Invincibility | 施放 | 特效(400,10,100ms,LibraryFile.MagicEx5,60→60,Globals.NoneColour) {Blend=true, Target=this} | InvincibilityStart |
| Beckon | 施放 | 特效(580,10,100ms,LibraryFile.MagicEx2,60→60,Globals.NoneColour) {Blend=true, Direction=action.Direction, Target=this} | TeleportationStart |
| Might | 施放 | 特效(60,10,100ms,LibraryFile.MagicEx2,60→60,Globals.NoneColour) {Blend=true, Target=this} | DragonRise |
| SeismicSlam | 施放 | 特效(4900,6,100ms,LibraryFile.MagicEx5,10→35,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this} | SeismicSlam |
| CrushingWave | 施放 | 特效(100,6,100ms,LibraryFile.MagicEx6,0→0,Globals.LightningColour) {Blend=true, Direction=action.Direction, Target=this} | - |
| Endurance | 施放 | 特效(190,10,100ms,LibraryFile.MagicEx3,60→60,Globals.NoneColour) {Blend=true, Target=this} | DefianceStart |
| ReflectDamage | 施放 | 特效(1220,10,100ms,LibraryFile.MagicEx2,60→60,Globals.NoneColour) {Blend=true, Target=this} | ReflectDamageStart |
| MassBeckon | 施放 | 特效(100,10,100ms,LibraryFile.MagicEx5,60→60,Globals.NoneColour) {Blend=true, Target=this} | TeleportationStart |
| Fetter | 施放 | 特效(2370,10,100ms,LibraryFile.MagicEx2,60→60,Globals.NoneColour) {Blend=true, Target=this} | DragonRise |
| Repulsion | 施放 | 特效(90,10,100ms,LibraryFile.Magic,10→35,Globals.WindColour) {Blend=true, Target=this} | RepulsionEnd |
| Teleportation | 施放 | 特效(110,10,60ms,LibraryFile.Magic,10→35,Globals.PhantomColour) {Blend=true, Target=this} | TeleportationStart |
| FireWall | 施放 | 特效(910,10,60ms,LibraryFile.Magic,10→35,Globals.FireColour) {Blend=true, Target=this} | FireWallStart |
| GeoManipulation | 施放 | 特效(110,10,60ms,LibraryFile.Magic,10→35,Globals.PhantomColour) {Blend=true, Target=this} | TeleportationStart |
| MagicShield | 施放 | 特效(830,19,60ms,LibraryFile.Magic,10→35,Globals.PhantomColour) {Blend=true, Target=this} | MagicShieldStart |
| Renounce | 施放 | 特效(80,10,100ms,LibraryFile.MagicEx2,10→35,Globals.PhantomColour) {Blend=true, Target=this} | DefianceStart |
| Tempest | 施放 | 特效(910,10,60ms,LibraryFile.MagicEx2,10→35,Globals.WindColour) {Blend=true, Target=this} | BlowEarthStart |
| JudgementOfHeaven | 施放 | - | LightningStrikeEnd |
| MirrorImage | 施放 | 特效(1260,6,100ms,LibraryFile.MagicEx2,10→35,Globals.NoneColour) {Blend=true, Target=this} | ShacklingTalismanStart |
| FrostBite | 施放 | 特效(500,16,60ms,LibraryFile.MagicEx5,10→35,Globals.IceColour) {Blend=true, Target=this} | FrostBiteStart |
| SuperiorMagicShield | 施放 | 特效(1900,17,60ms,LibraryFile.MagicEx2,10→35,Globals.FireColour) {Blend=true, Target=this} | MagicShieldStart |
| Tornado | 施放 | 特效(2400,4,100ms,LibraryFile.MagicEx5,10→10,Globals.WindColour) | TornadoStart |
| SummonSkeleton | 施放 | - | - |
| SummonJinSkeleton | 施放 | - | - |
| SummonDemonicCreature | 施放 | 特效(740,10,60ms,LibraryFile.Magic,10→35,Globals.PhantomColour) {Blend=true, Target=this} | SummonSkeletonStart |
| Invisibility | 施放 | 特效(810,10,60ms,LibraryFile.Magic,10→35,Globals.PhantomColour) {Blend=true, Target=this} | InvisibilityEnd |
| CombatKick | 施放 | - | TaoistCombatKickStart |
| SummonShinsu | 施放 | 特效(2590,19,60ms,LibraryFile.Magic,10→35,Globals.PhantomColour) {Target=this} | SummonShinsuStart |
| Transparency | 施放 | 特效(430,7,100ms,LibraryFile.MagicEx2,10→35,Globals.PhantomColour) {Blend=true, Target=this} | InvisibilityEnd |
| CursedDoll | 施放 | 特效(690,10,60ms,LibraryFile.MagicEx3,10→35,Globals.FireColour) {Blend=true, Target=this} | SummonSkeletonStart |
| ThunderKick | 施放 | 特效(1190,10,100ms,LibraryFile.MagicEx2,20→40,Globals.NoneColour) {Blend=true, StartTime=CEnvir.Now.AddMilliseconds(400), MapTarget=front, Target=front} | FireStormEnd, TaoistCombatKickStart |
| Spiritualism | 施放 | 特效(1580,11,100ms,LibraryFile.MagicEx2,60→60,attackColour) {Blend=true, DrawColour=attackColour, Target=this} | DefianceStart |
| SummonDead | 施放 | 特效(740,10,60ms,LibraryFile.Magic,10→35,Globals.PhantomColour) {Blend=true, Target=this} | SummonSkeletonStart |
| Cloak | 施放 | 特效(600,10,60ms,LibraryFile.MagicEx4,10→35,Globals.PhantomColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | CloakStart |
| Rake | 施放 | - | - |
| SummonPuppet | 施放 | 特效(800,16,100ms,LibraryFile.MagicEx4,80→50,Globals.PhantomColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | SummonPuppet |
| TheNewBeginning | 施放 | 特效(2200,8,100ms,LibraryFile.MagicEx4,60→60,Globals.NoneColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | TheNewBeginning |
| DragonRepulse | 施放 | 特效(1000,10,60ms,LibraryFile.MagicEx4,0→0,Globals.NoneColour) {MapTarget=CurrentLocation, Target=CurrentLocation}; 特效(1020,10,60ms,LibraryFile.MagicEx4,80→50,Globals.LightningColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | DragonRepulseStart |
| Abyss | 施放 | 特效(2000,14,70ms,LibraryFile.MagicEx4,80→50,Globals.PhantomColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | AbyssStart |
| FlashOfLight | 施放 | 特效(2300,8,60ms,LibraryFile.MagicEx4,35→35,Globals.NoneColour) {Blend=true, Direction=Direction, MapTarget=CurrentLocation, Target=CurrentLocation} | - |
| Evasion | 施放 | 特效(2500,12,70ms,LibraryFile.MagicEx4,80→50,Globals.NoneColour) {Blend=true, DrawType=DrawType.Floor, MapTarget=CurrentLocation, Target=CurrentLocation} | EvasionStart |
| RagingWind | 施放 | 特效(2600,12,70ms,LibraryFile.MagicEx4,80→50,Globals.NoneColour) {Blend=true, DrawType=DrawType.Floor, MapTarget=CurrentLocation, Target=CurrentLocation} | RagingWindStart |
| Concentration | 施放 | 特效(300,15,100ms,LibraryFile.MagicEx5,60→60,Globals.NoneColour) {Blend=true, Target=this} | Concentration |
| Containment | 施放 | 特效(590,9,60ms,LibraryFile.MagicEx3,60→60,Globals.NoneColour) {Blend=true, Target=this} | Containment |
| DoomClawRightPinch | 施放 | 特效(2640,7,100ms,LibraryFile.MonMagicEx19,0→0,Globals.NoneColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation}; 特效(2680,9,100ms,LibraryFile.MonMagicEx19,0→0,Globals.NoneColour) {Blend=true, MapTarget=Functions.Move(Functions.Move(CurrentLocation, Target=Functions.Move(Functions.Move(CurrentLocation} | - |
| DoomClawLeftPinch | 施放 | 特效(2660,7,100ms,LibraryFile.MonMagicEx19,0→0,Globals.NoneColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation}; 特效(2680,9,100ms,LibraryFile.MonMagicEx19,0→0,Globals.NoneColour) {Blend=true, MapTarget=Functions.Move(CurrentLocation, Target=Functions.Move(CurrentLocation} | - |
| DoomClawRightSwipe | 施放 | 特效(2700,8,100ms,LibraryFile.MonMagicEx19,0→0,Globals.NoneColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | - |
| DoomClawLeftSwipe | 施放 | 特效(2720,8,100ms,LibraryFile.MonMagicEx19,0→0,Globals.NoneColour) {Blend=true, MapTarget=CurrentLocation, Target=CurrentLocation} | - |
| DoomClawSpit | 施放 | 投射(2500,7,100ms,LibraryFile.MonMagicEx19,0→0,Globals.NoneColour,origin=p) {Blend=true, Skip=0, Explode=true, MapTarget=point, Target=point}; 特效(2520,8,100ms,LibraryFile.MonMagicEx19,0→0,Globals.NoneColour) {Blend=true, MapTarget=eff.MapTarget, Target=eff.MapTarget} | - |

 ---
# 第 7 章 地图渲染(MapControl.cs / .map 格式 / 光照 / 天气)

> 数据来源:`Client/Scenes/Views/MapControl.cs`(1940 行,整文件核对)、`LibraryCore/SystemModels/MapInfo.cs`、`LibraryCore/Enum.cs`、`Client/Models/Particles/Weather/*.cs`。

## 7.1 网格与屏幕常量(MapControl.cs:178-187)

| 常量 | 值 | 语义 |
|---|---|---|
| `CellWidth` | 48 | 每格像素宽 |
| `CellHeight` | 32 | 每格像素高(菱形格,宽高比 3:2) |
| `ManualHeightOffset` | 34 | Y 方向手动补偿,用于对准 3/4 俯视的可见格面 |
| `ViewRangeX` | 12 | 默认横向视野格数 |
| `ViewRangeY` | 24 | 默认纵向视野格数 |

屏幕中心换算(构造/大小变化时计算,行 145-148):

```
OffSetX = Size.Width  / 2 / CellWidth;      // 中心在网格中的 X 偏移
OffSetY = Size.Height / 2 / CellHeight;     // 中心在网格中的 Y 偏移
PixelOffsetX = (Size.Width  - CellWidth ) / 2 - OffSetX * CellWidth;
PixelOffsetY = (Size.Height - CellHeight) / 2 - OffSetY * CellHeight - ManualHeightOffset;
```

(行 183-186 声明为 `public static int`,四个均为静态。)

## 7.2 .map 二进制文件格式(LoadMap,MapControl.cs:484-545)

读取路径:`Path.Combine(Config.MapPath, MapInfo.FileName + ".map")`,整文件读入 `MemoryStream` 后按如下偏移解析(小端序,BinaryReader):

| 偏移 | 读取 | 字段 |
|---|---|---|
| 0-21 | — | 头部 22 字节(未知,跳过) |
| 22 | Int16 ×2 | `Width`,`Height`(格数) |
| 28 | 见下 | 单元格数据 |

**第一段:背景层(半分辨率)** — 循环 `x < Width/2`,`y < Height/2`:

| 读取 | 写入 |
|---|---|
| Byte | `Cells[x*2, y*2].BackFile`(背景库索引) |
| UInt16 | `Cells[x*2, y*2].BackImage`(背景贴图索引) |

背景只存偶数格 `(x*2, y*2)`,即 `Width/2 × Height/2` 个条目。

**第二段:全分辨率单元格** — 循环 `x < Width`,`y < Height`,每格按序:

| 顺序 | 读取 | 写入 |
|---|---|---|
| 1 | Byte | `flag`(原始标志字节,后面算阻挡用) |
| 2 | Byte | `MiddleAnimationFrame`(中层动画编码,原样存入,见 7.4) |
| 3 | Byte | `value` → `FrontAnimationFrame = value == 255 ? 0 : value; 再 &= 0x8F`(注释 "Probably a Blend Flag";0xFF 视为无动画) |
| 4 | Byte | `FrontFile`(前景库索引) |
| 5 | Byte | `MiddleFile`(中层库索引) |
| 6 | UInt16 | `MiddleImage = 读值 + 1`(**注意 +1**,绘制时再 -1 还原) |
| 7 | UInt16 | `FrontImage = 读值 + 1` |
| 8 | 跳过 3 字节 | `mStream.Seek(3, Current)` |
| 9 | Byte | `Light = (byte)(读值 & 0x0F) * 2`(0-30,低 4 位 ×2) |
| 10 | 跳过 1 字节 | `mStream.Seek(1, Current)` |
| 11 | (用第 1 步的 flag) | `Flag = ((flag & 0x01) != 1) || ((flag & 0x02) != 2)` |

每格合计:1+1+1+1+1+2+2+3+1+1 = 14 字节。

**Cell 结构字段**(MapControl.cs:1876-1902):

| 字段 | 类型 | 说明 |
|---|---|---|
| `BackFile` / `BackImage` | int | 背景库/贴图(仅偶数格有效) |
| `MiddleFile` / `MiddleImage` | int | 中层库/贴图(绘制用 `MiddleImage - 1`) |
| `FrontFile` / `FrontImage` | int | 前景库/贴图 |
| `FrontAnimationFrame` / `FrontAnimationTick` | int | 前景动画编码/时钟 |
| `FrontAnimationCount` | int(只读) | `FrontAnimationFrame & 0x0F`(低 4 位 = 帧数) |
| `FrontAnimationBlend` | bool(只读) | `(FrontAnimationFrame & 0x80) != 0`(bit7 = 混合标志) |
| `MiddleAnimationFrame` / `MiddleAnimationTick` | int | 中层动画编码/时钟 |
| `MiddleAnimationCount` | int(只读) | `MiddleAnimationFrame & 0x0F` |
| `MiddleAnimationBlend` | bool(只读) | `(MiddleAnimationFrame & 0x80) != 0` |
| `Light` | int | 格光照值(0-30) |
| `Flag` | bool | 阻挡标志 |
| `MiddleLibrary` / `FrontLibrary` | MirLibrary | 懒加载缓存 |
| `LibrariesLoaded` | bool | 库加载完成标志 |
| `Objects` | List<MapObject> | 本格对象列表 |

常量:`FrontFrameMask = 0x0F`、`FrontBlendBit = 0x80`、`MiddleFrameMask = 0x0F`、`MiddleBlendBit = 0x80`。

**阻挡判定** `Blocking()`:遍历 `Objects`,任一 `ob.Blocking` 为 true 则阻挡;否则返回 `Flag`。

**注意差异**:`FrontAnimationFrame` 在装载时已被 `& 0x8F` 清洗(只保留低 4 位 + bit7),而 `MiddleAnimationFrame` 是原样字节——两者取帧数/混合标志时都通过 `& 0x0F` / `& 0x80` 提取,因此中层字节的 bit4-6 会被忽略,高层动画位的语义两边一致。

## 7.3 KROrder:地图文件字节 → LibraryFile 映射表(62 条)

`Libraries.KROrder`(键 0-71,有缺号),用途:把单元格里的 `MiddleFile`/`FrontFile`/`BackFile` 字节映射成 `LibraryFile` 枚举,再经 `CEnvir.LibraryList` 得到实际 `.Zl` 库。绘制时**跳过 `Tilesc`**。库路径见第 2 章(根级 `Data\Map Data\*.Zl`,其余 `Data\Map Data\{Wood,Sand,Snow,Forest}\*.Zl`)。

| 键 | LibraryFile | 键 | LibraryFile | 键 | LibraryFile | 键 | LibraryFile |
|---|---|---|---|---|---|---|---|
| 0 | Tilesc | 1 | Tiles30c | 2 | Tiles5c | 3 | SmTilesc |
| 4 | Housesc | 5 | Cliffsc | 6 | Dungeonsc | 7 | Innersc |
| 8 | Furnituresc | 9 | Wallsc | 10 | SmObjectsc | 11 | Animationsc |
| 12 | Object1c | 13 | Object2c | 14 | (缺) | 15 | Wood_Tilesc |
| 16 | Wood_Tiles30c | 17 | Wood_Tiles5c | 18 | Wood_SmTilesc | 19 | Wood_Housesc |
| 20 | Wood_Cliffsc | 21 | Wood_Dungeonsc | 22 | Wood_Innersc | 23 | Wood_Furnituresc |
| 24 | Wood_Wallsc | 25 | Wood_SmObjectsc | 26 | Wood_Animationsc | 27-29 | (缺) |
| 30 | Sand_Tilesc | 31 | Sand_Tiles30c | 32 | Sand_Tiles5c | 33 | Sand_SmTilesc |
| 34 | Sand_Housesc | 35 | Sand_Cliffsc | 36 | Sand_Dungeonsc | 37 | Sand_Innersc |
| 38 | Sand_Furnituresc | 39 | Sand_Wallsc | 40 | Sand_SmObjectsc | 41 | Sand_Animationsc |
| 42-44 | (缺) | 45 | Snow_Tilesc | 46 | Snow_Tiles30c | 47 | Snow_Tiles5c |
| 48 | Snow_SmTilesc | 49 | Snow_Housesc | 50 | Snow_Cliffsc | 51 | Snow_Dungeonsc |
| 52 | Snow_Innersc | 53 | Snow_Furnituresc | 54 | Snow_Wallsc | 55 | Snow_SmObjectsc |
| 56 | Snow_Animationsc | 57-59 | (缺) | 60 | Forest_Tilesc | 61 | Forest_Tiles30c |
| 62 | Forest_Tiles5c | 63 | Forest_SmTilesc | 64 | Forest_Housesc | 65 | Forest_Cliffsc |
| 66 | Forest_Dungeonsc | 67 | Forest_Innersc | 68 | Forest_Furnituresc | 69 | Forest_Wallsc |
| 70 | Forest_SmObjectsc | 71 | Forest_Animationsc | | | | |

布局规律:0-14 根级 15 项、15-29 Wood 15 项、30-44 Sand 15 项、45-59 Snow 15 项、60-71 Forest 12 项(无 Object1c/2c)。缺号:14、27-29、42-44、57-59。

## 7.4 主绘制管线(OnClearTexture,MapControl.cs:203-327)

`OnClearTexture` 每帧执行,顺序固定(自底向上):

1. `DrawBackground()` — `MapInfo.Background > 0` 时,取 `LibraryFile.Background` 库 `TryGetTexture(MapInfo.Background)`,`PresentTexture` 铺满 `DisplayArea`(行 331-340)。
2. `FLayer` 渲染纹理整层 blit(背景地面层,见 7.5)。
3. `Config.DrawEffects` 时:遍历 `Effects`,画所有 `DrawType == DrawType.Floor` 的特效。
4. `DrawObjects()` — 按行自后向前画地形对象与地图对象(见 7.6)。
5. `MapObject.MouseObject != null` 时 `MouseObject.DrawBlend()`(悬停高亮)。
6. `Config.DrawEffects` 时:画所有 `DrawType == DrawType.Final` 的特效。
7. `RenderingPipelineManager.FlushSprite()`。
8. 保存当前混合状态 → `SetBlend(true, 1F, BlendMode.LIGHTMAP)` → `LLayer` 光照纹理整层 blit(见 7.7)→ 恢复原混合状态。
9. 名字层:`Objects` 中未死亡对象按 `Race` 开关画名(`Config.ShowPlayerNames`/`ShowItemNames`(且 `CurrentLocation != MapLocation`)/`ShowMonsterNames`;NPC、Spell 恒画);再画 `MouseObject` 名字(Item 除外)。
10. 遍历 `Objects`:`DrawChat()` → `DrawPoison()` → `DrawHealth()`。
11. `Config.ShowDamageNumbers` 时 `DrawDamage()`。
12. 物品焦点环:仅当 `MapLocation` 在格内,对该格 `Objects` 中的 `ItemObject` 从后往前 `DrawFocus(layer++)`。

## 7.5 DrawBackground(行 331-340)

```
if (MapInfo.Background <= 0) return;
CEnvir.LibraryList.TryGetValue(LibraryFile.Background, out library);
library.TryGetTexture(MapInfo.Background, ImageType.Image, out _, out texture, out sourceRectangle);
PresentTexture(texture, sourceRectangle, Parent, DisplayArea, Color.White, this, 0, 0, 1F);
```

`MapInfo.Background` 为 0 表示无背景贴图。

## 7.6 DrawObjects 坐标与绘制规则(行 342-482)

**可见范围**(以玩家格为中心):

```
minX = max(0, User.X - OffSetX - 4);   maxX = min(Width-1,  User.X + OffSetX + 4);
minY = max(0, User.Y - OffSetY - 4);   maxY = min(Height-1, User.Y + OffSetY + 25);   // 下方多 25 格(向上叠放的贴图高)
```

**每格屏幕坐标**(核心公式,行 349/353):

```
drawX = (x - User.CurrentLocation.X + OffSetX) * CellWidth  + PixelOffsetX - User.MovingOffSet.X - User.ShakeScreenOffset.X;
drawY = (y - User.CurrentLocation.Y + OffSetY + 1) * CellHeight + PixelOffsetY - User.MovingOffSet.Y - User.ShakeScreenOffset.Y;
```

注意:drawY **多加 1 格**(+1 × CellHeight),即对象行整体下移一格;X 不加。

**库解析(懒加载)**:`KROrder.TryGetValue(cell.MiddleFile, out file) && file != LibraryFile.Tilesc` → `CEnvir.LibraryList.TryGetValue(file, out cell.MiddleLibrary)`;Front 同理。

**中层绘制**:

```
index = cell.MiddleImage - 1;
blend = false;
if (cell.MiddleAnimationFrame > 1 && cell.MiddleAnimationFrame < 255) {
    blend = cell.MiddleAnimationBlend;
    index += Animation % cell.MiddleAnimationCount;      // Animation = 全局动画计数器
}
s = MiddleLibrary.GetSize(index);
if (非(48×32) 且非(96×64)):   // 大贴图按底边对齐
    blend ? DrawBlend(index, drawX, drawY - s.Height, White, false, 0.5F) : Draw(index, drawX, drawY - s.Height, White, false, 1F);
else:
    Draw(index, drawX, drawY - s.Height, White, false, 1F);
```

**前景绘制**:

```
index = cell.FrontImage - 1;
blend = false;
if (cell.FrontAnimationFrame > 1 && cell.FrontAnimationFrame < 255) {
    blend = cell.FrontAnimationBlend;
    frameCount = cell.FrontAnimationCount;
    if (frameCount > 0) index += Animation % frameCount;
}
s = FrontLibrary.GetSize(index);
if (非格尺寸)  blend ? DrawBlend(index, drawX, drawY - s.Height,  White, false, 0.5F) : Draw(index, drawX, drawY - s.Height, White, false, 1F);
else           blend ? DrawBlend(index, drawX, drawY - CellHeight, White, false, 0.5F) : Draw(index, drawX, drawY - CellHeight, White, false, 1F);
```

要点:
- 非格尺寸贴图按**底边**对齐(`drawY - s.Height`);格尺寸(48×32 或 96×64)贴图按**格底**对齐(`drawY - CellHeight`,前沿层)。
- 动画格:帧号 = 静态帧 + `全局Animation % 帧数`;混合标志位时用 `DrawBlend` 0.5F 半透明。
- 动画编码:低 4 位帧数(0-15),bit7 混合标志;`FrontAnimationFrame` 装载时 `&0x8F` 清洗,`MiddleAnimationFrame` 不洗。

**按行绘制对象**(每行 y 结束后):

```
foreach (MapObject ob in Objects)
    if (ob.RenderY == y) ob.Draw();
```

**Object 型特效的行归属**:

```
if (ob.MapTarget.IsEmpty && ob.Target != null):  ob.Target.RenderY == y && ob.Target != User → ob.Draw()
else if (ob.MapTarget.Y == y):                   ob.Draw()
```

**行循环之后**:

```
if (User.Opacity == 1f) {            // 玩家半透明重画(自身被遮挡时)
    User.Opacity = 0.65F;
    User.DrawPlayer(false, false);
    User.Opacity = 1F;
}
if (Config.DrawEffects && Config.DrawParticles)
    foreach (ParticleEffects) ob.Draw();
if (Config.DrawEffects)
    foreach (Effects): DrawType == Object && MapTarget 为空 && Target == User → ob.Draw();
```

## 7.7 Floor 层(背景地面,MapControl.cs:1465-1549)

`public sealed class Floor : DXControl`,`IsControl = false`,`Draw()`/`DrawControl()` 为空(只渲染到 `ControlTexture` 供主管线 blit)。

`OnClearTexture` 两段循环:

**段 1(背景贴图,半分辨率)** — 范围 `±(OffSet+4)`,坐标公式**不带 +1**(行 1483):

```
drawY = (y - User.Y + OffSetY) * CellHeight + PixelOffsetY - User.MovingOffSet.Y - User.ShakeScreenOffset.Y;
```

仅画 `y % 2 == 0 && x % 2 == 0` 的偶数格(与 7.2 背景层半分辨率存法对应):

```
if (y%2==0 && x%2==0):
    KROrder.TryGetValue(tile.BackFile, out file) → CEnvir.LibraryList.TryGetValue(file, out library)
    library.Draw(tile.BackImage, drawX, drawY, Color.White, false, 1F, ImageType.Image);
```

**段 2(静态中层/前景贴图)** — 坐标公式**带 +1**(行 1510,同主管线):

```
drawY = (y - User.Y + OffSetY + 1) * CellHeight + PixelOffsetY - ...
```

- 跳过动画格:`MiddleAnimationFrame > 1 && < 255 → continue`(动画格只由主管线画)。
- 只画格尺寸贴图(48×32 或 96×64),`drawY - CellHeight` 对齐;非格尺寸(大贴图)在 Floor 层跳过(由主管线 DrawObjects 处理)。

## 7.8 光照层 LLayer(行 1561-1810)

`public sealed class Light : DXControl`,`IsControl = false`。

**常量**(行 1562-1566):

| 常量 | 值 |
|---|---|
| `LightScale` | 0.02F |
| `BaseLightSize` | 0.1F |
| `TileLightScaleMultiplier` | 30F |
| `EffectLightScaleDivisor` | 5F |
| `TileLightSearchPadding` | 15 |

构造函数:`BackColour = Color.FromArgb(15, 15, 15)`(深灰底 = 夜间环境光)。

**UpdateAmbientLight()**(行 1756-1781),按 `MapInfo.Light`(LightSetting 枚举:Default=0, Light=1, Night=2, Twilight=3)切换:

| MapInfo.Light | BackColour | Visible |
|---|---|---|
| `Default` | `Color.FromArgb(shading, shading, shading)`,shading = `(byte)(255 * GameScene.Game.DayTime)` | true |
| `Night` | `(15, 15, 15)` | true |
| `Twilight` | `(100, 100, 100)` | true |
| `Light` | `(255, 255, 255)` | true |

Abyss 毒(玩家 `Poison & PoisonType.Abyss`):`BackColour = (15,15,15)` 且 `Visible = false`。

**ShouldRenderLightLayer()**:玩家死亡或 Abyss → true;`MapInfo.Light == LightSetting.Light` → false;否则 BackColour 任一通道 < 255 → true。

**CheckTexture()**:UpdateAmbientLight → 不渲染则丢弃纹理;校验管线共享光纹理有效;`GetLightSignature()` 签名相同则跳过重绘(缓存)。

**OnClearTexture()** 顺序:
1. `user.Dead` → `Clear(RenderClearFlags.Target, Color.IndianRed, 0, 0)`(死亡红屏)。
2. `SetBlend(true, 1F, BlendMode.COLORFY)`(颜色混合模式画光)。
3. Abyss:`Clear(Color.Black)` → `scale = BaseLightSize + 4*LightScale` → 以屏幕中心 `DrawLight(..., Color.White)` → 恢复混合 → 播放 `user.CreateMagicEffect(MagicEffect.Abyss)` 并 Draw。
4. 对象光:遍历 `map.Objects`,`ShouldDrawObjectLight(ob, user)` = `ob.Light > 0 && (!ob.Dead || ob == user || ob.Race == ObjectType.Spell)`:

```
scale    = BaseLightSize + ob.Light * 2 * LightScale;
objectX  = (OffSetX + ob.X - user.X) * CellWidth  + PixelOffsetX + ob.MovingOffSet.X - user.MovingOffSet.X + CellWidth /2F - (lightSize.Width  * scale) / 2F;
objectY  = (OffSetY + ob.Y - user.Y) * CellHeight + PixelOffsetY + ob.MovingOffSet.Y - user.MovingOffSet.Y - (lightSize.Height * scale) / 2F;
DrawLight(lightTexture, lightSource, lightSize, objectX, objectY, scale, ob.LightColour);
```

5. 特效光:`ob.FrameLight > 0`:

```
scale = BaseLightSize + frameLight * 2 * LightScale / EffectLightScaleDivisor;
DrawLight(..., ob.DrawX + CellWidth/2F - ..., ob.DrawY + CellHeight/2F - ..., scale, ob.FrameLightColour);
```

6. 格光:范围 `User ± (OffSet + TileLightSearchPadding)`,`tile.Light == 0` 跳过;drawY **不带 +1**(行 1707);`scale = BaseLightSize + tile.Light * TileLightScaleMultiplier * LightScale`,白色。
7. 恢复原混合状态。

`DrawLight`:目标矩形 = `(topLeftX, topLeftY, lightSize.Width*scale, lightSize.Height*scale)`,`RenderingPipelineManager.DrawTexture(lightTexture, lightSource, destination, colour)`;宽高 ≤ 0 时跳过。

## 7.9 天气粒子(Client/Models/Particles/Weather/)

门控:`Config.DrawWeather`(默认 true)与 `Config.DrawParticles`(默认 **false**)。`UpdateWeather()` 清空 `ParticleEffects`,按 `MapInfo.Weather` 位标志(`[Flags] Weather:None=0, Rain=1, Snow=2, Fog=4, Lightning=8;组合:SnowFog=6, RainLightning=9, FogLightning=12, RainFogLightning=13`)用 `Activator.CreateInstance` 在屏幕中心 `(Size.Width/2, Size.Height/2)` 创建发射器。

纹理均出自 `LibraryFile.ProgUse`:509(雨)、500(雪)、550(雾)、540(闪电)。

**Rain / RainParticle**:

| 参数 | 值 |
|---|---|
| `MaxCount` | `int.MaxValue` |
| `SpawnFrequency` | 10ms |
| 纹理 | 509,Color.White |
| 出生点 | 80% 顶部 `(random(Width), 1)`;20% 右侧 `(Width, random(Height))` |
| 速度 | `(-1, 5)` |
| scale | `random(1,3)`,scaleRate 0 |
| 起始角 | 0.4F,角速度 0 |
| ttl | `random(500, 2000)` ms |
| fade | false |
| `useMovingOffset` | false |
| Completed | 速度置 0、保留(落到地面) |
| Updated | 速度==0 时更新间隔变 100ms,`TextureIndex++`,>514 移除(地面水花帧 510-514) |

**Snow / SnowParticle**:

| 参数 | 值 |
|---|---|
| `MaxCount` | 500 |
| `SpawnFrequency` | 20ms |
| 纹理 | 500,Color.White |
| 出生点 | 顶部 `(random(Width), 0)` |
| 速度 | `(random(-1,1), 1F)` |
| scale | `random.NextDouble() * 1.5F`,scaleRate 0 |
| 角速度 | 0.1F |
| ttl | `random(4000, 10000)` ms |
| fade | false |
| `useMovingOffset` | false |
| Completed | 速度置 0、`Fade=true, FadeRate=0.01F, AngularVelocity=0, ScaleRate=-0.01F`(落地消融) |

**Fog / FogParticle**:

| 参数 | 值 |
|---|---|
| `MaxCount` | 4 |
| `SpawnFrequency` | 0ms(立即) |
| 纹理 | 550,Color.DarkGray |
| scale | 4F |
| 速度 | `(1F, 0)` 向右飘 |
| 出生点 | 发射器位置;后续粒子 = 上一个 `Position.X - size.Width*scale`(左侧接续,形成循环贴图) |
| ttl | 1 小时 |
| 特殊 | `Updated` 首帧(UpdateCount==1)重排整链位置,补偿出生不同步 |

**Lightning / LightningParticle**:

| 参数 | 值 |
|---|---|
| `SpawnFrequency` | 随机 `random(1000, 5000)` ms(每次取随机) |
| `MaxCount` | 3 |
| 纹理 | 540,Color.White |
| 出生点 | 顶部 `(random(Width), 0)` |
| 速度 | `(0, 0)` |
| scale | `random(1, 4)` |
| ttl | `random(100, 200)` ms |
| fade | true,`FadeRate = 0.1F` |
| `useMovingOffset` | **true**(跟随屏幕抖动) |

## 7.10 MapInfo 相关字段(与渲染相关部分)

来源 `LibraryCore/SystemModels/MapInfo.cs`(DBObject):

| 字段 | 类型 | 渲染用途 |
|---|---|---|
| `FileName` | string | 身份键;`.map` 文件名 = `FileName + ".map"` |
| `Description` | string | 地图名 |
| `PlayerDescription` | string | 去除尾部 `\s\d+$` 的描述 |
| `ServerDescription` | string | `$"{FileName} - {Description}"` |
| `MiniMap` | int | 小地图贴图索引 |
| `Light` | LightSetting | 光照模式(见 7.8) |
| `Weather` | Weather(位标志) | 天气组合(见 7.9) |
| `Fight` | FightSetting(None/Safe/Fight) | 战斗模式 |
| `AllowRT` / `AllowTT` | bool | 允许回城/传送门 |
| `SkillDelay` | int | 技能延迟 |
| `CanHorse` | bool | 允许坐骑 |
| `CanAutoPath` | bool | 允许自动寻路 |
| `CanMine` | bool | 允许挖矿 |
| `CanMarriageRecall` | bool | 允许夫妻召回 |
| `AllowRecall` | bool | 允许召回 |
| `MinimumLevel` / `MaximumLevel` | int | 等级限制 |
| `Background` | int | 背景贴图索引(0=无,见 7.5) |
| `MonsterHealth` / `MonsterDamage` / `DropRate` / `ExperienceRate` / `GoldRate` | int | 倍率(另有 `Max*` 5 个上限字段) |
| `Expanded` | bool | 编辑器展开标志(默认 true,非渲染) |

---

 ---
# 第 8 章 Godot 客户端移植对照路线(逐包实现)

> 前提(已在前期完成):全部资源下载并通过 MD5 校验,按清单重排;客户端为 WinForms + SharpDX/Vortice D3D11,无法在 Linux/Apple Silicon 构建;`ServerCore`(net10.0,无头)在 Linux 本机 127.0.0.1:7000 原样运行,Godot 客户端经 TCP 直连(协议链路已用 ClientProbe 验证);`DevExpress 25.2.6` 仅阻塞 `Server`/`LibraryEditor`,与本方案无关。
> 本路线把第 1-7 章的数据逐条映射到 Godot 4.x(C#)实现,供移植时对照,不再需要读客户端源码。

## 8.1 总体策略

| 决策 | 内容 |
|---|---|
| 复用(不重写) | 服务端:`ServerCore` 独立进程零改动;客户端:`LibraryCore`(`BaseConnection` 网络层、`FrameSet`、`Globals` 常量、`MirDB`)、`WeMadeLibrary.cs`(.Zl)/`WTLLibrary.cs`(.WTL)/`Astc.cs`(ASTC 解码)格式读取器、`.map`/`.wav`/`System.db` 资源 |
| 重写(仅渲染/UI) | 坐标变换、帧动画、分层绘制、特效、光照、天气、背包/装备/魔法 UI |
| 语言 | Godot 4.x + C#(.NET 10,与 net10.0 项目直接引用) |
| 输入 | 键盘/鼠标移动、点击寻路(复用服务端寻路逻辑) |
| 数据来源 | 本文档第 1-7 章所有表格(已从源码全量提取,含 316 LibraryFile、294 MonsterImage、94 帧表、153 魔法/266 特效/191 音效、291 怪物 case、62 KROrder) |

Godot 侧关键对应:

| 客户端(D3D11) | Godot 对应 |
|---|---|
| `RenderingPipelineManager` / `PresentTexture` / `FlushSprite` | CanvasItem 绘制 + `RenderingServer`;单 CanvasLayer 自绘或逐节点 Sprite2D |
| `MirLibrary.Draw(frame, x, y, colour, offSet, opacity, ImageType, scale)` | `TextureRegion` 取帧 + `CanvasItem.draw_texture_rect` / Sprite2D 的 `frame`/`region` |
| `DrawBlend`(0.5F/0.2F 等混合率) | `modulate.a` + `CanvasItemMaterial(BlendMode)` |
| `SetBlend(true, 1F, BlendMode.LIGHTMAP)` | CanvasLayer `light_mask` + Light2D |
| DXControl 控件树(Location/Size/Parent) | Control 节点树(anchors/offsets) |
| `DXSoundManager.Play` | `AudioStreamPlayer`(.wav 直接导入) |

## 8.2 资源管线(Godot 侧一次性搭建)

1. **.Zl 库**:用 `WeMadeLibrary.cs`(LibraryEditor 版)解析。注意客户端启动时(Program.cs:48-55)遍历 `CEnvir.LibraryList`(314 条),**跳过缺失文件**;`Config.UseZlAtlasPages = true`(默认)会把库帧打包到图集页——Godot 侧直接按帧索引建 `Texture2D` 缓存即可,天然等价。库键语义:`LibraryFile` 枚举(316 成员,第 2 章)→ `LibraryList` 路径表(314 条,None 与 HorseS 无条目)。
2. **.map**:按第 7 章 7.2 的二进制布局写解析器(头部 22 字节 → Width/Height → 背景半分辨率层 → 每格 14 字节)。`MiddleImage/FrontImage` 已 +1,绘制时 -1。
3. **System.db**:SQLite 格式(服务端数据),用 `ServerLibrary` 的 DB 层读取 `MapInfo`、`ItemInfo`、`MonsterInfo`、`NPCInfo`、`MagicInfo` 等表。
4. **.wav**:Godot 原生导入;`DXSoundManager` 的音效名(191 个唯一名,第 6 章)映射到文件。

## 8.3 包级实现路线(按数据章节顺序)

### 包 A:坐标系统(第 1、7 章)— 先于一切

- 常量:`CellWidth=48, CellHeight=32, ManualHeightOffset=34`(1.1/7.1)。
- 屏幕变换(7.6 核心公式,注意 drawY 的 +1 格):
  ```
  drawX = (x - User.X + OffSetX) * 48 + PixelOffsetX - MovingOffSet.X - ShakeScreenOffset.X
  drawY = (y - User.Y + OffSetY + 1) * 32 + PixelOffsetY - MovingOffSet.Y - ShakeScreenOffset.Y
  ```
- 移动插值(1.2,MapObject.cs SetFrame):每方向 `±(int)(CellWidth*MoveDistance/FrameCount*(FrameCount-(frame+1)))`,Sum 模式变体;`MovingOffSet` 用于光照/绘制补偿。
- 逆变换(鼠标→格):`(mouse - PixelOffset) / Cell` 再按 `InRange(中心, 2)` 就近取方向(7.6 行 1263-1274)。
- 实现:一个 `IsometricGrid` 单例,输出格→屏幕坐标;所有渲染节点共用。

### 包 B:地图渲染(第 7 章)— 场景骨架

- .map 解析器(7.2)→ `Cell[,]` 结构(BackFile/BackImage、Middle/Front 文件与图像、动画编码、Light、Flag)。
- `KROrder`(7.3,62 条)把字节 → `LibraryFile` → 库路径(第 2 章表)。
- 三层绘制(7.4-7.7),按 `RenderY` 行排序(z 序 = 格 y + 行内绘制顺序):
  1. 背景层:半分辨率,只画偶数格 `(y%2==0 && x%2==0)`,`drawY` 不带 +1(7.7 段 1);
  2. 中层/前景静态贴图(7.7 段 2):只画格尺寸贴图,`drawY - CellHeight`;
  3. 主管线 DrawObjects(7.6):动画格 `index += Animation % count`、混合位 0x80、大贴图底边对齐 `drawY - s.Height`。
- 阻挡判定:`Cell.Flag` 或格上对象 `Blocking`(7.2)。
- 光照层(7.8):`LLayer` 常量(LightScale=0.02F、BaseLightSize=0.1F、TileLightScaleMultiplier=30F、EffectLightScaleDivisor=5F、TileLightSearchPadding=15)、`UpdateAmbientLight` 按 MapInfo.Light 切 BackColour(Default → 255×DayTime 灰阶;Night 15;Twilight 100;Light 255)、玩家死亡红屏(IndianRed)、Abyss 黑屏 + MagicEffect.Abyss。
  - Godot:`CanvasModulate`(环境光)+ `Light2D`(点光:对象光 `BaseLightSize + Light*2*0.02`,格光 `BaseLightSize + Light*30*0.02`,特效光 ÷5)。注意特效光位置用 `ob.DrawX/DrawY` 中心,对象光用格坐标 + MovingOffSet。
- 天气(7.9):`Config.DrawWeather`(true)默认;Rain/Snow/Fog/Lightning 参数表已全量;Godot 用 `CPUParticles2D`(纹理 ProgUse 509/500/550/540)复刻参数(出生区、速度、ttl、fade)。
- 完成标准:能加载任意 .map,角色在地图上移动、地形遮挡顺序正确、光照/天气与客户端一致。

### 包 C:玩家渲染(第 4 章)

- 常量(4.1):`FemaleOffSet=5000, AssassinOffSet=50000, RightHandOffSet=50, ArmourShapeOffSet(战法道 5000/刺客 3000), WeaponShapeOffSet=5000, HairTypeOffSet=5000`。
- 五张装备字典(4.2):ShieldList 4 / WeaponList 52 / HelmetList 30 / ArmourList 30 / CostumeList 6(全表在文档)。
- 帧公式(4.3,源码原样):
  ```
  HairFrame   = DrawFrame + (HairType-1)*HairTypeOffSet
  HelmetFrame = DrawFrame + ((HelmetShape-1)%10)*ArmourShapeOffSet + ArmourShift
  WeaponFrame = DrawFrame + (WeaponShape%10)*WeaponShapeOffSet
  ShieldFrame = DrawFrame + (ShieldShape%10)*ArmourShapeOffSet + ArmourShift
  ArmourFrame = DrawFrame + (CostumeShape>=0 ? CostumeShape%10 : ArmourShape%11)*ArmourShapeOffSet + ArmourShift
  ```
- ArmourShift 全表(仅刺客;Standing 0 / Walking+Running 1600 / Creep* 240 / Pushed 160 / Combat1 -400 / Combat2 0 / Combat3 0 / Combat4 80 / Combat5-7 400 / Combat8 720 / Combat9 -960 / Combat10 -480 / Combat11-13 -400 / Combat14 + DragonRepulse* 0 / Harvest 160 / Stance 160 / Struck -640 / Die -400 / Dead -400 / Horse* 80 / Fishing* 80 / Taming* 0;default 抛异常)。
- 分层绘制(4.5):马 → 背部武器/盾 → 身体 → 头 → 前部武器/盾(顺序固定)。
- 阴影(4.5/4.8):DrawShadow2 剪切变换 `Matrix3x2(1,0,-0.5,0.5, tx, ty)` + 半透明黑(0.5),`SetTextureFilter(Point)`;马影 0.5F。
- 血条(4.8):Interface 库帧 80 背景框、79 填充(`percent = clamp(HP/最大, 0,1)`,裁剪矩形);三条:off=59 Goldenrod、55 HP OrangeRed、51 MP DodgerBlue。
- `FrameSet.Players`(第 3 章)提供全部动画帧表(56 条目),`Combat1.Delays[1]=200ms`、`Combat2.Delays[3]=200ms` 覆写。
- Godot:每件装备一层 Sprite2D,帧号按公式算出后取库帧;z 序:玩家身位行内固定层序。

### 包 D:怪物渲染(第 5 章)

- `MonsterImage` 枚举(294 成员,全表 5.2)← 怪物 `Image` 值。
- 291-case 映射表(5.3):`Image → {库, BodyShape, 攻击/站立/死亡音效, 帧表覆写, Extra 分支}`;`DefaultMonster` 兜底。
- 帧公式:`BodyFrame = DrawFrame + (BodyShape % 10) * 1000`(BodyOffSet=1000;CastleFlag 覆写 100)。
- 特殊 case(5.4):Shinsu 双分支(Mon_10/0 vs Mon_9/9)、CastleFlag 变量 BodyShape、OmaMage→Mon_29+SDMob8 帧表、LobsterLord 三层绘制(+0/1000/2000,阴影三层 0.5F)、NewMob1 MonMagicEx20 发光叠加(+2000)、NewMob10 高度特例 `y -= CellHeight*4`、ChestnutTree `y -= CellHeight`、事件覆写(EasterEvent→Mon_30/4)、`UpdateLibraries default → Mon_1/0`。
- 每怪动作/音效/特效 45 行表(5.5)。
- Godot:按表生成 `MonsterInfo → 帧表 + 库 + 特效` 配置;渲染循环与玩家共用 BodyFrame 公式。

### 包 E:NPC / 物品 / 地面法术(第 6 章)

- NPC:`BodyFrame = DrawFrame + BodyShape*100`(BodyOffSet=100,BodyShape=NPCInfo.Image);20 个单帧特殊 Image 值(6.1 表);任务图标 QuestEffect 表(QuestType → 起始帧)。
- 物品:Ground 掉落渲染、Rarity 发光(Common+属性/ Superior/ Elite → ProgUse 帧 110/100/120,Blend,BlendRate=0.5F)、名字颜色、`DrawFocus` 焦点环、货币图标查表。
- 地面法术残留 SpellObject(6.3 表,10 特效)。
- 常驻/受击特效 CreateMagicEffect(6.4,28 case)。
- 网络包特效映射(6.5):ObjectEffect 21 条 / ObjectProjectile 4 条 / MapEffect 10 条 / ObjectLeveled / ObjectRevive——在线方案下这些**原样**对应 Godot 端的网络包处理方法 `Process(S.ObjectEffect p)` / `Process(S.ObjectProjectile p)` 等(继承 `BaseConnection`,同原版 CConnection 模式),表内帧号/库/颜色参数直接使用,零改写。
- Attack/Struck 元素特效(6.6):攻击 12 魔法、受击 8 元素(帧号 930+元素序 ×20,MagicEx 库,光强 10→30)。

### 包 F:魔法特效总表(第 6 章 6.7)— 数据驱动

- 153 魔法 / 210 上下文 / 266 特效 / 191 音效引用全表已生成。
- 每特效格式:`(起始帧, 帧数, 延时ms, 库, 光强起→光强止, 颜色) {对象初始化器属性}`。
- MirEffect 原语(1.4)/ MirProjectile(1.5)/ MirLineEffect(1.6,LinkLength=30、Gravity=0.05、SpringStrength=0.15、Damping=0.9、AnchorOffsetY=50、投掷弧 easeOutCubic/Quad)。
- Godot:表驱动生成 `AnimationPlayer` 或逐帧 Sprite2D + Tween;投射物 = `Area2D` 直线/抛物线;链状特效 = 节点链物理模拟或 Line2D。
- 注意:**重复魔法名按"施放 vs 命中"两上下文分别实现**(96 个单上下文、57 个双上下文)。

### 包 G:游戏循环与 UI(运行指南已覆盖 UI 常量)

- `UserLocation/ObjectMoved → ObjectAttack → 背包 → 魔法` 顺序接入;每包完成后用原版截图对比渲染结果(见 8.5)。
- `Config` 渲染开关逐项对应(7.10/Config.cs):DrawEffects/DrawParticles/DrawWeather/ShowTargetOutline/ShowItemNames/ShowMonsterNames/ShowPlayerNames/ShowDamageNumbers 等。
- 渲染管线解耦:客户端已把绘制收敛到 `RenderingPipelineManager.RenderFrame(drawScene)`(CEnvir.cs)——Godot 侧等价为单帧渲染调度。

## 8.4 数据导出建议

把本文档表格落成 JSON(或 Godot 资源),避免手写配置:

| 数据 | 来源章节 | 导出文件 |
|---|---|---|
| LibraryFile → 路径 | 2 | library.json(316/314) |
| KROrder | 7.3 | krorder.json(62) |
| FrameSet 帧表 | 3 | frameset.json(94 表/560 条目) |
| MonsterImage → 怪物配置 | 5.2/5.3 | monster.json(294/291) |
| 怪物特效 | 5.5 | monster_effects.json(45) |
| 魔法特效 | 6.7 | magic.json(153/210/266) |
| 玩家装备字典 | 4.2 | player_libs.json |
| 天气/光照常量 | 7.8/7.9 | 直接硬编码 |

## 8.5 里程碑与验证

| 里程碑 | 内容 | 验证 |
|---|---|---|
| M0 | Godot 项目 + ServerLibrary/LibraryCore 引用编译通过;System.db 可读 | `dotnet build`;控制台打印 MapInfo 条数 |
| M1 | 包 A+B:地图加载 + 坐标 + 地形绘制 + 移动 | 与原版截图逐帧对比地形层 |
| M2 | 包 C:玩家外观(装备换装/动画) | 原版 vs Godot 同装备帧对比 |
| M3 | 包 D+E:怪物/NPC/物品渲染 | 同场景怪物图鉴截图对比 |
| M4 | 包 F:魔法/特效全表 | 施放每技能,特效帧号/颜色对照文档 |
| M5 | UI + 背包 + 战斗循环 | 完整流程可玩(连本机服务端) |

所有帧号、库名、音效名以本文档表格为准;发现不一致时,先查对应源码行号(文中已标注),再改文档与实现。

---

# 附录 A 数据一致性核对清单(移植验收用)

- [ ] LibraryFile 枚举 316 成员 ↔ LibraryList 314 路径(缺 None/HorseS)
- [ ] MonsterImage 294 成员 ↔ switch 291 case(缺 Shinsu1/SDMob8)
- [ ] FrameSet 94 表(ShinsuBig/LobsterSpawn 声明未初始化;Players Delays 覆写)
- [ ] PlayerObject 5 字典键 ↔ 4.2 表
- [ ] SetAction 210 case / 153 唯一魔法 ↔ 6.7 表
- [ ] KROrder 62 键(0-71 缺号)↔ 7.3 表
- [ ] 元素颜色 11 常量(1.8)↔ Globals.cs:47-59
- [ ] DrawHealth 帧(Interface 库 79/80 + off 59/55/51)↔ 4.8
