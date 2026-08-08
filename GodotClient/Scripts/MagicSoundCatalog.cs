using System;
using System.Collections.Generic;
using Library;

namespace ZirconClient.Scripts;

public enum MagicSoundPhase { Start, Travel, End, Duration }

/// <summary>
/// Resolves the original Start/Travel/End/Duration naming convention and keeps
/// the handful of legacy names whose SoundIndex does not match MagicType.
/// </summary>
public static class MagicSoundCatalog
{
    private static readonly Dictionary<(MagicType, MagicSoundPhase), SoundIndex> Explicit = new()
    {
        [(MagicType.CrushingWave, MagicSoundPhase.End)] = SoundIndex.DestructiveSurge,
        [(MagicType.OffensiveBlow, MagicSoundPhase.End)] = SoundIndex.OffensiveBlow,
        [(MagicType.Assault, MagicSoundPhase.Start)] = SoundIndex.AssaultStart,
        [(MagicType.HundredFist, MagicSoundPhase.Start)] = SoundIndex.HundredFist,
        [(MagicType.FireBounce, MagicSoundPhase.Travel)] = SoundIndex.GreaterFireBallTravel,
        [(MagicType.FireBounce, MagicSoundPhase.End)] = SoundIndex.GreaterFireBallEnd,
        [(MagicType.LightningStrike, MagicSoundPhase.End)] = SoundIndex.LightningBeamEnd,
        [(MagicType.ElementalSwords, MagicSoundPhase.End)] = SoundIndex.ElementalSwordsEnd,
        [(MagicType.Rake, MagicSoundPhase.Start)] = SoundIndex.RakeStart,
        [(MagicType.DragonRepulse, MagicSoundPhase.Start)] = SoundIndex.DragonRepulseStart,
        [(MagicType.WraithGrip, MagicSoundPhase.End)] = SoundIndex.WraithGripEnd,
        [(MagicType.SweetBrier, MagicSoundPhase.Start)] = SoundIndex.SweetBrier,
        [(MagicType.Karma, MagicSoundPhase.Start)] = SoundIndex.SweetBrier,
    };

    public static SoundIndex Resolve(MagicType magic, MagicSoundPhase phase)
    {
        if (Explicit.TryGetValue((magic, phase), out var sound)) return sound;
        var suffix = phase switch
        {
            MagicSoundPhase.Start => "Start",
            MagicSoundPhase.Travel => "Travel",
            MagicSoundPhase.End => "End",
            MagicSoundPhase.Duration => "Duration",
            _ => "",
        };
        if (Enum.TryParse($"{magic}{suffix}", out SoundIndex resolved) &&
            SoundCatalog.Entries.ContainsKey(resolved)) return resolved;
        return SoundIndex.None;
    }

    public static IEnumerable<SoundIndex> ResolveAll(MagicType magic, MagicSoundPhase phase)
    {
        var sound = Resolve(magic, phase);
        if (sound != SoundIndex.None && magic != MagicType.SweetBrier)
            yield return sound;
    }
}
