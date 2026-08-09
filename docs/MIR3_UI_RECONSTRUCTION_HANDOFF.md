# Mir3 EI 3.0 原版客户端 UI 还原工程交接文档

更新时间：2026-08-10

## 1. 工程目标

本工程不是单纯制作一个静态截图，也不是只还原底部操作栏。最终目标是：

1. 以 20 年前 EI 3.0 原版 Mir3 客户端为唯一主要事实来源，恢复完整的 800×600 客户端 UI。
2. 通过原版 EXE 反汇编、WIL/WIX 贴图、DAT 数据和运行时/静态调用关系，推导真实窗口坐标、控件坐标、图层顺序、素材帧、按钮状态和显示条件。
3. 在 Zircon 中逐步实现可运行的原版布局。
4. 同时交付一个独立可打开的 HTML 网页模拟器：固定 800×600 逻辑画布，使用真实原版贴图和已推导坐标，模拟整个客户端的视觉布局和基本操作手感。

网页模拟器必须能表现：人物、怪物、地图、底部 HUD、血蓝、经验、罗盘、聊天、技能、背包、装备、人物状态、任务、NPC、商店/仓库、提示框、系统窗口等。鼠标指向怪物时应显示目标头像/目标信息；点击人物或打开人物窗口时应显示人物装备；窗口、按钮、背包格、技能格和地图区域都应可点击或产生明确反馈。

## 2. 重要事实来源

原版客户端：

```text
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/mir3.dat
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/GameInter.wil
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/GameInter.wix
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/Interface1c.wil
/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/Interface1c.wix
```

项目研究资料：

```text
docs/research/ei-ui-layout/
Tools/extract_mir3_ui_layout.py
Tools/enrich_mir3_layout_evidence.py
Tools/verify_mir3_ui_evidence.py
Tools/wilviewer.py
```

不要把现代 C# Zircon 的坐标直接当作原版事实。Zircon 代码只能作为待修改目标、名称线索或功能参考；原版坐标必须标记来源和证据等级。

## 3. 当前已完成成果

当前验证基线：

```text
核心原版文件：6/6
布局记录：29
标准化绘制调用：57
专项控件矩形：22
内容分类：17
JSON 证据文件：63
尚未完全闭合的证据项：41
```

已经建立或完成初步证据的部分包括：

- 800×600 主视口和底部 HUD。
- GameInter.wil 的底部金属底板、血球、蓝球、经验条、罗盘和按钮资源。
- 窗口初始化、窗口显示/隐藏调度、绘制顺序和窗口提升关系。
- 技能窗口的 11 组按钮帧及相对位置。
- NPC 对话文本扫描、换行、颜色/模式和文本区域布局。
- 商店/仓库 0～4 状态图、状态切换和部分控件帧。
- 聊天、马匹、背包、人物状态、任务、NPC、技能等窗口的资源层预览。
- 社交/角色选择/部分 Interface1c 资源族。
- ID15 通知/提示窗口候选资源和位置。
- 确认框调用者，包括支付金币、丢弃金币、连接断开、返回人物选择、行会提示等。
- 机器可读的 `layout.json`、`ui-coverage-matrix.json` 和各窗口 evidence JSON。

## 4. 证据规则

每一项结论必须记录：

- 原始文件和绝对路径。
- EXE 虚拟地址、反汇编地址或资源帧编号。
- 坐标的坐标系：屏幕绝对坐标、窗口相对坐标、父控件相对坐标或素材内部坐标。
- 宽高、锚点、偏移、裁剪方式和缩放方式。
- 证据等级：`primary`（原版二进制/资源直接证明）、`derived`（由多个 primary 推导）、`candidate`（合理候选）、`pending`（尚未确认）。
- 解码编码和不确定性。不能因为字符串看起来合理就伪造确定结论。

坐标恢复优先级：

1. 反汇编中的静态构造参数、SetRect/SetPos/绘制调用。
2. 原版资源尺寸、资源族和窗口基类绘制关系。
3. EXE 中的父窗口偏移、子控件偏移和固定 800×600 锚点。
4. 原版客户端运行时观察或截图。
5. 手动视觉估计只能作为 candidate，不能升级为 primary。

## 5. HTML 模拟器最终规格

建议新建独立目录，例如：

```text
Tools/mir3_client_simulator/
```

要求：

- 纯 HTML/CSS/JavaScript 即可本地打开，最好同时支持 `python3 -m http.server`。
- 逻辑画布始终为 800×600；浏览器窗口变大时只做整数或等比缩放，不能改变逻辑坐标。
- 贴图必须由 WIL/WIX 解码结果生成或直接引用已解出的 PNG/WebP；不要用占位色块替代已有素材。
- 所有控件从统一数据模型读取，不要在 HTML 中散落重复坐标。
- 每个控件至少包含：`id`、`rect`、`frame/resource`、`state`、`zIndex`、`hitTest`、`evidence`。
- 支持窗口打开/关闭、拖动或固定定位、按钮 normal/hover/pressed/disabled、背包格选择、技能选择、目标选择、聊天输入、提示框确认/取消。
- 场景层至少支持地图背景、人物精灵、怪物精灵、NPC 精灵、目标框、人物装备纸娃娃和掉落物。
- 鼠标悬停实体显示名称/头像/目标框；点击实体将其设为当前目标。
- 人物面板显示装备槽和对应原版装备贴图；背包和技能窗口显示可点击格子。
- 提供“证据模式”：显示控件 ID、矩形、素材帧、证据等级和来源。
- 提供窗口导航或测试面板，能逐个打开 HUD、状态、背包、技能、任务、聊天、NPC、商店、仓库、地图、系统、社交、提示等界面。
- 预览中明确区分“已证实”和“候选模拟”，不能把未确认的内容伪装成原版事实。

推荐数据文件：

```text
simulator/data/layout.json
simulator/data/resources.json
simulator/data/entities.json
simulator/data/windows.json
simulator/index.html
simulator/app.js
simulator/style.css
```

## 6. 尚未完成的重点

重点闭合以下 evidence，而不是只继续美化预览：

- 聊天窗口完整绘制和输入/滚动区域。
- 地图、小地图、地图按钮及地图资源的准确对应。
- 状态、背包、任务、NPC 窗口的全部子控件和最终坐标。
- 商店/仓库所有状态的最终屏幕坐标、按钮命中区和状态切换。
- 确认框/通知框的构造器分类、运行时 hover/click 状态。
- 全局窗口的实际 draw order、visibility dispatch 和 position dispatch。
- 角色装备槽、怪物目标框、人物/怪物头像、场景实体资源族。
- 原版资源的完整解码、索引、透明色/调色板/裁剪规则。

## 7. 工作纪律与交付

这是一个需要持续运行十几个小时的大工程。接手智能体应自主运行，不因小的不确定性中断，不向用户反复询问。遇到无法证明的内容，记录为 pending/candidate，继续推进其他可验证部分。

每完成一个实质性发现：

1. 更新对应 JSON/Markdown 文档。
2. 更新 `UI_COMPLETION_AUDIT.md` 或 `ui-coverage-matrix.json`。
3. 运行 `python3 Tools/enrich_mir3_layout_evidence.py`。
4. 运行 `python3 Tools/verify_mir3_ui_evidence.py`。
5. 运行 `git diff --check` 和必要的编译/网页 smoke test。
6. 进行小而清晰的 commit，并推送到当前远程分支。

不要删除或覆盖用户已有的无关改动，特别是工作树中可能存在的未跟踪文件 `\\Config\\ExperienceList.txt`。

最终交付必须包括：

- 完整研究文档和证据 JSON。
- 坐标、资源、绘制顺序和窗口状态的统一数据模型。
- Zircon 侧的可继续实现的布局基础。
- 可本地运行的完整 800×600 HTML 客户端模拟器。
- 运行说明、已完成/候选/待确认清单。
- 最终验证报告、commit hash 和远程推送结果。
