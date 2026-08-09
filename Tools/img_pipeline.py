#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""img_pipeline.py — 从 EI/mir3ei 客户端 WIL 提取全部条目图片到 /tmp/wiki_imgs/。

数据源: /tmp/wiki_data_v2.json 各板块 img 字段 {lib, frame}
输出:   /tmp/wiki_imgs/{board}/{id}.png (trim + 缩放, 透明背景)
  monsters  -> 96x96  (正面站立帧)
  items     -> 64x64  (StoreItem 图标)
  skills    -> 64x64  (MIcon 图标)
  npcs      -> 96x96  (NPCface 头像, 回退 NPC.wil 全身)
  companion -> 96x96  (绑定怪物正面)

库优先级: mir3ei 客户端优先 (内容更新), 回退 EI 客户端。
"""
import json, os, re, struct, sys

ROOT = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ROOT)
import wilsdk
from PIL import Image

EI_DATA = "/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data"
MEI_DATA = "/home/tetsuya/NAS/TMP/mir3ei/Data"
ZIR_DATA = "/home/tetsuya/development/Zircon/Debug/Client/Data"
OUT = "/tmp/wiki_imgs"

SIZES = {"monsters": 96, "items": 64, "skills": 64, "npcs": 96, "companion": 96}

# ---------------------------------------------------------------- DXT1 解码 (共用)
def decode_dxt1(data, w, h):
    """DXT1/BC1 → RGBA 像素 (字节串)。 宽高须为 4 的倍数。"""
    bw, bh = (w + 3) // 4, (h + 3) // 4
    px = bytearray(w * h * 4)

    def c565(v):
        return ((v >> 11) & 0x1F) << 3, ((v >> 5) & 0x3F) << 2, (v & 0x1F) << 3

    for by in range(bh):
        for bx in range(bw):
            off = (by * bw + bx) * 8
            c0, c1 = struct.unpack_from("<HH", data, off)
            idx = data[off + 4:off + 8]
            r0, g0, b0 = c565(c0)
            r1, g1, b1 = c565(c1)
            if c0 > c1:
                pal = [(r0, g0, b0), (r1, g1, b1),
                       ((2 * r0 + r1) // 3, (2 * g0 + g1) // 3, (2 * b0 + b1) // 3),
                       ((r0 + 2 * r1) // 3, (g0 + 2 * g1) // 3, (b0 + 2 * b1) // 3)]
            else:
                pal = [(r0, g0, b0), (r1, g1, b1),
                       ((r0 + r1) // 2, (g0 + g1) // 2, (b0 + b1) // 2), (0, 0, 0)]
            for iy in range(4):
                for ix in range(4):
                    x, y = bx * 4 + ix, by * 4 + iy
                    if x >= w or y >= h:
                        continue
                    code = (idx[iy] >> (ix * 2)) & 3
                    r, g, b = pal[code]
                    a = 0 if (c0 <= c1 and code == 3) else 255
                    o = (y * w + x) * 4
                    px[o:o + 4] = bytes((b, g, r, a))
    return px

# ---------------------------------------------------------------- Zl v0 (DXT1)
class ZlLibrary:
    """Zircon 的旧格式 .Zl 图库 (v0): 内嵌索引 + DXT1 图像。接口对齐 wilsdk.WilLibrary。"""

    def __init__(self, path):
        self.path = path
        self.name = os.path.basename(path)
        with open(path, "rb") as f:
            meta_size = struct.unpack("<I", f.read(4))[0]
            meta = f.read(meta_size)
            self._file = open(path, "rb")
            self._meta = meta
        value = struct.unpack("<I", meta[0:4])[0]
        self.count = value & 0x1FFFFFF
        self.version = (value >> 25) & 0x7F
        self._frames = []
        pos = 4
        for _ in range(self.count):
            present = meta[pos]
            pos += 1
            if present:
                self._frames.append(struct.unpack("<ihhhhBhhhhhh", meta[pos:pos + 25]))
                pos += 25
            else:
                self._frames.append(None)

    def decode(self, index):
        m = self._frames[index] if 0 <= index < self.count else None
        if m is None or m[0] == 0:
            return None
        posn, w, h = m[0], m[1], m[2]
        if w <= 0 or h <= 0:
            return None
        aw, ah = (w + 3) // 4 * 4, (h + 3) // 4 * 4
        self._file.seek(posn)
        payload = self._file.read(aw * ah // 2)
        px = decode_dxt1(payload, aw, ah)
        img = Image.frombuffer("RGBA", (aw, ah), bytes(px), "raw", "RGBA", 0, 1)
        return img.crop((0, 0, w, h))

_lib_cache = {}

# ---------------------------------------------------------------- Zl2 (ZL2 容器)
class Zl2Library:
    """Zircon 的 ZL2 图库 (v2): 头部 + 索引表 + metadata + Deflate payload。
    物品图标实际 codec 为 Bgra32/Png (无 DXT/BC7)。接口对齐 wilsdk.WilLibrary。"""

    def __init__(self, path):
        self.path = path
        self.name = os.path.basename(path)
        with open(path, "rb") as f:
            self._d = f.read()
        sig, ver, imgcount, atlascount, defcomp, flags, reserved = struct.unpack(
            "<3siiibbH", self._d[:19]
        )
        meta_off, meta_size, idx_off, idx_size = struct.unpack(
            "<qiqi", self._d[19:43]
        )
        self.count = imgcount
        self.version = ver
        # index
        idx = self._d[idx_off:idx_off + idx_size]
        (n,) = struct.unpack("<i", idx[:4])
        self._entries = {}
        pos = 4
        for _ in range(n):
            t, eid, unc, comp, off, c, codec = struct.unpack(
                "<BiiiqBB", idx[pos:pos + 23]
            )
            self._entries[eid] = (unc, comp, off, c, codec)
            pos += 23
        # metadata
        md = self._d[meta_off:meta_off + meta_size]
        mv, count, agic, aps = struct.unpack("<iiii", md[:16])
        self._imgs = {}
        pos = 16
        for i in range(count):
            present = md[pos]
            pos += 1
            if not present:
                continue
            m = struct.unpack("<ihhhhBhhhhhh", md[pos:pos + 25])
            pos += 25
            (ap,) = struct.unpack("<i", md[pos:pos + 4])
            pos += 4
            sx, sy, sw, sh, vx, vy, vw, vh = struct.unpack(
                "<hhhhhhhh", md[pos:pos + 16]
            )
            pos += 16
            ic, sc, oc = md[pos:pos + 3]
            pos += 3
            rp, srp, orp = md[pos:pos + 3]
            pos += 3
            sds, b7s, fbs, s_sds, s_b7s, s_fbs, o_sds, o_b7s, o_fbs = struct.unpack(
                "<9i", md[pos:pos + 36]
            )
            pos += 36
            self._imgs[i] = {
                "pos": m[0], "w": m[1], "h": m[2], "ox": m[3], "oy": m[4],
                "stype": m[5], "sw": m[6], "sh": m[7],
                "codec": ic, "pref": rp,
                "stored": sds, "bc7": b7s, "fallback": fbs,
            }

    def _payload(self, entry_id):
        e = self._entries.get(entry_id)
        if e is None:
            return None
        unc, comp, off, c, codec = e
        if c == 0:  # ZlContainerCompression.None — 原始数据
            return self._d[off:off + unc]
        import zlib
        return zlib.decompress(self._d[off:off + comp], -15)

    def decode(self, index):
        m = self._imgs.get(index)
        if m is None or m["w"] <= 0 or m["h"] <= 0:
            return None
        payload = self._payload(m["pos"])
        if not payload:
            return None
        data = payload[:m["stored"]]
        codec = m["codec"]
        if data:
            if codec == 2:  # Bgra32
                raw = data[: m["w"] * m["h"] * 4]
                img = Image.frombuffer(
                    "RGBA", (m["w"], m["h"]), raw, "raw", "BGRA", 0, 1
                )
                return img
            if codec == 4:  # Png
                import io
                return Image.open(io.BytesIO(data)).convert("RGBA")
            if codec == 0:  # Dxt1
                aw, ah = (m["w"] + 3) // 4 * 4, (m["h"] + 3) // 4 * 4
                px = decode_dxt1(data, aw, ah)
                img = Image.frombuffer("RGBA", (aw, ah), bytes(px), "raw", "RGBA", 0, 1)
                return img.crop((0, 0, m["w"], m["h"]))
            if codec == 3:  # Bc7 (BPTC) — 构造 DDS 头喂 dds 库
                return _decode_bc7(data, m["w"], m["h"])
        # Bc7 / fallback 段
        if m["bc7"] > 0:
            bc7 = payload[m["stored"]:m["stored"] + m["bc7"]]
            if bc7:
                return _decode_bc7(bc7, m["w"], m["h"])
        if m["fallback"] > 0:
            fb = payload[m["stored"] + m["bc7"]:m["stored"] + m["bc7"] + m["fallback"]]
            if fb:
                aw, ah = (m["w"] + 3) // 4 * 4, (m["h"] + 3) // 4 * 4
                px = decode_dxt1(fb, aw, ah)
                img = Image.frombuffer("RGBA", (aw, ah), bytes(px), "raw", "RGBA", 0, 1)
                return img.crop((0, 0, m["w"], m["h"]))
        return None


def _decode_bc7(data, w, h):
    """BC7 (BPTC_UNORM) 解码: 裸块数据 → RGBA PIL。
    texture2ddecoder.decode_bc7 输出 BGR(A) 字节序, 需交换 R/B。
    已与客户端同款 BCnEncoder 逐像素比对 (0 差异)。"""
    try:
        import texture2ddecoder as _t2d
    except ImportError:
        return None
    px = _t2d.decode_bc7(data, w, h)
    arr = bytearray(px)
    for i in range(0, len(arr), 4):
        arr[i], arr[i + 2] = arr[i + 2], arr[i]
    from PIL import Image
    return Image.frombytes("RGBA", (w, h), bytes(arr))


def get_zir_lib(name):
    """仅 Zircon 侧图库 (Zl v0 / ZL2)。"""
    key = "zir:" + name.lower()
    if key in _lib_cache:
        return _lib_cache[key]
    zname = re.sub(r"\.wil$", ".Zl", name, flags=re.IGNORECASE)
    zl_key = zname.lower()
    for zf in os.listdir(ZIR_DATA):
        if zf.lower() == zl_key:
            try:
                with open(os.path.join(ZIR_DATA, zf), "rb") as _f:
                    sig = _f.read(4)
                zl = Zl2Library(os.path.join(ZIR_DATA, zf)) if sig.startswith(b"ZL2") else ZlLibrary(os.path.join(ZIR_DATA, zf))
                _lib_cache[key] = zl
                return zl
            except Exception:
                break
    _lib_cache[key] = None
    return None

def get_lib(name):
    """按库名取图库; 大小写不敏感。优先级: mir3ei > EI (WIL), 回退 Zircon (Zl/ZL2)。"""
    """按库名取图库; 大小写不敏感。优先级: mir3ei > EI (WIL), 回退 Zircon (Zl)。"""
    key = name.lower()
    if key in _lib_cache:
        return _lib_cache[key]
    for d in (MEI_DATA, EI_DATA):
        if not os.path.isdir(d):
            continue
        for fname in os.listdir(d):
            if fname.lower() == key:
                try:
                    wl = wilsdk.open_library(os.path.join(d, fname))
                    _lib_cache[key] = wl
                    return wl
                except Exception:
                    pass
    zl = get_zir_lib(name)
    if zl is not None:
        _lib_cache[key] = zl
        return zl
    _lib_cache[key] = None
    return None

def trim_scale(img, size):
    """裁透明边 → 缩放到 size (保持比例, 居中, 透明背景)。"""
    bbox = img.getbbox()
    if not bbox:
        return None
    img = img.crop(bbox)
    w, h = img.size
    s = size / max(w, h)
    if s < 1:
        img = img.resize((max(1, int(w * s)), max(1, int(h * s))), 1)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.paste(img, ((size - img.size[0]) // 2, (size - img.size[1]) // 2), img)
    return canvas

def render(board, item_id, img_spec):
    """返回 (ok, reason)。"""
    frame = img_spec.get("frame", 0)
    lib = get_lib(img_spec["lib"])
    if lib is None:
        lib = get_zir_lib(img_spec["lib"])
    if lib is None:
        return False, f"库缺失 {img_spec['lib']}"
    if frame >= lib.count:
        # 帧号超出客户端 WIL → 换 Zircon 扩展库 (ZL2 含新装备帧)
        zir = get_zir_lib(img_spec["lib"])
        if zir is not None and frame < zir.count:
            lib = zir
    if frame >= lib.count:
        return False, f"帧越界 {frame}/{lib.count}"
    im = lib.decode(frame)
    if im is None and lib is not get_zir_lib(img_spec["lib"]):
        zir = get_zir_lib(img_spec["lib"])
        if zir is not None and frame < zir.count:
            im = zir.decode(frame)
            if im is not None:
                lib = zir
    if im is None:
        # 回退: 该 shape 块内的站立帧 (方向 0..9 的站立帧 0..90), 再到块首帧
        base = frame // 1000 * 1000
        for alt in [base, base + 10, base + 20, base + 30, base + 40, base + 50, base + 60, base + 70, base + 80, base + 90]:
            if alt >= lib.count:
                continue
            im = lib.decode(alt)
            if im is not None:
                break
    if im is None:
        return False, f"帧空 {frame}"
    out = trim_scale(im, SIZES.get(board, 96))
    if out is None:
        return False, f"全透明 {frame}"
    d = os.path.join(OUT, board)
    os.makedirs(d, exist_ok=True)
    out.save(os.path.join(d, f"{item_id}.png"))
    return True, ""

def main():
    w = json.load(open("/tmp/wiki_data_v2.json"))
    total = ok = 0
    fails = []
    for board in ["monsters", "items", "skills", "npcs", "companion"]:
        for x in w[board]:
            spec = x.get("img")
            if not spec:
                continue
            total += 1
            good, reason = render(board, x["id"], spec)
            if good:
                ok += 1
            else:
                fails.append((board, x["id"], x["name"], reason))
    print(f"图片管线: {ok}/{total} 成功")
    if fails:
        print(f"失败 {len(fails)} 条:")
        for b, i, n, r in fails[:25]:
            print(f"  {b} #{i} {n}: {r}")

if __name__ == "__main__":
    main()
