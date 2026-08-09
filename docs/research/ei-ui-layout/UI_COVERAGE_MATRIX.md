# EI 3.0 原版 UI 覆盖矩阵

本文只统计“20 年前 EI 3.0 客户端”证据，不把现代 Zircon 的布局代码当作原版事实。
状态含义：

- `已恢复`：已有原版机器码/资源的坐标或结构证据。
- `候选`：能绑定原版资源或构造路径，但业务语义、状态或运行时顺序仍未完全确认。
- `待追踪`：目前只有资源族/全局控件线索，尚未恢复完整构造或绘制路径。

| UI 类别 | 原版资源/入口 | 当前证据 | 状态 | 仍需完成 |
|---|---|---|---|---|
| 800×600 主 HUD | `GameInter.wil` F50，`0x00427600` | `layout.json`、主 HUD 15 个按钮 | 已恢复 | 运行时截图与最终 z-order |
| HP/MP/经验 | `GameInter.wil` F60/F61/F63 | 主 HUD 资源和坐标分析 | 候选 | 追踪填充比例和绘制调用参数 |
| 技能窗口/技能类别 | `Magic.exp`、GameInter F400/F410–459 | `skill-window-context.json`、技能渲染循环 | 已恢复 | 运行时技能列表、文字字段与图标 |
| 人物状态/装备槽 | GameInter F200，装备槽 38×38 | `status-window-render-evidence.json` | 已恢复 | 装备图层、属性文字实际调用 |
| 背包 | GameInter F250，36 px 网格 | `inventory-window-render-evidence.json` | 已恢复 | 行列数量、物品图标/数量文本顺序 |
| 任务 | GameInter F700 | `quest-window-render-evidence.json` | 已恢复 | 任务详情页/分页状态 |
| 商店/购买 | GameInter F1000 | `store-window-render-evidence.json` | 候选 | 父窗口绑定和异常宽度坐标的真实解释 |
| 交换 | GameInter F1050 | `system-window-render-evidence.json` | 候选 | 交换双方格子、确认按钮和状态机 |
| 仓库/存取 | 现有全局控件与窗口候选 | `window-control-calls.json`、资源族目录 | 待追踪 | 找到独立构造/绘制入口并绑定资源 |
| NPC 对话 | GameInter F1100/F1101/F1102，`0x0043ED00/0x0043F040` | `npc-window-render-evidence.json` | 已恢复 | 动态条目字段、文字调用、按钮业务名 |
| 组队 | GameInter F900 | `social-window-render-evidence.json` | 已恢复 | 成员文本/图标和操作状态 |
| 行会 | GameInter F600 | `social-window-render-evidence.json` | 候选 | 4 个寄存器流坐标、成员列表绘制 |
| 聊天 | GameInter F350 | `chat-window-render-evidence.json` | 已恢复 | 频道名称、字体颜色、滚动状态 |
| 好友/社交列表 | `Interface1c.wil` 及未归属控件调用 | 全局控件目录 | 待追踪 | 通过字符串/xref/状态入口确认窗口 |
| 系统设置 | GameInter F750 | `system-window-render-evidence.json` | 已恢复 | 选项标签和配置字段 |
| 坐骑 | GameInter F850 | `system-window-render-evidence.json` | 候选 | 坐骑数据字段和按钮语义 |
| 小地图/地图 | `MMap.wil`、`FMMap.wil`，`0x0043D4D0/0x0043D780` | `map-ui-resource-evidence.json` | 候选 | 小地图控件位置、地图窗口绘制与切换 |
| 角色选择/创建 | Interface1c F50，`0x004026E0/0x00456CB0` | `interface1c-*-context.json`，`/ui` 次级预览 | 候选 | 运行时状态转换和准确中文标签 |
| 提示框/确认框 | GameInter/Interface1c 未归属控件 | 全局控件构造目录 | 待追踪 | 找到消息状态机和 Frame 资源 |
| 普通/悬停/按下状态 | 各控件 frame pair | `layout.json`、控件资源交叉表 | 候选 | 运行时输入和状态切换验证 |
| 绘制层级 | `0x00423D00`、`0x004179B0`、`0x0043F040` | `draw-order-evidence.json` | 候选 | 重叠窗口运行时调用序列 |

## 当前硬性原则

1. 原始客户端目录只读；所有分析结果写入本目录的 JSON/Markdown。
2. `primary-static` 只表示机器码/资源直接证据，不等于运行时确认。
3. 坐标表达式异常、超出父窗口或寄存器复用不清时，保留原始表达式并降级状态。
4. 预览器的候选层可以帮助检查视觉布局，但不能反过来证明原版坐标。
5. 每完成一个类别，都要同步更新本矩阵、`RESEARCH_LOG.md` 和 `layout.json`。
