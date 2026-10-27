using AAEmu.Game.Models.Game.DoodadObj;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadPhaseAreaTriggerRulesTests
{
    [Test]
    public async Task Clear_WhenAnyTriggerAttached()
    {
        await Assert.That(DoodadPhaseAreaTriggerRules.ShouldClearAttachedTriggers(0)).IsFalse();
        await Assert.That(DoodadPhaseAreaTriggerRules.ShouldClearAttachedTriggers(1)).IsTrue();
        await Assert.That(DoodadPhaseAreaTriggerRules.ShouldClearAttachedTriggers(3)).IsTrue();
    }
}
