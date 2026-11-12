using AAEmu.Game.Models.Game.Indun;

namespace AAEmu.UnitTests.Game.Models.Game.Indun;

public class IndunTicketPurchaseTests
{
    [Test]
    public async Task TwoRequestsForTheLastPass_PayOnlyOnce()
    {
        // enter_count 1: one pass may be registered today; two packets arrive together.
        var purchase = new IndunTicketPurchase();
        var registered = 0;
        var paid = 0;
        using var bothChecked = new Barrier(2);

        bool Buy() => purchase.Run(characterId: 7,
            canBuy: () => Volatile.Read(ref registered) < 1,
            store: () => true,
            pay: () =>
            {
                bothChecked.SignalAndWait(TimeSpan.FromMilliseconds(200));
                Interlocked.Increment(ref paid);
                return true;
            },
            unstore: () => { },
            register: () => Interlocked.Increment(ref registered));

        var results = await Task.WhenAll(Task.Run(Buy), Task.Run(Buy));

        await Assert.That(paid).IsEqualTo(1);
        await Assert.That(registered).IsEqualTo(1);
        await Assert.That(results.Count(ok => ok)).IsEqualTo(1);
    }

    [Test]
    public async Task AFailedPayment_RegistersNothing()
    {
        var purchase = new IndunTicketPurchase();
        var registered = false;

        var ok = purchase.Run(7, () => true, () => true, () => false, () => { }, () => registered = true);

        await Assert.That(ok).IsFalse();
        await Assert.That(registered).IsFalse();
    }

    [Test]
    public async Task AFailedPayment_RemovesTheStoredPass()
    {
        var purchase = new IndunTicketPurchase();
        var stored = false;

        purchase.Run(7, () => true, () => stored = true, () => false, () => stored = false, () => { });

        await Assert.That(stored).IsFalse();
    }

    [Test]
    public async Task APaymentThatThrows_RemovesTheStoredPass()
    {
        var purchase = new IndunTicketPurchase();
        var stored = false;

        Assert.Throws<InvalidOperationException>(() => purchase.Run(
            7, () => true, () => stored = true, () => throw new InvalidOperationException("boom"), () => stored = false, () => { }));

        await Assert.That(stored).IsFalse();
    }

    [Test]
    public async Task AFailedStore_TakesNoPayment()
    {
        var purchase = new IndunTicketPurchase();
        var paid = false;

        var ok = purchase.Run(7, () => true, () => false, () => paid = true, () => { }, () => { });

        await Assert.That(ok).IsFalse();
        await Assert.That(paid).IsFalse();
    }
}
