using AAEmu.Game.Models.Game.Skills.Effects;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects;

public class KillNpcWithoutCorpseDeferRulesTests
{
    [Test]
    public async Task Defer_WhenVictimStillAlive()
    {
        await Assert.That(KillNpcWithoutCorpseDeferRules.ShouldDeferRemoval(victimAlive: true)).IsTrue();
        await Assert.That(KillNpcWithoutCorpseDeferRules.ShouldDeferRemoval(victimAlive: false)).IsFalse();
        await Assert.That(KillNpcWithoutCorpseDeferRules.DeferRemovalMs).IsGreaterThan(0);
    }
}
