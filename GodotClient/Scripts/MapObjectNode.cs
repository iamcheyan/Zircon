using System;
using System.Collections.Generic;
using Godot;
using Library;
using Library.Network;

namespace ZirconClient.Scripts;

// 地图对象基类 (怪物/NPC/物品共用): 动画帧推进 + 移动插值 + 屏幕定位
// 帧号公式与 PlayerRenderer 一致: DrawFrame = FrameIndex + StartIndex + OffSet * dir
public partial class MapObjectNode : Node2D
{
    public uint ObjectID;
    public MirDirection Direction;
    public MirAnimation Animation = MirAnimation.Standing;
    public int FrameIndex;
    public double FrameStartMs;
    protected Frame _currentFrame;

    // 服务端权威格子坐标 + 平滑移动的像素偏移
    public int CellX, CellY;
    public float OffsetX, OffsetY;

    // M5 战斗: 血量 (0=未知不显示血条)
    public int Health;
    public int MaxHealth;
    public bool ShowHealthBar;
    public bool Dead;

    // 一次性动画 (Combat/Struck/Die): 播完回 Standing (或 Die 后移除)
    private MirAnimation _oneShotAnim = MirAnimation.Standing;

    // 移动插值 (格子级线性插值, 与玩家路径一致)
    public System.Drawing.Point MoveFrom;
    public double MoveStartMs;
    public int MoveFrameCount = 1;

    private Dictionary<MirAnimation, Frame> _frameTable = new(FrameSet.DefaultMonster);
    public virtual Dictionary<MirAnimation, Frame> FrameTable => _frameTable;

    public virtual void SetAnimation(MirAnimation anim)
    {
        Animation = anim;
        _currentFrame = FrameTable.TryGetValue(anim, out var f) ? f : FrameSet.DefaultMonster[MirAnimation.Standing];
        FrameStartMs = Godot.Time.GetTicksMsec(); // 从当前时刻起播, 保证从第 0 帧开始
        FrameIndex = 0;
        // 一次性动作: Combat/Struck/Die 播完不循环
        _oneShotAnim = anim is MirAnimation.Combat1 or MirAnimation.Combat2 or MirAnimation.Combat3
            or MirAnimation.Struck or MirAnimation.Die or MirAnimation.Show or MirAnimation.Hide ? anim : MirAnimation.Standing;
        QueueRedraw();
    }

    // 由 FrameSet.Frame 结构: StartIndex/FrameCount/OffSet/Delays
    protected int GetFrameIndex(double nowMs, bool loop)
    {
        if (_currentFrame == null) return 0;
        if (_currentFrame.FrameCount <= 1) return 0;

        double sum = _currentFrame.Sum;
        double elapsed = nowMs - FrameStartMs;
        int frame = 0;
        double acc = 0;
        for (int i = 0; i < _currentFrame.FrameCount; i++)
        {
            acc += _currentFrame.Delays[i].TotalMilliseconds;
            if (elapsed < acc) { frame = i; break; }
            frame = i;
        }
        if (elapsed >= sum)
        {
            if (loop) frame = (int)((elapsed - (elapsed % sum)) / sum) % _currentFrame.FrameCount;
            else frame = _currentFrame.FrameCount - 1;
        }
        return frame;
    }

    public override void _Process(double delta)
    {
        double nowMs = Godot.Time.GetTicksMsec();
        int frame = GetFrameIndex(nowMs, _oneShotAnim == MirAnimation.Standing);
        if (frame != FrameIndex)
        {
            FrameIndex = frame;
            QueueRedraw();
        }

        // 一次性动作播完: 回 Standing (Die 由 GameScene 延迟移除)
        if (_oneShotAnim != MirAnimation.Standing && !Dead)
        {
            var f = _currentFrame;
            if (f != null && nowMs - FrameStartMs >= f.Sum)
            {
                _oneShotAnim = MirAnimation.Standing;
                SetAnimation(MirAnimation.Standing);
            }
        }

        // 移动插值: 在行走动画时长内从起点插到终点
        if (MoveFrameCount > 1)
        {
            const double walkMs = 6 * 100.0; // 6帧 * 100ms
            double elapsed = nowMs - MoveStartMs;
            double t = Math.Clamp(elapsed / walkMs, 0.0, 1.0);

            CellX = (int)Math.Round(MoveFrom.X + (_targetX - MoveFrom.X) * t);
            CellY = (int)Math.Round(MoveFrom.Y + (_targetY - MoveFrom.Y) * t);

            if (t >= 1.0)
            {
                CellX = _targetX;
                CellY = _targetY;
                MoveFrameCount = 1;
                SetAnimation(MirAnimation.Standing);
            }
        }
    }

    private int _targetX, _targetY;

    // 开始一格(或多格)移动: 终点为服务端权威位置
    public void StartMove(System.Drawing.Point to, MirDirection dir)
    {
        MoveFrom = new System.Drawing.Point(CellX, CellY);
        _targetX = to.X;
        _targetY = to.Y;
        Direction = dir;
        MoveStartMs = Godot.Time.GetTicksMsec();
        MoveFrameCount = 6; // Walking 帧数
        SetAnimation(MirAnimation.Walking);
    }

    public int DrawFrame => FrameIndex + _currentFrame.StartIndex + _currentFrame.OffSet * (int)Direction;

    // 对象头顶血条 (受伤后显示, 原客户端同款: 黑底 + 绿/黄/红条)
    protected void DrawHealthBar()
    {
        if (!ShowHealthBar || Dead || MaxHealth <= 0) return;
        float percent = Math.Clamp(Health / (float)MaxHealth, 0f, 1f);
        if (percent <= 0f) return;

        const float w = 40, h = 5;
        float x = -w / 2, y = -50;
        DrawRect(new Rect2(x - 1, y - 1, w + 2, h + 2), new Color(0f, 0f, 0f, 0.75f));
        var col = percent > 0.5f ? new Color(0f, 0.8f, 0.29f)
                : percent > 0.25f ? new Color(0.9f, 0.8f, 0.1f)
                : new Color(0.9f, 0.2f, 0.1f);
        DrawRect(new Rect2(x, y, w * percent, h), col);
    }

    // 计算本节点屏幕位置 (相机锚定玩家)
    public void ComputeScreenPos(int camCenterX, int camCenterY, int viewRangeX, int viewRangeY, float screenOffsetX, float screenOffsetY)
    {
        Position = new Vector2(
            (CellX - camCenterX + viewRangeX) * CellWidth + screenOffsetX + OffsetX,
            (CellY - camCenterY + viewRangeY) * CellHeight + screenOffsetY + OffsetY
        );
    }

    private const int CellWidth = 48;
    private const int CellHeight = 32;
}
