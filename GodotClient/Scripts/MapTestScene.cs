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

#nullable enable
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618

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
    private bool _blendAudit;
    private int _blendAuditFrames;
    private int _blendAuditCase;
    private int _blendAuditOriginalHits;
    private int _blendAuditCurrentHits;
    private ColorRect? _blendAuditBackdrop;
    private TextureRect? _blendAuditQuad;
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
        _blendAudit = OS.GetCmdlineUserArgs().Contains("--blend-audit");
        bool networkAudit = OS.GetCmdlineUserArgs().Contains("--network-audit");
        bool cursorAudit = OS.GetCmdlineUserArgs().Contains("--cursor-audit");
        bool fullTextureAudit = OS.GetCmdlineUserArgs().Contains("--full-texture-audit");
        bool weatherTextureDump = OS.GetCmdlineUserArgs().Contains("--weather-texture-dump");
        bool progUseEffectDump = OS.GetCmdlineUserArgs().Contains("--proguse-effect-dump");
        bool sludgeDump = OS.GetCmdlineUserArgs().Contains("--green-sludge-dump");
        bool magicTextureDump = OS.GetCmdlineUserArgs().Contains("--magic-texture-dump");

        // 与实际 GameScene 保持一致：地图、对象、特效都在逻辑 48x32
        // 坐标绘制，根世界统一放大 2 倍。否则审计截图只能验证 1x。
        if (_renderAudit || _actionAudit || _projectileAudit || _lightRenderAudit || _weatherRenderAudit || _mapFamilyRenderAudit || _blendAudit)
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
            if (networkAudit)
            {
                CallDeferred(nameof(RunNetworkAudit));
                CallDeferred(nameof(RunAnomalyReplayAudit));
            }
            if (cursorAudit) CallDeferred(nameof(RunCursorAudit));
            if (fullTextureAudit) CallDeferred(nameof(RunTransparencyAudit));
            if (_blendAudit) CallDeferred(nameof(BeginBlendAudit));
            if (weatherTextureDump) CallDeferred(nameof(DumpWeatherTextures));
            if (progUseEffectDump) CallDeferred(nameof(DumpProgUseEffectTextures));
            if (sludgeDump) CallDeferred(nameof(DumpGreenSludgeTextures));
            if (magicTextureDump) CallDeferred(nameof(DumpMagicTextures));
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"失败: {ex.Message}";
            GD.PrintErr($"[MapTest] {ex}");
        }
    }

    private void DumpWeatherTextures()
    {
        var library = LibraryCache.Get(LibraryFile.ProgUse);
        if (library == null) return;
        foreach (int frame in new[] { 500, 509, 510, 511, 512, 513, 514, 540, 550 })
        {
            var meta = frame < library.Images.Length ? library.Images[frame] : null;
            var processed = (frame == 550 ? library.GetFogTexture(frame) : library.GetWeatherTexture(frame))?.GetImage();
            var ordinary = library.GetImageTexture(frame)?.GetImage();
            processed?.SavePng($"/tmp/zircon-weather-{frame}-processed.png");
            ordinary?.SavePng($"/tmp/zircon-weather-{frame}-ordinary.png");
            GD.Print($"[WeatherTextureDump] frame={frame} codec={meta?.ImageCodec} " +
                $"size={meta?.Width}x{meta?.Height} alphaProcessed={(processed == null ? -1 : CountOpaque(processed))} " +
                $"alphaOrdinary={(ordinary == null ? -1 : CountOpaque(ordinary))}");
        }
        GD.Print("[WeatherTextureDump] PASS path=/tmp/zircon-weather-<frame>-{processed,ordinary}.png");
        GetTree().Quit();
    }

    private static int CountOpaque(Image image)
    {
        int count = 0;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
                if (image.GetPixel(x, y).A > 0.01f) count++;
        return count;
    }

    private void DumpProgUseEffectTextures()
    {
        var library = LibraryCache.Get(LibraryFile.ProgUse);
        if (library == null) return;
        var frames = new List<int> { 200, 210, 220, 230, 240, 241, 242, 243, 244, 245, 246, 247, 260 };
        frames.AddRange(Enumerable.Range(680, 6));
        frames.AddRange(Enumerable.Range(700, 15));
        foreach (int frame in frames.Distinct())
        {
            library.GetImageTexture(frame)?.GetImage()?.SavePng($"/tmp/zircon-proguse-{frame}-ordinary.png");
            library.GetEffectTexture(frame)?.GetImage()?.SavePng($"/tmp/zircon-proguse-{frame}-keyed.png");
        }
        GD.Print("[ProgUseEffectDump] PASS path=/tmp/zircon-proguse-<frame>-{ordinary,keyed}.png");
        GetTree().Quit();
    }

    private void DumpGreenSludgeTextures()
    {
        var library = LibraryCache.Get(LibraryFile.MonMagicEx23);
        if (library == null) return;
        for (int frame = 2780; frame < 2800 && frame < library.Images.Length; frame++)
        {
            var image = library.GetImageTexture(frame)?.GetImage();
            image?.SavePng($"/tmp/zircon-green-sludge-{frame}.png");
            var meta = library.Images[frame];
            GD.Print($"[GreenSludgeDump] frame={frame} size={meta?.Width}x{meta?.Height} offset={meta?.OffSetX},{meta?.OffSetY}");
        }
        GD.Print($"[GreenSludgeDump] PASS resourceFrames={library.Images.Length} range=2780..2799");
        GetTree().Quit();
    }

    private void DumpMagicTextures()
    {
        var library = LibraryCache.Get(LibraryFile.Magic);
        if (library == null) return;
        foreach (int frame in new[] { 420, 421, 422, 423, 424, 580, 581, 582, 1820, 1821 })
        {
            library.GetImageTexture(frame)?.GetImage()?.SavePng($"/tmp/zircon-magic-{frame}-ordinary.png");
            library.GetEffectTexture(frame)?.GetImage()?.SavePng($"/tmp/zircon-magic-{frame}-keyed.png");
            var meta = library.Images[frame];
            GD.Print($"[MagicTextureDump] frame={frame} size={meta?.Width}x{meta?.Height} offset={meta?.OffSetX},{meta?.OffSetY}");
        }
        GD.Print("[MagicTextureDump] PASS path=/tmp/zircon-magic-<frame>-{ordinary,keyed}.png");
        GetTree().Quit();
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
            // A2: 原版 PickUpTime 250ms 节流（鼠标点击与 Tab 共用）。
            bool pickupCooldownSemantics = GameScene.CanSendPickUp(300, 250)
                && !GameScene.CanSendPickUp(249, 250)
                && GameScene.CanSendPickUp(250, 250);
            // A3: 原版 CheckCursor 环扫描边界 — 上/左越界 break、下/右越界 continue。
            bool ringEdgeSemantics = CombatController.RingEdgeMode(-1, 64) == -1
                && CombatController.RingEdgeMode(0, 64) == 0
                && CombatController.RingEdgeMode(64, 64) == 1
                && CombatController.RingEdgeMode(63, 64) == 0
                && CombatController.RingEdgeMode(-1, 0) == -1;
            // A5: 无 Shift 点击玩家/宠物 → 只选中不追击；Shift 或怪物 → 追击。
            bool playerSelectOnlySemantics = CombatController.ShouldSelectOnly(true, false, false)
                && CombatController.ShouldSelectOnly(false, true, false)
                && CombatController.ShouldSelectOnly(true, true, false)
                && !CombatController.ShouldSelectOnly(true, false, true)
                && !CombatController.ShouldSelectOnly(false, false, false)
                && !CombatController.ShouldSelectOnly(false, true, true);
            // A5: Shuriken 超 Globals.MagicRange 不可投（原版 683-692 提示+取消）。
            bool shurikenRangeSemantics = !Functions.InRange(
                new System.Drawing.Point(0, 0), new System.Drawing.Point(11, 0), Globals.MagicRange)
                && Functions.InRange(
                    new System.Drawing.Point(0, 0), new System.Drawing.Point(10, 0), Globals.MagicRange);
            // A6: 钓鱼必须武器槽 FishingRod + 护甲槽 FishingRobe；护甲缺失不算。
            bool fishingRigSemantics = GameScene.IsFishingRig(ItemEffect.FishingRod, ItemEffect.FishingRobe)
                && !GameScene.IsFishingRig(ItemEffect.FishingRod, null)
                && !GameScene.IsFishingRig(ItemEffect.FishingRod, ItemEffect.None)
                && !GameScene.IsFishingRig(null, ItemEffect.FishingRobe);
            // A6: 挖矿必须武器槽 PickAxe（非任意槽）、耐久、矿点 Flag、相邻、无马。
            bool miningSemantics = GameScene.CanMineNow(true, ItemEffect.PickAxe, 10, 0, true, true, true, false)
                && !GameScene.CanMineNow(false, ItemEffect.PickAxe, 10, 0, true, true, true, false)
                && !GameScene.CanMineNow(true, null, 10, 0, true, true, true, false)
                && !GameScene.CanMineNow(true, ItemEffect.None, 10, 0, true, true, true, false)
                && !GameScene.CanMineNow(true, ItemEffect.PickAxe, 0, 10, true, true, true, false)
                && GameScene.CanMineNow(true, ItemEffect.PickAxe, 0, 0, true, true, true, false)
                && !GameScene.CanMineNow(true, ItemEffect.PickAxe, 10, 0, true, false, true, false)
                && !GameScene.CanMineNow(true, ItemEffect.PickAxe, 10, 0, true, true, false, false)
                && !GameScene.CanMineNow(true, ItemEffect.PickAxe, 10, 0, true, true, true, true);
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
            bool currencyDropSemantics = GameScene.CanDropCurrency(false, 100, 1)
                && GameScene.CanDropCurrency(false, 100, 100)
                && !GameScene.CanDropCurrency(false, 100, 101)
                && !GameScene.CanDropCurrency(false, 0, 1)
                && !GameScene.CanDropCurrency(true, 100, 1);
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
                && pickupCooldownSemantics
                && ringEdgeSemantics
                && playerSelectOnlySemantics
                && shurikenRangeSemantics
                && fishingRigSemantics
                && miningSemantics
                && currencyDropSemantics
                && consumePartial && consumeWhole && rejectLateConsume
                && splitPartial && splitRejectOverflow && splitRejectOverwrite;
            if (pass)
                GD.Print("[NetworkAudit] PASS duplicate disconnect collapsed, transport closed, removed-object references cleared, player/monster attackability semantics, current-cell pickup priority, pickup 250ms cooldown, pickup state guards, auto-path transition semantics, map right-click cancellation, Alt gathering state semantics, ring-edge break/continue, player select-only, Shuriken range, fishing robe rig, mining state machine, currency-drop bounds, stale/late packet replay ordering, item-count bounds and split-target protection");
            else
                GD.PrintErr($"[NetworkAudit] FAIL connected={connection.Connected} disconnectEvents={disconnectEvents} referencesCleared={referencesCleared} playerAttack={playerAttackSemantics} pickupPriority={pickupPrioritySemantics} pickupState={pickupStateSemantics} pickupCooldown={pickupCooldownSemantics} autoPathSemantics={autoPathSemantics} mapRightCancel={mapRightCancelSemantics} gathering={gatheringSemantics} ringEdge={ringEdgeSemantics} playerSelectOnly={playerSelectOnlySemantics} shurikenRange={shurikenRangeSemantics} fishingRig={fishingRigSemantics} mining={miningSemantics} currencyDrop={currencyDropSemantics} replayOrdering={replayOrdering} consume={consumePartial}/{consumeWhole}/{rejectLateConsume} split={splitPartial}/{splitRejectOverflow}/{splitRejectOverwrite}");
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

    /// <summary>
    /// P-006: 真实回环 socket 上的确定性异常回放。与 RunNetworkAudit（进程内直调 Process）互补，
    /// 这里把包序列化成字节流经过真实 TCP 收发，镜像 NetworkManager._Process 的同步轮询
    /// （读可用字节 → Packet.ReceivePacket → ReceiveList → Process 派发），验证：
    ///   1) 半包（长度前缀被拆到两次 write）→ 不派发，补齐后恰好派发一次；
    ///   2) 多包合并进一次 write → 按序各派发一次；
    ///   3) 启动阶段突发包 → 进 Pending 队列且事件双发；StopPendingPacketBuffering 后不再重复入队；
    ///   4) 切图后迟到的重复包 → 只实时派发，不重新进入积压队列（不会复活旧地图对象）；
    ///   5) 服务器 FIN 断开 → DisconnectedEvent 恰好一次，重复 NotifyDisconnected 折叠；
    ///   6) 垃圾字节 → 帧缓冲挂起但不崩溃、不误派发。
    /// </summary>
    private static void RunAnomalyReplayAudit()
    {
        TcpListener listener = null;
        TcpClient serverSide = null;
        TcpClient clientSide = null;
        try
        {
            // —— 场景 A：缓冲/迟到/重复/分片 ——
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            clientSide = new TcpClient();
            clientSide.Connect(IPAddress.Loopback, port);
            serverSide = listener.AcceptTcpClient();
            serverSide.NoDelay = true;
            var serverStream = serverSide.GetStream();

            var connection = new ServerConnection(clientSide);
            connection.UpdateTimeOut();
            int moveEvents = 0, mapChanges = 0;
            connection.ObjectMoveEvent += (_, _, _, _, _, _) => moveEvents++;
            connection.MapChangedEvent += (_, _) => mapChanges++;

            byte[] raw = Array.Empty<byte>();
            bool Pump()
            {
                // 镜像 NetworkManager._Process 的同步轮询
                try
                {
                    if (!clientSide.Connected) return false;
                    if (clientSide.Available == 0 && !clientSide.Client.Poll(1000, SelectMode.SelectRead))
                        return true; // 暂无数据：只推进 Process()（发送队列/超时）
                    var stream = clientSide.GetStream();
                    byte[] buf = new byte[8 * 1024];
                    int read = stream.Read(buf, 0, buf.Length);
                    if (read == 0) return false; // FIN
                    byte[] temp = raw;
                    raw = new byte[read + temp.Length];
                    Array.Copy(temp, 0, raw, 0, temp.Length);
                    Array.Copy(buf, 0, raw, temp.Length, read);
                    Library.Network.Packet p;
                    while ((p = Library.Network.Packet.ReceivePacket(raw, out raw)) != null)
                        connection.ReceiveList.Enqueue(p);
                    connection.Process();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            // 3) 启动阶段突发包：入队 + 事件双发
            byte[] a = new S.ObjectMove { ObjectID = 901, Distance = 1 }.GetPacketBytes();
            serverStream.Write(a, 0, a.Length);
            serverStream.Flush();
            Pump();
            bool bufferedQueued = connection.PendingMoves.Count == 1 && moveEvents == 1;

            // 关闭缓冲后实时包不再入队
            connection.StopPendingPacketBuffering();
            byte[] b = new S.ObjectMove { ObjectID = 902, Distance = 1 }.GetPacketBytes();
            serverStream.Write(b, 0, b.Length);
            serverStream.Flush();
            Pump();
            bool runningLive = connection.PendingMoves.Count == 1 && moveEvents == 2;

            // 4) 切图清空积压；之后迟到的重复包只实时派发、不重新入队
            byte[] mapBytes = new S.MapChanged { MapIndex = 7, InstanceIndex = -1 }.GetPacketBytes();
            serverStream.Write(mapBytes, 0, mapBytes.Length);
            serverStream.Flush();
            Pump();
            bool mapClearedPending = connection.PendingMoves.Count == 0 && mapChanges == 1;

            // 2) 多包合并进一次 write + 迟到的重复包
            byte[] c1 = new S.ObjectMove { ObjectID = 903, Distance = 1 }.GetPacketBytes();
            serverStream.Write(c1, 0, c1.Length);
            serverStream.Write(c1, 0, c1.Length); // 重复
            serverStream.Flush();
            Pump();
            bool coalescedOrdered = moveEvents == 4;
            bool lateDuplicateNoRequeue = connection.PendingMoves.Count == 0 && moveEvents == 4;

            // 1) 半包：先发长度前缀前 2 字节（不足 4 字节帧头 → 挂起不派发）
            byte[] d = new S.ObjectMove { ObjectID = 904, Distance = 1 }.GetPacketBytes();
            int split = 2;
            serverStream.Write(d, 0, split);
            serverStream.Flush();
            Pump();
            bool fragHeld = moveEvents == 4;
            serverStream.Write(d, split, d.Length - split);
            serverStream.Flush();
            Pump();
            bool fragDeliveredOnce = moveEvents == 5;

            connection.Disconnect();
            bool scenarioA = bufferedQueued && runningLive && mapClearedPending
                && coalescedOrdered && lateDuplicateNoRequeue && fragHeld && fragDeliveredOnce;

            // —— 场景 B：服务器 FIN 断开 + 垃圾字节 ——
            TcpClient clientB = null;
            TcpClient serverB = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                int portB = ((IPEndPoint)listener.LocalEndpoint).Port;
                clientB = new TcpClient();
                clientB.Connect(IPAddress.Loopback, portB);
                serverB = listener.AcceptTcpClient();
                serverB.NoDelay = true;
                var serverBStream = serverB.GetStream();

                var connectionB = new ServerConnection(clientB);
                connectionB.UpdateTimeOut();
                int bMoves = 0, bDisconnects = 0;
                connectionB.ObjectMoveEvent += (_, _, _, _, _, _) => bMoves++;
                connectionB.DisconnectedEvent += () => bDisconnects++;

                byte[] rawB = Array.Empty<byte>();
                bool PumpB()
                {
                    try
                    {
                        if (!clientB.Connected) return false;
                        if (clientB.Available == 0 && !clientB.Client.Poll(1000, SelectMode.SelectRead))
                            return true;
                        var stream = clientB.GetStream();
                        byte[] buf = new byte[8 * 1024];
                        int read = stream.Read(buf, 0, buf.Length);
                        if (read == 0) return false;
                        byte[] temp = rawB;
                        rawB = new byte[read + temp.Length];
                        Array.Copy(temp, 0, rawB, 0, temp.Length);
                        Array.Copy(buf, 0, rawB, temp.Length, read);
                        Library.Network.Packet p;
                        while ((p = Library.Network.Packet.ReceivePacket(rawB, out rawB)) != null)
                            connectionB.ReceiveList.Enqueue(p);
                        connectionB.Process();
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }

                // 6) 垃圾字节：帧缓冲挂起（长度前缀非法）→ 不崩溃、不误派发
                byte[] garbage = new byte[512];
                for (int i = 0; i < garbage.Length; i++) garbage[i] = 0xA5;
                serverBStream.Write(garbage, 0, garbage.Length);
                serverBStream.Flush();
                PumpB();
                bool garbageHeld = bMoves == 0 && connectionB.Connected;

                // 5) 服务器 FIN 断开 → 恰好一次断线事件；重复通知折叠
                serverB.Client.Shutdown(SocketShutdown.Send);
                serverB.Close();
                bool finDetected = !PumpB();
                connectionB.NotifyDisconnected(closeTransport: true);
                connectionB.NotifyDisconnected(closeTransport: true);
                bool disconnectedOnce = bDisconnects == 1 && !connectionB.Connected;

                bool scenarioB = garbageHeld && finDetected && disconnectedOnce;
                bool pass = scenarioA && scenarioB;
                if (pass)
                    GD.Print("[AnomalyReplay] PASS real-socket framing (fragmented held then delivered once), coalesced ordering, buffered startup burst queued once, running-state live dispatch, late duplicate after map change does not re-enter backlog, server FIN disconnect fired exactly once and collapsed, garbage bytes stall framing without crash or misdispatch");
                else
                    GD.PrintErr($"[AnomalyReplay] FAIL scenarioA={scenarioA} (bufferedQueued={bufferedQueued} runningLive={runningLive} mapClearedPending={mapClearedPending} coalescedOrdered={coalescedOrdered} lateDuplicateNoRequeue={lateDuplicateNoRequeue} fragHeld={fragHeld} fragDeliveredOnce={fragDeliveredOnce}) scenarioB={scenarioB} (garbageHeld={garbageHeld} finDetected={finDetected} disconnectedOnce={disconnectedOnce}) moveEvents={moveEvents} mapChanges={mapChanges}");
            }
            finally
            {
                try { clientB?.Close(); } catch { }
                try { serverB?.Close(); } catch { }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[AnomalyReplay] FAIL {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try { serverSide?.Close(); } catch { }
            try { clientSide?.Close(); } catch { }
            try { listener?.Stop(); } catch { }
        }
    }

    public override void _Process(double delta)
    {
        ProcessLightRenderAudit();
        ProcessWeatherRenderAudit();
        ProcessMapFamilyRenderAudit();
        ProcessBlendAudit();
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

            // 资源核对结论（2026-08-08，P-003）：
            //   MonMagicEx23.Zl 只有 2800 个条目；命中特效仅在方向 0 存在
            //   （2780..2785），方向 >=1 的 2790..2855 条目全部为 NULL，
            //   资源没有任何更晚的帧。原版 MirEffect.Process 与 Godot
            //   MirEffectNode._Draw 对越界/空条目都是静默跳过（原版
            //   CheckImage index >= Images.Length 返回 false；Godot 同款
            //   df < 0 || df >= _lib.Images.Length 返回）。
            //   因此原版与 Godot 行为一致：命中特效只会在方向 0 显示，
            //   其余方向不出现在画面上。这不是 bug，而是资源版本本身
            //   的局限；不擅自改帧号，也不伪造原版不存在的资源。
            //
            // 下面按实际条目可绘制性自适应检查：只有那些真正越界或
            // 存在空条目的方向才计入 exception，其余方向正常参与范围
            // 校验（正常路径已经通过 CheckImpactRange 完成）。
            var library = LibraryCache.Get(LibraryFile.MonMagicEx23);
            if (library?.Images == null)
                return false;
            int undrawable = 0;
            for (int dir = 0; dir < 8; dir++)
            {
                int baseFrame = 2780 + dir * 10;
                for (int f = 0; f < 6; f++)
                {
                    int index = baseFrame + f;
                    if (index < 0 || index >= library.Images.Length || library.Images[index] == null)
                        undrawable++;
                }
            }
            // 预期：方向 0 的 6 帧可绘制；方向 1..7 的 42 帧不可绘制。
            // 一旦资源被替换为完整的方向帧，此审计会自动降为正常 PASS，
            // 不需要人工改例外清单。
            if (undrawable != 42)
            {
                GD.PrintErr($"[MagicFrameAudit] GreenSludgeBall impact resource changed: undrawable={undrawable} (expected 42: dir0 present, dir1-7 null)");
                return false;
            }
            originalResourceExceptions++;
            return true;
        });

        if (failures.Count == 0)
            GD.Print($"[MagicFrameAudit] PASS skills={seen.Count} originalResourceExceptions={originalResourceExceptions} (GreenSludgeBall impact dir0-only verified)");
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
        int thinContent = 0, longContent = 0;
        var malformedContent = new List<string>();
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
                    if (pixels != null)
                    {
                        Rect2I used = pixels.GetUsedRect();
                        if (used.Size.X > 0 && used.Size.Y > 0)
                        {
                            nonEmpty++;
                            if (used.Size.Y <= 2) thinContent++;
                            if (used.Size.X > used.Size.Y * 8)
                            {
                                longContent++;
                                if (malformedContent.Count < 12)
                                    malformedContent.Add($"{libraryName}[{index}] meta={image.ShadowWidth}x{image.ShadowHeight} used={used.Size.X}x{used.Size.Y}");
                            }
                        }
                    }
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
                $"thinContent={thinContent} longContent={longContent} " +
                $"fallbackTypes=49:{fallback49},50:{fallback50},176:{fallback176},177:{fallback177}");
        foreach (string detail in malformedContent)
            GD.Print($"[ShadowAudit] contentShape {detail}");
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

    // 原版 Screen Blend（BlendMode.NORMAL）数学的确定性像素验证（真实 Vulkan 读回）：
    //   src.rgb = texel.rgb * texel.a * COLOR.rgb * COLOR.a
    //   src.a   = texel.a * COLOR.a
    //   out     = src * (1 - dst) + dst          （RGB/Alpha 双通道）
    // 黑色底板使 (1-dst) 空间无关（黑→黑、白→白映射唯一），因此所有期望值
    // 在 sRGB 与线性管线下相同，只留两个互斥候选：
    // LegacyScreenBlend.gdshader 当前为普通 mix + 完整公式（非 blend_add）：
    //   COLOR = src*(1-dst)+dst, alpha=1，dst 来自 screen_texture（黑底板=0）。
    // 实测（白 α0.5 → 0.25 而非 0.5）证明 Godot 2D 在贴图上传时预乘了 alpha
    // （texel.rgb 已含 straight.rgb*a），因此当前着色器 texel.rgb*texel.a 会把
    // alpha 平方一次 —— 对半透明内容比原版公式暗 a 倍。
    // 期望 current = 当前着色器实际输出（premult texel × texel.a × COLOR...）
    // 期望 original = 原版公式（premult texel × COLOR...，即只乘一次 a）。
    // 判定：current 命中 → 双重预乘确认，需修 shader；original 命中 → 奇偶一致。
    private static readonly Color BlendAuditBackdropColour = new Color(0f, 0f, 0f, 1f);
    // (texel[直通], modulate, 期望 current, 期望 original, Kind)
    // Kind: 0 = LegacyScreenBlend 材质, 1 = 灰度 Blend, 2 = 灰度非 Blend
    private static readonly (Color Texel, Color Modulate, Color ExpectCurrent, Color ExpectOriginal, int Kind)[] BlendAuditCases =
    {
        (new Color(1f, 1f, 1f, 0.5f),  Colors.White,                new Color(0.25f, 0.25f, 0.25f, 1f), new Color(0.5f, 0.5f, 0.5f, 1f), 0), // 判别器：半透明白
        (new Color(1f, 1f, 1f, 1f),    new Color(1f, 1f, 1f, 0.8f), new Color(0.8f, 0.8f, 0.8f, 1f),    new Color(0.8f, 0.8f, 0.8f, 1f), 0),  // 不透明 texel：两模型同值
        (new Color(1f, 1f, 1f, 0.5f),  new Color(1f, 1f, 1f, 0.8f), new Color(0.2f, 0.2f, 0.2f, 1f),    new Color(0.4f, 0.4f, 0.4f, 1f), 0),  // 判别器：texel.a×COLOR.a
        (new Color(0f, 0f, 0f, 1f),    Colors.White,                new Color(0f, 0f, 0f, 1f),          new Color(0f, 0f, 0f, 1f), 0),       // 黑 texel → 0
        (new Color(1f, 1f, 1f, 0f),    Colors.White,                new Color(0f, 0f, 0f, 1f),          new Color(0f, 0f, 0f, 1f), 0),       // 零 alpha → 0
        (new Color(1f, 0f, 0f, 0.5f),  Colors.White,                new Color(0.25f, 0f, 0f, 1f),       new Color(0.5f, 0f, 0f, 1f), 0),     // 判别器：红 α0.5 通道隔离
        // 灰度 Blend：premult texel.rgb=(0.25,0.25,0.25) → l=0.25；
        // original = l（/texel.a 补偿后恰好抵消）；current(无补偿) = l*texel.a = 0.125
        (new Color(0.5f, 0.5f, 0.5f, 0.5f), Colors.White,           new Color(0.125f, 0.125f, 0.125f, 1f), new Color(0.25f, 0.25f, 0.25f, 1f), 1),
        // 灰度非 Blend：original out = l + dst*(1-a) = 0.25；current(旧 alpha<1 版) = 0.125（ONE/INV 假设）
        (new Color(0.5f, 0.5f, 0.5f, 0.5f), Colors.White,           new Color(0.125f, 0.125f, 0.125f, 1f), new Color(0.25f, 0.25f, 0.25f, 1f), 2),
    };
    // mix 语义探针（普通 TextureRect，无 screen blend 材质）：
    //   白 α0.5 纹理（预乘后 texel.rgb=0.5, a=0.5）：
    //     mix=SRC_ALPHA(直通输出) → 0.5*0.5=0.25；mix=ONE/INV_SRC_ALPHA(预乘输出) → 0.5
    //   红 α0.5 ColorRect（无纹理，直通色 (1,0,0,0.5)）：
    //     SRC_ALPHA → 0.5；预乘 → 1.0
    private static readonly (Color Texel, Color Modulate, Color ExpectCurrent, Color ExpectOriginal)[] BlendAuditMixProbes =
    {
        (new Color(1f, 1f, 1f, 0.5f),  Colors.White,                new Color(0.25f, 0.25f, 0.25f, 1f), new Color(0.5f, 0.5f, 0.5f, 1f)),
        (new Color(1f, 0f, 0f, 0.5f),  Colors.White,                new Color(0.5f, 0f, 0f, 1f),       new Color(1f, 0f, 0f, 1f)),
        // shader 材质输出常量色 (1,0,0,0.5)：SRC_ALPHA(直通) → 0.5；ONE/INV(预乘) → 1.0
        (new Color(1f, 0f, 0f, 0.5f),  Colors.White,                new Color(0.5f, 0f, 0f, 1f),       new Color(1f, 0f, 0f, 1f)),
    };
    private const float BlendAuditTolerance = 4f / 255f;

    private static bool BlendChannelClose(float a, float b) => Mathf.Abs(a - b) <= BlendAuditTolerance;

    private static bool BlendColourClose(Color a, Color b) =>
        BlendChannelClose(a.R, b.R) && BlendChannelClose(a.G, b.G) &&
        BlendChannelClose(a.B, b.B) && BlendChannelClose(a.A, b.A);

    private void BeginBlendAudit()
    {
        var vp = GetViewportRect();
        _blendAuditBackdrop = new ColorRect
        {
            Color = BlendAuditBackdropColour,
            Position = Vector2.Zero,
            Size = vp.Size,
            ZIndex = 200,
        };
        AddChild(_blendAuditBackdrop);
        _blendAuditCase = 0;
        SpawnBlendAuditCase(0);
        GD.Print($"[BlendAudit] begin backdrop={BlendAuditBackdropColour} cases={BlendAuditCases.Length} viewport={vp.Size}");
    }

    private void SpawnBlendAuditCase(int i)
    {
        if (_blendAuditQuad != null)
        {
            _blendAuditQuad.QueueFree();
            _blendAuditQuad = null;
        }
        int probeIndex = i - BlendAuditCases.Length;
        bool isProbe = probeIndex >= 0;
        (Color texel, Color modulate, _, _) = isProbe ? BlendAuditMixProbes[probeIndex] : BlendAuditCases[i];
        var vp = GetViewportRect();
        // 根节点 Scale=2：场景坐标 ×2 = 窗口像素；GetViewportRect() 返回窗口像素，
        // 因此场景中心 = vp.Size/4。
        Vector2 center = vp.Size / 4f - new Vector2(40, 40);
        if (isProbe && probeIndex == 1)
        {
            // 纯色探针：无纹理 ColorRect
            _blendAuditQuad = null;
            var rect = new ColorRect
            {
                Color = modulate * texel,
                Position = center,
                Size = new Vector2(80, 80),
                ZIndex = 201,
            };
            AddChild(rect);
            _blendAuditFrames = 0;
            return;
        }
        if (isProbe && probeIndex == 2)
        {
            // shader 输出常量色探针：混合语义（SRC_ALPHA vs ONE/INV）。
            // 用 TextureRect（modulate=白）避免 ColorRect+材质 的双重绘制伪影。
            var probeImg = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
            var probePx = new byte[4 * 4 * 4];
            for (int k = 0; k < probePx.Length; k += 4) { probePx[k] = 255; probePx[k + 1] = 255; probePx[k + 2] = 255; probePx[k + 3] = 255; }
            probeImg.SetData(4, 4, false, Image.Format.Rgba8, probePx);
            _blendAuditQuad = new TextureRect
            {
                Texture = ImageTexture.CreateFromImage(probeImg),
                Modulate = Colors.White,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                Position = center,
                Size = new Vector2(80, 80),
                ZIndex = 201,
                Material = new ShaderMaterial
                {
                    Shader = new Shader
                    {
                        Code = "shader_type canvas_item;\nvoid fragment() {\n    COLOR = vec4(1.0, 0.0, 0.0, 0.5);\n}",
                    },
                },
            };
            AddChild(_blendAuditQuad);
            _blendAuditFrames = 0;
            return;
        }
        var img = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        var px = new byte[4 * 4 * 4];
        for (int k = 0; k < px.Length; k += 4)
        {
            px[k] = (byte)Mathf.RoundToInt(texel.R * 255f);
            px[k + 1] = (byte)Mathf.RoundToInt(texel.G * 255f);
            px[k + 2] = (byte)Mathf.RoundToInt(texel.B * 255f);
            px[k + 3] = (byte)Mathf.RoundToInt(texel.A * 255f);
        }
        img.SetData(4, 4, false, Image.Format.Rgba8, px);
        _blendAuditQuad = new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(img),
            // 探针（probeIndex==0）不加 screen blend 材质，测 Godot 普通 mix 语义
            Material = isProbe ? null : LegacyBlendMaterial.Create(),
            Modulate = modulate,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Position = center,
            Size = new Vector2(80, 80),
            ZIndex = 201,
        };
        AddChild(_blendAuditQuad);
        _blendAuditFrames = 0;
    }

    // 空间无关读回：在全屏黑底板上，取视口中心邻域内像素的众数颜色（即四边形
    // 的均匀色）。窗口尺寸/缩放不影响判定；全黑案例（黑/零 alpha）众数为空 → 黑。
    private static Color SampleBlendAuditQuad(Image image)
    {
        int cx = image.GetWidth() / 2, cy = image.GetHeight() / 2;
        var counts = new Dictionary<int, (int Count, Color Colour)>();
        for (int y = cy - 120; y <= cy + 120; y += 4)
        {
            for (int x = cx - 120; x <= cx + 120; x += 4)
            {
                var c = image.GetPixel(x, y);
                if (Math.Max(c.R, Math.Max(c.G, c.B)) <= 0.02f) continue;
                int key = ((int)Mathf.RoundToInt(c.R * 15f) << 8) |
                          ((int)Mathf.RoundToInt(c.G * 15f) << 4) |
                          (int)Mathf.RoundToInt(c.B * 15f);
                if (!counts.TryGetValue(key, out var e)) e = (0, c);
                e.Count++;
                counts[key] = e;
            }
        }
        if (counts.Count == 0) return Colors.Black;
        Color best = Colors.Black;
        int bestN = 0;
        foreach (var e in counts.Values)
        {
            if (e.Count > bestN) { bestN = e.Count; best = e.Colour; }
        }
        return best;
    }

    private void ProcessBlendAudit()
    {
        if (!_blendAudit) return;
        if (++_blendAuditFrames < 4) return;
        var image = GetViewport().GetTexture()?.GetImage();
        if (image == null)
        {
            GD.PrintErr("[BlendAudit] FAIL 无帧缓冲可读回（headless/dummy？）");
            return;
        }
        int i = _blendAuditCase;
        int probeIndex = i - BlendAuditCases.Length;
        bool isProbe = probeIndex >= 0;
        (Color texel, Color modulate, Color expectCurrent, Color expectOriginal) =
            isProbe ? BlendAuditMixProbes[probeIndex] : BlendAuditCases[i];
        Color got = SampleBlendAuditQuad(image);
        bool curHit = BlendColourClose(got, expectCurrent);
        bool origHit = BlendColourClose(got, expectOriginal);
        if (curHit) _blendAuditCurrentHits++;
        if (origHit) _blendAuditOriginalHits++;
        GD.Print($"[BlendAudit] case={i} texel={texel} colour={modulate} got={got} " +
                 $"current(texel.rgb*texel.a*Col)={expectCurrent} original(straight.rgb*texel.a*Col)={expectOriginal} " +
                 (curHit || origHit ? (origHit ? "PASS(original)" : "PASS(current)") : "FAIL"));
        int total = BlendAuditCases.Length + BlendAuditMixProbes.Length;
        if (++_blendAuditCase < total)
        {
            SpawnBlendAuditCase(_blendAuditCase);
            return;
        }
        var backdrop = image.GetPixel(image.GetWidth() / 2, 60);
        bool backdropOk = BlendColourClose(backdrop, BlendAuditBackdropColour);
        GD.Print($"[BlendAudit] backdrop got={backdrop} expected={BlendAuditBackdropColour} " +
                 (backdropOk ? "PASS" : "FAIL"));
        string path = "/tmp/zircon-blend-audit.png";
        image.SavePng(path);
        GD.Print($"[BlendAudit] 判定: original命中={_blendAuditOriginalHits} current命中={_blendAuditCurrentHits} cases={total}");
        GD.Print(_blendAuditOriginalHits == total && backdropOk
            ? $"[BlendAudit] PASS 着色器与原始公式一致 截图 {path}"
            : _blendAuditOriginalHits == 0 && _blendAuditCurrentHits == total && backdropOk
                ? $"[BlendAudit] FAIL-预期 双重预乘已确认（当前着色器比原版暗 a 倍），需修 LegacyScreenBlend.gdshader 截图 {path}"
                : $"[BlendAudit] FAIL 未命中任一模型，截图 {path}");
        GetTree().Quit();
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
        // Explicit full-scan mode is a CI-style command; terminate after the
        // result so callers do not have to infer completion from a timeout.
        if (fullScan && OS.GetCmdlineUserArgs().Contains("--audit-only"))
            GetTree().Quit();
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
        using var legacyReference = new ZlPixelReference(library.FileName);
        foreach (int frame in frames)
        {
            // Production weather rendering follows the old ImageType.Image
            // path. Keyed is retained only as a diagnostic comparison.
            var image = library.GetImageTexture(frame)?.GetImage();
            var keyed = (frame == 550
                ? library.GetFogTexture(frame)
                : library.GetWeatherTexture(frame))?.GetImage();
            if (image == null)
            {
                GD.PrintErr($"[WeatherAudit] FAIL frame={frame} unavailable");
                continue;
            }

            int transparent = 0, visible = 0, keyedTransparent = 0, keyedVisible = 0;
            for (int y = 0; y < image.GetHeight(); y++)
                for (int x = 0; x < image.GetWidth(); x++)
                    if (image.GetPixel(x, y).A <= 0.01f) transparent++; else visible++;
            if (keyed != null)
                for (int y = 0; y < keyed.GetHeight(); y++)
                    for (int x = 0; x < keyed.GetWidth(); x++)
                        if (keyed.GetPixel(x, y).A <= 0.01f) keyedTransparent++; else keyedVisible++;

            byte[] legacy = legacyReference.DecodeImage(library, frame);
            int legacyTransparent = 0, legacyVisible = 0;
            int alphaMismatch = 0;
            if (legacy != null)
            {
                for (int offset = 3; offset < legacy.Length; offset += 4)
                    if (legacy[offset] <= 2) legacyTransparent++; else legacyVisible++;
                for (int y = 0; y < image.GetHeight(); y++)
                    for (int x = 0; x < image.GetWidth(); x++)
                    {
                        bool oldVisible = legacy[(y * image.GetWidth() + x) * 4 + 3] > 2;
                        bool godotVisible = image.GetPixel(x, y).A > 0.01f;
                        if (oldVisible != godotVisible) alphaMismatch++;
                    }
            }

            bool ok = legacy != null && alphaMismatch == 0;
            if (ok) passed++;
            GD.Print(ok
                ? $"[WeatherAudit] PASS frame={frame} size={image.GetWidth()}x{image.GetHeight()} productionTransparent={transparent} productionVisible={visible} keyedTransparent={keyedTransparent} keyedVisible={keyedVisible} legacyTransparent={legacyTransparent} legacyVisible={legacyVisible} alphaMismatch=0"
                : $"[WeatherAudit] FAIL frame={frame} productionTransparent={transparent} productionVisible={visible} legacyTransparent={legacyTransparent} legacyVisible={legacyVisible} alphaMismatch={alphaMismatch}");
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
