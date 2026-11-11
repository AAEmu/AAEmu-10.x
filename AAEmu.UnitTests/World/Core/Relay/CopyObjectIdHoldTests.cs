using AAEmu.World.Core.Relay;

namespace AAEmu.UnitTests.World.Core.Relay;

public class CopyObjectIdHoldTests
{
    [Test]
    public async Task CopyIds_AreHeldUntilThatCopyCloses()
    {
        // Island copy 100 (zone 410): the zone kept removed crop spawner 899 and rejected Songsong reusing it.
        var hold = new CopyObjectIdHold();

        await Assert.That(hold.TryHold(410, 100, 899)).IsTrue();
        await Assert.That(hold.TryHold(410, 100, 901)).IsTrue();
        await Assert.That(hold.TryHold(410, 101, 950)).IsTrue();

        await Assert.That(hold.Release(410, 100)).IsEquivalentTo(new uint[] { 899, 901 });
        await Assert.That(hold.Release(410, 100)).IsEmpty();
        await Assert.That(hold.Release(410, 101)).IsEquivalentTo(new uint[] { 950 });
    }

    [Test]
    public async Task OpenWorldIds_AreNotHeld()
    {
        var hold = new CopyObjectIdHold();

        await Assert.That(hold.TryHold(129, 0, 500)).IsFalse();
        await Assert.That(hold.Release(129, 0)).IsEmpty();
    }
}
