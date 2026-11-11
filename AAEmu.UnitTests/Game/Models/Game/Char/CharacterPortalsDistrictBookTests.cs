using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.UnitTests.Utils.Mocks;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class CharacterPortalsDistrictBookTests
{
    private const uint SubZoneId = 7;
    private const uint DistrictId = 10;
    private const uint ReturnPointId = 77;

    private static (CharacterPortals Portals, Portal Entry) CreateBookWithOneDistrict()
    {
        var owner = new CharacterMock();
        var portals = new CharacterPortals(owner);
        owner.Portals = portals;
        portals.RestoreVisitedDistrictRecord(new VisitedDistrict { Id = 1, SubZone = SubZoneId, Owner = owner.Id });
        var entry = new Portal { Id = DistrictId, Type = ReturnPointId, Name = "district", SubZoneId = SubZoneId };
        portals.DistrictPortals[DistrictId] = entry;
        return (portals, entry);
    }

    [Test]
    public async Task Rebuild_PublishesAReplacementBook_AndLeavesTheOneAReaderHoldsWhole()
    {
        // Portal casts and deletes read the district book without the book lock, while the
        // return-district tick rebuilds it. A rebuild must not change a book already handed out.
        var (portals, entry) = CreateBookWithOneDistrict();
        var held = portals.DistrictPortals;
        using var reading = ((IEnumerable<Portal>)held.Values).GetEnumerator();

        portals.RemoveFromBookPortal(entry, false);

        await Assert.That(ReferenceEquals(held, portals.DistrictPortals)).IsFalse();
        await Assert.That(held.Count).IsEqualTo(1);
        await Assert.That(ReferenceEquals(held[DistrictId], entry)).IsTrue();
        await Assert.That(reading.MoveNext()).IsTrue();
        await Assert.That(ReferenceEquals(reading.Current, entry)).IsTrue();
    }

    [Test]
    public async Task Rebuild_IsWhatLaterLookupsAnswerFrom()
    {
        var (portals, entry) = CreateBookWithOneDistrict();
        await Assert.That(ReferenceEquals(portals.GetDistrictPortalInfo(DistrictId), entry)).IsTrue();
        await Assert.That(ReferenceEquals(portals.GetDistrictPortalInfo(ReturnPointId), entry)).IsTrue();

        portals.RemoveFromBookPortal(entry, false);

        await Assert.That(portals.DistrictPortals.Count).IsEqualTo(0);
        await Assert.That(portals.GetDistrictPortalInfo(DistrictId)).IsNull();
        await Assert.That(portals.GetDistrictPortalInfo(ReturnPointId)).IsNull();
        await Assert.That(portals.HasVisitedDistrictRecord(SubZoneId)).IsFalse();
    }
}
