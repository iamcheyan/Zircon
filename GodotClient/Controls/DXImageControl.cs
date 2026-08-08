using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 贴图控件 (移植自 Client/Controls/DXImageControl.cs)。
/// 用 .Zl 图库的一张图绘制自己; 支持普通/悬停/按下三态与偏移。
/// </summary>
public partial class DXImageControl : DXControl
{
    private ShaderMaterial _blendMaterial;
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

        // 旧版 DXImageControl 的 Blend 标记通过
        // RenderingPipelineManager.SetBlend(true, ImageOpacity, BlendMode)
        // 绘制：ImageOpacity 落在 NORMAL 混合的 blendRate 参数上并被忽略
        // （AppliesBlendRateToVertexColour 不含 NORMAL），顶点 Alpha 保持
        // ForeColour 全不透明。只有非 Blend 路径 SetOpacity(ImageOpacity)
        // 才会衰减顶点 Alpha。
        if (GrayScale)
        {
            Material = Blend ? (_grayBlendMaterial ??= CreateGrayMaterial(true))
                : (_grayMaterial ??= CreateGrayMaterial(false));
        }
        else
        {
            Material = Blend ? (_blendMaterial ??= LegacyBlendMaterial.Create()) : null;
        }

        Vector2I off = UseOffSet ? MirSkin.GetOffset(LibraryFile, idx) : Vector2I.Zero;
        Rect2 dest = new(off, tex.GetSize());

        // 旧版 PresentTexture 会把 ForeColour（禁用时为灰色）直接乘到
        // 贴图，并把 ImageOpacity 作为源 Alpha；在贴图上再盖半透明灰块
        // 会改变透明边缘和黑色有效像素，不能作为等价实现。
        Color tint = IsEnabled ? ForeColour : new Color(0.29f, 0.29f, 0.29f, 1f);
        if (!Blend)
            tint.A *= Mathf.Clamp(ImageOpacity, 0f, 1f);
        DrawTextureRect(tex, dest, false, tint);
    }

    internal static ShaderMaterial CreateGrayMaterial(bool blend)
    {
        // 原版 GrayscaleD3D11.hlsl（默认管线）：
        //   gray = dot(texel.rgb, (0.299, 0.587, 0.114))   // 直通 texel
        //   out.rgb = gray * Col.rgb * texel.a * Col.a     // 预乘两个 Alpha
        //   out.a   = texel.a * Col.a
        // Blend 变体再叠加 NORMAL 屏幕混合 out = src*(1-dst)+dst。
        // Godot 贴图为直通 RGBA8，因此 texel.a 预乘必须在 shader 内完成；
        // 灰度值只能对直通 texel.rgb 计算，不能先乘 COLOR（原版 Col 只乘
        // 一次，出现在 gray 之外）。
        // 两个变体都用普通 mix 输出完整结果 (alpha=1)，不用 blend_add：
        // Godot 的 canvas blend_add 把 shader 输出当预乘处理
        // (贡献 = COLOR.rgb*COLOR.a)，而 screen 公式的 alpha 通道
        // (texel.a*(1-dst.a)) 会把 RGB 贡献压成 0，导致特效不可见。
        // 另：Godot 默认 mix 的混合因子是 TEXTURE alpha (final =
        // COLOR.rgb*texel.a + dst*(1-texel.a))，输出会被 texel.a 再预乘一次；
        // 故 src 项除以 texel.a 补偿，最终 = src*(1-dst)+dst (旧端精确数学)。
        // 非 blend 变体（普通 alpha 混合 out = src + dst*(1-src.a)）同样输出
        // alpha=1、在 shader 内用 screen_texture 完成 dst 项：实测 Godot 对
        // alpha<1 的 shader 输出混入额外预乘（常量 a=0.5 的 shader 在黑底上
        // 渲染出 0.75 而非 0.5），无法按标准公式预测；alpha=1 + 显式 dst 项
        // 则字节级可验证。
        // 注意 Godot 2D 贴图上传即预乘：texel.rgb = 直通.rgb * texel.a，
        // 故 l = dot(texel.rgb) 已含 texel.a；src = l * Col.rgb * Col.a
        // = 直通gray * Col.rgb * (texel.a*Col.a)，与旧端单次预乘一致。
        var shader = new Shader
        {
            Code = "shader_type canvas_item;\nuniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;\n" +
                "void fragment() {\n" +
                "    vec4 texel = texture(TEXTURE, UV);\n" +
                "    float l = dot(texel.rgb, vec3(0.299, 0.587, 0.114));\n" +
                (blend
                    ? "    vec4 destination = textureLod(screen_texture, SCREEN_UV, 0.0);\n" +
                      "    vec3 source = vec3(l) * COLOR.rgb * texel.a * COLOR.a;\n" +
                      "    vec3 out_rgb = destination.rgb + source * (vec3(1.0) - destination.rgb) / max(texel.a, 0.0001);\n" +
                      "    COLOR = vec4(out_rgb, 1.0);\n"
                    : "    vec4 destination = textureLod(screen_texture, SCREEN_UV, 0.0);\n" +
                      "    float a = texel.a * COLOR.a;\n" +
                      "    vec3 source = vec3(l) * COLOR.rgb * COLOR.a;\n" +
                      "    COLOR = vec4(source + destination.rgb * (1.0 - a), 1.0);\n") +
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
