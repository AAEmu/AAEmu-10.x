using System.Diagnostics;

using AAEmu.Commons.Network.Core;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Core.Managers.World;

/// <summary>
/// The shutdown logout has to reach every player in the world exactly once, leave lobby connections and
/// players who left on their own alone, and take over a logout that is already counting down.
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
            connection.ActiveChar = null;
        });

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
        });
        logout.Begin(TimeSpan.FromSeconds(5));

        var granted = logout.Grant();

        await Assert.That(granted).IsEqualTo(1);
        await Assert.That(reached).IsEquivalentTo(new List<uint> { 1, 2 });
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
}
