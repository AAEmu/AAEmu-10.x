using AAEmu.Game.Models.Game.DoodadObj;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadStayOnPhaseRulesTests
{
    [Test]
    public async Task ChoiceOption_NextToAnAdvancingOne_KeepsTheDoodad()
    {
        // Trembling Cactus 18171 group 53045: Kill the Bugs! (-1), Defeat the Stonecrabs! (-1), Help the Cactus! (53047).
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad("DoodadFuncUse", -1, [-1, -1, 53047])).IsTrue();
        // Contaminated Well 5061: Draw Water (-1), Purify the Water (13168).
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad("DoodadFuncUse", -1, [-1, 13168])).IsTrue();
    }

    [Test]
    public async Task OnlyOutcome_IsConsumed()
    {
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad("DoodadFuncUse", -1, [-1])).IsFalse();
    }

    [Test]
    public async Task PickingUp_IsConsumedEvenNextToAnAdvancingFunc()
    {
        // Whirligear Machine 2234: RecoverItem (-1) beside FakeUse (4310).
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad("DoodadFuncRecoverItem", -1, [4310, -1])).IsFalse();
    }
}
