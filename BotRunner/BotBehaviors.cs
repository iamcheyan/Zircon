using System.Drawing;
using Library;
using Library.Network;
using Library.SystemModels;
using C = Library.Network.ClientPackets;
using S = Library.Network.ServerPackets;

namespace Zircon.BotRunner;

/// <summary>
/// 拟真行为接口: 每 Tick 由调度器打分取最高执行(带滞回), 行为内部用
/// 自己的状态机跨越多个 Tick 完成动作序列。
/// </summary>
public interface IBotBehavior
{
    string Name { get; }

    /// <summary>0 = 本 tick 不参与竞争。分数要素: 人格权重 × 条件紧迫度。</summary>
    double Score(BotAgent bot, DateTime now);

    /// <summary>执行本 tick 的动作(移动一步/施法一次/发包)。由调度器保证互斥。</summary>
    void Execute(BotAgent bot, DateTime now);

    /// <summary>activity report 里的行为统计。</summary>
    string Stats { get; }
}

/// <summary>
/// Utility 调度器: 全部行为打分, 最高者执行。滞回: 现行行为被显著超越
/// (1.3 倍 + 5)才切换, 避免两个高分行为逐 tick 抖动。
/// </summary>
public sealed class BotBehaviorScheduler
{
    private readonly IBotBehavior[] _behaviors;
    private IBotBehavior _current;
    public string Current => _current?.Name ?? "none";

    private readonly double _switchRatio;

    public BotBehaviorScheduler(IBotBehavior[] behaviors, double switchRatio = 1.3)
    {
        _behaviors = behaviors;
        _switchRatio = switchRatio;
    }
    public IBotBehavior Pick(BotAgent bot, DateTime now)
    {
        IBotBehavior best = null;
        double bestScore = 0;
        foreach (var behavior in _behaviors)
        {
            double score = SafeScore(behavior, bot, now);
            if (score > bestScore) { bestScore = score; best = behavior; }
        }

        if (best == null) return null;

        if (_current != null && best != _current)
        {
            double currentScore = SafeScore(_current, bot, now);
            if (currentScore > 0 && bestScore < currentScore * _switchRatio + 5)
            bot.Log($"behavior: {_current.Name} -> {best.Name} (score {currentScore:F0}->{bestScore:F0})");
        }
        _current = best;
        return _current;
    }

    private static double SafeScore(IBotBehavior behavior, BotAgent bot, DateTime now)
    {
        try { return behavior.Score(bot, now); }
        catch { return 0; }
    }
}

// ============================================================================
// ⭐ 安全区练技能(核心新行为)
// 道士: 走到本图守卫(大刀)旁 ±4 格, 循环召唤(骷髅/神兽/圣兽, 已满则召回
//       身边再召=反复练) + 给周围人群放群体 buff + 治疗受伤玩家。
// 法师: 维持魔法盾 CD 循环(每施放一次涨熟练度)。
// 战士/刺客: 挥武器技能空练(挪到训练点)。
// ============================================================================
public sealed class SafeZoneTrainingBehavior : IBotBehavior
{
    internal static readonly MagicType[] SummonMagics =
    {
        MagicType.SummonDemonicCreature, MagicType.SummonShinsu, MagicType.SummonJinSkeleton,
        MagicType.SummonSkeleton, MagicType.SummonDead,
    };
    private static readonly MagicType[] BuffMagics =
    {
        MagicType.MagicResistance, MagicType.BloodLust, MagicType.BrainStorm,
        MagicType.ElementalSuperiority, MagicType.LifeSteal, MagicType.MassInvisibility,
        MagicType.Might, MagicType.Defiance, MagicType.Endurance, MagicType.Invincibility,
    };
    private static readonly MagicType[] HealMagics =
    {
        MagicType.EmpoweredHealing, MagicType.Heal, MagicType.MassHeal, MagicType.CelestialLight,
    };

    public string Name => "train";

    private Point _spot = Point.Empty;
    private int _spotMap;
    private DateTime _spotChosenAt = DateTime.MinValue;
    private DateTime _nextCast = DateTime.MinValue;
    private DateTime _nextBuffCast = DateTime.MinValue;
    private DateTime _nextHealCheck = DateTime.MinValue;
    private int _summonCasts;
    private int _buffCasts;
    private int _healCasts;

    public string Stats => $"summon={_summonCasts} buff={_buffCasts} heal={_healCasts}";

    public double Score(BotAgent bot, DateTime now)
    {
        if (!bot.Config.EnableSkillTraining || !bot.Profile.Trainer) return 0;
        if (!bot.InTownArea) return 0;
        // 道士练召唤不卡补给门: 真人回城等 CD/复活后就地练,
        // 否则供给链(东南商店 250 格)会永久压住训练。
        if (bot.World.Class != MirClass.Taoist && bot.NeedsShopping) return 0;

        bool hasTrainable = bot.World.Class switch
        {
            MirClass.Taoist => bot.FindMagic(SummonMagics) != null || bot.FindMagic(BuffMagics) != null,
            MirClass.Wizard => bot.FindMagic(MagicType.SuperiorMagicShield, MagicType.MagicShield) != null,
            _ => bot.SelectAttackSkill() != MagicType.None,
        };
        if (!hasTrainable) return 0;

        // 道士练召唤是标志性真人行为: 城内高分(75, 高于队员集合 15/20,
        // 低于助攻 110); 其他职业在城驻留时顺路练。
        return bot.World.Class == MirClass.Taoist
            ? 75 * (0.85 + bot.Profile.WeightIdle * 0.15)
            : 38 * (0.8 + bot.Profile.WeightIdle * 0.3);
    }

    public void Execute(BotAgent bot, DateTime now)
    {
        // 换图后训练点失效
        if (_spot != Point.Empty && _spotMap != 0 && bot.World.MapIndex != _spotMap)
            _spot = Point.Empty;

        if (_spot == Point.Empty || (now - _spotChosenAt).TotalMinutes > 30)
        {
            _spot = ChooseTrainingSpot(bot);
            _spotMap = bot.World.MapIndex;
            _spotChosenAt = now;
            if (_spot != Point.Empty)
                bot.Log($"train: spot {_spot} (guard={(bot.World.Class == MirClass.Taoist ? "yes" : "no")})");
        }
        if (_spot == Point.Empty) return;

        if (bot.DistanceTo(_spot) > 4)
        {
            bot.MoveToDestination(_spot, now);
            return;
        }

        // 到位后偶尔转身张望(真人)
        if (bot.Rng.NextDouble() < 0.08)
            bot.Connection.Enqueue(new C.Turn { Direction = (MirDirection)bot.Rng.Next(8) });

        switch (bot.World.Class)
        {
            case MirClass.Taoist: PracticeTaoist(bot, now); break;
            case MirClass.Wizard: PracticeWizard(bot, now); break;
            default: PracticeWeapon(bot, now); break;
        }
    }

    private void PracticeTaoist(BotAgent bot, DateTime now)
    {
        // 1) 召唤循环: 宠未满召新宠; 已满再施放=召回身边, 视觉上就是
        //    "在大刀旁反复练召唤", 每次施放都涨熟练度。
        if (now >= _nextCast)
        {
            var summon = bot.FindMagic(SummonMagics);
            if (summon != null && bot.CastMagic(summon, 0, bot.World.Location, (MirDirection)bot.Rng.Next(8)))
            {
                _summonCasts++;
                // 服务端 MagicDelay=2s 硬节流 + 拟真抖动
                _nextCast = now.AddSeconds(2.8 + bot.Rng.NextDouble() * 1.8);
                bot.Log($"train: summon {summon.Info.Name} (#{_summonCasts} pets={bot.OwnedSummonCount()})");
                return;
            }
            _nextCast = now.AddSeconds(3);
            return;
        }

        // 2) 群体 buff: 对自己脚下放, 覆盖周围人群(给别人加 buff 练熟练度)
        if (now >= _nextBuffCast)
        {
            var buff = bot.FindMagic(BuffMagics);
            if (buff != null && bot.CastMagic(buff, 0, bot.World.Location, MirDirection.Down))
            {
                _buffCasts++;
                _nextBuffCast = now.AddSeconds(7 + bot.Rng.NextDouble() * 5);
                bot.Log($"train: group buff {buff.Info.Name}");
                return;
            }
            _nextBuffCast = now.AddSeconds(5);
            return;
        }

        // 3) 治疗附近受伤的玩家
        if (now >= _nextHealCheck)
        {
            _nextHealCheck = now.AddSeconds(2);
            var heal = bot.FindMagic(HealMagics);
            if (heal != null && bot.TryHealNearby(heal))
                _healCasts++;
        }
    }

    private void PracticeWizard(BotAgent bot, DateTime now)
    {
        if (now < _nextCast) return;
        var shield = bot.FindMagic(MagicType.SuperiorMagicShield) ?? bot.FindMagic(MagicType.MagicShield);
        if (shield == null) return;
        if (bot.CastMagic(shield, bot.World.SelfObjectId, bot.World.Location, MirDirection.Down))
        {
            _buffCasts++;
            // 盾有持续时间: 按节奏补盾, 不是无脑刷
            _nextCast = now.AddSeconds(30 + bot.Rng.NextDouble() * 25);
            bot.Log($"train: shield {shield.Info.Name} (#{_buffCasts})");
        }
        else
            _nextCast = now.AddSeconds(5);
    }

    private void PracticeWeapon(BotAgent bot, DateTime now)
    {
        if (now < _nextCast) return;
        _nextCast = now.AddSeconds(4 + bot.Rng.NextDouble() * 3);
        bot.SwingWeaponSkill();
        _buffCasts++;
    }

    private static Point ChooseTrainingSpot(BotAgent bot)
    {
        // 道士: 距自己最近的守卫(大刀)旁——真人练召唤的站位;
        // 其他职业: 城中心锚点附近的可走格。
        if (bot.World.Class == MirClass.Taoist)
        {
            var guard = bot.NearestGuardSpot();
            if (guard != Point.Empty) return guard;
        }
        return bot.RandomWalkableNear(bot.HomeAnchor, 6);
    }
}

// ============================================================================
// 打怪(练级): 按等级选 Respawn 刷怪区 → 出城 → 战斗循环 → 捡掉落
// → 到点/背包满/缺药回城。
// ============================================================================
public sealed class GrindFarmingBehavior : IBotBehavior
{
    public string Name => "grind";

    private enum Phase { Dwell, TravelOut, Hunt, ReturnHome }
    private Phase _phase = Phase.Dwell;
    private DateTime _phaseSince = DateTime.MinValue;
    private DateTime _nextTrip = DateTime.MinValue;
    private DateTime _huntEnd = DateTime.MinValue;
    private Point _zoneAnchor = Point.Empty;
    private int _zoneMapIndex;
    private string _zoneName = "";
    private DateTime _nextShieldCheck = DateTime.MinValue;
    private DateTime _nextSummonCheck = DateTime.MinValue;
    private int _attacks;

    public bool Traveling => _phase is Phase.TravelOut or Phase.ReturnHome;

    public string Stats => $"zone={_zoneName} phase={_phase} attacks={_attacks}";

    public double Score(BotAgent bot, DateTime now)
    {
        if (!bot.Config.EnableGrinding) return 0;
        // 队员跟随优先由 GroupPlay 接管; 队长带团打怪走本行为
        if (bot.IsGroupMember && !bot.IsGroupLeader) return 0;
        if (bot.World.Dead) return 0;
        if (bot.NeedsShopping && bot.InTownArea) return 0; // 补给链优先

        double score;
        if (_phase == Phase.Dwell)
        {
            // 城驻留期间不与练技/休息竞争(返回 0); 到点出发时分数冲高,
            // 否则 Score 永远低于 train 会导致 Execute 得不到执行、行程
            // 永远不开始的饥饿死锁。
            if (now < _nextTrip) return 0;
            score = 85 * bot.Profile.WeightGrind * bot.Profile.ActivityNow(now);
        }
        else
        {
            // 行程/狩猎进行中不被打断
            score = 48 * bot.Profile.WeightGrind + 25;
        }
        if (bot.Profile.Personality == BotPersonality.Grinder) score += 10;
        return score;
    }
    public void Execute(BotAgent bot, DateTime now)
    {
        switch (_phase)
        {
            case Phase.Dwell:
                if (_nextTrip == DateTime.MinValue)
                    _nextTrip = now.AddSeconds(12 + bot.Rng.NextDouble() * 50);
                if (now >= _nextTrip)
                {
                    var zone = ChooseHuntingZone(bot);
                    if (zone.Anchor == Point.Empty) { _nextTrip = now.AddSeconds(60); return; }
                    _zoneAnchor = zone.Anchor;
                    _zoneMapIndex = zone.MapIndex;
                    _zoneName = zone.Description;
                    _phase = Phase.TravelOut;
                    _phaseSince = now;
                    bot.AutoPathTo(zone.MapIndex, zone.Anchor);
                    bot.Log($"grind: trip to {zone.Description} map={zone.MapIndex} {zone.Anchor}");
                }
                break;

            case Phase.TravelOut:
                if (bot.World.MapIndex == _zoneMapIndex && bot.DistanceTo(_zoneAnchor) < 12 ||
                    (now - _phaseSince).TotalMinutes > 3.5)
                {
                    bool timedOut = (now - _phaseSince).TotalMinutes > 3.5;
                    _phase = Phase.Hunt;
                    _phaseSince = now;
                    _huntEnd = now.AddSeconds(150 + bot.Rng.NextDouble() * 210);
                    bot.Log($"grind: hunting at {bot.World.Location} (timeout={timedOut})");
                }
                break;

            case Phase.Hunt:
                Hunt(bot, now);
                break;

            case Phase.ReturnHome:
                if (bot.NearHome(12))
                {
                    _phase = Phase.Dwell;
                    _nextTrip = now.AddSeconds(bot.Config.HomeDwellSecondsMin +
                        bot.Rng.NextDouble() * (bot.Config.HomeDwellSecondsMax - bot.Config.HomeDwellSecondsMin));
                    bot.Log("grind: back in town");
                }
                else if ((now - _phaseSince).TotalMinutes > 4)
                {
                    // 回程卡死(无路线/卷轴失败): 重新发起寻路, 让跨图兜底链介入
                    bot.AutoPathTo(bot.Config.HomeMapIndex, bot.HomeAnchor);
                    _phaseSince = now;
                }
                break;
        }
    }

    private void Hunt(BotAgent bot, DateTime now)
    {
        // 血量危险: 先拉开距离(喝药由背景补给反应)
        if (bot.World.MaxHealth > 0 && bot.World.CurrentHealth * 100 < bot.World.MaxHealth * 25)
        {
            if (bot.CanMove(now))
                bot.WalkStepAwayFromThreat(now);
            return;
        }

        bool tripOver = now >= _huntEnd || bot.BagNearlyFull || bot.PotionSupplyLow;
        if (tripOver)
        {
            _phase = Phase.ReturnHome;
            _phaseSince = now;
            bot.AutoPathTo(bot.Config.HomeMapIndex, bot.HomeAnchor);
            bot.Log($"grind: trip over (bag={bot.BagNearlyFull} potion={bot.PotionSupplyLow}), head home");
            return;
        }

        MaintainClass(bot, now);

        var target = bot.SelectHuntTarget(now);
        if (target != null)
        {
            if (bot.CombatStep(target, now))
                _attacks++;
            return;
        }

        // 没目标: 捡附近掉落, 或朝刷怪锚点小范围游走
        if (bot.TryLootStep(now)) return;
        if (bot.CanMove(now))
            bot.MoveToDestination(bot.RandomWalkableNear(_zoneAnchor, 8), now);
    }

    private void MaintainClass(BotAgent bot, DateTime now)
    {
        if (now >= _nextShieldCheck)
        {
            _nextShieldCheck = now.AddSeconds(20 + bot.Rng.NextDouble() * 15);
            var shield = bot.FindMagic(MagicType.SuperiorMagicShield) ?? bot.FindMagic(MagicType.MagicShield);
            if (shield != null && bot.CastMagic(shield, bot.World.SelfObjectId, bot.World.Location, MirDirection.Down))
                bot.Log($"grind: maintain {shield.Info.Name}");
        }

        if (bot.World.Class == MirClass.Taoist && now >= _nextSummonCheck && bot.OwnedSummonCount() == 0)
        {
            _nextSummonCheck = now.AddSeconds(8);
            var summon = bot.FindMagic(SafeZoneTrainingBehavior.SummonMagics);
            if (summon != null && bot.CastMagic(summon, 0, bot.World.Location, (MirDirection)bot.Rng.Next(8)))
                bot.Log($"grind: resummon {summon.Info.Name}");
        }
    }

    internal static (Point Anchor, int MapIndex, string Description) ChooseHuntingZone(BotAgent bot)
    {
        // 规则: 在家图(比奇县)里选怪物等级与自己最接近的刷怪区; 升级后
        // 换区(每次出发都重算)。低级区在城北, 顺序即"出城→打怪区"。
        int myLevel = Math.Max(1, bot.World.Level);
        MapInfo homeMap = bot.MapInfoByIndex(bot.Config.HomeMapIndex);
        if (homeMap?.Regions == null) return (Point.Empty, 0, "");

        RespawnInfo bestRespawn = null;
        int bestGap = int.MaxValue;
        foreach (var region in homeMap.Regions)
        {
            if (region.Respawns == null) continue;
            foreach (var respawn in region.Respawns)
            {
                MonsterInfo monster = respawn.Monster;
                if (monster == null || respawn.EventSpawn) continue;
                int gap = Math.Abs(monster.Level - myLevel);
                if (gap < bestGap)
                {
                    bestGap = gap;
                    bestRespawn = respawn;
                }
            }
        }
        if (bestRespawn == null) return (Point.Empty, 0, "");

        int width = bot.MapWidthOf(homeMap);
        var points = bestRespawn.Region?.GetPoints(width);
        if (points == null || points.Count == 0) return (Point.Empty, 0, "");

        string desc = $"{bestRespawn.Monster.MonsterName} Lv{bestRespawn.Monster.Level}";
        var candidates = points.ToArray();
        // 最多试 6 个随机点, 只接受本地 A* 真能规划出路线(否则行程半路
        // 卡死, 靠超时兜底浪费整个行程), 且距离 ≤120 格——比奇野外刷怪
        // 区密布, 走太远等于死亡行军, 真人也是就近选点。
        for (int i = 0; i < 6 && i < candidates.Length; i++)
        {
            var anchor = candidates[bot.Rng.Next(candidates.Length)];
            if (bot.DistanceTo(anchor) > 120) continue;
            var probe = new BotPathfinder(bot.CurrentMapData);
            if (probe.SetDestination(bot.World.Location, anchor))
                return (anchor, homeMap.Index, desc);
        }
        return (Point.Empty, 0, "");
    }
}

// ============================================================================
// 组队协同: 队长发邀请 → 队员跟随(保持 3~5 格) → 队长打怪时队员助攻
// ============================================================================
public sealed class GroupPlayBehavior : IBotBehavior
{
    public string Name => "group";
    public string Stats => $"invites={_invitesSent}/{_inviteRounds}r follow={_followSteps} assists={_assists}";


    private int _inviteRounds;
    private int _invitesSent;
    private int _followSteps;
    private int _assists;
    private DateTime _nextRegroupCheck = DateTime.MinValue;
    private Point _lastLeaderPoint = Point.Empty;
    private DateTime _leaderLastSeen = DateTime.MinValue;
    private DateTime _nextInviteAttempt = DateTime.MinValue;
    public double Score(BotAgent bot, DateTime now)
    {
        if (!bot.Config.EnableGrouping) return 0;
        if (bot.World.Dead) return 0;

        // 已在队伍中的队员(分场景, 数值须与滞回 1.3x+5 联动):
        // - 队长附近有怪 → 110 助攻(能压过 train 75 的滞回 102.5)
        // - 队长在视野但无战斗 → 15(各干各的: 道士练召唤/换装/休整)
        // - 队长出视野 ≤2.5min → 60 追队(低于道士训练 75: 队长跑了,
        //   道士留在城里练召唤, 真人也是这么干的)
        // - 队长失联 >2.5min → 20 放弃追赶回城等重组(防止无限追人)
        if (bot.IsGroupMember && !bot.IsGroupLeader)
        {
            var leader = bot.GroupLeaderPlayer;
            if (leader == null)
                return (now - _leaderLastSeen).TotalMinutes > 2.5 ? 20 : 45;
            _leaderLastSeen = now;
            return bot.NearestMonsterNear(leader.Location, 8) != null ? 110 : 15;
        }

        // 队长(确定性轮转): 建队窗口期得分高; 建成后交给打怪/练级行为带队。
        if (bot.Profile.LeaderRole && !bot.IsGroupMember && now >= _nextInviteAttempt)
            return 68;
        // 普通社交型落单: 偶尔凑热闹跟队走
        if (bot.Profile.WeightSocial > 0.8 && !bot.IsGroupMember && now >= _nextRegroupCheck)
            return 30;
        return 0;
    }

    public void Execute(BotAgent bot, DateTime now)
    {
        if (bot.IsGroupMember && !bot.IsGroupLeader)
        {
            FollowLeader(bot, now);
            return;
        }

        if (bot.Profile.LeaderRole && !bot.IsGroupMember)
        {
            InviteSquad(bot, now);
            return;
        }

        // 落单社交: 朝最近的同图玩家聚拢(下次自然被邀请/跟随)
        _nextRegroupCheck = now.AddSeconds(20 + bot.Rng.NextDouble() * 20);
        var other = bot.NearestOtherBot(14);
        if (other != null && bot.DistanceTo(other.Location) > 4 && bot.CanMove(now))
            bot.MoveToDestination(other.Location, now);
    }

    private void InviteSquad(BotAgent bot, DateTime now)
    {
        _nextInviteAttempt = now.AddSeconds(35 + bot.Rng.NextDouble() * 20);
        _inviteRounds++;
        int invites = 0;
        foreach (var candidate in bot.SquadCandidateNames())
        {
            if (bot.World.GroupMembers.Contains(candidate)) continue;
            bot.Connection.Enqueue(new C.GroupInvite { Name = candidate });
            _invitesSent++;
            invites++;
            if (invites >= 3) break;
        }
        if (invites > 0)
            bot.Log($"group: invite x{invites} (round {_inviteRounds}, members={bot.World.GroupMembers.Count})");
    }

    private void FollowLeader(BotAgent bot, DateTime now)
    {
        var leader = bot.GroupLeaderPlayer;
        if (leader == null)
        {
            // 失联 ≤2.5min: 朝最后已知位置追; 之后放弃回城等重组
            // (分数层同时降到 20, 行为层自然让位)。
            bool gaveUp = (now - _leaderLastSeen).TotalMinutes > 2.5;
            if (!gaveUp && _lastLeaderPoint != Point.Empty && bot.DistanceTo(_lastLeaderPoint) > 4 && bot.CanMove(now))
                bot.MoveToDestination(_lastLeaderPoint, now);
            else if (bot.CanMove(now) && bot.Rng.NextDouble() < 0.3)
                bot.MoveToDestination(bot.RandomWalkableNear(bot.HomeAnchor, 8), now);
            return;
        }
        _lastLeaderPoint = leader.Location;

        int dist = bot.DistanceTo(leader.Location);

        // 队长附近有怪: 助攻同一片战场(锁离队长最近的活怪)
        var assistTarget = bot.NearestMonsterNear(leader.Location, 8);
        if (assistTarget != null && dist <= 9)
        {
            if (bot.CombatStep(assistTarget, now)) _assists++;
            return;
        }

        if (dist > 5)
        {
            // 跟队但保持散开: 目标点 = 队长位置 + 稳定随机偏移
            var followPoint = bot.FollowPointNear(leader.Location);
            if (bot.CanMove(now))
            {
                bot.MoveToDestination(followPoint, now);
                _followSteps++;
            }
        }
        // 距离 2~5 格: 原地待命/张望(真人跟队)
        else if (bot.Rng.NextDouble() < 0.06)
            bot.Connection.Enqueue(new C.Turn { Direction = (MirDirection)bot.Rng.Next(8) });
    }
}

// ============================================================================
// 装备成长: 背包里评分更高且职业/性别/等级匹配的装备穿上。
// ============================================================================
public sealed class EquipUpgradeBehavior : IBotBehavior
{
    public string Name => "equip";

    private DateTime _nextScan = DateTime.MinValue;
    private DateTime _nextSwap = DateTime.MinValue;
    private int _equips;

    public string Stats => $"equips={_equips}";

    public double Score(BotAgent bot, DateTime now)
    {
        if (!bot.Config.EnableEquipUpgrade) return 0;
        if (now < _nextScan) return 0;
        _nextScan = now.AddSeconds(3 + bot.Rng.NextDouble() * 3);
        return bot.HasBetterUnequippedItem() ? 80 : 0;
    }

    public void Execute(BotAgent bot, DateTime now)
    {
        if (now < _nextSwap) return;
        _nextSwap = now.AddSeconds(4);
        if (bot.EquipBestUpgrade())
            _equips++;
    }
}

// ============================================================================
// 休息: 悠闲型/夜间在安全区小范围踱步/驻足/下马张望
// ============================================================================
public sealed class RestIdleBehavior : IBotBehavior
{
    public string Name => "rest";

    private DateTime _nextWander = DateTime.MinValue;
    private Point _restSpot = Point.Empty;
    private int _idleTicks;

    public string Stats => $"ticks={_idleTicks}";

    public double Score(BotAgent bot, DateTime now)
    {
        if (!bot.InTownArea) return 0;
        if (bot.NeedsShopping) return 0;
        double activity = bot.Profile.ActivityNow(now);
        // 悠闲型或"夜晚"都提高休息权重
        return 20 + bot.Profile.WeightIdle * 25 + (1 - activity) * 30;
    }

    public void Execute(BotAgent bot, DateTime now)
    {
        _idleTicks++;
        if (bot.World.Horse != HorseType.None && bot.Rng.NextDouble() < 0.02)
        {
            bot.Connection.Enqueue(new C.Mount());
            bot.Log("rest: dismount in town");
            return;
        }
        if (now < _nextWander) return;
        // 驻足时间 > 走动时间: 真人大部分时间是站着的
        if (bot.Rng.NextDouble() < 0.55)
        {
            _nextWander = now.AddSeconds(2 + bot.Rng.NextDouble() * 4);
            return;
        }
        _nextWander = now.AddSeconds(1.5 + bot.Rng.NextDouble() * 1.5);
        // 离家太远时回锚点附近歇脚(防止随机漂移出城)
        if (bot.DistanceTo(bot.HomeAnchor) > 28)
            _restSpot = bot.RandomWalkableNear(bot.HomeAnchor, 5);
        else if (_restSpot == Point.Empty || bot.DistanceTo(_restSpot) > 10)
            _restSpot = bot.RandomWalkableNear(bot.World.Location, 4);
        if (bot.CanMove(now))
            bot.MoveToDestination(_restSpot, now);
    }
}

// ============================================================================
// 巡逻兜底: 沿用原有锚点漂移闲逛逻辑
// ============================================================================
public sealed class PatrolFallbackBehavior : IBotBehavior
{
    public string Name => "patrol";

    private Point _target = Point.Empty;
    private DateTime _pauseUntil = DateTime.MinValue;
    private int _steps;

    public string Stats => $"steps={_steps}";

    public double Score(BotAgent bot, DateTime now) => 5; // 永远可用, 永远最低

    public void Execute(BotAgent bot, DateTime now)
    {
        if (bot.Rng.NextDouble() < 0.12) return; // 偶发驻足
        bool arrived = _target != Point.Empty && bot.DistanceTo(_target) <= 1;
        if (arrived && now < _pauseUntil) return;
        if (arrived || _target == Point.Empty)
        {
            // 离家太远时目标选回家锚点附近(闲逛是扩散随机游走,
            // 不加引力会一路漂出城, 把"在城里"的行为全封死)。
            _target = bot.DistanceTo(bot.HomeAnchor) > 28
                ? bot.RandomWalkableNear(bot.HomeAnchor, 6)
                : bot.RandomWalkableNear(bot.World.Location, 6);
            if (arrived) _pauseUntil = now.AddSeconds(1.5 + bot.Rng.NextDouble() * 2.5);
        }
        if (bot.CanMove(now))
        {
            bot.MoveToDestination(_target, now);
            _steps++;
        }
    }
}

/// <summary>中文聊天语料: 按场景模板 + 变量生成, 避免全服同一句。</summary>
public static class BotChatCorpus
{
    private static readonly string[] Idle =
    {
        "今天{map}人真多", "有没有人看到{map}的{monster}", "收点药钱, 谁带带我",
        "刚回城, 修下装备去", "{map}真热闹", "走了走了, 出去练级",
        "这游戏还是人多好玩", "无聊, 谁聊两句", "今天运气一般",
    };
    private static readonly string[] Grinding =
    {
        "这{monster}太耐打了", "打了半天{monster}, 蓝都喝光了", "谁来{map}一起练级",
        "{monster}又刷了", "差一级就能换图了", "组队练级效率高多了",
        "这波掉了不少东西", "小心点, {monster}有点疼",
    };
    private static readonly string[] Lfg =
    {
        "求组! {cls}一名, {map}练级", "有没有队缺人, {cls}求组", "来人组队刷{monster}, 差你一个",
        "{cls}找队伍, 走起", "组队练级来人",
    };
    private static readonly string[] Trade =
    {
        "出点刚打的装备, 价格好商量", "收武器, 有的密我", "清理背包, 便宜出了",
        "高价收{monster}掉的装备",
    };

    public static string Compose(BotAgent bot, Random rng)
    {
        var (map, monster) = bot.ChatContext();
        string cls = bot.World.Class switch
        {
            MirClass.Warrior => "战士", MirClass.Wizard => "法师",
            MirClass.Taoist => "道士", MirClass.Assassin => "刺客", _ => "玩家",
        };
        string[] pool = bot.InTownArea
            ? (bot.Profile.Personality == BotPersonality.Merchant ? Trade :
               rng.NextDouble() < 0.3 ? Lfg : Idle)
            : Grinding;
        return pool[rng.Next(pool.Length)]
            .Replace("{map}", map)
            .Replace("{monster}", monster)
            .Replace("{cls}", cls);
    }
}
