using System;
using System.Linq;
using Godot;
using Library;
using Library.Network;
using G = Library.Network.GeneralPackets;
using S = Library.Network.ServerPackets;
using C = Library.Network.ClientPackets;

namespace ZirconClient.Scripts;

public partial class GameScene : Control
{
    private Network.NetworkManager _net;
    private MapView _mapView;
    private Label _statusLabel;
    private Sprite2D _playerSprite;

    // SelectScene 传入的进游戏信息(StartGame 回包在场景创建前已处理完)
    public StartInformation StartInfo { get; set; }

    private uint _playerObjectID;
    private int _playerMapIndex;
    private System.Drawing.Point _playerLocation;
    private MirDirection _playerDirection;

    // CallDeferred 缓冲
    private StartGameResult _pendingStartResult;
    private StartInformation _pendingStartInfo;
    private int _pendingMapIndex;
    private MirDirection _pendingDir;
    private int _pendingX, _pendingY;

    public override void _Ready()
    {
        _net = GetNode<Network.NetworkManager>("/root/NetworkManager");

        _mapView = new MapView();
        AddChild(_mapView);

        _statusLabel = new Label();
        _statusLabel.Position = new Vector2(10, 10);
        _statusLabel.Size = new Vector2(500, 80);
        _statusLabel.ZIndex = 100;
        AddChild(_statusLabel);

        _playerSprite = new Sprite2D();
        var img = Image.CreateEmpty(32, 48, false, Image.Format.Rgba8);
        img.Fill(new Godot.Color(1, 0, 0, 0.8f));
        _playerSprite.Texture = ImageTexture.CreateFromImage(img);
        _playerSprite.ZIndex = 50;
        AddChild(_playerSprite);

        _net.Connection.StartGameResultEvent += OnStartGameResult;
        _net.Connection.MapChangedEvent += OnMapChanged;
        _net.Connection.UserLocationEvent += OnUserLocation;

        if (StartInfo != null)
        {
            // 主线程内直接处理, 无需 CallDeferred
            _pendingStartResult = StartGameResult.Success;
            _pendingStartInfo = StartInfo;
            ShowStartGameResult();
        }
        else
        {
            _statusLabel.Text = "等待进入游戏...";
        }
    }

    public override void _ExitTree()
    {
        if (_net?.Connection != null)
        {
            _net.Connection.StartGameResultEvent -= OnStartGameResult;
            _net.Connection.MapChangedEvent -= OnMapChanged;
            _net.Connection.UserLocationEvent -= OnUserLocation;
        }
    }

    private void OnStartGameResult(StartGameResult result, StartInformation info)
    {
        _pendingStartResult = result;
        _pendingStartInfo = info;
        CallDeferred(nameof(ShowStartGameResult));
    }

    private void ShowStartGameResult()
    {
        if (_pendingStartResult == StartGameResult.Success && _pendingStartInfo != null)
        {
            _playerObjectID = _pendingStartInfo.ObjectID;
            _playerMapIndex = _pendingStartInfo.MapIndex;
            _playerLocation = _pendingStartInfo.Location;
            _playerDirection = _pendingStartInfo.Direction;

            GD.Print($"[Game] 进入游戏! 玩家: {_pendingStartInfo.Name}, 位置: ({_pendingStartInfo.Location.X},{_pendingStartInfo.Location.Y}), 方向: {_pendingStartInfo.Direction}, 地图: {_pendingStartInfo.MapIndex}");
            _statusLabel.Text = $"进入游戏: {_pendingStartInfo.Name}\n位置: ({_pendingStartInfo.Location.X},{_pendingStartInfo.Location.Y}) 方向: {_pendingStartInfo.Direction}";

            LoadPlayerMap();
        }
        else
        {
            _statusLabel.Text = $"进入游戏失败: {_pendingStartResult}";
            GD.Print($"[Game] StartGame 失败: {_pendingStartResult}");
        }
    }

    private void OnMapChanged(int mapIndex, int instanceIndex)
    {
        _pendingMapIndex = mapIndex;
        CallDeferred(nameof(ShowMapChanged));
    }

    private void ShowMapChanged()
    {
        _playerMapIndex = _pendingMapIndex;
        GD.Print($"[Game] 地图切换: MapIndex={_pendingMapIndex}");
        LoadPlayerMap();
    }

    private void OnUserLocation(MirDirection dir, System.Drawing.Point loc)
    {
        _pendingDir = dir;
        _pendingX = loc.X;
        _pendingY = loc.Y;
        CallDeferred(nameof(ShowUserLocation));
    }

    private void ShowUserLocation()
    {
        _playerDirection = _pendingDir;
        _playerLocation = new System.Drawing.Point(_pendingX, _pendingY);
        UpdatePlayerPosition();
        _statusLabel.Text = $"位置: ({_pendingX},{_pendingY}) 方向: {_pendingDir}";
    }

    private void LoadPlayerMap()
    {
        var mapInfo = Globals.MapInfoList?.Binding.FirstOrDefault(m => m.Index == _playerMapIndex);
        if (mapInfo == null)
        {
            GD.PrintErr($"[Game] 找不到地图: MapIndex={_playerMapIndex}");
            _statusLabel.Text = $"找不到地图: MapIndex={_playerMapIndex}";
            return;
        }

        GD.Print($"[Game] 加载地图: MapIndex={_playerMapIndex} -> {mapInfo.FileName} ({mapInfo.Description})");
        _mapView.LoadMap(mapInfo.FileName);
        UpdatePlayerPosition();
    }

    private void UpdatePlayerPosition()
    {
        if (_mapView?.Map == null) return;

        const int CellWidth = 48;
        const int CellHeight = 32;
        float offsetX = GetViewport().GetVisibleRect().Size.X / 2 - _mapView.ViewRangeX * CellWidth;
        float offsetY = GetViewport().GetVisibleRect().Size.Y / 2 - _mapView.ViewRangeY * CellHeight;

        float px = (_playerLocation.X - _mapView.CenterX + _mapView.ViewRangeX) * CellWidth + offsetX;
        float py = (_playerLocation.Y - _mapView.CenterY + _mapView.ViewRangeY) * CellHeight + offsetY;

        _playerSprite.Position = new Vector2(px, py);
        _mapView.CenterOn(_playerLocation.X, _playerLocation.Y);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed) return;
        if (_net?.Connection?.Connected != true) return;

        MirDirection? dir = key.Keycode switch
        {
            Key.Up => MirDirection.Up,
            Key.Down => MirDirection.Down,
            Key.Left => MirDirection.Left,
            Key.Right => MirDirection.Right,
            _ => null,
        };

        if (dir != null)
        {
            _net.Connection.Enqueue(new C.Move { Direction = dir.Value, Distance = 1 });
        }
    }
}