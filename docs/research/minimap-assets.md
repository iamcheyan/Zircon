# 小地图（MiniMap）素材关系调查报告

> 调查时间：2026-08-09 · 涉及：2017 ZL 客户端（`Debug/Client/`）与 2003 WIL 客户端（`/home/tetsuya/NAS/TMP/mir3ei/`）
> 背景：mapviewer（http://127.0.0.1:8899/）右上角小地图目前**没有**使用游戏自带小地图素材，需要接入。

## 1. 素材文件位置

| 客户端 | 小地图库 | 大地图/全图库 | 备注 |
|---|---|---|---|
| 2017 ZL | `Debug/Client/Data/MiniMap.Zl` | `Debug/Client/Data/Fmmap.Zl` | 帧为 2003 式小地图 |
| 2003 WIL | `/home/tetsuya/NAS/TMP/mir3ei/Data/mmap.wil` + `.wix` | `/home/tetsuya/NAS/TMP/mir3ei/Data/Fmmap.wil` + `.wix` | 与 2017 同名同用途 |

- `MiniMap.Zl` / `mmap.wil`：**小地图**（每张地图一个帧，游戏内右上角小地图用的就是它）
- `Fmmap.Zl` / `Fmmap.wil`：**大地图/全图**（按区域分块的大地图视图）

## 2. 索引机制（关键）

**每张地图的 `MapInfo` 记录里有一个 `MiniMap` 整数字段，就是 `MiniMap.Zl` 库里的帧号。**

权威代码链：

```
Client/Scenes/Views/MiniMapDialog.cs:242
    Image.Index = GameScene.Game.MapControl.MapInfo.MiniMap;
```

即：游戏客户端切地图时，读取当前地图的 `MapInfo.MiniMap`（帧索引）→ 从 `MiniMap.Zl`（`LibraryFile.MiniMap`）取对应帧 → 显示为右上角小地图。

- `LibraryFile.MiniMap` 枚举对应 `MiniMap.Zl`（见 `Client/Scenes/Views/MiniMapDialog.cs:128` 附近 `LibraryFile = LibraryFile.MiniMap`）
- 帧索引 `< 0` 表示无小地图（对话框里 `Image.Index <= 0` 有降级处理）

## 3. MapInfo 数据源

`LibraryCore/SystemModels/MapInfo.cs`：

```
public string FileName   // 如 "0.map"
public int MiniMap       // MiniMap.Zl 帧索引
```

- MapInfo 集合存在 `Debug/Client/Database/System.db`（**注意：不是 SQLite，是 .NET 二进制序列化**，`sqlite3` 打开报 "file is not a database"，首字节 `33 00 00 00 1d 4c 69 62 72 61 72 79...`）
- 服务端编辑器 `Server/Views/MapInfoView.cs` 的 `colMiniMap` 字段名即 `"MiniMap"`，说明该字段在数据库里按此名存储
- `/tmp/mud3_mapinfo.json`（365 条）是**地图中文名映射**（如 `"0": "比奇县"`），不含 MiniMap 帧索引
- `/tmp/ei_maps.json`（544 条）是文件名 + 尺寸，也不含 MiniMap 索引

## 4. 解析 System.db 的路径（未完成）

System.db 是 .NET `BinaryFormatter` 序列化，反序列化需要 .NET 类型上下文（`Library.SystemModels.BaseStat` 等）。可选方案：

1. 用 .NET（mono/dotnet）写个小工具加载 `LibraryCore` 程序集反序列化
2. 手写二进制解析（.NET 序列化格式有公开规范，可行但要处理字符串表/类型表）
3. 找客户端启动时导出的文本版 MapInfo（未发现现成的）

> 尚未验证：2017 `MiniMap.Zl` 的帧号与 2003 `mmap.wil` 的帧号是否一致（两者地图名可能不同——2017 拆分了城堡/城镇，见下）。

## 5. 地图差异警示（与本调查相关）

- 2017 `0.map`（比奇城，350×350）**无城堡**；城堡在 `10.map`（比奇城堡，500×500）——2003 的 0.map 是带城堡的（catalog `views/0.png` 可见中央青绿菱形围墙）
- 2017 与 2003 的同名地图**可能不是同一张**，MiniMap 帧索引未必能跨客户端复用，接入时需按客户端各自解析

## 6. 结论 / 已实现（2026-08-09）

- 小地图必须用游戏素材（`MiniMap.Zl` / `mmap.wil`），不能直接缩大图——**用户明确要求，已实现**
- 实现（`Tools/mapviewer.py`）：
  1. `Tools/SystemDbProbe --minimap <out>` 新增模式：dump `Debug/Client/Database/System.db` 的 MapInfo → `{FileName \t MiniMap帧号}`（244 条）
  2. mapviewer 新增 `MiniMapSource`：懒打开 `MiniMap.Zl`（data dir 下，缺失时降级 `mmap.wil`），`frame(stem)` 按映射取帧
  3. 新增 HTTP `/minimap?map=X.map`：解码帧 → JPEG（缓存头 max-age=86400）
  4. 前端 `loadMini()` 改请求 `/minimap`；404（地图无小地图）时自动降级回 `/fullmap` 最浅档
- 已实测：`0.map`→帧1、`10.map`→帧151、`11.map`→帧163 均 200；`11_001`（无记录）404→前端降级 fullmap；浏览器右上角显示游戏自带小地图（800×600）
- 14 张地图无小地图记录：`00, 11_001, D012_1..6, D1506, D29031, D29032, GM_001, Ithuejingot, Ithuejingot_WaitR`
- 2003 端（NAS）的 `mmap.wil` 已可被 wilsdk 直接读取（wilsdk 分类里已有 "mmap"、"fmmap" 条目），2017 端 `MiniMap.Zl` 由 zlsdk 读取，已验证 537 帧

## 7. EI 客户端（2003 WIL）小地图索引（2026-08-09 补充）

EI 客户端（`/home/tetsuya/NAS/TMP/EI传奇3.0客户端/`，544 图）**没有 System.db**，MiniMap 索引在 **EI 服务端** `Mud3/Envir/MiniMap.txt`（313 条，`地图名 值`）。值解释（已对渲染图逐帧验证）：

| 值范围 | 库 | 帧号 | 验证例 |
|---|---|---|---|
| `>= 1001`（大地图/城镇） | `FMMap.wil`（29 帧，1200×800 起） | `值 - 1001` | `0` 比奇城→帧0（城墙城+西南湖+北河 ✓）、`12` 潘夜岛→帧4（岛屿 ✓） |
| `< 1001`（野外/地牢） | `MMap.wil`（255 帧，600×400） | `值` | `D001` 幽灵森林→1（野外 ✓）、`D401` 迷宫→11（迷宫 ✓） |

- `10.map`（比奇城堡）、`11.map`（道馆）在 MiniMap.txt **无条目** → EI 版这两张无小地图（与 2017 不同——2017 有帧 151/163）
- 客户端存在但 MiniMap.txt 未列、或帧无内容的图 → 无小地图（约 100+ 张野外小图）
- 生成脚本：`Tools/gen_minimap_ei.py <MiniMap.txt> <Map目录> <Data目录>` → 输出 `Tools/minimap_map_ei.txt` 格式 `地图名\t库名\t帧号`（182 条）
- mapviewer `MiniMapSource` 已支持**双体系自动检测**：data dir 有 `MiniMap.Zl` → 2017 模式（`_minimap_index()`）；有 `MMap.wil` → EI 模式（`_minimap_index_ei()`）
- 两版同帧号**内容不同**（2017 帧1=比奇城森林城堡，EI FMMap 帧0=比奇城扁平色块），索引不能跨客户端复用

## 8. 服务现状（2026-08-09）

- **8898**：2017 版（258 图），`setsid nohup` 独立进程（hub 的 mvtest 反复被回收，改独立跑），http://127.0.0.1:8898/
- **8899**：EI 版（544 图），用户手动启动（`Tools/mapviewer.py /home/tetsuya/NAS/TMP/EI传奇3.0客户端/Map --data .../Data --port 8899`），http://127.0.0.1:8899/
- 同一份 `Tools/mapviewer.py` 自动适配两版数据（MiniMapSource 按 data dir 检测）
- 用户新增 hash 深链接逻辑（`#map=X.map&cur=N&x=Y&y=Y&g=0/1&o=0/1`）：静态地图 URL 可还原到指定地图/缩放/中心坐标，`updateUrlHash()` + `hashchange` 监听，已实测工作正常
