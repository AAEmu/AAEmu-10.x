using AAEmu.Game.Models.Game.NPChar;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

public class NpcSpawnerEventRouteRulesTests
{
    [Test]
    public async Task Resolve_PrefersZonesThatOwnThePlacement()
    {
        // Abyssal seed casts from Silent Sea 213; Marianople recruiter placement 140800 is on 183.
        var byZone = new Dictionary<uint, IReadOnlyCollection<uint>>
        {
            [213] = [16859, 16864],
            [183] = [140800],
            [191] = [140851, 140833],
        };

        var targets = NpcSpawnerEventRouteRules.ResolveTargetZones(
            placementId: 140800,
            creatorZoneId: 213,
            byZone);

        await Assert.That(targets.Count).IsEqualTo(1);
        await Assert.That(targets[0]).IsEqualTo(183u);
    }

    [Test]
    public async Task Resolve_FallsBackToCreatorWhenPlacementUnknown()
    {
        var byZone = new Dictionary<uint, IReadOnlyCollection<uint>>
        {
            [213] = [16859],
        };

        var targets = NpcSpawnerEventRouteRules.ResolveTargetZones(
            placementId: 999999,
            creatorZoneId: 213,
            byZone);

        await Assert.That(targets.Count).IsEqualTo(1);
        await Assert.That(targets[0]).IsEqualTo(213u);
    }

    [Test]
    public async Task Resolve_CanHitMultipleOwnerZones()
    {
        var byZone = new Dictionary<uint, IReadOnlyCollection<uint>>
        {
            [183] = [140800],
            [186] = [140800],
        };

        var targets = NpcSpawnerEventRouteRules.ResolveTargetZones(140800, creatorZoneId: 213, byZone);
        await Assert.That(targets.Count).IsEqualTo(2);
        await Assert.That(targets.Contains(183u)).IsTrue();
        await Assert.That(targets.Contains(186u)).IsTrue();
    }
}
