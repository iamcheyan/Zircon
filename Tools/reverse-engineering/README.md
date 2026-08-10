# 原版客户端逆向工具

这里的脚本以 EI 3.0 原版 `Mir3.exe`、`mir3.dat` 和 WIL/WIX 资源为主要输入，输出到 `docs/research/ei-ui-layout/`。

常用命令（在仓库根目录执行）：

```bash
python3 Tools/reverse-engineering/enrich_mir3_layout_evidence.py
python3 Tools/reverse-engineering/verify_mir3_ui_evidence.py
python3 Tools/reverse-engineering/extract_mir3_ui_layout.py --help
```

`common/wilsdk.py` 和 `common/zlsdk.py` 是共享资源解码库；根目录的同名 Python 文件只是兼容入口。
