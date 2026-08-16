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
    private static readonly HashSet<MagicType> OriginalSpellCases = new();
    // E5/B4 cutover: 数据本体已迁 zircon/ClientData/magic-effects.json (DataLayer 装载)

    public static bool IsOriginalSpellCase(MagicType type) => OriginalSpellCases.Contains(type);

    // 旧端对应分支只播放音效，不创建 MirEffect；它们不是视觉迁移遗漏。
    private static readonly HashSet<MagicType> NoVisualSpellCases = new();

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
        // true=跳过黑色透明键(用 GetImageTexture)。暗色火焰帧经 Dxt1 压缩后主体 RGB≤32 会被误删。
        public bool NoColourKey;
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
        // true=跳过黑色透明键抠除(用 GetImageTexture, 仅靠 Dxt1 alpha 透明)。
        // 暗色火焰/冰系帧经 Dxt1 压缩后主体 RGB≤32 会被透明键误删, 需设 true。
        public bool NoColourKey;

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

    private static readonly Dictionary<MagicType, ImpactDef> _attackTable = new();

    private static readonly Dictionary<MagicType, CastEffect> _table = new();
}
