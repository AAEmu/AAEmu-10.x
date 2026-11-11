using System.Numerics;

using AAEmu.Game.Core.Packets.C2G;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Effects.SpecialEffects;
using AAEmu.Game.Models.Game.Slaves;
using AAEmu.Game.Models.Game.Units;

using Portal = AAEmu.Game.Models.Game.Portal;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

public class CharacterResurrectWhereFallenTests
{
    [Test]
    [Arguments(ResurrectionKind.InPlace, true, false, true)]
    [Arguments(ResurrectionKind.ReturnPoint, true, false, false)]
    [Arguments(ResurrectionKind.ReturnPoint, false, false, false)]
    [Arguments(ResurrectionKind.InPlace, false, false, false)]
    [Arguments(ResurrectionKind.InPlace, true, true, false)]
    [Arguments(ResurrectionKind.ReturnPoint, false, true, false)]
    [Arguments(ResurrectionKind.SpecialRevive, true, false, false)]
    [Arguments(ResurrectionKind.SpecialRevive, false, false, false)]
    public async Task ResurrectsWhereFallen_OnlyAnAcceptedOfferAndNeverOverTheCourt(
        ResurrectionKind kind, bool offerPending, bool summonedToCourt, bool expected)
    {
        await Assert.That(CharacterDeathRules.ResurrectsWhereFallen(kind, offerPending, summonedToCourt))
            .IsEqualTo(expected);
    }

    /// <summary>
    /// The request is one byte with three kinds, not a flag: 2 (special revive) is not in place, and a value the
    /// client never sends is not either.
    /// </summary>
    [Test]
    [Arguments((byte)0, ResurrectionKind.ReturnPoint, false)]
    [Arguments((byte)1, ResurrectionKind.InPlace, true)]
    [Arguments((byte)2, ResurrectionKind.SpecialRevive, false)]
    [Arguments((byte)7, (ResurrectionKind)7, false)]
    public async Task TheRequestByte_IsAKind_AndOnlyOneIsInPlace(byte sent, ResurrectionKind kind, bool whereFallen)
    {
        var (healer, fallen) = Pair();
        Resurrect(healer, fallen);

        await Assert.That((ResurrectionKind)sent).IsEqualTo(kind);
        await Assert.That(fallen.TakeResurrectOffer((ResurrectionKind)sent, summonedToCourt: false)).IsEqualTo(whereFallen);
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

    [Test]
    public async Task AcceptedOffer_IsUsedOnceAndCannotBeReused()
    {
        var (healer, fallen) = Pair();
        Resurrect(healer, fallen);

        await Assert.That(fallen.TakeResurrectOffer(ResurrectionKind.InPlace, summonedToCourt: false)).IsTrue();
        await Assert.That(fallen.ResurrectOfferPending).IsFalse();
        await Assert.That(fallen.TakeResurrectOffer(ResurrectionKind.InPlace, summonedToCourt: false)).IsFalse();
    }

    [Test]
    [Arguments(ResurrectionKind.ReturnPoint, false)]
    [Arguments(ResurrectionKind.SpecialRevive, false)]
    [Arguments(ResurrectionKind.InPlace, true)]
    public async Task Offer_IsSpentByAResurrectionThatDoesNotUseIt(ResurrectionKind kind, bool summonedToCourt)
    {
        var (healer, fallen) = Pair();
        Resurrect(healer, fallen);

        await Assert.That(fallen.TakeResurrectOffer(kind, summonedToCourt)).IsFalse();
        await Assert.That(fallen.ResurrectOfferPending).IsFalse();
        await Assert.That(fallen.TakeResurrectOffer(ResurrectionKind.InPlace, summonedToCourt: false)).IsFalse();
    }

    /// <summary>
    /// The in-place kind with no offer waiting is what an unmodified client sends for the free in-place revive and
    /// the grace revive. Those are not handled yet: the character comes back at the return point, as before.
    /// </summary>
    [Test]
    public async Task InPlaceRequest_WithoutAnOffer_DoesNotStayWhereFallen()
    {
        var (_, fallen) = Pair();

        await Assert.That(fallen.TakeResurrectOffer(ResurrectionKind.InPlace, summonedToCourt: false)).IsFalse();
    }

    [Test]
    public async Task ResurrectionSpot_WhereFallenOnADeck_KeepsTheLocalTransform()
    {
        var ship = new Slave { Template = new SlaveTemplate { SlaveKind = SlaveKind.SmallSailingShip } };
        ship.Transform.Local.SetPosition(13078f, 9623f, 100f, 0f, 0f, MathF.PI / 2f);
        // Any unit standing on the deck; a Character's re-parenting also talks to its client.
        var passenger = new Slave { Template = new SlaveTemplate() };
        passenger.Transform.Parent = ship.Transform;
        passenger.Transform.Local.SetPosition(3f, -14f, 6f, 0f, 0f, 0.5f);
        var world = passenger.Transform.World;

        var spot = CSResurrectCharacterPacket.ResurrectionSpot(null, passenger.Transform);

        await Assert.That(spot.Local).IsEqualTo(new Vector3(3f, -14f, 6f));
        await Assert.That(spot.LocalYaw).IsEqualTo(0.5f);
        await Assert.That(spot.World).IsEqualTo(world.Position);
        await Assert.That(spot.WorldYaw).IsEqualTo(world.Rotation.Z);
        // The world spot is the ship's offset applied once; writing it into the local transform would apply it twice.
        await Assert.That(Vector3.Distance(spot.World, spot.Local)).IsGreaterThan(1000f);
    }

    [Test]
    public async Task ResurrectionSpot_AtAReturnPoint_IsThePortal()
    {
        var (_, fallen) = Pair();
        fallen.Transform.Local.SetPosition(10f, 20f, 30f);
        var portal = new Portal { X = 500f, Y = 600f, Z = 70f, ZRot = 1.25f };

        var spot = CSResurrectCharacterPacket.ResurrectionSpot(portal, fallen.Transform);

        await Assert.That(spot.World).IsEqualTo(new Vector3(500f, 600f, 70f));
        await Assert.That(spot.Local).IsEqualTo(spot.World);
        await Assert.That(spot.WorldYaw).IsEqualTo(1.25f);
        await Assert.That(spot.LocalYaw).IsEqualTo(1.25f);
    }

    private static void Resurrect(Character healer, Character target) =>
        new Resurrection().Execute(healer, new SkillCasterUnit(healer.ObjId), target, null, null, null, null,
            DateTime.UtcNow, 0, 30, 20, 0);

    private static (Character Healer, Character Fallen) Pair() =>
        (new Character(new UnitCustomModelParams()) { Id = 1042, ObjId = 501, Name = "Ayla", Hp = 900 },
            new Character(new UnitCustomModelParams()) { Id = 1043, ObjId = 502, Name = "Corin", Hp = 0 });
}
