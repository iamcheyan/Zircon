using System;
using System.Net.Sockets;
using System.Threading;
using Library.Network;
using G = Library.Network.GeneralPackets;
using C = Library.Network.ClientPackets;
using S = Library.Network.ServerPackets;

class TestConnection : BaseConnection
{
    protected override TimeSpan TimeOutDelay => TimeSpan.FromSeconds(30);
    public TestConnection(TcpClient client) : base(client) { }

    public override void TryDisconnect() { Connected = false; }
    public override void TrySendDisconnect(Packet p) { SendDisconnect(p); }
    public void StartReceive() { BeginReceive(); }

    protected override void ProcessUnhandledPacket(Packet p)
    {
        Console.WriteLine($"[<-] (未处理包) {p.PacketType.Name}");
    }

    public void Process(G.Connected p)
    {
        Console.WriteLine("[<-] Connected (服务端确认连接)");
        Enqueue(new G.Connected());
        Console.WriteLine("[->] Connected (客户端回握)");
    }
    public void Process(G.GoodVersion p)
    {
        Console.WriteLine("[<-] GoodVersion  *** 版本校验通过, 进入 Login 阶段 ***");
        Console.WriteLine($"    SystemDatabaseVersion={p.SystemDatabaseVersion}");
        Console.WriteLine($"    DatabaseKey={(p.DatabaseKey == null ? "null" : $"{p.DatabaseKey.Length} bytes")}");
        Enqueue(new C.Login { EMailAddress = "test@test.com", Password = "test123" });
        Console.WriteLine("[->] Login (尝试登录 test@test.com / test)");
    }
    public void Process(G.Disconnect p)
    {
        Console.WriteLine($"[<-] Disconnect  *** 被断开: {p.Reason} ***");
        Connected = false;
    }
    public void Process(S.Login p) { Console.WriteLine($"[<-] S.Login 结果: {p.Result}"); }
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
        Console.WriteLine("TCP 已连接");

        var conn = new TestConnection(client);
        conn.OnException = (o, ex) => Console.WriteLine($"[!!] {ex.GetType().Name}: {ex.Message}");
        conn.StartReceive();
        conn.UpdateTimeOut();

        for (int i = 0; i < 300; i++)
        {
            try { conn.Process(); }
            catch (Exception ex) { Console.WriteLine($"[!!] Process 抛: {ex.Message}"); break; }
            Thread.Sleep(100);
            if (!conn.Connected) { Console.WriteLine("连接已断开, 退出"); break; }
        }
        Console.WriteLine("验证结束");
    }
}