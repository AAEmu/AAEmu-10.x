using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Models.Game.Indun;

public class IndunCloseWarningRulesTests
{
    private static readonly int[] Thresholds = [180, 60, 10];

    [Test]
    public async Task EachThreshold_IsAnnouncedOnceOnTheWayDown()
    {
        // Island end window: 181 s left right after the run ends.
        await Assert.That(IndunCloseWarningRules.Next(181, int.MaxValue, Thresholds)).IsNull();
        await Assert.That(IndunCloseWarningRules.Next(180, int.MaxValue, Thresholds)).IsEqualTo(180);
        await Assert.That(IndunCloseWarningRules.Next(179, 180, Thresholds)).IsNull();
        await Assert.That(IndunCloseWarningRules.Next(60, 180, Thresholds)).IsEqualTo(60);
        await Assert.That(IndunCloseWarningRules.Next(10, 60, Thresholds)).IsEqualTo(10);
        await Assert.That(IndunCloseWarningRules.Next(3, 10, Thresholds)).IsNull();
    }

    [Test]
    public async Task ASkippedTick_AnnouncesOnlyTheNearest()
    {
        await Assert.That(IndunCloseWarningRules.Next(55, int.MaxValue, Thresholds)).IsEqualTo(60);
    }

    [Test]
    public async Task Message_ReadsInMinutesOrSeconds()
    {
        await Assert.That(IndunCloseWarningRules.Message(180)).IsEqualTo("This dungeon closes in 3 minutes.");
        await Assert.That(IndunCloseWarningRules.Message(60)).IsEqualTo("This dungeon closes in 1 minute.");
        await Assert.That(IndunCloseWarningRules.Message(10)).IsEqualTo("This dungeon closes in 10 seconds.");
    }
}
