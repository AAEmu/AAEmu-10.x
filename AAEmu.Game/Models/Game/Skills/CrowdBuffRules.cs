using AAEmu.Game.Models.Game.Faction;

namespace AAEmu.Game.Models.Game.Skills;

/// <summary>What a crowd source counts: holders of the check buff or tag, filtered by relation or limited to the owner.</summary>
public readonly record struct CrowdCheck(uint CheckBuffId, uint CheckTagId, int Number, bool Friendly, bool Hostile, bool CheckOwner);

/// <summary>
/// <c>buffs.crowd_*</c>: a holder gets <c>crowd_buff_id</c> while <c>crowd_number</c> counted units stand within
/// <c>crowd_radius</c> of it, and loses it again when they are gone.
/// </summary>
public static class CrowdBuffRules
{
    public static bool CrowdReached(int count, int crowdNumber) => crowdNumber > 0 && count >= crowdNumber;

    /// <summary>The buff a counted unit must hold; 0 when only the tag is checked.</summary>
    public static uint CheckBuffId(uint buffId, uint crowdCheckBuffId, uint crowdCheckBuffTagId)
        => crowdCheckBuffId != 0 ? crowdCheckBuffId : crowdCheckBuffTagId != 0 ? 0 : buffId;

    public static bool HoldsCheck(CrowdCheck check, Func<uint, bool> hasBuff, Func<uint, bool> hasTag)
        => check.CheckBuffId != 0 ? hasBuff(check.CheckBuffId) : check.CheckTagId != 0 && hasTag(check.CheckTagId);

    /// <summary>Neither flag set counts every relation; the holder is friendly to itself.</summary>
    public static bool RelationCounts(RelationState relation, bool friendly, bool hostile)
        => (!friendly && !hostile)
           || (friendly && relation == RelationState.Friendly)
           || (hostile && relation == RelationState.Hostile);

    /// <param name="around">Units within the crowd radius of the holder (the holder may be absent).</param>
    public static bool Reached<T>(T holder, T owner, IEnumerable<T> around, Func<T, bool> holdsCheck,
        Func<T, RelationState> relationToHolder, CrowdCheck check) where T : class
    {
        var candidates = check.CheckOwner
            ? around.Where(unit => owner != null && ReferenceEquals(unit, owner))
            : around.Append(holder);

        var count = candidates
            .Distinct()
            .Count(unit => holdsCheck(unit)
                           && RelationCounts(ReferenceEquals(unit, holder) ? RelationState.Friendly : relationToHolder(unit),
                               check.Friendly, check.Hostile));

        return CrowdReached(count, check.Number);
    }

    /// <summary>A granted crowd buff without a duration is taken back once its crowd is gone; timed ones run out.</summary>
    public static bool RemovesGranted(bool reached, int crowdBuffDuration) => !reached && crowdBuffDuration == 0;
}
