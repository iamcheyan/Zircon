# EI 3.0 原版 UI 覆盖矩阵

本文只统计“20 年前 EI 3.0 客户端”证据，不把现代 Zircon 的布局代码当作原版事实。最近核对：2026-08-10。
状态含义：

- `已恢复`：已有原版机器码/资源的坐标或结构证据。
- `候选`：能绑定原版资源或构造路径，但业务语义、状态或运行时顺序仍未完全确认。
- `待追踪`：目前只有资源族/全局控件线索，尚未恢复完整构造或绘制路径。

| UI 类别 | 原版资源/入口 | 当前证据 | 状态 | 仍需完成 |
|---|---|---|---|---|
| 800×600 主 HUD | `GameInter.wil` F50，`0x00427600` | `layout.json`、主 HUD 15 个按钮 | 已恢复 | 运行时截图与最终 z-order |
| HP/MP/经验 | `GameInter.wil` F60/F61/F63 | 主 HUD 资源、固定 Rect、`0x00429740` 比例链、`0x00466800/0x004542F0` 合成调用 | 候选（比例链已恢复） | 运行时确认全局字段的 HP/MP/EXP 命名、精确纹理裁剪方向和最终 z-order |
| 技能窗口/技能类别 | `Magic.exp`、GameInter F400/F410–459 | `skill-window-context.json`、技能渲染循环 | 已恢复 | 运行时技能列表、文字字段与图标 |
| 人物状态/装备槽 | GameInter F200，11 条连续几何记录，8 个装备候选槽 38×38 | `status-window-render-evidence.json`、`0x0044B6B0/0x0044B720/0x004341F0` | 已恢复 | 装备索引业务命名、属性文字实际调用 |
| 背包 | GameInter F250，6×6、36 px 网格；Interface1c F267/268 角色图候选 | `inventory-window-render-evidence.json`、`layout.json`、`0x0042F150/0x0042F2A0` | 候选（几何已恢复） | 物品图标/数量文本顺序、运行时选中态；第三资源不是普通按钮 |
| 任务 | GameInter F700 | `quest-window-render-evidence.json` | 已恢复 | 任务详情页/分页状态 |
| 商店/购买 | GameInter F1000、F1001–F1003；F1000 五行列表、F1001 紧凑网格、F1002 宽组合、F1003 当前副本空帧 | `store-window-render-evidence.json`、`store-state-graph.json`、`0x0044E9B0` 状态机、Mud3 商店 NPC 交叉表 | 候选（状态图与资源形态已恢复） | 通过客户端状态/协议参数区分 NPC 商店、选中物品和扩展面板 |
| 交换 | GameInter F1050 | `exchange-window-render-evidence.json`、`0x004159D0/0x00415B10` | 候选（左右分区与 6×5 格已恢复） | 确认按钮、协议状态和窗口最终原点 |
| 仓库/存取 | GameInter F1000、F1002/F1003 状态分支候选 | `store-window-render-evidence.json`、`store-state-graph.json`、`0x00423E80` 工厂调用、Mud3 `NPC_Storage` 交叉表 | 候选（服务端仓库入口已确认） | 把 state 0–4 与客户端业务入口绑定，确认仓库屏幕原点和按钮语义 |
| NPC 对话 | GameInter F1100/F1101/F1102，`0x0043ED00/0x0043F040`；Interface1c NPCFace.WIL | `npc-window-render-evidence.json`；`0x00440750–0x00440AA0` 已恢复分隔符、16 项上限、14/21 px 行距和三个动态控件位置；1102 当前副本为空 | 候选（绘制链已恢复） | 动态条目字段、文字调用、按钮业务名 |
| 组队 | GameInter F900，成员两列 100 px、行距 20 px | `social-window-render-evidence.json`、`0x004243D0` | 已恢复 | 成员字段文字/图标顺序与运行时上限 |
| 行会 | GameInter F600，单列最多 18 行、滚动行高由字体度量决定 | `social-window-render-evidence.json`、`0x00425280` | 候选 | 4 个控件寄存器流坐标、标签页语义、特殊行颜色 |
| 聊天 | GameInter F350 | `chat-window-render-evidence.json`；6 个固定频道/命令位置、GBK 字符串、文字起点 `(40,29)`、实际 `14px` 视觉行距，以及通用控件 `control+0x34` 字符串字段绑定已从绘制/构造链恢复 | 已恢复 | 共享控件究竟把频道字符串绘为标题、提示还是命令说明；字体颜色、滚动状态 |
| 好友/社交列表 | 当前 15 个通用窗口构造及主 HUD 控件清单中无独立好友窗口/按钮；行会 F600 与 Interface1c 动态簇仍是候选承载者 | `social-window-render-evidence.json` 的 `friend_entry_audit`、全局控件目录 | 静态范围已排除独立构造，功能入口待追踪 | 从行会页签、动态分配路径或 Interface1c 状态入口确认好友页 |
| 系统设置 | GameInter F750 | `system-window-render-evidence.json` | 已恢复 | 选项标签和配置字段 |
| 坐骑 | GameInter F850、860–867 | `horse-window-render-evidence.json`、`0x004269C0/0x00426A80` | 候选（坐标与命令已恢复） | 状态字段与韩文美术标签到命令的运行时绑定；标签为 `말타기/말내리기/말숨기기/말꺼내기` |
| 小地图/地图 | `MMap.wil`、`FMMap.wil`，`0x0043D4D0/0x0043D780`；服务器 `MiniMap.txt` 映射 | `map-ui-resource-evidence.json`、`minimap-server-crossref.json`；小地图 `(672,0)-(800,128)`；`0x0043DE40` 明确切换 `256×256/128×128` 表面模式；绿色/黄色标记分支已确认 | 候选（资源、固定小地图 Rect、模式切换、颜色层已恢复） | 完整地图专用 UI 容器、地图窗口打开入口、缩放/滚动和切换命令语义；标记对象类型 |
| 角色选择/创建 | Interface1c F50，`0x004026E0/0x00456CB0`；已直接读出 `选择角色/创建账号/修改密码/创建角色/删除角色/开始游戏` | `interface1c-*-context.json`，`/ui` 次级预览 | 候选（按钮文字已由原版像素确认） | 运行时状态转换、Frame 17/57 空资源差异和剩余按钮语义 |
| 公告/提示/确认框 | GameInter F602/F603/F604/F605–607、确认框 F950、`0x00418030`/`0x0043E260` | `notice-prompt-window-evidence.json`、`confirmation-prompt-evidence.json`；公告父窗口 `(107,110)-(691,362)` 与 `[行会公告]/[行会修改]` 原版 GBK 文字已由静态绘制调用闭合；`0x00418030` 直接调用者已关联交易、丢金币、仓库、组队和网络提示文本 | 候选 | 区分独立公告框与行会子状态、状态机和确认框 F950 的运行时原点 |
| 普通/悬停/按下状态 | 各控件 frame pair | `layout.json`、控件资源交叉表 | 候选 | 运行时输入和状态切换验证 |
| 绘制层级 | `0x00423D00`、`0x004179B0`、`0x0043F040` | `draw-order-evidence.json`；已确认窗口基类背景先于本窗口派生绘制/子控件 | 候选 | 重叠窗口运行时调用序列 |

## 当前硬性原则

1. 原始客户端目录只读；所有分析结果写入本目录的 JSON/Markdown。
2. `primary-static` 只表示机器码/资源直接证据，不等于运行时确认。
3. 坐标表达式异常、超出父窗口或寄存器复用不清时，保留原始表达式并降级状态。
4. 预览器的候选层可以帮助检查视觉布局，但不能反过来证明原版坐标。
5. 每完成一个类别，都要同步更新本矩阵、`RESEARCH_LOG.md` 和 `layout.json`。
