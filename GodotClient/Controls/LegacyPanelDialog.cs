using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>尚未接入服务器数据的原版窗口壳，先保证原版入口、尺寸、皮肤和关闭行为一致。</summary>
public partial class LegacyPanelDialog : DXWindow
{
    public LegacyPanelDialog(string title, int backgroundIndex, Vector2I size, string[] sections)
    {
        HasTitle = false;
        HasFooter = false;
        Size = size;
        AddControl(new DXImageControl { LibraryFile = LibraryFile.Interface, Index = backgroundIndex, FixedSize = true, Size = Size, MouseFilter = MouseFilterEnum.Ignore });
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I((int)Size.X - 30, 3) };
        close.MouseClick += (o, e) => WindowManager.Close(this);
        AddControl(close);
        AddControl(new DXLabel { Text = title, FontSize = 12, TextColour = new Color(1f, 0.85f, 0.3f), DrawOutline = true, OutlineColour = Colors.Black, Align = HorizontalAlignment.Center, AutoSize = false, Size = new Vector2I((int)Size.X, 28), IsControl = false });

        for (int i = 0; i < sections.Length; i++)
        {
            AddControl(new DXButton { Text = sections[i], FontSize = 11, TextColour = new Color(0.9f, 0.8f, 0.5f), Size = new Vector2I(Mathf.Min((int)Size.X - 32, 180), 28), Location = new Vector2I(16, 44 + i * 34), LibraryFile = LibraryFile.Interface, Index = -1 });
        }
    }
}
