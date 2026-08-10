#!/usr/bin/env bash
# rebuild_all.sh — 全链重建 wiki 数据产物（NAS 挂载恢复后运行）。
#
# 数据链:
#   NAS 原始数据 → rebuild_tmp → wiki_build → three_versions_check
#   → SystemDbProbe (--images/--stores) → magic_anim → map_routes
#   → dat_integrate → ver_tags → stores_build → img_pipeline → thumb_gen
#
# 输入依赖:
#   /home/tetsuya/NAS/TMP（Mud3 服务端 + EI 客户端 + mir3ei）
#   Zircon/Debug/Server/Database/System.db（dotnet SystemDbProbe）
set -euo pipefail

cd "$(dirname "$0")/../.."

NAS=/home/tetsuya/NAS/TMP
DB=/home/tetsuya/development/Zircon/Debug/Server/Database

if [ ! -d "$NAS" ]; then
    echo "!! NAS 未挂载: $NAS" >&2
    exit 1
fi
if [ ! -f "$DB/System.db" ]; then
    echo "!! System.db 缺失: $DB/System.db" >&2
    exit 1
fi

step() { echo; echo "===== [$SECONDS s] $* ====="; }

step "1/11 rebuild_tmp (NAS 聚合)"
python3 Tools/web/rebuild_tmp.py

step "2/11 wiki_build (百科主数据)"
python3 Tools/web/wiki_build.py

step "3/11 three_versions_check (三版本比对)"
python3 Tools/common/three_versions_check.py

step "4/11 SystemDbProbe (Zircon 图片映射 + 商店)"
dotnet run --project Tools/SystemDbProbe -- "$DB/" --images /tmp/wiki_images.json --stores /tmp/stores.json

step "5/11 magic_anim (施法动画表)"
python3 Tools/content/magic_anim.py

step "6/11 map_routes (地图路线)"
python3 Tools/maps/map_routes.py

step "7/11 dat_integrate (老版三 DAT 接入)"
python3 Tools/content/dat_integrate.py

step "8/11 ver_tags (版本标签 + 挂靠)"
python3 Tools/common/ver_tags.py

step "9/11 stores_build (商店板块)"
python3 Tools/content/stores_build.py

step "10/11 img_pipeline (素材图渲染)"
python3 Tools/web/img_pipeline.py

step "11/11 thumb_gen (地图缩略图)"
python3 Tools/maps/thumb_gen.py

echo
echo "===== 全链重建完成（$((SECONDS/60))m$((SECONDS%60))s）====="
