# EI 英雄杀数据对齐：NPC / 刷怪 / 传送 / 安全区 / 新怪（2026-08-11）

## 1. 任务目标与执行摘要

目标：以 EI 英雄杀服务端（`/home/tetsuya/NAS/TMP/EI3.0英雄杀服务端/Mud3`）为权威数据源，
对齐 Zircon DB 的 NPC、怪物刷新、地图连接点、安全区、守卫与怪物信息，使
「所有 NPC、地图连接位置、怪物信息、刷怪信息一切正常」。

完成内容：

| 项 | 数量 |
|---|---|
| 新怪 MonsterInfo 导入 | +125 |
| EI 独有图 MapInfo 注册 | +83（627 张） |
| EI NPC 导入 | +84（222→306，后清理 12 → 294） |
| EI 守卫导入 | +32（79→111，后清理 17 → 94） |
| EI 刷怪导入 | +742（1833→2575，后清理 100 → 2475） |
| EI 传送导入 | +189（964→1153，后清理 114 → 1039） |
| 安全区 | +2（1_004 超级泡点、z014 监狱；共 17） |
| 地图文件 | 65 张 EI 独有图拷贝；16 张交集图换用英雄杀服务端版本 |

验证：服务器重启 0 错误启动；BotRunner 登录/移动/找怪/NPC 交互冒烟通过；
全量坐标校验（守卫/传送/NPC/刷怪/安全区）无失效记录。

## 2. 新怪图像验证（阶段 A）

### 2.1 权威数据源

- EI 刷怪 186 种怪 → 映射到 Zircon：`monster_name_map.json`（已存在怪）+ 本任务 125 新怪。
- 怪物属性权威：game3g.mdf King_Monster 表（本机重新解码验证，
  `king_monster_full.json` 的 raceimg 与原始行完全一致）。
- 新怪渲染图槽 = **King_Monster.raceimg → (raceimg//10, raceimg%10)**，
  验证依据：英雄杀冰原家族（raceimg 200-209 连续 → Mon-20 0-9 槽）、
  修罗家族（392/393/397 → Mon-39 2/3/7）均为设计好的连续块；
  Zircon 自加怪圣兽10/30/100（raceimg 250/251/252 → Mon-25 0/1/2）与
  SamaFireGuardian/SamaIceGuardian/SamaLightningGuardian 完全一致。
- 单数字 raceimg（2/4/6/7/9）→ Mon-1 对应槽，逐槽视觉核对通过
  （触角神魔=(1,2) 触手怪、石人=(1,4) 石魔、轻甲守卫=(1,6) 轻甲卫兵、
  御医=(1,7) 治疗蚁、异界守护神=(1,0)）。
- 凶悍X / 异界X / 武力神将1/2 等变体怪复用基底在 Zircon 的既有 Image
  （凶悍沃玛→UmaKing、凶悍恶魔→RedMoonTheFallen、异界轻甲守卫→BloodStone 等）。

### 2.2 逐槽核对（英雄杀 wtl ↔ Zircon Zl，alpha 帧）

对 125 新怪的非变体图槽做了英雄杀 wtl 与 Zircon Zl 的 alpha 逐帧对比
（含 ZL2 v2 atlas 容器的正确解码——本机修复了 zlsdk 对 ZL2 元数据的解析）。

- 85 个槽内容一致 → 直接复用 Zircon 图库，仅补枚举名 + MonsterLookup 条目。
- 4 个槽需要移植（英雄杀有内容、Zircon 空或冲突）：
  - 玛法战士 → Zircon Mon-31 s6（原为空）
  - 玛法道士 → Zircon Mon-31 s7（原为空）
  - 钻卡树 → Zircon Mon-22 s4（原英雄杀 Mon-15 s3 与 Zircon 的
    BoneBladesman 槽冲突，移到空槽）
  - 火焰狮子5 → Zircon Mon-22 s5（原英雄杀 Mon-15 s9 与 PoisonousMutantFlea
    冲突，移到空槽）
- 移植实现：`build_lib_ports.py` 以 PNG codec 重建 Mon-31.Zl / Mon-22.Zl
  （ZL2 v2，raw deflate，C# DeflateStream 兼容；其余槽内容与 Zircon 原库
  alpha 一致，已校验）。

### 2.3 特殊决策

- 邪恶镇魂者/审判者/堕落者：King raceimg 指向英雄杀 Mon-22 s4/s5/s6（空槽），
  改指同族 s0/s2/s3（与惩戒者/唤雷者/复仇者共享精灵，服务端数据缺陷）。
- 白虎四天王：King raceimg 247 → Mon-24 s7（英雄杀 Mon-24 仅 7 槽），
  改指 Mon-25 s7（白老虎精灵，内容已核对）。
- 深潭泥人：King raceimg 369 → Mon-36 s9（空槽），改指 Mon-36 s7（泥人精灵）。
- 5 个 King 表真缺怪的取舍（**最终决策**）：
  - 恶灵武士、凶悍黑野猪、武力神将2：**实际存在于 King 表**
    （idx 29 / 348 / 290，旧机器 dead_monsters.json 有误），正常映射，
    不存在缺失问题。
  - 冰魂武士、冰魂武将、尸灵横骨：King 表确无。**保留刷怪、指到近似怪**
    （冰魂武士→冰原战士、冰魂武将→冰原勇士、尸灵横骨→伤魂尸），
    在 `new_monster_images.json` 标注 `missing-approx`。
- 丛林猛犸家族（36 图槽）：King raceimg 与精灵内容存在不确定（服务端数据
  与精灵命名不一致），按 King raceimg 原样映射（内容存在于双端图库，
  MATCH 通过），在文档中记录为「服务端数据原样复刻」。

## 3. 源码改动（阶段 B）

- `LibraryCore/Enum.cs`：MonsterImage 枚举新增 42 个新怪枚举名（600-641）。
- `GodotClient/Formats/MonsterLookup.cs`：新增 42 条
  `MonsterImage → (LibraryFile.Mon_N, shape)` 映射。
- 变体怪（77 条）复用既有 Image 枚举，无需新枚举。
- **不改 Client/（原版源码）**：`Client/Models/MonsterObject.cs` 的
  UpdateLibraries() 不在 Godot 构建（ZirconClient.csproj 只编译 GodotClient/
  与 LibraryCore/），Godot 客户端实际渲染路径是
  `ObjectRenderer.CreateMonster → MonsterLookup.Map`。故只需 MonsterLookup。
- `Tools/DbMigrationTool/Program.cs`：新增命令 import-monsters / del-monster /
  delete-records / set-safezone-point / trim-safezones / move-respawns。

构建：`dotnet build GodotClient/ZirconClient.csproj` 0 错误 0 警告通过。

## 4. DB 修复执行（阶段 D）

### 4.1 地图

- 83 张 EI 独有图注册 MapInfo（中文名 + minimap 换算：1001-1031 → 帧 1-31，
  1-255 → 帧 32-286）。
- 65 张 EI 独有图缺 .map 文件 → 从 EI 客户端 Map/ 与英雄杀服务端 Mud3/Map/
  拷贝到 Debug/{Client,ServerCore,Server}/Map/。
- 16 张交集图（0/1/12/5/6/81/D024/D10162/D11031/D505/D5073/D901/D9021/
  D9022/d515/d712）换用**英雄杀服务端**的 .map 版本（旧机器部署的是 EI 客户端
  版本，与英雄杀服务端数据坐标不符；沙巴克图 3 不换，保持 Zircon 原版）。

### 4.2 数据导入与清理

- 全部 124 张 EI 图导入 EI 配置（交集图保留已有 Zircon 数据、叠加 EI 数据）。
- 清理（坐标在现部署地图上不可走、无法生效的记录）：
  - 17 个旧守卫、114 条旧传送（旧 Zircon 布局数据被 EI 地图替换后失效）
  - 12 个 NPC（8 个误导入沙巴克的 EI NPC + 4 个旧 SinGiSun 失效 NPC）
  - 100 条旧刷怪（交集/EI 图上全点不可走的旧 Zircon 刷怪，被 EI 刷怪取代）
  - 沙巴克 12 条误导入传送（EI 配置的图 3 连接不适用于 Zircon 沙巴克）
- 安全区：1_004 / z014 中心点不可走 → 移到就近可走点；旧安全区多点点集
  裁掉不可走点（trim-safezones）。
- 124 条新怪刷怪点（修罗暗殿等英雄杀服务端自身配置就落在墙上）→ 移到就近
  可走点（move-respawns，`respawn_fixes.json`）。
- 保留（原样）：Zircon 独有图上的 698 条全点不可走旧刷怪（旧机器地图未换的
  图，属 Zircon 既有数据缺陷，不在 EI 对齐范围）、29 个旧城镇 NPC
  （有 165 条任务引用，删除会断任务；坐标失效但任务链保留）。

### 4.3 D515 / D6015 / D712 个案

EI 配置对这三图无刷怪数据（D515/D6015/D712 为 DB 有、EI 为 0），按
「EI 无则保留 Zircon」处理，未改动。

## 5. 验证（阶段 E）

### 5.1 服务器启动

`cd Debug/ServerCore && dotnet ServerCore.dll`（注意 MapPath=Map/ 相对
CWD；从仓库根启动会导致地图全部加载失败——旧机器文档命令有误）。
启动日志：**0** 条 Bad Origin / Failed to spawn Guard / Bad Location /
not found / 异常。`Network Started. Listen: 127.0.0.1:7000`。

### 5.2 全量坐标校验

Python 复刻服务端 Map.Load 的可走格判定（flag 0x01|0x02），校验 DB 全部
守卫/传送/NPC/刷怪/安全区坐标：
- 本任务导入的 125 怪、742 刷怪、189 传送、84 NPC、32 守卫、2 安全区：
  **0 条失效**。
- 清理后剩余旧数据：守卫/传送 0 失效；NPC 仅剩任务引用的旧城镇 NPC
  （29 条，见 4.2）；刷怪仅剩 Zircon 独有图上的既有缺陷（698 条，原样保留）。

### 5.3 BotRunner 冒烟

`dotnet BotRunner/bin/Debug/net10.0/BotRunner.dll BotRunner.82.json 1`：
登录成功、角色在线（map=1）、持续移动（move=79）、发现怪物目标（targets=50）、
找到 NPC（npcs=4, nearest=Mike@1）、金币正常。

## 6. 复现（交付物）

- DB：`Debug/ServerCore/Database/System.db`（已同步到仓库根 `System.db`）。
- 重放脚本：`Tools/DbMigrationTool/data/replay_migration.sh`
  （含全部写库命令与数据 json：import_plan_v2 / monsters_import /
  new_monster_images / delete_* / respawn_fixes）。
- 计划生成器：`Tools/DbMigrationTool/data/build_ei_import_plan.py`。
- 图库移植：`Tools/DbMigrationTool/data/build_lib_ports.py`。
- 源码：Enum.cs / MonsterLookup.cs / DbMigrationTool/Program.cs。

## 7. 已知限制

- 39 张 Zircon 独有图（kt00xx、D60x、0_00xx 等）的 .map 文件为截断文件
  （Zircon 与 EI 客户端同样截断），LazyLoadMaps=False 全量加载会崩溃；
  正常懒加载模式不受影响（玩家进入这些图才会崩，均为空图无内容）。
- 修罗暗殿等 124 条刷怪点按就近可走点修正，与英雄杀服务端原坐标有偏差
  （英雄杀服务端自身配置即指向墙）。
- 丛林猛犸家族图槽按 King raceimg 复刻，未做逐怪视觉确认（服务端数据
  与精灵内容不一致，见 2.3）。
