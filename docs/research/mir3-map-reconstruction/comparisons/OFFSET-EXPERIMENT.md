# Mir3 EI 地图层 Offset 三模式实验（反汇编 + 渲染对照）

回答两个问题：**原版 Mir3.exe 是否对地图层（ground/mid/front）帧应用 WIL 帧 offset
（frame+4/+6）？** 以及 **mapviewer 渲染器应如何对齐图层？**

## 结论（evidence 摘要）

- **[confirmed] 原版地图层全部零 offset**：ground/mid/front 三个绘制函数的所有分支
  （普通 blit、blend 浮点路径、动画/选择段）只读帧尺寸 `frameW@+0` / `frameH@+2` 与
  位图数据指针 `srcData@entry+0x3c`，**从不读取 frame+4/+6（offset 字段）**。
- **[confirmed] 原版 actor 层读 offset**：actor 绘制函数（0x430b00）在
  `430aab/430aaf` 处 `movswl 0x4(%eax),%ecx`（off_x）/ `movswl 0x6(%eax),%edx`
  （off_y）并 `add` 进 destX/destY——地图物件不带 offset、角色/怪物带 offset，
  这正是 WIL 帧 offset 字段的用途（供 actor 帧对齐脚底点）。
- **[confirmed] 地图层全部格底/格左锚定**：mid/front 帧底 = `destY + h =
  (y−viewY)·32 − scrollY − 125` = 格底；ground 底 = −157 + 32 = −125 同一条线。
  与 ZL C# `MapControl.drawY`（格底 +1）一致。
- **[confirmed] mapviewer 三模式行为**：`none` = 原版零 offset（格底锚）；
  `all`/`midfront` 在锚点上**加性**应用帧 offset（×scale）。实验显示 `all` 破坏原版
  观感（ground 与物件双重偏移错位）、`midfront` 与 `none` 差异小且协调。
  → 原版观感 = **none（零 offset，格底锚）**。

## 反汇编证据（Mir3.exe）

### 地图层：零 offset

| 函数 | 职责 | blit 点 | offset 读取 |
|---|---|---|---|
| 0x43b440 / 0x43b9a0 / 0x43c330 / 0x43c4c9 | ground | 43b5c2 / 43baf9 / 43c4c9 | 无（destY 常数 −157） |
| 0x43bb10 | mid/front **48×32 专用**（W==0x30 && H==0x20） | 43bce6 → 0x460240；blend 43bcf5 → 0x4542a0 → 0x466800 → 0x4542f0 | 无 |
| 0x43be00 | mid/front **非 48×32**（43bed9 起尺寸门控跳过 48×32） | 43bfd2 → 0x460240 | 无 |

- 0x43bb10 / 0x43be00 的所有分支（选择段、动画段、普通 dest+blit、blend 浮点路径）
  仅使用 `frameW@+0`、`frameH@+2`、`srcData@0x3c`，从不读 frame+4/+6。
- 调用点 41c59a/41c5a5/41c66d/41c678（sel=0 mid、1 front 各 2 次），实参序
  (ecx=this, arg1, arg2, arg3=sel)，`ret $0xc`。
- **dest 公式**：`destX = (x−viewX)·48 − scrollX − 200`，
  `destY = (y−viewY)·32 − scrollY − h − 125` ⇒ 帧底 = 格底（−125 同 ground 线）。
- 0x434a20（原怀疑第三 per-cell 调用）= **选区足迹几何**，非绘制：调 0x434670
  整数坐标助手、fsqrt 半径判定、向 this+0x35b2c0 写 u16 点对（targeting/rangering）。
  0x434670-0x434a1f 区段无任何 blit 调用；0x468520 = fldcw+fistpll 舍入助手。

### actor 层：读 offset（对比证据）

- 0x430b00 内 `430aab: movswl 0x4(%eax),%ecx`（off_x）、
  `430aaf: movswl 0x6(%eax),%edx`（off_y），随后 `add %ecx,%ebx` / `add %edx,%ebp`
  （destX += off_x / destY += off_y；eax = 0x566a40 当前帧结构）。
- 同层另两处 actor 带 offset 绘制调用点：40b583 / 40fb57 / 430b5b。

## 三模式实验

mapviewer `/fullmap` 与 `/tile` 支持 `om` 参数（`none`/`all`/`midfront`）：
- `none`：原版公式，零 offset。
- `all`：ground 与 mid/front 帧 offset 全部加性应用（×scale）。
- `midfront`：仅 mid/front 层应用（ground 保持原版）。

### 全图条带（z=4，三面板 none|all|midfront）

`comparisons/{0,01,0_003,1,123,3,41,5_0013,D10031,D9022}__offset_modes_z4.png`
（10 张，140KB–1.5MB）+ `offset-mode-diff-stats.json`（以 none 为基准，逐像素）。

| map | mean abs (vs all) | %>24 (vs all) | mean abs (vs midfront) | %>24 (vs midfront) |
|---|---|---|---|---|
| 0.map | 20.15 | 0.704 | 14.06 | 0.532 |
| 01.map | 13.60 | 0.453 | 7.76 | 0.300 |
| 0_003.map | 21.01 | 0.628 | 14.65 | 0.397 |
| 1.map | 13.38 | 0.479 | 9.33 | 0.368 |
| 123.map | 16.80 | 0.662 | 12.03 | 0.441 |
| 3.map | 14.15 | 0.469 | 8.60 | 0.306 |
| 41.map | 19.80 | 0.540 | 12.07 | 0.292 |
| 5_0013.map | 4.01 | 0.087 | 2.74 | 0.049 |
| D10031.map | 21.42 | 0.455 | 20.39 | 0.420 |
| D9022.map | 6.45 | 0.241 | 5.38 | 0.184 |

（差值随缩放放大：z=4 时 ×16，模拟器 z=2 时 ×4。）

### 视觉结论（0.map 比奇城条带）

- **中（all）面板中央建筑群破碎/错位**：ground 与 mid/front 物件同时被各自帧
  offset 推动，墙体与地基分离——非原版观感。
- 左（none）与右（midfront）协调且近同 ⇒ 原版 = none；midfront 是原版的
  可用近似。

### 模拟器帧（800×600 模拟器，1024×576 截图）

`comparisons/sim_{map}__none|all|midfront.webp` × 30：每图三种模式同取景
（0.map c=400,400 z=2；01.map c=300,300 z=1；0_003.map c=30,50 z=0；
1.map c=300,300 z=1；123.map c=200,200 z=1；3.map c=200,300 z=1；
41.map c=200,200 z=1；5_0013.map c=34,34 z=0；D10031.map c=150,150 z=0；
D9022.map c=70,70 z=0），HUD/实体/小地图同原版视角集。
模式切换经像素差验证生效（0.map：none-vs-all mean diff 23.7、none-vs-midfront
20.3、all-vs-midfront 9.5 @z2）。

### 洞穴图补充（D1423，黑帧代表）

EI 集合中黑帧最多的地图（29697 格全黑/近黑）在模拟器同取景
（c=200,200 z=1）三模式对比：

| 对比（z=4 全图 diff，800×1200） | mean abs | %>24 |
|---|---|---|
| none vs all | 14.49 | 0.266 |
| none vs midfront | 0.8 | 0.022 |
| mid vs all | 14.42 | 0.265 |

`sim_D1423__{none,all,midfront}.webp`（1024×768）目视：三帧均大范围纯黑洞穴；
all 模式下地面（岩石/铁轨）整体错位破碎，midfront 与 none 几乎一致。
→ 洞穴图同样证实原版 = none。

## 含义

- mapviewer 默认 `om=none` 即原版渲染路径（格底锚 + 零 offset）——与
  ORIGINAL-VIEW-10MAPS.md 的 rect 基准一致。
- WIL 帧 offset 字段（+4/+6）是 actor 层特性；地图层帧的该字段（若非零）在原版
  中不参与绘制，模拟器据此保留字段但默认不应用。
- 3.map / 41.map / D10031.map 的物件库 offset 若非零，均属素材侧遗留，不影响
  原版对齐结论。

## 产物

- `comparisons/*__offset_modes_z4.png` ×10、`offset-mode-diff-stats.json`
- `comparisons/sim_*.map__{none,all,midfront}.webp` ×30
- `comparisons/D201/D202/D203__offset_modes_z4.png`（废弃矿洞 3 图条带，
  目视 midfront 撕裂严重、all 轻微，与 none 结论一致）
- `comparisons/sim_D1423__{none,all,midfront}.webp`（黑帧洞穴图补充帧，1024×768）
- 反汇编文本 `/tmp/mir3_text.txt`（161763 行；grep 仅前 4MB ≈ 行 98000）
- 生成脚本 `/tmp/gen_offset_strips.py`
