using System.Collections.Generic;
using Library;

namespace ZirconClient.Scripts;

public readonly record struct MonsterSoundSet(SoundIndex Attack, SoundIndex Struck, SoundIndex Die);

/// <summary>Mechanical port of MonsterObject.UpdateLibraries sound selection.</summary>
public static class MonsterSoundCatalog
{
    public static readonly IReadOnlyDictionary<MonsterImage, MonsterSoundSet> Entries = new Dictionary<MonsterImage, MonsterSoundSet>();
    // E5/B4 cutover: 数据本体已迁 zircon/ClientData/sounds.json (DataLayer 装载)

    public static MonsterSoundSet Get(MonsterImage image) =>
        Entries.TryGetValue(image, out var sounds)
            ? sounds
            : new(SoundIndex.None, SoundIndex.GenericStruckMonster, SoundIndex.None);
}

