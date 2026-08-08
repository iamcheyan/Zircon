using Zircon.BotRunner;
using Library.Network;

var configPath = args.Length > 0 ? args[0] : "BotRunner.json";
var config = BotConfig.Load(configPath);
BaseConnection.NetworkLogging = config.VerboseNetworkLogging;
if (args.Length > 1 && int.TryParse(args[1], out var requestedCount))
    config.MaxBots = requestedCount;
BotDatabaseLoader.Load(config.DatabasePath);
var count = Math.Clamp(config.MaxBots, 1, 20);
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };

Console.WriteLine($"Zircon BotRunner: {count} bots -> {config.Host}:{config.Port}");
var bots = Enumerable.Range(1, count).Select(i => new BotAgent(i, config)).ToList();
var tasks = bots.Select(bot => bot.StartAsync(cancellation.Token)).ToArray();
try { await Task.WhenAll(tasks); }
catch (OperationCanceledException) { }
Console.WriteLine("BotRunner stopped.");
