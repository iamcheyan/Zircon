using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 CaptionDialog：限制字母/数字长度并发送 CaptionChange。</summary>
public sealed partial class CaptionDialog : DXWindow
{
    private readonly DXTextInput _input;
    private readonly DXButton _change;

    public CaptionDialog()
    {
        Text = "标题";
        Movable = true;
        HasFooter = true;
        Size = new Vector2I(343, 150);
        AddControl(new LegacyWindowFrame { Size = Size, HasTitle = true, HasFooter = true });
        AddControl(new DXLabel { Text = "标题：", FontSize = 10, Location = new Vector2I(9, 52), IsControl = false });
        _input = new DXTextInput { Location = new Vector2I(73, 52), Size = new Vector2I(180, 20) };
        AddControl(_input);
        AddControl(new DXLabel
        {
            Text = "[?]",
            FontSize = 10,
            TextColour = new Color(1f, .85f, .3f),
            Location = new Vector2I(260, 50),
            IsControl = false,
            TooltipText = $"Caption.\nAccepted characters: a-z A-Z 0-9.\nLength: {Globals.MinCaptionLength}-{Globals.MaxCaptionLength}.\nAvoid harmful and racist words.",
        });
        _change = new DXButton { Text = "更换", Size = new Vector2I(60, 24), Location = new Vector2I(273, 50), Enabled = false };
        _change.MouseClick += (s, e) => Submit();
        AddControl(_change);
        _input.TextChanged += value => { _change.Enabled = CanSubmit(value); _input.BorderColour = _change.Enabled ? Colors.Green : new Color(.55f, .4f, .18f); };
    }

    public static bool CanSubmit(string caption)
        => !string.IsNullOrWhiteSpace(caption) && Globals.CaptionReg.IsMatch(caption);

    private void Submit()
    {
        if (!_change.Enabled || !CanSubmit(_input.Text)) return;
        GameScene.Game?.SendCaptionChange(_input.Text);
        WindowManager.Close(this);
    }
}
