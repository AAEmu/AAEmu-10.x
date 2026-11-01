namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// Resolves the unit named by a <see cref="SkillCaster"/> when the world table may already
/// have dropped it (same-event despawn before a nested SkillUse).
/// </summary>
public static class SkillCasterLookupRules
{
    /// <summary>
    /// Prefer the live world lookup; fall back to the held caster object when it is the same id.
    /// </summary>
    public static bool PreferHeldCasterWhenLookupMisses(
        uint skillCasterObjId,
        uint heldCasterObjId,
        bool lookupFound) =>
        !lookupFound && skillCasterObjId != 0 && skillCasterObjId == heldCasterObjId;
}
