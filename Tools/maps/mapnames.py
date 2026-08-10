"""Zircon / Mir3 EI 地图中文名表。

数据来源:
- 244 张数据库地图: docs/notes/22-传奇EI2.0资料整理-地图装备技能阶段.md §3.2
  (中文家族名) + docs/legacy-atlas/content/catalog-maps.html 的 Zircon 表
  (逐张英文名, 由 Tools/SystemDbProbe 生成)。
- 12 张不在数据库中的客户端遗留文件 (00, 11_001, D012_1..6, D1506,
  D29031/32, GM_001, Ithuejingot, Ithuejingot_WaitR): 按旧版地图体系推断,
  标注 "(未收录)" 或 "(遗留)"。

文件表 (Debug/Client/Map, 258 张) = 数据库 244 张 + 遗留 14 张。
"""

# ---------------------------------------------------------------------------
# 显式条目: 城镇 / 建筑 / 地表野图 / 特殊。优先于规则。
# ---------------------------------------------------------------------------
EXPLICIT = {
    # 城镇与地表 (18)
    "0": "比奇城", "1": "失乐园", "2": "潘夜村", "3": "沙巴克城",
    "4": "诺马村", "5": "沙漠土城", "7": "炼狱岛", "8": "冰雪村",
    "10": "比奇城堡", "11": "道馆", "12": "潘夜岛", "13": "潘约岛",
    "14": "毒港", "15": "15(未命名)", "16": "西部荒野(西沙)", "17": "失落绿洲",
    "18": "干涸平原", "19": "失落村庄",
    # 城内建筑
    "0_000": "市政厅", "0_001": "左翼", "0_002": "右翼",
    "3_000": "沙巴克领地", "5_000": "沙漠领地",
    "8_001": "圣殿入口(旧)", "8_002": "圣殿入口",
    "11_002": "武器店", "11_003": "药水商", "11_004": "铠甲店",
    "11_005": "杂货商", "14_000": "刺客藏身处",
    # 地表野图
    "D001": "幽灵森林", "D002": "沙漠", "D003": "失乐园森林",
    "D004": "潘夜谷南", "D005": "故乡兵营", "D006": "熔岩地带 1",
    "D007": "熔岩地带 2", "D008": "龙渊入口", "D009": "毒地",
    "E01": "北路", "E02": "北路", "E11": "南路", "E12": "南路",
    "D4000": "南部沙丘", "D4001": "南部荒原", "D4002": "南部海岸",
    "D4003": "南部哨卡", "D4101": "南墙", "D4102": "迷失之路",
    "D3400": "失落之地", "D3400_1": "失落之地 2", "ER51_Ice": "失落之地 3",
    "GM": "GM 图",
    "16_001": "彼岸", "16_002": "西海岸", "16_003": "西关",
    "19_1": "失落关口",
    "ID3_014": "The Wall(长城)", "ID3_024": "The Wall 2(长城 2)",
    # 招牌 / 特殊
    "D1206": "真天宫大殿", "D1401": "幽灵船入口", "D1406": "幽灵船甲板",
    "D1501": "诺马遗迹 1 层", "D1502": "诺马遗迹 2 层",
    "D1601": "诺马丘陵", "D1602": "诺马谷", "D1603": "诺马谷",
    "D1604": "诺马要塞", "D1802": "炼狱", "D2004": "沙漠王殿",
    "D2900": "巢穴入口", "D2907": "巢穴",
    "D3004": "龙渊 3 层", "D3006": "龙渊大殿",
    "D3005_BH": "龙渊 4 层(变体)", "D3005_CR": "龙渊 4 层(变体)",
    "D3005_HM": "龙渊 4 层(变体)", "D3005_JJ": "龙渊 4 层(变体)",
    "ID9_00": "废弃小镇", "ID9_01": "遗忘修道院 1 层", "ID9_02": "遗忘修道院 2 层",
    # 同名变体区分 (英文表同名, 实为多张图)
    "D2205": "冰封女王殿",
    "D15031": "诺马遗迹 3 层(1)", "D15032": "诺马遗迹 3 层(2)",
    "D15033": "诺马遗迹 3 层(3)", "D15034": "诺马遗迹 3 层(4)",
    "D21051": "冰封地牢 5 层(1)", "D21052": "冰封地牢 5 层(2)",
    "D21053": "冰封地牢 5 层(3)", "D21054": "冰封地牢 5 层(4)",
    "D21055": "冰封地牢 5 层(5)", "D21056": "冰封地牢 5 层(6)",
    # 预留 / 未命名
    "15_001": "15_001(未命名)", "15_002": "15_002(未命名)", "15_003": "15_003(未命名)",
    "D3101": "D3101(预留新图)", "D3102": "D3102(预留新图)",
    "D3103": "D3103(预留新图)", "D3106": "D3106(预留新图)",
    "D3901": "D3901(预留新图)", "D3902": "D3902(预留新图)",
    "D3903": "D3903(预留新图)", "D3904": "D3904(预留新图)",
    "D3905": "D3905(预留新图)", "D3906": "D3906(预留新图)",
    # 不在数据库的客户端遗留文件 (14 张)
    "00": "00(未收录大地图)",
    "11_001": "11_001(未收录)",
    "D012_1": "天然洞穴 1(遗留)", "D012_2": "天然洞穴 2(遗留)",
    "D012_3": "天然洞穴 3(遗留)", "D012_4": "天然洞穴 4(遗留)",
    "D012_5": "天然洞穴 5(遗留)", "D012_6": "天然洞穴 6(遗留)",
    "D1506": "D1506(未收录)",
    "D29031": "巢穴 3 层西(推断)", "D29032": "巢穴 3 层东(推断)",
    "GM_001": "GM 活动图",
    "Ithuejingot": "活动房(2003 遗留)",
    "Ithuejingot_WaitR": "活动房等待室(2003 遗留)",
}

# ---------------------------------------------------------------------------
# 家族规则: (英文名正则, 中文模板)。按顺序匹配, 方向 W/E/S/N → 西/东/南/北。
# ---------------------------------------------------------------------------
_DIR = {"W": "西", "E": "东", "S": "南", "N": "北"}
_FAMILIES = [
    (r"^Banya Temple Lv (\d+)-([WENS])$", lambda m: f"潘夜神殿 {m[0]} 层{_DIR[m[1]]}"),
    (r"^Banya Temple Lv 9 (West|East)$", lambda m: f"潘夜神殿 9 层{'西' if m[0]=='West' else '东'}"),
    (r"^Banya Temple Lv (\d+)$", lambda m: f"潘夜神殿 {m[0]} 层"),
    (r"^Banya Temple Hall$", lambda _: "潘夜神殿大殿"),
    (r"^Banya Temple$", lambda _: "潘夜神殿"),
    (r"^Zuma Temple Lv (\d+)$", lambda m: f"祖玛神殿 {m[0]} 层"),
    (r"^Zuma Temple$", lambda _: "祖玛神殿"),
    (r"^Jinchon Palace Lv (\d+)-([WENS])$", lambda m: f"真天宫 {m[0]} 层{_DIR[m[1]]}"),
    (r"^Jinchon Palace Lv (\d+)$", lambda m: f"真天宫 {m[0]} 层"),
    (r"^Jinchon Palace$", lambda _: "真天宫大殿"),
    (r"^Black Palace Lv (\d+)-([WENS])$", lambda m: f"黑宫 {m[0]} 层{_DIR[m[1]]}"),
    (r"^Black Palace Lv (\d+)$", lambda m: f"黑宫 {m[0]} 层"),
    (r"^Black Palace$", lambda _: "黑宫大殿"),
    (r"^Phantom Ship Ent$", lambda _: "幽灵船入口"),
    (r"^Phantom Ship Lv (\d+)$", lambda m: f"幽灵船 {m[0]} 层"),
    (r"^Flight Deck$", lambda _: "幽灵船甲板"),
    (r"^Numa Ruins Lv (\d+)$", lambda m: f"诺马遗迹 {m[0]} 层"),
    (r"^Numa Hill$", lambda _: "诺马丘陵"),
    (r"^Numa Valley$", lambda _: "诺马谷"),
    (r"^Numa Stronghold$", lambda _: "诺马要塞"),
    (r"^Frost Dungeon Lv (\d+)$", lambda m: f"冰封地牢 {m[0]} 层"),
    (r"^Frost Dungeon$", lambda _: "冰封地牢大殿"),
    (r"^Frost Holy Palace Lv (\d+)$", lambda m: f"冰封圣殿 {m[0]} 层"),
    (r"^Frost Holy Palace Old$", lambda _: "冰封旧殿"),
    (r"^Queen's Chamber$", lambda _: "冰封女王殿"),
    (r"^Goru Cave Lv (\d+)$", lambda m: f"高丽洞 {m[0]} 层"),
    (r"^Hyunmoon Temple Lv (\d+)$", lambda m: f"玄月殿 {m[0]} 层"),
    (r"^Hyunmoon Temple$", lambda _: "玄月殿"),
    (r"^Departed Valley Lv (\d+)$", lambda m: f"幽谷 {m[0]} 层"),
    (r"^The Lair Lv (\d+) (West|East)$", lambda m: f"巢穴 {m[0]} 层{'西' if m[1]=='West' else '东'}"),
    (r"^The Lair Lv (\d+)$", lambda m: f"巢穴 {m[0]} 层"),
    (r"^The Lair Entrance$", lambda _: "巢穴入口"),
    (r"^The Lair$", lambda _: "巢穴"),
    (r"^Dragon Abyss Lv (\d+) - OLD$", lambda m: f"龙渊 {m[0]} 层(旧)"),
    (r"^Dragon Abyss Lv (\d+)$", lambda m: f"龙渊 {m[0]} 层"),
    (r"^Dragon Abyss Ent$", lambda _: "龙渊入口"),
    (r"^Dragon Abyss$", lambda _: "龙渊大殿"),
    (r"^Desert Dungeon Lv (\d+)$", lambda m: f"沙漠地牢 {m[0]} 层"),
    (r"^Underground City Lv (\d+)$", lambda m: f"地下城 {m[0]} 层"),
    (r"^Underground Mine Lv (\d+)$", lambda m: f"地下矿 {m[0]} 层"),
    (r"^Bichon Cave Lv (\d+)$", lambda m: f"比奇矿 {m[0]} 层"),
    (r"^Deserted Mine Lv (\d+)$", lambda m: f"废矿 {m[0]} 层"),
    (r"^Quartz Mine Lv (\d+)$", lambda m: f"石英矿 {m[0]} 层"),
    (r"^Quartz Mine$", lambda _: "石英矿殿"),
    (r"^Banya Cave Lv (\d+)$", lambda m: f"潘夜洞 {m[0]} 层"),
    (r"^Banya Stone Cave Lv (\d+)$", lambda m: f"潘夜石窟 {m[0]} 层"),
    (r"^Banyo Cave$", lambda _: "潘夜洞"),
    (r"^Lost Paradise Cave Lv (\d+)$", lambda m: f"失乐园洞 {m[0]} 层"),
    (r"^Flea Cave Lv (\d+)$", lambda m: f"跳蚤洞 {m[0]} 层"),
    (r"^Ant Cave (North|South|West|East)$", lambda m: f"蚂蚁洞{_DIR[m[0][0]]}"),
    (r"^Uma Temple Lv (\d+)$", lambda m: f"乌玛殿 {m[0]} 层"),
    (r"^Uma Temple$", lambda _: "乌玛殿"),
    (r"^Carved Stone Tomb Lv (\d+)$", lambda m: f"石刻墓碑 {m[0]} 层"),
    (r"^Carved Stone Tomb$", lambda _: "石刻墓碑殿"),
    (r"^Despair Valley Lv (\d+)$", lambda m: f"绝望谷 {m[0]} 层"),
    (r"^Life Death Hall$", lambda _: "生死殿"),
    (r"^Red Moon Valley Lv (\d+)$", lambda m: f"赤月谷 {m[0]} 层"),
    (r"^Red Moon Valley$", lambda _: "赤月谷"),
    (r"^Lost Land 2$", lambda _: "失落之地 2"),
    (r"^Lost Land 3$", lambda _: "失落之地 3"),
    (r"^Lost Land$", lambda _: "失落之地"),
    (r"^Abandoned Town$", lambda _: "废弃小镇"),
    (r"^Forgot+en Monastery Lv (\d+)$", lambda m: f"遗忘修道院 {m[0]} 层"),
    (r"^Lava Area Lv (\d+)$", lambda m: f"熔岩地带 {m[0]}"),
    (r"^Phantom Forest$", lambda _: "幽灵森林"),
    (r"^Lost Paradise Forest$", lambda _: "失乐园森林"),
    (r"^Banya Valley South$", lambda _: "潘夜谷南"),
    (r"^Homeland TroopBase$", lambda _: "故乡兵营"),
    (r"^Toxic Lands$", lambda _: "毒地"),
    (r"^North Way$", lambda _: "北路"),
    (r"^South Way$", lambda _: "南路"),
    (r"^Southern Dunes$", lambda _: "南部沙丘"),
    (r"^Southern Wastes$", lambda _: "南部荒原"),
    (r"^Southern Coast$", lambda _: "南部海岸"),
    (r"^Southern Check Point$", lambda _: "南部哨卡"),
    (r"^Southern Wall$", lambda _: "南墙"),
    (r"^Lost Way$", lambda _: "迷失之路"),
    (r"^Purgatory$", lambda _: "炼狱"),
    (r"^Western Arids$", lambda _: "西部荒野(西沙)"),
    (r"^Lost Oasis$", lambda _: "失落绿洲"),
    (r"^Arid Flats$", lambda _: "干涸平原"),
    (r"^Lost Village$", lambda _: "失落村庄"),
    (r"^GM Map$", lambda _: "GM 图"),
    (r"^Western Coast$", lambda _: "西海岸"),
    (r"^Western Pass$", lambda _: "西关"),
    (r"^Town Hall$", lambda _: "市政厅"),
    (r"^Left Wing$", lambda _: "左翼"),
    (r"^Right Wing$", lambda _: "右翼"),
    (r"^Sabuk Guild Territory$", lambda _: "沙巴克领地"),
    (r"^Desert Guild Territory$", lambda _: "沙漠领地"),
    (r"^Holy Palace Ent old$", lambda _: "圣殿入口(旧)"),
    (r"^Holy Palace Ent$", lambda _: "圣殿入口"),
    (r"^Weapon Shop$", lambda _: "武器店"),
    (r"^Potion Merchant$", lambda _: "药水商"),
    (r"^Armor Shop$", lambda _: "铠甲店"),
    (r"^Misc Item Vendor$", lambda _: "杂货商"),
    (r"^Assassin's Hideout$", lambda _: "刺客藏身处"),
]

import re as _re


def resolve(file_name: str, english: str = "") -> str:
    """返回 map 文件的中文名; 无匹配时回退英文 (或文件 ID)。"""
    cn = EXPLICIT.get(file_name)
    if cn:
        return cn
    if english:
        for pat, fn in _FAMILIES:
            m = _re.match(pat, english)
            if m:
                return fn(m.groups())
    return english or file_name
