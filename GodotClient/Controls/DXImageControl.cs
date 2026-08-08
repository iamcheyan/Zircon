using Godot;
using Library;

namespace ZirconClient.Controls;

/// <summary>
/// 贴图控件 (移植自 Client/Controls/DXImageControl.cs)。
/// 用 .Zl 图库的一张图绘制自己; 支持普通/悬停/按下三态与偏移。
/// </summary>
public partial class DXImageControl : DXControl
{
    private CanvasItemMaterial _blendMaterial;
    private ShaderMaterial _grayMaterial;
    private ShaderMaterial _grayBlendMaterial;
    public LibraryFile LibraryFile = LibraryFile.Interface;

    private int _index = -1;
    public int Index
    {
        get => _index;
        set
        {
            if (_index == value) return;
            _index = value;
            QueueRedraw();
            if (!FixedSize)
                Size = MirSkin.GetSize(LibraryFile, value);
        }
    }

    public int HoverIndex = -1;
    public int PressedIndex = -1;

    /// <summary>true: 尺寸固定为 Size, 不随图自动调整 (贴图偏移仍生效)</summary>
    public bool FixedSize;

    public bool DrawImage = true;

    /// <summary>原版 Blend 标记；图库 UI 光效使用高亮混合。</summary>
    public bool Blend;

    /// <summary>绘制图库的 Overlay 层，而不是普通 Image 层。</summary>
    public bool UseOverlayTexture;

    /// <summary>true: 绘制时加上图的 OffSetX/OffSetY 偏移</summary>
    public bool UseOffSet = true;

    public float ImageOpacity = 1f;
    public bool GrayScale;

    public override void _Ready()
    {
        // 原版 DXWindow/DXImageControl 对 Interface[15] 关闭按钮统一提供
        // CommonControlClose Hint；窗口若有更具体提示，可在此之前覆盖。
        if (LibraryFile == LibraryFile.Interface && Index == 15 && string.IsNullOrEmpty(TooltipText))
            TooltipText = "关闭";
        base._Ready();
    }

    protected override void DrawControl()
    {
        if (!DrawImage) return;

        int idx = GetCurrentIndex();
        if (idx < 0) return;

        var tex = UseOverlayTexture
            ? MirSkin.GetOverlayTexture(LibraryFile, idx)
            : MirSkin.GetTexture(LibraryFile, idx);
        if (tex == null) return;

        // 旧版 DXImageControl 在 Blend=true 时切换到亮化混合，而不是
        // 普通 SourceAlpha/InverseSourceAlpha。Godot 的 Add 模式对应旧版
        // NORMAL blend 的视觉用途：光效叠加会提亮底图，不再变成灰暗透明层。
        if (GrayScale)
        {
            Material = Blend ? (_grayBlendMaterial ??= CreateGrayMaterial(true))
                : (_grayMaterial ??= CreateGrayMaterial(false));
        }
        else
        {
            Material = Blend ? (_blendMaterial ??= new CanvasItemMaterial
            {
                BlendMode = CanvasItemMaterial.BlendModeEnum.Add
            }) : null;
        }

        Vector2I off = UseOffSet ? MirSkin.GetOffset(LibraryFile, idx) : Vector2I.Zero;
        Rect2 dest = new(off, tex.GetSize());

        // 旧版 PresentTexture 会把 ForeColour（禁用时为灰色）直接乘到
        // 贴图，并把 ImageOpacity 作为源 Alpha；在贴图上再盖半透明灰块
        // 会改变透明边缘和黑色有效像素，不能作为等价实现。
        Color tint = IsEnabled ? ForeColour : new Color(0.29f, 0.29f, 0.29f, 1f);
        tint.A *= Mathf.Clamp(ImageOpacity, 0f, 1f);
        if (GrayScale)
        {
            // 灰度由材质对贴图 RGB 处理，tint 仍负责旧版 ForeColour/Opacity。
        }
        DrawTextureRect(tex, dest, false, tint);
    }

    private static ShaderMaterial CreateGrayMaterial(bool additive)
    {
        var shader = new Shader
        {
            Code = (additive ? "shader_type canvas_item; render_mode blend_add;\n" : "shader_type canvas_item;\n") +
                "void fragment() {\n" +
                "    vec4 sampled = texture(TEXTURE, UV) * COLOR;\n" +
                "    float l = dot(sampled.rgb, vec3(0.299, 0.587, 0.114));\n" +
                "    COLOR = vec4(vec3(l), sampled.a);\n" +
                "}"
        };
        return new ShaderMaterial { Shader = shader };
    }

    /// <summary>当前应显示的图索引 (按下 > 悬停 > 普通)</summary>
    public int GetCurrentIndex()
    {
        if (IsPressed && PressedIndex >= 0) return PressedIndex;
        if (IsHovered && HoverIndex >= 0) return HoverIndex;
        return Index;
    }
}
