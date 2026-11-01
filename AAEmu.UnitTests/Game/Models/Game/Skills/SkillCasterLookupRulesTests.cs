using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class SkillCasterLookupRulesTests
{
    [Test]
    public async Task PreferHeld_WhenLookupMissesSameObjId()
    {
        await Assert.That(SkillCasterLookupRules.PreferHeldCasterWhenLookupMisses(10, 10, lookupFound: false))
            .IsTrue();
        await Assert.That(SkillCasterLookupRules.PreferHeldCasterWhenLookupMisses(10, 10, lookupFound: true))
            .IsFalse();
        await Assert.That(SkillCasterLookupRules.PreferHeldCasterWhenLookupMisses(10, 11, lookupFound: false))
            .IsFalse();
        await Assert.That(SkillCasterLookupRules.PreferHeldCasterWhenLookupMisses(0, 10, lookupFound: false))
            .IsFalse();
    }
}
