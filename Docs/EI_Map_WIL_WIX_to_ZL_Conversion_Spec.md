# EI 传奇 3.0 地图迁移与 WIL/WIX → ZL 转换规范

本文档用于把以下旧客户端资源迁移到 Zircon Godot 客户端，并可直接交给其他自动化工具或 AI 执行。

旧客户端目录：

```text
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/
```

目标客户端目录：

```text
/home/tetsuya/development/Zircon/Debug/Client/
```

## 1. 最重要的结论

### 地图文件

旧客户端的 `Map/*.map` 与当前客户端使用的是同一类 Mir3 地图结构，不需要转成 ZL，也不能把地图内容重新编码成图片。

可以直接复制：

```text
旧客户端/Map/xxx.map
→ Zircon/Debug/Client/Map/xxx.map
```

当前 `GodotClient/Formats/MapReader.cs` 的读取格式为：

```text
偏移 22：Int16 Width
偏移 24：Int16 Height
偏移 28：背景层
背景层：Width/2 × Height/2，每项 3 字节
地图单元：Width × Height，每项 14 字节
```

### 图库文件

旧客户端的图库是：

```text
name.wil + name.wix
```

当前客户端只读取：

```text
name.Zl
```

因此所有被地图引用的 WIL/WIX 必须成对转换为 ZL。WIX 不能省略，因为它保存图片索引、偏移和帧信息；WIL 保存图像数据。

## 2. 资源依赖关系

地图文件不直接保存文件名，而是保存数字文件编号和图片编号：

```text
MapCell.BackFile   + BackImage
MapCell.MiddleFile + MiddleImage
MapCell.FrontFile  + FrontImage
```

文件编号通过 `LibraryCore/Libraries.cs` 中的 `Libraries.KROrder` 映射为 `LibraryFile`，再通过 `Libraries.LibraryList` 映射为 ZL 路径。

必须保持下面两个不变量：

1. 同一个文件编号仍然指向同一个图库；
2. 转换前后的图片索引必须完全一致。

不能因为转换工具重新排序、删除空帧或压缩空项而改变图片索引。地图中的 `Image=1234` 必须仍然能在目标 ZL 中读取第 1234 帧。

## 3. 当前文件编号映射

标准地图资源编号大致如下：

```text
0   Tilesc
1   Tiles30c
2   Tiles5c
3   SmTilesc
4   Houses
5   Cliffs
6   Dungeons
7   Inners
8   Furnituresc
9   Wallsc
10  SmObjectsc
11  Animationsc
```

主题地图资源使用独立编号段：

```text
15–26  Wood
30–41  Sand
45–56  Snow
60–71  Forest
```

完整映射必须以当前代码为准，不要凭名称猜测：

```text
/home/tetsuya/development/Zircon/LibraryCore/Libraries.cs
```

转换工具应该读取这张映射表，并生成一份实际使用的依赖报告。

## 4. 目录对应关系

旧客户端资源与目标 ZL 目录的对应关系：

```text
旧 Data/Tilesc.wil + Tilesc.wix
→ Debug/Client/Data/Map Data/Tilesc.Zl

旧 Data/Wood/Tilesc.wil + Tilesc.wix
→ Debug/Client/Data/Map Data/Wood/Tilesc.Zl

旧 Data/Sand/Tilesc.wil + Tilesc.wix
→ Debug/Client/Data/Map Data/Sand/Tilesc.Zl

旧 Data/Snow/Tilesc.wil + Tilesc.wix
→ Debug/Client/Data/Map Data/Snow/Tilesc.Zl

旧 Data/Forest/Tiles.wil + Tiles.WIX
→ Debug/Client/Data/Map Data/Forest/Tilesc.Zl
```

注意旧客户端主题目录中可能使用 `Tiles.wil`，而当前目标名称使用 `Tilesc.Zl`。这不是图片索引转换，而是资源文件名到 `LibraryFile` 的目标命名映射，必须根据 `Libraries.LibraryList` 输出。

## 5. 完整转换流程

### 第一步：建立隔离工作区

不要直接覆盖目标资源。创建：

```text
/tmp/zircon-ei-convert/source
/tmp/zircon-ei-convert/converted
/tmp/zircon-ei-convert/report
/tmp/zircon-ei-convert/backup
```

复制旧客户端的 `Map` 和 `Data` 到 `source`，目标 ZL 先输出到 `converted`。

### 第二步：扫描旧资源

递归查找：

```text
*.map
*.wil
*.wix
*.WIL
*.WIX
```

对每一个 WIL 必须检查同名 WIX 是否存在；对每一个 WIX 也必须检查同名 WIL 是否存在。

输出：

```text
missing-index
missing-image
duplicate-basename
unsupported-format
```

同名但大小写不同的文件必须按不区分大小写匹配，但目标输出名称统一使用当前 `Libraries.LibraryList` 的名称。

### 第三步：扫描地图依赖

对每一个地图：

1. 用 22/24 偏移读取 Width/Height；
2. 检查宽高为正数且不超过合理上限；
3. 按当前 `MapReader.cs` 读取所有背景、中层、前层记录；
4. 收集所有非空的：
   - `BackFile`
   - `MiddleFile`
   - `FrontFile`
5. 对每个文件编号收集最大图片索引；
6. 用 `Libraries.KROrder` 将文件编号解析成 `LibraryFile`；
7. 用 `Libraries.LibraryList` 得到目标 ZL 相对路径。

地图依赖报告至少包含：

```text
mapName
width
height
usedFileIds
usedLibraryFiles
maxImageIndexByFile
missingKROrderIds
```

只有被目标地图实际引用的图库才需要优先转换。为了完整迁移，也可以转换整个 Data 目录，但必须仍然保留索引。

### 第四步：转换每一对 WIL/WIX

转换器必须按以下原则工作：

```text
读取 WIX 索引
→ 得到图片数量和每个图片在 WIL 中的偏移
→ 按原索引 i 读取 WIL 第 i 帧
→ 解码原始调色板/颜色键/Alpha
→ 生成 ZL 第 i 帧
→ 写入 ZL 元数据和像素数据
```

禁止：

- 删除 WIL 中的空帧；
- 重新排序图片；
- 只转换地图当前出现的图片并压缩索引；
- 把透明黑色无条件应用到所有图库；
- 把阴影图当成普通图片覆盖到主图；
- 只转换 WIL 而忽略 WIX。

项目现有转换实现：

```text
LibraryEditor/WeMadeLibrary.cs
LibraryEditor/Mir3Library.cs
```

WIL/WIX 应通过 `WeMadeLibrary` 读取，再通过 `ToMLibrary(...)` 输出 ZL2。不要另写一个只处理 PNG 的简化转换器，因为那样容易丢失原始索引、偏移、阴影和颜色键语义。

### 第五步：透明度规则

透明度不能全局统一处理，必须按旧端绘制用途区分：

1. 地砖、建筑和普通对象：保留原图库 Alpha/颜色键语义；
2. 特效：只有旧端明确使用颜色键时才转换颜色键；
3. 阴影：作为 ZL Shadow 层保存，不与主图合并；
4. Overlay/Mask：如果原 WIL/WIX 有独立遮罩，应写入 ZL Overlay；
5. 调色板 WIL：使用旧端调色板还原成 RGBA；
6. 不能因为某张图看起来是黑色就自动把黑色全部变透明。

否则会产生火球变红、透明层丢失、建筑镂空、阴影错位等问题。

### 第六步：目标路径安装

转换成功后，把输出按 `Libraries.LibraryList` 的目标路径安装。例如：

```text
converted/Data/Map Data/Forest/Tilesc.Zl
→ Debug/Client/Data/Map Data/Forest/Tilesc.Zl
```

安装前执行：

1. 备份同名旧 ZL；
2. 比较输出数量和最大索引；
3. 确认目标目录存在；
4. 写入转换清单和 SHA256；
5. 不覆盖未参与本次转换的资源。

## 6. 转换后的自动验证

### 地图结构验证

每个输出地图必须满足：

```text
Width > 0
Height > 0
文件长度 >= 28 + Width/2*Height/2*3 + Width*Height*14
```

### ZL 索引验证

对于地图引用的每个图库：

```text
0 <= imageIndex < Zl.Images.Length
Zl.Images[imageIndex] != null 或为合法空帧
```

空帧可以存在，但不能把越界索引当成空帧掩盖。

### 引用完整性验证

输出以下报告：

```text
PASS map=xxx fileId=60 library=Forest_Tilesc image=1234
MISSING map=xxx fileId=62 library=Forest_Tiles5c image=9000
UNSUPPORTED map=xxx fileId=99
```

只要存在 `MISSING` 或 `UNSUPPORTED`，就不能认为地图转换完成。

### 视觉验证

至少逐一检查：

- 背景地砖；
- 中层地形；
- 房屋和建筑；
- 前景遮挡；
- 动画地面；
- 阴影；
- 透明边缘；
- 地图阻挡标志；
- 夜晚/光照效果。

应使用同一个地图、同一个坐标、同一个视野，分别截图旧客户端和新客户端进行对照。

## 7. 推荐的自动化伪代码

```text
sourceRoot = /home/tetsuya/NAS/TMP/EI传奇3.0客户端
targetRoot = /home/tetsuya/development/Zircon/Debug/Client

load KROrder and LibraryList from current Zircon source
scan sourceRoot/Map/*.map

for map in maps:
    parsed = read_map_using_current_14_byte_layout(map)
    deps = collect_non_empty_back_middle_front_file_ids(parsed)

    for fileId in deps:
        libraryFile = KROrder[fileId]
        targetZl = LibraryList[libraryFile]
        sourceBase = resolve_old_wil_wix_by_library_file(libraryFile, sourceRoot/Data)
        require sourceBase.wil and sourceBase.wix
        add dependency(libraryFile, sourceBase, targetZl, maxImageIndex)

for dependency in unique_dependencies:
    pair = load_wil_and_wix(dependency.sourceBase)
    zl = convert_without_reordering(pair)
    validate_index_preservation(pair, zl)
    save_to_staging(zl, dependency.targetZl)

for map in maps:
    validate_map_structure(map)
    validate_all_referenced_zl_frames(map, stagingRoot)

write conversion-manifest.json
write dependency-report.json
write validation-report.txt

if no errors:
    backup existing target files
    install staged maps and ZL files
else:
    do not modify target
```

## 8. 最终交付物

另一个转换工具完成后，必须交付：

```text
转换后的 .map 文件
转换后的 .Zl 文件
conversion-manifest.json
dependency-report.json
validation-report.txt
每个文件的 SHA256
失败文件清单（如果有）
```

`conversion-manifest.json` 至少记录：

```json
{
  "source": "旧 WIL/WIX 路径",
  "target": "目标 ZL 路径",
  "imageCountBefore": 0,
  "imageCountAfter": 0,
  "maxReferencedIndex": 0,
  "shadowPreserved": true,
  "overlayPreserved": true,
  "sha256": "..."
}
```

## 9. 最容易出错的地方

### 沙巴克地图的混合图库引用

EI 的部分地图并不是“一个 file byte 永远对应一个独立主题图库”。例如
`3.map`（沙巴克）会使用 file byte `24`、`25` 的高帧索引；EI 的
`Data/Wood/Wallsc.wix` 与 `Data/Wood/SmObjectsc.wix` 帧数不足以覆盖这些
索引，而根目录的 `Wallsc.wix`、`SmObjectsc.wix` 可以覆盖。客户端运行时必须
保持以下规则：

1. 先从 file byte 对应的主题 ZL 读取；
2. 主题 ZL 没有该帧或该帧为空时，回退到同名根 ZL；
3. 不能因为主题 ZL 文件存在，就认为该地图的所有引用都有效；必须检查
   地图实际使用的每一个帧索引。

当前 Godot 客户端在 `GodotClient/Scripts/MapView.cs` 实现了这一运行时回退，
只影响主题库中不存在的帧，不覆盖主题库中已有的有效帧。

1. 把 `.map` 也错误地转换成 ZL；
2. 只转换根目录 Data，没有转换 Forest/Snow/Sand/Wood；
3. WIL/WIX 图片数量不同却继续转换；
4. 删除空帧导致地图索引整体错位；
5. 把 `Tiles.wil` 错映射成普通 `Tilesc.Zl`；
6. 透明黑色处理过度，导致建筑或特效颜色异常；
7. 没有保留阴影和 Overlay；
8. 只验证文件存在，没有验证地图实际引用的图片索引；
9. 直接覆盖当前 ZL，失败后无法回滚；
10. 没有使用当前 `Libraries.KROrder`，自行猜测文件编号。

## 10. 推荐执行顺序

最稳妥的执行顺序是：

```text
先转换一个小地图
→ 生成依赖报告
→ 验证所有引用索引
→ 新旧客户端同坐标截图对比
→ 再批量转换同主题地图
→ 最后转换全部地图和共享图库
```

第一批建议选择尺寸较小、同时包含地砖/建筑/前景的地图，不要一开始就批量覆盖整个客户端资源目录。
