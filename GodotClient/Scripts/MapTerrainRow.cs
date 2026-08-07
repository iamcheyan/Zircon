using Godot;

namespace ZirconClient.Scripts;

// 地图地形的单行 CanvasItem。独立节点让大型地图贴图参与全局 Y 排序，
// 不会因为 MapView 整体先绘制而永远压在角色后面。
public partial class MapTerrainRow : Node2D
{
    public MapView OwnerView;
    public int Row;

    public override void _Draw()
    {
        OwnerView?.DrawTerrainRow(this, Row);
    }
}
