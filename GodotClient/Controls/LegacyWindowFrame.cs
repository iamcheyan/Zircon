using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>
/// 原版 DXWindow.DrawEdges 的 Godot 贴图版。工艺类窗口没有独立整图背景，
/// 必须用 Interface 的边、标题带和底边拼出可变尺寸窗口。
/// </summary>
public sealed partial class LegacyWindowFrame : DXControl
{
    public bool HasTitle { get; set; } = true;
    public bool HasFooter { get; set; }

    public LegacyWindowFrame()
    {
        IsControl = false;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    protected override void DrawControl()
    {
        DrawStretch(HasTitle ? 0 : 2, new Rect2(0, 0, Size.X, TextureHeight(HasTitle ? 0 : 2)));

        int topHeight = TextureHeight(HasTitle ? 0 : 2);
        var side = MirSkin.GetTexture(LibraryFile.Interface, 1);
        if (side != null)
        {
            DrawTextureRect(side, new Rect2(0, topHeight, side.GetWidth(), Mathf.Max(0, Size.Y - topHeight)), false);
            DrawTextureRect(side, new Rect2(Size.X - side.GetWidth(), topHeight, side.GetWidth(), Mathf.Max(0, Size.Y - topHeight)), false);
        }

        if (HasTitle)
        {
            DrawStretch(3, new Rect2(TextureWidth(1), topHeight, Mathf.Max(0, Size.X - TextureWidth(1) * 2), TextureHeight(3)));
            DrawStretch(4, new Rect2(0, topHeight + TextureHeight(3) - 3, Size.X, TextureHeight(4)));
            DrawStretch(5, new Rect2(Size.X - TextureWidth(5), topHeight + TextureHeight(3) - 3, TextureWidth(5), TextureHeight(5)));
        }

        DrawTextureAt(HasTitle ? 11 : 25, Vector2.Zero);
        DrawTextureAt(HasTitle ? 12 : 26, new Vector2(Size.X - TextureWidth(HasTitle ? 12 : 26), 0));

        int bottomIndex = HasFooter ? 126 : 2;
        int bottomHeight = TextureHeight(bottomIndex);
        DrawStretch(bottomIndex, new Rect2(0, Size.Y - bottomHeight, Size.X, bottomHeight));
        DrawTextureAt(8, new Vector2(0, Size.Y - TextureHeight(8)));
        DrawTextureAt(9, new Vector2(Size.X - TextureWidth(9), Size.Y - TextureHeight(9)));
        if (HasFooter)
        {
            DrawStretch(10, new Rect2(TextureWidth(1), Size.Y - bottomHeight - TextureHeight(10),
                Mathf.Max(0, Size.X - TextureWidth(1) * 2), TextureHeight(10)));
            DrawStretch(2, new Rect2(0, Size.Y - bottomHeight - TextureHeight(10) - TextureHeight(2), Size.X, TextureHeight(2)));
        }
    }

    private void DrawStretch(int index, Rect2 destination)
    {
        var texture = MirSkin.GetTexture(LibraryFile.Interface, index);
        if (texture == null || destination.Size.X <= 0 || destination.Size.Y <= 0) return;
        DrawTextureRect(texture, destination, false);
    }

    private void DrawTextureAt(int index, Vector2 position)
    {
        var texture = MirSkin.GetTexture(LibraryFile.Interface, index);
        if (texture != null) DrawTexture(texture, position);
    }

    private static int TextureWidth(int index) => MirSkin.GetTexture(LibraryFile.Interface, index)?.GetWidth() ?? 0;
    private static int TextureHeight(int index) => MirSkin.GetTexture(LibraryFile.Interface, index)?.GetHeight() ?? 0;
}
