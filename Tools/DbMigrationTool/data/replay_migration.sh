#!/usr/bin/env bash
# 重放全部 EI 对齐 DB 变更（从 data/ 下的 json 重放）。
#
# 前置：
#   1. dotnet build Tools/DbMigrationTool/DbMigrationTool.csproj
#   2. Debug/ServerCore/Database/System.db 为 544 图基线（本任务开始时的状态）
#   3. 地图文件已部署（83 张 EI 独有 .map 拷贝到 Debug/{Client,ServerCore,Server}/Map/，
#      16 张交集图换成英雄杀服务端 Mud3/Map/ 版本 —— 见 docs/EI_ALIGNMENT_2026-08-11.md）
#   4. Mon-31.Zl / Mon-22.Zl 由 build_lib_ports.py 生成（新怪图槽移植）
#
# 用法：bash Tools/DbMigrationTool/data/replay_migration.sh
set -euo pipefail
cd "$(dirname "$0")/../../.."   # 仓库根

ROOT=/home/tetsuya/development/zircon/Debug/ServerCore/Database
DATA=Tools/DbMigrationTool/data
TOOL="dotnet Tools/DbMigrationTool/bin/Debug/net10.0/DbMigrationTool.dll --root $ROOT"

# 1. 新怪 MonsterInfo（125 条）
$TOOL import-monsters "$DATA/monsters_import.json"

# 2. EI 全量数据（83 新图 + 交集图：NPC/守卫/安全区/刷怪/传送）
$TOOL import-ei "$DATA/import_plan_v2.json"

# 3. 坐标失效清理（旧 Zircon 守卫/传送 + 沙巴克误导入 NPC + 失效旧刷怪）
$TOOL delete-records GuardInfo "$DATA/delete_guards.json"
$TOOL delete-records MovementInfo "$DATA/delete_movements.json"
$TOOL delete-records NPCInfo "$DATA/delete_npcs.json"
$TOOL delete-records RespawnInfo "$DATA/delete_old_respawns_ei.json"

# 4. 安全区与刷怪点修正
$TOOL set-safezone-point 31 1 423 102    # 图1 玩家出生点 -> EI StartPoint
$TOOL set-safezone-point 53 1_004 9 12   # 超级泡点中心不可走 -> 就近可走点
$TOOL set-safezone-point 54 z014 10 10   # 监狱中心不可走 -> 就近可走点
$TOOL trim-safezones                     # 旧安全区多点点集裁掉不可走点
$TOOL move-respawns "$DATA/respawn_fixes.json"   # 新怪刷怪点就近修正（124 条）

echo "重放完成。"
