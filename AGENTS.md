# AGENTS.md — Zircon 智能体协作指引

本文件供所有智能体（AI agent）在 Zircon 仓库工作时参考。

## 查找文档与资料

**重要：需要找文档、研究资料、历史结论、交接说明时，优先去以下目录：**

1. **`/home/tetsuya/development/Mir3-Research/docs/`** — 原版客户端逆向研究、资源解码、
   地图审计、UI 证据、渲染对比、问题排查等**所有研究文档**都集中在这里。
   包括但不限于：
   - `MAP_TILE_DESERTED_MINE_BUG.md` — 矿区/僵尸洞地面贴图问题完整分析（含终局裁决）
   - `GODOT_RENDERING_AUDIT.md`、`RENDER_PARITY_AUDIT_*.md` — 渲染对比审计
   - `AGENT_HANDOFF_PARITY_GOAL.md`、`OPERATION_PARITY_*.md` — 交接文档
   - `docs/database/`、`docs/notes/`、`docs/handoffs/`、`docs/research/` — 子目录分类
   - `Tools/` — 解码/审计工具（`Tools/common/wilsdk.py` 为 WIL/WIX 解码库，
     `Tools/maps/` 为地图工具）

2. **本仓库 `docs/`** — Zircon 自身项目文档（修复记录、迁移文档、部署说明）：
   - `MAP_FORMAT_COMPARISON.md` — .map 格式对比（Zircon .Zl vs NAS .wil）
   - `MAGIC_FULL_AUDIT.md`、`MAGIC_GROUND_EFFECT_FIXES.md` — 魔法特效审计
   - `REMOTE_SERVER_AND_CLIENT_SETUP.md` — 服务器部署说明
   - `Docs/`（大写）— 其它临时分析文档

3. **Mir3-Research 环境变量约定**（工具脚本依赖）：
   ```bash
   export MIR3_EI_ROOT=/home/tetsuya/NAS/TMP/EI传奇3.0客户端
   export MIR3_MUD3_ROOT=/home/tetsuya/NAS/TMP/Mud3
   export MIR3_ZIRCON_ROOT=/home/tetsuya/development/Zircon
   ```

## 测试账号

```bash
godot-mono --path /home/tetsuya/development/Zircon/GodotClient -- --user test@test.com --pass test123 --char TestHero --window
```

- 账号：`test@test.com`，密码：`test123`，角色：`TestHero`
- 用途：登录本地/远程测试服务器进行游戏内验证
- 构建客户端：`cd /home/tetsuya/development/Zircon && dotnet build GodotClient/ZirconClient.csproj`
  （注意必须在仓库根目录执行，`~` 下找不到项目文件）
- GM 权限：**该账号 `Admin = True`（永久 GM）**，可直接使用所有 `@` 管理命令；
  另外服务器支持 `MasterPassword` 机制（非邮箱格式登录名 + 主密码 → TempAdmin），
  详见 `ServerLibrary/Envir/SEnvir.cs` Login 逻辑

## GM 命令（管理员权限）

**用法**：游戏内聊天框直接输入，`@` 开头 + 空格分隔参数。
命令定义在 `ServerLibrary/Envir/Commands/Command/Admin/`，仅 `Account.Admin` 或
`TempAdmin` 可用（`AdminCommandHandler.IsAllowedByPlayer`）。

### 传送类

| 命令 | 用途 | 示例 |
|------|------|------|
| `@move 地图  x  y` | 传送到地图指定坐标（省略坐标=随机点） | `@move D201`、`@move D201 54 287` |
| `@goto 角色名` | 传送到某角色身边 | `@goto TestHero` |
| `@recall 角色名` | 把某角色拉到身边 | `@recall TestHero` |

### 角色/状态类

| 命令 | 用途 | 示例 |
|------|------|------|
| `@level 数字` | 设置等级 | `@level 50` |
| `@addstat 属性 值` | 加属性点 | `@addstat` |
| `@giveSkills` | 给技能 | `@giveSkills` |
| `@levelSkill` | 技能升级 | `@levelSkill` |
| `@levelWeapon` | 武器升级 | `@levelWeapon` |
| `@toggleGM` | 切换 GameMaster 模式（怪物不主动攻击） | `@toggleGM` |
| `@toggleSuperman` | 超人模式（无敌） | `@toggleSuperman` |
| `@toggleObserver` | 观察者模式 | `@toggleObserver` |
| `@resetDiscipline` | 重置修炼 | `@resetDiscipline` |
| `@removePKPoints` | 清除 PK 值 | `@removePKPoints` |

### 物品/生成类

| 命令 | 用途 | 示例 |
|------|------|------|
| `@make 物品名` | 刷物品到背包 | `@make 金创药` |
| `@giveHorse` | 给马 | `@giveHorse` |
| `@spawn 怪物 数量` | 刷怪 | `@spawn GhostSorcerer 5` |
| `@setCompanionLevel/Stat` | 宠物等级/属性 | `@setCompanionLevel` |
| `@setHermitStat` | 隐士属性 | `@setHermitStat` |

### 服务器管理类

| 命令 | 用途 | 示例 |
|------|------|------|
| `@kick 角色名` | 踢人下线 | `@kick TestHero` |
| `@ban 账号 天数 原因` | 封禁 | `@ban` |
| `@chatban` | 禁言 | `@chatban` |
| `@clearIPBlocks` | 清空 IP 封禁 | `@clearIPBlocks` |
| `@reboot` | 重启服务器 | `@reboot` |
| `@createGuild` | 创建公会 | `@createGuild` |
| `@giveGameGold` | 发游戏金币 | `@giveGameGold` |
| `@takeGameGold` | 扣游戏金币 | `@takeGameGold` |
| `@promoteFame` | 提升声望 | `@promoteFame` |

### 常用矿区传送（测试用）

```
@move D201    ← 废矿1层（僵尸洞，黑/熔岩地砖）
@move D202    ← 废矿2层
@move D203    ← 废矿3层
@move D101    ← 比奇矿洞1层
```

## 工作约定

- 推送远程是 `fork`（iamcheyan/Zircon），不是 `origin`（Suprcode/Zircon）
- 不要触碰原版 `Client/` 源码（除非通过 `NoColourKey` 机制等明确手段）
- 用 `dotnet build GodotClient/ZirconClient.csproj` 验证 Godot 端修复
- 用 `hub` 启动/停止服务（`zircon-dev`、`zircon-server`）
- commit 信息用中文，遵循仓库现有风格
- 切勿泄露个人信息或 API keys
