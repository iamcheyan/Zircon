# 素材格式解码 ② — 新版 Zl 图库(2017 中文版)

> 来源:对 2017 中文版客户端 `Data/Map Data/*.Zl` 的逆向(`/home/tetsuya/development/Zircon/Debug/Client/Data/Map Data/`),配合本目录 `tools/cave_analysis.py` 与 `GodotClient/Formats/ZlReader.cs`。
> 范围:旧格式(version 0/1,绝大多数)+ ZL2 容器(version 2,少数)。

## 1. 文件结构

```
偏移 0      : Int32 LE — 元数据块大小 meta_size
偏移 4      : 元数据块(meta_size 字节)
偏移 4+meta : 图像数据区
```

**关键陷阱**:文件第一个 Int32 是**元数据块大小,不是帧数**。必须先把 meta 块读进内存流,再在其中解析 count/version,否则整库帧位置全错(旧客户端 `MirLibrary.cs:69` 即此结构)。

## 2. 元数据块

```
Int32 value → count = value & 0x1FFFFFF
              ver   = (value >> 25) & 0x7F
```

- `ver == 0` 时 `count = value`(旧库无高位标志,直接取原值)
- `ver == 0` → 帧图像为 **DXT1**(每 4×4 块 8 字节)
- `ver == 1` → 帧图像为 **DXT5**(每 4×4 块 16 字节)

### 2.1 帧记录(每条 26 字节)

```
u8   present            — 0 = 无此帧(跳过)
i32  Position           — 图像数据在文件内偏移
i16  Width, Height      — 像素尺寸
i16  OffX, OffY         — 绘制偏移
u8   ShadowType
i16  ShW, ShH, ShX, ShY — 阴影区
i16  OvW, OvH           — 覆盖区
```

> `ver >= 2`(ZL2)的记录更长:另有 AtlasPage/SourceRectangle/VisibleBounds、逐部分 codec 与尺寸(见 `ZlReader.cs` 的 `ZlImage.Read`)。

## 3. DXT 解码(纯 Python 参考)

### 3.1 565 颜色字

```python
R = ((v >> 11) & 0x1F) << 3; R |= R >> 5
G = ((v >> 5)  & 0x3F) << 2; G |= G >> 6
B = (v & 0x1F) << 3;         B |= B >> 5
```

### 3.2 DXT1(8 字节/块)

```
@0–3 : c0, c1(各 2 字节 565)
@4–7 : 2bpp 颜色索引(每像素 2 位)
```

- `c0 > c1`:4 色模式,`c2=(2c0+c1)/3`, `c3=(c0+2c1)/3`
- `c0 <= c1`:3 色模式,`c2=(c0+c1)/2`, `c3 = 透明黑`
- 索引:像素 (i,j) 取 `idx[j] >> (2*i) & 3`

### 3.3 DXT5(16 字节/块) — 位布局易错点

```
@0–1 : a0, a1(alpha 端点)
@2–7 : alpha 3bpp 索引(6 字节 = 48 位,每像素 3 位)
@8–9 : c0, c1(565 颜色端点)
@12–15: 颜色 2bpp 索引
```

**易错点(本会话修复过的 bug)**:颜色部分**不是** `@8` 起连续 8 字节——`@8–9` 是 c0/c1,**`@12–15` 才是 2bpp 索引**,`@10–11` 不参与。若按连续布局解码,画面全灰/全错。

alpha 解码:8 个 alpha 值;`a0 > a1` 时 8 值线性插值,否则前 6 值插值 + 第 6/7 值为 0/255。

```python
# alpha 值表
if a0 > a1:
    a = [a0, a1] + [( (7-k)*a0 + k*a1 ) // 7 for k in range(1,7)]
else:
    a = [a0, a1] + [( (5-k)*a0 + k*a1 ) // 5 for k in range(1,5)] + [0, 255]
# 每像素 3 位索引: 48 位打包在 @2–7, 像素 (i,j) 取位 (j*4+i)*3
```

### 3.4 其余编解码器(ZL2 / ver>=2)

`ZlImageCodec` 枚举:`Dxt1, Dxt5, Bgra32, Bc7, Png`。Godot 端用 BCnEncoder.NET 解码 DXT1/5/BC7(`GodotClient/Formats/BcnDecoder.cs`)。

## 4. ZL2 压缩容器(7 个库)

- 文件头签名 `ZL2` + 索引表 + Deflate 压缩,按 entry 索引
- 已支持:GodotClient `ZlReader.cs`(`Zl2Entry`);`cave_analysis.py` 不支持
- 遇到时打印警告跳过即可,地面库几乎全为旧格式

## 5. 库字节 → 磁盘路径

权威映射在 `LibraryCore/Libraries.cs`:`KROrder` 字典(文件字节 → `LibraryFile` 枚举,62 条)+ `LibraryList`(枚举 → 磁盘路径)。路径根为 `Data/Map Data/`,子目录与旧版同构(Wood/Sand/Snow/Forest 等)。

典型库:`Tiles30c.Zl` 1080 帧、`Tiles5c.Zl` 等;文件字节 0–13 与旧版同名(见 FORMAT_WIL_WIX.md §4)。

## 6. 新旧对应关系(调查结论)

- 旧 WIL 库与新 Zl 库**同名同编号**:旧 `Tiles5c.wil` 帧 N ≡ 新 `Tiles5c.Zl` 帧 N(像素级一致,仅 DXT5 有损量化 vs RLE565 的噪声差异)
- 2017 版已精简地图:旧版 566 张中约 392 张在新版无对应(指纹相似度落在 0.40 噪声底)
- 城镇按编号同名沿用:0 比奇城、1 失乐园、2 潘夜村、3 沙巴克城、4 努玛村、5 沙漠土城、8 南哨站

## 7. 相关实现

- `tools/cave_analysis.py`:`read_zl` / `decode_dxt1` / `decode_dxt5`(纯 Python,含修复后的位布局)
- `GodotClient/Formats/ZlReader.cs`:Godot 生产版读取器(ZL2 支持、codec 分发)
- 渲染/指纹链路:见 `formats/RENDER_PIPELINE.md`
