# 研究报告：洞穴/废矿地面贴图异常

> **日期**：2026-08-09  
> **状态**：根因已定位；修复方案待选型  
> **读者**：需要自行研究资源与客户端的同事  
> **相关实现**：`GodotClient`（重置客户端）与原版 `Client` **共用** `Debug/Client/Data`、`Debug/Client/Map`

本文把「废矿/僵尸洞看起来像熔岩、地砖发黑/看不见」的完整排查结论、证据与建议方案写清楚，方便你离线研究。

---

## 0. 一句话结论

| 问题 | 结论 |
|------|------|
| 是不是 Godot 把库画错了？ | **基本不是。** 地图引用与 `KROrder` 映射正确。 |
| 是不是你单独下错了资源？ | **不是。** 本地 `Tiles5c` 与 LOMCN 当前 **patch** 的 MD5 完全一致；2019 官方 `Client.7z` 里帧 20–24 **同样是黑的**。 |
| 主因是什么？ | **官方 `Tiles5c.Zl` 中，被废矿大面积使用的帧 20–24 是纯黑空帧**（资源内容坏/空）。 |
| 次因是什么？ | Godot 地图绘制还有两处未对齐原版（`OffSet`、`BackImage==0`），会加重缺砖与错位，但解决不了黑帧本身。 |

**地图要画「矿洞地砖」→ 指到 `Tiles5c[20–24]` → 资源里这几帧是黑的 → 屏幕上大块黑/灰地。**

---

## 1. 现象（玩家侧）

### 1.1 观感

- 进入比奇附近的「僵尸洞」后，地面不像经典矿洞碎石/泥土地，偏黑，有时像熔岩/暗洞。
- 视野里有大量**实心灰/黑矩形**，部分地砖「看不见」。
- 墙体、石柱、怪物、角色一般仍可辨认。

### 1.2 复现入口

| 项 | 值 |
|----|-----|
| 典型坐标 | `(54, 287)`（截图） |
| 周围怪物 | GhostSorcerer、Voracious Ghost、Corpse Raising Ghost、Cave Maggot |
| 地图文件 | `Debug/Client/Map/D201.map` |
| 数据库名 | Deserted Mine Lv 1（#136） |
| 中文习惯名 | 废矿 1 层 / 比奇僵尸洞 |
| 同系列 | `D202`、`D203`（废矿 2/3 层） |

### 1.3 易混地图（不要搞错）

| 系列 | 文件 | 名称 | 备注 |
|------|------|------|------|
| 比奇**矿** | `D101`–`D103` | Bichon Cave | 矿洞，不是僵尸洞 |
| 比奇**废矿/僵尸洞** | `D201`–`D203` | Deserted Mine | 本次问题地图 |
| 跳蚤洞等 | `D301` 等 | 多用 `Dungeonsc` 等 | 另有 OffSet 类问题，见文末 |

---

## 2. 客户端地图贴图是怎么工作的（背景）

理解链路后，才能判断「错在哪一环」。

### 2.1 三层结构

每张 `.map` 大致两段数据：

1. **背景层 Back**（半分辨率）：只在偶数格 `(x*2, y*2)` 存 `BackFile` + `BackImage`  
2. **中/前景 Middle / Front**（全分辨率）：每格 14 字节，含 file、image（存盘时 +1，绘制时 -1）、动画、光、阻挡等

绘制顺序（原版）：

1. 可选全屏 Background 图  
2. **FLayer（地面层）**：Back + 部分标准尺寸 Middle/Front  
3. **DrawObjects**：按行画 Middle → Front → 对象 → 特效  

### 2.2 图库索引 `KROrder`

`.map` 里的 file 字节（0–71 一带）经 `LibraryCore/Libraries.cs` 的 `KROrder` 映射到 `LibraryFile`，再映射到 `Data\Map Data\...\.Zl`。

与本次相关的映射：

| file 字节 | LibraryFile | 路径 |
|-----------|-------------|------|
| 0 | Tilesc | `Data/Map Data/Tilesc.Zl` |
| 2 | **Tiles5c** | `Data/Map Data/Tiles5c.Zl` |
| 6 | Dungeonsc | `Data/Map Data/Dungeonsc.Zl` |
| 17 | Wood_Tiles5c | `Data/Map Data/Wood/Tiles5c.Zl` |
| 21 | Wood_Dungeonsc | `Data/Map Data/Wood/Dungeonsc.Zl` |

- **Back 层允许 file=0（Tilesc）**  
- **Middle/Front 绘制时跳过 Tilesc**（原版与 Godot 一致）

### 2.3 原版绘制是否使用图库 OffSet？

原版 `MapControl` 地图绘制一律：

```csharp
library.Draw(index, x, y, Color.White, /* useOffSet */ false, 1F, ImageType.Image);
```

即：**地图层不使用** ZL 帧里的 `OffSetX/Y`。  
角色/怪物绘制一般 `useOffSet=true`，与地图不同。

---

## 3. D201 实际引用了什么

### 3.1 整图 Back 层统计

| 引用 | 格子数（约） | 占比 |
|------|--------------|------|
| `Tiles5c` 帧 **20–24** | 18667 | **~61%** |
| `Wood_Tiles5c` 帧 **0–4** | 11958 | ~39% |
| 其中 `Wood_Tiles5c` 帧 **0** | 2390 | （含在上项内） |

中层以 **`Wood_Dungeonsc`（file=21）** 为主（墙/柱），内容正常。

### 3.2 `(54, 287)` 附近样例

- 地面：`Tiles5c[20–24]` 与 `Wood_Tiles5c[0–4]` 混铺  
- 墙：`Wood_Dungeonsc` 多帧  

**没有**引用「熔岩专用」库；视觉像熔岩 = 大块黑地 + 岩石墙 + 黑夜光照 + 棕色土夹杂。

---

## 4. 根因 A（主因）：`Tiles5c[20–24]` 是纯黑资源

### 4.1 文件指纹

| 文件 | 大小 | MD5 |
|------|------|-----|
| 本地 `Debug/Client/Data/Map Data/Tiles5c.Zl` | 10 673 688 | `286b1220005970c6d511e0a9599f0d11` |
| LOMCN patch `Data-Map Data-Tiles5c.Zl`（解压后） | 同上 | **相同** |
| 官方 `Client.7z` 内 `Data/Map Data/Tiles5c.Zl`（约 2017 资源） | 10 478 577 | `7585c6e3f3e85306e7448c70aa8a6694` |

本地 `Tiles5c` 元数据：version=0（DXT1），标称 20000 帧，**实际有像素数据约 3440 帧**。

### 4.2 帧内容抽检

| 帧 | 内容 | 平均亮度（约） |
|----|------|----------------|
| 0–4 | 青/水色纹理 | 高 |
| **15–19** | **正常棕岩石**（像矿洞地面） | 中高 |
| **20–24** | **近乎纯黑** | ~1.2 |
| 25–26 | 正常深色岩石 | 中 |
| 35+ | 草地等 | — |

帧 20 的 DXT 块头（本地与 2019 Client.7z **相同**）：

```text
01 00 00 08 54 55 55 55 00 00 00 08 55 55 55 55
```

这是「颜色端点接近黑、整块铺开」的数据，**不是解码器把正常图解坏了**。

### 4.3 全游戏引用规模

对全部 `Debug/Client/Map/*.map` 统计 `BackFile==2`（Tiles5c）时：

- 引用量最高的正是帧 **20–24**（合计约 **186 万格**）  
- 有纹理的 15–19、25+ 引用少得多  

→ 坏帧恰恰是**用得最多的地砖**。

### 4.4 解码样张（已落盘）

目录：`docs/research/tile-probe-d201/`

| 文件 | 说明 |
|------|------|
| `t5_15.png` … `t5_19.png` | 正常岩石 |
| `t5_20.png` | 黑块（问题帧） |
| `t5_25.png` 等 | 正常深岩 |
| `wood_t5_dxt5_0.png` 等 | 正常棕色矿土 |

### 4.5 官方资源对照结论

| 对比 | 结果 |
|------|------|
| 本地 vs LOMCN **patch** | MD5 **完全一致** → 不是本机单独下错 patch |
| 本地 vs 2019 **Client.7z** | 文件不同（patch 多约 63 帧），但 **20–24 两边都黑、头字节相同** |
| `how-to.txt` | 写明轻量上传不全，应用 Launcher/Client Data；`Client.7z` 是 **2019 基线**，不是「最新」 |

**结论：至少从 2017–2019 官方 Client Data 起，`Tiles5c[20–24]` 就是空黑帧。这是上游资源与地图数据的长期不一致，不是 Godot 独有 bug。**

本地已保存：

```text
Debug/Client.7z                          # 官方 2019 完整包
Debug/Client.7z.md5                      # 0837bfb278e354a2940c426639553aba
Debug/client7z-tiles-compare/Tiles5c.Zl  # 从包内抽出的对照副本
```

官方资源站：

- <https://files.lomcn.co.uk/resources/mir3/zircon/>
- patch 地砖：`patch/Data-Map Data-Tiles5c.Zl.gz`

---

## 5. 根因 B（次因 / 加重）：Godot 绘制未完全对齐原版

即使修好 `Tiles5c`，下列差异仍会让地面/墙体与原版错位或漏画。

### 5.1 地图层错误使用了 `OffSet`（应修）

| | 原版 | Godot `MapView.DrawCell` |
|--|------|---------------------------|
| 地图 Back/Middle/Front | `useOffSet=false` | **始终** `px+OffSetX, py+OffSetY` |

地图库常见 `OffSet = (-24, -16)`（半格）。影响：

- 相对角色偏约半格  
- 个别库（如部分 `Dungeonsc`）存在极大 OffSet 时，墙体会飞出视野 → 空洞  

角色/怪物用 OffSet 是正确的；**仅地图三层应关闭 OffSet。**

代码位置：

- 原版：`Client/Scenes/Views/MapControl.cs`（`DrawObjects` / `Floor.OnClearTexture`）  
- Godot：`GodotClient/Scripts/MapView.cs` 约 `Rect2 dest = new Rect2(px + img.OffSetX, y + img.OffSetY, ...)`

### 5.2 `BackImage == 0` 被错误跳过（应修）

| | 原版 | Godot |
|--|------|-------|
| BackImage=0 | **会画** index 0 | `BackImage <= 0` **跳过** |

`D201` 上 `Wood_Tiles5c` **index 0 有 2390 格**（合法棕色矿土）。Godot 整批不画 → 额外「缺地砖」。

代码：`MapView._Draw` 中 `cell.BackImage <= 0` 的 continue。

### 5.3 已对齐、可忽略的部分

| 项目 | 状态 |
|------|------|
| `.map` 解析（半分辨率 Back、Middle/Front +1/-1） | 与原版一致 |
| 动画编码（低 4 位帧数、`0x80` blend） | 一致 |
| Middle/Front 跳过 Tilesc | 一致 |
| Middle 标准格不走 Blend | 已对齐 |

---

## 6. 问题分层总表

| 层级 | 是否有问题 | 说明 |
|------|------------|------|
| 地图 `.map` 引用 | 引用「坏帧」 | 逻辑上指向 20–24，但 20–24 内容为空 |
| `KROrder` / 路径 | 正常 | 没有选错库 |
| `Tiles5c.Zl` 内容 | **异常** | 20–24 纯黑；15–19/25 正常 |
| 官方 patch / Client.7z | **同样异常** | 非本机独有 |
| Godot OffSet | 异常 | 加重错位 |
| Godot BackImage==0 | 异常 | 漏画合法 0 号砖 |
| 墙体 Wood_Dungeonsc | 正常 | 所以洞壁还像洞 |

---

## 7. 建议的解决方案（按推荐顺序）

下面几条可以**并行研究**，不必只选一条。标注了优先级与风险。

---

### 方案 A — 修 Godot 绘制对齐原版（低风险、必做）

**做什么**

1. `MapView.DrawCell`：地图 Back/Middle/Front **不要加** `OffSetX/Y`（对齐 `useOffSet=false`）。  
2. 背景层：允许 `BackImage == 0`；仅在库缺失、空元数据帧、纹理解码失败时跳过。

**预期效果**

- 角色与地砖对齐改善  
- `Wood_Tiles5c[0]` 等 0 号砖重新出现  
- **不能**让 `Tiles5c[20–24]` 变出纹理（资源仍是黑的）

**验证**

- `D201 (54,287)`：棕色土是否补全  
- 城镇 `0`/`1`：角色脚底是否还偏半格  
- `D301`/`D401`：异常 OffSet 墙体是否归位  

**涉及文件**

- `GodotClient/Scripts/MapView.cs`

---

### 方案 B — 用原版 WinForms 客户端 + 同一 Data 做对照（确认用，优先做）

**做什么**

1. 用原版 `Client.exe`，Data/Map 指向同一套 `Debug/Client/Data`、`Debug/Client/Map`。  
2. 进 `D201` 同一坐标截图。

**如何解读**

| 原版表现 | 含义 |
|----------|------|
| 同样大块黑地 | 100% 确认资源问题；Godot 不是主因 |
| 原版正常、Godot 黑 | 重新查 Godot 解码/索引（当前证据不支持此方向） |

**成本**：低；**价值**：极高（给自己/社区定论）。

---

### 方案 C — 从「健康」的原始地图库重导 `Tiles5c`（治本，推荐研究）

**思路**

Zircon 的 `.Zl` 多由更早格式（WTL / Lib / 其它编辑器）转换而来。若转换时 20–24 被写成空黑，而原始库仍有纹理，应重导。

**建议步骤**

1. 在 LOMCN / 其它 Mir3 资源源寻找 **Tiles5c 原始库**（名称可能是 `Tiles5c`、`Tile5`、韩文客户端 Map 目录下同名文件）。  
2. 用仓库内 `LibraryEditor` / `ImageManager` / 历史转换脚本导入，导出新的 `Tiles5c.Zl`。  
3. 检查帧 20–24 是否有正常岩石纹理。  
4. 替换 `Debug/Client/Data/Map Data/Tiles5c.Zl`，进 `D201` 验证。  
5. 记录新文件 MD5，更新文档。

**风险**

- 找不到完整原始库  
- 帧顺序与 Zircon 地图索引不一致 → 全图错砖  
- 必须对多张图做抽样验证（`D201`、`D101`、城镇等）

**仓库内可参考**

- `LibraryEditor/`  
- `ImageManager/`  
- `docs/RENDERING_PORT_GUIDE.md`（ZL 格式、KROrder）  
- `Tools/WtlToZl.py`（若适用）

---

### 方案 D — 在资源侧「修补」黑帧（务实折中）

若短期找不到原始库，可在 **资源文件** 上做手术，而不是改客户端逻辑硬映射。

**思路示例**

1. 用工具打开 `Tiles5c.Zl`，把帧 **20–24** 的像素数据替换为相邻正常帧（如 **15–19** 或 **25–29** 的变体）。  
2. 或从 `Wood/Tiles5c` 的泥土色系做色调接近的填充（视觉上统一矿洞）。  
3. 保持帧数量与索引不变，避免改 `.map`。

**优点**：不改地图、不改代码映射；一改全局所有引用 20–24 的图受益。  
**缺点**：非「真·原版像素」；需要美术/观感验收；最好备份原 ZL。

**不建议**：在 Godot 里写死 `if index in 20..24: index = 15`——掩盖数据错误，难维护。

---

### 方案 E — 改地图 `.map` 引用（不推荐作首选）

把 `D201` 等图中 `BackFile=2, BackImage∈[20,24]` 批量改成引用有纹理的帧。

**缺点**

- 地图多、引用广（全服约 186 万格）  
- 易漏  
- 治标不治本（别的工具/编辑器仍会假定 20–24 是标准地砖）

仅在无法动 ZL、又要快速出演示时考虑。

---

### 方案 F — 社区/上游求证

- LOMCN 论坛 Zircon 区：是否已知 `Tiles5c` 缺帧  
- 对比其它运营服/私服客户端 Data 的 `Tiles5c.Zl` MD5 与帧 20–24 内容  
- 若有人持有「完整」库，直接 MD5 + 抽帧对比最快  

资源目录：

- <https://files.lomcn.co.uk/resources/mir3/zircon/>  
- 论坛：Zircon Source / Mir3 Files 相关帖（`how-to.txt` 指向 Jamie 发布）

---

## 8. 建议的研究与修复路线图

```text
第 1 天
  ├─ 方案 B：原版客户端同 Data 进 D201 截图对照（定论）
  └─ 方案 A：修 MapView OffSet + BackImage==0（代码小改）

第 2–3 天
  ├─ 从 Client.7z / patch 以外的来源找 Tiles5c 原始库（方案 C）
  ├─ 对候选库抽帧 15–30，看 20–24 是否有纹理
  └─ 有好源则重导 ZL，替换后回归 D201/D101/城镇

若无好源
  └─ 方案 D：备份后修补 ZL 黑帧（用 15–19/25–29 作模板）
```

**验收清单**

- [ ] `D201 (54,287)`：无大块纯黑/灰矩形，地面呈矿洞岩石/泥土  
- [ ] `Wood_Tiles5c[0]` 区域可见  
- [ ] 角色站位与地砖网格对齐  
- [ ] 城镇与其它洞穴无明显新回归  
- [ ] 新 `Tiles5c.Zl` MD5 已记录  

---

## 9. 相关代码与文件索引

| 用途 | 路径 |
|------|------|
| 原版地图绘制 | `Client/Scenes/Views/MapControl.cs` |
| Godot 地图绘制 | `GodotClient/Scripts/MapView.cs` |
| Godot 地图读取 | `GodotClient/Formats/MapReader.cs` |
| KROrder / LibraryList | `LibraryCore/Libraries.cs` |
| 问题地图 | `Debug/Client/Map/D201.map`（及 D202/D203） |
| 问题地砖库 | `Debug/Client/Data/Map Data/Tiles5c.Zl` |
| 正常棕色土 | `Debug/Client/Data/Map Data/Wood/Tiles5c.Zl` |
| 正常墙体 | `Debug/Client/Data/Map Data/Wood/Dungeonsc.Zl` |
| 解码样张 | `docs/research/tile-probe-d201/` |
| 官方 Client 包 | `Debug/Client.7z` |
| 包内对照 Tiles5c | `Debug/client7z-tiles-compare/Tiles5c.Zl` |
| 早期问题笔记 | `docs/MAP_TILE_DESERTED_MINE_BUG.md`（可与本文对照） |

---

## 10. 自助复现命令（研究用）

### 10.1 看 D201 某格引用

```bash
# 需本机 python3；仅示意：解析 (54,287) 附近 Back
python3 - <<'PY'
import struct
from pathlib import Path
data = Path('Debug/Client/Map/D201.map').read_bytes()
w,h = struct.unpack_from('<hh', data, 22)
print('size', w, h)
# Back 层从偏移 28 开始，半分辨率
off = 28
cx, cy = 54, 287
for x in range(w//2):
    for y in range(h//2):
        bf = data[off]; off += 1
        bi = struct.unpack_from('<H', data, off)[0]; off += 2
        if abs(x*2-cx)<=4 and abs(y*2-cy)<=4:
            print(f'({x*2},{y*2}) BackFile={bf} BackImage={bi}')
PY
```

### 10.2 对比本地与 patch 的 MD5

```bash
md5sum "Debug/Client/Data/Map Data/Tiles5c.Zl"
# 远程 patch 解压后应等于：
# 286b1220005970c6d511e0a9599f0d11
```

### 10.3 从已下载的 Client.7z 抽 Tiles5c

```bash
7z x -y Debug/Client.7z "Data/Map Data/Tiles5c.Zl" -o/tmp/tiles5c-from-client7z
md5sum "/tmp/tiles5c-from-client7z/Data/Map Data/Tiles5c.Zl"
# 期望：7585c6e3f3e85306e7448c70aa8a6694
```

---

## 11. 附录：其它洞穴上的相关注意点

| 地图 | 现象线索 | 与本文关系 |
|------|----------|------------|
| `D201`–`D203` 废矿 | 大面积 `Tiles5c[20–24]` 黑帧 | **本文主战场** |
| `D101` 等比奇矿 | 也用 Tiles5c / Wood_Tiles5c | 若引用 20–24 同样会黑 |
| `D301` 跳蚤洞、`D401` 蚂蚁洞 | 多用 `Dungeonsc`，部分帧 OffSet 异常 | 主要吃 **方案 A（关 OffSet）**；黑帧问题次要 |
| 城镇 `0`/`1` | 多用 Tilesc | 一般不受 20–24 影响；可作 OffSet 对齐回归基准 |

---

## 12. 最终结论（给决策用）

1. **资源侧**：官方 Zircon 客户端 `Tiles5c.Zl` 的 **20–24 帧是空黑**，而废矿等地图大面积使用这些帧 → **资源与地图不匹配，属于上游数据问题**。  
2. **下载侧**：你们当前 Data **没有**单独下错 patch；2019 `Client.7z` 也救不了 20–24。  
3. **客户端侧**：Godot 还有 OffSet / `BackImage==0` 应对齐，属于**次要但应修**。  
4. **推荐路径**：先 A+B 定论与对齐 → 再 C 找原始库重导 → 不行则 D 修补黑帧。

若你研究后找到「20–24 有纹理」的 `Tiles5c` 来源，优先做 MD5 + 抽帧对比，再决定是否全量替换。

---

## 13. 决定性结论（2026-08-09 素材三方比对，回答"熔岩溶洞 vs 矿区"）

### 13.1 比对对象（用户澄清）
不比对修改记录，而是直接比对**正在使用的素材文件**：
- 本地 Zircon：`Debug/Client/Data/Map Data/Tiles5c.Zl`（DXT1, ver0, count=20000, 3440 帧有数据, MD5 `286b1220005970c6d511e0a9599f0d11`）
- 官方 mir3asia 韩服：`/home/tetsuya/NAS/传奇私服/mir3asia/Data/Tiles5c.wtl`（WTL v1.3, 3440 帧, 20000 索引, 10,840,352 B, MD5 `f1cd64a79ec8bcc18bf648612fc240ff`）
- 官方 2017 Client.7z：`/tmp/zircon-client7z-extract/Data/Map Data/Tiles5c.Zl`（MD5 `7585c6e3f3e85306e7448c70aa8a6694`）

### 13.2 WTL v1.3 帧数据布局（修正先前理解）
- 魔数 `"ILIB v1.3-WEMADE"` 在 0x02；0x00 = uint16 version=2；0x18 = frame_count；0x1C = index_count；0x20 起 = 4B/条 offset 表。
- 每帧：`w,h,offX,offY`（各 int16）在帧内 +0；**数据区从 +24 起，结构为 `[768B DXT1][8B flag]` × N**（flag 是 `0818…` 占位块），末尾可能有余数。
- **本地 Zl 帧数据 = 官方 WTL 去掉 8B flag 后的纯 768×4 = 3072B DXT1**；帧索引稀疏（0–199 仅 55 帧，其余在 10000–16278）。

### 13.3 像素级比对结果（3062 共同帧）
| 类别 | 帧数 |
|------|------|
| 字节完全一致 | 1368 |
| 字节不同但像素逐点一致（重编码） | 1335 |
| 轻微差异（<5% 像素，编码噪声） | 339 |
| 明显差异（5–13%，最差帧 12445 差 796/6144） | 20 |

→ **本地 Tiles5c.Zl 是官方 WTL 的忠实拷贝（视觉近无损），无内容被改。**

### 13.4 矿洞地面关键帧（本地 == 官方2017 == 韩服WTL）
| 帧 | avg 色 | 观感 |
|----|--------|------|
| Tiles5c[0] | (43,178,179) | 青草地 |
| Tiles5c[15–19] | (131–150, 112–128, 88–99) | 棕岩石 |
| **Tiles5c[20–24]** | **(7,0,0)** | **近纯黑（暗红）** |
| Tiles5c[25–26] | (66,57,47) | 深棕 |
| Wood/Tiles5c[0–4]（DXT5） | (82–86, 76–79, 62–65) | 棕矿土 |

- **官方 2017 客户端 Zl 帧 20–24 同样是 (7,0,0)**——黑帧从 2017 官方包起就是如此，不是 Zircon 移植产生，也不是后来被改。
- 本地 `Wood/Tiles5c.Zl`（DXT5）解码修正后与官方 `Wood/Tiles5c.wtl`（DXT1）帧 0–4 像素一致（之前"近黑"结论是 Python DXT5 颜色索引误用 3-bit 的解码 bug；BC3 颜色索引为 2-bit/像素）。

### 13.5 地图与渲染逻辑比对
- **官方 mir3asia `Map/D101.map` MD5 == 本地 `Debug/Client/Map/D101.map`** = `8dc4bc72f7f02ecff916eed2871ccb78`（D102/D103/D201 同目录存在）。
- D101 Back 层：file=17(Wood/Tiles5c) img=0–4 共 5204 格 + file=2(Tiles5c) img=20–24 共 4796 格。
- **原版 `Client/Scenes/Views/MapControl.cs`（约 1493–1516 行）Back 层 = `KROrder[BackFile] → library.Draw(BackImage)`，无场景重定向、无 BackImage==0 检查**；Middle/Front 有 `file != LibraryFile.Tilesc` 特判但不影响 D101（Middle=21/255）。
- f34795b 让 Godot 端对齐原版：允许 `BackImage==0`（Wood/Tiles5c[0] 的 1015 格可画）+ 去 OffSet。

### 13.6 最终判定（回答用户）
1. **文件未被修改、贴图未贴错**：本地 Zl/地图 = 官方（像素级/MD5 级一致，2017 官方包与 2025 韩服 WTL 交叉验证）。
2. **"熔岩溶洞"观感 = 官方资源本身**：矿洞地面 44% 是官方黑帧 Tiles5c[20–24]（近纯黑 (7,0,0)）+ 56% 棕矿土 Wood/Tiles5c[0–4]，原版客户端直接绘制，无任何版本故意改动。
3. **"矿区"（全棕）画面需要黑帧不显示或 file=2→Wood 重定向，原版没有这种逻辑**；若用户记忆中历史画面是全棕矿区，其来源应是其他私服版本的地图/资源（当前两份官方资源均为黑帧），或修复前 `BackImage==0` 漏画造成的画面差异。
