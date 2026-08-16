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
    private static readonly Dictionary<(MagicType, MagicSoundPhase), SoundSpec[]> Explicit = new();
        // E5/B4 cutover: 数据本体已迁 zircon/ClientData/sounds.json (DataLayer 装载)

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
