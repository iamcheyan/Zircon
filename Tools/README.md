# 传奇3 EI 素材查看工具集

用于查看传奇3 EI（Mir3）客户端资源的本地工具。依赖 **Python 3 + Pillow**，零其他依赖。

```
mir3ei/
├── Data/            # *.wil + *.wix 贴图库、*.map 地图
├── Sound/           # *.wav 音效
└── tools/
    ├── wilsdk.py       # 核心解析库（WIL/WIX 解码）
    ├── wilviewer.py    # Web 浏览器查看器（推荐）
    ├── wilextract.py   # 命令行批量导出
    └── README.md
```

## 快速开始

### Web 查看器（推荐）

```bash
cd tools
python3 wilviewer.py                      # 自动探测根目录（$MIR3EI_ROOT → 脚本上级 → NAS 路径）
python3 wilviewer.py --root /path/to/mir3ei --port 8765 --open
```

浏览器打开 http://127.0.0.1:8765 ，功能：

- **左侧**：全部 83 个图库按分类分组（怪物动画 / 角色与装备外观 / 物品图标 / 地图贴图 / NPC / 魔法特效 / 坐骑 / UI 界面 / 其他），显示帧数、文件大小；支持文件名搜索
- **网格页**：无限滚动加载（滚到底自动追加），调整列数与放大倍率（缩略图随放大 NEAREST 像素风放大）
- **点击任意格**：弹窗显示大图（×4 最近邻放大）、尺寸 / 锚点 / 阴影 / 数据量元信息，可导出 PNG（原尺寸 / ×4）
- **▶ 动画**：选取起始帧 + 帧数 + fps，在线生成 GIF 动画（怪物/装备动作）
- **声音页签**：Sound/*.wav 全部 742 个在线播放

### 命令行导出

```bash
# 单个图库 → 逐帧 PNG
python3 wilextract.py Data/storeitem.wil -o out

# 帧范围 + 元数据 sidecar
python3 wilextract.py Data/Mon-1.wil -r 0-360 -o mon1 --meta

# 拼图（montage）单张 PNG
python3 wilextract.py Data/Inventory.wil --sheet --cols 20 --scale 3 -o sheet.png

# 整个 Data/ 目录批量导出（每库一个子目录；91 万帧全量会很久，建议加 -r）
python3 wilextract.py Data --all -o out -r 0-100
```

### 作为库使用

```python
import sys; sys.path.insert(0, 'tools')
import wilsdk

lib = wilsdk.WilLibrary('Data/storeitem.wil')   # 自动找同名 .wix
print(lib.count)                                 # 帧数
hdr = lib.header(1)                              # {width, height, offsetX, offsetY, shadow, words, ...}
img = lib.decode(1)                              # PIL RGBA Image（透明底）
img.save('frame1.png')

# 全部图库
for lib in wilsdk.scan_libraries('Data'):
    print(lib.name, lib.count)
```

## WIL 格式要点（实现依据 Zircon/LibraryEditor/WeMadeLibrary.cs）

- **.wix** 索引：26 字节头 + uint16 魔数 `0xB13A`，之后每 4 字节一个 int32 图像偏移；偏移为 0 = 空白占位帧
- **.wil** 图像：17 字节头（int16 宽高/锚点 + 阴影标记 + int16 阴影偏移 + int32 数据字数）+ RLE 数据（字节数 = 字数×2）
- **RLE**：每行开头 uint16 为累计终点；opcode `0xC0` 跳过 / `0xC1` `0xC3` 实色 / `0xC2` overlay（颜色+掩码双层）；16bit RGB565 像素，0 = 透明，扫描线自上而下
- 图库分类规则见 `wilsdk.categorize()`，可自行调整

## 常见问题

- **慢？** 地图大图库（Tiles*/object*）单帧解码 10-130ms，翻页稍等即可；动画建议选小帧数
- **空白格**：索引为 0 的偏移是占位帧，属正常
- **端口占用**：`--port 其他数字`
- **跨平台**：Linux/macOS/Windows 均可，仅需 Python 3.8+ 与 Pillow
