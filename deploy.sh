#!/usr/bin/env bash
# deploy.sh — 构建并部署最新服务端 + 客户端到 NAS Mir2ei-godot/deploy
#
# 部署结构:
#   /home/tetsuya/NAS/Mir2ei-godot/deploy/
#   ├── server/                # 完整可运行服务端 (dotnet ServerCore.dll 直接跑)
#   │   ├── ServerCore.dll + 依赖 dll + deps/runtimeconfig
#   │   ├── Server.ini
#   │   ├── Database/System.db + Users.db
#   │   └── Map/ (743 张 EI 地图)
#   └── client/                # 完整客户端 (Godot 导出二进制 + 全部资源)
#       ├── Mir2eiClient       # Godot 4.6.3 mono 导出可执行文件 (arm64, 内嵌 pck)
#       └── Debug/Client/      # 资源: Data(.Zl 图库) / Map / Sound / Database
#
# 用法:
#   bash deploy.sh             # 构建 + 增量部署 (rsync, 只同步变更)
#   bash deploy.sh full        # 构建 + 全量部署 (先清空 deploy 目录)
#   bash deploy.sh --skip-build# 不构建, 只同步现有产物
#
# 部署后启动:
#   cd deploy/server && dotnet ServerCore.dll
#   cd deploy/client && ./Mir2eiClient -- --server 127.0.0.1 --port 7000 \
#       --user test@test.com --pass test123 --char TestHero --window
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVER_DIR="$ROOT/Debug/ServerCore"          # 服务端运行目录 (构建输出)
CLIENT_RES="$ROOT/Debug/Client"              # 客户端资源 (Data/Map/Sound)
CLIENT_EXPORT="$ROOT/deploy-out/Mir2eiClient" # Godot 导出产物
DEPLOY_ROOT="/home/tetsuya/NAS/Mir2ei-godot/deploy"
DEPLOY_SERVER="$DEPLOY_ROOT/server"
DEPLOY_CLIENT="$DEPLOY_ROOT/client"

MODE="${1:-incremental}"

# rsync 排除: 备份/临时/调试符号 + Windows 遗留反斜杠畸形文件 (CIFS 拒绝)
EXCLUDES=(--exclude='Backup/' --exclude='*.bak' --exclude='*.pdb' --exclude='nohup.out' --exclude='.\*')

echo "════════════════════════════════════════════════"
echo "  Zircon → Mir2ei-godot/deploy 部署"
echo "  模式: $MODE"
echo "════════════════════════════════════════════════"

# ---------- 0. 目标目录 ----------
mkdir -p "$DEPLOY_SERVER" "$DEPLOY_CLIENT"
if [ "$MODE" = "full" ]; then
    echo "[0/5] 全量模式: 清空 deploy 目录..."
    rm -rf "$DEPLOY_SERVER"/* "$DEPLOY_CLIENT"/*
fi

# ---------- 1. 构建服务端 ----------
if [ "$MODE" = "--skip-build" ]; then
    echo "[1/5] 跳过构建 (--skip-build)"
else
    echo "[1/5] 构建服务端 (输出到 $SERVER_DIR)..."
    dotnet build "$ROOT/ServerCore/ServerCore.csproj" --no-restore -o "$SERVER_DIR" 2>&1 | tail -3
fi

# ---------- 2. 构建客户端 C# ----------
if [ "$MODE" = "--skip-build" ]; then
    echo "[2/5] 跳过构建 (--skip-build)"
else
    echo "[2/5] 构建客户端 C# (ZirconClient)..."
    dotnet build "$ROOT/GodotClient/ZirconClient.csproj" --no-restore 2>&1 | tail -3
fi

# ---------- 3. Godot 导出客户端二进制 ----------
if [ "$MODE" = "--skip-build" ] && [ -f "$CLIENT_EXPORT" ]; then
    echo "[3/5] 跳过导出 (--skip-build, 已有产物)"
else
    echo "[3/5] Godot 导出客户端 (Linux arm64, 内嵌 pck)..."
    mkdir -p "$ROOT/deploy-out"
    godot-mono --headless --path "$ROOT/GodotClient" \
        --export-release Linux "$CLIENT_EXPORT" 2>&1 | tail -3
fi
[ -f "$CLIENT_EXPORT" ] || { echo "❌ 客户端导出失败: $CLIENT_EXPORT 不存在"; exit 1; }

# ---------- 4. 部署服务端 ----------
echo "[4/5] 同步服务端 → $DEPLOY_SERVER ..."
rsync -a --delete "${EXCLUDES[@]}" "$SERVER_DIR/" "$DEPLOY_SERVER/"
# 服务端可执行权限
chmod +x "$DEPLOY_SERVER/ServerCore.dll" 2>/dev/null || true

# ---------- 5. 部署客户端 ----------
echo "[5/5] 同步客户端 → $DEPLOY_CLIENT ..."
# 5a. 二进制 + C# 程序集数据目录 (Godot mono 导出: 可执行文件 + data_ZirconClient_*)
rsync -a "$CLIENT_EXPORT" "$DEPLOY_CLIENT/Mir2eiClient"
chmod +x "$DEPLOY_CLIENT/Mir2eiClient"
for d in "$ROOT"/deploy-out/data_*; do
    [ -d "$d" ] || continue
    rsync -a --delete "$d/" "$DEPLOY_CLIENT/$(basename "$d")/"
done
# 5b. 资源 (8.7G, rsync 增量)
mkdir -p "$DEPLOY_CLIENT/Debug"
rsync -a --delete "${EXCLUDES[@]}" "$CLIENT_RES/" "$DEPLOY_CLIENT/Debug/Client/"

# ---------- 摘要 ----------
echo ""
echo "════════════════════════════════════════════════"
echo "  ✅ 部署完成"
echo "════════════════════════════════════════════════"
echo "  server: $DEPLOY_SERVER"
echo "    $(du -sh "$DEPLOY_SERVER" | cut -f1)  ·  $(find "$DEPLOY_SERVER" -type f | wc -l) 文件"
echo "    启动: cd $DEPLOY_SERVER && dotnet ServerCore.dll"
echo ""
echo "  client: $DEPLOY_CLIENT"
echo "    $(du -sh "$DEPLOY_CLIENT" | cut -f1)  ·  $(find "$DEPLOY_CLIENT" -type f | wc -l) 文件"
echo "    启动: cd $DEPLOY_CLIENT && ./Mir2eiClient -- --server 127.0.0.1 --port 7000 \\"
echo "           --user test@test.com --pass test123 --char TestHero --window"
echo "════════════════════════════════════════════════"
