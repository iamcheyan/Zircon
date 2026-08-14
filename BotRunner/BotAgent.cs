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
    private bool _accountReady;
    private BotConnection _connection;
    private DateTime _nextMove;
    private DateTime _nextChat;
    private DateTime _nextAttack;
    private DateTime _nextPotion;
    private Point _homeAnchor;
    private DateTime _crossMapStuckSince = DateTime.MinValue;
    private DateTime _nextCrossMapDiag;
    private DateTime _nextSupplyDiag;
    // 跨图 ItemUse 回城卷的追踪: 用卷后一段时间仍非家图 = 卷传送无效
    // (BindPoint 被职业安全区/红区改写, 如刺客巢穴 459), 停止烧卷并重登。
    private DateTime _portalUseAt = DateTime.MinValue;
    private int? _portalUseSlot;
    private uint _targetMonsterId;
    private DateTime _nextTargetScan;
    private DateTime _nextTorchAction;
    private DateTime _nextRepairAction;
    private DateTime _nextQuestAction;
    private DateTime _nextSupplyAction;
    private DateTime _nextSellAction;
    private bool _supplyPurchasePending;
    private Point? _supplyTravelDest;
    private DateTime _supplyTravelUntil = DateTime.MinValue;
    private DateTime _supplyInteractionUntil = DateTime.MinValue;
    private bool _sellAutopathBlocked;
    private DateTime _nextAmuletAction = DateTime.MinValue;
    private int _npcPageHops;
    private bool _supplyAutopathBlocked;
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
    private DateTime _nextGuildAction;
    private bool _starterGuildAttempted;
    private DateTime _nextContainerAction;
    private int _containerSlot = -1;
    private DateTime _nextFishingAction;
    private bool _fishingActive;
    private Point _fishingPoint;
    private bool _tradeActive;
    private DateTime _nextPvpAction;
    private DateTime _pvpRoundEnd;
    private Point _pvpStagingPoint;
    private int _pvpActions;
    private DateTime _nextActivityReport;
    private const int ResourceMapIndex = 136;
    private int _moveActions;
    private int _attackActions;
    private int _magicActions;
    private int _targetSelections;
    private int _combatActions;
    private int _pickupRequests;
    private int _itemsGainedEvents;
    private uint _npcObjectId;
    private bool _autoPathActive;
    private bool _travelActive;
    private Point _travelDest;
    private int _travelMapIndex;
    private DateTime _travelSince = DateTime.MinValue;
    private DateTime _nextTravelDebug;
    private int _crossMapFailCount;
    private DateTime _crossMapGraceUntil = DateTime.MinValue;
    private readonly int _index;
    private BotMap _map;
    private string _mapFile = string.Empty;
    private int _reconnectAttempt;

    public string Name { get; }
    private string CharacterName => $"{_config.AccountPrefix}{_index:00}";
    public BotStatus Status { get; private set; } = BotStatus.Created;
    public BotWorld World { get; } = new();

    // ==== 拟真行为系统 ====
    public BotProfile Profile { get; }
    public BotConfig Config => _config;
    public BotWorld WorldState => World;
    public BotConnection Connection => _connection;
    public Random Rng => _random;
    public Point HomeAnchor => _homeAnchor;
    private BotBehaviorScheduler _scheduler;
    private SafeZoneTrainingBehavior _trainBehavior;
    private GrindFarmingBehavior _grindBehavior;
    private GroupPlayBehavior _groupBehavior;
    private EquipUpgradeBehavior _equipBehavior;
    private RestIdleBehavior _restBehavior;
    private PatrolFallbackBehavior _patrolBehavior;
    private BotPathfinder _pathfinder;
    private Point _pathGoal = Point.Empty;
    private DateTime _nextBehaviorLog = DateTime.MinValue;
    private string _lastBehavior = "";
    private Point _pendingStepGoal;
    private int _blacklistFailStreak;
    private Point _lastPathFailGoal;
    private readonly Dictionary<Point, DateTime> _blockedAt = new();
    private readonly HashSet<Point> _runtimeBlocked = new();
    private Point _pendingStep;
    private Point _pendingStepFrom;
    private Point _pendingStepObserved;
    private DateTime _surroundedSince = DateTime.MinValue;
    private DateTime _pendingStepAt = DateTime.MinValue;
    private readonly Dictionary<Point, (Point Step, Point From, DateTime At)> _rejectTracker = new();
    private int _trainDeferCount;
    private int _regionNpcIndex;
    private Point _regionPointCache;
    private DateTime _regionPointAt = DateTime.MinValue;
    private DateTime _pathFailRetryAt;
    private DateTime _nextEquipShopBuy = DateTime.MinValue;
    private DateTime _nextBreakoutLog = DateTime.MinValue;
    private DateTime _nextPathFailLog = DateTime.MinValue;
    private DateTime _nextChatCorpus = DateTime.MinValue;
    private DateTime _lastPositionSample = DateTime.MinValue;
    private Point _lastSampledPosition;
    private int _positionStallTicks;
    private int _stuckRecoveries;
    private string _groupLeaderName = "";
    private Point _lastPathPosition;

    public BotAgent(int index, BotConfig config)
    {
        _config = config;
        _index = index;
        _random = new Random(1000 + index * 7919);
        Name = $"Bot{index:00}";
        _email = $"{config.AccountPrefix}{index:00}@bot.local";
        _password = config.Password;
        Profile = BotProfile.Create(index, config);

        _trainBehavior = new SafeZoneTrainingBehavior();
        _grindBehavior = new GrindFarmingBehavior();
        _groupBehavior = new GroupPlayBehavior();
        _equipBehavior = new EquipUpgradeBehavior();
        _restBehavior = new RestIdleBehavior();
        _patrolBehavior = new PatrolFallbackBehavior();
        _scheduler = new BotBehaviorScheduler(new IBotBehavior[]
        {
            _trainBehavior, _grindBehavior, _groupBehavior, _equipBehavior,
            _restBehavior, _patrolBehavior,
        }, config.BehaviorSwitchRatio);
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
                if (_config.AutoCreateAccount)
                {
                    _connection.Enqueue(new C.NewAccount
                    {
                        EMailAddress = _email,
                        Password = _password,
                        RealName = Name,
                        BirthDate = new DateTime(1990, 1, 1),
                        Referral = string.Empty,
                        CheckSum = string.Empty
                    });
                }
                else
                    QueueLogin();
                break;
            case S.NewAccount account:
                if (account.Result is NewAccountResult.Success or NewAccountResult.AlreadyExists)
                {
                    _accountReady = true;
                    Console.WriteLine($"[{Name}] account ready ({account.Result})");
                    QueueLogin();
                }
                else
                    Fail($"account creation failed: {account.Result}");
                break;
            case S.Login login:
                if (login.Result != LoginResult.Success)
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
                if (login.Characters is { Count: > 0 })
                    QueueStartGame(login.Characters[0]);
                else if (_config.AutoCreateAccount)
                {
                    // 职业按序号轮转(与 BotProvisioner 种子规则一致),
                    // 保证 4 职业分布均匀: 战士/法师/道士/刺客。
                    var cls = BotProfile.ClassForIndex(_index);
                    _connection.Enqueue(new C.NewCharacter
                    {
                        CharacterName = CharacterName,
                        Class = cls,
                        Gender = (MirGender)(_index % 2),
                        HairType = 1 + _index % 9,
                        HairColour = Color.FromArgb(255, 60 + _index * 7 % 180, 40 + _index * 11 % 160, 30 + _index * 13 % 140),
                        ArmourColour = cls == MirClass.Assassin ? Color.FromArgb(0, 0, 0, 0) : Color.White,
                        CheckSum = string.Empty
                    });
                }
                else
                    Fail("login succeeded but account has no characters");
                break;
            case S.NewCharacter character:
                if (character.Result == NewCharacterResult.Success && character.Character != null)
                    QueueStartGame(character.Character);
                else if (character.Result == NewCharacterResult.AlreadyExists)
                    QueueLogin();
                else
                    Fail($"character creation failed: {character.Result}");
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
            case S.GroupMember p:
                World.Apply(p);
                // 第一个收到的成员即队长(服务端建队时先广播队长)
                if (!string.IsNullOrWhiteSpace(p.Name) && World.GroupMembers.Count <= 1)
                    _groupLeaderName = p.Name;
                if (World.GroupMembers.Count >= 2 && _groupLeaderName == Name)
                    Console.WriteLine($"[{Name}] group: formed, members={string.Join(",", World.GroupMembers)}");
                break;
            case S.GroupRemove p:
                World.Apply(p);
                if (p.ObjectID == World.SelfObjectId) _groupLeaderName = "";
                break;
            case S.MagicLeveled p:
                World.Apply(p);
                if (p.Level > 0 && p.Level != World.Magics.FirstOrDefault(x => x.Info?.Index == p.InfoIndex)?.Level)
                    Console.WriteLine($"[{Name}] skill: {p.Info?.Name} leveled to {p.Level}");
                break;
            case S.NewMagic p: World.Apply(p); break;
            case S.MagicCooldown p: World.Apply(p); break;
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
                break;
            case S.TradeUnlock:
                _tradeActive = false;
                break;
            case S.BundleOpen p:
                HandleBundleOpen(p);
                break;
            case S.JoinInstance p:
                Console.WriteLine($"[{Name}] instance: {(p.Success ? "joined" : $"rejected ({p.Result})")}");
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
                    // 服务器寻路失败: 清状态让行为层换目标/走回城兜底链;
                    // 同时给卖/买链退避, 改走手动寻路兜底(见 TrySell/Supply)。
                    _autoPathActive = false;
                    _resourcePathToMine = false;
                    _resourcePathHome = false;
                    _pathGoal = Point.Empty;
                    _pathfinder?.Reset();
                    _nextResourceAction = DateTime.UtcNow.AddMinutes(2 + _random.NextDouble() * 3);
                    _sellAutopathBlocked = true;
                    _supplyAutopathBlocked = true;
                    var backoff = DateTime.UtcNow.AddSeconds(60 + _random.NextDouble() * 60);
                    if (_nextSellAction < backoff) _nextSellAction = backoff;
                    if (_nextSupplyAction < backoff) _nextSupplyAction = backoff;
                }
                // Chat is still sent to the server, but echoing every nearby
                // player's line from all 8 clients hides the useful behavior
                // telemetry. Keep only system/error-like messages here.
                if (p.Text.StartsWith("你", StringComparison.Ordinal) || p.Text.Contains("无法", StringComparison.Ordinal))
                    Console.WriteLine($"[{Name}] chat: {p.Text}");
                break;
        }
    }

    private void QueueLogin()
    {
        if (_accountReady || !_config.AutoCreateAccount)
            _connection.Enqueue(new C.Login { EMailAddress = _email, Password = _password, CheckSum = string.Empty });
    }

    private void QueueStartGame(SelectInfo character)
    {
        _connection.Enqueue(new C.StartGame { CharacterIndex = character.CharacterIndex });
        Status = BotStatus.Starting;
        _startRequestedAt = DateTime.UtcNow + StartResponseTimeout;
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

        // ---- 背景反应层(与行为调度并行, 每 tick 都跑) ----
        if (World.Dead)
        {
            if (now >= _nextAttack)
            {
                // 回城复活: 世界坐标整体重置, 拉黑条目全部失效
                _runtimeBlocked.Clear();
                _blockedAt.Clear();
                _connection.Enqueue(new C.TownRevive());
                _nextAttack = now.AddSeconds(5);
            }
            return;
        }

        // 道士护符穿戴必须最先执行: 后续任意层返回/goto 都会跳过层尾的
        // 周期块, 导致买了 200 符却永远不穿。
        if (now >= _nextAmuletAction)
        {
            TryEquipAmulet();
            _nextAmuletAction = now.AddSeconds(8);
        }

        // ---- 破围层: 被静态怪(城镇动物等)围死时砍开一条路 ----
        if (TryBreakout(now)) return;


        // ---- 计划性优先层(保留原有可靠机制) ----
        // PvP 回合(人格 PvP 角色) → 计划外跨图回城兜底 → 供给链(买药/修装/
        // 卖垃圾) → 生活玩法(挖矿/钓鱼)。这些吃满一个 tick 就返回。
        if (TryPvPBehavior(now)) goto AfterMovement;

        // 任何非主城地图滞留兜底: 连续 ~10 分钟回不去(无卷/副本/寻路无路/
        // 供给断)就主动重登, 重登后 SetBindPoint 会把绑定点重选为主城。
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

        // 服务端 AutoPath 走线中(跨图): 短路行为层, 等到达/失败。
        if (_autoPathActive && World.MapIndex == _config.HomeMapIndex) goto AfterMovement;

        // 本图长距离行程(本地 A* 驱动): 每 tick 朝目的地走一步, 到达
        // (≤8 格)/换图/超时(6 分钟, 商店区在城东南 250+ 格)后交还行为层。
        if (_travelActive)
        {
            if (World.MapIndex != _travelMapIndex ||
                DistanceTo(_travelDest) <= 8 ||
                (now - _travelSince).TotalMinutes > 6)
            {
                _travelActive = false;
                Log($"travel: done/abort (dist={DistanceTo(_travelDest)})");
            }
            else
            {
                if (now >= _nextTravelDebug)
                {
                    _nextTravelDebug = now.AddSeconds(10);
                    Log($"travel: dist={DistanceTo(_travelDest)} nextMoveIn={(_nextMove - now).TotalSeconds:F1}s canMove={CanMove(now)} path={_pathfinder?.HasPath == true} goal={_pathGoal}");
                }
                if (CanMove(now)) MoveToDestination(_travelDest, now);
                goto AfterMovement;
            }
        }

        if (World.MapIndex == _config.HomeMapIndex && _crossMapStuckSince != DateTime.MinValue)
            _crossMapStuckSince = DateTime.MinValue;
        if (World.MapIndex == _config.HomeMapIndex && _portalUseAt != DateTime.MinValue)
        {
            _portalUseAt = DateTime.MinValue;
            _portalUseSlot = null;
        }

        if (TryCrossMapReturn(now)) return;

        if (!_starterGuildAttempted && now >= _nextGuildAction)
        {
            _connection.Enqueue(new C.JoinStarterGuild());
            _starterGuildAttempted = true;
            Console.WriteLine($"[{Name}] social: join starter guild");
        }

        // 供给链(药水<30%/背包满/耐久低): 回城 → 商店 → 买/修/卖。保留
        // 原有可靠的 NPCCall/AutoPathStart 实现, 作为行为系统的"补给"层。
        if (TryTownServices(now)) goto AfterMovement;

        // 生活玩法(悠闲型: 挖矿/钓鱼/开包)沿用原有实现
        if (Profile.Lifestyle)
        {
            if (TryResourceBehavior(now)) goto AfterMovement;
            if (TryFishingBehavior(now)) goto AfterMovement;
        }
        if (TryContainerBehavior(now)) goto AfterMovement;

        // ---- 拟真行为调度层(utility) ----
        var behavior = _scheduler.Pick(this, now);
        behavior?.Execute(this, now);

        // 行为切换观测日志(节流)
        if (_scheduler.Current != _lastBehavior && now >= _nextBehaviorLog)
        {
            _nextBehaviorLog = now.AddSeconds(10);
            _lastBehavior = _scheduler.Current;
        }

    AfterMovement:
        // ---- 社交层: 组队广播(与行为层并行, 队长人格由 GroupPlay 发邀请) ----

        // ---- 聊天语料(模板+变量) ----
        if (_config.EnableChatCorpus && now >= _nextChatCorpus)
        {
            _connection.Enqueue(new C.Chat { Text = BotChatCorpus.Compose(this, _random) });
            _nextChatCorpus = now.AddSeconds(Math.Max(30, _config.ChatIntervalSeconds) * (0.8 + _random.NextDouble() * 0.6));
        }
        else if (now >= _nextChat)
        {
            _connection.Enqueue(new C.Chat { Text = $"{_config.ChatPrefix}，我叫{Name}。" });
            _nextChat = now.AddSeconds(Math.Max(30, _config.ChatIntervalSeconds) + _random.NextDouble() * 45);
        }

        // 附近(≤3 格)有掉落就走过去捡(真人打完怪顺手捡装备)
        var item = World.Items.Values.OrderBy(x => Distance(World.Location, x.Location)).FirstOrDefault(x => Distance(World.Location, x.Location) <= 3);
        if (item != null)
        {
            if (Distance(World.Location, item.Location) <= 1)
            {
                _connection.Enqueue(new C.PickUp());
                _pickupRequests++;
            }
            else if (CanMove(now))
            {
                MoveToDestination(item.Location, now);
            }
        }
    }

    /// <summary>背景反应层: 喝药/低血道士治疗/职业准备(盾/召唤维持)/整理背包/火炬。</summary>
    private void BackgroundReactions(DateTime now)
    {
        if (now >= _nextActivityReport)
        {
            Console.WriteLine($"[{Name}] active map={World.MapIndex}:{World.Location} role={Profile.Personality} class={World.Class} behavior={_scheduler.Current} safe={World.InSafeZone} gold={World.Gold} move={_moveActions} attack={_attackActions} magic={_magicActions} shop={_shopPurchases}/{_shopSales} pickup={_pickupRequests}/{_itemsGainedEvents} pets={OwnedSummonCount()} | train[{_trainBehavior.Stats}] grind[{_grindBehavior.Stats}] group[{_groupBehavior.Stats}] equip[{_equipBehavior.Stats}]");
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
            // 技能熟练度(只列有经验的, 验证练技能有效性)
            foreach (var magic in World.Magics.Where(x => x.Info != null && !x.ItemRequired && x.Experience > 0))
                Console.WriteLine($"[{Name}] magic: {magic.Info.Name} Lv{magic.Level} exp={magic.Experience}");
            _nextActivityReport = now.AddSeconds(45 + _random.NextDouble() * 15);
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
                    _connection.Enqueue(new C.ItemSort { Grid = GridType.Inventory });
                    _nextPotion = now.AddSeconds(8);
                }
                else
                {
                    _connection.Enqueue(new C.ItemUse
                    {
                        Link = new CellLinkInfo { GridType = GridType.Inventory, Slot = potion.Slot, Count = 1 }
                    });
                    _nextPotion = now.AddSeconds(1.0 + _random.NextDouble() * 1.5);
                }
            }
        }

        // 低血队友治疗(道士反应, 与行为层并行)
        if (World.Class == MirClass.Taoist && now >= _nextSupport && TrySupportAlly(now))
            _nextSupport = now.AddSeconds(3 + _random.NextDouble() * 2);

        // 火炬: 夜间视野
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

        // 定期整理背包
        if (now >= _nextInventorySort && World.Inventory.Count >= Math.Max(10, Globals.InventorySize / 2))
        {
            _connection.Enqueue(new C.ItemSort { Grid = GridType.Inventory });
            _nextInventorySort = now.AddMinutes(5);
        }

        // 移动卡死检测(全局): 位置长期不变且不在 AutoPath/挖矿/钓鱼 → 自愈
        UpdateStuckDetection(now);
    }

    /// <summary>全局卡死检测: 同图连续 ~12s 位置不变 → 重置寻路/取消 AutoPath。</summary>
    private void UpdateStuckDetection(DateTime now)
    {
        if (_lastPositionSample == DateTime.MinValue)
        {
            _lastPositionSample = now;
            _lastSampledPosition = World.Location;
            return;
        }
        if (World.Location == _lastSampledPosition)
        {
            _positionStallTicks++;
            if (_positionStallTicks >= (int)(12_000 / Math.Max(100, _config.TickMilliseconds)))
            {
                _positionStallTicks = 0;
                if (_autoPathActive)
                {
                    _connection.Enqueue(new C.AutoPathCancel());
                    _autoPathActive = false;
                    _stuckRecoveries++;
                    Console.WriteLine($"[{Name}] stuck: cancel autopath, resync (#{_stuckRecoveries})");
                }
                _pathfinder?.Reset();
                _pathGoal = Point.Empty;
                _nextMove = now.AddSeconds(1);
            }
        }
        else
        {
            _positionStallTicks = 0;
            _lastSampledPosition = World.Location;
        }
        _lastPositionSample = now;
    }

    /// <summary>计划外跨图回城(保留原逻辑: 回城卷 → AutoPath → 宽限补给)。</summary>
    private bool TryCrossMapReturn(DateTime now)
    {
        bool grindTraveling = _grindBehavior.Traveling;
        bool miningActive = _resourceTripEnd != DateTime.MinValue && now < _resourceTripEnd;
        bool inInstance = World.InstanceIndex >= 0;
        if (World.MapIndex == _config.HomeMapIndex || grindTraveling || miningActive || inInstance)
        {
            if (World.MapIndex != _config.HomeMapIndex && now >= _nextCrossMapDiag)
            {
                _nextCrossMapDiag = now.AddSeconds(90);
                Console.WriteLine($"[{Name}] cross-map exempt grind={grindTraveling} mining={miningActive} inst={inInstance}");
            }
            return false;
        }

        if (now < _crossMapGraceUntil)
            return false; // 宽限期: 放行主链让供给链买回城卷

        if (now < _nextMove) return true;

        // ItemUse 回城卷后 ~30s 仍非家图: 卷传送无效(BindPoint 被改写),
        // 停止烧卷, 重登让 SetBindPoint 重选绑定。
        if (_portalUseAt != DateTime.MinValue && _portalUseSlot.HasValue &&
            (now - _portalUseAt).TotalSeconds >= 30)
        {
            Console.WriteLine($"[{Name}] town portal ineffective (bindpoint hijacked), map={World.MapIndex} relog");
            _portalUseAt = DateTime.MinValue;
            _portalUseSlot = null;
            _connection.TryDisconnect();
            return true;
        }

        var scroll = World.Inventory.FirstOrDefault(x => x?.Info != null &&
            x.Info.ItemType == ItemType.Consumable && x.Info.Shape == 2 &&
            x.Info.ItemName.Contains("Town Portal", StringComparison.OrdinalIgnoreCase));
        if (scroll != null && scroll.Count > 0)
        {
            // 在线购买的新物品 slot 恒为 -1, 整理背包拿正确 slot
            if (scroll.Slot < 0)
            {
                _connection.Enqueue(new C.ItemSort { Grid = GridType.Inventory });
                _nextMove = now.AddSeconds(10);
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
            Console.WriteLine($"[{Name}] away from home map, town portal home");
            return true;
        }
        _crossMapFailCount++;
        if (_crossMapFailCount >= 4)
        {
            _crossMapFailCount = 0;
            _crossMapGraceUntil = now.AddSeconds(45);
            Console.WriteLine($"[{Name}] cross-map autopath failing, resupply grace");
            return true;
        }
        // 优先朝缓存的卖卷 NPC 步行(宽限期的供给链会买卷回城)
        if (_knownSupplyNpcLocations.TryGetValue(World.MapIndex, out var shopDict) && shopDict.Count > 0)
        {
            var shopPoint = shopDict.Values.First();
            MoveToward(shopPoint, 1, now);
            _nextMove = now.AddSeconds(5);
            Console.WriteLine($"[{Name}] away from home, walk to known shop {shopPoint}");
            return true;
        }
        _connection.Enqueue(new C.AutoPathWaypoint
        {
            MapIndex = _config.HomeMapIndex,
            Location = _homeAnchor
        });
        _nextMove = now.AddSeconds(8);
        Console.WriteLine($"[{Name}] away from home map, autopath home");
        return true;
    }

    private bool TryTownServices(DateTime now)
    {
        bool npcPriorityMove = false;
        // 缺职业补给(道士护符)时供给绝对优先: 否则修装/任务分支
        // 会把 bot 反复拉向就近 NPC, 与供给目的地(如 Lennard)形成
        // NW-SE 锯齿拉锯, 永远到不了。
        if (NeedsClassSupplies() && TrySupplyBehavior(now)) return true;
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
                    MoveToDestination(repairNpc.Object.CurrentLocation, now);
                    npcPriorityMove = true;
                }
            }
        }

        if (!npcPriorityMove && now >= _nextQuestAction && now >= _supplyInteractionUntil && !_npcCallPending)
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
                else if (now >= _nextMove)
                {
                    MoveToDestination(questNpc.Object.CurrentLocation, now);
                    npcPriorityMove = true;
                }
            }
        }

        if (npcPriorityMove) return true;

        // 商人人格逛街卖货更勤: 只缩短冷却, 绝不推到过去(否则失败的
        // AutoPath 会每 tick 重发造成刷屏死循环)。
        if (Profile.Personality == BotPersonality.Merchant &&
            _nextSellAction - now > TimeSpan.FromSeconds(30))
            _nextSellAction = now.AddSeconds(30);

        if (TrySellBehavior(now)) return true;
        if (TrySupplyBehavior(now)) return true;
        return false;
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

    // ====================================================================
    // 拟真行为系统辅助 API(供 IBotBehavior 调用)
    // ====================================================================

    /// <summary>日志(带 bot 名前缀, 供测试统计 grep)。</summary>
    public void Log(string message) => Console.WriteLine($"[{Name}] {message}");

    public bool CanMove(DateTime now) => now >= _nextMove && !World.Dead;

    public int DistanceTo(Point p) => Distance(World.Location, p);

    public bool NearHome(int radius) => World.MapIndex == _config.HomeMapIndex && DistanceTo(_homeAnchor) < radius;

    /// <summary>在城镇活动区(安全区或城中心 45 格内——按地理中心判定,
    /// 不用各自随机锚点, 否则走远一点就"不在城里"把行为层全封死)。</summary>
    public bool InTownArea
        => World.MapIndex == _config.HomeMapIndex &&
           (World.InSafeZone || DistanceTo(new Point(_config.HomeMapX, _config.HomeMapY)) < 45);

    /// <summary>需要补给(药水少/背包满)。城镇内走供给链, 野外触发回城。</summary>
    public bool NeedsShopping => NeedsPotionSupply() || BagNearlyFull;

    public bool BagNearlyFull
        => World.Inventory.Count(x => x is { Slot: >= 0, Info: not null } &&
                                      x.Slot < Globals.InventorySize) >= Globals.InventorySize - 4;

    public bool PotionSupplyLow
        => World.Inventory.Where(x => x.Info?.CanAutoPot == true).Sum(x => Math.Max(0, x.Count)) < 3;

    public MapInfo MapInfoByIndex(int index)
        => Globals.MapInfoList?.Binding.FirstOrDefault(x => x.Index == index);

    /// <summary>地图像素宽(GetPoints 摊平 BitRegion 用)。</summary>
    public int MapWidthOf(MapInfo info)
    {
        var map = CurrentMapOf(info);
        return map?.Width ?? 1000;
    }

    private BotMap CurrentMapOf(MapInfo info)
    {
        if (info == null || string.IsNullOrWhiteSpace(info.FileName)) return null;
        string path = Path.Combine(_config.MapPath, $"{info.FileName}.map");
        return BotMap.Load(path);
    }

    /// <summary>找到已学技能中第一个匹配的类型(顺序优先)。</summary>
    public ClientUserMagic FindMagic(params MagicType[] types)
    {
        foreach (var type in types)
        {
            var magic = World.Magics.FirstOrDefault(x => x.Info?.Magic == type && !x.ItemRequired);
            if (magic != null) return magic;
        }
        return null;
    }

    /// <summary>施法(尊重服务端 CD 与本地节奏)。返回是否已发包。</summary>
    public bool CastMagic(ClientUserMagic magic, uint targetId, Point location, MirDirection direction)
    {
        if (magic?.Info == null) return false;
        if (DateTime.UtcNow < magic.NextCast) return false;
        _connection.Enqueue(new C.Magic
        {
            Direction = direction,
            Action = MirAction.Spell,
            Type = magic.Info.Magic,
            Target = targetId,
            Location = location
        });
        _magicActions++;
        // 服务端 MagicDelay=2s 硬节流; MagicCooldown 包会同步真实 CD
        magic.NextCast = DateTime.UtcNow + magic.Cooldown;
        return true;
    }

    /// <summary>挥武器技能(城内空练)。</summary>
    public void SwingWeaponSkill()
    {
        _connection.Enqueue(new C.Attack
        {
            Direction = (MirDirection)_random.Next(8),
            Action = MirAction.Attack,
            AttackMagic = World.Class is MirClass.Warrior or MirClass.Assassin ? SelectAttackSkill() : MagicType.None
        });
        _attackActions++;
    }

    /// <summary>本图距自己最近的守卫(大刀)旁的可走格。</summary>
    public Point NearestGuardSpot()
    {
        var info = MapInfoByIndex(World.MapIndex);
        var guards = info?.Guards;
        if (guards == null || guards.Count == 0) return Point.Empty;
        GuardInfo best = null;
        int bestDist = int.MaxValue;
        foreach (var guard in guards)
        {
            int d = Distance(World.Location, new Point(guard.X, guard.Y));
            if (d < bestDist) { bestDist = d; best = guard; }
        }
        if (best == null) return Point.Empty;
        return RandomWalkableNear(new Point(best.X, best.Y), 3);
    }

    /// <summary>锚点附近随机可走格。</summary>
    public Point RandomWalkableNear(Point anchor, int radius)
    {
        var map = CurrentMap();
        for (int i = 0; i < 20; i++)
        {
            var point = new Point(anchor.X + _random.Next(-radius, radius + 1),
                                  anchor.Y + _random.Next(-radius, radius + 1));
            if (map == null || map.CanWalk(point)) return point;
        }
        return anchor;
    }

    public void MoveToDestination(Point goal, DateTime now)
    {
        if (!CanMove(now)) return;
        if (DistanceTo(goal) <= 1) return;

        var map = CurrentMap();
        if (map == null)
        {
            MoveToward(goal, 1, now);
            return;
        }

        // 服务端拒收检测(按目标追踪): 同一目标连续 >2.5s 位置不变
        // → 该步格被服务端动态占位, 拉黑重算。多调用方(供给/跟随/行程)
        // 交替换目标也不会互相重置计时。
        if (_rejectTracker.TryGetValue(goal, out var track) &&
            World.Location == track.From &&
            (now - track.At).TotalSeconds > 2.5)
        {
            // 拉黑(45s TTL, 见 A* 构造处的过期清理)。死亡回城时位置
            // 整体失效, 黑名单一并清空(见 TownRevive 发送点)。
            _runtimeBlocked.Add(track.Step);
            _blockedAt[track.Step] = now;
            if (_runtimeBlocked.Count > 40)
            {
                _runtimeBlocked.Clear();
                _blockedAt.Clear();
            }
            _pathGoal = Point.Empty;
            _stuckRecoveries++;
            Log($"path: server-blocked {track.Step}, blacklisted ({_runtimeBlocked.Count}), reroute");
            if (_runtimeBlocked.Count % 3 == 1)
            {
                var around = World.Monsters.Values.Where(m => !m.Dead && Distance(World.Location, m.Location) <= 3)
                    .Select(m => $"M@{m.Location}");
                var players = World.Players.Values.Where(pl => Distance(World.Location, pl.Location) <= 3)
                    .Select(pl => $"P@{pl.Location}");
                var npcs = World.Npcs.Values.Where(n => Distance(World.Location, n.CurrentLocation) <= 3)
                    .Select(n => $"N@{n.CurrentLocation}");
                Log($"path: near=[{string.Join(",", around)}|{string.Join(",", players)}|{string.Join(",", npcs)}] self={World.Location}");
            }
            _rejectTracker.Remove(goal);
        }



        if (_pathfinder == null || _pathGoal != goal)
        {
            // 换目标时不清挂起步: 拒收检测按目标匹配, 交错调用(供给+跟随)
            // 也能各自累计拒收时长。
            // A* 失败(目的地太远/地形阻隔)后短期内不再重算, 期间贪心直走
            if (_lastPathFailGoal == goal && now < _pathFailRetryAt)
            {
                MoveToward(goal, 1, now);
                return;
            }
            // 过期黑名单清理: 怪物占位是瞬态的, >45s 的拉黑条目重新开放,
            // 否则走廊被永久毒化, A* 找不到长路(城镇→Lennard 实际 291 步)。
            if (_blockedAt.Count > 0)
            {
                var expired = _blockedAt.Where(kv => (now - kv.Value).TotalSeconds > 45)
                    .Select(kv => kv.Key).ToList();
                foreach (var p in expired) { _blockedAt.Remove(p); _runtimeBlocked.Remove(p); }
            }
            _pathfinder = new BotPathfinder(map, _runtimeBlocked);
            if (!_pathfinder.SetDestination(World.Location, goal))
            {
                _pathGoal = Point.Empty;
                _pathfinder = null;
            }
            else
                _pathGoal = goal;
            if (!_pathfinder.HasPath)
            {
                bool sameGoal = _lastPathFailGoal == goal;
                _lastPathFailGoal = goal;
                _pathFailRetryAt = now.AddSeconds(8);
                if (now >= _nextPathFailLog)
                {
                    Log($"path: A* fail to {goal} ({DistanceTo(goal)} cells) on map {World.MapIndex}({map.Width}x{map.Height}) at {World.Location} walk(S)={map.CanWalk(World.Location)} walk(G)={map.CanWalk(goal)} bl={_runtimeBlocked.Count}, greedy fallback");
                    _nextPathFailLog = now.AddSeconds(10);
                }
                // 同一目标连续失败: 动态占位把路围死了, 清空黑名单整体重置
                if (sameGoal) _blacklistFailStreak++;
                if (_blacklistFailStreak >= 2)
                {
                    _runtimeBlocked.Clear();
                    _blockedAt.Clear();
                    _blacklistFailStreak = 0;
                    Log("path: blacklist reset (persistent A* fail)");
                }
                MoveToward(goal, 1, now);
                return;
            }
            _lastPathFailGoal = Point.Empty;
            _blacklistFailStreak = 0;
        }

        if (!_pathfinder.TryGetStep(World.Location, out var step))
        {
            // 无路可走/已到: 直接贪心(服务端会纠正撞墙)
            MoveToward(goal, 1, now);
            _pathGoal = Point.Empty;
            return;
        }

        // 位置由服务器回包异步更新: 与上次调用比位置变化判移动
        bool moved = World.Location != _lastPathPosition;
        _pendingStepObserved = step;
        _pendingStepGoal = goal;
        if (_rejectTracker.Count > 8) _rejectTracker.Clear();
        if (World.Location != _pendingStepFrom)
        {
            _pendingStepFrom = World.Location;
            _rejectTracker[goal] = (step, World.Location, now);
        }
        else if (!_rejectTracker.TryGetValue(goal, out var cur) || cur.Step != step)
        {
            _rejectTracker[goal] = (step, World.Location, now);
        }
        MoveToward(step, 1, now);
        _pathfinder.Advance(World.Location, moved);
    }

    /// <summary>战斗步进: 距离>2 追击, ≤2 攻击(技能/普攻)。返回是否完成一次攻击。</summary>
    public bool CombatStep(S.ObjectMonster target, DateTime now)
    {
        int distance = DistanceTo(target.Location);
        if (distance > 2)
        {
            if (CanMove(now)) MoveToDestination(target.Location, now);
            return false;
        }
        if (now < _nextAttack) return false;

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
        // 法师/道士施法受服务端 MagicDelay=2000ms 节流 → 慢节奏;
        // 战士/刺客按武器节奏(AttackTime 下限 800ms)。
        bool casting = World.Class is MirClass.Wizard or MirClass.Taoist;
        _nextAttack = now.AddSeconds(casting ? 2.2 + _random.NextDouble() : 0.9 + _random.NextDouble() * 0.4);
        // 真人连击会绕目标走位
        if (_combatActions >= 3 && _random.NextDouble() < 0.25 && CanMove(now))
        {
            _combatActions = 0;
            MoveToDestination(FlankPoint(target.Location), now);
        }
        return true;
    }

    /// <summary>选择狩猎目标(排除他人宠物/同伴, 12 格内最近)。</summary>
    public S.ObjectMonster SelectHuntTarget(DateTime now) => SelectTarget(now);

    /// <summary>附近(≤8 格)掉落: 走过去捡或直接拾取。返回是否消耗本 tick。</summary>
    public bool TryLootStep(DateTime now)
    {
        var loot = World.Items.Values
            .OrderBy(x => Distance(World.Location, x.Location))
            .FirstOrDefault(x => Distance(World.Location, x.Location) <= 8);
        if (loot == null) return false;
        if (Distance(World.Location, loot.Location) <= 1)
        {
            _connection.Enqueue(new C.PickUp());
            _pickupRequests++;
            return true;
        }
        if (CanMove(now)) MoveToDestination(loot.Location, now);
        return true;
    }

    /// <summary>远离最近的威胁走一步(低血撤退)。</summary>
    public void WalkStepAwayFromThreat(DateTime now)
    {
        var threat = World.Monsters.Values
            .Where(x => !x.Dead && string.IsNullOrWhiteSpace(x.PetOwner))
            .OrderBy(x => Distance(World.Location, x.Location))
            .FirstOrDefault(x => Distance(World.Location, x.Location) <= 6);
        if (threat == null) return;
        var away = new Point(
            World.Location.X + Math.Sign(World.Location.X - threat.Location.X) * 4,
            World.Location.Y + Math.Sign(World.Location.Y - threat.Location.Y) * 4);
        MoveToDestination(RandomWalkableNear(away, 2), now);
    }

    // ---- 组队辅助 ----
    /// <summary>已在队伍中。</summary>
    public bool IsGroupMember => World.GroupMembers.Count > 0;

    /// <summary>自己是队长(建队时捕获的第一名成员)。</summary>
    public bool IsGroupLeader => IsGroupMember &&
        _groupLeaderName.Equals(Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>队长在视野内。</summary>
    public bool GroupLeaderNearby => GroupLeaderPlayer != null;

    /// <summary>视野内的队长玩家对象。</summary>
    public S.ObjectPlayer GroupLeaderPlayer
    {
        get
        {
            if (!IsGroupMember) return null;
            string leader = _groupLeaderName;
            if (string.IsNullOrEmpty(leader)) return null;
            return World.Players.Values.FirstOrDefault(x =>
                x.Name.Equals(leader, StringComparison.OrdinalIgnoreCase) && !x.Dead);
        }
    }

    /// <summary>队长邀请的队友候选名单(同图玩家优先, 其次固定小队序号)。</summary>
    public IEnumerable<string> SquadCandidateNames()
    {
        // 身边可见的其他 bot 优先(同图才能即时响应)
        var visible = World.Players.Values
            .Where(x => !x.Dead && !x.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Name.StartsWith(_config.AccountPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => Distance(World.Location, x.Location))
            .Select(x => x.Name)
            .Take(3)
            .ToList();
        foreach (var name in visible) yield return name;
        // 固定小队: 序号相邻的同人格段 bot(跨图邀请也合法, 上线即入队)
        int squadStart = ((_index - 1) / 4) * 4 + 1;
        for (int i = squadStart; i < squadStart + 4 && i <= _config.MaxBots; i++)
        {
            if (i == _index) continue;
            yield return $"{_config.AccountPrefix}{i:00}";
        }
    }

    /// <summary>视野内最近的非本人 bot 玩家。</summary>
    public S.ObjectPlayer NearestOtherBot(int radius)
        => World.Players.Values
            .Where(x => !x.Dead && !x.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Name.StartsWith(_config.AccountPrefix, StringComparison.OrdinalIgnoreCase))
            .Where(x => Distance(World.Location, x.Location) <= radius)
            .OrderBy(x => Distance(World.Location, x.Location))
            .FirstOrDefault();

    /// <summary>某点附近最近的活怪(助攻用)。</summary>
    public S.ObjectMonster NearestMonsterNear(Point center, int radius)
        => World.Monsters.Values
            .Where(x => !x.Dead && string.IsNullOrWhiteSpace(x.PetOwner) && x.CompanionObject == null)
            .Where(x => Distance(center, x.Location) <= radius)
            .OrderBy(x => Distance(World.Location, x.Location))
            .FirstOrDefault();

    /// <summary>跟队散开点(队长位置 + 按自己序号的稳定偏移)。</summary>
    public Point FollowPointNear(Point leader)
    {
        int angle = _index % 8;
        var offset = new Point(((angle % 4) - 1) * 2, ((angle / 2) % 3 - 1) * 2);
        var point = new Point(leader.X + offset.X, leader.Y + offset.Y);
        var map = CurrentMap();
        return map != null && !map.CanWalk(point) ? leader : point;
    }

    /// <summary>道士: 治疗附近受伤玩家(练治疗熟练度)。</summary>
    public bool TryHealNearby(ClientUserMagic heal)
    {
        var ally = World.Players.Values
            .Where(x => !x.Dead)
            .Where(x => World.PlayerVitals.TryGetValue(x.ObjectID, out var vital) &&
                        World.PlayerMaxVitals.TryGetValue(x.ObjectID, out var max) &&
                        vital.Health * 100 < Math.Max(1, max.MaxHealth) * 80)
            .OrderBy(x => Distance(World.Location, x.Location))
            .FirstOrDefault(x => Distance(World.Location, x.Location) <= 9);

        // 没有受伤的玩家就给自己附近放(目标=自己也有效)
        uint targetId = ally?.ObjectID ?? World.SelfObjectId;
        Point targetLocation = ally?.Location ?? World.Location;
        return CastMagic(heal, targetId, targetLocation, DirectionTo(World.Location, targetLocation));
    }

    /// <summary>寻路移动: 本图目标走本地 A*(服务端 AutoPath 在本数据集
    /// 全部地图 CanAutoPath=false 不可用), 跨图仍发服务端寻路。</summary>
    public void AutoPathTo(int mapIndex, Point location)
    {
        if (mapIndex == World.MapIndex)
        {
            if (location == Point.Empty) return; // 空目标: 不行程也不发寻路
            _travelDest = location;
            _travelMapIndex = mapIndex;
            _travelActive = true;
            _travelSince = DateTime.UtcNow;
            return;
        }
        _connection.Enqueue(new C.AutoPathWaypoint { MapIndex = mapIndex, Location = location });
        _nextMove = DateTime.UtcNow.AddSeconds(8);
        _autoPathActive = true;
    }

    /// <summary>本图服务端 AutoPath 是否可用(数据集 CanAutoPath 开关)。</summary>
    public bool ServerAutoPathUsable => MapInfoByIndex(World.MapIndex)?.CanAutoPath == true;

    // ---- 装备成长辅助 ----

    /// <summary>物品综合评分(属性和 + 等级需求权重)。</summary>
    private static int ItemScore(ItemInfo info)
    {
        if (info?.Stats == null) return -1;
        int score = 0;
        foreach (var pair in info.Stats.Values)
        {
            // 主属性权重更高
            score += pair.Key switch
            {
                Stat.MaxDC or Stat.MaxMC or Stat.MaxSC => pair.Value * 3,
                Stat.MinDC or Stat.MinMC or Stat.MinSC => pair.Value * 3,
                Stat.Health or Stat.Mana => pair.Value,
                Stat.Accuracy or Stat.Agility => pair.Value * 2,
                _ => pair.Value,
            };
        }
        score += info.RequiredType == RequiredType.Level ? info.RequiredAmount : 0;
        return score;
    }

    /// <summary>物品类型 → 装备槽位(可穿的类型才返回值)。</summary>
    private static int? EquipmentSlotOf(ItemInfo info)
        => info.ItemType switch
        {
            ItemType.Weapon => (int)EquipmentSlot.Weapon,
            ItemType.Armour => (int)EquipmentSlot.Armour,
            ItemType.Helmet => (int)EquipmentSlot.Helmet,
            ItemType.Torch => (int)EquipmentSlot.Torch,
            ItemType.Necklace => (int)EquipmentSlot.Necklace,
            ItemType.Bracelet => (int)EquipmentSlot.BraceletL,
            ItemType.Ring => (int)EquipmentSlot.RingL,
            ItemType.Shoes => (int)EquipmentSlot.Shoes,
            ItemType.Amulet => (int)EquipmentSlot.Amulet,
            ItemType.Shield => (int)EquipmentSlot.Shield,
            _ => null,
        };

    /// <summary>背包里是否有比身上更好且职业/性别/等级匹配的装备。</summary>
    public bool HasBetterUnequippedItem()
    {
        foreach (var candidate in World.Inventory.Where(x => x is { Slot: >= 0, Info: not null } && x.Slot < Globals.InventorySize))
        {
            var slot = EquipmentSlotOf(candidate.Info);
            if (slot == null || !IsClassSuitable(candidate.Info)) continue;
            var equipped = World.Inventory.FirstOrDefault(x => x.Slot == Globals.EquipmentOffSet + slot);
            if (equipped == null) return ItemScore(candidate.Info) >= 0;
            if (candidate.Info.ItemType == ItemType.Torch) continue; // 火炬由背景层管
            if (ItemScore(candidate.Info) > ItemScore(equipped.Info) + 5)
                return true;
        }
        return false;
    }

    /// <summary>穿上背包里评分最高的可穿装备。返回是否穿了一件。</summary>
    public bool EquipBestUpgrade()
    {
        ClientUserItem bestItem = null;
        int bestSlot = 0, bestGain = 0;
        foreach (var candidate in World.Inventory.Where(x => x is { Slot: >= 0, Info: not null } && x.Slot < Globals.InventorySize))
        {
            var slot = EquipmentSlotOf(candidate.Info);
            if (slot == null || !IsClassSuitable(candidate.Info)) continue;
            if (candidate.Info.ItemType == ItemType.Torch) continue;
            if (candidate.Slot < 0) continue; // slot 缓存失效, 等整理
            var equipped = World.Inventory.FirstOrDefault(x => x.Slot == Globals.EquipmentOffSet + slot);
            int equippedScore = equipped?.Info == null ? -1 : ItemScore(equipped.Info);
            int gain = ItemScore(candidate.Info) - equippedScore;
            if (gain > bestGain && gain > 5)
            {
                bestGain = gain;
                bestItem = candidate;
                bestSlot = slot.Value;
            }
        }
        if (bestItem == null) return false;

        var equippedNow = World.Inventory.FirstOrDefault(x => x.Slot == Globals.EquipmentOffSet + bestSlot);
        if (equippedNow != null)
        {
            // 服务端 ItemMove 在目标格有物品时执行原子交换: 一步
            // Inventory↔Equipment 即完成换装。绝不能发第二个包(旧武器
            // 会被穿回去, 造成无限来回震荡)。
            _connection.Enqueue(new C.ItemMove
            {
                FromGrid = GridType.Inventory,
                FromSlot = bestItem.Slot,
                ToGrid = GridType.Equipment,
                ToSlot = bestSlot,
                MergeItem = false
            });
            Console.WriteLine($"[{Name}] equip: swap {bestItem.Info.ItemName} into slot {bestSlot} (was {equippedNow.Info.ItemName}, gain {bestGain})");
        }
        else
        {
            _connection.Enqueue(new C.ItemMove
            {
                FromGrid = GridType.Inventory,
                FromSlot = bestItem.Slot,
                ToGrid = GridType.Equipment,
                ToSlot = bestSlot,
                MergeItem = false
            });
            Console.WriteLine($"[{Name}] equip: wear {bestItem.Info.ItemName} slot {bestSlot} (gain {bestGain})");
        }
        return true;
    }

    /// <summary>道士把背包里的护身符(shape 0, 召唤用)装进符槽。
    /// 装备评分不覆盖消耗品槽位, 这里独立处理。</summary>
    public void TryEquipAmulet()
    {
        if (World.Class != MirClass.Taoist) return;
        var amuletSlot = (int)EquipmentSlot.Amulet;
        var equipped = World.Inventory.FirstOrDefault(x => x.Slot == Globals.EquipmentOffSet + amuletSlot);
        if (equipped?.Info?.ItemType == ItemType.Amulet && equipped.Count > 5 && equipped.Info.Shape == 0) return;
        var bag = World.Inventory.FirstOrDefault(x =>
            x is { Slot: >= 0, Info: not null } && x.Slot < Globals.InventorySize &&
            x.Info.ItemType == ItemType.Amulet && x.Info.Shape == 0 && x.Count > 0);
        if (bag == null)
        {
            var anyAmu = World.Inventory.Where(x => x?.Info?.ItemType == ItemType.Amulet).ToList();
            if (anyAmu.Count > 0)
                Log($"equip: amulet in bag but unusable: {string.Join(",", anyAmu.Select(x => $"slot={x.Slot} shape={x.Info.Shape} cnt={x.Count}"))} invSize={Globals.InventorySize}");
            return;
        }
        _connection.Enqueue(new C.ItemMove
        {
            FromGrid = GridType.Inventory,
            FromSlot = bag.Slot,
            ToGrid = GridType.Equipment,
            ToSlot = amuletSlot,
            MergeItem = false
        });
        Log($"equip: wear amulet {bag.Info.ItemName} x{bag.Count}");
    }

    /// <summary>聊天语料变量: (地图名, 最近怪名)。</summary>
    public (string Map, string Monster) ChatContext()
    {
        string map = MapInfoByIndex(World.MapIndex)?.Description ?? "这边";
        var monster = World.Monsters.Values
            .Where(x => !x.Dead && string.IsNullOrWhiteSpace(x.PetOwner))
            .OrderBy(x => Distance(World.Location, x.Location))
            .FirstOrDefault(x => Distance(World.Location, x.Location) <= 18);
        string monsterName = monster != null
            ? Globals.MonsterInfoList?.Binding.FirstOrDefault(m => m.Index == monster.MonsterIndex)?.MonsterName ?? "怪"
            : "怪";
        return (map, monsterName);
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
            if (now >= _nextMove) _patrolBehavior.Execute(this, now);
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
        // remaining==0 才是"已到位"。A* 路径的下一格恒为 1 格之遥,
        // 必须允许迈进去(此前 <=1 直接 return 造成寻路永久冻结)。
        if (remaining <= 0) return;


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

        // 第一轮: 静态可走 + 无活物占位(玩家/怪/NPC 站位服务端会拒);
        // 第二轮: 仅静态可走(占位是瞬态的, 有空就钻)。
        foreach (var candidate in candidates)
        {
            var cell = NextPoint(World.Location, candidate);
            if (map.CanWalk(cell) && !CellOccupied(cell)) return candidate;
        }
        foreach (var candidate in candidates)
        {
            if (map.CanWalk(NextPoint(World.Location, candidate))) return candidate;
        }

        return preferred;
    }

    /// <summary>该格是否有可见活物占位(服务端 IsBlocking 拒收移动)。
    /// 死亡怪物(S.ObjectDied)仍留在字典里, 必须跳过, 否则尸体永远"占格"。</summary>
    private bool CellOccupied(Point cell)
    {
        foreach (var m in World.Monsters.Values)
            if (!m.Dead && m.Location == cell) return true;
        foreach (var p in World.Players.Values)
            if (p.Location == cell) return true;
        foreach (var n in World.Npcs.Values)
            if (n.CurrentLocation == cell) return true;
        return false;
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

    /// <summary>当前地图数据(行为层选可达狩猎区用)。</summary>
    public BotMap CurrentMapData => CurrentMap();

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

    // PvP 角色由人格档案决定(BotProfile.PvpRole), 不再按序号硬划分。
    private bool IsPvpBot(int index) => Profile.PvpRole;

    // 城中心出生点。登录时 SpawnMapIndex 是角色下线位置而非固定出生图,
    // 因此家在配置的 HomeMap(比奇县), 仅当登录位置就在出生图时用其坐标。
    private Point HomeLocation()
    {
        return World.SpawnMapIndex == _config.HomeMapIndex
            ? World.SpawnLocation
            : new Point(_config.HomeMapX, _config.HomeMapY);
    }

    // 每个 bot 在城中心出生点周围选一个可走点作为自己的"家",
    // 带抖动让众人散在城中心不同角落而不是叠在同一点。
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


    public int OwnedSummonCount()
        => World.Monsters.Values.Count(x => x.PetOwner?.Equals(Name, StringComparison.OrdinalIgnoreCase) == true && x.CompanionObject == null);

    public MagicType SelectAttackSkill()
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
        _nextChatCorpus = now.AddSeconds(20 + _random.NextDouble() * 30);
        _nextPotion = now.AddSeconds(2 + _random.NextDouble());
        _homeAnchor = ChooseHomeAnchor();
        _targetMonsterId = 0;
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
        _nextGuildAction = now.AddSeconds(15 + _random.NextDouble() * 20);
        _starterGuildAttempted = false;
        _nextContainerAction = now.AddSeconds(45 + _random.NextDouble() * 30);
        _containerSlot = -1;
        _nextFishingAction = now.AddSeconds(40 + _random.NextDouble() * 30);
        _fishingActive = false;
        _fishingPoint = Point.Empty;
        _nextPvpAction = Profile.PvpRole
            ? now.AddSeconds(Math.Max(5, _config.PvPStartDelaySeconds) + _random.NextDouble() * 15)
            : DateTime.MaxValue;
        _pvpRoundEnd = DateTime.MinValue;
        _pvpStagingPoint = Point.Empty;
        _pvpActions = 0;
        _nextActivityReport = now.AddSeconds(10 + _random.NextDouble() * 10);
        _moveActions = 0;
        _attackActions = 0;
        _magicActions = 0;
        _targetSelections = 0;
        _combatActions = 0;
        // 重连后行为状态复位(行为对象按人格跨会话复用)
        _lastBehavior = "";
        _pathGoal = Point.Empty;
        _pathfinder = null;
        _groupLeaderName = "";
        _lastPositionSample = DateTime.MinValue;
        _positionStallTicks = 0;
        _travelActive = false;
        _travelDest = Point.Empty;
        _sellAutopathBlocked = false;
        _supplyAutopathBlocked = false;
        Console.WriteLine($"[{Name}] profile {Profile}");
    }


    private bool TryFishingBehavior(DateTime now)
    {
        // 钓鱼仅悠闲型人格参与, 且需要真实装备(钓竿/钓鱼服)与服务器钓鱼区。
        if (Profile.Personality != BotPersonality.Idle) return false;

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
        // 挖矿仅悠闲型人格(Lifestyle)参与, 镐子由 BotProvisioner 配给。
        if (!Profile.Lifestyle || now < _nextResourceAction) return false;
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

        // 道士缺符但当前页没有护符: 商店可能是多页的(如火把/卷轴首页+
        // 护符子页), 沿按钮继续翻页找 BuySell 子页(限深 4 防环)。
        if (page.DialogType == NPCDialogType.BuySell && World.Class == MirClass.Taoist && NeedsAmulets() &&
            page.Goods?.Any(x => x.Item?.ItemType == ItemType.Amulet && x.Item.Shape == 0) != true)
        {
            var next = page.Buttons?.FirstOrDefault(x => x.DestinationPage?.DialogType == NPCDialogType.BuySell);
            if (next != null && _npcPageHops < 4)
            {
                _npcPageHops++;
                _connection.Enqueue(new C.NPCButton { ButtonID = next.ButtonID });
                return;
            }
        }
        _npcPageHops = 0;
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
                .ThenByDescending(x => x.Item.ItemType == ItemType.Amulet && World.Class == MirClass.Taoist &&
                    NeedsAmulets() && x.Item.Shape == 0)
                .ThenBy(x => x.Index)
                .FirstOrDefault();
            if (potion?.Item != null)
            {
                long amount = potion.Item.CanAutoPot ? 20
                    : potion.Item.ItemType == ItemType.Amulet ? 200
                    : potion.Item.ItemType == ItemType.Consumable && potion.Item.Shape == 2 ? 3 : 1;
                _connection.Enqueue(new C.NPCBuy { Index = potion.Index, Amount = amount, GuildFunds = false });
                _nextSupplyAction = DateTime.UtcNow.AddSeconds(90);
                _supplyPurchasePending = false;
                _shopPurchases++;
                var shopNpc = World.Npcs.Values.FirstOrDefault(n => n.ObjectID == _npcObjectId);
                var shopName = shopNpc != null
                    ? Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == shopNpc.NPCIndex)?.NPCName ?? "?"
                    : $"obj#{_npcObjectId}";
                Console.WriteLine($"[{Name}] shop: buy {potion.Item.ItemName} x{amount} at {shopName} gold={World.Gold}");
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

            // 逛到装备店顺手买升级件(真人行为: 卖完垃圾看武器)
            if (DateTime.UtcNow >= _nextEquipShopBuy && page.Goods != null)
            {
                ItemInfo bestItemInfo = null; int bestGain = 0, bestIdx = 0;
                foreach (var good in page.Goods.Where(g => g.Item != null))
                {
                    var info = good.Item;
                    var slot = EquipmentSlotOf(info);
                    if (slot == null || !IsClassSuitable(info) ||
                        info.ItemType is not (ItemType.Weapon or ItemType.Armour or ItemType.Helmet)) continue;
                    var equipped = World.Inventory.FirstOrDefault(x => x.Slot == Globals.EquipmentOffSet + slot);
                    int gain = ItemScore(info) - (equipped?.Info == null ? -1 : ItemScore(equipped.Info));
                    if (gain > bestGain && gain > 5) { bestGain = gain; bestIdx = good.Index; bestItemInfo = info; }
                }
                if (bestItemInfo != null && World.Gold > 50000)
                {
                    _connection.Enqueue(new C.NPCBuy { Index = bestIdx, Amount = 1, GuildFunds = false });
                    _nextEquipShopBuy = DateTime.UtcNow.AddMinutes(3);
                    _shopPurchases++;
                    Console.WriteLine($"[{Name}] shop: buy upgrade {bestItemInfo.ItemName} (gain {bestGain}) gold={World.Gold}");
                }
            }
        }

        if (page.DialogType != NPCDialogType.Repair) return;
        var links = World.Inventory
            .Where(x => x.Info != null && IsEquipped(x) && x.Info.CanRepair)
            .Where(x => x.CurrentDurability < x.MaxDurability && x.MaxDurability > 0)
            .Where(x => x.Info.ItemType is ItemType.Weapon or ItemType.Armour or ItemType.Helmet or ItemType.Necklace
                or ItemType.Bracelet or ItemType.Ring or ItemType.Shoes or ItemType.Shield)
            .Take(40)
            .Select(x => new CellLinkInfo { GridType = GridType.Equipment, Slot = x.Slot - Globals.EquipmentOffSet, Count = 1 })
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
        // 回城卷轴: 老口径只认 ItemType.Scroll, 但本数据集的 Town Portal
        // 是 Consumable Shape==2 — 两者都算, 备 3 张。否则买了也判缺,
        // 陷入无限买卷且供给层永久垄断 tick。
        var portals = World.Inventory
            .Where(x => x.Info?.ItemType == ItemType.Scroll ||
                        (x.Info?.ItemType == ItemType.Consumable && x.Info.Shape == 2))
            .Sum(x => Math.Max(0, x.Count));
        return portals < 3;
    }

    /// <summary>低血/低蓝判断(药水使用与购买共享)。</summary>
    private bool ShouldUseConsumable()
        => World.MaxHealth > 0 && World.CurrentHealth * 100 < World.MaxHealth * 45 ||
           (World.MaxMana > 0 && World.CurrentMana * 100 < World.MaxMana * 35 &&
            World.Class is MirClass.Wizard or MirClass.Taoist);


    private bool NeedsManaPotion()
        => World.MaxMana > 0 && World.CurrentMana * 100 < World.MaxMana * 35 &&
           !(World.MaxHealth > 0 && World.CurrentHealth * 100 < World.MaxHealth * 45);


    /// <summary>破围: 被攻击性怪围困时, 优先钻空格逃出; 8 邻全堵死
    /// 才攻击最近怪开路(真人被野猪围住也是这么干的)。</summary>
    private bool TryBreakout(DateTime now)
    {
        int monsterNeighbors = 0;
        foreach (var m in World.Monsters.Values)
            if (!m.Dead && Distance(World.Location, m.Location) <= 1) monsterNeighbors++;
        if (monsterNeighbors < 1)
        {
            _surroundedSince = DateTime.MinValue;
            return false;
        }

        var map = CurrentMap();
        bool freeCell = false;
        if (map != null)
        {
            for (int dx = -1; dx <= 1 && !freeCell; dx++)
            for (int dy = -1; dy <= 1 && !freeCell; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var cell = new Point(World.Location.X + dx, World.Location.Y + dy);
                if (map.CanWalk(cell) && !CellOccupied(cell)) freeCell = true;
            }
        }
        if (freeCell)
        {
            _surroundedSince = DateTime.MinValue;
            return false; // 有空可钻交给正常移动
        }

        // 全堵死: 困住 >8s 开始砍最近怪开路。
        if (_surroundedSince == DateTime.MinValue)
        {
            _surroundedSince = now;
            return false;
        }
        if ((now - _surroundedSince).TotalSeconds < 8) return false;

        S.ObjectMonster nearest = null;
        int nearestDist = int.MaxValue;
        foreach (var m in World.Monsters.Values)
        {
            if (m.Dead) continue;
            int d = Distance(World.Location, m.Location);
            if (d > 1 || d >= nearestDist) continue;
            nearestDist = d;
            nearest = m;
        }
        if (nearest != null)
        {
            if (now >= _nextBreakoutLog)
            {
                var info = Globals.MonsterInfoList?.Binding.FirstOrDefault(m => m.Index == nearest.MonsterIndex);
                Log($"breakout: walled by {monsterNeighbors} monsters ({info?.MonsterName ?? "?"}), cutting through");
                _nextBreakoutLog = now.AddSeconds(10);
            }
            CombatStep(nearest, now);
            return true;
        }
        return false;
    }
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
        // 粘性行走: 上次选定的供给目的地未到站前一直持有移动权,
        // 否则 1s 冷却间隙会被行为层(grind)反向拉扯造成拉锯。
        if (_supplyTravelDest is Point travelDest && now < _supplyTravelUntil && !World.Dead)
        {
            if (DistanceTo(travelDest) <= 2) _supplyTravelDest = null;
            else if (CanMove(now))
            {
                MoveToDestination(travelDest, now);
                return true;
            }
        }
        else _supplyTravelDest = null;
        if (NeedsClassSupplies() && now >= _nextSupplyDiag)
        {
            _nextSupplyDiag = now.AddSeconds(45);
            var amu = World.Inventory.Where(x => x.Info?.ItemType == ItemType.Amulet).Sum(x => Math.Max(0, x.Count));
            var eq = World.Inventory.FirstOrDefault(x => x.Slot == Globals.EquipmentOffSet + (int)EquipmentSlot.Amulet);
            Log($"supply: diag needs-class=True inv-amulet={amu} eq-amulet={eq?.Count ?? 0} cooldown={(_nextSupplyAction - now).TotalSeconds:F0}s at={World.Location} sticky={( _supplyTravelDest?.ToString() ?? "-")} sell-blocked={_sellAutopathBlocked}");
        }
        if (now < _nextSupplyAction) return false;
        // 道士在城内且想练召唤 → 推迟补给 60s 让行为层训练; 最多连缓 2 次
        if (World.Class == MirClass.Taoist && InTownArea && !World.Dead &&
            !NeedsClassSupplies() && _trainDeferCount < 2 && _trainBehavior.Score(this, now) > 0)
        {
            _trainDeferCount++;
            _nextSupplyAction = now.AddSeconds(60);
            return false;
        }
        _trainDeferCount = 0;
        var needAmulets = World.Class == MirClass.Taoist && NeedsAmulets();
        // 缺回城卷(任何职业): 只选真卖卷的店, 否则非道士会在武器店
        // 空转("no suitable goods")循环。
        var needPortal = !needAmulets &&
            World.Inventory.Where(x => x.Info?.ItemType == ItemType.Scroll ||
                    (x.Info?.ItemType == ItemType.Consumable && x.Info.Shape == 2))
                .Sum(x => Math.Max(0, x.Count)) < 3;
        var npc = World.Npcs.Values
            .Select(x => (Object: x, Info: Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == x.NPCIndex)))
            .Where(x => x.Info != null && HasSupplyShop(x.Info))
            // 缺特定货时可见 NPC 必须真能卖它, 否则选了也白跑 —
            // 交给 fallback 走去有货的店(如 Lennard)。
            .Where(x => !needAmulets || NpcSellsAmulet(x.Info))
            .Where(x => !needPortal || SellsTownPortal(x.Info))
            .OrderByDescending(x => needAmulets && NpcSellsAmulet(x.Info))
            .ThenByDescending(x => needPortal && SellsTownPortal(x.Info))
            .ThenBy(x => Distance(World.Location, x.Object.CurrentLocation))
            .FirstOrDefault(x => Distance(World.Location, x.Object.CurrentLocation) <= Math.Max(20, _config.PatrolRadius * 2));
        // retry at the next slow interval.
        _supplyPurchasePending = false;

        if (npc.Object == null)
        {
            var supplyNpc = Globals.NPCInfoList?.Binding
                .Where(n => HasSupplyShop(n) && n.Region?.Map?.Index == World.MapIndex)
                .Where(n => !needAmulets || NpcSellsAmulet(n))
                .Where(n => !needPortal || SellsTownPortal(n))
                .OrderByDescending(n => needAmulets && NpcSellsAmulet(n))
                .ThenByDescending(n => needPortal && SellsTownPortal(n))
                .FirstOrDefault()
                ?? Globals.NPCInfoList?.Binding.FirstOrDefault(HasSupplyShop);
            if (supplyNpc == null) return false;
            // 跨图时本地无供给 NPC, 先朝卖卷 NPC 走(供给链会买卷回城)。
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
            // AutoPath 失败过(或本图不可用): 本地 A* 走向供给 NPC 出生 region。
            // 走路期间只设 ~1s 冷却(一步一冷却会让 NPC 永远到不了)。
            if (_supplyAutopathBlocked || !ServerAutoPathUsable)
            {
                var dest = NpcRegionPoint(supplyNpc);
                if (now >= _nextSupplyDiag)
                {
                    Log($"supply: fallback npc={supplyNpc.NPCName} idx={supplyNpc.Index} regionPt={dest} from={World.Location}");
                    _nextSupplyDiag = now.AddSeconds(45);
                }
                if (dest != Point.Empty)
                {
                    if (DistanceTo(dest) > 2 && CanMove(now))
                    {
                        _supplyTravelDest = dest;
                        _supplyTravelUntil = now.AddSeconds(120);
                        MoveToDestination(dest, now);
                        _nextSupplyAction = now.AddSeconds(1);
                        return true;
                    }
                    return DistanceTo(dest) <= 10;
                }
            }
            _supplyPurchasePending = true;
            _supplyInteractionUntil = now.AddSeconds(30);
            _npcCallPending = true;
            _nextSupplyAction = now.AddSeconds(120);
            _connection.Enqueue(new C.AutoPathStart { NPCIndex = supplyNpc.Index });
            Console.WriteLine($"[{Name}] shop: auto-path supply NPC {supplyNpc.NPCName}");
            return true;
        }
        _supplyAutopathBlocked = false;


        _npcObjectId = npc.Object.ObjectID;
        if (Distance(World.Location, npc.Object.CurrentLocation) > 2)
        {
            if (now >= _nextMove) MoveToDestination(npc.Object.CurrentLocation, now);
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
            var sellNpc = Globals.NPCInfoList?.Binding
                .Where(n => HasSellShop(n) && n.Region?.Map?.Index == World.MapIndex)
                .FirstOrDefault()
                ?? Globals.NPCInfoList?.Binding.FirstOrDefault(HasSellShop);
            if (sellNpc == null) return false;

            // 服务器 AutoPath 到不了(或本图 CanAutoPath=false): 用本地 A*
            // 走向 NPC 出生 region。走路期间只设 ~1s 冷却。
            if (_sellAutopathBlocked || !ServerAutoPathUsable)
            {
                var dest = NpcRegionPoint(sellNpc);
                if (dest != Point.Empty)
                {
                    if (DistanceTo(dest) > 2 && CanMove(now))
                    {
                        MoveToDestination(dest, now);
                        _nextSellAction = now.AddSeconds(1);
                        return true;
                    }
                    if (DistanceTo(dest) <= 10)
                    {
                        _nextSellAction = now.AddSeconds(180 + _random.NextDouble() * 60);
                        return false; // 已在店边但没看到 NPC, 放行其他行为
                    }
                    _nextSellAction = now.AddSeconds(5);
                    return true;
                }
            }
            _nextSellAction = now.AddSeconds(180 + _random.NextDouble() * 60);
            _connection.Enqueue(new C.AutoPathStart { NPCIndex = sellNpc.Index });
            Console.WriteLine($"[{Name}] shop: auto-path sell NPC {sellNpc.NPCName}");
            return true;
        }
        _sellAutopathBlocked = false;



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

    private bool NeedsAmulets()
        => World.Class == MirClass.Taoist &&
           World.Inventory.Where(x => x.Info?.ItemType == ItemType.Amulet).Sum(x => Math.Max(0, x.Count)) < 20;

    /// <summary>NPC 是否卖护身符(沿入口页按钮链 BFS 找货架)。</summary>
    private static bool NpcSellsAmulet(NPCInfo npc)
    {
        if (npc?.EntryPage == null) return false;
        var seen = new HashSet<NPCPage> { npc.EntryPage };
        var queue = new Queue<NPCPage>();
        queue.Enqueue(npc.EntryPage);
        for (int depth = 0; queue.Count > 0 && depth < 40; depth++)
        {
            var page = queue.Dequeue();
            if (page.Goods != null && page.Goods.Any(g => g.Item?.ItemType == ItemType.Amulet))
                return true;
            foreach (var b in page.Buttons ?? Enumerable.Empty<NPCButton>())
                if (b.DestinationPage != null && seen.Add(b.DestinationPage))
                    queue.Enqueue(b.DestinationPage);
        }
        return false;
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
        var seen = new HashSet<NPCPage> { info.EntryPage };
        var queue = new Queue<NPCPage>();
        queue.Enqueue(info.EntryPage);
        for (int depth = 0; queue.Count > 0 && depth < 40; depth++)
        {
            var page = queue.Dequeue();
            if (page.DialogType == NPCDialogType.BuySell) return true;
            foreach (var b in page.Buttons ?? Enumerable.Empty<NPCButton>())
                if (b.DestinationPage != null && seen.Add(b.DestinationPage))
                    queue.Enqueue(b.DestinationPage);
        }
        return false;
    }

    private Point NpcRegionPoint(NPCInfo npcInfo)
    {
        if (npcInfo.Index == _regionNpcIndex && DateTime.UtcNow.Subtract(_regionPointAt).TotalSeconds < 60)
            return _regionPointCache;
        var region = npcInfo.Region;
        if (region?.Map == null || region.Map.Index != World.MapIndex) return Point.Empty;
        var points = region.GetPoints(MapWidthOf(region.Map));
        if (points == null || points.Count == 0) return Point.Empty;
        var arr = points.ToArray();
        var map = CurrentMap();
        var walkable = map == null ? arr : arr.Where(p => map.CanWalk(p)).ToArray();
        Point dest;
        if (walkable.Length == 0)
        {
            var near = RandomWalkableNear(arr[0], 10);
            dest = map != null && !map.CanWalk(near) ? Point.Empty : near;
        }
        else dest = walkable[_random.Next(walkable.Length)];
        if (dest != Point.Empty)
        {
            _regionNpcIndex = npcInfo.Index;
            _regionPointCache = dest;
            _regionPointAt = DateTime.UtcNow;
        }
        return dest;
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
