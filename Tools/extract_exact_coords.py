#!/usr/bin/env python3
"""extract_exact_coords.py — Parse exact (Index, X, Y) array instructions from Mir3.exe binary."""
import struct

def main():
    path = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe"
    with open(path, "rb") as f:
        data = f.read()

    # Target method block: 0x49C00 to 0x4A100
    chunk = data[0x49C00:0x4A100]
    
    print("Disassembling 0x49C00 to 0x4A100 call parameters:")
    i = 0
    while i < len(chunk) - 8:
        # Check for push imm16/imm32 (0x68 xx xx xx xx) followed by push byte (0x6a xx)
        if chunk[i] == 0x68 and chunk[i+5] == 0x6a:
            val_x = struct.unpack_from("<I", chunk, i+1)[0]
            val_idx = chunk[i+6]
            offset = 0x49C00 + i
            print(f"0x{offset:05X}: Index={val_idx:3d} (0x{val_idx:02X}), X_coord={val_x}")
            i += 7
        elif chunk[i] == 0x6a and chunk[i+2] == 0x68: # push byte idx, push dword X
            val_idx = chunk[i+1]
            val_x = struct.unpack_from("<I", chunk, i+3)[0]
            offset = 0x49C00 + i
            print(f"0x{offset:05X}: Index={val_idx:3d} (0x{val_idx:02X}), X_coord={val_x}")
            i += 7
        else:
            i += 1

if __name__ == "__main__":
    main()
