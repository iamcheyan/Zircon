# WebData — 浏览器世界观测试台资源产物

本目录为**可随时重建的构建产物**（不入库，本 README 除外），由 Mir3-Research 的
`Tools/webres/webres.py` 从客户端素材包（`.Zl`/`.map`，一字节未动）提取转换生成。
服务端 `Tools/webclient/serve.py`（FastAPI :8822）直接读本目录出静态站。

## 目录结构

| 路径 | 内容 |
|---|---|
| `data/*.json` | 地图清单(627)/NPC(294)/怪物(434)/重生点/技能(174)/物品/外观表 + `walk/{图}.bin` 行走位图 |
| `maps/{图}/{x}_{y}.webp` | 地图瓦片（512px，q85 有损）。分级方案：`_core_stems.txt` 126 张核心图离线全渲染，其余 501 张按需渲染 |
| `sprites/{库}/{帧}.webp` | 精灵帧（无损 WebP）+ `{库}/manifest.json` 帧元数据（w/h/ox/oy） |
| `maps/_estimate.json` | 全量渲染估算与分级决策记录（外推 22GB/26h+ 超 3G 预算 → 分级） |

## 重建

```bash
cd /home/tetsuya/development/Mir3-Research/Tools/webres
PY=/home/tetsuya/mir3-venv/bin/python
$PY webres.py data                              # 数据清单 (≈12s)
$PY webres.py sprites --what all                # 精灵帧批量预渲染
$PY webres.py maps --stems @Debug/Client/WebData/maps/_core_stems.txt   # 核心图离线渲染 (断点续跑)
# 其余地图无需预渲染: serve.py 按需渲染单瓦片并缓存, 30s 磁盘守卫超 3G 返回 507
```

## 启动

```bash
cd /home/tetsuya/development/Mir3-Research/Tools/webclient
/home/tetsuya/mir3-venv/bin/python serve.py     # http://127.0.0.1:8822
```
