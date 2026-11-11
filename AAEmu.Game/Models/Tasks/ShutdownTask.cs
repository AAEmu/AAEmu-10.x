using System.Drawing;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets.G2C;

using NLog;

using DotNetTask = System.Threading.Tasks.Task;

namespace AAEmu.Game.Models.Tasks;

public class ShutdownTask : Task
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    // private DateTime _lastCheckTime = DateTime.MinValue;
    // 0 until the shutdown sequence starts; set once with Interlocked so overlapping runs of this task cannot start it twice.
    private int _shuttingDown;
    private DateTime _shutdownTime;
    private DateTime _nextTriggerTime = DateTime.MinValue;
    private readonly int _exitCode;
    // Timings to show a popup, in seconds
    private readonly List<uint> _triggerPoints =
    [
        14400, // 4h
        10800, // 3h
        7200, // 2h
        3600, // 60m
        2700, // 45m
        1800, // 30m
        1200, // 20m
        900, // 15m
        600, // 10m
        300, // 5m
        240, // 4m
        180, // 3m
        120, // 2m
        90,
        60, // 1m
        45,
        30,
        20,
        10
    ];

    public ShutdownTask(DateTime shutdownTime, int exitCode)
    {
        _shutdownTime = shutdownTime;
        _exitCode = exitCode;
        _nextTriggerTime = CalculateLargestNextTrigger();
    }

    /// <summary>
    /// True once the shutdown sequence has started (players are being logged out): from then on it can no longer
    /// be cancelled or moved, only forced.
    /// </summary>
    public bool Started => Volatile.Read(ref _shuttingDown) != 0;

    private DateTime CalculateLargestNextTrigger()
    {
        var remaining = _shutdownTime - DateTime.UtcNow;
        var remainingDelta = Math.Ceiling(remaining.TotalSeconds);
        foreach (var triggerPoint in _triggerPoints)
        {
            if (remainingDelta > triggerPoint)
                return _shutdownTime - TimeSpan.FromSeconds(triggerPoint);
        }

        return DateTime.MaxValue;
    }

    public void ChangeShutdownTime(DateTime shutdownTime)
    {
        _shutdownTime = shutdownTime;
        var remaining = (int)Math.Ceiling((_shutdownTime - DateTime.UtcNow).TotalMinutes);
        WorldManager.Instance.BroadcastPacketToServer(new SCNoticeMessagePacket(3, Color.Red, 10000, $"Server shutdown has been rescheduled to {remaining} minutes from now!"));
        _nextTriggerTime = CalculateLargestNextTrigger();
    }

    public override void Execute()
    {
        if (_shutdownTime <= DateTime.UtcNow)
        {
            ExecuteShutdown();
            return;
        }

        if (Started)
            return;

        var remainingTime = _shutdownTime - DateTime.UtcNow;
        var remainingSeconds = (int)Math.Ceiling(remainingTime.TotalSeconds);
        if (DateTime.UtcNow < _nextTriggerTime)
            return;

        _nextTriggerTime = CalculateLargestNextTrigger();

        var showSeconds = remainingSeconds <= 100;
        var showMinutes = remainingSeconds <= 3600 && !showSeconds;
        var showHours = remainingSeconds > 3600 && !showSeconds; ;

        var shutdownText = "The server is shutting down soon!";
        var popupTime = 3000;

        if (showHours)
        {
            shutdownText = $"The server is shutting down in {remainingSeconds / 3600} hours!";
            popupTime = 10000;
        }
        else if (showMinutes)
        {
            shutdownText = $"The server is shutting down in {remainingSeconds / 60} minutes!";
            popupTime = 6000;
        }
        else if (showSeconds)
        {
            shutdownText = $"The server is shutting down in {remainingSeconds} seconds!";
            popupTime = 3000;
        }

        WorldManager.Instance.BroadcastPacketToServer(
            new SCNoticeMessagePacket(3,
                Color.Red,
                popupTime,
                shutdownText));
    }

    /// <summary>
    /// Starts the shutdown once, however often the task runs after the shutdown time and even when its runs
    /// overlap: players are sent to character select through the normal leave handshake (their clients only send
    /// the quest tracker and the rest of their UI data when they leave the world, see <see cref="ShutdownLogout"/>),
    /// then the world is saved and the process exits.
    /// </summary>
    private void ExecuteShutdown()
    {
        if (Interlocked.Exchange(ref _shuttingDown, 1) != 0)
            return;

        // Wipe all BuyBack containers manually, as there will be no trigger for disconnected players
        try
        {
            foreach (var character in WorldManager.Instance.GetAllCharacters())
                character.BuyBackItems.Wipe();
        }
        catch (Exception)
        {
            // Ignore
        }

        _ = RunShutdownAsync();
    }

    /// <summary>
    /// The whole sequence in one flow, so nothing else can save and exit while it runs: the grace for the UI saves
    /// starts only after every grant is done, and the final save waits for the logout to finish and for every
    /// character whose save failed on leave to be saved.
    /// </summary>
    private async DotNetTask RunShutdownAsync()
    {
        var logout = ShutdownLogout.ForServer();
        try
        {
            await logout.RunAsync(ShutdownLogout.DefaultCountdown, ShutdownLogout.DefaultGrace);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Shutdown: logout failed, saving without the remaining UI data");
        }

        while (true)
        {
            try
            {
                // Characters that left the world with a failed save are not in the world save below.
                if (!logout.SaveUnsaved())
                {
                    Logger.Error("Shutdown: {0} character(s) not saved yet, retrying", logout.Unsaved);
                }
                else
                {
                    FactionPowerManager.Instance.Save();
                    if (SaveManager.Instance.DoSave())
                    {
                        WorldManager.Instance.BroadcastPacketToServer(new SCNoticeMessagePacket(3, Color.Magenta, 15000,
                            "The server is shutting down right now!"));
                        Environment.Exit(_exitCode); // Manual Shutdown
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Shutdown: final save failed, retrying");
            }

            // A save that did not go through (one already running, say) is retried, as the once-a-second task did.
            await DotNetTask.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
