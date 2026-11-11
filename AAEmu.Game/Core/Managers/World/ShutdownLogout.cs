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

    /// <summary>
    /// Rounds <see cref="RunAsync"/> runs at most: the first logs out everyone in the world, the next ones catch
    /// a player whose world entry was already under way when entry was closed.
    /// </summary>
    public const int MaxRounds = 3;

    private static volatile bool _worldEntryClosed;

    private readonly Func<IEnumerable<Character>> _inWorld;
    private readonly Func<GameConnection, Character, bool> _grant;
    private readonly Func<Character, bool> _save;
    private readonly Action _closeWorldEntry;
    private readonly List<(GameConnection Connection, Character Character)> _leaving = [];
    private readonly List<Character> _unsaved = [];

    /// <param name="inWorld">The characters currently in the world.</param>
    /// <param name="grant">
    /// Completes one player's leave (saves, removes the character from the world and sends SCLeaveWorldGranted);
    /// false when the character's save failed.
    /// </param>
    /// <param name="save">Saves a character again whose save failed during its leave; false when it failed again.</param>
    /// <param name="closeWorldEntry">Stops new world entries; called before the players are collected.</param>
    public ShutdownLogout(Func<IEnumerable<Character>> inWorld, Func<GameConnection, Character, bool> grant,
        Func<Character, bool> save, Action closeWorldEntry = null)
    {
        _inWorld = inWorld ?? throw new ArgumentNullException(nameof(inWorld));
        _grant = grant ?? throw new ArgumentNullException(nameof(grant));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _closeWorldEntry = closeWorldEntry;
    }

    /// <summary>
    /// Set once a server shutdown logout has begun; character selection then refuses to enter the world, so
    /// nobody arrives after the leave handshake and loses their UI data at the final save. Connections stay
    /// open, so the clients that were just sent to character select can still deliver their UI saves.
    /// </summary>
    public static bool WorldEntryClosed => _worldEntryClosed;

    /// <summary>The live wiring: WorldManager's character list and EnterWorldManager's leave completion.</summary>
    public static ShutdownLogout ForServer() => new(
        () => WorldManager.Instance.GetAllCharacters(),
        (connection, character) =>
            EnterWorldManager.Instance.LeaveWorldAndSave(connection, LeaveWorldTargetType.CharacterSelect, character),
        character => character.SaveDirectlyToDatabase(),
        () => _worldEntryClosed = true);

    /// <summary>Players told to leave by <see cref="Begin"/> and not yet granted.</summary>
    public int Pending => _leaving.Count;

    /// <summary>
    /// Characters whose save failed during their leave. They are no longer in the world, so the final world save
    /// does not reach them: <see cref="SaveUnsaved"/> has to succeed before the server may stop.
    /// </summary>
    public int Unsaved => _unsaved.Count;

    /// <summary>
    /// Closes world entry, then sends the leave countdown to every player in the world. A logout a player already
    /// started is taken over, so its own timer cannot complete the leave a second time. Returns how many players
    /// were told.
    /// </summary>
    public int Begin(TimeSpan countdown)
    {
        _closeWorldEntry?.Invoke();
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
    /// flushes its UI data on the grant. A character whose save failed is kept for <see cref="SaveUnsaved"/>.
    /// Returns how many leaves were completed.
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
                if (!_grant(connection, character))
                {
                    Logger.Error("Shutdown: save failed for {0} - {1} on leave, retried before the server stops", character.Id, character.Name);
                    KeepUnsaved(character);
                }

                granted++;
            }
            catch (Exception e)
            {
                // The leave may have stopped after the character left the world but before its save.
                Logger.Error(e, "Shutdown: leave failed for {0} - {1}", character.Id, character.Name);
                KeepUnsaved(character);
            }
        }

        _leaving.Clear();
        return granted;
    }

    /// <summary>
    /// Countdown, grant and grace until nobody is left in the world (at most <see cref="MaxRounds"/> rounds).
    /// The grace starts only once every grant of the round is done. Returns at once when nobody is in the world.
    /// </summary>
    public async Task RunAsync(TimeSpan countdown, TimeSpan grace, CancellationToken cancellationToken = default)
    {
        for (var round = 0; round < MaxRounds; round++)
        {
            if (Begin(countdown) == 0)
                return;

            await Task.Delay(countdown, cancellationToken);
            if (Grant() > 0)
                await Task.Delay(grace, cancellationToken);
        }

        var left = _inWorld().Count(character => character?.Connection?.State == GameState.World);
        if (left > 0)
            Logger.Warn("Shutdown: {0} player(s) still in the world after {1} logout rounds are saved without their UI data", left, MaxRounds);
    }

    /// <summary>
    /// Saves again every character whose save failed during its leave. Returns true once none is left unsaved;
    /// the ones that fail again stay for the next call.
    /// </summary>
    public bool SaveUnsaved()
    {
        for (var i = _unsaved.Count - 1; i >= 0; i--)
        {
            var character = _unsaved[i];
            try
            {
                if (_save(character))
                {
                    _unsaved.RemoveAt(i);
                    continue;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Shutdown: save retry threw for {0} - {1}", character.Id, character.Name);
            }

            Logger.Error("Shutdown: save retry failed for {0} - {1}", character.Id, character.Name);
        }

        return _unsaved.Count == 0;
    }

    private void KeepUnsaved(Character character)
    {
        if (!_unsaved.Contains(character))
            _unsaved.Add(character);
    }
}
