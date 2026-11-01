namespace AAEmu.Game.Models.Game.Skills.Effects;

/// <summary>
/// When kill-without-corpse may remove the unit after the rest of the same buff-event effects run.
/// </summary>
/// <remarks>
/// Timeout rows often list KillNpcWithoutCorpse first and SpecialEffect SkillUse later (Whalesong
/// 21093). Immediate <c>RemoveNpcNow</c> unregisters the caster before the scheduled SkillUse
/// lands, so the strip skill misses and Duration-0 peace Clouts stay armed. Deferring one task
/// tick keeps the unit in-world for delay-0 SkillUse on the same event.
/// </remarks>
public static class KillNpcWithoutCorpseDeferRules
{
    /// <summary>Hold before <c>RemoveNpcNow</c> / despawn so same-event SkillUse can cast.</summary>
    public const int DeferRemovalMs = 1;

    /// <summary>
    /// True when removal must wait for the rest of this effect wave (always for a live victim).
    /// </summary>
    public static bool ShouldDeferRemoval(bool victimAlive) => victimAlive;
}
