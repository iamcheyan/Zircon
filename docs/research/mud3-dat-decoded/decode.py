#!/usr/bin/env python3
"""Mud3 (Mir3 / 传奇3 EI) Envir DAT 解码器.

数据来源: /home/tetsuya/NAS/TMP/Mud3/Envir/{stditem,magic,monster}.dat
格式结论（全部经穷举验证）:
  - stditem.dat : 4B 明文 UINT32 LE 记录数 + N×184B，记录区 XOR 0x04，N=1143
  - magic.dat   : 4B 明文 UINT32 LE 记录数 + N×120B，记录区 XOR 0x11，N=105
  - monster.dat : 无独立头部，纯 N×252B 记录区 XOR 0x09，N=433；
                  记录 0 = 头部占位（raw 首 u32=433 明文 + 0x09 填充）

字段语义状态: verified / high / medium / low / unverified（见各 FIELDS 表）。
禁止用 Zircon 数据反推老版字段；未确认字段一律 unverified。

用法:
  python3 decode.py            # 生成 stditem.json / magic.json / monster.json
  python3 decode.py stditem    # 只生成单个
"""
from __future__ import annotations

import json
import struct
import sys
from pathlib import Path

ENVIR = Path("/home/tetsuya/NAS/TMP/Mud3/Envir")
OUT = Path(__file__).resolve().parent


def u32(b: bytes, o: int) -> int:
    return struct.unpack_from("<I", b, o)[0]


def gbk_name(b: bytes, len_off: int, data_off: int) -> str:
    ln = b[len_off]
    if ln == 0:
        return ""
    return b[data_off : data_off + ln].decode("gbk", "replace")


# ---------------------------------------------------------------- stditem ---
STDITEM_FIELDS = [
    # (offset, name, type, status, note)
    (0, "Index", "u32", "verified", "0-based 记录号（文件头=记录数 1143）"),
    (36, "StdMode", "u32", "verified", "物品类型：0药品/5武器/10-11衣服/20首饰/22戒指/24手镯/25药粉材料/26手镯/30杂物/31药包/40材料/41货币(金币)/51技能书/58任务/99特殊。锚点：金币=41、金创药(小)=0、布衣=10、木剑=5、火球术(书)=51"),
    (40, "Shape", "u32", "high", "形状/子类。衣服：布衣1/轻型盔甲2/重盔甲3/魔法长袍4/灵魂战衣5；剑类：木剑9/铁剑23/凝霜27/裁决15/屠龙4。隐身戒指=111"),
    (44, "Weight", "u32", "high", "重量。药水1/肉3/布衣5/铁剑10/重盔甲23/凝霜33；价 5/10/23 等与物品重感一致"),
    (48, "AniCount", "u32", "unverified", "32 条非零(0-12)，多为特殊外观/任务品"),
    (52, "NeedIdentify", "u32", "unverified", "9 条非零(0-27)"),
    (56, "Special", "u32", "unverified", "1 条非零(=1)"),
    (60, "Special2", "u32", "unverified", "26 条非零，top=8/23/111(隐身戒指=111)"),
    (64, "SubType", "u32", "unverified", "288 条非零，药品=3(金创药/魔法药)，top 1×140"),
    (68, "Looks", "u32", "verified", "外观图 ID：金创药5/魔法药15/肉300/蜡烛290/技能书304/布衣940/木剑1042/铁剑1043/凝霜1044/屠龙1044区"),
    (72, "DuraMax", "u32", "medium", "持久上限：木剑4000/布衣5000/铁剑10000/凝霜20000/重盔甲25000；消耗品=1(药水/金币)、书=7、肉=10000。装备值域与 EI 持久体系一致"),
    (76, "ACMin", "u32", "verified", "防御下限：布衣2/轻型盔甲3/重盔甲4/魔法长袍3；金创药(小)=30(药品复用为 HP 恢复量)"),
    (80, "ACMax", "u32", "verified", "防御上限：布衣2/重盔甲9/魔法长袍7；金创药(小)=0"),
    (84, "Reserved", "u32", "unverified", "全 0"),
    (88, "MACMin", "u32", "verified", "魔御下限：布衣0/轻型盔甲1/重盔甲2/魔法长袍3；魔法药(小)=40(药品复用为 MP 恢复量)"),
    (92, "MACMax", "u32", "verified", "魔御上限：布衣1/轻型盔甲2/重盔甲3/魔法长袍4；魔法药(小)=0"),
    (96, "Unk96", "u32", "unverified", "5 条非零(0-6)"),
    (104, "DCMin", "u32", "verified", "攻击下限：木剑2/铁剑4/青铜剑3/凝霜10/井中月13/裁决0/屠龙2"),
    (108, "DCMax", "u32", "verified", "攻击上限：木剑5/铁剑9/凝霜16/井中月23/裁决38/屠龙40"),
    (112, "Attr1", "u32", "unverified", "魔法/属性区。牛角戒指1-1、古铜戒指1-0、蓝色水晶戒指2-1；法袍1。方向未定"),
    (116, "Attr2", "u32", "unverified", "与 off112 成对(魔法上限/下限之一)；白金项链=1、金戒指=1"),
    (120, "Attr3", "u32", "unverified", "道术/属性区：魅力戒指2、道德戒指3、龙纹剑9、逍遥扇11"),
    (124, "Attr4", "u32", "unverified", "元素属性(体验炼狱火/冰/雷/风=1/2/3/4)；龙纹剑5(圣?)"),
    (128, "Luck", "u32", "unverified", "幸运/诅咒？156 条非零，max=8；龙纹剑5、无极棍4"),
    (132, "Unk132", "u32", "unverified", "17 条非零(0-5)"),
    (136, "Unk136", "u32", "unverified", "9 条非零(0-3)"),
    (140, "NeedLevel", "u32", "verified", "需求等级：青铜斧14/短剑10/凝霜25/重盔甲22/井中月35/裁决138(38级+100标记)/旋风流星刀145(45级+100)；技能书=职业(基本剑术0战士/火球术1法师/治愈术2道士)"),
    (144, "Price", "u32", "verified", "价格：金币1/金创药(小)80/肉200/布衣500/木剑50/铁剑1000/凝霜8000/裁决40000/屠龙80000"),
    (148, "StackSize", "u32", "medium", "堆叠上限：食物(肉/包子/干肉)=10000、药粉=50、装备/药品=5"),
    (152, "NameLen", "u8", "verified", "名称长度(1B)，最大 31B"),
    (153, "Name", "gbk", "verified", "GBK 名称，1143/1143 可解码：金币/金创药(小)/布衣(男)/木剑/凝霜…"),
]


def load_stditem() -> dict:
    d = ENVIR.joinpath("stditem.dat").read_bytes()
    n = struct.unpack_from("<I", d, 0)[0]
    assert n == 1143 and len(d) == 4 + n * 184, (n, len(d))
    recs = []
    for i in range(n):
        raw = bytes(b ^ 0x04 for b in d[4 + i * 184 : 4 + (i + 1) * 184])
        rec = {"Index": i, "StdMode": u32(raw, 36), "Shape": u32(raw, 40),
               "Weight": u32(raw, 44), "AniCount": u32(raw, 48),
               "NeedIdentify": u32(raw, 52), "Special": u32(raw, 56),
               "Special2": u32(raw, 60), "SubType": u32(raw, 64),
               "Looks": u32(raw, 68), "DuraMax": u32(raw, 72),
               "ACMin": u32(raw, 76), "ACMax": u32(raw, 80),
               "Reserved": u32(raw, 84), "MACMin": u32(raw, 88),
               "MACMax": u32(raw, 92), "Unk96": u32(raw, 96),
               "DCMin": u32(raw, 104), "DCMax": u32(raw, 108),
               "Attr1": u32(raw, 112), "Attr2": u32(raw, 116),
               "Attr3": u32(raw, 120), "Attr4": u32(raw, 124),
               "Luck": u32(raw, 128), "Unk132": u32(raw, 132),
               "Unk136": u32(raw, 136), "NeedLevel": u32(raw, 140),
               "Price": u32(raw, 144), "StackSize": u32(raw, 148),
               "NameLen": raw[152], "Name": gbk_name(raw, 152, 153),
               "raw": [u32(raw, o) for o in range(0, 184, 4)]}
        recs.append(rec)
    return {"file": "stditem.dat", "count": n, "record_size": 184,
            "mask": 0x04, "header": "4B 明文 UINT32 = 记录数",
            "fields": STDITEM_FIELDS, "records": recs}


# ----------------------------------------------------------------- magic ---
MAGIC_FIELDS = [
    (0, "Index", "u32", "verified", "技能 ID（非顺序：25,23,33,53,107,108,39,40,110,105,111,12,5,113,112,120,9,24,101,123…）"),
    (20, "MagicSchool", "u32", "high", "系别：0火/1冰/2雷/3风/4治疗/5符咒/6召唤/7战技/99装备变体。锚点：火球术0/爆裂火焰0/冰咆哮1/雷电术2/治愈术4/施毒术5/召唤骷髅6/半月弯刀7/烈火剑法7"),
    (24, "Unk24", "u32", "unverified", "0-10 小值"),
    (28, "Requires", "u32", "low", "疑似前置技能 ID（≈Index-2；火球术=1/治愈术=2 例外）"),
    (32, "TrioA1", "u32", "verified", "威力三元组 (A1,A2,A3)。攻击系 A2/A3=每级威力增量，与 Zircon 逐项吻合：火球(3,6,10)↔每级+6-10、大火球(6,15,19)↔+15-19、雷电术(7,15,19)↔+15-19、冰月震天(7,13,17)↔+13-17、风掌(3,5,9)、冰月神掌(4,4,8)、霹雳掌(4,6,10)、治愈术(7,11,15)↔+11-15；A1≈L1 威力下限基数。召唤/护盾类 A2=A3=0 且 A1=召唤物强度/护盾值（召唤骷髅15、魔法盾20、回生100），语义独立。群攻技能（爆裂15,14,18/冰咆哮19,12,16）非单调，属各技能独立数值"),
    (36, "TrioA2", "u32", "verified", "见 TrioA1（攻击系=每级威力增量，已验证）"),
    (40, "TrioA3", "u32", "verified", "见 TrioA1（攻击系=每级威力增量，已验证）"),
    (44, "TrioB1", "u32", "verified", "耗蓝三元组 (B1,B2,B3)。B1=L1 耗蓝，30+ 技能与 Zircon 吻合：火球1/治愈2/召唤骷髅10/魔法盾30/圣言术30/群体治愈20/回生100/困魔咒10；铁布衫25 vs Defiance 40、怒神霹雳38 vs 30、焰天火雨33 vs 40 为版本数值调整。B2/B3 非单调（火球(1,0,4)/治愈(2,0,0)），疑 L2/L3 耗蓝，待定"),
    (48, "TrioB2", "u32", "unverified", "见 TrioB1（B2/B3 语义待定）"),
    (52, "TrioB3", "u32", "unverified", "见 TrioB1（B2/B3 语义待定）"),
    (56, "Kind", "u32", "medium", "0=战技/被动(半月弯刀/基本剑术/精神力战法)、1=攻击魔法(火球术/雷电术/圣言术)、2=辅助(治愈/隐身/召唤/铁布衫)"),
    (60, "NeedLevel1", "u32", "high", "1级学习等级。Zircon 交叉验证：火球7/召唤骷髅17/集体隐身23/火墙24/召唤神兽30 与 Zircon 完全一致"),
    (64, "TrainExp1", "u32", "high", "1级修炼经验。火球10(比值×10)/召唤骷髅4(×100)/集体隐身600(×1) — 比值差异=版本证据"),
    (68, "NeedLevel2", "u32", "high", "2级学习等级（火球9/召唤骷髅19/集体隐身25/火墙26/召唤神兽32，同 Zircon）"),
    (72, "TrainExp2", "u32", "high", "2级修炼经验"),
    (76, "NeedLevel3", "u32", "high", "3级学习等级（火球11/召唤骷髅21/集体隐身27/火墙28/召唤神兽34，同 Zircon）"),
    (80, "TrainExp3", "u32", "high", "3级修炼经验"),
    (84, "SkillPoints", "u32", "medium", "技能点/熟练度需求或 Delay。战技/被动=0-100、低级魔法40-60、中级70-90、高级100-250、回生500/召唤200/铁布衫200(难练)。火球50/治愈40/召唤骷髅200"),
    (88, "FX1", "u32", "unverified", "特效资源引用(14 条非零：聚集/连锁系/抗拒火环/雷电术等)，形态 0x00XXYY00"),
    (92, "FX2", "u32", "unverified", "见 FX1"),
    (96, "FX3", "u32", "unverified", "见 FX1"),
    (100, "FX4", "u32", "unverified", "见 FX1"),
    (104, "NameLen", "u8", "verified", "名称长度(1B)"),
    (105, "Name", "gbk", "verified", "GBK 名称 24B 区：半月弯刀/爆裂火焰/冰咆哮/乾坤大挪移…105/105 可解码"),
]


def load_magic() -> dict:
    d = ENVIR.joinpath("magic.dat").read_bytes()
    n = struct.unpack_from("<I", d, 0)[0]
    assert n == 105 and len(d) == 4 + n * 120, (n, len(d))
    recs = []
    for i in range(n):
        raw = bytes(b ^ 0x11 for b in d[4 + i * 120 : 4 + (i + 1) * 120])
        rec = {"Index": u32(raw, 0), "MagicSchool": u32(raw, 20),
               "Unk24": u32(raw, 24), "Requires": u32(raw, 28),
               "TrioA1": u32(raw, 32), "TrioA2": u32(raw, 36), "TrioA3": u32(raw, 40),
               "TrioB1": u32(raw, 44), "TrioB2": u32(raw, 48), "TrioB3": u32(raw, 52),
               "Kind": u32(raw, 56),
               "NeedLevel1": u32(raw, 60), "TrainExp1": u32(raw, 64),
               "NeedLevel2": u32(raw, 68), "TrainExp2": u32(raw, 72),
               "NeedLevel3": u32(raw, 76), "TrainExp3": u32(raw, 80),
               "SkillPoints": u32(raw, 84),
               "FX1": u32(raw, 88), "FX2": u32(raw, 92),
               "FX3": u32(raw, 96), "FX4": u32(raw, 100),
               "NameLen": raw[104], "Name": gbk_name(raw, 104, 105),
               "raw": [u32(raw, o) for o in range(0, 120, 4)]}
        recs.append(rec)
    return {"file": "magic.dat", "count": n, "record_size": 120,
            "mask": 0x11, "header": "4B 明文 UINT32 = 记录数",
            "fields": MAGIC_FIELDS, "records": recs}


# --------------------------------------------------------------- monster ---
MONSTER_FIELDS = [
    (0, "Index", "u32", "verified", "怪物 ID（值域 1..433；记录 1..203 与行号一致，其余 229 行乱序=后追加）"),
    (24, "Appr", "u32", "high", "外观图 ID：81(守卫/战士系 136条)/97(血系)/115(赤月恶魔)/230(霸王守卫)"),
    (28, "Race", "u32", "medium", "种族：19(普通怪 312条)/49(祖玛教主)/47(祖玛雕像)/34(赤月恶魔)/13(食人花)"),
    (32, "RaceImg", "u32", "unverified", "图集/动作集：140/160/108/43/104…分散"),
    (36, "Level", "u32", "medium", "等级：守卫系列99/祖玛教主94/赤月恶魔93/霸王教主96/白野猪75/半兽人13/食人花10/暗黑战士25"),
    (40, "Flag40", "u32", "unverified", "0/1 标记（1×138条）"),
    (44, "Flag44", "u32", "unverified", "0/1 标记（1×103条）"),
    (48, "Flag48", "u32", "unverified", "0/2/5/10（5×70条）"),
    (52, "Strength", "u32", "unverified", "强度档/未知：0×151条、10×85、100×74、50×29、60×27。守卫武将10/赤月100/半兽人0"),
    (56, "HP", "u32", "high", "生命：半兽人30/食人花21/暗黑战士320/白野猪4500/祖玛教主14000/赤月恶魔13000/霸王教主20000；名字带61后缀变体=1(活动/一击死)"),
    (60, "Exp", "u32", "medium", "经验（或 MP）：半兽人30/食人花21/暗黑战士240/祖玛教主10500/赤月恶魔9750/霸王教主12000"),
    (64, "Unk64", "u32", "unverified", "5 条非零(10/40/100)"),
    (68, "ACMin", "u32", "medium", "物防（与 off72 恒等，min=max）：半兽人0/守卫武将9/白野猪13/祖玛教主65/赤月恶魔52"),
    (72, "ACMax", "u32", "medium", "见 ACMin（恒等于 ACMin）"),
    (76, "MAC", "u32", "medium", "魔防：半兽人1/暗黑战士6/守卫武将27/白野猪30/祖玛教主120/赤月恶魔100"),
    (80, "Prop1", "u32", "unverified", "属性位图/抗性区 1（值域 5/4/0/-1..-4，祖玛教主=-2 0xFFFFFFFE）"),
    (84, "Prop2", "u32", "unverified", "抗性区 2（5/1/0/-1..-5）"),
    (88, "Prop3", "u32", "unverified", "抗性区 3"),
    (92, "Prop4", "u32", "unverified", "抗性区 4（-1 0xFFFFFFFF 59条）"),
    (96, "Prop5", "u32", "unverified", "抗性区 5"),
    (100, "Prop6", "u32", "unverified", "抗性区 6"),
    (104, "Prop7", "u32", "unverified", "抗性区 7"),
    (108, "DCMin", "u32", "medium", "物攻下限：半兽人4/暗黑战士14/守卫武将77/白野猪44/祖玛教主70/赤月恶魔90/霸王教主145"),
    (112, "DCMax", "u32", "medium", "物攻上限：半兽人8/暗黑战士28/守卫武将84/白野猪66/祖玛教主175/赤月恶魔180/霸王教主245"),
    (116, "MC", "u32", "unverified", "魔法攻击？120 条非零：守卫狮子6/祖玛教主1/火焰狮子1"),
    (120, "Unk120", "u32", "unverified", "分散：祖玛教主208/守卫狮子158/白野猪4"),
    (124, "Unk124", "u32", "unverified", "分散：守卫狮子/火焰狮子44"),
    (128, "HitSpeed", "u32", "unverified", "命中/攻击速度：半兽人10/食人花9/守卫武将18/祖玛教主23/赤月23"),
    (132, "MoveSpeed", "u32", "unverified", "敏捷/移动速度：半兽人12/食人花11/守卫武将19/祖玛教主25/赤月25"),
    (136, "Unk136", "u32", "unverified", "500-1500：守卫武将700/半兽人1500/祖玛教主1000/赤月恶魔0"),
    (140, "Unk140", "u32", "unverified", "仅 4 条非零(=1)"),
    (144, "Unk144", "u32", "unverified", "1×392 条（赤月恶魔0/霸群雕像0 除外）"),
    (148, "Unk148", "u32", "unverified", "1200-3000：守卫武将2500/半兽人2500/祖玛教主1500/赤月恶魔1500/署箭0"),
    (152, "DropTable", "u32", "unverified", "掉落组 ID：守卫系列15/普通怪10（78条非零，354条=0）"),
    (156, "Zero156", "u32", "unverified", "off156-231 全 0（19 个 u32 保留区）"),
    (233, "NameLen", "u8", "verified", "名称长度(1B)，最大 18B，432/432 验证通过"),
    (234, "Name", "gbk", "verified", "GBK 名称：守卫武将/赤月恶魔/祖玛教主/霸王教主/白野猪/半兽人…；重复名(魔石狂热者×2/圣诞树×2)=强度变体"),
]


def load_monster() -> dict:
    d = ENVIR.joinpath("monster.dat").read_bytes()
    n = len(d) // 252
    assert n == 433 and len(d) == n * 252, (n, len(d))
    recs = []
    for i in range(n):
        raw = bytes(b ^ 0x09 for b in d[i * 252 : (i + 1) * 252])
        if i == 0:
            rec = {"Index": 0, "HeaderCount": 433, "HeaderUnk36": u32(raw, 36),
                   "Note": "头部占位记录（raw 首 u32=433 明文，其余 0x09 填充；off36=60 含义未确认）",
                   "Name": "", "raw": [u32(raw, o) for o in range(0, 252, 4)]}
            recs.append(rec)
            continue
        rec = {"Index": u32(raw, 0), "Appr": u32(raw, 24), "Race": u32(raw, 28),
               "RaceImg": u32(raw, 32), "Level": u32(raw, 36),
               "Flag40": u32(raw, 40), "Flag44": u32(raw, 44),
               "Flag48": u32(raw, 48), "Strength": u32(raw, 52),
               "HP": u32(raw, 56), "Exp": u32(raw, 60), "Unk64": u32(raw, 64),
               "ACMin": u32(raw, 68), "ACMax": u32(raw, 72), "MAC": u32(raw, 76),
               "Prop1": u32(raw, 80), "Prop2": u32(raw, 84), "Prop3": u32(raw, 88),
               "Prop4": u32(raw, 92), "Prop5": u32(raw, 96), "Prop6": u32(raw, 100),
               "Prop7": u32(raw, 104),
               "DCMin": u32(raw, 108), "DCMax": u32(raw, 112),
               "MC": u32(raw, 116), "Unk120": u32(raw, 120), "Unk124": u32(raw, 124),
               "HitSpeed": u32(raw, 128), "MoveSpeed": u32(raw, 132),
               "Unk136": u32(raw, 136), "Unk140": u32(raw, 140),
               "Unk144": u32(raw, 144), "Unk148": u32(raw, 148),
               "DropTable": u32(raw, 152),
               "NameLen": raw[233], "Name": gbk_name(raw, 233, 234),
               "raw": [u32(raw, o) for o in range(0, 252, 4)]}
        recs.append(rec)
    return {"file": "monster.dat", "count": n, "record_size": 252,
            "mask": 0x09, "header": "无独立头部，rec0=占位头记录",
            "fields": MONSTER_FIELDS, "records": recs}


LOADERS = {"stditem": load_stditem, "magic": load_magic, "monster": load_monster}


def main() -> None:
    targets = sys.argv[1:] or list(LOADERS)
    for t in targets:
        data = LOADERS[t]()
        out = OUT / f"{t}.json"
        out.write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
        print(f"{out.name}: {data['count']} records, {out.stat().st_size} bytes")


if __name__ == "__main__":
    main()
