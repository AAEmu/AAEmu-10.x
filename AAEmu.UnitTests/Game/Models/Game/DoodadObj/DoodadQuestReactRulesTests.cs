using AAEmu.Game.Models.Game.DoodadObj;
using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.UnitTests.Game.Models.Game.DoodadObj;

public class DoodadQuestReactRulesTests
{
    [Test]
    public async Task CompletedRow_MatchesCompletedOnly()
    {
        await Assert.That(DoodadQuestReactRules.MatchesStatus(5, QuestStatus.Completed)).IsTrue();
        await Assert.That(DoodadQuestReactRules.MatchesStatus(5, QuestStatus.Progress)).IsFalse();
        await Assert.That(DoodadQuestReactRules.MatchesStatus(5, QuestStatus.Invalid)).IsFalse();
    }

    [Test]
    public async Task ProgressRow_MatchesProgress()
    {
        await Assert.That(DoodadQuestReactRules.MatchesStatus(1, QuestStatus.Progress)).IsTrue();
        await Assert.That(DoodadQuestReactRules.MatchesStatus(1, QuestStatus.Completed)).IsFalse();
    }

    [Test]
    public async Task ZeroComponent_AcceptsAny()
    {
        await Assert.That(DoodadQuestReactRules.MatchesComponent(0, 0, false)).IsTrue();
        await Assert.That(DoodadQuestReactRules.MatchesComponent(0, 10824, false)).IsTrue();
    }

    [Test]
    public async Task NamedComponent_NeedsExactOrReadyStep()
    {
        await Assert.That(DoodadQuestReactRules.MatchesComponent(39998, 39998, false)).IsTrue();
        await Assert.That(DoodadQuestReactRules.MatchesComponent(39998, 0, false)).IsFalse();
        await Assert.That(DoodadQuestReactRules.MatchesComponent(39998, 0, true)).IsTrue();
    }

    [Test]
    public async Task ShouldAdvance_IgnoresMissingAndSelf()
    {
        await Assert.That(DoodadQuestReactRules.ShouldAdvance(41731, 41730)).IsTrue();
        await Assert.That(DoodadQuestReactRules.ShouldAdvance(41731, 41731)).IsFalse();
        await Assert.That(DoodadQuestReactRules.ShouldAdvance(-1, 41730)).IsFalse();
        await Assert.That(DoodadQuestReactRules.ShouldAdvance(0, 41730)).IsFalse();
    }

    [Test]
    public async Task QuestReact_DoesNotMoveTheSharedPhase()
    {
        await Assert.That(DoodadQuestReactRules.ShouldMutateSharedPhase()).IsFalse();
        await Assert.That(DoodadQuestReactRules.ShouldKeepViewerPhase(41880, 41881)).IsTrue();
        await Assert.That(DoodadQuestReactRules.ShouldKeepViewerPhase(41880, 41880)).IsFalse();
        await Assert.That(DoodadQuestReactRules.NextViewerPhase(41880, 41881)).IsEqualTo(41881u);
        await Assert.That(DoodadQuestReactRules.NextViewerPhase(41880, 41880)).IsEqualTo(41880u);
    }

    [Test]
    public async Task SharedPhaseChange_DropsViewerOverlays()
    {
        await Assert.That(DoodadQuestReactRules.ShouldInvalidateViewerPhases(41880, 41882)).IsTrue();
        await Assert.That(DoodadQuestReactRules.ShouldInvalidateViewerPhases(41880, 41880)).IsFalse();
    }

    [Test]
    public async Task FinalUse_InAPhaseAnotherFuncMovesOn_StaysForTheNextUse()
    {
        // Ironwood family tree 11541, phase 32889: three role uses (-1) and "Start a Family Quest" (-> 33308).
        await Assert.That(DoodadQuestReactRules.StaysForTheNextUse([-1, -1, -1, 33308])).IsTrue();
    }

    [Test]
    public async Task FinalUse_InAPhaseNothingMovesOn_IsConsumed()
    {
        await Assert.That(DoodadQuestReactRules.StaysForTheNextUse([-1])).IsFalse();
        await Assert.That(DoodadQuestReactRules.StaysForTheNextUse([-1, -1])).IsFalse();
        await Assert.That(DoodadQuestReactRules.StaysForTheNextUse([0, -1])).IsFalse();
    }

    [Test]
    public async Task FinalUse_WithNoPhaseFuncsKnown_IsConsumed()
    {
        await Assert.That(DoodadQuestReactRules.StaysForTheNextUse([])).IsFalse();
        await Assert.That(DoodadQuestReactRules.StaysForTheNextUse(null)).IsFalse();
    }
}
