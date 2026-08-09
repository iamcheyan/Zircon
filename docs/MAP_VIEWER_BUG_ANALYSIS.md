# 地图浏览工具 (Tools/mapviewer.py) 贴图错乱彻底修复报告

## 1. 核心问题重温

用户打开地图浏览工具 (`Tools/mapviewer.py`) 时，全地图平铺了重复的小圆圈和错误切片，没有正常地表纹理。

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
python3 Tools/mapviewer.py Debug/Client/Map
```
已成功准确解析 `0.map` (`350x350`) 的地表 `back_file=1` (`Tiles30c`), `back_img=721` 等真实资源，全图纹理与地表贴图已完全恢复正常！
