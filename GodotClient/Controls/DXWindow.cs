using System;
using Godot;

namespace ZirconClient.Controls;

/// <summary>
/// 窗口 (移植自 Client/Controls/DXWindow.cs 的核心行为):
/// 标题栏 + 子控件容器 + 拖拽移动。背景是子控件贴图 (旧窗口都用
/// DXImageControl { Index = xxx, LibraryFile = Interface } 做背景, 抄窗口时照搬)。
/// </summary>
public abstract partial class DXWindow : DXControl
{
    public static readonly System.Collections.Generic.List<DXWindow> Windows = new();

    public bool HasTitle = true;
    public bool HasTopBorder = true;
    public bool HasFooter;
    public bool SlimFooter;

    private DXLabel _titleLabel;

    /// <summary>客户区 (相对窗口左上角; 标题栏下方)</summary>
    public Rect2 ClientArea;

    public const int TitleHeight = 24;
    public const int FooterHeight = 20;

    private bool _moving;
    private Vector2 _moveGrabOffset;

    protected DXWindow()
    {
        Windows.Add(this);
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        base._Ready();

        if (_titleLabel == null && HasTitle)
        {
            _titleLabel = new DXLabel
            {
                Name = "TitleLabel",
                Text = Text,
                FontSize = 13,
                TextColour = new Color(1f, 0.95f, 0.7f),
                Align = HorizontalAlignment.Center,
                VAlign = VerticalAlignment.Center,
                Location = new Vector2I(30, 2),
                Size = new Vector2(Size.X - 60, TitleHeight - 4),
                MouseFilter = MouseFilterEnum.Ignore,
                ZIndex = 100, // 必须盖在背景贴图之上 (背景是后添加的子控件)
            };
            AddChild(_titleLabel);
        }

        UpdateClientArea();
    }

    public new string Text
    {
        get => base.Text;
        set
        {
            base.Text = value;
            if (_titleLabel != null) _titleLabel.Text = value;
        }
    }

    protected void UpdateClientArea()
    {
        float top = HasTitle ? TitleHeight : 0;
        float bottom = Size.Y - (HasFooter ? FooterHeight : 0);
        ClientArea = new Rect2(0, top, Size.X, bottom - top);
    }

    public override void _GuiInput(InputEvent e)
    {
        if (!IsEnabled) return;

        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                // 只允许在标题栏区域拖动
                if (HasTitle && mb.Position.Y < TitleHeight)
                {
                    _moving = true;
                    _moveGrabOffset = mb.Position;
                    BringToFront();
                    AcceptEvent();
                    return;
                }
            }
            else
            {
                _moving = false;
            }
        }
        else if (e is InputEventMouseMotion mm && _moving)
        {
            Position += mm.Relative;
            AcceptEvent();
            return;
        }

        base._GuiInput(e);
    }

    /// <summary>把窗口挂到场景并显示 (旧客户端由 ActiveScene 管理, Godot 里显式挂载)</summary>
    public void ShowWindow(Node parent)
    {
        if (GetParent() == null)
        {
            parent.AddChild(this);
        }
        Visible = true;
        BringToFront();
        QueueRedraw();
    }

    public virtual void Close()
    {
        _moving = false;
        Visible = false;
        if (GetParent() != null)
        {
            GetParent().RemoveChild(this);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _moving = false;
    }
}
