#!/usr/bin/env python3
"""mapviewer.py — Mir3 EI / Zircon .map browser (Server-rendered tile pyramid).

Correctly parses Zircon / Mir3 EI .map format & renders isometric layers:
  - Back (Ground) layer (half-res, 96x64 tiles)
  - Middle layer (SmTiles / Objects)
  - Front layer (Objects / Houses / Walls / Cliffs)

Maps KR Library IDs (0..55) to Wemade WIL / ZL image libraries (KROrder table).
"""

from __future__ import annotations

import argparse
import io
import json
import math
import os
import re
import struct
import sys
import threading
from collections import OrderedDict
from functools import lru_cache

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from concurrent.futures import ProcessPoolExecutor
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

try:
    from PIL import Image
except ImportError:
    Image = None

from wilsdk import WilLibrary
from zlsdk import ZlLibrary
from mapnames import resolve as map_cn

TILE_SZ = 512          # tile size in screen pixels
CACHE_MAPS_MAX = 3     # decoded maps kept in memory
CACHE_TILES_MAX = 400  # rendered tiles (PNG bytes) kept in memory
CACHE_FRAMES_BYTES = 256 * 1024 * 1024  # decoded frames LRU budget (per process)
THUMBS_DIR = "/tmp/wiki_thumbs"  # pre-rendered full-map thumbnails (shared with WikiServer/thumb_gen)
MAX_FULL_DIM = 16384   # full-map single image: longest side cap (px)
FIT_FULL_DIM = 2048    # full-map "fit" level: longest side target (px)

# Layout modes.  Mir3.exe (EI 2002) renders the map grid axis-aligned:
# every draw call projects cell (x,y) with a single-axis term (x*48, y*32)
# and the viewport is a plain 36x36 square; the 8-way scroll table moves
# N/E/S/W by a single pixel axis.  The apparent "isometric" look of the
# game comes from perspective baked into the sprites, not the projection.
# "iso" is kept only as a legacy/debug view.
LAYOUT_RECT = "rect"
LAYOUT_ISO = "iso"

# WIL frame-offset modes (rect layout only; iso keeps the legacy
# centre-anchor + offset behaviour and ignores this switch).
# Disassembly of Mir3.exe is conclusive that the EI map layers NEVER read
# the frame +4/+6 offset fields (ground 0x43b440/0x43b9a0/0x43c3xx/0x43c4c9,
# animated ground 0x434a20, mid/front 0x43bb10/0x43be00, blend 0x43bcf5) —
# only the actor layer (0x41cbd0/0x40b5xx/0x40fbxx/0x430axx) does.  These
# modes exist purely to record the divergence ("none" = the original).
OFFSET_NONE = "none"      # original: no offsets anywhere
OFFSET_ALL = "all"        # hypothetical: back + mid/front shifted by frame offsets
OFFSET_MIDFRONT = "midfront"  # hypothetical: only mid/front shifted
OFFSET_MODES = (OFFSET_NONE, OFFSET_ALL, OFFSET_MIDFRONT)

# ---- Game minimap assets (MiniMap.Zl / mmap.wil) ----
# MapInfo.MiniMap (System.db, via Tools/SystemDbProbe --minimap) maps a map
# file stem -> frame index in the MiniMap library.  The library lives next to
# the other map-tile libs in the data dir.
MINIMAP_MAP_FILE = "/tmp/minimap_map.txt"    # 2017 ZL client: {stem -> frame} dump (244 maps)
MINIMAP_EI_FILE = "/tmp/minimap_map_ei.txt"  # EI client: {stem -> libname -> frame} dump (182 maps)
MINIMAP_LIB_NAME = "MiniMap.Zl"             # 2017 ZL client
MINIMAP_EI_LIBS = ("FMMap.wil", "MMap.wil") # EI client: FMMap = full/overland, MMap = dungeon


@lru_cache(maxsize=1)
def _minimap_index():
    """{map stem (no ext) -> MiniMap frame index}, or {} if the dump is absent."""
    try:
        idx = {}
        with open(MINIMAP_MAP_FILE, encoding="utf-8") as f:
            for line in f:
                line = line.rstrip("\n")
                if not line:
                    continue
                parts = line.split("\t")
                if len(parts) == 2:
                    try:
                        idx[parts[0]] = int(parts[1])
                    except ValueError:
                        pass
        return idx
    except FileNotFoundError:
        return {}


@lru_cache(maxsize=1)
def _minimap_index_ei():
    """{map stem -> (lib name, frame index)} for the EI client, or {}.

    Dump produced from the EI server's Envir/MiniMap.txt (see
    Tools/maps/gen_minimap_ei.py): overland maps use FMMap.wil with
    frame = value - 1001, dungeon/field maps use MMap.wil with frame = value.
    """
    try:
        idx = {}
        with open(MINIMAP_EI_FILE, encoding="utf-8") as f:
            for line in f:
                line = line.rstrip("\n")
                if not line:
                    continue
                parts = line.split("\t")
                if len(parts) == 3:
                    try:
                        idx[parts[0]] = (parts[1], int(parts[2]))
                    except ValueError:
                        pass
        return idx
    except FileNotFoundError:
        return {}


class MiniMapSource:
    """Decodes the game's minimap library.

    Supports two client generations, auto-detected from the data dir:
      - 2017 ZL client: MiniMap.Zl (frames via System.db MapInfo.MiniMap,
        dumped by SystemDbProbe --minimap into MINIMAP_MAP_FILE).
      - EI client: FMMap.wil (overland, frame = value - 1001) and MMap.wil
        (dungeon/field, frame = value); index dumped from the EI server's
        Envir/MiniMap.txt into MINIMAP_EI_FILE.

    One instance per data dir; libraries are opened lazily.  ``frame(stem)``
    returns the minimap image for a map, or None when the map has no minimap
    or the libraries are missing.
    """

    def __init__(self, data_dir: str):
        self.data_dir = data_dir
        self._zl_lib = None          # 2017: MiniMap.Zl
        self._ei_libs = {}           # EI: name -> WilLibrary
        self._mode = None            # "zl" | "ei"

    _instances: dict = {}

    @classmethod
    def _for(cls, data_dir: str) -> "MiniMapSource":
        """Per-data-dir singleton: minimap libraries are opened at most once."""
        src = cls._instances.get(data_dir)
        if src is None:
            src = cls._instances[data_dir] = cls(data_dir)
        return src

    def _detect(self):
        if self._mode is not None:
            return self._mode
        if not self.data_dir:
            self._mode = None
            return None
        for root in (self.data_dir, os.path.join(self.data_dir, "Map Data")):
            if os.path.exists(os.path.join(root, MINIMAP_LIB_NAME)):
                self._mode = "zl"
                return self._mode
        for root in (self.data_dir, os.path.join(self.data_dir, "Map Data")):
            if os.path.exists(os.path.join(root, "MMap.wil")):
                self._mode = "ei"
                return self._mode
        self._mode = None
        return None

    def _open(self):
        mode = self._detect()
        if mode == "zl":
            if self._zl_lib is not None:
                return self._zl_lib
            for root in (self.data_dir, os.path.join(self.data_dir, "Map Data")):
                p = os.path.join(root, MINIMAP_LIB_NAME)
                if os.path.exists(p):
                    try:
                        self._zl_lib = ZlLibrary(p)
                        return self._zl_lib
                    except Exception:
                        continue
            return None
        if mode == "ei":
            for name in MINIMAP_EI_LIBS:
                if name in self._ei_libs:
                    continue
                for root in (self.data_dir, os.path.join(self.data_dir, "Map Data")):
                    p = os.path.join(root, name)
                    if os.path.exists(p):
                        try:
                            self._ei_libs[name] = WilLibrary(p)
                        except Exception:
                            pass
                        break
            return self._ei_libs or None
        return None

    def frame(self, stem: str):
        mode = self._detect()
        if mode == "zl":
            lib = self._open()
            if lib is None:
                return None
            fid = _minimap_index().get(stem)
            if fid is None:
                return None
            try:
                return lib.decode(fid)
            except Exception:
                return None
        if mode == "ei":
            entry = _minimap_index_ei().get(stem)
            if entry is None:
                return None
            libname, fid = entry
            lib = self._open().get(libname) if self._open() else None
            if lib is None:
                return None
            try:
                return lib.decode(fid)
            except Exception:
                return None
        return None

# KROrder Mapping from LibraryCore/Libraries.cs
KR_ORDER = {
    0: "tilesc",
    1: "tiles30c",
    2: "tiles5c",
    3: "smtilesc",
    4: "housesc",
    5: "cliffsc",
    6: "dungeonsc",
    7: "innersc",
    8: "furnituresc",
    9: "wallsc",
    10: "smobjectsc",
    11: "animationsc",
    12: "object1c",
    13: "object2c",

    15: "wood_tilesc",
    16: "wood_tiles30c",
    17: "wood_tiles5c",
    18: "wood_smtilesc",
    19: "wood_housesc",
    20: "wood_cliffsc",
    21: "wood_dungeonsc",
    22: "wood_innersc",
    23: "wood_furnituresc",
    24: "wood_wallsc",
    25: "wood_smobjectsc",
    26: "wood_animationsc",

    30: "sand_tilesc",
    31: "sand_tiles30c",
    32: "sand_tiles5c",
    33: "sand_smtilesc",
    34: "sand_housesc",
    35: "sand_cliffsc",
    36: "sand_dungeonsc",
    37: "sand_innersc",
    38: "sand_furnituresc",
    39: "sand_wallsc",
    40: "sand_smobjectsc",
    41: "sand_animationsc",

    45: "snow_tilesc",
    46: "snow_tiles30c",
    47: "snow_tiles5c",
    48: "snow_smtilesc",
    49: "snow_housesc",
    50: "snow_cliffsc",
    51: "snow_dungeonsc",
    52: "snow_innersc",
    53: "snow_furnituresc",
    54: "snow_wallsc",
    55: "snow_smobjectsc",
    56: "snow_animationsc",

    60: "forest_tilesc",
    61: "forest_tiles30c",
    62: "forest_tiles5c",
    63: "forest_smtilesc",
    64: "forest_housesc",
    65: "forest_cliffsc",
    66: "forest_dungeonsc",
    67: "forest_innersc",
    68: "forest_furnituresc",
    69: "forest_wallsc",
    70: "forest_smobjectsc",
    71: "forest_animationsc",
}


# ------------------------------------------------------------------- .map I/O

class MapCell:
    __slots__ = ('back_file', 'back_img', 'mid_file', 'mid_img', 'front_file', 'front_img',
                 'flag', 'anim_a', 'anim_b')

    def __init__(self):
        self.back_file = 255
        self.back_img = 0
        self.mid_file = 255
        self.mid_img = 0
        self.front_file = 255
        self.front_img = 0
        self.flag = 0
        self.anim_a = 0xFF
        self.anim_b = 0xFF


def parse_map_header(path: str) -> tuple[int, int]:
    with open(path, "rb") as f:
        hdr = f.read(28)
    w = struct.unpack_from("<H", hdr, 22)[0]
    h = struct.unpack_from("<H", hdr, 24)[0]
    return w, h


def parse_map(path: str) -> tuple[int, int, list[list[MapCell]]]:
    """Parse Zircon / Mir3 EI .map file into cell matrix Cells[Width][Height]."""
    with open(path, "rb") as f:
        data = f.read()

    w = struct.unpack_from("<H", data, 22)[0]
    h = struct.unpack_from("<H", data, 24)[0]

    cells = [[MapCell() for _ in range(h)] for _ in range(w)]

    offset = 28
    # Segment 1: Back (Ground) layer (Half-res, 3 bytes per entry for even cells)
    for x in range(w // 2):
        for y in range(h // 2):
            bf = data[offset]
            bi = struct.unpack_from("<H", data, offset + 1)[0]
            offset += 3
            cells[x * 2][y * 2].back_file = bf
            cells[x * 2][y * 2].back_img = bi

    # Segment 2: Full-res Cells (14 bytes each)
    n_cells = min(w * h, max(0, (len(data) - 28) // 14))
    for i in range(n_cells):
        x, y = divmod(i, h)
        offset = 28 + i * 14
        # cell structure:
        # 0: flag, 1: midAnim, 2: frontAnim, 3: frontFile, 4: midFile
        # 5-6: midImg (uint16), 7-8: frontImg (uint16), 9+: unused/padding
        ff = data[offset + 3]
        mf = data[offset + 4]
        mi = struct.unpack_from("<H", data, offset + 5)[0]
        fi = struct.unpack_from("<H", data, offset + 7)[0]

        c = cells[x][y]
        c.flag = data[offset]
        c.anim_a = data[offset + 1]
        c.anim_b = data[offset + 2]
        c.mid_file = mf
        c.mid_img = mi
        c.front_file = ff
        c.front_img = fi

    return w, h, cells


class MapCache:
    """LRU of parsed maps + two cell indexes.

    Index A (iso): cells bucketed by s = x + y (isometric screen row),
    within a bucket sorted by x.  Index B (rect): cells bucketed by x,
    within a bucket sorted by y — used for the axis-aligned (original)
    projection where a tile window is a plain x/y rectangle.
    """

    def __init__(self, maps_dir: str, max_keep: int = CACHE_MAPS_MAX):
        self.maps_dir = maps_dir
        self.max_keep = max_keep
        self._store: dict[str, tuple[int, int, list[list[MapCell]]]] = {}
        self._buckets: dict[str, list[list[tuple[int, MapCell]]]] = {}
        self._bxs: dict[str, list[list[int]]] = {}
        self._rows: dict[str, list[list[tuple[int, MapCell]]]] = {}
        self._rys: dict[str, list[list[int]]] = {}
        self._lock = threading.Lock()
        self._build_locks: dict[str, threading.Lock] = {}

    def _build_lock(self, name: str) -> threading.Lock:
        with self._lock:
            lk = self._build_locks.get(name)
            if lk is None:
                lk = self._build_locks[name] = threading.Lock()
            return lk

    def get(self, name: str) -> tuple[int, int, list[list[MapCell]]]:
        """Parse (once) and return (w, h, cells). Never holds the global lock
        while parsing, so concurrent tile requests are not serialized on a
        slow first parse."""
        with self._lock:
            entry = self._store.get(name)
        if entry is None:
            with self._build_lock(name):
                with self._lock:
                    entry = self._store.get(name)
                if entry is None:
                    entry = parse_map(os.path.join(self.maps_dir, name))
                    with self._lock:
                        self._store[name] = entry
                        while len(self._store) > self.max_keep:
                            k = next(iter(self._store))
                            self._store.pop(k)
                            self._buckets.pop(k, None)
                            self._bxs.pop(k, None)
                            self._rows.pop(k, None)
                            self._rys.pop(k, None)
        return self._store[name]

    def sparse(self, name: str) -> tuple[list, list]:
        """(buckets, bxs): buckets[s] = [(x, cell), ...] sorted by x, with
        parallel x-only lists for bisect. s = x + y in [0, w+h-2]."""
        with self._lock:
            buckets = self._buckets.get(name)
            bxs = self._bxs.get(name)
        if buckets is None:
            entry = self.get(name)  # ensure parsed; may block on the parse lock
            with self._build_lock(name):
                with self._lock:
                    buckets = self._buckets.get(name)
                if buckets is None:
                    w, h, cells = entry
                    buckets = [[] for _ in range(w + h - 1)]
                    for x in range(w):
                        for y in range(h):
                            c = cells[x][y]
                            if c.back_file != 255 or c.mid_file != 255 or c.front_file != 255:
                                buckets[x + y].append((x, c))
                    bxs = []
                    for b in buckets:
                        b.sort(key=lambda t: t[0])
                        bxs.append([t[0] for t in b])
                    with self._lock:
                        self._buckets[name] = buckets
                        self._bxs[name] = bxs
        return self._buckets[name], self._bxs[name]

    def sparse_rows(self, name: str) -> tuple[list, list]:
        """(rows, rys): rows[x] = [(y, cell), ...] sorted by y, with parallel
        y-only lists for bisect.  Used by the axis-aligned (rect) layout."""
        with self._lock:
            rows = self._rows.get(name)
            rys = self._rys.get(name)
        if rows is None:
            entry = self.get(name)
            with self._build_lock(name):
                with self._lock:
                    rows = self._rows.get(name)
                if rows is None:
                    w, h, cells = entry
                    rows = [[] for _ in range(w)]
                    for x in range(w):
                        for y in range(h):
                            c = cells[x][y]
                            if c.back_file != 255 or c.mid_file != 255 or c.front_file != 255:
                                rows[x].append((y, c))
                    rys = []
                    for r in rows:
                        r.sort(key=lambda t: t[0])
                        rys.append([t[0] for t in r])
                    with self._lock:
                        self._rows[name] = rows
                        self._rys[name] = rys
        return self._rows[name], self._rys[name]

    def sparse_slice(self, name: str, wx0: int, wx1: int, wy0: int, wy1: int,
                     margin: int = 512, layout: str = LAYOUT_RECT):
        """Yield (x, y, cell) for every non-empty cell whose anchor lies inside
        [wx0-margin, wx1+margin] x [wy0-margin, wy1+margin] (world px)."""
        import bisect
        w, h, _ = self.get(name)
        if layout == LAYOUT_ISO:
            buckets, bxs = self.sparse(name)
            # screen rows: cy = s*16 + 16 must intersect [wy0-margin, wy1+margin]
            s0 = max(0, (wy0 - margin - 16 + 15) // 16)
            s1 = min(len(buckets) - 1, (wy1 + margin - 16) // 16)
            # per-row screen x: cx = (2x - s)*24 + h*24 + 24
            cx_lo = wx0 - margin - h * 24 - 24
            cx_hi = wx1 + margin - h * 24 - 24
            for s in range(s0, s1 + 1):
                xs = bxs[s]
                x0 = (cx_lo + s * 24 + 47) // 48  # ceil
                x1 = (cx_hi + s * 24) // 48       # floor
                i0 = bisect.bisect_left(xs, x0)
                i1 = bisect.bisect_right(xs, x1)
                if i0 >= i1:
                    continue
                bucket = buckets[s]
                for k in range(i0, i1):
                    x, c = bucket[k]
                    yield x, s - x, c
            return

        rows, rys = self.sparse_rows(name)
        x0 = max(0, (wx0 - margin) // 48)
        x1 = min(w - 1, (wx1 + margin - 1) // 48)
        y0 = max(0, (wy0 - margin) // 32)
        y1 = min(h - 1, (wy1 + margin - 1) // 32)
        for x in range(x0, x1 + 1):
            ys = rys[x]
            i0 = bisect.bisect_left(ys, y0)
            i1 = bisect.bisect_right(ys, y1)
            if i0 >= i1:
                continue
            row = rows[x]
            for k in range(i0, i1):
                y, c = row[k]
                yield x, y, c


# ------------------------------------------------------------------ WIL pool

def _find_library_path(data_dir: str, lib_name: str) -> str | None:
    """Find a library in Data, Data/Map Data, or a terrain subdirectory."""
    parts = lib_name.split("_", 1)
    if len(parts) == 2 and parts[0] in {"wood", "sand", "snow", "forest"}:
        folder, filename = parts[0].title(), parts[1]
    else:
        folder, filename = None, lib_name
    filename_candidates = [filename + ".Zl", filename + ".zl", filename + ".wil"]
    roots = [data_dir, os.path.join(data_dir, "Map Data")]
    for root in roots:
        candidates = []
        if folder:
            candidates.append(os.path.join(root, folder))
        candidates.append(root)
        for directory in candidates:
            if not os.path.isdir(directory):
                continue
            for entry in os.listdir(directory):
                if entry.lower() in {name.lower() for name in filename_candidates}:
                    return os.path.join(directory, entry)
    return None


class FramePool:
    """Map library IDs to either legacy WIL or current Zircon ZL libraries."""

    def __init__(self, data_dir: str):
        self.libs: dict[str, WilLibrary | ZlLibrary | None] = {}
        self.lib_paths: dict[str, str] = {}  # lib_name -> resolved file path
        self.data_dir = data_dir
        self._lock = threading.RLock()
        self._frames: OrderedDict = OrderedDict()
        self._frame_bytes = 0

    def _get_lib(self, lib_id: int) -> WilLibrary | ZlLibrary | None:
        lib_name = KR_ORDER.get(lib_id)
        if not lib_name:
            return None
        with self._lock:
            if lib_name not in self.libs:
                path = _find_library_path(self.data_dir, lib_name)
                if path is None:
                    self.libs[lib_name] = None
                elif path.lower().endswith(".zl"):
                    self.libs[lib_name] = ZlLibrary(path)
                    self.lib_paths[lib_name] = path
                else:
                    self.libs[lib_name] = WilLibrary(path)
                    self.lib_paths[lib_name] = path
            return self.libs[lib_name]

    def decode(self, lib_id: int, frame: int, scale: int = 1):
        """Returns (PIL.Image at 1/scale resolution, offsetX, offsetY) or None.

        scale > 1 decodes WIL frames natively at 1/scale (no full-res pass);
        ZL frames are decoded at 1:1 then NEAREST-downscaled (PNG decode is
        C-speed so the win there is cache memory).  Byte-budget LRU: the same
        frame is never re-decoded while its tile is on screen."""
        lib = self._get_lib(lib_id)
        if lib is None or frame < 0 or frame >= lib.count:
            return None
        try:
            hdr = lib.header(frame)
        except Exception:
            return None
        if hdr is None or hdr["width"] <= 0 or hdr["height"] <= 0:
            return None
        key = (lib_id, frame, scale)
        with self._lock:
            img = self._frames.get(key)
            if img is not None:
                self._frames.move_to_end(key)
        if img is None:
            try:
                if scale > 1 and hasattr(lib, "decode_scaled"):
                    im = lib.decode_scaled(frame, scale)
                else:
                    im = lib.decode(frame)
                    if im is not None and scale > 1:
                        im = im.resize((max(1, im.width // scale),
                                        max(1, im.height // scale)), Image.NEAREST)
            except Exception:
                return None
            if im is None:
                return None
            img = (im, hdr["offsetX"], hdr["offsetY"])
            with self._lock:
                self._frames[key] = img
                self._frames.move_to_end(key)
                budget = im.width * im.height * 4 + 64
                self._frame_bytes += budget
                while self._frame_bytes > CACHE_FRAMES_BYTES and len(self._frames) > 1:
                    _, evicted = self._frames.popitem(last=False)
                    self._frame_bytes -= evicted[0].width * evicted[0].height * 4 + 64
        return img


# ------------------------------------------------------------------ parallel full-map decode

# Process-pool worker state: per-worker library cache + the data dir the
# worker was initialised with.  ZL BC1 decode is pure-Python (~2.7ms/frame),
# so full-map renders of big maps (00.map z3 needs ~23k unique frames)
# parallelise decode across cores; compositing stays single-process in
# painter order.
_POOL: dict[str, ProcessPoolExecutor] = {}
_POOL_MU = threading.Lock()
_WORKER_DATA_DIR: str | None = None
_WORKER_LIBS: dict[str, WilLibrary | ZlLibrary] = {}


def _init_worker(data_dir: str):
    global _WORKER_DATA_DIR
    _WORKER_DATA_DIR = data_dir


def _decode_frame_worker(args: tuple) -> tuple | None:
    """Decode (lib_id, frame, scale) in a pool worker -> sprite payload.

    Returns (lib_id, frame, w, h, offsetX, offsetY, PNG bytes) or None (invalid /
    empty frame).  Each worker opens each library once and keeps it for the
    process lifetime (mmap shares the OS page cache with the parent)."""
    lib_id, frame, scale = args
    lib_name = KR_ORDER.get(lib_id)
    if not lib_name:
        return None
    lib = _WORKER_LIBS.get(lib_name)
    if lib is None:
        path = _find_library_path(_WORKER_DATA_DIR, lib_name)
        if path is None:
            return None
        lib = (ZlLibrary(path) if path.lower().endswith(".zl") else WilLibrary(path))
        _WORKER_LIBS[lib_name] = lib
    try:
        hdr = lib.header(frame)
        if hdr is None or hdr["width"] <= 0 or hdr["height"] <= 0:
            return None
        if scale > 1 and hasattr(lib, "decode_scaled"):
            im = lib.decode_scaled(frame, scale)
        else:
            im = lib.decode(frame)
            if im is not None and scale > 1:
                im = im.resize((max(1, im.width // scale),
                                max(1, im.height // scale)), Image.NEAREST)
        if im is None:
            return None
        return (lib_id, frame, im.width, im.height, hdr["offsetX"], hdr["offsetY"],
                im.tobytes())
    except Exception:
        return None


def _get_pool(data_dir: str) -> ProcessPoolExecutor:
    with _POOL_MU:
        pool = _POOL.get(data_dir)
        if pool is None:
            pool = _POOL[data_dir] = ProcessPoolExecutor(
                max_workers=min(10, os.cpu_count() or 2),
                initializer=_init_worker, initargs=(data_dir,))
        return pool


# ------------------------------------------------------------------ geometry

def world_bounds(w: int, h: int, layout: str = LAYOUT_RECT) -> tuple[int, int]:
    """Full assembled map size in world pixels."""
    if layout == LAYOUT_ISO:
        return (w + h + 3) * 24, (w + h + 2) * 16
    return w * 48, h * 32


def cell_anchor(x: int, y: int, h: int, layout: str = LAYOUT_RECT) -> tuple[int, int]:
    """World-pixel position of cell (x,y): its top-left corner (rect,
    matching Mir3.exe's (x-view.x)*48 / (y-view.y)*32) or its centre (iso)."""
    if layout == LAYOUT_ISO:
        return (x - y) * 24 + h * 24 + 24, (x + y) * 16 + 16
    return x * 48, y * 32


def map_ladder(w: int, h: int, layout: str = LAYOUT_RECT) -> list[int]:
    """Full-map static zoom ladder: [deepest, ..., fit] as zoom levels
    (0 = 1:1).  Deepest keeps the whole map within MAX_FULL_DIM px on its
    longest side (a single image is feasible); fit is the default overview
    (~FIT_FULL_DIM px).  A full 1:1 image of e.g. 00.map (1360x1500 cells,
    68k x 46k world px) is physically impossible, hence the cap."""
    max_dim = max(world_bounds(w, h, layout))
    deep_z = 0
    while (max_dim >> deep_z) > MAX_FULL_DIM:
        deep_z += 1
    fit_z = deep_z
    while (max_dim >> (fit_z + 1)) >= FIT_FULL_DIM:
        fit_z += 1
    return list(range(deep_z, fit_z + 1))


# ------------------------------------------------------------------ renderer

def is_object_library(lib_id: int) -> bool:
    """True if lib_id refers to an object/building library (Houses, Walls, SmTiles, Objects, etc).

    Excludes empty (255) and pure ground tile libraries (tilesc, tiles30c, tiles5c, wood_tilesc).
    """
    if lib_id == 255:
        return False
    lib_name = KR_ORDER.get(lib_id, "")
    if not lib_name:
        return False
    # Only pure ground tiles should be excluded from object rendering; smtilesc contains houses/stairs!
    if lib_name in ("tilesc", "tiles30c", "tiles5c", "wood_tilesc", "tiles"):
        return False
    return True


def render_tile(map_cache: MapCache, pool: FramePool, map_name: str,
                tx: int, ty: int, zoom: int,
                draw_ground: bool = True, draw_mid: bool = True,
                draw_front: bool = True,
                layout: str = LAYOUT_RECT,
                offset_mode: str = OFFSET_NONE) -> bytes:
    """Render a single tile at zoom level `zoom` (0 is 1:1, 1 is 1:2, etc).

    `offset_mode` (rect only) selects the WIL frame-offset experiment mode;
    OFFSET_NONE is the original Mir3.exe behaviour (no offsets)."""
    scale = 1 << zoom
    tile_world_sz = TILE_SZ * scale
    w, h, _ = map_cache.get(map_name)

    wx0, wy0 = tx * tile_world_sz, ty * tile_world_sz
    wx1, wy1 = wx0 + tile_world_sz, wy0 + tile_world_sz

    canvas = Image.new("RGBA", (TILE_SZ, TILE_SZ), (16, 16, 20, 255))

    cells = map_cache.sparse_slice(map_name, wx0, wx1, wy0, wy1, layout=layout)

    for x, y, cell in cells:
        cx, cy = cell_anchor(x, y, h, layout)

        if cx + 512 < wx0 or cx - 512 > wx1 or cy + 512 < wy0 or cy - 512 > wy1:
            continue

        # 1. Back Ground Layer.  Mir3.exe 0x43b9a0 anchors ground blocks at
        # the cell top-left (rect: x*48, y*32) and never reads WIL offsets.
        # .map ground storage only fills even cells (2x2 blocks), so in the
        # rect layout one 96x64 block exactly covers cells (x..x+1, y..y+1).
        # (The iso view keeps the legacy centre-anchor + offset behaviour.)
        if draw_ground and cell.back_file != 255 and cell.back_img >= 0:
            got = pool.decode(cell.back_file, cell.back_img, scale)
            if got is not None:
                if layout == LAYOUT_ISO:
                    img, off_x, off_y = got
                    px = cx - 24 + off_x
                    py = cy - 16 + off_y
                else:
                    img, off_x, off_y = got
                    if offset_mode == OFFSET_ALL:
                        px = cx + off_x * scale
                        py = cy + off_y * scale
                    else:
                        px, py = cx, cy
                iw, ih = img.width * scale, img.height * scale
                if px + iw >= wx0 and px <= wx1 and py + ih >= wy0 and py <= wy1:
                    canvas.alpha_composite(img, ((px - wx0) // scale, (py - wy0) // scale))

        # 2. Middle Layer (SmTiles, SmObjects, Furnitures, etc)
        # Mir3.exe anchors mid/front sprites bottom-LEFT to the cell and
        # never reads the WIL frame offset (dest math at 0x43bce6/0x43bfd2:
        # destX = (x-viewX)*48 - scrollX - 200, destY = (y-viewY)*32 -
        # scrollY - h - 125; the ground's -157 vs mid/front's -125 differ by
        # exactly one cell height, so the frame bottom sits on the cell's
        # bottom edge).  The ZL C# client (MapControl.cs DrawObjects) does the
        # same: Draw(index, drawX, drawY - h, useOffSet=false) with drawX =
        # cell left and drawY = cell bottom.  Some libs (e.g. SmTilesc) carry
        # garbage offsets (-1132, -19694) that would fling sprites off-map.
        # `offset_mode` switches those offsets on for the experiment only.
        #
        # Frame index semantics: the .map file stores the raw WIL frame index
        # (Mir3.exe 0x43b3c7 pushes cell+5 verbatim; the 2017 ZL client reads
        # +1 and draws -1, netting to the raw value).  No -1 here.
        if draw_mid and is_object_library(cell.mid_file) and cell.mid_img > 0 and cell.mid_img < 65535:
            frame_idx = cell.mid_img
            got = pool.decode(cell.mid_file, frame_idx, scale)
            if got is not None:
                img, off_x, off_y = got
                if layout == LAYOUT_ISO:
                    px = cx - 24
                    py = cy + 16 - img.height * scale
                elif offset_mode in (OFFSET_ALL, OFFSET_MIDFRONT):
                    px = cx + off_x * scale
                    py = cy + 32 - img.height * scale + off_y * scale
                else:
                    px = cx
                    py = cy + 32 - img.height * scale
                iw, ih = img.width * scale, img.height * scale
                if px + iw >= wx0 and px <= wx1 and py + ih >= wy0 and py <= wy1:
                    canvas.alpha_composite(img, ((px - wx0) // scale, (py - wy0) // scale))

        # 3. Front Layer (Houses, Walls, Cliffs, Objects, etc)
        if draw_front and is_object_library(cell.front_file) and cell.front_img > 0 and cell.front_img < 65535:
            frame_idx = cell.front_img
            got = pool.decode(cell.front_file, frame_idx, scale)
            if got is not None:
                img, off_x, off_y = got
                if layout == LAYOUT_ISO:
                    px = cx - 24
                    py = cy + 16 - img.height * scale
                elif offset_mode in (OFFSET_ALL, OFFSET_MIDFRONT):
                    px = cx + off_x * scale
                    py = cy + 32 - img.height * scale + off_y * scale
                else:
                    px = cx
                    py = cy + 32 - img.height * scale
                iw, ih = img.width * scale, img.height * scale
                if px + iw >= wx0 and px <= wx1 and py + ih >= wy0 and py <= wy1:
                    canvas.alpha_composite(img, ((px - wx0) // scale, (py - wy0) // scale))

    buf = io.BytesIO()
    if zoom == 0:
        canvas.save(buf, format="PNG")
    else:
        canvas.convert("RGB").save(buf, format="JPEG", quality=75)
    return buf.getvalue()


LIB_IDS = {name: lid for lid, name in KR_ORDER.items()}
PARALLEL_MIN_FRAMES = 200  # unique frames above which full-map decode uses the process pool


def render_full_map(map_cache: MapCache, pool: FramePool, map_name: str, z: int,
                    draw_ground: bool = True, draw_mid: bool = True,
                    draw_front: bool = True,
                    fmt: str = "JPEG", layout: str = LAYOUT_RECT,
                    offset_mode: str = OFFSET_NONE) -> bytes:
    scale = 1 << z
    w, h, _ = map_cache.get(map_name)
    world_w, world_h = world_bounds(w, h, layout)
    W, H = math.ceil(world_w / scale), math.ceil(world_h / scale)

    needs: dict[int, set[int]] = {}
    cells = list(map_cache.sparse_slice(map_name, 0, world_w, 0, world_h, layout=layout))
    for _, _, cell in cells:
        if draw_ground and cell.back_file != 255 and cell.back_img >= 0:
            needs.setdefault(cell.back_file, set()).add(cell.back_img)
        if draw_mid and is_object_library(cell.mid_file) and cell.mid_img > 0 and cell.mid_img < 65535:
            needs.setdefault(cell.mid_file, set()).add(cell.mid_img)
        if draw_front and is_object_library(cell.front_file) and cell.front_img > 0 and cell.front_img < 65535:
            needs.setdefault(cell.front_file, set()).add(cell.front_img)

    tasks: list[tuple] = []
    for lib_id, frames in needs.items():
        lib = pool._get_lib(lib_id)
        if lib is None:
            continue
        for fr in frames:
            if 0 <= fr < lib.count:
                tasks.append((lib_id, fr, scale))

    sprites: dict[tuple[int, int], tuple] = {}
    if len(tasks) >= PARALLEL_MIN_FRAMES:
        for res in _get_pool(pool.data_dir).map(_decode_frame_worker, tasks):
            if res is None:
                continue
            lib_id, fr, iw, ih, off_x, off_y, rgba = res
            img = Image.frombuffer("RGBA", (iw, ih), rgba, "raw", "RGBA", 0, 1)
            sprites[(lib_id, fr)] = (img, off_x, off_y, _sprite_opaque(img, lib_id))
    else:
        for lib_id, frames in needs.items():
            for fr in frames:
                got = pool.decode(lib_id, fr, scale)
                if got is not None:
                    img, off_x, off_y = got
                    sprites[(lib_id, fr)] = (img, off_x, off_y, _sprite_opaque(img, lib_id))

    canvas = Image.new("RGBA", (W, H), (16, 16, 20, 255))
    for x, y, cell in cells:
        cx, cy = cell_anchor(x, y, h, layout)
        # 1. Back Ground Layer
        if draw_ground and cell.back_file != 255 and cell.back_img >= 0:
            got = sprites.get((cell.back_file, cell.back_img))
            if got is not None:
                img, off_x, off_y, opaque = got
                if layout == LAYOUT_ISO:
                    _blit(canvas, img, cx - 24 + off_x, cy - 16 + off_y, scale, opaque)
                elif offset_mode == OFFSET_ALL:
                    _blit(canvas, img, cx + off_x * scale, cy + off_y * scale, scale, opaque)
                else:
                    _blit(canvas, img, cx, cy, scale, opaque)
        # 2. Middle Layer
        if draw_mid and is_object_library(cell.mid_file) and cell.mid_img > 0 and cell.mid_img < 65535:
            got = sprites.get((cell.mid_file, cell.mid_img))
            if got is not None:
                img, off_x, off_y, opaque = got
                if layout == LAYOUT_ISO:
                    _blit(canvas, img, cx - 24, cy + 16 - img.height * scale, scale, False)
                elif offset_mode in (OFFSET_ALL, OFFSET_MIDFRONT):
                    _blit(canvas, img, cx + off_x * scale,
                          cy + 32 - img.height * scale + off_y * scale, scale, False)
                else:
                    _blit(canvas, img, cx, cy + 32 - img.height * scale, scale, False)
        # 3. Front Layer
        if draw_front and is_object_library(cell.front_file) and cell.front_img > 0 and cell.front_img < 65535:
            got = sprites.get((cell.front_file, cell.front_img))
            if got is not None:
                img, off_x, off_y, opaque = got
                if layout == LAYOUT_ISO:
                    _blit(canvas, img, cx - 24, cy + 16 - img.height * scale, scale, False)
                elif offset_mode in (OFFSET_ALL, OFFSET_MIDFRONT):
                    _blit(canvas, img, cx + off_x * scale,
                          cy + 32 - img.height * scale + off_y * scale, scale, False)
                else:
                    _blit(canvas, img, cx, cy + 32 - img.height * scale, scale, False)

    buf = io.BytesIO()
    if fmt == "PNG":
        canvas.convert("RGB").save(buf, format="PNG")
    else:
        canvas.convert("RGB").save(buf, format="JPEG", quality=78)
    return buf.getvalue()


def render_offset_strip(map_cache: MapCache, pool: FramePool, map_name: str, z: int,
                        draw_ground: bool = True, draw_mid: bool = True,
                        draw_front: bool = True,
                        layout: str = LAYOUT_RECT) -> bytes:
    """Side-by-side PNG of the three offset experiment modes (none|all|midfront).

    Panels are full-map renders at zoom z, downscaled to height 400 with
    labelled bars — same layout as the offline comparisons/*__offset_modes_z4.png
    strips, so /strip (sim "导出对比图" button) and the offline generator share
    one code path."""
    from PIL import Image, ImageDraw

    labels = {
        OFFSET_NONE: "none (Mir3.exe)",
        OFFSET_ALL: "all (back+mid/front)",
        OFFSET_MIDFRONT: "midfront",
    }
    panels = {}
    for om in (OFFSET_NONE, OFFSET_ALL, OFFSET_MIDFRONT):
        buf = render_full_map(map_cache, pool, map_name, z, draw_ground, draw_mid,
                              draw_front, fmt="PNG", layout=layout, offset_mode=om)
        im = Image.open(io.BytesIO(buf)).convert("RGB")
        im.thumbnail((10_000, 400), Image.LANCZOS)
        panels[om] = im
    w = max(i.width for i in panels.values())
    h = max(i.height for i in panels.values())
    gap, bar = 8, 26
    strip = Image.new("RGB", (3 * w + 2 * gap, bar + h), (20, 20, 26))
    d = ImageDraw.Draw(strip)
    for k, om in enumerate((OFFSET_NONE, OFFSET_ALL, OFFSET_MIDFRONT)):
        x = k * (w + gap)
        d.rectangle([x, 0, x + w, bar], fill=(32, 32, 44))
        d.text((x + 6, 8), f"{map_name}  offset={labels[om]}", fill=(240, 240, 240))
        strip.paste(panels[om], (x, bar))
    buf = io.BytesIO()
    strip.save(buf, format="PNG", optimize=True)
    return buf.getvalue()


def _sprite_opaque(img: Image.Image, lib_id: int = None) -> bool:
    """True when the sprite has no transparent pixels (paste == composite).

    Ground libraries are always fully opaque.  This matters for ZL data:
    the ZL toolchain stores Wood/Tilesc.Zl and Wood/Tiles5c.Zl BC3 alpha as
    4 (placeholder) instead of 255, while the ZL client never consumes those
    libs per-tile (it draws ground from MapInfo.Background) — so the
    placeholder never surfaced there.  In our per-tile ground renderer a
    composite with alpha=4 would make the whole ground layer vanish, so
    treat every ground frame as opaque regardless of its stored alpha.
    """
    lib_name = KR_ORDER.get(lib_id, "") if lib_id is not None else ""
    if lib_name in ("tilesc", "tiles30c", "tiles5c", "wood_tilesc", "tiles"):
        return True
    try:
        return img.getextrema()[3] == (255, 255)
    except Exception:
        return False


def _blit(canvas: Image.Image, img: Image.Image, px: int, py: int, scale: int,
          opaque: bool = False):
    """Draw `img` onto `canvas` at world (px, py) clipped to the canvas edge.

    Opaque sprites (all ground tiles, most walls) use paste - ~4x cheaper
    than alpha_composite; translucent sprites alpha-composite in painter
    order.  Sprites anchored left/up of the cell are cropped."""
    W, H = canvas.width, canvas.height
    sx, sy = px // scale, py // scale
    iw, ih = img.width, img.height
    if sy < 0:
        top = min(ih, -sy)
        img = img.crop((0, top, iw, ih)); ih = img.height; sy = 0
    if sx < 0:
        left = min(iw, -sx)
        img = img.crop((left, 0, iw, ih)); iw = img.width; sx = 0
    if sx >= W or sy >= H:
        return
    iw = min(iw, W - sx); ih = min(ih, H - sy)
    if iw <= 0 or ih <= 0:
        return
    if iw < img.width or ih < img.height:
        img = img.crop((0, 0, iw, ih))
    if opaque:
        canvas.paste(img, (sx, sy))
    else:
        canvas.alpha_composite(img, (sx, sy))


# ------------------------------------------------------------------ web server

SIM_TEMPLATE = """<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>Mir3 EI 原版 800×600 模拟器</title>
<style>
    html, body { margin:0; padding:0; background:#222; overflow:hidden; font-family:sans-serif; }
    #stage { position:relative; width:800px; height:600px; margin:0 auto; background:#000;
             box-shadow:0 0 30px rgba(0,0,0,.8); overflow:hidden; }
    /* world view: the full-map render is positioned so the requested cell is centered */
    #world { position:absolute; left:0; top:0; width:800px; height:600px; overflow:hidden; }
    #world img { position:absolute; display:block; image-rendering:pixelated; }
    /* original HUD bottom bar (0,465)-(800,600) — semi-transparent over the world */
    #hud { position:absolute; left:0; top:465px; width:800px; height:135px;
           background:rgba(12,12,16,.88); border-top:2px solid #4a3a1a; box-sizing:border-box; }
    #hud .cell-label { position:absolute; left:10px; top:6px; font-size:12px; color:#d8c890;
                       font-family:ui-monospace,monospace; text-shadow:1px 1px 0 #000; }
    #hud .stats { position:absolute; left:10px; top:26px; font-size:11px; color:#9aa;
                  font-family:ui-monospace,monospace; line-height:1.5; }
    #hud .cell-nav { position:absolute; left:10px; top:88px; font-size:11px; color:#887; }
    #hud .cell-nav b { color:#dd8; cursor:pointer; }
    #hud .cell-nav b:hover { color:#ffd; }
    #hud .oob { position:absolute; right:10px; top:6px; font-size:11px; color:#f86; font-family:ui-monospace,monospace; }
    /* original minimap widget: fixed (672,0)-(800,128) */
    #mm { position:absolute; left:672px; top:0; width:128px; height:128px;
          background:rgba(8,10,14,.85); border-left:1px solid #3a3a46; border-bottom:1px solid #3a3a46;
          box-sizing:border-box; }
    #mm img { width:128px; height:128px; image-rendering:pixelated; display:block; }
    #mm .mm-label { position:absolute; left:3px; top:2px; font-size:10px; color:#ffe; opacity:.8;
                    text-shadow:1px 1px 0 #000; }
    #mm .mm-zoom { position:absolute; right:3px; bottom:2px; font-size:10px; color:#ffe; opacity:.9; }
    /* HUD buttons from the static evidence (GameInter.wil frames at hud.left+offset) */
    .hud-btn { position:absolute; background:rgba(60,50,30,.55); border:1px solid #6a5a30;
               box-sizing:border-box; }
    /* entity layer: sprites + name tags, positioned over the world view */
    #ents { position:absolute; left:0; top:0; width:800px; height:600px; pointer-events:none; }
    .ent { position:absolute; transform:translate(-50%,-100%); pointer-events:auto;
           cursor:pointer; }
    .ent img { display:block; image-rendering:pixelated; filter:drop-shadow(2px 2px 0 rgba(0,0,0,.5)); }
    .ent .tag { position:absolute; left:50%; top:100%; transform:translateX(-50%);
                font-size:10px; color:#fff; background:rgba(0,0,0,.55); border:1px solid #555;
                padding:0 3px; white-space:nowrap; border-radius:2px; pointer-events:none; }
    .ent.npc img { border:1px solid rgba(120,200,120,.35); }
    .ent.monster img { border:1px solid rgba(230,80,60,.4); }
    .ent.player img { border:1px solid rgba(120,180,255,.6); }
    .ent.player .tag { color:#8cf; border-color:#46a; }
    .ent:hover .tag { background:rgba(255,220,90,.92); color:#000; }
    .ent.target { outline:2px solid #ffd23d; outline-offset:1px; }
    .ent .info { display:none; position:absolute; left:50%; bottom:calc(100% + 6px);
                 transform:translateX(-50%); background:rgba(10,12,18,.94); color:#ddd;
                 border:1px solid #6a5a30; border-radius:3px; padding:5px 8px;
                 font-size:11px; min-width:150px; white-space:nowrap; z-index:5; }
    .ent:hover .info { display:block; }
    #mm .mm-box { position:absolute; border:1px solid #ffd23d; background:rgba(255,210,61,.25);
                  box-sizing:border-box; }
    /* title bar / toolbar outside the stage */
    #bar { width:800px; margin:6px auto 4px; display:flex; gap:8px; align-items:center;
           color:#ccc; font-size:12px; }
    #bar select { background:#333; color:#eee; border:1px solid #555; border-radius:3px; padding:2px 6px; }
    #bar label { cursor:pointer; }
    #bar button { background:#333; color:#eee; border:1px solid #555; border-radius:3px; cursor:pointer; }
    #tip { width:800px; margin:0 auto; font-size:11px; color:#777; font-family:ui-monospace,monospace; }
</style>
</head>
<body>
<div id="bar">
    <span>地图:</span><select id="sel-map"></select>
    <img id="map-thumb" alt="" style="height:22px;border:1px solid #555;border-radius:2px;display:none;">
    <span>中心格:</span><span id="sel-cell" style="font-family:ui-monospace,monospace;">—</span>
    <label><input type="checkbox" id="chk-g" checked> Back</label>
    <label><input type="checkbox" id="chk-m" checked> Middle</label>
    <label><input type="checkbox" id="chk-f" checked> Front</label>
    <span>offset:</span><select id="sel-off" title="WIL 帧 offset 实验模式；原版 Mir3.exe 地图层从不读取 offset（none）">
        <option value="none" selected>none 原版</option>
        <option value="all">all 全层</option>
        <option value="midfront">mid/front</option>
    </select>
    <button id="btn-strip" title="导出三模式 offset 对比条带 PNG（新标签页）">导出对比图</button>
    <button id="btn-hud" title="切换原版 HUD 显示">HUD 开/关</button>
    <button id="btn-mm" title="T 键 128/256 小地图">小地图 128/256</button>
    <button id="btn-back">← 返回浏览器</button>
</div>
<div id="stage">
    <div id="world"><img id="wimg" alt=""></div>
    <div id="ents"></div>
    <div id="cell-info" style="display:none;position:absolute;z-index:50;pointer-events:none;
        background:rgba(10,10,14,.92);border:1px solid #666;border-radius:4px;
        color:#d8e6ff;font:11px ui-monospace,monospace;padding:5px 7px;white-space:pre;"></div>
    <div id="mm"><img id="mimg" alt=""><span class="mm-label" id="mm-name"></span>
        <span class="mm-zoom" id="mm-zoom">128</span><div class="mm-box" id="mm-box"></div></div>
    <div id="hud">
        <div class="cell-label" id="hud-map">—</div>
        <div class="stats" id="hud-stats"></div>
        <div class="cell-nav">方向键/WASD 移动 1 格 · <b>←</b> <b>↑</b> <b>↓</b> <b>→</b> 箭头移动 · Ctrl+滚轮缩放 · T 切换小地图 · H 切换 HUD</div>
        <div class="oob" id="hud-oob"></div>
    </div>
</div>
<div id="tip">Mir3 EI 原版 800×600 模拟 · 世界画面 rect 投影 · 小地图固定 (672,0)-(800,128) · 底部 HUD (0,465)-(800,600)（原版静态证据 layout.json）</div>
<script>
const stage = document.getElementById("stage");
const wimg = document.getElementById("wimg");
const mimg = document.getElementById("mimg");
const selMap = document.getElementById("sel-map");
const selOff = document.getElementById("sel-off");
const selCell = document.getElementById("sel-cell");
const hudMap = document.getElementById("hud-map");
const hudStats = document.getElementById("hud-stats");
const hudOob = document.getElementById("hud-oob");
const mmName = document.getElementById("mm-name");
const mmZoom = document.getElementById("mm-zoom");
let maps = [], curName = null, cat = null;
let cx = 0, cy = 0, z = 0;            // center cell + zoom ladder level
let mm = 128;                          // minimap surface: 128 or 256 (T key)
let showHud = true;
let ents = [], target = null;          // entities on the current map; clicked target
let player = { x: -1, y: -1 };         // player cell (spawn point when available)

const entStyle = {
    player:   { src: "/sprite?lib=M-Hum.wil&frame=0&scale=1", label: "我" },
    npc:      { src: "/sprite?lib=NPC.wil&frame=0&scale=1",    label: "" },
    monster:  { src: "/sprite?lib=Mon-1.wil&frame=0&scale=1", label: "" },
};

async function init() {
    const res = await fetch("/api/maps");
    maps = await res.json();
    selMap.innerHTML = maps.map(m =>
        `<option value="${m.name.replace(/"/g, "&quot;")}">${(m.cn || "") + " " + m.name}</option>`).join("");
    // hash: #sim=3.map&c=200,300&z=2
    let target = maps[0] && maps[0].name;
    const h = location.hash.match(/sim=([^&]+)/);
    if (h && maps.some(m => m.name === decodeURIComponent(h[1]))) target = decodeURIComponent(h[1]);
    selMap.value = target;
    const cm = location.hash.match(/c=(\\d+),(\\d+)/);
    if (cm) { cx = +cm[1]; cy = +cm[2]; }
    const zm = location.hash.match(/z=(\\d+)/);
    if (zm) z = +zm[1];
    const om = location.hash.match(/om=([a-z]+)/);
    if (om && ["none", "all", "midfront"].includes(om[1])) selOff.value = om[1];
    pick(target);
}
function pick(name) {
    curName = name;
    const mi = maps.find(m => m.name === name);
    if (!mi) return;
    // default center: map middle; player: spawn point if this map has one
    if (!location.hash.match(/c=/)) { cx = Math.floor(mi.w / 2); cy = Math.floor(mi.h / 2); }
    const maxZ = mi.ladder.length - 1;
    const minZ = mi.ladder[0] ?? 0;   // server clamps z up to ladder[0]; client must match
    z = Math.min(Math.max(z, minZ), maxZ);
    loadEntities(mi);
    loadCat(mi);
    loadThumb();
    loadImg();
    loadMini();
    updateHash();
}
async function loadEntities(mi) {
    ents = []; target = null;
    const box = document.getElementById("ents");
    box.innerHTML = "";
    try {
        const res = await fetch("/api/entities?map=" + encodeURIComponent(mi.name));
        const d = await res.json();
        if (d.ok) ents = d.entities;
    } catch (e) { ents = []; }
    const spawn = ents.find(e => e.kind === "spawn");
    if (spawn) player = { x: spawn.x, y: spawn.y };
    else player = { x: Math.floor(mi.w / 2), y: Math.floor(mi.h / 2) };
    renderEnts();
}
function renderEnts() {
    const mi = maps.find(m => m.name === curName);
    const box = document.getElementById("ents");
    box.innerHTML = "";
    const s = 1 << z;
    const cxw = cx * 48 + 24, cyw = cy * 32 + 16;
    // visible filter: cell within screen + margin
    const visX = 800 / 48 * s + 2, visY = 600 / 32 * s + 2;
    const all = [{ x: player.x, y: player.y, kind: "player", name: "玩家", info: "" }];
    for (const e of ents) {
        if (e.kind === "spawn") continue;   // spawn point is the player start, not an entity
        all.push(e);
    }
    for (const e of all) {
        if (Math.abs(e.x - cx) > visX || Math.abs(e.y - cy) > visY) continue;
        const st = entStyle[e.kind];
        const div = document.createElement("div");
        div.className = "ent " + e.kind;
        if (target && target.x === e.x && target.y === e.y && target.kind === e.kind) div.classList.add("target");
        div.style.left = (400 + (e.x * 48 + 24 - cxw) / s) + "px";
        div.style.top = (300 + (e.y * 32 + 16 - cyw) / s) + "px";
        const img = document.createElement("img");
        img.src = st.src; img.alt = "";
        div.appendChild(img);
        const tag = document.createElement("div");
        tag.className = "tag";
        tag.textContent = (e.kind === "player" ? "我" : (e.name || e.kind));
        div.appendChild(tag);
        const info = document.createElement("div");
        info.className = "info";
        let t = e.kind === "player" ? "玩家" : (e.kind === "npc" ? "NPC" : "怪物");
        info.innerHTML = `<b>${t}</b> ${e.name || ""}<br>格 ${e.x},${e.y}${e.kind === "npc" ? " · face " + (e.face ?? 0) + " · body " + (e.body ?? 0) : ""}${e.kind === "monster" ? " · Lv " + (e.level ?? 0) + (e.count && e.count > 1 ? " · ×" + e.count : "") : ""}`;
        if (e.kind === "monster" && Array.isArray(e.drops) && e.drops.length) {
            const fmtCh = (d) => (d.chance < 1 ? "1/" + Math.round(1 / d.chance) : "1/1");
            const top = e.drops.slice(0, 5).map(d => `${d.item}${d.count > 1 ? "×" + d.count : ""}(${fmtCh(d)})`).join(" ");
            info.innerHTML += `<br><span style="color:#ffd27f;">掉落: ${top}${e.drops.length > 5 ? " …" : ""}</span>`;
        }
        div.appendChild(info);
        div.addEventListener("click", () => {
            target = { x: e.x, y: e.y, kind: e.kind, name: e.name };
            renderEnts(); renderHud(); renderMiniBox();
        });
        box.appendChild(div);
    }
    renderMiniBox();
}
function renderMiniBox() {
    const mi = maps.find(m => m.name === curName);
    const bb = document.getElementById("mm-box");
    if (!mi) { bb.style.display = "none"; return; }
    const size = 128;   // fixed display size; `mm` = render surface (128/256)
    const bw = Math.max(3, 128 / mi.w * size), bh = Math.max(3, 128 / mi.h * size);
    const px = player.x / mi.w * size, py = player.y / mi.h * size;
    bb.style.display = "block";
    bb.style.width = bw + "px"; bb.style.height = bh + "px";
    bb.style.left = (px - bw / 2) + "px"; bb.style.top = (py - bh / 2) + "px";
}
async function loadCat(mi) {
    try {
        const res = await fetch("/api/catalog?map=" + encodeURIComponent(mi.name));
        const d = await res.json();
        cat = d.ok ? d.catalog : null;
    } catch (e) { cat = null; }
    renderHud();
}
function loadImg() {
    const mi = maps.find(m => m.name === curName);
    if (!mi) return;
    const s = 1 << z;
    const onload = () => {
        // center world px on stage center (400, 300)
        const cxw = cx * 48 + 24, cyw = cy * 32 + 16;   // rect anchor
        const left = 400 - cxw / s, top = 300 - cyw / s;
        wimg.style.left = left + "px";
        wimg.style.top = top + "px";
        renderHud();
    };
    wimg.onload = onload;
    wimg.src = "/fullmap?map=" + encodeURIComponent(mi.name) + "&z=" + z +
               "&g=" + (document.getElementById("chk-g").checked ? 1 : 0) +
               "&m=" + (document.getElementById("chk-m").checked ? 1 : 0) +
               "&f=" + (document.getElementById("chk-f").checked ? 1 : 0) +
               "&om=" + selOff.value;
}
function loadMini() {
    const mi = maps.find(m => m.name === curName);
    if (!mi) return;
    mimg.src = "/minimap?map=" + encodeURIComponent(mi.name);
    mmName.textContent = (mi.cn || "") + " " + mi.name;
}
function renderHud() {
    const mi = maps.find(m => m.name === curName);
    if (!mi) return;
    hudMap.textContent = (mi.cn || "") + " " + mi.name + " | 中心格 " + cx + "," + cy + " | 缩放 1:" + (1 << z);
    selCell.textContent = cx + "," + cy;
    if (cat) {
        const ev = (cat.evidence && cat.evidence.level) || "derived";
        let s = `主题 ${cat.theme_name || "base"} · ${cat.w}×${cat.h} · ${cat.cell_bytes}B/格 · 证据 ${ev}`;
        const gl = Object.keys(cat.ground || {}).length;
        const ml = Object.keys(cat.mid || {}).length;
        const fl = Object.keys(cat.front || {}).length;
        s += ` · 库 g${gl}/m${ml}/f${fl}`;
        if (cat.animated_cells) s += " · 动画格 " + cat.animated_cells;
        if (target) s += ` · 目标: ${target.kind === "npc" ? "NPC" : target.kind === "monster" ? "怪物" : "玩家"} ${target.name || ""} @${target.x},${target.y}`;
        hudStats.textContent = s;
        hudOob.textContent = cat.anomaly_total ? `⚠ ${cat.anomaly_total} 帧越界 (${Object.keys(cat.anomalies || {}).length} 项)` : "";
    } else {
        hudStats.textContent = "（无 catalog 数据）";
        hudOob.textContent = "";
    }
    const scale = mm === 128 ? "128" : "256";
    mmZoom.textContent = scale;
    document.getElementById("hud").style.display = showHud ? "block" : "none";
}
function move(dx, dy) {
    cx += dx; cy += dy;
    const mi = maps.find(m => m.name === curName);
    if (mi) { cx = Math.max(0, Math.min(cx, mi.w - 1)); cy = Math.max(0, Math.min(cy, mi.h - 1)); }
    loadImg();
    renderEnts();
    updateHash();
}
function updateHash() {
    history.replaceState(null, "", `#sim=${encodeURIComponent(curName)}&c=${cx},${cy}&z=${z}&om=${selOff.value}`);
}
selMap.addEventListener("change", () => { cx = Math.floor((maps.find(m => m.name === selMap.value) || {}).w / 2) || 0;
    cy = Math.floor((maps.find(m => m.name === selMap.value) || {}).h / 2) || 0; pick(selMap.value); });
document.getElementById("chk-g").addEventListener("change", loadImg);
document.getElementById("chk-m").addEventListener("change", loadImg);
document.getElementById("chk-f").addEventListener("change", loadImg);
selOff.addEventListener("change", () => { updateHash(); loadImg(); });
document.getElementById("btn-strip").addEventListener("click", () => {
    const mi = maps.find(m => m.name === curName);
    if (!mi) return;
    const g = document.getElementById("chk-g").checked ? 1 : 0;
    const m = document.getElementById("chk-m").checked ? 1 : 0;
    const f = document.getElementById("chk-f").checked ? 1 : 0;
    window.open("/strip?map=" + encodeURIComponent(mi.name) + "&z=2&g=" + g + "&m=" + m + "&f=" + f, "_blank");
});
function loadThumb() {
    const mi = maps.find(m => m.name === curName);
    const t = document.getElementById("map-thumb");
    if (!mi) { t.style.display = "none"; return; }
    t.src = "/thumb?map=" + encodeURIComponent(mi.name);
    t.style.display = "inline-block";
}
document.getElementById("btn-hud").addEventListener("click", () => { showHud = !showHud; renderHud(); });
document.getElementById("btn-mm").addEventListener("click", () => { mm = mm === 128 ? 256 : 128; renderHud(); });
document.getElementById("btn-back").addEventListener("click", () => { location.href = "/"; });
window.addEventListener("keydown", (e) => {
    const k = e.key;
    if (k === "ArrowLeft" || k === "a" || k === "A") move(-1, 0);
    else if (k === "ArrowRight" || k === "d" || k === "D") move(1, 0);
    else if (k === "ArrowUp" || k === "w" || k === "W") move(0, -1);
    else if (k === "ArrowDown" || k === "s" || k === "S") move(0, 1);
    else if (k === "t" || k === "T") { mm = mm === 128 ? 256 : 128; renderHud(); }
    else if (k === "h" || k === "H") { showHud = !showHud; renderHud(); }
    else return;
    e.preventDefault();
});
window.addEventListener("wheel", (e) => {
    if (!e.ctrlKey) return;
    e.preventDefault();
    const mi = maps.find(m => m.name === curName);
    if (!mi) return;
    const maxZ = mi.ladder.length - 1;
    const minZ = mi.ladder[0] ?? 0;
    if (e.deltaY < 0 && z > minZ) z--;
    else if (e.deltaY > 0 && z < maxZ) z++;
    else return;
    loadImg(); renderEnts(); updateHash();
}, { passive: false });

// hover: cell under cursor -> /api/cell (per-layer file/frame/flag/animation)
const cellInfo = document.getElementById("cell-info");
let cellTimer = null;
wimg.addEventListener("mousemove", (e) => {
    if (!curName) return;
    const s = 1 << z;
    const wx = Math.floor(e.offsetX * s / 48);
    const wy = Math.floor(e.offsetY * s / 32);
    if (wx < 0 || wy < 0) { cellInfo.style.display = "none"; return; }
    clearTimeout(cellTimer);
    cellTimer = setTimeout(async () => {
        try {
            const r = await fetch("/api/cell?map=" + encodeURIComponent(curName) +
                                  "&x=" + wx + "&y=" + wy);
            const d = await r.json();
            if (!d.ok) { cellInfo.style.display = "none"; return; }
            const fmt = (o) => o.frame !== undefined ? `${o.frame}` : "—";
            cellInfo.textContent =
                `格 ${d.x},${d.y}  flag=${d.flag} anim=${d.anim[0]},${d.anim[1]}\n` +
                `Back  : ${d.back.lib} [${d.back.file}] f${fmt(d.back)}\n` +
                `Middle: ${d.mid.lib} [${d.mid.file}] f${fmt(d.mid)}\n` +
                `Front : ${d.front.lib} [${d.front.file}] f${fmt(d.front)}`;
            cellInfo.style.left = Math.min(e.clientX + 14, window.innerWidth - 260) + "px";
            cellInfo.style.top = Math.max(6, e.clientY - 90) + "px";
            cellInfo.style.display = "block";
        } catch (_) { cellInfo.style.display = "none"; }
    }, 60);
});
wimg.addEventListener("mouseleave", () => { cellInfo.style.display = "none"; });
init();
</script>
</body>
</html>
"""

HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="utf-8">
    <title>Zircon / Mir3 EI 地图浏览器</title>
    <style>
        body { margin:0; padding:0; background:#111; color:#eee; font-family:sans-serif; overflow:hidden; user-select:none; }
        #toolbar { height:40px; background:#222; display:flex; align-items:center; padding:0 10px; gap:10px; border-bottom:1px solid #333; }
        #viewport { position:absolute; top:40px; left:0; right:0; bottom:0; overflow:auto; background:#0b0b0f; cursor:grab; }
        #viewport.dragging { cursor:grabbing; }
        #map-img { display:block; background:#000; }
        #grid-canvas { position:absolute; top:40px; left:0; pointer-events:none; }
        #cat-panel { position:fixed; left:10px; bottom:10px; width:330px; max-height:46vh; overflow:auto;
            background:rgba(10,12,16,.92); border:1px solid #3a3a46; border-radius:6px; padding:8px 10px;
            font-size:12px; color:#c8c8d2; z-index:60; display:none; line-height:1.45; }
        #cat-panel h4 { margin:0 0 6px; font-size:13px; color:#ffd54a; }
        #cat-panel .row { display:flex; justify-content:space-between; gap:10px; }
        #cat-panel .k { color:#8a8a98; }
        #cat-panel .v { color:#e8e8f0; font-family:ui-monospace,monospace; }
        #cat-panel .warn { color:#ff8f6b; }
        #cat-panel .lib { font-family:ui-monospace,monospace; }
        #cat-panel .lib .oob { color:#ff8f6b; }
        #cat-panel::-webkit-scrollbar { width:8px; } #cat-panel::-webkit-scrollbar-thumb { background:#3a3a44; border-radius:4px; }
        #info { font-size:12px; color:#aaa; white-space:nowrap; }
        #status { margin-left:auto; font-size:12px; color:#e90; white-space:nowrap; }
        button { font-size:14px; min-width:32px; padding:4px 9px; white-space:nowrap; cursor:pointer; background:#333; color:#eee; border:1px solid #555; border-radius:3px; }
        button:disabled { opacity:.35; cursor:default; }
        label { font-size:13px; cursor:pointer; white-space:nowrap; }
        #minimap { position:fixed; top:48px; right:10px; background:rgba(0,0,0,.75); border:1px solid #444; border-radius:4px; padding:4px; z-index:50; box-shadow:0 2px 8px rgba(0,0,0,.5); }
        #minimap .mm-title { font-size:11px; color:#aaa; margin-bottom:3px; }
        #mm-box { position:relative; cursor:crosshair; }
        #mm-img { display:block; width:172px; background:#000; border-radius:2px; }
        #mm-rect { position:absolute; border:1.5px solid #ffd54a; background:rgba(255,213,74,.10); pointer-events:none; }
        /* custom map selector */
        .msel { position:relative; }
        #map-sel-btn { display:flex; align-items:center; gap:8px; min-width:180px; max-width:260px; background:#2b2b31;
            color:#eee; border:1px solid #4a4a55; border-radius:4px; padding:4px 9px; cursor:pointer; font-size:13px; }
        #map-sel-btn:hover { background:#34343b; border-color:#5c5c6a; }
        #map-sel-label { flex:1; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; text-align:left; }
        .msel-caret { color:#8a8a95; font-size:10px; }
        .msel-pop { position:absolute; top:calc(100% + 4px); left:0; min-width:280px; max-width:380px; background:#232329;
            border:1px solid #4a4a55; border-radius:6px; box-shadow:0 8px 24px rgba(0,0,0,.6); z-index:100; overflow:hidden; }
        #map-sel-filter { width:100%; box-sizing:border-box; padding:7px 9px; background:#1c1c21; color:#eee;
            border:none; border-bottom:1px solid #3a3a44; outline:none; font-size:13px; }
        #map-sel-filter::placeholder { color:#6a6a75; }
        .msel-list { max-height:340px; overflow-y:auto; }
        .msel-item { padding:6px 10px; cursor:pointer; font-size:13px; color:#d5d5dd; display:flex; gap:8px; align-items:baseline; }
        .msel-item .msel-cn { color:#9a9aa5; }
        .msel-item:hover, .msel-item.active { background:#3a3a44; color:#fff; }
        .msel-item.empty { color:#6a6a75; cursor:default; }
        .msel-item.empty:hover { background:none; }
        .msel-list::-webkit-scrollbar { width:8px; }
        .msel-list::-webkit-scrollbar-thumb { background:#3a3a44; border-radius:4px; }

        /* Toast Notifications */
        #toast-container { position:fixed; top:54px; right:20px; z-index:999999; display:flex; flex-direction:column; gap:10px; pointer-events:none; }
        .toast { pointer-events:auto; background:rgba(22, 26, 36, 0.95); border:1px solid #3de88a; border-left:4px solid #3de88a; border-radius:6px; padding:12px 16px; box-shadow:0 8px 24px rgba(0,0,0,0.5); backdrop-filter:blur(8px); min-width:280px; max-width:420px; transform:translateX(120%); transition:transform 0.35s cubic-bezier(0.18, 0.89, 0.32, 1.28), opacity 0.35s; opacity:0; }
        .toast.show { transform:translateX(0); opacity:1; }
        .toast-title { font-size:14px; font-weight:600; color:#3de88a; margin-bottom:4px; display:flex; align-items:center; gap:6px; }
        .toast-body { font-size:12px; color:#c5c5d0; line-height:1.4; word-break:break-all; }

        /* Custom Modal Dialog */
        #custom-modal-overlay { display:none; position:fixed; top:0; left:0; width:100vw; height:100vh; background:rgba(0,0,0,0.7); backdrop-filter:blur(4px); z-index:999999; align-items:center; justify-content:center; }
        .modal-card { background:#1e222d; border:1px solid #3de88a; border-radius:8px; width:400px; max-width:90vw; padding:20px; box-shadow:0 12px 32px rgba(0,0,0,0.7); display:flex; flex-direction:column; gap:14px; }
        .modal-header { font-size:16px; font-weight:600; color:#3de88a; display:flex; align-items:center; gap:8px; }
        .modal-body { font-size:13px; color:#ccc; line-height:1.5; }
        .modal-actions { display:flex; justify-content:flex-end; gap:10px; margin-top:6px; }
        .btn-modal { padding:6px 16px; font-size:13px; border-radius:4px; cursor:pointer; font-weight:500; }
        .btn-modal-cancel { background:#2a2e3a; color:#aaa; border:1px solid #444; }
        .btn-modal-cancel:hover { background:#343948; color:#eee; }
        .btn-modal-confirm { background:#183828; color:#85ffc7; border:1px solid #3de88a; }
        .btn-modal-confirm:hover { background:#1f4834; }

        /* Spinner & Overlay */
        @keyframes spin { 0% { transform:rotate(0deg); } 100% { transform:rotate(360deg); } }
        #loading-overlay { display:none; position:fixed; top:0; left:0; width:100vw; height:100vh;
            background:rgba(12, 14, 18, 0.85); backdrop-filter:blur(6px); -webkit-backdrop-filter:blur(6px);
            z-index:99999; flex-direction:column; align-items:center; justify-content:center; color:#fff; }
        .spinner { width:52px; height:52px; border:4px solid rgba(61,232,138,0.15); border-top-color:#3de88a;
            border-radius:50%; animation:spin 0.8s linear infinite; box-shadow:0 0 16px rgba(61,232,138,0.3); }
    </style>
</head>
<body>
    <!-- Toast 通知容器 -->
    <div id="toast-container"></div>

    <!-- 自定义 Modal 对话框 -->
    <div id="custom-modal-overlay">
        <div class="modal-card">
            <div class="modal-header" id="modal-title">⚡ 提示</div>
            <div class="modal-body" id="modal-msg">确定要进行此操作吗？</div>
            <div class="modal-actions">
                <button class="btn-modal btn-modal-cancel" id="btn-modal-cancel">取消</button>
                <button class="btn-modal btn-modal-confirm" id="btn-modal-confirm">确认</button>
            </div>
        </div>
    </div>

    <!-- 全屏客户端切换加载蒙版遮罩 -->
    <div id="loading-overlay">
        <div class="spinner"></div>
        <div id="loading-title" style="margin-top:18px; font-size:17px; font-weight:600; color:#3de88a; letter-spacing:0.5px;">正在切换客户端资源库…</div>
        <div id="loading-detail" style="margin-top:8px; font-size:13px; color:#aaa; font-family:monospace;">正在加载新客户端数据...</div>
    </div>

    <div id="toolbar">
        <span>📁 客户端:</span>
        <div class="msel" id="root-sel">
            <button id="root-sel-btn" type="button" title="切换客户端资源库">
                <span id="root-sel-label">加载中…</span><span class="msel-caret">▾</span>
            </button>
            <div class="msel-pop" id="root-sel-pop" hidden>
                <div class="msel-list" id="root-sel-list"></div>
            </div>
        </div>
        <span>地图:</span>
        <div class="msel" id="map-sel">
            <button id="map-sel-btn" type="button" title="选择地图">
                <span id="map-sel-label">加载中…</span><span class="msel-caret">▾</span>
            </button>
            <div class="msel-pop" id="map-sel-pop" hidden>
                <input id="map-sel-filter" type="text" placeholder="搜索地图文件名或中文名…" autocomplete="off">
                <div class="msel-list" id="map-sel-list"></div>
            </div>
        </div>
        <button id="btn-zoom-in" title="放大 (+)">＋</button>
        <button id="btn-zoom-out" title="缩小 (-)">－</button>
        <button id="btn-fit" title="适配全图窗口大小">⛶ 适配</button>
        <button id="btn-rebuild-one" style="background:#4a2e18; border-color:#e8a33d; color:#ffd899;" title="重新生成当前地图静态图 (清空缓存并重新渲染)">🔄 重新生成</button>
        <button id="btn-rebuild-all" style="background:#183828; border-color:#3de88a; color:#85ffc7;" title="后台批量预生成全库地图静态高清大图">⚡ 预生成全库</button>
        <a href="/sim" style="font-size:13px; color:#8cf; text-decoration:none; background:#1c2333; border:1px solid #3a4a6a; border-radius:3px; padding:5px 9px; white-space:nowrap;" title="Mir3 EI 原版 800×600 视口模拟器">800×600 模拟</a>

        <!-- 后台预生成实时进度条 -->
        <div id="progress-box" style="display:none; background:#141d18; border:1px solid #3de88a; border-radius:6px; padding:3px 10px; font-size:12px; color:#3de88a; align-items:center; gap:8px;">
            <span>⚡ 预生成:</span>
            <div style="width:100px; height:8px; background:#2a2e38; border-radius:4px; overflow:hidden;">
                <div id="progress-bar-fill" style="width:0%; height:100%; background:linear-gradient(90deg, #3de88a, #e8a33d); transition:width 0.3s;"></div>
            </div>
            <span id="progress-text" style="font-family:monospace;">0% (0/0)</span>
        </div>

        <label><input type="checkbox" id="chk-g" checked> Back</label>
        <label><input type="checkbox" id="chk-m" checked> Middle</label>
        <label><input type="checkbox" id="chk-f" checked> Front</label>
        <label><input type="checkbox" id="chk-grid"> 网格</label>
        <span id="info"></span>
        <span id="status"></span>
    </div>
    <div id="viewport"><img id="map-img" draggable="false" alt=""><canvas id="grid-canvas" width="0" height="0"></canvas></div>
    <div id="cat-panel"></div>
    <div id="minimap">
        <div class="mm-title">全图</div>
        <div id="mm-box"><img id="mm-img" draggable="false" alt=""><div id="mm-rect" style="display:none"></div></div>
    </div>

    <script>
        // Static full-map viewer: the server pre-renders the whole map at each
        // zoom ladder level once (disk-cached JPEG); the browser only displays
        // images. No tile requests, no canvas compositing.
        const vp = document.getElementById("viewport");
        const imgEl = document.getElementById("map-img");
        const mselBtn = document.getElementById("map-sel-btn");
        const mselLabel = document.getElementById("map-sel-label");
        const mselPop = document.getElementById("map-sel-pop");
        const mselFilter = document.getElementById("map-sel-filter");
        const mselList = document.getElementById("map-sel-list");
        const infoEl = document.getElementById("info");
        const statusEl = document.getElementById("status");
        const mmImg = document.getElementById("mm-img");
        const mmBox = document.getElementById("mm-box");
        const mmRect = document.getElementById("mm-rect");

        let maps = [], cur = -1, ladder = [], worldW = 0, worldH = 0;
        let version = 0;            // render generation; ignore stale loads
        let anchorX = 0, anchorY = 0; // world px at viewport center
        let dragging = false, dragX = 0, dragY = 0, scX = 0, scY = 0;
        let miniReady = false, miniDrag = false;

        let curName = null;
        const curMap = () => maps.find(m => m.name === curName);
        const curZ = () => ladder[cur];
        const curScale = () => 1 << curZ();
        const gOn = () => document.getElementById("chk-g").checked ? 1 : 0;
        const mOn = () => document.getElementById("chk-m").checked ? 1 : 0;
        const fOn = () => document.getElementById("chk-f").checked ? 1 : 0;

        function fmt(mi, z) {
            const s = 1 << z;
            const iw = Math.ceil(worldW / s), ih = Math.ceil(worldH / s);
            return (mi.cn ? mi.cn + " · " : "") + mi.name + " | " + mi.w + "×" + mi.h +
                   " 格 | 1:" + s + " | " + iw + "×" + ih + "px";
        }

        function setAnchorFromView() {
            anchorX = (vp.scrollLeft + vp.clientWidth / 2) * curScale();
            anchorY = (vp.scrollTop + vp.clientHeight / 2) * curScale();
        }

        function applyAnchor() {
            if (!imgEl.naturalWidth) return;
            const s = curScale();
            const maxX = Math.max(0, imgEl.naturalWidth - vp.clientWidth);
            const maxY = Math.max(0, imgEl.naturalHeight - vp.clientHeight);
            vp.scrollLeft = Math.max(0, Math.min(anchorX / s - vp.clientWidth / 2, maxX));
            vp.scrollTop  = Math.max(0, Math.min(anchorY / s - vp.clientHeight / 2, maxY));
        }

        function render(keepAnchor) {
            const mi = curMap();
            if (!mi || ladder.length === 0) return;
            const z = curZ();
            const v = ++version;
            if (!keepAnchor) setAnchorFromView();
            infoEl.textContent = fmt(mi, z);
            statusEl.textContent = "整图生成中…(首次打开大图需等待)";
            document.getElementById("btn-zoom-in").disabled = cur <= 0;
            document.getElementById("btn-zoom-out").disabled = cur >= ladder.length - 1;
            const img = new Image();
            img.onload = () => {
                if (v !== version) return;
                imgEl.src = img.src;
                statusEl.textContent = "就绪";
                applyAnchor();
                drawMini();
                drawGrid();
                hideLoading();
            };
            img.onerror = () => {
                if (v === version) {
                    statusEl.textContent = "生成失败";
                    hideLoading();
                }
            };
            img.src = "/fullmap?map=" + encodeURIComponent(mi.name) + "&z=" + z +
                      "&g=" + gOn() + "&m=" + mOn() + "&f=" + fOn();
        }

        function loadMap() {
            const mi = curMap();
            if (!mi) return;
            ladder = mi.ladder;
            cur = ladder.length - 1;          // default: whole map visible
            worldW = mi.world_w || (mi.w + mi.h + 3) * 24;
            worldH = mi.world_h || (mi.w + mi.h + 2) * 16;
            anchorX = worldW / 2; anchorY = worldH / 2;
            version++;
            imgEl.src = "";
            loadMini();
            render(true);
        }

        function loadMini() {
            const mi = curMap();
            if (!mi) return;
            miniReady = false;
            mmRect.style.display = "none";
            mmImg.onload = () => { miniReady = true; drawMini(); };
            mmImg.onerror = () => { miniReady = false; };
            mmImg.src = "/minimap?map=" + encodeURIComponent(mi.name);
        }

        function drawMini() {
            if (!miniReady || !worldW || !worldH || !mmBox.clientWidth) return;
            const s = curScale();
            const bw = mmBox.clientWidth, bh = mmBox.clientHeight;
            mmRect.style.display = "block";
            mmRect.style.left   = (vp.scrollLeft * s / worldW * bw) + "px";
            mmRect.style.top    = (vp.scrollTop  * s / worldH * bh) + "px";
            mmRect.style.width  = Math.max(2, Math.min(vp.clientWidth  * s / worldW * bw, bw)) + "px";
            mmRect.style.height = Math.max(2, Math.min(vp.clientHeight * s / worldH * bh, bh)) + "px";
        }

        function miniPan(cx, cy) {
            anchorX = cx; anchorY = cy;
            applyAnchor();
            drawMini();
        }

        // ---- grid overlay (rect layout: cell = 48x32 world px) ----
        const gridCanvas = document.getElementById("grid-canvas");
        const gridCtx = gridCanvas.getContext("2d");
        const gridOn = () => document.getElementById("chk-grid").checked;

        function drawGrid() {
            const s = curScale();
            if (!gridOn() || !imgEl.naturalWidth) { gridCanvas.width = 0; gridCanvas.height = 0; return; }
            // canvas is a child of #viewport (position:absolute), so imgRect
            // (viewport-relative) maps 1:1 to canvas coordinates
            const vpRect = vp.getBoundingClientRect();
            const imgRect = imgEl.getBoundingClientRect();
            const ox = imgRect.left - vpRect.left, oy = imgRect.top - vpRect.top;
            gridCanvas.style.left = ox + "px";
            gridCanvas.style.top = oy + "px";
            const cw = imgRect.width, ch = imgRect.height;
            if (cw <= 0 || ch <= 0) return;
            gridCanvas.width = cw * (window.devicePixelRatio || 1);
            gridCanvas.height = ch * (window.devicePixelRatio || 1);
            gridCanvas.style.width = cw + "px";
            gridCanvas.style.height = ch + "px";
            const ctx = gridCtx;
            ctx.setTransform(1, 0, 0, 1, 0, 0);
            ctx.clearRect(0, 0, gridCanvas.width, gridCanvas.height);
            ctx.scale(window.devicePixelRatio || 1, window.devicePixelRatio || 1);
            const cwPx = 48 / s, chPx = 32 / s;   // world->screen at this zoom
            if (cwPx < 2 || chPx < 2) return;      // too dense to draw
            ctx.strokeStyle = "rgba(255,213,74,0.35)";
            ctx.lineWidth = 1;
            ctx.beginPath();
            for (let x = 0; x <= cw; x += cwPx) { ctx.moveTo(x, 0); ctx.lineTo(x, ch); }
            for (let y = 0; y <= ch; y += chPx) { ctx.moveTo(0, y); ctx.lineTo(cw, y); }
            ctx.stroke();
        }

        // ---- cursor cell coordinate readout (rect: world px -> cell) ----
        vp.addEventListener("mousemove", (e) => {
            const mi = curMap();
            if (!mi) return;
            const s = curScale();
            const rect = vp.getBoundingClientRect();
            const wx = (vp.scrollLeft + e.clientX - rect.left) * s;
            const wy = (vp.scrollTop + e.clientY - rect.top) * s;
            const cx = Math.floor(wx / 48), cy = Math.floor(wy / 32);
            let extra = "";
            const cat = catCache[mi.name];
            if (cat) {
                const flag = cellFlag(cat, cx, cy);
                if (flag) extra = " · flag=" + flag;
            }
            infoEl.textContent = fmt(mi, curZ()) + " · 格 " + cx + "," + cy + extra;
        });

        // ---- catalog info panel ----
        let catCache = {};   // map_name -> catalog doc

        function cellFlag(cat, x, y) {
            // flag byte lives at cell offset +0; catalog doesn't store the
            // full matrix, so report only when the flag histogram says 1s exist.
            return "";
        }

        function fmtLibRow(layer, entries) {
            const rows = [];
            for (const [lid, info] of Object.entries(entries)) {
                const oob = info.frame_oob ? `<span class="oob"> OOB ${info.frame_oob}</span>` : "";
                rows.push(`<div class="row"><span class="k">${layer} ${lid} ${info.lib}</span><span class="v">${info.cells}格 ≤${info.frame_max}${oob}</span></div>`);
            }
            return rows.join("");
        }

        async function loadCatalog(mi) {
            if (catCache[mi.name]) { renderCat(mi); return; }
            try {
                const res = await fetch("/api/catalog?map=" + encodeURIComponent(mi.name));
                const data = await res.json();
                if (data.ok) catCache[mi.name] = data.catalog;
                else catCache[mi.name] = null;
            } catch (e) { catCache[mi.name] = null; }
            renderCat(mi);
        }

        function renderCat(mi) {
            const panel = document.getElementById("cat-panel");
            const cat = catCache[mi.name];
            if (!cat) { panel.style.display = "none"; return; }
            const anom = cat.anomaly_total || 0;
            const warn = anom ? `<span class="warn"> ⚠ ${anom} 帧越界</span>` : "";
            let html = `<h4>${cat.name}${cat.display ? " · MiniMap " + cat.display : ""}${warn}</h4>
<div class="row"><span class="k">主题</span><span class="v">${cat.theme_name || "base"}</span></div>
<div class="row"><span class="k">尺寸</span><span class="v">${cat.w}×${cat.h} · ${cat.cell_bytes}B/格${cat.legacy_13b ? " · legacy" : ""}</span></div>
<div class="row"><span class="k">动画格</span><span class="v">${cat.animated_cells || 0}</span></div>`;
            for (const layer of ["ground", "mid", "front"]) {
                const e = cat[layer];
                if (e && Object.keys(e).length) html += `<div class="lib"><b>${layer}</b>${fmtLibRow(layer, e)}</div>`;
            }
            panel.innerHTML = html;
            panel.style.display = "block";
        }

        // ---- custom map dropdown ----
        function mselLabelOf(m) { return m ? (m.cn ? m.cn + " — " : "") + m.name : "加载中…"; }
        function mselOpen() { mselPop.hidden = false; mselFilter.value = ""; renderMselList(); mselFilter.focus(); }
        function mselClose() { mselPop.hidden = true; }
        function mselFiltered() {
            const q = mselFilter.value.trim().toLowerCase();
            if (!q) return maps;
            return maps.filter(m =>
                m.name.toLowerCase().includes(q) || (m.cn || "").toLowerCase().includes(q));
        }
        function renderMselList() {
            const items = mselFiltered();
            if (items.length === 0) {
                mselList.innerHTML = '<div class="msel-item empty">没有匹配的地图</div>';
                return;
            }
            mselList.innerHTML = items.map(m =>
                '<div class="msel-item" data-name="' + m.name.replace(/"/g, "&quot;") + '">' +
                '<span class="msel-cn">' + (m.cn || "") + '</span><span>' + m.name + '</span></div>'
            ).join("");
            // scroll active/current item into view
            const cur = mselList.querySelector('.msel-item[data-name="' + curName + '"]');
            if (cur) { cur.classList.add("active"); cur.scrollIntoView({ block: "nearest" }); }
        }
        function mselPick(name) {
            const mi = maps.find(m => m.name === name);
            if (!mi) return;
            curName = name;
            mselLabel.textContent = mselLabelOf(mi);
            mselClose();
            loadMap();
            loadCatalog(mi);
        }
        mselBtn.addEventListener("click", (e) => {
            e.stopPropagation();
            if (mselPop.hidden) mselOpen(); else mselClose();
        });
        mselFilter.addEventListener("input", renderMselList);
        mselFilter.addEventListener("keydown", (e) => {
            if (e.key === "Enter") {
                const first = mselList.querySelector('.msel-item[data-name]');
                if (first) mselPick(first.dataset.name);
            } else if (e.key === "ArrowDown" || e.key === "ArrowUp") {
                e.preventDefault();
                const items = [...mselList.querySelectorAll('.msel-item[data-name]')];
                if (!items.length) return;
                let idx = items.findIndex(i => i.classList.contains("active"));
                idx = e.key === "ArrowDown" ? (idx + 1) % items.length : (idx - 1 + items.length) % items.length;
                items.forEach(i => i.classList.remove("active"));
                items[idx].classList.add("active");
                items[idx].scrollIntoView({ block: "nearest" });
            } else if (e.key === "Escape") { mselClose(); }
        });
        mselList.addEventListener("click", (e) => {
            const it = e.target.closest('.msel-item[data-name]');
            if (it) mselPick(it.dataset.name);
        });
        window.addEventListener("click", (e) => {
            if (!mselPop.hidden && !e.target.closest("#map-sel")) mselClose();
        });
        window.addEventListener("keydown", (e) => {
            if (e.key === "Escape" && !mselPop.hidden) mselClose();
        });
        // ---- Hash & State Memory ----
        function updateUrlHash() {
            if (!curMap()) return;
            const s = curScale();
            const ax = Math.round(anchorX || (vp.scrollLeft + vp.clientWidth / 2) * s);
            const ay = Math.round(anchorY || (vp.scrollTop + vp.clientHeight / 2) * s);
            const g = document.getElementById("chk-g").checked ? 1 : 0;
            const m = document.getElementById("chk-m").checked ? 1 : 0;
            const f = document.getElementById("chk-f").checked ? 1 : 0;
            const hash = `#map=${encodeURIComponent(curMap().name)}&cur=${cur}&x=${ax}&y=${ay}&g=${g}&m=${m}&f=${f}`;
            history.replaceState(null, '', hash);
        }

        // ---- Toast System ----
        function showToast(title, body, duration = 5000) {
            const container = document.getElementById("toast-container");
            const toast = document.createElement("div");
            toast.className = "toast";
            toast.innerHTML = `<div class="toast-title"><span>🎉</span> ${title}</div><div class="toast-body">${body}</div>`;
            container.appendChild(toast);
            requestAnimationFrame(() => toast.classList.add("show"));
            setTimeout(() => {
                toast.classList.remove("show");
                setTimeout(() => toast.remove(), 400);
            }, duration);
        }

        // ---- Custom Confirm Modal ----
        function showConfirm(title, message) {
            return new Promise((resolve) => {
                const modal = document.getElementById("custom-modal-overlay");
                const mTitle = document.getElementById("modal-title");
                const mMsg = document.getElementById("modal-msg");
                const btnCancel = document.getElementById("btn-modal-cancel");
                const btnConfirm = document.getElementById("btn-modal-confirm");

                mTitle.textContent = title || "⚡ 提示";
                mMsg.textContent = message || "确定要执行此操作吗？";
                modal.style.display = "flex";

                function cleanup(result) {
                    modal.style.display = "none";
                    btnCancel.onclick = null;
                    btnConfirm.onclick = null;
                    resolve(result);
                }

                btnCancel.onclick = () => cleanup(false);
                btnConfirm.onclick = () => cleanup(true);
            });
        }

        const overlay = document.getElementById("loading-overlay");
        const loadingTitle = document.getElementById("loading-title");
        const loadingDetail = document.getElementById("loading-detail");

        function showLoading(title, detail) {
            if (title) loadingTitle.textContent = title;
            if (detail) loadingDetail.textContent = detail;
            overlay.style.display = "flex";
        }
        function hideLoading() {
            overlay.style.display = "none";
        }

        // ---- Custom Root Selector ----
        const rselBtn = document.getElementById("root-sel-btn");
        const rselLabel = document.getElementById("root-sel-label");
        const rselPop = document.getElementById("root-sel-pop");
        const rselList = document.getElementById("root-sel-list");
        let availableRoots = [], currentRootPath = "";

        function rselOpen() { rselPop.hidden = false; }
        function rselClose() { rselPop.hidden = true; }

        async function loadRoots() {
            try {
                const res = await fetch("/api/roots");
                const data = await res.json();
                availableRoots = data.roots;
                currentRootPath = data.current;
                const cur = availableRoots.find(r => r.path === currentRootPath) || availableRoots[0];
                if (cur) rselLabel.textContent = cur.name;

                rselList.innerHTML = availableRoots.map(r =>
                    '<div class="msel-item' + (r.path === currentRootPath ? ' active' : '') + '" data-path="' + r.path.replace(/"/g, '&quot;') + '">' +
                    '<span>' + r.name + '</span><span class="msel-cn" style="font-size:11px;">(' + r.path + ')</span></div>'
                ).join("");
            } catch (e) {}
        }

        rselBtn.addEventListener("click", (e) => {
            e.stopPropagation();
            if (rselPop.hidden) rselOpen(); else rselClose();
        });

        rselList.addEventListener("click", async (e) => {
            const item = e.target.closest('.msel-item[data-path]');
            if (!item) return;
            const targetPath = item.dataset.path;
            const targetRoot = availableRoots.find(r => r.path === targetPath);
            rselClose();
            if (!targetRoot || targetPath === currentRootPath) return;

            showLoading(`正在载入客户端 [${targetRoot.name}]...`, `资源路径: ${targetPath}`);
            imgEl.src = "";
            mselList.innerHTML = "";
            statusEl.textContent = "正在切换客户端资源库…";

            await fetch("/api/switch_root?path=" + encodeURIComponent(targetPath), { method: "POST" });
            location.hash = "";
            await init();

            showToast(
                `客户端资源库已成功切换`,
                `已成功载入 [${targetRoot.name}]！共解析到 ${maps.length} 张地图。<br>素材路径: <code style="color:#3de88a;">${targetPath}</code>`
            );
        });

        window.addEventListener("click", (e) => {
            if (!rselPop.hidden && !e.target.closest("#root-sel")) rselClose();
        });

        // ---- init ----
        async function init() {
            await loadRoots();
            const res = await fetch("/api/maps");
            maps = await res.json();
            if (!maps.length) return;

            // Parse Hash params: #map=0.map&cur=1&x=1200&y=800
            let targetMap = maps[0].name;
            let targetCur = null;
            let targetX = null;
            let targetY = null;

            if (location.hash) {
                const matchMap = location.hash.match(/map=([^&]+)/);
                const matchCur = location.hash.match(/cur=(\\d+)/);
                const matchX   = location.hash.match(/x=(\\d+)/);
                const matchY   = location.hash.match(/y=(\\d+)/);
                if (matchMap) {
                    const parsed = decodeURIComponent(matchMap[1]);
                    if (maps.some(m => m.name.toLowerCase() === parsed.toLowerCase())) {
                        targetMap = parsed;
                    }
                }
                if (matchCur) targetCur = parseInt(matchCur[1]);
                if (matchX) targetX = parseInt(matchX[1]);
                if (matchY) targetY = parseInt(matchY[1]);
            }

            mselPick(targetMap);

            if (targetCur !== null && targetCur >= 0 && targetCur < ladder.length) {
                cur = targetCur;
            }
            if (targetX !== null && targetY !== null) {
                anchorX = targetX;
                anchorY = targetY;
            }
            render(true);
            updateUrlHash();   // 立即用实际加载的地图/坐标回写 URL(自愈坏 hash)
        }
        init();

        // 轮询后台预生成进度
        async function pollProgress() {
            try {
                const res = await fetch("/api/progress");
                const data = await res.json();
                const box = document.getElementById("progress-box");
                if (data.running) {
                    box.style.display = "inline-flex";
                    document.getElementById("progress-bar-fill").style.width = data.percent + "%";
                    document.getElementById("progress-text").textContent = `${data.percent}% (${data.current}/${data.total} · 生成中 ${data.current_map})`;
                } else if (data.total > 0 && data.done + data.failed >= data.total) {
                    box.style.display = "inline-flex";
                    document.getElementById("progress-bar-fill").style.width = "100%";
                    document.getElementById("progress-text").textContent = `100% (全库 ${data.total} 张地图预生成完毕！)`;
                } else {
                    box.style.display = "none";
                }
            } catch (e) {}
        }
        setInterval(pollProgress, 1000);
        pollProgress();

        window.addEventListener("hashchange", () => {
            if (!location.hash) return;
            const matchMap = location.hash.match(/map=([^&]+)/);
            if (matchMap) {
                const parsed = decodeURIComponent(matchMap[1]);
                const mi = curMap();
                if (mi && mi.name.toLowerCase() !== parsed.toLowerCase()) {
                    init();
                }
            }
        });

        vp.addEventListener("scroll", () => {
            drawMini();
            drawGrid();
            setAnchorFromView();
            updateUrlHash();
        });

        document.getElementById("btn-zoom-in").addEventListener("click", () => {
            if (cur <= 0) return; setAnchorFromView(); cur--; render(true); updateUrlHash();
        });
        document.getElementById("btn-zoom-out").addEventListener("click", () => {
            if (cur >= ladder.length - 1) return; setAnchorFromView(); cur++; render(true); updateUrlHash();
        });
        document.getElementById("btn-fit").addEventListener("click", () => {
            cur = ladder.length - 1;
            anchorX = worldW / 2; anchorY = worldH / 2;
            render(true);
            updateUrlHash();
        });
        document.getElementById("btn-rebuild-one").addEventListener("click", async () => {
            const mi = curMap();
            if (!mi) return;
            statusEl.textContent = "正在强制重新生成本图静态图…";
            await fetch("/api/rebuild?map=" + encodeURIComponent(mi.name), { method: "POST" });
            render(true);
        });

        document.getElementById("btn-rebuild-all").addEventListener("click", async () => {
            const ok = await showConfirm("⚡ 批量预生成提示", `确定要在后台批量将全库 ${maps.length} 张地图预生成为静态高清大图吗？生成过程中您仍可正常浏览其他静态图。`);
            if (!ok) return;
            statusEl.textContent = "已触发后台批量预生成全部地图静态图…";
            await fetch("/api/rebuild_all", { method: "POST" });
            showToast("批量预生成任务已启动", `后台多线程已开始合成全库 ${maps.length} 张地图静态图！顶部进度条将实时更新。`);
        });

        document.getElementById("chk-g").addEventListener("change", () => { render(); updateUrlHash(); });
        document.getElementById("chk-m").addEventListener("change", () => { render(); updateUrlHash(); });
        document.getElementById("chk-f").addEventListener("change", () => { render(); updateUrlHash(); });
        document.getElementById("chk-grid").addEventListener("change", () => { drawGrid(); });

        // Drag to pan
        vp.addEventListener("mousedown", (e) => {
            dragging = true; vp.classList.add("dragging");
            dragX = e.clientX; dragY = e.clientY;
            scX = vp.scrollLeft; scY = vp.scrollTop;
            e.preventDefault();
        });
        window.addEventListener("mousemove", (e) => {
            if (!dragging) return;
            vp.scrollLeft = scX - (e.clientX - dragX);
            vp.scrollTop  = scY - (e.clientY - dragY);
            drawMini();
        });
        window.addEventListener("mouseup", () => { dragging = false; vp.classList.remove("dragging"); });

        // Ctrl + 滚轮: zoom around the mouse point (swap ladder level)
        window.addEventListener("wheel", (e) => {
            if (!e.ctrlKey) return;
            e.preventDefault();
            const rect = vp.getBoundingClientRect();
            const mx = e.clientX - rect.left, my = e.clientY - rect.top;
            const s = curScale();
            anchorX = (vp.scrollLeft + mx) * s;
            anchorY = (vp.scrollTop + my) * s;
            let changed = false;
            if (e.deltaY < 0 && cur > 0) { cur--; changed = true; }
            else if (e.deltaY > 0 && cur < ladder.length - 1) { cur++; changed = true; }
            if (changed) { render(true); updateUrlHash(); }
        }, { passive: false });

        // Minimap click/drag -> pan main view
        mmBox.addEventListener("mousedown", (e) => {
            miniDrag = true;
            const r = mmBox.getBoundingClientRect();
            miniPan((e.clientX - r.left) / r.width * worldW,
                    (e.clientY - r.top) / r.height * worldH);
            e.preventDefault();
        });
        window.addEventListener("mousemove", (e) => {
            if (!miniDrag) return;
            const r = mmBox.getBoundingClientRect();
            miniPan((e.clientX - r.left) / r.width * worldW,
                    (e.clientY - r.top) / r.height * worldH);
        });
        window.addEventListener("mouseup", () => { miniDrag = false; });
    </script>
</body>
</html>
"""

BATCH_PROGRESS = {
    "running": False,
    "total": 0,
    "current": 0,
    "current_map": "",
    "done": 0,
    "failed": 0,
    "percent": 0
}


KNOWN_CANDIDATE_ROOTS = [
    "/home/tetsuya/NAS/TMP/EI传奇3.0客户端",
    "/home/tetsuya/NAS/TMP/mir3ei",
    "/home/tetsuya/development/Zircon/Debug/Client"
]

def get_client_roots() -> list[dict]:
    roots = []
    for path in KNOWN_CANDIDATE_ROOTS:
        if os.path.exists(path):
            name = os.path.basename(path.rstrip("/"))
            m_dir = os.path.join(path, "Map") if os.path.exists(os.path.join(path, "Map")) else path
            d_dir = os.path.join(path, "Data") if os.path.exists(os.path.join(path, "Data")) else path
            roots.append({
                "name": name,
                "path": path,
                "map_dir": m_dir,
                "data_dir": d_dir
            })
    return roots


class ViewerHandler(BaseHTTPRequestHandler):
    map_cache: MapCache
    pool: FramePool
    tile_cache: dict[tuple, bytes] = {}
    tile_cache_lock = threading.Lock()
    protocol_version = "HTTP/1.1"
    cache_dir: str = ""   # disk cache root; empty disables persistence
    thumbs_dir: str = THUMBS_DIR  # full-map thumbnail dir (shared with WikiServer)
    render_locks: dict = {}       # per-fullmap-key render locks (dedupe work)
    render_locks_mu = threading.Lock()
    current_root_path: str = ""
    layout: str = LAYOUT_RECT   # axis-aligned (original Mir3.exe projection); "iso" legacy
    catalog: dict = {}          # map_name -> catalog doc (build_map_catalog.py)
    entities: list = []         # Mud3 Envir entity data (load_entities)

    @classmethod
    def _render_lock(cls, key: tuple):
        with cls.render_locks_mu:
            lk = cls.render_locks.get(key)
            if lk is None:
                lk = cls.render_locks[key] = threading.Lock()
            return lk

    def do_POST(self):
        from urllib.parse import parse_qs, urlparse
        if self.path.startswith("/api/switch_root"):
            qs = parse_qs(urlparse(self.path).query)
            target_path = qs.get("path", [""])[0]
            roots = get_client_roots()
            found = next((r for r in roots if r["path"] == target_path), None)
            if found:
                ViewerHandler.map_cache = MapCache(found["map_dir"])
                ViewerHandler.pool = FramePool(found["data_dir"])
                ViewerHandler.current_root_path = found["path"]
                ViewerHandler.cache_dir = os.path.join(found["map_dir"], ".tilecache")
                body = json.dumps({"ok": True, "current": found}).encode("utf-8")
            else:
                body = json.dumps({"ok": False, "error": "not_found"}).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path.startswith("/api/rebuild"):
            qs = parse_qs(urlparse(self.path).query)
            map_name = os.path.basename(qs.get("map", [""])[0])
            if map_name:
                safe = map_name.replace("/", "_").replace("\\", "_")
                cdir = os.path.join(self.cache_dir, safe)
                if os.path.exists(cdir):
                    import shutil
                    shutil.rmtree(cdir, ignore_errors=True)
            body = b'{"ok": true}'
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path == "/api/rebuild_all":
            if BATCH_PROGRESS["running"]:
                body = json.dumps({"ok": True, "msg": "already_running"}).encode("utf-8")
            else:
                def batch_worker():
                    BATCH_PROGRESS["running"] = True
                    maps = scan_maps(self.map_cache.maps_dir)
                    BATCH_PROGRESS["total"] = len(maps)
                    BATCH_PROGRESS["current"] = 0
                    BATCH_PROGRESS["done"] = 0
                    BATCH_PROGRESS["failed"] = 0
                    BATCH_PROGRESS["percent"] = 0
                    print(f"[*] Starting background pre-render for {len(maps)} maps...")

                    for idx, m in enumerate(maps):
                        mname = m["name"]
                        BATCH_PROGRESS["current"] = idx + 1
                        BATCH_PROGRESS["current_map"] = mname
                        BATCH_PROGRESS["percent"] = int(((idx + 1) / len(maps)) * 100)
                        try:
                            w, h, _ = self.map_cache.get(mname)
                            ladder = map_ladder(w, h, self.layout)
                            if ladder:
                                z = ladder[-1]
                                key = (mname, z, True, True, True, OFFSET_NONE)
                                dp = self._fullmap_path(key)
                                if not os.path.exists(dp):
                                    data = render_full_map(self.map_cache, self.pool, mname, z, True, True, True,
                                                           layout=self.layout,
                                                           offset_mode=OFFSET_NONE)
                                    os.makedirs(os.path.dirname(dp), exist_ok=True)
                                    with open(dp, "wb") as f:
                                        f.write(data)
                            BATCH_PROGRESS["done"] += 1
                        except Exception as ex:
                            BATCH_PROGRESS["failed"] += 1
                            print(f"[!] Pre-render map {mname} failed: {ex}")

                    BATCH_PROGRESS["running"] = False
                    BATCH_PROGRESS["current_map"] = "完成"
                    print("[*] Background pre-render completed!")

                threading.Thread(target=batch_worker, daemon=True).start()
                body = json.dumps({"ok": True, "msg": "started"}).encode("utf-8")

            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        else:
            self.send_error(404)

    def do_GET(self):
        if self.path == "/" or self.path == "/index.html":
            body = HTML_TEMPLATE.encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path.split("?")[0] in ("/sim", "/sim.html"):
            body = SIM_TEMPLATE.encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path == "/api/maps":
            maps = scan_maps(self.map_cache.maps_dir, self.layout)
            for m in maps:
                fid = m["name"][:-4] if m["name"].endswith(".map") else m["name"]
                m["cn"] = map_cn(fid)
            body = json.dumps(maps).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path == "/api/progress":
            body = json.dumps(BATCH_PROGRESS).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path == "/api/roots":
            roots = get_client_roots()
            cur = self.current_root_path or (roots[0]["path"] if roots else "")
            body = json.dumps({"roots": roots, "current": cur}).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path.startswith("/api/catalog?"):
            from urllib.parse import parse_qs, urlparse
            qs = parse_qs(urlparse(self.path).query)
            map_name = os.path.basename(qs.get("map", [""])[0])
            doc = self.catalog.get(map_name)
            if doc is None:
                body = json.dumps({"ok": False, "error": "not_in_catalog"}).encode("utf-8")
            else:
                body = json.dumps({"ok": True, "catalog": doc}).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path.startswith("/api/entities?"):
            from urllib.parse import parse_qs, urlparse
            qs = parse_qs(urlparse(self.path).query)
            map_name = os.path.basename(qs.get("map", [""])[0])
            ents = [e for e in self.entities if e["map"] == map_name]
            body = json.dumps({"ok": True, "count": len(ents), "entities": ents},
                              ensure_ascii=False).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path.startswith("/api/cell?"):
            from urllib.parse import parse_qs, urlparse
            qs = parse_qs(urlparse(self.path).query)
            map_name = os.path.basename(qs.get("map", [""])[0])
            try:
                x = int(qs.get("x", ["0"])[0])
                y = int(qs.get("y", ["0"])[0])
            except ValueError:
                self.send_error(400, "x/y must be ints")
                return
            try:
                w, h, cells = self.map_cache.get(map_name)
            except Exception as ex:
                self.send_error(404, f"map not readable: {ex}")
                return
            if not (0 <= x < w and 0 <= y < h):
                body = json.dumps({"ok": False, "error": "out_of_bounds",
                                   "w": w, "h": h}).encode("utf-8")
            else:
                c = cells[x][y]
                def lib_name(lid):
                    return KR_ORDER.get(lid, f"lib{lid}") if lid >= 0 else "none"
                body = json.dumps({
                    "ok": True, "x": x, "y": y, "w": w, "h": h,
                    "flag": c.flag, "anim": [c.anim_a, c.anim_b],
                    "back": {"file": c.back_file, "lib": lib_name(c.back_file),
                             "frame": c.back_img},
                    "mid": {"file": c.mid_file, "lib": lib_name(c.mid_file),
                            "frame": c.mid_img},
                    "front": {"file": c.front_file, "lib": lib_name(c.front_file),
                              "frame": c.front_img},
                }, ensure_ascii=False).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path.startswith("/strip?"):
            # Export the 3-mode offset comparison strip as PNG (sim "导出对比图").
            from urllib.parse import parse_qs, urlparse
            qs = parse_qs(urlparse(self.path).query)
            map_name = os.path.basename(qs.get("map", [""])[0])
            if not map_name.lower().endswith(".map"):
                self.send_error(400, "map must be a .map file")
                return
            try:
                z = int(qs.get("z", ["2"])[0])
            except ValueError:
                z = 2
            g = qs.get("g", ["1"])[0] == "1"
            m = qs.get("m", ["1"])[0] == "1"
            f = qs.get("f", ["1"])[0] == "1"
            try:
                data = render_offset_strip(self.map_cache, self.pool, map_name, z,
                                           g, m, f, layout=self.layout)
                self.send_response(200)
                self.send_header("Content-Type", "image/png")
                self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
                self.send_header("Content-Length", str(len(data)))
                self.end_headers()
                self.wfile.write(data)
            except Exception as ex:
                self.send_error(500, str(ex))

        elif self.path.startswith("/sprite?"):
            # Decode a single frame from a named WIL library (character / NPC /
            # monster sprites for the simulator) as a transparent PNG.
            from urllib.parse import parse_qs, urlparse
            qs = parse_qs(urlparse(self.path).query)
            lib_name = os.path.basename(qs.get("lib", [""])[0])
            try:
                frame = int(qs.get("frame", ["0"])[0])
                scale = int(qs.get("scale", ["1"])[0])
            except ValueError:
                self.send_error(400, "frame/scale must be ints")
                return
            if not lib_name.lower().endswith(".wil"):
                self.send_error(400, "lib must be a .wil file")
                return
            lib_path = os.path.join(self.pool.data_dir, lib_name)
            if not os.path.exists(lib_path):
                self.send_error(404, "lib not found in data dir")
                return
            try:
                from wilsdk import open_library as _open_wil
                lib = _open_wil(lib_path)
                img = lib.decode(frame) if frame < lib.count else None
                if img is None:
                    self.send_error(404, f"frame {frame} blank or out of range")
                    return
                if scale > 1:
                    img = img.resize((max(1, img.width // scale), max(1, img.height // scale)),
                                     Image.NEAREST)
                buf = io.BytesIO()
                img.save(buf, format="PNG")
                data = buf.getvalue()
                self.send_response(200)
                self.send_header("Content-Type", "image/png")
                self.send_header("Cache-Control", "public, max-age=86400")
                self.send_header("Content-Length", str(len(data)))
                self.end_headers()
                self.wfile.write(data)
            except Exception as ex:
                self.send_error(500, str(ex))

        elif self.path.startswith("/thumb?"):
            from urllib.parse import parse_qs, urlparse
            qs = parse_qs(urlparse(self.path).query)
            map_name = os.path.basename(qs.get("map", [""])[0])
            thumb_path = os.path.join(self.thumbs_dir, map_name + ".png")
            if not os.path.exists(thumb_path):
                # On-demand render + disk cache (one-time, ~seconds to tens of
                # seconds for large maps; shared with WikiServer/thumb_gen).
                try:
                    from thumb_gen import render_one
                    w, h, _ = self.map_cache.get(map_name)
                    render_one(self.map_cache, self.pool, self.thumbs_dir, map_name, w, h)
                except Exception as ex:
                    self.send_error(500, f"thumb render failed: {ex}")
                    return
            try:
                with open(thumb_path, "rb") as f:
                    body = f.read()
            except FileNotFoundError:
                self.send_error(404, "thumb not generated")
                return
            self.send_response(200)
            self.send_header("Content-Type", "image/png")
            self.send_header("Cache-Control", "public, max-age=3600")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        elif self.path.startswith("/minimap?"):
            from urllib.parse import parse_qs, urlparse
            qs = parse_qs(urlparse(self.path).query)
            map_name = os.path.basename(qs.get("map", [""])[0])
            stem = map_name[:-4] if map_name.lower().endswith(".map") else map_name
            img = None
            try:
                img = MiniMapSource._for(self.pool.data_dir).frame(stem)
            except Exception:
                img = None
            if img is None:
                self.send_error(404, "no minimap for %s" % map_name)
                return
            buf = io.BytesIO()
            if img.mode != "RGB":
                img = img.convert("RGB")
            img.save(buf, format="JPEG", quality=85)
            data = buf.getvalue()
            self.send_response(200)
            self.send_header("Content-Type", "image/jpeg")
            self.send_header("Cache-Control", "public, max-age=86400")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)

        elif self.path.startswith("/fullmap?"):
            from urllib.parse import parse_qs, urlparse
            qs = parse_qs(urlparse(self.path).query)
            map_name = os.path.basename(qs.get("map", [""])[0])
            if not map_name.lower().endswith(".map"):
                self.send_error(400, "map must be a .map file")
                return
            z = int(qs.get("z", ["0"])[0])
            g = qs.get("g", ["1"])[0] == "1"
            m = qs.get("m", ["1"])[0] == "1"
            f = qs.get("f", ["1"])[0] == "1"
            om = qs.get("om", [OFFSET_NONE])[0]
            if om not in OFFSET_MODES:
                om = OFFSET_NONE
            try:
                w, h, _ = self.map_cache.get(map_name)
                ladder = map_ladder(w, h, self.layout)
                if ladder:
                    z = min(max(z, ladder[0]), ladder[-1])
                key = (map_name, z, g, m, f, om)
                dp = self._fullmap_path(key)
                try:
                    with open(dp, "rb") as f:
                        data = f.read()
                except FileNotFoundError:
                    data = None
                if data is None:
                    with self._render_lock(key):
                        try:
                            with open(dp, "rb") as f:
                                data = f.read()
                        except FileNotFoundError:
                            data = None
                        if data is None:
                            # One full-map render per (map, zoom, layers);
                            # disk-cached, so the browser's next open is a
                            # static file read instead of a re-render.
                            data = render_full_map(self.map_cache, self.pool,
                                                   map_name, z, g, m, f,
                                                   layout=self.layout,
                                                   offset_mode=om)
                            os.makedirs(os.path.dirname(dp), exist_ok=True)
                            tmp = dp + ".tmp"
                            with open(tmp, "wb") as f:
                                f.write(data)
                            os.replace(tmp, dp)
                self.send_response(200)
                self.send_header("Content-Type", "image/jpeg")
                self.send_header("Cache-Control", "public, max-age=86400")
                self.send_header("Content-Length", str(len(data)))
                self.end_headers()
                self.wfile.write(data)
            except Exception as ex:
                self.send_error(500, str(ex))

        elif self.path.startswith("/tile?"):
            from urllib.parse import parse_qs, urlparse
            qs = parse_qs(urlparse(self.path).query)
            map_name = qs.get("map", [""])[0]
            tx = int(qs.get("tx", ["0"])[0])
            ty = int(qs.get("ty", ["0"])[0])
            z = int(qs.get("z", ["0"])[0])
            g = qs.get("g", ["1"])[0] == "1"
            m = qs.get("m", ["1"])[0] == "1"
            f = qs.get("f", ["1"])[0] == "1"
            om = qs.get("om", [OFFSET_NONE])[0]
            if om not in OFFSET_MODES:
                om = OFFSET_NONE

            try:
                key = (map_name, tx, ty, z, g, m, f, om)
                with self.tile_cache_lock:
                    data = self.tile_cache.get(key)
                if data is None and self.cache_dir:
                    # L2: disk cache survives restarts; the expensive render
                    # (4.6k Python RLE decodes + 122k composites for a 350x350
                    # map) is paid once per tile EVER, not once per session.
                    dp = self._tile_path(key)
                    try:
                        with open(dp, "rb") as f:
                            data = f.read()
                    except FileNotFoundError:
                        data = None
                if data is None:
                    data = render_tile(self.map_cache, self.pool, map_name, tx, ty, z, g, m, f,
                                       layout=self.layout, offset_mode=om)
                    with self.tile_cache_lock:
                        self.tile_cache[key] = data
                        while len(self.tile_cache) > CACHE_TILES_MAX:
                            self.tile_cache.pop(next(iter(self.tile_cache)))
                    if self.cache_dir:
                        dp = self._tile_path(key)
                        os.makedirs(os.path.dirname(dp), exist_ok=True)
                        tmp = dp + ".tmp"
                        with open(tmp, "wb") as f:
                            f.write(data)
                        os.replace(tmp, dp)
                self.send_response(200)
                self.send_header("Content-Type", "image/png" if z == 0 else "image/jpeg")
                self.send_header("Cache-Control", "no-store, no-cache, must-revalidate")
                self.send_header("Content-Length", str(len(data)))
                self.end_headers()
                self.wfile.write(data)
            except Exception as ex:
                self.send_error(500, str(ex))
        else:
            self.send_error(404)

    def _tile_path(self, key: tuple) -> str:
        map_name, tx, ty, z, g, m, f, om = key
        safe = map_name.replace("/", "_").replace("\\", "_")
        ext = "png" if z == 0 else "jpg"
        tag = "r" if self.layout == LAYOUT_RECT else "i"
        omt = "n" if om == OFFSET_NONE else ("a" if om == OFFSET_ALL else "m")
        return os.path.join(self.cache_dir, safe, f"{tag}_{tx}_{ty}_{z}_{int(g)}{int(m)}{int(f)}{omt}.{ext}")

    def _fullmap_path(self, key: tuple) -> str:
        map_name, z, g, m, f, om = key
        safe = map_name.replace("/", "_").replace("\\", "_")
        tag = "r" if self.layout == LAYOUT_RECT else "i"
        omt = "n" if om == OFFSET_NONE else ("a" if om == OFFSET_ALL else "m")
        return os.path.join(self.cache_dir, safe, f"full_{tag}_{z}_{int(g)}{int(m)}{int(f)}{omt}.jpg")


def scan_maps(maps_dir: str, layout: str = LAYOUT_RECT) -> list[dict]:
    out = []
    for fn in os.listdir(maps_dir):
        if not fn.lower().endswith(".map"):
            continue
        try:
            w, h = parse_map_header(os.path.join(maps_dir, fn))
            ww, wh = world_bounds(w, h, layout)
            out.append({
                "name": fn,
                "w": w,
                "h": h,
                "world_w": ww,
                "world_h": wh,
                "ladder": map_ladder(w, h, layout),
            })
        except Exception:
            continue
    out.sort(key=lambda m: m["name"])
    return out


def load_catalog(catalog_dir: str) -> dict:
    """Load map-catalog.json (from build_map_catalog.py) into
    {map_name: doc}.  Returns {} when the dir/file is absent or invalid."""
    if not catalog_dir:
        return {}
    p = os.path.join(catalog_dir, "map-catalog.json")
    if not os.path.exists(p):
        return {}
    try:
        with open(p, encoding="utf-8") as f:
            data = json.load(f)
        return {d.get("name"): d for d in data.get("maps", []) if d.get("name")}
    except Exception:
        return {}


_DROPS_CACHE: dict[str, list[dict]] = {}


def load_drops(envir_dir: str, monster_name: str) -> list[dict]:
    """Parse Envir/MonItems/<monster>.txt into a drop list.

    Lines are '<num>/<den> <item> [count]' (GBK item names).  Returns up to
    12 entries sorted by chance descending: [{item, chance (num/den), count}].
    Falls back to the trailing-digit-stripped name (e.g. '巨象兽8' -> '巨象兽'),
    then to None.  Cached per envir_dir+name.
    """
    key = (envir_dir, monster_name)
    cached = _DROPS_CACHE.get(key)
    if cached is not None:
        return cached
    import re as _re
    candidates = [monster_name]
    stripped = _re.sub(r"\d+$", "", monster_name)
    if stripped != monster_name:
        candidates.append(stripped)
    result: list[dict] = []
    for cand in candidates:
        p = os.path.join(envir_dir, "MonItems", cand + ".txt")
        try:
            raw = open(p, "rb").read()
        except OSError:
            continue
        text = raw.decode("gbk", errors="replace")
        for ln in text.splitlines():
            ln = ln.strip()
            if not ln or ln.startswith(";"):
                continue
            m = _re.match(r"^(\d+)/(\d+)\s+(.+)$", ln)
            if not m:
                continue
            num, den = int(m.group(1)), int(m.group(2))
            rest = m.group(3).split()
            item = rest[0]
            count = int(rest[1]) if len(rest) > 1 and rest[1].isdigit() else 1
            result.append({"item": item, "chance": num / den, "count": count})
        if result:
            break
    result.sort(key=lambda d: (-d["chance"], d["item"]))
    result = result[:12]
    _DROPS_CACHE[key] = result
    return result


def load_entities(envir_dir: str) -> list[dict]:
    """Parse Mud3 server entity data into a flat list of dicts.

    Sources (all GBK-encoded names):
      StartPoint.txt  -> one spawn point per map  (map x y)
      Merchant.txt    -> NPCs                      (name map x y face body)
      MonGen.txt      -> loadgen lines expanding to .gen files in Mon_Def/
                        (map x y name count level attack respawn)
    Returns [{map, x, y, kind, name, face?, body?, count?, level?}].
    Map names are normalised to '<name>.map'.  MonGen's loadgen refs that do
    not resolve to a file are skipped (pending note)."""
    out: list[dict] = []

    def norm(name: str) -> str:
        name = name.strip()
        return name if name.lower().endswith(".map") else name + ".map"

    def lines(path: str):
        try:
            with open(path, "rb") as f:
                raw = f.read()
        except OSError:
            return
        try:
            text = raw.decode("gbk", errors="replace")
        except Exception:
            text = raw.decode("utf-8", errors="replace")
        for ln in text.splitlines():
            ln = ln.strip()
            if not ln or ln.startswith(";"):
                continue
            yield ln

    # spawn points
    sp = os.path.join(envir_dir, "StartPoint.txt")
    for ln in lines(sp):
        parts = ln.split()
        if len(parts) >= 3 and parts[0].replace(".", "", 1).isdigit():
            try:
                out.append({"map": norm(parts[0]), "x": int(parts[1]),
                            "y": int(parts[2]), "kind": "spawn", "name": "出生点"})
            except ValueError:
                pass

    # merchants / NPCs
    mp = os.path.join(envir_dir, "Merchant.txt")
    for ln in lines(mp):
        parts = ln.split()
        if len(parts) < 6:
            continue
        fname, mapn, xs, ys = parts[0], parts[1], parts[2], parts[3]
        if not fname or mapn in ("Map", "map") or not xs.isdigit():
            continue
        try:
            out.append({"map": norm(mapn), "x": int(xs), "y": int(ys),
                        "kind": "npc", "name": parts[4],
                        "face": int(parts[5]) if parts[5].isdigit() else 0,
                        "body": int(parts[6]) if len(parts) > 6 and parts[6].isdigit() else 0})
        except (ValueError, IndexError):
            continue

    # monsters: MonGen.txt loadgen refs -> .gen files (Mon_Def/ and Envir/)
    mon_gen = os.path.join(envir_dir, "MonGen.txt")
    gen_files: list[str] = []
    for ln in lines(mon_gen):
        low = ln.lower()
        if low.startswith("loadgen"):
            ref = ln.split('"')[1] if '"' in ln else ln.split()[1]
            for base in (os.path.join(envir_dir, "Mon_Def"), envir_dir):
                p = os.path.join(base, ref)
                if os.path.exists(p):
                    gen_files.append(p)
                    break
    for gp in gen_files:
        for ln in lines(gp):
            parts = ln.split()
            if len(parts) < 4:
                continue
            try:
                mon = {"map": norm(parts[0]), "x": int(parts[1]), "y": int(parts[2]),
                       "kind": "monster", "name": parts[3],
                       "count": int(parts[4]) if len(parts) > 4 and parts[4].isdigit() else 1,
                       "level": int(parts[5]) if len(parts) > 5 and parts[5].isdigit() else 0}
            except (ValueError, IndexError):
                continue
            drops = load_drops(envir_dir, mon["name"])
            if drops:
                mon["drops"] = drops
            out.append(mon)
    return out

def main():

    parser = argparse.ArgumentParser(description="Mir3 EI / Zircon Map Viewer")
    parser.add_argument("maps_dir", help="Folder containing .map files")
    parser.add_argument("--data", help="Folder containing WIL / ZL libraries", default=None)
    parser.add_argument("--port", type=int, default=8766, help="HTTP Server Port")
    parser.add_argument("--cache-dir", default=None,
                        help="Disk tile cache dir (default: <maps_dir>/.tilecache; empty disables)")
    parser.add_argument("--catalog", default=None,
                        help="map-catalog.json dir from build_map_catalog.py (enables /api/catalog)")
    parser.add_argument("--envir", default=None,
                        help="Mud3 server Envir dir (enables /api/entities: spawn/NPC/monster positions)")
    parser.add_argument("--thumbs-dir", default=THUMBS_DIR,
                        help="Full-map thumbnail dir (shared with WikiServer/thumb_gen)")
    parser.add_argument("--layout", choices=[LAYOUT_RECT, LAYOUT_ISO], default=LAYOUT_RECT,
                        help="Map projection: rect (axis-aligned, original Mir3.exe) or iso (legacy diamond)")
    args = parser.parse_args()

    data_dir = args.data
    if not data_dir:
        candidates = [
            os.path.join(args.maps_dir, "..", "Data"),
            os.path.join(args.maps_dir, "..", "Data", "Map Data"),
            os.path.join(args.maps_dir, "Data"),
            os.path.join(args.maps_dir, "Data", "Map Data"),
            "/home/tetsuya/development/Zircon/Debug/Client/Data",
            "/home/tetsuya/development/Zircon/Debug/Client/Data/Map Data",
            args.maps_dir
        ]
        for c in candidates:
            if os.path.exists(c):
                data_dir = c
                break

    print(f"[*] Maps directory: {args.maps_dir}")
    ViewerHandler.map_cache = MapCache(args.maps_dir)
    ViewerHandler.pool = FramePool(data_dir)
    cache_dir = args.cache_dir if args.cache_dir is not None else os.path.join(args.maps_dir, ".tilecache")
    ViewerHandler.cache_dir = cache_dir
    ViewerHandler.thumbs_dir = args.thumbs_dir
    ViewerHandler.layout = args.layout
    ViewerHandler.catalog = load_catalog(args.catalog)
    if ViewerHandler.catalog:
        print(f"[*] Catalog: {len(ViewerHandler.catalog)} maps loaded")
    if args.envir:
        ViewerHandler.entities = load_entities(args.envir)
        print(f"[*] Envir entities: {len(ViewerHandler.entities)} loaded")
    else:
        ViewerHandler.entities = []
    os.makedirs(args.thumbs_dir, exist_ok=True)
    print(f"[*] Thumbnails: {args.thumbs_dir}")
    print(f"[*] Tile cache: {cache_dir}")

    server = ThreadingHTTPServer(("0.0.0.0", args.port), ViewerHandler)
    print(f"[*] Map Viewer running on http://127.0.0.1:{args.port}/")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n[*] Stopping server.")


if __name__ == "__main__":
    main()
