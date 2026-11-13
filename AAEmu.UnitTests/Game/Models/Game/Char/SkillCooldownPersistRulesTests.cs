using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class SkillCooldownPersistRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 23, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task RunningCooldown_IsSaved_RunOnceMarkerIsNot()
    {
        await Assert.That(SkillCooldownPersistRules.ShouldSave(new UnitCooldowns.CooldownSnapshot(50650, 14_400_000, 1000))).IsTrue();
        await Assert.That(SkillCooldownPersistRules.ShouldSave(new UnitCooldowns.CooldownSnapshot(1, uint.MaxValue, 1000))).IsFalse();
        await Assert.That(SkillCooldownPersistRules.ShouldSave(new UnitCooldowns.CooldownSnapshot(1, 5000, 0))).IsFalse();
    }

    [Test]
    public async Task Cooldowns_KeepRunningWhileOffline()
    {
        var endsAt = Now.AddHours(1);

        await Assert.That(SkillCooldownPersistRules.RemainingMs(endsAt, Now.AddMinutes(30))).IsEqualTo(1_800_000u);
        await Assert.That(SkillCooldownPersistRules.RemainingMs(endsAt, Now.AddHours(2))).IsEqualTo(0u);
    }
}
