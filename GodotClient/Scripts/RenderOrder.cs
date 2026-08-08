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

    public static int TerrainMiddle(int renderY) => TerrainBase + Math.Clamp(renderY, 0, 1000) * RowStride;
    public static int TerrainFront(int renderY) => TerrainMiddle(renderY) + 1;
    public static int Object(int renderY) => TerrainMiddle(renderY) + 2;
    public static int ObjectEffect(int renderY) => TerrainMiddle(renderY) + 3;
    // Legacy MapControl draws the local player's target effects after all
    // particle emitters, so keep this above Particles.
    public static int LocalPlayerEffect => Particles + 1;
}
