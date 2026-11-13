namespace AAEmu.Game.Models.Game.Char;

/// <summary>Login lands in the main world; a saved zone of another world, or of none, cannot be hosted there.</summary>
public static class LoginPlacementRules
{
    public static bool MustLeaveSavedZone(uint savedZoneWorldId, uint mainWorldId) => savedZoneWorldId != mainWorldId;
}
