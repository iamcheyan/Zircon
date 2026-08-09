# 矿洞/废矿地面贴图异常：问题记录与排查

> 日期：2026-08-09  
> 状态：已定位（资源 + 客户端绘制双重问题）  
> 现象入口：比奇附近僵尸洞 / 废矿（`D201` 等），地面发黑、灰块、像熔岩/缺地砖  
> 相关客户端：`GodotClient`（重置端）与原版 `Client` 共用同一套 `Debug/Client/Data`

---

## 1. 现象

玩家报告（截图 + 口述）：

1. 进入比奇县附近的**僵尸洞**后，地面不像经典**矿洞地砖**，更像偏黑/熔岩感；
2. 视野内有大量**实心灰/黑矩形**，部分地砖“看不见”；
3. 墙体、怪物、角色尚可辨认。

截图示例坐标：`(54, 287)`，周围怪物为 `GhostSorcerer` / `Voracious Ghost` / `Corpse Raising Ghost` / `Cave Maggot`。

---

## 2. 地图身份确认

| 项目 | 值 |
|------|-----|
| 数据库地图 | `Deserted Mine Lv 1`（#136） |
| 中文习惯称呼 | 废矿 1 层 / 比奇僵尸洞 |
| 地图文件 | `Debug/Client/Map/D201.map` |
| 尺寸 | 350×350 |
| 同系列 | `D202`、`D203`（废矿 2/3 层），同样以幽灵/洞蛆为主 |

说明：比奇**矿洞**是 `D101–D103`（Bichon Cave）；**僵尸洞/废矿**是 `D201–D203`。本次截图坐标与怪物刷新均指向 **D201**。

---

## 3. 该图实际引用的贴图（不是“选错熔岩库”）

在 `(54, 287)` 附近及整图统计：

| 层 | `.map` 中的 file 字节 | `Libraries.KROrder` | 磁盘路径 | 帧号 | 角色 |
|----|----------------------|---------------------|----------|------|------|
| Back 地面 | `2` | `Tiles5c` | `Data/Map Data/Tiles5c.Zl` | **20–24**（主用量） | 应是矿洞地砖 |
| Back 地面 | `17` | `Wood_Tiles5c` | `Data/Map Data/Wood/Tiles5c.Zl` | **0–4** | 泥土/碎石 |
| Middle 墙体 | `21` | `Wood_Dungeonsc` | `Data/Map Data/Wood/Dungeonsc.Zl` | 多帧 | 岩石墙/柱 |

`D201` 整图 Back 层比例（约）：

- `Tiles5c[20–24]`：**约 61%**（18667 格）
- `Wood_Tiles5c[0–4]`：**约 39%**（11958 格，其中 index **0** 有 **2390** 格）
- 其它 Back：0

结论：

- **KROrder / 库选择正确**，没有把废矿指到独立“熔岩”库；
- 墙体库 `Wood_Dungeonsc` 解码为正常岩石，不是问题主因；
- 视觉错误集中在 **地面 Back 层**，尤其是 **`Tiles5c` 帧 20–24**。

---

## 4. 根因 A（主因）：`Tiles5c.Zl` 中高频帧为纯黑

### 4.1 文件指纹（本地）

| 文件 | 大小 | MD5 |
|------|------|-----|
| `Debug/Client/Data/Map Data/Tiles5c.Zl` | 10 673 688 | `286b1220005970c6d511e0a9599f0d11` |
| `Debug/Client/Data/Map Data/Wood/Tiles5c.Zl` | 370 208 | `1eb9d5284c78fe35e8ad94079f4df9fa` |

`Tiles5c.Zl` 元数据：version=0，标称 20000 帧，**实际有数据 3440 帧**。

### 4.2 帧内容抽检（DXT1 解包）

| 帧 | 观感 | 备注 |
|----|------|------|
| 0–4 | 青/水色 | 有完整纹理 |
| 15–19 | 正常棕岩石 | 像矿洞地面 |
| **20–24** | **近乎纯黑** | DXT 端点即为黑；**不是解码器算错** |
| 25–26 | 深色岩石纹理 | 正常 |
| 35+ | 草地等其它地貌 | 正常 |

二进制特征（帧 20 头 16 字节）：

```text
01 00 00 08 54 55 55 55 00 00 00 08 55 55 55 55
```

即 565 色接近黑、索引铺满——资源位本身就是黑块。

### 4.3 全图引用与黑帧重合度

对全部 `Debug/Client/Map/*.map` 统计 `BackFile==2`（Tiles5c）时：

- 引用量最高的帧正是 **20–24**（合计约 **186 万格**）；
- 这些帧在当前 `Tiles5c.Zl` 里几乎全是黑；
- 相邻有纹理的 15–19 / 25+ **几乎不被地图引用**。

因此游戏会大面积画出“黑地砖”：在黑夜光照下变成灰黑矩形，棕色 `Wood_Tiles5c` 夹杂其中 → 像熔岩/烂洞，而不是完整矿洞。

### 4.4 与 LOMCN 官方资源对比（2026-08-09）

资源站：

- 目录：<https://files.lomcn.co.uk/resources/mir3/zircon>
- 说明：`how-to.txt` 要求用 Zircon Launcher / Client Data 装到 `Debug/Client`
- 分包补丁：`patch/Data-Map Data-Tiles5c.Zl.gz`、`patch/Data-Map Data-Wood-Tiles5c.Zl.gz`

实测：

```text
远程 patch 解压后 Tiles5c.Zl  MD5 = 286b1220005970c6d511e0a9599f0d11
本地 Tiles5c.Zl               MD5 = 286b1220005970c6d511e0a9599f0d11
→ IDENTICAL

远程 patch 解压后 Wood/Tiles5c MD5 = 1eb9d5284c78fe35e8ad94079f4df9fa
本地 Wood/Tiles5c              MD5 = 1eb9d5284c78fe35e8ad94079f4df9fa
→ IDENTICAL
```

**结论：当前本地 `Tiles5c` / `Wood/Tiles5c` 与 LOMCN Zircon 站点上的 patch 包完全一致，不是“本机单独下错了 patch 文件”。**

仍待核实：

1. 完整包 `Client.7z`（约 1.6 GB，2019-07）内的 `Tiles5c.Zl` 是否与 patch 相同；  
2. 是否存在更早/其它来源的完整地砖库（WTL/Lib 原版）在转换进 ZL 时把 20–24 弄丢；  
3. 原版 WinForms 客户端用**同一 Data** 进 `D201` 是否同样发黑（预期：若资源相同，原版也会黑——用于排除“仅 Godot 解码错”）。

完整客户端包入口：

| 资源 | URL | 备注 |
|------|-----|------|
| 完整 Client | <https://files.lomcn.co.uk/resources/mir3/zircon/Client.7z> | ~1.6 GB；**见下节：官方但非最新** |
| 依赖/运行库 | <https://files.lomcn.co.uk/resources/mir3/zircon/ZirconClientDependencies.zip> | |
| 地图相关 patch 中的 Tiles5c | <https://files.lomcn.co.uk/resources/mir3/zircon/patch/Data-Map%20Data-Tiles5c.Zl.gz> | 已与本地一致 |
| Wood Tiles5c patch | <https://files.lomcn.co.uk/resources/mir3/zircon/patch/Data-Map%20Data-Wood-Tiles5c.Zl.gz> | 已与本地一致 |
| Korean Maps | <https://files.lomcn.co.uk/resources/mir3/zircon/Korean%20Maps.7z> | 地图侧，不是地砖 ZL |

### 4.5 `Client.7z` 是否“官方最新客户端”？（2026-08-09 确认）

**结论：是 LOMCN Zircon 官方目录里的官方 Client 包，但不是最新的客户端数据。**

依据（目录：`https://files.lomcn.co.uk/resources/mir3/zircon/`）：

| 项 | 事实 |
|----|------|
| 来源 | 官方资源树 `resources/mir3/zircon/`，Jamie 开源发布配套 |
| `Client.7z` 修改时间 | **2019-07-04**（约 1 644 796 KB） |
| `how-to.txt` | 同日（2019-07-04）；明确写轻量上传不全，**请用 Zircon Launcher 拉 Client Data** |
| `patch/` 目录 | 持续更新，最近可见 **2026-07-18**；含大量 `Data-*.Zl.gz`（含 Map Data） |
| `ZirconClientDependencies.zip` | 2020-02-04 |
| `Database.7z` | 2024-02-24 |

含义：

1. **`Client.7z` = 2019 年基线完整客户端**（适合当“最初官方快照”对照，不能当“现在线上最新贴图”）。  
2. **真正较新的资源在 `patch/`**（按文件增量补丁更新 Data）。  
3. 我们已经比对过：本地 `Tiles5c.Zl` **等于** 当前 `patch` 里的 `Data-Map Data-Tiles5c.Zl`——说明本地数据至少已经跟到 patch 链路，而不是只停留在 2019 基线；**黑帧 20–24 在“当前 patch 版”里就存在**。  
4. 仍应用 `Client.7z` 做一次抽取对照：确认 2019 基线里 `Tiles5c` 是否同样是黑帧，还是后来 patch 弄坏的。

本地已下载（2026-08-09）：

```text
/home/tetsuya/development/Zircon/Debug/Client.7z
/home/tetsuya/development/Zircon/Debug/Client.7z.md5
```

| 项 | 值 |
|----|-----|
| 大小 | 1 644 796 742 字节（与服务器 Content-Length 一致） |
| MD5 | `0837bfb278e354a2940c426639553aba` |
| 来源 URL | `https://files.lomcn.co.uk/resources/mir3/zircon/Client.7z` |
| 服务器 Last-Modified | 2019-07-04 |

### 4.6 已完成：从 `Client.7z` 抽出 `Tiles5c` 对比（2026-08-09）

| 来源 | 大小 | MD5 | present 帧数 |
|------|------|-----|--------------|
| `Client.7z` → `Data/Map Data/Tiles5c.Zl`（包内日期约 2017-08） | 10 478 577 | `7585c6e3f3e85306e7448c70aa8a6694` | 3377 |
| 本地 / 当前 patch | 10 673 688 | `286b1220005970c6d511e0a9599f0d11` | 3440 |

文件**整体不同**（patch 多了约 63 帧），但关键帧 **20–24 在两边都是纯黑**，且 DXT 头字节完全相同：

```text
01 00 00 08 54 55 55 55 00 00 00 08 55 55 55 55
avgLum ≈ 1.2
```

**结论：黑地砖不是“后来 patch 弄坏”或“你单独下错了最新 patch”。至少从 2017–2019 官方 Client 基线起，`Tiles5c[20–24]` 就是空黑帧。**  
废矿地图却大量引用这些帧——这是上游资源/地图与图库长期不一致的问题，不是 Godot 独有。

探针截图目录（本地解码样张）：

- `docs/research/tile-probe-d201/`  
  - `t5_15.png` … `t5_19.png`：正常岩石  
  - `t5_20.png`：黑块  
  - `t5_25.png` 等：正常深岩  
  - `wood_t5_dxt5_0.png` 等：正常棕色矿土  

---

## 5. 根因 B（加重）：Godot 地图绘制与原版不一致

即使修好 `Tiles5c`，下列差异仍会让地面/墙体与原版错位或漏画。

### 5.1 地图层错误使用了图库 `OffSet`（高优先级）

原版 `Client/Scenes/Views/MapControl.cs` 地图绘制一律：

```csharp
library.Draw(..., useOffSet: false, ...);
```

适用于 Back / Middle / Front。

Godot `GodotClient/Scripts/MapView.cs` 的 `DrawCell` 始终：

```csharp
Rect2 dest = new Rect2(px + img.OffSetX, y + img.OffSetY, img.Width, img.Height);
```

地图库常见 `OffSet = (-24, -16)`。结果：

- 相对角色偏约半格；
- 部分帧若 OffSet 异常（如 `Dungeonsc` 中出现极大偏移），会直接飞出视野 → 空洞。

角色/怪物层使用 OffSet 是正确的；**仅地图三层应对齐 `useOffSet=false`**。

### 5.2 `BackImage == 0` 被错误跳过

Godot：

```csharp
if (cell.BackImage <= 0) continue;
```

原版 FLayer：**不判断** `BackImage > 0`，index 0 会画。

`D201` 上 `Wood_Tiles5c` **index 0 共 2390 格**（合法棕色矿土）。Godot 整批跳过 → 额外“缺地砖”。

### 5.3 其它（次要 / 已大致对齐）

| 项目 | 状态 |
|------|------|
| `.map` 解析（半分辨率 Back、Middle/Front +1/-1、动画 0x0F/0x80） | 与原版一致 |
| Middle/Front 跳过 `Tilesc` | 一致 |
| Middle 标准格不走 Blend | 已对齐 |
| FLayer 与 DrawObjects 双绘路径 | Godot 只走主路径；标准 48×32 通常等价 |

---

## 6. 问题分层总结

| 层级 | 是否“下错资源” | 说明 |
|------|----------------|------|
| `KROrder` / 地图引用 | 否 | 指向正确库与帧号 |
| 本地 vs LOMCN **patch** `Tiles5c` | **否，MD5 完全一致** | 不是 patch 下错 |
| `Tiles5c` 帧 20–24 内容 | **资源本身坏/空** | 官方 patch 也是黑帧；可能是上游转换/打包问题 |
| Godot 绘制 | **有代码 bug** | OffSet、`BackImage==0` |

“像熔岩”的观感 = **大面积黑地砖 + 正常岩石墙 + 黑夜光照 + 棕色土夹杂**，不是 KROrder 选到了熔岩图集。

---

## 7. 修复计划

### 7.1 客户端（代码，必做）

1. `MapView.DrawCell`：地图 Back/Middle/Front **不使用** `OffSetX/Y`（对齐 `useOffSet=false`）。  
2. 背景层允许 `BackImage == 0`；仅在库缺失、空帧、纹理失败时跳过。  
3. 回归：`D201`、`D101`、城镇 `0/1`；关注角色站位与地砖对齐、废矿黑块是否减少（资源未修前黑帧仍在）。

### 7.2 资源（数据，治本）

1. 用原版 WinForms 客户端 + **同一** `Debug/Client/Data` 进 `D201` 截图对照。  
   - 若同样黑 → 确认纯资源问题。  
2. 从 `Client.7z` 单独抽出 `Data/Map Data/Tiles5c.Zl`，与当前 MD5 对比。  
3. 若完整包也是黑帧：  
   - 寻找原始 WTL/Lib 地图库重新转换；或  
   - 向 LOMCN/Zircon 社区确认是否已知缺帧；  
   - **不要**在客户端把 20–24 硬映射到 15–19 当长期方案（掩盖数据错误）。  
4. 修好后替换本地 `Tiles5c.Zl`，并记录新 MD5。

### 7.3 验证清单

- [ ] Godot `D201 (54,287)`：无大块灰黑矩形、棕色土 index0 可见  
- [ ] 原版同资源对照截图  
- [ ] 远程 `Client.7z` 与 patch 的 `Tiles5c` MD5 是否不同  
- [ ] 跳蚤洞 `D301` / 蚂蚁洞 `D401` 等依赖 `Dungeonsc` 的地图：OffSet 修复后墙体是否归位  

---

## 8. 关键代码与文件路径

| 用途 | 路径 |
|------|------|
| 原版地图绘制 | `Client/Scenes/Views/MapControl.cs`（`DrawObjects` / `Floor.OnClearTexture`） |
| Godot 地图绘制 | `GodotClient/Scripts/MapView.cs` |
| Godot 地图读取 | `GodotClient/Formats/MapReader.cs` |
| KROrder | `LibraryCore/Libraries.cs` |
| 问题地图 | `Debug/Client/Map/D201.map`（及 D202/D203） |
| 问题地砖库 | `Debug/Client/Data/Map Data/Tiles5c.Zl` |
| 正常棕色土 | `Debug/Client/Data/Map Data/Wood/Tiles5c.Zl` |
| 墙体 | `Debug/Client/Data/Map Data/Wood/Dungeonsc.Zl` |
| 解码样张 | `docs/research/tile-probe-d201/` |

---

## 9. 一句话结论

**废矿僵尸洞的“错贴图”：地图逻辑指向正确，但官方包内 `Tiles5c` 的主用地砖帧 20–24 是纯黑；本地与 LOMCN patch 一致，不像单独下错 patch。Godot 另有地图 OffSet 与 `BackImage==0` 漏画，会加重缺砖和错位。先修客户端绘制，再从完整 Client 包/原库追查 `Tiles5c` 真源。**

---

## 10. 终局裁决（2026-08-09，素材三方比对后）

**结论：不是故意改的，也不是贴图贴错 —— 矿洞"熔岩溶洞"画面就是官方资源与官方地图的本来样貌。**

### 证据链（全部实测）
1. **本地 = 官方**：`Tiles5c.Zl`(本地) vs `Tiles5c.wtl`(韩服 mir3asia) 3062 共同帧像素级 98–100% 一致；差异仅为 DXT1 重编码噪声（1368 字节一致 + 1335 像素一致 + 339 轻微 + 20 帧 ≤13%）。本地 `Wood/Tiles5c.Zl` 解码修正后与官方帧 0–4 逐点一致。
2. **黑帧是官方的**：官方 2017 `Client.7z` 的 `Tiles5c.Zl` 帧 20–24 同样是 (7,0,0) 近纯黑；韩服 WTL 帧 20 字节级 = 本地帧 20（剥离 8B flag 后）。
3. **地图是官方的**：官方 mir3asia `Map/D101.map` 与本地 **MD5 相同**（`8dc4bc72…`）；D101–D103/D201 地面引用 file=2[20–24] + file=17[0–4]，墙 file=21。
4. **渲染逻辑无改动**：原版 `MapControl.cs` Back 层直接 `KROrder[BackFile] → Draw(BackImage)`，无场景重定向；Godot 端 f34795b 修复（BackImage==0 可画 + 去 OffSet）反而是向原版对齐。
5. **"熔岩"来源**：D101 全图 44% 像素 = (8,0,0)，即 Tiles5c[20–24] 官方暗红黑帧；混合 Wood 棕矿土帧后呈现"焦黑矿洞/熔岩溶洞"观感。

### 对历史"矿区"记忆的解释
官方资源两处（2017 官方包 + 2025 韩服）黑帧 20–24 都存在，原版无重定向——所以"全棕矿区"画面不属于当前这套官方素材。若记忆中确有全棕矿区，来源只能是：其他私服版本的改图/地图引用，或历史版本 Godot 端 `BackImage==0` 漏画造成的差异画面。当前 Zircon 用官方文件渲染出的就是官方画面。

### 遗留（无法在 ARM64 Linux 无 wine 下验证）
官方客户端实际亮度/伽马渲染差异（A/B 原版客户端）——仅影响观感亮度，不影响"素材内容一致"的结论。
