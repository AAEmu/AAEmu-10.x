using AAEmu.Game.Core.Managers;
using AAEmu.Game.Models.Game.Schedules;

namespace AAEmu.UnitTests.Game.Models.Game.Schedules;

public class GameSchedulePeriodRulesTests
{
    [Test]
    public async Task Aggregate_Empty_IsNotFound()
    {
        await Assert.That(GameSchedulePeriodRules.Aggregate([]))
            .IsEqualTo(GameScheduleManager.PeriodStatus.NotFound);
    }

    [Test]
    public async Task Aggregate_AnyInProgress_Wins()
    {
        await Assert.That(GameSchedulePeriodRules.Aggregate(
        [
            GameSchedulePeriodRules.WindowKind.Ended,
            GameSchedulePeriodRules.WindowKind.InProgress,
            GameSchedulePeriodRules.WindowKind.NotStarted,
        ])).IsEqualTo(GameScheduleManager.PeriodStatus.InProgress);
    }

    [Test]
    public async Task Aggregate_PendingWindow_BeatsStaleEnded()
    {
        // Christmas 16708: older years Ended, next year NotStarted → must arm cron, not skip.
        await Assert.That(GameSchedulePeriodRules.Aggregate(
        [
            GameSchedulePeriodRules.WindowKind.Ended,
            GameSchedulePeriodRules.WindowKind.Ended,
            GameSchedulePeriodRules.WindowKind.NotStarted,
        ])).IsEqualTo(GameScheduleManager.PeriodStatus.NotStarted);
    }

    [Test]
    public async Task Aggregate_AllEnded_IsEnded()
    {
        await Assert.That(GameSchedulePeriodRules.Aggregate(
        [
            GameSchedulePeriodRules.WindowKind.Ended,
            GameSchedulePeriodRules.WindowKind.Ended,
        ])).IsEqualTo(GameScheduleManager.PeriodStatus.Ended);
    }

    [Test]
    public async Task Aggregate_OnlyNotStarted()
    {
        await Assert.That(GameSchedulePeriodRules.Aggregate(
        [
            GameSchedulePeriodRules.WindowKind.NotStarted,
        ])).IsEqualTo(GameScheduleManager.PeriodStatus.NotStarted);
    }

    [Test]
    public async Task Classify_StartedEndedSides()
    {
        await Assert.That(GameSchedulePeriodRules.Classify(started: true, ended: false))
            .IsEqualTo(GameSchedulePeriodRules.WindowKind.InProgress);
        await Assert.That(GameSchedulePeriodRules.Classify(started: false, ended: true))
            .IsEqualTo(GameSchedulePeriodRules.WindowKind.Ended);
        await Assert.That(GameSchedulePeriodRules.Classify(started: false, ended: false))
            .IsEqualTo(GameSchedulePeriodRules.WindowKind.NotStarted);
        // started+ended should not happen for a coherent window; treat as Ended.
        await Assert.That(GameSchedulePeriodRules.Classify(started: true, ended: true))
            .IsEqualTo(GameSchedulePeriodRules.WindowKind.Ended);
    }
}
