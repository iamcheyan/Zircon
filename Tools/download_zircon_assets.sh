#!/usr/bin/env bash
# Download all Zircon client assets (Data/*.Zl, Map/*.map, Sound/*.wav, Database/System.db)
# from the LOMCN file server into the current directory.
#
# Sources:
#   https://files.lomcn.co.uk/resources/mir3/zircon/          (root index)
#   https://files.lomcn.co.uk/resources/mir3/zircon/patch/    (Data/Map/Sound, live-server data)
#   https://files.lomcn.co.uk/resources/mir3/zircon/Database.7z (System.db, 2024-02-24)
#
# Usage: bash Tools/download_zircon_assets.sh [target_dir]
# Requires: curl, gzip. Parallel downloader: aria2c (optional, recommended).
set -euo pipefail

BASE="https://files.lomcn.co.uk/resources/mir3/zircon"
PATCH="$BASE/patch"
TARGET="${1:-.}"
JOBS=8

mkdir -p "$TARGET"
cd "$TARGET"
mkdir -p Data Map Sound Database

echo "==> [1/3] System.db (Database.7z)"
if [ ! -f Database/System.db ]; then
    curl -fL --retry 3 -o Database.7z "$BASE/Database.7z"
    if command -v 7z >/dev/null 2>&1; then
        7z e -y Database.7z -oDatabase >/dev/null
    elif command -v 7za >/dev/null 2>&1; then
        7za e -y Database.7z -oDatabase >/dev/null
    else
        echo "!! 7z not found - extract Database.7z manually (contains System.db)" >&2
    fi
    rm -f Database.7z
fi

echo "==> [2/3] Fetching patch file list"
curl -fsSL --retry 3 -o /tmp/zircon_patch_index.html "$PATCH/"
# h5ai index: href="/resources/mir3/zircon/patch/<Name>"
grep -oE 'href="[^"]*patch/[^"]+"' /tmp/zircon_patch_index.html \
    | sed 's/href="//;s/"//' \
    | sed 's/.*patch\///' \
    | sed 's/%20/ /g' \
    | grep -E '^(Data|Map|Sound)-' \
    | grep -viE 'desktop\.ini' \
    | sort -u > /tmp/zircon_filelist.txt
rm -f /tmp/zircon_patch_index.html

TOTAL=$(wc -l < /tmp/zircon_filelist.txt)
echo "    $TOTAL files to fetch (Data/Map/Sound)."

fetch_one() {
    local name="$1"
    local url="$PATCH/$(printf '%s' "$name" | sed 's/ /%20/g')"
    local outdir outfile
    case "$name" in
        Data-*)  outdir=Data  ; outfile="${name#Data-}"  ;;
        Map-*)   outdir=Map   ; outfile="${name#Map-}"   ;;
        Sound-*) outdir=Sound ; outfile="${name#Sound-}" ;;
        *) return ;;
    esac
    outfile="${outfile%.gz}"   # strip .gz: download stores .gz, extract strips it
    local gz="$outdir/$outfile.gz"
    if [ -f "$outdir/$outfile" ]; then return; fi   # already done
    if command -v aria2c >/dev/null 2>&1; then
        aria2c -q -x4 -s4 -c --file-allocation=none -d "$outdir" -o "$outfile.gz" "$url"
    else
        curl -fL --retry 3 -C - -o "$gz" "$url"
    fi
    gzip -dc "$gz" > "$outdir/$outfile"
    rm -f "$gz"
}
export -f fetch_one
export PATCH

echo "==> [3/3] Downloading + decompressing (jobs=$JOBS)"
if command -v xargs >/dev/null 2>&1; then
    cat /tmp/zircon_filelist.txt | xargs -P "$JOBS" -I{} bash -c 'fetch_one "$1"' _ {}
else
    while read -r f; do fetch_one "$f"; done < /tmp/zircon_filelist.txt
fi

# Reorganize "Map Data-*" libraries into Data\Map Data\[biome]\ subdirs
# (patch flattens paths with dashes; client expects Data\Map Data\Animationsc.Zl etc.)
for f in Data/Map\ Data-*; do
    [ -f "$f" ] || continue
    name="${f#Data/Map Data-}"   # e.g. "Forest-Animationsc.Zl"
    sub="${name%%-*}"            # biome prefix or the filename itself
    case "$sub" in
        Forest|Sand|Snow|Wood)
            mkdir -p "Data/Map Data/$sub"
            mv -- "$f" "Data/Map Data/$sub/${name#*-}" ;;
        *)
            mkdir -p "Data/Map Data"
            mv -- "$f" "Data/Map Data/$name" ;;
    esac
done

echo
echo "Done. Contents:"
echo "  Database/System.db  $(du -h Database/System.db 2>/dev/null | cut -f1 || echo '?')"
echo "  Data/    $(find Data    -type f 2>/dev/null | wc -l) files"
echo "  Map/     $(find Map     -type f 2>/dev/null | wc -l) files"
echo "  Sound/   $(find Sound   -type f 2>/dev/null | wc -l) files"
