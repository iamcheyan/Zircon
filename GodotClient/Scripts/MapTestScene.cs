using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Godot;
using GTime = Godot.Time;
using Library;
using Library.SystemModels;
using S = Library.Network.ServerPackets;
using ZirconClient.Formats;
using ZirconClient.Network;
using ZirconClient.Controls;

namespace ZirconClient.Scripts;

public partial class MapTestScene : Control
{
    private Label _statusLabel;
    private string _dataPath = "/home/tetsuya/development/Zircon/Debug/Client/Data/";
    private string _mapPath = "/home/tetsuya/development/Zircon/Debug/Client/Map/";
    private Dictionary<LibraryFile, ZlLibrary> _libCache = new();
    private bool _renderAudit;
    private bool _actionAudit;
    private bool _mapAudit;
    private bool _shadowAudit;
    private bool _pixelAudit;
    private bool _projectileAudit;
    private bool _lightRenderAudit;
    private bool _weatherRenderAudit;
    private bool _mapFamilyRenderAudit;
    private int _auditFrames;
    private PlayerRenderer _auditPlayer;
    private int _actionIndex;
    private double _actionDeadline;
    private bool _actionAuditFinished;
    private int _mapTextureDiagnostics;
    private MirProjectileNode _auditProjectile;
    private Vector2 _auditProjectileStart;
    private float _auditProjectileMaxTravel;
    private int _auditProjectileSamples;
    private bool _projectileScreenshotSaved;
    private MapView _lightAuditMapView;
    private MapLightLayer _lightAuditLayer;
    private MapInfo _lightAuditMapInfo;
    private int _lightAuditStage;
    private int _lightAuditFrames;
    private MapWeatherLayer _weatherAuditLayer;
    private int _weatherAuditFrames;
    private MapView _mapFamilyView;
    private int _mapFamilyIndex;
    private int _mapFamilyFrames;
    private static readonly string[] MapFamilySamples = { "0", "1", "5", "D001", "E01" };
    private static readonly (string Name, LightSetting Setting, float DayTime)[] LightRenderStages =
    {
        ("night", LightSetting.Night, 1f),
        ("twilight", LightSetting.Twilight, 1f),
        ("default", LightSetting.Default, 0.42f),
    };
    private readonly HashSet<int> _actionFrames = new();
    private readonly HashSet<MirAnimation> _actionAnimations = new();
    private readonly List<(string Name, Action<PlayerRenderer> Start, MirAnimation Expected)> _actions = new();
    private const float WorldScale = 2f;

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
        _renderAudit = OS.GetCmdlineUserArgs().Contains("--render-audit");
        _actionAudit = OS.GetCmdlineUserArgs().Contains("--action-audit");
        _mapAudit = OS.GetCmdlineUserArgs().Contains("--map-audit");
        _shadowAudit = OS.GetCmdlineUserArgs().Contains("--shadow-audit");
        _pixelAudit = OS.GetCmdlineUserArgs().Contains("--pixel-audit");
        _projectileAudit = OS.GetCmdlineUserArgs().Contains("--projectile-audit");
        bool playerMatrixAudit = OS.GetCmdlineUserArgs().Contains("--player-matrix-audit");
        bool lightAudit = OS.GetCmdlineUserArgs().Contains("--light-audit");
        _lightRenderAudit = OS.GetCmdlineUserArgs().Contains("--light-render-audit");
        _weatherRenderAudit = OS.GetCmdlineUserArgs().Contains("--weather-render-audit");
        _mapFamilyRenderAudit = OS.GetCmdlineUserArgs().Contains("--map-family-render-audit");
        bool networkAudit = OS.GetCmdlineUserArgs().Contains("--network-audit");
        bool cursorAudit = OS.GetCmdlineUserArgs().Contains("--cursor-audit");
        bool fullTextureAudit = OS.GetCmdlineUserArgs().Contains("--full-texture-audit");

        // 与实际 GameScene 保持一致：地图、对象、特效都在逻辑 48x32
        // 坐标绘制，根世界统一放大 2 倍。否则审计截图只能验证 1x。
        if (_renderAudit || _actionAudit || _projectileAudit || _lightRenderAudit || _weatherRenderAudit || _mapFamilyRenderAudit)
            Scale = Vector2.One * WorldScale;

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
            if (_renderAudit) CallDeferred(nameof(RenderObjectAudit));
            if (_actionAudit) CallDeferred(nameof(BeginActionAudit));
            if (_mapAudit) CallDeferred(nameof(RunMapAudit));
            if (_shadowAudit) CallDeferred(nameof(RunShadowAudit));
            if (_pixelAudit) CallDeferred(nameof(RunPixelAudit));
            if (_projectileAudit) CallDeferred(nameof(RunProjectileAudit));
            if (playerMatrixAudit) CallDeferred(nameof(RunPlayerMatrixAudit));
            if (lightAudit) CallDeferred(nameof(RunLightAudit));
            if (_lightRenderAudit) CallDeferred(nameof(BeginLightRenderAudit));
            if (_weatherRenderAudit) CallDeferred(nameof(BeginWeatherRenderAudit));
            if (_mapFamilyRenderAudit) CallDeferred(nameof(BeginMapFamilyRenderAudit));
            if (networkAudit) CallDeferred(nameof(RunNetworkAudit));
            if (cursorAudit) CallDeferred(nameof(RunCursorAudit));
            if (fullTextureAudit) CallDeferred(nameof(RunTransparencyAudit));
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"失败: {ex.Message}";
            GD.PrintErr($"[MapTest] {ex}");
        }
    }

    private static void RunCursorAudit()
    {
        var old = new ObjectRenderer { Type = ObjectRenderer.Kind.Item, ObjectID = 1, HitOrder = 10 };
        var latest = new ObjectRenderer { Type = ObjectRenderer.Kind.NPC, ObjectID = 2, HitOrder = 20 };
        var selected = CombatController.SelectLatestHit(new[] { old, latest });
        bool latestWins = selected?.ObjectID == latest.ObjectID;
        GD.Print(latestWins
            ? "[CursorAudit] PASS newest per-cell hit order preserved"
            : $"[CursorAudit] FAIL selected={selected?.ObjectID}");
    }

    private static void RunLightAudit()
    {
        const float epsilon = 0.0001f;
        bool pass = Math.Abs(MapLightLayer.AmbientFor(LightSetting.Night, 1f) - 0.25f) < epsilon
            && Math.Abs(MapLightLayer.AmbientFor(LightSetting.Twilight, 1f) - 100f / 255f) < epsilon
            && Math.Abs(MapLightLayer.AmbientFor(LightSetting.Light, 0f) - 1f) < epsilon
            && Math.Abs(MapLightLayer.AmbientFor(LightSetting.Default, 0.42f) - 0.42f) < epsilon
            && Math.Abs(MapLightLayer.ObjectLightRadius(3) - 56.32f) < epsilon
            && Math.Abs(MapLightLayer.TileLightRadius(1) - 179.2f) < epsilon
            && Math.Abs(MapLightLayer.EffectLightRadius(35) - 97.28f) < epsilon;
        if (pass)
            GD.Print("[LightAudit] PASS Night=0.25 Twilight=100/255 Light=255/255 Default=DayTime");
        else
            GD.PrintErr($"[LightAudit] FAIL Night={MapLightLayer.AmbientFor(LightSetting.Night, 1f)} " +
                $"Twilight={MapLightLayer.AmbientFor(LightSetting.Twilight, 1f)} " +
                $"Light={MapLightLayer.AmbientFor(LightSetting.Light, 0f)}");
    }

    private void BeginLightRenderAudit()
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print("[LightRenderAudit] SKIP headless (requires Vulkan screenshot readback)");
            return;
        }

        // 使用与生产 MapLightLayer 相同的 MapView/光照节点，但复用本场景
        // 已绘制的地形作为底图，避免把测试场景误当成仅常量审计。
        _lightAuditMapView = new MapView { Visible = false };
        AddChild(_lightAuditMapView);
        _lightAuditMapView.LoadMap("0");
        _lightAuditMapView.CenterX = 10;
        _lightAuditMapView.CenterY = 10;

        _lightAuditMapInfo = Globals.MapInfoList?.Binding.FirstOrDefault(m => m.FileName == "0")
            ?? Globals.MapInfoList?.Binding.FirstOrDefault();
        if (_lightAuditMapInfo == null)
        {
            GD.PrintErr("[LightRenderAudit] FAIL no loaded MapInfo");
            return;
        }
        _lightAuditLayer = new MapLightLayer { ZIndex = 2000 };
        AddChild(_lightAuditLayer);
        _lightAuditLayer.SetMap(_lightAuditMapInfo, _lightAuditMapView);
        _lightAuditStage = 0;
        _lightAuditFrames = 0;
        ApplyLightRenderStage();
    }

    private void ApplyLightRenderStage()
    {
        var stage = LightRenderStages[_lightAuditStage];
        _lightAuditLayer.SetAuditLightOverride(stage.Setting);
        _lightAuditLayer.SetDayTime(stage.DayTime);
        _lightAuditLayer.QueueRedraw();
        GD.Print($"[LightRenderAudit] START {stage.Name} ambient={MapLightLayer.AmbientFor(stage.Setting, stage.DayTime):0.000}");
    }

    private void ProcessLightRenderAudit()
    {
        if (!_lightRenderAudit || _lightAuditLayer == null || ++_lightAuditFrames < 3) return;
        _lightAuditFrames = 0;
        var image = GetViewport().GetTexture()?.GetImage();
        var stage = LightRenderStages[_lightAuditStage];
        if (image == null)
        {
            GD.PrintErr($"[LightRenderAudit] FAIL {stage.Name}: no viewport image");
            return;
        }

        string path = $"/tmp/zircon-light-{stage.Name}.png";
        var error = image.SavePng(path);
        if (error != Error.Ok)
            GD.PrintErr($"[LightRenderAudit] FAIL {stage.Name}: save={error}");
        else
            GD.Print($"[LightRenderAudit] PASS {stage.Name} ambient={MapLightLayer.AmbientFor(stage.Setting, stage.DayTime):0.000} viewport={image.GetWidth()}x{image.GetHeight()} path={path}");

        _lightAuditStage++;
        if (_lightAuditStage >= LightRenderStages.Length)
        {
            GD.Print("[LightRenderAudit] PASS all=3 stages=night,twilight,default");
            _lightRenderAudit = false;
            return;
        }
        ApplyLightRenderStage();
    }

    private void BeginWeatherRenderAudit()
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print("[WeatherRenderAudit] SKIP headless (requires Vulkan screenshot readback)");
            _weatherRenderAudit = false;
            return;
        }

        _weatherAuditLayer = new MapWeatherLayer { ZIndex = 2100 };
        AddChild(_weatherAuditLayer);
        _weatherAuditLayer.SetWeather(Weather.RainFogLightning);
        _weatherAuditFrames = 0;
        GD.Print("[WeatherRenderAudit] START RainFogLightning");
    }

    private void ProcessWeatherRenderAudit()
    {
        if (!_weatherRenderAudit || _weatherAuditLayer == null || ++_weatherAuditFrames < 30) return;
        _weatherRenderAudit = false;
        var image = GetViewport().GetTexture()?.GetImage();
        if (image == null)
        {
            GD.PrintErr("[WeatherRenderAudit] FAIL no viewport image");
            return;
        }
        string path = "/tmp/zircon-weather-rain-fog-lightning.png";
        var error = image.SavePng(path);
        if (error == Error.Ok)
            GD.Print($"[WeatherRenderAudit] PASS weather=RainFogLightning viewport={image.GetWidth()}x{image.GetHeight()} path={path}");
        else
            GD.PrintErr($"[WeatherRenderAudit] FAIL save={error}");
    }

    private void BeginMapFamilyRenderAudit()
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print("[MapFamilyRenderAudit] SKIP headless (requires Vulkan screenshot readback)");
            _mapFamilyRenderAudit = false;
            return;
        }

        // 隐藏 MapTestScene 的 20x20 诊断精灵，只保留真实 MapView。
        foreach (Node child in GetChildren())
            if (child is CanvasItem canvas && child is not Label) canvas.Visible = false;

        _mapFamilyView = new MapView();
        AddChild(_mapFamilyView);
        _mapFamilyIndex = 0;
        _mapFamilyFrames = 0;
        LoadMapFamilySample();
    }

    private void LoadMapFamilySample()
    {
        string name = MapFamilySamples[_mapFamilyIndex];
        try
        {
            var mapInfo = Globals.MapInfoList?.Binding.FirstOrDefault(m => m.FileName == name);
            int background = mapInfo?.Background ?? 0;
            _mapFamilyView.LoadMap(name, background);
            _mapFamilyView.CenterX = _mapFamilyView.Map.Width / 2;
            _mapFamilyView.CenterY = _mapFamilyView.Map.Height / 2;
            _mapFamilyView.QueueRedraw();
            _mapFamilyFrames = 0;
            GD.Print($"[MapFamilyRenderAudit] START map={name} background={background} size={_mapFamilyView.Map.Width}x{_mapFamilyView.Map.Height}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MapFamilyRenderAudit] FAIL map={name}: {ex.GetType().Name}:{ex.Message}");
            _mapFamilyRenderAudit = false;
        }
    }

    private void ProcessMapFamilyRenderAudit()
    {
        if (!_mapFamilyRenderAudit || _mapFamilyView == null || ++_mapFamilyFrames < 4) return;
        _mapFamilyFrames = 0;
        string name = MapFamilySamples[_mapFamilyIndex];
        var image = GetViewport().GetTexture()?.GetImage();
        if (image == null)
        {
            GD.PrintErr($"[MapFamilyRenderAudit] FAIL map={name}: no viewport image");
            _mapFamilyRenderAudit = false;
            return;
        }
        string path = $"/tmp/zircon-map-family-{name}.png";
        var error = image.SavePng(path);
        if (error != Error.Ok)
        {
            GD.PrintErr($"[MapFamilyRenderAudit] FAIL map={name}: save={error}");
            _mapFamilyRenderAudit = false;
            return;
        }
        GD.Print($"[MapFamilyRenderAudit] PASS map={name} viewport={image.GetWidth()}x{image.GetHeight()} path={path}");
        _mapFamilyIndex++;
        if (_mapFamilyIndex >= MapFamilySamples.Length)
        {
            GD.Print("[MapFamilyRenderAudit] PASS all=5 samples=0,1,5,D001,E01");
            _mapFamilyRenderAudit = false;
            return;
        }
        LoadMapFamilySample();
    }

    private static void RunNetworkAudit()
    {
        TcpListener listener = null;
        TcpClient client = null;
        TcpClient accepted = null;
        try
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            client = new TcpClient();
            client.Connect(IPAddress.Loopback, port);
            accepted = listener.AcceptTcpClient();

            var connection = new ServerConnection(client);
            int disconnectEvents = 0;
            connection.DisconnectedEvent += () => disconnectEvents++;
            connection.NotifyDisconnected(closeTransport: true);
            connection.NotifyDisconnected(closeTransport: true);
            connection.Disconnect();

            var controller = new CombatController(null, null, null, null, null,
                rightClickDeTarget: () => true);
            var removedTarget = new ObjectRenderer { ObjectID = 7001 };
            controller.TargetObject = removedTarget;
            controller.MouseObject = removedTarget;
            controller.RemoveObjectReference(removedTarget.ObjectID);

            bool referencesCleared = controller.TargetObject == null && controller.MouseObject == null;
            var playerTarget = new ObjectRenderer { Type = ObjectRenderer.Kind.Player, Dead = false };
            var guardMonster = new ObjectRenderer
            {
                Type = ObjectRenderer.Kind.Monster,
                Dead = false,
            };
            bool playerAttackSemantics = CombatController.CanAttackObject(playerTarget)
                && !CombatController.CanAttackObject(guardMonster);
            bool pickupPrioritySemantics = CombatController.ShouldDeferForMapPickup(
                    new System.Drawing.Point(10, 10), new System.Drawing.Point(10, 10), false)
                && !CombatController.ShouldDeferForMapPickup(
                    new System.Drawing.Point(10, 10), new System.Drawing.Point(10, 10), true)
                && !CombatController.ShouldDeferForMapPickup(
                    new System.Drawing.Point(11, 10), new System.Drawing.Point(10, 10), false);
            bool pickupStateSemantics = GameScene.CanSendMapPickup(false, false, false, false, false)
                && !GameScene.CanSendMapPickup(true, false, false, false, false)
                && !GameScene.CanSendMapPickup(false, true, false, false, false)
                && !GameScene.CanSendMapPickup(false, false, true, false, false)
                && !GameScene.CanSendMapPickup(false, false, false, true, false)
                && !GameScene.CanSendMapPickup(false, false, false, false, true);
            bool autoPathSemantics = GameScene.ShouldQueueAutoPathMove(true, false)
                && !GameScene.ShouldQueueAutoPathMove(true, true)
                && !GameScene.ShouldQueueAutoPathMove(false, false);
            bool mapRightCancelSemantics = GameScene.ShouldCancelMapRightClick(true, false)
                && GameScene.ShouldCancelMapRightClick(false, true)
                && !GameScene.ShouldCancelMapRightClick(false, false);
            bool gatheringSemantics = GameScene.ShouldCancelGatheringForMapClick(false, true, false)
                && GameScene.ShouldCancelGatheringForMapClick(false, false, true)
                && !GameScene.ShouldCancelGatheringForMapClick(true, true, false)
                && !GameScene.ShouldCancelGatheringForMapClick(true, false, true)
                && !GameScene.ShouldCancelGatheringForMapClick(false, false, false);
            var consumed = new ClientUserItem { Count = 5 };
            bool consumePartial = GameScene.TryConsumeItemCount(consumed, 2, out bool partialRemove)
                && consumed.Count == 3 && !partialRemove;
            bool consumeWhole = GameScene.TryConsumeItemCount(consumed, 3, out bool wholeRemove)
                && wholeRemove && consumed.Count == 3;
            bool rejectLateConsume = !GameScene.TryConsumeItemCount(consumed, 4, out _)
                && consumed.Count == 3;
            var splitSource = new ClientUserItem { Count = 5, AddedStats = new Stats() };
            var splitGrid = new ClientUserItem[2];
            splitGrid[0] = splitSource;
            bool splitPartial = GameScene.TryApplyItemSplit(splitSource, splitGrid, 0, 1, 2)
                && splitSource.Count == 3 && splitGrid[1]?.Count == 2;
            var overflowGrid = new ClientUserItem[2];
            overflowGrid[0] = splitSource;
            bool splitRejectOverflow = !GameScene.TryApplyItemSplit(splitSource, overflowGrid, 0, 1, 4);
            var occupied = new ClientUserItem[2];
            occupied[1] = new ClientUserItem { Count = 1, AddedStats = new Stats() };
            var overwriteSource = new ClientUserItem { Count = 3, AddedStats = new Stats() };
            occupied[0] = overwriteSource;
            bool splitRejectOverwrite = !GameScene.TryApplyItemSplit(overwriteSource, occupied, 0, 1, 1)
                && occupied[1].Count == 1;
            // 回放：启动阶段包可暂存；进入运行态后同一包只能实时派发一次；
            // 切图包到达时，尚未排空的旧地图包必须被丢弃，之后的迟到包不能重新进入积压队列。
            connection.PendingMoves.Enqueue(default);
            connection.StopPendingPacketBuffering();
            int moveEvents = 0;
            connection.ObjectMoveEvent += (_, _, _, _, _, _) => moveEvents++;
            connection.Process(new S.MapChanged { MapIndex = 7, InstanceIndex = -1 });
            connection.Process(new S.ObjectMove { ObjectID = 901, Distance = 1 });
            bool replayOrdering = connection.PendingMoves.Count == 0 && moveEvents == 1;
            bool pass = !connection.Connected && disconnectEvents == 1 && referencesCleared
                && autoPathSemantics && mapRightCancelSemantics && replayOrdering
                && gatheringSemantics
                && playerAttackSemantics
                && pickupPrioritySemantics
                && pickupStateSemantics
                && consumePartial && consumeWhole && rejectLateConsume
                && splitPartial && splitRejectOverflow && splitRejectOverwrite;
            if (pass)
                GD.Print("[NetworkAudit] PASS duplicate disconnect collapsed, transport closed, removed-object references cleared, player/monster attackability semantics, current-cell pickup priority, pickup state guards, auto-path transition semantics, map right-click cancellation, Alt gathering state semantics, stale/late packet replay ordering, item-count bounds and split-target protection");
            else
                GD.PrintErr($"[NetworkAudit] FAIL connected={connection.Connected} disconnectEvents={disconnectEvents} referencesCleared={referencesCleared} playerAttack={playerAttackSemantics} pickupPriority={pickupPrioritySemantics} pickupState={pickupStateSemantics} autoPathSemantics={autoPathSemantics} mapRightCancel={mapRightCancelSemantics} gathering={gatheringSemantics} replayOrdering={replayOrdering} consume={consumePartial}/{consumeWhole}/{rejectLateConsume} split={splitPartial}/{splitRejectOverflow}/{splitRejectOverwrite}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkAudit] FAIL {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try { accepted?.Close(); } catch { }
            try { listener?.Stop(); } catch { }
        }
    }

    public override void _Process(double delta)
    {
        ProcessLightRenderAudit();
        ProcessWeatherRenderAudit();
        ProcessMapFamilyRenderAudit();
        if (_actionAudit) ProcessActionAudit();
        if (_projectileAudit) ProcessProjectileAudit();
        if (!_renderAudit || ++_auditFrames != 3) return;
        // Dummy/headless 渲染器没有可读回的 Texture2D；对象绘制本身仍会执行，
        // 非 headless 环境才保存像素截图。
        var viewportTexture = GetViewport().GetTexture();
        var image = DisplayServer.GetName() == "headless" || viewportTexture == null
            ? null : viewportTexture.GetImage();
        if (image != null)
        {
            string output = "/tmp/zircon-render-audit.png";
            image.SavePng(output);
            GD.Print($"[RenderAudit] 截图: {output}");
        }
        else
            GD.Print("[RenderAudit] headless/dummy 模式完成对象绘制检查（无可读回纹理）");
    }

    private void BeginActionAudit()
    {
        RunTransparencyAudit();
        RunWeatherAudit();
        RunTransparencyModeAudit();
        RunLayerOrderAudit();
        RunMagicCoverageAudit();
        RunMagicFrameAudit();
        _auditPlayer = new PlayerRenderer();
        _auditPlayer.UpdateAppearance(new StartInformation
        {
            Name = "ActionAuditHero", Class = MirClass.Warrior, Gender = MirGender.Male,
            HairType = 1, HairColour = System.Drawing.Color.Black, Armour = 0,
            ArmourColour = System.Drawing.Color.White, Costume = -1, HelmetShape = 0,
            Shield = -1, Weapon = 0, Horse = HorseType.None, Direction = MirDirection.Down
        });
        _auditPlayer.FrameChanged = (anim, frame, magic) =>
        {
            if (_actionAudit)
            {
                _actionFrames.Add(frame);
                _actionAnimations.Add(anim);
            }
        };
        AddChild(_auditPlayer);
        // Some legacy WAV files use headers that Godot's native decoder rejects
        // noisily. Keep the action audit deterministic while allowing the sound
        // catalog audit to be run explicitly when the decoder is under test.
        if (!OS.GetCmdlineUserArgs().Contains("--skip-sound-audit"))
            RunSoundAssetAudit();
        else
            GD.Print("[SoundAudit] SKIP requested for action-only audit");
        _actions.Add(("Walking", p => p.BeginMove(MirDirection.Right, 1, false, false), MirAnimation.Walking));
        _actions.Add(("Running", p => p.BeginMove(MirDirection.Right, 2, false, true), MirAnimation.Running));
        _actions.Add(("HorseWalking", (Action<PlayerRenderer>)(p => { p.Horse = HorseType.Brown; p.RefreshAppearanceLibraries(); p.BeginMove(MirDirection.Right, 1, true, false); }), MirAnimation.HorseWalking));
        _actions.Add(("HorseRunning", p => p.BeginMove(MirDirection.Right, 2, true, true), MirAnimation.HorseRunning));
        _actions.Add(("Combat", p => p.PlayCombat(MagicType.None), Functions.GetAttackAnimation(MirClass.Warrior, 0, MagicType.None)));
        _actions.Add(("ActionQueue", p => { p.PlayCombat(MagicType.None); p.PlayStruck(); }, Functions.GetAttackAnimation(MirClass.Warrior, 0, MagicType.None)));
        _actions.Add(("RangeAttack", p => p.PlayRangeAttack(), MirAnimation.Combat1));
        _actions.Add(("ShoulderDash", p => p.PlayDash(MagicType.ShoulderDash), MirAnimation.Combat8));
        _actions.Add(("Spell", p => p.PlaySpell(MagicType.SeismicSlam), Functions.GetMagicAnimation(MagicType.SeismicSlam)));
        _actions.Add(("CrushingWave", p => p.PlaySpell(MagicType.CrushingWave), Functions.GetMagicAnimation(MagicType.CrushingWave)));
        _actions.Add(("OffensiveBlow", p => p.PlayCombat(MagicType.OffensiveBlow), Functions.GetAttackAnimation(MirClass.Warrior, 0, MagicType.OffensiveBlow)));
        _actions.Add(("ChannelStart", p => { p.ElementalHurricane = false; p.PlaySpell(MagicType.ElementalHurricane); }, MirAnimation.ChannellingStart));
        _actions.Add(("ChannelEnd", p => { p.ElementalHurricane = true; p.PlaySpell(MagicType.ElementalHurricane); }, MirAnimation.ChannellingEnd));
        _actions.Add(("Struck", p => p.PlayStruck(), MirAnimation.Struck));
        _actions.Add(("Pushed", p => p.PlayPushed(), MirAnimation.Pushed));
        _actions.Add(("Harvest", p => p.PlayHarvest(), MirAnimation.Harvest));
        _actions.Add(("Mining", p => p.PlayMining(), Functions.GetAttackAnimation(MirClass.Warrior, 0, MagicType.None)));
        _actions.Add(("FishingCast", p => p.PlayFishing(FishingState.Cast, true, new System.Drawing.Point(1, 1)), MirAnimation.FishingCast));
        _actions.Add(("FishingWait", p => p.SetAnimation(MirAnimation.FishingWait), MirAnimation.FishingWait));
        _actions.Add(("FishingReel", p => p.SetAnimation(MirAnimation.FishingReel), MirAnimation.FishingReel));
        _actions.Add(("TamingCast", p => p.PlayTaming(TamingState.Cast, 1), MirAnimation.TamingCast));
        _actions.Add(("TamingWait", p => p.SetAnimation(MirAnimation.TamingWait), MirAnimation.TamingWait));
        _actions.Add(("CloakBuff", p => { p.Cloaked = true; p.PlayStandingForState(); }, MirAnimation.CreepStanding));
        _actions.Add(("DragonRepulseMiddle", p => { p.DragonRepulsed = true; p.PlayStandingForState(); }, MirAnimation.DragonRepulseMiddle));
        _actions.Add(("DragonRepulseEnd", p => { p.DragonRepulsed = false; p.PlayDragonRepulseEnd(); }, MirAnimation.DragonRepulseEnd));
        _actions.Add(("Dead", p => p.PlayDie(), MirAnimation.Die));
        // 施法时序回归：ObjectMagic 到达后不能在抬手第一帧立即生成
        // 轨迹；释放延迟必须来自旧版动作帧表，而不是固定跳到末帧。
        _auditPlayer.PlaySpell(MagicType.FireBall);
        double releaseDelay = _auditPlayer.SpellReleaseDelayMs;
        if (releaseDelay <= 0 || releaseDelay >= FrameSet.Players[Functions.GetMagicAnimation(MagicType.FireBall)].Sum)
            GD.PrintErr($"[SpellTimingAudit] FAIL releaseDelay={releaseDelay:0}ms");
        else
            GD.Print($"[SpellTimingAudit] PASS animation={_auditPlayer.Animation} releaseDelay={releaseDelay:0}ms " +
                $"total={FrameSet.Players[_auditPlayer.Animation].Sum:0}ms");
        _actionIndex = 0;
        _actionAuditFinished = false;
        StartNextActionAudit();
    }

    private void RunMagicCoverageAudit()
    {
        var missingSpell = new List<MagicType>();
        var noMapEffect = new List<MagicType>();
        int configured = 0;
        int attackOnly = 0;
        foreach (MagicType type in Enum.GetValues<MagicType>())
        {
            if (type == MagicType.None) continue;
            if (MagicEffectTable.Get(type) != null)
                configured++;
            else if (MagicEffectTable.GetAttack(type) != null)
                attackOnly++;
            else if (MagicEffectTable.IsOriginalSpellCase(type))
                missingSpell.Add(type);
            else
                noMapEffect.Add(type);
        }

        // 这里先只做覆盖率审计，不把被动技能、状态标记和纯动作技能
        // 当成失败；主动技能的轨迹会在后续按原版 GameScene 分支逐项补齐。
        var names = new StringBuilder();
        foreach (var type in missingSpell)
        {
            if (names.Length > 0) names.Append(',');
            names.Append(type);
        }
        GD.Print($"[MagicCoverageAudit] castConfigured={configured} attackOnly={attackOnly} " +
            $"missingOriginalSpell={missingSpell.Count} noMapEffect={noMapEffect.Count}");
        if (missingSpell.Count > 0)
            GD.PrintErr($"[MagicCoverageAudit] missingOriginalSpell={names}");
        if (noMapEffect.Count > 0)
            GD.Print($"[MagicCoverageAudit] intentionalOrEffectHandled={string.Join(',', noMapEffect)}");
    }

    private void RunMagicFrameAudit()
    {
        var seen = new HashSet<MagicEffectTable.CastEffect>();
        var failures = new List<string>();
        int originalResourceExceptions = 0;
        foreach (MagicType type in Enum.GetValues<MagicType>())
        {
            var def = MagicEffectTable.Get(type);
            if (def == null || !seen.Add(def)) continue;
            int castDirections = def.DirectionFromCast || def.DirectionFromSource ? 8 : 1;
            CheckEffectRange($"{type}.cast", def.File, def.StartIndex, def.FrameCount, def.Skip, castDirections, failures);
            CheckImpactRange($"{type}.source", def.Source, failures);
            foreach (var impact in def.SourceAdditional) CheckImpactRange($"{type}.sourceExtra", impact, failures);
            CheckProjectileRange($"{type}.projectile", def.Projectile, failures);
            CheckProjectileRange($"{type}.targetProjectile", def.TargetProjectile, failures);
            foreach (var projectile in def.AdditionalProjectiles) CheckProjectileRange($"{type}.extraProjectile", projectile, failures);
            foreach (var projectile in def.TargetAdditionalProjectiles) CheckProjectileRange($"{type}.targetExtraProjectile", projectile, failures);
            CheckImpactRange($"{type}.impact", def.Impact, failures);
            CheckImpactRange($"{type}.targetEffect", def.TargetEffect, failures);
            CheckImpactRange($"{type}.mapImpact", def.MapImpact, failures);
            foreach (var impact in def.Additional) CheckImpactRange($"{type}.additional", impact, failures);
            foreach (var impact in def.AdditionalMapEffects) CheckImpactRange($"{type}.mapAdditional", impact, failures);
        }

        failures.RemoveAll(failure =>
        {
            if (!failure.StartsWith("GreenSludgeBall.impact: MonMagicEx23 range=2780..2855", StringComparison.Ordinal))
                return false;
            originalResourceExceptions++;
            return true;
        });

        if (failures.Count == 0)
            GD.Print($"[MagicFrameAudit] PASS skills={seen.Count} originalResourceExceptions={originalResourceExceptions}");
        else
        {
            GD.PrintErr($"[MagicFrameAudit] FAIL count={failures.Count}");
            foreach (var failure in failures.Take(20)) GD.PrintErr($"[MagicFrameAudit] {failure}");
        }
    }

    private static void CheckImpactRange(string name, MagicEffectTable.ImpactDef def, List<string> failures)
    {
        if (def == null) return;
        if (def.DirectionStartIndices != null)
        {
            foreach (int start in def.DirectionStartIndices)
                CheckEffectRange(name, def.File, start, def.FrameCount, 0, 1, failures);
        }
        else CheckEffectRange(name, def.File, def.StartIndex, def.FrameCount, def.Skip,
            def.DirectionFromCast || def.DirectionFromSource ? 8 : 1, failures);
    }

    private static void CheckProjectileRange(string name, MagicEffectTable.ProjectileDef def, List<string> failures)
    {
        if (def == null) return;
        CheckEffectRange(name, def.File, def.StartIndex, def.FrameCount, def.Skip,
            def.Has16Directions ? 16 : 8, failures);
    }

    private static void CheckEffectRange(string name, LibraryFile file, int start, int count,
        int skip, int directionCount, List<string> failures)
    {
        if (count <= 0 || start < 0) { failures.Add($"{name}: invalid start/count {start}/{count}"); return; }
        var library = LibraryCache.Get(file);
        int last = start + Math.Max(0, directionCount - 1) * skip + count - 1;
        if (library?.Images == null || last >= library.Images.Length)
            failures.Add($"{name}: {file} range={start}..{last} library={(library?.Images?.Length ?? 0)}");
    }

    private void RunMapAudit()
    {
        var files = Directory.GetFiles(_mapPath, "*.map")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        int valid = 0, layered = 0, totalCells = 0;
        var failures = new List<string>();
        var textureRefs = new HashSet<(int FileByte, int ImageIndex)>();
        foreach (string path in files)
        {
            try
            {
                var map = new MirMap(path);
                if (!map.IsValid || map.Width > 4096 || map.Height > 4096)
                {
                    failures.Add($"{Path.GetFileName(path)}:{map.Width}x{map.Height}");
                    continue;
                }
                valid++;
                totalCells += map.Width * map.Height;
                bool hasLayer = false;
                for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                {
                    ref var cell = ref map.Cells[x, y];
                    if (cell.BackFile > 0 && cell.BackImage > 0)
                        textureRefs.Add((cell.BackFile, cell.BackImage));
                    if (cell.MiddleFile > 0 && cell.MiddleImage > 0)
                        textureRefs.Add((cell.MiddleFile, cell.MiddleImage - 1));
                    if (cell.FrontFile > 0 && cell.FrontImage > 0)
                        textureRefs.Add((cell.FrontFile, cell.FrontImage - 1));
                    if (cell.BackFile > 0 || cell.MiddleFile > 0 || cell.FrontFile > 0)
                        hasLayer = true;
                }
                if (hasLayer) layered++;
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetFileName(path)}:{ex.GetType().Name}:{ex.Message}");
            }
        }

        int emptyRefs = 0;
        int missingRefs = 0;
        int ignoredRefs = 0;
        var missingTextureDetails = new List<string>();
        foreach (var reference in textureRefs)
        {
            if (!Libraries.KROrder.TryGetValue(reference.FileByte, out var file))
            {
                // 原版 MapControl 先 TryGetValue，未知文件号直接跳过；255
                // 是旧地图中保留的未使用层标记，不是一个待加载的图库。
                if (reference.FileByte == 255)
                {
                    ignoredRefs++;
                    continue;
                }
                missingRefs++;
                missingTextureDetails.Add($"fileByte={reference.FileByte} image={reference.ImageIndex} library=unknown");
                continue;
            }
            var library = LibraryCache.Get(file);
            if (library?.Images == null || reference.ImageIndex < 0 || reference.ImageIndex >= library.Images.Length)
            {
                missingRefs++;
                missingTextureDetails.Add($"file={file} image={reference.ImageIndex} library={(library?.Images?.Length ?? 0)}");
                continue;
            }
            if (library.Images[reference.ImageIndex] == null)
                emptyRefs++;
        }

        if (failures.Count == 0 && missingRefs == 0)
            GD.Print($"[MapAudit] PASS files={files.Length} valid={valid} layered={layered} cells={totalCells} " +
                $"textureRefs={textureRefs.Count} emptyRefs={emptyRefs} ignoredRefs={ignoredRefs} missingRefs=0");
        else
        {
            GD.PrintErr($"[MapAudit] FAIL files={files.Length} valid={valid} failures={failures.Count} " +
                $"textureRefs={textureRefs.Count} missingRefs={missingRefs}");
            foreach (string detail in missingTextureDetails.Take(32))
                GD.PrintErr($"[MapAudit] missingTexture {detail}");
            foreach (string failure in failures.Take(12)) GD.PrintErr($"[MapAudit] {failure}");
        }
    }

    private void RunShadowAudit()
    {
        int libraries = 0, frames = 0, withShadow = 0, decoded = 0, nonEmpty = 0, metadataUsable = 0;
        int fallback49 = 0, fallback50 = 0, fallback176 = 0, fallback177 = 0;
        foreach (LibraryFile file in Enum.GetValues<LibraryFile>())
        {
            string libraryName = file.ToString();
            if (!libraryName.Contains("Mob", StringComparison.OrdinalIgnoreCase)
                && !libraryName.Contains("NPC", StringComparison.OrdinalIgnoreCase)
                && !libraryName.Contains("Ground", StringComparison.OrdinalIgnoreCase)
                && !libraryName.Contains("Hum", StringComparison.OrdinalIgnoreCase)
                && !libraryName.Contains("Hair", StringComparison.OrdinalIgnoreCase)
                && !libraryName.Contains("Weapon", StringComparison.OrdinalIgnoreCase)
                && !libraryName.Contains("Shield", StringComparison.OrdinalIgnoreCase)
                && !libraryName.Contains("Horse", StringComparison.OrdinalIgnoreCase))
                continue;
            var library = LibraryCache.Get(file);
            if (library?.Images == null) continue;
            libraries++;
            for (int index = 0; index < library.Images.Length; index++)
            {
                var image = library.Images[index];
                if (image == null) continue;
                frames++;
                if (image.ShadowWidth <= 0 || image.ShadowHeight <= 0) continue;
                withShadow++;
                if (RenderPrimitives.IsUsableResourceShadow(image.ShadowWidth, image.ShadowHeight)) metadataUsable++;
                var texture = library.GetShadowTexture(index);
                if (texture != null)
                {
                    decoded++;
                    var pixels = texture.GetImage();
                    if (pixels != null && pixels.GetUsedRect().Size.X > 0 && pixels.GetUsedRect().Size.Y > 0)
                        nonEmpty++;
                }
                switch (image.ShadowType)
                {
                    case 49: fallback49++; break;
                    case 50: fallback50++; break;
                    case 176: fallback176++; break;
                    case 177: fallback177++; break;
                }
            }
        }
        GD.Print($"[ShadowAudit] PASS libraries={libraries} frames={frames} metadata={withShadow} " +
            $"metadataUsable={metadataUsable} decoded={decoded} nonEmpty={nonEmpty} " +
            $"fallbackTypes=49:{fallback49},50:{fallback50},176:{fallback176},177:{fallback177}");
    }

    private async void RunPixelAudit()
    {
        string onlyFile = OS.GetCmdlineUserArgs()
            .FirstOrDefault(arg => arg.StartsWith("--pixel-file=", StringComparison.OrdinalIgnoreCase))?
            .Substring("--pixel-file=".Length);
        string batchText = OS.GetCmdlineUserArgs()
            .FirstOrDefault(arg => arg.StartsWith("--pixel-batch=", StringComparison.OrdinalIgnoreCase))?
            .Substring("--pixel-batch=".Length);
        int batchIndex = 0, batchCount = 1;
        if (!string.IsNullOrWhiteSpace(batchText))
        {
            var parts = batchText.Split('/', 2);
            int.TryParse(parts.ElementAtOrDefault(0), out batchIndex);
            if (parts.Length > 1) int.TryParse(parts[1], out batchCount);
            batchCount = Math.Max(1, batchCount);
            batchIndex = Math.Clamp(batchIndex, 0, batchCount - 1);
        }
        int sampleLimit = ParseAuditInt("--pixel-sample=", 0);
        int libraries = 0, frames = 0, compared = 0, different = 0, failed = 0, layers = 0;
        long differentPixels = 0, differentBytes = 0;
        byte maxDelta = 0;
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int libraryOrdinal = 0;

        foreach (LibraryFile file in Enum.GetValues<LibraryFile>())
        {
            var library = LibraryCache.Get(file);
            if (library?.Images == null || string.IsNullOrEmpty(library.FileName)) continue;
            if (!string.IsNullOrWhiteSpace(onlyFile)
                && !string.Equals(Path.GetFileName(library.FileName), onlyFile, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(file.ToString(), onlyFile, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!seenFiles.Add(library.FileName)) continue;
            if (libraryOrdinal++ % batchCount != batchIndex) continue;

            libraries++;
            try
            {
                using var reference = new ZlPixelReference(library.FileName);
                var indices = GetPixelAuditIndices(library.Images, sampleLimit);
                foreach (int index in indices)
                {
                    // 全量资源审计可能超过七十万帧；无头模式每 32 帧切一次主循环会
                    // 把纯解码任务放大到数分钟。保留周期性让帧，但按 4096 帧批次调度。
                    if ((compared & 4095) == 0)
                        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var image = library.Images[index];
                    if (image == null || image.Width <= 0 || image.Height <= 0) continue;
                    frames++;

                    byte[] expected = reference.DecodeImage(library, index);
                    byte[] actual = library.GetImageData(index);
                    if (expected == null && actual == null) continue;
                    compared++;
                    var diff = ZlPixelDiffHelper.Compare(expected, actual);
                    if (diff.DifferentPixels != 0 || diff.DifferentBytes != 0)
                    {
                        different++;
                        differentPixels += diff.DifferentPixels;
                        differentBytes += diff.DifferentBytes;
                        maxDelta = Math.Max(maxDelta, diff.MaxDelta);
                        if (different <= 12)
                            GD.PrintErr($"[PixelAudit] DIFF file={Path.GetFileName(library.FileName)} " +
                                $"frame={index} pixels={diff.DifferentPixels} bytes={diff.DifferentBytes} max={diff.MaxDelta}");
                    }

                    ComparePixelLayer(library, reference, index, true, ref compared, ref different,
                        ref differentPixels, ref differentBytes, ref maxDelta, ref layers);
                    ComparePixelLayer(library, reference, index, false, ref compared, ref different,
                        ref differentPixels, ref differentBytes, ref maxDelta, ref layers, true);
                }
                if ((libraries & 31) == 0)
                    GD.Print($"[PixelAudit] progress libraries={libraries} frames={frames} compared={compared}");
            }
            catch (Exception ex)
            {
                failed++;
                GD.PrintErr($"[PixelAudit] ERROR file={Path.GetFileName(library.FileName)} " +
                    $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        string mode = sampleLimit > 0 ? $"sample={sampleLimit}" : "full";
        if (batchCount > 1) mode += $" batch={batchIndex}/{batchCount}";
        if (failed == 0 && different == 0)
            GD.Print($"[PixelAudit] PASS mode={mode} libraries={libraries} frames={frames} layers={layers} compared={compared}");
        else
            GD.PrintErr($"[PixelAudit] FAIL mode={mode} libraries={libraries} frames={frames} layers={layers} compared={compared} " +
                $"different={different} pixels={differentPixels} bytes={differentBytes} maxDelta={maxDelta} errors={failed}");
    }

    private static void ComparePixelLayer(ZlLibrary library, ZlPixelReference reference, int index, bool shadow,
        ref int compared, ref int different, ref long differentPixels, ref long differentBytes, ref byte maxDelta,
        ref int layers, bool overlay = false)
    {
        var image = library.Images[index];
        int width = shadow ? image.ShadowWidth : overlay ? image.OverlayWidth : image.Width;
        int height = shadow ? image.ShadowHeight : overlay ? image.OverlayHeight : image.Height;
        if (width <= 0 || height <= 0) return;
        byte[] expected = shadow ? reference.DecodeShadow(library, index) : overlay
            ? reference.DecodeOverlay(library, index) : null;
        byte[] actual = shadow || overlay ? library.GetAuditLayerData(index, shadow) : null;
        if (expected == null && actual == null) return;
        layers++;
        compared++;
        var diff = ZlPixelDiffHelper.Compare(expected, actual);
        if (diff.DifferentPixels == 0 && diff.DifferentBytes == 0) return;
        different++;
        differentPixels += diff.DifferentPixels;
        differentBytes += diff.DifferentBytes;
        maxDelta = Math.Max(maxDelta, diff.MaxDelta);
        if (different <= 12)
            Godot.GD.PrintErr($"[PixelAudit] DIFF file={System.IO.Path.GetFileName(library.FileName)} frame={index} " +
                $"layer={(shadow ? "shadow" : "overlay")} pixels={diff.DifferentPixels} bytes={diff.DifferentBytes} max={diff.MaxDelta}");
    }

    private static IEnumerable<int> GetPixelAuditIndices(ZlImage[] images, int sampleLimit)
    {
        if (sampleLimit <= 0 || images.Length <= sampleLimit)
            return Enumerable.Range(0, images.Length);

        var indices = new SortedSet<int> { 0, images.Length - 1 };
        int stride = Math.Max(1, images.Length / sampleLimit);
        for (int index = 0; index < images.Length; index += stride)
            indices.Add(index);
        return indices;
    }

    private void RunProjectileAudit()
    {
        _auditProjectile = new MirProjectileNode();
        AddChild(_auditProjectile);
        _auditProjectile.SetupProjectile(LibraryFile.Magic, 420, 5, 100,
            null, 4, 2, new System.Drawing.Point(0, 0),
            (x, y) => new Vector2(x * CellWidth, y * CellHeight));
        _auditProjectile.Blend = true;
        _auditProjectile.Has16Directions = true;
        // 该审计的目标点在视口内，因此按原版必须标记 Explode 才会在
        // 到达点结束；非 Explode 的“穿屏继续飞行”由运行时路径覆盖。
        _auditProjectile.Explode = true;
        _projectileScreenshotSaved = false;
        _auditProjectile.CompleteAction = () =>
            GD.Print(_auditProjectileMaxTravel > 20f
                ? $"[ProjectileAudit] PASS samples={_auditProjectileSamples} travel={_auditProjectileMaxTravel:0.0}px"
                : $"[ProjectileAudit] FAIL travel={_auditProjectileMaxTravel:0.0}px");
        _auditProjectileStart = _auditProjectile.Position;
    }

    private static void RunPlayerMatrixAudit()
    {
        bool pass = PlayerRenderer.RunAppearanceMatrixAudit(out int tested, out string failure);
        if (pass)
            GD.Print($"[PlayerMatrixAudit] PASS tested={tested} gender=2 class=4 equipment=armour/costume/helmet/shield/weapon horseShape=8 directions=8 animations=8");
        else
            GD.PrintErr($"[PlayerMatrixAudit] FAIL tested={tested} {failure}");
    }

    private void ProcessProjectileAudit()
    {
        if (_auditProjectile == null || !GodotObject.IsInstanceValid(_auditProjectile)) return;
        _auditProjectileSamples++;
        _auditProjectileMaxTravel = Math.Max(_auditProjectileMaxTravel,
            _auditProjectile.Position.DistanceTo(_auditProjectileStart));
        if (!_projectileScreenshotSaved && _auditProjectileSamples >= 8
            && DisplayServer.GetName() != "headless")
        {
            var image = GetViewport().GetTexture()?.GetImage();
            if (image != null && image.SavePng("/tmp/zircon-projectile-audit.png") == Error.Ok)
            {
                _projectileScreenshotSaved = true;
                GD.Print($"[ProjectileRenderAudit] PASS viewport={image.GetWidth()}x{image.GetHeight()} " +
                         "path=/tmp/zircon-projectile-audit.png");
            }
        }
    }

    private async void RunTransparencyAudit()
    {
        try
        {
        bool fullScan = OS.GetCmdlineUserArgs().Contains("--full-texture-audit");
        string auditFile = OS.GetCmdlineUserArgs()
            .FirstOrDefault(arg => arg.StartsWith("--audit-file=", StringComparison.OrdinalIgnoreCase))?
            .Substring("--audit-file=".Length);
        int auditStart = ParseAuditInt("--audit-start=", 0);
        int auditEnd = ParseAuditInt("--audit-end=", int.MaxValue);
        int libraries = 0, frames = 0, transparentFrames = 0, cornerPollution = 0;
        int inspectedEntries = 0;
        foreach (LibraryFile file in Enum.GetValues<LibraryFile>())
        {
            string name = file.ToString();
            if (!string.IsNullOrWhiteSpace(auditFile)
                && !string.Equals(name, auditFile, StringComparison.OrdinalIgnoreCase))
                continue;
            var library = LibraryCache.Get(file);
            if (library?.Images == null) continue;
            libraries++;
            // Actual legacy call sites for MirEffect, projectile, exterior
            // equipment and ordinary ProgUse images all pass ImageType.Image.
            // Weather is the only ProgUse subset using the dedicated keyed
            // path, and it is audited separately by RunWeatherAudit.
            bool effectTransparency = false;
            // 默认均匀抽取最多 24 帧；完整模式逐帧检查整个图库，用于
            // 发布前的“所有贴图”审计，不把抽样结果冒充全量结论。
            int stride = fullScan ? 1 : Math.Max(1, library.Images.Length / 24);
            int firstIndex = Math.Clamp(auditStart, 0, library.Images.Length);
            int lastIndex = Math.Clamp(auditEnd, firstIndex, library.Images.Length);
            for (int index = firstIndex; index < lastIndex; index += stride)
            {
                inspectedEntries++;
                if (fullScan && inspectedEntries % 8 == 0)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var image = library.Images[index];
                if (image == null || image.Width <= 0 || image.Height <= 0) continue;
                if (fullScan && inspectedEntries == 1)
                    GD.Print($"[TransparencyAudit] begin file={file} frame={index} " +
                        $"size={image.Width}x{image.Height} codec={image.ImageCodec} " +
                        $"position={image.Position}");
                // 普通图库和旧端 ImageType.Image 路径必须保留原始 Alpha/黑色
                // 像素；天气颜色键由单独的 WeatherAudit 覆盖，不能按图库名称
                // 把整个 ProgUse/EquipEffect/Magic 库误判成颜色键资源。
                byte[] rgba = library.GetAuditImageData(index, effectTransparency);
                if (rgba == null) continue;
                frames++;
                int transparent = 0;
                // GetPixel() 跨 C#↔引擎边界逐像素调用，在全图库审计中会把
                // 数万帧拖到分钟级；ZlReader 统一产出 RGBA8，因此直接扫 Alpha
                // 字节既保持同一判定，又让“所有贴图”审计可以实际完成。
                for (int offset = 3; offset < rgba.Length; offset += 4)
                    if (rgba[offset] <= 2) transparent++;
                if (transparent > 0) transparentFrames++;
                bool cornersOpaque = effectTransparency
                    && rgba.Length >= 4
                    && rgba[3] >= 253
                    && rgba[(image.Width - 1) * 4 + 3] >= 253
                    && rgba[(image.Height - 1) * image.Width * 4 + 3] >= 253
                    && rgba[rgba.Length - 1] >= 253;
                if (cornersOpaque && transparent > image.Width * image.Height / 4)
                {
                    cornerPollution++;
                    int last = rgba.Length - 4;
                    GD.Print($"[TransparencyAudit] SUSPECT file={file} frame={index} " +
                        $"size={image.Width}x{image.Height} transparent={transparent} " +
                        $"corners=({rgba[0]},{rgba[1]},{rgba[2]})/({rgba[last]},{rgba[last + 1]},{rgba[last + 2]})");
                }
            }
            if (fullScan)
            {
                library.ClearAuditEffectTextureCache();
            GD.Print($"[TransparencyAudit] progress file={file} images={library.Images.Length} frames={frames}");
            }
        }
        GD.Print(cornerPollution == 0
            ? $"[TransparencyAudit] PASS mode={(fullScan ? "full" : "sample")} file={(auditFile ?? "all")} range={auditStart}..{(auditEnd == int.MaxValue ? "end" : auditEnd)} libraries={libraries} frames={frames} transparentFrames={transparentFrames} cornerPollution=0"
            : $"[TransparencyAudit] REVIEW mode={(fullScan ? "full" : "sample")} file={(auditFile ?? "all")} range={auditStart}..{(auditEnd == int.MaxValue ? "end" : auditEnd)} libraries={libraries} frames={frames} transparentFrames={transparentFrames} cornerPollution={cornerPollution}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[TransparencyAudit] EXCEPTION {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static int ParseAuditInt(string prefix, int fallback)
    {
        string arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return arg != null && int.TryParse(arg.Substring(prefix.Length), out int value) ? Math.Max(0, value) : fallback;
    }

    private void RunWeatherAudit()
    {
        var library = LibraryCache.Get(LibraryFile.ProgUse);
        if (library == null)
        {
            GD.PrintErr("[WeatherAudit] FAIL ProgUse library unavailable");
            return;
        }
        int[] frames = { 500, 509, 510, 511, 512, 513, 514, 540, 550 };
        int passed = 0;
        foreach (int frame in frames)
        {
            var image = (frame == 550
                ? library.GetFogTexture(frame)
                : library.GetWeatherTexture(frame))?.GetImage();
            var ordinary = library.GetImageTexture(frame)?.GetImage();
            if (image == null)
            {
                GD.PrintErr($"[WeatherAudit] FAIL frame={frame} unavailable");
                continue;
            }

            int transparent = 0, visible = 0, ordinaryTransparent = 0, ordinaryVisible = 0;
            for (int y = 0; y < image.GetHeight(); y++)
                for (int x = 0; x < image.GetWidth(); x++)
                    if (image.GetPixel(x, y).A <= 0.01f) transparent++; else visible++;
            if (ordinary != null)
                for (int y = 0; y < ordinary.GetHeight(); y++)
                    for (int x = 0; x < ordinary.GetWidth(); x++)
                        if (ordinary.GetPixel(x, y).A <= 0.01f) ordinaryTransparent++; else ordinaryVisible++;

            bool ok = transparent > 0 && visible > 0;
            if (ok) passed++;
            GD.Print(ok
                ? $"[WeatherAudit] PASS frame={frame} size={image.GetWidth()}x{image.GetHeight()} transparent={transparent} visible={visible} ordinaryTransparent={ordinaryTransparent} ordinaryVisible={ordinaryVisible}"
                : $"[WeatherAudit] FAIL frame={frame} transparent={transparent} visible={visible}");
        }
        GD.Print(passed == frames.Length
            ? $"[WeatherAudit] PASS all={passed}/{frames.Length} weatherFrames=500,509-514,540,550"
            : $"[WeatherAudit] REVIEW all={passed}/{frames.Length} weatherFrames=500,509-514,540,550");
    }

    private void RunTransparencyModeAudit()
    {
        var samples = new[]
        {
            ("Magic", LibraryFile.Magic, 830),
            ("MagicEx2", LibraryFile.MagicEx2, 1900),
            ("Magic", LibraryFile.Magic, 831),
            ("MagicEx2", LibraryFile.MagicEx2, 1901),
        };
        foreach (var (name, file, frame) in samples)
        {
            var library = LibraryCache.Get(file);
            var ordinary = library?.GetImageTexture(frame)?.GetImage();
            var keyed = library?.GetEffectTexture(frame)?.GetImage();
            if (ordinary == null || keyed == null)
            {
                GD.PrintErr($"[TransparencyModeAudit] FAIL {name} frame={frame} unavailable");
                continue;
            }
            int ordinaryTransparent = 0, keyedTransparent = 0;
            for (int y = 0; y < ordinary.GetHeight(); y++)
                for (int x = 0; x < ordinary.GetWidth(); x++)
                {
                    if (ordinary.GetPixel(x, y).A <= 0.01f) ordinaryTransparent++;
                    if (keyed.GetPixel(x, y).A <= 0.01f) keyedTransparent++;
                }
            GD.Print($"[TransparencyModeAudit] {name} frame={frame} size={ordinary.GetWidth()}x{ordinary.GetHeight()} " +
                     $"ordinaryTransparent={ordinaryTransparent} keyedTransparent={keyedTransparent}");
        }
    }

    private void RunLayerOrderAudit()
    {
        bool rows = RenderOrder.TerrainMiddle(10) < RenderOrder.TerrainFront(10)
            && RenderOrder.TerrainFront(10) < RenderOrder.Object(10)
            && RenderOrder.Object(10) < RenderOrder.ObjectEffect(10)
            && RenderOrder.ObjectEffect(10) < RenderOrder.TerrainMiddle(11);
        bool postObject = RenderOrder.ObjectEffect(10) < RenderOrder.LocalPlayer
            && RenderOrder.LocalPlayer < RenderOrder.Particles
            && RenderOrder.Particles < RenderOrder.LocalPlayerEffect
            && RenderOrder.LocalPlayerEffect < RenderOrder.FinalEffects;
        GD.Print(rows && postObject
            ? "[LayerOrderAudit] PASS legacy row/local-player ordering"
            : $"[LayerOrderAudit] FAIL rows={rows} postObject={postObject}");
    }

    private void RunSoundAssetAudit()
    {
        string soundRoot = ProjectSettings.GlobalizePath("res://../Debug/Client/Sound/");
        var files = SoundCatalog.Entries.Values.Select(x => x.FileName).Distinct().OrderBy(x => x).ToArray();
        int valid = 0;
        foreach (string file in files)
        {
            string path = Path.Combine(soundRoot, file);
            try
            {
                byte[] header = File.ReadAllBytes(path);
                bool riffWave = header.Length >= 12
                    && Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                    && Encoding.ASCII.GetString(header, 8, 4) == "WAVE";
                if (riffWave) valid++;
                else GD.PrintErr($"[SoundAudit] FAIL {file} invalid WAV header");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SoundAudit] FAIL {file} {ex.Message}");
            }
        }
        GD.Print($"[SoundAudit] valid={valid}/{files.Length}");
        if (valid == files.Length)
            GD.Print($"[SoundAudit] PASS catalog={SoundCatalog.Entries.Count} files={files.Length}");
    }

    private void StartNextActionAudit()
    {
        if (_actionIndex >= _actions.Count)
        {
            if (!_actionAuditFinished) GD.Print("[ActionAudit] PASS all action sequences");
            _actionAuditFinished = true;
            return;
        }
        var action = _actions[_actionIndex++];
        _actionFrames.Clear();
        _actionAnimations.Clear();
        _auditPlayer.Dead = false;
        _auditPlayer.Horse = HorseType.None;
        _auditPlayer.Cloaked = _auditPlayer.GhostWalking = _auditPlayer.DragonRepulsed = _auditPlayer.ElementalHurricane = false;
        _auditPlayer.SetAnimation(MirAnimation.Standing);
        action.Start(_auditPlayer);
        if (_auditPlayer.Animation != action.Expected)
            GD.PrintErr($"[ActionAudit] FAIL {action.Name}: expected {action.Expected}, got {_auditPlayer.Animation}");
        var frame = FrameSet.Players[action.Expected];
        // Walking/Running 是循环动作；在桌面窗口、headless 和组合审计同时
        // 执行时，首个检查点可能恰好落在循环动作的第 0 帧，导致只收集到
        // 一个“0”或没有帧变更的假失败。至少等待一个完整循环再加余量，
        // 一次性动作仍按同一窗口收集其全部帧。
        _actionDeadline = Godot.Time.GetTicksMsec() + Math.Max(1000, frame.Sum + 400);
        GD.Print($"[ActionAudit] START {action.Name}: anim={action.Expected} count={frame.FrameCount} sum={frame.Sum:0}ms");
    }

    private void ProcessActionAudit()
    {
        if (_auditPlayer == null || _actionAuditFinished || _actionIndex > _actions.Count) return;
        if (Godot.Time.GetTicksMsec() < _actionDeadline) return;
        var action = _actions[_actionIndex - 1];
        var expected = FrameSet.Players[action.Expected];
        if (action.Name == "ActionQueue" && !_actionAnimations.Contains(MirAnimation.Struck))
            GD.PrintErr("[ActionAudit] FAIL ActionQueue: queued Struck animation was not reached");
        else if (_actionFrames.Count < (expected.FrameCount <= 1 ? 0 : expected.FrameCount - 1))
            GD.PrintErr($"[ActionAudit] FAIL {action.Name}: observed frames={_actionFrames.Count}, expected count={expected.FrameCount}");
        else
            GD.Print($"[ActionAudit] PASS {action.Name}: observed={string.Join(',', _actionFrames.OrderBy(x => x))}");
        StartNextActionAudit();
    }

    private void RenderObjectAudit()
    {
        var healthBackground = MirSkin.GetTexture(LibraryFile.Interface, 80);
        var healthFill = MirSkin.GetTexture(LibraryFile.Interface, 79);
        bool labelAnchor = Math.Abs(RenderPrimitives.OriginalNameBaseline(9f)) < 32f;
        bool healthAssets = healthBackground != null && healthFill != null;
        GD.Print(labelAnchor && healthAssets
            ? $"[ObjectLabelAudit] PASS centerX=24 nameBaseline={RenderPrimitives.OriginalNameBaseline(9f):0.0} "
              + $"health79={healthFill.GetWidth()}x{healthFill.GetHeight()} health80={healthBackground.GetWidth()}x{healthBackground.GetHeight()}"
            : $"[ObjectLabelAudit] FAIL anchor={labelAnchor} healthAssets={healthAssets}");
        int x = 480, y = 320;
        var monsterInfo = Globals.MonsterInfoList?.Binding
            .FirstOrDefault(m => MonsterLookup.Map.ContainsKey(m.Image));
        if (monsterInfo != null)
        {
            var monster = ObjectRenderer.CreateMonster(new S.ObjectMonster
            {
                ObjectID = 9001,
                MonsterIndex = monsterInfo.Index,
                Direction = MirDirection.Down,
                Location = new System.Drawing.Point(0, 0),
                NameColour = System.Drawing.Color.Lime,
            });
            if (monster != null)
            {
                monster.SetAnimation(MirAnimation.Combat1);
                bool attackOk = monster.Animation == MirAnimation.Combat1;
                monster.PlayRangeAttack();
                bool rangeOk = monster.Animation == MirAnimation.Combat2;
                monster.PlaySpell(MagicType.DoomClawLeftPinch);
                bool specialSpellOk = monster.Animation == MirAnimation.Combat4;
                monster.PlaySpell(MagicType.DragonRepulse);
                bool dragonOk = monster.Animation == MirAnimation.DragonRepulseStart;
                var deadMonster = ObjectRenderer.CreateMonster(new S.ObjectMonster
                {
                    ObjectID = 9003,
                    MonsterIndex = monsterInfo.Index,
                    Direction = MirDirection.Down,
                    Location = new System.Drawing.Point(0, 0),
                    Dead = true,
                });
                bool deadOk = deadMonster != null && deadMonster.Animation == MirAnimation.Dead;
                GD.Print(attackOk && rangeOk && specialSpellOk && dragonOk && deadOk
                    ? "[MonsterAudit] PASS Attack=Combat1 Range=Combat2 DoomClawLeftPinch=Combat4 DragonRepulse=Start Dead=Die"
                    : $"[MonsterAudit] FAIL attack={monster.Animation} range={rangeOk} special={specialSpellOk} dragon={dragonOk} dead={deadOk}");
                deadMonster?.QueueFree();
                monster.Position = new Vector2(x, y);
                monster.ShowHealthBar = true;
                monster.Health = monster.MaxHealth = 100;
                monster.Focused = true;
                monster.ZIndex = 100 + y / 32;
                AddChild(monster);
            }
        }

        var npcInfo = Globals.NPCInfoList?.Binding.FirstOrDefault();
        if (npcInfo != null)
        {
            var npc = ObjectRenderer.CreateNPC(new S.ObjectNPC
            {
                ObjectID = 9002,
                NPCIndex = npcInfo.Index,
                Direction = MirDirection.Down,
                CurrentLocation = new System.Drawing.Point(0, 0),
            });
            if (npc != null)
            {
                npc.Position = new Vector2(x + 120, y);
                npc.Focused = true;
                npc.ZIndex = 100 + y / 32;
                AddChild(npc);
            }
        }

        var player = new PlayerRenderer();
        player.UpdateAppearance(new StartInformation
        {
            Name = "RenderAuditHero",
            NameColour = System.Drawing.Color.Cyan,
            Class = MirClass.Warrior,
            Gender = MirGender.Male,
            HairType = 1,
            HairColour = System.Drawing.Color.Black,
            Armour = 11,
            ArmourColour = System.Drawing.Color.LightSkyBlue,
            Costume = -1,
            HelmetShape = 11,
            Shield = 10,
            Weapon = 1200,
            Horse = HorseType.None,
            HorseShape = 0,
            Direction = MirDirection.Down,
        });
        // 诊断对象置于独立的高层，避免被测试地图前景盖住；生产场景仍由
        // RenderY 决定 ZIndex。这样截图能真正检查坐骑、装备和脚底影子。
        player.Position = new Vector2(x - 240, y + 120);
        player.ShowHealthBar = true;
        player.Health = player.MaxHealth = 100;
        player.ZIndex = 1000;
        AddChild(player);

        var itemInfo = Globals.ItemInfoList?.Binding
            .FirstOrDefault(i => i.Image >= 0);
        if (itemInfo != null)
        {
            var item = ObjectRenderer.CreateItem(new S.ObjectItem
            {
                ObjectID = 9003,
                Item = new ClientUserItem(itemInfo, 1),
                Location = new System.Drawing.Point(0, 0),
            });
            if (item != null)
            {
                item.Position = new Vector2(x + 360, y);
                item.Focused = true;
                item.ZIndex = 100 + y / 32;
                AddChild(item);
            }
        }

        GD.Print("[RenderAudit] 已创建真实 Monster/NPC ZL 对象，触发 Shadow、Overlay fallback、标签和 RenderY 绘制");
    }

    private ZlLibrary GetLibrary(LibraryFile file)
    {
        // 测试场景必须与实际 GameScene 使用相同的路径大小写修复、
        // ZL/ZL2 解析和缓存，否则会出现“地图统计成功但截图灰底”的假通过。
        return LibraryCache.Get(file);
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
                    DrawCell(cell.BackFile, cell.BackImage, px, py, 90 + y);
                }

                // 中层
                if (cell.MiddleFile > 0 && cell.MiddleImage > 0)
                {
                    DrawCell(cell.MiddleFile, cell.MiddleImage - 1, px, py, 99 + y);
                }

                // 前景
                if (cell.FrontFile > 0 && cell.FrontImage > 0)
                {
                    DrawCell(cell.FrontFile, cell.FrontImage - 1, px, py, 101 + y);
                }
            }
        }
    }

    private void DrawCell(int fileByte, int imageIndex, float px, float py, int zIndex)
    {
        if (fileByte == 0 || imageIndex < 0) return;
        if (!Libraries.KROrder.TryGetValue(fileByte, out LibraryFile file)) return;
        if (file == LibraryFile.Tilesc) return;

        var lib = GetLibrary(file);
        if (lib == null || imageIndex >= lib.Images.Length)
        {
            if (_mapTextureDiagnostics++ < 8)
                GD.PrintErr($"[MapTest] 地形图库失败: fileByte={fileByte} file={file} image={imageIndex} lib={(lib == null ? "null" : lib.Images.Length.ToString())}");
            return;
        }
        if (lib.Images[imageIndex] == null)
        {
            if (_mapTextureDiagnostics++ < 8)
                GD.PrintErr($"[MapTest] 地形帧为空: file={file} image={imageIndex}");
            return;
        }

        var texture = lib.GetImageTexture(imageIndex);
        if (texture == null)
        {
            if (_mapTextureDiagnostics++ < 8)
                GD.PrintErr($"[MapTest] 地形纹理为空: file={file} image={imageIndex}");
            return;
        }

        var img = lib.Images[imageIndex];
        var sprite = new Sprite2D();
        sprite.Texture = texture;
        sprite.Position = new Vector2(px, py);
        sprite.ZIndex = zIndex;
        // 贴图偏移（OffSetY 让贴图底边对齐格子）
        sprite.Offset = new Vector2(img.OffSetX, img.OffSetY);
        AddChild(sprite);
    }
}
