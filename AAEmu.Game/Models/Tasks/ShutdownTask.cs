using System.Drawing;
using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Core.Packets.G2C;

namespace AAEmu.Game.Models.Tasks;

public class ShutdownTask : Task
{
    // private DateTime _lastCheckTime = DateTime.MinValue;
    private bool _alreadyShuttingDown;
    private ShutdownLogout _logout;
    private bool _granted;
    private DateTime _grantAt;
    private DateTime _exitAt;
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

        if (_alreadyShuttingDown)
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
    /// Runs once a second after the shutdown time: players are sent to character select through the normal
    /// leave handshake (their clients only send the quest tracker and the rest of their UI data when they
    /// leave the world, see <see cref="ShutdownLogout"/>), then the world is saved and the process exits.
    /// </summary>
    private void ExecuteShutdown()
    {
        var now = DateTime.UtcNow;
        if (!_alreadyShuttingDown)
        {
            _alreadyShuttingDown = true;

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

            _logout = ShutdownLogout.ForServer();
            if (_logout.Begin(ShutdownLogout.DefaultCountdown) > 0)
            {
                _grantAt = now + ShutdownLogout.DefaultCountdown;
                _exitAt = _grantAt + ShutdownLogout.DefaultGrace;
                return;
            }

            _granted = true;
            _grantAt = now;
            _exitAt = now;
        }

        if (!_granted)
        {
            if (now < _grantAt)
                return;

            _granted = true;
            if (_logout.Grant() == 0)
                _exitAt = now;
        }

        if (now < _exitAt)
            return;

        FactionPowerManager.Instance.Save();
        if (SaveManager.Instance.DoSave())
        {
            WorldManager.Instance.BroadcastPacketToServer(new SCNoticeMessagePacket(3, Color.Magenta, 15000,
                "The server is shutting down right now!"));
            Environment.Exit(_exitCode); // Manual Shutdown
        }
    }
}
