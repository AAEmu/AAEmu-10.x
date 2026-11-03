using AAEmu.Game.Models.Game.Quests;
using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.UnitTests.Game.Models.Game.Quests;

public class QuestCinemaProgressRulesTests
{
    [Test]
    public async Task Replay_OnlyUnwatchedProgressCinema()
    {
        await Assert.That(QuestCinemaProgressRules.ShouldReplayOnWorldEntry(
            QuestComponentKind.Progress, 163, 0)).IsTrue();
        await Assert.That(QuestCinemaProgressRules.ShouldReplayOnWorldEntry(
            QuestComponentKind.Progress, 163, 1)).IsFalse();
        await Assert.That(QuestCinemaProgressRules.ShouldReplayOnWorldEntry(
            QuestComponentKind.Progress, 0, 0)).IsFalse();
        await Assert.That(QuestCinemaProgressRules.ShouldReplayOnWorldEntry(
            QuestComponentKind.Start, 163, 0)).IsFalse();
    }

    [Test]
    public async Task Credit_WaitsForWorldEntry()
    {
        await Assert.That(QuestCinemaProgressRules.ShouldCreditCinemaStarted(false)).IsFalse();
        await Assert.That(QuestCinemaProgressRules.ShouldCreditCinemaStarted(true)).IsTrue();
    }
}
