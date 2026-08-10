# 网页与模拟器工具

这里负责 WIL 预览器、百科服务、网页数据生成和 800×600 客户端模拟器。

```bash
python3 Tools/web/wilviewer.py \
  --root /home/tetsuya/NAS/TMP/EI传奇3.0客户端 --port 8765
```

打开 `/ui/` 查看 UI 证据预览，打开 `/sim/` 查看客户端模拟器。模拟器页面在 `../mir3_client_simulator/`，数据由 `build_mir3_simulator_data.py` 生成。
