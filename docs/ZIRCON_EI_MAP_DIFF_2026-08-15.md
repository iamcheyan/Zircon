# Zircon 原版独有地图清单（Zircon 有、EI 没有）

> 生成日期：2026-08-15
> 数据源（三方互验一致）：
> - 原版 Zircon：`NAS/TMP/zircon-backup-20260811-095139/System-server-original.db`（SystemDbProbe 正规解析，244 行 MapInfo）
> - 现行 EI：`Zircon/Debug/ServerCore/Database/System.db`（dbeditor workspace 导出，627 行）
> - 历史文档：`Mir3-Research/docs/map-migration-comparison.md`（2026-08-11 迁移当日删除表）

## 结论速览

| 口径 | 数量 | 说明 |
|---|---|---|
| Zircon 原版 MapInfo | 244 | NAS 备份 DB，英文 Description |
| 现行 EI MapInfo | 627 | 英雄杀对齐后 |
| 两边共有 FileName | 75 | 其中 16 张 Description 被 EI 改写挪用（见 §3） |
| **Zircon 有、EI 没有（DB 注册口径）** | **169** | 即本清单；全部 169 的 .map 文件至今仍在 `Debug/Client/Map/` 磁盘上 |
| Zircon 磁盘遗留、原版 DB 也未注册 | 14 | 见 §2 |
| EI 有、Zircon 没有 | 552 | B 系/kt 系/中文图等 |
| 历史文档口径「Zircon 独有 199」 | 199 | 2026-08-11 迁移当日口径（基准为当时 544 行 EI 库），含 169 + 14 遗留 + 当时的交集改写图 |

历史背景：2026-08-11 单日完成 EI↔Zircon 对调（备份→EI 544 覆盖→英雄杀对齐 627）。
Zircon 独有图的 **DB 行被删、.map 文件留盘**——所以它们现在都出现在 mapviewer 的
「🗑️ 未使用」分类里（576 张的组成部分）。原版全量备份在
`NAS/TMP/zircon-backup-20260811-095139/`（258 .map + 原版 DB + 原版图库 + MiniMap），
未拆封权威源 `Debug/Client.7z`（LOMCN 2017）。

> **2026-08-15 补齐（当日已修正）**：先从 NAS 拷回 18 张 `<原名>_Z.map`。随后全量 MD5
> 比对发现：§3 表那 16 张**盘上文件本来就是原版内容**——EI 迁移只改写了 MapInfo 描述文字，
> 故 16 张 `_Z` 副本实为重复，已移入 `Debug/Map-duplicates-20260815/` 隔离区（见下）。
> 真正内容被换、`_Z` 副本独特而留盘的只有 **`D201_Z`（原版废矿1层）** 和
> **`D3102_Z`（原版空名预留图）**。结论不变：**两个版本的全部地图内容都完整在盘，合集闭环**。

> **2026-08-15 去重**：全量 MD5 去重，未注册重复 48 个 stem（含上述 16 张 `_Z` 副本）
> 移入 `Debug/Map-duplicates-20260815/`（manifest.tsv 记录对应正主与原因），已注册地图
> 86 组 241 份内部重复一律未动。此后 **778 个文件 = 537 种内容，未注册文件全部唯一**；
> 未使用分类 546（未登记 151 + 已登记不可达 395）。

## 1. 169 张注册地图完整清单（原版英文名）

### 城镇/野外（14 + ER51_Ice）
| FileName | 原版名 | FileName | 原版名 |
|---|---|---|---|
| 10 | Bichon Castle | 17 | Lost Oasis |
| 11 | Taoist Temple | 18 | Arid Flats |
| 13 | Banyo Island | 19 | Lost Village |
| 16 | Western Arids | 7 | Infernal Island |
| E01 / E02 | North Way | E11 / E12 | South Way |
| GM | GM Map | ER51_Ice | Lost Land 3 |

（15 为原版空名预留图）

### 城内建筑/子区（16）
| FileName | 原版名 | FileName | 原版名 |
|---|---|---|---|
| 11_002 | Weapon Shop | 16_001 | Beyond Shore |
| 11_003 | Potion Merchant | 16_002 | Western Coast |
| 11_004 | Armor Shop | 16_003 | Western Pass |
| 11_005 | Misc Item Vendor | 19_1 | Lost Pass |
| 14_000 | Assassin's Hideout | 3_000 | Sabuk Guild Territory |
| 8_001 | Holy Palace Ent old | 5_000 | Desert Guild Territory |

（15_001–15_003 为空名预留）

### 洞穴/地牢（139）
按副本家族分组（完整机器可读清单：`docs/zircon_only_maps_2026-08-15.txt`，169 行）；最后一列 `file-kept` = .map 至今仍在客户端磁盘：

- **Bichon Cave（比奇矿洞）**: D101 Lv1, D102 Lv2, D103 Lv3 —— 对应现在 AGENTS.md 里失效的 `@move D101`
- **Banya Temple（潘夜神殿）**: D1005–D1009 (Lv5–8), D10101/D10102 (Lv9 W/E), D1103 (Zuma Lv3), D1106 (Zuma)
- **Lost Paradise Cave**: D111–D113
- **Jinchon Palace（真天宫）**: D1200, D12011–14 (Lv2 W/S/E/N), D12021–24 (Lv3 W/S/E/N), D12033, D12041/42, D1206
- **Banya Cave（废矿）**: D121 Lv1, D122 Lv2, D123 Lv3 —— `@move D201`（EI 版废矿1层）在原版里对应的是这族
- **Black Palace**: D13021/D13022
- **幽灵船**: D1406 Flight Deck
- **Numa Ruins（诺玛遗迹）**: D1501, D1502, D15031–34, D1504, D1505
- **Numa 野区**: D1601 Hill, D1602/D1603 Valley, D1604 Stronghold
- **Purgatory**: D1802
- **Desert Dungeon**: D2001, D20011, D20012
- **Underground City**: D2002, D20021–23
- **Underground Mine**: D2003, D20031, D20032
- **Desert Royal Chamber**: D2004
- **Deserted Mine**: D201 Lv1（同名 EI 图已换成别的内容）
- **Frost Dungeon（冰封地牢）**: D2101–D2104, D21051–56 (Lv5 ×6), D2106, D2107
- **Frost Holy Palace**: D22021, D22022, D22031, D22032
- **Goru Cave**: D2301–D2304
- **Hyunmoon Temple（玄门寺院）**: D2401–D2404
- **Departed Valley**: D2501–D2503
- **Banyo Cave**: D2601
- **The Lair（巢穴）**: D2904, D29052, D2906, D2907
- **Dragon Abyss（龙渊）**: D3001–D3006, D3005_BH/CR/HM/JJ（四变体）, D3003 OLD
- **Flea Cave**: D301–D303
- **预留**: D3103, D3106, D3901–D3906（空名）
- **Lost Land**: D3400, D3400_1
- **Southern 系**: D4000–D4003, D4101, D4102
- **Carved Stone Tomb（石刻墓碑）**: D702–D705
- **Red Moon Valley（赤月山谷）**: D902–D905
- **The Wall（长城）**: ID3_014, ID3_024
- **Quartz Mine**: ID7_000–ID7_004
- **废弃小镇/修道院**: ID9_00, ID9_01, ID9_02

## 2. 文件级补充：14 张未注册遗留（在磁盘、原版 DB 也没有行）

00（未收录大地图 1360×1500）、11_001、D012_1–D012_6（天然洞穴 1–6）、D1506、
D29031、D29032、GM_001、Ithuejingot、Ithuejingot_WaitR。
（258 备份文件 − 244 注册行 = 14；这些图在原版 Zircon 里本来就是没接入世界的死资源）

## 3. 75 张交集图中被 EI 改写挪用的 16 张

| FileName | Zircon 原版名 | EI 现名 |
|---|---|---|
| 14 | Toxic Docks | 月河城 |
| 8_002 | Holy Palace Ent old | 绝情塔口 |
| D006 / D007 | Lava Area 1 / 2 | 邪恶之地①② |
| D008 | Dragon Abyss Ent | 深度雪原 |
| D009 | Toxic Lands | 月河渊 |
| D2201 / D2204 / D2205 | Frost Holy Palace 1/3/Queen's | 绝情塔1层/2层/绝情宫殿 |
| D2900 | The Lair Entrance | 邪恶之地 |
| D2901 / D2902 | The Lair Lv1/Lv2 | 会员练级/BOSS集中营 |
| D29051 | The Lair Lv4 West | 玛法.四象 |
| D3005 | Dragon Abyss Lv4 | 龙穴生死堂 |
| D3101 / D3102 | （空） | 卧龙寺/卧龙宫 |

即：这些文件名还在，但地图内容与原版已完全不是一回事。

## 4. 文件内容现状（MD5 逐一比对）

- 258 张原版 .map 中 197 张在当前客户端**内容未变**；61 张被改/换（59 张属交集图被 EI 版同名覆盖，2 张 Zircon-only 被改：`3_000.map` Sabuk Guild Territory、`D201.map` Deserted Mine Lv 1——沙巴克/废矿调整所致）
- 若要恢复原版任一张：从 `NAS/TMP/zircon-backup-20260811-095139/Map/` 拷回 + 用 dbeditor 补 MapInfo 行即可（.map 格式两版完全一致，无需转换）

## 4.1 原版小地图补齐（2026-08-15）

原版小地图图库 = NAS 备份 `MiniMap-original.Zl`（旧版 v0 DXT1 格式，537 帧），
帧号记录在原版 MapInfo 的 `MiniMap` 字段。EI 客户端图库 `Data/MiniMap.Zl` 是 ZL2 v2
（PNG 帧），格式不同不能直接搬字节，已解码→PNG→按 ZL2 规范追加：

- **未使用图 137 帧**（139 个 stem 对应）：追加后帧号 287–423，mapviewer 索引已登记
- **已注册但小地图空白的 16 图**（§3 挪用表 + `0_000` 市政厅）：追加帧 424–439，
  并经 dbeditor 写入双库 `MapInfo.MiniMap` 字段（同步成功，round-trip 校验通过，
  备份 `Backup/System/System 2026-08-15 17-06.db.gz`）
- 游戏内实测：`@move D006` 邪恶之地① 右上角小地图显示原版熔岩地形；`0_000` 市政厅
  显示室内布局——链路（原库→MiniMap.Zl→System.db→客户端）全通
- 原文件备份：`Data/MiniMap.Zl.bak-20260815`（同步前 287 帧原版）

注：这 16 图的 .map 文件本就是原版内容（§补齐说明），所以小地图画面与实际地形吻合。

## 5. 相关文档索引

- 对调全史：`Mir3-Research/docs/map-data-migration-investigation.md`、`map-migration-comparison.md`、`map-library-architecture.md`
- 199 张逐张详解：`Mir3-Research/docs/zircon-unique-content-details.md`
- 英雄杀二次对齐（+83→627）：`Zircon/docs/EI_ALIGNMENT_2026-08-11.md`
- 格式结论（.map 完全一致）：`Zircon/docs/MAP_FORMAT_COMPARISON.md`
- 备份位置：`NAS/TMP/zircon-backup-20260811-095139/`（原版）、`ei-state-20260811-1438/`（EI 态）
