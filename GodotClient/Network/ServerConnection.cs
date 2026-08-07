using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Godot;
using Library;
using Library.Network;
using G = Library.Network.GeneralPackets;
using C = Library.Network.ClientPackets;
using S = Library.Network.ServerPackets;

namespace ZirconClient.Network;

// Godot 版网络连接: 继承 LibraryCore/BaseConnection, 用 C# event 通知 UI
public partial class ServerConnection : BaseConnection
{
    protected override TimeSpan TimeOutDelay => TimeSpan.FromSeconds(30);
    public ServerConnection(TcpClient client) : base(client) { }

    public override void TryDisconnect() { Connected = false; }
    public override void TrySendDisconnect(Packet p) { SendDisconnect(p); }
    public void StartReceive() { BeginReceive(); }

    protected override void ProcessUnhandledPacket(Packet p)
    {
        GD.Print($"[Net] 未处理包: {p.PacketType.Name}");
        UnhandledPacket?.Invoke(p.PacketType.Name);
    }

    // UI 层订阅这些事件
    public event Action<string> Log;
    public event Action<string> UnhandledPacket;
    public event Action ConnectedEvent;
    public event Action<string, string> VersionOK;       // version, dbKeyInfo
    public event Action DisconnectedEvent;
    public event Action<LoginResult, string, List<SelectInfo>> LoginResultEvent;
    public event Action<NewAccountResult> NewAccountResultEvent;
    public event Action<NewCharacterResult, SelectInfo> NewCharacterResultEvent;
    public event Action<StartGameResult, StartInformation> StartGameResultEvent;
    public event Action<int, int> MapChangedEvent;       // mapIndex, instanceIndex
    public event Action<MirDirection, System.Drawing.Point> UserLocationEvent;
    public event Action<uint, MirDirection, System.Drawing.Point, int> ObjectMoveEvent; // objectID, dir, loc, distance
    public event Action<S.ObjectMonster> ObjectMonsterEvent;
    public event Action<S.ObjectNPC> ObjectNPCEvent;
    public event Action<S.ObjectItem> ObjectItemEvent;
    public event Action<uint> ObjectRemoveEvent;
    public event Action<uint, MirDirection> ObjectTurnEvent;
    // M5 战斗
    public event Action<uint, MirDirection, System.Drawing.Point, MagicType, uint> ObjectAttackEvent; // id, dir, loc, magic, targetID
    public event Action<uint, MirDirection, System.Drawing.Point, MagicType, List<uint>, List<System.Drawing.Point>, bool> ObjectMagicEvent; // id, dir, loc, type, targets, locations, cast
    public event Action<uint, int, bool, bool, bool> HealthChangedEvent; // id, change, miss, block, critical
    public event Action<uint, int, int, bool> DataObjectHealthManaEvent; // id, health, mana, dead
    public event Action<uint, int, int> DataObjectMaxHealthManaEvent; // id, maxHealth, maxMana
    public event Action<uint, int, int, int, bool> DataObjectMonsterEvent; // id, health, maxHealth, monsterIndex, dead
    public event Action<uint> ObjectDiedEvent;
    public event Action<uint, MirDirection, System.Drawing.Point, uint, Element> ObjectStruckEvent; // id, dir, loc, attackerID, element
    public event Action<S.StatsUpdate> StatsUpdateEvent; // 完整属性
    public event Action<float> DayTimeChangedEvent;
    public event Action<TimeOfDay, string> TimeOfDayChangedEvent;
    // M12 HUD: 等级/经验/蓝/专注/Buff
    public event Action<S.LevelChanged> LevelChangedEvent;
    public event Action<decimal> GainedExperienceEvent;    // amount
    public event Action<decimal> InformMaxExperienceEvent; // maxExperience
    public event Action<uint, int> ManaChangedEvent;       // objectID, change
    public event Action<uint, int> FocusChangedEvent;      // objectID, change
    public event Action<S.BuffAdd> BuffAddEvent;
    public event Action<int> BuffRemoveEvent;              // index
    public event Action<S.BuffChanged> BuffChangedEvent;
    public event Action<S.BuffTime> BuffTimeEvent;
    public event Action<int, bool> BuffPausedEvent;        // index, paused
    public event Action<AttackMode> AttackModeChangedEvent; // 服务端回显
    public event Action<PetMode> PetModeChangedEvent;
    // M9 物品系统
    public event Action<S.ItemsGained> ItemsGainedEvent;
    public event Action<S.ItemMove> ItemMoveEvent;
    public event Action<S.ItemSort> ItemSortEvent;
    public event Action<S.ItemSplit> ItemSplitEvent;
    public event Action<S.ItemDelete> ItemDeleteEvent;
    public event Action<S.ItemLock> ItemLockEvent;
    public event Action<S.ItemUseDelay> ItemUseDelayEvent;
    public event Action<S.ItemChanged> ItemChangedEvent;
    public event Action<S.ItemStatsChanged> ItemStatsChangedEvent;
    public event Action<S.ItemStatsRefreshed> ItemStatsRefreshedEvent;
    public event Action<S.ItemDurability> ItemDurabilityEvent;
    public event Action<S.ItemExperience> ItemExperienceEvent;
    public event Action<S.ItemsChanged> ItemsChangedEvent;
    public event Action<int, long> CurrencyChangedEvent;   // currencyIndex, amount
    public event Action<int, int, int> WeightUpdateEvent;  // bag, wear, hand
    public event Action<int> StorageSizeEvent;
    // StartGame 突发包缓冲: GameScene._Ready 前的事件订阅来不及, 这些包在订阅前已被 Process 丢弃。
    // Process 里 Enqueue + Invoke 双发; GameScene._Ready 一次性 Drain 积压, 之后靠事件接实时包。
    public readonly Queue<S.ObjectMove> PendingMoves = new();
    public readonly Queue<S.ObjectMonster> PendingMonsters = new();
    public readonly Queue<S.ObjectNPC> PendingNPCs = new();
    public readonly Queue<S.ObjectItem> PendingItems = new();
    public readonly Queue<uint> PendingRemoves = new();
    public readonly Queue<(uint, MirDirection)> PendingTurns = new();
    public readonly Queue<S.ObjectAttack> PendingAttacks = new();
    public readonly Queue<S.ObjectMagic> PendingMagics = new();
    public readonly Queue<S.HealthChanged> PendingHealthChanges = new();
    public readonly Queue<S.DataObjectHealthMana> PendingHealthManas = new();
    public readonly Queue<S.DataObjectMaxHealthMana> PendingMaxHealthManas = new();
    public readonly Queue<S.DataObjectMonster> PendingDataMonsters = new();
    public readonly Queue<uint> PendingDeaths = new();
    public readonly Queue<S.ObjectStruck> PendingStruck = new();
    public readonly Queue<S.StatsUpdate> PendingStats = new();
    public readonly Queue<S.LevelChanged> PendingLevelChanges = new();
    public readonly Queue<decimal> PendingGainedExperience = new();
    public readonly Queue<decimal> PendingMaxExperience = new();
    public readonly Queue<S.ManaChanged> PendingManaChanges = new();
    public readonly Queue<S.FocusChanged> PendingFocusChanges = new();
    public readonly Queue<S.BuffAdd> PendingBuffAdds = new();
    public readonly Queue<int> PendingBuffRemoves = new();
    public readonly Queue<S.BuffChanged> PendingBuffChangeds = new();
    public readonly Queue<S.BuffTime> PendingBuffTimes = new();
    public readonly Queue<(int, bool)> PendingBuffPauseds = new();
    public readonly Queue<S.ItemsGained> PendingItemsGained = new();
    public readonly Queue<S.ItemMove> PendingItemMoves = new();
    public readonly Queue<S.ItemSort> PendingItemSorts = new();
    public readonly Queue<S.ItemSplit> PendingItemSplits = new();
    public readonly Queue<S.ItemDelete> PendingItemDeletes = new();
    public readonly Queue<S.ItemLock> PendingItemLocks = new();
    public readonly Queue<S.ItemUseDelay> PendingItemUseDelays = new();
    public readonly Queue<S.ItemChanged> PendingItemChangeds = new();
    public readonly Queue<S.ItemStatsChanged> PendingItemStatsChangeds = new();
    public readonly Queue<S.ItemStatsRefreshed> PendingItemStatsRefresheds = new();
    public readonly Queue<S.ItemDurability> PendingItemDurabilities = new();
    public readonly Queue<S.ItemExperience> PendingItemExperiences = new();
    public readonly Queue<S.ItemsChanged> PendingItemsChangeds = new();
    public readonly Queue<(int, long)> PendingCurrencyChangeds = new();
    public readonly Queue<(int, int, int)> PendingWeightUpdates = new();
    public readonly Queue<int> PendingStorageSizes = new();

    public void Process(G.Connected p)
    {
        ConnectedEvent?.Invoke();
        Enqueue(new G.Connected());
    }
    public void Process(G.GoodVersion p)
    {
        VersionOK?.Invoke(p.SystemDatabaseVersion ?? "", p.DatabaseKey?.Length.ToString() ?? "null");
    }
    public void Process(G.Disconnect p)
    {
        GD.Print($"[Net] Disconnect: {p.Reason}");
        DisconnectedEvent?.Invoke();
        Connected = false;
    }
    public void Process(G.Ping p) { Enqueue(new G.Ping()); }

    // S.Login.Items = 仓库物品 (登录时到达, GameScene 在 StartGame 后消费 FillStorage)
    public List<ClientUserItem> PendingStorageItems = new();

    public void Process(S.Login p)
    {
        PendingStorageItems = p.Items ?? new List<ClientUserItem>();
        LoginResultEvent?.Invoke(p.Result, p.Message ?? "", p.Characters);
    }
    public void Process(S.NewAccount p)
    {
        NewAccountResultEvent?.Invoke(p.Result);
    }
    public void Process(S.NewCharacter p)
    {
        NewCharacterResultEvent?.Invoke(p.Result, p.Character);
    }
    public void Process(S.StartGame p)
    {
        GD.Print($"[Net] 收到 S.StartGame: Result={p.Result}");
        StartGameResultEvent?.Invoke(p.Result, p.StartInformation);
    }
    public void Process(S.MapChanged p)
    {
        MapChangedEvent?.Invoke(p.MapIndex, p.InstanceIndex);
    }
    public void Process(S.UserLocation p)
    {
        UserLocationEvent?.Invoke(p.Direction, p.Location);
    }

    public void Process(S.ObjectMove p)
    {
        PendingMoves.Enqueue(p);
        ObjectMoveEvent?.Invoke(p.ObjectID, p.Direction, p.Location, p.Distance);
    }

    public void Process(S.ObjectMonster p)
    {
        PendingMonsters.Enqueue(p);
        ObjectMonsterEvent?.Invoke(p);
    }

    public void Process(S.ObjectNPC p)
    {
        PendingNPCs.Enqueue(p);
        ObjectNPCEvent?.Invoke(p);
    }

    public void Process(S.ObjectItem p)
    {
        PendingItems.Enqueue(p);
        ObjectItemEvent?.Invoke(p);
    }

    public void Process(S.ObjectRemove p)
    {
        PendingRemoves.Enqueue(p.ObjectID);
        ObjectRemoveEvent?.Invoke(p.ObjectID);
    }

    public void Process(S.ObjectTurn p)
    {
        PendingTurns.Enqueue((p.ObjectID, p.Direction));
        ObjectTurnEvent?.Invoke(p.ObjectID, p.Direction);
    }

    public void Process(S.ObjectAttack p)
    {
        PendingAttacks.Enqueue(p);
        ObjectAttackEvent?.Invoke(p.ObjectID, p.Direction, p.Location, p.AttackMagic, p.TargetID);
    }

    public void Process(S.ObjectMagic p)
    {
        PendingMagics.Enqueue(p);
        ObjectMagicEvent?.Invoke(p.ObjectID, p.Direction, p.CurrentLocation, p.Type, p.Targets, p.Locations, p.Cast);
    }

    public void Process(S.HealthChanged p)
    {
        PendingHealthChanges.Enqueue(p);
        HealthChangedEvent?.Invoke(p.ObjectID, p.Change, p.Miss, p.Block, p.Critical);
    }

    public void Process(S.DataObjectHealthMana p)
    {
        PendingHealthManas.Enqueue(p);
        DataObjectHealthManaEvent?.Invoke(p.ObjectID, p.Health, p.Mana, p.Dead);
    }

    public void Process(S.DataObjectMaxHealthMana p)
    {
        PendingMaxHealthManas.Enqueue(p);
        DataObjectMaxHealthManaEvent?.Invoke(p.ObjectID, p.MaxHealth, p.MaxMana);
    }

    public void Process(S.DataObjectMonster p)
    {
        PendingDataMonsters.Enqueue(p);
        int maxHealth = p.Stats != null ? p.Stats[Stat.Health] : 0;
        DataObjectMonsterEvent?.Invoke(p.ObjectID, p.Health, maxHealth, p.MonsterIndex, p.Dead);
    }

    public void Process(S.ObjectDied p)
    {
        PendingDeaths.Enqueue(p.ObjectID);
        ObjectDiedEvent?.Invoke(p.ObjectID);
    }

    public void Process(S.ObjectStruck p)
    {
        PendingStruck.Enqueue(p);
        ObjectStruckEvent?.Invoke(p.ObjectID, p.Direction, p.Location, p.AttackerID, p.Element);
    }

    public void Process(S.StatsUpdate p)
    {
        PendingStats.Enqueue(p);
        StatsUpdateEvent?.Invoke(p);
    }

    public void Process(S.DayChanged p)
    {
        DayTimeChangedEvent?.Invoke(p.DayTime);
    }

    public void Process(S.TimeOfDayChanged p)
    {
        TimeOfDayChangedEvent?.Invoke(p.TimeOfDay, p.TimeOfDayLabel);
    }

    public void Process(S.LevelChanged p)
    {
        PendingLevelChanges.Enqueue(p);
        LevelChangedEvent?.Invoke(p);
    }

    public void Process(S.GainedExperience p)
    {
        PendingGainedExperience.Enqueue(p.Amount);
        GainedExperienceEvent?.Invoke(p.Amount);
    }

    public void Process(S.InformMaxExperience p)
    {
        PendingMaxExperience.Enqueue(p.MaxExperience);
        InformMaxExperienceEvent?.Invoke(p.MaxExperience);
    }

    public void Process(S.ManaChanged p)
    {
        PendingManaChanges.Enqueue(p);
        ManaChangedEvent?.Invoke(p.ObjectID, p.Change);
    }

    public void Process(S.FocusChanged p)
    {
        PendingFocusChanges.Enqueue(p);
        FocusChangedEvent?.Invoke(p.ObjectID, p.Change);
    }

    public void Process(S.BuffAdd p)
    {
        PendingBuffAdds.Enqueue(p);
        BuffAddEvent?.Invoke(p);
    }

    public void Process(S.BuffRemove p)
    {
        PendingBuffRemoves.Enqueue(p.Index);
        BuffRemoveEvent?.Invoke(p.Index);
    }

    public void Process(S.BuffChanged p)
    {
        PendingBuffChangeds.Enqueue(p);
        BuffChangedEvent?.Invoke(p);
    }

    public void Process(S.BuffTime p)
    {
        PendingBuffTimes.Enqueue(p);
        BuffTimeEvent?.Invoke(p);
    }

    public void Process(S.BuffPaused p)
    {
        PendingBuffPauseds.Enqueue((p.Index, p.Paused));
        BuffPausedEvent?.Invoke(p.Index, p.Paused);
    }

    public void Process(S.ChangeAttackMode p)
    {
        AttackModeChangedEvent?.Invoke(p.Mode);
    }

    public void Process(S.ChangePetMode p)
    {
        PetModeChangedEvent?.Invoke(p.Mode);
    }

    // ---- M9 物品系统 ----

    public void Process(S.ItemsGained p)
    {
        PendingItemsGained.Enqueue(p);
        ItemsGainedEvent?.Invoke(p);
    }

    public void Process(S.ItemMove p)
    {
        PendingItemMoves.Enqueue(p);
        ItemMoveEvent?.Invoke(p);
    }

    public void Process(S.ItemSort p)
    {
        PendingItemSorts.Enqueue(p);
        ItemSortEvent?.Invoke(p);
    }

    public void Process(S.ItemSplit p)
    {
        PendingItemSplits.Enqueue(p);
        ItemSplitEvent?.Invoke(p);
    }

    public void Process(S.ItemDelete p)
    {
        PendingItemDeletes.Enqueue(p);
        ItemDeleteEvent?.Invoke(p);
    }

    public void Process(S.ItemLock p)
    {
        PendingItemLocks.Enqueue(p);
        ItemLockEvent?.Invoke(p);
    }

    public void Process(S.ItemUseDelay p)
    {
        PendingItemUseDelays.Enqueue(p);
        ItemUseDelayEvent?.Invoke(p);
    }

    public void Process(S.ItemChanged p)
    {
        PendingItemChangeds.Enqueue(p);
        ItemChangedEvent?.Invoke(p);
    }

    public void Process(S.ItemStatsChanged p)
    {
        PendingItemStatsChangeds.Enqueue(p);
        ItemStatsChangedEvent?.Invoke(p);
    }

    public void Process(S.ItemStatsRefreshed p)
    {
        PendingItemStatsRefresheds.Enqueue(p);
        ItemStatsRefreshedEvent?.Invoke(p);
    }

    public void Process(S.ItemDurability p)
    {
        PendingItemDurabilities.Enqueue(p);
        ItemDurabilityEvent?.Invoke(p);
    }

    public void Process(S.ItemExperience p)
    {
        PendingItemExperiences.Enqueue(p);
        ItemExperienceEvent?.Invoke(p);
    }

    public void Process(S.ItemsChanged p)
    {
        PendingItemsChangeds.Enqueue(p);
        ItemsChangedEvent?.Invoke(p);
    }

    public void Process(S.CurrencyChanged p)
    {
        PendingCurrencyChangeds.Enqueue((p.CurrencyIndex, p.Amount));
        CurrencyChangedEvent?.Invoke(p.CurrencyIndex, p.Amount);
    }

    public void Process(S.WeightUpdate p)
    {
        PendingWeightUpdates.Enqueue((p.BagWeight, p.WearWeight, p.HandWeight));
        WeightUpdateEvent?.Invoke(p.BagWeight, p.WearWeight, p.HandWeight);
    }

    public void Process(S.StorageSize p)
    {
        PendingStorageSizes.Enqueue(p.Size);
        StorageSizeEvent?.Invoke(p.Size);
    }

    // UI 层调用: 发包
    public void SendLogin(string email, string password)
    {
        Enqueue(new C.Login { EMailAddress = email, Password = password });
    }
    public void SendNewAccount(string email, string password, string realName = "Player")
    {
        Enqueue(new C.NewAccount
        {
            EMailAddress = email,
            Password = password,
            BirthDate = new DateTime(1990, 1, 1),
            RealName = realName,
            CheckSum = "",
        });
    }
    public void SendNewCharacter(string name, MirClass cls, MirGender gender)
    {
        Enqueue(new C.NewCharacter
        {
            CharacterName = name,
            Class = cls,
            Gender = gender,
            HairType = 1,
            HairColour = System.Drawing.Color.Black,
            ArmourColour = System.Drawing.Color.White,
            CheckSum = "",
        });
    }
    public void SendStartGame(int characterIndex)
    {
        GD.Print($"[Net] SendStartGame charIndex={characterIndex}, Connected={Connected}, SendList={(SendList?.Count ?? -1)}");
        Enqueue(new C.StartGame { CharacterIndex = characterIndex });
    }

    // ---- M9 物品发包 ----

    public void SendItemMove(GridType fromGrid, GridType toGrid, int fromSlot, int toSlot, bool mergeItem)
    {
        Enqueue(new C.ItemMove
        {
            FromGrid = fromGrid,
            ToGrid = toGrid,
            FromSlot = fromSlot,
            ToSlot = toSlot,
            MergeItem = mergeItem,
        });
    }

    public void SendItemUse(GridType grid, int slot)
    {
        Enqueue(new C.ItemUse { Link = new CellLinkInfo { GridType = grid, Slot = slot, Count = 1 } });
    }

    public void SendItemLock(GridType grid, int slot, bool locked)
    {
        Enqueue(new C.ItemLock { GridType = grid, SlotIndex = slot, Locked = locked });
    }

    public void SendItemSort(GridType grid)
    {
        Enqueue(new C.ItemSort { Grid = grid });
    }

    public void SendItemDelete(GridType grid, int slot)
    {
        Enqueue(new C.ItemDelete { Grid = grid, Slot = slot });
    }

    public void SendPickUp()
    {
        Enqueue(new C.PickUp());
    }

    public void SendBeltLinkChanged(int slot, int linkInfoIndex, int linkItemIndex)
    {
        Enqueue(new C.BeltLinkChanged { Slot = slot, LinkIndex = linkInfoIndex, LinkItemIndex = linkItemIndex });
    }

    // 玩家学新技能 (S.NewMagic)
    public event Action<ClientUserMagic> NewMagicEvent;
    public readonly Queue<ClientUserMagic> PendingNewMagics = new();
    public void Process(S.NewMagic p)
    {
        PendingNewMagics.Enqueue(p.Magic);
        NewMagicEvent?.Invoke(p.Magic);
    }
}
