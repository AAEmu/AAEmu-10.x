using AAEmu.Game.Models.Game.Quests;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestStarterSphereRulesTests
{
    [Test]
    public async Task Enter_WaitsForWorldEntry()
    {
        await Assert.That(QuestStarterSphereRules.ShouldAcceptOnEnter(false, false, true)).IsFalse();
        await Assert.That(QuestStarterSphereRules.ShouldAcceptOnEnter(true, false, true)).IsTrue();
        await Assert.That(QuestStarterSphereRules.ShouldAcceptOnEnter(true, true, true)).IsFalse();
        await Assert.That(QuestStarterSphereRules.ShouldAcceptOnEnter(true, false, false)).IsFalse();
    }

    [Test]
    public async Task LastPosition_StaysOutsideUntilEntryCompletes()
    {
        await Assert.That(QuestStarterSphereRules.ShouldUpdateLastPosition(false, false)).IsFalse();
        await Assert.That(QuestStarterSphereRules.ShouldUpdateLastPosition(false, true)).IsTrue();
        await Assert.That(QuestStarterSphereRules.ShouldUpdateLastPosition(true, false)).IsTrue();
    }

    [Test]
    public async Task QuestAreaAndClientSphere_WaitForWorldEntry()
    {
        await Assert.That(QuestStarterSphereRules.ShouldReconcileQuestArea(false)).IsFalse();
        await Assert.That(QuestStarterSphereRules.ShouldReconcileQuestArea(true)).IsTrue();
        await Assert.That(QuestStarterSphereRules.ShouldAcceptClientSphere(false)).IsFalse();
        await Assert.That(QuestStarterSphereRules.ShouldAcceptClientSphere(true)).IsTrue();
    }
}
