using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

public class CrowdBuffsTests
{
    [Test]
    public async Task ASummonedNpc_ResolvesItsOwnerFromOwnerId()
    {
        var owner = new Character(new UnitCustomModelParams()) { Id = 42 };
        var summon = new Npc { OwnerId = 42 };

        var resolved = CrowdBuffs.ResolveOwner(summon, id => id == 42 ? owner : null);

        await Assert.That(resolved).IsSameReferenceAs(owner);
    }

    [Test]
    public async Task AnNpcWithoutAnOwner_ResolvesNone()
    {
        var resolved = CrowdBuffs.ResolveOwner(new Npc(), _ => throw new InvalidOperationException("no lookup expected"));

        await Assert.That(resolved).IsNull();
    }

    [Test]
    public async Task ACharacter_IsNotItsOwnOwner()
    {
        var character = new Character(new UnitCustomModelParams()) { Id = 7 };

        await Assert.That(CrowdBuffs.ResolveOwner(character, _ => character)).IsNull();
    }

    [Test]
    public async Task OneHolder_IsNeverCheckedByTwoThreadsAtOnce()
    {
        var holder = new Unit();
        var inside = 0;
        var overlapped = false;

        Parallel.For(0, 64, _ => CrowdBuffs.WithHolder(holder, _ =>
        {
            if (Interlocked.Increment(ref inside) > 1)
                overlapped = true;
            Thread.SpinWait(2000);
            Interlocked.Decrement(ref inside);
            return true;
        }));
        CrowdBuffs.WithHolder(holder, _ => false);

        await Assert.That(overlapped).IsFalse();
    }

    [Test]
    public async Task AHolderWithNothingLeft_IsDroppedAndTrackedAgainOnNextUse()
    {
        var holder = new Unit();

        CrowdBuffs.WithHolder(holder, _ => false);
        var dropped = !CrowdBuffs.IsTracked(holder);
        CrowdBuffs.WithHolder(holder, _ => true);
        var tracked = CrowdBuffs.IsTracked(holder);
        CrowdBuffs.WithHolder(holder, _ => false);

        await Assert.That(dropped).IsTrue();
        await Assert.That(tracked).IsTrue();
    }
}
