#!/usr/bin/env python3
"""Mud3 DAT 解码探测脚本 — 只读 NAS 原文件, 不改动.
分析 magic.dat / stditem.dat / monster.dat 的记录结构.
"""
import struct, sys
from collections import Counter

NAS = "/home/tetsuya/NAS/TMP/Mud3/Envir"

def load(path, rsize, xork):
    d = open(path, 'rb').read()
    n = struct.unpack('<I', d[:4])[0]
    return n, [bytes(b ^ xork for b in d[4 + i*rsize:4 + (i+1)*rsize]) for i in range(n)]

def u32(r, o): return struct.unpack('<I', r[o:o+4])[0]
def u16(r, o): return struct.unpack('<H', r[o:o+2])[0]

def gbk_at(r, off):
    """length-prefixed GBK name at off (1-byte len then bytes)."""
    if off >= len(r): return ''
    ln = r[off]
    if ln <= 0 or off + 1 + ln > len(r): return ''
    try:
        s = r[off+1:off+1+ln].decode('gbk')
    except Exception:
        return ''
    return s if all(ord(c) > 0x20 or c == ' ' for c in s) else ''

def gbk_fixed(r, off, length):
    try:
        return r[off:off+length].decode('gbk').split('\x00')[0]
    except Exception:
        return ''

def scan_names(recs, rsize):
    """try every offset as a 1-byte-length-prefixed GBK name; report offsets where >=80% records yield plausible names."""
    best = []
    for off in range(rsize - 4):
        ok = 0; total = 0; samples = []
        for r in recs:
            nm = gbk_at(r, off)
            if nm:
                ok += 1
                if len(samples) < 6: samples.append(nm)
        if ok >= len(recs) * 0.8:
            best.append((off, ok, samples))
    return best

def main():
    which = sys.argv[1] if len(sys.argv) > 1 else 'magic'

    if which == 'magic':
        n, recs = load(f"{NAS}/magic.dat", 120, 0x11)
        print(f"== magic.dat n={n} rsize=120 xor=0x11")
        ids = [u32(r,0) for r in recs]
        print("ids sorted:", sorted(ids))
        print("name@104:", [gbk_at(r,104) for r in recs[:10]])
        # how many records have a name at 104
        named = sum(1 for r in recs if gbk_at(r,104))
        print(f"records with GBK name@104: {named}/{n}")
        # field nonzero-rate
        print("\nnonzero-rate per u32 offset:")
        for off in range(0, 120, 4):
            vals = [u32(r,off) for r in recs]
            nz = sum(1 for v in vals if v)
            if nz:
                c = Counter(v for v in vals if v)
                top = c.most_common(5)
                print(f"  off{off:3}: nz={nz:3}/{n} range={min(vals)}..{max(vals)} top={top}")

    elif which == 'stditem':
        n, recs = load(f"{NAS}/stditem.dat", 184, 0x04)
        print(f"== stditem.dat n={n} rsize=184 xor=0x04")
        # index check 0-based
        bad = [i for i,r in enumerate(recs) if u32(r,0) != i]
        print("index@0 0-based mismatches:", len(bad), bad[:10])
        print("\nGBK name scan (length-prefixed):")
        for off, ok, samples in scan_names(recs, 184):
            print(f"  off{off}: {ok}/{n} e.g. {samples}")
        print("\nnonzero-rate per u32 offset (nonzero only):")
        for off in range(4, 184, 4):
            vals = [u32(r,off) for r in recs]
            nz = sum(1 for v in vals if v)
            if nz:
                c = Counter(v for v in vals if v)
                top = c.most_common(3)
                print(f"  off{off:3}: nz={nz:3}/{n} min={min(vals)} max={max(vals)} top={top}")

    elif which == 'monster':
        d = open(f"{NAS}/monster.dat",'rb').read()
        print(f"== monster.dat size={len(d)}")
        print("header 16:", d[:16].hex())
        hdr = struct.unpack('<I', d[:4])[0]
        print("first u32:", hdr)
        # try factoring payload
        payload = len(d) - 4
        print("payload len:", payload)
        import math
        divs = [x for x in range(1, 300) if payload % x == 0]
        print("divisors <=300 of payload:", divs)

    print("done")

if __name__ == '__main__':
    main()
