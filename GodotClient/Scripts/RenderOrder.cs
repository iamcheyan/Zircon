namespace ZirconClient.Scripts;

/// <summary>
/// Global canvas ordering equivalent to Client/Scenes/Views/MapControl.DrawObjects.
/// One map row is: middle terrain, front terrain, objects, object effects.
/// The local player is drawn by the legacy client after every map object.
/// </summary>
public static class RenderOrder
{
    public const int TerrainBase = 100;
    public const int RowStride = 4;
    public const int FloorEffects = 50;
    public const int LocalPlayer = 200_000;
    public const int Particles = 210_000;
    public const int FinalEffects = 220_000;

    public static int TerrainMiddle(int renderY) => TerrainBase + renderY * RowStride;
    public static int TerrainFront(int renderY) => TerrainMiddle(renderY) + 1;
    public static int Object(int renderY) => TerrainMiddle(renderY) + 2;
    public static int ObjectEffect(int renderY) => TerrainMiddle(renderY) + 3;
    // Legacy MapControl draws the local player's target effects after all
    // particle emitters, so keep this above Particles.
    public static int LocalPlayerEffect => Particles + 1;
}
