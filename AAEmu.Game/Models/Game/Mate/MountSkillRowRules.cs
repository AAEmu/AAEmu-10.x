using AAEmu.Game.Models.Game.Skills.Templates;

namespace AAEmu.Game.Models.Game.Mate;

/// <summary>
/// Which <c>mount_skills</c> row a press on a mount or vehicle bar is, and what its rider helper does to
/// the seat. Several rows share one carrier skill and differ only in the rider helper
/// <c>mount_attached_skills</c> gives them: the four human-cannon rows 69 / 72 / 74 / 179 all fire skill
/// 16650 and launch the rider with 15626 (1300 m), 15757 (1000 m), 15623 (1080 m) and 20075 (Loka).
/// </summary>
public static class MountSkillRowRules
{
    /// <summary>
    /// The <c>mount_skills.id</c> a cast of <paramref name="castSkillId"/> stands for.
    /// </summary>
    /// <param name="castSkillId">The skill the carrier was asked to use.</param>
    /// <param name="clientRowId">The row the client named in its <c>SkillCasterMount</c>, or 0.</param>
    /// <param name="rows">Every <c>mount_skills</c> row, in table order.</param>
    /// <param name="carrierRows">
    /// The rows the carrier itself carries (<c>slave_mount_skills</c> / <c>npc_mount_skills</c>); empty
    /// when the tables say nothing about it.
    /// </param>
    /// <remarks>
    /// The first row naming the skill used to win for everyone, so every human cannon launched its rider
    /// with row 69's 1300 m jump. The carrier's own rows decide first; the client's row only picks between
    /// them, so it cannot borrow a row its carrier does not have. A carrier the tables are silent about
    /// takes the client's row when it is one for this skill, and the first row otherwise, as before.
    /// </remarks>
    public static uint PickRow(uint castSkillId, uint clientRowId, IEnumerable<MountSkills> rows,
        IReadOnlyCollection<uint> carrierRows)
    {
        var candidates = rows.Where(row => row.SkillId == castSkillId).Select(row => row.Id).ToList();
        if (candidates.Count == 0)
            return 0;

        if (carrierRows is { Count: > 0 })
        {
            var carried = candidates.Where(carrierRows.Contains).ToList();
            if (carried.Count > 0)
                return carried.Contains(clientRowId) ? clientRowId : carried[0];
            return candidates[0];
        }

        return candidates.Contains(clientRowId) ? clientRowId : candidates[0];
    }

    /// <summary>
    /// Whether the rider helper has to take the rider out of the vehicle seat before it runs.
    /// </summary>
    /// <remarks>
    /// A rider helper with <c>skills.unmount</c> set moves the rider away from the vehicle: the cannon
    /// jumps (15623 / 15626 / 15757 / 20075, plots that float the rider and then leap it hundreds of
    /// metres) and the jump out of the cannon (16826). <c>Skill.Use</c> only unmounts from mates, so a
    /// slave rider stayed parented to the vehicle: the plot's leap controller was refused for a seated
    /// unit, the client was never told it had left the seat, and only the bubble buff landed.
    /// </remarks>
    public static bool RiderHelperLeavesSeat(SkillTemplate riderSkill, bool seatedOnCarrier) =>
        seatedOnCarrier && riderSkill is { Unmount: true };
}
