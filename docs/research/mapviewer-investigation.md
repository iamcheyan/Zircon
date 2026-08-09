# 地图查看工具 mapviewer.py — 调查与使用说明

> 日期:2026-08-09。对象:`Tools/mapviewer.py`(911 行,未提交,`??` 状态)。
> 结论:**mapviewer 不是独立新工具,是 `Tools/` 下 WIL 素材工具集(wilsdk/wilviewer/wilextract)的延伸**——它复用同一个 `wilsdk.WilLibrary` 解码核心,只是消费方式完全不同:前者看素材帧,它把 `.map` 组装成可平移缩放的等距地图。

---

## 1. 它是什么

一个 **HTTP 服务型地图浏览器**:输入一个 `.map` 文件目录(+ 素材库目录),服务端把每张地图的 ground/object 层渲染成**可缩放瓦片金字塔(z=0 即 1:1,每瓦片 512px)**,浏览器端平移/缩放/切层/网格叠加/缩略图导航。实测 566 张地图全部可用。

## 2. 工具集关系:同一家族的延伸,不是新工具

`Tools/` 下四个 Python 工具,共享 `wilsdk.py` 这个解码核心:

```
wilsdk.py      核心库:WilLibrary(mmap 惰性解码, 17B 帧头 + 0xC0/0xC1/0xC2/0xC3 RLE, 565→RGBA)
   ├── wilviewer.py  素材浏览器(83 库 / 逐帧 / GIF / 声音)—— 看素材
   ├── wilextract.py 命令行批量导出 PNG / 拼图
   └── mapviewer.py  地图浏览器(组装 .map → 等距渲染)—— 看地图 ★本文
```

**证据(来源关系)**:

| 项目 | mapviewer | wilsdk |
|---|---|---|
| `from wilsdk import WilLibrary` | ✓ | — |
| 帧解码(RLE/565/17B 头) | 全部走 `WilLibrary.header()/decode()` | ✓ 实现 |
| `scan_libraries/categorize` | 不用 | ✓ |
| 服务端渲染 | 自带(瓦片金字塔 + LRU 缓存) | 无 |

- **独立的部分**:`.map` 解析(`parse_map`)、等距几何(`cell_anchor`/`world_bounds`/painter 序)、瓦片金字塔切片与 HTTP 层——这些都是 mapviewer 自有的,不在 wilsdk 里
- **不依赖** `mir3ei-map-catalog/` 图鉴那套脚本(那是"按 back 层指纹识别地图",mapviewer 是"逐格实时组装渲染",两者独立)

## 3. 使用方法

```bash
# 基本用法:maps 目录必须,data 自动探测(<maps_dir>/../Data、<maps_dir>/Data、maps_dir 自身)
python3 Tools/mapviewer.py <maps_dir> [--data <data_dir>] [--port 8766]

# 实测(NAS 镜像)
python3 Tools/mapviewer.py /home/tetsuya/NAS/TMP/mir3ei/Map \
    --data /home/tetsuya/NAS/TMP/mir3ei/Data --port 8766
# 启动日志:maps: 566  data: ...   libraries: ground[4] objects[59]
# 浏览器打开 http://127.0.0.1:8766/
```

依赖:**Python 3 + Pillow + wilsdk.py(同目录)**,零其他依赖。实测系统 Python 3.14 正常。

### 3.1 浏览器 UI

- **左侧**:566 张地图列表(名字搜索过滤),显示 `宽×高 · 大小`
- **主视图**:等距地形,拖拽平移、滚轮缩放、`+`/`−`/`Fit` 缩放控制
- **工具栏**:`Grid`(网格叠加)、`Ground`/`Objects`(切层)、右下角**缩略图导航**(点击跳转、红框示视野)
- 底部状态栏:当前 cell 坐标 / 世界像素 / 已加载瓦片数

### 3.2 HTTP API

| 端点 | 说明 |
|---|---|
| `GET /api/maps` | 全部地图清单 `[{name, w, h, size}]` |
| `GET /api/mapinfo?map=<name>.map` | 尺寸/瓦片帧范围/所用库/zmax |
| `GET /api/tile?map=<name>.map&z=&tx=&ty=&layers=go` | 512px 瓦片 PNG(空瓦片返回 1×1 透明,可缓存) |
| `GET /api/thumb?map=<name>.map` | 全图缩略图 PNG(320px 宽) |

> ⚠️ `map` 参数**必须带 `.map` 后缀**(如 `D2011.map`),否则 404。

## 4. 实现要点(逆向结论)

### 4.1 .map 解析(mapviewer 版)

```
28 字节头;UInt16 W @22、UInt16 H @24
flags 区  W*H*3/4 字节 → 单元格区  W*H × 14 字节(列主序,index = x*h + y)
每格 14 字节:
  [0:2] ground tile 索引(16 位,低 12 位 = WIL 帧号;0xFFF/4095 = 无;0xF02=3842 是真实 Tilesc 帧)
  [2:4] object 索引(同上;4090 以上视为无)
  …
总大小 = 28 + W*H*14.75,多文件验证过
```

### 4.2 等距投影

```
格心世界坐标: cx = (x−y)*24 + h*24 + 24,  cy = (x+y)*16 + 16
地面帧 96×64 画在 (cx−24, cy−16)(格心锚定)
物体帧画在 (cx+offX, cy+offY)(off 取自帧头)
绘制序:按 (x+y) 升序(远→近),每格先 ground 后 object
```

- 世界尺寸 `(w+h+3)*24 × (w+h+2)*16`
- 瓦片金字塔:z 层 1 屏像素 = 2^z 世界像素;z=0 为 1:1,zmax 整图缩进 ~1024px
- 稀疏渲染:`MapCache.sparse()` 预计算有内容的格,按 painter 序(v=x+y)排序,bisect 切瓦片视野

### 4.3 帧解析兜底逻辑(FramePool)

- ground 候选库优先序:`tilesc, tiles5c, tiles30c, tiles, tiles5, tiles2, smtiles, tilesb, tiles3`
- object 候选库:`object2c, object1c, objectc, object2, object1, smobjectsc, smobjects, housesc, house, wallsc, wall, objectb, smobject`
- 排除非几何库:anim/magic/monster/npc/mount/ui/effect/title/face/font/icon/item/weapon/armor 等
- 帧"存在"判定:该库有该帧且头有效(width>0、height>0、bytes>0);按优先序第一个命中

### 4.4 缓存

| 缓存 | 上限 | 内容 |
|---|---|---|
| `CACHE_MAPS_MAX` | 3 | 解析后的地图原始区 |
| `CACHE_TILES_MAX` | 400 | 渲染瓦片 PNG 字节 |
| `CACHE_FRAMES_MAX` | 600 | 解码帧(惰性,1:1 一次 + 每缩放一份) |
| `CACHE_THUMBS_MAX` | 12 | 全图缩略图 |

## 5. 实测记录

| 项目 | 结果 |
|---|---|
| 566 张地图全部加载 | ✓ `maps: 566` |
| `/api/mapinfo`(0.map 800×800,640k 格,zmax 7) | ✓ |
| `/api/tile`(D2011.map z1) | ✓ 512×512 RGBA 非空 |
| `/api/thumb`(D2011.map) | ✓ 320×213 非空 |
| 浏览器 UI(地形/网格/缩略图/切层) | ✓ 视觉确认正常 |
| 性能 | 单 tile 请求毫秒级;大图首屏由稀疏渲染 + LRU 支撑 |

> 端口冲突:8766 已被一个常驻 mapviewer(pid 103432)占用——这就是"再启动报 Address already in use"的原因;另有 wilviewer 占 8765。要开新实例换 `--port`。

## 6. 与素材格式文档的关系

- 帧解码(WIL/WIX 头、RLE opcode、565)细节见 `docs/research/mir3ei-map-catalog/formats/FORMAT_WIL_WIX.md`(wilsdk 与 `tools/mir3ei_render.py` 是同一格式的两种实现)
- `.map` 的 back 层/三层语义见 `formats/FORMAT_MAP.md`(mapviewer 的 `parse_map` 读的是全分辨率单元格区,和 back 层半分辨率是同一文件的两个区)
- 瓦片金字塔/等距几何是 mapviewer 独有,无先例参考,逆向自 Mir 经典渲染器

## 7. 状态与收尾建议

- `Tools/` 下 mapviewer/wilsdk/wilviewer/wilextract 及 README **全部未提交**(`??`);已提交的只有 `Tools/CharacterEditor` 等旧目录
- `Tools/README.md` 只写了 wilviewer/wilextract,**没提 mapviewer**——建议补一节"地图浏览器",并把 `__pycache__/` 加入 .gitignore
- 若要入库:`git add Tools/mapviewer.py Tools/wilsdk.py Tools/wilviewer.py Tools/wilextract.py Tools/README.md`
