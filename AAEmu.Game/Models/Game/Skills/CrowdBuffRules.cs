namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// <c>buffs.crowd_*</c>: once <c>crowd_number</c> holders of the check buff stand within <c>crowd_radius</c>,
/// each of them gets <c>crowd_buff_id</c>.
/// </summary>
public static class CrowdBuffRules
{
    public static bool CrowdReached(int holders, int crowdNumber) => crowdNumber > 0 && holders >= crowdNumber;

    public static uint CheckBuffId(uint buffId, uint crowdCheckBuffId) => crowdCheckBuffId != 0 ? crowdCheckBuffId : buffId;

    /// <summary>The holders that get the crowd buff: the owner plus neighbours holding the check buff, once enough.</summary>
    public static List<T> Recipients<T>(T owner, IEnumerable<T> around, Func<T, bool> holdsCheckBuff, int crowdNumber)
    {
        var holders = around.Where(holdsCheckBuff).Append(owner).Distinct().ToList();
        return CrowdReached(holders.Count, crowdNumber) ? holders : [];
    }

    /// <summary>A crowd source whose crowd buff is not granted yet is checked again, as holders may since have gathered.</summary>
    public static bool NeedsRecheck(uint crowdBuffId, bool holdsCrowdBuff) => crowdBuffId != 0 && !holdsCrowdBuff;
}
