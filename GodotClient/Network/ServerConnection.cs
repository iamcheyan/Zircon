using System;
using System.Collections.Generic;
using System.Net.Sockets;
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
        StartGameResultEvent?.Invoke(p.Result, p.StartInformation);
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
        Enqueue(new C.StartGame { CharacterIndex = characterIndex });
    }
}
