#!/usr/bin/env python3
"""Build a self-contained HTML catalog of the mir3ei (2003 Korean Mir3) 566 maps.
Embeds per-map JPEG thumbnails as base64, plus 34 family contact sheets.
Sources: /tmp/mir3ei_views/*.png, /tmp/mir3ei_contact/*.png,
         /tmp/mir3ei_bestmatch.json, /tmp/zircon_mapnames.txt
Visual classification notes come from the 34 contact-sheet audits (session 019fd6dc).
"""
import base64, glob, io, json, os, re
from PIL import Image

BASE = os.path.dirname(os.path.abspath(__file__))
ROOT = BASE
VIEWS = os.path.join(BASE, "views")
CONTACT = os.path.join(BASE, "contact")
DATA = os.path.join(BASE, "data")
BEST = json.load(open(os.path.join(DATA, "mir3ei_bestmatch.json"), encoding="utf-8"))
NAMES = {}
for line in open(os.path.join(DATA, "zircon_mapnames.txt"), encoding="utf-8"):
    if "=" in line:
        k, v = line.split("=", 1)
        NAMES[k.strip()] = v.strip()

THUMB_W = 300  # max thumb width px

def thumb_b64(path, maxw=THUMB_W, q=78):
    im = Image.open(path).convert("RGB")
    im.thumbnail((maxw, maxw * 2))
    buf = io.BytesIO()
    im.save(buf, "JPEG", quality=q, optimize=True)
    return base64.b64encode(buf.getvalue()).decode()

def sheet_b64(path, maxw=900, q=80):
    im = Image.open(path).convert("RGB")
    im.thumbnail((maxw, maxw * 2))
    buf = io.BytesIO()
    im.save(buf, "JPEG", quality=q, optimize=True)
    return base64.b64encode(buf.getvalue()).decode()

# ---------------- classification ----------------
# category: town / wilderness / dungeon / special / empty / mixed

FAMILIES = {
    "num": dict(title="城镇与据点 (0–81)", category="mixed",
        desc="经典 Mir3 城镇编号体系(0–8)与野外大图。城镇布局(城墙围合院落、建筑群、放射路网)与野外(沙漠/海岸/山道迷宫)混合。",
        evidence="城镇名沿用经典编号与 2017 中文版同名:0 比奇城、1 失乐园、2 潘夜村、3 沙巴克城、4 努玛村、5 沙漠土城、8 南哨站;指纹:4/5→17 Lost Oasis(2017 重排号)、6→16 Western Arids、8→D4003 Southern Check Point、41–44→D4000/D4001/D4002/18、71–78→16_001/16_002/16_003、12/121–125→D3901–3906(全部 sim≈1.0)。",
        sheet="num.png"),
    "other": dict(title="城镇建筑内部 / 野外 / 洞穴 (130)", category="mixed",
        desc="三大类混合:0_001–0_0033 比奇城建筑内部(矩形房间/门洞/楼梯,对应新版 Town Hall/Left Wing/Right Wing)、4_001–4_005 努玛村室内、5_0011–5_006 沙漠土城室内、d501–d515 城镇建筑室内;1_001–1_023 失乐园周边野外(1_009 唯一绿地、1_020 隧道);d60011–d611 洞穴通道网、d7101–d714 洞穴、d802–d828 混合(约 15 张沙漠斜坡野外 + 16 张蚂蚁洞系迷宫洞穴,d8 系指纹 →D404 系 0.71–0.73)。",
        evidence="视觉鉴定 + 指纹:d8 迷宫系 → D404/D402/D403 Ant Cave 系 0.71–0.73(未达 0.85 阈值,但同属蚂蚁洞家族);小图指纹(0_001→D1401 0.99)为单瓦片主导的巧合,以视觉为准。",
        sheet="other.png"),
    "kt": dict(title="特殊活动房 (21)", category="special",
        desc="小型特殊地图:kt0000/kt0002–0010 九张浅棕小岩室;kt0001 深灰菱形网格室内(唯一带网格铺地的);kt0011–0017 深棕大室/长廊/竖井(kt0012/13 长廊、kt0016/161 竖井、kt0017 楔形大厅);kt0018/kt00181 对称十字形竞技场(四臂 + 中央节点)。",
        evidence="指纹全部 ≤0.25(如 Ithuejingot 0.25)→ 2017 版无对应,2003 独有活动房。kt0001→D1401 0.94 为小图巧合。",
        sheet="kt.png"),
    "E": dict(title="事件实例图 (13)", category="special",
        desc="E 前缀事件图:迷宫 4 张(E001/E401/E601/E604 细道缠绕)+ 竞技场 5 张(E404/E602 同心菱形、E605 厚 V 形、E002_001/E402_001 近实心大室)+ 天然洞穴 3 张(E402/E403/E603 锯齿边缘)。全为黑白双色碰撞掩码。",
        evidence="E401/E402 系指纹 → 新版 Ithuejingot_WaitR/Ithuejingot 0.25(弱相关)。",
        sheet="E.png"),
    "DM": dict(title="DM 特殊区 (3)", category="special",
        desc="DM001/DM011 全黑空图(无任何几何);DM002 金黄/赭色碎片水平带 + 不规则簇,洞穴或迷宫带。",
        evidence="DM 前缀疑似 Dark Maze;视觉无几何可读。",
        sheet="DM.png"),
    "B": dict(title="B 系 (61)", category="mixed",
        desc="约 45 张灰色小型洞穴斑块(B10x–B14x,蚂蚁洞式小腔);B010/B011 构造要塞/城镇(最规整);B102_001/B103_001 沙漠野外(顶部绿边)、B106_001–3/B115_001/B118_001/B132_001/B134_001 荒野斑块;B139_001/B140_001 深灰菱形堡垒;3 张全黑空图。",
        evidence="视觉鉴定。B 系无指纹锚定(2017 精简)。",
        sheet="B.png"),
    "D00xx": dict(title="洞穴迷宫 (5)", category="dungeon",
        desc="全洞穴:D001/D002 迷宫、D001_001/D002_001 块状单室、D003 通道网。",
        evidence="视觉:棕褐岩地 + 黑虚空。", sheet="D00xx.png"),
    "D01xx": dict(title="洞穴 (3)", category="dungeon",
        desc="D011 迷宫、D012 十字形复合体、D012_001 简单矩形室。",
        evidence="视觉。", sheet="D01xx.png"),
    "D02xx": dict(title="沙漠/迷宫混合 (7)", category="mixed",
        desc="D021/D023_001/D023_002/D024 实心沙漠荒野块(无内墙);D022/D023 人工菱形/网格迷宫;D022_001 天然洞穴。",
        evidence="视觉。", sheet="D02xx.png"),
    "D03xx": dict(title="洞穴迷宫 (2)", category="dungeon",
        desc="D032 中央矩形室 + 轴向走廊枢纽;D033 高密度蛛网迷宫。",
        evidence="视觉。", sheet="D03xx.png"),
    "D04xx": dict(title="洞穴迷宫 (1)", category="dungeon",
        desc="D042 菱形足印内密集窄通道 + 两个矩形室。",
        evidence="视觉。", sheet="D04xx.png"),
    "D05xx": dict(title="洞穴迷宫 (2)", category="dungeon",
        desc="D052 中央矩形枢纽室 + 分支通道;D053 高密度互联迷宫,中央不规则斑块。",
        evidence="视觉。", sheet="D05xx.png"),
    "D10xx": dict(title="天然洞穴系 (31)", category="dungeon",
        desc="棕褐/米色粗糙岩地 + 黑虚空;迷宫通道 + 大腔室 + 椭圆形 boss 房模板(D10061/D10062/D10071 等);无城镇/船/绿地。",
        evidence="视觉鉴定;指纹无锚定(2017 精简)。", sheet="D10xx.png"),
    "D11xx": dict(title="潘夜神殿 Banya Temple (14)", category="dungeon",
        desc="石构房间/走廊网络 + 菱形 boss 房:D1115 嵌套同心菱形、D1102/D1111 对称菱形;D1110 实心大块、D1116 大凹腔室为例外;D11031/D1112/D1113 紧密走廊迷宫。",
        evidence="指纹锚定:D1101→D1001 Banya Temple Lv 1、D1102→D1002,sim=1.0。", sheet="D11xx.png"),
    "D12xx": dict(title="石构迷宫/洞穴混合 (12)", category="dungeon",
        desc="几何石构迷宫(D1202 网格、D1204 格状、D12032 尖角回廊、D1213、D1215 建筑复合)与有机洞穴(D1201/D12031/D1205/D1211/D1214)混合;D12122 近实心三角块。",
        evidence="指纹全家族 sim=1.0 → 新版 D203/D2304/D904/D601/D603/D1802/D2501 等,新版未命名。", sheet="D12xx.png"),
    "D13xx": dict(title="城镇建筑内部 (15)", category="town",
        desc="直角矩形房间/走廊/门洞,灰褐米色(D1301–05/D1311–15/D1321–25);与 Town Hall/Potion Merchant 布局相符。",
        evidence="指纹 → 0_000 Town Hall 0.87 + 视觉确认。", sheet="D13xx.png"),
    "D14xx": dict(title="暖棕洞穴地牢 (15)", category="dungeon",
        desc="虫状隧道,无冰蓝/冰晶观感;'Frost Dungeon Lv 6' 名称与视觉不符(或 minimap 固定调色板)。",
        evidence="指纹 → D2106 Frost Dungeon Lv 6 / D2004 / D1305,0.9。", sheet="D14xx.png"),
    "D15xx": dict(title="真天宫 Jinchon Palace (21)", category="dungeon",
        desc="17 张对称宫殿内部(细走廊 + 小矩形室 + 中央庭院环形廊,严格双轴对称);4 张例外:D1506 水平条带、D1510 大开放洞窟、D15111 粗通道大室、D15112 S 形蛇道。",
        evidence="指纹锚定:D15011→D12011 Jinchon Palace Lv 2-W、D15021→D12021,sim=1.0。", sheet="D15xx.png"),
    "D20xx": dict(title="洞穴/沙漠 (4)", category="mixed",
        desc="D2011 大不规则腔室、D2012 蛇形走廊、D203 高密窄道迷宫;D202 沙漠荒野(浅坦地面 + 干河纹)。",
        evidence="D202 → 17 Lost Oasis 0.97;其余视觉。", sheet="D20xx.png"),
    "D40xx": dict(title="洞穴 + 菱形竞技场 (14)", category="dungeon",
        desc="D401–D406 不规则洞穴网 + 内部洞;D401_001–D406_001 六张对称菱形 boss 竞技场;D404_002 大倾斜厅。",
        evidence="锚定:D404→D404 Ant Cave East sim=1.0。", sheet="D40xx.png"),
    "D41xx": dict(title="洞穴/石构 (6)", category="dungeon",
        desc="D411–D413 天然洞穴(圆润叶状);D414–D416 斜向石构走廊 + 节点房。",
        evidence="视觉。", sheet="D41xx.png"),
    "D42xx": dict(title="洞穴/堡垒 (7)", category="dungeon",
        desc="D421 有机网 + 双斜纹、D422/D423 洞穴迷宫;D421_001/D422_001 实心菱形竞技场;D420_001 大单室、D422_002 尖角堡垒。",
        evidence="视觉。", sheet="D42xx.png"),
    "D43xx": dict(title="洞穴 + 菱形竞技场 (13)", category="dungeon",
        desc="D431–D436 有机洞穴网;D43x_001 六张菱形竞技场;D434_002 大矩形室。",
        evidence="视觉。", sheet="D43xx.png"),
    "D44xx": dict(title="洞穴 (6)", category="dungeon",
        desc="D441–D443 有机洞穴(D443 中央空洞);D444–D446 斜向宽通道。",
        evidence="视觉。", sheet="D44xx.png"),
    "D45xx": dict(title="洞穴 + 菱形竞技场 (4)", category="dungeon",
        desc="D451/D452 有机洞穴网;D451_001/D452_001 实心菱形竞技场(带左右入口凸块)。",
        evidence="视觉。", sheet="D45xx.png"),
    "D50xx": dict(title="洞穴系 (14)", category="dungeon",
        desc="D505 构造迷宫(同心嵌套矩形,寺院/塔式);D5061–D5069 九张近同小岩室(约 3×3 网格);D5071 大开放洞窟;D5072/D5074 直角石砌墓室;D5073 天然洞穴网。",
        evidence="视觉,疑沃玛神殿式多段地牢。", sheet="D50xx.png"),
    "D60xx": dict(title="沙漠系 (10)", category="wilderness",
        desc="全沙漠:D6004 岩石荒野、D6012 密集迷宫、D60131 沙丘盆地、D60133 环形坑、D60134 干谷、D6014 蛇形窄道、D6025 围墙要塞/城镇遗迹(最规整)。",
        evidence="指纹弱(0.4 噪声底)→ 2017 无对应;视觉归类。", sheet="D60xx.png"),
    "D61xx": dict(title="洞穴地牢 (8)", category="dungeon",
        desc="D614/D615/D617 更矩形(要塞/神庙腔);D617 对称菱形中央穹顶 + 侧室(boss 圣所);D613 甜甜圈大室;其余有机洞穴。",
        evidence="视觉。", sheet="D61xx.png"),
    "D70xx": dict(title="洞穴网 (1)", category="dungeon",
        desc="D701 中央节点 + 五条蜿蜒死端通道,纯有机。",
        evidence="视觉。", sheet="D70xx.png"),
    "D71xx": dict(title="石构迷宫 + boss 房集 (32)", category="dungeon",
        desc="D716 人造石构建筑(寺院/墓/要塞内部,网格铺地迷宫);D71601–71625 + D71650–71653 共 30 张单个椭圆天然土岩腔室(boss 房集);D717 更大不规则洞 → 石构入口 + 30 单人房。",
        evidence="视觉。", sheet="D71xx.png"),
    "D80xx": dict(title="蚂蚁洞 Ant Cave (11)", category="dungeon",
        desc="沙色隧道/腔室:D801 气泡腔室、D8031 巨矩形厅、D8032 大椭圆室、D804 对称十字、D805 分形巢、D806 星鱼形;D8041 水平条带例外(矿井/仓库式)。",
        evidence="锚定:D8001→D404 Ant Cave East sim=1.0。", sheet="D80xx.png"),
    "D81xx": dict(title="蚂蚁洞系 (13)", category="dungeon",
        desc="11 张同族沙色隧道(D8101–8104/D811 蜿蜒通道;D8121/D8131/D8132 大实体腔室);D8141/D8161 水平条带例外。",
        evidence="同 D80xx 家族(沙色 + 有机)。", sheet="D81xx.png"),
    "D82xx": dict(title="蚂蚁洞系 (13)", category="dungeon",
        desc="10 张同族沙色隧道/腔室(D8201–8204/D824/D825/D826 有机丝状;D8221/D8232 大实体);D8231/D8241/D8261 水平条带例外(条码状)。",
        evidence="同 D80xx 家族。", sheet="D82xx.png"),
    "D90xx": dict(title="幽灵船 Phantom Ship (23)", category="dungeon",
        desc="窄甲板走廊 + 小舱室 + 楼梯三角刻线,统一棕灰木色;D900 有机金棕块(入口);5 张无细节扁平船体;其余按层级树细分(D9021→D90211–24)。",
        evidence="锚定:D900→D1401 Phantom Ship Ent sim=1.0;D901→D1403、D9021→D1403、D903→D1405、D904→D1406 均 1.0。", sheet="D90xx.png"),
}

# per-map overrides: map_id -> (category, display_name, note)
OVERRIDES = {
    "0": ("town", "比奇城 Bichon Town", "城墙围合院落 + 河流;指纹→16_001 0.77(边缘巧合)"),
    "1": ("town", "失乐园 Lost Paradise", "滨海绿地 + 大片空地;无密集建筑"),
    "2": ("town", "潘夜村 Banya Village", "散村,数十小块建筑 + 小路"),
    "3": ("town", "沙巴克城 Sabuk Keep", "密集灰色建筑群 + 放射路网;指纹→D3903 0.72"),
    "4": ("town", "努玛村 Numa Village", "纯沙漠前哨;指纹与新 17 Lost Oasis sim=1.0(2017 重排号)"),
    "5": ("town", "沙漠土城 Desert Mud Fortress", "沙漠中的浅灰角状要塞;指纹→17 0.94"),
    "8": ("town", "南哨站 Southern Check Point", "绿岛/半岛 + 蓝海;指纹→D4003 0.86"),
    "81": ("wilderness", "失落地二层 Lost Land 2", "蓝底绿条,低可读;指纹→D3400 0.85"),
    "41": ("wilderness", "南部沙丘 Southern Dunes", "指纹→18 Arid Flats 0.999 / D4001"),
    "42": ("wilderness", "南部荒原 Southern Wastes", "指纹→D4001 1.0"),
    "43": ("wilderness", "南部沙丘 Southern Dunes", "指纹→D4000 1.0"),
    "44": ("wilderness", "南部荒原系", "指纹→18 1.0"),
    "6": ("wilderness", "西部荒野 Western Arids", "指纹→16 1.0"),
    "71": ("wilderness", "彼岸 Beyond Shore", "指纹→16_001 0.994"),
    "72": ("wilderness", "西海岸 Western Coast", "指纹→16_002/D4002 1.0"),
    "73": ("wilderness", "西部关隘 Western Pass", "指纹→16_003 1.0"),
    "74": ("wilderness", "西部关隘 Western Pass", "指纹→16_003 0.963(含小暗点)"),
    "76": ("wilderness", "西部关隘 Western Pass", "指纹→16_003 1.0"),
    "77": ("wilderness", "西部关隘 Western Pass", "指纹→16_003 1.0"),
    "78": ("wilderness", "西部关隘 Western Pass", "指纹→16_003 1.0"),
    "12": ("wilderness", "山道迷宫(失乐园外围)", "指纹→D3903 1.0;绿底棕道"),
    "121": ("wilderness", "山道迷宫", "指纹→D3902 1.0"),
    "122": ("wilderness", "山道迷宫", "指纹→D3905 1.0"),
    "123": ("wilderness", "山道迷宫", "指纹→D3906 1.0"),
    "124": ("wilderness", "山道迷宫", "指纹→D3906 1.0"),
    "125": ("wilderness", "山道迷宫", "指纹→D3906 1.0"),
    "01": ("wilderness", "滨海野外", "绿地 + 蓝湖;指纹→16_001 0.72"),
    "02": ("wilderness", "滨海/道路", "绿 + 棕干河道;指纹→D3903 0.70"),
    "0150": ("empty", "空图(全黑)", "无几何可读"),
    "0157": ("empty", "空图(全黑)", "无几何可读"),
    "D1101": ("dungeon", "潘夜神殿 Lv1 同构", "指纹→D1001 Banya Temple Lv 1,sim=1.0"),
    "D1102": ("dungeon", "潘夜神殿 Lv2 同构", "指纹→D1002 sim=1.0"),
    "D1115": ("dungeon", "潘夜神殿 boss 房", "嵌套同心菱形,极规整"),
    "D1201": ("dungeon", "石构/洞穴混合", "指纹→D2304 1.0(新版未命名)"),
    "D1215": ("dungeon", "建筑复合体", "矩形外边界 + 内部隔间"),
    "D15011": ("dungeon", "真天宫 Lv2-W 同构", "指纹→D12011 Jinchon Palace Lv 2-W,sim=1.0"),
    "D15021": ("dungeon", "真天宫 Lv2 同构", "指纹→D12021 sim=0.998"),
    "D1510": ("dungeon", "真天宫例外:大洞窟", "稀疏大开放空间,厚墙"),
    "D15112": ("dungeon", "真天宫例外:S 形蛇道", "粗蛇形中央走廊连接大室"),
    "D8001": ("dungeon", "蚂蚁洞东 Ant Cave East", "指纹→D404 Ant Cave East,sim=1.0"),
    "D900": ("dungeon", "幽灵船入口 Phantom Ship Ent", "指纹→D1401 Phantom Ship Ent,sim=1.0;有机金棕块"),
    "D901": ("dungeon", "幽灵船甲板", "指纹→D1403 sim=1.0"),
    "D904": ("dungeon", "幽灵船甲板", "指纹→D1406 sim=1.0"),
    "D202": ("wilderness", "沙漠荒野", "指纹→17 Lost Oasis 0.97;浅坦 + 干河纹"),
    "D404": ("dungeon", "蚂蚁洞东 Ant Cave East", "指纹→D404 自身 sim=1.0(旧=新同名)"),
    "D401_001": ("dungeon", "菱形 boss 竞技场", "对称菱形,无内部细节"),
    "D402_001": ("dungeon", "菱形 boss 竞技场", "对称菱形"),
    "D403_001": ("dungeon", "菱形 boss 竞技场", "对称菱形"),
    "D404_001": ("dungeon", "菱形 boss 竞技场", "对称菱形"),
    "D405_001": ("dungeon", "菱形 boss 竞技场", "对称菱形"),
    "D406_001": ("dungeon", "菱形 boss 竞技场", "对称菱形"),
    "D421_001": ("dungeon", "菱形竞技场", "实心菱形,尖朝上"),
    "D422_001": ("dungeon", "菱形竞技场", "实心菱形,与 D421_001 相同"),
    "D431_001": ("dungeon", "菱形竞技场", "与 D432–436_001 相同模板"),
    "D432_001": ("dungeon", "菱形竞技场", ""),
    "D433_001": ("dungeon", "菱形竞技场", ""),
    "D434_001": ("dungeon", "菱形竞技场", ""),
    "D435_001": ("dungeon", "菱形竞技场", ""),
    "D436_001": ("dungeon", "菱形竞技场", ""),
    "D451_001": ("dungeon", "菱形竞技场", "带左右入口凸块"),
    "D452_001": ("dungeon", "菱形竞技场", "带左右入口凸块,略宽"),
    "D617": ("dungeon", "boss 圣所", "对称菱形中央穹顶 + 侧室"),
    "D613": ("dungeon", "甜甜圈大室", "环形通道 + 中央实体"),
    "D716": ("dungeon", "石构网格迷宫", "寺院/墓/要塞内部,网格铺地"),
    "D717": ("dungeon", "大洞窟", "石构入口 + 不规则洞"),
    "D71601": ("dungeon", "椭圆 boss 单室", "模板房(共 25 张:D71601–71625)"),
    "D71650": ("dungeon", "椭圆 boss 单室", "模板房(共 4 张:D71650–71653)"),
    "D505": ("dungeon", "构造迷宫", "同心嵌套矩形,寺院/塔式"),
    "D5061": ("dungeon", "小岩室", "近同模板(共 9 张:D5061–5069,约 3×3 网格)"),
    "D5072": ("dungeon", "直角石砌墓室", ""),
    "D5074": ("dungeon", "直角石砌墓室", ""),
    "D6025": ("wilderness", "围墙要塞遗迹", "沙漠中最规整"),
    "D8041": ("dungeon", "水平条带(例外)", "非有机结构,矿井/仓库式"),
    "D8141": ("dungeon", "水平条带(例外)", "与 D8161 同模板"),
    "D8161": ("dungeon", "水平条带(例外)", ""),
    "D8231": ("dungeon", "水平条带(例外)", "厚横带 + 黑间隔"),
    "D8241": ("dungeon", "水平条带(例外)", "条码状细横带"),
    "D8261": ("dungeon", "水平条带(例外)", "条码状细横带"),
    "kt0001": ("special", "灰菱形网格室内", "唯一带等距网格铺地的;指纹→D1401 0.94 为小图巧合"),
    "kt0018": ("special", "十字形竞技场", "四臂对称,中央节点;kt00181 同"),
    "kt00181": ("special", "十字形竞技场", ""),
    "kt0012": ("special", "长廊大厅", "细长横条"),
    "kt0013": ("special", "长廊大厅", "细长横条"),
    "kt0016": ("special", "竖井", "高瘦锯齿轮廓;kt00161 近同"),
    "0_001": ("town", "比奇城左翼 Left Wing", "矩形房间/门洞/楼梯,米色地面"),
    "0_0011": ("town", "比奇城建筑内部", "指纹→D2107/D1305/D1206 0.84(同构室内)"),
    "0_002": ("town", "比奇城右翼 Right Wing", "指纹→11_001 0.835"),
    "0_0021": ("town", "比奇城建筑内部", "同 0_002"),
    "1_009": ("wilderness", "失乐园绿地", "other 系唯一绿色野外"),
    "1_020": ("dungeon", "隧道", "明显蜿蜒通道;指纹→11_003/0_000 0.987"),
    "1_008": ("empty", "空图(全黑)", ""),
    "1_012": ("empty", "空图(全黑)", ""),
    "4_001": ("town", "努玛村建筑内部", "白色/米色块状房间 + 门洞"),
    "4_003": ("town", "努玛村建筑内部", ""),
    "4_005": ("empty", "空图(全黑)", ""),
    "5_0011": ("town", "沙漠土城建筑内部", "室内网格/家具"),
    "5_005": ("town", "沙漠土城建筑内部", ""),
    "d501": ("town", "城镇建筑内部", "刚性矩形平面 + 柜台/桌椅"),
    "d502": ("town", "城镇建筑内部", ""),
    "d503": ("town", "城镇建筑内部", ""),
    "d504": ("town", "城镇建筑内部", ""),
    "d511": ("town", "城镇建筑内部", ""),
    "d512": ("town", "城镇建筑内部", ""),
    "d513": ("town", "城镇建筑内部", ""),
    "d514": ("town", "城镇建筑内部", ""),
    "d515": ("wilderness", "灰菱形空地/大厅", "无门无家具,介于野外与大厅"),
    "02_001": ("empty", "空图(全黑)", "渲染 0 内容(88×108 极小图)"),
    "01_003": ("empty", "空图(全黑)", ""),
    "0_0031": ("empty", "空图(全黑)", ""),
    "0_0032": ("empty", "空图(全黑)", ""),
    "0_0033": ("empty", "空图(全黑)", ""),
    "1_008": ("empty", "空图(全黑)", ""),
    "1_012": ("empty", "空图(全黑)", ""),
    "4_005": ("empty", "空图(全黑)", ""),
    "5_002": ("wilderness", "不规则棕块", "介于野外与室内之间"),
    "5_003": ("wilderness", "不规则棕块", "介于野外与室内之间"),
    "5_004": ("wilderness", "不规则棕块", "介于野外与室内之间"),
    "B125_001": ("empty", "空图(全黑)", ""),
    "B136_001": ("empty", "空图(全黑)", ""),
    "B136_002": ("empty", "空图(全黑)", ""),
    "B102_001": ("wilderness", "沙漠野外", "顶部绿边"),
    "B103_001": ("wilderness", "沙漠野外", "顶部绿边"),
    "B106_001": ("wilderness", "荒野斑块", ""),
    "B106_002": ("wilderness", "荒野斑块", ""),
    "B106_003": ("wilderness", "荒野斑块", ""),
    "B115_001": ("wilderness", "荒野斑块", ""),
    "B118_001": ("wilderness", "荒野斑块", ""),
    "B132_001": ("wilderness", "荒野斑块", ""),
    "B134_001": ("wilderness", "荒野斑块", ""),
    "B010": ("town", "构造要塞/城镇", "B 系最规整"),
    "B011": ("town", "构造要塞/城镇", ""),
    "B139_001": ("special", "深灰菱形堡垒", ""),
    "B140_001": ("special", "深灰菱形堡垒", ""),
    "d802": ("dungeon", "蚂蚁洞系迷宫", "指纹→D404 0.732"),
    "d803": ("dungeon", "蚂蚁洞系迷宫", "指纹→D403 0.728"),
    "d807": ("dungeon", "蚂蚁洞系迷宫", "指纹→D403 0.714"),
    "d817": ("dungeon", "蚂蚁洞系迷宫", "指纹→D403 0.714"),
    "d822": ("dungeon", "蚂蚁洞系迷宫", "指纹→D404 0.732"),
    "d828": ("dungeon", "蚂蚁洞系迷宫", "指纹→D403 0.717"),
    "DM001": ("empty", "空图(全黑)", ""),
    "DM011": ("empty", "空图(全黑)", ""),
    "DM002": ("dungeon", "洞穴带", "金黄/赭碎片水平带"),
    "E001": ("special", "事件迷宫", "细道缠绕"),
    "E404": ("special", "事件竞技场", "同心菱形,高度对称"),
    "E602": ("special", "事件竞技场", "同心方形/菱形"),
    "E605": ("special", "事件竞技场", "厚 V 形单块"),
    "E002_001": ("special", "事件大室", "近实心矩形 + 小黑缺"),
    "E402_001": ("special", "事件大室", "近实心矩形"),
    "B010": ("town", "构造要塞/城镇", "B 系最规整"),
    "B011": ("town", "构造要塞/城镇", ""),
    "B139_001": ("special", "深灰菱形堡垒", ""),
    "B140_001": ("special", "深灰菱形堡垒", ""),
}

CAT_LABEL = {
    "town": ("城镇", "#2e7d32"),
    "wilderness": ("野外", "#1565c0"),
    "dungeon": ("洞穴地牢", "#8d5a1f"),
    "special": ("特殊/事件", "#6a1b9a"),
    "empty": ("空图", "#616161"),
    "mixed": ("混合", "#37474f"),
}

def family_of(mid):
    if mid.startswith("kt"): return "kt"
    if mid.startswith("E"): return "E"
    if mid.startswith("DM"): return "DM"
    if mid.startswith("B"): return "B"
    if re.fullmatch(r"[0-9]+", mid): return "num"
    if mid[0].isdigit() and "_" in mid: return "other"
    if mid.startswith("d"): return "other"
    if mid.startswith("D"):
        m = re.match(r"D(\d{2})", mid)
        if m:
            fam = "D" + m.group(1) + "xx"
            if fam in FAMILIES: return fam
    return "other"

# Objective: per-map class for the mixed families (num / other / B), from visual audits.
# Categories: town / wilderness / dungeon / special / empty
def class_of(mid):
    if mid in OVERRIDES:
        return OVERRIDES[mid][0]
    if mid.startswith("B"):
        return "dungeon"          # default: gray cave patches; exceptions in OVERRIDES
    if re.fullmatch(r"[0-9]+", mid):
        return "wilderness"       # num: 01/02/6/12/41-44/71-78/81/121-125; towns/empties in OVERRIDES
    if mid[0].isdigit() and "_" in mid:
        if mid.startswith("1_"): return "wilderness"   # 1_009/1_010/1_011/1_013-023; 1_008/1_012/1_020 overridden
        if mid.startswith("0_"): return "town"         # 0_001..0_003; 0_0031-33 overridden empty
        if mid.startswith("4_"): return "town"         # 4_001..4_004; 4_005 overridden empty
        if mid.startswith("5_"): return "town"         # 5_0011/12/13/005/006; 5_002-004 overridden wilderness
        if mid.startswith("01_") or mid.startswith("02_"): return "wilderness"  # 01_001/002, 02_002-014; empties overridden
    if mid.startswith("d"):
        if mid.startswith("d6"): return "dungeon"      # d60011..d611 cave networks
        if mid.startswith("d71"): return "dungeon"     # d7101..d714 caves
        if mid.startswith("d5"): return "town"         # d501..d504/d511..d514 interiors; d515 overridden
        if mid.startswith("d102"): return "dungeon"    # d1021, d10231
        if mid == "d043": return "dungeon"
        if mid.startswith("d8"):
            if len(mid) == 4: return "dungeon"         # d802,d803,d807,... patterned maze floors
            if mid in ("d8033", "d8051", "d8122", "d8271"): return "dungeon"
            return "wilderness"                        # d8021,d8022,d8061,d8062,... desert slopes
    return "wilderness"

def category_of(mid):
    if mid in OVERRIDES:
        return OVERRIDES[mid][0]
    fam = FAMILIES[family_of(mid)]
    if fam["category"] != "mixed": return fam["category"]
    return class_of(mid)

def note_of(mid):
    if mid in OVERRIDES:
        return OVERRIDES[mid][2]
    return ""

def name_of(mid):
    if mid in OVERRIDES:
        return OVERRIDES[mid][1]
    return ""

# ---- gather maps ----
maps = {}
for p in sorted(glob.glob(os.path.join(VIEWS, "*.png"))):
    mid = os.path.basename(p)[:-4]
    fam = family_of(mid)
    maps[mid] = dict(
        id=mid, family=fam,
        cat=category_of(mid),
        cat_label=CAT_LABEL[category_of(mid)][0],
        cat_color=CAT_LABEL[category_of(mid)][1],
        name=name_of(mid),
        note=note_of(mid),
        thumb="data:image/jpeg;base64," + thumb_b64(p),
        best=BEST.get(mid, []),
    )

# name lookup for best matches
def best_names(best):
    out = []
    for sim, name in best[:3]:
        nm = NAMES.get(name, name)
        out.append(f"{name} ({nm}, {sim:.2f})")
    return "; ".join(out)

# ---- families with maps ----
fam_maps = {}
for mid, info in maps.items():
    fam_maps.setdefault(info["family"], []).append(info)

for fam in FAMILIES:
    fam_maps.setdefault(fam, [])

order = ["num", "other", "kt", "E", "DM", "B",
         "D00xx", "D01xx", "D02xx", "D03xx", "D04xx", "D05xx",
         "D10xx", "D11xx", "D12xx", "D13xx", "D14xx", "D15xx",
         "D20xx", "D40xx", "D41xx", "D42xx", "D43xx", "D44xx", "D45xx",
         "D50xx", "D60xx", "D61xx", "D70xx", "D71xx",
         "D80xx", "D81xx", "D82xx", "D90xx"]

# ---- stats ----
from collections import Counter
cat_counter = Counter(m["cat"] for m in maps.values())

# ---- render ----
def esc(s):
    return (s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
             .replace('"', "&quot;"))

css = """
:root{--bg:#14181d;--panel:#1d242c;--panel2:#242e38;--line:#33404d;--text:#d8e0e8;--muted:#8fa0b0;--gold:#e8c06a;--accent:#7fb3e0;}
*{box-sizing:border-box;margin:0;padding:0;}
body{background:var(--bg);color:var(--text);font-family:'Noto Sans SC','PingFang SC','Microsoft YaHei',system-ui,sans-serif;line-height:1.55;}
header{background:linear-gradient(160deg,#1a2330 0%,#10151c 60%,#0b0e12 100%);border-bottom:1px solid var(--line);padding:34px 28px 26px;position:sticky;top:0;z-index:50;}
header h1{font-size:26px;color:var(--gold);letter-spacing:1px;}
header .sub{color:var(--muted);font-size:13px;margin-top:6px;}
header .meta{color:var(--muted);font-size:12px;margin-top:4px;font-family:ui-monospace,Consolas,monospace;}
nav{display:flex;flex-wrap:wrap;gap:8px;margin-top:14px;}
nav a{color:var(--accent);text-decoration:none;font-size:12px;border:1px solid var(--line);padding:3px 10px;border-radius:14px;background:#161d25;}
nav a:hover{background:#22303e;border-color:var(--accent);}
main{max-width:1240px;margin:0 auto;padding:22px 20px 80px;}
h2{font-size:20px;color:var(--gold);margin:34px 0 14px;border-bottom:1px solid var(--line);padding-bottom:8px;}
h2 .n{color:var(--muted);font-size:13px;font-weight:normal;}
.statrow{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:10px;}
.stat{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:14px 16px;}
.stat b{font-size:24px;display:block;}
.stat span{font-size:12px;color:var(--muted);}
.stat .dot{display:inline-block;width:9px;height:9px;border-radius:50%;margin-right:6px;}
.method{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:16px 18px;font-size:14px;color:#c6d2dc;}
.method li{margin:6px 0 6px 18px;}
.method code{background:#0d1117;border:1px solid var(--line);padding:1px 5px;border-radius:4px;font-size:12px;color:var(--gold);}
.famindex{display:grid;grid-template-columns:repeat(auto-fill,minmax(270px,1fr));gap:12px;}
.famcard{background:var(--panel);border:1px solid var(--line);border-radius:10px;overflow:hidden;cursor:pointer;transition:transform .12s,border-color .12s;}
.famcard:hover{transform:translateY(-2px);border-color:var(--accent);}
.famcard img{width:100%;display:block;background:#000;}
.famcard .fc{ padding:9px 12px;}
.famcard .ft{font-size:14px;font-weight:600;}
.famcard .fm{font-size:12px;color:var(--muted);margin-top:3px;}
.famcard .badge{display:inline-block;font-size:11px;padding:1px 8px;border-radius:10px;margin-left:6px;color:#fff;vertical-align:1px;}
section.fam{margin:26px 0 6px;scroll-margin-top:150px;}
section.fam>h3{font-size:17px;color:var(--gold);}
section.fam>h3 .cnt{color:var(--muted);font-size:13px;font-weight:normal;margin-left:8px;}
.fambody{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:16px;margin-top:10px;}
.famdesc{font-size:14px;color:#c6d2dc;}
.famev{font-size:12.5px;color:var(--muted);margin-top:8px;border-left:3px solid var(--accent);padding-left:10px;}
.sheet{width:100%;border-radius:8px;margin:12px 0 4px;background:#000;border:1px solid var(--line);}
.sheetcap{font-size:11px;color:var(--muted);text-align:center;margin-bottom:10px;}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(170px,1fr));gap:10px;margin-top:12px;}
.cell{background:var(--panel2);border:1px solid var(--line);border-radius:8px;padding:6px;transition:transform .12s;position:relative;}
.cell:hover{transform:scale(1.35);z-index:20;box-shadow:0 6px 18px rgba(0,0,0,.6);border-color:var(--gold);}
.cell img{width:100%;display:block;border-radius:5px;background:#000;}
.cell .cid{font-size:11.5px;font-family:ui-monospace,Consolas,monospace;color:var(--gold);margin-top:5px;word-break:break-all;}
.cell .cn{font-size:11px;color:var(--accent);margin-top:1px;}
.cell .note{font-size:10.5px;color:var(--muted);margin-top:2px;line-height:1.35;}
.cell .cb{display:inline-block;font-size:9.5px;padding:0 6px;border-radius:8px;color:#fff;margin-top:4px;}
.best{font-size:10.5px;color:#9fb4c8;margin-top:3px;line-height:1.3;font-family:ui-monospace,Consolas,monospace;}
footer{color:var(--muted);font-size:12px;text-align:center;padding:24px;border-top:1px solid var(--line);}
details.tbl{border:1px solid var(--line);border-radius:10px;background:var(--panel);}
details.tbl summary{padding:12px 16px;cursor:pointer;color:var(--gold);font-size:14px;}
.tblwrap{overflow-x:auto;max-height:520px;overflow-y:auto;}
table{width:100%;border-collapse:collapse;font-size:12.5px;}
th{position:sticky;top:0;background:#182029;color:var(--gold);text-align:left;padding:8px 10px;border-bottom:1px solid var(--line);font-weight:600;}
td{padding:6px 10px;border-bottom:1px solid #222c36;vertical-align:top;}
tr:hover td{background:#1c2530;}
td.mid{font-family:ui-monospace,Consolas,monospace;color:var(--gold);white-space:nowrap;}
"""

js = """
document.querySelectorAll('.famcard').forEach(c=>{
  c.addEventListener('click',()=>{
    const t=c.dataset.target; if(t) document.getElementById(t).scrollIntoView({behavior:'smooth'});
  });
});
"""

def render():
    parts = []
    parts.append(f"""<!DOCTYPE html>
<html lang="zh-CN"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>mir3ei 2003 韩服 Mir3 — 566 张地图全图鉴</title>
<style>{css}</style></head><body>""")

    # header
    parts.append(f"""
<header>
<h1>mir3ei — 2003 韩服 Mir3 客户端地图全图鉴</h1>
<div class="sub">566 张地图 · 8 座城镇 · 野外 · 洞穴地牢 · 特殊事件房 — 逐张渲染图册 + 指纹锚定 + 视觉分类</div>
<div class="meta">来源: /home/tetsuya/NAS/TMP/mir3ei (Mir3.exe 2003-05-14) · Map/ 目录 566 个 .map · 渲染: 8 个地面库纯 back 层 24×16/格 · 锚定库: Zircon 2017 中文版 244 张地图</div>
<nav>
<a href="#overview">总览</a><a href="#method">方法</a><a href="#index">家族索引</a><a href="#gallery">图册</a><a href="#table">逐张明细表</a>
</nav>
</header>
<main>""")

    # overview stats
    c = cat_counter
    anchored = sum(1 for m in maps.values() if m["best"] and m["best"][0][0] >= 0.85)
    parts.append(f"""
<section id="overview">
<h2>总览 <span class="n">分类统计</span></h2>
<div class="statrow">
<div class="stat"><b>{c.get('town',0)}</b><span><span class="dot" style="background:{CAT_LABEL['town'][1]}"></span>城镇 / 建筑内部</span></div>
<div class="stat"><b>{c.get('wilderness',0)}</b><span><span class="dot" style="background:{CAT_LABEL['wilderness'][1]}"></span>野外</span></div>
<div class="stat"><b>{c.get('dungeon',0)}</b><span><span class="dot" style="background:{CAT_LABEL['dungeon'][1]}"></span>洞穴地牢</span></div>
<div class="stat"><b>{c.get('special',0)}</b><span><span class="dot" style="background:{CAT_LABEL['special'][1]}"></span>特殊/事件</span></div>
<div class="stat"><b>{c.get('empty',0)}</b><span><span class="dot" style="background:{CAT_LABEL['empty'][1]}"></span>空图</span></div>
<div class="stat"><b>{anchored}</b><span>指纹锚定(≥0.85, 2017 版)</span></div>
<div class="stat"><b>{len(maps)}</b><span>地图总数</span></div>
</div>
</section>""")

    # method
    parts.append(f"""
<section id="method">
<h2>方法与证据链</h2>
<div class="method">
<ol>
<li><b>逐张渲染</b> — 566 个 .map 全部解析,以 8 个地面图库(WIL 1.0: Tilesc/Tiles30c/Tiles5c/SmTilesc/Housesc/Cliffsc/Dungeonsc/Innersc 等)纯 back 层 24×16 px/格渲染,共 19313 个唯一帧,0 错误。</li>
<li><b>指纹锚定</b> — 每张旧地图统计瓦片使用分布,与 2017 中文版 244 张地图比对(sim 为逐瓦片分布余弦/重合度)。<b>sim=1.0 锚点</b>: D1101→D1001 潘夜神殿、D15011→D12011 真天宫、D8001→D404 蚂蚁洞、D900→D1401 幽灵船、D12xx 全系、41–44→南部沙漠、71–78→西部关隘、12/121–125→D390x。sim 0.40 为噪声底(未识别家族 = 2017 版已精简对应地图)。</li>
<li><b>视觉鉴定</b> — 34 张家族接片图(566 张全覆盖,缩略 160×90、8 列、黄字点名)逐张人工鉴定:城镇看建筑/街道,野外看连续色块,洞穴看迷宫/腔室/菱形竞技场模板。</li>
<li><b>编号惯例</b> — 城镇 0–8 沿用经典 Mir3 编号并与 2017 版同名(0 比奇、1 失乐园、2 潘夜村、3 沙巴克城、4 努玛村、5 沙漠土城、8 南哨站)。</li>
<li><b>渲染器正确性闭环</b> — 旧 WIL 渲染与新版 Zl 渲染 A/B 逐像素对比:同布局同画质,差异仅为 DXT5 有损量化 vs RLE565 的噪声(MAE 14–45,exact 像素 0.32–0.71)。</li>
</ol>
</div>
</section>""")

    # index
    parts.append(f"""
<section id="index">
<h2>家族索引 <span class="n">34 家族 · 点击跳转</span></h2>
<div class="famindex">""")
    for fam in order:
        f = FAMILIES[fam]
        fm = fam_maps.get(fam, [])
        cat = f["category"]
        cl, cc = CAT_LABEL[cat]
        sheet = f.get("sheet")
        if sheet and os.path.exists(os.path.join(CONTACT, sheet)):
            sb = sheet_b64(os.path.join(CONTACT, sheet), maxw=420, q=72)
            img = f'<img src="data:image/jpeg;base64,{sb}" alt="{esc(fam)}">'
        else:
            img = ""
        parts.append(f"""
<div class="famcard" data-target="fam-{fam}">
{img}
<div class="fc"><div class="ft">{esc(f['title'])} <span class="badge" style="background:{cc}">{cl}</span></div>
<div class="fm">{len(fm)} 张</div></div>
</div>""")
    parts.append("</div></section>")

    # gallery
    parts.append('<section id="gallery"><h2>图册 <span class="n">566 张 · 悬停放大</span></h2>')
    for fam in order:
        f = FAMILIES[fam]
        fm = sorted(fam_maps.get(fam, []), key=lambda m: m["id"])
        cat = f["category"]
        cl, cc = CAT_LABEL[cat]
        parts.append(f"""
<section class="fam" id="fam-{fam}">
<h3>{esc(f['title'])} <span class="cnt">{len(fm)} 张 · {cl}</span></h3>
<div class="fambody">
<div class="famdesc">{esc(f['desc'])}</div>
<div class="famev">证据: {esc(f['evidence'])}</div>""")
        sheet = f.get("sheet")
        if sheet and os.path.exists(os.path.join(CONTACT, sheet)):
            sb = sheet_b64(os.path.join(CONTACT, sheet))
            parts.append(f'<img class="sheet" src="data:image/jpeg;base64,{sb}" alt="家族接片图">'
                         f'<div class="sheetcap">家族接片图(全部 {len(fm)} 张,8 列,黄字点名)</div>')
        parts.append('<div class="grid">')
        for m in fm:
            name = esc(m["name"])
            note = esc(m["note"])
            best = esc(best_names(m["best"]))
            parts.append(f"""
<div class="cell">
<img loading="lazy" src="{m['thumb']}" alt="{esc(m['id'])}">
<div class="cid">{esc(m['id'])}</div>
{f'<div class="cn">{name}</div>' if name else ''}
{f'<div class="note">{note}</div>' if note else ''}
<span class="cb" style="background:{m['cat_color']}">{m['cat_label']}</span>
{f'<div class="best">指纹: {best}</div>' if m['best'] and m['best'][0][0] >= 0.7 else ''}
</div>""")
        parts.append("</div></div></section>")

    parts.append("</section>")

    # full table
    rows = []
    for m in sorted(maps.values(), key=lambda x: x["id"]):
        best = best_names(m["best"])
        rows.append(f"""<tr><td class="mid">{esc(m['id'])}</td><td>{esc(m['family'])}</td>
<td><span style="color:{m['cat_color']}">{m['cat_label']}</span></td>
<td>{esc(m['name']) if m['name'] else '—'}</td>
<td>{esc(m['note']) if m['note'] else '—'}</td>
<td class="best">{esc(best) if best else '—'}</td></tr>""")
    parts.append(f"""
<section id="table">
<h2>逐张明细表 <span class="n">566 行</span></h2>
<details class="tbl"><summary>展开 566 张完整明细(文件名 / 家族 / 分类 / 命名 / 注记 / 指纹 top-3)</summary>
<div class="tblwrap"><table>
<thead><tr><th>地图</th><th>家族</th><th>分类</th><th>命名</th><th>注记</th><th>指纹 top-3</th></tr></thead>
<tbody>{''.join(rows)}</tbody></table></div></details>
</section>""")

    parts.append(f"""
</main>
<footer>mir3ei_map_catalog.html · 生成于 2026-08-09 · 生成脚本 build_report.py · 数据 data/ · 接片 contact/ · 全尺寸渲染 views/ · 渲染脚本 tools/mir3ei_render.py
<br>结论: 566 张 = 8 城镇/据点 + ~80 野外 + ~440 洞穴地牢 + ~50 特殊/事件 + 少量空图;6 系洞穴经指纹锚定到 2017 中文版同名地图(潘夜神殿/真天宫/蚂蚁洞/幽灵船),其余为 2003 版独有或已精简地图。</footer>
<script>{js}</script>
</body></html>""")

    html = "".join(parts)
    out = os.path.join(ROOT, "mir3ei_map_catalog.html")
    open(out, "w", encoding="utf-8").write(html)
    print("wrote", out, f"{os.path.getsize(out)/1e6:.1f} MB")
    print("maps:", len(maps), "families:", len(order))

render()
