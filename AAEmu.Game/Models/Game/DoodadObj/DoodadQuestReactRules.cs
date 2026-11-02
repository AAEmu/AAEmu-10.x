using AAEmu.Game.Models.Game.Quests.Static;

namespace AAEmu.Game.Models.Game.DoodadObj;

/// <summary>
/// Compact <c>doodad_func_quest_reacts</c> match. A row moves the doodad when that
/// character's quest status (and optional component) matches.
/// </summary>
public static class DoodadQuestReactRules
{
    public static bool MatchesStatus(uint requiredStatusId, QuestStatus actualStatus)
    {
        return requiredStatusId == (uint)actualStatus;
    }

    /// <summary>
    /// <paramref name="requiredComponentId"/> 0 means any component. A Ready-step row may name
    /// the Ready component even when the live <c>ComponentId</c> was cleared on the last step.
    /// </summary>
    public static bool MatchesComponent(
        uint requiredComponentId,
        uint activeComponentId,
        bool readyStepContainsRequiredComponent)
    {
        if (requiredComponentId == 0)
            return true;
        if (activeComponentId == requiredComponentId)
            return true;
        return readyStepContainsRequiredComponent;
    }

    public static bool ShouldAdvance(int nextPhase, uint currentFuncGroupId)
    {
        return nextPhase > 0 && (uint)nextPhase != currentFuncGroupId;
    }

    /// <summary>
    /// QuestReact is per viewer. The shared persisted phase must not move.
    /// </summary>
    public static bool ShouldMutateSharedPhase() => false;

    /// <summary>
    /// A timer or normal func that hops the shared phase must drop overlays
    /// computed against the previous group.
    /// </summary>
    public static bool ShouldInvalidateViewerPhases(uint previousSharedPhase, uint nextSharedPhase) =>
        previousSharedPhase != nextSharedPhase;

    public static bool ShouldKeepViewerPhase(uint sharedPhase, uint viewerPhase) =>
        viewerPhase != 0 && viewerPhase != sharedPhase;

    /// <summary>
    /// A func used on a client doodad's per-viewer overlay advances that viewer only. Client
    /// doodads are one copy per player in the official client; the shared phase stays put.
    /// </summary>
    public static bool ShouldAdvanceViewerOnly(bool clientDoodad, uint usePhase, uint sharedPhase) =>
        clientDoodad && usePhase != sharedPhase;

    /// <summary>
    /// A player's final use (<c>next_phase</c> -1) consumes the doodad. A client doodad is one copy
    /// per player in the official client, so the shared copy must stay for everyone else; that
    /// player's own view moves on through QuestReact.
    /// </summary>
    public static bool ShouldConsumeOnFinalUse(bool clientDoodad, bool usedByPlayer) =>
        !(clientDoodad && usedByPlayer);

    public static uint NextViewerPhase(uint currentPhase, int reactNextPhase) =>
        ShouldAdvance(reactNextPhase, currentPhase) ? (uint)reactNextPhase : currentPhase;

    public const int MaxViewerHops = 8;
}
