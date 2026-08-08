using Godot;

namespace ZirconClient.Scripts;

/// Material for the original DrawTextureBlend/BlendMode.NORMAL path.
internal static class LegacyBlendMaterial
{
    private static Shader _shader;

    public static ShaderMaterial Create()
    {
        _shader ??= GD.Load<Shader>("res://Shaders/LegacyScreenBlend.gdshader");
        return new ShaderMaterial { Shader = _shader };
    }
}
