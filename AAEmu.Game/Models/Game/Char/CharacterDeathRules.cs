using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Char;

/// <summary>A debuff a resurrection gives; a <see cref="ForcedDurationMs"/> above 0 replaces the template's duration.</summary>
public readonly record struct RevivalDebuff(uint BuffId, int ForcedDurationMs = 0);

/// <summary>What a resurrection costs: the share of maximum HP and MP it restores, in percent, and the debuffs it gives.</summary>
public readonly record struct ResurrectionCosts(uint HpPercent, uint MpPercent, IReadOnlyList<RevivalDebuff> Debuffs);

/// <summary>
/// Death durability and experience from <c>item_configs.death_durability_loss_ratio</c>,
/// formulas 8/9, and unit attributes 155-157; and where a resurrection puts the character and what it costs.
/// </summary>
public static class CharacterDeathRules
{
    /// <summary>The share of maximum HP and MP a temple (return point) revive restores.</summary>
    public const uint TempleReviveVitalsPercent = 10;

    /// <summary>How long the <c>RespawnCooldown</c> debuff a temple revive gives lasts: 5 minutes.</summary>
    public const int RespawnCooldownDurationMs = 300_000;

    public static int DurabilityLoss(int current, int max, int deathRatio, int mul)
    {
        if (current <= 0 || max <= 0 || deathRatio <= 0)
            return 0;
        var raw = max * deathRatio / 100;
        return Math.Min(current, AttributeGainRules.ApplyPercentPoints(raw, mul));
    }

    /// <summary>
    /// Whether a resurrection leaves the character where it fell instead of moving it to a return point: the
    /// client asks for it in place (<c>CSResurrectCharacterPacket</c>'s one flag, sent when another player's
    /// resurrection is accepted) and such an offer is waiting. A wanted character's courthouse summons still wins.
    /// </summary>
    public static bool ResurrectsWhereFallen(bool inPlace, bool offerPending, bool summonedToCourt) =>
        inPlace && offerPending && !summonedToCourt;

    /// <summary>
    /// What a resurrection costs. Only an accepted offer that is really used - the character comes back where it
    /// fell (<see cref="ResurrectsWhereFallen"/>) - costs nothing and restores the offer's own share of HP and MP
    /// (a share of 0 counts as 1 %). Everything else costs exactly what a temple revive costs, whatever the client
    /// asked for: <see cref="TempleReviveVitalsPercent"/> of HP and MP, the war leech after a PvP death in a zone at
    /// War, Weakened Body after a death that was not PvP, and the 5-minute respawn cooldown after any death. That
    /// includes a summoned defendant who accepted a revive and goes to the courthouse instead.
    /// </summary>
    public static ResurrectionCosts Costs(bool acceptedOffer, uint offerHpPercent, uint offerMpPercent,
        bool diedInPvp, bool diedInPvpWarZone)
    {
        if (acceptedOffer)
            return new ResurrectionCosts(Math.Max(1u, offerHpPercent), Math.Max(1u, offerMpPercent), []);

        var cooldown = new RevivalDebuff((uint)BuffConstants.RespawnCooldown, RespawnCooldownDurationMs);
        IReadOnlyList<RevivalDebuff> debuffs = diedInPvpWarZone
            ? [new RevivalDebuff((uint)BuffConstants.WarZoneLeech), cooldown]
            : diedInPvp
                ? [cooldown]
                : [new RevivalDebuff((uint)BuffConstants.WeakenedBody), cooldown];
        return new ResurrectionCosts(TempleReviveVitalsPercent, TempleReviveVitalsPercent, debuffs);
    }

    /// <summary>
    /// The HP and MP a resurrection leaves: the given share of each maximum, rounded up, with at least 1 HP.
    /// </summary>
    public static (int Hp, int Mp) RevivalVitals(int maxHp, int maxMp, uint hpPercent, uint mpPercent) =>
        (Math.Max(1, (int)Math.Ceiling(maxHp * (hpPercent / 100.0))),
            Math.Max(0, (int)Math.Ceiling(maxMp * (mpPercent / 100.0))));

    public static int ClampLostExp(int experience, int levelFloor, int lost)
    {
        if (lost <= 0)
            return 0;
        var intoLevel = Math.Max(0, experience - Math.Max(0, levelFloor));
        return Math.Min(intoLevel, lost);
    }
}
