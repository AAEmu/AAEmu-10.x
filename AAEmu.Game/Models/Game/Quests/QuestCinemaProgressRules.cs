using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Progress ObjCinema is client-driven. When the accept packet arrived during
/// the load screen the first play attempt can fail; world-entry must ask again
/// while the objective is still unmet.
/// </summary>
public static class QuestCinemaProgressRules
{
    public static bool ShouldReplayOnWorldEntry(
        QuestComponentKind step,
        uint progressCinemaId,
        int cinemaObjectiveCount) =>
        step == QuestComponentKind.Progress
        && progressCinemaId != 0
        && cinemaObjectiveCount < 1;

    /// <summary>
    /// CSStartedCinema during the load screen is a failed login2 path lookup, not
    /// a watched film — only credit after world entry.
    /// </summary>
    public static bool ShouldCreditCinemaStarted(bool worldEntryCompleted) => worldEntryCompleted;
}
