using System;
using Godot;

namespace ZirconClient.Controls;

/// <summary>
/// 三态按钮 (移植自 Client/Controls/DXButton.cs)。
/// 贴图三态 (普通/悬停/按下) + 居中文字; 点击触发 MouseClick。
/// </summary>
public partial class DXButton : DXImageControl
{
    public enum ButtonType
    {
        Normal,
        SmallButton,
        LongButton,
        YellowButton,
        GreenButton,
        Default,
    }

    public ButtonType Type = ButtonType.Normal;

    public bool CanBePressed = true;
    public new bool HasFocus;

    private DXLabel _label;

    public DXLabel Label => _label;

    public override void _Ready()
    {
        base._Ready();
        if (_label == null)
        {
            _label = new DXLabel
            {
                Name = "Label",
                Align = HorizontalAlignment.Center,
                VAlign = VerticalAlignment.Center,
                Text = Text,
                FontSize = FontSize,
                TextColour = TextColour,
            };
            AddChild(_label);
            _label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _label.MouseFilter = MouseFilterEnum.Ignore;
        }
    }

    private int _fontSize = 12;
    public int FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value;
            if (_label != null) _label.FontSize = value;
        }
    }

    private Color _textColour = Colors.White;
    public Color TextColour
    {
        get => _textColour;
        set
        {
            _textColour = value;
            if (_label != null) _label.TextColour = value;
        }
    }

    public new string Text
    {
        get => base.Text;
        set
        {
            base.Text = value;
            if (_label != null) _label.Text = value;
        }
    }

    public override void _GuiInput(InputEvent e)
    {
        base._GuiInput(e);

        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed && CanBePressed)
            {
                Pressed = true;
                HasFocus = true;
                QueueRedraw();
            }
            else if (!mb.Pressed && Pressed)
            {
                Pressed = false;
                QueueRedraw();
            }
        }
    }

    private bool _pressed;
    public bool Pressed
    {
        get => _pressed;
        set
        {
            if (_pressed == value) return;
            _pressed = value;
            QueueRedraw();
        }
    }

    protected override void DrawControl()
    {
        base.DrawControl();
        // 按钮文字画在贴图上; 若贴图缺失则画一个底色框保证可见
        if (MirSkin.GetTexture(LibraryFile, GetCurrentIndex()) == null)
        {
            Color back = IsHovered ? new Color(0.4f, 0.4f, 0.6f, 0.8f) : new Color(0.25f, 0.25f, 0.3f, 0.8f);
            DrawRect(new Rect2(Vector2.Zero, Size), back);
        }
    }
}
