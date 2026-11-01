using System.Numerics;

using AAEmu.Game.Core.Managers.World;

namespace AAEmu.UnitTests.Game.Core.Managers.World;

public class HullPoseSpaceRulesTests
{
    // Silent Sea 213 origin cell (16,18) × 1024 — same shape as a local-as-world leak.
    private static readonly Vector3 Origin = new(16 * 1024f, 18 * 1024f, 0f);

    [Test]
    public async Task Repair_WhenReportedLocalFarFromContinentSeed()
    {
        var seed = new Vector3(20632.3f, 22271.5f, 100.8f);
        var local = seed - Origin;

        var repaired = HullPoseSpaceRules.TryRepairLocalAsWorld(
            seed, local, p => p + Origin, out var world);

        await Assert.That(repaired).IsTrue();
        await Assert.That(world.X).IsEqualTo(seed.X).Within(0.1f);
        await Assert.That(world.Y).IsEqualTo(seed.Y).Within(0.1f);
    }

    [Test]
    public async Task Leave_WhenReportAlreadyNearSeed()
    {
        var seed = new Vector3(13048f, 10152f, 100f);
        var reported = seed + new Vector3(12f, -8f, 0.2f);

        var repaired = HullPoseSpaceRules.TryRepairLocalAsWorld(
            seed, reported, p => p + Origin, out var world);

        await Assert.That(repaired).IsFalse();
        await Assert.That(world).IsEqualTo(reported);
    }

    [Test]
    public async Task Leave_WhenWorldLiftIsNotCloser()
    {
        var seed = new Vector3(20632f, 22271f, 100f);
        // A real multi-km sail, not a local poison — lifting by origin makes it worse.
        var reported = seed + new Vector3(5000f, 0f, 0f);

        var repaired = HullPoseSpaceRules.TryRepairLocalAsWorld(
            seed, reported, p => p + Origin, out _);

        await Assert.That(repaired).IsFalse();
    }
}
