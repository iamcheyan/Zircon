using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.Network;
using Library.SystemModels;
using G = Library.Network.GeneralPackets;
using S = Library.Network.ServerPackets;
using C = Library.Network.ClientPackets;
using ZirconClient.Controls;

namespace ZirconClient.Scripts;

public partial class GameScene : Control
{
    private Network.NetworkManager _net;
    private MapView _mapView;
    private Label _statusLabel;
    private PlayerRenderer _player;

    // M11: UI 窗口层 (与 2D 世界分层) + 窗口管理器
    private CanvasLayer _uiLayer;
    private StatusWindow _statusWindow;
    private double _statusRefreshMs;

    // M12: HUD + 键位
    private MainPanel _mainPanel;
    private MiniMapDialog _miniMap;
    private BigMapDialog _bigMap;
    private BuffDialog _buffDialog;
    private QuestTrackerDialog _questTracker;
    private readonly Dictionary<int, ClientBuffInfo> _buffs = new();
    private Stats _playerStats = new Stats();
    private int _playerLevel;
    private decimal _playerExperience, _playerMaxExperience;
    private int _currentHP, _currentMP, _currentFP;
    private AttackMode _attackMode;
    private PetMode _petMode;

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

        // M11: 窗口层 CanvasLayer, 所有窗口挂这里 (独立于 2D 世界, 永远最顶层)
        _uiLayer = new CanvasLayer();
        _uiLayer.Layer = 10;
        AddChild(_uiLayer);

        _statusWindow = new StatusWindow(); // 初始隐藏, F2 打开
        CreateHud();

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
        _net.Connection.LevelChangedEvent += OnLevelChanged;
        _net.Connection.GainedExperienceEvent += OnGainedExperience;
        _net.Connection.InformMaxExperienceEvent += OnInformMaxExperience;
        _net.Connection.ManaChangedEvent += OnManaChanged;
        _net.Connection.FocusChangedEvent += OnFocusChanged;
        _net.Connection.BuffAddEvent += OnBuffAdd;
        _net.Connection.BuffRemoveEvent += OnBuffRemove;
        _net.Connection.BuffChangedEvent += OnBuffChanged;
        _net.Connection.BuffTimeEvent += OnBuffTime;
        _net.Connection.BuffPausedEvent += OnBuffPaused;
        _net.Connection.AttackModeChangedEvent += OnAttackModeChanged;
        _net.Connection.PetModeChangedEvent += OnPetModeChanged;

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
            _net.Connection.LevelChangedEvent -= OnLevelChanged;
            _net.Connection.GainedExperienceEvent -= OnGainedExperience;
            _net.Connection.InformMaxExperienceEvent -= OnInformMaxExperience;
            _net.Connection.ManaChangedEvent -= OnManaChanged;
            _net.Connection.FocusChangedEvent -= OnFocusChanged;
            _net.Connection.BuffAddEvent -= OnBuffAdd;
            _net.Connection.BuffRemoveEvent -= OnBuffRemove;
            _net.Connection.BuffChangedEvent -= OnBuffChanged;
            _net.Connection.BuffTimeEvent -= OnBuffTime;
            _net.Connection.BuffPausedEvent -= OnBuffPaused;
            _net.Connection.AttackModeChangedEvent -= OnAttackModeChanged;
            _net.Connection.PetModeChangedEvent -= OnPetModeChanged;
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

            InitHudData(_pendingStartInfo);
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

    private void HandleKeyBind(KeyBindAction action)
    {
        switch (action)
        {
            case KeyBindAction.MapMiniWindow:
                _miniMap.Visible = !_miniMap.Visible;
                break;
            case KeyBindAction.MapBigWindow:
                if (_bigMap.Visible) _bigMap.Visible = false;
                else OpenBigMap();
                break;
            case KeyBindAction.QuestTrackerWindow:
                _questTracker.Visible = !_questTracker.Visible;
                break;
            case KeyBindAction.ChangeAttackMode:
                CycleAttackMode();
                break;
            case KeyBindAction.ChangePetMode:
                CyclePetMode();
                break;
            default:
                // MenuWindow/HelpWindow/ConfigWindow/CharacterWindow/InventoryWindow/MagicWindow:
                // 对应对话框在 M9/M13 移植, 先 no-op
                GD.Print($"[Game] 键位 {action} 暂未接入 (后续里程碑)");
                break;
        }
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
        _miniMap?.RemoveObject(objectID);
        _bigMap?.RemoveObject(objectID);
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
        // M6: 通用序列帧特效 (替代 M5 EffectNode 单帧占位)
        // Magic.Zl 爆炸帧 580 起 10 帧, 每帧 100ms, Blend 半透明 (参考原版火球爆炸)
        var fx = new MirEffectNode();
        AddChild(fx);
        fx.Setup(LibraryFile.Magic, 580, 10, 100, null, cellX, cellY, () => ComputeObjectScreenPos(cellX, cellY));
        fx.Blend = true;
    }

    // 血量变化: 受伤扣血并显示血条 (Miss/Block 只播动画不扣)
    private void OnHealthChanged(uint objectID, int change, bool miss, bool block, bool critical)
    {
        if (objectID == _playerObjectID)
        {
            if (_player == null) return;
            if (!miss && !block)
            {
                _player.Health += change;
                _currentHP = _player.Health;
                _mainPanel?.SetHealth(_currentHP);
            }
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
            _currentHP = health;
            _mainPanel?.SetHealth(_currentHP);
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
        _player.MaxMana = maxMana;
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
            _miniMap?.UpdatePlayer(_player.CellX, _player.CellY);
            _bigMap?.UpdatePlayer(_player.CellX, _player.CellY);
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

    // ---- M12 HUD ----

    // 挂载坐标照原版 GameScene (Size = 视口): MainPanel 底中, MiniMap 右上,
    // QuestTracker 小地图下方, Buff 小地图左侧, BigMap 居中
    private void CreateHud()
    {
        var vp = GetViewport().GetVisibleRect().Size;

        _mainPanel = new MainPanel();
        _mainPanel.Location = new Vector2I((int)(vp.X - _mainPanel.Size.X) / 2, (int)(vp.Y - _mainPanel.Size.Y));
        _uiLayer.AddChild(_mainPanel);

        _miniMap = new MiniMapDialog();
        _miniMap.Location = new Vector2I((int)vp.X - 200, 0);
        _uiLayer.AddChild(_miniMap);
        _miniMap.Visible = true; // DXWindow 默认隐藏, HUD 常驻
        _miniMap.SetBigMapRequestHandler(OpenBigMap);

        _questTracker = new QuestTrackerDialog();
        _questTracker.Location = new Vector2I((int)vp.X - 250, 200 + 5);
        _uiLayer.AddChild(_questTracker);
        _questTracker.Visible = true;

        _buffDialog = new BuffDialog();
        _buffDialog.Location = new Vector2I((int)vp.X - 200 - (int)_buffDialog.Size.X - 5, 0);
        _uiLayer.AddChild(_buffDialog);
        _buffDialog.Visible = true;

        _bigMap = new BigMapDialog();
        _bigMap.SetRecenterMapProvider(() => GetMapInfo(_playerMapIndex), OpenBigMapForMap);
        _uiLayer.AddChild(_bigMap);
    }

    private MapInfo GetMapInfo(int mapIndex)
    {
        return Globals.MapInfoList?.Binding.FirstOrDefault(m => m.Index == mapIndex);
    }

    private void OpenBigMap()
    {
        OpenBigMapForMap(GetMapInfo(_playerMapIndex));
    }

    private void OpenBigMapForMap(MapInfo map)
    {
        if (map == null) return;
        var vp = GetViewport().GetVisibleRect().Size;
        _bigMap.Size = new Vector2I(400, 300);
        _bigMap.Location = new Vector2I((int)(vp.X - 400) / 2, (int)(vp.Y - 300) / 2);
        bool isCurrent = map.Index == _playerMapIndex;
        _bigMap.SetMap(map, _mapView.Map?.Width ?? 0, _mapView.Map?.Height ?? 0, _playerObjectID, isCurrent);
        _bigMap.Visible = true;
    }

    // StartGame 数据 -> HUD (等级/职业/属性/血蓝/经验/Buff/任务/攻击宠物模式)
    private void InitHudData(StartInformation info)
    {
        _playerStats = new Stats();
        _playerLevel = info.Level;
        _playerExperience = info.Experience;
        // MaxExperience 由 S.InformMaxExperience / S.LevelChanged 填充 (排空可能已先到)
        _currentHP = info.CurrentHP;
        _currentMP = info.CurrentMP;
        _currentFP = info.CurrentFP;
        _attackMode = info.AttackMode;
        _petMode = info.PetMode;

        _mainPanel?.SetLevel(_playerLevel);
        _mainPanel?.SetClass(info.Class);
        _mainPanel?.SetExperience(_playerExperience, _playerMaxExperience);
        _mainPanel?.SetAttackMode(_attackMode);
        _mainPanel?.SetPetMode(_petMode);
        RefreshPlayerBars();

        _buffs.Clear();
        if (info.Buffs != null)
            foreach (var b in info.Buffs)
                _buffs[b.Index] = b;
        _buffDialog?.BuffsChanged(_buffs);

        _questTracker?.PopulateQuests(info.Quests ?? Enumerable.Empty<ClientUserQuest>());
    }

    // 血/蓝/专注条刷新 (Stats 就绪后与增量变化后调用)
    private void RefreshPlayerBars()
    {
        _mainPanel?.SetHealth(_currentHP);
        _mainPanel?.SetMana(_currentMP);
        _mainPanel?.SetFocus(_currentFP);
    }

    private void OnStatsUpdate(S.StatsUpdate p)
    {
        _playerStats = p.Stats ?? new Stats();
        _mainPanel?.SetStats(_playerStats);
        if (_player == null) return;
        _player.MaxHealth = _playerStats[Stat.Health];
        _player.MaxMana = _playerStats[Stat.Mana];
        if (_player.Health <= 0) _player.Health = _playerStats[Stat.Health];
        RefreshPlayerBars();
    }

    private void OnLevelChanged(S.LevelChanged p)
    {
        _playerLevel = p.Level;
        _playerExperience = p.Experience;
        _playerMaxExperience = p.MaxExperience;
        _mainPanel?.SetLevel(_playerLevel);
        _mainPanel?.SetExperience(_playerExperience, _playerMaxExperience);
    }

    private void OnGainedExperience(decimal amount)
    {
        _playerExperience += amount;
        _mainPanel?.SetExperience(_playerExperience, _playerMaxExperience);
    }

    private void OnInformMaxExperience(decimal maxExperience)
    {
        _playerMaxExperience = maxExperience;
        _mainPanel?.SetExperience(_playerExperience, _playerMaxExperience);
    }

    // 蓝/专注: 照原版按 ObjectID 累加 Change (只有玩家的会到 HUD)
    private void OnManaChanged(uint objectID, int change)
    {
        if (objectID != _playerObjectID) return;
        _currentMP += change;
        RefreshPlayerBars();
    }

    private void OnFocusChanged(uint objectID, int change)
    {
        if (objectID != _playerObjectID) return;
        _currentFP += change;
        RefreshPlayerBars();
    }

    private void OnBuffAdd(S.BuffAdd p)
    {
        if (p.Buff == null) return;
        _buffs[p.Buff.Index] = p.Buff;
        _buffDialog?.BuffsChanged(_buffs);
    }

    private void OnBuffRemove(int index)
    {
        if (_buffs.Remove(index))
            _buffDialog?.BuffsChanged(_buffs);
    }

    private void OnBuffChanged(S.BuffChanged p)
    {
        if (_buffs.TryGetValue(p.Index, out var buff))
            buff.Stats = p.Stats;
        _buffDialog?.BuffsChanged(_buffs);
    }

    private void OnBuffTime(S.BuffTime p)
    {
        if (_buffs.TryGetValue(p.Index, out var buff))
            buff.RemainingTime = p.Time;
        _buffDialog?.BuffsChanged(_buffs);
    }

    private void OnBuffPaused(int index, bool paused)
    {
        if (_buffs.TryGetValue(index, out var buff))
            buff.Pause = paused;
        _buffDialog?.BuffsChanged(_buffs);
    }

    // 攻击/宠物模式循环 (照原版 (Mode+1)%5, 仅本地切 + 发包)
    private void CycleAttackMode()
    {
        _attackMode = (AttackMode)(((int)_attackMode + 1) % 5);
        _mainPanel?.SetAttackMode(_attackMode);
        _net.Connection.Enqueue(new C.ChangeAttackMode { Mode = _attackMode });
    }

    private void CyclePetMode()
    {
        _petMode = (PetMode)(((int)_petMode + 1) % 5);
        _mainPanel?.SetPetMode(_petMode);
        _net.Connection.Enqueue(new C.ChangePetMode { Mode = _petMode });
    }

    // 服务端回显 (权威): 与本地循环一致, 双保险
    private void OnAttackModeChanged(AttackMode mode)
    {
        _attackMode = mode;
        _mainPanel?.SetAttackMode(mode);
    }

    private void OnPetModeChanged(PetMode mode)
    {
        _petMode = mode;
        _mainPanel?.SetPetMode(mode);
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
            OnStatsUpdate(conn.PendingStats.Dequeue());
        while (conn.PendingLevelChanges.Count > 0)
            OnLevelChanged(conn.PendingLevelChanges.Dequeue());
        while (conn.PendingGainedExperience.Count > 0)
            OnGainedExperience(conn.PendingGainedExperience.Dequeue());
        while (conn.PendingMaxExperience.Count > 0)
            OnInformMaxExperience(conn.PendingMaxExperience.Dequeue());
        while (conn.PendingManaChanges.Count > 0)
        {
            var m = conn.PendingManaChanges.Dequeue();
            OnManaChanged(m.ObjectID, m.Change);
        }
        while (conn.PendingFocusChanges.Count > 0)
        {
            var f = conn.PendingFocusChanges.Dequeue();
            OnFocusChanged(f.ObjectID, f.Change);
        }
        while (conn.PendingBuffAdds.Count > 0)
            OnBuffAdd(conn.PendingBuffAdds.Dequeue());
        while (conn.PendingBuffRemoves.Count > 0)
            OnBuffRemove(conn.PendingBuffRemoves.Dequeue());
        while (conn.PendingBuffChangeds.Count > 0)
            OnBuffChanged(conn.PendingBuffChangeds.Dequeue());
        while (conn.PendingBuffTimes.Count > 0)
            OnBuffTime(conn.PendingBuffTimes.Dequeue());
        while (conn.PendingBuffPauseds.Count > 0)
        {
            var (idx, paused) = conn.PendingBuffPauseds.Dequeue();
            OnBuffPaused(idx, paused);
        }
    }

    private void AddObject(ObjectRenderer ob, uint objectID, int zIndex)
    {
        ob.ObjectID = objectID;
        ob.ZIndex = zIndex;
        AddChild(ob);
        _objects[objectID] = ob;
        UpdateObjectPositions();
        GD.Print($"[Game] 添加物体: {ob.Type} '{ob.DisplayName}' ObjectID={objectID} Cell=({ob.CellX},{ob.CellY})");

        // M12: 小/大地图动态标记
        _miniMap?.UpdateObject(objectID, ob.CellX, ob.CellY, ob.Type);
        _bigMap?.UpdateObject(objectID, ob.CellX, ob.CellY, ob.Type);
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

        // M12: 地图玩家标记跟随
        _miniMap?.UpdatePlayer(_player.CellX, _player.CellY);
        _bigMap?.UpdatePlayer(_player.CellX, _player.CellY);
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

        // M12: 小地图/大地图换图 (清动态标记, 重建静态 NPC/出口)
        if (_mapView.Map != null)
        {
            _miniMap?.SetMap(mapInfo, _mapView.Map.Width, _mapView.Map.Height, _playerObjectID);
            _bigMap?.SetMap(mapInfo, _mapView.Map.Width, _mapView.Map.Height, _playerObjectID, isCurrentMap: true);
        }

        // 换图: 清空旧地图的周围物体 (首次进图时 _objects 里是 Drain 的新图对象, 不清)
        if (clearObjects)
        {
            foreach (var ob in _objects.Values)
                ob.QueueFree();
            _objects.Clear();
        }
        else
        {
            // 首次进图: SetMap 已清动态标记, 补回 Drain 阶段加过的
            foreach (var ob in _objects.Values)
            {
                _miniMap?.UpdateObject(ob.ObjectID, ob.CellX, ob.CellY, ob.Type);
                _bigMap?.UpdateObject(ob.ObjectID, ob.CellX, ob.CellY, ob.Type);
            }
        }

        _player.CellX = _playerLocation.X;
        _player.CellY = _playerLocation.Y;
        _player.UpdateAppearance(_pendingStartInfo ?? StartInfo);
        UpdatePlayerPosition();

        // M12: 玩家标记 (小地图居中/大地图复位)
        _miniMap?.UpdatePlayer(_player.CellX, _player.CellY);
        _bigMap?.UpdatePlayer(_player.CellX, _player.CellY);
    }

    public override void _Process(double delta)
    {
        UpdateViewRange();

        // M11: 状态窗口节流刷新 (200ms)
        if (_statusWindow != null && _statusWindow.Visible && _player != null)
        {
            double nowMs = Godot.Time.GetTicksMsec();
            if (nowMs - _statusRefreshMs > 200)
            {
                _statusRefreshMs = nowMs;
                RefreshStatusWindow();
            }
        }

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

    // M11: 填充状态窗口 (玩家真实数据)
    private void RefreshStatusWindow()
    {
        string mapName = Globals.MapInfoList?.Binding.FirstOrDefault(m => m.Index == _playerMapIndex)?.Description ?? $"Map{_playerMapIndex}";
        string className = StartInfo?.Class.ToString() ?? "-";
        _statusWindow.Refresh(
            StartInfo?.Name ?? "-", className,
            _player.Health, _player.MaxHealth, _player.MaxMana,
            _playerLocation.X, _playerLocation.Y, _playerDirection,
            mapName, _objects.Count);
    }

    // 相机锚定玩家后, 计算所有周围物体的屏幕位置 (含移动像素偏移 OffsetX/OffsetY)
    private void UpdateObjectPositions()
    {
        if (_mapView?.Map == null) return;

        const int CellWidth = 48;
        const int CellHeight = 32;
        float offsetX = GetViewport().GetVisibleRect().Size.X / 2 - _mapView.ViewRangeX * CellWidth;
        float offsetY = GetViewport().GetVisibleRect().Size.Y / 2 - _mapView.ViewRangeY * CellHeight;

        foreach (var ob in _objects.Values)
            ob.ComputeScreenPos(_mapView.CenterX, _mapView.CenterY, _mapView.ViewRangeX, _mapView.ViewRangeY, offsetX, offsetY);
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

        // M11: F2 开关状态窗口, Esc 关闭最上层窗口
        if (key.Keycode == Key.F2)
        {
            WindowManager.Toggle(_statusWindow, _uiLayer);
            return;
        }
        if (key.Keycode == Key.Escape)
        {
            if (WindowManager.CloseTop()) return;
        }

        // M12: 键位表分发 (N/H/O/Q/W/E 对应对话框未移植, V/B/L + Ctrl+H/A 生效)
        KeyBindAction bind = KeyBindManager.GetAction(key);
        if (bind != KeyBindAction.None)
        {
            HandleKeyBind(bind);
            return;
        }

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
