using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 ExitDialog：退出前确认，而不是直接关闭客户端。</summary>
public partial class ExitDialog : DXWindow
{
    public ExitDialog()
    {
        HasTitle = false;
        HasFooter = false;
        Size = new Vector2I(252, 128);

        AddControl(new DXImageControl
        {
            LibraryFile = LibraryFile.Interface,
            Index = 281,
            FixedSize = true,
            Size = Size,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        var close = new DXButton
        {
            LibraryFile = LibraryFile.Interface,
            Index = 15,
            Location = new Vector2I((int)Size.X - 30, 3),
        };
        close.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(close);

        AddControl(new DXLabel
        {
            Text = "退出游戏",
            FontSize = 11,
            TextColour = new Color(1f, 0.85f, 0.3f),
            DrawOutline = true,
            OutlineColour = Colors.Black,
            Align = HorizontalAlignment.Center,
            VAlign = VerticalAlignment.Center,
            AutoSize = false,
            Size = new Vector2I((int)Size.X, 25),
            IsControl = false,
        });

        var select = CreateButton("返回角色选择", 48);
        select.MouseClick += (o, e) => GameScene.Game?.LeaveGame();
        var exit = CreateButton("退出客户端", 78);
        exit.MouseClick += (o, e) => GameScene.Game?.ExitClient();
    }

    private DXButton CreateButton(string text, int y)
    {
        var button = new DXButton
        {
            Text = text,
            FontSize = 10,
            TextColour = new Color(1f, 0.85f, 0.3f),
            Size = new Vector2I(130, 25),
            Location = new Vector2I(61, y),
            LibraryFile = LibraryFile.Interface,
            Index = -1,
            Type = DXButton.ButtonType.SmallButton,
        };
        AddControl(button);
        return button;
    }
}
