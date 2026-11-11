using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.World.Transform;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>Moves a logging-in character to its return district when its saved zone cannot be hosted.</summary>
public static class LoginPlacement
{
    public static void EnsureHostableZone(Character character)
    {
        var worldId = WorldManager.Instance.GetWorldIdByZoneKey(character.Transform.ZoneId);
        if (!LoginPlacementRules.MustLeaveSavedZone(worldId, WorldManager.DefaultWorldTemplateId))
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
