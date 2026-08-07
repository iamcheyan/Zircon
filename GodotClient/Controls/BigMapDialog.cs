using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>
/// 大地图 (移植自 Client/Scenes/Views/BigMapDialog.cs)。
/// 可拖动/缩放适配的 MiniMap 大图; 当前地图时以玩家为中心, Recenter 按钮复位。
/// M12 不移植双击寻路。
/// </summary>
public partial class BigMapDialog : DXWindow
{
    public Rect2 Area;
    public DXImageControl Image;
    public DXControl Panel;
    public DXButton RecenterButton;

    public static float ScaleX, ScaleY;

    private int _mapWidth, _mapHeight;
    private int _mapIndex;
    private bool _isCurrentMap;
    private uint _playerObjectID;
    private int _playerCellX, _playerCellY;

    private readonly Dictionary<uint, DXMapInfoControl> _objectMarkers = new();

    private Func<MapInfo> _recenterMapProvider = () => null;
    private Action<MapInfo> _openBigMap = _ => { };

    public BigMapDialog()
    {
        BackColour = Colors.Black;
        HasFooter = true;

        Panel = new DXControl();
        AddControl(Panel);

        Image = new DXImageControl
        {
            LibraryFile = LibraryFile.MiniMap,
            Movable = true,
            IgnoreMoveBounds = true,
            Clip = true,
        };
        Panel.AddControl(Image);

        RecenterButton = new DXButton
        {
            Type = DXButton.ButtonType.Default,
            Text = "Recenter",
            Size = new Vector2I(80, 24),
        };
        RecenterButton.MouseClick += (o, e) => Recenter();
        AddControl(RecenterButton);
    }

    /// <summary>GameScene 注入: 返回当前地图; 打开指定地图 (Recenter 点击时使用)</summary>
    public void SetRecenterMapProvider(Func<MapInfo> provider, Action<MapInfo> openBigMap)
    {
        _recenterMapProvider = provider;
        _openBigMap = openBigMap;
    }

    public override void _Ready()
    {
        base._Ready();
        Area = new Rect2(0, TitleHeight, Size.X, Size.Y - TitleHeight - FooterHeight);
        Panel.Location = (Vector2I)Area.Position;
        Panel.Size = Area.Size;
        RecenterButton.Location = new Vector2I((int)(Size.X - 30 - 80), (int)(Size.Y - 43));
    }

    public void SetMap(MapInfo map, int mapWidth, int mapHeight, uint playerObjectID, bool isCurrentMap)
    {
        _mapWidth = mapWidth;
        _mapHeight = mapHeight;
        _mapIndex = map.Index;
        _playerObjectID = playerObjectID;
        _isCurrentMap = isCurrentMap;

        Text = map.PlayerDescription;
        Image.Index = map.MiniMap;

        foreach (var m in _objectMarkers.Values)
            m.Dispose();
        _objectMarkers.Clear();

        // 窗口尺寸适配贴图 (320,240)-(800,520)
        var img = Image.Size;
        var client = new Vector2I(
            (int)Math.Clamp(img.X, 320, 800),
            (int)Math.Clamp(img.Y + FooterHeight, 240, 520));
        Size = new Vector2I(client.X, (int)(client.Y + TitleHeight + FooterHeight));

        ScaleX = Image.Size.X / (float)Math.Max(1, mapWidth);
        ScaleY = Image.Size.Y / (float)Math.Max(1, mapHeight);

        bool imageLargerThanPanel = img.X > client.X || img.Y > client.Y;
        Image.Movable = imageLargerThanPanel;
        Image.IgnoreMoveBounds = imageLargerThanPanel;

        RecenterButton.Enabled = !isCurrentMap;

        // 默认居中
        Image.Location = new Vector2I(-(int)(img.X - client.X) / 2, -(int)(img.Y - client.Y) / 2);
        if (isCurrentMap) Recenter();

        foreach (var npc in Globals.NPCInfoList?.Binding ?? Enumerable.Empty<NPCInfo>())
            UpdateStatic(npc);
        foreach (var mv in Globals.MovementInfoList?.Binding ?? Enumerable.Empty<MovementInfo>())
            UpdateStatic(mv);

        QueueRedraw();
    }

    /// <summary>玩家格坐标 (换图/移动时由 GameScene 更新)</summary>
    public void SetPlayerLocation(int cellX, int cellY)
    {
        _playerCellX = cellX;
        _playerCellY = cellY;
        if (_isCurrentMap) Recenter();
    }

    public void UpdateObject(uint objectID, int cellX, int cellY, ObjectRenderer.Kind kind)
    {
        if (objectID == _playerObjectID) { SetPlayerLocation(cellX, cellY); return; }

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
            ObjectRenderer.Kind.Item => new Color(0.0f, 0.0f, 0.55f),
            _ => Colors.White,
        };
        dot.Location = new Vector2I((int)(ScaleX * cellX) - 1, (int)(ScaleY * cellY) - 1);
    }

    /// <summary>玩家标记 (移动/换图时由 GameScene 更新)</summary>
    public void UpdatePlayer(int cellX, int cellY) => SetPlayerLocation(cellX, cellY);

    public void RemoveObject(uint objectID)
    {
        if (_objectMarkers.Remove(objectID, out var dot))
            dot.Dispose();
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
        MiniMapDialog.UpdateMapIcon(icon, mv.Icon);
        icon.Location = new Vector2I(
            (int)(ScaleX * c.Value.X) - (int)icon.Size.X / 2,
            (int)(ScaleY * c.Value.Y) - (int)icon.Size.Y / 2);
        Image.AddControl(icon);
    }

    private void Recenter()
    {
        if (!_isCurrentMap)
        {
            // 原版: Recenter 点击把 SelectedInfo 切回当前地图
            var current = _recenterMapProvider?.Invoke();
            if (current != null)
                _openBigMap(current);
            return;
        }

        var panel = Panel.Size;

        // 玩家像素坐标
        float px = ScaleX * _playerCellX;
        float py = ScaleY * _playerCellY;

        int targetX = (int)Math.Round(panel.X / 2f - px);
        int targetY = (int)Math.Round(panel.Y / 2f - py);

        // 图比面板小时整体居中, 否则钳位到面板范围内
        int minX = Math.Min(0, (int)panel.X - (int)Image.Size.X);
        int maxX = Math.Max(0, (int)panel.X - (int)Image.Size.X);
        int minY = Math.Min(0, (int)panel.Y - (int)Image.Size.Y);
        int maxY = Math.Max(0, (int)panel.Y - (int)Image.Size.Y);

        Image.Location = new Vector2I(
            Math.Max(minX, Math.Min(maxX, targetX)),
            Math.Max(minY, Math.Min(maxY, targetY)));
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
}
