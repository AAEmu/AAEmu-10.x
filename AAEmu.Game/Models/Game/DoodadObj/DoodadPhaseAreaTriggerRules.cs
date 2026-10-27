namespace AAEmu.Game.Models.Game.DoodadObj;

/// <summary>
/// Whether a phase change must drop attached area triggers before the new phase arms.
/// </summary>
/// <remarks>
/// Duration-0 Clouts (Whalesong peace 12312) never schedule a lifecycle task, so their
/// <c>AttachAreaTriggers</c> would otherwise leak across SkillHit phase hops and keep
/// re-applying the inside buff. Duration&gt;0 Clouts cancel their FuncTask on phase change
/// without running the task's RemoveAreaTrigger path — same clear.
/// </remarks>
public static class DoodadPhaseAreaTriggerRules
{
    /// <summary>
    /// True when this doodad still owns area triggers that belong to the phase being left.
    /// </summary>
    public static bool ShouldClearAttachedTriggers(int attachedCount) => attachedCount > 0;
}
