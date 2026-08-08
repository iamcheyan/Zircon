# 传奇 EI 2.0 资料归档

> 归档日期: 2026-08-07 | 用途: 参考存档(网页正文/图片/本地数据库 dump)

## 目录结构
```
ei2-research/
├── README.md          本索引
├── web/               网页正文存档 (07 个 md)
├── images/            图片素材
│   ├── ei2cc/         ei2.cc 复刻版截图 (18M, 含 1.wav)
│   ├── iamcheyan/     iamcheyan 复刻项目素材 (8.9M, banner/截图/二维码)
│   ├── 17173-map/     17173 传奇3 地图图 (17 张 jpg, 含世界大地图)
│   ├── sdo-map/       SDO 西沙地下城市 1-4 层图
│   ├── sdo-weapon/    SDO 西沙武器图 (沙漠流星刀/破山剑/沙漠魔刃/天神法杖/沙漠封魔剑/泰伦拂尘)
│   ├── sdo-item/      SDO 西沙防具图 (虎面头盔/玄冥靴/天问腰带)
│   └── sdo-mob/       SDO 西沙怪物图 (变异蜥系/魔石系/守护神/地天灭王)
└── data/              本地 System.db dump (SystemDbProbe 生成, 156 个 md)
```

## 网页存档清单
| 文件 | 来源 | 内容 |
|---|---|---|
| 01-ei2cc-复刻版官网首页.md | http://www.ei2.cc/ | EI 2.0 身份/历史,DS-BLUE 复刻 |
| 02-ei2cc-地图攻略.md | http://www.ei2.cc/quest.html | 幽灵船开门时间、BOSS 刷新表、13 张地图攻略 |
| 03-17173-传奇3地图专区.md | https://mir3.17173.com/map/map.htm | 官方地图全集列表(18 张地图) |
| 04-盛大-西沙新版本公告.md | actmir3.web.sdo.com | 西沙地图/BOSS/装备/强化/阵法/怪物/副本全文 |
| 05-17173-西沙版本报道.md | mir3.17173.com 2024-09-04 | 寻龙探宝区西沙版本公告 |
| 06-iamcheyan-恶魔的幻影复刻项目.md | https://iamcheyan.com/app/mir/ | GeeM2/V8M2 复刻、假人系统、武器元素限制 |
| 07-lomcn-MIR3源码帖.md | lomcn.net 帖子 | 「Mir3 2010 源码」= EI 2.0 老服务端确认 |

## 数据 dump 说明
- 生成工具: `Tools/SystemDbProbe --dump`
- 数据源: `/tmp/zircon-server/Database/` (Zircon 服务端 System.db)
- 内容: 244 地图 / 1078 物品 / 174 魔法 / 309 怪物 及全部关联表
- 重新生成: `dotnet run --project Tools/SystemDbProbe -- /tmp/zircon-server/Database/ --dump docs/database`

## 关键结论速览(详见整理文档 docs/notes/22-传奇EI2.0资料整理-地图装备技能阶段.md)
- 本地数据库地图/装备/技能 = 传奇3 后期全量(含 2021 西沙)+ 私服原创扩展
- EI 2.0 原生内容(真天宫/幽灵船/诺马/潘夜等)全部覆盖
