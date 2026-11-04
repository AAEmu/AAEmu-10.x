using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets.G2C.UnitState;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Expeditions;
using AAEmu.Game.Models.Game.Faction;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.StaticValues;

namespace AAEmu.UnitTests.Game.Core.Packets.G2C;

/// <summary>
/// The 10.0.2.13 client stores the second packed group of a character's unit state in the unit's
/// faction, expedition and family fields (Unit+0x6c6b / +0x6c6f / +0x6c73). X2Family:IsFamily()
/// tests that family field, so a non-zero third value puts the player in a phantom family.
/// </summary>
public sealed class UnitStatePackedStateTests
{
    private static uint[] SecondGroup(Character character)
    {
        var stream = new PacketStream();
        UnitStateGameplaySerializer.WritePackedState(stream, new UnitStateWireContext(character, BaseUnitType.Character));
        stream.Rollback();
        stream.ReadPisc(4);
        return stream.ReadPisc(3);
    }

    private static Character NewCharacter()
    {
        var character = new Character(new UnitCustomModelParams()) { Id = 1006, ObjId = 77, Name = "Ook" };
        character.Appellations = new CharacterAppellations(character);
        character.Faction = new SystemFaction { Id = (FactionsEnum)187 };
        return character;
    }

    [Test]
    public async Task CharacterWithoutFamilyOrGuildSendsZeroFamilyNotTheFactionId()
    {
        var group = SecondGroup(NewCharacter());

        await Assert.That(group[1]).IsEqualTo(0u);
        await Assert.That(group[2]).IsEqualTo(0u);
    }

    [Test]
    public async Task CharacterInFamilyAndGuildSendsTheirIds()
    {
        var character = NewCharacter();
        character.Family = 42;
        character.Expedition = new Expedition { Id = (FactionsEnum)1002 };

        var group = SecondGroup(character);

        await Assert.That(group[1]).IsEqualTo(1002u);
        await Assert.That(group[2]).IsEqualTo(42u);
    }
}
