using System;
using System.Collections.Generic;
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
    private PlayerRenderer _player;

    // 周围物体 (怪物/NPC/物品): ObjectID -> 渲染节点
    private readonly System.Collections.Generic.Dictionary<uint, ObjectRenderer> _objects = new();

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

    // 移动插值状态
    private System.Drawing.Point _moveFrom;
    private double _moveStartMs;
    private int _moveFrameCount = 1;
    public override void _Ready()
    {
        _net = GetNode<Network.NetworkManager>("/root/NetworkManager");

        _mapView = new MapView();
        AddChild(_mapView);
        UpdateViewRange();

        _player = new PlayerRenderer();
        _player.ZIndex = 50;
        AddChild(_player);

        _statusLabel = new Label();
        _statusLabel.Position = new Vector2(10, 10);
        _statusLabel.Size = new Vector2(500, 80);
        _statusLabel.ZIndex = 100;
        AddChild(_statusLabel);

        _net.Connection.StartGameResultEvent += OnStartGameResult;
        _net.Connection.MapChangedEvent += OnMapChanged;
        _net.Connection.UserLocationEvent += OnUserLocation;
        _net.Connection.ObjectMoveEvent += OnObjectMove;
        _net.Connection.ObjectMonsterEvent += OnObjectMonster;
        _net.Connection.ObjectNPCEvent += OnObjectNPC;
        _net.Connection.ObjectItemEvent += OnObjectItem;
        _net.Connection.ObjectRemoveEvent += OnObjectRemove;
        _net.Connection.ObjectTurnEvent += OnObjectTurn;
        _net.Connection.ObjectAttackEvent += OnObjectAttack;
        _net.Connection.ObjectMagicEvent += OnObjectMagic;
        _net.Connection.HealthChangedEvent += OnHealthChanged;
        _net.Connection.DataObjectHealthManaEvent += OnDataObjectHealthMana;
        _net.Connection.DataObjectMaxHealthManaEvent += OnDataObjectMaxHealthMana;
        _net.Connection.DataObjectMonsterEvent += OnDataObjectMonsterInfo;
        _net.Connection.ObjectDiedEvent += OnObjectDied;
        _net.Connection.ObjectStruckEvent += OnObjectStruck;
        _net.Connection.StatsUpdateEvent += OnStatsUpdate;

        // StartGame 突发包在 _Ready 前已被 Process 处理(订阅未生效), 一次性排空积压队列
        DrainPendingObjects();

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
            _net.Connection.ObjectMoveEvent -= OnObjectMove;
            _net.Connection.ObjectMonsterEvent -= OnObjectMonster;
            _net.Connection.ObjectNPCEvent -= OnObjectNPC;
            _net.Connection.ObjectItemEvent -= OnObjectItem;
            _net.Connection.ObjectRemoveEvent -= OnObjectRemove;
            _net.Connection.ObjectTurnEvent -= OnObjectTurn;
            _net.Connection.ObjectAttackEvent -= OnObjectAttack;
            _net.Connection.ObjectMagicEvent -= OnObjectMagic;
            _net.Connection.HealthChangedEvent -= OnHealthChanged;
            _net.Connection.DataObjectHealthManaEvent -= OnDataObjectHealthMana;
            _net.Connection.DataObjectMaxHealthManaEvent -= OnDataObjectMaxHealthMana;
            _net.Connection.DataObjectMonsterEvent -= OnDataObjectMonsterInfo;
            _net.Connection.ObjectDiedEvent -= OnObjectDied;
            _net.Connection.ObjectStruckEvent -= OnObjectStruck;
            _net.Connection.StatsUpdateEvent -= OnStatsUpdate;
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

            LoadPlayerMap(clearObjects: false);
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

    private void OnObjectMove(uint objectID, MirDirection dir, System.Drawing.Point loc, int distance)
    {
        if (objectID == _playerObjectID)
        {
            _pendingDir = dir;
            _pendingX = loc.X;
            _pendingY = loc.Y;
            CallDeferred(nameof(ShowUserLocation));
            return;
        }

        // 其他玩家/怪物移动 (M4)
        if (_objects.TryGetValue(objectID, out var ob))
            ob.StartMove(loc, dir);
    }

    private void OnObjectMonster(S.ObjectMonster p)
    {
        GD.Print($"[Game] OnObjectMonster: ObjectID={p.ObjectID} MonsterIndex={p.MonsterIndex} Dead={p.Dead}");
        if (_objects.ContainsKey(p.ObjectID)) return; // 已有(重复包)
        var ob = ObjectRenderer.CreateMonster(p);
        if (ob == null) return;
        AddObject(ob, p.ObjectID, zIndex: 40);
    }

    private void OnObjectNPC(S.ObjectNPC p)
    {
        if (_objects.ContainsKey(p.ObjectID)) return;
        var ob = ObjectRenderer.CreateNPC(p);
        if (ob == null) return;
        AddObject(ob, p.ObjectID, zIndex: 40);
    }

    private void OnObjectItem(S.ObjectItem p)
    {
        if (_objects.ContainsKey(p.ObjectID)) return;
        var ob = ObjectRenderer.CreateItem(p);
        if (ob == null) return;
        AddObject(ob, p.ObjectID, zIndex: 30);
    }

    private void OnObjectRemove(uint objectID)
    {
        if (!_objects.Remove(objectID, out var ob)) return;
        ob.QueueFree();
        GD.Print($"[Game] 移除物体: ObjectID={objectID}");
    }

    private void OnObjectTurn(uint objectID, MirDirection dir)
    {
        if (_objects.TryGetValue(objectID, out var ob))
        {
            ob.Direction = dir;
            ob.QueueRedraw();
        }
    }

    // ---- M5 战斗 ----

    // 攻击: 攻击者播攻击动画, 被攻击者播 Struck
    private void OnObjectAttack(uint objectID, MirDirection dir, System.Drawing.Point loc, MagicType magic, uint targetID)
    {
        if (objectID == _playerObjectID)
        {
            if (_player != null) _player.PlayCombat(magic);
        }
        else if (_objects.TryGetValue(objectID, out var ob))
        {
            ob.Direction = dir;
            ob.SetAnimation(MirAnimation.Combat1);
        }

        if (targetID != 0)
        {
            if (targetID == _playerObjectID)
            {
                if (_player != null) _player.PlayStruck();
            }
            else if (_objects.TryGetValue(targetID, out var tgt))
            {
                tgt.SetAnimation(MirAnimation.Struck);
            }
        }
    }

    // 魔法施放: 施法者播攻击动画, 目标位置显示特效帧
    private void OnObjectMagic(uint objectID, MirDirection dir, System.Drawing.Point loc, MagicType type, List<uint> targets, List<System.Drawing.Point> locations, bool cast)
    {
        if (objectID == _playerObjectID)
        {
            if (_player != null) _player.PlayCombat(type);
        }
        else if (_objects.TryGetValue(objectID, out var ob))
        {
            ob.Direction = dir;
            ob.SetAnimation(MirAnimation.Combat1);
        }

        // 目标位置放一个短暂魔法特效 (Magic.Zl 帧, 500ms 自删)
        if (locations.Count > 0)
        {
            foreach (var locPt in locations)
                SpawnEffectAt(locPt.X, locPt.Y);
        }
        else if (targets.Count > 0)
        {
            foreach (uint tid in targets)
            {
                if (_objects.TryGetValue(tid, out var tgt))
                {
                    tgt.SetAnimation(MirAnimation.Struck);
                    SpawnEffectAt(tgt.CellX, tgt.CellY);
                }
            }
        }
    }

    private void SpawnEffectAt(int cellX, int cellY)
    {
        var fx = new EffectNode();
        AddChild(fx);
        fx.Setup(cellX, cellY, () => ComputeObjectScreenPos(cellX, cellY));
    }

    // 血量变化: 受伤扣血并显示血条 (Miss/Block 只播动画不扣)
    private void OnHealthChanged(uint objectID, int change, bool miss, bool block, bool critical)
    {
        if (objectID == _playerObjectID)
        {
            if (_player == null) return;
            if (!miss && !block) _player.Health += change;
            _player.ShowHealthBar = true;
            if (!miss && !block) _player.PlayStruck();
            return;
        }
        if (!_objects.TryGetValue(objectID, out var ob)) return;
        ob.ShowHealthBar = true;
        if (!miss && !block)
        {
            ob.Health += change;
            ob.SetAnimation(MirAnimation.Struck);
        }
    }

    private void OnDataObjectHealthMana(uint objectID, int health, int mana, bool dead)
    {
        if (objectID == _playerObjectID)
        {
            if (_player == null) return;
            _player.Health = health;
            _player.ShowHealthBar = true;
            return;
        }
        if (_objects.TryGetValue(objectID, out var ob))
        {
            ob.Health = health;
            ob.Dead = dead;
            ob.ShowHealthBar = true;
        }
    }

    private void OnDataObjectMaxHealthMana(uint objectID, int maxHealth, int maxMana)
    {
        if (objectID == _playerObjectID)
        {
            if (_player == null) return;
            _player.MaxHealth = maxHealth;
            return;
        }
        if (_objects.TryGetValue(objectID, out var ob))
            ob.MaxHealth = maxHealth;
    }

    // DataObjectMonster: 视野内怪物的权威血量 (进游戏时批量发, 血条数据源)
    private void OnDataObjectMonsterInfo(uint objectID, int health, int maxHealth, int monsterIndex, bool dead)
    {
        if (!_objects.TryGetValue(objectID, out var ob)) return;
        ob.Health = health;
        ob.MaxHealth = maxHealth;
        ob.Dead = dead;
        ob.ShowHealthBar = maxHealth > 0;
    }

    // 玩家属性: MaxHealth/MaxMana 来源
    private void OnStatsUpdate(int maxHealth, int maxMana)
    {
        if (_player == null) return;
        _player.MaxHealth = maxHealth;
        if (_player.Health <= 0) _player.Health = maxHealth;
    }

    // 受击: 被击退到新位置 + 播 Struck 动画
    private void OnObjectStruck(uint objectID, MirDirection dir, System.Drawing.Point loc, uint attackerID, Element element)
    {
        if (objectID == _playerObjectID)
        {
            if (_player == null) return;
            _playerLocation = loc;
            _player.CellX = loc.X;
            _player.CellY = loc.Y;
            _player.Direction = dir;
            _player.PlayStruck();
            UpdatePlayerPosition();
            return;
        }
        if (_objects.TryGetValue(objectID, out var ob))
        {
            ob.CellX = loc.X;
            ob.CellY = loc.Y;
            ob.Direction = dir;
            ob.SetAnimation(MirAnimation.Struck);
            ob.Position = ComputeObjectScreenPos(loc.X, loc.Y);
        }
    }

    // 死亡: 播 Die 动画后延迟移除
    private void OnObjectDied(uint objectID)
    {        if (objectID == _playerObjectID)
        {
            if (_player != null) _player.PlayDie();
            return;
        }
        if (_objects.TryGetValue(objectID, out var ob))
        {
            ob.Dead = true;
            ob.SetAnimation(MirAnimation.Die);
            var renderer = ob;
            GetTree().CreateTimer(1.2).Timeout += () =>
            {
                if (renderer.IsInsideTree() && _objects.Remove(objectID, out _))
                    renderer.QueueFree();
            };
        }
    }

    // 排空 StartGame 突发积压包(顺序与服务器一致: Move/Turn/Monster/NPC/Item/Remove)
    private void DrainPendingObjects()
    {
        var conn = _net.Connection;
        while (conn.PendingMoves.Count > 0)
        {
            var m = conn.PendingMoves.Dequeue();
            OnObjectMove(m.ObjectID, m.Direction, m.Location, m.Distance);
        }
        while (conn.PendingTurns.Count > 0)
        {
            var (id, dir) = conn.PendingTurns.Dequeue();
            OnObjectTurn(id, dir);
        }
        while (conn.PendingMonsters.Count > 0)
            OnObjectMonster(conn.PendingMonsters.Dequeue());
        while (conn.PendingNPCs.Count > 0)
            OnObjectNPC(conn.PendingNPCs.Dequeue());
        while (conn.PendingItems.Count > 0)
            OnObjectItem(conn.PendingItems.Dequeue());
        while (conn.PendingRemoves.Count > 0)
            OnObjectRemove(conn.PendingRemoves.Dequeue());
        while (conn.PendingAttacks.Count > 0)
        {
            var a = conn.PendingAttacks.Dequeue();
            OnObjectAttack(a.ObjectID, a.Direction, a.Location, a.AttackMagic, a.TargetID);
        }
        while (conn.PendingMagics.Count > 0)
        {
            var m = conn.PendingMagics.Dequeue();
            OnObjectMagic(m.ObjectID, m.Direction, m.CurrentLocation, m.Type, m.Targets, m.Locations, m.Cast);
        }
        while (conn.PendingHealthChanges.Count > 0)
        {
            var h = conn.PendingHealthChanges.Dequeue();
            OnHealthChanged(h.ObjectID, h.Change, h.Miss, h.Block, h.Critical);
        }
        while (conn.PendingHealthManas.Count > 0)
        {
            var h = conn.PendingHealthManas.Dequeue();
            OnDataObjectHealthMana(h.ObjectID, h.Health, h.Mana, h.Dead);
        }
        while (conn.PendingMaxHealthManas.Count > 0)
        {
            var h = conn.PendingMaxHealthManas.Dequeue();
            OnDataObjectMaxHealthMana(h.ObjectID, h.MaxHealth, h.MaxMana);
        }
        while (conn.PendingDataMonsters.Count > 0)
        {
            var m = conn.PendingDataMonsters.Dequeue();
            OnDataObjectMonsterInfo(m.ObjectID, m.Health, m.Stats != null ? m.Stats[Stat.Health] : 0, m.MonsterIndex, m.Dead);
        }
        while (conn.PendingDeaths.Count > 0)
            OnObjectDied(conn.PendingDeaths.Dequeue());
        while (conn.PendingStruck.Count > 0)
        {
            var s = conn.PendingStruck.Dequeue();
            OnObjectStruck(s.ObjectID, s.Direction, s.Location, s.AttackerID, s.Element);
        }
        while (conn.PendingStats.Count > 0)
        {
            var st = conn.PendingStats.Dequeue();
            OnStatsUpdate(st.Stats != null ? st.Stats[Stat.Health] : 0, st.Stats != null ? st.Stats[Stat.Mana] : 0);
        }
    }

    private void AddObject(ObjectRenderer ob, uint objectID, int zIndex)
    {        ob.ObjectID = objectID;
        ob.ZIndex = zIndex;
        AddChild(ob);
        _objects[objectID] = ob;
        UpdateObjectPositions();
        GD.Print($"[Game] 添加物体: {ob.Type} '{ob.DisplayName}' ObjectID={objectID} Cell=({ob.CellX},{ob.CellY})");
    }

    private void ShowUserLocation()
    {
        _playerDirection = _pendingDir;

        // 平滑移动: 起点是当前(旧)位置, 终点是服务端新位置
        _moveFrom = _playerLocation;
        _moveStartMs = Godot.Time.GetTicksMsec();
        _moveFrameCount = 6; // Walking 帧数

        _playerLocation = new System.Drawing.Point(_pendingX, _pendingY);
        _player.Direction = _pendingDir;
        _player.SetAnimation(MirAnimation.Walking);

        UpdatePlayerPosition();
        _statusLabel.Text = $"位置: ({_pendingX},{_pendingY}) 方向: {_pendingDir}";
    }

    private void LoadPlayerMap() => LoadPlayerMap(clearObjects: true);

    private void LoadPlayerMap(bool clearObjects)
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

        // 换图: 清空旧地图的周围物体 (首次进图时 _objects 里是 Drain 的新图对象, 不清)
        if (clearObjects)
        {
            foreach (var ob in _objects.Values)
                ob.QueueFree();
            _objects.Clear();
        }

            _player.CellX = _playerLocation.X;
            _player.CellY = _playerLocation.Y;
            _player.UpdateAppearance(_pendingStartInfo ?? StartInfo);
            UpdatePlayerPosition();
    }

    public override void _Process(double delta)
    {
        UpdateViewRange();

        // 移动插值: 在 Walking 帧时长内从起点插到终点
        if (_moveFrameCount > 1 && _player != null)
        {
            const double walkMs = 6 * 100.0; // 6帧 * 100ms
            double elapsed = Godot.Time.GetTicksMsec() - _moveStartMs;
            double t = Math.Clamp(elapsed / walkMs, 0.0, 1.0);

            _player.CellX = (int)Math.Round(_moveFrom.X + (_playerLocation.X - _moveFrom.X) * t);
            _player.CellY = (int)Math.Round(_moveFrom.Y + (_playerLocation.Y - _moveFrom.Y) * t);

            if (t >= 1.0)
            {
                _moveFrameCount = 1;
                _player.SetAnimation(MirAnimation.Standing);
            }
            UpdatePlayerPosition();
        }
        else if (_player != null)
        {
            _player.CellX = _playerLocation.X;
            _player.CellY = _playerLocation.Y;
            UpdatePlayerPosition();
        }
    }

    private void UpdatePlayerPosition()
    {
        if (_mapView?.Map == null) return;

        const int CellWidth = 48;
        const int CellHeight = 32;
        float offsetX = GetViewport().GetVisibleRect().Size.X / 2 - _mapView.ViewRangeX * CellWidth;
        float offsetY = GetViewport().GetVisibleRect().Size.Y / 2 - _mapView.ViewRangeY * CellHeight;

        float px = (_player.CellX - _mapView.CenterX + _mapView.ViewRangeX) * CellWidth + offsetX;
        float py = (_player.CellY - _mapView.CenterY + _mapView.ViewRangeY) * CellHeight + offsetY;

        _player.Position = new Vector2(px, py);
        _mapView.CenterOn(_player.CellX, _player.CellY);
        UpdateObjectPositions();
    }

    // 视野范围随视口尺寸自适应 (窗口模式视口大, 固定 12x15 画不满)
    private void UpdateViewRange()
    {
        if (_mapView == null) return;
        var vp = GetViewport().GetVisibleRect().Size;
        int vrx = (int)Math.Ceiling(vp.X / (2f * 48)) + 1;
        int vry = (int)Math.Ceiling(vp.Y / (2f * 32)) + 1;
        _mapView.ViewRangeX = Math.Max(_mapView.ViewRangeX, vrx);
        _mapView.ViewRangeY = Math.Max(_mapView.ViewRangeY, vry);
    }

    // 相机锚定玩家后, 计算所有周围物体的屏幕位置
    private void UpdateObjectPositions()
    {
        if (_mapView?.Map == null) return;

        foreach (var ob in _objects.Values)
            ob.Position = ComputeObjectScreenPos(ob.CellX, ob.CellY);
    }

    // 格子坐标 -> 屏幕坐标 (与玩家居中公式一致)
    private Vector2 ComputeObjectScreenPos(int cellX, int cellY)
    {
        if (_mapView?.Map == null) return Vector2.Zero;

        const int CellWidth = 48;
        const int CellHeight = 32;
        float offsetX = GetViewport().GetVisibleRect().Size.X / 2 - _mapView.ViewRangeX * CellWidth;
        float offsetY = GetViewport().GetVisibleRect().Size.Y / 2 - _mapView.ViewRangeY * CellHeight;

        return new Vector2(
            (cellX - _mapView.CenterX + _mapView.ViewRangeX) * CellWidth + offsetX,
            (cellY - _mapView.CenterY + _mapView.ViewRangeY) * CellHeight + offsetY);
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
        else if (key.Keycode == Key.F12)
        {
            var img = GetViewport().GetTexture().GetImage();
            img.SavePng("/tmp/game_screenshot.png");
            GD.Print("[Game] 截图保存 /tmp/game_screenshot.png");
        }
    }
}
