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
    private string _mapPath;
    public MirMap Map { get; private set; }
    public string MapFileName { get; private set; }
    public int BackgroundIndex { get; private set; }
    public int MissingLibraryCount { get; private set; }
    public int MissingTextureCount { get; private set; }

    const int CellWidth = 48;
    const int CellHeight = 32;
    private const float WorldScale = 2f;

    // 当前视野中心（玩家位置）
    public int CenterX = 0;
    public int CenterY = 0;
    public int ViewRangeX = 12;
    public int ViewRangeY = 15;

    private double _animationTime;
    private int _mapAnimation;
    private readonly Dictionary<int, MapTerrainRow> _terrainRows = new();

    private const int ManualHeightOffset = 34;

    public override void _Ready()
    {
        var projectDir = ProjectSettings.GlobalizePath("res://");
        _mapPath = Path.GetFullPath(Path.Combine(projectDir, "..", "Debug", "Client", "Map"));
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        if (Map == null) return;
        _animationTime += delta;
        if (_animationTime < 0.1) return;
        _animationTime = 0;
        _mapAnimation++;
        foreach (var row in _terrainRows.Values)
            row.QueueRedraw();
        QueueRedraw();
    }

    public void LoadMap(string mapFileName, int backgroundIndex = 0)
    {
        MapFileName = mapFileName;
        BackgroundIndex = backgroundIndex;
        MissingLibraryCount = 0;
        MissingTextureCount = 0;
        _debugLogged = false;
        _warned = false;
        _countLogged = false;
        string full = Path.Combine(_mapPath, mapFileName + ".map");
        Map = new MirMap(full);
        GD.Print($"[MapView] 加载 {mapFileName}: {Map.Width}x{Map.Height}");
        SyncTerrainRows();
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

        // 原版地图层与对象层使用不同的 Y 基线：地图地面不加一格，
        // 中/前景对象加一格，并以贴图底边对齐。下方额外保留空间，
        // 否则高建筑会在视野边缘被截断。
        int sx = Math.Max(0, CenterX - ViewRangeX - 4);
        int sy = Math.Max(0, CenterY - ViewRangeY - 4);
        int ex = Math.Min(Map.Width, CenterX + ViewRangeX + 5);
        int ey = Math.Min(Map.Height, CenterY + ViewRangeY + 26);

        Vector2 viewport = GetViewport().GetVisibleRect().Size / WorldScale;
        float offsetX = (viewport.X - CellWidth) / 2f - ViewRangeX * CellWidth;
        float offsetY = (viewport.Y - CellHeight) / 2f - ViewRangeY * CellHeight - ManualHeightOffset;

        int drawn = 0;
        DrawMapBackground(viewport);
        // 背景层：.map 只在偶数格保存一项，不能和中/前景混在同一基线。
        for (int x = sx; x < ex; x++)
        {
            for (int y = sy; y < ey; y++)
            {
                ref var cell = ref Map.Cells[x, y];
                if (x % 2 != 0 || y % 2 != 0 || cell.BackImage <= 0) continue;
                float px = (x - CenterX + ViewRangeX) * CellWidth + offsetX;
                float py = (y - CenterY + ViewRangeY) * CellHeight + offsetY;
                if (DrawCell(cell.BackFile, cell.BackImage, px, py, false, false, 0)) drawn++;
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
            GD.Print($"[MapView] 贴图诊断: missingLibraries={MissingLibraryCount}, missingTextures={MissingTextureCount}");
        }
    }

    private bool _warned;
    private bool _countLogged;

    private int AnimatedIndex(int baseIndex, int animationFrame, out bool blend)
    {
        blend = false;
        if (animationFrame > 1 && animationFrame < 255)
        {
            int count = animationFrame & 0x0F;
            blend = (animationFrame & 0x80) != 0;
            if (count > 0) baseIndex += _mapAnimation % count;
        }
        return baseIndex;
    }

    private void DrawMapBackground(Vector2 viewport)
    {
        if (BackgroundIndex <= 0) return;
        var lib = LibraryCache.Get(LibraryFile.Background);
        if (lib == null || BackgroundIndex >= lib.Images.Length)
        {
            MissingLibraryCount++;
            return;
        }
        var tex = lib.GetImageTexture(BackgroundIndex);
        if (tex == null)
        {
            MissingTextureCount++;
            return;
        }
        DrawTextureRect(tex, new Rect2(Vector2.Zero, viewport), false);
    }

    private void SyncTerrainRows()
    {
        if (Map == null) return;
        int first = Math.Max(0, CenterY - ViewRangeY - 4);
        int last = Math.Min(Map.Height - 1, CenterY + ViewRangeY + 25);

        foreach (var pair in _terrainRows)
            pair.Value.Visible = pair.Key >= first && pair.Key <= last;

        for (int y = first; y <= last; y++)
        {
            if (!_terrainRows.TryGetValue(y, out var row))
            {
                row = new MapTerrainRow { OwnerView = this, Row = y };
                _terrainRows[y] = row;
                AddChild(row);
            }
            row.Row = y;
            row.ZIndex = 100 + y;
            row.QueueRedraw();
        }
    }

    // 由 MapTerrainRow 调用。每一行独立成为 CanvasItem，才能和角色
    // 使用同一套全局 Z 顺序；绘制规则仍然保持原版 y->x->中层->前景。
    public void DrawTerrainRow(CanvasItem canvas, int y)
    {
        if (Map == null || y < 0 || y >= Map.Height) return;

        Vector2 viewport = GetViewport().GetVisibleRect().Size / WorldScale;
        float offsetX = (viewport.X - CellWidth) / 2f - ViewRangeX * CellWidth;
        float offsetY = (viewport.Y - CellHeight) / 2f - ViewRangeY * CellHeight - ManualHeightOffset;
        int firstX = Math.Max(0, CenterX - ViewRangeX - 4);
        int lastX = Math.Min(Map.Width - 1, CenterX + ViewRangeX + 4);

        for (int x = firstX; x <= lastX; x++)
        {
            ref var cell = ref Map.Cells[x, y];
            float px = (x - CenterX + ViewRangeX) * CellWidth + offsetX;
            float py = (y - CenterY + ViewRangeY + 1) * CellHeight + offsetY;

            if (cell.MiddleImage > 0)
            {
                int index = AnimatedIndex(cell.MiddleImage - 1, cell.MiddleAnimationFrame,
                    out bool blend);
                DrawCell(canvas, cell.MiddleFile, index, px, py, true, blend, CellHeight);
            }

            if (cell.FrontImage > 0)
            {
                int index = AnimatedIndex(cell.FrontImage - 1, cell.FrontAnimationFrame,
                    out bool blend);
                DrawCell(canvas, cell.FrontFile, index, px, py, true, blend, CellHeight);
            }
        }
    }

    // skipTilesc: Middle/Front 跳过 Tilesc；背景层允许 fileByte=0。
    // baselineHeight=0 表示背景层；否则使用 max(贴图高度, baselineHeight)
    // 的底边对齐规则。
    private bool DrawCell(int fileByte, int imageIndex, float px, float py,
        bool skipTilesc, bool blend, int baselineHeight)
        => DrawCell(this, fileByte, imageIndex, px, py, skipTilesc, blend, baselineHeight);

    private bool DrawCell(CanvasItem canvas, int fileByte, int imageIndex, float px, float py,
        bool skipTilesc, bool blend, int baselineHeight)
    {
        if (imageIndex < 0) return false;
        if (!Libraries.KROrder.TryGetValue(fileByte, out LibraryFile file))
        {
            MissingLibraryCount++;
            return false;
        }
        if (skipTilesc && file == LibraryFile.Tilesc) return false;

        var lib = GetLibrary(file);
        if (lib == null || imageIndex >= lib.Images.Length)
        {
            MissingLibraryCount++;
            return false;
        }
        if (lib.Images[imageIndex] == null)
        {
            MissingTextureCount++;
            return false;
        }

        var img = lib.Images[imageIndex];

        var texture = lib.GetImageTexture(imageIndex);
        if (texture == null)
        {
            MissingTextureCount++;
            return false;
        }

        float y = baselineHeight == 0 ? py : py -
            ((img.Width == CellWidth || img.Width == CellWidth * 2) &&
             (img.Height == CellHeight || img.Height == CellHeight * 2)
                ? baselineHeight : img.Height);
        Rect2 dest = new Rect2(px + img.OffSetX, y + img.OffSetY, img.Width, img.Height);
        Rect2 src = new Rect2(0, 0, img.Width, img.Height);
        canvas.DrawTextureRectRegion(texture, dest, src,
            blend ? new Color(1f, 1f, 1f, 0.5f) : Colors.White);
        return true;
    }

    public Vector2 CellToScreen(int cellX, int cellY, bool objectBaseline)
    {
        Vector2 viewport = GetViewport().GetVisibleRect().Size / WorldScale;
        float offsetX = (viewport.X - CellWidth) / 2f - ViewRangeX * CellWidth;
        float offsetY = (viewport.Y - CellHeight) / 2f - ViewRangeY * CellHeight - ManualHeightOffset;
        return new Vector2(
            (cellX - CenterX + ViewRangeX) * CellWidth + offsetX,
            (cellY - CenterY + ViewRangeY + (objectBaseline ? 1 : 0)) * CellHeight + offsetY);
    }

    private ZlLibrary GetLibrary(LibraryFile file)
    {
        return LibraryCache.Get(file);
    }

    public void CenterOn(int x, int y)
    {
        CenterX = x;
        CenterY = y;
        SyncTerrainRows();
        QueueRedraw();
    }
}
