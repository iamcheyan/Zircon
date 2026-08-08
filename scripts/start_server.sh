#!/usr/bin/env bash
set -e

# 获取项目根目录绝对路径
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

SERVER_TMP_DIR="/tmp/zircon-server"
BUILD_OUT_DIR="$PROJECT_ROOT/ServerCore/bin/Debug/net10.0"

echo "=== Zircon 服务端启动脚本 ==="

# 1. 检查服务器进程是否已在运行
RUNNING_PID=$(pgrep -f "ServerCore.dll" || true)

if [ -n "$RUNNING_PID" ]; then
    echo "提示: 检测到服务端正在运行 (PID: $RUNNING_PID)"
    read -t 5 -p "是否要重启服务端？(y/N, 5秒后默认不重启): " -n 1 -r || true
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "正在停止旧的服务端进程 (PID: $RUNNING_PID)..."
        kill $RUNNING_PID 2>/dev/null || true
        sleep 1
        # 如果还在运行，强制 kill
        if kill -0 $RUNNING_PID 2>/dev/null; then
            kill -9 $RUNNING_PID 2>/dev/null || true
        fi
    else
        echo "保持当前运行状态，脚本退出。"
        exit 0
    fi
fi

# 2. 检查并准备 /tmp/zircon-server 运行环境
echo "检查/构建运行环境: $SERVER_TMP_DIR ..."
mkdir -p "$SERVER_TMP_DIR"

# 建立 Database 软链接
if [ ! -L "$SERVER_TMP_DIR/Database" ]; then
    rm -rf "$SERVER_TMP_DIR/Database"
    ln -s "$PROJECT_ROOT/Debug/Server/Database" "$SERVER_TMP_DIR/Database"
    echo " -> 已建立 Database 软链接"
fi

# 建立 Map 软链接
if [ ! -L "$SERVER_TMP_DIR/Map" ]; then
    rm -rf "$SERVER_TMP_DIR/Map"
    ln -s "$PROJECT_ROOT/Debug/Client/Map" "$SERVER_TMP_DIR/Map"
    echo " -> 已建立 Map 软链接"
fi

# 3. 检查编译产物，不存在则自动编译
if [ ! -f "$BUILD_OUT_DIR/ServerCore.dll" ]; then
    echo "未找到编译产物，正在自动编译 ServerCore..."
    dotnet build "$PROJECT_ROOT/ServerCore/ServerCore.csproj" -c Debug
fi

# 同步最新的编译产物
echo "同步最新服务端程序文件..."
cp -r "$BUILD_OUT_DIR"/* "$SERVER_TMP_DIR/" 2>/dev/null || true

# 4. 启动服务端
echo "正在启动服务端..."
cd "$SERVER_TMP_DIR"
exec dotnet ServerCore.dll
