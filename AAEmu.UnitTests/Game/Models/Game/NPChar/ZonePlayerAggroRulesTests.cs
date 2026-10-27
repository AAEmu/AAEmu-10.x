using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.NPChar;

public class ZonePlayerAggroRulesTests
{
    [Test]
    public async Task SightMetres_ScalesFromBaseBand()
    {
        await Assert.That(ZonePlayerAggroRules.SightMetres(1f)).IsEqualTo(30f);
        await Assert.That(ZonePlayerAggroRules.SightMetres(2f)).IsEqualTo(60f);
        await Assert.That(ZonePlayerAggroRules.SightMetres(0f)).IsEqualTo(30f);
    }

    [Test]
    public async Task ShouldSeed_RequiresZoneAuthorityAggressionAndRange()
    {
        await Assert.That(ZonePlayerAggroRules.ShouldSeed(
            zoneAuthority: false,
            aggression: true,
            canAttackPlayer: true,
            npcAlive: true,
            playerOnline: true,
            distanceSquared: 1f,
            sightMetresSquared: 900f,
            currentTarget: null,
            playerObjId: 10)).IsFalse();

        await Assert.That(ZonePlayerAggroRules.ShouldSeed(
            zoneAuthority: true,
            aggression: false,
            canAttackPlayer: true,
            npcAlive: true,
            playerOnline: true,
            distanceSquared: 1f,
            sightMetresSquared: 900f,
            currentTarget: null,
            playerObjId: 10)).IsFalse();

        await Assert.That(ZonePlayerAggroRules.ShouldSeed(
            zoneAuthority: true,
            aggression: true,
            canAttackPlayer: true,
            npcAlive: true,
            playerOnline: true,
            distanceSquared: 901f,
            sightMetresSquared: 900f,
            currentTarget: null,
            playerObjId: 10)).IsFalse();

        await Assert.That(ZonePlayerAggroRules.ShouldSeed(
            zoneAuthority: true,
            aggression: true,
            canAttackPlayer: true,
            npcAlive: true,
            playerOnline: true,
            distanceSquared: 100f,
            sightMetresSquared: 900f,
            currentTarget: null,
            playerObjId: 10)).IsTrue();
    }

    [Test]
    public async Task HasBlockingTarget_LivingNpcBlocks_DeadSelfOrPlayerDoesNot()
    {
        var livingNpc = new Unit { ObjId = 50, Hp = 100 };
        // Unit alone is treated as blocking (not a Character).
        await Assert.That(ZonePlayerAggroRules.HasBlockingTarget(livingNpc, playerObjId: 10)).IsTrue();

        var dead = new Unit { ObjId = 51, Hp = 0 };
        await Assert.That(ZonePlayerAggroRules.HasBlockingTarget(dead, playerObjId: 10)).IsFalse();

        await Assert.That(ZonePlayerAggroRules.HasBlockingTarget(null, playerObjId: 10)).IsFalse();

        var samePlayer = new Unit { ObjId = 10, Hp = 100 };
        await Assert.That(ZonePlayerAggroRules.HasBlockingTarget(samePlayer, playerObjId: 10)).IsFalse();

        var self = new Unit { ObjId = 99, Hp = 100 };
        await Assert.That(ZonePlayerAggroRules.HasBlockingTarget(self, playerObjId: 10, npcObjId: 99)).IsFalse();
    }
}
