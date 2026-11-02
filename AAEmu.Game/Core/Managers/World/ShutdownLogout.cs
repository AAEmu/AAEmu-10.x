using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.StaticValues;

using NLog;

namespace AAEmu.Game.Core.Managers.World;

/// <summary>
/// Logs every player out through the normal leave-world handshake before the World stops.
/// </summary>
/// <remarks>
/// The 10.0.2.13 client keeps its UI data (quest tracker list and decals, journal check boxes, action bars,
/// map filters …) in memory and only sends CSSaveUIData when it leaves the world: "Save Game Option" in the
/// client log, once when the logout starts and once more on SCLeaveWorldGranted. Live World log 2026-11-02:
/// not one save in 2 h 20 of questing, twelve in the second a player pressed logout. A World that just closes
/// the sockets therefore keeps the tracker of the last graceful logout, which players see as "quest tracking
/// resets on reboot". Sending the leave countdown and the grant gives each client its flush; the saves are
/// written to <c>options</c> as they arrive (<see cref="CharacterOptionStore"/>), so a short grace after the
/// grant is enough before the final save and exit.
/// </remarks>
public sealed class ShutdownLogout
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    /// <summary>Countdown the clients see before they are moved to character select.</summary>
    public static readonly TimeSpan DefaultCountdown = TimeSpan.FromSeconds(5);

    /// <summary>Time the clients get after the grant to send their UI saves.</summary>
    public static readonly TimeSpan DefaultGrace = TimeSpan.FromSeconds(3);

    private readonly Func<IEnumerable<Character>> _inWorld;
    private readonly Action<GameConnection, Character> _grant;
    private readonly List<(GameConnection Connection, Character Character)> _leaving = [];

    /// <param name="inWorld">The characters currently in the world.</param>
    /// <param name="grant">Completes one player's leave (saves and sends SCLeaveWorldGranted).</param>
    public ShutdownLogout(Func<IEnumerable<Character>> inWorld, Action<GameConnection, Character> grant)
    {
        _inWorld = inWorld ?? throw new ArgumentNullException(nameof(inWorld));
        _grant = grant ?? throw new ArgumentNullException(nameof(grant));
    }

    /// <summary>The live wiring: WorldManager's character list and EnterWorldManager's leave completion.</summary>
    public static ShutdownLogout ForServer() => new(
        () => WorldManager.Instance.GetAllCharacters(),
        (connection, character) =>
            EnterWorldManager.Instance.LeaveWorldTask(connection, LeaveWorldTargetType.CharacterSelect, character));

    /// <summary>Players told to leave by <see cref="Begin"/> and not yet granted.</summary>
    public int Pending => _leaving.Count;

    /// <summary>
    /// Sends the leave countdown to every player in the world. A logout a player already started is taken over,
    /// so its own timer cannot complete the leave a second time. Returns how many players were told.
    /// </summary>
    public int Begin(TimeSpan countdown)
    {
        _leaving.Clear();
        foreach (var character in _inWorld())
        {
            var connection = character?.Connection;
            if (connection == null || connection.State != GameState.World)
                continue;

            if (connection.LeaveTask != null)
            {
                try
                {
                    connection.CancelTokenSource?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // The timer already ran out; LeaveWorldTask is running or done for this connection.
                }

                connection.LeaveTask = null;
            }

            connection.SendPacket(new SCPrepareLeaveWorldPacket(
                (int)countdown.TotalMilliseconds, LeaveWorldTargetType.CharacterSelect, false));
            _leaving.Add((connection, character));
        }

        if (_leaving.Count > 0)
            Logger.Info("Shutdown: {0} player(s) sent to character select, {1:F0} s countdown", _leaving.Count, countdown.TotalSeconds);
        return _leaving.Count;
    }

    /// <summary>
    /// Completes the leave for everyone told in <see cref="Begin"/> who is still in the world. Each client
    /// flushes its UI data on the grant. Returns how many leaves were completed.
    /// </summary>
    public int Grant()
    {
        var granted = 0;
        foreach (var (connection, character) in _leaving)
        {
            // Left on their own (logout finished, disconnect) while the countdown ran.
            if (connection.ActiveChar == null)
                continue;

            try
            {
                _grant(connection, character);
                granted++;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Shutdown: leave failed for {0} - {1}", character.Id, character.Name);
            }
        }

        _leaving.Clear();
        return granted;
    }

    /// <summary>Countdown, grant and grace in one go, for a host stop. Returns at once when nobody is in the world.</summary>
    public async Task RunAsync(TimeSpan countdown, TimeSpan grace, CancellationToken cancellationToken = default)
    {
        if (Begin(countdown) == 0)
            return;

        await Task.Delay(countdown, cancellationToken);
        var granted = Grant();
        if (granted == 0)
            return;

        await Task.Delay(grace, cancellationToken);
    }
}
