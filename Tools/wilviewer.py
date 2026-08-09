#!/usr/bin/env python3
"""wilviewer.py — local web viewer for Mir3 EI client assets.

Serves a browser UI to browse every .wil/.wix image library (items, monsters,
characters, weapons, map tiles...) plus the Sound/*.wav files.

Usage:
    python3 wilviewer.py                 # root auto-detected (see _default_root)
    python3 wilviewer.py --root /path/to/mir3ei --port 8765 --open

Then open http://127.0.0.1:8765 in a browser.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import threading
import webbrowser
import zipfile
from functools import lru_cache
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from io import BytesIO
from pathlib import Path
from urllib.parse import urlparse, parse_qs

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import wilsdk  # noqa: E402

DEFAULT_ROOT = None  # resolved by _default_root() below


def _default_root() -> str:
    """Pick the mir3ei root: $MIR3EI_ROOT, then ../ of this file, then known NAS path."""
    candidates = []
    env = os.environ.get("MIR3EI_ROOT")
    if env:
        candidates.append(env)
    here = os.path.dirname(os.path.abspath(__file__))
    candidates.append(os.path.normpath(os.path.join(here, "..")))
    candidates.append("/home/tetsuya/NAS/TMP/mir3ei")
    for c in candidates:
        if os.path.isdir(os.path.join(c, "Data")):
            return c
    return candidates[0] if candidates else "."


DEFAULT_ROOT = _default_root()
SOUND_DIR = "Sound"
INDEX_LOCK = threading.Lock()
# All roots ever switched to, keyed by data_dir -> AssetIndex.  Lets /api/diff
# compare libraries across two client roots without re-scanning.
ROOTS: dict[str, "AssetIndex"] = {}
PROJECT_ROOT = Path(__file__).resolve().parent.parent
UI_LAYOUT_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/layout.json"
UI_RESOURCE_ANALYSIS_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/window-resource-analysis.json"
UI_CONTROL_RESOURCE_ANALYSIS_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/window-control-resource-analysis.json"
UI_DRAW_CALLS_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/button-draw-calls.json"
UI_WINDOW_DRAW_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/window-base-draw-evidence.json"
UI_VTABLE_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/window-vtable-evidence.json"
UI_VTABLE_BINDINGS_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/window-vtable-bindings.json"
UI_NPC_PAINT_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/npc-paint-evidence.json"
UI_RESOURCE_HANDLE_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/window-resource-handle-bindings.json"
UI_GLOBAL_CONTROLS_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/global-control-constructor-catalog.json"
UI_RESOURCE_PATH_TABLE_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/resource-path-table.json"
UI_RESOURCE_FAMILY_CATALOG_PATH = PROJECT_ROOT / "docs/research/ei-ui-layout/resource-family-catalog.json"


def load_ui_evidence() -> dict:
    """Load the generated evidence catalog without modifying original assets."""
    try:
        layout = json.loads(UI_LAYOUT_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError) as exc:
        return {"error": f"cannot read {UI_LAYOUT_PATH}: {exc}", "records": []}
    try:
        resource = json.loads(UI_RESOURCE_ANALYSIS_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        resource = {"records": []}
    try:
        control_resource = json.loads(UI_CONTROL_RESOURCE_ANALYSIS_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        control_resource = {"records": []}
    try:
        draw_calls = json.loads(UI_DRAW_CALLS_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        draw_calls = {"all_composition_call_sites": [], "button_renderer": {"draw_calls": []}}
    try:
        window_draw = json.loads(UI_WINDOW_DRAW_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        window_draw = {"routine": {"direct_calls": []}}
    try:
        vtables = json.loads(UI_VTABLE_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        vtables = {"vtable_tables": [], "constructor_assignments": [], "indirect_plus_0xc_calls": []}
    try:
        vtable_bindings = json.loads(UI_VTABLE_BINDINGS_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        vtable_bindings = {"records": []}
    try:
        npc_paint = json.loads(UI_NPC_PAINT_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        npc_paint = {"calls": []}
    try:
        resource_handles = json.loads(UI_RESOURCE_HANDLE_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        resource_handles = {"records": [], "main_ui_resource": {}}
    try:
        global_controls = json.loads(UI_GLOBAL_CONTROLS_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        global_controls = {"records": [], "counts": {}}
    try:
        resource_path_table = json.loads(UI_RESOURCE_PATH_TABLE_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        resource_path_table = {"records": []}
    try:
        resource_family_catalog = json.loads(UI_RESOURCE_FAMILY_CATALOG_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        resource_family_catalog = {"records": [], "counts": {}}
    return {"layout": layout, "window_resource_analysis": resource,
            "window_control_resource_analysis": control_resource,
            "draw_calls": draw_calls,
            "window_base_draw": window_draw,
            "window_vtables": vtables,
            "window_vtable_bindings": vtable_bindings,
            "npc_paint": npc_paint,
            "resource_handle_bindings": resource_handles,
            "global_control_catalog": global_controls,
            "resource_path_table": resource_path_table,
            "resource_family_catalog": resource_family_catalog}


def _looks_like_client(p: str) -> bool:
    """A dir with Data/ or .wil files directly inside."""
    if os.path.isdir(os.path.join(p, "Data")):
        return True
    try:
        n = 0
        for e in os.scandir(p):
            if e.is_file() and e.name.lower().endswith(".wil"):
                return True
            n += 1
            if n > 200:
                break
    except OSError:
        return False
    return False


def discover_roots() -> list[str]:
    """Candidate data roots: env, repo, known NAS clients, and any dir under
    /home/tetsuya/NAS/TMP that looks like a client root."""
    out: list[str] = []
    seen = set()

    def add(p: str):
        p = os.path.normpath(p)
        if p not in seen and os.path.isdir(p):
            seen.add(p)
            out.append(p)

    env = os.environ.get("MIR3EI_ROOT")
    if env:
        add(env)
    here = os.path.dirname(os.path.abspath(__file__))
    add(os.path.normpath(os.path.join(here, "..")))
    add("/home/tetsuya/NAS/TMP/mir3ei")
    add("/home/tetsuya/NAS/TMP/EI传奇3.0客户端")
    parent = "/home/tetsuya/NAS/TMP"
    if os.path.isdir(parent):
        try:
            for e in sorted(os.scandir(parent), key=lambda e: e.name):
                if e.is_dir() and _looks_like_client(e.path):
                    add(e.path)
        except OSError:
            pass
    return out


def switch_root(root: str) -> tuple[str | None, str | None]:
    """Rebuild the asset index for a new root. Returns (data_dir, error)."""
    global INDEX
    root = os.path.normpath(root)
    if not os.path.isdir(root):
        return None, f"no such directory: {root}"
    data_dir = os.path.join(root, "Data") if os.path.isdir(os.path.join(root, "Data")) else root
    try:
        entries = os.listdir(data_dir)
    except OSError as e:
        return None, str(e)
    if not any(f.lower().endswith(".wil") for f in entries):
        return None, f"no .wil libraries under {data_dir}"
    with INDEX_LOCK:
        wilsdk.open_library.cache_clear()   # drop file handles from the old root
        thumb_bytes.cache_clear()           # thumbnails keyed by lib name only
        INDEX = AssetIndex(root)
        ROOTS[INDEX.data_dir] = INDEX       # keep for cross-root compare
    return INDEX.data_dir, None

# ---------------------------------------------------------------- asset index
class AssetIndex:
    def __init__(self, root: str):
        self.root = root
        self.data_dir = os.path.join(root, "Data") if os.path.isdir(os.path.join(root, "Data")) else root
        self.sound_dir = os.path.join(root, SOUND_DIR) if os.path.isdir(os.path.join(root, SOUND_DIR)) else None
        if self.sound_dir is None:
            # root given as <client>/Data → sounds live in <client>/Sound
            parent = os.path.dirname(os.path.normpath(root))
            cand = os.path.join(parent, SOUND_DIR)
            if os.path.isdir(cand):
                self.sound_dir = cand
        self.libs = {}  # name -> WilLibrary
        self._lock = threading.Lock()
        for lib in wilsdk.scan_libraries(self.data_dir):
            self.libs[lib.name] = lib

    def files_payload(self) -> dict:
        libs = []
        for name, lib in self.libs.items():
            try:
                size_mb = os.path.getsize(lib.wil_path) / 1048576
            except OSError:
                size_mb = 0
            libs.append({
                "name": name,
                "category": wilsdk.categorize(name),
                "count": lib.count,
                "size_mb": round(size_mb, 1),
            })
        libs.sort(key=lambda x: (x["category"], x["name"]))
        sounds = []
        if self.sound_dir:
            for f in sorted(os.listdir(self.sound_dir)):
                if f.lower().endswith(".wav"):
                    p = os.path.join(self.sound_dir, f)
                    sounds.append({"name": f, "size_kb": round(os.path.getsize(p) / 1024)})
        return {"root": self.data_dir, "libs": libs, "sounds": sounds}

    def get_lib(self, name: str) -> wilsdk.WilLibrary | None:
        with self._lock:
            lib = self.libs.get(name)
            if lib is None:
                # WIL names are case-insensitive (Windows heritage); help
                # callers that pass the wrong case (e.g. compare page keys).
                lname = name.lower()
                for k, v in self.libs.items():
                    if k.lower() == lname:
                        return v
            return lib


# ------------------------------------------------------------------- helpers
def png_bytes(img) -> bytes:
    buf = BytesIO()
    img.save(buf, format="PNG")
    return buf.getvalue()


def gif_bytes(imgs, fps: int, scale: int, bg="checker") -> bytes:
    return wilsdk.make_gif(imgs, fps, scale, bg)


def json_bytes(obj) -> bytes:
    return json.dumps(obj, ensure_ascii=False).encode("utf-8")


# Pillow import here (lazy) to keep module import cheap for CLI use of wilsdk
from PIL import Image as _PILImage  # noqa: E402

Image_LANCZOS = _PILImage.LANCZOS if hasattr(_PILImage, "LANCZOS") else _PILImage.BILINEAR
Image_NEAREST = _PILImage.NEAREST


def Image_transparent_1x1():
    return _PILImage.new("RGBA", (1, 1), (0, 0, 0, 0))


@lru_cache(maxsize=4096)
def thumb_bytes(data_dir: str, lib_name: str, index: int, size: int):
    """PNG thumbnail for grid cells, or None for blank/fully-transparent frames.
    `data_dir` is part of the cache key so cross-root compare never mixes roots."""
    lib = (ROOTS.get(data_dir) or INDEX).get_lib(lib_name)
    if lib is None:
        return png_bytes(Image_transparent_1x1())
    try:
        im = lib.decode(index)
    except Exception:
        im = None
    if im is None or im.getbbox() is None:  # blank placeholder or no opaque pixel
        return None
    w, h = im.size
    s = size / max(w, h)
    im = im.resize((max(1, round(w * s)), max(1, round(h * s))), Image_NEAREST)
    if im.size != (size, size):
        canvas = _PILImage.new("RGBA", (size, size), (0, 0, 0, 0))
        canvas.paste(im, ((size - im.width) // 2, (size - im.height) // 2), im)
        im = canvas
    return png_bytes(im)


@lru_cache(maxsize=512)
def thumb_strip_bytes(data_dir: str, lib_name: str, start: int, count: int, size: int):
    """One PNG strip of `count` thumbnails starting at `start` (single request
    instead of N round-trips).  Blank frames render as fully transparent cells;
    the page layers its checker background underneath.  Header-only blanks are
    skipped from decode; opaque probes are never needed client-side.
    `data_dir` is part of the cache key so cross-root compare never mixes roots."""
    lib = (ROOTS.get(data_dir) or INDEX).get_lib(lib_name)
    if lib is None:
        return b""
    canvas = _PILImage.new("RGBA", (size * count, size), (0, 0, 0, 0))
    for k in range(count):
        i = start + k
        if i >= lib.count:
            break
        if wilsdk.is_blank(lib, i):
            continue
        try:
            im = lib.decode(i)
        except Exception:
            continue
        if im is None or im.getbbox() is None:
            continue
        w, h = im.size
        s = size / max(w, h)
        im = im.resize((max(1, round(w * s)), max(1, round(h * s))), Image_NEAREST)
        x = k * size + (size - im.width) // 2
        y = (size - im.height) // 2
        canvas.paste(im, (x, y), im)
    return png_bytes(canvas)


def lib_from_dir(data_dir: str, name: str) -> wilsdk.WilLibrary | None:
    """Look up a library in a specific root (for cross-root compare).
    Accepts either a client root or its Data dir; normalizes to Data."""
    data_dir = os.path.normpath(data_dir)
    if not data_dir.endswith("Data"):
        cand = os.path.join(data_dir, "Data")
        if os.path.isdir(cand):
            data_dir = cand
    idx = ROOTS.get(data_dir)
    if idx is None:
        # Not seen yet: try a quick scan without switching the active root.
        if os.path.isdir(data_dir) and any(
                f.lower().endswith(".wil") for f in os.listdir(data_dir)):
            with INDEX_LOCK:
                idx = AssetIndex(data_dir)
                ROOTS[data_dir] = idx
        else:
            return None
    lib = idx.get_lib(name)
    if lib is not None:
        lib._data_dir = data_dir
    return lib


# ------------------------------------------------------------------- handler
class Handler(BaseHTTPRequestHandler):
    server_version = "WilViewer/1.0"

    def log_message(self, fmt, *args):
        pass  # quiet

    def _send(self, data, ctype: str, extra=None):
        if isinstance(data, str):
            data = data.encode("utf-8")
        try:
            self.send_response(200)
            self.send_header("Content-Type", ctype)
            self.send_header("Content-Length", str(len(data)))
            self.send_header("Cache-Control", "no-cache")
            for k, v in (extra or {}).items():
                self.send_header(k, v)
            self.end_headers()
            self.wfile.write(data)
        except (BrokenPipeError, ConnectionResetError):
            pass  # client aborted (e.g. fast scrolling cancels thumbnails)

    def _err(self, code: int, msg: str):
        body = msg.encode("utf-8")
        try:
            self.send_response(code)
            self.send_header("Content-Type", "text/plain; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
        except (BrokenPipeError, ConnectionResetError):
            pass

    def do_GET(self):  # noqa: N802
        parsed = urlparse(self.path)
        q = parse_qs(parsed.query)
        path = parsed.path

        if path == "/":
            self._send(PAGE_HTML, "text/html; charset=utf-8")
            return
        if path == "/ui":
            self._send(UI_LAYOUT_HTML, "text/html; charset=utf-8")
            return
        if path == "/api/ui-layout":
            self._send(json_bytes(load_ui_evidence()), "application/json; charset=utf-8")
            return
        if path == "/compare":
            self._send(COMPARE_HTML, "text/html; charset=utf-8")
            return
        if path == "/api/root":
            self._send(json_bytes({
                "current": INDEX.data_dir,
                "candidates": discover_roots(),
            }), "application/json; charset=utf-8")
            return
        if path == "/api/files":
            r = self._qstr(q, "r")
            if r:
                r = os.path.normpath(r)
                if not r.endswith("Data"):
                    cand = os.path.join(r, "Data")
                    if os.path.isdir(cand):
                        r = cand
                idx = ROOTS.get(r)
                if idx is None and os.path.isdir(r) and any(
                        f.lower().endswith(".wil") for f in os.listdir(r)):
                    with INDEX_LOCK:
                        idx = AssetIndex(r)
                        ROOTS[r] = idx
                if idx is None:
                    self._err(404, f"root not found: {r}")
                    return
                self._send(json_bytes(idx.files_payload()), "application/json; charset=utf-8")
                return
            self._send(json_bytes(INDEX.files_payload()), "application/json; charset=utf-8")
            return
        if path == "/api/image":
            self.api_image(q, download=False)
            return
        if path == "/api/thumb":
            self.api_thumb(q)
            return
        if path == "/api/thumbs":
            self.api_thumbs(q)
            return
        if path == "/api/info":
            self.api_info(q)
            return
        if path == "/api/anim":
            self.api_anim(q)
            return
        if path == "/api/sheet":
            self.api_sheet(q)
            return
        if path == "/api/sound":
            self.api_sound(q)
            return
        if path == "/api/export":
            self.api_export(q)
            return
        if path == "/api/sound-zip":
            self.api_sound_zip(q)
            return
        if path == "/api/ranges":
            self.api_ranges(q)
            return
        if path == "/api/diff":
            self.api_diff(q)
            return
        self._err(404, "not found")

    def do_POST(self):  # noqa: N802
        parsed = urlparse(self.path)
        if parsed.path != "/api/root":
            self._err(404, "not found")
            return
        length = int(self.headers.get("Content-Length", 0) or 0)
        body = self.rfile.read(length) if length else b""
        try:
            req = json.loads(body or b"{}")
        except ValueError:
            self._err(400, "bad json")
            return
        root = (req.get("root") or "").strip()
        data_dir, err = switch_root(root)
        if err:
            self._err(400, err)
            return
        payload = INDEX.files_payload()
        payload["ok"] = True
        payload["root"] = data_dir
        self._send(json_bytes(payload), "application/json; charset=utf-8")

    # -- helpers ---------------------------------------------------------
    def _qint(self, q, key, default):
        try:
            return int(q.get(key, [default])[0])
        except (ValueError, TypeError):
            return default

    def _qstr(self, q, key, default=""):
        return (q.get(key, [default])[0] or default).strip()

    def _qbool(self, q, key, default=False):
        v = self._qstr(q, key, "1" if default else "0").lower()
        return v in ("1", "true", "yes", "on")

    def _lib_or_404(self, q):
        name = q.get("f", [""])[0]
        r = self._qstr(q, "r")
        if r:
            lib = lib_from_dir(r, name)
        else:
            lib = INDEX.get_lib(name)
        if lib is None:
            self._err(404, f"library not found: {name}")
            return None
        if not hasattr(lib, "_data_dir"):
            lib._data_dir = INDEX.data_dir
        return lib

    # -- endpoints ---------------------------------------------------------
    def api_thumb(self, q):
        lib = self._lib_or_404(q)
        if lib is None:
            return
        i = self._qint(q, "i", 0)
        s = min(max(self._qint(q, "s", 48), 8), 256)
        try:
            data = thumb_bytes(getattr(lib, "_data_dir", INDEX.data_dir), lib.name, i, s)
        except Exception as e:
            self._err(500, str(e))
            return
        if data is None:
            self.send_response(204)  # blank frame; no body
            self.send_header("Content-Length", "0")
            self.end_headers()
            return
        self._send(data, "image/png")

    def api_image(self, q, download: bool):
        lib = self._lib_or_404(q)
        if lib is None:
            return
        i = self._qint(q, "i", 0)
        scale = min(max(self._qint(q, "scale", 1), 1), 8)
        bg = self._qstr(q, "bg", "transparent")
        try:
            im = lib.decode(i)
            if im is None:
                im = Image_transparent_1x1()
            if bg != "transparent":
                im = wilsdk._composite_bg(im, bg)
            if scale > 1:
                im = im.resize((im.width * scale, im.height * scale), Image_NEAREST)
            data = png_bytes(im)
        except Exception as e:
            self._err(500, str(e))
            return
        extra = {"Content-Disposition": f'attachment; filename="{lib.name}_{i:05d}.png"'} if download else None
        self._send(data, "image/png", extra)

    def api_info(self, q):
        lib = self._lib_or_404(q)
        if lib is None:
            return
        i = self._qint(q, "i", 0)
        hdr = lib.header(i)
        if hdr is None:
            self._send(json_bytes({"index": i, "blank": True}), "application/json; charset=utf-8")
            return
        self._send(json_bytes(hdr), "application/json; charset=utf-8")

    def api_anim(self, q):
        lib = self._lib_or_404(q)
        if lib is None:
            return
        start = self._qint(q, "start", 0)
        count = min(max(self._qint(q, "count", 12), 1), 300)
        fps = min(max(self._qint(q, "fps", 8), 1), 30)
        scale = min(max(self._qint(q, "scale", 1), 1), 4)
        bg = self._qstr(q, "bg", "checker")
        skipblank = self._qbool(q, "skipblank", True)
        imgs = []
        i = start
        while len(imgs) < count and i < lib.count:
            try:
                im = lib.decode(i)
            except Exception:
                im = None
            if im is not None:
                imgs.append(im)
            elif not skipblank:
                imgs.append(None)
            i += 1
        try:
            data = gif_bytes(imgs, fps, scale, bg)
        except Exception as e:
            self._err(500, str(e))
            return
        if not data:
            data = png_bytes(Image_transparent_1x1())
            self._send(data, "image/png")
            return
        self._send(data, "image/gif")

    def api_sheet(self, q):
        lib = self._lib_or_404(q)
        if lib is None:
            return
        start = self._qint(q, "start", 0)
        count = min(max(self._qint(q, "count", 240), 1), 500)
        cols = min(max(self._qint(q, "cols", 24), 1), 60)
        scale = min(max(self._qint(q, "scale", 1), 1), 4)
        imgs = []
        for i in range(start, min(start + count, lib.count)):
            try:
                im = lib.decode(i)
            except Exception:
                im = None
            imgs.append(im)
        try:
            sheet = wilsdk.contact_sheet(imgs, cols, scale)
            data = png_bytes(sheet)
        except Exception as e:
            self._err(500, str(e))
            return
        self._send(data, "image/png")

    def api_sound(self, q):
        if not INDEX.sound_dir:
            self._err(404, "no Sound directory")
            return
        name = q.get("n", [""])[0]
        if not re.fullmatch(r"[\w.\- ]+\.wav", name, re.IGNORECASE):
            self._err(400, "bad name")
            return
        p = os.path.join(INDEX.sound_dir, name)
        if not os.path.isfile(p):
            self._err(404, "not found")
            return
        try:
            with open(p, "rb") as f:
                data = f.read()
        except OSError as e:
            self._err(500, str(e))
            return
        self._send(data, "audio/wav")

    def api_thumbs(self, q):
        lib = self._lib_or_404(q)
        if lib is None:
            return
        start = max(self._qint(q, "start", 0), 0)
        count = min(max(self._qint(q, "count", 120), 1), 512)
        s = min(max(self._qint(q, "s", 48), 8), 256)
        try:
            data = thumb_strip_bytes(getattr(lib, "_data_dir", INDEX.data_dir), lib.name, start, count, s)
        except Exception as e:
            self._err(500, str(e))
            return
        if not data:
            data = png_bytes(Image_transparent_1x1())
        self._send(data, "image/png")

    def api_ranges(self, q):
        lib = self._lib_or_404(q)
        if lib is None:
            return
        self._send(json_bytes({"ranges": wilsdk.scan_ranges(lib)}),
                   "application/json; charset=utf-8")

    def api_export(self, q):
        lib = self._lib_or_404(q)
        if lib is None:
            return
        f = self._qstr(q, "f")
        scale = min(max(self._qint(q, "scale", 1), 1), 8)
        kind = self._qstr(q, "kind", "png")   # png | sheet
        cols = min(max(self._qint(q, "cols", 24), 1), 60)
        bg = self._qstr(q, "bg", "transparent")
        idxs: list[int] = []
        raw = q.get("i", []) or []
        if len(raw) == 1 and ("," in raw[0] or "-" in raw[0] or ":" in raw[0]):
            raw = [raw[0]]
            for tok in re.split(r"[, ]+", raw[0]):
                tok = tok.strip()
                if not tok:
                    continue
                m = re.fullmatch(r"(\d+)(?:-(\d+))?", tok)
                if m:
                    a = int(m.group(1))
                    b = int(m.group(2)) if m.group(2) else a
                    idxs.extend(range(a, min(b, lib.count - 1) + 1))
        else:
            for tok in raw:
                try:
                    idxs.append(int(tok))
                except ValueError:
                    continue
        if not idxs:
            self._err(400, "no frames selected (use i=1&i=2 or i=1-5)")
            return
        idxs = sorted(set(i for i in idxs if 0 <= i < lib.count))
        if not idxs:
            self._err(400, "no valid frames in range")
            return
        if kind == "sheet":
            imgs = []
            for i in idxs:
                try:
                    im = lib.decode(i)
                except Exception:
                    im = None
                if im is not None and bg != "transparent":
                    im = wilsdk._composite_bg(im, bg)
                imgs.append(im)
            try:
                sheet = wilsdk.contact_sheet(imgs, cols, scale)
                data = png_bytes(sheet)
            except Exception as e:
                self._err(500, str(e))
                return
            self._send(data, "image/png")
            return
        # zip of individual PNGs + manifest.json (mirrors wilextract --meta)
        buf = BytesIO()
        manifest = []
        with zipfile.ZipFile(buf, "w", zipfile.ZIP_DEFLATED) as z:
            for i in idxs:
                try:
                    im = lib.decode(i)
                except Exception:
                    continue
                if im is None:
                    continue
                if bg != "transparent":
                    im = wilsdk._composite_bg(im, bg)
                if scale > 1:
                    im = im.resize((im.width * scale, im.height * scale), Image_NEAREST)
                hdr = lib.header(i)
                z.writestr(f"{f.replace('.wil', '')}_{i:05d}.png", png_bytes(im))
                manifest.append({
                    "index": i, "width": hdr["width"], "height": hdr["height"],
                    "offsetX": hdr["offsetX"], "offsetY": hdr["offsetY"],
                    "shadow": hdr["shadow"], "shadowX": hdr["shadowX"],
                    "shadowY": hdr["shadowY"], "words": hdr["words"],
                })
            z.writestr("manifest.json", json_bytes(manifest))
        data = buf.getvalue()
        self._send(data, "application/zip",
                   {"Content-Disposition": f'attachment; filename="{f.replace(".wil", "")}_{len(idxs)}f.zip"'})

    def api_sound_zip(self, q):
        if not INDEX.sound_dir:
            self._err(404, "no Sound directory")
            return
        names = q.get("n", []) or []
        safe = []
        for name in names:
            if re.fullmatch(r"[\w.\- ]+\.wav", name, re.IGNORECASE):
                p = os.path.join(INDEX.sound_dir, name)
                if os.path.isfile(p):
                    safe.append(name)
        if not safe:
            self._err(400, "no valid sounds selected")
            return
        buf = BytesIO()
        with zipfile.ZipFile(buf, "w", zipfile.ZIP_DEFLATED) as z:
            for name in sorted(safe):
                try:
                    with open(os.path.join(INDEX.sound_dir, name), "rb") as f:
                        z.writestr(name, f.read())
                except OSError:
                    continue
        self._send(buf.getvalue(), "application/zip",
                   {"Content-Disposition": f'attachment; filename="sounds_{len(safe)}.zip"'})

    def api_diff(self, q):
        f = self._qstr(q, "f")
        ra = self._qstr(q, "a")
        rb = self._qstr(q, "b")
        if not f or not ra or not rb:
            self._err(400, "need f, a, b")
            return
        la = lib_from_dir(ra, f)
        lb = lib_from_dir(rb, f)
        if la is None or lb is None:
            self._err(404, f"library not found in one root: {f}")
            return
        c = wilsdk.compare_libraries(la, lb)
        frames = []
        for i in c["differ"]:
            ha, hb = la.header(i), lb.header(i)
            fa = {"blank": ha is None or ha["width"] <= 0}
            fb = {"blank": hb is None or hb["width"] <= 0}
            if not fa["blank"]:
                fa.update({"width": ha["width"], "height": ha["height"]})
            if not fb["blank"]:
                fb.update({"width": hb["width"], "height": hb["height"]})
            frames.append({"i": i, "a": fa, "b": fb})
        self._send(json_bytes({
            "file": f, "root_a": ra, "root_b": rb,
            "a": {"count": la.count}, "b": {"count": lb.count},
            "diff_count": len(c["differ"]),
            "ranges": c["ranges"],
            "missing_a": c["missing_a"], "missing_b": c["missing_b"],
            "frames": frames,
        }), "application/json; charset=utf-8")


# ---------------------------------------------------------------------- page
PAGE_HTML = r"""<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>Mir3 EI Asset Viewer</title>
<style>
  :root { --bg:#15181d; --panel:#1e232b; --panel2:#262c36; --line:#333b47;
          --fg:#d7dde6; --dim:#8b95a3; --acc:#e8a33d; }
  * { box-sizing:border-box; margin:0; padding:0; }
  body { background:var(--bg); color:var(--fg); font:14px/1.5 "PingFang SC","Microsoft YaHei",sans-serif;
         display:flex; height:100vh; overflow:hidden; }
  #sidebar { width:300px; min-width:300px; background:var(--panel); border-right:1px solid var(--line);
             display:flex; flex-direction:column; }
  #sidehead { display:flex; align-items:center; justify-content:space-between; padding:12px 14px;
              border-bottom:1px solid var(--line); }
  #sidebar h1 { font-size:16px; color:var(--acc); }
  #sidehead a { color:var(--dim); font-size:12px; text-decoration:none; }
  #sidehead a:hover { color:var(--acc); }
  #rootrow { display:flex; gap:6px; padding:10px 12px 0; }
  #root { flex:1; min-width:0; padding:6px 8px; background:var(--panel2); border:1px solid var(--line);
          border-radius:6px; color:var(--fg); outline:none; font-size:12px; }
  #rootgo { padding:6px 10px; background:var(--panel2); border:1px solid var(--line); color:var(--acc);
            border-radius:6px; cursor:pointer; font-size:13px; }
  #rootgo:hover { border-color:var(--acc); }
  #search { margin:10px 12px; padding:7px 10px; background:var(--panel2); border:1px solid var(--line);
            border-radius:6px; color:var(--fg); outline:none; }
  #tabs { display:flex; padding:0 12px 8px; gap:6px; }
  #tabs button { flex:1; padding:6px; background:var(--panel2); border:1px solid var(--line); color:var(--dim);
                 border-radius:6px; cursor:pointer; }
  #tabs button.active { color:var(--acc); border-color:var(--acc); }
  #tabs button:disabled { opacity:.4; cursor:default; }
  #tree { flex:1; overflow-y:auto; padding:0 6px 12px; }
  .cat { color:var(--acc); font-weight:bold; font-size:12px; padding:10px 8px 4px; }
  .file { display:flex; justify-content:space-between; padding:5px 8px; border-radius:5px; cursor:pointer; }
  .file:hover { background:var(--panel2); }
  .file.active { background:#2f3b4d; }
  .file .nm { overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
  .file .cnt { color:var(--dim); font-size:12px; margin-left:8px; flex-shrink:0; }
  #main { flex:1; display:flex; flex-direction:column; min-width:0; }
  #toolbar { display:flex; align-items:center; gap:8px; padding:8px 14px; border-bottom:1px solid var(--line);
             background:var(--panel); flex-wrap:wrap; width:100%; box-sizing:border-box;
             max-width:100%; overflow-x:visible; }
  #toolbar .lbl { color:var(--dim); }
  #toolbar select { padding:4px 8px; background:var(--panel2); border:1px solid var(--line);
             color:var(--fg); border-radius:5px; }
  #toolbar input[type=range] { width:110px; accent-color:var(--acc); }
  #toolbar input[type=number] { width:64px; padding:4px 6px; background:var(--panel2); border:1px solid var(--line);
             color:var(--fg); border-radius:5px; }
  #toolbar button { padding:5px 12px; background:var(--panel2); border:1px solid var(--line); color:var(--fg);
             border-radius:5px; cursor:pointer; white-space:nowrap; }
  #toolbar button:hover { border-color:var(--acc); color:var(--acc); }
  #toolbar button:disabled { opacity:.4; cursor:default; }
  #selbar { display:flex; align-items:center; gap:6px; }
  #selbar:empty { display:none; }
  .tsep { width:1px; height:22px; background:var(--line); }
  #gridwrap { flex:1; overflow:auto; padding:14px; }
  #grid { display:grid; gap:4px; justify-content:start; align-content:start;
          grid-template-columns:repeat(auto-fill, var(--cell)); }
  .cell { aspect-ratio:1; border:1px solid var(--line); border-radius:3px; position:relative;
          cursor:pointer; image-rendering:pixelated; background-repeat:no-repeat,repeat;
          background-position:center, 0 0; }
  .cell:hover { border-color:var(--acc); }
  .cell.sel { border-color:var(--acc); box-shadow:inset 0 0 0 2px var(--acc); }
  .cell.focus { outline:2px solid #fff; outline-offset:-2px; }
  .cell .idx { position:absolute; left:2px; bottom:1px; font-size:9px; color:#fff; text-shadow:0 0 2px #000;
               opacity:.75; pointer-events:none; }
  #loadbar { padding:10px 14px; color:var(--dim); text-align:center; border-top:1px solid var(--line);
             background:var(--panel); font-size:12px; display:none; }
  #anim { display:none; padding:8px 14px; border-top:1px solid var(--line); background:var(--panel);
          align-items:center; gap:8px; flex-wrap:wrap; }
  #anim img { image-rendering:pixelated; max-height:180px; }
  #anim input[type=number] { width:64px; padding:4px 6px; background:var(--panel2); border:1px solid var(--line);
             color:var(--fg); border-radius:5px; }
  #anim select { padding:4px 8px; background:var(--panel2); border:1px solid var(--line); color:var(--fg);
             border-radius:5px; }
  .empty { color:var(--dim); padding:30px; text-align:center; }
  #sounds { display:none; flex-direction:column; gap:4px; padding:14px; overflow:auto; }
  #sndbar { display:flex; align-items:center; gap:8px; margin-bottom:8px; }
  #sndsearch { flex:1; padding:7px 10px; background:var(--panel2); border:1px solid var(--line);
               border-radius:6px; color:var(--fg); outline:none; }
  #sndbar button { padding:5px 12px; background:var(--panel2); border:1px solid var(--line); color:var(--fg);
               border-radius:5px; cursor:pointer; }
  #sndbar button:hover { border-color:var(--acc); color:var(--acc); }
  #sndbar button:disabled { opacity:.4; cursor:default; }
  #sounds audio { width:100%; }
  .sound-row { display:flex; align-items:center; gap:10px; padding:6px 8px; background:var(--panel2);
               border-radius:5px; }
  .sound-row .nm { flex:1; color:var(--dim); font-size:12px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
  #overlay { position:fixed; inset:0; background:rgba(0,0,0,.65); display:none; z-index:50; }
  #modal { position:fixed; left:50%; top:50%; transform:translate(-50%,-50%); background:var(--panel);
           border:1px solid var(--line); border-radius:10px; padding:16px; z-index:51; display:none;
           max-width:92vw; max-height:92vh; overflow:auto; }
  #modal h3 { margin-bottom:8px; color:var(--acc); }
  #modal .row { display:flex; gap:14px; flex-wrap:wrap; }
  #modal img { image-rendering:pixelated; background:
      repeating-conic-gradient(#2a2f38 0% 25%, #232830 0% 50%) 0 0/16px 16px; border:1px solid var(--line);
      max-width:70vw; max-height:60vh; }
  #meta { font-size:12px; color:var(--dim); white-space:pre; }
  #modal .btn { display:inline-block; margin-top:10px; padding:6px 14px; background:var(--panel2);
      border:1px solid var(--acc); color:var(--acc); border-radius:6px; text-decoration:none; cursor:pointer;
      font-size:13px; }
  #modal .btn:hover { background:#333c4d; }
  #close { float:right; cursor:pointer; color:var(--dim); font-size:18px; }
  #close:hover { color:var(--fg); }
  #dbar { display:flex; align-items:center; gap:8px; margin-top:8px; flex-wrap:wrap; }
  #dbar select, #dbar input[type=number] { padding:4px 6px; background:var(--panel2); border:1px solid var(--line);
      color:var(--fg); border-radius:5px; }
  #dbar input[type=number] { width:70px; }
  #bookmark-note { padding:4px 6px; background:var(--panel2); border:1px solid var(--line); color:var(--fg);
      border-radius:5px; width:180px; }
  #bkmodal { position:fixed; left:50%; top:50%; transform:translate(-50%,-50%); background:var(--panel);
           border:1px solid var(--line); border-radius:10px; padding:16px; z-index:52; display:none;
           width:560px; max-width:92vw; max-height:80vh; overflow:auto; }
  #bkmodal h3 { color:var(--acc); margin-bottom:8px; }
  #bklist { font-size:13px; }
  .bk-row { display:flex; gap:8px; align-items:center; padding:5px 4px; border-bottom:1px solid #2b333e; }
  .bk-row .bk-go { color:var(--acc); cursor:pointer; }
  .bk-row .bk-del { color:#c96; cursor:pointer; }
  .bk-row .bk-note { color:var(--dim); flex:1; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
  #bkmodal .btn { display:inline-block; margin-top:10px; padding:6px 14px; background:var(--panel2);
      border:1px solid var(--acc); color:var(--acc); border-radius:6px; text-decoration:none; cursor:pointer; }
  #hint { position:fixed; right:14px; bottom:10px; color:var(--dim); font-size:11px; z-index:5; pointer-events:none; }
</style>
</head>
<body>
<aside id="sidebar">
  <div id="sidehead">
    <h1>Mir3 EI Asset Viewer</h1>
    <a href="/compare" title="跨版本资源对比">⇄ Compare</a>
  </div>
  <div id="rootrow">
    <input id="root" list="rootlist" placeholder="Data directory…" spellcheck="false" title="Data directory (e.g. /path/to/client/Data)">
    <datalist id="rootlist"></datalist>
    <button id="rootgo" title="Switch directory">⇄</button>
  </div>
  <input id="search" placeholder="Search files… (e.g. Mon-1, storeitem)">
  <div id="tabs">
    <button id="tab-img" class="active">Images</button>
    <button id="tab-snd">Sounds</button>
  </div>
  <div id="tree"></div>
</aside>
<main id="main">
  <div id="toolbar">
    <span id="cur" class="lbl">Select a library on the left</span>
    <span id="loadinfo" class="lbl"></span>
    <span class="tsep"></span>
    <span class="lbl">Zoom</span><input type="range" id="zoom" min="1" max="8" step="0.5" value="2">
    <span id="zoomval" class="lbl">2×</span>
    <label class="lbl" title="隐藏空白帧"><input type="checkbox" id="hideblank" checked> Blank</label>
    <span class="lbl">跳帧</span><input type="number" id="goframe" min="0" title="跳到指定帧 (Enter)">
    <span class="tsep"></span>
    <span id="selbar"></span>
    <span style="flex:1"></span>
    <button id="btn-bk" title="书签批注">📑</button>
    <button id="btn-hud" style="color:#e8a33d; border-color:#e8a33d; font-weight:bold;">🖥️ UI 组装预览</button>
    <button id="btn-anim">▶ Animate</button>
  </div>
  <div id="anim">
    <span class="lbl">Start</span><input type="number" id="astart" min="0" value="0">
    <span class="lbl">Frames</span><input type="number" id="acount" min="1" value="12">
    <span class="lbl">fps</span><input type="number" id="afps" min="1" max="60" value="8">
    <span class="lbl">Bg</span>
    <select id="abg"><option value="checker">棋盘格</option><option value="black">黑</option><option value="white">白</option></select>
    <select id="arange" title="非空区间建议"></select>
    <button id="play" title="播放 / 暂停显示当前帧">▶ Play</button>
    <button id="astep" title="用选区定义动画 (Shift+点击选帧)">⤢ 选区</button>
    <button id="aseq" title="导出 PNG 序列 (ZIP)">⤓ PNG序列</button>
    <button id="hide-anim">×</button>
    <img id="gif" alt="" title="空格: 暂停/继续 · ←/→: 上一帧/下一帧">
  </div>
  <div id="gridwrap"><div id="grid"></div></div>
  <div id="sounds">
    <div id="sndbar">
      <input id="sndsearch" placeholder="搜索音效… (e.g. monster, magic)">
      <span id="sndcount" class="lbl"></span>
      <button id="sndzip" disabled>⤓ 下载选中</button>
    </div>
    <div id="sndlist"></div>
  </div>
  <div id="loadbar">Loading…</div>
</main>
<div id="overlay"></div>
<div id="modal">
  <span id="close">✕</span>
  <h3 id="mtitle"></h3>
  <div class="row">
    <div><img id="mimg" alt=""></div>
    <div id="meta"></div>
  </div>
  <div id="dbar">
    <button class="btn" id="mprev">◀ 上一帧</button>
    <button class="btn" id="mnext">下一帧 ▶</button>
    <span class="lbl" style="color:var(--dim)">缩放</span>
    <select id="dscale"><option value="1">1×</option><option value="2" selected>2×</option><option value="4">4×</option><option value="8">8×</option></select>
    <span class="lbl" style="color:var(--dim)">背景</span>
    <select id="dbg"><option value="transparent">透明</option><option value="checker" selected>棋盘格</option><option value="white">白</option><option value="black">黑</option></select>
    <button class="btn" id="mcopy" title="复制帧引用 (库名[帧号] 尺寸 锚点)">⧉ 复制引用</button>
    <input id="bookmark-note" placeholder="书签备注…">
    <button class="btn" id="mbk">🔖 存书签</button>
  </div>
  <div style="margin-top:10px">
    <a class="btn" id="mdown" download>Export PNG</a>
    <a class="btn" id="mdown4" download>Export ×4</a>
    <a class="btn" id="mdownzip" download>⤓ 帧 ZIP</a>
  </div>
</div>

<!-- 书签列表 -->
<div id="bkmodal">
  <span id="bk-close" style="float:right; cursor:pointer; color:var(--dim); font-size:18px;">✕</span>
  <h3>📑 书签批注</h3>
  <div id="bklist"></div>
  <button class="btn" id="bk-export">⤓ 导出 JSON</button>
</div>

<!-- 模拟显示屏与 UI 拼装预览 Modal -->
<div id="hud-modal" style="position:fixed; left:50%; top:50%; transform:translate(-50%,-50%); background:#181c24; border:2px solid #e8a33d; border-radius:12px; padding:20px; z-index:60; display:none; max-width:95vw; max-height:95vh; box-shadow:0 0 35px rgba(0,0,0,0.9);">
  <span id="hud-close" style="float:right; cursor:pointer; color:#aaa; font-size:22px; font-weight:bold;">✕</span>
  <h3 style="color:#e8a33d; margin-bottom:12px; display:flex; align-items:center; justify-content:space-between;">
    <span>🖥️ EI 3.0 原版客户端 UI 界面 控件热区与坐标边框拆解</span>
    <label style="font-size:13px; color:#fff; cursor:pointer; font-weight:normal; margin-right:20px;">
      <input type="checkbox" id="chk-show-borders" checked onchange="toggleControlBorders(this.checked)"> 显隐控件碰撞红框 (Show Red Bounding Boxes)
    </label>
  </h3>

  <!-- 800x600 模拟显示器 -->
  <div id="monitor-frame" style="width:800px; height:600px; background:#000; border:12px solid #2a2e38; border-radius:6px; position:relative; overflow:hidden; box-shadow:inset 0 0 20px #000;">
    <div style="position:absolute; inset:0; background:linear-gradient(135deg, #18201a 0%, #0d120f 100%); opacity:0.85;"></div>
    <div style="position:absolute; left:20px; top:20px; color:#445544; font-size:14px; font-family:monospace; pointer-events:none;">
      [Mir3 EI Client Viewport: 800 × 600]
    </div>
    <div id="hud-main-panel" style="position:absolute; left:0; bottom:0; width:800px; height:136px; pointer-events:auto;">
      <img id="part-bg" src="" style="position:absolute; left:0; top:0; width:800px; height:136px; z-index:1;" title="主框架底座 (GameInter[50])" alt="">
      <div class="ui-ctrl-box" style="position:absolute; left:59px; top:16px; width:56px; height:110px; border:1.5px solid red; overflow:hidden; z-index:2;" title="HP血球控件 (Index 60) Rect:(59, 480, 56, 110)">
        <img id="part-hp-ball" src="" style="position:absolute; left:0; bottom:0; width:56px; height:110px;" alt="">
      </div>
      <div class="ui-ctrl-box" style="position:absolute; left:115px; top:16px; width:56px; height:110px; border:1.5px solid blue; overflow:hidden; z-index:2;" title="MP魔球控件 (Index 61) Rect:(115, 480, 56, 110)">
        <img id="part-mp-ball" src="" style="position:absolute; left:0; bottom:0; width:56px; height:110px;" alt="">
      </div>
      <div class="ui-ctrl-box" style="position:absolute; left:350px; top:11px; width:164px; height:6px; border:1.5px solid #ff0; overflow:hidden; z-index:3;" title="经验条控件 (Index 63) Rect:(350, 475, 164, 6)">
        <img id="part-exp-line" src="" style="position:absolute; left:0; top:0; width:164px; height:6px;" alt="">
      </div>
      <div class="ui-ctrl-box" style="position:absolute; left:200px; top:20px; width:380px; height:100px; border:1.5px dashed cyan; z-index:4; pointer-events:auto; cursor:pointer;" title="聊天日志与文本输入区域 (ChatText & InputArea) Rect:(200, 484, 380, 100)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:204px; top:2px; width:24px; height:16px; border:1.5px solid red; z-index:5; cursor:pointer;" title="交换窗口 (Frame 80/81) Rect:(204, 467, 24, 16)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:228px; top:2px; width:24px; height:16px; border:1.5px solid red; z-index:5; cursor:pointer;" title="小地图入口 (Frame 82/83) Rect:(228, 467, 24, 16)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:252px; top:2px; width:24px; height:16px; border:1.5px solid red; z-index:5; cursor:pointer;" title="技能入口 (Frame 84/85) Rect:(252, 467, 24, 16)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:161px; top:46px; width:28px; height:26px; border:1.5px solid red; z-index:5; cursor:pointer;" title="退出 (Frame 90/91) Rect:(161, 511, 28, 26)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:161px; top:82px; width:28px; height:26px; border:1.5px solid red; z-index:5; cursor:pointer;" title="登出 (Frame 92/93) Rect:(161, 547, 28, 26)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:616px; top:47px; width:28px; height:26px; border:1.5px solid red; z-index:5; cursor:pointer;" title="组队 (Frame 94/95) Rect:(616, 512, 28, 26)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:616px; top:82px; width:28px; height:26px; border:1.5px solid red; z-index:5; cursor:pointer;" title="行会 (Frame 96/97) Rect:(616, 547, 28, 26)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:703px; top:16px; width:40px; height:38px; border:1.5px solid red; z-index:5; cursor:pointer;" title="技能窗口 (Frame 100/101) Rect:(703, 481, 40, 38)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:718px; top:32px; width:40px; height:38px; border:1.5px solid red; z-index:5; cursor:pointer;" title="聊天窗口 (Frame 102/103) Rect:(718, 497, 40, 38)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:718px; top:70px; width:40px; height:38px; border:1.5px solid red; z-index:5; cursor:pointer;" title="任务窗口 (Frame 104/105) Rect:(718, 535, 40, 38)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:703px; top:85px; width:40px; height:38px; border:1.5px solid red; z-index:5; cursor:pointer;" title="选项 (Frame 106/107) Rect:(703, 550, 40, 38)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:664px; top:86px; width:40px; height:38px; border:1.5px solid red; z-index:5; cursor:pointer;" title="组队附属入口 (Frame 108/109) Rect:(664, 551, 40, 38)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:648px; top:70px; width:40px; height:38px; border:1.5px solid red; z-index:5; cursor:pointer;" title="人物状态 (Frame 110/111) Rect:(648, 535, 40, 38)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:648px; top:32px; width:40px; height:38px; border:1.5px solid red; z-index:5; cursor:pointer;" title="背包 (Frame 112/113) Rect:(648, 497, 40, 38)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:665px; top:16px; width:40px; height:38px; border:1.5px solid red; z-index:5; cursor:pointer;" title="商店 (Frame 114/115) Rect:(665, 481, 40, 38)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:651px; top:54px; width:28px; height:28px; border:1.5px solid gold; z-index:6; cursor:pointer; border-radius:50%;" title="中心挂锁/退出 (Index 109) Rect:(651, 518, 28, 28)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:578px; top:18px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="跑步切替 (Index 91)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:596px; top:30px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="攻击模式 (Index 90)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:578px; top:48px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="交易请求 (Index 92)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:596px; top:64px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="行会面板 (Index 93)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:578px; top:82px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="声名查看 (Index 94)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:596px; top:96px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="退出游戏 (Index 95)"></div>
    </div>
    <div class="ui-ctrl-box" style="position:absolute; left:672px; top:0; width:128px; height:128px; border:2px solid red; background:rgba(0,0,0,0.5); display:flex; flex-direction:column; align-items:center; justify-content:center; color:#e8a33d; font-size:12px;">
      <span>MiniMap destination</span>
      <span style="font-size:10px; color:#aaa; margin-top:4px;">Rect: (672, 0, 128, 128)</span>
    </div>
  </div>
  <div style="margin-top:12px; display:flex; justify-content:space-between; align-items:center;">
    <div id="hud-inspector" style="font-size:13px; color:#aaa; font-family:monospace;">
      💡 提示：已开启 [红框/彩框] 控件检测，悬停或点击任意红框查看控件响应矩形 Rect(X, Y, W, H)。
    </div>
    <div style="color:#e8a33d; font-size:12px;">Standard Resolution: 800 × 600</div>
  </div>
</div>
<div id="hint">双击帧看详情 · Shift 点击区间选择 · Ctrl 点击追加 · ←→↑↓ 移动 · Enter 详情 · G 跳帧 · Esc 关闭</div>
<script>
const $ = s => document.querySelector(s);
let STATE = { lib:null, count:0, loaded:0, per:120, loading:false, all:null, gen:0,
              sel:new Set(), focus:null, anchor:null, blankSet:null, ranges:[],
              animTimer:null, animPaused:false, animPos:0, detail:null };
const gw = $('#gridwrap');
const cellSize = () => Math.max(24, Math.floor(48 * (+$('#zoom').value) / 2));
const kfmt = n => n >= 10000 ? (n/1000).toFixed(1) + 'k' : String(n);
const esc = s => String(s).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const libBase = () => (STATE.lib || '').replace(/\.wil$/i, '');

// ---------------------------------------------------------------- hash state
function saveHash(){
  const p = new URLSearchParams();
  if (STATE.lib) p.set('file', STATE.lib);
  p.set('zoom', $('#zoom').value);
  p.set('blank', $('#hideblank').checked ? '1' : '0');
  p.set('tab', $('#tab-img').classList.contains('active') ? 'img' : 'snd');
  if (STATE.focus != null) p.set('frame', String(STATE.focus));
  if (STATE.sel.size) p.set('sel', [...STATE.sel].sort((a,b)=>a-b).slice(0,400).join(','));
  const a = +$('#astart').value, ac = +$('#acount').value, af = +$('#afps').value;
  if (!isNaN(a)) p.set('a', String(a));
  if (!isNaN(ac)) p.set('ac', String(ac));
  if (!isNaN(af)) p.set('af', String(af));
  if ($('#hud-modal').style.display === 'block') p.set('hud', '1');
  history.replaceState(null, '', '#' + p.toString());
}
let hashTimer = null;
function queueHash(){ clearTimeout(hashTimer); hashTimer = setTimeout(saveHash, 250); }

function restoreFromHash(){
  if (!STATE.all || !STATE.all.libs) return;
  const h = new URLSearchParams(location.hash.slice(1));
  const file = h.get('file');
  if (file){
    const target = STATE.all.libs.find(l => l.name.toLowerCase() === file.toLowerCase() || l.name.toLowerCase() === (file + '.wil').toLowerCase());
    if (target){
      selectLib(target.name, false);
      const zoom = h.get('zoom'); if (zoom) $('#zoom').value = zoom;
      $('#hideblank').checked = h.get('blank') !== '0';
      applyCellSize();
      const tab = h.get('tab'); if (tab === 'snd') $('#tab-snd').click(); else $('#tab-img').click();
      const a = h.get('a'), ac = h.get('ac'), af = h.get('af');
      if (a != null) $('#astart').value = a;
      if (ac != null) $('#acount').value = ac;
      if (af != null) $('#afps').value = af;
      const sel = h.get('sel');
      if (sel){
        for (const tok of sel.split(',')){
          const m = tok.match(/^(\d+)(?:-(\d+))?$/);
          if (!m) continue;
          const a = +m[1], b = m[2] ? +m[2] : a;
          for (let k = a; k <= b; k++){
            if (k >= 0 && k < STATE.count) STATE.sel.add(k);
          }
        }
      }
      renderSelUI();
      const frame = h.get('frame');
      if (frame != null){
        setTimeout(() => scrollToFrame(+frame), 50);
        setTimeout(() => scrollToFrame(+frame), 400);
      }
    }
  }
  if (h.get('hud') === '1' || localStorage.getItem('hud_preview_open') === '1') openHudPreview();
}
window.addEventListener('hashchange', restoreFromHash);

// ---------------------------------------------------------------- roots / tree
async function loadFiles(){
  const r = await fetch('/api/files');
  const d = await r.json();
  STATE.all = d;
  renderTree(d.libs);
  if (d.sounds.length) renderSounds(d.sounds);
  else $('#tab-snd').disabled = true;
  restoreFromHash();
}
async function loadRoots(){
  try {
    const r = await fetch('/api/root');
    const d = await r.json();
    const dl = $('#rootlist'); dl.innerHTML = '';
    for (const c of d.candidates){
      const o = document.createElement('option'); o.value = c; dl.appendChild(o);
    }
    $('#root').value = d.current;
  } catch (e) { /* server older than this feature */ }
}
async function switchRoot(){
  const v = $('#root').value.trim();
  if (!v) return;
  $('#rootgo').disabled = true;
  try {
    const r = await fetch('/api/root', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ root: v }),
    });
    const d = await r.json();
    if (!r.ok){ alert(d); return; }
    STATE = { ...STATE, all: d, lib: null, count: 0, loaded: 0, loading: false, gen: STATE.gen + 1,
              sel: new Set(), focus: null, anchor: null, blankSet: null, ranges: [] };
    $('#root').value = d.root;
    $('#cur').textContent = 'Select a library on the left';
    $('#grid').innerHTML = ''; $('#loadinfo').textContent = ''; $('#anim').style.display = 'none';
    renderTree(d.libs);
    if (d.sounds && d.sounds.length){ renderSounds(d.sounds); $('#tab-snd').disabled = false; }
    else { $('#tab-snd').disabled = true; $('#sndlist').innerHTML = ''; }
    saveHash();
  } catch (e) { alert('switch failed: ' + e); }
  finally { $('#rootgo').disabled = false; }
}
$('#rootgo').onclick = switchRoot;
$('#root').addEventListener('keydown', e => { if (e.key === 'Enter') switchRoot(); });
function renderTree(libs){
  const tree = $('#tree'); tree.innerHTML = '';
  let cur = null;
  for (const l of libs){
    if (l.category !== cur){ cur = l.category;
      const c = document.createElement('div'); c.className='cat'; c.textContent = l.category; tree.appendChild(c); }
    const d = document.createElement('div'); d.className='file';
    d.innerHTML = `<span class="nm"></span><span class="cnt"></span>`;
    d.querySelector('.nm').textContent = l.name.replace(/\.wil$/i,'');
    d.querySelector('.cnt').textContent = kfmt(l.count);
    d.title = `${l.name} · ${l.count} frames · ${l.size_mb} MB`;
    d.onclick = () => { selectLib(l.name); setActive(d); };
    tree.appendChild(d);
  }
}
function setActive(d){ document.querySelectorAll('.file.active').forEach(x=>x.classList.remove('active')); d.classList.add('active'); }
function selectLib(name, updateHash = true){
  const lib = STATE.all.libs.find(l=>l.name===name); if(!lib) return;
  STATE = {...STATE, lib:name, count:lib.count, loaded:0, loading:false, gen: STATE.gen + 1,
           sel:new Set(), focus:null, anchor:null, blankSet:null, ranges:[]};
  $('#cur').textContent = `${lib.name} · ${lib.count} frames · ${lib.size_mb} MB`;
  $('#loadinfo').textContent = '';
  $('#anim').style.display='none';
  stopAnim();
  $('#astart').value = 0; $('#acount').value = 12;
  gw.scrollTop = 0;
  $('#grid').innerHTML = '';
  loadRanges();
  loadMore();
  renderSelUI();
  if (updateHash) saveHash();
}
// ---------------------------------------------------------------- ranges / blank
async function loadRanges(){
  try {
    const r = await fetch(`/api/ranges?f=${encodeURIComponent(STATE.lib)}`);
    const d = await r.json();
    STATE.ranges = d.ranges || [];
    const bs = new Set(); let prev = 0;
    for (const rg of STATE.ranges){ for (let i=prev;i<rg.start;i++) bs.add(i); prev = rg.end; }
    for (let i=prev;i<STATE.count;i++) bs.add(i);
    STATE.blankSet = bs;
    fillRangeSelect();
  } catch (e) { STATE.blankSet = null; }
}
function fillRangeSelect(){
  const s = $('#arange'); s.innerHTML = '';
  const opts = (STATE.ranges || []).filter(r => r.count >= 2);
  if (!opts.length){
    const o = document.createElement('option'); o.value=''; o.textContent='无连续非空区间'; s.appendChild(o);
    return;
  }
  let focusIdx = -1;
  for (const r of opts){
    const o = document.createElement('option');
    o.value = `${r.start},${r.count}`;
    o.textContent = `F${r.start}-${r.end-1} (${r.count})`;
    s.appendChild(o);
    if (STATE.focus != null && STATE.focus >= r.start && STATE.focus < r.end && focusIdx < 0){
      focusIdx = s.options.length - 1;
    }
  }
  s.selectedIndex = focusIdx >= 0 ? focusIdx : 0;
}
// ---------------------------------------------------------------- grid (strips)
const CHECKER = 'repeating-conic-gradient(#2a2f38 0% 25%, #232830 0% 50%)';
function gridCols(cell){ return Math.max(1, Math.floor(gw.clientWidth / (cell + 4))); }
function loadMore(){
  if (!STATE.lib || STATE.loading || STATE.loaded >= STATE.count) return;
  STATE.loading = true;
  const cell = cellSize();
  const cols = gridCols(cell);
  const strip = Math.max(cols, cols * Math.max(2, Math.ceil((gw.clientHeight + 300) / (cell + 4))));
  const start = STATE.loaded, end = Math.min(start + strip, STATE.count);
  const gen = STATE.gen;
  const hideBlank = $('#hideblank').checked && STATE.blankSet;
  const img = new Image();
  img.onload = () => {
    if (gen !== STATE.gen) return;
    const g = $('#grid');
    g.style.setProperty('--cell', cell + 'px');
    const frag = document.createDocumentFragment();
    for (let k = 0; k < end - start; k++){
      const i = start + k;
      if (hideBlank && STATE.blankSet.has(i)) continue;
      const d = document.createElement('div'); d.className='cell'; d.dataset.idx = i;
      d.style.backgroundImage = `url("${img.src}")` + ', ' + CHECKER;
      d.style.backgroundSize = `${(end - start) * cell}px ${cell}px, 16px 16px`;
      d.style.backgroundPosition = `-${k * cell}px 0, 0 0`;
      const t = document.createElement('span'); t.className='idx'; t.textContent = i; d.appendChild(t);
      d.onclick = ev => onCellClick(i, ev);
      d.ondblclick = ev => { ev.stopPropagation(); openDetail(i); };
      frag.appendChild(d);
    }
    STATE.loaded = end;
    g.appendChild(frag);
    STATE.loading = false;
    refreshCellClasses();
    $('#loadinfo').textContent = `${g.children.length} / ${STATE.count}`;
    if (STATE.loaded >= STATE.count){ $('#loadbar').style.display='none'; return; }
    const needMore = g.scrollHeight <= gw.clientHeight;
    if (needMore || gw.scrollTop + gw.clientHeight >= gw.scrollHeight - 800) loadMore();
  };
  img.onerror = () => { STATE.loading = false; };
  img.src = `/api/thumbs?f=${encodeURIComponent(STATE.lib)}&start=${start}&count=${end - start}&s=${cell}`;
}
gw.addEventListener('scroll', () => {
  if (gw.scrollTop + gw.clientHeight >= gw.scrollHeight - 800) loadMore();
  saveScrollHash();
});
let scrollTimer = null;
function saveScrollHash(){
  if (!STATE.lib) return;
  clearTimeout(scrollTimer);
  scrollTimer = setTimeout(() => {
    const cell = cellSize() + 4, cols = gridCols(cellSize());
    const row = Math.max(0, Math.floor(gw.scrollTop / cell));
    STATE.focus = row * cols;
    queueHash();
  }, 300);
}
function reloadGrid(){
  if (!STATE.lib) return;
  STATE.loaded = 0; STATE.loading = false; STATE.gen++;
  $('#grid').innerHTML = '';
  gw.scrollTop = 0;
  loadMore();
}
function applyCellSize(){
  const cell = cellSize();
  $('#grid').style.setProperty('--cell', cell + 'px');
  $('#zoomval').textContent = $('#zoom').value + '×';
}
$('#zoom').oninput = applyCellSize;
$('#zoom').onchange = reloadGrid;
$('#hideblank').onchange = reloadGrid;
// ---------------------------------------------------------------- selection
function refreshCellClasses(){
  document.querySelectorAll('#grid .cell').forEach(d => {
    const i = +d.dataset.idx;
    d.classList.toggle('sel', STATE.sel.has(i));
    d.classList.toggle('focus', STATE.focus === i);
  });
}
function onCellClick(i, ev){
  ev.preventDefault();
  if (ev.shiftKey && STATE.anchor != null){
    const [a, b] = [Math.min(STATE.anchor, i), Math.max(STATE.anchor, i)];
    for (let k = a; k <= b; k++){
      if (STATE.blankSet && STATE.blankSet.has(k) && $('#hideblank').checked) continue;
      STATE.sel.add(k);
    }
  } else if (ev.ctrlKey || ev.metaKey){
    STATE.sel.has(i) ? STATE.sel.delete(i) : STATE.sel.add(i);
    STATE.anchor = i;
  } else {
    if (ev.detail > 1) return; // double click handled separately
    STATE.sel.clear(); STATE.sel.add(i); STATE.anchor = i;
  }
  STATE.focus = i;
  refreshCellClasses();
  renderSelUI();
  queueHash();
}
function scrollToFrame(i){
  if (!STATE.lib || i < 0 || i >= STATE.count) return;
  STATE.focus = i;
  const cell = cellSize() + 4, cols = gridCols(cellSize());
  const row = Math.floor(i / cols);
  gw.scrollTop = Math.max(0, row * cell - 40);
  const need = Math.min(STATE.count, Math.ceil((row * cell + cell) / cell) * cols + cols * 4);
  if (STATE.loaded < need){
    const old = STATE.loaded;
    STATE.loading = false;
    STATE.loaded = Math.max(STATE.loaded, need - STATE.loaded);
    // force-load enough strips: load in batches until loaded covers need
    const target = Math.min(STATE.count, need);
    const loop = async () => {
      while (STATE.loaded < target && !STATE.loading){
        const s = STATE.loaded;
        await loadBatch(s);
        if (STATE.loaded === s) break;
      }
      setTimeout(() => { refreshCellClasses(); scrollToFrame(i); }, 60);
    };
    loop();
  } else {
    refreshCellClasses();
    const el = document.querySelector(`#grid .cell[data-idx="${i}"]`);
    if (el) el.scrollIntoView({ block: 'nearest' });
  }
  queueHash();
}
function loadBatch(start){
  return new Promise(res => {
    const cell = cellSize();
    const cols = gridCols(cell);
    const strip = Math.max(cols, cols * Math.max(2, Math.ceil((gw.clientHeight + 300) / (cell + 4))));
    const end = Math.min(start + strip, STATE.count);
    const gen = STATE.gen;
    const img = new Image();
    img.onload = () => {
      if (gen !== STATE.gen) return res(false);
      const g = $('#grid');
      const frag = document.createDocumentFragment();
      const hideBlank = $('#hideblank').checked && STATE.blankSet;
      for (let k = 0; k < end - start; k++){
        const i = start + k;
        if (hideBlank && STATE.blankSet.has(i)) continue;
        const d = document.createElement('div'); d.className='cell'; d.dataset.idx = i;
        d.style.backgroundImage = `url("${img.src}")` + ', ' + CHECKER;
        d.style.backgroundSize = `${(end - start) * cell}px ${cell}px, 16px 16px`;
        d.style.backgroundPosition = `-${k * cell}px 0, 0 0`;
        const t = document.createElement('span'); t.className='idx'; t.textContent = i; d.appendChild(t);
        d.onclick = ev => onCellClick(i, ev);
        d.ondblclick = ev => { ev.stopPropagation(); openDetail(i); };
        frag.appendChild(d);
      }
      g.appendChild(frag);
      STATE.loaded = end;
      res(true);
    };
    img.onerror = () => res(false);
    img.src = `/api/thumbs?f=${encodeURIComponent(STATE.lib)}&start=${start}&count=${end - start}&s=${cell}`;
  });
}
function renderSelUI(){
  const bar = $('#selbar');
  const n = STATE.sel.size;
  if (!n){ bar.innerHTML = ''; return; }
  bar.innerHTML = `<span class="lbl">选中 <b style="color:var(--acc)">${n}</b></span>` +
    `<button id="sel-anim" title="用选中帧定义动画">▶ 动画</button>` +
    `<button id="sel-zip" title="导出选中帧为 PNG ZIP">⤓ ZIP</button>` +
    `<button id="sel-sheet" title="导出选中帧雪碧图">▦ 雪碧图</button>` +
    `<button id="sel-clear" title="清空选中">✕</button>`;
  $('#sel-anim').onclick = () => { selToAnim(); };
  $('#sel-zip').onclick = () => { selExport('png'); };
  $('#sel-sheet').onclick = () => { selExport('sheet'); };
  $('#sel-clear').onclick = () => { STATE.sel.clear(); refreshCellClasses(); renderSelUI(); queueHash(); };
}
function selCompress(){
  const arr = [...STATE.sel].sort((a,b)=>a-b);
  const parts = []; let runStart = arr[0];
  for (let i = 1; i <= arr.length; i++){
    if (i === arr.length || arr[i] !== arr[i-1] + 1){
      parts.push(arr[i-1] === runStart ? String(runStart) : `${runStart}-${arr[i-1]}`);
      runStart = arr[i];
    }
  }
  return parts.join(',');
}
function selExport(kind){
  if (!STATE.sel.size) return;
  const url = `/api/export?f=${encodeURIComponent(STATE.lib)}&kind=${kind}&scale=${Math.max(1, Math.min(8, Math.round(+$('#zoom').value)))}` +
    (kind === 'sheet' ? `&cols=24&bg=checker` : '') + `&i=${selCompress()}`;
  const a = document.createElement('a'); a.href = url;
  a.download = kind === 'sheet' ? `${libBase()}_sheet.png` : `${libBase()}_sel_${STATE.sel.size}f.zip`;
  document.body.appendChild(a); a.click(); a.remove();
}
// ---------------------------------------------------------------- keyboard
document.addEventListener('keydown', e => {
  if (e.target.matches('input, select, textarea')) {
    if (e.key === 'Enter' && e.target.id === 'goframe'){ jumpToFrame(); }
    if (e.key === 'Escape'){ e.target.blur(); }
    return;
  }
  const cell = cellSize() + 4, cols = gridCols(cellSize());
  const cur = STATE.focus != null ? STATE.focus : 0;
  let next = null;
  switch (e.key){
    case 'ArrowRight': next = Math.min(STATE.count - 1, cur + 1); break;
    case 'ArrowLeft':  next = Math.max(0, cur - 1); break;
    case 'ArrowDown':  next = Math.min(STATE.count - 1, cur + cols); break;
    case 'ArrowUp':    next = Math.max(0, cur - cols); break;
    case 'Enter': if (STATE.focus != null) openDetail(STATE.focus); return;
    case 'Escape': closeModal(); closeHudModal(); $('#bkmodal').style.display='none'; $('#overlay').style.display='none'; return;
    case 'g': case 'G':
      $('#goframe').focus(); $('#goframe').select(); e.preventDefault(); return;
    case '+': case '=': $('#zoom').value = Math.min(8, +$('#zoom').value + 0.5); applyCellSize(); reloadGrid(); return;
    case '-': $('#zoom').value = Math.max(1, +$('#zoom').value - 0.5); applyCellSize(); reloadGrid(); return;
    case ' ': e.preventDefault(); toggleAnimPause(); return;
    case 'ArrowLeft': case 'ArrowRight': break;
    default: return;
  }
  if (next != null){
    e.preventDefault();
    if (!e.shiftKey){ STATE.sel.clear(); STATE.anchor = next; }
    STATE.sel.add(next);
    STATE.focus = next;
    scrollToFrame(next);
    refreshCellClasses();
    renderSelUI();
    queueHash();
  }
});
$('#goframe').addEventListener('keydown', e => { if (e.key === 'Enter') jumpToFrame(); });
function jumpToFrame(){
  const v = Math.max(0, Math.min(STATE.count - 1, Math.floor(+$('#goframe').value || 0)));
  scrollToFrame(v);
  $('#goframe').value = '';
}
// ---------------------------------------------------------------- detail modal
function openDetail(i){
  if (!STATE.lib || i < 0 || i >= STATE.count) return;
  STATE.detail = i;
  $('#mtitle').textContent = `${STATE.lib} · frame #${i}`;
  renderDetail();
  $('#overlay').style.display='block'; $('#modal').style.display='block';
}
function renderDetail(){
  const i = STATE.detail;
  const scale = +$('#dscale').value, bg = $('#dbg').value;
  fetch(`/api/info?f=${encodeURIComponent(STATE.lib)}&i=${i}`).then(r=>r.json()).then(h=>{
    $('#mimg').src = `/api/image?f=${encodeURIComponent(STATE.lib)}&i=${i}&scale=${scale}&bg=${bg}`;
    const base = `/api/image?f=${encodeURIComponent(STATE.lib)}&i=${i}&scale=1&bg=${bg}`;
    $('#mdown').href = base; $('#mdown').setAttribute('download', `${libBase()}_${i}.png`);
    $('#mdown4').href = `/api/image?f=${encodeURIComponent(STATE.lib)}&i=${i}&scale=4&bg=${bg}`;
    $('#mdown4').setAttribute('download', `${libBase()}_${i}_x4.png`);
    $('#mdownzip').href = `/api/export?f=${encodeURIComponent(STATE.lib)}&kind=png&scale=4&bg=${bg}&i=${i}`;
    $('#mdownzip').setAttribute('download', `${libBase()}_${i}.zip`);
    $('#mprev').style.visibility = i > 0 ? 'visible' : 'hidden';
    $('#mnext').style.visibility = i < STATE.count - 1 ? 'visible' : 'hidden';
    const bk = (getBookmarks()[STATE.lib] || {})[i];
    $('#bookmark-note').value = bk || '';
    if (h.blank){ $('#meta').textContent = 'Blank placeholder frame (index 0)'; }
    else {
      $('#meta').textContent =
`Size: ${h.width} × ${h.height}
Anchor: x=${h.offsetX}  y=${h.offsetY}
Shadow: ${h.shadow?'yes':'no'} (${h.shadowX}, ${h.shadowY})
Data: ${h.words} words (${h.bytes} B)`;
    }
  });
}
$('#dscale').onchange = renderDetail;
$('#dbg').onchange = renderDetail;
$('#mprev').onclick = () => { STATE.detail--; openDetail(STATE.detail); };
$('#mnext').onclick = () => { STATE.detail++; openDetail(STATE.detail); };
$('#mcopy').onclick = async () => {
  const i = STATE.detail;
  const h = await (await fetch(`/api/info?f=${encodeURIComponent(STATE.lib)}&i=${i}`)).json();
  const ref = `${libBase()}[${i}] // ${h.width}x${h.height} anchor(${h.offsetX},${h.offsetY})${h.shadow ? ` shadow(${h.shadowX},${h.shadowY})` : ''}`;
  try { await navigator.clipboard.writeText(ref); $('#mcopy').textContent = '✓ 已复制'; setTimeout(()=>$('#mcopy').textContent='⧉ 复制引用', 1200); }
  catch (e) { prompt('复制帧引用:', ref); }
};
$('#overlay').onclick = () => { closeModal(); closeHudModal(); $('#bkmodal').style.display='none'; $('#overlay').style.display='none'; };
$('#close').onclick = closeModal;
function closeModal(){ $('#overlay').style.display='none'; $('#modal').style.display='none'; }
// ---------------------------------------------------------------- bookmarks
const BK_KEY = 'wilviewer_bookmarks_v1';
function getBookmarks(){
  try { return JSON.parse(localStorage.getItem(BK_KEY) || '{}'); } catch (e) { return {}; }
}
function saveBookmarks(b){ localStorage.setItem(BK_KEY, JSON.stringify(b)); }
$('#mbk').onclick = () => {
  const i = STATE.detail, note = $('#bookmark-note').value.trim();
  if (!note){ alert('备注不能为空'); return; }
  const b = getBookmarks();
  b[STATE.lib] = b[STATE.lib] || {};
  b[STATE.lib][i] = note;
  saveBookmarks(b);
  $('#mbk').textContent = '✓ 已存'; setTimeout(()=>$('#mbk').textContent='🔖 存书签', 1200);
};
$('#btn-bk').onclick = openBkModal;
function openBkModal(){
  const list = $('#bklist'); list.innerHTML = '';
  const b = getBookmarks();
  let n = 0;
  for (const [lib, frames] of Object.entries(b)){
    for (const [fr, note] of Object.entries(frames)){
      n++;
      const row = document.createElement('div'); row.className='bk-row';
      row.innerHTML = `<span class="bk-go">${esc(lib.replace(/\.wil$/i,''))}[${fr}]</span><span class="bk-note">${esc(note)}</span><span class="bk-del">✕</span>`;
      row.querySelector('.bk-go').onclick = () => {
        $('#bkmodal').style.display='none'; $('#overlay').style.display='none';
        const target = STATE.all.libs.find(l => l.name.toLowerCase() === lib.toLowerCase());
        if (target && STATE.lib !== target.name){ selectLib(target.name); }
        scrollToFrame(+fr);
        setTimeout(()=>openDetail(+fr), 500);
      };
      row.querySelector('.bk-del').onclick = () => {
        delete b[lib][fr];
        if (!Object.keys(b[lib]).length) delete b[lib];
        saveBookmarks(b);
        openBkModal();
      };
      list.appendChild(row);
    }
  }
  if (!n) list.innerHTML = '<div class="empty">暂无书签 — 打开帧详情后填写备注并保存</div>';
  $('#bkmodal').style.display='block'; $('#overlay').style.display='block';
}
$('#bk-close').onclick = () => { $('#bkmodal').style.display='none'; $('#overlay').style.display='none'; };
$('#bk-export').onclick = () => {
  const blob = new Blob([JSON.stringify(getBookmarks(), null, 2)], { type: 'application/json' });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = `wilviewer_bookmarks_${new Date().toISOString().slice(0,10)}.json`;
  a.click();
  setTimeout(()=>URL.revokeObjectURL(a.href), 3000);
};
// ---------------------------------------------------------------- animation
function stopAnim(){
  if (STATE.animTimer){ clearInterval(STATE.animTimer); STATE.animTimer = null; }
  STATE.animPaused = false; STATE.animPos = 0;
  $('#play').textContent = '▶ Play';
}
function selToAnim(){
  if (!STATE.sel.size) return;
  const arr = [...STATE.sel].sort((a,b)=>a-b);
  $('#astart').value = arr[0];
  $('#acount').value = arr[arr.length-1] - arr[0] + 1;
  $('#anim').style.display='flex';
  playAnim();
}
$('#btn-anim').onclick = () => {
  $('#anim').style.display='flex';
  const cell = cellSize();
  const colsVis = gridCols(cell);
  const row = Math.max(0, Math.floor(gw.scrollTop / (cell + 4)));
  const guess = Math.min(STATE.count - 1, row * colsVis);
  // 优先当前可视帧所在的非空区间，其次下一个区间
  let start = guess;
  if (STATE.ranges && STATE.ranges.length){
    let rg = STATE.ranges.find(r => guess >= r.start && guess < r.end);
    if (!rg) rg = STATE.ranges.find(r => r.start >= guess) || STATE.ranges[0];
    start = rg ? rg.start : guess;
  }
  $('#astart').value = Math.max(0, start);
  if (!isNaN(+$('#acount').value) && +$('#acount').value > 1 && $('#acount').value !== '12'){}
  $('#afps').value = $('#afps').value || 8;
  playAnim();
};
$('#hide-anim').onclick = () => { $('#anim').style.display='none'; stopAnim(); };
function animUrl(){
  const start = Math.max(0, Math.floor(+$('#astart').value || 0));
  const count = Math.max(1, Math.floor(+$('#acount').value || 1));
  const fps = Math.min(60, Math.max(1, Math.floor(+$('#afps').value || 8)));
  const scale = Math.max(1, Math.min(4, Math.round(+$('#zoom').value)));
  return `/api/anim?f=${encodeURIComponent(STATE.lib)}&start=${start}&count=${count}&fps=${fps}&scale=${scale}&bg=${$('#abg').value}&skipblank=1`;
}
function playAnim(){
  stopAnim();
  $('#gif').src = animUrl();
  $('#play').textContent = '⏸ 暂停';
  $('#play').onclick = toggleAnimPause;
  queueHash();
}
function toggleAnimPause(){
  if (STATE.animTimer){ stopAnim(); }
  else {
    const start = Math.max(0, Math.floor(+$('#astart').value || 0));
    STATE.animPaused = true;
    STATE.animPos = start;
    const scale = Math.max(1, Math.min(8, Math.round(+$('#zoom').value)));
    const step = () => {
      $('#gif').src = `/api/image?f=${encodeURIComponent(STATE.lib)}&i=${STATE.animPos}&scale=${scale}&bg=${$('#abg').value}`;
    };
    step();
    STATE.animTimer = setInterval(() => {
      STATE.animPos++;
      if (STATE.animPos >= Math.min(STATE.count, start + Math.floor(+$('#acount').value || 12))) STATE.animPos = start;
      step();
    }, Math.max(16, Math.floor(1000 / Math.min(60, Math.max(1, Math.floor(+$('#afps').value || 8))))));
    $('#play').textContent = '⏸ 逐帧';
  }
}
$('#astart').oninput = queueHash; $('#acount').oninput = queueHash; $('#afps').oninput = queueHash; $('#abg').onchange = queueHash;
$('#arange').onchange = () => {
  const v = $('#arange').value; if (!v) return;
  const [st, cnt] = v.split(',').map(Number);
  $('#astart').value = st; $('#acount').value = cnt;
};
$('#aseq').onclick = () => {
  const start = Math.max(0, Math.floor(+$('#astart').value || 0));
  const count = Math.max(1, Math.floor(+$('#acount').value || 1));
  const scale = Math.max(1, Math.min(8, Math.round(+$('#zoom').value)));
  const a = document.createElement('a');
  a.href = `/api/export?f=${encodeURIComponent(STATE.lib)}&kind=png&scale=${scale}&bg=transparent&i=${start}-${start+count-1}`;
  a.download = `${libBase()}_anim_${start}-${start+count-1}.zip`;
  document.body.appendChild(a); a.click(); a.remove();
};
// ---------------------------------------------------------------- sounds
let SND = { all: [], sel: new Set() };
function renderSounds(list){
  SND.all = list;
  renderSoundRows(list);
}
function renderSoundRows(list){
  const box = $('#sndlist'); box.innerHTML = '';
  const q = $('#sndsearch').value.trim().toLowerCase();
  const rows = list.filter(s => !q || s.name.toLowerCase().includes(q));
  $('#sndcount').textContent = `${rows.length} / ${list.length}`;
  for (const s of rows){
    const row = document.createElement('div'); row.className='sound-row';
    const cb = document.createElement('input'); cb.type='checkbox';
    cb.checked = SND.sel.has(s.name);
    cb.onchange = () => { SND.sel.has(s.name) ? SND.sel.delete(s.name) : SND.sel.add(s.name); $('#sndzip').disabled = !SND.sel.size; };
    row.appendChild(cb);
    const nm = document.createElement('span'); nm.className='nm';
    nm.textContent = `${s.name} (${s.size_kb} KB)`; row.appendChild(nm);
    const au = document.createElement('audio'); au.controls = true; au.preload='none';
    au.src = `/api/sound?n=${encodeURIComponent(s.name)}`; row.appendChild(au);
    box.appendChild(row);
  }
}
$('#sndsearch').oninput = () => renderSoundRows(SND.all);
$('#sndzip').onclick = () => {
  if (!SND.sel.size) return;
  const a = document.createElement('a');
  a.href = `/api/sound-zip?` + [...SND.sel].map(n => 'n=' + encodeURIComponent(n)).join('&');
  a.download = `sounds_${SND.sel.size}.zip`;
  document.body.appendChild(a); a.click(); a.remove();
};
$('#tab-img').onclick = ()=>{ $('#tab-img').classList.add('active'); $('#tab-snd').classList.remove('active');
  $('#gridwrap').style.display=''; $('#sounds').style.display='none'; queueHash(); };
$('#tab-snd').onclick = ()=>{ $('#tab-snd').classList.add('active'); $('#tab-img').classList.remove('active');
  $('#gridwrap').style.display='none'; $('#sounds').style.display='flex'; queueHash(); };
// ---------------------------------------------------------------- HUD preview
$('#btn-hud').onclick = openHudPreview;
$('#hud-close').onclick = closeHudModal;
function toggleControlBorders(visible){
  document.querySelectorAll('.ui-ctrl-box').forEach(el => {
    el.style.outline = visible ? '' : 'none';
    el.style.borderWidth = visible ? '1.5px' : '0px';
  });
}
function openHudPreview(){
  const lib = 'GameInter.wil';
  const frames = [[50,'part-bg'],[60,'part-hp-ball'],[61,'part-mp-ball'],[63,'part-exp-line']];
  // 跨版本适配: 帧缺失(空白)时显示占位说明而不是裂图
  frames.forEach(([fr, id]) => {
    fetch(`/api/info?f=${encodeURIComponent(lib)}&i=${fr}`).then(r=>r.json()).then(h=>{
      const el = document.getElementById(id);
      if (h.blank || h.width <= 0){
        el.src = '';
        el.title = `GameInter[${fr}] 在当前 root 为空白帧 (版本差异)`;
      } else {
        el.src = `/api/image?f=${encodeURIComponent(lib)}&i=${fr}&scale=1`;
        el.title = `GameInter[${fr}]`;
      }
    }).catch(()=>{});
  });
  $('#overlay').style.display = 'block';
  $('#hud-modal').style.display = 'block';
  localStorage.setItem('hud_preview_open', '1');
  queueHash();
}
function closeHudModal(){
  $('#overlay').style.display = 'none';
  $('#hud-modal').style.display = 'none';
  localStorage.setItem('hud_preview_open', '0');
  queueHash();
}
document.querySelectorAll('#hud-main-panel img, .hud-part-btn').forEach(el => {
  el.onmouseenter = () => {
    const info = el.getAttribute('title') || el.alt || 'MainPanel Element';
    $('#hud-inspector').innerHTML = `<span style="color:#e8a33d; font-weight:bold;">🔍 零部件检测:</span> ${esc(info)}`;
    el.style.outline = '2px solid #e8a33d';
  };
  el.onmouseleave = () => {
    el.style.outline = 'none';
    $('#hud-inspector').innerHTML = '💡 提示：悬停或点击组件查看 C# 代码中 Location(X, Y) 绝对坐标拼装原理。';
  };
});
// ---------------------------------------------------------------- search / boot
$('#search').oninput = function(){
  const q = this.value.trim().toLowerCase();
  document.querySelectorAll('#tree .file').forEach(d=>{
    d.style.display = !q || d.querySelector('.nm').textContent.toLowerCase().includes(q) ? '' : 'none';
  });
};
loadFiles();
loadRoots();
applyCellSize();
</script>
</body>
</html>
"""


# ------------------------------------------------------------------- compare page
COMPARE_HTML = r"""<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>Mir3 EI 跨版本资源对比</title>
<style>
  :root { --bg:#15181d; --panel:#1e232b; --panel2:#262c36; --line:#333b47;
          --fg:#d7dde6; --dim:#8b95a3; --acc:#e8a33d; }
  * { box-sizing:border-box; margin:0; padding:0; }
  body { background:var(--bg); color:var(--fg); font:14px/1.5 "PingFang SC","Microsoft YaHei",sans-serif; padding:20px; }
  header { display:flex; align-items:center; gap:16px; margin-bottom:18px; flex-wrap:wrap; }
  header h1 { color:var(--acc); font-size:19px; }
  header a { color:var(--dim); text-decoration:none; font-size:13px; }
  header a:hover { color:var(--acc); }
  label { color:var(--dim); font-size:13px; }
  select { padding:6px 10px; background:var(--panel2); border:1px solid var(--line); color:var(--fg);
           border-radius:6px; min-width:180px; }
  button { padding:6px 16px; background:var(--panel2); border:1px solid var(--acc); color:var(--acc);
           border-radius:6px; cursor:pointer; }
  button:hover { background:#333c4d; }
  button:disabled { opacity:.4; cursor:default; }
  #panel { background:var(--panel); border:1px solid var(--line); border-radius:10px; padding:16px;
           display:flex; gap:14px; align-items:flex-end; flex-wrap:wrap; margin-bottom:16px; }
  .col { display:flex; flex-direction:column; gap:6px; }
  .col .lbl { font-size:12px; color:var(--acc); font-weight:bold; }
  #stat { color:var(--dim); margin-bottom:14px; }
  #stat b { color:var(--fg); }
  #diffsel { display:flex; gap:10px; flex-wrap:wrap; margin-bottom:14px; }
  .ditem { background:var(--panel2); border:1px solid var(--line); border-radius:6px; padding:10px 14px;
           cursor:pointer; min-width:220px; }
  .ditem:hover { border-color:var(--acc); }
  .ditem .dn { color:var(--acc); font-weight:bold; }
  .ditem .dd { color:var(--dim); font-size:12px; margin-top:4px; }
  #frames { max-height:46vh; overflow:auto; margin-bottom:16px; }
  table { width:100%; border-collapse:collapse; font-size:13px; }
  th,td { padding:5px 10px; text-align:left; border-bottom:1px solid #2b333e; }
  th { color:var(--dim); font-weight:normal; position:sticky; top:0; background:var(--panel); }
  tr.diff { color:#ffb8b0; cursor:pointer; }
  tr.diff:hover { background:var(--panel2); }
  tr.blankrow { color:var(--dim); }
  #view { display:flex; gap:20px; align-items:flex-start; flex-wrap:wrap; }
  .frame-col { background:var(--panel); border:1px solid var(--line); border-radius:10px; padding:12px;
               max-width:48%; }
  .frame-col h3 { font-size:13px; color:var(--acc); margin-bottom:8px; }
  .frame-col img { image-rendering:pixelated; background:
      repeating-conic-gradient(#2a2f38 0% 25%, #232830 0% 50%) 0 0/16px 16px;
      border:1px solid var(--line); max-width:100%; }
  .fc-meta { color:var(--dim); font-size:12px; margin-top:6px; }
  .empty { color:var(--dim); text-align:center; padding:40px; }
</style>
</head>
<body>
<header>
  <h1>⇄ 跨版本资源对比</h1>
  <a href="/">← 返回查看器</a>
  <span style="color:var(--dim); font-size:12px;">对比两个客户端 Data 目录中的同名 .wil 库</span>
</header>
<div id="panel">
  <div class="col">
    <span class="lbl">版本 A (当前 root)</span>
    <select id="rootA"></select>
  </div>
  <div class="col">
    <span class="lbl">版本 B</span>
    <select id="rootB"></select>
  </div>
  <div class="col">
    <span class="lbl">库 (按名匹配)</span>
    <select id="libsel"></select>
  </div>
  <button id="cmp">对比</button>
</div>
<div id="stat"></div>
<div id="diffsel"></div>
<div id="frames"></div>
<div id="view"></div>
<script>
const $ = s => document.querySelector(s);
let A = null, B = null, LIBS = [];
let libA = null, libB = null;

async function loadRoots(){
  const r = await fetch('/api/root');
  const d = await r.json();
  const curRoot = d.current.replace(/\/Data$/, '');
  const cands = d.candidates;
  const fill = (sel, cur) => {
    sel.innerHTML = '';
    for (const c of cands){
      const o = document.createElement('option'); o.value = c; o.textContent = c;
      if (c === cur) o.selected = true;
      sel.appendChild(o);
    }
    if (![...sel.options].some(o => o.selected)){
      sel.value = cands.includes(cur) ? cur : (cands[0] || '');
    }
  };
  fill($('#rootA'), curRoot);
  // rootB 默认选另一个客户端目录（跳过仓库自身与当前 root）
  const isRepo = c => /development[\\/]Zircon/.test(c);
  const other = cands.find(c => c !== curRoot && !isRepo(c))
             || cands.find(c => c !== curRoot)
             || curRoot;
  fill($('#rootB'), other);
  $('#cmp').disabled = false;
  await loadLibs();
}
async function loadLibs(){
  const ra = $('#rootA').value, rb = $('#rootB').value;
  if (!ra || !rb) return;
  let fa, fb;
  try {
    [fa, fb] = await Promise.all([
      fetch(`/api/files?r=${encodeURIComponent(ra)}`).then(r => { if (!r.ok) throw new Error('A: ' + r.status); return r.json(); }),
      fetch(`/api/files?r=${encodeURIComponent(rb)}`).then(r => { if (!r.ok) throw new Error('B: ' + r.status); return r.json(); }),
    ]);
  } catch (e) {
    $('#stat').textContent = '加载库列表失败: ' + e;
    $('#libsel').innerHTML = '';
    LIBS = [];
    $('#cmp').disabled = true;
    return;
  }
  const map = m => new Map(m.libs.map(l => [l.name.toLowerCase(), l]));
  const ma = map(fa), mb = map(fb);
  const common = [...ma.keys()].filter(k => mb.has(k)).sort();
  const sel = $('#libsel'); sel.innerHTML = '';
  for (const k of common){
    const o = document.createElement('option');
    o.value = k;
    o.textContent = `${ma.get(k).name} (${ma.get(k).count} vs ${mb.get(k).count} 帧)`;
    sel.appendChild(o);
  }
  LIBS = common;
  $('#cmp').disabled = !common.length;
}
$('#rootA').onchange = loadLibs;
$('#rootB').onchange = loadLibs;

async function compare(){
  const name = LIBS[$('#libsel').selectedIndex];
  if (!name) return;
  const ra = $('#rootA').value, rb = $('#rootB').value;
  const url = `/api/diff?a=${encodeURIComponent(ra)}&b=${encodeURIComponent(rb)}&f=${encodeURIComponent(name)}`;
  const d = await (await fetch(url)).json();
  $('#stat').innerHTML = `库 <b>${d.file}</b> — A 帧数 <b>${d.a.count}</b>, B 帧数 <b>${d.b.count}</b>, 内容不同的帧 <b style="color:#ffb8b0">${d.diff_count}</b> / ${Math.max(d.a.count, d.b.count)} · 不同区间 <b>${d.ranges.length}</b>`;
  // 区间
  const ds = $('#diffsel'); ds.innerHTML = '';
  if (!d.ranges.length){
    ds.innerHTML = '<div class="empty" style="padding:10px">完全一致</div>';
  }
  d.ranges.forEach((rg, idx) => {
    const el = document.createElement('div'); el.className='ditem';
    el.innerHTML = `<div class="dn">F${rg.start}–${rg.end-1} (${rg.count} 帧)</div>
      <div class="dd">${rg.count >= 2 ? '连续差异区间 — 点击查看' : '单帧差异'}</div>`;
    el.onclick = () => showRange(rg.start, Math.min(rg.end, rg.start + 60));
    ds.appendChild(el);
  });
  // 帧表
  const fw = $('#frames'); fw.innerHTML = '';
  const t = document.createElement('table');
  t.innerHTML = `<thead><tr><th>帧</th><th>A</th><th>B</th><th>说明</th></tr></thead>`;
  const tb = document.createElement('tbody');
  const rows = d.frames.slice(0, 2000);
  for (const fr of rows){
    const tr = document.createElement('tr'); tr.className = 'diff';
    const blankA = fr.a.blank, blankB = fr.b.blank;
    const note = blankA && blankB ? '均空白' : blankA ? 'A 空白' : blankB ? 'B 空白' : `尺寸/锚点/内容不同`;
    tr.innerHTML = `<td>${fr.i}</td><td>${blankA ? '—' : `${fr.a.width}×${fr.a.height}`}</td><td>${blankB ? '—' : `${fr.b.width}×${fr.b.height}`}</td><td>${note}</td>`;
    tr.onclick = () => showFrame(fr.i);
    tb.appendChild(tr);
  }
  t.appendChild(tb); fw.appendChild(t);
}
async function showFrame(i){
  const name = LIBS[$('#libsel').selectedIndex];
  const ra = $('#rootA').value, rb = $('#rootB').value;
  const v = $('#view');
  v.innerHTML = `<div class="frame-col" id="fca"><h3>A</h3><img src="/api/image?f=${encodeURIComponent(name)}&i=${i}&scale=2&r=${encodeURIComponent(ra)}"><div class="fc-meta">loading…</div></div>
                 <div class="frame-col" id="fcb"><h3>B</h3><img src="/api/image?f=${encodeURIComponent(name)}&i=${i}&scale=2&r=${encodeURIComponent(rb)}"><div class="fc-meta">loading…</div></div>`;
  for (const [id, root] of [['fca', ra], ['fcb', rb]]){
    try {
      const h = await (await fetch(`/api/info?f=${encodeURIComponent(name)}&i=${i}&r=${encodeURIComponent(root)}`)).json();
      const col = document.getElementById(id);
      if (h.blank || h.width <= 0){
        col.innerHTML = `<h3>${id === 'fca' ? 'A' : 'B'}</h3><div class="empty" style="padding:20px">空白帧 (无内容)</div>`;
      } else {
        col.querySelector('.fc-meta').textContent = `${h.width}×${h.height} · anchor(${h.offsetX},${h.offsetY}) · ${h.bytes} B`;
      }
    } catch (e) {}
  }
  v.scrollIntoView({ behavior: 'smooth', block: 'start' });
}
function showRange(start, end){
  const name = LIBS[$('#libsel').selectedIndex];
  const ra = $('#rootA').value, rb = $('#rootB').value;
  const v = $('#view');
  let html = '';
  for (let i = start; i <= end; i++){
    html += `<div class="frame-col" style="max-width:220px">
      <h3>F${i}</h3>
      <img loading="lazy" src="/api/image?f=${encodeURIComponent(name)}&i=${i}&scale=1&r=${encodeURIComponent(ra)}">
      <div style="height:4px"></div>
      <img loading="lazy" src="/api/image?f=${encodeURIComponent(name)}&i=${i}&scale=1&r=${encodeURIComponent(rb)}">
    </div>`;
  }
  v.innerHTML = html;
  v.scrollIntoView({ behavior: 'smooth', block: 'start' });
}
$('#cmp').onclick = compare;
loadRoots();
</script>
</body>
</html>
"""


# ---------------------------------------------------------------- evidence UI
UI_LAYOUT_HTML = r"""<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<title>Mir3 EI 原版 UI 证据布局</title>
<style>
  :root { --bg:#11151a; --panel:#1c222a; --line:#3a4552; --fg:#e4e8ed; --dim:#9ba7b4; --acc:#e8a33d; }
  * { box-sizing:border-box; } body { margin:0; background:var(--bg); color:var(--fg); font:14px/1.45 "Microsoft YaHei",sans-serif; }
  header { padding:12px 18px; background:var(--panel); border-bottom:1px solid var(--line); display:flex; gap:18px; align-items:center; flex-wrap:wrap; }
  header h1 { margin:0; color:var(--acc); font-size:18px; } header .hint { color:var(--dim); font-size:12px; }
  header label { color:var(--dim); } header input { accent-color:var(--acc); }
  header select { background:#11151a; color:var(--fg); border:1px solid var(--line); padding:3px 6px; }
  #wrap { padding:18px; display:flex; gap:18px; align-items:flex-start; }
  #screen { width:800px; height:600px; flex:none; position:relative; overflow:hidden; background:linear-gradient(135deg,#18201a,#0d120f); border:8px solid #2a3038; box-shadow:0 0 30px #000; image-rendering:pixelated; }
  #screen img { image-rendering:pixelated; } #world-label { position:absolute; left:16px; top:14px; color:#596d5d; font:12px monospace; pointer-events:none; }
  .diff-overlay { position:absolute; left:0; top:0; width:800px; height:600px; object-fit:fill; z-index:200; pointer-events:none; image-rendering:pixelated; }
  .layer-legend { display:none; position:absolute; right:8px; top:34px; z-index:220; width:250px; padding:7px 9px; background:rgba(10,14,18,.88); border:1px solid #e8a33d; color:#e4e8ed; font:11px/1.45 monospace; pointer-events:none; }
  .show-layers .layer-legend { display:block; }
  .hud-base { position:absolute; left:0; top:465px; width:800px; height:136px; z-index:10; }
  .evidence-button { position:absolute; z-index:20; background:transparent; border:1px solid transparent; cursor:pointer; }
  .debug .evidence-button { border-color:#ff4d4d; background:rgba(255,30,30,.08); }
  .evidence-button img { width:100%; height:100%; display:block; }
  .evidence-window { position:absolute; z-index:30; border:1px solid transparent; overflow:hidden; background:rgba(20,24,30,.25); }
  .debug .evidence-window { border:1px dashed #47d7ff; }
  .evidence-window img { position:absolute; max-width:none; max-height:none; }
  .internal-control { position:absolute; z-index:45; border:1px solid transparent; pointer-events:auto; }
  .debug .internal-control { border:1px dotted #ffb347; background:rgba(255,160,30,.10); }
  .debug .internal-control.outside-control { border-color:#ff5b5b; background:rgba(255,30,30,.12); }
  .debug .internal-control.unresolved-size { border-style:dashed; }
  .focus-geometry { position:absolute; z-index:48; border:1px dashed #62e6a7; background:rgba(40,190,120,.07); pointer-events:none; color:#b8ffd9; font:10px monospace; overflow:visible; }
  .focus-geometry span { position:absolute; left:2px; top:1px; white-space:nowrap; background:rgba(8,18,14,.84); padding:1px 3px; }
  .secondary-control { position:absolute; z-index:35; border:1px solid transparent; pointer-events:auto; }
  .debug .secondary-control { border:1px solid #b78cff; background:rgba(130,70,255,.10); }
  .map-evidence { position:absolute; left:672px; top:0; width:128px; height:128px; z-index:25; border:2px solid #ff4d4d; background:rgba(255,40,40,.08); pointer-events:none; }
  .map-evidence span { position:absolute; left:3px; top:3px; padding:2px 4px; color:#ffd0d0; background:rgba(35,8,8,.88); border:1px solid #ff4d4d; font:10px monospace; white-space:nowrap; }
  .internal-control .button-label { color:#ffcf88; border-color:#ffb347; }
  .window-label,.button-label { display:none; position:absolute; z-index:80; pointer-events:none; background:#10151b; border:1px solid var(--acc); color:#fff; padding:3px 5px; font-size:11px; white-space:nowrap; }
  .debug .window-label,.debug .button-label { display:block; }
  .no-frames .window-label,.no-frames .button-label { display:none !important; }
  #side { width:360px; max-height:600px; overflow:auto; background:var(--panel); border:1px solid var(--line); padding:12px; }
  #side h2 { font-size:14px; color:var(--acc); margin:0 0 8px; } #summary { color:var(--dim); font-size:12px; margin-bottom:10px; }
  .row { padding:7px 4px; border-bottom:1px solid #2b333e; cursor:pointer; } .row:hover { background:#28323e; }
  .row .name { color:#fff; } .row .meta { color:var(--dim); font:11px monospace; }
  .tag { display:inline-block; border:1px solid #677483; color:#b8c3ce; padding:0 3px; margin-left:5px; font-size:10px; }
  .primary { border-color:#47d7ff; color:#47d7ff; }
  @media (max-width:1240px) { #wrap { overflow:auto; } }
</style>
</head>
<body>
<header>
  <h1>Mir3 EI 3.0 原版 UI 证据布局</h1>
  <span class="hint">固定视口 800×600 · 坐标来自 Mir3.exe/WIL · 未确认内容不自动伪装成结论</span>
  <label><input id="debug" type="checkbox" checked> 显示坐标/命中框</label>
  <label><input id="frames" type="checkbox" checked> 显示资源 Frame</label>
  <label>预览模式 <select id="mode"><option value="hud">主 HUD / 窗口组合</option><option value="map-candidate">完整地图资源候选 / FMMap F0</option><option value="window.status">人物状态 / 装备</option><option value="window.inventory">背包 6×6</option><option value="window.other-14-candidate">技能类别 / 技能书</option><option value="window.group">组队</option><option value="window.group-pop-candidate">组队成员信息候选</option><option value="window.guild-candidate">行会</option><option value="window.chat-pop">聊天</option><option value="window.quest">任务</option><option value="window.store-candidate">商店候选 / F1000</option><option value="window.store-state-0">商店状态0 / F1000</option><option value="window.store-state-1">商店状态1 / F1003</option><option value="window.store-state-2">商店状态2 / F1001</option><option value="window.store-state-3">商店状态3 / F1000</option><option value="window.store-state-4">商店状态4 / F1002</option><option value="window.exchange-candidate">交换候选</option><option value="window.option">系统设置</option><option value="window.horse">坐骑</option><option value="window.npc-candidate">NPC 对话候选</option><option value="secondary-0">角色选择/创建候选 A</option><option value="secondary-1">角色选择候选 B</option></select></label>
  <label>差异截图 <input id="diff-file" type="file" accept="image/*"></label>
  <label><input id="diff-show" type="checkbox"> 叠加</label>
  <label>透明度 <input id="diff-opacity" type="range" min="0" max="100" value="50"></label>
  <label><input id="map-rect" type="checkbox"> 显示原版小地图 Rect</label>
  <label><input id="layers" type="checkbox"> 显示绘制层级</label>
  <button id="reset">恢复默认状态</button>
</header>
<div id="wrap"><div id="screen" class="debug"><span id="world-label">[EI 3.0 evidence viewport: 800 × 600]</span></div>
<aside id="side"><h2>布局记录</h2><div id="summary">读取中…</div><div id="records"></div></aside></div>
<script>
const $ = s => document.querySelector(s); const screen = $('#screen');
let DATA = null; let SCREEN_MODE='hud'; let DIFF_URL=''; let MAP_FILE='FMMap.wil'; let MAP_FRAME=0; const key='mir3_evidence_ui_state';
function state(){ try{return JSON.parse(localStorage.getItem(key)||'{}')}catch(e){return {}} }
function save(){localStorage.setItem(key,JSON.stringify({debug:$('#debug').checked,frames:$('#frames').checked,mode:SCREEN_MODE,diffShow:$('#diff-show').checked,diffOpacity:$('#diff-opacity').value,mapRect:$('#map-rect').checked,layers:$('#layers').checked,mapFile:MAP_FILE,mapFrame:MAP_FRAME}));}
function esc(s){return String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));}
function resolve(v,base){return typeof v==='number'?v:(v&&v.offset+(base[v.base]||0));}
function evidenceWindow(id){return (DATA?.layout?.specialized_window_evidence||[]).find(x=>x.window?.id===id||x.id===id||x.window_id===id)||{};}
function addLabel(el,text,klass){const l=document.createElement('span');l.className=klass;l.textContent=text;el.appendChild(l);}
function addMain(){const img=document.createElement('img');img.className='hud-base';img.src='/api/image?f=GameInter.wil&i=50&scale=1';img.title='GameInter.wil Frame 50 · primary-static';screen.appendChild(img);}
function addButton(r,base){const x=resolve(r.position.x,base),y=resolve(r.position.y,base),w=r.size.width,h=r.size.height;const b=document.createElement('div');b.className='evidence-button';b.style.cssText=`left:${x}px;top:${y}px;width:${w}px;height:${h}px`;const im=document.createElement('img');im.src=`/api/image?f=${encodeURIComponent(r.resource.file)}&i=${r.resource.frames.normal}&scale=1`;b.appendChild(im);addLabel(b,`${r.id} · F${r.resource.frames.normal} · (${x},${y},${w},${h})`,'button-label');b.onclick=()=>focusRecord(r.id);screen.appendChild(b);}
function addWindow(r,analysis){const w=r.size.width,h=r.size.height,x=r.position.x,y=r.position.y;const box=document.createElement('div');box.className='evidence-window';box.style.cssText=`left:${x}px;top:${y}px;width:${w}px;height:${h}px`;const a=analysis.find(q=>q.id===r.id);const bb=a&&a.resource&&a.resource.visible_bbox;const im=document.createElement('img');im.src=`/api/image?f=${encodeURIComponent(r.resource.file)}&i=${r.resource.frame}&scale=1`;if(bb)im.style.cssText=`left:${-bb.left}px;top:${-bb.top}px`;box.appendChild(im);const lib=r.resource_handle?.library?.file||'resource unresolved';addLabel(box,`${r.id} · ${lib} · F${r.resource.frame} · (${x},${y},${w},${h})`,'window-label');box.onclick=()=>focusRecord(r.id);screen.appendChild(box);}
function addFocusedGeometry(x,y,w,h,text){const d=document.createElement('div');d.className='focus-geometry';d.style.cssText+=`left:${x}px;top:${y}px;width:${w}px;height:${h}px`;if(text){const s=document.createElement('span');s.textContent=text;d.appendChild(s);}screen.appendChild(d);}
function addFocusedWindow(r,analysis){
  addWindow(r,analysis);
  const controls=(DATA.layout.control_constructors||[]).filter(c=>c.window_id===r.id);
  for(const c of controls)addInternalControl(c,[r],DATA.window_control_resource_analysis?.records||[]);
  const ox=r.position.x,oy=r.position.y;
  if(r.id==='window.inventory'){
    for(let row=0;row<6;row++)for(let col=0;col<6;col++)addFocusedGeometry(ox+25+36*col,oy+41+36*row,36,36,(row===0&&col===0)?'6×6 / 36px':'' );
  }else if(r.id==='window.status'){
    const se=evidenceWindow(r.id);const e=se.equipment_and_attribute_rects||[];
    for(const q of e)addFocusedGeometry(ox+q.left,oy+q.top,q.width,q.height,q.role.replace('-candidate',''));
    const textEvidence=se.attribute_text_draw_chain;
    if(textEvidence){
      for(const q of (textEvidence.first_column_labels||[]))addFocusedGeometry(ox+255,oy+q.y_offset,145,14,q.text);
      for(const q of (textEvidence.second_column_labels||[]))addFocusedGeometry(ox+638,oy+q.y_offset,150,14,q.text);
    }
  }else if(r.id==='window.group'){
    for(let i=0;i<12;i++)addFocusedGeometry(ox+45+100*(i%2),oy+90+20*Math.floor(i/2),92,18,i===0?'成员两列 / 20px':'' );
    const ge=evidenceWindow(r.id),sc=ge.state_text_and_controls?.paint_repositioned_controls?.records||[];
    for(const q of sc)addFocusedGeometry(ox+q.relative_position[0],oy+q.relative_position[1],40,20,`组队控件 ${q.object_offset}`);
    addFocusedGeometry(ox+110,oy+58,70,18,'[允许]/[拒绝] · permission field candidate');
  }else if(r.id==='window.guild-candidate'){
    for(let i=0;i<18;i++)addFocusedGeometry(ox+35,oy+60+20*i,500,18,i===0?'最多18行 / 动态字体行距':'' );
    const ge=evidenceWindow(r.id),pc=ge.paint_repositioned_controls?.records||[];
    for(const q of pc)addFocusedGeometry(ox+q.relative_position[0],oy+q.relative_position[1],40,20,`paint控件 ${q.object_offset}`);
  }else if(r.id==='window.chat-pop'){
    addFocusedGeometry(ox+40,oy+29,491,279,'聊天历史区');addFocusedGeometry(ox+25,oy+311,499,15,'输入区');
    for(let i=0;i<19;i++)addFocusedGeometry(ox+40,oy+29+15*i,491,15,i===0?'19行 / 16字节记录':'' );
  }else if(r.id==='window.quest'){
    for(let i=0;i<19;i++)addFocusedGeometry(ox+65,oy+90+15*i,210,15,i===0?'任务列表 / 15px':'' );
    addFocusedGeometry(ox+65,oy+294,250,16,'任务详情 Frame 705 / 背景候选');
    const qe=evidenceWindow(r.id).detail_text?.body_text_draw;
    if(qe)for(let i=0;i<qe.visible_line_count;i++)addFocusedGeometry(ox+qe.origin.x,oy+qe.origin.y+qe.line_spacing*i,220,15,i===0?'详情正文 / 3行':'' );
  }else if(r.id==='window.other-14-candidate'){
    addFocusedGeometry(ox+15,oy+235,260,75,'技能列表 / 15px行距');
  }else if(r.id==='window.npc-candidate'){
    const ne=evidenceWindow(r.id),pg=ne.paint_geometry||{},loop=pg.dynamic_entry_loop||{};
    const count=ne.window?.dynamic_entry_count_default||13,visible=Math.min(count,6);
    addFocusedGeometry(ox+12,oy+20,528,visible*18,`F1101 / 最多${count}项 / 18px stride candidate`);
    for(let i=0;i<visible;i++)addFocusedGeometry(ox+12,oy+20+18*i,528,18,i===0?'运行时目标字段决定实际屏幕位置':'' );
    addFocusedGeometry(ox+12,oy+20+visible*18,528,16,'F1102 最终项：index=max(count-1,0)，位置字段待解析');
  }else if(r.id==='window.store-candidate'){
    const se=evidenceWindow(r.id),cr=se.constructor_rect_initializers||{};
    const ll=cr.left_list_rects||{count:5,left:28,top_start:26,row_step:49,width:36,height:36};
    for(let i=0;i<ll.count;i++)addFocusedGeometry(ox+ll.left,oy+ll.top_start+ll.row_step*i,ll.right-ll.left,ll.height,i===0?'原始 SetRect 左侧列表 / 5行':'' );
    const lt=cr.left_text_rects||{count:5,left:69,top_start:21,row_step:49,right:256,height:45};
    for(let i=0;i<lt.count;i++)addFocusedGeometry(ox+lt.left,oy+lt.top_start+lt.row_step*i,lt.right-lt.left,lt.height,i===0?'原始 SetRect 说明区 / 5行':'' );
    const rg=cr.right_item_grid_rects||{columns:4,rows:3,x_start:323,y_start:43,x_step:38,y_step:38,width:37,height:37};
    for(let row=0;row<rg.rows;row++)for(let col=0;col<rg.columns;col++)addFocusedGeometry(ox+rg.x_start+rg.x_step*col,oy+rg.y_start+rg.y_step*row,(rg.width||37),(rg.height||37),(row===0&&col===0)?'原始 SetRect 右侧 4×3 / 参数坐标':'' );
    addFocusedGeometry(ox+210,oy+20,75,270,'状态0/3列表面板');
  }
  const label=document.createElement('div');label.style.cssText='position:absolute;left:12px;top:10px;color:#e8a33d;background:rgba(10,14,18,.88);padding:4px 6px;border:1px solid #e8a33d;font:12px monospace;z-index:70';label.textContent=`[${r.id} · original window evidence · fixed 800×600 viewport]`;screen.appendChild(label);
}
function addInternalControl(r,windows,analysis){if(!r.position||r.coordinate_status==='unresolved')return;const pair=r.resource?.frame_pair||[];const w=r.size?.width||20,h=r.size?.height||20;let x=r.position.x,y=r.position.y;const parent=windows.find(q=>q.id===r.window_id);const relative=r.position.coordinate_space==='window-relative';if(relative&&parent){x+=parent.position.x;y+=parent.position.y;}const ca=analysis.find(q=>q.call_va===r.call_va);const libs=ca?.frames?.[0]?.libraries||{};const lib=libs['GameInter.wil']?'GameInter.wil':(libs['Interface1c.wil']?'Interface1c.wil':'GameInter.wil');const box=document.createElement('div');box.className=`internal-control ${r.coordinate_status==='outside-window'?'outside-control':''} ${r.size?'':'unresolved-size'}`;box.style.cssText=`left:${x}px;top:${y}px;width:${w}px;height:${h}px`;if(pair.length){const im=document.createElement('img');im.src=`/api/image?f=${encodeURIComponent(lib)}&i=${pair[0]}&scale=1`;im.alt=`${lib} Frame ${pair[0]}`;im.style.cssText='width:100%;height:100%;image-rendering:pixelated';box.appendChild(im);}addLabel(box,`${r.window_id} · ${lib} F${pair.join('/')} · (${x},${y},${w},${h})${relative?' · window-relative':''} · ${r.coordinate_status}${r.size?'':' · size unresolved'}`,'button-label');box.onclick=()=>focusRecord(r.id);screen.appendChild(box);}
function addSecondaryControl(r){const box=document.createElement('div');const p=r.position,s=r.size;box.className='secondary-control';box.style.cssText=`left:${p.x}px;top:${p.y}px;width:${s.width}px;height:${s.height}px`;addLabel(box,`${r.id} · Interface1c F${r.resource.frame} · (${p.x},${p.y},${s.width},${s.height})`,'button-label');box.onclick=()=>focusRecord(r.id);screen.appendChild(box);}
function addSecondaryScreen(l,index){const candidate=(l.secondary_screen_candidates||[])[index];if(!candidate)return;const bg=candidate.interface1c_frames?.['50']||{width:640,height:480};const image=document.createElement('img');image.src='/api/image?f=Interface1c.wil&i=50&scale=1';image.style.cssText=`position:absolute;left:${(800-bg.width)/2}px;top:${(600-bg.height)/2}px;width:${bg.width}px;height:${bg.height}px`;image.title=`Interface1c.wil Frame 50 · secondary screen candidate ${index}`;screen.appendChild(image);const scope=index===0?'interface1c-cluster-0x456d':'interface1c-cluster-0x4027';for(const r of (l.secondary_control_constructors||[])){if(r.scope!==scope)continue;const copy={...r,position:{...r.position},size:{...r.size}};copy.position.x+=(800-bg.width)/2;copy.position.y+=(600-bg.height)/2;addSecondaryControl(copy);}const label=document.createElement('div');label.style.cssText='position:absolute;left:16px;top:14px;color:#e8a33d;font:12px monospace;z-index:70';label.textContent=`[Interface1c secondary candidate ${index} · 640×480 centered in 800×600]`;screen.appendChild(label);}
function addMapCandidate(l){const image=document.createElement('img');image.src='/api/image?f=FMMap.wil&i=0&scale=1';image.style.cssText='position:absolute;left:0;top:34px;width:800px;height:533px;image-rendering:auto';image.title='FMMap.wil Frame 0 · full-map resource candidate';screen.appendChild(image);const label=document.createElement('div');label.style.cssText='position:absolute;left:14px;top:10px;color:#e8a33d;background:rgba(10,14,18,.88);padding:4px 6px;border:1px solid #e8a33d;font:12px monospace;z-index:70';label.textContent='[FMMap.wil Frame 0 · 1200×800 source scaled to 800×533 · resource candidate only]';screen.appendChild(label);}
function addMapSelectionCandidate(l){const image=document.createElement('img');image.src=`/api/image?f=${encodeURIComponent(MAP_FILE)}&i=${MAP_FRAME}&scale=1`;image.style.cssText='position:absolute;left:0;top:34px;width:800px;height:533px;image-rendering:auto';image.title=`${MAP_FILE} Frame ${MAP_FRAME} · selected resource candidate`;screen.appendChild(image);const label=document.createElement('div');label.style.cssText='position:absolute;left:14px;top:10px;color:#e8a33d;background:rgba(10,14,18,.88);padding:4px 6px;border:1px solid #e8a33d;font:12px monospace;z-index:70';label.textContent=`[${MAP_FILE} Frame ${MAP_FRAME} · selected map resource · scaled candidate only]`;screen.appendChild(label);}
function addStoreStateCandidate(l,stateNo){const e=evidenceWindow('window.store-candidate');const rows=e.state_machine_evidence?.state_transitions||[];const t=rows.find(x=>x.state===stateNo)||rows[0];if(!t)return;const w=t.args.width,h=t.args.height,x=Math.round((800-w)/2),y=Math.round((600-h)/2);const box=document.createElement('div');box.className='evidence-window';box.style.cssText=`left:${x}px;top:${y}px;width:${w}px;height:${h}px`;const im=document.createElement('img');im.src=`/api/image?f=GameInter.wil&i=${t.frame}&scale=1`;im.title=`GameInter.wil Frame ${t.frame} · state ${stateNo}`;box.appendChild(im);addLabel(box,`商店状态${stateNo} · F${t.frame} · 工厂居中候选 (${x},${y},${w},${h})`,'window-label');screen.appendChild(box);const tag=document.createElement('div');tag.style.cssText='position:absolute;left:14px;top:10px;color:#e8a33d;background:rgba(10,14,18,.9);padding:5px 7px;border:1px solid #e8a33d;font:12px monospace;z-index:70';tag.textContent=`[state ${stateNo} · factory call args (${t.args.x},${t.args.y},${t.args.width},${t.args.height}) · final business name unresolved]`;screen.appendChild(tag);const geo=e.state_machine_evidence?.state_dispatch_geometry?.[`state_${stateNo}`];if(geo){const hits=[geo.first_rect_offset,geo.second_rect_offset];hits.forEach((rr,i)=>{if(rr)addFocusedGeometry(x+rr[0],y+rr[1],40,20,`hit${i+1} (${rr[0]},${rr[1]})`);});}}
function addMapMinimapCandidate(l){const r=l.map_ui_evidence?.viewport?.fixed_minimap_widget?.screen_rect||{left:672,top:0,right:800,bottom:128};const frame=document.createElement('div');frame.style.cssText=`position:absolute;left:${r.left}px;top:${r.top}px;width:${r.right-r.left}px;height:${r.bottom-r.top}px;overflow:hidden;background:#08110e;border:2px solid #e8a33d;z-index:24`;const image=document.createElement('img');image.src='/api/image?f=FMMap.wil&i=0&scale=1';image.style.cssText='width:128px;height:85px;margin-top:21px;image-rendering:auto;opacity:.92';frame.appendChild(image);const label=document.createElement('div');label.style.cssText='position:absolute;left:4px;top:4px;color:#ffd58a;background:rgba(10,14,18,.88);padding:2px 3px;font:10px monospace;z-index:2';label.textContent='FMMap F0 → 128×128';frame.appendChild(label);screen.appendChild(frame);const note=document.createElement('div');note.style.cssText='position:absolute;left:14px;top:10px;color:#e8a33d;background:rgba(10,14,18,.88);padding:4px 6px;border:1px solid #e8a33d;font:12px monospace;z-index:70';note.textContent='[固定小地图目标 Rect (672,0)-(800,128) · 资源缩放/裁剪候选，不代表最终边框]';screen.appendChild(note);}
function addPromptEvidence(mode){if(mode==='prompt.confirmation'){const x=220,y=151,w=360,h=190;const box=document.createElement('div');box.style.cssText=`position:absolute;left:${x}px;top:${y}px;width:${w}px;height:${h}px;overflow:hidden;border:1px solid #e8a33d;z-index:24`;const im=document.createElement('img');im.src='/api/image?f=GameInter.wil&i=950&scale=1';im.style.cssText='position:absolute;left:0;top:0;width:360px;height:190px';box.appendChild(im);screen.appendChild(box);[[51,125,44,20,151],[147,125,64,20,157],[244,125,44,20,154]].forEach(([rx,ry,rw,rh,frame])=>{const b=document.createElement('div');b.style.cssText=`position:absolute;left:${x+rx}px;top:${y+ry}px;width:${rw}px;height:${rh}px;border:1px dashed #62e6a7;background:rgba(40,190,120,.08);z-index:48`;const bi=document.createElement('img');bi.src=`/api/image?f=GameInter.wil&i=${frame}&scale=1`;bi.style.cssText='width:100%;height:100%;image-rendering:pixelated';b.appendChild(bi);screen.appendChild(b);});const label=document.createElement('div');label.style.cssText='position:absolute;left:14px;top:10px;color:#e8a33d;background:rgba(10,14,18,.88);padding:4px 6px;border:1px solid #e8a33d;font:12px monospace;z-index:70';label.textContent='[F950 · 360×190 · -1/-1 center rule → (220,151)]';screen.appendChild(label);}else{const box=document.createElement('div');box.style.cssText='position:absolute;left:107px;top:110px;width:584px;height:252px;overflow:hidden;border:1px solid #e8a33d;z-index:24';const im=document.createElement('img');im.src='/api/image?f=GameInter.wil&i=602&scale=1';im.style.cssText='position:absolute;left:0;top:0;width:1024px;height:256px';box.appendChild(im);screen.appendChild(box);[['[行会修改 请自行修改行会等级、成员排行信息]',23,94,'#ffd58a'],['[行会公告，请自行修改公告内容.]',23,94,'#9ed0ff']].forEach(([txt,rx,ry,color],i)=>{const t=document.createElement('div');t.style.cssText=`position:absolute;left:${107+rx}px;top:${110+ry+i*20}px;color:${color};background:rgba(8,12,16,.72);font:12px sans-serif;z-index:48;white-space:nowrap`;t.textContent=`state${i} · ${txt}`;screen.appendChild(t);});const label=document.createElement('div');label.style.cssText='position:absolute;left:14px;top:10px;color:#e8a33d;background:rgba(10,14,18,.88);padding:4px 6px;border:1px solid #e8a33d;font:12px monospace;z-index:70';label.textContent='[F602 · parent (107,110) · 584×252 · 行会公告/行会修改候选文字 · state分支]';screen.appendChild(label);}}
function addDiffOverlay(){if(!DIFF_URL||!$('#diff-show').checked)return;const im=document.createElement('img');im.className='diff-overlay';im.src=DIFF_URL;im.style.opacity=(Number($('#diff-opacity').value||50)/100).toFixed(2);im.title='本地截图差异叠加层 · 不属于原版坐标证据';screen.appendChild(im);}
function addLayerLegend(l){const box=document.createElement('div');box.className='layer-legend';const layers=l.draw_order_evidence?.layers||[];box.innerHTML='<b>原版绘制层级候选</b><br>'+layers.map(x=>`<div>${esc(String(x.order).padStart(3,'0'))} · ${esc(x.id)} <span style="color:#9ba7b4">[${esc(x.confidence||'candidate')}]</span></div>`).join('');screen.appendChild(box);}
function addMapEvidence(l){if(!$('#map-rect').checked)return;const e=l.map_ui_evidence?.viewport?.fixed_minimap_widget;if(!e)return;const r=e.screen_rect;const box=document.createElement('div');box.className='map-evidence';box.style.cssText=`left:${r.left}px;top:${r.top}px;width:${r.right-r.left}px;height:${r.bottom-r.top}px`;const label=document.createElement('span');label.textContent=`MMap target · (${r.left},${r.top}) ${r.right-r.left}×${r.bottom-r.top} · ${e.evidence_level}`;box.appendChild(label);screen.appendChild(box);}
function focusRecord(id){document.querySelectorAll('.row').forEach(x=>x.style.outline='');const r=document.querySelector(`[data-id="${CSS.escape(id)}"]`);if(r){r.scrollIntoView({block:'nearest'});r.style.outline='1px solid #e8a33d';}}
function render(){if(!DATA)return;screen.querySelectorAll(':scope > *:not(#world-label)').forEach(x=>x.remove());const l=DATA.layout,base={'hud.left':0,'hud.top':465},windows=l.records.filter(q=>q.kind==='window'),controlAnalysis=DATA.window_control_resource_analysis?.records||[];if(SCREEN_MODE==='hud'){addMain();addMapEvidence(l);}else if(SCREEN_MODE==='map-candidate')addMapCandidate(l);else addSecondaryScreen(l,Number(SCREEN_MODE.slice(10)));const rec=$('#records');rec.innerHTML='';let buttons=0,windowCount=0;for(const r of l.records){if(r.kind==='button'){if(SCREEN_MODE==='hud')addButton(r,base);buttons++;}if(r.kind==='window'){if(SCREEN_MODE==='hud')addWindow(r,DATA.window_resource_analysis.records||[]);windowCount++;}const row=document.createElement('div');row.className='row';row.dataset.id=r.id;const e=r.evidence||{};const handle=r.resource_handle?.library?.file||r.resource?.file||'';row.innerHTML=`<div class="name">${esc(r.id)} <span class="tag ${e.level&&e.level.startsWith('primary')?'primary':''}">${esc(e.level||'unknown')}</span></div><div class="meta">${r.kind} · ${handle} · Frame ${r.resource?.frame??r.resource?.frames?.normal??'—'}</div>`;row.onclick=()=>focusRecord(r.id);rec.appendChild(row);}const controls=(l.control_constructors||[]);let internal=0;for(const r of controls){if(SCREEN_MODE==='hud')addInternalControl(r,windows,controlAnalysis);if(r.coordinate_status==='inside-window'||r.coordinate_status==='resolved-primary-redraw')internal++;const e=r.evidence||{};const row=document.createElement('div');row.className='row';row.dataset.id=r.id;row.innerHTML=`<div class="name">${esc(r.id)} <span class="tag ${e.level&&e.level.startsWith('primary')?'primary':''}">${esc(e.level||'unknown')}</span></div><div class="meta">window control · Frame pair ${(r.resource?.frame_pair||[]).join('/')||'—'} · ${r.coordinate_status||'position unresolved'}${r.size?' · '+r.size.width+'×'+r.size.height:''}</div>`;row.onclick=()=>focusRecord(r.id);rec.appendChild(row);}const secondary=l.secondary_control_constructors||[];for(const r of secondary){if(SCREEN_MODE!=='hud' && !SCREEN_MODE.startsWith('secondary'))continue;if(SCREEN_MODE==='hud')addSecondaryControl(r);const row=document.createElement('div');row.className='row';row.dataset.id=r.id;row.innerHTML=`<div class="name">${esc(r.id)} <span class="tag primary">primary-static-closed</span></div><div class="meta">${esc(r.resource.file)} · Frame ${r.resource.frame} · (${r.position.x},${r.position.y},${r.size.width}×${r.size.height})</div>`;row.onclick=()=>focusRecord(r.id);rec.appendChild(row);}for(const w of (l.secondary_window_candidates||[])){const row=document.createElement('div');row.className='row';row.dataset.id='secondary-window-'+w.constructor_va;row.innerHTML=`<div class="name">次级窗口候选 ${esc(w.constructor_va)} <span class="tag">candidate</span></div><div class="meta">Frame ${w.frame} · 资源 ${esc(w.resource)} · 原始参数语义待确认</div>`;rec.appendChild(row);}const global=DATA.global_control_catalog||{};const unassigned=(global.records||[]).filter(r=>r.classification==='unassigned-control-candidate');const gr=document.createElement('div');gr.className='row';gr.dataset.id='global-unassigned-controls';gr.innerHTML=`<div class="name">未归属控件候选 <span class="tag">evidence catalog</span></div><div class="meta">全局 0x00417550 调用 ${global.counts?.all||0} · 未归属 ${unassigned.length} · 仅保留反汇编/Frame证据，坐标待绑定</div>`;gr.onclick=()=>focusRecord('global-unassigned-controls');rec.appendChild(gr);const draw=DATA.draw_calls||{};const sites=draw.all_composition_call_sites||[];const baseDraw=DATA.window_base_draw?.routine?.direct_calls||[];const vt=DATA.window_vtables||{};const vRows=vt.vtable_tables||[];const vAssign=vt.constructor_assignments||[];const binds=DATA.window_vtable_bindings?.records||[];const npc=DATA.npc_paint?.calls||[];const handleStatus=DATA.resource_handle_bindings?.main_ui_resource?.wil_file||'resource handle unresolved';const dr=document.createElement('div');dr.className='row';dr.dataset.id='draw-chain';dr.innerHTML=`<div class="name">原版绘制链 <span class="tag primary">primary-static-draw-candidate</span></div><div class="meta">资源 ${esc(handleStatus)} · 按钮 0x4179B0 → 0x45F2D0 · ${sites.length} 个共享合成调用点 · 窗口基类 0x423D00 → ${baseDraw.length} 个分支 · vtable ${vRows.length}/${vAssign.length} · 绑定 ${binds.length} · NPC专用绘制 ${npc.length} 调用</div>`;rec.prepend(dr);$('#summary').textContent=`${buttons} 个按钮 · ${windowCount} 个窗口 · ${controls.length} 个窗口控件构造 · 次级控件 ${secondary.length} · ${internal} 个几何通过调试框 · 未归属控件 ${unassigned.length} · 主 UI 资源 ${handleStatus} · ${sites.length} 个共享合成调用点 · ${baseDraw.length} 个窗口基类绘制分支 · ${vRows.length} 个vtable · ${binds.length} 个窗口绑定候选 · ${l.version}`;addDiffOverlay();addLayerLegend(l);}
function load(){const s=state();if(typeof s.debug==='boolean')$('#debug').checked=s.debug;if(typeof s.frames==='boolean')$('#frames').checked=s.frames;if(typeof s.mode==='string')SCREEN_MODE=s.mode;if(typeof s.diffShow==='boolean')$('#diff-show').checked=s.diffShow;if(s.diffOpacity!=null)$('#diff-opacity').value=s.diffOpacity;if(typeof s.mapRect==='boolean')$('#map-rect').checked=s.mapRect;if(typeof s.layers==='boolean')$('#layers').checked=s.layers;$('#mode').value=SCREEN_MODE;screen.classList.toggle('show-layers',$('#layers').checked);$('#mode').onchange=()=>{SCREEN_MODE=$('#mode').value;save();render()};$('#layers').onchange=()=>{screen.classList.toggle('show-layers',$('#layers').checked);save()};$('#map-rect').onchange=()=>{save();render()};$('#diff-show').onchange=()=>{save();render()};$('#diff-opacity').oninput=()=>{save();render()};$('#diff-file').onchange=()=>{const f=$('#diff-file').files?.[0];if(!f)return;const reader=new FileReader();reader.onload=()=>{DIFF_URL=String(reader.result||'');$('#diff-show').checked=true;save();render()};reader.readAsDataURL(f)};$('#debug').onchange=()=>{screen.classList.toggle('debug',$('#debug').checked);save()};$('#frames').onchange=()=>{screen.classList.toggle('no-frames',!$('#frames').checked);save()};$('#reset').onclick=()=>{localStorage.removeItem(key);location.reload()};fetch('/api/ui-layout').then(r=>r.json()).then(d=>{DATA=d;render()}).catch(e=>$('#summary').textContent='读取布局失败：'+e);}
const renderEvidenceLayout = render;
render = function(){
  if(SCREEN_MODE==='map-candidate'){
    const wanted=SCREEN_MODE;
    SCREEN_MODE='hud';
    renderEvidenceLayout();
    SCREEN_MODE=wanted;
    screen.querySelectorAll(':scope > *:not(#world-label)').forEach(x=>x.remove());
    addMapSelectionCandidate(DATA.layout);
    return;
  }
  if(SCREEN_MODE.startsWith('window.store-state-')){
    const wanted=SCREEN_MODE;SCREEN_MODE='hud';renderEvidenceLayout();SCREEN_MODE=wanted;
    screen.querySelectorAll(':scope > *:not(#world-label)').forEach(x=>x.remove());
    addStoreStateCandidate(DATA.layout,Number(wanted.slice('window.store-state-'.length)));
    return;
  }
  if(SCREEN_MODE==='prompt.confirmation'||SCREEN_MODE==='prompt.notice'){
    screen.querySelectorAll(':scope > *:not(#world-label)').forEach(x=>x.remove());
    addPromptEvidence(SCREEN_MODE);
    return;
  }
  if(SCREEN_MODE==='map-minimap-candidate'){
    const wanted=SCREEN_MODE;
    SCREEN_MODE='hud';
    renderEvidenceLayout();
    SCREEN_MODE=wanted;
    screen.querySelectorAll(':scope > *:not(#world-label)').forEach(x=>x.remove());
    addMapMinimapCandidate(DATA.layout);
    return;
  }
  if(SCREEN_MODE.startsWith('window.')){
    const wanted = SCREEN_MODE;
    SCREEN_MODE = 'hud';
    renderEvidenceLayout();
    SCREEN_MODE = wanted;
    screen.querySelectorAll(':scope > *:not(#world-label)').forEach(x=>x.remove());
    const target = DATA?.layout?.records?.find(r=>r.kind==='window' && r.id===wanted);
    if(target) addFocusedWindow(target, DATA.window_resource_analysis?.records||[]);
    if(wanted==='window.chat-pop'&&target){
      const boxes=[...screen.querySelectorAll('.focus-geometry')];
      boxes.slice(-19).forEach((b,i)=>{b.style.top=`${target.position.y+29+14*i}px`;b.style.left=`${target.position.x+40}px`;b.style.width='491px';b.style.height='14px';const s=b.querySelector('span');if(i===0&&s)s.textContent='19行 / 记录16字节 / 视觉14px';});
    }
    return;
  }
  renderEvidenceLayout();
};
const mapMinimapOption=document.createElement('option');mapMinimapOption.value='map-minimap-candidate';mapMinimapOption.textContent='固定小地图 128×128 候选';$('#mode').appendChild(mapMinimapOption);
const promptConfirmationOption=document.createElement('option');promptConfirmationOption.value='prompt.confirmation';promptConfirmationOption.textContent='确认框 F950 / 居中规则';$('#mode').appendChild(promptConfirmationOption);
const promptNoticeOption=document.createElement('option');promptNoticeOption.value='prompt.notice';promptNoticeOption.textContent='公告框 F602 / 原点证据';$('#mode').appendChild(promptNoticeOption);
const mapSelector=document.createElement('span');
mapSelector.innerHTML='地图资源 <select id="map-library"><option value="FMMap.wil">FMMap.wil（全图）</option><option value="MMap.wil">MMap.wil（小地图）</option></select> Frame <input id="map-frame" type="number" min="0" step="1" value="0" style="width:64px">';
document.querySelector('header').appendChild(mapSelector);
const restored=state();
if(typeof restored.mapFile==='string'&&['FMMap.wil','MMap.wil'].includes(restored.mapFile))MAP_FILE=restored.mapFile;
if(Number.isInteger(restored.mapFrame)&&restored.mapFrame>=0)MAP_FRAME=restored.mapFrame;
$('#map-library').value=MAP_FILE;$('#map-frame').value=MAP_FRAME;
const syncMap=()=>{MAP_FILE=$('#map-library').value;MAP_FRAME=Math.max(0,Number($('#map-frame').value)||0);save();if(SCREEN_MODE==='map-candidate')render();};
$('#map-library').onchange=syncMap;$('#map-frame').oninput=syncMap;
load();
</script></body></html>"""


# --------------------------------------------------------------------- main
def main():
    ap = argparse.ArgumentParser(description="Mir3 EI asset web viewer")
    ap.add_argument("--root", default=DEFAULT_ROOT, help=f"mir3ei directory (default: {DEFAULT_ROOT})")
    ap.add_argument("--port", type=int, default=8765)
    ap.add_argument("--open", action="store_true", help="open browser automatically")
    args = ap.parse_args()

    global INDEX
    INDEX = AssetIndex(args.root)
    ROOTS[INDEX.data_dir] = INDEX
    print(f"root: {INDEX.data_dir}")
    print(f"libraries: {len(INDEX.libs)}  ({sum(l.count for l in INDEX.libs.values())} frames total)")
    if INDEX.sound_dir:
        n = len([f for f in os.listdir(INDEX.sound_dir) if f.lower().endswith('.wav')])
        print(f"sounds: {n} wav files")

    srv = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
    url = f"http://127.0.0.1:{args.port}/"
    print(f"serving on {url}  (Ctrl-C to stop)")
    if args.open:
        webbrowser.open(url)
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        print("\nbye")


if __name__ == "__main__":
    main()
