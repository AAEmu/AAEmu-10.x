using AAEmu.Game.Models.Game.DoodadObj;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadStayOnPhaseRulesTests
{
    private const string Use = "DoodadFuncUse";

    [Test]
    public async Task ChoiceOption_NextToAnAdvancingUse_KeepsTheDoodad()
    {
        // Trembling Cactus 18171 group 53045: Kill the Bugs! (-1), Defeat the Stonecrabs! (-1), Help the Cactus! (53047).
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad(Use, -1, [(Use, -1), (Use, -1), (Use, 53047)], false)).IsTrue();
        // Contaminated Well 5061: Draw Water (-1), Purify the Water (13168).
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad(Use, -1, [(Use, -1), (Use, 13168)], false)).IsTrue();
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad(Use, -1, [(Use, -1), ("DoodadFuncConditionalUse", 7)], false)).IsTrue();
    }

    [Test]
    public async Task OnlyOutcome_IsConsumed()
    {
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad(Use, -1, [(Use, -1)], false)).IsFalse();
    }

    [Test]
    public async Task PickingUp_IsConsumedEvenNextToAnAdvancingFunc()
    {
        // Whirligear Machine 2234: RecoverItem (-1) beside FakeUse (4310).
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad("DoodadFuncRecoverItem", -1, [("DoodadFuncFakeUse", 4310), ("DoodadFuncRecoverItem", -1)], false)).IsFalse();
    }

    [Test]
    public async Task UseNextToANonChoiceAdvance_IsConsumed()
    {
        // Moonlight vein group 28539: Mine (-1) beside a SkillHit (28547); mining must still deplete the vein.
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad(Use, -1, [(Use, -1), ("DoodadFuncSkillHit", 28547)], false)).IsFalse();
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad(Use, -1, [(Use, -1), ("DoodadFuncInsertCounter", 5)], false)).IsFalse();
    }

    [Test]
    public async Task ItemGrantingChoice_IsConsumed()
    {
        // Family doodad group 33309: Obtain Wish Tree Log (-1, gain_item) beside an advancing use (32926).
        await Assert.That(DoodadStayOnPhaseRules.KeepsDoodad(Use, -1, [(Use, -1), (Use, 32926)], true)).IsFalse();
    }
}
