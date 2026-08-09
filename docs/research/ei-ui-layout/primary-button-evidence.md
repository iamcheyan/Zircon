# EI 3.0 原版二进制：底部 HUD 按钮初始化证据

## 结论范围

在 `Mir3.exe` 中确认了直接调用 `VA 0x00417550` 的 UI 控件初始化函数。该函数会把资源/帧参数和位置参数写入控件对象，并依据 WIL 当前帧尺寸建立命中矩形。以下记录来自原版二进制，不是现代 Zircon 坐标。

原始机器码证据：

- 构造函数：`0x00417550`
- 连续初始化区间：`0x004279B2`–`0x00427D94`
- 图像对象字段：调用前从 `[esi+0x1c]` 取得
- 主界面横向基准：`[esi+0xc58]`
- 主界面纵向基准：`[esi+0xc5c]`
- 每组调用都使用一对连续 Frame 编号，并把两个位置寄存器传给控件初始化函数

## 已确认的 16 组帧对与位置表达式

这里的 `X`/`Y` 是反汇编中由寄存器计算出来的控件位置表达式；目前已确认它们是按钮初始化的目标位置，但 `[esi+0xc58]` 和 `[esi+0xc5c]` 的高层字段名称仍待继续追踪。

| 调用 VA | Frame 对 | X | Y | 早期源码对应语义 | 证据 |
|---|---:|---:|---:|---|---|
| `0x4279B2` | `80/81` | `baseX+204` | `baseY+2` | 交换窗口 | primary-static-control |
| `0x4279E6` | `82/83` | `baseX+228` | `baseY+2` | 小地图 | primary-static-control |
| `0x427A1A` | `84/85` | `baseX+252` | `baseY+2` | 技能/武功入口 | primary-static-control |
| `0x427A4E` | `90/91` | `baseX+161` | `baseY+46` | 退出 | primary-static-control |
| `0x427A82` | `92/93` | `baseX+161` | `baseY+82` | 登出 | primary-static-control |
| `0x427AB6` | `94/95` | `baseX+616` | `baseY+47` | 组队 | primary-static-control |
| `0x427AEA` | `96/97` | `baseX+616` | `baseY+82` | 行会 | primary-static-control |
| `0x427B58` | `100/101` | `baseX+703` | `baseY+16` | 技能窗口 | primary-static-control |
| `0x427BAA` | `102/103` | `baseX+718` | `baseY+32` | 聊天窗口 | primary-static-control |
| `0x427BFC` | `104/105` | `baseX+718` | `baseY+70` | 任务窗口 | primary-static-control |
| `0x427C4D` | `106/107` | `baseX+703` | `baseY+85` | 选项 | primary-static-control |
| `0x427C9F` | `108/109` | `baseX+664` | `baseY+86` | 组队窗口 | primary-static-control |
| `0x427CF1` | `110/111` | `baseX+648` | `baseY+70` | 人物/状态窗口 | primary-static-control |
| `0x427D42` | `112/113` | `baseX+648` | `baseY+32` | 背包窗口 | primary-static-control |
| `0x427D94` | `114/115` | `baseX+665` | `baseY+16` | 商店窗口 | primary-static-control |

另外，腰带帧对 `52/53` 在其他控件初始化代码中出现，已被提取器识别，但尚未与这段 16 按钮连续表放在同一个主 HUD 构造函数中。

## 与早期源码的比较

早期公开 Mir3 源码也给出相同的 X 偏移和相同的帧对顺序：

```text
80/81  -> +204
82/83  -> +228
84/85  -> +252
90/91  -> +161
92/93  -> +161
94/95  -> +616
96/97  -> +616
100/101 -> +703
102/103 -> +718
104/105 -> +718
106/107 -> +703
108/109 -> +664
110/111 -> +648
112/113 -> +648
114/115 -> +665
```

这是目前“源码交叉参考”和“EI 原版二进制”最强的一组吻合证据。Y 偏移没有完全相同，因此当前数据模型必须同时保存：

1. 原版二进制的 `base + constant` 表达式；
2. 早期源码的相对坐标假设；
3. 二者的差值；
4. 最终待运行时截图或绘图调用验证的状态。

## 机器可读数据

完整调用邻域、寄存器赋值和对象地址见：

```text
docs/research/ei-ui-layout/button_constructor_calls.json
```

该文件中的 `confidence=primary-static-control-initializer` 只表示“原版静态控件初始化证据”，不表示已经完成运行时截图验证。

## 原版资源尺寸

从原版 `/home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data/GameInter.wil` 读取的尺寸如下：

| Frame 范围 | 尺寸 |
|---|---|
| 50 | `800×136` |
| 52/53 | `12×8` |
| 60/61 | `56×110` |
| 63 | `164×6` |
| 67 | `4×70` |
| 80–85 | `24×16` |
| 90–97 | `28×26` |
| 100–115 | `40×38` |

机器可读的完整尺寸记录见：

```text
docs/research/ei-ui-layout/gameinter-frame-metadata.json
```
