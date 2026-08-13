using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Godot;
using ZirconClient.Controls;
using ZirconClient.Scripts;

namespace ZirconClient.Network;

/// <summary>
/// 单机模式启动器：客户端启动时若目标端口没有服务端监听，自动拉起
/// 本地 ServerCore，客户端退出时自动关闭（进程生命周期绑定）。
///
/// 设计要点（与用户沟通后确认）：
///  - 复用现有 test@test.com/TestHero 账号（不新建）；
///  - 端口已有监听（如用户手动开的服务器）→ 直接连，不拉起、退出时不杀；
///  - 仅杀掉"本启动器拉起"的 ServerCore（记录 PID），不误杀外部进程；
///  - 服务器数据目录：Debug/ServerCore（System.db + 809 张地图）。
///
/// 用法：
///  - 默认自动模式：端口无监听即拉起（连不上才触发，联网时无感）；
///  - 命令行 --single / --offline：强制单机模式（探测+拉起）；
///  - 命令行 --server host：指定远程服务器时不触发单机逻辑。
/// </summary>
public partial class SinglePlayerLauncher : Node
{
    /// <summary>本启动器拉起的 ServerCore 进程（退出时只杀它）。</summary>
    private Process _spawnedServer;

    /// <summary>本启动器是否拉起了服务端（LoginScene 据此决定是否等待就绪）。</summary>
    public bool IsSpawned => _spawnedServer != null;

    /// <summary>端口探测+拉起的互斥锁，避免重复拉起。</summary>
    private int _started;

    /// <summary>客户端进程退出钩子里调用（NOTIFICATION_WM_CLOSE / AutoAcceptQuit）。</summary>
    public void EnsureServerRunning(string host, int port)
    {
        if (_spawnedServer != null) return; // 已拉起
        if (Interlocked.Exchange(ref _started, 1) != 0) return;

        // 远程服务器模式：不触发单机逻辑
        if (AutoLoginArgs.ServerAddress != null) return;

        // 端口已有监听 → 连现有的（用户手动开的服务器），不拉起
        if (IsPortOpen(host, port))
        {
            GD.Print($"[Single] 端口 {host}:{port} 已有监听，使用现有服务端（不拉起）");
            return;
        }

        GD.Print("[Single] 未检测到服务端，启动单机模式：拉起本地 ServerCore ...");
        try
        {
            string root = Path.GetFullPath(Path.Combine(
                ProjectSettings.GlobalizePath("res://"), "..", "Debug", "ServerCore"));
            string dll = Path.Combine(root, "ServerCore.dll");
            if (!File.Exists(dll))
            {
                GD.PrintErr($"[Single] ServerCore.dll 不存在: {dll}");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                // --singleplayer-dev: 服务端给测试账号注入满级/全技能/全装备，
                // 配合客户端自动登录，双击客户端即可单机测试 UI。
                Arguments = "ServerCore.dll --singleplayer-dev",
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
                // ServerCore 主线程 Console.ReadLine() 会阻塞等 stdin：
                // 重定向 stdin 并立即关闭，让 ReadLine 返回 null、主线程进入循环等待。
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            _spawnedServer = Process.Start(psi);
            _spawnedServer.StandardInput.Close(); // EOF → ReadLine 返回 null，不抛异常
            GD.Print($"[Single] ServerCore 已启动 PID={_spawnedServer.Id}");

            // 异步排空输出，避免管道写满阻塞
            _spawnedServer.OutputDataReceived += (_, e) => { if (e.Data != null) GD.Print($"[Srv] {e.Data}"); };
            _spawnedServer.ErrorDataReceived += (_, e) => { if (e.Data != null) GD.PrintErr($"[Srv] {e.Data}"); };
            _spawnedServer.BeginOutputReadLine();
            _spawnedServer.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Single] 启动 ServerCore 失败: {ex.Message}");
            _spawnedServer = null;
        }
    }

    /// <summary>等待服务端端口就绪（最多约 15 秒，每 250ms 探测一次）。</summary>
    public bool WaitForServer(string host, int port, int timeoutMs = 15000)
    {
        var deadline = System.Environment.TickCount64 + timeoutMs;
        while (System.Environment.TickCount64 < deadline)
        {
            if (IsPortOpen(host, port)) return true;
            if (_spawnedServer != null && _spawnedServer.HasExited)
            {
                GD.PrintErr($"[Single] ServerCore 已退出: code={_spawnedServer.ExitCode}");
                return false;
            }
            Thread.Sleep(250);
        }
        GD.PrintErr("[Single] 等待服务端就绪超时");
        return false;
    }

    /// <summary>客户端退出时调用：只杀本启动器拉起的 ServerCore。</summary>
    public void Shutdown()
    {
        var p = _spawnedServer;
        _spawnedServer = null;
        if (p == null || p.HasExited) return;
        GD.Print($"[Single] 关闭单机模式服务端 PID={p.Id}");
        try { p.Kill(entireProcessTree: true); } catch { /* 已退出 */ }
        try { p.WaitForExit(3000); } catch { /* 忽略 */ }
        p.Dispose();
    }

    public override void _Notification(int what)
    {
        // 窗口关闭 / 场景树退出：关闭自己拉起的服务端（不误杀外部进程）
        if (what == NotificationWMCloseRequest || what == NotificationPredelete || what == NotificationExitTree)
            Shutdown();
    }

    private static bool IsPortOpen(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var ar = client.BeginConnect(host, port, null, null);
            return ar.AsyncWaitHandle.WaitOne(500) && client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
