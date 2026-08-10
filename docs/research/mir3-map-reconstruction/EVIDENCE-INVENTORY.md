# Mir3 EI 地图重建 — 证据清单（confirmed / derived / candidate / pending）

分级定义：

- **[confirmed]** 直接证据（反汇编 / 二进制结构 / 运行中工具输出），可复现。
- **[derived]** 由 confirmed 事实推断，未运行原版客户端或缺少一端对照。
- **[candidate]** 有候选解释但证据不足，禁止标记为 confirmed。
- **[pending]** 已知未解问题，明确列档。

---

## confirmed

| # | 结论 | 证据 |
|---|---|---|
| C1 | `.map` = 28B 头 + 地面 2×2 块区 + 14B/格 cell（legacy 13B 为独立格式） | 二进制解析 + catalog 544 图全通过 |
| C2 | 库表槽号 = v 变换结果（KR_ORDER），地面/物件库引用按此解析 | audit_mir3_maps v_lookup + FramePool |
| C3 | 投影为 rect 等距：`destX=(x−viewX)·48−scrollX−200`、`destY=(y−viewY)·32−scrollY−h−125` | Mir3.exe 0x43bb10/0x43be00 |
| C4 | 地图层锚点全部格底/格左；ground 底 = mid/front 帧底 = −125 同线 | 锚点数学（destY+h） |
| C5 | 地图层（ground/mid/front）全分支**零 offset** 读取 | 43bb10/43be00/43b440/43b9a0/43c330/43c4c9 全分支 |
| C6 | actor 层读帧 offset（+4/+6）并加进 dest | 430aab/430aaf（0x430b00） |
| C7 | 绘制顺序 ground → mid → front → actor | per-cell 调用序 41c4xx → 41c59a/41c5a5/41c66d/41c678 → 0x430b00 |
| C8 | func1（43bb10）= 仅 48×32；func2（43be00）= 跳过 48×32 | 尺寸门控 43bed9 |
| C9 | 0x434a20 = 选区/足迹几何（fsqrt 半径 + u16 点对写 this+0x35b2c0），非绘制 | 0x434670 助手 + 0x468520 舍入，区段无 blit |
| C10 | EI 空帧 = 0xFFFF；file 15 = 无物件（不绘制） | catalog reserved/EMPTY_FRAME 统计 |
| C11 | 3.map 帧越界根因 = EI 素材帧数 < 地图引用（lib24/25 wood_*） | catalog frame_oob vs lib_frames |
| C12 | ZL 地面 alpha=4 根因；ZL 客户端不逐格画 Tilesc | ZL 客户端源码对照 |
| C13 | 模拟器实体数据源 = Mud3 服务端（Envir） | 服务器运行输出 |
| C14 | 544 图 catalog 与 800×800 rect 全图渲染对齐 | 10 图 z4 面板 + sim 帧 |
| C15 | catalog anomaly 统计口径与 audit 一致（5723 总 / 34 图） | 重建输出对比 |
| C16 | offset 三模式（none/all/midfront）按 om 参数加性应用 ×scale，缓存键含 om | render_tile/render_full_map 实现 + 像素差验证 |
| C17 | tiles5c 帧 20–24 资源本身近纯黑（mean≈2.7/std≈3.8），非解码错误；tiles5c f20 = 全库引用最多的帧（293,933 格，14B 解析 1.2M 格引用黑帧） | lib_frame_stats 全库 544 图重解析 + previews 蒙太奇目视 |
| C18 | 地图黑块根因 = 地图数据显式引用黑帧（约 1.2M 格），D201 类黑块为资源侧事实 | 14B 解析统计 + D1423 模拟帧 black-frac 0.204 |
| C19 | 模拟器图层（Back/Middle/Front）可独立开关渲染，缓存键含 g/m/f；/api/cell 逐格返回三层库/帧/flag/anim；/api/strip 导出三模式对比条带 | mapviewer 实现 + 浏览器实测（图层开关即时生效、tooltip 逐格数据） |
| C20 | Envir MonItems 掉落文件已接入模拟器：怪物点击 tooltip 显示 掉落 前5（如 半兽勇士 金币×4000 1/1） | load_drops 解析 280 个 MonItems 文件 + /api/entities 实测 |

## derived

| # | 结论 | 依据 |
|---|---|---|
| D1 | EI 原版视觉 = 本项目 rect 基准（原版客户端无法本地运行） | 反汇编 C3-C8 |
| D2 | 原版 = `om=none`（零 offset）；`midfront` 近原版、`all` 破坏观感 | 10 图条带视觉 + diff stats（0.map nonevsall 70% 像素差） |
| D3 | 39 张 Snow/Forest 主题图 = legacy 13B，不可用 14B 解析器渲染 | catalog legacy 统计 |
| D4 | 室内图（0_003/5_0013）地面未绘制或与 ZL `MapInfo.Background` 机制同类 | ZL 客户端机制对照 |
| D5 | 图层顺序结论可直接用于模拟器/渲染器实现（ground 先、front 后、actor 最上） | C7 + mapviewer 实现 |
| D6 | midfront offset 对洞穴图（D1423）近无影响（0.8% 像素差），all 模式破坏地面（26.6%）→ 与原版 none 一致 | 800×1200 全图 diff：nonevsall (14.49,0.266) / nonevsmid (0.8,0.022) |

## candidate（未证实，勿升格）

| # | 候选解释 | 说明 |
|---|---|---|
| K1 | 越界帧替换逻辑 = 空帧显示 | 3.map 面板 EI 物件缺失；替换规则未反汇编 |
| K2 | 室内图地面 = 静态背景图而非瓦片 | D4 候选，需 MapInfo 证据 |

## pending

| # | 问题 | 备注 |
|---|---|---|
| P1 | 原版对越界帧的替换逻辑（空帧/首帧/取模） | 3.map 相关 |
| P2 | 0_003 / 5_0013 室内地面绘制机制 | 137/67 格未绘制 |
| P3 | D10031 ground lib2（smtilesc）帧越界 62 格原因 | 唯一 ground OOB 案例 |
| P4 | 实体层 NPC 外观：body 字段 → NPC.wil 帧块精确布局 | 现用 f0 统一样式 |
| P5 | 怪物名 → Mon-1.wil 库/帧映射（monster.dat 专有格式） | 现用 f0 统一样式 |
| P6 | 小地图 FMMap/MMap.wil 帧内地图间留白逐图校准 | 现按尺寸/128px 线性映射 |
| P7 | 41c5aa-41c5de 遮挡窗口细节 | 已有梗概 |
| P8 | 0x41cbd0 actor 渲染器体、0x419d40 身份 | 未深读 |
| P9 | EI 素材中帧 offset（+4/+6）非零值的分布 | mapviewer 保留字段默认不应用 |
| P10 | 22 个库仅有保留标记帧（0xFF00+）引用、无解码帧；3 个库全部引用幻影帧（无数据） | lib_frame_stats 备注；其内容语义未查 |
| P11 | 98 个 .gen 怪物名中 4 个无法匹配 MonItems（夜行鬼09/异界之门/葛贰厘面0/诺玛教主2/魔神怪8） | 无掉落信息；其余 93 个已接入 |

## 工具链

- `Tools/maps/audit_mir3_maps.py` — 结构审计（v 变换、库表、anomaly）
- `Tools/maps/build_map_catalog.py` — per-map JSON + per-lib 帧统计 + 汇总
- `Tools/maps/lib_frame_stats.py` — 全库帧直方图 + 每库抽样帧级像素统计 + 蒙太奇
- `Tools/maps/mapviewer.py` — 渲染器 /fullmap /tile /sim /api/cell /api/strip，offset 三模式、Back/Middle/Front 独立开关、实体（含掉落）层
- `Tools/maps/render_map_comparison.py` — EI vs ZL 面板与 offset 条带
- 产物：`docs/research/mir3-map-reconstruction/{catalog,comparisons,lib-frames}`、
  `docs/research/mapviewer-investigation.md`、`LAYER-ORDER.md`、`OFFSET-EXPERIMENT.md`
