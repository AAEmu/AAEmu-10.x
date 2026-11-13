using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Skills.Static;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class StaleDoodadTargetRulesTests
{
    [Test]
    public async Task NoTarget_OnADoodadTheServerNoLongerHas_IsStale()
    {
        // An expired Shining crop the client still showed.
        await Assert.That(StaleDoodadTargetRules.IsStale(SkillResult.NoTarget, 148613, doodadExists: false)).IsTrue();
    }

    [Test]
    public async Task NoTarget_OnALiveDoodad_IsNotStale()
    {
        await Assert.That(StaleDoodadTargetRules.IsStale(SkillResult.NoTarget, 148613, doodadExists: true)).IsFalse();
    }

    [Test]
    public async Task OtherFailures_AreNotStale()
    {
        await Assert.That(StaleDoodadTargetRules.IsStale(SkillResult.TooFarRange, 148613, doodadExists: false)).IsFalse();
    }

    [Test]
    public async Task NoDoodadTarget_IsNotStale()
    {
        await Assert.That(StaleDoodadTargetRules.IsStale(SkillResult.NoTarget, 0, doodadExists: false)).IsFalse();
    }
}
