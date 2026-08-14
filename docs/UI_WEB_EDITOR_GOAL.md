# Godot UI Web 编辑器（所见即所得调 UI）— 完整任务目标

## 一、终极目标

用户在浏览器里看到 Godot 客户端的全部 UI（窗口/控件/贴图/文字，1:1 还原），
拖拽调整位置和大小、改文字，保存为 JSON overlay；点「同步」后 Godot 客户端
加载 overlay 应用改动——**不用重编译就能在游戏里看到调整结果**（热重载）。

## 二、架构（四件套）

```
[GodotClient] --①导出--> ui_tree.json + frames 清单
     ↑                        ↓
④热重载加载 overlay    [Web 编辑器 :8820]（贴图走 /zl/ 实时解码）
     ↑                        ↓
[GodotClient/UI/ui_overlay.json] <--③同步-- 编辑器保存
                    + ②游戏截图 underlay
```

### A. 导出器（C#，GodotClient 内加调试模式 `--ui-export`）
- 反射枚举全部 DXWindow 子类（GodotClient/Controls/ 下 60+ 窗口），
  无头实例化（或实例化后不进主循环），强制执行布局代码
- 深度遍历 DXControl.Controls 树，每个节点导出：
  - `path`（稳定路径：`窗口类名/0/3/1` 子索引链）、`type`（DXButton/DXLabel/DXImageControl/...）
  - `location [x,y]`、`size [w,h]`、`text`、`fontSize`、`foreColour/backColour`、
    `visible`、`libraryFile+index`（图片控件）、`hint`
- 图片帧不逐个导 PNG——导 manifest（LibraryFile→帧号集合），web 端按需解码
- 输出 `GodotClient/UI/ui_tree.json`（含每窗口 1024x768 逻辑坐标基准）
- ⚠️ 布局基准铁律：**所有坐标按逻辑画布 1024x768 导出**（UiScaler 缩放前的值），
  否则 4K 下双倍偏移（踩过的坑）

### B. Web 编辑器（Mir3-Research/Tools/uieditor/，FastAPI :8820，仿 dbeditor 模式）
- **渲染**：absolute 定位 DOM（不是 canvas——DOM 天然支持选择/拖拽/事件），
  图片控件 `<img src="/zl/Interface/123.png">`（复用 dbeditor 的 zlsdk 实时解码端点模式）
- **左侧窗口列表**（按类名+中文名）+ **中间画布**（1024x768 底板，可 0.5x/1x/2x 缩放）
  + **右侧树视图**（控件树，点击选中联动）+ **属性面板**（Location/Size/Text/
  FontSize/颜色/Visible——只开放 overlay 可安全覆盖的属性）
- **交互**：点选、框选、方向键微调（1px，Shift=10px）、拖拽移动、8 向手柄缩放、
  对齐吸附（可选开关：吸附到 2px 网格/其他控件边缘）、Undo/Redo（Ctrl+Z）
- **截图 underlay（关键体验）**：每窗口可选叠加一张真实游戏截图做底（半透明），
  控件贴着真实游戏画面对位置——所见即所得的铁保证。截图由 D 阶段产出
- **保存**：只存 diff（`ui_overlay.json`：`{窗口类名: {控件path: {location: [x,y]}}}`），
  未改动的控件不进 overlay
- **同步**：写 `~/development/zircon/GodotClient/UI/ui_overlay.json`（原子写+备份上一版）
- 移动端 390px 至少能看（浏览模式，编辑桌面优先）

### C. Godot 端 overlay 加载器（C#）
- `UiOverlay.cs`：启动时读 `UI/ui_overlay.json`；在每个窗口布局完成后
  （DXWindow 构造末尾/统一 hook）按 path 查表应用 override
  （Location/Size/Text/FontSize/Visible——属性面板开放的同一集合）
- **热重载**：游戏内按 F12（或 GM 命令 @uiReload）重新读文件并刷新全部已开窗口——
  浏览器改完点同步→游戏里按一下 F12 立即生效，**零重启迭代**
- 校验：path 不存在时告警日志跳过（不崩）；类型不匹配的属性忽略

### D. 游戏截图 underlay 生成（辅助脚本）
- 无头客户端（Xvfb :100 + openbox）逐窗口截图：启动游戏→F 键开窗口（KeyBindManager
  映射：W=背包 Q=角色 E=技能 G=行会 B=大地图…）→ scrot → 存 `uieditor/shots/窗口名.png`
- 至少覆盖 8 个高频窗口（背包/角色/技能/设置/行会/大地图/聊天/任务日志）

## 三、执行顺序

1. **A 导出器**（dotnet，--ui-export 跑一次出 ui_tree.json，验证：JSON 里 ≥40 窗口、
   InventoryDialog 树深 ≥3、坐标全在 0-1024/0-768 内）
2. **B 编辑器骨架**（读 ui_tree.json 渲染 + 选择/拖拽/属性面板 + 保存 overlay）
3. **C 加载器 + 热重载**（这是闭环关键）
4. **D 截图 underlay**（体验增强）
5. 全链路验收

## 四、验收标准（全过才算完成）

1. `--ui-export` 产出 ui_tree.json：≥40 窗口含完整控件树+贴图引用
2. 浏览器 :8820 打开编辑器：窗口列表可见，选「背包 InventoryDialog」显示
   背包窗口全部控件（含贴图图片、文字标签），截图 /tmp/uied_bag.png
3. 拖动背包标题 Label 改位置 → 保存 → ui_overlay.json 出现该 diff（只有该条）
4. **游戏内闭环**：起无头客户端开背包 → 浏览器改标题位置 → 点同步 → 游戏内 F12 →
   截图对比标题确实移动了（/tmp/uied_before.png、/tmp/uied_after.png）
5. overlay 为空时游戏零变化（加载器无副作用）
6. `dotnet build` 0 错误；编辑器移动端可浏览
7. zircon 仓库提交（--ui-export + UiOverlay.cs + UI/ 目录）+ Mir3-Research 提交（uieditor 工具）
   都 push（中文信息）

## 五、边界与红线

- **overlay 只改视觉属性**，永不动逻辑/事件绑定
- zircon 推 fork 远程；不碰原版 Client/ 源码
- 端口 8820（8810 dbeditor/8899 mapviewer/8765 wilviewer 已占）
- uieditor venv 用 uv（照抄 dbeditor 的 run.sh 模式）
- 编辑器是开发工具：不做多人/鉴权，但写文件必须原子+备份
- 若与 mvtoolkit goal（也在 Mir3-Research）文件冲突，只会在不同目录，无需协调；
  mapviewer 改动别碰

## 六、已知踩坑速查（做之前先读，别重踩）

- 坐标基准 1024x768（见 A）；无 WM 的 Xvfb 视口固定 1024x768（测试用 ZIRCON_UI_SCALE=2 强制缩放验证）
- 改 static 文件要 bump `?v=N` 防浏览器缓存
- zlsdk 解码：BGRA→RGBA 已处理，物品图标用 Storeitems.Zl、UI 库是 Interface.Zl（Libraries.cs 查）
- MirDB/工具类外部工程要传两个程序集（本任务用不到 DB，但若要读什么表记住这条）
- 测试账号 test@test.com/test123/TestHero；服务端 7000；无头验证走 :100+openbox
