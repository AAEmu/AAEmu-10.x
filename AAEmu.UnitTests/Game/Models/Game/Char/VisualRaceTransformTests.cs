using AAEmu.Commons.Network;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Char;

/// <summary>
/// A transform (Daru Transformation, race 9) is the client's visual-race override, sent as
/// SCCharacterVisualRaceModifiedPacket (0x273). The real model id stays unchanged, so the owner is never
/// re-sent as a full unit (a self SCUnitState turns the player invisible).
/// </summary>
public class VisualRaceTransformTests
{
    [Test]
    public async Task Packet_UsesTheVisualRaceOpcode()
    {
        var packet = new SCCharacterVisualRaceModifiedPacket(1001, VisualRaceBodyParts.None, new UnitCustomModelParams());

        await Assert.That(packet.TypeId).IsEqualTo((ushort)0x273);
    }

    [Test]
    public async Task Packet_WritesCharacterIdThenSixPartsInClientOrderThenAppearance()
    {
        var parts = new VisualRaceBodyParts(Face: 54123, Body: 54129, Beard: 3, Tail: 4, Hair: 5, Horns: 6);
        var appearance = new UnitCustomModelParams();

        var stream = new SCCharacterVisualRaceModifiedPacket(1001, parts, appearance).Write(new PacketStream());
        stream.Rollback();

        await Assert.That(stream.ReadUInt64()).IsEqualTo(1001UL);
        uint[] expected = [54123, 54129, 3, 4, 5, 6];
        foreach (var id in expected)
            await Assert.That(stream.ReadUInt32()).IsEqualTo(id);

        // The appearance block follows; an empty one is a single type byte.
        await Assert.That(stream.ReadByte()).IsEqualTo((byte)0);
        await Assert.That(stream.LeftBytes).IsEqualTo(0);
    }

    [Test]
    public async Task FromTemplate_TakesTheTemplateFaceAndTheBodyPartArray()
    {
        // Template body parts are indexed by slot_type_id - 23: Face, Hair, Glasses, Horns, Tail, Body, Beard.
        uint[] items = [54124, 11, 0, 13, 14, 54129, 16];

        var parts = VisualRaceBodyParts.FromTemplate(faceItemId: 54123, items);

        await Assert.That(parts).IsEqualTo(new VisualRaceBodyParts(54123, 54129, 16, 14, 11, 13));
    }

    [Test]
    public async Task FromTemplate_WithoutAFaceItem_FallsBackToTheFaceBodyPart()
    {
        uint[] items = [54124, 0, 0, 0, 0, 0, 0];

        var parts = VisualRaceBodyParts.FromTemplate(faceItemId: 0, items);

        await Assert.That(parts.Face).IsEqualTo(54124u);
    }

    [Test]
    public async Task FromEquipment_ReadsTheSixAppearanceSlots()
    {
        var bySlot = new Dictionary<EquipmentItemSlot, uint>
        {
            [EquipmentItemSlot.Face] = 101,
            [EquipmentItemSlot.Body] = 102,
            [EquipmentItemSlot.Beard] = 103,
            [EquipmentItemSlot.Tail] = 104,
            [EquipmentItemSlot.Hair] = 105,
            [EquipmentItemSlot.Reserved] = 106,
        };

        var parts = VisualRaceBodyParts.FromEquipment(slot => bySlot.GetValueOrDefault(slot));

        await Assert.That(parts).IsEqualTo(new VisualRaceBodyParts(101, 102, 103, 104, 105, 106));
    }

    [Test]
    public async Task ClearTransient_RemovesASkillTransformButKeepsATimedVisualRace()
    {
        var transformed = new UnitCustomModelParams { Race = 4, VisualRace = 9, VisualGender = 1, VisualRaceExpiredTime = 0 };
        var timed = new UnitCustomModelParams { Race = 4, VisualRace = 2, VisualGender = 1, VisualRaceExpiredTime = 1_800_000_000 };

        VisualRaceBodyParts.ClearTransientVisualRace(transformed);
        VisualRaceBodyParts.ClearTransientVisualRace(timed);

        await Assert.That(transformed.VisualRace).IsEqualTo((byte)0);
        await Assert.That(timed.VisualRace).IsEqualTo((byte)2);
        await Assert.That(timed.VisualRaceExpiredTime).IsEqualTo(1_800_000_000L);
    }

    [Test]
    public async Task ApplyAndClear_SetAndRemoveTheOverrideOnly()
    {
        var appearance = new UnitCustomModelParams { Race = 4, Gender = 2 };

        VisualRaceBodyParts.ApplyVisualRace(appearance, race: 9, gender: 1);

        await Assert.That(appearance.VisualRace).IsEqualTo((byte)9);
        await Assert.That(appearance.VisualGender).IsEqualTo((byte)1);
        await Assert.That(appearance.VisualRaceExpiredTime).IsEqualTo(0L);
        await Assert.That(appearance.Race).IsEqualTo((byte)4);

        VisualRaceBodyParts.ClearVisualRace(appearance);

        await Assert.That(appearance.VisualRace).IsEqualTo((byte)0);
        await Assert.That(appearance.VisualGender).IsEqualTo((byte)0);
        await Assert.That(appearance.Gender).IsEqualTo((byte)2);
    }

    [Test]
    public async Task Effect_BeforeTheClientIsInWorld_UpdatesStateButAnnouncesNothing()
    {
        // A saved Daru buff is restored in CSSelectCharacter, before the client has the player unit. Sending
        // the form packets then crashed the 10.0.2.13 client on loading in (2026-11-10 12:10:58).
        var character = new Character(new UnitCustomModelParams { Race = 4, VisualRace = 9, VisualGender = 1 })
        {
            Id = 1,
            IsCharTransformed = true
        };
        var revert = new AAEmu.Game.Models.Game.Skills.Effects.CharTransformEffect { CharRaceId = 9, CharGenderId = 1, IsTransform = false };

        await Assert.That(AAEmu.Game.Models.Game.Skills.Effects.CharTransformEffect.ShouldAnnounce(character)).IsFalse();

        revert.Apply(null, null, character, null, null, null, null, DateTime.UtcNow);

        await Assert.That(character.IsCharTransformed).IsFalse();
        await Assert.That(character.ModelParams.VisualRace).IsEqualTo((byte)0);
    }

    [Test]
    public async Task Effect_RestoredBeforeSpawn_KeepsTheRealBodyInTheAppearance()
    {
        // The self SCUnitState sent on CSSpawnCharacter carries ModelParams. A visual race already set there
        // (restored Daru buff) made the client build the local player as a Daru at spawn and crash
        // (2026-11-10 12:17:56). Before the client is in world only the form is recorded.
        var character = new Character(new UnitCustomModelParams { Race = 4, Gender = 2 }) { Id = 1 };
        var transform = new AAEmu.Game.Models.Game.Skills.Effects.CharTransformEffect { CharRaceId = 9, CharGenderId = 1, IsTransform = true };

        transform.Apply(null, null, character, null, null, null, null, DateTime.UtcNow);

        await Assert.That(character.IsCharTransformed).IsTrue();
        await Assert.That(character.CharTransformRace).IsEqualTo((byte)9);
        await Assert.That(character.CharTransformGender).IsEqualTo((byte)1);
        await Assert.That(character.ModelParams.VisualRace).IsEqualTo((byte)0);
    }
}
