# 怪物死亡尸体保留与清理机制修复总结

## 1. 修复内容总结

在 [`GameScene.cs`](file:///home/tetsuya/development/Zircon/GodotClient/Scripts/GameScene.cs#L6890-L6915) 的 `OnObjectDied` 回调函数中：
- **彻底删除了客户端私下设置的 1.2 秒 `GetTree().CreateTimer(1.2).Timeout` 销毁定时器**。
- 怪物或玩家死亡时，客户端**仅切换为 `Dead = true` 状态并播放倒下死亡动画与声音**，保留尸体平躺在地面上。
- 尸体的物理消除由服务端权威控制：当服务端的 `DeadTime` 到期（普通怪物默认保留 20 秒至 1 分钟）广播 `S.ObjectRemove` 数据包时，客户端才在 `OnObjectRemove` 中统一移除释放尸体节点。

---

## 2. 效果与传奇原版完全一致

1. **尸体留存**：击杀怪物后，怪物会保持死亡动作倒在地面上，不再瞬间蒸发消失。
2. **交互支持**：尸体保留期间，支持玩家点击高亮选中及屠宰/割肉（Meat Harvest）等原版机制。
3. **权威清理**：只有当服务端认为尸体生命周期结束广播 `ObjectRemove` 包后，尸体才会自然被消除。
