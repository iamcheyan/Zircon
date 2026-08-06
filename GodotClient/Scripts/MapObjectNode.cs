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
        int frame = GetFrameIndex(nowMs, true);
        if (frame != FrameIndex)
        {
            FrameIndex = frame;
            QueueRedraw();
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
