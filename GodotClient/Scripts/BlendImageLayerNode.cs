using Godot;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

// Additive layer for legacy DrawBlend calls whose source is an ordinary image
// (not a black-key effect texture).  Used by player exterior equipment.
public partial class BlendImageLayerNode : Node2D
{
    private readonly CanvasItemMaterial _material = new();

    private ZlLibrary _library;
    private int _frame = -1;
    private float _offsetX;
    private float _offsetY;
    private Color _colour = Colors.White;

    public BlendImageLayerNode()
    {
        Material = _material;
    }

    public void Configure(ZlLibrary library, int frame, Color colour, int zIndex,
        float offsetX = 0f, float offsetY = 0f, bool additive = true)
    {
        _library = library;
        _frame = frame;
        _colour = colour;
        _offsetX = offsetX;
        _offsetY = offsetY;
        _material.BlendMode = additive
            ? CanvasItemMaterial.BlendModeEnum.Add
            : CanvasItemMaterial.BlendModeEnum.Mix;
        ZIndex = zIndex;
        Visible = library != null && frame >= 0;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Visible || _library == null || _frame < 0 || _frame >= _library.Images.Length)
            return;
        var image = _library.Images[_frame];
        if (image == null || image.Width <= 0 || image.Height <= 0) return;
        var texture = _library.GetImageTexture(_frame);
        if (texture == null) return;
        DrawTextureRectRegion(texture,
            new Rect2(_offsetX + image.OffSetX, _offsetY + image.OffSetY, image.Width, image.Height),
            new Rect2(0, 0, image.Width, image.Height), _colour);
    }
}
