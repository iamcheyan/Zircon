using System;
using System.Collections.Generic;
using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>登录页原版辅助窗口的共同布局：输入行、主按钮、取消按钮和可选的次级链接。</summary>
public sealed partial class LegacyLoginDialog : DXWindow
{
    private readonly List<DXTextInput> _inputs = new();
    public IReadOnlyList<DXTextInput> Inputs => _inputs;
    public event Action<IReadOnlyList<string>> Submitted;
    public event Action SecondaryClicked;

    public LegacyLoginDialog(string title, Vector2I size, string[] labels, bool[] secret = null, string secondary = null)
    {
        HasTitle = false;
        HasFooter = false;
        Size = size;
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 164, FixedSize = true, Size = size, MouseFilter = MouseFilterEnum.Ignore });
        AddControl(new DXLabel { Text = title, FontSize = 12, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, Align = HorizontalAlignment.Center, Size = new Vector2I(size.X, 28), IsControl = false });

        int inputX = size.X <= 300 ? 85 : 105;
        int inputWidth = Math.Min(190, size.X - inputX - 30);
        for (int i = 0; i < labels.Length; i++)
        {
            int y = 45 + i * 25;
            AddControl(new DXLabel { Text = labels[i], FontSize = 9, TextColour = new Color(1f, .82f, .5f), Location = new Vector2I(10, y + 3), Size = new Vector2I(inputX - 18, 20), IsControl = false });
            var edit = new DXTextInput { Location = new Vector2I(inputX, y), Size = new Vector2I(inputWidth, 20), Secret = secret != null && i < secret.Length && secret[i] };
            AddControl(edit);
            _inputs.Add(edit);
        }

        if (!string.IsNullOrWhiteSpace(secondary))
        {
            var link = new DXButton { Text = secondary, FontSize = 9, TextColour = new Color(1f, .75f, .25f), Size = new Vector2I(inputWidth, 22), Location = new Vector2I(inputX, 70), LibraryFile = LibraryFile.Interface, Index = -1 };
            link.MouseClick += (o, e) => SecondaryClicked?.Invoke();
            AddControl(link);
        }

        var submit = new DXButton { Text = "确定", FontSize = 10, Size = new Vector2I(80, 28), Location = new Vector2I(size.X / 2 - 90, size.Y - 43), LibraryFile = LibraryFile.Interface, Index = -1 };
        submit.MouseClick += (o, e) => Submitted?.Invoke(ReadValues());
        AddControl(submit);
        var cancel = new DXButton { Text = "取消", FontSize = 10, Size = new Vector2I(80, 28), Location = new Vector2I(size.X / 2 + 10, size.Y - 43), LibraryFile = LibraryFile.Interface, Index = -1 };
        cancel.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(cancel);
    }

    public string[] ReadValues()
    {
        var values = new string[_inputs.Count];
        for (int i = 0; i < values.Length; i++) values[i] = _inputs[i].Text.Trim();
        return values;
    }
}
