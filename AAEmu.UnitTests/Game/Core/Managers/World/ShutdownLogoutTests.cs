using System.Diagnostics;

using AAEmu.Commons.Network.Core;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Core.Managers.World;

/// <summary>
/// The shutdown logout has to reach every player in the world exactly once, leave lobby connections and
/// players who left on their own alone, take over a logout that is already counting down, close world entry
/// first and still catch a player whose entry was already under way.
/// </summary>
public class ShutdownLogoutTests
{
    private readonly List<(GameConnection Connection, Character Character)> _granted = [];

    private static (GameConnection Connection, Character Character, List<byte[]> Sent) MakePlayer(uint id, GameState state)
    {
        var sent = new List<byte[]>();
        var session = Mock.Of<ISession>();
        session.SendPacket(Any<byte[]>()).Callback((byte[] bytes) => sent.Add(bytes));
        var connection = new GameConnection(session.Object) { State = state };
        var character = new Character(new UnitCustomModelParams()) { Id = id, Connection = connection };
        connection.ActiveChar = character;
        return (connection, character, sent);
    }

    private ShutdownLogout Make(params Character[] inWorld) =>
        new(() => inWorld, (connection, character) =>
        {
            _granted.Add((connection, character));
            LeaveLikeTheServer(connection);
            return true;
        }, SavesFine);

    private static bool SavesFine(Character character) => true;

    // What EnterWorldManager.LeaveWorldTask does to the connection.
    private static void LeaveLikeTheServer(GameConnection connection)
    {
        connection.State = GameState.Lobby;
        connection.ActiveChar = null;
    }

    [Test]
    public async Task Begin_TellsOnlyPlayersInTheWorld()
    {
        var inWorld = MakePlayer(1, GameState.World);
        var inLobby = MakePlayer(2, GameState.Lobby);
        var orphan = new Character(new UnitCustomModelParams()) { Id = 3 };
        var logout = Make(inWorld.Character, inLobby.Character, orphan);

        var told = logout.Begin(TimeSpan.FromSeconds(5));

        await Assert.That(told).IsEqualTo(1);
        await Assert.That(logout.Pending).IsEqualTo(1);
        await Assert.That(inWorld.Sent.Count).IsEqualTo(1);
        await Assert.That(inLobby.Sent.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Begin_TakesOverALogoutAlreadyCountingDown()
    {
        var player = MakePlayer(1, GameState.World);
        var cancel = new CancellationTokenSource();
        player.Connection.CancelTokenSource = cancel;
        player.Connection.LeaveTask = Task.Delay(Timeout.Infinite, cancel.Token);
        var logout = Make(player.Character);

        logout.Begin(TimeSpan.FromSeconds(5));

        await Assert.That(cancel.IsCancellationRequested).IsTrue();
        await Assert.That(player.Connection.LeaveTask == null).IsTrue();
        await Assert.That(player.Sent.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Grant_CompletesEveryPendingLeaveOnce_SkipsPlayersWhoLeftMeanwhile()
    {
        var staying = MakePlayer(1, GameState.World);
        var gone = MakePlayer(2, GameState.World);
        var logout = Make(staying.Character, gone.Character);
        logout.Begin(TimeSpan.FromSeconds(5));
        gone.Connection.ActiveChar = null; // finished their own logout during the countdown

        var granted = logout.Grant();

        await Assert.That(granted).IsEqualTo(1);
        await Assert.That(_granted.Count).IsEqualTo(1);
        await Assert.That(_granted[0].Character.Id).IsEqualTo(1u);
        await Assert.That(logout.Pending).IsEqualTo(0);
        await Assert.That(logout.Grant()).IsEqualTo(0);
        await Assert.That(_granted.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Grant_OneFailingLeaveDoesNotStopTheOthers()
    {
        var first = MakePlayer(1, GameState.World);
        var second = MakePlayer(2, GameState.World);
        var reached = new List<uint>();
        var logout = new ShutdownLogout(() => [first.Character, second.Character], (_, character) =>
        {
            reached.Add(character.Id);
            if (character.Id == 1)
                throw new InvalidOperationException("synthetic");
            return true;
        }, SavesFine);
        logout.Begin(TimeSpan.FromSeconds(5));

        var granted = logout.Grant();

        await Assert.That(granted).IsEqualTo(1);
        await Assert.That(reached).IsEquivalentTo(new List<uint> { 1, 2 });
        // The thrown leave may have taken the character out of the world unsaved.
        await Assert.That(logout.Unsaved).IsEqualTo(1);
    }

    [Test]
    public async Task RunAsync_WithNobodyInTheWorld_ReturnsAtOnce()
    {
        var logout = Make();
        var watch = Stopwatch.StartNew();

        await logout.RunAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        await Assert.That(watch.Elapsed).IsLessThan(TimeSpan.FromSeconds(5));
        await Assert.That(_granted.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RunAsync_CountsDownThenGrants()
    {
        var player = MakePlayer(1, GameState.World);
        var logout = Make(player.Character);

        await logout.RunAsync(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(10));

        await Assert.That(player.Sent.Count).IsEqualTo(1);
        await Assert.That(_granted.Count).IsEqualTo(1);
        await Assert.That(player.Connection.ActiveChar).IsNull();
    }

    [Test]
    public async Task Begin_ClosesWorldEntryBeforeCollectingPlayers()
    {
        var player = MakePlayer(1, GameState.World);
        var closed = false;
        var closedWhenCollected = false;
        var logout = new ShutdownLogout(() =>
        {
            closedWhenCollected = closed;
            return [player.Character];
        }, (_, _) => true, SavesFine, () => closed = true);

        logout.Begin(TimeSpan.FromSeconds(5));

        await Assert.That(closed).IsTrue();
        await Assert.That(closedWhenCollected).IsTrue();
    }

    [Test]
    public async Task RunAsync_LogsOutAPlayerWhoEnteredDuringTheFirstRound()
    {
        var first = MakePlayer(1, GameState.World);
        var late = MakePlayer(2, GameState.World);
        var inWorld = new List<Character> { first.Character };
        var logout = new ShutdownLogout(() => inWorld.ToList(), (connection, character) =>
        {
            _granted.Add((connection, character));
            inWorld.Remove(character);
            LeaveLikeTheServer(connection);
            if (character.Id == 1)
                inWorld.Add(late.Character); // finished entering while the first round ran
            return true;
        }, SavesFine);

        await logout.RunAsync(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10));

        await Assert.That(_granted.Select(g => g.Character.Id).ToList()).IsEquivalentTo(new List<uint> { 1, 2 });
        await Assert.That(late.Sent.Count).IsEqualTo(1);
        await Assert.That(first.Sent.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RunAsync_GraceStartsOnlyAfterTheGrantsFinish()
    {
        var player = MakePlayer(1, GameState.World);
        var watch = Stopwatch.StartNew();
        var grantsDoneAt = TimeSpan.Zero;
        var logout = new ShutdownLogout(() => [player.Character], (connection, _) =>
        {
            Thread.Sleep(150); // a slow save
            LeaveLikeTheServer(connection);
            grantsDoneAt = watch.Elapsed;
            return true;
        }, SavesFine);
        var grace = TimeSpan.FromMilliseconds(200);

        await logout.RunAsync(TimeSpan.FromMilliseconds(10), grace);

        // Small allowance for the timer resolution of Task.Delay.
        await Assert.That(watch.Elapsed - grantsDoneAt).IsGreaterThanOrEqualTo(grace - TimeSpan.FromMilliseconds(20));
    }

    [Test]
    public async Task RunAsync_GivesUpAfterMaxRounds()
    {
        var player = MakePlayer(1, GameState.World);
        var grants = 0;
        // Never leaves: stands for a player who re-enters every round.
        var logout = new ShutdownLogout(() => [player.Character], (_, _) =>
        {
            grants++;
            return true;
        }, SavesFine);

        await logout.RunAsync(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        await Assert.That(grants).IsEqualTo(ShutdownLogout.MaxRounds);
        await Assert.That(player.Sent.Count).IsEqualTo(ShutdownLogout.MaxRounds);
    }

    [Test]
    public async Task Grant_KeepsAFailedSaveUntilARetrySucceeds()
    {
        var fine = MakePlayer(1, GameState.World);
        var failing = MakePlayer(2, GameState.World);
        var retries = 0;
        var logout = new ShutdownLogout(() => [fine.Character, failing.Character], (connection, character) =>
        {
            LeaveLikeTheServer(connection);
            return character.Id != 2; // the database refused player 2's save
        }, character =>
        {
            retries++;
            return retries > 1; // fails once more, then goes through
        });
        logout.Begin(TimeSpan.FromSeconds(5));

        var granted = logout.Grant();

        // The leave itself completed for both: both clients got their grant and flushed their UI data.
        await Assert.That(granted).IsEqualTo(2);
        await Assert.That(logout.Unsaved).IsEqualTo(1);
        await Assert.That(logout.SaveUnsaved()).IsFalse();
        await Assert.That(logout.Unsaved).IsEqualTo(1);
        await Assert.That(logout.SaveUnsaved()).IsTrue();
        await Assert.That(logout.Unsaved).IsEqualTo(0);
        await Assert.That(retries).IsEqualTo(2);
    }

    [Test]
    public async Task SaveUnsaved_WithNothingFailed_IsDoneWithoutSaving()
    {
        var saves = 0;
        var logout = new ShutdownLogout(() => [], (_, _) => true, _ => ++saves > 0);

        await Assert.That(logout.SaveUnsaved()).IsTrue();
        await Assert.That(saves).IsEqualTo(0);
    }
}
