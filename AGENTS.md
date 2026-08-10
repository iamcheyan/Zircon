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

## 工作约定

- 推送远程是 `fork`（iamcheyan/Zircon），不是 `origin`（Suprcode/Zircon）
- 不要触碰原版 `Client/` 源码（除非通过 `NoColourKey` 机制等明确手段）
- 用 `dotnet build GodotClient/ZirconClient.csproj` 验证 Godot 端修复
- 用 `hub` 启动/停止服务（`zircon-dev`、`zircon-server`）
- commit 信息用中文，遵循仓库现有风格
- 切勿泄露个人信息或 API keys
