#!/usr/bin/env python3
"""zlsdk.py — Python reader for Zircon .Zl compressed image libraries.

Supports ZL2 container format used by Zircon (Deflate compressed PNG/RAW image entries).
"""
from __future__ import annotations

import io
import os
import struct
import zlib

try:
    from PIL import Image
except ImportError:
    Image = None


class Zl2Entry:
    __slots__ = ('id', 'offset', 'length', 'packed_length', 'compressed', 'format')

    def __init__(self, entry_id: int, offset: int, length: int, packed_length: int, compressed: bool, fmt: int):
        self.id = entry_id
        self.offset = offset
        self.length = length
        self.packed_length = packed_length
        self.compressed = compressed
        self.format = fmt


class ZlImageHeader:
    __slots__ = ('width', 'height', 'offset_x', 'offset_y', 'position')

    def __init__(self, width: int, height: int, offset_x: int, offset_y: int, position: int):
        self.width = width
        self.height = height
        self.offset_x = offset_x
        self.offset_y = offset_y
        self.position = position


def _rgb565(value: int) -> tuple[int, int, int]:
    return ((value >> 11 & 0x1F) * 255 // 31,
            (value >> 5 & 0x3F) * 255 // 63,
            (value & 0x1F) * 255 // 31)


def _decode_bc1(data: bytes, width: int, height: int, with_alpha: bool = True) -> bytes:
    out = bytearray(width * height * 4)
    for by in range((height + 3) // 4):
        for bx in range((width + 3) // 4):
            p = (by * ((width + 3) // 4) + bx) * 8
            if p + 8 > len(data):
                continue
            c0, c1, bits = struct.unpack_from('<HHI', data, p)
            a = _rgb565(c0)
            b = _rgb565(c1)
            palette = [a, b]
            if c0 > c1 or not with_alpha:
                palette += [tuple((2 * a[i] + b[i]) // 3 for i in range(3)),
                            tuple((a[i] + 2 * b[i]) // 3 for i in range(3))]
            else:
                palette += [tuple((a[i] + b[i]) // 2 for i in range(3)), (0, 0, 0)]
            for iy in range(4):
                for ix in range(4):
                    x, y = bx * 4 + ix, by * 4 + iy
                    if x >= width or y >= height:
                        continue
                    index = (bits >> (2 * (iy * 4 + ix))) & 3
                    r, g, b = palette[index]
                    alpha = 0 if with_alpha and c0 <= c1 and index == 3 else 255
                    q = (y * width + x) * 4
                    out[q:q + 4] = bytes((r, g, b, alpha))
    return bytes(out)


def _decode_bc3(data: bytes, width: int, height: int) -> bytes:
    out = bytearray(width * height * 4)
    blocks_w = (width + 3) // 4
    for by in range((height + 3) // 4):
        for bx in range(blocks_w):
            p = (by * blocks_w + bx) * 16
            if p + 16 > len(data):
                continue
            a0, a1 = data[p], data[p + 1]
            alpha_bits = int.from_bytes(data[p + 2:p + 8], 'little')
            alpha = [a0, a1]
            if a0 > a1:
                alpha += [(7 * a0 + a1) // 8, (6 * a0 + 2 * a1) // 8,
                          (5 * a0 + 3 * a1) // 8, (4 * a0 + 4 * a1) // 8,
                          (3 * a0 + 5 * a1) // 8, (2 * a0 + 6 * a1) // 8]
            else:
                alpha += [(6 * a0 + a1) // 7, (5 * a0 + 2 * a1) // 7,
                          (4 * a0 + 3 * a1) // 7, (3 * a0 + 4 * a1) // 7,
                          (2 * a0 + 5 * a1) // 7, (a0 + 6 * a1) // 7, 0, 255]
            color = _decode_bc1(data[p + 8:p + 16], 4, 4, False)
            for iy in range(4):
                for ix in range(4):
                    x, y = bx * 4 + ix, by * 4 + iy
                    if x >= width or y >= height:
                        continue
                    ai = (alpha_bits >> (3 * (iy * 4 + ix))) & 7
                    src = (iy * 4 + ix) * 4
                    q = (y * width + x) * 4
                    out[q:q + 4] = color[src:src + 3] + bytes((alpha[ai],))
    return bytes(out)


class ZlLibrary:
    """Python loader for Zircon .Zl libraries."""

    def __init__(self, path: str):
        self.path = path
        self.name = os.path.basename(path)
        with open(path, "rb") as f:
            self.data = f.read()

        self.entries: dict[int, Zl2Entry] = {}
        self.headers: dict[int, ZlImageHeader] = {}
        self.count = 0
        self.is_zl2 = False
        self._parse()

    def _parse(self):
        if len(self.data) < 43 or self.data[:3] != b"ZL2":
            self._parse_v1()
            return

        self.is_zl2 = True
        meta_offset = struct.unpack_from("<q", self.data, 19)[0]
        meta_size = struct.unpack_from("<i", self.data, 27)[0]
        index_offset = struct.unpack_from("<q", self.data, 31)[0]
        index_size = struct.unpack_from("<i", self.data, 39)[0]

        # 1. Parse Index Block
        idx_data = self.data[index_offset: index_offset + index_size]
        idx_pos = 0
        entry_count = struct.unpack_from("<i", idx_data, idx_pos)[0]
        idx_pos += 4

        for _ in range(entry_count):
            eid = struct.unpack_from("<i", idx_data, idx_pos)[0]
            off = struct.unpack_from("<q", idx_data, idx_pos + 4)[0]
            length = struct.unpack_from("<i", idx_data, idx_pos + 12)[0]
            packed_len = struct.unpack_from("<i", idx_data, idx_pos + 16)[0]
            comp = bool(idx_data[idx_pos + 20])
            fmt = idx_data[idx_pos + 21]
            idx_pos += 22
            self.entries[eid] = Zl2Entry(eid, off, length, packed_len, comp, fmt)

        # 2. Parse Metadata Block
        meta_data = self.data[meta_offset: meta_offset + meta_size]
        mpos = 0
        version = struct.unpack_from("<i", meta_data, mpos)[0]
        count = struct.unpack_from("<i", meta_data, mpos + 4)[0]
        mpos += 16 # Skip Version, count, AtlasGroupImageCount, AtlasPageSize
        self.count = count

        for i in range(count):
            present = meta_data[mpos] != 0
            mpos += 1
            if not present:
                continue
            # ZlImage.Read(reader, Version): width(h), height(h), offsetX(h), offsetY(h), shadowX(h), shadowY(h)...
            w, h, ox, oy = struct.unpack_from("<hhhh", meta_data, mpos)
            mpos += 12 # Skip w, h, ox, oy, sx, sy
            if version >= 1:
                pos = struct.unpack_from("<i", meta_data, mpos)[0]
                mpos += 4
            else:
                pos = i
            mpos += 4 # Skip Light/Shadow/Flags
            self.headers[i] = ZlImageHeader(w, h, ox, oy, pos)

    def _parse_v1(self):
        # Legacy ZL container: int32 metadata size, followed by a packed
        # metadata block and raw DXT payloads at each image Position.
        if len(self.data) < 4:
            return
        meta_size = struct.unpack_from('<i', self.data, 0)[0]
        if meta_size <= 4 or 4 + meta_size > len(self.data):
            return
        meta = self.data[4:4 + meta_size]
        value = struct.unpack_from('<i', meta, 0)[0]
        version = (value >> 25) & 0x7F
        count = value & 0x1FFFFFF
        if version == 0:
            count = value
        self.count = max(0, count)
        pos = 4
        for i in range(self.count):
            if pos >= len(meta):
                break
            present = meta[pos] != 0
            pos += 1
            if not present or pos + 25 > len(meta):
                pos += 25 if present else 0
                continue
            image_pos = struct.unpack_from('<i', meta, pos)[0]
            width, height, ox, oy = struct.unpack_from('<hhhh', meta, pos + 4)
            self.headers[i] = ZlImageHeader(width, height, ox, oy, image_pos)
            pos += 25
        self.version = version

    def header(self, index: int) -> dict | None:
        hdr = self.headers.get(index)
        if hdr is None or hdr.width <= 0 or hdr.height <= 0:
            return None
        return {
            "index": index,
            "width": hdr.width,
            "height": hdr.height,
            "offsetX": hdr.offset_x,
            "offsetY": hdr.offset_y,
        }

    def decode(self, index: int) -> "Image.Image | None":
        if Image is None:
            return None
        hdr = self.headers.get(index)
        if hdr is None or hdr.width <= 0 or hdr.height <= 0:
            return None

        if self.is_zl2:
            entry_id = hdr.position
            if entry_id not in self.entries:
                return None
            entry = self.entries[entry_id]
            raw = self.data[entry.offset: entry.offset + entry.packed_length]
            if entry.compressed:
                raw = zlib.decompress(raw)
        else:
            block_size = ((hdr.width + 3) // 4) * ((hdr.height + 3) // 4) * (8 if self.version == 0 else 16)
            raw = self.data[hdr.position:hdr.position + block_size]

        try:
            if self.is_zl2:
                # ZL2 primary payloads are normally PNG; keep this path
                # extensible for the newer container's codec variants.
                im = Image.open(io.BytesIO(raw))
                im.load()
                return im.convert("RGBA")
            pixels = (_decode_bc1(raw, hdr.width, hdr.height, True)
                      if self.version == 0 else
                      _decode_bc3(raw, hdr.width, hdr.height))
            return Image.frombytes("RGBA", (hdr.width, hdr.height), pixels)
        except Exception:
            return None

    def decode_scaled(self, index: int, scale: int) -> "Image.Image | None":
        """Decode -> RGBA PIL Image at 1/scale resolution, byte-identical to
        decode() + NEAREST resize for dimensions divisible by scale (all
        tiles).

        Legacy BC1 payloads are decoded block-sampled: only the 4x4 blocks
        touched by PIL's NEAREST source grid (out(j) <- in(j*scale +
        scale//2)) are unpacked, so cost drops ~1/scale^2 (1/4 of blocks at
        scale 8).  ZL2/PNG payloads decode 1:1 then resize (PNG is C-speed).
        """
        if Image is None:
            return None
        hdr = self.headers.get(index)
        if hdr is None or hdr.width <= 0 or hdr.height <= 0:
            return None
        w, h = hdr.width, hdr.height
        if scale <= 1:
            return self.decode(index)
        ow, oh = max(1, w // scale), max(1, h // scale)
        cols = [min(w - 1, int((j + 0.5) * w / ow)) for j in range(ow)]
        rows = [min(h - 1, int((r + 0.5) * h / oh)) for r in range(oh)]

        if self.is_zl2:
            im = self.decode(index)
            if im is None:
                return None
            return im.resize((ow, oh), Image.NEAREST)

        entry = self.entries.get(hdr.position) if self.is_zl2 else None
        if self.version == 0:
            # BC1 block-sampled decode
            if self.is_zl2:
                raw = self.data[entry.offset: entry.offset + entry.packed_length]
                if entry.compressed:
                    raw = zlib.decompress(raw)
            else:
                block_size = ((w + 3) // 4) * ((h + 3) // 4) * 8
                raw = self.data[hdr.position:hdr.position + block_size]
            blocks_w = (w + 3) // 4
            colmap: dict[int, list] = {}
            for j in range(ow):
                sx = cols[j]
                colmap.setdefault(sx // 4, []).append((sx % 4, j))
            rowmap: dict[int, list] = {}
            for i in range(oh):
                sy = rows[i]
                rowmap.setdefault(sy // 4, []).append((sy % 4, i))
            buf = bytearray(ow * oh * 4)
            for by, rowpix in rowmap.items():
                for bx, colpix in colmap.items():
                    p = (by * blocks_w + bx) * 8
                    if p + 8 > len(raw):
                        continue
                    c0, c1, bits = struct.unpack_from("<HHI", raw, p)
                    a = _rgb565(c0)
                    b = _rgb565(c1)
                    palette = [a, b]
                    if c0 > c1:
                        palette += [tuple((2 * a[i] + b[i]) // 3 for i in range(3)),
                                    tuple((a[i] + 2 * b[i]) // 3 for i in range(3))]
                    else:
                        palette += [tuple((a[i] + b[i]) // 2 for i in range(3)),
                                    (0, 0, 0)]
                    for iy, i in rowpix:
                        for ix, j in colpix:
                            index = (bits >> (2 * (iy * 4 + ix))) & 3
                            r, g, bb = palette[index]
                            alpha = 0 if index == 3 and c0 <= c1 else 255
                            q = (i * ow + j) * 4
                            buf[q:q + 4] = bytes((r, g, bb, alpha))
            return Image.frombuffer("RGBA", (ow, oh), bytes(buf), "raw", "RGBA", 0, 1)
        # BC3 (version 1) full decode + resize; block-sampling BC3 is a
        # follow-up if a version-1 library ever appears in a big map.
        im = self.decode(index)
        if im is None:
            return None
        return im.resize((ow, oh), Image.NEAREST)
