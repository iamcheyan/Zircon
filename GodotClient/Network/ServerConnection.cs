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
    public event Action<int, int> StatsUpdateEvent; // maxHealth, maxMana
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

    public void Process(S.Login p)
    {
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
        int maxHealth = p.Stats != null ? p.Stats[Stat.Health] : 0;
        int maxMana = p.Stats != null ? p.Stats[Stat.Mana] : 0;
        StatsUpdateEvent?.Invoke(maxHealth, maxMana);
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
}
