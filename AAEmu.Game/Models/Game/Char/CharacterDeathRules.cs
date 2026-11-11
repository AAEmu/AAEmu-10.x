using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>
/// Death durability and experience from <c>item_configs.death_durability_loss_ratio</c>,
/// formulas 8/9, and unit attributes 155-157; and where a resurrection puts the character.
/// </summary>
public static class CharacterDeathRules
{
    public static int DurabilityLoss(int current, int max, int deathRatio, int mul)
    {
        if (current <= 0 || max <= 0 || deathRatio <= 0)
            return 0;
        var raw = max * deathRatio / 100;
        return Math.Min(current, AttributeGainRules.ApplyPercentPoints(raw, mul));
    }

    /// <summary>
    /// Whether a resurrection leaves the character where it fell instead of moving it to a return point: the
    /// client asks for it in place (<see cref="ResurrectionKind.InPlace"/>, which accepting another player's
    /// resurrection sends) and such an offer is waiting. A wanted character's courthouse summons still wins. The
    /// free in-place and grace revives send the same kind without an offer; they are not handled yet and come back
    /// at the return point, as before.
    /// </summary>
    public static bool ResurrectsWhereFallen(ResurrectionKind kind, bool offerPending, bool summonedToCourt) =>
        kind == ResurrectionKind.InPlace && offerPending && !summonedToCourt;

    public static int ClampLostExp(int experience, int levelFloor, int lost)
    {
        if (lost <= 0)
            return 0;
        var intoLevel = Math.Max(0, experience - Math.Max(0, levelFloor));
        return Math.Min(intoLevel, lost);
    }
}
