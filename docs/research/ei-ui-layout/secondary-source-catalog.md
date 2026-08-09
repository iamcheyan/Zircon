# EI 3.0 UI 反编译：早期 Mir3 源码交叉证据目录

> 本文件不是 EI 3.0 的最终坐标表。数据来自公开的早期 Mir3 C++ 客户端源码，作为第二证据源，用来给 `Mir3.exe` 的反汇编结果命名、分组和提出待验证假设。最终结论必须回到 `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Mir3.exe` 和其 WIL/WIX 资源。

## 来源

- 仓库：`https://github.com/tensafe/LegendOfMir3_Src`
- 本地副本：`/tmp/legendofmir3-src`
- 关键目录：`LegendOfMir3_Client/GameProcess/`
- 资源加载名：`Data/gameinter.wil`、`Data/interfacec2.wil`

证据等级：`secondary-source`。它可以帮助理解 EI 二进制中的类和字段，但不能单独证明 EI 版本的坐标完全相同。

## 主 HUD / 底部操作栏

来源：`GameProcess/Interface.cpp`，初始化段约 174–227 行。

| 对象 | WIL 帧/参数 | 坐标表达式 | 语义假设 | 状态 |
|---|---:|---:|---|---|
| 主底板 | 50 | `x=0, y=600-imageHeight` | 底部锚定，实际宽高取 Frame 50 | 待 EI 二进制验证 |
| 交换窗口按钮 | 80/81 | `main.left+204, main.top+0` | 普通/按下或悬停状态 | 待验证 |
| 小地图按钮 | 82/83 | `main.left+228, main.top+0` | 普通/按下或悬停状态 | 待验证 |
| 武功/技能入口 | 84/85 | `main.left+252, main.top+0` | 入口按钮 | 待验证 |
| 退出 | 90/91 | `main.left+161, main.top+65` | 左侧按钮 | 待验证 |
| 登出 | 92/93 | `main.left+161, main.top+101` | 左侧按钮 | 待验证 |
| 组队 | 94/95 | `main.left+616, main.top+66` | 右侧罗盘按钮 | 待验证 |
| 行会 | 96/97 | `main.left+616, main.top+102` | 右侧罗盘按钮 | 待验证 |
| 腰带翻页 | 52/53 | `main.left+397, main.top+13` | 上/下或交替状态 | 待验证 |
| 技能窗口 | 100/101 | `main.left+703, main.top+34` | 罗盘按钮 | 待验证 |
| 聊天窗口 | 102/103 | `main.left+718, main.top+50` | 罗盘按钮 | 待验证 |
| 任务窗口 | 104/105 | `main.left+718, main.top+88` | 罗盘按钮 | 待验证 |
| 选项 | 106/107 | `main.left+703, main.top+103` | 罗盘按钮 | 待验证 |
| 组队窗口 | 108/109 | `main.left+664, main.top+104` | 罗盘按钮 | 待验证 |
| 人物/状态窗口 | 110/111 | `main.left+648, main.top+88` | 罗盘按钮 | 待验证 |
| 背包窗口 | 112/113 | `main.left+648, main.top+50` | 罗盘按钮 | 待验证 |
| 商店窗口 | 114/115 | `main.left+665, main.top+34` | 罗盘按钮 | 待验证 |

## 主 HUD 内部数据绘制

来源：`GameProcess/Interface.cpp` 的主界面绘制函数。这里的 `main.left/top` 依赖底板 Frame 50 的真实尺寸，不能直接把相对坐标当成屏幕绝对坐标。

| 元素 | WIL 帧 | 相对位置 | 说明 |
|---|---:|---:|---|
| HP 红球填充 | 60 | `main.left+46, main.top+34` | 按当前 HP 裁切源图后绘制 |
| MP 蓝球填充 | 61 | `main.left+104, main.top+34` | 按当前 MP 裁切源图后绘制 |
| 经验条 | 63 | `main.left+76, main.top+35` | 按经验比例裁切 |
| 重量条 | 67 | `main.left+211, main.top+35` | 早期源码存在，需核对 EI 的 Frame 67 |
| 等级文本 | — | `Rect(693,528,709,544)` | 直接绘制文字的目标区 |
| 地图名/坐标文本 | — | `Rect(219,584,379,599)` | 目标区 |
| AC/防御类文本 | — | `Rect(422,584,483,599)` | 目标区 |
| 另一项属性文本 | — | `Rect(520,584,581,599)` | 目标区 |
| 职业图标 | 64/65/66 | `main.left+82, main.top+71` | 不同职业使用不同 Frame |

## 窗口资源编号

来源：`LegendOfMir3_Client/Define.h` 的 `_WNDIMGIDX_*` 定义。编号需要在 EI 的 `GameInter.wil` 中逐一核验尺寸和内容。

| 窗口 | Frame 候选 |
|---|---:|
| 主 HUD | 50 |
| 腰带 | 51 |
| 人物装备/物品设置 | 200 |
| 人物状态 | 201 |
| 技能设置 | 202 |
| 背包 | 250 |
| 交易 | 251 |
| 商店/仓库 | 253 |
| 行会掌柜 | 600 |
| 行会 | 169（源码使用 interfacec2） |
| 组队 | 900 |
| 组队弹窗 | 145（源码使用 interfacec2） |
| 聊天弹窗 | 350 |
| NPC 对话 | 300 |
| 任务 | 700 |
| 选项 | 750 |
| 马匹 | 850 |
| 消息框 | 254/255 |

## 人物状态 / 装备栏局部命中区

来源：`GameProcess/StatusWnd.cpp`。这些是窗口局部坐标，不是 800×600 屏幕坐标；窗口左上角必须先由 EI 的窗口创建与移动逻辑确认。

| 装备槽 | 局部 Rect |
|---|---|
| 项链 | `(187,70)-(190+CELL_W,70+CELL_H)` |
| 右手/护符类槽 | `(37,264)-(37+CELL_W,264+CELL_H)` |
| 护符 | `(74,264)-(74+CELL_W,264+CELL_H)` |
| 左腕 | `(37,186)-(37+CELL_W,186+CELL_H)` |
| 右腕 | `(185,186)-(185+CELL_W,186+CELL_H)` |
| 左戒指 | `(37,227)-(37+CELL_W,227+CELL_H)` |
| 右戒指 | `(185,227)-(185+CELL_W,227+CELL_H)` |
| 头盔 | `(104,71)-(143,104)` |
| 衣服 | `(96,114)-(146,204)` |
| 武器 | `(48,70)-(91,154)` |

技能列表局部命中区：

```text
(39,77)-(74,112)
(39,115)-(74,149)
(39,152)-(74,186)
(39,188)-(74,223)
(39,225)-(74,260)
```

## 背包窗口局部结构

来源：`GameProcess/InventoryWnd.cpp` 和 `Define.h`。

- 单格尺寸候选：`38×38`
- 关闭按钮：Frame `280/281`，位置 `startX+255, startY+291`
- 滚动条：Frame `270`，局部参数约 `x=10, y=218`
- 背包格子由 `_INVEN_CELL_XSTART`、`_INVEN_CELL_YSTART`、横纵格数和 `38×38` 计算生成
- 腰带格子由 `_BELT_CELL_XSTART`、`_BELT_CELL_YSTART`、`_BELT_CELL_XGAP` 生成

下一步必须从 EI 二进制中恢复这些常量对应的构造函数，确认它们是否仍然是 EI 3.0 的同一版布局。

## 与 EI 二进制的核验规则

1. 先在 `Mir3.exe` 中定位 WIL 加载与 Frame 设置调用。
2. 用 Frame 对、整数常量序列、`SetRect`/`PtInRect` 调用和对象字段读取定位函数。
3. 对同一元素至少保留：函数 VA、调用 VA、资源帧、坐标来源、原始反汇编片段。
4. 只有 EI 二进制直接支持的记录才标为 `primary-static`；源码与二进制同时支持才标为 `verified-cross-source`。
5. 未匹配的源码记录只能标为 `secondary-source-hypothesis`。

