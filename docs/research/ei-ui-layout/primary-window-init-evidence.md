# EI 3.0 原版窗口初始化证据

## 通用窗口基类

原版 `Mir3.exe` 的 `VA 0x00423B30` 是通用窗口初始化函数候选。它的静态行为与早期源码 `CGameWnd::CreateGameWnd` 一致：

- 保存窗口 ID；
- 保存 WIL 图像对象；
- 调用 `0x00466130` 选择窗口 Frame；
- 读取当前 Frame 宽高；
- 通过 `USER32.SetRect` 建立图像矩形和窗口矩形；
- 保存可移动状态和显式宽高。

从主初始化函数的调用顺序，可以按早期客户端接口形状解析为：

```text
CreateWindowLike(id, image, frame, startX, startY, width, height, canMove)
```

这是调用约定和源码交叉得到的参数解释；每个窗口的具体类名仍须结合对象字段、后续绘图和命中测试进一步确认。

## 主 UI 初始化函数

窗口初始化调用集中在：

```text
Mir3.exe: VA 0x00427600 附近
```

主 HUD 本身在 `0x00427679` 设置 `[esi+0xC58]`，窗口对象则保存在主对象的不同字段中。

## 已解析的窗口调用表

| 调用 VA | 对象字段候选 | ID | Frame 候选 | 起点 `(x,y)` | 显式尺寸 `(w,h)` | 可移动 | 早期源码/业务候选 | 证据 |
|---|---|---:|---:|---:|---:|---:|---|---|
| `0x00427750` → `0x42EA80` | `[esi+0x6554]` | 0 | 250 | `(518,0)` | `284×324` | 1 | 背包 Inventory | primary-static + secondary match |
| `0x00427776` → `0x44B130` | `[esi+0x29CE4]` | 1 | 200 | `(0,0)` | `244×328` | 1 | 人物装备/状态 | primary-static + secondary match |
| `0x0042779C` → `0x44D310` | `[esi+0x33188]` | 2 | 1000 | `(0,0)` | `300×304` | 0 | 商店/仓库候选 | primary-static, Frame semantic pending |
| `0x004277C2` → `0x4159D0` | `[esi+0x3399C]` | 3 | 1050 | `(0,0)` | `484×330` | 1 | 交易/交换候选 | primary-static, Frame semantic pending |
| `0x004277E8` → `0x424E60` | `[esi+0x4707C]` | 4 | 600 | `(102,22)` | `596×446` | 1 | 行会掌柜/行会相关候选 | primary-static + secondary frame family |
| `0x00427811` → `0x424250` | `[esi+0x47834]` | 6 | 900 | `(272,123)` | `256×244` | 1 | 组队窗口 | primary-static + secondary match |
| `0x00427839` → `0x414060` | `[esi+0x507EC]` | 8 | 350 | `(114,76)` | `572×388` | 1 | 聊天弹窗 | primary-static + secondary match |
| `0x00427862` → `0x4503B0` | `[esi+0x47C28]` | 7 | 200 | `(560,0)` | `244×328` | 1 | 组队弹窗/附属面板候选 | primary-static, object semantic pending |
| `0x0042788D` → `0x440FE0` | `[esi+0x518E0]` | 12 | 750 | `(276,113)` | `248×264` | 1 | 系统选项 | primary-static + secondary match |
| `0x004278B3` → `0x4473E0` | `[esi+0x516E8]` | 11 | 700 | `(0,0)` | `340×440` | 1 | 任务窗口 | primary-static + secondary match |
| `0x004278D9` → `0x4268C0` | `[esi+0x52118]` | 13 | 850 | `(0,0)` | `296×332` | 1 | 马匹/坐骑窗口 | primary-static + secondary match |
| `0x00427904` → `0x439250` | `[esi+0x524F0]` | 14 | 400 | `(0,0)` | `296×332` | 1 | 其他窗口候选 | primary-static, Frame semantic pending |
| `0x0042792A` → `0x43ED00` | `[esi+0x51150]` | 9 | 1100 | `(0,0)` | `552×176` | 0 | NPC 对话窗口候选 | primary-static, Frame semantic pending |

### 参数解析说明

调用点以 x86 反向压栈形式出现。例如 `0x00427750` 附近：

```asm
push 0x1       ; canMove
push 0x144     ; height = 324
push 0x11c     ; width  = 284
push 0x0       ; startY
push 0x206     ; startX = 518
push 0xfa      ; frame = 250
push edx       ; image object
push 0x0       ; id = 0
call 0x42ea80
```

这组参数与 `CGameWnd::CreateGameWnd` 的字段用途吻合。窗口基类随后在 `0x00423B30` 中根据 Frame 的真实宽高建立图像 Rect；显式 `width/height` 则作为窗口命中/拖动区域尺寸。

## 重要版本差异

早期公开源码中有：

```text
Inventory  -> Frame 250, start (0,0), size from image
Status     -> Frame 200, start (510,0), size from image
Store      -> Frame 253
Exchange   -> Frame 251
Guild      -> Frame 169 / interfacec2
Group      -> Frame 900
ChatPop    -> Frame 350
Quest      -> Frame 700
Option     -> Frame 750
Horse      -> Frame 850
NPC        -> Frame 300
```

EI 二进制目前直接确认的部分与源码高度吻合：背包 250、状态 200、组队 900、聊天 350、任务 700、选项 750、马匹 850。商店/交易/NPC 等出现了 `1000/1050/1100` 等 EI 版本候选，不能擅自改成源码中的 253/251/300。

## 尚未完成的窗口级证据

1. 需要在 `GameInter.wil` 和 `Interface1c.wil` 中确认每个候选 Frame 的实际图像尺寸和透明区域。
2. 需要追踪每个 wrapper 内调用 `0x00417550` 的按钮帧对，建立窗口内按钮表。
3. 需要追踪每个对象字段的 `PtInRect` 调用，恢复拖动区、关闭按钮和内容命中区。
4. 需要追踪窗口显示函数中的绘制顺序，区分背板、图标、文字、物品和特效层。
5. `ID=5` 等未出现在当前截取范围的对象必须从完整主初始化函数继续补齐，不能因为早期源码有对应类就直接补写。

完整原始调用邻域见：

```text
docs/research/ei-ui-layout/window_init_candidates.json
```
