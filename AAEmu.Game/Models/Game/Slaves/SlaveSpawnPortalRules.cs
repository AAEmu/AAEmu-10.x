namespace AAEmu.Game.Models.Game.Slaves;

/// <summary>
/// UnitState flags bit 11 (portal arrival fx) and a short stream grace so soft-cull cannot
/// cancel that fx with SCUnitsRemoved before the client finishes the portal.
/// </summary>
public static class SlaveSpawnPortalRules
{
    /// <summary>
    /// Soft-cull must not drop a freshly summoned hull: SCUnitsRemoved cancels the portal fx
    /// and the re-stream has no bit 11, so the ship pops in with no animation (or never lands
    /// near the summoner when a bad zone pose also pushed it out of the Ship AOI).
    /// </summary>
    public static readonly TimeSpan StreamGrace = TimeSpan.FromSeconds(5);

    public static bool ShouldSetPortalFlag(bool hideSpawnEffect) => !hideSpawnEffect;

    /// <summary>
    /// Bit 11 is one-shot: the first SCUnitState that paints the hull consumes it. Later AOI
    /// re-streams must not replay the portal.
    /// </summary>
    public static bool ConsumePortalFlag(ref bool pendingSpawnPortal)
    {
        if (!pendingSpawnPortal)
            return false;
        pendingSpawnPortal = false;
        return true;
    }

    public static bool ShouldKeepStreamedDuringSpawn(
        bool pendingSpawnPortal, DateTime spawnTimeUtc, DateTime nowUtc) =>
        pendingSpawnPortal || nowUtc - spawnTimeUtc < StreamGrace;
}
