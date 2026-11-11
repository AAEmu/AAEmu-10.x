using System.Numerics;

namespace AAEmu.Game.Models.Game.World.Interactions;

/// <summary>Where a summoned doodad stands: on the terrain when its summoner hovers just above it.</summary>
public static class SummonedDoodadPlacement
{
    /// <summary>Farther above the terrain than this, the summon is on something else (deck, roof, bridge).</summary>
    public const float MaxSnapDistance = 2f;

    public static float GroundZ(float summonZ, float terrainZ)
    {
        if (terrainZ <= 0f)
            return summonZ;
        var above = summonZ - terrainZ;
        return above > 0f && above <= MaxSnapDistance ? terrainZ : summonZ;
    }

    /// <summary>Farther than this from its spawn point, the summoner has moved and its spawn says nothing.</summary>
    public const float SameSpotMetres = 1f;

    /// <summary>
    /// A summon at the summoner's zone spawn point (recorded only when the mirror could not be grounded) stands on
    /// the ground under it: the spawn height less the dedicate's spawn lift, however the summoner has bobbed since.
    /// </summary>
    public static float UnliftedZ(Vector3 summon, Vector3? zoneSpawn)
    {
        if (zoneSpawn is not { } spawn)
            return summon.Z;
        var sideways = Vector2.Distance(new Vector2(summon.X, summon.Y), new Vector2(spawn.X, spawn.Y));
        var groundZ = spawn.Z - MirrorHeightRules.DedicateSpawnLiftMetres;
        return sideways <= SameSpotMetres && MathF.Abs(summon.Z - groundZ) <= MaxSnapDistance ? groundZ : summon.Z;
    }
}
