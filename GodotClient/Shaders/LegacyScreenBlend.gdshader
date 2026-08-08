shader_type canvas_item;

// Legacy BlendMode.NORMAL is screen blending in the original renderer:
//   out = src * (1 - dst) + dst        (colour AND alpha)
// Original math (verified against the registered pipelines, SilkD3D11 default
// + SilkVulkan):
//   src.rgb = texel.rgb * texel.a * vertexColour.rgb * vertexColour.a
//   src.a   = texel.a * vertexColour.a
//
// Godot 4 canvas pipeline (byte-verified in MapTestScene's blend audit):
//   - texture(TEXTURE, UV) returns the STRAIGHT texel (rgb and a unpremultiplied).
//   - The fragment COLOR input is already vertexColour * texel: the canvas
//     template executes color *= texture(color_texture, uv) before user code.
//     So COLOR.rgb carries an extra texel.rgb factor and COLOR.a carries texel.a.
//   - screen_texture is the same-frame framebuffer right before this item.
//   - The default MIX factor is the emitted COLOR.a, NOT the TEXTURE alpha.
//     This shader emits alpha 1.0, so the factor is 1 and the written colour
//     is exactly COLOR.rgb (byte-verifiable; no hidden re-premultiply).
// With COLOR = vertexColour * texel the naive src = texel.rgb * texel.a *
// COLOR.rgb * COLOR.a equals vertexColour.rgb*vertexColour.a * texel.rgb^2 *
// texel.a^2 — off by texel.rgb for non-binary texels (the white/red test
// texels with channels in {0,1} hid this). Dropping the leading texel.rgb
// (COLOR.rgb already contains it):
//   src.rgb = texel.a * COLOR.rgb * COLOR.a
//          = vertexColour.rgb * vertexColour.a * texel.rgb * texel.a   = exact.
// The /max(texel.a) then cancels the texel.a hidden inside COLOR.a, yielding
// the exact original out = dst + src*(1-dst). sRGB note: texture() returns
// straight linear RGB, matching the original renderer's UNORM_SRGB pipeline.
//
// Implemented as plain mix outputting the FULL screen-blend result with
// alpha 1.0, NOT blend_add: Godot's canvas blend_add treats the shader output
// as premultiplied (contribution = COLOR.rgb * COLOR.a), so with the screen
// formula's alpha the RGB contribution collapses to 0 and the effect becomes
// invisible.
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

void fragment()
{
    vec4 texel = texture(TEXTURE, UV);
    // Transparent sprite pixels must not write the sampled screen colour.
    // Writing destination with alpha=1 makes the entire frame rectangle
    // opaque, which is especially visible at the client's 2x world scale.
    if (texel.a <= 0.01)
        discard;
    vec3 source_rgb = texel.a * COLOR.rgb * COLOR.a;
    vec4 destination = textureLod(screen_texture, SCREEN_UV, 0.0);
    vec3 out_rgb = destination.rgb
        + source_rgb * (vec3(1.0) - destination.rgb) / max(texel.a, 0.0001);
    COLOR = vec4(out_rgb, 1.0);
}
