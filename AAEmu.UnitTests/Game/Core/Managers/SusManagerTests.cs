using System.Numerics;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;
using AAEmu.UnitTests.Utils;

namespace AAEmu.UnitTests.Game.Core.Managers;

public class SusManagerTests
{
    [Test]
    public async Task Constructor_WithMockedWorldManager_DoesNotThrow()
    {
        var mockWorldManager = Mock.Of<IWorldManager>();

        var manager = new SusManager(mockWorldManager.Object);

        await Assert.That(manager).IsNotNull();
        Mock.VerifyNoOtherCalls(mockWorldManager);
    }

    [Test]
    public void ResetAnalyzePlayerDeltaMovement_DoesNotCallWorldManager()
    {
        var mockWorldManager = Mock.Of<IWorldManager>();
        var manager = new SusManager(mockWorldManager.Object);

        // Should not throw and should not call worldManager
        manager.ResetAnalyzePlayerDeltaMovement(playerId: 42u);
        Mock.VerifyNoOtherCalls(mockWorldManager);
    }

    [Test]
    public async Task AnalyzePlayerDeltaMovement_FirstCall_SeedsAtTheCharactersPosition()
    {
        var manager = new SusManager(Mock.Of<IWorldManager>().Object);
        var player = NewCharacter(9101, new Vector3(100f, 200f, 10f));

        manager.AnalyzePlayerDeltaMovement(player, 5f);

        await Assert.That(manager.LastPlayerPositions.TryGet(player.Id, out var position, out var skipTime)).IsTrue();
        await Assert.That(position).IsEqualTo(new Vector3(100f, 200f, 10f));
        await Assert.That(skipTime).IsEqualTo(0f);
    }

    [Test]
    public async Task AnalyzePlayerDeltaMovement_WhileRiding_RecordsWithoutJudging()
    {
        var manager = new SusManager(Mock.Of<IWorldManager>().Object);
        var player = NewCharacter(9102, Vector3.Zero);
        manager.AnalyzePlayerDeltaMovement(player, 5f);

        player.IsRiding = true;
        player.Transform.Local.SetPosition(1000f, 0f, 0f);
        manager.AnalyzePlayerDeltaMovement(player, 5f);

        // A flag would have left 10 s of quiet time; an unjudged check only counts down.
        await Assert.That(manager.LastPlayerPositions.TryGet(player.Id, out var position, out var skipTime)).IsTrue();
        await Assert.That(position).IsEqualTo(new Vector3(1000f, 0f, 0f));
        await Assert.That(skipTime).IsEqualTo(-5f);
    }

    [Test]
    public async Task AnalyzePlayerDeltaMovement_UnderTheLimit_CountsDown()
    {
        var manager = new SusManager(Mock.Of<IWorldManager>().Object);
        var player = NewCharacter(9103, Vector3.Zero);
        manager.AnalyzePlayerDeltaMovement(player, 5f);

        // 120 m in 5 s is 24 m/s.
        player.Transform.Local.SetPosition(120f, 0f, 0f);
        manager.AnalyzePlayerDeltaMovement(player, 5f);

        await Assert.That(manager.LastPlayerPositions.TryGet(player.Id, out _, out var skipTime)).IsTrue();
        await Assert.That(skipTime).IsEqualTo(-5f);
    }

    [Test]
    public async Task AnalyzeMountDeltaMovement_UnderTheMountLimit_LooksUpNoOwner()
    {
        var mockWorldManager = Mock.Of<IWorldManager>();
        var manager = new SusManager(mockWorldManager.Object);
        var mount = new Mate { Id = 9104, OwnerObjId = 9105, Name = "Mount" };
        mount.Transform.Local.SetPosition(0f, 0f, 0f);
        manager.AnalyzeMountDeltaMovement(mount, 5f);

        // 130 m in 5 s is 26 m/s: over a player's limit, under a mount's.
        mount.Transform.Local.SetPosition(130f, 0f, 0f);
        manager.AnalyzeMountDeltaMovement(mount, 5f);

        await Assert.That(manager.LastPetPositions.TryGet(mount.Id, out var position, out var skipTime)).IsTrue();
        await Assert.That(position).IsEqualTo(new Vector3(130f, 0f, 0f));
        await Assert.That(skipTime).IsEqualTo(-5f);
        await Assert.That(manager.LastPlayerPositions.Count).IsEqualTo(0);
        Mock.VerifyNoOtherCalls(mockWorldManager);
    }

    [Test]
    [NotInParallel]
    public async Task DisabledSetPosition_ResetsThatCharactersAnalysis()
    {
        var manager = new SusManager(Mock.Of<IWorldManager>().Object);
        using var scope = new SingletonScope<SusManager>(manager);
        var player = NewCharacter(9106, Vector3.Zero);
        var other = NewCharacter(9107, Vector3.Zero);
        manager.AnalyzePlayerDeltaMovement(player, 5f);
        manager.AnalyzePlayerDeltaMovement(other, 5f);

        player.DisabledSetPosition = true;

        await Assert.That(manager.LastPlayerPositions.TryGet(player.Id, out _, out _)).IsFalse();
        await Assert.That(manager.LastPlayerPositions.TryGet(other.Id, out _, out _)).IsTrue();
    }

    /// <summary>
    /// Players' packet threads and the bots' tick thread analyze and reset at once. Each thread walks its own
    /// character (2 m/s, nothing to report) and resets its own and the other threads' characters.
    /// </summary>
    [Test]
    public async Task AnalyzeAndReset_FromManyThreads_DoNotThrow()
    {
        const int threads = 8;
        const int iterations = 2000;
        var manager = new SusManager(Mock.Of<IWorldManager>().Object);
        var players = Enumerable.Range(0, threads).Select(i => NewCharacter(9200u + (uint)i, new Vector3(0f, i * 10f, 0f))).ToArray();
        using var start = new ManualResetEventSlim(false);

        var workers = Enumerable.Range(0, threads).Select(thread => Task.Factory.StartNew(() =>
        {
            start.Wait();
            var random = new Random(thread);
            var player = players[thread];
            for (var i = 0; i < iterations; i++)
            {
                player.Transform.Local.SetPosition((i + 1) * 10f, thread * 10f, 0f);
                manager.AnalyzePlayerDeltaMovement(player, 5f);
                if (random.Next(4) == 0)
                    manager.ResetAnalyzePlayerDeltaMovement(players[random.Next(threads)].Id);
            }
        }, TaskCreationOptions.LongRunning)).ToArray();
        start.Set();
        await Task.WhenAll(workers);

        // Each walked at 2 m/s: nothing was flagged, so no skip time is above zero.
        var flagged = 0;
        foreach (var player in players)
        {
            if (manager.LastPlayerPositions.TryGet(player.Id, out _, out var skipTime) && skipTime > 0f)
                flagged++;
        }

        await Assert.That(flagged).IsEqualTo(0);
        await Assert.That(manager.LastPlayerPositions.Count).IsLessThanOrEqualTo(threads);
    }

    private static Character NewCharacter(uint id, Vector3 position)
    {
        var character = new Character(new UnitCustomModelParams()) { Id = id, ObjId = id, Name = $"Sus{id}" };
        character.Transform.Local.SetPosition(position.X, position.Y, position.Z);
        return character;
    }
}
