<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --dump docs/database -->

# 魔法（MagicInfo）

> 记录 #1 – #174，共 174 条。

## 快速浏览

| # | Name | Magic | Class | School | Property | Icon | NeedLevel1 | NeedLevel3 | BaseCost | Delay |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Swordsmanship | Swordsmanship | Warrior | Passive | Passive | 4 | 7 | 11 | 0 | 0 |
| 2 | Potion Mastery | PotionMastery | Warrior | Passive | Passive | 262 | 12 | 33 | 0 | 0 |
| 3 | Slaying | Slaying | Warrior | Passive | Passive | 12 | 14 | 18 | 0 | 0 |
| 4 | Thrusting | Thrusting | Warrior | Toggle | Toggle | 22 | 19 | 23 | 0 | 0 |
| 5 | Half Moon | HalfMoon | Warrior | Toggle | Toggle | 48 | 24 | 28 | 3 | 0 |
| 6 | Shoulder Dash | ShoulderDash | Warrior | Active | Active | 52 | 27 | 31 | 0 | 4000 |
| 7 | Flaming Sword | FlamingSword | Warrior | Active | Charge | 50 | 32 | 36 | 7 | 7000 |
| 8 | Dragon Rise | DragonRise | Warrior | Active | Charge | 68 | 35 | 39 | 8 | 7000 |
| 9 | Blade Storm | BladeStorm | Warrior | Active | Charge | 66 | 38 | 42 | 9 | 7000 |
| 10 | Destructive Surge | DestructiveSurge | Warrior | Toggle | Toggle | 204 | 40 | 46 | 7 | 0 |
| 11 | Interchange | Interchange | Warrior | Active | Active | 212 | 42 | 48 | 10 | 5000 |
| 12 | Defiance | Defiance | Warrior | Active | Active | 202 | 44 | 50 | 40 | 0 |
| 13 | Beckon | Beckon | Warrior | Active | Active | 214 | 46 | 52 | 20 | 5000 |
| 14 | Might | Might | Warrior | Active | Active | 210 | 48 | 54 | 50 | 0 |
| 15 | Swift Blade | SwiftBlade | Warrior | Active | Active | 260 | 49 | 65 | 50 | 7000 |
| 16 | Assault | Assault | Warrior | Active | Augmentation | 216 | 50 | 56 | 0 | 8000 |
| 17 | Endurance | Endurance | Warrior | Active | Active | 254 | 51 | 59 | 20 | 120000 |
| 18 | Reflect Damage | ReflectDamage | Warrior | Active | Active | 250 | 53 | 63 | 10 | 120000 |
| 19 | Fetter | Fetter | Warrior | Active | Active | 258 | 55 | 67 | 35 | 0 |
| 20 | Advanced Destructive Surge | AugmentDestructiveSurge | Warrior | Toggle | Augmentation | 526 | 84 | 96 | 0 | 0 |
| 21 | Advanced Defiance | AugmentDefiance | Warrior | Passive | Augmentation | 388 | 80 | 84 | 0 | 0 |
| 22 | Advanced Reflect Damage | AugmentReflectDamage | Warrior | Passive | Augmentation | 458 | 82 | 82 | 0 | 0 |
| 23 | Fire Ball | FireBall | Wizard | Fire | Active | 0 | 7 | 11 | 1 | 0 |
| 24 | Lightning Ball | LightningBall | Wizard | Lightning | Active | 80 | 8 | 12 | 1 | 0 |
| 25 | Ice Bolt | IceBolt | Wizard | Ice | Active | 76 | 9 | 13 | 1 | 0 |
| 26 | Gust Blast | GustBlast | Wizard | Wind | Active | 132 | 10 | 14 | 1 | 0 |
| 27 | Repulsion | Repulsion | Wizard | Wind | Active | 14 | 12 | 16 | 1 | 0 |
| 28 | Electric Shock | ElectricShock | Wizard | Lightning | Active | 38 | 13 | 17 | 3 | 0 |
| 29 | Teleportation | Teleportation | Wizard | Phantom | Active | 40 | 14 | 18 | 10 | 7000 |
| 30 | Adamantine Fire Ball | AdamantineFireBall | Wizard | Fire | Active | 8 | 15 | 19 | 6 | 0 |
| 31 | Thunder Bolt | ThunderBolt | Wizard | Lightning | Active | 20 | 16 | 20 | 6 | 0 |
| 32 | Ice Blades | IceBlades | Wizard | Ice | Active | 78 | 17 | 21 | 6 | 0 |
| 33 | Cyclone | Cyclone | Wizard | Wind | Active | 146 | 18 | 22 | 6 | 0 |
| 34 | Scorched Earth | ScortchedEarth | Wizard | Fire | Active | 16 | 20 | 24 | 15 | 0 |
| 35 | Lightning Beam | LightningBeam | Wizard | Lightning | Active | 18 | 21 | 25 | 15 | 0 |
| 36 | Frozen Earth | FrozenEarth | Wizard | Ice | Active | 104 | 22 | 26 | 15 | 0 |
| 37 | Blow Earth | BlowEarth | Wizard | Wind | Active | 144 | 23 | 27 | 15 | 0 |
| 38 | Fire Wall | FireWall | Wizard | Fire | Active | 42 | 24 | 28 | 30 | 0 |
| 39 | Expel Undead | ExpelUndead | Wizard | Phantom | Active | 62 | 26 | 30 | 30 | 0 |
| 40 | Geo Manipulation | GeoManipulation | Wizard | Phantom | Active | 206 | 27 | 31 | 20 | 5000 |
| 41 | Magic Shield | MagicShield | Wizard | Phantom | Active | 60 | 29 | 33 | 30 | 0 |
| 42 | Fire Storm | FireStorm | Wizard | Fire | Active | 44 | 32 | 36 | 20 | 0 |
| 43 | Lightning Wave | LightningWave | Wizard | Lightning | Active | 46 | 33 | 37 | 20 | 0 |
| 44 | Ice Storm | IceStorm | Wizard | Ice | Active | 64 | 34 | 38 | 20 | 0 |
| 45 | Dragon Tornado | DragonTornado | Wizard | Wind | Active | 142 | 35 | 39 | 20 | 0 |
| 46 | Greater Frozen Earth | GreaterFrozenEarth | Wizard | Ice | Active | 218 | 38 | 44 | 20 | 0 |
| 47 | Chain Lightning | ChainLightning | Wizard | Lightning | Active | 220 | 40 | 44 | 30 | 0 |
| 48 | Meteor Shower | MeteorShower | Wizard | Fire | Active | 224 | 43 | 47 | 40 | 0 |
| 49 | Renounce | Renounce | Wizard | Phantom | Active | 222 | 46 | 50 | 10 | 0 |
| 50 | Tempest | Tempest | Wizard | Wind | Active | 226 | 49 | 53 | 40 | 0 |
| 51 | Judgement Of Heaven | JudgementOfHeaven | Wizard | Lightning | Active | 264 | 52 | 62 | 40 | 0 |
| 52 | Thunder Storm | ThunderStrike | Wizard | Lightning | Active | 266 | 54 | 64 | 30 | 0 |
| 53 | Fire Bounce | FireBounce | Wizard | None | Active | 0 | 15 | 19 | 6 | 0 |
| 54 | Elemental Hurricane | ElementalHurricane | Wizard | Wind | Active | 436 | 83 | 87 | 20 | 0 |
| 55 | Superior Magic Shield | SuperiorMagicShield | Wizard | Phantom | Active | 444 | 65 | 75 | 20 | 0 |
| 56 | Burning | Burning | Wizard | Fire | Augmentation | 484 | 76 | 86 | 20 | 0 |
| 57 | Shock | Shocked | Wizard | Lightning | Augmentation | 532 | 85 | 91 | 20 | 0 |
| 58 | Lightning Strike | LightningStrike | Wizard | Lightning | Active | 452 | 90 | 90 | 50 | 0 |
| 59 | Heal | Heal | Taoist | Holy | Active | 2 | 7 | 11 | 2 | 0 |
| 60 | Spirit Sword | SpiritSword | Taoist | Physical | Passive | 6 | 8 | 12 | 0 | 0 |
| 61 | Poison Dust | PoisonDust | Taoist | Dark | Active | 10 | 12 | 16 | 5 | 0 |
| 62 | Explosive Talisman | ExplosiveTalisman | Taoist | Dark | Active | 24 | 13 | 17 | 3 | 0 |
| 63 | Evil Slayer | EvilSlayer | Taoist | Holy | Active | 72 | 14 | 18 | 3 | 0 |
| 64 | Invisibility | Invisibility | Taoist | Dark | Active | 34 | 20 | 24 | 5 | 0 |
| 65 | Magic Resistance | MagicResistance | Taoist | Dark | Active | 26 | 21 | 25 | 5 | 0 |
| 66 | Mass Invisibility | MassInvisibility | Taoist | Dark | Active | 36 | 23 | 27 | 5 | 0 |
| 67 | Greater Evil Slayer | GreaterEvilSlayer | Taoist | Holy | Active | 74 | 24 | 28 | 4 | 0 |
| 68 | Resilience | Resilience | Taoist | Dark | Active | 28 | 25 | 29 | 5 | 0 |
| 69 | Trap Octagon | TrapOctagon | Taoist | Dark | Active | 30 | 27 | 31 | 10 | 0 |
| 70 | Combat Kick | CombatKick | Taoist | Physical | Active | 70 | 28 | 32 | 10 | 0 |
| 71 | Elemental Superiority | ElementalSuperiority | Taoist | Dark | Active | 176 | 29 | 33 | 5 | 0 |
| 72 | Mass Heal | MassHeal | Taoist | Holy | Active | 56 | 31 | 35 | 20 | 0 |
| 73 | Blood Lust | BloodLust | Taoist | Dark | Active | 186 | 34 | 38 | 5 | 0 |
| 74 | Resurrection | Resurrection | Taoist | Holy | Active | 152 | 35 | 39 | 100 | 0 |
| 75 | Purification | Purification | Taoist | Holy | Active | 238 | 38 | 44 | 10 | 0 |
| 76 | Transparency | Transparency | Taoist | Dark | Active | 240 | 43 | 47 | 80 | 5000 |
| 77 | Celestial Light | CelestialLight | Taoist | Holy | Active | 242 | 46 | 50 | 50 | 0 |
| 78 | Empowered Healing | EmpoweredHealing | Taoist | Holy | Augmentation | 256 | 47 | 60 | 2 | 0 |
| 79 | Life Steal | LifeSteal | Taoist | Holy | Active | 270 | 48 | 62 | 10 | 0 |
| 80 | Improved Explosive Talisman | ImprovedExplosiveTalisman | Taoist | Dark | Active | 246 | 49 | 53 | 10 | 0 |
| 81 | Empowered Poison Dust | AugmentPoisonDust | Taoist | Dark | Augmentation | 268 | 50 | 58 | 0 | 5000 |
| 82 | Cursed Doll | CursedDoll | Taoist | Phantom | Active | 272 | 52 | 61 | 15 | 0 |
| 83 | Thunder Kick | ThunderKick | Taoist | Physical | Active | 248 | 54 | 64 | 10 | 0 |
| 84 | Soul Resonance | SoulResonance | Taoist | Holy | Active | 482 | 84 | 84 | 55 | 0 |
| 85 | Parasite | Parasite | Taoist | Dark | Active | 396 | 62 | 70 | 80 | 0 |
| 86 | Spiritualism | Spiritualism | Taoist | Dark | Active | 394 | 80 | 84 | 15 | 0 |
| 87 | Willow Dance | WillowDance | Assassin | Atrocity | Passive | 308 | 7 | 11 | 0 | 0 |
| 88 | Vine Tree Dance | VineTreeDance | Assassin | Atrocity | Passive | 310 | 10 | 14 | 0 | 0 |
| 89 | Discipline | Discipline | Assassin | Atrocity | Passive | 314 | 12 | 16 | 0 | 0 |
| 90 | Poisonous Cloud | PoisonousCloud | Assassin | Atrocity | Active | 312 | 14 | 18 | 5 | 20000 |
| 91 | Full Bloom | FullBloom | Assassin | Kill | Charge | 328 | 19 | 23 | 2 | 3000 |
| 92 | Cloak | Cloak | Assassin | Assassination | Active | 324 | 20 | 24 | 50 | 0 |
| 93 | White Lotus | WhiteLotus | Assassin | Kill | Charge | 330 | 22 | 26 | 3 | 3000 |
| 94 | Calamity Of Full Moon | CalamityOfFullMoon | Assassin | Kill | Passive | 340 | 22 | 26 | 4 | 0 |
| 95 | Wraith Grip | WraithGrip | Assassin | Atrocity | Active | 316 | 24 | 28 | 10 | 60000 |
| 96 | Red Lotus | RedLotus | Assassin | Kill | Charge | 332 | 24 | 28 | 4 | 3000 |
| 97 | Hell Fire | HellFire | Assassin | Kill | Active | 318 | 26 | 30 | 10 | 20000 |
| 98 | Pledge Of Blood | PledgeOfBlood | Assassin | Assassination | Passive | 352 | 26 | 30 | 0 | 0 |
| 99 | Rake | Rake | Assassin | Assassination | Active | 376 | 26 | 30 | 5 | 5000 |
| 100 | Sweetbrier | SweetBrier | Assassin | Kill | Charge | 334 | 27 | 31 | 5 | 3000 |
| 101 | Summon Puppet | SummonPuppet | Assassin | Assassination | Active | 326 | 30 | 34 | 10 | 30000 |
| 102 | Karma | Karma | Assassin | Assassination | Charge | 342 | 30 | 40 | 0 | 15000 |
| 103 | Touch Of The Departed | TouchOfTheDeparted | Assassin | Atrocity | Augmentation | 354 | 30 | 34 | 0 | 0 |
| 104 | Waning Moon | WaningMoon | Assassin | Assassination | Passive | 350 | 32 | 36 | 4 | 0 |
| 105 | Ghost Walk | GhostWalk | Assassin | Assassination | Augmentation | 356 | 32 | 36 | 0 | 0 |
| 106 | Elemental Puppet | ElementalPuppet | Assassin | Assassination | Augmentation | 358 | 34 | 38 | 0 | 0 |
| 107 | Rejuvenation | Rejuvenation | Assassin | Atrocity | Passive | 336 | 35 | 39 | 6 | 0 |
| 108 | Resolution | Resolution | Assassin | Assassination | Augmentation | 344 | 35 | 39 | 2 | 0 |
| 109 | Change Of Seasons | ChangeOfSeasons | Assassin | None | None | 360 | 36 | 0 | 0 | 0 |
| 110 | Release | Release | Assassin | Assassination | Passive | 378 | 36 | 40 | 1 | 0 |
| 111 | Flame Splash | FlameSplash | Assassin | Kill | Toggle | 320 | 38 | 42 | 0 | 0 |
| 112 | Bloody Flower | BloodyFlower | Assassin | Kill | Charge | 374 | 12 | 33 | 0 | 0 |
| 113 | The New Beginning | TheNewBeginning | Assassin | Atrocity | Active | 346 | 40 | 46 | 0 | 1000 |
| 114 | Dance Of Swallow | DanceOfSwallow | Assassin | Kill | Active | 362 | 40 | 44 | 0 | 5000 |
| 115 | Dark Conversion | DarkConversion | Assassin | Atrocity | Active | 364 | 42 | 46 | 0 | 0 |
| 116 | Dragon Repulse | DragonRepulse | Assassin | Atrocity | Active | 322 | 45 | 49 | 100 | 30000 |
| 117 | Advent Of Demon | AdventOfDemon | Assassin | Kill | Passive | 338 | 45 | 49 | 4 | 0 |
| 118 | Advent Of Devil | AdventOfDevil | Assassin | Assassination | Passive | 348 | 45 | 49 | 3 | 0 |
| 119 | Abyss | Abyss | Assassin | Atrocity | Active | 366 | 45 | 49 | 1 | 10000 |
| 120 | Flash Of Light | FlashOfLight | Assassin | Kill | Active | 368 | 45 | 50 | 22 | 5000 |
| 121 | Stealth | Stealth | Assassin | Assassination | Augmentation | 380 | 45 | 49 | 0 | 0 |
| 122 | Evasion | Evasion | Assassin | Atrocity | Active | 370 | 47 | 51 | 20 | 0 |
| 123 | Raging Wind | RagingWind | Assassin | Atrocity | Active | 372 | 47 | 51 | 20 | 0 |
| 124 | Empowered Explosive Talisman | AugmentExplosiveTalisman | Taoist | None | Augmentation | 24 | 17 | 51 | 0 | 3000 |
| 125 | Empowered Evil Slayer | AugmentEvilSlayer | Taoist | None | Augmentation | 72 | 17 | 51 | 0 | 3000 |
| 126 | Empowered Purification | AugmentPurification | Taoist | None | Augmentation | 238 | 55 | 62 | 0 | 10000 |
| 127 | Empowered Resurrection | AugmentResurrection | Taoist | None | Augmentation | 152 | 75 | 80 | 0 | 60000 |
| 128 | Demon Explosion | DemonExplosion | Taoist | Phantom | Active | 306 | 52 | 58 | 100 | 10000 |
| 129 | Strength Of Faith | StrengthOfFaith | Taoist | Phantom | Active | 244 | 40 | 44 | 30 | 0 |
| 130 | Summon Skeleton | SummonSkeleton | Taoist | Phantom | Active | 32 | 17 | 21 | 10 | 0 |
| 131 | Mirror Image | MirrorImage | Wizard | None | Active | 252 | 56 | 66 | 10 | 0 |
| 132 | Summon Jin Skeleton | SummonJinSkeleton | Taoist | Phantom | Active | 208 | 33 | 37 | 25 | 0 |
| 133 | Summon Shinsu | SummonShinsu | Taoist | Phantom | Active | 58 | 30 | 34 | 15 | 0 |
| 134 | Summon Demonic Creature | SummonDemonicCreature | Taoist | Phantom | Active | 304 | 50 | 56 | 20 | 0 |
| 135 | Advanced Potion Mastery | AdvancedPotionMastery | Warrior | None | Augmentation | 262 | 40 | 60 | 0 | 0 |
| 136 | _Blank_ | Unused | Assassin | None | None | 0 | 0 | 0 | 0 | 0 |
| 137 | Ice Rain | IceRain | Wizard | Ice | Active | 486 | 82 | 90 | 50 | 0 |
| 138 | Mass Beckon | MassBeckon | Warrior | Active | Active | 386 | 60 | 66 | 100 | 5000 |
| 139 | Frost Bite | FrostBite | Wizard | Ice | Active | 390 | 58 | 62 | 100 | 25000 |
| 140 | Infection | Infection | Taoist | Dark | Augmentation | 446 | 65 | 75 | 0 | 3000 |
| 141 | Massacre | Massacre | Assassin | None | Passive | 382 | 65 | 75 | 0 | 0 |
| 142 | Seismic Slam | SeismicSlam | Warrior | Active | Active | 434 | 83 | 87 | 50 | 18000 |
| 143 | Demonic Recovery | DemonicRecovery | Taoist | Phantom | Active | 536 | 48 | 54 | 100 | 0 |
| 144 | Asteroid | Asteroid | Wizard | Fire | Active | 392 | 80 | 84 | 200 | 3300 |
| 145 | Art of Shadows | ArtOfShadows | Assassin | None | Passive | 396 | 75 | 82 | 0 | 0 |
| 146 | Invincibility | Invincibility | Warrior | Active | Active | 442 | 65 | 75 | 0 | 5000 |
| 147 | Crushing Wave | CrushingWave | Warrior | Active | Active | 450 | 90 | 90 | 0 | 0 |
| 148 | Neutralize | Neutralize | Taoist | Dark | Active | 480 | 80 | 91 | 10 | 0 |
| 149 | Empowered Neutralize | AugmentNeutralize | Taoist | None | Augmentation | 480 | 80 | 91 | 0 | 0 |
| 150 | Dark Soul Prison | DarkSoulPrison | Taoist | Dark | Active | 454 | 90 | 90 | 10 | 0 |
| 151 | Searing Light | SearingLight | Taoist | Holy | Active | 438 | 83 | 87 | 15 | 5000 |
| 152 | Defensive Mastery | DefensiveMastery | Warrior | Passive | Passive | 466 | 70 | 80 | 0 | 0 |
| 153 | Physical Immunity | PhysicalImmunity | Warrior | Passive | Passive | 468 | 80 | 88 | 0 | 0 |
| 154 | Magic Immunity | MagicImmunity | Warrior | Passive | Passive | 470 | 80 | 88 | 0 | 0 |
| 155 | Defensive Blow | DefensiveBlow | Warrior | Active | Charge | 488 | 86 | 91 | 50 | 10000 |
| 156 | Elemental Swords | ElementalSwords | Warrior | Active | Active | 502 | 95 | 97 | 10 | 5000 |
| 157 | Storm | Storm | Wizard | None | Active | 492 | 86 | 91 | 0 | 0 |
| 158 | Tornado | Tornado | Wizard | Wind | Active | 508 | 95 | 97 | 0 | 0 |
| 159 | Empowered Celestial Light | AugmentCelestialLight | Taoist | Holy | Augmentation | 462 | 82 | 84 | 0 | 0 |
| 160 | Corpse Exploder | CorpseExploder | Taoist | Dark | Active | 490 | 86 | 95 | 30 | 0 |
| 161 | Summon Dead | SummonDead | Taoist | Phantom | Active | 514 | 95 | 97 | 0 | 0 |
| 162 | Dragon Blood | DragonBlood | Assassin | Kill | Passive | 382 | 60 | 64 | 0 | 0 |
| 163 | Fatal Blow | FatalBlow | Assassin | Kill | Passive | 474 | 60 | 80 | 0 | 0 |
| 164 | Last Stand | LastStand | Assassin | Atrocity | Passive | 448 | 65 | 85 | 0 | 0 |
| 165 | Magic Combustion | MagicCombustion | Assassin | Atrocity | Active | 478 | 70 | 76 | 10 | 10000 |
| 166 | Vitality | Vitality | Assassin | Atrocity | Passive | 472 | 70 | 80 | 0 | 0 |
| 167 | Chain | Chain | Assassin | Atrocity | Active | 476 | 75 | 84 | 15 | 15000 |
| 168 | Concentration | Concentration | Assassin | Atrocity | Active | 384 | 80 | 84 | 30 | 0 |
| 169 | Dual Weapon Skills | DualWeaponSkills | Assassin | Atrocity | Passive | 464 | 82 | 84 | 0 | 0 |
| 170 | Containment | Containment | Assassin | Atrocity | Active | 440 | 83 | 87 | 0 | 5000 |
| 171 | Dragon Wave | DragonWave | Assassin | Kill | Augmentation | 542 | 85 | 95 | 1 | 0 |
| 172 | Hemorrhage | Hemorrhage | Assassin | Kill | Active | 494 | 86 | 91 | 25 | 5000 |
| 173 | Burning Fire | BurningFire | Assassin | Kill | Active | 456 | 90 | 90 | 30 | 5000 |
| 174 | Chain Of Fire | ChainOfFire | Assassin | Atrocity | Augmentation | 520 | 95 | 97 | 0 | 0 |

### Warrior（32 个）

| # | Name | Magic | 1级 | 2级 | 3级 | 基础耗蓝 | 施法延迟 |
|---|---|---|---|---|---|---|---|
| 1 | Swordsmanship | Swordsmanship | 7 | 9 | 11 | 0 | 0 |
| 2 | Potion Mastery | PotionMastery | 12 | 22 | 33 | 0 | 0 |
| 3 | Slaying | Slaying | 14 | 16 | 18 | 0 | 0 |
| 4 | Thrusting | Thrusting | 19 | 21 | 23 | 0 | 0 |
| 5 | Half Moon | HalfMoon | 24 | 26 | 28 | 3 | 0 |
| 6 | Shoulder Dash | ShoulderDash | 27 | 29 | 31 | 0 | 4000 |
| 7 | Flaming Sword | FlamingSword | 32 | 34 | 36 | 7 | 7000 |
| 8 | Dragon Rise | DragonRise | 35 | 37 | 39 | 8 | 7000 |
| 9 | Blade Storm | BladeStorm | 38 | 40 | 42 | 9 | 7000 |
| 10 | Destructive Surge | DestructiveSurge | 40 | 43 | 46 | 7 | 0 |
| 11 | Interchange | Interchange | 42 | 45 | 48 | 10 | 5000 |
| 12 | Defiance | Defiance | 44 | 47 | 50 | 40 | 0 |
| 13 | Beckon | Beckon | 46 | 49 | 52 | 20 | 5000 |
| 14 | Might | Might | 48 | 51 | 54 | 50 | 0 |
| 15 | Swift Blade | SwiftBlade | 49 | 57 | 65 | 50 | 7000 |
| 16 | Assault | Assault | 50 | 53 | 56 | 0 | 8000 |
| 17 | Endurance | Endurance | 51 | 55 | 59 | 20 | 120000 |
| 18 | Reflect Damage | ReflectDamage | 53 | 58 | 63 | 10 | 120000 |
| 19 | Fetter | Fetter | 55 | 61 | 67 | 35 | 0 |
| 20 | Advanced Destructive Surge | AugmentDestructiveSurge | 84 | 90 | 96 | 0 | 0 |
| 21 | Advanced Defiance | AugmentDefiance | 80 | 82 | 84 | 0 | 0 |
| 22 | Advanced Reflect Damage | AugmentReflectDamage | 82 | 82 | 82 | 0 | 0 |
| 135 | Advanced Potion Mastery | AdvancedPotionMastery | 40 | 50 | 60 | 0 | 0 |
| 138 | Mass Beckon | MassBeckon | 60 | 63 | 66 | 100 | 5000 |
| 142 | Seismic Slam | SeismicSlam | 83 | 85 | 87 | 50 | 18000 |
| 146 | Invincibility | Invincibility | 65 | 70 | 75 | 0 | 5000 |
| 147 | Crushing Wave | CrushingWave | 90 | 90 | 90 | 0 | 0 |
| 152 | Defensive Mastery | DefensiveMastery | 70 | 75 | 80 | 0 | 0 |
| 153 | Physical Immunity | PhysicalImmunity | 80 | 84 | 88 | 0 | 0 |
| 154 | Magic Immunity | MagicImmunity | 80 | 84 | 88 | 0 | 0 |
| 155 | Defensive Blow | DefensiveBlow | 86 | 88 | 91 | 50 | 10000 |
| 156 | Elemental Swords | ElementalSwords | 95 | 96 | 97 | 10 | 5000 |

### Wizard（42 个）

| # | Name | Magic | 1级 | 2级 | 3级 | 基础耗蓝 | 施法延迟 |
|---|---|---|---|---|---|---|---|
| 23 | Fire Ball | FireBall | 7 | 9 | 11 | 1 | 0 |
| 24 | Lightning Ball | LightningBall | 8 | 10 | 12 | 1 | 0 |
| 25 | Ice Bolt | IceBolt | 9 | 11 | 13 | 1 | 0 |
| 26 | Gust Blast | GustBlast | 10 | 12 | 14 | 1 | 0 |
| 27 | Repulsion | Repulsion | 12 | 14 | 16 | 1 | 0 |
| 28 | Electric Shock | ElectricShock | 13 | 15 | 17 | 3 | 0 |
| 29 | Teleportation | Teleportation | 14 | 16 | 18 | 10 | 7000 |
| 30 | Adamantine Fire Ball | AdamantineFireBall | 15 | 17 | 19 | 6 | 0 |
| 31 | Thunder Bolt | ThunderBolt | 16 | 18 | 20 | 6 | 0 |
| 32 | Ice Blades | IceBlades | 17 | 19 | 21 | 6 | 0 |
| 33 | Cyclone | Cyclone | 18 | 20 | 22 | 6 | 0 |
| 34 | Scorched Earth | ScortchedEarth | 20 | 22 | 24 | 15 | 0 |
| 35 | Lightning Beam | LightningBeam | 21 | 23 | 25 | 15 | 0 |
| 36 | Frozen Earth | FrozenEarth | 22 | 24 | 26 | 15 | 0 |
| 37 | Blow Earth | BlowEarth | 23 | 25 | 27 | 15 | 0 |
| 38 | Fire Wall | FireWall | 24 | 26 | 28 | 30 | 0 |
| 39 | Expel Undead | ExpelUndead | 26 | 28 | 30 | 30 | 0 |
| 40 | Geo Manipulation | GeoManipulation | 27 | 29 | 31 | 20 | 5000 |
| 41 | Magic Shield | MagicShield | 29 | 31 | 33 | 30 | 0 |
| 42 | Fire Storm | FireStorm | 32 | 34 | 36 | 20 | 0 |
| 43 | Lightning Wave | LightningWave | 33 | 35 | 37 | 20 | 0 |
| 44 | Ice Storm | IceStorm | 34 | 36 | 38 | 20 | 0 |
| 45 | Dragon Tornado | DragonTornado | 35 | 37 | 39 | 20 | 0 |
| 46 | Greater Frozen Earth | GreaterFrozenEarth | 38 | 41 | 44 | 20 | 0 |
| 47 | Chain Lightning | ChainLightning | 40 | 42 | 44 | 30 | 0 |
| 48 | Meteor Shower | MeteorShower | 43 | 45 | 47 | 40 | 0 |
| 49 | Renounce | Renounce | 46 | 48 | 50 | 10 | 0 |
| 50 | Tempest | Tempest | 49 | 51 | 53 | 40 | 0 |
| 51 | Judgement Of Heaven | JudgementOfHeaven | 52 | 57 | 62 | 40 | 0 |
| 52 | Thunder Storm | ThunderStrike | 54 | 59 | 64 | 30 | 0 |
| 53 | Fire Bounce | FireBounce | 15 | 17 | 19 | 6 | 0 |
| 54 | Elemental Hurricane | ElementalHurricane | 83 | 85 | 87 | 20 | 0 |
| 55 | Superior Magic Shield | SuperiorMagicShield | 65 | 70 | 75 | 20 | 0 |
| 56 | Burning | Burning | 76 | 80 | 86 | 20 | 0 |
| 57 | Shock | Shocked | 85 | 88 | 91 | 20 | 0 |
| 58 | Lightning Strike | LightningStrike | 90 | 90 | 90 | 50 | 0 |
| 131 | Mirror Image | MirrorImage | 56 | 61 | 66 | 10 | 0 |
| 137 | Ice Rain | IceRain | 82 | 86 | 90 | 50 | 0 |
| 139 | Frost Bite | FrostBite | 58 | 60 | 62 | 100 | 25000 |
| 144 | Asteroid | Asteroid | 80 | 82 | 84 | 200 | 3300 |
| 157 | Storm | Storm | 86 | 88 | 91 | 0 | 0 |
| 158 | Tornado | Tornado | 95 | 96 | 97 | 0 | 0 |

### Taoist（47 个）

| # | Name | Magic | 1级 | 2级 | 3级 | 基础耗蓝 | 施法延迟 |
|---|---|---|---|---|---|---|---|
| 59 | Heal | Heal | 7 | 9 | 11 | 2 | 0 |
| 60 | Spirit Sword | SpiritSword | 8 | 10 | 12 | 0 | 0 |
| 61 | Poison Dust | PoisonDust | 12 | 14 | 16 | 5 | 0 |
| 62 | Explosive Talisman | ExplosiveTalisman | 13 | 15 | 17 | 3 | 0 |
| 63 | Evil Slayer | EvilSlayer | 14 | 16 | 18 | 3 | 0 |
| 64 | Invisibility | Invisibility | 20 | 22 | 24 | 5 | 0 |
| 65 | Magic Resistance | MagicResistance | 21 | 23 | 25 | 5 | 0 |
| 66 | Mass Invisibility | MassInvisibility | 23 | 25 | 27 | 5 | 0 |
| 67 | Greater Evil Slayer | GreaterEvilSlayer | 24 | 26 | 28 | 4 | 0 |
| 68 | Resilience | Resilience | 25 | 27 | 29 | 5 | 0 |
| 69 | Trap Octagon | TrapOctagon | 27 | 29 | 31 | 10 | 0 |
| 70 | Combat Kick | CombatKick | 28 | 30 | 32 | 10 | 0 |
| 71 | Elemental Superiority | ElementalSuperiority | 29 | 31 | 33 | 5 | 0 |
| 72 | Mass Heal | MassHeal | 31 | 33 | 35 | 20 | 0 |
| 73 | Blood Lust | BloodLust | 34 | 36 | 38 | 5 | 0 |
| 74 | Resurrection | Resurrection | 35 | 37 | 39 | 100 | 0 |
| 75 | Purification | Purification | 38 | 41 | 44 | 10 | 0 |
| 76 | Transparency | Transparency | 43 | 45 | 47 | 80 | 5000 |
| 77 | Celestial Light | CelestialLight | 46 | 48 | 50 | 50 | 0 |
| 78 | Empowered Healing | EmpoweredHealing | 47 | 53 | 60 | 2 | 0 |
| 79 | Life Steal | LifeSteal | 48 | 55 | 62 | 10 | 0 |
| 80 | Improved Explosive Talisman | ImprovedExplosiveTalisman | 49 | 51 | 53 | 10 | 0 |
| 81 | Empowered Poison Dust | AugmentPoisonDust | 50 | 54 | 58 | 0 | 5000 |
| 82 | Cursed Doll | CursedDoll | 52 | 56 | 61 | 15 | 0 |
| 83 | Thunder Kick | ThunderKick | 54 | 59 | 64 | 10 | 0 |
| 84 | Soul Resonance | SoulResonance | 84 | 84 | 84 | 55 | 0 |
| 85 | Parasite | Parasite | 62 | 66 | 70 | 80 | 0 |
| 86 | Spiritualism | Spiritualism | 80 | 82 | 84 | 15 | 0 |
| 124 | Empowered Explosive Talisman | AugmentExplosiveTalisman | 17 | 34 | 51 | 0 | 3000 |
| 125 | Empowered Evil Slayer | AugmentEvilSlayer | 17 | 34 | 51 | 0 | 3000 |
| 126 | Empowered Purification | AugmentPurification | 55 | 58 | 62 | 0 | 10000 |
| 127 | Empowered Resurrection | AugmentResurrection | 75 | 77 | 80 | 0 | 60000 |
| 128 | Demon Explosion | DemonExplosion | 52 | 56 | 58 | 100 | 10000 |
| 129 | Strength Of Faith | StrengthOfFaith | 40 | 42 | 44 | 30 | 0 |
| 130 | Summon Skeleton | SummonSkeleton | 17 | 19 | 21 | 10 | 0 |
| 132 | Summon Jin Skeleton | SummonJinSkeleton | 33 | 35 | 37 | 25 | 0 |
| 133 | Summon Shinsu | SummonShinsu | 30 | 32 | 34 | 15 | 0 |
| 134 | Summon Demonic Creature | SummonDemonicCreature | 50 | 54 | 56 | 20 | 0 |
| 140 | Infection | Infection | 65 | 70 | 75 | 0 | 3000 |
| 143 | Demonic Recovery | DemonicRecovery | 48 | 51 | 54 | 100 | 0 |
| 148 | Neutralize | Neutralize | 80 | 86 | 91 | 10 | 0 |
| 149 | Empowered Neutralize | AugmentNeutralize | 80 | 86 | 91 | 0 | 0 |
| 150 | Dark Soul Prison | DarkSoulPrison | 90 | 90 | 90 | 10 | 0 |
| 151 | Searing Light | SearingLight | 83 | 85 | 87 | 15 | 5000 |
| 159 | Empowered Celestial Light | AugmentCelestialLight | 82 | 83 | 84 | 0 | 0 |
| 160 | Corpse Exploder | CorpseExploder | 86 | 88 | 95 | 30 | 0 |
| 161 | Summon Dead | SummonDead | 95 | 96 | 97 | 0 | 0 |

### Assassin（53 个）

| # | Name | Magic | 1级 | 2级 | 3级 | 基础耗蓝 | 施法延迟 |
|---|---|---|---|---|---|---|---|
| 87 | Willow Dance | WillowDance | 7 | 9 | 11 | 0 | 0 |
| 88 | Vine Tree Dance | VineTreeDance | 10 | 12 | 14 | 0 | 0 |
| 89 | Discipline | Discipline | 12 | 14 | 16 | 0 | 0 |
| 90 | Poisonous Cloud | PoisonousCloud | 14 | 16 | 18 | 5 | 20000 |
| 91 | Full Bloom | FullBloom | 19 | 21 | 23 | 2 | 3000 |
| 92 | Cloak | Cloak | 20 | 22 | 24 | 50 | 0 |
| 93 | White Lotus | WhiteLotus | 22 | 24 | 26 | 3 | 3000 |
| 94 | Calamity Of Full Moon | CalamityOfFullMoon | 22 | 24 | 26 | 4 | 0 |
| 95 | Wraith Grip | WraithGrip | 24 | 26 | 28 | 10 | 60000 |
| 96 | Red Lotus | RedLotus | 24 | 26 | 28 | 4 | 3000 |
| 97 | Hell Fire | HellFire | 26 | 28 | 30 | 10 | 20000 |
| 98 | Pledge Of Blood | PledgeOfBlood | 26 | 28 | 30 | 0 | 0 |
| 99 | Rake | Rake | 26 | 28 | 30 | 5 | 5000 |
| 100 | Sweetbrier | SweetBrier | 27 | 29 | 31 | 5 | 3000 |
| 101 | Summon Puppet | SummonPuppet | 30 | 32 | 34 | 10 | 30000 |
| 102 | Karma | Karma | 30 | 35 | 40 | 0 | 15000 |
| 103 | Touch Of The Departed | TouchOfTheDeparted | 30 | 32 | 34 | 0 | 0 |
| 104 | Waning Moon | WaningMoon | 32 | 34 | 36 | 4 | 0 |
| 105 | Ghost Walk | GhostWalk | 32 | 34 | 36 | 0 | 0 |
| 106 | Elemental Puppet | ElementalPuppet | 34 | 36 | 38 | 0 | 0 |
| 107 | Rejuvenation | Rejuvenation | 35 | 37 | 39 | 6 | 0 |
| 108 | Resolution | Resolution | 35 | 37 | 39 | 2 | 0 |
| 109 | Change Of Seasons | ChangeOfSeasons | 36 | 0 | 0 | 0 | 0 |
| 110 | Release | Release | 36 | 38 | 40 | 1 | 0 |
| 111 | Flame Splash | FlameSplash | 38 | 40 | 42 | 0 | 0 |
| 112 | Bloody Flower | BloodyFlower | 12 | 22 | 33 | 0 | 0 |
| 113 | The New Beginning | TheNewBeginning | 40 | 43 | 46 | 0 | 1000 |
| 114 | Dance Of Swallow | DanceOfSwallow | 40 | 42 | 44 | 0 | 5000 |
| 115 | Dark Conversion | DarkConversion | 42 | 44 | 46 | 0 | 0 |
| 116 | Dragon Repulse | DragonRepulse | 45 | 47 | 49 | 100 | 30000 |
| 117 | Advent Of Demon | AdventOfDemon | 45 | 47 | 49 | 4 | 0 |
| 118 | Advent Of Devil | AdventOfDevil | 45 | 47 | 49 | 3 | 0 |
| 119 | Abyss | Abyss | 45 | 47 | 49 | 1 | 10000 |
| 120 | Flash Of Light | FlashOfLight | 45 | 47 | 50 | 22 | 5000 |
| 121 | Stealth | Stealth | 45 | 47 | 49 | 0 | 0 |
| 122 | Evasion | Evasion | 47 | 49 | 51 | 20 | 0 |
| 123 | Raging Wind | RagingWind | 47 | 49 | 51 | 20 | 0 |
| 136 | _Blank_ | Unused | 0 | 0 | 0 | 0 | 0 |
| 141 | Massacre | Massacre | 65 | 70 | 75 | 0 | 0 |
| 145 | Art of Shadows | ArtOfShadows | 75 | 79 | 82 | 0 | 0 |
| 162 | Dragon Blood | DragonBlood | 60 | 62 | 64 | 0 | 0 |
| 163 | Fatal Blow | FatalBlow | 60 | 70 | 80 | 0 | 0 |
| 164 | Last Stand | LastStand | 65 | 75 | 85 | 0 | 0 |
| 165 | Magic Combustion | MagicCombustion | 70 | 72 | 76 | 10 | 10000 |
| 166 | Vitality | Vitality | 70 | 74 | 80 | 0 | 0 |
| 167 | Chain | Chain | 75 | 80 | 84 | 15 | 15000 |
| 168 | Concentration | Concentration | 80 | 82 | 84 | 30 | 0 |
| 169 | Dual Weapon Skills | DualWeaponSkills | 82 | 83 | 84 | 0 | 0 |
| 170 | Containment | Containment | 83 | 85 | 87 | 0 | 5000 |
| 171 | Dragon Wave | DragonWave | 85 | 90 | 95 | 1 | 0 |
| 172 | Hemorrhage | Hemorrhage | 86 | 88 | 91 | 25 | 5000 |
| 173 | Burning Fire | BurningFire | 90 | 90 | 90 | 30 | 5000 |
| 174 | Chain Of Fire | ChainOfFire | 95 | 96 | 97 | 0 | 0 |

### #1 · Swordsmanship

| 字段 | 值 |
|---|---|
| Name | Swordsmanship |
| Magic | Swordsmanship |
| Class | Warrior |
| School | Passive |
| Property | Passive |
| Icon | 4 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 10 |
| MaxLevelPower | 10 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 7 |
| NeedLevel2 | 9 |
| NeedLevel3 | 11 |
| Experience1 | 100 |
| Experience2 | 200 |
| Experience3 | 300 |
| Delay | 0 |
| Description | A skill that increases your accuracy. The skill becomes more effective as you train. |

### #2 · Potion Mastery

| 字段 | 值 |
|---|---|
| Name | Potion Mastery |
| Magic | PotionMastery |
| Class | Warrior |
| School | Passive |
| Property | Passive |
| Icon | 262 |
| MinBasePower | 10 |
| MaxBasePower | 10 |
| MinLevelPower | 15 |
| MaxLevelPower | 15 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 12 |
| NeedLevel2 | 22 |
| NeedLevel3 | 33 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 40000 |
| Delay | 0 |
| Description | You become adept at using recovery items. You may recover more depending upon the level of your training. |

### #3 · Slaying

| 字段 | 值 |
|---|---|
| Name | Slaying |
| Magic | Slaying |
| Class | Warrior |
| School | Passive |
| Property | Passive |
| Icon | 12 |
| MinBasePower | 8 |
| MaxBasePower | 8 |
| MinLevelPower | 7 |
| MaxLevelPower | 7 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 14 |
| NeedLevel2 | 16 |
| NeedLevel3 | 18 |
| Experience1 | 300 |
| Experience2 | 400 |
| Experience3 | 500 |
| Delay | 0 |
| Description | A skill that increases your accuracy and damage. The skill becomes more effective as you train. |

### #4 · Thrusting

| 字段 | 值 |
|---|---|
| Name | Thrusting |
| Magic | Thrusting |
| Class | Warrior |
| School | Toggle |
| Property | Toggle |
| Icon | 22 |
| MinBasePower | 50 |
| MaxBasePower | 50 |
| MinLevelPower | 50 |
| MaxLevelPower | 50 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 19 |
| NeedLevel2 | 21 |
| NeedLevel3 | 23 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 0 |
| Description | Attack enemies outside of the melee range using the energy of your weapon. The damage done increases with training. |

### #5 · Half Moon

| 字段 | 值 |
|---|---|
| Name | Half Moon |
| Magic | HalfMoon |
| Class | Warrior |
| School | Toggle |
| Property | Toggle |
| Icon | 48 |
| MinBasePower | 40 |
| MaxBasePower | 40 |
| MinLevelPower | 50 |
| MaxLevelPower | 50 |
| BaseCost | 3 |
| LevelCost | 0 |
| NeedLevel1 | 24 |
| NeedLevel2 | 26 |
| NeedLevel3 | 28 |
| Experience1 | 600 |
| Experience2 | 700 |
| Experience3 | 800 |
| Delay | 0 |
| Description | An ancient skill attack that hits 4 nearby enemies with the power of wind created from the swing of a weapon. Its damage increases with training. |

### #6 · Shoulder Dash

| 字段 | 值 |
|---|---|
| Name | Shoulder Dash |
| Magic | ShoulderDash |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 52 |
| MinBasePower | 2 |
| MaxBasePower | 3 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 0 |
| LevelCost | 20 |
| NeedLevel1 | 27 |
| NeedLevel2 | 29 |
| NeedLevel3 | 31 |
| Experience1 | 700 |
| Experience2 | 800 |
| Experience3 | 900 |
| Delay | 4000 |
| Description | A shoulder blow to push away enemies of lower levels. As you train harder, you'll be able to push more enemies. |

### #7 · Flaming Sword

| 字段 | 值 |
|---|---|
| Name | Flaming Sword |
| Magic | FlamingSword |
| Class | Warrior |
| School | Active |
| Property | Charge |
| Icon | 50 |
| MinBasePower | 160 |
| MaxBasePower | 160 |
| MinLevelPower | 100 |
| MaxLevelPower | 100 |
| BaseCost | 7 |
| LevelCost | 2 |
| NeedLevel1 | 32 |
| NeedLevel2 | 34 |
| NeedLevel3 | 36 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 7000 |
| Description | Transfer your body's energy onto the weapon to increase its damage.  The damage bonus increases with training. |

### #8 · Dragon Rise

| 字段 | 值 |
|---|---|
| Name | Dragon Rise |
| Magic | DragonRise |
| Class | Warrior |
| School | Active |
| Property | Charge |
| Icon | 68 |
| MinBasePower | 120 |
| MaxBasePower | 120 |
| MinLevelPower | 100 |
| MaxLevelPower | 100 |
| BaseCost | 8 |
| LevelCost | 2 |
| NeedLevel1 | 35 |
| NeedLevel2 | 37 |
| NeedLevel3 | 39 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 7000 |
| Description | Harness the body's energy to throw yourself into the enemies. Its damage increases with training. |

### #9 · Blade Storm

| 字段 | 值 |
|---|---|
| Name | Blade Storm |
| Magic | BladeStorm |
| Class | Warrior |
| School | Active |
| Property | Charge |
| Icon | 66 |
| MinBasePower | 240 |
| MaxBasePower | 240 |
| MinLevelPower | 100 |
| MaxLevelPower | 100 |
| BaseCost | 9 |
| LevelCost | 3 |
| NeedLevel1 | 38 |
| NeedLevel2 | 40 |
| NeedLevel3 | 42 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 7000 |
| Description | A fast attack that deals 2 blows to the enemy. Its damage increases with training. |

### #10 · Destructive Surge

| 字段 | 值 |
|---|---|
| Name | Destructive Surge |
| Magic | DestructiveSurge |
| Class | Warrior |
| School | Toggle |
| Property | Toggle |
| Icon | 204 |
| MinBasePower | 70 |
| MaxBasePower | 70 |
| MinLevelPower | 30 |
| MaxLevelPower | 30 |
| BaseCost | 7 |
| LevelCost | 0 |
| NeedLevel1 | 40 |
| NeedLevel2 | 43 |
| NeedLevel3 | 46 |
| Experience1 | 2000 |
| Experience2 | 3000 |
| Experience3 | 6000 |
| Delay | 0 |
| Description | Transfers your body's energy onto your weapon to deal damage to your enemies around you. Its damage increases with training. |

### #11 · Interchange

| 字段 | 值 |
|---|---|
| Name | Interchange |
| Magic | Interchange |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 212 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 10 |
| LevelCost | 40 |
| NeedLevel1 | 42 |
| NeedLevel2 | 45 |
| NeedLevel3 | 48 |
| Experience1 | 4000 |
| Experience2 | 6000 |
| Experience3 | 12000 |
| Delay | 5000 |
| Description | A skill that allows you to switch the location of you and your target. Its chance of success increases with training. |

### #12 · Defiance

| 字段 | 值 |
|---|---|
| Name | Defiance |
| Magic | Defiance |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 202 |
| MinBasePower | 30 |
| MaxBasePower | 30 |
| MinLevelPower | 90 |
| MaxLevelPower | 90 |
| BaseCost | 40 |
| LevelCost | 80 |
| NeedLevel1 | 44 |
| NeedLevel2 | 47 |
| NeedLevel3 | 50 |
| Experience1 | 6000 |
| Experience2 | 9000 |
| Experience3 | 18000 |
| Delay | 0 |
| Description | Significantly increase your defense and magic resistance whilst reducing attack power. Duration increases with training. |

### #13 · Beckon

| 字段 | 值 |
|---|---|
| Name | Beckon |
| Magic | Beckon |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 214 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 20 |
| LevelCost | 40 |
| NeedLevel1 | 46 |
| NeedLevel2 | 49 |
| NeedLevel3 | 52 |
| Experience1 | 8000 |
| Experience2 | 12000 |
| Experience3 | 24000 |
| Delay | 5000 |
| Description | A skill that pulls a distant target into melee range. Its success rate increases with training. |

### #14 · Might

| 字段 | 值 |
|---|---|
| Name | Might |
| Magic | Might |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 210 |
| MinBasePower | 30 |
| MaxBasePower | 30 |
| MinLevelPower | 90 |
| MaxLevelPower | 90 |
| BaseCost | 50 |
| LevelCost | 100 |
| NeedLevel1 | 48 |
| NeedLevel2 | 51 |
| NeedLevel3 | 54 |
| Experience1 | 10000 |
| Experience2 | 15000 |
| Experience3 | 30000 |
| Delay | 0 |
| Description | Moderately increase your damage. The damage bonus and duration increases with training. |

### #15 · Swift Blade

| 字段 | 值 |
|---|---|
| Name | Swift Blade |
| Magic | SwiftBlade |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 260 |
| MinBasePower | 80 |
| MaxBasePower | 80 |
| MinLevelPower | 100 |
| MaxLevelPower | 100 |
| BaseCost | 50 |
| LevelCost | 80 |
| NeedLevel1 | 49 |
| NeedLevel2 | 57 |
| NeedLevel3 | 65 |
| Experience1 | 12000 |
| Experience2 | 16000 |
| Experience3 | 32000 |
| Delay | 7000 |
| Description | A skill that deals damage to enemies by releasing the energy within your weapon as you thrust it into the ground. |

### #16 · Assault

| 字段 | 值 |
|---|---|
| Name | Assault |
| Magic | Assault |
| Class | Warrior |
| School | Active |
| Property | Augmentation |
| Icon | 216 |
| MinBasePower | 1000 |
| MaxBasePower | 1000 |
| MinLevelPower | 2000 |
| MaxLevelPower | 2000 |
| BaseCost | 0 |
| LevelCost | 50 |
| NeedLevel1 | 50 |
| NeedLevel2 | 53 |
| NeedLevel3 | 56 |
| Experience1 | 10000 |
| Experience2 | 15000 |
| Experience3 | 30000 |
| Delay | 8000 |
| Description | Enemies affected by shoulder dash becomes unconscious. |

### #17 · Endurance

| 字段 | 值 |
|---|---|
| Name | Endurance |
| Magic | Endurance |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 254 |
| MinBasePower | 10 |
| MaxBasePower | 10 |
| MinLevelPower | 10 |
| MaxLevelPower | 10 |
| BaseCost | 20 |
| LevelCost | 40 |
| NeedLevel1 | 51 |
| NeedLevel2 | 55 |
| NeedLevel3 | 59 |
| Experience1 | 10000 |
| Experience2 | 18000 |
| Experience3 | 32000 |
| Delay | 120000 |
| Description | Uses your body's energy to avoid being poisoned, pushed back from enemy attacks, and any other harmful status effects. |

### #18 · Reflect Damage

| 字段 | 值 |
|---|---|
| Name | Reflect Damage |
| Magic | ReflectDamage |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 250 |
| MinBasePower | 35 |
| MaxBasePower | 35 |
| MinLevelPower | 40 |
| MaxLevelPower | 40 |
| BaseCost | 10 |
| LevelCost | 10 |
| NeedLevel1 | 53 |
| NeedLevel2 | 58 |
| NeedLevel3 | 63 |
| Experience1 | 10000 |
| Experience2 | 18000 |
| Experience3 | 32000 |
| Delay | 120000 |
| Description | Returns a percentage of damage done to you back to the attacker. |

### #19 · Fetter

| 字段 | 值 |
|---|---|
| Name | Fetter |
| Magic | Fetter |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 258 |
| MinBasePower | 5000 |
| MaxBasePower | 5000 |
| MinLevelPower | 7000 |
| MaxLevelPower | 7000 |
| BaseCost | 35 |
| LevelCost | 55 |
| NeedLevel1 | 55 |
| NeedLevel2 | 61 |
| NeedLevel3 | 67 |
| Experience1 | 20000 |
| Experience2 | 30000 |
| Experience3 | 40000 |
| Delay | 0 |
| Description | You slow your enemy's movement speed and attack speed using your body's energy. |

### #20 · Advanced Destructive Surge

| 字段 | 值 |
|---|---|
| Name | Advanced Destructive Surge |
| Magic | AugmentDestructiveSurge |
| Class | Warrior |
| School | Toggle |
| Property | Augmentation |
| Icon | 526 |
| MinBasePower | 15 |
| MaxBasePower | 15 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 84 |
| NeedLevel2 | 90 |
| NeedLevel3 | 96 |
| Experience1 | 20000 |
| Experience2 | 30000 |
| Experience3 | 60000 |
| Delay | 0 |
| Description | Provides a wider area of attack for Destructive Surge at level 3 or above, and increases overall power. Damage dealt increases with training. |

### #21 · Advanced Defiance

| 字段 | 值 |
|---|---|
| Name | Advanced Defiance |
| Magic | AugmentDefiance |
| Class | Warrior |
| School | Passive |
| Property | Augmentation |
| Icon | 388 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 80 |
| NeedLevel2 | 82 |
| NeedLevel3 | 84 |
| Experience1 | 3000 |
| Experience2 | 4500 |
| Experience3 | 9000 |
| Delay | 0 |
| Description | Improves Defiance by removing the need to reduce attack power. Duration increases even further with training. |

### #22 · Advanced Reflect Damage

| 字段 | 值 |
|---|---|
| Name | Advanced Reflect Damage |
| Magic | AugmentReflectDamage |
| Class | Warrior |
| School | Passive |
| Property | Augmentation |
| Icon | 458 |
| MinBasePower | 1 |
| MaxBasePower | 1 |
| MinLevelPower | 1 |
| MaxLevelPower | 1 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 82 |
| NeedLevel2 | 82 |
| NeedLevel3 | 82 |
| Experience1 | 20000 |
| Experience2 | 40000 |
| Experience3 | 60000 |
| Delay | 0 |
| Description | Improves Reflect Damage by increasing reflect damage percentage and duration even further with training. |

### #23 · Fire Ball

| 字段 | 值 |
|---|---|
| Name | Fire Ball |
| Magic | FireBall |
| Class | Wizard |
| School | Fire |
| Property | Active |
| Icon | 0 |
| MinBasePower | 0 |
| MaxBasePower | 4 |
| MinLevelPower | 6 |
| MaxLevelPower | 10 |
| BaseCost | 1 |
| LevelCost | 3 |
| NeedLevel1 | 7 |
| NeedLevel2 | 9 |
| NeedLevel3 | 11 |
| Experience1 | 100 |
| Experience2 | 200 |
| Experience3 | 300 |
| Delay | 0 |
| Description | The most basic Fire spell. Its damage increases with training. |

### #24 · Lightning Ball

| 字段 | 值 |
|---|---|
| Name | Lightning Ball |
| Magic | LightningBall |
| Class | Wizard |
| School | Lightning |
| Property | Active |
| Icon | 80 |
| MinBasePower | 0 |
| MaxBasePower | 4 |
| MinLevelPower | 6 |
| MaxLevelPower | 10 |
| BaseCost | 1 |
| LevelCost | 4 |
| NeedLevel1 | 8 |
| NeedLevel2 | 10 |
| NeedLevel3 | 12 |
| Experience1 | 100 |
| Experience2 | 200 |
| Experience3 | 300 |
| Delay | 0 |
| Description | The most basic Lightning spell. Its damage increases with training. |

### #25 · Ice Bolt

| 字段 | 值 |
|---|---|
| Name | Ice Bolt |
| Magic | IceBolt |
| Class | Wizard |
| School | Ice |
| Property | Active |
| Icon | 76 |
| MinBasePower | 0 |
| MaxBasePower | 4 |
| MinLevelPower | 4 |
| MaxLevelPower | 8 |
| BaseCost | 1 |
| LevelCost | 4 |
| NeedLevel1 | 9 |
| NeedLevel2 | 11 |
| NeedLevel3 | 13 |
| Experience1 | 100 |
| Experience2 | 200 |
| Experience3 | 300 |
| Delay | 0 |
| Description | The most basic Ice spell that damages the enemy and slows its movement speed. Its damage increases with training. |

### #26 · Gust Blast

| 字段 | 值 |
|---|---|
| Name | Gust Blast |
| Magic | GustBlast |
| Class | Wizard |
| School | Wind |
| Property | Active |
| Icon | 132 |
| MinBasePower | 0 |
| MaxBasePower | 4 |
| MinLevelPower | 5 |
| MaxLevelPower | 9 |
| BaseCost | 1 |
| LevelCost | 3 |
| NeedLevel1 | 10 |
| NeedLevel2 | 12 |
| NeedLevel3 | 14 |
| Experience1 | 100 |
| Experience2 | 200 |
| Experience3 | 300 |
| Delay | 0 |
| Description | The most basic Wind spell that pushes away enemies. Its damage and chance to push away your enemy increases with training. |

### #27 · Repulsion

| 字段 | 值 |
|---|---|
| Name | Repulsion |
| Magic | Repulsion |
| Class | Wizard |
| School | Wind |
| Property | Active |
| Icon | 14 |
| MinBasePower | 2 |
| MaxBasePower | 3 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 1 |
| LevelCost | 8 |
| NeedLevel1 | 12 |
| NeedLevel2 | 14 |
| NeedLevel3 | 16 |
| Experience1 | 200 |
| Experience2 | 300 |
| Experience3 | 400 |
| Delay | 0 |
| Description | A defensive spell that allows you to push away enemies of lower level. This spell has a fixed chance for success. The range of the spell and chance of success increase with training. |

### #28 · Electric Shock

| 字段 | 值 |
|---|---|
| Name | Electric Shock |
| Magic | ElectricShock |
| Class | Wizard |
| School | Lightning |
| Property | Active |
| Icon | 38 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 3 |
| LevelCost | 3 |
| NeedLevel1 | 13 |
| NeedLevel2 | 15 |
| NeedLevel3 | 17 |
| Experience1 | 200 |
| Experience2 | 300 |
| Experience3 | 400 |
| Delay | 0 |
| Description | A spell that knocks the enemy unconscious.  Your chance of success and duration of its effect increases with training. |

### #29 · Teleportation

| 字段 | 值 |
|---|---|
| Name | Teleportation |
| Magic | Teleportation |
| Class | Wizard |
| School | Phantom |
| Property | Active |
| Icon | 40 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 10 |
| LevelCost | 10 |
| NeedLevel1 | 14 |
| NeedLevel2 | 16 |
| NeedLevel3 | 18 |
| Experience1 | 300 |
| Experience2 | 400 |
| Experience3 | 500 |
| Delay | 7000 |
| Description | A spell that moves you to a random location. Your chance of success increases with training. |

### #30 · Adamantine Fire Ball

| 字段 | 值 |
|---|---|
| Name | Adamantine Fire Ball |
| Magic | AdamantineFireBall |
| Class | Wizard |
| School | Fire |
| Property | Active |
| Icon | 8 |
| MinBasePower | 7 |
| MaxBasePower | 11 |
| MinLevelPower | 15 |
| MaxLevelPower | 19 |
| BaseCost | 6 |
| LevelCost | 6 |
| NeedLevel1 | 15 |
| NeedLevel2 | 17 |
| NeedLevel3 | 19 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 0 |
| Description | A spell that is more powerful than the Fireball. Its damage increases with training. |

### #31 · Thunder Bolt

| 字段 | 值 |
|---|---|
| Name | Thunder Bolt |
| Magic | ThunderBolt |
| Class | Wizard |
| School | Lightning |
| Property | Active |
| Icon | 20 |
| MinBasePower | 7 |
| MaxBasePower | 11 |
| MinLevelPower | 15 |
| MaxLevelPower | 19 |
| BaseCost | 6 |
| LevelCost | 7 |
| NeedLevel1 | 16 |
| NeedLevel2 | 18 |
| NeedLevel3 | 20 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 0 |
| Description | A spell that creates Thunderbolts to strike down on your enemies. Its damage increases with training. |

### #32 · Ice Blades

| 字段 | 值 |
|---|---|
| Name | Ice Blades |
| Magic | IceBlades |
| Class | Wizard |
| School | Ice |
| Property | Active |
| Icon | 78 |
| MinBasePower | 7 |
| MaxBasePower | 11 |
| MinLevelPower | 13 |
| MaxLevelPower | 17 |
| BaseCost | 6 |
| LevelCost | 7 |
| NeedLevel1 | 17 |
| NeedLevel2 | 19 |
| NeedLevel3 | 21 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 0 |
| Description | A spell that is more powerful than the Iceball. Its damage increases with training. |

### #33 · Cyclone

| 字段 | 值 |
|---|---|
| Name | Cyclone |
| Magic | Cyclone |
| Class | Wizard |
| School | Wind |
| Property | Active |
| Icon | 146 |
| MinBasePower | 7 |
| MaxBasePower | 11 |
| MinLevelPower | 14 |
| MaxLevelPower | 18 |
| BaseCost | 6 |
| LevelCost | 6 |
| NeedLevel1 | 18 |
| NeedLevel2 | 20 |
| NeedLevel3 | 22 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 0 |
| Description | A Cyclone attack spell that damages a single enemy. Its damage increases with training. |

### #34 · Scorched Earth

| 字段 | 值 |
|---|---|
| Name | Scorched Earth |
| Magic | ScortchedEarth |
| Class | Wizard |
| School | Fire |
| Property | Active |
| Icon | 16 |
| MinBasePower | 12 |
| MaxBasePower | 16 |
| MinLevelPower | 14 |
| MaxLevelPower | 18 |
| BaseCost | 15 |
| LevelCost | 11 |
| NeedLevel1 | 20 |
| NeedLevel2 | 22 |
| NeedLevel3 | 24 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 700 |
| Delay | 0 |
| Description | A spell that shoots out line of fire along the ground, damaging all enemies caught within the fire. Its damage increases with training. |

### #35 · Lightning Beam

| 字段 | 值 |
|---|---|
| Name | Lightning Beam |
| Magic | LightningBeam |
| Class | Wizard |
| School | Lightning |
| Property | Active |
| Icon | 18 |
| MinBasePower | 12 |
| MaxBasePower | 16 |
| MinLevelPower | 14 |
| MaxLevelPower | 14 |
| BaseCost | 15 |
| LevelCost | 12 |
| NeedLevel1 | 21 |
| NeedLevel2 | 23 |
| NeedLevel3 | 25 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 700 |
| Delay | 0 |
| Description | A spell that shoots out a beam of lightning, damaging all enemies caught within the lightning. Its damage increases with training. |

### #36 · Frozen Earth

| 字段 | 值 |
|---|---|
| Name | Frozen Earth |
| Magic | FrozenEarth |
| Class | Wizard |
| School | Ice |
| Property | Active |
| Icon | 104 |
| MinBasePower | 12 |
| MaxBasePower | 16 |
| MinLevelPower | 12 |
| MaxLevelPower | 16 |
| BaseCost | 15 |
| LevelCost | 12 |
| NeedLevel1 | 22 |
| NeedLevel2 | 24 |
| NeedLevel3 | 26 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 700 |
| Delay | 0 |
| Description | A spell that erects shards of ice from the ground in a line, damaging all enemies in the path. Its damage increases with training. |

### #37 · Blow Earth

| 字段 | 值 |
|---|---|
| Name | Blow Earth |
| Magic | BlowEarth |
| Class | Wizard |
| School | Wind |
| Property | Active |
| Icon | 144 |
| MinBasePower | 12 |
| MaxBasePower | 16 |
| MinLevelPower | 13 |
| MaxLevelPower | 17 |
| BaseCost | 15 |
| LevelCost | 13 |
| NeedLevel1 | 23 |
| NeedLevel2 | 25 |
| NeedLevel3 | 27 |
| Experience1 | 600 |
| Experience2 | 700 |
| Experience3 | 800 |
| Delay | 0 |
| Description | A spell that allows you to harness the power of the winds and cause it to blow in front of the caster in a straight line. Its damage increases with training. |

### #38 · Fire Wall

| 字段 | 值 |
|---|---|
| Name | Fire Wall |
| Magic | FireWall |
| Class | Wizard |
| School | Fire |
| Property | Active |
| Icon | 42 |
| MinBasePower | 1 |
| MaxBasePower | 6 |
| MinLevelPower | 2 |
| MaxLevelPower | 9 |
| BaseCost | 30 |
| LevelCost | 22 |
| NeedLevel1 | 24 |
| NeedLevel2 | 26 |
| NeedLevel3 | 28 |
| Experience1 | 600 |
| Experience2 | 700 |
| Experience3 | 800 |
| Delay | 0 |
| Description | A spell that creates a wall of fire in the shape of a cross damaging enemies over a period of time. Its damage increases with training. |

### #39 · Expel Undead

| 字段 | 值 |
|---|---|
| Name | Expel Undead |
| Magic | ExpelUndead |
| Class | Wizard |
| School | Phantom |
| Property | Active |
| Icon | 62 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 30 |
| LevelCost | 30 |
| NeedLevel1 | 26 |
| NeedLevel2 | 28 |
| NeedLevel3 | 30 |
| Experience1 | 700 |
| Experience2 | 800 |
| Experience3 | 900 |
| Delay | 0 |
| Description | A spell that instantly kills undead enemies. Its chance of success increases with training. |

### #40 · Geo Manipulation

| 字段 | 值 |
|---|---|
| Name | Geo Manipulation |
| Magic | GeoManipulation |
| Class | Wizard |
| School | Phantom |
| Property | Active |
| Icon | 206 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 20 |
| LevelCost | 25 |
| NeedLevel1 | 27 |
| NeedLevel2 | 29 |
| NeedLevel3 | 31 |
| Experience1 | 800 |
| Experience2 | 900 |
| Experience3 | 1000 |
| Delay | 5000 |
| Description | A spell that harnesses the body's energy to move to a desired location. The chance of success increases with training. |

### #41 · Magic Shield

| 字段 | 值 |
|---|---|
| Name | Magic Shield |
| Magic | MagicShield |
| Class | Wizard |
| School | Phantom |
| Property | Active |
| Icon | 60 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 30 |
| LevelCost | 20 |
| NeedLevel1 | 29 |
| NeedLevel2 | 31 |
| NeedLevel3 | 33 |
| Experience1 | 900 |
| Experience2 | 1000 |
| Experience3 | 1100 |
| Delay | 0 |
| Description | Creates a magical shield around the caster that reduces the damage taken for a set period of time. The duration of the spell increases with training. |

### #42 · Fire Storm

| 字段 | 值 |
|---|---|
| Name | Fire Storm |
| Magic | FireStorm |
| Class | Wizard |
| School | Fire |
| Property | Active |
| Icon | 44 |
| MinBasePower | 14 |
| MaxBasePower | 18 |
| MinLevelPower | 14 |
| MaxLevelPower | 18 |
| BaseCost | 20 |
| LevelCost | 15 |
| NeedLevel1 | 32 |
| NeedLevel2 | 34 |
| NeedLevel3 | 36 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 0 |
| Description | A spell that deals fire damage to all enemies within a fixed area(3x3) of choice. Its damage increases with training. |

### #43 · Lightning Wave

| 字段 | 值 |
|---|---|
| Name | Lightning Wave |
| Magic | LightningWave |
| Class | Wizard |
| School | Lightning |
| Property | Active |
| Icon | 46 |
| MinBasePower | 14 |
| MaxBasePower | 18 |
| MinLevelPower | 14 |
| MaxLevelPower | 18 |
| BaseCost | 20 |
| LevelCost | 17 |
| NeedLevel1 | 33 |
| NeedLevel2 | 35 |
| NeedLevel3 | 37 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 0 |
| Description | A spell that deals lightning damage to all enemies within a fixed area(3x3) of choice. Its damage increases with training. |

### #44 · Ice Storm

| 字段 | 值 |
|---|---|
| Name | Ice Storm |
| Magic | IceStorm |
| Class | Wizard |
| School | Ice |
| Property | Active |
| Icon | 64 |
| MinBasePower | 14 |
| MaxBasePower | 18 |
| MinLevelPower | 12 |
| MaxLevelPower | 16 |
| BaseCost | 20 |
| LevelCost | 19 |
| NeedLevel1 | 34 |
| NeedLevel2 | 36 |
| NeedLevel3 | 38 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 0 |
| Description | A spell that deals ice damage to all enemies within a fixed area(3x3) of choice with ice. Its damage increases with training. |

### #45 · Dragon Tornado

| 字段 | 值 |
|---|---|
| Name | Dragon Tornado |
| Magic | DragonTornado |
| Class | Wizard |
| School | Wind |
| Property | Active |
| Icon | 142 |
| MinBasePower | 14 |
| MaxBasePower | 18 |
| MinLevelPower | 13 |
| MaxLevelPower | 17 |
| BaseCost | 20 |
| LevelCost | 18 |
| NeedLevel1 | 35 |
| NeedLevel2 | 37 |
| NeedLevel3 | 39 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 0 |
| Description | A spell that attacks all enemies within a fixed area(3x3) of choice with a cyclone. Its damage increases with training. |

### #46 · Greater Frozen Earth

| 字段 | 值 |
|---|---|
| Name | Greater Frozen Earth |
| Magic | GreaterFrozenEarth |
| Class | Wizard |
| School | Ice |
| Property | Active |
| Icon | 218 |
| MinBasePower | 16 |
| MaxBasePower | 20 |
| MinLevelPower | 16 |
| MaxLevelPower | 20 |
| BaseCost | 20 |
| LevelCost | 20 |
| NeedLevel1 | 38 |
| NeedLevel2 | 41 |
| NeedLevel3 | 44 |
| Experience1 | 1000 |
| Experience2 | 1500 |
| Experience3 | 3000 |
| Delay | 0 |
| Description | A spell that erects 3 lines of ice shards from the ground in three directions to damage and slow the enemies. Its damage increases with training. |

### #47 · Chain Lightning

| 字段 | 值 |
|---|---|
| Name | Chain Lightning |
| Magic | ChainLightning |
| Class | Wizard |
| School | Lightning |
| Property | Active |
| Icon | 220 |
| MinBasePower | 20 |
| MaxBasePower | 30 |
| MinLevelPower | 20 |
| MaxLevelPower | 40 |
| BaseCost | 30 |
| LevelCost | 40 |
| NeedLevel1 | 40 |
| NeedLevel2 | 42 |
| NeedLevel3 | 44 |
| Experience1 | 2000 |
| Experience2 | 3000 |
| Experience3 | 6000 |
| Delay | 0 |
| Description | A spell that inflicts lightning damage to enemies in a large area(9x9), but the damage inflicted decreases as the lightning spreads out from its attack point. Its damage increases with training. |

### #48 · Meteor Shower

| 字段 | 值 |
|---|---|
| Name | Meteor Shower |
| Magic | MeteorShower |
| Class | Wizard |
| School | Fire |
| Property | Active |
| Icon | 224 |
| MinBasePower | 14 |
| MaxBasePower | 22 |
| MinLevelPower | 32 |
| MaxLevelPower | 40 |
| BaseCost | 40 |
| LevelCost | 38 |
| NeedLevel1 | 43 |
| NeedLevel2 | 45 |
| NeedLevel3 | 47 |
| Experience1 | 5000 |
| Experience2 | 7500 |
| Experience3 | 15000 |
| Delay | 0 |
| Description | A spell that damages enemies within a set range. Its damage and the number of enemies affected increases with training. |

### #49 · Renounce

| 字段 | 值 |
|---|---|
| Name | Renounce |
| Magic | Renounce |
| Class | Wizard |
| School | Phantom |
| Property | Active |
| Icon | 222 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 10 |
| LevelCost | 60 |
| NeedLevel1 | 46 |
| NeedLevel2 | 48 |
| NeedLevel3 | 50 |
| Experience1 | 8000 |
| Experience2 | 12000 |
| Experience3 | 24000 |
| Delay | 0 |
| Description | A spell that sacrifices your HP to increase your Spell Power for a fixed period of time.  The Spell Power bonus increases with training. |

### #50 · Tempest

| 字段 | 值 |
|---|---|
| Name | Tempest |
| Magic | Tempest |
| Class | Wizard |
| School | Wind |
| Property | Active |
| Icon | 226 |
| MinBasePower | 3 |
| MaxBasePower | 8 |
| MinLevelPower | 4 |
| MaxLevelPower | 11 |
| BaseCost | 40 |
| LevelCost | 30 |
| NeedLevel1 | 49 |
| NeedLevel2 | 51 |
| NeedLevel3 | 53 |
| Experience1 | 10000 |
| Experience2 | 15000 |
| Experience3 | 30000 |
| Delay | 0 |
| Description | A spell that creates a Cyclone to attack enemies in a set area(3x3) for a set period of time. Its damage and duration increases with training. |

### #51 · Judgement Of Heaven

| 字段 | 值 |
|---|---|
| Name | Judgement Of Heaven |
| Magic | JudgementOfHeaven |
| Class | Wizard |
| School | Lightning |
| Property | Active |
| Icon | 264 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 40 |
| LevelCost | 30 |
| NeedLevel1 | 52 |
| NeedLevel2 | 57 |
| NeedLevel3 | 62 |
| Experience1 | 20000 |
| Experience2 | 30000 |
| Experience3 | 50000 |
| Delay | 0 |
| Description | The enemy that attacks you gets struck by lightning each time you take damage. |

### #52 · Thunder Storm

| 字段 | 值 |
|---|---|
| Name | Thunder Storm |
| Magic | ThunderStrike |
| Class | Wizard |
| School | Lightning |
| Property | Active |
| Icon | 266 |
| MinBasePower | 14 |
| MaxBasePower | 18 |
| MinLevelPower | 14 |
| MaxLevelPower | 18 |
| BaseCost | 30 |
| LevelCost | 70 |
| NeedLevel1 | 54 |
| NeedLevel2 | 59 |
| NeedLevel3 | 64 |
| Experience1 | 15000 |
| Experience2 | 20000 |
| Experience3 | 30000 |
| Delay | 0 |
| Description | When cast, lightning strikes down randomly in a fixed area, dealing massive damage. |

### #53 · Fire Bounce

| 字段 | 值 |
|---|---|
| Name | Fire Bounce |
| Magic | FireBounce |
| Class | Wizard |
| School | None |
| Property | Active |
| Icon | 0 |
| MinBasePower | 7 |
| MaxBasePower | 11 |
| MinLevelPower | 15 |
| MaxLevelPower | 19 |
| BaseCost | 6 |
| LevelCost | 6 |
| NeedLevel1 | 15 |
| NeedLevel2 | 17 |
| NeedLevel3 | 19 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 0 |
| Description | Bounces a powerful fireball between close targets. The number of bounces increase with training. |

### #54 · Elemental Hurricane

| 字段 | 值 |
|---|---|
| Name | Elemental Hurricane |
| Magic | ElementalHurricane |
| Class | Wizard |
| School | Wind |
| Property | Active |
| Icon | 436 |
| MinBasePower | 16 |
| MaxBasePower | 24 |
| MinLevelPower | 15 |
| MaxLevelPower | 18 |
| BaseCost | 20 |
| LevelCost | 35 |
| NeedLevel1 | 83 |
| NeedLevel2 | 85 |
| NeedLevel3 | 87 |
| Experience1 | 15000 |
| Experience2 | 30000 |
| Experience3 | 45000 |
| Delay | 0 |
| Description | You condense your body's energy and burst it in a line, dealing damage to anyone caught in its path. The energy does not have any properties unless used with a dark stone. |

### #55 · Superior Magic Shield

| 字段 | 值 |
|---|---|
| Name | Superior Magic Shield |
| Magic | SuperiorMagicShield |
| Class | Wizard |
| School | Phantom |
| Property | Active |
| Icon | 444 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 20 |
| LevelCost | 60 |
| NeedLevel1 | 65 |
| NeedLevel2 | 70 |
| NeedLevel3 | 75 |
| Experience1 | 2000 |
| Experience2 | 4000 |
| Experience3 | 6000 |
| Delay | 0 |
| Description | Creates a barrier around the caster that blocks all damage taken until depleted. The amount of damage absorbed increases with training. |

### #56 · Burning

| 字段 | 值 |
|---|---|
| Name | Burning |
| Magic | Burning |
| Class | Wizard |
| School | Fire |
| Property | Augmentation |
| Icon | 484 |
| MinBasePower | 12 |
| MaxBasePower | 18 |
| MinLevelPower | 12 |
| MaxLevelPower | 16 |
| BaseCost | 20 |
| LevelCost | 120 |
| NeedLevel1 | 76 |
| NeedLevel2 | 80 |
| NeedLevel3 | 86 |
| Experience1 | 40000 |
| Experience2 | 60000 |
| Experience3 | 80000 |
| Delay | 0 |
| Description | Ignites the enemy and inflicts a continuous fire attack for a certain period of time. Passively applies to all fire skills. Damage and duration increase with training. |

### #57 · Shock

| 字段 | 值 |
|---|---|
| Name | Shock |
| Magic | Shocked |
| Class | Wizard |
| School | Lightning |
| Property | Augmentation |
| Icon | 532 |
| MinBasePower | 20 |
| MaxBasePower | 30 |
| MinLevelPower | 18 |
| MaxLevelPower | 24 |
| BaseCost | 20 |
| LevelCost | 45 |
| NeedLevel1 | 85 |
| NeedLevel2 | 88 |
| NeedLevel3 | 91 |
| Experience1 | 450 |
| Experience2 | 1000 |
| Experience3 | 1650 |
| Delay | 0 |
| Description | Put the enemy in a state of temporary shock for a certain period of time. Passively applies to all lightning skills. Success and duration increases with training. |

### #58 · Lightning Strike

| 字段 | 值 |
|---|---|
| Name | Lightning Strike |
| Magic | LightningStrike |
| Class | Wizard |
| School | Lightning |
| Property | Active |
| Icon | 452 |
| MinBasePower | 16 |
| MaxBasePower | 22 |
| MinLevelPower | 10 |
| MaxLevelPower | 14 |
| BaseCost | 50 |
| LevelCost | 45 |
| NeedLevel1 | 90 |
| NeedLevel2 | 90 |
| NeedLevel3 | 90 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 30000 |
| Delay | 0 |
| Description | A spell that bounces a powerful elecric shock between close targets, getting stronger each hit. Number of bounces and power multiplier increases with training. |

### #59 · Heal

| 字段 | 值 |
|---|---|
| Name | Heal |
| Magic | Heal |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 2 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 11 |
| MaxLevelPower | 15 |
| BaseCost | 2 |
| LevelCost | 7 |
| NeedLevel1 | 7 |
| NeedLevel2 | 9 |
| NeedLevel3 | 11 |
| Experience1 | 100 |
| Experience2 | 200 |
| Experience3 | 300 |
| Delay | 0 |
| Description | A spell to heal yourself or others by using your body's energy. The amount healed increases with training. |

### #60 · Spirit Sword

| 字段 | 值 |
|---|---|
| Name | Spirit Sword |
| Magic | SpiritSword |
| Class | Taoist |
| School | Physical |
| Property | Passive |
| Icon | 6 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 9 |
| MaxLevelPower | 9 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 8 |
| NeedLevel2 | 10 |
| NeedLevel3 | 12 |
| Experience1 | 100 |
| Experience2 | 200 |
| Experience3 | 300 |
| Delay | 0 |
| Description | A skill that increases your Accuracy. The Accuracy bonus increases with training. |

### #61 · Poison Dust

| 字段 | 值 |
|---|---|
| Name | Poison Dust |
| Magic | PoisonDust |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 10 |
| MinBasePower | 15 |
| MaxBasePower | 25 |
| MinLevelPower | 25 |
| MaxLevelPower | 55 |
| BaseCost | 5 |
| LevelCost | 10 |
| NeedLevel1 | 12 |
| NeedLevel2 | 14 |
| NeedLevel3 | 16 |
| Experience1 | 200 |
| Experience2 | 300 |
| Experience3 | 400 |
| Delay | 0 |
| Description | A spell that decreases your enemy's defense or strength, depending on the Poison Powder, for a set period of time. The effect's duration increases with training. |

### #62 · Explosive Talisman

| 字段 | 值 |
|---|---|
| Name | Explosive Talisman |
| Magic | ExplosiveTalisman |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 24 |
| MinBasePower | 3 |
| MaxBasePower | 3 |
| MinLevelPower | 6 |
| MaxLevelPower | 10 |
| BaseCost | 3 |
| LevelCost | 6 |
| NeedLevel1 | 13 |
| NeedLevel2 | 15 |
| NeedLevel3 | 17 |
| Experience1 | 200 |
| Experience2 | 300 |
| Experience3 | 400 |
| Delay | 0 |
| Description | Throws an explosive talisman at the enemy. Its power is increased when used with a Dark Talisman.  Its damage increases with training. |

### #63 · Evil Slayer

| 字段 | 值 |
|---|---|
| Name | Evil Slayer |
| Magic | EvilSlayer |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 72 |
| MinBasePower | 3 |
| MaxBasePower | 3 |
| MinLevelPower | 6 |
| MaxLevelPower | 10 |
| BaseCost | 3 |
| LevelCost | 6 |
| NeedLevel1 | 14 |
| NeedLevel2 | 16 |
| NeedLevel3 | 18 |
| Experience1 | 300 |
| Experience2 | 400 |
| Experience3 | 500 |
| Delay | 0 |
| Description | Harness the holy energy in your hands to throw at your enemy. Its power is increased when used with a Holy Talisman. Its damage increases with training. |

### #64 · Invisibility

| 字段 | 值 |
|---|---|
| Name | Invisibility |
| Magic | Invisibility |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 34 |
| MinBasePower | 5 |
| MaxBasePower | 10 |
| MinLevelPower | 5 |
| MaxLevelPower | 15 |
| BaseCost | 5 |
| LevelCost | 5 |
| NeedLevel1 | 20 |
| NeedLevel2 | 22 |
| NeedLevel3 | 24 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 700 |
| Delay | 0 |
| Description | Temporarily hide yourself, vanishing from your enemy's sight.  Its duration increases with training. |

### #65 · Magic Resistance

| 字段 | 值 |
|---|---|
| Name | Magic Resistance |
| Magic | MagicResistance |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 26 |
| MinBasePower | 30 |
| MaxBasePower | 50 |
| MinLevelPower | 40 |
| MaxLevelPower | 120 |
| BaseCost | 5 |
| LevelCost | 10 |
| NeedLevel1 | 21 |
| NeedLevel2 | 23 |
| NeedLevel3 | 25 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 700 |
| Delay | 0 |
| Description | Using your body's energy, you increase your resistance to magic. The resistance bonus and duration of the effect increases with training. |

### #66 · Mass Invisibility

| 字段 | 值 |
|---|---|
| Name | Mass Invisibility |
| Magic | MassInvisibility |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 36 |
| MinBasePower | 5 |
| MaxBasePower | 10 |
| MinLevelPower | 5 |
| MaxLevelPower | 15 |
| BaseCost | 5 |
| LevelCost | 10 |
| NeedLevel1 | 23 |
| NeedLevel2 | 25 |
| NeedLevel3 | 27 |
| Experience1 | 600 |
| Experience2 | 700 |
| Experience3 | 800 |
| Delay | 0 |
| Description | An advanced invisibility technique that allows you to hide everyone in a fixed area(3x3). Its duration increases with training. |

### #67 · Greater Evil Slayer

| 字段 | 值 |
|---|---|
| Name | Greater Evil Slayer |
| Magic | GreaterEvilSlayer |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 74 |
| MinBasePower | 7 |
| MaxBasePower | 7 |
| MinLevelPower | 8 |
| MaxLevelPower | 14 |
| BaseCost | 4 |
| LevelCost | 8 |
| NeedLevel1 | 24 |
| NeedLevel2 | 26 |
| NeedLevel3 | 28 |
| Experience1 | 600 |
| Experience2 | 700 |
| Experience3 | 800 |
| Delay | 0 |
| Description | A spell that is more powerful than the Holy Strike. Its damage increases with training, and even more so if used with a Holy Talisman. |

### #68 · Resilience

| 字段 | 值 |
|---|---|
| Name | Resilience |
| Magic | Resilience |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 28 |
| MinBasePower | 30 |
| MaxBasePower | 50 |
| MinLevelPower | 40 |
| MaxLevelPower | 120 |
| BaseCost | 5 |
| LevelCost | 10 |
| NeedLevel1 | 25 |
| NeedLevel2 | 27 |
| NeedLevel3 | 29 |
| Experience1 | 700 |
| Experience2 | 800 |
| Experience3 | 900 |
| Delay | 0 |
| Description | Using your body's energy, you increase your defense against physical attacks. Its duration and defense bonus increases with training. |

### #69 · Trap Octagon

| 字段 | 值 |
|---|---|
| Name | Trap Octagon |
| Magic | TrapOctagon |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 30 |
| MinBasePower | 10 |
| MaxBasePower | 20 |
| MinLevelPower | 10 |
| MaxLevelPower | 20 |
| BaseCost | 10 |
| LevelCost | 15 |
| NeedLevel1 | 27 |
| NeedLevel2 | 29 |
| NeedLevel3 | 31 |
| Experience1 | 800 |
| Experience2 | 900 |
| Experience3 | 1000 |
| Delay | 0 |
| Description | A spell that uses a talisman to trap enemies in a small area. Its duration increases with training. |

### #70 · Combat Kick

| 字段 | 值 |
|---|---|
| Name | Combat Kick |
| Magic | CombatKick |
| Class | Taoist |
| School | Physical |
| Property | Active |
| Icon | 70 |
| MinBasePower | 2 |
| MaxBasePower | 3 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 10 |
| LevelCost | 20 |
| NeedLevel1 | 28 |
| NeedLevel2 | 30 |
| NeedLevel3 | 32 |
| Experience1 | 900 |
| Experience2 | 1000 |
| Experience3 | 1100 |
| Delay | 0 |
| Description | Kick your enemies away from you. Its damage and chance to push back your enemies increases with training. |

### #71 · Elemental Superiority

| 字段 | 值 |
|---|---|
| Name | Elemental Superiority |
| Magic | ElementalSuperiority |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 176 |
| MinBasePower | 30 |
| MaxBasePower | 50 |
| MinLevelPower | 40 |
| MaxLevelPower | 120 |
| BaseCost | 5 |
| LevelCost | 10 |
| NeedLevel1 | 29 |
| NeedLevel2 | 31 |
| NeedLevel3 | 33 |
| Experience1 | 900 |
| Experience2 | 1000 |
| Experience3 | 1100 |
| Delay | 0 |
| Description | Use a talisman to increase the spell power of yourself or others, or use the elemental talisman to increase the attack elements.  Its duration increases with training. |

### #72 · Mass Heal

| 字段 | 值 |
|---|---|
| Name | Mass Heal |
| Magic | MassHeal |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 56 |
| MinBasePower | 8 |
| MaxBasePower | 8 |
| MinLevelPower | 16 |
| MaxLevelPower | 24 |
| BaseCost | 20 |
| LevelCost | 10 |
| NeedLevel1 | 31 |
| NeedLevel2 | 33 |
| NeedLevel3 | 35 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 0 |
| Description | A spell that heals multiple people within a fixed area(5x5) tiles at the same time. The amount healed increases with training. |

### #73 · Blood Lust

| 字段 | 值 |
|---|---|
| Name | Blood Lust |
| Magic | BloodLust |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 186 |
| MinBasePower | 30 |
| MaxBasePower | 50 |
| MinLevelPower | 40 |
| MaxLevelPower | 120 |
| BaseCost | 5 |
| LevelCost | 10 |
| NeedLevel1 | 34 |
| NeedLevel2 | 36 |
| NeedLevel3 | 38 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 0 |
| Description | Use your body's energy to increase the attack of yourself or others. Its duration and damage bonus increases with training. |

### #74 · Resurrection

| 字段 | 值 |
|---|---|
| Name | Resurrection |
| Magic | Resurrection |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 152 |
| MinBasePower | 10 |
| MaxBasePower | 20 |
| MinLevelPower | 15 |
| MaxLevelPower | 30 |
| BaseCost | 100 |
| LevelCost | 100 |
| NeedLevel1 | 35 |
| NeedLevel2 | 37 |
| NeedLevel3 | 39 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 700 |
| Delay | 0 |
| Description | Revive the dead by using a Talisman of Soul. The chance to revive increases with training. |

### #75 · Purification

| 字段 | 值 |
|---|---|
| Name | Purification |
| Magic | Purification |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 238 |
| MinBasePower | 10 |
| MaxBasePower | 20 |
| MinLevelPower | 15 |
| MaxLevelPower | 30 |
| BaseCost | 10 |
| LevelCost | 20 |
| NeedLevel1 | 38 |
| NeedLevel2 | 41 |
| NeedLevel3 | 44 |
| Experience1 | 1000 |
| Experience2 | 1500 |
| Experience3 | 3000 |
| Delay | 0 |
| Description | Removes all magical and skill effects. Not only does it remove poison and paralysis, but any ability enhancement effects also. |

### #76 · Transparency

| 字段 | 值 |
|---|---|
| Name | Transparency |
| Magic | Transparency |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 240 |
| MinBasePower | 5 |
| MaxBasePower | 10 |
| MinLevelPower | 5 |
| MaxLevelPower | 15 |
| BaseCost | 80 |
| LevelCost | 120 |
| NeedLevel1 | 43 |
| NeedLevel2 | 45 |
| NeedLevel3 | 47 |
| Experience1 | 5000 |
| Experience2 | 7500 |
| Experience3 | 15000 |
| Delay | 5000 |
| Description | You become invisible to monsters and other players. Its duration increases with training. |

### #77 · Celestial Light

| 字段 | 值 |
|---|---|
| Name | Celestial Light |
| Magic | CelestialLight |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 242 |
| MinBasePower | 30 |
| MaxBasePower | 30 |
| MinLevelPower | 30 |
| MaxLevelPower | 30 |
| BaseCost | 50 |
| LevelCost | 60 |
| NeedLevel1 | 46 |
| NeedLevel2 | 48 |
| NeedLevel3 | 50 |
| Experience1 | 8000 |
| Experience2 | 12000 |
| Experience3 | 24000 |
| Delay | 0 |
| Description | Draws upon your remaining bit of energy to block an enemy attack that would otherwise kill you. Also restores a small amount of HP. |

### #78 · Empowered Healing

| 字段 | 值 |
|---|---|
| Name | Empowered Healing |
| Magic | EmpoweredHealing |
| Class | Taoist |
| School | Holy |
| Property | Augmentation |
| Icon | 256 |
| MinBasePower | 12 |
| MaxBasePower | 12 |
| MinLevelPower | 24 |
| MaxLevelPower | 32 |
| BaseCost | 2 |
| LevelCost | 7 |
| NeedLevel1 | 47 |
| NeedLevel2 | 53 |
| NeedLevel3 | 60 |
| Experience1 | 10000 |
| Experience2 | 18000 |
| Experience3 | 32000 |
| Delay | 0 |
| Description | Increases the amount healed by your next healing spell. The bonus amount healed depends on your level and training. |

### #79 · Life Steal

| 字段 | 值 |
|---|---|
| Name | Life Steal |
| Magic | LifeSteal |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 270 |
| MinBasePower | 30 |
| MaxBasePower | 30 |
| MinLevelPower | 60 |
| MaxLevelPower | 60 |
| BaseCost | 10 |
| LevelCost | 25 |
| NeedLevel1 | 48 |
| NeedLevel2 | 55 |
| NeedLevel3 | 62 |
| Experience1 | 10000 |
| Experience2 | 18000 |
| Experience3 | 32000 |
| Delay | 0 |
| Description | Use your body's energy to increase the Life stealing of yourself or others. Its duration and Life steal bonus increases with training. |

### #80 · Improved Explosive Talisman

| 字段 | 值 |
|---|---|
| Name | Improved Explosive Talisman |
| Magic | ImprovedExplosiveTalisman |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 246 |
| MinBasePower | 8 |
| MaxBasePower | 8 |
| MinLevelPower | 12 |
| MaxLevelPower | 20 |
| BaseCost | 10 |
| LevelCost | 18 |
| NeedLevel1 | 49 |
| NeedLevel2 | 51 |
| NeedLevel3 | 53 |
| Experience1 | 10000 |
| Experience2 | 15000 |
| Experience3 | 30000 |
| Delay | 0 |
| Description | An upgraded version of the Explosive Talisman, used to throw 3 explosive talismans to deal massive amounts of damage. Its damage increases if used with Dark Talisman. |

### #81 · Empowered Poison Dust

| 字段 | 值 |
|---|---|
| Name | Empowered Poison Dust |
| Magic | AugmentPoisonDust |
| Class | Taoist |
| School | Dark |
| Property | Augmentation |
| Icon | 268 |
| MinBasePower | 3 |
| MaxBasePower | 3 |
| MinLevelPower | 9 |
| MaxLevelPower | 9 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 50 |
| NeedLevel2 | 54 |
| NeedLevel3 | 58 |
| Experience1 | 30000 |
| Experience2 | 33000 |
| Experience3 | 36000 |
| Delay | 5000 |
| Description | Augment's Poison dust, When casting poison dust there is a chance it will effect nearby enemies. |

### #82 · Cursed Doll

| 字段 | 值 |
|---|---|
| Name | Cursed Doll |
| Magic | CursedDoll |
| Class | Taoist |
| School | Phantom |
| Property | Active |
| Icon | 272 |
| MinBasePower | 10 |
| MaxBasePower | 10 |
| MinLevelPower | 20 |
| MaxLevelPower | 20 |
| BaseCost | 15 |
| LevelCost | 50 |
| NeedLevel1 | 52 |
| NeedLevel2 | 56 |
| NeedLevel3 | 61 |
| Experience1 | 10000 |
| Experience2 | 15000 |
| Experience3 | 25000 |
| Delay | 0 |
| Description | Create a dummy that resembles your target by cursing them with a Talisman of Soul. Damage done to this dummy will damage your enemy instead. Duration of curse increases with training. |

### #83 · Thunder Kick

| 字段 | 值 |
|---|---|
| Name | Thunder Kick |
| Magic | ThunderKick |
| Class | Taoist |
| School | Physical |
| Property | Active |
| Icon | 248 |
| MinBasePower | 2 |
| MaxBasePower | 3 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 10 |
| LevelCost | 20 |
| NeedLevel1 | 54 |
| NeedLevel2 | 59 |
| NeedLevel3 | 64 |
| Experience1 | 10000 |
| Experience2 | 18000 |
| Experience3 | 32000 |
| Delay | 0 |
| Description | When you kick an enemy, your target enemy remains in place, but the surrounding enemies gets pushed back or receives a large amount of damage. |

### #84 · Soul Resonance

| 字段 | 值 |
|---|---|
| Name | Soul Resonance |
| Magic | SoulResonance |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 482 |
| MinBasePower | 20 |
| MaxBasePower | 20 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 55 |
| LevelCost | 60 |
| NeedLevel1 | 84 |
| NeedLevel2 | 84 |
| NeedLevel3 | 84 |
| Experience1 | 0 |
| Experience2 | 0 |
| Experience3 | 0 |
| Delay | 0 |
| Description | Resonates your soul with the target to increase both of your healths. If either of you die the other does too. Only available when in a group. |

### #85 · Parasite

| 字段 | 值 |
|---|---|
| Name | Parasite |
| Magic | Parasite |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 396 |
| MinBasePower | 2 |
| MaxBasePower | 2 |
| MinLevelPower | 1 |
| MaxLevelPower | 1 |
| BaseCost | 80 |
| LevelCost | 150 |
| NeedLevel1 | 62 |
| NeedLevel2 | 66 |
| NeedLevel3 | 70 |
| Experience1 | 7500 |
| Experience2 | 10000 |
| Experience3 | 13000 |
| Delay | 0 |
| Description | Attaches a dark parasite to the target which slowly drains their health, then eventually explodes and damages surrounding enemies. Duration of parasite increases with training. |

### #86 · Spiritualism

| 字段 | 值 |
|---|---|
| Name | Spiritualism |
| Magic | Spiritualism |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 394 |
| MinBasePower | 1 |
| MaxBasePower | 1 |
| MinLevelPower | 1 |
| MaxLevelPower | 1 |
| BaseCost | 15 |
| LevelCost | 5 |
| NeedLevel1 | 80 |
| NeedLevel2 | 82 |
| NeedLevel3 | 84 |
| Experience1 | 5500 |
| Experience2 | 7000 |
| Experience3 | 9000 |
| Delay | 0 |
| Description | Using the energy of your talisman you increase your elemental defense strength. Its duration and ability increases with training. |

### #87 · Willow Dance

| 字段 | 值 |
|---|---|
| Name | Willow Dance |
| Magic | WillowDance |
| Class | Assassin |
| School | Atrocity |
| Property | Passive |
| Icon | 308 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 6 |
| MaxLevelPower | 6 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 7 |
| NeedLevel2 | 9 |
| NeedLevel3 | 11 |
| Experience1 | 100 |
| Experience2 | 200 |
| Experience3 | 300 |
| Delay | 0 |
| Description | Increases your Agility. Your Agility bonus increases with training. |

### #88 · Vine Tree Dance

| 字段 | 值 |
|---|---|
| Name | Vine Tree Dance |
| Magic | VineTreeDance |
| Class | Assassin |
| School | Atrocity |
| Property | Passive |
| Icon | 310 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 9 |
| MaxLevelPower | 9 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 10 |
| NeedLevel2 | 12 |
| NeedLevel3 | 14 |
| Experience1 | 250 |
| Experience2 | 350 |
| Experience3 | 450 |
| Delay | 0 |
| Description | Increases your Accuracy. Your accuracy bonus increases with training. |

### #89 · Discipline

| 字段 | 值 |
|---|---|
| Name | Discipline |
| Magic | Discipline |
| Class | Assassin |
| School | Atrocity |
| Property | Passive |
| Icon | 314 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 12 |
| MaxLevelPower | 12 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 12 |
| NeedLevel2 | 14 |
| NeedLevel3 | 16 |
| Experience1 | 300 |
| Experience2 | 400 |
| Experience3 | 500 |
| Delay | 0 |
| Description | Increases your accuracy and minimum damage. Accuracy and minimum damage bonus increases with training. |

### #90 · Poisonous Cloud

| 字段 | 值 |
|---|---|
| Name | Poisonous Cloud |
| Magic | PoisonousCloud |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 312 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 10 |
| MaxLevelPower | 10 |
| BaseCost | 5 |
| LevelCost | 20 |
| NeedLevel1 | 14 |
| NeedLevel2 | 16 |
| NeedLevel3 | 18 |
| Experience1 | 300 |
| Experience2 | 400 |
| Experience3 | 500 |
| Delay | 20000 |
| Description | Creates a cloud of poison around you, increasing your dodge rate while standing in the clouds. |

### #91 · Full Bloom

| 字段 | 值 |
|---|---|
| Name | Full Bloom |
| Magic | FullBloom |
| Class | Assassin |
| School | Kill |
| Property | Charge |
| Icon | 328 |
| MinBasePower | 85 |
| MaxBasePower | 85 |
| MinLevelPower | 85 |
| MaxLevelPower | 85 |
| BaseCost | 2 |
| LevelCost | 4 |
| NeedLevel1 | 19 |
| NeedLevel2 | 21 |
| NeedLevel3 | 23 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 3000 |
| Description | Converts a percentage of your max mana into additional physical damage. |

### #92 · Cloak

| 字段 | 值 |
|---|---|
| Name | Cloak |
| Magic | Cloak |
| Class | Assassin |
| School | Assassination |
| Property | Active |
| Icon | 324 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 50 |
| LevelCost | 20 |
| NeedLevel1 | 20 |
| NeedLevel2 | 22 |
| NeedLevel3 | 24 |
| Experience1 | 200 |
| Experience2 | 300 |
| Experience3 | 400 |
| Delay | 0 |
| Description | You become invisible. While in this state, you can move, attack, and use items at the expense of your HP.  You become visible when you attack or use an item. The amount of HP used decreases with training. |

### #93 · White Lotus

| 字段 | 值 |
|---|---|
| Name | White Lotus |
| Magic | WhiteLotus |
| Class | Assassin |
| School | Kill |
| Property | Charge |
| Icon | 330 |
| MinBasePower | 65 |
| MaxBasePower | 65 |
| MinLevelPower | 45 |
| MaxLevelPower | 45 |
| BaseCost | 3 |
| LevelCost | 5 |
| NeedLevel1 | 22 |
| NeedLevel2 | 24 |
| NeedLevel3 | 26 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 700 |
| Delay | 3000 |
| Description | Converts a percentage of your max mana into additional physical damage. Enemies damaged by Full Bloom receives additional damage. |

### #94 · Calamity Of Full Moon

| 字段 | 值 |
|---|---|
| Name | Calamity Of Full Moon |
| Magic | CalamityOfFullMoon |
| Class | Assassin |
| School | Kill |
| Property | Passive |
| Icon | 340 |
| MinBasePower | 15 |
| MaxBasePower | 15 |
| MinLevelPower | 15 |
| MaxLevelPower | 15 |
| BaseCost | 4 |
| LevelCost | 6 |
| NeedLevel1 | 22 |
| NeedLevel2 | 24 |
| NeedLevel3 | 26 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 700 |
| Delay | 0 |
| Description | You have chance to deal additional damage when attacking monsters out of cloak. Its chance and damage bonus increases with training. |

### #95 · Wraith Grip

| 字段 | 值 |
|---|---|
| Name | Wraith Grip |
| Magic | WraithGrip |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 316 |
| MinBasePower | 4 |
| MaxBasePower | 4 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 10 |
| LevelCost | 24 |
| NeedLevel1 | 24 |
| NeedLevel2 | 26 |
| NeedLevel3 | 28 |
| Experience1 | 300 |
| Experience2 | 400 |
| Experience3 | 500 |
| Delay | 60000 |
| Description | Summons a creature of darkness that prevents the enemies from moving and steals mana for a set period of time. Its duration increases with training. |

### #96 · Red Lotus

| 字段 | 值 |
|---|---|
| Name | Red Lotus |
| Magic | RedLotus |
| Class | Assassin |
| School | Kill |
| Property | Charge |
| Icon | 332 |
| MinBasePower | 70 |
| MaxBasePower | 70 |
| MinLevelPower | 50 |
| MaxLevelPower | 50 |
| BaseCost | 4 |
| LevelCost | 6 |
| NeedLevel1 | 24 |
| NeedLevel2 | 26 |
| NeedLevel3 | 28 |
| Experience1 | 600 |
| Experience2 | 700 |
| Experience3 | 800 |
| Delay | 3000 |
| Description | Converts a percentage of your max mana into additional physical damage. Enemies damaged by White Lotus receives additional damage. |

### #97 · Hell Fire

| 字段 | 值 |
|---|---|
| Name | Hell Fire |
| Magic | HellFire |
| Class | Assassin |
| School | Kill |
| Property | Active |
| Icon | 318 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 10 |
| LevelCost | 10 |
| NeedLevel1 | 26 |
| NeedLevel2 | 28 |
| NeedLevel3 | 30 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 20000 |
| Description | Summons a demon from the Flames of Fire and Brimstone to inflict fire damage to an opponent and additional fire damage over time. Its damage increases with time. |

### #98 · Pledge Of Blood

| 字段 | 值 |
|---|---|
| Name | Pledge Of Blood |
| Magic | PledgeOfBlood |
| Class | Assassin |
| School | Assassination |
| Property | Passive |
| Icon | 352 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 7 |
| MaxLevelPower | 7 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 26 |
| NeedLevel2 | 28 |
| NeedLevel3 | 30 |
| Experience1 | 150 |
| Experience2 | 300 |
| Experience3 | 450 |
| Delay | 0 |
| Description | When cloaking, your HP consumption is reduced by a fixed percentage depending on the level of your training. |

### #99 · Rake

| 字段 | 值 |
|---|---|
| Name | Rake |
| Magic | Rake |
| Class | Assassin |
| School | Assassination |
| Property | Active |
| Icon | 376 |
| MinBasePower | 50 |
| MaxBasePower | 50 |
| MinLevelPower | 50 |
| MaxLevelPower | 50 |
| BaseCost | 5 |
| LevelCost | 10 |
| NeedLevel1 | 26 |
| NeedLevel2 | 28 |
| NeedLevel3 | 30 |
| Experience1 | 2000 |
| Experience2 | 2200 |
| Experience3 | 2400 |
| Delay | 5000 |
| Description | Your attacks made while in a cloaking state reduces your enemy's movement speed by 50%. This ability brings you out of the cloaking state. Its effect and duration increases with training. |

### #100 · Sweetbrier

| 字段 | 值 |
|---|---|
| Name | Sweetbrier |
| Magic | SweetBrier |
| Class | Assassin |
| School | Kill |
| Property | Charge |
| Icon | 334 |
| MinBasePower | 75 |
| MaxBasePower | 75 |
| MinLevelPower | 65 |
| MaxLevelPower | 65 |
| BaseCost | 5 |
| LevelCost | 7 |
| NeedLevel1 | 27 |
| NeedLevel2 | 29 |
| NeedLevel3 | 31 |
| Experience1 | 700 |
| Experience2 | 800 |
| Experience3 | 900 |
| Delay | 3000 |
| Description | Converts a percentage of your max mana into additional physical damage. Enemies damaged by Red Lotus receives additional damage. |

### #101 · Summon Puppet

| 字段 | 值 |
|---|---|
| Name | Summon Puppet |
| Magic | SummonPuppet |
| Class | Assassin |
| School | Assassination |
| Property | Active |
| Icon | 326 |
| MinBasePower | 60 |
| MaxBasePower | 60 |
| MinLevelPower | 40 |
| MaxLevelPower | 40 |
| BaseCost | 10 |
| LevelCost | 30 |
| NeedLevel1 | 30 |
| NeedLevel2 | 32 |
| NeedLevel3 | 34 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 30000 |
| Description | Trick your enemies to believe you're dead while you summon a puppet and become invisible. The summoned puppet deals fire damage when attacked. This can be used while in combat and number of puppets summoned increases with training. |

### #102 · Karma

| 字段 | 值 |
|---|---|
| Name | Karma |
| Magic | Karma |
| Class | Assassin |
| School | Assassination |
| Property | Charge |
| Icon | 342 |
| MinBasePower | 10 |
| MaxBasePower | 10 |
| MinLevelPower | 15 |
| MaxLevelPower | 20 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 30 |
| NeedLevel2 | 35 |
| NeedLevel3 | 40 |
| Experience1 | 200 |
| Experience2 | 400 |
| Experience3 | 800 |
| Delay | 15000 |
| Description | While in an invisible state, you deal massive damage to your enemy by spending a percentage of your total HP. You cannot use a recovery item for 10 seconds after the attack. |

### #103 · Touch Of The Departed

| 字段 | 值 |
|---|---|
| Name | Touch Of The Departed |
| Magic | TouchOfTheDeparted |
| Class | Assassin |
| School | Atrocity |
| Property | Augmentation |
| Icon | 354 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 10 |
| NeedLevel1 | 30 |
| NeedLevel2 | 32 |
| NeedLevel3 | 34 |
| Experience1 | 150 |
| Experience2 | 300 |
| Experience3 | 450 |
| Delay | 0 |
| Description | Your opponent becomes dominated by the Wraith Grip spell effect and is unable to use any Mu-Gong. You also receive a percentage of your target's Mana. The amount of Mana received increases with training. |

### #104 · Waning Moon

| 字段 | 值 |
|---|---|
| Name | Waning Moon |
| Magic | WaningMoon |
| Class | Assassin |
| School | Assassination |
| Property | Passive |
| Icon | 350 |
| MinBasePower | 20 |
| MaxBasePower | 20 |
| MinLevelPower | 20 |
| MaxLevelPower | 20 |
| BaseCost | 4 |
| LevelCost | 12 |
| NeedLevel1 | 32 |
| NeedLevel2 | 34 |
| NeedLevel3 | 36 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 700 |
| Delay | 0 |
| Description | You have chance to deal additional damage when attacking monsters cloaked. Its chance and damage bonus increases with training. |

### #105 · Ghost Walk

| 字段 | 值 |
|---|---|
| Name | Ghost Walk |
| Magic | GhostWalk |
| Class | Assassin |
| School | Assassination |
| Property | Augmentation |
| Icon | 356 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 16 |
| NeedLevel1 | 32 |
| NeedLevel2 | 34 |
| NeedLevel3 | 36 |
| Experience1 | 150 |
| Experience2 | 300 |
| Experience3 | 450 |
| Delay | 0 |
| Description | When Cloaking, you have a chance to increase your movement speed by 100%. This chance increases with training. |

### #106 · Elemental Puppet

| 字段 | 值 |
|---|---|
| Name | Elemental Puppet |
| Magic | ElementalPuppet |
| Class | Assassin |
| School | Assassination |
| Property | Augmentation |
| Icon | 358 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 34 |
| NeedLevel2 | 36 |
| NeedLevel3 | 38 |
| Experience1 | 1000 |
| Experience2 | 2000 |
| Experience3 | 3000 |
| Delay | 0 |
| Description | The effect of your puppet summoned by the Summon Puppet spell changes depending on your equipped dark stone. The charges of the dark stone decreases when you use the Summon Puppet spell with the dark stone equipped.  The effect of your puppet increases with training. |

### #107 · Rejuvenation

| 字段 | 值 |
|---|---|
| Name | Rejuvenation |
| Magic | Rejuvenation |
| Class | Assassin |
| School | Atrocity |
| Property | Passive |
| Icon | 336 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 6 |
| LevelCost | 2 |
| NeedLevel1 | 35 |
| NeedLevel2 | 37 |
| NeedLevel3 | 39 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 0 |
| Description | Your HP and Mana regeneration rate increases while not in combat. The regeneration rate bonus increases with training. |

### #108 · Resolution

| 字段 | 值 |
|---|---|
| Name | Resolution |
| Magic | Resolution |
| Class | Assassin |
| School | Assassination |
| Property | Augmentation |
| Icon | 344 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 2 |
| LevelCost | 4 |
| NeedLevel1 | 35 |
| NeedLevel2 | 37 |
| NeedLevel3 | 39 |
| Experience1 | 200 |
| Experience2 | 250 |
| Experience3 | 300 |
| Delay | 0 |
| Description | Increases the accuracy of Karma and allows it to ignore your opponent's defense by a set amount. Accuracy and defense ignored increases with training. |

### #109 · Change Of Seasons

| 字段 | 值 |
|---|---|
| Name | Change Of Seasons |
| Magic | ChangeOfSeasons |
| Class | Assassin |
| School | None |
| Property | None |
| Icon | 360 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 36 |
| NeedLevel2 | 0 |
| NeedLevel3 | 0 |
| Experience1 | 0 |
| Experience2 | 0 |
| Experience3 | 0 |
| Delay | 0 |
| Description | Removed from game |

### #110 · Release

| 字段 | 值 |
|---|---|
| Name | Release |
| Magic | Release |
| Class | Assassin |
| School | Assassination |
| Property | Passive |
| Icon | 378 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 40 |
| MaxLevelPower | 80 |
| BaseCost | 1 |
| LevelCost | 4 |
| NeedLevel1 | 36 |
| NeedLevel2 | 38 |
| NeedLevel3 | 40 |
| Experience1 | 1200 |
| Experience2 | 1500 |
| Experience3 | 1800 |
| Delay | 0 |
| Description | When performing Karma, your HP consumption is reduced by a set percentage, depending on your level of training. |

### #111 · Flame Splash

| 字段 | 值 |
|---|---|
| Name | Flame Splash |
| Magic | FlameSplash |
| Class | Assassin |
| School | Kill |
| Property | Toggle |
| Icon | 320 |
| MinBasePower | 30 |
| MaxBasePower | 30 |
| MinLevelPower | 100 |
| MaxLevelPower | 100 |
| BaseCost | 0 |
| LevelCost | 6 |
| NeedLevel1 | 38 |
| NeedLevel2 | 40 |
| NeedLevel3 | 42 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 0 |
| Description | Deals physical damage as fire damage to 4 enemies around you. The effect is triggered by chance when you deal damage to an enemy. Amount of damage dealt increases with training. |

### #112 · Bloody Flower

| 字段 | 值 |
|---|---|
| Name | Bloody Flower |
| Magic | BloodyFlower |
| Class | Assassin |
| School | Kill |
| Property | Charge |
| Icon | 374 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 10 |
| MaxLevelPower | 10 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 12 |
| NeedLevel2 | 22 |
| NeedLevel3 | 33 |
| Experience1 | 2800 |
| Experience2 | 3200 |
| Experience3 | 3600 |
| Delay | 0 |
| Description | Increases your Life Stealing. Its effects increases with training. |

### #113 · The New Beginning

| 字段 | 值 |
|---|---|
| Name | The New Beginning |
| Magic | TheNewBeginning |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 346 |
| MinBasePower | 20 |
| MaxBasePower | 20 |
| MinLevelPower | 30 |
| MaxLevelPower | 30 |
| BaseCost | 0 |
| LevelCost | 80 |
| NeedLevel1 | 40 |
| NeedLevel2 | 43 |
| NeedLevel3 | 46 |
| Experience1 | 200 |
| Experience2 | 250 |
| Experience3 | 300 |
| Delay | 1000 |
| Description | When activated, your next Flash of Light used within 60 seconds deals multiple attacks as additional magic damage. Its damage increases with training. |

### #114 · Dance Of Swallow

| 字段 | 值 |
|---|---|
| Name | Dance Of Swallow |
| Magic | DanceOfSwallow |
| Class | Assassin |
| School | Kill |
| Property | Active |
| Icon | 362 |
| MinBasePower | 1 |
| MaxBasePower | 1 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 0 |
| LevelCost | 10 |
| NeedLevel1 | 40 |
| NeedLevel2 | 42 |
| NeedLevel3 | 44 |
| Experience1 | 750 |
| Experience2 | 1050 |
| Experience3 | 1350 |
| Delay | 5000 |
| Description | Damage your opponent after you teleport and also has a chance to stun your opponent. Its chance to stun increases with training. |

### #115 · Dark Conversion

| 字段 | 值 |
|---|---|
| Name | Dark Conversion |
| Magic | DarkConversion |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 364 |
| MinBasePower | 2 |
| MaxBasePower | 2 |
| MinLevelPower | 8 |
| MaxLevelPower | 8 |
| BaseCost | 0 |
| LevelCost | 2 |
| NeedLevel1 | 42 |
| NeedLevel2 | 44 |
| NeedLevel3 | 46 |
| Experience1 | 1100 |
| Experience2 | 2200 |
| Experience3 | 3300 |
| Delay | 0 |
| Description | While active, a set percentage of your maximum Mana is converted to your HP, providing HP equal to 2 times the Mana used.  Its effect increases with training. |

### #116 · Dragon Repulse

| 字段 | 值 |
|---|---|
| Name | Dragon Repulse |
| Magic | DragonRepulse |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 322 |
| MinBasePower | 50 |
| MaxBasePower | 50 |
| MinLevelPower | 50 |
| MaxLevelPower | 50 |
| BaseCost | 100 |
| LevelCost | 100 |
| NeedLevel1 | 45 |
| NeedLevel2 | 47 |
| NeedLevel3 | 49 |
| Experience1 | 6000 |
| Experience2 | 9000 |
| Experience3 | 18000 |
| Delay | 30000 |
| Description | Inflicts physical damage and pushes nearby enemies back. The push back effect does not work on enemies 8 levels higher than you. Damage dealt increases with training. |

### #117 · Advent Of Demon

| 字段 | 值 |
|---|---|
| Name | Advent Of Demon |
| Magic | AdventOfDemon |
| Class | Assassin |
| School | Kill |
| Property | Passive |
| Icon | 338 |
| MinBasePower | 3 |
| MaxBasePower | 3 |
| MinLevelPower | 7 |
| MaxLevelPower | 7 |
| BaseCost | 4 |
| LevelCost | 2 |
| NeedLevel1 | 45 |
| NeedLevel2 | 47 |
| NeedLevel3 | 49 |
| Experience1 | 6000 |
| Experience2 | 9000 |
| Experience3 | 18000 |
| Delay | 0 |
| Description | Increases your Max AC. Its effects increases with training. |

### #118 · Advent Of Devil

| 字段 | 值 |
|---|---|
| Name | Advent Of Devil |
| Magic | AdventOfDevil |
| Class | Assassin |
| School | Assassination |
| Property | Passive |
| Icon | 348 |
| MinBasePower | 3 |
| MaxBasePower | 3 |
| MinLevelPower | 7 |
| MaxLevelPower | 7 |
| BaseCost | 3 |
| LevelCost | 2 |
| NeedLevel1 | 45 |
| NeedLevel2 | 47 |
| NeedLevel3 | 49 |
| Experience1 | 6000 |
| Experience2 | 9000 |
| Experience3 | 18000 |
| Delay | 0 |
| Description | Increases your Max MR. Its effects increases with training. |

### #119 · Abyss

| 字段 | 值 |
|---|---|
| Name | Abyss |
| Magic | Abyss |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 366 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 1 |
| LevelCost | 20 |
| NeedLevel1 | 45 |
| NeedLevel2 | 47 |
| NeedLevel3 | 49 |
| Experience1 | 2500 |
| Experience2 | 3000 |
| Experience3 | 3500 |
| Delay | 10000 |
| Description | You shroud your opponent in darkness, reducing their sight and accuracy by a set percentage. The duration increases with training. |

### #120 · Flash Of Light

| 字段 | 值 |
|---|---|
| Name | Flash Of Light |
| Magic | FlashOfLight |
| Class | Assassin |
| School | Kill |
| Property | Active |
| Icon | 368 |
| MinBasePower | 180 |
| MaxBasePower | 180 |
| MinLevelPower | 180 |
| MaxLevelPower | 180 |
| BaseCost | 22 |
| LevelCost | 72 |
| NeedLevel1 | 45 |
| NeedLevel2 | 47 |
| NeedLevel3 | 50 |
| Experience1 | 3000 |
| Experience2 | 3300 |
| Experience3 | 3600 |
| Delay | 5000 |
| Description | You deal massive damage to all enemies within 2 tiles. |

### #121 · Stealth

| 字段 | 值 |
|---|---|
| Name | Stealth |
| Magic | Stealth |
| Class | Assassin |
| School | Assassination |
| Property | Augmentation |
| Icon | 380 |
| MinBasePower | 10 |
| MaxBasePower | 10 |
| MinLevelPower | 15 |
| MaxLevelPower | 15 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 45 |
| NeedLevel2 | 47 |
| NeedLevel3 | 49 |
| Experience1 | 1600 |
| Experience2 | 2000 |
| Experience3 | 2400 |
| Delay | 0 |
| Description | When you perform an action that causes you to break cloak, you're able prevent it by spending the charged “The New Beginning”. Cannot be used without the The New Beginning effect. The chance of remaining in the cloaking state increases with training. |

### #122 · Evasion

| 字段 | 值 |
|---|---|
| Name | Evasion |
| Magic | Evasion |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 370 |
| MinBasePower | 45 |
| MaxBasePower | 45 |
| MinLevelPower | 45 |
| MaxLevelPower | 45 |
| BaseCost | 20 |
| LevelCost | 20 |
| NeedLevel1 | 47 |
| NeedLevel2 | 49 |
| NeedLevel3 | 51 |
| Experience1 | 1400 |
| Experience2 | 2100 |
| Experience3 | 2800 |
| Delay | 0 |
| Description | You have a chance to dodge your opponent's Elemental attack. Its chance increases with training. |

### #123 · Raging Wind

| 字段 | 值 |
|---|---|
| Name | Raging Wind |
| Magic | RagingWind |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 372 |
| MinBasePower | 45 |
| MaxBasePower | 45 |
| MinLevelPower | 45 |
| MaxLevelPower | 45 |
| BaseCost | 20 |
| LevelCost | 20 |
| NeedLevel1 | 47 |
| NeedLevel2 | 49 |
| NeedLevel3 | 51 |
| Experience1 | 2400 |
| Experience2 | 2800 |
| Experience3 | 3200 |
| Delay | 0 |
| Description | Your Minimum defense increases by a percentage of your Maximum defense , and your Maximum defense is decreased by a set percentage. Its effects increases with training. |

### #124 · Empowered Explosive Talisman

| 字段 | 值 |
|---|---|
| Name | Empowered Explosive Talisman |
| Magic | AugmentExplosiveTalisman |
| Class | Taoist |
| School | None |
| Property | Augmentation |
| Icon | 24 |
| MinBasePower | 1 |
| MaxBasePower | 1 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 17 |
| NeedLevel2 | 34 |
| NeedLevel3 | 51 |
| Experience1 | 6000 |
| Experience2 | 12000 |
| Experience3 | 24000 |
| Delay | 3000 |
| Description | Augments Explosive Talisman (and Improved), When casting Explosive Talisman there is a chance it will effect nearby enemies. |

### #125 · Empowered Evil Slayer

| 字段 | 值 |
|---|---|
| Name | Empowered Evil Slayer |
| Magic | AugmentEvilSlayer |
| Class | Taoist |
| School | None |
| Property | Augmentation |
| Icon | 72 |
| MinBasePower | 1 |
| MaxBasePower | 1 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 17 |
| NeedLevel2 | 34 |
| NeedLevel3 | 51 |
| Experience1 | 6000 |
| Experience2 | 12000 |
| Experience3 | 24000 |
| Delay | 3000 |
| Description | Augments Evil Slayer (and Greater), When casting Evil Slayer there is a chance it will effect nearby enemies. |

### #126 · Empowered Purification

| 字段 | 值 |
|---|---|
| Name | Empowered Purification |
| Magic | AugmentPurification |
| Class | Taoist |
| School | None |
| Property | Augmentation |
| Icon | 238 |
| MinBasePower | 1 |
| MaxBasePower | 1 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 55 |
| NeedLevel2 | 58 |
| NeedLevel3 | 62 |
| Experience1 | 15000 |
| Experience2 | 20000 |
| Experience3 | 28000 |
| Delay | 10000 |
| Description | Augments Purification, When casting Purification, there is a chance it will effect nearby targets. |

### #127 · Empowered Resurrection

| 字段 | 值 |
|---|---|
| Name | Empowered Resurrection |
| Magic | AugmentResurrection |
| Class | Taoist |
| School | None |
| Property | Augmentation |
| Icon | 152 |
| MinBasePower | 1 |
| MaxBasePower | 1 |
| MinLevelPower | 3 |
| MaxLevelPower | 3 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 75 |
| NeedLevel2 | 77 |
| NeedLevel3 | 80 |
| Experience1 | 2500 |
| Experience2 | 5000 |
| Experience3 | 10000 |
| Delay | 60000 |
| Description | Augments Resurrection, When casting Resurrection, there is a chance it will effect nearby Allies. |

### #128 · Demon Explosion

| 字段 | 值 |
|---|---|
| Name | Demon Explosion |
| Magic | DemonExplosion |
| Class | Taoist |
| School | Phantom |
| Property | Active |
| Icon | 306 |
| MinBasePower | 25 |
| MaxBasePower | 25 |
| MinLevelPower | 25 |
| MaxLevelPower | 25 |
| BaseCost | 100 |
| LevelCost | 80 |
| NeedLevel1 | 52 |
| NeedLevel2 | 56 |
| NeedLevel3 | 58 |
| Experience1 | 5500 |
| Experience2 | 6500 |
| Experience3 | 8500 |
| Delay | 10000 |
| Description | Cause your summoned creature to explode, inflicting damage to nearby enemies. damage inflicted depends on the level of your summoned creature. |

### #129 · Strength Of Faith

| 字段 | 值 |
|---|---|
| Name | Strength Of Faith |
| Magic | StrengthOfFaith |
| Class | Taoist |
| School | Phantom |
| Property | Active |
| Icon | 244 |
| MinBasePower | 60 |
| MaxBasePower | 60 |
| MinLevelPower | 180 |
| MaxLevelPower | 180 |
| BaseCost | 30 |
| LevelCost | 40 |
| NeedLevel1 | 40 |
| NeedLevel2 | 42 |
| NeedLevel3 | 44 |
| Experience1 | 2000 |
| Experience2 | 3000 |
| Experience3 | 6000 |
| Delay | 0 |
| Description | Increases the damage of your summoned creature by sacrificing your own damage. The damage bonus of your summoned creature increases with training. |

### #130 · Summon Skeleton

| 字段 | 值 |
|---|---|
| Name | Summon Skeleton |
| Magic | SummonSkeleton |
| Class | Taoist |
| School | Phantom |
| Property | Active |
| Icon | 32 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 10 |
| LevelCost | 15 |
| NeedLevel1 | 17 |
| NeedLevel2 | 19 |
| NeedLevel3 | 21 |
| Experience1 | 400 |
| Experience2 | 500 |
| Experience3 | 600 |
| Delay | 0 |
| Description | By inserting your body's energy into the talisman, you summon a powerful spirit to fight for you. The strength of your summoned spirit increases with training. |

### #131 · Mirror Image

| 字段 | 值 |
|---|---|
| Name | Mirror Image |
| Magic | MirrorImage |
| Class | Wizard |
| School | None |
| Property | Active |
| Icon | 252 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 10 |
| LevelCost | 10 |
| NeedLevel1 | 56 |
| NeedLevel2 | 61 |
| NeedLevel3 | 66 |
| Experience1 | 10000 |
| Experience2 | 18000 |
| Experience3 | 32000 |
| Delay | 0 |
| Description | The spell creates an illusion of yourself which has the same magic attack abilities. You must have a dark ore to cast this spell. |

### #132 · Summon Jin Skeleton

| 字段 | 值 |
|---|---|
| Name | Summon Jin Skeleton |
| Magic | SummonJinSkeleton |
| Class | Taoist |
| School | Phantom |
| Property | Active |
| Icon | 208 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 25 |
| LevelCost | 20 |
| NeedLevel1 | 33 |
| NeedLevel2 | 35 |
| NeedLevel3 | 37 |
| Experience1 | 1000 |
| Experience2 | 1100 |
| Experience3 | 1200 |
| Delay | 0 |
| Description | By focusing your body's energy onto a talisman, you summon a mighty spirit to fight for you. You may use this Mu-Gong even if you have already summoned a Holy Beast or a Skeleton. The strength of the mighty spirit increases with training. |

### #133 · Summon Shinsu

| 字段 | 值 |
|---|---|
| Name | Summon Shinsu |
| Magic | SummonShinsu |
| Class | Taoist |
| School | Phantom |
| Property | Active |
| Icon | 58 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 15 |
| LevelCost | 15 |
| NeedLevel1 | 30 |
| NeedLevel2 | 32 |
| NeedLevel3 | 34 |
| Experience1 | 900 |
| Experience2 | 1000 |
| Experience3 | 1100 |
| Delay | 0 |
| Description | By focusing your body's energy onto a talisman, you call upon the Holy Beasts to fight for you. The strength of the Holy Beast increases with training. |

### #134 · Summon Demonic Creature

| 字段 | 值 |
|---|---|
| Name | Summon Demonic Creature |
| Magic | SummonDemonicCreature |
| Class | Taoist |
| School | Phantom |
| Property | Active |
| Icon | 304 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 20 |
| LevelCost | 30 |
| NeedLevel1 | 50 |
| NeedLevel2 | 54 |
| NeedLevel3 | 56 |
| Experience1 | 7000 |
| Experience2 | 9000 |
| Experience3 | 11000 |
| Delay | 0 |
| Description | By inserting your body's energy into the talisman, you summon a infernal creature to fight for you. The power of your summoned creature increases with training. |

### #135 · Advanced Potion Mastery

| 字段 | 值 |
|---|---|
| Name | Advanced Potion Mastery |
| Magic | AdvancedPotionMastery |
| Class | Warrior |
| School | None |
| Property | Augmentation |
| Icon | 262 |
| MinBasePower | 10 |
| MaxBasePower | 10 |
| MinLevelPower | 15 |
| MaxLevelPower | 15 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 40 |
| NeedLevel2 | 50 |
| NeedLevel3 | 60 |
| Experience1 | 20000 |
| Experience2 | 40000 |
| Experience3 | 80000 |
| Delay | 0 |
| Description | You become further adept at using recovery items. You may recover more depending upon the level of your training. Works with Regular Potion Mastery. |

### #136 · _Blank_

| 字段 | 值 |
|---|---|
| Name | _Blank_ |
| Magic | Unused |
| Class | Assassin |
| School | None |
| Property | None |
| Icon | 0 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 0 |
| NeedLevel2 | 0 |
| NeedLevel3 | 0 |
| Experience1 | 0 |
| Experience2 | 0 |
| Experience3 | 0 |
| Delay | 0 |
| Description | UNUSED |

### #137 · Ice Rain

| 字段 | 值 |
|---|---|
| Name | Ice Rain |
| Magic | IceRain |
| Class | Wizard |
| School | Ice |
| Property | Active |
| Icon | 486 |
| MinBasePower | 5 |
| MaxBasePower | 10 |
| MinLevelPower | 2 |
| MaxLevelPower | 4 |
| BaseCost | 50 |
| LevelCost | 50 |
| NeedLevel1 | 82 |
| NeedLevel2 | 86 |
| NeedLevel3 | 90 |
| Experience1 | 50000 |
| Experience2 | 100000 |
| Experience3 | 150000 |
| Delay | 0 |
| Description | A spell that rains down with ice shards, repeatedly damaging anyone inside with a chance to freeze them. Duration increases with training. |

### #138 · Mass Beckon

| 字段 | 值 |
|---|---|
| Name | Mass Beckon |
| Magic | MassBeckon |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 386 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 100 |
| LevelCost | 50 |
| NeedLevel1 | 60 |
| NeedLevel2 | 63 |
| NeedLevel3 | 66 |
| Experience1 | 5000 |
| Experience2 | 10000 |
| Experience3 | 25000 |
| Delay | 5000 |
| Description | When successful all enemies around the caster are pulled to the perimeter of the caster. Success increases with training. |

### #139 · Frost Bite

| 字段 | 值 |
|---|---|
| Name | Frost Bite |
| Magic | FrostBite |
| Class | Wizard |
| School | Ice |
| Property | Active |
| Icon | 390 |
| MinBasePower | 50 |
| MaxBasePower | 50 |
| MinLevelPower | 75 |
| MaxLevelPower | 75 |
| BaseCost | 100 |
| LevelCost | 100 |
| NeedLevel1 | 58 |
| NeedLevel2 | 60 |
| NeedLevel3 | 62 |
| Experience1 | 2500 |
| Experience2 | 5000 |
| Experience3 | 9000 |
| Delay | 25000 |
| Description | Protect yourself with frost magic that stores up received damage over time and then expells it in a wide radius to damage and slow the enemy. Damage stored, chance to store and duration all increase with training. |

### #140 · Infection

| 字段 | 值 |
|---|---|
| Name | Infection |
| Magic | Infection |
| Class | Taoist |
| School | Dark |
| Property | Augmentation |
| Icon | 446 |
| MinBasePower | 2 |
| MaxBasePower | 2 |
| MinLevelPower | 2 |
| MaxLevelPower | 3 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 65 |
| NeedLevel2 | 70 |
| NeedLevel3 | 75 |
| Experience1 | 7500 |
| Experience2 | 15000 |
| Experience3 | 30000 |
| Delay | 3000 |
| Description | When a monster is infected with a parasite, the parasite and all other poisons will now spread to all nearby monsters. Transferred damage of infection increases with training. |

### #141 · Massacre

| 字段 | 值 |
|---|---|
| Name | Massacre |
| Magic | Massacre |
| Class | Assassin |
| School | None |
| Property | Passive |
| Icon | 382 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 65 |
| NeedLevel2 | 70 |
| NeedLevel3 | 75 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 40000 |
| Delay | 0 |
| Description | Upon killing an enemy a percentage of the remaining damage dealt is passed to all nearby targets. Damage dealt increases with training. |

### #142 · Seismic Slam

| 字段 | 值 |
|---|---|
| Name | Seismic Slam |
| Magic | SeismicSlam |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 434 |
| MinBasePower | 120 |
| MaxBasePower | 120 |
| MinLevelPower | 150 |
| MaxLevelPower | 150 |
| BaseCost | 50 |
| LevelCost | 100 |
| NeedLevel1 | 83 |
| NeedLevel2 | 85 |
| NeedLevel3 | 87 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 30000 |
| Delay | 18000 |
| Description | Attacks an area with deadly force that has the potential to knock out enemies for a period of time. |

### #143 · Demonic Recovery

| 字段 | 值 |
|---|---|
| Name | Demonic Recovery |
| Magic | DemonicRecovery |
| Class | Taoist |
| School | Phantom |
| Property | Active |
| Icon | 536 |
| MinBasePower | 25 |
| MaxBasePower | 25 |
| MinLevelPower | 75 |
| MaxLevelPower | 75 |
| BaseCost | 100 |
| LevelCost | 80 |
| NeedLevel1 | 48 |
| NeedLevel2 | 51 |
| NeedLevel3 | 54 |
| Experience1 | 9000 |
| Experience2 | 10000 |
| Experience3 | 11000 |
| Delay | 0 |
| Description | Passively increases the health of all summoned creatures, and actively restores the health of any summoned demonic creature. Health restored increases with training. |

### #144 · Asteroid

| 字段 | 值 |
|---|---|
| Name | Asteroid |
| Magic | Asteroid |
| Class | Wizard |
| School | Fire |
| Property | Active |
| Icon | 392 |
| MinBasePower | 50 |
| MaxBasePower | 80 |
| MinLevelPower | 70 |
| MaxLevelPower | 120 |
| BaseCost | 200 |
| LevelCost | 200 |
| NeedLevel1 | 80 |
| NeedLevel2 | 82 |
| NeedLevel3 | 84 |
| Experience1 | 7500 |
| Experience2 | 15000 |
| Experience3 | 22500 |
| Delay | 3300 |
| Description | Summons fire from the sky to inflict massive area damage, leaving fire walls in its wake. Damage increases with training. |

### #145 · Art of Shadows

| 字段 | 值 |
|---|---|
| Name | Art of Shadows |
| Magic | ArtOfShadows |
| Class | Assassin |
| School | None |
| Property | Passive |
| Icon | 396 |
| MinBasePower | 2 |
| MaxBasePower | 3 |
| MinLevelPower | 4 |
| MaxLevelPower | 6 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 75 |
| NeedLevel2 | 79 |
| NeedLevel3 | 82 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 40000 |
| Delay | 0 |
| Description | Increases the amount of puppets summoned, and the area in which they appear. Amount summed increases with training. |

### #146 · Invincibility

| 字段 | 值 |
|---|---|
| Name | Invincibility |
| Magic | Invincibility |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 442 |
| MinBasePower | 0 |
| MaxBasePower | 30 |
| MinLevelPower | 20 |
| MaxLevelPower | 20 |
| BaseCost | 0 |
| LevelCost | 100 |
| NeedLevel1 | 65 |
| NeedLevel2 | 70 |
| NeedLevel3 | 75 |
| Experience1 | 1200 |
| Experience2 | 1800 |
| Experience3 | 3000 |
| Delay | 5000 |
| Description | Ignores all attacks except internal attacks for a certain period of time. The duration increases with training. |

### #147 · Crushing Wave

| 字段 | 值 |
|---|---|
| Name | Crushing Wave |
| Magic | CrushingWave |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 450 |
| MinBasePower | 0 |
| MaxBasePower | 30 |
| MinLevelPower | 20 |
| MaxLevelPower | 20 |
| BaseCost | 0 |
| LevelCost | 100 |
| NeedLevel1 | 90 |
| NeedLevel2 | 90 |
| NeedLevel3 | 90 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 30000 |
| Delay | 0 |
| Description | Sends a powerful energy through your weapon which cuts down enemies from a distance. Damage increases with training. |

### #148 · Neutralize

| 字段 | 值 |
|---|---|
| Name | Neutralize |
| Magic | Neutralize |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 480 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 10 |
| LevelCost | 5 |
| NeedLevel1 | 80 |
| NeedLevel2 | 86 |
| NeedLevel3 | 91 |
| Experience1 | 20000 |
| Experience2 | 40000 |
| Experience3 | 60000 |
| Delay | 0 |
| Description | Lowers the targets attack power, movement and attack speed. When incapacited they will not be able to recover health. Duration of effect increases with training. |

### #149 · Empowered Neutralize

| 字段 | 值 |
|---|---|
| Name | Empowered Neutralize |
| Magic | AugmentNeutralize |
| Class | Taoist |
| School | None |
| Property | Augmentation |
| Icon | 480 |
| MinBasePower | 1 |
| MaxBasePower | 1 |
| MinLevelPower | 1 |
| MaxLevelPower | 1 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 80 |
| NeedLevel2 | 86 |
| NeedLevel3 | 91 |
| Experience1 | 4000 |
| Experience2 | 6000 |
| Experience3 | 8000 |
| Delay | 0 |
| Description | Augments Neutralize, When casting Neutralize it will now affect nearby targets. Amount of targets affected increases with training. |

### #150 · Dark Soul Prison

| 字段 | 值 |
|---|---|
| Name | Dark Soul Prison |
| Magic | DarkSoulPrison |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 454 |
| MinBasePower | 12 |
| MaxBasePower | 15 |
| MinLevelPower | 1 |
| MaxLevelPower | 2 |
| BaseCost | 10 |
| LevelCost | 5 |
| NeedLevel1 | 90 |
| NeedLevel2 | 90 |
| NeedLevel3 | 90 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 30000 |
| Delay | 0 |
| Description | Cast a 7x7 spell of darkness that instills pain and fear in the enemy, continously damaging anyone in its radius. Duration and damage increases with training. |

### #151 · Searing Light

| 字段 | 值 |
|---|---|
| Name | Searing Light |
| Magic | SearingLight |
| Class | Taoist |
| School | Holy |
| Property | Active |
| Icon | 438 |
| MinBasePower | 12 |
| MaxBasePower | 15 |
| MinLevelPower | 1 |
| MaxLevelPower | 2 |
| BaseCost | 15 |
| LevelCost | 5 |
| NeedLevel1 | 83 |
| NeedLevel2 | 85 |
| NeedLevel3 | 87 |
| Experience1 | 7000 |
| Experience2 | 10000 |
| Experience3 | 16000 |
| Delay | 5000 |
| Description | Deals a certain amount of soul damage, and depending on the target also temporarily prevents attack or movement. Damage dealt and effect duration increases with training. |

### #152 · Defensive Mastery

| 字段 | 值 |
|---|---|
| Name | Defensive Mastery |
| Magic | DefensiveMastery |
| Class | Warrior |
| School | Passive |
| Property | Passive |
| Icon | 466 |
| MinBasePower | 1 |
| MaxBasePower | 1 |
| MinLevelPower | 1 |
| MaxLevelPower | 1 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 70 |
| NeedLevel2 | 75 |
| NeedLevel3 | 80 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 40000 |
| Delay | 0 |
| Description | Increases the probability of using your maximum defensive capabilities. Probability increases with training. |

### #153 · Physical Immunity

| 字段 | 值 |
|---|---|
| Name | Physical Immunity |
| Magic | PhysicalImmunity |
| Class | Warrior |
| School | Passive |
| Property | Passive |
| Icon | 468 |
| MinBasePower | 1 |
| MaxBasePower | 6 |
| MinLevelPower | 1 |
| MaxLevelPower | 1 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 80 |
| NeedLevel2 | 84 |
| NeedLevel3 | 88 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 40000 |
| Delay | 0 |
| Description | Permanently reduce all incoming physical attacks. Reduction amount increases with training. |

### #154 · Magic Immunity

| 字段 | 值 |
|---|---|
| Name | Magic Immunity |
| Magic | MagicImmunity |
| Class | Warrior |
| School | Passive |
| Property | Passive |
| Icon | 470 |
| MinBasePower | 1 |
| MaxBasePower | 6 |
| MinLevelPower | 1 |
| MaxLevelPower | 1 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 80 |
| NeedLevel2 | 84 |
| NeedLevel3 | 88 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 40000 |
| Delay | 0 |
| Description | Permanently reduce all incoming magic attacks. Reduction amount increases with training. |

### #155 · Defensive Blow

| 字段 | 值 |
|---|---|
| Name | Defensive Blow |
| Magic | DefensiveBlow |
| Class | Warrior |
| School | Active |
| Property | Charge |
| Icon | 488 |
| MinBasePower | 10 |
| MaxBasePower | 10 |
| MinLevelPower | 10 |
| MaxLevelPower | 10 |
| BaseCost | 50 |
| LevelCost | 0 |
| NeedLevel1 | 86 |
| NeedLevel2 | 88 |
| NeedLevel3 | 91 |
| Experience1 | 15000 |
| Experience2 | 20000 |
| Experience3 | 30000 |
| Delay | 10000 |
| Description | Use the full force of your swing to temporarily decrease your targets defensive abilities. Defensives reduces increases with training. |

### #156 · Elemental Swords

| 字段 | 值 |
|---|---|
| Name | Elemental Swords |
| Magic | ElementalSwords |
| Class | Warrior |
| School | Active |
| Property | Active |
| Icon | 502 |
| MinBasePower | 5 |
| MaxBasePower | 15 |
| MinLevelPower | 5 |
| MaxLevelPower | 10 |
| BaseCost | 10 |
| LevelCost | 5 |
| NeedLevel1 | 95 |
| NeedLevel2 | 96 |
| NeedLevel3 | 97 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 30000 |
| Delay | 5000 |
| Description | Throws a sword at the target inflicting damage. If the target is killed there is a chance to regain a percentage of your drained mana. Amount of swords thrown, damage dealt and mana regained increases with training. |

### #157 · Storm

| 字段 | 值 |
|---|---|
| Name | Storm |
| Magic | Storm |
| Class | Wizard |
| School | None |
| Property | Active |
| Icon | 492 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 86 |
| NeedLevel2 | 88 |
| NeedLevel3 | 91 |
| Experience1 | 20000 |
| Experience2 | 30500 |
| Experience3 | 41000 |
| Delay | 0 |
| Description | Creates a massive area storm that continously damages anything in its radius. Damage increases with training. |

### #158 · Tornado

| 字段 | 值 |
|---|---|
| Name | Tornado |
| Magic | Tornado |
| Class | Wizard |
| School | Wind |
| Property | Active |
| Icon | 508 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 95 |
| NeedLevel2 | 96 |
| NeedLevel3 | 97 |
| Experience1 | 15000 |
| Experience2 | 30000 |
| Experience3 | 45000 |
| Delay | 0 |
| Description | Summon a tornado which damages anything in its path. The effects of the tornado increase with training. |

### #159 · Empowered Celestial Light

| 字段 | 值 |
|---|---|
| Name | Empowered Celestial Light |
| Magic | AugmentCelestialLight |
| Class | Taoist |
| School | Holy |
| Property | Augmentation |
| Icon | 462 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 82 |
| NeedLevel2 | 83 |
| NeedLevel3 | 84 |
| Experience1 | 4000 |
| Experience2 | 6000 |
| Experience3 | 12000 |
| Delay | 0 |
| Description | Provides further HP regain to Celestial Light at level 3 or above, and acts as a shield giving the caster a small offensive and defensive bonus. Amount increases with training. |

### #160 · Corpse Exploder

| 字段 | 值 |
|---|---|
| Name | Corpse Exploder |
| Magic | CorpseExploder |
| Class | Taoist |
| School | Dark |
| Property | Active |
| Icon | 490 |
| MinBasePower | 15 |
| MaxBasePower | 30 |
| MinLevelPower | 5 |
| MaxLevelPower | 10 |
| BaseCost | 30 |
| LevelCost | 5 |
| NeedLevel1 | 86 |
| NeedLevel2 | 88 |
| NeedLevel3 | 95 |
| Experience1 | 30000 |
| Experience2 | 30000 |
| Experience3 | 30000 |
| Delay | 0 |
| Description | Throw a talisman at a dead target to explode their body and damage the surrounding area. |

### #161 · Summon Dead

| 字段 | 值 |
|---|---|
| Name | Summon Dead |
| Magic | SummonDead |
| Class | Taoist |
| School | Phantom |
| Property | Active |
| Icon | 514 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 95 |
| NeedLevel2 | 96 |
| NeedLevel3 | 97 |
| Experience1 | 10000 |
| Experience2 | 11000 |
| Experience3 | 12000 |
| Delay | 0 |
| Description | Reawake a corpse and command the dead. Requires a soul amulet. |

### #162 · Dragon Blood

| 字段 | 值 |
|---|---|
| Name | Dragon Blood |
| Magic | DragonBlood |
| Class | Assassin |
| School | Kill |
| Property | Passive |
| Icon | 382 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 60 |
| NeedLevel2 | 62 |
| NeedLevel3 | 64 |
| Experience1 | 3000 |
| Experience2 | 3200 |
| Experience3 | 3400 |
| Delay | 0 |
| Description | Poisons targets hit by your swing, dealing a percentage of damage every 2 seconds for 10 seconds. Stacks up to 4 times. Chance to poison and damage dealt increases with training. Requires Poison Bottle. |

### #163 · Fatal Blow

| 字段 | 值 |
|---|---|
| Name | Fatal Blow |
| Magic | FatalBlow |
| Class | Assassin |
| School | Kill |
| Property | Passive |
| Icon | 474 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 60 |
| NeedLevel2 | 70 |
| NeedLevel3 | 80 |
| Experience1 | 3000 |
| Experience2 | 6000 |
| Experience3 | 8000 |
| Delay | 0 |
| Description | Gain increased attack power when your targets health falls below 30%. Damage dealt increases with training. |

### #164 · Last Stand

| 字段 | 值 |
|---|---|
| Name | Last Stand |
| Magic | LastStand |
| Class | Assassin |
| School | Atrocity |
| Property | Passive |
| Icon | 448 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 65 |
| NeedLevel2 | 75 |
| NeedLevel3 | 85 |
| Experience1 | 500 |
| Experience2 | 600 |
| Experience3 | 800 |
| Delay | 0 |
| Description | Increases your physical resistance when your health falls below 30%. Resistance amount and probability to activate increases with training. |

### #165 · Magic Combustion

| 字段 | 值 |
|---|---|
| Name | Magic Combustion |
| Magic | MagicCombustion |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 478 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 10 |
| LevelCost | 10 |
| NeedLevel1 | 70 |
| NeedLevel2 | 72 |
| NeedLevel3 | 76 |
| Experience1 | 15000 |
| Experience2 | 25000 |
| Experience3 | 33000 |
| Delay | 10000 |
| Description | Throw a spear which burns away your targets mana. Applies only in PvP. Mana removed increases with training. |

### #166 · Vitality

| 字段 | 值 |
|---|---|
| Name | Vitality |
| Magic | Vitality |
| Class | Assassin |
| School | Atrocity |
| Property | Passive |
| Icon | 472 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 70 |
| NeedLevel2 | 74 |
| NeedLevel3 | 80 |
| Experience1 | 2000 |
| Experience2 | 3000 |
| Experience3 | 6000 |
| Delay | 0 |
| Description | Increases your recovery rate when your health falls below 30%. Applies to all forms of recovery. Rate of recovery increases with training. |

### #167 · Chain

| 字段 | 值 |
|---|---|
| Name | Chain |
| Magic | Chain |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 476 |
| MinBasePower | 20 |
| MaxBasePower | 20 |
| MinLevelPower | 20 |
| MaxLevelPower | 20 |
| BaseCost | 15 |
| LevelCost | 15 |
| NeedLevel1 | 75 |
| NeedLevel2 | 80 |
| NeedLevel3 | 84 |
| Experience1 | 20000 |
| Experience2 | 35000 |
| Experience3 | 40000 |
| Delay | 15000 |
| Description | Chains your target to all nearby targets, preventing them from moving far apart from each other. Amount of targets which can be chained together and duration of chain increases with training. |

### #168 · Concentration

| 字段 | 值 |
|---|---|
| Name | Concentration |
| Magic | Concentration |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 384 |
| MinBasePower | 60 |
| MaxBasePower | 60 |
| MinLevelPower | 60 |
| MaxLevelPower | 60 |
| BaseCost | 30 |
| LevelCost | 30 |
| NeedLevel1 | 80 |
| NeedLevel2 | 82 |
| NeedLevel3 | 84 |
| Experience1 | 5000 |
| Experience2 | 5500 |
| Experience3 | 6000 |
| Delay | 0 |
| Description | Increases your crit chance for a period of time. Rate and duration increases with training. |

### #169 · Dual Weapon Skills

| 字段 | 值 |
|---|---|
| Name | Dual Weapon Skills |
| Magic | DualWeaponSkills |
| Class | Assassin |
| School | Atrocity |
| Property | Passive |
| Icon | 464 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 82 |
| NeedLevel2 | 83 |
| NeedLevel3 | 84 |
| Experience1 | 35000 |
| Experience2 | 38000 |
| Experience3 | 41000 |
| Delay | 0 |
| Description | Permanently increases the attack power of all dual weapon attacks. Power increases with training. |

### #170 · Containment

| 字段 | 值 |
|---|---|
| Name | Containment |
| Magic | Containment |
| Class | Assassin |
| School | Atrocity |
| Property | Active |
| Icon | 440 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 83 |
| NeedLevel2 | 85 |
| NeedLevel3 | 87 |
| Experience1 | 12000 |
| Experience2 | 18000 |
| Experience3 | 36000 |
| Delay | 5000 |
| Description | Prevents targets in a large radius of the caster from moving for 3 seconds whilst dealing continuous damage. Number of targets affected increases with training. |

### #171 · Dragon Wave

| 字段 | 值 |
|---|---|
| Name | Dragon Wave |
| Magic | DragonWave |
| Class | Assassin |
| School | Kill |
| Property | Augmentation |
| Icon | 542 |
| MinBasePower | 0 |
| MaxBasePower | 0 |
| MinLevelPower | 0 |
| MaxLevelPower | 0 |
| BaseCost | 1 |
| LevelCost | 1 |
| NeedLevel1 | 85 |
| NeedLevel2 | 90 |
| NeedLevel3 | 95 |
| Experience1 | 50000 |
| Experience2 | 55000 |
| Experience3 | 60000 |
| Delay | 0 |
| Description | Reduces the cost of Flame Splash. At level 3 or above it increases the amount of targets hit. Cost reduces further with training. |

### #172 · Hemorrhage

| 字段 | 值 |
|---|---|
| Name | Hemorrhage |
| Magic | Hemorrhage |
| Class | Assassin |
| School | Kill |
| Property | Active |
| Icon | 494 |
| MinBasePower | 5 |
| MaxBasePower | 5 |
| MinLevelPower | 5 |
| MaxLevelPower | 5 |
| BaseCost | 25 |
| LevelCost | 25 |
| NeedLevel1 | 86 |
| NeedLevel2 | 88 |
| NeedLevel3 | 91 |
| Experience1 | 40000 |
| Experience2 | 42000 |
| Experience3 | 46000 |
| Delay | 5000 |
| Description | Continuously damages the enemy and prevents any health recovery. Damage and duration increases with training. |

### #173 · Burning Fire

| 字段 | 值 |
|---|---|
| Name | Burning Fire |
| Magic | BurningFire |
| Class | Assassin |
| School | Kill |
| Property | Active |
| Icon | 456 |
| MinBasePower | 15 |
| MaxBasePower | 15 |
| MinLevelPower | 15 |
| MaxLevelPower | 15 |
| BaseCost | 30 |
| LevelCost | 30 |
| NeedLevel1 | 90 |
| NeedLevel2 | 90 |
| NeedLevel3 | 90 |
| Experience1 | 10000 |
| Experience2 | 20000 |
| Experience3 | 30000 |
| Delay | 5000 |
| Description | Create a trap on the floor from the fires of Hell. Lasts for 15 seconds and when stepped on deals damage in a 3x3 area. Damage dealt and amount of traps which can be placed increases with training. |

### #174 · Chain Of Fire

| 字段 | 值 |
|---|---|
| Name | Chain Of Fire |
| Magic | ChainOfFire |
| Class | Assassin |
| School | Atrocity |
| Property | Augmentation |
| Icon | 520 |
| MinBasePower | 100 |
| MaxBasePower | 100 |
| MinLevelPower | 100 |
| MaxLevelPower | 100 |
| BaseCost | 0 |
| LevelCost | 0 |
| NeedLevel1 | 95 |
| NeedLevel2 | 96 |
| NeedLevel3 | 97 |
| Experience1 | 20000 |
| Experience2 | 35000 |
| Experience3 | 40000 |
| Delay | 0 |
| Description | All chained targets are slowed. At level 1 or above damage done to the leader will be dealt to all targets they're chained to. At level 2 or above continuous fire damage will be dealt to all targets. At level 3 or above will inflict damage in a 3x3 radius when the leader dies. |

