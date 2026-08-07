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
    private const float UiScale = 2f;
    private const float WorldScale = 2f;

    public float DayTime { get; private set; } = 1f;
    public TimeOfDay TimeOfDay { get; private set; } = TimeOfDay.Day;

    private Network.NetworkManager _net;
    private MapView _mapView;
    private Label _statusLabel;
    private PlayerRenderer _player;
    private CombatController _combatController;
    private MapLightLayer _lightLayer;
    private MapWeatherLayer _weatherLayer;
    private MouseWalker _mouseWalker;

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
    private readonly System.Collections.Generic.Dictionary<int, ClientBuffInfo> _buffs = new();
    private MagicBar _magicBar;
    private MagicDialog _magicDialog;
    private Stats _playerStats = new Stats();
    private int _playerLevel;
    private decimal _playerExperience, _playerMaxExperience;
    private int _currentHP, _currentMP, _currentFP;
    private AttackMode _attackMode;
    private PetMode _petMode;

    // ---- M9 物品系统: 数据模型 (数组即底层格, DXItemCell 直读直写) ----
    public static GameScene Game;

    public ClientUserItem[] Inventory = new ClientUserItem[Globals.InventorySize];
    public ClientUserItem[] Equipment = new ClientUserItem[Globals.EquipmentSize];
    public ClientUserItem[] Storage = new ClientUserItem[Globals.StorageSize];
    public ClientUserItem[] PartsStorage = new ClientUserItem[Globals.StorageSize];
    public List<ClientUserCurrency> Currencies = new();
    public ClientBeltLink[] BeltLinks = new ClientBeltLink[Globals.MaxBeltCount];
    public int StorageSize = Globals.StorageSize;
    public int BagWeight, WearWeight, HandWeight;

    public DXItemCell[] InventoryCells = Array.Empty<DXItemCell>();
    public DXItemCell[] EquipmentCells = Array.Empty<DXItemCell>();

    // 物品交互状态
    public double UseItemTime;          // 服务端 S.ItemUseDelay 给的下次可用时间 (绝对 ms)
    private double _pickUpNextMs;       // Tab 拾取节流 (250ms)
    private DXLabel _mouseItemLabel;    // 拿起物品跟随鼠标的悬浮图标
    private DXLabel _hoverLabel;        // 物品悬浮提示
    private ClientUserItem _hoverItem;
    private readonly System.Collections.Generic.Dictionary<uint, MirEffectNode> _itemGlows = new(); // 地面物品稀有度光效

    private InventoryDialog _inventoryDialog;
    private CharacterDialog _characterDialog;
    private StorageDialog _storageDialog;
    private BeltDialog _beltDialog;

    public Stats PlayerStats => _playerStats;

    // 周围物体 (怪物/NPC/物品): ObjectID -> 渲染节点
    private readonly System.Collections.Generic.Dictionary<uint, ObjectRenderer> _objects = new();

    // SelectScene 传入的进游戏信息(StartGame 回包在场景创建前已处理完)
    public StartInformation StartInfo { get; set; }

    private uint _playerObjectID;
    private int _playerMapIndex;
    private System.Drawing.Point _playerLocation;
    private MirDirection _playerDirection;
    private Library.HorseType _playerHorse = Library.HorseType.None;

    // 玩家已学技能: MagicInfo -> ClientUserMagic (S.NewMagic 维护)
    public readonly System.Collections.Generic.Dictionary<MagicInfo, ClientUserMagic> UserMagics = new();
    public int MagicBarSpellSet = 1;  // F1~F8 当前栏组 (1~4, 原版 Ctrl+1~4 切)
    private bool _autoRun;  // D 键切换自动跑步 (原版 AutoRun)

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
        Game = this;

        // 世界坐标使用原版 48x32 逻辑格，最终整体按 2 倍输出。
        // UI CanvasLayer 有独立缩放，不会被这里重复缩放。
        Scale = Vector2.One * WorldScale;

        _net = GetNode<Network.NetworkManager>("/root/NetworkManager");

        _mapView = new MapView();
        AddChild(_mapView);
        _lightLayer = new MapLightLayer { ZIndex = 900 };
        AddChild(_lightLayer);
        _lightLayer.SetObjectSources(GetObjectLightSources);
        _weatherLayer = new MapWeatherLayer { ZIndex = 950 };
        AddChild(_weatherLayer);
        _mouseWalker = new MouseWalker(_mapView, (dir, dist) =>
        {
            if (_net?.Connection?.Connected != true) return;
            _net.Connection.Enqueue(new C.Move { Direction = dir, Distance = dist });
        },
        () => _combatController?.MouseObject != null && _combatController.MouseObject.Type != ObjectRenderer.Kind.Item,
        () =>
        {
            // 原版 Run: 基础1, 负重允许+1, 骑马+1
            int cap = _playerStats[Stat.BagWeight] + _playerStats[Stat.WearWeight];
            int steps = (BagWeight + WearWeight) <= cap ? 2 : 1;
            if (_playerHorse != Library.HorseType.None) steps++;
            return steps;
        });
        AddChild(_mouseWalker);
        _combatController = new CombatController(_mapView,
            () => _objects,
            () => _playerLocation,
            (dir, action, magic) =>
            {
                if (_net?.Connection?.Connected != true) return;
                _net.Connection.Enqueue(new C.Attack { Direction = dir, Action = action, AttackMagic = magic });
            });
        AddChild(_combatController);
        _combatController.ZIndex = 200;  // 高亮框画在物体之上
        UpdateViewRange();

        _player = new PlayerRenderer();
        _player.ZIndex = 100;
        AddChild(_player);

        _statusLabel = new Label();
        _statusLabel.Position = new Vector2(10, 10);
        _statusLabel.Size = new Vector2(500, 80);
        _statusLabel.ZIndex = 100;
        AddChild(_statusLabel);

        // M11: 窗口层 CanvasLayer, 所有窗口挂这里 (独立于 2D 世界, 永远最顶层)
        _uiLayer = new CanvasLayer();
        _uiLayer.Layer = 10;
        _uiLayer.Transform = Transform2D.Identity.Scaled(Vector2.One * UiScale);
        AddChild(_uiLayer);

        _statusWindow = new StatusWindow(); // 初始隐藏, F2 打开
        CreateHud();
        Resized += OnGameResized;
        CallDeferred(nameof(LayoutHud));

        _net.Connection.StartGameResultEvent += OnStartGameResult;
        _net.Connection.MapChangedEvent += OnMapChanged;
        _net.Connection.UserLocationEvent += OnUserLocation;
        _net.Connection.ObjectMoveEvent += OnObjectMove;
        _net.Connection.ObjectMonsterEvent += OnObjectMonster;
        _net.Connection.ObjectNPCEvent += OnObjectNPC;
        _net.Connection.ChatEvent += OnChat;
        _net.Connection.ObjectMagicEvent += OnObjectMagic;
        _net.Connection.NewMagicEvent += OnNewMagic;
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
        _net.Connection.DayTimeChangedEvent += OnDayTimeChanged;
        _net.Connection.TimeOfDayChangedEvent += OnTimeOfDayChanged;
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
        // M9 物品系统
        _net.Connection.ItemsGainedEvent += OnItemsGained;
        _net.Connection.ItemMoveEvent += OnItemMove;
        _net.Connection.ItemSortEvent += OnItemSort;
        _net.Connection.ItemSplitEvent += OnItemSplit;
        _net.Connection.ItemDeleteEvent += OnItemDelete;
        _net.Connection.ItemLockEvent += OnItemLock;
        _net.Connection.ItemUseDelayEvent += OnItemUseDelay;
        _net.Connection.ItemChangedEvent += OnItemChanged;
        _net.Connection.ItemStatsChangedEvent += OnItemStatsChanged;
        _net.Connection.ItemStatsRefreshedEvent += OnItemStatsRefreshed;
        _net.Connection.ItemDurabilityEvent += OnItemDurability;
        _net.Connection.ItemExperienceEvent += OnItemExperience;
        _net.Connection.ItemsChangedEvent += OnItemsChanged;
        _net.Connection.CurrencyChangedEvent += OnCurrencyChanged;
        _net.Connection.WeightUpdateEvent += OnWeightUpdate;
        _net.Connection.StorageSizeEvent += OnStorageSize;

        // M9: 鼠标跟随物品图标 + 悬浮提示 (窗口层最顶)
        _mouseItemLabel = new DXLabel
        {
            TextColour = Colors.White,
            DrawOutline = true,
            OutlineColour = Colors.Black,
            AutoSize = true,
            ZIndex = 500,
            Visible = false,
        };
        _uiLayer.AddChild(_mouseItemLabel);

        _hoverLabel = new DXLabel
        {
            TextColour = new Color(0.9f, 0.9f, 0.5f),
            DrawOutline = true,
            OutlineColour = Colors.Black,
            AutoSize = true,
            ZIndex = 500,
            Visible = false,
        };
        _uiLayer.AddChild(_hoverLabel);

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
            _net.Connection.ChatEvent -= OnChat;
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
            _net.Connection.DayTimeChangedEvent -= OnDayTimeChanged;
            _net.Connection.TimeOfDayChangedEvent -= OnTimeOfDayChanged;
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
            // M9 物品系统
            _net.Connection.ItemsGainedEvent -= OnItemsGained;
            _net.Connection.ItemMoveEvent -= OnItemMove;
            _net.Connection.ItemSortEvent -= OnItemSort;
            _net.Connection.ItemSplitEvent -= OnItemSplit;
            _net.Connection.ItemDeleteEvent -= OnItemDelete;
            _net.Connection.ItemLockEvent -= OnItemLock;
            _net.Connection.ItemUseDelayEvent -= OnItemUseDelay;
            _net.Connection.ItemChangedEvent -= OnItemChanged;
            _net.Connection.ItemStatsChangedEvent -= OnItemStatsChanged;
            _net.Connection.ItemStatsRefreshedEvent -= OnItemStatsRefreshed;
            _net.Connection.ItemDurabilityEvent -= OnItemDurability;
            _net.Connection.ItemExperienceEvent -= OnItemExperience;
            _net.Connection.ItemsChangedEvent -= OnItemsChanged;
            _net.Connection.CurrencyChangedEvent -= OnCurrencyChanged;
            _net.Connection.WeightUpdateEvent -= OnWeightUpdate;
            _net.Connection.StorageSizeEvent -= OnStorageSize;
        }

        if (Game == this) Game = null;
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
            _playerDirection = _pendingStartInfo.Direction;
            _playerHorse = _pendingStartInfo.Horse;
            DayTime = _pendingStartInfo.DayTime;
            TimeOfDay = _pendingStartInfo.TimeOfDay;

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

    private void OnDayTimeChanged(float dayTime)
    {
        DayTime = Math.Clamp(dayTime, 0f, 1f);
        _lightLayer?.SetDayTime(DayTime);
    }

    private void OnTimeOfDayChanged(TimeOfDay timeOfDay, string label)
    {
        TimeOfDay = timeOfDay;
        GD.Print($"[Game] 时间阶段: {timeOfDay} {label}");
        _lightLayer?.QueueRedraw();
    }

    private void ShowMapChanged()
    {
        _playerMapIndex = _pendingMapIndex;
        GD.Print($"[Game] 地图切换: MapIndex={_pendingMapIndex}");
        LoadPlayerMap();
    }

    private void OnUserLocation(MirDirection dir, System.Drawing.Point loc)
    {
        GD.Print($"[Game] USERLOC dir={dir} loc=({loc.X},{loc.Y})");
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
            case KeyBindAction.CharacterWindow:
                WindowManager.Toggle(_characterDialog, _uiLayer);
                break;
            case KeyBindAction.InventoryWindow:
                WindowManager.Toggle(_inventoryDialog, _uiLayer);
                break;
            case KeyBindAction.StorageWindow:
                WindowManager.Toggle(_storageDialog, _uiLayer);
                break;
            case KeyBindAction.BeltWindow:
                WindowManager.Toggle(_beltDialog, _uiLayer);
                break;
            case KeyBindAction.MagicWindow:
                if (_magicDialog is not null)
                {
                    WindowManager.Toggle(_magicDialog, _uiLayer);
                    _magicDialog.Refresh();
                }
                break;
            default:
                // MenuWindow/HelpWindow/ConfigWindow/MagicWindow:
                // 对应对话框在 M13/M14 移植, 先 no-op
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
        SpawnItemGlow(ob, p.Item);
    }

    private void OnChat(S.Chat p)
    {
        if (p == null || string.IsNullOrWhiteSpace(p.Text)) return;
        if (p.ObjectID == _playerObjectID) _player?.SetChat(p.Text);
        else if (_objects.TryGetValue(p.ObjectID, out var ob)) ob.SetChat(p.Text);
    }

    // 稀有度光效 (原版 ItemObject: Common+AddedStats / Superior / Elite)
    private void SpawnItemGlow(ObjectRenderer ob, ClientUserItem item)
    {
        if (item?.Info == null) return;
        var info = item.Info;

        int fxIndex;
        Color colour;
        switch (info.Rarity)
        {
            case Rarity.Superior:
                fxIndex = 100; colour = new Color(0.6f, 1f, 0.6f); break;  // PaleGreen
            case Rarity.Elite:
                fxIndex = 120; colour = new Color(0.72f, 0.6f, 1f); break; // MediumPurple
            default:
                // Common: 带附加属性且非零件才有光效
                if (item.AddedStats?.Count > 0 && info.ItemEffect != ItemEffect.ItemPart)
                {
                    fxIndex = 110; colour = new Color(0.3f, 0.7f, 1f); break; // DeepSkyBlue
                }
                return;
        }

        var fx = new MirEffectNode();
        AddChild(fx);
        fx.Setup(LibraryFile.ProgUse, fxIndex, 10, 100, ob, ob.CellX, ob.CellY, null);
        fx.Loop = true;
        fx.Blend = true;
        fx.BlendRate = 0.5f;
        fx.ZIndex = 25;
        fx.SelfModulate = colour;
        _itemGlows[ob.ObjectID] = fx;
    }

    private void OnObjectRemove(uint objectID)
    {
        if (_itemGlows.Remove(objectID, out var fx)) fx.QueueFree();
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

    // 魔法施放: 施法者播攻击动画, 按 MagicType 查特效表播放站桩/弹道/落地特效
    private void OnObjectMagic(uint objectID, MirDirection dir, System.Drawing.Point loc, MagicType type, List<uint> targets, List<System.Drawing.Point> locations, bool cast)
    {
        // 施法者动画
        if (objectID == _playerObjectID)
        {
            if (_player != null) _player.PlayCombat(type);
        }
        else if (_objects.TryGetValue(objectID, out var ob))
        {
            ob.Direction = dir;
            ob.SetAnimation(MirAnimation.Combat1);
        }

        if (!cast) return;  // 原版: !MagicCast 时不播特效

        var def = MagicEffectTable.Get(type);

        // 收集所有目标格 (locations + targets 的位置)
        var destCells = new List<(int x, int y)>();
        foreach (var lp in locations) destCells.Add((lp.X, lp.Y));
        foreach (uint tid in targets)
            if (_objects.TryGetValue(tid, out var tgt)) destCells.Add((tgt.CellX, tgt.CellY));

        // 目标受击动画
        foreach (uint tid in targets)
            if (_objects.TryGetValue(tid, out var tgt)) tgt.SetAnimation(MirAnimation.Struck);

        // 兜底: 查不到特效定义, 用通用爆炸 (保持原占位行为)
        if (def == null)
        {
            foreach (var (x, y) in destCells) SpawnGenericExplosion(x, y);
            return;
        }

        // 1. 施法者站桩特效 (原版部分魔法在施法者位置播)
        if (def.Projectile == null)
            SpawnCastEffect(def, loc.X, loc.Y);

        // 2. 每个目标格: 弹道 + 落地
        foreach (var (x, y) in destCells)
        {
            if (def.Projectile != null)
            {
                SpawnProjectile(def, loc.X, loc.Y, x, y);
            }
            else if (def.Impact != null)
            {
                SpawnImpact(def.Impact, x, y);
            }
        }
    }

    private void SpawnCastEffect(MagicEffectTable.CastEffect def, int x, int y)
    {
        var fx = new MirEffectNode();
        AddChild(fx);
        fx.Setup(def.File, def.StartIndex, def.FrameCount, def.DelayMs, null, x, y, () => ComputeObjectScreenPos(x, y));
        fx.Blend = def.Blend;
        fx.FrameLight = 10;
        fx.FrameLightColour = def.Colour;
    }

    private void SpawnImpact(MagicEffectTable.ImpactDef imp, int x, int y)
    {
        var fx = new MirEffectNode();
        AddChild(fx);
        fx.Setup(imp.File, imp.StartIndex, imp.FrameCount, imp.DelayMs, null, x, y, () => ComputeObjectScreenPos(x, y));
        fx.Blend = true;
        fx.FrameLight = 10;
        fx.FrameLightColour = imp.Colour;
    }

    private void SpawnProjectile(MagicEffectTable.CastEffect def, int fromX, int fromY, int toX, int toY)
    {
        var proj = def.Projectile;
        var pn = new MirProjectileNode();
        AddChild(pn);
        pn.SetupProjectile(proj.File, proj.StartIndex, proj.FrameCount, proj.DelayMs, null, toX, toY,
            new System.Drawing.Point(fromX, fromY), (cx, cy) => ComputeObjectScreenPos(cx, cy));
        pn.Blend = true;
        pn.FrameLightColour = proj.Colour;
        // 到达后播落地特效
        if (def.Impact != null)
        {
            var impact = def.Impact;
            pn.CompleteAction = () => SpawnImpact(impact, toX, toY);
        }
    }

    private void SpawnGenericExplosion(int cellX, int cellY)
    {
        var fx = new MirEffectNode();
        AddChild(fx);
        fx.Setup(LibraryFile.Magic, 580, 10, 100, null, cellX, cellY, () => ComputeObjectScreenPos(cellX, cellY));
        fx.Blend = true;
        fx.FrameLight = 10;
        fx.FrameLightColour = new Color(1f, 0.62f, 0.25f);
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
            SpawnDamagePopup(_player, change, critical);
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
            SpawnDamagePopup(ob, change, critical);
        }
    }

    private void SpawnDamagePopup(Node2D target, int value, bool critical)
    {
        if (target == null || value == 0) return;
        var popup = new DamagePopupNode { Position = target.Position + new Vector2(0f, -62f) };
        AddChild(popup);
        popup.Setup(value, critical);
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
    private void OnDataObjectMonsterInfo(uint objectID, int health, int maxHealth, int light, int monsterIndex, bool dead)
    {
        if (!_objects.TryGetValue(objectID, out var ob)) return;
        ob.Health = health;
        ob.MaxHealth = maxHealth;
        ob.Light = light;
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
        _mainPanel = new MainPanel();
        _uiLayer.AddChild(_mainPanel);

        _miniMap = new MiniMapDialog();
        _uiLayer.AddChild(_miniMap);
        _miniMap.Visible = true; // DXWindow 默认隐藏, HUD 常驻
        _miniMap.SetBigMapRequestHandler(OpenBigMap);
        _miniMap.LayoutChanged += LayoutHud;

        _questTracker = new QuestTrackerDialog();
        _uiLayer.AddChild(_questTracker);
        _questTracker.Visible = true;

        _buffDialog = new BuffDialog();
        _uiLayer.AddChild(_buffDialog);
        _buffDialog.Visible = true;

        _bigMap = new BigMapDialog();
        _bigMap.SetRecenterMapProvider(() => GetMapInfo(_playerMapIndex), OpenBigMapForMap);
        _uiLayer.AddChild(_bigMap);

        // ---- M9: 背包/角色/仓库/腰带对话框 ----
        _inventoryDialog = new InventoryDialog();
        _uiLayer.AddChild(_inventoryDialog);

        _characterDialog = new CharacterDialog();
        _characterDialog.Location = Vector2I.Zero;
        _uiLayer.AddChild(_characterDialog);

        _storageDialog = new StorageDialog();
        _uiLayer.AddChild(_storageDialog);

        _beltDialog = new BeltDialog();
        _uiLayer.AddChild(_beltDialog);
        _beltDialog.Visible = true; // 腰带常驻 (原版主面板上方)

        _magicBar = new MagicBar(this);
        _uiLayer.AddChild(_magicBar);
        _magicBar.Visible = true;

        _magicDialog = new MagicDialog();
        _uiLayer.AddChild(_magicDialog);

        // 数组注入: 先设 ItemGrid 再 CreateGrid (格子建立时快照 ItemGrid)
        _inventoryDialog.Grid.ItemGrid = Inventory;
        _inventoryDialog.Grid.CreateGrid();
        InventoryCells = _inventoryDialog.Grid.Cells;

        foreach (var cell in _characterDialog.Grid)
            cell.ItemGrid = Equipment;
        EquipmentCells = _characterDialog.Grid;

        _storageDialog.Grid.ItemGrid = Storage;
        _storageDialog.Grid.CreateGrid();
        _storageDialog.RefreshStorage(); // 行数 = StorageSize/10, 重建格 + 滚轮重绑

        BeltLinks = _beltDialog.Links; // 与对话框共享同一数组 (QuickInfo/QuickItem 写回)

        // M9: 主面板功能按钮 -> 对话框开关
        _mainPanel.CharacterButton.MouseClick += (o, e) => WindowManager.Toggle(_characterDialog, _uiLayer);
        _mainPanel.InventoryButton.MouseClick += (o, e) => WindowManager.Toggle(_inventoryDialog, _uiLayer);
        _mainPanel.BeltButton.MouseClick += (o, e) => WindowManager.Toggle(_beltDialog, _uiLayer);

        LayoutHud();
    }

    private void OnGameResized()
    {
        LayoutHud();
        UpdateViewRange();
        UpdatePlayerPosition();
    }

    // 所有常驻 HUD 都基于当前 viewport 重新锚定。不能只在 _Ready 中
    // 计算一次：Linux/Windows 高 DPI 下 Godot 可能在场景创建后才完成
    // 窗口尺寸调整，旧坐标会把底栏留在屏幕中间。
    private void LayoutHud()
    {
        if (_uiLayer == null || !IsInstanceValid(_uiLayer)) return;
        Vector2 vp = GetViewport().GetVisibleRect().Size / UiScale;
        if (vp.X <= 0 || vp.Y <= 0) return;

        if (_mainPanel != null)
            _mainPanel.Location = new Vector2I(
                Math.Max(0, (int)((vp.X - _mainPanel.Size.X) / 2f)),
                Math.Max(0, (int)(vp.Y - _mainPanel.Size.Y)));

        if (_miniMap != null)
            _miniMap.Location = new Vector2I(
                Math.Max(0, (int)(vp.X - _miniMap.Size.X)), 0);

        if (_questTracker != null)
            _questTracker.Location = new Vector2I(
                Math.Max(0, (int)(vp.X - _questTracker.Size.X)),
                (int)_miniMap.Size.Y + 5);

        if (_buffDialog != null)
            _buffDialog.Location = new Vector2I(
                Math.Max(0, (int)(vp.X - _miniMap.Size.X - _buffDialog.Size.X - 5)), 0);

        if (_inventoryDialog != null)
            _inventoryDialog.Location = new Vector2I(
                Math.Max(0, (int)(vp.X - _inventoryDialog.Size.X)),
                (int)_miniMap.Size.Y);

        if (_storageDialog != null)
            _storageDialog.Location = new Vector2I(
                Math.Max(0, (int)(vp.X - _storageDialog.Size.X - _inventoryDialog.Size.X)), 0);

        if (_beltDialog != null)
            _beltDialog.Location = new Vector2I(
                (int)(_mainPanel.Location.X + _mainPanel.Size.X - _beltDialog.Size.X),
                Math.Max(0, (int)(_mainPanel.Location.Y - _beltDialog.Size.Y)));

        if (_magicBar != null)
            _magicBar.Position = new Vector2(
                Math.Max(0, (int)(_mainPanel.Location.X - _magicBar.Size.X - 5)),
                Math.Max(0, (int)(vp.Y - _mainPanel.Size.Y - _magicBar.Size.Y - 5)));
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
        var vp = GetViewport().GetVisibleRect().Size / UiScale;
        bool isCurrent = map.Index == _playerMapIndex;
        _bigMap.SetMap(map, _mapView.Map?.Width ?? 0, _mapView.Map?.Height ?? 0, _playerObjectID, isCurrent);
        _bigMap.Location = new Vector2I(
            Math.Max(0, (int)((vp.X - _bigMap.Size.X) / 2f)),
            Math.Max(0, (int)((vp.Y - _bigMap.Size.Y) / 2f)));
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

        // ---- M9: 物品数据 (StartInformation + 登录仓库包) ----
        FillItems(info.Items);
        ApplyBeltLinks(info.BeltLinks);

        // 已学技能 (StartInformation.Magics 一次性下发, S.NewMagic 只在学新技能时发)
        UserMagics.Clear();
        if (info.Magics != null)
        {
            foreach (var m in info.Magics)
            {
                if (m?.Info == null)
                    m?.Complete();  // 反序列化时 Info 未绑定, 手动补
                if (m?.Info != null)
                    UserMagics[m.Info] = m;
            }
            GD.Print($"[Magic] 加载已有技能 {UserMagics.Count} 个");
        }
        _magicBar?.Refresh();
        _magicDialog?.Refresh();
        Currencies.Clear();
        if (info.Currencies != null) Currencies.AddRange(info.Currencies);
        RefreshCurrency();

        if (info.StorageSize > 0) StorageSize = info.StorageSize;
        FillStorage(_net.Connection.PendingStorageItems);
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
        _player.Light = _playerStats[Stat.Light];
        if (_player.Health <= 0) _player.Health = _playerStats[Stat.Health];
        RefreshPlayerBars();
    }

    private IEnumerable<MapLightLayer.LightSource> GetObjectLightSources()
    {
        if (_player != null && _player.Light > 0)
            yield return new MapLightLayer.LightSource(_player.Position, _player.Light,
                new Color(1f, 0.86f, 0.55f));
        foreach (var ob in _objects.Values)
            if (ob.Light > 0)
                yield return new MapLightLayer.LightSource(ob.Position, ob.Light,
                    new Color(1f, 0.86f, 0.55f));
        foreach (Node child in GetChildren())
            if (child is MirEffectNode fx && fx.FrameLight > 0)
                yield return new MapLightLayer.LightSource(fx.Position, fx.FrameLight, fx.FrameLightColour);
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

    // ==================== M9 物品系统 ====================

    private ClientUserItem[] GetGrid(GridType type)
    {
        switch (type)
        {
            case GridType.Inventory: return Inventory;
            case GridType.Equipment: return Equipment;
            case GridType.Storage: return Storage;
            case GridType.PartsStorage: return PartsStorage;
            default: return null;
        }
    }

    private ClientUserItem ItemAt(GridType type, int slot)
    {
        var arr = GetGrid(type);
        if (arr == null || slot < 0 || slot >= arr.Length) return null;
        return arr[slot];
    }

    // 解锁源/目标格 (服务端回包后解除 Locked, 允许后续操作)
    private void UnlockCell(GridType type, int slot)
    {
        DXItemCell[] cells = type switch
        {
            GridType.Inventory => InventoryCells,
            GridType.Equipment => EquipmentCells,
            GridType.Belt => _beltDialog?.Grid?.Cells,
            GridType.Storage => _storageDialog?.Grid?.Cells,
            _ => null,
        };
        if (cells != null && slot >= 0 && slot < cells.Length && cells[slot] != null)
            cells[slot].Locked = false;
    }

    // 批量变更后刷新所有可见格
    private void RefreshItemGrids()
    {
        foreach (var c in InventoryCells) c?.RefreshItem();
        foreach (var c in EquipmentCells) c?.RefreshItem();
        _beltDialog?.Grid?.RefreshGrid();
        _storageDialog?.Grid?.RefreshGrid();
    }

    // 背包权重重算 (服务端未发 WeightUpdate 时的本地兜底)
    public void RefreshInventoryWeights()
    {
        int bag = 0;
        foreach (var item in Inventory)
            if (item != null) bag += item.Weight;
        BagWeight = bag;
        _inventoryDialog?.SetWeight(bag);
    }

    public void RefreshCurrency()
    {
        long gold = Currencies.FirstOrDefault(x => x.Info?.Type == CurrencyType.Gold)?.Amount ?? 0;
        long gg = Currencies.FirstOrDefault(x => x.Info?.Type == CurrencyType.GameGold)?.Amount ?? 0;
        _inventoryDialog?.SetCurrency(gold, gg);

        // 主面板 FP/CP (原版 CurrencyChanged 语义)
        if (_mainPanel != null)
        {
            _mainPanel.FPLabel.Text = Currencies.FirstOrDefault(x => x.Info?.Type == CurrencyType.FP)?.Amount.ToString() ?? "0";
            _mainPanel.CPLabel.Text = Currencies.FirstOrDefault(x => x.Info?.Type == CurrencyType.CP)?.Amount.ToString() ?? "0";
        }
    }

    // 拿起物品跟随鼠标 + 悬浮提示
    private void UpdateMouseItem()
    {
        var dragged = DXItemCell.SelectedCell?.Item;
        if (dragged != null)
        {
            _mouseItemLabel.Visible = true;
            _mouseItemLabel.Text = $"{dragged.Info?.ItemName}" + (dragged.Count > 1 ? $" x{dragged.Count}" : "");
        }
        else
        {
            _mouseItemLabel.Visible = false;
        }

        if (_hoverItem != null && dragged == null)
        {
            _hoverLabel.Visible = true;
            _hoverLabel.Text = _hoverItem.Info?.ItemName ?? "";
        }
        else
        {
            _hoverLabel.Visible = false;
        }

        if (_mouseItemLabel.Visible || _hoverLabel.Visible)
        {
            var p = GetGlobalMousePosition() / UiScale;
            _mouseItemLabel.Position = new Vector2(p.X + 14, p.Y + 10);
            _hoverLabel.Position = new Vector2(p.X + 14, p.Y + 10);
        }
    }

    public void SetHoverItem(ClientUserItem item)
    {
        _hoverItem = item;
    }

    // ---- 发包转发 (DXItemCell/对话框调用) ----
    public void SendItemMove(GridType fromGrid, GridType toGrid, int fromSlot, int toSlot, bool mergeItem)
        => _net.Connection.SendItemMove(fromGrid, toGrid, fromSlot, toSlot, mergeItem);

    public void SendItemUse(GridType grid, int slot)
        => _net.Connection.SendItemUse(grid, slot);

    public void SendItemLock(GridType grid, int slot, bool locked)
        => _net.Connection.SendItemLock(grid, slot, locked);

    public void SendItemSort(GridType grid)
        => _net.Connection.SendItemSort(grid);

    public void SendItemDelete(GridType grid, int slot)
        => _net.Connection.SendItemDelete(grid, slot);

    public void SendBeltLinkChanged(int slot, int linkInfoIndex, int linkItemIndex)
        => _net.Connection.SendBeltLinkChanged(slot, linkInfoIndex, linkItemIndex);

    public void SendPickUp()
        => _net.Connection.SendPickUp();

    // ---- Tab 拾取 (250ms 节流) ----
    private void PickUpItems()
    {
        double now = Godot.Time.GetTicksMsec();
        if (now < _pickUpNextMs) return;
        _pickUpNextMs = now + 250;
        _net.Connection.SendPickUp();
    }

    // ---- 使用冷却 ----
    public bool IsUseItemOnCooldown(ClientUserItem item) => Godot.Time.GetTicksMsec() < UseItemTime;

    public void SetUseItemCooldown(double ms)
    {
        double until = Godot.Time.GetTicksMsec() + ms;
        if (until > UseItemTime) UseItemTime = until;
    }

    // ---- 数据填充 ----
    public void FillItems(List<ClientUserItem> items)
    {
        if (items == null) return;
        foreach (var item in items)
        {
            if (item.Slot >= Globals.EquipmentOffSet)
            {
                int slot = item.Slot - Globals.EquipmentOffSet;
                if (slot >= 0 && slot < Equipment.Length) Equipment[slot] = item;
            }
            else if (item.Slot >= 0 && item.Slot < Inventory.Length)
            {
                Inventory[item.Slot] = item;
            }
        }
        RefreshItemGrids();
    }

    public void FillStorage(List<ClientUserItem> items)
    {
        if (items == null) return;
        Array.Clear(Storage, 0, Storage.Length);
        Array.Clear(PartsStorage, 0, PartsStorage.Length);
        foreach (var item in items)
        {
            if (item.Slot >= Globals.PartsStorageOffset)
            {
                int slot = item.Slot - Globals.PartsStorageOffset;
                if (slot < PartsStorage.Length) PartsStorage[slot] = item;
            }
            else if (item.Slot >= 0 && item.Slot < Storage.Length)
            {
                Storage[item.Slot] = item;
            }
        }
        _storageDialog?.RefreshStorage();
    }

    public void ApplyBeltLinks(List<ClientBeltLink> links)
    {
        for (int i = 0; i < BeltLinks.Length; i++)
        {
            BeltLinks[i].Slot = i;
            BeltLinks[i].LinkInfoIndex = -1;
            BeltLinks[i].LinkItemIndex = -1;
        }
        if (links != null)
        {
            foreach (var link in links)
            {
                if (link.Slot < 0 || link.Slot >= BeltLinks.Length) continue;
                BeltLinks[link.Slot].LinkInfoIndex = link.LinkInfoIndex;
                BeltLinks[link.Slot].LinkItemIndex = link.LinkItemIndex;
            }
        }
        _beltDialog?.UpdateLinks();
    }

    // 物品离身/变更: 遍历腰带清失效链接 (原版 ItemChanged 的 ShouldLinkInfo 分支)
    private void ClearBeltLinkItem(int itemIndex, ClientUserItem replaceItem)
    {
        for (int i = 0; i < BeltLinks.Length; i++)
        {
            var link = BeltLinks[i];
            if (link.LinkItemIndex != itemIndex) continue;

            var cell = _beltDialog?.Grid?.Cells != null && i < _beltDialog.Grid.Cells.Length ? _beltDialog.Grid.Cells[i] : null;
            if (cell != null) cell.QuickItem = null; // setter 同步 link.LinkItemIndex = -1

            if (replaceItem != null && !replaceItem.Info.ShouldLinkInfo)
            {
                link.LinkItemIndex = replaceItem.Index;
                if (cell != null) cell.QuickItem = replaceItem;
            }
            SendBeltLinkChanged(link.Slot, link.LinkInfoIndex, link.LinkItemIndex);
        }
    }

    // ---- 16 个 S 包处理器 ----

    // 拾取获得/系统发放
    private void OnItemsGained(S.ItemsGained p)
    {
        AddItems(p.Items ?? new List<ClientUserItem>());
    }

    public void AddItems(List<ClientUserItem> items)
    {
        foreach (var item in items)
        {
            if (item.Info?.ItemEffect == ItemEffect.Experience) continue;
            if ((item.Flags & UserItemFlags.QuestItem) == UserItemFlags.QuestItem) continue;

            var currency = Currencies.FirstOrDefault(x => x.Info?.DropItem == item.Info);
            if (currency != null)
            {
                currency.Amount += item.Count;
                RefreshCurrency();
                continue;
            }

            bool handled = false;
            if (item.Info.StackSize > 1 && (item.Flags & UserItemFlags.Expirable) != UserItemFlags.Expirable)
            {
                foreach (var cellItem in Inventory)
                {
                    if (cellItem == null || cellItem.Info != item.Info) continue;
                    if (cellItem.Count >= cellItem.Info.StackSize) continue;
                    if ((cellItem.Flags & UserItemFlags.Expirable) == UserItemFlags.Expirable) continue;
                    if ((cellItem.Flags & UserItemFlags.Bound) != (item.Flags & UserItemFlags.Bound)) continue;
                    if ((cellItem.Flags & UserItemFlags.Worthless) != (item.Flags & UserItemFlags.Worthless)) continue;
                    if ((cellItem.Flags & UserItemFlags.NonRefinable) != (item.Flags & UserItemFlags.NonRefinable)) continue;
                    if (!cellItem.AddedStats.Compare(item.AddedStats)) continue;

                    if (cellItem.Count + item.Count <= item.Info.StackSize)
                    {
                        cellItem.Count += item.Count;
                        handled = true;
                        break;
                    }
                    item.Count -= item.Info.StackSize - cellItem.Count;
                    cellItem.Count = item.Info.StackSize;
                }
                if (handled) continue;
            }

            for (int i = 0; i < Inventory.Length; i++)
            {
                if (Inventory[i] != null) continue;
                Inventory[i] = item;
                item.Slot = i;
                break;
            }
        }
        RefreshItemGrids();
    }

    // 物品移动 (服务端权威, 直接执行; 原版无双向确认)
    private void OnItemMove(S.ItemMove p)
    {
        var fromArr = GetGrid(p.FromGrid);
        var toArr = GetGrid(p.ToGrid);
        if (fromArr == null || toArr == null) return;
        if (p.FromSlot < 0 || p.FromSlot >= fromArr.Length) return;
        if (p.ToSlot < 0 || p.ToSlot >= toArr.Length) return;

        UnlockCell(p.FromGrid, p.FromSlot);
        UnlockCell(p.ToGrid, p.ToSlot);

        var fromItem = fromArr[p.FromSlot];
        var toItem = toArr[p.ToSlot];

        // 背包<->装备移动: 清理旧物品的腰带链接
        if (p.FromGrid != p.ToGrid && p.Success)
        {
            if (p.FromGrid == GridType.Inventory && fromItem != null && !fromItem.Info.ShouldLinkInfo)
                ClearBeltLinkItem(fromItem.Index, p.ToGrid == GridType.Equipment ? toItem : null);
            else if (p.ToGrid == GridType.Inventory && toItem != null && !toItem.Info.ShouldLinkInfo)
                ClearBeltLinkItem(toItem.Index, null);
        }

        if (!p.Success) return;

        if (p.MergeItem)
        {
            if (fromItem == null || toItem == null) return;
            if (toItem.Count + fromItem.Count <= toItem.Info.StackSize)
            {
                toItem.Count += fromItem.Count;
                fromArr[p.FromSlot] = null;
            }
            else
            {
                fromItem.Count -= toItem.Info.StackSize - toItem.Count;
                toItem.Count = toItem.Info.StackSize;
            }
        }
        else
        {
            toArr[p.ToSlot] = fromItem;
            fromArr[p.FromSlot] = toItem;
            if (fromItem != null) fromItem.Slot = p.ToSlot;
            if (toItem != null) toItem.Slot = p.FromSlot;
        }

        RefreshItemGrids();
    }

    // 整理 (清空重排)
    private void OnItemSort(S.ItemSort p)
    {
        var arr = GetGrid(p.Grid);
        if (arr == null) return;

        for (int i = 0; i < arr.Length; i++) arr[i] = null;

        if (p.Success && p.Items != null)
        {
            foreach (var item in p.Items)
            {
                int slot = item.Slot;
                if (p.Grid == GridType.PartsStorage) slot -= Globals.PartsStorageOffset;
                if (slot < 0 || slot >= arr.Length) continue;
                arr[slot] = item;
            }
        }
        RefreshItemGrids();
    }

    // 拆分
    private void OnItemSplit(S.ItemSplit p)
    {
        var arr = GetGrid(p.Grid);
        if (arr == null) return;
        if (p.Slot < 0 || p.Slot >= arr.Length) return;
        UnlockCell(p.Grid, p.Slot);
        if (!p.Success) return;

        var fromItem = arr[p.Slot];
        if (fromItem == null) return;
        if (p.NewSlot < 0 || p.NewSlot >= arr.Length) return;

        arr[p.NewSlot] = new ClientUserItem(fromItem, p.Count) { Slot = p.NewSlot };
        if (p.Count >= fromItem.Count) arr[p.Slot] = null;
        else fromItem.Count -= p.Count;

        RefreshItemGrids();
    }

    // 删除 (丢弃/移除)
    private void OnItemDelete(S.ItemDelete p)
    {
        var arr = GetGrid(p.Grid);
        if (arr == null) return;
        if (p.Slot < 0 || p.Slot >= arr.Length) return;
        UnlockCell(p.Grid, p.Slot);
        DXItemCell.SelectedCell = null;
        if (!p.Success) return;

        var item = arr[p.Slot];
        if (item == null) return;

        if (!item.Info.ShouldLinkInfo)
            ClearBeltLinkItem(item.Index, null);

        arr[p.Slot] = null;
        RefreshItemGrids();
    }

    // 锁定
    private void OnItemLock(S.ItemLock p)
    {
        var item = ItemAt(p.Grid, p.Slot);
        if (item == null) return;
        if (p.Locked) item.Flags |= UserItemFlags.Locked;
        else item.Flags &= ~UserItemFlags.Locked;
        RefreshItemGrids();
    }

    // 使用延迟: 服务端给的绝对冷却 (下次可用时间)
    private void OnItemUseDelay(S.ItemUseDelay p)
    {
        UseItemTime = Godot.Time.GetTicksMsec() + p.Delay.TotalMilliseconds;
    }

    // 数量变更 (使用消耗/拆分结果)
    private void OnItemChanged(S.ItemChanged p)
    {
        var arr = GetGrid(p.Link.GridType);
        if (arr == null) return;
        if (p.Link.Slot < 0 || p.Link.Slot >= arr.Length) return;
        UnlockCell(p.Link.GridType, p.Link.Slot);
        if (!p.Success) return;

        var item = arr[p.Link.Slot];
        if (item == null) return;

        if (!item.Info.ShouldLinkInfo)
            ClearBeltLinkItem(item.Index, null);

        if (p.Link.Count == 0) arr[p.Link.Slot] = null;
        else item.Count = p.Link.Count;

        RefreshItemGrids();
    }

    // 属性变更 (附魔等)
    private void OnItemStatsChanged(S.ItemStatsChanged p)
    {
        var item = ItemAt(p.GridType, p.Slot);
        if (item == null) return;
        item.AddedStats.Add(p.NewStats);
        GD.Print($"[Item] 属性变更: {item.Info?.ItemName} +{p.NewStats.Count} 条");
        RefreshItemGrids();
    }

    private void OnItemStatsRefreshed(S.ItemStatsRefreshed p)
    {
        var item = ItemAt(p.GridType, p.Slot);
        if (item == null) return;
        item.AddedStats = p.NewStats;
        RefreshItemGrids();
    }

    // 耐久变化: 归零提示
    private void OnItemDurability(S.ItemDurability p)
    {
        var item = ItemAt(p.GridType, p.Slot);
        if (item == null) return;
        item.CurrentDurability = p.CurrentDurability;
        if (p.CurrentDurability == 0)
            GD.Print($"[Item] {item.Info?.ItemName} 耐久已耗尽");
        RefreshItemGrids();
    }

    // 武器熟练度经验
    private void OnItemExperience(S.ItemExperience p)
    {
        var item = ItemAt(p.Target.GridType, p.Target.Slot);
        if (item == null) return;
        item.Experience = p.Experience;
        item.Level = p.Level;
        if (p.Level > 0) item.Flags |= UserItemFlags.Bound;
        RefreshItemGrids();
    }

    // 批量变更 (仓库/交易等; Count == 当前 Count 表示整格移除)
    private void OnItemsChanged(S.ItemsChanged p)
    {
        if (p.Links == null)
        {
            DXItemCell.SelectedCell = null;
            return;
        }

        foreach (var link in p.Links)
        {
            var arr = GetGrid(link.GridType);
            if (arr == null) continue;
            if (link.Slot < 0 || link.Slot >= arr.Length) continue;
            UnlockCell(link.GridType, link.Slot);

            var item = arr[link.Slot];
            if (item == null || !p.Success) continue;

            if (!item.Info.ShouldLinkInfo)
                ClearBeltLinkItem(item.Index, null);

            if (link.Count == item.Count) arr[link.Slot] = null;
            else item.Count -= link.Count;
        }
        DXItemCell.SelectedCell = null;
        RefreshItemGrids();
    }

    // 货币变更
    private void OnCurrencyChanged(int currencyIndex, long amount)
    {
        var currency = Currencies.FirstOrDefault(x => x.CurrencyIndex == currencyIndex);
        if (currency != null) currency.Amount = amount;
        RefreshCurrency();
    }

    // 负重变更
    private void OnWeightUpdate(int bag, int wear, int hand)
    {
        BagWeight = bag;
        WearWeight = wear;
        HandWeight = hand;
        _inventoryDialog?.SetWeight(bag);
        _characterDialog?.SetWeight(wear, hand);
    }

    // 仓库容量变更
    private void OnStorageSize(int size)
    {
        StorageSize = size;
        _storageDialog?.RefreshStorage();
    }

    // ---- 使用/穿戴校验 (移植自原版 CanUseItem/CanWearItem) ----
    public bool CanUseItem(ClientUserItem item)
    {
        if (item?.Info == null) return false;

        if (item.Info.RequiredGender != RequiredGender.None &&
            item.Info.RequiredGender != (RequiredGender)StartInfo?.Gender)
            return false;

        if (item.Info.RequiredClass != RequiredClass.None &&
            item.Info.RequiredClass != (RequiredClass)StartInfo?.Class)
            return false;

        switch (item.Info.RequiredType)
        {
            case RequiredType.Level:
                if (_playerLevel < item.Info.RequiredAmount && _playerStats[Stat.Rebirth] == 0) return false;
                break;
            case RequiredType.MaxLevel:
                if (_playerLevel > item.Info.RequiredAmount && _playerStats[Stat.Rebirth] == 0) return false;
                break;
        }

        // 负重检查 (原版 User.CheckWeight)
        int weight = BagWeight + WearWeight + item.Weight;
        if (weight > _playerStats[Stat.BagWeight] + _playerStats[Stat.WearWeight])
        {
            GD.Print($"[Item] 负重不足, 无法使用 {item.Info.ItemName}");
            return false;
        }

        // Book 类: 魔法系统 (M13) 未移植, 客户端直接拦
        if (item.Info.ItemType == ItemType.Book) return false;

        return true;
    }

    public bool CanWearItem(ClientUserItem item, EquipmentSlot slot)
    {
        if (item?.Info == null) return false;

        // 类型与槽位匹配 (原版 CorrectSlot 校验, 客户端先行)
        if (!Functions.CorrectSlot(item.Info.ItemType, slot))
        {
            GD.Print($"[Item] {item.Info.ItemName} 不能穿戴到 {slot}");
            return false;
        }

        if (item.Info.RequiredGender != RequiredGender.None &&
            item.Info.RequiredGender != (RequiredGender)StartInfo?.Gender)
            return false;

        if (item.Info.RequiredClass != RequiredClass.None &&
            item.Info.RequiredClass != (RequiredClass)StartInfo?.Class)
            return false;

        switch (item.Info.RequiredType)
        {
            case RequiredType.Level:
                if (_playerLevel < item.Info.RequiredAmount && _playerStats[Stat.Rebirth] == 0) return false;
                break;
            case RequiredType.MaxLevel:
                if (_playerLevel > item.Info.RequiredAmount && _playerStats[Stat.Rebirth] == 0) return false;
                break;
        }

        // 负重: 手持槽 (Weapon/Torch/Shield) 查 HandWeight, 其余查 WearWeight; 卸下旧装备减重
        ClientUserItem old = Equipment[(int)slot];
        int weight = item.Weight - (old?.Weight ?? 0);
        if (slot == EquipmentSlot.Weapon || slot == EquipmentSlot.Torch || slot == EquipmentSlot.Shield)
        {
            if (HandWeight + weight > _playerStats[Stat.HandWeight])
            {
                GD.Print($"[Item] 手持负重不足, 无法穿戴 {item.Info.ItemName}");
                return false;
            }
        }
        else if (WearWeight + weight > _playerStats[Stat.WearWeight])
        {
            GD.Print($"[Item] 穿戴负重不足, 无法穿戴 {item.Info.ItemName}");
            return false;
        }

        return true;
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
        while (conn.PendingChats.Count > 0)
            OnChat(conn.PendingChats.Dequeue());
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
            OnDataObjectMonsterInfo(m.ObjectID, m.Health, m.Stats != null ? m.Stats[Stat.Health] : 0,
                m.Stats != null ? m.Stats[Stat.Light] : 0, m.MonsterIndex, m.Dead);
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
        // M9 物品系统
        while (conn.PendingItemsGained.Count > 0)
            OnItemsGained(conn.PendingItemsGained.Dequeue());
        while (conn.PendingItemMoves.Count > 0)
            OnItemMove(conn.PendingItemMoves.Dequeue());
        while (conn.PendingItemSorts.Count > 0)
            OnItemSort(conn.PendingItemSorts.Dequeue());
        while (conn.PendingItemSplits.Count > 0)
            OnItemSplit(conn.PendingItemSplits.Dequeue());
        while (conn.PendingItemDeletes.Count > 0)
            OnItemDelete(conn.PendingItemDeletes.Dequeue());
        while (conn.PendingItemLocks.Count > 0)
            OnItemLock(conn.PendingItemLocks.Dequeue());
        while (conn.PendingItemUseDelays.Count > 0)
            OnItemUseDelay(conn.PendingItemUseDelays.Dequeue());
        while (conn.PendingItemChangeds.Count > 0)
            OnItemChanged(conn.PendingItemChangeds.Dequeue());
        while (conn.PendingItemStatsChangeds.Count > 0)
            OnItemStatsChanged(conn.PendingItemStatsChangeds.Dequeue());
        while (conn.PendingItemStatsRefresheds.Count > 0)
            OnItemStatsRefreshed(conn.PendingItemStatsRefresheds.Dequeue());
        while (conn.PendingItemDurabilities.Count > 0)
            OnItemDurability(conn.PendingItemDurabilities.Dequeue());
        while (conn.PendingItemExperiences.Count > 0)
            OnItemExperience(conn.PendingItemExperiences.Dequeue());
        while (conn.PendingItemsChangeds.Count > 0)
            OnItemsChanged(conn.PendingItemsChangeds.Dequeue());
        while (conn.PendingCurrencyChangeds.Count > 0)
        {
            var (idx, amount) = conn.PendingCurrencyChangeds.Dequeue();
            OnCurrencyChanged(idx, amount);
        }
        while (conn.PendingWeightUpdates.Count > 0)
        {
            var (bag, wear, hand) = conn.PendingWeightUpdates.Dequeue();
            OnWeightUpdate(bag, wear, hand);
        }
        while (conn.PendingStorageSizes.Count > 0)
            OnStorageSize(conn.PendingStorageSizes.Dequeue());
        while (conn.PendingNewMagics.Count > 0)
            OnNewMagic(conn.PendingNewMagics.Dequeue());
    }

    private void OnNewMagic(ClientUserMagic m)
    {
        if (m?.Info == null) return;
        UserMagics[m.Info] = m;
        GD.Print($"[Magic] 学会技能: {m.Info.Name} (Magic={m.Info.Magic}) Set1={m.Set1Key} Level={m.Level}");
        _magicBar?.Refresh();
        _magicDialog?.Refresh();
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
        _mapView.LoadMap(mapInfo.FileName, mapInfo.Background);
        _lightLayer?.SetMap(mapInfo, _mapView);
        _lightLayer?.SetDayTime(DayTime);
        _weatherLayer?.SetWeather(mapInfo.Weather);

        // M12: 小地图/大地图换图 (清动态标记, 重建静态 NPC/出口)
        if (_mapView.Map != null)
        {
            _miniMap?.SetMap(mapInfo, _mapView.Map.Width, _mapView.Map.Height, _playerObjectID);
            _bigMap?.SetMap(mapInfo, _mapView.Map.Width, _mapView.Map.Height, _playerObjectID, isCurrentMap: true);
        }

        // 换图: 清空旧地图的周围物体 (首次进图时 _objects 里是 Drain 的新图对象, 不清)
        if (clearObjects)
        {
            foreach (var fx in _itemGlows.Values)
                fx.QueueFree();
            _itemGlows.Clear();
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

        // M9: 拿起物品跟随鼠标 + 悬浮提示
        UpdateMouseItem();
        if (_combatController != null)
        {
            foreach (var ob in _objects.Values)
            {
                bool focused = ob.Type == ObjectRenderer.Kind.Item && ob == _combatController.MouseObject;
                if (ob.Focused != focused)
                {
                    ob.Focused = focused;
                    ob.QueueRedraw();
                }
            }
        }

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
        _mapView.CenterOn(_player.CellX, _player.CellY);
        _player.Position = _mapView.CellToScreen(_player.CellX, _player.CellY, true);
        _player.ZIndex = 100 + _player.CellY;
        UpdateObjectPositions();
    }

    // 视野范围随视口尺寸自适应 (窗口模式视口大, 固定 12x15 画不满)
    private void UpdateViewRange()
    {
        if (_mapView == null) return;
        var vp = GetViewport().GetVisibleRect().Size / WorldScale;
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

        foreach (var ob in _objects.Values)
        {
            ob.ComputeScreenPos(_mapView.CenterX, _mapView.CenterY, _mapView.ViewRangeX,
                _mapView.ViewRangeY, 0, 0, _mapView);
            ob.ZIndex = 100 + ob.CellY;
        }
    }

    // 格子坐标 -> 屏幕坐标 (与玩家居中公式一致)
    private Vector2 ComputeObjectScreenPos(int cellX, int cellY)
    {
        if (_mapView?.Map == null) return Vector2.Zero;

        return _mapView.CellToScreen(cellX, cellY, true);
    }


    // F1~F8 -> SpellKey.Spell01~08 -> 当前栏组里 SetXKey 匹配的技能 -> C.Magic
    private void UseMagicKey(int slot)
    {
        if (slot < 0 || slot > 7) return;
        var key = (Library.SpellKey)(slot + 1);  // Spell01 = 1
        ClientUserMagic magic = null;
        foreach (var kv in UserMagics)
        {
            var m = kv.Value;
            if (m == null) continue;
            if (MagicBarSpellSet == 1 && m.Set1Key == key) magic = m;
            else if (MagicBarSpellSet == 2 && m.Set2Key == key) magic = m;
            else if (MagicBarSpellSet == 3 && m.Set3Key == key) magic = m;
            else if (MagicBarSpellSet == 4 && m.Set4Key == key) magic = m;
            if (magic != null) break;
        }
        if (magic == null) return;
        // 朝当前目标方向; 无目标朝玩家朝向
        MirDirection dir = _playerDirection;
        var pCell = _playerLocation;
        uint targetID = _combatController?.TargetObject?.ObjectID ?? 0;
        _net.Connection.Enqueue(new C.Magic
        {
            Direction = dir,
            Action = MirAction.Spell,
            Type = magic.Info.Magic,
            Target = targetID,
            Location = new System.Drawing.Point(pCell.X, pCell.Y),
        });
        GD.Print($"[Magic] 释放 {magic.Info.Name} (Magic={magic.Info.Magic}) 方向={dir} 目标={targetID}");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed) return;
        GD.Print($"[Game] KEY {key.Keycode} ctrl={key.CtrlPressed} alt={key.AltPressed} shift={key.ShiftPressed}");
        if (_net?.Connection?.Connected != true) return;

        // M11: F2 开关状态窗口, Esc 关闭最上层窗口
        if (key.Keycode == Key.F2)
        {
            WindowManager.Toggle(_statusWindow, _uiLayer);
            return;
        }
        // F1, F3~F6, F8 = 释放魔法 (F2 状态窗口/F7 调试/F12 截图 已占用)
        // Ctrl+F1~F4 = 切魔法栏组 (原版 SpellSet01~04)
        if (key.Keycode >= Key.F1 && key.Keycode <= Key.F8)
        {
            if (key.CtrlPressed)
            {
                int set = key.Keycode switch
                {
                    Key.F1 => 1, Key.F2 => 2, Key.F3 => 3, Key.F4 => 4,
                    _ => 0,
                };
                if (set > 0)
                {
                    MagicBarSpellSet = set;
                    _magicBar?.Refresh();
                    GD.Print($"[Magic] 切换栏组 -> Set{set}");
                }
                return;
            }
            if (key.Keycode == Key.F2 || key.Keycode == Key.F7) { /* 已占用, 走下面 */ }
            else
            {
                UseMagicKey((int)(key.Keycode - Key.F1));  // 0~7 -> Spell01~08
                return;
            }
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

        // 功能键 (不走 KeyBindManager, 避免改 KeyBindManager 与他人冲突)
        if (key.Keycode == Key.M)
        {
            _net.Connection.Enqueue(new C.Mount());
            GD.Print("[Game] 请求上下马");
            return;
        }
        if (key.Keycode == Key.D && !key.CtrlPressed && !key.AltPressed && !key.ShiftPressed)
        {
            _autoRun = !_autoRun;
            _mouseWalker.AutoRun = _autoRun;
            GD.Print($"[Game] 自动跑步: {(_autoRun ? "开" : "关")}");
        }
        // T 请求交易 (原版 TradeRequest)
        if (key.Keycode == Key.T && !key.CtrlPressed && !key.AltPressed && !key.ShiftPressed)
        {
            var t = _combatController?.TargetObject;
            if (t != null && t.Type == ObjectRenderer.Kind.Monster) { /* 怪物不能交易, 忽略 */ }
            // TODO: 交易需要选中玩家; 当前无玩家选中, 预留
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
            GD.Print($"[Game] MOVE {dir.Value}");
            _net.Connection.Enqueue(new C.Move { Direction = dir.Value, Distance = 1 });
        }
        else if (key.Keycode == Key.F12)
        {
            var img = GetViewport().GetTexture().GetImage();
            img.SavePng("/tmp/game_screenshot.png");
            GD.Print("[Game] 截图保存 /tmp/game_screenshot.png");
        }
        else if (key.Keycode == Key.F7)
        {
            // TEMP M9 验证钩子: 地面掉落 10 金币 (提交前删除)
            GD.Print("[Game] TEMP CurrencyDrop 10 gold");
            _net.Connection.Enqueue(new C.CurrencyDrop { CurrencyIndex = 1, Amount = 10 });
        }
    }
}
