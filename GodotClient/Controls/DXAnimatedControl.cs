using System;
using Godot;

namespace ZirconClient.Controls;

/// <summary>
/// 原版 DXAnimatedControl 的图库动画控件。
/// AnimationDelay 与原客户端相同，表示播放一轮的总时长，而不是单帧时长。
/// </summary>
public partial class DXAnimatedControl : DXImageControl
{
    private int _baseIndex = -1;
    private int _frameCount;
    // Legacy DXAnimatedControl starts in animated/looping mode.  A number of
    // old dialogs rely on these constructor defaults and do not set the two
    // properties explicitly.
    private bool _animated = true;
    private bool _loop = true;
    private bool _finished;

    public int BaseIndex
    {
        get => _baseIndex;
        set { _baseIndex = value; if (!Animated) Index = value; }
    }

    public int FrameCount
    {
        get => _frameCount;
        set { _frameCount = Math.Max(0, value); QueueRedraw(); }
    }

    public bool Animated
    {
        get => _animated;
        set
        {
            if (_animated == value) return;
            _animated = value;
            _finished = false;
            AnimationStart = DateTime.MinValue;
            if (!value) Index = BaseIndex;
            QueueRedraw();
        }
    }

    public bool Loop
    {
        get => _loop;
        set { _loop = value; _finished = false; }
    }

    public TimeSpan AnimationDelay = TimeSpan.Zero;
    public DateTime AnimationStart = DateTime.MinValue;
    public event EventHandler AfterAnimation;
    public event EventHandler AfterAnimationLoop;

    public override void _Process(double delta)
    {
        if (!Animated || FrameCount <= 0 || AnimationDelay <= TimeSpan.Zero || _finished)
            return;

        if (AnimationStart == DateTime.MinValue)
            AnimationStart = DateTime.UtcNow;

        double elapsed = (DateTime.UtcNow - AnimationStart).TotalSeconds;
        double duration = AnimationDelay.TotalSeconds;
        int frame = (int)Math.Floor(elapsed / duration * FrameCount);

        if (Loop)
        {
            if (frame >= FrameCount)
            {
                AfterAnimationLoop?.Invoke(this, EventArgs.Empty);
                frame %= FrameCount;
            }
        }
        else if (frame >= FrameCount)
        {
            Index = BaseIndex + FrameCount - 1;
            _finished = true;
            Animated = false;
            AfterAnimation?.Invoke(this, EventArgs.Empty);
            QueueRedraw();
            return;
        }

        Index = BaseIndex + Math.Clamp(frame, 0, FrameCount - 1);
        QueueRedraw();
    }

    public void Restart(bool loop = false)
    {
        Loop = loop;
        Animated = true;
        AnimationStart = DateTime.MinValue;
        _finished = false;
    }

    public void ClearAnimationHandlers()
    {
        AfterAnimation = null;
        AfterAnimationLoop = null;
    }
}
