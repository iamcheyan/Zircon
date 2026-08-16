using System.Collections.Generic;
using Library;

namespace ZirconClient.Scripts;

/// <summary>
/// Mechanical port of Client/Envir/DXSoundManager.SoundList.
/// Keep this table data-only; trigger timing belongs to the owning behavior.
/// </summary>
public enum SoundCategory { Music, Player, System, Magic, Monster }

public readonly record struct SoundEntry(string FileName, SoundCategory Category, bool Loop = false);

public static class SoundCatalog
{
    public static readonly IReadOnlyDictionary<SoundIndex, SoundEntry> Entries = new Dictionary<SoundIndex, SoundEntry>();
    // E5/B4 cutover: 数据本体已迁 zircon/ClientData/sounds.json (DataLayer 装载)

    public static bool TryGet(SoundIndex sound, out SoundEntry entry) => Entries.TryGetValue(sound, out entry);
}
