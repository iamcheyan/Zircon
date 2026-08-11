# 战士被动技能效果分析报告

> [!NOTE]
> 分析对象：截图中展示的 7 个战士被动技能（全部 Level 3）。
> 本文所有结论均来自对仓库源码的逐行核对，以及对运行库 `System.db` 中 `MagicInfo` 表的**实测读取**（用 `Tools/MagicInfoDump` 工具直接读出 Power 字段，非推测）。
> 上一版报告的核心诊断"DB 里 Power 全为 0"已被实测推翻，本文为更正版本。

---

## 总结

| # | 技能名称 | 枚举值 | 服务端实现 | 实际是否生效 | 问题说明 |
|---|---------|--------|-----------|------------|---------|
| 1 | **Swordsmanship** (剑术) | `MagicType.Swordsmanship = 100` | `GetPassiveStats` → `Stat.Accuracy` | ✅ 生效（+10 命中@Lv3） | 静默，无任何反馈 |
| 2 | **Potion Mastery** (药剂精通) | `MagicType.PotionMastery = 101` | `PlayerObject` 喝药逻辑 | ✅ 生效（+25%@Lv3） | 仅喝药时触发，静默 |
| 3 | **Slaying** (杀戮) | `MagicType.Slaying = 102` | `GetPassiveStats` + `AttackCast` | ✅ 生效（被动 +6DC/+6命中，暴击 +15） | 暴击有特效，被动静默 |
| 4 | **Defensive Mastery** (防御精通) | `MagicType.DefensiveMastery = 127` | `GetPassiveStats` → `Stat.DefensiveMastery`；`GetAC()` 概率满防 | ✅ 生效（约 20% 满防@Lv3） | **部分减伤无触发反馈**，玩家看不见 |
| 5 | **Advanced Defiance** (高级抗性 / AugmentDefiance) | `MagicType.AugmentDefiance = 120` | augment，本身无独立效果 | ⚠️ 设计如此 | 必须配合 Defiance 才生效 |
| 6 | **Magic Immunity** (魔法免疫) | `MagicType.MagicImmunity = 129` | `PlayerObject.Attacked` 减伤 | ⚠️ 生效但太弱（2–7%@Lv3）且**无反馈** | 部分减伤不置任何标志，玩家完全看不见 |
| 7 | **Physical Immunity** (物理免疫) | `MagicType.PhysicalImmunity = 128` | `PlayerObject.Attacked` 减伤 | ⚠️ 生效但太弱（2–7%@Lv3）且**无反馈** | 同上 |

**核心结论（更正）**：这 7 个技能在代码层面**都有实现且 Power 值已配置非零**，并不是"没有效果"。玩家感觉"没生效"的真正原因是：

1. **Godot 客户端缺失 Miss/Block 飘字**（回归 bug）：旧版客户端会画 "Miss"/"Block"/暴击图标，Godot 的 `OnHealthChanged` 在 `miss||block` 时直接跳过飘字，`DamagePopupNode` 也没有 miss/block 分支。导致闪避、格挡、免疫把伤害减到 0 时——玩家在 Godot 里看不到任何反馈。
2. **免疫/防御精通的"部分减伤"在服务端就没有任何信号**：`PlayerObject.Attacked` 里免疫只在把伤害减到 0 时才置 `DisplayMiss`，部分减伤（2–7%）不置任何标志；`GetAC()` 的防御精通满防也是静默的。所以即便生效，两客户端都看不见。
3. **免疫数值偏弱**：`MinBase=1/MaxBase=6/MinLevel=1/MaxLevel=1`，Lv3 约 2–7%，且几乎不随等级成长。

> [!IMPORTANT]
> 上一版报告说"Defensive Mastery、Magic Immunity、Physical Immunity、Potion Mastery 的 .cs 是空壳（只有构造函数）"——这是错的：
> - **DefensiveMastery 不是空壳**，它 override 了 `GetPassiveStats()`（`DefensiveMastery.cs:16-24`）。
> - 真正的空壳是 MagicImmunity、PhysicalImmunity、PotionMastery，以及文档漏掉的 **AdvancedPotionMastery**。
> - "Swordsmanship ✅ 正常"与"PotionMastery 依赖 Power 会失效"在同一份报告里自相矛盾；实测两者 Power 均非零，都生效。

---

## 一、实测数据库 Power 值（反驳"Power=0"假设）

用 `Tools/MagicInfoDump`（只读 .NET 工具，通过 `MirDB.Session` 加载 `System.db`）直接读出 7 个技能的 `MagicInfo` Power 字段：

| 技能 | MinBasePower | MaxBasePower | MinLevelPower | MaxLevelPower | NeedLevel1 |
|---|---|---|---|---|---|
| Swordsmanship | 0 | 0 | 10 | 10 | 7 |
| PotionMastery | 10 | 10 | 15 | 15 | 12 |
| Slaying | 8 | 8 | 7 | 7 | 14 |
| DefensiveMastery | 1 | 1 | 1 | 1 | 70 |
| PhysicalImmunity | 1 | 6 | 1 | 1 | 80 |
| MagicImmunity | 1 | 6 | 1 | 1 | 80 |
| AugmentDefiance | 0 | 0 | 0 | 0 | 80 |

> **结论：除 AugmentDefiance（设计如此，它是 augment，不用 GetPower）外，其余 6 个技能的 Power 字段全部非零且已正确配置。** 上一版"Power 全为 0 导致失效"的假设不成立。

`GetPower()` 公式（`UserMagic.cs:179-188`）：
```csharp
int min = Info.MinBasePower + Level * Info.MinLevelPower / 3;
int max = Info.MaxBasePower + Level * Info.MaxLevelPower / 3;
if (min < 0) min = 0;
if (min >= max) return min;
return SEnvir.Random.Next(min, max + 1);
```

Level 3 时（整数除法 `3*N/3 = N`）的实际效果：

| 技能 | GetPower@Lv3 | 实际效果 |
|---|---|---|
| Swordsmanship | 10 | `Stat.Accuracy += 10` |
| PotionMastery | 25 | 药水 HP/MP/FP `+25%` |
| Slaying | 15（暴击额外伤害） | 被动 `Accuracy/MinDC/MaxDC += Level*2 = 6`（用 Level，不靠 Power） |
| DefensiveMastery | 2 | `Stat.DefensiveMastery = 2` → `GetAC()` 里 `Random.Next(10) < 2` ≈ **20% 概率满防** |
| PhysicalImmunity | 2–7（随机） | 物理伤害 `-= 2~7%` |
| MagicImmunity | 2–7（随机） | 魔法伤害 `-= 2~7%` |
| AugmentDefiance | 0 | augment，无独立效果 |

---

## 二、服务端集成点（源码行号）

- **被动属性加成**：`PlayerObject.cs:2256-2263`，`RefreshStats` 内遍历 `MagicObjects`，`if (magicObject.CanUseMagic()) Stats.Add(magicObject.GetPassiveStats());`。Swordsmanship/DefensiveMastery/Slaying 的被动从这里生效。
- **`CanUseMagic()` 门控**：`MagicObject.cs:56-70`，非装备类技能要求 `Player.Level >= Magic.Info.NeedLevel1`。免疫/防御精通 NeedLevel1=70/80，低等级角色（例如 GM 直接发的技能）即便学到了也不会触发 `GetMagic` 与 `GetPassiveStats`。
- **`GetMagic<T>`**：`PlayerObject.cs:16032-16044`，内部也校验 `CanUseMagic()`，所以免疫/药剂精通这类"触发型"效果在等级不足时同样不生效。
- **药剂精通**：`PlayerObject.cs:6346-6364`，喝药时 `health += health * potionMastery.Magic.GetPower() / 100`。`PotionMastery.cs` 本身是空壳，效果在 PlayerObject。`AdvancedPotionMastery`（`PlayerObject.cs:6356`）同款，文档原本漏列。
- **魔法免疫**：`PlayerObject.cs:15624-15635`，受元素攻击时 `power -= power * magicImmunity.Magic.GetPower() / 100`；减到 0 才置 `DisplayMiss`，**部分减伤不置任何标志**。
- **物理免疫**：`PlayerObject.cs:15645-15656`，同上（`element == Element.None` 分支）。
- **防御精通**：`DefensiveMastery.cs:16-24` 提供 `Stat.DefensiveMastery`；`MapObject.cs:1819-1836` `GetAC()` 里 `if (defensiveMastery > 0) { if (defensiveMastery >= 10) return max; if (Random.Next(10) < defensiveMastery) return max; }`——概率取最大 AC，**静默**。
- **Advanced Defiance**：`AugmentDefiance.cs` 空壳；`Defiance.cs:53-62` 里 `GetAugmentedSkill(MagicType.AugmentDefiance)` 只在玩家施放 Defiance 时增强其效果（减少攻击惩罚、延长持续时间）。无 Defiance 则完全不起作用——设计如此。

---

## 三、旧版客户端对比（关键回归发现）

服务端广播伤害结果在 `MapObject.cs:168-181`：`S.HealthChanged { ObjectID, Change, Critical, Miss, Block }`。

**旧版客户端**（`Client/`）会渲染 Miss/Block/暴击图标：
- `Client/Envir/CConnection.cs:1863-1875` `Process(S.HealthChanged p)`：`ob.DamageList.Add(new DamageInfo { Value = p.Change, Block = p.Block, Critical = p.Critical, Miss = p.Miss });`
- `Client/Models/DamageInfo.cs:270-298` `Draw()`：`Value==0 && Miss` → 画 Interface 第 76 帧（"Miss"字样）；`Value==0 && Block` → 画第 77 帧（"格挡"）；有伤害时画数字，`Critical` 叠第 78 帧。

**Godot 客户端**缺失这部分：
- `GodotClient/Scripts/GameScene.cs:4016` `OnHealthChanged(...)`：**只有 `!miss && !block` 时才 `SpawnDamagePopup`**，miss/block 时什么都不画。
- `GodotClient/Scripts/DamagePopupNode.cs:13` `Setup(int value, bool critical)`：只接受数字与 critical，**没有 miss/block 分支**。

→ 结论：Godot 端 Miss/Block 飘字完全没实现，是相对旧版客户端的**回归**。Evasion（闪避）、BlockChance（格挡率）、免疫把伤害减到 0——这些情况在 Godot 里全部"无声无息"，是"感觉没生效"的直接来源。

---

## 四、修复方案

### Fix 1 — Godot 客户端：补回 Miss/Block/暴击飘字（回归修复，必做）

- `GodotClient/Scripts/DamagePopupNode.cs`：`Setup` 扩展为接受 `miss/block/critical/resist`，按种类渲染——Miss 画 "Miss"、Block 画 "Block"（白色文字），暴击黄色数字。
- `GodotClient/Scripts/GameScene.cs:4016` `OnHealthChanged`：无论 `miss/block` 与否都 `SpawnDamagePopup`（扣血/受击动作仍只在 `!miss && !block` 时执行，这部分已有逻辑保持不变）。
- 无需协议变更，纯客户端。

### Fix 2 — 服务端 + 两客户端：给免疫/防御精通的"部分减伤"加触发反馈（核心修复）

服务端已有但**未使用**的字段 `MapObject.DisplayResist`（`MapObject.cs:68`，全仓库无任何读写—— vestigial）。复用它：

1. `LibraryCore/Network/ServerPackets.cs:505-512` `HealthChanged` 加 `public bool Resist;`。
2. `ServerLibrary/Models/MapObject.cs:168-181` 广播块：条件加 `|| DisplayResist`，包里 `Resist = DisplayResist`，复位 `DisplayResist = false`。
3. `ServerLibrary/Models/PlayerObject.cs:15624-15635`（MagicImmunity）与 `:15645-15656`（PhysicalImmunity）：部分减伤（`power > 0`）时置 `DisplayResist = true`；减到 0 仍走 `DisplayMiss`。
4. `ServerLibrary/Models/MapObject.cs:1828-1833`（`GetAC` 防御精通满防分支）：触发时置 `DisplayResist = true`。`GetAC()` 全部调用点都是受击方减伤计算（`MonsterObject.cs:1731`、`PlayerObject.cs:15199` 及刺客技能），在此置位安全。
5. Godot：`Network/ServerConnection.cs:195` 事件签名加 `bool resist`，`:672` 传递 `p.Resist`；`GameScene.OnHealthChanged` 与 `DamagePopupNode` 渲染 "Resist"/"吸收"（青色数字或文字）。
6. 旧版 C# 客户端：`CConnection.cs:1871` 加 `Resist = p.Resist`；`DamageInfo.cs` 加 `Resist` 字段，`Draw()` 里 `Resist && Value<0` 时用白色数字（复用现有 Interface 第 75 帧，无需新素材）。

> 说明：`DisplayResist` 复用既有 vestigial 字段，服务端对象侧无需新增字段；协议仅加一个 bool，两客户端同步更新即可保持线格式一致。

### Fix 3 — 数值平衡（可选，需策划决定）

免疫 2–7% 且 `MaxLevelPower=1` 几乎不成长，偏弱。若设计上希望有感，可在 Server 管理端 MagicInfo 编辑器（`Server/Views/MagicInfoView.cs`）调高 MagicImmunity/PhysicalImmunity 的 `MinBasePower/MaxBasePower/MinLevelPower/MaxLevelPower`，例如改为 `MinBase=5, MaxBase=10, MinLevel=2, MaxLevel=2`（满级约 10–20% 减伤）。这是平衡决策，不是代码 bug，故不在本次代码改动中自动应用。

---

## 五、实施清单（文件:行）

| 改动 | 文件 | 位置 |
|---|---|---|
| Fix 2 协议 | `LibraryCore/Network/ServerPackets.cs` | `HealthChanged` :505-512 加 `Resist` |
| Fix 2 广播 | `ServerLibrary/Models/MapObject.cs` | `ProcessHPMP` :168-181 接入 `DisplayResist` |
| Fix 2 防御精通 | `ServerLibrary/Models/MapObject.cs` | `GetAC` :1828-1833 置 `DisplayResist` |
| Fix 2 免疫 | `ServerLibrary/Models/PlayerObject.cs` | MagicImmunity :15624-15635、PhysicalImmunity :15645-15656 |
| Fix 1+2 Godot 飘字 | `GodotClient/Scripts/DamagePopupNode.cs` | `Setup` 全方法 |
| Fix 1+2 Godot 处理 | `GodotClient/Scripts/GameScene.cs` | `OnHealthChanged` :4016、`SpawnDamagePopup` :4060 |
| Fix 2 Godot 协议 | `GodotClient/Network/ServerConnection.cs` | 事件 :195、`Process` :672 |
| Fix 2 旧客户端 | `Client/Envir/CConnection.cs` | `Process(HealthChanged)` :1863-1875 |
| Fix 2 旧客户端渲染 | `Client/Models/DamageInfo.cs` | `Resist` 字段 + `Draw()` :299-336 |

---

## 六、源文件索引

原版/服务端：
- `ServerLibrary/Models/Magics/Warrior/Swordsmanship.cs` — `GetPassiveStats` → Accuracy
- `ServerLibrary/Models/Magics/Warrior/PotionMastery.cs` — 空壳，效果在 PlayerObject
- `ServerLibrary/Models/Magics/Warrior/Slaying.cs` — 被动 Level*2 + 暴击
- `ServerLibrary/Models/Magics/Warrior/DefensiveMastery.cs` — `GetPassiveStats` → Stat.DefensiveMastery
- `ServerLibrary/Models/Magics/Warrior/MagicImmunity.cs` / `PhysicalImmunity.cs` — 空壳，效果在 PlayerObject.Attacked
- `ServerLibrary/Models/Magics/Warrior/AugmentDefiance.cs` / `Defiance.cs` — augment + 被增强的主动技
- `ServerLibrary/Models/PlayerObject.cs` — `RefreshStats` :2256、`Attacked` :15609、`GetMagic` :16032、药剂精通 :6346
- `ServerLibrary/Models/MapObject.cs` — `ProcessHPMP` 广播 :161-206、`GetAC` :1819-1836、`DisplayResist` :68
- `ServerLibrary/DBModels/UserMagic.cs` — `GetPower()` :179-188
- `LibraryCore/SystemModels/MagicInfo.cs` — Power 字段定义 :98-137
- `LibraryCore/Network/ServerPackets.cs` — `HealthChanged` :505-512

客户端：
- `Client/Envir/CConnection.cs` — `Process(HealthChanged)` :1863（旧版 Miss/Block/Critical 渲染入口）
- `Client/Models/DamageInfo.cs` — Miss/Block/Critical 飘字渲染 :270-368
- `GodotClient/Scripts/GameScene.cs` — `OnHealthChanged` :4016、`SpawnDamagePopup` :4060（缺失项）
- `GodotClient/Scripts/DamagePopupNode.cs` — 飘字节点（缺失 Miss/Block 分支）
- `GodotClient/Network/ServerConnection.cs` — `HealthChangedEvent` :195、`Process` :672

诊断工具：
- `Tools/MagicInfoDump/` — 只读 dump MagicInfo Power 字段（`dotnet run -- <RootDir>`，RootDir 含 System.db）