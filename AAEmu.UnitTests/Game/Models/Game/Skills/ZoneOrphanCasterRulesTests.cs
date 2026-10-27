using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class ZoneOrphanCasterRulesTests
{
    [Test]
    public async Task ShouldRematerialize_OnlyWhenCasterMissingAndRemovalCached()
    {
        await Assert.That(ZoneOrphanCasterRules.ShouldRematerialize(casterMissing: true, hasRecentRemoval: true))
            .IsTrue();
        await Assert.That(ZoneOrphanCasterRules.ShouldRematerialize(casterMissing: true, hasRecentRemoval: false))
            .IsFalse();
        await Assert.That(ZoneOrphanCasterRules.ShouldRematerialize(casterMissing: false, hasRecentRemoval: true))
            .IsFalse();
        await Assert.That(ZoneOrphanCasterRules.ShouldRematerialize(casterMissing: false, hasRecentRemoval: false))
            .IsFalse();
    }
}
