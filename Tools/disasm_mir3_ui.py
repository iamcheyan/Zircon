#!/usr/bin/env python3
"""Disassemble Mir3.exe to find exact DrawImage call sites and (index, x, y) coordinates."""
import struct
import re

def search_push_patterns(data: bytes):
    print(f"Scanning {len(data)} bytes for x86 Push/Call sequences...")
    # Look for sequences of push 0xXX, push 0xXX, push 0xXX followed by call
    # 6A xx: push byte
    # 68 xx xx xx xx: push dword
    
    # In Delphi/C++ Mir3 client, UI drawing calls often look like:
    # PUSH Y (6A/68) -> PUSH X (6A/68) -> PUSH Index (6A/68) -> CALL DrawImage
    
    # Search for index 100 (0x64), 101 (0x65), 102 (0x66), 105 (0x69)
    for idx in range(100, 116):
        pattern = b'\x6a' + bytes([idx]) # push byte idx
        pos = 0
        while True:
            found = data.find(pattern, pos)
            if found == -1:
                break
            # Inspect 20 bytes before and after
            start = max(0, found - 16)
            end = min(len(data), found + 24)
            ctx = data[start:end]
            hex_str = ctx.hex(' ')
            print(f"Index {idx} (0x{idx:02X}) at 0x{found:08X}: {hex_str}")
            pos = found + 1

def main():
    path = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe"
    with open(path, "rb") as f:
        data = f.read()
    search_push_patterns(data)

if __name__ == "__main__":
    main()
