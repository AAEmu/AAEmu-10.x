namespace AAEmu.Game.Models.Game.Indun;

/// <summary>
/// Pure rules for dungeon daily entries and create cooldowns (no DB / world state).
/// </summary>
public static class IndunEntryRules
{
    /// <summary>UTC calendar day start used for daily enter caps (UI: reset at 0:00).</summary>
    public static DateTime DailyWindowStartUtc(DateTime utcNow) =>
        DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);

    public static int CountEntriesInDailyWindow(IEnumerable<DateTime> entryTimesUtc, DateTime utcNow)
    {
        var dayStart = DailyWindowStartUtc(utcNow);
        var n = 0;
        foreach (var raw in entryTimesUtc)
        {
            var t = raw.Kind switch
            {
                DateTimeKind.Utc => raw,
                DateTimeKind.Local => raw.ToUniversalTime(),
                _ => DateTime.SpecifyKind(raw, DateTimeKind.Utc)
            };

            if (t >= dayStart)
                n++;
        }

        return n;
    }

    /// <summary>
    /// True when a new instance create is blocked by <see cref="IndunZone.RestoreItemTime"/>.
    /// Rejoin of an already-bound copy must not call this.
    /// </summary>
    public static bool IsCreateOnCooldown(
        DateTime? lastCreateUtc,
        DateTime utcNow,
        uint restoreItemTimeSeconds)
    {
        if (restoreItemTimeSeconds == 0 || lastCreateUtc is null)
            return false;

        var last = lastCreateUtc.Value;
        if (last.Kind == DateTimeKind.Unspecified)
            last = DateTime.SpecifyKind(last, DateTimeKind.Utc);
        else if (last.Kind == DateTimeKind.Local)
            last = last.ToUniversalTime();

        var elapsed = (utcNow - last).TotalSeconds;
        return elapsed < restoreItemTimeSeconds;
    }

    /// <summary>
    /// Prefer <c>instances.enter_count</c> for IndunZone rows; fall back for zones with no catalog row.
    /// </summary>
    public static uint ResolveEnterCount(uint? instancesEnterCount, bool selectChannel, uint zoneGroupId)
    {
        if (instancesEnterCount.HasValue)
            return instancesEnterCount.Value;

        // Legacy fallback when compact has no instances row for this zone group.
        return zoneGroupId == 49 || selectChannel ? 1000u : 3u;
    }

    /// <summary>Client <c>IVT_RESET</c> — buy a visit-count reset ticket.</summary>
    public const sbyte VisitTypeReset = 3;
    /// <summary>Client <c>IVT_PERMIT</c> — buy an extra daily enter.</summary>
    public const sbyte VisitTypePermit = 4;

    /// <summary>Ticket stack cost for the next RESET purchase.</summary>
    public static int ResetTicketCost(int currentResetCount, int increaseScale)
    {
        var scale = increaseScale > 0 ? increaseScale : 1;
        return scale * (currentResetCount + 1);
    }

    /// <summary>True when another RESET purchase is allowed.</summary>
    public static bool CanBuyReset(int currentResetCount, int resetLimit) =>
        resetLimit <= 0 || currentResetCount < resetLimit;

    /// <summary>
    /// The client's entry check: today's entries stay below enter_count and, where the dungeon needs a pass
    /// (permit_enter_count_item_id), below the passes registered today.
    /// </summary>
    public static bool CanEnter(uint enterCount, bool needsPermit, int used, int permitsUsed) =>
        used < enterCount && (!needsPermit || used < permitsUsed);

    /// <summary>
    /// Drops a character's daily records for one zone group, or every group when <paramref name="zoneGroupId"/> is 0.
    /// </summary>
    public static int ClearDaily(
        Dictionary<uint, Dictionary<uint, List<DateTime>>> store, uint characterId, uint zoneGroupId)
    {
        if (!store.TryGetValue(characterId, out var byZone))
            return 0;

        if (zoneGroupId != 0)
            return byZone.TryGetValue(zoneGroupId, out var one) ? one.RemoveAll(_ => true) : 0;

        var total = byZone.Values.Sum(entries => entries.Count);
        byZone.Clear();
        return total;
    }

    /// <summary>Passes registered today never exceed enter_count (the client shows them as "Allowed Entries x/enter_count").</summary>
    public static bool CanRegisterPermit(uint enterCount, int permitsUsed) => permitsUsed < enterCount;

    /// <summary>
    /// The zone_key a dungeon's visit row is keyed by: the first zones row (lowest id) of its group, or 0 when none.
    /// </summary>
    public static uint VisitRowZoneKey(uint zoneGroupId, IEnumerable<(uint Id, uint ZoneKey, uint GroupId)> zones) =>
        zones.Where(z => z.GroupId == zoneGroupId).OrderBy(z => z.Id).Select(z => z.ZoneKey).FirstOrDefault();

    /// <summary>A dungeon's visit row; permittedCount is the passes registered today.</summary>
    public static InstanceVisitCountRecord VisitRow(uint zoneKey, int used, int resetCount, int permitsUsed) =>
        new((int)zoneKey, 0, used, resetCount, permitsUsed);
}
