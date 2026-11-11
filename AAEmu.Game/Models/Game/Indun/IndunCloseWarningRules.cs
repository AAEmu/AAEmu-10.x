namespace AAEmu.Game.Models.Game.Indun;

/// <summary>Which close warning is due while a copy counts down its end window.</summary>
public static class IndunCloseWarningRules
{
    /// <summary>The threshold to announce now, below <paramref name="lastAnnounced"/>; null when none is due.</summary>
    public static int? Next(int secondsLeft, int lastAnnounced, IReadOnlyList<int> thresholds)
    {
        int? due = null;
        foreach (var threshold in thresholds)
        {
            if (secondsLeft <= threshold && threshold < lastAnnounced && (due == null || threshold < due))
                due = threshold;
        }

        return due;
    }

    public static string Message(int seconds) => seconds >= 60
        ? $"This dungeon closes in {seconds / 60} minute{(seconds / 60 == 1 ? "" : "s")}."
        : $"This dungeon closes in {seconds} seconds.";
}
