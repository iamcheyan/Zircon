# GOAL: 服务端接手文档（ServerCore + ServerLibrary + Server/ + 共享库服务端侧）

> 本文档自包含。你是 omp goal 会话，没有聊天上下文，一切信息以本文为准。

## 任务

为 Zircon **服务端**撰写一份超级详细的接手文档，供从未见过本项目的人（人或 AI）接手开发。
**只写文档，不改任何代码。**

- 产出文件：`docs/handoffs/SERVER_HANDOFF.md`（新建，中文）
- 覆盖范围：`ServerCore/`（当前服务端入口）+ `ServerLibrary/`（核心逻辑 446 文件 ~7 万行）
  + `Server/`（旧 MirServer 框架入口，102 文件，说明它与新入口的关系与现状即可，不必逐文件）
  + `LibraryCore/` 的**服务端侧**（MirDB/SystemModels/Network 包定义/Stat/Enum）
- 仓库：`/home/tetsuya/development/zircon`，分支 master，推送远程 `origin`（iamcheyan/Zircon）

## 动手前必读（按序，共约 20 分钟）

1. `AGENTS.md`（仓库根）— 工作约定/GM 命令/验证深度约定/红线
2. `docs/codebase/_index.md` — 已有的 23 篇原版代码深读文档索引（战斗/怪物/协议/任务/行会/掉落…）
   **你的文档不要重写这些机制**——机制细节引用 zdocs 篇目即可；你写的是"接手视角"：
   怎么跑起来、代码在哪、怎么改、坑在哪。
3. `docs/handoffs/`（若已有兄弟文档）+ `~/development/Mir3-Research/Tools/TOOL_INDEX.md`
   （工具总目录，服务端相关工具：SystemDbProbe/DBImporter/dbeditor/NpcMover/questdata）

## 文档必含章节（超级详细 = 每节都要具体到 文件:行号）

1. **这是什么**：服务端定位、技术栈（.NET 版本看 csproj `TargetFramework`）、代码规模表
   （复用 _index.md 盘点数据并核对）、三入口关系（ServerCore vs Server/ vs --singleplayer-dev）
2. **怎么跑起来**：构建（`dotnet build`，服务端输出到 `Debug/ServerCore` 的完整命令）、
   启动（`Debug/ServerCore/dotnet ServerCore.dll`，端口 7000）、配置文件位置与关键项、
   测试账号（test@test.com/test123/TestHero，永久 GM）、单机模式
   （SinglePlayerLauncher 自动拉起 + `--singleplayer-dev` 满级注入，见 ServerCore/Program.cs
   与 ServerLibrary/Envir/DevSinglePlayer.cs）、控制台命令（Program.cs 的 ReadLine 循环）
3. **架构总览**：SEnvir 主循环（`ServerLibrary/Envir/SEnvir.cs` ~4600 行：定时器/刷新流程）、
   SConnection 包处理入口（`Envir/SConnection.cs` ~1670 行）、对象层级
   （MapObject → PlayerObject 17,915 行 / MonsterObject / MagicObject / ItemObject）、
   Magics 四职业目录、Monsters 101 类、Commands 体系、Events、WebServer、EmailService。
   画一张 ASCII 模块图 + 一张"一个攻击包从收到到结算"的数据流
4. **关键文件地图**：按目录列表格（文件 | 行数 | 职责一句话），覆盖 Envir/Models/
   Models/Magics/Models/Monsters/DBModels/Commands/Converter；LibraryCore 侧列
   SystemModels 39 文件、DBModels 31 文件、Network 四个 Packet 文件、Stat.cs、Enum.cs
5. **数据层**：System.db（世界静态数据，BinaryFormatter 非 SQLite）与 Users.db（玩家存档）
   的表-模型对应、双库位置（`Debug/ServerCore/Database/`）、**写库纪律**
   （服务端运行中绝不写/双库同步/先备份/round-trip；MirDB Session 必须
   LibraryCore+ServerLibrary 两个程序集都传否则静默 0 表）——写清"外部工具写库"
   的正确路径（dbeditor 缓冲区 → sync.sh → DBImporter）
6. **常见修改配方**（每条 = 改哪个文件 → 关键方法 → 怎么验证）：
   加/改一个技能（MagicInfo + Magics/Xxx.cs）、加怪物（MonsterInfo + Monsters/）
   、加 GM 命令（Commands/Admin/）、改掉落（DropInfo/MonsterObject.Drop）、
   改经验/属性公式、改地图刷新（RespawnInfo）
7. **与其他组件的接口**：协议（LibraryCore/Network 包表，客户端是谁在消费）、
   客户端副本 System.db 同步、Config/ 与 ClientData/ 目录、与 Mir3-Research 工具链
   的 symlink 关系
8. **已知坑**（从 AGENTS.md、docs/ 现有文档、代码注释里搜集 + 自己读码发现的，
   每条注明来源）：MirDB 双程序集、写库纪律、GB18030 文本配置、
   验证深度约定（编译过≠逻辑对，见 AGENTS.md 教训）、BinaryFormatter 兼容性等
9. **别做什么**：不写运行中的库、不删 `~/immich` 类无关内容不涉及、
   upstream 合并逻辑冲突必须先问用户、Server/ 旧入口谨慎动
10. **延伸资料**：zdocs 相关篇目指针、docs/ 下服务端相关审计文档、
    Mir3-Research/Tools/TOOL_INDEX.md

## 质量红线

- 所有事实来自实际读码/读文档，引用格式 `路径:行号`（相对仓库根）；没验证的明确写"未验证"，**绝不编造**
- 行数/文件数用 `find ... | wc -l` 实测，不抄旧数据（盘点数据可能过期）
- 引用抽查：完稿后随机抽 20 处 `路径:行号` 引用，用 `sed -n 'Np'` 核对行号内容真实匹配，
  把抽查结果写进文档末尾的自检节
- 读不懂的机制标注"未完全理解"并给出你读到的最远位置，不要硬编
- 中文撰写，术语保留英文原名（类名/方法名/表名）

## 完成定义（DoD）

1. `docs/handoffs/SERVER_HANDOFF.md` 落盘，章节齐全
2. 文末附自检节：三问自答（只读这一篇能否 (a)跑起来 (b)知道改哪 (c)不踩坑）+ 20 处引用抽查记录
3. `git add docs/handoffs/SERVER_HANDOFF.md docs/handoffs/GOAL_SERVER_HANDOFF.md && git commit`（中文信息）并 `git push origin master`
4. 汇报：文档行数、章节清单、发现的重要坑、未验证项清单

## 禁止

- 改任何 .cs / .csproj / 配置文件（本任务是纯文档）
- 写 System.db / Users.db
- 启动服务端做重量级验证（读码为主；如需确认启动命令，引用 AGENTS.md 即可）
- 触碰 `docs/reviews/`（别人未提交的工作区）
- 动 `Client/` 目录（那是另一个 goal 的范围）
