using System.Text.Json;

namespace Zircon.BotRunner;

public sealed class BotConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7000;
    public int TickMilliseconds { get; set; } = 250;
    // 真人步频。服务端 Globals.MoveTime=600ms 是每步硬下限, 250ms 决策
    // tick 会把实际步间隔量化到 ~750ms; 0.65s/格步行、0.6s/2格跑步。
    public double WalkIntervalSeconds { get; set; } = 0.65;
    public double RunIntervalSeconds { get; set; } = 0.6;
    public int PatrolRadius { get; set; } = 12;
    // 练级角色(野外打怪)的活动锚点。比奇县(0.map)北部 y5..117 是怪物
    // 刷新区;锚点带半径抖动, 每个 bot 选一个自己的练级点。
    public int FieldAnchorX { get; set; } = 175;
    public int FieldAnchorY { get; set; } = 60;
    public int FieldRadius { get; set; } = 40;
    public bool VerboseNetworkLogging { get; set; } = false;
    public bool EnableBotPvP { get; set; } = true;
    public int PvPStartDelaySeconds { get; set; } = 35;
    public int PvPStagingRadius { get; set; } = 24;
    public int PvPStagingX { get; set; } = 158;
    public int PvPStagingY { get; set; } = 278;
    public int PvPRoundSeconds { get; set; } = 90;
    public int PvPRestSeconds { get; set; } = 150;
    public int ChatIntervalSeconds { get; set; } = 45;
    public int MaxBots { get; set; } = 20;
    public string AccountPrefix { get; set; } = "bot";
    public string Password { get; set; } = "bot123456";
    public string ChatPrefix { get; set; } = "大家好";
    public string ClientHashPath { get; set; } = "";
    public string DatabasePath { get; set; } = "Debug/Server/Database";
    public string MapPath { get; set; } = "Debug/Client/Map";

    public static BotConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var config = new BotConfig();
            File.WriteAllText(path, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
            return config;
        }

        return JsonSerializer.Deserialize<BotConfig>(File.ReadAllText(path), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new BotConfig();
    }
}
