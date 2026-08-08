using System;
using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>原版 DXMessageBox 的最小可复用确认窗口，供商城/仓库等危险操作使用。</summary>
public sealed partial class ConfirmDialog : DXWindow
{
    private readonly Action _confirm;

    public ConfirmDialog(string message, string title, Action confirm)
    {
        Text = title ?? "确认";
        HasTitle = false;
        HasFooter = false;
        Size = new Vector2I(252, 128);
        _confirm = confirm;

        AddControl(new DXImageControl
        {
            LibraryFile = LibraryFile.Interface,
            Index = 281,
            FixedSize = true,
            Size = Size,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        AddControl(new DXLabel
        {
            Text = Text,
            FontSize = 11,
            TextColour = new Color(1f, .85f, .3f),
            DrawOutline = true,
            OutlineColour = Colors.Black,
            Align = HorizontalAlignment.Center,
            AutoSize = false,
            Size = new Vector2I(252, 25),
            IsControl = false,
        });
        AddControl(new DXLabel
        {
            Text = message ?? string.Empty,
            FontSize = 10,
            TextColour = Colors.White,
            DrawOutline = true,
            OutlineColour = Colors.Black,
            Location = new Vector2I(18, 30),
            Size = new Vector2I(216, 48),
            IsControl = false,
        });

        var yes = new DXButton { Text = "确定", Type = DXButton.ButtonType.SmallButton, FontSize = 10, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(38, 93), Size = new Vector2I(76, 25) };
        yes.MouseClick += (o, e) => { _confirm?.Invoke(); WindowManager.Close(this); };
        AddControl(yes);
        var no = new DXButton { Text = "取消", Type = DXButton.ButtonType.SmallButton, FontSize = 10, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(138, 93), Size = new Vector2I(76, 25) };
        no.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(no);
    }
}
