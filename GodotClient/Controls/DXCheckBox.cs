using System;
using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>原版 DXCheckBox 的 Godot 版。</summary>
public sealed partial class DXCheckBox : DXControl
{
    private bool _checked;
    private readonly DXImageControl _box;

    public DXLabel Label { get; }
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            UpdateBox();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public bool ReadOnly { get; set; }
    public int LabelBoxPadding { get; set; }
    public event EventHandler<EventArgs> CheckedChanged;

    public DXCheckBox()
    {
        Label = new DXLabel { IsControl = false, FontSize = 10 };
        AddControl(Label);
        _box = new DXImageControl
        {
            LibraryFile = LibraryFile.GameInter,
            Index = 161,
            IsControl = false,
            Location = new Vector2I(0, 1),
        };
        AddControl(_box);
        Size = new Vector2I(18, 18);
    }

    public void SetSilentState(bool value)
    {
        _checked = value;
        UpdateBox();
    }

    public override void _Ready()
    {
        base._Ready();
        UpdateLayout();
    }

    public override void _GuiInput(InputEvent e)
    {
        base._GuiInput(e);
        if (!ReadOnly && IsEnabled && e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
            Checked = !Checked;
    }

    private void UpdateLayout()
    {
        Vector2I boxSize = (Vector2I)_box.Size;
        if (boxSize.X <= 0 || boxSize.Y <= 0) boxSize = new Vector2I(18, 18);
        _box.Location = new Vector2I((int)Label.Size.X + LabelBoxPadding, 1);
        Size = new Vector2I((int)Label.Size.X + LabelBoxPadding + boxSize.X, Math.Max(18, boxSize.Y + 1));
    }

    private void UpdateBox() => _box.Index = _checked ? 162 : 161;
}
