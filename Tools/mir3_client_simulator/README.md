# Mir3 EI 3.0 原版客户端 UI 模拟器

以 20 年前的 EI 3.0 原版客户端（`/home/tetsuya/NAS/TMP/EI传奇3.0客户端/`）为唯一事实来源的
800×600 客户端 UI 模拟器。所有几何数据来自统一证据库
`docs/research/ei-ui-layout/`，所有贴图为 **WIL/WIX 实时解码的真实原版贴图**（无 CSS 假图、
无色块占位、无文字代替）。

## 运行方式

```bash
python3 Tools/web/wilviewer.py --root /home/tetsuya/NAS/TMP/EI传奇3.0客户端 --port 8765
# 浏览器打开 http://127.0.0.1:8765/sim/
```

`wilviewer.py` 是既有资产查看器；本模拟器作为其新路由 `/sim` 内嵌，复用其
`/api/image?f=<WIL>&i=<frame>&scale=N&bg=transparent` 实时解码管线
（`wilsdk.WilLibrary.decode(index)` → PNG）。因此**无需预导出贴图**，贴图永远是
原版客户端文件的第一手解码结果。

依赖：`python3` + Pillow（wilviewer 既有依赖）。

## 数据模型（唯一事实，坐标不散落）

| 文件 | 内容 |
|---|---|
| `data/layout.json` | 全量 bundle（windows/controls/resources/entities/equipment_slots/skills/maps/hud/viewport/meta） |
| `data/windows.json` | 14 个窗口：id、rect、frame、resource_library、evidence_level、origin 证据 |
| `data/controls.json` | 37 个控件：22 条 `specialized_control_rects` + 15 个 HUD 按钮；frame_pair、evidence 字段原样透传 |
| `data/hud.json` | HUD 底板 F50、HP/MP/EXP 条 rect、15 按钮 rect、聊天区、目标面板、小地图 |
| `data/entities.json` | 场景实体（玩家/怪物/NPC/掉落，真实库真实帧，全部标 `candidate`） |
| `data/maps.json` | 地图底 `FMMap.wil F0`、小地图 `MMap.wil F0` |
| `data/equipment_slots.json` | 装备槽（6 个，candidate） |
| `data/skills.json` | 技能格（12 个，Magic.wil 帧，candidate） |
| `data/resources.json` | 157 个资源（WIL 库 + 帧数），来自 `resource-family-catalog.json` |

生成器：`Tools/web/build_mir3_simulator_data.py`（单向、幂等），HTML/JS 只消费这些文件。
`data/layout.json` 的 `meta` 记录 `generated_by` 与规则声明：
**candidate 几何永不冒充 primary 事实**。

## 界面结构（固定 800×600 逻辑坐标）

- 整数缩放：`scale = max(1, floor(min(availW/800, availH/600)))`，`transform: scale(n)`，
  浏览器放大时只做整数等比缩放，逻辑坐标永不改变。
- 7 层：scene（地图底 + 实体精灵 + nameplate）→ hud（F50 底板 + 血蓝经验条 + 15 按钮 +
  聊天区 + 目标面板 + 小地图）→ windows（14 窗口）→ prompts（确认框/公告）→
  evidence-overlay（证据模式）→ targetbox（目标框）→ 顶部导航。
- HUD 事实（`primary-static`）：主 HUD `GameInter.wil F50` 800×136 @ `(0,465)`；
  HP 条 rect `(61,496,104,566)`、MP `(105,496,147,566)`、EXP `(61,586,400,597)`；
  聊天总区 `(224,492,578,566)`；小地图 `(672,0)-(800,128)`。
- 15 个 HUD 按钮（含帧对，HUD 相对偏移）：exchange 80/81、minimap 82/83、skill-entry 84/85、
  exit 90/91、logout 92/93、party 94/95、guild 96/97、skill 100/101、chat 102/103、
  quest 104/105、option 106/107、group 108/109、status 110/111、inventory 112/113、
  store 114/115。

## 交互

- 场景：hover 显示虚线框 + 名牌；点击怪物/玩家设为目标（目标面板 + 目标框 + 名牌常显）；
  点击 NPC 打开 NPC 对话窗。
- HUD 按钮：normal / hover（提亮）/ pressed（按压缩放）三态；打开/关闭对应窗口；
  logout/exit 弹确认框。
- 窗口：拖拽（titlebar）、关闭钮（frame pair 证据）、点击置顶、`.closed` 态隐藏。
- 背包 6×6 网格（36px/格，primary-static）、状态窗 6 装备槽 + 属性、技能窗
  （`Magic.wil` 帧）、商店窗（状态 0-4 切换，帧 `1000/1003/1001/1000/1002`）、
  聊天窗（历史 `(40,29,531,308)` + 输入 `(25,311,524,326)`，回车发消息）、
  任务窗、系统设置窗、组队/行会/坐骑窗、NPC 对话窗。
- 确认框：`GameInter.wil F950` 360×190，居中 `(400,246)`；三子按钮
  rel `[51,125,44,20]`F151/152、`[147,125,64,20]`F157/158、`[244,125,44,20]`F154/155。
- 公告：`GameInter.wil F602`，子按钮 F161/162 与 F606/607。
- 证据模式（顶部「证据模式」开关）：每个控件/窗口/实体画出矩形 + ID 标签 +
  资源库/Frame + 相对坐标 + evidence_level（primary 蓝 / candidate 橙 / pending 红）。
- 测试导航（顶部「测试导航」）：一键开/关全部 14 窗口 + 确认框/公告演示 +
  商店状态切换 + 随机地图帧。

## 已完成 / candidate / pending 清单

### 已完成（primary / primary-static / derived 证据支撑）
- 800×600 固定逻辑画布 + 整数缩放（CSS var + `transform: scale(n)`）。
- HUD：F50 底板、HP/MP/EXP 填充条（rect 与帧号来自证据）、15 按钮三态、聊天区、
  目标面板、小地图（`MMap.wil F0`）。
- 窗口框架：14 窗口全部可开/关/拖拽/置顶；已确认静态原点的窗口使用证据原点
  （guild `(102,22)`、group `(272,123)`、chat-pop `(114,76)`、option `(276,113)`、
  notice `(107,110)`）。
- 背包 6×6 网格、状态窗装备槽、技能窗、聊天窗（含输入）、确认框 F950（子按钮证据）、
  公告 F602（子按钮证据）、NPC 对话窗。
- 证据模式覆盖层、测试导航、状态栏 hover 信息。
- `/sim` 路由（含 `/sim` → `/sim/` 301 重定向）已接入 wilviewer.py，原 `/ui`、`/api/*` 不受影响。

### candidate（有资源帧，几何/语义待闭合）
- 未闭合原点的窗口（inventory/status/store/exchange/quest/option/horse/npc/skill）：
  以视口居中呈现并标 `candidate`（`data/windows.json` 每项含 origin_evidence）。
- 场景实体（玩家/怪物/NPC/掉落）坐标与帧选择：真实库真实帧（M-Hum.wil F0、
  NPC.wil F0/F1、DMon-1.wil F0/F2、Ground.wil F0），标 `candidate`。
- 商店状态 0-4 → 仓库/买卖/选中/扩展面板的业务映射：**不得仅凭 F1000-1003 画面推断**，
  保持 candidate 并在 `store-state-graph.json` 记录 pending。
- 装备槽/技能格/商店格的具体物品：演示用真实 `Equip.wil`/`Magic.wil` 帧，candidate。

### pending（需原版运行时/更多二进制证据）
- 各窗口业务内布局（格子的精确行列起点、分隔线等）多数未闭合。
- 聊天命令串（`/加入行会` 等六条原版字符串）的渲染语义。
- 商店状态 0-4 到业务的映射、包字段语义、0x00423E80 后最终父窗口原点。
- HUD 601-height 与早期公开源码 600-height 的差异：版本差异，保留笔记不擅自"修复"。

## 冒烟测试结果（2026-08-10，headless Chromium，1280×900 视口）

- 页面加载无 JS 错误；状态栏「就绪」；`windows=14 controls=37 entities=6 resources=157`。
- 贴图：117/117 张 `<img>` 全部 `naturalWidth>0`（含 HUD F50、地图底、怪物/NPC 精灵、
  窗口边框帧、按钮帧）。
- 交互（synthetic pointer 事件驱动）：怪物点击→目标面板；status/背包/技能/商店/聊天/
  任务/设置/组队/行会/交换/坐骑/NPC 对话 全部可开；背包格可选中；logout→确认框弹出并可
  关闭；证据模式 ≥30 矩形；测试导航 14 窗口按钮 + 5 工具按钮；关闭钮、窗口拖拽
  （258→319）均工作；无 console/page error。
- 截图：真实像素 HUD（金属侧板 + 圆盘 + 罗盘）、纹理窗口框、草/树/水地形、蜘蛛怪物精灵
  均清晰可见；无破图/空白。
- 已知观察：HP/MP/EXP 数值标签随演示动画同步（修复后验证通过）。

## 文件清单

```
Tools/mir3_client_simulator/
├── index.html          # 页面骨架（7 层）
├── style.css           # 800x600 固定画布、整数缩放、层/窗口/控件样式
├── app.js              # 交互引擎（纯数据驱动，无硬编码坐标）
├── data/*.json         # 统一数据模型（build 脚本生成）
└── README.md           # 本文件
Tools/web/build_mir3_simulator_data.py   # 数据生成器（单向、幂等）
Tools/web/wilviewer.py                   # 服务器 + /sim 路由（+ 既有 /ui /api）
```
