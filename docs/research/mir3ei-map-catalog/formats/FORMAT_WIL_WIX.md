# 素材格式解码 ① — 旧版 WIL/WIX(2002–2003 韩服 Mir3)

> 来源:对 `/home/tetsuya/NAS/TMP/mir3ei/`(Mir3.exe 2003-05-14)的逆向,配合 `docs/research/mir3ei-map-catalog/tools/mir3ei_render.py`(本目录可运行脚本)。
> 适用范围:2002–2003 原版客户端的所有 `.wil` 素材库。

## 1. 文件对:`.wil` + `.wix`

每个素材库是**两个文件**:

| 文件 | 内容 |
|---|---|
| `xxx.wil` | 图像帧数据(16-bit 565 像素 + RLE 压缩) |
| `xxx.wix` | 帧索引(每帧起始偏移) |

`.wix` 头之后是每帧 4 字节的 UInt32 LE 偏移表。

## 2. WIX 索引布局

```
偏移 0–19    : 头部(本解码器未使用)
偏移 20      : UInt32 LE — 帧数 n
偏移 24+4*i  : UInt32 LE — 第 i 帧数据在 .wil 内的起始偏移 offs[i]
```

读法(`Wil.__init__`):

```python
self.n = struct.unpack_from('<I', w, 20)[0]
self.offs = [struct.unpack_from('<I', w, 24+4*i)[0] for i in range(self.n)]
self.offs.append(len(self.d))          # 末尾哨兵 = 文件长度
```

### 2.1 有效帧规则(重要)

```
帧 i 有效 ⇔  28 <= offs[i] < len(wil文件)
```

- `offs[i] < 28`:WIX 头部区域内的偏移,不是帧
- `offs[i] >= len(wil)`:越界
- 无效帧在渲染时必须跳过,否则读出的"帧"是垃圾数据

### 2.2 别名重映射索引

**`Tiles5c.wix` 是 2002 原版的别名重映射索引**:部分帧偏移为 0 或指向其他库的实际数据(旧地图大量使用 Tiles5c 帧号,但数据在别处)。这也是"旧客户端地面全灰"类 bug 的常见根源之一——**先判有效帧,再取数据**。

## 3. 帧数据布局(在 .wil 内)

```
offs[i] + 0    : UInt16 LE — 帧宽 W
offs[i] + 2    : UInt16 LE — 帧高 H
offs[i] + 4..16: 13 字节帧头(未解码,与像素无关)
offs[i] + 17   : 像素数据起点
```

> 帧头共 17 字节:W(2) + H(2) + 13 字节未知。WIL 文件头本身未被解析——帧数据完全由 WIX 偏移寻址。

### 3.1 帧长度

```
extent(i) = offs[i+1]          若 offs[i+1] > offs[i]   (下一帧起点 = 本帧终点)
           min(offs[i]+12689, len(d))  否则           (兜底:12689 为经验值)
```

### 3.2 行 RLE 编码(0xC0 标记)

每一行:

```
UInt16 — 该行元素数 cnt
随后 cnt 个元素,每个元素:
    UInt16 — 标记 m
    UInt16 — 长度 rl
    若 m == 0xC0: 跳过 rl 个像素(透明)
    否则        : 连续 rl 个 16-bit 像素(565 格式),紧跟元素头之后
```

行尾校验:解析后的游标必须精确等于行元素区终点,否则 `misalign`(数据损坏或格式假设错误)。

> 实测:地面瓦片只出现 0xC0(透明跳过)与直接像素行两种元素。0xC1–0xC3 等其他标记值未出现在解码路径中。

### 3.3 565 像素 → 8-bit RGB

```python
R = ((px >> 11) & 0x1F) << 3
G = ((px >> 5)  & 0x3F) << 2
B = (px        & 0x1F) << 3
```

5/6/5 位直接左移到 8 位(不做伽马校正)。

## 4. 库命名与文件字节映射

`.map` 的 back 层每格存一个**文件字节**(0–13 基础库 + 15–71 主题子目录)。旧版映射:

```
0  Tilesc          1  Tiles30c       2  Tiles5c       3  SmTilesc
4  Housesc         5  Cliffsc        6  Dungeonsc     7  Innersc
8  Furnituresc     9  Wallsc        10  SmObjectsc   11  Animationsc
12 Object1c       13  Object2c
```

主题子目录(Wood 15–26 / Sand 30–41 / Snow 45–56 / Forest 60–71),例:

```
15 Wood/Tilesc    17 Wood/T5c       21 Wood/Dungeonsc   30 Sand/Tilesc
45 Snow/Tiles    60 Forest/Tilesc  62 Forest/T5c
```

个别文件名需重命名才能对上磁盘文件:`Wood/T5c → Wood/Tiles5c`、`Forest/T5c → Forest/tiles5c`。

## 5. 解码产物与精度

- 地面瓦片原始尺寸 **96×64**(WIL 帧);minimap 渲染按 `SCALE=8` 缩到 **12×8**/格
- 纯 back 层渲染(不叠 middle/front):底色 `RGB(40,40,40)`,瓦片按 alpha 合成
- 566 张地图渲染结果见 `views/`,家族接片见 `contact/`

## 6. 相关实现

- 本目录 `tools/mir3ei_render.py`:`Wil` 类(WIX/帧/RLE)+ `read_back_positions`(.map back 层)+ 多进程渲染
- 新版格式对照:见 `formats/FORMAT_ZL.md`
