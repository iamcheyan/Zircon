using System;
using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版商城赠送窗口：输入合法角色名后发送 GameStoreGift。</summary>
public sealed partial class GameStoreGiftDialog : DXWindow
{
    private readonly DXTextInput _recipient;
    private readonly DXButton _confirm;
    private readonly Action<string> _send;

    public GameStoreGiftDialog(string itemName, Action<string> send)
    {
        Text = Lang.GameStoreDialogGiftCaption;
        HasTitle = false;
        HasFooter = false;
        Size = new Vector2I(252, 128);
        _send = send;

        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 281, FixedSize = true, Size = Size, MouseFilter = MouseFilterEnum.Ignore });
        AddControl(new DXLabel { Text = Lang.GameStoreDialogGiftCaption, FontSize = 10, TextColour = new Color(1f, .85f, .3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, VAlign = VerticalAlignment.Center, AutoSize = false, Location = new Vector2I(0, 8), Size = new Vector2I(252, 18), IsControl = false });
        AddControl(new DXLabel { Text = $"物品: {itemName}", FontSize = 9, Location = new Vector2I(16, 28), Size = new Vector2I(220, 18), IsControl = false });
        AddControl(new DXLabel { Text = "角色名:", FontSize = 10, Location = new Vector2I(16, 55), Size = new Vector2I(60, 22), IsControl = false });
        _recipient = new DXTextInput { Location = new Vector2I(76, 53), Size = new Vector2I(160, 20) };
        _recipient.TextChanged += value => _confirm.Enabled = Globals.CharacterReg.IsMatch(value ?? string.Empty);
        AddControl(_recipient);
        _confirm = new DXButton { Text = "确定赠送", Type = DXButton.ButtonType.SmallButton, FontSize = 9, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(24, 93), Size = new Vector2I(88, 25), Enabled = false };
        _confirm.MouseClick += (o, e) =>
        {
            string recipient = _recipient.Text.Trim();
            if (!CanConfirm(GameScene.Game?.IsObserver == true, recipient)) return;
            _send?.Invoke(recipient);
            WindowManager.Close(this);
        };
        AddControl(_confirm);
        var cancel = new DXButton { Text = "取消", Type = DXButton.ButtonType.SmallButton, FontSize = 10, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(142, 93), Size = new Vector2I(76, 25) };
        cancel.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(cancel);
    }

    public static bool CanConfirm(bool observer, string recipient)
        => !observer && !string.IsNullOrWhiteSpace(recipient) && Globals.CharacterReg.IsMatch(recipient.Trim());
}
