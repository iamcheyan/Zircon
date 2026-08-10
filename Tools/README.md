# 工具导航

`Tools/` 是项目工具区，不是正式客户端/服务端源码。工具按用途分为以下几类；现阶段保留原有入口路径，避免破坏已有命令，后续再逐步迁移到子目录并提供兼容入口。

## 逆向与资源

- `disasm_mir3_ui.py`、`extract_mir3_*.py`、`analyze_mir3_*.py`：原版 EXE、窗口、控件和资源证据提取。
- `wilsdk.py`、`zlsdk.py`、`wilextract.py`、`wilviewer.py`：WIL/WIX/ZL 解码和可视化。
- `decode_mir3_exp.py`、`parse_mir3_magic_exp.py`：DAT/经验/技能数据调查。

## 地图

- `mapviewer.py`：地图浏览和渲染。
- `audit_mir3_maps.py`、`build_map_catalog.py`、`check_map_resource_consistency.py`：逐图审计和资源一致性。
- `render_map_comparison.py`、`diagnose_map_glitch.py`：地图对比和异常诊断。
- `gen_minimap_ei.py`、`extract_mir3_minimap_server_crossref.py`：小地图调查。

## 数据与网页

- `build_content_catalog.py`、`build_ei_map_catalog.py`：内容百科和地图目录生成。
- `build_mir3_simulator_data.py`：生成网页模拟器使用的数据模型。
- `mir3_client_simulator/`：800×600 HTML 客户端模拟器。
- `WikiServer.py`、`wiki_build.py`：网页/百科生成。

## 探针与辅助程序

`AccountProbe/`、`ClientProbe/`、`ServerProbe/`、`SystemDbProbe/`、`MapFlagsProbe/`、`CharacterEditor/` 等是面向本地客户端、服务端或数据库的独立探针。

## 组织原则

新工具应优先放到明确的专题子目录；如果暂时必须放在顶层，应在本 README 中登记用途、输入、输出和是否属于正式流水线。不要把生成的 JSON、PNG、缓存和反编译中间文件直接散落在源码目录。
