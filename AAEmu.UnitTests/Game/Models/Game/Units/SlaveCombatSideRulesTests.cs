using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Units;
using AAEmu.UnitTests.Utils.Mocks;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

public class SlaveCombatSideRulesTests
{
    [Test]
    public async Task OwnerlessCannon_WithDriver_FightsAsTheDriver()
    {
        // Quest 1401's Andelph cannon (slave 609) is World-spawned: no summoner, template faction Friendly.
        var gunner = new CharacterMock { ObjId = 2135 };
        var cannon = new Slave { ObjId = 866 };
        cannon.AttachedCharacters.Add(AttachPointKind.Driver, gunner);

        await Assert.That(cannon.CombatSide).IsSameReferenceAs(gunner);
    }

    [Test]
    public async Task OwnerlessCannon_Empty_KeepsItsOwnSide()
    {
        var cannon = new Slave { ObjId = 866 };

        await Assert.That(cannon.CombatSide).IsSameReferenceAs(cannon);
    }

    [Test]
    public async Task OwnerlessCannon_PassengerOnly_KeepsItsOwnSide()
    {
        var cannon = new Slave { ObjId = 866 };
        cannon.AttachedCharacters.Add(AttachPointKind.Passenger0, new CharacterMock { ObjId = 2135 });

        await Assert.That(cannon.CombatSide).IsSameReferenceAs(cannon);
    }

    [Test]
    public async Task SummonedVehicle_KeepsTheOwnersFactionItAlreadyHas()
    {
        var owner = new CharacterMock { ObjId = 1 };
        var other = new CharacterMock { ObjId = 2 };
        var tank = new Slave { ObjId = 866, Summoner = owner };
        tank.AttachedCharacters.Add(AttachPointKind.Driver, other);

        await Assert.That(tank.CombatSide).IsSameReferenceAs(tank);
    }

    [Test]
    public async Task HullPart_KeepsTheHullsFaction()
    {
        // A ship cannon is a child slave whose OwnerObjId is the hull.
        var cannon = new Slave { ObjId = 900, OwnerObjId = 899 };
        cannon.AttachedCharacters.Add(AttachPointKind.Driver, new CharacterMock { ObjId = 2135 });

        await Assert.That(cannon.CombatSide).IsSameReferenceAs(cannon);
    }

    [Test]
    public async Task Rule_NullSeats_KeepsOwnSide()
    {
        await Assert.That(SlaveCombatSideRules.DriverSide(false, 0, null)).IsNull();
    }
}
