#!/usr/bin/env python3
"""移植英雄杀图库帧到 Zircon Mon-31.Zl / Mon-22.Zl（PNG codec ZL2）。

背景（见 /tmp/investigate/new_monster_images.json 与文档）：
- 玛法战士 raceimg=316 -> 英雄杀 Mon-31 s6；玛法道士 317 -> Mon-31 s7。
  Zircon Mon-31 s6/s7 当前为空 -> 整库按英雄杀 wtl 重建（其余槽与 Zircon 现内容一致）。
- 钻卡树 raceimg=153 -> 英雄杀 Mon-15 s3；火焰狮子5 159 -> Mon-15 s9。
  Zircon Mon-15 s3/s9 已被自家怪占用（BoneBladesman / PoisonousMutantFlea），
  不能覆盖 -> 把英雄杀帧移植到 Zircon Mon-22 s4 / s5（两库均空）。
"""
import json
import struct
import sys
import zlib
import io
from pathlib import Path

sys.path.insert(0, '/home/tetsuya/development/Mir3-Research/Tools/common')
from WtlToZl import read_wtl

HERO = Path('/tmp/investigate/hero_client/英雄杀传奇三/Data')
ZIRCON = Path('/home/tetsuya/development/zircon/Debug/Client/Data')

CODEC_PNG = 4
CODEC_DXT5 = 1
COMPRESS_DEFLATE = 1


def png_bytes(image):
    """image = (w, h, ox, oy, sx, sy, rgba_bytes) -> PNG bytes"""
    from PIL import Image as PILImage
    w, h = image[0], image[1]
    im = PILImage.frombytes('RGBA', (w, h), image[6])
    buf = io.BytesIO()
    im.save(buf, format='PNG')
    return buf.getvalue()


def metadata_block(images, png_payloads):
    """ZL2 v2 metadata: per image, present byte + 87-byte record (present only)."""
    meta = bytearray(struct.pack('<4i', 2, len(images), 0, 0))  # Version, Count, AtlasGroup, AtlasPage
    for i, image in enumerate(images):
        if image is None:
            meta.append(0)
            continue
        meta.append(1)
        w, h, ox, oy, sx, sy = image[0], image[1], image[2], image[3], image[4], image[5]
        png = png_payloads[i]
        meta.extend(struct.pack(
            '<ihhhhBhhhhhh'
            'i4h4h'
            'BBB BBB'
            '9i',
            i,          # Position = entry id
            w, h, ox, oy,
            0,          # ShadowType
            0, 0, 0, 0, # Shadow w/h/ox/oy
            0, 0,       # Overlay w/h
            -1,         # AtlasPage
            0, 0, w, h, # SourceRectangle
            0, 0, w, h, # VisibleBounds
            CODEC_PNG, CODEC_DXT5, CODEC_DXT5,  # codecs
            0, 0, 0,                            # runtime prefs
            len(png), 0, 0,   # StoredImageDataSize, Bc7, Fallback
            0, 0, 0, 0, 0, 0, # shadow sizes
        ))
    return bytes(meta)


def index_block(png_payloads):
    """ZL2 index: entry per present image. Raw-deflate compressed PNG."""
    entries = []   # (id, payload_deflated, uncompressed)
    for i, png in png_payloads.items():
        comp = zlib.compressobj(6, zlib.DEFLATED, -15)  # raw deflate (C# DeflateStream compatible)
        payload = comp.compress(png) + comp.flush()
        entries.append((i, payload, len(png)))
    index = bytearray(struct.pack('<i', len(entries)))
    offset = 43
    body = bytearray()
    for eid, payload, uncomp in entries:
        index.extend(struct.pack('<BiiiqBB', 1, eid, uncomp, len(payload), offset, COMPRESS_DEFLATE, CODEC_PNG))
        body.extend(payload)
        offset += len(payload)
    return bytes(index), bytes(body)


def write_zl2(path, images):
    png_payloads = {}
    for i, image in enumerate(images):
        if image is not None:
            png_payloads[i] = png_bytes(image)
    meta = metadata_block(images, png_payloads)
    index, body = index_block(png_payloads)
    meta_offset = 43 + len(body)
    index_offset = meta_offset + len(meta)
    header = struct.pack('<3siiiBBh', b'ZL2', 2, len(images), 0, COMPRESS_DEFLATE, 0, 0)
    header += struct.pack('<qiqi', meta_offset, len(meta), index_offset, len(index))
    assert len(header) == 43
    Path(path).write_bytes(header + body + meta + index)
    print(f'wrote {path}: images={len(images)} present={len(png_payloads)}')


def main():
    out = '/tmp/investigate'
    # 1. Mon-31: 整库按英雄杀 Mon-31.wtl 重建
    imgs31 = read_wtl(HERO / 'Mon-31.wtl')
    write_zl2(ZIRCON / 'Mon-31.Zl', imgs31)

    # 2. Mon-22: 英雄杀 Mon-22.wtl + 覆盖 s4/s5
    imgs22 = read_wtl(HERO / 'Mon-22.wtl')
    imgs15 = read_wtl(HERO / 'Mon-15.wtl')
    # 钻卡树 英雄杀 Mon-15 s3 (3000..3999) -> Mon-22 s4 (4000..4999)
    for i in range(1000):
        imgs22[4000 + i] = imgs15[3000 + i]
    # 火焰狮子5 英雄杀 Mon-15 s9 (9000..9999) -> Mon-22 s5 (5000..5999)
    for i in range(1000):
        imgs22[5000 + i] = imgs15[9000 + i]
    write_zl2(ZIRCON / 'Mon-22.Zl', imgs22)

    print('done')


if __name__ == '__main__':
    main()
