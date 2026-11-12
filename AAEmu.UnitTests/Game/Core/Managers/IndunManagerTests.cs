using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Indun;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;
using AAEmu.Game.Models.Game.World.Zones;

using System.Reflection;

namespace AAEmu.UnitTests.Game.Core.Managers;

[NotInParallel]
public class IndunManagerTests
{
    private object _originalIndunZones;

    [Before(Test)]
    public void SaveIndunZones()
    {
        _originalIndunZones = GetIndunZonesField().GetValue(IndunGameData.Instance)!;
    }

    [After(Test)]
    public void RestoreIndunZones()
    {
        GetIndunZonesField().SetValue(IndunGameData.Instance, _originalIndunZones);
    }

    [Test]
    public async Task RequestLeaveInstance_RejectsCharacterOutsideCurrentDungeon()
    {
        var manager = new IndunManager(
            Mock.Of<ITickManager>().Object,
            Mock.Of<IWorldManager>().Object,
            Mock.Of<IZoneManager>().Object,
            Mock.Of<ITeamManager>().Object);

        var result = manager.RequestLeaveInstance(new AAEmu.Game.Models.Game.Char.Character(new UnitCustomModelParams()));

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task VisitRow_CountsAnEntryAsSoonAsItIsCharged()
    {
        // The island entry was charged but the client only learned of it at the next character select.
        var manager = new IndunManager(
            Mock.Of<ITickManager>().Object,
            Mock.Of<IWorldManager>().Object,
            Mock.Of<IZoneManager>().Object,
            Mock.Of<ITeamManager>().Object);
        var island = new IndunZone { ZoneGroupId = 158, EnterCount = 2 };

        manager.CheckEntryAttemptCount(1001, 158, island, true);

        await Assert.That(manager.GetVisitRow(1001, island).UsedCount).IsEqualTo(1);
    }

    private sealed class MemoryPermitStore : IIndunPermitStore
    {
        public readonly List<(uint CharacterId, uint ZoneGroupId, DateTime At, DateTime? SpentAt)> Rows = [];

        public IReadOnlyList<(uint ZoneGroupId, DateTime At, DateTime? SpentAt)> Load(uint characterId, DateTime sinceUtc) =>
            Rows.Where(r => r.CharacterId == characterId && r.At >= sinceUtc)
                .Select(r => (r.ZoneGroupId, r.At, r.SpentAt)).ToList();

        // DATETIME(3): the store keeps whole milliseconds.
        // Like INSERT IGNORE on the primary key: false when the row already exists.
        public bool Add(uint characterId, uint zoneGroupId, DateTime atUtc)
        {
            atUtc = atUtc.AddTicks(-(atUtc.Ticks % TimeSpan.TicksPerMillisecond));
            if (Rows.Any(r => r.CharacterId == characterId && r.ZoneGroupId == zoneGroupId && r.At == atUtc))
                return false;
            Rows.Add((characterId, zoneGroupId, atUtc, null));
            return true;
        }

        public void MarkSpent(uint characterId, uint zoneGroupId, DateTime spentAtUtc)
        {
            var i = Rows.FindIndex(r => r.CharacterId == characterId && r.ZoneGroupId == zoneGroupId && r.SpentAt == null);
            if (i >= 0)
                Rows[i] = Rows[i] with { SpentAt = spentAtUtc.AddTicks(-(spentAtUtc.Ticks % TimeSpan.TicksPerMillisecond)) };
        }

        public void Clear(uint characterId, uint zoneGroupId) =>
            Rows.RemoveAll(r => r.CharacterId == characterId && (zoneGroupId == 0 || r.ZoneGroupId == zoneGroupId));
    }

    private static IndunManager ManagerWith(IIndunPermitStore store) =>
        new(Mock.Of<ITickManager>().Object, Mock.Of<IWorldManager>().Object, Mock.Of<IZoneManager>().Object,
            Mock.Of<ITeamManager>().Object) { PermitStore = store };

    [Test]
    public async Task APaidPass_ComesBackAfterAWorldRestart()
    {
        // The pass item is used up when it is registered; a restart before entering must not lose it.
        var store = new MemoryPermitStore();
        var island = new IndunZone { ZoneGroupId = 158, EnterCount = 2, UseUtcEntranceTimes = true };
        ManagerWith(store).RecordPermit(1001, 158, DateTime.UtcNow);

        var afterRestart = ManagerWith(store);
        afterRestart.LoadPermitRegistrations(1001);
        afterRestart.LoadPermitRegistrations(1001);

        await Assert.That(afterRestart.GetVisitRow(1001, island).PermittedCount).IsEqualTo(1);
    }

    [Test]
    public async Task ARelog_DoesNotCountAPassTwice()
    {
        var store = new MemoryPermitStore();
        var island = new IndunZone { ZoneGroupId = 158, EnterCount = 2, UseUtcEntranceTimes = true };
        var manager = ManagerWith(store);
        manager.RecordPermit(1001, 158, DateTime.UtcNow.AddTicks(1234));

        manager.LoadPermitRegistrations(1001);

        await Assert.That(manager.GetVisitRow(1001, island).PermittedCount).IsEqualTo(1);
    }

    [Test]
    public async Task TwoPurchasesInTheSameMillisecond_AreTwoPasses()
    {
        var store = new MemoryPermitStore();
        var island = new IndunZone { ZoneGroupId = 158, EnterCount = 2, UseUtcEntranceTimes = true };
        var manager = ManagerWith(store);
        var at = DateTime.UtcNow;

        manager.RecordPermit(1001, 158, at);
        manager.RecordPermit(1001, 158, at);

        await Assert.That(manager.GetVisitRow(1001, island).PermittedCount).IsEqualTo(2);
        await Assert.That(store.Rows.Count).IsEqualTo(2);
    }

    private sealed class FlakyPermitStore(int failures) : IIndunPermitStore
    {
        public readonly MemoryPermitStore Inner = new();
        private int _failuresLeft = failures;

        public IReadOnlyList<(uint ZoneGroupId, DateTime At, DateTime? SpentAt)> Load(uint characterId, DateTime sinceUtc) =>
            Inner.Load(characterId, sinceUtc);

        public void MarkSpent(uint characterId, uint zoneGroupId, DateTime spentAtUtc) =>
            Inner.MarkSpent(characterId, zoneGroupId, spentAtUtc);

        public bool Add(uint characterId, uint zoneGroupId, DateTime atUtc)
        {
            if (_failuresLeft-- > 0)
                throw new InvalidOperationException("db down");
            return Inner.Add(characterId, zoneGroupId, atUtc);
        }

        public void Clear(uint characterId, uint zoneGroupId) => Inner.Clear(characterId, zoneGroupId);

        public void Heal() => _failuresLeft = 0;
    }

    [Test]
    public async Task APassWhoseWriteFailsOnce_IsStoredOnRetry()
    {
        var store = new FlakyPermitStore(1);

        ManagerWith(store).RecordPermit(1001, 158, DateTime.UtcNow);

        await Assert.That(store.Inner.Rows.Count).IsEqualTo(1);
    }

    [Test]
    public async Task APassWhoseWritesKeepFailing_IsStoredAtTheNextLogin()
    {
        var store = new FlakyPermitStore(100);
        var manager = ManagerWith(store);
        manager.RecordPermit(1001, 158, DateTime.UtcNow);
        store.Heal();

        manager.LoadPermitRegistrations(1001);

        await Assert.That(store.Inner.Rows.Count).IsEqualTo(1);
    }

    [Test]
    public async Task APassWhoseSlotIsTakenInTheStore_MovesToTheNextMillisecond()
    {
        // Memory is empty (the login load failed) but the store already holds a pass at that millisecond.
        var store = new MemoryPermitStore();
        var at = DateTime.UtcNow;
        store.Add(1001, 158, at);

        ManagerWith(store).RecordPermit(1001, 158, at);

        await Assert.That(store.Rows.Select(r => r.At).Distinct().Count()).IsEqualTo(2);
    }

    [Test]
    public async Task APassUsedToEnter_StaysSpentAfterAWorldRestart()
    {
        var store = new MemoryPermitStore();
        var dungeon = new IndunZone { ZoneGroupId = 158, EnterCount = 1, PermitEnterCountItemId = 9, UseUtcEntranceTimes = true };
        var before = ManagerWith(store);
        before.RecordPermit(1001, 158, DateTime.UtcNow);
        before.CheckEntryAttemptCount(1001, 158, dungeon, true);

        var afterRestart = ManagerWith(store);
        afterRestart.LoadPermitRegistrations(1001);
        var allowed = afterRestart.CheckEntryAttemptCount(1001, 158, dungeon, false);

        await Assert.That(allowed).IsFalse();
    }

    [Test]
    public async Task AnUnusedPass_StillAllowsOneEntryAfterAWorldRestart()
    {
        var store = new MemoryPermitStore();
        var dungeon = new IndunZone { ZoneGroupId = 158, EnterCount = 1, PermitEnterCountItemId = 9, UseUtcEntranceTimes = true };
        ManagerWith(store).RecordPermit(1001, 158, DateTime.UtcNow);

        var afterRestart = ManagerWith(store);
        afterRestart.LoadPermitRegistrations(1001);

        await Assert.That(afterRestart.CheckEntryAttemptCount(1001, 158, dungeon, false)).IsTrue();
    }

    [Test]
    public async Task ARelog_DoesNotCountASpentPassAsASecondEntry()
    {
        var store = new MemoryPermitStore();
        var dungeon = new IndunZone { ZoneGroupId = 158, EnterCount = 2, PermitEnterCountItemId = 9, UseUtcEntranceTimes = true };
        var manager = ManagerWith(store);
        manager.RecordPermit(1001, 158, DateTime.UtcNow);
        manager.RecordPermit(1001, 158, DateTime.UtcNow);
        manager.CheckEntryAttemptCount(1001, 158, dungeon, true);

        manager.LoadPermitRegistrations(1001);

        await Assert.That(manager.GetVisitRow(1001, dungeon).UsedCount).IsEqualTo(1);
    }

    [Test]
    public async Task ClearingEntryHistory_AlsoForgetsStoredPasses()
    {
        var store = new MemoryPermitStore();
        var manager = ManagerWith(store);
        manager.RecordPermit(1001, 158, DateTime.UtcNow);

        manager.ClearEntryHistory(1001, 158);

        await Assert.That(store.Rows).IsEmpty();
    }

    [Test]
    public void Initialize_SubscribesToTickManager()
    {
        var mockTick = Mock.Of<ITickManager>();
        mockTick.OnTick.Returns(new TickManager.TickEventHandler());
        var manager = new IndunManager(mockTick.Object, Mock.Of<IWorldManager>().Object, Mock.Of<IZoneManager>().Object, Mock.Of<ITeamManager>().Object);
        manager.Initialize();

        mockTick.OnTick.WasCalled(Times.Once);
    }

    [Test]
    public async Task IsDungeonFull_AtMaxCapacity_ReturnsTrue()
    {
        var method = typeof(IndunManager).GetMethod("IsDungeonFull",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        var result = method!.Invoke(null, [1, 1u]);

        await Assert.That(result).IsEqualTo(true);
    }

    [Test]
    public async Task IsDungeonFull_BelowMaxCapacity_ReturnsFalse()
    {
        var method = typeof(IndunManager).GetMethod("IsDungeonFull",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        var result = method!.Invoke(null, [0, 1u]);

        await Assert.That(result).IsEqualTo(false);
    }

    [Test]
    public async Task RequestSystemInstance_ClosedSchedule_StopsBeforeWorldLookup()
    {
        var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc); // Monday
        var zone = new Zone { Id = 900, ZoneKey = 901, GroupId = 902 };
        var indun = new IndunZone { ZoneGroupId = zone.GroupId, InstanceCatalogId = 903, UseUtcEntranceTimes = true };
        indun.EntranceTimes.Add(new InstanceEntranceTime(2, 0, 0, 0, 0)); // Tuesday only

        var manager = CreateAdmissionManager(zone, indun);
        manager.AdmissionUtcNow = () => now;

        var accepted = manager.RequestSystemInstance(
            new Character(new UnitCustomModelParams()) { Id = 1, ObjId = 1, Name = "Closed" },
            zone.Id,
            0,
            out var dungeon);

        await Assert.That(accepted).IsFalse();
        await Assert.That(dungeon).IsNull();
    }

    [Test]
    public async Task RequestSystemInstance_ProhibitedBuffTag_StopsBeforeWorldLookup()
    {
        var zone = new Zone { Id = 910, ZoneKey = 911, GroupId = 912 };
        var indun = new IndunZone { ZoneGroupId = zone.GroupId, InstanceCatalogId = 913, UseUtcEntranceTimes = true };
        indun.PermissionTags.Add(new InstancePermissionTag(InstancePermissionTagKind.Buff, 5947));

        var manager = CreateAdmissionManager(zone, indun);
        manager.AdmissionTagMatcher = (_, kind, tagId) =>
            kind == InstancePermissionTagKind.Buff && tagId == 5947;

        var accepted = manager.RequestSystemInstance(
            new Character(new UnitCustomModelParams()) { Id = 2, ObjId = 2, Name = "Transformed" },
            zone.Id,
            0,
            out var dungeon);

        await Assert.That(accepted).IsFalse();
        await Assert.That(dungeon).IsNull();
    }

    [Test]
    public async Task RequestDungeonInstance_ClosedSchedule_RejectedWithoutCreatingInstance()
    {
        var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc); // Monday
        var zone = new Zone { Id = 920, ZoneKey = 921, GroupId = 922 };
        var indun = new IndunZone { ZoneGroupId = zone.GroupId, InstanceCatalogId = 923, UseUtcEntranceTimes = true };
        indun.EntranceTimes.Add(new InstanceEntranceTime(2, 0, 0, 0, 0)); // Tuesday only

        // A dungeon rejoin must stay reachable while the window is closed, so the schedule is checked at
        // VerifyDungeonEnterRequirements after the rejoin loop rather than before the copy lookup.
        var manager = CreateAdmissionManager(zone, indun, allowWorldLookup: true);
        manager.AdmissionUtcNow = () => now;

        var accepted = manager.RequestDungeonInstance(
            new Character(new UnitCustomModelParams()) { Id = 3, ObjId = 3, Name = "ClosedDungeon" },
            zone.Id,
            0);

        await Assert.That(accepted).IsFalse();
    }

    [Test]
    public async Task PreparedMatch_ChangedAdmissionStopsBeforeQueuePlayer()
    {
        var manager = new IndunMatchmakingManager();
        var character = new Character(new UnitCustomModelParams()) { Id = 4, ObjId = 4, Name = "BuffExpired" };
        var dungeonZone = new IndunZone { ZoneGroupId = 930, InstanceCatalogId = 931 };
        var queueCalls = 0;
        var order = new List<string>();
        manager.PreparedCanQueue = (_, _) => true;
        manager.AdmissionCheck = (_, _) => { order.Add("admission"); return false; };
        manager.PreparedQueuePlayer = (_, _) => { order.Add("queue"); queueCalls++; return true; };
        var method = typeof(IndunMatchmakingManager).GetMethod("TryEnterPreparedPlayer",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var admitted = (bool)method.Invoke(manager, [null, dungeonZone, character])!;

        await Assert.That(admitted).IsFalse();
        await Assert.That(queueCalls).IsEqualTo(0);
        await Assert.That(order.Count).IsEqualTo(1);
        await Assert.That(order[0]).IsEqualTo("admission");
    }

    private static IndunManager CreateAdmissionManager(
        Zone zone,
        IndunZone indun,
        bool allowWorldLookup = false)
    {
        var tick = Mock.Of<ITickManager>();
        var world = Mock.Of<IWorldManager>();
        world.GetWorldTemplateByZoneKey(zone.ZoneKey).Returns(new WorldTemplate { Id = 1, Name = "admission-test" });
        if (allowWorldLookup)
            world.GetWorlds().Returns([]);
        else
            world.GetWorlds().Throws(new InvalidOperationException("Admission rejection must happen before world lookup."));
        var zones = Mock.Of<IZoneManager>();
        zones.GetZoneById(zone.Id).Returns(zone);

        GetIndunZonesField().SetValue(IndunGameData.Instance, new Dictionary<uint, IndunZone> { [indun.ZoneGroupId] = indun });

        return new IndunManager(tick.Object, world.Object, zones.Object, Mock.Of<ITeamManager>().Object);
    }

    private static FieldInfo GetIndunZonesField() =>
        typeof(IndunGameData).GetField("_indunZones", BindingFlags.Instance | BindingFlags.NonPublic)!;
}
