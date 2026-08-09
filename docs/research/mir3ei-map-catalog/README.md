# mir3ei — 2003 韩服 Mir3 客户端地图全图鉴

对 `/home/tetsuya/NAS/TMP/mir3ei/`(2002–2003 韩服原版 Mir3,`Mir3.exe` 2003-05-14)566 张地图的完整盘点:
**8 座城镇/据点 + ~103 野外 + ~366 洞穴地牢 + ~36 特殊/事件 + 15 空图**,其中 174 张经指纹锚定到 2017 中文版同名地图。

## 目录组织

```
docs/research/mir3ei-map-catalog/
├── mir3ei_map_catalog.html   # ★ 主交付物 — 单文件自包含图册(浏览器直接打开,6.2MB)
├── build_report.py           # HTML 生成脚本(python3 build_report.py 重跑)
├── README.md                 # 本文档
├── data/                     # 指纹/解析数据
│   ├── mir3ei_maps.json      # 566 张地图基础解析(尺寸/模式/back 层瓦片)
│   ├── mir3ei_profiles.json  # 逐地图瓦片使用统计
│   ├── mir3ei_fp.json        # 旧版 566 张指纹(7.2MB)
│   ├── zircon_fp.json        # 新版 244 张指纹(9.8MB)
│   ├── mir3ei_tileidx.json   # 瓦片帧索引
│   ├── mir3ei_match.json     # 匹配过程数据
│   ├── mir3ei_bestmatch.json # 每张 top-3 最佳匹配(sim + 新版地图名)
│   └── zircon_mapnames.txt   # 2017 中文版 244 张地图显示名(0=比奇城、D1001=潘夜神殿 Lv1 …)
├── formats/                  # ★ 素材/地图格式解码文档(供学习参考)
│   ├── FORMAT_WIL_WIX.md     # 旧版 2003 WIL/WIX 素材格式(WIX 索引/RLE 帧/有效帧规则)
│   ├── FORMAT_ZL.md          # 新版 Zl 图库格式(meta 块/DXT1/DXT5 位布局/ZL2/KROrder)
│   ├── FORMAT_MAP.md         # .map 地图格式(back 半分辨率层/14 字节单元格/三层语义)
│   └── RENDER_PIPELINE.md    # 渲染管线与验证(指纹匹配/A-B 对比/踩坑记录/重建流程)
├── tools/
│   ├── mir3ei_render.py      # 566 张地面渲染脚本(WIL 1.0 纯 back 层 24×16/格)
│   ├── make_contact.py       # 34 张家族接片图重建脚本(8 列、160×90、黄字点名)
│   └── cave_analysis.py      # WIL/Zl 解码参考(含 DXT5 正确位布局)
├── views/                    # 566 张全尺寸渲染图(12×8 px/格,189MB,gitignore)
└── contact/                  # 34 张家族接片图(160×90、8 列、黄字点名,566 张全覆盖)
```

## 关键结论

- **城镇(编号 0–8,沿用经典 Mir3 编号与 2017 版同名)**: 0 比奇城 / 1 失乐园 / 2 潘夜村 / 3 沙巴克城 / 4 努玛村 / 5 沙漠土城 / 8 南哨站;另有城镇建筑内部 ~27 张(0_001–0_0033 比奇城建筑、4_001–4_005、5_0011–5_006、d501–d515)。
- **指纹锚定(sim=1.0)**: D1101→D1001 潘夜神殿、D15011→D12011 真天宫、D8001→D404 蚂蚁洞、D900→D1401 幽灵船;D12xx 全系、num 41–44→南部沙漠、71–78→西部关隘、12/121–125→D390x 系。
- **sim 0.40 = 噪声底**: 未识别家族 = 2017 版已精简对应地图,靠视觉归类(洞穴系/沙漠/城镇/事件房)。
- **渲染器正确性闭环**: 旧 WIL 渲染 vs 新版 Zl 渲染 A/B 逐像素一致(差异仅 DXT5 有损量化 vs RLE565 噪声)。

## 素材/地图格式解码(学习参考)

`formats/` 下 4 篇文档,按阅读顺序:

1. **FORMAT_WIL_WIX.md** — 2003 旧版 `.wil/.wix` 素材:WIX 索引布局、有效帧规则(28 ≤ offs < len)、别名重映射、0xC0 RLE 行编码、565 像素
2. **FORMAT_ZL.md** — 2017 新版 `.Zl` 图库:meta 块解析、26 字节帧记录、**DXT1/DXT5 位布局(含易错点)**,ZL2 容器、KROrder 映射
3. **FORMAT_MAP.md** — `.map` 地图:半分辨率 back 层、14 字节单元格、三层语义、文件字节→图库映射
4. **RENDER_PIPELINE.md** — 从素材到图鉴的完整管线、指纹匹配判读(1.0 锚定/0.85 阈值/0.40 噪声底)、新旧 A/B 验证、渲染 bug 踩坑记录

配套可运行代码:`tools/mir3ei_render.py`(WIL 渲染)、`tools/cave_analysis.py`(Zl DXT 解码)、`GodotClient/Formats/ZlReader.cs` + `MapReader.cs`(生产版)。

## 重跑方法

```bash
# 1. 重新渲染 566 张(需要原始客户端路径;输出覆盖 views/)
python3 tools/mir3ei_render.py

# 2. 重建 34 张家族接片图(从 views/ 生成,输出覆盖 contact/)
python3 tools/make_contact.py

# 3. 重新生成 HTML 图册(内嵌缩略图与接片,自包含)
python3 build_report.py
```

## 备注

- 原始镜像 `/home/tetsuya/NAS/TMP/mir3ei/` 保持只读,分析产物全部在本目录。
- `/tmp/mir3ei_*` 留有一份同名副本供 peer 会话(map-texture)复用,可随时删除。
- 相关调查链: `docs/MAP_TILE_DESERTED_MINE_BUG.md`、`docs/RESEARCH_CAVE_TILE_BLACK_FLOOR.md`。
