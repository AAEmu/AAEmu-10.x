namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// When <c>SpecialEffect SkillUse</c> runs the nested skill inline vs on the task queue.
/// </summary>
/// <remarks>
/// Delay 0 is authored as "cast now" (Whalesong buff 21093 → skill 36659 after the same
/// timeout's KillNpcWithoutCorpse). Scheduling even a 0 ms task lets a same-event despawn
/// unregister the caster before <c>Use</c>, which yields <c>NoTarget</c> and leaves peace
/// Clout buffs up.
/// </remarks>
public static class SkillUseScheduleRules
{
    /// <summary>
    /// True when the nested skill must run on this call stack, not via <c>TaskManager</c>.
    /// </summary>
    public static bool ShouldExecuteImmediately(int delayMs) => delayMs <= 0;
}
