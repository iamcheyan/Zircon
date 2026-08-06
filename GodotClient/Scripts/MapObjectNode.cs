using System;
using System.Collections.Generic;
using Godot;
using Library;
using Library.Network;

namespace ZirconClient.Scripts;

// 地图对象基类 (怪物/NPC/物品共用): 动画帧推进 + 动作队列 + 移动插值 + 屏幕定位
// M7 对齐原版 Client/Models/MapObject.cs 的 UpdateFrame/SetFrame/DoNextAction:
//   * 帧推进 = Frame.GetFrame 语义: doubleSpeed 双倍速 / Reversed 倒放 / StaticSpeed 固定速
//   * 动作队列: 一次性动作播完或被 Standing/Dead 打断 -> DoNextAction 弹下一个动作
//   * 移动 = 权威格不变 + 8 向像素偏移 (OffsetX/OffsetY), 动画播完落格
//   * FrameIndexChanged 虚钩子 + SetScale (原版 5172/5314 行)
// 帧号公式与 PlayerRenderer 一致: DrawFrame = FrameIndex + StartIndex + OffSet * dir
public partial class MapObjectNode : Node2D
{
    public uint ObjectID;
    public MirDirection Direction;
    public MirAnimation Animation = MirAnimation.Standing;
    public int FrameIndex;
    public double FrameStartMs;
    protected Frame _currentFrame;

    // 服务端权威格子坐标 + 移动期间像素偏移 (权威格不变, 偏移回拉, 播完落格)
    public int CellX, CellY;
    public float OffsetX, OffsetY;

    // M5 战斗: 血量 (0=未知不显示血条)
    public int Health;
    public int MaxHealth;
    public bool ShowHealthBar;
    public bool Dead;

    // ---- M7: 动作队列 (原版 ActionQueue) ----
    public readonly Queue<MirAnimation> ActionQueue = new();

    // 打断标记: Standing/Dead 立即打断当前动画; 其他动作播完再切 (原版 SetFrame 的 Interupt)
    private bool _interupt = true;

    // 缩放百分比 (原版 SetScale, -50..50)
    private int _scalePercent;

    // 移动插值 (原版 MovingOffSet): 起点格 + 终点格 + 动画时长
    public System.Drawing.Point MoveFrom;
    public double MoveStartMs;
    private int _targetX, _targetY;

    private Dictionary<MirAnimation, Frame> _frameTable = new(FrameSet.DefaultMonster);
    public virtual Dictionary<MirAnimation, Frame> FrameTable => _frameTable;

    // 立即切换动画 (原版 SetAnimation + SetFrame 的 Interupt 规则)
    public virtual void SetAnimation(MirAnimation anim)
    {
        Animation = anim;
        _currentFrame = FrameTable.TryGetValue(anim, out var f) ? f : FrameSet.DefaultMonster[MirAnimation.Standing];
        FrameStartMs = Godot.Time.GetTicksMsec(); // 从当前时刻起播, 保证从第 0 帧开始
        FrameIndex = 0;
        // 原版 SetFrame: Standing/Dead 立即打断 (Interupt=true), 其他动作播完再切
        _interupt = anim is MirAnimation.Standing or MirAnimation.Dead;
        QueueRedraw();
    }

    // 原版 SetFrame: 设定当前动作 (含打断规则)
    public virtual void SetFrame(MirAnimation anim) => SetAnimation(anim);

    // 动作入队: 当前动作播完/被打断后执行 (原版 ActionQueue.Add)
    public void QueueAction(MirAnimation anim) => ActionQueue.Enqueue(anim);

    // 原版 DoNextAction: 队列空 -> Standing (Die 后保持 Dead); 否则弹队首
    public virtual void DoNextAction()
    {
        if (ActionQueue.Count == 0)
        {
            SetAnimation(Dead ? MirAnimation.Dead : MirAnimation.Standing);
            return;
        }
        SetAnimation(ActionQueue.Dequeue());
    }

    // 帧号变化钩子 (原版 FrameIndexChanged): 攻击/受击/死亡帧事件, 子类可 override
    public virtual void FrameIndexChanged() { }

    // 原版 SetScale: sizePercent -50..50, 以格中心为锚点缩放
    public void SetScale(int sizePercent)
    {
        _scalePercent = sizePercent;
        float s = (100f + Math.Min(50, Math.Max(-50, sizePercent))) / 100f;
        Scale = new Vector2(s, s);
    }

    // Frame.GetFrame 移植 (LibraryCore/FrameSet.cs 1119 行):
    //   doubleSpeed && !StaticSpeed -> elapsed 翻倍; Reversed -> 倒序累计
    //   返回 [0, FrameCount), FrameCount 表示动画已播完 (由 _Process 决定收尾)
    protected int GetFrameIndex(double nowMs, bool doubleSpeed)
    {
        if (_currentFrame == null) return 0;
        if (_currentFrame.FrameCount <= 1) return 0;

        double elapsed = nowMs - FrameStartMs;
        if (doubleSpeed && !_currentFrame.StaticSpeed) elapsed *= 2.0;

        var delays = _currentFrame.Delays;
        if (_currentFrame.Reversed)
        {
            for (int i = 0; i < delays.Length; i++)
            {
                elapsed -= delays[delays.Length - 1 - i].TotalMilliseconds;
                if (elapsed >= 0) continue;
                return delays.Length - 1 - i; // 逻辑帧号 (原版 UpdateFrame 的 FrameCount-frame-1)
            }
        }
        else
        {
            for (int i = 0; i < delays.Length; i++)
            {
                elapsed -= delays[i].TotalMilliseconds;
                if (elapsed >= 0) continue;
                return i;
            }
        }
        return _currentFrame.FrameCount;
    }

    public override void _Process(double delta)
    {
        double nowMs = Godot.Time.GetTicksMsec();

        // 双倍速: 原版 (this != User || Observer) && ActionQueue.Count > 1
        // (Godot 客户端玩家走 PlayerRenderer, 这里只有周围物体, 恒非 User)
        bool doubleSpeed = ActionQueue.Count > 1;
        int frame = GetFrameIndex(nowMs, doubleSpeed);

        // 播完 或 被打断且有排队动作 -> 弹下一个 (原版 UpdateFrame 597-610 行)
        if (frame == _currentFrame.FrameCount || (_interupt && ActionQueue.Count > 0))
        {
            DoNextAction();
            frame = GetFrameIndex(nowMs, doubleSpeed);
            if (frame == _currentFrame.FrameCount)
                frame -= 1; // 停末帧
        }

        UpdateMoveOffset(nowMs);

        if (frame != FrameIndex)
        {
            FrameIndex = frame;
            FrameIndexChanged();
            QueueRedraw();
        }
    }

    // 移动期间像素偏移 (原版 MovingOffSet, 平滑分支): 权威格=终点, 偏移从起点回拉
    private void UpdateMoveOffset(double nowMs)
    {
        if (Animation is not (MirAnimation.Walking or MirAnimation.Running))
        {
            OffsetX = 0;
            OffsetY = 0;
            return;
        }
        if (_currentFrame.FrameCount <= 1)
        {
            OffsetX = 0;
            OffsetY = 0;
            return;
        }

        double sum = _currentFrame.Sum;
        if (sum <= 0)
        {
            OffsetX = 0;
            OffsetY = 0;
            return;
        }

        double t = Math.Clamp((nowMs - MoveStartMs) / sum, 0.0, 1.0);
        double k = 1.0 - t; // 1 -> 起点, 0 -> 终点(权威格)
        int dx = _targetX - MoveFrom.X;
        int dy = _targetY - MoveFrom.Y;

        int x = (int)(dx * CellWidth * k);
        int y = (int)(dy * CellHeight * k);
        x -= x % 2; // 偶数像素对齐 (原版 x -= x % 2)
        y -= y % 2;
        OffsetX = x;
        OffsetY = y;
    }

    // 开始一格(或多格)移动: 终点为服务端权威位置, 起点为当前格
    public void StartMove(System.Drawing.Point to, MirDirection dir)
    {
        MoveFrom = new System.Drawing.Point(CellX, CellY);
        _targetX = to.X;
        _targetY = to.Y;
        Direction = dir;
        MoveStartMs = Godot.Time.GetTicksMsec();
        CellX = to.X;  // 权威格立即到终点, 视觉位置由 OffsetX/OffsetY 回拉
        CellY = to.Y;
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

    // 计算本节点屏幕位置 (相机锚定玩家, 含移动像素偏移)
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
