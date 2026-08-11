#!/usr/bin/env python3
"""从原始 EI 配置生成 import-ei 计划（import_plan_v2.json）。

输入（权威数据源）：
  - ei_server_data.json    英雄杀服务端 Envir 解析（745 刷怪 / 89 NPC / 32 守卫）
  - ei_server_mapinfo.json 英雄杀 MapInfo.txt 解析（124 图 + 199 传送点）
  - ei_config_data.json    EI 全量配置（minimaps）
  - monster_name_map.json  EI 中文怪名 -> Zircon 英文名
  - new_monster_images.json 本任务新怪清单（125 条，含 English 名）
  - monsters_import.json   新怪导入清单（Image 枚举映射）
  - db_dump_*.json         Zircon DB dump（用于怪物名/地图集合判定）

输出：import_plan_v2.json（DbMigrationTool import-ei 的输入）。

要点：
  - 全部 124 张 EI 图（83 张 EI 独有 + 41 张交集）都导入 EI 数据；交集图保留
    已有 Zircon 数据，叠加 EI 数据。
  - 地图文件同时查 EI 客户端 Map/ 与英雄杀服务端 Mud3/Map/（大小写不敏感）。
  - 怪物名解析：monster_name_map（已存在怪）+ 新怪 English 名。
  - 坐标越界/无 .map 的行跳过并计数。
"""
import json
import struct
import os
from collections import Counter

INV = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(INV, 'import_plan_v2.json')

EI_CLIENT_MAP = '/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Map'
HERO_SERVER_MAP = '/home/tetsuya/NAS/TMP/EI3.0英雄杀服务端/Mud3/Map'
MAP_DIRS = [EI_CLIENT_MAP, HERO_SERVER_MAP]


def main():
    sd = json.load(open(os.path.join(INV, 'ei_server_data.json')))
    mi = json.load(open(os.path.join(INV, 'ei_server_mapinfo.json')))
    cfg = json.load(open(os.path.join(INV, 'ei_config_data.json')))
    mmap = json.load(open(os.path.join(INV, 'monster_name_map.json')))
    final = json.load(open(os.path.join(INV, 'new_monster_images.json')))
    mimp = json.load(open(os.path.join(INV, 'monsters_import.json')))
    db = json.load(open(os.path.join(INV, 'db_dump_after.json')))

    # 地图文件索引（大小写不敏感）
    index = {}
    for d in MAP_DIRS:
        if not os.path.isdir(d):
            continue
        for f in os.listdir(d):
            if f.lower().endswith('.map'):
                index.setdefault(f[:-4].lower(), (d, f[:-4]))

    def dims(path):
        raw = open(path, 'rb').read(26)
        return struct.unpack('<hh', raw[22:26])

    def convert_minimap(v):
        v = int(v)
        if 1001 <= v <= 1031:
            return v - 1000
        if 1 <= v <= 255:
            return v + 31
        return 0

    maps = []
    for name, info in mi['maps'].items():
        hit = index.get(name.lower())
        if not hit:
            continue
        d, actual = hit
        w, h = dims(os.path.join(d, actual + '.map'))
        ev = cfg['minimaps'].get(name) or cfg['minimaps'].get(name.lower())
        maps.append({'fileName': actual, 'description': info.get('name') or f'EI {actual}',
                     'width': w, 'height': h,
                     'miniMap': convert_minimap(ev) if ev else 0, 'eiMiniMap': int(ev) if ev else 0})
    map_dims = {m['fileName'].lower(): (m['width'], m['height']) for m in maps}

    dblow = {r['MonsterName'].lower() for r in db['MonsterInfo']['rows']}
    en_by_ei = {f['ei_name']: None for f in final}
    for m in mimp:
        for f in final:
            if f['image'] == m['image'] and en_by_ei.get(f['ei_name']) is None:
                en_by_ei[f['ei_name']] = m['name']

    def resolve(ei_name):
        v = mmap.get(ei_name)
        if v and v.lower() in dblow:
            return v
        if en_by_ei.get(ei_name):
            return en_by_ei[ei_name]
        return None

    npcs, guards, safezones, respawns, movements = [], [], [], [], []
    skip = Counter()

    for n in sd['npcs']:
        if n['map'].lower() not in map_dims:
            skip['npc_nomap'] += 1
            continue
        w, h = map_dims[n['map'].lower()]
        x, y = int(n['x']), int(n['y'])
        if x >= w or y >= h:
            skip['npc_oob'] += 1
            continue
        parts = n['script'].split()
        img = int(parts[-1]) if parts and parts[-1].isdigit() else 0
        npcs.append({'map': n['map'], 'x': x, 'y': y, 'name': n['name'], 'image': img, 'desc': n['name']})

    for g in sd['guards']:
        if g['map'].lower() not in map_dims:
            continue
        w, h = map_dims[g['map'].lower()]
        if g['x'] >= w or g['y'] >= h:
            continue
        guards.append({'map': g['map'], 'x': g['x'], 'y': g['y'], 'dir': g['dir'], 'monster': 'Guard'})

    # 安全区：MapInfo SAFE 标记的 1_004（超级泡点）/ z014（监狱），取中心点
    for sz_map in ('1_004', 'z014'):
        k = sz_map.lower()
        if k in map_dims:
            w, h = map_dims[k]
            safezones.append({'map': sz_map, 'x': w // 2, 'y': h // 2})

    for r in sd['respawns']:
        if r['map'].lower() not in map_dims:
            skip['respawn_nomap'] += 1
            continue
        en = resolve(r['monster'])
        if not en:
            skip['respawn_monster'] += 1
            continue
        w, h = map_dims[r['map'].lower()]
        if r['x'] >= w or r['y'] >= h:
            skip['respawn_oob'] += 1
            continue
        respawns.append({'map': r['map'], 'x': r['x'], 'y': r['y'], 'monster': en, 'count': int(r['count'])})

    for t in mi['teleports']:
        s, d2 = t['src_map'].lower(), t['dst_map'].lower()
        if s not in map_dims or d2 not in map_dims:
            skip['mov_nomap'] += 1
            continue
        ws, hs = map_dims[s]
        wd, hd = map_dims[d2]
        if t['src_x'] >= ws or t['src_y'] >= hs or t['dst_x'] >= wd or t['dst_y'] >= hd:
            skip['mov_oob'] += 1
            continue
        movements.append({'srcMap': t['src_map'], 'srcX': t['src_x'], 'srcY': t['src_y'],
                          'dstMap': t['dst_map'], 'dstX': t['dst_x'], 'dstY': t['dst_y']})

    plan = {
        'maps': maps, 'npcs': npcs, 'guards': guards, 'safezones': safezones,
        'respawns': respawns, 'movements': movements,
        'skips': dict(skip),
        'notes': {
            'scope': 'all 124 EI maps (83 EI-only + 41 intersection); intersection keep existing + add EI',
            'miniMapConversion': '1001-1031 -> v-1000, 1-255 -> v+31',
            'safezones': '1_004 / z014 center (MapInfo SAFE flags)',
            'npcImage': 'merchant script last token (hero-era NPC.Zl indices)',
        },
    }
    json.dump(plan, open(OUT, 'w'), ensure_ascii=False, indent=1)
    print(f'maps={len(maps)} npcs={len(npcs)} guards={len(guards)} safezones={len(safezones)} '
          f'respawns={len(respawns)} movements={len(movements)} skips={dict(skip)}')


if __name__ == '__main__':
    main()
