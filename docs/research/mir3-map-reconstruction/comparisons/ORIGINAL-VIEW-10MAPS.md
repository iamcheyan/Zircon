# Mir3 EI 原版视角 10 图对照（rect 基准）

基准与证据见 `docs/research/mir3-map-reconstruction/mapviewer-investigation.md`（Mir3.exe
反汇编：rect 投影、cell anchor、v 变换、库表槽号 = v）与 `comparisons/README.md`
（EI WIL vs ZL 数据差异、地面 alpha=4 根因、真实 ZL SmObjectsc = 12586 帧）。

## 方法

每张图产出一组证据：

1. **EI vs ZL 面板** `comparisons/<stem>__ei_vs_zl_z4.png` — 同一 `.map` 分别用
   EI 3.0 客户端 WIL 主题库与 ZL 2017 `Debug/Client/Data/Map Data` 渲染并排。
2. **原版视角模拟帧** `comparisons/sim_<stem>.map.webp` — 800×600 模拟器取景：
   rect 投影、指定格居中、原版 HUD 叠加（底部条 (0,465)-(800,600)）、小地图
   固定区 (672,0)-(800,128)、Mud3 服务端实体（NPC/怪物）叠印。

原版客户端无法在本地运行（Windows/老旧资源环境），对照以反汇编 + 资源证据为准，
不做"与原版视觉一致"的断言；差异均标注证据级别。

## 10 图清单

| map | 尺寸 | MiniMap | 主题库（mid/front 代表） | anomaly | 对照要点 |
|---|---|---|---|---|---|
| 3.map 沙巴克城 | 400×600 | 1018 | wood_smobjectsc 等 | 3255 | 帧越界最多（mid/front lib24/25 OOB） |
| 0.map 比奇城 | 800×800 | 1001 | housesc / smtilesc | 3 | 最大城镇图，地面 3 格未绘制 |
| 41.map 沙漠 | 400×400 | 1021 | sand_tilesc / sand_* | 1619 | 沙漠主题，lib34/40 帧越界 |
| 1.map 失乐园 | 600×600 | 1004 | smtilesc / wood_* | 7 | 实体最密集（NPC/怪物） |
| D9022.map 地牢 | 140×140 | 148 | dungeonsc / furnituresc | 1 | 地下城，基本正常 |
| 0_003.map 房屋 | 60×100 | — | housesc / wallsc | 137 | 室内房屋，地面 137 格未绘制 |
| 5_0013.map 内室 | 68×68 | — | innersc / furnituresc | 67 | 室内，地面 67 格未绘制 |
| 123.map 悬崖 | 400×400 | 1027 | cliffsc / animationsc | 34 | 野外悬崖 + 动画物件 |
| D10031.map 基础 | 300×300 | 103 | object1c / object2c | 62 | 物件库引用越界（lib2 62 格） |
| 01.map 新比奇 | 600×600 | 1002 | smtilesc / wood_* | 0 | 零异常基线图 |

（display = MiniMap.txt 索引；`—` = 不在服务端小地图索引中。）

## 逐图说明

### 3.map 沙巴克城（anomaly 3255 — 全库最高）
- 异常全部为 **lib24/lib25 帧越界**（mid 172+2575、front 8+500 格）：库表槽 24/25 =
  wood_housesc/wood_smobjectsc，EI 库帧数远小于地图引用（lib25 EI 969 帧 vs
  frame_max 2531）。EI 面板这些物件格大量缺失；ZL 面板完整（ZL SmObjectsc 12586 帧）。
- 模拟帧：城墙/房屋/旗帜 NPC 可见，中心取景 (220,200) 为城内。
- [confirmed] 帧越界根因 = EI 素材帧数 < 地图引用；[pending] 原版客户端对越界帧的
  替换逻辑（空帧/首帧/取模）未反汇编确认。

### 0.map 比奇城（anomaly 3）
- 仅 3 格地面未绘制；EI/ZL 面板整体一致。最大城镇图（800×800），sim 帧含
  NPC（啊琨/金氏/肉店老板/老张）与出生点。
- [confirmed] 800×800 rect 全图渲染与 544 图 catalog 完全对齐。

### 41.map 沙漠（anomaly 1619）
- lib34/40（sand_housesc/sand_smobjectsc）帧越界 450+1099（mid）、1+69（front）。
- EI 面板沙漠物件稀疏，ZL 面板完整。

### 1.map 失乐园（anomaly 7）
- 地面 7 格未绘制；实体最密集（NPC 书鬼/铁匠等 + 怪物鸡/牛/猪 Lv10 ×38 群刷点）。

### D9022.map 地牢（anomaly 1）
- 地下城 140×140，dungeonsc/furnituresc，基本无异常——14B 格式小图基准。

### 0_003.map 房屋 / 5_0013.map 内室（anomaly 137/67）
- 全为 ground_not_drawn：室内图地面层大量格未引用任何地面库（14B 格式，非 legacy）。
- [pending] 室内图地面在 EI 客户端是否为 `MapInfo.Background` 静态图（ZL 客户端机制）
  需更多证据。

### 123.map 悬崖（anomaly 34）
- 野外悬崖 cliffsc + 动画物件 animationsc；34 格地面未绘制。

### D10031.map 基础（anomaly 62）
- 唯一 `ground_lib2_frame_oob` 案例：ground lib2（smtilesc 10180 帧）62 格越界。
- [pending] lib2 槽位在 EI 库表 = smtilesc；帧越界原因与 3.map 同类（素材版本）。

### 01.map 新比奇（anomaly 0）
- 零异常基线：600×600，MiniMap 1002，EI/ZL 面板差异仅为素材风格。

## 限制与 pending

- Snow/Forest 主题图全部为 **13B legacy 格式**（如 0_002.map、kt0014.map），
  `parse_map`（14B/格）无法解析，comparison 工具跳过；模拟器亦不渲染。
  共 39 张 legacy 13B（audit 已统计）。[confirmed]
- 原版客户端无法本地运行 → 无法直接对照像素级视觉；全部结论基于反汇编/资源证据
  （见 mapviewer-investigation.md 证据链）。[derived]
- 实体层 NPC 外观用 NPC.wil frame 0 统一样式（body 字段 → NPC.wil 帧块的精确布局
  未反汇编）；怪物用 Mon-1.wil frame 0（怪物名→库/帧映射依赖 monster.dat 专有格式，
  未解析）。[pending]
- 小地图位置框按 地图尺寸/128px 线性映射；EI 客户端 FMMap/MMap.wil 帧为 600×400 /
  300×200 拼接图，帧内地图间留白未逐图校准。[pending]
