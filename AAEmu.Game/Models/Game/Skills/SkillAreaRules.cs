using System.Linq;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// The shape of a skill's area: the cone <c>target_area_angle</c> / <c>front_angle</c> describe, the
/// corridor a <c>Line</c> selection describes, and where the area is gathered.
/// </summary>
/// <remarks>
/// The gather used to be a full sphere around the resolved target: <c>target_area_angle</c> was loaded and
/// never read, so a 90° cleave hit everything within <c>target_area_radius</c>, including units behind the
/// caster. 1,338 skills carry an angle below the 360 default and 26 carry 365, which is past a full
/// sweep and takes no cone; 33 of the 1,338 are ability skills.
///
/// <c>target_area_angle</c> is the full width of the cone, read the same way <c>aoe_shapes.value3</c> is:
/// <c>ConeHalfAngle</c> returns it and the gather halves it, so 120 means ±60. That is the reading
/// <c>AreaShape.SphereConeValue3IsFullSweep</c> settles for the shape column, and the two have to agree or
/// a skill's own cone and its shape's cone would disagree about where "in front" ends. 360 is "no cone"
/// and is skipped rather than halving to 180.
///
/// <c>front_angle</c> only supplies a cone when <c>target_area_angle</c> is the 360 default. 93 skills
/// carry a usable <c>front_angle</c> (above 0 and below 360): 54 of them have the area angle at its
/// default and are the ones this column actually shapes, and the other 39 also author a non-default
/// <c>target_area_angle</c>, which is the more specific statement and shadows them. A further 22 rows
/// carry 360 and 9 carry a negative angle (-110, -107 twice, -90 five times, -20) — the only negative
/// angle in either column — and both groups fall through to "no cone" on the guard below.
///
/// <c>target_selection_id</c> is 1 (Source) on 25,912 skills, 2 (Target) on 11,802 and 4 (Location) on 329.
/// No shipped row uses 3 (Line); the corridor rule is written anyway so that a row which does use it is
/// narrowed rather than silently treated as a full sphere. Source and Target keep the gather they have —
/// changing which unit 25,912 skills centre on is a live-behaviour change this task does not ask for and
/// no evidence in the content settles.
/// </remarks>
public static class SkillAreaRules
{
    /// <summary>The angle meaning "every bearing", which is also the DB default.</summary>
    public const int FullSweepDegrees = 360;

    /// <summary>
    /// The cone's half-angle in degrees, or 0 when the skill has no cone.
    /// </summary>
    public static double ConeHalfAngle(int targetAreaAngle, int frontAngle)
    {
        if (targetAreaAngle > 0 && targetAreaAngle < FullSweepDegrees)
            return targetAreaAngle;
        if (frontAngle > 0 && frontAngle < FullSweepDegrees)
            return frontAngle;
        return 0d;
    }

    /// <summary>
    /// Whether a bearing from the caster's facing is inside the cone. <paramref name="bearing"/> is
    /// <see cref="MathUtil.CalculateAngleFrom(Units.BaseUnit, Units.BaseUnit)"/>'s signed result, so a
    /// unit behind the caster reads near ±180.
    /// </summary>
    public static bool IsInsideCone(double bearing, double halfAngle)
        => halfAngle <= 0d || Math.Abs(MathUtil.ClampDegAngle(bearing)) <= halfAngle;

    /// <summary>
    /// Whether a <c>Line</c> selection reaches a unit: within <paramref name="halfWidth"/> of the segment
    /// from the caster to the cast target, using the X/Y plane the rest of the AoE code works in. The
    /// segment is capped at both ends, so the shape is a stadium rather than an infinite band: a unit
    /// beside the caster or beside the target is inside, one past either end is not.
    /// </summary>
    public static bool IsWithinCorridor(
        (float X, float Y) caster, (float X, float Y) target, (float X, float Y) unit, double halfWidth)
    {
        if (halfWidth <= 0d)
            return false;

        var segmentX = target.X - caster.X;
        var segmentY = target.Y - caster.Y;
        var lengthSquared = segmentX * segmentX + segmentY * segmentY;

        // A degenerate segment (the cast target is the caster) has no line to be near.
        if (lengthSquared <= float.Epsilon)
            return false;

        var projection = ((unit.X - caster.X) * segmentX + (unit.Y - caster.Y) * segmentY) / lengthSquared;
        // Only the part of the line between the caster and the target counts: past the target the area
        // stops, and behind the caster it never started.
        var clamped = Math.Clamp(projection, 0f, 1f);
        var closestX = caster.X + clamped * segmentX;
        var closestY = caster.Y + clamped * segmentY;
        var distanceX = unit.X - closestX;
        var distanceY = unit.Y - closestY;

        return distanceX * distanceX + distanceY * distanceY <= halfWidth * halfWidth;
    }

    /// <summary>
    /// Whether a selection gathers around the position the cast was aimed at rather than around the
    /// resolved unit. A <c>Location</c> cast is already resolved to a position pseudo-unit, so this only
    /// records the intent; <c>Line</c> narrows to the corridor instead.
    /// </summary>
    public static bool GathersAroundCastPosition(SkillTargetSelection selection)
        => selection == SkillTargetSelection.Location;

    /// <summary>Whether a selection narrows the gathered set to the caster-to-target corridor.</summary>
    public static bool UsesCorridor(SkillTargetSelection selection)
        => selection == SkillTargetSelection.Line;

    /// <summary>
    /// Whether the unit the area is gathered around must be kept in the hit list.
    /// </summary>
    /// <remarks>
    /// <c>WorldManager.GetAround</c> excludes the search center by ObjId. An AoE whose radius is
    /// measured from the cast target (Target-selection skills) would otherwise land on neighbours
    /// only and miss the unit the skill was aimed at. Source selection already re-added the center;
    /// Target needs the same keep.
    /// <para>
    /// Location casts use a temporary position marker (<see cref="uint.MaxValue"/>) as the gather
    /// center. That marker is not a hittable unit: adding it would pass faction filters, sort at
    /// distance zero, and steal a <c>target_area_count</c> slot from a real enemy.
    /// </para>
    /// </remarks>
    public static bool IncludesAreaCenterUnit(float areaRadius, BaseUnit centerUnit)
        => areaRadius > 0f && IsHittableAreaCenter(centerUnit);

    /// <summary>
    /// Whether <paramref name="centerUnit"/> is a real unit that can occupy an AoE hit slot.
    /// </summary>
    public static bool IsHittableAreaCenter(BaseUnit centerUnit)
        => centerUnit is { ObjId: > 0 and not uint.MaxValue };

    /// <summary>
    /// Whether a <c>target_type = self</c> AoE should drop the caster from the counted hit list
    /// before <c>target_area_count</c> is applied.
    /// </summary>
    /// <remarks>
    /// Self is the gather origin, not a neighbour hit. With <c>target_relation = any</c> and
    /// <c>target_area_count = 1</c>, keeping the caster sorts it to distance 0 and consumes the only
    /// slot — nearby doodads never receive <c>OnSkillHit</c> (Whalesong curse-mine reveal).
    /// Source-applied effects still use the caster directly; this only trims <c>possibleTargets</c>.
    /// </remarks>
    public static bool ExcludeCasterFromSelfAreaHits(SkillTargetType targetType, float areaRadius)
        => targetType == SkillTargetType.Self && areaRadius > 0f;

    /// <summary>
    /// Whether <c>target_area_count</c> should also truncate doodad hits.
    /// </summary>
    /// <remarks>
    /// Never. The count column caps unit effect slots (damage, buff paint). Doodads only receive
    /// <c>OnSkillHit</c> / skill-less Use, and their matching rows already gate on skill id — a
    /// combined cap lets nearer mines steal slots from the peace pad (Whalesong 36659: count 4 +
    /// BuffEffect, SkillHit on doodad 12312). Effectless mine openers (count 1) need the same
    /// split. <paramref name="skillEffectCount"/> is kept so call sites stay stable.
    /// </remarks>
    public static bool TargetAreaCountCapsDoodads(int skillEffectCount) => false;

    /// <summary>
    /// Applies <paramref name="targetAreaCount"/> to units only; doodads in the gather stay.
    /// Returns the input list when no unit cap is needed.
    /// </summary>
    public static List<BaseUnit> ApplyTargetAreaCount(
        IReadOnlyList<BaseUnit> targets,
        BaseUnit sortOrigin,
        int targetAreaCount,
        int skillEffectCount)
    {
        if (targets == null || targets.Count == 0 || targetAreaCount <= 0)
            return targets as List<BaseUnit> ?? targets?.ToList() ?? [];

        // skillEffectCount is unused: doodads are never count-capped (TargetAreaCountCapsDoodads).
        _ = skillEffectCount;

        var units = new List<BaseUnit>();
        var doodads = new List<BaseUnit>();
        foreach (var t in targets)
        {
            if (t == null)
                continue;
            if (t is Unit)
                units.Add(t);
            else
                doodads.Add(t);
        }

        if (units.Count <= targetAreaCount)
        {
            if (units.Count + doodads.Count == targets.Count)
                return targets as List<BaseUnit> ?? targets.ToList();
            units.AddRange(doodads);
            return units;
        }

        if (sortOrigin != null)
            units = units.OrderBy(t => t.GetDistanceTo(sortOrigin)).Take(targetAreaCount).ToList();
        else
            units = units.Take(targetAreaCount).ToList();

        units.AddRange(doodads);
        return units;
    }
}
