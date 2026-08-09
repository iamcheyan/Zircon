import struct, sys
from PIL import Image
from collections import Counter

# ---------- Zl reader ----------
def read_zl(path):
    with open(path,'rb') as f: d = f.read()
    meta_size = int.from_bytes(d[0:4],'little')
    value = int.from_bytes(d[4:8],'little')
    count = value & 0x1FFFFFF
    ver = (value>>25)&0x7F
    if ver == 0: count = value
    pos = 8
    frames = {}
    for i in range(count):
        present = d[pos]; pos += 1
        if not present: continue
        Position, W, H, OffX, OffY = struct.unpack_from('<ihhhh', d, pos); pos += 12
        ShadowType = d[pos]; pos += 1
        ShW,ShH,ShX,ShY = struct.unpack_from('<hhhh', d, pos); pos += 8
        OvW,OvH = struct.unpack_from('<hh', d, pos); pos += 4
        frames[i] = (Position, W, H, OffX, OffY, 1 if ver==0 else 5)
    return d, frames, ver

def c565(v):
    b = (v & 0x1F) << 3; b |= b >> 5
    g = ((v >> 5) & 0x3F) << 2; g |= g >> 6
    r = ((v >> 11) & 0x1F) << 3; r |= r >> 5
    return r, g, b

def decode_dxt1(data, w, h):
    bw = (w+3)//4; bh = (h+3)//4
    img = Image.new('RGB', (bw*4, bh*4))
    px = img.load()
    for by in range(bh):
        for bx in range(bw):
            off = (by*bw+bx)*8
            c0, c1 = struct.unpack_from('<HH', data, off)
            r0,g0,b0 = c565(c0); r1,g1,b1 = c565(c1)
            if c0 > c1:
                c2 = (r0*2+r1)//3, (g0*2+g1)//3, (b0*2+b1)//3
                c3 = (r0+r1*2)//3, (g0+g1*2)//3, (b0+b1*2)//3
            else:
                c2 = (r0+r1)//2, (g0+g1)//2, (b0+b1)//2
                c3 = None
            idx = data[off+4:off+8]
            for j in range(4):
                for i in range(4):
                    code = (idx[j] >> (2*i)) & 3
                    col = ((r0,g0,b0),(r1,g1,b1),c2,c3)[code] if code < 3 or c3 else (0,0,0)
                    px[bx*4+i, by*4+j] = col
    return img.crop((0,0,w,h))

def decode_dxt5(data, w, h):
    bw = (w+3)//4; bh = (h+3)//4
    img = Image.new('RGB', (bw*4, bh*4))
    px = img.load()
    for by in range(bh):
        for bx in range(bw):
            off = (by*bw+bx)*16
            a0, a1 = data[off], data[off+1]
            alphas = [a0, a1]
            if a0 > a1:
                for i in range(6): alphas.append(((6-i)*a0 + (i+1)*a1)//7)
            else:
                for i in range(4): alphas.append(((4-i)*a0 + (i+1)*a1)//5)
                alphas += [0, 255]
            c0, c1 = struct.unpack_from('<HH', data, off+8)
            r0,g0,b0 = c565(c0); r1,g1,b1 = c565(c1)
            if c0 > c1:
                c2 = (r0*2+r1)//3, (g0*2+g1)//3, (b0*2+b1)//3
                c3 = (r0+r1*2)//3, (g0+g1*2)//3, (b0+b1*2)//3
            else:
                c2 = (r0+r1)//2, (g0+g1)//2, (b0+b1)//2
                c3 = None
            bits = int.from_bytes(data[off+12:off+16], 'little')
            colors = [(r0,g0,b0),(r1,g1,b1),c2,c3]
            for j in range(4):
                for i in range(4):
                    code = (bits >> (3*(j*4+i))) & 7
                    col = colors[min(code,3)] if code < 3 or c3 else (0,0,0)
                    a = alphas[code] if code < len(alphas) else 255
                    if a < 128: col = (0,0,0)
                    px[bx*4+i, by*4+j] = col
    return img.crop((0,0,w,h))

def get_frame(zl_d, frames, idx):
    if idx not in frames: return None
    pos, w, h, ox, oy, codec = frames[idx]
    if w <= 0 or h <= 0: return None
    nblk = ((w+3)//4)*((h+3)//4)
    size = nblk*16 if codec == 5 else nblk*8
    data = zl_d[pos:pos+size]
    if len(data) < size: return None
    img = decode_dxt5(data, w, h) if codec == 5 else decode_dxt1(data, w, h)
    return img, w, h, ox, oy

# ---------- Map reader ----------
def read_map(path):
    with open(path,'rb') as f: d = f.read()
    w = int.from_bytes(d[22:24],'little'); h = int.from_bytes(d[24:26],'little')
    p = 28
    back = {}
    for x in range(w//2):
        for y in range(h//2):
            bf = d[p]; p += 1
            bi = int.from_bytes(d[p:p+2],'little'); p += 2
            back[(x*2,y*2)] = (bf, bi)
    cells = []
    for x in range(w):
        for y in range(h):
            flag = d[p]; maf = d[p+1]; val = d[p+2]; ff = d[p+3]; mf = d[p+4]
            mi = int.from_bytes(d[p+5:p+7],'little') + 1
            fi = int.from_bytes(d[p+7:p+9],'little') + 1
            p += 14
            cells.append((x,y,mf,mi,ff,fi,maf,val))
    return w, h, back, cells

# ---------- stats ----------
def layer_stats(cells, layer):
    c = Counter()
    frames_used = Counter()
    for (x,y,mf,mi,ff,fi,maf,val) in cells:
        if layer == 'M':
            f, i, anim = mf, mi, maf
        else:
            f, i, anim = ff, fi, val
        if i <= 0: continue
        c[f] += 1
        frames_used[(f, i-1)] += 1
    return c, frames_used

# ---------- color analysis ----------
def color_stats(img):
    im = img.convert('RGB')
    small = im.resize((max(1,im.width//4), max(1,im.height//4)))
    px = list(small.getdata())
    n = len(px)
    # red-dominant: r > 120, r > g*1.5, r > b*1.5
    red = sum(1 for (r,g,b) in px if r > 120 and r > g*1.5 and r > b*1.5)
    brown = sum(1 for (r,g,b) in px if 60 < r < 200 and g < r*0.85 and b < g)
    avg = tuple(sum(c[i] for c in px)//n for i in range(3))
    return red, brown, n, avg

def main():
    maps = ['D201','D101','D102','D103']
    zl_dir = 'Debug/Client/Data/Map Data'
    libs = {
        2: ('Tiles5c', f'{zl_dir}/Tiles5c.Zl'),
        17: ('Wood_Tiles5c', f'{zl_dir}/Wood/Tiles5c.Zl'),
        21: ('Wood_Dungeonsc', f'{zl_dir}/Wood/Dungeonsc.Zl'),
        6: ('Dungeonsc', f'{zl_dir}/Dungeonsc.Zl'),
    }
    cache = {}
    def getlib(key):
        if key not in cache:
            cache[key] = read_zl(key)
        return cache[key]
    for m in maps:
        w, h, back, cells = read_map(f'Debug/Client/Map/{m}.map')
        print(f'\n===== {m} {w}x{h} =====')
        for layer in ('M','F'):
            c, fu = layer_stats(cells, layer)
            tot = sum(c.values())
            print(f'  {layer}层: 总瓦片={tot} file分布={dict(c.most_common(6))}')
            # 每 file 的 top 帧
            perfile = {}
            for (f,i), n in fu.items():
                perfile.setdefault(f, []).append((i, n))
            for f in sorted(perfile):
                name, path = libs.get(f, (str(f), None))
                print(f'    file={f} ({name}): 帧 {sorted(perfile[f], key=lambda t:-t[1])[:6]}')
        # 背景层统计
        bc = Counter(v[0] for v in back.values())
        print(f'  Back层 file分布={dict(bc.most_common(6))}')

if __name__ == '__main__':
    main()

def decode_dxt5_fixed(data, w, h):
    """BC3: alpha 3-bit + BC1 色块(2-bit 索引)"""
    bw = (w+3)//4; bh = (h+3)//4
    img = Image.new('RGB', (bw*4, bh*4))
    px = img.load()
    for by in range(bh):
        for bx in range(bw):
            off = (by*bw+bx)*16
            a0, a1 = data[off], data[off+1]
            alphas = [a0, a1]
            if a0 > a1:
                for i in range(6): alphas.append(((6-i)*a0 + (i+1)*a1)//7)
            else:
                for i in range(4): alphas.append(((4-i)*a0 + (i+1)*a1)//5)
                alphas += [0, 255]
            c0, c1 = struct.unpack_from('<HH', data, off+8)
            r0,g0,b0 = c565(c0); r1,g1,b1 = c565(c1)
            if c0 > c1:
                c2 = (r0*2+r1)//3, (g0*2+g1)//3, (b0*2+b1)//3
                c3 = (r0+r1*2)//3, (g0+g1*2)//3, (b0+b1*2)//3
            else:
                c2 = (r0+r1)//2, (g0+g1)//2, (b0+b1)//2
                c3 = None
            abits = int.from_bytes(data[off+2:off+8], 'little')
            cbits = int.from_bytes(data[off+12:off+16], 'little')
            for j in range(4):
                for i in range(4):
                    p = j*4+i
                    acode = (abits >> (3*p)) & 7
                    ccode = (cbits >> (2*p)) & 3
                    col = ((r0,g0,b0),(r1,g1,b1),c2,c3)[ccode] if ccode < 3 or c3 else (0,0,0)
                    if alphas[acode] < 128: col = (0,0,0)
                    px[bx*4+i, by*4+j] = col
    return img.crop((0,0,w,h))
