# EI 3.0 原版主 HUD Rect 初始化证据

## 主 HUD 主矩形

在 `Mir3.exe` 的函数 `0x00427600` 附近发现主界面初始化逻辑：

1. 从调用参数建立 GameInter 图像对象；
2. 调用资源选择函数 `0x00466130`，参数 `0x32`，即 Frame 50；
3. 读取当前 WIL Frame 的宽高；
4. 通过 USER32 `SetRect`（本文件中该 PE 的 IAT VA 为 `0x004762B0`）写入 `[esi+0xC58]`。

反汇编核心形式：

```asm
mov  eax, dword ptr [ecx + 0x38] ; 当前 WIL Frame 信息
mov  ecx, 0x259                  ; 601
movsx edx, word ptr [eax]        ; width
movsx eax, word ptr [eax + 0x2]  ; height
sub  ecx, eax                    ; 601 - height
push 0x258                       ; bottom = 600
push edx                         ; right = width
push ecx                         ; top = 601 - height
push 0                           ; left = 0
push edx                         ; rect pointer = [esi+0xC58]
call SetRect
```

因此当前可以确认：

```text
main_rect.left   = 0
main_rect.top    = 601 - GameInter[50].height
main_rect.right  = GameInter[50].width
main_rect.bottom = 600
```

对于当前原版资源，Frame 50 是 `800×136`，所以静态结果为：

```text
main_rect = (0, 465, 800, 600)
```

注意：早期公开源码写的是 `600 - imageHeight`，EI 二进制实际机器码表现为 `601 - height`。这不是简单覆盖，必须保留为版本差异；最终显示是否存在一像素边界约定，需要运行时截图验证。

## 同一函数中的其他 SetRect

这些矩形都来自原版二进制的 SetRect 调用。对象字段的语义还在追踪中，因此只列出字段偏移和矩形参数，不把它们强行命名成 HP、聊天或地图。

| 调用 VA | 对象字段 | Rect `(left,top,right,bottom)` | 当前候选语义 | 证据 |
|---|---|---|---|---|
| `0x00427679` | `[esi+0xC58]` | `(0, 601-height, width, 600)` | 主 HUD 背板 | primary-static, Frame 50 已匹配 |
| `0x00427696` | `[esi+0xCF8]` | `(224,492,578,566)` | 聊天/文本总区域候选 | primary-static, semantic pending |
| `0x004276B3` | `[esi+0xCA8]` 起始数组 | `(224, ebx, 578, 492)`，其中 `ebx` 由 `eax+15` 计算 | 多行文本/聊天行候选 | primary-static, dynamic pending |
| `0x004276D6` | `[esi+0xC68]` | `(61,496,104,566)` | 左侧 HUD 数值/血球相关候选 | primary-static, semantic pending |
| `0x004276F0` | `[esi+0xC78]` | `(105,496,147,566)` | 左侧 HUD 数值/魔球相关候选 | primary-static, semantic pending |
| `0x0042770D` | `[esi+0xC88]` | `(235,586,400,597)` | 底部地图名/坐标文字候选 | primary-static, semantic pending |
| `0x0042772A` | `[esi+0xC98]` | `(206,499,215,574)` | 底部小型状态/图标候选 | primary-static, semantic pending |

`0x004276B3` 是循环中的一个代表调用，循环每次将目标对象增加 `0x10`，因此实际包含 16 个相邻矩形记录。提取器保留了每一次原始调用，后续需要把循环边界和数组字段写进 layout 数据模型。

## 与源码交叉结果

早期源码交叉参考中存在：

```text
chat = (224,471,578,545)
```

EI 二进制的直接 SetRect 候选为：

```text
(224,492,578,566)
```

两者 X 坐标一致，但 Y 和高度不同。当前不能判定其中一个“错”：可能是版本差异、调用对象不同、聊天历史区与输入区的不同层级，或源码中的坐标经过了其他偏移。必须继续追踪 `[esi+0xCF8]` 的读取和绘图调用后才能命名。

## 机器可读证据

完整 155 条 SetRect 调用候选见：

```text
docs/research/ei-ui-layout/setrect_calls.json
```

其中 `0x00427600` 附近的间接调用通过先前加载的 `USER32.SetRect` 函数指针识别，标记为 `call_mode=indirect-register`。

## 动态血条/蓝条/经验比例的新增证据

`0x00429740` 是主 HUD 的动态绘制路径之一。它先从原版全局状态读取数值，使用 x87 `fild/fidiv` 计算比例，并把结果限制在 `0.0–1.0`；这说明这里不是手工拖动校准，而是固定 Rect 加运行时裁剪/合成。

已确认的比例计算：

| 候选条 | 原版计算 | 机器码位置 | 状态 |
|---|---|---|---|
| 第一资源条（对应 `[this+0xC68]` Rect） | `low16(0x007D9264) / low16(0x007D9262)` | `0x00429876–0x004298B1` | 比例链与 HP 语义已确认 |
| 第二资源条（对应 `[this+0xC78]` Rect） | `low16(0x007D9266) / low16(0x007DA113)` | `0x0042996C–0x004299A4` | 比例链与 MP 语义已确认 |
| 经验进度 | `0x007DA115 / 0x007DA119` | `0x00429920–0x0042994F` | 比例链已确认 |
| 负重/负载 | `low16(0x007DA109) / low16(0x007DA11F)` | `0x0042996C–0x004299A4` | 比例链与负重语义已确认 |

随后调用 `0x00466800` 准备条带几何/纹理参数，并通过 `0x004542F0` 合成到画面；第一、第二资源 Rect 的屏幕位置仍是前文的 `(61,496)-(104,566)` 和 `(105,496)-(147,566)`。因此还原 Zircon 时应直接实现“固定外框 + 按比例裁剪填充”，不要让使用者手动拖坐标。完整字段和地址见 `hud-bars-render-evidence.json`。

同一主 HUD 绘制族还在 `0x0042A065–0x0042A087` 读取经验比例，乘以浮点常量
`[0x0047644C]`，再通过 `0x0046811C` 格式化后绘制到 `[this+0xC88] = (235,586)-(400,597)`。
原版 `.data` 中的 GB18030 字面量已直接解出：`0x0047BD4C = (经验条)%.2f%s`，
`0x0047BD5C = %`；同一族还明确包含 `0x0047BD70 = (血量)%d/%d`、
`0x0047BD60 = (魔法量)%d/%d`、`0x0047BD40 = (负重)%d/%d`。因此这些字段的内容语义
已经由原版字面量确认，不再只是运行时命名候选。
