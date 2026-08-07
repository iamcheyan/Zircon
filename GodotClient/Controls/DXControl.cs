using System;
using System.Collections.Generic;
using Godot;

namespace ZirconClient.Controls;

/// <summary>
/// Godot 版自绘控件基类 (移植自 Client/Controls/DXControl.cs 的控件模型)。
/// 与原版一致的约定: 控件 = 贴图 + 坐标 + 状态; 属性名与事件名照抄旧窗口代码,
/// 这样从旧 Client/Scenes/Views/ 抄窗口布局时只需要改 using 与 new Point/Size。
/// </summary>
public partial class DXControl : Control
{
    public static DXControl MouseControl;
    public static DXControl FocusControl;
    public static readonly List<DXControl> MessageBoxList = new();

    /// <summary>子控件列表 (与 Godot 节点树平行, 便于按旧代码遍历)</summary>
    public List<DXControl> Controls { get; } = new();

    /// <summary>逻辑父控件 (与 Godot 的 Parent 节点区分)</summary>
    public DXControl ParentControl { get; private set; }

    private bool _enabled = true;
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            UpdateMouseFilter();
            QueueRedraw();
        }
    }

    public bool IsEnabled => _enabled && (ParentControl == null || ParentControl.IsEnabled);

    /// <summary>原版 IsControl: false 时不接收鼠标 (如纯文本标签)</summary>
    private bool _isControl = true;
    public bool IsControl
    {
        get => _isControl;
        set { if (_isControl == value) return; _isControl = value; UpdateMouseFilter(); }
    }

    /// <summary>鼠标事件穿透到下层控件 (MouseFilter.Ignore 别名)</summary>
    private bool _passThrough;
    public bool PassThrough
    {
        get => _passThrough;
        set { if (_passThrough == value) return; _passThrough = value; UpdateMouseFilter(); }
    }

    /// <summary>裁剪子控件到本控件边界</summary>
    public bool Clip
    {
        get => ClipContents;
        set => ClipContents = value;
    }

    /// <summary>鼠标拖拽移动本控件 (原版 DXControl.Movable)</summary>
    public bool Movable;

    /// <summary>拖拽时不受父控件边界限制 (用于大地图图片)</summary>
    public bool IgnoreMoveBounds;

    /// <summary>拖拽中每帧触发 (原版 Moving 事件, 供大地图移动同步子控件)</summary>
    public event EventHandler<EventArgs> Moving;

    private bool _dragging;

    private void UpdateMouseFilter()
    {
        MouseFilter = (_enabled && _isControl && !_passThrough) ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    private bool _visible = true;
    public new bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value) return;
            _visible = value;
            base.Visible = value;
        }
    }

    private string _text = "";
    public string Text
    {
        get => _text;
        set { if (_text == value) return; _text = value ?? ""; QueueRedraw(); }
    }

    private Color _backColour = Colors.Transparent;
    public Color BackColour
    {
        get => _backColour;
        set { _backColour = value; QueueRedraw(); }
    }

    private Color _foreColour = Colors.White;
    public Color ForeColour
    {
        get => _foreColour;
        set { _foreColour = value; QueueRedraw(); }
    }

    private bool _border;
    public bool Border
    {
        get => _border;
        set { _border = value; QueueRedraw(); }
    }

    public Color BorderColour = Colors.White;

    public float Opacity
    {
        get => Modulate.A;
        set => Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, value);
    }

    public object Tag;

    /// <summary>相对父控件的坐标 (旧代码叫 Location)</summary>
    public Vector2I Location
    {
        get => (Vector2I)Position;
        set => Position = value;
    }

    // ---- 事件 (签名与旧 DXControl 一致, 抄窗口代码时不用改) ----
    public event EventHandler<EventArgs> MouseEnter, MouseLeave;
    public event EventHandler<EventArgs> MouseDown, MouseUp, MouseClick, MouseDoubleClick, MouseMove;
    public event EventHandler<MouseWheelEventArgs> MouseWheel;
    public event EventHandler<EventArgs> Focus, LostFocus;
    public event EventHandler<EventArgs> BeforeDraw, AfterDraw;

    /// <summary>每帧更新 (供窗口逻辑用)</summary>
    public virtual void Process() { }

    protected bool IsHovered { get; private set; }
    protected bool IsPressed { get; private set; }

    // ---- 树操作 ----
    public void AddControl(DXControl c)
    {
        if (c == null || c.ParentControl == this) return;
        c.ParentControl = this;
        AddChild(c); // Godot 自动建立节点父子关系 (Position 相对父)
        Controls.Add(c);
        OnControlAdded(c);
    }

    public void RemoveControl(DXControl c)
    {
        if (c == null || c.ParentControl != this) return;
        c.ParentControl = null;
        RemoveChild(c);
        Controls.Remove(c);
    }

    protected virtual void OnControlAdded(DXControl c) { }

    public void BringToFront()
    {
        var p = GetParent();
        if (p != null) p.MoveChild(this, p.GetChildCount() - 1);
    }

    // ---- 绘制 ----
    public override void _Draw()
    {
        BeforeDraw?.Invoke(this, EventArgs.Empty);

        if (BackColour.A > 0)
            DrawRect(new Rect2(Vector2.Zero, Size), BackColour);

        DrawControl();

        if (Border)
            DrawRect(new Rect2(Vector2.Zero, Size), BorderColour, false, 1f);

        AfterDraw?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void DrawControl() { }

    // ---- 输入 ----
    public override void _Ready()
    {
        base._Ready();
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void OnMouseEntered()
    {
        IsHovered = true;
        MouseControl = this;
        MouseEnter?.Invoke(this, EventArgs.Empty);
        QueueRedraw();
    }

    private void OnMouseExited()
    {
        IsHovered = false;
        IsPressed = false;
        if (MouseControl == this) MouseControl = null;
        MouseLeave?.Invoke(this, EventArgs.Empty);
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (!IsEnabled) return;

        if (e is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    IsPressed = true;
                    FocusControl = this;
                    Focus?.Invoke(this, EventArgs.Empty);
                    MouseDown?.Invoke(this, EventArgs.Empty);

                    if (Movable && IsEnabled)
                    {
                        _dragging = true;
                    }
                }
                else if (IsPressed)
                {
                    IsPressed = false;
                    MouseUp?.Invoke(this, EventArgs.Empty);
                    MouseClick?.Invoke(this, EventArgs.Empty);
                    if (mb.DoubleClick) MouseDoubleClick?.Invoke(this, EventArgs.Empty);
                    _dragging = false;
                }
            }
            else if (mb.Pressed && mb.ButtonIndex == MouseButton.Right)
            {
                MouseClick?.Invoke(this, EventArgs.Empty);
            }
            else if (mb.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                int delta = mb.ButtonIndex == MouseButton.WheelUp ? 1 : -1;
                MouseWheel?.Invoke(this, new MouseWheelEventArgs(delta));
            }
            AcceptEvent();
        }
        else if (e is InputEventMouseMotion mm)
        {
            if (_dragging)
            {
                Vector2 target = Position + mm.Relative;
                if (!IgnoreMoveBounds && GetParent() is Control parent)
                {
                    target.X = Mathf.Clamp(target.X, 0, Mathf.Max(0, parent.Size.X - Size.X));
                    target.Y = Mathf.Clamp(target.Y, 0, Mathf.Max(0, parent.Size.Y - Size.Y));
                }
                Position = target;
                Moving?.Invoke(this, EventArgs.Empty);
            }
            MouseMove?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>移除并释放控件 (原版 Dispose 的 Godot 等价)</summary>
    public void Dispose()
    {
        var parent = ParentControl;
        if (parent != null) parent.RemoveControl(this);
        else if (GetParent() is Node p) p.RemoveChild(this);
        QueueFree();
    }
}

public class MouseWheelEventArgs : EventArgs
{
    public int Delta { get; }
    public MouseWheelEventArgs(int delta) { Delta = delta; }
}
