using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Skills;
using TUnit.Assertions.Enums;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

/// <summary>
/// What a resurrection costs follows the offer that is really used, not the client's in-place flag: only coming back
/// where it fell on an accepted offer is free and restores the offer's share; everything else, the courthouse
/// included, costs a temple revive's 10 %, Weakened Body or the war leech, and the 5-minute respawn cooldown.
/// </summary>
public class CharacterResurrectionCostsTests
{
    private const uint OfferHp = 30;
    private const uint OfferMp = 20;

    private static readonly RevivalDebuff Cooldown = new((uint)BuffConstants.RespawnCooldown, 300_000);
    private static readonly RevivalDebuff WeakenedBody = new((uint)BuffConstants.WeakenedBody);
    private static readonly RevivalDebuff WarLeech = new((uint)BuffConstants.WarZoneLeech);

    /// <summary>
    /// Every combination of what the client asks (in place or not), whether an offer waits and whether a courthouse
    /// summons applies, after a PvE death.
    /// </summary>
    [Test]
    // An honest accepted offer: free, the offer's share.
    [Arguments(true, true, false, true)]
    // In place without an offer (a modified client): a temple revive's costs.
    [Arguments(true, false, false, false)]
    // The offer was declined and the temple chosen: as before.
    [Arguments(false, true, false, false)]
    [Arguments(false, false, false, false)]
    // A summoned defendant goes to court and pays a temple revive's costs, whether or not it accepted a revive.
    [Arguments(true, true, true, false)]
    [Arguments(true, false, true, false)]
    [Arguments(false, true, true, false)]
    [Arguments(false, false, true, false)]
    public async Task Costs_FollowTheOfferReallyUsed(bool inPlace, bool offerPending, bool summonedToCourt, bool free)
    {
        var acceptedOffer = CharacterDeathRules.ResurrectsWhereFallen(inPlace, offerPending, summonedToCourt);

        var costs = CharacterDeathRules.Costs(acceptedOffer, OfferHp, OfferMp, diedInPvp: false, diedInPvpWarZone: false);

        if (free)
        {
            await Assert.That((costs.HpPercent, costs.MpPercent)).IsEqualTo((OfferHp, OfferMp));
            await Assert.That(costs.Debuffs).IsEmpty();
        }
        else
        {
            await Assert.That((costs.HpPercent, costs.MpPercent)).IsEqualTo((10u, 10u));
            await Assert.That(costs.Debuffs).IsEquivalentTo([WeakenedBody, Cooldown], CollectionOrdering.Matching);
        }
    }

    [Test]
    public async Task ATempleRevive_ChargesForTheWayTheCharacterDied()
    {
        var pve = CharacterDeathRules.Costs(false, OfferHp, OfferMp, diedInPvp: false, diedInPvpWarZone: false);
        var pvp = CharacterDeathRules.Costs(false, OfferHp, OfferMp, diedInPvp: true, diedInPvpWarZone: false);
        var war = CharacterDeathRules.Costs(false, OfferHp, OfferMp, diedInPvp: true, diedInPvpWarZone: true);

        await Assert.That(pve.Debuffs).IsEquivalentTo([WeakenedBody, Cooldown], CollectionOrdering.Matching);
        await Assert.That(pvp.Debuffs).IsEquivalentTo([Cooldown], CollectionOrdering.Matching);
        await Assert.That(war.Debuffs).IsEquivalentTo([WarLeech, Cooldown], CollectionOrdering.Matching);
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task AnAcceptedOffer_CostsNoDebuff_HoweverTheCharacterDied(bool pvp, bool warZone)
    {
        await Assert.That(CharacterDeathRules.Costs(true, OfferHp, OfferMp, pvp, warZone).Debuffs).IsEmpty();
    }

    [Test]
    public async Task AnAcceptedOfferOfNothing_StillRestoresOnePercent()
    {
        var costs = CharacterDeathRules.Costs(true, 0, 0, false, false);

        await Assert.That((costs.HpPercent, costs.MpPercent)).IsEqualTo((1u, 1u));
    }

    [Test]
    [Arguments(1234, 999, 10u, 124, 100)]
    [Arguments(5, 0, 10u, 1, 0)]
    [Arguments(0, 0, 10u, 1, 0)]
    [Arguments(1000, 500, 30u, 300, 150)]
    [Arguments(1000, 500, 1u, 10, 5)]
    public async Task RevivalVitals_RoundUp_AndLeaveAtLeastOneHp(int maxHp, int maxMp, uint percent, int hp, int mp)
    {
        await Assert.That(CharacterDeathRules.RevivalVitals(maxHp, maxMp, percent, percent)).IsEqualTo((hp, mp));
    }

    /// <summary>The temple share is the 10 % the packet always restored (<c>Math.Ceiling(max * 0.1)</c>).</summary>
    [Test]
    public async Task RevivalVitals_AtTheTempleShare_AreTheOldTenPercent()
    {
        for (var max = 0; max <= 50_000; max += 7)
        {
            var (hp, mp) = CharacterDeathRules.RevivalVitals(max, max, CharacterDeathRules.TempleReviveVitalsPercent,
                CharacterDeathRules.TempleReviveVitalsPercent);
            if (hp != Math.Max(1, (int)Math.Ceiling(max * 0.1)) || mp != Math.Max(0, (int)Math.Ceiling(max * 0.1)))
                Assert.Fail($"max {max}: {hp}/{mp}");
        }

        await Assert.That(CharacterDeathRules.TempleReviveVitalsPercent).IsEqualTo(10u);
    }
}
