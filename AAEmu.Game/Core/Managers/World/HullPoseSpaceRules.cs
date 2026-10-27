using System.Numerics;

namespace AAEmu.Game.Core.Managers.World;

/// <summary>
/// Dedicate ship bodies are continent on ZW. When a report is zone-local by mistake, the World
/// mirror jumps multi-km onto the zone origin cell — soft-cull then despawns the hull for the
/// summoner and the client never sees the boat at the seed.
/// </summary>
public static class HullPoseSpaceRules
{
    /// <summary>Same band used for NPC mirror local-as-world poisoning.</summary>
    public const float PoisonJumpMetres = 2000f;

    /// <summary>
    /// When <paramref name="reported"/> is much farther from <paramref name="currentWorld"/> than
    /// the same point lifted through <paramref name="toWorld"/>, treat the report as zone-local.
    /// </summary>
    public static bool TryRepairLocalAsWorld(
        Vector3 currentWorld,
        Vector3 reported,
        Func<Vector3, Vector3> toWorld,
        out Vector3 repaired)
    {
        repaired = reported;
        if (toWorld == null)
            return false;

        var rawD2 = DistanceSq(reported, currentWorld);
        var threshold = PoisonJumpMetres * PoisonJumpMetres;
        if (rawD2 <= threshold)
            return false;

        var asWorld = toWorld(reported);
        var worldD2 = DistanceSq(asWorld, currentWorld);
        if (worldD2 >= rawD2 * 0.25f)
            return false;

        repaired = asWorld;
        return true;
    }

    private static float DistanceSq(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }
}
