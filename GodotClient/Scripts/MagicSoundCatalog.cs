using System;
using System.Collections.Generic;
using Library;

namespace ZirconClient.Scripts;

public enum MagicSoundPhase { Start, Travel, End, Duration }

/// <summary>音效门控: 原版 release switch 尾部检查 MagicLocations/AttackTargets 计数的直接移植。</summary>
public enum MagicSoundGate { Always, Locations, Targets, LocationsOrTargets }

/// <summary>一条音效规格: 音效 + 播放门控。</summary>
public readonly record struct SoundSpec(SoundIndex Sound, MagicSoundGate Gate);

/// <summary>
/// 原版音效表 (MapObject.SetFrame start switch + release switch) 的移植。
///
/// 阶段语义 (FINAL):
/// - Start:   施法抬手时无条件播放。原版 SetFrame 的 MagicType start switch 在
///            !MagicCast 时也执行 (仅 release 有 MagicCast 门控), 故 Start 不依赖施法。
/// - End:     施法动画结束释放时播放, 按门控 (原版 release switch 尾部的
///            if (MagicLocations.Count > 0 || AttackTargets.Count > 0) Play(...) 等)。
/// - Duration:施法持续音 (OnObjectSpell 时长表), 未变。
/// - Travel:  已废弃 (OnObjectProjectile 改为原版 4-case 字面量; 释放时音效全在 End)。
///
/// 逐目标/逐落点着弹音效 (原版 CompleteAction 内的 Play) 走 def 的
/// CompletionSound / ArrivalSound, 不进本表。
/// </summary>
public static class MagicSoundCatalog
{
    private static readonly Dictionary<(MagicType, MagicSoundPhase), SoundSpec[]> Explicit = new()
    {
        // ================= Start (施法抬手, 原版 start switch 无条件播放) =================
        [(MagicType.HundredFist, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.HundredFist, MagicSoundGate.Always) },
        [(MagicType.HalfMoon, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.HalfMoon, MagicSoundGate.Always) },
        [(MagicType.DestructiveSurge, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DestructiveSurge, MagicSoundGate.Always) },
        [(MagicType.FlamingSword, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.FlamingSword, MagicSoundGate.Always) },
        [(MagicType.DragonRise, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DragonRise, MagicSoundGate.Always) },
        [(MagicType.BladeStorm, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.BladeStorm, MagicSoundGate.Always) },
        [(MagicType.DefensiveBlow, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DefensiveBlow, MagicSoundGate.Always) },
        // 原版 start switch: FlameSplash 播 BladeStorm (不是 FlameSplash)
        [(MagicType.FlameSplash, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.BladeStorm, MagicSoundGate.Always) },
        // 原版仅音效、无抬手特效
        [(MagicType.WaningMoon, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.WaningMoon, MagicSoundGate.Always) },
        [(MagicType.CalamityOfFullMoon, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.CalamityOfFullMoon, MagicSoundGate.Always) },
        [(MagicType.Defiance, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DefianceStart, MagicSoundGate.Always) },
        [(MagicType.Endurance, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DefianceStart, MagicSoundGate.Always) },
        [(MagicType.Renounce, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DefianceStart, MagicSoundGate.Always) },
        [(MagicType.Spiritualism, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DefianceStart, MagicSoundGate.Always) },
        [(MagicType.Invincibility, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.InvincibilityStart, MagicSoundGate.Always) },
        [(MagicType.ReflectDamage, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ReflectDamageStart, MagicSoundGate.Always) },
        [(MagicType.Teleportation, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.TeleportationStart, MagicSoundGate.Always) },
        [(MagicType.Interchange, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.TeleportationStart, MagicSoundGate.Always) },
        [(MagicType.Beckon, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.TeleportationStart, MagicSoundGate.Always) },
        [(MagicType.MassBeckon, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.TeleportationStart, MagicSoundGate.Always) },
        [(MagicType.GeoManipulation, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.TeleportationStart, MagicSoundGate.Always) },
        [(MagicType.Might, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DragonRise, MagicSoundGate.Always) },
        [(MagicType.Fetter, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DragonRise, MagicSoundGate.Always) },
        [(MagicType.SeismicSlam, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.SeismicSlam, MagicSoundGate.Always) },
        [(MagicType.FireBall, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.FireBallStart, MagicSoundGate.Always) },
        [(MagicType.LightningBall, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ThunderBoltStart, MagicSoundGate.Always) },
        // 原版 IceBolt start 无音效; IceBoltStart 存在于枚举但原版不播, 显式压制
        [(MagicType.IceBolt, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.None, MagicSoundGate.Always) },
        [(MagicType.IceAura, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.IceBoltStart, MagicSoundGate.Always) },
        [(MagicType.IceDragon, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.IceBoltStart, MagicSoundGate.Always) },
        [(MagicType.GustBlast, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.GustBlastStart, MagicSoundGate.Always) },
        // 原版 Repulsion start 播 RepulsionEnd
        [(MagicType.Repulsion, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.RepulsionEnd, MagicSoundGate.Always) },
        [(MagicType.ElectricShock, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ElectricShockStart, MagicSoundGate.Always) },
        [(MagicType.AdamantineFireBall, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.GreaterFireBallStart, MagicSoundGate.Always) },
        [(MagicType.MeteorShower, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.GreaterFireBallStart, MagicSoundGate.Always) },
        [(MagicType.FireBounce, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.GreaterFireBallStart, MagicSoundGate.Always) },
        [(MagicType.ThunderBolt, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.LightningStrikeStart, MagicSoundGate.Always) },
        [(MagicType.ThunderStrike, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.LightningStrikeStart, MagicSoundGate.Always) },
        [(MagicType.IceBlades, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.GreaterIceBoltStart, MagicSoundGate.Always) },
        [(MagicType.Cyclone, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.CycloneStart, MagicSoundGate.Always) },
        // 怪物释放 ScortchedEarth 时原版不播 (Race != Monster 门控), GameScene 处理
        [(MagicType.ScortchedEarth, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.LavaStrikeStart, MagicSoundGate.Always) },
        [(MagicType.LightningBeam, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ThunderBoltStart, MagicSoundGate.Always) },
        [(MagicType.FrozenEarth, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.FrozenEarthStart, MagicSoundGate.Always) },
        [(MagicType.BlowEarth, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.BlowEarthStart, MagicSoundGate.Always) },
        [(MagicType.FireWall, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.FireWallStart, MagicSoundGate.Always) },
        [(MagicType.ExpelUndead, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ExpelUndeadStart, MagicSoundGate.Always) },
        [(MagicType.MagicShield, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.MagicShieldStart, MagicSoundGate.Always) },
        [(MagicType.SuperiorMagicShield, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.MagicShieldStart, MagicSoundGate.Always) },
        [(MagicType.FireStorm, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.FireStormStart, MagicSoundGate.Always) },
        [(MagicType.LightningWave, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.LightningWaveStart, MagicSoundGate.Always) },
        [(MagicType.IceStorm, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.IceStormStart, MagicSoundGate.Always) },
        [(MagicType.DragonTornado, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DragonTornadoStart, MagicSoundGate.Always) },
        [(MagicType.GreaterFrozenEarth, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.GreaterFrozenEarthStart, MagicSoundGate.Always) },
        [(MagicType.ChainLightning, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ChainLightningStart, MagicSoundGate.Always) },
        [(MagicType.Tempest, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.BlowEarthStart, MagicSoundGate.Always) },
        // 原版 JudgementOfHeaven start 播 LightningStrikeEnd
        [(MagicType.JudgementOfHeaven, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.LightningStrikeEnd, MagicSoundGate.Always) },
        [(MagicType.FrostBite, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.FrostBiteStart, MagicSoundGate.Always) },
        // 原版 LightningStrike start 播 ChainLightningStart
        [(MagicType.LightningStrike, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ChainLightningStart, MagicSoundGate.Always) },
        [(MagicType.IceRain, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.LightningStrikeStart, MagicSoundGate.Always) },
        [(MagicType.Tornado, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.TornadoStart, MagicSoundGate.Always) },
        [(MagicType.Heal, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.HealStart, MagicSoundGate.Always) },
        [(MagicType.PoisonDust, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.PoisonDustStart, MagicSoundGate.Always) },
        [(MagicType.ExplosiveTalisman, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ExplosiveTalismanStart, MagicSoundGate.Always) },
        [(MagicType.EvilSlayer, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.HolyStrikeStart, MagicSoundGate.Always) },
        [(MagicType.SummonSkeleton, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.SummonSkeletonStart, MagicSoundGate.Always) },
        [(MagicType.SummonJinSkeleton, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.SummonSkeletonStart, MagicSoundGate.Always) },
        [(MagicType.SummonDemonicCreature, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.SummonSkeletonStart, MagicSoundGate.Always) },
        // 原版 Invisibility/Transparency start 播 InvisibilityEnd
        [(MagicType.Invisibility, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.InvisibilityEnd, MagicSoundGate.Always) },
        [(MagicType.Transparency, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.InvisibilityEnd, MagicSoundGate.Always) },
        [(MagicType.GreaterEvilSlayer, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ImprovedHolyStrikeStart, MagicSoundGate.Always) },
        [(MagicType.TrapOctagon, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ShacklingTalismanStart, MagicSoundGate.Always) },
        [(MagicType.CombatKick, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.TaoistCombatKickStart, MagicSoundGate.Always) },
        [(MagicType.SummonShinsu, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.SummonShinsuStart, MagicSoundGate.Always) },
        [(MagicType.MassHeal, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.MassHealStart, MagicSoundGate.Always) },
        [(MagicType.Resurrection, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ResurrectionStart, MagicSoundGate.Always) },
        [(MagicType.Purification, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.PurificationStart, MagicSoundGate.Always) },
        [(MagicType.StrengthOfFaith, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.StrengthOfFaithStart, MagicSoundGate.Always) },
        [(MagicType.CelestialLight, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.MagicShieldStart, MagicSoundGate.Always) },
        [(MagicType.LifeSteal, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.HolyStrikeStart, MagicSoundGate.Always) },
        [(MagicType.ImprovedExplosiveTalisman, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ExplosiveTalismanStart, MagicSoundGate.Always) },
        [(MagicType.CursedDoll, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.SummonSkeletonStart, MagicSoundGate.Always) },
        // 原版 ThunderKick: 前方有目标时播 FireStormEnd, 随后无条件播 TaoistCombatKickStart
        [(MagicType.ThunderKick, MagicSoundPhase.Start)] = new[]
        {
            new SoundSpec(SoundIndex.FireStormEnd, MagicSoundGate.Always),
            new SoundSpec(SoundIndex.TaoistCombatKickStart, MagicSoundGate.Always),
        },
        [(MagicType.Neutralize, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ExplosiveTalismanStart, MagicSoundGate.Always) },
        [(MagicType.CorpseExploder, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ExplosiveTalismanStart, MagicSoundGate.Always) },
        [(MagicType.SummonDead, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.SummonSkeletonStart, MagicSoundGate.Always) },
        [(MagicType.BindingTalisman, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.ExplosiveTalismanStart, MagicSoundGate.Always) },
        // 原版 BrainStorm start 播 BindingTalisman
        [(MagicType.BrainStorm, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.BindingTalisman, MagicSoundGate.Always) },
        [(MagicType.Cloak, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.CloakStart, MagicSoundGate.Always) },
        [(MagicType.WraithGrip, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.WraithGripStart, MagicSoundGate.Always) },
        [(MagicType.HellFire, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.WraithGripStart, MagicSoundGate.Always) },
        [(MagicType.Rake, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.RakeStart, MagicSoundGate.Always) },
        [(MagicType.SummonPuppet, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.SummonPuppet, MagicSoundGate.Always) },
        [(MagicType.TheNewBeginning, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.TheNewBeginning, MagicSoundGate.Always) },
        [(MagicType.DragonRepulse, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.DragonRepulseStart, MagicSoundGate.Always) },
        [(MagicType.Concentration, MagicSoundPhase.Start)] = new[] { new SoundSpec(SoundIndex.Concentration, MagicSoundGate.Always) },

        // ================= End (施法动画结束释放时, 按门控) =================
        [(MagicType.OffensiveBlow, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.OffensiveBlow, MagicSoundGate.Always) },
        [(MagicType.FireBall, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.FireBallTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.LightningBall, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ThunderBoltTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.IceBolt, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.IceBoltTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.AdamantineFireBall, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.GreaterFireBallTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.MeteorShower, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.GreaterFireBallTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.FireBounce, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.GreaterFireBallTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.ThunderBolt, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.LightningStrikeEnd, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.ThunderStrike, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.LightningStrikeEnd, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.IceBlades, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.GreaterIceBoltTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.Cyclone, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.CycloneEnd, MagicSoundGate.Always) },
        [(MagicType.LightningBeam, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.LightningBeamEnd, MagicSoundGate.Locations) },
        [(MagicType.FrozenEarth, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.FrozenEarthEnd, MagicSoundGate.Locations) },
        [(MagicType.GreaterFrozenEarth, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.GreaterFrozenEarthEnd, MagicSoundGate.Locations) },
        [(MagicType.BlowEarth, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.BlowEarthTravel, MagicSoundGate.Locations) },
        [(MagicType.ExpelUndead, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ExpelUndeadEnd, MagicSoundGate.Always) },
        [(MagicType.FireStorm, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.FireStormEnd, MagicSoundGate.Always) },
        [(MagicType.LightningWave, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.LightningWaveEnd, MagicSoundGate.Always) },
        [(MagicType.IceStorm, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.IceStormEnd, MagicSoundGate.Always) },
        [(MagicType.DragonTornado, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.DragonTornadoEnd, MagicSoundGate.Always) },
        [(MagicType.ChainLightning, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ChainLightningEnd, MagicSoundGate.Locations) },
        [(MagicType.LightningStrike, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.LightningBeamEnd, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.IceRain, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.IceBoltTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.Heal, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.HealEnd, MagicSoundGate.Targets) },
        [(MagicType.PoisonDust, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.PoisonDustEnd, MagicSoundGate.Targets) },
        [(MagicType.ExplosiveTalisman, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ExplosiveTalismanTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.ImprovedExplosiveTalisman, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ExplosiveTalismanTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.BindingTalisman, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ExplosiveTalismanTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.Neutralize, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.NeutralizeTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.CorpseExploder, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ExplosiveTalismanTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.Parasite, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ParasiteTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.SearingLight, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.HolyStrikeTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.PinkFireBall, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.FireBallTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.GreenSludgeBall, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.FireBallTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.GustBlast, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.GustBlastTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.IceDragon, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.IceDragonTravel, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.IceAura, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.IceAuraTravel, MagicSoundGate.Always) },
        [(MagicType.IceBreaker, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.IceBreaker, MagicSoundGate.Always) },
        [(MagicType.FrozenDragon, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.FrozenDragon, MagicSoundGate.Always) },
        [(MagicType.FireSword, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.FireSword, MagicSoundGate.Always) },
        [(MagicType.TaecheonSword, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.TaecheonSword, MagicSoundGate.Locations) },
        [(MagicType.SwiftBlade, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.SwiftBladeEnd, MagicSoundGate.Always) },
        [(MagicType.BurningFire, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.FireWallStart, MagicSoundGate.Always) },
        [(MagicType.ElectricShock, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ElectricShockEnd, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.MassHeal, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.MassHealEnd, MagicSoundGate.Always) },
        [(MagicType.Purification, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.PurificationEnd, MagicSoundGate.Targets) },
        [(MagicType.StrengthOfFaith, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.StrengthOfFaithEnd, MagicSoundGate.Targets) },
        [(MagicType.BloodLust, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.BloodLustTravel, MagicSoundGate.Locations) },
        [(MagicType.Resilience, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ResilienceTravel, MagicSoundGate.Locations) },
        [(MagicType.MagicResistance, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.MagicResistanceTravel, MagicSoundGate.Locations) },
        [(MagicType.ElementalSuperiority, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.BloodLustTravel, MagicSoundGate.Locations) },
        [(MagicType.MassInvisibility, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.MassInvisibilityTravel, MagicSoundGate.Locations) },
        [(MagicType.DarkSoulPrison, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.DarkSoulPrison, MagicSoundGate.Always) },
        [(MagicType.WraithGrip, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.WraithGripEnd, MagicSoundGate.Targets) },
        [(MagicType.HellFire, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.WraithGripEnd, MagicSoundGate.Targets) },
        [(MagicType.LifeSteal, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.HolyStrikeEnd, MagicSoundGate.Targets) },
        [(MagicType.Hemorrhage, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.Hemorrhage, MagicSoundGate.LocationsOrTargets) },
        [(MagicType.Shredding, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.Shredding, MagicSoundGate.Targets) },
        [(MagicType.TrapOctagon, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.ShacklingTalismanEnd, MagicSoundGate.Always) },
        [(MagicType.HeavenlySky, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.HeavenlySky, MagicSoundGate.Locations) },
        [(MagicType.PoisonCloud, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.PoisonCloud, MagicSoundGate.Locations) },
        // 原版释放无音效 (着弹音效在 def 的 CompletionSound)
        [(MagicType.Resurrection, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.None, MagicSoundGate.Always) },
        [(MagicType.CelestialLight, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.None, MagicSoundGate.Always) },
        [(MagicType.FlamingDaggers, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.None, MagicSoundGate.Always) },
        [(MagicType.BrainStorm, MagicSoundPhase.End)] = new[] { new SoundSpec(SoundIndex.None, MagicSoundGate.Always) },
    };

    /// <summary>解析 (魔法, 阶段) 的全部音效规格。显式 None 也会压制后缀回退。</summary>
    public static IEnumerable<SoundSpec> ResolveSpecs(MagicType magic, MagicSoundPhase phase)
    {
        if (Explicit.TryGetValue((magic, phase), out var specs))
        {
            foreach (var spec in specs)
                if (spec.Sound != SoundIndex.None)
                    yield return spec;
            yield break;
        }
        var suffix = phase switch
        {
            MagicSoundPhase.Start => "Start",
            MagicSoundPhase.Travel => "Travel",
            MagicSoundPhase.End => "End",
            MagicSoundPhase.Duration => "Duration",
            _ => "",
        };
        if (Enum.TryParse($"{magic}{suffix}", out SoundIndex resolved) &&
            resolved != SoundIndex.None &&
            SoundCatalog.Entries.ContainsKey(resolved))
            yield return new SoundSpec(resolved, MagicSoundGate.Always);
    }

    /// <summary>门控判断 (原版 release switch 的计数检查)。</summary>
    public static bool GateSatisfied(MagicSoundGate gate, bool hasLocations, bool hasTargets) => gate switch
    {
        MagicSoundGate.Always => true,
        MagicSoundGate.Locations => hasLocations,
        MagicSoundGate.Targets => hasTargets,
        MagicSoundGate.LocationsOrTargets => hasLocations || hasTargets,
        _ => false,
    };

    public static SoundIndex Resolve(MagicType magic, MagicSoundPhase phase)
    {
        foreach (var spec in ResolveSpecs(magic, phase)) return spec.Sound;
        return SoundIndex.None;
    }

    public static IEnumerable<SoundIndex> ResolveAll(MagicType magic, MagicSoundPhase phase)
    {
        foreach (var spec in ResolveSpecs(magic, phase))
            if (spec.Sound != SoundIndex.None && magic != MagicType.SweetBrier)
                yield return spec.Sound;
    }
}
