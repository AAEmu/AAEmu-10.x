using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class SkillUseScheduleRulesTests
{
    [Test]
    public async Task Immediate_WhenDelayIsZeroOrNegative()
    {
        await Assert.That(SkillUseScheduleRules.ShouldExecuteImmediately(0)).IsTrue();
        await Assert.That(SkillUseScheduleRules.ShouldExecuteImmediately(-1)).IsTrue();
        await Assert.That(SkillUseScheduleRules.ShouldExecuteImmediately(1)).IsFalse();
        await Assert.That(SkillUseScheduleRules.ShouldExecuteImmediately(50)).IsFalse();
    }
}
