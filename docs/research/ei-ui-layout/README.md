# Mir3 EI 原版 UI 静态提取进度

> 本目录是“20 年前 EI 3.0 原版 UI”证据库，不是现代 Zircon 布局说明。所有结论按
> `primary-static`、`secondary-cross-reference`、`candidate`、`pending` 分级；原始
> 客户端目录只读，任何无法由 `Mir3.exe`/WIL/WIX 或服务器交叉资料证明的业务名称都
> 必须保留为候选。

## 当前覆盖范围（2026-08-10）

已建立统一 `layout.json` 与固定 `800×600` 证据预览，覆盖主 HUD、HP/MP/经验、人物
状态/装备、背包、技能、任务、聊天、组队、行会、商店/仓库状态机、NPC、系统设置、坐骑、
公告/提示、角色选择候选、MMap/FMMap 小地图资源和地图模式切换。机器码已恢复的重点
内容包括：背包 `6×6 / 36px` 网格、状态窗口 `8 个装备候选槽 + 3 个非装备记录`、组队
两列成员位置、行会最多 18 行、聊天六个原版 GBK 命令字符串、NPC 最多 13 个动态条目、
地图固定 Rect `(672,0)-(800,128)` 以及 `256×256/128×128` 表面切换。

尚未完成的项目仍明确列在 [`UI_COVERAGE_MATRIX.md`](UI_COVERAGE_MATRIX.md) 和各专题
JSON 的 `pending` 字段中，尤其是运行时窗口打开入口、好友是否复用行会/Interface1c、
商店状态的业务命名、地图完整窗口容器及若干动态字段；这些不会被预览器伪装为已确认。

专题证据入口：

- [`UI_COVERAGE_MATRIX.md`](UI_COVERAGE_MATRIX.md)：按 UI 类别的完成度与剩余工作。
- [`RESEARCH_LOG.md`](RESEARCH_LOG.md)：按 Finding 记录反汇编地址、推导和纠错。
- [`layout.json`](layout.json)：统一窗口、按钮、控件、资源、命中框和证据等级。
- [`map-ui-resource-evidence.json`](map-ui-resource-evidence.json)：MMap/FMMap、服务器映射、地图表面和输入路径。
- [`npc-window-render-evidence.json`](npc-window-render-evidence.json)：NPC F1100/F1101/F1102、控件和动态条目。
- [`chat-window-render-evidence.json`](chat-window-render-evidence.json)：聊天窗口几何和原版命令字符串。
- [`skill-window-render-loop-evidence.json`](skill-window-render-loop-evidence.json)：技能列表绘制循环与 Magic.exp。
- [`inventory-window-render-evidence.json`](inventory-window-render-evidence.json)：背包 6×6 槽位证据。
- [`status-window-render-evidence.json`](status-window-render-evidence.json)：人物状态/装备位置记录与绘制链。
- [`store-server-crossref.json`](store-server-crossref.json)：Mud3 仓库、购买、出售 NPC 的第二证据交叉表。

预览器运行后访问 `http://127.0.0.1:8765/ui`。模式选择包含主 HUD、完整地图资源候选、固定
小地图 128×128 候选、技能、
状态、背包、组队、行会、聊天、任务、商店、交换、系统设置、坐骑、NPC 等固定视口模式；
调试框、Frame 编号、图层、地图 Rect、截图差异叠加和 localStorage 状态记忆均属于预览
辅助，不会取代原版静态证据。

地图逐帧对照：选择“完整地图资源候选”后，顶部地图资源选择器可以切换
`FMMap.wil` / `MMap.wil` 并输入 Frame 编号。选择会保存到
`mir3_evidence_ui_state`（浏览器 localStorage），刷新后仍保留，适合按照服务器交叉表
逐张检查。预览器将资源缩放到观察区只是查看手段；不会把大图尺寸误当成原版 128×128
目标 Rect，也不会把 Frame 号自动解释为地图业务名称。

持续研究日志（记录反汇编地址、推理过程、失败尝试和待验证事项）：

```text
docs/research/ei-ui-layout/RESEARCH_LOG.md
```

## 当前结论

`Mir3.exe` 中存在一组静态 UI 初始化代码。当前提取器已定位到 93 次调用固定 helper `0x00449C50` 的记录。该 helper 将三个 `WORD` 参数写入一个对象结构，调用点集中在几个连续的构造函数中。专题研究日志目前已记录到 Finding 94；93 是提取器历史记录数，不是 Finding 编号。

提取脚本：

```text
Tools/extract_mir3_ui_layout.py
```

生成数据：

```text
docs/research/ei-ui-layout/static_rect_initializers.json
```

运行：

```bash
python3 Tools/extract_mir3_ui_layout.py
```

只读完整性审计：

```bash
python3 Tools/verify_mir3_ui_evidence.py
```

审计只检查原版文件存在性、统一布局的 `800×600` 不变量、记录 ID/证据等级和专题 JSON
可解析性；`PENDING` 输出是研究范围的一部分，不会被脚本自动降级或隐藏。

## 证据示例

在 `0x00449C80` 附近可以看到连续初始化：

```text
call VA       object offset   三个 WORD
0x00449C9C    +0x18           0, 4, 200
0x00449CAD    +0x1E           80, 6, 100
0x00449CC1    +0x24           160, 5, 75
0x00449CD5    +0x2A           240, 5, 75
0x00449CE9    +0x30           320, 1, 100
```

注意：x86 调用点的 `push` 顺序与函数看到的参数顺序相反，提取器已经按调用约定还原为 `value1,value2,value3`；`raw_pushes` 仍保留机器码中的原始顺序。

这些数值具有明显的连续分组和固定间隔特征，但目前只能称为“静态初始化候选”，还不能直接等同于屏幕 Rect。下一步必须追踪该对象后续被哪个绘制函数读取，以及三个字段的真实语义。

另一个调用组位于 `0x00458BC6` 附近，第三个字段呈现 `200, 260, 320, 380...` 的连续模式，可能是资源表、动画帧或控件布局表，需要结合调用者和 WIL 资源访问继续确认。

## 下一步

1. 对 `0x00449C50` 的所有调用者建立函数边界。
2. 追踪这些对象字段的读取位置。
3. 找出与 `GameInter.wil`、`Interface1c.wil`、`MIcon.wil`、`Magic.wil` 相关的绘制函数。
4. 将静态结构字段和 WIL Frame 编号关联。
5. 区分：
   - 资源源矩形
   - 屏幕目标坐标
   - 控件命中矩形
   - 动画/状态索引
6. 对每条记录保留 exe 虚拟地址和反汇编证据。

## 最近补充

`notice-prompt-window-evidence.json` 已补充 `0x0043E3C0` 的真实 GBK 文字绘制：Frame 602
状态分支会显示“[行会公告，请自行修改公告内容.]”或“[行会修改 请自行修改行会等级、成员排行信息]”，
主文字相对父窗口基线为 `(23,94)`。这只能证明内容和绘制坐标，不能单独证明窗口业务归属。

## 第二证据源

早期 Mir3 C++ 客户端源码的交叉目录见：

```text
docs/research/ei-ui-layout/secondary-source-catalog.md
```

该目录已经整理了主 HUD、16 个底部按钮、Frame 50/60/61/63/67、人物状态装备槽、技能槽、背包格子和窗口 Frame 候选。它只用于给原版 `Mir3.exe` 反汇编结果建立语义假设；没有被 EI 二进制匹配的记录不得升级为最终坐标。

原版二进制已经确认的底部按钮初始化证据见：

```text
docs/research/ei-ui-layout/primary-button-evidence.md
docs/research/ei-ui-layout/button_constructor_calls.json
```

按钮命中矩形的原版证据见：

```text
docs/research/ei-ui-layout/primary-ptinrect-evidence.md
docs/research/ei-ui-layout/ptinrect_calls.json
```

`0x00417550` 会用控件位置和 WIL 当前 Frame 尺寸调用 `SetRect`，因此预览器应自动生成外接命中矩形；没有必要把手动拖动作为坐标来源。

窗口创建簇见：

```text
docs/research/ei-ui-layout/primary-window-init-evidence.md
docs/research/ei-ui-layout/window_layout.json
```

统一目录为 `layout.json`。当前版本 `0.3-primary-evidence-vtable-enriched` 已合并 15 个底部 HUD 按钮、13 个窗口和 72 个窗口控件构造；窗口记录还包含 vtable/绘制槽候选，技能、背包和人物状态窗口另有专门的机器可读绘制证据，尚未确认的窗口业务名称仍保留“候选”表述。

完整的范围盘点和剩余工作清单见 [`UI_COVERAGE_MATRIX.md`](UI_COVERAGE_MATRIX.md)。

`layout.json.control_constructors` 还收录窗口内部的 72 个控件构造调用。它们暂不伪造屏幕坐标，直到资源句柄和位置参数追踪完成。

原版资源初始化的完整路径表另存为 `resource-path-table.json`。它由
`Tools/extract_mir3_resource_path_table.py` 从 `Mir3.exe` 的静态字符串复制序列恢复，
当前包含 157 条记录（其中 140 条来自批量 WIL 路径表，17 条来自独立加载器参数），覆盖
`GameInter`、`Interface1c`、`Magic`、`Inventory`、`Equip`、`MIcon`、`NPC`、`StoreItem`、
人物/武器/怪物、地图地形以及 `MMap`/`FMMap`/`NPCFace` 等资源。该表用于把“界面业务类别”与原版资源族
建立可追溯连接；`owner+偏移` 仍是静态字段证据，不能直接当作已命名窗口。

`resource-family-catalog.json` 进一步把路径表与原始 WIL/WIX 头部合并，记录实际存在的库、
总 Frame 槽位和非空 Frame 数量，作为技能、装备、NPC、地图等资源交叉匹配的索引；它不
替代窗口构造和绘制调用证据。

窗口资源有效像素边界与构造尺寸的比较见：

```text
Tools/analyze_mir3_window_resources.py
docs/research/ei-ui-layout/window-resource-analysis.json
```

这个比较用于验证资源裁剪关系，不替代绘制调用追踪。

原版资源/Frame 选择调用的初步提取见：

```text
Tools/extract_mir3_resource_select_calls.py
docs/research/ei-ui-layout/resource_select_calls.json
```

该文件是下一阶段按窗口函数追踪绘制顺序的索引，不把所有 `SetIndex` 调用直接当成屏幕绘制。

地图资源与服务器地图名的交叉表见：

```text
Tools/extract_mir3_minimap_server_crossref.py
docs/research/ei-ui-layout/minimap-server-crossref.json
```

它读取原版 Mud3 `Envir/MiniMap.txt`，按 `1001+ -> FMMap.wil(value-1001)`、低于
`1001 -> MMap.wil(value)` 生成映射，并用客户端 `Map/*.map` 与 WIL 解码结果做存在性校验。
这是服务器配置的第二证据源，不能替代 `Mir3.exe` 内部的资源选择和绘制调用证据。

按钮实际绘制链的初步证据见：

```text
Tools/extract_mir3_button_draw_calls.py
docs/research/ei-ui-layout/button-draw-calls.json
```

它记录 `0x004179B0 -> 0x0045F2D0` 的调用关系、Frame 尺寸读取、缩放字段和原始指令。当前仍将 `0x0045F2D0` 标记为绘制/合成候选；`0x004179B0` 中的中间 `SetRect` 不应直接作为最终屏幕坐标。

WIL 加载和资源对象初始化调用见：

```text
Tools/extract_mir3_wil_load_calls.py
docs/research/ei-ui-layout/wil_load_calls.json
```

主 UI 资源句柄的进一步绑定见：

```text
Tools/resolve_mir3_window_resource_handles.py
docs/research/ei-ui-layout/window-resource-handle-bindings.json
```

该证据把 `0x00427600` 保存的 `main_ui_this+0x1c`、13 个窗口包装器、72 个内部控件
构造调用和 `Data/GameInter.wil` 路径初始化串起来。它是一级静态句柄流证据；运行时
对象指针和间接绘制顺序仍需验证。

窗口内部控件构造调用见：

```text
Tools/extract_mir3_window_controls.py
docs/research/ei-ui-layout/window-control-calls.json
```

全局 `0x00417550` 控件构造调用目录见：

```text
Tools/build_mir3_global_control_catalog.py
docs/research/ei-ui-layout/global-control-constructor-catalog.json
```

当前共保留 109 条直接调用：72 条已绑定主窗口、15 条属于主 HUD、22 条仍待归属。
未归属项仍保留原始反汇编邻域和 Frame 候选，不会因为暂时无法命名而从证据集中删除。

每条记录还保留了 `0x00417550` 的参数槽位：资源对象、普通/状态 Frame、`x_arg4`、`y_arg5` 以及其余标志参数。

通用窗口背景绘制候选见：

```text
Tools/extract_mir3_window_base_draw.py
docs/research/ei-ui-layout/window-base-draw-evidence.json
```

`0x00423D00` 的一条分支直接出现 `800`、`600` 目标尺寸，并调用 `0x00460240`；另一分支调用 `0x004542A0/0x004542F0`。这证明窗口背景绘制与窗口构造是不同阶段，不能把构造函数本身当成完整 paint 顺序。

窗口包装函数与共享合成后端的交叉结果见：

```text
Tools/extract_mir3_window_draw_calls.py
docs/research/ei-ui-layout/window-draw-calls.json
```

这些包装函数被标记为构造/控件初始化阶段；它们没有直接调用共享合成函数，这个否定结果也保留在证据中。

窗口 vtable 与派生类绘制槽的证据见：

```text
Tools/extract_mir3_window_vtables.py
docs/research/ei-ui-layout/window-vtable-evidence.json
```

它把构造函数的 vtable 赋值、`.rdata` 中的函数表内容和间接 `call [vtable+0xc]` 调用统一保存；`+0xc` 目前仍是“绘制槽候选”，不是未经验证的源码类名。

主初始化窗口与派生 vtable 的绑定候选见：

```text
Tools/bind_mir3_windows_to_vtables.py
docs/research/ei-ui-layout/window-vtable-bindings.json
```

NPC 对话专用绘制路径见：

```text
Tools/extract_mir3_npc_paint.py
docs/research/ei-ui-layout/npc-paint-evidence.json
```

NPC 路径已确认读取 Frame 1100/1101/1102，并存在独立绘制函数 `0x0043F040`；构造函数
`0x0043ED00` 的三个子控件窗口相对坐标也已恢复，动态条目数量与 18 字节步长已记录。
业务语义、条目字段和文字绘制顺序仍需继续追踪，详见 `npc-window-render-evidence.json`。

寄存器表达式解析结果见：

```text
Tools/resolve_mir3_control_positions.py
docs/research/ei-ui-layout/window-control-position-analysis.json
```

解析器只输出静态表达式和保守的绝对坐标候选；轴向不一致或超出窗口容器的结果会单独标记，不会自动当作最终坐标。

`/ui` 证据页会把 `geometric_status=inside-window` 的控件显示为橙色调试框；这仍是“坐标解析候选”层，资源库句柄未确认的控件不会被伪装成最终绘制层。

```text
Tools/extract_mir3_window_controls.py
docs/research/ei-ui-layout/window-control-calls.json
```

窗口控件 Frame 在 `GameInter.wil` / `Interface1c.wil` 中的资源交叉检查见：

```text
Tools/analyze_mir3_control_resources.py
docs/research/ei-ui-layout/window-control-resource-analysis.json
```

## 证据布局预览

`Tools/wilviewer.py` 新增独立证据预览页：

```text
http://127.0.0.1:8765/ui
```

它从 `layout.json` 动态读取当前已确认/候选记录，固定 800×600 显示主 HUD、按钮和窗口候选，并提供坐标框、Frame 编号、证据等级和本地状态记忆。现在还可在顶部切换两个 `Interface1c.wil` 次级界面候选，以原始 640×480 Frame 50 居中显示并叠加其静态控件；还可导入本地原版截图作为半透明差异层，透明度和开关状态会记忆。差异层只用于视觉比对，不作为原版坐标证据。原来的 `/` 页面保留为 WIL 资源浏览器和旧版 HUD 拆解页；旧页中的手写热区只作历史参考，不作为原版坐标证据。

公告提示模式还会叠加 `0x0043E260` 直接构造的 F161/F606 子控件；主 HUD 的键盘语义字符串
（腰带、技能书、聊天记录）见 `hud-label-evidence.json`，不会被误当作现代 UI 文案。

全局控件目录 `global-control-constructor-catalog.json` 会把已由 Interface1c、公告包装器和
确认框专用证据闭合的 20 条调用标记为 `secondary-window-control`；另有 1 条由主 HUD
字符串/坐标证据提升为 `main-hud-text-control`，现在只有剩余 1 条真正未归属调用保留在
`unassigned-control-clusters.json` 中。

## 证据等级

```text
static-initializer-candidate  从原版机器码静态提取，尚未确认字段语义
static-draw-confirmed         已确认被绘图调用读取
runtime-confirmed             原版运行时捕获到实际参数
verified                      至少两种独立证据一致
```

主布局记录仍以 `primary-static` 为主，窗口内部控件同时保留 `primary-static-redraw-position`、
`resolved-primary-redraw` 等更细证据等级；任何未确认的业务语义和运行时状态仍不能直接
当作最终结论。
