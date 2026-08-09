import json, struct, os, sys, numpy as np
import multiprocessing as mp
try:
    mp.set_start_method("fork")
except RuntimeError:
    pass
from collections import defaultdict
from multiprocessing import Pool
from PIL import Image

DATA = '/home/tetsuya/NAS/TMP/mir3ei/Data'
MAPDIR = '/home/tetsuya/NAS/TMP/mir3ei/Map'
# 输出目录:优先使用项目内 docs/research/mir3ei-map-catalog/views(脚本上级的 views)
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'views')
# 指纹数据:项目内 data/mir3ei_fp.json(可由 tools/mir3ei_fp_build.py 重建)
FP = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'data', 'mir3ei_fp.json')
SCALE = 8
TW, TH = 96//SCALE, 64//SCALE

KRO = {0:'Tilesc',1:'Tiles30c',2:'Tiles5c',3:'SmTilesc',4:'Housesc',5:'Cliffsc',
       6:'Dungeonsc',7:'Innersc',8:'Furnituresc',9:'Wallsc',10:'SmObjectsc',
       11:'Animationsc',12:'Object1c',13:'Object2c'}
SUBDIRS = {15:'Wood/Tilesc',16:'Wood/T30c',17:'Wood/T5c',18:'Wood/SmTilesc',19:'Wood/Housesc',
    20:'Wood/Cliffsc',21:'Wood/Dungeonsc',22:'Wood/Innersc',23:'Wood/Furnituresc',
    24:'Wood/Wallsc',25:'Wood/SmObjectsc',26:'Wood/Animationsc',
    30:'Sand/Tilesc',31:'Sand/T30c',32:'Sand/T5c',33:'Sand/SmTilesc',34:'Sand/Housesc',
    35:'Sand/Cliffsc',36:'Sand/Dungeonsc',37:'Sand/Innersc',38:'Sand/Furnituresc',
    39:'Sand/Wallsc',40:'Sand/SmObjectsc',41:'Sand/Animationsc',
    45:'Snow/Tiles',46:'Snow/Tiles30',47:'Snow/SmTiles',48:'Snow/Cliffs',49:'Snow/Dungeons',
    50:'Snow/Houses',51:'Snow/furnitures',52:'Snow/Walls',53:'Snow/SmObjects',54:'Snow/Animations',
    55:'Snow/Objs',56:'Snow/Objs2',
    60:'Forest/Tilesc',61:'Forest/T30c',62:'Forest/T5c',63:'Forest/SmTilesc',64:'Forest/Housesc',
    65:'Forest/Cliffsc',66:'Forest/Dungeonsc',67:'Forest/Innersc',68:'Forest/Furnituresc',
    69:'Forest/Wallsc',70:'Forest/SmObjectsc',71:'Forest/Animationsc'}
def krname(f):
    if f in KRO: return KRO[f]
    return SUBDIRS.get(f)
REAL = {'Wood/T5c':'Wood/Tiles5c', 'Forest/T5c':'Forest/tiles5c'}
def realname(name): return REAL.get(name, name)

class Wil:
    def __init__(self, wil_path):
        self.d = open(wil_path,'rb').read()
        w = open(wil_path[:-4]+'.wix','rb').read()
        self.n = struct.unpack_from('<I', w, 20)[0]
        self.offs = [struct.unpack_from('<I', w, 24+4*i)[0] for i in range(self.n)]
        self.offs.append(len(self.d))
        # 有效帧 = 起始偏移在文件内(别名重映射索引: 后续偏移可能为0)
        self.valid = set()
        for i in range(self.n):
            if 28 <= self.offs[i] < len(self.d):
                self.valid.add(i)
    def extent(self, i):
        nxt = self.offs[i+1]
        if nxt > self.offs[i]:
            return min(nxt, len(self.d))
        return min(self.offs[i] + 12689, len(self.d))
    def frame_down(self, i):
        o = self.offs[i]; buf = self.d
        w, h = struct.unpack_from('<HH', buf, o)
        start = o + 17; end = self.extent(i)
        rgb = np.zeros((h, w, 3), np.uint8)
        alpha = np.zeros((h, w), np.uint8)
        off = start
        for r in range(h):
            cnt = struct.unpack_from('<H', buf, off)[0]; off += 2
            rend = off + cnt*2; col = 0
            while off < rend:
                m = struct.unpack_from('<H', buf, off)[0]
                rl = struct.unpack_from('<H', buf, off+2)[0]
                if m == 0xC0:
                    off += 4; col += rl
                else:
                    px = np.frombuffer(buf[off+4:off+4+rl*2], np.uint16)
                    R = ((px>>11)&31)<<3; G = ((px>>5)&63)<<2; B = (px&31)<<3
                    rgb[r, col:col+rl] = np.stack([R,G,B],-1)
                    alpha[r, col:col+rl] = 255
                    off += 4 + rl*2; col += rl
            if off != rend: raise ValueError('misalign')
        ph = (h + SCALE-1)//SCALE*SCALE; pw = (w + SCALE-1)//SCALE*SCALE
        if ph != h or pw != w:
            nr = np.zeros((ph, pw, 3), np.uint8); na = np.zeros((ph, pw), np.uint8)
            nr[:h,:w] = rgb; na[:h,:w] = alpha
            rgb, alpha = nr, na
        sh = ph//SCALE; sw = pw//SCALE
        rgb = rgb.reshape(sh, SCALE, sw, SCALE, 3).mean(axis=(1,3)).astype(np.uint8)
        a = alpha.reshape(sh, SCALE, sw, SCALE).any(axis=(1,3)).astype(np.uint8)
        return rgb, a

def read_back_positions(path):
    data = open(path,'rb').read()
    w, h = struct.unpack_from('<HH', data, 22)
    nback = (w//2)*(h//2)
    base = 28 + nback*3
    ncell = w*h
    if base + ncell*14 == len(data): mode = 14
    elif base + ncell*13 == len(data): mode = 13
    else: raise ValueError(path)
    b = np.frombuffer(data, np.uint8, nback*3, 28)
    bf = b[0::3].astype(np.int32)
    bi = (b[1::3].astype(np.int32) | (b[2::3].astype(np.int32)<<8))
    return w, h, bf.reshape(h//2, w//2), bi.reshape(h//2, w//2)

def decode_one(libs, args):
    name, f = args
    return libs[name].frame_down(f)

def render_map(args):
    name, path = args
    try:
        w, h, bf, bi = read_back_positions(path)
    except Exception as e:
        return name, f'ERR {e}'
    gw, gh = bf.shape[1], bf.shape[0]
    canvas = np.full((gh*TH, gw*TW, 3), 40, np.uint8)
    idx = IDX
    n_lib = N_LIB
    for y in range(gh):
        y0 = y*TH
        for x in range(gw):
            f = bf[y, x]
            if f == 255: continue
            fr = int(bi[y, x])
            lib = krname(f)
            if lib is None: continue
            key = f'{lib}:{fr}'
            i = idx.get(key)
            if i is None: continue
            t = TILES[i]
            if MASK[i].all():
                canvas[y0:y0+TH, x*TW:x*TW+TW] = t
            else:
                sl = canvas[y0:y0+TH, x*TW:x*TW+TW]
                m = MASK[i][..., None] > 0
                canvas[y0:y0+TH, x*TW:x*TW+TW] = np.where(m, t, sl)
    img = Image.fromarray(canvas)
    img.save(os.path.join(OUT, name + '.png'))
    return name, 'ok'

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    mfp = json.load(open(FP))
    back_need = defaultdict(set)
    skip = defaultdict(int)
    for n,(w,h,mode,back,mid,fr) in mfp.items():
        for k,c in back.items():
            a,b = k.split(':')
            f = int(a)
            if f == 255: continue
            name = krname(f)
            back_need[name].add(int(b))
            skip[(name, int(b))] += 1
    libs = {}
    for name in back_need:
        p = os.path.join(DATA, realname(name)+'.wil')
        libs[name] = Wil(p)
    valid_need = defaultdict(set)
    for (name, f), cnt in skip.items():
        if f < libs[name].n and f in libs[name].valid: valid_need[name].add(f)
    rows = []
    for name in sorted(valid_need):
        for f in sorted(valid_need[name]):
            rows.append((name, f))
    print('unique frames:', len(rows))
    with Pool(8) as p:
        import functools
        res = p.map(functools.partial(decode_one, libs), rows)
    N = len(rows)
    TILES = np.zeros((N, TH, TW, 3), np.uint8)
    MASK = np.zeros((N, TH, TW), np.uint8)
    for i,(rgb,a) in enumerate(res):
        TILES[i] = rgb; MASK[i] = a
    IDX = {f'{name}:{f}': i for i,(name,f) in enumerate(rows)}
    N_LIB = len(libs)
    # 渲染
    names = sorted(fn[:-4] for fn in os.listdir(MAPDIR) if fn.endswith('.map'))
    tasks = [(n, os.path.join(MAPDIR, n+'.map')) for n in names]
    with Pool(8) as p:
        out = p.map(render_map, tasks)
    bad = [o for o in out if o[1] != 'ok']
    print('done', len(out), 'errors:', bad[:5] if bad else 'none')
