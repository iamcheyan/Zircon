using System.Collections.Generic;
using Godot;
using Library;

namespace ZirconClient.Scripts;

/// <summary>
/// 魔法特效定义表 (提取自原版 Client/Models/MapObject.cs:768 的 case MirAction.Spell)。
/// 每个魔法: 施法站桩特效 + 可选飞行弹道 + 可选落地/命中特效。
/// 颜色用原版 Globals.*Colour 的近似值。
/// 不覆盖全部 174 个, 先做常见 ~30 个; 查不到的回落到通用爆炸特效。
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
        // 旧端 Spell 分支中挂在施法者/自身对象上的效果（如 HealStart）。
        public bool CastAtSource;
        // 飞行弹道: 非空表示从施法者飞到目标
        public ProjectileDef Projectile;
        // 落地/命中特效 (目标位置)
        public ImpactDef Impact;
    }

    public class ProjectileDef
    {
        public LibraryFile File;
        public int StartIndex;
        public int FrameCount;
        public int DelayMs = 100;
        public Color Colour = Colors.White;
    }

    public class ImpactDef
    {
        public LibraryFile File;
        public int StartIndex;
        public int FrameCount;
        public int DelayMs = 100;
        public Color Colour = Colors.White;
    }

    /// <summary>按 MagicType 查施法特效。null=用通用爆炸兜底。</summary>
    public static CastEffect Get(MagicType type)
    {
        if (_table.TryGetValue(type, out var def)) return def;
        return null;  // 兜底由调用方处理
    }

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
        [MagicType.Cyclone] = new CastEffect { File = LibraryFile.MagicEx, StartIndex = 1990, FrameCount = 5, Colour = Wind },
        [MagicType.FireStorm] = new CastEffect { File = LibraryFile.Magic, StartIndex = 950, FrameCount = 7, Colour = Fire },
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
        [MagicType.MassInvisibility] = new CastEffect { File = LibraryFile.Magic, StartIndex = 2080, FrameCount = 6, DelayMs = 80, Colour = Phantom },
        [MagicType.PoisonCloud] = new CastEffect { File = LibraryFile.MagicEx5, StartIndex = 5500, FrameCount = 56, Colour = Dark },

        // ---- 刺客 ----
        [MagicType.FlameSplash] = new CastEffect { File = LibraryFile.Magic, StartIndex = 580, FrameCount = 10, Colour = Fire },
    };
}
