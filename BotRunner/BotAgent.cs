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
    private uint _targetMonsterId;
    private DateTime _nextTargetScan;
    private DateTime _nextGroupAction;
    private DateTime _nextTorchAction;
    private DateTime _nextRepairAction;
    private DateTime _nextQuestAction;
    private DateTime _nextSupplyAction;
    private DateTime _nextSellAction;
    private bool _supplyPurchasePending;
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
                { Fail($"login failed: {login.Result} {login.Message}"); break; }
                _connection.Enqueue(new C.StartGame { CharacterIndex = login.Characters[0].CharacterIndex });
                Status = BotStatus.Starting;
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
        if (Status != BotStatus.Running) return;
        var now = DateTime.UtcNow;
        if (now >= _nextActivityReport)
        {
        Console.WriteLine($"[{Name}] active map={World.MapIndex}:{World.Location} class={World.Class} safe={World.InSafeZone} gold={World.Gold} move={_moveActions} attack={_attackActions} magic={_magicActions} pvp={_pvpActions} shop={_shopPurchases}/{_shopSales} pickup={_pickupRequests}/{_itemsGainedEvents} targets={_targetSelections} pets={OwnedSummonCount()}");
            _nextActivityReport = now.AddSeconds(45 + _random.NextDouble() * 15);
        }
        if (World.Dead)
        {
            if (now >= _nextAttack) { _connection.Enqueue(new C.TownRevive()); _nextAttack = now.AddSeconds(5); }
            return;
        }

        if (TryPvPBehavior(now)) goto AfterMovement;

        // A PvP round takes priority over long-running life routes. Otherwise
        // a mining/trade auto-path can starve the scheduled arena behavior.
        if (_autoPathActive) goto AfterMovement;

        if (TryResourceBehavior(now)) goto AfterMovement;

        if (TryFishingBehavior(now)) goto AfterMovement;

        if (TryInstanceBehavior(now)) goto AfterMovement;

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
        // 阈值比巡逻半径上限(8~12 格)宽裕, 避免"走向目标途中被拽回出生点"的边界拉锯。
        if (Distance(World.Location, World.SpawnLocation) > _config.PatrolRadius + 8)
        {
            if (now >= _nextMove) MoveToward(World.SpawnLocation, 1, now);
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
        if (now >= _nextRepairAction && NeedsRepair())
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
                    _nextRepairAction = now.AddSeconds(8);
                }
                else if (now >= _nextMove)
                {
                    MoveToward(repairNpc.Object.CurrentLocation, 1, now);
                    npcPriorityMove = true;
                }
            }
        }

        if (now >= _nextQuestAction)
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
        if (!_config.EnableBotPvP || now < _nextPvpAction) return false;

        if (World.MapIndex != World.SpawnMapIndex)
        {
            _autoPathActive = false;
            if (now >= _nextMove)
            {
                _connection.Enqueue(new C.AutoPathWaypoint { MapIndex = World.SpawnMapIndex, Location = World.SpawnLocation });
                _nextMove = now.AddSeconds(8);
                Console.WriteLine($"[{Name}] pvp: return to map {World.SpawnMapIndex}");
            }
            return true;
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
        // 大部分时候小范围漫步(2~6 格), 偶发以出生点为锚做一次远足(8~12 格)。
        Point anchor = _random.NextDouble() < 0.8 ? World.Location : World.SpawnLocation;
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
        if (_config.EnableBotPvP) return false;
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
        if (_config.EnableBotPvP) return false;
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
        if (_config.EnableBotPvP) return false;
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
                _connection.Enqueue(new C.Mining { Direction = (MirDirection)_random.Next(8) });
                _nextResourceAction = now.AddSeconds(1.1 + _random.NextDouble() * 0.7);
                return true;
            }

            Point home = World.SpawnLocation;
            _connection.Enqueue(new C.AutoPathWaypoint { MapIndex = World.SpawnMapIndex, Location = home });
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

        if (World.MapIndex == World.SpawnMapIndex && _resourcePathHome)
        {
            _resourcePathHome = false;
            _resourceTripEnd = DateTime.MinValue;
            _nextResourceAction = now.AddSeconds(60 + _random.NextDouble() * 60);
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

        if (page.DialogType != NPCDialogType.Repair)
        {
            var repairButton = page.Buttons?.FirstOrDefault(x => x.DestinationPage?.DialogType == NPCDialogType.Repair);
            if (repairButton != null && NeedsRepair() && DateTime.UtcNow >= _nextRepairAction)
            {
                _connection.Enqueue(new C.NPCButton { ButtonID = repairButton.ButtonID });
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
            var potion = page.Goods?.Where(x => x.Item != null)
                .Where(x => x.Item.CanAutoPot || x.Item.ItemType == ItemType.Scroll ||
                    (x.Item.ItemType == ItemType.Amulet && World.Class == MirClass.Taoist))
                .Where(x => x.Item.CanAutoPot || x.Item.ItemType is ItemType.Scroll or ItemType.Amulet)
                .OrderByDescending(x => x.Item.CanAutoPot && NeedsPotionSupply())
                .ThenBy(x => x.Index)
                .FirstOrDefault();
            if (potion?.Item != null)
            {
                long amount = potion.Item.CanAutoPot ? (_supplyPurchasePending ? 1 : 5) : 1;
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
        return World.Inventory.All(x => x.Info?.ItemType != ItemType.Scroll);
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
        // A failed/unfinished AutoPathStart must not permanently block the
        // supply state machine. The server remains the authority; we simply
        // retry at the next slow interval.
        _supplyPurchasePending = false;

        var npc = World.Npcs.Values
            .Select(x => (Object: x, Info: Globals.NPCInfoList?.Binding.FirstOrDefault(n => n.Index == x.NPCIndex)))
            .Where(x => x.Info != null && HasSupplyShop(x.Info))
            .OrderBy(x => Distance(World.Location, x.Object.CurrentLocation))
            .FirstOrDefault(x => Distance(World.Location, x.Object.CurrentLocation) <= Math.Max(20, _config.PatrolRadius * 2));
        if (npc.Object == null)
        {
            var supplyNpc = Globals.NPCInfoList?.Binding.FirstOrDefault(HasSupplyShop);
            if (supplyNpc == null) return false;
            _supplyPurchasePending = true;
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
        _nextSupplyAction = now.AddSeconds(120);
        Console.WriteLine($"[{Name}] shop: approach NPC {npc.Info.NPCName}");
        return true;
    }

    private bool TrySellBehavior(DateTime now)
    {
        if (now < _nextSellAction) return false;

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

    private static bool HasSupplyShop(NPCInfo info)
    {
        if (info?.EntryPage == null) return false;
        var pages = new[] { info.EntryPage }
            .Concat(info.EntryPage.Buttons?.Where(x => x.DestinationPage != null).Select(x => x.DestinationPage) ?? Enumerable.Empty<NPCPage>());
        return pages.Any(x => x.DialogType == NPCDialogType.BuySell &&
            x.Goods?.Any(g => g.Item?.CanAutoPot == true || g.Item?.ItemType is ItemType.Scroll or ItemType.Amulet) == true);
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
