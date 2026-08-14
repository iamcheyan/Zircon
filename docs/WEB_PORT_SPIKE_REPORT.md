# Godot 客户端 Web 完整移植 — 阶段0 可行性 Spike 报告

日期：2026-08-14 · 执行环境：Linux 6.12 / Godot 4.6.3 stable mono (7d41c59c4) / dotnet 10.0.302 / Xvfb :100 + headless Chromium

---

## 总裁决（先读这里）

| 关卡 | 结论 | 一句话 |
|---|---|---|
| 关卡1 Godot 4.6.3 mono Web 导出 | ❌ **不可行（官方工具链硬阻断）** | C#→Web 在 Godot 4 全系被编辑器拒绝，4.6.3 mono 导出模板根本不含 web 模板；替代路径 **GDScript 壳 Web 导出已实测可行**（浏览器内逻辑运行证据 + 截图） |
| 关卡2 资源瘦身管线 | ✅ 可行且收益明确 | Interface.Zl 4.71MB→lossless WebP 2.05MB；全量资源预估 8.0GB→**3.7GB (lossless) / ~2.1GB (q90)**，音频 OGG 化 10.5× |
| 关卡3 WebSocket 网关 | ✅ 可行 | 独立 WS:7001→TCP:7000 透传网关实测通过，登录包被服务器接受，RTT 开销见 §3 |

**阶段1 修正方向：放弃"同一份 C# 编译两次"的原始构想，改为「GDScript 渲染壳（Web/桌面同一份）+ C# 逻辑库（桌面直接引用，Web 侧待 .NET WASM 成熟或用 GDScript 重写热路径）」或「C# 逻辑抽 .NET 库 + PixiJS 壳」。详见 §1.4 替代方案对比表——推荐方案 B。**

---

## 关卡1：Godot 4.6.3 mono 的 Web 导出可行性 ⭐最高风险

### 1.1 结论：不可行——两重硬阻断，均为上游官方行为

**阻断①（工具链层）：mono 导出模板不含 Web 模板。**
`Godot_v4.6.3-stable_mono_export_templates.tpz`（1.1GB，2026-05-20 官方 release）27 个条目全部清点：android/linux/macos/windows/ios，**没有任何 `web_*.zip`**。Web 导出模板只存在于标准版（非 mono）模板包。

**阻断②（编辑器层）：Web preset + C# 项目被无条件拒绝，错误先于模板查找。**
最小 C# 项目（1 个 Label + 1 行 `_Ready` 赋值，`Godot.NET.Sdk/4.6.3`，net8.0，`dotnet build` 通过）执行导出：

```
$ Godot_v4.6.3-stable_mono_linux.x86_64 --headless --export-release "Web" build/web/index.html
ERROR: Cannot export project with preset "Web" due to configuration errors:
Godot 4 中目前尚不支持使用 C#/.NET 导出到 Web。要在 Web 目标上使用 C#/Mono，请改用 Godot 3。
如果这个项目不使用 C#，请使用非 C# 版本的编辑器来导出项目。
   at: _fs_changed (editor/editor_node.cpp:1332)
```

完整日志：`docs/web-spike/export_min_csharp_failed.log`

**真实项目同样被拦**（GodotClient net10.0 + LibraryCore + BCnEncoder）：同一错误、同一行号，未进入编译/打包阶段。完整日志：`docs/web-spike/export_zircon_failed.log`。测试后 `export_presets.cfg` 已还原。

**阻断③（平台层，上游路线图）：官方文档明示 C# 不支持 Web 目标**；唯一在途实现是 Draft 状态的 [godot PR #106125 "[.NET] Add web export support"](https://github.com/godotengine/godot/issues/106125)（静态链接 Mono WASM，作者自述 WIP、需手改生成 JS），无任何发布承诺。4.x 系列从 4.0 起即因 Godot wasm 模块与 .NET WASM 运行时对入口点/链接方式的占有权冲突而不支持（官方博文《Current state of C# platform support in Godot 4.2》）。

> 判定：这不是"配置不对/绕一下"的问题，是 4.6.3 官方工具链的产品边界。除非自编译引擎+模板（移植 PR #106125），C# Web 导出无路。

### 1.2 替代路径实测：GDScript 壳 Web 导出 ✅ 成功

用**标准版编辑器**（mono 版对 Web preset 一律拒绝，即使项目无 C#）+ 官方 web 模板导出同一最小项目（Label + 计数器，`JavaScriptBridge.eval` 回写验证）：

- 导出产物 37.2MB（index.wasm 37.7MB + pck/js），导出耗时 ~12s
- 用带 COOP/COEP 头的本地 http 服务加载，headless Chromium 实测：
  - `window.crossOriginIsolated === true`（SharedArrayBuffer 可用，headers 生效）
  - 游戏逻辑在浏览器内执行并由 JS bridge 回写出证据：
    `window.GD_INFO = "gdshell ok on Web, engine 4.6.3-stable (official)"`
  - 截图：`docs/web-spike/web_gdshell_browser.png`（注：headless 隐藏 tab 下 rAF 节流，帧计数器不推进属预期，`_ready` 逻辑正常执行）
- 复现脚本与工程在 `/tmp/web_spike_gdshell`（Spike 临时产物），关键参数：preset `variant/thread_support=false`；服务必须发 `Cross-Origin-Opener-Policy: same-origin` + `Cross-Origin-Embedder-Policy: require-corp`

**踩坑记录（会写进阶段1手册）**：
1. mono 编辑器导 Web：连纯 GDScript 项目也拦——必须装标准版编辑器（4.6.3 标准版模板目录 `4.6.3.stable` 与 mono 的 `4.6.3.stable.mono.official.<hash>` 不同名，symlink 可复用）。
2. **COEP 隔离页面拉跨源资源**：COOP/COEP 开启后，WASM 客户端从资源服务（如 :8821）拉图，资源服务必须回 `Cross-Origin-Resource-Policy: cross-origin`，否则浏览器直接拦（本 Spike 实测踩中，同源加载正常、跨源被 COEP 拦截）。
3. Godot 4 的 `OS.get_name()`（不是 3.x 的 `OS.GetName()`）。

### 1.3 障碍清单（真实项目 Web 化全量预判）

| # | 障碍 | 现象 | 根因 | 绕过方案 | 是否致命 |
|---|---|---|---|---|---|
| 1 | C# 代码无法进 Web 包 | 编辑器拒绝导出（§1.1） | 上游不支持 .NET WASM | 重写为 GDScript / 等 PR #106125 / 自编译引擎 | **致命（对"C#直导"）** |
| 2 | mono 模板缺 web | 模板包无 web_*.zip | 官方未发布 | 用标准版模板（仅 GDScript 可用） | 对替代路径非致命 |
| 3 | 项目引用 BCnEncoder.NET 等纯 .NET 库 | —（未到该步） | .NET 生态库多数可 AOT，但见 #1 | 阶段1选 GDScript 壳后此题消失；WebP 资源由服务端预转（关卡2 管线） | 非致命 |
| 4 | 文件系统访问 | GodotClient 直读 `Debug/Client/Data/*.Zl` | Web 无任意 FS | 按需 HTTP 拉取（关卡2 已原型） | 可解 |
| 5 | TCP 直连服务器 | 浏览器无 raw TCP | 平台限制 | WS 网关（关卡3 已原型） | 可解 |

### 1.4 替代方案对比表（阶段1路线决策依据）

| 方案 | 描述 | 保 C# 逻辑 | 工作量 | 风险 | 桌面/Web 一致性 |
|---|---|---|---|---|---|
| A. 自编译 Godot + PR #106125 | 移植 Draft PR，自维护引擎/模板 | ✅ 100% | 极高（维护引擎 fork，PR 未合） | 上游 API 漂移、.NET WASM 体积/性能未知 | ✅ 最好 |
| **B. GDScript 壳（推荐）** | 渲染/场景/UI 层迁 GDScript（Web+桌面同一份）；C# 库仅桌面保留或渐进下线 | ⚠️ 部分（逻辑需重写/移植） | 高但可控（本 Spike 已验证壳可导） | 双语言长期维护 | ⚠️ 壳一致，逻辑需纪律 |
| C. .NET 逻辑库 + PixiJS 壳 | C# 抽成纯逻辑库（协议/规则），Web 壳用 TS+PixiJS，桌面壳保留 Godot | ✅（双壳各绑） | 高（两套渲染壳） | 渲染双实现漂移 | ❌ 最差 |
| D. Godot 3.x mono | 3.x 有 mono Web 导出 | ✅ | 全项目降级 3.x，API 大改 | 引擎古老、WebGL1、弃维护 | ✅ 但整体倒退 |

**推荐 B**：唯一同时满足"官方工具链可导 Web（本 Spike 实证）+ 桌面 Web 同源数据（ui_overlay.json/System.db/WebP manifest 均与引擎语言无关）"。阶段1 第一里程碑：LoginScene 用 GDScript 重写并在浏览器渲染真实 Interface1c 资源（关卡2 管线供图）+ 经关卡3 网关完成登录。

---

## 关卡2：资源瘦身管线原型 ✅

代码：`Mir3-Research/Tools/webres/`（decode_zl_webp.py + serve.py + ESTIMATE.md）。复用 `Tools/common/zlsdk.py` 解码（Png/DXT1/BC7 全 codec 覆盖）。

### 2.1 实测数字（Interface.Zl 全量 282 帧）

| 指标 | 值 |
|---|---|
| 源 | 4.71 MB（全 Png codec） |
| lossless WebP | **2.05 MB（2.29×）** · 17.1s 编码 |
| lossy q90 | 0.87 MB（5.41×） |
| Interface1c.Zl（118.7MB DXT1）抽样外推 | lossless ≈ **15–24 MB**（纯色 UI 位图压缩比极高） |

### 2.2 按需加载原型（已验证）

FastAPI `:8821`，`/res/interface/{frame}.webp` + `/res/interface/manifest.json`。
- curl：manifest 200 application/json；帧 0/50/150 → 200 `image/webp`
- 浏览器 `<img>` 实测：3 帧解码显示，naturalWidth/Height = 1024×10 / 16×18 / 784×104 ✅
- ⚠️ 跨源（COOP/COEP 页面）需加 CORP 头，见 §1.2 踩坑 2

### 2.3 全量资源 Web 化预估（明细见 Tools/webres/ESTIMATE.md）

| 资产 | 当前 | lossless | q90 有损 |
|---|---|---|---|
| Data .Zl ×279 | 6.38 GB | 2.8–6.4 GB | 1.1–1.6 GB |
| Sound → OGG | 835 MB | **79 MB（10.5×，1578 对 wav/ogg 实测 9.86:1）** | — |
| Map（.map 原样） | 775 MB | 775 MB | — |
| **合计** | **8.0 GB** | **≈3.7 GB** | **≈2.1 GB** |

### 2.4 分阶段加载清单（登录→选人→进城）

依据：`LoginScene.cs`/`SelectScene.cs` 的 LibraryFile 引用、`GameScene.LoadMap`。

| 阶段 | 必拉资源 | 量级（Web 化后） |
|---|---|---|
| 客户端启动（引擎+wasm 壳） | index.wasm+pck（GDScript 壳，无 C# 增量） | ~38 MB（可 gzip/brotli 到 ~10） |
| 登录界面 | `Interface1c.Zl` 帧（登录框/按钮） + 字体 + System.db（账号规则） | Interface1c lossless 15–24 MB，首屏只需抽帧 → **首屏可 <5 MB** |
| 选人界面 | `Interface1c.Zl` 剩余 + `Interface.Zl` 全量 + `Background.Zl`（若用） | +2–25 MB |
| 进城（比奇） | 当前地图 `.map` + 地图背景 + `M-HumA*/WM-HumA*`（人物/装备）+ 本图 `Mon-*` + `GameInter*.Zl` + UI 库剩余 + 常用音效 OGG | 地图级按需：单图 ~1–10 MB + 精灵库按 manifest 惰性拉 |
| 长尾 | 其余 279 个 .Zl / 全部音频 | 驻留 CDN，按需 |

管线已产出 manifest（帧号→文件+尺寸+offset），即未来 WASM 客户端的拉取索引原型。

---

## 关卡3：WebSocket 网关原型 ✅

代码：`Mir3-Research/Tools/wsgateway/`（wsgateway.py + login_client.py + TEST_RESULTS.md + packet_id_dump/）。
架构：独立进程，WS :7001 ⇄ TCP 127.0.0.1:7000 纯字节透传（~150 行 Python），不动 ServerCore 一行代码。

### 3.1 实测结果：全通

| 项 | 结果 |
|---|---|
| 登录包构造 | C.Login 49B，逐字段有源码依据（`LibraryCore/Network/Packet.cs:172,182,191-193`、`ClientPackets.cs:55-60`、`ServerConnection.cs:23-31`）；帧格式 `[int32 LE 总长][int16 LE id=182][7bit变长string]`，无加密无版本握手（CheckVersion=False） |
| 服务器接受 | ✅ Python 客户端与**真实 Chromium 无头浏览器**两种客户端经 WS 网关均得 `S.Login Result=Success (code 10)`；服务器日志 `[Account Logon] Account: test@test.com, Security: 5f0e8a1b9c3d7f2a4e6b` 与发送字节逐字一致 |
| RTT（12 次均值） | 直连 TCP **3.582ms** (max 11.8) vs 经 WS **2.318ms** (max 7.663)——网关每包开销 <0.2ms，被 ServerCore 主循环 tick 抖动（两路径同分布 0.15–11ms）淹没；WS 连接建立多 ~5-40ms（HTTP Upgrade，仅登录一次） |
| 断开传播 | 双向验证：客户端断→网关关 TCP；服务器踢号 G.Disconnect(AnotherUser)→网关回 WS 1000 关闭 |
| 保活约束 | 服务器 2s 发 G.Ping 必须回，否则 20s 踢——Web 客户端需实现心跳应答 |

### 3.2 评估结论：先透传、后内嵌

- **开发期/Spike/小规模**：透传网关是正解（零侵入、延迟无损、已全链路验证）。
- **正式 Web 服**不宜长期纯透传：最大风险是 **IP 语义坍缩**——SConnection 只见网关 IP，IPCount 限流失效、Account-in-use 判定退化为仅看 CheckSum、封禁/审计全指向网关。演进路径：透传上线 → 网关↔ServerCore 传真实 IP（PROXY 协议式最小改）→ 最终给 ServerCore 内嵌 WebSocket 监听（SConnection 抽象 Stream，回归面大，放最后）。
- **浏览器限制实测**：`about:blank`（opaque origin）连 `ws://127.0.0.1` 被 Chrome Local Network Access 拒绝；页面由 `http://127.0.0.1` 提供即成功 → Web 部署需游戏页与网关同源，公网则走 `wss://`。
- packet id 注意：C.Login=182/S.Login=183 取自对**实际部署的 LibraryCore.dll** 反射导出（`packet_id_dump/`），Packet.cs 排序表手算不可靠、重建会漂移——透传网关不解析协议故不受影响。

明细：`Mir3-Research/Tools/wsgateway/TEST_RESULTS.md`（字节级构造表、日志原文、RTT 样本、踢号报文 hex）

---

## 阶段1（全量移植）修正计划

1. **壳迁移**（方案 B）：LoginScene → GDScript，浏览器内渲染真实 Interface1c 帧（关卡2 管线供图 + CORP 头）。
2. **网络**：Godot WebSocketPeer ↔ wsgateway :7001（关卡3 已验证链路）；心跳/断线重连按 TEST_RESULTS.md 评估结论处理。
3. **资源服务化**：全量 .Zl→WebP（关卡2 管线批量化）+ wav→OGG + manifest 索引；资源服务与游戏页同站或带 CORP。
4. **桌面端**：同一 GDScript 壳原生导出（4.6.3 桌面导出成熟），System.db/ui_overlay.json/WebP manifest 双端共用——单一数据源达成。
5. **C# 逻辑处置**：LibraryCore 协议封包/校验规则先以"规格文档+测试向量"形式固化为跨语言契约，再渐进移植 GDScript；桌面版过渡期可继续跑 C# 客户端。
6. **风险跟踪**：每季度复查 godot PR #106125，若合入主线则重估方案 A。

## 交付物清单

- 本报告：`zircon/docs/WEB_PORT_SPIKE_REPORT.md` + `docs/web-spike/`（截图×1、失败日志×2）
- 关卡2/3 代码：`Mir3-Research/Tools/webres/`、`Mir3-Research/Tools/wsgateway/`
- 验证状态：管线可重跑（README 命令）；:8821 服务在跑；网关/服务器按 TEST_RESULTS.md
