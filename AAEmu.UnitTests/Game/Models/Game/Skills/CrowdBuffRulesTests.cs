using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class CrowdBuffRulesTests
{
    [Test]
    public async Task IslandRoleCheck_TwoHolders_GrantReadyToHelp()
    {
        // Buff 32721 (role check): crowd_number 2, crowd_buff 32722 "Ready to Help!" within 100 m.
        await Assert.That(CrowdBuffRules.CrowdReached(1, 2)).IsFalse();
        await Assert.That(CrowdBuffRules.CrowdReached(2, 2)).IsTrue();
        await Assert.That(CrowdBuffRules.CrowdReached(4, 2)).IsTrue();
    }

    [Test]
    public async Task NoCrowdNumber_NeverGrants()
    {
        await Assert.That(CrowdBuffRules.CrowdReached(5, 0)).IsFalse();
    }

    [Test]
    public async Task CheckBuff_DefaultsToTheBuffItself()
    {
        await Assert.That(CrowdBuffRules.CheckBuffId(32721, 0)).IsEqualTo(32721u);
        await Assert.That(CrowdBuffRules.CheckBuffId(32721, 32700)).IsEqualTo(32700u);
    }
}
