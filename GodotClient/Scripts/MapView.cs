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
    private string _mapPath = "/home/tetsuya/development/Zircon/Debug/Client/Map/";
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

    private bool _debugLogged;

    public override void _Draw()
    {
        if (Map == null) return;
        if (!_debugLogged)
        {
            _debugLogged = true;
            GD.Print($"[MapView] 首帧诊断: viewport={GetViewport().GetVisibleRect().Size} " +
                     $"center=({CenterX},{CenterY}) range=({ViewRangeX},{ViewRangeY})");
        }

        int sx = Math.Max(0, CenterX - ViewRangeX);
        int sy = Math.Max(0, CenterY - ViewRangeY);
        int ex = Math.Min(Map.Width, CenterX + ViewRangeX);
        int ey = Math.Min(Map.Height, CenterY + ViewRangeY);

        // 屏幕中心偏移
        float offsetX = GetViewport().GetVisibleRect().Size.X / 2 - ViewRangeX * CellWidth;
        float offsetY = GetViewport().GetVisibleRect().Size.Y / 2 - ViewRangeY * CellHeight;

        int drawn = 0;
        for (int x = sx; x < ex; x++)
        {
            for (int y = sy; y < ey; y++)
            {
                ref var cell = ref Map.Cells[x, y];
                float px = (x - CenterX + ViewRangeX) * CellWidth + offsetX;
                float py = (y - CenterY + ViewRangeY) * CellHeight + offsetY;

                // 背景层（半分辨率，只画偶数格; BackFile=0 即 KROrder[0]=Tilesc 地面）
                if (x % 2 == 0 && y % 2 == 0 && cell.BackImage > 0)
                {
                    if (DrawCell(cell.BackFile, cell.BackImage, px, py, false)) drawn++;
                }

                // 中层（跳过 Tilesc 大地面贴图集, 只画 1x1/2x2 装饰）
                if (cell.MiddleFile > 0 && cell.MiddleImage > 0)
                {
                    if (DrawCell(cell.MiddleFile, cell.MiddleImage - 1, px, py, true)) drawn++;
                }

                // 前景
                if (cell.FrontFile > 0 && cell.FrontImage > 0)
                {
                    if (DrawCell(cell.FrontFile, cell.FrontImage - 1, px, py, true)) drawn++;
                }
            }
        }
        if (drawn == 0 && !_warned)
        {
            _warned = true;
            GD.PrintErr("[MapView] 警告: 视野内没有可绘制的格子!");
        }
        else if (!_countLogged)
        {
            _countLogged = true;
            GD.Print($"[MapView] 首帧绘制: {drawn} 格, viewport={GetViewport().GetVisibleRect().Size}");
        }
    }

    private bool _warned;
    private bool _countLogged;

    // skipTilesc: Middle/Front 跳过 Tilesc; 背景层不过滤
    private bool DrawCell(int fileByte, int imageIndex, float px, float py, bool skipTilesc)
    {
        if (imageIndex < 0) return false;
        if (!Libraries.KROrder.TryGetValue(fileByte, out LibraryFile file)) return false;
        if (skipTilesc && file == LibraryFile.Tilesc) return false;

        var lib = GetLibrary(file);
        if (lib == null || imageIndex >= lib.Images.Length) return false;
        if (lib.Images[imageIndex] == null) return false;

        var img = lib.Images[imageIndex];

        // 原客户端: Middle/Front 只画 1x1 或 2x2 尺寸的贴图 (跳过大型贴图集)
        if (skipTilesc && !((img.Width == CellWidth && img.Height == CellHeight) ||
                            (img.Width == CellWidth * 2 && img.Height == CellHeight * 2)))
            return false;

        var texture = lib.GetImageTexture(imageIndex);
        if (texture == null) return false;

        Rect2 dest = new Rect2(px + img.OffSetX, py + img.OffSetY, img.Width, img.Height);
        Rect2 src = new Rect2(0, 0, img.Width, img.Height);
        DrawTextureRectRegion(texture, dest, src);
        return true;
    }

    private ZlLibrary GetLibrary(LibraryFile file)
    {
        return LibraryCache.Get(file);
    }

    public void CenterOn(int x, int y)
    {
        CenterX = x;
        CenterY = y;
        QueueRedraw();
    }
}
