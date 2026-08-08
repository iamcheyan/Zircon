shader_type canvas_item;

// Legacy BlendMode.NORMAL is screen blending in the original renderer:
//   out = src * (1 - dst) + dst        (colour AND alpha)
// Verified against the registered pipelines (SilkD3D11 default + SilkVulkan):
//   src.rgb = texel.rgb * texel.a * vertexColour.rgb * vertexColour.a
//   src.a   = texel.a * vertexColour.a
// i.e. RGB is premultiplied by texture alpha AND vertex alpha (D3D11 does it
// in the pixel shader; Vulkan premultiplies at upload for Bgra32/decompressed
// DXT and in the shader for native BC — same effective math). The Godot port
// feeds straight-alpha RGBA8, so this shader must apply the texel.a factor
// itself. blendRate is NOT part of NORMAL blending (the original
// AppliesBlendRateToVertexColour excludes NORMAL), so vertex alpha is simply
// COLOR.a.
//
// Implemented as plain mix outputting the FULL screen-blend result with
// alpha 1.0, NOT blend_add: Godot's canvas blend_add treats the shader output
// as premultiplied (contribution = COLOR.rgb * COLOR.a), so with the screen
// formula's alpha = src.a*(1-dst.a) the RGB contribution collapses to 0 and
// the effect becomes invisible.
//
// Second Godot quirk, verified pixel-level: the default canvas mix uses the
// TEXTURE alpha as the blend factor (final = COLOR.rgb * texel.a + dst.rgb *
// (1 - texel.a)), i.e. the shader's output rgb is premultiplied AGAIN by
// texel.a at blend time. Naively emitting out = src*(1-dst) + dst therefore
// renders semi-transparent texels too dark (white a=0.5 on black measured
// 0.25 instead of 0.5). Compensate by dividing the src term by texel.a:
//   COLOR.rgb = dst + src*(1-dst)/texel.a
// gives final = (dst + src*(1-dst)/texel.a)*texel.a + dst*(1-texel.a)
//            = src*(1-dst) + dst         <- exact original D3D11 math
// Verified on black and green backdrops (white a0.5 -> 0.5; orange a0.8 on
// green -> matches original). Also note sRGB textures: texture() returns
// linear RGB with straight alpha, which matches the original renderer's
// UNORM_SRGB pipeline, so no extra gamma handling is needed.
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

void fragment()
{
    vec4 texel = texture(TEXTURE, UV);
    vec3 source_rgb = texel.rgb * texel.a * COLOR.rgb * COLOR.a;
    vec4 destination = textureLod(screen_texture, SCREEN_UV, 0.0);
    vec3 out_rgb = destination.rgb
        + source_rgb * (vec3(1.0) - destination.rgb) / max(texel.a, 0.0001);
    COLOR = vec4(out_rgb, 1.0);
}
