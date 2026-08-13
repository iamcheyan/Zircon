#!/usr/bin/env python3
"""生成未匹配中文文本 -> 语义键名 映射表（供审核）
用法: python3 scripts/i18n_gen_keys.py > /tmp/i18n_keymap.txt
"""
import re, glob, os

os.chdir('/home/tetsuya/development/zircon')

# 旧版全部键名（避免重复）
cn = open('GodotClient/Translations/ChineseMessages.cs', encoding='utf-8').read()
pairs = re.findall(r'public override string (\w+) \{ get; set; \} = "([^"]*)"', cn)
known_values = set(v for _, v in pairs)
existing_keys = set(k for k, _ in pairs)

# 中→英词典（翻译阶段的键名语义用；未命中的用 Ui 前缀+序号）
DICT = {
    "背包": "Bag", "金币": "Gold", "设置": "Settings", "角色": "Character",
    "技能": "Skill", "任务": "Quest", "行会": "Guild", "队伍": "Group",
    "聊天": "Chat", "出售": "Sell", "购买": "Buy", "寄售": "Consignment",
    "仓库": "Storage", "商店": "Store", "商城": "Market", "排行": "Ranking",
    "好友": "Friend", "邮件": "Mail", "公告": "Notice", "帮助": "Help",
    "退出": "Exit", "取消": "Cancel", "确定": "Ok", "确认": "Confirm",
    "搜索": "Search", "筛选": "Filter", "排序": "Sort", "名称": "Name",
    "等级": "Level", "职业": "Class", "性别": "Gender", "经验": "Exp",
    "饥饿": "Hunger", "饱食度": "Hunger", "加成": "Bonus", "稀有度": "Rarity", "未获得": "NotObtained",
    "未召唤": "NotSummoned", "伙伴": "Companion", "宠物": "Companion",
    "成员": "Member", "职务": "Rank", "权限": "Permission", "贡献": "Contribution",
    "接受": "Accept", "拒绝": "Decline", "邀请": "Invite", "允许": "Allow",
    "负重": "Weight", "防御": "Defense", "攻击": "Attack", "魔法": "Magic",
    "道术": "Tao", "战斗": "Combat", "系统": "System", "提示": "Hint",
    "本地": "Local", "世界": "Global", "喊话": "Shout", "私聊": "Whisper",
    "观察": "Observer", "在线": "Online", "离线": "Offline", "忙碌": "Busy",
    "离开": "Away", "收起": "Store", "召回": "Retrieve", "释放": "Release",
    "开始": "Start", "结束": "End", "暂停": "Pause", "继续": "Resume",
    "返回": "Back", "下一页": "Next", "上一页": "Prev", "刷新": "Refresh",
    "保存": "Save", "删除": "Delete", "添加": "Add", "移除": "Remove",
    "复制": "Copy", "粘贴": "Paste", "全部": "All", "无": "None",
    "是": "Yes", "否": "No", "选择": "Select", "创建": "Create",
    "登录": "Login", "注册": "Register", "密码": "Password", "账号": "Account",
    "邮箱": "Email", "验证": "Verify", "武器": "Weapon", "防具": "Armour",
    "头盔": "Helmet", "项链": "Necklace", "戒指": "Ring", "手镯": "Bracelet",
    "鞋子": "Shoes", "毒药": "Poison", "护身符": "Amulet", "宝石": "Gem",
    "镶嵌": "Socket", "精炼": "Refine", "制作": "Craft", "修理": "Repair",
    "锻造": "Forge", "升级": "Upgrade", "合成": "Combine", "分解": "Fragment",
    "兑换": "Exchange", "强化": "Enhance", "附魔": "Enchant", "洗练": "Reroll",
    "今天": "Today", "明天": "Tomorrow", "昨天": "Yesterday", "日期": "Date",
    "时间": "Time", "数量": "Count", "价格": "Price", "总价": "Total",
    "单价": "UnitPrice", "黎明": "Dawn", "黄昏": "Dusk", "夜晚": "Night", "白天": "Day",
    "重量": "Weight", "金币数": "GoldCount", "物品": "Item", "道具": "Item",
    "装备": "Equipment", "药品": "Potion", "卷轴": "Scroll", "材料": "Material",
    "任务日志": "QuestLog", "怪物": "Monster", "奖励": "Reward", "目标": "Target",
    "条件": "Requirement", "描述": "Description", "详情": "Details",
    "购买数量": "BuyAmount", "最高价": "HighestPrice", "最低价": "LowestPrice",
    "最新": "Newest", "历史": "History", "下架": "RemoveListing",
    "上架": "AddListing", "行会资金": "GuildFunds", "我的寄售": "MyConsignments",
    "寄售行": "ConsignmentTitle", "在线状态": "OnlineState", "查看状态": "ViewState",
    "用户": "User", "未知": "Unknown", "玩家": "Player", "位置": "Location",
    "安全区": "SafeZone", "战争": "War", "编组": "Group", "快捷": "Quick",
    "快捷键": "Hotkey", "界面": "UI", "画面": "Graphics", "声音": "Sound",
    "游戏": "Game", "网络": "Network", "显示": "Display", "效果": "Effects",
    "操作": "Controls", "颜色": "Colours", "重置": "Reset", "恢复默认": "ResetDefault",
}

def to_key(word):
    for cn_word, en in DICT.items():
        if cn_word in word:
            return en
    return None

# 扫描所有文件
files = glob.glob('GodotClient/Controls/*.cs') + glob.glob('GodotClient/Scripts/*.cs') + glob.glob('GodotClient/Scenes/*.cs')
done = {'InventoryDialog.cs', 'MainPanel.cs', 'ConfigDialog.cs', 'Lang.cs'}
line_re = re.compile(r'"([^"]*[\u4e00-\u9fff][^"]*)"')

rows = []  # (file, line, value, win_prefix, suggested_key)
generated_keys = set()  # 本次生成的键名（防自重复）
for f in files:
    name = f.split('/')[-1]
    if name in done: continue
    win = name.replace('.cs', '').replace('Dialog', '').replace('Scene', '')
    for i, line in enumerate(open(f, encoding='utf-8'), 1):
        stripped = line.strip()
        if stripped.startswith('//') or stripped.startswith('*'): continue
        if 'GD.Print' in line or 'GD.PushError' in line or 'Console.' in line: continue
        if re.match(r'^\[[A-Za-z]', stripped): continue
        for m in line_re.finditer(line):
            v = m.group(1)
            if v in known_values: continue
            # 排除复杂插值（含嵌套引号）——脚本无法安全处理，留给人工
            if '"' in v: continue
            en = to_key(v)
            if en:
                key = f'{win}{en}Label'
            else:
                key = f'{win}Ui{len(rows)}Label'
            # 避免与旧版已有键或本次已生成的键冲突
            n = 2
            base = key
            while key in existing_keys or key in generated_keys:
                key = f'{base}{n}'
                n += 1
            generated_keys.add(key)
            rows.append((name, i, v, key))

# 去重输出（同值同键）
seen = set()
for name, i, v, key in rows:
    if (v, key) in seen: continue
    seen.add((v, key))
    dup = " [重复值!]" if sum(1 for r in rows if r[2] == v) > 1 else ""
    print(f'{key}\t"{v}"\t{name}:{i}{dup}')
print(f'\n# 共 {len(rows)} 处, 唯一值 {len(set(r[2] for r in rows))}', file=__import__('sys').stderr)
