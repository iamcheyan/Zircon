using System;
using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>
/// 垂直滚动条 (移植自 Client/Controls/DXVScrollBar.cs)。
/// 上/下箭头 (Interface 44/46) + 可拖动滑块 (Interface 45);
/// Value 钳位在 [MinValue, MaxValue - VisibleSize]。
/// </summary>
public partial class DXVScrollBar : DXControl
{
    private int _value;
    public int Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            OnValueChanged();
        }
    }

    /// <summary>Value 越界时回钳 (照原版 OnValueChanged 首行语义)</summary>
    private void OnValueChanged()
    {
        int clamped = Math.Max(MinValue, Math.Min(MaxValue - VisibleSize, Value));
        if (Value != clamped)
        {
            Value = clamped;
            return;
        }
        UpdateScrollBar();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private int _maxValue;
    public int MaxValue
    {
        get => _maxValue;
        set
        {
            if (_maxValue == value) return;
            _maxValue = value;
            if (Value + VisibleSize > MaxValue)
                Value = MaxValue - VisibleSize;
            UpdateScrollBar();
            MaxValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int _minValue;
    public int MinValue
    {
        get => _minValue;
        set
        {
            if (_minValue == value) return;
            _minValue = value;
            UpdateScrollBar();
            MinValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int _visibleSize;
    public int VisibleSize
    {
        get => _visibleSize;
        set
        {
            if (_visibleSize == value) return;
            _visibleSize = value;
            UpdateScrollBar();
            VisibleSizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _hideWhenNoScroll;
    public bool HideWhenNoScroll
    {
        get => _hideWhenNoScroll;
        set
        {
            if (_hideWhenNoScroll == value) return;
            _hideWhenNoScroll = value;
            UpdateScrollBar();
            HideWhenNoScrollChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler<EventArgs> ValueChanged;
    public event EventHandler<EventArgs> MaxValueChanged;
    public event EventHandler<EventArgs> MinValueChanged;
    public event EventHandler<EventArgs> VisibleSizeChanged;
    public event EventHandler<EventArgs> HideWhenNoScrollChanged;

    /// <summary>每步滚动量 (滚轮/箭头)</summary>
    public int Change = 10;

    public DXButton UpButton, DownButton, PositionBar;

    private int ScrollHeight => Math.Max(0, (int)Size.Y - 50);

    public DXVScrollBar()
    {
        Border = true;
        BorderColour = new Color(0.8f, 0.6f, 0.2f);
        BackColour = Colors.Black;

        UpButton = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 44,
            Location = new Vector2I(1, 1),
            Enabled = false,
        };
        UpButton.MouseClick += (o, e) => Value -= Change;
        UpButton.MouseWheel += DoMouseWheel;

        DownButton = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 46,
            Location = new Vector2I(1, 0),
            Enabled = false,
        };
        DownButton.MouseClick += (o, e) => Value += Change;
        DownButton.MouseWheel += DoMouseWheel;

        PositionBar = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 45,
            Location = new Vector2I(1, 17),
            Enabled = false,
            Movable = true,
            CanBePressed = false,
        };
        PositionBar.Moving += PositionBarMoving;
        PositionBar.MouseWheel += DoMouseWheel;

        AddControl(UpButton);
        AddControl(DownButton);
        AddControl(PositionBar);

        Size = new Vector2I(14, 100);
    }

    public override void _Ready()
    {
        base._Ready();
        ResizeChildren();
        Resized += OnResized;
    }

    private void OnResized()
    {
        ResizeChildren();
    }

    private void ResizeChildren()
    {
        if (ScrollHeight < 0) return;
        DownButton.Location = new Vector2I(UpButton.Location.X, (int)Size.Y - 13);
        UpdateScrollBar();
    }

    private void UpdateScrollBar()
    {
        UpButton.Enabled = Value > MinValue;
        DownButton.Enabled = Value < MaxValue - VisibleSize;
        PositionBar.Enabled = MaxValue - MinValue > VisibleSize;

        if (MaxValue - MinValue - VisibleSize != 0)
            PositionBar.Location = new Vector2I(UpButton.Location.X, 16 + (int)(ScrollHeight * (Value / (float)(MaxValue - MinValue - VisibleSize))));

        if (HideWhenNoScroll)
            Visible = UpButton.Enabled || DownButton.Enabled;
    }

    private void PositionBarMoving(object sender, EventArgs e)
    {
        if (MaxValue - MinValue - VisibleSize == 0) return;
        Value = (int)Math.Round((PositionBar.Location.Y - 16) * (MaxValue - MinValue - VisibleSize) / (float)ScrollHeight);
    }

    public void DoMouseWheel(object sender, MouseWheelEventArgs e)
    {
        Value -= e.Delta * Change;
    }
}
