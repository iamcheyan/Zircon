# 地图文件格式对比：Zircon vs NAS EI传奇3.0

## 结论

| 维度 | Zircon (Godot 客户端) | NAS (原版 EI 传奇3.0) | 是否一致 |
|---|---|---|---|
| **.map 文件格式** | 22字节头 + Width/Height + 背景层(半分辨率) + 全分辨率单元格(14字节/格) | 完全相同 | ✅ 完全一致 |
| **file byte → 图库映射** | `Libraries.KROrder` (LibraryCore 共享代码) | 同一份 `KROrder` | ✅ 完全一致 |
| **图库文件格式** | `.Zl` (Zircon 自定义压缩格式, Dxt1/Dxt5/BC7) | `.wil/.wix` (Wemade 原始格式) | ❌ 不同 |
| **图库文件组织** | `Data/Map Data/*.Zl` + `Forest/` `Sand/` `Snow/` `Wood/` 子目录 | `Data/*.wil` + `Forest/` `Sand/` `Snow/` `Wood/` 子目录 | ✅ 结构一致 |
| **地图内容** | 0.map = 350×350, 1.8MB | 0.map = 800×800, 9.4MB | ❌ 不同版本 |
| **贴图解码方法** | `ZlReader` (CPU 解码 DXT→BGRA→Godot ImageTexture) | 原 Mir3 客户端 GPU 直接采样 WIL 纹理 | ❌ 不同 |
| **地图渲染方法** | `MapView.cs` + `MapTerrainRow.cs` (Godot Node2D + DrawTextureRect) | 原 Mir3 `MapControl.cs` (DirectX/SharpDX) | ❌ 不同引擎 |

## 详细分析

### 1. .map 文件格式（完全一致）

两个客户端使用**完全相同的 .map 二进制格式**。`MapReader.cs`（移植自原版
`Client/Scenes/Views/MapControl.cs:484-545`）能无修改地读取两种地图。

**格式结构：**
```
偏移 0-21:  22字节头部（跳过）
偏移 22-23: Width (Int16, 小端)
偏移 24-25: Height (Int16, 小端)
偏移 26-27: 2字节（跳过）
偏移 28+:   背景层 (Width/2 × Height/2 × 3字节: 1 byte backFile + 2 bytes backImage)
            全分辨率层 (Width × Height × 14字节/格)
```

**每格 14 字节布局：**
```
byte 0: flag (阻挡标志)
byte 1: middleAnimationFrame
byte 2: frontAnimationFrame (255→0, &0x8F)
byte 3: frontFile (图库索引)
byte 4: middleFile (图库索引)
bytes 5-6: middleImage (+1)
bytes 7-8: frontImage (+1)
bytes 9-11: 跳过
byte 12: light (低4位 ×2)
byte 13: 跳过
```

**文件大小验证：**
- Zircon 0.map: 350×350 → 28 + 175×175×3 + 350×350×14 = 1,806,903 bytes ✅
- NAS 0.map: 800×800 → 28 + 400×400×3 + 800×800×14 = 9,440,028 bytes ✅

两者文件大小与格式计算**精确匹配**，确认格式完全一致。

### 2. 图库索引映射（完全一致）

地图 cell 中的 `backFile`/`middleFile`/`frontFile` 字节通过
`Libraries.KROrder`（`LibraryCore/Libraries.cs:384`）映射到 `LibraryFile`
枚举值。这个映射表在**两个客户端共享的 LibraryCore 中**，完全相同：

```
file byte 0  → LibraryFile.Tilesc      → Data\Map Data\Tilesc.Zl (或 .wil)
file byte 1  → LibraryFile.Tiles30c     → Data\Map Data\Tiles30c.Zl
file byte 5  → LibraryFile.Cliffsc     → Data\Map Data\Cliffsc.Zl
file byte 10 → LibraryFile.SmObjectsc  → Data\Map Data\SmObjectsc.Zl
file byte 15 → LibraryFile.Wood_Tilesc → Data\Map Data\Wood\Tilesc.Zl
file byte 30 → LibraryFile.Sand_Tilesc → Data\Map Data\Sand\Tilesc.Zl
file byte 45 → LibraryFile.Snow_Tilesc → Data\Map Data\Snow\Tilesc.Zl
...
```

两个客户端的地图文件引用的 file byte 值都在这张表里，映射到同一套图库名称。

### 3. 图库文件格式（不同）

**Zircon：`.Zl` 格式**
- 自定义二进制容器（支持旧格式 version 0/1 和 ZL2 压缩容器 version 2）
- 帧数据用 Dxt1/Dxt5/BC7 压缩，CPU 解码后上传 Godot `ImageTexture`
- 解码器：`GodotClient/Formats/ZlReader.cs`
- 位置：`Debug/Client/Data/Map Data/*.Zl`（62 个文件，含 Forest/Sand/Snow/Wood 子目录）

**NAS：`.wil/.wix` 格式**
- Wemade 原始图库格式（.wil = 图片数据, .wix = 索引）
- 原版客户端用 GPU 直接采样 WIL 纹理
- 位置：`Data/*.wil` + `Forest/` `Sand/` `Snow/` `Wood/` 子目录

Zircon 的 .Zl 文件是从 .wil 文件转换来的（相同的图片内容，不同的压缩容器）。
`ZlReader.cs` 注释明确写着"移植自 RenderingCore/Library/MirLibrary.cs"，
原版 MirLibrary 既能读 .wil 也能读 .Zl。

### 4. 贴图渲染方法（不同引擎，相同逻辑）

**Zircon Godot 客户端：**
- `MapView.cs` → `MapTerrainRow.cs`（按行渲染，Godot `Node2D._Draw` + `DrawTextureRectRegion`）
- 背景层用 `LibraryFile.Background` 绘制全屏背景图
- 地形层用 `KROrder` 映射 file byte → LibraryFile → `.Zl` 库 → `ZlReader.GetImageTexture(index)` → Godot 纹理
- 等距坐标：`CellToScreen(x, y)` 把地图格坐标转为屏幕等距坐标

**原版客户端：**
- `Client/Scenes/Views/MapControl.cs` → DirectX/SharpDX 直接绘制
- 同样按行渲染，同样的 file byte → LibraryFile → `.wil` 库映射
- 同样的等距坐标系

渲染逻辑（层级、行排序、等距投影）一致，只是图形 API 不同
（Godot DrawTextureRectRegion vs DirectX DrawTexturedPrimitive）。

### 5. 地图内容差异

同一张地图（如 0.map = 比奇城/Bichon Town）在两个版本中内容不同：

| 属性 | Zircon | NAS |
|---|---|---|
| 0.map 尺寸 | 350×350 | 800×800 |
| 0.map 大小 | 1.8MB | 9.4MB |
| 地图文件数 | 258 | 544 |
| 引用的图库 file byte | 0,1,5,10,255 | 0,1,2,10,15,255 |

NAS 版本是更大、更完整的版本（更多地图、更大尺寸），Zircon 版本可能是
精简版或不同服务端配套的版本。

## 能否通用？

### .map 文件：可以直接互换读取

格式完全一致，`MapReader.cs` 能无修改地读取两个来源的 .map 文件。
把 NAS 的 .map 文件复制到 `Debug/Client/Map/` 即可被 Godot 客户端读取。

### 能否正确渲染：取决于 .Zl 图库内容

NAS .map 文件引用的 tile 索引指向 .Zl 图库中的帧。如果：

1. **对应 .Zl 文件存在** → Zircon 有 62 个地形 .Zl 文件（含 Forest/Sand/Snow/Wood
   子目录），覆盖 KROrder 表中的主要索引。✅

2. **tile 索引在 .Zl 库中存在且有相同内容** → 这取决于 .Zl 是否从相同版本的
   .wil 转换而来。如果 NAS 的 .wil 和 Zircon 的 .Zl 来自同一游戏版本，
   tile 内容一致；如果来自不同版本，同一索引可能对应不同的贴图。

3. **Zircon 缺少的 .Zl 文件** → 检查 NAS 地图引用的 file byte 是否都在
   KROrder 表中且对应 .Zl 文件存在。NAS 地图引用了 file byte 2（Tiles5c）
   和 15（Wood_Tilesc），需确认这些 .Zl 文件在 `Map Data/` 或子目录中。

### 实际操作建议

1. **读取 NAS 地图**：直接复制 .map 文件到 `Debug/Client/Map/`，客户端能加载
   并显示地图尺寸、阻挡、光照等结构数据。

2. **渲染 NAS 地图**：需要确保引用的 .Zl 图库文件存在且 tile 索引有效。
   可以先用 Godot 客户端加载 NAS 地图，观察是否有空白/缺失贴图——
   缺失的 .Zl 文件会导致对应区域不渲染（不崩溃）。

3. **图库转换**：如果需要完全支持 NAS 地图，可以将 NAS 的 .wil/.wix 文件
   转换为 .Zl 格式。Zircon 项目已有 WIL 读取工具（`Tools/` 目录下的
   `wilextract.py`、`wil_probe.py` 等），可用于提取 WIL 帧后重新打包为 .Zl。

## 相关源文件

- `GodotClient/Formats/MapReader.cs` — .map 文件解码器（格式移植自原版）
- `GodotClient/Scripts/MapView.cs` — 地图渲染视图
- `GodotClient/Formats/ZlReader.cs` — .Zl 图库读取器
- `LibraryCore/Libraries.cs:384` — `KROrder` 映射表（file byte → LibraryFile）
- `LibraryCore/Libraries.cs:315-370` — LibraryFile → .Zl 文件路径映射
- `Client/Scenes/Views/MapControl.cs:484-545` — 原版地图加载代码（只读参考）