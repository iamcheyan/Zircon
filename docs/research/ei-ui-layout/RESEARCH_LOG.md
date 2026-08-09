# EI 3.0 原版 UI 反编译持续研究日志

本日志记录反编译过程中的发现、推理依据、失败尝试和待验证事项。它和机器可读数据同等重要：后续继续分析时，任何结论都应能追溯到本日志中的原始地址、资源文件或交叉来源。

## 2026-08-09：建立原版 UI 证据链

### 研究目标

恢复 20 年前 EI 3.0 传奇 3 客户端的完整 800×600 UI。当前范围不仅是底部操作栏，还包括主 HUD、人物状态、装备、背包、技能、任务、地图、小地图、聊天、NPC、商店、仓库、组队、行会、好友、系统菜单和各种弹窗。

### 第一证据源

```text
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/mir3.dat
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/*.wil
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/*.wix
```

已确认的关键资源：

- `Data/GameInter.wil`：主 HUD 和大量窗口/按钮资源，当前库计数 1103。
- `GameInter.wil` Frame 50：原版资源尺寸 `800×136`，可以作为底部 HUD 背板证据。
- `GameInter.wil` Frame 60/61：`56×110`，血球/魔球候选。
- `GameInter.wil` Frame 63：`164×6`，经验条候选。
- `GameInter.wil` Frame 67：`4×70`，重量条候选。
- Frame 80–85：`24×16`。
- Frame 90–97：`28×26`。
- Frame 100–115：`40×38`，与右侧罗盘入口按钮组吻合。

资源尺寸由 `Tools/build_mir3_ui_resource_metadata.py` 从原版 WIL 头部读取，输出到 `gameinter-frame-metadata.json`。尺寸不是人工测量，也不是现代客户端推断。

### 发现一：固定控件初始化函数

在 `Mir3.exe` 中定位到：

```text
VA 0x00417550
```

该函数不是普通业务函数。静态分析显示它：

1. 从调用参数读取资源对象和两个连续 Frame 编号；
2. 把资源/状态字段写入 `this` 对象；
3. 保存两个位置参数；
4. 读取 WIL 当前帧的宽高；
5. 通过 `SetRect` IAT（`VA 0x004762B0`；USER32 import descriptor 的 IAT RVA 为 `0x76234`，SetRect 是该表第 31 项）建立控件的命中区域。

因此，本项目暂时把 `0x00417550` 命名为“原版 UI 控件初始化候选”，证据等级为 `primary-static-control-initializer`。还没有把它命名成源码中的具体类名，因为 EI 二进制没有符号。

### 发现二：底部 HUD 有连续的 16 组按钮初始化

原版二进制在 `0x004279B2` 至 `0x00427D94` 之间连续调用上述函数。每组调用都有连续 Frame 对，且 X 坐标常量与早期 Mir3 源码一致：

| 调用 VA | Frame | 二进制 X 偏移 | 二进制 Y 偏移 |
|---|---:|---:|---:|
| `0x4279B2` | 80/81 | 204 | 2 |
| `0x4279E6` | 82/83 | 228 | 2 |
| `0x427A1A` | 84/85 | 252 | 2 |
| `0x427A4E` | 90/91 | 161 | 46 |
| `0x427A82` | 92/93 | 161 | 82 |
| `0x427AB6` | 94/95 | 616 | 47 |
| `0x427AEA` | 96/97 | 616 | 82 |
| `0x427B58` | 100/101 | 703 | 16 |
| `0x427BAA` | 102/103 | 718 | 32 |
| `0x427BFC` | 104/105 | 718 | 70 |
| `0x427C4D` | 106/107 | 703 | 85 |
| `0x427C9F` | 108/109 | 664 | 86 |
| `0x427CF1` | 110/111 | 648 | 70 |
| `0x427D42` | 112/113 | 648 | 32 |
| `0x427D94` | 114/115 | 665 | 16 |

这些位置不是裸数字。实际形式是：

```text
X = [esi + 0xc58] + 常量
Y = [esi + 0xc5c] + 常量
```

`[esi+0xc58]` 和 `[esi+0xc5c]` 的高层字段名仍待继续追踪；目前不能直接把它们叫作 `main.left/top`，只能叫二进制中的 X/Y 基准字段。

### 发现三：x86 参数顺序容易造成误判

早期提取器针对 `VA 0x00449C50` 的三 WORD helper 最初按机器码出现顺序记录参数，导致字段顺序反了。x86 调用者是反向压栈：

```text
push value3
push value2
push value1
push pointer
call helper
```

现已在 `Tools/extract_mir3_ui_layout.py` 中反转还原，并在 JSON 中保留 `raw_pushes` 作为原始证据。这个 helper 的字段语义仍未确认，所以 93 条记录继续标记为 `static-initializer-candidate`，不能当成屏幕 Rect。

### 发现四：源码交叉参考与 EI 不完全一致

公开的早期 Mir3 C++ 源码给出相同的 Frame 对和 X 偏移，说明 EI 与该早期版本有很强的结构关联。但源码中的 Y 偏移与 EI 二进制不完全一致，例如源码写作 `main.top+34` 的技能按钮，在 EI 二进制中表现为 `[esi+0xc5c]+16`。

当前处理原则：

- 帧对和 X 偏移同时被 EI 二进制确认，可以记录为 `primary-static`，并标注源码交叉吻合。
- Y 偏移只采用 EI 反汇编表达式，不强行套源码。
- 公开源码记录统一标记为 `secondary-source-hypothesis`，不能替代 EI 证据。
- 未确认的字段名、按钮业务名称和最终屏幕绝对坐标都不能写成“已确定”。

### 发现五：窗口基类和窗口创建簇

在 `0x00423B30` 处确认了通用窗口基类初始化逻辑。它调用 `0x00466130` 选择 Frame，然后用 Frame 的宽高设置图像矩形；如果调用者传入显式宽高，则另外保存窗口矩形尺寸。这个函数与早期源码中的 `CGameWnd::CreateGameWnd` 结构对应。

主 UI 初始化函数 `0x00427600` 附近出现一组连续窗口创建调用。当前已解析出：

```text
ID 0  Frame 250   x=518 y=0   w=284 h=324  movable=1  背包候选
ID 1  Frame 200   x=0   y=0   w=244 h=328  movable=1  人物状态候选
ID 2  Frame 1000  x=0   y=0   w=300 h=304  movable=0  商店/仓库候选
ID 3  Frame 1050  x=0   y=0   w=484 h=330  movable=1  交易候选
ID 4  Frame 600   x=102 y=22  w=596 h=446  movable=1  行会候选
ID 6  Frame 900   x=272 y=123 w=256 h=244  movable=1  组队候选
ID 8  Frame 350   x=114 y=76  w=572 h=388  movable=1  聊天弹窗候选
ID 7  Frame 200   x=560 y=0   w=244 h=328  movable=1  附属面板候选
ID 12 Frame 750   x=276 y=113 w=248 h=264  movable=1  选项候选
ID 11 Frame 700   x=0   y=0   w=340 h=440  movable=1  任务候选
ID 13 Frame 850   x=0   y=0   w=296 h=332  movable=1  马匹候选
ID 14 Frame 400   x=0   y=0   w=296 h=332  movable=1  其他窗口候选
ID 9  Frame 1100  x=0   y=0   w=552 h=176  movable=0  NPC 对话候选
```

其中 `250/200/900/350/750/700/850` 与早期源码资源族吻合；`1000/1050/1100` 是 EI 二进制直接出现的编号，暂时不能用源码中的 `253/251/300` 替换。窗口表和原始调用邻域分别保存于 `window_layout.json`、`window_init_candidates.json` 和 `primary-window-init-evidence.md`。

### 发现六：按钮命中矩形可以由原版自动恢复

对 `VA 0x00417550` 的完整反汇编确认了此前的关键推断：

```text
this+0x14 -> 当前 WIL 资源对象
this+0x28 -> position_x
this+0x2c -> position_y
this+0x04 -> RECT
```

函数在资源有效时读取当前 Frame 的宽高，并调用 `USER32.SetRect` IAT `0x004762B0`，把位置与宽高组合成命中矩形。随后控件鼠标处理函数 `0x00417780`、`0x004177C0`、`0x004177F0` 分别在 `0x00417791`、`0x004177D1`、`0x00417802` 通过 `PtInRect` IAT `0x004762B4` 测试 `this+0x04`。

这证明“手动拖动校准按钮命中区域”不是必要路线。正确路线是：提取原版控件的位置参数，读取对应 WIL Frame 的尺寸，再合并所属窗口基准位置。尚未确认所属窗口基准的位置时，仍必须把结果标记为相对坐标。

调用点提取器和证据说明：

```text
Tools/extract_mir3_ptinrect_calls.py
docs/research/ei-ui-layout/ptinrect_calls.json
docs/research/ei-ui-layout/primary-ptinrect-evidence.md
```

证据边界：`SetRect`/`PtInRect` 的调用关系是一级静态证据；具体业务名称、窗口归属和透明像素轮廓尚未全部确认。WIL Frame 的外接矩形可能大于可见像素区域。

### 发现七：窗口 Frame 的资源头与窗口矩形是两套数据

从原版 `Data/GameInter.wil/.wix` 读取窗口候选 Frame 的 17 字节头部后，发现资源头尺寸与 `0x00427600` 传给通用窗口初始化器的显式窗口尺寸并不总是相同。例如：

| Frame | WIL 头部 width×height | 窗口初始化显式尺寸 |
|---:|---:|---:|
| 200 | 256×512 | 244×328 |
| 250 | 512×512 | 284×324 |
| 350 | 1024×512 | 572×388 |
| 600 | 1024×512 | 596×446 |
| 700 | 512×512 | 340×440 |
| 750 | 256×512 | 248×264 |
| 850 | 512×512 | 296×332 |
| 900 | 256×256 | 256×244 |
| 1000 | 512×512 | 300×304 |
| 1050 | 512×512 | 484×330 |
| 1100 | 512×256 | 552×176 |

这里的“WIL 头部尺寸”来自 `Tools/wilsdk.py` 的原始解码，不是把窗口尺寸倒推出来的；“显式尺寸”来自窗口创建调用的参数。二者不能混为一谈：前者是资源绘制/解码矩形，后者是窗口容器或裁剪/交互区域候选。Frame 145、202、251、253、254、255 在 `GameInter.wil` 中为空或不存在，早期源码中相同数字的含义不能直接覆盖 EI 的资源编号。

这一差异是后续还原完整窗口时的重要约束：预览器必须同时显示 `resource_rect` 与 `window_rect`，并记录两者的证据来源。

### 发现八：窗口构造尺寸与非透明像素边界高度相关

使用 `Tools/analyze_mir3_window_resources.py` 解码 GameInter.wil，并对每个窗口候选取 RGBA 非透明像素的外接框，得到如下对照：

| 窗口候选 | Frame | 非透明像素框 | 构造尺寸 | 差值（构造−像素框） |
|---|---:|---:|---:|---:|
| inventory | 250 | 281×324 | 284×324 | +3×0 |
| status | 200 | 241×327 | 244×328 | +3×1 |
| chat-pop | 350 | 570×387 | 572×388 | +2×1 |
| guild | 600 | 594×445 | 596×446 | +2×1 |
| quest | 700 | 340×439 | 340×440 | 0×1 |
| option | 750 | 248×273 | 248×264 | 0×−9 |
| exchange | 1050 | 483×330 | 484×330 | +1×0 |

这不是偶然的尺寸相等：多个窗口的显式尺寸与非透明边界只差 0–3 个像素。当前最合理的解释是窗口构造尺寸和资源的有效内容/裁剪区域有关，但仍不能仅凭尺寸确定绘制原点、透明边缘是否参与命中或窗口是否使用了额外的内部裁剪。

Frame 1100 的非透明边界与当前 `552×176` 构造参数差异较大，因此 NPC 窗口候选暂时降级为“资源/调用关联已确认、尺寸语义待追踪”，不能套用其他窗口的规律。

机器可读结果：

```text
docs/research/ei-ui-layout/window-resource-analysis.json
```

### 发现九：建立数据驱动的证据布局预览页

为避免旧版 HUD 拆解页中的手写红框继续被误认为原版坐标，`Tools/wilviewer.py` 新增：

```text
/ui                 固定 800×600 证据布局页面
/api/ui-layout      返回 layout.json 与窗口资源边界分析
```

页面当前行为：

- Frame 50 按二进制主 HUD 结果放置在 `(0,465)`；
- 15 个按钮读取 `layout.json` 的 Frame、相对位置和 WIL 尺寸；
- 13 个窗口候选读取构造坐标/尺寸；窗口资源图像用非透明边界对齐仅作为“资源裁剪推断”显示；
- 坐标/命中框开关、Frame 标签开关和 localStorage 状态记忆已实现；
- 每条记录显示证据等级，未确认业务名称不会被自动升级。

离线验证已通过：布局记录 28 条（15 按钮 + 13 窗口），窗口资源分析 13 条，页面模板可正常加载。由于当前执行沙箱禁止绑定本地 TCP 端口，HTTP 访问只能在用户本机启动服务后验证；这不影响静态数据和 Python 语法检查。

### 发现十：资源 Frame 选择调用的索引入口

原版二进制中 `VA 0x00466130` 被大量调用，调用形式通常是先把资源对象放入 `ECX`，再压入一个 Frame 编号。该函数与窗口构造和控件初始化中的资源选择行为一致，因此新增了初步索引器：

```text
Tools/extract_mir3_resource_select_calls.py
docs/research/ei-ui-layout/resource_select_calls.json
```

索引器只提取调用点、最近压栈参数、`ECX` 设置和 Frame 候选；它没有把所有调用升级为绘制调用。后续应以窗口构造函数 `0x00423B30` 的调用者为入口，沿每个窗口类函数继续追踪 `0x00466130` 后的资源绘制/裁剪调用，以建立真正的 `draw-call` 顺序。

### 发现十一：窗口内部也复用了同一个控件初始化器

对已确认的 13 个窗口包装函数进行函数体范围扫描后，发现它们内部多次直接调用 `0x00417550`。例如背包候选包装函数 `0x0042EA80` 中至少有：

| 调用 VA | Frame 对候选 | 控件对象字段 |
|---|---:|---|
| `0x0042EADB` | 161/162 | `[esi+0x5C]` |
| `0x0042EB07` | 264/265 | `[esi+0x110]` |
| `0x0042EB2D` | 267/268 | `[esi+0x1C4]` |

状态窗口 `0x0044B130` 也出现 161/162、171/172 等局部按钮对；商店候选 `0x0044D310` 出现 1010–1017 的连续状态帧。它们不是底部 HUD 的 80–115 组按钮，说明完整 UI 还需要按窗口对象分别建立控件表。

机器可读产物：

```text
Tools/extract_mir3_window_controls.py
docs/research/ei-ui-layout/window-control-calls.json
```

这些 Frame 对和对象字段是一级静态证据；控件的最终相对坐标仍需从调用前的位置参数、窗口基准和 WIL 有效边界继续解析。

当前清理后的扫描结果为 72 个窗口内部控件构造调用，其中 70 个具有连续 Frame 对候选。提取器现在按“当前 `0x00417550` 调用之前的上一个调用边界”截取参数块，避免把相邻控件的 Frame 对串入当前记录；原始指令和压栈参数仍全部保留。

这些控件构造调用已通过 `Tools/enrich_mir3_layout_evidence.py` 写入统一 `layout.json` 的 `control_constructors` 字段。字段明确保留 `position=null`、`size=null` 和未解析资源句柄，防止预览器把 Frame 对误画成已经确认的屏幕坐标。

进一步根据 `0x00417550` 的栈访问和 `ret 0x24` 确认了参数槽位：调用者按 `arg9..arg1` 压栈；`arg1` 是资源对象，`arg2/arg3` 是普通/状态 Frame，`arg4/arg5` 是控件的 X/Y，后续参数是控件标志和附加字段。窗口控件 JSON 现在保留这些命名槽位。X/Y 的寄存器值仍可能是窗口基准加偏移，尚未把寄存器表达式误算成绝对屏幕坐标。

新增 `Tools/resolve_mir3_control_positions.py`，从窗口包装函数入口到每次 `0x00417550` 调用执行有限的寄存器/ESP 符号追踪。结果同时保存：

- 原始 X/Y 参数寄存器；
- `window.x + offset` / `window.y + offset` 表达式；
- 可安全代入的绝对坐标候选；
- X/Y 轴向一致性；
- 是否落在对应窗口容器内。

当前 72 条记录中，36 条得到轴向一致的绝对候选，其中 29 条落在窗口几何范围内、7 条被标记为超出窗口；其余仍是表达式或待验证状态。这个数量不是“已完成坐标数”，而是静态解析器通过的保守子集。

机器可读结果：

```text
docs/research/ei-ui-layout/window-control-position-analysis.json
```

证据预览页 `/ui` 已增加橙色窗口内部控件调试层：只有 `geometric_status=inside-window` 的位置候选进入该层；它显示 Frame 对和候选矩形，但不改变 `layout.json` 中“资源句柄未解析”的证据等级。

### 发现十二：窗口控件 Frame 存在跨 WIL 库编号重用

窗口控件 Frame 不能只按数字在一个库中查找。比如 `264/265` 在 GameInter.wil 中是 `64×20`/`64×20`，在 Interface1c.wil 中则是完全不同的图像；`267/268` 在 GameInter.wil 中为空，但在 Interface1c.wil 中存在有效图像。相同编号在两个 WIL 库中被重用，说明必须继续追踪窗口包装函数收到的资源对象句柄，不能仅凭 Frame 编号猜库。

已生成跨库交叉检查：

```text
Tools/analyze_mir3_control_resources.py
docs/research/ei-ui-layout/window-control-resource-analysis.json
```

这一步只证明“某库中存在该编号及其尺寸”，不证明实际绘制使用了哪个库；资源句柄追踪仍属于下一阶段一级证据工作。

### 发现十三：确认资源对象选择器与 WIL 加载辅助函数

反汇编显示：

```text
0x004660E0  资源/WIL 加载或重置辅助函数
0x00466130  当前 Frame 选择函数
```

`0x00466130` 先检查资源对象 `this+0x04` 的类型字节，再分派到不同的 Frame 选择实现；因此它不是一个全局“按数字取图”函数，调用者传入的资源对象决定实际 WIL 库。

在 `0x004660E0` 的调用点可以看到 PE `.rdata` 中的路径字符串：`0x0047AAA0` 对应 `Data/Interface1c.wil`，`0x0047AAB8` 对应 `Data/gameinter.wil`。这解释了为什么同一个 Frame 编号在不同 WIL 库中会有不同图像。新增提取器：

```text
Tools/extract_mir3_wil_load_calls.py
docs/research/ei-ui-layout/wil_load_calls.json
```

当前只把路径字符串和资源对象 `LEA` 作为一级候选记录，尚未把某个窗口强行绑定到某个库；下一步需要追踪 `0x00427600` 初始化时的 `arg+0x5898` 对象和各窗口包装函数接收的资源指针。

### 反汇编标签纠错记录

后续复核 `.rdata` 原始字节后发现，之前版本的 `wil_load_calls.json` 曾把两个相邻字符串地址的标签写反。正确关系是：

```text
0x0047AAA0 -> .\Data\Interface1c.wil
0x0047AAB8 -> .\Data\gameinter.wil
```

现已修正 `Tools/extract_mir3_wil_load_calls.py` 并重新生成 JSON。机器码中的地址、调用 VA 和原始反汇编没有改变；这是路径语义标签纠正，不是原版客户端文件修改。

### 发现十四：按钮已经进入原版绘制链，但不能把所有 SetRect 当成最终坐标

在原版 `Mir3.exe` 的 `0x004179B0` 找到按钮/控件渲染函数候选。它的执行顺序提供了比“构造控件”更接近真实画面的证据：

1. 从 `this+0x04` 取资源对象，从 `this+0x08` 取当前 Frame 编号；
2. 调用 `0x00466130` 选择实际资源帧；
3. 从资源对象 `+0x38` 读取当前帧的有符号 WORD 宽高；
4. 根据 `this+0x0c`、`this+0x10` 计算缩放/动画相关尺寸；
5. 通过 `SetRect` IAT `0x004762B0` 构造多个经过模式变换的矩形；
6. 从资源对象 `+0x3c` 取得像素/解码缓冲区，最后在 `0x00417C17` 或 `0x00417C65` 调用 `0x0045F2D0`。

`0x0045F2D0` 的函数体会读取传入结构的 `+0/+4/+8/+0xc` 字段，计算裁剪后的宽高，并通过上下文对象的虚表偏移 `+0x64` 继续处理像素缓冲区。这使它成为“图像合成/绘制后端”候选，证据等级暂定 `primary-static-draw-candidate`；它的精确调用约定和最终屏幕 API 仍需运行时或全调用者交叉验证。

交叉搜索发现 `0x0045F2D0` 不只由按钮调用，还出现在 `0x0040B83B`、`0x0040CA06`、`0x00416BBA`、`0x00419A25`、`0x00429A42`、`0x00429C4E`、`0x00429CC6`、`0x0042A03D`、`0x0042A248`、`0x0042F960` 等调用点。多个调用者在 `ECX=0x008AB7A8` 时传入像素缓冲区、矩形指针、X/Y 和 `0xffff` 裁剪边界，说明该函数很可能是共享的屏幕图像合成例程，而不是按钮专属函数。各调用点的原始前置压栈已收录在 `button-draw-calls.json` 的 `all_composition_call_sites` 中；具体参数顺序仍标为待验证。

重要边界：`0x00417AA7`、`0x00417B06`、`0x00417BBA` 的 `SetRect` 调用发生在缩放、翻转/模式分支中，不能直接当成最终屏幕 UI Rect。当前只能确认它们是绘制前的矩形计算；最终坐标要继续追踪 `0x0045F2D0` 的参数和后端。

新增机器可读证据：

```text
Tools/extract_mir3_button_draw_calls.py
docs/research/ei-ui-layout/button-draw-calls.json
```

该提取器保留 `0x004179B0` 全部原始指令、两个 `0x0045F2D0` 调用点及其前置证据，也保留被分析的 `0x0045F2D0` 函数体片段。它不把推测性的绘制后端名称或坐标写成事实。

### 发现十五：窗口构造函数与窗口背景绘制函数是两个阶段

对前面列出的 13 个窗口包装函数重新检查后发现，它们主要调用通用初始化器 `0x00423B30` 和控件构造器 `0x00417550`，并没有直接调用 `0x0045F2D0`。因此 `window-draw-calls.json` 中这些函数的 `0` 个共享合成调用是一个重要的否定结果：不能把窗口构造阶段误写成绘制顺序。

在 `Mir3.exe` 的 `0x00423D00` 找到共享窗口背景绘制候选：

- `this+0x30` 先作为有效状态检查；资源对象来自 `this+0x2c`；
- 通过 `0x00466130` 选择当前 Frame，并从资源对象 `+0x38/+0x3c` 读取尺寸和像素缓冲；
- 非零渲染上下文分支在 `0x00423D62` 调用 `0x00460240`，前置压栈中直接出现目标宽 `0x320`（800）和目标高 `0x258`（600），并出现两个 `0xffff` 裁剪边界；
- 另一分支在 `0x00423DFA` 调用 `0x004542A0`，随后在 `0x00423E66` 调用 `0x004542F0`，使用位置/尺寸浮点计算以及 `this+0x50/0x51` 的颜色/透明度字节。

证据等级为 `primary-static-window-paint-candidate`。800×600 常量是原版二进制直接证据，但三个被调用函数的精确图形 API 语义、派生窗口的子控件绘制顺序仍需继续追踪。

继续反汇编 `0x00460240` 后，发现它不是简单的 `SetRect` 辅助：函数会做源/目标边界裁剪，读取上下文 `+0x1c` 的对象，通过虚表 `+0x64` 调用，并在内层循环中处理像素缓冲；同时识别 `0xc0/0xc1/0xc2/0xc3` 等压缩/透明编码标记。当前最稳妥的命名是“透明/编码图像 blit 或解码到目标缓冲区候选”。这增强了 `0x00423D62` 为实际窗口背景图像处理调用的证据，但还不足以证明它就是最终 GPU/窗口 API。

### 发现十六：窗口 vtable 把派生窗口连接到共享背景绘制

从 `Mir3.exe` `.rdata` 读取 vtable，并搜索构造函数中的 `mov [esi], <vtable>`：

- 发现 61 个窗口/相关对象 vtable 表；
- 发现 119 个直接 vtable 赋值点；
- 其中 59 个赋值点的 vtable `+0x0c` 项指向 `0x00423D00`；
- 基础窗口 vtable `0x00476624` 的前几项包含 `0x00423CF0`、`0x00423D00`，而 `0x00423D00` 也被多个 `call [object-vtable+0xc]` 形式的间接调用路径使用。

这说明派生窗口通常采用“派生构造函数建立自己的控件 + 共享基类绘制方法绘制窗口背景”的结构。vtable 表本身是一级二进制证据，但 `+0x0c` 的业务名称仍使用“绘制槽候选”，不擅自命名为源码类的 `Paint`。

新增机器可读结果：

```text
Tools/extract_mir3_window_vtables.py
docs/research/ei-ui-layout/window-vtable-evidence.json
```

该结果保留每个表的原始函数地址、每个构造赋值点附近的反汇编，以及 `call [vtable+0xc]` 的间接调用邻域，后续可按窗口对象继续绑定。

### 发现十七：13 个主窗口已经可以建立 vtable 绑定候选，NPC 窗口存在专用绘制方法

将主 UI 初始化中的 13 个窗口包装调用与前方同一对象类代码簇中的派生 vtable 写入进行关联，得到 13/13 个静态绑定候选；其中 12 个候选的 vtable `+0x0c` 指向共享背景绘制 `0x00423D00`。当前结果必须仍标记为候选，因为绑定采用“包装函数前 500 条反汇编指令内最近的非基类 vtable 写入”启发式，并保留了距离与原始邻域。

NPC 候选窗口是例外：其候选 vtable 为 `0x00476938`，`+0x0c` 指向 `0x0043F040`，不是共享 `0x00423D00`。对 `0x0043F040` 的静态分析确认：

- 选择连续 Frame `1100/1101/1102`；
- 在多个分支使用固定目标 `800×600` 和 `0xffff/0xffff` 裁剪边界；
- 通过 `0x00460240` 合成对话背景/编码图像；
- 使用 `this+0x520/0x524`、`+0x530/0x534`、`+0x540/0x544` 等字段作为对话内容或条目坐标/数据来源；
- 读取 `this+0x51c` 循环计数，并按 `0x12` 步长处理重复内容；
- fallback 路径调用 `0x004542A0/0x004542F0`，并读取 `this+0x580/0x581` 的透明度/颜色字节。

这已经是“NPC 对话窗口不只是一个背景 Frame，而是由多个连续 Frame 和动态条目组成”的一级静态证据。新增：

```text
Tools/bind_mir3_windows_to_vtables.py
docs/research/ei-ui-layout/window-vtable-bindings.json
Tools/extract_mir3_npc_paint.py
docs/research/ei-ui-layout/npc-paint-evidence.json
```

### 发现十八：vtable/特殊绘制证据已并入统一 layout

重新运行 `Tools/enrich_mir3_layout_evidence.py` 后，统一 `layout.json` 已升级为 `0.3-primary-evidence-vtable-enriched`：

- 13 个窗口记录各自包含 `vtable.derived_vtable`、vtable 赋值地址、`paint_slot_plus_0xc` 和候选证据等级；
- `window.npc-candidate` 额外包含 `special_paint`，保存 `0x0043F040`、连续 Frame 和调用邻域；
- 顶层 `draw_evidence` 保存背景、按钮、vtable、绑定和 NPC 专用绘制证据文件的路径；
- 15 个 HUD 按钮、13 个窗口和 72 个窗口控件构造记录保持不变。

这使预览器和后续导出器可以只依赖一个统一布局文件，同时仍能回溯到原始反汇编 JSON；候选绑定仍不会被提升为 `verified`。

新增：

```text
Tools/extract_mir3_window_base_draw.py
docs/research/ei-ui-layout/window-base-draw-evidence.json
```

### 发现十九：主 UI 的资源句柄已绑定到 GameInter.wil

本次完成了此前日志中留下的资源对象追踪，原始证据来自同一个 `Mir3.exe`：

1. `0x00427600` 读取调用者传入的资源管理对象指针，`0x00427609` 加上
   `0x5898`，并在 `0x00427611` 写入 `main_ui_this+0x1c`。
2. `0x00427750` 至 `0x0042792A` 的 13 个主窗口创建调用，都在调用包装器之前从
   `main_ui_this+0x1c` 取出同一个资源句柄，并作为窗口资源参数传入。
3. 这些窗口包装函数内的 72 个 `0x00417550` 控件构造调用，均在当前静态提取结果中
   以 `edi` 作为 `resource_arg1`；`edi` 是包装器接收的资源参数。这个寄存器传播仍
   保留“静态流候选”警告，不能替代运行时对象检查。
4. 资源路径初始化函数 `0x0045361D` 从绝对地址 `0x0047CE0C` 复制字符串
   `./Data/GameInter.wil` 到资源所有者的 `+0xF848` 字段；`0x00452AA0` 的第二组
   资源加载循环从所有者 `+0x5898` 开始，使用 `+0xF848` 起始的路径表，循环次数
   为 `0x46`（70）。因此主 UI 句柄的 WIL 文件绑定现在有一级静态路径证据。

新增机器可读结果：

```text
Tools/resolve_mir3_window_resource_handles.py
docs/research/ei-ui-layout/window-resource-handle-bindings.json
```

`layout.json` 已把窗口和 72 个子控件的 `resource_handle` 写入统一目录，版本仍为
`0.3-primary-evidence-vtable-enriched`。这次没有把 `Interface1c.wil` 的同编号 Frame
混入主 UI；后续分析其它资源族时，必须先建立同样的“对象句柄 → 路径表 → WIL 文件”证据。

替代解释与边界：`+0x5898` 是资源句柄数组/对象起始地址的静态表达式，尚未通过调试器
读取运行时指针验证；`0x00466130` 的资源对象类型分派也尚未完全命名。因此文档使用
“primary-static-handle-flow”，而不是 `runtime-verified`。

### 发现二十：控件坐标必须在压栈瞬间取值

复核 `Tools/resolve_mir3_control_positions.py` 时发现一个反汇编数据流陷阱：窗口
控件构造调用使用 x86 反向压栈，`push x` / `push y` 之后，调用前还会把同一个寄存器
改写为控件对象的 `this` 指针。如果在 `call 0x00417550` 时读取寄存器最终状态，
会把对象地址表达式误认为坐标。

以背包包装函数 `0x0042EA80` 的第二个控件为例：

```text
0x0042EAF4  push ecx       ; y 参数，当前为 window.y + 0x106
0x0042EAF5  push ebp       ; x 参数，当前为 window.x + 0xB0
0x0042EB00  push edi       ; resource
0x0042EB01  lea  ecx,[esi+0x110] ; 随后改写 ecx 为控件 this
0x0042EB07  call 0x00417550
```

提取器现在保存每个寄存器在对应 `push` 指令时的表达式，而不是使用 call 点的最终
寄存器状态。重新计算结果为：72 个控件中 65 个同时得到 x/y 绝对候选，51 个位于
对应窗口矩形内。对于拥有 GameInter Frame 尺寸的 60 个控件，已根据
`0x00417550 SetRect` 生成命中矩形。

新增/更新：

```text
Tools/resolve_mir3_control_positions.py
docs/research/ei-ui-layout/window-control-position-analysis.json
docs/research/ei-ui-layout/layout.json
Tools/wilviewer.py
```

预览器现在显示所有已解析但窗口外的控件为红色调试框；坐标未闭合或 Frame 尺寸缺失
的控件显示虚线占位框和明确的 `size unresolved` 标签，不再静默隐藏。Frame 在
GameInter 中为空而在 Interface1c 中存在的情况继续保留两库交叉记录，不能仅凭编号
替换资源文件。

### 发现二十一：全局控件构造调用不能只按主窗口筛选

对 `Mir3.exe` 全部直接调用 `0x00417550` 的记录进行分类后，共有 109 条：

| 分类 | 数量 | 当前状态 |
|---|---:|---|
| 主窗口内部控件 | 72 | 已绑定 13 个主窗口并进入 `layout.json` |
| 主 HUD 控件 | 15 | 已绑定底部 HUD 相对坐标 |
| 未归属控件候选 | 22 | 保留原始 Frame/对象/反汇编，等待函数归属和资源句柄追踪 |

未归属记录分布在 `0x004027DF`、`0x00418176`、`0x00418968`、`0x0043E2BB`、
`0x00455AF5`、`0x00456DC1` 等代码簇中；其中一部分出现 Frame `151/152`、
`154/155`、`606/607`、`86/87`、`89/90` 等连续状态帧。这说明原版还有未纳入当前
13 个主窗口表的 UI/对象控件，不能因为没有立即识别出窗口名称就丢弃。

新增目录：

```text
Tools/build_mir3_global_control_catalog.py
docs/research/ei-ui-layout/global-control-constructor-catalog.json
```

该目录只做证据保全，不把未归属控件伪装成完整坐标；下一阶段将以这些代码簇为入口，
追踪各自的构造函数、资源句柄和窗口开关状态。

进一步按地址间距整理后，22 条未归属调用形成 7 个复核簇：

```text
Tools/cluster_mir3_unassigned_controls.py
docs/research/ei-ui-layout/unassigned-control-clusters.json
```

其中 `0x00418176–0x004181E0`、`0x00418968–0x0041898E`、
`0x0043E2BB–0x0043E2E4` 和 `0x00456DC1–0x00456EC8` 含有连续 Frame 对，优先级
高于只有逻辑参数、没有连续 Frame 的簇。

其中第一个簇 `0x004027DF–0x00402845` 已进一步闭合资源来源：`0x00402735` 附近
将 `Interface1c.wil`（路径字面量 `0x0047AAA0`）加载到 `owner+0x5B0`，四个控件
构造调用均以 `esi=owner+0x5B0` 作为资源参数。它们的 Frame 参数是 11、13、15、17
等小编号，当前可以确认属于 Interface1c 资源对象，但业务窗口名称和最终绘制层级仍
未确认。

### 发现二十二：Interface1c 代码簇已形成第一个完整次级控件样本

对 `0x004027DF`、`0x00402801`、`0x00402823`、`0x00402845` 四个调用，按
`0x00417550` 的参数顺序恢复出以下矩形：

| 调用 | Frame | x | y | w | h |
|---|---:|---:|---:|---:|---:|
| `0x004027DF` | 11 | 459 | 436 | 96 | 24 |
| `0x00402801` | 13 | 139 | 379 | 96 | 26 |
| `0x00402823` | 15 | 279 | 379 | 96 | 26 |
| `0x00402845` | 17 | 439 | 379 | 48 | 26 |

坐标来自 `Mir3.exe` 的 `push` 常量，尺寸来自原版 `Data/Interface1c.wil` 的 17 字节
Frame 头部；四条记录均生成 `0x00417550 SetRect` 命中框。业务名称、父窗口名称和
绘制层级仍标为待验证。

新增：

```text
Tools/build_interface1c_cluster_catalog.py
docs/research/ei-ui-layout/interface1c-cluster-4027.json
```

这四条记录已写入 `layout.json.secondary_control_constructors`，并在 800×600 预览器
中以紫色证据框显示。

### 发现二十三：第二个 Interface1c 控件簇已闭合

函数 `0x00456CB0` 中：

- `0x00456CC1` 将 `Interface1c.wil` 加载到 `owner+0x14C`；
- `0x00456DC1–0x00456EC8` 的 9 个控件均把 `ebx=owner+0x14C` 作为资源参数；
- Frame、普通/状态帧、x/y 常量和对象偏移均可从连续反汇编直接恢复。

该簇已生成 9 个 `SetRect` 命中框，Frame/尺寸来自 Interface1c 原始 WIL：

```text
Tools/build_interface1c_cluster_456d_catalog.py
docs/research/ei-ui-layout/interface1c-cluster-456d.json
```

当前两个 Interface1c 簇共 13 个次级控件已经进入 `layout.json` 和 800×600 预览器。
它们的具体业务名称、父窗口和绘制层级仍保持待验证状态。

### 发现二十四：主初始化中存在额外的 GameInter 窗口候选

在主 UI 初始化 `0x0042797E` 发现对 `0x0043E260` 的额外窗口构造调用。调用参数中
直接出现 Frame `602`，并传入 `main_ui_this+0x1c`。该包装函数内部继续构造：

| 调用 | Frame 对 | 静态 x | 静态 y | GameInter 尺寸 |
|---|---:|---:|---:|---:|
| `0x0043E2BB` | 161/162 | 655 | 16 | 28×26 |
| `0x0043E2E4` | 606/607 | 603 | 27 | 40×20 |

坐标表达式分别来自 `window_arg4+0x224` / `window_arg8+0x10` 及其后续增量；主调用
的原始参数为 `15, resource, 602, 107, 110, 584, 252, 0, 3`。由于该包装器的窗口
参数槽位语义尚未完全确认，Frame 602 的窗口容器矩形暂不强行写入绝对布局；两个
控件作为 `primary-static-candidate` 记录。

新增：

```text
Tools/build_gameinter_cluster_43e260_catalog.py
docs/research/ei-ui-layout/gameinter-cluster-43e260.json
```

当前统一布局已包含 15 个次级控件和 1 个额外窗口候选。

### 发现二十五：原版初始化器包含完整的 WIL 路径表

针对 `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe`，扫描所有形如
`mov edi, <绝对地址>` 的路径字面量、独立加载器的 `push <路径地址>` 参数，并沿资源初始化复制序列追踪
`lea edx,[ebx+偏移]` 目标，恢复出 157 条静态路径字段。结果覆盖四组地形资源（普通、Wood、Sand、Forest、Snow）
以及主界面、角色、武器、技能、背包、装备、地面物品、图标、坐骑、怪物、NPC、魔法特效和商店物品等资源族。

典型记录：

```text
GameInter.wil  -> owner+0xF848
Magic.wil      -> owner+0x10478
Inventory.wil  -> owner+0x1057C
Equip.wil      -> owner+0x10680
MIcon.wil      -> owner+0x10888
NPC.wil        -> owner+0x13434
StoreItem.wil  -> owner+0x13F9C
```

证据等级为 `primary-static-path-table`：它证明原版程序把这些路径复制到对象字段，
但不能单独证明每个字段对应哪个运行时窗口或绘制类。该表首先用于资源族与后续控件/窗口簇的交叉匹配，
不能把同编号 Frame 直接等同为同一素材。

新增：

```text
Tools/extract_mir3_resource_path_table.py
docs/research/ei-ui-layout/resource-path-table.json
```

预览器的 `/api/ui-layout` 数据现在同时携带该表，供后续制作资源族导航和证据检查使用。

### 发现二十六：路径表与原始 WIL/WIX 库已完成资源族索引

将发现二十五的路径记录和原始客户端目录重新合并，得到 157 条资源路径记录及 89 组实际存在的 WIL/WIX
库。重要资源库的头部统计如下：

| 资源库 | 总 Frame 槽位 | 非空 Frame | 资源族 |
|---|---:|---:|---|
| `GameInter.wil` | 1103 | 253 | UI/HUD |
| `Magic.wil` | 3550 | 1948 | 技能/魔法 |
| `Inventory.wil` | 1440 | 499 | 背包物品 |
| `Equip.wil` | 1320 | 125 | 装备 |
| `MIcon.wil` | 1106 | 138 | 图标 |
| `NPC.wil` | 6400 | 1994 | NPC |
| `StoreItem.wil` | 1440 | 490 | 商店物品 |

这里的“非空 Frame”来自原始 `.wix` 偏移和 WIL 17 字节头解析，不代表每个 Frame 都在
当前游戏状态中被绘制。资源族分类也只是导航和交叉匹配标签，不能直接推出窗口业务名称。

新增：

```text
Tools/build_mir3_resource_family_catalog.py
docs/research/ei-ui-layout/resource-family-catalog.json
```

预览器 `/api/ui-layout` 已携带路径表和资源族索引，后续可以在同一个 800×600 证据界面
中跳转查看 UI、技能、装备、NPC 和地图素材来源。

### 发现二十七：`mir3.dat` 与 `Mir3.exe` 的资源路径表一致

对原版 `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/mir3.dat` 单独进行 PE 头和反汇编检查：
它是一个 PE32 GUI 可执行文件，时间戳为 2002-10-25，入口点为 `0x0046A882`，并且包含
同一批 `Data/*.wil` 路径字面量。使用同一提取器恢复出 157 条路径记录，与 `Mir3.exe`
逐条比较后，路径名和对象字段偏移全部一致。

这不是把两个文件混为一个程序，而是一个很有价值的交叉验证：`Mir3.exe` 中得到的资源
族表不是偶然扫描结果，`mir3.dat` 也保留了同样的初始化表结构。当前还没有宣称两者所有
窗口函数和绘制实现完全相同；坐标/窗口结论仍以实际调用点分别确认。

新增：

```text
docs/research/ei-ui-layout/mir3-dat-resource-path-table.json
```

### 发现二十八：资源族索引已进入统一 layout

`Tools/enrich_mir3_layout_evidence.py` 现在把路径表和 WIL/WIX 资源族索引写入
`layout.json.resource_evidence`，记录两个机器可读 artifact、157 条路径记录及当前库统计。
这让 800×600 预览器、交接文档和后续窗口匹配共享同一份资源证据入口，同时继续保留“资源存在
不等于窗口绘制”的警告。

新增字段并更新：

```text
docs/research/ei-ui-layout/layout.json.resource_evidence
docs/research/ei-ui-layout/layout.schema.json
```

### 发现二十九：两个 Interface1c 控件簇已与独立加载字段精确交叉匹配

将 `resource-path-table.json` 的 `owner+偏移` 与已闭合控件簇中的资源对象表达式进行精确连接：

| 控件簇 | 对象字段 | 匹配资源 | 状态 |
|---|---|---|---|
| `0x004027DF–0x00402845` | `owner+0x5B0` | `Interface1c.wil` | matched |
| `0x00456DC1–0x00456EC8` | `owner+0x14C` | `Interface1c.wil` | matched |
| `0x0043E260` | `main_ui_this+0x1C` | GameInter 句柄字段表达式不同 | unresolved |

前两个簇因此拥有“路径字面量 → 资源对象字段 → Frame → 尺寸 → 坐标/命中框”的完整
静态链；但它们的业务名称和父窗口仍未强行猜测。第三个簇保留为未匹配候选，因为主 UI
句柄是另一层对象传播，不应把 `+0x1C` 直接等同于路径表字段。

新增：

```text
Tools/crossmatch_mir3_resource_clusters.py
docs/research/ei-ui-layout/resource-cluster-crossmatch.json
```

### 发现三十：Interface1c 簇属于一个 640×480 的前置角色界面候选

对 `0x00456A90` 对象初始化器和 `0x00456CB0` 构造函数继续向上追踪，发现它们都操作同一个
全局对象 `ECX=0x008A7140`，并在两个状态转换点 `0x00402989`、`0x00419C0A` 被调用。
构造函数同时加载 `GameInter.wil`（`owner+0x8`）和 `Interface1c.wil`（`owner+0x14C`），
然后建立 9 组 Interface1c 普通/状态按钮：

```text
Frame 51/52  -> (440, 93), 96×26
Frame 53/54  -> (79, 243), 96×26
Frame 55/56  -> (259, 49), 96×24
Frame 57/58  -> (28, 438), 48×26
Frame 92/93  -> (266,419), 40×38
Frame 95/96  -> (308,419), 40×38
Frame 98/99  -> (352,419), 40×38
Frame 86/87  -> (450,444), 28×28
Frame 89/90  -> (491,444), 28×28
```

Interface1c Frame 50 的头部尺寸为 640×480，Frame 51–58 是中文文字按钮素材。结合完整
屏幕尺寸、固定按钮布局和状态转换位置，目前将其标为“角色选择/创建角色界面候选”，不是
最终业务命名。原始素材的视觉检查显示它与游戏内 800×600 HUD 是不同状态，不能把这 9 个
按钮误并入底部操作栏。

新增：

```text
Tools/analyze_mir3_interface1c_parent.py
docs/research/ei-ui-layout/interface1c-parent-context.json
```

该候选已进入 `layout.json.secondary_screen_candidates`，证据等级为
`candidate-not-runtime-confirmed`。

### 发现三十一：启动阶段存在独立的角色选择界面

`0x004026E0` 由启动流程 `0x004020A8` 调用。它独立加载 `GameInter.wil` 到 `owner+0x46C`
和 `Interface1c.wil` 到 `owner+0x5B0`，并构造 4 个 Interface1c 按钮：

```text
Frame 11/11 -> (459,436), 96×24
Frame 13/13 -> (139,379), 96×26
Frame 15/15 -> (279,379), 96×26
Frame 17/17 -> (439,379), 48×26
```

对这些帧进行原始素材视觉检查后，按钮文字属于角色选择操作族；结合它们在启动阶段的
调用位置，将该界面标为 `character-selection-screen` 候选。它与发现三十的 9 按钮界面
是两个不同初始化器，不能合并成一个窗口；两者最终状态仍需运行时确认。

新增：

```text
Tools/analyze_mir3_interface1c_select_screen.py
docs/research/ei-ui-layout/interface1c-select-screen-context.json
```

两个候选屏幕现在都进入 `layout.json.secondary_screen_candidates`。

### 发现三十二：主窗口 Frame 的视觉语义已与静态窗口表分层记录

对原始 `GameInter.wil` 的 13 个窗口底图做了逐帧解码和视觉复核。得到的高置信资源形态包括：

```text
Frame 250  背包网格
Frame 400  技能书/技能界面候选
Frame 700  任务卷轴
Frame 750  系统选项
Frame 850  坐骑界面候选
Frame 900  组队界面
Frame 1000 商店物品列表候选
Frame 1050 交易界面
Frame 1100 NPC 对话底图（另有 1101/1102 状态帧）
```

Frame 200 的装备/人物状态角色仍保持二义性；Frame 600 的行会/社交管理语义也保持候选。
这些视觉判断没有覆盖或改写 `window_layout.json` 的一级静态证据，而是以
`visual_semantics` 字段进入统一 `layout.json`，证据等级为
`secondary-resource-visual-review`。

新增：

```text
Tools/annotate_mir3_window_visual_semantics.py
docs/research/ei-ui-layout/window-frame-visual-semantics.json
```

### 发现三十三：58 个窗口控件形成可识别的功能组候选

把 72 个主窗口控件按窗口、普通/状态 Frame 对和原始窗口底图形态交叉检查后，得到 58 条
辅助语义记录：

```text
技能书分类/技能槽候选       11
商店导航/物品操作候选        8
行会/社交页签操作候选        8
系统选项开关/数值候选        8
聊天频道/操作候选            6
组队操作候选                 4
坐骑操作候选                 4
窗口关闭按钮                 3
交易操作候选                 2
任务翻页候选                 2
NPC 对话选项/标记候选        2
```

这些分组来自 Frame 对重复规律、控件尺寸和原始窗口图形，不是最终业务名称；例如技能
槽位的实际页签文字、商店按钮行为和 NPC 对话选项仍需从字符串/输入处理路径确认。记录已
挂到 `layout.json.control_constructors[*].semantic_candidate`。

新增：

```text
Tools/annotate_mir3_control_semantics.py
docs/research/ei-ui-layout/control-semantic-catalog.json
```

### 发现三十四：技能书窗口出现原版中文元素分类字符串

在主 UI 初始化 `0x00427904` 调用的窗口包装器 `0x00439250` 中，窗口 Frame 400 的控件
构造附近直接引用了 `Mir3.exe` 的 GB18030 字符串，并与控件调用点一一对应：

| 字面量 VA | 原文 | 分类键 | 控件调用 |
|---|---|---|---|
| `0x0047C330` | 火 | fire | `0x00439334` |
| `0x0047C32C` | 冰 | ice | `0x0043935D` |
| `0x0047C328` | 电 | lightning | `0x00439386` |
| `0x0047C324` | 风 | wind | `0x004393B3` |
| `0x0047C31C` | 神圣 | holy | `0x004393E0` |
| `0x0047C314` | 黑暗 | dark | `0x0043940D` |
| `0x0047C30C` | 幻影 | illusion | `0x00439437` |
| `0x0047C308` | 剑 | sword | `0x00439464` |

同一构造器还引用 `Magic.exp`，并创建 Frame 410–459 范围内的技能页签/技能槽候选。

### Finding 35：`Magic.exp` 是客户端根目录文件，不能与 Mud3 `magic.dat` 混用（2026-08-09）

复核 `Mir3.exe` 的字面量与实际客户端目录后确认：程序引用的是裸文件名
`Magic.exp`，实际供应文件为 `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Magic.exp`，
不是 `Data/Magic.exp`。该文件为编码/加密二进制。`/home/tetsuya/NAS/TMP/Mud3/Envir/magic.dat`
是独立的服务端技能表，虽然已经可以解出 105 条老版记录，但不能替代客户端技能窗口
的读取证据。详细的参数映射、坐标边界和后续路线见：

`docs/research/ei-ui-layout/skill-window-static-evidence.md`
这是比单纯观察 Frame 400 更强的一级静态文本证据：可以确认该窗口存在元素/流派分类，
但还不能据此推出每个分类下的完整技能名称或技能等级，需要继续追踪技能数据加载和输入分支。

### Finding 36：技能窗口存在独立的 `Magic.exp` 加载链（2026-08-09）

`0x00439150` 在窗口创建后准备 16 字节栈上初始化数据，调用 `0x00452580`，随后
把 `Magic.exp` 和 `/%s` 传给 `0x0046926D`。返回对象经 `0x00469382`、`0x00468B1A`
处理后保存到窗口对象 `this+0x968`；析构函数 `0x00439220` 会释放并清零该字段。

这已把证据从“出现文件名字面量”提升到 `primary-static-loader-chain`：客户端确实
为技能窗口加载独立扩展数据。当前仍未知初始化数据的算法、记录大小和字段布局，不能
把它直接解释成技能名称或等级表。完整调用链和待验证项见
`docs/research/ei-ui-layout/skill-window-static-evidence.md`。

### Finding 37：`Magic.exp` 已恢复为 50 条客户端技能记录（2026-08-09）

使用已复现的 `0x004525F0` 解码器后，`Magic.exp` 可按 GB18030 文本解析出 50 条
记录：文件顺序形成战士候选 8 条、法师候选 23 条、道士候选 19 条。三段起点技能
ID 分别为 3、1、2；这些数字是技能 ID，不是独立的区段头。每条记录含
原版 ID、中文名、属性、元素、1–4 级门槛、修炼值和说明；第 4 级的“未知”保持原样。

逐条 JSON 和转 UTF-8 的原文已落在 `magic-exp-records.json`、`Magic.exp.decoded.txt`，
内容目录见 `exp-content-catalog.md`。这使技能百科首次有客户端文件一级证据，
但仍不能把文件区段直接当成八个 UI 页签，页签映射要继续追踪控件回调。

新增：

```text
Tools/analyze_mir3_skill_window.py
docs/research/ei-ui-layout/skill-window-context.json
```

该记录已进入 `layout.json.specialized_window_evidence`。

### Finding 38：技能分类控件的八组相对坐标由窗口重绘函数完整恢复（2026-08-09）

继续反汇编 `Mir3.exe` 的技能窗口刷新路径后，确认 `0x00439500` 会在每次重绘时
调用通用定位逻辑，把八个分类控件按窗口对象 `this+0x18/this+0x1c` 的原点重新放置。
这条路径直接给出了最终的窗口相对坐标，修正了早先只依据构造器寄存器表达式时对后四项
坐标的“未解析”标记：

| 分类 | 控件对象偏移 | X | Y |
|---|---:|---:|---:|
| 火 | `this+0x2f4` | 5 | 21 |
| 冰 | `this+0x3a8` | 3 | 56 |
| 电 | `this+0x45c` | 4 | 91 |
| 风 | `this+0x510` | 2 | 126 |
| 神圣 | `this+0x5c4` | 2 | 161 |
| 黑暗 | `this+0x678` | 2 | 196 |
| 幻影 | `this+0x72c` | 1 | 231 |
| 剑 | `this+0x7e0` | 2 | 266 |

证据等级为 `primary-static-redraw-position`。这些值是窗口内部坐标；仍需继续恢复
窗口基类的屏幕原点、移动状态和控件最终 RECT，才能得到屏幕绝对坐标。机器可读结果同步
写入 `skill-window-context.json` 与 `layout.json` 的 `control_constructors`。

本次方法：以 `llvm-objdump` 反汇编 `0x00439500`，核对八个 `0x00417830` 定位调用
使用的 X/Y 常量与 `this` 内对象偏移；再由 `analyze_mir3_skill_window.py` 和
`enrich_mir3_layout_evidence.py` 写入结构化证据。分类控件也可能是带图标的复合控件，
但坐标本身不依赖业务命名，因此可直接用于 800×600 预览器。

### Finding 39：技能列表刷新循环暴露了原版列表起点和行间距（2026-08-09）

在 `0x0043A440` 继续恢复技能窗口刷新函数。原版从 `this+0x968+8` 的数据流逐行读取
记录：以 `0x00468BF0(0x0d, stream)` 取得行，`#` 行用于段/ID 筛选，`;` 行作为分隔或
注释。匹配后的数据经 `0x0045E200` 解析到局部记录区域，再由 `0x0045DBA0`、
`0x0045DD70` 交替绘制多个字段。

可直接用于布局的一级静态坐标是：首行原点为 `this+0x18+0x0f,
this+0x1c+0xeb`，之后每行 Y 增加 `0x0f`（15 像素）；记录缓冲区每行增加 `0x104`。
这解释了技能书内列表的固定行距，并为预览器提供了不依赖手动拖动的初始参数。字段含义、
列宽和窗口移动后的屏幕原点仍标记为待解析。详情写入
`skill-window-render-loop-evidence.json` 与技能窗口静态证据文档。

### Finding 40：背包窗口包含三组子控件并使用 36 像素物品网格步长（2026-08-09）

在 `Mir3.exe` 的背包构造函数 `0x0042EA80` 和绘制函数 `0x0042EB7F` 中确认：Frame 250
窗口之外，原版还在 `this+0x5c`、`this+0x110`、`this+0x1c4` 创建三个子控件，调用
分别为 `0x0042EADB`、`0x0042EB07`、`0x0042EB2D`，帧对为 `161/162`、`264/265`、
`267/268`。其中第三组静态资源交叉结果指向 `Interface1c.wil`，第二组同时存在
`GameInter.wil` 和 `Interface1c.wil` 候选，不能在没有运行时句柄前武断选择。

绘制路径在 `0x0042EC54`、`0x0042EC64` 明确出现 `index*36` 的横纵网格计算，起点候选
为窗口原点偏移 `(0x19,0x29)`；这足以作为预览器的一级静态网格参数，但列数、行数和
具体物品字段仍待继续追踪。完整机器记录见
`inventory-window-render-evidence.json`。

### Finding 41：人物状态窗口的装备槽矩形由原版 SetRect 调用直接恢复（2026-08-09）

在 `Mir3.exe` 的状态窗口构造/绘制路径（`0x0044B130`、`0x0044B2D0`）中，确认背景为
GameInter Frame 200（构造尺寸 244×328），并找到 11 个连续的 `SetRect` 初始化区域。
其中 7 个 38×38 区域位于 `(27,186)`、`(175,186)`、`(27,227)`、`(175,227)`、
`(27,264)`、`(64,264)`、`(103,264)`，另有顶部 `(177,70)` 的 38×38 区域；这些
是人物装备槽的一级位置候选。窗口还包含 `(86,114)-(146,204)` 的 60×90 人物图像区域、
`(38,70)-(91,154)` 的属性区域和 `(94,71)-(143,104)` 的头像/名称区域候选。

这些矩形不是从现代 Zircon 坐标反推，而是由原版 `0x004762B0` SetRect 的参数顺序
逐个还原。对象偏移、VA、尺寸、证据边界和未决装备语义已写入
`status-window-render-evidence.json`。

### Finding 42：任务窗口的操作按钮与文本列表坐标由刷新函数恢复（2026-08-09）

在 `Mir3.exe` 任务窗口构造函数 `0x00447400` 和刷新函数 `0x00447470` 中确认 Frame 700
背景尺寸 340×440。两个控件构造调用 `0x0044743B`、`0x0044745E` 使用 Frame 对
`723/724`、`721/722`，并在重绘时固定到窗口相对 `(290,59)` 与 `(290,89)`。

任务文本通过 `this+0x1E8` 数据链交给 `0x0045E0C0` 读取；字段绘制路径在
`0x0044760B`、`0x004477EF` 计算窗口相对的首列 `(65,90)`，每行增加 15 像素，
并保留最多 19 行的边界判断。字段分隔、换行宽度和滚动状态仍待解析。机器记录见
`quest-window-render-evidence.json`。

### Finding 43：商店路径确认动态商品链表、8 行区域和 38 像素商品格循环（2026-08-09）

在 `Mir3.exe` 的商店候选构造函数 `0x0044D310` 与绘制函数 `0x0044D590` 中，确认
资源帧对覆盖 `1010/1011`、`1012/1013`、`1014/1015`、`1016/1017`，并且对象包含
`this+0x64C` 的动态链表候选。`0x0044D4C4` 的 SetRect 循环产生 8 个列表行，
`0x0044D51E` 的循环产生横向步长 38、纵向步长 38 的商品格候选（5 列×4 行的静态
循环边界候选）。

这里不能直接把常量升级为最终屏幕坐标：原始格子 X 起点为 323，而当前 Frame 1000
可见宽度只有 300，说明商店窗口绑定、父坐标或 Frame 1000 的完整组合仍有替代解释。
因此机器记录保留 `arg4/arg5` 原始表达式和“parent-basis-pending”证据等级，见
`store-window-render-evidence.json`。

### Finding 44：原版地图对象明确装载 MMap/FMMap 并按地图编号选择 Frame（2026-08-09）

在 `Mir3.exe` 的地图资源对象路径 `0x0043D4D0` 中，`Data/MMap.wil` 被装载到
`owner+0x04`，`Data/FMMap.wil` 被装载到 `owner+0x148`。`owner+0x2D0` 是后续
初始化的运行时矩形/状态区域，不是 WIL 句柄。资源头交叉确认 MMap 为 255 槽/154 非空，
FMMap 为 31 槽/29 非空。

在 `0x0043D780` 看到 `map_id >= 1000` 的分支：选择 FMMap 资源时使用精确的
`frame = map_id - 1000` 表达式；低于 1000 时选择 MMap 并使用 `frame = map_id`，
然后把帧头源矩形送入 `owner+0x2E0` 目标矩形。地图
表面初始化函数 `0x0043D5F0` 还维护 `owner+0x2C0` 视口矩形和 `owner+0x2B8/0x2BC`
视图位置字段。当前这些是地图子系统一级证据，尚未把输出绑定到 GameInter 的小地图
控件或全地图窗口；机器记录见 `map-ui-resource-evidence.json`。

### 当前产物

```text
Tools/extract_mir3_ui_layout.py
Tools/find_mir3_ui_patterns.py
Tools/extract_mir3_button_calls.py
Tools/extract_mir3_button_draw_calls.py
Tools/build_mir3_ui_resource_metadata.py

docs/research/ei-ui-layout/static_rect_initializers.json
docs/research/ei-ui-layout/ui-pattern-candidates.json
docs/research/ei-ui-layout/button_constructor_calls.json
docs/research/ei-ui-layout/button-draw-calls.json
docs/research/ei-ui-layout/gameinter-frame-metadata.json
docs/research/ei-ui-layout/layout.schema.json
docs/research/ei-ui-layout/layout.json
docs/research/ei-ui-layout/primary-button-evidence.md
docs/research/ei-ui-layout/secondary-source-catalog.md
```

### 尚未完成、不能提前宣称的内容

1. `hud.left/top` 基准字段的真实来源和分辨率变化逻辑。
2. 各按钮的最终 `hit_rect`，因为必须读取对应 WIL Frame 尺寸并确认控件初始化函数的边界计算。
3. Frame 50、60/61、63/67 的真实绘制顺序和裁切参数。
4. 人物状态、背包、技能、任务、地图、聊天、NPC、商店、仓库、组队、行会、好友和系统弹窗的完整窗口构造函数。
5. 原版运行时截图、绘图调用参数和最终 800×600 差异叠加验证。
6. `mir3.dat` 对 UI 状态、窗口开关或坐标的影响。
7. 其它 WIL 资源族（Interface1c、Inventory、Magic、MIcon 等）各自的句柄起点、完整
   窗口绘制顺序，以及窗口打开/关闭状态机。

## 后续日志规则

每次得到新结论时必须记录：

1. 原始文件路径和文件版本；
2. 函数 VA、调用 VA 或 WIL Frame；
3. 使用的反汇编/资源解析方法；
4. 结论本身及其证据等级；
5. 与已有源码/文档是否一致；
6. 仍然存在的替代解释；
7. 对 `layout.json` 或预览工具产生的具体变更。

### Finding 45：聊天窗口的历史区、输入区与频道按钮坐标已恢复（2026-08-09）

在 `Mir3.exe` 聊天窗口构造路径 `0x00414080` 中确认 Frame 350（572×388）。原版
`SetRect` 直接给出历史区 `(40,29)-(531,308)`、输入区 `(25,311)-(524,326)`；频道
控件 Frame 对 `360/361`、`362/363`、`364/365`、`366/367`、`368/369`、`370/371`
按窗口相对 X=25、65、105、145、185、225 排列，Y 均为 332。右侧另有 Frame 对
`380/381`、`382/383`，坐标为 `(539,25)` 和 `(539,311)`。

刷新路径 `0x004142C0` 维护 `this+0x720` 的文本行缓存，步长 16，行数 19 为当前
静态候选；频道业务名称和共享文本渲染参数仍待确认。机器记录见
`chat-window-render-evidence.json`。

### Finding 46：NPC 对话窗口的三层资源与动态条目步长已恢复（2026-08-09）

在原版 `Mir3.exe` 的 NPC 窗口构造函数 `0x0043ED00` 和绘制函数 `0x0043F040` 中，确认
主底图为 `Data/GameInter.wil` Frame 1100，构造尺寸为 `552×176`；绘制路径随后选择
连续的 Frame 1101 与 Frame 1102 作为动态/状态层。三个控件构造调用分别位于
`0x0043ED65`、`0x0043ED8B`、`0x0043EDB1`，窗口相对坐标为 `(7,141)`、`(290,145)`、
`(306,136)`，帧对分别是 `161/162`、`52/53`、`54/55`。

绘制循环从 `this+0x51C` 读取条目数量，每个条目使源数据偏移增加 `0x12`（18 字节）；
动态合成坐标使用 `this+0x530/+0x534`，末层使用 `this+0x540/+0x544`，调用中还固定
传入 `800×600` 视口参数。上述内容属于 `primary-static`，但控件的业务名称、条目字段
语义及文字绘制顺序仍未提升到 runtime-confirmed。机器记录见
`npc-window-render-evidence.json`，并已接入 `layout.json` 的 `specialized_window_evidence`
和顶层 `npc_window_evidence`。

### Finding 47：组队与行会窗口的控件构造位置已整理（2026-08-09）

原版 `Mir3.exe` 的组队窗口构造路径 `0x004242AB`（主初始化 `0x00427811`）绑定
`GameInter.wil` Frame 900，尺寸 `256×244`，主屏候选原点为 `(272,123)`。五个控件
调用使用 Frame `161/162`、`910/911`、`912/913`、`914/915`、`920/921`，窗口相对
坐标分别为 `(226,214)`、`(17,197)`、`(80,197)`、`(159,197)`、`(9,52)`。

行会窗口构造路径 `0x00424EC0`（主初始化 `0x004277E8`）绑定 Frame 600，尺寸
`596×446`，原点候选 `(102,22)`。共发现 9 个控件构造调用，其中 5 个坐标可直接解析，
4 个因寄存器复用暂保留表达式歧义；控件帧对覆盖 `161/162`、`610/611` 至 `624/625`。
这批结果属于 `primary-static`，尚未把控件业务名和成员列表文字绘制顺序升级为最终结论。
机器记录见 `social-window-render-evidence.json`，并已接入 `layout.json`。

### Finding 48：800×600 证据预览增加次级 Interface1c 界面切换（2026-08-09）

`Tools/wilviewer.py` 的 `/ui` 页面新增预览模式选择器：主 HUD、角色选择/创建候选 A、
角色选择候选 B。次级模式读取 `layout.json.secondary_screen_candidates` 中的原版
`Interface1c.wil` Frame 50（640×480），按原始坐标居中到固定 800×600 视口，并叠加对应
`secondary_control_constructors` 的 Frame 和命中矩形。模式选择与调试/Frame 开关共同写入
`mir3_evidence_ui_state`，刷新后恢复。此功能只展示候选证据，不把候选屏幕名称升级成已
运行时确认的业务结论。

### Finding 50：证据预览支持本地原版截图差异叠加（2026-08-09）

`/ui` 新增本地图片导入、叠加开关和 0–100% 透明度控制。截图会以固定
`800×600` 视口尺寸覆盖在当前 HUD/次级界面证据层之上，用于直接检查资源边界、窗口
原点和层级偏差；开关与透明度写入 `mir3_evidence_ui_state`。浏览器安全限制下，原始
图片本身不写入仓库，也不伪装成静态反编译证据。

### Finding 49：建立统一 UI 绘制层级候选表（2026-08-09）

新增 `draw-order-evidence.json`，把分散在按钮绘制、窗口基类、NPC 专用绘制和
`Interface1c` 屏幕初始化记录中的顺序约束汇总为统一层：场景底层 → HUD Frame 50 →
HUD 控件/血球/经验条 → 普通窗口底图 → 窗口子控件与文字 → NPC 专用合成 → 次级全屏界面。
其中明确标记了已由机器码确认的约束，以及尚未有运行时重叠窗口截图支持的候选顺序；该表
已接入 `layout.json.draw_order_evidence`，不会把推测的 z-order 当成最终事实。

### Finding 55：任务详情 Frame 705 与正文绘制坐标已恢复（2026-08-09）

在任务窗口刷新路径 `0x00447D00` 附近确认：当详情文本存在时，`0x00447E07` 选择
`GameInter.wil` Frame 705，并在 `0x00447E43` 通过共享文本/合成调用绘制；窗口原点上
的固定偏移为 `(0x41,0x126)`，即窗口相对 `(65,294)`。这与任务窗口 Frame 700 和
列表起点 `(65,90)` 形成同一窗口内的列表/详情两段布局。证据已补入
`quest-window-render-evidence.json` 和 `layout.json`。

### Finding 52：恢复公告板与 YES/NO 提示资源簇（2026-08-09）

`GameInter.wil` Frame 602 的原始图像检查确认其为公告/公告板样式的大窗口资源，头部尺寸
为 `1024×256`，可见内容候选约 `584×252`。原版 `0x0043E260` 簇构造了 Frame
`161/162` 和 `606/607` 两个控件对；构造入口由 `0x0042797E` 调用。附近 Frame 603
视觉上是中文 YES/NO 确认提示，Frame 604 是带勾选状态的窄消息/输入面板，605–607 是
小型状态资源。由于这些帧是否由同一运行时状态机组合仍未确认，全部记录为
`primary-static-candidate`，原始参数表达式和候选坐标均保留在
`notice-prompt-window-evidence.json`。

### Finding 53：确认 YES/NO/勾选按钮的未归属构造簇（2026-08-09）

对全局未归属控件中的 `0x00418176–0x004181E0` 和 `0x00418968–0x0041898E` 两个代码簇
进行了原始 WIL 视觉交叉检查。Frame `151/152`、`154/155` 是 YES/NO 的普通/绿色状态
对，`157/158` 是勾选确认状态对，尺寸分别为 `44×20` 和 `64×20`。前一个簇构造三个
横向操作控件，后一个簇再构造两个同资源族控件；它们都直接调用 `0x00417550`。因此
这批资源可以确定属于确认/提示操作族，但父窗口、文字来源和最终屏幕坐标仍为待追踪。
机器记录见 `confirmation-prompt-evidence.json`，已接入 `layout.json`。

### Finding 54：确认提示父窗口 Frame 950 已由原版构造函数闭合（2026-08-09）

继续追踪 `0x00418176–0x004181E0` 所在函数，确认其父构造入口为 `0x00418030`；在
`0x0041804E` 通过 `0x00466130` 选择 `GameInter.wil` Frame 950。WIL 头部尺寸为
`360×190`，视觉上是带文字区域和底部操作条的宽提示框。随后该函数构造 Frame
`151/152`、`157/158`、`154/155` 三组控件。因此 YES/NO/勾选控件属于这个确认提示
父窗口的证据强度提升到 `primary-static-parent-and-resource`；文字内容和运行时坐标仍
保留待验证状态。

### Finding 51：预览器增加绘制层级可视图例（2026-08-09）

`/ui` 增加“显示绘制层级”开关，读取 `layout.json.draw_order_evidence.layers`，在固定
视口内显示层序号、层名称和证据等级。图例与差异截图叠加层分离，便于同时检查“资源边界
偏差”和“层级顺序候选”；开关状态写入同一份本地预览状态。

### Finding 56：商店/仓库共用状态机的多种原版面板尺寸已恢复（2026-08-09）

继续反汇编 `0x0044E9B0` 附近的物品业务状态机，确认 `this+0x5F8` 会取值 `0–4`，并多次调用通用窗口工厂 `0x00423E80`。调用点绑定了 Frame 1000 `(0,186,300,304)`、Frame 1003 `(1,186,498,304)`、Frame 1001 `(-4,182,205,205)`、Frame 1000 `(0,186,300,304)` 和 Frame 1002 `(0,184,540,307)` 五组状态面板几何参数。

这些是原版机器码直接传入窗口工厂的参数，不是手动拖拽校准结果。当前可以把 F1000–F1003 归入同一个商店/仓库/物品操作资源簇，并将仓库从“待追踪”提升为“候选”；但 state 数值与 NPC 商店、仓库、扩展购买、物品详情的业务名称仍必须通过打开窗口的协议入口或运行时调用继续确认，不能仅凭图片外观命名。完整参数和待办见 `store-window-render-evidence.json`。

### Finding 57：原版小地图固定目标矩形恢复为右上角 128×128（2026-08-09）

反汇编地图对象初始化 `0x0043D4D0–0x0043D5F0` 时，在 `0x0043D551` 发现对
`owner+0x2C0` 的直接 `SetRect` 调用。按 Win32 `SetRect(rect,left,top,right,bottom)` 的
压栈顺序，原版常量为 `left=0x2A0 (672)`、`top=0`、`right=0x320 (800)`、
`bottom=0x80 (128)`，因此 800×600 画面的固定小地图目标矩形是
`(672,0)-(800,128)`，尺寸 `128×128`。

同一初始化路径还建立了 800×800 的内部地图表面，并在 `0x0043D780` 根据
`map_id >= 1000` 从 `MMap.wil` 选择 `map_id-1000` 帧，再将视图/源矩形合成到上述目标区。
这给出了原版小地图的静态屏幕位置，不是手动拖拽校准；地图边框、玩家/队伍标记和完整地图
窗口仍需继续从对应绘制调用区分。

### Finding 59：证据预览器已改用原版小地图 Rect 并持久化调试开关（2026-08-09）

修正 `Tools/wilviewer.py` 中旧的占位框：原先错误的 `(650,10,140×140)` 已替换为原版
机器码确认的 `(672,0,128×128)`。证据布局 `/ui` 新增“显示原版小地图 Rect”开关，直接
读取 `layout.map_ui_evidence.viewport.fixed_minimap_widget.screen_rect`，并把开关与
调试框、Frame、差异截图、绘制层级一起写入 `mir3_evidence_ui_state`。这只是坐标/证据层，
不会把未知地图帧或对象标记伪装成已经确认的运行时画面。

### Finding 58：小地图底图合成、对象标记和点击坐标转换路径已闭合（2026-08-09）

在 `0x0043DA80` 的地图绘制函数中确认，原版流程不止选择 MMap 帧：`0x0043DB0B–0x0043DB2B`
调用 `0x004542F0`，把地图/视图数据合成到固定小地图区域；随后通过共享绘制辅助
`0x0045E570` 画出多个地图对象标记。已恢复的直接调用包括：

- `0x0043DB7F`：计算对象矩形后四边扩展 4 像素，颜色参数 `0x64C864`；
- `0x0043DCB8`：遍历全局链表 `0x00560070`，只处理对象 `entry+0x88 == 0x32`，矩形四边扩展 2 像素，颜色参数 `0xFFFF00`；
- `0x0043DD75`：遍历全局链表 `0x005600A0`，矩形四边各扩展 1 像素，颜色参数 `0x64C864`。

`0x0043DDB0` 的命中测试直接使用 `owner+0x2C0` 的 128×128 Rect，并把点击位置转换到
`owner+0x2F0/0x2F4` 的视图相对字段。这样已经得到小地图“底图 + 标记 + 点击换算”的
静态结构；但由于二进制中尚未确认两个全局链表的业务类名，当前只能称为地图对象标记，
不能擅自命名成玩家、NPC、队友或地面物品。

### Finding 60：MMap/FMMap 的多分辨率尺寸族确认完整地图资源分工（2026-08-09）

读取原版 WIL 17 字节帧头后确认：`MMap.wil` 的有效帧包含 `600×400`、`300×200`、
`152×100` 和 `76×50` 等尺寸族，绝大多数帧偏移为 `(-24,-16)`，小尺寸 35–38 使用
`(7,-44)`；`FMMap.wil` 则包含 `1200×800`、`900×600`、`600×500`、`600×400`、
`600×200`、`600×600` 及 `452×300` 等尺寸族。

这与机器码中“先选择资源帧，再裁剪/缩放到 128×128 小地图 Rect”的路径一致：MMap 是
多分辨率小地图图像族，FMMap 是完整地图/大地图图像族候选。尺寸和帧列表已写入
`map-ui-resource-evidence.json`，但具体地图编号到 FMMap 帧号的业务表仍需继续从加载参数
和完整地图打开入口追踪，不能按帧号顺序臆测地图名称。

### Finding 61：地图资源选择、绘制、命中和尺寸模式调用链已串联（2026-08-10）

进一步追踪地图类的调用者，得到以下静态链：`0x00420C3C → 0x0043D780` 传入
`(word & 0xFFFF)-1` 进行地图编号/资源帧选择；主世界绘制 `0x004295B4 → 0x0043DA80`；
小地图 Rect 命中与坐标换算 `0x0042BDC0 → 0x0043DDB0`；交互/视图状态候选
`0x0042C75C → 0x0043DEB0`。另外 `0x0042CED2` 调用表面初始化并传入 `256×256`，
`0x0042CEF0` 传入 `128×128`，说明原版保留大地图表面与固定小地图表面两种运行模式。

这些调用点已加入 `map-ui-resource-evidence.json`。目前可以确认地图系统不是一个简单的
静态贴图，而是地图编号选择 → 表面尺寸模式 → 视图合成 → 对象标记 → 点击坐标转换的
连续链路；完整地图窗口的最终屏幕布局仍需从打开入口继续确认。

### Finding 62：预览器增加完整地图资源候选模式（2026-08-10）

`Tools/wilviewer.py` 的固定 800×600 证据预览新增“完整地图资源候选 / FMMap F0”模式，
把原版 `FMMap.wil` Frame 0（头部尺寸 `1200×800`）按比例显示在 800×600 视口内，并
明确标记为资源候选，不伪装成已确认的地图编号、窗口原点或最终缩放规则。主 HUD 模式
仍独立显示原版小地图 Rect `(672,0,128×128)`，两者不会混在同一层中。

### Finding 63：Frame 602 窗口容器坐标恢复，但业务归属应为公告窗口（2026-08-10）

此前把 `0x0042797E → 0x0043E260` 只记录成“Frame 602 窗口候选”，本轮重新检查了包装器和
后续控件构造。主初始化向该对象传入的原始参数为：

```text
ID=15, resource=main_ui_this+0x1c (GameInter), Frame=602,
x=107, y=110, width=584, height=252, trailing flags=0,3
```

因此，按通用窗口参数的前七个槽位解释，Frame 602 公告容器的屏幕矩形候选为
`(107,110)-(691,362)`。它与固定小地图 `(672,0)-(800,128)` 是两条不同的 UI 路径，
但不能再称为完整地图容器：Frame 602 的真实图像是公告板/公告窗口。

该包装器内部还直接构造两个 GameInter 控件：

- Frame `161/162`，大小 `28×26`，原始位置 `(655,16)`，调用 `0x0043E2BB`；
- Frame `606/607`，大小 `40×20`，原始位置 `(603,27)`，调用 `0x0043E2E4`。

两者的位置表达式来自包装器参数槽位和常量增量，已写入
`gameinter-cluster-43e260.json`。容器 Frame 602 的窗口原点是否在运行时再次叠加父窗口偏移，
以及 FMMap 的具体帧/滚动缩放绑定，仍需从地图打开、绘制和输入状态继续验证；不能只凭这组
参数断言最终画面中的地图内容已经完全恢复。

### Finding 64：Frame 602 公告对象进入主 UI 生命周期，并拥有独立绘制/命中分发（2026-08-10）

继续追踪主对象字段 `main_ui_this+0x52E5C`，确认它不是只在初始化时短暂创建的贴图容器：

- `0x0043E0E0` / `0x0043E170` 分别负责该对象的初始化/释放；
- `0x0043E680` 遍历两个子控件并调用共享控件虚表的 `+0x08`，是子控件绘制/更新分发候选；
- `0x0043E640` 遍历两个子控件并调用虚表 `+0x0C`，是命中/事件分发候选；
- 主 UI 生命周期在 `0x004271CC` 和 `0x00427513` 通过该对象虚表的 `+0x04/+0x08` 调用它。

这闭合了“公告窗口构造 → 主 UI 生命周期 → 子控件绘制/命中”的证据链。完整地图的专用
UI 容器仍未从当前静态证据中确认，不能用 Frame 602 代替；FMMap 的资源绑定和地图表面
绘制链仍需与真正的地图打开入口继续关联。

### Finding 65：原版静态窗口清单中没有独立好友窗口构造（2026-08-10）

对 `Mir3.exe` 中全部 15 个通用窗口基类调用及主 HUD 的帧对进行了归档。已能分别归类为
背包、状态、商店、交易、行会、组队、聊天、组队附属、选项、任务、坐骑、技能/其他、NPC、
确认/提示和公告 ID 15；没有出现一个可以独立命名为好友/好友列表的窗口构造函数，也没有
在主 HUD 的原版帧对表中发现专用好友按钮。

这是一条“静态范围内未发现”的负证据，不等于功能绝对不存在。当前最合理的待查方向是：
好友列表作为行会/社交 F600 的页签或状态、由动态分配的通用对话框承载，或藏在未归属的
`Interface1c.wil` 控件簇中。已写入 `social-window-render-evidence.json.friend_entry_audit`，
并禁止预览器再凭现代客户端概念硬编码一个好友按钮。

### Finding 66：原版 Mud3 MiniMap.txt 闭合 FMMap/MMap 的服务器映射规则（2026-08-10）

检查原版服务器 `/home/tetsuya/NAS/TMP/Mud3/Envir/MiniMap.txt`，得到本发行版的明确规则：

```text
服务器值 >= 1001：FMMap.wil，frame = value - 1001
服务器值 <  1001：MMap.wil，frame = value
```

共解析 313 条配置记录，其中 45 条指向 FMMap、268 条指向 MMap；与 EI 客户端 `Map/*.map`
文件名匹配 211 条，WIL 帧实际可解码 209 条。`0 -> FMMap F0`、`01 -> FMMap F1`、
`02 -> FMMap F2`、`1 -> FMMap F3` 等基础映射均可直接复核。

这条证据说明完整地图资源并不是泛泛的“FMMap 候选”，而是服务器配置明确使用的资源族；但
它属于服务器配置的第二证据源。exe 内 `0x0043D780` 的 `map_id >= 1000` 分支与服务器值
之间仍需继续追踪调用者的归一化过程，不能把两个数值条件未经验证地当成同一个输入。

### Finding 67：纠正地图资源字段绑定，并闭合服务器值与 exe 分支（2026-08-10）

重新反汇编 `0x0043D4D0` 和 `0x0043D780` 后确认此前字段记录有误，正确关系为：

```text
owner+0x04   <- .\Data\MMap.wil   (literal 0x0047C428)
owner+0x148  <- .\Data\FMMap.wil  (literal 0x0047C414)
owner+0x2D0  <- 运行时矩形/状态字段，不是 WIL 资源句柄
```

`0x0043D780` 的分支是：`map_id >= 1000` 时选择 `owner+0x148` 的 FMMap，帧号为
`map_id-1000`；否则选择 `owner+0x04` 的 MMap，帧号为 `map_id`。而 `0x00420C24–0x00420C3C`
在调用前对网络/状态字段做 `word & 0xffff` 后再减一，因此服务器 `MiniMap.txt` 的
`1001 -> FMMap F0` 恰好归一化为 exe 的 `1000 -> FMMap F0`。

这修正了早期“owner+0x148 是 MMap、owner+0x2D0 是 FMMap”的错误表述；相关机器可读证据
已同步更新，后续地图 UI 不得再使用旧绑定。

### Finding 68：主 HUD 资源条不是静态贴图，原版明确计算 0–1 比例（2026-08-10）

反汇编 `Mir3.exe` 的 `0x00429740`，确认主 HUD 在绘制过程中先计算多个归一化比例，而不是
只把 F60/F61/F63 原图整张贴上去。关键路径使用 x87 `fild`/`fidiv`，并在比较后把结果钳制到
`0.0–1.0`；之后进入 `0x00466800` 的条带几何/纹理准备，再由 `0x004542F0` 合成。

当前可复核的第一条比例是 `low16(0x007D9264) / low16(0x007D9262)`，第二条为
`low16(0x007D9266) / low16(0x007DA113)`，经验为 `0x007DA115 / 0x007DA119`，
负重为 `low16(0x007DA109) / low16(0x007DA11F)`。同时在 `.data` 中直接解出
`(血量)%d/%d`、`(魔法量)%d/%d`、`(负重)%d/%d` 和 `(经验条)%.2f%s`，分别对应
这些全局字段的业务语义。
第一、第二条分别受固定 Rect `[this+0xC68] = (61,496)-(104,566)` 与
`[this+0xC78] = (105,496)-(147,566)` 约束。全局字段的业务名称仍不凭现代源码猜测，
完整地址、调用点和置信度已保存到 `hud-bars-render-evidence.json`。

同一绘制族还将经验比例乘以 `[0x0047644C]`，通过格式化函数 `0x0046811C` 写入
`[this+0xC88] = (235,586)-(400,597)` 对应的底部文字区域（`0x0042A065–0x0042A087`）。
因此主 HUD 的经验显示至少包含“比例条 + 百分比/进度文字候选”两层，预览器不能只复原
F63 的 164×6 贴图。

### Finding 72：组队窗口成员列表是两列链表绘制（2026-08-10）

在组队窗口绘制函数 `0x004243D0` 中，原版从 `this+0x58` 链表遍历成员，使用从 0 开始
的局部序号。`0x00424445–0x0042448E` 对序号做奇偶判断：偶数成员列位 0，奇数成员列位
1；每两名成员换下一行。精确窗口相对坐标为：

```text
x = window.x + 45 + 100 * (index % 2)
y = window.y + 90 + 20 * floor(index / 2)
```

成员字段最终交给共享文字绘制函数 `0x0045DD70`。因此组队窗口不应只实现三个按钮，
还必须按这个两列列表显示动态成员；成员字段的具体排列和链表可见上限仍保持待验证。

### Finding 73：行会窗口成员/公告列表的滚动几何与原文标记已恢复（2026-08-10）

行会绘制函数 `0x00425280` 从 `this+0xD4` 链表读取条目，以 `this+0x9C` 作为滚动起点，
可见数量由 `this+0xE4 - scroll_start` 给出并限制为最多 `0x12`（18）行。每行的窗口相对
横坐标固定为 `35`，纵坐标为 `60 + (index-scroll_start) * (font_metric_height+5)`；
行高由 `0x00425297–0x004252C8` 的原版字体度量计算出来。

原版还直接比较并识别 GB18030 字符串 `[联盟行会]`、`[敌对行会]`、`[行会公告]`，
说明行会页并非只有普通成员名字，还包含联盟、敌对和公告类别行。相关特殊颜色/字段
分支仍保持为待验证，不能在预览器中把三类标记误画成普通成员。

### Finding 69：技能窗口包装器的原始构造参数已补档（2026-08-10）

在主 UI `0x00427904` 的调用点确认技能窗口包装器 `0x00439250` 的压栈常量为
`3, 1, 0x17C, 0x1C4, 0, 0x15C, GameInter, 0x0E`。包装器会重新排列这些值后
调用通用窗口基类 `0x00423B30`，因此它们不能未经签名恢复就全部命名成 x/y/w/h。
该原始参数序列已写入 `skill-window-static-evidence.md`，用于后续完整恢复窗口原点和
拖动边界；现阶段仍以 Frame 400 尺寸及 `0x00439500` 重绘出的八个分类控件坐标为
可靠的窗口内部证据。

### Finding 70：背包是原版确定的 6×6、36 像素物品网格（2026-08-10）

`0x0042F150` 的命中/搜索循环分别以 `0,36,72,108,144,180` 扫描横纵轴，并以
`< 0xD8` 结束；`0x0042F2A0` 再将槽位索引除以 6，余数为列、商为行。由此可以直接
得到 36 个槽位的窗口相对矩形：

```text
x = window.x + 0x19 + 36 * column
y = window.y + 0x29 + 36 * row
size = 36 × 36
column,row = 0..5
```

同时复核 `0x0042EA80` 的调用者 `0x00427750`：它把主 UI 的 GameInter 资源句柄传给
库存窗口，窗口内部三个控件均使用同一句柄。因此 Frame 264/265 与 267/268 在此 EI
版本中也是 `GameInter.wil`，此前的 Interface1c 候选已纠正。

### Finding 71：状态窗口装备绘制使用 11 条连续位置记录和 0xC24 物品记录步长（2026-08-10）

`0x0044B6B0` → `0x0044B720` 的命中路径返回一个 `0..10` 的槽位索引；该索引同时
选择 `this+0x1C0+index*0x10` 的位置记录和 `this+0x2F4+index*0xC24` 的物品记录，
最终在 `0x004341F0` 绘制物品。11 条几何记录与构造器写入的 Rect 一一对应：

```text
0: (86,114)-(146,204)  人物图区域候选
1: (38,70)-(91,154)    属性区候选
2: (27,264)-(65,302)   装备候选
3: (177,70)-(215,108)  装备候选
4: (94,71)-(143,104)   姓名/头像区候选
5: (27,186)-(65,224)   装备候选
6: (175,186)-(213,224) 装备候选
7: (27,227)-(65,265)   装备候选
8: (175,227)-(213,265) 装备候选
9: (64,264)-(102,302)  装备候选
10:(103,264)-(141,302) 装备候选
```

其中索引 `0/1/4` 的代码分支把物品/人物绘制送到固定中央目标
`(window.x+0x61, window.y+0xC8)`，其余 8 个索引使用各自位置记录的前两个 dword
并加 `0x0F`。因此“8 个装备槽 + 3 个非装备显示记录”的结构已经闭合，尚待把 8 个
索引和具体的武器、首饰、戒指、手镯等业务名称对应起来。

### Finding 74：原版地图存在明确的 256×256 / 128×128 表面模式切换（2026-08-10）

反汇编 `0x0043DE40` 可见，例程先把 `owner+0x294` 作为布尔状态取反，然后根据新状态
调用同一个表面初始化函数 `0x0043D5F0`：真分支压入 `256,256`，假分支压入
`128,128`，两条路径均返回成功。该例程由 `0x0042C75C` 通过 `0x0043DEB0` 的地图
交互路径触发。

这证明原版并非只有一个固定尺寸的地图绘制状态：资源选择（MMap/FMMap）、地图表面尺寸、
视图矩形和最终合成是连续链路中的不同层。`0x0043DEB0` 先对固定小地图 Rect
`owner+0x2C0` 做 `PtInRect`，再检查两个输入状态（传入 `1` 与 `0x11`），并将点击坐标
按 `owner+0x2F0/+0x2F4` 与 `owner+0x2B8/+0x2BC` 换算后写回 Rect；因此坐标转换证据已
闭合，但两个输入状态的用户-facing 命令名称仍不能仅凭数值猜测。

服务器值与 exe 分支的关系已在 Finding 67 闭合：`MiniMap.txt` 的 `1001` 在调用
`0x0043D780` 前被减一为运行时 `1000`，从而选择 FMMap Frame 0；本条不再作为待解决项。

### Finding 75：聊天窗口的六个频道/命令字符串与固定位置已恢复（2026-08-10）

从 `0x00414080` 构造器的六个字符串参数回溯到 `.data`，并按 GBK 解码得到：

| 控件对象 | 窗口相对 X | 原始地址 | 原版字符串 | 内容含义 |
|---|---:|---|---|---|
| `this+0x120` | 25 | `0x0047AD08` | `拒绝和 某人 私聊(@拒绝 某人名)` | 拒绝某人私聊 |
| `this+0x1D4` | 65 | `0x0047ACF8` | `大喊话(!喊话)` | 大喊话 |
| `this+0x288` | 105 | `0x0047ACE4` | `编组 喊话(!!喊话)` | 编组/组队喊话 |
| `this+0x33C` | 145 | `0x0047ACD0` | `行会 喊话(!~喊话)` | 行会喊话 |
| `this+0x3F0` | 185 | `0x0047ACB8` | `拒绝 私聊(@拒绝私聊)` | 拒绝私聊 |
| `this+0x4A4` | 225 | `0x0047AC98` | `拒绝 行会 聊天(@拒绝行会聊天)` | 拒绝行会聊天 |

这些字符串与控件构造调用及固定 X 坐标一一对应，因此聊天窗口的频道/命令内容不再只是
根据按钮帧号推测。仍需从共用控件绘制函数确认它们最终表现为按钮文字、鼠标提示还是命令
说明；但字符串本身、地址、顺序和布局位置已经是原版静态证据。

### Finding 76：地图表面切换已经关联到原版键盘分发（2026-08-10）

继续检查主输入分发 `0x0042CC76–0x0042CF1F`，发现它调用键状态 IAT `0x00476278` 检查
`0x54`，随后切换主对象 `main+0x64A8`。当该字段由 0 变 1 时，原版直接调用
`0x0043D5F0(256,256)`；由 1 变 0 时调用 `0x0043D5F0(128,128)`。`0x54` 与 ASCII
字符 `T` 相符，但这里仅记录为键码/ASCII 候选，不把它擅自命名成“打开大地图”快捷键。

因此当前可以确定：地图模式切换不是我们手动拖动校准出来的，也不是现代客户端坐标推测，
而是原版主循环中的静态键盘分支；地图表面尺寸和切换状态均有机器码来源。另一个相邻的
`0x59` 分支只切换 `main+0x64A4`，其与地图 UI 的业务关系暂不命名。

### Finding 77：NPC 对话窗口的动态条目容量、步长和三段资源状态已补齐（2026-08-10）

NPC 构造器 `0x0043ED00` 将 `this+0x51C` 初始化为 `0x0D`，即默认最多 13 个动态
条目；绘制函数 `0x0043F040` 按 `entry_index*0x12`（18 字节）步长读取/生成条目，
分别经过资源/状态编号 `0x44C`、`0x44D`、`0x44E` 对应的加载与共享合成调用，并在
`this+0x530/+0x534`、`this+0x540/+0x544` 两组位置字段上绘制前后状态。

因此 NPC 窗口目前可以确定为：GameInter F1100 背景（552×176）+ 最多 13 行动态
内容 + F1101 重复条目状态 + F1102 最终状态。具体字段是 NPC 名称、对话文本、选项
还是动作按钮，仍需继续追踪条目填充调用；这里不以现代客户端命名替代原始字段证据。

同一构造器 `0x0043EDC5` 还把 `Data/NPCFace.WIL`（路径字符串 VA `0x0047C4EC`）绑定到
`owner+0x278`，原始库头为 440 帧、46 个非空帧。由此可将 NPC UI 的资源分成两层：
GameInter 提供窗口背景/控件，NPCFace 提供 NPC 对象的头像资源；不能把头像帧误列为
GameInter 帧。

### Finding 78：商店/仓库类状态机已找到协议分发入口（2026-08-10）

主消息处理区 `0x0042BE20–0x0042C359` 先通过 `0x0042AAB0` 读取子码，再用跳表
`0x0042C4D4` 分发。跳表第 2 项是 `0x0042BFE1`，该分支依次调用
`0x0044E910`（物品/商店数据处理候选）和 `0x0044E9B0`（窗口状态机）。因此“商店类
窗口完全没有入口”这一假设可以排除：原版存在明确的消息分发 → 数据处理 → UI 状态机链。

该链路仍不能单凭子码数值命名为“NPC 商店”或“仓库”，所以状态 0–4 的业务标签继续
保持候选；但现在已经有真实的协议入口地址，可以继续沿参数字段和服务端处理寻找名称。

### Finding 79：服务端脚本确认仓库、购买、出售 NPC 入口（2026-08-10）

新增 `Tools/extract_mir3_store_server_crossref.py`，只读解析 `Mud3/Envir/Merchant.txt`
以及 `Market_Def`、`Convert_Def/Market_Def` 脚本，并把商店类服务端资料写入
`store-server-crossref.json`。共发现 318 条商人记录，全部能匹配到脚本；其中 19 条包含
`NPC_Storage`/`NPC_GetBack` 仓库存取入口，108 条包含购买入口，108 条包含出售入口。

三个可复核示例：

- `19GM_INN-Z014` 的 `[NPC_Main]` 暴露“寄存/取回”，并有 `[NPC_Storage]`、
  `[NPC_GetBack]` 段落及对应提示文本；
- `06Inn_Oasis` 的服务端名称为“绿洲仓库保管员”，归入仓库入口；
- `04Potion_Bichon1` 的“药店老板”同时存在 `[@NPC_Buy]` 与 `[@NPC_Sell]`。

这些资料确认原版时期的服务端业务入口确实存在，并可作为客户端 F1000 商店/仓库窗口
继续追踪的第二证据源；但服务端 NPC 名称和脚本分类不能证明客户端状态 0–4 的具体数值
含义，也不能替代 Mir3.exe 的绘制与命中证据，因此相关业务标签仍保持 candidate/pending。

### Finding 80：地图对象的固定小地图 Rect 与内部视图状态不是同一层（2026-08-10）

重新核对 `0x0043D4D0–0x0043DA80` 的连续机器码后，地图对象至少包含三类不同几何：

- `owner+0x2C0` 在构造器中通过 `SetRect` 固定为 `(672,0)-(800,128)`，这是屏幕上的小地图
  目标区；
- `owner+0x2B8/+0x2BC` 保存视图位置，`0x0043D5F0` 会依据资源源图尺寸限制它，并重新建立
  同一个目标 Rect；
- `owner+0x2D0` 保存绘制过程使用的内部视图/裁剪 Rect，`owner+0x2E0` 保存所选 WIL
  Frame 的源尺寸/偏移换算结果。`0x0043D780` 在选帧后才调用 `0x0043D5F0`，所以不能
  把 WIL 图片尺寸直接当成屏幕窗口尺寸。

`0x0043DA80` 的绘制链先把视图位置、目标 Rect 和缩放参数交给共享合成函数
`0x004542F0`，再按三个不同的全局对象链绘制绿色/黄色候选标记。由此，预览器新增的
“固定小地图 128×128 候选”只展示屏幕目标和资源缩放关系；完整地图窗口、边框、滚动条
及标记业务语义仍保持 pending，避免用一张拉伸后的 FMMap 图伪造原版布局。

### Finding 81：公告窗口的 800×600 父窗口原点由初始化参数直接闭合（2026-08-10）

反汇编 `0x00427960–0x0042797E` 的参数压栈顺序为：窗口 ID `15`、主 UI 资源句柄、
Frame `602`、`x=107`、`y=110`、`width=584`、`height=252`、末尾标志 `0`，随后调用
`0x0043E260`。因此公告窗口在固定视口中的原点和外框候选不是 `(252,110)`，而是准确的
`(107,110)-(691,362)`；此前记录已更正。

`0x0043E260` 的两个子控件仍以父窗口参数表达相对位置：关闭/确认类控件为
`(548,16)`、`28×26`，另一个公告动作控件为 `(496,43)`、`40×20`。如果父窗口原点
直接参与最终屏幕合成，它们对应 `(655,126)` 与 `(603,153)`；这两个屏幕位置已记录为
派生值，子控件最终业务语义和文字绘制仍保持候选。
### Finding 82：确认框 F950 存在原版固定中心定位规则（2026-08-10）

`0x00418030` 的构造路径在位置参数为 `-1/-1` 时，从资源句柄 `this+0x45C` 读取图像尺寸，
按固定中心 `(400,246)` 计算父窗口左上角。对应 `GameInter.wil` Frame 950 的原始尺寸
`360×190`，得到 `(220,151)-(580,341)`。这条坐标来自构造器算术和 WIL 头部，不是人工
校准；机器可读结果已写入 `confirmation-prompt-evidence.json`。

目前仍不能仅凭静态代码证明所有确认框调用都走 `-1/-1` 分支，也不能把 151/152、154/155、
157/158 的业务文字和颜色状态全部命名，因此状态机和运行时分支继续保持候选。

### Finding 83：聊天记录步长与屏幕视觉行距已分离（2026-08-10）

在聊天绘制函数 `0x00414700–0x00414999` 中，链表记录从 `this+0x720` 按 `0x10` 字节
移动，但用于屏幕绘制的局部索引每行增加 `0x0E`。结合构造器建立的 Rect，可恢复：

- 文字绘制起点为窗口相对 `(40,29)`；
- 相邻可见行的视觉 Y 步长为 `14px`；
- 每行裁剪 Rect 第一行相对值为 `(35,28)-(520,43)`，后续每行上下各增加 `14px`；
- 记录内存步长仍是 `16` 字节，不能把它误当成字体行距。

该结论来自 `0x004147BA–0x0041481F` 的坐标计算和 `0x0041496D–0x00414997` 的逐行
裁剪循环。频道字符串仍经共享控件绘制，具体字体颜色和字符串是否同时作为 tooltip 继续
保留待验证。

### Finding 84：商店状态值的按钮分支与面板重建路径已分开记录（2026-08-10）

在 `0x0044E910–0x0044EA07` 和 `0x0044E9B0` 中，`this+0x5F8` 的状态字节出现且只比较
`0、1、2、3、4`。静态行为如下：状态 1/3/4 进入第一组按钮/命中路径，状态 1/2/4
进入第二组路径；状态 2 的第二组路径可在控件处理成功后直接返回。命中测试使用窗口
对象的 `this+0x18/+0x1C/+0x20/+0x24` 几何字段，并在状态分支中出现相对偏移
`(300,208)`、`(300,100)`。

状态切换后的工厂调用也能闭合资源层：状态 0/3 重建 `GameInter F1000` 的
`300×304` 面板，状态 2 创建 `F1001` 的 `205×205` 紧凑面板，状态 4 创建
`F1002` 的 `540×307` 宽面板，状态 1 创建 `F1003` 的 `498×304` 扩展面板。
这些是机器码中的 Frame/尺寸/调用关系，不是“仓库”“购买”“出售”等业务名称；业务
映射仍必须依靠消息参数和服务端脚本继续交叉，不能由视觉相似度命名。

### Finding 85：人物状态窗口的绘制状态与装备循环已由反汇编闭合（2026-08-10）

继续反汇编 `0x0044B2D0–0x0044B629` 后确认，状态窗口并非只有静态 Frame 200：入口读取
`this+0x54`，状态 0 和 1 都经过 `0x0044B560` 的准备/合成路径，并建立相同的裁剪表达式
`SetRect(window.x-0x0A, window.y+0x1E, window.x+0xFF, window.y+0x32)`，随后调用
`0x0045DBA0`、`0x0045DE50`、`0x0044BC80`、`0x00466130` 和 `0x0045FD50` 等共享绘制链。

`0x0044B5D9–0x0044B629` 是 11 次迭代的装备/角色记录循环：物品记录从 `this+0x2F4` 开始、
步长 `0xC24`，位置记录从 `this+0x1C0` 开始、步长 `0x10`，空记录以当前基址首 dword 为零
跳过。索引 0、1、4 使用固定中心目标 `window origin+(0x61,0xC8)` 并把合成标志设为 1；
其它非空索引使用位置记录的两个 dword 加窗口原点和 `0x0F` 偏移，并把标志设为 0，最终调用
`0x00430A40`。这强化了装备槽/角色区的机器码证据，但仍不能仅凭静态代码命名每个索引对应
的武器、头盔、项链、戒指等业务栏位。

完整原始参数已同步到 `status-window-render-evidence.json` 的
`paint_state.primary_disassembly_details`，所有业务名称和运行时资源句柄继续保持待验证。

### Finding 86：人物属性文字的中文语义与两列基线已从原版字符串引用恢复（2026-08-10）

`0x0044BC80–0x0044CCCC` 是状态窗口属性文字辅助路径。它直接把 Mir3.exe 内的 GBK 字符串
传给 `0x0046811C` 格式化，再经 `0x0045DD70` 合成到窗口：第一列从
`window.x+0xFF, window.y+0x43` 开始，每行步长 `15px`，依次包含 `LEVEL`、`HP`、`MP`、
`经验`、`包袱负重`、`装备负重`、`腕力`、`准确`、`敏捷`、`毒物躲避`、`中毒恢复`、`生命恢复`、
`魔法恢复`；第二列在代码中执行 `x+=0x17F,y+=0x1E` 后，从
`window.x+0x27E, window.y+0x127` 开始，包含 `防御`、`攻击`、`魔法`、`火(火焰)`、`冰(冰冻)`、
`电(雷电)`、`风(狂风)`、`治疗(神圣)`、`攻击(黑暗)`、`召唤(幻影)`、`魔法防御力`。

这次恢复的是原版字符串引用和绘制基线，不是根据现代客户端或截图猜标签。对应字符串地址、
引用指令和数值格式化调用已写入 `status-window-render-evidence.json` 的
`attribute_text_draw_chain`；属性值对应的全局字段、字体颜色和最终 z-order 仍单独标记为待验证。

### Finding 87：地图模式键盘入口的守卫、状态字段和两种表面尺寸已闭合（2026-08-10）

在 `0x0042CC76–0x0042CF1F` 的同一输入分发函数中，键码由 `0x00476278` 间接读取。键码
`0x54` 只有在 `main+0x6518 == 1` 的地图/世界子系统状态下才进入切换：
`0x0042CEA5–0x0042CEBA` 翻转 `main+0x64A8`，非零分支调用 `0x0043D5F0(256,256)`，
零分支调用 `0x0043D5F0(128,128)`。因此这是原版明确存在的地图显示表面切换入口，数字键码和
调用参数是静态确定的；“T键”“大地图”“小地图”等用户界面名称仍不能只凭二进制命名。

相邻键码 `0x59` 在 `0x0042CEF7–0x0042CF19` 翻转 `main+0x64A4`，但该分支没有直接重建
地图表面，暂记为同一客户端状态机的未命名相邻功能。上述信息已同步到
`map-ui-resource-evidence.json` 的 `mode_switch.keyboard_dispatch_evidence` 和
`adjacent_key_evidence`。

### Finding 88：商店状态面板已加入工厂算法与逐状态800×600预览（2026-08-10）

`0x00423E80` 的反汇编确认它不是简单把调用参数写成屏幕坐标：先用 `0x00466130` 选择资源，
从资源句柄 `this+0x2C` 的 `+0x38` 读取 WIL 头部尺寸，建立栈上局部 RECT，再把计算结果写入
对象的 `this+0x40/+0x44`，最后在 `0x00423F55` 和 `0x00423F6D` 对 `this+0x08`、`this+0x18`
执行最终矩形设置，函数以 `ret 0x14` 清理五个参数。由此，状态 0–4 的原始调用参数和最终
父窗口定位必须分开保存。

预览器新增“商店状态0–4”五种模式，分别使用 F1000/F1003/F1001/F1000/F1002，并按
`(800-width)/2,(600-height)/2` 显示一个明确标注为“工厂居中候选”的观察位置，同时显示原始
工厂调用参数和状态命中矩形。该观察位置是可视化推导，不提升证据等级；原始工厂算法与参数
仍是唯一坐标依据。

### Finding 89：任务窗口的列表与详情正文绘制基线已闭合（2026-08-10）

在 `0x00447470` 任务绘制函数中，任务列表通过 `this+0x1E8 -> 0x0045E0C0` 取得记录，
文字基线为 `window.x+0x41, window.y+0x5A+row*15`，并受 19 行边界保护。任务详情背景
路径在 `0x00447E07` 选择 GameInter Frame 705，并从 `window.x+0x41,window.y+0x126`
进入正文区；随后 `this+0x6C` 的正文字符串按 `this+0x60 <= line < this+0x60+3`
显示最多三行，正文起点为 `window.x+0x50,window.y+0x136`，行距同为 `15px`，文本测量
上限为160字节。列表与正文都经共享 `0x0045DD70` 合成。

这些坐标和行数已写入 `quest-window-render-evidence.json`，任务预览也从原先的大范围详情
候选框改为 Frame 705 背景条与三行正文证据框；任务标题、字段分隔符和滚动业务语义仍保持
待验证。

### Finding 91：地图从源图到固定小地图的归一化合成链已补全（2026-08-10）

在 `0x0043DA80` 的地图绘制函数中，程序先读取 `owner+0x2C0/+0x2C4` 的目标视图尺寸，
再结合 `owner+0x2B8/+0x2BC` 的当前视图位置，调用共享浮点归一化助手 `0x00466800`。
随后在 `0x0043DB0B–0x0043DB2B` 通过公共合成器 `0x004542F0`、上下文
`0x005600FC` 送入固定地图目标。`owner+0x2D0/+0x2D4` 是源偏移/裁剪状态，不能与屏幕
坐标混用；`owner+0x300` 还参与一个 0 到 800 的中间动画/时序值。

合成参数还受 `owner+0x290` 分支影响：零值路径使用 `1.0f`，非零路径使用静态浮点常量
`0x3F2FAFB0`。这说明仅把 FMMap/MMap 原图缩放到 128×128 会丢失原版的裁剪、视图和
透明度行为。上述字段、调用点和分支已写入 `map-ui-resource-evidence.json` 的
`render_evidence.source_to_view_transform`，后续 Zircon 还原应实现这条数据链，而不是手动
校准一张截图。

### Finding 90：NPC 对话窗口的动态条目绘制循环已从静态代码闭合（2026-08-10）

继续检查 `0x0043F040` 绘制函数后，Frame 1101 并不是一个只显示一次的装饰图，而是在
`0x0043F0B2–0x0043F10B` 中按 `this+0x51C` 次循环绘制。循环索引为 `edi`，条目偏移寄存器
从 0 开始，每次增加 `0x12`（18 字节）；共享合成器为 `0x00460240`。条目的目标坐标来自
`this+0x530`/`this+0x534`，其中 Y 明确按 `entry_index*18` 递增，X 路径在
`0x0043F0FA` 对读出的基准值再加 1。构造函数默认计数是 13，但运行时计数仍必须以对象字段为准。

循环之后，程序在 `0x0043F120` 选择 Frame 1102，并在 `0x0043F16D` 绘制最后一个条目；其
索引是 `max(count-1,0)`，目标为 `this+0x540` 与 `this+0x544+index*18`。因此现在可以确定
NPC 对话框的动态区具有 18px 行节奏和“重复条目 + 最终选中/状态条目”的两阶段绘制结构，
但不能把 `this+0x530` 等字段直接误命名为屏幕坐标或正文字符串。

当全局 `0x008B1874 == 0` 时，函数从 `0x0043F179` 进入另一条归一化/透明度合成分支，读取
`this+0x520/+0x524/+0x528/+0x52C`，调用 `0x004542A0` 和 `0x00466800`。这条分支已记录为
静态候选，暂不解释为 NPC 业务文本。预览器现在将 13 个 18px 行框和最后条目框明确标为
candidate，避免把运行时字段尚未解析的目标位置伪装成已证实坐标。

### Finding 92：背包选中物品不是简单格子贴图，而是原版矩形合成链（2026-08-10）

重新反汇编 `0x0042EB7F–0x0042F050` 后，背包的选中物品路径在 `0x0042EC8C–0x0042EE2A`
先由 6×6 命中结果建立源矩形和目标矩形，再经 `0x00466800` 做浮点尺寸/坐标归一化，最后
通过 `0x004542F0`、上下文 `0x005600FC` 合成。这证明背包图标与选中态还原时应保留原版
的资源矩形和合成参数，不能只在 36×36 格子里放一个缩略图。

同一绘制函数还存在两组独立文字/数值绘制路径：`0x0042EE62` 通过 `0x0046811C` 使用
数据字符串 VA `0x0047A214` 的 `%d` 格式；`0x0042EFC4–0x0042F003` 通过 `0x0045DE50`
绘制第二组固定参数文字。当前已精确记录调用地址、窗口相对基线和共享合成器，但全局字段
对应“数量/名称/负重”等业务含义仍不以猜测命名，继续保留 pending。

### Finding 93：人物状态装备的选中资源由原版表驱动选择（2026-08-10）

在 `0x0044B560–0x0044B6A8`，状态窗口不是把选中装备固定画成某个 Frame：程序读取
`0x00777720` 的低字节，调用资源选择器 `0x00466130`，使用表 `0x00566DD4`，并从
`0x00566E0C/+0x04` 取得所选资源的尺寸/头信息，再交给 `0x0045FD50` 合成到人物中心
目标 `window+(0x61,0xC8)`。另一条分支还使用索引表达式
`low8(0x00777720)*10 + low8(0x00777723) + 0x3B`。

这条证据确认了装备图标/覆盖图的真实机制是“运行时类型或状态 → 原版资源表 → 资源头尺寸
→ 合成目标”，而不是现代 Zircon 的静态装备图标映射。字段的业务名字和每个索引对应的
武器、头盔、项链等名称仍未从原版符号中得到，因此继续标为候选，不强行命名。

### Finding 94：Frame 602 窗口的真实文字是行会公告/行会修改占位内容（2026-08-10）

对 `0x0043E260` 的绘制邻域继续追踪，在 `0x0043E3C0` 分支中发现了两组直接引用的 GBK
字符串：`0x0047C440` 为“[行会公告，请自行修改公告内容.]”，`0x0047C460` 为
“[行会修改 请自行修改行会等级、成员排行信息]”。两者都通过 `0x0045DD70` 绘制，主文字
基线为窗口相对 `(23,94)`；`this+0x1D0` 决定使用哪一组，随后还有相对 `(24,95)` 的辅助
文字路径。

因此 Frame 602 不能只标成无语义的“公告框”：内容证据明确指向行会/公告信息。但构造器的
主初始化参数仍是 id15、Frame602、`(107,110,584,252)`，二进制没有在这一段直接证明它
是否是独立提示窗口，还是行会窗口内部的一个状态。预览和 JSON 已记录真实字符串、分支、
坐标和颜色，同时保留“公告/行会信息候选”的归属状态。

### Finding 95：F950 确认框包含参数驱动的消息区域高度分支（2026-08-10）

在确认框包装器 `0x00418030` 中，构造参数在 `0x004181E5` 被测试；随后通过已确认的
`SetRect` IAT `0x004762B0` 设置 `this+0x18` 消息区域。非零参数分支的原始表达式为
`left=[esi]+0x18, top=argument_y+0x17, right=[esi]+0x14D, bottom=argument_y+0x64`，
零参数分支保持左右和顶部表达式不变，但底部改为 `argument_y+0x78`。这说明 F950 的
消息区并非固定按截图手调，至少存在两种由构造状态决定的高度。

RECT 设置后，代码调用 `0x004762BC`、`0x004762B8` 和 `0x004762AC`，参数中出现资源/上下文
`0x008AA48C`、常量 `0x135` 和视图偏移字段；由于这些是 IAT 间接调用，当前只把它们记录
为文本/字体资源操作候选，不把 API 名称或文字内容过度解释。三组按钮仍由
`0x00418176/AB/E0` 直接构造，父框、消息区和按钮状态现在可以在同一 JSON 中分层复核。
### Finding 96：商店构造函数的右侧物品网格确认为 4×3（2026-08-10）

`0x0044D4C4–0x0044D53B` 的商店构造函数实际初始化了三组矩形：左侧 `this+0x660` 为 5 行、
`(left,right)=(28,64)`、起始 y=26、步长49、高36；左侧文字/说明区 `this+0x6B0` 为 5 行、
`(left,right)=(69,256)`、起始 y=21、步长49、高45；右侧 `this+0x720` 的嵌套循环边界为
x=323,361,399,437 与 y=43,81,119，严格是 `4列×3行`、每格 `37×37`、步长38。

这纠正了此前把右侧网格写成“5×4 candidate”的错误。上述数字来自连续 `SetRect` 调用，
已写入 `store-window-render-evidence.json` 的 `constructor_rect_initializers`；它们仍是
窗口参数坐标，不能因为超出 F1000 的当前可见宽度就擅自平移或裁剪。

### Finding 97：商店绘制的可见商品列表上限为 5，且资源/价格链已闭合（2026-08-10）

在 `0x0044D631–0x0044DB15` 的主绘制循环中，商品链表头为 `this+0x64C`，节点下一指针为
`node+0x04`，商品资源 ID 为 `node+0x30`。局部行索引从 0 开始，达到 5 后退出，因此原版
当前窗口的可见商品行上限是 5；之前预览器使用 8 项是错误候选，已改为读取构造器的 5 行
矩形。

每个商品资源通过 `0x00466130` 和表 `0x0056B0E8` 选择，资源头尺寸来自 `0x0056B120` 等字段，
再经 `0x00466800` 归一化并由 `0x004542F0` 合成。对应文字走 `0x0046811C` 与
`0x0045DD70`，格式字符串 VA `0x0047C784` 的原始字节为 `(%d两)`，可确定存在价格/数量
类数值显示，但不把它单独命名为“价格”还是“重量”。

同函数后半段 `0x0044DB50–0x0044E021` 处理选中商品与分页：选中索引字段为 `this+0x7E4`，
记录入口为 `this+0x728+index*0x10`，状态 2 的页数按 `ceil((this+0x71C)/12)` 计算。
这些是静态字段和算法证据；商品业务是购买、出售还是仓库取存，仍需与状态入口绑定。

### Finding 98：行会窗口的三态绘制分支与九个控件位置已闭合（2026-08-10）

在行会窗口绘制函数 `0x00425040` 中，`this+0x98` 明确分派到三个子绘制函数：状态 0 调用
`0x00425280`，状态 1 调用 `0x00425440`，其它状态调用 `0x00425590`。这说明行会窗口不
是单一静态页，而是至少有三种内容/页签绘制状态。

同一函数在 `0x00425152–0x00425258` 通过 `0x00417830` 重新设置九个子控件的位置；相对
窗口原点的坐标依次为：`this+0x118=(556,409)`、`+0x1CC=(34,376)`、`+0x280=(34,402)`、
`+0x334=(121,402)`、`+0x3E8=(309,376)`、`+0x49C=(397,376)`、`+0x550=(484,376)`、
`+0x604=(309,402)`、`+0x6B8=(397,402)`。这些是绘制阶段真实的 SetPosition 参数，优先级
高于构造阶段寄存器尚未完全命名的表达式；Frame 610–625 的具体标签业务仍保持待绑定。

### Finding 99：组队窗口直接绘制“允许/拒绝”状态文字并重定位五个控件（2026-08-10）

在 `0x004243D0` 的组队窗口绘制函数中，成员链表仍从 `this+0x58` 读取，列表项通过
`0x0045DD70` 绘制；另一个状态字段 `this+0x3F0` 在 `0x00424532–0x00424570` 选择原版
GBK 字符串 `0x0047BA00=[拒绝]` 或 `0x0047BA08=[允许]`。这证明组队窗口确实包含权限/邀请
类状态显示，而不是只有成员名字。

同一段还通过 `0x00417830` 重新设置子控件：`this+0x6C=(226,214)`、`+0x120=(17,197)`、
`+0x1D4=(80,197)`、`+0x288=(159,197)`、`+0x33C=(9,52)`，均为窗口相对绘制阶段位置。
文字基线的寄存器来源还需运行时或更完整调用者上下文确认，因此保留 candidate；控件位置和
“允许/拒绝”字符串本身已是原版静态证据。

### Finding 100：聊天窗口的频道语义、滚动行数与重绘坐标已补齐（2026-08-10）

在聊天窗口刷新函数 `0x004142C0–0x0041482A` 中，原版从 `this+0x5C` 遍历聊天记录链表，
最多绘制 19 行；记录结构步长为 16 字节，但屏幕文字的视觉行距由绘制循环的 `0x0E` 确定，
即 14 像素。共享文字合成调用位于 `0x004147F3`，文字来源候选为 `node+0x08`，目标坐标
表达式为 `x=this+0x6C0+window.x`、`y=this+0x6C4+window.y+row_offset`。这比单纯用窗口背景
或现代客户端布局推测更接近原版真实绘制链。

六个频道/命令控件的状态分支位于 `0x00414A24–0x00414C00`，状态检查对象偏移依次为
`this+0x120/+0x1D4/+0x288/+0x33C/+0x3F0/+0x4A4`，对应静态命令字符串分别为：
`@拒绝 `、`!`、`!!`、`!~`、`@拒绝私聊`、`@拒绝行会聊天`。结合构造器中的完整中文说明，
可确定它们分别代表私聊拒绝、普通喊话、组队喊话、行会喊话、拒绝私聊开关、拒绝行会聊天开关。
是否把中文说明直接显示为按钮文字，仍取决于共享控件实现；预览器将其标记为命令语义候选，
不伪装成已经确认的 UI caption。

刷新阶段还会重新设置频道控件的固定窗口相对坐标：`(25,332)`、`(65,332)`、`(105,332)`、
`(145,332)`、`(185,332)`、`(225,332)`；关闭/首控件为 `(532,350)`。这些坐标与构造器坐标
一致，已写入 `chat-window-render-evidence.json` 并在 `wilviewer.py` 的聊天模式显示命令语义框。
输入框解析邻域还确认了 `/`、`(`、`)`、空格和冒号等语法标记，以及 `/%s ` 格式字符串；
剩余待确认项是共享文字渲染器的字体、颜色、裁剪和记录字段的精确顺序。

### Finding 101：任务列表存在长度分支与状态颜色分支（2026-08-10）

重新核对任务刷新函数 `0x00447470` 的完整反汇编后，任务记录通过 `0x0045E0C0` 解析，文本
记录字段候选为 `record+0x04`。列表文字仍以窗口相对 `(65,90)` 为首行、15 像素为行距，
最多受 19 行守卫限制；但长度超过 200 字节会进入 `0x0044755C–0x0044764E` 的长记录路径，
短记录走 `0x0044777E–0x00447824`，因此不能把所有任务标题都当作单一固定宽度字符串。

长短路径都会依据记录附近的 `+0x204/+0x210` 状态字段选择不同颜色常量候选
`0x0019197D/0x001919C8`，再调用共享文字合成 `0x0045DD70`。详情正文同样存在长行处理路径，
单行长度阈值候选约 190 字节，正文区域只显示当前滚动窗口的 3 行、15 像素行距。颜色常量
和记录字段顺序仍标为候选，但长度分支、列表坐标、详情坐标与 19/3 行上限均已固化到 JSON。

### Finding 102：系统设置窗口的九个控件在绘制阶段再次确认（2026-08-10）

系统设置窗口构造函数 `0x0044103E` 使用 Frame 750、窗口大小 `248×264`，随后绘制/重定位函数
`0x00441380` 通过通用位置函数再次写入九个控件的位置：关闭控件 `(218,238)`；两列开关
分别在 `(148,43)/(185,43)`、`(148,116)/(185,116)`、`(148,190)/(185,190)` 和
`(148,217)/(185,217)`。这些位置来自 `0x0044139E–0x0044148A` 的窗口相对坐标表达式，
因此优先级高于单次构造调用。原版还在 `0x00441CC0` 读取配置文件并把结果写入对象/全局字段，
但当前没有把这些字段强行命名成具体“音效、显示”等现代设置；预览器只显示静态控件几何框，
设置标签和状态字段继续保留为待解析内容。

### Finding 103：背包选中物品与数值文本的状态门控已补齐（2026-08-10）

背包绘制函数 `0x0042EB7F` 的选中物品分支并不是无条件绘制：它先检查全局字段
`0x007DA1C0/0x007DA1C4` 和 `0x007243D8`，再通过 `0x0042F150`、`0x0042F2A0` 解析鼠标/选中
槽位，最后进入源矩形、目标矩形、浮点归一化 `0x00466800` 和合成 `0x004542F0`。原版还在
`0x0042EB9B` 选择 GameInter Frame 94（十六进制 `0x5E`）作为窗口顶部/详情组合素材。

数值文字链也已从反汇编中具体化：主数值读取 `0x007DA100`，格式字符串地址为 `0x0047A214`，
目标布局使用窗口相对 x=`0x41`、y=`0x11A/0x12B`；第二组文字读取
`0x007DA11D/0x007DA11F`，并由 `this+0x54` 的四态分支选择格式/颜色路径，格式字符串候选位于
`0x0047BDFC`、`0x0047BE10`、`0x0047BE18`。这些字段已经写入背包证据 JSON，但由于没有
符号和运行时数据，仍不把它们擅自命名为“负重、金币或数量”。

### Finding 104：交换窗口的左右交易区由原版构造器明确二分（2026-08-10）

交换窗口构造函数 `0x004159D0` 在创建三个控件前，直接读取对象边界字段
`this+0x18/+0x1C/+0x20/+0x24`，计算 `center_x=(left+right)/2`，然后通过两个 `SetRect`
调用把 `this+0x5C` 设为 `[left,top,center_x,bottom]`，把 `this+0x6C` 设为
`[center_x,top,right,bottom]`。因此 Frame 1050 的交易界面确实是左右双方区域，而不是
把两个候选面板凭视觉拼接出来。

交换绘制函数 `0x00415B10` 还给出了双方物品格的真实步长：状态 0 的起点为窗口相对
`(0x15,0x30)`，状态 1 的起点为 `(0xFD,0x30)`，横纵步长均为 36 像素；行索引来自
`this+0x54` 或其状态索引表。选中物品仍走源/目标矩形、`0x00466800` 归一化和
`0x004542F0` 合成链。两个 Frame 94 侧边资源还分别在 `(window.x+0xD1,window.y-0x73)`
和 `(window.x+0x1B9,window.y-0x73)` 被绘制。交换窗口的最终屏幕原点仍由父对象传入，
所以保留窗口原点待确认，不把构造参数直接当成 800×600 绝对坐标。

### Finding 105：坐骑窗口五个动作控件与点击分派已从原版闭合（2026-08-10）

坐骑窗口 Frame 850 的绘制/重定位函数 `0x004269C0–0x00426A56` 再次确认五个控件的固定
窗口相对位置：关闭 `(252,293)`，四个动作控件依次为 `(28,244)`、`(74,244)`、`(133,244)`、
`(192,244)`，与构造函数的 Frame 860–867 成对资源一致。

点击处理函数 `0x00426A80–0x00426B45` 按相同五个控件对象顺序进行命中测试，并根据全局
`0x007DA060` 选择不同动作/提示字符串候选 `0x0047B058/0x0047B060/0x0047B068`，最终进入
`0x004520F0`。其中 `0x008A68BC=0x12C` 和 `0x008A68C0=0` 是动作后的计时/状态写入候选。
因此坐骑窗口不只是静态背景，五个按钮的命中顺序和状态分支现在有原版机器码证据；具体中文
标签与坐骑字段仍保持待解析。

### Finding 106：商店/仓库共享面板状态图独立固化（2026-08-10）

为避免把商店、仓库和选中物品页混成一个业务窗口，新增
`store-state-graph.json`。它把协议入口 `0x0042BFE1`、共享状态字段 `this+0x5F8`、工厂
`0x00423E80` 与状态 0–4 的 Frame/调用点/原始工厂参数分开记录：状态 0/3 使用 F1000
`300×304`，状态 1 使用 F1003 `498×304`，状态 2 使用 F1001 `205×205`，状态 4 使用
F1002 `540×307`。同时明确记录工厂会继续做父级居中和 RECT 计算，因此这些参数不能直接
当作 800×600 屏幕坐标。

服务器 `Merchant.txt` 与 `Market_Def` 的 19 个仓储、108 个买入、108 个卖出分类只作为
二级交叉证据，暂不把任何一个客户端 state 强行命名为“仓库”或“商店”。

### Finding 107：NPC 对话条目数量、换行间距与动态按钮位置已闭合（2026-08-10）

在 NPC 对话数据准备函数 `0x00440750–0x00440AA0` 中，输入对话/菜单字符串按 `0x5C` 反斜杠
分隔，状态字段 `this+0x582`、源偏移 `this+0x584`、原始条目计数 `this+0x588` 最终生成
绘制计数 `this+0x51C = max(raw_count-6,0)`，并限制为 16 项；超过上限时设置 `this+0x58C`
溢出标记。绘制行距不是固定猜测：当 `this+0x582==1 && this+0x58C==1` 时为 14 像素，
否则为 21 像素，值存于 `this+0x594`。

同一准备路径还会按窗口底边重定位三个控件：`this+0x58` 到
`(window.x+0x15B, window.bottom-0x24)`，`this+0x1C0` 到
`(window.x+0x0B8, window.bottom-0x1E)`，`this+0x10C` 到
`(window.x+0x0C8, window.bottom-0x1E)`。这解释了为什么只看构造函数会得到不完整的 NPC
按钮布局；现在这些动态表达式已加入 NPC JSON，业务按钮名称仍保持待验证。

### Finding 108：窗口基类绘制门控与父背景先于子控件（2026-08-10）

窗口基类绘制函数 `0x00423D00` 只有在 `this+0x30 != 0` 且全局 `0x008B1874 != 0` 时才进入资源背景绘制；`this+0x28` 保存 Frame，`this+0x2C` 保存资源句柄，经 `0x00466130` 选择资源头后调用 `0x00460240`，源视口为 `800×600`。全局为 0 时会转入 `0x00466800` 的归一化/alpha fallback，这不是另一个独立窗口层。

这组机器码只能严格证明一个局部顺序约束：可见窗口的基类背景必须先于该窗口的派生绘制和子控件绘制；它不能证明两个可移动窗口之间谁覆盖谁。因此统一预览器将其标为 `base-before-child`，而把跨窗口 z-order 保留为 pending，避免把调用地址顺序误读成完整的运行时窗口管理顺序。

### Finding 109：公告预览器已接入原始子控件图层（2026-08-10）

统一证据预览的 `prompt.notice` 模式现在除 Frame 602 父容器外，还按
`0x0043E260` 包装器的原始表达式叠加两个子控件：Frame 161/162 的
`(655,126,28×26)` 与 Frame 606/607 的 `(603,153,40×20)`。这些位置来自父参数
`(107,110)` 加上已记录的相对偏移，不是视觉手工校准；控件的业务语义和 Frame 603/604
是否属于同一状态机仍保持候选。

### Finding 110：全局控件目录已把已知次级窗口控件从未归属池分离（2026-08-10）

全量 `0x00417550` 直接调用目录共 109 条，其中 72 条属于主窗口、15 条属于主 HUD；
另外 20 条已经能够由专用静态证据绑定到 Interface1c 选择界面、Interface1c 主界面候选、
确认框两个控件簇以及 Frame 602 公告包装器。它们现在标记为
`secondary-window-control`，并保留各自的 owner，不再和真正尚未追踪的 2 条调用混在一起。

这次分类只提升“代码归属”证据等级，不等同于确认业务名称或运行时状态；未归属的两条仍需
继续追踪 wrapper 和资源句柄后才能提升为坐标记录。

### Finding 111：主 HUD 的原版键盘语义字符串已从构造调用闭合（2026-08-10）

在主 HUD 初始化附近，`0x00427B24`、`0x00427B58` 和 `0x00427BAA` 还会把原版 GBK
字符串绑定到控件构造调用：`腰带(Ctrl+Z, Z)` 位于 Frame 159 的
`(hud.left+393, hud.top+13)`；`技能书(Ctrl+E, E)` 对应 F100/F101 的
`(hud.left+703, hud.top+16)`；`聊天记录(Ctrl+R, R)` 对应 F102/F103 的
`(hud.left+718, hud.top+32)`。后两项与已经恢复的技能、聊天历史 HUD 按钮坐标完全
重合，说明这些字符串是原版控件语义/提示链的一部分，而不是现代代码推断。

这些记录已写入 `hud-label-evidence.json` 并合并进 `layout.json`。Frame 159 是否在所有
状态下可见、以及文字颜色/字体的最终绘制路径仍保持 pending。

### Finding 112：腰带辅助文字控件已从未归属池提升为主 HUD 文本控件（2026-08-10）

`0x00427B24` 位于主 HUD 初始化连续调用中，直接使用 Frame 159、字符串
`0x0047BC68`（`腰带(Ctrl+Z, Z)`），并以 `[esi+0x0C58]+0x189`、
`[esi+0x0C5C]+0x0D` 计算位置。由于调用上下文、资源帧、原始字符串和坐标表达式均已
闭合，它现在在全局目录中标为 `main-hud-text-control / hud.belt-label`；这不等同于
确认它是始终可见的独立按钮，Frame 159 的最终显示状态仍按 HUD 文本 pending 处理。
### Finding 113：统一证据预览已显示主 HUD 原始语义控件框（2026-08-10）

`/ui` 的固定 800×600 HUD 模式现在从 `layout.json.hud_label_evidence` 读取三个原版
文本/辅助控件记录，并用金色调试框显示其 Frame、文本和绝对化坐标。该显示层只帮助核对
原版构造表达式，不会把字符串渲染成现代客户端的最终字体，也不会改变主 HUD 15 个按钮
的资源和命中矩形。

### Finding 114：剩余未归属控件已定位到独立组件 0x13，但暂不提升为游戏窗口（2026-08-10）

全局目录剩余的 `0x00455AF5` 不再是无上下文的孤立调用：它位于组件初始化
`0x00418CF1` 创建的 `owner+0x362354` 对象中，父构造器为 `0x00455A80`、vtable 为
`0x00476B7C`，实际控件初始化方法为 `0x00455AC0`。该方法在资源参数非空时使用
Frame 2/3，固定构造位置 `(135,400)`，并把资源参数保存到 `this+0x20EC`。

这些是可靠的静态组件/控件事实，但目前还没有证明资源参数属于 GameInter、也没有找到
它的可见绘制入口。因此它继续保留在 `unassigned-control-clusters.json`，并明确标为
`primary-static-component-context`，不把 `(135,400)` 擅自当成主游戏 UI 绝对坐标。

### Finding 115：商店/仓库状态的居中候选公式已进入统一布局数据（2026-08-10）

原版公共工厂 `0x00423E80` 会读取资源尺寸并执行父级矩形/居中算术；结合固定
`800×600` 证据视口，可得到状态 0/3 `(250,148)`、状态 1 `(151,148)`、状态 2
`(298,198)`、状态 4 `(130,147)` 的预览候选原点。它们现在写入
`store-state-graph.json.factory_centering_evidence` 并合并进 `layout.json`。

这些原点只表示“父级为完整 800×600 视口”的候选，不覆盖运行时父容器平移，因此仍不把
它们提升为 runtime-confirmed 绝对坐标。
### Finding 116：原版窗口位置更新由 0–15 号运行时窗口表分派（2026-08-10）

在 `0x0042B430` 发现了统一的位置更新分派：它先检查窗口运行时列表，读取选中节点 ID，
再通过 `0x0042B658` 跳转表把位置参数转给 `0x00423FA0`。已确认的映射包括背包 ID0、
状态 ID1、商店 ID2、交换 ID3、行会 ID4、组队 ID6、组队附属 ID7、聊天 ID8、NPC ID9、
任务 ID11、设置 ID12、坐骑 ID13 和其他窗口 ID14；ID5/10 是空分支，ID15 指向额外组件
`main+0x52E5C`。

这证明可移动窗口的“最终位置”不能只从初始化构造参数读取：原版会在运行时通过窗口表更新
位置。该证据已进入 `window-position-dispatch-evidence.json` 和统一 `layout.json`；由于
`0x00423FA0` 的初始窗口表尚未完全闭合，但其参数 ABI 已经闭合：普通 `flag=0` 分支中，
调用者压入的第一个位置参数是 X，第二个是 Y；函数分别加上 `this+0x40`、`this+0x44`，
再将左上角和保存的宽高写入窗口 RECT。

### Finding 118：位置更新辅助函数的 X/Y ABI 已由 SetRect 数据流闭合（2026-08-10）

`0x00423FA0` 的栈布局在函数序言后为：原始第一个参数位于 `[esp+0x18]`，第二个位于
`[esp+0x1C]`，第三个 flag 位于 `[esp+0x20]`。在 `flag=0` 分支，第一个参数与
`this+0x40` 相加形成 left，第二个参数与 `this+0x44` 相加形成 top，随后调用
`0x004762B0` 写入 RECT；窗口宽高来自 `this+0x10-this+0x08` 与
`this+0x14-this+0x0C`。

因此原版坐标恢复可以沿“注册/位置调用 → 参数 → 0x423FA0 → SetRect”自动化，不能再把
X/Y ABI 标成未知；目前真正剩余的是每个窗口初始注册调用传入的数值及运行时拖动后的更新。

位置分派的已知调用者只有 `0x0042C745`：外层 `0x0042C511` 先把两个输入参数保存到
`edi/ebx`，再按 `push edi; push ebx` 转交给 `0x0042B430`。这条链确认了运行时位置更新
来自统一窗口输入/更新入口；它还没有给出启动时每个窗口的初始常量，因此启动注册链仍需
继续追踪。

### Finding 117：原版窗口可见性是独立的显示/隐藏状态机（2026-08-10）

在 `0x0042ADB0` 发现了与位置更新分开的 0–15 号窗口可见性分派。它按窗口 ID 跳转，
再根据对象内的状态字段选择 `0x0042AC50`（显示）或 `0x0042AC30`（隐藏），并通过
对象 vtable `+0x10` 以参数 `1/0` 通知可见性变化。背包分支还会在首次显示后初始化子项
列表，说明“对象已构造”与“窗口当前显示”不是同一件事。

完整跳转表已核对：ID 0/1/2/3/4/6/7/8/9/11/12/13/14/15 的分支入口分别为
`0x0042ADCF`、`0x0042AE42`、`0x0042AE91`、`0x0042AEE0`、`0x0042B06B`、
`0x0042B0BA`、`0x0042B131`、`0x0042B180`、`0x0042B25E`、`0x0042AF2F`、
`0x0042AF7E`、`0x0042AFCD`、`0x0042B01C`、`0x0042B2AD`；ID5/10 落入默认分支。
这组证据已经写入 `window-visibility-dispatch-evidence.json` 并合并进 `layout.json`。
初始窗口注册顺序与 ID15 的业务身份继续保留为 pending；预览器不应仅凭构造器存在就默认
显示所有窗口。

进一步反汇编确认了两个辅助函数的方向：`0x0042AC30` 是显示路径，它把 `main+0xD24`
作为链表管理器并调用 `0x00449870` 分配/插入窗口 ID 节点；`0x0042AC50` 是隐藏路径，
遍历 `main+0xD28` 链表，摘除匹配 ID、递减数量并通过 `0x004680F8` 释放节点。分派中的
状态测试地址均等于对应窗口对象的 `this+0x30`，随后 vtable `+0x10` 收到 `1/0`。
这使“注册对象”“进入可见窗口链表”和“收到绘制可见通知”三个状态可以严格区分。
可见窗口链表的节点布局也已确认：节点 `+0` 保存窗口 ID，`+4/+8` 为前后指针；管理器
位于 `main+0xD24`，其 head/current/tail/index/count 分别为 `+0xD28/+0xD2C/
+0xD30/+0xD34/+0xD38`。这为后续按原版 z-order/窗口可见状态重建提供了直接数据结构依据。

### Finding 120：可见窗口链表存在独立的运行时遍历/重置入口（2026-08-10）

`0x0042B820` 从 `main+0xD28` 开始按节点 `+0x04` 遍历当前可见窗口，按 ID 分派到 13
个窗口对象，并调用 `0x00423F90` 将每个对象的 `this+0x34` 置零。该入口确认了原版
运行时遍历顺序和窗口 ID 映射，但它调用的是状态字段 setter，不是窗口 vtable 绘制槽；因此
暂时不能把它直接命名为最终跨窗口 z-order。证据已写入 `window-traversal-evidence.json`，
并把“找到真正消费该顺序的绘制入口”列为 pending。

### Finding 119：主 UI 初始化器完整注册了 0–15 窗口表中的可见对象（2026-08-10）

在 `0x00427600` 主 UI 初始化函数中，已逐项记录窗口包装器的构造调用：背包、状态、商店、
交换、行会、组队、组队附属、聊天、NPC、任务、设置、坐骑、其他窗口以及 ID15 的附属组件。
每条记录保留原始 wrapper 地址、GameInter Frame、800×600 中的构造位置和尺寸；这些值来自
原版调用参数，不是预览器手动拖动结果。

这组数据写入 `window-initialization-evidence.json` 并合并到统一 `layout.json`。显示状态仍
单独由 `0x0042ADB0` 分派，因此“已注册”不会被错误解释为“启动时必定可见”；ID15 仍标记为
附属提示/公告组件候选。

### Finding 122：人物状态窗口的局部绘制顺序已闭合（2026-08-10）

原版状态窗口 `0x0044B2D0` 的公共准备函数 `0x0044B560` 首先经窗口 vtable `+0x0C`
调用基类背景绘制 `0x00423D00`；随后执行选中装备/人物覆盖层 `0x0045FD50`，再进入
11 槽装备物品循环 `0x00430A40`，最后调用 `0x0044BC80` 绘制人物属性标签和格式化数值。
这条“背景 → 选中覆盖 → 装备物品 → 属性文本”的局部顺序来自原版调用地址，不代表跨窗口
覆盖顺序；已写入 `status-window-render-evidence.json`。

### Finding 121：坐骑窗口的基类背景与五个子控件绘制顺序已闭合（2026-08-10）

原版 `0x004269C0` 先通过窗口 vtable `+0x0C` 调用共享背景绘制候选 `0x00423D00`，
随后依次调用 `0x00417830` 重定位并绘制 `this+0x54`、`+0x108`、`+0x1BC`、`+0x270`、
`+0x324` 五个控件。相对坐标分别为 `(252,293)`、`(28,244)`、`(74,244)`、`(133,244)`、
`(192,244)`，对应 Frame 161/162、860/861、862/863、864/865、866/867。

这证明至少对坐骑窗口，背景必定先于子控件，且子控件顺序来自原版调用顺序；证据已写入
`horse-window-render-evidence.json` 并接入统一 `layout.json`。控件业务名称和最终命中框仍保持
pending。

### Finding 129：组队窗口局部绘制顺序已由原版入口闭合（2026-08-10）

原版组队窗口成员列表绘制入口为 `0x004243D0`。函数先经窗口 vtable `+0x0C` 绘制 Frame 900
背景并绘制固定头部文本；存在成员链表时，再按索引奇偶分成两列，以 `(window.x+45,
window.y+90)` 为起点、列间距 100、行间距 20 绘制成员文本。随后按固定坐标重新定位关闭/操作
控件，依据 `this+0x3F0` 绘制 `[允许]` 或 `[拒绝]`，最后按原始对象顺序调用五个子控件的
vtable `+0x04` 绘制槽。

因此组队窗口当前可确认的局部顺序是：Frame 900 背景/头部 → 成员列表 → 控件定位 → 权限
状态文字 → 子控件绘制。成员记录字段与头像/图标顺序、权限文字的最终命中框以及运行时打开
状态仍保持 pending；证据已接入组队聚焦预览。

### Finding 123：背包窗口局部绘制顺序已由入口反汇编闭合（2026-08-10）

原版背包绘制入口为 `0x0042EB7F`。函数一开始经窗口 vtable `+0x0C` 调用基类背景，随后
以 GameInter Frame 94 和 `(window.x+0xF8, window.y-0xA5)` 进入背包专用顶部/详情组合。
接下来才处理选中物品分支：先检查选择状态，调用 `0x0042F150`/`0x0042F2A0` 完成 6×6
格命中与索引换算，再通过 `0x00466800` 和 `0x004542F0` 组合源/目标矩形。两条分支随后
汇合到 `0x0042F790`，再绘制主数量/数值文本；最后按固定偏移更新三个子控件，并依据
`this+0x54` 的状态分支绘制第二组文本。

因此当前可用于重建的局部顺序是：背景 → 顶部/详情组合候选 → 选中物品命中/图标组合 →
物品列表路径 → 数量/数值文本 → 三个子控件定位 → 状态相关文本。该顺序是函数内调用顺序，
不等同于所有窗口之间的全局 z-order；业务字段名称、运行时物品句柄和数量/名称的确切语义
仍保持 pending。证据已写入 `inventory-window-render-evidence.json`，并接入 `/ui` 的背包
聚焦预览右上角“原版局部绘制顺序”面板。

### Finding 124：任务窗口局部绘制顺序已由刷新函数闭合（2026-08-10）

原版任务窗口刷新/绘制入口为 `0x00447470`。入口先经窗口 vtable `+0x0C` 绘制 Frame 700
背景，然后定位并调用两个操作控件的 vtable `+0x04` 绘制槽；之后刷新任务链表，使用
`0x0045E0C0` 解码长/短任务记录并按每行 15 像素绘制列表。列表更新结束后，如果存在当前
任务详情，`0x00447E07` 选择 Frame 705 并在 `(window.x+0x41,window.y+0x126)` 绘制详情
区域，最后在 `(window.x+0x50,window.y+0x136+15*line)` 绘制最多三行正文。

因此任务窗口的局部顺序是：Frame 700 背景 → 两个操作控件 → 任务列表行 → Frame 705
详情区域 → 详情正文。详情字段分隔符、换行宽度和运行时业务名称仍保持 pending；证据已写入
`quest-window-render-evidence.json` 并接入背包/状态/坐骑同样的预览器绘制顺序面板。

### Finding 125：商店/仓库候选窗口的局部物品绘制顺序已补齐（2026-08-10）

原版商店候选窗口绘制入口为 `0x0044D590`。它先按照 `this+0x5F8` 状态构造裁剪矩形，随后
遍历 `this+0x64C` 的物品链表；每个可见记录通过 `0x00466130` 选择资源，使用
`0x00466800` 与 `0x004542F0` 完成图标源/目标矩形组合，最多处理五个可见行。选中项路径
随后调用 `0x0045E570` 选择标记并绘制描述区域，再格式化 `this+0x24` 的数值，使用
`0x0047C784`（原始字符串为 `(%d两)`）输出价格/数量文本，最后按状态绘制辅助操作文字。

这条证据把商店/仓库候选的“裁剪 → 物品行 → 选中标记/描述 → 价格/数量 → 辅助文本”局部
顺序接入预览器；它没有把 state 0–4 强行命名为商店或仓库，因为这些业务名称仍需协议和
运行时入口绑定。原始窗口工厂的父级居中算法与最终屏幕原点继续保持 pending。

### Finding 126：聊天窗口局部绘制顺序已由原版绘制入口闭合（2026-08-10）

聊天窗口实际绘制入口为 `0x00414700`；`0x004142C0` 是刷新/滚动数据准备路径。绘制函数
首先经窗口 vtable `+0x0C` 绘制背景，随后写入固定的输入区裁剪矩形
`(window.x+25,window.y+311)-(window.x+524,window.y+326)`。之后遍历聊天链表，以
`(window.x+40,window.y+29)` 为首行位置、每行 14 像素绘制最多 19 行历史文本。历史文本完成
后，函数按固定偏移定位关闭、六个频道和两个滚动控件，依次调用九个子控件的 vtable
`+0x04` 绘制槽，最后根据输入缓冲区生成输入字符矩形。

这使聊天窗口的局部顺序明确为：背景 → 输入区裁剪 → 聊天历史 → 控件定位/绘制 → 输入字符
矩形。频道命令语义和文本字段已保留在原证据中；共享文本渲染器的字体、颜色及第一个控件
的最终命中语义仍保持 pending。证据已接入聊天聚焦预览的“原版局部绘制顺序”面板。

### Finding 128：系统设置窗口局部绘制顺序已由原版重绘入口闭合（2026-08-10）

系统设置窗口重绘入口为 `0x00441380`。函数首先经窗口 vtable `+0x0C` 绘制 Frame 750
背景，然后按原版调用顺序定位关闭控件和八个选项控件，并额外定位两个状态相关控件；所有
位置写入完成后，循环调用前九个子对象的 vtable `+0x04` 绘制槽。

因此当前可确认的局部顺序是：Frame 750 背景 → 九个固定控件定位 → 两个附加状态控件定位
→ 九个子控件绘制。Frame 760/762 的选项语义、状态字段以及 y=96/170 的非 Frame 文本项
仍保持 pending，避免把控件外观误命名为业务标签。证据已接入系统设置聚焦预览。

### Finding 127：NPC 对话窗口的资源绘制顺序已由原版入口闭合（2026-08-10）

NPC 窗口绘制入口为 `0x0043F040`。在主资源路径中，函数首先选择并合成 GameInter Frame 1100，
随后按 `this+0x51C` 循环选择 Frame 1101，最后将索引限制到最后一项并选择 Frame 1102。
这些操作全部经过原版 800×600 图像合成器 `0x00460240`，每项来源记录保持 18 字节步进。
当主资源路径不可用时，函数转入单独的 `0x004542A0` 归一化/透明合成分支；目前没有证据
把该分支命名为正文绘制。

因此当前可确认的局部顺序是：Frame 1100 主对话背景/入口 → Frame 1101 重复对话或菜单项 →
Frame 1102 最后一项 → 透明/归一化 fallback。三枚子控件的动态底部位置来自独立重排路径，
仍不把它们错误地插入到本绘制入口中。证据已接入 NPC 聚焦预览的局部绘制顺序面板。

### Finding 132：确认框与公告框局部绘制顺序已闭合（2026-08-10）

确认框类的绘制入口为 `0x004182A0`：先合成 Frame 950 父面板，再通过共享文本/资源路径
绘制消息缓冲区，最后按对象顺序处理最多三个 YES/NO/确认动作控件。`-1/-1` 构造参数的
Frame 950 居中规则为屏幕矩形 `(220,151)-(580,341)`。

公告/行会提示窗口绘制入口为 `0x0043E3C0`：先调用窗口 vtable `+0x0C`，再根据
`this+0x1D0` 绘制原版 GBK 状态文本，随后绘制第二条文本，最后定位并绘制两个子控件。
两组证据均已写入对应 JSON，且预览器的确认框/公告框模式现在显示“原版局部绘制顺序”面板。
确认框父业务类型、公告 Frame 602 与行会页面的最终绑定仍保持 pending。

### Finding 131：交换窗口局部绘制顺序已由原版交易入口闭合（2026-08-10）

交换窗口绘制入口为 `0x00415B10`。它首先调用窗口 vtable `+0x0C`，然后依据窗口边界写入
左右交易区的中心分割矩形。接着读取选择状态和 `0x007DA1C0/0x007DA1C4`，按状态选择左侧
`x+0x15` 或右侧 `x+0xFD` 的 36 像素物品格，并把物品源/目标矩形交给 `0x00466800` 与
`0x004542F0`。物品路径完成后，函数调用 `0x004169B0` 并输出交易标签、物品文字及数值
状态；最后在 `x+0xD1` 和 `x+0x1B9`、`y-0x73` 绘制两个 Frame 94 侧板。

当前可确认的局部顺序是：背景/左右分区 → 状态物品格与选中合成 → 交易文本/数值 → 两个
Frame 94 侧板。窗口工厂父级居中后的最终屏幕原点及三个控件的业务语义仍保持 pending；证据
已接入交换聚焦预览。

### Finding 133：技能书窗口主重绘顺序已由原版入口闭合（2026-08-10）

技能书/技能类别候选窗口的主重绘入口为 `0x00439500`。原版先通过窗口 vtable `+0x0C`
绘制 GameInter Frame 400 基础面板，随后调用 `0x004397A0` 组合当前页和类别下的技能图标、
名称及列表内容，再调用 `0x0043A440` 从 `Magic.exp` 流解析并绘制说明/列表文字。文字完成后，
函数按固定偏移重定位并绘制三个页签/翻页控件，随后重定位并绘制八个类别按钮（火、冰、电、风、
神圣、黑暗、幻影、剑），最后调用共享文字渲染路径输出当前类别/页码等数值状态。

因此当前可确认的局部顺序是：Frame 400 基础面板 → 技能图标/名称组合 → Magic.exp 技能
文字 → 三个页签/翻页控件 → 八个类别按钮 → 数值/状态标签。技能类别按钮的静态相对位置和
Frame 对已记录在 `skill-window-context.json`；Magic.exp 的加密/编码格式、列表字段语义以及
窗口最终屏幕原点仍保持 pending，不能仅凭静态调用顺序强行命名。

补充核对 `0x00439500` 的尾部调用后，分页状态文字也已闭合：程序读取当前类别的记录计数
`this+0x58+4*byte(this+0x54)`，通过 `imul 0x2AAAAAAB` 得到按六条一页的商，再分别计算
`商*2+1` 与 `商*2+2`，使用格式化入口 `0x0046811C` 和共享文字绘制入口 `0x0045DD70`，
在窗口相对 `(117,299)` 与 `(118,309)` 附近输出两组状态字符串。这里的“起始/结束页”是
根据计算形式得出的语义候选，格式字符串的最终中文含义仍保留证据等级，不把它强行改名为
现代客户端的页码标签。

### Finding 134：窗口默认可见性门与完整 ID 分派已由共享构造器闭合（2026-08-10）

重新核对 `0x00427600` 主 UI 初始化段和所有登记包装器后确认：15 个窗口包装器都进入共享
构造路径 `0x00423B30`，其基类初始化实现 `0x00423CA0` 明确把对象 `+0x30` 可见性门写为
`0`。主初始化段按 ID 0、1、2、3、4、6、7、8、9、11、12、13、14、15 构造/注册对象，
但在这段登记序列中没有调用 `0x0042AC30` 显示辅助函数。

因此原版初始状态可以静态确定为：窗口对象存在，但默认不进入 `main+0xD24` 可见链表；之后
由 `0x0042ADB0` 的 ID 分派调用显示辅助函数，将 ID 插入链表并对对象虚表 `+0x10` 传入 `1`。
此前把“默认 visibility gate 初值”列为 pending 的标记已移除。ID15 是否业务上对应 Frame 602
公告/提示家族仍单独保留为待确认事项。

### Finding 135：ID15 与 Frame 602 公告/行会提示对象身份闭合（2026-08-10）

`0x0042797E` 的主初始化调用 `0x0043E260`，该包装器在 `0x0043E295` 进入共享基类构造，
随后直接创建 `this+0x54` 的 Frame 161/162 控件和 `this+0x108` 的 Frame 606/607 控件。
同一对象的重绘入口从 `0x0043E3C0` 开始：它先调用对象 vtable `+0x0C`，读取同一对象的
`this+0x1D0` 状态字段绘制两组原版行会/公告文字，再重定位并绘制这两个子控件。

这条对象字段、构造器、绘制入口和子控件偏移的连续链条足以确认：窗口 ID15 就是 Frame 602
公告/行会提示窗口，而不是独立的地图 UI 或未知 secondary component。仍未强行推断的是它由
行会页面还是某个独立命令打开，以及 F603/F604 是否属于同一状态机。

### Finding 130：行会窗口状态分派与局部绘制顺序已闭合（2026-08-10）

行会窗口的公共绘制包装器为 `0x00425040`。它先通过窗口 vtable `+0x0C` 绘制 Frame 600
背景并写入窗口裁剪矩形/头部文本，然后根据 `this+0x98` 分派到三个状态路径：
`0x00425280`、`0x00425440`、`0x00425590`。三条路径都使用 `this+0x9C` 作为滚动起点，遍历
对应链表并通过 `0x0045DD70` 绘制可见行；返回公共包装器后，才按固定参数重定位九个控件，
最后按原始对象顺序调用九个子控件的 vtable `+0x04`。

因此当前可确认的顺序是：Frame 600 背景/头部 → 状态页列表 → 九个控件定位 → 九个子控件
绘制。三个状态的业务名称、四个构造阶段寄存器歧义项和特殊标记颜色仍保持 pending；证据
已接入行会聚焦预览。

### Finding 138：人物装备图标的原版资源选择链已定位（2026-08-10）

人物状态绘制命中某个槽位后，`0x0044B6F7` 把该槽位记录的 `+0x04`（即窗口对象中
`this+0x2F8+index*0xC24` 的图形对象）传给通用物品绘制入口 `0x004341F0`。该入口读取
图形对象 `+0x28` 的 WORD，作为参数调用 `0x00466130`，并固定使用选择上下文
`0x005668C4`；随后从 `0x005668FC` 读取所选帧的宽高，再进入原版图形合成路径。

这已经证明装备图标是“每件物品自己的记录 → 原版帧选择器 → 原版合成器”的链路，而不是
现代客户端可以随意替换的统一占位图。当前仍未把选择上下文反查到具体 `WIL/WIX` 文件名，
所以该文件绑定继续标为 pending；相关字段和地址已写入状态窗口证据 JSON。

### Finding 137：ID15 的显示/隐藏分派路径已闭合（2026-08-10）

`0x0042ADB0` 的 ID15 分支 `0x0042B2AD` 先测试 `main+0x52E8C`，然后在两条路径中分别调用
`0x0042AC30`/`0x0042AC50` 修改可见窗口链表，并通过对象虚表 `+0x10` 写入显示值 `1` 或
隐藏值 `0`。显示路径还会调用一个带全局位置参数的外部提示/消息例程；这证明公告窗口有
独立的运行时状态切换入口，但仅凭这一层仍不能把业务触发者冒充成“行会页面”，因此该
业务来源继续保持 pending。相关原始地址和分支已写入 `notice-prompt-window-evidence.json`。

### Finding 136：人物状态窗口的 11 个槽位索引与固定矩形闭合（2026-08-10）

人物状态窗口构造器 `0x0044B130` 通过 `SetRect` IAT `0x004762B0` 为 `this+0x1C0` 起始的 11 个位置记录写入固定矩形；绘制循环 `0x0044B5D9-0x0044B629` 以相同的 11 项、每项 `0xC24` 的物品记录步长和每项 `0x10` 的位置记录步长逐项消费它们。因此下表是原版索引事实，不是现代客户端的业务猜测：

| 索引 | 位置记录 | 相对矩形 | 当前语义边界 |
|---:|---|---|---|
| 0 | `this+0x1C0` | `(86,114)-(146,204)` | 中央角色图/特殊装备绘制目标 |
| 1 | `this+0x1D0` | `(38,70)-(91,154)` | 属性/角色区域候选 |
| 2 | `this+0x1E0` | `(27,264)-(65,302)` | 装备槽候选 |
| 3 | `this+0x1F0` | `(177,70)-(215,108)` | 装备槽候选 |
| 4 | `this+0x200` | `(94,71)-(143,104)` | 中央头像/名称区域候选 |
| 5 | `this+0x210` | `(27,186)-(65,224)` | 装备槽候选 |
| 6 | `this+0x220` | `(175,186)-(213,224)` | 装备槽候选 |
| 7 | `this+0x230` | `(27,227)-(65,265)` | 装备槽候选 |
| 8 | `this+0x240` | `(175,227)-(213,265)` | 装备槽候选 |
| 9 | `this+0x250` | `(64,264)-(102,302)` | 装备槽候选 |
| 10 | `this+0x260` | `(103,264)-(141,302)` | 装备槽候选 |

索引 `0/1/4` 走 `0x00430A40` 的特殊中心绘制分支；其余八个索引使用各自位置记录的左上坐标加窗口偏移和 `+0x0F` 边距。槽位的“武器/头盔/项链”等业务名称，以及实际装备图标资源句柄，仍必须从物品数据/资源选择路径继续闭合，不能仅凭左右位置命名。

### Finding 139：坐骑窗口五个控件的命中矩形与点击分支闭合（2026-08-10）

坐骑窗口的五个子控件不需要手工拖动校准。共享控件构造器 `0x00417550–0x004175B0` 在选择资源帧后，以 `SetRect(this+0x04, x, y, x+selected_frame.width, y+selected_frame.height)` 写入命中矩形。结合 GameInter.wil 的帧头尺寸，可以得到相对于 Frame 850 左上角的精确矩形：`161/162=(252,293,28,26)`、`860/861=(28,244,44,20)`、`862/863=(74,244,60,20)`、`864/865=(133,244,60,20)`、`866/867=(192,244,56,20)`。

点击入口 `0x00426A80–0x00426B45` 按同一对象顺序测试五个控件。首个对象只返回 handled；其余分支根据 `byte [0x007DA060]` 和子控件处理结果，把运行时数据指针 `0x0047B058/0x0047B060/0x0047B068` 交给共享消息入口 `0x004520F0(this=0x008AB828)`。这证明了交互顺序和消息分派，但这些指针位于运行时数据区，中文业务标签尚未静态解出，因此没有冒充成“上马/喂养”等名称。

Frame 850 的资源可见 alpha 包围盒为 `275×323`，窗口构造尺寸为 `296×332`；目前可确认它是窗口底图的完整资源范围，不能据此断言整个最终合成只包含底图，状态相关文字或叠加层仍可能在其后绘制。

### Finding 140：背包物品绘制的资源句柄与记录字段入口进一步闭合（2026-08-10）

背包窗口不是用现代客户端的统一图标表填充。原版主对象在 `0x00452AE6` 开始装载 70 个资源句柄，GameInter 路径字面量为 `0x0047CE0C`；窗口构造调用 `0x00417550` 时把该句柄通过 `EDI` 传给关闭按钮和另外两个状态控件。背包的物品绘制入口 `0x0042F790–0x0042FA68` 从 `this+0x774+4*record_index` 的记录数组读取物品/位置/状态字段，使用 `window.x+0x19+36*column`、`window.y+0x29+36*row` 形成 36×36 网格格位，再以记录中的 WORD 帧选择值调用 `0x00466130(context=0x005668C4)`，最终通过 `0x00466800 → 0x004542F0` 做原版源矩形/目标矩形合成。

这批证据把“资源句柄从哪里来”和“图标绘制从哪条记录开始”从 pending 中移出；尚未强行命名的是 `x+0xF8/y-0xA5` 的 Frame 94 具体业务用途，以及数量/名称文字字段的完整排列和记录步长。

### Finding 141：聊天窗口首七个控件的命中矩形和输入顺序闭合（2026-08-10）

聊天窗口的首个关闭/控制帧 `161/162` 命中矩形为 `(532,350,28,26)`；六个频道按钮使用 GameInter 帧宽高 `36×34`，相对矩形依次为 `(25,332)`、`(65,332)`、`(105,332)`、`(145,332)`、`(185,332)`、`(225,332)`。这些矩形不是按截图估计，而是共享 `0x00417550` 根据所选帧头尺寸写入 `SetRect` 的结果。

聊天输入处理入口 `0x004149A0–0x00414C56` 先测试 `this+0x6C` 的子控件 `vtable+0x10`；若已处理立即返回，所以这条路径确认它是通用关闭/控制入口，不应误命名为聊天频道。只有该控件未处理后，程序才按构造顺序测试六个频道控件并更新对应的命令状态；滚动条控件 `this+0x558/0x60C` 在其后处理，因状态资源来自不同库，其最终命中矩形继续保留待解。

### Finding 142：主 HUD 血条/经验条资源调用顺序闭合（2026-08-10）

主 HUD 动态绘制入口 `0x00429740` 的资源调用顺序已从分支地址闭合：状态动态帧 `0x82–0x85`（`0x00429819`）→ GameInter Frame 62（`0x004299CB`）→ Frame 60（`0x00429BDB`）→ Frame 61（`0x00429C53`）→ Frame 63（`0x00429FD5`）→ 经验百分比文字（`0x0042A065`）。每个填充路径都先计算归一化比例，再经 `0x00466800` 和 `0x004542F0` 合成；经验文字使用原版格式字面量 `0x0047BD4C/0x0047BD5C`。

这确认了原版内部资源调用顺序，但不能把所有帧简单命名成“前景/背景”：Frame 60/61/62/63 在不同状态分支中可能承担不同方向或覆盖层角色，最终可见剪裁方向仍需运行时绘图捕获验证。
