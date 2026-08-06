using System;
using System.Net.Sockets;
using System.Threading;
using Library;
using Library.Network;
using G = Library.Network.GeneralPackets;
using C = Library.Network.ClientPackets;
using S = Library.Network.ServerPackets;

// 自动建号工具: 连服务端 → 建账号 → 验证登录成功
class SetupConnection : BaseConnection
{
    protected override TimeSpan TimeOutDelay => TimeSpan.FromSeconds(30);
    public SetupConnection(TcpClient client) : base(client) { }

    public override void TryDisconnect() { Connected = false; }
    public override void TrySendDisconnect(Packet p) { SendDisconnect(p); }
    public void StartReceive() { BeginReceive(); }

    protected override void ProcessUnhandledPacket(Packet p)
    {
        Console.WriteLine($"[<-] (未处理) {p.PacketType.Name}");
    }

    public void Process(G.Connected p)
    {
        Enqueue(new G.Connected());
    }
    public void Process(G.GoodVersion p)
    {
        Console.WriteLine("[OK] 版本校验通过, 开始建号");
        Enqueue(new C.NewAccount
        {
            EMailAddress = "test@test.com",
            Password = "test123",
            BirthDate = new DateTime(1990, 1, 1),
            RealName = "TestPlayer",
            Referral = "",
            CheckSum = "",
        });
        Console.WriteLine("[->] NewAccount (test@test.com / test)");
    }
    public void Process(S.NewAccount p)
    {
        Console.WriteLine($"[<-] NewAccount 结果: {p.Result}");
        if (p.Result == NewAccountResult.Success || p.Result == NewAccountResult.AlreadyExists)
        {
            Console.WriteLine("[OK] 账号就绪, 尝试登录验证");
            Enqueue(new C.Login { EMailAddress = "test@test.com", Password = "test123" });
            Console.WriteLine("[->] Login (验证)");
        }
        else
        {
            Console.WriteLine($"[FAIL] 建号失败: {p.Result}");
            Connected = false;
        }
    }
    public void Process(G.Disconnect p)
    {
        Console.WriteLine($"[<-] Disconnect: {p.Reason}");
        Connected = false;
    }
    public void Process(S.Login p)
    {
        Console.WriteLine($"[<-] Login 结果: {p.Result}");
        if (p.Result == LoginResult.Success)
        {
            Console.WriteLine($"[OK] *** 登录成功! 角色数: {p.Characters?.Count ?? 0} ***");
            foreach (var c in p.Characters ?? new System.Collections.Generic.List<SelectInfo>())
                Console.WriteLine($"    #{c.CharacterIndex} {c.CharacterName} Lv{c.Level} {c.Class} {c.Gender}");
        }
        else
        {
            Console.WriteLine($"[FAIL] 登录失败: {p.Result} {p.Message}");
        }
        Connected = false; // 验证完就断
    }
}

class Program
{
    static void Main(string[] args)
    {
        Packet.IsClient = true;
        string host = args.Length > 0 ? args[0] : "127.0.0.1";
        int port = args.Length > 1 ? int.Parse(args[1]) : 7000;

        Console.WriteLine($"连接 {host}:{port} ...");
        TcpClient client = new TcpClient();
        try { client.Connect(host, port); }
        catch (Exception ex) { Console.WriteLine("连接失败: " + ex.Message); return; }

        var conn = new SetupConnection(client);
        conn.OnException = (o, ex) => Console.WriteLine($"[!!] {ex.GetType().Name}: {ex.Message}");
        conn.StartReceive();
        conn.UpdateTimeOut();

        for (int i = 0; i < 150 && conn.Connected; i++)
        {
            try { conn.Process(); }
            catch (Exception ex) { Console.WriteLine($"[!!] {ex.Message}"); break; }
            Thread.Sleep(100);
        }
        Console.WriteLine("完成");
    }
}