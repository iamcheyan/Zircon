using System;

namespace ZirconClient.Scripts;

/// <summary>
/// Global canvas ordering equivalent to Client/Scenes/Views/MapControl.DrawObjects.
/// One map row is: middle terrain, front terrain, objects, object effects.
/// The local player is drawn by the legacy client after every map object.
/// </summary>
public static class RenderOrder
{
    // Godot CanvasItem.ZIndex is limited to [-4096, 4096].  The previous
    // values copied the old client's unrestricted painter-order integers and
    // were rejected by Godot at runtime, which could make the layer order
    // undefined.  The visible map range only needs a compact relative order.
    public const int TerrainBase = 0;
    // Four distinct slots are required; with three slots the object-effect
    // slot equals the next row's middle terrain slot.
    public const int RowStride = 4;
    public const int FloorEffects = 50;
    public const int LocalPlayer = 3200;
    public const int Particles = 3300;
    public const int FinalEffects = 3400;
    // 原版 MapControl.OnBeforeDraw 在 DrawObjects()(地形+对象+天气粒子)
    // 之后才绘制 LLayer 光纹理做全屏合成: 夜晚环境光应覆盖包括对象在内的
    // 全部世界内容, 光源光斑再在覆盖层上恢复亮度。此处取 FinalEffects 之后
    // 最后一个世界槽位, 让 hint_screen_texture 采样到完整的场景。
    public const int LightOverlay = FinalEffects + 1;

    public static int TerrainMiddle(int renderY) => TerrainBase + Math.Clamp(renderY, 0, 1000) * RowStride;
    public static int TerrainFront(int renderY) => TerrainMiddle(renderY) + 1;
    public static int Object(int renderY) => TerrainMiddle(renderY) + 2;
    public static int ObjectEffect(int renderY) => TerrainMiddle(renderY) + 3;
    // Legacy MapControl draws the local player's target effects after all
    // particle emitters, so keep this above Particles.
    public static int LocalPlayerEffect => Particles + 1;
}
