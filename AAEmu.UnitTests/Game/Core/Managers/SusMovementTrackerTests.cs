using System.Numerics;
using AAEmu.Game.Core.Managers;

namespace AAEmu.UnitTests.Game.Core.Managers;

/// <summary>
/// The per-id bookkeeping behind the movement speed checks: the arithmetic the checks have always done (the first
/// observation seeds, 15 s left alone after a flag, a reset starts over), and safe to call from players' packet
/// threads and the bots' tick thread at once.
/// </summary>
public class SusMovementTrackerTests
{
    /// <summary>What <c>Transform.FinalizeTransform</c> passes: one check every 5 s.</summary>
    private const float Cadence = 5f;

    private const uint Id = 7;

    [Test]
    public async Task Analyze_FirstObservation_OnlySeeds()
    {
        var tracker = new SusMovementTracker();

        var flagged = tracker.Analyze(Id, new Vector3(5000f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(flagged).IsNull();
        await Assert.That(tracker.TryGet(Id, out var position, out var skipTime)).IsTrue();
        await Assert.That(position).IsEqualTo(new Vector3(5000f, 0f, 0f));
        await Assert.That(skipTime).IsEqualTo(0f);
        await Assert.That(tracker.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Analyze_UnderTheLimit_ReportsNothingAndCountsDown()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        var first = tracker.Analyze(Id, new Vector3(100f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out var afterFirst, out var skipAfterFirst);
        var second = tracker.Analyze(Id, new Vector3(200f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out var afterSecond, out var skipAfterSecond);

        await Assert.That(first).IsNull();
        await Assert.That(second).IsNull();
        await Assert.That(afterFirst).IsEqualTo(new Vector3(100f, 0f, 0f));
        await Assert.That(skipAfterFirst).IsEqualTo(-5f);
        await Assert.That(afterSecond).IsEqualTo(new Vector3(200f, 0f, 0f));
        await Assert.That(skipAfterSecond).IsEqualTo(-10f);
    }

    [Test]
    public async Task Analyze_ExactlyAtTheLimit_IsNotFlagged()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        // 125 m in 5 s is 25 m/s: only faster than the limit is flagged.
        var flagged = tracker.Analyze(Id, new Vector3(125f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(flagged).IsNull();
    }

    [Test]
    public async Task Analyze_OverTheLimit_ReportsTheSpeedThenLeavesTheIdAloneFor15Seconds()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        // 150 m flat (90, 120) in 5 s is 30 m/s.
        var flag = tracker.Analyze(Id, new Vector3(90f, 120f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out _, out var skipAfterFlag);
        // Two more checks 5 s apart jump a kilometre each and are not judged.
        var quiet1 = tracker.Analyze(Id, new Vector3(1090f, 120f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out _, out var skipAfterQuiet1);
        var quiet2 = tracker.Analyze(Id, new Vector3(2090f, 120f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out var afterQuiet2, out var skipAfterQuiet2);
        // 15 s after the flag the check runs again, measured from the last recorded position.
        var again = tracker.Analyze(Id, new Vector3(3090f, 120f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out _, out var skipAfterAgain);

        await Assert.That(flag).IsEqualTo(30f);
        await Assert.That(skipAfterFlag).IsEqualTo(10f);
        await Assert.That(quiet1).IsNull();
        await Assert.That(skipAfterQuiet1).IsEqualTo(5f);
        await Assert.That(quiet2).IsNull();
        await Assert.That(afterQuiet2).IsEqualTo(new Vector3(2090f, 120f, 0f));
        await Assert.That(skipAfterQuiet2).IsEqualTo(0f);
        await Assert.That(again).IsEqualTo(200f);
        await Assert.That(skipAfterAgain).IsEqualTo(10f);
    }

    [Test]
    public async Task Analyze_MeasuresFlatDistanceOnly()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        // A fall or a lift of a kilometre has no flat delta: not judged, still recorded and counted down.
        var vertical = tracker.Analyze(Id, new Vector3(0f, 0f, 1000f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out var afterVertical, out var skipAfterVertical);
        // The height of a flat move is ignored: 150 m flat is 30 m/s however far it climbed.
        var climbing = tracker.Analyze(Id, new Vector3(90f, 120f, -500f), true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(vertical).IsNull();
        await Assert.That(afterVertical).IsEqualTo(new Vector3(0f, 0f, 1000f));
        await Assert.That(skipAfterVertical).IsEqualTo(-5f);
        await Assert.That(climbing).IsEqualTo(30f);
    }

    [Test]
    public async Task Analyze_MountLimit_Is27AndAHalf()
    {
        var player = new SusMovementTracker();
        var mount = new SusMovementTracker();
        player.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        mount.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.MountCheckSpeed);
        var mountAtLimit = new SusMovementTracker();
        mountAtLimit.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.MountCheckSpeed);
        var mountOver = new SusMovementTracker();
        mountOver.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.MountCheckSpeed);
        var playerJustOver = new SusMovementTracker();
        playerJustOver.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        // 130 m in 5 s is 26 m/s: too fast on foot, fine on a mount.
        var playerAt26 = player.Analyze(Id, new Vector3(130f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        var playerAt25AndAHalf = playerJustOver.Analyze(Id, new Vector3(127.5f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        var mountAt26 = mount.Analyze(Id, new Vector3(130f, 0f, 0f), true, Cadence, SusManager.MountCheckSpeed);
        var mountAt27AndAHalf = mountAtLimit.Analyze(Id, new Vector3(137.5f, 0f, 0f), true, Cadence, SusManager.MountCheckSpeed);
        var mountAt28 = mountOver.Analyze(Id, new Vector3(140f, 0f, 0f), true, Cadence, SusManager.MountCheckSpeed);

        // With 125 m not flagged (Analyze_ExactlyAtTheLimit_IsNotFlagged) the limits are 25 and 27.5 m/s.
        await Assert.That(playerAt26).IsEqualTo(26f);
        await Assert.That(playerAt25AndAHalf).IsEqualTo(25.5f);
        await Assert.That(mountAt26).IsNull();
        await Assert.That(mountAt27AndAHalf).IsNull();
        await Assert.That(mountAt28).IsEqualTo(28f);
    }

    [Test]
    public async Task Analyze_WhileTheCheckMayNotRun_RecordsThePositionWithoutJudging()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        // Riding or position disabled: a kilometre is not judged, but the position moves on.
        var riding = tracker.Analyze(Id, new Vector3(1000f, 0f, 0f), false, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out var afterRiding, out var skipAfterRiding);
        // Back on foot, the next check is measured from where the ride ended.
        var walking = tracker.Analyze(Id, new Vector3(1020f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(riding).IsNull();
        await Assert.That(afterRiding).IsEqualTo(new Vector3(1000f, 0f, 0f));
        await Assert.That(skipAfterRiding).IsEqualTo(-5f);
        await Assert.That(walking).IsNull();
    }

    [Test]
    public async Task Reset_ForgetsTheId_SoTheNextObservationOnlySeeds()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.Analyze(Id, new Vector3(10f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);

        tracker.Reset(Id);
        var tracked = tracker.TryGet(Id, out _, out _);
        // A teleport of 5 km after the reset is only the new starting point.
        var afterReset = tracker.Analyze(Id, new Vector3(5000f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out var seeded, out var skipTime);

        await Assert.That(tracked).IsFalse();
        await Assert.That(afterReset).IsNull();
        await Assert.That(seeded).IsEqualTo(new Vector3(5000f, 0f, 0f));
        await Assert.That(skipTime).IsEqualTo(0f);
    }

    [Test]
    public async Task Reset_DuringTheQuietTime_StartsOverWithoutIt()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        var first = tracker.Analyze(Id, new Vector3(500f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);

        tracker.Reset(Id);
        tracker.Analyze(Id, new Vector3(600f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        // Without the reset this would still be inside the 15 s after the first flag.
        var second = tracker.Analyze(Id, new Vector3(1100f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(first).IsEqualTo(100f);
        await Assert.That(second).IsEqualTo(100f);
    }

    [Test]
    public async Task Reset_OfAnUnknownId_DoesNothing()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        tracker.Reset(Id + 1);

        await Assert.That(tracker.Count).IsEqualTo(1);
        await Assert.That(tracker.TryGet(Id + 1, out _, out _)).IsFalse();
    }

    /// <summary>Where the server sent the client in the landing tests: 3 km off, 600 m/s over one check.</summary>
    private static readonly Vector3 Landing = new(3000f, 0f, 0f);

    [Test]
    public async Task ExpectLanding_TheObservationThatReachesIt_IsNotJudged_ThenChecksGoOnFromThere()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        tracker.ExpectLanding(Id, Landing);
        // The client walked on a little after it made the jump; the height it settled at does not count.
        var arrival = tracker.Analyze(Id, Landing + new Vector3(30f, 40f, -2f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out var position, out var skipTime);
        var stillExpected = tracker.TryGetExpectedLanding(Id, out _);
        // 200 m in the next 5 s is judged from the arrival.
        var tooFast = tracker.Analyze(Id, Landing + new Vector3(230f, 40f, -2f), true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(arrival).IsNull();
        await Assert.That(position).IsEqualTo(Landing + new Vector3(30f, 40f, -2f));
        await Assert.That(skipTime).IsEqualTo(-5f);
        await Assert.That(stillExpected).IsFalse();
        await Assert.That(tooFast).IsEqualTo(40f);
    }

    /// <summary>
    /// Moves the client sent before it made the jump arrive after the server expected the landing. One the check
    /// falls on is judged from the old spot as usual and does not use the landing up.
    /// </summary>
    [Test]
    public async Task ExpectLanding_AMoveStillInFlightFromTheOldSpot_IsJudgedAsUsual_AndTheLandingIsStillReached()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        tracker.ExpectLanding(Id, Landing);
        var inFlight = tracker.Analyze(Id, new Vector3(50f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out var afterInFlight, out _);
        var expectedAfterInFlight = tracker.TryGetExpectedLanding(Id, out var landing);
        var arrival = tracker.Analyze(Id, Landing, true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(inFlight).IsNull();
        await Assert.That(afterInFlight).IsEqualTo(new Vector3(50f, 0f, 0f));
        await Assert.That(expectedAfterInFlight).IsTrue();
        await Assert.That(landing).IsEqualTo(Landing);
        await Assert.That(arrival).IsNull();
        await Assert.That(tracker.TryGetExpectedLanding(Id, out _)).IsFalse();
    }

    /// <summary>With no entry yet, the move still in flight only seeds and the arrival is still not judged.</summary>
    [Test]
    public async Task ExpectLanding_WithNothingRecorded_TheMoveInFlightSeeds_AndTheArrivalIsNotJudged()
    {
        var tracker = new SusMovementTracker();

        tracker.ExpectLanding(Id, Landing);
        var inFlight = tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        var arrival = tracker.Analyze(Id, Landing, true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out var position, out _);

        await Assert.That(inFlight).IsNull();
        await Assert.That(arrival).IsNull();
        await Assert.That(position).IsEqualTo(Landing);
    }

    /// <summary>
    /// A client that is sent somewhere and goes elsewhere gains nothing: anywhere but the landing is judged from the
    /// spot it was last seen at, as if nothing had been expected.
    /// </summary>
    [Test]
    public async Task ExpectLanding_AJumpAnywhereElse_IsJudgedAsBefore()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        tracker.ExpectLanding(Id, Landing);
        // 1 km off the old spot and 2 km short of the landing.
        var elsewhere = tracker.Analyze(Id, new Vector3(1000f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(elsewhere).IsEqualTo(200f);
    }

    /// <summary>
    /// The landing is a packet the client cannot send, but some server moves can be triggered at will (an escape
    /// cast out of combat). Expecting a landing before every check changes nothing for a client that never goes
    /// there: the same speeds are reported at the same checks as with no landing at all.
    /// </summary>
    [Test]
    public async Task ExpectLanding_BeforeEveryCheck_ReportsAClientThatNeverGoesThereExactlyAsBefore()
    {
        var withLandings = new SusMovementTracker();
        var without = new SusMovementTracker();
        withLandings.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        without.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        var reportedWith = new List<float?>();
        var reportedWithout = new List<float?>();

        // 200 m/s along X, with the landing 3 km to the side of the route every time.
        for (var check = 1; check <= 8; check++)
        {
            var position = new Vector3(check * 1000f, 0f, 0f);
            withLandings.ExpectLanding(Id, position + new Vector3(0f, 3000f, 0f));
            reportedWith.Add(withLandings.Analyze(Id, position, true, Cadence, SusManager.PlayerCheckSpeed));
            reportedWithout.Add(without.Analyze(Id, position, true, Cadence, SusManager.PlayerCheckSpeed));
        }

        await Assert.That(reportedWith.SequenceEqual(reportedWithout)).IsTrue();
        // Checks 1, 4 and 7: each report leaves the id alone for the two checks after it.
        await Assert.That(reportedWith.Count(speed => speed == 200f)).IsEqualTo(3);
    }

    [Test]
    public async Task ExpectLanding_IsGivenUpAfterThreeChecksThatDoNotReachIt()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        tracker.ExpectLanding(Id, Landing);
        for (var check = 1; check < SusMovementTracker.ExpectedLandingChecks; check++)
            tracker.Analyze(Id, new Vector3(check * 10f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        var expectedBeforeTheLast = tracker.TryGetExpectedLanding(Id, out _);
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        var expectedAfterTheLast = tracker.TryGetExpectedLanding(Id, out _);
        // A jump there now is a jump like any other.
        var late = tracker.Analyze(Id, Landing, true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(expectedBeforeTheLast).IsTrue();
        await Assert.That(expectedAfterTheLast).IsFalse();
        await Assert.That(late).IsEqualTo(600f);
    }

    [Test]
    public async Task ExpectLanding_ANewOneReplacesTheOneStillExpected()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        tracker.ExpectLanding(Id, Landing);
        tracker.ExpectLanding(Id, new Vector3(0f, 3000f, 0f));
        var atTheFirst = tracker.Analyze(Id, Landing, true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(atTheFirst).IsEqualTo(600f);
    }

    [Test]
    public async Task Reset_AlsoForgetsAnExpectedLanding()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.ExpectLanding(Id, Landing);

        tracker.Reset(Id);

        await Assert.That(tracker.TryGetExpectedLanding(Id, out _)).IsFalse();
        await Assert.That(tracker.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ExpectLanding_IsPerId()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(1, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.Analyze(2, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        tracker.ExpectLanding(1, Landing);
        var one = tracker.Analyze(1, Landing, true, Cadence, SusManager.PlayerCheckSpeed);
        var two = tracker.Analyze(2, Landing, true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(one).IsNull();
        await Assert.That(two).IsEqualTo(600f);
    }

    [Test]
    public async Task Analyze_UsesTheGivenDeltaTime()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(Id, Vector3.Zero, true, 2f, SusManager.PlayerCheckSpeed);

        var slow = tracker.Analyze(Id, new Vector3(40f, 0f, 0f), true, 2f, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out _, out var skipAfterSlow);
        var fast = tracker.Analyze(Id, new Vector3(100f, 0f, 0f), true, 2f, SusManager.PlayerCheckSpeed);
        tracker.TryGet(Id, out _, out var skipAfterFast);

        await Assert.That(slow).IsNull();
        await Assert.That(skipAfterSlow).IsEqualTo(-2f);
        await Assert.That(fast).IsEqualTo(30f);
        // 15 s on the flag, then this check's own 2 s off it.
        await Assert.That(skipAfterFast).IsEqualTo(13f);
    }

    [Test]
    public async Task Ids_AreTrackedSeparately()
    {
        var tracker = new SusMovementTracker();
        tracker.Analyze(1, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.Analyze(2, Vector3.Zero, true, Cadence, SusManager.PlayerCheckSpeed);

        var one = tracker.Analyze(1, new Vector3(500f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);
        tracker.Reset(1);
        var two = tracker.Analyze(2, new Vector3(500f, 0f, 0f), true, Cadence, SusManager.PlayerCheckSpeed);

        await Assert.That(one).IsEqualTo(100f);
        await Assert.That(two).IsEqualTo(100f);
        await Assert.That(tracker.TryGet(1, out _, out _)).IsFalse();
        await Assert.That(tracker.TryGet(2, out _, out var skipTwo)).IsTrue();
        await Assert.That(skipTwo).IsEqualTo(10f);
    }

    /// <summary>
    /// A long random run of checks and resets gives exactly what the checks computed before the tracker existed,
    /// bit for bit: every reported speed, every position and every skip time.
    /// </summary>
    [Test]
    public async Task Analyze_MatchesTheOriginalArithmetic_OnARandomRun()
    {
        var tracker = new SusMovementTracker();
        var original = new Dictionary<uint, (Vector3 pos, float skipTime)>();
        var random = new Random(1234);
        var mismatches = 0;
        var flags = 0;

        for (var i = 0; i < 20000; i++)
        {
            var id = (uint)random.Next(1, 6);
            if (random.Next(12) == 0)
            {
                tracker.Reset(id);
                _ = original.Remove(id);
                continue;
            }

            var position = new Vector3(random.NextSingle() * 400f - 200f, random.NextSingle() * 400f - 200f, random.NextSingle() * 50f);
            if (random.Next(6) == 0)
                position = position with { X = 0f, Y = 0f };
            var mayCheck = random.Next(5) != 0;
            var deltaTime = random.Next(3) == 0 ? random.NextSingle() * 10f + 0.1f : Cadence;
            var maxSpeed = random.Next(2) == 0 ? SusManager.PlayerCheckSpeed : SusManager.MountCheckSpeed;

            var reported = tracker.Analyze(id, position, mayCheck, deltaTime, maxSpeed);
            var expected = OriginalAnalyze(original, id, position, mayCheck, deltaTime, maxSpeed);
            if (expected != null)
                flags++;

            if (!SameBits(reported, expected))
                mismatches++;
            tracker.TryGet(id, out var trackedPosition, out var trackedSkipTime);
            var originalEntry = original[id];
            if (trackedPosition != originalEntry.pos || BitConverter.SingleToInt32Bits(trackedSkipTime) != BitConverter.SingleToInt32Bits(originalEntry.skipTime))
                mismatches++;
        }

        await Assert.That(mismatches).IsEqualTo(0);
        await Assert.That(flags).IsGreaterThan(100);
        await Assert.That(tracker.Count).IsEqualTo(original.Count);
    }

    /// <summary>
    /// Eight threads check and reset ids at once, half of them shared by every thread and half each thread's own,
    /// as players' packet threads and the bots' tick thread do. Nothing throws, every thread's own ids end exactly as
    /// the same calls made one after another leave them, and the shared ids hold only values a thread wrote.
    /// </summary>
    [Test]
    public async Task AnalyzeAndReset_FromManyThreads_StayConsistent()
    {
        const int threads = 8;
        const int iterations = 5000;
        var tracker = new SusMovementTracker();
        var ownFlags = new List<float?>[threads];
        using var start = new ManualResetEventSlim(false);

        var workers = Enumerable.Range(0, threads).Select(thread => Task.Factory.StartNew(() =>
        {
            start.Wait();
            ownFlags[thread] = Run(tracker, thread, iterations);
        }, TaskCreationOptions.LongRunning)).ToArray();
        start.Set();
        await Task.WhenAll(workers);

        // The same thread's calls on its own ids, one after another on a tracker nobody else uses.
        var mismatches = 0;
        for (var thread = 0; thread < threads; thread++)
        {
            var alone = new SusMovementTracker();
            var aloneFlags = Run(alone, thread, iterations, ownIdsOnly: true);
            if (!aloneFlags.SequenceEqual(ownFlags[thread]))
                mismatches++;
            foreach (var id in OwnIds(thread))
            {
                var inShared = tracker.TryGet(id, out var sharedPosition, out var sharedSkipTime);
                var inAlone = alone.TryGet(id, out var alonePosition, out var aloneSkipTime);
                if (inShared != inAlone || sharedPosition != alonePosition || sharedSkipTime != aloneSkipTime)
                    mismatches++;
            }
        }

        // Shared ids: a position some thread wrote, a skip time the 5 s steps can reach.
        var badShared = 0;
        var tracked = 0;
        for (uint id = 1; id <= SharedIds; id++)
        {
            if (!tracker.TryGet(id, out var position, out var skipTime))
                continue;
            tracked++;
            if (position.X != MathF.Floor(position.X) || position.Z != id || skipTime > 10f || skipTime % 5f != 0f)
                badShared++;
        }

        for (var thread = 0; thread < threads; thread++)
            tracked += OwnIds(thread).Count(id => tracker.TryGet(id, out _, out _));

        await Assert.That(mismatches).IsEqualTo(0);
        await Assert.That(badShared).IsEqualTo(0);
        await Assert.That(ownFlags.Sum(f => f.Count(speed => speed != null))).IsGreaterThan(0);
        await Assert.That(tracker.Count).IsEqualTo(tracked);

        for (uint id = 1; id <= SharedIds; id++)
            tracker.Reset(id);
        for (var thread = 0; thread < threads; thread++)
        {
            foreach (var id in OwnIds(thread))
                tracker.Reset(id);
        }

        await Assert.That(tracker.Count).IsEqualTo(0);
    }

    private const uint SharedIds = 4;

    private static uint[] OwnIds(int thread) => [100u + (uint)thread * 2, 101u + (uint)thread * 2];

    /// <summary>
    /// One thread's calls: mostly checks, some resets, on shared ids (1-4) and its own two. The calls depend only on
    /// the thread number. Returns what the checks on its own ids reported, in order.
    /// </summary>
    private static List<float?> Run(SusMovementTracker tracker, int thread, int iterations, bool ownIdsOnly = false)
    {
        var random = new Random(thread + 1);
        var ownIds = OwnIds(thread);
        var reported = new List<float?>();
        for (var i = 0; i < iterations; i++)
        {
            var shared = random.Next(2) == 0;
            var id = shared ? (uint)random.Next(1, (int)SharedIds + 1) : ownIds[random.Next(ownIds.Length)];
            var reset = random.Next(8) == 0;
            // Whole metres along X, the id as the height: whatever ends up stored must look like this.
            var position = new Vector3(random.Next(-200, 201), random.Next(-200, 201), id);
            var mayCheck = random.Next(6) != 0;
            if (shared && ownIdsOnly)
                continue;

            if (reset)
            {
                tracker.Reset(id);
                continue;
            }

            var speed = tracker.Analyze(id, position, mayCheck, Cadence, SusManager.PlayerCheckSpeed);
            if (!shared)
                reported.Add(speed);
        }

        return reported;
    }

    /// <summary>The body of the player check before the tracker, verbatim but for the riding and position tests.</summary>
    private static float? OriginalAnalyze(Dictionary<uint, (Vector3 pos, float skipTime)> lastPositions, uint id, Vector3 position, bool mayCheck, float deltaTime, double playerCheckSpeed)
    {
        if (!lastPositions.TryGetValue(id, out var last))
        {
            lastPositions.Add(id, (position, 0f));
            return null;
        }

        float? reported = null;
        var deltaPos = position - last.pos;
        var deltaFlatPos = deltaPos with { Z = 0 };

        if (mayCheck &&
            deltaFlatPos != Vector3.Zero &&
            last.skipTime <= 0f)
        {
            var observedSpeed = deltaFlatPos.Length() / deltaTime;
            if (observedSpeed > playerCheckSpeed)
            {
                last.skipTime = 15f;
                reported = observedSpeed;
            }
        }

        last.skipTime -= deltaTime;
        lastPositions[id] = (position, last.skipTime);
        return reported;
    }

    private static bool SameBits(float? a, float? b) =>
        a.HasValue == b.HasValue && (!a.HasValue || BitConverter.SingleToInt32Bits(a.Value) == BitConverter.SingleToInt32Bits(b.Value));
}
