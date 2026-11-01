using AAEmu.Game.Core.Managers;

namespace AAEmu.Game.Models.Game.Schedules;

/// <summary>
/// Pure aggregation for a template bound to one or more <c>game_schedules</c> rows.
/// Christmas trees / festival stands often carry many year windows — a stale Ended year must
/// not hide a later NotStarted year (cron arm / plant Decide).
/// </summary>
public static class GameSchedulePeriodRules
{
    public enum WindowKind
    {
        NotStarted = 0,
        InProgress,
        Ended,
    }

    public static WindowKind Classify(bool started, bool ended)
    {
        if (started && !ended)
            return WindowKind.InProgress;
        if (ended)
            return WindowKind.Ended;
        return WindowKind.NotStarted;
    }

    /// <summary>
    /// Overall status for plant / cron decisions.
    /// Any live window wins; else any pending window; else Ended only when every known window ended.
    /// </summary>
    public static GameScheduleManager.PeriodStatus Aggregate(IEnumerable<WindowKind> windows)
    {
        var hasInProgress = false;
        var hasNotStarted = false;
        var hasEnded = false;
        var any = false;

        if (windows != null)
        {
            foreach (var kind in windows)
            {
                any = true;
                switch (kind)
                {
                    case WindowKind.InProgress:
                        hasInProgress = true;
                        break;
                    case WindowKind.NotStarted:
                        hasNotStarted = true;
                        break;
                    case WindowKind.Ended:
                        hasEnded = true;
                        break;
                }
            }
        }

        if (!any)
            return GameScheduleManager.PeriodStatus.NotFound;
        if (hasInProgress)
            return GameScheduleManager.PeriodStatus.InProgress;
        if (hasNotStarted)
            return GameScheduleManager.PeriodStatus.NotStarted;
        if (hasEnded)
            return GameScheduleManager.PeriodStatus.Ended;
        return GameScheduleManager.PeriodStatus.NotStarted;
    }
}
