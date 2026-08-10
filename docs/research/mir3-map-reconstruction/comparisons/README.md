# EI vs ZL map rendering comparison

Tool: `Tools/render_map_comparison.py` — renders the *same* `.map` file through
the authoritative renderer (`mapviewer`, rect layout = Mir3.exe projection)
with two library sources and stitches side-by-side PNGs:

- **EI 3.0 client** data: WIL theme folders (`Wood/`, `Sand/`, ...)
- **ZL 2017** data: `Debug/Client/Data/Map Data` ZL libraries

Output: `comparisons/<stem>__ei_vs_zl_z<z>.png` (labelled panels, vertical divider).

## How to run

```bash
python3 Tools/render_map_comparison.py '<EI Map dir>' \
    --data-ei '<EI Data dir>' \
    --data-zl '<Zircon>/Debug/Client/Data/Map Data' \
    --maps 3,0,41 --z 4 \
    --out docs/research/mir3-map-reconstruction/comparisons
```

The ZL data dir must be the real 2017 client `Map Data` (root ZLs +
`Wood/`/`Sand/`/`Snow/`/`Forest/` subdirs).  Passing the EI mirror data
(`mir3ei/Data`) produces identical panels — a useless comparison.

## Renderer fix required for fair ZL panels

`mapviewer._sprite_opaque` forces **ground libraries** (`tilesc`,
`tiles30c`, `tiles5c`, `wood_tilesc`, `tiles`) to blit opaque regardless of
the stored alpha.  This matters because the ZL toolchain stores
`Wood/Tilesc.Zl` and `Wood/Tiles5c.Zl` BC3 alpha as a constant **4**
(placeholder), not 255.  The 2017 ZL client never notices: it draws ground
from `MapInfo.Background` (static image) and skips per-tile `Tilesc` blits
(`Client/Scenes/Views/MapControl.cs` `DrawObjects` guards
`file != LibraryFile.Tilesc`).  Our per-tile ground renderer would
alpha-composite alpha=4 frames to ~invisible, wiping the ZL panel's ground
layer; the forced-opaque path reproduces what both clients show.  EI WIL
ground frames are alpha=255, so the fix is a no-op there.

## Findings (z4, 3.map / 41.map / 0.map)

1. **No systematic hole in either panel.**  Exact canvas-background pixels are
   0 in both panels for all three maps (z4).  Every map renders fully under
   both data sets.

2. **ZL panels are slightly darker overall — artwork difference, not
   missing frames.**  Dark fraction (mean RGB < 120) at z4:
   `3.map` EI 0.0603 / ZL 0.0770, `0.map` EI 0.0884 / ZL 0.0950,
   `41.map` EI 0.0165 / ZL 0.0173.  ZL sprites exist for the same frame ids
   but are a different, marginally darker artwork generation.

3. **Real resource shortfall is in the EI *data*, hidden by layering.**
   `3.map` mid file 25 -> `wood_smobjectsc`: EI `Wood/SmObjectsc.wil` has 969
   frames while the map's `frame_max` is 2531 — 2575 mid + 500 front cells
   decode to `None` under EI data (audit `map-audit.json`, anomaly total
   3255).  ZL `Wood/SmObjectsc.Zl` has 12586 frames, so the ZL panel renders
   those cells.  Visually the missing EI sprites are masked because the cells
   sit above ground/neighbour sprites (per-cell: only 289 of 2575 OOB cells
   are measurably brighter under EI).  Similarly 41.map: file 34
   `sand_housesc` (EI 1274 vs map frame_max 1752) and file 40
   `sand_smobjectsc` (EI 631 vs map frame_max 3618) — 1619 OOB cells.

4. **Library-frame table** (`ei-vs-zl-libraries.json`): EI is the *smaller*
   side for every differing lib except wood_smobjectsc/wood_wallsc where ZL
   is vastly larger (12586/7531 vs 969/3791).  For housesc/cliffsc etc. the
   counts are close (9010 vs 14607, 7619 vs 7915) and EI renders fully.

## Interpretation for map reconstruction

- EI client data + EI maps: objects with frame > EI lib count silently
  vanish (decode None) but are covered by ground/other sprites — the map
  "renders" without obvious holes.
- ZL data is a *superset* for object libs (smobjectsc 12586 vs 969,
  wallsc 7531 vs 3791) but its frames are a different, often darker,
  artwork generation.
- Neither data set alone is "the original look": EI is the authentic frame
  numbering/art for 3.map's objects within frame range; ZL fills the
  out-of-range cells with different art.  Cross-referencing both is required
  to reconstruct a map faithfully (see audit + catalog stages).

## 800×600 simulator (原版视角)

```bash
python3 Tools/mapviewer.py '/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Map' \
    --data '/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data' \
    --catalog docs/research/mir3-map-reconstruction/catalog \
    --envir /home/tetsuya/NAS/TMP/Mud3/Envir \
    --port 8766
```

- `/` 地图浏览器（catalog 面板、网格、格坐标、frame 越界警告）
- `/sim#sim=3.map&c=220,200&z=3` 800×600 模拟器：rect 投影取景、方向键/WASD 移动、
  Ctrl+滚轮缩放、T 小地图 128/256、H 切 HUD；实体 = Mud3 服务端数据
  （StartPoint.txt 出生点 / Merchant.txt NPC / MonGen.txt→Mon_Def/*.gen 怪物），
  悬停显示信息、点击设目标（黄框 + HUD 读数）；小地图黄框 = 玩家位置。
- 原版布局证据：HUD 底部条 (0,465)-(800,600)、小地图固定 (672,0)-(800,128)
  （`docs/research/ei-ui-layout/layout.json`，records=29）。

10 张代表性地图的原版视角帧见 `sim_*.map.webp`，逐图说明见
`ORIGINAL-VIEW-10MAPS.md`。

## 数据驱动（本轮新增）

- **图层独立开关**：模拟器与浏览器均提供 Back/Middle/Front 三个复选框，
  缓存键与 /fullmap 文件均含 g/m/f；模拟器 hash 不持久化图层位
  （`om` 仍持久化）。
- **`/api/cell?map=&x=&y=`**：逐格返回 `{flag, anim[a,b], back/mid/front{file,lib,frame}}`；
  模拟器悬停任意格子显示该 JSON（60ms 防抖）。实测 0.map 格 400,400
  flag=15、back=tilesc f9633；D1423 格 200,202 back=tiles5c f24（黑帧）。
- **`/api/strip?map=&z=&g=&m=&f=`**：三模式（none/all/midfront）对比条带 PNG
  （z 缩放、400px 高缩略、带标签条），模拟器“导出对比图”按钮新标签页打开。
- **HUD 证据级**：`证据 confirmed/derived` + 三层库计数 + 动画格数 +
  越界警告 + 目标实体，全部来自 catalog。
- **地图选择器缩略图**：`/thumb?map=` 预渲染全图缩略（`/tmp/wiki_thumbs`）。
- **怪物掉落**：Envir `MonItems/*.txt`（280 文件，GBK，`1/N 物品 [数量]`）解析后
  挂到怪物实体，点击 tooltip 显示前 5 条掉落（实测 半兽勇士 金币×4000(1/1) …）；
  93/98 个 .gen 怪物名匹配到掉落文件（4 个未匹配列档 P11）。
- **补充证据帧**：`sim_D1423__{none,all,midfront}.webp`（1024×768）——
  黑帧最多（29697 格）的 EI 洞穴图；none 帧 black-frac 0.204/mean 33.4，
  all/midfront 与 none 的 diff 见 `offset-mode-diff-stats.json`（D1423 行）。
