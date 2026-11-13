namespace AAEmu.Game.Models.Game.Teleport;

/// <summary>
/// Same-instance return is a seamless <c>SCTeleportUnit</c>. Crossing instances
/// is the only path that may send <c>SCLoadInstance</c>.
/// </summary>
public static class ReturnTeleportRules
{
    public static bool NeedsInstanceLoad(uint currentInstanceId, uint destInstanceId) =>
        currentInstanceId != destInstanceId;

    /// <summary>
    /// Origin is the unplaced default. Return must never resolve there —
    /// missing catalog, fallback, or a same-world <c>SCLoadInstance</c>
    /// that answers <c>CSTeleportEnded</c> at (0,0,0) all plant the unit
    /// off the map (no character pip).
    /// </summary>
    public static bool HasValidDestination(float x, float y, float z) =>
        x != 0f || y != 0f || z != 0f;

    public static bool ShouldApplyEndedPosition(float x, float y, float z) =>
        HasValidDestination(x, y, z);

    public static uint LoadWorldId(uint portalWorldId, uint defaultWorldTemplateId) =>
        portalWorldId != 0 ? portalWorldId : defaultWorldTemplateId;

    /// <summary>
    /// A return point in the zone the character is in while inside a copy (a dungeon's own portal) lands in
    /// that copy; the zone does not exist in the main world.
    /// </summary>
    public static bool StaysInCurrentCopy(uint destZoneId, uint currentZoneId, uint currentInstanceId) =>
        currentInstanceId != 0 && destZoneId != 0 && destZoneId == currentZoneId;
}
