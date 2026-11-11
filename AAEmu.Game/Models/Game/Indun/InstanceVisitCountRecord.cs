namespace AAEmu.Game.Models.Game.Indun;

/// <summary>
/// One row of SCInstanceVisitCounts / SCInstanceVisitCountChange (20-byte elem). Client field names: zoneId, data,
/// count, resetCount, permittedCount. A dungeon row is keyed (zone_key, 0); see <see cref="IndunEntryRules.VisitRow"/>.
/// </summary>
public readonly record struct InstanceVisitCountRecord(
    int ZoneId,
    uint Data,
    int UsedCount,
    int ResetCount,
    int PermittedCount);
