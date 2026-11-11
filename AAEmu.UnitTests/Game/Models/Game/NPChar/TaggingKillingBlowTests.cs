using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Faction;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Static;
using AAEmu.Game.Models.StaticValues;
using AAEmu.UnitTests.Utils.Mocks;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

/// <summary>
/// The two rules on their own: when a hit counts as a killing blow to tag, and who a tag by a vehicle
/// goes to. <see cref="NpcOneShotKillCreditTests"/> runs them through a real NPC death.
/// </summary>
public class TaggingKillingBlowTests
{
    private const int MaxHp = 2900;

    private sealed class Victim : Slave
    {
        public override int MaxHp => TaggingKillingBlowTests.MaxHp;
    }

    private static bool Tags(Tagging tagging, BaseUnit attacker, BaseUnit victim,
        int oldHp = MaxHp, int newHp = 0, KillReason reason = KillReason.Damage) =>
        Tagging.KillingBlowTags(tagging.Tagger != null, tagging.TagTeam, attacker, victim, oldHp, newHp, reason);

    [Test]
    public async Task OneShotCannonKill_TagsTheGunner()
    {
        // Quest 1401: an Andelph cannon (ownerless slave 609, faction Friendly) kills a golem (npc 2301)
        // with its first shot, so DamageEffect never got to record a hit before the golem died.
        var golem = new Victim { ObjId = 3407 };
        var gunner = new CharacterMock { ObjId = 2135 };
        var cannon = new Slave { ObjId = 984, Faction = new SystemFaction { Id = FactionsEnum.Friendly } };
        cannon.AttachedCharacters.Add(AttachPointKind.Driver, gunner);
        var tagging = new Tagging(golem);

        await Assert.That(Tags(tagging, cannon, golem)).IsTrue();
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

        tagging.AddTagger(new Slave { ObjId = 984, Faction = new SystemFaction { Id = FactionsEnum.Friendly } },
            golem.MaxHp);

        await Assert.That(tagging.Tagger).IsNull();
    }

    [Test]
    public async Task AlreadyTagged_KeepsItsTagger()
    {
        var golem = new Victim { ObjId = 3407 };
        var killer = new CharacterMock { ObjId = 1 };

        await Assert.That(Tagging.KillingBlowTags(true, 0, killer, golem, MaxHp, 0, KillReason.Damage)).IsFalse();
        await Assert.That(Tagging.KillingBlowTags(false, 7, killer, golem, MaxHp, 0, KillReason.Damage)).IsFalse();
    }

    [Test]
    public async Task OwnDeath_DoesNotTag()
    {
        var golem = new Victim { ObjId = 3407 };
        var tagging = new Tagging(golem);

        await Assert.That(Tags(tagging, golem, golem)).IsFalse();
        await Assert.That(Tags(tagging, null, golem)).IsFalse();
    }

    [Test]
    public async Task HitThatDoesNotKill_DoesNotTag()
    {
        // Those are recorded by DamageEffect after the hit, as before.
        var golem = new Victim { ObjId = 3407 };
        var tagging = new Tagging(golem);
        var player = new CharacterMock { ObjId = 11 };

        await Assert.That(Tags(tagging, player, golem, oldHp: MaxHp, newHp: 1)).IsFalse();
        // A heal or a re-published health value on a unit that is already dead is no blow at all.
        await Assert.That(Tags(tagging, player, golem, oldHp: 0, newHp: 0)).IsFalse();
        await Assert.That(Tags(tagging, player, golem, oldHp: 0, newHp: MaxHp)).IsFalse();
    }

    [Test]
    [Arguments(KillReason.Gm)]
    [Arguments(KillReason.Capture)]
    [Arguments(KillReason.NpcRemovalOnly)]
    [Arguments(KillReason.Unknown)]
    public async Task RemovalThatIsNotDamage_DoesNotTag(KillReason reason)
    {
        // A GM kill and a pet capture also take the health to zero in the name of a character.
        var golem = new Victim { ObjId = 3407 };
        var tagging = new Tagging(golem);
        var player = new CharacterMock { ObjId = 11 };

        await Assert.That(Tags(tagging, player, golem, reason: reason)).IsFalse();
    }
}
