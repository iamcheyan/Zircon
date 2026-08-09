#!/usr/bin/env python3
"""find_gameinter_calls.py — Find code sections referencing GameInter library handle."""
import struct

def main():
    path = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe"
    with open(path, "rb") as f:
        data = f.read()

    # Address of string ".\\Data\\gameinter.wil" in binary
    str_offset = data.lower().find(b'gameinter.wil')
    print(f"String '.\\Data\\gameinter.wil' offset: 0x{str_offset:05X}")

    # Search for references to str_offset or VA (Virtual Address assuming imagebase 0x00400000)
    va = 0x00400000 + str_offset
    va_bytes = struct.pack("<I", va)
    print(f"Target VA: 0x{va:08X} (bytes: {va_bytes.hex()})")

    pos = 0
    while True:
        idx = data.find(va_bytes, pos)
        if idx == -1:
            break
        print(f"Found VA reference at offset: 0x{idx:05X} (VA: 0x{0x00400000+idx:08X})")
        pos = idx + 4

if __name__ == "__main__":
    main()
