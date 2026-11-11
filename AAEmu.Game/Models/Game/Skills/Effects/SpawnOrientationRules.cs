namespace AAEmu.Game.Models.Game.Skills.Effects;

/// <summary>
/// A spawn placed around a unit and oriented by that same unit faces it, turned by <c>ori_angle</c>
/// (0 faces the unit, 180 faces away). Forward is (-sin yaw, cos yaw).
/// </summary>
public static class SpawnOrientationRules
{
    public static bool FacesOrientationUnit(bool sameUnit, float distance) => sameUnit && distance > 0f;

    public static float FacingYaw(float spawnX, float spawnY, float unitX, float unitY, float oriAngleDeg) =>
        MathF.Atan2(-(unitX - spawnX), unitY - spawnY) + oriAngleDeg * MathF.PI / 180f;
}
