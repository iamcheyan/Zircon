#!/usr/bin/env python3
"""zl2writer.py — Python writer for Zircon ZL2 compressed image libraries.

Produces files readable by GodotClient/Formats/ZlReader.cs (TryReadCompressedContainer)
and RenderingCore/Library/MirLibrary.cs.  Uses PNG codec (lossless, alpha preserved,
no DXT compression artifacts).

ZL2 container layout (all little-endian):
  Header (43 bytes): "ZL2" + Version(i32) + ImageCount(i32) + AtlasCount(i32)
    + DefaultCompression(u8) + Flags(u8) + Reserved(i16)
    + MetaOffset(i64) + MetaSize(i32) + IndexOffset(i64) + IndexSize(i32)
  Metadata block: Version(i32) + Count(i32) + AtlasGroupImageCount(i32)
    + AtlasPageSize(i32) + per-image entries
  Data blocks: one Deflate-compressed PNG payload per non-blank frame
  Index block: EntryCount(i32) + entries (23 bytes each)
"""
from __future__ import annotations

import io
import struct
import zlib

from PIL import Image

# --- Constants matching C# enums ---
ZL2_SIGNATURE = b"ZL2"
ZL2_VERSION = 2
ATLAS_COUNT = 0
FLAGS_NO_ATLAS = 0
COMPRESSION_NONE = 0
COMPRESSION_DEFLATE_FAST = 1

ZL_ENTRY_IMAGE_PAYLOAD = 0
CODEC_PNG = 4

# --- Metadata per-image layout (version >= 2) ---
# Present(u8) + Position(i32) + W(i16)+H(i16)+OX(i16)+OY(i16) + ShadowType(u8)
# + ShadowW(i16)+ShadowH(i16)+ShadowOX(i16)+ShadowOY(i16)
# + OverlayW(i16)+OverlayH(i16)
# + AtlasPage(i32) + SrcRect(4×i16) + VisBounds(4×i16)
# + ImageCodec(u8)+ShadowCodec(u8)+OverlayCodec(u8)
# + RuntimePref(3×u8)
# + StoredImgSize(i32)+Bc7Size(i32)+FallbackSize(i32)
# + StoredShadowSize(i32)+ShadowBc7Size(i32)+ShadowFallbackSize(i32)
# + StoredOverlaySize(i32)+OverlayBc7Size(i32)+OverlayFallbackSize(i32)


def _png_bytes(img: Image.Image) -> bytes:
    """Encode RGBA PIL Image to PNG bytes."""
    buf = io.BytesIO()
    img.save(buf, format="PNG", optimize=False, compress_level=1)
    return buf.getvalue()


def write_zl2(path: str, frames: list, *, use_deflate: bool = True):
    """Write a ZL2 library file.

    Args:
        path: output .Zl file path
        frames: list of dicts, one per frame index.  Each dict:
            None  → blank/absent frame (Present=false)
            dict  → present frame with keys:
                image: PIL RGBA Image (required for non-blank)
                offsetX: int (default 0)
                offsetY: int (default 0)
                shadowType: int (default 0)
        use_deflate: compress payloads with Deflate (True) or store raw (False)
    """
    count = len(frames)

    # Build metadata + collect payloads
    meta_buf = io.BytesIO()
    meta = struct.Struct

    # Metadata header
    meta_buf.write(struct.pack("<iiii", ZL2_VERSION, count, 0, 0))  # Version, Count, AtlasGroup, AtlasPage

    payloads = []  # (entry_id, compressed_bytes, uncompressed_size)
    entry_id = 0

    for i in range(count):
        frame = frames[i]
        if frame is None:
            meta_buf.write(struct.pack("<B", 0))  # Present = false
            continue

        meta_buf.write(struct.pack("<B", 1))  # Present = true

        img = frame["image"]
        w, h = img.size
        ox = frame.get("offsetX", 0)
        oy = frame.get("offsetY", 0)
        shadow_type = frame.get("shadowType", 0)

        # Encode PNG
        png_data = _png_bytes(img)
        uncompressed_size = len(png_data)

        if use_deflate:
            # C# DeflateStream expects raw deflate (RFC1951), NOT zlib-wrapped
            # (RFC1950).  zlib.compress() adds a 2-byte header + adler32 trailer,
            # which GodotClient ZlReader.DecompressDeflate rejects with
            # "unsupported compression method".  Use wbits=-15 for raw deflate.
            co = zlib.compressobj(1, zlib.DEFLATED, -15)
            compressed = co.compress(png_data) + co.flush()
            if len(compressed) >= uncompressed_size:
                payload = png_data
                compression = COMPRESSION_NONE
            else:
                payload = compressed
                compression = COMPRESSION_DEFLATE_FAST
        else:
            payload = png_data
            compression = COMPRESSION_NONE

        position = entry_id  # entry id in index
        payloads.append((entry_id, payload, compression, uncompressed_size))
        entry_id += 1

        # Write image metadata (Mir3Image.SaveHeader format, version >= 2)
        meta_buf.write(struct.pack("<i", position))        # Position
        meta_buf.write(struct.pack("<hh", w, h))            # Width, Height
        meta_buf.write(struct.pack("<hh", ox, oy))          # OffSetX, OffSetY
        meta_buf.write(struct.pack("<B", shadow_type))      # ShadowType
        meta_buf.write(struct.pack("<hh", 0, 0))            # ShadowWidth, ShadowHeight
        meta_buf.write(struct.pack("<hh", 0, 0))            # ShadowOffSetX, ShadowOffSetY
        meta_buf.write(struct.pack("<hh", 0, 0))            # OverlayWidth, OverlayHeight
        # version >= 2 extensions
        meta_buf.write(struct.pack("<i", 0))                # AtlasPage
        meta_buf.write(struct.pack("<hhhh", 0, 0, w, h))    # SourceRectangle (x,y,w,h)
        meta_buf.write(struct.pack("<hhhh", 0, 0, w, h))    # VisibleBounds (x,y,w,h)
        meta_buf.write(struct.pack("<BBB", CODEC_PNG, 1, 1))  # ImageCodec=Png, ShadowCodec=Dxt5, OverlayCodec=Dxt5
        meta_buf.write(struct.pack("<BBB", 0, 0, 0))        # RuntimePreferences
        meta_buf.write(struct.pack("<i", uncompressed_size))  # StoredImageDataSize
        meta_buf.write(struct.pack("<i", 0))                # Bc7DataSize
        meta_buf.write(struct.pack("<i", 0))                # FallbackDataSize
        meta_buf.write(struct.pack("<i", 0))                # StoredShadowDataSize
        meta_buf.write(struct.pack("<i", 0))                # ShadowBc7DataSize
        meta_buf.write(struct.pack("<i", 0))                # ShadowFallbackDataSize
        meta_buf.write(struct.pack("<i", 0))                # StoredOverlayDataSize
        meta_buf.write(struct.pack("<i", 0))                # OverlayBc7DataSize
        meta_buf.write(struct.pack("<i", 0))                # OverlayFallbackDataSize

    metadata_bytes = meta_buf.getvalue()

    # Build index entries
    index_buf = io.BytesIO()
    index_buf.write(struct.pack("<i", len(payloads)))  # entry count
    data_offset = 43 + len(metadata_bytes)  # data starts after header + metadata

    for eid, payload, compression, uncompressed_size in payloads:
        payload_len = len(payload)
        index_buf.write(struct.pack("<B", ZL_ENTRY_IMAGE_PAYLOAD))  # Type
        index_buf.write(struct.pack("<i", eid))                     # Id
        index_buf.write(struct.pack("<i", uncompressed_size))       # UncompressedSize
        index_buf.write(struct.pack("<i", payload_len))             # CompressedSize
        index_buf.write(struct.pack("<q", data_offset))             # Offset
        index_buf.write(struct.pack("<B", compression))             # Compression
        index_buf.write(struct.pack("<B", CODEC_PNG))               # Codec
        data_offset += payload_len

    index_bytes = index_buf.getvalue()

    # Write file
    with open(path, "wb") as f:
        # Header (43 bytes)
        f.write(ZL2_SIGNATURE)                                  # 3
        f.write(struct.pack("<i", ZL2_VERSION))                 # 4
        f.write(struct.pack("<i", count))                       # 4
        f.write(struct.pack("<i", ATLAS_COUNT))                 # 4
        f.write(struct.pack("<B", COMPRESSION_DEFLATE_FAST if use_deflate else COMPRESSION_NONE))  # 1
        f.write(struct.pack("<B", FLAGS_NO_ATLAS))              # 1
        f.write(struct.pack("<h", 0))                           # 2  reserved
        meta_offset = 43
        f.write(struct.pack("<q", meta_offset))                 # 8  MetaOffset
        f.write(struct.pack("<i", len(metadata_bytes)))          # 4  MetaSize
        index_offset = meta_offset + len(metadata_bytes) + sum(len(p[1]) for p in payloads)
        f.write(struct.pack("<q", index_offset))                # 8  IndexOffset
        f.write(struct.pack("<i", len(index_bytes)))             # 4  IndexSize
        # = 43 bytes

        # Metadata
        f.write(metadata_bytes)

        # Data blocks
        for _, payload, _, _ in payloads:
            f.write(payload)

        # Index block
        f.write(index_bytes)

    return {
        "frame_count": count,
        "payload_count": len(payloads),
        "file_size": 43 + len(metadata_bytes) + sum(len(p[1]) for p in payloads) + len(index_bytes),
    }