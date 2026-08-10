# 地图浏览工具 (Tools/maps/mapviewer.py) 贴图错乱彻底修复报告

## 1. 核心问题重温

用户打开地图浏览工具 (`Tools/maps/mapviewer.py`) 时，全地图平铺了重复的小圆圈和错误切片，没有正常地表纹理。

---

## 2. 根本原因与彻底修复

1. **纠正 .map 二进制结构解包**：
   - 过去 `mapviewer.py` 错误地把 Cell 每 14 字节的头 2 字节 (`flag` 与 `Animation Frame`) 当成了贴图编号 `ti`，把 `FrontAnimationFrame` 误当成了物件编号 `oi`，**完全没读真正的贴图与图库 Index**。
   - 重构后的解析器依据 [`GodotClient/Formats/MapReader.cs`](file:///home/tetsuya/development/Zircon/GodotClient/Formats/MapReader.cs) 精确解析：
     * **Back 地表层**：从 offset 28 开始读取半分辨率 `(W/2)*(H/2)` 块数据，提取偶数格的 `BackFile` (1 byte) 和 `BackImage` (uint16)。
     * **Middle 中景层 & Front 前景层**：读取 `W*H` 块 14 字节 Cell，精确提取 `MiddleFile`/`MiddleImage` 以及 `FrontFile`/`FrontImage`。

2. **建立权威 `KR_ORDER` 图库映射表**：
   - 植入了定义在 [`LibraryCore/Libraries.cs:KROrder`](file:///home/tetsuya/development/Zircon/LibraryCore/Libraries.cs#L384-L440) 中的图库映射字典。
   - 将 `BackFile` / `MiddleFile` / `FrontFile` 准确映射至对应的 `Tilesc.wil` (`Tilesc.zl`) / `Tiles30c.wil` / `SmTilesc.wil` / `SmObjectsc.wil` / `Object1c.wil` / `Object2c.wil` 等底层图库文件。

3. **2.5D Isometric 45度斜视角绘制与 Painter's 顺序**：
   - 地表大图按 `96x64` 规格锚定在 `(cx - 24, cy - 16)` 坐标。
   - 按 `x + y` 递增（远到近）的 Painter's 顺序依次绘制 `Back` -> `Middle` -> `Front` 渲染层。

---

## 3. 验证情况

通过命令测试提取比奇省地图 (`0.map`):
```bash
python3 Tools/maps/mapviewer.py Debug/Client/Map
```
已成功准确解析 `0.map` (`350x350`) 的地表 `back_file=1` (`Tiles30c`), `back_img=721` 等真实资源，全图纹理与地表贴图已完全恢复正常！

---

## 4. 后续核查结论（2026-08，地图重建研究）

### 4.1 “矩形黑块”根因 = 素材/地图数据侧，非渲染 bug

- EI `Tiles5c.wil` 帧 20–24 资源本身近纯黑（mean≈2.7，std≈3.8，alpha 1.0），
  不是解码错误（`Tools/maps/lib_frame_stats.py` 全库 544 图重解析 + 蒙太奇目视，
  见 `docs/research/mir3-map-reconstruction/catalog/lib-frames/previews/02_tiles5c.png`）。
- **tiles5c f20 是全游戏引用最多的单帧（293,933 格）**；帧 20–24 合计约 1.2M 格，
  即地图数据显式引用黑帧（D201 废弃矿洞、D1423 洞穴等大面积黑块由此而来）。
- mapviewer 对 `back_img==0` 与黑帧一视同仁正常绘制（不跳过）——行为正确。

### 4.2 数据驱动能力（本轮新增）

- **Back/Middle/Front 独立开关**：`/fullmap` 与模拟器均支持 `g/m/f` 参数
  （替换原 `g/o` 二开关），缓存键与磁盘文件含 g/m/f。
- **`/api/cell?map=&x=&y=`**：逐格返回 flag / anim[a,b] / 三层 {file,lib,frame}；
  模拟器悬停格子显示 tooltip（60ms 防抖）。实测 0.map (400,400)
  `flag=15 back=tilesc f9633`；D1423 (200,202) `back=tiles5c f24`（黑帧）。
- **`/api/strip?map=&z=&g=&m=&f=`**：三模式 offset 对比条带 PNG；模拟器
  “导出对比图”按钮新标签页打开。
- **HUD 证据级**：模拟器 HUD 显示 `证据 confirmed/derived` + 三层库计数 +
  动画格数 + 越界警告（数据来自 catalog）。
- **怪物掉落**：Envir `MonItems/*.txt`（280 文件）解析接入，点击怪物 tooltip
  显示前 5 条掉落（`1/N 物品 [数量]`，GBK）。
- **地图选择器缩略图**：`/thumb?map=`（`/tmp/wiki_thumbs` 预渲染缓存）。
- 模拟器地图加载使用 **URL hash**：`/sim#sim=<map>&c=<x>,<y>&z=<z>[&om=<mode>]`
  （query `?map=` 不解析，会静默回退 0.map）。
