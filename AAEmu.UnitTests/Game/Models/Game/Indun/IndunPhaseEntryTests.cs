using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Models.Game.Indun;

public class IndunPhaseEntryTests
{
    [Test]
    public async Task WalkingTheSamePhaseAgain_IsNotANewEntry()
    {
        var entries = new DoodadPhaseEntries();

        await Assert.That(entries.IsNewEntry(52619)).IsTrue();
        await Assert.That(entries.IsNewEntry(52619)).IsFalse();
    }

    [Test]
    public async Task ReturningToAPhaseAfterAnotherOne_IsANewEntryAgain()
    {
        // A crop: ripe -> harvested -> ripe again, each harvest scores once.
        var entries = new DoodadPhaseEntries();

        entries.IsNewEntry(52619);
        entries.IsNewEntry(52620);

        await Assert.That(entries.IsNewEntry(52619)).IsTrue();
    }
}
