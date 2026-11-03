namespace AAEmu.Game.Models.Game.Quests;

/// <summary>
/// Sphere accepts (starter list and quest_area reconcile) must wait until the
/// client finished loading. Accepting during SelectCharacter/load looks up the
/// Progress ObjCinema under the login world (`login2/cinemas/...`), the XML miss
/// still sends CSStartedCinema, and the objective completes with no film.
/// </summary>
public static class QuestStarterSphereRules
{
    public static bool ShouldAcceptOnEnter(bool worldEntryCompleted, bool oldInside, bool newInside) =>
        worldEntryCompleted && !oldInside && newInside;

    /// <summary>
    /// Keep the last check "outside" while entry is incomplete so the enter edge
    /// fires again after NotifyInGameCompleted.
    /// </summary>
    public static bool ShouldUpdateLastPosition(bool worldEntryCompleted, bool acceptedOrNotAnEnter) =>
        worldEntryCompleted || acceptedOrNotAnEnter;

    /// <summary>
    /// quest_area_sphere reconcile on SelectCharacter is what started Nuian 6839
    /// before NotifyInGameCompleted; hold it until entry so the client is on
    /// main_world when SCQuestContextStarted triggers PlayCinema.
    /// </summary>
    public static bool ShouldReconcileQuestArea(bool worldEntryCompleted) => worldEntryCompleted;

    public static bool ShouldAcceptClientSphere(bool worldEntryCompleted) => worldEntryCompleted;
}
