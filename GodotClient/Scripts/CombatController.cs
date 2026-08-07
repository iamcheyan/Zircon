using System;
using System.Collections.Generic;
using Godot;
using Library;
using Library.Network;

namespace ZirconClient.Scripts;

/// <summary>
/// 战斗交互 (移植自 Client/Scenes/Views/MapControl.cs ProcessInput 战斗部分)。
/// 独立节点, 挂在 GameScene 下。不修改 GameScene/MapView。
///
/// 职责:
///   1. 鼠标悬停 -> 高亮最近可点物体 (MouseObject), 这一步暂只存内不画高亮 (留 UI 层)
///   2. 左键点怪物 -> 选中为 TargetObject (服务端无包, 纯客户端状态)
///   3. 选中后靠近 (距离=1) 且冷却到 -> 自动平砍 (C.Attack)
///   4. Shift + 左键 -> 原地攻击 (朝鼠标方向, 不论是否选中)
///   5. 右键 -> 取消选中 (RightClickDeTarget)
///
/// 攻击冷却: 原版用服务端 User.AttackTime; 本地暂用 250ms 近似 (服务端会再校验)。
/// </summary>
public partial class CombatController : Node2D
{
    // 与 MapView 一致的渲染常量 (用于命中测试的坐标换算)
    private const float CellWidth = 48f;
    private const float CellHeight = 32f;
    private const float WorldScale = 2f;
    private const double AttackIntervalMs = 250.0;

    private readonly MapView _mapView;
    private readonly Func<IReadOnlyDictionary<uint, ObjectRenderer>> _getObjects;
    private readonly Func<System.Drawing.Point> _getPlayerCell;
    private readonly Action<MirDirection, MirAction, MagicType> _sendAttack;

    public bool Enabled = true;

    // 选中目标 (null=无)。原版 MapObject.TargetObject。
    public ObjectRenderer TargetObject;
    // 鼠标悬停物体 (高亮用, 暂未画)
    public ObjectRenderer MouseObject;

    private double _nextAttackMs;

    public CombatController(MapView mapView,
        Func<IReadOnlyDictionary<uint, ObjectRenderer>> getObjects,
        Func<System.Drawing.Point> getPlayerCell,
        Action<MirDirection, MirAction, MagicType> sendAttack)
    {
        _mapView = mapView;
        _getObjects = getObjects;
        _getPlayerCell = getPlayerCell;
        _sendAttack = sendAttack;
    }

    public override void _Process(double delta)
    {
        if (!Enabled || _mapView?.Map == null) return;

        // 更新鼠标悬停物体
        MouseObject = PickObjectAtMouse();
        QueueRedraw();  // 重画悬停/选中高亮

        // 自动攻击: 选中目标 + 距离=1 + 冷却到
        if (TargetObject == null || TargetObject.Dead) { TargetObject = null; return; }

        var pCell = _getPlayerCell();
        int dist = Math.Max(Math.Abs(TargetObject.CellX - pCell.X), Math.Abs(TargetObject.CellY - pCell.Y));
        if (dist > 1) return;  // 不在攻击范围内, 等玩家走过去 (MouseWalker 会靠近)

        double now = Godot.Time.GetTicksMsec();
        if (now < _nextAttackMs) return;

        // 朝目标方向砍
        MirDirection dir = Functions.DirectionFromPoint(pCell, new System.Drawing.Point(TargetObject.CellX, TargetObject.CellY));
        _sendAttack(dir, MirAction.Attack, MagicType.None);
        _nextAttackMs = now + AttackIntervalMs;
    }

    public override void _Draw()
    {
        // 悬停: 黄框; 选中: 红框 (复刻原版 MapControl 的高亮)
        if (MouseObject != null && IsInstanceValid(MouseObject)) DrawBoxAround(MouseObject, new Color(1f, 0.9f, 0.2f, 0.9f));
        if (TargetObject != null && IsInstanceValid(TargetObject)) DrawBoxAround(TargetObject, new Color(1f, 0.25f, 0.2f, 0.9f));
    }

    private void DrawBoxAround(ObjectRenderer ob, Color c)
    {
        Vector2 pos = _mapView.CellToScreen(ob.CellX, ob.CellY, true);
        // 物体脚下一格大小的框 (CellWidth x CellHeight)
        DrawRect(new Rect2(pos.X - 24, pos.Y - 16, 48, 32), c, filled: false, width: 2);
    }

    // 选中/攻击用鼠标点击 (不和 GameScene._Input 的键盘处理冲突: 不同输入类型)
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Enabled || _mapView?.Map == null) return;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                // Shift+左键 = 原地攻击 (朝鼠标方向)
                if (Input.IsKeyPressed(Key.Shift))
                {
                    var pCell = _getPlayerCell();
                    MirDirection dir = DirectionToMouse(pCell);
                    double now = Godot.Time.GetTicksMsec();
                    if (now >= _nextAttackMs)
                    {
                        _sendAttack(dir, MirAction.Attack, MagicType.None);
                        _nextAttackMs = now + AttackIntervalMs;
                    }
                    return;
                }

                // 点怪物 -> 选中
                ObjectRenderer hit = PickObjectAtMouse();
                if (hit != null && hit.Type == ObjectRenderer.Kind.Monster && !hit.Dead)
                {
                    TargetObject = hit;
                    GD.Print($"[Combat] 选中目标: {hit.DisplayName} ObjectID={hit.ObjectID}");
                }
                else
                {
                    // 点空地 -> 取消选中
                    TargetObject = null;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                // 右键取消选中 (RightClickDeTarget)
                TargetObject = null;
            }
        }
    }

    /// <summary>鼠标位置下方最近的可点物体 (怪物/NPC/物品), 1 格内才算命中。</summary>
    private ObjectRenderer PickObjectAtMouse()
    {
        Control parent = GetParent() as Control;
        if (parent == null) return null;

        Vector2 mouseWorld = parent.GetGlobalMousePosition() / WorldScale;
        Vector2 playerWorld = _mapView.CellToScreen(_mapView.CenterX, _mapView.CenterY, true);

        // 鼠标相对玩家的格距离
        float gx = (mouseWorld.X - playerWorld.X) / CellWidth;
        float gy = (mouseWorld.Y - playerWorld.Y) / CellHeight;

        // 找最近的物体 (鼠标格坐标与物体格坐标距离 < 0.7 格算命中)
        ObjectRenderer best = null;
        float bestDist = 0.7f;
        var objs = _getObjects();
        foreach (var kv in objs)
        {
            var ob = kv.Value;
            if (ob == null) continue;
            // 物体相对玩家的格坐标
            float ox = (ob.Position.X - playerWorld.X) / CellWidth;
            float oy = (ob.Position.Y - playerWorld.Y) / CellHeight;
            float d = Math.Max(Math.Abs(ox - gx), Math.Abs(oy - gy));
            if (d < bestDist)
            {
                bestDist = d;
                best = ob;
            }
        }
        return best;
    }

    /// <summary>玩家格 -> 鼠标方向 (8 方向)。</summary>
    private MirDirection DirectionToMouse(System.Drawing.Point pCell)
    {
        Control parent = GetParent() as Control;
        if (parent == null) return MirDirection.Down;
        Vector2 mouseWorld = parent.GetGlobalMousePosition() / WorldScale;
        Vector2 playerWorld = _mapView.CellToScreen(_mapView.CenterX, _mapView.CenterY, true);
        float dx = mouseWorld.X - playerWorld.X;
        float dy = mouseWorld.Y - playerWorld.Y;
        if (Math.Abs(dx / CellWidth) < 0.15f && Math.Abs(dy / CellHeight) < 0.15f) return MirDirection.Down;
        double angle = Math.Atan2(dx / CellWidth, -(dy / CellHeight)) * 180.0 / Math.PI;
        if (angle < 0) angle += 360;
        int idx = (int)Math.Floor((angle + 22.5) / 45.0) & 7;
        return (MirDirection)idx;
    }

    private void SetProcessAlways() { } // 占位, 保留扩展点
}