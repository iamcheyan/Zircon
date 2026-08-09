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
            return self.libs.get(name)


# ------------------------------------------------------------------- helpers
def png_bytes(img) -> bytes:
    buf = BytesIO()
    img.save(buf, format="PNG")
    return buf.getvalue()


def gif_bytes(imgs, fps: int, scale: int) -> bytes:
    return wilsdk.make_gif(imgs, fps, scale)


def json_bytes(obj) -> bytes:
    return json.dumps(obj, ensure_ascii=False).encode("utf-8")


# Pillow import here (lazy) to keep module import cheap for CLI use of wilsdk
from PIL import Image as _PILImage  # noqa: E402

Image_LANCZOS = _PILImage.LANCZOS if hasattr(_PILImage, "LANCZOS") else _PILImage.BILINEAR
Image_NEAREST = _PILImage.NEAREST


def Image_transparent_1x1():
    return _PILImage.new("RGBA", (1, 1), (0, 0, 0, 0))


@lru_cache(maxsize=4096)
def thumb_bytes(lib_name: str, index: int, size: int):
    """PNG thumbnail for grid cells, or None for blank/fully-transparent frames."""
    lib = INDEX.get_lib(lib_name)
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
        if path == "/api/root":
            self._send(json_bytes({
                "current": INDEX.data_dir,
                "candidates": discover_roots(),
            }), "application/json; charset=utf-8")
            return
        if path == "/api/files":
            self._send(json_bytes(INDEX.files_payload()), "application/json; charset=utf-8")
            return
        if path == "/api/image":
            self.api_image(q, download=False)
            return
        if path == "/api/thumb":
            self.api_thumb(q)
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

    def _lib_or_404(self, q):
        name = q.get("f", [""])[0]
        lib = INDEX.get_lib(name)
        if lib is None:
            self._err(404, f"library not found: {name}")
            return None
        return lib

    # -- endpoints ---------------------------------------------------------
    def api_thumb(self, q):
        lib = self._lib_or_404(q)
        if lib is None:
            return
        i = self._qint(q, "i", 0)
        s = min(max(self._qint(q, "s", 48), 8), 256)
        try:
            data = thumb_bytes(lib.name, i, s)
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
        try:
            im = lib.decode(i)
            if im is None:
                im = Image_transparent_1x1()
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
        count = min(max(self._qint(q, "count", 12), 1), 200)
        fps = min(max(self._qint(q, "fps", 8), 1), 30)
        scale = min(max(self._qint(q, "scale", 1), 1), 4)
        imgs = []
        for i in range(start, min(start + count, lib.count)):
            try:
                im = lib.decode(i)
            except Exception:
                im = None
            imgs.append(im)
        try:
            data = gif_bytes(imgs, fps, scale)
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
  #sidebar h1 { font-size:16px; padding:12px 14px; color:var(--acc); border-bottom:1px solid var(--line); }
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
  #tree { flex:1; overflow-y:auto; padding:0 6px 12px; }
  .cat { color:var(--acc); font-weight:bold; font-size:12px; padding:10px 8px 4px; }
  .file { display:flex; justify-content:space-between; padding:5px 8px; border-radius:5px; cursor:pointer; }
  .file:hover { background:var(--panel2); }
  .file.active { background:#2f3b4d; }
  .file .nm { overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
  .file .cnt { color:var(--dim); font-size:12px; margin-left:8px; flex-shrink:0; }
  #main { flex:1; display:flex; flex-direction:column; min-width:0; }
  #toolbar { display:flex; align-items:center; gap:10px; padding:8px 14px; border-bottom:1px solid var(--line);
             background:var(--panel); flex-wrap:wrap; width:100%; box-sizing:border-box;
             max-width:100%; overflow-x:visible; }
  #toolbar .lbl { color:var(--dim); }
  #toolbar select { padding:4px 8px; background:var(--panel2); border:1px solid var(--line);
             color:var(--fg); border-radius:5px; width:70px; }
  #toolbar input[type=range] { width:130px; accent-color:var(--acc); }
  #toolbar button { padding:5px 12px; background:var(--panel2); border:1px solid var(--line); color:var(--fg);
             border-radius:5px; cursor:pointer; }
  #toolbar button:hover { border-color:var(--acc); color:var(--acc); }
  #gridwrap { flex:1; overflow:auto; padding:14px; }
  #grid { display:grid; gap:4px; justify-content:start; align-content:start;
          grid-template-columns:repeat(auto-fill, var(--cell)); }
  .cell { aspect-ratio:1; border:1px solid var(--line); border-radius:3px; position:relative;
          cursor:pointer; image-rendering:pixelated; background-repeat:no-repeat,repeat;
          background-position:center, 0 0; }
  .cell:hover { border-color:var(--acc); }
  .cell .idx { position:absolute; left:2px; bottom:1px; font-size:9px; color:#fff; text-shadow:0 0 2px #000;
               opacity:.75; pointer-events:none; }
  #loadbar { padding:10px 14px; color:var(--dim); text-align:center; border-top:1px solid var(--line);
             background:var(--panel); font-size:12px; display:none; }
  #anim { display:none; padding:8px 14px; border-top:1px solid var(--line); background:var(--panel);
          align-items:center; gap:8px; flex-wrap:wrap; }
  #anim img { image-rendering:pixelated; max-height:180px; }
  .empty { color:var(--dim); padding:30px; text-align:center; }
  #sounds { display:none; flex-direction:column; gap:4px; padding:14px; overflow:auto; }
  #sounds audio { width:100%; }
  .sound-row { display:flex; align-items:center; gap:10px; padding:6px 8px; background:var(--panel2);
               border-radius:5px; }
  .sound-row .nm { flex:1; color:var(--dim); font-size:12px; }
  #overlay { position:fixed; inset:0; background:rgba(0,0,0,.65); display:none; z-index:50; }
  #modal { position:fixed; left:50%; top:50%; transform:translate(-50%,-50%); background:var(--panel);
           border:1px solid var(--line); border-radius:10px; padding:16px; z-index:51; display:none;
           max-width:90vw; max-height:90vh; overflow:auto; }
  #modal h3 { margin-bottom:8px; color:var(--acc); }
  #modal .row { display:flex; gap:14px; flex-wrap:wrap; }
  #modal img { image-rendering:pixelated; background:
      repeating-conic-gradient(#2a2f38 0% 25%, #232830 0% 50%) 0 0/16px 16px; border:1px solid var(--line); }
  #meta { font-size:12px; color:var(--dim); white-space:pre; }
  #modal .btn { display:inline-block; margin-top:10px; padding:6px 14px; background:var(--panel2);
      border:1px solid var(--acc); color:var(--acc); border-radius:6px; text-decoration:none; }
  #close { float:right; cursor:pointer; color:var(--dim); font-size:18px; }
  #close:hover { color:var(--fg); }
</style>
</head>
<body>
<aside id="sidebar">
  <h1>Mir3 EI Asset Viewer</h1>
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
    <span style="flex:1"></span>
    <span class="lbl">Zoom</span><input type="range" id="zoom" min="1" max="8" step="0.5" value="2">
    <span id="zoomval" class="lbl">2×</span>
    <label class="lbl"><input type="checkbox" id="hideblank" checked> Hide blank</label>
    <button id="btn-hud" style="color:#e8a33d; border-color:#e8a33d; font-weight:bold;">🖥️ UI 组装预览</button>
    <button id="btn-anim">▶ Animate</button>
  </div>
  <div id="anim">
    <span class="lbl">Start frame</span><input id="astart" value="0">
    <span class="lbl">Frames</span><input id="acount" value="12">
    <span class="lbl">fps</span><input id="afps" value="8">
    <button id="play">Play</button>
    <button id="hide-anim">Close</button>
    <img id="gif" alt="">
  </div>
  <div id="gridwrap"><div id="grid"></div></div>
  <div id="sounds"></div>
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
  <a class="btn" id="mdown" download>Export PNG</a>
  <a class="btn" id="mdown4" download>Export ×4</a>
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
    <!-- 游戏伪背景 -->
    <div style="position:absolute; inset:0; background:linear-gradient(135deg, #18201a 0%, #0d120f 100%); opacity:0.85;"></div>
    
    <!-- 模拟地图文字 -->
    <div style="position:absolute; left:20px; top:20px; color:#445544; font-size:14px; font-family:monospace; pointer-events:none;">
      [Mir3 EI Client Viewport: 800 × 600]
    </div>

    <!-- HUD 800x136 主面板容器 (贴合在 600px 屏幕最底部) -->
    <div id="hud-main-panel" style="position:absolute; left:0; bottom:0; width:800px; height:136px; pointer-events:auto;">
      <!-- 1. 800x136 一体化底层主金属架 (GameInter Frame 50) -->
      <img id="part-bg" src="" style="position:absolute; left:0; top:0; width:800px; height:136px; z-index:1;" title="主框架底座 (GameInter[50])" alt="">

      <!-- 2. 左侧大红血球 (Frame 60, 宽56x110, 精确 Location: 59, 16) -->
      <div class="ui-ctrl-box" style="position:absolute; left:59px; top:16px; width:56px; height:110px; border:1.5px solid red; overflow:hidden; z-index:2;" title="HP血球控件 (Index 60) Rect:(59, 480, 56, 110)">
        <img id="part-hp-ball" src="" style="position:absolute; left:0; bottom:0; width:56px; height:110px;" alt="">
      </div>

      <!-- 3. 右侧大蓝魔球 (Frame 61, 宽56x110, 精确 Location: 115, 16) -->
      <div class="ui-ctrl-box" style="position:absolute; left:115px; top:16px; width:56px; height:110px; border:1.5px solid blue; overflow:hidden; z-index:2;" title="MP魔球控件 (Index 61) Rect:(115, 480, 56, 110)">
        <img id="part-mp-ball" src="" style="position:absolute; left:0; bottom:0; width:56px; height:110px;" alt="">
      </div>

      <!-- 4. 中间黄色经验长条 (Frame 63, 宽164x6, 精确 Location: 350, 11) -->
      <div class="ui-ctrl-box" style="position:absolute; left:350px; top:11px; width:164px; height:6px; border:1.5px solid #ff0; overflow:hidden; z-index:3;" title="经验条控件 (Index 63) Rect:(350, 475, 164, 6)">
        <img id="part-exp-line" src="" style="position:absolute; left:0; top:0; width:164px; height:6px;" alt="">
      </div>

      <!-- 5. 中间聊天输入与历史信息框控件 (ChatLog / Input Area, 精确 Location: 200, 20, 380x100) -->
      <div class="ui-ctrl-box" style="position:absolute; left:200px; top:20px; width:380px; height:100px; border:1.5px dashed cyan; z-index:4; pointer-events:auto; cursor:pointer;" title="聊天日志与文本输入区域 (ChatText & InputArea) Rect:(200, 484, 380, 100)"></div>

      <!-- 6. 罗盘盘面 13 个功能按钮热区 (基于圆心 665, 68 精确测算) -->
      <div class="ui-ctrl-box" style="position:absolute; left:648px; top:12px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="属性按钮 [F10] (Index 100) Rect:(648, 476, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:668px; top:18px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="装备按钮 [F11] (Index 101) Rect:(668, 482, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:682px; top:32px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="背包按钮 [F9]  (Index 102) Rect:(682, 496, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:686px; top:52px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="技能按钮 [F3]  (Index 105) Rect:(686, 516, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:682px; top:72px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="组队按钮 [F4]  (Index 104) Rect:(682, 536, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:668px; top:88px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="任务按钮 [F7]  (Index 103) Rect:(668, 552, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:648px; top:92px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="帮助按钮 [?]   (Index 108) Rect:(648, 556, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:628px; top:88px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="腰带按钮 [Z]   (Index 107) Rect:(628, 552, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:614px; top:72px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="快捷按钮 [R]   (Index 106) Rect:(614, 536, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:610px; top:52px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="好友按钮 [F]   (Index 111) Rect:(610, 516, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:614px; top:32px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="挂机按钮 [A]   (Index 110) Rect:(614, 496, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:628px; top:18px; width:26px; height:24px; border:1.5px solid red; z-index:5; cursor:pointer;" title="设置按钮 [ESC] (Index 112) Rect:(628, 482, 26, 24)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:651px; top:54px; width:28px; height:28px; border:1.5px solid gold; z-index:6; cursor:pointer; border-radius:50%;" title="中心挂锁/退出 (Index 109) Rect:(651, 518, 28, 28)"></div>

      <!-- 7. 罗盘左侧小绿圈按键 (Index 90..95, 精确 Location) -->
      <div class="ui-ctrl-box" style="position:absolute; left:578px; top:18px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="跑步切替 (Index 91)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:596px; top:30px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="攻击模式 (Index 90)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:578px; top:48px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="交易请求 (Index 92)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:596px; top:64px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="行会面板 (Index 93)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:578px; top:82px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="声名查看 (Index 94)"></div>
      <div class="ui-ctrl-box" style="position:absolute; left:596px; top:96px; width:18px; height:18px; border:1.5px solid #0f0; z-index:5; border-radius:50%;" title="退出游戏 (Index 95)"></div>
    </div>

    <!-- 右上角小地图面板控件 (MiniMap Window) -->
    <div class="ui-ctrl-box" style="position:absolute; right:10px; top:10px; width:140px; height:140px; border:2px solid red; background:rgba(0,0,0,0.5); display:flex; flex-direction:column; align-items:center; justify-content:center; color:#e8a33d; font-size:12px;">
      <span>MiniMap (Index 210)</span>
      <span style="font-size:10px; color:#aaa; margin-top:4px;">TopRight: (650, 10)</span>
    </div>
  </div>

  <!-- 提示卡片 -->
  <div style="margin-top:12px; display:flex; justify-content:space-between; align-items:center;">
    <div id="hud-inspector" style="font-size:13px; color:#aaa; font-family:monospace;">
      💡 提示：已开启 [红框/彩框] 控件检测，悬停或点击任意红框查看控件响应矩形 Rect(X, Y, W, H)。
    </div>
    <div style="color:#e8a33d; font-size:12px;">Standard Resolution: 800 × 600</div>
  </div>
</div>
<script>
const $ = s => document.querySelector(s);
let STATE = { lib:null, count:0, loaded:0, per:120, loading:false, all:null, gen:0 };
const gw = $('#gridwrap');
const cellSize = () => Math.max(24, Math.floor(48 * (+$('#zoom').value) / 2));

async function loadFiles(){
  const r = await fetch('/api/files');
  const d = await r.json();
  STATE.all = d;
  renderTree(d.libs);
  if (d.sounds.length) renderSounds(d.sounds);
  else $('#tab-snd').disabled = true;
  restoreFromHash();
}

function restoreFromHash(){
  if (!STATE.all || !STATE.all.libs) return;
  const hash = location.hash;
  if (hash){
    const match = hash.match(/file=([^&]+)/);
    if (match){
      const fileName = decodeURIComponent(match[1]);
      const target = STATE.all.libs.find(l => l.name.toLowerCase() === fileName.toLowerCase() || l.name.toLowerCase() === (fileName + '.wil').toLowerCase());
      if (target){
        selectLib(target.name, false);
        document.querySelectorAll('#tree .file').forEach(d => {
          if (d.querySelector('.nm').textContent.toLowerCase() === target.name.replace(/\.wil$/i,'').toLowerCase()){
            setActive(d);
          }
        });
      }
    }
  }

  // Restore HUD preview open state
  if (location.hash.includes('hud=1') || localStorage.getItem('hud_preview_open') === '1'){
    openHudPreview();
  }
}
window.addEventListener('hashchange', restoreFromHash);
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
    STATE = { ...STATE, all: d, lib: null, count: 0, loaded: 0, loading: false, gen: STATE.gen + 1 };
    $('#root').value = d.root;
    $('#cur').textContent = 'Select a library on the left';
    $('#grid').innerHTML = ''; $('#loadinfo').textContent = ''; $('#anim').style.display = 'none';
    renderTree(d.libs);
    if (d.sounds && d.sounds.length){ renderSounds(d.sounds); $('#tab-snd').disabled = false; }
    else { $('#tab-snd').disabled = true; $('#sounds').innerHTML = ''; }
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
    d.querySelector('.cnt').textContent = l.count;
    d.onclick = () => { selectLib(l.name); setActive(d); };
    tree.appendChild(d);
  }
}
function setActive(d){ document.querySelectorAll('.file.active').forEach(x=>x.classList.remove('active')); d.classList.add('active'); }
function selectLib(name, updateHash = true){
  const lib = STATE.all.libs.find(l=>l.name===name); if(!lib) return;
  STATE = {...STATE, lib:name, count:lib.count, loaded:0, loading:false};
  if (updateHash) {
    history.replaceState(null, '', '#file=' + encodeURIComponent(name));
  }
  $('#cur').textContent = `${lib.name} · ${lib.count} frames · ${lib.size_mb} MB`;
  $('#loadinfo').textContent = '';
  $('#anim').style.display='none';
  $('#astart').value = 0;
  gw.scrollTop = 0;
  $('#grid').innerHTML = '';
  loadMore();
}
function loadMore(){
  if (!STATE.lib || STATE.loading || STATE.loaded >= STATE.count) return;
  STATE.loading = true;
  const start = STATE.loaded, end = Math.min(start + STATE.per, STATE.count);
  const cell = cellSize();
  const g = $('#grid');
  g.style.setProperty('--cell', cell + 'px');
  const frag = document.createDocumentFragment();
  const checker = ', repeating-conic-gradient(#2a2f38 0% 25%, #232830 0% 50%)';
  const hideBlank = $('#hideblank').checked;
  const gen = STATE.gen;
  const mkCell = i => {
    const d = document.createElement('div'); d.className='cell';
    d.style.backgroundImage = `url('/api/thumb?f=${encodeURIComponent(STATE.lib)}&i=${i}&s=${cell}')` + checker;
    d.style.backgroundSize = 'contain, 16px 16px';
    const t = document.createElement('span'); t.className='idx'; t.textContent=i; d.appendChild(t);
    d.onclick = () => openDetail(i);
    return d;
  };
  let pending = end - start;
  const done = () => {
    if (--pending > 0) return;
    if (gen !== STATE.gen) return;              // stale batch (reload/switched)
    STATE.loaded = end;
    g.appendChild(frag);
    STATE.loading = false;
    $('#loadinfo').textContent = `${g.children.length} / ${STATE.count}`;
    if (STATE.loaded >= STATE.count){ $('#loadbar').style.display='none'; return; }
    // keep scanning while content doesn't fill the viewport (blank frames skipped)
    const needMore = g.scrollHeight <= gw.clientHeight;
    if (needMore || gw.scrollTop + gw.clientHeight >= gw.scrollHeight - 800){ loadMore(); }
  };
  for (let i=start;i<end;i++){
    if (hideBlank){
      // probe first: server returns 204 for blank frames → skip them entirely
      const probe = new Image();
      probe.onload = () => { if (gen !== STATE.gen) return; frag.appendChild(mkCell(i)); done(); };
      probe.onerror = () => done();
      probe.src = `/api/thumb?f=${encodeURIComponent(STATE.lib)}&i=${i}&s=${cell}`;
    } else {
      frag.appendChild(mkCell(i));
      done();
    }
  }
}
gw.addEventListener('scroll', () => {
  if (gw.scrollTop + gw.clientHeight >= gw.scrollHeight - 800) loadMore();
});
function reloadGrid(){
  if (!STATE.lib) return;
  STATE.loaded = 0; STATE.loading = false; STATE.gen++;
  $('#grid').innerHTML = '';
  gw.scrollTop = 0;
  loadMore();
}
function applyCellSize(){
  const cell = cellSize();
  $('#grid').style.setProperty('--cell', cell + 'px');  // only the CSS variable changes; cell & image scale together
  $('#zoomval').textContent = $('#zoom').value + '×';
}
$('#zoom').oninput = applyCellSize;   // drag: CSS variable applies live, no relayout storm
$('#zoom').onchange = reloadGrid;     // release: re-fetch thumbnails at exact size
$('#hideblank').onchange = reloadGrid; // toggle hide-blank → re-render to apply
function openDetail(i){
  fetch(`/api/info?f=${encodeURIComponent(STATE.lib)}&i=${i}`).then(r=>r.json()).then(h=>{
    $('#mtitle').textContent = `${STATE.lib} · frame #${i}`;
    $('#mimg').src = `/api/image?f=${encodeURIComponent(STATE.lib)}&i=${i}&scale=4`;
    const base = `/api/image?f=${encodeURIComponent(STATE.lib)}&i=${i}&scale=1`;
    $('#mdown').href = base; $('#mdown').setAttribute('download', `${STATE.lib.replace(/\W/g,'_')}_${i}.png`);
    $('#mdown4').href = `/api/image?f=${encodeURIComponent(STATE.lib)}&i=${i}&scale=4`;
    $('#mdown4').setAttribute('download', `${STATE.lib.replace(/\W/g,'_')}_${i}_x4.png`);
    if (h.blank){
      $('#meta').textContent = 'Blank placeholder frame (index 0)';
    } else {
      $('#meta').textContent =
`Size: ${h.width} × ${h.height}
Anchor: x=${h.offsetX}  y=${h.offsetY}
Shadow: ${h.shadow?'yes':'no'} (${h.shadowX}, ${h.shadowY})
Data: ${h.words} words (${h.bytes} B)`;
    }
    $('#overlay').style.display='block'; $('#modal').style.display='block';
  });
}
$('#overlay').onclick = () => { closeModal(); closeHudModal(); };
$('#close').onclick = closeModal;
function closeModal(){ $('#overlay').style.display='none'; $('#modal').style.display='none'; }

// HUD 组装原理模拟器
$('#btn-hud').onclick = openHudPreview;
$('#hud-close').onclick = closeHudModal;

function toggleControlBorders(visible){
  document.querySelectorAll('.ui-ctrl-box').forEach(el => {
    el.style.outline = visible ? '' : 'none';
    el.style.borderWidth = visible ? '1.5px' : '0px';
  });
}

function updateUrlHash(){
  const file = STATE.lib || 'GameInter.wil';
  const hudOpen = $('#hud-modal').style.display === 'block' ? '&hud=1' : '';
  history.replaceState(null, '', '#file=' + encodeURIComponent(file) + hudOpen);
}

function openHudPreview(){
  const lib = 'GameInter.wil';
  // 原版 800x600 客户端真实绘图
  $('#part-bg').src = `/api/image?f=${encodeURIComponent(lib)}&i=50&scale=1`;
  $('#part-hp-ball').src = `/api/image?f=${encodeURIComponent(lib)}&i=60&scale=1`;
  $('#part-mp-ball').src = `/api/image?f=${encodeURIComponent(lib)}&i=61&scale=1`;
  $('#part-exp-line').src = `/api/image?f=${encodeURIComponent(lib)}&i=63&scale=1`;

  $('#overlay').style.display = 'block';
  $('#hud-modal').style.display = 'block';
  localStorage.setItem('hud_preview_open', '1');
  updateUrlHash();
}

function closeHudModal(){
  $('#overlay').style.display = 'none';
  $('#hud-modal').style.display = 'none';
  localStorage.setItem('hud_preview_open', '0');
  updateUrlHash();
}

// 悬停交互高亮卡片
document.querySelectorAll('#hud-main-panel img, .hud-part-btn').forEach(el => {
  el.onmouseenter = () => {
    const info = el.getAttribute('title') || el.alt || 'MainPanel Element';
    $('#hud-inspector').innerHTML = `<span style="color:#e8a33d; font-weight:bold;">🔍 零部件检测:</span> ${info}`;
    el.style.outline = '2px solid #e8a33d';
  };
  el.onmouseleave = () => {
    el.style.outline = 'none';
    $('#hud-inspector').innerHTML = '💡 提示：悬停或点击组件查看 C# 代码中 Location(X, Y) 绝对坐标拼装原理。';
  };
});
$('#search').oninput = function(){
  const q = this.value.trim().toLowerCase();
  document.querySelectorAll('#tree .file').forEach(d=>{
    d.style.display = !q || d.querySelector('.nm').textContent.toLowerCase().includes(q) ? '' : 'none';
  });
};
$('#btn-anim').onclick = ()=>{
  $('#anim').style.display='flex';
  const cell = cellSize();
  const colsVis = Math.max(1, Math.floor(gw.clientWidth / (cell + 4)));
  const row = Math.floor(gw.scrollTop / (cell + 4));
  $('#astart').value = String(Math.max(0, Math.min(row * colsVis, Math.max(0, STATE.count - 1))));
};
$('#hide-anim').onclick = ()=>{ $('#anim').style.display='none'; };
$('#play').onclick = ()=>{
  const start=+$('#astart').value, count=+$('#acount').value, fps=+$('#afps').value;
  $('#gif').src = `/api/anim?f=${encodeURIComponent(STATE.lib)}&start=${start}&count=${count}&fps=${fps}&scale=${+$('#zoom').value}`;
};
$('#tab-img').onclick = ()=>{ $('#tab-img').classList.add('active'); $('#tab-snd').classList.remove('active');
  $('#gridwrap').style.display=''; $('#sounds').style.display='none'; };
$('#tab-snd').onclick = ()=>{ $('#tab-snd').classList.add('active'); $('#tab-img').classList.remove('active');
  $('#gridwrap').style.display='none'; $('#sounds').style.display='flex'; };
function renderSounds(list){
  const box = $('#sounds'); box.innerHTML='';
  for (const s of list){
    const row = document.createElement('div'); row.className='sound-row';
    row.innerHTML = `<span class="nm">${s.name} (${s.size_kb} KB)</span>
      <audio controls preload="none" src="/api/sound?n=${encodeURIComponent(s.name)}"></audio>`;
    box.appendChild(row);
  }
}
loadFiles();
loadRoots();
applyCellSize();
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
  #wrap { padding:18px; display:flex; gap:18px; align-items:flex-start; }
  #screen { width:800px; height:600px; flex:none; position:relative; overflow:hidden; background:linear-gradient(135deg,#18201a,#0d120f); border:8px solid #2a3038; box-shadow:0 0 30px #000; image-rendering:pixelated; }
  #screen img { image-rendering:pixelated; } #world-label { position:absolute; left:16px; top:14px; color:#596d5d; font:12px monospace; pointer-events:none; }
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
  .secondary-control { position:absolute; z-index:35; border:1px solid transparent; pointer-events:auto; }
  .debug .secondary-control { border:1px solid #b78cff; background:rgba(130,70,255,.10); }
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
  <button id="reset">恢复默认状态</button>
</header>
<div id="wrap"><div id="screen" class="debug"><span id="world-label">[EI 3.0 evidence viewport: 800 × 600]</span></div>
<aside id="side"><h2>布局记录</h2><div id="summary">读取中…</div><div id="records"></div></aside></div>
<script>
const $ = s => document.querySelector(s); const screen = $('#screen');
let DATA = null; const key='mir3_evidence_ui_state';
function state(){ try{return JSON.parse(localStorage.getItem(key)||'{}')}catch(e){return {}} }
function save(){localStorage.setItem(key,JSON.stringify({debug:$('#debug').checked,frames:$('#frames').checked}));}
function esc(s){return String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));}
function resolve(v,base){return typeof v==='number'?v:(v&&v.offset+(base[v.base]||0));}
function addLabel(el,text,klass){const l=document.createElement('span');l.className=klass;l.textContent=text;el.appendChild(l);}
function addMain(){const img=document.createElement('img');img.className='hud-base';img.src='/api/image?f=GameInter.wil&i=50&scale=1';img.title='GameInter.wil Frame 50 · primary-static';screen.appendChild(img);}
function addButton(r,base){const x=resolve(r.position.x,base),y=resolve(r.position.y,base),w=r.size.width,h=r.size.height;const b=document.createElement('div');b.className='evidence-button';b.style.cssText=`left:${x}px;top:${y}px;width:${w}px;height:${h}px`;const im=document.createElement('img');im.src=`/api/image?f=${encodeURIComponent(r.resource.file)}&i=${r.resource.frames.normal}&scale=1`;b.appendChild(im);addLabel(b,`${r.id} · F${r.resource.frames.normal} · (${x},${y},${w},${h})`,'button-label');b.onclick=()=>focusRecord(r.id);screen.appendChild(b);}
function addWindow(r,analysis){const w=r.size.width,h=r.size.height,x=r.position.x,y=r.position.y;const box=document.createElement('div');box.className='evidence-window';box.style.cssText=`left:${x}px;top:${y}px;width:${w}px;height:${h}px`;const a=analysis.find(q=>q.id===r.id);const bb=a&&a.resource&&a.resource.visible_bbox;const im=document.createElement('img');im.src=`/api/image?f=${encodeURIComponent(r.resource.file)}&i=${r.resource.frame}&scale=1`;if(bb)im.style.cssText=`left:${-bb.left}px;top:${-bb.top}px`;box.appendChild(im);const lib=r.resource_handle?.library?.file||'resource unresolved';addLabel(box,`${r.id} · ${lib} · F${r.resource.frame} · (${x},${y},${w},${h})`,'window-label');box.onclick=()=>focusRecord(r.id);screen.appendChild(box);}
function addInternalControl(r){if(!r.position||r.coordinate_status==='unresolved')return;const pair=r.resource?.frame_pair||[];const w=r.size?.width||20,h=r.size?.height||20;const box=document.createElement('div');box.className=`internal-control ${r.coordinate_status==='outside-window'?'outside-control':''} ${r.size?'':'unresolved-size'}`;box.style.cssText=`left:${r.position.x}px;top:${r.position.y}px;width:${w}px;height:${h}px`;addLabel(box,`${r.window_id} · F${pair.join('/')} · (${r.position.x},${r.position.y},${w},${h}) · ${r.coordinate_status}${r.size?'':' · size unresolved'}`,'button-label');box.onclick=()=>focusRecord(r.id);screen.appendChild(box);}
function addSecondaryControl(r){const box=document.createElement('div');const p=r.position,s=r.size;box.className='secondary-control';box.style.cssText=`left:${p.x}px;top:${p.y}px;width:${s.width}px;height:${s.height}px`;addLabel(box,`${r.id} · Interface1c F${r.resource.frame} · (${p.x},${p.y},${s.width},${s.height})`,'button-label');box.onclick=()=>focusRecord(r.id);screen.appendChild(box);}
function focusRecord(id){document.querySelectorAll('.row').forEach(x=>x.style.outline='');const r=document.querySelector(`[data-id="${CSS.escape(id)}"]`);if(r){r.scrollIntoView({block:'nearest'});r.style.outline='1px solid #e8a33d';}}
function render(){if(!DATA)return;screen.querySelectorAll(':scope > *:not(#world-label)').forEach(x=>x.remove());addMain();const l=DATA.layout,base={'hud.left':0,'hud.top':465};const rec=$('#records');rec.innerHTML='';let buttons=0,windows=0;for(const r of l.records){if(r.kind==='button'){addButton(r,base);buttons++;}if(r.kind==='window'){addWindow(r,DATA.window_resource_analysis.records||[]);windows++;}const row=document.createElement('div');row.className='row';row.dataset.id=r.id;const e=r.evidence||{};const handle=r.resource_handle?.library?.file||r.resource?.file||'';row.innerHTML=`<div class="name">${esc(r.id)} <span class="tag ${e.level&&e.level.startsWith('primary')?'primary':''}">${esc(e.level||'unknown')}</span></div><div class="meta">${r.kind} · ${handle} · Frame ${r.resource?.frame??r.resource?.frames?.normal??'—'}</div>`;row.onclick=()=>focusRecord(r.id);rec.appendChild(row);}const controls=(l.control_constructors||[]);let internal=0;for(const r of controls){addInternalControl(r);if(r.coordinate_status==='inside-window')internal++;const e=r.evidence||{};const row=document.createElement('div');row.className='row';row.dataset.id=r.id;row.innerHTML=`<div class="name">${esc(r.id)} <span class="tag ${e.level&&e.level.startsWith('primary')?'primary':''}">${esc(e.level||'unknown')}</span></div><div class="meta">window control · Frame pair ${(r.resource?.frame_pair||[]).join('/')||'—'} · ${r.coordinate_status||'position unresolved'}${r.size?' · '+r.size.width+'×'+r.size.height:''}</div>`;row.onclick=()=>focusRecord(r.id);rec.appendChild(row);}const secondary=l.secondary_control_constructors||[];for(const r of secondary){addSecondaryControl(r);const row=document.createElement('div');row.className='row';row.dataset.id=r.id;row.innerHTML=`<div class="name">${esc(r.id)} <span class="tag primary">primary-static-closed</span></div><div class="meta">${esc(r.resource.file)} · Frame ${r.resource.frame} · (${r.position.x},${r.position.y},${r.size.width}×${r.size.height})</div>`;row.onclick=()=>focusRecord(r.id);rec.appendChild(row);}for(const w of (l.secondary_window_candidates||[])){const row=document.createElement('div');row.className='row';row.dataset.id='secondary-window-'+w.constructor_va;row.innerHTML=`<div class="name">次级窗口候选 ${esc(w.constructor_va)} <span class="tag">candidate</span></div><div class="meta">Frame ${w.frame} · 资源 ${esc(w.resource)} · 原始参数语义待确认</div>`;rec.appendChild(row);}const global=DATA.global_control_catalog||{};const unassigned=(global.records||[]).filter(r=>r.classification==='unassigned-control-candidate');const gr=document.createElement('div');gr.className='row';gr.dataset.id='global-unassigned-controls';gr.innerHTML=`<div class="name">未归属控件候选 <span class="tag">evidence catalog</span></div><div class="meta">全局 0x00417550 调用 ${global.counts?.all||0} · 未归属 ${unassigned.length} · 仅保留反汇编/Frame证据，坐标待绑定</div>`;gr.onclick=()=>focusRecord('global-unassigned-controls');rec.appendChild(gr);const draw=DATA.draw_calls||{};const sites=draw.all_composition_call_sites||[];const baseDraw=DATA.window_base_draw?.routine?.direct_calls||[];const vt=DATA.window_vtables||{};const vRows=vt.vtable_tables||[];const vAssign=vt.constructor_assignments||[];const binds=DATA.window_vtable_bindings?.records||[];const npc=DATA.npc_paint?.calls||[];const handleStatus=DATA.resource_handle_bindings?.main_ui_resource?.wil_file||'resource handle unresolved';const dr=document.createElement('div');dr.className='row';dr.dataset.id='draw-chain';dr.innerHTML=`<div class="name">原版绘制链 <span class="tag primary">primary-static-draw-candidate</span></div><div class="meta">资源 ${esc(handleStatus)} · 按钮 0x4179B0 → 0x45F2D0 · ${sites.length} 个共享合成调用点 · 窗口基类 0x423D00 → ${baseDraw.length} 个分支 · vtable ${vRows.length}/${vAssign.length} · 绑定 ${binds.length} · NPC专用绘制 ${npc.length} 调用</div>`;rec.prepend(dr);$('#summary').textContent=`${buttons} 个按钮 · ${windows} 个窗口 · ${controls.length} 个窗口控件构造 · 次级控件 ${secondary.length} · ${internal} 个几何通过调试框 · 未归属控件 ${unassigned.length} · 主 UI 资源 ${handleStatus} · ${sites.length} 个共享合成调用点 · ${baseDraw.length} 个窗口基类绘制分支 · ${vRows.length} 个vtable · ${binds.length} 个窗口绑定候选 · ${l.version}`;}
function load(){const s=state();if(typeof s.debug==='boolean')$('#debug').checked=s.debug;if(typeof s.frames==='boolean')$('#frames').checked=s.frames;$('#debug').onchange=()=>{screen.classList.toggle('debug',$('#debug').checked);save()};$('#frames').onchange=()=>{screen.classList.toggle('no-frames',!$('#frames').checked);save()};$('#reset').onclick=()=>{localStorage.removeItem(key);location.reload()};fetch('/api/ui-layout').then(r=>r.json()).then(d=>{DATA=d;render()}).catch(e=>$('#summary').textContent='读取布局失败：'+e);}
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
