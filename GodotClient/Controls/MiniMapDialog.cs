using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 右上角小地图 (移植自 Client/Scenes/Views/MiniMapDialog.cs)。
/// MiniMap.Zl 贴图 + NPC/出口静态标记 + 怪物/物品/玩家动态标记;
/// 玩家居中跟随 (Image 平移), 右上三按钮悬停显示。
/// </summary>
public partial class MiniMapDialog : DXWindow
{
    public Rect2 Area;
    public DXImageControl Image;
    public DXControl Panel;
    public DXButton SizeButton, TransparencyButton, BigMapButton;

    private static readonly Vector2I DefaultMiniMapSize = new(200, 200);
    private static readonly Vector2I LargeMiniMapSize = new(300, 300);
    private const float TransparentOpacity = 0.5F;
    private bool IsLarge, IsTransparent;

    public static float ScaleX, ScaleY;

    // 动态标记: ObjectID -> 色点 (怪物/物品/其他玩家); NPC/出口标记在 SetMap 重建
    private readonly Dictionary<uint, DXMapInfoControl> _objectMarkers = new();

    private int _mapWidth, _mapHeight;
    private int _mapIndex;
    private uint _playerObjectID;
    private bool _hasPlayer;

    private Action _onBigMapRequest;

    public override void _Ready()
    {
        base._Ready();
        UpdateButtonLocations();
        Area = new Rect2(0, TitleHeight, Size.X, Size.Y - TitleHeight);
        Panel.Location = (Vector2I)Area.Position;
        Panel.Size = Area.Size;
    }

    public MiniMapDialog()
    {
        BackColour = Colors.Black;
        HasFooter = false;
        Size = DefaultMiniMapSize;

        Panel = new DXControl();
        AddControl(Panel);

        Image = new DXImageControl
        {
            LibraryFile = LibraryFile.MiniMap,
            Movable = true,
            IgnoreMoveBounds = true,
        };
        Image.Moving += (o, e) => ClipMap();
        Panel.AddControl(Image);

        SizeButton = new DXButton
        {
            LibraryFile = LibraryFile.GameInter,
            Index = 132,
            Visible = false,
        };
        SizeButton.MouseClick += (o, e) => ToggleSize();
        AddControl(SizeButton);

        TransparencyButton = new DXButton
        {
            LibraryFile = LibraryFile.GameInter,
            Index = 130,
            Visible = false,
        };
        TransparencyButton.MouseClick += (o, e) => ToggleTransparency();
        AddControl(TransparencyButton);

        BigMapButton = new DXButton
        {
            LibraryFile = LibraryFile.GameInter,
            Index = 137,
            Visible = false,
        };
        BigMapButton.MouseClick += (o, e) => _onBigMapRequest?.Invoke();
        AddControl(BigMapButton);
    }

    public void SetBigMapRequestHandler(Action handler) => _onBigMapRequest = handler;

    // ---- 数据源 (GameScene 调用) ----

    /// <summary>换图: 重建静态标记, 计算缩放, 清空动态标记</summary>
    public void SetMap(MapInfo map, int mapWidth, int mapHeight, uint playerObjectID)
    {
        _mapWidth = mapWidth;
        _mapHeight = mapHeight;
        _mapIndex = map.Index;
        _playerObjectID = playerObjectID;
        _hasPlayer = false;

        Text = map.PlayerDescription;
        Image.Index = map.MiniMap;
        Image.Location = Vector2I.Zero;

        foreach (var m in _objectMarkers.Values)
            m.Dispose();
        _objectMarkers.Clear();

        ScaleX = Image.Size.X / (float)Math.Max(1, mapWidth);
        ScaleY = Image.Size.Y / (float)Math.Max(1, mapHeight);

        foreach (var npc in Globals.NPCInfoList?.Binding ?? Enumerable.Empty<NPCInfo>())
            UpdateStatic(npc);
        foreach (var mv in Globals.MovementInfoList?.Binding ?? Enumerable.Empty<MovementInfo>())
            UpdateStatic(mv);

        QueueRedraw();
    }

    private void UpdateStatic(NPCInfo npc)
    {
        if (npc.Region?.Map?.Index != _mapIndex) return;
        if (npc.Region.PointList == null) npc.Region.CreatePoints(_mapWidth);
        var c = RegionCenter(npc.Region.PointList);
        if (c == null) return;

        var dot = new DXMapInfoControl
        {
            BackColour = Colors.White,
            Size = new Vector2I(3, 3),
        };
        dot.Location = new Vector2I((int)(ScaleX * c.Value.X) - 1, (int)(ScaleY * c.Value.Y) - 1);
        Image.AddControl(dot);
    }

    private void UpdateStatic(MovementInfo mv)
    {
        if (mv.SourceRegion?.Map?.Index != _mapIndex) return;
        if (mv.DestinationRegion?.Map == null || mv.Icon == MapIcon.None) return;
        if (mv.SourceRegion.PointList == null) mv.SourceRegion.CreatePoints(_mapWidth);
        var c = RegionCenter(mv.SourceRegion.PointList);
        if (c == null) return;

        var icon = new DXImageControl { LibraryFile = LibraryFile.MiniMapIcon };
        UpdateMapIcon(icon, mv.Icon);
        icon.Location = new Vector2I(
            (int)(ScaleX * c.Value.X) - (int)icon.Size.X / 2,
            (int)(ScaleY * c.Value.Y) - (int)icon.Size.Y / 2);
        Image.AddControl(icon);
    }

    /// <summary>出口/洞穴图标着色 (移植自原版 GameScene.UpdateMapIcon)</summary>
    public static void UpdateMapIcon(DXImageControl control, MapIcon icon)
    {
        switch (icon)
        {
            case MapIcon.Cave:
                control.Index = 1;
                control.ForeColour = Colors.Red;
                break;
            case MapIcon.Exit:
                control.Index = 1;
                control.ForeColour = Colors.Green;
                break;
            case MapIcon.Down:
                control.Index = 1;
                control.ForeColour = new Color(0.78f, 0.08f, 0.52f); // MediumVioletRed
                break;
            case MapIcon.Up:
                control.Index = 1;
                control.ForeColour = new Color(0.0f, 0.75f, 1.0f); // DeepSkyBlue
                break;
            case MapIcon.Province:
                control.Index = 7;
                break;
            case MapIcon.Building:
                control.Index = 6;
                break;
            default:
                control.Index = (int)icon;
                break;
        }
    }

    private static System.Drawing.Point? RegionCenter(List<System.Drawing.Point> points)
    {
        if (points == null || points.Count == 0) return null;
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var p in points)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }
        return new System.Drawing.Point((minX + maxX) / 2, (minY + maxY) / 2);
    }

    /// <summary>动态物体标记 (怪物/物品/其他玩家)。返回 true 表示是玩家 (用于居中)</summary>
    public bool UpdateObject(uint objectID, int cellX, int cellY, ObjectRenderer.Kind kind)
    {
        if (objectID == _playerObjectID)
        {
            _hasPlayer = true;
            UpdatePlayerMarker(cellX, cellY);
            return true;
        }

        if (!_objectMarkers.TryGetValue(objectID, out var dot))
        {
            dot = new DXMapInfoControl { Size = new Vector2I(3, 3) };
            Image.AddControl(dot);
            _objectMarkers[objectID] = dot;
        }

        dot.Visible = true;
        dot.BackColour = kind switch
        {
            ObjectRenderer.Kind.Monster => Colors.Red,
            ObjectRenderer.Kind.Item => new Color(0.0f, 0.0f, 0.55f), // DarkBlue
            _ => Colors.White,
        };
        dot.Location = new Vector2I(
            (int)(ScaleX * cellX) - (int)dot.Size.X / 2,
            (int)(ScaleY * cellY) - (int)dot.Size.Y / 2);
        return false;
    }

    /// <summary>玩家标记 (移动/换图时由 GameScene 更新)</summary>
    public void UpdatePlayer(int cellX, int cellY)
    {
        _hasPlayer = true;
        UpdatePlayerMarker(cellX, cellY);
    }

    private void UpdatePlayerMarker(int cellX, int cellY)
    {
        if (!_objectMarkers.TryGetValue(_playerObjectID, out var dot))
        {
            dot = new DXMapInfoControl();
            Image.AddControl(dot);
            _objectMarkers[_playerObjectID] = dot;
        }

        dot.Visible = true;
        dot.Hollow = true;
        dot.BackColour = Colors.Lime;
        dot.Size = new Vector2I(5, 5);
        dot.Location = new Vector2I((int)(ScaleX * cellX) - 2, (int)(ScaleY * cellY) - 2);

        // 玩家居中: 地图图平移
        Image.Location = new Vector2I(
            -dot.Location.X + (int)Area.Size.X / 2,
            -dot.Location.Y + (int)Area.Size.Y / 2);
        ClipMap();
    }

    public void RemoveObject(uint objectID)
    {
        if (_objectMarkers.Remove(objectID, out var dot))
            dot.Dispose();
    }

    public void ClearObjects()
    {
        foreach (var dot in _objectMarkers.Values)
            dot.Dispose();
        _objectMarkers.Clear();
        _hasPlayer = false;
    }

    private void ClipMap()
    {
        float imgW = Image.Size.X, imgH = Image.Size.Y;
        float panelW = Panel.Size.X, panelH = Panel.Size.Y;
        float x = Image.Location.X, y = Image.Location.Y;

        if (x + imgW < panelW) x = panelW - imgW;
        if (x > 0) x = 0;
        if (y + imgH < panelH) y = panelH - imgH;
        if (y > 0) y = 0;

        if (imgW < panelW) x = -((imgW - panelW) / 2);
        if (imgH < panelH) y = -((imgH - panelH) / 2);

        Image.Location = new Vector2I((int)x, (int)y);
    }

    private void ToggleSize()
    {
        int right = Location.X + (int)Size.X;
        IsLarge = !IsLarge;
        Size = IsLarge ? LargeMiniMapSize : DefaultMiniMapSize;
        Location = new Vector2I(right - (int)Size.X, Location.Y);
        UpdateButtonLocations();
    }

    private void ToggleTransparency()
    {
        IsTransparent = !IsTransparent;
        Opacity = IsTransparent ? TransparentOpacity : 1F;
    }

    public override void Process()
    {
        base.Process();
        bool hovered = Visible && GetGlobalRect().HasPoint(GetViewport().GetMousePosition());
        SizeButton.Visible = hovered;
        TransparencyButton.Visible = hovered;
        BigMapButton.Visible = hovered;
    }

    private void UpdateButtonLocations()
    {
        if (SizeButton == null || TransparencyButton == null || BigMapButton == null) return;

        const int rightPadding = 3;
        int top = TitleHeight;
        SizeButton.Location = new Vector2I((int)(Size.X - SizeButton.Size.X) - rightPadding, top);
        TransparencyButton.Location = new Vector2I(
            (int)(Size.X - TransparencyButton.Size.X) - rightPadding,
            SizeButton.Location.Y + (int)SizeButton.Size.Y);
        BigMapButton.Location = new Vector2I(
            (int)(Size.X - BigMapButton.Size.X) - rightPadding,
            TransparencyButton.Location.Y + (int)TransparencyButton.Size.Y);
    }
}
