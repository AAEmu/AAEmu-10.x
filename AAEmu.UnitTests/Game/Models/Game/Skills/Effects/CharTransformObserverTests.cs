using AAEmu.Commons.Network.Core;
using AAEmu.Game.Core.Network.Connections;
using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Char.Templates;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects;

/// <summary>
/// Someone who comes into view of a Daru gets the unit state, whose body slots are the real equipment, so the
/// form's face and body have to follow in their own visual-race packet.
/// </summary>
public class CharTransformObserverTests
{
    private const uint DaruFace = 54123;
    private readonly List<byte[]> _sentPackets = [];

    private static readonly CharacterTemplate DaruForm = new()
    {
        FaceItemId = DaruFace,
        Items = [54124, 0, 0, 0, 0, 54129, 0]
    };

    [Test]
    public async Task NewObserver_OfADaru_GetsTheFormThenTheDisarm()
    {
        var daru = new Character(new UnitCustomModelParams())
        {
            Id = 1, ObjId = 1, IsCharTransformed = true, CharTransformRace = 9, CharTransformGender = 1
        };
        var observer = Observer();

        CharTransformEffect.ShowFormTo(daru, observer, (_, _) => DaruForm);

        await Assert.That(_sentPackets.Count).IsEqualTo(2);
        await Assert.That(Opcode(_sentPackets[0])).IsEqualTo(SCOffsets.SCCharacterVisualRaceModifiedPacket);
        await Assert.That(Opcode(_sentPackets[1])).IsEqualTo(SCOffsets.SCDisarmamentChangedPacket);
        // Body: u64 character id, then the face.
        await Assert.That(BitConverter.ToUInt32(_sentPackets[0], 16)).IsEqualTo(DaruFace);
    }

    [Test]
    public async Task NewObserver_OfAnUntransformedCharacter_GetsNothingExtra()
    {
        var plain = new Character(new UnitCustomModelParams()) { Id = 1, ObjId = 1 };

        CharTransformEffect.ShowFormTo(plain, Observer(), (_, _) => DaruForm);

        await Assert.That(_sentPackets).IsEmpty();
    }

    private Character Observer()
    {
        var observer = new Character(new UnitCustomModelParams()) { Id = 2, ObjId = 2 };
        var session = Mock.Of<ISession>();
        session.SendPacket(Any<byte[]>()).Callback((byte[] bytes) => _sentPackets.Add(bytes));
        observer.Connection = new GameConnection(session.Object) { ActiveChar = observer };
        return observer;
    }

    // Length prefix, then dd, level, crc, counter, then the u16 type id (unencrypted test connection).
    private static ushort Opcode(byte[] packet) => (ushort)(packet[6] | (packet[7] << 8));
}
