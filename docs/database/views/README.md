<!-- 由 Tools/SystemDbProbe 自动生成，请勿手改。重新生成: dotnet run --project Tools/SystemDbProbe -- --view docs/database/views -->

# 游戏数据总览（玩家视图）

> 把 System.db 的 1078 件物品 / 309 种怪物 / 174 个技能 / 244 张地图 / 34 个任务 / 125 个 NPC 整理成「人看的」分类视图。
> 原始逐字段数据保留在 [../data](../data)（说明见 [../README.md](../README.md)）。

## 板块

| 板块 | 文件 | 内容 |
|---|---|---|
| 职业技能 | [skills.md](skills.md) | 四职业全部技能：威力 / 耗蓝 / 等级门槛 / 说明 |
| 怪物图鉴 | [monsters.md](monsters.md) | Boss 与等级分区：属性 / 刷新地图 / 掉落 |
| 物品 · 武器 | [items/weapons.md](items/weapons.md) | 全部武器 |
| 物品 · 防具 | [items/armour.md](items/armour.md) | 护甲 / 头盔 / 鞋子 / 盾牌 / 时装 |
| 物品 · 饰品 | [items/jewellery.md](items/jewellery.md) | 项链 / 戒指 / 手镯 / 护身符 |
| 物品 · 消耗品 | [items/consumables.md](items/consumables.md) | 药水 / 技能书 / 卷轴 / 肉类 / 钓鱼用品 |
| 物品 · 材料 | [items/materials.md](items/materials.md) | 矿石 / 宝石 / 货币 / 礼包等 |
| 地图 | [maps.md](maps.md) | 等级范围 / 环境 / 怪物分布 |
| 任务 | [quests.md](quests.md) | 接取 / 目标 / 奖励 |
| NPC | [npcs.md](npcs.md) | 位置 / 功能 / 关联任务 |

## 阅读约定

- 职业：战 = 战士，法 = 法师，道 = 道士，刺 = 刺客；「全」= 全职业
- 属性：物攻 = 物理攻击，魔攻 = 魔法攻击，道术 = 道术攻击，物防 / 魔防 = 物理 / 魔法防御；完整属性字典见 [../data/stats.md](../data/stats.md)
- 掉落「1/30」表示三十分之一概率；「组N」为不同刷新点的掉落组
- 图标 / 头像数字为客户端图片资源编号
