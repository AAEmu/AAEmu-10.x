using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class CharacterResurrectWhereFallenTests
{
    [Test]
    [Arguments(true, true, false, true)]
    [Arguments(false, true, false, false)]
    [Arguments(false, false, false, false)]
    [Arguments(true, false, false, false)]
    [Arguments(true, true, true, false)]
    [Arguments(false, false, true, false)]
    public async Task ResurrectsWhereFallen_OnlyAnAcceptedOfferAndNeverOverTheCourt(
        bool inPlace, bool offerPending, bool summonedToCourt, bool expected)
    {
        await Assert.That(CharacterDeathRules.ResurrectsWhereFallen(inPlace, offerPending, summonedToCourt))
            .IsEqualTo(expected);
    }

    [Test]
    public async Task ResurrectionEffect_OffersItToADeadCharacter()
    {
        var (healer, fallen) = Pair();

        Resurrect(healer, fallen);

        await Assert.That(fallen.ResurrectOfferPending).IsTrue();
        await Assert.That(fallen.ResurrectHpPercent).IsEqualTo(30u);
        await Assert.That(fallen.ResurrectMpPercent).IsEqualTo(20u);
    }

    [Test]
    public async Task ResurrectionEffect_OffersNothingToTheLiving()
    {
        var (healer, standing) = Pair();
        standing.Hp = 100;

        Resurrect(healer, standing);

        await Assert.That(standing.ResurrectOfferPending).IsFalse();
        await Assert.That(standing.ResurrectHpPercent).IsEqualTo(1u);
    }

    private static void Resurrect(Character healer, Character target) =>
        new Resurrection().Execute(healer, new SkillCasterUnit(healer.ObjId), target, null, null, null, null,
            DateTime.UtcNow, 0, 30, 20, 0);

    private static (Character Healer, Character Fallen) Pair() =>
        (new Character(new UnitCustomModelParams()) { Id = 1042, ObjId = 501, Name = "Ayla", Hp = 900 },
            new Character(new UnitCustomModelParams()) { Id = 1043, ObjId = 502, Name = "Corin", Hp = 0 });
}
