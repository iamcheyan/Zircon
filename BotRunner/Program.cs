using Zircon.BotRunner;
using Library.Network;

var configPath = args.Length > 0 ? args[0] : "BotRunner.json";
var config = BotConfig.Load(configPath);
if (args.Length > 1 && int.TryParse(args[1], out var requestedCount))
    config.MaxBots = requestedCount;
BotDatabaseLoader.Load(config.DatabasePath);
var count = Math.Clamp(config.MaxBots, 1, 20);
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };

// 未处理异常兜底: 打印完整栈到 stdout(供 hub 日志采集), 而不是静默
// exit(1) — 否则崩溃根因会被高频日志挤出环形缓冲, 永远看不到。
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    Console.WriteLine($"[FATAL] UnhandledException: {e.ExceptionObject}");
    Console.Out.Flush();
};
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    Console.WriteLine($"[FATAL] UnobservedTaskException: {e.Exception}");
    e.SetObserved();
};

Console.WriteLine($"Zircon BotRunner: {count} bots -> {config.Host}:{config.Port}");
var bots = Enumerable.Range(1, count).Select(i => new BotAgent(i, config)).ToList();
var tasks = bots.Select(bot => bot.StartAsync(cancellation.Token)).ToArray();
try { await Task.WhenAll(tasks); }
catch (OperationCanceledException) { }
catch (Exception ex)
{
    Console.WriteLine($"[FATAL] bot task crashed: {ex}");
    Console.Out.Flush();
}
Console.WriteLine("BotRunner stopped.");
