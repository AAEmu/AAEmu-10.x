using AAEmu.Game.Models.Game.Skills.Effects;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects;

public class SpawnOrientationRulesTests
{
    private static float Deg(float rad) => rad * 180f / MathF.PI;

    [Test]
    public async Task RingAroundTheUnit_FacesIt()
    {
        // Spawned 10 m east of the unit: facing west (+90° from +Y).
        await Assert.That(Deg(SpawnOrientationRules.FacingYaw(10f, 0f, 0f, 0f, 0f))).IsEqualTo(90f).Within(0.01f);
        // Spawned 10 m north: facing south.
        await Assert.That(MathF.Abs(Deg(SpawnOrientationRules.FacingYaw(0f, 10f, 0f, 0f, 0f)))).IsEqualTo(180f).Within(0.01f);
    }

    [Test]
    public async Task OriAngle_TurnsFromFacingTheUnit()
    {
        // ori_angle 180 on a ring faces away from the unit.
        await Assert.That(Deg(SpawnOrientationRules.FacingYaw(10f, 0f, 0f, 0f, 180f))).IsEqualTo(270f).Within(0.01f);
    }

    [Test]
    public async Task FacesTheUnit_OnlyWhenPlacedAroundItAndAwayFromIt()
    {
        await Assert.That(SpawnOrientationRules.FacesOrientationUnit(sameUnit: true, distance: 7f)).IsTrue();
        await Assert.That(SpawnOrientationRules.FacesOrientationUnit(sameUnit: false, distance: 7f)).IsFalse();
        await Assert.That(SpawnOrientationRules.FacesOrientationUnit(sameUnit: true, distance: 0f)).IsFalse();
    }
}
