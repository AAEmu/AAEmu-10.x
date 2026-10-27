using AAEmu.Game.Models.Game.Slaves;

namespace AAEmu.UnitTests.Game.Models.Game.Slaves;

public class SlaveSpawnPortalRulesTests
{
    [Test]
    public async Task ShouldSetPortalFlag_WhenEffectNotHidden()
    {
        await Assert.That(SlaveSpawnPortalRules.ShouldSetPortalFlag(hideSpawnEffect: false)).IsTrue();
        await Assert.That(SlaveSpawnPortalRules.ShouldSetPortalFlag(hideSpawnEffect: true)).IsFalse();
    }

    [Test]
    public async Task ConsumePortalFlag_IsOneShot()
    {
        var pending = true;
        await Assert.That(SlaveSpawnPortalRules.ConsumePortalFlag(ref pending)).IsTrue();
        await Assert.That(pending).IsFalse();
        await Assert.That(SlaveSpawnPortalRules.ConsumePortalFlag(ref pending)).IsFalse();
    }

    [Test]
    public async Task KeepStream_WhilePortalPending()
    {
        var spawn = DateTime.UtcNow.AddMinutes(-1);
        await Assert.That(
            SlaveSpawnPortalRules.ShouldKeepStreamedDuringSpawn(true, spawn, DateTime.UtcNow)).IsTrue();
    }

    [Test]
    public async Task KeepStream_DuringGraceAfterSpawn()
    {
        var now = DateTime.UtcNow;
        var spawn = now - TimeSpan.FromSeconds(2);
        await Assert.That(
            SlaveSpawnPortalRules.ShouldKeepStreamedDuringSpawn(false, spawn, now)).IsTrue();
    }

    [Test]
    public async Task DropStream_AfterGraceWithoutPortal()
    {
        var now = DateTime.UtcNow;
        var spawn = now - SlaveSpawnPortalRules.StreamGrace - TimeSpan.FromSeconds(1);
        await Assert.That(
            SlaveSpawnPortalRules.ShouldKeepStreamedDuringSpawn(false, spawn, now)).IsFalse();
    }
}
