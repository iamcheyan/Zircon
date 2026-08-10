# Mir3 EI 图层绘制顺序与锚点结论文档

结论全部来自 Mir3.exe 反汇编（`/tmp/mir3_text.txt`，161763 行；grep 限前 4MB ≈
行 98000）+ mapviewer 渲染实现对照。分级：confirmed = 反汇编直接证据；
derived = 由反汇编推断、未运行原版客户端；pending = 未定。

## 绘制顺序：ground → mid → front → actor [confirmed]

地图格循环（41c4xx 区）每格按序调用：

1. **ground**：0x43b440 / 0x43b9a0 / 0x43c330 / 0x43c4c9（destY 常数 −157；
   blit 点 43b5c2 / 43baf9 / 43c4c9）。10/10 张 0x460240 调用点归属 ground。
2. **mid / front（物件层）**：0x43bb10（48×32 专用，blit @43bce6→0x460240、
   blend 43bcf5→0x4542a0→0x466800→0x4542f0）、0x43be00（非 48×32，blit
   @43bfd2→0x460240；48×32 门控 @43bed9）。调用点 41c59a/41c5a5/41c66d/41c678，
   sel=0 → mid、sel=1 → front。
3. **actor（角色/怪物/NPC）**：0x430b00 系（另 40b583 / 40fb57 / 430b5b 带 offset
   绘制调用点）。

mid 先于 front（同函数对不同 sel 调用次序）。遮挡/窗口裁剪（41c5aa-41c5de：
x vs 0xf532c、y vs 0xf5330、窗口内相对偏移，否则 0）位于 mid/front 绘制段之后
[derived 细节，见 pending]。

## 锚点：地图层全部格底/格左 [confirmed]

- mid/front dest 公式：`destX = (x−viewX)·48 − scrollX − 200`、
  `destY = (y−viewY)·32 − scrollY − h − 125` ⇒ **帧底 = destY + h =
  (y−viewY)·32 − scrollY − 125 = 格底**；帧左 = 格左（−200 常数含窗口偏移 −200）。
- ground 底 = −157 + 32 = **−125，与 mid/front 帧底同一条线** ⇒ 地面与物件
  共用格底对齐基线。
- 与 ZL C# `MapControl.drawY`（格底 +1）一致（+1 为像素级微调，同一锚点语义）。
- mapviewer 锚点修复 `py = cy + 32 − h·scale`（mid/front 格底）与此吻合
  [confirmed]；iso 模式沿用旧中心锚（cx−24+off_x, cy−16+off_y）[derived]。

## Offset：地图层零 offset，actor 层读 offset [confirmed]

- **地图层全分支零 offset 读取**：0x43bb10 / 0x43be00（mid/front）与
  0x43b440 / 0x43b9a0 / 0x43c330 / 0x43c4c9（ground）的所有分支（选择段、
  动画段、普通 dest+blit、blend 浮点路径）只读 `frameW@+0` / `frameH@+2` /
  `srcData@entry+0x3c`，**从不读 frame+4/+6**。
- **actor 层读 offset**：0x430b00 内 `430aab: movswl 0x4(%eax),%ecx`（off_x）、
  `430aaf: movswl 0x6(%eax),%edx`（off_y）→ `add %ecx,%ebx`（destX += off_x）/
  `add %edx,%ebp`（destY += off_y）。
- 含义：WIL 帧 offset 字段（+4/+6）服务于 actor 帧脚底对齐；地图层帧的该字段
  原版不参与绘制。mapviewer 默认 `om=none`（零 offset）即原版路径。

## 帧结构拆分 [confirmed]

- func1（0x43bb10）= 仅绘 **48×32**（W==0x30 && H==0x20）帧。
- func2（0x43be00）= 跳过 48×32（43bed9 起 `mov 0x38(%ecx),%eax; cmpw $0x30,
  (%eax); jne; cmpw $0x20,0x2(%eax); je 0x43c0d9`），绘其余尺寸帧。

## 对照实验（三模式）

详见 `comparisons/OFFSET-EXPERIMENT.md`：`none`（原版零 offset）vs `all`
（ground+mid/front 全 offset）vs `midfront`（仅 mid/front）。10 图 z4 条带 +
模拟器 30 帧 + 逐像素 diff stats。结论：`all` 破坏原版观感（建筑群错位破碎），
`midfront` 与 `none` 近同协调 ⇒ **原版 = none**。

## pending（未定项）

- 41c5aa-41c5de 遮挡窗口细节（已有梗概，未逐分支核对）。
- 0x41cbd0 actor 渲染器体、0x419d40 身份（未深读）。
- 0x434a20 = 选区足迹几何（非绘制）已确认；其与绘制循环的交互细节未读。
