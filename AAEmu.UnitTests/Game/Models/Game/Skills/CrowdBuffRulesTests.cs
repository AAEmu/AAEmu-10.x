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

    [Test]
    public async Task HoldersApart_ThenWalkingTogether_ReachTheCrowdOnTheRecheck()
    {
        // Both hold role check 32721 (crowd_number 2) but got it out of range of each other.
        static bool Holds(string unit) => true;

        var whenApart = CrowdBuffRules.Recipients("a", [], Holds, crowdNumber: 2);
        var afterWalkingTogether = CrowdBuffRules.Recipients("a", ["b"], Holds, crowdNumber: 2);

        await Assert.That(whenApart).IsEmpty();
        await Assert.That(afterWalkingTogether).IsEquivalentTo(["a", "b"]);
    }

    [Test]
    public async Task Recipients_CountOnlyNeighboursHoldingTheCheckBuff()
    {
        var recipients = CrowdBuffRules.Recipients("a", ["b", "stranger"], unit => unit != "stranger", crowdNumber: 3);

        await Assert.That(recipients).IsEmpty();
    }

    [Test]
    [Arguments(32722u, false, true)] // crowd buff not yet granted: check again
    [Arguments(32722u, true, false)] // already granted
    [Arguments(0u, false, false)]    // not a crowd source
    public async Task NeedsRecheck_OnlyForCrowdSourcesStillWaiting(uint crowdBuffId, bool holdsCrowdBuff, bool expected)
    {
        await Assert.That(CrowdBuffRules.NeedsRecheck(crowdBuffId, holdsCrowdBuff)).IsEqualTo(expected);
    }
}
