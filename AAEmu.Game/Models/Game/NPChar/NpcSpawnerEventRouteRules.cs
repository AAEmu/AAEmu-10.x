namespace AAEmu.Game.Models.Game.NPChar;

/// <summary>
/// Where to send a zone <c>NpcSpawnerEvent</c> for a level-file placement id.
/// Cross-ocean event skills (Abyssal seed OnSpawn) arm city recruiters whose
/// placements live in other zones — never only the caster's zone.
/// </summary>
public static class NpcSpawnerEventRouteRules
{
    /// <summary>
    /// Prefer every loaded zone that owns the placement; otherwise the creator's zone
    /// (same-zone spawners / unknown placement id).
    /// </summary>
    public static IReadOnlyList<uint> ResolveTargetZones(
        uint placementId,
        uint creatorZoneId,
        IReadOnlyDictionary<uint, IReadOnlyCollection<uint>> placementIdsByLoadedZone)
    {
        if (placementId == 0)
            return creatorZoneId != 0 ? [creatorZoneId] : [];

        List<uint> hits = null;
        foreach (var (zoneId, ids) in placementIdsByLoadedZone)
        {
            if (zoneId == 0 || ids == null || ids.Count == 0)
                continue;
            if (!ids.Contains(placementId))
                continue;
            hits ??= [];
            hits.Add(zoneId);
        }

        if (hits is { Count: > 0 })
            return hits;

        return creatorZoneId != 0 ? [creatorZoneId] : [];
    }
}
