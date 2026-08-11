#!/usr/bin/env python3
"""Replace selected frames in a ZL2 MiniMap library without changing bindings.

Used for the mixed deployment where most maps use the EI MiniMap.Zl but a
Zircon-only map (currently map 3 / Sabuk Keep) keeps its original map and must
keep the matching Zircon minimap frame.
"""
from __future__ import annotations

import io
import struct
import sys
import zlib
from pathlib import Path

from PIL import Image

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
from zl2writer import write_zl2  # noqa: E402


def read_zl2(path: Path):
    data = path.read_bytes()
    if data[:3] != b"ZL2":
        raise ValueError(f"{path} is not a ZL2 file")
    (_signature, version, image_count, _atlas_count, _default_compression, _flags,
     _reserved, metadata_offset, metadata_size, index_offset,
     index_size) = struct.unpack_from("<3siiiBBhqiqi", data, 0)
    if version != 2:
        raise ValueError(f"unsupported ZL2 version {version}")

    entries = {}
    pos = index_offset
    (entry_count,) = struct.unpack_from("<i", data, pos)
    pos += 4
    for _ in range(entry_count):
        entry_type, entry_id, uncompressed, compressed, offset, compression, codec = \
            struct.unpack_from("<BiiiqBB", data, pos)
        pos += 23
        entries[entry_id] = (offset, compressed, compression, codec, uncompressed)

    meta = memoryview(data)[metadata_offset:metadata_offset + metadata_size]
    version2, count, _group_count, _page_size = struct.unpack_from("<iiii", meta, 0)
    if count != image_count:
        raise ValueError("ZL2 image count mismatch")
    pos = 16
    frames = []
    for index in range(count):
        (present,) = struct.unpack_from("<B", meta, pos)
        pos += 1
        if not present:
            frames.append(None)
            continue
        # Metadata layout matches Tools/zl2writer.py and ZlReader.cs.
        (position, width, height, offset_x, offset_y, shadow_type,
         _shadow_w, _shadow_h, _shadow_ox, _shadow_oy,
         _overlay_w, _overlay_h) = struct.unpack_from("<ihhhhBhhhhhh", meta, pos)
        pos += struct.calcsize("<ihhhhBhhhhhh")
        pos += 4 + 8 + 8  # atlas page, source rectangle, visible bounds
        image_codec, _shadow_codec, _overlay_codec = struct.unpack_from("<BBB", meta, pos)
        pos += 3 + 3  # codecs + runtime preferences
        sizes = struct.unpack_from("<iiiiiiiii", meta, pos)
        pos += 36
        entry = entries.get(position)
        if entry is None:
            raise ValueError(f"missing payload entry for frame {index}, id {position}")
        payload = data[entry[0]:entry[0] + entry[1]]
        if entry[2] == 1:
            payload = zlib.decompress(payload, -15)
        elif entry[2] != 0:
            raise ValueError(f"unsupported compression {entry[2]}")
        image = Image.open(io.BytesIO(payload)).convert("RGBA")
        frames.append({"image": image, "offsetX": offset_x, "offsetY": offset_y,
                       "shadowType": shadow_type})
    return frames


def main() -> int:
    if len(sys.argv) < 4:
        print("usage: merge_minimap_override.py <base.Zl> <output.Zl> <index> <png> [...] [--metadata-from <source.Zl>]")
        return 2
    base = Path(sys.argv[1])
    output = Path(sys.argv[2])
    frames = read_zl2(base)
    replacements = sys.argv[3:]
    metadata_source = None
    explicit_offset = None
    if "--metadata-from" in replacements:
        marker = replacements.index("--metadata-from")
        if marker + 1 >= len(replacements):
            raise ValueError("--metadata-from requires a ZL file")
        metadata_source = read_zl2(Path(replacements[marker + 1]))
        replacements = replacements[:marker]
    if "--offset" in replacements:
        marker = replacements.index("--offset")
        if marker + 2 >= len(replacements):
            raise ValueError("--offset requires X and Y")
        explicit_offset = (int(replacements[marker + 1]), int(replacements[marker + 2]))
        replacements = replacements[:marker]
    if len(replacements) % 2 != 0:
        print("usage: merge_minimap_override.py <base.Zl> <output.Zl> <index> <png> [...] [--metadata-from <source.Zl>]")
        return 2
    for i in range(0, len(replacements), 2):
        index = int(replacements[i])
        png = Path(replacements[i + 1])
        if not 0 <= index < len(frames):
            raise ValueError(f"frame index out of range: {index}")
        if frames[index] is None:
            raise ValueError(f"base frame is absent: {index}")
        image = Image.open(png).convert("RGBA")
        frames[index]["image"] = image
        if metadata_source is not None:
            if metadata_source[index] is None:
                raise ValueError(f"metadata source frame is absent: {index}")
            frames[index]["offsetX"] = metadata_source[index]["offsetX"]
            frames[index]["offsetY"] = metadata_source[index]["offsetY"]
            frames[index]["shadowType"] = metadata_source[index]["shadowType"]
        if explicit_offset is not None:
            frames[index]["offsetX"], frames[index]["offsetY"] = explicit_offset
        print(f"override frame={index} size={image.size} source={png}")
    stats = write_zl2(str(output), frames)
    print(f"wrote frames={len(frames)} payloads={stats['payload_count']} size={stats['file_size']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
