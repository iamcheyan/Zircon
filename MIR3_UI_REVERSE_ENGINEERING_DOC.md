# 20年前原版传奇 3 (Mir3 EI 3.0) 客户端 UI 界面反编译与还原指南文档

## 1. 项目背景与核心目标

本项目的目标是**100% 精确还原 20 年前原版传奇 3 (EI 3.0) 800×600 客户端 UI 操作界面的拼接原理与几何坐标排布**。

> ⚠️ **重要注意事项**：
> - **切勿参考** 现代 C# Zircon 项目中的 `Client/Scenes/Views/MainPanel.cs` 坐标代码，那是重新设计的现代宽屏布局，与 20 年前原版 EI 3.0 客户端不匹配。
> - **必须严格依据** `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/` 目录下的原版二进制可执行文件（`Mir3.exe` / `mir3.dat`）与 `Data/GameInter.wil` 素材贴图库进行反编译和分析。

---

## 2. 核心客户端文件与路径

| 文件/目录路径 | 说明 |
| :--- | :--- |
| `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/` | 原版 20 年前 EI 3.0 客户端根目录 |
| `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe` | 核心 32 位 Delphi/C++ 客户端主可执行程序 (524 KB) |
| `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/mir3.dat` | 客户端数据扩展/主逻辑二进制包 (532 KB) |
| `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/GameInter.wil` | 主界面（HUD、血球、经验条、罗盘按钮、小地图）WIL 贴图库 |
| `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/Interface1c.wil` | 辅助窗口、对话框、菜单 WIL 贴图库 |

---

## 3. `GameInter.wil` 素材贴图渲染机制解析

### 3.0 资源句柄绑定（新增一级静态证据）

主 UI 并不是通过预览器人为指定 `GameInter.wil`。`Mir3.exe` 在 `0x00427611` 将
`caller_arg+0x5898` 保存为主 UI 对象的资源字段 `main_ui_this+0x1c`，13 个主窗口
创建调用继续传递这个字段，窗口内部的 `0x00417550` 控件构造调用则继续使用包装器
收到的资源寄存器。资源初始化代码 `0x0045361D` 从 `0x0047CE0C` 指向的字符串复制
`./Data/GameInter.wil` 到 `owner+0xF848`，而 `0x00452AA0` 从 `owner+0x5898`
开始加载这组路径表。因此当前主 HUD/主窗口素材绑定到 `Data/GameInter.wil` 具有
`primary-static-handle-flow` 证据等级。

完整机器可读记录见：

```text
docs/research/ei-ui-layout/window-resource-handle-bindings.json
```

注意：这里的 `+0x5898`、寄存器传播和资源类型分派还没有经过运行时调试器确认，不能
写成 `runtime-verified`。

### 3.0.1 原版完整 WIL 路径表

继续扫描 `Mir3.exe` 的资源初始化复制序列和独立加载器参数后，恢复出 157 条路径字段。
其中 140 条来自批量资源表，17 条来自单独的加载器调用。这个结果很重要：
原版的 UI 还原不能只围绕 `GameInter.wil`，因为人物、技能、装备、背包、NPC、怪物和
商店等内容分别使用多个 WIL 资源族。

已识别的 UI/角色相关资源族包括：

| 资源族 | 原版路径 | 用途方向（仅资源族层级） |
|---|---|---|
| 主界面 | `Data/GameInter.wil` | HUD、窗口底图及主控件候选 |
| 技能/魔法 | `Data/Magic.wil`, `MagicEx.wil`, `MonMagic.wil`, `MonMagicEx.wil` | 技能图标、魔法/特效候选 |
| 物品/装备 | `Inventory.wil`, `Equip.wil`, `Ground.wil`, `StoreItem.wil`, `MIcon.wil` | 背包、装备、地面物品、商店物品、图标候选 |
| 角色外观 | `M-Hum.wil`, `M-Weapon1.wil`–`M-Weapon4.wil`, `M-Hair.wil`, `M-Helmet1.wil`及 WM 对应族 | 人物及装备外观候选 |
| NPC/怪物 | `NPC.wil`, `Mon-1.wil`–`Mon-20.wil`, `MonS-1.wil`–`MonS-20.wil` | NPC、怪物及静态图像候选 |
| 地形 | 普通、`Wood/`、`Sand/`、`Forest/`、`Snow/` 下的 tiles/object 族 | 地图场景资源 |

机器可读结果：

```text
Tools/extract_mir3_resource_path_table.py
docs/research/ei-ui-layout/resource-path-table.json
```

记录中的 `owner+偏移` 是原版对象字段的静态写入位置，不等于已经确认的窗口名称。
后续必须把这些字段与 WIL Frame 使用点、窗口构造函数和绘制调用交叉验证；不能仅按
Frame 编号或文件名猜测人物/技能/装备窗口。

### 3.0.2 原始库容量索引

路径表已与原始 `.wil/.wix` 配对并解析头部。当前资料库中实际存在 89 组 WIL/WIX；主要
资源规模如下：`GameInter.wil` 为 1103 个 Frame 槽位/253 个非空帧，`Magic.wil` 为
3550/1948，`Inventory.wil` 为 1440/499，`Equip.wil` 为 1320/125，`MIcon.wil` 为
1106/138，`NPC.wil` 为 6400/1994，`StoreItem.wil` 为 1440/490。

完整机器可读索引：

```text
Tools/build_mir3_resource_family_catalog.py
docs/research/ei-ui-layout/resource-family-catalog.json
```

非空帧数量只能说明资源确实存在，不能说明它一定会出现在主 HUD 或某个窗口中；窗口
名称、绘制顺序和状态仍必须由 `Mir3.exe` 的调用链继续确认。

### 3.0.3 `mir3.dat` 交叉验证

`mir3.dat` 本身也是 PE32 GUI 可执行文件，时间戳为 2002-10-25，入口点为
`0x0046A882`。对它单独反汇编后，恢复出与 `Mir3.exe` 完全一致的 157 条资源路径记录，
路径名和对象字段偏移逐条相同。该结果支持资源表属于原版客户端初始化结构，而不是只
来自某一个文件的偶然字符串匹配。

交叉验证产物：

```text
docs/research/ei-ui-layout/mir3-dat-resource-path-table.json
```

注意：这只验证资源路径表，不代表两个 PE 的全部窗口函数、坐标和绘制顺序无需分别检查。

### 3.0.4 Interface1c 前置角色界面候选

`0x00456A90`/`0x00456CB0` 初始化了一个 640×480 状态界面：Interface1c Frame 50 是
全屏背景候选，Frame 51–58、86–90、92–99 组成 9 组固定按钮，坐标已从原版构造调用恢复。
其调用者 `0x00402989` 和 `0x00419C0A` 都把 `ECX=0x008A7140` 作为同一状态对象，因此
这些控件很可能属于角色选择/创建角色阶段，而不是游戏内主 HUD。

这个判断目前仍是候选，原因是尚未完成运行时输入路径和中文按钮文字的程序状态确认。完整
上下文与 Frame 头部记录见：

```text
Tools/analyze_mir3_interface1c_parent.py
docs/research/ei-ui-layout/interface1c-parent-context.json
```

另一个启动阶段初始化器 `0x004026E0` 在 `0x004020A8` 被调用，加载同样的
GameInter/Interface1c 资源对象，并构造 Frame 11、13、15、17 四个固定按钮。原始素材
视觉上属于角色选择操作族，因此记录为独立的 `character-selection-screen` 候选；它不能
与上述 9 按钮的角色创建界面合并。

```text
Tools/analyze_mir3_interface1c_select_screen.py
docs/research/ei-ui-layout/interface1c-select-screen-context.json
```

## 4. 主窗口素材的视觉语义（辅助证据）

对原始 `GameInter.wil` 窗口底图进行解码后，可把静态窗口表与素材形态做如下分层对应：

| Frame | 视觉语义候选 |
|---:|---|
| 250 | 背包网格 |
| 400 | 技能书/技能界面 |
| 700 | 任务卷轴 |
| 750 | 系统选项 |
| 850 | 坐骑界面 |
| 900 | 组队 |
| 1000 | 商店物品列表 |
| 1050 | 交易 |
| 1100–1102 | NPC 对话底图及状态帧 |

Frame 200 仍记录为装备/人物状态候选，Frame 600 仍记录为行会/社交管理候选。视觉
形态是辅助证据，不能替代窗口构造函数和运行时状态机确认。

```text
Tools/annotate_mir3_window_visual_semantics.py
docs/research/ei-ui-layout/window-frame-visual-semantics.json
```

窗口内部控件也建立了辅助功能分组：技能书控件、商店操作、行会/社交页签、系统选项、聊天
频道、组队、坐骑、任务翻页、NPC 对话标记和交易按钮等。它们保留原始 `call_va`、Frame 对、
坐标和尺寸，分组只作为后续文本/输入路径分析的索引。

```text
Tools/annotate_mir3_control_semantics.py
docs/research/ei-ui-layout/control-semantic-catalog.json
```

### 4.1 技能书窗口的原版分类文字

窗口 Frame 400 的构造函数 `0x00439250` 附近直接出现以下原版字符串：
`火`、`冰`、`电`、`风`、`神圣`、`黑暗`、`幻影`、`剑`。它们分别绑定到
`0x00439334`、`0x0043935D`、`0x00439386`、`0x004393B3`、`0x004393E0`、
`0x0043940D`、`0x00439437`、`0x00439464` 控件构造调用；同一窗口还使用
`Magic.exp`。因此 Frame 400 不只是“看起来像技能书”，而是有一级静态文本证据支持
其元素/流派分类功能。

完整记录：

```text
Tools/analyze_mir3_skill_window.py
docs/research/ei-ui-layout/skill-window-context.json
```

技能名称、等级和具体页签切换仍需从 `Magic.exp`/相关数据加载与输入处理路径继续确认。

技能窗口的独立静态证据记录见：

`docs/research/ei-ui-layout/skill-window-static-evidence.md`

随后从窗口重绘函数 `0x00439500` 恢复了八个分类控件的最终窗口相对坐标：
火 `(5,21)`、冰 `(3,56)`、电 `(4,91)`、风 `(2,126)`、神圣 `(2,161)`、黑暗
`(2,196)`、幻影 `(1,231)`、剑 `(2,266)`。这些数值是在每次刷新时重新写入控件
位置的静态证据，不是人工拖拽值；窗口屏幕原点仍需单独恢复。

技能列表的读取与绘制循环也已定位到 `0x0043A440`：首行从窗口原点偏移 `(15,235)`
开始，行距固定为 15 像素，数据记录缓冲区步进为 `0x104`。该循环识别 `#` 段标记、
跳过 `;` 行，并通过 `0x0045E200` 解析后使用 `0x0045DBA0/0x0045DD70` 绘制多列
字段。详细机器记录见：

`docs/research/ei-ui-layout/skill-window-render-loop-evidence.json`

特别注意：原版代码引用的 `Magic.exp` 是客户端根目录裸文件名，实际路径为
`/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Magic.exp`；不要写成 `Data/Magic.exp`。
`/home/tetsuya/NAS/TMP/Mud3/Envir/magic.dat` 是另一套服务端数据，必须在文档和
工具输出中独立标记来源。

进一步的静态追踪已经确认技能窗口初始化函数 `0x00439150` 会准备 16 字节初始化
数据，经 `0x00452580` 后调用 `0x0046926D` 加载 `Magic.exp`，最终把数据句柄保存
到 `this+0x968`；析构函数 `0x00439220` 会释放它。这个结果只证明客户端加载链，
尚未证明加密算法和字段语义，详见：

`docs/research/ei-ui-layout/skill-window-static-evidence.md`

当前已补充两个可直接装配的窗口证据集：

- 背包 Frame 250：三组子控件（161/162、264/265、267/268）及物品绘制路径中的 36 像素网格步长，见 `inventory-window-render-evidence.json`。
- 人物状态 Frame 200：原版 `SetRect` 直接恢复的装备槽、人物图像区域和属性区域，见 `status-window-render-evidence.json`。

这两组坐标都是窗口内部坐标，最终屏幕位置必须再加窗口对象 `this+0x18/this+0x1c`，
并保留当前窗口移动状态；Frame 200/250 的业务名称和个别资源句柄仍按证据等级标注，
不能因为视觉相似就升级为确定语义。

### 3.1 控件坐标的恢复方法

窗口内部控件的坐标不是手工拖拽校准得到的。对每个 `0x00417550` 调用，提取器按
x86 调用约定识别 `arg4=x`、`arg5=y`，并在对应 `push` 指令发生的瞬间保存寄存器
表达式。这样可以避开调用前 `lea ecx,[window+offset]` 把寄存器改写成控件 `this`
指针造成的误判。

当前结果：72 个主窗口控件中 65 个拥有完整 x/y 绝对候选，51 个通过窗口矩形几何
检查；60 个同时能在 `GameInter.wil` 找到尺寸并生成 `SetRect` 命中框。其余记录仍
保留表达式、原始指令和未解析状态。

全局扫描还保留了原版全部 109 个控件构造调用，其中 22 个暂未绑定到主窗口：

```text
docs/research/ei-ui-layout/global-control-constructor-catalog.json
```

预览器中的红色框表示“坐标候选落在窗口外”，虚线框表示尺寸或坐标仍未闭合；这些
视觉标记是证据状态提示，不代表我们已经确认了业务名称或最终绘制顺序。

目前已经闭合一个独立的 Interface1c 控件簇：

```text
Interface1c Frame 11: (459,436,96,24)
Interface1c Frame 13: (139,379,96,26)
Interface1c Frame 15: (279,379,96,26)
Interface1c Frame 17: (439,379,48,26)
```

其资源对象为 `owner+0x5B0`，路径由 `0x0047AAA0` 指向 `Data/Interface1c.wil`。
完整证据见 `docs/research/ei-ui-layout/interface1c-cluster-4027.json`；这些记录已经
进入 `layout.json.secondary_control_constructors`。

另一个已闭合的 Interface1c 资源簇位于 `0x00456DC1–0x00456EC8`，资源对象为
`owner+0x14C`，由 `0x00456CB0` 初始化。该簇包含 9 个控件，Frame 范围包括
51、53、55、57、86–90、92–98；完整记录见：

```text
docs/research/ei-ui-layout/interface1c-cluster-456d.json
```

这两个簇共 13 个次级控件已经进入统一 layout，但暂时不能仅凭静态代码给它们命名为
人物、装备、技能或其它具体业务窗口。

另外，主 UI 初始化在 `0x0042797E` 调用了额外窗口构造函数 `0x0043E260`，使用
`GameInter.wil` Frame 602，并包含 Frame 161/162 与 606/607 两个控件，静态命中框
候选分别为 `(655,16,28,26)` 和 `(603,27,40,20)`。由于该包装器的容器参数槽位仍
在复核，Frame 602 的窗口矩形暂不作为已确认布局使用。记录见：

```text
docs/research/ei-ui-layout/gameinter-cluster-43e260.json
```

通过对 `Mir3.exe` 反汇编和 `GameInter.wil` 位图结构分析得出：

1. **主背板底座 (`Frame 50`)**：
   - 尺寸为 **800 × 136 像素**。原版 `0x00427600` 的二进制算式为
     `top = 601 - height`，当前一级静态证据是 `(0, 465)` 到 `(800, 600)`。
   - 该贴图是一张完整一体化的底图，内部已经包含了右侧九宫格罗盘、中间聊天框底、左侧大血球透明套槽的图样。
2. **双血球 (`Frame 60` & `Frame 61`)**：
   - **`Frame 60`** / **`Frame 61`** (各 56×110 像素): 血球/魔球候选；素材尺寸已确认，
     具体绘制坐标和绘制顺序仍待绘制调用闭合，不能把旧预览中的 `(59,480)` / `(115,480)`
     当成已验证坐标。
3. **经验进度条 (`Frame 63`)**：
   - 尺寸为 **164 × 6 像素**；具体绘制坐标仍待原版绘制调用闭合。
4. **罗盘扇形切片按钮 (`Frame 100 ~ 115`)**：
   - 包括属性 [100]、装备 [101]、背包 [102]、技能 [105]、组队 [104]、任务 [103]、帮助 [108]、腰带 [107]、快捷 [106]、好友 [111]、挂机 [110]、设置 [112] 和中心锁扣 [109]。
   - 二进制确认其为 15 组普通/状态 Frame 对，尺寸为 40×38；候选偏移和 `Frame 50`
     底图位置已记录在 `layout.json`。平态是否完全由 `Frame 50` 提供、以及每个编号对应
     的业务名称，仍需运行时绘制/输入路径确认。

---

## 4. 精确物理坐标与控件框矩形表 (Rect Table)

以下表格是历史预览中的坐标草案，不是当前统一目录的最终证据。当前一级证据只把
`Frame 50` 的矩形确定为 `(0,465,800,136)`，以及把按钮偏移记录为相对
`[esi+0xc58]/[esi+0xc5c]` 的表达式；窗口内部控件仍必须经过句柄和位置追踪。

在 800×600 标准屏幕下的历史坐标草案（控件基准点 `(0, 465)`）：

| 控件名称 | 对应 WIL Index | 绝对坐标 X | 绝对坐标 Y | 宽度 W | 高度 H | 说明 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 主框架底座 | `GameInter[50]` | 0 | 465 | 800 | 136 | 一体化 HUD 金属底座；一级静态证据 |
| HP 红血球 | `GameInter[60]` | 59 | 480 | 56 | 110 | 左半红血球 |
| MP 蓝魔球 | `GameInter[61]` | 115 | 480 | 56 | 110 | 右半蓝魔球 |
| 经验进度条 | `GameInter[63]` | 350 | 475 | 164 | 6 | 顶部黄色经验条 |
| 聊天框区域 | - | 200 | 484 | 380 | 100 | 文本信息与输入热区 |
| 属性按钮 [F10] | `GameInter[100]`| 648 | 476 | 26 | 24 | 罗盘弧形热区 1 |
| 装备按钮 [F11] | `GameInter[101]`| 668 | 482 | 26 | 24 | 罗盘弧形热区 2 |
| 背包按钮 [F9]  | `GameInter[102]`| 682 | 496 | 26 | 24 | 罗盘弧形热区 3 |
| 技能按钮 [F3]  | `GameInter[105]`| 686 | 516 | 26 | 24 | 罗盘弧形热区 4 |
| 组队按钮 [F4]  | `GameInter[104]`| 682 | 536 | 26 | 24 | 罗盘弧形热区 5 |
| 任务按钮 [F7]  | `GameInter[103]`| 668 | 552 | 26 | 24 | 罗盘弧形热区 6 |
| 帮助按钮 [?]   | `GameInter[108]`| 648 | 556 | 26 | 24 | 罗盘弧形热区 7 |
| 腰带按钮 [Z]   | `GameInter[107]`| 628 | 552 | 26 | 24 | 罗盘弧形热区 8 |
| 快捷按钮 [R]   | `GameInter[106]`| 614 | 536 | 26 | 24 | 罗盘弧形热区 9 |
| 好友按钮 [F]   | `GameInter[111]`| 610 | 516 | 26 | 24 | 罗盘弧形热区 10 |
| 挂机按钮 [A]   | `GameInter[110]`| 614 | 496 | 26 | 24 | 罗盘弧形热区 11 |
| 设置按钮 [ESC]| `GameInter[112]`| 628 | 482 | 26 | 24 | 罗盘弧形热区 12 |
| 中心锁扣按钮 | `GameInter[109]`| 651 | 518 | 28 | 28 | 罗盘圆心挂锁/退出 |

---

## 5. 素材预览与 UI 组装工具使用说明

系统内置的 WIL 素材查看与 UI 组装预览 Web 工具为 `Tools/wilviewer.py`。

### 1) 启动 Web 服务
```bash
python3 Tools/wilviewer.py --root /home/tetsuya/NAS/TMP/EI传奇3.0客户端 --port 8765
```

### 2) 关键功能点
- **网址 URL 自动记忆**：侧边栏选择 WIL 文件时，地址栏会自动更新 Hash，如 `http://127.0.0.1:8765/#file=GameInter.wil`。
- **UI 组装弹窗自动记录**：在 Modal 开启状态下刷新网页，URL Hash 会追加 `&hud=1` 并且通过 `localStorage` 自动重新弹出 **`🖥️ UI 组装预览`** Modal 视口。
- **红框碰撞检测开关**：弹窗顶部右侧自带 **`[x] 显隐控件碰撞红框`** 勾选项，点击可开启/隐藏所有控件边界框。
