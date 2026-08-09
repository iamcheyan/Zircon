# 地图格式解码 — .map 文件

> 来源:`GodotClient/Formats/MapReader.cs`(移植自原客户端 `Scenes/Views/MapControl.cs:484-545`)与 `tools/mir3ei_render.py` 的 `read_back_positions`(2003 旧版)。
> 两版结构一致,唯一差异在单元格层长度(旧版 13/14 字节自动探测,新版固定 14 字节)。

## 1. 总体布局

```
偏移 0–21 : 头部 22 字节(跳过)
偏移 22   : UInt16 LE — Width  W
偏移 24   : UInt16 LE — Height H
偏移 28   : 数据区
  ├─ 背景层: (W/2) × (H/2) 条,每条 3 字节 —— 半分辨率,只存偶数格
  └─ 单元格层: W × H 条,每格 14 字节(旧版部分文件 13 字节)
```

校验:数据区起点 = `28 + (W/2)*(H/2)*3`;单元格层总长 = W×H×14(或 ×13,自动探测)。

## 2. 背景层(back)— 地面

每条 3 字节:

```
u8    file   — 图库文件字节(0–13 基础库,15+ 主题子目录;255 = 空瓦片)
u16   image  — 库内帧号(小端)
```

- 半分辨率:只存 `(W/2)×(H/2)`,渲染时一个 back 格覆盖 2×2 世界格
- **地面渲染 = 只画 back 层**,逐格取 `file:frame` → 图库 → 瓦片,255 跳过

## 3. 单元格层(每格 14 字节,新版)

```
u8   flag            — 阻挡标志: (flag&0x01)!=1 || (flag&0x02)!=2 → 阻挡
u8   middleAnimationFrame
u8   value           — FrontAnimationFrame: 255→0, 再 &= 0x8F
u8   frontFile
u8   middleFile
u16  middleImage     — 存储时 +1(绘制时 -1)
u16  frontImage      — 存储时 +1(绘制时 -1)
3B   跳过
u8   light           — 低 4 位 ×2
1B   跳过
```

合计 5 + 2 + 2 + 3 + 1 + 1 = **14 字节**。

### 3.1 旧版(2003)

`read_back_positions` 自动探测:单元格层总长匹配 `W×H×14` → mode 14;匹配 `W×H×13` → mode 13。旧版 13 字节布局的字段差异未进一步拆分(渲染只用 back 层)。

## 4. 三层语义与绘制顺序

| 层 | 内容 | 分辨率 |
|---|---|---|
| back(背景) | 地面瓦片 | 半分辨率(偶数格) |
| middle(中景) | 建筑/可交互对象层 | 全分辨率 |
| front(前景) | 遮挡层 | 全分辨率 |

绘制顺序 `back → middle → front`。原版跳过 `Tilesc`(地面专用库)不做叠层。middle/front 的 image 号 `+1` 存储,绘制时 `-1`(0 号帧是空帧占位)。

## 5. 网格常量

```
世界格: CellWidth = 48 px, CellHeight = 32 px
地面瓦片: 96×64(旧 WIL)/ 96×64(新 Zl),一格 back 覆盖 2×2 世界格
```

## 6. 文件字节 → 图库名

- 0–13:`Tilesc / Tiles30c / Tiles5c / SmTilesc / Housesc / Cliffsc / Dungeonsc / Innersc / Furnituresc / Wallsc / SmObjectsc / Animationsc / Object1c / Object2c`
- 15–71:主题子目录(Wood/Sand/Snow/Forest),完整表见 `formats/FORMAT_WIL_WIX.md` §4
- 新版权威映射:`LibraryCore/Libraries.cs` 的 `KROrder`(62 条)+ `LibraryList`

## 7. 实测样例

- `D101.map` 等:双方(旧/新客户端)MD5 一致(`8dc4…`),格式互通
- 2003 版 back 层 file 分布(矿洞系 D201 等):`{0: 322861, 1: 1284671, 2: 4464500, 15: 4051, 17: 212656, 30: 31109, 60: 34196, 62: 183580, 255: 737}`,总 6,538,361 瓦片

## 8. 相关实现

- `GodotClient/Formats/MapReader.cs`(生产读取器,`MapCell` 结构)
- `tools/mir3ei_render.py` `read_back_positions`(back 层快速读取,13/14 探测)
- 渲染流程:见 `formats/RENDER_PIPELINE.md`
