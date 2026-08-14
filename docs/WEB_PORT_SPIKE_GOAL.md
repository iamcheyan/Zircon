# Godot 客户端 Web 完整移植 — 阶段0 可行性 Spike — 完整任务目标

## 一、终极愿景（用户拍板的架构）

**同一份 C# 代码库（GodotClient）编译两次**：桌面原生版 + WebAssembly 版。
用户打开浏览器 URL 就能玩真实游戏（连真服务器、真数据、真资源）。
数据和 Godot 完全通用——System.db、Zl 资源、ui_overlay.json（UI 编辑器产物）
都是单一来源，**调 UI 位置两端同时生效**。Web 只是壳子，底层逻辑一份。

```
        GodotClient (C#, Godot 4.6.3 mono)
       ↙ 原生导出                    ↘ Web 导出 (WASM + WebGL2)
 桌面/无头客户端                      浏览器 PWA
       ↘                              ↙
    同一份 Debug/Client 资源(瘦身后) + ui_overlay.json
       ↘                              ↙
            ServerCore :7000 (TCP) :7001 (WebSocket 新增)
```

## 二、本 goal 范围 = 阶段0 Spike（验证三关卡，不做全量）

### 关卡1：Godot 4.6.3 mono 的 Web 导出可行性 ⭐最高风险
1. 用 /tmp/godot-mono 的编辑器 CLI 或下载 web 导出模板
   （`Godot_v4.6.3-stable_mono_export_templates.tpz`，~1GB 内）
2. **先导最小 C# 项目**：新建空 Godot C# 项目（一个 Label + 一行 C# 逻辑），
   导出 HTML5 → 本地起 http 服务（python3 -m http.server，**必须带 COOP/COEP
   response headers**——.NET WASM 需要 SharedArrayBuffer：
   `Cross-Origin-Opener-Policy: same-origin` + `Cross-Origin-Embedder-Policy: require-corp`）
   → 无头浏览器（Xvfb :100 + chromium 或 browser CDP）加载 → 截图证明 C# 逻辑
   在浏览器里跑起来了（/tmp/web_spike_min.png）
3. **再导 GodotClient 子集**：如果最小项目成功，尝试导真实项目（只求编译通过+
   浏览器里到登录界面渲染，哪怕资源加载失败也记录具体报错）。
   预期障碍逐个记录：导出模板缺 mono Web 支持？C# API 不兼容？文件系统访问？
   每个障碍记录「现象/根因/绕过方案/是否致命」
4. **结论明确**：如果 4.6.3 mono Web 导出不可行，给出替代路径评估
   （升级 4.x 新版？降级 GDScript 壳+C# 逻辑库？C# 逻辑抽 .NET 库+PixiJS 渲染壳？）
   ——**这一条结论比任何代码都重要**

### 关卡2：资源瘦身管线原型
1. 写 `Tools/webres/`（Mir3-Research）：zlsdk 批量解码 Interface.Zl 的指定帧范围
   → PIL → cwebp/l无损 WebP → `Debug/Client/WebData/interface/` 目录
   （帧号.webp），生成 manifest.json（帧号→文件+尺寸）
2. 产出量级报告：Interface.Zl 全量转 WebP 后多大？压缩比？（Zl 的 BC7/DXT
   解出来是 PNG 再 WebP，通常 5-10x 压缩）
3. 按需加载原型：FastAPI :8821 起 `/res/interface/{frame}.webp` 静态服务，
   浏览器 img 拉几帧验证（这是未来 WASM 客户端拉资源的同款模式）
4. 全量估算：9G 资源（Data 7.4G+Map 775M+Sound 835M）全部 WebP/OGG 化后
   预计多大？登录→选人→进城各阶段需要拉哪些文件（列清单）？

### 关卡3：WebSocket 网关原型
1. ServerCore 侧：不动 C# 服务器——先写**独立网关进程**
   `Tools/wsgateway/`（Python asyncio 或 C# console）：监听 :7001 WebSocket，
   每个 WS 连接对应一条到 127.0.0.1:7000 的 TCP 连接，双向透传字节流
   （不做协议解析，纯 pipe）
2. 测试：Python websocket 客户端连 7001 → 发登录包（从 GodotClient C# 源码
   抄 Login 包的字节构造）→ 服务器日志出现登录尝试即通
3. 评估：透传延迟（localhost 实测 ping-pong RTT）；后续要不要内嵌进 ServerCore
   （结论写报告）

### 交付物
1. `~/development/zircon/docs/WEB_PORT_SPIKE_REPORT.md`——三关卡结论+截图+
   障碍清单+阶段1（全量移植）的修正计划（如果关卡1失败给替代方案对比表）
2. Tools/webres/ + Tools/wsgateway/ 代码 commit（Mir3-Research）
3. 最小 demo 截图 + 真实项目导出尝试的完整报错记录（失败也要——这是决策依据）

## 三、验收标准

1. 关卡1有**明确结论**：可行（附浏览器截图）/ 不可行（附完整报错+替代方案表）
2. Interface.Zl → WebP 管线可重跑：manifest.json 生成，抽 3 帧浏览器可显示
3. WS 网关：Python ws 客户端发的登录包在 ServerCore 日志可见回包
4. 报告含全量资源 Web 化的体积估算表和分阶段加载清单
5. 两个仓库 commit+push（中文信息）

## 四、边界

- zircon 仓库只加 docs 报告（不动 C# 代码——阶段0 不改游戏本体）
- Mir3-Research 加两个工具目录
- /tmp/godot-mono 编辑器已在；Web 导出模板下载放 /tmp（磁盘刚清完，注意
  模板 ~1G，用完删 tpz 只留安装的）
- 无头浏览器测试用 Xvfb :100（openbox 在跑）
- 磁盘红线：新增文件总预算 <2G，结束清理 /tmp 的 tpz/中间产物

## 五、踩坑速查

- COOP/COEP headers 必须，否则 SharedArrayBuffer 被禁 → .NET WASM 白屏
- 浏览器测 http 服务必须 http://（file:// 不行）
- zlsdk 解码 BC7 需要 texture2ddecoder（mir3-venv 已装）
- ServerCore 登录包结构：Client/Network 或 GodotClient/Network 的 C.Login
  构造函数（抄字节序）
- 浏览器测试用 browser_exec 的 CDP（Emulation 手机视口不需要，桌面视口即可）
