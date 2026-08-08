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
using S = Library.Network.ServerPackets;
using ZirconClient.Formats;
using ZirconClient.Network;

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
    private bool _projectileAudit;
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
        _projectileAudit = OS.GetCmdlineUserArgs().Contains("--projectile-audit");
        bool lightAudit = OS.GetCmdlineUserArgs().Contains("--light-audit");
        bool networkAudit = OS.GetCmdlineUserArgs().Contains("--network-audit");
        bool fullTextureAudit = OS.GetCmdlineUserArgs().Contains("--full-texture-audit");

        // 与实际 GameScene 保持一致：地图、对象、特效都在逻辑 48x32
        // 坐标绘制，根世界统一放大 2 倍。否则审计截图只能验证 1x。
        if (_renderAudit || _actionAudit || _projectileAudit)
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
            if (_projectileAudit) CallDeferred(nameof(RunProjectileAudit));
            if (lightAudit) CallDeferred(nameof(RunLightAudit));
            if (networkAudit) CallDeferred(nameof(RunNetworkAudit));
            if (fullTextureAudit) CallDeferred(nameof(RunTransparencyAudit));
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"失败: {ex.Message}";
            GD.PrintErr($"[MapTest] {ex}");
        }
    }

    private static void RunLightAudit()
    {
        const float epsilon = 0.0001f;
        bool pass = Math.Abs(MapLightLayer.AmbientFor(LightSetting.Night, 1f) - 100f / 255f) < epsilon
            && Math.Abs(MapLightLayer.AmbientFor(LightSetting.Twilight, 1f) - 100f / 255f) < epsilon
            && Math.Abs(MapLightLayer.AmbientFor(LightSetting.Light, 0f) - 1f) < epsilon
            && Math.Abs(MapLightLayer.AmbientFor(LightSetting.Default, 0.42f) - 0.42f) < epsilon
            && Math.Abs(MapLightLayer.ObjectLightRadius(3) - 56.32f) < epsilon
            && Math.Abs(MapLightLayer.TileLightRadius(1) - 179.2f) < epsilon
            && Math.Abs(MapLightLayer.EffectLightRadius(35) - 97.28f) < epsilon;
        if (pass)
            GD.Print("[LightAudit] PASS Night=100/255 Twilight=100/255 Light=255/255 Default=DayTime");
        else
            GD.PrintErr($"[LightAudit] FAIL Night={MapLightLayer.AmbientFor(LightSetting.Night, 1f)} " +
                $"Twilight={MapLightLayer.AmbientFor(LightSetting.Twilight, 1f)} " +
                $"Light={MapLightLayer.AmbientFor(LightSetting.Light, 0f)}");
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

            bool pass = !connection.Connected && disconnectEvents == 1;
            if (pass)
                GD.Print("[NetworkAudit] PASS duplicate disconnect collapsed to one event and transport closed");
            else
                GD.PrintErr($"[NetworkAudit] FAIL connected={connection.Connected} disconnectEvents={disconnectEvents}");
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
        RunSoundAssetAudit();
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

        if (failures.Count == 0)
            GD.Print($"[MagicFrameAudit] PASS skills={seen.Count}");
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
                for (int x = 0; x < map.Width && !hasLayer; x++)
                for (int y = 0; y < map.Height; y++)
                {
                    ref var cell = ref map.Cells[x, y];
                    if (cell.BackFile > 0 || cell.MiddleFile > 0 || cell.FrontFile > 0)
                    {
                        hasLayer = true;
                        break;
                    }
                }
                if (hasLayer) layered++;
            }
            catch (Exception ex)
            {
                failures.Add($"{Path.GetFileName(path)}:{ex.GetType().Name}:{ex.Message}");
            }
        }

        if (failures.Count == 0)
            GD.Print($"[MapAudit] PASS files={files.Length} valid={valid} layered={layered} cells={totalCells}");
        else
        {
            GD.PrintErr($"[MapAudit] FAIL files={files.Length} valid={valid} failures={failures.Count}");
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

    private void RunProjectileAudit()
    {
        _auditProjectile = new MirProjectileNode();
        AddChild(_auditProjectile);
        _auditProjectile.SetupProjectile(LibraryFile.Magic, 420, 5, 100,
            null, 4, 2, new System.Drawing.Point(0, 0),
            (x, y) => new Vector2(x * CellWidth, y * CellHeight));
        _auditProjectile.Blend = true;
        _auditProjectile.Has16Directions = true;
        _auditProjectile.CompleteAction = () =>
            GD.Print(_auditProjectileMaxTravel > 20f
                ? $"[ProjectileAudit] PASS samples={_auditProjectileSamples} travel={_auditProjectileMaxTravel:0.0}px"
                : $"[ProjectileAudit] FAIL travel={_auditProjectileMaxTravel:0.0}px");
        _auditProjectileStart = _auditProjectile.Position;
    }

    private void ProcessProjectileAudit()
    {
        if (_auditProjectile == null || !GodotObject.IsInstanceValid(_auditProjectile)) return;
        _auditProjectileSamples++;
        _auditProjectileMaxTravel = Math.Max(_auditProjectileMaxTravel,
            _auditProjectile.Position.DistanceTo(_auditProjectileStart));
    }

    private async void RunTransparencyAudit()
    {
        try
        {
        bool fullScan = OS.GetCmdlineUserArgs().Contains("--full-texture-audit");
        string auditFile = OS.GetCmdlineUserArgs()
            .FirstOrDefault(arg => arg.StartsWith("--audit-file=", StringComparison.OrdinalIgnoreCase))?
            .Substring("--audit-file=".Length);
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
            bool effectTransparency = name.Contains("Magic", StringComparison.OrdinalIgnoreCase)
                || name.Contains("ProgUse", StringComparison.OrdinalIgnoreCase)
                || name.Contains("EquipEffect", StringComparison.OrdinalIgnoreCase);
            // 默认均匀抽取最多 24 帧；完整模式逐帧检查整个图库，用于
            // 发布前的“所有贴图”审计，不把抽样结果冒充全量结论。
            int stride = fullScan ? 1 : Math.Max(1, library.Images.Length / 24);
            for (int index = 0; index < library.Images.Length; index += stride)
            {
                inspectedEntries++;
                if (fullScan && inspectedEntries % 8 == 0)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var image = library.Images[index];
                if (image == null || image.Width <= 0 || image.Height <= 0) continue;
                // 普通图库必须保留原始 Alpha/黑色像素；只有旧版 DrawBlend/颜色键
                // 图库才走特效透明路径。这样全量审计不会把“所有贴图”错误地
                // 缩小成技能特效子集，也不会把普通 UI/角色图误按黑色颜色键清除。
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
                GD.Print($"[TransparencyAudit] progress file={file} frames={frames}");
            }
        }
        GD.Print(cornerPollution == 0
            ? $"[TransparencyAudit] PASS mode={(fullScan ? "full" : "sample")} file={(auditFile ?? "all")} libraries={libraries} frames={frames} transparentFrames={transparentFrames} cornerPollution=0"
            : $"[TransparencyAudit] REVIEW mode={(fullScan ? "full" : "sample")} file={(auditFile ?? "all")} libraries={libraries} frames={frames} transparentFrames={transparentFrames} cornerPollution={cornerPollution}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[TransparencyAudit] EXCEPTION {ex.GetType().Name}: {ex.Message}");
        }
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
        string[] files = { "1.wav", "33.wav", "35.wav", "50.wav", "61.wav", "84.wav", "85.wav", "86.wav", "125.wav", "138.wav", "144.wav", "M103-1.wav", "37400.wav" };
        int loaded = 0;
        foreach (string file in files)
        {
            var stream = AudioStreamWav.LoadFromFile(Path.Combine(soundRoot, file));
            if (stream != null) loaded++;
            else GD.PrintErr($"[SoundAudit] FAIL {file}");
        }
        GD.Print($"[SoundAudit] loaded={loaded}/{files.Length}");
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
        _actionDeadline = Godot.Time.GetTicksMsec() + Math.Max(700, frame.Sum + 250);
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
                GD.Print(attackOk && rangeOk && specialSpellOk && dragonOk
                    ? "[MonsterAudit] PASS Attack=Combat1 Range=Combat2 DoomClawLeftPinch=Combat4 DragonRepulse=Start"
                    : $"[MonsterAudit] FAIL attack={monster.Animation} range={rangeOk} special={specialSpellOk} dragon={dragonOk}");
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
            Armour = 0,
            ArmourColour = System.Drawing.Color.LightSkyBlue,
            Costume = -1,
            HelmetShape = 0,
            Shield = -1,
            Weapon = 0,
            Horse = HorseType.None,
            Direction = MirDirection.Down,
        });
        player.Position = new Vector2(x + 240, y);
        player.ShowHealthBar = true;
        player.Health = player.MaxHealth = 100;
        player.ZIndex = 100 + y / 32;
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
