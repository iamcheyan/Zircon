# 资源工具跨平台移植指南(LibraryEditor / ImageManager / Launcher / Patcher)

> 日期:2026-08-06
> 状态:方案定稿,图片查看器(ZlViewer)已实施,其余待做
> 关联:`docs/RENDERING_PORT_GUIDE.md`(渲染数据移植)、`docs/notes/05`(Godot 客户端骨架)

---

## 1. 一句话结论

四个 Windows 工具都是 **「纯 .NET 的干活逻辑」+「WinForms 的窗口壳」** 的组合:

- **干活逻辑**(格式读写、下载、解压、校验、转换)——全部跨平台,可直接复用;
- **窗口壳**(WinForms + DevExpress 皮肤 + GDI+ 画图)——Windows 专属,扔掉重写。

所以"把它们做成跨平台"不是重写,而是**剥壳**。工作量比想象小得多。

---

## 2. 四工具现状(源码证据)

### 2.1 Launcher(玩家登录器)

| 项 | 内容 |
|---|---|
| 入口 | `Launcher/Program.cs` → `LMain.cs`(`XtraForm`) |
| 职责 | 启动时比对补丁版本 → 从 HTTP 下载缺失/过期文件 → 解压 → 启动游戏 exe |
| 核心逻辑 | `CheckPatch` / `LoadVersion` / `GetPatchInformation` / `CalculatePatch` / `DownloadPatch` / `Download` / `Extract` / `Decompress` |
| Windows 依赖 | 仅 DevExpress 窗体 + `Process.Start("Zircon.exe")`(启动游戏这一行) |
| 复用价值 | **极高**。逻辑 100% 是 .NET 标准库:`HttpClient` 下载、`GZipStream` 解压、`Version.bin` 二进制比对、checksum 校验。没有任何 Windows API 调用 |

关键事实:`LMain.cs` 里所有文件操作、网络操作、压缩操作都是 `System.*` 命名空间,**窗口只是画了进度条**。

### 2.2 ImageManager(图片资源批处理)

| 项 | 内容 |
|---|---|
| 入口 | `ImageManager/Program.cs` → `IMain.cs`(`XtraForm`) |
| 职责 | ① 目录批量 WTL→Zl 转换(10 线程并行);② 从 BMP + Placements.txt 生成 .Zl 图库;③ 大图缩放到 800×600 内 |
| 核心逻辑 | `ConvertLibrariesButton_Click`(WTL→Zl)、`CreaetLibrariesButton_Click`(BMP 打包)、`WTLLibrary.Convert()`、`Mir3Library.Save()` |
| Windows 依赖 | GDI+ `System.Drawing.Bitmap`(读 BMP 像素、取宽高)、WinForms 窗体、`FolderDialog` |
| 复用价值 | **高**。转换核心是纯文件读写(`BinaryReader/Writer`);GDI+ 只用在"把 BMP 变成像素字节"这一步,这一步在 Godot 里可用 `Image.LoadBmpFromBuffer` 替代 |

关键事实:两个按钮的 handler 都是 `Task.Run(() => Parallel.For(...))` 批处理 + 轮询刷新进度条标签——**本质就是带窗口的命令行工具**。

### 2.3 LibraryEditor(资源库编辑器)

| 项 | 内容 |
|---|---|
| 入口 | `LibraryEditor/Program.cs` → `LMain.cs`(支持文件关联单参数打开) |
| 职责 | 编辑 .Zl/.WTL 资源库:查看帧、编辑偏移/尺寸、转换格式、保存 |
| 核心逻辑 | `WeMadeLibrary.cs`(.Zl 读写)、`WTLLibrary.cs`(.WTL 读写)、`Astc.cs`(ASTC 解码)、`Mir3Library.cs`、`CrystalLibraryV1/V2.cs` |
| Windows 依赖 | GDI+ 画帧控件、DevExpress 皮肤与表格、**`ManagedSquish.dll`(native C++ DXT 压缩库,`Components/` 里的 NativeSquish_x86/x64.dll)** |
| 复用价值 | **格式层可复用**;编辑 UI 重写成本最高 |

关键事实:格式层(`WeMadeLibrary`/`WTLLibrary`)是纯 `BinaryReader/Writer` 读写;唯一 native 依赖是 Squish(DXT 压缩),跨平台替代品是 NuGet 纯托管包 **BCnEncoder.NET**(已在 Godot 客户端验证可用,见 §3)。

### 2.4 Patcher(自更新器)

| 项 | 内容 |
|---|---|
| 入口 | `Patcher/Program.cs` → `PMain.cs` |
| 职责 | Launcher 需要自更新时,替换被占用的 `Launcher.exe`(下载到 `.tmp` 再改名) |
| Windows 依赖 | **进程占用文件**语义(Linux 上运行中的文件可自由覆盖,无此问题) |
| 复用价值 | **低——Linux 上直接不需要** |

---

## 3. 共享格式层:已经有人搬了一半

四个工具共同的底层是"读 .Zl / .WTL / .map / 解码 DXT"这一层。仓库现状:

| 格式 | Windows 原实现 | 跨平台现状 |
|---|---|---|
| .Zl 图库 | `RenderingCore/Library/MirLibrary.cs` + `LibraryFormat/ZlFormat.cs`(格式定义) | ✅ `GodotClient/Formats/ZlReader.cs`(Godot 版读取器,支持旧版 version 0/1) |
| DXT1/5/BC7 解码 | `ManagedSquish.dll`(native C++) | ✅ `GodotClient/Formats/BcnDecoder.cs`(BCnEncoder.NET 纯托管,已验证) |
| .map 地图 | `Client/Scenes/Views/MapControl.cs:484-545` | ✅ `GodotClient/Formats/MapReader.cs` |
| .WTL 图库 | `LibraryEditor/WTLLibrary.cs`、`ImageManager/WTLLibrary.cs` | ❌ 未移植(转换时读 WTL 用) |
| ZL2 压缩容器 | `LibraryFormat/ZlFormat.cs` | ❌ 未移植(277 个 .Zl 中 7 个用 ZL2,查看器标注"不支持") |

**结论**:格式读取层跨平台版已存在且验证过,后续工具(CLI 转换、编辑器)直接引用 `GodotClient/Formats/` 或把它提升为共享库即可。

---

## 4. 跨平台方案

按"你实际用不用得上"排序,不是按难易。

### 4.1 Launcher → 并入 Godot 客户端(最高 ROI)

- **做法**:启动流程加"检查补丁"一步——查 `Version.bin` 与服务端补丁列表,有缺则下载;然后把"启动 Zircon.exe"换成 **直接切进 Godot 登录场景**(游戏就是客户端自己)。
- **收益**:
  - 登录器与客户端合并成一个程序,省一个进程;
  - 顺带消灭 `Process.Start("Zircon.exe")` 这个 Windows 假设;
  - Patcher 的自更新机制随之退役(合并后没有"被占用的 exe")。
- **工作量**:一个场景 + 复用 `LMain.cs` 的下载/解压/比对逻辑(几乎照抄)。

### 4.2 ImageManager → CLI 工具(次高 ROI)

- **做法**:两个批处理按钮原样变成命令行:
  - `zircon-image convert --in <dir> --out <dir>`(WTL→Zl,递归)
  - `zircon-image pack --in <dir> --out <dir>`(BMP+Placements.txt→Zl)
  - `zircon-image thumb --dir <dir>`(大图缩到 800×600)
- **收益**:比 Windows 版更好用——能进 shell 脚本,能 `find | xargs` 批量。仓库已有先例:`Tools/convert_audio_to_ogg.cmd`、`Tools/setup_environment.sh` 都是"资源批处理脚本化"趋势。
- **技术点**:GDI+ 读 BMP 换成 Godot 的 `Image.LoadBmpFromBuffer`(或 ImageSharp NuGet);进度条换 `Console.WriteLine`;`Parallel.For` 照用。
- **前置**:先移植 `WTLLibrary.cs` 读取(当前唯一未移植的格式层)。

### 4.3 LibraryEditor → 查看器 + 可选编辑器

拆成两个能力,别一起做:

- **"看资源"** → **已由 ZlViewer 完成**(§5):Godot 查看器,浏览 Data 目录所有 .Zl。跨平台、零 GDI+。
- **"改资源"**(编辑帧偏移/尺寸、重新打包)→ 用 **Avalonia**(跨平台桌面 UI,.NET 原生,不依赖 GDI+,有免费表格/树控件)重画界面,底层复用 `GodotClient/Formats/` + BCnEncoder。
- **砍半建议**:如果没人明确要"改资源",这步可以永远不做。Godot 客户端本身就是最好的查看器(地图渲染出来后,`.map` + `.Zl` 直接在游戏里看)。

### 4.4 Patcher → 退役

Linux/macOS 无"运行中文件被占用"问题,自更新不需要独立进程。合并进 Launcher 方案后彻底删除。

### 4.5 通用原则:格式层收敛成一份共享库

`GodotClient/Formats/`(ZlReader/MapReader/BcnDecoder)和 Windows 版(`LibraryEditor/*`、`RenderingCore/*`)现在各抄一份。做 CLI 工具时,应把 `GodotClient/Formats/` 提升为共享项目(如 `SharedFormats/`),Windows 版与跨平台版共用,避免双份维护。

---

## 5. 已实施:ZlViewer(Godot 图片查看器)

### 5.1 用途

浏览 `Debug/Client/Data/` 下全部 277 个 .Zl 图库(怪物/玩家/装备/道具/UI 图标等),查看每一帧图像与元数据。跨平台(Linux/macOS/Windows 都能跑,复用 Godot 引擎渲染)。

### 5.2 用法

```bash
# 浏览整个 Data 目录(默认 ../Debug/Client/Data)
godot-mono --path GodotClient/ -- --zl-dir Debug/Client/Data

# 只看单个文件
godot-mono --path GodotClient/ -- --view-zl Debug/Client/Data/Weapon.Zl
```

### 5.3 界面

```
┌──────────────────────────────────────────────┐
│ 资源文件                      │ 状态: xxx.Zl: 512 帧 (version 0) │
│ ┌────────────────┐           ├──────────────────────────────────┤
│ │ Background.Zl  │           │ 帧网格(缩略图,点击看大图)         │
│ │ CBIcons.Zl     │           │                                  │
│ │ Equip.Zl       │           │                                  │
│ │ ...            │           │                                  │
│ └────────────────┘           ├──────────────────────────────────┤
│ [刷新]                       │ 大图显示                          │
│                              │ index=123 64x96 offset=(-32,-48) │
└──────────────────────────────┴──────────────────────────────────┘
```

### 5.4 实现

- `Scenes/ZlViewer.tscn` — 布局(HSplitContainer:左文件列表 + 右帧网格/大图)
- `Scripts/ZlViewer.cs` — 逻辑
- `Scripts/LoginScene.cs:_Ready()` 开头加参数检测,`--zl-dir`/`--view-zl` 时直接切到查看器场景并跳过服务端连接

复用的现有代码:`Formats/ZlReader.cs`(读 .Zl + DXT 解码 → `ImageTexture`)、`Formats/BcnDecoder.cs`(BCnEncoder.NET)。

### 5.5 已知限制

- ZL2 压缩容器(7 个文件)暂不支持,文件列表中标"不支持";旧格式 270 个全可看
- 一次解码全部帧的缩略图,超大图库(数千帧)首次加载可能慢
- 帧网格直接贴原尺寸纹理(显示缩略,内存按原尺寸),超大贴图(2048²)文件内存峰值高——后续可加"解码后缩小再上屏"

---

## 6. 待办(按依赖顺序)

| # | 任务 | 依赖 | 工作量 |
|---|---|---|---|
| 1 | ✅ ZlViewer 图片查看器 | 无 | 已完成 |
| 2 | 移植 `WTLLibrary.cs` 读取到跨平台 | 无 | 小 |
| 3 | ImageManager → CLI(`convert`/`pack`/`thumb`) | #2 | 中 |
| 4 | Launcher → 并入 Godot(补丁检查 + 下载) | 无 | 中 |
| 5 | 格式层收敛为共享库 `SharedFormats/` | #2 | 小 |
| 6 | LibraryEditor "改资源" → Avalonia(可选) | #5 | 大,可砍 |
| 7 | Patcher 退役删除 | #4 | 零 |
