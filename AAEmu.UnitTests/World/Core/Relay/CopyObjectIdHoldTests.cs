using AAEmu.World.Core.Relay;

namespace AAEmu.UnitTests.World.Core.Relay;

public class CopyObjectIdHoldTests
{
    [Test]
    public async Task CopyIds_AreHeldUntilThatCopyCloses()
    {
        // Island copy 100 (zone 410): the zone kept removed crop spawner 899 and rejected Songsong reusing it.
        var hold = new CopyObjectIdHold(_ => true);

        await Assert.That(hold.TryHold(410, 100, 899)).IsTrue();
        await Assert.That(hold.TryHold(410, 100, 901)).IsTrue();
        await Assert.That(hold.TryHold(410, 101, 950)).IsTrue();

        await Assert.That(hold.Release(410, 100)).IsEquivalentTo(new uint[] { 899, 901 });
        await Assert.That(hold.Release(410, 100)).IsEmpty();
        await Assert.That(hold.Release(410, 101)).IsEquivalentTo(new uint[] { 950 });
    }

    [Test]
    public async Task ALateRemoval_AfterTheCopyClosed_IsNotHeld()
    {
        // A delayed remove can land after the disconnect drained the copy; nothing would drain a new hold.
        var hold = new CopyObjectIdHold(_ => true);
        hold.TryHold(410, 100, 899);
        hold.Release(410, 100);

        await Assert.That(hold.TryHold(410, 100, 901)).IsFalse();
        await Assert.That(hold.Release(410, 100)).IsEmpty();
    }

    [Test]
    public async Task AnIdThatIsLiveAgain_IsNoLongerHeld()
    {
        // A removed caster that is rematerialized owns its id again; the copy closing must not free it twice.
        var hold = new CopyObjectIdHold(_ => true);
        hold.TryHold(410, 100, 899);
        hold.TryHold(410, 100, 901);

        hold.Unhold(410, 100, 899);

        await Assert.That(hold.Release(410, 100)).IsEquivalentTo(new uint[] { 901 });
    }

    [Test]
    public async Task OpenWorldIds_AreNotHeld()
    {
        var hold = new CopyObjectIdHold(_ => true);

        await Assert.That(hold.TryHold(129, 0, 500)).IsFalse();
        await Assert.That(hold.Release(129, 0)).IsEmpty();
    }

    [Test]
    public async Task SystemInstanceIds_AreNotHeld()
    {
        // arche_mall_world (system instance 1) runs on a shared host the supervisor never started; it lives until
        // shutdown, so its ids must go back to the pool as usual.
        var hold = new CopyObjectIdHold(instanceId => instanceId == 100);

        await Assert.That(hold.TryHold(410, 1, 700)).IsFalse();
        await Assert.That(hold.TryHold(410, 100, 701)).IsTrue();
    }
}
