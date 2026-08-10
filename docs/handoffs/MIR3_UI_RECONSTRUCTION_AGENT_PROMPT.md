# 可直接交给下一个智能体的执行 Prompt

你现在接手 `/home/tetsuya/development/Zircon` 中的 Mir3 EI 3.0 原版客户端 UI 还原工程。请把下面内容视为一个需要持续运行十几个小时的大型自主 Goal。不要只做分析报告，也不要停在局部 HUD；必须持续调查、实现、验证、写文档、提交并推送，直到可交付版本形成。

## 总目标

以 20 年前 EI 3.0 原版客户端为事实来源，完整恢复 800×600 的客户端 UI 和基本操作手感，并在项目中交付一个可以本地运行的 HTML 客户端模拟器。模拟器不是静态截图：需要能显示地图、人物、怪物、NPC、目标头像/信息、人物装备、底部操作栏、血蓝经验、罗盘、聊天、技能、背包、状态、任务、商店、仓库、系统、社交和各类提示窗口；控件可以点击，窗口可以打开关闭，鼠标悬停和选中状态有反馈。

## 必须先阅读

先完整阅读：

```text
docs/handoffs/MIR3_UI_RECONSTRUCTION_HANDOFF.md
docs/research/ei-ui-layout/README.md
docs/research/ei-ui-layout/UI_COMPLETION_AUDIT.md
docs/research/ei-ui-layout/UI_COVERAGE_MATRIX.md
docs/research/ei-ui-layout/layout.json
docs/research/ei-ui-layout/ui-coverage-matrix.json
```

再检查：

```text
Tools/reverse-engineering/extract_mir3_ui_layout.py
Tools/reverse-engineering/enrich_mir3_layout_evidence.py
Tools/reverse-engineering/verify_mir3_ui_evidence.py
Tools/web/wilviewer.py
```

原版资源位于：

```text
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/
```

主要研究对象是 `Mir3.exe`、`mir3.dat`、`Data/GameInter.wil`、`Data/GameInter.wix`、`Data/Interface1c.wil`、`Data/Interface1c.wix`。不要直接把现代 Zircon 的坐标当作原版坐标。

## 执行要求

### A. 继续原版静态证据调查

- 用反汇编、字符串交叉引用、资源帧、构造器调用、SetRect/SetPos、父窗口偏移和绘制调用继续闭合 pending 项。
- 优先完成地图、聊天、状态、背包、任务、NPC、商店/仓库、确认框、目标框、人物装备和实体精灵。
- 对每个坐标记录屏幕坐标/窗口相对坐标/父控件相对坐标，不要混用。
- 每个结论写明 VA、资源帧、文件、解码编码、证据等级和推导过程。
- 无法证明的内容写成 `candidate` 或 `pending`，不要猜测后标为确定。

### B. 建立统一客户端数据模型

扩展现有 `layout.json` 体系，形成统一的：

- `windows`
- `controls`
- `draw_calls`
- `resources`
- `entities`
- `equipment_slots`
- `skills`
- `items`
- `maps`
- `state_transitions`
- `hit_rects`

每条记录至少要有稳定 ID、矩形、资源/帧、状态、层级、命中测试区域、来源和证据等级。HTML 和 Zircon 尽量消费同一份数据，避免两套坐标漂移。

### C. 实现完整 HTML 模拟器

请在项目中创建清晰的模拟器目录，例如 `Tools/mir3_client_simulator/` 或 `web/mir3-client/`，并真正实现：

- 固定 800×600 逻辑坐标，外部窗口只等比缩放。
- 使用真实解码贴图，不使用能替换真实素材的占位方块。
- 地图场景层：地图、人物、怪物、NPC、掉落物、可点击实体。
- 鼠标悬停实体显示名称/头像/目标框；点击设置当前目标。
- 人物面板：角色头像、属性、装备槽、装备贴图。
- HUD：血球、蓝球、经验条、聊天区、罗盘/操作按钮、目标信息、小地图。
- 窗口：状态、背包、技能、任务、聊天、NPC、商店、仓库、系统、社交、提示/确认。
- normal/hover/pressed/disabled/selected 状态和可视化反馈。
- 背包格、技能格、装备槽、窗口按钮、地图实体都能点击。
- 证据调试模式：显示控件 ID、Rect、Frame、来源和证据等级。
- 测试导航：能从一个面板打开所有已实现窗口，并能重置场景状态。
- 本地启动说明：至少支持 `python3 -m http.server`，必要时提供脚本。

先实现完整结构和真实资源，再逐项提高交互精度；不要只做一个漂亮但没有真实数据的 demo。

### D. 持续文档化

每获得一个重要结果就更新文档和 JSON，至少维护：

- 研究日志：发现编号、时间、证据、结论、剩余疑问。
- 资源目录：WIL/WIX 文件、帧号、尺寸、用途、透明规则。
- 窗口目录：窗口 ID、坐标、尺寸、初始化函数、绘制函数、显示条件。
- 控件目录：按钮/格子/头像/装备槽的 Rect、资源帧、交互状态。
- 覆盖矩阵：已证实、已实现、候选、pending。
- HTML 模拟器 README 和操作说明。

不要把过程知识只留在聊天里。

### E. 验证、提交和推送

每个阶段完成后执行：

```bash
    python3 Tools/reverse-engineering/enrich_mir3_layout_evidence.py
    python3 Tools/reverse-engineering/verify_mir3_ui_evidence.py
python3 -m py_compile Tools/*.py
git diff --check
```

网页至少做本地启动、HTTP 请求、浏览器可加载和主要交互 smoke test。每一组完整改动都要：

```bash
git add <明确的文件>
git commit -m "<清晰描述本阶段成果>"
git push fork master
```

保留用户已有无关文件，不执行破坏性 reset/checkout，不删除 `\\Config\\ExperienceList.txt`。

## 不能接受的结果

- 只写概念性报告，不产生真实坐标和资源数据。
- 只还原底部操作栏。
- 只做一个静态截图或几个占位色块。
- 使用现代 Zircon 坐标冒充原版事实。
- 把 candidate/pending 内容写成确定事实。
- 只修改代码不写研究文档。
- 没有验证、commit 或 push。

## 最终验收标准

最终必须给出：

1. 原版证据整理完成度和仍待确认事项。
2. 统一布局/资源/控件/实体数据文件。
3. Zircon 侧还原基础。
4. 可运行的 800×600 HTML 客户端模拟器。
5. 模拟器中地图、人物、怪物、NPC、目标、人物装备、HUD、窗口和基本点击交互的演示路径。
6. 本地运行命令和截图/测试结果。
7. 全部文档、commit hash、push 结果和明确的已完成/候选/pending 清单。

现在开始执行。除非遇到真正无法通过本地资料解决的权限或外部依赖问题，否则不要向用户提问或暂停；自行做保守决定并记录依据，持续运行到本轮交付完成。
