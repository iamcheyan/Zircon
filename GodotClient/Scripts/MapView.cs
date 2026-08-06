using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Library;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

// 地图渲染视图: 加载 .map + 渲染地形层，可滚动
public partial class MapView : Node2D
{
    private string _dataPath = "/home/tetsuya/development/Zircon/Debug/Client/Data/";
    private string _mapPath = "/home/tetsuya/development/Zircon/Debug/Client/Map/";
    private Dictionary<LibraryFile, ZlLibrary> _libCache = new();
    public MirMap Map { get; private set; }
    public string MapFileName { get; private set; }

    const int CellWidth = 48;
    const int CellHeight = 32;

    // 当前视野中心（玩家位置）
    public int CenterX = 0;
    public int CenterY = 0;
    public int ViewRangeX = 12;
    public int ViewRangeY = 15;

    public void LoadMap(string mapFileName)
    {
        MapFileName = mapFileName;
        string full = Path.Combine(_mapPath, mapFileName + ".map");
        Map = new MirMap(full);
        GD.Print($"[MapView] 加载 {mapFileName}: {Map.Width}x{Map.Height}");
    }

    public override void _Draw()
    {
        if (Map == null) return;

        int sx = Math.Max(0, CenterX - ViewRangeX);
        int sy = Math.Max(0, CenterY - ViewRangeY);
        int ex = Math.Min(Map.Width, CenterX + ViewRangeX);
        int ey = Math.Min(Map.Height, CenterY + ViewRangeY);

        // 屏幕中心偏移
        float offsetX = GetViewport().GetVisibleRect().Size.X / 2 - ViewRangeX * CellWidth;
        float offsetY = GetViewport().GetVisibleRect().Size.Y / 2 - ViewRangeY * CellHeight;

        for (int x = sx; x < ex; x++)
        {
            for (int y = sy; y < ey; y++)
            {
                ref var cell = ref Map.Cells[x, y];
                float px = (x - CenterX + ViewRangeX) * CellWidth + offsetX;
                float py = (y - CenterY + ViewRangeY) * CellHeight + offsetY;

                // 背景层
                if (x % 2 == 0 && y % 2 == 0 && cell.BackFile > 0)
                    DrawCell(cell.BackFile, cell.BackImage, px, py);

                // 中层
                if (cell.MiddleFile > 0 && cell.MiddleImage > 0)
                    DrawCell(cell.MiddleFile, cell.MiddleImage - 1, px, py);

                // 前景
                if (cell.FrontFile > 0 && cell.FrontImage > 0)
                    DrawCell(cell.FrontFile, cell.FrontImage - 1, px, py);
            }
        }
    }

    private void DrawCell(int fileByte, int imageIndex, float px, float py)
    {
        if (fileByte == 0 || imageIndex < 0) return;
        if (!Libraries.KROrder.TryGetValue(fileByte, out LibraryFile file)) return;
        if (file == LibraryFile.Tilesc) return;

        var lib = GetLibrary(file);
        if (lib == null || imageIndex >= lib.Images.Length) return;
        if (lib.Images[imageIndex] == null) return;

        var texture = lib.GetImageTexture(imageIndex);
        if (texture == null) return;

        var img = lib.Images[imageIndex];
        Rect2 dest = new Rect2(px + img.OffSetX, py + img.OffSetY, img.Width, img.Height);
        Rect2 src = new Rect2(0, 0, img.Width, img.Height);
        DrawTextureRectRegion(texture, dest, src);
    }

    private ZlLibrary GetLibrary(LibraryFile file)
    {
        if (_libCache.TryGetValue(file, out var lib)) return lib;
        if (!Libraries.LibraryList.TryGetValue(file, out string path)) return null;
        if (path.StartsWith("Data/")) path = path.Substring(5);
        path = path.Replace('\\', '/');
        string fullPath = Path.Combine(_dataPath, path);
        if (!File.Exists(fullPath)) return null;
        lib = new ZlLibrary(fullPath);
        _libCache[file] = lib;
        return lib;
    }

    public void CenterOn(int x, int y)
    {
        CenterX = x;
        CenterY = y;
        QueueRedraw();
    }
}
