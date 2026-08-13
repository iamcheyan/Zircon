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
    // 所有角色以城中心出生点为活动主场: 各自在出生点周围选一个"家"角落。
    // 出生点是比奇县(地图索引 1), 登录时服务器返回的 SpawnMapIndex 是角色
    // 下线位置, 不是固定出生图, 因此这里显式配置家的地图与坐标。
    public int HomeMapIndex { get; set; } = 1;
    public int HomeMapX { get; set; } = 159;
    public int HomeMapY { get; set; } = 233;
    public int HomeAnchorRadius { get; set; } = 15;
    // 打怪行为外出节奏: 在城驻留 HomeDwell 秒 → 野外狩猎 150~360s → 回城。
    public int HomeDwellSecondsMin { get; set; } = 90;
    public int HomeDwellSecondsMax { get; set; } = 180;
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
    public bool AutoCreateAccount { get; set; } = true;
    public string ChatPrefix { get; set; } = "大家好";
    public string ClientHashPath { get; set; } = "";
    public string DatabasePath { get; set; } = "Debug/Server/Database";
    public string MapPath { get; set; } = "Debug/Client/Map";

    // ==== 拟真行为系统 ====
    public bool EnableSkillTraining { get; set; } = true;
    public bool EnableGrouping { get; set; } = true;
    public bool EnableGrinding { get; set; } = true;
    public bool EnableEquipUpgrade { get; set; } = true;
    public bool EnableChatCorpus { get; set; } = true;
    // 人格分布权重(无需归一, 按比例抽取)
    public PersonalityWeightsConfig PersonalityWeights { get; set; } = new();
    // 行为调度的滞回参数
    public double BehaviorSwitchRatio { get; set; } = 1.3;

    public sealed class PersonalityWeightsConfig
    {
        public double Grinder { get; set; } = 0.50;
        public double Social { get; set; } = 0.25;
        public double Idle { get; set; } = 0.15;
        public double Merchant { get; set; } = 0.10;
    }

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
