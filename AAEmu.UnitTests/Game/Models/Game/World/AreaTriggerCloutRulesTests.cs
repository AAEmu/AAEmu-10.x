using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.UnitTests.Game.Models.Game.World;

public class AreaTriggerCloutRulesTests
{
    [Test]
    [Arguments(true, false, true)]
    [Arguments(true, true, true)]
    [Arguments(false, true, true)]
    [Arguments(false, false, false)]
    public async Task TracksOccupantsWithoutBuff_AllowsEffectOnlyClouts(
        bool hasInsideBuff, bool hasEffects, bool expected)
    {
        await Assert.That(AreaTriggerCloutRules.TracksOccupantsWithoutBuff(hasInsideBuff, hasEffects))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments(true, false, true)]
    [Arguments(false, true, true)]
    [Arguments(false, false, false)]
    public async Task AppliesEffectsOnTick_AllowsEffectOnlyClouts(
        bool hasInsideBuff, bool hasEffects, bool expected)
    {
        await Assert.That(AreaTriggerCloutRules.AppliesEffectsOnTick(hasInsideBuff, hasEffects))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments(120, 200, true)]
    [Arguments(100, 200, true)]
    [Arguments(200, 200, true)]
    [Arguments(201, 200, false)]
    [Arguments(0, 200, false)]
    [Arguments(10000, 200, false)]
    public async Task NeedsImmediatePulse_WhenDurationFitsInManagerTick(
        int durationMs, int managerTickMs, bool expected)
    {
        await Assert.That(AreaTriggerCloutRules.NeedsImmediatePulse(durationMs, managerTickMs))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments(false, true)]
    [Arguments(true, false)]
    public async Task WaitUntilOwnerVisible_SkipsThePulseBeforeSpawn(bool ownerVisible, bool expected)
    {
        await Assert.That(AreaTriggerCloutRules.WaitUntilOwnerVisible(ownerVisible)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(false, true)]
    [Arguments(true, false)]
    public async Task NeedsPostSpawnPulse_WhenArmRanBeforeVisible(bool ownerWasVisibleAtArm, bool expected)
    {
        await Assert.That(AreaTriggerCloutRules.NeedsPostSpawnPulse(ownerWasVisibleAtArm)).IsEqualTo(expected);
    }

    [Test]
    public async Task MayTickAfterDelete_BlocksQueuedRemovals()
    {
        // SkillHit clears the Clout before the manager drains its remove queue.
        await Assert.That(AreaTriggerCloutRules.MayTickAfterDelete(deleted: false)).IsTrue();
        await Assert.That(AreaTriggerCloutRules.MayTickAfterDelete(deleted: true)).IsFalse();
    }

    [Test]
    public async Task TargetMaxCount_KeepsTheCasterOverNearerUnits()
    {
        // Island crop clout 5328 (target_max_count 1): the harvester, not the spawner NPCs around the crop.
        var harvester = new Unit { ObjId = 888 };
        var spawnerA = new Unit { ObjId = 959 };
        var spawnerB = new Unit { ObjId = 929 };
        var distance = new Dictionary<Unit, double> { [harvester] = 2.5, [spawnerA] = 0.5, [spawnerB] = 1.0 };

        var picked = AreaTriggerCloutRules.Targets([spawnerA, spawnerB, harvester], harvester, 1, u => distance[u]);

        await Assert.That(picked).IsEquivalentTo(new[] { harvester });
    }

    [Test]
    public async Task NoTargetMaxCount_KeepsEveryone()
    {
        var a = new Unit { ObjId = 1 };
        var b = new Unit { ObjId = 2 };

        var picked = AreaTriggerCloutRules.Targets([a, b], a, 0, _ => 0d);

        await Assert.That(picked.Count).IsEqualTo(2);
    }
}
