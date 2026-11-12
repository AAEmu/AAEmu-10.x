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
}
