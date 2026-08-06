# 讨论记录 06：地图渲染 —— .Zl 图库与 .map 地图读取器

> 日期：2026-08-06
> 关联：讨论 05 完成了登录/选角色，本篇实现第 3 步核心：.Zl 图库读取 + .map 地图解析 + 地形渲染。
> 结论：**.Zl 读取 + DXT 解码 + .map 解析 + 地形渲染全部走通。** GameScene 骨架已建，StartGame 包流处理框架就位，但进游戏后的完整包流对接有遗留问题。

---

## 1. 目标

第 3 步的核心：把游戏资产（.Zl 图库、.map 地图）在 Godot 里渲染出来。参考用户整理的 `docs/RENDERING_PORT_GUIDE.md`（4309 行，全量提取自客户端源码）。

## 2. 资产格式调研

### 2.1 .Zl 图库（339 个文件）

- **旧格式**（332 个）：头部 `Int32`(元数据块大小) → 元数据块 → 图像数据
- **ZL2 压缩容器**（7 个）：签名 `ZL2` + 索引表 + 元数据，暂不支持
- 编解码器：`Dxt1`（version 0 默认）、`Dxt5`（version 1+）、`Bgra32`、`Bc7`、`Png`
- 每帧元数据：Position(Int32) + Width(Int16) + Height(Int16) + OffSetX/Y(Int16) + Shadow* + Overlay*
- version >= 2 额外有：AtlasPage + SourceRectangle + VisibleBounds + codecs + sizes

**关键发现**：旧格式的第一个 `Int32` 是**元数据块大小**，不是 count。需要读该块到内存后在内存流里解析 count+version。源码 `MirLibrary.cs:69` 确认。

### 2.2 .map 地图（258 个文件）

格式（来自 RENDERING_PORT_GUIDE.md §7.2）：
- 头部 22 字节（跳过）
- Int16 ×2：Width, Height
- 背景层（半分辨率）：`Width/2 × Height/2` 个条目，每条 `Byte + UInt16`
- 全分辨率单元格：`Width × Height` 个条目，每格 14 字节

### 2.3 KROrder 与 LibraryList

`LibraryCore/Libraries.cs` 已有 `KROrder`（62 条，文件字节 → LibraryFile 枚举）和 `LibraryList`（314 条，LibraryFile → 磁盘路径）——**直接复用，无需重写**。

## 3. 实现

### 3.1 ZlLibrary（Formats/ZlReader.cs）

移植自 `RenderingCore/Library/MirLibrary.cs` + `ZlImageMetadata.cs`，去掉 D3D9 依赖：

```
ReadLibrary():
  1. 检查 ZL2 签名 → 暂不支持
  2. 读 Int32(metaSize) → 读 metaSize 字节到内存流
  3. 在内存流里读 Int32(value) → count = value & 0x1FFFFFF, version = (value>>25) & 0x7F
  4. 逐帧: ReadBoolean(有无) → ZlImage.Read(reader, version)

GetImageTexture(index):
  1. Seek 到 Images[index].Position
  2. 读 ImageDataSize 字节
  3. 按 ImageCodec 解码:
     - Bgra32 → 直接用
     - Png → Godot Image.LoadPngFromBuffer
     - Dxt1/Dxt5/Bc7 → BCnEncoder.NET 解码
  4. BGRA → RGBA 转换 → Godot Image.CreateFromData → ImageTexture
```

### 3.2 BcnDecoder（Formats/BcnDecoder.cs）

用 BCnEncoder.NET 2.3.0（NuGet 包）解码 DXT1/5/BC7 → BGRA32：
- Dxt1 → CompressionFormat.Bc1WithAlpha
- Dxt5 → CompressionFormat.Bc3
- Bc7 → CompressionFormat.Bc7

### 3.3 MirMap（Formats/MapReader.cs）

按 §7.2 格式解析：背景层（半分辨率）+ 全分辨率单元格（每格 14 字节）。MiddleImage/FrontImage 已 +1，绘制时 -1。

### 3.4 MapView（Scripts/MapView.cs）

可复用的地图渲染 Node2D：
- `LoadMap(fileName)` 加载 .map
- `_Draw()` 按视野范围渲染三层（背景/中层/前景）
- `CenterOn(x,y)` 滚动到指定坐标
- 用 `KROrder` 把 cell 的 fileByte → LibraryFile → LibraryList 路径 → ZlLibrary
- 跳过 `Tilesc`（与原版一致）

### 3.5 GameScene（Scripts/GameScene.cs）

游戏主场景骨架：
- 处理 `S.StartGame`(成功后拿 StartInformation：玩家位置/方向/地图)
- 处理 `S.MapChanged`(地图切换)
- 处理 `S.UserLocation`(玩家移动)
- MapView 渲染地图 + 红色方块代表玩家
- 键盘方向键发 `C.Move` 包

## 4. 验证结果

### .Zl 读取验证
```
[MapTest] 加载库: Tiles30c (1080 帧)
[MapTest] Tiles30c: 1080 帧, version=0
[MapTest] 帧0: 96x64 codec=Dxt1 pos=24488
[MapTest] 帧0 纹理: 96x64          ← DXT1 解码成 Godot 纹理成功 ★
```

### .map 解析验证
```
[MapTest] 地图: 350x350
[MapTest] 20x20 区域: 背景=99, 中层=400, 前景=400
```

### 地图渲染验证
```
[MapTest] 渲染完成    ← 400 格三层贴图（背景+中层+前景）全部加载渲染 ★
```

## 5. 踩坑记录

| 坑 | 原因 | 修复 |
|---|---|---|
| .Zl 第一个 Int32 当 count 导致帧数据全错 | 它是元数据块大小，不是 count | 读该块到内存流，在内存流里解析 count+version |
| `LibraryList` 路径 `Data\Map Data\...` 拼接重复 `Data/` | `_dataPath` 已含 `Data/`，path 也以 `Data/` 开头 | `if (path.StartsWith("Data/")) path = path.Substring(5)` |
| `Replace("Data/", "")` 破坏 `Map Data/` 路径 | 全局替换把 `Map Data/` 变成 `Map ` | 改用 `StartsWith` 判断只去前缀 |
| `Color` 歧义（System.Drawing vs Godot） | 两个命名空间都有 Color | 用 `Godot.Color` 全限定 |
| `CallDeferred` 不能传 `StartInformation` | 不是 Godot Variant | 存成员变量，调无参方法 |
| `C.Move` 没有 `Location` 字段 | 只有 `Direction` + `Distance` | 去掉 Location |
| ZL2 压缩容器格式（7 个文件） | 签名 + 索引表 + 元数据，结构复杂 | 暂不支持，打印警告跳过 |

## 6. 遗留问题（进游戏完整包流）

GameScene 骨架已建，但 StartGame 成功后的完整包流对接有遗留问题：

- **现象**：`C.StartGame` 发出后服务端收到（日志确认 `Stage=Select`），但 Godot 端收不到服务端回的 `S.StartGame` 回包，连接随后断开
- **可能原因**：包路由（`ProcessPacket` 反射找 `Process(S.StartGame)` 方法）未匹配，或回包序列化/反序列化问题
- **影响**：进游戏后看不到玩家在地图上（地图渲染本身已验证通过，见 MapTestScene）

**当前可验证**：
- `MapTestScene`（主场景）→ 直接加载 0.map 渲染地形（headless 验证通过）
- 登录→选角色流程（讨论 05 验证通过）

**待解决**：
- StartGame 回包的路由问题
- 进游戏后完整包流（ObjectInformation、周围物体等）

## 7. 涉及的文件

| 文件 | 用途 |
|---|---|
| `GodotClient/Formats/ZlReader.cs` | .Zl 图库读取器（移植自 MirLibrary） |
| `GodotClient/Formats/BcnDecoder.cs` | DXT1/5/BC7 解码（BCnEncoder.NET） |
| `GodotClient/Formats/MapReader.cs` | .map 地图解析器 |
| `GodotClient/Scripts/MapView.cs` | 可复用地图渲染视图 |
| `GodotClient/Scripts/MapTestScene.cs` | 地图渲染测试场景 |
| `GodotClient/Scripts/GameScene.cs` | 游戏主场景骨架 |
| `GodotClient/Scenes/MapTestScene.tscn` | 地图测试场景 |
| `GodotClient/Scenes/GameScene.tscn` | 游戏场景 |
| `GodotClient/ZirconClient.csproj` | 加 BCnEncoder.NET 2.3.0 |

## 8. 参考文档

- `docs/RENDERING_PORT_GUIDE.md`：用户整理的全量渲染数据文档（4309 行）
  - §7.1 网格常量（CellWidth=48, CellHeight=32）
  - §7.2 .map 二进制格式
  - §7.3 KROrder 映射表
  - §8.3 包级实现路线（包 A-G）
- `docs/vortice-migration.md`：SharpDX→Vortice 迁移（与 Godot 方案无关，可忽略）

## 9. 下一步

1. **解决 StartGame 回包路由问题**——可能需要在 ServerConnection 检查包 ID 映射，或用 `PacketMethods` 缓存调试
2. **处理进游戏后的包流**：`S.StartGame` 成功后的 `StartInformation` → 加载对应地图 → `S.ObjectInformation`（玩家自己）→ 周围物体
3. **从 System.db 查 MapIndex → MapInfo.FileName**（目前硬编码 0.map）
4. **玩家精灵**：用 M-Hum.zl 渲染真实玩家外观（替代红色方块）