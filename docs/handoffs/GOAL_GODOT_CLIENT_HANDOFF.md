# GOAL: Godot 客户端接手文档（GodotClient/ + 无头验证 + Web 移植线衔接）

> 本文档自包含。你是 omp goal 会话，没有聊天上下文，一切信息以本文为准。

## 任务

为 Zircon **Godot 4 客户端**撰写一份超级详细的接手文档，供从未见过本项目的人（人或 AI）
接手开发。**只写文档，不改任何代码。**

- 产出文件：`docs/handoffs/GODOT_CLIENT_HANDOFF.md`（新建，中文）
- 覆盖范围：`GodotClient/`（138 文件 ~54,682 行，现役客户端主线）
  + 无头验证配方 + 单机模式 + UI 体系 + 翻译/本地化 + Web 移植线的衔接说明
- 仓库：`/home/tetsuya/development/zircon`，分支 master，推送远程 `origin`（iamcheyan/Zircon）

## 动手前必读（按序）

1. `AGENTS.md`（仓库根）— 测试账号/构建命令/无头验证配方/端口表/验证深度约定
2. `docs/codebase/_index.md` — zdocs 索引（移植对照时用）
3. `~/development/Mir3-Research/Tools/TOOL_INDEX.md` — 工具总目录
   （uieditor :8820 / webclient :8822 / webport :8823 与 Godot 的关系）
4. Godot UI 相关研究文档（在 Mir3-Research skill references 提到的：
   `docs/` 下的 UI_TEXT_FULL_AUDIT / UI_BUTTON_WIRING_AUDIT /
   SINGLE_PLAYER_MODE / CONFIG_DIALOG_AUDIT / BGM_AUDIT 等，能找到几篇读几篇）

## 文档必含章节（超级详细 = 每节都要具体到 文件:行号）

1. **这是什么**：Godot 4 客户端定位（**现役主线**，从原版 Client/ 移植，
   行为权威=原版源码；"功能只可能比旧版少不可能多"铁律）、
   Godot/C# 版本（看 project.godot 与 csproj）、代码规模实测
2. **怎么跑起来**：构建（`dotnet build GodotClient/ZirconClient.csproj`，仓库根执行，
   增量有缓存坑用 --no-incremental）、编辑器（godot-mono 4.6.3，本机无桌面时下载
   Godot 4.6.3 mono Linux zip 解压用）、命令行启动参数全表
   （--server/--port/--user/--pass/--char/--window/--lang/--ui-export/
   --singleplayer-dev 等，逐个 grep 入口代码确认）、测试账号
   （test@test.com/test123/TestHero）、单机模式
   （7000 无监听时 SinglePlayerLauncher 自动拉起服务端，进程生命周期绑定；
   ⚠️ 会持久化满级数据到 Users.db，还原用 Users.db.empty-backup）
3. **无头验证配方**（重点章节，接手者必用）：Xvfb + openbox + godot-mono +
   xdotool + scrot 全流程（:101 4K 位与 :100 手动测试位的区分、
   ZIRCON_UI_SCALE=2 强制倍率、xdotool 按键/鼠标的窗口几何换算、
   渲染窗口≠WM 壳窗口的 WID 陷阱、截图交付方式）——
   把 AGENTS.md「模型交接注意」展开成可照抄的命令序列
4. **架构总览**：场景结构（LoginScene/SelectScene/GameScene/MapTestScene）、
   DXControl 自绘体系（不走 Godot Anchor/Container，Location 相对父硬坐标 +
   LayoutHud 集中布局 + _uiLayer/UiScale 整层缩放，逻辑画布 1024x768 基准）、
   渲染层（Formats/LibraryCache 图库加载、RenderPrimitives 世界层绘制、
   MirEffect/Projectile 特效 _Draw）、Network/（ServerConnection 事件接线、
   SinglePlayerLauncher）、MirSkin 皮肤/字体体系（pt→px 4/3 缩放、
   DrawString Y=基线 vs GDI 顶部语义）、Translations 三件套 + Lang 门面 +
   db_names.json 显示名本地化（Local() 扩展）。画 ASCII 模块图 +
   "登录→选人→进游戏"全流程时序（哪些类哪些方法接力）
5. **关键文件地图**：按目录列表格（文件 | 行数 | 职责一句话）——Scripts/UI/Controls/
   Scenes/Formats/Network/translations 全覆盖；GameScene.cs 是巨文件要给内部分区导读
   （哪个区段管什么）
6. **UI 体系深潜**（Godot 移植最大工作量区）：DXControl/DXWindow/DXButton/DXLabel/
   DXVScrollBar 继承树与自绘机制、WindowManager、MainPanel 九键模式 B 接线
   （GameScene 统一绑）、UiOverlay.cs F12 热重载（uieditor 联动）、
   ConfigDialog 分区/下拉 Reparent 机制、UiScaler 公式
   clamp(min(h/768,w/1024),1,2) 与三条铁律（逻辑画布基准/全窗口挂缩放层/
   Xvfb 无 WM 视口陷阱）
7. **常见修改配方**（每条 = 改哪个文件 → 关键方法 → 怎么验证）：
   改 UI 文本（Lang 键 vs 硬编码中文的判定：系统 UI 走 Lang 三语，中文服内容硬编码）、
   加/改窗口、改按键绑定（KeyBindManager）、改贴图（哪个 .Zl 库哪帧）、
   改技能特效（MagicEffectTable）、加语言（translations 三件套 + ConfigSelect）、
   音效/BGM（SoundCatalog + BusFor + MapInfo.Music）
8. **Web 移植线衔接**：webport/webclient 是什么、与 GodotClient 的权威关系
   （以 GodotClient 源码行为 + ui_tree.json + 真实协议为唯一权威，零差异还原；
   WASM 不可行的结论与静态 Web 路线，引用 docs/WEB_PORT_SPIKE_REPORT.md）
9. **已知坑**（搜集 + 新发现，注明来源）：DataPath 大小写静默失败、
   改 C# 不重编译跑旧程序集、注释吞代码、登录/选人场景缩放层遗漏、
   DXLabel baseline 补偿、Vue/异步渲染类问题不适用但 uieditor 相关坑要写、
   xdotool type 打不进 DXTextInput（走 DB 直建账号）
10. **别做什么**：不碰原版 Client/（参照可以）、行为对齐争议以原版为准、
    upstream 合并逻辑冲突先问用户、不要在服务端运行时写库
11. **延伸资料**：zdocs 篇目、Mir3-Research 工具链（uieditor/webport/wsgateway）、
    docs/ 下 Godot 相关审计文档清单

## 质量红线

- 所有事实来自实际读码/读文档，引用 `路径:行号`；没验证的明确写"未验证"，**绝不编造**
- 行数/文件数实测
- 引用抽查：完稿后随机抽 20 处 `路径:行号` 引用核对，结果写进文末自检节
- 中文撰写，术语保留英文原名

## 完成定义（DoD）

1. `docs/handoffs/GODOT_CLIENT_HANDOFF.md` 落盘，章节齐全
2. 文末自检节：三问自答 + 20 处引用抽查记录
3. `git add docs/handoffs/GODOT_CLIENT_HANDOFF.md docs/handoffs/GOAL_GODOT_CLIENT_HANDOFF.md && git commit`（中文信息）并 `git push origin master`
4. 汇报：文档行数、章节清单、发现的重要坑、未验证项清单

## 禁止

- 改任何 .cs / .csproj / project.godot / 配置文件（纯文档任务）
- 写 System.db / Users.db
- 启动重量级验证（引用 AGENTS.md 配方即可；如要实测无头启动，限一次、
  用完杀干净进程）
- 触碰 `docs/reviews/`（未提交工作区）
- 动 `ServerCore/ ServerLibrary/ Server/ Client/`（其他 goal 的范围）——
  **读可以，写禁止**
