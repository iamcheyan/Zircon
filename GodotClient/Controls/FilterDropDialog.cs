using System;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 FilterDropBox：10 条掉落名称过滤词，保存后由拾取/显示逻辑读取。</summary>
public sealed partial class FilterDropDialog : DXWindow
{
    private readonly DXTextInput[] _filters = new DXTextInput[10];

    public FilterDropDialog()
    {
        Text = "Drop Filter";
        // 原版 SetClientSize(266, 371)：包含标题栏、边框和无底栏区域后
        // 的实际窗口尺寸为 284x429。
        Size = new Vector2I(284, 429);
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = false });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I(254, 3) };
        close.MouseClick += (s, e) => WindowManager.Close(this);
        AddControl(close);
        for (int i = 0; i < _filters.Length; i++)
        {
            int slot = i;
            AddControl(new DXLabel { Text = $"Filter {i + 1}", FontSize = 10, Location = new Vector2I(20, 50 + i * 28), IsControl = false });
            _filters[i] = new DXTextInput { Location = new Vector2I(90, 50 + i * 28), Size = new Vector2I(150, 18) };
            AddControl(_filters[i]);
        }
        var save = new DXButton
        {
            Text = "Save",
            Type = DXButton.ButtonType.SmallButton,
            LibraryFile = LibraryFile.Interface,
            Index = -1,
            Size = new Vector2I(80, 25),
            Location = new Vector2I(100, 399),
        };
        save.MouseClick += (s, e) => Save();
        AddControl(save);
    }

    public void LoadFilters(string[] filters)
    {
        for (int i = 0; i < _filters.Length; i++)
            _filters[i].Text = filters != null && i < filters.Length ? filters[i] ?? string.Empty : string.Empty;
    }

    public string[] Save()
    {
        var result = new string[_filters.Length];
        for (int i = 0; i < result.Length; i++) result[i] = _filters[i].Text.Trim();
        GameScene.Game?.SetDropFilters(result);
        return result;
    }
}

/// <summary>可嵌入 DX 窗口的文本输入，保留 Godot 输入法/复制粘贴能力。</summary>
public sealed partial class DXTextInput : DXControl
{
    private readonly LineEdit _edit;
    private int _fontSize = 10;
    public event Action<string> TextChanged;
    public event Action<string> TextSubmitted;
    /// <summary>输入框按 Escape 时触发（原版 DXTextBox 的 KeyPress Escape 路径）。</summary>
    public event Action Canceled;

    public new string Text
    {
        get => _edit.Text;
        set => _edit.Text = value ?? string.Empty;
    }

    public bool Secret
    {
        get => _edit.Secret;
        set => _edit.Secret = value;
    }

    public int MaxLength
    {
        get => _edit.MaxLength;
        set => _edit.MaxLength = Math.Max(0, value);
    }

    public int CaretColumn
    {
        get => _edit.CaretColumn;
        set => _edit.CaretColumn = value;
    }

    public int FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = Math.Max(1, value);
            _edit.AddThemeFontSizeOverride("font_size", _fontSize);
        }
    }

    public new void GrabFocus() => _edit.GrabFocus();
    public new void ReleaseFocus() => _edit.ReleaseFocus();

    public DXTextInput()
    {
        Border = true;
        BorderColour = new Color(.55f, .4f, .18f);
        _edit = new LineEdit { Flat = true, MouseFilter = MouseFilterEnum.Stop, Position = new Vector2(2, 1), Size = new Vector2(Size.X - 4, Size.Y - 2) };
        var font = MirSkin.GetFont();
        if (font != null) _edit.AddThemeFontOverride("font", font);
        _edit.AddThemeFontSizeOverride("font_size", _fontSize);
        _edit.AddThemeColorOverride("font_color", Colors.White);
        _edit.AddThemeColorOverride("font_placeholder_color", new Color(1f, 1f, 1f, .55f));
        _edit.AddThemeColorOverride("caret_color", new Color(1f, .85f, .3f));
        AddChild(_edit);
        _edit.TextChanged += value => TextChanged?.Invoke(value);
        _edit.TextSubmitted += value => TextSubmitted?.Invoke(value);
        _edit.GuiInput += e =>
        {
            if (e is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
                Canceled?.Invoke();
        };
        Resized += () => _edit.Size = Size - new Vector2(4, 2);
    }
}
