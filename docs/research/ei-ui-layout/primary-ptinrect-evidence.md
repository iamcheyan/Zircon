# EI 原版 `PtInRect` 命中区域证据

## 结论

原版按钮的命中区域不是靠人工校准得到的，而是在控件初始化时由二进制动态计算：

```text
RECT* = this + 0x04
left   = position_x
top    = position_y
right  = position_x + WIL.current_frame.width
bottom = position_y + WIL.current_frame.height
```

这段逻辑位于原版 `Mir3.exe` 的 `VA 0x00417550`。它从 `this+0x14` 找到当前资源对象，读取当前 Frame 的宽高，然后调用 `USER32.SetRect` 的 IAT `0x004762B0`：

```text
push frame_height + position_y
push frame_width  + position_x
push position_y
push position_x
push this + 0x04
call dword ptr [0x004762B0]
```

## 鼠标处理链

同一个控件对象的三个函数随后使用 `USER32.PtInRect` 的 IAT `0x004762B4`：

| 调用 VA | 作用（按行为命名） | 命中矩形来源 |
|---|---|---|
| `0x00417791` | 鼠标经过/状态更新 | `this+0x04` |
| `0x004177D1` | 点击测试 | `this+0x04` |
| `0x00417802` | 另一种点击/辅助测试 | `this+0x04` |

例如 `0x00417791` 的机器码先计算 `lea edx,[esi+0x04]`，再按 `y, x, rect_ptr` 压栈调用 `PtInRect`。命中后会把 `this+0x25` 设置为状态值；这解释了普通/悬停/按下状态为什么可以复用同一个静态矩形。

## 证据边界

- `0x00417550` 的矩形计算是一级二进制证据。
- `PtInRect` 调用位置是一级二进制证据，但调用者所属窗口和具体业务名称仍需逐函数追踪。
- 矩形的最终绝对坐标必须把控件的 `position_x/position_y` 与其所属窗口的基准位置合并；不能把 `this+0x04` 误认为全局坐标。
- WIL 帧可能包含透明留白，因此“命中矩形”是整张 Frame 的外接矩形，不一定等于可见像素轮廓。
- 其他窗口中发现的 `PtInRect` 调用暂时全部保留为候选，不能因为存在固定常量就直接命名为某个按钮。

## 机器可读产物

```text
Tools/extract_mir3_ptinrect_calls.py
docs/research/ei-ui-layout/ptinrect_calls.json
```

该 JSON 保存每个调用点附近的原始反汇编、最近的三次压栈和推定角色。后续应将它与 `button_constructor_calls.json`、`window_layout.json` 按控件对象/调用者函数合并。

## 对预览器的直接影响

预览器的按钮调试层应优先使用：

```text
position = 原版 0x00417550 的两个位置参数
size     = 对应 WIL Frame 的 width/height
hit_rect = position + size
```

只有当位置基准尚未解析时，才显示“相对坐标/待合并”标记；不能让用户拖动矩形来替代二进制提取。
