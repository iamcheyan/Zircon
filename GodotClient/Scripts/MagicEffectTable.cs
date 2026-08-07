using System.Collections.Generic;
using Godot;
using Library;

namespace ZirconClient.Scripts;

/// <summary>
/// 魔法特效定义表 (提取自原版 Client/Models/MapObject.cs:768 的 case MirAction.Spell)。
/// 每个魔法: 施法站桩特效 + 可选飞行弹道 + 可选落地/命中特效。
/// 颜色用原版 Globals.*Colour 的近似值。
/// 当前表按原版的施法轨迹维护：投射物、命中特效、地面/范围特效和自身特效分别配置。
/// 未覆盖的技能不再伪造通用爆炸，由 GameScene 记录诊断，便于继续补齐原版资源。
/// </summary>
public static class MagicEffectTable
{
    /// <summary>元素颜色 (近似原版 Globals.*Colour)。</summary>
    public static readonly Color Fire = new(1.0f, 0.27f, 0.0f);       // OrangeRed
    public static readonly Color Ice = new(0.69f, 0.95f, 0.93f);      // PaleTurquoise
    public static readonly Color Lightning = new(0.53f, 0.81f, 0.98f);// LightSkyBlue
    public static readonly Color Wind = new(0.13f, 0.7f, 0.67f);      // LightSeaGreen
    public static readonly Color Holy = new(0.76f, 0.65f, 0.37f);     // DarkKhaki
    public static readonly Color Dark = new(0.43f, 0.25f, 0.09f);     // SaddleBrown
    public static readonly Color Phantom = new(0.63f, 0.13f, 0.94f); // Purple
    public static readonly Color None = Colors.White;

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
        public double StartDelayMs;
        public int DistanceDelayMs;
        // 旧端 Spell 分支中挂在施法者/自身对象上的效果（如 HealStart）。
        public bool CastAtSource;
        // 飞行弹道: 非空表示从施法者飞到目标
        public ProjectileDef Projectile;
        // 落地/命中特效 (目标位置)
        public ImpactDef Impact;
        public List<ImpactDef> Additional = new();
        public List<OffsetImpactDef> AdditionalMapEffects = new();
        public List<ProjectileDef> AdditionalProjectiles = new();
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
        public double StartDelayMs;
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
        public double StartDelayMs;
        public int DistanceDelayMs;
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
        [MagicType.DefensiveBlow] = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 800, FrameCount = 9, Colour = Fire, DrawType = MirEffectNode.EffectLayer.Floor },
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
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 420, FrameCount = 5, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 580, FrameCount = 10, Colour = Fire },
        },
        [MagicType.LightningBall] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 3070, FrameCount = 6, Colour = Lightning,
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 3070, FrameCount = 6, Colour = Lightning },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 3230, FrameCount = 10, Colour = Lightning },
        },
        [MagicType.IceBolt] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 2700, FrameCount = 3, Colour = Ice,
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 2700, FrameCount = 3, Colour = Ice },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2860, FrameCount = 10, Colour = Ice },
        },
        [MagicType.GustBlast] = new CastEffect
        {
            File = LibraryFile.MagicEx, StartIndex = 430, FrameCount = 5, Colour = Wind,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx, StartIndex = 430, FrameCount = 5, Colour = Wind },
            Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 590, FrameCount = 10, Colour = Wind },
        },
        [MagicType.ElectricShock] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 10, FrameCount = 10, Colour = Lightning,
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 10, FrameCount = 10, Colour = Lightning },
        },
        [MagicType.ThunderBolt] = new CastEffect { File = LibraryFile.Magic, StartIndex = 1450, FrameCount = 3, DelayMs = 150, Colour = Lightning },
        [MagicType.ThunderStrike] = new CastEffect { File = LibraryFile.Magic, StartIndex = 1450, FrameCount = 3, DelayMs = 150, Colour = Lightning },
        [MagicType.FireBounce] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 1640, FrameCount = 6, Colour = Fire,
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 1640, FrameCount = 6, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1800, FrameCount = 10, Colour = Fire },
        },
        [MagicType.IceBlades] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 2960, FrameCount = 6, DelayMs = 50, Colour = Ice,
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 2960, FrameCount = 6, DelayMs = 50, Colour = Ice },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 2970, FrameCount = 10, Colour = Ice },
        },
        [MagicType.Cyclone] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 1990, FrameCount = 5, Colour = Wind, Additional = { new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 2000, FrameCount = 8, Colour = Wind } } },
        [MagicType.FireStorm] = new CastEffect { File = LibraryFile.Magic, StartIndex = 940, FrameCount = 10, DelayMs = 60, Colour = Fire, CastAtSource = true, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 950, FrameCount = 7, Colour = Fire } },
        [MagicType.MagicShield] = new CastEffect { File = LibraryFile.Magic, StartIndex = 830, FrameCount = 19, DelayMs = 60, Colour = Phantom, CastAtSource = true },
        [MagicType.SuperiorMagicShield] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 1900, FrameCount = 17, DelayMs = 60, Colour = Fire, CastAtSource = true },

        // ---- 道士 ----
        [MagicType.Heal] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 660, FrameCount = 10, DelayMs = 60, Colour = Holy,
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 610, FrameCount = 10, Colour = Holy },
            CastAtSource = true,
        },
        [MagicType.MassHeal] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 660, FrameCount = 10, DelayMs = 60, Colour = Holy,
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 670, FrameCount = 7, Colour = Holy },
            CastAtSource = true,
        },
        [MagicType.PoisonDust] = new CastEffect
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
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 420, FrameCount = 5, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 580, FrameCount = 10, Colour = Fire },
        },
        [MagicType.ScortchedEarth] = new CastEffect { File = LibraryFile.Magic, StartIndex = 1900, FrameCount = 30, DelayMs = 50, DistanceDelayMs = 50, Colour = Fire, DrawType = MirEffectNode.EffectLayer.Floor, BlendRate = 1f, Additional = { new ImpactDef { File = LibraryFile.ProgUse, StartIndex = 220, FrameCount = 1, DelayMs = 3500, StartDelayMs = 500, DistanceDelayMs = 50, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor, Opacity = 0.8f } } },
        [MagicType.LightningBeam] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 1180, FrameCount = 4, Colour = Lightning },
        [MagicType.FrozenEarth] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 90, FrameCount = 20, DelayMs = 50, Colour = Ice, BlendRate = 0.5f, Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 90, FrameCount = 20, DelayMs = 50, DistanceDelayMs = 50, Colour = Ice, Opacity = 0.5f }, Additional = { new ImpactDef { File = LibraryFile.ProgUse, StartIndex = 260, FrameCount = 1, DelayMs = 2500, StartDelayMs = 1000, DistanceDelayMs = 50, Colour = Ice, DrawType = MirEffectNode.EffectLayer.Floor, Opacity = 0.8f } } },
        [MagicType.BlowEarth] = new CastEffect
        {
            File = LibraryFile.MagicEx, StartIndex = 1990, FrameCount = 5, Colour = Wind,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx, StartIndex = 1990, FrameCount = 5, Colour = Wind, Skip = 0, Explode = true },
            Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 2000, FrameCount = 8, Colour = Wind },
        },
        [MagicType.ExpelUndead] = new CastEffect { File = LibraryFile.Magic, StartIndex = 130, FrameCount = 10, Colour = Phantom, CastAtSource = true, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 140, FrameCount = 10, Colour = Phantom } },
        [MagicType.FireWall] = new CastEffect { File = LibraryFile.Magic, StartIndex = 910, FrameCount = 10, DelayMs = 60, Colour = Fire, CastAtSource = true },
        [MagicType.GeoManipulation] = new CastEffect { File = LibraryFile.Magic, StartIndex = 110, FrameCount = 10, DelayMs = 60, Colour = Phantom, CastAtSource = true },
        [MagicType.LightningWave] = new CastEffect { File = LibraryFile.Magic, StartIndex = 1430, FrameCount = 12, DelayMs = 50, Colour = Lightning, CastAtSource = true, Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 980, FrameCount = 8, Colour = Lightning } },
        [MagicType.IceStorm] = new CastEffect { File = LibraryFile.Magic, StartIndex = 770, FrameCount = 10, DelayMs = 60, Colour = Ice, CastAtSource = true, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 780, FrameCount = 7, Colour = Ice } },
        [MagicType.DragonTornado] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 1030, FrameCount = 10, DelayMs = 60, Colour = Wind, CastAtSource = true, Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 1040, FrameCount = 16, Colour = Wind } },
        [MagicType.GreaterFrozenEarth] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 90, FrameCount = 20, DelayMs = 50, Colour = Ice, BlendRate = 0.5f, Impact = new ImpactDef { File = LibraryFile.MagicEx, StartIndex = 90, FrameCount = 20, DelayMs = 50, DistanceDelayMs = 50, Colour = Ice, Opacity = 0.5f }, Additional = { new ImpactDef { File = LibraryFile.ProgUse, StartIndex = 260, FrameCount = 1, DelayMs = 2500, StartDelayMs = 1000, DistanceDelayMs = 50, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor, Opacity = 0.8f } } },
        [MagicType.ChainLightning] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 470, FrameCount = 10, Colour = Lightning },
        [MagicType.Asteroid] = new CastEffect
        {
            File = LibraryFile.MagicEx5, StartIndex = 1300, FrameCount = 10, Colour = Fire,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 1300, FrameCount = 10, Colour = Fire, Skip = 0, Explode = true },
            Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 1320, FrameCount = 8, Colour = None },
        },
        [MagicType.LightningStrike] = new CastEffect
        {
            File = LibraryFile.MagicEx6, StartIndex = 500, FrameCount = 8, Colour = Lightning,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx6, StartIndex = 500, FrameCount = 8, Colour = Lightning, Skip = 0 },
            Impact = new ImpactDef { File = LibraryFile.MagicEx6, StartIndex = 500, FrameCount = 8, Colour = Lightning },
        },
        [MagicType.IceRain] = new CastEffect
        {
            File = LibraryFile.MagicEx7, StartIndex = 700, FrameCount = 7, Colour = Ice,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx7, StartIndex = 700, FrameCount = 7, Colour = Ice, Skip = 0, Explode = true },
            Impact = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 720, FrameCount = 7, Colour = Ice },
        },
        [MagicType.IceAura] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 2500, FrameCount = 6, Colour = Ice, Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 2500, FrameCount = 6, Colour = Ice, Has16Directions = false } },
        [MagicType.IceDragon] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 2800, FrameCount = 6, Colour = Ice, Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 2800, FrameCount = 6, Colour = Ice, Has16Directions = false }, AdditionalProjectiles = { new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 2900, FrameCount = 6, Colour = Ice, Has16Directions = false } }, Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 3000, FrameCount = 12, Colour = Ice } },
        [MagicType.IceBreaker] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5200, FrameCount = 37, Colour = Ice },
        [MagicType.FrozenDragon] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5300, FrameCount = 41, Colour = Ice },

        // ---- 道士扩展 ----
        [MagicType.ExplosiveTalisman] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = Dark,
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = Dark },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 1140, FrameCount = 10, Colour = Dark },
        },
        [MagicType.EvilSlayer] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 3330, FrameCount = 6, Colour = Holy,
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 3330, FrameCount = 6, Colour = Holy, Skip = 0 },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 3340, FrameCount = 10, Colour = Holy },
        },
        [MagicType.Invisibility] = new CastEffect { File = LibraryFile.Magic, StartIndex = 810, FrameCount = 10, DelayMs = 60, Colour = Phantom, CastAtSource = true },
        [MagicType.MagicResistance] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = None, CastAtSource = true, Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = None, Explode = true }, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 200, FrameCount = 8, Colour = None } },
        [MagicType.GreaterEvilSlayer] = new CastEffect
        {
            File = LibraryFile.Magic, StartIndex = 3440, FrameCount = 6, DelayMs = 50, Colour = Holy,
            Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 3440, FrameCount = 6, DelayMs = 50, Colour = Holy, Skip = 0 },
            Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 3450, FrameCount = 10, Colour = Holy },
        },
        [MagicType.Resilience] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = None, CastAtSource = true, Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = None, Explode = true }, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 170, FrameCount = 8, Colour = None } },
        [MagicType.MassInvisibility] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = Phantom, CastAtSource = true, Projectile = new ProjectileDef { File = LibraryFile.Magic, StartIndex = 980, FrameCount = 3, Colour = Phantom, Explode = true }, Impact = new ImpactDef { File = LibraryFile.Magic, StartIndex = 820, FrameCount = 7, Colour = Phantom } },
        [MagicType.Resurrection] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 320, FrameCount = 7, Colour = Holy },
        [MagicType.StrengthOfFaith] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 370, FrameCount = 10, Colour = Phantom },
        [MagicType.CelestialLight] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 290, FrameCount = 9, Colour = Holy },
        [MagicType.LifeSteal] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 2500, FrameCount = 10, Colour = Dark },
        [MagicType.ImprovedExplosiveTalisman] = new CastEffect
        {
            File = LibraryFile.MagicEx2, StartIndex = 980, FrameCount = 6, Colour = Dark,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx2, StartIndex = 980, FrameCount = 6, Colour = Dark, Has16Directions = false, Skip = 0 },
            Impact = new ImpactDef { File = LibraryFile.MagicEx2, StartIndex = 1160, FrameCount = 10, Colour = Dark },
        },
        [MagicType.Parasite] = new CastEffect
        {
            File = LibraryFile.MagicEx5, StartIndex = 800, FrameCount = 6, Colour = None,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 800, FrameCount = 6, Colour = None, Has16Directions = false },
            Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 1200, FrameCount = 10, Colour = None },
        },
        [MagicType.Neutralize] = new CastEffect
        {
            File = LibraryFile.MagicEx7, StartIndex = 300, FrameCount = 4, DelayMs = 80, Colour = Fire,
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
        [MagicType.BindingTalisman] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 3600, FrameCount = 1, Colour = None, Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 3600, FrameCount = 1, Colour = None } },
        [MagicType.BrainStorm] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 3200, FrameCount = 5, Colour = None, Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 3200, FrameCount = 5, Colour = None }, Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 3400, FrameCount = 15, Colour = None } },
        [MagicType.HeavenlySky] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5400, FrameCount = 39, Colour = Lightning },
        [MagicType.WraithGrip] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 1420, FrameCount = 14, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor, BlendRate = 0.4f },
        [MagicType.HellFire] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 1500, FrameCount = 10, Colour = Fire, DrawType = MirEffectNode.EffectLayer.Floor },
        [MagicType.BurningFire] = new CastEffect { File = LibraryFile.MagicEx6, StartIndex = 900, FrameCount = 10, DelayMs = 60, Colour = Fire },
        [MagicType.MagicCombustion] = new CastEffect { File = LibraryFile.MagicEx7, StartIndex = 100, FrameCount = 6, Colour = None, Projectile = new ProjectileDef { File = LibraryFile.MagicEx7, StartIndex = 100, FrameCount = 6, Colour = None, Explode = true }, Impact = new ImpactDef { File = LibraryFile.MagicEx7, StartIndex = 280, FrameCount = 10, Colour = None } },
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
        [MagicType.DragonRepulse] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 1020, FrameCount = 10, DelayMs = 60, Colour = Lightning },
        [MagicType.Abyss] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 2000, FrameCount = 14, DelayMs = 70, Colour = Phantom },
        [MagicType.FlashOfLight] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 2300, FrameCount = 8, DelayMs = 60, Colour = None },
        [MagicType.Evasion] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 2500, FrameCount = 12, DelayMs = 70, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor },
        [MagicType.RagingWind] = new CastEffect { File = LibraryFile.MagicEx4, StartIndex = 2600, FrameCount = 12, DelayMs = 70, Colour = None, DrawType = MirEffectNode.EffectLayer.Floor },
        [MagicType.Concentration] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 300, FrameCount = 15, Colour = None, CastAtSource = true },
        [MagicType.Containment] = new CastEffect { File = LibraryFile.MagicEx3, StartIndex = 590, FrameCount = 9, DelayMs = 60, Colour = None, CastAtSource = true },
        [MagicType.Assault] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 740, FrameCount = 3, Colour = None, CastAtSource = true },
        [MagicType.ElementalSwords] = new CastEffect { File = LibraryFile.MagicEx10, StartIndex = 300, FrameCount = 5, Colour = None },
        [MagicType.HundredFist] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 2100, FrameCount = 5, DelayMs = 200, Colour = Fire, CastAtSource = true },
        [MagicType.ThunderKick] = new CastEffect { File = LibraryFile.MagicEx2, StartIndex = 1190, FrameCount = 10, Colour = None },
        [MagicType.CorpseExploder] = new CastEffect
        {
            File = LibraryFile.MagicEx7, StartIndex = 300, FrameCount = 4, Colour = Fire,
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
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 3900, FrameCount = 7, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 4100, FrameCount = 8, Colour = Fire },
        },
        [MagicType.Shredding] = new CastEffect
        {
            File = LibraryFile.MagicEx5, StartIndex = 4300, FrameCount = 5, Colour = Fire,
            Projectile = new ProjectileDef { File = LibraryFile.MagicEx5, StartIndex = 4300, FrameCount = 5, Colour = Fire },
            Impact = new ImpactDef { File = LibraryFile.MagicEx5, StartIndex = 4500, FrameCount = 10, Colour = Fire },
        },
        [MagicType.PinkFireBall] = new CastEffect
        {
            File = LibraryFile.MonMagicEx20, StartIndex = 1500, FrameCount = 6, Colour = Phantom,
            Projectile = new ProjectileDef { File = LibraryFile.MonMagicEx20, StartIndex = 1500, FrameCount = 6, Colour = Phantom },
            Impact = new ImpactDef { File = LibraryFile.MonMagicEx20, StartIndex = 1700, FrameCount = 10, Colour = Phantom },
        },
        [MagicType.GreenSludgeBall] = new CastEffect
        {
            File = LibraryFile.MonMagicEx23, StartIndex = 2600, FrameCount = 7, Colour = new Color(0.6f, 1f, 0.1f),
            Projectile = new ProjectileDef { File = LibraryFile.MonMagicEx23, StartIndex = 2600, FrameCount = 7, Colour = new Color(0.6f, 1f, 0.1f), Has16Directions = false },
            Impact = new ImpactDef { File = LibraryFile.MonMagicEx23, StartIndex = 2780, FrameCount = 6, Colour = new Color(0.6f, 1f, 0.1f) },
        },
        // ---- 怪物魔法：旧端同样使用 MapTarget，不能回退到玩家技能的素材 ----
        [MagicType.MonsterScortchedEarth] = new CastEffect { File = LibraryFile.Magic, StartIndex = 1930, FrameCount = 30, DelayMs = 50, Colour = Fire, DrawType = MirEffectNode.EffectLayer.Floor, BlendRate = 1f },
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
        [MagicType.DoomClawLeftPinch] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2660, FrameCount = 7, Colour = None, AdditionalMapEffects = { new OffsetImpactDef { File = LibraryFile.MonMagicEx19, StartIndex = 2680, FrameCount = 9, Colour = None, OffsetX = 5 } } },
        [MagicType.DoomClawLeftSwipe] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2720, FrameCount = 8, Colour = None },
        [MagicType.DoomClawRightPinch] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2640, FrameCount = 7, Colour = None, AdditionalMapEffects = { new OffsetImpactDef { File = LibraryFile.MonMagicEx19, StartIndex = 2680, FrameCount = 9, Colour = None, OffsetX = 5 } } },
        [MagicType.DoomClawRightSwipe] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2700, FrameCount = 8, Colour = None },
        [MagicType.DoomClawSpit] = new CastEffect { File = LibraryFile.MonMagicEx19, StartIndex = 2500, FrameCount = 7, Colour = None, Projectile = new ProjectileDef { File = LibraryFile.MonMagicEx19, StartIndex = 2500, FrameCount = 7, Colour = None, Skip = 0, Explode = true }, Impact = new ImpactDef { File = LibraryFile.MonMagicEx19, StartIndex = 2520, FrameCount = 8, Colour = None } },

        // ---- 刺客 ----
        [MagicType.FlameSplash] = new CastEffect { File = LibraryFile.Magic, StartIndex = 580, FrameCount = 10, Colour = Fire },
    };
}
