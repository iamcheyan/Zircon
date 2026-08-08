using Godot;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

/// <summary>
/// 单独的亮化序列帧层。Godot 的 CanvasItemMaterial 作用于整个节点，
/// 因此需要把原版 DrawBlend 的附加层拆成子节点，不能把主体也一起 Add。
/// </summary>
public partial class BlendLayerNode : Node2D
{
    private readonly CanvasItemMaterial _blendMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Mix
    };

    private ZlLibrary _library;
    private int _frame = -1;
    private float _offsetX;
    private float _offsetY;
    private Color _colour = Colors.White;

    public BlendLayerNode()
    {
        Material = _blendMaterial;
        ZIndex = 1;
    }

    public void Configure(ZlLibrary library, int frame, float offsetX, float offsetY, Color colour)
    {
        _library = library;
        _frame = frame;
        _offsetX = offsetX;
        _offsetY = offsetY;
        _colour = colour;
        Visible = library != null && frame >= 0;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Visible || _library == null || _frame < 0 || _frame >= _library.Images.Length)
            return;

        var image = _library.Images[_frame];
        if (image == null || image.Width <= 0 || image.Height <= 0)
            return;

        // ExteriorEffectManager calls DrawBlend(..., ImageType.Image).
        var texture = _library.GetImageTexture(_frame);
        if (texture == null)
            return;

        DrawTextureRectRegion(texture,
            new Rect2(_offsetX + image.OffSetX, _offsetY + image.OffSetY, image.Width, image.Height),
            new Rect2(0, 0, image.Width, image.Height), _colour);
    }
}
