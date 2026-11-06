using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units;
using AAEmu.UnitTests.Utils.Mocks;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

public class TaggingKillingBlowTests
{
    private sealed class Victim : Slave
    {
        public override int MaxHp => 2900;
    }

    [Test]
    public async Task OneShotCannonKill_TagsTheGunner()
    {
        // Quest 1401: an Andelph cannon (ownerless slave 609) kills a golem (npc 2301) with its first
        // shot, so DamageEffect never got to record a hit before the golem died.
        var golem = new Victim { ObjId = 3407 };
        var gunner = new CharacterMock { ObjId = 2135 };
        var cannon = new Slave { ObjId = 984 };
        cannon.AttachedCharacters.Add(AttachPointKind.Driver, gunner);
        var tagging = new Tagging(golem);

        await Assert.That(Tagging.KillingBlowTags(tagging.Tagger != null, tagging.TagTeam, cannon, golem)).IsTrue();
        tagging.AddTagger(cannon, golem.MaxHp);

        await Assert.That(tagging.Tagger).IsSameReferenceAs(gunner);
    }

    [Test]
    public async Task OneShotKillByACharacter_TagsTheCharacter()
    {
        var mob = new Victim { ObjId = 10 };
        var player = new CharacterMock { ObjId = 11 };
        var tagging = new Tagging(mob);

        tagging.AddTagger(player, mob.MaxHp);

        await Assert.That(tagging.Tagger).IsSameReferenceAs(player);
    }

    [Test]
    public async Task EmptyCannon_TagsNobody()
    {
        var golem = new Victim { ObjId = 3407 };
        var tagging = new Tagging(golem);

        tagging.AddTagger(new Slave { ObjId = 984 }, golem.MaxHp);

        await Assert.That(tagging.Tagger).IsNull();
    }

    [Test]
    public async Task AlreadyTagged_KeepsItsTagger()
    {
        var golem = new Victim { ObjId = 3407 };

        await Assert.That(Tagging.KillingBlowTags(true, 0, new CharacterMock { ObjId = 1 }, golem)).IsFalse();
        await Assert.That(Tagging.KillingBlowTags(false, 7, new CharacterMock { ObjId = 1 }, golem)).IsFalse();
    }

    [Test]
    public async Task OwnDeath_DoesNotTag()
    {
        var golem = new Victim { ObjId = 3407 };

        await Assert.That(Tagging.KillingBlowTags(false, 0, golem, golem)).IsFalse();
        await Assert.That(Tagging.KillingBlowTags(false, 0, null, golem)).IsFalse();
    }
}
