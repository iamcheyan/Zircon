#!/usr/bin/env bash
# ============================================================================
# Zircon 开发环境一键搭建脚本 (setup_environment.sh)
#
# 用途: clone 本仓库后运行此脚本, 即可得到与本仓库一致的开发环境:
#   - 客户端资源  → Debug/Client/{Data, Map, Sound, Database}
#   - 服务端运行目录 (可选 --server-dir, 含 dotnet publish + Server.ini)
#   - wav→ogg 音频优化 (可选 --convert-ogg, 与 Tools/convert_audio_to_ogg.cmd 一致)
#
# 资源来源: https://files.lomcn.co.uk/resources/mir3/zircon/
#   Database.7z → System.db (2024-02-24, sha256 固定校验)
#   patch/      → Data-* / Map-* / Sound-* (live 服务端数据, 逐个 gzip)
#
# 已还原的本仓库环境修复:
#   1. Map Data-* 平铺文件重组织为 Data/Map Data/{Forest,Sand,Snow,Wood}/
#      (patch 用短横线拍平了路径, 客户端期望子目录结构)
#   2. HorseS.Zl / MagicEx10.wtl / MagicEx11.wtl 随全部 Data-* 一起进 Data/
#      (早期手工下载时被放到 _extra/, 正确位置是 Data/ 图库目录)
#   3. Data/System.db: 客户端 Session root = .\Data\ (CEnvir.cs:372),
#      需把 System.db 复制一份到 Data/ 下
#   4. Server.ini 开发模式: CheckVersion=False, AllowStartGame=True
#      (路径分隔符跨平台修复已在代码中提交, 无需脚本处理)
#
# 依赖: curl, gzip; 建议 aria2c (并行下载); 7z 或 7za (解 Database.7z);
#       ffmpeg (仅 --convert-ogg); dotnet SDK (仅 --server-dir)
#
# 用法:
#   bash Tools/setup_environment.sh                 # 下载全部资源到 Debug/Client/
#   bash Tools/setup_environment.sh --server-dir    # 另建服务端运行目录 (Debug/ServerCore)
#   bash Tools/setup_environment.sh --skip-data     # 只处理 System.db, 不下载大资源
#   bash Tools/setup_environment.sh --convert-ogg   # 转换音频 wav→ogg (体积优化)
# ============================================================================
set -euo pipefail

BASE="https://files.lomcn.co.uk/resources/mir3/zircon"
PATCH="$BASE/patch"
SYSTEM_DB_SHA256="c43ed64125dbd651a955b62409b7bb80594d9eda46bb800e1bca4423d9ccc4d0"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CLIENT_DIR="$REPO_ROOT/Debug/Client"
SERVER_DIR=""
CONVERT_OGG=0
SKIP_DATA=0
JOBS=8

usage() {
    sed -n '2,31p' "$0" | sed 's/^# \{0,1\}//'
    exit 0
}

while [ $# -gt 0 ]; do
    case "$1" in
        --client-dir) CLIENT_DIR="${2:?--client-dir 需要目录}"; shift 2 ;;
        --server-dir) SERVER_DIR="${2:-$REPO_ROOT/Debug/ServerCore}"; [ "${2:-}" != "" ] && shift; shift ;;
        --convert-ogg) CONVERT_OGG=1; shift ;;
        --skip-data) SKIP_DATA=1; shift ;;
        --jobs) JOBS="${2:?--jobs 需要数字}"; shift 2 ;;
        --help|-h) usage ;;
        *) echo "未知参数: $1 (--help 查看用法)"; exit 2 ;;
    esac
done

echo "=== Zircon 环境搭建 ==="
echo "资源目录: $CLIENT_DIR"
[ -n "$SERVER_DIR" ] && echo "服务端目录: $SERVER_DIR"
[ "$CONVERT_OGG" = 1 ] && echo "音频转换: 开"
[ "$SKIP_DATA" = 1 ] && echo "跳过 Data/Map/Sound: 开"
echo

mkdir -p "$CLIENT_DIR/Data" "$CLIENT_DIR/Map" "$CLIENT_DIR/Sound" "$CLIENT_DIR/Database"

# ---------------------------------------------------------------------------
# Stage 1/5: System.db (Database.7z → 解压 → sha256 固定校验)
# ---------------------------------------------------------------------------
echo "==> [1/5] System.db"
TARGET_DB="$CLIENT_DIR/Database/System.db"

if [ -s "$TARGET_DB" ] && echo "$SYSTEM_DB_SHA256  $TARGET_DB" | sha256sum -c - >/dev/null 2>&1; then
    echo "  System.db 已存在且校验一致, 跳过"
else
    echo "  System.db 缺失或校验不一致, 重新下载..."
    curl -fL --retry 3 -o "$CLIENT_DIR/Database.7z" "$BASE/Database.7z"
    if ! (cd "$CLIENT_DIR/Database" && 7z e -y ../Database.7z >/dev/null 2>&1 || 7za e -y ../Database.7z >/dev/null 2>&1); then
        echo "!! 解压 Database.7z 失败, 请手动安装 7z 或 7za" >&2
        exit 1
    fi
    rm -f "$CLIENT_DIR/Database.7z"
    if ! echo "$SYSTEM_DB_SHA256  $TARGET_DB" | sha256sum -c - >/dev/null 2>&1; then
        echo "!! System.db 校验失败: $(sha256sum "$TARGET_DB")" >&2
        exit 1
    fi
    echo "  System.db 下载并校验通过"
fi

# 客户端副本: 客户端 Session root = .\Data\ (CEnvir.cs:372)
if [ ! -s "$CLIENT_DIR/Data/System.db" ]; then
    cp "$TARGET_DB" "$CLIENT_DIR/Data/System.db"
    echo "  Data/System.db (客户端副本) 已就位"
else
    echo "  Data/System.db 已存在"
fi

# ---------------------------------------------------------------------------
# Stage 2/5: 抓取 patch 文件清单
# ---------------------------------------------------------------------------
echo "==> [2/5] 抓取 patch 文件清单"
curl -fsSL --retry 3 -o /tmp/zircon_patch_index.html "$PATCH/"
# h5ai 索引: href=".../patch/<Name>"
grep -oE 'href="[^"]*patch/[^"]+"' /tmp/zircon_patch_index.html \
    | sed 's/href="//;s/"//' \
    | sed 's/.*patch\///' \
    | sed 's/%20/ /g' \
    | grep -E '^(Data|Map|Sound)-' \
    | grep -viE 'desktop\.ini' \
    | sort -u > /tmp/zircon_filelist.txt
rm -f /tmp/zircon_patch_index.html

EXPECT_DATA=$(grep -cE '^Data-' /tmp/zircon_filelist.txt || true)
EXPECT_MAP=$(grep -cE '^Map-' /tmp/zircon_filelist.txt || true)
EXPECT_SOUND=$(grep -cE '^Sound-' /tmp/zircon_filelist.txt || true)
echo "  清单: Data=$EXPECT_DATA Map=$EXPECT_MAP Sound=$EXPECT_SOUND"

if [ "$SKIP_DATA" = 1 ]; then
    echo "  (--skip-data) 跳过 Data/Map/Sound 下载"
else
    # -----------------------------------------------------------------------
    # Stage 3/5: 并行下载 + 解压
    # -----------------------------------------------------------------------
    echo "==> [3/5] 下载 + 解压 (jobs=$JOBS)"

    # 计算某个 patch 文件解压后的最终落点 (Map Data-* 重组织后的位置也算出来)
    final_path() {
        local name="$1" outdir outfile
        case "$name" in
            Data-*)  outdir="$CLIENT_DIR/Data"  ; outfile="${name#Data-}"  ;;
            Map-*)   outdir="$CLIENT_DIR/Map"   ; outfile="${name#Map-}"   ;;
            Sound-*) outdir="$CLIENT_DIR/Sound" ; outfile="${name#Sound-}" ;;
            *) return 1 ;;
        esac
        outfile="${outfile%.gz}"
        if [[ "$outfile" == "Map Data-"* ]]; then
            local rest="${outfile#Map Data-}" sub="${outfile#Map Data-}"
            sub="${sub%%-*}"
            case "$sub" in
                Forest|Sand|Snow|Wood) echo "$outdir/Map Data/$sub/${rest#*-}" ;;
                *) echo "$outdir/Map Data/$rest" ;;
            esac
        else
            echo "$outdir/$outfile"
        fi
    }

    fetch_one() {
        local name="$1" url final
        final="$(final_path "$name")" || return 0
        [ -s "$final" ] && return 0                    # 已存在且非空 → 跳过
        url="$PATCH/$(printf '%s' "$name" | sed 's/ /%20/g')"
        local dir
        dir="$(dirname "$final")"
        mkdir -p "$dir"
        local gz="$dir/.$(basename "$final").gz.part"
        if command -v aria2c >/dev/null 2>&1; then
            aria2c -q -x4 -s4 -c --file-allocation=none -d "$dir" -o "$(basename "$gz")" "$url"
        else
            curl -fsSL --retry 3 -C - -o "$gz" "$url" || { rm -f "$gz"; return 1; }
        fi
        if ! gzip -dc "$gz" > "$final.tmp" 2>/dev/null; then
            rm -f "$gz" "$final.tmp"
            return 1
        fi
        mv "$final.tmp" "$final"
        rm -f "$gz"
    }
    export -f fetch_one final_path
    export CLIENT_DIR PATCH

    if command -v xargs >/dev/null 2>&1; then
        cat /tmp/zircon_filelist.txt | xargs -P "$JOBS" -I{} bash -c 'fetch_one "$1"' _ {}
    else
        while read -r f; do fetch_one "$f"; done < /tmp/zircon_filelist.txt
    fi
    echo "  下载完成"

    # -----------------------------------------------------------------------
    # Stage 4/5: Map Data 重组织 (patch 平铺 → 客户端子目录结构)
    # -----------------------------------------------------------------------
    echo "==> [4/5] Map Data 重组织"
    for f in "$CLIENT_DIR"/Data/Map\ Data-*; do
        [ -f "$f" ] || continue
        name="${f#*Data/Map Data-}"            # 如 Forest-Animationsc.Zl
        sub="${name%%-*}"
        case "$sub" in
            Forest|Sand|Snow|Wood)
                mkdir -p "$CLIENT_DIR/Data/Map Data/$sub"
                mv -- "$f" "$CLIENT_DIR/Data/Map Data/$sub/${name#*-}" ;;
            *)
                mkdir -p "$CLIENT_DIR/Data/Map Data"
                mv -- "$f" "$CLIENT_DIR/Data/Map Data/$name" ;;
        esac
    done
    echo "  重组织完成"
fi

# ---------------------------------------------------------------------------
# Stage 5/5: 验证
# ---------------------------------------------------------------------------
echo "==> [5/5] 验证"
COUNT_DATA=$(find "$CLIENT_DIR/Data" -type f | wc -l | tr -d ' ')
COUNT_MAP=$(find "$CLIENT_DIR/Map" -type f | wc -l | tr -d ' ')
COUNT_SOUND=$(find "$CLIENT_DIR/Sound" -type f | wc -l | tr -d ' ')
COUNT_DB_ZL=$(find "$CLIENT_DIR/Data" -name '*.Zl' | wc -l | tr -d ' ')
COUNT_DB_MAP=$(find "$CLIENT_DIR/Map" -name '*.map' | wc -l | tr -d ' ')
COUNT_DB_WAV=$(find "$CLIENT_DIR/Sound" -name '*.wav' | wc -l | tr -d ' ')

echo "  Data/    $COUNT_DATA 个文件 (含 $COUNT_DB_ZL 个 .Zl)"
echo "  Map/     $COUNT_MAP 个文件 ($COUNT_DB_MAP 个 .map)"
echo "  Sound/   $COUNT_SOUND 个文件 ($COUNT_DB_WAV 个 .wav)"
echo "  Database/System.db   $(du -h "$TARGET_DB" 2>/dev/null | cut -f1)"

if [ "$SKIP_DATA" = 1 ]; then
    echo "  (跳过下载, 验证跳过)"
elif [ "$COUNT_MAP" -lt "$EXPECT_MAP" ] || [ "$COUNT_SOUND" -lt "$EXPECT_SOUND" ] || [ "$COUNT_DATA" -lt "$EXPECT_DATA" ]; then
    echo "!! 警告: 文件数少于清单 (Data $EXPECT_DATA / Map $EXPECT_MAP / Sound $EXPECT_SOUND), 请重跑脚本续传" >&2
fi

for special in "Data/HorseS.Zl" "Data/MagicEx10.wtl" "Data/MagicEx11.wtl" "Data/System.db"; do
    if [ -s "$CLIENT_DIR/$special" ]; then
        echo "  ✓ $special"
    else
        echo "  ✗ $special 缺失!" >&2
    fi
done

# ---------------------------------------------------------------------------
# 可选: 服务端运行目录
# ---------------------------------------------------------------------------
if [ -n "$SERVER_DIR" ]; then
    echo "==> [server] 搭建服务端运行目录: $SERVER_DIR"
    mkdir -p "$SERVER_DIR"
    if command -v dotnet >/dev/null 2>&1; then
        dotnet publish "$REPO_ROOT/ServerCore/ServerCore.csproj" -c Release -o "$SERVER_DIR" -v q
    else
        echo "!! dotnet SDK 未安装, 跳过 publish (请手动编译 ServerCore)" >&2
    fi
    cp -r "$CLIENT_DIR/Map/." "$SERVER_DIR/Map/" 2>/dev/null || true
    mkdir -p "$SERVER_DIR/Database"
    cp "$TARGET_DB" "$SERVER_DIR/Database/System.db"
    cat > "$SERVER_DIR/Server.ini" <<'EOF'
[Network]
IPAddress=127.0.0.1
Port=7000

[System]
CheckVersion=False
MapPath=./Map/

[Control]
AllowStartGame=True
RelogDelay=00:00:02
EOF
    echo "  Server.ini 已写入 (开发模式: CheckVersion=False, AllowStartGame=True)"
    echo "  启动: dotnet \"$SERVER_DIR/ServerCore.dll\""
fi

# ---------------------------------------------------------------------------
# 可选: wav→ogg (与 Tools/convert_audio_to_ogg.cmd 相同参数: libvorbis -q:a 5)
# ---------------------------------------------------------------------------
if [ "$CONVERT_OGG" = 1 ]; then
    echo "==> [ogg] 转换音频 wav→ogg"
    if ! command -v ffmpeg >/dev/null 2>&1; then
        echo "!! ffmpeg 未安装, 跳过 (客户端有 .wav 回退, 不影响运行)" >&2
    else
        CONVERTED=0
        while IFS= read -r -d '' wav; do
            ogg="${wav%.wav}.ogg"
            if [ -f "$ogg" ] && [ "$ogg" -nt "$wav" ]; then continue; fi
            if ffmpeg -hide_banner -loglevel warning -y -i "$wav" -vn -c:a libvorbis -q:a 5 "$ogg" 2>/dev/null; then
                CONVERTED=$((CONVERTED + 1))
            fi
        done < <(find "$CLIENT_DIR/Sound" -name '*.wav' -print0)
        echo "  转换完成: $CONVERTED 个"
    fi
fi

echo
echo "=== 完成 ==="
echo "客户端资源: $CLIENT_DIR (Godot 客户端: godot-mono --path GodotClient/)"
[ -n "$SERVER_DIR" ] && echo "服务端: $SERVER_DIR (dotnet ServerCore.dll, 监听 127.0.0.1:7000)"
