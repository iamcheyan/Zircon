#!/usr/bin/env python3
"""Decode EI 3.0 Magic.exp/MExplain.exp using Mir3.exe's 0x4525f0 logic.

This reproduces the verified client-side routine rather than applying a guessed
whole-file XOR.  The original files are never modified; decoded bytes are
written to the requested output path.
"""
from __future__ import annotations

import argparse
import struct
from pathlib import Path


# 0x452580 stores these two dwords in the global decode context.  The second
# value is written directly by the executable (0x9fde1a93).  The first value
# comes from a caller-provided 4-byte seed; the supplied files use different
# seeds, so it must not be hard-coded globally.
KEY2 = 0x9FDE1A93


def checksum(payload: bytes) -> int:
    # 0x4525f0: sum((byte[i] + 1) * i), i=0..decoded_length-9.
    return sum((value + 1) * index for index, value in enumerate(payload)) & 0xFFFFFFFF


def decode(data: bytes, key1: int | None = None) -> tuple[bytes, dict]:
    if len(data) < 8:
        raise ValueError("file is shorter than the 8-byte EI header")
    raw_length, raw_check = struct.unpack_from("<II", data, 0)
    # For the supplied EI files the decoded stream occupies the complete
    # file.  This gives a deterministic seed candidate, then the executable's
    # checksum proves or rejects it.  --key1 can be used for files with
    # padding or a different loader context.
    selected_key1 = key1 if key1 is not None else (raw_length ^ len(data))
    decoded_length = raw_length ^ selected_key1
    if decoded_length < 8 or decoded_length > len(data):
        raise ValueError(
            f"decoded length {decoded_length} is outside file size {len(data)}; "
            "the runtime seed may differ for this client build"
        )
    payload_len = decoded_length - 8
    payload = bytearray(data[8 : 8 + payload_len])
    expected = KEY2 ^ checksum(payload)
    if expected != raw_check:
        raise ValueError(
            f"header checksum mismatch: expected 0x{expected:08x}, "
            f"got 0x{raw_check:08x}"
        )

    # 0x45267f..0x45269a: four passes, starting with header bytes 3,2,1,0;
    # the XOR byte increments for every payload byte in each pass.
    for header_index in range(3, -1, -1):
        mask = data[header_index]
        for index in range(len(payload)):
            payload[index] ^= mask
            mask = (mask + 1) & 0xFF

    meta = {
        "input_size": len(data),
        "raw_length": raw_length,
        "decoded_length": decoded_length,
        "payload_length": payload_len,
        "raw_checksum": raw_check,
        "expected_checksum": expected,
        "key1": selected_key1,
        "key2": KEY2,
        "algorithm_source": "Mir3.exe 0x00452580 + 0x004525F0",
    }
    return bytes(payload), meta


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("inputs", nargs="+", type=Path)
    ap.add_argument("--out-dir", type=Path, default=Path("/tmp/mir3-exp-decoded"))
    ap.add_argument("--key1", type=lambda value: int(value, 0), default=None,
                    help="override the first 32-bit decode seed (hex or decimal)")
    args = ap.parse_args()
    args.out_dir.mkdir(parents=True, exist_ok=True)
    for source in args.inputs:
        decoded, meta = decode(source.read_bytes(), args.key1)
        target = args.out_dir / f"{source.name}.decoded"
        target.write_bytes(decoded)
        print(f"{source}: {meta['input_size']} -> {meta['payload_length']} bytes; {target}")
        try:
            text = decoded.decode("gb18030")
        except UnicodeDecodeError:
            text = decoded.decode("gb18030", errors="replace")
        print("  preview:", repr(text[:120]))


if __name__ == "__main__":
    main()
