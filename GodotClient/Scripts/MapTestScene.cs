using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Library;
using ZirconClient.Formats;

namespace ZirconClient.Scripts;

public partial class MapTestScene : Control
{
    private Label _statusLabel;
    private string _dataPath = "/home/tetsuya/development/Zircon/Debug/Client/Data/";
    private string _mapPath = "/home/tetsuya/development/Zircon/Debug/Client/Map/";
    private Dictionary<LibraryFile, ZlLibrary> _libCache = new();

    // 网格常量（第 7.1 章）
    const int CellWidth = 48;
    const int CellHeight = 32;

    public override void _Ready()
    {
        _statusLabel = new Label();
        _statusLabel.Position = new Vector2(10, 10);
        _statusLabel.Size = new Vector2(600, 60);
        _statusLabel.ZIndex = 100;
        AddChild(_statusLabel);

        string mapFile = Path.Combine(_mapPath, "0.map");
        GD.Print($"[MapTest] 加载: {mapFile}");

        try
        {
            var map = new MirMap(mapFile);
            GD.Print($"[MapTest] 地图: {map.Width}x{map.Height}");

            // 统计单元格数据
            int bgCount = 0, midCount = 0, frontCount = 0;
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 20; y++)
                {
                    if (map.Cells[x, y].BackFile > 0) bgCount++;
                    if (map.Cells[x, y].MiddleFile > 0) midCount++;
                    if (map.Cells[x, y].FrontFile > 0) frontCount++;
                }
            GD.Print($"[MapTest] 20x20 区域: 背景={bgCount}, 中层={midCount}, 前景={frontCount}");

            // 渲染 20x20 区域
            RenderArea(map, 0, 0, 20, 20);
            _statusLabel.Text = $"地图 0.map: {map.Width}x{map.Height}\n渲染 20x20 区域完成";
            GD.Print("[MapTest] 渲染完成");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"失败: {ex.Message}";
            GD.PrintErr($"[MapTest] {ex}");
        }
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

    private void RenderArea(MirMap map, int startX, int startY, int viewW, int viewH)
    {
        for (int x = startX; x < startX + viewW && x < map.Width; x++)
        {
            for (int y = startY; y < startY + viewH && y < map.Height; y++)
            {
                ref var cell = ref map.Cells[x, y];
                float px = x * CellWidth;
                float py = y * CellHeight;

                // 背景层（半分辨率，只画偶数格）
                if (x % 2 == 0 && y % 2 == 0 && cell.BackFile > 0)
                {
                    DrawCell(cell.BackFile, cell.BackImage, px, py);
                }

                // 中层
                if (cell.MiddleFile > 0 && cell.MiddleImage > 0)
                {
                    DrawCell(cell.MiddleFile, cell.MiddleImage - 1, px, py);
                }

                // 前景
                if (cell.FrontFile > 0 && cell.FrontImage > 0)
                {
                    DrawCell(cell.FrontFile, cell.FrontImage - 1, px, py);
                }
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
        var sprite = new Sprite2D();
        sprite.Texture = texture;
        sprite.Position = new Vector2(px, py);
        // 贴图偏移（OffSetY 让贴图底边对齐格子）
        sprite.Offset = new Vector2(img.OffSetX, img.OffSetY);
        AddChild(sprite);
    }
}
