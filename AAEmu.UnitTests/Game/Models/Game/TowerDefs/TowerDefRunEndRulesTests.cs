using AAEmu.Game.Models.Game.TowerDefs;

namespace AAEmu.UnitTests.Game.Models.Game.TowerDefs;

public class TowerDefRunEndRulesTests
{
    private static readonly DateTime T0 = new(2026, 11, 11, 10, 18, 35, DateTimeKind.Utc);

    [Test]
    public async Task LastStep_EndsWhenItsOwnTimeIsUp()
    {
        // Island tower 202: prog 801 (index 5 of 6) holds 3 s and is the last step.
        var due = T0.AddSeconds(3);

        await Assert.That(TowerDefRunEndRules.IsComplete(6, 6, -1, due, T0.AddSeconds(2))).IsFalse();
        await Assert.That(TowerDefRunEndRules.IsComplete(6, 6, -1, due, due)).IsTrue();
    }

    [Test]
    public async Task StepsLeft_OrKillQuotaOpen_IsNotComplete()
    {
        await Assert.That(TowerDefRunEndRules.IsComplete(5, 6, -1, T0, T0)).IsFalse();
        await Assert.That(TowerDefRunEndRules.IsComplete(6, 6, 5, T0, T0)).IsFalse();
        await Assert.That(TowerDefRunEndRules.IsComplete(0, 0, -1, T0, T0)).IsFalse();
    }
}
