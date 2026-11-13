using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.DoodadObj.Static;
using AAEmu.Game.Models.Game.Faction;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.StaticValues;
using AAEmu.UnitTests.Utils.Mocks;

namespace AAEmu.UnitTests.Game.Models.Game.Units;

public class SlaveCombatSideRulesTests
{
    /// <summary>A world-spawned cannon: no summoner, no owner, template faction Friendly (1).</summary>
    private static Slave WorldCannon() =>
        new() { ObjId = 866, Faction = new SystemFaction { Id = FactionsEnum.Friendly } };

    [Test]
    public async Task OwnerlessCannon_WithDriver_FightsAsTheDriver()
    {
        // Quest 1401's Andelph cannon (slave 609) is World-spawned: no summoner, template faction Friendly.
        var gunner = new CharacterMock { ObjId = 2135 };
        var cannon = WorldCannon();
        cannon.AttachedCharacters.Add(AttachPointKind.Driver, gunner);

        await Assert.That(cannon.CombatSide).IsSameReferenceAs(gunner);
    }

    [Test]
    public async Task OwnerlessCannon_Empty_KeepsItsOwnSide()
    {
        var cannon = WorldCannon();

        await Assert.That(cannon.CombatSide).IsSameReferenceAs(cannon);
    }

    [Test]
    public async Task OwnerlessCannon_PassengerOnly_KeepsItsOwnSide()
    {
        var cannon = WorldCannon();
        cannon.AttachedCharacters.Add(AttachPointKind.Passenger0, new CharacterMock { ObjId = 2135 });

        await Assert.That(cannon.CombatSide).IsSameReferenceAs(cannon);
    }

    [Test]
    public async Task OwnerlessSlaveWithARealFaction_KeepsItsOwnSide()
    {
        // Only Friendly needs the stand-in: a world slave whose template names a real faction already
        // has a side of its own, whoever is driving it.
        var slave = new Slave { ObjId = 866, Faction = new SystemFaction { Id = FactionsEnum.Monstrosity } };
        slave.AttachedCharacters.Add(AttachPointKind.Driver, new CharacterMock { ObjId = 2135 });

        await Assert.That(slave.CombatSide).IsSameReferenceAs(slave);
    }

    [Test]
    public async Task SummonedVehicle_KeepsTheOwnersFactionItAlreadyHas()
    {
        var owner = new CharacterMock { ObjId = 1 };
        var other = new CharacterMock { ObjId = 2 };
        var tank = WorldCannon();
        tank.Summoner = owner;
        tank.AttachedCharacters.Add(AttachPointKind.Driver, other);

        await Assert.That(tank.CombatSide).IsSameReferenceAs(tank);
    }

    [Test]
    public async Task HullPart_KeepsTheHullsFaction()
    {
        // A ship cannon is a child slave whose OwnerObjId is the hull.
        var cannon = new Slave
        {
            ObjId = 900, OwnerObjId = 899, Faction = new SystemFaction { Id = FactionsEnum.Friendly }
        };
        cannon.AttachedCharacters.Add(AttachPointKind.Driver, new CharacterMock { ObjId = 2135 });

        await Assert.That(cannon.CombatSide).IsSameReferenceAs(cannon);
    }

    [Test]
    public async Task Rule_NullSeats_KeepsOwnSide()
    {
        await Assert.That(SlaveCombatSideRules.DriverSide(false, 0, FactionsEnum.Friendly, null)).IsNull();
    }

    [Test]
    [Arguments(FactionsEnum.Invalid)]
    [Arguments(FactionsEnum.Neutral)]
    [Arguments(FactionsEnum.Hostile)]
    [Arguments(FactionsEnum.Nuian)]
    public async Task Rule_AnyFactionButFriendly_KeepsOwnSide(FactionsEnum ownFaction)
    {
        var seats = new Dictionary<AttachPointKind, Character>
        {
            [AttachPointKind.Driver] = new CharacterMock { ObjId = 2135 }
        };

        await Assert.That(SlaveCombatSideRules.DriverSide(false, 0, ownFaction, seats)).IsNull();
    }
}
