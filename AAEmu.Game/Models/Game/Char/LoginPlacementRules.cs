using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.World.Transform;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>
/// Login always lands in the main world. A character saved in a zone of another world (a dungeon copy that
/// is gone) or of no world at all cannot be hosted there and goes to its return district instead.
/// </summary>
public static class LoginPlacementRules
{
    public static bool MustLeaveSavedZone(uint savedZoneWorldId, uint mainWorldId) => savedZoneWorldId != mainWorldId;

    /// <summary>Moves <paramref name="character"/> to its return district when its saved zone cannot be hosted.</summary>
    public static void EnsureHostableZone(Character character)
    {
        var worldId = WorldManager.Instance.GetWorldIdByZoneKey(character.Transform.ZoneId);
        if (!MustLeaveSavedZone(worldId, WorldManager.DefaultWorldTemplateId))
            return;

        var returnPointId = PortalManager.Instance.GetDistrictReturnPoint(
            character.ReturnDistrictId, character.Faction.Id);
        var portal = PortalManager.Instance.GetRecallById(returnPointId);
        if (portal == null)
            return;

        character.Transform = new Transform(character, null, portal.ZoneId, WorldManager.DefaultInstanceId,
            portal.X, portal.Y, portal.Z, 0f, 0f, portal.Yaw.DegToRad());
    }
}
