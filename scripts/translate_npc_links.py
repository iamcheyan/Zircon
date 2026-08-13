#!/usr/bin/env python3
"""NPC 对话链接文本中文化：把 Say 里的 [Text:ID] 链接文字替换为中文。
用法: python3 scripts/translate_npc_links.py   （更新 scripts/translations/npc_say_zh.tsv）
"""
import re, sys, os

os.chdir('/home/tetsuya/development/zircon')

# 链接文本词典（英文 -> 中文）。键必须与 Say 里的 [Text:ID] 完全一致（不区分大小写）。
LINK_DICT = {
    # 通用
    "Main": "主菜单", "Close": "关闭", "Exit": "退出", "Back": "返回",
    "Back away slowly": "慢慢后退", "Continue": "继续", "Accept": "接受",
    "Ask": "询问", "Inquire": "查询", "Proceed": "继续", "Start": "开始",
    "About": "关于", "Retry": "重试", "Change": "更换", "Reset": "重置",
    "Upgrade": "升级", "Buy": "购买", "Sell": "出售", "Refine": "精炼",
    "Yes, Refine": "是，精炼", "Sign Here": "在此签名", "Level Up": "升级",
    "Purchase Title": "购买称号", "Freedom Pass": "通行证",
    # 商店
    "Browse Potions": "浏览药水", "Browse Weapons": "浏览武器",
    "Browse Armours": "浏览护甲", "Browse Helmets and Shoes": "浏览头盔和鞋子",
    "Browse Rings": "浏览戒指", "Browse Bracelets": "浏览手镯",
    "Browse Necklaces": "浏览项链", "Browse Essentials": "浏览必需品",
    "Browse Taoist Goods": "浏览道士用品", "Browse Dark Stones": "浏览暗石",
    "Browse Store": "浏览商店", "Repair Weapons": "修理武器",
    "Repair Armours": "修理护甲", "Repair Jewellery": "修理首饰",
    "Repair Items": "修理物品", "Buy Items": "购买物品", "Buy Pet Food": "购买宠物粮",
    "Manage Pets": "宠物管理", "Upgrade Trinkets": "升级饰品",
    "Fragment Items": "碎片物品", "Exchange Frament": "兑换碎片",
    "Exchange Frament (II)": "兑换碎片（二）", "Craft Weapon": "打造武器",
    "Reset Weapon": "重置武器", "Remove Ring": "摘除戒指",
    "Retrieve": "取出", "Sell Horse": "出售马匹",
    # 职业书
    "Warrior Books": "战士书籍", "Wizard Books": "法师书籍",
    "Taoist Books": "道士书籍", "Assassin Books": "刺客书籍",
    # 传送
    "Bichon Town": "比奇县", "Banya Village": "潘夜村", "Banya Island": "潘夜岛",
    "Sabuk Keep": "沙巴克城", "Numa Village": "诺玛村", "Lost Paradise": "失乐园",
    "Desert Mud Wall": "沙漠土墙", "Holy Palace": "圣殿", "Inner Wall": "内墙",
    "Lost Land": "失落之地", "Banyo Cave": "潘夜洞穴", "Bichon Castle": "比奇城",
    "Taoist Temple": "道观", "Western Arids": "西部荒原", "Arid Flats": "荒原",
    "Frost Village": "霜村", "Infernal Island": "魔岛", "Mystery Ship": "神秘船",
    "Lava Area": "熔岩区域",
    # 结婚
    "Get Married": "结婚", "Get Devorced": "离婚", "Remove Wedding Ring": "摘除婚戒",
    "Make Wedding Ring": "制作婚戒",
    # 宠物
    "Brown Horse": "棕马", "White Horse": "白马", "Red Horse": "红马", "Black Horse": "黑马",
    # 属性
    "Health": "生命", "Mana": "魔法", "AC": "防御", "MR": "魔抗",
    "Accuracy": "准确", "Agility": "敏捷", "Attack Speed": "攻击速度",
    "Critical Chance": "暴击率", "Critical Damage": "暴击伤害",
    "Block Chance": "格挡率", "Evasion Chance": "闪避率", "Life Steal": "吸血",
    # 元素
    "Fire": "火", "Ice": "冰", "Lightning": "雷", "Wind": "风",
    "Holy": "神圣", "Dark": "暗", "Phantom": "幻影", "Slow": "减速",
    "Paralysis": "麻痹", "Silence": "沉默",
    # 转化
    "Yellow Orb to Yellow Trinket": "黄宝珠→黄饰品",
    "Yellow Trinket to Yellow Cube": "黄饰品→黄方块",
    "Blue Orb to Blue Trinket": "蓝宝珠→蓝饰品",
    "Blue Trinket to Blue Cube": "蓝饰品→蓝方块",
    "Red Orb to Red Trinket": "红宝珠→红饰品",
    "Red Trinket to Red Cube": "红饰品→红方块",
    "Purple Orb to Purple Trinket": "紫宝珠→紫饰品",
    "Purple Trinket to Purple Cube": "紫饰品→紫方块",
    "Green Orb to Green Trinket": "绿宝珠→绿饰品",
    "Green Trinket to Green Cube": "绿饰品→绿方块",
    "Grey Orb to Grey Trinket": "灰宝珠→灰饰品",
    "Grey Trinket to Grey Cube": "灰饰品→灰方块",
    # 精炼材料名（保留物品名映射）
    "Rusty Signet Of Myrmidon": "生锈的武士印章",
    "Rusty Signet Of Evoker": "生锈的召唤师印章",
    "Rusty Signet Of Vicar": "生锈的牧师印章",
    "Rusty Charm Of The Destroyer": "生锈的毁灭者护符",
    "Rusty Amulet Of Dark Sorcery": "生锈的黑暗巫术护符",
    "Rusty Pendant Of Purification": "生锈的净化吊坠",
    "Rusty Bracer Of Revelation": "生锈的启示护腕",
    "Rusty Ring Of Enlightenment": "生锈的启蒙戒指",
    "Rusty Bracelet Of Ascension": "生锈的飞升手镯",
    "Bracelet Of Overlord": "霸主手镯",
    "Medallion Of Overlord": "霸主勋章",
    "Seal Of Overlord": "霸主之印",
    "Arcanist's Band Of Dignity": "法师的尊严之戒",
    "Arcanist's Bracelet Of Dignity": "法师的尊严手镯",
    "Arcanist's Amulet Of Dignity": "法师的尊严护符",
    "Hierophant's Signet Of Moon": "圣者的月之印章",
    "Hierophant's Bracer Of Moon": "圣者的月之护腕",
    "Hierophant's Pendant Of Moon": "圣者的月之吊坠",
    "Fragment (III)": "碎片（三）",
    # 其他
    "Rebirth": "重生", "Fire": "火", "Again": "再来",
}

def translate_links(say):
    def repl(m):
        text = m.group(1).strip()
        key = text.lower()
        # 精确匹配（忽略大小写）
        for en, zh in LINK_DICT.items():
            if en.lower() == key:
                return f'[{zh}:{m.group(2)}]'
        # 未匹配的保持原样
        return m.group(0)
    return re.sub(r'\[([^\[\]:]+):(\d+)\]', repl, say)

# 处理 npc_say_zh.tsv
path = 'scripts/translations/npc_say_zh.tsv'
lines = open(path, encoding='utf-8').read().strip().split('\n')
out = []
changed = 0
for l in lines:
    parts = l.split('\t')
    if len(parts) >= 2:
        new_say = translate_links(parts[1])
        if new_say != parts[1]:
            changed += 1
        parts[1] = new_say
    out.append('\t'.join(parts))

with open(path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(out) + '\n')

print(f'处理 {len(lines)} 行，{changed} 行链接已中文化')

# 输出未匹配的链接（需要补词典）
all_links = set()
for l in out:
    parts = l.split('\t')
    if len(parts) >= 2:
        for m in re.finditer(r'\[([^\[\]:]+):(\d+)\]', parts[1]):
            all_links.add(m.group(1).strip().lower())
matched = set(k.lower() for k in LINK_DICT)
unmatched = all_links - matched
print(f'\n未匹配链接文本（需补词典）: {len(unmatched)}')
for u in sorted(unmatched)[:20]:
    print(f'  [{u}]')
