using AAEmu.Game.Models.Game.Skills.Effects;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects;

public class WorldMessageAudienceTests
{
    [Test]
    public async Task ZoneGroupOnly_ReachesEveryoneInTheCastersZoneGroup()
    {
        // Island timer message 1418: zone group 158, whoever is in the copy, however far from the timer NPC.
        var players = new[] { (Id: 1u, Group: 158u), (Id: 2u, Group: 158u), (Id: 3u, Group: 1u) };

        var audience = WorldMessageAudience.ZoneGroupMembers(players, 158, p => p.Group).Select(p => p.Id);

        await Assert.That(audience).IsEquivalentTo(new[] { 1u, 2u });
    }

    [Test]
    public async Task NoCasterZoneGroup_ReachesNobody()
    {
        var players = new[] { (Id: 1u, Group: 0u) };

        await Assert.That(WorldMessageAudience.ZoneGroupMembers(players, 0, p => p.Group)).IsEmpty();
    }
}
