# Godot/旧客户端渲染一致性审计（2026-08-08）

## 本次截图对应的问题

- 灰色/云状矩形：优先检查天气帧、地图前景层和技能特效是否把带背景的帧当作普通贴图绘制。
- 魔法盾施法时人物主体消失：检查技能帧是否使用了错误的黑色透明键，以及特效节点是否覆盖了主体。

## 已确认的旧端绘制语义

旧端以下调用都明确传入 `ImageType.Image`：

- `Client/Models/MirEffect.cs` 的普通特效与目标特效；
- `Client/Models/Particles/Particle.cs` 的天气粒子；
- `MirProjectile`/`SpellObject` 的技能和投射物；
- `Client/Models/Player/ExteriorEffectManager.cs` 的武器、翅膀、光环等外观层。

Godot 的普通图像路径对应 `ZlLibrary.GetImageTexture`。`GetEffectTexture` 是额外的黑色/近黑色透明键处理，不是旧端 `ImageType.Image` 的等价实现。

## 当前判定

### 技能、投射物、外观层

这些路径已改为 `GetImageTexture`：

- `MirEffectNode`；
- `MirProjectileNode`；
- `BlendLayerNode`。

Magic 830/831 和 MagicEx2 1900/1901 的逐帧检查显示，普通图像已有透明像素，而黑键路径会额外删除大量像素，符合“魔法盾内人物消失”的症状。

### 天气

天气是明确例外。ProgUse 的雪、雨、雷电、雾帧中，普通解码结果有部分帧整块不透明；旧端天气帧需要透明键才能避免整张矩形覆盖地图。因此天气继续使用：

- 雨雪雷电：`GetWeatherTexture`；
- 雾：`GetFogTexture`。

天气不能直接统一改成 `GetImageTexture`。

## 可重复验证

在仓库根目录运行：

```bash
dotnet build GodotClient/ZirconClient.csproj --no-restore
godot-mono --headless --path GodotClient --scene Scenes/MapTestScene.tscn -- --action-audit
```

应重点确认以下结果：

- `TransparencyAudit PASS`；
- `WeatherAudit PASS all=9/9`；
- `TransparencyModeAudit` 输出 Magic/MagicEx2 普通与黑键透明像素差异；
- `MagicFrameAudit PASS skills=142`；
- `ActionAudit PASS all action sequences`。

后续审计如果发现某个资源确实没有 alpha，必须按旧端的具体 `ImageType` 和资源类型加入例外，不能按“特效”名称整体启用黑键。
