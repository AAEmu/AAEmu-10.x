using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Models.Game.Indun;

public class IndunEntryRulesTests
{
    [Test]
    public async Task DailyWindowStartUtc_IsMidnightOfThatUtcDay()
    {
        var now = new DateTime(2026, 8, 21, 15, 30, 0, DateTimeKind.Utc);
        await Assert.That(IndunEntryRules.DailyWindowStartUtc(now))
            .IsEqualTo(new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc));
    }

    private static readonly TimeZoneInfo Seoul = TimeZoneInfo.CreateCustomTimeZone("UTC+9", TimeSpan.FromHours(9), "UTC+9", "UTC+9");

    [Test]
    public async Task NonUtcInstance_DayStartsAtLocalMidnight()
    {
        // instances.use_utc = f (75 of 76, the Island among them): 23:30 local is still today, 00:30 local is tomorrow.
        var lateEvening = new DateTime(2026, 8, 21, 14, 30, 0, DateTimeKind.Utc); // 23:30 at UTC+9
        var afterMidnight = new DateTime(2026, 8, 21, 15, 30, 0, DateTimeKind.Utc); // 00:30 at UTC+9

        await Assert.That(IndunEntryRules.DailyWindowStartUtc(lateEvening, Seoul))
            .IsEqualTo(new DateTime(2026, 8, 20, 15, 0, 0, DateTimeKind.Utc));
        await Assert.That(IndunEntryRules.DailyWindowStartUtc(afterMidnight, Seoul))
            .IsEqualTo(new DateTime(2026, 8, 21, 15, 0, 0, DateTimeKind.Utc));
        await Assert.That(IndunEntryRules.CountEntriesInDailyWindow([lateEvening], afterMidnight, Seoul)).IsEqualTo(0);
    }

    [Test]
    public async Task DailyTimeZone_FollowsUseUtc()
    {
        await Assert.That(IndunEntryRules.DailyTimeZone(useUtc: true)).IsEqualTo(TimeZoneInfo.Utc);
        await Assert.That(IndunEntryRules.DailyTimeZone(useUtc: false)).IsEqualTo(TimeZoneInfo.Local);
    }

    [Test]
    public async Task CountEntriesInDailyWindow_IgnoresYesterday()
    {
        var now = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);
        var entries = new[]
        {
            new DateTime(2026, 8, 20, 23, 59, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 21, 11, 0, 0, DateTimeKind.Utc)
        };
        await Assert.That(IndunEntryRules.CountEntriesInDailyWindow(entries, now)).IsEqualTo(2);
    }

    [Test]
    public async Task IsCreateOnCooldown_RespectsRestoreItemTime()
    {
        var now = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);
        var last = now.AddSeconds(-3599);
        await Assert.That(IndunEntryRules.IsCreateOnCooldown(last, now, 3600)).IsTrue();
        await Assert.That(IndunEntryRules.IsCreateOnCooldown(now.AddSeconds(-3600), now, 3600)).IsFalse();
        await Assert.That(IndunEntryRules.IsCreateOnCooldown(null, now, 3600)).IsFalse();
        await Assert.That(IndunEntryRules.IsCreateOnCooldown(last, now, 0)).IsFalse();
    }

    [Test]
    public async Task IsCreateOnCooldown_NullLastCreate_MeansNoCooldown()
    {
        // Same as ClearCreateCooldown after portal reset — next create is allowed.
        var now = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);
        await Assert.That(IndunEntryRules.IsCreateOnCooldown(null, now, 3600)).IsFalse();
    }

    [Test]
    public async Task ResolveEnterCount_PrefersInstancesTable()
    {
        await Assert.That(IndunEntryRules.ResolveEnterCount(3, selectChannel: false, zoneGroupId: 51)).IsEqualTo(3u);
        await Assert.That(IndunEntryRules.ResolveEnterCount(null, selectChannel: true, zoneGroupId: 99)).IsEqualTo(1000u);
        await Assert.That(IndunEntryRules.ResolveEnterCount(null, selectChannel: false, zoneGroupId: 49)).IsEqualTo(1000u);
        await Assert.That(IndunEntryRules.ResolveEnterCount(null, selectChannel: false, zoneGroupId: 51)).IsEqualTo(3u);
    }

    [Test]
    public async Task ResetTicketCost_ScalesByPurchaseCount()
    {
        await Assert.That(IndunEntryRules.ResetTicketCost(0, 1)).IsEqualTo(1);
        await Assert.That(IndunEntryRules.ResetTicketCost(2, 1)).IsEqualTo(3);
        await Assert.That(IndunEntryRules.ResetTicketCost(1, 2)).IsEqualTo(4);
        await Assert.That(IndunEntryRules.ResetTicketCost(0, 0)).IsEqualTo(1);
    }

    [Test]
    public async Task CanBuyReset_RespectsLimit()
    {
        await Assert.That(IndunEntryRules.CanBuyReset(0, 0)).IsTrue();
        await Assert.That(IndunEntryRules.CanBuyReset(2, 3)).IsTrue();
        await Assert.That(IndunEntryRules.CanBuyReset(3, 3)).IsFalse();
    }

    [Test]
    public async Task CanEnter_WithoutPermitItem_OnlyCountsEntries()
    {
        await Assert.That(IndunEntryRules.CanEnter(enterCount: 3, needsPermit: false, used: 2, permitsUsed: 0)).IsTrue();
        await Assert.That(IndunEntryRules.CanEnter(enterCount: 3, needsPermit: false, used: 3, permitsUsed: 0)).IsFalse();
    }

    [Test]
    public async Task CanEnter_PermitDungeon_NeedsAPassPerEntry()
    {
        // Island of Abundance: enter_count 1, permit item 54614.
        await Assert.That(IndunEntryRules.CanEnter(enterCount: 1, needsPermit: true, used: 0, permitsUsed: 0)).IsFalse();
        await Assert.That(IndunEntryRules.CanEnter(enterCount: 1, needsPermit: true, used: 0, permitsUsed: 1)).IsTrue();
        await Assert.That(IndunEntryRules.CanEnter(enterCount: 1, needsPermit: true, used: 1, permitsUsed: 1)).IsFalse();
    }

    [Test]
    public async Task CanRegisterPermit_StopsAtEnterCount()
    {
        await Assert.That(IndunEntryRules.CanRegisterPermit(enterCount: 1, permitsUsed: 0)).IsTrue();
        await Assert.That(IndunEntryRules.CanRegisterPermit(enterCount: 1, permitsUsed: 1)).IsFalse();
    }

    [Test]
    public async Task VisitRowZoneKey_IsTheFirstZoneOfTheGroup()
    {
        // The client keys a dungeon's visit row by the zone_key of the first zones row in its group.
        (uint Id, uint ZoneKey, uint GroupId)[] zones = [(400, 999, 158), (323, 410, 158), (1, 1, 1)];

        await Assert.That(IndunEntryRules.VisitRowZoneKey(158, zones)).IsEqualTo(410u);
        await Assert.That(IndunEntryRules.VisitRowZoneKey(77, zones)).IsEqualTo(0u);
    }

    [Test]
    public async Task VisitRow_CarriesZoneKeyAndPassesUsed()
    {
        var row = IndunEntryRules.VisitRow(zoneKey: 410, used: 0, resetCount: 2, permitsUsed: 1);

        await Assert.That(row).IsEqualTo(new InstanceVisitCountRecord(ZoneId: 410, Data: 0, UsedCount: 0, ResetCount: 2, PermittedCount: 1));
    }

    [Test]
    public async Task PortalReset_ShouldAllowImmediateCreate_AfterClearingCooldown()
    {
        // Documented contract: restore_item_time blocks create until cleared by 초기화.
        var now = new DateTime(2026, 8, 22, 3, 17, 0, DateTimeKind.Utc);
        var lastCreate = now.AddMinutes(-5);
        await Assert.That(IndunEntryRules.IsCreateOnCooldown(lastCreate, now, 3600)).IsTrue();
        // After ClearCreateCooldown, lastCreate is null → not on cooldown.
        await Assert.That(IndunEntryRules.IsCreateOnCooldown(null, now, 3600)).IsFalse();
    }

    [Test]
    public async Task ClearDaily_DropsOneZoneGroupForOneCharacter()
    {
        // Island passes (zone group 158) blocked a GM retest after /indun reset cleared only the entries.
        var t = new DateTime(2026, 11, 11, 16, 0, 0, DateTimeKind.Utc);
        var store = new Dictionary<uint, Dictionary<uint, List<DateTime>>>
        {
            [1001] = new() { [158] = [t, t], [7] = [t] },
            [1002] = new() { [158] = [t] }
        };

        await Assert.That(IndunEntryRules.ClearDaily(store, 1001, 158)).IsEqualTo(2);
        await Assert.That(store[1001][158]).IsEmpty();
        await Assert.That(store[1001][7].Count).IsEqualTo(1);
        await Assert.That(store[1002][158].Count).IsEqualTo(1);
    }

    [Test]
    public async Task ClearDaily_ZoneGroupZero_DropsEveryGroupOfThatCharacter()
    {
        var t = new DateTime(2026, 11, 11, 16, 0, 0, DateTimeKind.Utc);
        var store = new Dictionary<uint, Dictionary<uint, List<DateTime>>>
        {
            [1001] = new() { [158] = [t], [7] = [t, t] }
        };

        await Assert.That(IndunEntryRules.ClearDaily(store, 1001, 0)).IsEqualTo(3);
        await Assert.That(store[1001]).IsEmpty();
        await Assert.That(IndunEntryRules.ClearDaily(store, 9999, 0)).IsEqualTo(0);
    }
}
