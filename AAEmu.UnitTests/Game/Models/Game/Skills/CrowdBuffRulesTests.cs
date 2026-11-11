using AAEmu.Game.Models.Game.Faction;
using AAEmu.Game.Models.Game.Skills;

namespace AAEmu.UnitTests.Game.Models.Game.Skills;

public class CrowdBuffRulesTests
{
    // Trade-pack "heavy" 1454: crowd 1857 at 3 carriers within 5 m, no relation filter, checks itself.
    private static readonly CrowdCheck PackCarriers = new(CheckBuffId: 1454, CheckTagId: 0, Number: 3, Friendly: false, Hostile: false, CheckOwner: false);

    private static RelationState Friendly(string _) => RelationState.Friendly;

    [Test]
    public async Task CrowdReached_NeedsTheNumber()
    {
        await Assert.That(CrowdBuffRules.CrowdReached(1, 2)).IsFalse();
        await Assert.That(CrowdBuffRules.CrowdReached(2, 2)).IsTrue();
        await Assert.That(CrowdBuffRules.CrowdReached(5, 0)).IsFalse();
    }

    [Test]
    public async Task Check_DefaultsToTheSourceBuff_UnlessATagIsChecked()
    {
        await Assert.That(CrowdBuffRules.CheckBuffId(32721, 0, 0)).IsEqualTo(32721u);
        await Assert.That(CrowdBuffRules.CheckBuffId(32070, 32067, 0)).IsEqualTo(32067u);
        // Wolf 29567 checks tag 5377 only.
        await Assert.That(CrowdBuffRules.CheckBuffId(29567, 0, 5377)).IsEqualTo(0u);
    }

    [Test]
    public async Task PackCarriers_ThreeTogether_ThenOneWalksOff()
    {
        static bool Carries(string _) => true;

        await Assert.That(CrowdBuffRules.Reached("a", null, ["b", "c"], Carries, Friendly, PackCarriers)).IsTrue();
        await Assert.That(CrowdBuffRules.Reached("a", null, ["b"], Carries, Friendly, PackCarriers)).IsFalse();
    }

    [Test]
    public async Task NeighboursWithoutTheCheck_DoNotCount()
    {
        await Assert.That(CrowdBuffRules.Reached("a", null, ["b", "stranger"], u => u != "stranger", Friendly, PackCarriers)).IsFalse();
    }

    [Test]
    public async Task OwnerCheck_CountsOnlyTheOwner()
    {
        // Pet untargetable 18928: stealth 22095 while the owner, within 50 m, holds a stealth (tag 100) buff.
        var petStealth = new CrowdCheck(0, 100, 1, Friendly: true, Hostile: false, CheckOwner: true);

        // The pet itself holds tag 100 through 22095 but does not count.
        await Assert.That(CrowdBuffRules.Reached("pet", "owner", ["owner"], u => u == "pet", Friendly, petStealth)).IsFalse();
        await Assert.That(CrowdBuffRules.Reached("pet", "owner", ["owner"], _ => true, Friendly, petStealth)).IsTrue();
        await Assert.That(CrowdBuffRules.Reached("pet", "owner", [], _ => true, Friendly, petStealth)).IsFalse();
        await Assert.That(CrowdBuffRules.Reached("pet", null, ["someone"], _ => true, Friendly, petStealth)).IsFalse();
    }

    [Test]
    public async Task FriendlyOnly_IgnoresHostileNeighbours()
    {
        // Submarine formation 6789: 3 friendly holders within 10 m.
        var formation = new CrowdCheck(6789, 0, 3, Friendly: true, Hostile: false, CheckOwner: false);
        RelationState Relation(string u) => u == "enemy" ? RelationState.Hostile : RelationState.Friendly;

        await Assert.That(CrowdBuffRules.Reached("a", null, ["b", "enemy"], _ => true, Relation, formation)).IsFalse();
        await Assert.That(CrowdBuffRules.Reached("a", null, ["b", "c"], _ => true, Relation, formation)).IsTrue();
    }

    [Test]
    public async Task HostileOnly_DoesNotCountTheHolder()
    {
        // Provocative smell 24394: one hostile unit with tag 4095 within 5 m.
        var smell = new CrowdCheck(0, 4095, 1, Friendly: false, Hostile: true, CheckOwner: false);
        RelationState Relation(string u) => u == "enemy" ? RelationState.Hostile : RelationState.Friendly;

        await Assert.That(CrowdBuffRules.Reached("boar", null, [], _ => true, Relation, smell)).IsFalse();
        await Assert.That(CrowdBuffRules.Reached("boar", null, ["ally"], _ => true, Relation, smell)).IsFalse();
        await Assert.That(CrowdBuffRules.Reached("boar", null, ["enemy"], _ => true, Relation, smell)).IsTrue();
    }

    [Test]
    public async Task HoldsCheck_UsesTheBuffOrTheTag()
    {
        var wolf = new CrowdCheck(0, 5377, 2, false, false, false);

        await Assert.That(CrowdBuffRules.HoldsCheck(wolf, _ => false, tag => tag == 5377)).IsTrue();
        await Assert.That(CrowdBuffRules.HoldsCheck(PackCarriers, id => id == 1454, _ => false)).IsTrue();
        await Assert.That(CrowdBuffRules.HoldsCheck(PackCarriers, _ => false, _ => true)).IsFalse();
    }

    [Test]
    [Arguments(false, 0, true)]      // crowd gone, permanent crowd buff (1857): removed
    [Arguments(true, 0, false)]      // crowd still there
    [Arguments(false, 60000, false)] // timed crowd buff runs out on its own
    public async Task RemovesGranted_OnlyPermanentCrowdBuffsWhenTheCrowdIsGone(bool reached, int duration, bool expected)
    {
        await Assert.That(CrowdBuffRules.RemovesGranted(reached, duration)).IsEqualTo(expected);
    }
}
