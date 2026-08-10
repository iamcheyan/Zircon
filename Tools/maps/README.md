# 地图工具

这里负责 `.map` 解析、WIL/ZL 地图资源映射、地图审计、等距渲染、小地图和地图对比。

```bash
python3 Tools/maps/mapviewer.py \
  /home/tetsuya/NAS/TMP/EI传奇3.0客户端/Map \
  --data /home/tetsuya/NAS/TMP/EI传奇3.0客户端/Data --port 8899
python3 Tools/maps/audit_mir3_maps.py --help
python3 Tools/maps/render_map_comparison.py --help
```

地图调查结果写入 `docs/research/mir3-map-reconstruction/`，运行时缓存继续放在被忽略的本地目录。
