namespace AAEmu.Game.Models.Game.Skills.Effects;

/// <summary>Who a scoped world message reaches.</summary>
public static class WorldMessageAudience
{
    public static IEnumerable<T> ZoneGroupMembers<T>(IEnumerable<T> players, uint casterZoneGroupId, Func<T, uint> zoneGroupOf) =>
        casterZoneGroupId == 0 ? [] : players.Where(player => zoneGroupOf(player) == casterZoneGroupId);
}
