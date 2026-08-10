# 矿区（僵尸洞）地图对比：Zircon vs NAS 原版 EI传奇3.0

> 日期：2026-08-11
> 结论：**NAS 原版的僵尸洞地面同样是黑帧**——"熔岩/黑色矿区"是官方资源的本来样貌，
> 不是 Zircon 改的，也不是 Zircon 独有的解码 bug。

## 1. 地图身份

比奇僵尸洞 = **Deserted Mine（废矿）**，地图文件：

| 版本 | 地图文件 | 尺寸 | 大小 |
|------|----------|------|------|
| Zircon | `Debug/Client/Map/D201.map` | 350×350 | 1,806,903 |
| Zircon | `Debug/Client/Map/D202.map` | 350×350 | 1,806,903 |
| Zircon | `Debug/Client/Map/D203.map` | 350×350 | 1,806,903 |
| NAS | `D2011.map` | 100×100 | 147,528 |
| NAS | `D2012.map` | 200×200 | 590,028 |
| NAS | `D202.map` | 300×300 | 1,327,528 |
| NAS | `D203.map` | 300×300 | 1,327,528 |

**地图布局不同**：Zircon 的三层废矿都是 350×350 大图；NAS 拆成 1层(D2011/D2012)、
2层、3层 不同尺寸。文件 MD5 也不同（D202: `9caf95ce…` vs `4bb50302…`）。
这是**不同游戏版本的地图数据**，不是同一张图的两份拷贝。

## 2. 地面贴图引用（像素级验证）

### 各版本矿区地图引用的图库与帧号

| 地图 | BackFile | KROrder 库 | 帧号 | 帧内容（实测） |
|------|----------|-----------|------|---------------|
| Zircon D201 | 2 | Tiles5c | **20–24** | **纯黑 (8,0,0)**，100% 暗像素 |
| Zircon D201 | 17 | Wood_Tiles5c | 0–4 | 正常棕矿土（约 39% 格子） |
| NAS D2011/D2012 | 2 | Tiles5c | **11950–11954** | **纯黑 (8,0,0)**，100% 暗像素 |
| NAS D202 | 1 | Tiles30c | 0–4, 305–309 | **正常沙色** (195,178,137)，0% 暗 |
| NAS D203 | 2 | Tiles5c | 11950–11954, 13125 | 黑帧为主 |
| Zircon D202 | 2 | Tiles5c | 20–24 | 纯黑 (8,0,0) |
| Zircon D202 | 17 | Wood_Tiles5c | 0–4 | 正常棕矿土 |

### 帧内容三方验证（2026-08-11 实测）

**Zircon 本地 `Tiles5c.Zl`（DXT1 解码）：**
```
frame 15-19: meanRGB ≈ (131-150, 112-128, 88-99) — 正常岩石
frame 20-24: meanRGB = (8,0,0)，100% 暗像素 — 纯黑
frame 25-26: meanRGB ≈ (66,58,47) — 深色岩石
```

**NAS 原版 `Tiles5c.wil`（WIL RLE 解码）：**
```
frame 20-24: meanRGB = (8,0,0)，100% 暗像素 — 与 Zircon .Zl 完全一致
frame 11950-11954: meanRGB = (8,0,0)，100% 暗像素 — NAS 矿区主用帧也是黑
```

**NAS 原版 `Tiles30c.wil`（NAS D202 用）：**
```
frame 0-4:   meanRGB ≈ (193,177,136) — 正常沙色地砖
frame 305-309: meanRGB ≈ (200,177,134) — 正常沙色地砖
```

## 3. 结论

1. **Zircon 的"熔岩/黑色矿区"不是故意改的**：Zircon D201 引用的 Tiles5c 帧 20–24
   在官方资源（2017 Client.7z、当前 LOMCN patch、本地 .Zl）里**本来就是纯黑**
   `(8,0,0)`。帧 15–19/25+ 是有纹理的岩石，但地图几乎不引用它们。

2. **NAS 原版同样黑**：NAS 僵尸洞 D2011/D2012 引用的 Tiles5c 帧 11950–11954
   **同样是纯黑**。用原版 WIL 解码器逐帧确认，不是 Zircon 独有的现象。

3. **唯一差异在 D202**：NAS D202 用 Tiles30c 的**正常沙色地砖**（无黑帧），
   Zircon D202 用 Tiles5c 黑帧。这是地图版本差异（不同版本重新引用帧号），
   不是贴图资源损坏。

4. **"全棕矿区"的记忆不匹配当前官方素材**：当前这套官方资源（Zircon patch +
   韩服 WTL + NAS EI）里，矿区主用地砖帧均为黑。若记忆中的全棕矿区真实存在，
   只可能来自其他私服版本的改图/改引用（详见
   `Mir3-Research/docs/MAP_TILE_DESERTED_MINE_BUG.md` §10 终局裁决）。

## 4. 佐证文档

- `/home/tetsuya/development/Mir3-Research/docs/MAP_TILE_DESERTED_MINE_BUG.md` —
  完整排查记录（2026-08-09）：KROrder 选库正确、本地=官方 patch MD5 一致、
  黑帧是官方 2017/2025 资源本来就有的、原版渲染逻辑无重定向。
- `Tools/common/wilsdk.py` / `zlsdk.py` — 本次验证用的 WIL/ZL 解码器。

## 5. 相关源文件

- `LibraryCore/Libraries.cs:384` — KROrder 映射（file byte → LibraryFile）
- `Debug/Client/Data/Map Data/Tiles5c.Zl` — Zircon 地砖库（帧 20–24 黑）
- `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/Tiles5c.wil` — NAS 原版地砖库（帧 20–24 黑，11950–11954 黑）
- `GodotClient/Formats/MapReader.cs` — 地图解码
- `GodotClient/Scripts/MapView.cs` — 地图渲染（OffSet/BackImage==0 已对齐原版）
