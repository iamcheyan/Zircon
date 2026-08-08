# Godot 音效一致性迁移清单

状态：盘点进行中（2026-08-08）  
目标：让 Godot 客户端在同一游戏行为、同一动画/事件时机下，播放原版对应的音效；不能用一个“通用音效”替代原版已经区分的音效。

## 1. 盘点结论

原版的音效不是按“一个技能一个声音”简单分布，而是由三层组成：

1. `SoundIndex`：行为语义索引，位于 `LibraryCore/Enum.cs` 的 `#region Sound`。
2. `DXSoundManager.SoundList`：把索引映射到 `Debug/Client/Sound/*.wav`，并声明类别（Music、Player、System、Magic、Monster）及是否循环。
3. 行为触发点：玩家/怪物动画帧、服务器对象事件、技能/持续区域效果、UI 控件点击、地图切换和场景生命周期。

当前原版资源目录盘点：

| 项目 | 结果 |
|---|---:|
| `Debug/Client/Sound` 文件总数 | 3166 |
| 原版文件格式 | WAV 为基准；同名 OGG 为压缩副本 |
| 原版 `DXSoundManager` 显式映射行 | 727 |
| `SoundIndex` 枚举条目（含别名/保留项） | 742 |
| Godot 当前显式动作映射 | 约 29 个索引 |
| Godot 当前非动作音效入口 | 未形成统一音效服务；大量行为没有接入 |

“资源存在但未播放”是当前差异的主要原因，不是资源本身不足。

## 2. 原版分类清单

### 2.1 地图/场景音乐

来源：`DXSoundManager.cs` 的音乐区，以及 `MapInfo.Music`。包括登录/选角音乐和地图音乐：

`Opening.wav`、`Main.wav`、`Ending.wav`、`SelChr.wav`，以及 `B000.wav`、`B2.wav`、`B8.wav`、`B009D.wav`、`B009N.wav`、`B0014D.wav`、`B0014N.wav`、`B100.wav`、`B122.wav`、`B300.wav`、`B400.wav`、`B14001.wav`、`BD00.wav`、`BD01.wav`、`BD02.wav`、`BD041.wav`、`BD042.wav`、`BD50.wav`、`BD60.wav`、`BD70.wav`、`BD99.wav`、`BD100.wav`、`BD101.wav`、`BD210.wav`、`BD211.wav`、`BDUnderseaCave.wav`、`BDUnderseaCaveBoss.wav`、`D3101.wav`、`D3102.wav`、`D3400.wav`、`Dungeon_1.wav`、`Dungeon_2.wav`、`ID1_001.wav`、`ID1_002.wav`、`ID1_003.wav`、`TS001.wav`、`TS002.wav`、`TS003.wav`。

要求：地图进入/离开时停止旧音乐、播放新音乐；场景音乐循环；同一地图不重复重启。

### 2.2 玩家移动、攻击、受击和生活行为

原版索引与文件：

| 行为 | `SoundIndex` | 文件 |
|---|---|---|
| 脚步随机 | `Foot1..Foot4` | `1.wav..4.wav` |
| 骑马行走/奔跑 | `HorseWalk1/2`, `HorseRun` | `33.wav..35.wav` |
| 钓鱼 | `FishingCast/Bob/Reel` | `84.wav..86.wav` |
| 挖矿 | `MiningHit/MiningStruck` | `125.wav`, `126.wav` |
| 玩家受击 | `MaleStruck/FemaleStruck` | `138.wav`, `139.wav` |
| 玩家死亡 | `MaleDie/FemaleDie` | `144.wav`, `145.wav` |
| 通用受击层 | `GenericStruckPlayer` | `61.wav` |
| 徒手/武器攻击 | `FistSwing`, `WoodSwing`, `IronSwordSwing`, `ShortSwordSwing`, `AxeSwing`, `ClubSwing`, `WandSwing`, `DaggerSwing` | `50.wav..57.wav`（按索引映射） |
| 刺客特殊攻击 | `GlaiveAttack`, `ClawAttack` | `63.wav`, `64.wav` |

攻击音效按武器外形/职业选择，不能只按攻击动画选择。播放时机是原版 `PlayerObject`/`MapObject` 的攻击帧，而不是收到网络包的任意时刻。

### 2.3 系统/UI/物品/任务音效

| 行为组 | 索引 | 原版文件 |
|---|---|---|
| 按钮 | `ButtonA/B/C` | `103.wav`, `104.wav`, `105.wav` |
| 选角 | `SelectWarrior/Wizard/Taoist/Assassin` 的男女项 | `JMCre.wav`、`JWCre.wav`、`SMCre.wav`、`SWCre.wav`、`DMCre.wav`、`DWCre.wav`、`AMCre.wav`、`AWCre.wav` |
| 传送 | `TeleportIn/TeleportOut` | `109.wav`, `110.wav` |
| 物品 | `ItemPotion/Weapon/Armour/Ring/Bracelet/Necklace/Helmet/Shoes/Default` | `108.wav`、`111.wav..118.wav` |
| 金币 | `GoldPickUp/GoldGained` | `120.wav`, `122.wav` |
| 小游戏 | `RollDice/RollYut` | `dice_roll.wav`, `yut_sticks.wav` |
| 任务 | `QuestTake/QuestComplete` | `Qtake.wav`, `Qcomp.wav` |
| 宝石 | `GemStart/GemCombine` | `Sopen.wav`, `Scombine.wav` |

原版 UI 控件把 `SoundIndex` 作为控件属性，在鼠标按下/释放等统一事件中触发；迁移时应保留这一机制，不能只给少数窗口手工补播放。

### 2.4 技能音效

技能索引位于 `SoundIndex` 的 `#region Magics`，按生命周期拆成 `Start`、`Travel`、`End`、`Duration`：

- 战士/刺客：`Slaying`、`HalfMoon`、`FlamingSword`、`DragonRise`、`BladeStorm`、`DefensiveBlow`、`DestructiveSurge`、`Assault`、`SwiftBlade`、`SeismicSlam`、`HundredFist`、`OffensiveBlow` 等。
- 法师投射物：火球、雷电、冰弹、风弹、强化火球、强化冰弹等，均分别有起手/飞行/命中声音。
- 法师区域/持续：`FireWall`、`FireStorm`、`LightningWave`、`FrozenEarth`、`IceStorm`、`TempestDuration` 等；其中持续声音在原版是循环音效，结束时必须 `Stop`。
- 道士/辅助：治疗、护盾、隐身、群体隐身、召唤、复活、净化、束缚、神兽等，按 Start/Travel/End 触发。
- 刺客/扩展技能：`FullBloom`、`WhiteLotus`、`RedLotus`、`SweetBrier`（男女分支）、`Cloak`、`WraithGrip`、`HellFire`、`Karma`、`TheNewBeginning` 等。
- 新增技能编号：`37400.wav`、`37410.wav`、`37440.wav`、`37450.wav`、`37460.wav`、`37470.wav`、`37510.wav`、`37520.wav`、`40300.wav..40370.wav`。

完整的索引→文件逐行权威映射仍以 `Client/Envir/DXSoundManager.cs` 为准；实现阶段将把该映射搬入 Godot 的数据表，并以脚本审计每个索引的文件存在性。

### 2.5 怪物音效

原版为每个怪物分别提供 `Attack`、`Struck`、`Die`，部分怪物还有 `Appear` 或 `Attack2/Attack3`。`SoundIndex` 中已覆盖鸡、猪、牛、鹿、羊、各类蜘蛛/蝎子/骷髅、祖玛、努玛、冰宫、神兽、龙、守卫、首领等完整怪物集合；`DXSoundManager` 使用怪物外形编号作为文件前缀，常见格式为：

`<monster-shape>-2.wav` = Attack，`<monster-shape>-4.wav` = Struck，`<monster-shape>-5.wav` = Die，`<monster-shape>-0.wav` = Appear（存在时）。

不能用 `GenericStruckMonster` 覆盖所有怪物。必须从对象的怪物类型/外形取得具体索引，并在 `ObjectRenderer` 的攻击、受击、死亡动画帧触发；没有专用声音时才按原版规则回退通用声音。`WhiteTiger*` 在原版枚举明确标记为缺失，属于已知例外而不是 Godot 漏移。

## 3. 触发分布与迁移映射

| 原版触发层 | 原版位置 | Godot 目标入口 | 验收重点 |
|---|---|---|---|
| 场景音乐 | `Client/Scenes/Views/MapControl.cs`、登录/选角场景 | `AudioService` + 场景生命周期 | 循环、切图停止旧曲、音量分类 |
| UI 控件 | `Client/Controls/DXControl.cs` | Godot 控件基类/统一点击回调 | 所有声明了 `Sound` 的控件都可触发 |
| 玩家动画 | `Client/Models/PlayerObject.cs`、`Client/Models/UserObject.cs` | `PlayerRenderer.FrameChanged` | 帧号、随机脚步、武器映射、男女受击/死亡 |
| 技能动画 | `Client/Models/SpellObject.cs`、技能对象 | `GameScene` 的 spell/projectile/impact 生命周期 | Start/Travel/End/Duration 不合并 |
| 怪物动画 | 原版对象/怪物模型及 `DXSoundManager` Monster 区 | `ObjectRenderer` / `MapObjectNode` | 具体怪物 Attack/Struck/Die/Appear |
| 网络对象事件 | `ObjectAttack/ObjectRangeAttack/ObjectStruck/ObjectDied` | `GameScene` 对应事件处理 | 不因网络包重复播放；事件与动画只绑定一次 |
| 物品/任务/小游戏 | 各原版 Dialog/ItemCell | Godot 对应控件 | 保持原版播放时机和声音索引 |

## 4. 当前 Godot 缺口（盘点时）

- `GameScene.OnPlayerSoundCue` 只处理脚步、骑马、钓鱼、挖矿、玩家受击/死亡、武器挥击和少数技能。
- `PlayerRenderer` 已有玩家动画帧 Cue，但没有怪物专用 Cue；`ObjectRenderer` 目前只有动画播放，没有完整的怪物音效选择。
- 技能只在少数网络/动画分支播放 `AssaultStart`、`DestructiveSurge`、`OffensiveBlow` 等，绝大多数 Start/Travel/End/Duration 索引未接入。
- UI 没有统一的 `SoundIndex` 播放服务，原版 `DXControl.Sound` 的覆盖范围没有迁移。
- 地图音乐、登录音乐、选角音乐尚未统一到 Godot 音效服务。
- 当前动作加载器写死 `res://../Debug/Client/Sound`，且是按需的局部 switch；需要改为统一索引表、缓存、类别音量和循环控制。

## 5. 实施顺序与完成标准

1. 从 `DXSoundManager.cs` 建立完整 `SoundIndex → 文件/类别/循环` 表，并检查每个文件在 `Debug/Client/Sound` 存在。
2. 实现一个统一 Godot 音效服务：一次加载、缓存、播放、停止、循环、分类音量、缺失文件诊断；不改变原始 WAV 内容。
3. 接入场景音乐和 UI 控件音效。
4. 完成玩家与怪物动画帧音效，包括具体怪物选择、通用回退和特殊 Attack2/3/Appear。
5. 为每个技能建立 `MagicType/SpellEffect/Effect` 的 Start、Travel、Impact、End、Duration 触发表。
6. 用静态脚本比较：原版显式映射、Godot 映射、Godot 触发点、资源文件；任何未解释差异都留在本清单中。
7. 运行 headless 场景和可操作战斗场景，记录每类事件的播放日志；重点验证“别人攻击别人”“怪物攻击/受击/死亡”“同一持续效果停止”和地图切换。

完成条件：映射差异为零，或每个差异都有原版明确 TODO/资源缺失说明；所有已实现的行为均能从事件到 `SoundIndex` 再到原版文件闭环验证。

## 6. 证据索引

- 原版语义枚举：`LibraryCore/Enum.cs`，`SoundIndex`。
- 原版权威映射与类别/循环：`Client/Envir/DXSoundManager.cs`。
- 原版玩家触发：`Client/Models/PlayerObject.cs`、`Client/Models/UserObject.cs`、`Client/Models/SpellObject.cs`。
- 原版 UI 触发：`Client/Controls/DXControl.cs` 及各 Dialog/ItemCell。
- Godot 当前动作加载：`GodotClient/Scripts/GameScene.cs` 的 `OnPlayerSoundCue`。
- Godot 当前玩家动画 Cue：`GodotClient/Scripts/PlayerRenderer.cs`。
- 原版音频资产：`Debug/Client/Sound/`。

## 7. 盘点审计结果（最终静态/资源复核）

`SoundCatalog` 已由 `DXSoundManager.SoundList` 自动生成 724 条唯一映射。文件存在性审计后，原版映射中只有以下问题需要保留为明确例外：

- 原版映射把 `FlashOfLightEnd` 写成 `M123-3-1.wav`，但资源目录实际只有 `M123-3.wav`；Godot 已按实际原版资源修正为 `M123-3.wav`，并保留该差异记录。
- `SoundIndex` 中的 `B009D/B009N/B0014D/B0014N/B122/B300/B14001/BD100/BD101/BD211/D3101/D3102/D3400/Dungeon_1/Dungeon_2/ID1_001/ID1_002/ID1_003/TS001/TS002/TS003` 没有对应文件出现在当前 `Debug/Client/Sound` 资源集，因此不加入可播放表；地图数据若引用这些索引，应记录为资源缺失。
- `WhiteTigerAttack/Struck/Die` 在 `SoundIndex` 中明确标记为原版缺失。
- `ChaosKnightStruck` 在原版 `DXSoundManager` 中是注释掉的映射。
- `UmaMaceInfidel*`、`IcySpiritGeneral*` 等索引在原版枚举/怪物逻辑中存在，但没有有效的 `DXSoundManager` 文件映射；迁移时必须按原版行为回退或标为缺失，不能误用相邻怪物声音。
- `WraithGripEnd`、`RakeStart`、`DanceOfSwallowsEnd`、`DragonRepulseStart` 等技能索引在原版枚举中存在，但当前原版音效表没有有效文件映射，列入待核查而非假设存在。

第二轮行为引用审计（排除原版资源表/枚举声明，只比较实际客户端代码引用）结果：原版触发引用 736 个索引，Godot 触发引用 750 个索引；原版中 Godot 没有直接引用的只有 `Chain`、`Containment`、`LavaStrikeEnd`、`ChaosKnightStruck` 和 `WhiteTigerAttack/Struck/Die`。其中前四项在原版触发代码中是注释/未实现项，白虎三项在原版枚举中明确标记为缺失音效。其余原版实际引用已在 Godot 的统一服务、技能事件、怪物动画、UI 控件或场景生命周期中覆盖。

最终运行证据：

- `dotnet build GodotClient/ZirconClient.csproj --no-restore`：0 warnings、0 errors。
- `MapTestScene --action-audit`：`[SoundAudit] valid=666/666`，`[SoundAudit] PASS catalog=724 files=666`；动作序列审计全部 PASS。
- FireBounce 的 `GreaterFireBallEnd` 已从投射物创建时改为投射物 `CompleteAction` 时播放，与原版 `MirProjectile.CompleteAction` 一致。
- `ChainOfFireExplode` 不再在事件到达时提前播放，改为效果第 8 帧播放，与原版帧回调一致。

这些例外是“原版本身没有可用声音”的证据项；除 `M123-3-1.wav` 外，不应被误报成 Godot 资源漏拷贝。
