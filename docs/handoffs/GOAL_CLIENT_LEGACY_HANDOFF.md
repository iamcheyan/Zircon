# GOAL: 原版 C# 客户端接手文档（Client/ + LibraryCore 客户端侧 + 周边工程）

> 本文档自包含。你是 omp goal 会话，没有聊天上下文，一切信息以本文为准。

## 任务

为 Zircon **原版 C#（WinForms）客户端**撰写一份超级详细的接手文档，供从未见过本项目的
人（人或 AI）接手开发。**只写文档，不改任何代码。**

- 产出文件：`docs/handoffs/CLIENT_LEGACY_HANDOFF.md`（新建，中文）
- 覆盖范围：`Client/`（121 文件 ~104,344 行，DXManager 渲染的 WinForms 客户端）
  + `LibraryCore/` 的**客户端侧**（Network 包定义/LibraryImage/MirDB 客户端用法）
  + 周边工程概览：`Components/`、`RenderingCore/`、`Launcher/`、`Patcher/`、`PatchManager/`、
    `ImageManager/`、`LibraryEditor/`、`PluginCore/`、`PluginStandalone/`、`BotRunner/`
    （每个一两段说清定位即可）
- 仓库：`/home/tetsuya/development/zircon`，分支 master，推送远程 `origin`（iamcheyan/Zircon）

## 动手前必读（按序）

1. `AGENTS.md`（仓库根）— 注意"不要触碰原版 Client/ 源码"红线与验证深度约定
2. `docs/codebase/_index.md` — zdocs 索引；客户端相关篇目（protocol/packets-c2s、
   packets-s2c 等）引用即可，不要重写
3. `~/development/Mir3-Research/docs/research/ei-ui-layout/README.md` —
   原版 UI 逆向研究（你的文档要引用它作为 UI 证据来源）
4. `~/development/Mir3-Research/Tools/TOOL_INDEX.md` — 工具总目录（wilviewer/uieditor 等
   客户端相关工具）

## 文档必含章节（超级详细 = 每节都要具体到 文件:行号）

1. **这是什么**：原版客户端定位（本项目里它是**行为权威/移植参照**，不是维护主线——
   Godot 客户端才是现役客户端；"不要触碰 Client/ 源码"约定写进"别做什么"）、
   技术栈（WinForms + DirectX？看 csproj 引用与 DXManager 实际用的是什么，如实写）、
   代码规模表（实测 `find | wc -l`）
2. **怎么跑起来**：能否在本机 Linux 构建（预计不能——WinForms/DirectX，
   如实写"仅 Windows 可跑 + 未验证"）、Windows 上的理论启动方式（引用
   README/解决方案结构）、它与服务端的连接配置（读代码找配置加载点）
3. **架构总览**：启动流程（Program.cs → 哪个入口类）、Scenes 体系
   （LoginScene/SelectScene/GameScene 及 Views/）、Controls（DXControl 体系）、
   渲染管线（DXManager、OnPaint 循环、贴图库加载 LibraryFile → .Zl）、
   Envir/CEnvir（客户端网络环境）、UserModels。画 ASCII 模块图 +
   "一次鼠标点击 → 网络包发出"的数据流
4. **关键文件地图**：按目录列表格（文件 | 行数 | 职责一句话），覆盖全部子目录；
   特别标注 Godot 移植时最常对照的文件（GameScene/MapControl/各 Dialog）
5. **资源格式**：.Zl 图库（Inventory/Storeitems/Interface/Mon-*/M-Hum 等哪个库装什么，
   引用 zdocs 与 dbeditor 图标管线研究结论）、.map 地图、Sound 目录结构；
   数据路径解析（客户端从哪里加载 Data/Map，读代码确认相对路径约定）
6. **与 Godot 客户端的对照关系**：旧版功能 → 新版位置的映射原则
   （**判定"是否已移植"必须按功能概念 grep，不能按旧类名搜**——旧 NPCDialog.cs
   4000+ 行内嵌类在新版拆成了多个文件；引用 AGENTS.md 教训与
   docs/UI_INSPECT_WEAPONCRAFT_PORT_2026-08-13.md 的对照表方法）；
   翻译体系（CEnvir.Language 757 键 + 两层不完整问题，引用
   godot-translation-integration 研究结论所在文档）
7. **周边工程**：Components/RenderingCore（DXControl 渲染依赖）、Launcher/Patcher/
   PatchManager（更新链）、ImageManager/LibraryEditor（资源工具）、PluginCore/
   PluginStandalone、BotRunner——各自定位、是否还在用、入口
8. **已知坑**：从 AGENTS.md、docs/、Mir3-Research/docs/ 搜集客户端相关坑
   （UI 定位 SetDefaultLocations 机制、pt→px 字号、GDI vs Godot DrawString Y 语义、
   旧版翻译体系不完整两层问题等）+ 自己读码新发现的
9. **别做什么**：不改 Client/ 源码（除非 NoColourKey 等明确机制）、
   不把它当现役客户端开发主线、坐标/帧索引约定引用前先核对源码
10. **延伸资料**：zdocs 客户端相关篇目、ei-ui-layout 逆向文档、
    wilviewer/uieditor 工具用法指针

## 质量红线

- 所有事实来自实际读码/读文档，引用 `路径:行号`；没验证的明确写"未验证"，**绝不编造**
- 行数/文件数实测，不抄旧数据
- 引用抽查：完稿后随机抽 20 处 `路径:行号` 引用核对，结果写进文末自检节
- 中文撰写，术语保留英文原名

## 完成定义（DoD）

1. `docs/handoffs/CLIENT_LEGACY_HANDOFF.md` 落盘，章节齐全
2. 文末自检节：三问自答 + 20 处引用抽查记录
3. `git add docs/handoffs/CLIENT_LEGACY_HANDOFF.md docs/handoffs/GOAL_CLIENT_LEGACY_HANDOFF.md && git commit`（中文信息）并 `git push origin master`
4. 汇报：文档行数、章节清单、发现的重要坑、未验证项清单

## 禁止

- 改任何 .cs / .csproj / 配置文件（纯文档任务）
- 触碰 `docs/reviews/`（未提交工作区）
- 动 `ServerCore/ ServerLibrary/ Server/`（服务端 goal 的范围）与
  `GodotClient/`（Godot goal 的范围）——**读可以，写禁止**
- 重量级构建尝试（WinForms 在 Linux 构建必失败，不要浪费时间）
