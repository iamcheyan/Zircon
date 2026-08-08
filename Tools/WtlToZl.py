#!/usr/bin/env python3
"""Convert legacy WTL v1.3 images to a minimal ZL2 BGRA32 library.

This is intentionally dependency-free and mirrors ImageManager/WTLLibrary's
legacy block decoder. It is used for source assets which have not yet been
converted by the Windows LibraryEditor (currently MagicEx10.wtl).
"""
import struct
import sys
from pathlib import Path


def unpack_colour(block, offset, colours, dst):
    value = block[offset] | (block[offset + 1] << 8)
    blue = (value >> 11) & 0x1F
    green = (value >> 5) & 0x3F
    red = value & 0x1F
    colours[dst:dst + 4] = bytes((red * 255 // 31, green * 255 // 63,
                                  blue * 255 // 31, 255))
    return value


def decode_block(block):
    colours = bytearray(16)
    a = unpack_colour(block, 0, colours, 0)
    b = unpack_colour(block, 2, colours, 4)
    for i in range(3):
        c, d = colours[i], colours[4 + i]
        if a <= b:
            colours[8 + i] = (c + d) // 2
            colours[12 + i] = 0
        else:
            colours[8 + i] = (2 * c + d) // 3
            colours[12 + i] = (c + 2 * d) // 3
    colours[11] = 255
    colours[15] = 0 if a <= b else 255
    for i in range(4):
        if colours[i * 4:i * 4 + 4] == bytes((0, 0, 0, 255)):
            colours[i * 4:i * 4 + 3] = bytes((1, 1, 1))
    result = bytearray(64)
    for row in range(4):
        packed = block[4 + row]
        for col in range(4):
            index = (packed >> (col * 2)) & 3
            result[(row * 4 + col) * 4:(row * 4 + col + 1) * 4] = colours[index * 4:index * 4 + 4]
    return result


def decode_wtl_image(payload, width, height):
    texture_width = 2
    while texture_width < width:
        texture_width *= 2
    output = bytearray(width * height * 4)
    offset = 0
    cursor = 0
    while cursor < len(payload):
        if cursor + 8 > len(payload):
            break
        counts = payload[cursor:cursor + 8]
        cursor += 8
        for i, count in enumerate(counts):
            if i % 2 == 0:
                offset += count
                continue
            for _ in range(count):
                if cursor + 8 > len(payload):
                    break
                block = payload[cursor:cursor + 8]
                cursor += 8
                pixels = decode_block(block)
                block_x = offset % (texture_width // 4)
                block_y = offset // (texture_width // 4)
                x0, y0 = block_x * 4, block_y * 4
                for py in range(4):
                    for px in range(4):
                        x, y = x0 + px, y0 + py
                        if x >= width or y >= height:
                            continue
                        source = (py * 4 + px) * 4
                        target = (y * width + x) * 4
                        output[target:target + 4] = pixels[source:source + 4]
                offset += 1
    return output


def read_wtl(path):
    data = path.read_bytes()
    count = struct.unpack_from('<i', data, 28)[0]
    index = struct.unpack_from(f'<{count}i', data, 32)
    images = []
    for image_id, position in enumerate(index):
        if position <= 0 or position + 16 > len(data):
            images.append(None)
            continue
        width, height, off_x, off_y, shadow_x, shadow_y = struct.unpack_from('<hhhhhh', data, position)
        length = data[position + 12] | data[position + 13] << 8 | data[position + 14] << 16
        start = position + 16
        payload = data[start:start + length]
        if width <= 0 or height <= 0 or len(payload) != length:
            images.append(None)
            continue
        pixels = decode_wtl_image(payload, width, height)
        images.append((width, height, off_x, off_y, shadow_x, shadow_y, pixels))
    return images


def image_header(entry_id, image):
    width, height, off_x, off_y, shadow_x, shadow_y, pixels = image
    # ZL2 metadata: position is the image-payload entry id.
    return struct.pack('<ihhhhBhhhhhh i4h4h6B9i',
        entry_id, width, height, off_x, off_y, 0,
        0, 0, shadow_x, shadow_y, 0, 0,
        -1, 0, 0, width, height, 0, 0, width, height,
        2, 0, 0, 0, 0, 0,
        len(pixels), 0, 0, 0, 0, 0, 0, 0, 0)


def write_zl(path, images):
    payloads = {i: image[6] for i, image in enumerate(images) if image is not None}
    metadata = bytearray(struct.pack('<4i', 2, len(images), 0, 0))
    for i, image in enumerate(images):
        metadata.append(1 if image is not None else 0)
        if image is not None:
            metadata.extend(image_header(i, image))

    # Entry: type, id, uncompressed size, compressed size, absolute offset, compression, codec.
    index = bytearray(struct.pack('<i', len(payloads)))
    header_size = 43
    payload_offset = header_size
    entries = []
    for i in payloads:
        data = payloads[i]
        entries.append((i, payload_offset, len(data)))
        payload_offset += len(data)
    metadata_offset = payload_offset
    index_offset = metadata_offset + len(metadata)
    for i, offset, size in entries:
        index.extend(struct.pack('<Biii qBB', 1, i, size, size, offset, 0, 2))

    header = bytearray(b'ZL2')
    header.extend(struct.pack('<iiiBBh q i q i', 2, len(images), 0, 0, 0, 0,
                               metadata_offset, len(metadata), index_offset, len(index)))
    if len(header) != header_size:
        raise RuntimeError(f'bad ZL2 header size: {len(header)}')
    body = bytearray()
    for i, _, _ in entries:
        body.extend(payloads[i])
    path.write_bytes(header + body + metadata + index)


def main():
    if len(sys.argv) != 3:
        raise SystemExit('usage: WtlToZl.py input.wtl output.Zl')
    source, target = Path(sys.argv[1]), Path(sys.argv[2])
    images = read_wtl(source)
    write_zl(target, images)
    print(f'converted images={len(images)} valid={sum(x is not None for x in images)} output={target}')


if __name__ == '__main__':
    main()
