using Library;

namespace Zircon.BotRunner;

public enum BotPersonality
{
    /// <summary>勤奋打怪型: 大部分时间外出练级。</summary>
    Grinder,
    /// <summary>社交组队型: 组队/跟队/辅助。</summary>
    Social,
    /// <summary>悠闲挂机型: 城内闲逛/钓鱼/挖矿/聊天。</summary>
    Idle,
    /// <summary>商人型: 逛街/摆摊式往返/交易。</summary>
    Merchant,
}

public enum BotChronotype
{
    /// <summary>早鸟: 白天活跃, 傍晚后倾向回城休息。</summary>
    EarlyBird,
    /// <summary>夜猫: 白天休息, 晚上活跃打怪。</summary>
    NightOwl,
    /// <summary>全天在线。</summary>
    AllDay,
}

/// <summary>
/// 每个机器人的人格档案。按序号 + 当天日期生成稳定种子, 同一天内人格不变,
/// 重启 BotRunner 也不会变(像同一个"人"每天都上线)。
/// 职业按 (index-1)%4 轮转, 与 Tools/BotProvisioner 的种子规则一致,
/// 这样预置账号的职业与人格期望吻合。
/// </summary>
public sealed class BotProfile
{
    public readonly int Index;
    public readonly MirClass Class;
    public readonly BotPersonality Personality;
    public readonly BotChronotype Chronotype;

    // 行为权重(0~1): 调度器打分的乘数。
    public readonly double WeightGrind;
    public readonly double WeightSocial;
    public readonly double WeightIdle;
    public readonly double WeightTrade;

    /// <summary>安全区练技能倾向。道士必练召唤; 其他职业按人格概率。</summary>
    public readonly bool Trainer;

    /// <summary>社交型中的队长人选(发组队邀请)。</summary>
    public readonly bool LeaderRole;

    /// <summary>生活玩法(钓鱼/挖矿), 悠闲型专属。</summary>
    public readonly bool Lifestyle;

    /// <summary>PvP 爱好者(配合 BotConfig.EnableBotPvP)。</summary>
    public readonly bool PvpRole;

    private BotProfile(int index, MirClass cls, BotPersonality personality, BotChronotype chrono,
        double grind, double social, double idle, double trade, bool trainer, bool leader, bool lifestyle, bool pvp)
    {
        Index = index;
        Class = cls;
        Personality = personality;
        Chronotype = chrono;
        WeightGrind = grind;
        WeightSocial = social;
        WeightIdle = idle;
        WeightTrade = trade;
        Trainer = trainer;
        LeaderRole = leader;
        Lifestyle = lifestyle;
        PvpRole = pvp;
    }

    public static MirClass ClassForIndex(int index)
        => (MirClass)((index - 1) % 4);

    public static BotProfile Create(int index, BotConfig config)
    {
        // 种子 = 序号 + 当天日期: 当天稳定, 隔天缓慢演化。
        int seed = index * 7919 + DateTime.Today.DayOfYear * 131;
        var rng = new Random(seed);

        var cls = ClassForIndex(index);
        var personality = PickPersonality(rng, config);
        var chrono = (BotChronotype)rng.Next(3);

        double wGrind = personality == BotPersonality.Grinder ? 1.0 : 0.45 + rng.NextDouble() * 0.2;
        double wSocial = personality == BotPersonality.Social ? 1.0 : 0.3 + rng.NextDouble() * 0.2;
        double wIdle = personality == BotPersonality.Idle ? 1.0 : 0.25 + rng.NextDouble() * 0.2;
        double wTrade = personality == BotPersonality.Merchant ? 1.0 : 0.2 + rng.NextDouble() * 0.15;

        // 道士必练召唤(大刀旁召唤是标志性真人行为); 其他职业 45% 会进城练技。
        bool trainer = cls == MirClass.Taoist || rng.NextDouble() < 0.45;
        // 队长按序号确定性轮转(每 4 个 1 个, 组队规模 3~5 人):
        // 保证任意 bot 数量下都有队长, 不受人格随机影响。
        bool leader = index % 4 == 2;
        bool lifestyle = personality == BotPersonality.Idle && rng.NextDouble() < 0.6;
        bool pvp = config.EnableBotPvP && personality != BotPersonality.Idle && rng.NextDouble() < 0.2;

        return new BotProfile(index, cls, personality, chrono, wGrind, wSocial, wIdle, wTrade,
            trainer, leader, lifestyle, pvp);
    }

    private static BotPersonality PickPersonality(Random rng, BotConfig config)
    {
        var weights = config.PersonalityWeights;
        double roll = rng.NextDouble() * (weights.Grinder + weights.Social + weights.Idle + weights.Merchant);
        if ((roll -= weights.Grinder) < 0) return BotPersonality.Grinder;
        if ((roll -= weights.Social) < 0) return BotPersonality.Social;
        if ((roll -= weights.Idle) < 0) return BotPersonality.Idle;
        return BotPersonality.Merchant;
    }

    /// <summary>作息影响: 活跃度 0~1(打怪/组队分乘数; 休息分除数)。</summary>
    public double ActivityNow(DateTime now)
        => Chronotype switch
        {
            BotChronotype.EarlyBird => HourOf(now) is >= 6 and < 12 ? 1.0 : HourOf(now) is >= 12 and < 19 ? 0.7 : 0.25,
            BotChronotype.NightOwl => HourOf(now) is >= 20 or < 4 ? 1.0 : HourOf(now) is >= 12 and < 20 ? 0.5 : 0.3,
            _ => 0.85,
        };

    private static int HourOf(DateTime now) => now.Hour;

    public override string ToString()
        => $"{Personality}/{Chronotype} {Class} trainer={Trainer} leader={LeaderRole} life={Lifestyle} pvp={PvpRole}";
}
