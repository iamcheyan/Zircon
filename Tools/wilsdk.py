#!/usr/bin/env python3
"""wilsdk.py — Wemade WIL/WIX image library decoder (Mir3 EI / 传奇3 EI).

Implements the Mir3 WIL format as documented in Zircon's
LibraryEditor/WeMadeLibrary.cs (nType=3):

  .wix  index: 26 header bytes + uint16 magic 0xB13A, then int32 offsets
               (an offset of 0 marks a blank placeholder image)
  .wil  image: 17-byte header (int16 W/H/X/Y, byte shadowFlag,
               int16 shadowX/Y, int32 wordCount) + RLE scanlines
  RLE   opcodes: 0xC0 skip pixels, 0xC1/0xC3 solid colour run,
               0xC2 overlay (writes colour + mask plane)
  Pixels are 16-bit RGB565; value 0 is transparent.

Stdlib + Pillow only.
"""
from __future__ import annotations

import mmap
import os
import struct
from functools import lru_cache

try:
    from PIL import Image
except ImportError:  # pragma: no cover
    Image = None

HEADER_SIZE = 17
MAGIC = 0xB13A


# ---------------------------------------------------------------- wix index
def read_wix_offsets(wix_path: str) -> list[int]:
    """Parse a .wix index file -> list of image byte offsets into the .wil."""
    with open(wix_path, "rb") as f:
        data = f.read()
    pos = 26
    if len(data) > 28:
        magic = struct.unpack_from("<H", data, 26)[0]
        pos = 28 if magic == MAGIC else 24
    offsets = []
    while pos + 4 <= len(data):
        offsets.append(struct.unpack_from("<I", data, pos)[0])
        pos += 4
    return offsets


# ------------------------------------------------------------------- library
class WilLibrary:
    """A .wil/.wix pair with lazy image decoding (mmap-backed)."""

    def __init__(self, wil_path: str, wix_path: str | None = None):
        self.wil_path = wil_path
        self.name = os.path.basename(wil_path)
        self.wix_path = wix_path or os.path.splitext(wil_path)[0] + ".wix"
        self._file = open(wil_path, "rb")
        self._mm = mmap.mmap(self._file.fileno(), 0, access=mmap.ACCESS_READ)
        self.offsets = read_wix_offsets(self.wix_path) if os.path.exists(self.wix_path) else []

    @property
    def count(self) -> int:
        return len(self.offsets)

    def header(self, index: int) -> dict | None:
        """Decode the 17-byte image header. None for blank/out-of-range."""
        if not (0 <= index < len(self.offsets)):
            return None
        start = self.offsets[index]
        if start == 0 or start + HEADER_SIZE > len(self._mm):
            return None
        w, h, x, y = struct.unpack_from("<hhhh", self._mm, start)
        shadow = self._mm[start + 8]
        sx, sy = struct.unpack_from("<hh", self._mm, start + 9)
        nwords = struct.unpack_from("<i", self._mm, start + 13)[0]
        return {
            "index": index,
            "width": w,
            "height": h,
            "offsetX": x,
            "offsetY": y,
            "shadow": bool(shadow & 1),
            "shadowX": sx,
            "shadowY": sy,
            "words": nwords,
            "bytes": nwords * 2,
        }

    def decode(self, index: int) -> "Image.Image | None":
        """Decode image -> RGBA PIL Image (transparent background)."""
        return self.decode_scaled(index, 1)

    def decode_scaled(self, index: int, scale: int) -> "Image.Image | None":
        """Decode image -> RGBA PIL Image at 1/scale resolution.

        Walks the same RLE stream as decode() but only materialises pixels on
        the scale-grid PIL's NEAREST resize would pick (out[j] <- in[j*scale +
        scale//2], out size floor(w/scale)), so cost and output size scale
        ~1/scale^2 and the result is byte-identical to decode()+NEAREST
        resize for sizes divisible by scale (all ground tiles).  scale==1
        yields the identical full-resolution image via a faster buffer path.
        """
        if Image is None:
            raise RuntimeError("Pillow is required")
        if scale < 1:
            raise ValueError("scale must be >= 1")
        hdr = self.header(index)
        if hdr is None or hdr["width"] <= 0 or hdr["height"] <= 0:
            return None
        w, h = hdr["width"], hdr["height"]
        ow, oh = max(1, w // scale), max(1, h // scale)
        # PIL NEAREST-downscale mapping (identical to decode()+resize()):
        # out(j) <- in(min(w-1, int((j + 0.5) * w / ow))).  When scale divides
        # the dimension this collapses to j*scale + scale//2 (all ground tiles
        # are 96x64 so 2/4/8 divide evenly); odd object frames match PIL too.
        cols = [min(w - 1, int((j + 0.5) * w / ow)) for j in range(ow)]
        rows = [min(h - 1, int((r + 0.5) * h / oh)) for r in range(oh)]
        start = self.offsets[index]
        nbytes = hdr["bytes"]
        data = self._mm[start + HEADER_SIZE: start + HEADER_SIZE + nbytes]
        if len(data) < nbytes:
            data = data + b"\x00" * (nbytes - len(data))

        buf = bytearray(ow * oh * 4)  # RGBA, 0 (transparent) by default
        out_row = 0
        End = 0
        OffSet = 0
        Start = 0
        for Y in range(h):
            if out_row < oh and Y == rows[out_row]:
                px = bytearray(ow * 2)  # sampled row colour plane (565)
                do_flush = True
            else:
                px = None
                do_flush = False
            OffSet = Start * 2
            End += data[OffSet] | (data[OffSet + 1] << 8)
            Start += 1
            nX = Start
            OffSet += 2
            X = 0
            cj = 0  # column cursor; cols[] is monotone so this never rewinds
            while nX < End:
                op = data[OffSet]
                if op == 192:  # skip
                    nX += 2
                    cnt = data[OffSet + 3] << 8 | data[OffSet + 2]
                    X += cnt
                    OffSet += 4
                elif op in (193, 195):  # solid colour run
                    nX += 2
                    cnt = data[OffSet + 3] << 8 | data[OffSet + 2]
                    OffSet += 4
                    if do_flush and cnt and X < w:
                        stop = X + cnt
                        while cj < ow and cols[cj] < X:
                            cj += 1
                        while cj < ow and cols[cj] < stop:
                            jj = cj * 2
                            src = cols[cj]
                            px[jj] = data[OffSet + (src - X) * 2]
                            px[jj + 1] = data[OffSet + (src - X) * 2 + 1]
                            cj += 1
                    OffSet += cnt * 2
                    X += cnt
                    nX += cnt
                elif op == 194:  # overlay colour + mask plane (mask ignored)
                    nX += 2
                    cnt = data[OffSet + 3] << 8 | data[OffSet + 2]
                    OffSet += 4
                    if do_flush and cnt and X < w:
                        stop = X + cnt
                        while cj < ow and cols[cj] < X:
                            cj += 1
                        while cj < ow and cols[cj] < stop:
                            jj = cj * 2
                            src = cols[cj]
                            px[jj] = data[OffSet + (src - X) * 2]
                            px[jj + 1] = data[OffSet + (src - X) * 2 + 1]
                            cj += 1
                    OffSet += cnt * 2
                    X += cnt
                    nX += cnt
                else:
                    raise ValueError(
                        f"unsupported WIL opcode 0x{op:02X} at compressed byte {OffSet} "
                        f"(image {index} in {self.name})"
                    )
            End += 1
            Start = End

            if do_flush:
                base = out_row * ow * 4
                for xx in range(ow):
                    v = px[xx * 2] | (px[xx * 2 + 1] << 8)
                    if v:
                        i = base + xx * 4
                        buf[i] = (v & 0xF800) >> 8
                        buf[i + 1] = (v & 0x07E0) >> 3
                        buf[i + 2] = (v & 0x001F) << 3
                        buf[i + 3] = 255
                out_row += 1

        return Image.frombuffer("RGBA", (ow, oh), bytes(buf), "raw", "RGBA", 0, 1)

    def close(self):
        try:
            self._mm.close()
        finally:
            self._file.close()


def _c16(v: int) -> tuple[int, int, int, int]:
    if v == 0:
        return (0, 0, 0, 0)
    r = (v & 0xF800) >> 8
    g = (v & 0x07E0) >> 3
    b = (v & 0x001F) << 3
    return (r, g, b, 255)


# ------------------------------------------------------------------ helpers
@lru_cache(maxsize=64)
def open_library(wil_path: str) -> WilLibrary:
    return WilLibrary(wil_path)


def scan_libraries(root: str) -> list[WilLibrary]:
    """Find every .wil with a matching .wix under root (top level only)."""
    libs = []
    for entry in sorted(os.listdir(root)):
        if entry.lower().endswith(".wil"):
            base = os.path.join(root, os.path.splitext(entry)[0])
            if os.path.exists(base + ".wix") or os.path.exists(base + ".WIX"):
                libs.append(open_library(base + ".wil"))
    return libs


def categorize(name: str) -> str:
    n = name.lower().replace(".wil", "")
    if n.startswith(("mon-", "mons-", "dmon-", "monmagic", "monimg")):
        return "Monsters"
    if n.startswith(("m-hum", "wm-hum", "m-hair", "wm-hair",
                     "m-weapon", "wm-weapon", "m-helmet", "wm-helmet")):
        return "Character & Gear"
    if n == "horse":
        return "Mounts"
    if n.startswith(("storeitem", "inventory", "micon", "equip", "ground", "proguse")):
        return "Item Icons"
    if n in ("npc", "npcface"):
        return "NPC"
    if n.startswith("magic"):
        return "Magic Effects"
    if n.startswith(("tiles", "object", "houses", "dungeons", "inners",
                     "furnitures", "smobjects", "smtiles", "walls", "cliffs",
                     "animations", "mmap", "fmmap")):
        return "Map Tiles"
    if n in ("gameinter", "interface1c"):
        return "UI"
    return "Other"


def contact_sheet(imgs: list, cols: int, scale: int, bg=(60, 60, 60, 255)):
    """Lay decoded images into one RGBA sheet; scale > 1 uses nearest-neighbour."""
    if not imgs:
        return Image.new("RGBA", (1, 1))
    if scale > 1:
        imgs = [im.resize((im.width * scale, im.height * scale), Image.NEAREST)
                if im else None for im in imgs]
    rows = (len(imgs) + cols - 1) // cols
    cell_w = max((im.width if im else 0) for im in imgs) or 1
    cell_h = max((im.height if im else 0) for im in imgs) or 1
    pad = 4
    sheet = Image.new("RGBA", (cols * cell_w + (cols + 1) * pad,
                               rows * cell_h + (rows + 1) * pad), bg)
    for i, im in enumerate(imgs):
        if im is None:
            continue
        r, c = divmod(i, cols)
        sheet.paste(im, (pad + c * (cell_w + pad), pad + r * (cell_h + pad)), im)
    return sheet


def make_gif(imgs: list, fps: int, scale: int) -> bytes:
    """Frames -> animated GIF bytes (transparent background becomes checkboard)."""
    from io import BytesIO
    frames = []
    for im in imgs:
        if im is None:
            continue
        if scale > 1:
            im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        im = im.convert("RGB")
        frames.append(im)
    if not frames:
        return b""
    buf = BytesIO()
    duration = max(1, int(1000 / fps))
    frames[0].save(buf, format="GIF", save_all=True, append_images=frames[1:],
                   duration=duration, loop=0, disposal=2)
    return buf.getvalue()
