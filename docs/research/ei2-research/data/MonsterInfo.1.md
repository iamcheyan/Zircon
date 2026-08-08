<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --dump docs/database -->

# 怪物（MonsterInfo）

> 记录 #1 – #350，共 309 条（第 1/2 部分）。

[README](../README.md) · [下一部分 →](MonsterInfo.2.md)

## 快速浏览

| # | MonsterName | Image | AI | Level | Experience | ViewRange | IsBoss | Undead |
|---|---|---|---|---|---|---|---|---|
| 1 | Guard | Guard | -1 | 250 | 0 | 9 | false | false |
| 8 | Chicken | Chicken | 1 | 5 | 11 | 9 | false | false |
| 9 | Pig | Pig | 2 | 6 | 15 | 9 | false | false |
| 10 | Deer | Deer | 2 | 7 | 33 | 9 | false | false |
| 11 | Cow | Cow | 2 | 8 | 39 | 9 | false | false |
| 12 | Sheep | Sheep | 2 | 9 | 27 | 9 | false | false |
| 13 | Claw Cat | ClawCat | 0 | 10 | 39 | 9 | false | false |
| 14 | Wolf | Wolf | 3 | 15 | 137 | 9 | false | false |
| 15 | Forest Yeti | ForestYeti | 0 | 13 | 78 | 9 | false | false |
| 16 | Chestnut Tree | ChestnutTree | 4 | 10 | 39 | 9 | false | false |
| 17 | Carnivorous Plant | CarnivorousPlant | 5 | 10 | 41 | 9 | false | false |
| 18 | Oma Warrior | Oma | 0 | 13 | 98 | 9 | false | false |
| 19 | Tiger Snake | TigerSnake | 0 | 15 | 156 | 9 | false | false |
| 20 | Spitting Spider | SpittingSpider | 6 | 10 | 44 | 9 | false | false |
| 21 | Scarecrow | Scarecrow | 0 | 10 | 35 | 9 | false | true |
| 22 | Oma | Oma | 0 | 13 | 59 | 9 | false | false |
| 23 | Oma Hero | OmaHero | 0 | 250 | 15600 | 11 | true | false |
| 24 | Cave Bat | CaveBat | 0 | 18 | 234 | 9 | false | false |
| 25 | Scorpion | Scorpion | 3 | 18 | 245 | 9 | false | false |
| 26 | Skeleton Axeman | SkeletonAxeMan | 0 | 18 | 312 | 9 | false | true |
| 27 | Skeleton | Skeleton | 0 | 18 | 284 | 9 | false | true |
| 28 | Skeleton Axe Thrower | SkeletonAxeThrower | 7 | 18 | 390 | 9 | false | true |
| 29 | Skeleton Warrior | SkeletonWarrior | 0 | 18 | 351 | 9 | false | true |
| 30 | Skeleton Lord | SkeletonLord | 0 | 250 | 19500 | 11 | true | true |
| 31 | Cave Maggot | CaveMaggot | 8 | 20 | 390 | 9 | false | false |
| 32 | GhostSorcerer | GhostSorcerer | 9 | 20 | 741 | 9 | false | true |
| 33 | Ghost Mage | GhostMage | 10 | 20 | 683 | 9 | false | true |
| 34 | Voracious Ghost | VoraciousGhost | 11 | 20 | 605 | 9 | false | true |
| 35 | Devouring Ghost | DevouringGhost | 11 | 20 | 585 | 9 | false | true |
| 36 | Corpse Raising Ghost | CorpseRaisingGhost | 11 | 20 | 546 | 9 | false | true |
| 37 | Ghoul Champion | GhoulChampion | 0 | 250 | 29250 | 11 | true | false |
| 38 | Ant Soldier | AntSoldier | 0 | 25 | 624 | 9 | false | false |
| 39 | Ant Healer | AntHealer | 12 | 25 | 819 | 9 | false | false |
| 40 | Ant Needler | AntNeedler | 7 | 25 | 878 | 9 | false | false |
| 41 | Armoured Ant | ArmoredAnt | 0 | 25 | 780 | 9 | false | false |
| 42 | Ant Commander | ArmoredAnt | 0 | 250 | 33150 | 11 | true | false |
| 43 | Beetle | Beetle | 0 | 13 | 59 | 9 | false | false |
| 44 | Corpse Devourer | ShellNipper | 0 | 16 | 69 | 9 | false | false |
| 45 | Visceral Worm | VisceralWorm | 6 | 16 | 215 | 9 | false | false |
| 46 | Mutant Flea | MutantFlea | 0 | 20 | 702 | 9 | false | false |
| 47 | Poisonous Mutant Flea | PoisonousMutantFlea | 0 | 20 | 878 | 9 | false | false |
| 48 | Blaster Mutant Flea | BlasterMutantFlea | 0 | 20 | 585 | 9 | false | false |
| 49 | Terror Spike | PoisonousMutantFlea | 0 | 250 | 25350 | 11 | true | false |
| 50 | Wasp Hatchling | WaspHatchling | 0 | 30 | 1229 | 9 | false | false |
| 51 | Centipede | Centipede | 0 | 30 | 1190 | 9 | false | false |
| 52 | Butterfly Worm | ButterflyWorm | 0 | 30 | 1502 | 9 | false | false |
| 53 | Mutant Maggot | MutantMaggot | 0 | 30 | 1326 | 9 | false | false |
| 54 | Earwig | Earwig | 0 | 30 | 1287 | 9 | false | false |
| 55 | Iron Lance | IronLance | 0 | 250 | 58500 | 11 | true | false |
| 56 | Lord Ji'Nae | LordNiJae | 13 | 250 | 292500 | 11 | true | false |
| 57 | Rotting Ghoul | RottingGhoul | 0 | 25 | 1170 | 9 | false | true |
| 58 | Decaying Ghoul | DecayingGhoul | 14 | 25 | 936 | 9 | false | true |
| 59 | Blood Thristy Ghoul | BloodThirstyGhoul | 0 | 250 | 73125 | 11 | true | true |
| 60 | Blood Thristy Zombie | BloodThirstyGhoul | 0 | 250 | 121875 | 11 | true | true |
| 61 | Spined Dark Lizard | SpinedDarkLizard | 7 | 25 | 839 | 9 | false | false |
| 62 | Uma Infidel | UmaInfidel | 0 | 25 | 780 | 9 | false | true |
| 63 | Uma Flame Thrower | UmaFlameThrower | 15 | 25 | 878 | 9 | false | true |
| 64 | Uma Anguisher | UmaAnguisher | 0 | 250 | 39000 | 11 | true | false |
| 65 | Uma King | UmaKing | 16 | 250 | 195000 | 11 | true | false |
| 66 | Spider Bat | SpiderBat | 8 | 35 | 1268 | 9 | false | false |
| 67 | Arachnid Gazer | ArachnidGazer | 17 | 35 | 2340 | 9 | false | false |
| 68 | Larva | Larva | 18 | 250 | 0 | 9 | false | false |
| 69 | Red Moon Guardian | RedMoonGuardian | 0 | 40 | 1560 | 9 | false | false |
| 70 | Red Moon Protector | RedMoonProtector | 0 | 40 | 1755 | 9 | false | false |
| 71 | Venomous Arachnid | VenomousArachnid | 14 | 35 | 1463 | 9 | false | false |
| 72 | Dark Arachnid | DarkArachnid | 0 | 35 | 1365 | 9 | false | false |
| 73 | Arachnid Broodmother | DarkArachnid | 0 | 250 | 68250 | 11 | true | false |
| 74 | Red Moon Royal Guard | RedMoonProtector | 0 | 250 | 78000 | 11 | true | false |
| 75 | Red Moon The Fallen | RedMoonTheFallen | 19 | 250 | 487500 | 15 | true | true |
| 76 | Zuma Sharpshooter | ZumaSharpShooter | 20 | 45 | 8190 | 9 | false | false |
| 77 | Zuma Fanatic | ZumaFanatic | 21 | 45 | 5850 | 9 | false | true |
| 78 | Zuma Guardian | ZumaGuardian | 21 | 45 | 6825 | 9 | false | true |
| 79 | Vicious Rat | ViciousRat | 0 | 45 | 4875 | 9 | false | false |
| 80 | Zuma Keeper | ZumaGuardian | 21 | 250 | 117000 | 9 | true | true |
| 81 | Zuma King | ZumaKing | 22 | 250 | 780000 | 11 | true | true |
| 82 | Evil Fanatic | EvilFanatic | 0 | 46 | 4485 | 9 | false | false |
| 83 | Monkey | Monkey | 23 | 46 | 3510 | 9 | false | false |
| 84 | Evil Monkey | Monkey | 24 | 46 | 3120 | 9 | false | false |
| 85 | Evil Elephant | EvilElephant | 25 | 47 | 7800 | 9 | false | false |
| 86 | Cannibal Fanatic | CannibalFanatic | 7 | 46 | 4875 | 9 | false | false |
| 87 | Crazed Warrior | EvilFanatic | 0 | 250 | 107250 | 11 | true | false |
| 88 | Spiked Beetle | SpikedBeetle | 0 | 250 | 9750 | 11 | true | false |
| 89 | Numa Grunt | NumaGrunt | 0 | 35 | 1560 | 9 | false | false |
| 90 | Numa Mage | NumaMage | 26 | 35 | 2535 | 5 | false | false |
| 91 | Numa Elite | NumaElite | 0 | 35 | 1755 | 9 | false | false |
| 92 | Sand Shark | SandShark | 25 | 35 | 1794 | 9 | false | true |
| 93 | Stone Golem | StoneGolem | 27 | 35 | 1658 | 9 | false | false |
| 94 | Windfury Sorceress | WindfurySorceress | 28 | 35 | 2048 | 9 | false | true |
| 95 | Cursed Cactus | CursedCactus | 29 | 35 | 2925 | 9 | false | false |
| 96 | Netherworld Gate | NetherWorldGate | 30 | 250 | 0 | 9 | true | false |
| 97 | Raging Lizard | RagingLizard | 0 | 68 | 31200 | 9 | false | false |
| 98 | Numa Elder Shaman | NumaMage | 26 | 250 | 97500 | 11 | true | false |
| 99 | Saw Tooth Lizard | SawToothLizard | 0 | 68 | 35100 | 10 | false | false |
| 100 | Venom Spitter | VenomSpitter | 14 | 68 | 38025 | 10 | false | false |
| 101 | Sonic Lizard | SonicLizard | 31 | 68 | 49725 | 10 | false | false |
| 102 | Giant Lizard | GiantLizard | 33 | 68 | 54600 | 10 | false | false |
| 103 | Crazed Lizard | CrazedLizard | 34 | 68 | 46800 | 10 | false | false |
| 104 | Tainted Terror | TaintedTerror | 0 | 250 | 585000 | 13 | true | false |
| 105 | Death Lord Jichon | DeathLordJichon | 0 | 250 | 1755000 | 13 | false | false |
| 106 | Mutant Lizard | MutantLizard | 0 | 68 | 41925 | 10 | false | false |
| 107 | Minotaur | Minotaur | 0 | 45 | 4290 | 10 | false | false |
| 108 | Frost Minotaur | FrostMinotaur | 35 | 45 | 5070 | 10 | false | false |
| 109 | Banya Right Guard | BanyaRightGuard | 36 | 45 | 8775 | 9 | false | false |
| 110 | Shock Minotaur | ShockMinotaur | 37 | 45 | 5070 | 9 | false | false |
| 111 | Banya Left Guard | BanyaLeftGuard | 38 | 45 | 9360 | 9 | false | false |
| 112 | Fury Minotaur | FuryMinotaur | 39 | 45 | 5070 | 9 | false | false |
| 113 | Flame Minotaur | FlameMinotaur | 40 | 45 | 5070 | 9 | false | false |
| 114 | Banya Guardian | Minotaur | 0 | 250 | 1126750 | 22 | true | false |
| 115 | Emperor Sa'Woo | EmperorSaWoo | 41 | 250 | 585000 | 10 | true | true |
| 116 | Bone Archer | BoneArcher | 7 | 35 | 1950 | 9 | false | true |
| 117 | Bone Bladesman | BoneBladesman | 0 | 35 | 1365 | 9 | false | true |
| 118 | Bone Captain | BoneCaptain | 0 | 35 | 1716 | 9 | false | true |
| 119 | Bone Soldier | BoneSoldier | 42 | 35 | 1658 | 9 | false | true |
| 120 | Skeleton Enforcer | BoneCaptain | 0 | 250 | 52650 | 10 | true | true |
| 121 | Arch Lich Taedu | ArchLichTaedu | 43 | 250 | 370500 | 10 | true | true |
| 122 | Wedge Moth Larva | WedgeMothLarva | 44 | 30 | 1365 | 9 | false | false |
| 123 | Lesser Wedge Moth | LesserWedgeMoth | 0 | 30 | 0 | 9 | false | false |
| 124 | Wedge Moth | WedgeMoth | 8 | 30 | 1268 | 9 | false | false |
| 125 | Red Boar | RedBoar | 0 | 30 | 897 | 9 | false | true |
| 126 | Claw Serpent | ClawSerpent | 0 | 30 | 1365 | 9 | false | true |
| 127 | Black Boar | BlackBoar | 0 | 30 | 897 | 9 | false | true |
| 128 | Tusk Lord | TuskLord | 0 | 250 | 48750 | 10 | true | true |
| 129 | Razor Tusk | RazorTusk | 45 | 250 | 331500 | 10 | true | true |
| 130 | Pink Goddess Of Black Palace | PinkGoddess | 46 | 50 | 9750 | 9 | false | false |
| 131 | Green Goddess Of Black Palace | GreenGoddess | 47 | 50 | 9750 | 9 | false | false |
| 132 | Mutant Captain | MutantCaptain | 48 | 50 | 15600 | 9 | false | false |
| 133 | Stone Griffin | StoneGriffin | 49 | 52 | 13650 | 13 | false | false |
| 134 | Flame Griffin | FlameGriffin | 50 | 52 | 11700 | 13 | false | false |
| 135 | Black Palace Warlord | MutantCaptain | 48 | 250 | 156000 | 10 | true | false |
| 136 | Pink Goddess Of Underground | PinkGoddess | 46 | 55 | 15600 | 9 | false | false |
| 137 | Vicious Mutant Captain | MutantCaptain | 48 | 55 | 23400 | 9 | false | false |
| 138 | Green Goddess Of Underground | GreenGoddess | 47 | 55 | 15600 | 9 | false | false |
| 139 | Jinchon Warlord | MutantCaptain | 48 | 250 | 214500 | 10 | true | false |
| 140 | SummonPuppet | None | 51 | 250 | 0 | 0 | false | false |
| 141 | Apparition Archer | BoneArcher | 7 | 75 | 1950 | 10 | false | false |
| 142 | Apparition Bladesman | BoneBladesman | 0 | 75 | 1950 | 10 | false | false |
| 143 | Apparition Soldier | BoneSoldier | 42 | 75 | 1950 | 10 | false | false |
| 144 | Skeleton | WhiteBone | 52 | 17 | 0 | 9 | false | true |
| 145 | Jin Skeleton | WhiteBone | 52 | 33 | 0 | 9 | false | true |
| 146 | Shinsu | Shinsu | 53 | 30 | 0 | 9 | false | true |
| 147 | Infernal Soldier | InfernalSoldier | 103 | 50 | 0 | 9 | false | true |
| 150 | MirrorImage | None | 55 | 250 | 0 | 0 | false | false |
| 151 | Corpse Stalker | CorpseStalker | 0 | 75 | 41438 | 8 | false | true |
| 152 | Light Armed Soldier | LightArmedSoldier | 0 | 75 | 42413 | 9 | false | true |
| 153 | Corrosive Poison Spitter | CorrosivePoisonSpitter | 56 | 75 | 56063 | 12 | false | true |
| 154 | Phantom Soldier | PhantomSoldier | 0 | 75 | 73125 | 9 | false | true |
| 155 | Mutated Octopus | MutatedOctopus | 57 | 75 | 47532 | 12 | false | true |
| 156 | Aqua Lizard | AquaLizard | 0 | 75 | 41925 | 9 | false | true |
| 157 | Stomper | Stomper | 58 | 75 | 46800 | 7 | false | true |
| 158 | Crimson Necromancer | CrimsonNecromancer | 59 | 75 | 68250 | 12 | false | true |
| 159 | Chaos Knight | ChaosKnight | 60 | 250 | 292500 | 13 | true | true |
| 160 | Pachon The Chaos bringer | PachonTheChaosBringer | 61 | 250 | 1365000 | 15 | true | true |
| 161 | Numa Cavalry | NumaCavalry | 33 | 72 | 37050 | 10 | false | false |
| 162 | Numa High Mage | NumaHighMage | 62 | 72 | 35100 | 10 | false | true |
| 163 | Numa Stone Thrower | NumaStoneThrower | 63 | 72 | 58500 | 10 | false | false |
| 164 | Numa Royal Guard | NumaRoyalGuard | 48 | 72 | 39000 | 10 | false | false |
| 165 | Numa Armored Soldier | NumaArmoredSoldier | 64 | 72 | 33930 | 10 | false | false |
| 166 | Numa Assault Captain | NumaRoyalGuard | 48 | 72 | 370500 | 10 | true | false |
| 167 | Icy Ranger | IcyRanger | 34 | 80 | 107250 | 13 | false | false |
| 168 | Icy Goddess | IcyGoddess | 65 | 80 | 78000 | 10 | false | false |
| 169 | Icy Spirit Warrior | IcySpiritWarrior | 67 | 80 | 87750 | 11 | false | false |
| 170 | Icy Spirit General | IcySpiritGeneral | 66 | 100 | 97500 | 11 | false | false |
| 171 | Ghost Knight | GhostKnight | 25 | 75 | 81900 | 10 | false | false |
| 172 | Icy Spirit Spearman | IcySpiritSpearman | 0 | 80 | 62400 | 12 | false | false |
| 173 | Werewolf | Werewolf | 68 | 75 | 72150 | 12 | false | false |
| 174 | Whitefang | Whitefang | 23 | 75 | 66690 | 14 | false | false |
| 175 | Icy Spirit Solider | IcySpiritSolider | 24 | 80 | 74100 | 12 | false | false |
| 176 | Wild Boar | WildBoar | 0 | 75 | 64350 | 11 | false | false |
| 177 | Jinam Stone Gate | JinamStoneGate | 69 | 250 | 0 | 2 | true | false |
| 178 | Frost Lord Hwa | FrostLordHwa | 70 | 250 | 1852500 | 10 | true | false |
| 179 | Bloody Armed Beetle | SpikedBeetle | 0 | 80 | 140400 | 13 | false | false |
| 180 | Golden Armored Beetle | Beetle | 0 | 80 | 19500 | 13 | false | false |
| 181 | Earwig King | IronLance | 0 | 80 | 140400 | 13 | false | false |
| 182 | Mature Earwig | Earwig | 0 | 80 | 29250 | 13 | false | false |
| 183 | Millipede | Centipede | 0 | 80 | 39000 | 13 | false | false |
| 184 | Enraged Lord Ji'Nae | LordNiJae | 77 | 250 | 2925000 | 11 | true | false |
| 185 | Banyo Soldier | RottingGhoul | 0 | 80 | 120900 | 16 | false | false |
| 186 | Banyo Warrior | LightArmedSoldier | 117 | 80 | 146250 | 16 | false | false |
| 187 | Banyo Captain | PhantomSoldier | 72 | 80 | 136000 | 16 | false | false |
| 188 | Banyo Lord Guzak | PachonTheChaosBringer | 74 | 250 | 3900000 | 16 | true | true |
| 189 | Pig | Companion_Pig | -2 | 0 | 0 | 6 | false | false |
| 190 | Tusk Lord | Companion_TuskLord | -2 | 0 | 0 | 7 | false | false |
| 191 | Skeleton Lord | Companion_SkeletonLord | -2 | 0 | 0 | 7 | false | false |
| 192 | Griffin | Companion_Griffin | -2 | 0 | 0 | 7 | false | false |
| 193 | Dragon | Companion_Dragon | -2 | 0 | 0 | 7 | false | false |
| 194 | Donkey | Companion_Donkey | -2 | 0 | 0 | 7 | false | false |
| 195 | Sheep | Companion_Sheep | -2 | 0 | 0 | 7 | false | false |
| 196 | Pachon  | Companion_BanyoLordGuzak | -2 | 0 | 0 | 7 | false | false |
| 197 | Panda | Companion_Panda | -2 | 0 | 0 | 7 | false | false |
| 198 | Rabbit | Companion_Rabbit | -2 | 0 | 0 | 7 | false | false |
| 199 | Jinchon Devil | JinchonDevil | 78 | 250 | 975000 | 12 | true | true |
| 200 | Black Palace Demon | JinchonDevil | 78 | 250 | 877500 | 12 | true | true |
| 201 | Brass Feral Warrior | FlameMinotaur | 79 | 80 | 109200 | 11 | false | false |
| 202 | Obsidian Feral Warrior | FuryMinotaur | 79 | 80 | 109200 | 11 | false | false |
| 203 | Sun Feral Warrior | BanyaLeftGuard | 80 | 80 | 122850 | 11 | false | false |
| 204 | Moon Feral Warrior | BanyaRightGuard | 81 | 80 | 126750 | 11 | false | false |
| 205 | Ox Feral General | UmaAnguisher | 82 | 80 | 104325 | 11 | false | false |
| 206 | Flame Demon | UmaFlameThrower | 83 | 80 | 101400 | 11 | false | false |
| 207 | Winged Horror | UmaKing | 84 | 250 | 585000 | 11 | true | true |
| 208 | Enraged Emperor Sa'Woo | EmperorSaWoo | 85 | 250 | 2340000 | 11 | true | true |
| 209 | Ferocious Flame Demon | UmaFlameThrower | 86 | 80 | 76050 | 11 | false | false |
| 210 | Oma Warlord | OmaWarlord | 87 | 80 | 78000 | 11 | false | false |
| 211 | Goru Spearman | BoneSoldier | 88 | 80 | 109200 | 11 | false | true |
| 212 | Goru Archer | BoneArcher | 89 | 80 | 222850 | 12 | false | true |
| 213 | Goru General | BoneCaptain | 90 | 80 | 117000 | 12 | false | true |
| 215 | Enraged Arch Lich Taedu | ArchLichTaedu | 91 | 250 | 1852500 | 35 | true | true |
| 216 | Escort Commander | EscortCommander | 93 | 80 | 136500 | 33 | false | false |
| 217 | Fiery Dancer | FieryDancer | 94 | 80 | 130650 | 33 | false | false |
| 218 | Emerald Dancer | EmeraldDancer | 95 | 100 | 240400 | 33 | false | false |
| 219 | Queen Of Dawn | QueenOfDawn | 96 | 250 | 1950000 | 33 | true | false |
| 220 | Sabuk Lord | JinchonDevil | 1000 | 250 | 0 | 12 | true | true |
| 221 | Oyoung Beast | OYoungBeast | 97 | 80 | 23400 | 9 | false | false |
| 222 | Yumgon Witch | YumgonWitch | 98 | 80 | 84825 | 11 | false | false |
| 223 | Ma Warden | OYoungBeast | 97 | 80 | 70200 | 9 | false | false |
| 224 | Ma Warlord | MaWarlord | 64 | 80 | 73125 | 9 | false | false |
| 225 | Jinhwan Spirit | JinhwanSpirit | 99 | 80 | 24375 | 9 | false | false |
| 226 | Jinhwan Guardian | JinhwanGuardian | 26 | 80 | 30810 | 10 | false | false |
| 227 | Oyoung General | MaWarlord | 64 | 80 | 26325 | 9 | false | false |
| 228 | Yumgon General | YumgonGeneral | 0 | 80 | 81900 | 11 | false | false |
| 229 | Chiwoo General Of East | ChiwooGeneral | 100 | 250 | 819000 | 10 | true | false |
| 230 | Chiwoo General Of West | ChiwooGeneral | 100 | 250 | 819000 | 10 | true | false |
| 231 | Dragon Queen Jin'Ru | DragonQueen | 101 | 250 | 3510000 | 44 | true | false |
| 232 | Dragon Lord Jin'Ryung | DragonLord | 102 | 250 | 3997500 | 44 | true | false |
| 233 | Ferocious Ice Tiger | FerociousIceTiger | 104 | 250 | 1599000 | 12 | true | false |
| 244 | Escort Commander | EscortCommander | 0 | 80 | 136500 | 33 | false | false |
| 245 | Sama Cursed Bladesman | SamaCursedBladesman | 71 | 85 | 441090 | 10 | false | false |
| 246 | Sama Cursed Flame Mage | SamaCursedFlameMage | 106 | 85 | 304200 | 10 | false | false |
| 248 | Sama Cursed Slave | SamaCursedSlave | 105 | 85 | 327015 | 10 | false | false |
| 249 | Sama Fire Guardian | SamaFireGuardian | 107 | 88 | 524745 | 11 | false | false |
| 250 | Sama Ice Guardian | SamaIceGuardian | 108 | 88 | 692664 | 11 | false | false |
| 251 | Sama Lightning Guardian | SamaLightningGuardian | 109 | 88 | 608705 | 11 | false | false |
| 252 | Sama Wind Guardian | SamaWindGuardian | 110 | 88 | 629694 | 11 | false | false |
| 253 | Black Sama | BlackTortoise | 112 | 250 | 6084000 | 10 | true | false |
| 254 | Blue Sama | BlueDragon | 113 | 250 | 4461600 | 10 | true | false |
| 255 | Phoenix Sama | Phoenix | 111 | 250 | 3650400 | 10 | true | false |
| 256 | White Tiger Sama | WhiteTiger | 114 | 250 | 4867200 | 10 | true | false |
| 258 | Enshrinement Box | EnshrinementBox | 4 | 85 | 3900000 | 0 | false | false |
| 259 | Sama Prophet | SamaProphet | 115 | 250 | 10545600 | 12 | true | false |
| 260 | Sama Sorcerer | SamaSorcerer | 116 | 250 | 12421500 | 12 | true | false |
| 261 | Blood Stone | BloodStone | 4 | 250 | 0 | 0 | false | false |
| 262 | Life Stone | BloodStone | 4 | 250 | 0 | 0 | false | false |
| 263 | Dark Stone | BloodStone | 4 | 250 | 0 | 0 | false | false |
| 264 | Young Tiger | OrangeTiger | 0 | 58 | 97500 | 7 | false | false |
| 265 | Tiger | RegularTiger | 0 | 63 | 130000 | 7 | false | false |
| 266 | Blood Tiger | RedTiger | 6 | 90 | 56250 | 30 | false | false |
| 267 | Blizzard Tiger | SnowTiger | 68 | 90 | 56250 | 30 | false | false |
| 268 | Dark Tiger | BlackTiger | 0 | 90 | 56250 | 30 | false | false |
| 269 | Elder Dark Tiger | BigBlackTiger | 0 | 250 | 750000 | 30 | true | false |
| 270 | Elder White Tiger | BigWhiteTiger | 68 | 250 | 1000000 | 30 | true | false |
| 271 | Tiger General | OrangeBossTiger | 0 | 250 | 0 | 7 | false | false |
| 272 | Tiger War Lord | FerociousIceTiger | 104 | 250 | 2500000 | 15 | true | false |
| 273 | Wild Elephant | EvilElephant | 0 | 60 | 119600 | 8 | false | false |
| 274 | Wild Monkey | WildMonkey | 23 | 63 | 83200 | 9 | false | false |
| 275 | Wild Fanatic | EvilFanatic | 0 | 61 | 135135 | 10 | false | false |
| 276 | Frost Yeti | FrostYeti | 0 | 90 | 56250 | 30 | false | false |
| 277 | Evil Snake | EvilSnake | 0 | 85 | 864000 | 10 | false | false |
| 278 | Salamander | Salamander | 0 | 85 | 1104000 | 9 | false | false |
| 279 | Sand Golem | SandGolem | 27 | 85 | 1200000 | 10 | false | false |
| 284 | Oma Mage | OmaMage | 118 | 100 | 600000 | 17 | false | false |
| 291 | Crystal Golem | CrystalGolem | 0 | 85 | 1344000 | 10 | false | false |
| 292 | Dust Devil | DustDevil | 0 | 100 | 864000 | 13 | false | false |
| 293 | Twin Tail Scorpion | TwinTailScorpion | 119 | 85 | 816000 | 10 | false | false |
| 294 | Bloody Mole | BloodyMole | 0 | 85 | 1008000 | 4 | false | false |
| 295 | Imp | SDMob19 | 0 | 100 | 1760000 | 10 | false | false |
| 296 | Ettin | SDMob20 | 0 | 92 | 2200000 | 12 | false | false |
| 297 | Centurion | SDMob21 | 89 | 100 | 1610000 | 11 | false | false |
| 298 | Rot Wraith | SDMob22 | 7 | 93 | 2475000 | 16 | false | false |
| 299 | Cotoblepas | SDMob23 | 0 | 89 | 2200000 | 12 | false | false |
| 300 | Azog | SDMob24 | 0 | 250 | 40000000 | 500 | true | false |
| 301 | Urukhia | SDMob25 | 0 | 250 | 40000000 | 500 | true | false |
| 302 | Gang Spider | GangSpider | 0 | 85 | 672000 | 10 | false | false |
| 303 | Venom Spider | VenomSpider | 46 | 85 | 1056000 | 10 | false | false |
| 304 | Chubarak | SDMob26 | 0 | 250 | 40000000 | 500 | true | false |
| 305 | Doom Claw | LobsterLord | 120 | 500 | 15000000 | 7 | true | false |
| 307 | Zauhk Spawn | NewMob5 | 0 | 250 | 5000000 | 100 | false | false |
| 308 | Shell Spliter | NewMob9 | 123 | 250 | 6000 | 22 | false | false |
| 309 | Ember Mage | NewMob7 | 111 | 250 | 15300000 | 35 | false | false |
| 310 | Bobbit Worm | NewMob2 | 125 | 250 | 3600000 | 2 | false | false |
| 311 | Cobalt Golum | NewMob4 | 59 | 250 | 6668250 | 15 | false | true |
| 312 | Shimmer Wings | NewMob1 | 121 | 250 | 3240400 | 13 | false | false |
| 313 | Vex Wings | NewMob3 | 42 | 250 | 4200000 | 12 | false | false |
| 314 | Rot Wraith | SDMob22 | 7 | 93 | 2475000 | 16 | false | false |
| 331 | Rot Wraith | SDMob22 | 7 | 93 | 2475000 | 16 | false | false |
| 332 | Ember SpearMan | NewMob6 | 88 | 250 | 14750000 | 15 | false | false |
| 333 | Kongeegen | NewMob10 | 124 | 250 | 3900000 | 0 | false | false |
| 334 | Adamantoise | NewMob8 | 122 | 250 | 40000000 | 500 | true | false |
| 335 | Zauhk | NewMob5 | 117 | 250 | 9000000 | 6 | true | false |
| 336 | MonasteryRaisingGhost | CorpseRaisingGhost | 10 | 79 | 2200000 | 12 | false | true |
| 337 | MonasteryGhoul | GhostMage | 119 | 79 | 2200000 | 13 | false | true |
| 338 | MonasterySorcer | GhostSorcerer | 9 | 79 | 2200000 | 13 | false | true |
| 339 | MonasteryVoracious | VoraciousGhost | 0 | 89 | 2200000 | 13 | false | true |
| 341 | MonasteryDevour | DevouringGhost | 10 | 79 | 2200000 | 13 | false | false |
| 342 | Sumerian | MonasteryMon4 | 126 | 250 | 50000000 | 15 | true | true |
| 343 | Sacrifice | MonasteryMon5 | 117 | 250 | 50000000 | 15 | true | true |
| 344 | Enheduanna | MonasteryMon2 | 117 | 250 | 3240400 | 15 | false | true |
| 345 | Quadishtu | MonasteryMon3 | 0 | 250 | 3240400 | 9 | false | true |
| 347 | Sumerian King | MonasteryMon6 | 127 | 250 | 50000000 | 19 | true | true |
| 348 | Puabi | MonasteryMon1 | 0 | 250 | 3240400 | 14 | false | true |
| 349 | Bobbit Bobbit | NewMob2 | 125 | 250 | 33600000 | 2 | false | false |
| 350 | Sabuk Flag | CastleFlag | 1001 | 250 | 0 | 5 | true | false |

### #1 · Guard

| 字段 | 值 |
|---|---|
| MonsterName | Guard |
| Image | Guard |
| AI | -1 |
| Level | 250 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Stats | MaxDC 438, Accuracy 1000 |

### #8 · Chicken

| 字段 | 值 |
|---|---|
| MonsterName | Chicken |
| Image | Chicken |
| AI | 1 |
| Level | 5 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 11 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 10 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 7, MinMR 2, MaxMR 2, MaxDC 1, Accuracy 6, Agility 5 |

### #9 · Pig

| 字段 | 值 |
|---|---|
| MonsterName | Pig |
| Image | Pig |
| AI | 2 |
| Level | 6 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 15 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 11 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 15, MinMR 2, MaxMR 2, MaxDC 2, Accuracy 8, Agility 5 |

### #10 · Deer

| 字段 | 值 |
|---|---|
| MonsterName | Deer |
| Image | Deer |
| AI | 2 |
| Level | 7 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 33 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 10 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 20, MinMR 2, MaxMR 2, MinDC 1, MaxDC 3, Accuracy 9, Agility 8 |

### #11 · Cow

| 字段 | 值 |
|---|---|
| MonsterName | Cow |
| Image | Cow |
| AI | 2 |
| Level | 8 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 39 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 11 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 25, MinMR 2, MaxMR 2, MinDC 1, MaxDC 4, Accuracy 10, Agility 5 |

### #12 · Sheep

| 字段 | 值 |
|---|---|
| MonsterName | Sheep |
| Image | Sheep |
| AI | 2 |
| Level | 9 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 27 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 10 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 20, MinMR 2, MaxMR 2, MinDC 1, MaxDC 2, Accuracy 7, Agility 8 |

### #13 · Claw Cat

| 字段 | 值 |
|---|---|
| MonsterName | Claw Cat |
| Image | ClawCat |
| AI | 0 |
| Level | 10 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 39 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 37 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 3 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 20, MinMR 2, MaxMR 2, MinDC 3, MaxDC 6, Accuracy 11, Agility 9 |

### #14 · Wolf

| 字段 | 值 |
|---|---|
| MonsterName | Wolf |
| Image | Wolf |
| AI | 3 |
| Level | 15 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 137 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 9 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 3 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 60, MinMR 4, MaxMR 4, MinDC 5, MaxDC 8, Accuracy 13, Agility 11 |

### #15 · Forest Yeti

| 字段 | 值 |
|---|---|
| MonsterName | Forest Yeti |
| Image | ForestYeti |
| AI | 0 |
| Level | 13 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 78 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 52 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 3 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 40, MinMR 3, MaxMR 3, MinDC 5, MaxDC 10, Accuracy 12, Agility 10 |

### #16 · Chestnut Tree

| 字段 | 值 |
|---|---|
| MonsterName | Chestnut Tree |
| Image | ChestnutTree |
| AI | 4 |
| Level | 10 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 39 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 3 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 9 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 3 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 7, MinMR 500, MaxMR 500 |

### #17 · Carnivorous Plant

| 字段 | 值 |
|---|---|
| MonsterName | Carnivorous Plant |
| Image | CarnivorousPlant |
| AI | 5 |
| Level | 10 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 41 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 9 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 21, MinMR 2, MaxMR 2, MinDC 3, MaxDC 8, Accuracy 11, Agility 9 |

### #18 · Oma Warrior

| 字段 | 值 |
|---|---|
| MonsterName | Oma Warrior |
| Image | Oma |
| AI | 0 |
| Level | 13 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 98 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 51 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 50, MinMR 3, MaxMR 3, MinDC 3, MaxDC 12, Accuracy 12, Agility 10 |

### #19 · Tiger Snake

| 字段 | 值 |
|---|---|
| MonsterName | Tiger Snake |
| Image | TigerSnake |
| AI | 0 |
| Level | 15 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 156 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2300 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 58 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 70, MinMR 4, MaxMR 4, MinDC 3, MaxDC 15, Accuracy 13, Agility 11 |

### #20 · Spitting Spider

| 字段 | 值 |
|---|---|
| MonsterName | Spitting Spider |
| Image | SpittingSpider |
| AI | 6 |
| Level | 10 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 44 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 10 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 6 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 22, MinMR 2, MaxMR 2, MinDC 2, MaxDC 9, MinSC 2, MaxSC 5, Accuracy 11, Agility 9, DarkAffinity 1 |

### #21 · Scarecrow

| 字段 | 值 |
|---|---|
| MonsterName | Scarecrow |
| Image | Scarecrow |
| AI | 0 |
| Level | 10 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 35 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 37 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 3 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 15, MinMR 2, MaxMR 2, MinDC 2, MaxDC 4, Accuracy 7, Agility 9 |

### #22 · Oma

| 字段 | 值 |
|---|---|
| MonsterName | Oma |
| Image | Oma |
| AI | 0 |
| Level | 13 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 59 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 51 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 25, MinMR 3, MaxMR 3, MinDC 3, MaxDC 8, Accuracy 12, Agility 10 |

### #23 · Oma Hero

| 字段 | 值 |
|---|---|
| MonsterName | Oma Hero |
| Image | OmaHero |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 15600 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 59 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 500, MinAC 3, MaxAC 3, MinMR 17, MaxMR 17, MinDC 25, MaxDC 125, Accuracy 25, Agility 23 |

### #24 · Cave Bat

| 字段 | 值 |
|---|---|
| MonsterName | Cave Bat |
| Image | CaveBat |
| AI | 0 |
| Level | 18 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 234 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 9 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 16 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 90, MinMR 5, MaxMR 5, MinDC 10, MaxDC 18, Accuracy 14, Agility 12 |

### #25 · Scorpion

| 字段 | 值 |
|---|---|
| MonsterName | Scorpion |
| Image | Scorpion |
| AI | 3 |
| Level | 18 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 245 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 8 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 95, MinMR 5, MaxMR 5, MinDC 10, MaxDC 19, Accuracy 14, Agility 12 |

### #26 · Skeleton Axeman

| 字段 | 值 |
|---|---|
| MonsterName | Skeleton Axeman |
| Image | SkeletonAxeMan |
| AI | 0 |
| Level | 18 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 312 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 9 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 104 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 105, MinMR 5, MaxMR 5, MinDC 10, MaxDC 21, Accuracy 14, Agility 12 |

### #27 · Skeleton

| 字段 | 值 |
|---|---|
| MonsterName | Skeleton |
| Image | Skeleton |
| AI | 0 |
| Level | 18 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 284 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 2300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 9 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 106 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 100, MinMR 5, MaxMR 5, MinDC 10, MaxDC 20, Accuracy 14, Agility 12 |

### #28 · Skeleton Axe Thrower

| 字段 | 值 |
|---|---|
| MonsterName | Skeleton Axe Thrower |
| Image | SkeletonAxeThrower |
| AI | 7 |
| Level | 18 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 390 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 104 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 100, MinMR 5, MaxMR 5, MinDC 11, MaxDC 16, Accuracy 14, Agility 12 |

### #29 · Skeleton Warrior

| 字段 | 值 |
|---|---|
| MonsterName | Skeleton Warrior |
| Image | SkeletonWarrior |
| AI | 0 |
| Level | 18 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 351 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2300 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 104 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 110, MinMR 5, MaxMR 5, MinDC 10, MaxDC 23, Accuracy 14, Agility 12 |

### #30 · Skeleton Lord

| 字段 | 值 |
|---|---|
| MonsterName | Skeleton Lord |
| Image | SkeletonLord |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 19500 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 94 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 850, MinAC 6, MaxAC 6, MinMR 20, MaxMR 20, MinDC 38, MaxDC 138, Accuracy 25, Agility 23 |

### #31 · Cave Maggot

| 字段 | 值 |
|---|---|
| MonsterName | Cave Maggot |
| Image | CaveMaggot |
| AI | 8 |
| Level | 20 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 390 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 8 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 8 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 100, MinMR 6, MaxMR 6, MinDC 13, MaxDC 13, Accuracy 15, Agility 13, DarkAffinity 1 |

### #32 · GhostSorcerer

| 字段 | 值 |
|---|---|
| MonsterName | GhostSorcerer |
| Image | GhostSorcerer |
| AI | 9 |
| Level | 20 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 741 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 8 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 81 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 160, MinMR 6, MaxMR 6, MinDC 11, MaxDC 25, Accuracy 15, Agility 13, LightningAffinity 1 |

### #33 · Ghost Mage

| 字段 | 值 |
|---|---|
| MonsterName | Ghost Mage |
| Image | GhostMage |
| AI | 10 |
| Level | 20 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 683 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 14 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 79 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 140, MinMR 6, MaxMR 6, MinDC 13, MaxDC 25, Accuracy 15, Agility 13, FireAffinity 1, IceAffinity 1, LightningAffinity 1, WindAffinity 1, HolyAffinity 1, DarkAffinity 1, PhantomAffinity 1 |

### #34 · Voracious Ghost

| 字段 | 值 |
|---|---|
| MonsterName | Voracious Ghost |
| Image | VoraciousGhost |
| AI | 11 |
| Level | 20 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 605 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 76 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 120, MinMR 6, MaxMR 6, MinDC 13, MaxDC 24, Accuracy 15, Agility 13 |

### #35 · Devouring Ghost

| 字段 | 值 |
|---|---|
| MonsterName | Devouring Ghost |
| Image | DevouringGhost |
| AI | 11 |
| Level | 20 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 585 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 76 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 120, MinMR 6, MaxMR 6, MinDC 13, MaxDC 24, Accuracy 15, Agility 13 |

### #36 · Corpse Raising Ghost

| 字段 | 值 |
|---|---|
| MonsterName | Corpse Raising Ghost |
| Image | CorpseRaisingGhost |
| AI | 11 |
| Level | 20 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 546 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 76 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 100, MinMR 6, MaxMR 6, MinDC 13, MaxDC 23, Accuracy 15, Agility 13 |

### #37 · Ghoul Champion

| 字段 | 值 |
|---|---|
| MonsterName | Ghoul Champion |
| Image | GhoulChampion |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 29250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 61 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 1300, MinAC 8, MaxAC 8, MinMR 23, MaxMR 23, MinDC 50, MaxDC 150, Accuracy 25, Agility 23 |

### #38 · Ant Soldier

| 字段 | 值 |
|---|---|
| MonsterName | Ant Soldier |
| Image | AntSoldier |
| AI | 0 |
| Level | 25 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 624 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1400 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 67 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 200, MinMR 11, MaxMR 11, MinDC 8, MaxDC 19, Accuracy 17, Agility 13 |

### #39 · Ant Healer

| 字段 | 值 |
|---|---|
| MonsterName | Ant Healer |
| Image | AntHealer |
| AI | 12 |
| Level | 25 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 819 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 11 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 220, MinAC 12, MaxAC 12, MinMR 11, MaxMR 11, MinSC 5, MaxSC 15, Accuracy 17, Agility 15, Healing 100, HealingCap 5 |

### #40 · Ant Needler

| 字段 | 值 |
|---|---|
| MonsterName | Ant Needler |
| Image | AntNeedler |
| AI | 7 |
| Level | 25 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 878 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 2500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 240, MinMR 11, MaxMR 11, MinDC 8, MaxDC 19, Accuracy 17, Agility 13 |

### #41 · Armoured Ant

| 字段 | 值 |
|---|---|
| MonsterName | Armoured Ant |
| Image | ArmoredAnt |
| AI | 0 |
| Level | 25 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 780 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 260, MinMR 11, MaxMR 11, MinDC 8, MaxDC 25, Accuracy 17, Agility 13 |

### #42 · Ant Commander

| 字段 | 值 |
|---|---|
| MonsterName | Ant Commander |
| Image | ArmoredAnt |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 33150 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 25 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1700, MinAC 9, MaxAC 9, MinMR 25, MaxMR 25, MinDC 63, MaxDC 163, Accuracy 25, Agility 23 |

### #43 · Beetle

| 字段 | 值 |
|---|---|
| MonsterName | Beetle |
| Image | Beetle |
| AI | 0 |
| Level | 13 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 59 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 53 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 25, MinMR 3, MaxMR 3, MinDC 5, MaxDC 10, Accuracy 12, Agility 10 |

### #44 · Corpse Devourer

| 字段 | 值 |
|---|---|
| MonsterName | Corpse Devourer |
| Image | ShellNipper |
| AI | 0 |
| Level | 16 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 69 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2400 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 52 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 30, MinMR 3, MaxMR 3, MinDC 5, MaxDC 11, Accuracy 12, Agility 10 |

### #45 · Visceral Worm

| 字段 | 值 |
|---|---|
| MonsterName | Visceral Worm |
| Image | VisceralWorm |
| AI | 6 |
| Level | 16 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 215 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2300 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 10 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 11 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 40, MinMR 3, MaxMR 3, MinDC 5, MaxDC 13, MinSC 2, MaxSC 5, Accuracy 12, Agility 10, DarkAffinity 1 |

### #46 · Mutant Flea

| 字段 | 值 |
|---|---|
| MonsterName | Mutant Flea |
| Image | MutantFlea |
| AI | 0 |
| Level | 20 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 702 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 1700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 67 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 100, MinMR 6, MaxMR 6, MinDC 14, MaxDC 19, Accuracy 15, Agility 13 |

### #47 · Poisonous Mutant Flea

| 字段 | 值 |
|---|---|
| MonsterName | Poisonous Mutant Flea |
| Image | PoisonousMutantFlea |
| AI | 0 |
| Level | 20 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 878 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 160, MinMR 6, MaxMR 6, MinDC 13, MaxDC 26, Accuracy 15, Agility 13 |

### #48 · Blaster Mutant Flea

| 字段 | 值 |
|---|---|
| MonsterName | Blaster Mutant Flea |
| Image | BlasterMutantFlea |
| AI | 0 |
| Level | 20 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 585 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 1400 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 120, MinMR 6, MaxMR 6, MinDC 13, MaxDC 24, Accuracy 15, Agility 13 |

### #49 · Terror Spike

| 字段 | 值 |
|---|---|
| MonsterName | Terror Spike |
| Image | PoisonousMutantFlea |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 25350 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 25 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 1300, MinAC 8, MaxAC 8, MinMR 23, MaxMR 23, MinDC 50, MaxDC 150, Accuracy 25, Agility 23 |

### #50 · Wasp Hatchling

| 字段 | 值 |
|---|---|
| MonsterName | Wasp Hatchling |
| Image | WaspHatchling |
| AI | 0 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1229 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 1700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 10 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 220, MinAC 3, MaxAC 3, MinMR 7, MaxMR 7, MinDC 41, MaxDC 55, Accuracy 19, Agility 18 |

### #51 · Centipede

| 字段 | 值 |
|---|---|
| MonsterName | Centipede |
| Image | Centipede |
| AI | 0 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1190 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2300 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 13 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 11 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 200, MinAC 3, MaxAC 3, MinMR 7, MaxMR 7, MinDC 41, MaxDC 55, Accuracy 19, Agility 18, FireResistance -1, IceResistance 1, LightningResistance 1, PhantomResistance -1 |

### #52 · Butterfly Worm

| 字段 | 值 |
|---|---|
| MonsterName | Butterfly Worm |
| Image | ButterflyWorm |
| AI | 0 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1502 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2100 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 12 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 10 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 260, MinAC 3, MaxAC 3, MinMR 7, MaxMR 7, MinDC 41, MaxDC 56, Accuracy 19, Agility 18, IceResistance 2, LightningResistance -1, PhantomResistance -2 |

### #53 · Mutant Maggot

| 字段 | 值 |
|---|---|
| MonsterName | Mutant Maggot |
| Image | MutantMaggot |
| AI | 0 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1326 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 11 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 10 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 67 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 240, MinAC 3, MaxAC 3, MinMR 7, MaxMR 7, MinDC 28, MaxDC 56, Accuracy 19, Agility 18, IceResistance 1, LightningResistance -2 |

### #54 · Earwig

| 字段 | 值 |
|---|---|
| MonsterName | Earwig |
| Image | Earwig |
| AI | 0 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1287 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1900 |
| MoveDelay | 1600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 13 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 8 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 280, MinAC 3, MaxAC 3, MinMR 7, MaxMR 7, MinDC 41, MaxDC 59, Accuracy 19, Agility 18, FireResistance -1, IceResistance 2, LightningResistance -1, PhantomResistance 1 |

### #55 · Iron Lance

| 字段 | 值 |
|---|---|
| MonsterName | Iron Lance |
| Image | IronLance |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 58500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 70 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2000, MinAC 12, MaxAC 12, MinMR 29, MaxMR 29, MinDC 75, MaxDC 175, Accuracy 25, Agility 23 |

### #56 · Lord Ji'Nae

| 字段 | 值 |
|---|---|
| MonsterName | Lord Ji'Nae |
| Image | LordNiJae |
| AI | 13 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 292500 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 0 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 36 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 16500, MinAC 48, MaxAC 48, MinMR 500, MaxMR 500, MinDC 225, MaxDC 330, MinSC 10, MaxSC 25, Accuracy 40, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 2, PhantomResistance 3, DarkAffinity 1 |

### #57 · Rotting Ghoul

| 字段 | 值 |
|---|---|
| MonsterName | Rotting Ghoul |
| Image | RottingGhoul |
| AI | 0 |
| Level | 25 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1170 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 22 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 230, MinAC 2, MaxAC 2, MinMR 6, MaxMR 6, MinDC 29, MaxDC 44, Accuracy 17, Agility 15 |

### #58 · Decaying Ghoul

| 字段 | 值 |
|---|---|
| MonsterName | Decaying Ghoul |
| Image | DecayingGhoul |
| AI | 14 |
| Level | 25 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 936 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 12 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 22 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 210, MinAC 2, MaxAC 2, MinMR 6, MaxMR 6, MinDC 29, MaxDC 41, MinSC 3, MaxSC 9, Accuracy 17, Agility 15, DarkAffinity 1 |

### #59 · Blood Thristy Ghoul

| 字段 | 值 |
|---|---|
| MonsterName | Blood Thristy Ghoul |
| Image | BloodThirstyGhoul |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 73125 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2100 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 48 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3800, MinAC 20, MaxAC 20, MinMR 36, MaxMR 36, MinDC 75, MaxDC 225, Accuracy 30, Agility 22, IceResistance 5, LightningResistance -1, WindResistance 5, HolyResistance -2, DarkResistance 5, PhantomResistance -1 |

### #60 · Blood Thristy Zombie

| 字段 | 值 |
|---|---|
| MonsterName | Blood Thristy Zombie |
| Image | BloodThirstyGhoul |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 121875 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 500 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 59 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 6000, MinAC 20, MaxAC 20, MinMR 36, MaxMR 36, MinDC 38, MaxDC 288, Accuracy 30, Agility 22, IceResistance 5, LightningResistance -1, WindResistance 5, HolyResistance -2, DarkResistance 5, PhantomResistance -1 |

### #61 · Spined Dark Lizard

| 字段 | 值 |
|---|---|
| MonsterName | Spined Dark Lizard |
| Image | SpinedDarkLizard |
| AI | 7 |
| Level | 25 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 839 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 2500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 8 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 62 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 240, MinMR 7, MaxMR 7, MinDC 15, MaxDC 31, Accuracy 17, Agility 15 |

### #62 · Uma Infidel

| 字段 | 值 |
|---|---|
| MonsterName | Uma Infidel |
| Image | UmaInfidel |
| AI | 0 |
| Level | 25 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 780 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 12 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 63 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 200, MinMR 7, MaxMR 7, MinDC 18, MaxDC 29, Accuracy 17, Agility 15 |

### #63 · Uma Flame Thrower

| 字段 | 值 |
|---|---|
| MonsterName | Uma Flame Thrower |
| Image | UmaFlameThrower |
| AI | 15 |
| Level | 25 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 878 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1700 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 8 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 12 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 62 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 260, MinMR 7, MaxMR 7, MinDC 18, MaxDC 35, Accuracy 17, Agility 15, FireAffinity 1 |

### #64 · Uma Anguisher

| 字段 | 值 |
|---|---|
| MonsterName | Uma Anguisher |
| Image | UmaAnguisher |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 39000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 66 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1700, MinAC 9, MaxAC 9, MinMR 25, MaxMR 25, MinDC 63, MaxDC 163, Accuracy 25, Agility 23 |

### #65 · Uma King

| 字段 | 值 |
|---|---|
| MonsterName | Uma King |
| Image | UmaKing |
| AI | 16 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 195000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 800 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 44 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 13500, MinAC 45, MaxAC 45, MinMR 52, MaxMR 52, MinDC 210, MaxDC 315, Accuracy 40, Agility 48, FireResistance 5, IceResistance 4, LightningResistance 2, WindResistance 3, HolyResistance 2, DarkResistance 3, PhantomResistance 5, LightningAffinity 1 |

### #66 · Spider Bat

| 字段 | 值 |
|---|---|
| MonsterName | Spider Bat |
| Image | SpiderBat |
| AI | 8 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 20 |
| Experience | 1268 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 14 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 14 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 340, MinAC 2, MaxAC 2, MinMR 9, MaxMR 9, MinDC 39, MaxDC 39, Accuracy 21, Agility 18, IceResistance -1, LightningResistance 1, HolyResistance 1, DarkResistance -1, DarkAffinity 1 |

### #67 · Arachnid Gazer

| 字段 | 值 |
|---|---|
| MonsterName | Arachnid Gazer |
| Image | ArachnidGazer |
| AI | 17 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 2340 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 2300 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 11 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 340, MinAC 2, MaxAC 2, MinMR 9, MaxMR 9, Accuracy 21, Agility 18, IceResistance -1, LightningResistance 1, HolyResistance 1, DarkResistance -1 |

### #68 · Larva

| 字段 | 值 |
|---|---|
| MonsterName | Larva |
| Image | Larva |
| AI | 18 |
| Level | 250 |
| ViewRange | 9 |
| CoolEye | 10 |
| Experience | 0 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 500 |
| MoveDelay | 500 |
| IsBoss | false |
| Flag | Larva |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 10 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | Health 5, MinMR 2, MaxMR 2, MinDC 33, MaxDC 48, MinSC 5, MaxSC 10, Accuracy 21, Agility 18, DarkAffinity 1 |

### #69 · Red Moon Guardian

| 字段 | 值 |
|---|---|
| MonsterName | Red Moon Guardian |
| Image | RedMoonGuardian |
| AI | 0 |
| Level | 40 |
| ViewRange | 9 |
| CoolEye | 10 |
| Experience | 1560 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2300 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 13 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 420, MinAC 3, MaxAC 3, MinMR 12, MaxMR 12, MinDC 48, MaxDC 64, Accuracy 22, Agility 18, IceResistance -2, LightningResistance 2, HolyResistance 2, DarkResistance -2 |

### #70 · Red Moon Protector

| 字段 | 值 |
|---|---|
| MonsterName | Red Moon Protector |
| Image | RedMoonProtector |
| AI | 0 |
| Level | 40 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1755 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 1600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 13 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 460, MinMR 13, MaxMR 13, MinDC 48, MaxDC 65, Accuracy 22, Agility 18, IceResistance -2, LightningResistance 2, HolyResistance 2, DarkResistance -2 |

### #71 · Venomous Arachnid

| 字段 | 值 |
|---|---|
| MonsterName | Venomous Arachnid |
| Image | VenomousArachnid |
| AI | 14 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1463 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 350, MinAC 2, MaxAC 2, MinMR 9, MaxMR 9, MinDC 39, MaxDC 60, MinSC 5, MaxSC 10, Accuracy 21, Agility 18, IceResistance -1, LightningResistance 1, HolyResistance 1, DarkResistance -1, DarkAffinity 1 |

### #72 · Dark Arachnid

| 字段 | 值 |
|---|---|
| MonsterName | Dark Arachnid |
| Image | DarkArachnid |
| AI | 0 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1365 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 1900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 13 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 21 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 370, MinAC 2, MaxAC 2, MinMR 9, MaxMR 9, MinDC 39, MaxDC 63, Accuracy 21, Agility 18, IceResistance -1, LightningResistance 1, HolyResistance 1, DarkResistance -1 |

### #73 · Arachnid Broodmother

| 字段 | 值 |
|---|---|
| MonsterName | Arachnid Broodmother |
| Image | DarkArachnid |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 68250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 11 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 22 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 2400, MinAC 18, MaxAC 18, MinMR 36, MaxMR 36, MinDC 88, MaxDC 188, Accuracy 25, Agility 23, LightningResistance 1, HolyResistance 1 |

### #74 · Red Moon Royal Guard

| 字段 | 值 |
|---|---|
| MonsterName | Red Moon Royal Guard |
| Image | RedMoonProtector |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 78000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 11 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 26 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2600, MinAC 24, MaxAC 24, MinMR 42, MaxMR 42, MinDC 100, MaxDC 200, Accuracy 25, Agility 23, FireResistance 1, HolyResistance 1 |

### #75 · Red Moon The Fallen

| 字段 | 值 |
|---|---|
| MonsterName | Red Moon The Fallen |
| Image | RedMoonTheFallen |
| AI | 19 |
| Level | 250 |
| ViewRange | 15 |
| CoolEye | 100 |
| Experience | 487500 |
| Undead | true |
| CanPush | false |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 0 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 28 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 19500, MinAC 51, MaxAC 51, MinMR 58, MaxMR 58, MinDC 240, MaxDC 345, Accuracy 40, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 3, WindResistance 4, HolyResistance 5, DarkResistance 3, PhantomResistance 2, PhantomAffinity 1 |

### #76 · Zuma Sharpshooter

| 字段 | 值 |
|---|---|
| MonsterName | Zuma Sharpshooter |
| Image | ZumaSharpShooter |
| AI | 20 |
| Level | 45 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 8190 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 800 |
| MoveDelay | 2500 |
| IsBoss | false |
| Flag | ZumaArcherMonster |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 14 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 21 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1400, MinAC 2, MaxAC 2, MinMR 22, MaxMR 22, MinDC 33, MaxDC 53, Accuracy 22, Agility 16, FireResistance -3, IceResistance -2, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance -5, PhantomResistance -1 |

### #77 · Zuma Fanatic

| 字段 | 值 |
|---|---|
| MonsterName | Zuma Fanatic |
| Image | ZumaFanatic |
| AI | 21 |
| Level | 45 |
| ViewRange | 9 |
| CoolEye | 40 |
| Experience | 5850 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | ZumaFanaticMonster |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 15 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 11 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1200, MinAC 2, MaxAC 2, MinMR 21, MaxMR 21, MinDC 30, MaxDC 50, Accuracy 22, Agility 16, FireResistance -5, LightningResistance 5, WindResistance 5, HolyResistance 4, DarkResistance -3, PhantomResistance -3 |

### #78 · Zuma Guardian

| 字段 | 值 |
|---|---|
| MonsterName | Zuma Guardian |
| Image | ZumaGuardian |
| AI | 21 |
| Level | 45 |
| ViewRange | 9 |
| CoolEye | 40 |
| Experience | 6825 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | ZumaGuardianMonster |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 15 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 11 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1300, MinAC 3, MaxAC 3, MinMR 21, MaxMR 21, MinDC 30, MaxDC 53, Accuracy 22, Agility 16, FireResistance -5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance -4, PhantomResistance -2 |

### #79 · Vicious Rat

| 字段 | 值 |
|---|---|
| MonsterName | Vicious Rat |
| Image | ViciousRat |
| AI | 0 |
| Level | 45 |
| ViewRange | 9 |
| CoolEye | 30 |
| Experience | 4875 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1900 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1150, MinAC 1, MaxAC 1, MinMR 20, MaxMR 20, MinDC 30, MaxDC 49, Accuracy 22, Agility 16, FireResistance -4, IceResistance -1, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance -5, PhantomResistance -1 |

### #80 · Zuma Keeper

| 字段 | 值 |
|---|---|
| MonsterName | Zuma Keeper |
| Image | ZumaGuardian |
| AI | 21 |
| Level | 250 |
| ViewRange | 9 |
| CoolEye | 100 |
| Experience | 117000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | ZumaKeeperMonster |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 14 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 24 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3300, MinAC 33, MaxAC 33, MinMR 52, MaxMR 52, MinDC 113, MaxDC 213, Accuracy 25, Agility 23, IceResistance 1, LightningResistance 2, WindResistance 2, HolyResistance 2, PhantomResistance 1 |

### #81 · Zuma King

| 字段 | 值 |
|---|---|
| MonsterName | Zuma King |
| Image | ZumaKing |
| AI | 22 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 780000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 21000, MinAC 54, MaxAC 54, MinMR 61, MaxMR 61, MinDC 255, MaxDC 360, Accuracy 40, Agility 48, FireResistance 3, IceResistance 4, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 2, PhantomResistance 3, FireAffinity 1 |

### #82 · Evil Fanatic

| 字段 | 值 |
|---|---|
| MonsterName | Evil Fanatic |
| Image | EvilFanatic |
| AI | 0 |
| Level | 46 |
| ViewRange | 9 |
| CoolEye | 40 |
| Experience | 4485 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 1600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 15 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 900, MinAC 5, MaxAC 5, MinMR 9, MaxMR 9, MinDC 49, MaxDC 74, Accuracy 21, Agility 18, FireResistance 5, IceResistance 4, LightningResistance -1, WindResistance -4, HolyResistance 4, DarkResistance -3 |

### #83 · Monkey

| 字段 | 值 |
|---|---|
| MonsterName | Monkey |
| Image | Monkey |
| AI | 23 |
| Level | 46 |
| ViewRange | 9 |
| CoolEye | 30 |
| Experience | 3510 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 700, MinAC 5, MaxAC 5, MinMR 8, MaxMR 8, MinDC 49, MaxDC 68, MinSC 3, MaxSC 6, Accuracy 22, Agility 18, FireResistance 4, IceResistance 5, LightningResistance -2, WindResistance -5, HolyResistance 5, DarkResistance -4, PhantomResistance -1, DarkAffinity 1 |

### #84 · Evil Monkey

| 字段 | 值 |
|---|---|
| MonsterName | Evil Monkey |
| Image | Monkey |
| AI | 24 |
| Level | 46 |
| ViewRange | 9 |
| CoolEye | 30 |
| Experience | 3120 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 700, MinAC 5, MaxAC 5, MinMR 8, MaxMR 8, MinDC 49, MaxDC 68, MinSC 3, MaxSC 6, Accuracy 22, Agility 18, FireResistance 5, IceResistance 5, LightningResistance -3, WindResistance -4, HolyResistance 5, DarkResistance -4, PhantomResistance -1, DarkAffinity 1 |

### #85 · Evil Elephant

| 字段 | 值 |
|---|---|
| MonsterName | Evil Elephant |
| Image | EvilElephant |
| AI | 25 |
| Level | 47 |
| ViewRange | 9 |
| CoolEye | 40 |
| Experience | 7800 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3200 |
| MoveDelay | 2100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 1100, MinAC 9, MaxAC 9, MinMR 11, MaxMR 11, MinDC 61, MaxDC 74, MinSC 2, MaxSC 8, Accuracy 25, Agility 18, FireResistance 5, IceResistance 5, LightningResistance -1, WindResistance -4, HolyResistance 4, DarkResistance -5, PhantomResistance -2, DarkAffinity 1 |

### #86 · Cannibal Fanatic

| 字段 | 值 |
|---|---|
| MonsterName | Cannibal Fanatic |
| Image | CannibalFanatic |
| AI | 7 |
| Level | 46 |
| ViewRange | 9 |
| CoolEye | 40 |
| Experience | 4875 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 900, MinAC 5, MaxAC 5, MinMR 11, MaxMR 11, MinDC 55, MaxDC 68, Accuracy 22, Agility 18, FireResistance 4, IceResistance 5, LightningResistance -2, WindResistance -5, HolyResistance 5, DarkResistance -4, PhantomResistance -1 |

### #87 · Crazed Warrior

| 字段 | 值 |
|---|---|
| MonsterName | Crazed Warrior |
| Image | EvilFanatic |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 107250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 26 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3300, MinAC 33, MaxAC 33, MinMR 52, MaxMR 52, MinDC 113, MaxDC 213, Accuracy 25, Agility 23, FireResistance 1, IceResistance 2, LightningResistance 1, HolyResistance 2, PhantomResistance 1 |

### #88 · Spiked Beetle

| 字段 | 值 |
|---|---|
| MonsterName | Spiked Beetle |
| Image | SpikedBeetle |
| AI | 0 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 9750 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 59 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 330, MinAC 3, MaxAC 3, MinMR 17, MaxMR 17, MinDC 25, MaxDC 125, Accuracy 25, Agility 23 |

### #89 · Numa Grunt

| 字段 | 值 |
|---|---|
| MonsterName | Numa Grunt |
| Image | NumaGrunt |
| AI | 0 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 35 |
| Experience | 1560 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 13 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 340, MinAC 1, MaxAC 1, MinMR 13, MaxMR 13, MinDC 18, MaxDC 44, Accuracy 21, Agility 16, IceResistance 1, WindResistance -1, HolyResistance 1, DarkResistance -1 |

### #90 · Numa Mage

| 字段 | 值 |
|---|---|
| MonsterName | Numa Mage |
| Image | NumaMage |
| AI | 26 |
| Level | 35 |
| ViewRange | 5 |
| CoolEye | 35 |
| Experience | 2535 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2444 |
| MoveDelay | 3555 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 12 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 27 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 330, MinMR 16, MaxMR 16, MinDC 18, MaxDC 44, Accuracy 21, Agility 16, IceResistance 1, WindResistance -1, HolyResistance 1, DarkResistance -1, LightningAffinity 1 |

### #91 · Numa Elite

| 字段 | 值 |
|---|---|
| MonsterName | Numa Elite |
| Image | NumaElite |
| AI | 0 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 35 |
| Experience | 1755 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 13 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 380, MinAC 2, MaxAC 2, MinMR 14, MaxMR 14, MinDC 18, MaxDC 49, Accuracy 21, Agility 16, IceResistance 1, WindResistance -1, HolyResistance 1, DarkResistance -1 |

### #92 · Sand Shark

| 字段 | 值 |
|---|---|
| MonsterName | Sand Shark |
| Image | SandShark |
| AI | 25 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 35 |
| Experience | 1794 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 310, MinAC 1, MaxAC 1, MinMR 15, MaxMR 15, MinDC 18, MaxDC 39, MinSC 2, MaxSC 6, Accuracy 21, Agility 16, IceResistance 1, WindResistance -1, HolyResistance -1, DarkResistance 1, DarkAffinity 1 |

### #93 · Stone Golem

| 字段 | 值 |
|---|---|
| MonsterName | Stone Golem |
| Image | StoneGolem |
| AI | 27 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 35 |
| Experience | 1658 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 14 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 320, MinAC 2, MaxAC 2, MinMR 14, MaxMR 14, MinDC 18, MaxDC 41, Accuracy 21, Agility 16, IceResistance 1, WindResistance -1, HolyResistance 1, DarkResistance -1, FireAffinity 1 |

### #94 · Windfury Sorceress

| 字段 | 值 |
|---|---|
| MonsterName | Windfury Sorceress |
| Image | WindfurySorceress |
| AI | 28 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 35 |
| Experience | 2048 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 14 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 360, MinAC 2, MaxAC 2, MinMR 14, MaxMR 14, MinDC 18, MaxDC 53, Accuracy 21, Agility 16, IceResistance 1, WindResistance -1, HolyResistance -1, DarkResistance 1, WindAffinity 1 |

### #95 · Cursed Cactus

| 字段 | 值 |
|---|---|
| MonsterName | Cursed Cactus |
| Image | CursedCactus |
| AI | 29 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 35 |
| Experience | 2925 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 2300 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 15 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 340, MinMR 25, MaxMR 25, MinDC 25, MaxDC 63, Accuracy 21, Agility 16, IceResistance 1, WindResistance -1, HolyResistance 1, DarkResistance -1, PhantomResistance 1, PhantomAffinity 1 |

### #96 · Netherworld Gate

| 字段 | 值 |
|---|---|
| MonsterName | Netherworld Gate |
| Image | NetherWorldGate |
| AI | 30 |
| Level | 250 |
| ViewRange | 9 |
| CoolEye | 100 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 0 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |

### #97 · Raging Lizard

| 字段 | 值 |
|---|---|
| MonsterName | Raging Lizard |
| Image | RagingLizard |
| AI | 0 |
| Level | 68 |
| ViewRange | 9 |
| CoolEye | 68 |
| Experience | 31200 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 550 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 15 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 42 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1500, MinAC 20, MaxAC 20, MinMR 32, MaxMR 32, MinDC 63, MaxDC 106, Accuracy 20, Agility 18, FireResistance -1, IceResistance 1, LightningResistance 2, WindResistance 2, HolyResistance -2, DarkResistance 2, PhantomResistance 2 |

### #98 · Numa Elder Shaman

| 字段 | 值 |
|---|---|
| MonsterName | Numa Elder Shaman |
| Image | NumaMage |
| AI | 26 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 106 |
| Experience | 97500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 12 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2400, MinAC 18, MaxAC 18, MinMR 36, MaxMR 36, MinDC 88, MaxDC 188, Accuracy 25, Agility 23, IceResistance 1, HolyResistance 1, LightningAffinity 1 |

### #99 · Saw Tooth Lizard

| 字段 | 值 |
|---|---|
| MonsterName | Saw Tooth Lizard |
| Image | SawToothLizard |
| AI | 0 |
| Level | 68 |
| ViewRange | 10 |
| CoolEye | 60 |
| Experience | 35100 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 14 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 42 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1900, MinAC 21, MaxAC 21, MinMR 32, MaxMR 32, MinDC 81, MaxDC 113, Accuracy 21, Agility 18, FireResistance 1, IceResistance 1, LightningResistance 3, WindResistance 2, HolyResistance -2, DarkResistance 2, PhantomResistance 1 |

### #100 · Venom Spitter

| 字段 | 值 |
|---|---|
| MonsterName | Venom Spitter |
| Image | VenomSpitter |
| AI | 14 |
| Level | 68 |
| ViewRange | 10 |
| CoolEye | 70 |
| Experience | 38025 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 14 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 42 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2000, MinAC 21, MaxAC 21, MinMR 35, MaxMR 35, MinDC 81, MaxDC 131, MinSC 10, MaxSC 25, Accuracy 21, Agility 18, LightningResistance 1, WindResistance 2, HolyResistance -3, DarkResistance 1, PhantomResistance 1, DarkAffinity 1 |

### #101 · Sonic Lizard

| 字段 | 值 |
|---|---|
| MonsterName | Sonic Lizard |
| Image | SonicLizard |
| AI | 31 |
| Level | 68 |
| ViewRange | 10 |
| CoolEye | 70 |
| Experience | 49725 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 14 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 42 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2400, MinAC 22, MaxAC 22, MinMR 34, MaxMR 34, MinDC 88, MaxDC 150, Accuracy 23, Agility 18, FireResistance 1, IceResistance 1, LightningResistance 2, WindResistance 2, HolyResistance -1, DarkResistance 3, PhantomResistance 1, FireAffinity 1 |

### #102 · Giant Lizard

| 字段 | 值 |
|---|---|
| MonsterName | Giant Lizard |
| Image | GiantLizard |
| AI | 33 |
| Level | 68 |
| ViewRange | 10 |
| CoolEye | 70 |
| Experience | 54600 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 11 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 42 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2800, MinAC 23, MaxAC 23, MinMR 35, MaxMR 35, MinDC 94, MaxDC 138, Accuracy 22, Agility 18, FireResistance -1, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance -2, DarkResistance 2, PhantomResistance 2 |

### #103 · Crazed Lizard

| 字段 | 值 |
|---|---|
| MonsterName | Crazed Lizard |
| Image | CrazedLizard |
| AI | 34 |
| Level | 68 |
| ViewRange | 10 |
| CoolEye | 70 |
| Experience | 46800 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 14 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 14 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 42 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2000, MinAC 21, MaxAC 21, MinMR 35, MaxMR 35, MinDC 81, MaxDC 131, Accuracy 21, Agility 18, LightningResistance 1, WindResistance 2, HolyResistance -3, DarkResistance 1, PhantomResistance 1 |

### #104 · Tainted Terror

| 字段 | 值 |
|---|---|
| MonsterName | Tainted Terror |
| Image | TaintedTerror |
| AI | 0 |
| Level | 250 |
| ViewRange | 13 |
| CoolEye | 100 |
| Experience | 585000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 49 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 7700, MinAC 53, MaxAC 53, MinMR 70, MaxMR 70, MinDC 138, MaxDC 238, Accuracy 25, Agility 22, FireResistance 3, IceResistance 4, LightningResistance 4, WindResistance 5, HolyResistance 3, DarkResistance 4, PhantomResistance 4 |

### #105 · Death Lord Jichon

| 字段 | 值 |
|---|---|
| MonsterName | Death Lord Jichon |
| Image | DeathLordJichon |
| AI | 0 |
| Level | 250 |
| ViewRange | 13 |
| CoolEye | 100 |
| Experience | 1755000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 21000, MinAC 50, MaxAC 50, MinMR 62, MaxMR 62, MinDC 240, MaxDC 360, Accuracy 40, Agility 48, FireResistance 1, IceResistance 1, LightningResistance 2, WindResistance 3, HolyResistance 2, DarkResistance 3, PhantomResistance 3 |

### #106 · Mutant Lizard

| 字段 | 值 |
|---|---|
| MonsterName | Mutant Lizard |
| Image | MutantLizard |
| AI | 0 |
| Level | 68 |
| ViewRange | 10 |
| CoolEye | 70 |
| Experience | 41925 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 15 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 11 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 42 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2100, MinAC 22, MaxAC 22, MinMR 33, MaxMR 33, MinDC 69, MaxDC 125, Accuracy 22, Agility 18, FireResistance 1, LightningResistance 2, WindResistance 1, HolyResistance -2, DarkResistance 4, PhantomResistance 1 |

### #107 · Minotaur

| 字段 | 值 |
|---|---|
| MonsterName | Minotaur |
| Image | Minotaur |
| AI | 0 |
| Level | 45 |
| ViewRange | 10 |
| CoolEye | 40 |
| Experience | 4290 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 39 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 49 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1100, MinAC 3, MaxAC 3, MinMR 12, MaxMR 12, MinDC 43, MaxDC 60, Accuracy 22, Agility 18, FireResistance 5, IceResistance -4, LightningResistance 5, WindResistance -1, HolyResistance -2, DarkResistance 5, PhantomResistance 2 |

### #108 · Frost Minotaur

| 字段 | 值 |
|---|---|
| MonsterName | Frost Minotaur |
| Image | FrostMinotaur |
| AI | 35 |
| Level | 45 |
| ViewRange | 10 |
| CoolEye | 40 |
| Experience | 5070 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 40 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 49 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1200, MinAC 4, MaxAC 4, MinMR 12, MaxMR 12, MinDC 43, MaxDC 64, Accuracy 22, Agility 18, FireResistance 5, IceResistance -5, LightningResistance 5, WindResistance -1, HolyResistance -3, DarkResistance 5, PhantomResistance 1, IceAffinity 1 |

### #109 · Banya Right Guard

| 字段 | 值 |
|---|---|
| MonsterName | Banya Right Guard |
| Image | BanyaRightGuard |
| AI | 36 |
| Level | 45 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 8775 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 27 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 53 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1400, MinAC 5, MaxAC 5, MinMR 12, MaxMR 12, MinDC 38, MaxDC 56, Accuracy 22, Agility 18, FireResistance 4, IceResistance -5, LightningResistance 5, WindResistance -2, HolyResistance -2, DarkResistance 5, PhantomResistance 2, LightningAffinity 1 |

### #110 · Shock Minotaur

| 字段 | 值 |
|---|---|
| MonsterName | Shock Minotaur |
| Image | ShockMinotaur |
| AI | 37 |
| Level | 45 |
| ViewRange | 9 |
| CoolEye | 40 |
| Experience | 5070 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 40 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 53 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1200, MinAC 4, MaxAC 4, MinMR 12, MaxMR 12, MinDC 43, MaxDC 64, Accuracy 22, Agility 18, FireResistance 5, IceResistance -4, LightningResistance 4, WindResistance -1, HolyResistance -3, DarkResistance 5, PhantomResistance 1, LightningAffinity 1 |

### #111 · Banya Left Guard

| 字段 | 值 |
|---|---|
| MonsterName | Banya Left Guard |
| Image | BanyaLeftGuard |
| AI | 38 |
| Level | 45 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 9360 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 27 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 49 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1400, MinAC 5, MaxAC 5, MinMR 12, MaxMR 12, MinDC 38, MaxDC 56, Accuracy 22, Agility 18, FireResistance 4, IceResistance -5, LightningResistance 5, WindResistance -2, HolyResistance -2, DarkResistance 5, PhantomResistance 2, FireAffinity 1 |

### #112 · Fury Minotaur

| 字段 | 值 |
|---|---|
| MonsterName | Fury Minotaur |
| Image | FuryMinotaur |
| AI | 39 |
| Level | 45 |
| ViewRange | 9 |
| CoolEye | 60 |
| Experience | 5070 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 40 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 49 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1200, MinAC 4, MaxAC 4, MinMR 12, MaxMR 12, MinDC 43, MaxDC 64, Accuracy 22, Agility 18, FireResistance 5, IceResistance -4, LightningResistance 5, WindResistance -3, HolyResistance -3, DarkResistance 5, PhantomResistance 1, WindAffinity 1 |

### #113 · Flame Minotaur

| 字段 | 值 |
|---|---|
| MonsterName | Flame Minotaur |
| Image | FlameMinotaur |
| AI | 40 |
| Level | 45 |
| ViewRange | 9 |
| CoolEye | 40 |
| Experience | 5070 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 40 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 49 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1200, MinAC 4, MaxAC 4, MinMR 12, MaxMR 12, MinDC 43, MaxDC 64, Accuracy 22, Agility 18, FireResistance 3, IceResistance -4, LightningResistance 5, WindResistance -1, HolyResistance -3, DarkResistance 5, PhantomResistance 1, FireAffinity 1 |

### #114 · Banya Guardian

| 字段 | 值 |
|---|---|
| MonsterName | Banya Guardian |
| Image | Minotaur |
| AI | 0 |
| Level | 250 |
| ViewRange | 22 |
| CoolEye | 100 |
| Experience | 1126750 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 14 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 53 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 23300, MinAC 33, MaxAC 33, MinMR 52, MaxMR 52, MinDC 113, MaxDC 213, Accuracy 25, Agility 23, FireResistance 1, LightningResistance 2, WindResistance 1, DarkResistance 2, PhantomResistance 1 |

### #115 · Emperor Sa'Woo

| 字段 | 值 |
|---|---|
| MonsterName | Emperor Sa'Woo |
| Image | EmperorSaWoo |
| AI | 41 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 585000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 21000, MinAC 54, MaxAC 54, MinMR 61, MaxMR 61, MinDC 255, MaxDC 360, Accuracy 40, Agility 48, FireResistance 5, IceResistance 3, LightningResistance 5, WindResistance 4, HolyResistance 2, DarkResistance 5, PhantomResistance 3, WindAffinity 1 |

### #116 · Bone Archer

| 字段 | 值 |
|---|---|
| MonsterName | Bone Archer |
| Image | BoneArcher |
| AI | 7 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1950 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 2500 |
| IsBoss | false |
| Flag | BoneArcher |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 72 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 2 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 250, MinMR 11, MaxMR 11, MinDC 14, MaxDC 39, Accuracy 19, Agility 16, FireResistance 2 |

### #117 · Bone Bladesman

| 字段 | 值 |
|---|---|
| MonsterName | Bone Bladesman |
| Image | BoneBladesman |
| AI | 0 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1365 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | BoneBladesman |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 72 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 2 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 210, MinMR 11, MaxMR 11, MinDC 15, MaxDC 30, Accuracy 19, Agility 16, FireResistance 1, IceResistance -1, PhantomResistance -1 |

### #118 · Bone Captain

| 字段 | 值 |
|---|---|
| MonsterName | Bone Captain |
| Image | BoneCaptain |
| AI | 0 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 10 |
| Experience | 1716 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | BoneCaptain |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 72 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 2 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 270, MinMR 11, MaxMR 11, MinDC 15, MaxDC 35, Accuracy 19, Agility 16, FireResistance 2, LightningResistance -1, DarkResistance -1 |

### #119 · Bone Soldier

| 字段 | 值 |
|---|---|
| MonsterName | Bone Soldier |
| Image | BoneSoldier |
| AI | 42 |
| Level | 35 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1658 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | BoneSoldier |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 72 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 2 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 230, MinMR 11, MaxMR 11, MinDC 15, MaxDC 30, Accuracy 19, Agility 16, FireResistance 1, IceResistance -1 |

### #120 · Skeleton Enforcer

| 字段 | 值 |
|---|---|
| MonsterName | Skeleton Enforcer |
| Image | BoneCaptain |
| AI | 0 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 52650 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | SkeletonEnforcer |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 76 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 2 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 2000, MinAC 12, MaxAC 12, MinMR 29, MaxMR 29, MinDC 75, MaxDC 175, Accuracy 25, Agility 23 |

### #121 · Arch Lich Taedu

| 字段 | 值 |
|---|---|
| MonsterName | Arch Lich Taedu |
| Image | ArchLichTaedu |
| AI | 43 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 370500 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 15000, MinAC 48, MaxAC 48, MinMR 55, MaxMR 55, MinDC 225, MaxDC 330, Accuracy 40, Agility 48, FireResistance 2, IceResistance 5, LightningResistance 4, WindResistance 3, HolyResistance 5, DarkResistance 5, PhantomResistance 2, FireAffinity 1 |

### #122 · Wedge Moth Larva

| 字段 | 值 |
|---|---|
| MonsterName | Wedge Moth Larva |
| Image | WedgeMothLarva |
| AI | 44 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1365 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 6 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 16 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 240, MinMR 9, MaxMR 9, Accuracy 19, Agility 18, PhantomResistance 1 |

### #123 · Lesser Wedge Moth

| 字段 | 值 |
|---|---|
| MonsterName | Lesser Wedge Moth |
| Image | LesserWedgeMoth |
| AI | 0 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 100 |
| Experience | 0 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | LesserWedgeMoth |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 7 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | Health 3, MinMR 2, MaxMR 2, MaxDC 26, Accuracy 19, Agility 18, PhantomResistance 1 |

### #124 · Wedge Moth

| 字段 | 值 |
|---|---|
| MonsterName | Wedge Moth |
| Image | WedgeMoth |
| AI | 8 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 20 |
| Experience | 1268 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 17 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 240, MinMR 9, MaxMR 9, MinDC 28, MaxDC 28, Accuracy 19, Agility 18, PhantomResistance 1, DarkAffinity 1 |

### #125 · Red Boar

| 字段 | 值 |
|---|---|
| MonsterName | Red Boar |
| Image | RedBoar |
| AI | 0 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 897 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 72 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 200, MinAC 3, MaxAC 3, MinMR 6, MaxMR 6, MinDC 28, MaxDC 41, Accuracy 19, Agility 18 |

### #126 · Claw Serpent

| 字段 | 值 |
|---|---|
| MonsterName | Claw Serpent |
| Image | ClawSerpent |
| AI | 0 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 1365 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 22 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 280, MinMR 9, MaxMR 9, MinDC 28, MaxDC 49, Accuracy 19, Agility 18 |

### #127 · Black Boar

| 字段 | 值 |
|---|---|
| MonsterName | Black Boar |
| Image | BlackBoar |
| AI | 0 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 897 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 72 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 200, MinMR 12, MaxMR 12, MinDC 28, MaxDC 41, Accuracy 19, Agility 18 |

### #128 · Tusk Lord

| 字段 | 值 |
|---|---|
| MonsterName | Tusk Lord |
| Image | TuskLord |
| AI | 0 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 48750 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 2000, MinAC 12, MaxAC 12, MinMR 29, MaxMR 29, MinDC 75, MaxDC 163, Accuracy 25, Agility 23 |

### #129 · Razor Tusk

| 字段 | 值 |
|---|---|
| MonsterName | Razor Tusk |
| Image | RazorTusk |
| AI | 45 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 331500 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| QuestDetails | `QuestTaskMonsterDetails` × 1 条（明细见 [QuestTaskMonsterDetails.md](QuestTaskMonsterDetails.md)） |
| Stats | Health 18000, MinAC 48, MaxAC 48, MinMR 55, MaxMR 55, MinDC 225, MaxDC 330, Accuracy 40, Agility 48, FireResistance 2, IceResistance 5, LightningResistance 4, WindResistance 5, HolyResistance 4, DarkResistance 4, PhantomResistance 2, HolyAffinity 1 |

### #130 · Pink Goddess Of Black Palace

| 字段 | 值 |
|---|---|
| MonsterName | Pink Goddess Of Black Palace |
| Image | PinkGoddess |
| AI | 46 |
| Level | 50 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 9750 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1800, MinAC 5, MaxAC 5, MinMR 18, MaxMR 18, MinDC 31, MaxDC 50, Accuracy 22, Agility 16, FireResistance -3, IceResistance 4, LightningResistance 4, WindResistance -3, HolyResistance -4, DarkResistance 5, PhantomResistance -2, DarkAffinity 1 |

### #131 · Green Goddess Of Black Palace

| 字段 | 值 |
|---|---|
| MonsterName | Green Goddess Of Black Palace |
| Image | GreenGoddess |
| AI | 47 |
| Level | 50 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 9750 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1800, MinAC 5, MaxAC 5, MinMR 18, MaxMR 18, MinDC 31, MaxDC 50, MinSC 3, MaxSC 8, Accuracy 22, Agility 16, FireResistance -3, IceResistance 4, LightningResistance 4, WindResistance -3, HolyResistance -4, DarkResistance 5, PhantomResistance -2, DarkAffinity 1 |

### #132 · Mutant Captain

| 字段 | 值 |
|---|---|
| MonsterName | Mutant Captain |
| Image | MutantCaptain |
| AI | 48 |
| Level | 50 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 15600 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2300, MinAC 7, MaxAC 7, MinMR 23, MaxMR 23, MinDC 38, MaxDC 63, Accuracy 22, Agility 16, FireResistance -2, IceResistance 3, LightningResistance 3, WindResistance -2, HolyResistance -5, DarkResistance 5, PhantomResistance -1, FireAffinity 1 |

### #133 · Stone Griffin

| 字段 | 值 |
|---|---|
| MonsterName | Stone Griffin |
| Image | StoneGriffin |
| AI | 49 |
| Level | 52 |
| ViewRange | 13 |
| CoolEye | 60 |
| Experience | 13650 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 2500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 24 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 93 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1500, MinAC 7, MaxAC 7, MinMR 20, MaxMR 20, MinDC 44, MaxDC 56, Accuracy 22, Agility 16, FireResistance -4, IceResistance 5, LightningResistance 5, WindResistance -4, HolyResistance -3, DarkResistance 5, PhantomResistance -3, DarkAffinity 1 |

### #134 · Flame Griffin

| 字段 | 值 |
|---|---|
| MonsterName | Flame Griffin |
| Image | FlameGriffin |
| AI | 50 |
| Level | 52 |
| ViewRange | 13 |
| CoolEye | 60 |
| Experience | 11700 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 2500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 22 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 93 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1700, MinAC 7, MaxAC 7, MinMR 20, MaxMR 20, MinDC 44, MaxDC 63, Accuracy 22, Agility 16, FireResistance -4, IceResistance 5, LightningResistance 5, WindResistance -4, HolyResistance -3, DarkResistance 5, PhantomResistance -3, FireAffinity 1 |

### #135 · Black Palace Warlord

| 字段 | 值 |
|---|---|
| MonsterName | Black Palace Warlord |
| Image | MutantCaptain |
| AI | 48 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 156000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4000, MinAC 35, MaxAC 35, MinMR 57, MaxMR 57, MinDC 138, MaxDC 275, Accuracy 25, Agility 23, FireResistance 2, IceResistance 3, LightningResistance 2, WindResistance 3, HolyResistance 1, DarkResistance 3, PhantomResistance 2, FireAffinity 1 |

### #136 · Pink Goddess Of Underground

| 字段 | 值 |
|---|---|
| MonsterName | Pink Goddess Of Underground |
| Image | PinkGoddess |
| AI | 46 |
| Level | 55 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 15600 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 16 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 65 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2300, MinAC 14, MaxAC 14, MinMR 25, MaxMR 25, MinDC 56, MaxDC 81, Accuracy 22, Agility 18, FireResistance -3, IceResistance 4, LightningResistance 4, WindResistance -3, HolyResistance -4, DarkResistance 5, PhantomResistance -2, DarkAffinity 1 |

### #137 · Vicious Mutant Captain

| 字段 | 值 |
|---|---|
| MonsterName | Vicious Mutant Captain |
| Image | MutantCaptain |
| AI | 48 |
| Level | 55 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 23400 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 11 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 65 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2500, MinAC 16, MaxAC 16, MinMR 27, MaxMR 27, MinDC 69, MaxDC 94, Accuracy 22, Agility 18, FireResistance -2, IceResistance 3, LightningResistance 3, WindResistance -2, HolyResistance -5, DarkResistance 5, PhantomResistance -1, FireAffinity 1 |

### #138 · Green Goddess Of Underground

| 字段 | 值 |
|---|---|
| MonsterName | Green Goddess Of Underground |
| Image | GreenGoddess |
| AI | 47 |
| Level | 55 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 15600 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 16 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 65 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2300, MinAC 14, MaxAC 14, MinMR 25, MaxMR 25, MinDC 56, MaxDC 81, MinSC 5, MaxSC 13, Accuracy 22, Agility 18, FireResistance -3, IceResistance 4, LightningResistance 4, WindResistance -3, HolyResistance -4, DarkResistance 5, PhantomResistance -2, DarkAffinity 1 |

### #139 · Jinchon Warlord

| 字段 | 值 |
|---|---|
| MonsterName | Jinchon Warlord |
| Image | MutantCaptain |
| AI | 48 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 214500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 68 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4400, MinAC 39, MaxAC 39, MinMR 62, MaxMR 62, MinDC 150, MaxDC 250, Accuracy 25, Agility 23, FireResistance 2, IceResistance 3, LightningResistance 2, WindResistance 3, HolyResistance 1, DarkResistance 3, PhantomResistance 2, FireAffinity 1 |

### #140 · SummonPuppet

| 字段 | 值 |
|---|---|
| MonsterName | SummonPuppet |
| Image | None |
| AI | 51 |
| Level | 250 |
| ViewRange | 0 |
| CoolEye | 100 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | SummonPuppet |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 3 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1, MinMR 2, MaxMR 2 |

### #141 · Apparition Archer

| 字段 | 值 |
|---|---|
| MonsterName | Apparition Archer |
| Image | BoneArcher |
| AI | 7 |
| Level | 75 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 1950 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 6 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3300, MinAC 33, MaxAC 33, MinMR 52, MaxMR 52, MinDC 75, MaxDC 113, Accuracy 25, Agility 23 |

### #142 · Apparition Bladesman

| 字段 | 值 |
|---|---|
| MonsterName | Apparition Bladesman |
| Image | BoneBladesman |
| AI | 0 |
| Level | 75 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 1950 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 6 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3300, MinAC 33, MaxAC 33, MinMR 52, MaxMR 52, MinDC 75, MaxDC 113, Accuracy 25, Agility 23 |

### #143 · Apparition Soldier

| 字段 | 值 |
|---|---|
| MonsterName | Apparition Soldier |
| Image | BoneSoldier |
| AI | 42 |
| Level | 75 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 1950 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 9 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 6 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3300, MinAC 33, MaxAC 33, MinMR 52, MaxMR 52, MinDC 75, MaxDC 113, Accuracy 25, Agility 23 |

### #144 · Skeleton

| 字段 | 值 |
|---|---|
| MonsterName | Skeleton |
| Image | WhiteBone |
| AI | 52 |
| Level | 17 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2300 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | Skeleton |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | Health 250, MinAC 7, MaxAC 7, MinMR 12, MaxMR 12, MinDC 8, MaxDC 21, Accuracy 14, Agility 12, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance 2, DarkResistance 2, PhantomResistance 2, PhantomAffinity 1 |

### #145 · Jin Skeleton

| 字段 | 值 |
|---|---|
| MonsterName | Jin Skeleton |
| Image | WhiteBone |
| AI | 52 |
| Level | 33 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | JinSkeleton |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 350, MinAC 13, MaxAC 13, MinMR 22, MaxMR 22, MinDC 25, MaxDC 48, Accuracy 20, Agility 18, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance 2, DarkResistance 2, PhantomResistance 2, PhantomAffinity 1 |

### #146 · Shinsu

| 字段 | 值 |
|---|---|
| MonsterName | Shinsu |
| Image | Shinsu |
| AI | 53 |
| Level | 30 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | Shinsu |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 500, MinAC 13, MaxAC 13, MinMR 22, MaxMR 22, MinDC 10, MaxDC 26, Accuracy 19, Agility 18, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance 2, DarkResistance 2, PhantomResistance 2, PhantomAffinity 1 |

### #147 · Infernal Soldier

| 字段 | 值 |
|---|---|
| MonsterName | Infernal Soldier |
| Image | InfernalSoldier |
| AI | 103 |
| Level | 50 |
| ViewRange | 9 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | InfernalSoldier |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1900, MinAC 80, MaxAC 90, MinMR 70, MaxMR 79, MinDC 50, MaxDC 75, Accuracy 30, Agility 50, FireResistance 7, IceResistance 7, LightningResistance 7, WindResistance 7, HolyResistance 7, DarkResistance 7, PhantomResistance 7, LifeSteal 13, PhantomAffinity 1, PhysicalResistance 4 |

### #150 · MirrorImage

| 字段 | 值 |
|---|---|
| MonsterName | MirrorImage |
| Image | None |
| AI | 55 |
| Level | 250 |
| ViewRange | 0 |
| CoolEye | 100 |
| Experience | 0 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | MirrorImage |
| FaceImage | 0 |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |

### #151 · Corpse Stalker

| 字段 | 值 |
|---|---|
| MonsterName | Corpse Stalker |
| Image | CorpseStalker |
| AI | 0 |
| Level | 75 |
| ViewRange | 8 |
| CoolEye | 60 |
| Experience | 41438 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 10 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 40 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1800, MinAC 27, MaxAC 27, MinMR 20, MaxMR 20, MinDC 90, MaxDC 125, Accuracy 22, Agility 18, FireResistance -2, IceResistance 4, LightningResistance -5, WindResistance 5, HolyResistance -3, DarkResistance 5, PhantomResistance 5 |

### #152 · Light Armed Soldier

| 字段 | 值 |
|---|---|
| MonsterName | Light Armed Soldier |
| Image | LightArmedSoldier |
| AI | 0 |
| Level | 75 |
| ViewRange | 9 |
| CoolEye | 60 |
| Experience | 42413 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1400 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 16 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 40 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1650, MinAC 27, MaxAC 27, MinMR 20, MaxMR 20, MinDC 70, MaxDC 138, Accuracy 22, Agility 18, FireResistance -1, IceResistance 5, LightningResistance -4, WindResistance 5, HolyResistance -3, DarkResistance 5, PhantomResistance 5 |

### #153 · Corrosive Poison Spitter

| 字段 | 值 |
|---|---|
| MonsterName | Corrosive Poison Spitter |
| Image | CorrosivePoisonSpitter |
| AI | 56 |
| Level | 75 |
| ViewRange | 12 |
| CoolEye | 60 |
| Experience | 56063 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 40 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2100, MinAC 30, MaxAC 30, MinMR 18, MaxMR 18, MinDC 81, MaxDC 138, MinSC 10, MaxSC 20, Accuracy 22, Agility 18, FireResistance -3, IceResistance 5, LightningResistance -4, WindResistance 5, HolyResistance -3, DarkResistance 5, PhantomResistance 5, DarkAffinity 1 |

### #154 · Phantom Soldier

| 字段 | 值 |
|---|---|
| MonsterName | Phantom Soldier |
| Image | PhantomSoldier |
| AI | 0 |
| Level | 75 |
| ViewRange | 9 |
| CoolEye | 60 |
| Experience | 73125 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 40 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2500, MinAC 35, MaxAC 35, MinMR 26, MaxMR 26, MinDC 125, MaxDC 200, Accuracy 22, Agility 18, FireResistance -2, IceResistance 4, LightningResistance -5, WindResistance 5, HolyResistance -3, DarkResistance 5, PhantomResistance 5, PhantomAffinity 1 |

### #155 · Mutated Octopus

| 字段 | 值 |
|---|---|
| MonsterName | Mutated Octopus |
| Image | MutatedOctopus |
| AI | 57 |
| Level | 75 |
| ViewRange | 12 |
| CoolEye | 60 |
| Experience | 47532 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1400 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 10 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 44 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2000, MinAC 28, MaxAC 28, MinMR 18, MaxMR 18, MinDC 75, MaxDC 106, Accuracy 22, Agility 18, FireResistance -1, IceResistance 3, LightningResistance -4, WindResistance 5, HolyResistance -3, DarkResistance 5, PhantomResistance 5, LightningAffinity 1 |

### #156 · Aqua Lizard

| 字段 | 值 |
|---|---|
| MonsterName | Aqua Lizard |
| Image | AquaLizard |
| AI | 0 |
| Level | 75 |
| ViewRange | 9 |
| CoolEye | 60 |
| Experience | 41925 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1800 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 10 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 40 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1900, MinAC 30, MaxAC 30, MinMR 22, MaxMR 22, MinDC 50, MaxDC 194, Accuracy 22, Agility 18, FireResistance -3, IceResistance 5, LightningResistance -4, WindResistance 5, HolyResistance -3, DarkResistance 5, PhantomResistance 5, IceAffinity 1 |

### #157 · Stomper

| 字段 | 值 |
|---|---|
| MonsterName | Stomper |
| Image | Stomper |
| AI | 58 |
| Level | 75 |
| ViewRange | 7 |
| CoolEye | 60 |
| Experience | 46800 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 40 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1900, MinAC 28, MaxAC 28, MinMR 23, MaxMR 23, MinDC 81, MaxDC 119, Accuracy 22, Agility 18, FireResistance -2, IceResistance 4, LightningResistance -5, WindResistance 5, HolyResistance -3, DarkResistance 5, PhantomResistance 5, HolyAffinity 1 |

### #158 · Crimson Necromancer

| 字段 | 值 |
|---|---|
| MonsterName | Crimson Necromancer |
| Image | CrimsonNecromancer |
| AI | 59 |
| Level | 75 |
| ViewRange | 12 |
| CoolEye | 70 |
| Experience | 68250 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 40 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2300, MinAC 22, MaxAC 22, MinMR 20, MaxMR 20, MinDC 88, MaxDC 138, Accuracy 22, Agility 18, FireResistance -1, IceResistance 5, LightningResistance -4, WindResistance 5, HolyResistance -2, DarkResistance 5, PhantomResistance 5, PhantomAffinity 1 |

### #159 · Chaos Knight

| 字段 | 值 |
|---|---|
| MonsterName | Chaos Knight |
| Image | ChaosKnight |
| AI | 60 |
| Level | 250 |
| ViewRange | 13 |
| CoolEye | 100 |
| Experience | 292500 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 59 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 6600, MinAC 60, MaxAC 60, MinMR 67, MaxMR 67, MinDC 138, MaxDC 238, Accuracy 25, Agility 23, FireResistance 4, IceResistance 4, LightningResistance 3, WindResistance 4, HolyResistance 3, DarkResistance 4, PhantomResistance 4, PhantomAffinity 1 |

### #160 · Pachon The Chaos bringer

| 字段 | 值 |
|---|---|
| MonsterName | Pachon The Chaos bringer |
| Image | PachonTheChaosBringer |
| AI | 61 |
| Level | 250 |
| ViewRange | 15 |
| CoolEye | 100 |
| Experience | 1365000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 53 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 24000, MinAC 80, MaxAC 80, MinMR 80, MaxMR 80, MinDC 285, MaxDC 390, Accuracy 80, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 4, WindResistance 5, HolyResistance 4, DarkResistance 5, PhantomResistance 5, PhantomAffinity 1 |

### #161 · Numa Cavalry

| 字段 | 值 |
|---|---|
| MonsterName | Numa Cavalry |
| Image | NumaCavalry |
| AI | 33 |
| Level | 72 |
| ViewRange | 10 |
| CoolEye | 60 |
| Experience | 37050 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1400 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 16 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 46 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1700, MinAC 8, MaxAC 8, MinMR 27, MaxMR 27, MinDC 69, MaxDC 113, Accuracy 22, Agility 18, FireResistance 3, IceResistance 3, LightningResistance 1, WindResistance -4, HolyResistance -3, DarkResistance 5, PhantomResistance -3 |

### #162 · Numa High Mage

| 字段 | 值 |
|---|---|
| MonsterName | Numa High Mage |
| Image | NumaHighMage |
| AI | 62 |
| Level | 72 |
| ViewRange | 10 |
| CoolEye | 70 |
| Experience | 35100 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1400 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 16 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 50 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2000, MinAC 10, MaxAC 10, MinMR 31, MaxMR 31, MinDC 69, MaxDC 68, Accuracy 22, Agility 18, FireResistance 1, IceResistance 3, LightningResistance 3, WindResistance -2, HolyResistance -5, DarkResistance 5, PhantomResistance -1, LightningAffinity 1 |

### #163 · Numa Stone Thrower

| 字段 | 值 |
|---|---|
| MonsterName | Numa Stone Thrower |
| Image | NumaStoneThrower |
| AI | 63 |
| Level | 72 |
| ViewRange | 10 |
| CoolEye | 70 |
| Experience | 58500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 16 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 46 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1500, MinAC 9, MaxAC 9, MinMR 28, MaxMR 28, MinDC 50, MaxDC 68, Accuracy 22, Agility 18, FireResistance 4, IceResistance 2, WindResistance -3, HolyResistance -2, DarkResistance 5, PhantomResistance -4, HolyAffinity 1 |

### #164 · Numa Royal Guard

| 字段 | 值 |
|---|---|
| MonsterName | Numa Royal Guard |
| Image | NumaRoyalGuard |
| AI | 48 |
| Level | 72 |
| ViewRange | 10 |
| CoolEye | 60 |
| Experience | 39000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 16 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 46 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2400, MinAC 12, MaxAC 12, MinMR 32, MaxMR 32, MinDC 75, MaxDC 113, Accuracy 22, Agility 18, FireResistance 3, IceResistance 2, LightningResistance 1, WindResistance -2, HolyResistance -4, DarkResistance 5, PhantomResistance -3, FireAffinity 1 |

### #165 · Numa Armored Soldier

| 字段 | 值 |
|---|---|
| MonsterName | Numa Armored Soldier |
| Image | NumaArmoredSoldier |
| AI | 64 |
| Level | 72 |
| ViewRange | 10 |
| CoolEye | 60 |
| Experience | 33930 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 16 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 46 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2000, MinAC 12, MaxAC 12, MinMR 27, MaxMR 27, MinDC 75, MaxDC 100, Accuracy 22, Agility 18, FireResistance 2, IceResistance 3, LightningResistance 1, WindResistance -2, HolyResistance -4, DarkResistance 5, PhantomResistance -3 |

### #166 · Numa Assault Captain

| 字段 | 值 |
|---|---|
| MonsterName | Numa Assault Captain |
| Image | NumaRoyalGuard |
| AI | 48 |
| Level | 72 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 370500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 61 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 7700, MinAC 53, MaxAC 53, MinMR 70, MaxMR 70, MinDC 138, MaxDC 238, Accuracy 25, Agility 23, FireResistance 4, IceResistance 4, LightningResistance 4, WindResistance 3, HolyResistance 3, DarkResistance 4, PhantomResistance 4, FireAffinity 1 |

### #167 · Icy Ranger

| 字段 | 值 |
|---|---|
| MonsterName | Icy Ranger |
| Image | IcyRanger |
| AI | 34 |
| Level | 80 |
| ViewRange | 13 |
| CoolEye | 70 |
| Experience | 107250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 12 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 29 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2800, MinAC 22, MaxAC 22, MinMR 49, MaxMR 49, MinDC 88, MaxDC 183, Accuracy 24, Agility 20, FireResistance 3, IceResistance 5, LightningResistance -1, WindResistance 4, HolyResistance -2, DarkResistance 1, PhantomResistance 2, IceAffinity 1 |

### #168 · Icy Goddess

| 字段 | 值 |
|---|---|
| MonsterName | Icy Goddess |
| Image | IcyGoddess |
| AI | 65 |
| Level | 80 |
| ViewRange | 10 |
| CoolEye | 70 |
| Experience | 78000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1400 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 9 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2600, MinAC 20, MaxAC 20, MinMR 47, MaxMR 47, MinDC 110, MaxDC 200, Accuracy 22, Agility 19, FireResistance 2, IceResistance 3, LightningResistance 1, WindResistance 4, HolyResistance -1, DarkResistance 3, IceAffinity 1 |

### #169 · Icy Spirit Warrior

| 字段 | 值 |
|---|---|
| MonsterName | Icy Spirit Warrior |
| Image | IcySpiritWarrior |
| AI | 67 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 80 |
| Experience | 87750 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 12 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2900, MinAC 20, MaxAC 20, MinMR 50, MaxMR 50, MinDC 85, MaxDC 162, Accuracy 21, Agility 18, FireResistance 2, IceResistance 3, LightningResistance 1, WindResistance 4, HolyResistance 1, DarkAffinity 1 |

### #170 · Icy Spirit General

| 字段 | 值 |
|---|---|
| MonsterName | Icy Spirit General |
| Image | IcySpiritGeneral |
| AI | 66 |
| Level | 100 |
| ViewRange | 11 |
| CoolEye | 70 |
| Experience | 97500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1550 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 11 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 29 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2950, MinAC 20, MaxAC 20, MinMR 50, MaxMR 50, MinDC 110, MaxDC 169, Accuracy 21, Agility 18, FireResistance 3, IceResistance 4, WindResistance 3, HolyResistance 2, DarkResistance -1, DarkAffinity 1 |

### #171 · Ghost Knight

| 字段 | 值 |
|---|---|
| MonsterName | Ghost Knight |
| Image | GhostKnight |
| AI | 25 |
| Level | 75 |
| ViewRange | 10 |
| CoolEye | 70 |
| Experience | 81900 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2800, MinAC 20, MaxAC 20, MinMR 46, MaxMR 46, MinDC 106, MaxDC 199, MinSC 15, MaxSC 25, Accuracy 23, Agility 19, FireResistance 3, IceResistance 5, LightningResistance -1, WindResistance 2, HolyResistance -2, DarkResistance 2, DarkAffinity 1 |

### #172 · Icy Spirit Spearman

| 字段 | 值 |
|---|---|
| MonsterName | Icy Spirit Spearman |
| Image | IcySpiritSpearman |
| AI | 0 |
| Level | 80 |
| ViewRange | 12 |
| CoolEye | 80 |
| Experience | 62400 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 14 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 16 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2400, MinAC 20, MaxAC 20, MinMR 46, MaxMR 46, MinDC 95, MaxDC 170, Accuracy 23, Agility 20, FireResistance 3, IceResistance 3, WindResistance 3, DarkResistance 2, PhantomResistance 1 |

### #173 · Werewolf

| 字段 | 值 |
|---|---|
| MonsterName | Werewolf |
| Image | Werewolf |
| AI | 68 |
| Level | 75 |
| ViewRange | 12 |
| CoolEye | 60 |
| Experience | 72150 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2400, MinAC 18, MaxAC 18, MinMR 46, MaxMR 46, MinDC 113, MaxDC 194, Accuracy 22, Agility 18, FireResistance 2, IceResistance 4, LightningResistance -1, WindResistance 3, DarkResistance 2, PhantomResistance 1, IceAffinity 1 |

### #174 · Whitefang

| 字段 | 值 |
|---|---|
| MonsterName | Whitefang |
| Image | Whitefang |
| AI | 23 |
| Level | 75 |
| ViewRange | 14 |
| CoolEye | 60 |
| Experience | 66690 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2500, MinAC 18, MaxAC 18, MinMR 44, MaxMR 44, MinDC 94, MaxDC 213, MinSC 10, MaxSC 25, Accuracy 23, Agility 18, FireResistance 2, IceResistance 3, WindResistance 2, HolyResistance 2, DarkResistance -1, PhantomResistance 2, DarkAffinity 1 |

### #175 · Icy Spirit Solider

| 字段 | 值 |
|---|---|
| MonsterName | Icy Spirit Solider |
| Image | IcySpiritSolider |
| AI | 24 |
| Level | 80 |
| ViewRange | 12 |
| CoolEye | 80 |
| Experience | 74100 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 12 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2500, MinAC 22, MaxAC 22, MinMR 49, MaxMR 49, MinDC 138, MaxDC 194, Accuracy 24, Agility 19, FireResistance 3, IceResistance 3, WindResistance 3, HolyResistance -1, DarkResistance 2, PhantomResistance 2, IceAffinity 1 |

### #176 · Wild Boar

| 字段 | 值 |
|---|---|
| MonsterName | Wild Boar |
| Image | WildBoar |
| AI | 0 |
| Level | 75 |
| ViewRange | 11 |
| CoolEye | 60 |
| Experience | 64350 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 20 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2900, MinAC 18, MaxAC 18, MinMR 44, MaxMR 44, MinDC 94, MaxDC 213, Accuracy 22, Agility 18, FireResistance 2, IceResistance 3, WindResistance 2, HolyResistance 2, DarkResistance -1, PhantomResistance 2, IceAffinity 1 |

### #177 · Jinam Stone Gate

| 字段 | 值 |
|---|---|
| MonsterName | Jinam Stone Gate |
| Image | JinamStoneGate |
| AI | 69 |
| Level | 250 |
| ViewRange | 2 |
| CoolEye | 100 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 0 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |

### #178 · Frost Lord Hwa

| 字段 | 值 |
|---|---|
| MonsterName | Frost Lord Hwa |
| Image | FrostLordHwa |
| AI | 70 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 1852500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 600 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 207000, MinAC 120, MaxAC 120, MinMR 142, MaxMR 142, MinDC 450, MaxDC 555, Accuracy 40, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 4, DarkResistance 4, PhantomResistance 4, LifeSteal 300, CriticalChance 10 |

### #179 · Bloody Armed Beetle

| 字段 | 值 |
|---|---|
| MonsterName | Bloody Armed Beetle |
| Image | SpikedBeetle |
| AI | 0 |
| Level | 80 |
| ViewRange | 13 |
| CoolEye | 100 |
| Experience | 140400 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 10 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 5000, MinAC 50, MaxAC 50, MinMR 50, MaxMR 50, MinDC 70, MaxDC 170, Accuracy 30, Agility 23 |

### #180 · Golden Armored Beetle

| 字段 | 值 |
|---|---|
| MonsterName | Golden Armored Beetle |
| Image | Beetle |
| AI | 0 |
| Level | 80 |
| ViewRange | 13 |
| CoolEye | 100 |
| Experience | 19500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | GoldenArmouredBeetle |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 19 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1300, MinAC 80, MaxAC 80, MinMR 30, MaxMR 30, MinDC 100, MaxDC 188, Accuracy 30, Agility 23, FireResistance 5, IceResistance 5, LightningResistance 4, WindResistance 5, HolyResistance 5, DarkResistance 3, PhantomResistance 4 |

### #181 · Earwig King

| 字段 | 值 |
|---|---|
| MonsterName | Earwig King |
| Image | IronLance |
| AI | 0 |
| Level | 80 |
| ViewRange | 13 |
| CoolEye | 100 |
| Experience | 140400 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 10 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 5000, MinAC 50, MaxAC 50, MinMR 50, MaxMR 50, MinDC 70, MaxDC 170, Accuracy 30, Agility 23, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance 2, DarkResistance 2, PhantomResistance 2 |

### #182 · Mature Earwig

| 字段 | 值 |
|---|---|
| MonsterName | Mature Earwig |
| Image | Earwig |
| AI | 0 |
| Level | 80 |
| ViewRange | 13 |
| CoolEye | 100 |
| Experience | 29250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | MatureEarwig |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 19 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1650, MinAC 90, MaxAC 90, MinMR 35, MaxMR 35, MinDC 100, MaxDC 200, Accuracy 30, Agility 23, FireResistance 5, IceResistance 5, LightningResistance 4, WindResistance 5, HolyResistance 5, DarkResistance 3, PhantomResistance 4 |

### #183 · Millipede

| 字段 | 值 |
|---|---|
| MonsterName | Millipede |
| Image | Centipede |
| AI | 0 |
| Level | 80 |
| ViewRange | 13 |
| CoolEye | 100 |
| Experience | 39000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3500 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | Millipede |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 19 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1400, MinAC 80, MaxAC 80, MinMR 40, MaxMR 40, MinDC 150, MaxDC 325, Accuracy 30, Agility 23, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 4, HolyResistance 5, DarkResistance 3, PhantomResistance 5 |

### #184 · Enraged Lord Ji'Nae

| 字段 | 值 |
|---|---|
| MonsterName | Enraged Lord Ji'Nae |
| Image | LordNiJae |
| AI | 77 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 2925000 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 0 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 41 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 54000, MinAC 60, MaxAC 60, MinMR 500, MaxMR 500, MinDC 225, MaxDC 345, MinSC 30, MaxSC 60, Accuracy 40, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5, DarkAffinity 1 |

### #185 · Banyo Soldier

| 字段 | 值 |
|---|---|
| MonsterName | Banyo Soldier |
| Image | RottingGhoul |
| AI | 0 |
| Level | 80 |
| ViewRange | 16 |
| CoolEye | 100 |
| Experience | 120900 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 750 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 29 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3500, MinAC 25, MaxAC 25, MinMR 50, MaxMR 50, MinDC 120, MaxDC 140, Accuracy 30, Agility 24, IceResistance -2, LightningResistance 4, WindResistance 5, HolyResistance 3, DarkResistance 5, PhantomResistance 5, HolyAffinity 1 |

### #186 · Banyo Warrior

| 字段 | 值 |
|---|---|
| MonsterName | Banyo Warrior |
| Image | LightArmedSoldier |
| AI | 117 |
| Level | 80 |
| ViewRange | 16 |
| CoolEye | 100 |
| Experience | 146250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 29 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 5650, MinAC 30, MaxAC 30, MinMR 60, MaxMR 60, MinDC 150, MaxDC 165, Accuracy 30, Agility 24, IceResistance -2, LightningResistance 4, WindResistance 5, HolyResistance 3, DarkResistance 5, PhantomResistance 5 |

### #187 · Banyo Captain

| 字段 | 值 |
|---|---|
| MonsterName | Banyo Captain |
| Image | PhantomSoldier |
| AI | 72 |
| Level | 80 |
| ViewRange | 16 |
| CoolEye | 100 |
| Experience | 136000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 800 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | BanyoCaptain |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 34 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 5225, MinAC 35, MaxAC 35, MinMR 70, MaxMR 70, MinDC 190, MaxDC 230, Accuracy 30, Agility 24, IceResistance -2, LightningResistance 3, WindResistance 5, HolyResistance 4, DarkResistance 5, PhantomResistance 5, LightningAffinity 1 |

### #188 · Banyo Lord Guzak

| 字段 | 值 |
|---|---|
| MonsterName | Banyo Lord Guzak |
| Image | PachonTheChaosBringer |
| AI | 74 |
| Level | 250 |
| ViewRange | 16 |
| CoolEye | 100 |
| Experience | 3900000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 800 |
| MoveDelay | 60000 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 44 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 100000, MinAC 228, MaxAC 228, MinMR 300, MaxMR 300, MinDC 650, MaxDC 655, Accuracy 50, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 4, HolyResistance 5, DarkResistance 5, PhantomResistance 5, LightningAffinity 1 |

### #189 · Pig

| 字段 | 值 |
|---|---|
| MonsterName | Pig |
| Image | Companion_Pig |
| AI | -2 |
| Level | 0 |
| ViewRange | 6 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 660 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 2, CompanionBagWeight 50 |

### #190 · Tusk Lord

| 字段 | 值 |
|---|---|
| MonsterName | Tusk Lord |
| Image | Companion_TuskLord |
| AI | -2 |
| Level | 0 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 3, CompanionBagWeight 70 |

### #191 · Skeleton Lord

| 字段 | 值 |
|---|---|
| MonsterName | Skeleton Lord |
| Image | Companion_SkeletonLord |
| AI | -2 |
| Level | 0 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 3, CompanionBagWeight 70 |

### #192 · Griffin

| 字段 | 值 |
|---|---|
| MonsterName | Griffin |
| Image | Companion_Griffin |
| AI | -2 |
| Level | 0 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 3, CompanionBagWeight 70 |

### #193 · Dragon

| 字段 | 值 |
|---|---|
| MonsterName | Dragon |
| Image | Companion_Dragon |
| AI | -2 |
| Level | 0 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 3, CompanionBagWeight 70 |

### #194 · Donkey

| 字段 | 值 |
|---|---|
| MonsterName | Donkey |
| Image | Companion_Donkey |
| AI | -2 |
| Level | 0 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 3, CompanionBagWeight 70 |

### #195 · Sheep

| 字段 | 值 |
|---|---|
| MonsterName | Sheep |
| Image | Companion_Sheep |
| AI | -2 |
| Level | 0 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 3, CompanionBagWeight 70 |

### #196 · Pachon 

| 字段 | 值 |
|---|---|
| MonsterName | Pachon  |
| Image | Companion_BanyoLordGuzak |
| AI | -2 |
| Level | 0 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 3, CompanionBagWeight 70 |

### #197 · Panda

| 字段 | 值 |
|---|---|
| MonsterName | Panda |
| Image | Companion_Panda |
| AI | -2 |
| Level | 0 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 3, CompanionBagWeight 70 |

### #198 · Rabbit

| 字段 | 值 |
|---|---|
| MonsterName | Rabbit |
| Image | Companion_Rabbit |
| AI | -2 |
| Level | 0 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 2 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | CompanionInventory 3, CompanionBagWeight 70 |

### #199 · Jinchon Devil

| 字段 | 值 |
|---|---|
| MonsterName | Jinchon Devil |
| Image | JinchonDevil |
| AI | 78 |
| Level | 250 |
| ViewRange | 12 |
| CoolEye | 100 |
| Experience | 975000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 800 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 53 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 22500, MinAC 70, MaxAC 70, MinMR 70, MaxMR 70, MinDC 270, MaxDC 375, Accuracy 40, Agility 48, FireResistance 4, IceResistance 5, LightningResistance 4, WindResistance 5, HolyResistance 4, DarkResistance 5, PhantomResistance 5, DarkAffinity 1 |

### #200 · Black Palace Demon

| 字段 | 值 |
|---|---|
| MonsterName | Black Palace Demon |
| Image | JinchonDevil |
| AI | 78 |
| Level | 250 |
| ViewRange | 12 |
| CoolEye | 100 |
| Experience | 877500 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 37 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 21750, MinAC 55, MaxAC 55, MinMR 62, MaxMR 62, MinDC 262, MaxDC 367, Accuracy 40, Agility 48, FireResistance 4, IceResistance 5, LightningResistance 4, WindResistance 5, HolyResistance 4, DarkResistance 5, PhantomResistance 5, DarkAffinity 1 |

### #201 · Brass Feral Warrior

| 字段 | 值 |
|---|---|
| MonsterName | Brass Feral Warrior |
| Image | FlameMinotaur |
| AI | 79 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 109200 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4200, MinAC 15, MaxAC 15, MinMR 15, MaxMR 15, MinDC 115, MaxDC 125, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 1, HolyResistance 1, DarkResistance -1, PhantomResistance 1, FireAffinity 1 |

### #202 · Obsidian Feral Warrior

| 字段 | 值 |
|---|---|
| MonsterName | Obsidian Feral Warrior |
| Image | FuryMinotaur |
| AI | 79 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 109200 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4200, MinAC 15, MaxAC 15, MinMR 15, MaxMR 15, MinDC 115, MaxDC 125, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 1, HolyResistance 1, PhantomResistance 1, WindAffinity 1 |

### #203 · Sun Feral Warrior

| 字段 | 值 |
|---|---|
| MonsterName | Sun Feral Warrior |
| Image | BanyaLeftGuard |
| AI | 80 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 122850 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 5000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 5400, MinAC 15, MaxAC 15, MinMR 15, MaxMR 15, MinDC 130, MaxDC 150, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 1, HolyResistance 1, DarkResistance -1, PhantomResistance 1, FireAffinity 1 |

### #204 · Moon Feral Warrior

| 字段 | 值 |
|---|---|
| MonsterName | Moon Feral Warrior |
| Image | BanyaRightGuard |
| AI | 81 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 126750 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 5000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 39 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 5000, MinAC 15, MaxAC 15, MinMR 15, MaxMR 15, MinDC 125, MaxDC 145, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 1, HolyResistance 1, DarkResistance 1, PhantomResistance 1, LightningAffinity 1 |

### #205 · Ox Feral General

| 字段 | 值 |
|---|---|
| MonsterName | Ox Feral General |
| Image | UmaAnguisher |
| AI | 82 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 104325 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 700 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 5000, MinAC 25, MaxAC 25, MinMR 25, MaxMR 25, MinDC 140, MaxDC 150, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 1, HolyResistance 1, PhantomResistance 1 |

### #206 · Flame Demon

| 字段 | 值 |
|---|---|
| MonsterName | Flame Demon |
| Image | UmaFlameThrower |
| AI | 83 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 101400 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | FlameDemon |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 34 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3260, MinAC 15, MaxAC 15, MinMR 15, MaxMR 15, MinDC 80, MaxDC 100, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 1, HolyResistance 1, PhantomResistance 1, FireAffinity 1 |

### #207 · Winged Horror

| 字段 | 值 |
|---|---|
| MonsterName | Winged Horror |
| Image | UmaKing |
| AI | 84 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 585000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 5000 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 52 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 25500, MinAC 62, MaxAC 62, MinMR 62, MaxMR 62, MinDC 224, MaxDC 300, Accuracy 40, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 2, WindResistance 5, HolyResistance 4, DarkResistance 5, PhantomResistance 4, LightningAffinity 1 |

### #208 · Enraged Emperor Sa'Woo

| 字段 | 值 |
|---|---|
| MonsterName | Enraged Emperor Sa'Woo |
| Image | EmperorSaWoo |
| AI | 85 |
| Level | 250 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 2340000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 60000 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 46 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 33000, MinAC 120, MaxAC 120, MinMR 75, MaxMR 75, MinDC 300, MaxDC 333, Accuracy 50, Agility 48, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 4, DarkResistance 5, PhantomResistance 4, WindAffinity 1 |

### #209 · Ferocious Flame Demon

| 字段 | 值 |
|---|---|
| MonsterName | Ferocious Flame Demon |
| Image | UmaFlameThrower |
| AI | 86 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 76050 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | FerociousFlameDemon |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3260, MinAC 15, MaxAC 15, MinMR 15, MaxMR 15, MinDC 80, MaxDC 100, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 1, HolyResistance 1, DarkResistance -2, PhantomResistance 1, FireAffinity 1 |

### #210 · Oma Warlord

| 字段 | 值 |
|---|---|
| MonsterName | Oma Warlord |
| Image | OmaWarlord |
| AI | 87 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 75 |
| Experience | 78000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1400 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 25 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1900, MinAC 17, MaxAC 17, MinMR 42, MaxMR 42, MinDC 80, MaxDC 100, Accuracy 30, Agility 17, FireResistance 3, IceResistance -2, LightningResistance 3, HolyResistance 1, DarkResistance 4, PhantomResistance 2 |

### #211 · Goru Spearman

| 字段 | 值 |
|---|---|
| MonsterName | Goru Spearman |
| Image | BoneSoldier |
| AI | 88 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 75 |
| Experience | 109200 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | GoruSpearman |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2200, MinAC 17, MaxAC 17, MinMR 42, MaxMR 42, MinDC 120, MaxDC 140, Accuracy 30, Agility 17, FireResistance 4, IceResistance -1, LightningResistance 2, WindResistance -2, HolyResistance 2, DarkResistance 3, PhantomResistance 1, FireAffinity 1 |

### #212 · Goru Archer

| 字段 | 值 |
|---|---|
| MonsterName | Goru Archer |
| Image | BoneArcher |
| AI | 89 |
| Level | 80 |
| ViewRange | 12 |
| CoolEye | 75 |
| Experience | 222850 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1700 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | GoruArcher |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2500, MinAC 20, MaxAC 20, MinMR 45, MaxMR 45, MinDC 108, MaxDC 157, Accuracy 30, Agility 19, FireResistance 3, IceResistance -2, LightningResistance 1, WindResistance -1, HolyResistance 1, DarkResistance 4, PhantomResistance 3 |

### #213 · Goru General

| 字段 | 值 |
|---|---|
| MonsterName | Goru General |
| Image | BoneCaptain |
| AI | 90 |
| Level | 80 |
| ViewRange | 12 |
| CoolEye | 75 |
| Experience | 117000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | GoruGeneral |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 32 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2700, MinAC 20, MaxAC 20, MinMR 46, MaxMR 46, MinDC 140, MaxDC 150, Accuracy 30, Agility 17, FireResistance 4, IceResistance -2, LightningResistance 1, WindResistance -2, HolyResistance 2, DarkResistance 3, PhantomResistance 1, DarkAffinity 1 |

### #215 · Enraged Arch Lich Taedu

| 字段 | 值 |
|---|---|
| MonsterName | Enraged Arch Lich Taedu |
| Image | ArchLichTaedu |
| AI | 91 |
| Level | 250 |
| ViewRange | 35 |
| CoolEye | 100 |
| Experience | 1852500 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 13100, MinAC 50, MaxAC 50, MinMR 72, MaxMR 72, MinDC 444, MaxDC 666, Accuracy 100, Agility 48, FireResistance 4, IceResistance 4, LightningResistance 4, WindResistance 3, HolyResistance 4, DarkResistance 4, PhantomResistance 3, SilenceChance 2 |

### #216 · Escort Commander

| 字段 | 值 |
|---|---|
| MonsterName | Escort Commander |
| Image | EscortCommander |
| AI | 93 |
| Level | 80 |
| ViewRange | 33 |
| CoolEye | 77 |
| Experience | 136500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 46 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3100, MinAC 23, MaxAC 23, MinMR 42, MaxMR 42, MinDC 130, MaxDC 170, Accuracy 25, Agility 18, FireResistance 5, IceResistance 4, WindResistance 5, HolyResistance 2, DarkResistance 1, PhantomResistance 1, LightningAffinity 1 |

### #217 · Fiery Dancer

| 字段 | 值 |
|---|---|
| MonsterName | Fiery Dancer |
| Image | FieryDancer |
| AI | 94 |
| Level | 80 |
| ViewRange | 33 |
| CoolEye | 77 |
| Experience | 130650 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 4000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 33 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2600, MinAC 18, MaxAC 18, MinMR 44, MaxMR 44, MinDC 120, MaxDC 155, Accuracy 24, Agility 19, FireResistance 5, IceResistance 4, WindResistance 5, HolyResistance 2, DarkResistance 3, PhantomResistance 1, FireAffinity 1 |

### #218 · Emerald Dancer

| 字段 | 值 |
|---|---|
| MonsterName | Emerald Dancer |
| Image | EmeraldDancer |
| AI | 95 |
| Level | 100 |
| ViewRange | 33 |
| CoolEye | 77 |
| Experience | 240400 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 4000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 33 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2800, MinAC 21, MaxAC 21, MinMR 45, MaxMR 45, MinDC 140, MaxDC 145, Accuracy 24, Agility 21, FireResistance 5, IceResistance 3, WindResistance 5, HolyResistance 1, DarkResistance 3, PhantomResistance 1, DarkAffinity 1 |

### #219 · Queen Of Dawn

| 字段 | 值 |
|---|---|
| MonsterName | Queen Of Dawn |
| Image | QueenOfDawn |
| AI | 96 |
| Level | 250 |
| ViewRange | 33 |
| CoolEye | 100 |
| Experience | 1950000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 41 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 126700, MinAC 82, MaxAC 82, MinMR 300, MaxMR 300, MinDC 437, MaxDC 750, Accuracy 27, Agility 48, FireResistance 5, IceResistance 4, LightningResistance 3, WindResistance 5, HolyResistance 4, DarkResistance 4, PhantomResistance 2, HolyAffinity 1 |

### #220 · Sabuk Lord

| 字段 | 值 |
|---|---|
| MonsterName | Sabuk Lord |
| Image | JinchonDevil |
| AI | 1000 |
| Level | 250 |
| ViewRange | 12 |
| CoolEye | 100 |
| Experience | 0 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1111111 |
| MoveDelay | 800 |
| IsBoss | true |
| Flag | CastleObjective |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 6 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 3 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3000, MinDC 1500, MaxDC 1500, Accuracy 100, IceAttack 1, PoisonResistance 100 |

### #221 · Oyoung Beast

| 字段 | 值 |
|---|---|
| MonsterName | Oyoung Beast |
| Image | OYoungBeast |
| AI | 97 |
| Level | 80 |
| ViewRange | 9 |
| CoolEye | 20 |
| Experience | 23400 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 1400 |
| IsBoss | false |
| Flag | OYoungBeast |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 75 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3100, MinAC 50, MaxAC 50, MinMR 35, MaxMR 35, MinDC 55, MaxDC 85, Accuracy 18, Agility 18, LightningResistance -2, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -5, FireAffinity 1 |

### #222 · Yumgon Witch

| 字段 | 值 |
|---|---|
| MonsterName | Yumgon Witch |
| Image | YumgonWitch |
| AI | 98 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 84825 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1400 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | YumgonWitch |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 75 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4350, MinAC 70, MaxAC 70, MinMR 40, MaxMR 40, MinDC 90, MaxDC 116, Accuracy 24, Agility 24, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -5, PhantomAffinity 1 |

### #223 · Ma Warden

| 字段 | 值 |
|---|---|
| MonsterName | Ma Warden |
| Image | OYoungBeast |
| AI | 97 |
| Level | 80 |
| ViewRange | 9 |
| CoolEye | 40 |
| Experience | 70200 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | MaWarden |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 8 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 71 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3600, MinAC 50, MaxAC 50, MinMR 35, MaxMR 35, MinDC 80, MaxDC 97, Accuracy 22, Agility 22, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -5, FireAffinity 1 |

### #224 · Ma Warlord

| 字段 | 值 |
|---|---|
| MonsterName | Ma Warlord |
| Image | MaWarlord |
| AI | 64 |
| Level | 80 |
| ViewRange | 9 |
| CoolEye | 40 |
| Experience | 73125 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | MaWarlord |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 8 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 71 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3800, MinAC 60, MaxAC 60, MinMR 40, MaxMR 40, MinDC 92, MaxDC 106, Accuracy 22, Agility 22, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -5, FireAffinity 1 |

### #225 · Jinhwan Spirit

| 字段 | 值 |
|---|---|
| MonsterName | Jinhwan Spirit |
| Image | JinhwanSpirit |
| AI | 99 |
| Level | 80 |
| ViewRange | 9 |
| CoolEye | 100 |
| Experience | 24375 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | JinhwanSpirit |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 62 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2800, MinAC 45, MaxAC 45, MinMR 26, MaxMR 26, MinDC 37, MaxDC 75, Accuracy 25, Agility 22, LightningResistance -1, WindResistance 5, HolyResistance -2, DarkResistance 5, PhantomResistance -1, LightningAffinity 1 |

### #226 · Jinhwan Guardian

| 字段 | 值 |
|---|---|
| MonsterName | Jinhwan Guardian |
| Image | JinhwanGuardian |
| AI | 26 |
| Level | 80 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 30810 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1250 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | JinhwanGuardian |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 71 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2950, MinAC 50, MaxAC 50, MinMR 26, MaxMR 26, MinDC 50, MaxDC 87, Accuracy 25, Agility 22, LightningResistance -1, WindResistance 5, HolyResistance -2, DarkResistance 5, PhantomResistance -1, DarkAffinity 1 |

### #227 · Oyoung General

| 字段 | 值 |
|---|---|
| MonsterName | Oyoung General |
| Image | MaWarlord |
| AI | 64 |
| Level | 80 |
| ViewRange | 9 |
| CoolEye | 20 |
| Experience | 26325 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | OyoungGeneral |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 71 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3500, MinAC 60, MaxAC 60, MinMR 40, MaxMR 40, MinDC 67, MaxDC 93, Accuracy 18, Agility 18, LightningResistance -2, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -5, FireAffinity 1 |

### #228 · Yumgon General

| 字段 | 值 |
|---|---|
| MonsterName | Yumgon General |
| Image | YumgonGeneral |
| AI | 0 |
| Level | 80 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 81900 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 1600 |
| IsBoss | false |
| Flag | YumgonGeneral |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 71 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4200, MinAC 80, MaxAC 80, MinMR 55, MaxMR 55, MinDC 93, MaxDC 131, Accuracy 24, Agility 24, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -5, FireAffinity 1 |

### #229 · Chiwoo General Of East

| 字段 | 值 |
|---|---|
| MonsterName | Chiwoo General Of East |
| Image | ChiwooGeneral |
| AI | 100 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 819000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 900 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 79 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 55555, MinAC 150, MaxAC 150, MinMR 105, MaxMR 105, MinDC 187, MaxDC 444, Accuracy 34, Agility 24, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -5, FireAffinity 1 |

### #230 · Chiwoo General Of West

| 字段 | 值 |
|---|---|
| MonsterName | Chiwoo General Of West |
| Image | ChiwooGeneral |
| AI | 100 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 819000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 900 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 79 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 55000, MinAC 150, MaxAC 150, MinMR 105, MaxMR 105, MinDC 187, MaxDC 444, Accuracy 34, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -5, FireAffinity 1 |

### #231 · Dragon Queen Jin'Ru

| 字段 | 值 |
|---|---|
| MonsterName | Dragon Queen Jin'Ru |
| Image | DragonQueen |
| AI | 101 |
| Level | 250 |
| ViewRange | 44 |
| CoolEye | 100 |
| Experience | 3510000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 600 |
| MoveDelay | 1000 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 75 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 48000, MinAC 300, MaxAC 300, MinMR 60, MaxMR 60, MinDC 360, MaxDC 382, Accuracy 50, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5, IceAffinity 1 |

### #232 · Dragon Lord Jin'Ryung

| 字段 | 值 |
|---|---|
| MonsterName | Dragon Lord Jin'Ryung |
| Image | DragonLord |
| AI | 102 |
| Level | 250 |
| ViewRange | 44 |
| CoolEye | 100 |
| Experience | 3997500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 1000 |
| IsBoss | true |
| Flag | DragonLord |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 85 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 73500, MinAC 350, MaxAC 350, MinMR 75, MaxMR 75, MinDC 375, MaxDC 382, Accuracy 50, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5, HolyAffinity 1 |

### #233 · Ferocious Ice Tiger

| 字段 | 值 |
|---|---|
| MonsterName | Ferocious Ice Tiger |
| Image | FerociousIceTiger |
| AI | 104 |
| Level | 250 |
| ViewRange | 12 |
| CoolEye | 100 |
| Experience | 1599000 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 800 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 41 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 75000, MinAC 200, MaxAC 200, MinMR 168, MaxMR 168, MinDC 330, MaxDC 500, Accuracy 250, Agility 48, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 7 |

### #244 · Escort Commander

| 字段 | 值 |
|---|---|
| MonsterName | Escort Commander |
| Image | EscortCommander |
| AI | 0 |
| Level | 80 |
| ViewRange | 33 |
| CoolEye | 77 |
| Experience | 136500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 24 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3100, MinAC 23, MaxAC 23, MinMR 42, MaxMR 42, MinDC 130, MaxDC 170, Accuracy 25, Agility 18, FireResistance 5, IceResistance 4, WindResistance 5, HolyResistance 2, DarkResistance 1, PhantomResistance 1 |

### #245 · Sama Cursed Bladesman

| 字段 | 值 |
|---|---|
| MonsterName | Sama Cursed Bladesman |
| Image | SamaCursedBladesman |
| AI | 71 |
| Level | 85 |
| ViewRange | 10 |
| CoolEye | 20 |
| Experience | 441090 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 13 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 8700, MinAC 100, MaxAC 100, MinMR 35, MaxMR 35, MinDC 150, MaxDC 270, Accuracy 25, Agility 25 |

### #246 · Sama Cursed Flame Mage

| 字段 | 值 |
|---|---|
| MonsterName | Sama Cursed Flame Mage |
| Image | SamaCursedFlameMage |
| AI | 106 |
| Level | 85 |
| ViewRange | 10 |
| CoolEye | 20 |
| Experience | 304200 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 14 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 6000, MinAC 35, MaxAC 35, MinMR 85, MaxMR 85, MinDC 150, MaxDC 220, Accuracy 25, Agility 25, FireAffinity 1 |

### #248 · Sama Cursed Slave

| 字段 | 值 |
|---|---|
| MonsterName | Sama Cursed Slave |
| Image | SamaCursedSlave |
| AI | 105 |
| Level | 85 |
| ViewRange | 10 |
| CoolEye | 20 |
| Experience | 327015 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 13 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 6450, MinAC 35, MaxAC 35, MinMR 75, MaxMR 75, MinDC 120, MaxDC 200, Accuracy 25, Agility 25, DarkAffinity 1 |

### #249 · Sama Fire Guardian

| 字段 | 值 |
|---|---|
| MonsterName | Sama Fire Guardian |
| Image | SamaFireGuardian |
| AI | 107 |
| Level | 88 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 524745 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 14 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 9000, MinAC 120, MaxAC 120, MinMR 85, MaxMR 85, MinDC 200, MaxDC 260, Accuracy 25, Agility 25 |

### #250 · Sama Ice Guardian

| 字段 | 值 |
|---|---|
| MonsterName | Sama Ice Guardian |
| Image | SamaIceGuardian |
| AI | 108 |
| Level | 88 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 692664 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 14 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 11880, MinAC 120, MaxAC 120, MinMR 85, MaxMR 85, MinDC 230, MaxDC 230, Accuracy 25, Agility 25 |

### #251 · Sama Lightning Guardian

| 字段 | 值 |
|---|---|
| MonsterName | Sama Lightning Guardian |
| Image | SamaLightningGuardian |
| AI | 109 |
| Level | 88 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 608705 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1600 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 18 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 10440, MinAC 120, MaxAC 120, MinMR 85, MaxMR 85, MinDC 10, MaxDC 450, Accuracy 25, Agility 25 |

### #252 · Sama Wind Guardian

| 字段 | 值 |
|---|---|
| MonsterName | Sama Wind Guardian |
| Image | SamaWindGuardian |
| AI | 110 |
| Level | 88 |
| ViewRange | 11 |
| CoolEye | 100 |
| Experience | 629694 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 14 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 10800, MinAC 120, MaxAC 120, MinMR 85, MaxMR 85, MinDC 210, MaxDC 250, Accuracy 25, Agility 25 |

### #253 · Black Sama

| 字段 | 值 |
|---|---|
| MonsterName | Black Sama |
| Image | BlackTortoise |
| AI | 112 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 6084000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 120000, MinAC 150, MaxAC 150, MinMR 105, MaxMR 105, MinDC 450, MaxDC 450, Accuracy 80, Agility 30, FireResistance 2, IceResistance 5, LightningResistance 2, WindResistance 2, HolyResistance -3, DarkResistance 2, PhantomResistance 2, IceAffinity 1 |

### #254 · Blue Sama

| 字段 | 值 |
|---|---|
| MonsterName | Blue Sama |
| Image | BlueDragon |
| AI | 113 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 4461600 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 27 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 88000, MinAC 100, MaxAC 100, MinMR 105, MaxMR 105, MinDC 450, MaxDC 450, Accuracy 80, Agility 30, FireResistance 2, IceResistance 5, LightningResistance 5, WindResistance 2, HolyResistance -3, DarkResistance 2, PhantomResistance 2, LightningAffinity 1 |

### #255 · Phoenix Sama

| 字段 | 值 |
|---|---|
| MonsterName | Phoenix Sama |
| Image | Phoenix |
| AI | 111 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 3650400 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 72000, MinAC 120, MaxAC 120, MinMR 105, MaxMR 105, MinDC 450, MaxDC 450, Accuracy 80, Agility 30, FireResistance 5, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance -3, DarkResistance 2, PhantomResistance 2, FireAffinity 1 |

### #256 · White Tiger Sama

| 字段 | 值 |
|---|---|
| MonsterName | White Tiger Sama |
| Image | WhiteTiger |
| AI | 114 |
| Level | 250 |
| ViewRange | 10 |
| CoolEye | 100 |
| Experience | 4867200 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 23 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 96000, MinAC 150, MaxAC 150, MinMR 105, MaxMR 105, MinDC 350, MaxDC 450, Accuracy 80, Agility 30, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 5, HolyResistance -3, DarkResistance 2, PhantomResistance 2, WindAffinity 1 |

### #258 · Enshrinement Box

| 字段 | 值 |
|---|---|
| MonsterName | Enshrinement Box |
| Image | EnshrinementBox |
| AI | 4 |
| Level | 85 |
| ViewRange | 0 |
| CoolEye | 0 |
| Experience | 3900000 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 3 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 5 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 17 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 30, MinMR 500, MaxMR 500 |

### #259 · Sama Prophet

| 字段 | 值 |
|---|---|
| MonsterName | Sama Prophet |
| Image | SamaProphet |
| AI | 115 |
| Level | 250 |
| ViewRange | 12 |
| CoolEye | 0 |
| Experience | 10545600 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 0 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 24 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 208000, MinAC 270, MaxAC 270, MinMR 200, MaxMR 200, MinDC 180, MaxDC 355, Accuracy 80, Agility 30, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5 |

### #260 · Sama Sorcerer

| 字段 | 值 |
|---|---|
| MonsterName | Sama Sorcerer |
| Image | SamaSorcerer |
| AI | 116 |
| Level | 250 |
| ViewRange | 12 |
| CoolEye | 0 |
| Experience | 12421500 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 25000 |
| IsBoss | true |
| Flag | SamaSorcerer |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 3 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 245000, MinAC 330, MaxAC 330, MinMR 150, MaxMR 150, MinDC 350, MaxDC 550, Accuracy 80, Agility 30, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5, PhantomAffinity 1 |

### #261 · Blood Stone

| 字段 | 值 |
|---|---|
| MonsterName | Blood Stone |
| Image | BloodStone |
| AI | 4 |
| Level | 250 |
| ViewRange | 0 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | BloodStone |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 1 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 400 |

### #262 · Life Stone

| 字段 | 值 |
|---|---|
| MonsterName | Life Stone |
| Image | BloodStone |
| AI | 4 |
| Level | 250 |
| ViewRange | 0 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | BloodStone |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 1 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 400 |

### #263 · Dark Stone

| 字段 | 值 |
|---|---|
| MonsterName | Dark Stone |
| Image | BloodStone |
| AI | 4 |
| Level | 250 |
| ViewRange | 0 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | BloodStone |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 1 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 2 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 400 |

### #264 · Young Tiger

| 字段 | 值 |
|---|---|
| MonsterName | Young Tiger |
| Image | OrangeTiger |
| AI | 0 |
| Level | 58 |
| ViewRange | 7 |
| CoolEye | 50 |
| Experience | 97500 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 18 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3000, MinAC 25, MaxAC 25, MinMR 25, MaxMR 25, MinDC 70, MaxDC 130, Accuracy 19, Agility 22, HolyResistance -3, DarkResistance -3, PhantomResistance -3, HolyAffinity 1 |

### #265 · Tiger

| 字段 | 值 |
|---|---|
| MonsterName | Tiger |
| Image | RegularTiger |
| AI | 0 |
| Level | 63 |
| ViewRange | 7 |
| CoolEye | 50 |
| Experience | 130000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 18 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4000, MinAC 30, MaxAC 30, MinMR 30, MaxMR 30, MinDC 90, MaxDC 150, Accuracy 22, Agility 25, HolyResistance -3, DarkResistance -3, PhantomResistance -3, WindAffinity 1, PhysicalResistance -2 |

### #266 · Blood Tiger

| 字段 | 值 |
|---|---|
| MonsterName | Blood Tiger |
| Image | RedTiger |
| AI | 6 |
| Level | 90 |
| ViewRange | 30 |
| CoolEye | 50 |
| Experience | 56250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 18 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4500, MinAC 30, MaxAC 30, MinMR 30, MaxMR 30, MinDC 210, MaxDC 250, Accuracy 22, Agility 25, HolyResistance -3, DarkResistance -3, PhantomResistance -3, PhantomAffinity 1, PhysicalResistance -2 |

### #267 · Blizzard Tiger

| 字段 | 值 |
|---|---|
| MonsterName | Blizzard Tiger |
| Image | SnowTiger |
| AI | 68 |
| Level | 90 |
| ViewRange | 30 |
| CoolEye | 50 |
| Experience | 56250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2200 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 17 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4500, MinAC 30, MaxAC 30, MinMR 30, MaxMR 30, MinDC 210, MaxDC 250, Accuracy 22, Agility 25, HolyResistance -3, DarkResistance -3, PhantomResistance -3, IceAffinity 1, PhysicalResistance -3 |

### #268 · Dark Tiger

| 字段 | 值 |
|---|---|
| MonsterName | Dark Tiger |
| Image | BlackTiger |
| AI | 0 |
| Level | 90 |
| ViewRange | 30 |
| CoolEye | 50 |
| Experience | 56250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 17 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4500, MinAC 30, MaxAC 30, MinMR 30, MaxMR 30, MinDC 210, MaxDC 250, Accuracy 22, Agility 25, HolyResistance -3, DarkResistance -3, PhantomResistance -3, PhysicalResistance -3 |

### #269 · Elder Dark Tiger

| 字段 | 值 |
|---|---|
| MonsterName | Elder Dark Tiger |
| Image | BigBlackTiger |
| AI | 0 |
| Level | 250 |
| ViewRange | 30 |
| CoolEye | 100 |
| Experience | 750000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 1200 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 34 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 7000, MinAC 120, MaxAC 120, MinMR 85, MaxMR 85, MinDC 420, MaxDC 500, Accuracy 40, Agility 30, HolyResistance -3, DarkResistance -3, PhantomResistance -3, PhysicalResistance -5 |

### #270 · Elder White Tiger

| 字段 | 值 |
|---|---|
| MonsterName | Elder White Tiger |
| Image | BigWhiteTiger |
| AI | 68 |
| Level | 250 |
| ViewRange | 30 |
| CoolEye | 100 |
| Experience | 1000000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 1200 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 34 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 8000, MinAC 120, MaxAC 120, MinMR 85, MaxMR 85, MinDC 420, MaxDC 500, Accuracy 40, Agility 30, HolyResistance -3, DarkResistance -3, PhantomResistance -3, WindAffinity 1, PhysicalResistance -5 |

### #271 · Tiger General

| 字段 | 值 |
|---|---|
| MonsterName | Tiger General |
| Image | OrangeBossTiger |
| AI | 0 |
| Level | 250 |
| ViewRange | 7 |
| CoolEye | 0 |
| Experience | 0 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| Drops | `DropInfo` × 17 条（明细见 [DropInfo.md](DropInfo.md)） |

### #272 · Tiger War Lord

| 字段 | 值 |
|---|---|
| MonsterName | Tiger War Lord |
| Image | FerociousIceTiger |
| AI | 104 |
| Level | 250 |
| ViewRange | 15 |
| CoolEye | 100 |
| Experience | 2500000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 51 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 100000, MinAC 220, MaxAC 220, MinMR 220, MaxMR 220, MinDC 500, MaxDC 700, Accuracy 60, Agility 30, FireResistance 3, IceResistance 3, LightningResistance 3, WindResistance 3, HolyResistance 3, DarkResistance 3, PhantomResistance 3, WindAffinity 1, PhantomAffinity 1, PhysicalResistance -4 |

### #273 · Wild Elephant

| 字段 | 值 |
|---|---|
| MonsterName | Wild Elephant |
| Image | EvilElephant |
| AI | 0 |
| Level | 60 |
| ViewRange | 8 |
| CoolEye | 50 |
| Experience | 119600 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3200 |
| MoveDelay | 1600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 18 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4000, MinAC 12, MaxAC 12, MinMR 15, MaxMR 15, MinDC 80, MaxDC 120, Accuracy 25, Agility 18, HolyResistance -3, DarkResistance -3, PhantomResistance -3, HolyAffinity 1, PhysicalResistance -3 |

### #274 · Wild Monkey

| 字段 | 值 |
|---|---|
| MonsterName | Wild Monkey |
| Image | WildMonkey |
| AI | 23 |
| Level | 63 |
| ViewRange | 9 |
| CoolEye | 50 |
| Experience | 83200 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 18 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2500, MinAC 16, MaxAC 16, MinMR 27, MaxMR 27, MinDC 69, MaxDC 94, Accuracy 22, Agility 18, HolyResistance -3, DarkResistance -3, PhantomResistance -3, WindAffinity 1, PhysicalResistance -3 |

### #275 · Wild Fanatic

| 字段 | 值 |
|---|---|
| MonsterName | Wild Fanatic |
| Image | EvilFanatic |
| AI | 0 |
| Level | 61 |
| ViewRange | 10 |
| CoolEye | 50 |
| Experience | 135135 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1900 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 18 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3850, MinAC 15, MaxAC 15, MinMR 52, MaxMR 52, MinDC 90, MaxDC 95, Accuracy 25, Agility 23, HolyResistance -3, DarkResistance -3, PhantomResistance -3, PhysicalResistance -3 |

### #276 · Frost Yeti

| 字段 | 值 |
|---|---|
| MonsterName | Frost Yeti |
| Image | FrostYeti |
| AI | 0 |
| Level | 90 |
| ViewRange | 30 |
| CoolEye | 50 |
| Experience | 56250 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 15 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 4500, MinAC 30, MaxAC 30, MinMR 30, MaxMR 30, MinDC 210, MaxDC 250, Accuracy 22, Agility 25, HolyResistance -3, DarkResistance -3, PhantomResistance -3, PhysicalResistance 2 |

### #277 · Evil Snake

| 字段 | 值 |
|---|---|
| MonsterName | Evil Snake |
| Image | EvilSnake |
| AI | 0 |
| Level | 85 |
| ViewRange | 10 |
| CoolEye | 50 |
| Experience | 864000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 27 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 18000, MinAC 80, MaxAC 80, MinMR 100, MaxMR 100, MinDC 200, MaxDC 225, Accuracy 30, Agility 22, LightningResistance 3, WindResistance 2, HolyResistance -2, DarkResistance -3, PhantomResistance -2 |

### #278 · Salamander

| 字段 | 值 |
|---|---|
| MonsterName | Salamander |
| Image | Salamander |
| AI | 0 |
| Level | 85 |
| ViewRange | 9 |
| CoolEye | 75 |
| Experience | 1104000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 1300 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 27 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 23000, MinAC 50, MaxAC 50, MinMR 50, MaxMR 50, MinDC 280, MaxDC 300, Accuracy 25, Agility 30, FireResistance 2, IceResistance 2, LightningResistance 2, HolyResistance -3, DarkResistance -3, PhantomResistance -3 |

### #279 · Sand Golem

| 字段 | 值 |
|---|---|
| MonsterName | Sand Golem |
| Image | SandGolem |
| AI | 27 |
| Level | 85 |
| ViewRange | 10 |
| CoolEye | 0 |
| Experience | 1200000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 27 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 25000, MinAC 80, MaxAC 80, MinMR 80, MaxMR 80, MinDC 90, MaxDC 130, Accuracy 25, Agility 25, FireResistance -4, IceResistance -4, LightningResistance -4, WindResistance -4, HolyResistance -4, DarkResistance -4, PhantomResistance -4 |

### #284 · Oma Mage

| 字段 | 值 |
|---|---|
| MonsterName | Oma Mage |
| Image | OmaMage |
| AI | 118 |
| Level | 100 |
| ViewRange | 17 |
| CoolEye | 35 |
| Experience | 600000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 4000 |
| MoveDelay | 1400 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 12 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 10000, MinAC 12, MaxAC 175, MinMR 12, MaxMR 200, MinDC 300, MaxDC 400, Accuracy 30, Agility 30, FireResistance 4, LightningResistance 2, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -4, LightningAffinity 1, PhysicalResistance -1 |

### #291 · Crystal Golem

| 字段 | 值 |
|---|---|
| MonsterName | Crystal Golem |
| Image | CrystalGolem |
| AI | 0 |
| Level | 85 |
| ViewRange | 10 |
| CoolEye | 20 |
| Experience | 1344000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 2000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 27 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 28000, MinAC 10, MaxAC 10, MinMR 20, MaxMR 20, MinDC 100, MaxDC 250, Accuracy 35, Agility 25, PhantomAffinity 1 |

### #292 · Dust Devil

| 字段 | 值 |
|---|---|
| MonsterName | Dust Devil |
| Image | DustDevil |
| AI | 0 |
| Level | 100 |
| ViewRange | 13 |
| CoolEye | 100 |
| Experience | 864000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 800 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 27 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 18000, MinAC 125, MaxAC 125, MinMR 35, MaxMR 35, MinDC 180, MaxDC 210, Accuracy 25, Agility 25, FireResistance 1, IceResistance 2, LightningResistance 2, WindResistance 5, HolyResistance 1, DarkResistance 2, PhantomResistance 3, WindAffinity 1 |

### #293 · Twin Tail Scorpion

| 字段 | 值 |
|---|---|
| MonsterName | Twin Tail Scorpion |
| Image | TwinTailScorpion |
| AI | 119 |
| Level | 85 |
| ViewRange | 10 |
| CoolEye | 39 |
| Experience | 816000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 17000, MinAC 80, MaxAC 80, MinMR 120, MaxMR 120, MinDC 130, MaxDC 150, Accuracy 30, Agility 30, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance -1, HolyResistance -1, DarkResistance -1, PhantomResistance -3, LightningAffinity 1, PhysicalResistance -1 |

### #294 · Bloody Mole

| 字段 | 值 |
|---|---|
| MonsterName | Bloody Mole |
| Image | BloodyMole |
| AI | 0 |
| Level | 85 |
| ViewRange | 4 |
| CoolEye | 100 |
| Experience | 1008000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3500 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 27 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 21000, MinAC 80, MaxAC 80, MinMR 90, MaxMR 90, MinDC 700, MaxDC 800, Accuracy 25, Agility 25, FireResistance -2, IceResistance 4, LightningResistance -2, WindResistance -2, HolyResistance -2, DarkResistance -2, PhantomResistance -2, FireAffinity 1, PhysicalResistance -2 |

### #295 · Imp

| 字段 | 值 |
|---|---|
| MonsterName | Imp |
| Image | SDMob19 |
| AI | 0 |
| Level | 100 |
| ViewRange | 10 |
| CoolEye | 36 |
| Experience | 1760000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 800 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 15 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 32000, MinAC 60, MaxAC 60, MinMR 70, MaxMR 70, MinDC 120, MaxDC 750, Accuracy 30, Agility 25, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance -1, DarkResistance -1, PhantomResistance -1, FireAffinity 1, PhysicalResistance -1 |

### #296 · Ettin

| 字段 | 值 |
|---|---|
| MonsterName | Ettin |
| Image | SDMob20 |
| AI | 0 |
| Level | 92 |
| ViewRange | 12 |
| CoolEye | 55 |
| Experience | 2200000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1400 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 12 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 18 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 40000, MinAC 25, MaxAC 25, MinMR 70, MaxMR 70, MinDC 150, MaxDC 700, Accuracy 30, Agility 25, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance -1, DarkResistance -1, PhantomResistance -1, PhysicalResistance -2 |

### #297 · Centurion

| 字段 | 值 |
|---|---|
| MonsterName | Centurion |
| Image | SDMob21 |
| AI | 89 |
| Level | 100 |
| ViewRange | 11 |
| CoolEye | 80 |
| Experience | 1610000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 3000 |
| MoveDelay | 1100 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 8 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 15 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 23000, MinAC 80, MaxAC 80, MinMR 10, MaxMR 10, MinDC 200, MaxDC 400, Accuracy 30, Agility 20, FireResistance -2, IceResistance -2, LightningResistance -2, WindResistance -2, HolyResistance -3, DarkResistance -3, PhantomResistance -3, IgnoreStealth 1 |

### #298 · Rot Wraith

| 字段 | 值 |
|---|---|
| MonsterName | Rot Wraith |
| Image | SDMob22 |
| AI | 7 |
| Level | 93 |
| ViewRange | 16 |
| CoolEye | 15 |
| Experience | 2475000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 15 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 45000, MinAC 60, MaxAC 60, MinMR 120, MaxMR 120, MinDC 300, MaxDC 680, Accuracy 30, Agility 20, DarkAffinity 1, IgnoreStealth 1 |

### #299 · Cotoblepas

| 字段 | 值 |
|---|---|
| MonsterName | Cotoblepas |
| Image | SDMob23 |
| AI | 0 |
| Level | 89 |
| ViewRange | 12 |
| CoolEye | 251 |
| Experience | 2200000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 6 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 15 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 40000, MinAC 60, MaxAC 60, MinMR 10, MaxMR 40, MinDC 350, MaxDC 700, Accuracy 30, Agility 20, PhysicalResistance -1 |

### #300 · Azog

| 字段 | 值 |
|---|---|
| MonsterName | Azog |
| Image | SDMob24 |
| AI | 0 |
| Level | 250 |
| ViewRange | 500 |
| CoolEye | 251 |
| Experience | 40000000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 38 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 500000, MinAC 125, MaxAC 125, MinMR 125, MaxMR 125, MinDC 800, MaxDC 1000, Accuracy 50, Agility 50, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5, PhantomAffinity 1 |

### #301 · Urukhia

| 字段 | 值 |
|---|---|
| MonsterName | Urukhia |
| Image | SDMob25 |
| AI | 0 |
| Level | 250 |
| ViewRange | 500 |
| CoolEye | 251 |
| Experience | 40000000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 5 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 36 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 500000, MinAC 125, MaxAC 125, MinMR 125, MaxMR 125, MinDC 800, MaxDC 1000, Accuracy 50, Agility 50, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5, HolyAffinity 1 |

### #302 · Gang Spider

| 字段 | 值 |
|---|---|
| MonsterName | Gang Spider |
| Image | GangSpider |
| AI | 0 |
| Level | 85 |
| ViewRange | 10 |
| CoolEye | 60 |
| Experience | 672000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1250 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 26 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 14000, MinAC 75, MaxAC 75, MinMR 20, MaxMR 20, MinDC 300, MaxDC 300, Accuracy 25, Agility 25, FireResistance 3, IceResistance 2, LightningResistance 2, WindResistance 3, PhysicalResistance -1 |

### #303 · Venom Spider

| 字段 | 值 |
|---|---|
| MonsterName | Venom Spider |
| Image | VenomSpider |
| AI | 46 |
| Level | 85 |
| ViewRange | 10 |
| CoolEye | 80 |
| Experience | 1056000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 26 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 22000, MinAC 75, MaxAC 75, MinMR 75, MaxMR 75, MinDC 150, MaxDC 200, Accuracy 33, Agility 20, IceResistance 1, WindResistance -2, HolyResistance -1, DarkResistance -1, PhantomResistance -3, DarkAffinity 1 |

### #304 · Chubarak

| 字段 | 值 |
|---|---|
| MonsterName | Chubarak |
| Image | SDMob26 |
| AI | 0 |
| Level | 250 |
| ViewRange | 500 |
| CoolEye | 100 |
| Experience | 40000000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 900 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 3 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 38 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 500000, MinAC 125, MaxAC 125, MinMR 125, MaxMR 125, MinDC 800, MaxDC 1000, Accuracy 50, Agility 50, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5 |

### #305 · Doom Claw

| 字段 | 值 |
|---|---|
| MonsterName | Doom Claw |
| Image | LobsterLord |
| AI | 120 |
| Level | 500 |
| ViewRange | 7 |
| CoolEye | 100 |
| Experience | 15000000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2000 |
| MoveDelay | 0 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 12 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 81 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 6000000, MinDC 900, MaxDC 1700, Accuracy 1000, FireAffinity 1, IceAffinity 1, LightningAffinity 1, WindAffinity 1, HolyAffinity 1, DarkAffinity 1, PhantomAffinity 1, CriticalDamage 50 |

### #307 · Zauhk Spawn

| 字段 | 值 |
|---|---|
| MonsterName | Zauhk Spawn |
| Image | NewMob5 |
| AI | 0 |
| Level | 250 |
| ViewRange | 100 |
| CoolEye | 77 |
| Experience | 5000000 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 22 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 28 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 300000, MinAC 60, MaxAC 60, MinMR 120, MaxMR 120, MinDC 600, MaxDC 700, Accuracy 55, Agility 33, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance 2, DarkResistance 2, PhantomResistance 2, HolyAffinity 1, PhantomAffinity 1, PhysicalResistance 2, BlockChance 33, EvasionChance 33, IgnoreStealth 100 |

### #308 · Shell Spliter

| 字段 | 值 |
|---|---|
| MonsterName | Shell Spliter |
| Image | NewMob9 |
| AI | 123 |
| Level | 250 |
| ViewRange | 22 |
| CoolEye | 35 |
| Experience | 6000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | QuartzMiniTurtle |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Stats | Health 50000, MinAC 175, MaxAC 175, MinMR 200, MaxMR 200, MinDC 1250, MaxDC 1750, Accuracy 30, Agility 30, FireResistance 4, LightningResistance 2, WindResistance -1, HolyResistance -2, DarkResistance -1, PhantomResistance -4, PhantomAffinity 1, PhysicalResistance -1, IgnoreStealth 100 |

### #309 · Ember Mage

| 字段 | 值 |
|---|---|
| MonsterName | Ember Mage |
| Image | NewMob7 |
| AI | 111 |
| Level | 250 |
| ViewRange | 35 |
| CoolEye | 100 |
| Experience | 15300000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | QuartzRedHood |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 5 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 19 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 120000, MinAC 150, MaxAC 150, MinMR 105, MaxMR 105, MinDC 777, MaxDC 900, Accuracy 80, Agility 30, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, DarkResistance 2, PhantomResistance 2, FireAffinity 1, IgnoreStealth 100 |

### #310 · Bobbit Worm

| 字段 | 值 |
|---|---|
| MonsterName | Bobbit Worm |
| Image | NewMob2 |
| AI | 125 |
| Level | 250 |
| ViewRange | 2 |
| CoolEye | 44 |
| Experience | 3600000 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 900 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 8 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 16 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 120000, MinAC 150, MaxAC 150, MinMR 105, MaxMR 105, MinDC 600, MaxDC 2300, Accuracy 46, Agility 30, FireResistance -1, IceResistance -1, LightningResistance -1, WindResistance -1, HolyResistance -1, DarkResistance -1, PhantomResistance -1 |

### #311 · Cobalt Golum

| 字段 | 值 |
|---|---|
| MonsterName | Cobalt Golum |
| Image | NewMob4 |
| AI | 59 |
| Level | 250 |
| ViewRange | 15 |
| CoolEye | 70 |
| Experience | 6668250 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 900 |
| IsBoss | false |
| Flag | QuartzBlueCrystal |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 5 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 15 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 90000, MinAC 79, MaxAC 79, MinMR 105, MaxMR 105, MinDC 666, MaxDC 900, Accuracy 80, Agility 30, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, DarkResistance 2, PhantomResistance 2, IceAffinity 1 |

### #312 · Shimmer Wings

| 字段 | 值 |
|---|---|
| MonsterName | Shimmer Wings |
| Image | NewMob1 |
| AI | 121 |
| Level | 250 |
| ViewRange | 13 |
| CoolEye | 77 |
| Experience | 3240400 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2800 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | QuartzPinkBat |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 7 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 15 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 92800, MinAC 21, MaxAC 21, MinMR 45, MaxMR 45, MinDC 333, MaxDC 666, Accuracy 24, Agility 21, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance 1, DarkResistance 1, PhantomResistance 1, PhantomAffinity 1 |

### #313 · Vex Wings

| 字段 | 值 |
|---|---|
| MonsterName | Vex Wings |
| Image | NewMob3 |
| AI | 42 |
| Level | 250 |
| ViewRange | 12 |
| CoolEye | 55 |
| Experience | 4200000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1400 |
| IsBoss | false |
| Flag | QuartzBlueBat |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 10 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 21 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 140000, MinAC 25, MaxAC 25, MinMR 70, MaxMR 70, MinDC 333, MaxDC 700, Accuracy 30, Agility 25, FireResistance -3, IceResistance -3, LightningResistance -3, WindResistance -3, HolyResistance -3, DarkResistance -3, PhantomResistance -3, LightningAffinity 1, PhysicalResistance 1 |

### #314 · Rot Wraith

| 字段 | 值 |
|---|---|
| MonsterName | Rot Wraith |
| Image | SDMob22 |
| AI | 7 |
| Level | 93 |
| ViewRange | 16 |
| CoolEye | 15 |
| Experience | 2475000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |

### #331 · Rot Wraith

| 字段 | 值 |
|---|---|
| MonsterName | Rot Wraith |
| Image | SDMob22 |
| AI | 7 |
| Level | 93 |
| ViewRange | 16 |
| CoolEye | 15 |
| Experience | 2475000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |

### #332 · Ember SpearMan

| 字段 | 值 |
|---|---|
| MonsterName | Ember SpearMan |
| Image | NewMob6 |
| AI | 88 |
| Level | 250 |
| ViewRange | 15 |
| CoolEye | 100 |
| Experience | 14750000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 1200 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 15 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 200000, MinAC 125, MaxAC 125, MinMR 125, MaxMR 125, MinDC 800, MaxDC 1000, Accuracy 50, Agility 50, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance 2, DarkResistance 2, PhantomResistance 2, FireAffinity 1 |

### #333 · Kongeegen

| 字段 | 值 |
|---|---|
| MonsterName | Kongeegen |
| Image | NewMob10 |
| AI | 124 |
| Level | 250 |
| ViewRange | 0 |
| CoolEye | 0 |
| Experience | 3900000 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 0 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 4 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3250, MinMR 500, MaxMR 500, LightningAffinity 1 |

### #334 · Adamantoise

| 字段 | 值 |
|---|---|
| MonsterName | Adamantoise |
| Image | NewMob8 |
| AI | 122 |
| Level | 250 |
| ViewRange | 500 |
| CoolEye | 100 |
| Experience | 40000000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1000 |
| MoveDelay | 600 |
| IsBoss | true |
| Flag | QuartzTurtleSub |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 24 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 2000000, MinAC 125, MaxAC 125, MinMR 125, MaxMR 125, MinDC 1766, MaxDC 2500, Accuracy 50, Agility 50, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5, LifeSteal 2100, PhantomAffinity 1, ReflectDamage 6, PhysicalResistance 3, EvasionChance 20, IgnoreStealth 1500 |

### #335 · Zauhk

| 字段 | 值 |
|---|---|
| MonsterName | Zauhk |
| Image | NewMob5 |
| AI | 117 |
| Level | 250 |
| ViewRange | 6 |
| CoolEye | 77 |
| Experience | 9000000 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 50000 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 26 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 35 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3000000, MinAC 60, MaxAC 60, MinMR 120, MaxMR 120, MinDC 2277, MaxDC 3000, Accuracy 30, Agility 20, FireResistance 2, IceResistance 2, LightningResistance 2, WindResistance 2, HolyResistance 2, DarkResistance 2, PhantomResistance 2, LifeSteal 400, HolyAffinity 1, PhantomAffinity 1, ReflectDamage 5, PhysicalResistance 2, BlockChance 7, EvasionChance 20 |

### #336 · MonasteryRaisingGhost

| 字段 | 值 |
|---|---|
| MonsterName | MonasteryRaisingGhost |
| Image | CorpseRaisingGhost |
| AI | 10 |
| Level | 79 |
| ViewRange | 12 |
| CoolEye | 55 |
| Experience | 2200000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 63225, MinAC 35, MaxAC 35, MinMR 70, MaxMR 70, MinDC 190, MaxDC 230, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 2, HolyResistance 2, DarkResistance 1, PhysicalResistance 1 |

### #337 · MonasteryGhoul

| 字段 | 值 |
|---|---|
| MonsterName | MonasteryGhoul |
| Image | GhostMage |
| AI | 119 |
| Level | 79 |
| ViewRange | 13 |
| CoolEye | 55 |
| Experience | 2200000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 63225, MinAC 35, MaxAC 35, MinMR 70, MaxMR 70, MinDC 799, MaxDC 900, Accuracy 30, Agility 24, FireResistance 1, IceResistance -2, LightningResistance 3, WindResistance 5, HolyResistance 3, DarkResistance 2, PhantomResistance 2, PhysicalResistance 1 |

### #338 · MonasterySorcer

| 字段 | 值 |
|---|---|
| MonsterName | MonasterySorcer |
| Image | GhostSorcerer |
| AI | 9 |
| Level | 79 |
| ViewRange | 13 |
| CoolEye | 55 |
| Experience | 2200000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 18 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 63225, MinAC 35, MaxAC 35, MinMR 70, MaxMR 70, MinDC 800, MaxDC 800, Accuracy 30, Agility 24, FireResistance 2, IceResistance 1, LightningResistance 2, WindResistance 2, HolyResistance 2, DarkResistance 2, PhantomResistance 2, HolyAffinity 1, PhysicalResistance 2 |

### #339 · MonasteryVoracious

| 字段 | 值 |
|---|---|
| MonsterName | MonasteryVoracious |
| Image | VoraciousGhost |
| AI | 0 |
| Level | 89 |
| ViewRange | 13 |
| CoolEye | 55 |
| Experience | 2200000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1500 |
| MoveDelay | 1500 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 31 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 63225, MinAC 35, MaxAC 35, MinMR 70, MaxMR 70, MinDC 700, MaxDC 700, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 2, HolyResistance 2, DarkResistance 1, PhysicalResistance 1 |

### #341 · MonasteryDevour

| 字段 | 值 |
|---|---|
| MonsterName | MonasteryDevour |
| Image | DevouringGhost |
| AI | 10 |
| Level | 79 |
| ViewRange | 13 |
| CoolEye | 55 |
| Experience | 2200000 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 2500 |
| MoveDelay | 1800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 17 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 22 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 63225, MinAC 35, MaxAC 35, MinMR 70, MaxMR 70, MinDC 700, MaxDC 700, Accuracy 30, Agility 24, FireResistance 1, IceResistance 1, LightningResistance 1, WindResistance 2, HolyResistance 2, DarkResistance 1, PhysicalResistance 1 |

### #342 · Sumerian

| 字段 | 值 |
|---|---|
| MonsterName | Sumerian |
| Image | MonasteryMon4 |
| AI | 126 |
| Level | 250 |
| ViewRange | 15 |
| CoolEye | 55 |
| Experience | 50000000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1100 |
| MoveDelay | 800 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 21 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 48 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 5000000, MinAC 125, MaxAC 125, MinMR 125, MaxMR 125, MinDC 4000, MaxDC 4000, Accuracy 50, Agility 50, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5, ReflectDamage 5, EvasionChance 20, PoisonResistance 100 |

### #343 · Sacrifice

| 字段 | 值 |
|---|---|
| MonsterName | Sacrifice |
| Image | MonasteryMon5 |
| AI | 117 |
| Level | 250 |
| ViewRange | 15 |
| CoolEye | 55 |
| Experience | 50000000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 800 |
| MoveDelay | 800 |
| IsBoss | true |
| Flag | Sacrifice |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 20 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Drops | `DropInfo` × 48 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 3500000, MinAC 125, MaxAC 125, MinMR 125, MaxMR 125, MinDC 3000, MaxDC 3000, Accuracy 50, Agility 50, FireResistance 5, IceResistance 5, LightningResistance 5, WindResistance 5, HolyResistance 5, DarkResistance 5, PhantomResistance 5, PhantomAffinity 1, ReflectDamage 80, EvasionChance 20 |

### #344 · Enheduanna

| 字段 | 值 |
|---|---|
| MonsterName | Enheduanna |
| Image | MonasteryMon2 |
| AI | 117 |
| Level | 250 |
| ViewRange | 15 |
| CoolEye | 55 |
| Experience | 3240400 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1300 |
| MoveDelay | 600 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 20 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 13 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 123225, MinAC 135, MaxAC 135, MinMR 70, MaxMR 70, MinDC 1555, MaxDC 1000, Accuracy 30, Agility 24, FireResistance 3, IceResistance 7, LightningResistance 1, WindResistance 2, HolyResistance 2, DarkResistance 1, HolyAffinity 1, PhysicalResistance 1, BlockChance 10, EvasionChance 25 |

### #345 · Quadishtu

| 字段 | 值 |
|---|---|
| MonsterName | Quadishtu |
| Image | MonasteryMon3 |
| AI | 0 |
| Level | 250 |
| ViewRange | 9 |
| CoolEye | 55 |
| Experience | 3240400 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 800 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 19 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 13 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 123225, MinAC 135, MaxAC 135, MinMR 70, MaxMR 70, MinDC 1500, MaxDC 1700, Accuracy 30, Agility 24, FireResistance 3, IceResistance 7, LightningResistance 1, WindResistance 2, HolyResistance 2, DarkResistance 1, HolyAffinity 1, PhysicalResistance 1, BlockChance 10 |

### #347 · Sumerian King

| 字段 | 值 |
|---|---|
| MonsterName | Sumerian King |
| Image | MonasteryMon6 |
| AI | 127 |
| Level | 250 |
| ViewRange | 19 |
| CoolEye | 55 |
| Experience | 50000000 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1200 |
| MoveDelay | 700 |
| IsBoss | true |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 21 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 1 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 48 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 5000000, MinAC 125, MaxAC 125, MinMR 125, MaxMR 125, MinDC 2222, MaxDC 4444, Accuracy 50, Agility 50, ReflectDamage 25, EvasionChance 20, PoisonResistance 100 |

### #348 · Puabi

| 字段 | 值 |
|---|---|
| MonsterName | Puabi |
| Image | MonasteryMon1 |
| AI | 0 |
| Level | 250 |
| ViewRange | 14 |
| CoolEye | 55 |
| Experience | 3240400 |
| Undead | true |
| CanPush | true |
| CanTame | false |
| AttackDelay | 1400 |
| MoveDelay | 1000 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 20 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 2 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 22 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 123225, MinAC 135, MaxAC 135, MinMR 70, MaxMR 70, MinDC 190, MaxDC 700, Accuracy 30, Agility 24, FireResistance 3, IceResistance 7, LightningResistance 1, WindResistance 2, HolyResistance 2, DarkResistance 1, HolyAffinity 1, PhysicalResistance 1, BlockChance 10, IgnoreStealth 1 |

### #349 · Bobbit Bobbit

| 字段 | 值 |
|---|---|
| MonsterName | Bobbit Bobbit |
| Image | NewMob2 |
| AI | 125 |
| Level | 250 |
| ViewRange | 2 |
| CoolEye | 44 |
| Experience | 33600000 |
| Undead | false |
| CanPush | false |
| CanTame | false |
| AttackDelay | 700 |
| MoveDelay | 0 |
| IsBoss | false |
| Flag | None |
| FaceImage | 0 |
| MonsterInfoStats | `MonsterInfoStat` × 16 条（明细见 [MonsterInfoStat.md](MonsterInfoStat.md)） |
| Respawns | `RespawnInfo` × 4 条（明细见 [RespawnInfo.md](RespawnInfo.md)） |
| Drops | `DropInfo` × 62 条（明细见 [DropInfo.md](DropInfo.md)） |
| Stats | Health 1620000, MinAC 150, MaxAC 150, MinMR 105, MaxMR 105, MinDC 1333, MaxDC 3000, Accuracy 46, Agility 30, FireResistance -1, IceResistance -1, LightningResistance -1, WindResistance -1, HolyResistance -1, DarkResistance -1, PhantomResistance -1 |

### #350 · Sabuk Flag

| 字段 | 值 |
|---|---|
| MonsterName | Sabuk Flag |
| Image | CastleFlag |
| AI | 1001 |
| Level | 250 |
| ViewRange | 5 |
| CoolEye | 100 |
| Experience | 0 |
| Undead | false |
| CanPush | true |
| CanTame | false |
| AttackDelay | 5000 |
| MoveDelay | 800 |
| IsBoss | true |
| Flag | CastleObjective |
| FaceImage | 0 |

