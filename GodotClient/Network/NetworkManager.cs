using System;
using System.Net.Sockets;
using Godot;
using Library.Network;

namespace ZirconClient.Network;

// 自动加载单例: 管理服务端连接生命周期, 每帧驱动 BaseConnection.Process()
public partial class NetworkManager : Node
{
    public ServerConnection Connection { get; private set; }
    public new bool IsConnected => Connection != null && Connection.Connected;

    public event Action<string> Log;

    public override void _Process(double delta)
    {
        // 每帧处理收到的包 (BaseConnection.Process 内部循环 ReceiveList)
        if (Connection != null && Connection.Connected)
        {
            try { Connection.Process(); }
            catch (Exception ex)
            {
                Log?.Invoke($"[Net] Process 异常: {ex.Message}");
                Connection.Connected = false;
            }
        }
    }

    public bool Connect(string host, int port)
    {
        Packet.IsClient = true;
        var client = new TcpClient();
        try { client.Connect(host, port); }
        catch (Exception ex)
        {
            Log?.Invoke($"[Net] 连接失败: {ex.Message}");
            return false;
        }

        Connection = new ServerConnection(client);
        Connection.OnException = (o, ex) => Log?.Invoke($"[Net] {ex.GetType().Name}: {ex.Message}");
        Connection.StartReceive();
        Connection.UpdateTimeOut();  // 必须调, 否则秒断
        Log?.Invoke($"[Net] TCP 已连接 {host}:{port}");
        return true;
    }

    public void Disconnect()
    {
        if (Connection != null)
        {
            Connection.Connected = false;
            Connection = null;
        }
    }
}
