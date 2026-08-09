using System.Drawing;
using System.Net.Sockets;
using Library;
using Library.Network;
using Library.SystemModels;
using C = Library.Network.ClientPackets;
using G = Library.Network.GeneralPackets;
using S = Library.Network.ServerPackets;

namespace Zircon.BotRunner;

public enum BotStatus { Created, Connecting, LoggingIn, Starting, Running, Failed, Stopped }

public sealed class BotAgent
{
    private readonly BotConfig _config;
    private readonly Random _random;
    private readonly string _email;
    private readonly string _password;
    private BotConnection _connection;
    private DateTime _nextMove;
    private DateTime _nextChat;
    private DateTime _nextAttack;
    private DateTime _nextPotion;
    private Point _patrolTarget;
    private DateTime _arrivedPauseUntil;
    private Point _fieldAnchor;
    private Point _homeAnchor;
    private DateTime _nextFieldTrip;
    private DateTime _fieldTripEnd;
    private bool _fieldPathToField;
    private bool _fieldPathHome;
    private DateTime _nextTownCast;
    private DateTime _crossMapStuckSince = DateTime.MinValue;
    private DateTime _nextCrossMapDiag;
    private DateTime _nextSupplyDiag;
    // 跨图 ItemUse 回城卷的追踪: 用卷后一段时间仍非家图 = 卷传送无效
    // (BindPoint 被职业安全区/红区改写, 如刺客巢穴 459), 停止烧卷并重登。
    private DateTime _portalUseAt = DateTime.MinValue;
    private int? _portalUseSlot;
    private uint _targetMonsterId;
    private DateTime _nextTargetScan;
    private DateTime _nextGroupAction;
    private DateTime _nextTorchAction;
    private DateTime _nextRepairAction;
    private DateTime _nextQuestAction;
    private DateTime _nextSupplyAction;
    private DateTime _nextSellAction;
    private bool _supplyPurchasePending;
    private DateTime _supplyInteractionUntil = DateTime.MinValue;
    private bool _npcCallPending;
    private bool _repairCallPending;
    // StartGame 已发出但迟迟未收到 S.StartGame 响应(服务器 Spawn 卡死等)时的显式失败。
    private static readonly TimeSpan StartResponseTimeout = TimeSpan.FromSeconds(30);
    private DateTime _startRequestedAt = DateTime.MinValue;
    // 看到卖卷 NPC 时记录其位置(MapIndex → NPCInfo.Index → Point), 跨图兜底
    // 移动用(视野外也能朝商店走); 竞技场等地图 AutoPath 无路线时必须靠它。
    private readonly Dictionary<int, Dictionary<int, Point>> _knownSupplyNpcLocations = new();
    private int _shopPurchases;
    private int _shopSales;
    private DateTime _nextHarvest;
    private DateTime _nextInventorySort;
    private DateTime _nextSupport;
    private DateTime _nextResourceAction;
    private DateTime _resourceTripEnd;
    private bool _resourcePathToMine;
    private bool _resourcePathHome;
    private bool _resourceSwapPending;
    private bool _groupInviteAttempted;
    private DateTime _nextTradeAction;
    private bool _tradeRequestSent;
    private bool _tradeActive;
    private bool _tradePathRequested;
    private bool _tradeAutoPathAllowed;
    private bool _tradeFacingPrimed;
    private DateTime _nextGuildAction;
    private bool _starterGuildAttempted;
    private DateTime _nextMountAction;
    private DateTime _mountStarted;
    private DateTime _nextContainerAction;
    private int _containerSlot = -1;
    private DateTime _nextFishingAction;
    private bool _fishingActive;
    private Point _fishingPoint;
    private DateTime _nextInstanceAction;
    private DateTime _nextPvpAction;
    private DateTime _pvpRoundEnd;
    private Point _pvpStagingPoint;
    private int _pvpActions;
    private DateTime _nextProfessionAction;
    private DateTime _nextActivityReport;
    private int _moveActions;
    private int _attackActions;
    private int _magicActions;
    private int _targetSelections;
    private int _combatActions;
    private int _pickupRequests;
    private int _itemsGainedEvents;
    private const int ResourceMapIndex = 136;
    private uint _npcObjectId;
    private bool _autoPathActive;
    private DateTime _pullbackStuckSince = DateTime.MinValue;
    private int _crossMapFailCount;
    private DateTime _crossMapGraceUntil = DateTime.MinValue;
    private readonly int _index;
    private BotMap _map;
    private string _mapFile = string.Empty;
    private int _reconnectAttempt;

    public string Name { get; }
    public BotStatus Status { get; private set; } = BotStatus.Created;
    public BotWorld World { get; } = new();

    public BotAgent(int index, BotConfig config)
    {
        _config = config;
        _index = index;
        _random = new Random(1000 + index * 7919);
        Name = $"Bot{index:00}";
        _email = $"{config.AccountPrefix}{index:00}@bot.local";
        _password = config.Password;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _reconnectAttempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            Status = BotStatus.Connecting;
            try
            {
                var tcp = new TcpClient { NoDelay = true };
                await tcp.ConnectAsync(_config.Host, _config.Port, cancellationToken);
                byte[] clientHash = !string.IsNullOrWhiteSpace(_config.ClientHashPath) && File.Exists(_config.ClientHashPath)
                    ? System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(_config.ClientHashPath))
                    : Array.Empty<byte>();
                _connection = new BotConnection(tcp, clientHash, _config.VerboseNetworkLogging);
                _connection.PacketReceived += OnPacket;
                _connection.ConnectionError += (_, ex) =>
                {
                    Console.WriteLine($"[{Name}] connection lost: {DescribeException(ex)}");
                };
                Status = BotStatus.LoggingIn;

                while (!cancellationToken.IsCancellationRequested && _connection.Connected && Status != BotStatus.Failed)
                {
                    _connection.Process();
                    Tick();
                    await Task.Delay(Math.Max(50, _config.TickMilliseconds), cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] connection attempt failed: {DescribeException(ex)}");
            }
            finally
            {
                if (_connection?.Connected == true)
                    _connection.TrySendDisconnect(new G.Disconnect { Reason = DisconnectReason.Unknown });
            }

            if (cancellationToken.IsCancellationRequested || Status == BotStatus.Failed) break;
            _reconnectAttempt++;
            int delaySeconds = Math.Min(30, 2 + _reconnectAttempt * 2);
            Console.WriteLine($"[{Name}] reconnecting in {delaySeconds}s");
            try { await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken); }
            catch (OperationCanceledException) { break; }
        }

        Status = Status == BotStatus.Failed ? Status : BotStatus.Stopped;
        Console.WriteLine($"[{Name}] stopped ({Status})");
    }

    private void OnPacket(BotConnection _, Packet packet)
    {
        switch (packet)
        {
            case G.GoodVersion:
                _connection.Enqueue(new C.SelectLanguage { Language = "CHINESE" });
                _connection.Enqueue(new C.Login { EMailAddress = _email, Password = _password, CheckSum = string.Empty });
                break;
            case S.Login login:
                if (login.Result != LoginResult.Success || login.Characters == null || login.Characters.Count == 0)
                {
                    // 服务器重启后旧会话的断连检测有延迟, 快速重连会撞上
                    // AlreadyLoggedIn。等服务器踢掉残留会话再重试, 不算致命失败。
                    if (login.Result == LoginResult.AlreadyLoggedIn)
                    {
                        Status = BotStatus.Connecting;
                        _connection.TryDisconnect();
                        Console.WriteLine($"[{Name}] login already logged in, wait for stale session");
                        break;
                    }
                    Fail($"login failed: {login.Result} {login.Message}"); break;
                }
                _connection.Enqueue(new C.StartGame { CharacterIndex = login.Characters[0].CharacterIndex });
                Status = BotStatus.Starting;
                _startRequestedAt = DateTime.UtcNow + StartResponseTimeout;
                break;
            case S.StartGame start:
                if (start.Result != StartGameResult.Success) { Fail($"start failed: {start.Result} {start.Message}"); break; }
                World.Apply(start);
                _reconnectAttempt = 0;
                // Keep PvE bots peaceful toward strangers while allowing the
                // server-managed companion to move and attack normally.
                _connection.Enqueue(new C.ChangeAttackMode { Mode = _config.EnableBotPvP ? AttackMode.All : AttackMode.Group });
                _connection.Enqueue(new C.ChangePetMode { Mode = PetMode.Both });
                Status = BotStatus.Running;
                ScheduleNextActions();
                Console.WriteLine($"[{Name}] online {World.MapIndex}:{World.Location} Lv.{World.Level} {start.StartInformation?.Class} pvp={_config.EnableBotPvP} nextPvp={_nextPvpAction:HH:mm:ss}");
                break;
            case S.MapChanged p: World.Apply(p); break;
            case S.SafeZoneChanged p: World.Apply(p); break;
            case S.CurrencyChanged p: World.Apply(p); break;
            case S.UserLocation p: World.Apply(p); break;
            case S.ObjectMove p: World.Apply(p); break;
            case S.ObjectTurn p: World.Apply(p); break;
            case S.ObjectMount p: World.Apply(p); break;
            case S.ObjectFishing p when p.ObjectID == World.SelfObjectId:
                HandleFishingState(p);
                break;
            case S.ObjectMonster p:
                World.Apply(p);
                if (p.CompanionObject != null && p.PetOwner?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true)
                    Console.WriteLine($"[{Name}] companion: {p.CompanionObject.Name}");
                break;
            case S.ObjectPlayer p: World.Apply(p); break;
            case S.DataObjectHealthMana p: World.Apply(p); break;
            case S.DataObjectMaxHealthMana p: World.Apply(p); break;
            case S.ObjectNPC p: World.Apply(p); break;
            case S.ObjectItem p: World.Apply(p); break;
            case S.ObjectRemove p: World.Apply(p); break;
            case S.HealthChanged p: World.Apply(p); break;
            case S.ManaChanged p: World.Apply(p); break;
            case S.StatsUpdate p: World.Apply(p); break;
            case S.ItemsGained p:
                World.Apply(p);
                if (p.Items is { Count: > 0 }) _itemsGainedEvents++;
                break;
            case S.ItemMove p:
                World.Apply(p);
                if (p.Success) _resourceSwapPending = false;
                break;
            case S.ItemSort p: World.Apply(p); break;
            case S.ItemDelete p: World.Apply(p); break;
            case S.ItemChanged p: World.Apply(p); break;
            case S.ItemDurability p: World.Apply(p); break;
            case S.GroupInvite p:
                _connection.Enqueue(new C.GroupResponse { Name = p.Name, Accept = true });
                break;
            case S.GroupRequest p:
                _connection.Enqueue(new C.GroupResponse { Name = p.Name, Accept = true });
                break;
            case S.GroupMember p: World.Apply(p); break;
            case S.GroupRemove p: World.Apply(p); break;
            case S.TradeRequest p when p.Name?.Equals("Bot01", StringComparison.OrdinalIgnoreCase) == true:
                Console.WriteLine($"[{Name}] trade: received request from {p.Name}");
                _connection.Enqueue(new C.TradeRequestResponse { Accept = true });
                break;
            case S.TradeOpen p:
                Console.WriteLine($"[{Name}] trade: opened with {p.Name}");
                _tradeActive = true;
                if (_index == 1)
                    _connection.Enqueue(new C.TradeAddGold { Gold = 1 });
                _connection.Enqueue(new C.TradeConfirm());
                break;
            case S.TradeClose:
                if (_tradeActive)
                    Console.WriteLine($"[{Name}] trade: closed");
                _tradeActive = false;
                _tradeRequestSent = false;
                _tradeFacingPrimed = false;
                _nextTradeAction = DateTime.UtcNow.AddMinutes(3 + _random.NextDouble() * 3);
                break;
            case S.TradeUnlock:
                _tradeActive = false;
                _tradeRequestSent = false;
                _tradeFacingPrimed = false;
                _nextTradeAction = DateTime.UtcNow.AddMinutes(2);
                break;
            case S.BundleOpen p:
                HandleBundleOpen(p);
                break;
            case S.BundleClose:
                _containerSlot = -1;
                _nextContainerAction = DateTime.UtcNow.AddMinutes(2);
                break;
            case S.JoinInstance p:
                Console.WriteLine($"[{Name}] instance: {(p.Success ? "joined" : $"rejected ({p.Result})")}");
                _nextInstanceAction = DateTime.UtcNow.AddMinutes(p.Success ? 10 : 3);
                break;
            case S.AutoPathChanged p:
                _autoPathActive = p.Routes?.Count > 0;
                if (_autoPathActive)
                    _connection.Enqueue(new C.AutoPathMoveStarted());
                break;
            case S.NPCResponse p: HandleNpcResponse(p); break;
            case S.ObjectDied p: World.Apply(p); break;
            case S.ObjectRevive p: World.Apply(p); break;
            case S.LevelChanged p: World.Apply(p); break;
            case S.Chat p when p.Text?.Length > 0:
                if (p.Text.Contains("无法找到自动寻路路线", StringComparison.Ordinal))
                {
                    _autoPathActive = false;
                    _resourcePathToMine = false;
                    _resourcePathHome = false;
                    _tradePathRequested = false;
                    _tradeAutoPathAllowed = false;
                    _tradeRequestSent = false;
                    _tradeFacingPrimed = false;
                    _fieldPathToField = false;
                    _fieldPathHome = false;
                    _fieldTripEnd = DateTime.MinValue;
                    _nextFieldTrip = DateTime.UtcNow.AddSeconds(300);
                    _nextTradeAction = DateTime.UtcNow.AddSeconds(20);
                    _nextResourceAction = DateTime.UtcNow.AddMinutes(2 + _random.NextDouble() * 3);
                }
                // Chat is still sent to the server, but echoing every nearby
                // player's line from all 20 clients hides the useful behavior
                // telemetry. Keep only system/error-like messages here.
                if (p.Text.StartsWith("你", StringComparison.Ordinal) || p.Text.Contains("无法", StringComparison.Ordinal))
                    Console.WriteLine($"[{Name}] chat: {p.Text}");
                break;
        }
    }

    private void Tick()
    {
        if (Status == BotStatus.Starting && DateTime.UtcNow >= _startRequestedAt)
        {
            // 服务器对 StartGame 无响应(如 Spawn 卡死/地图数据缺失), 不再无限挂起。
            Fail($"start timeout: no S.StartGame response in {StartResponseTimeout.TotalSeconds:0}s");
            _connection.TryDisconnect();
            return;
        }
        if (Status != BotStatus.Running) return;
        var now = DateTime.UtcNow;
        if (now >= _nextActivityReport)
        {
        Console.WriteLine($"[{Name}] active map={World.MapIndex} inst={World.InstanceIndex}:{World.Location} role={RoleName(_index)} class={World.Class} safe={World.InSafeZone} gold={World.Gold} move={_moveActions} attack={_attackActions} magic={_magicActions} pvp={_pvpActions} shop={_shopPurchases}/{_shopSales} pickup={_pickupRequests}/{_itemsGainedEvents} targets={_targetSelections} pets={OwnedSummonCount()}");
            foreach (var npc in World.Npcs.Values)
            {
                var info = Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == npc.NPCIndex);
                if (info != null && SellsTownPortal(info))
                {
                    if (!_knownSupplyNpcLocations.TryGetValue(World.MapIndex, out var dict))
                        _knownSupplyNpcLocations[World.MapIndex] = dict = new Dictionary<int, Point>();
                    dict[info.Index] = npc.CurrentLocation;
                }
            }
            _nextActivityReport = now.AddSeconds(45 + _random.NextDouble() * 15);
        }
        if (World.Dead)
        {
            if (now >= _nextAttack) { _connection.Enqueue(new C.TownRevive()); _nextAttack = now.AddSeconds(5); }
            return;
        }

        if (TryPvPBehavior(now)) goto AfterMovement;

        // 任何非主城地图滞留兜底: 连续 ~10 分钟回不去(无卷/副本/寻路无路/供给断)
        // 就主动重登, 重登后 SetBindPoint 会把绑定点重选为主城, 出生即回城。
        // 正常练级外出/矿洞/副本时长都远短于此阈值。
        if (World.MapIndex != _config.HomeMapIndex)
        {
            if (_crossMapStuckSince == DateTime.MinValue) _crossMapStuckSince = now;
            else if ((now - _crossMapStuckSince).TotalMinutes >= 10)
            {
                _crossMapStuckSince = DateTime.MinValue;
                Console.WriteLine($"[{Name}] stuck away from home, relog to rebind home");
                _connection.TryDisconnect();
                return;
            }
        }

        // A PvP round takes priority over long-running life routes. Otherwise
        // a mining/trade auto-path can starve the scheduled arena behavior.
        // 跨图时 AutoPath 可能指向无效/不可达路线(如竞技场地图), 不短路,
        // 让跨图检查的宽限逻辑接管(商店补给回城卷)。
        if (_autoPathActive && World.MapIndex == _config.HomeMapIndex) goto AfterMovement;

        // 已回主城: 清除跨图滞留计时, 避免下次计划外跨图误判。
        if (World.MapIndex == _config.HomeMapIndex && _crossMapStuckSince != DateTime.MinValue)
            _crossMapStuckSince = DateTime.MinValue;
        if (World.MapIndex == _config.HomeMapIndex && _portalUseAt != DateTime.MinValue)
        {
            _portalUseAt = DateTime.MinValue;
            _portalUseSlot = null;
        }

        if (TryResourceBehavior(now)) goto AfterMovement;

        if (TryFishingBehavior(now)) goto AfterMovement;

        if (TryFieldTripBehavior(now)) goto AfterMovement;

        if (TryInstanceBehavior(now)) goto AfterMovement;

        // 计划外跨图(重启前遗留其他地图等): 直接 AutoPath 回城, 不在陌生地图
        // 执行交易/闲逛等本地行为。挖矿中(矿洞)、练级外出中、副本中由各自
        // 行为驱动豁免; PvP 跨图已由 TryPvPBehavior 处理。
        bool fieldTripActive = _fieldPathToField || _fieldPathHome ||
            (_fieldTripEnd != DateTime.MinValue && now < _fieldTripEnd);
        bool miningActive = _resourceTripEnd != DateTime.MinValue && now < _resourceTripEnd;
        // 服务器非副本时 InstanceIndex 为 -1(CurrentMap.Instance?.Index ?? -1),
        // 副本中才是实例索引(>=0)。旧判断 !=0 把 -1 误判成副本, 导致
        // %5==2 的角色在地图 11(竞技场)被永久豁免跨图回城。
        bool inInstance = _index % 5 == 2 && World.InstanceIndex >= 0;
        if (World.MapIndex != _config.HomeMapIndex && !fieldTripActive && !miningActive && !inInstance)
        {
            if (now >= _crossMapGraceUntil)
            {
                if (now >= _nextMove)
                {
                    // ItemUse 回城卷后 ~30s 仍非家图: 卷传送无效(BindPoint 被
                    // 职业安全区/红区改写, 如刺客巢穴 459 绑定后卷原地传送)。
                    // 停止烧卷, 重登让 SetBindPoint 重选绑定; 存档侧另行净化。
                    if (_portalUseAt != DateTime.MinValue && _portalUseSlot.HasValue &&
                        (now - _portalUseAt).TotalSeconds >= 30)
                    {
                        Console.WriteLine($"[{Name}] town portal ineffective (bindpoint hijacked), map={World.MapIndex} relog");
                        _portalUseAt = DateTime.MinValue;
                        _portalUseSlot = null;
                        _connection.TryDisconnect();
                        return;
                    }
                    // 跨图 AutoPath 常无路线(如竞技场地图), 优先用回城卷轴传送
                    // 到绑定点(出生城); 无卷轴再试 AutoPath, 连续失败则宽限补给。
                    var scroll = World.Inventory.FirstOrDefault(x => x?.Info != null &&
                        x.Info.ItemType == ItemType.Consumable && x.Info.Shape == 2 &&
                        x.Info.ItemName.Contains("Town Portal", StringComparison.OrdinalIgnoreCase));
                    if (now >= _nextCrossMapDiag)
                    {
                        _nextCrossMapDiag = now.AddSeconds(90);
                        Console.WriteLine($"[{Name}] cross-map scroll: found={scroll != null} count={scroll?.Count ?? -1} slot={scroll?.Slot ?? -999} stale={scroll?.Slot < 0}");
                    }
                    if (scroll != null && scroll.Count > 0)
                    {
                        // 在线购买(合并)的新物品在 ItemsGained 里 slot 恒为 -1
                        // (服务器在入背包前序列化), 缓存不可信时整理背包拿正确 slot。
                        if (scroll.Slot < 0)
                        {
                            _connection.Enqueue(new C.ItemSort { Grid = GridType.Inventory });
                            _nextMove = now.AddSeconds(10);
                            Console.WriteLine($"[{Name}] stale inventory slot, refresh sort");
                            return;
                        }
                        _connection.Enqueue(new C.ItemUse
                        {
                            Link = new CellLinkInfo { GridType = GridType.Inventory, Slot = scroll.Slot, Count = 1 }
                        });
                        _portalUseAt = now;
                        _portalUseSlot = scroll.Slot;
                        _nextMove = now.AddSeconds(25);
                        _supplyPurchasePending = false;
                        Console.WriteLine($"[{Name}] away from home map, town portal home");
                        return;
                    }
                    _crossMapFailCount++;
                    if (_crossMapFailCount >= 4)
                    {
                        _crossMapFailCount = 0;
                        _crossMapGraceUntil = now.AddSeconds(45);
                        Console.WriteLine($"[{Name}] cross-map autopath failing, resupply grace");
                        return;
                    }
                    // 竞技场等地图跨图 AutoPath 必失败: 优先朝缓存的卖卷 NPC 位置
                    // 步行(进入视野后宽限期的 supply 会 approach 买卷回城)。
                    if (_knownSupplyNpcLocations.TryGetValue(World.MapIndex, out var shopDict) && shopDict.Count > 0)
                    {
                        var shopPoint = shopDict.Values.First();
                        MoveToward(shopPoint, 1, now);
                        _nextMove = now.AddSeconds(5);
                        Console.WriteLine($"[{Name}] away from home, walk to known shop {shopPoint}");
                        return;
                    }
                    _connection.Enqueue(new C.AutoPathWaypoint
                    {
                        MapIndex = _config.HomeMapIndex,
                        Location = _homeAnchor
                    });
                    _nextMove = now.AddSeconds(8);
                    Console.WriteLine($"[{Name}] away from home map, autopath home");
                }
                return;
            }
            // 宽限期(now < _crossMapGraceUntil): 放行主链, 让商店补给买到回城卷轴
        }
        else if (World.MapIndex != _config.HomeMapIndex && now >= _nextCrossMapDiag)
        {
            _nextCrossMapDiag = now.AddSeconds(90);
            Console.WriteLine($"[{Name}] cross-map exempt fieldTrip={fieldTripActive} mining={miningActive} inst={inInstance} tripEnd={(_fieldTripEnd == DateTime.MinValue ? "none" : _fieldTripEnd.ToString("HH:mm:ss"))} fail={_crossMapFailCount} grace={(_crossMapGraceUntil == DateTime.MinValue ? "none" : (_crossMapGraceUntil - now).TotalSeconds.ToString("F0") + "s")}");
        }

        if (!_starterGuildAttempted && now >= _nextGuildAction)
        {
            _connection.Enqueue(new C.JoinStarterGuild());
            _starterGuildAttempted = true;
            Console.WriteLine($"[{Name}] social: join starter guild");
        }

        if (_index % 4 == 0 && now >= _nextMountAction)
        {
            var currentMap = Globals.MapInfoList?.Binding.FirstOrDefault(x => x.Index == World.MapIndex);
            if (World.Horse == HorseType.None && currentMap?.CanHorse == true)
            {
                _connection.Enqueue(new C.Mount());
                _mountStarted = now;
                _nextMountAction = now.AddMinutes(2 + _random.NextDouble());
                Console.WriteLine($"[{Name}] travel: mount");
            }
            else if (World.Horse != HorseType.None)
            {
                _connection.Enqueue(new C.Mount());
                _nextMountAction = now.AddMinutes(1 + _random.NextDouble());
                Console.WriteLine($"[{Name}] travel: dismount");
            }
        }

        if (TryTradeBehavior(now)) goto AfterMovement;

        // 防止无路径寻路时被障碍物或错误方向带离陪玩区域。
        // 仅在同一张地图内做回拉(跨图坐标不可比);阈值比巡逻半径上限
        // 宽裕, 避免"走向目标途中被拽回锚点"的边界拉锯。
        // 练级角色外出(去程/打怪中/回程)是计划移动, 豁免回拉。
        if (World.MapIndex == _config.HomeMapIndex && !fieldTripActive &&
            Distance(World.Location, ActivityAnchor()) > _config.PatrolRadius + 8)
        {
            if (now >= _nextMove)
            {
                double distBefore = Distance(World.Location, ActivityAnchor());
                MoveToward(ActivityAnchor(), 1, now);
                // 贪心逐格回拉可能被建筑/墙带卡住原地振荡, 连续几秒走
                // 不近就改用服务端 AutoPath 绕行回锚点(与外出回程同一机制)。
                if (Distance(World.Location, ActivityAnchor()) > distBefore - 1.5)
                {
                    _pullbackStuckSince = _pullbackStuckSince == DateTime.MinValue ? now : _pullbackStuckSince;
                    if ((now - _pullbackStuckSince).TotalSeconds > 6)
                    {
                        _pullbackStuckSince = DateTime.MinValue;
                        _nextMove = now.AddSeconds(8);
                        _connection.Enqueue(new C.AutoPathWaypoint
                        {
                            MapIndex = _config.HomeMapIndex,
                            Location = ActivityAnchor()
                        });
                        Console.WriteLine($"[{Name}] pullback stuck, autopath home");
                        return;
                    }
                }
                else
                {
                    _pullbackStuckSince = DateTime.MinValue;
                }
            }
            return;
        }

        if (now >= _nextPotion && ShouldUseConsumable())
        {
            var potion = World.Inventory
                .Where(x => x.Count > 0 && x.Info != null && x.Info.CanAutoPot)
                .OrderByDescending(x => IsManaPotion(x.Info) == NeedsManaPotion())
                .FirstOrDefault();
            if (potion != null)
            {
                if (potion.Slot < 0)
                {
                    // 在线购买的新物品 slot 缓存为 -1, 整理背包拿正确 slot
                    _connection.Enqueue(new C.ItemSort { Grid = GridType.Inventory });
                    _nextPotion = now.AddSeconds(8);
                    return;
                }
                _connection.Enqueue(new C.ItemUse
                {
                    Link = new CellLinkInfo { GridType = GridType.Inventory, Slot = potion.Slot, Count = 1 }
                });
                _nextPotion = now.AddSeconds(1.0 + _random.NextDouble() * 1.5);
            }
        }

        if (World.Class == MirClass.Taoist && now >= _nextSupport && TrySupportAlly(now))
        {
            _nextSupport = now.AddSeconds(3 + _random.NextDouble() * 2);
            goto AfterMovement;
        }

        if (now >= _nextProfessionAction && TryProfessionPreparation(now))
        {
            _nextProfessionAction = now.AddSeconds(12 + _random.NextDouble() * 8);
            goto AfterMovement;
        }

        if (now >= _nextInventorySort && World.Inventory.Count >= Math.Max(10, Globals.InventorySize / 2))
        {
            _connection.Enqueue(new C.ItemSort { Grid = GridType.Inventory });
            _nextInventorySort = now.AddMinutes(5);
        }

        if (TryContainerBehavior(now)) goto AfterMovement;

        if (now >= _nextTorchAction && World.EquippedTorch == null && World.SpareTorch != null)
        {
            _connection.Enqueue(new C.ItemMove
            {
                FromGrid = GridType.Inventory,
                FromSlot = World.SpareTorch.Slot,
                ToGrid = GridType.Equipment,
                ToSlot = (int)EquipmentSlot.Torch,
                MergeItem = false
            });
            _nextTorchAction = now.AddSeconds(8);
        }

        bool npcPriorityMove = false;
        if (now >= _nextRepairAction && now >= _supplyInteractionUntil && !_npcCallPending && NeedsRepair())
        {
            var repairNpc = World.Npcs.Values
                .Select(x => (Object: x, Info: Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == x.NPCIndex)))
                .Where(x => x.Info != null)
                .OrderBy(x => Distance(World.Location, x.Object.CurrentLocation))
                .FirstOrDefault(x => Distance(World.Location, x.Object.CurrentLocation) <= Math.Max(12, _config.PatrolRadius * 2));
            if (repairNpc.Object != null)
            {
                _npcObjectId = repairNpc.Object.ObjectID;
                int distance = Distance(World.Location, repairNpc.Object.CurrentLocation);
                if (distance <= 2)
                {
                    _connection.Enqueue(new C.NPCCall { ObjectID = _npcObjectId });
                    _npcCallPending = true;
                    _repairCallPending = true;
                    _nextRepairAction = now.AddSeconds(8);
                }
                else if (now >= _nextMove)
                {
                    MoveToward(repairNpc.Object.CurrentLocation, 1, now);
                    npcPriorityMove = true;
                }
            }
        }

        if (now >= _nextQuestAction && now >= _supplyInteractionUntil && !_npcCallPending)
        {
            var questNpc = World.Npcs.Values
                .Select(x => (Object: x, Info: Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == x.NPCIndex)))
                .Where(x => x.Info != null && (x.Info.StartQuests?.Count > 0 || x.Info.FinishQuests?.Count > 0))
                .OrderBy(x => Distance(World.Location, x.Object.CurrentLocation))
                .FirstOrDefault(x => Distance(World.Location, x.Object.CurrentLocation) <= Math.Max(12, _config.PatrolRadius * 2));
            if (questNpc.Object != null)
            {
                _npcObjectId = questNpc.Object.ObjectID;
                int distance = Distance(World.Location, questNpc.Object.CurrentLocation);
                if (distance <= 2)
                {
                    _connection.Enqueue(new C.NPCCall { ObjectID = _npcObjectId });
                    _npcCallPending = true;
                    _nextQuestAction = now.AddSeconds(10);
                }
                else if (!npcPriorityMove && now >= _nextMove)
                {
                    MoveToward(questNpc.Object.CurrentLocation, 1, now);
                    npcPriorityMove = true;
                }
            }
        }

        if (npcPriorityMove) goto AfterMovement;

        if (TrySellBehavior(now)) goto AfterMovement;

        if (TrySupplyBehavior(now)) goto AfterMovement;

        var target = SelectTarget(now);
        if (target != null)
        {
            int distance = Distance(World.Location, target.Location);
            if (distance <= 2 && now >= _nextAttack)
            {
                var direction = DirectionTo(World.Location, target.Location);
                var attackSkill = SelectAttackSkill();
                var magic = SelectCombatMagic();
                if (attackSkill != MagicType.None && World.Class is MirClass.Warrior or MirClass.Assassin && _random.Next(100) < 55)
                {
                    _connection.Enqueue(new C.Attack { Direction = direction, Action = MirAction.Attack, AttackMagic = attackSkill });
                    _attackActions++;
                }
                else if (magic != MagicType.None && World.Class is MirClass.Wizard or MirClass.Taoist && _random.Next(100) < 65)
                {
                    _connection.Enqueue(new C.Magic
                    {
                        Direction = direction,
                        Action = MirAction.Spell,
                        Type = magic,
                        Target = target.ObjectID,
                        Location = target.Location
                    });
                    _magicActions++;
                }
                else
                {
                    _connection.Enqueue(new C.Attack { Direction = direction, Action = MirAction.Attack, AttackMagic = MagicType.None });
                    _attackActions++;
                }
                _combatActions++;
                // 物理职业按武器节奏连击(0.9~1.3s, 服务端 AttackTime 下限 800ms);
                // 法师/道士施法受 MagicDelay=2000ms 节流, 2.2~3.2s 一次。
                bool casting = World.Class is MirClass.Wizard or MirClass.Taoist;
                _nextAttack = now.AddSeconds(casting
                    ? 2.2 + _random.NextDouble()
                    : 0.9 + _random.NextDouble() * 0.4);
                // 真人连击会绕目标走位再打, 不是机械每 3 刀就巡逻走开。
                if (_combatActions >= 3 && _random.NextDouble() < 0.25 && now >= _nextMove)
                {
                    _combatActions = 0;
                    MoveToward(FlankPoint(target.Location), 1, now);
                    goto AfterMovement;
                }
            }
            else if (distance > 2 && now >= _nextMove)
            {
                MoveToward(target.Location, distance > 5 ? 2 : 1, now);
            }
        }
        else if (now >= _nextMove)
        {
            var corpse = World.Monsters.Values
                .Where(x => x.Dead)
                .OrderBy(x => Distance(World.Location, x.Location))
                .FirstOrDefault(x => Distance(World.Location, x.Location) <= 2);
            if (corpse != null && now >= _nextHarvest)
            {
                _connection.Enqueue(new C.Harvest { Direction = DirectionTo(World.Location, corpse.Location) });
                _nextHarvest = now.AddSeconds(0.3 + _random.NextDouble() * 0.5);
                goto AfterMovement;
            }

            var leader = World.GroupMembers.Contains("Bot01")
                ? World.Players.Values.FirstOrDefault(x => x.Name.Equals("Bot01", StringComparison.OrdinalIgnoreCase))
                : null;
            if (leader != null && Distance(World.Location, leader.Location) > 7)
            {
                // 跟队但别挤在同一点: 以队长为锚加随机偏移, 小队自然散开。
                Point followPoint = new Point(
                    leader.Location.X + _random.Next(-3, 4),
                    leader.Location.Y + _random.Next(-3, 4));
                if (CurrentMap()?.CanWalk(followPoint) != true)
                    followPoint = leader.Location;
                MoveToward(followPoint, 1, now);
                goto AfterMovement;
            }

            var loot = World.Items.Values
                .OrderBy(x => Distance(World.Location, x.Location))
                .FirstOrDefault(x => Distance(World.Location, x.Location) <= 10);
            if (loot != null)
            {
                if (Distance(World.Location, loot.Location) <= 1)
                {
                    _connection.Enqueue(new C.PickUp());
                    _pickupRequests++;
                }
                else
                    MoveToward(loot.Location, 1, now);
            }
            else
            {
                if (TryTownCastingBehavior(now)) goto AfterMovement;
                Patrol(now);
            }
        }

    AfterMovement:
        if ((_index == 1 || _index == 9) && !_groupInviteAttempted && now >= _nextGroupAction)
        {
            // Two separate squads create visible team-vs-team behavior while
            // retaining the server's normal group loot grace period.
            int first = _index == 1 ? 2 : 10;
            int last = _index == 1 ? 8 : 16;
            for (int i = first; i <= last; i++)
                _connection.Enqueue(new C.GroupInvite { Name = $"Bot{i:00}" });
            _groupInviteAttempted = true;
            _nextGroupAction = now.AddSeconds(45);
        }

        if (now >= _nextChat)
        {
            _connection.Enqueue(new C.Chat { Text = $"{_config.ChatPrefix}，我叫{Name}。" });
            _nextChat = now.AddSeconds(Math.Max(30, _config.ChatIntervalSeconds) + _random.NextDouble() * 45);
        }

        var item = World.Items.Values.OrderBy(x => Distance(World.Location, x.Location)).FirstOrDefault(x => Distance(World.Location, x.Location) <= 1);
        if (item != null)
        {
            _connection.Enqueue(new C.PickUp());
            _pickupRequests++;
        }
    }

    private S.ObjectMonster SelectTarget(DateTime now)
    {
        if (now < _nextTargetScan && World.Monsters.TryGetValue(_targetMonsterId, out var cached) && !cached.Dead)
            return cached;

        var target = World.Monsters.Values
            // Companions are broadcast as ObjectMonster too, but they belong
            // to players and should never be selected as normal PvE targets.
            .Where(x => !x.Dead && string.IsNullOrWhiteSpace(x.PetOwner) && x.CompanionObject == null)
            .OrderBy(x => Distance(World.Location, x.Location))
            .FirstOrDefault(x => Distance(World.Location, x.Location) <= 12);
        _targetMonsterId = target?.ObjectID ?? 0;
        if (target != null) _targetSelections++;
        _nextTargetScan = now.AddSeconds(1);
        return target;
    }

    private bool TryPvPBehavior(DateTime now)
    {
        if (!_config.EnableBotPvP || !IsPvpBot(_index) || now < _nextPvpAction) return false;

        if (World.MapIndex != _config.HomeMapIndex)
        {
            _autoPathActive = false;
            // 有回城卷优先传送; 无卷放行主链, 由跨图检查的宽限逻辑去商店补给
            var scroll = World.Inventory.FirstOrDefault(x => x?.Info != null &&
                x.Info.ItemType == ItemType.Consumable && x.Info.Shape == 2 &&
                x.Info.ItemName.Contains("Town Portal", StringComparison.OrdinalIgnoreCase));
            if (scroll != null && scroll.Count > 0)
            {
                if (now >= _nextMove)
                {
                    if (scroll.Slot < 0)
                    {
                        _connection.Enqueue(new C.ItemSort { Grid = GridType.Inventory });
                        _nextMove = now.AddSeconds(10);
                        Console.WriteLine($"[{Name}] stale inventory slot, refresh sort");
                        return true;
                    }
                    _connection.Enqueue(new C.ItemUse
                    {
                        Link = new CellLinkInfo { GridType = GridType.Inventory, Slot = scroll.Slot, Count = 1 }
                    });
                    _portalUseAt = now;
                    _portalUseSlot = scroll.Slot;
                    _nextMove = now.AddSeconds(25);
                    _supplyPurchasePending = false;
                    Console.WriteLine($"[{Name}] pvp: town portal home");
                }
                return true;
            }
            return false; // 无卷: 放行主链跨图检查的 grace 逻辑买卷
        }
        if (_pvpRoundEnd == DateTime.MinValue)
        {
            _pvpRoundEnd = now.AddSeconds(Math.Max(20, _config.PvPRoundSeconds));
            _pvpStagingPoint = ChoosePvpPoint();
            if (_autoPathActive)
            {
                _connection.Enqueue(new C.AutoPathCancel());
                _autoPathActive = false;
            }
            Console.WriteLine($"[{Name}] pvp: round start safe={World.InSafeZone} staging={_pvpStagingPoint}");
        }
        else if (now >= _pvpRoundEnd)
        {
            _pvpRoundEnd = DateTime.MinValue;
            _nextPvpAction = now.AddSeconds(Math.Max(30, _config.PvPRestSeconds) + _random.NextDouble() * 30);
            _pvpStagingPoint = Point.Empty;
            return false;
        }

        if (_pvpStagingPoint == Point.Empty)
            _pvpStagingPoint = ChoosePvpPoint();

        // PvP is deliberately staged outside the server-reported safe zone.
        // This lets the normal PlayerObject.CanAttackTarget rules decide
        // whether damage is legal instead of bypassing them in the bot.
        if (World.InSafeZone)
        {
            if (now >= _nextMove) MoveToward(_pvpStagingPoint, 1, now);
            return true;
        }

        var target = SelectPlayerTarget();
        if (target == null)
        {
            if (now >= _nextMove) Patrol(now);
            _nextPvpAction = now.AddSeconds(1.5 + _random.NextDouble() * 2);
            return true;
        }

        int distance = Distance(World.Location, target.Location);
        if (distance > 2)
        {
            if (now >= _nextMove) MoveToward(target.Location, 1, now);
            _nextPvpAction = now.AddSeconds(0.5 + _random.NextDouble());
            return true;
        }

        var direction = DirectionTo(World.Location, target.Location);
        var magic = SelectCombatMagic();
        if (magic != MagicType.None && World.Class is MirClass.Wizard or MirClass.Taoist && _random.Next(100) < 60)
        {
            _connection.Enqueue(new C.Magic { Direction = direction, Action = MirAction.Spell,
                Type = magic, Target = target.ObjectID, Location = target.Location });
            _magicActions++;
        }
        else
        {
            _connection.Enqueue(new C.Attack { Direction = direction, Action = MirAction.Attack,
                AttackMagic = SelectAttackSkill() });
            _attackActions++;
        }
        _pvpActions++;
        bool casting = World.Class is MirClass.Wizard or MirClass.Taoist;
        _nextPvpAction = now.AddSeconds(casting
            ? 2.2 + _random.NextDouble()
            : 0.9 + _random.NextDouble() * 0.4);
        if (_pvpActions >= 3 && _random.NextDouble() < 0.25)
        {
            _pvpActions = 0;
            if (now >= _nextMove) { _nextMove = now; MoveToward(FlankPoint(target.Location), 1, now); }
        }
        return true;
    }

    private S.ObjectPlayer SelectPlayerTarget()
    {
        return World.Players.Values
            .Where(x => !x.Dead && !x.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
            .Where(x => IsPvpOpponent(x.Name))
            .Where(x => Distance(World.Location, x.Location) <= 12)
            .OrderBy(x => Distance(World.Location, x.Location))
            .FirstOrDefault();
    }

    private bool IsPvpOpponent(string name)
    {
        if (!name.StartsWith("Bot", StringComparison.OrdinalIgnoreCase) || !int.TryParse(name[3..], out int other)) return false;
        int duelPartner = DuelPartner(_index);
        int otherPartner = DuelPartner(other);
        if (duelPartner > 0 || otherPartner > 0)
        {
            // Dedicated duel lanes: Bot05/06, Bot10/11, Bot15/16, Bot20/01.
            return duelPartner == other && otherPartner == _index;
        }
        // Other bots are split into four-person squads. Different squads are
        // opponents; same-squad members can still heal and support each other.
        return (_index - 1) / 4 != (other - 1) / 4;
    }

    private static int DuelPartner(int index) => index switch
    {
        5 => 6, 6 => 5, 10 => 11, 11 => 10, 15 => 16, 16 => 15, 20 => 1, 1 => 20,
        _ => 0
    };

    private Point ChoosePvpPoint()
    {
        var map = CurrentMap();
        for (int i = 0; i < 30; i++)
        {
            // Use one shared rendezvous area so the 20 independent clients
            // actually meet. A small per-bot offset prevents one-cell stacking.
            var point = new Point(_config.PvPStagingX + _random.Next(-3, 4),
                _config.PvPStagingY + _random.Next(-3, 4));
            if (map == null || map.CanWalk(point)) return point;
        }
        return new Point(_config.PvPStagingX, _config.PvPStagingY);
    }

    private void MoveToward(Point location, int distance, DateTime now)
    {
        int remaining = Distance(World.Location, location);
        if (remaining <= 1) return; // 已在目标旁, 不空走一格

        var direction = ChooseWalkDirection(location);
        // 目标较远且前方两格都可行走时跑步(distance=2), 否则步行。
        // 服务器对每步逐格做阻挡校验, 两格可达性验证避免跑步撞墙触发
        // UserLocation 纠正的闪动。
        bool run = remaining > 4 && CanWalkTwo(World.Location, direction);
        int step = run ? 2 : 1;
        if (step >= remaining) step = 1; // 一步不越过目标点

        _connection.Enqueue(new C.Turn { Direction = direction });
        _connection.Enqueue(new C.Move { Direction = direction, Distance = step });
        _moveActions++;
        // 真人步频带自然抖动, 不是节拍器: 走 0.65~0.8s/格, 跑 0.6~0.75s/2格。
        // 服务端 MoveTime=600ms 是硬下限, 此区间不会触发排队或纠正。
        double interval = (run ? _config.RunIntervalSeconds : _config.WalkIntervalSeconds)
            + _random.NextDouble() * 0.15;
        _nextMove = now.AddSeconds(interval);
    }

    private bool CanWalkTwo(Point from, MirDirection direction)
    {
        var map = CurrentMap();
        if (map == null) return false;
        var first = NextPoint(from, direction);
        if (!map.CanWalk(first)) return false;
        return map.CanWalk(NextPoint(first, direction));
    }

    private MirDirection ChooseWalkDirection(Point target)
    {
        var preferred = DirectionTo(World.Location, target);
        var candidates = new[]
        {
            preferred,
            Rotate(preferred, 1), Rotate(preferred, 7),
            Rotate(preferred, 2), Rotate(preferred, 6),
            Rotate(preferred, 3), Rotate(preferred, 5), Rotate(preferred, 4)
        };

        var map = CurrentMap();
        if (map == null) return preferred;
        foreach (var candidate in candidates)
        {
            if (map.CanWalk(NextPoint(World.Location, candidate))) return candidate;
        }

        return preferred;
    }

    private BotMap CurrentMap()
    {
        var info = Globals.MapInfoList?.Binding.FirstOrDefault(x => x.Index == World.MapIndex);
        if (info == null || string.IsNullOrWhiteSpace(info.FileName)) return null;
        string path = Path.Combine(_config.MapPath, $"{info.FileName}.map");
        if (path.Equals(_mapFile, StringComparison.OrdinalIgnoreCase)) return _map;
        _mapFile = path;
        _map = BotMap.Load(path);
        return _map;
    }

    private static Point NextPoint(Point point, MirDirection direction)
        => direction switch
        {
            MirDirection.Up => new Point(point.X, point.Y - 1),
            MirDirection.UpRight => new Point(point.X + 1, point.Y - 1),
            MirDirection.Right => new Point(point.X + 1, point.Y),
            MirDirection.DownRight => new Point(point.X + 1, point.Y + 1),
            MirDirection.Down => new Point(point.X, point.Y + 1),
            MirDirection.DownLeft => new Point(point.X - 1, point.Y + 1),
            MirDirection.Left => new Point(point.X - 1, point.Y),
            MirDirection.UpLeft => new Point(point.X - 1, point.Y - 1),
            _ => point
        };

    private static MirDirection Rotate(MirDirection direction, int steps)
        => (MirDirection)(((int)direction + steps + 8) % 8);

    // 角色划分(按 index): %5==0 矿工, %5==1/2 野外练级, %5==3 PvP, %5==4 城中心社交。
    // 让 bot 分布到各自的固定活动点, 而不是全部挤在出生地空地打转。
    private bool IsPvpBot(int index) => index % 5 == 3;
    private bool IsFieldBot(int index) => index % 5 == 1 || index % 5 == 2;
    private string RoleName(int index)
    {
        if (index % 5 == 0) return "miner";
        if (index % 5 == 1 || index % 5 == 2) return "field";
        if (index % 5 == 3) return "pvp";
        return "social";
    }

    // 当前地图内的活动锚点: 巡逻远足、回拉都以它为准。
    // 所有角色(含 PvP)都锚在城中心自己的"家"角落, 让出生点长期有人;
    // PvP 回合中由 TryPvPBehavior 直接接管, 不经过这里的锚点。
    private Point ActivityAnchor() => _homeAnchor;

    // 城中心出生点。登录时 SpawnMapIndex 是角色下线位置而非固定出生图,
    // 因此家在配置的 HomeMap(比奇县), 仅当登录位置就在出生图时用其坐标。
    private Point HomeLocation()
    {
        return World.SpawnMapIndex == _config.HomeMapIndex
            ? World.SpawnLocation
            : new Point(_config.HomeMapX, _config.HomeMapY);
    }

    // 每个 bot 在城中心出生点周围选一个可走点作为自己的"家",
    // 带抖动让 20 人散在城中心不同角落而不是叠在同一点。
    private Point ChooseHomeAnchor()
    {
        var home = HomeLocation();
        var map = CurrentMap();
        for (int i = 0; i < 30; i++)
        {
            var point = new Point(
                Math.Clamp(home.X + _random.Next(-_config.HomeAnchorRadius, _config.HomeAnchorRadius + 1), 0, 349),
                Math.Clamp(home.Y + _random.Next(-_config.HomeAnchorRadius, _config.HomeAnchorRadius + 1), 0, 349));
            if (map == null || map.CanWalk(point)) return point;
        }
        return home;
    }

    // 练级 bot 各自在野外怪区选一个可走锚点, 带抖动避免扎堆。
    private Point ChooseFieldAnchor()
    {
        var map = CurrentMap();
        for (int i = 0; i < 30; i++)
        {
            var point = new Point(
                Math.Clamp(_config.FieldAnchorX + _random.Next(-_config.FieldRadius, _config.FieldRadius + 1), 0, 349),
                Math.Clamp(_config.FieldAnchorY + _random.Next(-_config.FieldRadius, _config.FieldRadius + 1), 0, 349));
            if (map == null || map.CanWalk(point)) return point;
        }
        return new Point(_config.FieldAnchorX, _config.FieldAnchorY);
    }

    private void Patrol(DateTime now)
    {
        // 真人闲逛会偶发驻足(看路/犹豫), 让节奏不像精确节拍器。
        if (_random.NextDouble() < 0.12)
        {
            _nextMove = now.AddSeconds(0.5 + _random.NextDouble() * 1.2);
            return;
        }
        bool arrived = _patrolTarget != Point.Empty && Distance(World.Location, _patrolTarget) <= 1;
        // 到点后驻留 1.5~4s "看看路" 再挑下一个点, 否则会变成永动钟摆。
        if (arrived && now < _arrivedPauseUntil)
        {
            _nextMove = now.AddSeconds(0.3);
            return;
        }
        if (arrived || _patrolTarget == Point.Empty)
        {
            _patrolTarget = ChoosePatrolPoint();
            if (arrived)
                _arrivedPauseUntil = now.AddSeconds(1.5 + _random.NextDouble() * 2.5);
        }
        MoveToward(_patrolTarget, 1, now);
    }

    private Point FlankPoint(Point center)
    {
        // 战斗走位: 绕目标横向 1~2 格取可行走点, 造成"边走边打",
        // 而不是机械地每 N 刀巡逻去远处。
        int side = _random.Next(2) == 0 ? -1 : 1;
        var map = CurrentMap();
        for (int i = 0; i < 6; i++)
        {
            var p = new Point(center.X + side * (1 + _random.Next(2)), center.Y + _random.Next(-2, 3));
            if (map == null || map.CanWalk(p)) return p;
        }
        return center;
    }

    private Point ChoosePatrolPoint()
    {
        var map = CurrentMap();
        // 锚点随当前位置漂移, 让闲逛轨迹自然蔓延, 而不是绕出生点钟摆折返。
        // 大部分时候小范围漫步(2~6 格), 偶发以锚点为锚做一次远足(8~12 格)。
        // 练级角色外出打怪中, 远足锚用怪区点而不是城中心, 避免往城漂。
        bool tripActive = _fieldPathToField || _fieldPathHome || _fieldTripEnd != DateTime.MinValue;
        Point anchor = _random.NextDouble() < 0.8
            ? World.Location
            : (tripActive ? _fieldAnchor : ActivityAnchor());
        int radius = _random.NextDouble() < 0.8
            ? 2 + _random.Next(0, 5)
            : 8 + _random.Next(0, Math.Max(1, _config.PatrolRadius - 7));
        for (int i = 0; i < 20; i++)
        {
            var point = new Point(anchor.X + _random.Next(-radius, radius + 1),
                anchor.Y + _random.Next(-radius, radius + 1));
            // 防折返: 新目标离刚走到的目标太近(<3 格)时重抽, 避免 A->B->B->A。
            if (_patrolTarget != Point.Empty && Distance(point, _patrolTarget) <= 3) continue;
            if (map == null || map.CanWalk(point)) return point;
        }
        return World.Location;
    }

    private bool TryProfessionPreparation(DateTime now)
    {
        var known = World.Magics.Where(x => x.Info != null && !x.ItemRequired).ToList();
        if (World.Class is MirClass.Wizard or MirClass.Taoist)
        {
            var shield = known.FirstOrDefault(x => x.Info.Magic == MagicType.SuperiorMagicShield)
                ?? known.FirstOrDefault(x => x.Info.Magic == MagicType.MagicShield);
            if (shield != null)
            {
                _connection.Enqueue(new C.Magic { Direction = MirDirection.Down, Action = MirAction.Spell,
                    Type = shield.Info.Magic, Target = World.SelfObjectId, Location = World.Location });
                _magicActions++;
                Console.WriteLine($"[{Name}] skill: maintain {shield.Info.Magic}");
                // 真人护盾是持续时间型, 按护盾时长节奏补, 补完继续做自己的事,
                // 不会被强制"补盾→立刻挪步"。
                return true;
            }
        }

        if (World.Class == MirClass.Taoist && OwnedSummonCount() == 0)
        {
            var summon = known.Where(x => x.Info.Magic is MagicType.SummonDemonicCreature or MagicType.SummonShinsu
                or MagicType.SummonJinSkeleton or MagicType.SummonSkeleton)
                .OrderByDescending(x => x.Info.NeedLevel1).FirstOrDefault();
            if (summon != null)
            {
                _connection.Enqueue(new C.Magic { Direction = (MirDirection)_random.Next(8), Action = MirAction.Spell,
                    Type = summon.Info.Magic, Target = 0, Location = World.Location });
                _magicActions++;
                Console.WriteLine($"[{Name}] skill: summon {summon.Info.Magic}");
                return true;
            }
        }
        return false;
    }

    private int OwnedSummonCount()
        => World.Monsters.Values.Count(x => x.PetOwner?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true && x.CompanionObject == null);

    private MagicType SelectAttackSkill()
    {
        var preferred = World.Class switch
        {
            MirClass.Warrior => new[] { MagicType.DragonRise, MagicType.BladeStorm, MagicType.HalfMoon, MagicType.Slaying, MagicType.Thrusting, MagicType.DefensiveBlow, MagicType.OffensiveBlow },
            MirClass.Assassin => new[] { MagicType.HundredFist, MagicType.Shuriken, MagicType.Hemorrhage, MagicType.FlamingDaggers, MagicType.Shredding, MagicType.ThunderKick },
            _ => Array.Empty<MagicType>()
        };
        return preferred.FirstOrDefault(x => World.Magics.Any(m => m.Info?.Magic == x && !m.ItemRequired));
    }

    private MagicType SelectCombatMagic()
    {
        var usable = World.Magics
            // Only choose spells that have actual offensive power. This keeps
            // utility spells such as teleport, shields and healing out of the
            // attack loop, where they would otherwise be rejected or look
            // unnatural to nearby players.
            .Where(x => x.Info != null && !x.ItemRequired && x.Info.MinBasePower > 0)
            .Where(x => x.Info.School is MagicSchool.Fire or MagicSchool.Ice
                or MagicSchool.Lightning or MagicSchool.Wind or MagicSchool.Holy
                or MagicSchool.Dark or MagicSchool.Phantom or MagicSchool.Physical
                or MagicSchool.Atrocity or MagicSchool.Kill or MagicSchool.Assassination)
            .Where(x => World.Class == MirClass.Wizard
                ? x.Info.Magic is not (MagicType.ExplosiveTalisman or MagicType.PoisonDust)
                : World.Class == MirClass.Taoist
                    ? x.Info.Magic is not (MagicType.FireBall or MagicType.LightningBall or MagicType.IceBolt)
                    : false)
            .Select(x => x.Info.Magic)
            .Where(x => x != MagicType.None)
            .ToList();
        return usable.Count == 0 ? MagicType.None : usable[_random.Next(usable.Count)];
    }

    private void ScheduleNextActions()
    {
        var now = DateTime.UtcNow;
        _nextMove = now.AddSeconds(0.5 + _random.NextDouble());
        _nextAttack = now.AddSeconds(1 + _random.NextDouble());
        _nextChat = now.AddSeconds(8 + _random.NextDouble() * 12);
        _nextPotion = now.AddSeconds(2 + _random.NextDouble());
        _patrolTarget = Point.Empty;
        _arrivedPauseUntil = DateTime.MinValue;
        _fieldAnchor = ChooseFieldAnchor();
        _homeAnchor = ChooseHomeAnchor();
        _nextFieldTrip = now.AddSeconds(120 + _random.NextDouble() * 120);
        _fieldTripEnd = DateTime.MinValue;
        _fieldPathToField = false;
        _fieldPathHome = false;
        _nextTownCast = now.AddSeconds(15 + _random.NextDouble() * 25);
        _targetMonsterId = 0;
        _nextGroupAction = now.AddSeconds(8);
        _nextTorchAction = now.AddSeconds(4);
        _nextRepairAction = now.AddSeconds(15);
        _nextQuestAction = now.AddSeconds(20);
        _nextHarvest = now.AddSeconds(2 + _random.NextDouble() * 2);
        _nextInventorySort = now.AddSeconds(40 + _random.NextDouble() * 20);
        _nextSupport = now.AddSeconds(4 + _random.NextDouble() * 4);
        _nextSupplyAction = now.AddSeconds(40 + _random.NextDouble() * 20);
        _nextSellAction = now.AddSeconds(60 + _random.NextDouble() * 30);
        _supplyPurchasePending = false;
        _shopPurchases = 0;
        _shopSales = 0;
        _nextResourceAction = now.AddSeconds(20 + _random.NextDouble() * 20);
        _resourceTripEnd = DateTime.MinValue;
        _resourcePathToMine = false;
        _resourcePathHome = false;
        _resourceSwapPending = false;
        _groupInviteAttempted = false;
        _nextTradeAction = now.AddSeconds(15 + _random.NextDouble() * 15);
        _tradeRequestSent = false;
        _tradeActive = false;
        _tradePathRequested = false;
        _tradeAutoPathAllowed = true;
        _tradeFacingPrimed = false;
        _nextGuildAction = now.AddSeconds(15 + _random.NextDouble() * 20);
        _starterGuildAttempted = false;
        _nextMountAction = now.AddSeconds(25 + _random.NextDouble() * 20);
        _mountStarted = DateTime.MinValue;
        _nextContainerAction = now.AddSeconds(45 + _random.NextDouble() * 30);
        _containerSlot = -1;
        _nextFishingAction = now.AddSeconds(40 + _random.NextDouble() * 30);
        _fishingActive = false;
        _fishingPoint = Point.Empty;
        _nextInstanceAction = now.AddSeconds(60 + _random.NextDouble() * 40);
        _nextPvpAction = now.AddSeconds(Math.Max(5, _config.PvPStartDelaySeconds) + _random.NextDouble() * 15);
        _pvpRoundEnd = DateTime.MinValue;
        _pvpStagingPoint = Point.Empty;
        _pvpActions = 0;
        _nextProfessionAction = now.AddSeconds(4 + _random.NextDouble() * 5);
        _nextActivityReport = now.AddSeconds(10 + _random.NextDouble() * 10);
        _moveActions = 0;
        _attackActions = 0;
        _magicActions = 0;
        _targetSelections = 0;
        _combatActions = 0;
    }

    private bool TryInstanceBehavior(DateTime now)
    {
        if (IsPvpBot(_index)) return false;
        // Only a subset uses the dungeon finder, leaving the rest in the
        // overworld so the test map still has ordinary social traffic.
        if (_index % 5 != 2 || World.InstanceIndex != 0 || now < _nextInstanceAction)
            return false;

        var instance = Globals.InstanceInfoList?.Binding
            .Where(x => x != null && x.ShowOnDungeonFinder)
            .Where(x => (x.MinPlayerLevel == 0 || World.Level >= x.MinPlayerLevel) &&
                        (x.MaxPlayerLevel == 0 || World.Level <= x.MaxPlayerLevel))
            .OrderBy(x => x.MinPlayerLevel)
            .FirstOrDefault();
        if (instance == null) return false;

        _connection.Enqueue(new C.JoinInstance { Index = instance.Index });
        _nextInstanceAction = now.AddMinutes(5);
        Console.WriteLine($"[{Name}] instance: join {instance.Name}");
        return true;
    }

    private bool TryFishingBehavior(DateTime now)
    {
        if (IsPvpBot(_index)) return false;
        // One specialist per five bots keeps the world varied. A fishing
        // action is enabled only when both the real equipment and a server
        // configured fishing region exist.
        if (_index % 5 != 1) return false;

        if (_fishingActive && now >= _nextFishingAction)
        {
            _connection.Enqueue(new C.FishingCast
            {
                State = FishingState.Cancel,
                Direction = World.Direction,
                FloatLocation = _fishingPoint,
                CaughtFish = false
            });
            _fishingActive = false;
            _fishingPoint = Point.Empty;
            _nextFishingAction = now.AddMinutes(1);
            Console.WriteLine($"[{Name}] life: fishing timeout, cancel");
            return true;
        }

        if (now < _nextFishingAction) return _fishingActive;

        var rod = World.Inventory.FirstOrDefault(x => x.Info?.ItemEffect == ItemEffect.FishingRod && IsEquipped(x));
        var robe = World.Inventory.FirstOrDefault(x => x.Info?.ItemEffect == ItemEffect.FishingRobe && IsEquipped(x));
        if (rod == null || robe == null) return false;

        var fishingInfo = Globals.FishingInfoList?.Binding.FirstOrDefault(x => x.Region?.Map?.Index == World.MapIndex);
        var map = CurrentMap();
        if (fishingInfo?.Region == null || map == null) return false;

        if (_fishingPoint == Point.Empty || !map.CanWalk(_fishingPoint))
        {
            var points = fishingInfo.Region.GetPoints(map.Width);
            if (points == null || points.Count == 0) return false;
            _fishingPoint = points.ElementAt(_random.Next(points.Count));
        }

        if (_fishingActive) return true;
        if (Distance(World.Location, _fishingPoint) > 4)
        {
            if (now >= _nextMove) MoveToward(_fishingPoint, 1, now);
            return true;
        }

        var direction = DirectionTo(World.Location, _fishingPoint);
        _connection.Enqueue(new C.FishingCast
        {
            State = FishingState.Cast,
            Direction = direction,
            FloatLocation = _fishingPoint,
            CaughtFish = false
        });
        _fishingActive = true;
        _nextFishingAction = now.AddSeconds(20);
        Console.WriteLine($"[{Name}] life: cast fishing at {_fishingPoint}");
        return true;
    }

    private void HandleFishingState(S.ObjectFishing packet)
    {
        if (packet.State is FishingState.None or FishingState.Cancel)
        {
            _fishingActive = false;
            _fishingPoint = Point.Empty;
            _nextFishingAction = DateTime.UtcNow.AddMinutes(2);
            return;
        }

        if (packet.State == FishingState.Cast && packet.FishFound)
        {
            _connection.Enqueue(new C.FishingCast
            {
                State = FishingState.Reel,
                Direction = packet.Direction,
                FloatLocation = packet.FloatLocation,
                CaughtFish = true
            });
            _nextFishingAction = DateTime.UtcNow.AddSeconds(1.5 + _random.NextDouble() * 2);
        }
    }

    private bool TryContainerBehavior(DateTime now)
    {
        if (_containerSlot >= 0 || now < _nextContainerAction) return false;

        var bundle = World.Inventory.FirstOrDefault(x => x.Slot >= 0 && x.Slot < Globals.InventorySize &&
            x.Info != null && Globals.BundleInfoList?.Binding.Any(b => b.Index == x.Info.Shape) == true);
        if (bundle == null) return false;

        _containerSlot = bundle.Slot;
        _connection.Enqueue(new C.BundleOpen { Slot = bundle.Slot });
        _nextContainerAction = now.AddSeconds(30);
        Console.WriteLine($"[{Name}] item: open bundle slot {bundle.Slot}");
        return true;
    }

    private void HandleBundleOpen(S.BundleOpen packet)
    {
        int slot = packet.Slot;
        var source = World.Inventory.FirstOrDefault(x => x.Slot == slot);
        var info = Globals.BundleInfoList?.Binding.FirstOrDefault(x => x.Index == source?.Info?.Shape);
        if (info == null)
        {
            _containerSlot = -1;
            return;
        }

        int choice = info.Type == BundleType.OneOf
            ? packet.Items?.Where(x => x != null).OrderBy(x => x.Slot).Select(x => x.Slot).FirstOrDefault(-1) ?? -1
            : -1;
        _connection.Enqueue(new C.BundleConfirm { Slot = slot, Choice = choice });
        _containerSlot = -1;
        _nextContainerAction = DateTime.UtcNow.AddMinutes(2);
        Console.WriteLine($"[{Name}] item: confirm bundle slot {slot} choice {choice}");
    }

    private bool TryTradeBehavior(DateTime now)
    {
        if (_tradeActive) return true;

        var bot01 = World.Players.Values.FirstOrDefault(x =>
            x.Name.Equals("Bot01", StringComparison.OrdinalIgnoreCase));

        // The recipient keeps facing the initiator so the server's normal
        // face-to-face trade validation can succeed.
        if (_index == 2 && bot01 != null && Distance(World.Location, bot01.Location) <= 4)
        {
            int targetDistance = Distance(World.Location, bot01.Location);
            if (targetDistance > 1)
            {
                if (now >= _nextMove)
                    MoveToward(bot01.Location, 1, now);
                return true;
            }

            var direction = DirectionTo(World.Location, bot01.Location);
            _connection.Enqueue(new C.Turn { Direction = direction });
            return false;
        }

        if (_index != 1) return false;
        if (_tradeRequestSent && !_tradeActive && now >= _nextTradeAction)
        {
            _tradeRequestSent = false;
            _tradePathRequested = false;
            _nextTradeAction = now.AddSeconds(2);
        }
        if (_tradeRequestSent || now < _nextTradeAction) return false;

        var target = World.Players.Values.FirstOrDefault(x =>
            x.Name.Equals("Bot02", StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            _nextTradeAction = now.AddSeconds(15);
            return false;
        }

        int distance = Distance(World.Location, target.Location);
        if (distance > 1)
        {
            if (_tradeAutoPathAllowed && !_tradePathRequested)
            {
                _connection.Enqueue(new C.AutoPathWaypoint
                {
                    MapIndex = World.MapIndex,
                    Location = target.Location
                });
                _tradePathRequested = true;
            }
            if (now >= _nextMove)
                MoveToward(target.Location, 1, now);
            return true;
        }

        var facing = DirectionTo(World.Location, target.Location);
        if (!_tradeFacingPrimed || World.Direction != facing)
        {
            _connection.Enqueue(new C.Turn { Direction = facing });
            _tradeFacingPrimed = true;
            _nextTradeAction = now.AddSeconds(2);
            return true;
        }

        _connection.Enqueue(new C.TradeRequest());
        _tradeRequestSent = true;
        _tradePathRequested = false;
        _tradeFacingPrimed = false;
        _nextTradeAction = now.AddSeconds(20);
        Console.WriteLine($"[{Name}] trade: request Bot02");
        return true;
    }

    // 练级角色(1/2)的外出循环: 大部分时间在城中心驻留/闲逛/练技,
    // 每隔 FieldTripInterval 秒去北部怪区打 FieldTripDuration 秒怪再回城。
    // 去程/回程由服务端 AutoPath 驱动(_autoPathActive 短路主链), 到达后
    // 由战斗逻辑接管。返回 true 表示本 tick 已发出计划移动。
    private bool TryFieldTripBehavior(DateTime now)
    {
        if (!IsFieldBot(_index)) return false;
        if (World.MapIndex != _config.HomeMapIndex) return false; // 已在其他图, 由对应行为接管

        // 回程中: 到家附近即驻留(距离判到达, 同图无 MapChanged 事件)。
        if (_fieldPathHome)
        {
            if (Distance(World.Location, _homeAnchor) < 12)
            {
                _fieldPathHome = false;
                _fieldTripEnd = DateTime.MinValue;
                _nextFieldTrip = now.AddSeconds(_config.HomeDwellSecondsMin +
                    _random.NextDouble() * (_config.HomeDwellSecondsMax - _config.HomeDwellSecondsMin));
                Console.WriteLine($"[{Name}] field: back in town");
            }
            return false; // AutoPath 仍在走, 主链已短路
        }

        // 去程中: 到怪区锚点附近即开始打怪。
        if (_fieldPathToField)
        {
            if (Distance(World.Location, _fieldAnchor) < 10)
            {
                _fieldPathToField = false;
                _fieldTripEnd = now.AddSeconds(_config.FieldTripDurationSeconds + _random.NextDouble() * 60);
                Console.WriteLine($"[{Name}] field: trip reached, hunting");
            }
            return false;
        }

        // 打怪中: 到点回城; 若已被怪引回城附近则直接驻留。
        if (_fieldTripEnd != DateTime.MinValue)
        {
            if (now >= _fieldTripEnd)
            {
                if (Distance(World.Location, _homeAnchor) < 12)
                {
                    _fieldTripEnd = DateTime.MinValue;
                    _nextFieldTrip = now.AddSeconds(_config.HomeDwellSecondsMin +
                        _random.NextDouble() * (_config.HomeDwellSecondsMax - _config.HomeDwellSecondsMin));
                }
                else
                {
                    Console.WriteLine($"[{Name}] field: trip over, head home");
                    _connection.Enqueue(new C.AutoPathWaypoint { MapIndex = _config.HomeMapIndex, Location = _homeAnchor });
                    _fieldPathHome = true;
                    _nextMove = now.AddSeconds(8);
                }
            }
            return false; // 打怪/寻路中, 战斗逻辑接管
        }

        // 在城驻留结束 → 出发去怪区。
        if (now >= _nextFieldTrip)
        {
            Console.WriteLine($"[{Name}] field: trip to {_fieldAnchor}");
            _connection.Enqueue(new C.AutoPathWaypoint { MapIndex = _config.HomeMapIndex, Location = _fieldAnchor });
            _fieldPathToField = true;
            _nextMove = now.AddSeconds(8);
            return true;
        }
        return false;
    }

    // 城内"练技"表演: 在城中心随机挥刀/放技能, 制造真实玩家在城里
    // 试招的热闹观感。法师/道士的护盾/召唤/治疗由职业准备与治疗逻辑覆盖,
    // 这里统一做物理挥砍动作(C.Attack 无目标, 服务端按方向挥空, 安全)。
    private bool TryTownCastingBehavior(DateTime now)
    {
        if (now < _nextTownCast) return false;
        bool inTown = World.InSafeZone || Distance(World.Location, _homeAnchor) < 25;
        if (!inTown) return false;

        _connection.Enqueue(new C.Attack
        {
            Direction = (MirDirection)_random.Next(8),
            Action = MirAction.Attack,
            AttackMagic = World.Class is MirClass.Warrior or MirClass.Assassin ? SelectAttackSkill() : MagicType.None
        });
        _attackActions++;
        _nextTownCast = now.AddSeconds(_config.TownCastMinSeconds +
            _random.NextDouble() * (_config.TownCastMaxSeconds - _config.TownCastMinSeconds));
        return true;
    }

    private bool TrySupportAlly(DateTime now)
    {
        MagicType heal = World.Magics
            .Where(x => x.Info != null && !x.ItemRequired)
            .Select(x => x.Info.Magic)
            .FirstOrDefault(x => x is MagicType.Heal or MagicType.EmpoweredHealing or MagicType.MassHeal or MagicType.CelestialLight);

        if (heal == MagicType.None) return false;

        uint targetId = World.SelfObjectId;
        Point targetLocation = World.Location;
        int health = World.CurrentHealth;
        int maxHealth = World.MaxHealth;

        var ally = World.Players.Values
            .Where(x => World.GroupMembers.Contains(x.Name) && !x.Dead)
            .Where(x => World.PlayerVitals.TryGetValue(x.ObjectID, out var vital) &&
                        World.PlayerMaxVitals.TryGetValue(x.ObjectID, out var max) &&
                        vital.Health * 100 < Math.Max(1, max.MaxHealth) * 65)
            .OrderBy(x => Distance(World.Location, x.Location))
            .FirstOrDefault(x => Distance(World.Location, x.Location) <= 12);

        if (ally != null && World.PlayerVitals.TryGetValue(ally.ObjectID, out var allyVital))
        {
            targetId = ally.ObjectID;
            targetLocation = ally.Location;
            health = allyVital.Health;
            maxHealth = World.PlayerMaxVitals[ally.ObjectID].MaxHealth;
        }

        if (maxHealth <= 0 || health * 100 >= maxHealth * 65) return false;

        _connection.Enqueue(new C.Magic
        {
            Direction = DirectionTo(World.Location, targetLocation),
            Action = MirAction.Spell,
            Type = heal,
            Target = targetId,
            Location = targetLocation
        });
        return true;
    }

    private bool TryResourceBehavior(DateTime now)
    {
        if (IsPvpBot(_index)) return false;
        // Every fifth bot is a resource specialist. The provisioner gives these
        // bots a real pickaxe; all other bots remain combat/social specialists.
        if (_index % 5 != 0 || now < _nextResourceAction) return false;
        if (_resourceSwapPending)
        {
            _nextResourceAction = now.AddSeconds(3);
            return true;
        }

        var map = Globals.MapInfoList?.Binding.FirstOrDefault(x => x.Index == World.MapIndex);
        int weaponSlot = Globals.EquipmentOffSet + (int)EquipmentSlot.Weapon;
        bool hasPickaxe = World.Inventory.Any(x => x.Info?.ItemEffect == ItemEffect.PickAxe && x.Slot == weaponSlot);
        var sparePickaxe = World.Inventory.FirstOrDefault(x => x.Info?.ItemEffect == ItemEffect.PickAxe && x.Slot != weaponSlot);

        if (!hasPickaxe && sparePickaxe != null)
        {
            var equippedWeapon = World.Inventory.FirstOrDefault(x => x.Slot == weaponSlot && x.Info?.ItemEffect != ItemEffect.PickAxe);
            if (equippedWeapon != null)
            {
                int freeSlot = Enumerable.Range(0, Globals.InventorySize)
                    .FirstOrDefault(slot => World.Inventory.All(x => x.Slot != slot), -1);
                if (freeSlot < 0)
                {
                    _nextResourceAction = now.AddMinutes(1);
                    return false;
                }

                Console.WriteLine($"[{Name}] resource: stow weapon for pickaxe");
                _connection.Enqueue(new C.ItemMove
                {
                    FromGrid = GridType.Equipment,
                    FromSlot = (int)EquipmentSlot.Weapon,
                    ToGrid = GridType.Inventory,
                    ToSlot = freeSlot,
                    MergeItem = false
                });
                _resourceSwapPending = true;
                _nextResourceAction = now.AddSeconds(5);
                return true;
            }

            Console.WriteLine($"[{Name}] resource: equip pickaxe");
            _connection.Enqueue(new C.ItemMove
            {
                FromGrid = GridType.Inventory,
                FromSlot = sparePickaxe.Slot,
                ToGrid = GridType.Equipment,
                ToSlot = (int)EquipmentSlot.Weapon,
                MergeItem = false
            });
            _nextResourceAction = now.AddSeconds(10);
            return true;
        }

        if (World.MapIndex == ResourceMapIndex && map?.CanMine == true && hasPickaxe)
        {
            if (_resourceTripEnd == DateTime.MinValue)
                _resourceTripEnd = now.AddMinutes(2);

            if (now < _resourceTripEnd)
            {
                // 矿洞站桩挖矿 2 分钟, 中途低血先喝药再继续, 避免被打死。
                if (ShouldUseConsumable())
                {
                    var potion = World.Inventory
                        .Where(x => x.Count > 0 && x.Info != null && x.Info.CanAutoPot)
                        .OrderByDescending(x => IsManaPotion(x.Info) == NeedsManaPotion())
                        .FirstOrDefault();
                    if (potion != null)
                    {
                        if (potion.Slot < 0)
                        {
                            _connection.Enqueue(new C.ItemSort { Grid = GridType.Inventory });
                            _nextResourceAction = now.AddSeconds(8);
                            return true;
                        }
                        _connection.Enqueue(new C.ItemUse
                        {
                            Link = new CellLinkInfo { GridType = GridType.Inventory, Slot = potion.Slot, Count = 1 }
                        });
                        _nextResourceAction = now.AddSeconds(1.5);
                        return true;
                    }
                }

                _connection.Enqueue(new C.Mining { Direction = (MirDirection)_random.Next(8) });
                _nextResourceAction = now.AddSeconds(1.1 + _random.NextDouble() * 0.7);
                return true;
            }

            Point home = _homeAnchor;
            _connection.Enqueue(new C.AutoPathWaypoint { MapIndex = _config.HomeMapIndex, Location = home });
            _resourcePathHome = true;
            _resourcePathToMine = false;
            _nextResourceAction = now.AddSeconds(10);
            return true;
        }

        var targetMap = Globals.MapInfoList?.Binding.FirstOrDefault(x => x.Index == ResourceMapIndex);
        Point miningPoint = FindMiningPoint(targetMap);
        if (targetMap == null || miningPoint == Point.Empty)
        {
            _nextResourceAction = now.AddMinutes(1);
            return false;
        }

        if (World.MapIndex != ResourceMapIndex && !_resourcePathToMine && !_resourcePathHome)
        {
            Console.WriteLine($"[{Name}] resource: path to mine map {ResourceMapIndex} at {miningPoint}");
            _connection.Enqueue(new C.AutoPathWaypoint { MapIndex = ResourceMapIndex, Location = miningPoint });
            _resourcePathToMine = true;
            _nextResourceAction = now.AddSeconds(10);
            return true;
        }

        if (World.MapIndex == _config.HomeMapIndex && _resourcePathHome)
        {
            _resourcePathHome = false;
            _resourceTripEnd = DateTime.MinValue;
            // 回城后驻留 4~8 分钟(卖矿/补给/闲逛/练技), 让城中心长期有人。
            _nextResourceAction = now.AddSeconds(240 + _random.NextDouble() * 240);
            return true;
        }

        _nextResourceAction = now.AddSeconds(10);
        return true;
    }

    private Point FindMiningPoint(MapInfo map)
    {
        if (map?.Mining == null) return Point.Empty;
        string path = Path.Combine(_config.MapPath, $"{map.FileName}.map");
        if (!File.Exists(path)) return Point.Empty;

        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);
            stream.Seek(22, SeekOrigin.Begin);
            int width = reader.ReadInt16();
            if (width <= 0) return Point.Empty;

            foreach (var mine in map.Mining.OrderByDescending(x => x.Chance))
            {
                var points = mine.Region?.GetPoints(width);
                if (points == null || points.Count == 0) continue;
                return points.ElementAt(_random.Next(points.Count));
            }

            // The current database defines the legacy mine tables without a
            // region, which means every valid cell on that map can be mined.
            // Pick a real walkable cell from the map file for auto-pathing.
            var candidates = new List<Point>();
            stream.Seek(24, SeekOrigin.Begin);
            int height = reader.ReadInt16();
            stream.Seek(28 + (width / 2) * (height / 2) * 3, SeekOrigin.Begin);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte flag = reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadByte();
                    reader.ReadUInt16();
                    reader.ReadUInt16();
                    reader.ReadBytes(3);
                    reader.ReadByte();
                    reader.ReadByte();
                    if ((flag & 0x03) == 0x03) candidates.Add(new Point(x, y));
                }
            }
            if (candidates.Count > 0)
                return candidates[_random.Next(candidates.Count)];
        }
        catch (IOException) { }
        catch (ArgumentException) { }

        return Point.Empty;
    }

    private void HandleNpcResponse(S.NPCResponse response)
    {
        if (response == null) return;
        _npcCallPending = false;
        _npcObjectId = response.ObjectID;
        var page = response.Page ?? Globals.NPCPageList?.Binding.FirstOrDefault(x => x.Index == response.Index);
        if (page == null) return;

        var npcInfo = World.Npcs.Values
            .Where(x => x.ObjectID == response.ObjectID)
            .Select(x => Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == x.NPCIndex))
            .FirstOrDefault(x => x != null);
        if (npcInfo != null)
        {
            if (npcInfo.StartQuests != null)
                foreach (var quest in npcInfo.StartQuests)
                    _connection.Enqueue(new C.QuestAccept { Index = quest.Index });
            if (npcInfo.FinishQuests != null)
                foreach (var quest in npcInfo.FinishQuests)
                    _connection.Enqueue(new C.QuestComplete { Index = quest.Index, ChoiceIndex = 0 });
        }

        // 仅在本次 NPCResponse 确实是 repair 的 NPCCall 响应时点 Repair 按钮
        // (时间节流不可靠: repair 块 NPCCall 后 8s 内 NPCResponse 到达,
        // _nextRepairAction 还没到期会漏点, 造成 repair NPCCall 风暴反复清页,
        // 打断商店购买)。supply 的响应时 _repairCallPending=false, 不会抢页。
        if (page.DialogType != NPCDialogType.Repair && !_supplyPurchasePending && _repairCallPending)
        {
            var repairButton = page.Buttons?.FirstOrDefault(x => x.DestinationPage?.DialogType == NPCDialogType.Repair);
            if (repairButton != null && NeedsRepair())
            {
                _connection.Enqueue(new C.NPCButton { ButtonID = repairButton.ButtonID });
                _repairCallPending = false;
                _nextRepairAction = DateTime.UtcNow.AddSeconds(60);
            }
        }

        if (page.DialogType != NPCDialogType.BuySell)
        {
            var buyButton = page.Buttons?.FirstOrDefault(x => x.DestinationPage?.DialogType == NPCDialogType.BuySell);
            if (buyButton != null && (_supplyPurchasePending || NeedsPotionSupply() || NeedsClassSupplies()))
                _connection.Enqueue(new C.NPCButton { ButtonID = buyButton.ButtonID });
        }

        if (page.DialogType == NPCDialogType.BuySell && (_supplyPurchasePending || NeedsPotionSupply() || NeedsClassSupplies()) &&
            (_supplyPurchasePending || DateTime.UtcNow >= _nextSupplyAction))
        {
            if (_supplyPurchasePending)
                Console.WriteLine($"[{Name}] shop: candidates={string.Join(",", page.Goods?.Where(x => x.Item != null).Select(x => $"{x.Item.ItemName}:{x.Item.ItemType}") ?? Enumerable.Empty<string>())}");
            // 跨图缺回城卷时优先补卷, 否则药水排序会一直压过卷轴,
            // 导致困在陌生地图(如竞技场)买不到卷回城。
            // Count>0: 用尽的卷轴条目(缓存残留)不算有卷, 否则永远补不上。
            bool needPortal = World.MapIndex != _config.HomeMapIndex &&
                !World.Inventory.Any(x => x?.Info != null && x.Count > 0 &&
                    x.Info.ItemType == ItemType.Consumable && x.Info.Shape == 2 &&
                    x.Info.ItemName.Contains("Town Portal", StringComparison.OrdinalIgnoreCase));
            var potion = page.Goods?.Where(x => x.Item != null)
                .Where(x => x.Item.CanAutoPot || x.Item.ItemType == ItemType.Scroll ||
                    (x.Item.ItemType == ItemType.Consumable && x.Item.Shape == 2) ||
                    (x.Item.ItemType == ItemType.Amulet && World.Class == MirClass.Taoist))
                .Where(x => x.Item.CanAutoPot || x.Item.ItemType is ItemType.Scroll or ItemType.Amulet ||
                    (x.Item.ItemType == ItemType.Consumable && x.Item.Shape == 2))
                .OrderByDescending(x => needPortal && x.Item.ItemType == ItemType.Consumable && x.Item.Shape == 2 &&
                    x.Item.ItemName?.Contains("Town Portal", StringComparison.OrdinalIgnoreCase) == true)
                .ThenByDescending(x => x.Item.CanAutoPot && NeedsPotionSupply())
                .ThenBy(x => x.Index)
                .FirstOrDefault();
            if (potion?.Item != null)
            {
                long amount = potion.Item.CanAutoPot ? (_supplyPurchasePending ? 1 : 5)
                    : potion.Item.ItemType == ItemType.Consumable && potion.Item.Shape == 2 ? 3 : 1;
                _connection.Enqueue(new C.NPCBuy { Index = potion.Index, Amount = amount, GuildFunds = false });
                _nextSupplyAction = DateTime.UtcNow.AddSeconds(90);
                _supplyPurchasePending = false;
                _shopPurchases++;
                Console.WriteLine($"[{Name}] shop: buy {potion.Item.ItemName} x{amount} gold={World.Gold}");
            }
            else if (_supplyPurchasePending)
                Console.WriteLine($"[{Name}] shop: no suitable goods on this page");
        }

        if (page.DialogType == NPCDialogType.BuySell && !_supplyPurchasePending)
        {
            var sellTypes = page.Types?.Select(x => x.ItemType).ToHashSet() ?? new HashSet<ItemType>();
            var sellLinks = World.Inventory
                .Where(x => x?.Info != null && x.Count > 0 && x.Slot >= 0)
                .Where(x => !IsEquipped(x) && x.Info.CanSell && sellTypes.Contains(x.Info.ItemType))
                .Where(x => x.Info.Rarity != Rarity.Elite)
                .Where(x => x.Info.ItemType is ItemType.Weapon or ItemType.Armour or ItemType.Helmet or
                    ItemType.Necklace or ItemType.Bracelet or ItemType.Ring or ItemType.Shoes or ItemType.Shield or
                    ItemType.Ore or ItemType.Meat or ItemType.Flower or ItemType.DarkStone)
                .Take(8)
                .Select(x => new CellLinkInfo { GridType = GridType.Inventory, Slot = x.Slot, Count = Math.Min(x.Count, 20) })
                .ToList();
            if (sellLinks.Count > 0)
            {
                _connection.Enqueue(new C.NPCSell { Links = sellLinks });
                _shopSales++;
                Console.WriteLine($"[{Name}] shop: sell {sellLinks.Count} item stacks");
            }
        }

        if (page.DialogType != NPCDialogType.Repair) return;
        var links = World.Inventory
            .Where(x => x.Info != null && IsEquipped(x) && x.Info.CanRepair)
            .Where(x => x.CurrentDurability < x.MaxDurability && x.MaxDurability > 0)
            .Where(x => x.Info.ItemType is ItemType.Weapon or ItemType.Armour or ItemType.Helmet or ItemType.Necklace
                or ItemType.Bracelet or ItemType.Ring or ItemType.Shoes or ItemType.Shield)
            .Select(x => new CellLinkInfo
            {
                GridType = GridType.Equipment,
                Slot = x.Slot - Globals.EquipmentOffSet,
                Count = 1
            })
            .ToList();
        if (links.Count > 0)
        {
            _connection.Enqueue(new C.NPCRepair { Links = links, Special = false, GuildFunds = false });
            _nextRepairAction = DateTime.UtcNow.AddSeconds(90);
        }
    }

    private bool NeedsPotionSupply()
        => World.Inventory.Where(x => x.Info?.CanAutoPot == true).Sum(x => Math.Max(0, x.Count)) < 5;

    private bool NeedsClassSupplies()
    {
        if (World.Class == MirClass.Taoist &&
            World.Inventory.Where(x => x.Info?.ItemType == ItemType.Amulet).Sum(x => Math.Max(0, x.Count)) < 20)
            return true;
        if (World.Inventory.All(x => x.Info?.ItemType != ItemType.Scroll))
            return true;
        // 跨图回城卷轴(Consumable Shape==2): 备 3 张, 免于卡在异地
        return World.Inventory
            .Where(x => x.Info?.ItemType == ItemType.Consumable && x.Info.Shape == 2)
            .Sum(x => Math.Max(0, x.Count)) < 3;
    }

    private bool ShouldUseConsumable()
    {
        bool lowHealth = World.MaxHealth > 0 && World.CurrentHealth * 100 < World.MaxHealth * 45;
        bool lowMana = World.MaxMana > 0 && World.CurrentMana * 100 < World.MaxMana * 35;
        return lowHealth || (lowMana && World.Class is MirClass.Wizard or MirClass.Taoist);
    }

    private bool NeedsManaPotion()
        => World.MaxMana > 0 && World.CurrentMana * 100 < World.MaxMana * 35 &&
           !(World.MaxHealth > 0 && World.CurrentHealth * 100 < World.MaxHealth * 45);

    private static bool IsManaPotion(ItemInfo info)
        => info.ItemEffect == ItemEffect.ManaElixir ||
           info.ItemName?.Contains("Mana", StringComparison.OrdinalIgnoreCase) == true ||
           info.ItemName?.Contains("魔", StringComparison.OrdinalIgnoreCase) == true;

    private bool IsClassSuitable(ItemInfo info)
    {
        if (info == null) return false;
        RequiredClass required = World.Class switch
        {
            MirClass.Warrior => RequiredClass.Warrior,
            MirClass.Wizard => RequiredClass.Wizard,
            MirClass.Taoist => RequiredClass.Taoist,
            MirClass.Assassin => RequiredClass.Assassin,
            _ => RequiredClass.All
        };
        return info.RequiredClass == RequiredClass.None || info.RequiredClass == RequiredClass.All ||
               (info.RequiredClass & required) != 0;
    }

    private bool TrySupplyBehavior(DateTime now)
    {
        if (now < _nextSupplyAction) return false;
        if (now >= _nextSupplyDiag)
        {
            _nextSupplyDiag = now.AddSeconds(60);
            var nearest = World.Npcs.Values
                .Select(x => (Dist: Distance(World.Location, x.CurrentLocation),
                              Name: Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == x.NPCIndex)?.NPCName ?? "?"))
                .OrderBy(x => x.Dist).FirstOrDefault();
            Console.WriteLine($"[{Name}] supply: called nextSupply={(_nextSupplyAction - now).TotalSeconds:F0}s npcs={World.Npcs.Count} nearest={nearest.Name}@{nearest.Dist} loc={World.Location}");
        }
        // A failed/unfinished AutoPathStart must not permanently block the
        // supply state machine. The server remains the authority; we simply
        // retry at the next slow interval.
        _supplyPurchasePending = false;

        var npc = World.Npcs.Values
            .Select(x => (Object: x, Info: Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == x.NPCIndex)))
            .Where(x => x.Info != null && HasSupplyShop(x.Info))
            .OrderBy(x => SellsTownPortal(x.Info) ? 0 : 1)
            .ThenBy(x => Distance(World.Location, x.Object.CurrentLocation))
            .FirstOrDefault(x => Distance(World.Location, x.Object.CurrentLocation) <= Math.Max(20, _config.PatrolRadius * 2));
        if (npc.Object == null)
        {
            var supplyNpc = Globals.NPCInfoList?.Binding.FirstOrDefault(HasSupplyShop);
            if (supplyNpc == null) return false;
            // 跨图时本地无供给 NPC。AutoPathWaypoint(跨图)必失败, 但
            // AutoPathStart(卖卷 NPC) 的寻路目标是 NPC 的 Region——若该 NPC
            // 就在本图(竞技场内的 Lavar)则同图寻路可行; 失败由服务器报错
            // (chat: 无法找到自动寻路路线), BotRunner 下轮改用手动移动兜底。
            if (World.MapIndex != _config.HomeMapIndex)
            {
                var portalNpc = Globals.NPCInfoList?.Binding.FirstOrDefault(SellsTownPortal);
                if (portalNpc != null)
                {
                    _supplyInteractionUntil = now.AddSeconds(30);
                    _npcCallPending = true;
                    _nextSupplyAction = now.AddSeconds(30);
                    _connection.Enqueue(new C.AutoPathStart { NPCIndex = portalNpc.Index });
                    Console.WriteLine($"[{Name}] shop: auto-path portal NPC {portalNpc.NPCName}");
                    return true;
                }
                if (now >= _nextSupplyDiag)
                {
                    _nextSupplyDiag = now.AddSeconds(60);
                    var nearest = World.Npcs.Values
                        .Select(x => (Dist: Distance(World.Location, x.CurrentLocation), Name: Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == x.NPCIndex)?.NPCName ?? "?"))
                        .OrderBy(x => x.Dist)
                        .FirstOrDefault();
                    Console.WriteLine($"[{Name}] supply: no local shop npc, npcs={World.Npcs.Count} nearest={nearest.Name}@{nearest.Dist}");
                }
                return false;
            }
            _supplyPurchasePending = true;
            _supplyInteractionUntil = now.AddSeconds(30);
            _npcCallPending = true;
            _nextSupplyAction = now.AddSeconds(120);
            _connection.Enqueue(new C.AutoPathStart { NPCIndex = supplyNpc.Index });
            Console.WriteLine($"[{Name}] shop: auto-path supply NPC {supplyNpc.NPCName}");
            return true;
        }

        _npcObjectId = npc.Object.ObjectID;
        if (Distance(World.Location, npc.Object.CurrentLocation) > 2)
        {
            if (now >= _nextMove) MoveToward(npc.Object.CurrentLocation, 1, now);
            return true;
        }

        _connection.Enqueue(new C.NPCCall { ObjectID = _npcObjectId });
        _supplyPurchasePending = true;
        _supplyInteractionUntil = now.AddSeconds(30);
        _npcCallPending = true;
        _repairCallPending = false;
        _nextSupplyAction = now.AddSeconds(120);
        Console.WriteLine($"[{Name}] shop: approach NPC {npc.Info.NPCName}");
        return true;
    }

    private bool TrySellBehavior(DateTime now)
    {
        if (now < _nextSellAction) return false;
        // 跨图时先补给回城卷轴, 不卖东西(本地卖店可能太远触发跨图寻路失败)
        if (World.MapIndex != _config.HomeMapIndex) return false;

        var npc = World.Npcs.Values
            .Select(x => (Object: x, Info: Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == x.NPCIndex)))
            .Where(x => x.Info != null && HasSellShop(x.Info))
            .OrderBy(x => Distance(World.Location, x.Object.CurrentLocation))
            .FirstOrDefault(x => Distance(World.Location, x.Object.CurrentLocation) <= Math.Max(20, _config.PatrolRadius * 2));
        if (npc.Object == null)
        {
            var sellNpc = Globals.NPCInfoList?.Binding.FirstOrDefault(HasSellShop);
            if (sellNpc == null) return false;
            _nextSellAction = now.AddSeconds(180 + _random.NextDouble() * 60);
            _connection.Enqueue(new C.AutoPathStart { NPCIndex = sellNpc.Index });
            Console.WriteLine($"[{Name}] shop: auto-path sell NPC {sellNpc.NPCName}");
            return true;
        }

        _npcObjectId = npc.Object.ObjectID;
        _nextSellAction = now.AddSeconds(180 + _random.NextDouble() * 60);
        if (Distance(World.Location, npc.Object.CurrentLocation) > 2)
        {
            if (now >= _nextMove) MoveToward(npc.Object.CurrentLocation, 1, now);
            return true;
        }

        _connection.Enqueue(new C.NPCCall { ObjectID = _npcObjectId });
        Console.WriteLine($"[{Name}] shop: sell visit NPC {npc.Info.NPCName}");
        return true;
    }

    private static bool HasSellShop(NPCInfo info)
    {
        if (info?.EntryPage == null) return false;
        var pages = new[] { info.EntryPage }
            .Concat(info.EntryPage.Buttons?.Where(x => x.DestinationPage != null).Select(x => x.DestinationPage) ?? Enumerable.Empty<NPCPage>());
        return pages.Any(x => x.DialogType == NPCDialogType.BuySell && x.Types?.Count > 0);
    }

    private static bool SellsTownPortal(NPCInfo info)
    {
        if (info?.EntryPage == null) return false;
        var pages = new[] { info.EntryPage }
            .Concat(info.EntryPage.Buttons?.Where(x => x.DestinationPage != null).Select(x => x.DestinationPage) ?? Enumerable.Empty<NPCPage>());
        return pages.Any(x => x.DialogType == NPCDialogType.BuySell &&
            x.Goods?.Any(g => g.Item?.ItemType == ItemType.Consumable && g.Item.Shape == 2 &&
                g.Item.ItemName?.Contains("Town Portal", StringComparison.OrdinalIgnoreCase) == true) == true);
    }

    private static bool HasSupplyShop(NPCInfo info)
    {
        if (info?.EntryPage == null) return false;
        var pages = new[] { info.EntryPage }
            .Concat(info.EntryPage.Buttons?.Where(x => x.DestinationPage != null).Select(x => x.DestinationPage) ?? Enumerable.Empty<NPCPage>());
        return pages.Any(x => x.DialogType == NPCDialogType.BuySell &&
            x.Goods?.Any(g => g.Item?.CanAutoPot == true || g.Item?.ItemType is ItemType.Scroll or ItemType.Amulet ||
                (g.Item?.ItemType == ItemType.Consumable && g.Item.Shape == 2)) == true);
    }

    private bool NeedsRepair() => World.Inventory.Any(x => x.Info != null && IsEquipped(x) &&
        x.Info.CanRepair && x.MaxDurability > 0 && x.CurrentDurability < x.MaxDurability * 0.75 &&
        x.Info.ItemType is ItemType.Weapon or ItemType.Armour or ItemType.Helmet or ItemType.Necklace or
        ItemType.Bracelet or ItemType.Ring or ItemType.Shoes or ItemType.Shield);

    private static bool IsEquipped(ClientUserItem item)
        => item.Slot >= Globals.EquipmentOffSet && item.Slot < Globals.EquipmentOffSet + 10;

    private void Fail(string reason)
    {
        if (Status == BotStatus.Failed) return;
        Status = BotStatus.Failed;
        Console.WriteLine($"[{Name}] failed: {reason}");
    }

    private static string DescribeException(Exception exception)
    {
        return exception.ToString();
    }

    private static int Distance(Point a, Point b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    private static MirDirection DirectionTo(Point from, Point to)
    {
        int x = Math.Sign(to.X - from.X), y = Math.Sign(to.Y - from.Y);
        return (x, y) switch
        {
            (0, -1) => MirDirection.Up, (1, -1) => MirDirection.UpRight, (1, 0) => MirDirection.Right,
            (1, 1) => MirDirection.DownRight, (0, 1) => MirDirection.Down, (-1, 1) => MirDirection.DownLeft,
            (-1, 0) => MirDirection.Left, (-1, -1) => MirDirection.UpLeft, _ => MirDirection.Down
        };
    }
}
