namespace AAEmu.Game.Models.Game.Skills;

/// <summary>
/// <c>buffs.crowd_*</c>: once <c>crowd_number</c> holders of the check buff stand within <c>crowd_radius</c>,
/// each of them gets <c>crowd_buff_id</c>.
/// </summary>
public static class CrowdBuffRules
{
    public static bool CrowdReached(int holders, int crowdNumber) => crowdNumber > 0 && holders >= crowdNumber;

    public static uint CheckBuffId(uint buffId, uint crowdCheckBuffId) => crowdCheckBuffId != 0 ? crowdCheckBuffId : buffId;
}
