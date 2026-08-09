#!/usr/bin/env python3
"""extract_wemade_layout.py — Extract Wemade dat files and search for UI layout structures."""
import sys
import os
import struct

# Wemade crypt unpacker
def decode_wemade_bytes(data: bytes) -> bytes:
    # Key XOR decryption used by Wemade 2002 Mir3 EI client
    key = b"Mir3WemadeEI"
    out = bytearray(len(data))
    for i in range(len(data)):
        out[i] = data[i] ^ key[i % len(key)]
    return bytes(out)

def main():
    dat_path = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/wemade.dat"
    if not os.path.exists(dat_path):
        print("wemade.dat not found:", dat_path)
        return

    with open(dat_path, "rb") as f:
        data = f.read()

    print("Loaded wemade.dat size:", len(data))

    # Inspect first 200 bytes
    print("Header bytes:", data[:64].hex())

if __name__ == "__main__":
    main()
