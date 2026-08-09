# 走路/跑步移动回拉与卡住修复方案（给执行智能体）

> 日期：2026-08-09
> 目标：修复 Godot 客户端"走两步回拉再瞬移"和"卡在原地不动（空气墙）"两个移动缺陷。
> 本文档是**精确到行号的施工单**，执行智能体按本文改完即可，无需重新调研。
> 前置分析已由主智能体完成，见末尾"根因"节，不要推翻结论，只按本文执行。

---

## 修复要点（三处改动）

### 改动 1：GameScene 加 ServerTime 门控字段 + 发包上锁 / 回包解锁

原版 `UserObject` 有 `ServerTime` 字段，`AttemptAction` 第一行 `if (CEnvir.Now < ServerTime) return;`，发完动作设 `ServerTime = Now.AddSeconds(5)`，收到回包设 `ServerTime = DateTime.MinValue`。Godot 完全没有这个门控，是回拉/瞬移/卡住的根因。

**改 `GodotClient/Scripts/GameScene.cs`：**

**(1a) 加字段** — 在第 816 行 `private int _moveFrameCount = 1;` 之后插入一行：

```csharp
    private int _moveFrameCount = 1;
    // 原版 UserObject.ServerTime 门控: 发完一个移动请求后锁住, 等服务端回包
    // (S.ObjectMove 确认 或 S.UserLocation 纠正)才解锁, 一次只发一个 C.Move。
    // 0 = 未锁定(可发包); >0 = 锁定到该时刻。用 double 而非 DateTime 避免
    // 每帧分配。锁定期内 MouseWalker 不再发新移动, 消除预判与回包重叠。
    private double _moveServerLockUntilMs;
```

**(1b) SendMouseMove 发包后上锁** — `SendMouseMove` 在 `GodotClient/Scripts/GameScene.cs`，当前约第 7224 行。在 `_net.Connection.Enqueue(new C.Move { ... });` 这一行**之后**加一行设置锁：

找到这段（约 7224-7232 行）：
```csharp
    private void SendMouseMove(MirDirection direction, int distance, bool running)
    {
        if (_net?.Connection?.Connected != true) return;
        // 原版 UserObject.AttemptAction(Moving) 在发包前就切换本地动作；
        // 服务端回包只负责确认终点和最终距离，不能让网络往返时间表现成站立。
        _player?.BeginMove(direction, Math.Max(1, distance), _playerHorse != HorseType.None,
            running && distance >= 2);
        _net.Connection.Enqueue(new C.Move { Direction = direction, Distance = distance });
        // 原版 UserObject.AttemptAction(Moving) 在发包后立即允许下一段 Run。
        _canRun = true;
```

在 `_net.Connection.Enqueue(new C.Move { ... });` 这行之后、`_canRun = true;` 之前，插入：
```csharp
        // 原版 AttemptAction 末尾 ServerTime = Now.AddSeconds(5): 锁住直到回包。
        // 5 秒是容错上限, 正常回包几十毫秒就解锁; 超时仍解锁避免永久卡死。
        _moveServerLockUntilMs = Godot.Time.GetTicksMsec() + 5000.0;
```

**(1c) OnObjectMove 玩家分支解锁** — `OnObjectMove` 在约第 1979 行。在玩家分支（`objectID == _playerObjectID`）里，**两个出口**都要解锁。当前代码：

```csharp
    private void OnObjectMove(uint objectID, MirDirection dir, System.Drawing.Point loc, int distance,
        TimeSpan slow = default, bool mapChanged = false)
    {
        ClearMovementEffect(objectID);
        if (objectID == _playerObjectID)
        {
            bool autoPathActive = _autoPathRoutes.Count > 0 || _autoPathCancelPending;
            if (autoPathActive)
            {
                if (mapChanged)
                {
                    _pendingAutoPathMove = null;
                    ApplyAuthoritativePlayerLocation(loc, slow);
                    return;
                }
                _pendingAutoPathMove = new PendingAutoPathMove { ... };
                return;
            }
            _canRun = true;
            _mouseWalker?.AddMoveDelay(slow);
            CallDeferred(nameof(ShowUserLocation), (int)dir, loc.X, loc.Y, Math.Max(1, distance));
            return;
        }
```

在 `if (objectID == _playerObjectID)` 块的**最开头**（`bool autoPathActive = ...` 之前）加一行解锁——因为无论走哪个出口（autoPath / mapChanged / 普通），回包都到了，都该解锁：

```csharp
        if (objectID == _playerObjectID)
        {
            _moveServerLockUntilMs = 0;  // 收到移动回包, 解除 ServerTime 门控
            bool autoPathActive = _autoPathRoutes.Count > 0 || _autoPathCancelPending;
            ...
```

**(1d) OnUserLocation 解锁** — `OnUserLocation` 在约第 1797 行。服务端拒绝移动会回 `S.UserLocation`，这也是回包，同样要解锁。当前：

```csharp
    private void OnUserLocation(MirDirection dir, System.Drawing.Point loc)
    {
        if (_player == null) return;
        _playerDirection = dir;
        _player.Direction = dir;
        ApplyAuthoritativePlayerLocation(loc);
        _player.PlayStandingForState();
        _canRun = IsRunInputHeld();
    }
```

在 `if (_player == null) return;` 之后加一行：
```csharp
        _moveServerLockUntilMs = 0;  // 服务端纠正(拒绝移动), 同样解除门控
```

**(1e) ApplyAuthoritativePlayerLocation 也要解锁** — 该方法在约第 3057 行，被多个回包路径调用（OnUserLocation、mapChanged 分支、各种 S.Object*）。为稳妥，在其内部也设解锁，覆盖所有经过它的纠正路径。当前：

```csharp
    private void ApplyAuthoritativePlayerLocation(System.Drawing.Point loc, TimeSpan slow = default)
    {
        if (_player == null) return;
        _playerLocation = loc;
        _pendingDistance = 1;
        _moveFrameCount = 1;
        ...
```

在 `if (_player == null) return;` 之后加：
```csharp
        _moveServerLockUntilMs = 0;  // 权威位置应用即解锁, 覆盖所有纠正路径
```

> 注意：1c、1d、1e 三处都设 `_moveServerLockUntilMs = 0` 是有意冗余——确保任何回包出口都解锁，不依赖单一路径。设 0 是幂等的，不会出错。

---

### 改动 2：MouseWalker 加 ServerTime 门控 + CanMove 基准改用权威 _playerLocation

**改 `GodotClient/Scripts/MouseWalker.cs`：**

**(2a) 加两个闭包字段** — 在第 38 行 `private readonly Func<bool> _blockRightMouse;` 之后插入：

```csharp
    private readonly Func<bool> _blockRightMouse;
    // 原版 ServerTime 门控: 返回 true = 正在等服务端回包, 本帧不发移动/不判阻挡。
    private readonly Func<bool> _awaitingServer;
    // 原版 CanMove 用 User.CurrentLocation(权威格子)做起点; Godot 相机基准 CenterX/Y
    // 由回包驱动且经 CallDeferred 延迟一帧, 与真实玩家位置有窗口期不同步, 会误判阻挡
    // (空气墙)。改用权威 _playerLocation 做起点判定。null 时回退到 CenterX/Y。
    private readonly Func<System.Drawing.Point> _playerCell;
```

**(2b) 构造函数加参数** — 当前构造函数（约第 56-74 行）：

```csharp
    public MouseWalker(MapView mapView, Action<MirDirection, int, bool> sendMove,
        Func<bool> blockLeftWalk = null, Func<int> getRunSteps = null,
        Action<MirDirection> sendTurn = null, Func<bool> mouseOverUi = null,
        Func<int, int, bool> cellBlocked = null, Func<bool> movementAllowed = null,
        Func<bool> turnAllowed = null, Func<bool> blockLeftMouse = null,
        Func<bool> blockRightMouse = null)
    {
        _mapView = mapView;
        _sendMove = sendMove;
        _blockLeftWalk = blockLeftWalk;
        _getRunSteps = getRunSteps;
        _sendTurn = sendTurn;
        _mouseOverUi = mouseOverUi;
        _cellBlocked = cellBlocked;
        _movementAllowed = movementAllowed;
        _turnAllowed = turnAllowed;
        _blockLeftMouse = blockLeftMouse;
        _blockRightMouse = blockRightMouse;
    }
```

改为（在末尾加两个可选参数 + 赋值）：

```csharp
    public MouseWalker(MapView mapView, Action<MirDirection, int, bool> sendMove,
        Func<bool> blockLeftWalk = null, Func<int> getRunSteps = null,
        Action<MirDirection> sendTurn = null, Func<bool> mouseOverUi = null,
        Func<int, int, bool> cellBlocked = null, Func<bool> movementAllowed = null,
        Func<bool> turnAllowed = null, Func<bool> blockLeftMouse = null,
        Func<bool> blockRightMouse = null, Func<bool> awaitingServer = null,
        Func<System.Drawing.Point> playerCell = null)
    {
        _mapView = mapView;
        _sendMove = sendMove;
        _blockLeftWalk = blockLeftWalk;
        _getRunSteps = getRunSteps;
        _sendTurn = sendTurn;
        _mouseOverUi = mouseOverUi;
        _cellBlocked = cellBlocked;
        _movementAllowed = movementAllowed;
        _turnAllowed = turnAllowed;
        _blockLeftMouse = blockLeftMouse;
        _blockRightMouse = blockRightMouse;
        _awaitingServer = awaitingServer;
        _playerCell = playerCell;
    }
```

**(2c) _Process 加门控** — 在 `_Process` 里 `if (now < _nextSendMs) return;`（第 124 行）**之后**插入 ServerTime 门控。这要在所有输入判定之后、移动判定之前：

找到第 119-124 行：
```csharp
        double now = Godot.Time.GetTicksMsec();
        int steps = _getRunSteps?.Invoke() ?? 2;
        bool run = rightDown || autoRun;
        int distance = run ? steps : 1;
        double interval = run ? RunIntervalMs : WalkIntervalMs;
        if (now < _nextSendMs) return;
```

在 `if (now < _nextSendMs) return;` 之后加一行：
```csharp
        // 原版 AttemptAction: if (Now < ServerTime) return; 一次只发一个移动, 等回包。
        if (_awaitingServer?.Invoke() == true) return;
```

**(2d) CanMove(dir, distance) 基准改用权威格子** — 当前（第 207-216 行）：

```csharp
    private bool CanMove(MirDirection dir, int distance)
    {
        int px = _mapView.CenterX, py = _mapView.CenterY;
        for (int i = 1; i <= distance; i++)
        {
            var p = Functions.Move(new System.Drawing.Point(px, py), dir, i);
            if (!CanMove(p.X, p.Y)) return false;
        }
        return true;
    }
```

改为（用 _playerCell 闭包取权威位置，回退到 CenterX/Y）：

```csharp
    private bool CanMove(MirDirection dir, int distance)
    {
        // 原版用 User.CurrentLocation(权威格); Godot CenterX/Y 由回包驱动且
        // 经 CallDeferred 延迟, 与真实位置有窗口期不同步 → 误判空气墙。
        var cell = _playerCell?.Invoke() ?? new System.Drawing.Point(_mapView.CenterX, _mapView.CenterY);
        int px = cell.X, py = cell.Y;
        for (int i = 1; i <= distance; i++)
        {
            var p = Functions.Move(new System.Drawing.Point(px, py), dir, i);
            if (!CanMove(p.X, p.Y)) return false;
        }
        return true;
    }
```

**(2e) ComputeDirection / BestWalkDirection / IsMouseWithinCells 的 playerWorld 基准** — 这三处用 `_mapView.CellToScreen(_mapView.CenterX, _mapView.CenterY, false)` 算玩家屏幕中心（用于角度计算）。这些是**方向计算**，不是阻挡判定，用相机基准即可（玩家恒居中渲染，CenterX/Y 是视觉中心，适合算方向）。**这三处不改**——只有 `CanMove(dir, distance)` 的阻挡判定需要权威格。注释里把 `玩家在 CenterX/CenterY` 那行注释改一下即可（第 204 行）：

把第 203-206 行的注释：
```csharp
    /// <summary>
    /// 玩家在 CenterX/CenterY, 朝 dir 走 distance 格, 途经任一格阻挡则不可行。
    /// 复刻原版 CanMove(direction, distance)。
    /// </summary>
```
改为：
```csharp
    /// <summary>
    /// 玩家朝 dir 走 distance 格, 途经任一格阻挡则不可行。
    /// 复刻原版 CanMove(direction, distance); 起点用权威 _playerLocation(原版用 User.CurrentLocation)。
    /// </summary>
```

---

### 改动 3：GameScene 构造 MouseWalker 时传入两个新闭包

`MouseWalker` 构造在 `GodotClient/Scripts/GameScene.cs` 约第 942-955 行。当前：

```csharp
        _mouseWalker = new MouseWalker(_mapView, SendMouseMove,
        () => _combatController?.MouseObject != null
            && (_combatController.MouseObject.Type == ObjectRenderer.Kind.Item
                || (!_combatController.MouseObject.Dead
                    && !(_combatController.MouseObject.Type == ObjectRenderer.Kind.Monster
                        && !string.IsNullOrWhiteSpace(_combatController.MouseObject.PetOwner)))),
        GetRunSteps,
        SendTurn,
        () => IsMouseOverUi(),
        IsMovementCellBlocked,
        CanPlayerMove,
        CanPlayerTurn,
        BlockLeftMouseMovement,
        () => Input.IsKeyPressed(Key.Ctrl) && _combatController?.MouseObject?.Type == ObjectRenderer.Kind.Player);
        AddChild(_mouseWalker);
```

在最后一个参数 `() => Input.IsKeyPressed(Key.Ctrl) && ...` 之后、`)` 之前，加两个闭包参数：

```csharp
        () => Input.IsKeyPressed(Key.Ctrl) && _combatController?.MouseObject?.Type == ObjectRenderer.Kind.Player,
        // ServerTime 门控: 锁定期内 MouseWalker 不发新移动, 等服务端回包。
        () => Godot.Time.GetTicksMsec() < _moveServerLockUntilMs,
        // CanMove 阻挡判定的权威起点 = _playerLocation(原版 User.CurrentLocation)。
        () => _playerLocation);
```

---

## 验证

改完按顺序执行：

```bash
# 1. 构建 0 警告 0 错误
dotnet build GodotClient/ZirconClient.csproj --no-incremental 2>&1 | tail -5

# 2. headless 自动登录冒烟（确认没改坏进入游戏）
timeout 75s /home/tetsuya/.local/bin/godot-mono --headless --path GodotClient -- --auto-login 2>&1 \
  | grep -E "进入游戏|异常|Exception|断连|Error"
# 期望: [Game] 进入游戏! 玩家: TestHero ...  无异常/断连; exit=124 是超时正常

# 3. 走路测试（确认移动仍工作；注意标志是 --test-running, 不是 --running-test）
timeout 60s /home/tetsuya/.local/bin/godot-mono --headless --path GodotClient -- --auto-login --test-running 2>&1 \
  | grep -E "RunningTest|APPLY|SEND.*distance|RESULT|FAIL"
# 期望: 出现 INPUT -> SEND -> APPLY -> RESULT 序列, 无 FAIL; animation 为 Walking/Running
```

## 验收标准

- [ ] `dotnet build` 0 警告 0 错误
- [ ] headless 自动登录进入游戏，0 异常
- [ ] running-test 走路/跑步动画正常（Walking/Running），location 推进，无 FAIL
- [ ] 代码注释用中文，与代码库现有风格一致
- [ ] 三处改动全部落地，无遗漏

## 不要做的事

- **不要改 PlayerRenderer**（其他玩家移动逻辑与本问题无关，已验证正确）
- **不要改 MapView.CenterOn / 相机跟随逻辑**（相机每帧跟随 _player.CellX/Y 是对的）
- **不要改 _moveFrameCount 插值逻辑**（插值本身正确，问题在门控）
- **不要改 IsMovementCellBlocked**（阻挡判定逻辑正确，问题在判定基准过时，改动 2d 已解决）
- **不要重写 ShowUserLocation**（CallDeferred 是 Godot 渲染线程安全所必需，门控加上后预判/回包不再重叠）
- **不要动 CombatController**（战斗追击是独立路径）
- **不要加测试文件**（这是行为修正，用 headless 冒烟验证即可）

---

## 根因（执行智能体不必关心，仅供复习）

### 现象
1. **走两步回拉再瞬移**：见下方"第二轮：双重插值根因"——真正根因是 SendMouseMove 预判只设动画不启动插值，等回包 ShowUserLocation 才跳位置+重启插值，回包比插值快得多 → 回包到达时上一段插值还在播却被重置回起点。第一轮的 ServerTime 门控只防连发重叠，未防单段双重插值，故仍可见轻微回拉且晃动加剧。
2. **卡住不动 / 空气墙**：`MouseWalker.CanMove` 用 `_mapView.CenterX/CenterY`（回包驱动 + CallDeferred 延迟一帧）判阻挡，在"发包后、回包前"窗口用的是上一个确认位置，若该位置前方恰好被 `IsMovementCellBlocked` 误判（其他玩家 CellX/CellY 立即跳终点但视觉还在起点），进入"只转身 + 600ms 冷却"循环 → 卡住。
3. **反复发包被拒**：客户端判能走、服务端判不能走（物体位置不同步），无门控收敛 → 反复发包→被拒→拉回。

### 原版如何避免
原版 `UserObject.AttemptAction`（`Client/Models/UserObject.cs:447-450`）：
```csharp
if (CEnvir.Now < NextActionTime || ActionQueue.Count > 0) return;
if (CEnvir.Now < ServerTime) return; // 发完动作锁 5 秒, 回包才解锁
```
发完动作 `ServerTime = CEnvir.Now.AddSeconds(5)`（第 708 行）；收到回包 `ServerTime = DateTime.MinValue`（`CConnection.cs:1011` 确认 / `Displacement` 内设 `DateTime.MinValue` 拒绝）。这保证**一次只处理一个移动**，预判动画与服务端确认不重叠。

### 本修复如何对应
- 改动 1 = 复刻 ServerTime 门控（`_moveServerLockUntilMs`）
- 改动 2 = 复刻原版 CanMove 用权威 `User.CurrentLocation` 而非相机基准
- 改动 3 = 把两个门控接到 MouseWalker

三者合一：一次只发一个 C.Move → 等回包解锁 → 回包驱动 ShowUserLocation 更新权威格 → MouseWalker 基于最新权威格判 CanMove → 不重叠、不误判、不反复被拒。

---

## 实测结果（2026-08-09 主智能体实现并复核）

- `dotnet build GodotClient/ZirconClient.csproj --no-incremental`：0 警告 0 错误
  （注：`Func<System.Drawing.Point>?` 的 `?` 在本项目（无 nullable 上下文）触发 CS8632，实现时去掉了 `?`，`?.`/`??` 用法不变）
- headless 自动登录：`[Game] 进入游戏! 玩家: TestHero, 位置: (74,253)`, 0 异常
- `--test-running`（注意标志名，非 `--running-test`）：
  ```
  [RunningTest] INPUT phase=walk canRun=False steps=1 ...
  [RunningTest] SEND distance=1 running=False direction=Up
  [RunningTest] APPLY distance=1 animation=Walking frameStart=0 location=(74,252)
  [RunningTest] INPUT phase=run canRun=True steps=2 ...
  [RunningTest] SEND distance=2 running=True direction=Up
  [RunningTest] APPLY distance=2 animation=Running frameStart=0 location=(74,250)
  [RunningTest] RESULT animation=Standing frame=0 location=(74,250)
  ```
  SEND → APPLY 严格串行（锁→回包解锁→下一个），walk 1 格 / run 2 格位置均正确，无 FAIL。
- 尚未做真实鼠标输入下的肉眼验证（需用户在游戏内确认"回拉/瞬移/空气墙"消失）。

---

## 第二轮：双重插值根因 + 预判即插值（2026-08-09，用户反馈"仍有轻微回拉+晃动加剧"后追加）

### 根因（真正根因，第一轮未抓到）
第一轮 ServerTime 门控消除了"连发重叠"，但用户反馈仍有轻微回拉且**晃动加剧**。复核发现真正的回拉来源是**单段内的双重插值**：

- `SendMouseMove` 的本地预判只调 `_player.BeginMove(...)`（设动画+MoveDistance），**不跳 `_playerLocation`/CellX/Y、不设 Offset、不设 `_moveStartMs`/`_moveFrameCount`** → 发包后玩家视觉静止在旧格播放走/跑动画。
- 真正启动插值的是回包 `ShowUserLocation`：设 `_moveFrom=_playerLocation`、跳 `_playerLocation`/CellX/Y 到终点、设 `Offset=(旧-新)*48`（视觉回起点）、`_moveStartMs=now`、`_moveFrameCount=2` → `_Process` 开始按 `k=1-t` 插值。
- 问题：回包 RTT（局域几十 ms）**远快于**插值时长 `MovementDurationMs`（~600ms）。回包到达时，上一段插值**还在播**（k>0，Offset 在变化中），`ShowUserLocation` 把 `_moveStartMs` 重设为 now、Offset 重置回全量起点 → **视觉从当前进度突然跳回起点再走一遍** = 回拉。
- 第一轮门控串行化后，每段都经历这个"预判静止→回包重启插值回拉"，节奏更分明 → 晃动加剧。

### 原版机制对照
原版 `AttemptAction` → `SetAction(Moving)`（`MapObject.cs:3211-3212`）：**立即** `CurrentLocation = action.Location`（跳终点）+ 设 `MoveDistance`，随后 `UpdateFrame`（620-701）用 `MovingOffSet = CellSize*MoveDistance/FrameCount*(FrameCount-(frame+1))` 从终点往回算偏移（视觉=起点），逐帧偏移→0（视觉到终点）。**一次 SetAction，插值持续整段**。
原版回包 `S.ObjectMove`（`CConnection.cs:1001-1015`）：设 `ServerTime=MinValue`（解锁）、`NextActionTime+=Slow`，**正常不重 SetAction**；仅当服务端位置≠预判才 `Displacement`（重 SetAction 纠正）。Godot 之前是**每次回包都 ShowUserLocation 重启插值**——与原版相反。

### 改动（两处，GameScene.cs）

#### 改动 4：SendMouseMove 预判即插值（复刻原版 SetAction）
发包时立即跳到预测终点 + 设起点反向 Offset + 启动插值，不等回包：
```csharp
    private void SendMouseMove(MirDirection direction, int distance, bool running)
    {
        if (_net?.Connection?.Connected != true) return;
        if (_player == null) return;
        distance = Math.Max(1, distance);
        var predicted = Functions.Move(_playerLocation, direction, distance);
        _moveFrom = _playerLocation;
        _moveStartMs = Godot.Time.GetTicksMsec();
        _playerLocation = predicted;
        _player.CellX = predicted.X;
        _player.CellY = predicted.Y;
        _player.OffsetX = (_moveFrom.X - predicted.X) * 48f;
        _player.OffsetY = (_moveFrom.Y - predicted.Y) * 32f;
        _player.Direction = direction;
        _pendingDistance = distance;
        _player.BeginMove(direction, distance, _playerHorse != HorseType.None, running && distance >= 2);
        _moveFrameCount = 2;
        UpdatePlayerPosition();
        UpdateAutoPathProgress();
        _net.Connection.Enqueue(new C.Move { Direction = direction, Distance = distance });
        _moveServerLockUntilMs = Godot.Time.GetTicksMsec() + 5000.0;
        _canRun = true;
        ...
    }
```
插值从发包时刻持续到完成，回包不再重启它。

#### 改动 5：ShowUserLocation 仅在预判偏差时纠正，否则不重启插值
```csharp
    private void ShowUserLocation(int direction, int x, int y, int distance)
    {
        if (_player == null) return;
        MirDirection dir = (MirDirection)direction;
        _playerDirection = dir;
        // 预判命中: 服务端终点 == 预判终点(且距离一致) → 插值已在播, 不重启。
        if (_playerLocation.X == x && _playerLocation.Y == y && _pendingDistance == distance)
        {
            _player.Direction = dir;
            UpdateAutoPathProgress();
            ... // APPLY(confirmed) 日志 + 小地图/状态, 不动 _moveStartMs/_moveFrameCount/Offset
            return;
        }
        // 纠正路径(原版 Displacement 等价): 服务端位置≠预判, 重跳+重启插值。
        _moveFrom = _playerLocation;
        _moveStartMs = Godot.Time.GetTicksMsec();
        _playerLocation = new System.Drawing.Point(x, y);
        ... // 原逻辑
    }
```
正常情况（预判=服务端）走 confirmed 分支，插值不受干扰 → **无双重视觉，无回拉**。异常（撞墙/被推/距离被改）走 corrected 分支纠正。

### autoPath 兼容性
autoPath 模式不走 SendMouseMove（靠 OnObjectMove 回包驱动 `_pendingAutoPathMove` → `ProcessPendingAutoPathMove` → `ShowUserLocation`），`_playerLocation` 未经预判（仍为旧位置）→ ShowUserLocation 的 `loc != _playerLocation` → 走 corrected 分支（原逻辑），**autoPath 行为不变**。

### 第二轮验证
- `dotnet build`：0 警告 0 错误
- `--test-running`：
  ```
  [RunningTest] SEND distance=1 running=False direction=Up predicted=(76,247)
  [RunningTest] APPLY(confirmed) distance=1 animation=Walking frameStart=0 location=(76,247)
  [RunningTest] SEND distance=2 running=True direction=Up predicted=(76,245)
  [RunningTest] APPLY(confirmed) distance=2 animation=Running frameStart=0 location=(76,245)
  [RunningTest] RESULT animation=Standing frame=0 location=(76,245)
  ```
  **APPLY(confirmed)** 证明预判终点 = 服务端终点，ShowUserLocation 跳过了重启插值——无双重视觉。位置正确推进（248→247→245），无 FAIL。
- 仍需用户在游戏内肉眼确认回拉/晃动消失。

### 为何第一轮门控仍是必要的
第二轮消除"单段双重插值"，但"连发重叠"仍需第一轮 ServerTime 门控防止：若无门控，预判插值进行中又发第二段预判，`_moveStartMs` 被第二段重设 → 第一段插值被打断 → 同样回拉。两轮互补：门控保证一次一段，预判即插值保证每段插值不被回包打断。