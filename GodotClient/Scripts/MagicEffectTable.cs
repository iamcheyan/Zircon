using System.Collections.Generic;
using Godot;
using Library;

namespace ZirconClient.Scripts;

/// <summary>
/// 魔法特效定义表 (提取自原版 Client/Models/MapObject.cs:768 的 case MirAction.Spell)。
/// 每个魔法: 施法站桩特效 + 可选飞行弹道 + 可选落地/命中特效。
/// 颜色直接使用原版 Globals.*Colour，避免 Godot 端手工猜色造成元素特效偏色。
/// 当前表按原版的施法轨迹维护：投射物、命中特效、地面/范围特效和自身特效分别配置。
/// 未覆盖的技能不再伪造通用爆炸，由 GameScene 记录诊断，便于继续补齐原版资源。
/// </summary>
public static class MagicEffectTable
{
    // 直接对照 Client/Models/MapObject.SetAction 的 MirAction.Spell 分支。
    // 这不是“所有 MagicType”，而是原版确实会在施法包中创建地图/目标
    // 特效的集合；被动、Buff、纯动作和 Effect 包在这里不能算缺失。
    private static readonly HashSet<MagicType> OriginalSpellCases = new()
    {
        MagicType.AdamantineFireBall, MagicType.Assault, MagicType.Asteroid,
        MagicType.AugmentPoisonDust, MagicType.BindingTalisman, MagicType.BloodLust,
        MagicType.BlowEarth, MagicType.BrainStorm, MagicType.BurningFire,
        MagicType.CelestialLight, MagicType.Chain, MagicType.ChainLightning,
        MagicType.CorpseExploder, MagicType.CrescentMoon, MagicType.Cyclone,
        MagicType.DarkSoulPrison, MagicType.DragonTornado, MagicType.ElectricShock,
        MagicType.ElementalSuperiority, MagicType.EvilSlayer, MagicType.ExpelUndead,
        MagicType.ExplosiveTalisman, MagicType.FireBall, MagicType.FireBounce,
        MagicType.FireStorm, MagicType.FireSword, MagicType.FlamingDaggers,
        MagicType.FourWheels, MagicType.FrozenDragon, MagicType.FrozenEarth,
        MagicType.GreaterEvilSlayer, MagicType.GreaterFrozenEarth,
        MagicType.GreenSludgeBall, MagicType.GustBlast, MagicType.Heal,
        MagicType.HeavenlySky, MagicType.HellFire, MagicType.Hemorrhage,
        MagicType.HundredFist, MagicType.IceAura, MagicType.IceBlades,
        MagicType.IceBolt, MagicType.IceBreaker, MagicType.IceDragon,
        MagicType.IceRain, MagicType.IceStorm, MagicType.ImprovedExplosiveTalisman,
        MagicType.LifeSteal, MagicType.LightningBall, MagicType.LightningBeam,
        MagicType.LightningStrike, MagicType.LightningWave, MagicType.MagicCombustion,
        MagicType.MagicResistance, MagicType.MassHeal, MagicType.MassInvisibility,
        MagicType.MeteorShower, MagicType.MonsterIceStorm,
        MagicType.MonsterScortchedEarth, MagicType.MonsterThunderStorm,
        MagicType.Neutralize, MagicType.Parasite, MagicType.PinkFireBall,
        MagicType.PoisonCloud, MagicType.PoisonDust, MagicType.Purification,
        MagicType.Resilience, MagicType.Resurrection, MagicType.SamaBlackIce,
        MagicType.SamaBlueLightning, MagicType.SamaGuardianFire,
        MagicType.SamaGuardianIce, MagicType.SamaGuardianLightning,
        MagicType.SamaGuardianWind, MagicType.SamaPhoenixFire,
        MagicType.SamaProphetFire, MagicType.SamaProphetLightning,
        MagicType.SamaProphetWind, MagicType.SamaWhiteWind,
        MagicType.ScortchedEarth, MagicType.SearingLight, MagicType.Shredding,
        MagicType.SoulResonance, MagicType.StrengthOfFaith, MagicType.SwiftBlade,
        MagicType.TaecheonSword, MagicType.ThunderBolt, MagicType.ThunderStrike,
        MagicType.TrapOctagon, MagicType.WraithGrip
    };

    public static bool IsOriginalSpellCase(MagicType type) => OriginalSpellCases.Contains(type);

    // 旧端对应分支只播放音效，不创建 MirEffect；它们不是视觉迁移遗漏。
    private static readonly HashSet<MagicType> NoVisualSpellCases = new()
    {
        MagicType.CombatKick,
        MagicType.JudgementOfHeaven,
    };

    public static bool IsNoVisualSpellCase(MagicType type) => NoVisualSpellCases.Contains(type);

    /// <summary>元素颜色 (原版 Globals.*Colour 的 System.Drawing → Godot 转换)。</summary>
    public static readonly Color Fire = ToGodot(Globals.FireColour);
    public static readonly Color Ice = ToGodot(Globals.IceColour);
    public static readonly Color Lightning = ToGodot(Globals.LightningColour);
    public static readonly Color Wind = ToGodot(Globals.WindColour);
    public static readonly Color Holy = ToGodot(Globals.HolyColour);
    public static readonly Color Dark = ToGodot(Globals.DarkColour);
    public static readonly Color Phantom = ToGodot(Globals.PhantomColour);
    public static readonly Color None = ToGodot(Globals.NoneColour);
    // 原版 Globals 无这两个颜色常量，个别特效直接使用 System.Drawing 色值。
    public static readonly Color Purple = ToGodot(System.Drawing.Color.Purple);
    public static readonly Color GreenYellow = ToGodot(System.Drawing.Color.GreenYellow);

    private static Color ToGodot(System.Drawing.Color colour)
        => new(colour.R / 255f, colour.G / 255f, colour.B / 255f, colour.A / 255f);

    /// <summary>站桩特效 (施法者位置 或 地图位置)。</summary>
    public class CastEffect
    {
        public LibraryFile File;
        public int StartIndex;
        public int FrameCount;
        public int DelayMs = 100;
        public Color Colour = Colors.White;
        public bool Blend = true;
        public MirEffectNode.EffectLayer DrawType = MirEffectNode.EffectLayer.Object;
        public float BlendRate = 0.7f;
        public float Opacity = 1f;
        public int Skip = 10;
        public int FrameLight = 10;
        public double StartDelayMs;
        public int DistanceDelayMs;
        public bool DirectionFromSource;
        // 旧端目标命中特效有时直接沿 Spell 动作方向绘制，
        // 而不是按施法者到目标的方向重新选帧。
        public bool DirectionFromCast;
        // 旧端 Spell 分支中挂在施法者/自身对象上的效果（如 HealStart）。
        public bool CastAtSource;
        // 三段技能的施法者起手特效；与 Projectile/Impact 素材独立。
        public ImpactDef Source;
        public List<ImpactDef> SourceAdditional = new();
        // 旧端 LightningBeam 等: MirEffect(...){ Target=this, Direction=
        // DirectionFromPoint(施法者, 格) }——每个 MagicLocation 在施法者身上
        // 各播一次，方向指向该格；目标上不挂魔法特效 (命中表现走 Struck)。
        public List<ImpactDef> SourcePerLocation = new();
        // 旧端该技能对 AttackTargets 不创建任何魔法特效 (如 Asteroid 只有
        // 地面落点弹道)；置 true 时 targets 循环不播放回退特效。
        public bool NoTargetVisual;
        // 旧端该技能对 MagicLocations 不创建特效 (DarkSoulPrison 的 release 是
        // Target=this，只挂在施法者身上)。
        public bool NoLocationVisual;
        // 旧端 release 挂在施法者自身 (Target=this) 的整段特效 (DarkSoulPrison 600,9)。
        public bool ReleaseAtCaster;
        // 旧端弹道只到最后一个 MagicLocation (BlowEarth 的弹道逐点后移)。
        public bool ProjectileLastLocationOnly;
        // 飞行弹道: 非空表示从施法者飞到目标
        public ProjectileDef Projectile;
        public ProjectileDef TargetProjectile;
        // 落地/命中特效 (目标位置)
        public ImpactDef Impact;
        // 旧端对象 Target 收到的首段特效，可能与 MapTarget 首段不同。
        public ImpactDef TargetEffect;
        // 旧端只有部分地图弹道 (Asteroid/IceRain) 在 MapTarget 完成后播落地特效；
        // 普通火球类的落地特效只挂在对象 Target 上。
        public ImpactDef MapImpact;
        public List<ImpactDef> Additional = new();
        public List<OffsetImpactDef> AdditionalMapEffects = new();
        public List<ProjectileDef> AdditionalProjectiles = new();
        public List<ProjectileDef> TargetAdditionalProjectiles = new();
        public double ProjectileDelayStepMs;
    }

    public class ProjectileDef
    {
        public LibraryFile File;
        public int StartIndex;
        public int FrameCount;
        public int DelayMs = 100;
        public Color Colour = Colors.White;
        public bool Has16Directions = true;
        public bool Explode;
        public int Skip = 10;
        public MirEffectNode.EffectLayer DrawType = MirEffectNode.EffectLayer.Object;
        public float BlendRate = 0.7f;
        public float Opacity = 1f;
        // 旧端 MirProjectile 构造器的 start/end light 默认值为 35。
        public int FrameLight = 35;
        public int OriginOffsetX;
        public int OriginOffsetY;
        public bool OriginFromTarget;
        public double StartDelayMs;
        // 落地特效 (旧端 CompleteAction 里的 MirEffect)。
        public ImpactDef Arrival;
        // 到达音效 (旧端 CompleteAction 内的 Play，如各弹道的 *End)。
        public SoundIndex ArrivalSound = SoundIndex.None;
        // 完成音效 (旧端 CompleteAction 末尾的 Play)。
        public SoundIndex CompletionSound = SoundIndex.None;
    }

    public class ImpactDef
    {
        public LibraryFile File;
        public int StartIndex;
        public int FrameCount;
        public int DelayMs = 100;
        public Color Colour = Colors.White;
        public MirEffectNode.EffectLayer DrawType = MirEffectNode.EffectLayer.Object;
        public float BlendRate = 0.7f;
        public float Opacity = 1f;
        public int Skip = 10;
        // 旧端 MirEffect 的 StartLight/EndLight；用于夜间局部照明。
        public int FrameLight = 10;
        public double StartDelayMs;
        public int DistanceDelayMs;
        public bool DirectionFromSource;
        public bool DirectionFromCast;
        // 特效播放到该帧时播一次音效 (原版攻击表/MirEffect.FrameIndexChanged 的帧音效)。
        public int SoundFrame = -1;
        public SoundIndex SoundFrameSound = SoundIndex.None;
        // 旧端按 8 方向分组的起始帧；未配置时使用 StartIndex。
        public int[] DirectionStartIndices;

        public int ResolveStartIndex(MirDirection direction)
        {
            if (DirectionStartIndices == null || DirectionStartIndices.Length < 8)
                return StartIndex;

            return direction switch
            {
                MirDirection.Up => DirectionStartIndices[0],
                MirDirection.UpLeft => DirectionStartIndices[1],
                MirDirection.UpRight => DirectionStartIndices[1],
                MirDirection.Left => DirectionStartIndices[2],
                MirDirection.Right => DirectionStartIndices[2],
                MirDirection.DownLeft => DirectionStartIndices[3],
                MirDirection.DownRight => DirectionStartIndices[3],
                MirDirection.Down => DirectionStartIndices[4],
                _ => StartIndex
            };
        }
    }

    public sealed class OffsetImpactDef : ImpactDef
    {
        public int OffsetX;
        public int OffsetY;
    }

    /// <summary>按 MagicType 查施法特效。null 表示该技能尚未完成原版迁移。</summary>
    public static CastEffect Get(MagicType type)
    {
        if (_table.TryGetValue(type, out var def)) return def;
        return null;  // 兜底由调用方处理
    }

    /// <summary>旧端 MirAction.Attack 的攻击者目标特效（不是 Spell 的落点特效）。</summary>
    public static ImpactDef GetAttack(MagicType type)
    {
        _attackTable.TryGetValue(type, out var def);
        return def;
    }

    private static readonly Dictionary<MagicType, ImpactDef> _attackTable = new()
    {
        [MagicType.None] = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1090, FrameCount = 6, Colour = None },
        [MagicType.Slaying] = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1350, FrameCount = 6, Colour = None },
        [MagicType.Thrusting] = new ImpactDef { File = LibraryFile.MagicEx3, StartIndex = 0, FrameCount = 6, Colour = None, Skip = 10 },
        [MagicType.HalfMoon] = new ImpactDef { File = LibraryFile.Magic, StartIndex = 230, FrameCount = 6, Colour = None },
        [MagicType.DestructiveSurge] = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 1420, FrameCount = 6, Colour = None },
        [MagicType.FlamingSword] = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1470, FrameCount = 6, Colour = Fire },
        [MagicType.DragonRise] = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2185, FrameCount = 10, Colour = None },
        [MagicType.BladeStorm] = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1780, FrameCount = 10, DelayMs = 60, Colour = None },
        [MagicType.DefensiveBlow] = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 800, FrameCount = 9, Colour = Fire, DrawType = MirEffectNode.EffectLayer.Floor, StartDelayMs = 200 },
        [MagicType.FlameSplash] = new ImpactDef { File = LibraryFile.MagicEx4, StartIndex = 900, FrameCount = 8, Colour = Fire },
        [MagicType.DragonBlood] = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 200, FrameCount = 7, Colour = None },
        [MagicType.SeismicSlam] = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 4900, FrameCount = 6, Colour = Lightning },
        [MagicType.CrushingWave] = new ImpactDef { File = LibraryFile.MagicEx6, StartIndex = 100, FrameCount = 6, Colour = Lightning },
        [MagicType.Endurance] = new ImpactDef { File = LibraryFile.MagicEx3, StartIndex = 190, FrameCount = 10, Colour = None },
        [MagicType.ReflectDamage] = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 1220, FrameCount = 10, Colour = None },
        [MagicType.Fetter] = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 2370, FrameCount = 10, Colour = None },
        [MagicType.Repulsion] = new ImpactDef { File = LibraryFile.Magic, StartIndex = 90, FrameCount = 10, Colour = Wind },
        [MagicType.Renounce] = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 80, FrameCount = 10, Colour = Phantom },
        [MagicType.Tempest] = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 910, FrameCount = 10, DelayMs = 60, Colour = Wind },
        [MagicType.MirrorImage] = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 1260, FrameCount = 6, Colour = None },
        [MagicType.FrostBite] = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 500, FrameCount = 16, DelayMs = 60, Colour = Ice },
        [MagicType.Transparency] = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 430, FrameCount = 7, Colour = Phantom },
        [MagicType.CursedDoll] = new ImpactDef { File = LibraryFile.MagicEx3, StartIndex = 690, FrameCount = 10, DelayMs = 60, Colour = Fire },
        [MagicType.Spiritualism] = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 1580, FrameCount = 11, Colour = None },
        [MagicType.Containment] = new ImpactDef { File = LibraryFile.MagicEx3, StartIndex = 590, FrameCount = 9, DelayMs = 60, Colour = None },
        [MagicType.Rake] = new ImpactDef
        {
            File = LibraryFile.MagicEx4, StartIndex = 1200, FrameCount = 9, Colour = Ice,
            DirectionStartIndices = new[] { 1200, 1210, 1220, 1230, 1240, 1200, 1200, 1200 }
        },
    };

    private static readonly Dictionary<MagicType, CastEffect> _table = new()
    {
        // ---- 战士 ----
        [MagicType.SwiftBlade] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 2330, FrameCount = 16, Colour = None },
        [MagicType.TaecheonSword] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5000, FrameCount = 31, Colour = Fire },
        [MagicType.FireSword] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5100, FrameCount = 39, Colour = Fire },
        [MagicType.HalfMoon] = new CastEffect { File = LibraryFile.Magic, StartIndex = 480, FrameCount = 8, Colour = None },
        [MagicType.DestructiveSurge] = new CastEffect { File = LibraryFile.Magic, StartIndex = 500, FrameCount = 10, Colour = None },

        // ---- 法师 ----
        [MagicType.FireBall] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 420, FrameCount = 5, Colour = Fire,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1820, FrameCount = 8, DelayMs = 70, Colour = Fire },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 420, FrameCount = 5, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 580, FrameCount = 10, Colour = Fire },
        },
        [MagicType.LightningBall] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 3070, FrameCount = 6, Colour = Lightning,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2990, FrameCount = 6, DelayMs = 80, Colour = Lightning },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 3070, FrameCount = 6, Colour = Lightning },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 3230, FrameCount = 10, Colour = Lightning },
        },
        [MagicType.IceBolt] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 2700, FrameCount = 3, Colour = Ice,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2620, FrameCount = 6, DelayMs = 80, Colour = Ice },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 2700, FrameCount = 3, Colour = Ice },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2860, FrameCount = 10, Colour = Ice },
        },
        [MagicType.GustBlast] = new CastEffect
        {
            File = LibraryFile.MagicEx, StartIndex = 430, FrameCount = 5, Colour = Wind,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 350, FrameCount = 7, DelayMs = 70, Colour = Wind },
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx, StartIndex = 430, FrameCount = 5, Colour = Wind },
            Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 590, FrameCount = 10, Colour = Wind },
        },
        [MagicType.ElectricShock] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 10, FrameCount = 10, Colour = Lightning,
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 10, FrameCount = 10, Colour = Lightning },
        },
        // 原版有两段: 施法自身上段 MirEffect(1430,12,50ms){Target=this} +
        // 命中段 MirEffect(1450,3,150ms,light 150,50) 挂在 MagicLocations/
        // AttackTargets。旧实现把命中段当作施法特效，漏掉 1430 起手。
        [MagicType.ThunderBolt] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 1430, FrameCount = 12, DelayMs = 50, Colour = Lightning,
            CastAtSource = true,
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1450, FrameCount = 3, DelayMs = 150, Colour = Lightning, FrameLight = 150 },
        },
        // 原版 MapObject.cs:1084-1108: 同一帧 Effect(1450,3,150ms) 对 MagicLocations(MapTarget=point)
        // 和 AttackTargets(Target=attackTarget) 各播一次. 旧配置只有 Impact(目标), 漏了地面 MapImpact.
        [MagicType.ThunderStrike] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 1430, FrameCount = 12, DelayMs = 50, Colour = Lightning,
            CastAtSource = true,
            MapImpact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1450, FrameCount = 3, DelayMs = 150, Colour = Lightning, FrameLight = 150 },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1450, FrameCount = 3, DelayMs = 150, Colour = Lightning, FrameLight = 150 },
        },
        [MagicType.FireBounce] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 1640, FrameCount = 6, Colour = Fire,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1560, FrameCount = 9, DelayMs = 65, Colour = Fire },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 1640, FrameCount = 6, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1800, FrameCount = 10, Colour = Fire },
        },
        [MagicType.MeteorShower] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 1640, FrameCount = 6, Colour = Fire,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1560, FrameCount = 9, DelayMs = 65, Colour = Fire },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 1640, FrameCount = 6, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1800, FrameCount = 10, Colour = Fire },
        },
        [MagicType.IceBlades] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 2960, FrameCount = 6, DelayMs = 50, Colour = Ice,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2880, FrameCount = 6, DelayMs = 115, Colour = Ice },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 2960, FrameCount = 6, DelayMs = 50, Colour = Ice },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2970, FrameCount = 10, Colour = Ice },
        },
        [MagicType.Cyclone] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 1990, FrameCount = 5, Colour = Wind, Source = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1970, FrameCount = 10, DelayMs = 60, Colour = Wind, FrameLight = 50 }, MapImpact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1990, FrameCount = 5, Colour = Wind, FrameLight = 50 }, TargetEffect = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1970, FrameCount = 10, DelayMs = 60, Colour = Wind, FrameLight = 50 }, Additional = { new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 2000, FrameCount = 8, StartDelayMs = 500, Colour = Wind, FrameLight = 50 } } },
        // 原版 MapObject.cs:1351 release: Effect(950,7,100ms,MapTarget=point) 每个 MagicLocation 一枚地面火焰;
        // start(:4046): Effect(940,10,60ms,Target=this). 旧配置把 950 配成 Impact(目标命中),
        // 但 FireStorm 是地面范围魔法 — CastAtSource=true 时 destCells 循环不播 Impact, 只在 targets
        // 循环对目标对象播, 地面火焰丢失. 改为 MapImpact 在地面格播放.
        [MagicType.FireStorm] = new CastEffect { File = LibraryFile.Magic, StartIndex = 940, FrameCount = 10, DelayMs = 60, Colour = Fire, CastAtSource = true, MapImpact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 950, FrameCount = 7, Colour = Fire } },
        [MagicType.MagicShield] = new CastEffect { File = LibraryFile.Magic, StartIndex = 830, FrameCount = 19, DelayMs = 60, Colour = Phantom, CastAtSource = true },
        [MagicType.SuperiorMagicShield] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 1900, FrameCount = 17, DelayMs = 60, Colour = Fire, CastAtSource = true },

        // ---- 道士 ----
        [MagicType.Heal] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 660, FrameCount = 10, DelayMs = 60, Colour = Holy,
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 610, FrameCount = 10, Colour = Holy },
            CastAtSource = true,
        },
        // 原版: start Effect(660,10,60ms,Target=this) + release Effect(670,7,100ms,MapTarget=point) 地面治疗.
        // 旧配置把 670 配成 Impact(目标), 但 MassHeal 是地面范围治疗 — CastAtSource=true 阻断 destCells,
        // 地面治疗光环丢失. 改为 MapImpact.
        [MagicType.MassHeal] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 660, FrameCount = 10, DelayMs = 60, Colour = Holy,
            MapImpact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 670, FrameCount = 7, Colour = Holy },
            CastAtSource = true,
        },
        [MagicType.PoisonDust] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 60, FrameCount = 10, DelayMs = 60, Colour = Dark,
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 70, FrameCount = 10, Colour = Dark },
            CastAtSource = true,
        },
        [MagicType.AugmentPoisonDust] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 60, FrameCount = 10, DelayMs = 60, Colour = Dark,
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 70, FrameCount = 10, Colour = Dark },
            CastAtSource = true,
        },
        [MagicType.Purification] = new CastEffect
        {
            File = LibraryFile.MagicEx2, StartIndex = 220, FrameCount = 10, Colour = Holy,
            Impact = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 230, FrameCount = 10, Colour = Holy },
            CastAtSource = true,
        },
        [MagicType.SummonDemonicCreature] = new CastEffect { File = LibraryFile.Magic, StartIndex = 740, FrameCount = 10, DelayMs = 60, Colour = Phantom },
        [MagicType.SummonShinsu] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2590, FrameCount = 19, DelayMs = 60, Colour = Phantom },
        [MagicType.PoisonCloud] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5500, FrameCount = 56, Colour = Dark },

        // ---- 法师扩展：这些分支在旧端不是通用爆炸，而是明确的地图/目标特效 ----
        [MagicType.Repulsion] = new CastEffect { File = LibraryFile.Magic, StartIndex = 90, FrameCount = 10, Colour = Wind },
        [MagicType.Teleportation] = new CastEffect { File = LibraryFile.Magic, StartIndex = 110, FrameCount = 10, DelayMs = 60, Colour = Phantom, CastAtSource = true },
        [MagicType.AdamantineFireBall] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 420, FrameCount = 5, Colour = Fire,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1560, FrameCount = 9, DelayMs = 65, Colour = Fire },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 420, FrameCount = 5, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 580, FrameCount = 10, Colour = Fire },
        },
        // 原版 MapObject.cs:1209-1238 对每个 MagicLocation 创建 3 个 MirEffect:
        //   1) ProgUse 220, 1帧, 3500ms — 地面焦痕(Floor, Opacity 0.8)
        //   2) Magic 2450+rand*10, 10帧, 250ms — 地面火焰动画(Floor, Blend)  ← 旧实现漏了这个
        //   3) Magic 1900, 30帧, 50ms — 爆发火焰(Blend, BlendRate 1)
        // Godot 旧配置只有 1900(主) + 220(焦痕) + 1820(Source,原版无), 漏掉 2450 地面火焰,
        // 导致"只看到焦痕和爆发, 看不到那团持续燃烧的火"。补齐 2450。
        [MagicType.ScortchedEarth] = new CastEffect { File = LibraryFile.Magic, StartIndex = 1900, FrameCount = 30, DelayMs = 50, DistanceDelayMs = 50, Colour = Fire, DrawType = MirEffectNode.EffectLayer.Floor, BlendRate = 1f, DirectionFromCast = true, Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1820, FrameCount = 8, DelayMs = 60, Colour = Fire }, Additional = { new ImpactDef { File = LibraryFile.ProgUse, StartIndex = 220, FrameCount = 1, DelayMs = 3500, StartDelayMs = 500, DistanceDelayMs = 50, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor, Opacity = 0.8f }, new ImpactDef { File = LibraryFile.Magic, StartIndex = 2450, FrameCount = 10, DelayMs = 250, StartDelayMs = 500, DistanceDelayMs = 50, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor, BlendRate = 1f } } },
        // 原版: 起手 MirEffect(1970,10,30ms){Target=this}；光束 MirEffect
        // (1180,4,100ms,light 150){Target=this, Direction=施法者→格} 每个
        // MagicLocation 各播一次，目标上无特效。旧实现把光束挂到目标上。
        [MagicType.LightningBeam] = new CastEffect
        {
            File = LibraryFile.MagicEx, StartIndex = 1180, FrameCount = 4, Colour = Lightning,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1970, FrameCount = 10, DelayMs = 30, Colour = Lightning },
            SourcePerLocation = { new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1180, FrameCount = 4, Colour = Lightning, FrameLight = 150 } },
            NoTargetVisual = true,
        },
        [MagicType.FrozenEarth] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 90, FrameCount = 20, DelayMs = 50, Colour = Ice, BlendRate = 0.5f, DirectionFromCast = true, Source = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 0, FrameCount = 10, DelayMs = 50, Colour = Ice }, TargetEffect = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 0, FrameCount = 10, DelayMs = 50, Colour = Ice, DirectionFromCast = true }, Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 90, FrameCount = 20, DelayMs = 50, DistanceDelayMs = 50, Colour = Ice, FrameLight = 20, Opacity = 0.5f }, Additional = { new ImpactDef { File = LibraryFile.ProgUse, StartIndex = 260, FrameCount = 1, DelayMs = 2500, StartDelayMs = 1000, DistanceDelayMs = 50, Colour = Ice, FrameLight = 0, DrawType = MirEffectNode.EffectLayer.Floor, Opacity = 0.8f } } },
        [MagicType.BlowEarth] = new CastEffect
        {
            File = LibraryFile.MagicEx, StartIndex = 1990, FrameCount = 5, Colour = Wind,
            Source = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1970, FrameCount = 10, DelayMs = 60, Colour = Wind },
            TargetEffect = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1970, FrameCount = 10, DelayMs = 60, Colour = Wind },
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx, StartIndex = 1990, FrameCount = 5, Colour = Wind, Skip = 0, Explode = true },
            Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 2000, FrameCount = 8, Colour = Wind },
            MapImpact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 2000, FrameCount = 8, Colour = Wind },
        },
        [MagicType.ExpelUndead] = new CastEffect { File = LibraryFile.Magic, StartIndex = 130, FrameCount = 10, Colour = Phantom, CastAtSource = true, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 140, FrameCount = 10, Colour = Phantom } },
        [MagicType.FireWall] = new CastEffect { File = LibraryFile.Magic, StartIndex = 910, FrameCount = 10, DelayMs = 60, Colour = Fire, CastAtSource = true },
        [MagicType.GeoManipulation] = new CastEffect { File = LibraryFile.Magic, StartIndex = 110, FrameCount = 10, DelayMs = 60, Colour = Phantom, CastAtSource = true },
        // 原版: start Effect(1430,12,50ms,Target=this) + release Effect(980,8,100ms,MapTarget=point) 地面闪电.
        // 旧配置 Impact(目标) 被 CastAtSource=true 阻断, 地面闪电丢失. 改为 MapImpact.
        [MagicType.LightningWave] = new CastEffect { File = LibraryFile.Magic, StartIndex = 1430, FrameCount = 12, DelayMs = 50, Colour = Lightning, CastAtSource = true, MapImpact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 980, FrameCount = 8, Colour = Lightning } },
        // 原版: start Effect(770,10,60ms,Target=this) + release Effect(780,7,100ms,MapTarget=point) 地面冰暴.
        // 旧配置 Impact(目标) 被 CastAtSource=true 阻断, 地面冰暴丢失. 改为 MapImpact.
        [MagicType.IceStorm] = new CastEffect { File = LibraryFile.Magic, StartIndex = 770, FrameCount = 10, DelayMs = 60, Colour = Ice, CastAtSource = true, MapImpact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 780, FrameCount = 7, Colour = Ice } },
        // 原版: start Effect(1030,10,60ms,Target=this) + release Effect(1040,16,100ms,MapTarget=point) 地面龙卷.
        // 旧配置 Impact(目标) 被 CastAtSource=true 阻断, 地面龙卷丢失. 改为 MapImpact.
        [MagicType.DragonTornado] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 1030, FrameCount = 10, DelayMs = 60, Colour = Wind, CastAtSource = true, MapImpact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1040, FrameCount = 16, Colour = Wind } },
        [MagicType.GreaterFrozenEarth] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 90, FrameCount = 20, DelayMs = 50, Colour = Ice, BlendRate = 0.5f, DirectionFromCast = true, Source = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 0, FrameCount = 10, DelayMs = 50, Colour = Ice }, TargetEffect = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 0, FrameCount = 10, DelayMs = 50, Colour = Ice, DirectionFromCast = true }, Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 90, FrameCount = 20, DelayMs = 50, DistanceDelayMs = 50, Colour = Ice, FrameLight = 20, Opacity = 0.5f }, Additional = { new ImpactDef { File = LibraryFile.ProgUse, StartIndex = 260, FrameCount = 1, DelayMs = 2500, StartDelayMs = 1000, DistanceDelayMs = 50, Colour = None, FrameLight = 0, DrawType = MirEffectNode.EffectLayer.Floor, Opacity = 0.8f } } },
        [MagicType.ChainLightning] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 470, FrameCount = 10, Colour = Lightning, Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1430, FrameCount = 12, DelayMs = 50, Colour = Lightning } },
        // 原版只有地面落点弹道 (Origin=落点+(4,-10) 的直落陨石)，对
        // AttackTargets 不创建任何魔法特效；旧实现会在 targets 循环里从
        // 施法者再发一枚追踪弹。NoTargetVisual 关闭该回退。
        [MagicType.Asteroid] = new CastEffect
        {
            File = LibraryFile.MagicEx5, StartIndex = 1300, FrameCount = 10, Colour = Fire,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 1300, FrameCount = 10, Colour = Fire, Skip = 0, Explode = true, OriginOffsetX = 4, OriginOffsetY = -10, OriginFromTarget = true },
            Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 1320, FrameCount = 8, Colour = None, FrameLight = 100 },
            MapImpact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 1320, FrameCount = 8, Colour = None, FrameLight = 100 },
            NoTargetVisual = true,
        },
        [MagicType.LightningStrike] = new CastEffect
        {
            File = LibraryFile.MagicEx6, StartIndex = 500, FrameCount = 8, Colour = Lightning,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.MagicEx6, StartIndex = 400, FrameCount = 8, Colour = Lightning },
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx6, StartIndex = 500, FrameCount = 8, Colour = Lightning, Skip = 0 },
            Impact = new ImpactDef { File = LibraryFile.MagicEx6, StartIndex = 500, FrameCount = 8, Colour = Lightning },
        },
        [MagicType.IceRain] = new CastEffect
        {
            File = LibraryFile.MagicEx7, StartIndex = 700, FrameCount = 7, Colour = Ice,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1430, FrameCount = 12, DelayMs = 50, Colour = Ice },
            ProjectileDelayStepMs = 200,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx7, StartIndex = 700, FrameCount = 7, Colour = Ice, Skip = 0, Explode = true, OriginOffsetY = -10, OriginFromTarget = true },
            Impact = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 720, FrameCount = 7, Colour = Ice, FrameLight = 100 },
            MapImpact = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 720, FrameCount = 7, Colour = Ice, FrameLight = 100 },
        },
        [MagicType.IceAura] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 2500, FrameCount = 6, Colour = Ice, DirectionFromCast = true, Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2620, FrameCount = 6, DelayMs = 80, Colour = Ice }, Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 2500, FrameCount = 6, Colour = Ice, Has16Directions = false } },
        [MagicType.IceDragon] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 2800, FrameCount = 6, Colour = Ice, DirectionFromCast = true, Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2620, FrameCount = 6, DelayMs = 80, Colour = Ice }, Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 2800, FrameCount = 6, Colour = Ice, Has16Directions = false }, TargetProjectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 2800, FrameCount = 6, DelayMs = 150, Colour = Ice, Has16Directions = false }, AdditionalProjectiles = { new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 2900, FrameCount = 6, Colour = Ice, Has16Directions = false } }, TargetAdditionalProjectiles = { new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 2900, FrameCount = 6, DelayMs = 150, Colour = Ice, Has16Directions = false } }, Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 3000, FrameCount = 12, Colour = Ice } },
        [MagicType.IceBreaker] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5200, FrameCount = 37, Colour = Ice },
        [MagicType.FrozenDragon] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5300, FrameCount = 41, Colour = Ice },

        // ---- 道士扩展 ----
        [MagicType.ExplosiveTalisman] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = Dark,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = Dark },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = Dark },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1140, FrameCount = 10, Colour = Dark },
        },
        [MagicType.EvilSlayer] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 3330, FrameCount = 6, Colour = Holy,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 3250, FrameCount = 6, DelayMs = 80, Colour = Holy },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 3330, FrameCount = 6, Colour = Holy, Skip = 0 },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 3340, FrameCount = 10, Colour = Holy },
        },
        [MagicType.Invisibility] = new CastEffect { File = LibraryFile.Magic, StartIndex = 810, FrameCount = 10, DelayMs = 60, Colour = Phantom, CastAtSource = true },
        [MagicType.MagicResistance] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = None, CastAtSource = true, DirectionFromCast = true, Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = None, Explode = true }, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 200, FrameCount = 8, Colour = None } },
        [MagicType.BloodLust] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = Dark, CastAtSource = true, Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = Dark, Explode = true }, Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 140, FrameCount = 7, Colour = Dark } },
        [MagicType.GreaterEvilSlayer] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 3440, FrameCount = 6, DelayMs = 50, Colour = Holy,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 3360, FrameCount = 6, DelayMs = 80, Colour = Holy },
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 3440, FrameCount = 6, DelayMs = 50, Colour = Holy, Skip = 0 },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 3450, FrameCount = 10, Colour = Holy },
        },
        [MagicType.Resilience] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = None, CastAtSource = true, DirectionFromCast = true, Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = None, Explode = true }, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 170, FrameCount = 8, Colour = None } },
        [MagicType.ElementalSuperiority] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = None, CastAtSource = true, DirectionFromCast = true, Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = None, Explode = true }, Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1870, FrameCount = 10, Colour = None } },
        [MagicType.MassInvisibility] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = Phantom, CastAtSource = true, Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = Phantom, Explode = true }, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 820, FrameCount = 7, Colour = Phantom } },
        [MagicType.Resurrection] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 320, FrameCount = 7, Colour = Holy },
        [MagicType.StrengthOfFaith] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 370, FrameCount = 10, Colour = Phantom },
        [MagicType.CelestialLight] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 290, FrameCount = 9, Colour = Holy },
        [MagicType.LifeSteal] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 2500, FrameCount = 10, Colour = Dark, DirectionFromCast = true, Source = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 2410, FrameCount = 9, Colour = Dark } },
        [MagicType.ImprovedExplosiveTalisman] = new CastEffect
        {
            File = LibraryFile.MagicEx2, StartIndex = 980, FrameCount = 6, Colour = Dark,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 6, DelayMs = 80, Colour = Dark },
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx2, StartIndex = 980, FrameCount = 6, Colour = Dark, Has16Directions = false, Skip = 0 },
            Impact = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 1160, FrameCount = 10, Colour = Dark },
        },
        [MagicType.Parasite] = new CastEffect
        {
            File = LibraryFile.MagicEx5, StartIndex = 800, FrameCount = 6, Colour = None,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 1000, FrameCount = 5, Colour = None },
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 800, FrameCount = 6, Colour = None, Has16Directions = false },
            Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 1200, FrameCount = 10, Colour = None },
        },
        [MagicType.Neutralize] = new CastEffect
        {
            File = LibraryFile.MagicEx7, StartIndex = 300, FrameCount = 4, DelayMs = 80, Colour = Fire,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = Dark },
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx7, StartIndex = 300, FrameCount = 4, DelayMs = 80, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 460, FrameCount = 10, Colour = Fire },
        },
        [MagicType.DarkSoulPrison] = new CastEffect { File = LibraryFile.MagicEx6, StartIndex = 600, FrameCount = 9, Colour = Dark },
        [MagicType.SearingLight] = new CastEffect
        {
            File = LibraryFile.MagicEx3, StartIndex = 1210, FrameCount = 10, DelayMs = 70, Colour = Holy,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx3, StartIndex = 1210, FrameCount = 10, DelayMs = 70, Colour = Holy, Has16Directions = false },
            Impact = new ImpactDef { File = LibraryFile.MagicEx3, StartIndex = 1300, FrameCount = 10, Colour = Fire },
        },
        [MagicType.SoulResonance] = new CastEffect
        {
            File = LibraryFile.MagicEx7, StartIndex = 500, FrameCount = 8, Colour = None,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx7, StartIndex = 500, FrameCount = 8, Colour = None },
            Impact = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 670, FrameCount = 9, Colour = None },
        },
        [MagicType.BindingTalisman] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 3600, FrameCount = 1, Colour = None, DirectionFromCast = true, Source = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 3500, FrameCount = 4, Colour = None }, Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 3600, FrameCount = 1, Colour = None } },
        [MagicType.TrapOctagon] = new CastEffect { File = LibraryFile.Magic, StartIndex = 630, FrameCount = 10, DelayMs = 60, Colour = Dark, CastAtSource = true },
        [MagicType.BrainStorm] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 3200, FrameCount = 5, Colour = None, DirectionFromCast = true, Source = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 4600, FrameCount = 10, Colour = None }, Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 3200, FrameCount = 5, Colour = None, FrameLight = 15 }, Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 3400, FrameCount = 15, Colour = None } },
        [MagicType.HeavenlySky] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5400, FrameCount = 39, Colour = Lightning },
        [MagicType.WraithGrip] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 1420, FrameCount = 14, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor, BlendRate = 0.4f },
        [MagicType.HellFire] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 1500, FrameCount = 10, Colour = Fire, DrawType = MirEffectNode.EffectLayer.Floor },
        [MagicType.BurningFire] = new CastEffect { File = LibraryFile.MagicEx6, StartIndex = 900, FrameCount = 10, DelayMs = 60, Colour = Fire },
        [MagicType.MagicCombustion] = new CastEffect { File = LibraryFile.MagicEx7, StartIndex = 0, FrameCount = 6, Colour = None, Projectile = new ProjectileDef { File = LibraryFile.MagicEx7, StartIndex = 0, FrameCount = 6, Colour = None, FrameLight = 0 }, TargetProjectile = new ProjectileDef { File = LibraryFile.MagicEx7, StartIndex = 0, FrameCount = 6, Colour = None, Explode = true, FrameLight = 0 }, Impact = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 280, FrameCount = 10, Colour = None, FrameLight = 20 } },
        [MagicType.Chain] = new CastEffect { File = LibraryFile.MagicEx7, StartIndex = 20, FrameCount = 7, Colour = None },
        [MagicType.FourWheels] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5600, FrameCount = 35, Colour = Fire },
        [MagicType.CrescentMoon] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5700, FrameCount = 21, Colour = Phantom },

        // ---- 战士/法师/道士状态与近战技能 ----
        [MagicType.Swordsmanship] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 0, FrameCount = 9, Colour = None, CastAtSource = true },
        [MagicType.FlamingSword] = new CastEffect { File = LibraryFile.Magic, StartIndex = 1470, FrameCount = 6, Colour = Fire, CastAtSource = true },
        [MagicType.Interchange] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 0, FrameCount = 9, Colour = None, CastAtSource = true },
        [MagicType.Defiance] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 40, FrameCount = 10, Colour = None, CastAtSource = true },
        [MagicType.Invincibility] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 400, FrameCount = 10, Colour = None, CastAtSource = true },
        [MagicType.Beckon] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 580, FrameCount = 10, Colour = None, CastAtSource = true },
        [MagicType.Might] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 60, FrameCount = 10, Colour = None, CastAtSource = true },
        [MagicType.Endurance] = new CastEffect { File = LibraryFile.MagicEx3, StartIndex = 190, FrameCount = 10, Colour = None, CastAtSource = true },
        [MagicType.ReflectDamage] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 1220, FrameCount = 10, Colour = None, CastAtSource = true },
        [MagicType.Fetter] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 2370, FrameCount = 10, Colour = None, CastAtSource = true },
        [MagicType.MassBeckon] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 100, FrameCount = 10, Colour = None, CastAtSource = true },
        [MagicType.Renounce] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 80, FrameCount = 10, Colour = Phantom, CastAtSource = true },
        [MagicType.Tempest] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 910, FrameCount = 10, DelayMs = 60, Colour = Wind, CastAtSource = true },
        [MagicType.MirrorImage] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 1260, FrameCount = 6, Colour = None, CastAtSource = true },
        [MagicType.Tornado] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 2400, FrameCount = 4, Colour = Wind },
        [MagicType.Transparency] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 430, FrameCount = 7, Colour = Phantom, CastAtSource = true },
        [MagicType.CursedDoll] = new CastEffect { File = LibraryFile.MagicEx3, StartIndex = 690, FrameCount = 10, DelayMs = 60, Colour = Fire, CastAtSource = true },
        [MagicType.SummonSkeleton] = new CastEffect { File = LibraryFile.Magic, StartIndex = 750, FrameCount = 10, Colour = Phantom },
        [MagicType.SummonJinSkeleton] = new CastEffect { File = LibraryFile.Magic, StartIndex = 750, FrameCount = 10, Colour = Phantom },
        [MagicType.SummonDead] = new CastEffect { File = LibraryFile.Magic, StartIndex = 740, FrameCount = 10, DelayMs = 60, Colour = Phantom, CastAtSource = true },
        [MagicType.AugmentCelestialLight] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 300, FrameCount = 3, DelayMs = 200, Colour = Holy, CastAtSource = true },
        [MagicType.PoisonousCloud] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5500, FrameCount = 56, Colour = Dark },
        [MagicType.Cloak] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 600, FrameCount = 10, DelayMs = 60, Colour = Phantom },
        [MagicType.SummonPuppet] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 800, FrameCount = 16, Colour = Phantom, BlendRate = 0.8f },
        [MagicType.TheNewBeginning] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 2200, FrameCount = 8, Colour = None },
        [MagicType.DragonRepulse] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 1020, FrameCount = 10, DelayMs = 60, Colour = Lightning, Source = new ImpactDef { File = LibraryFile.MagicEx4, StartIndex = 1000, FrameCount = 10, DelayMs = 60, Colour = None }, SourceAdditional = { new ImpactDef { File = LibraryFile.MagicEx4, StartIndex = 1020, FrameCount = 10, DelayMs = 60, Colour = Lightning } } },
        [MagicType.Abyss] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 2000, FrameCount = 14, DelayMs = 70, Colour = Phantom },
        [MagicType.FlashOfLight] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 2300, FrameCount = 8, DelayMs = 60, Colour = None, DirectionFromCast = true },
        [MagicType.Evasion] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 2500, FrameCount = 12, DelayMs = 70, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor },
        [MagicType.RagingWind] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 2600, FrameCount = 12, DelayMs = 70, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor },
        [MagicType.Concentration] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 300, FrameCount = 15, Colour = None, CastAtSource = true },
        [MagicType.Containment] = new CastEffect { File = LibraryFile.MagicEx3, StartIndex = 590, FrameCount = 9, DelayMs = 60, Colour = None, CastAtSource = true },
        [MagicType.Assault] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 740, FrameCount = 3, Colour = None, CastAtSource = true },
        [MagicType.HundredFist] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 2100, FrameCount = 5, DelayMs = 200, Colour = Fire, CastAtSource = true },
        [MagicType.ThunderKick] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 1190, FrameCount = 10, Colour = None },
        [MagicType.CorpseExploder] = new CastEffect
        {
            File = LibraryFile.MagicEx7, StartIndex = 300, FrameCount = 4, Colour = Fire,
            DirectionFromCast = true,
            Source = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = Dark },
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx7, StartIndex = 300, FrameCount = 4, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 1000, FrameCount = 17, Colour = Fire },
        },
        [MagicType.Hemorrhage] = new CastEffect
        {
            File = LibraryFile.MagicEx7, StartIndex = 1100, FrameCount = 6, Colour = Fire,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx7, StartIndex = 1100, FrameCount = 6, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 1270, FrameCount = 10, Colour = Fire },
        },
        [MagicType.FlamingDaggers] = new CastEffect
        {
            File = LibraryFile.MagicEx5, StartIndex = 3900, FrameCount = 7, Colour = Fire,
            Source = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 3800, FrameCount = 10, Colour = Fire },
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 3900, FrameCount = 7, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 4100, FrameCount = 8, Colour = Fire },
        },
        [MagicType.Shredding] = new CastEffect
        {
            File = LibraryFile.MagicEx5, StartIndex = 4300, FrameCount = 5, Colour = Fire,
            Source = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 4200, FrameCount = 10, Colour = Fire },
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 4300, FrameCount = 5, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 4500, FrameCount = 10, Colour = Fire },
        },
        [MagicType.PinkFireBall] = new CastEffect
        {
            File = LibraryFile.MonMagicEx20, StartIndex = 1500, FrameCount = 6, Colour = Phantom,
            Projectile = new ProjectileDef { File = LibraryFile.MonMagicEx20, StartIndex = 1500, FrameCount = 6, Colour = Phantom },
            TargetProjectile = new ProjectileDef { File = LibraryFile.MonMagicEx20, StartIndex = 1600, FrameCount = 6, Colour = Phantom, Has16Directions = false },
            Impact = new ImpactDef { File = LibraryFile.MonMagicEx20, StartIndex = 1700, FrameCount = 10, Colour = Phantom },
        },
        [MagicType.GreenSludgeBall] = new CastEffect
        {
            File = LibraryFile.MonMagicEx23, StartIndex = 2600, FrameCount = 7, Colour = new Color(0.6f, 1f, 0.1f),
            Projectile = new ProjectileDef { File = LibraryFile.MonMagicEx23, StartIndex = 2600, FrameCount = 7, Colour = new Color(0.6f, 1f, 0.1f), Has16Directions = true },
            TargetProjectile = new ProjectileDef { File = LibraryFile.MonMagicEx23, StartIndex = 2600, FrameCount = 7, Colour = new Color(0.6f, 1f, 0.1f), Has16Directions = false },
            // The original MapObject assigns action.Direction to this impact.
            // The checked-in MonMagicEx23 resource currently lacks the later
            // direction ranges; the frame audit records that source/resource
            // inconsistency instead of silently forcing Up.
            Impact = new ImpactDef { File = LibraryFile.MonMagicEx23, StartIndex = 2780, FrameCount = 6, Colour = new Color(0.6f, 1f, 0.1f), DirectionFromCast = true },
        },
        // ---- 怪物魔法：旧端同样使用 MapTarget，不能回退到玩家技能的素材 ----
        [MagicType.MonsterScortchedEarth] = new CastEffect { File = LibraryFile.Magic, StartIndex = 1930, FrameCount = 30, DelayMs = 50, DistanceDelayMs = 50, Colour = Fire, DrawType = MirEffectNode.EffectLayer.Floor, BlendRate = 1f, Additional = { new ImpactDef { File = LibraryFile.ProgUse, StartIndex = 220, FrameCount = 1, DelayMs = 3000, StartDelayMs = 500, DistanceDelayMs = 50, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor, Opacity = 0.8f }, new ImpactDef { File = LibraryFile.Magic, StartIndex = 2450, FrameCount = 10, DelayMs = 250, StartDelayMs = 500, DistanceDelayMs = 50, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor } } },
        [MagicType.MonsterIceStorm] = new CastEffect { File = LibraryFile.MonMagicEx3, StartIndex = 6230, FrameCount = 10, Colour = Ice, BlendRate = 1f },
        [MagicType.MonsterDeathCloud] = new CastEffect { File = LibraryFile.MonMagicEx2, StartIndex = 850, FrameCount = 10, Colour = Dark, BlendRate = 1f },
        [MagicType.MonsterThunderStorm] = new CastEffect { File = LibraryFile.MonMagicEx5, StartIndex = 650, FrameCount = 6, Colour = Lightning, BlendRate = 1f },
        [MagicType.SamaGuardianFire] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 4000, FrameCount = 10, Colour = Fire },
        [MagicType.SamaGuardianIce] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 4100, FrameCount = 10, Colour = Ice },
        [MagicType.SamaGuardianLightning] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 4200, FrameCount = 10, Colour = Lightning },
        [MagicType.SamaGuardianWind] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 4300, FrameCount = 10, Colour = Wind },
        [MagicType.SamaPhoenixFire] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 4500, FrameCount = 10, Colour = Fire },
        [MagicType.SamaBlackIce] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 4600, FrameCount = 10, Colour = Ice },
        [MagicType.SamaBlueLightning] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 4700, FrameCount = 10, Colour = Lightning },
        [MagicType.SamaWhiteWind] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 4800, FrameCount = 10, Colour = Wind },
        [MagicType.SamaProphetFire] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 5600, FrameCount = 10, Colour = Fire },
        [MagicType.SamaProphetLightning] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 5200, FrameCount = 10, Colour = Lightning },
        [MagicType.SamaProphetWind] = new CastEffect { File = LibraryFile.MonMagicEx9, StartIndex = 5400, FrameCount = 10, Colour = Wind },
        [MagicType.DoomClawLeftPinch] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2660, FrameCount = 7, Colour = None, AdditionalMapEffects = { new OffsetImpactDef { File = LibraryFile.MonMagicEx19, StartIndex = 2680, FrameCount = 9, StartDelayMs = 700, Colour = None, OffsetX = 5 } } },
        [MagicType.DoomClawLeftSwipe] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2720, FrameCount = 8, Colour = None },
        [MagicType.DoomClawRightPinch] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2640, FrameCount = 7, Colour = None, AdditionalMapEffects = { new OffsetImpactDef { File = LibraryFile.MonMagicEx19, StartIndex = 2680, FrameCount = 9, StartDelayMs = 700, Colour = None, OffsetX = 5 } } },
        [MagicType.DoomClawRightSwipe] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2700, FrameCount = 8, Colour = None },
        [MagicType.DoomClawSpit] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2500, FrameCount = 7, Colour = None, Projectile = new ProjectileDef { File = LibraryFile.MonMagicEx19, StartIndex = 2500, FrameCount = 7, Colour = None, Skip = 0, Explode = true, FrameLight = 0, OriginOffsetY = -10, OriginFromTarget = true }, Impact = new ImpactDef { File = LibraryFile.MonMagicEx19, StartIndex = 2520, FrameCount = 8, Colour = None, FrameLight = 0 }, MapImpact = new ImpactDef { File = LibraryFile.MonMagicEx19, StartIndex = 2520, FrameCount = 8, Colour = None, FrameLight = 0 } },

        // ---- 刺客 ----
        [MagicType.FlameSplash] = new CastEffect { File = LibraryFile.Magic, StartIndex = 580, FrameCount = 10, Colour = Fire },
    };
}
