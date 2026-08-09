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
`owner+0x148`，`Data/FMMap.wil` 被装载到 `owner+0x2D0`。资源头交叉确认 MMap 为
255 槽/154 非空，FMMap 为 31 槽/29 非空。

在 `0x0043D780` 看到 `map_id >= 1000` 的分支：选择 MMap 资源时使用精确的
`frame = map_id - 1000` 表达式，并把帧头源矩形送入 `owner+0x2E0` 目标矩形。地图
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

### Finding 52：恢复公告板与 YES/NO 提示资源簇（2026-08-09）

`GameInter.wil` Frame 602 的原始图像检查确认其为公告/公告板样式的大窗口资源，头部尺寸
为 `1024×256`，可见内容候选约 `584×252`。原版 `0x0043E260` 簇构造了 Frame
`161/162` 和 `606/607` 两个控件对；构造入口由 `0x0042797E` 调用。附近 Frame 603
视觉上是中文 YES/NO 确认提示，Frame 604 是带勾选状态的窄消息/输入面板，605–607 是
小型状态资源。由于这些帧是否由同一运行时状态机组合仍未确认，全部记录为
`primary-static-candidate`，原始参数表达式和候选坐标均保留在
`notice-prompt-window-evidence.json`。

### Finding 51：预览器增加绘制层级可视图例（2026-08-09）

`/ui` 增加“显示绘制层级”开关，读取 `layout.json.draw_order_evidence.layers`，在固定
视口内显示层序号、层名称和证据等级。图例与差异截图叠加层分离，便于同时检查“资源边界
偏差”和“层级顺序候选”；开关状态写入同一份本地预览状态。
