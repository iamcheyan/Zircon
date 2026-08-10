# 工具导航

`Tools/` 是项目工具区，不是正式客户端/服务端源码。工具源文件按用途分区；仓库根部仍保留少量兼容转发入口，因此历史命令不会因为目录整理失效。

## 逆向与资源：`reverse-engineering/`

- `disasm_mir3_ui.py`、`extract_mir3_*.py`、`analyze_mir3_*.py`：原版 EXE、窗口、控件和资源证据提取。
- `wilextract.py`、`wil_probe.py`：WIL/WIX 资源调查。
- `enrich_mir3_layout_evidence.py`、`verify_mir3_ui_evidence.py`：UI 证据生成和验证。

共享资源解码库位于 `common/wilsdk.py` 和 `common/zlsdk.py`。

## 地图：`maps/`

- `mapviewer.py`：地图浏览和渲染。
- `audit_mir3_maps.py`、`build_map_catalog.py`、`check_map_resource_consistency.py`：逐图审计和资源一致性。
- `render_map_comparison.py`、`diagnose_map_glitch.py`：地图对比和异常诊断。
- `gen_minimap_ei.py`、`map_routes.py`：小地图和地图路线。

## 内容数据：`content/`

- `build_content_catalog.py`、`build_ei_map_catalog.py`：内容百科和地图目录生成。
- `decode_mir3_exp.py`、`parse_mir3_magic_exp.py`：DAT/经验/技能数据调查。
- `dat_integrate.py`、`stores_build.py`：内容聚合。

## 网页：`web/`

- `wilviewer.py`：WIL 资源和 UI 证据预览服务，并挂载 `/sim/`。
- `WikiServer.py`、`wiki_build.py`：网页/百科生成和服务。
- `build_mir3_simulator_data.py`：生成网页模拟器使用的数据模型。
- `mir3_client_simulator/`：800×600 HTML 客户端模拟器。

## 探针与辅助程序

`AccountProbe/`、`ClientProbe/`、`ServerProbe/`、`SystemDbProbe/`、`MapFlagsProbe/`、`CharacterEditor/` 等是面向本地客户端、服务端或数据库的独立探针。

## 通用工具：`common/`

`wilsdk.py`、`zlsdk.py`、`WtlToZl.py`、版本标记和构建链脚本位于这里。

## 组织原则

新工具必须放到明确的专题子目录；顶层同名文件如果存在，只能是兼容转发入口。工具的输入、输出、依赖和运行命令应写入所在目录 README。不要把生成的 JSON、PNG、缓存和反编译中间文件直接散落在源码目录。
