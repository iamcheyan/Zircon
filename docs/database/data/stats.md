<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改 -->

# 属性字典（Stat 枚举）

> `ItemInfo.Stats` / `MonsterInfo.Stats` / `MapInfo.BuffStats` 等字段中出现的属性名，均来自本枚举。
> 格式列的 `{0}` 即属性值（`{0}-{1}` 表示 Min-Max 区间，`{0:+#0%}` 表示百分比）。

| 成员 | 值 | 显示名 | 类型 | 备注 |
|---|---|---|---|---|
| BaseHealth | 0 | Base Health | None | —（格式 {0:+#0;-#0;#0}） |
| BaseMana | 1 | Base Mana | None | —（格式 {0:+#0;-#0;#0}） |
| Health | 2 | Health | Default | —（格式 {0:+#0;-#0;#0}） |
| Mana | 3 | Mana | Default | —（格式 {0:+#0;-#0;#0}） |
| MinAC | 4 | AC | Min | —（格式 {0}-0） |
| MaxAC | 5 | AC | Max | —（格式 {0}-{1}） |
| MinMR | 6 | MR | Min | —（格式 {0}-0） |
| MaxMR | 7 | MR | Max | —（格式 {0}-{1}） |
| MinDC | 8 | DC | Min | —（格式 {0}-0） |
| MaxDC | 9 | DC | Max | —（格式 {0}-{1}） |
| MinMC | 10 | MC | SpellPower | —（格式 {0}-0） |
| MaxMC | 11 | MC | SpellPower | —（格式 {0}-{1}） |
| MinSC | 12 | SC | SpellPower | —（格式 {0}-0） |
| MaxSC | 13 | SC | SpellPower | —（格式 {0}-{1}） |
| Accuracy | 14 | Accuracy | Default | —（格式 {0:+#0;-#0;#0}） |
| Agility | 15 | Agility | Default | —（格式 {0:+#0;-#0;#0}） |
| AttackSpeed | 16 | Attack Speed | Default | —（格式 {0:+#0;-#0;#0}） |
| Light | 17 | Light Radius | Default | —（格式 {0:+#0;-#0;#0}） |
| Strength | 18 | Strength | Default | —（格式 {0:+#0;-#0;#0}） |
| Luck | 19 | Luck | Default | —（格式 {0:+#0;-#0;#0}） |
| FireAttack | 20 | Fire | AttackElement | —（格式 {0:+#0;-#0;#0}） |
| FireResistance | 21 | Fire | ElementResistance | —（格式 {0:+#0;-#0;#0}） |
| IceAttack | 22 | Ice | AttackElement | —（格式 {0:+#0;-#0;#0}） |
| IceResistance | 23 | Ice | ElementResistance | —（格式 {0:+#0;-#0;#0}） |
| LightningAttack | 24 | Lightning | AttackElement | —（格式 {0:+#0;-#0;#0}） |
| LightningResistance | 25 | Lightning | ElementResistance | —（格式 {0:+#0;-#0;#0}） |
| WindAttack | 26 | Wind | AttackElement | —（格式 {0:+#0;-#0;#0}） |
| WindResistance | 27 | Wind | ElementResistance | —（格式 {0:+#0;-#0;#0}） |
| HolyAttack | 28 | Holy | AttackElement | —（格式 {0:+#0;-#0;#0}） |
| HolyResistance | 29 | Holy | ElementResistance | —（格式 {0:+#0;-#0;#0}） |
| DarkAttack | 30 | Dark | AttackElement | —（格式 {0:+#0;-#0;#0}） |
| DarkResistance | 31 | Dark | ElementResistance | —（格式 {0:+#0;-#0;#0}） |
| PhantomAttack | 32 | Phantom | AttackElement | —（格式 {0:+#0;-#0;#0}） |
| PhantomResistance | 33 | Phantom | ElementResistance | —（格式 {0:+#0;-#0;#0}） |
| Comfort | 34 | Comfort | Default | —（格式 {0:+#0;-#0;#0}） |
| LifeSteal | 35 | Life Steal | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| ExperienceRate | 36 | Experience Rate | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| DropRate | 37 | Drop Rate | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| None | 38 | Blank Stat | None | — |
| SkillRate | 39 | Skill Rate | Default | —（格式 x{0}） |
| PickUpRadius | 40 | Pick Up Range | Default | —（格式 {0:+#0;-#0;#0}） |
| Healing | 41 | Total Healing | Default | —（格式 {0:+#0;-#0;#0}） |
| HealingCap | 42 | Max Heal per Tick | Default | —（格式 {0:+#0;-#0;#0}） |
| Invisibility | 43 | Invisibility | Text | — |
| FireAffinity | 44 | Affinity: Fire | Text | — |
| IceAffinity | 45 | Affinity: Ice | Text | — |
| LightningAffinity | 46 | Affinity: Lightning | Text | — |
| WindAffinity | 47 | Affinity: Wind | Text | — |
| HolyAffinity | 48 | Affinity: Holy | Text | — |
| DarkAffinity | 49 | Affinity: Dark | Text | — |
| PhantomAffinity | 50 | Affinity: Phantom | Text | — |
| ReflectDamage | 51 | Reflect Damage | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| WeaponElement | 52 | — | None | — |
| Redemption | 53 | Temporary Innocence. | Text | — |
| HealthPercent | 54 | Health | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| CriticalChance | 55 | Critical Chance | Default | —（格式 {0:+#0;-#0;#0}%） |
| SaleBonus5 | 56 | 5% more profit when selling | Default | —（格式 {0} or more） |
| SaleBonus10 | 57 | 10% more profit when selling | Default | —（格式 {0} or more） |
| SaleBonus15 | 58 | 15% more profit when selling | Default | —（格式 {0} or more） |
| SaleBonus20 | 59 | 20% more profit when selling | Default | —（格式 {0} or more） |
| MagicShield | 60 | Magic Shield | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| Cloak | 61 | Invisible | Text | — |
| CloakDamage | 62 | Cloak Damage | Default | —（格式 {0} per tick） |
| TheNewBeginning | 63 | New Beginning Charges | Default | —（格式 {0}） |
| Brown | 64 | Brown, People can attack you freely | Text | — |
| PKPoint | 65 | PK Points | Default | —（格式 {0}） |
| GlobalShout | 66 | Global Shout no level restriction | Text | — |
| MCPercent | 67 | MC | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| JudgementOfHeaven | 68 | Chance of Judgement | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| Transparency | 69 | Transparency | Text | — |
| CelestialLight | 70 | HP Recovery | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| DarkConversion | 71 | MP Conversion | Default | —（格式 {0:+#0;-#0;#0}） |
| RenounceHPLost | 72 | HP Recovery | Default | —（格式 {0:+#0;-#0;#0}） |
| BagWeight | 73 | Inventory Weight | Default | —（格式 {0:+#0;-#0;#0}） |
| WearWeight | 74 | Wear Weight | Default | —（格式 {0:+#0;-#0;#0}） |
| HandWeight | 75 | Hand Weight | Default | —（格式 {0:+#0;-#0;#0}） |
| GoldRate | 76 | Gold Rate | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| OldDuration | 77 | OldDuration | Time | — |
| AvailableHuntGold | 78 | Available Hunt Gold | Default | —（格式 {0:+#0;-#0;#0}） |
| AvailableHuntGoldCap | 79 | Maximum Available Hunt Gold | Default | —（格式 {0:#0}） |
| ItemReviveTime | 80 | Revive Cool Down | Time | — |
| MaxRefineChance | 81 | Max Refine Chance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| CompanionInventory | 82 | Companion Inventory Space | Default | —（格式 {0:+#0;-#0;#0}） |
| CompanionBagWeight | 83 | Companion Inventory Weight | Default | —（格式 {0:+#0;-#0;#0}） |
| DCPercent | 84 | DC | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| SCPercent | 85 | SC | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| CompanionHunger | 86 | Companion Hunger | Default | —（格式 {0:+#0;-#0;#0}） |
| PetDCPercent | 87 | Pet's DC | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| BossTracker | 88 | Locates Boss Monsters on the Map | Text | — |
| PlayerTracker | 89 | Locates Players on the Map | Text | — |
| CompanionRate | 90 | Companion Rate | Default | —（格式 x{0}） |
| WeightRate | 91 | Weight Rate | Default | —（格式 x{0}） |
| MagicDefencePercent | 92 | Magic Defence | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| PhysicalDefencePercent | 93 | Physical Defence | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| ManaPercent | 94 | Mana | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| RecallSet | 95 | Recall Command: @GroupRecall | Text | — |
| MonsterExperience | 96 | Regular Monster's Base Experience | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| MonsterGold | 97 | Regular Monster's Base Gold | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| MonsterDrop | 98 | Regular Monster's Base Drop Rate | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| MonsterDamage | 99 | Regular Monster's Base Damage | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| MonsterHealth | 100 | Regular Monster's Base Health | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| ItemIndex | 101 | — | None | — |
| CompanionCollection | 102 | Improved Companion item collection. | Text | — |
| ProtectionRing | 103 | Protection Ring | Text | — |
| ClearRing | 104 | — | None | — |
| TeleportRing | 105 | — | None | — |
| BaseExperienceRate | 106 | Base Experience Rate | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| BaseGoldRate | 107 | Base Gold Rate | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| BaseDropRate | 108 | Base Drop Rate | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| FrostBiteDamage | 109 | Frost Bite Damage | Default | —（格式 {0}） |
| MaxMonsterExperience | 110 | Max Regular Monster's Base Experience | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| MaxMonsterGold | 111 | Max Regular Monster's Base Gold | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| MaxMonsterDrop | 112 | Max Regular Monster's Base Drop Rate | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| MaxMonsterDamage | 113 | Max Regular Monster's Base Damage | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| MaxMonsterHealth | 114 | Max Regular Monster's Base Health | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| CriticalDamage | 115 | Critical Dmg (PvE) | Percent | —（格式 x{0:+#0%;-#0%;#0%}） |
| Experience | 116 | Experience | Default | —（格式 {0}） |
| DeathDrops | 117 | Death Drops Enabled. | Text | — |
| PhysicalResistance | 118 | Physical | ElementResistance | —（格式 {0:+#0;-#0;#0}） |
| FragmentRate | 119 | Success Rate Per Fragment | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| MapSummoning | 120 | Chance to summon map  | Text | — |
| FrostBiteChance | 121 | Frost Bite Chance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| ParalysisChance | 122 | Paralysis Chance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| SlowChance | 123 | Slow Chance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| SilenceChance | 124 | Silence Chance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| BlockChance | 125 | Block Chance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| EvasionChance | 126 | Evasion Chance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| IgnoreStealth | 127 | — | None | — |
| FootballArmourAction | 128 | — | None | — |
| PoisonResistance | 129 | Poison Resistance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| Rebirth | 130 | Rebirth  | Default | —（格式 {0}） |
| Focus | 131 | Focus | Default | —（格式 {0:+#0;-#0;#0}） |
| SizePercent | 132 | Size Percent | None | — |
| GrowthLevel | 133 | Growth Level | Default | —（格式 {0}） |
| Invincibility | 134 | You are immune to all damage. | Text | — |
| SuperiorMagicShield | 135 | Absorbing Power | Default | Used in Superior Magic Shield Skill to represent remaining power to absorb.（格式 {0:+#0;-#0;#0}） |
| DefensiveMastery | 136 | Defensive Mastery | Percent | Used in Defensive Mastery Skill to give Luck on AC（格式 {0:+#0%;-#0%;#0%}） |
| SoulResonance | 137 | You are soulbound to another player. | Text | Used in Soul Resonance to tie together 2 players HP |
| Fame | 138 | — | None | — |
| ElementalSwords | 139 | Elemental Swords | Text | Used in Elemental Swords to track remaining swords（格式 {0}） |
| RoamDistance | 140 | — | None | — |
| ThrowDistance | 200 | Throw Distance | Default | 1 to 4（格式 {0}） |
| AutoCast | 201 | Auto Cast | Text | — |
| Flexibility | 202 | Flexibility | Default | —（格式 {0}） |
| FloatStrength | 203 | Float Strength | Default | —（格式 {0}） |
| ReelBonus | 204 | Reel Bonus | Default | —（格式 {0}） |
| NibbleChance | 205 | Nibble Chance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| FinderChance | 206 | Finder Chance | Percent | —（格式 {0:+#0%;-#0%;#0%}） |
| Random1 | 250 | — | None | — |
| Random2 | 251 | — | None | — |
| Counter1 | 252 | — | None | — |
| Counter2 | 253 | — | None | — |
| Duration | 10000 | Duration | Time | — |
