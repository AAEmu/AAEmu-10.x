using System.Numerics;

using AAEmu.Game.Models.Game.Teleport;

namespace AAEmu.UnitTests.Game.Models.Game.Teleport;

/// <summary>
/// The landings a client was sent to and the spot it reports when it confirms one (<c>CSTeleportEnded</c>): near any
/// of them is honest, farther than the tolerance from all of them is put back at the latest, and with none in the
/// character's world and instance the confirmation is taken as it comes.
/// </summary>
public class TeleportLandingWatchTests
{
    private const uint World = 1;
    private const uint Instance = 1;
    private const float Tolerance = 20f;

    private static readonly Vector3 First = new(4000f, 1000f, 100f);
    private static readonly Vector3 Second = new(9000f, 3000f, 250f);

    [Test]
    public async Task NoLandingWaiting_IsTakenAsItComes()
    {
        var watch = new TeleportLandingWatch();
        var reported = new Vector3(123f, 456f, 7f);

        var check = watch.Take(World, Instance, reported, Tolerance, out var sent);

        await Assert.That(check).IsEqualTo(TeleportEndedCheck.NoneExpected);
        await Assert.That(sent).IsEqualTo(reported);
    }

    [Test]
    [Arguments(0f, 0f, 0f)]
    [Arguments(3f, -2f, -0.5f)]
    [Arguments(0f, 0f, -19.9f)]
    [Arguments(20f, 0f, 0f)]
    public async Task AFewMetresOff_IsHonest(float dx, float dy, float dz)
    {
        var watch = Watching(First);

        var check = watch.Take(World, Instance, First + new Vector3(dx, dy, dz), Tolerance, out var sent);

        await Assert.That(check).IsEqualTo(TeleportEndedCheck.WithinTolerance);
        await Assert.That(sent).IsEqualTo(First);
    }

    /// <summary>The distance is 3D: a spot straight above or below the landing counts as much as one beside it.</summary>
    [Test]
    [Arguments(20.5f, 0f, 0f)]
    [Arguments(15f, 15f, 0f)]
    [Arguments(0f, 0f, 25f)]
    [Arguments(3000f, 0f, 0f)]
    public async Task FartherThanTheTolerance_IsFarOff_AndTheSentSpotIsTheLanding(float dx, float dy, float dz)
    {
        var watch = Watching(First);

        var check = watch.Take(World, Instance, First + new Vector3(dx, dy, dz), Tolerance, out var sent);

        await Assert.That(check).IsEqualTo(TeleportEndedCheck.FarOff);
        await Assert.That(sent).IsEqualTo(First);
    }

    /// <summary>A landing in another world or instance (a copy since entered or left) is not what this confirms.</summary>
    [Test]
    [Arguments(2u, 1u)]
    [Arguments(1u, 7u)]
    public async Task ALandingElsewhere_DoesNotCount(uint worldId, uint instanceId)
    {
        var watch = new TeleportLandingWatch();
        watch.Expect(new ExpectedTeleportLanding(worldId, instanceId, First));

        var check = watch.Take(World, Instance, First + new Vector3(500f, 0f, 0f), Tolerance, out _);

        await Assert.That(check).IsEqualTo(TeleportEndedCheck.NoneExpected);
    }

    /// <summary>
    /// A second landing sent before the client confirmed the first: the confirmation of either is honest, and one far
    /// from both is put back where the server sent the character last.
    /// </summary>
    [Test]
    public async Task RepeatedLandings_EitherIsHonest_AndFarOffMeansTheLatest()
    {
        var nearFirst = Watching(First, Second).Take(World, Instance, First + Vector3.UnitX, Tolerance, out var sentFirst);
        var nearSecond = Watching(First, Second).Take(World, Instance, Second + Vector3.UnitY, Tolerance, out var sentSecond);
        var farOff = Watching(First, Second).Take(World, Instance, new Vector3(1f, 1f, 1f) * 7000f, Tolerance, out var sentFar);

        await Assert.That((nearFirst, sentFirst)).IsEqualTo((TeleportEndedCheck.WithinTolerance, First));
        await Assert.That((nearSecond, sentSecond)).IsEqualTo((TeleportEndedCheck.WithinTolerance, Second));
        await Assert.That((farOff, sentFar)).IsEqualTo((TeleportEndedCheck.FarOff, Second));
    }

    [Test]
    public async Task OnlyTheLastFewLandingsAreKept()
    {
        var watch = new TeleportLandingWatch();
        for (var i = 0; i <= TeleportLandingWatch.MaxWaiting; i++)
            watch.Expect(new ExpectedTeleportLanding(World, Instance, First + new Vector3(i * 1000f, 0f, 0f)));

        var latest = First + new Vector3(TeleportLandingWatch.MaxWaiting * 1000f, 0f, 0f);
        var count = watch.Count;
        var check = watch.Take(World, Instance, First, Tolerance, out var sent);

        await Assert.That(count).IsEqualTo(TeleportLandingWatch.MaxWaiting);
        await Assert.That(check).IsEqualTo(TeleportEndedCheck.FarOff);
        await Assert.That(sent).IsEqualTo(latest);
    }

    [Test]
    [Arguments(0f)]
    [Arguments(-5f)]
    public async Task ANonPositiveTolerance_TurnsTheCheckOff(float tolerance)
    {
        var watch = Watching(First);
        var reported = First + new Vector3(3000f, 0f, 0f);

        var check = watch.Take(World, Instance, reported, tolerance, out var sent);

        await Assert.That(check).IsEqualTo(TeleportEndedCheck.NoneExpected);
        await Assert.That(sent).IsEqualTo(reported);
    }

    [Test]
    public async Task AConfirmationTakesEveryLanding_AndForgetDropsThem()
    {
        var taken = Watching(First, Second);
        _ = taken.Take(World, Instance, First, Tolerance, out _);
        var forgotten = Watching(First, Second);
        forgotten.Forget();

        await Assert.That(taken.Count).IsEqualTo(0);
        await Assert.That(forgotten.Count).IsEqualTo(0);
        await Assert.That(forgotten.Take(World, Instance, First + new Vector3(900f, 0f, 0f), Tolerance, out _))
            .IsEqualTo(TeleportEndedCheck.NoneExpected);
    }

    private static TeleportLandingWatch Watching(params Vector3[] landings)
    {
        var watch = new TeleportLandingWatch();
        foreach (var landing in landings)
            watch.Expect(new ExpectedTeleportLanding(World, Instance, landing));
        return watch;
    }
}
