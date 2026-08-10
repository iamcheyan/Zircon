# Zircon

传奇 3 的正式开发仓库：服务端、共享协议库、原客户端参考代码以及 Godot 客户端。

本仓库负责可编译、可运行的产品代码，不再承载原版客户端逆向资料、WIL/WIX/DAT/MAP 解码工具或研究网页。那些内容已经拆分到独立仓库：

**Mir3-Research**：工具、研究文档、证据 JSON、地图/UI 调查和 800×600 HTML 模拟器。

## 主要项目

- `Server/`、`ServerCore/`、`ServerLibrary/`：服务端和服务端核心
- `LibraryCore/`：MirDB、SystemModels 和网络协议
- `GodotClient/`：跨平台客户端重写
- `Client/`、`RenderingCore/`：原 Windows 客户端及渲染参考
- `Launcher/`、`PluginCore/`、`Components/`：启动器和共享组件

## 资源

大型运行资源不进入 Git。请按本机情况准备 `Debug/`、`Resource/` 和原版客户端目录；研究工具和资源索引在 `Mir3-Research` 仓库维护。

## 构建

请使用仓库中的 `Zircon Server.sln`，并根据目标项目安装对应的 .NET SDK、Godot .NET 版和 Windows 依赖。正式客户端与服务端的运行说明应随项目代码维护；原版资源调查、地图审计和网页工具请阅读 `Mir3-Research` 的 README。

## 仓库边界

```text
Zircon           正式源码、解决方案和产品代码
Mir3-Research    逆向工具、文档、证据数据、网页和模拟器
NAS              原版客户端与大型资源，不纳入 Git
```
