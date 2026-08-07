using System;
using Godot;
using Library;

namespace ZirconClient.Scripts;

/// <summary>
/// 鼠标按住走路/跑步 (移植自 Client/Scenes/Views/MapControl.cs ProcessInput + MouseDirection)。
/// 独立节点, 挂在 GameScene 下, 与 MapView 同世界坐标系。不修改 MapView。
///
/// 原版行为:
///   左键按住 -> 朝鼠标方向走 (Distance=1)
///   右键按住 -> 朝鼠标方向跑 (Distance=2, 含负重/骑马可更多; 此处先固定 2)
///   Shift+左键 -> 原地攻击 (此处只处理移动, 攻击交给战斗组件)
///
/// 方向算法 (原版 MouseDirection 22.5° 划分):
///   玩家恒在视野中心 (MapView 居中渲染)。鼠标相对玩家的角度按 22.5° 分成 8 份。
///   注意 CellWidth=48 != CellHeight=32, 必须先归一化到"格"单位再算角度, 否则对角线方向会偏。
/// </summary>
public partial class MouseWalker : Node2D
{
    // 与 MapView 完全一致的常量 (渲染不变量, 复制而非引用, 避免修改 MapView)
    private const float CellWidth = 48f;
    private const float CellHeight = 32f;
    private const float WorldScale = 2f;
    private const int ManualHeightOffset = 34;

    private readonly MapView _mapView;
    private readonly Action<MirDirection, int> _sendMove;  // (方向, 距离) -> C.Move
    private readonly Func<bool> _blockLeftWalk;  // 返回 true 时左键不走路 (鼠标下有可点物体)
    private readonly Func<int> _getRunSteps;  // 返回当前可跑步数 (1=走, 2=负重允许跑, 3=骑马); null=默认2

    // 移动节流: 原版按 MoveFrame (约 100ms/格) 发包, 防止刷爆服务端
    private const double WalkIntervalMs = 100.0;
    private const double RunIntervalMs = 110.0;
    private double _nextSendMs;
    public bool Enabled = true;  // GameScene 可在登录前关掉
    public bool AutoRun;  // D 键切换; 开启时左键也跑步

    public MouseWalker(MapView mapView, Action<MirDirection, int> sendMove, Func<bool> blockLeftWalk = null, Func<int> getRunSteps = null)
    {
        _mapView = mapView;
        _sendMove = sendMove;
        _blockLeftWalk = blockLeftWalk;
        _getRunSteps = getRunSteps;
    }

    public override void _Process(double delta)
    {
        if (!Enabled || _mapView?.Map == null) return;

        // 父节点 (GameScene) 是 Control 且 Scale=WorldScale; 鼠标全局位置已是 GameScene 局部像素。
        // 转回世界坐标: 除以 WorldScale。
        // 用父节点的 GetGlobalMousePosition 以兼容嵌套。
        Control parent = GetParent() as Control;
        if (parent == null) return;

        Vector2 mouseScreen = parent.GetGlobalMousePosition();
        Vector2 mouseWorld = mouseScreen / WorldScale;

        bool leftDown = Input.IsMouseButtonPressed(MouseButton.Left);
        bool rightDown = Input.IsMouseButtonPressed(MouseButton.Right);
        // Shift 按住 = 原地攻击, 不走
        bool shiftDown = Input.IsKeyPressed(Key.Shift);
        if (shiftDown) return;

        if (!leftDown && !rightDown) return;

        // 左键但鼠标下有可点物体 (怪物/NPC/物品) -> 让 CombatController 处理选中, 不走路
        if (leftDown && _blockLeftWalk != null && _blockLeftWalk())
            return;

        double now = Godot.Time.GetTicksMsec();
        int steps = _getRunSteps?.Invoke() ?? 2;
        bool run = rightDown || AutoRun;
        int distance = run ? steps : 1;
        double interval = run ? RunIntervalMs : WalkIntervalMs;
        if (now < _nextSendMs) return;

        MirDirection target = ComputeDirection(mouseWorld);
        // 撞墙绕路: 正方向走不通时找相邻可行方向 (复刻原版 MouseDirectionBest)
        MirDirection dir = BestWalkDirection(target);
        _sendMove(dir, distance);
        _nextSendMs = now + interval;
    }

    /// <summary>
    /// 鼠标世界坐标 -> 8 方向之一。复刻原版 MouseDirection 的 22.5° 划分。
    /// 玩家恒居中: 屏幕中心格的屏幕像素 = CellToScreen(CenterX, CenterY, true)。
    /// </summary>
    private MirDirection ComputeDirection(Vector2 mouseWorld)
    {
        // 玩家屏幕位置 (世界坐标): 用 MapView 的居中公式反推玩家自己格子的屏幕坐标
        // CellToScreen(CenterX, CenterY, true) 即玩家中心。为不依赖 MapView 内部常量,
        // 我们直接调用它的 public CellToScreen (它用玩家自己的 CenterX/Y 算)。
        Vector2 playerWorld = _mapView.CellToScreen(_mapView.CenterX, _mapView.CenterY, true);

        // 归一化到"格"单位 (消除 48x32 的宽高比), 再算角度
        float dx = mouseWorld.X - playerWorld.X;
        float dy = mouseWorld.Y - playerWorld.Y;
        float gx = dx / CellWidth;
        float gy = dy / CellHeight;

        // 鼠标几乎压在玩家身上 -> 不发方向 (原版同样跳过)
        if (Math.Abs(gx) < 0.15f && Math.Abs(gy) < 0.15f)
            return _lastDir;  // 保持上一次方向, 避免抖动

        // atan2 返回弧度; 原版以"正上=0°, 顺时针"为约定 (MirDirection.Up=0, UpRight=1, ...)
        // Godot 屏幕坐标 Y 向下为正, 所以正上方向 dy<0。用 atan2(gx, -gy) 让正上=0, 顺时针递增。
        double angle = Math.Atan2(gx, -gy) * 180.0 / Math.PI;  // [-180, 180], 正上=0
        if (angle < 0) angle += 360.0;

        // 每 45° 一个方向, 但原版以 22.5° 为分界 (即 0° 中心±22.5° 都是 Up)
        // (int)((angle + 22.5) / 45) % 8
        int idx = (int)Math.Floor((angle + 22.5) / 45.0) & 7;
        _lastDir = (MirDirection)idx;
        return _lastDir;
    }

    /// <summary>该格是否可行走 (边界 + MapCell.Flag)。复刻原版 CanMove。</summary>
    private bool CanMove(int x, int y)
    {
        var map = _mapView.Map;
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height) return false;
        return map.Cells[x, y].Flag;
    }

    /// <summary>
    /// 玩家在 CenterX/CenterY, 朝 dir 走 distance 格, 途经任一格阻挡则不可行。
    /// 复刻原版 CanMove(direction, distance)。
    /// </summary>
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

    /// <summary>
    /// 正方向走不通时, 按 22.5° 偏角找最近可行相邻方向。
    /// 复刻原版 MouseDirectionBest: 先试 dir, 不行试 ShiftDirection(dir, ±1), 再 ±2。
    /// 全都不行就原地转身 (返回 dir, 发 Move 会被服务端拒, 但转身方向要发)。
    /// </summary>
    private MirDirection BestWalkDirection(MirDirection target)
    {
        if (CanMove(target, 1)) return target;

        // ±1 (45° 偏)
        MirDirection left = Functions.ShiftDirection(target, -1);
        if (CanMove(left, 1)) return left;
        MirDirection right = Functions.ShiftDirection(target, 1);
        if (CanMove(right, 1)) return right;

        // ±2 (90° 偏)
        MirDirection left2 = Functions.ShiftDirection(target, -2);
        if (CanMove(left2, 1)) return left2;
        MirDirection right2 = Functions.ShiftDirection(target, 2);
        if (CanMove(right2, 1)) return right2;

        // 全堵 -> 返回 target (服务端会拒/玩家转身), 与原版一致
        return target;
    }

    private MirDirection _lastDir = MirDirection.Down;
}