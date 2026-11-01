namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// When ZoneAuthority AI keeps casting after World already dropped the mirror (ZWRemoveNpc
/// while the dedicate AI object leaked), <c>ZWStartSkill</c> arrives for a missing caster.
/// Continuous-summon Whalesong NPCs are the measured case: World deletes the mirror, Zone still
/// ticks the skill, and wave <c>NpcSpawnerSpawnEffect</c> casts time out — the bay looks idle.
/// </summary>
public static class ZoneOrphanCasterRules
{
    /// <summary>
    /// Rematerialize a recently-removed zone mirror so World can answer the cast. Any skill is
    /// eligible: spawn-relay skills need it to keep waves flowing; combat skills need it so the
    /// leaked AI can still land hits instead of timing out forever on an unresolved caster.
    /// </summary>
    public static bool ShouldRematerialize(
        bool casterMissing,
        bool hasRecentRemoval) =>
        casterMissing && hasRecentRemoval;
}
