#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""thumb_gen.py — 批量渲染 EI 客户端 544 张地图的缩略图。

复用 mapviewer.py 的 MapCache/FramePool/world_bounds/cell_anchor。
每图渲染全图（scale 自适应，拼接后 ~1000px）再缩到 TARGET 长边，
输出 /tmp/wiki_thumbs/<mapname>.png + manifest.json。

用法:
  python3 thumb_gen.py [--maps DIR] [--data DIR] [--out DIR] [--target 240] [--limit N] [--jobs 4]
"""
import argparse, json, math, os, sys, time
from concurrent.futures import ThreadPoolExecutor, as_completed
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mapviewer import MapCache, FramePool, world_bounds, cell_anchor, MapCell, parse_map
import struct

def parse_map_13(path):
    """EI 旧格式 .map: full-res cell 13 字节（前 9 字节与 14 字节版相同）。"""
    with open(path, "rb") as f:
        data = f.read()
    w = struct.unpack_from("<H", data, 22)[0]
    h = struct.unpack_from("<H", data, 24)[0]
    cells = [[MapCell() for _ in range(h)] for _ in range(w)]
    offset = 28
    for x in range(w // 2):
        for y in range(h // 2):
            bf = data[offset]
            bi = struct.unpack_from("<H", data, offset + 1)[0]
            offset += 3
            cells[x * 2][y * 2].back_file = bf
            cells[x * 2][y * 2].back_img = bi
    for x in range(w):
        for y in range(h):
            ff = data[offset + 3]
            mf = data[offset + 4]
            mi = struct.unpack_from("<H", data, offset + 5)[0]
            fi = struct.unpack_from("<H", data, offset + 7)[0]
            offset += 13
            c = cells[x][y]
            c.mid_file = mf
            c.mid_img = mi
            c.front_file = ff
            c.front_img = fi
    return w, h, cells

class MapCache13(MapCache):
    """14B 解析越界时回退 13B 旧格式。其余（sparse/sparse_slice）继承新版。"""
    def get(self, name):
        with self._lock:
            entry = self._store.get(name)
        if entry is None:
            with self._build_lock(name):
                with self._lock:
                    entry = self._store.get(name)
                if entry is None:
                    path = os.path.join(self.maps_dir, name)
                    try:
                        entry = parse_map(path)
                    except (IndexError, struct.error):
                        entry = parse_map_13(path)
                    with self._lock:
                        self._store[name] = entry
                        while len(self._store) > self.max_keep:
                            k = next(iter(self._store))
                            self._store.pop(k)
                            self._buckets.pop(k, None)
                            self._bxs.pop(k, None)
        return self._store[name]

TARGET = 240          # 缩略图长边像素
DARK = (16, 16, 20, 255)


def render_world(mc, pool, name, scale):
    """整图渲染到 (world/scale) 画布，返回 PIL Image."""
    w, h, _ = mc.get(name)
    world_w, world_h = world_bounds(w, h)
    W, H = math.ceil(world_w / scale), math.ceil(world_h / scale)
    canvas = Image.new("RGBA", (W, H), DARK)

    def blit(img, px, py):
        sx, sy = px // scale, py // scale
        iw, ih = img.width, img.height
        if sy < 0:
            top = min(ih, -sy)
            img = img.crop((0, top, iw, ih)); ih = img.height; sy = 0
        if sx < 0:
            left = min(iw, -sx)
            img = img.crop((left, 0, iw, ih)); iw = img.width; sx = 0
        if sx >= W or sy >= H:
            return
        iw = min(iw, W - sx); ih = min(ih, H - sy)
        if iw <= 0 or ih <= 0:
            return
        if iw < img.width or ih < img.height:
            img = img.crop((0, 0, iw, ih))
        canvas.alpha_composite(img, (sx, sy))

    cells = mc.sparse_slice(name, 0, world_w, 0, world_h)
    for x, y, cell in cells:
        cx, cy = cell_anchor(x, y, h)
        if cell.back_file != 255 and cell.back_img >= 0:
            got = pool.decode(cell.back_file, cell.back_img, scale)
            if got is not None:
                img, off_x, off_y = got
                blit(img, cx - 24 + off_x, cy - 16 + off_y)
        if cell.mid_file != 255 and cell.mid_img > 0:
            got = pool.decode(cell.mid_file, cell.mid_img - 1, scale)
            if got is not None:
                img, off_x, off_y = got
                blit(img, cx + off_x, cy + off_y)
        if cell.front_file != 255 and cell.front_img > 0:
            got = pool.decode(cell.front_file, cell.front_img - 1, scale)
            if got is not None:
                img, off_x, off_y = got
                blit(img, cx + off_x, cy + off_y)
    return canvas


def choose_scale(world_w, world_h):
    """使拼接世界图 ~1000px 长的 scale（2 的幂）。"""
    longest = max(world_w, world_h)
    s = 1
    while longest / s > 1000:
        s *= 2
    return s


def render_one(mc, pool, out_dir, name, w, h):
    out = os.path.join(out_dir, name + ".png")
    if os.path.exists(out):
        return name, True, "cached"
    world_w, world_h = world_bounds(w, h)
    scale = choose_scale(world_w, world_h)
    try:
        img = render_world(mc, pool, name, scale)
    except Exception as e:
        return name, False, f"render error: {e}"
    longest = max(img.width, img.height)
    if longest > TARGET:
        r = TARGET / longest
        img = img.resize((max(1, int(img.width * r)), max(1, int(img.height * r))),
                         Image.LANCZOS)
    img.convert("RGB").save(out, "PNG")
    return name, True, f"{img.width}x{img.height}"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--maps", default="/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Map")
    ap.add_argument("--data", default="/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data")
    ap.add_argument("--out", default="/tmp/wiki_thumbs")
    ap.add_argument("--target", type=int, default=None, help="缩略图长边像素")
    ap.add_argument("--limit", type=int, default=0, help="只渲染前 N 张（测试）")
    ap.add_argument("--jobs", type=int, default=4)
    ap.add_argument("--names", default="", help="逗号分隔，只渲染这些图")
    args = ap.parse_args()
    global TARGET  # noqa: PLW0603
    TARGET = args.target or TARGET

    os.makedirs(args.out, exist_ok=True)
    maps = json.load(open("/tmp/ei_maps.json", encoding="utf-8"))
    if args.names:
        want = set(args.names.split(","))
        maps = [m for m in maps if m["name"] in want]
    if args.limit:
        maps = maps[:args.limit]

    mc = MapCache13(args.maps, max_keep=8)
    pool = FramePool(args.data)

    t0 = time.time()
    done = ok = 0
    manifest = {}
    if args.jobs > 1:
        with ThreadPoolExecutor(args.jobs) as ex:
            futs = {ex.submit(render_one, mc, pool, args.out, m["name"], m["w"], m["h"]): m
                    for m in maps}
            for fut in as_completed(futs):
                name, success, info = fut.result()
                done += 1
                ok += success
                manifest[name] = {"ok": success, "info": info}
                if done % 20 == 0 or not success:
                    print(f"[{done}/{len(maps)}] {name}: {'OK' if success else 'FAIL'} {info}",
                          flush=True)
    else:
        for m in maps:
            name, success, info = render_one(mc, pool, args.out, m["name"], m["w"], m["h"])
            done += 1
            ok += success
            manifest[name] = {"ok": success, "info": info}
            if done % 10 == 0 or not success:
                print(f"[{done}/{len(maps)}] {name}: {'OK' if success else 'FAIL'} {info}",
                      flush=True)

    with open(os.path.join(args.out, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=1)
    print(f"\nDone {ok}/{len(maps)} in {time.time()-t0:.1f}s -> {args.out}")


if __name__ == "__main__":
    main()
