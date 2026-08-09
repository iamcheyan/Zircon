# 头顶血条显示机制分析与深度修复方案

## 1. 问题现象与排查分析

### 问题现象
玩家进入游戏或切换地图后，视野内的所有怪物和玩家头顶依然常驻显示血条，而不是“默认隐藏、受到伤害时才显示”。

### 关键漏网原因
之前我们虽然移除了 `OnObjectPlayerStats`、`OnDataObjectMonsterInfo` 等数据包在属性初始化时的 `ShowHealthBar = true`，但忽略了服务端在玩家进入地图时会批量发送 `S.DataObjectHealthMana`（全场怪物当前血量同步数据包）。

在之前的 `OnDataObjectHealthMana` 事件处理中：
```csharp
// GameScene.cs (错误写法)
else if (_objects.TryGetValue(objectID, out var ob))
{
    ob.Health = health;
    ob.Dead = dead;
    ob.ShowHealthBar = true; // 进地图时批量触发，导致所有怪物血条全部开启！
    ob.DrawHealthUntilMs = Godot.Time.GetTicksMsec() + 5000;
}
```
因为一进入地图所有怪物都接收了这个同步包，导致所有怪物的 `ShowHealthBar` 和 5 秒倒计时被同时激活，表现为全场怪物瞬间亮起血条。

---

## 2. 原版 C# 客户端权威机制

比对原版 C# 客户端：

1. **进入地图与初始化**：
   - 收到 `S.DataObjectHealthMana` / `S.DataObjectMonsterInfo` 时**仅更新血量数值**，绝不触发 `DrawHealthTime` 显示。
2. **伤害受击触发**：
   - 仅当收到 `S.HealthChanged`（真正受到伤害/扣血）时，才设置 `DrawHealthUntilMs = Godot.Time.GetTicksMsec() + 5000`。
   - 血条在受击后的 5 秒内显示，超时自动隐匿。

---

## 3. 最终修复总结

1. **`OnDataObjectHealthMana` 数据同步**：
   彻底移除在 `OnDataObjectHealthMana`（基础血量同步）中设置 `ShowHealthBar = true` 及刷新 `DrawHealthUntilMs` 的逻辑。
2. **`OnHealthChanged` 伤害受击**：
   保留且仅在真正的受击事件 `OnHealthChanged` 中激活 `ShowHealthBar = true` 并设置 5 秒倒计时 `DrawHealthUntilMs = Godot.Time.GetTicksMsec() + 5000`。
3. **渲染层控制 (`MapObjectNode.cs` / `PlayerRenderer.cs`)**：
   在 `_Draw()` / `DrawHealthBar()` 中增加判断：只有在 `Godot.Time.GetTicksMsec() <= DrawHealthUntilMs` 且 `ShowHealthBar == true` 时才绘制血条。
