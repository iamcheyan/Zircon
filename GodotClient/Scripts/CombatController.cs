using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.Network;

namespace ZirconClient.Scripts;

/// <summary>
/// 战斗交互 (移植自 Client/Scenes/Views/MapControl.cs ProcessInput 战斗部分)。
/// 独立节点, 挂在 GameScene 下。不修改 GameScene/MapView。
///
/// 职责:
///   1. 鼠标悬停 -> 高亮最近可点物体 (MouseObject)，由 GameScene/各主体渲染器绘制原版轮廓
///   2. 左键点怪物 -> 选中为 TargetObject (服务端无包, 纯客户端状态)
///   3. 选中后靠近 (距离=1) 且冷却到 -> 自动平砍 (C.Attack)
///   4. Shift + 左键 -> 原地攻击 (朝鼠标方向, 不论是否选中)
///   5. 右键 -> 取消选中 (RightClickDeTarget)
///
/// 攻击冷却: 使用原版 Globals.AttackDelay - Stats[AttackSpeed] * ASpeedRate 的本地预测，服务端回包仍是最终校验。
/// </summary>
public partial class CombatController : Node2D
{
    // 与 MapView 一致的渲染常量 (用于命中测试的坐标换算)
    private const float CellWidth = 48f;
    private const float CellHeight = 32f;
    private const double AttackIntervalMs = 800.0;

    private readonly MapView _mapView;
    private readonly Func<IReadOnlyDictionary<uint, ObjectRenderer>> _getObjects;
    private readonly Func<System.Drawing.Point> _getPlayerCell;
    private readonly Action<MirDirection, MirAction, MagicType> _sendAttack;
    private readonly Action<MirDirection, int> _sendMove;
    private readonly Func<double> _getAttackInterval;
    private readonly Func<ObjectRenderer, bool> _canRangeAttack;
    private readonly Action<MirDirection, uint> _sendRangeAttack;
    private readonly Func<int, int, bool> _cellBlocked;
    private readonly Func<bool> _rightClickDeTarget;
    private readonly Func<bool> _mouseOverUi;
    private readonly Func<bool> _canUseCombatInput;

    public bool Enabled = true;

    // 选中目标 (null=无)。原版 MapObject.TargetObject。
    public ObjectRenderer TargetObject;
    // 鼠标悬停物体 (目标高亮与点击攻击共用)
    public ObjectRenderer MouseObject;

    public static ObjectRenderer SelectLatestHit(IEnumerable<ObjectRenderer> candidates)
        => candidates?.Where(x => x != null)
            .OrderByDescending(x => x.HitOrder)
            .FirstOrDefault();

    public static bool CanAttackObject(ObjectRenderer target)
        => target != null && !target.Dead
            && target.Type is ObjectRenderer.Kind.Monster or ObjectRenderer.Kind.Player
            && (target.Type != ObjectRenderer.Kind.Monster || (target.MonsterInfo?.AI ?? -1) >= 0);

    /// <summary>
    /// 原版地图输入在玩家当前格优先处理脚下拾取；战斗控制器先收到
    /// Godot _Input，因此必须把同格的普通左键留给 GameScene._UnhandledInput。
    /// Shift 原地攻击仍由战斗分支接管。
    /// </summary>
    public static bool ShouldDeferForMapPickup(System.Drawing.Point mouseCell,
        System.Drawing.Point playerCell, bool shiftPressed)
        => !shiftPressed && mouseCell == playerCell;

    private double _nextAttackMs;

    /// <summary>
    /// 原版 CConnection.Process(ObjectRemove) 会清掉所有指向该对象的
    /// Target/Mouse 引用。切图和迟到的 ObjectRemove 都必须经过同一入口，
    /// 否则自动攻击会继续读取已经 QueueFree 的节点。
    /// </summary>
    public void RemoveObjectReference(uint objectId)
    {
        if (TargetObject?.ObjectID == objectId) TargetObject = null;
        if (MouseObject?.ObjectID == objectId) MouseObject = null;
        _nextAttackMs = 0;
        QueueRedraw();
    }

    public CombatController(MapView mapView,
        Func<IReadOnlyDictionary<uint, ObjectRenderer>> getObjects,
        Func<System.Drawing.Point> getPlayerCell,
        Action<MirDirection, MirAction, MagicType> sendAttack,
        Action<MirDirection, int> sendMove,
        Func<double> getAttackInterval = null,
        Func<ObjectRenderer, bool> canRangeAttack = null,
        Action<MirDirection, uint> sendRangeAttack = null,
        Func<int, int, bool> cellBlocked = null,
        Func<bool> rightClickDeTarget = null,
        Func<bool> mouseOverUi = null,
        Func<bool> canUseCombatInput = null)
    {
        _mapView = mapView;
        _getObjects = getObjects;
        _getPlayerCell = getPlayerCell;
        _sendAttack = sendAttack;
        _sendMove = sendMove;
        _getAttackInterval = getAttackInterval;
        _canRangeAttack = canRangeAttack;
        _sendRangeAttack = sendRangeAttack;
        _cellBlocked = cellBlocked;
        _rightClickDeTarget = rightClickDeTarget;
        _mouseOverUi = mouseOverUi;
        _canUseCombatInput = canUseCombatInput;
        SetProcessAlways();
    }

    public override void _Process(double delta)
    {
        if (!Enabled || _mapView?.Map == null) return;

        // 更新鼠标悬停物体
        MouseObject = PickObjectAtMouse();
        QueueRedraw();  // 重画悬停/选中高亮

        if (_canUseCombatInput?.Invoke() == false)
            return;

        // 原版是“按住 Shift + 左键”持续尝试攻击，而不是只在按键瞬间攻击。
        if (Input.IsKeyPressed(Key.Shift) && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            var shiftCell = _getPlayerCell();
            if (TargetObject != null && !TargetObject.Dead && IsAttackableMonster(TargetObject))
            {
                int targetDistance = Math.Max(Math.Abs(TargetObject.CellX - shiftCell.X),
                    Math.Abs(TargetObject.CellY - shiftCell.Y));
                if (targetDistance == 1)
                    TryAttack(Functions.DirectionFromPoint(shiftCell,
                        new System.Drawing.Point(TargetObject.CellX, TargetObject.CellY)));
                else if (targetDistance > 1)
                {
                    var rangeDirection = Functions.DirectionFromPoint(shiftCell,
                        new System.Drawing.Point(TargetObject.CellX, TargetObject.CellY));
                    if (_canRangeAttack?.Invoke(TargetObject) == true)
                        _sendRangeAttack?.Invoke(rangeDirection, TargetObject.ObjectID);
                    else
                        _sendMove?.Invoke(rangeDirection, 1);
                    _nextAttackMs = Godot.Time.GetTicksMsec() + 120.0;
                }
            }
            else
            {
                TryAttack(DirectionToMouse(shiftCell));
            }
            return;
        }

        // 原版 MapControl：鼠标左右键分支拥有优先级；目标自动接近只在
        // 没有鼠标移动输入时运行。点击目标的瞬间由 _Input 的
        // AttackOrApproach 处理，不能在同一帧再叠加自动移动。
        if (Input.IsMouseButtonPressed(MouseButton.Left)
            || Input.IsMouseButtonPressed(MouseButton.Right))
            return;

        // 自动攻击: 选中目标 + 距离=1 + 冷却到
        if (TargetObject == null || !IsInstanceValid(TargetObject) || TargetObject.Dead)
        {
            TargetObject = null;
            return;
        }
        if (TargetObject.Type == ObjectRenderer.Kind.Monster &&
            (TargetObject.MonsterInfo?.AI ?? -1) < 0)
        {
            TargetObject = null;
            return;
        }
        // 原版普通左键允许选中宠物，但 ProcessInput 不会对宠物自动
        // 接近/攻击；只有 Shift 分支才会继续走攻击判定。
        if (TargetObject.Type == ObjectRenderer.Kind.Monster &&
            !string.IsNullOrWhiteSpace(TargetObject.PetOwner))
            return;

        var playerCell = _getPlayerCell();
        int dist = Math.Max(Math.Abs(TargetObject.CellX - playerCell.X), Math.Abs(TargetObject.CellY - playerCell.Y));
        double now = Godot.Time.GetTicksMsec();
        if (now < _nextAttackMs) return;

        // MapControl.ProcessInput handles the player's current cell first
        // (PickUp); a target occupying that cell must not produce a local
        // attack from the independent combat loop.
        if (dist == 0) return;

        MirDirection dir = Functions.DirectionFromPoint(playerCell, new System.Drawing.Point(TargetObject.CellX, TargetObject.CellY));
        if (dist > 1)
        {
            // 选中目标后自动接近。之前这里直接 return，导致“选中了但永远不能攻击”。
            _sendMove?.Invoke(BestApproachDirection(playerCell, TargetObject), 1);
            _nextAttackMs = now + 120.0;
            return;
        }

        // 原版骑马不能进行普通近战攻击；保留目标，等待下马。
        // 坐骑状态在发送端控制，这里不重复猜测。
        // 朝目标方向砍；本地先播动作，服务端回包会再次校正。
        _sendAttack(dir, MirAction.Attack, MagicType.None);
        _nextAttackMs = now + GetAttackInterval();
    }

    public override void _Draw()
    {
        // 原版高亮属于目标主体的 DrawBody/DrawBlend，而不是一个固定
        // 48x32 的格子框；由 GameScene 将目标颜色传给 ObjectRenderer。
    }

    // 选中/攻击用鼠标点击 (不和 GameScene._Input 的键盘处理冲突: 不同输入类型)
    public override void _Input(InputEvent @event)
    {
        if (!Enabled || _mapView?.Map == null) return;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            // CombatController receives _Input before Control._GuiInput. The
            // legacy client dispatches UI clicks before MapControl, so a click
            // on an inventory/shop button must never select or attack a map
            // target. Automatic target processing in _Process intentionally
            // remains independent of this click-only guard.
            if (_mouseOverUi?.Invoke() == true) return;
            // 采集/钓鱼/驯服状态由 GameScene 的地图分支负责取消；
            // CombatController 先收到 _Input，必须先禁止攻击抢占这次点击。
            if (_canUseCombatInput?.Invoke() == false) return;

            if (mb.ButtonIndex == MouseButton.Left)
            {
                // Shift+左键 = 原地攻击 (朝鼠标方向)
                if (mb.ShiftPressed || Input.IsKeyPressed(Key.Shift))
                {
                    TryAttack(DirectionToMouse(_getPlayerCell()));
                    return;
                }

                // 原版 CanAttack 同时允许活着的怪物和其他玩家；远处先接近，
                // 近处立即攻击。怪物仍需满足 AI>=0，玩家不走该守卫。
                ObjectRenderer hit = PickObjectAtMouse();
                if (ShouldDeferForMapPickup(MouseCell(), _getPlayerCell(), false))
                    return;
                bool attackable = CanAttackObject(hit);
                if (attackable)
                {
                    TargetObject = hit;
                    GD.Print($"[Combat] 选中目标: {hit.DisplayName} ObjectID={hit.ObjectID}");
                    if (string.IsNullOrWhiteSpace(hit.PetOwner) ||
                        mb.ShiftPressed || Input.IsKeyPressed(Key.Shift))
                        AttackOrApproach(hit);
                }
                else
                {
                    // 点空地 -> 取消选中
                    TargetObject = null;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                // 原版只有启用 RightClickDeTarget 时才取消怪物目标；
                // 右键查看玩家装备不能意外清掉普通目标。
                if ((_rightClickDeTarget?.Invoke() ?? true)
                    && TargetObject?.Type == ObjectRenderer.Kind.Monster)
                    TargetObject = null;
            }
        }
    }

    private void TryAttack(MirDirection direction)
    {
        double now = Godot.Time.GetTicksMsec();
        if (now < _nextAttackMs) return;
        GD.Print($"[Combat] ATTACK direction={direction} target={TargetObject?.ObjectID ?? 0}");
        _sendAttack(direction, MirAction.Attack, MagicType.None);
        _nextAttackMs = now + GetAttackInterval();
    }

    private static bool IsAttackableMonster(ObjectRenderer target)
        => target?.Type != ObjectRenderer.Kind.Monster || (target.MonsterInfo?.AI ?? -1) >= 0;

    private double GetAttackInterval()
        => Math.Max(250.0, _getAttackInterval?.Invoke() ?? AttackIntervalMs);

    private MirDirection BestApproachDirection(System.Drawing.Point from, ObjectRenderer target)
    {
        var targetCell = new System.Drawing.Point(target.CellX, target.CellY);
        var direct = Functions.DirectionFromPoint(from, targetCell);
        if (CanStep(from, direct)) return direct;

        double angle = Math.Atan2(targetCell.X - from.X, -(targetCell.Y - from.Y)) * 180.0 / Math.PI;
        if (angle < 0) angle += 360.0;
        var best = (MirDirection)(int)(angle / 45.0);
        if (best == direct) best = Functions.ShiftDirection(direct, 1);
        var next = Functions.ShiftDirection(direct, -(int)best + (int)direct);
        if (CanStep(from, best)) return best;
        if (CanStep(from, next)) return next;
        return direct;
    }

    private bool CanStep(System.Drawing.Point from, MirDirection direction)
    {
        var next = Functions.Move(from, direction, 1);
        if (_mapView?.Map == null || next.X < 0 || next.Y < 0
            || next.X >= _mapView.Map.Width || next.Y >= _mapView.Map.Height)
            return false;
        return !_mapView.Map.Cells[next.X, next.Y].Flag
            && !(_cellBlocked?.Invoke(next.X, next.Y) ?? false);
    }

    private void AttackOrApproach(ObjectRenderer target)
    {
        var pCell = _getPlayerCell();
        int dist = Math.Max(Math.Abs(target.CellX - pCell.X), Math.Abs(target.CellY - pCell.Y));
        MirDirection dir = Functions.DirectionFromPoint(pCell,
            new System.Drawing.Point(target.CellX, target.CellY));
        double now = Godot.Time.GetTicksMsec();
        if (now < _nextAttackMs) return;
        if (dist > 1)
        {
            if (_canRangeAttack?.Invoke(target) == true)
                _sendRangeAttack?.Invoke(dir, target.ObjectID);
            else
                _sendMove?.Invoke(BestApproachDirection(pCell, target), 1);
            _nextAttackMs = now + 120.0;
        }
        else
        {
            TryAttack(dir);
        }
    }

    /// <summary>鼠标位置下方最近的可点物体 (怪物/NPC/物品), 1 格内才算命中。</summary>
    private ObjectRenderer PickObjectAtMouse()
    {
        // All map objects and this controller are children of the scaled
        // GameScene. Convert the viewport mouse point back into this node's
        // logical (48x32) coordinate space before comparing it with an object.
        // Comparing raw viewport pixels with GetGlobalTransformWithCanvas()
        // made the hit box drift when the 2x world scale/canvas transform was
        // applied.
        Vector2 mouseLocal = GetGlobalTransformWithCanvas().AffineInverse()
            * GetViewport().GetMousePosition();

        var mouseCell = _mapView.ScreenToCell(GetViewport().GetMousePosition());
        return PickObjectAt(mouseCell, mouseLocal);
    }

    /// <summary>
    /// 供在线交互审计使用：以指定逻辑格及其格中心执行同一套 CheckCursor
    /// 扫描，不直接注入 MouseObject，确保测试覆盖坐标转换和命中优先级。
    /// </summary>
    public ObjectRenderer PickObjectAtCellForAudit(System.Drawing.Point cell)
    {
        if (_mapView?.Map == null) return null;
        var candidate = SelectLatestHit(_getObjects().Values
            .Where(x => x != null && x.CellX == cell.X && x.CellY == cell.Y));
        Vector2 local = candidate?.Position ?? _mapView.CellToScreen(cell.X, cell.Y, true);
        return PickObjectAt(cell, local);
    }

    private ObjectRenderer PickObjectAt(System.Drawing.Point mouseCell, Vector2 mouseLocal)
    {

        // 原版 CheckCursor 按 d=0..3 的格子顺序扫描，而不是把所有对象
        // 收集后按距离排序。活着的对象立即返回；死亡对象/宠物和掉落物
        // 分别保留为后备命中，完全没有活对象时才使用它们。
        var objects = _getObjects();
        // 原版按每个 Cell.Objects 的逆序扫描，即最新加入/移动到该格的
        // 对象优先；全局字典顺序不能表达这个局部顺序。
        var orderedObjects = objects.Values
            .Where(x => x != null)
            .OrderByDescending(x => x.HitOrder)
            .ToArray();
        ObjectRenderer deadObject = null;
        ObjectRenderer itemObject = null;
        for (int d = 0; d < 4; d++)
        {
            for (int y = mouseCell.Y - d; y <= mouseCell.Y + d; y++)
            {
                if (y < 0 || y >= _mapView.Map.Height) continue;
                for (int x = mouseCell.X - d; x <= mouseCell.X + d; x++)
                {
                    if (x < 0 || x >= _mapView.Map.Width) continue;
                    ObjectRenderer cellSelect = null;
                    foreach (var ob in orderedObjects)
                    {
                        // 原版 CheckCursor 只排除本地玩家；其它玩家必须保留在
                        // 命中链路中，否则 Ctrl+右键观察/组队等操作永远拿不到目标。
                        if (ob == null || ob.CellX != x || ob.CellY != y)
                            continue;

                        // 非鼠标格必须实际覆盖鼠标；当前格则保留原版的
                        // cellSelect 回退，允许点击脚下格而不要求精确到像素。
                        Vector2 objectLocal = ob.Position;
                        float dx = Math.Abs(mouseLocal.X - objectLocal.X) / CellWidth;
                        float dy = Math.Abs(mouseLocal.Y - objectLocal.Y) / CellHeight;
                        float maxY = ob.Type == ObjectRenderer.Kind.Item ? 0.9f : 2.25f;
                        bool mouseOver = dx <= 1.0f && dy <= maxY;
                        if ((x != mouseCell.X || y != mouseCell.Y) && !mouseOver) continue;

                        bool deadOrPet = ob.Dead || ob.Type == ObjectRenderer.Kind.Monster && !string.IsNullOrWhiteSpace(ob.PetOwner);
                        if (deadOrPet)
                        {
                            deadObject ??= ob;
                            continue;
                        }
                        if (ob.Type == ObjectRenderer.Kind.Item)
                        {
                            itemObject ??= ob;
                            continue;
                        }
                        if (x == mouseCell.X && y == mouseCell.Y && !mouseOver)
                            cellSelect ??= ob;
                        else
                            return ob;
                    }
                    if (cellSelect != null) return cellSelect;
                }
            }
        }
        return deadObject ?? itemObject;
    }

    /// <summary>玩家格 -> 鼠标方向 (8 方向)。</summary>
    public System.Drawing.Point MouseCell()
    {
        if (_mapView?.Map == null) return _getPlayerCell();
        return _mapView.ScreenToCell(GetViewport().GetMousePosition());
    }

    /// <summary>玩家格到鼠标格的原版八方向。</summary>
    public MirDirection DirectionToMouse(System.Drawing.Point pCell)
    {
        return Functions.DirectionFromPoint(pCell, MouseCell());
    }

    private void SetProcessAlways() => ProcessMode = ProcessModeEnum.Always;
}
