using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.NPChar;

/// <summary>
/// When ZoneAuthority owns AI, aggressive NPCs must receive a World-authored aggro entry for a
/// nearby Character — the dedicate's sight path does not reliably pick players for almighty /
/// tower-wave templates the way it does for World-side remnant targets.
/// </summary>
public static class ZonePlayerAggroRules
{
    /// <summary>Same metres band <c>NpcSpawnerSpawnEffect</c> uses for mutual aggro.</summary>
    public const float BaseSightMetres = 30f;

    public static float SightMetres(float sightRangeScale) =>
        Math.Max(1f, sightRangeScale) * BaseSightMetres;

    public static float SightMetresSquared(float sightRangeScale)
    {
        var metres = SightMetres(sightRangeScale);
        return metres * metres;
    }

    /// <summary>
    /// Whether this NPC should publish aggro toward <paramref name="playerObjId"/>.
    /// A living non-player <paramref name="currentTarget"/> (e.g. the Bound Remnant) blocks the seed
    /// so World does not yank them mid-fight; a dead, self, or missing target does not.
    /// </summary>
    public static bool ShouldSeed(
        bool zoneAuthority,
        bool aggression,
        bool canAttackPlayer,
        bool npcAlive,
        bool playerOnline,
        float distanceSquared,
        float sightMetresSquared,
        BaseUnit currentTarget,
        uint playerObjId,
        uint npcObjId = 0)
    {
        if (!zoneAuthority || !aggression || !canAttackPlayer || !npcAlive || !playerOnline)
            return false;

        if (distanceSquared > sightMetresSquared)
            return false;

        if (HasBlockingTarget(currentTarget, playerObjId, npcObjId))
            return false;

        return true;
    }

    public static bool HasBlockingTarget(BaseUnit currentTarget, uint playerObjId, uint npcObjId = 0)
    {
        if (currentTarget == null || currentTarget.ObjId == 0)
            return false;

        if (currentTarget.ObjId == playerObjId)
            return false;

        // OnSpawn scripts often leave CurrentTarget on the caster itself — that must not block.
        if (npcObjId != 0 && currentTarget.ObjId == npcObjId)
            return false;

        if (currentTarget is Unit unit && (unit.Hp <= 0 || unit.IsDead))
            return false;

        // Living NPC / mate / slave — leave them on that fight.
        return currentTarget is not Character;
    }
}
